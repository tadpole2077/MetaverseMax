import { Component, Output, Input, EventEmitter, ViewChild, Inject, OnInit } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatCheckboxChange } from '@angular/material/checkbox';

import { Globals, WORLD } from '../common/global-var';
import { Mission, MissionCollection } from '../common/interface';
import { CUSTOM_BUILDING_CATEGORY, EVENT_TYPE, BUILDING_TYPE } from '../common/enum';

interface IDistrictInfo {
  id: number;
  total: number;
}

@Component({
  selector: 'app-mission-desk',
  templateUrl: './mission-desk.component.html',
  styleUrls: ['./mission-desk.component.css']
})

export class MissionDeskComponent {

  hidePaginator: boolean = true;
  private tableView: Mission[] = null;
  httpClient: HttpClient;
  baseUrl: string;
  worldMission: MissionCollection; 
  activeTextFilter: string = "";
  isMobileView: boolean = false;
  showCustom: boolean = true;
  missionCount: number = 0;
  missionReward: number = 0;
  districtList: IDistrictInfo[];

  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;

  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['district_id', 'reward', 'max', 'building_type_id', 'owner_name', 'last_refresh', 'pos_x', 'pos_y'];
  displayedColumnsMobile: string[] = ['district_id', 'reward', 'max', 'building_type_id'];

  @Output() parcelFilterChange = new EventEmitter<any>();
  @Output() buildingFilterChange = new EventEmitter<any>();
  
  constructor(public globals: Globals, public router: Router, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string) {

    this.httpClient = http;
    this.baseUrl = rootBaseUrl + "api/" + globals.worldCode;

    if (this.width < 768) {
      this.isMobileView = true;
      this.displayedColumns = this.displayedColumnsMobile;
    }

    this.searchAllMissions();

  }

  ngOnInit() {
  }

  ngOnDestroy() {
  }

  public get width() {
    return window.innerWidth;
  }

  searchAllMissions() {

    let params = new HttpParams();
    //params = params.append('opened', 'true');

    this.httpClient.get<MissionCollection>(this.baseUrl + '/plot/get_worldmission', { params: params })
      .subscribe({
        next: (result) => {
          
          this.worldMission = result;
          if (this.worldMission) {

            this.missionCount = this.worldMission.mission_count;
            this.missionReward = this.worldMission.mission_reward;

            if (this.worldMission.mission_list) {

              let change = new MatCheckboxChange();
              change.checked = true;
              //if (this.worldMission.building_count > 0 || this.worldParcel.parcel_count == 0) {
              //  this.buildingFilterChange.emit(true);             
              //  this.filterBuilding(change);              
              //}
              //else {
              //  this.parcelFilterChange.emit(true);
              //  this.filterParcel(change);
              //}

              this.dataSource = new MatTableDataSource<Mission>(this.worldMission.mission_list);
              this.dataSource.sort = this.sort;
              this.dataSource.filter = this.activeTextFilter;

              this.evalDistrict(this.worldMission.mission_list);
            }
          }
        },
        error: (error) => { console.error(error) }
      });


    return;
  }

  // Eval all filtered missions in passed list - group count by district
  evalDistrict(missionList: Mission[]) {

    let districtInfo: IDistrictInfo;
    this.districtList = new Array;

    for (var index = 0; index < missionList.length; index++) {
      let found = false;

      for (var index2 = 0; index2 < this.districtList.length; index2++) {
        if (this.districtList[index2].id == missionList[index].district_id) {
          this.districtList[index2].total++;
          found = true;
        }
      }

      if (found == false) {
        this.districtList.push(
          {
            id: missionList[index].district_id,
            total: 1
          } as IDistrictInfo
        );
      }

    }

    // Keep top 4 districts with most missions drop the rest, first sort by total
    this.districtList = this.districtList.sort(function (a, b) { return (a.total > b.total) ? -1 : ((b.total > a.total) ? 1 : 0); }).slice(0,4);    

    return;
  }

  applyFilter(value: string){

    this.activeTextFilter = value.trim().toLocaleLowerCase();
    this.dataSource.filter = value.trim().toLocaleLowerCase();

  }

