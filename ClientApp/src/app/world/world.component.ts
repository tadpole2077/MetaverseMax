import { Component, Output, Input, EventEmitter, ViewChild, Inject, OnInit } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { FormControl } from '@angular/forms';
import { MatCheckbox, MatCheckboxChange } from '@angular/material/checkbox';
import { MatSlideToggle, MatSlideToggleChange } from '@angular/material/slide-toggle';
import { MatTabChangeEvent } from '@angular/material/tabs';

import { CustomBuildingTableComponent } from '../custom-building-table/custom-building-table.component';
import { MissionDeskComponent } from '../mission-desk/mission-desk.component';
import { Globals, WORLD } from '../common/global-var';
import { Alert } from '../common/alert';
import { ALERT_TYPE, ALERT_ACTION } from '../common/enum';
import { Subscription } from 'rxjs';



@Component({
  selector: 'app-world',
  templateUrl: './world.component.html',
  styleUrls: ['./world.component.css']
})


export class WorldComponent {

  readonly ALERT_TYPE: typeof ALERT_TYPE = ALERT_TYPE;    // expose enum to view attributes
  
  httpClient: HttpClient;
  baseUrl: string;
  activeTextFilter: string = "";
  isMobileView: boolean = false;
  subscriptionAccountActive$: Subscription;
  showCustom: boolean = false;
  showMission: boolean = true;
  searchCustomTable = new FormControl('');
  searchMissionTable = new FormControl('');
  filterMissionByReward = new FormControl('');

  @ViewChild("buildingFilter", { static: true } as any) buildingFilter: MatCheckbox;
  @ViewChild("parcelFilter", { static: true } as any) parcelFilter: MatCheckbox;
  @ViewChild("alertSlide", { static: true } as any) alertSlide: MatSlideToggle;

  @ViewChild(CustomBuildingTableComponent, { static: false }) customComponent: CustomBuildingTableComponent;
  @ViewChild(MissionDeskComponent, { static: false }) missionComponent: MissionDeskComponent;


  constructor(public globals: Globals, public alert: Alert,  public router: Router, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string) {

    this.httpClient = http;
    this.baseUrl = rootBaseUrl + "api/" + globals.worldCode;

    if (this.width < 768) {
      this.isMobileView = true;
    }
  }

  ngOnInit() {

    // Monitor using service - when account status changes - active / inactive : Get alert switch state for account.
    this.subscriptionAccountActive$ = this.globals.accountActive$.subscribe(active => {

      if (active) {
        this.alert.getAlertSingle(0, ALERT_TYPE.NEW_BUILDING, this.alertSlide);
      }

    })

    if (this.globals.ownerAccount.wallet_active_in_world) {
      this.alert.getAlertSingle(0, ALERT_TYPE.NEW_BUILDING, this.alertSlide);
    }

  }

  ngOnDestroy() {
    this.subscriptionAccountActive$.unsubscribe();
  }

  get width() {
    return window.innerWidth;
  }

  applyFilterMission(value: string){
    this.missionComponent.applyFilter(value);
  }
  applyFilterCustom = (value: string) => {
    this.customComponent.applyFilter(value);
  }

  applyFilterMissionRewardByKey(rewardValue: string) {

    if (rewardValue != '') {
      this.applyFilterMissionReward(rewardValue);
    }
    else {
      this.removeRewardFilterMission();
    }
  }
  applyFilterMissionReward(rewardValue: string) {
    if ( rewardValue != '') {
      this.missionComponent.applyFilterMissionReward(Number(rewardValue));
    }
  }
  removeRewardFilterMission() {
    this.missionComponent.removeFilterMissionReward();
  }

  // Emit called from child component CustomBuildingTable to change checkbox on parent - used on initial table load depending on retrieved records
  buildingFilterChange(checkedFlag : boolean) {
    this.buildingFilter.checked = checkedFlag;
    return;
  }
  filterBuilding(eventCheckbox: MatCheckboxChange) {   
    this.customComponent.filterBuilding(eventCheckbox);
    return;
  }

  // Emit called from child component CustomBuildingTable to change checkbox on parent
  parcelFilterChange(checkedFlag : boolean) {
    this.parcelFilter.checked = checkedFlag;
    return;
  }
  filterParcel(eventCheckbox: MatCheckboxChange) {
    this.customComponent.filterParcel(eventCheckbox);
    return;
  }


  alertChange(eventSlider: MatSlideToggleChange, alertType: number) {

    // update db - WS call    
    this.alert.updateAlert(this.globals.ownerAccount.matic_key, alertType, 0, eventSlider.checked == true ? ALERT_ACTION.ADD : ALERT_ACTION.REMOVE);

  }

  changeTab(eventTab: MatTabChangeEvent) {
    
    if (eventTab.index == 1) {
      this.showCustom = true;
      this.showMission = false;
    }
    else {
      this.showCustom = false;
      this.showMission = true;
    }
  }
}
