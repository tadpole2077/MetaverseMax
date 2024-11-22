import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { Application, WORLD, APPROVE_STATE, PROMPT_TYPE } from '../common/global-var';


@Component({
    selector: 'app-account-approve',
    templateUrl: './account-approve.component.html',
    styleUrls: ['./account-approve.component.css']
})
export class AccountApproveComponent{

    readonly APPROVE_STATE = APPROVE_STATE;     // Expose type to template.
    readonly PROMPT_TYPE = PROMPT_TYPE;

    httpClient: HttpClient;
    baseUrl: string;
    private ethPublicKey = '';
   

    constructor( public app: Application, public router: Router,) {        

        console.log('Approve state : ', this.app.approveSwitch());

    }

    async clickAccountLink() {

        if (this.app.selectedWorld == WORLD.TRON) {
            this.app.approveTronAccountLink();
        }
        else if (this.app.selectedWorld == WORLD.BNB || this.app.selectedWorld == WORLD.ETH) {
            this.app.approveEthereumAccountLink();
        }

        return;
    }

}
