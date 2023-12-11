import { Component, NgZone } from "@angular/core";
import { MatDialog } from "@angular/material/dialog";
import { Subscription } from "rxjs";
import { HttpClient, HttpParams } from "@angular/common/http";
import { BalanceManageDialogComponent } from "../balance-manage-dialog/balance-manage-dialog.component";
import { DirectDepositDialogComponent } from "../direct-deposit-dialog/direct-deposit-dialog.component";
import DetectEthereumProvider from '@metamask/detect-provider';
import Web3 from 'web3';
import { Web3Utils } from '../common/web3Utils';

import { MMBankAbi } from "../Contract/contractMMBankAbi";
import { MCPMegaAbi } from "../Contract/contractMCPMegaAbi";
import { HEX_NETWORK, METAMASK_ERROR_CODE } from "../common/enum";
import { Globals } from "../common/global-var";

@Component({
  selector: 'app-balance',
  templateUrl: './balance.component.html',
  styleUrls: ['./balance.component.css']
})
export class BalanceComponent{

  web3Active: boolean = false;
  httpClient: HttpClient;
  ethereum: any;
  provider: any;
  web3: Web3 = null;
  utils: Web3Utils = new Web3Utils();
  balance: number = 0;
  accountMCPMegaBalance: number = 0;
  subscriptionAccountActive$: Subscription;
  subscriptionBalanceChange$: Subscription;
  currentPlayerWalletKey: string;
 
  readonly CONTRACT_MCPMEGA = "0x0af8c016620d3ed0c56381060e8ab2917775885e";
  readonly CONTRACT_MMBank = "0x6C92919Fb79d5f70cdc887653E4169f33fF63146";
  readonly MEGA_DECIMALS = 1e18;
  readonly MEGA_DECIMALS_COUNT = 18;

  constructor(public globals: Globals, private zone: NgZone, public dialog: MatDialog) {

    this.ethereum = (window as any).ethereum;    
  }
  
  ngOnInit() {

    this.startBalanceMonitor();

  }

  ngOnDestroy() {
    if (this.subscriptionAccountActive$) {
      this.subscriptionAccountActive$.unsubscribe();
    }
    if (this.subscriptionBalanceChange$) {
      this.subscriptionBalanceChange$.unsubscribe();
    }
  }

  web3Manager() {

    this.initWeb3().then((active) => {

      if (active) {
        this.currentPlayerWalletKey = this.globals.ownerAccount.public_key;

        this.checkBalances().then(() => {

          console.log("account status : " + active);
          this.zone.run(() => {
            this.balance = this.globals.ownerAccount.balance;
          });
        });
      }
    });
  }

  async initWeb3() {        

    let active: boolean = false;

    try {
      this.provider = await DetectEthereumProvider();
      this.ethereum = (window as any).ethereum;
      
      if (this.provider && this.provider.isMetaMask) {
        this.web3 = new Web3(this.provider);
        active = true;
        this.web3Active = true
      }
    }
    catch (error) {
      console.log("provider error: " + error);
      this.web3 = null;
    }

    return active;
  }

  startBalanceMonitor() {

    this.currentPlayerWalletKey = this.globals.ownerAccount.public_key;
    this.balance = this.globals.ownerAccount.balance;

    this.web3Manager();

    // Monitor using service - when account status changes - active / inactive.
    this.subscriptionAccountActive$ = this.globals.accountActive$.subscribe(active => {
      
      this.currentPlayerWalletKey = this.globals.ownerAccount.public_key;
      this.checkBalances().then(() => {

        console.log("account status : " + active);
        this.zone.run(() => {
          this.balance = this.globals.ownerAccount.balance;
        });
      });

    });

    this.subscriptionBalanceChange$ = this.globals.balaceChange$.subscribe(balanceChange => {
      if (balanceChange) {
        console.log("account balance updated");
        this.zone.run(() => {
          this.balance = this.globals.ownerAccount.balance;
        });
      }
    });
  }

  openDialog(enterAnimationDuration: string, exitAnimationDuration: string): void {
    //this.dialog.open(BalanceManageDialogComponent, {
    this.dialog.open(DirectDepositDialogComponent, {
      width: '600px',
      enterAnimationDuration,
      exitAnimationDuration,
    });
  }


  async checkBalances() {

    if (this.currentPlayerWalletKey != '') {
      this.checkBNBSmartChain().then((bnbSmartChangeActive) => {
        // Network Active check.
        if (bnbSmartChangeActive) {
          this.getMCPMegaBalance().then((megaBalance) => {

            this.zone.run(() => {
              this.accountMCPMegaBalance = megaBalance;
            });
          });
        }
      });
    }
  }

  // Read View - Get MCPMega Balance for current account.
  async getMCPMegaBalance() {

    // Create a new contract object using the ABI and bytecode
    const contractMCPMega = new this.web3.eth.Contract(
      MCPMegaAbi,
      this.CONTRACT_MCPMEGA);

    // Get the current value of my number
    const balanceReturned = await contractMCPMega.methods.balanceOf(this.currentPlayerWalletKey).call();

    return this.convertFromEVMtoCoinLocale(balanceReturned, 0);
  }

