import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject } from '@angular/core';
import { Injectable } from '@angular/core';
import { MatBottomSheet, MatBottomSheetRef } from '@angular/material/bottom-sheet';
import { Subscription, interval, Subject, async, Observable } from 'rxjs';
import { AlertBottomComponent } from '../alert-bottom/alert-bottom.component';
import { PENDING_ALERT, STATUS } from '../common/enum';
import { Application, JSend } from '../common/global-var';

interface AlertCollection {
  history_count: number,
  app_shutdown_warning_alert: boolean,
  alert: AlertPending[]
}

export interface AlertPending {
  alert_pending_key: number,
  last_updated: string,
  alert_message: string,
  alert_type: number,
  alert_id: number,
  icon_type: number,
  icon_type_change: number,
  trigger_active: boolean,  
}

export interface AlertPendingManager {
  manage: boolean,  
  alert: AlertPending[]
}


@Injectable({
    providedIn: 'root'
})
export class AlertManagerService {

    public subscriptionAlertInterval$: Subscription = null;
    public bottomAlertRef: MatBottomSheetRef = null;
    public manualFullActive = false;
    public autoAlertCheckProcessing = false;

    // Observable events exposed by this service - notifies observable subscribers
    private alertCount = new Subject<number>();
    public alertCount$ = this.alertCount.asObservable();

    private systemShutdownPending = new Subject<boolean>();
    public systemShutdownPending$ = this.systemShutdownPending.asObservable();

    constructor(private alertSheet: MatBottomSheet, private http: HttpClient, @Inject('BASE_URL') private baseUrl: string) {
    }

    // New subscription generated on account change.
    disableAlertChecker(){

        if (this.subscriptionAlertInterval$) {
            this.subscriptionAlertInterval$.unsubscribe();
            this.subscriptionAlertInterval$ = null;
        }

        return;
    }

    // converted to async due to service call and interval observable usage, promise to return alert flags.
    // Behaviours:  Need to support changing account, receiving alerts on a different account.
    enableAlertChecker = async (baseUrl: string, ownerMaticKey: string): Promise<boolean> => {        
        
        // Defensive coding - Dont run double subscription interval
        if (this.subscriptionAlertInterval$ !== null) {
            return;
        }

        // 3 min interval checking alerts
        this.subscriptionAlertInterval$ = interval(180000)
            .subscribe( async () => this.getAlerts(baseUrl, ownerMaticKey, PENDING_ALERT.UNREAD) );                                   

        return true;
        
    };

    getAlerts(baseUrl: string, ownerMaticKey: string, pendingAlert: PENDING_ALERT, manualFullActive = false): void {

        const alertPendingManager: AlertPendingManager = {
            alert: null,
            manage: false
        };
        let params = new HttpParams();

        params = params.append('matic_key', ownerMaticKey);
        params = params.append('pending_alert', pendingAlert);

        console.log('Account Alert Check : ' + new Date());

        // Skip interval check if full history is active and manually opened
        if (pendingAlert === PENDING_ALERT.UNREAD) {

            this.autoAlertCheckProcessing = true;       // Unread only alert only requested by automated alert checker.

            if (this.bottomAlertRef != null && this.manualFullActive === true) {
                console.log('Alert full History currently Open - skip new alert check : ' + new Date());
                return;
            }
        }

        this.http.get<JSend<AlertCollection>>(baseUrl + '/OwnerData/GetPendingAlert', { params: params })
            .subscribe({
                next: (result) => {
                    if (result.status == STATUS.SUCCESS) {

                        alertPendingManager.alert = result.data.alert;

                        // update observers, trigger notification to any subscribed features.
                        this.alertCount.next(result.data.history_count);
                        this.systemShutdownPending.next(result.data.app_shutdown_warning_alert);

                        this.manualFullActive = manualFullActive;

                        if (alertPendingManager.alert && alertPendingManager.alert.length > 0) {

                            this.bottomAlertRef = this.alertSheet
                                .open(AlertBottomComponent, {
                                    data: alertPendingManager,
                                });

                        }
                    }
                },
                error: (error) => {
                    console.error('WARNING Account Alert Check ERROR : ' + error);
                }
            });       
    }
}
