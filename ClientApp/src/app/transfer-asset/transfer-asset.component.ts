import { Component, Output, Input, EventEmitter, ViewChild, Inject, OnInit } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import Web3 from 'web3';
import DetectEthereumProvider from '@metamask/detect-provider';

import { PlayerMenuComponent } from '../player-menu/player-menu.component';
import { IPack } from '../owner-data/owner-interface';
import { Application, WORLD } from '../common/global-var';
import { TRANSACTION_STATUS, BLOCKCHAIN, TRANSACTION_TYPE, PRODUCT } from '../common/enum';
import { debounceTime, distinctUntilChanged } from 'rxjs';

const ETHEREUM_ID_HEX = '0x4';
const POLYGON_ID_HEX = '0x89';
const ETHEREUM_ID = '0x4';
const POLYGON_ID = '0x89';

const NETWORKS_DESC = {
    1: 'Ethereum Main Network',
    3: 'Ropsten Test Network',
    4: 'Rinkeby Test Network',
    5: 'Goerli Test Network',
    42: 'Kovan Test Network',
    56: 'Binance Smart Chain',
    1337: 'Ganache',
};
const NETWORKS = {
    ETHEREUM: '0x1',
    POLYGON: '0x89'

};


@Component({
    selector: 'app-transfer-asset',
    templateUrl: './transfer-asset.component.html',
    styleUrls: ['./transfer-asset.component.css']
})
export class TransferAssetComponent implements OnInit {

  @Input() index: number;
  @Input() pack_id: number;
  @Input() pack_unit_type: number;
  @Input() pack_unit_amount: number;
  @ViewChild(PlayerMenuComponent, { static: true }) playerMenu: PlayerMenuComponent;

  //@Output() searchPlotEvent = new EventEmitter<any>();
  public rotateActive = false;
  public addressText: string;
  public progressCaption = '';
  public debounce = 400;
  httpClient: HttpClient;
  baseUrl: string;
  addressKeyControl = new FormControl('');
  
  constructor(public globals: Application, public router: Router, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string) {

      this.httpClient = http;
      this.baseUrl = rootBaseUrl + 'api/' + globals.worldCode;    

  }

  ngOnInit() {

      // On Change of Input 
      this.addressKeyControl.valueChanges
          .pipe(debounceTime(this.debounce), distinctUntilChanged())
          .subscribe(playerKey => {

              console.log(playerKey);
              // Child component - handles wallet key change event - attempting to match a known user account.
              this.playerMenu.matchKey(playerKey);

          });

  }

  selectPlayer(playerPublicKey: string) {
      const x = playerPublicKey;
  }

  resetPlayerList() {
      this.playerMenu.clearPlayerName();
  }

  async transferSendMatic(addressTo: string) {

      const ethereum = (window as any).ethereum;
      let chainIdHex: string;

      if (ethereum && ethereum.isConnected()) {

          chainIdHex = await ethereum.request({ method: 'eth_chainId', params: [] });
          const chainIdNumber = parseInt(chainIdHex, 16); // convert to decimal

          console.log('Current Chain : ' + chainIdHex +' - '+ chainIdNumber);
          if (chainIdHex == NETWORKS.ETHEREUM) {
              console.log('Selected chain is Ethereum main-net, Request to switch to Polygon.');
          }

          if (chainIdHex != NETWORKS.POLYGON) {
              this.progressCaption = 'Request to switch to Polygon';
              await this.switchNetwork(NETWORKS.POLYGON);        
              this.progressCaption = '';
              //console.log("Switched chain to Eth");
              //this.swithNetwork(POLYGON_ID_HEX);
          }


          chainIdHex = await ethereum.request({ method: 'eth_chainId', params: [] });
          if (chainIdHex == NETWORKS.POLYGON) {
              await this.transferSendRequest(addressTo, this.globals.ownerAccount.public_key);
          }
      }    

  }

