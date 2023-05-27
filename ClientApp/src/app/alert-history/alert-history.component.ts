import { Component, Inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatBottomSheet, MatBottomSheetRef } from '@angular/material/bottom-sheet';
import { AlertBottomComponent } from '../alert-bottom/alert-bottom.component';
import { AlertCollection, AlertPendingManager, AlertPending, Globals, WORLD } from '../common/global-var';
import { ALERT_TYPE, ALERT_ICON_TYPE, PENDING_ALERT } from '../common/enum'

@Component({
  selector: 'app-alert-history',
  templateUrl: './alert-history.component.html',
  styleUrls: ['./alert-history.component.css']
})
export class AlertHistoryComponent {

  readonly ALERT_ICON_TYPE: typeof ALERT_ICON_TYPE = ALERT_ICON_TYPE;
  readonly ALERT_TYPE: typeof ALERT_TYPE = ALERT_TYPE;
  public alertCount: number = 0;
  private httpClient: HttpClient;
  private baseUrl: string;
  private bottomAlertRef: MatBottomSheetRef = null;
  private alertPendingManager: AlertPendingManager;  

  constructor(public globals: Globals, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string, private alertSheet: MatBottomSheet) {

    this.alertCount = globals.ownerAccount.alert_count;
    this.httpClient = http;
    this.baseUrl = rootBaseUrl + "api/" + globals.worldCode;

    this.alertPendingManager = {
      alert: null,
      manage: true
    };
  }


  showHistory() {

    var params = new HttpParams();
    var that = this;

    params = params.append('matic_key', this.globals.ownerAccount.matic_key);
    params = params.append('pending_alert', PENDING_ALERT.ALL);

    this.httpClient.get<AlertCollection>(this.baseUrl + '/OwnerData/GetPendingAlert', { params: params })
      .subscribe({
        next: (result) => {

          this.alertPendingManager.alert = result.alert;

          if (this.alertPendingManager.alert && this.alertPendingManager.alert.length > 0) {

            this.globals.ownerAccount.alert_count = result.historyCount;
            this.globals.newAlertSheetActive = false;   // flag that indicates if new alert sheet (checked at set intervals) is active - reset when showing full history

            // Close and remove existing bottom sheet
            if (that.bottomAlertRef != null) {
              that.bottomAlertRef.dismiss();
            }

            that.bottomAlertRef = that.alertSheet.open(AlertBottomComponent, {
              data: this.alertPendingManager,
            });

          }
        },
        error: (error) => {
          console.error("WARNING Account Alert Check ERROR : " + error);
        }
      });
  }
}
