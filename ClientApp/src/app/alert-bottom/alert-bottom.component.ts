import { Component, Inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatBottomSheet, MatBottomSheetRef, MAT_BOTTOM_SHEET_DATA } from '@angular/material/bottom-sheet';
import { AlertCollection, AlertPending, AlertPendingManager, Globals, WORLD } from '../common/global-var';
import { ALERT_TYPE, ALERT_ICON_TYPE, ICON_TYPE_CHANGE, PENDING_ALERT } from '../common/enum'

@Component({
  selector: 'app-alert-bottom',
  templateUrl: './alert-bottom.component.html',
  styleUrls: ['./alert-bottom.component.css']
})
export class AlertBottomComponent{

  readonly ALERT_ICON_TYPE: typeof ALERT_ICON_TYPE = ALERT_ICON_TYPE;
  readonly ICON_TYPE_CHANGE: typeof ICON_TYPE_CHANGE = ICON_TYPE_CHANGE;
  public managerEnabled: boolean = false;
  private httpClient: HttpClient;
  private baseUrl: string;
  public alertPendingManager: AlertPendingManager;

  // only one bottom sheet can be open at a time, use the Ref to close the currently opened sheet
  constructor(private bottomSheetRef: MatBottomSheetRef<AlertBottomComponent>, @Inject(MAT_BOTTOM_SHEET_DATA) public callerAlertPendingManager: AlertPendingManager, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string, public globals: Globals) {

    this.managerEnabled = callerAlertPendingManager.manage;
    this.alertPendingManager = callerAlertPendingManager;
    this.httpClient = http;
    this.baseUrl = rootBaseUrl + "api/" + globals.worldCode;
    this.globals.bottomAlertRef = bottomSheetRef;

    // Observable notified after bottomsheet closes
    bottomSheetRef.afterDismissed().subscribe(() => {

      let params = new HttpParams();
      this.globals.bottomAlertRef = null;     // Used by alert interval service - to check if alert bottomSheet already open - then either update if new alerts, or leave as is if showing history.

      // On close of New alerts sheet - Mark active alerts as read/seen - dont show in next [New alerts interval check]
      params = params.append('matic_key', this.globals.ownerAccount.matic_key);

      if (this.globals.newAlertSheetActive == true) {

        this.httpClient.get<any>(this.baseUrl + '/OwnerData/UpdateRead', { params: params })
          .subscribe({
            next: (result) => {

            },
            error: (error) => {
              console.error("WARNING Account Alert Update ERROR : " + error);
            }
          });
      }
    });
  }

  markDelete(event: MouseEvent, alertKey: number, alertIndex: number): void {       

    var params = new HttpParams();

    params = params.append('matic_key', this.globals.ownerAccount.matic_key);
    params = params.append('alert_pending_key', alertKey);

    this.httpClient.get<AlertCollection>(this.baseUrl + '/OwnerData/DeletePendingAlert', { params: params })
      .subscribe({
        next: (result) => {

          // Remove element
          delete this.alertPendingManager.alert[alertIndex];
          // Reindex array
          this.alertPendingManager.alert = this.alertPendingManager.alert.filter(_ => true);

          this.globals.ownerAccount.alert_count = result.historyCount;

          if (this.alertPendingManager.alert && this.alertPendingManager.alert.length == 0) {
            this.bottomSheetRef.dismiss();
            event.preventDefault();
          }
          else {
            event.stopPropagation();
          }

        },
        error: (error) => {
          console.error("WARNING Account Alert Check ERROR : " + error);
        }
      });

    event.stopPropagation();    
  }

  markRead(event: MouseEvent): void {
    this.bottomSheetRef.dismiss();
    event.preventDefault();
  }
}