  // Using : transferFrom(address src, address dst, uint256 rawAmount)
  // Note a 2nd transfer method used by MW : transferSender(address _sender, address _from, address _to, uint256 _tokenId) 
  async transferSendRequest(addressTo: string, addressFrom: string) {

      const provider = await DetectEthereumProvider();
      const ethereum = (window as any).ethereum;
      const MCPTransferMethod = '0x23b872dd';
      const MW_TRANSFER_ADDRESS = '0x4cc0c70a8a72f15bb43edfe252b07d3a4be4c252';

      let web3: Web3 = null;

      // Check Metamask Provider :  Supporting Metamask & CoinbaseWallet
      if (await this.globals.checkApprovedWalletType()) {

          console.log('Ethereum successfully detected!');
          this.rotateActive = true;

          try {
              web3 = new Web3(ethereum);
          }
          catch (error) {
              console.log('provider error: ' + error);
              web3 = null;
          }

          if (web3 != null) {

              const from_address = web3.utils.padLeft(addressFrom.toLowerCase(), 64).substring(2);
              const to_address = web3.utils.padLeft(addressTo.toLowerCase(), 64).substring(2);
              const token_uint256 = web3.utils.padLeft(web3.utils.toHex(this.pack_id), 64).substring(2);
              const tranferData = 
          MCPTransferMethod +
          from_address +
          to_address +
          token_uint256;

              await web3.eth.sendTransaction({
                  from: addressFrom,
                  to: MW_TRANSFER_ADDRESS,
                  //value: web3.utils.toWei(".01", "ether"),
                  gas: 147100,         // Max gas for transaction
                  data: tranferData
              })
                  .on('transactionHash', (transactionHash) => {
                      console.log('hash received - transaction not yet completed: ' + transactionHash);
                      this.recordTransaction(from_address, to_address, this.pack_unit_type, this.pack_unit_amount, 0, transactionHash, TRANSACTION_STATUS.PENDING, BLOCKCHAIN.POLYGON, TRANSACTION_TYPE.TRANSFER, this.pack_id);
                  })
                  .on('receipt', (receipt) => {
                      console.log('receipt: ' + receipt);
                  })
                  .then((result) => {
                      console.log('result: ' + result);
                      this.progressCaption = '';
                      this.rotateActive = false;
                  })
                  .catch((error) => {
                      console.log(error);
                      this.progressCaption = 'Error occured blocking Transaction';
                      this.rotateActive = false;
                      // If the request fails, the Promise rejects with an error.
                  });
          }

      }

  }


