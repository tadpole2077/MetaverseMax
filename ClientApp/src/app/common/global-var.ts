import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, Inject, signal, effect } from '@angular/core';
import { Subscription, interval, Subject } from 'rxjs';
import { OwnerDataComponent } from '../owner-data/owner-data.component';
import { AppComponent } from '../app.component';
import { AlertManagerService } from '../service/alert-manager.service';
//import TronLink from 'tronWeb';
//import TronWebProvider from 'tronweb';*/    // Massive package 599kb included in main.js file - only needed on server side Tron apps i think
import { UrlSegmentGroup, PRIMARY_OUTLET, UrlSegment, UrlTree, ActivatedRoute, Router } from '@angular/router';
import { Location } from '@angular/common';
import { STATUS, HEX_NETWORK } from '../common/enum';
import { PublicHashPipe } from '../pipe/public-hash.pipe';
import Web3, { EIP1193Provider } from 'web3';
import { EIP6963ProviderDetail } from 'web3/lib/commonjs/web3_eip6963';
import { environment } from '../../environments/environment';


interface OwnerAccount {
  wallet_active_in_world: boolean;
  matic_key: string;
  checked_matic_key: string;
  public_key: string;
  name: string;
  checked: boolean;
  pro_tools_enabled: boolean;
  avatar_id: number;
  dark_mode: boolean;
  alert_count: number;
  balance: number;
  balance_visible: boolean;
  app_shutdown_warning_alert: boolean;
}
interface RequestAccountsResponse {
  code: number, // 200：ok，4000：In-queue， 4001：user rejected
  message: string
}

interface JSend<dataObject> {
  status: string;
  data: dataObject;
}

enum WORLD {
    UNKNOWN = 0,
    TRON = 1,
    BNB = 2,
    ETH = 3
}

const APPROVAL_TYPE = {
    NONE: 0,
    NO_WALLET_ENABLED: 1,
    ACCOUNT_WITH_NO_PLOTS: 2
};

enum APPROVE_STATE {
    UPDATE = 0,
    SHOW = 1,
    HIDE = 2
}

enum PROMPT_TYPE {
    NONE,
    ETH_CONNECT,
    ETH_APPROVE,
    TRON_LOGIN,
    NO_PLOT,
}

@Injectable({
    providedIn: 'root'
})
export class Application {

    metaMask: EIP1193Provider<unknown> = null;

    public ownerAccount: OwnerAccount;
    public windowTron: any;
    public _selectedWorld: WORLD = WORLD.BNB;   // Default BNB
    public worldName = 'BSC';
    public worldURLPath = 'https://mcp3d.com/bsc/api/image/';   //Default BNB
    public firstCitizen = 24;   // Default BNB
    public ownerComponent: OwnerDataComponent = null;
    public appComponentInstance: AppComponent = null;

    public systemShutdownPending = false;

    // Flag triggers an update on any module that uses the Account Approval component
    public _requestApprove = false;

    // Flag tracks wallet allowed access to this site
    private _walletApproved = false;
    private _networkChainId: string;
    private _walletKeyFormated = '';

    public public_key: string;

    private _worldCode  = 'bnb';  // Default Bnb - Smartnet
    private rootBaseUrl: string;

    // ********** Observables ******************************
    // Service to capture when an account become active - used by components to update/enable account specific features
    private accountActiveSubject = new Subject<boolean>();
    public accountActive$ = this.accountActiveSubject.asObservable();

    private balanceChangeSubject = new Subject<boolean>();
    public balanceChange$ = this.balanceChangeSubject.asObservable();

    private systemShutdownSubject = new Subject<boolean>();
    public systemShutdown$ = this.systemShutdownSubject.asObservable();

    // Observable event - triggered on blockchain network change
    private networkChangeSubject = new Subject<string>();
    public networkChange$ = this.networkChangeSubject.asObservable();

    public approveSwitch = signal(APPROVE_STATE.HIDE);
    public promptType = signal(PROMPT_TYPE.NONE);

    // Subscription notifications within services
    //private subscriptionAlertCount$: Subscription;
    private subscriptionSystemShutdownPending$: Subscription;

