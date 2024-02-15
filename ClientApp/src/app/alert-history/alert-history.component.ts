import { Component, Inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatBottomSheet, MatBottomSheetRef } from '@angular/material/bottom-sheet';
import { AlertBottomComponent } from '../alert-bottom/alert-bottom.component';
import { JSend, AlertCollection, AlertPendingManager, AlertPending, Globals, WORLD } from '../common/global-var';
import { ALERT_TYPE, ALERT_ICON_TYPE, PENDING_ALERT, STATUS } from '../common/enum'

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


  showHistory(event: Event) {

    var params = new HttpParams();
    var that = this;

    params = params.append('matic_key', this.globals.ownerAccount.matic_key);
    params = params.append('pending_alert', PENDING_ALERT.ALL);

    this.httpClient.get<JSend<AlertCollection>>(this.baseUrl + '/OwnerData/GetPendingAlert', { params: params })
      .subscribe({
        next: (result) => {
          if (result.status == STATUS.SUCCESS) {
            this.alertPendingManager.alert = result.data.alert;

            if (this.alertPendingManager.alert && this.alertPendingManager.alert.length > 0) {

              this.globals.ownerAccount.alert_count = result.data.history_count;
              this.globals.systemShutdownPending = result.data.app_shutdown_warning_alert;

              // Close and remove existing bottom sheet
              if (that.bottomAlertRef != null) {
                that.bottomAlertRef.dismiss();
              }

              that.bottomAlertRef = that.alertSheet.open(AlertBottomComponent, {
                data: this.alertPendingManager,
              });

              this.globals.manualFullActive = true;   // flag that indicates if new alert sheet (checked at set intervals) is active - reset when showing full history

            }
          }
        },
        error: (error) => {
          console.error("WARNING Account Alert Check ERROR : " + error);
        }
      });
  }
}