  async transferSendRequestMatic(addressTo: string, addressFrom: string) {

      const provider = await DetectEthereumProvider();
      const ethereum = (window as any).ethereum;
      const GWEI200 = (200).toString(16); // convert to Hex
      const TRANSFER_BASE_FEE_WEI = 21000;
      const MAX_PRIORITY_30 = (30).toString(16); // convert to Hex
      const chainIdHex = await ethereum.request({ method: 'eth_chainId', params: [] });
      const MW_TRANSFER_ADDRESS = '0x4cc0c70a8a72f15bb43edfe252b07d3a4be4c252';
      let web3: Web3 = null;
      const that = this;
      let gasPrice: any;
    
      //const web3 = new Web3(new Web3.providers.HttpProvider('https://methodical-hidden-asphalt.matic.discover.quiknode.pro/f6aa53ca67f11b7b0192a1b89eaf6c99f62d7ed4/'));

      // Example of Polygon Package Transfer : https://polygonscan.com/tx/0x161c548ab08675f8d7a8244b1d215b66fe78794d321fc93b1902458a211a51d7
      // MW Package Polygon Contract : https://polygonscan.com/address/0x4cc0c70a8a72f15bb43edfe252b07d3a4be4c252
      // Method transferSender(address _sender, address _from, address _to, uint256 _tokenId)
    

      // Check Metamask Provider :  Supporting Metamask & CoinbaseWallet
      if (await this.globals.checkApprovedWalletType()) {

          console.log('Ethereum successfully detected!');
          this.rotateActive = true;

          try {
              //let test = new Web3.providers.HttpProvider('https://methodical-hidden-asphalt.matic.discover.quiknode.pro/f6aa53ca67f11b7b0192a1b89eaf6c99f62d7ed4/');
              //web3 = new Web3(new Web3.providers.HttpProvider('https://methodical-hidden-asphalt.matic.discover.quiknode.pro/f6aa53ca67f11b7b0192a1b89eaf6c99f62d7ed4/'));
              //web3 = new Web3(new Web3.providers.HttpProvider(ethereum));
              web3 = new Web3(ethereum);
          }
          catch (error) {
              console.log('provider error: ' + error);
              web3 = null;
          }


          if (web3 != null) {

              /*const fs = require('fs');
        const web3 = new Web3("https://polygon-rpc.com");
        const contractAddress = MW_TRANSFER_ADDRESS;
        const contractJson = fs.readFileSync('./abi.json');
        const abi = JSON.parse(contractJson);
        const mSandMaticContract = new web3.eth.Contract(abi, contractAddress)
        

        var encoded = mSandMaticContract.methods.getReward().encodeABI()
        var block = await web3.eth.getBlock("latest");
        var gasLimit = Math.round(block.gasLimit / block.transactions.length);

        var tx = {
          gas: "300",
          to: addressFrom,
          data: encoded
        }
        */

              //web3.eth.accounts.signTransaction(tx, privateKey).then(signed => {
              //  web3.eth.sendSignedTransaction(signed.rawTransaction).on('receipt', console.log)
              //})


              const currentBlockNumber = await web3.eth.getBlockNumber();
              console.log('Current block number:', currentBlockNumber);

              await web3.eth.getGasPrice()
                  .then((result) => {
                      gasPrice = result;
                  });

              console.log('Current price for one gas unit in WEI :', gasPrice);
              //console.log('Current price for one gas unit in WEI :', gasPrice);
              this.progressCaption = 'Transaction sign pending';

              await web3.eth.sendTransaction({
                  from: this.globals.ownerAccount.public_key,
                  to: addressTo,
                  value: web3.utils.toWei('.01', 'ether'),
                  gas: 25000,         // Max gas for transaction     
              })
                  .on('receipt', (receipt) => {
                      console.log('receipt: ' + receipt);
                  })
                  .then((result) => {
                      console.log('result: ' + result);
                      this.progressCaption = '';
                  })                        
                  .catch((error) => {
                      console.log(error);
                      this.progressCaption = 'Error occured blocking Transaction';
                      // If the request fails, the Promise rejects with an error.
                  });


              /*

        let params = [
          {
            _sender: this.globals.ownerAccount.public_key,
            _from: this.globals.ownerAccount.public_key,
            _to: addressTo,
            _tokenId: this.pack_id,
            gas: '0x' + GWEI200,              // 30400    - Customisable by uesr  - 200 GWEI  average for Matic.
            maxPriorityFeePerGas: '0x' + MAX_PRIORITY_30,
            // gasPrice: '0x9184e72a000',  // 10000000000000     - Customisable by user.
            //value: '0x9184e72a',        // example 2441406250 - Only required to send X of a token

            // Data used to specify contract methods and parameters - 
            data:
              '0xd46e8dd67c5d32be8d46e8dd67c5d32be8058bb8eb970870f072445675058bb8eb970870f072445675',
          },
        ];

        ethereum
          .request({
            method: 'transferSender',
            params,
          })
          .then((result) => {
            // The result varies by RPC method.
            // For example, this method returns a transaction hash hexadecimal string upon success.


            // Switch network back to Eth
            if (chainIdHex == NETWORKS.POLYGON) {
              console.log("Selected chain is matic-polygon");
              this.switchNetwork(NETWORKS.ETHEREUM);
              console.log("Switched chain to Eth");
              //this.swithNetwork(POLYGON_ID_HEX);
            }


          })
          .catch((error) => {
            // If the request fails, the Promise rejects with an error.
          });
        */
          }
      }

      this.rotateActive = false;
  }

  async switchNetwork(chainIdHex:string) {

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
                  if (switchError.code === 4902 && chainIdHex == NETWORKS.POLYGON) {
                      // Add Polygon chain
                      await ethereum.request({
                          method: 'wallet_addEthereumChain',
                          params: [
                              {
                                  chainId: NETWORKS.POLYGON,
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

              }
              catch (error) {
                  // user rejects the request to "add chain" or param values are wrong, maybe you didn't use hex above for `chainId`?
                  console.log('wallet_addEthereumChain Error: ${error.message}');
              }
          }

          return networkSwitched;
      }

  }

  public recordTransaction(fromKey: string, toKey: string, unitType: number, unitAmt: number, value: number, hash: string, status: number, blockchain: number, transactionType: number, tokenId: number) {

      let params = new HttpParams();
      params = params.append('from_wallet', fromKey);
      params = params.append('to_wallet', toKey);
      params = params.append('unit_type', unitType);
      params = params.append('unit_amount', unitAmt);
      params = params.append('value', value);
      params = params.append('hash', hash);
      params = params.append('status', status);
      params = params.append('blockchain', blockchain);
      params = params.append('transaction_type', transactionType);
      params = params.append('token_id', tokenId);


      this.httpClient.get<number>(this.baseUrl + '/transaction/log', { params: params })
          .subscribe({
              next: (result) => {          

              },
              error: (error) => { console.error(error); }
          });

      return;
  }
}