    constructor(private httpClient: HttpClient, private alertManagerService: AlertManagerService,  public router: Router, private location: Location, private route: ActivatedRoute, @Inject('BASE_URL') rootBaseUrl: string) {

        this.rootBaseUrl = rootBaseUrl;     // Unknow world type at this point, checkWorldFromURL will identify.
        this.initAccount();        
        this.getProviders();

        // Actions triggered by Signal Changes
        effect(() => {
            this.ownerAccount.alert_count = this.alertManagerService.alertCount();
            //console.log(`alert count is : ${ this.alertManagerService.alertCount() }`);
        });
    }
    
    // Wallet site link Approval flag
    set requestApprove(value) {    

        this._requestApprove = value;

        if (value) {
            this.approveSwitch.set(APPROVE_STATE.SHOW);
        }
        else {
            this.approveSwitch.set(APPROVE_STATE.HIDE);
            this.promptType.set(PROMPT_TYPE.NONE);
        }
        
    }
    get requestApprove() {
        return this._requestApprove;
    }

    set worldCode(value) {
        this._worldCode = value;
    }
    get worldCode() {
        return this._worldCode;
    }

    set selectedWorld(value) {
        this._selectedWorld = value;
    }
    get selectedWorld() {
        return this._selectedWorld;
    }

    // Check Metamask Provider :  Supporting Metamask & CoinbaseWallet  
    set walletApproved(value) {

        this._walletApproved = value;
        const publicHashPipe = new PublicHashPipe();

        if (this._walletApproved) {
            this._walletKeyFormated = publicHashPipe.transform( this.public_key );
        }
        else {
            this._walletKeyFormated = '';
        }
    }
    get walletApproved() {
        return this._walletApproved;
    }

    // Get formated Public key in shorthand text for use within Markup UI.
    get walletKeyFormated() {
        return this._walletKeyFormated;
    }

    get networkChainId() {
        return this._networkChainId;
    }

    get baseUrl() {
        return this.rootBaseUrl + 'api/' + this.worldCode;
    }

    initAccount() {

        this.ownerAccount = {
            wallet_active_in_world: false,
            public_key: '',
            matic_key: 'Not Found',
            checked_matic_key: '',
            name: '',
            checked: false,
            pro_tools_enabled: false,
            avatar_id: 0,
            dark_mode: true,        // default mode
            alert_count: 0,
            balance: 0,
            balance_visible: false,
            app_shutdown_warning_alert: false
        };    

        // Release any prior subscribed sub, active wallet accounts may change
        //if (this.subscriptionAlertCount$) {
        //    this.subscriptionAlertCount$.unsubscribe();
        //    this.subscriptionAlertCount$ = null;
        //}
        if (this.subscriptionSystemShutdownPending$) {
            this.subscriptionSystemShutdownPending$.unsubscribe();
            this.subscriptionSystemShutdownPending$ = null;
        }

        // Subscribe to observables related to account
        //this.subscriptionAlertCount$ = this.alertManagerService.alertCount$.subscribe(count => {
        //    this.ownerAccount.alert_count = this.alertManagerService.alertCount();
        //});

        this.subscriptionSystemShutdownPending$ = this.alertManagerService.systemShutdownPending$.subscribe(shutdown => {
            this.systemShutdownPending = shutdown;
        });

    }

 
    getProviders = async (system: number = WORLD.ETH) => {

        // Call and wait for the promise to resolve - Typescript requires mapping to type
        const providers = await Web3.requestEIP6963Providers() as Map<string, EIP6963ProviderDetail>;

        for (const [key, value] of providers) {

            /* Based on your DApp's logic show use list of providers and get selected provider's UUID from user for injecting its EIP6963ProviderDetail.provider EIP1193 object into web3 object */

            if (value.info.name === 'MetaMask') {

                this.metaMask = value.provider;

                this.setEventListeners(system);
            }
        }

        // Web3 feature does seems to work -  lock & unlock Metamask
        Web3.onNewProviderDiscovered((provider) => {
            // Log the populated providers map object, provider.detail has Map of all providers yet discovered
            console.log('New Provider Identified: ', provider.detail);

            // add logic here for updating UI of your DApp
        });
        
        console.log('2');

    };

