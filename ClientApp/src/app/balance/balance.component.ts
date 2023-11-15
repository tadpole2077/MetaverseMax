import { Component, NgZone } from "@angular/core";
import DetectEthereumProvider from '@metamask/detect-provider';
import { Globals } from "../common/global-var";
import { HEX_NETWORK, METAMASK_ERROR_CODE } from "../common/enum";
import Web3 from 'web3';
import { Web3Utils } from '../common/web3Utils';
import { MMBankAbi } from "../Contract/contractMMBankAbi";
import { Subscription } from "rxjs";
import { HttpClient, HttpParams } from "@angular/common/http";


@Component({
  selector: 'app-balance',
  templateUrl: './balance.component.html',
  styleUrls: ['./balance.component.css']
})
export class BalanceComponent{

  httpClient: HttpClient;
  ethereum: any;
  provider: any;
  web3: Web3 = null;
  utils: Web3Utils = new Web3Utils();
  balance: number = 0;
  subscriptionAccountActive$: Subscription;
  readonly CONTRACT_MMBank = "0x6C92919Fb79d5f70cdc887653E4169f33fF63146";
  readonly MEGA_DECIMALS = 1e18;
  readonly MEGA_DECIMALS_COUNT = 18;

  constructor(public globals: Globals, private zone: NgZone) {

    this.ethereum = (window as any).ethereum;    
  }
  
  ngOnInit() {
    this.startWebProcess();
  } 

  async startWebProcess() {

    let chainIdHex: string;

    // Init Web3 component with current wallet provider
    //await this.initWeb3();
    //this.balanceManager(true);
    this.balance = this.globals.ownerAccount.balance;


    // Monitor using service - when account status changes - active / inactive.
    this.subscriptionAccountActive$ = this.globals.accountActive$.subscribe(active => {

      console.log("account status : " + active);
      this.zone.run(() => {
        //this.balanceManager(active);
        this.balance = this.globals.ownerAccount.balance;
      });

    });

  }

  balanceManager(active: boolean) {

    this.checkNetwork(HEX_NETWORK.BINANCE_TESTNET_ID, "Binance Testnet")
      .then((connectCheck) => {

          if (connectCheck) {

            this.getCurrentWalletBalanceInBank()
              .then((value) => {
                //this.balance = value;
              });
          }
      });
  }

  async getCurrentWalletBalanceInBank() {

    const addressFrom = this.ethereum.selectedAddress;

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank);

    // Get the current value of my number    
    const balance = await testContract.methods.balances(addressFrom).call();

    
    return this.convertFromEVMtoCoinLocale(balance);
  }

  async checkNetwork(selectedNetwork: HEX_NETWORK, networkDesc: string) {

    let chainIdHex: string;
    let connected: boolean = false;

    if (this.ethereum && this.ethereum.isConnected()) {

      let addressFrom = this.ethereum.selectedAddress;

      chainIdHex = await this.ethereum.request({ method: "eth_chainId", params: [] })
      const chainIdNumber = parseInt(chainIdHex, 16); // convert to decimal

      console.log("Current Chain : " + chainIdHex + " - " + chainIdNumber);

      if (chainIdHex != selectedNetwork) {
        if (chainIdHex == HEX_NETWORK.ETHEREUM) {
          console.log("Selected chain is Ethereum main-net, Request to switch to " + networkDesc + ".");
        }

        // await this.switchNetwork(selectedNetwork);
      }
      else {
        connected = true;
      }
    }

    return connected;
  }

  async initWeb3() {        

    try {
      this.provider = await DetectEthereumProvider();
      
      if (this.provider && this.provider.isMetaMask) {
        this.web3 = new Web3(this.provider);
        //this.web3.eth.Contract.setProvider(this.provider)
      }
      //this.web3 = new Web3(this.ethereum);
    }
    catch (error) {
      console.log("provider error: " + error);
      this.web3 = null;
    }
  }

  // Calculate actual tokens amounts based on decimals in token
  // Convert to string - ensure no localised chars .  Some issue with BigNumber adding additoinal values - may be due to using es2015
  convertToCoinNumber(amount: number): string{
    
    // Check if number is fractional then remove fractional amount.
    let stringAmount = '0';
    let decimalLength = 0;
    let megaDecimalCount = this.MEGA_DECIMALS_COUNT;
    let bigAmount;

    if ((amount % 1 != 0)) {
      decimalLength = amount.toString().split(".")[1].length;
      amount = amount * (10 ** decimalLength);      // using Exponential ** operator
      bigAmount = BigInt(amount * (10 ** (this.MEGA_DECIMALS_COUNT - decimalLength)));
    }
    else {
      bigAmount = BigInt(amount) * BigInt(this.MEGA_DECIMALS);
    }

    return bigAmount.toString();
  }

  convertFromEVMtoCoinLocale(amount: any) {
    let amountString: string = amount.toString();
    let amountDecimal: string = '';
    let amountInteger: string = ''; 

    if (amount != 0) {
      if (amountString.length >= this.MEGA_DECIMALS_COUNT) {
        amountDecimal = amountString.substring(amountString.length - this.MEGA_DECIMALS_COUNT);
        amountInteger = amountString.substring(0, amountString.length - this.MEGA_DECIMALS_COUNT);                
      }
      else {
        amountDecimal = amountString.padStart(this.MEGA_DECIMALS_COUNT - amountString.length, '0');
      }
    }

    //remove trailing zeros from decimal string
    for (let counter = amountDecimal.length; counter--; counter >= 0) {
      if (amountDecimal.substring(amountDecimal.length - 1) == "0") {
        amountDecimal = amountDecimal.slice(0, -1); // Remove the last character
      }
      else {
        break;  // found a none zero char as end char.
      }
    }

    // Combine
    if (amountInteger != '' && amountDecimal != '') {
      amountString = amountInteger + '.' + amountDecimal;
    }
    else if (amountInteger == '' && amountDecimal != '') {
      amountString = '.' + amountDecimal;
    }
    else if (amountInteger != '' && amountDecimal == '') {
      amountString = amountInteger;
    }

    return amountString;
  }
}
