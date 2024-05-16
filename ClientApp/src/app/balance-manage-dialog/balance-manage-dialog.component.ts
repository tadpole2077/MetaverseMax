import {ChangeDetectorRef, Component, Inject, NgZone, OnInit, ViewChild} from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { AbstractControl, FormControl, Validators } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom, interval, Subscription } from 'rxjs';
import DetectEthereumProvider from '@metamask/detect-provider';
import Web3 from 'web3';
import { Web3Utils } from '../common/web3Utils';

import { MMBankAbi } from "../Contract/contractMMBankAbi";
import { MCPMegaAbiBNB } from "../Contract/contractMCPMegaAbiBNB";
import { MCPMegaAbiETH } from "../Contract/contractMCPMegaAbiETH";
import { MegaCoinMOCKAbi } from "../Contract/contractMockMegaCoinAbi";
import { Application, WORLD } from '../common/global-var';
import { HEX_NETWORK, METAMASK_ERROR_CODE, MCP_CONTRACT, MCP_CONTRACT_NAME } from "../common/enum";
import { maxBalanceValidator } from '../validator/max-balance.validator'
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatTabChangeEvent } from '@angular/material/tabs';
import { BalanceLogComponent } from '../balance-log/balance-log.component';

@Component({
  selector: 'app-balance-manage-dialog',
  styleUrls: ['./balance-manage-dialog.component.css'],
  templateUrl: './balance-manage-dialog.component.html',
})
export class BalanceManageDialogComponent implements OnInit{

  readonly MEGA_DECIMALS = 1e18;
  readonly MEGA_DECIMALS_COUNT = 18;
  readonly CONTRACT_MCPMEGA = "0x0af8c016620d3ed0c56381060e8ab2917775885e";
  readonly CONTRACT_MMBank = "0xd743E6A6de491Bbe89D262186AD4403aBb410707";
  readonly CONTRACT_MEGA_MOCK = "0x4Dd0308aE43e56439D026E3f002423E9A982aeaF";

  readonly CONTRACT_MCPMEGATOKEN_BNB = this.CONTRACT_MEGA_MOCK // MCP_CONTRACT.MW_BSC;
  readonly CONTRACT_MCPMEGATOKEN_ETH = MCP_CONTRACT.MW_ETHEREUM;

  readonly networkType = HEX_NETWORK.BINANCE_TESTNET_ID;

  insufficientMsg: string = "Insufficient Allowance";
  httpClient: HttpClient;
  baseUrl: string;
  provider: any;
  web3: Web3 = null;
  utils: Web3Utils = new Web3Utils();
  ethereum: any;
  balance: number = 0;
  accountMCPMegaBalance: number = 0;
  currentPlayerWalletKey: string = "";
  subscriptionAccountActive$: Subscription;
  subscriptionBalanceChange$: Subscription;
  subscriptionSystemShutdown$: Subscription;
  amountDepositControl = new FormControl('0');
  amountWithdrawControl = new FormControl('0');

  withdrawRotateActive: boolean = false;
  depositRotateActive: boolean = false;

  systemShutdownPending: boolean = false;
  networkCheckActive: boolean = false;
  networkChange: boolean = false;
  networkMsg: string;
  networkWarning: boolean = false;

  transactionStarted: boolean = false;
  processActive: boolean = true;
  progressMsg: string;
  progressWarning: boolean = false;
  accountActive: boolean = false;

  overLimit: boolean = false;
  depositFocus: boolean = true;
  withdrawFocus: boolean = false;

  tab1Visible: boolean = true;
  tab2Visible: boolean = false;
  balanceRecheckSubscription$: Subscription;

  @ViewChild(MatProgressBar, { static: true }) progressBar: MatProgressBar;
  @ViewChild(BalanceLogComponent, { static: false }) balanceLog: BalanceLogComponent;

  constructor(public dialog: MatDialog, public globals: Application, private zone: NgZone, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string, private cdf: ChangeDetectorRef) {

    this.httpClient = http;
    this.baseUrl = rootBaseUrl + "api/" + globals.worldCode;

    // force refresh of change dedection to display tab content, as initial load both tab hidden due to ngClass rules.
    //this.cdf.detectChanges();
  }

