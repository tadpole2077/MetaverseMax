import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, ChangeDetectorRef, Inject, OnDestroy, NgZone } from '@angular/core';
import { Subscription } from 'rxjs';
import { Router } from '@angular/router';
import { Application, WORLD, APPROVAL_TYPE, APPROVE_STATE } from '../common/global-var';


@Component({
    selector: 'app-account-approve',
    templateUrl: './account-approve.component.html',
    styleUrls: ['./account-approve.component.css']
})
export class AccountApproveComponent implements OnDestroy{

    httpClient: HttpClient;
    baseUrl: string;
    private ethPublicKey = '';
    showFlag = true;
    showTronLinkLoginFlag = false;
    showTronLinkNoPlotsFlag = false;
    showEthApproveFlag = false;
    showEthConnectFlag = false;
    approveSwitchSubscription$: Subscription;

    constructor(private zone: NgZone, private cdf: ChangeDetectorRef, public globals: Application, public router: Router, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

        this.httpClient = http;
        this.baseUrl = baseUrl;

        this.globals.approveSwitchComponent = this;
        this.showFlag = this.globals.requestApprove;

        this.approveSwitchSubscription$ = this.globals.approveSwitch$.subscribe( (switchState : APPROVE_STATE) => {

            if (switchState === APPROVE_STATE.SHOW) {
                this.show();
            }
            else if (switchState === APPROVE_STATE.HIDE) {
                this.hide();
            }
            else if (switchState === APPROVE_STATE.UPDATE) {
                this.update();
            }            
        });
    }

    ngOnDestroy() {

        if (this.approveSwitchSubscription$ ) {
            this.approveSwitchSubscription$.unsubscribe();
        }
    }

    async clickAccountLink() {

        if (this.globals.selectedWorld == WORLD.TRON) {
            this.globals.approveTronAccountLink();
        }
        else if (this.globals.selectedWorld == WORLD.BNB || this.globals.selectedWorld == WORLD.ETH) {
            this.globals.approveEthereumAccountLink();
        }

        return;
    }

    hide() {
        this.showFlag = false;
        this.showTronLinkLoginFlag = false;
        this.showEthApproveFlag = false;
        this.showTronLinkNoPlotsFlag = false;
        this.showEthConnectFlag = false;

        this.cdf.detectChanges();   // hide can be triggered out of render cycle - eg when metamask wallet log in
        return;
    }

    show() {
        this.showFlag = true;

        this.update();

        this.cdf.detectChanges();

        return;
    }

    update() {        

        this.zone.run(() => {

            if (this.globals.selectedWorld == WORLD.TRON) {

                this.showEthConnectFlag = false;
                this.showEthApproveFlag = false;

                if (this.globals.approvalType == APPROVAL_TYPE.NO_WALLET_ENABLED) {
                    this.showTronLinkNoPlotsFlag = false;
                    this.showTronLinkLoginFlag = true;
                }
                else if (this.globals.approvalType == APPROVAL_TYPE.ACCOUNT_WITH_NO_PLOTS) {
                    this.showTronLinkLoginFlag = false;
                    this.showTronLinkNoPlotsFlag = true;
                }
                else {  //Default
                    this.showTronLinkLoginFlag = true;
                    this.showTronLinkNoPlotsFlag = false;
                }

            }
            else if (this.globals.selectedWorld == WORLD.BNB || this.globals.selectedWorld == WORLD.ETH) {

                if (this.globals.requestApprove) {
                    this.showEthConnectFlag = false;
                    this.showEthApproveFlag = true;
                }
                else if (this.globals.connectWallet) {
                    this.showEthConnectFlag = true;
                    this.showEthApproveFlag = false;
                }

                this.showTronLinkLoginFlag = false;
            }

        });


    }

}
