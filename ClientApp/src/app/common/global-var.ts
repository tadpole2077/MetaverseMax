import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, ElementRef, ChangeDetectorRef } from '@angular/core';
import { AccountApproveComponent } from '../account-approve/account-approve.component';
import detectEthereumProvider from '@metamask/detect-provider';

interface OwnerAccount {  
  matic_key: string;
  checked_matic_key: string;
  public_key: string;
  name: string;
  checked: boolean;
  pro_tools_enabled: boolean;
}

const WORLD = {
  UNKNOWN: 0,
  TRON: 1,
  BNB: 2,
  ETH: 3
}

@Injectable()
export class Globals {

  public ownerAccount: OwnerAccount;
  public windowTron: any;
  public selectedWorld: number = WORLD.UNKNOWN;   // Default Tron
  public worldCode: string = "trx";  // Default Tron
  public worldURLPath: string = "https://mcp3d.com/tron/api/image/";   //Default Tron
  public firstCitizen: number = 0;   // Default Tron
  public worldName: string = "Tron";
  public approveSwitchComponent: AccountApproveComponent;


  // Flag triggers an update on any module that uses the Account Approval component
  public _metamaskRequestApprove: boolean = false;
  set metamaskRequestApprove(value) {    

    let changed = this._metamaskRequestApprove != value;
    this._metamaskRequestApprove = value;

    // Trigger any Approve component to update
    if (changed && this.approveSwitchComponent) {
      if (value) {
        this.approveSwitchComponent.show();
      }
      else {
        this.approveSwitchComponent.hide();
      }
    }
    console.log("RequestApprove:", this._metamaskRequestApprove);
  }
  get metamaskRequestApprove() {
    return this._metamaskRequestApprove;
  }

  constructor() {
    
    this.initAccount();
    
  }

  initAccount() {

    this.ownerAccount = {
      public_key: "",
      matic_key: "Not Found",
      checked_matic_key: "",
      name: "",
      checked: false,
      pro_tools_enabled: false
    };

  }

  CheckUserAccountKey(OwnerPublicKey: string, httpClient: HttpClient, baseUrl: string, requesterComponent: ChangeDetectorRef) {

    let params = new HttpParams();
    params = params.append('owner_public_key', OwnerPublicKey);

    httpClient.get<OwnerAccount>(baseUrl + '/OwnerData/CheckHasPortfolio', { params: params })
      .subscribe((result: OwnerAccount) => {

        this.ownerAccount = result;
        this.ownerAccount.checked = true;
        requesterComponent.detectChanges();

      }, error => console.error(error));


    return;
  }

  async getEthereumAccounts(httpClient: HttpClient, baseUrl: string, requesterCDF: ChangeDetectorRef) {

    const provider = await detectEthereumProvider();
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
        //this.ethPublicKey = ethereum.selectedAddress;
        this.metamaskRequestApprove = false;

        this.CheckUserAccountKey(ethereum.selectedAddress, httpClient, baseUrl, requesterCDF);
      }
      else {
        console.log(">>>No Ethereum Account linked<<<");
        console.log("ChainId = ", chainId);
        this.metamaskRequestApprove = true;
      }

    }

    return;
  }


  async approveEthereumAccountLink(httpClient: HttpClient, baseUrl: string, requesterCDF: ChangeDetectorRef) {

    const provider = await detectEthereumProvider();
    const ethereum = (window as any).ethereum;

    const accountsApproved = await ethereum.request({ method: 'eth_requestAccounts' });
    const account = accountsApproved[0];

    const chainId = await ethereum.request({ method: 'eth_chainId' });

    if (account && account !== "") {
      console.log(">>>Ethereum Account linked<<<");
      console.log("Key = ", ethereum.selectedAddress);

      this.ownerAccount.public_key = ethereum.selectedAddress;
      this.metamaskRequestApprove = false;
    }
    else {
      console.log(">>>No Ethereum Account linked<<<");
      console.log("ChainId = ", chainId);
      this.metamaskRequestApprove = true;
    }

    return account;
  }
}

export {
  OwnerAccount,
  WORLD
}
