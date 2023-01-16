import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, ChangeDetectorRef , Inject } from '@angular/core';
import { Router } from '@angular/router';
import { Globals, WORLD } from '../common/global-var';


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

  constructor(private cdf: ChangeDetectorRef, public globals: Globals, public router: Router, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

    this.httpClient = http;
    this.baseUrl = baseUrl;

    this.globals.approveSwitchComponent = this;
    this.showFlag = this.globals.metamaskRequestApprove;
  }

  async approveEthereumAccountLink() {

    this.globals.approveEthereumAccountLink(this.httpClient, this.baseUrl, this.cdf)
    
    return;
  }

  hide() {
    this.showFlag = false;
    return;
  }

  show() {
    this.showFlag = true;
    this.cdf.detectChanges()
    return;
  }
}
