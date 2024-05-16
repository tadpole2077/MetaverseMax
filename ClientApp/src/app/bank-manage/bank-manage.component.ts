import { HttpClient, HttpParams } from "@angular/common/http";
import { Component, Inject } from "@angular/core";
import { HEX_NETWORK, METAMASK_ERROR_CODE } from "../common/enum";
import { Application } from "../common/global-var";
import { FormControl } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';

import DetectEthereumProvider from '@metamask/detect-provider';
//import { Web3Eth } from 'web3-eth';
import Web3 from 'web3';
import { Web3Utils } from '../common/web3Utils';
//import Web3Contract from 'web3-eth-contract';
import { Contract } from 'web3-eth-contract';
import { isAddress } from 'web3-validator';
import { MMBankAbi } from "../Contract/contractMMBankAbi";
import { testBasic_abi } from "../Contract/contractBasicAbi";
import { MegaCoinMOCKAbi } from "../Contract/contractMockMegaCoinAbi";
import { firstValueFrom, lastValueFrom, Subscription } from "rxjs";


@Component({
  selector: 'app-bank-manage',
  templateUrl: './bank-manage.component.html',
  styleUrls: ['./bank-manage.component.css']
})
export class TESTBankManageComponent {

  subscriptionAccountActive$: Subscription;
  private httpClient: HttpClient;
  private baseUrl: string;
  progressCaption: string = "";
  contractRead: string = "";
  contractWrite: string = "";
  rotateActive: boolean = false;
  ethereum: any;
  provider: any;
  web3: Web3 = null;
  utils: Web3Utils = new Web3Utils();
  addressSender = new FormControl('');
  addressReceiver = new FormControl('');
  currentPlayerWalletKey: string;

  readonly CONTRACT_MMBank = "0xd743E6A6de491Bbe89D262186AD4403aBb410707";
  readonly CONTRACT_ADMIN = "0xFA87a94a37Ffd3e7d6Ae35FF33eB5d15A5A87467";
  readonly CONTRACT_MEGA = "0x4Dd0308aE43e56439D026E3f002423E9A982aeaF";
  readonly MEGA_DECIMALS = 1e18;
  readonly MEGA_DECIMALS_COUNT = 18;

  constructor(public globals: Application, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string) {

    this.httpClient = http;
    this.baseUrl = rootBaseUrl + "api/" + globals.worldCode;

    this.ethereum = (window as any).ethereum;
    this.currentPlayerWalletKey = globals.ownerAccount.public_key;
    
   
  }

  ngOnInit() {

    // Monitor using service - when account status changes - active / inactive.
    this.subscriptionAccountActive$ = this.globals.accountActive$.subscribe(active => {
      if (active) {
        this.currentPlayerWalletKey = this.globals.ownerAccount.public_key;
        this.currentPlayerWalletKey = "0xFA87a94a37Ffd3e7d6Ae35FF33eB5d15A5A87467";
      }
      else {
        this.currentPlayerWalletKey = "";
      }
    })

    this.startWebProcess();
  }

  ngOnDestroy() {
    this.subscriptionAccountActive$.unsubscribe();
  }

  confirmTransaction(hash: string) {

    let params = new HttpParams();
    params = params.append('hash', hash);

    this.httpClient.get<number>(this.baseUrl + '/bank/confirmTransaction', { params: params })
      .subscribe({
        next: (result) => {
          this.globals.updateUserBankBalance(this.baseUrl, this.currentPlayerWalletKey);
        },
        error: (error) => { console.error(error) }
      });
  
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
    }

