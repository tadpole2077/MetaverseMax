import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MatBottomSheet, MatBottomSheetRef } from '@angular/material/bottom-sheet';
import { Application } from '../common/global-var';
import { ALERT_TYPE, ALERT_ICON_TYPE, PENDING_ALERT } from '../common/enum';
import { AlertManagerService } from '../service/alert-manager.service';

@Component({
    selector: 'app-alert-history',
    templateUrl: './alert-history.component.html',
    styleUrls: ['./alert-history.component.css']
})
export class AlertHistoryComponent {

    readonly ALERT_ICON_TYPE: typeof ALERT_ICON_TYPE = ALERT_ICON_TYPE;
    readonly ALERT_TYPE: typeof ALERT_TYPE = ALERT_TYPE;
    //public alertCount = 0;
    private httpClient: HttpClient;
    private baseUrl: string;
    private bottomAlertRef: MatBottomSheetRef = null;

    constructor(public globals: Application, public alertManagerService: AlertManagerService, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string, private alertSheet: MatBottomSheet) {
        
        // Actions triggered by Signal Changes
        //effect(() => {
        //    this.alertCount = this.alertManagerService.alertCount();
        //});
        //this.alertCount = globals.ownerAccount.alert_count;

        this.httpClient = http;
        this.baseUrl = rootBaseUrl + 'api/' + globals.worldCode;

    }


    showHistory(event: Event) {
        
        // Close and remove existing bottom sheet
        if (this.alertManagerService.bottomAlertRef != null) {
            this.alertManagerService.bottomAlertRef.dismiss();
        }

        this.alertManagerService.getAlerts(this.baseUrl, this.globals.ownerAccount.matic_key, PENDING_ALERT.ALL, true);


    }
}