    // Only set once to avoid dups, remove listeners when switching worlds.
    setEventListeners = async (system: number) => {

        // On wallet account change - recheck linked account    
        //const provider = await DetectEthereumProvider();      // incudes 3 second timeout - useful to initiate ethereum object on client load.
        const ethereum = globalThis.ethereum;

        // Remove all prior listeners
        if (this.metaMask) {
            this.metaMask.removeListener('accountsChanged', this.ethAccountsChanged);
            this.metaMask.removeListener('chainChanged', this.ethNetworkChanged);
        }
        if (ethereum) {
            ethereum.removeListener('accountsChanged', this.ethAccountsChanged);
            ethereum.removeListener('networkChanged', this.ethNetworkChanged);
        }
        window.removeEventListener('message', this.trxAccountsChanged);

        // Add new Listeners
        if (system === WORLD.ETH || system === WORLD.BNB) {
            // Ensure only one instance of Eth event handler - remove any existing ethereum obj using Node.js EventEmitter tech
            // Metamask using EIP6963Providers
            if (this.metaMask) {

                this.metaMask.on('accountsChanged', this.ethAccountsChanged);
                this.metaMask.on('chainChanged', this.ethNetworkChanged);
            }
            else {
                ethereum.on('accountsChanged', this.ethAccountsChanged);

                // metamask : networkChanged depreciated on 3/2024 : https://docs.metamask.io/whats-new/#march-2024
                ethereum.on('networkChanged', this.ethNetworkChanged);
            }
        }
        else if (system === WORLD.TRON) {

            globalThis.addEventListener('message', this.trxAccountsChanged);

            const ethereum = (window as any).ethereum;
            if (ethereum) {
                ethereum.removeListener('accountsChanged', this.trxAccountsChanged);     // ethereum obj using Node.js EventEmitter
            }
        }

    };  

    // Check Metamask Provider :  Supporting Metamask & CoinbaseWallet
    checkApprovedWalletType = async () => {

        const ethereum = globalThis.ethereum;

        let approved = false;
        
        // EIP1193Provider active check
        if (this.metaMask) {
            approved = true;
        }
        else if (ethereum == null) {
            approved =false;   // legacy ethereum compatible plugin not found
        }
        else {
            approved =ethereum.isTrustWallet || ethereum.isCoinbaseWallet;
        }

        if (approved) {

            if (this.metaMask && ethereum.isTrustWallet) {
                console.log('Do you have multiple wallets installed?');
                return false;
            }
        }

        return approved;
    };

    // Using named function var with [ES6 Arrow Function] - allows use of [this] pointing to the original caller class, otherwise the eventEmitter class will be used.
    //@log
    private ethAccountsChanged = (accounts) => {
    
        console.log('Ethereum Account Changed');

        const priorDarkModeStatus = this.ownerAccount.dark_mode;      // retain existing mode
        this.initAccount();
        this.ownerAccount.dark_mode = priorDarkModeStatus;

        this.getEthereumAccounts(this.baseUrl, true);

    };

    private ethNetworkChanged = (chainId: string) => {
        console.log('Network change to ' + chainId);

        this._networkChainId = chainId;
        this.networkChangeSubject.next(chainId);

    // Get Hex value of chain and push to subject - any observables listeners will pick up and react.
    //const ethereum = (window as any).ethereum;
    //ethereum.request({ method: "eth_chainId", params: [] }).then((chainIdHex) => {
    //  this.networkChangeSubject.next(chainIdHex);
    //});    
    };

    // Using [ES6 Arrow Function], to support (a) using (component) this obj ref (b)support  window.removeEventListener()
    private trxAccountsChanged = (e) => {
    /*if (e.data.message && e.data.message.action == "setAccount") {
      console.log("setAccount event", e.data.message);
      console.log("current address:", e.data.message.data.address);

      this.globals.checkTronAccountKey(this.baseUrl, this.cdf);
    }*/

        if (e.data.message && e.data.message.action == 'accountsChanged') {
            console.log('Tron accountsChanged event', e.data.message);
            console.log('Tron current address:', e.data.message.data.address);

            const priorDarkModeStatus = this.ownerAccount.dark_mode;      // retain existing mode
            this.initAccount();
            this.ownerAccount.dark_mode = priorDarkModeStatus;

            this.checkTronAccountKey(this.baseUrl, true);
        }

    };

