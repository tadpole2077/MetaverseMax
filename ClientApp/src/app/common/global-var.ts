import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, ElementRef, ChangeDetectorRef } from '@angular/core';
import { Subscription, interval, Subject } from 'rxjs';
import { MatBottomSheet, MatBottomSheetRef } from '@angular/material/bottom-sheet';
import { AccountApproveComponent } from '../account-approve/account-approve.component';
import { OwnerDataComponent } from '../owner-data/owner-data.component';
import { AppComponent } from '../app.component';
import DetectEthereumProvider from '@metamask/detect-provider';
//import TronLink from 'tronWeb';
//import TronWebProvider from 'tronweb';*/    // Massive package 599kb included in main.js file - only needed on server side Tron apps i think
import { Router, UrlSegmentGroup, PRIMARY_OUTLET, UrlSegment, UrlTree, ActivatedRoute } from '@angular/router';
import { Location } from '@angular/common';
import { ALERT_TYPE, ALERT_ICON_TYPE, PENDING_ALERT } from '../common/enum'
import { AlertBottomComponent } from '../alert-bottom/alert-bottom.component';


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
  balance_visible: boolean
}
interface RequestAccountsResponse {
  code: number, // 200：ok，4000：In-queue， 4001：user rejected
  message: string
}


interface AlertCollection {
  historyCount: number,
  alert: AlertPending[]
}
interface AlertPending {
  alert_pending_key: number,
  last_updated: string,
  alert_message: string,
  alert_type: number,
  alert_id: number,
  icon_type: number,
  icon_type_change: number,
  trigger_active: boolean,  
}

interface AlertPendingManager {
  manage: boolean,  
  alert: AlertPending[]
}


const WORLD = {
  UNKNOWN: 0,
  TRON: 1,
  BNB: 2,
  ETH: 3
}

const APPROVAL_TYPE = {
  NONE: 0,
  NO_WALLET_ENABLED: 1,
  ACCOUNT_WITH_NO_PLOTS: 2
}


@Injectable()
export class Globals {

  public ownerAccount: OwnerAccount;
  public windowTron: any;
  public selectedWorld: number = WORLD.UNKNOWN;   // Default Tron
  public worldCode: string = "trx";  // Default Tron
  public worldName: string = "Tron";
  public worldURLPath: string = "https://mcp3d.com/tron/api/image/";   //Default Tron
  public firstCitizen: number = 0;   // Default Tron  
  public approveSwitchComponent: AccountApproveComponent;
  public homeCDF: ChangeDetectorRef = null;
  public menuCDF: ChangeDetectorRef = null;
  public ownerCDF: ChangeDetectorRef = null;
  public ownerComponent: OwnerDataComponent = null;
  public appComponentInstance: AppComponent = null;
  public alertSub: Subscription;
  public manualFullActive: boolean = false;
  public autoAlertCheckProcessing: boolean = false;
  public bottomAlertRef: MatBottomSheetRef = null;

  // Flag triggers an update on any module that uses the Account Approval component
  public _requestApprove: boolean = false;

  set requestApprove(value) {    

    let changed = this._requestApprove != value;
    this._requestApprove = value;

    // Trigger any Approve component to update
    if (changed && this.approveSwitchComponent) {
      if (value) {
        this.approveSwitchComponent.show();
      }
      else {
        this.approveSwitchComponent.hide();
      }
    }
    //console.log("RequestApprove:", this._requestApprove);
  }
  get requestApprove() {
    return this._requestApprove;
  }

  public approvalType: number = APPROVAL_TYPE.NONE;

  // ********** Observables ******************************
  // Service to capture when an account become active - used by components to update/enable account specific features
  private accountActiveSubject = new Subject<boolean>()
  public accountActive$ = this.accountActiveSubject.asObservable()

  private balanceChangeSubject = new Subject<boolean>()
  public balaceChange$ = this.balanceChangeSubject.asObservable()


  constructor(private httpClient: HttpClient, private alertSheet: MatBottomSheet, public router: Router, private location: Location, private route: ActivatedRoute) {
    
    this.initAccount();
    
  }

  initAccount() {

    this.ownerAccount = {
      wallet_active_in_world: false,
      public_key: "",
      matic_key: "Not Found",
      checked_matic_key: "",
      name: "",
      checked: false,
      pro_tools_enabled: false,
      avatar_id: 0,
      dark_mode: false,
      alert_count: 0,
      balance: 0,
      balance_visible: false
    };  

  }