  ngOnInit() {

    // Check client side contract address matching server side - MCP in-world(off-chain) address used for deposit contract.
    //this.checkMCPContractValid();

    this.startBalanceMonitor();

    this.checkSystemShutdown();

    // shutdownPending state change : Monitor state using observable service - true/false.
    this.subscriptionSystemShutdown$ = this.globals.systemShutdown$.subscribe(enabled => {
      this.checkSystemShutdown();
    });

    this.initWeb3().then((active) => {

      if (active && this.currentPlayerWalletKey !== "") {       
        this.checkBalances(true);                        
      }

    });
    
  }

  ngOnDestroy() {

    if (this.subscriptionAccountActive$) {
      this.subscriptionAccountActive$.unsubscribe();
    }
    if (this.subscriptionBalanceChange$) {
      this.subscriptionBalanceChange$.unsubscribe();
    }
    if (this.subscriptionSystemShutdown$) {
      this.subscriptionSystemShutdown$.unsubscribe();
    }
    if (this.balanceRecheckSubscription$) {
      this.balanceRecheckSubscription$.unsubscribe();
    }
  }

  checkSystemShutdown() {

    if (this.globals.systemShutdownPending == false && this.systemShutdownPending) {
      this.systemShutdownPending = true;  
      this.progressMsg = "System was Shutdown and restarted. Please refresh your web page due to new system updates deployed."

    }
    else if (this.globals.systemShutdownPending == true) {
      this.systemShutdownPending = true;      
      this.progressMsg = "System Shutdown in-progress, transaction activities disabled. Please try again in a few minutes time"
    }
  }
  
