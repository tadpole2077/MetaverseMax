import {Component, Inject, NgZone, ViewChild} from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { AbstractControl, FormControl, Validators } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom, Subscription } from 'rxjs';
import DetectEthereumProvider from '@metamask/detect-provider';
import Web3 from 'web3';
import { Web3Utils } from '../common/web3Utils';

import { MMBankAbi } from "../Contract/contractMMBankAbi";
import { MCPMegaAbi } from "../Contract/contractMCPMegaAbi";
import { MegaCoinMOCKAbi } from "../Contract/contractMegaCoinAbi";
import { Globals } from '../common/global-var';
import { HEX_NETWORK, METAMASK_ERROR_CODE } from "../common/enum";
import { maxBalanceValidator } from '../validator/max-balance.validator'
import { MatProgressBar } from '@angular/material/progress-bar';
import { OwnerDataComponent } from '../owner-data/owner-data.component';

@Component({
  selector: 'app-direct-deposit-dialog',
  styleUrls: ['./direct-deposit-dialog.component.css'],
  templateUrl: './direct-deposit-dialog.component.html',
})
export class DirectDepositDialogComponent {
  readonly MEGA_DECIMALS = 1e18;
  readonly MEGA_DECIMALS_COUNT = 18;
  readonly CONTRACT_MCPMEGATOKEN = "0x0af8c016620d3ed0c56381060e8ab2917775885e";
  readonly CONTRACT_MCPMEGA_DEPOSIT = "0xD96c0110E6cE40787602ec9038d66E7277E5c61d";
  readonly CONTRACT_MCPMEGA_MANAGER = "0x29E4590b970ca60de81BC8968759DbC9E98dB031";  

  //readonly CONTRACT_MMBank = "0x9Adf2de8c24c25B3EB1fc542598b69C51eE558A7";
  //readonly CONTRACT_MEGA_MOCK = "0x4Dd0308aE43e56439D026E3f002423E9A982aeaF";
  

  httpClient: HttpClient;
  baseUrl: string;
  provider: any;
  web3: Web3 = null;
  utils: Web3Utils = new Web3Utils();
  ethereum: any;
  accountMCPMegaBalance: number = 0;
  accountMCPMegaAllowance: number = 0;
  currentPlayerWalletKey: string = "";
  subscriptionAccountActive$: Subscription;
  subscriptionBalanceChange$: Subscription;
  amountDepositControl = new FormControl('0');

  depositRotateActive: boolean = false;
  networkChange: boolean = false;
  networkMsg: string;
  networkWarning: boolean = false;

  transactionStarted: boolean = false;
  processActive: boolean = true;
  progressMsg: string;
  accountActive: boolean = false;

  depositFocus: boolean = true;
  withdrawFocus: boolean = false;
  insufficientMsg: string = "Insufficient Allowance";

  @ViewChild(MatProgressBar, { static: true } as any) progressBar: MatProgressBar;

  constructor(public dialog: MatDialog, public globals: Globals, private zone: NgZone, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string) {
    this.httpClient = http;
    this.baseUrl = rootBaseUrl + "api/" + globals.worldCode;

  }

  ngOnInit() {

    this.startBalanceMonitor();

    this.initWeb3().then((active) => {

      if (active && this.currentPlayerWalletKey !== "") {

        this.checkBNBSmartChain().then((bnbSmartChangeActive) => {

          if (bnbSmartChangeActive) {
            this.checkBalances(true);                        
          }

        });
      };
    });

  }

  ngOnDestroy() {
    if (this.subscriptionAccountActive$) {
      this.subscriptionAccountActive$.unsubscribe();
    }
    if (this.subscriptionBalanceChange$) {
      this.subscriptionBalanceChange$.unsubscribe();
    }
  }

