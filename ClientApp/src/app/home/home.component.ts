import { ChangeDetectorRef, Component, Inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Subscription, Observable } from 'rxjs';
import { OwnerAccount, Globals, WORLD } from '../common/global-var';


@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent {

  private httpClient: HttpClient;
  private baseUrl: string;

  public worldName: string;
  public worldCode: string;


  constructor(private cdf: ChangeDetectorRef, public globals: Globals, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    
    this.httpClient = http;
    this.worldCode = (globals.selectedWorld == WORLD.TRON ? "trx" : globals.selectedWorld == WORLD.BNB ? "bnb" : "eth")
    this.worldName = (globals.selectedWorld == WORLD.TRON ? "Tron" : globals.selectedWorld == WORLD.BNB ? "BSC" : "Ethereum");
    this.baseUrl = baseUrl + "api/" + this.worldCode;

    globals.homeCDF = cdf;
  }
  
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

// Load twitter sharing button
(function (d, s, id) {
  var js, fjs = d.getElementsByTagName(s)[0], p = /^http:/.test(d.location.toString()) ? 'http' : 'https';
  if (!d.getElementById(id)) {
    js = d.createElement(s); js.id = id; js.src = p + '://platform.twitter.com/widgets.js';
    fjs.parentNode.insertBefore(js, fjs);
  }
}(document, 'script', 'twitter-wjs'));