  changeTab(eventTab: MatTabChangeEvent) {
    
    if (eventTab.index == 0) {
      this.tab1Visible = true;
      this.tab2Visible = false;      
    }
    else {
      this.tab1Visible = false;
      this.tab2Visible = true;
      this.balanceLog.getOwnerLog(this.globals.ownerAccount.matic_key);
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

    this.subscriptionBalanceChange$ = this.globals.balanceChange$.subscribe(balanceChange => {
      if (balanceChange) {
        console.log("account balance updated");
        this.checkBalances(false);
      }
    });
  }

  async checkBalances(MCPMegaCheck) {

    if (MCPMegaCheck) {
      
      this.manageNetwork(this.networkType).then((networkActive) => {

        // Network Active check.
        if (networkActive) {

          this.getMCPMegaBalance().then((megaBalance) => {

            this.accountActive = true;

            this.zone.run(() => {
              this.accountMCPMegaBalance = megaBalance;
              let orgValue = this.amountDepositControl.value;

              // Update validation rules - max and min.
              this.amountDepositControl = new FormControl( orgValue == "0" ? "0.1" : orgValue, [
                Validators.required,
                (control: AbstractControl) => Validators.max(Number(this.accountMCPMegaBalance))(control),
                (control: AbstractControl) => Validators.min(0.0001)(control)
              ]);

              this.insufficientMsg = "Insufficient Mega Balance"; 
            });

          });                     
        }

      });      
    }

    // Trigger zone update event, refresh balance
    this.zone.run(() => {
      
      this.balance = Math.floor(this.globals.ownerAccount.balance);
      let orgValue = this.amountWithdrawControl.value;
      
      this.amountWithdrawControl = new FormControl(orgValue == "0" ? "0.1" : orgValue, [
        Validators.required,
        (control: AbstractControl) => Validators.max(this.balance)(control),    // dynamic validator - find max per invoke.
        (control: AbstractControl) => Validators.min(0.0001)(control)
      ]);
    });
  }
  
  delayBalanceRecheck() {
    // Due to delays in contract log updates - check the balance again in a few seconds time.
    this.balanceRecheckSubscription$ = interval(3000)
      .subscribe(
        async (val) => {
          this.checkBalances(true);
          this.balanceRecheckSubscription$.unsubscribe();
          this.balanceRecheckSubscription$ = null;
        }
      );
  }


  // Read View - Get MCPMega Balance for current account.
  async getMCPMegaBalance() {

    // Create a new contract object using the ABI and bytecode
    const contractMCPMega = new this.web3.eth.Contract(
      this.globals.selectedWorld == WORLD.BNB ? MCPMegaAbiBNB : MCPMegaAbiETH,
      this.globals.selectedWorld == WORLD.BNB ? this.CONTRACT_MCPMEGATOKEN_BNB : this.CONTRACT_MCPMEGATOKEN_ETH);

    let balanceReturned = 0;

    try {
      // Get balance matching correct contract
      //balanceReturned = await contractMCPMega.methods.balanceOf(this.currentPlayerWalletKey).call();
      contractMCPMega.methods.balanceOf(this.currentPlayerWalletKey).call();
    }
    catch (error) {
      console.error(error);
    }

    return this.convertFromEVMtoCoinLocale(balanceReturned, 0);
  }


  // ************************************************************
  // Deposit Function set
  //  (1) approval -  allow deposit to MM Bank contract
  //  (2) depositMega - metaverseMax bank contract to record deposit and store Mega 
  //  (3) confirmTransaction - server side update account balance after security checks (a) correct network and contract (b) deposit event (c) not recorded previously
  async depositMegaToMMBankWithAllowance() {

    const addressFrom = this.currentPlayerWalletKey;
    const addressReceiver = this.CONTRACT_MMBank;
    const megaValue = this.amountDepositControl.value;

    this.depositRotateActive = true;

    // Test code to use testnet for deposit/withdraw
    let testnet:boolean = await this.checkBNBTestnet();

    if (testnet == false) {
      return;
    }

    // Create a new contract object using the ABI and bytecode
    const MCPMegaContract = new this.web3.eth.Contract(
      MegaCoinMOCKAbi,
      this.CONTRACT_MEGA_MOCK
    );

    try {

      this.transactionStarted = true;
      this.setProgressBarMsg("1. Allow Deposit Approval (Security Check)", true, 10, false);

      // totalFees = gasLimit * gasPrice (in Wei).
      // Get Current Gas Price in GWEI  - this is actually the max fee per gas GWEI - it changes per block. 
      const gasPrice = await this.web3.eth.getGasPrice();

      // Using Ethers, Get Estimate of Gas to Use. Add 25% extra buffer
      const estimatedGas = await MCPMegaContract.methods.approve(addressReceiver, this.convertToCoinNumber(megaValue, 0))
        .estimateGas(
          {
            from: addressFrom
          }
        ) * 5n / 4n;  

      await MCPMegaContract.methods.approve(addressReceiver, this.convertToCoinNumber(megaValue, 0))        
        .send({
          from: addressFrom,
          gasPrice: this.utils.toHex(gasPrice),
          gas: estimatedGas.toString()
        })
        .on('sent', (receipt) => {
          //console.log('receipt: ' + receipt);
          this.zone.run(() => {
            this.setProgressBarMsg('1. Approval Transaction In-Progress...', true, 15, false);
          });
        })
        .then((result) => {
          console.log('Allowance increased: ' + result);
          this.setProgressBarMsg("1. Allow Transaction Completed (Security Check)", true, 25, false);

          this.deposit(megaValue);

        })
        .catch((error) => {
          console.log(error);

          this.depositRotateActive = false;
          this.setProgressBarMsg("1. Allow Deposit Denied (Security Check)", false, 30, true);
        });

    }
    catch (error) {
      console.error(error);
      this.depositRotateActive = false;
      this.setProgressBarMsg("1. Allow Deposit Error (Security Check) - (contact support)", false, 30, true);
    }

    return;    
  }

  async deposit(megaValue: string) {

    const addressFrom = this.currentPlayerWalletKey;    
    //this.rotateActive = true;
    //this.progressCaption = "Transaction in Progress"

    // Create a new contract object using the ABI and bytecode
    const MMBankContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank
    );

    try {           

      // CHECK : Approval amount matching initial deposit amount, or less. Owner may have edited/reduced approval amount.
      const megaAllowance = await this.getMMBankMegaAllowance();
      megaValue = Number(megaAllowance) <= Number(megaValue) ? megaAllowance : megaValue

      const megaValueBN = this.convertToCoinNumber(megaValue);

      // totalFees = gasLimit * gasPrice (in Wei).
      // Get Current Gas Price in GWEI  - this is actually the max fee per gas GWEI - it changes per block. 
      const gasPrice = await this.web3.eth.getGasPrice();

      // Using Ethers, Get Estimate of Gas to Use. Add 25% extra buffer
      const estimatedGas = await MMBankContract.methods.depositMega(megaValueBN)  
        .estimateGas(
          {
            from: addressFrom
          }
      ) * 5n / 4n;


      await MMBankContract.methods.depositMega(megaValueBN)        
        .send({
          from: addressFrom,
          gasPrice: this.utils.toHex(gasPrice),
          gas: "150000"
        })
        .on('sent', (receipt) => {
          //console.log('receipt: ' + receipt);
          this.zone.run(() => {
            this.setProgressBarMsg('2. Deposit Transaction In-Progress...', true, 50, false);
          });
        })
        .then((result) => {
          console.log('Deposited mega to bank : ' + result);

          this.zone.run(() => {
            this.setProgressBarMsg("2. Deposit Completed", true, 65, false);
          });

          this.confirmTransaction(result.transactionHash, "3");
        })
        .catch((error) => {
          console.log(error);
          this.depositRotateActive = false;
          this.setProgressBarMsg("2. Deposit Transaction - Denied", false, 50, true);
        });

    }
    catch (error) {
      console.error(error);
      this.depositRotateActive = false;
      this.setProgressBarMsg("2. Deposit Transaction Error - (contact support)", false, 50, true);
    }
    return;    
  }

