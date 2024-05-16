import { Component, Output, Input, EventEmitter, ViewChild, Inject, OnInit, ContentChild, TemplateRef, ContentChildren, QueryList } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatCheckboxChange } from '@angular/material/checkbox';

import { Application, WORLD } from '../common/global-var';
import { Parcel, ParcelCollection } from '../common/interface';
import { CUSTOM_BUILDING_CATEGORY, EVENT_TYPE } from '../common/enum';


@Component({
  selector: 'app-custom-building-table',
  templateUrl: './custom-building-table.component.html',
  styleUrls: ['./custom-building-table.component.css']
})


export class CustomBuildingTableComponent {

  hidePaginator: boolean = true;
  private tableView: Parcel[] = null;
  httpClient: HttpClient;
  baseUrl: string;
  worldParcel: ParcelCollection; 
  activeTextFilter: string = "";
  isMobileView: boolean = false;
  showCustom: boolean = true;
  buildingCount: number = 0;
  parcelCount: number = 0;

  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;

  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['district_id', 'building_name', 'owner_name', 'plot_count', 'building_category_id', 'unit_forsale_count', 'last_actionUx', 'pos_x', 'pos_y'];
  displayedColumnsMobile: string[] = ['district_id', 'building_name', 'owner_name', 'pos_x'];

  @Output() parcelFilterChange = new EventEmitter<any>();
  @Output() buildingFilterChange = new EventEmitter<any>();


  constructor(public globals: Application, public router: Router, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string) {

    this.httpClient = http;
    this.baseUrl = rootBaseUrl + "api/" + globals.worldCode;

    if (this.width < 768) {
      this.isMobileView = true;
      this.displayedColumns = this.displayedColumnsMobile;
    }    

    this.searchAllParcels();
  }

  ngOnInit() {
  }

  ngOnDestroy() {
  }

  public get width() {
    return window.innerWidth;
  }

  searchAllParcels() {

    let params = new HttpParams();
    //params = params.append('opened', 'true');

    this.httpClient.get<ParcelCollection>(this.baseUrl + '/plot/get_worldparcel', { params: params })
      .subscribe({
        next: (result) => {
          
          this.worldParcel = result;

          if (this.worldParcel.parcel_list) {

            let change = new MatCheckboxChange();
            change.checked = true;
            if (this.worldParcel.building_count > 0 || this.worldParcel.parcel_count == 0) {
              this.buildingFilterChange.emit(true);             
              this.filterBuilding(change);              
            }
            else {
              this.parcelFilterChange.emit(true);
              this.filterParcel(change);
            }

            this.buildingCount = this.worldParcel.building_count;
            this.parcelCount = this.worldParcel.parcel_count;

            //this.dataSource = new MatTableDataSource<Parcel>(this.worldParcel.parcel_list);
            //this.dataSource.sort = this.sort;
          }
        },
        error: (error) => { console.error(error) }
      });


    return;
  }

  public applyFilter = (value: string) => {

    this.activeTextFilter = value.trim().toLocaleLowerCase();
    this.dataSource.filter = value.trim().toLocaleLowerCase();

  }


  filterBuilding(eventCheckbox: MatCheckboxChange) {

    if (!this.worldParcel) {
      return;
    }

    var buildings: Parcel[] = new Array;
    if (!this.isMobileView) {
      this.displayedColumns = ['district_id', 'building_name', 'owner_name', 'plot_count', 'building_category_id', 'unit_forsale_count', 'last_actionUx', 'pos_x', 'pos_y'];
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
      this.displayedColumns = ['district_id', 'building_name', 'owner_name', 'plot_count', 'building_category_id', 'last_actionUx', 'pos_x', 'pos_y'];
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

  applyFilterPredicate() {
    this.dataSource.filterPredicate = function (data: Parcel, filter: string): boolean {
      return data.building_name.toLowerCase().includes(filter)
        || data.owner_name.toLowerCase().includes(filter)
        || data.owner_matic.toString().includes(filter)
        || data.district_id.toString().includes(filter)
        || data.forsale_price.toString().includes(filter)
        || data.pos_x.toString().includes(filter)
        || data.pos_y.toString().includes(filter);
    };
  }

  getCustomCategoryName(categoryId: number) {
    return CUSTOM_BUILDING_CATEGORY[categoryId];
  }
  getLastActionType(lastActionType: number) {
    return EVENT_TYPE[lastActionType];
  }

}