  applyFilterMissionReward(rewardValue: number) {

    let missionList: Mission[] = new Array;
    let missionReward: number = 0;

    // Use current building view with any applied filters
    if (this.tableView == null) {
      this.tableView = this.worldMission.mission_list;
    }

    for (var index = 0; index < this.tableView.length; index++) {

      if (this.tableView[index].reward >= rewardValue) {
        missionList.push(this.tableView[index]);
        missionReward += this.tableView[index].reward;
      }
    }
    this.missionCount = missionList.length;
    this.missionReward = Number(missionReward.toFixed(2));
    this.evalDistrict( missionList);

    this.dataSource = new MatTableDataSource<Mission>(missionList);
    this.dataSource.sort = this.sort;
    this.dataSource.filter = this.activeTextFilter;

    return;
  }

  removeFilterMissionReward() {

    this.tableView == null;

    this.dataSource = new MatTableDataSource<Mission>(this.worldMission.mission_list);
    this.dataSource.sort = this.sort;
    this.dataSource.filter = this.activeTextFilter;

    this.missionCount = this.worldMission.mission_count;
    this.missionReward = this.worldMission.mission_reward;
    this.evalDistrict( this.worldMission.mission_list );

    return;
  }


  /*
  filterBuilding(eventCheckbox: MatCheckboxChange) {

    var buildings: Parcel[] = new Array;
    if (!this.isMobileView) {
      this.displayedColumns = ['district_id', 'building_name', 'owner_name', 'plot_count', 'building_category_id', 'unit_forsale_count', 'last_action', 'pos_x', 'pos_y'];
    }

    // Use current building view with any applied filters
    if (this.tableView == null) {
      this.tableView = this.worldParcel.parcel_list;
    }

    if (eventCheckbox.checked) {
      this.parcelFilterChange.emit(false);

      for (var index = 0; index < this.worldParcel.parcel_list.length; index++) {
        if (this.worldParcel.parcel_list[index].building_category_id > 0) {
          buildings.push(this.worldParcel.parcel_list[index]);
        }
      }

      this.dataSource = new MatTableDataSource<Parcel>(buildings);
      this.dataSource.sort = this.sort;
      this.dataSource.filter = this.activeTextFilter;
    }
    else {
      this.dataSource = new MatTableDataSource<Parcel>(this.tableView);
      this.dataSource.sort = this.sort;
      this.dataSource.filter = this.activeTextFilter;
    }

    this.hidePaginator = buildings.length == 0 || buildings.length < 1000 ? true : false;
    this.applyFilterPredicate();

    return;
  }

  filterParcel(eventCheckbox: MatCheckboxChange) {

    if (this.worldParcel == null) {
      return;
    }
    var buildings: Parcel[] = new Array;
    if (!this.isMobileView) {
      this.displayedColumns = ['district_id', 'building_name', 'owner_name', 'plot_count', 'building_category_id', 'last_action', 'pos_x', 'pos_y'];
    }

    // Use current building view with any applied filters
    if (this.tableView == null) {
      this.tableView = this.worldParcel.parcel_list;
    }

    if (eventCheckbox.checked) {
      this.buildingFilterChange.emit(false);

      for (var index = 0; index < this.worldParcel.parcel_list.length; index++) {
        if (this.worldParcel.parcel_list[index].building_category_id == 0) {
          buildings.push(this.worldParcel.parcel_list[index]);
        }
      }

      this.dataSource = new MatTableDataSource<Parcel>(buildings);
      this.dataSource.sort = this.sort;
      this.dataSource.filter = this.activeTextFilter;
    }
    else {
      this.dataSource = new MatTableDataSource<Parcel>(this.tableView);
      this.dataSource.sort = this.sort;
      this.dataSource.filter = this.activeTextFilter;
    }

    this.hidePaginator = buildings.length == 0 || buildings.length < 1000 ? true : false;
    this.applyFilterPredicate();

    return;
  }
  */

  applyFilterPredicate() {
    this.dataSource.filterPredicate = function (data: Mission, filter: string): boolean {
      return data.district_id.toString().includes(filter)
        || data.owner_name.toLowerCase().includes(filter)
        || data.owner_matic.toString().includes(filter)
        || data.reward.toString().includes(filter);
    };
  }

  getCustomCategoryName(categoryId: number) {
    return CUSTOM_BUILDING_CATEGORY[categoryId];
  }
  getLastActionType(lastActionType: number) {
    return EVENT_TYPE[lastActionType];
  }
  getBuildingType(typeId: number) {
    return  BUILDING_TYPE[typeId];
  }


}
