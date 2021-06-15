import { Component, Inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { Observable } from 'rxjs/Rx';
import { OwnerAccount, Globals } from '../common/global-var';


@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent {

  private httpClient: HttpClient;
  private baseUrl: string;
  private tronWeb: any;
  private tronPublicKey: string = "";
  //public ownerAccount: OwnerAccount;
  private subTron: Subscription;


  constructor(public globals: Globals, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    
    this.httpClient = http;
    this.baseUrl = baseUrl;
  }
  /*
  ngAfterViewInit() {

    
    this.tronWeb = window.tronWeb;

    // Delay check on Tron Widget load and init, must be a better way of hooking into it.
    this.subTron = Observable.interval(2000)
      .subscribe(        
        (val) => {
          if (this.tronWeb) {
            this.tronPublicKey = this.tronWeb.defaultAddress.base58;
            this.CheckUserTronKey(this.tronPublicKey);
            this.subTron.unsubscribe();
          }
        }
      );   
  }

  CheckUserTronKey(tronPublicKey: string) {

    let params = new HttpParams();
    params = params.append('owner_tron_public', tronPublicKey);

    this.httpClient.get<OwnerAccount>(this.baseUrl + 'api/OwnerData/CheckHasPortfolio', { params: params })
      .subscribe((result: OwnerAccount) => {

        this.ownerAccount = result;

      }, error => console.error(error));


    return;
  }
  */
  copyMessage(val: string){
      const selBox = document.createElement('textarea');
      selBox.style.position = 'fixed';
      selBox.style.left = '0';
      selBox.style.top = '0';
      selBox.style.opacity = '0';
      selBox.value = val;
      document.body.appendChild(selBox);
      selBox.focus();
      selBox.select();
      document.execCommand('copy');
      document.body.removeChild(selBox);
  }


}


//const TronWeb = require('tronweb');
/*
declare global {
  interface Window {
    tronWeb: any;
  }
}

window.onload = function () {
  if (!window.tronWeb) {
    const HttpProvider = TronWeb.providers.HttpProvider;
    const fullNode = new HttpProvider('https://api.trongrid.io');
    const solidityNode = new HttpProvider('https://api.trongrid.io');
    const eventServer = 'https://api.trongrid.io/';

    const tronWeb = new TronWeb(
      fullNode,
      solidityNode,
      eventServer,
    );

    window.tronWeb = tronWeb;
  }
};
*/
// Load twitter sharing button
(function (d, s, id) {
  var js, fjs = d.getElementsByTagName(s)[0], p = /^http:/.test(d.location.toString()) ? 'http' : 'https';
  if (!d.getElementById(id)) {
    js = d.createElement(s); js.id = id; js.src = p + '://platform.twitter.com/widgets.js';
    fjs.parentNode.insertBefore(js, fjs);
  }
}(document, 'script', 'twitter-wjs'));