  // Trigger check on user balance
  @log()
    updateUserBankBalance(baseUrl: string, maticKey: string) {

        let params = new HttpParams();
        params = params.append('matic_key', maticKey);

        this.balanceChangeSubject.next(false);      // Reset to off

        this.httpClient.get<number>(baseUrl + '/bank/GetBalance', { params: params })
            .subscribe({
                next: (result) => {
                    this.ownerAccount.balance = result;
                    this.balanceChangeSubject.next(true);
                },
                error: (error) => { console.error(error); }
            });
    
    }

  //@log('trace')
  checkUserAccountKey(OwnerPublicKey: string, baseUrl: string, checkMyPortfolio: boolean) {

      let params = new HttpParams();
      params = params.append('owner_public_key', OwnerPublicKey);

      // Clear any prior alert subscription - due to switching worlds.
      this.alertManagerService.disableAlertChecker();


      this.httpClient.get<JSend<OwnerAccount>>(baseUrl + '/OwnerData/CheckHasPortfolio', { params: params })
          .subscribe({
              next: (result) => {

                  if (result.status == STATUS.SUCCESS) {
                      this.ownerAccount = result.data;
                      this.ownerAccount.checked = true;

                      this.requestApprove = false;
                      if (this.ownerAccount.wallet_active_in_world) {
                          // Interval account alert checker job
                          this.alertManagerService.enableAlertChecker(baseUrl, this.ownerAccount.matic_key).then();

                          // Trigger account active observable flag 
                          this.accountActiveSubject.next(true);
                      }
                      else {              
                          this.promptType.set(PROMPT_TYPE.NO_PLOT);                                          
                          this.accountActiveSubject.next(false);                          
                      }

                      // Update alert count signal on init retrival of user account.
                      this.alertManagerService.alertCount.set(this.ownerAccount.alert_count);

                      if (checkMyPortfolio) {
                          this.checkMyPortfolio();
                      }

                      // On each user accunt init check - find current system state for use with transaction feature enabling
                      this.systemShutdownPending = this.ownerAccount.app_shutdown_warning_alert;
                      this.systemShutdownSubject.next(this.systemShutdownPending);
                  }

                  // Apply owner stored preference for dark_mode theme
                  this.appComponentInstance.darkModeChange(this.ownerAccount.dark_mode);
              },
              error: (error) => { console.error(error); }
          });


      return;
  }


  getTronAccounts = async (baseUrl: string) => {

      let attempts = 0;
      //const subTron: Subscription;
      //const TronWeb = require('tronweb')
      //const tronWebProvider = await TronWebProvider;
      //let requestAccountsResponse: RequestAccountsResponse;
      //const TronWeb = require('tronweb')
      //const HttpProvider = TronWeb.providers.HttpProvider;

      //let tronWeb;
      // if ((window as any).tronLink.ready) {
      //   tronWeb = (window as any).tronWeb;
      // }
      // else {
      //   const res = await (window as any).tronLink.request({ method: 'tron_requestAccounts' });
      //   if (res.code === 200) {
      //     tronWeb = (window as any).tronLink.tronWeb;
      //   }
      // }
      //const tronLink = TronLink();
      //const accounts = await TronLink.request({method:"tron_requestAccounts"})

      // reset wallet key format - updates UI markup usage
      this.walletApproved =false;


      // Delay check on Tron Widget load and init, must be a better way of hooking into it.  Try to find Tron account 5 times - 1 per 500ms, on find run WS or end.
      const subTron: Subscription = interval(500)
          .subscribe(
              async (val) => {

                  attempts++;
                  const tronWeb = globalThis.tronWeb;
                  const ownerPublicKey: any = tronWeb == null ? null : tronWeb.defaultAddress;          

                  // iterate 5 attempts to find tron account, even with tronWeb lib loaded, account may not yet be initiated.
                  if (attempts >= 5) {

                      subTron.unsubscribe();
                      this.checkTronAccountKey(baseUrl, false);    // Show login request/approval bar

                  }
                  else if (tronWeb && ownerPublicKey != null && ownerPublicKey.base58 != false) {

                      subTron.unsubscribe();

                      const x = tronWeb.isConnected();
                      //x.then(() => { console.log("connection response : " + x) });
                      //let x2 = tronWeb.isTronLink;      // true/false - will also Force an dApp approve connection.

                      this.checkTronAccountKey(baseUrl, false);

                      //requestAccountsResponse = await tronWebProvider.request({ method: 'tron_requestAccounts' });
                      //requestAccountsResponse = await tronWeb.request({ method: 'tron_requestAccounts' });
                      //if (requestAccountsResponse) {
                      //    console.log("requestAccountsResponse : " + requestAccountsResponse);}
                      //}
                  }
              }
          );
  };