  // Trigger check on user balance
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
        error: (error) => { console.error(error) }
      });
    
  }

  checkUserAccountKey(OwnerPublicKey: string, baseUrl: string, checkMyPortfolio: boolean) {

    let params = new HttpParams();
    params = params.append('owner_public_key', OwnerPublicKey);

    // Clear any prior alert subscription - due to switching worlds.
    if (this.alertSub && !this.alertSub.closed) {
      this.alertSub.unsubscribe();
      this.alertSub = null;
    }

    this.httpClient.get<OwnerAccount>(baseUrl + '/OwnerData/CheckHasPortfolio', { params: params })
      .subscribe({
        next: (result) => {

          this.ownerAccount = result;
          this.ownerAccount.checked = true;

          if (this.ownerAccount.wallet_active_in_world) {
            this.requestApprove = false;
            this.approvalType = APPROVAL_TYPE.NONE;

            this.enableAlertChecker(baseUrl, this.ownerAccount.matic_key);               // Interval account alert checker job
            this.accountActiveSubject.next(true);
          }
          else {
            this.requestApprove = true;
            this.approvalType = APPROVAL_TYPE.ACCOUNT_WITH_NO_PLOTS;
            this.accountActiveSubject.next(false);
          }

          this.requestApproveRefresh();

          if (checkMyPortfolio) {
            this.checkMyPortfolio();
          }

          // Apply owner stored preference for dark_mode theme
          this.appComponentInstance.darkModeChange(this.ownerAccount.dark_mode);
        },
        error: (error) => { console.error(error) }
      });


    return;
  }


  async getTronAccounts(baseUrl: string) {

    let attempts: number = 0;
    let subTron: Subscription;
    //const TronWeb = require('tronweb')
    //const tronWebProvider = await TronWebProvider;
    let requestAccountsResponse: RequestAccountsResponse;
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



    // Delay check on Tron Widget load and init, must be a better way of hooking into it.  Try to find Tron account 5 times - 1 per 500ms, on find run WS or end.
    subTron = interval(500)
      .subscribe(
        async (val) => {

          attempts++;
          const tronWeb = (window as any).tronWeb;
          let ownerPublicKey: any = tronWeb == null ? null : tronWeb.defaultAddress;          

          // iterate 5 attempts to find tron account, even with tronWeb lib loaded, account may not yet be initiated.
          if (attempts >= 5) {

            subTron.unsubscribe();
            this.checkTronAccountKey(baseUrl, false);    // Show login request/approval bar

          }
          else if (tronWeb && ownerPublicKey != null && ownerPublicKey.base58 != false) {

            subTron.unsubscribe();

            let x = tronWeb.isConnected();
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
  }

  checkTronAccountKey(baseUrl: string, checkMyPortfolio: boolean) {

    const tronWeb = (window as any).tronWeb;
    let ownerPublicKey: any = tronWeb == null ? null : tronWeb.defaultAddress;
    
    if (ownerPublicKey != null && ownerPublicKey.base58 != false) {

      this.checkUserAccountKey(ownerPublicKey.base58, baseUrl, checkMyPortfolio);
     
    }
    else {

      //this.appComponentInstance.darkModeChange(false);
      this.accountActiveSubject.next(false);              // Mark account status as false to any monitors of this observable

      this.requestApprove = true;
      this.approvalType = APPROVAL_TYPE.NO_WALLET_ENABLED;

      this.requestApproveRefresh();

      if (checkMyPortfolio) {
        this.checkMyPortfolio();
      }
    }
    
  }

  // Triggered by (a) Change World Type (b) On initial load of page or redirect load
  async getEthereumAccounts(baseUrl: string, checkMyPortfolio: boolean) {

    const provider = await DetectEthereumProvider();
    const ethereum = (window as any).ethereum;

    // Check Metamask Provider
    if (provider && provider.isMetaMask) {

      if (provider !== ethereum) {
        console.log('Do you have multiple wallets installed?');
        return;
      }

      const chainId = await ethereum.request({ method: 'eth_chainId' });
      const accounts = await ethereum.request({ method: 'eth_accounts' });

      if (accounts && accounts.length) {

        const selectedAddress = accounts[0];
        console.log(">>>Ethereum Account linked<<<");
        console.log("Key = ", selectedAddress);    // previously using depreciated ethereum.selectedAddress
        this.requestApprove = false;        

        this.checkUserAccountKey(selectedAddress, baseUrl, checkMyPortfolio);
      }
      else {
        console.log(">>>No Ethereum Account linked<<<");
        console.log("ChainId = ", chainId);

        //this.appComponentInstance.darkModeChange(false);
        this.accountActiveSubject.next(false);              // Mark account status as false to any monitors of this observable

        this.requestApprove = true;
        this.requestApproveRefresh();

        if (checkMyPortfolio) {
          this.checkMyPortfolio();
        }
      }      
    }

    return;
  }

  async approveTronAccountLink(httpClient: HttpClient, baseUrl: string) {

    const tronWeb = (window as any).tronWeb;
    let ownerPublicKey: any = tronWeb.defaultAddress;

  }

  async approveEthereumAccountLink(httpClient: HttpClient, baseUrl: string) {

    const ethereum = (window as any).ethereum;

    const accountsApproved = await ethereum.request({ method: 'eth_requestAccounts' });
    const accountAddress = accountsApproved != null ? accountsApproved[0] : "";

    const chainId = await ethereum.request({ method: 'eth_chainId' });

    if (accountAddress && accountAddress !== "") {
      console.log(">>>Ethereum Account linked<<<");
      console.log("Key = ", accountAddress);

      this.ownerAccount.public_key = accountAddress;
      this.requestApprove = false;      
    }
    else {
      console.log(">>>No Ethereum Account linked<<<");
      console.log("ChainId = ", chainId);
      this.requestApprove = true;
    }

    return accountAddress;
  }

  requestApproveRefresh() {
    if (this.approveSwitchComponent) {
      this.approveSwitchComponent.update();
    }

    if (this.menuCDF) {
      this.menuCDF.detectChanges();
    }
    if (this.homeCDF) {
      this.homeCDF.detectChanges();   // show/hide buttons based on account settings.
    }

  }

  checkMyPortfolio() {

    // CHECK if owner-data?matic=myportfolio, then reload portfolio as account just linked
    let segmentList: UrlSegment[] = this.extractPathComponents(this.location.path());
    if (segmentList) {

      let lastComponentName: string = segmentList[segmentList.length - 1].path.toLowerCase();
      let requestOwnerMatic = this.route.snapshot.queryParams["matic"];
      if (lastComponentName == "owner-data" && requestOwnerMatic == "myportfolio") {
        
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
    let lastComponentName: string = "/";

    if (routeSegmentGroup != undefined) {
      segmentList = routeSegmentGroup.segments;
    }

    return segmentList;
  }

  enableAlertChecker(baseUrl: string, ownerMaticKey: string) {
    
    var that = this;
    let alertPendingManager: AlertPendingManager = {
      alert: null,
      manage: false
    };
    let params = new HttpParams();    

    params = params.append('matic_key', ownerMaticKey);
    params = params.append('pending_alert', PENDING_ALERT.UNREAD);

    // Defencive coding - Dont run double interval
    if (this.alertSub != null) {
      return;
    }

    // 3 min interval checking alerts
    this.alertSub = interval(180000)
      .subscribe(
        async (val) => {

          console.log("Account Alert Check : " + new Date());

          // Skip interval check if full history is currently and manually open
          if (that.bottomAlertRef != null && that.manualFullActive == true) {
            console.log("Alert full History currently Open - skip new alert check : " + new Date());
            return;
          }

          this.httpClient.get<AlertCollection>(baseUrl + '/OwnerData/GetPendingAlert', { params: params })
            .subscribe({
              next: (result) => {

                alertPendingManager.alert = result.alert;
                that.ownerAccount.alert_count = result.historyCount;
                that.autoAlertCheckProcessing = true;

                if (alertPendingManager.alert && alertPendingManager.alert.length > 0) {                  

                  that.bottomAlertRef = that.alertSheet
                    .open(AlertBottomComponent, {
                      data: alertPendingManager,
                    });
           
                }
              },
              error: (error) => {
                console.error("WARNING Account Alert Check ERROR : " + error);
              }
            });
        }
    );
    
  }

}

export {
  OwnerAccount,
  WORLD,
  AlertPending,
  AlertPendingManager,
  AlertCollection,
  APPROVAL_TYPE
}
