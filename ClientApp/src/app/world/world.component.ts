import { Component, Output, Input, EventEmitter, ViewChild, Inject, OnInit, Directive, ContentChild, ContentChildren, TemplateRef, ViewContainerRef, QueryList } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { FormControl } from '@angular/forms';
import { MatCheckbox, MatCheckboxChange } from '@angular/material/checkbox';
import { MatSlideToggle, MatSlideToggleChange } from '@angular/material/slide-toggle';
import { MatTabChangeEvent } from '@angular/material/tabs';
import { Subscription } from 'rxjs';

import { CustomBuildingTableComponent } from '../custom-building-table/custom-building-table.component';
import { MissionDeskComponent } from '../mission-desk/mission-desk.component';
import { Application, WORLD } from '../common/global-var';
import { Alert } from '../common/alert';
import { ALERT_TYPE, ALERT_ACTION } from '../common/enum';
import { TabContainerLazyComponent } from '../tab-container-lazy/tab-container-lazy.component';
import { animate, state, style, transition, trigger } from '@angular/animations';



@Component({
  selector: 'app-world',
  templateUrl: './world.component.html',
  styleUrls: ['./world.component.css'],
  animations: [
    trigger('detailExpand', [
      state('collapsed', style({ height: '0px', minHeight: '0' })),
      state('expanded', style({ height: '*' })),
      transition('expanded <=> collapsed', animate('225ms cubic-bezier(0.4, 0.0, 0.2, 1)')),
    ]),
  ],
})


export class WorldComponent {

  readonly ALERT_TYPE: typeof ALERT_TYPE = ALERT_TYPE;    // expose enum to view attributes

  showDetail: false;
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

  @ViewChild("buildingFilter", { static: true }) buildingFilter: MatCheckbox;
  @ViewChild("parcelFilter", { static: true }) parcelFilter: MatCheckbox;
  @ViewChild("alertSlide", { static: true }) alertSlide: MatSlideToggle;

  @ViewChild(CustomBuildingTableComponent, { static: false }) customComponent: CustomBuildingTableComponent;
  @ViewChild(MissionDeskComponent, { static: false }) missionComponent: MissionDeskComponent;
  

  constructor(public globals: Application, public alert: Alert,  public router: Router, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string) {

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
      this.showDetail = false;
      this.showCustom = true;
      this.showMission = false;

      //this.customComponent.searchAllParcels();
      
    }
    else {
      this.showCustom = false;
      this.showMission = true;      
    }
  }
}
