import { Component, Inject, ViewChild } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatBottomSheet, MatBottomSheetRef, MAT_BOTTOM_SHEET_DATA } from '@angular/material/bottom-sheet';
import { Alert } from '../common/alert';
import { JSend, AlertCollection, AlertPending, AlertPendingManager, Application } from '../common/global-var';
import { ALERT_TYPE, ALERT_ICON_TYPE, ICON_TYPE_CHANGE, ALERT_ACTION } from '../common/enum';
import { Subscription } from 'rxjs';

@Component({
    selector: 'app-alert-bottom',
    templateUrl: './alert-bottom.component.html',
    styleUrls: ['./alert-bottom.component.css']
})
export class AlertBottomComponent{

    readonly ALERT_TYPE: typeof ALERT_TYPE = ALERT_TYPE;    // expose enum to view attributes
    readonly ALERT_ICON_TYPE: typeof ALERT_ICON_TYPE = ALERT_ICON_TYPE;
    readonly ICON_TYPE_CHANGE: typeof ICON_TYPE_CHANGE = ICON_TYPE_CHANGE;
    public managerEnabled = false;
    private httpClient: HttpClient;
    private baseUrl: string;
    public alertPendingManager: AlertPendingManager;
    afterDismissSubscription: Subscription = null;

    // only one bottom sheet can be open at a time, use the Ref to close the currently opened sheet
    constructor(private bottomSheetRef: MatBottomSheetRef<AlertBottomComponent>, @Inject(MAT_BOTTOM_SHEET_DATA) public callerAlertPendingManager: AlertPendingManager, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string, public globals: Application, public alert: Alert) {

        this.managerEnabled = callerAlertPendingManager.manage;
        this.alertPendingManager = callerAlertPendingManager;
        this.httpClient = http;
        this.baseUrl = rootBaseUrl + 'api/' + globals.worldCode;
        let firstTimeSheetShown = false;

        if (this.globals.bottomAlertRef === null) {
            firstTimeSheetShown = true;
        }
        this.globals.bottomAlertRef = bottomSheetRef;   

        // Observable notified after bottomsheet closes
        this.afterDismissSubscription = bottomSheetRef.afterDismissed().subscribe(() => {

            let params = new HttpParams();

            // On close of New alerts sheet - Mark active alerts as read/seen - dont show in next [New alerts interval check]
            params = params.append('matic_key', this.globals.ownerAccount.matic_key);
            console.log('Alert vars (a) manualFullActive : ' + this.globals.manualFullActive + ' (b)autoAlertCheckProcessing : ' + this.globals.autoAlertCheckProcessing + ' : ' + new Date());      

            if (this.globals.manualFullActive == true || this.globals.autoAlertCheckProcessing == false) {

                this.globals.manualFullActive = false;
                this.globals.bottomAlertRef = null;                 // Release to reset for a new auto check cycle

                this.httpClient.get<any>(this.baseUrl + '/OwnerData/UpdateRead', { params: params })
                    .subscribe({
                        next: (result) => {
                            console.log('All alerts marked as Viewed/Read');

                        },
                        error: (error) => {
                            console.error('WARNING Account Alert Update ERROR : ' + error);
                        }
                    });
            }

            this.globals.autoAlertCheckProcessing = false;      // Flag used to identify a manual close of autocheck alerts - set to true during autocheck process      
            this.afterDismissSubscription.unsubscribe();        // afterDismissed() event is invoked after ngOnDestory() so need to unsubscribe here.
        });

        // Corner case - Need to capture if user manually closes sheet within 3 minutes of first sheet popup showing - in this case there was no prior sheet to close to reset the processing flag in the observable.
        if (firstTimeSheetShown) {
            this.globals.autoAlertCheckProcessing = false; 
        }
    }

    markDelete(event: MouseEvent, alertKey: number, alertIndex: number): void {       

        let params = new HttpParams();

        params = params.append('matic_key', this.globals.ownerAccount.matic_key);
        params = params.append('alert_pending_key', alertKey);

        this.httpClient.get<JSend<boolean>>(this.baseUrl + '/OwnerData/DeletePendingAlert', { params: params })
            .subscribe({
                next: (result) => {

                    if (result.data) {
                        // Remove element
                        delete this.alertPendingManager.alert[alertIndex];
                        // Reindex array
                        this.alertPendingManager.alert = this.alertPendingManager.alert.filter(_ => true);

                        this.globals.ownerAccount.alert_count = this.alertPendingManager.alert.length;
                    }

                    if (this.alertPendingManager.alert && this.alertPendingManager.alert.length === 0) {
                        this.bottomSheetRef.dismiss();
                        event.preventDefault();
                    }
                    else {
                        event.stopPropagation();
                    }

                },
                error: (error) => {
                    console.error('WARNING Account Alert Check ERROR : ' + error);
                }
            });

        event.stopPropagation();    
    }

    markRead(event: MouseEvent): void {
        this.bottomSheetRef.dismiss();
        event.preventDefault();
    }

    alertChange(event: MouseEvent, alert: AlertPending) {

        // update db - WS call    
        this.alert.updateAlert(this.globals.ownerAccount.matic_key, alert.alert_type, alert.alert_id, alert.trigger_active ? ALERT_ACTION.REMOVE : ALERT_ACTION.ADD);
        alert.trigger_active = !alert.trigger_active;

        this.alertPendingManager.alert.forEach(a => {

            // New Building Alert - store the image id, but the alert trigger used to generate them is generic for all new buildings.
            if (alert.alert_type === ALERT_TYPE.NEW_BUILDING && a.alert_type === alert.alert_type) {
                a.trigger_active = alert.trigger_active;
            }
            else if (a.alert_type === alert.alert_type && a.alert_id === alert.alert_id) {
                a.trigger_active = alert.trigger_active;
            }

        });
    
        event.preventDefault();
        event.stopPropagation();
    }

    getBuildingImg(buildingId: number) {
        return 'https://builder.megaworld.io/preview/' + Math.trunc(buildingId / 100) + '/' + buildingId + '.png';
    }
}
