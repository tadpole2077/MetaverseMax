import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, ChangeDetectorRef , Inject } from '@angular/core';
import { Router } from '@angular/router';
import { Globals, WORLD, APPROVAL_TYPE } from '../common/global-var';


@Component({
  selector: 'app-account-approve',
  templateUrl: './account-approve.component.html',
  styleUrls: ['./account-approve.component.css']
})
export class AccountApproveComponent {

  httpClient: HttpClient;
  baseUrl: string;
  private ethPublicKey: string = "";
  public showFlag: boolean = true;
  public showTronLinkLoginFlag: boolean = false;
  public showTronLinkNoPlotsFlag: boolean = false;
  public showEthApproveFlag: boolean = false;

  constructor(private cdf: ChangeDetectorRef, public globals: Globals, public router: Router, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

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

      if (this.globals.approvalType == APPROVAL_TYPE.NO_WALLET_ENABLED) {
        this.showTronLinkNoPlotsFlag = false;
        this.showTronLinkLoginFlag = true;
        this.showEthApproveFlag = false;
      }
      else if (this.globals.approvalType == APPROVAL_TYPE.ACCOUNT_WITH_NO_PLOTS) {
        this.showTronLinkLoginFlag = false;
        this.showTronLinkNoPlotsFlag = true;
        this.showEthApproveFlag = false;
      }
      else {  //Default
        this.showTronLinkLoginFlag = true;
        this.showTronLinkNoPlotsFlag = false;
        this.showEthApproveFlag = false;
      }

    }
    else if (this.globals.selectedWorld == WORLD.BNB || this.globals.selectedWorld == WORLD.ETH) {
      this.showEthApproveFlag = true;
      this.showTronLinkLoginFlag = false;
    }

  }

}