  // Read View - Get MCPMega Allowance for current account.
  // Allowance between wallet owner && MEGA_MANAGER contract
  async getMMBankMegaAllowance() {

    let owner = this.currentPlayerWalletKey;
    let spender = this.globals.selectedWorld == WORLD.BNB ? this.CONTRACT_MMBank : this.CONTRACT_MMBank;   // spender  MM Bank contract - handles deposits into MMBalance

    // Create a new contract object using the ABI and bytecode
    const contractMCPMega = new this.web3.eth.Contract(
      MCPMegaAbiBNB,
      this.CONTRACT_MEGA_MOCK);
      //this.globals.selectedWorld == WORLD.BNB ? this.CONTRACT_MCPMEGATOKEN_BNB : this.CONTRACT_MCPMEGATOKEN_ETH);

    // Get the current value of my number
    const allowanceReturned = await contractMCPMega.methods.allowance(owner, spender).call();

    return this.convertFromEVMtoCoinLocale(allowanceReturned, 2);
  }

  confirmTransaction(hash: string, stage: string) {

    let params = new HttpParams();
    params = params.append('hash', hash);

    this.httpClient.get<boolean>(this.baseUrl + '/bank/confirmTransaction', { params: params })
      .subscribe({
        next: (result) => {
          this.globals.updateUserBankBalance(this.baseUrl, this.currentPlayerWalletKey);

          this.depositRotateActive = false;
          this.withdrawRotateActive = false;

          if (result == false) {
            this.setProgressBarMsg(stage + ". Transaction confirm issue identified, (contact support)", false, 100, true);
          }
          else {
            this.setProgressBarMsg(stage + ". Transaction Confirmed, Balance Updated", false, 100, false);
          }
        },
        error: (error) => {
          console.error(error);
          this.setProgressBarMsg(stage + ". Transaction confirm issue! (contact support)", false, 100, true);
          this.withdrawRotateActive = false;
          this.depositRotateActive = false;
        }
      });
  
    return;
  }  

  // Controls enable/disable of Deposit button
  get checkInvalidDeposit(): boolean {
    return this.amountDepositControl.hasError('max') ||
      this.amountDepositControl.hasError('min') ||
      this.amountDepositControl.hasError('required') ||
      this.accountActive == false ||
      this.systemShutdownPending; 
  }
  // ************************************************************


