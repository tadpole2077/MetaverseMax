import { Component, Output, Input, EventEmitter, ViewChild, Inject, OnInit } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatCheckboxChange } from '@angular/material/checkbox';

import { Globals, WORLD } from '../common/global-var';
import { Mission, MissionCollection } from '../common/interface';
import { EVENT_TYPE, BUILDING_TYPE } from '../common/enum';

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
  allMissionCount: number = 0;
  allMissionAvailableCount: number = 0;
  allMissionReward: number = 0;
  allMissionAvailableReward: number = 0;
  repeatableDailyReward: number = 0
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
            this.allMissionCount = this.worldMission.all_mission_count;
            this.allMissionAvailableCount = this.worldMission.all_mission_available_count;
            this.allMissionReward = this.worldMission.all_mission_reward;
            this.allMissionAvailableReward = this.worldMission.all_mission_available_reward;
            this.repeatableDailyReward = this.worldMission.repeatable_daily_reward;

            if (this.worldMission.mission_list) {

              let change = new MatCheckboxChange();
              change.checked = true;

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


  applyFilterPredicate() {
    this.dataSource.filterPredicate = function (data: Mission, filter: string): boolean {
      return data.district_id.toString().includes(filter)
        || data.owner_name.toLowerCase().includes(filter)
        || data.owner_matic.toString().includes(filter)
        || data.reward.toString().includes(filter);
    };
  }

  getLastActionType(lastActionType: number) {
    return EVENT_TYPE[lastActionType];
  }
  getBuildingType(typeId: number) {
    return  BUILDING_TYPE[typeId];
  }


}
