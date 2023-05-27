import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, Output, EventEmitter, Inject, ViewChild, Input } from '@angular/core';
import { Router } from '@angular/router';
import { Globals, WORLD } from '../common/global-var';
import { MatSlideToggle, MatSlideToggleChange } from '@angular/material/slide-toggle';
import { ALERT_TYPE, ALERT_ACTION } from '../common/enum'

interface OwnerAlert {
  key_type: number
}

@Component({
  selector: 'app-alert-menu',
  templateUrl: './alert-menu.component.html',
  styleUrls: ['./alert-menu.component.css']
})
export class AlertMenuComponent {

  readonly ALERT_TYPE: typeof ALERT_TYPE = ALERT_TYPE;
  checked = false;

  httpClient: HttpClient;
  baseUrl: string;
  ownerAlert: OwnerAlert[];

  @ViewChild("ilvSlide", { static: true } as any) ilvSlide: MatSlideToggle;
  @ViewChild("constructSlide", { static: true } as any) constructSlide: MatSlideToggle;
  @ViewChild("produceSlide", { static: true } as any) produceSlide: MatSlideToggle;
  @ViewChild("distributeSlide", { static: true } as any) distributeSlide: MatSlideToggle;

  @Input() districtId: number;

  constructor(public globals: Globals, public router: Router, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string) {

    this.httpClient = http;
    this.baseUrl = rootBaseUrl + "api/" + globals.worldCode;
  }


  alertChange(eventSlider: MatSlideToggleChange, alertType:number) {

    // update db - WS call    
    this.updateAlert(this.globals.ownerAccount.matic_key, alertType, this.districtId, eventSlider.checked == true ? ALERT_ACTION.ADD : ALERT_ACTION.REMOVE);

  }

  updateAlert(maticKey: string, alertType: number, districtId: number, action: number) {

    let params = new HttpParams();
    params = params.append('matic_key', maticKey);
    params = params.append('alert_type', alertType);
    params = params.append('id', districtId);
    params = params.append('action', action);


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

  getAlert(districtId: number) {

    let params = new HttpParams();
    params = params.append('matic_key', this.globals.ownerAccount.matic_key);
    params = params.append('district_id', districtId);    

    if (this.globals.ownerAccount.wallet_active_in_world) {

      // reset to default off
      this.ilvSlide.checked = false;
      this.constructSlide.checked = false;
      this.produceSlide.checked = false;
      this.distributeSlide.checked = false;

      this.httpClient.get<OwnerAlert[]>(this.baseUrl + '/OwnerData/GetAlert', { params: params })
        .subscribe({
          next: (result) => {

            this.ownerAlert = result;

            for (var index = 0; index < (this.ownerAlert == null ? 0 : this.ownerAlert.length); index++) {

              if (this.ownerAlert[index].key_type == ALERT_TYPE.INITIAL_LAND_VALUE) {
                this.ilvSlide.checked = true;
              }
              else if (this.ownerAlert[index].key_type == ALERT_TYPE.CONSTRUCTION_TAX) {
                this.constructSlide.checked = true;
              }
              else if (this.ownerAlert[index].key_type == ALERT_TYPE.PRODUCTION_TAX) {
                this.produceSlide.checked = true;
              }
              else if (this.ownerAlert[index].key_type == ALERT_TYPE.DISTRIBUTION) {
                this.distributeSlide.checked = true;
              }

            }
          },
          error: (error) => { console.error(error) }
        });

    }
    return;
  }
}
