import { Component, NgZone } from "@angular/core";
import { MatDialog } from "@angular/material/dialog";
import { Subscription } from "rxjs";
import { HttpClient, HttpParams } from "@angular/common/http";
import { BalanceManageDialogComponent } from "../balance-manage-dialog/balance-manage-dialog.component";
import { Globals, WORLD } from '../common/global-var';

@Component({
  selector: 'app-balance',
  templateUrl: './balance.component.html',
  styleUrls: ['./balance.component.css']
})
export class BalanceComponent{

  web3Active: boolean = false;
  httpClient: HttpClient;
  balance: number = 0;
  subscriptionAccountActive$: Subscription;
  subscriptionBalanceChange$: Subscription;
  currentPlayerWalletKey: string;


  constructor(public globals: Globals, private zone: NgZone, public dialog: MatDialog) {

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

  startBalanceMonitor() {

    this.currentPlayerWalletKey = this.globals.ownerAccount.public_key;
    this.balance = this.globals.ownerAccount.balance;

    // Monitor using service - when account status changes - active / inactive.
    this.subscriptionAccountActive$ = this.globals.accountActive$.subscribe(active => {

      // Update balance if active or inactive - incase of switching from active to inactive account
      this.currentPlayerWalletKey = this.globals.ownerAccount.public_key;
      console.log("account status : " + active);
      this.zone.run(() => {
        this.balance = this.globals.ownerAccount.balance;
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
    this.dialog.open(BalanceManageDialogComponent, {    
      width: '600px',
      enterAnimationDuration,
      exitAnimationDuration,
      autoFocus: 'false'
    });
  }
  
}