  checkTronAccountKey(baseUrl: string, checkMyPortfolio: boolean) {

      const tronWeb = (window as any).tronWeb;
      const ownerPublicKey: any = tronWeb == null ? null : tronWeb.defaultAddress;
    
      if (ownerPublicKey != null && ownerPublicKey.base58 != false) {

          this.public_key = ownerPublicKey.base58;
          this.walletApproved = true;
          this._networkChainId = HEX_NETWORK.TRON;

          this.checkUserAccountKey(ownerPublicKey.base58, baseUrl, checkMyPortfolio);
     
      }
      else {

          //this.appComponentInstance.darkModeChange(false);
          this.accountActiveSubject.next(false);              // Mark account status as false to any monitors of this observable

          this.promptType.set(PROMPT_TYPE.TRON_LOGIN);
          this.requestApprove = true;

          if (checkMyPortfolio) {
              this.checkMyPortfolio();
          }

          this.appComponentInstance.darkModeChange(this.ownerAccount.dark_mode);
      }
    
  }

  // Triggered by (a) Change World Type (b) On initial load of page or redirect load
  getEthereumAccounts = async (baseUrl: string, checkMyPortfolio: boolean) => {

      const ethereum = (window as any).ethereum;
      const walletExtension = this.metaMask != null ? this.metaMask : ethereum;

      // Reset approval settings - if valid wallet found/active will be initialized.
      this.walletApproved = false;

      // Check Metamask Provider :  Supporting Metamask & CoinbaseWallet
      if (await this.checkApprovedWalletType()) {

          const chainId = await walletExtension.request({ method: 'eth_chainId' });
          const accounts = await walletExtension.request({ method: 'eth_accounts' });

          if (accounts && accounts.length) {

              const selectedAddress = accounts[0];
              console.log('>>>Ethereum Account linked<<<');
              console.log('Key = ', selectedAddress);    // previously using depreciated ethereum.selectedAddress

              this.requestApprove = false;
              this.public_key = selectedAddress;
              this.walletApproved = true;
              //this.connectWallet = false;
              this._networkChainId = chainId;

              // trigger subscribed event handlers
              this.accountActiveSubject.next(true);

              this.checkUserAccountKey(selectedAddress, baseUrl, checkMyPortfolio);
          }
          else {
              console.log('>>>No Ethereum Account linked<<<');
              console.log('ChainId = ', chainId);

              //this.appComponentInstance.darkModeChange(false);
              this.accountActiveSubject.next(false);              // Mark account status as false to any monitors of this observable

              // Zone update components impacted by no connected wallet        
              //this.connectWallet = true;
              this.promptType.set(PROMPT_TYPE.ETH_CONNECT);
              this.approveSwitch.set(APPROVE_STATE.SHOW);

              if (checkMyPortfolio) {
                  this.checkMyPortfolio();
              }

              this.appComponentInstance.darkModeChange(this.ownerAccount.dark_mode);
          }      
      }

      return;
  };