  async checkBalances(MCPMegaCheck) {

    if (MCPMegaCheck) {

      this.checkBNBSmartChain().then((bnbSmartChangeActive) => {
        // Network Active check.
        if (bnbSmartChangeActive) {
          this.getMCPMegaBalance().then((megaBalance) => {

            this.getMCPMegaAllowance().then((MCPMegaAllowance) => {

              this.accountActive = true;

              this.zone.run(() => {
                this.accountMCPMegaBalance = megaBalance;
                this.accountMCPMegaAllowance = MCPMegaAllowance
                let orgValue = this.amountDepositControl.value;

                // Update validation rules - max and min.
                this.amountDepositControl = new FormControl(orgValue == "0" ? "0.1" : orgValue, [
                  Validators.required,
                  (control: AbstractControl) => Validators.max(Number(MCPMegaAllowance) < Number(megaBalance) ? Number(MCPMegaAllowance) : Number(megaBalance))(control),
                  (control: AbstractControl) => Validators.min(0.0001)(control)
                ]);

                this.insufficientMsg = Number(MCPMegaAllowance) < Number(megaBalance) ? "Insufficient Allowance" : "Insufficient Mega Balance"; 
              });

            });

          });                     
        }

      });      
    }
  }

  startBalanceMonitor() {

    this.currentPlayerWalletKey = this.globals.ownerAccount.public_key;

    // Monitor using service - when account status changes - active / inactive.
    this.subscriptionAccountActive$ = this.globals.accountActive$.subscribe(active => {

      this.currentPlayerWalletKey = this.globals.ownerAccount.public_key;
      if (active) {        

        console.log("account status : " + active);
        this.checkBalances(true);
      }

    });

    this.subscriptionBalanceChange$ = this.globals.balaceChange$.subscribe(balanceChange => {
      if (balanceChange) {
        console.log("account balance updated");
        this.checkBalances(false);
      }
    });
  }

  // Read View - Get MCPMega Balance for current account.
  async getMCPMegaBalance() {

    // Create a new contract object using the ABI and bytecode
    const contractMCPMega = new this.web3.eth.Contract(
      MCPMegaAbi,
      this.CONTRACT_MCPMEGATOKEN);

    // Get the current value of my number
    const balanceReturned = await contractMCPMega.methods.balanceOf(this.currentPlayerWalletKey).call();

    return this.convertFromEVMtoCoinLocale(balanceReturned, 0);
  }

  // Read View - Get MCPMega Allowance for current account.
  // Allowance between wallet owner && MEGA_MANAGER contract
  async getMCPMegaAllowance() {

    let owner = this.currentPlayerWalletKey;
    let spender = this.CONTRACT_MCPMEGA_MANAGER;   // spender  MCP MEGA Manager contract - handles deposits into Mega world

    // Create a new contract object using the ABI and bytecode
    const contractMCPMega = new this.web3.eth.Contract(
      MCPMegaAbi,
      this.CONTRACT_MCPMEGATOKEN);

    // Get the current value of my number
    const allowanceReturned = await contractMCPMega.methods.allowance(owner, spender).call();

    return this.convertFromEVMtoCoinLocale(allowanceReturned, 2);
  }


  // ************************************************************
  // Deposit Function set
  async depositMegaToMCP() {

    const addressFrom = this.currentPlayerWalletKey;
    //const addressReceiver = this.CONTRACT_MCPMEGA_MANAGER;
    const megaValue = this.amountDepositControl.value;

    // Test code to use testnet for deposit/withdraw
    //let testnet:boolean = await this.checkBNBTestnet();

    //if (testnet == false) {
    //  return;
    //}
    let bnbNet: boolean = await this.checkBNBSmartChain();

    if (bnbNet == false) {
      return;
    }

    // Create a new contract object using the ABI and bytecode
    const MCPMegaContract = new this.web3.eth.Contract(
      MCPMegaAbi,
      this.CONTRACT_MCPMEGA_DEPOSIT
    );

    try {

      this.depositRotateActive = true;
      this.transactionStarted = true;
      this.setProgressBarMsg("Deposit Transaction Pending ..", true, 10);

      const gasPrice = await this.web3.eth.getGasPrice();

      await MCPMegaContract.methods.addMega(this.convertToCoinNumber(megaValue, 0))        
        .send({
          from: addressFrom,
          gasPrice: this.utils.toHex(gasPrice),
          gas: "100000"
        })
        .on('sent', (receipt) => {
          //console.log('receipt: ' + receipt);
          this.zone.run(() => {
            this.setProgressBarMsg('Deposit Transaction In-Progress...', true, 50);
          });
        })
        .then((result) => {
          console.log('Deposit Completed: ' + result);
          this.setProgressBarMsg("Deposit Completed", false, 100);
          this.checkBalances(true);          
          this.depositRotateActive = false;
          //this.confirmTransaction(result.transactionHash);

        })
        .catch((error) => {
          console.log(error);

          this.depositRotateActive = false;
          this.setProgressBarMsg("Deposit Denied", false, 60);
        });

    }
    catch (error) {
      console.error(error);
      this.depositRotateActive = false;
      this.setProgressBarMsg("Deposit Denied - Error", false, 60);
    }

    return;    
  }