  async checkBNBSmartChain() {

    let chainIdHex: string;
    let bnbSmartChain: boolean = false;

    let connected = await this.checkNetwork(HEX_NETWORK.BINANCE_ID, "Binance Mainnet");

    chainIdHex = await this.ethereum.request({ method: "eth_chainId", params: [] })

    if (connected && chainIdHex == HEX_NETWORK.BINANCE_ID && this.provider) {
      bnbSmartChain = true;
    }

    return bnbSmartChain;
  }

  async checkNetwork(selectedNetwork: HEX_NETWORK, networkDesc: string) {

    let chainIdHex: string;
    let connected: boolean = false;

    if (this.ethereum && this.ethereum.isConnected()) {

      let addressFrom = this.currentPlayerWalletKey;

      chainIdHex = await this.ethereum.request({ method: "eth_chainId", params: [] })
      const chainIdNumber = parseInt(chainIdHex, 16); // convert to decimal

      console.log("Current Chain : " + chainIdHex + " - " + chainIdNumber);

      if (chainIdHex != selectedNetwork) {

        if (chainIdHex == HEX_NETWORK.ETHEREUM) {
          console.log("Selected chain is Ethereum main-net, Request to switch to " + networkDesc + ".");
        }

        await this.switchNetwork(selectedNetwork);        
      }

      connected = true;
    }

    return connected;
  }

  async switchNetwork(chainIdHex: string) {

    const ethereum = (window as any).ethereum;
    const provider = await DetectEthereumProvider();
    let networkSwitched = false;

    if (ethereum && ethereum.isConnected()) {

      try {
        // Returns Null if switch is successful or error if not
        await ethereum.request({
          method: 'wallet_switchEthereumChain',
          params: [{ chainId: chainIdHex }],
        });

        networkSwitched = true;
      }
      catch (switchError) {

        // ErrorCode 4902 indicates that chain is not added to wallet
        try {
          if (switchError.code === METAMASK_ERROR_CODE.UNRECOGNISED_CHAIN) {

            if (chainIdHex == HEX_NETWORK.POLYGON_ID) {
              // Add Polygon chain
              await ethereum.request({
                method: 'wallet_addEthereumChain',
                params: [
                  {
                    chainId: HEX_NETWORK.POLYGON_ID,
                    blockExplorerUrls: ['https://polygonscan.com/'],
                    chainName: 'Polygon Mainnet',
                    nativeCurrency: {
                      decimals: 18,
                      name: 'Polygon',
                      symbol: 'MATIC'
                    },
                    rpcUrls: ['https://polygon-rpc.com']
                  },
                ],
              });
            }
            else if (chainIdHex == HEX_NETWORK.BINANCE_TESTNET_ID) {
              // Add BNBTest chain
              await ethereum.request({
                method: 'wallet_addEthereumChain',
                params: [
                  {
                    chainId: HEX_NETWORK.BINANCE_TESTNET_ID,
                    blockExplorerUrls: ['https://testnet.bscscan.com'],
                    chainName: 'Smart Chain - Testnet',
                    nativeCurrency: {
                      decimals: 18,
                      name: 'BNB',
                      symbol: 'tBNB'
                    },
                    rpcUrls: ['https://data-seed-prebsc-1-s1.binance.org:8545/']
                  },
                ],
              });

            }
            else if (chainIdHex == HEX_NETWORK.BINANCE_ID) {
              // Add BNB Smart chain
              await ethereum.request({
                method: 'wallet_addEthereumChain',
                params: [
                  {
                    chainId: HEX_NETWORK.BINANCE_ID,
                    blockExplorerUrls: ['https://bscscan.com'],
                    chainName: 'BNB Smart Chain',
                    nativeCurrency: {
                      decimals: 18,
                      name: 'BNB',
                      symbol: 'BNB'
                    },
                    rpcUrls: ['https://bsc-dataseed.binance.org/']
                  },
                ],
              });

            }
          }

        }
        catch (error) {
          // user rejects the request to "add chain" or param values are wrong, maybe you didn't use hex above for `chainId`?
          console.log("wallet_addEthereumChain Error: ${error.message}");
        }
      }

     
    }
    return networkSwitched;
  }

  convertFromEVMtoCoinLocale(amount: any, decimalPlaces: number) {

    let amountString = amount.toString();
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
    amountDecimal = amountDecimal.substring(0, decimalPlaces);

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
    else {
      amountString = 0;
    }

    return amountString;
  }















  async startWebProcess() {

    //let chainIdHex: string;

    // Init Web3 component with current wallet provider
    //await this.initWeb3();
    //this.balanceManager(true);
  }

  // If using BNB Smart Contract to hold account Balance
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

    const addressFrom = this.globals.ownerAccount.public_key;

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank);

    // Get the current value of my number    
    const balance = await testContract.methods.balances(addressFrom).call();

    
    return this.convertFromEVMtoCoinLocale(balance,4);
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

  
}
