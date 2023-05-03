import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, ElementRef, ChangeDetectorRef } from '@angular/core';
import { Subscription, interval } from 'rxjs';
import { AccountApproveComponent } from '../account-approve/account-approve.component';
import { OwnerDataComponent } from '../owner-data/owner-data.component';
import { AppComponent } from '../app.component';
import DetectEthereumProvider from '@metamask/detect-provider';
/*import TronWebProvider from 'tronweb';*/    // Massive package 599kb included in main.js file - only needed on server side Tron apps i think
import { Router, UrlSegmentGroup, PRIMARY_OUTLET, UrlSegment, UrlTree, ActivatedRoute } from '@angular/router';
import { Location } from '@angular/common';


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
}
interface RequestAccountsResponse {
  code: Number, // 200：ok，4000：In-queue， 4001：user rejected
  message: String
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
    console.log("RequestApprove:", this._requestApprove);
  }
  get requestApprove() {
    return this._requestApprove;
  }

  public approvalType: number = APPROVAL_TYPE.NONE;  


  constructor(public router: Router, private location: Location, private route: ActivatedRoute) {
    
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
      dark_mode: false
    };  

  }

  CheckUserAccountKey(OwnerPublicKey: string, httpClient: HttpClient, baseUrl: string, checkMyPortfolio: boolean) {

    let params = new HttpParams();
    params = params.append('owner_public_key', OwnerPublicKey);

    httpClient.get<OwnerAccount>(baseUrl + '/OwnerData/CheckHasPortfolio', { params: params })
      .subscribe((result: OwnerAccount) => {

        this.ownerAccount = result;
        this.ownerAccount.checked = true;
        
        if (this.ownerAccount.wallet_active_in_world) {
          this.requestApprove = false;
          this.approvalType = APPROVAL_TYPE.NONE;
        }
        else {
          this.requestApprove = true;
          this.approvalType = APPROVAL_TYPE.ACCOUNT_WITH_NO_PLOTS;                    
        }        

        this.requestApproveRefresh();

        if (checkMyPortfolio) {
          this.checkMyPortfolio();
        }

        // Apply owner stored preference for dark_mode theme
        this.appComponentInstance.darkModeChange(this.ownerAccount.dark_mode);

      }, error => console.error(error));


    return;
  }


  async getTronAccounts(httpClient: HttpClient, baseUrl: string) {

    let attempts: number = 0;
    let subTron: Subscription;

    //const tronWebProvider = await TronWebProvider;
    let requestAccountsResponse: RequestAccountsResponse;


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
            this.checkTronAccountKey(httpClient, baseUrl, false);    // Show login request/approval bar

          }
          else if (tronWeb && ownerPublicKey != null && ownerPublicKey.base58 != false) {

            subTron.unsubscribe();

            let x = tronWeb.isConnected();
            //x.then(() => { console.log("connection response : " + x) });
            //let x2 = tronWeb.isTronLink;      // true/false - will also Force an dApp approve connection.

            this.checkTronAccountKey(httpClient, baseUrl, false);

            //requestAccountsResponse = await tronWebProvider.request({ method: 'tron_requestAccounts' });
            //requestAccountsResponse = await tronWeb.request({ method: 'tron_requestAccounts' });
            //if (requestAccountsResponse) {
            //    console.log("requestAccountsResponse : " + requestAccountsResponse);}
            //}
          }
        }
      );
  }

  checkTronAccountKey(httpClient: HttpClient, baseUrl: string, checkMyPortfolio: boolean) {

    const tronWeb = (window as any).tronWeb;
    let ownerPublicKey: any = tronWeb == null ? null : tronWeb.defaultAddress;

    if (ownerPublicKey != null && ownerPublicKey.base58 != false) {

      this.CheckUserAccountKey(ownerPublicKey.base58, httpClient, baseUrl, checkMyPortfolio);
     
    }
    else {

      this.appComponentInstance.darkModeChange(false);

      this.requestApprove = true;
      this.approvalType = APPROVAL_TYPE.NO_WALLET_ENABLED;
      this.requestApproveRefresh();
      if (checkMyPortfolio) {
        this.checkMyPortfolio();
      }
    }
    
  }

  // Triggered by (a) Change World Type (b) On initial load of page or redirect load
  async getEthereumAccounts(httpClient: HttpClient, baseUrl: string, checkMyPortfolio: boolean) {

    const provider = await DetectEthereumProvider();
    const ethereum = (window as any).ethereum;

    if (provider && provider.isMetaMask) {

      if (provider !== ethereum) {
        console.log('Do you have multiple wallets installed?');
        return;
      }

      const chainId = await ethereum.request({ method: 'eth_chainId' });
      const accounts = await ethereum.request({ method: 'eth_accounts' });

      if (accounts && accounts.length) {

        console.log(">>>Ethereum Account linked<<<");
        console.log("Key = ", ethereum.selectedAddress);
        this.requestApprove = false;        

        this.CheckUserAccountKey(ethereum.selectedAddress, httpClient, baseUrl, checkMyPortfolio);
      }
      else {
        console.log(">>>No Ethereum Account linked<<<");
        console.log("ChainId = ", chainId);

        this.appComponentInstance.darkModeChange(false);

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

    const provider = await DetectEthereumProvider();
    const ethereum = (window as any).ethereum;

    const accountsApproved = await ethereum.request({ method: 'eth_requestAccounts' });
    const account = accountsApproved[0];

    const chainId = await ethereum.request({ method: 'eth_chainId' });

    if (account && account !== "") {
      console.log(">>>Ethereum Account linked<<<");
      console.log("Key = ", ethereum.selectedAddress);

      this.ownerAccount.public_key = ethereum.selectedAddress;
      this.requestApprove = false;      
    }
    else {
      console.log(">>>No Ethereum Account linked<<<");
      console.log("ChainId = ", chainId);
      this.requestApprove = true;
    }

    return account;
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
}

export {
  OwnerAccount,
  WORLD,
  APPROVAL_TYPE
}
