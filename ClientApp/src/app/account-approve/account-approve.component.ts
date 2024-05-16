import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, ChangeDetectorRef , Inject } from '@angular/core';
import { Router } from '@angular/router';
import { Application, WORLD, APPROVAL_TYPE } from '../common/global-var';


@Component({
  selector: 'app-account-approve',
  templateUrl: './account-approve.component.html',
  styleUrls: ['./account-approve.component.css']
})
export class AccountApproveComponent {

  httpClient: HttpClient;
  baseUrl: string;
  private ethPublicKey: string = "";
  showFlag: boolean = true;
  showTronLinkLoginFlag: boolean = false;
  showTronLinkNoPlotsFlag: boolean = false;
  showEthApproveFlag: boolean = false;
  showEthConnectFlag: boolean = false;

  constructor(private cdf: ChangeDetectorRef, public globals: Application, public router: Router, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

    this.httpClient = http;
    this.baseUrl = baseUrl;

    this.globals.approveSwitchComponent = this;
    this.showFlag = this.globals.requestApprove;
  }

  async clickAccountLink() {

    if (this.globals.selectedWorld == WORLD.TRON) {
      this.globals.approveTronAccountLink(this.httpClient, this.baseUrl)
    }
    else if (this.globals.selectedWorld == WORLD.BNB || this.globals.selectedWorld == WORLD.ETH) {
      this.globals.approveEthereumAccountLink(this.httpClient, this.baseUrl)
    }

    return;
  }

  hide() {
    this.showFlag = false;
    this.showTronLinkLoginFlag = false;
    this.showEthApproveFlag = false;
    this.showTronLinkNoPlotsFlag = false;
    this.showEthConnectFlag = false;

    this.cdf.detectChanges();   // hide can be triggered out of render cycle - eg when metamask wallet log in
    return;
  }

  show() {
    this.showFlag = true;

    this.update();

    this.cdf.detectChanges();

    return;
  }

  update() {

    if (this.globals.selectedWorld == WORLD.TRON) {

      this.showEthConnectFlag = false;
      this.showEthApproveFlag = false;

      if (this.globals.approvalType == APPROVAL_TYPE.NO_WALLET_ENABLED) {
        this.showTronLinkNoPlotsFlag = false;
        this.showTronLinkLoginFlag = true;        
      }
      else if (this.globals.approvalType == APPROVAL_TYPE.ACCOUNT_WITH_NO_PLOTS) {
        this.showTronLinkLoginFlag = false;
        this.showTronLinkNoPlotsFlag = true;
      }
      else {  //Default
        this.showTronLinkLoginFlag = true;
        this.showTronLinkNoPlotsFlag = false;
      }

    }
    else if (this.globals.selectedWorld == WORLD.BNB || this.globals.selectedWorld == WORLD.ETH) {
      if (this.globals.requestApprove) {
        this.showEthConnectFlag = false;
        this.showEthApproveFlag = true;
      }
      else if (this.globals.connectWallet){
        this.showEthConnectFlag = true;
        this.showEthApproveFlag = false;
      }
      this.showTronLinkLoginFlag = false;
    }

  }

}