  get checkInvalidDeposit(): boolean {
    return this.amountDepositControl.hasError('max') ||
      this.amountDepositControl.hasError('min') ||
      this.amountDepositControl.hasError('required') ||
      this.accountActive == false; 

  }
  // ************************************************************



  setProgressBarMsg(message:string, active: boolean, barValue: number) {
    this.progressMsg = message;
    this.processActive = active;
    this.progressBar.value = barValue;  
  }

  async initWeb3() {        

    let active: boolean = false;

    try {
      this.provider = await DetectEthereumProvider();
      this.ethereum = (window as any).ethereum;
      
      if (this.provider && this.provider.isMetaMask) {
        this.web3 = new Web3(this.provider);
        active = true;
      }
    }
    catch (error) {
      console.log("provider error: " + error);
      this.web3 = null;
    }

    return active;
  }

  async checkBNBSmartChain() {

    let chainIdHex: string;
    let bnbSmartChain: boolean = false;

    let connected = await this.checkNetwork(HEX_NETWORK.BINANCE_ID, "Binance Mainnet");

    chainIdHex = await this.ethereum.request({ method: "eth_chainId", params: [] })

    if (connected && chainIdHex == HEX_NETWORK.BINANCE_ID && this.provider) {
      bnbSmartChain = true;
    }
    else {
      this.networkMsg = "Network Not Changed, unable to proceed..";
      this.networkWarning = true;
    }

    return bnbSmartChain;
  }

  async checkBNBTestnet() {

    let chainIdHex: string;
    let bnbSmartChainTestNet: boolean = false;

    let connected = await this.checkNetwork(HEX_NETWORK.BINANCE_TESTNET_ID, "BNB Smart Chain - Testnet");

    chainIdHex = await this.ethereum.request({ method: "eth_chainId", params: [] })

    if (connected && chainIdHex == HEX_NETWORK.BINANCE_TESTNET_ID && this.provider) {
      bnbSmartChainTestNet = true;
    }
    else {
      this.networkMsg = "Network Not Changed, unable to proceed..";
      this.networkWarning = true;
    }

    return bnbSmartChainTestNet;
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

        this.networkChange = true;
        this.networkMsg = "Change Network Required";
        this.networkWarning = false;

        if (chainIdHex == HEX_NETWORK.ETHEREUM) {
          console.log("Selected chain is Ethereum main-net, Request to switch to " + networkDesc + ".");
        }

        await this.switchNetwork(selectedNetwork);        
        this.networkChange = false;
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
        this.networkMsg = "Network Changed";
        this.networkWarning = false;
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
          this.networkChange = false;
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

  // Calculate actual tokens amounts based on decimals in token
  // Convert to string - ensure no localised chars .  Some issue with BigNumber adding additoinal values - may be due to using es2015
  convertToCoinNumber(amount: any, allowanceExtra:number = 0): string{
        
    let stringAmount = '0';
    let decimalLength = 0;
    let megaDecimalCount = this.MEGA_DECIMALS_COUNT;
    let bigAmount: bigint;

    if (amount != null) {
      amount = Number(amount);

      // Check if number is fractional then remove fractional amount.
      if ((amount % 1 != 0)) {
        decimalLength = amount.toString().split(".")[1].length;
        amount = amount * (10 ** decimalLength);                  // using Exponential ** operator
        bigAmount = BigInt(amount * (10 ** (this.MEGA_DECIMALS_COUNT - decimalLength)));
      }
      else {
        bigAmount = (BigInt(amount) * BigInt(this.MEGA_DECIMALS));
      }

      if (allowanceExtra != 0) {
        bigAmount += BigInt(allowanceExtra);
      }
    }
    else {
      bigAmount = BigInt(0);
    }

    return bigAmount.toString();
  }
}