  // Check if TronLink Wallet enabled, and site approved.
  approveTronAccountLink = async () => {

      const tronWeb = (globalThis).tronWeb;
      //const ownerPublicKey: any = tronWeb.defaultAddress;

      if (tronWeb && tronWeb.isTronLink) {
          try {
              const accountsApproved = await tronWeb.request({ method: 'tron_requestAccounts', params: { websiteName: 'MetaverseMax.com' } });
              console.log('tron approved:', accountsApproved);
          }
          catch (err) {
              console.log(err);
          }
      }
  };

  // Check if Ethemeum Wallet enabled, and account - site linked.
  approveEthereumAccountLink = async () => {

      const ethereum = globalThis.ethereum;

      const accountsApproved = await ethereum.request({ method: 'eth_requestAccounts' });
      const accountAddress = accountsApproved != null ? accountsApproved[0] : '';

      const chainId = await ethereum.request({ method: 'eth_chainId' });

      if (accountAddress && accountAddress !== '') {
          console.log('>>>Ethereum Account linked<<<');
          console.log('Key = ', accountAddress);

          this.ownerAccount.public_key = accountAddress;
          this.requestApprove = false;          
      }
      else {
          console.log('>>>No Ethereum Account linked<<<');
          console.log('ChainId = ', chainId);
          this.requestApprove = true;
          this.promptType.set(PROMPT_TYPE.ETH_APPROVE);
      }

      return accountAddress;
  };

  checkMyPortfolio() {

      // CHECK if owner-data?matic=myportfolio, then reload portfolio as account just linked
      const segmentList: UrlSegment[] = this.extractPathComponents(this.location.path());
      if (segmentList) {

          const lastComponentName: string = segmentList[segmentList.length - 1].path.toLowerCase();
          const requestOwnerMatic = this.route.snapshot.queryParams['matic'];
          if (lastComponentName == 'owner-data' && requestOwnerMatic == 'myportfolio') {
        
              if (this.ownerComponent) {
                  this.ownerComponent.triggerSearchByMatic(true);
              }
          }
      }
  }

  // typically para extractPathComponents(this.location.path())
  extractPathComponents(path: string) {

      const routeTree: UrlTree = this.router.parseUrl(path);
      const routeSegmentGroup: UrlSegmentGroup = routeTree.root.children[PRIMARY_OUTLET];
      let segmentList: UrlSegment[] = null;
      //const lastComponentName = '/';

      if (routeSegmentGroup != undefined) {
          segmentList = routeSegmentGroup.segments;
      }

      return segmentList;
  }


}

export type LOG_TYPE = 'console' | 'file' | 'rest' | 'trace';
// TS Decorators, factory pattern
export function log(logType: LOG_TYPE = 'console') {

    return function logDecorator(target: any, property: string, descriptor?: PropertyDescriptor) {
    //const DEBUG = true;   // must be defined before initialization stage of TS compile
        const PROD = environment.production;

        if (!PROD) {
            const wrapped = descriptor.value;        // copy of method wrapped by decorator

            if (logType === 'console' || logType ==='trace') {
                descriptor.value = function () {

                    if (logType === 'trace') {
                        console.trace(`${property} method started`);
                    }
                    else { 
                        console.log(`${property} method started`);
                    }

                    try {
                        wrapped.apply(this, arguments);
                    }
                    catch (err) {
                        console.log(`${property} method error: `, err);
                    }

                    console.log(`${property} method completed`);
                };
            }      
        }
    };
}

export function log2(target: any, property: string, descriptor?: PropertyDescriptor){
 
    //const DEBUG = true;   // must be defined before initialization stage of TS compile
    const PROD = environment.production;

    if (!PROD) {
        const wrapped = descriptor.value;        // copy of method wrapped by decorator.  descriptor.value returns undefined - syntax change when using angular.

        descriptor.value = function () {

            console.log(`${property} method started`);
            try {
                wrapped.apply(this, arguments);
            }
            catch (err) {
                console.log(`${property} method error: `, err);
            }
            console.log(`${property} method completed`);
        };
    }
  
}


export {
    OwnerAccount,
    WORLD,
    JSend,
    APPROVAL_TYPE,
    APPROVE_STATE,
    PROMPT_TYPE
};
