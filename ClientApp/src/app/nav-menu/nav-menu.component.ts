import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, Inject } from '@angular/core';
import { Subscription } from 'rxjs';
import { Observable } from 'rxjs/Rx';
import { OwnerAccount, Globals } from '../common/global-var';


@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})
export class NavMenuComponent {

  private httpClient: HttpClient;
  private baseUrl: string;

  private tronPublicKey: string = "";  
  private subTron: Subscription;
  private attempts: number = 0;
 
  isExpanded = false;

  constructor(public globals:Globals, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    
    this.httpClient = http;
    this.baseUrl = baseUrl;

  }

  ngAfterViewInit() {
   
    // Delay check on Tron Widget load and init, must be a better way of hooking into it.
    this.subTron = Observable.interval(1000)
      .subscribe(
        (val) => {

          this.attempts++;
          this.globals.windowTron = window;
          let tronWeb = this.globals.windowTron.tronWeb;

          if (tronWeb) {

            this.tronPublicKey = tronWeb.defaultAddress.base58;
            this.CheckUserTronKey(this.tronPublicKey);
            this.subTron.unsubscribe();            

          }
          else if(this.attempts >= 5){

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

        this.globals.ownerAccount = result;
        this.globals.ownerAccount.checked = true;

      }, error => console.error(error));


    return;
  }

  collapse() {
    this.isExpanded = false;
  }

  toggle() {
    this.isExpanded = !this.isExpanded;
  }
}