  // ************************************************************
  // Withdraw function set
  async withdrawAllowanceApprove() {

    const ownerMaticKey = this.currentPlayerWalletKey;
    let params = new HttpParams();    
    const withdrawMegaAmount = this.amountWithdrawControl.value;
    const withdrawMegaAmountNumber = Number(withdrawMegaAmount);

    if (withdrawMegaAmountNumber == null || withdrawMegaAmountNumber == 0) {
      return;
    }

    // Test code to use testnet for deposit/withdraw
    let testnet:boolean = await this.checkBNBTestnet();

    if (testnet == false) {
      return;
    }

    this.withdrawRotateActive = true;
    this.transactionStarted = true;
    this.setProgressBarMsg("1. Sign Withdraw Approval (Security Check)", true, 25, false);

    let signResult = await this.walletSign(withdrawMegaAmountNumber);

    if (signResult != "") {

      this.setProgressBarMsg("2. Checking Balance Allowed (Security Check)", true, 50, false);

      params = params.append('amount', withdrawMegaAmount);
      params = params.append('ownerMaticKey', ownerMaticKey);
      params = params.append('personalSign', signResult);
      
      this.httpClient.get<boolean>(this.baseUrl + '/bank/WithdrawAllowanceApprove', { params: params })
        .subscribe({
          next: (result) => {

            if (result == true) {
              this.setProgressBarMsg("3. Withdraw Completed, balance updated", false, 100, false);
              console.log('Withdrawal of mega from bank : ' + result);
                       
              // Check wallet for change in BB Mega Balance [internal call] , due to subscribed change event - this will also trigger local checkBalances() but with false parameter meaning it wont update the MCP 
              this.globals.updateUserBankBalance(this.baseUrl, this.currentPlayerWalletKey);

              // Check all balance changes : both on-chain wallet from Mega Balance Contract (MCP contract view call), and internal BB balance
              this.checkBalances(true);  

              this.delayBalanceRecheck();

              //this.withdrawMegaFromMMBank(withdrawMegaAmountNumber);
              this.withdrawRotateActive = false;
            }
            else {
              this.setProgressBarMsg("3. Invalid Withdraw - balance issue (Contact Support)", false, 75, true);              
              this.withdrawRotateActive = false;
            }

          },
          error: (error) => {
            console.error(error);
            this.withdrawRotateActive = false;
            this.setProgressBarMsg("2. Invalid Withdraw - balance error (Contact Support)", false, 75, true);
          }
        });
    }


    return;
  }

  async walletSign(amount: number) {
    const ethereum = (window as any).ethereum;
    let result: string = "";

    try {
      if (ethereum) {

        result = await ethereum.request({
          "method": "personal_sign",
          "params": [
            await this.getWithdrawSignCode(amount),
            this.currentPlayerWalletKey
          ]
        });

      }
    }
    catch(err) {
      console.error(err);
      this.setProgressBarMsg("1. Sign Denied (Security Check) - Canceled", false, 10, true);

      this.withdrawRotateActive = false;
    }

    return result;
  }

  async getWithdrawSignCode(amount: number) {

    let params = new HttpParams();
    params = params.append('amount', amount);
    params = params.append('ownerMaticKey', this.currentPlayerWalletKey);
    let code;

    await firstValueFrom(this.httpClient.get(this.baseUrl + '/bank/getWithdrawSignCode', { params: params, responseType: "text" }), { defaultValue: "" })      
      .then((result) => {
        code = result
      });

    return code;
  }

  // Not Used - Client side withdraw invoked call - done on server, owner only call design choice
  /*async withdrawMegaFromMMBank(megaValue: number) {

    const addressFrom = this.currentPlayerWalletKey;

    this.setProgressBarMsg('3. Actual Withdraw Transaction', true, 50, false);    

    // Create a new contract object using the ABI and bytecode
    const MMBankContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank
    );

    try {

      const gasPrice = await this.web3.eth.getGasPrice();
      const megaValueBN = this.convertToCoinNumber(megaValue);

      await MMBankContract.methods.withdrawMega(megaValueBN)        
        .send({
          from: addressFrom,
          gasPrice: this.utils.toHex(gasPrice),
          gas: "100000"
        })
        .on('sent', (receipt) => {
          //console.log('receipt: ' + receipt);
          this.zone.run(() => {
            this.setProgressBarMsg('3. Withdraw Transaction In-Progress...', true, 60, false);
          });
        })
        .then((result) => {
          console.log('Withdrawal of mega from bank : ' + result);

          this.setProgressBarMsg('3. Withdraw Completed', true, 75, false);

          this.confirmTransaction(result.transactionHash, "4");

          // Check external wallet for change in Mega Balance (MCP contract view call)
          this.checkBalances(true);           
        })
        .catch((error) => {
          console.log(error);
          this.setProgressBarMsg('3. Partial Withdraw Canceled (check bank log)', false, 75, true);
          this.withdrawRotateActive = false;
        });

    }
    catch (error) {
      console.error(error);
      this.setProgressBarMsg("3. Partial Withdraw Error - check bank log", false, 75, true);
      this.withdrawRotateActive = false;
    }

    return;    
  }*/

