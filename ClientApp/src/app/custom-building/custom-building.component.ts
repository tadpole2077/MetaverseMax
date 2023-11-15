import { Component, Inject, ViewChild, EventEmitter, ElementRef, Input } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatCheckbox, MatCheckboxChange } from '@angular/material/checkbox';

import { Globals, WORLD } from '../common/global-var';
import { CUSTOM_BUILDING_CATEGORY, ALERT_TYPE, ALERT_ACTION, EVENT_TYPE } from '../common/enum';
import { Parcel, ParcelCollection } from '../common/interface';

@Component({
  selector: 'app-custom-building',
  templateUrl: './custom-building.component.html',
  styleUrls: ['./custom-building.component.css']
})
export class CustomBuildingComponent {

  @Input() districtId: number;

  tableView: Parcel[] = null;
  httpClient: HttpClient;
  baseUrl: string;
  districtParcel: ParcelCollection;
  showBuildingData: boolean = true;
  showOwnerData: boolean = false;
  isMobileView: boolean = false;
  currentPage: number = 1;

  @ViewChild("buildingFilter", { static: true } as any) buildingFilter: MatCheckbox;
  @ViewChild("parcelFilter", { static: true } as any) parcelFilter: MatCheckbox;

  constructor(public globals: Globals, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

    this.httpClient = http;
    this.baseUrl = baseUrl + "api/" + globals.worldCode;

    if (this.width < 768) {
      this.isMobileView = true;
    }
   
  }

  ngOnInit() {
    this.searchAllParcels(this.districtId);
  }

  public get width() {
    return window.innerWidth;
  }

  searchAllParcels(districtId:number) {

    // Reset any prior showing Parcals/Buildings
    this.tableView = null;
    this.districtParcel = null;


    let params = new HttpParams();
    params = params.append('district_id', districtId);

    this.httpClient.get<ParcelCollection>(this.baseUrl + '/plot/get_districtparcel', { params: params })
      .subscribe({
        next: (result) => {

          this.districtParcel = result;

          if (this.districtParcel.parcel_list) {

            let change = new MatCheckboxChange();
            change.checked = true;
            if (this.districtParcel.building_count > 0 || this.districtParcel.parcel_count == 0) {
              this.buildingFilter.checked = true;
              this.filterBuilding(change);
            }
            else {
              this.parcelFilter.checked = true;
              this.filterParcel(change);
            }

            //this.dataSource = new MatTableDataSource<Parcel>(this.worldParcel.parcel_list);
            //this.dataSource.sort = this.sort;
          }
        },
        error: (error) => { console.error(error) }
      });


    return;
  }

  public applyFilter = (value: string) => {

    //this.activeTextFilter = value.trim().toLocaleLowerCase();
    //this.dataSource.filter = value.trim().toLocaleLowerCase();

  }


  filterBuilding(eventCheckbox: MatCheckboxChange) {

    var buildings: Parcel[] = new Array;

    // Use current building view with any applied filters
    if (this.tableView == null) {
      this.tableView = this.districtParcel.parcel_list;
    }

    if (eventCheckbox.checked) {
      this.parcelFilter.checked = false;

      for (var index = 0; index < this.districtParcel.parcel_list.length; index++) {
        if (this.districtParcel.parcel_list[index].building_category_id > 0) {
          buildings.push(this.districtParcel.parcel_list[index]);
        }
      }

      //this.dataSource = new MatTableDataSource<Parcel>(buildings);
      //this.dataSource.sort = this.sort;
      //this.dataSource.filter = this.activeTextFilter;
    }
    else {
      //this.dataSource = new MatTableDataSource<Parcel>(this.tableView);
      //this.dataSource.sort = this.sort;
      //this.dataSource.filter = this.activeTextFilter;
    }

    //this.hidePaginator = buildings.length == 0 || buildings.length < 1000 ? true : false;
    this.tableView = buildings;
    return;
  }

  filterParcel(eventCheckbox: MatCheckboxChange) {

    var buildings: Parcel[] = new Array;

    // Use current building view with any applied filters
    if (this.tableView == null) {
      this.tableView = this.districtParcel.parcel_list;
    }

    if (eventCheckbox.checked) {
      this.buildingFilter.checked = false;

      for (var index = 0; index < this.districtParcel.parcel_list.length; index++) {
        if (this.districtParcel.parcel_list[index].building_category_id == 0) {
          buildings.push(this.districtParcel.parcel_list[index]);
        }
      }

      //this.dataSource = new MatTableDataSource<Parcel>(buildings);
      //this.dataSource.sort = this.sort;
      //this.dataSource.filter = this.activeTextFilter;
    }
    else {
      //this.dataSource = new MatTableDataSource<Parcel>(this.tableView);
      //this.dataSource.sort = this.sort;
      //this.dataSource.filter = this.activeTextFilter;
    }

    //this.hidePaginator = buildings.length == 0 || buildings.length < 1000 ? true : false;
    this.tableView = buildings;
    return;
  }


  getCustomCategoryName(categoryId: number) {
    return CUSTOM_BUILDING_CATEGORY[categoryId];
  }
  getLastActionType(lastActionType: number) {
    return EVENT_TYPE[lastActionType];
  }
  getWorldNumber(worldName: string) {
    return worldName == 'bnb' ? 3 : worldName == 'tron' ? 2 : 1;
  }

  showPage(change: number) {
    this.currentPage = this.currentPage + change;
  }
}
