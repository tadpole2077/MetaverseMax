import { Component, Output, Input, EventEmitter, ViewChild, Inject, OnInit } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { FormControl } from '@angular/forms';
import { MatCheckbox, MatCheckboxChange } from '@angular/material/checkbox';
import { MatSlideToggle, MatSlideToggleChange } from '@angular/material/slide-toggle';

import { Globals, WORLD } from '../common/global-var';
import { Alert } from '../common/alert';
import { CUSTOM_BUILDING_CATEGORY, ALERT_TYPE, ALERT_ACTION } from '../common/enum';
import { Subscription } from 'rxjs';

interface WorldParcel {
  parcel_count: number;
  building_count: number;
  parcel_list: Parcel[];
}
interface Parcel {
  parcel_id: number;
  pos_x: number;
  pos_y: number;
  district_id: number;
  unit_count: number;
  owner_matic: string;
  owner_name: string;
  owner_avatar_id: number;
  forsale: boolean;
  forsale_price: number;
  last_action: string;
  last_actionUx: number;
  building_img: string;
  building_name: string;
  plot_count: number;
  building_category_id: number;
}

@Component({
  selector: 'app-world',
  templateUrl: './world.component.html',
  styleUrls: ['./world.component.css']
})


export class WorldComponent {

  readonly ALERT_TYPE: typeof ALERT_TYPE = ALERT_TYPE;    // expose enum to view attributes

  hidePaginator: boolean = true;
  private tableView: Parcel[] = null;
  httpClient: HttpClient;
  baseUrl: string;
  worldParcel: WorldParcel; 
  activeTextFilter: string = "";
  searchTable = new FormControl('');
  isMobileView: boolean = false;
  subscriptionAccountActive$: Subscription;

  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild("buildingFilter", { static: true } as any) buildingFilter: MatCheckbox;
  @ViewChild("parcelFilter", { static: true } as any) parcelFilter: MatCheckbox;
  @ViewChild("alertSlide", { static: true } as any) alertSlide: MatSlideToggle;

  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['district_id', 'building_name', 'owner_name', 'plot_count', 'building_category_id', 'last_action', 'pos_x', 'pos_y'];
  displayedColumnsMobile: string[] = ['district_id', 'building_name', 'owner_name', 'pos_x'];

  constructor(public globals: Globals, public alert: Alert,  public router: Router, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string) {

    this.httpClient = http;
    this.baseUrl = rootBaseUrl + "api/" + globals.worldCode;

    if (this.width < 768) {
      this.isMobileView = true;
      this.displayedColumns = this.displayedColumnsMobile;
    }

    this.searchAllParcels();

  }

  ngOnInit() {

    // Monitor using service - when account status changes - active / inactive : Get alert switch state for account.
    this.subscriptionAccountActive$ = this.globals.accountActive$.subscribe(active => {

      if (active) {
        this.alert.getAlertSingle(0, ALERT_TYPE.NEW_BUILDING, this.alertSlide);
      }

    })

  }

  ngOnDestroy() {
    this.subscriptionAccountActive$.unsubscribe();
  }

  public get width() {
    return window.innerWidth;
  }

  searchAllParcels() {

    let params = new HttpParams();
    //params = params.append('opened', 'true');

    this.httpClient.get<WorldParcel>(this.baseUrl + '/plot/get_worldparcel', { params: params })
      .subscribe({
        next: (result) => {
          
          this.worldParcel = result;

          if (this.worldParcel.parcel_list) {

            let change = new MatCheckboxChange();
            change.checked = true;
            if (this.worldParcel.building_count > 0 || this.worldParcel.parcel_count == 0) {
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

    this.activeTextFilter = value.trim().toLocaleLowerCase();
    this.dataSource.filter = value.trim().toLocaleLowerCase();

  }


  filterBuilding(eventCheckbox: MatCheckboxChange) {

    var buildings: Parcel[] = new Array;

    // Use current building view with any applied filters
    if (this.tableView == null) {
      this.tableView = this.worldParcel.parcel_list;
    }

    if (eventCheckbox.checked) {
      this.parcelFilter.checked = false;

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

    var buildings: Parcel[] = new Array;

    // Use current building view with any applied filters
    if (this.tableView == null) {
      this.tableView = this.worldParcel.parcel_list;
    }

    if (eventCheckbox.checked) {
      this.buildingFilter.checked = false;

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

  alertChange(eventSlider: MatSlideToggleChange, alertType: number) {

    // update db - WS call    
    this.alert.updateAlert(this.globals.ownerAccount.matic_key, alertType, 0, eventSlider.checked == true ? ALERT_ACTION.ADD : ALERT_ACTION.REMOVE);

  }
}