  checkWithdrawMax(valueEntered: number) {
    if (valueEntered > this.balance) {
      //this.amountWithdrawControl.addValidators.hasError('over-limit')
      this.overLimit = true;
    }
    else {
      this.overLimit = false;
    }
  }

  // accountActive (flag) : if Mega balance retrieved = true
  get checkInvalidWithdraw(): boolean {
    return this.amountWithdrawControl.hasError('max') ||
      this.amountWithdrawControl.hasError('min') ||
      this.amountWithdrawControl.hasError('required') ||
      this.accountActive == false ||
      this.systemShutdownPending;

  }
  //*****************************************************************


  setProgressBarMsg(message:string, active: boolean, barValue: number, warningActive: boolean) {
    this.progressMsg = message;
    this.processActive = active;
    this.progressBar.value = barValue;
    this.progressWarning = warningActive;
  }


  async initWeb3() {        

    let active: boolean = false;

    try {
      this.provider = await DetectEthereumProvider();
      this.ethereum = (window as any).ethereum;
      
      // Check Metamask Provider :  Supporting Metamask & CoinbaseWallet
      if (await this.globals.checkApprovedWalletType()) {
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

  //*******************************************************************
  // NETWORK Fn's
  async manageNetwork(selectedChain: HEX_NETWORK) {

    let chainIdHex: string;
    let networkCorrect: boolean = false;
    let connected: boolean = false;
    //let selectedChain: HEX_NETWORK;

    if (selectedChain == HEX_NETWORK.APP_SELECTED) {
      switch (this.globals.selectedWorld) {
				case WORLD.BNB:
          selectedChain = HEX_NETWORK.BINANCE_ID;
          break;
        case WORLD.ETH:
          selectedChain = HEX_NETWORK.ETHEREUM_ID;
          break;
        case WORLD.TRON:
          selectedChain = HEX_NETWORK.POLYGON_ID;
          break;
				default:
					selectedChain = HEX_NETWORK.BINANCE_ID;
			}
    }

    if (selectedChain == HEX_NETWORK.BINANCE_ID) {
      connected = await this.checkNetwork(selectedChain, "Binance Smart Chain");
    }
    else if (selectedChain == HEX_NETWORK.ETHEREUM_ID) {
      connected = await this.checkNetwork(selectedChain, "Ethereum Mainnet");
    }
    else if (selectedChain == HEX_NETWORK.POLYGON_ID) {
      connected = await this.checkNetwork(selectedChain, "Polygon Mainnet");
    }


    if (connected && this.provider) {
      // Must be wallet connected to request network active (chain type).
      chainIdHex = await this.ethereum.request({ method: "eth_chainId", params: [] });
      networkCorrect = chainIdHex == selectedChain;     
    }
    else {
      this.networkMsg = "Network Not Changed, unable to proceed..";
      this.networkWarning = true;
    }

    this.cdf.detectChanges();

    return networkCorrect;
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

        this.networkCheckActive = true;
        this.networkChange = true;
        this.networkMsg = "Change Network Required ("+ networkDesc + ")";
        this.networkWarning = false;

        if (chainIdHex == HEX_NETWORK.ETHEREUM_ID) {
          console.log("Selected chain is Ethereum main-net, Request to switch to " + networkDesc + ".");
        }
        else if (chainIdHex == HEX_NETWORK.BINANCE_ID) {
          console.log("Selected chain is Binance Smart Chain, Request to switch to " + networkDesc + ".");
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
            else if (chainIdHex == HEX_NETWORK.ETHEREUM_ID) {
              // Add Polygon chain
              await ethereum.request({
                method: 'wallet_addEthereumChain',
                params: [
                  {
                    chainId: HEX_NETWORK.POLYGON_ID,
                    blockExplorerUrls: ['https://etherscan.io'],
                    chainName: 'Ethereum Mainnet',
                    nativeCurrency: {
                      decimals: 18,
                      name: 'Ethereum',
                      symbol: 'ETH'
                    },
                    rpcUrls: ['https://mainnet.infura.io/v3/']
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
  //*******************************************************************


  //*******************************************************************
  // COIN CONVERTER Fn's 
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
  //*******************************************************************
}
