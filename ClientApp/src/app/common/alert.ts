import { Injectable, Inject} from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatSlideToggle, MatSlideToggleChange } from '@angular/material/slide-toggle';

import { Globals, WORLD } from '../common/global-var';
import { ALERT_TYPE, ALERT_ACTION } from '../common/enum'

interface OwnerAlert {
  key_type: number
}

@Injectable()
export class Alert {

  baseUrl: string;
  private ownerAlert: OwnerAlert[];

  constructor(public globals: Globals, private httpClient: HttpClient, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string) {
    this.httpClient = http;    
  }

  updateAlert(maticKey: string, alertType: number, id: number, action: number) {

    let params = new HttpParams();
    params = params.append('matic_key', maticKey);
    params = params.append('alert_type', alertType);
    params = params.append('id', id);
    params = params.append('action', action)

    this.baseUrl = this.rootBaseUrl + "api/" + this.globals.worldCode;      // needs to be refreshed - as class initated once and injected, may reflect prior world

    if (this.globals.ownerAccount.wallet_active_in_world) {

      this.httpClient.get<Object>(this.baseUrl + '/OwnerData/UpdateOwnerAlert', { params: params })
        .subscribe({
          next: (result) => {
          },
          error: (error) => { console.error(error) }
        });

    }

    return;
  }

  getAlertSingle(districtId: number, alertType: number, alertSlider: MatSlideToggle = null) {

    let params = new HttpParams();
    params = params.append('matic_key', this.globals.ownerAccount.matic_key);
    params = params.append('alert_type', alertType);

    this.baseUrl = this.rootBaseUrl + "api/" + this.globals.worldCode;      // needs to be refreshed - as class initated once and injected, may reflect prior world

    if (this.globals.ownerAccount.wallet_active_in_world) {

      this.httpClient.get<OwnerAlert[]>(this.baseUrl + '/OwnerData/GetAlertSingle', { params: params })
        .subscribe({
          next: (result) => {

            this.ownerAlert = result;

            for (var index = 0; index < (this.ownerAlert == null ? 0 : this.ownerAlert.length); index++) {

              if (this.ownerAlert[index].key_type == alertType) {
                if (alertSlider) {
                  alertSlider.checked = true;
                }
              }              

            }
          },
          error: (error) => { console.error(error) }
        });

    }
    return;
  }

}
