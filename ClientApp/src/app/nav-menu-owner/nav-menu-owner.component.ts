import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, Output, EventEmitter, Inject, ViewChild, NgZone } from '@angular/core';
import { NgbDropdown } from '@ng-bootstrap/ng-bootstrap';
import { Router } from '@angular/router';
import { DirectDepositDialogComponent } from '../direct-deposit-dialog/direct-deposit-dialog.component';
import { Application, WORLD } from '../common/global-var';
import { ThemePalette } from '@angular/material/core';
import { MatSlideToggleChange } from '@angular/material/slide-toggle';
import { MatDialog } from '@angular/material/dialog';


@Component({
    selector: 'app-nav-menu-owner',
    templateUrl: './nav-menu-owner.component.html',
    styleUrls: ['./nav-menu-owner.component.css']
})
export class NavMenuOwnerComponent {

    readonly WORLD = WORLD;
    color: ThemePalette = 'accent';
    checked = false;
    hoverIcon = false;
    hoverDropdown = false;
    selectedWorld;

    httpClient: HttpClient;
    baseUrl: string;

    @ViewChild(NgbDropdown, { static: true }) ownerDropdown: NgbDropdown;
    @Output() darkModeChangeEvent = new EventEmitter<boolean>();

    constructor(private zone: NgZone, public globals: Application, public router: Router, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string, public dialog: MatDialog) {

        this.httpClient = http;
        this.baseUrl = rootBaseUrl + 'api/' + globals.worldCode;

    }

    openDialog(enterAnimationDuration: string, exitAnimationDuration: string): void {
        //this.dialog.open(BalanceManageDialogComponent, {
        this.dialog.open(DirectDepositDialogComponent, {
            width: '600px',
            enterAnimationDuration,
            exitAnimationDuration,
        });
    }

    // to avoid a "Navigation triggered outside Angular zone" error, due to newly rendered links from wallet site link, need to run navigation within zone.run()
    navMyPortfolio() {
        this.zone.run(() => {
            this.router.navigate(['/', this.globals.worldCode, 'owner-data'], { queryParams: { matic: 'myportfolio' }, });
        });
    }

    darkModeChange(eventSlider: MatSlideToggleChange) {

        // needs to be updated again - due to possible change in world since component was initially loaded.
        this.baseUrl = this.rootBaseUrl + 'api/' + this.globals.worldCode;

        // bubble event up to parent component to trigger theme change
        this.darkModeChangeEvent.emit(eventSlider.checked);

        // update db - WS call    
        this.updateDarkMode(this.globals.ownerAccount, eventSlider.checked);

        this.globals.ownerAccount.dark_mode = eventSlider.checked;
    }

    mmBalanceChange(eventSlider: MatSlideToggleChange) {

        // needs to be updated again - due to possible change in world since component was initially loaded.
        this.baseUrl = this.rootBaseUrl + 'api/' + this.globals.worldCode;

        // update db - WS call    
        this.updateBalanceVisible(this.globals.ownerAccount.matic_key, eventSlider.checked);
    }


    // Purely an example of using generic extends - to reference a matic_key field from any passed type.
    updateDarkMode<T extends {matic_key:string}>(account: T, darkMode: boolean) {

        let params = new HttpParams();
        params = params.append('owner_matic_key', account.matic_key);
        params = params.append('dark_mode', darkMode ? 'true':'false');

        if (this.globals.ownerAccount.wallet_active_in_world) {

            this.httpClient.get<boolean>(this.baseUrl + '/OwnerData/SetDarkMode', { params: params })
                .subscribe({
                    next: (result) => {
                        if (!result) {
                            console.error('Unable to store darkmode preference');
                        }
                    },
                    error: (error) => { console.error(error); }
                });
        }
        else {
            console.warn('Anomoly Identified: User action change darkmode triggered but account is not active');
        }

        return;
    }

    updateBalanceVisible(maticKey: string, visible: boolean) {

        let params = new HttpParams();
        params = params.append('owner_matic_key', maticKey);
        params = params.append('balance_visible', visible ? 'true':'false');

        if (this.globals.ownerAccount.wallet_active_in_world) {

            this.httpClient.get<boolean>(this.baseUrl + '/OwnerData/SetBalanceVisible', { params: params })
                .subscribe({
                    next: () => {
                        this.zone.run(() => {
                            this.globals.ownerAccount.balance_visible = visible;
                        });
                    },
                    error: (error) => { console.error(error); }
                });
        }

        return;
    }

    onHoverShow(event, controlId: number): void {
        this.ownerDropdown.open();
        this.hoverIcon = controlId == 1;
        this.hoverDropdown = controlId == 2;
    }
    onHoverHide(event, callingControl): void {

        if ((this.hoverIcon == false && callingControl == 2) || (this.hoverDropdown == false && callingControl == 1) ) {
            this.ownerDropdown.close();
        }
    }
}