    return result;
  }

  async withdrawAllowanceApprove(amount: number, ownerMaticKey: string) {

    let params = new HttpParams();
    let signResult = await this.walletSign(amount);

    if (signResult != "") {

      params = params.append('amount', amount);
      params = params.append('ownerMaticKey', ownerMaticKey);
      params = params.append('personalSign', signResult);

      this.httpClient.get<boolean>(this.baseUrl + '/bank/WithdrawAllowanceApprove', { params: params })
        .subscribe({
          next: (result) => {

            if (result == true) {
              this.globals.updateUserBankBalance(this.baseUrl, ownerMaticKey);
              //this.withdrawMegaFromMMBank(amount);
            }

          },
          error: (error) => { console.error(error) }
        });
    }
    return;
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


  checkSafeNumber(amount: any) {
    return Number.isSafeInteger(amount);
  }

  // Calculate actual tokens amounts based on decimals in token
  // Convert to string - ensure no localised chars .  Some issue with BigNumber adding additoinal values - may be due to using es2015
  convertToCoinNumber(amount: number, allowanceExtra:number = 0): string{
    
    // Check if number is fractional then remove fractional amount.
    let stringAmount = '0';
    let decimalLength = 0;
    let megaDecimalCount = this.MEGA_DECIMALS_COUNT;
    let bigAmount;

    if ((amount % 1 != 0)) {
      decimalLength = amount.toString().split(".")[1].length;
      amount = amount * (10 ** decimalLength);      // using Exponential ** operator
      bigAmount = BigInt(amount * (10 ** (this.MEGA_DECIMALS_COUNT - decimalLength)) ) ;
    }
    else {
      bigAmount = (BigInt(amount) * BigInt(this.MEGA_DECIMALS));
    }

    if (allowanceExtra != 0) {
      bigAmount += BigInt(allowanceExtra);
    }

    return bigAmount.toString();
  }

  convertFromEVMtoCoinLocale(amount: any) {
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

  async startWebProcess() {

    let chainIdHex: string;
    //let addressTo = "0x3C0a39162f3bb1C4fE37676a4677E47F067e035b";

    await this.initWeb3();
    let connected = await this.checkNetwork(HEX_NETWORK.BINANCE_TESTNET_ID, "Binance Testnet");

    chainIdHex = await this.ethereum.request({ method: "eth_chainId", params: [] })
    if (connected &&
      chainIdHex == HEX_NETWORK.BINANCE_TESTNET_ID &&
      await this.globals.checkApprovedWalletType() &&
      this.web3) {

      const addressFrom = this.currentPlayerWalletKey;
      this.addressSender.setValue(this.CONTRACT_MMBank);
      this.addressReceiver.setValue(addressFrom);      
    }

  }

  async initWeb3() {        

    try {
      this.provider = await DetectEthereumProvider();
      
      // Check Metamask Provider :  Supporting Metamask & CoinbaseWallet
      if (await this.globals.checkApprovedWalletType()) {
        this.web3 = new Web3(this.provider);
      }
    }
    catch (error) {
      console.log("provider error: " + error);
      this.web3 = null;
    }
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
        if (chainIdHex == HEX_NETWORK.ETHEREUM_ID) {
          console.log("Selected chain is Ethereum main-net, Request to switch to " + networkDesc + ".");
        }

        this.progressCaption = "Request to switch to " + networkDesc;
        await this.switchNetwork(selectedNetwork);
        this.progressCaption = "";
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
          }

        }
        catch (error) {
          // user rejects the request to "add chain" or param values are wrong, maybe you didn't use hex above for `chainId`?
          console.log("wallet_addEthereumChain Error: ${error.message}")
        }
      }

      return networkSwitched;
    }

  }

  async estimateGas() {

    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank);


    //const gas = await testContract.methods.write().estimateGas({
    //    from: defaultAccount,
    //});
    //console.log('estimated gas:', gas);
  }


  // Using : transferFrom(address src, address dst, uint256 rawAmount)
  // Note a 2nd transfer method used by MW : transferSender(address _sender, address _from, address _to, uint256 _tokenId)
  async contractTestSendRequest(addressTo: string, addressFrom: string) {

    const provider = await DetectEthereumProvider();
    const ethereum = (window as any).ethereum;
    //const contractMethod = "0x60806040";
    const CONTRACT_ADDRESS = "0xa80f07449BA85d60b28E11FD74f0e7C9FB09D553";
    const valuePara1 = 666;

    //console.log('Ethereum successfully detected!');
    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"



    // Create a new contract object using the ABI and bytecode
    //const testContractJSON = require('../Contract/contract.json');

    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      CONTRACT_ADDRESS,
      this.web3);

    //let to_address = Utils.padLeft(addressTo.toLowerCase(), 64).substring(2);

    // Matic MCP Contract Call
    let token_uint256 = this.utils.padLeft(this.utils.toHex(valuePara1), 64).substring(2);
    //let transferData =
    //  contractMethod +
    //  token_uint256;

    try {
      const newNumber_uint256 = this.utils.padLeft(this.utils.toHex(14), 64).substring(2);

      type Fn = typeof testContract.methods.write;
      type Arguments = Parameters<Fn>;

      // Set the value of my number
      //const receipt = await testContract.methods['write(uint256)']()
      const receipt = await testContract.methods.seedAccount(newNumber_uint256)
        .send({
          from: addressFrom
        })
        .then((result) => {
          console.log('result: ' + result);
          this.progressCaption = "Transaction Completed";
          this.contractWrite = "Seed deposit assigned";
          this.rotateActive = false;
        })
        .catch((error) => {
          console.log(error);
          this.progressCaption = "Error occured blocking Transaction";
          this.rotateActive = false;
          // If the request fails, the Promise rejects with an error.
        });

      this.rotateActive = false;
    }
    catch (error) {
      console.error(error);
    }

    try {
      const newNumber_uint256 = this.utils.padLeft(this.utils.toHex(4), 64).substring(2);

      type Fn = typeof testContract.methods.write;
      type Arguments = Parameters<Fn>;

      // Set the value of my number
      //const receipt = await testContract.methods['write(uint256)']()
      const receipt = await testContract.methods.depositBank(newNumber_uint256)
        .send({
          from: addressFrom
        })
        .then((result) => {
          console.log('result: ' + result);
          this.progressCaption = "Transaction Completed";
          this.contractWrite = "Bank deposit sent";
          this.rotateActive = false;
        })
        .catch((error) => {
          console.log(error);
          this.progressCaption = "Error occured blocking Transaction";
          this.rotateActive = false;
          // If the request fails, the Promise rejects with an error.
        });

      this.rotateActive = false;
    }
    catch (error) {
      console.error(error);
    }


    try {
      let from_address = this.utils.padLeft(CONTRACT_ADDRESS, 64).substring(2);

      // Get the current value of my number
      const balance = await testContract.methods.balances(from_address).call();
      this.contractRead = "Bank Balance: " + balance;
    }
    catch (error) {
      console.error(error);
    }

    return;
  }

  // Read View Methods
  async getBankBalance(addressFrom: string) {

    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    const gasPrice = await this.web3.eth.getGasPrice();

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank);

    // Get the current value of my number
    const valueBank = await testContract.methods.getBalanceOfBank().call();
    //const value = await testContract.methods.getBalanceOf(addressFrom).call();

    this.contractRead = this.CONTRACT_MMBank + "Bank has Mega balance of : " + this.convertFromEVMtoCoinLocale(valueBank);
    this.rotateActive = false;
    return;
  }

  async getCurrentWalletBalanceInBank() {

    const addressFrom = this.currentPlayerWalletKey;
    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    const gasPrice = await this.web3.eth.getGasPrice();

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank);

    // Get the current value of my number    
    const balance = await testContract.methods.balances(addressFrom).call();

    this.contractRead = "Bank has Mega balance of : " + Number(BigInt(balance) / BigInt(this.MEGA_DECIMALS));
    this.rotateActive = false;
    return;
  }

  async getCurrentWalletBalanceMega() {

    const addressFrom = this.currentPlayerWalletKey;
    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    const gasPrice = await this.web3.eth.getGasPrice();

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MegaCoinMOCKAbi,
      this.CONTRACT_MEGA);

    // Get the current value of my number    
    const balance = await testContract.methods.balanceOf(addressFrom).call();

    this.contractRead = "Wallet has Mega balance of : " + Number(BigInt(balance) / BigInt(this.MEGA_DECIMALS));
    this.progressCaption = "Transaction Completed";
    this.rotateActive = false;
    return;
  }

  async getMegaContract() {

    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    const gasPrice = await this.web3.eth.getGasPrice();

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank);

    // Get the current value of my number
    const contract = await testContract.methods.getMegaContract().call();

    this.contractRead = "Mega Contract Address : " + contract;
    this.progressCaption = "Transaction Completed";
    this.rotateActive = false;
    return;
  }

  async contractTestMega() {

    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank,
      this.web3);

    // Get the current value of my number
    const value = await testContract.methods.testMega().call()  
      .then((result) => {
        console.log('result: ' + result);
        this.contractRead = " Test Mega response : " + result;
        this.progressCaption = "Transaction Completed";
        this.rotateActive = false;
      })
      .catch((error) => {
        console.log(error);
        this.progressCaption = "Error occured blocking Transaction";
        this.contractWrite = error;
        this.rotateActive = false;          
    });

  }

  async getBankAdmin() {

    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank);

    // Get the current value of my number
    const value = await testContract.methods.getAdminAddress().call()  
      .then((result) => {
        console.log('result: ' + result);
        this.contractRead = " Current admin address : " + result;
        this.progressCaption = "Transaction Completed";
        this.rotateActive = false;
      })
      .catch((error) => {
        console.log(error);
        this.progressCaption = "Error occured blocking Transaction";
        this.contractWrite = error;
        this.rotateActive = false;          
    });

  }

  async contractBalanceRead(addressFrom: string) {

    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank,
      this.web3);

    // Get the current value of my number
    const value = await testContract.methods.balances(addressFrom).call()  
      .then((result) => {
        console.log('result: ' + result);
        this.contractRead = addressFrom + " Balance : " + result;
        this.progressCaption = "Transaction Completed";
        this.rotateActive = false;
      })
      .catch((error) => {
        console.log(error);
        this.progressCaption = "Error occured blocking Transaction";
        this.contractWrite = error;
        this.rotateActive = false;          
    });

  }

  async getAllowanceCustom(addressSender: string, addressReceiver: string) {

    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank,
      this.web3);

    // Get the current value of my number
    const value = await testContract.methods.getAllowance(addressSender, addressReceiver).call()  
      .then((result) => {
        console.log('result: ' + result);
        this.contractRead = addressSender + " allowance for transfer to " + addressReceiver + ": " + this.convertFromEVMtoCoinLocale(result);
        this.progressCaption = "Transaction Completed";
        this.rotateActive = false;
      })
      .catch((error) => {
        console.log(error);
        this.progressCaption = "Error occured blocking Transaction";
        this.contractWrite = error;
        this.rotateActive = false;          
    });

  }

  async getAllowanceToBank() {

    const addressFrom = this.currentPlayerWalletKey;
    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank,
      this.web3);

    // Get the current value of my number
    const value = await testContract.methods.getAllowanceToBank(addressFrom).call()  
      .then((result) => {
        console.log('result: ' + result);
        this.contractRead = addressFrom + " allowance to MMBank : " + this.convertFromEVMtoCoinLocale(result);
        this.progressCaption = "Transaction Completed";
        this.rotateActive = false;
      })
      .catch((error) => {
        console.log(error);
        this.progressCaption = "Error occured blocking Transaction";
        this.contractWrite = error;
        this.rotateActive = false;          
    });

  }

  async contractTestReadRequest(addressTo: string, addressFrom: string) {

    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank,
      this.web3);

    // Get the current value of my number
    const value = await testContract.methods.read().call()
      .catch((error) => {
        console.log(error);
        this.progressCaption = "Error occured blocking Transaction";
        this.contractWrite = error;
        this.rotateActive = false;
        // If the request fails, the Promise rejects with an error.
      });;

    this.contractRead = "Stored Value: " + value;
    this.progressCaption = "Transaction Completed";
    this.rotateActive = false;

  }




  // Write Methods
  async increaseAllowance( addressReceiver: string, allowance:number) {

    const addressFrom = this.currentPlayerWalletKey;
    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MegaCoinMOCKAbi,
      this.CONTRACT_MEGA
    );

    try {

      const gasPrice = await this.web3.eth.getGasPrice();

      await testContract.methods.increaseAllowance(addressReceiver, this.convertToCoinNumber(allowance)+1)        
        .send({
          from: addressFrom,
          gasPrice: this.utils.toHex(gasPrice),
          gas: "100000"
        })
        .then((result) => {
            console.log('Allowance increased: ' + result);
            this.progressCaption = "Transaction Completed";
            this.contractWrite =  addressFrom + " Allowance to Bank increased by "+ allowance;
            this.rotateActive = false;
          })
        .catch((error) => {
          console.log(error);
          this.progressCaption = "Error occured blocking Transaction";
          this.contractWrite = error;
          this.rotateActive = false;
        });

    }
    catch (error) {
      console.error(error);
      this.progressCaption = error;
      this.rotateActive = false;
    }
    return;    
  }

  async increaseBankAllowance( allowance:number) {

    const addressFrom = this.currentPlayerWalletKey;
    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MegaCoinMOCKAbi,
      this.CONTRACT_MMBank
    );

    try {

      const gasPrice = await this.web3.eth.getGasPrice();

      await testContract.methods.increaseAllowance(addressFrom, this.convertToCoinNumber(allowance+1))        
        .send({
          from: addressFrom,
          gasPrice: this.utils.toHex(gasPrice),
          gas: "100000"
        })
        .then((result) => {
            console.log('Bank Allowance increased: ' + result);
            this.progressCaption = "Transaction Completed";
            this.contractWrite =  this.CONTRACT_MMBank + " Allowance to "+addressFrom+" increased by "+ allowance;
            this.rotateActive = false;
          })
        .catch((error) => {
          console.log(error);
          this.progressCaption = "Error occured blocking Transaction";
          this.contractWrite = error;
          this.rotateActive = false;
        });

    }
    catch (error) {
      console.error(error);
      this.progressCaption = error;
      this.rotateActive = false;
    }
    return;    
  }


  async testMegaWrite(megaValue: number, addressFrom: string) {

    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank
    );

    try {

      const gasPrice = await this.web3.eth.getGasPrice();

      await testContract.methods.testMegaWrite(megaValue)        
        .send({
          from: addressFrom,
          gasPrice: this.utils.toHex(gasPrice),
          gas: "100000"
        })
        .then((result) => {
            console.log('Deposited mega to bank : ' + result);
            this.progressCaption = "Transaction Completed";
            this.contractWrite =  addressFrom + " write "+ megaValue +" test on MockMega completed";
            this.rotateActive = false;
          })
        .catch((error) => {
          console.log(error);
          this.progressCaption = "Error occured blocking Transaction";
          this.contractWrite = error;
          this.rotateActive = false;
        });

    }
    catch (error) {
      console.error(error);
      this.progressCaption = error;
      this.rotateActive = false;
    }
    return;    
  }


  async withdraw(withdrawAmount: number, currentPlayerWalletKey: string) {

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank
    );

    try {

      const recipient = "0xb197dC47fCbE7D7734B60fA87FD3b0BA0ACaf441";
      const gasPrice = await this.web3.eth.getGasPrice();
      const megaValueBN = this.convertToCoinNumber(withdrawAmount);
     
      await testContract.methods.withdrawMega(recipient, megaValueBN)        
        .send({
          from: this.CONTRACT_ADMIN,
          gasPrice: this.utils.toHex(gasPrice),
          gas: "100000"
        })
        .then((result) => {
          console.log('Withdrawal of mega from bank : ' + result);
          this.progressCaption = "Transaction Completed";
          this.contractWrite =  currentPlayerWalletKey + " Withdrawal "+ withdrawAmount +" mega from bank";
          this.rotateActive = false;

          //this.confirmTransaction(result.transactionHash);
        })
        .catch((error) => {
          console.log(error);
          this.progressCaption = "Error occured blocking Transaction";
          this.contractWrite = error;
          this.rotateActive = false;
        });

    }
    catch (error) {
      console.error(error);
      this.progressCaption = error;
      this.rotateActive = false;
    }
    return;   
  }

  /*
  async withdrawMegaFromMMBank(megaValue: number) {

    let allowanceApproved: boolean = false;
    const addressFrom = this.currentPlayerWalletKey;
    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank
    );

    try {

      const gasPrice = await this.web3.eth.getGasPrice();
      const megaValueBN = this.convertToCoinNumber(megaValue);

      await testContract.methods.withdrawMega(megaValueBN)        
        .send({
          from: addressFrom,
          gasPrice: this.utils.toHex(gasPrice),
          gas: "100000"
        })
        .then((result) => {
          console.log('Withdrawal of mega from bank : ' + result);
          this.progressCaption = "Transaction Completed";
          this.contractWrite =  addressFrom + " Withdrawal "+ megaValue +" mega from bank";
          this.rotateActive = false;

          this.confirmTransaction(result.transactionHash);
        })
        .catch((error) => {
          console.log(error);
          this.progressCaption = "Error occured blocking Transaction";
          this.contractWrite = error;
          this.rotateActive = false;
        });

    }
    catch (error) {
      console.error(error);
      this.progressCaption = error;
      this.rotateActive = false;
    }
    return;    
  }
*/

  async depositMegaToMMBankWithAllowance(megaValue: number) {

    const addressFrom = this.currentPlayerWalletKey;
    const addressReceiver = this.CONTRACT_MMBank;
    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MegaCoinMOCKAbi,
      this.CONTRACT_MEGA
    );

    try {

      const gasPrice = await this.web3.eth.getGasPrice();

      await testContract.methods.increaseAllowance(addressReceiver, this.convertToCoinNumber(megaValue, 1))        
        .send({
          from: addressFrom,
          gasPrice: this.utils.toHex(gasPrice),
          gas: "100000"
        })
        .then((result) => {
            console.log('Allowance increased: ' + result);
            this.progressCaption = "Transaction Completed";
            this.contractWrite =  addressFrom + " Allowance to Bank increased by "+ megaValue;
            this.rotateActive = false;

            this.depositMegaToMMBank(megaValue);

          })
        .catch((error) => {
          console.log(error);
          this.progressCaption = "Error occured blocking Transaction";
          this.contractWrite = error;
          this.rotateActive = false;
        });

    }
    catch (error) {
      console.error(error);
      this.progressCaption = error;
      this.rotateActive = false;
    }
    return;    

  }

  async depositMegaToMMBank(megaValue: number) {

    const addressFrom = this.currentPlayerWalletKey;
    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank
    );

    try {

      const gasPrice = await this.web3.eth.getGasPrice();
      const megaValueBN = this.convertToCoinNumber(megaValue);

      await testContract.methods.depositMega(megaValueBN)        
        .send({
          from: addressFrom,
          gasPrice: this.utils.toHex(gasPrice),
          gas: "150000"
        })
        .then((result) => {
          console.log('Deposited mega to bank : ' + result);
          this.progressCaption = "Transaction Completed";
          this.contractWrite =  addressFrom + " Deposited "+ megaValue +" mega to bank";
          this.rotateActive = false;

          this.confirmTransaction(result.transactionHash);
        })
        .catch((error) => {
          console.log(error);
          this.progressCaption = "Error occured blocking Transaction";
          this.contractWrite = error;
          this.rotateActive = false;
        });

    }
    catch (error) {
      console.error(error);
      this.progressCaption = error;
      this.rotateActive = false;
    }
    return;    
  }

  async setMegaContract(addressMegaContract: string, addressFrom: string) {

    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    if (isAddress(addressMegaContract) == false) {
      this.progressCaption = "invalid Address parameter"
      return;
    }

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank
    );

    try {
      
      const gasPrice = await this.web3.eth.getGasPrice();
      const gasPriceHex = this.utils.toHex(gasPrice);

      await testContract.methods.setMegaContract(addressMegaContract)        
        .send({
          from: addressFrom,
          gasPrice: this.utils.toHex(gasPrice),
          gas: "100000"
        })
        .then((result) => {
            console.log('result: ' + result);
            this.progressCaption = "Transaction Completed";
            this.contractWrite =  addressMegaContract + " set as MegaContract";
            this.rotateActive = false;
          })
        .catch((error) => {
          console.log(error);
          this.progressCaption = "Error occured blocking Transaction";
          this.contractWrite = error;
          this.rotateActive = false;
        });

    }
    catch (error) {
      console.error(error);
      this.progressCaption = error;
      this.rotateActive = false;
    }
    return;    
  }

  async contractDepositBank(addressTo: string, addressFrom: string, amount:number) {

    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    if (isAddress(addressFrom) == false) {
      this.progressCaption = "invalid Address parameter"
      return;
    }

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank,
      this.web3
    );
    

    try {

      const gasPrice = await this.web3.eth.getGasPrice();

      await testContract.methods.depositBank(amount)        
        .send({
          from: addressFrom,
          gasPrice: this.utils.toHex(gasPrice),
          gas: "100000"
        })
        .then((result) => {
            console.log('result: ' + result);
            this.progressCaption = "Transaction Completed";
            this.contractWrite = amount + " deposited to Bank";
            this.rotateActive = false;
          })
        .catch((error) => {
          console.log(error);
          this.progressCaption = "Error occured blocking Transaction";
          this.contractWrite = error;
          this.rotateActive = false;
        });

    }
    catch (error) {
      console.error(error);
      this.progressCaption = error;
      this.rotateActive = false;
    }
    return;    
  }

  async contractTestSeedAccount(addressFrom: string) {
 
    const valuePara1 = 666;

    this.rotateActive = true;
    this.progressCaption = "Transaction in Progress"

    if (isAddress(addressFrom) == false) {
      this.progressCaption = "invalid Address parameter"
      return;
    }

    // Create a new contract object using the ABI and bytecode
    const testContract = new this.web3.eth.Contract(
      MMBankAbi,
      this.CONTRACT_MMBank,
      this.web3
    );

    try {

      let from_address = this.utils.padLeft(addressFrom, 64).substring(2);
      let token_uint256 = this.utils.padLeft(this.utils.toHex(valuePara1), 64).substring(2);
      const test2_uint256 = this.utils.toHex(123);
      const value_uint256 = this.utils.toBigInt(123);

      const gasPrice = await this.web3.eth.getGasPrice();

      await testContract.methods.seedAccount(4)        
        .send({
          from: addressFrom,
          gasPrice: this.utils.toHex(gasPrice),
          gas: "100000"
        })
        .then((result) => {
            console.log('result: ' + result);
            this.progressCaption = "Transaction Completed";
            this.contractWrite = "Seed value assigned to account "+addressFrom;
            this.rotateActive = false;
          })
        .catch((error) => {
          console.log(error);
          this.progressCaption = "Error occured blocking Transaction";
          this.contractWrite = error;
          this.rotateActive = false;
        });

    }
    catch (error) {
      console.error(error);
      this.progressCaption = error;
      this.rotateActive = false;
    }
    return;    
  }

  // Using : transferFrom(address src, address dst, uint256 rawAmount)
  // Note a 2nd transfer method used by MW : transferSender(address _sender, address _from, address _to, uint256 _tokenId) 
  async coinSendRequest(addressTo: string, addressFrom: string, coinAmount: string) {

    const provider = await DetectEthereumProvider();
    const ethereum = (window as any).ethereum;
    const MCPTransferMethod = "0x23b872dd";
    const MW_TRANSFER_ADDRESS = "0x4cc0c70a8a72f15bb43edfe252b07d3a4be4c252";
    const valuePara1 = 1;

    let web3: Web3 = null;

    // Check Metamask Provider :  Supporting Metamask & CoinbaseWallet
    if (await this.globals.checkApprovedWalletType()) {

      //console.log('Ethereum successfully detected!');
      this.rotateActive = true;
      this.progressCaption = "Transaction in Progress"

      try {
        web3 = new Web3(ethereum);
      }
      catch (error) {
        console.log("provider error: " + error);
        web3 = null;
      }

      if (web3 != null) {

        await web3.eth.sendTransaction({
          from: addressFrom,
          to: addressTo,
          value: this.utils.toWei(coinAmount, "ether"),     // only used when sending coin amount
          gas: 147100,                                      // Max gas for transaction
        })
          .on('transactionHash', (transactionHash) => {
            console.log('hash received - transaction not yet completed: ' + transactionHash);
            //this.recordTransaction(from_address, to_address, this.pack_unit_type, this.pack_unit_amount, 0, transactionHash, TRANSACTION_STATUS.PENDING, BLOCKCHAIN.POLYGON, TRANSACTION_TYPE.TRANSFER, this.pack_id)
          })
          .on('receipt', (receipt) => {
            console.log('receipt: ' + receipt);
          })
          .then((result) => {
            console.log('result: ' + result);
            this.progressCaption = coinAmount + " Coin sent to " + addressTo;
            this.rotateActive = false;
          })
          .catch((error) => {
            console.log(error);
            this.progressCaption = "Error occured blocking Transaction";
            this.rotateActive = false;
            // If the request fails, the Promise rejects with an error.
          });
      }

    }

  }

}
