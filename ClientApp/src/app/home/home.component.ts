import { Component } from '@angular/core';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent {

  constructor() {
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

/*
const TronWeb = require('tronweb');

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
