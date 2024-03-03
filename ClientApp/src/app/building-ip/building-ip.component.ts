import { Component, Inject, ViewChild, Output, Input, EventEmitter, ElementRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource, MatTable } from '@angular/material/table';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSort, MatSortable } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { FormControl } from '@angular/forms';
import { MatCheckbox, MatCheckboxChange } from '@angular/material/checkbox';

import { Alert } from '../common/alert';
import { ProdHistoryComponent } from '../production-history/prod-history.component';
import { BuildingFilterComponent } from '../building-filter/building-filter.component';
import { Globals, WORLD } from '../common/global-var';
import { Router } from '@angular/router';
import { ALERT_TYPE, ALERT_ACTION, BUILDING, BUILDING_TYPE, BUILDING_SUBTYPE } from '../common/enum'

interface OfficeGlobalIp {
  totalIP: number;
  globalFund: number;
  maxDailyDistribution: number;
  maxDailyDistributionPerIP: number;
  lastDistribution: number;
}

interface BuildingCollection {
  minIP: number;
  maxIP: number;
  avgIP: number;
  rangeIP: number;
  buildingIP_impact: number;
  sync_date: string;
  img_url: string;
  buildings: BuildingDetail[];
  total_produced: number[];
  show_prediction: boolean;
  active_buildings: ResourceActive[]
}

interface BuildingDetail {
  id: number;
  bid: number;
  dis: number;
  pos_x: number;
  pos_y: number;
  pos: number;  
      
  rank: number;
  al: number;     /* Alert 1=active, 0=inactive*/
  ip_t: number;
  ip_b: number;
  bon: number;
   
  nid: number;
  name_m: number;
  name: string;
    
  price: number;  
  pre: number;
  warn: string;
  img: string;

  con: number;
  act: boolean;
  r_p: number;
  poi: number;
  tax: number;
}

interface ResourceActive {
  name: string;
  total: number;
  active: number;
  active_total_ip: number;
  shutdown: number;
  building_id: number;
  building_img: string;
  building_name: string;
}


@Component({
  selector: 'app-building-ip',
  templateUrl: './building-ip.component.html',
  styleUrls: ['./building-ip.component.css']
})
export class BuildingIPComponent {

  @Output() filterBuildingEvent = new EventEmitter<number>();
  
  public buildingCollection: BuildingCollection = null;
  public viewBuildings: BuildingDetail[] = null;
  public officeGlobalIp: OfficeGlobalIp = null;
  public officeBCIndex: number = -1;
  public officeBC_MaxEarningsPer1kIP: number = 0;
  public officeBC_AvgEarningsPer1kIP: number = 0;
  public officeBC_ActiveTotalIpPercent: number = 0;
  public officeBC_ActiveAvgIP: number = 0;



  public hidePaginator: boolean;
  public historyShow: boolean = false;
  public buildingFilterShow: boolean = false;
  public showIPAlert: boolean = false;

  public test: boolean = true;
  public buildingType: number = 0;
  public selectedBuildingLvl: number = 7;
  public selectedType: string = "Select Type";
  public selectedLevel: string = "Level 1";
  public activeTextFilter: string = "";
  searchTable = new FormControl('');
  
  // UI class flags
  public searchBlinkOnce: boolean = false;

  public typeList: string[] = ["Residential", "Industry", "Production","Energy", "Office", "Commercial", "Municipal"];
  public levelList: string[] = ["Level 1","Level 2","Level 3","Level 4","Level 5","Huge","Mega"];

  httpClient: HttpClient;
  baseUrl: string;

  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatTable, { static: false }) ipRankTable: MatTable<BuildingDetail>;
  @ViewChild('paginatorTop', { static: false }) paginatorTop: MatPaginator;
  @ViewChild('paginatorBottom', { static: false }) paginatorBottom: MatPaginator;
  @ViewChild(ProdHistoryComponent, { static: true }) prodHistory: ProdHistoryComponent;
  @ViewChild(BuildingFilterComponent, { static: true }) buildingFilter: BuildingFilterComponent;
  @ViewChild('progressIcon', { static: false }) progressIcon: ElementRef;
  @ViewChild("activeChkbox", { static: true } as any) activeChkbox: MatCheckbox;
  @ViewChild("toRentChkbox", { static: true } as any) toRentChkbox: MatCheckbox;
  @ViewChild("forSaleChkbox", { static: true } as any) forSaleChkbox: MatCheckbox;

  displayedColumns: string[];
  displayColumnFull: string[] = ['pos', 'rank', 'ip_t', 'ip_b', 'bon', 'name', 'con', 'dis', 'pos_x', 'id'];
  displayedColumnsStandard: string[] = ['pos', 'rank', 'ip_t', 'ip_b', 'bon', 'name', 'con', 'dis', 'pos_x', 'id'];
  displayedColumnsPredict: string[] = ['pos', 'rank', 'ip_t', 'ip_b', 'bon', 'name', 'con', 'pre', 'dis', 'pos_x', 'id']
  displayedColumnsOffice: string[] = ['pos', 'ip_t', 'ip_b', 'bon', 'name', 'con', 'dis', 'pos_x', 'id'];
  isLoadingResults: boolean;
  resultsLength: any;
  isMobileView: boolean = false;

  constructor(public globals: Globals, private router: Router, http: HttpClient, @Inject('BASE_URL') baseUrl: string, public alert: Alert) {

    this.httpClient = http;
    this.baseUrl = baseUrl + "api/" + globals.worldCode;

    // Mobile View - remove secondary columns
    if (this.width < 415) {
      this.isMobileView = true;
    }
    else {
      this.isMobileView = false;
    }
  
  }

  public get width() {
    return window.innerWidth;
  }

  public assignColumns(columnSet:string[]) {
    return this.width < 415 ?
      columnSet = columnSet.filter(e => e !== 'ip_b').filter(e => e !== 'bon') : columnSet;    
  }

  public search(type: number, level: number) {

    // Redirect to home page if account does not have approval access right.
    if (this.globals.ownerAccount.pro_tools_enabled == false) {
      let navigateTo: string = '/' + this.globals.worldCode;
      this.router.navigate([navigateTo], {});
    }

    this.buildingFilterShow = false;
    this.buildingType = type;
    this.buildingCollection = null;

    this.progressIcon.nativeElement.classList.add("rotate");

    if (type == BUILDING.OFFICE) {
      this.getOfficeGlobalData();
    }


    let params = new HttpParams();
    params = params.append('type', type.toString());
    params = params.append('level', level.toString());
    params = params.append('requester_matic', this.globals.ownerAccount.matic_key);

    this.httpClient.get<BuildingCollection>(this.baseUrl + '/plot/BuildingIPbyTypeGet', { params: params })
      .subscribe({
        next: (result) => {

          this.buildingCollection = result;
          this.progressIcon.nativeElement.classList.remove("rotate");

          if (this.buildingCollection.show_prediction) {
            this.displayedColumns = this.assignColumns(this.displayedColumnsPredict);
          }
          else {
            this.displayedColumns = this.assignColumns(this.displayColumnFull);
          }

          if (type == BUILDING.OFFICE) {
            this.displayedColumns = this.assignColumns(this.displayedColumnsOffice);
            this.officeBCIndex = this.findBCIndex(this.buildingCollection.active_buildings);
            if (this.officeGlobalIp) {
              this.officeBC_MaxEarningsPer1kIP = this.officeGlobalIp.maxDailyDistribution / (this.buildingCollection.active_buildings[this.officeBCIndex].active_total_ip / 1000);
              this.officeBC_AvgEarningsPer1kIP = this.officeGlobalIp.lastDistribution / (this.buildingCollection.active_buildings[this.officeBCIndex].active_total_ip / 1000);

              this.officeBC_ActiveTotalIpPercent = this.officeGlobalIp.totalIP > 0 ? (this.buildingCollection.active_buildings[this.officeBCIndex].active_total_ip / this.officeGlobalIp.totalIP) * 100 : 0;
              this.officeBC_ActiveAvgIP = this.buildingCollection.active_buildings[this.officeBCIndex].active > 0 ? this.buildingCollection.active_buildings[this.officeBCIndex].active_total_ip / this.buildingCollection.active_buildings[this.officeBCIndex].active : 0;
            }
          }
          else {
            this.displayedColumns = this.assignColumns(this.displayedColumnsStandard);
          }

          if (this.buildingCollection.buildings != null && this.buildingCollection.buildings.length > 0) {
            this.loadBuildingData();
          }
          else {
            this.buildingFilter.initFilterIcons();
            this.dataSource = new MatTableDataSource<BuildingDetail>(null);
          }

          this.activeChkbox.checked = false;
          this.toRentChkbox.checked = false;
          this.forSaleChkbox.checked = false;

        },
        error: (error) => { console.error(error) }
      });

    return;
  }

  // Find index of Business center resource if it exist, used to display summary data on BC
  public findBCIndex(active_buildings: ResourceActive[] ) {
    let indexBC = -1;

    for (var index = 0; index < active_buildings.length; index++) {
      if (active_buildings[index].building_id == BUILDING_SUBTYPE.BUSINESS_CENTER) {
        indexBC = index;
      }
    }

    return indexBC;
  }

  public getOfficeGlobalData() {
    let params = new HttpParams();
    //params = params.append('type', type.toString());
    
    this.httpClient.get<OfficeGlobalIp>(this.baseUrl + '/plot/OfficeGlobalSummary', { params: params })
      .subscribe((result: OfficeGlobalIp) => {
        this.officeGlobalIp = result;
      });

  }


  loadBuildingData(): void {

    this.viewBuildings = null;
    this.dataSource = new MatTableDataSource<BuildingDetail>(this.buildingCollection.buildings);

    this.hidePaginator = this.buildingCollection.buildings.length == 0 || this.buildingCollection.buildings.length < 501 ? true : false;
    this.dataSource.paginator = this.paginatorTop;
    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }

    this.dataSource.sort = this.sort;
    this.sort.sort({ id: null, start: 'desc', disableClear: false }); //Clear any prior sort - reset sort arrows. best option to reset on each load.
    this.sort.sort(({ id: 'ip_efficiency', start: 'desc' }) as MatSortable);        // Default sort order on date

    this.dataSource.filter = this.activeTextFilter;

    this.showBuildingFilter(this.buildingCollection.active_buildings);

    // Add custom sort
    //this.dataSource.sortingDataAccessor = (item: BuildingDetail, property) => {
    //  switch (property) {
    //    case 'predict_eval_result': return item.predict_eval_result == null ? 0 : item.predict_eval_result;
    //    default: return item[property];
    //  }
    //};
  }

  showBuildingFilter(activeBuildings: ResourceActive[] ) {

    this.buildingFilter.initFilterIcons();
    this.buildingFilter.loadIcons(activeBuildings);

    this.buildingFilterShow = true;
  }

  ngAfterContentChecked(): void {
    if (this.paginatorTop) {
      this.paginatorBottom.length = this.paginatorTop.length;
    }
  }

  handlePaginatorTop(e): void {
    const { pageSize, pageIndex } = e;
    //this.paginatorTop.pageSize = pageSize
    this.paginatorTop.pageIndex = pageIndex;
    this.paginatorTop.page.emit(e);
  }

  handlePaginatorBottom(e): void {
    const { pageSize, pageIndex } = e;
    //this.paginatorBottom.pageSize = pageSize
    this.paginatorBottom.length = this.paginatorTop.length;
    this.paginatorBottom.pageIndex = pageIndex;
  }

  public searchFromDropdown(building: string, level: string) {

    this.selectedBuildingLvl = 1;

    if (building != "") {
      this.selectedType = building;
    }
    if (level != "") {
      this.selectedLevel = level;
    }
    if (this.selectedLevel == "Select Type" || this.selectedLevel == "Select Level") {
      return;
    }

    if (this.selectedLevel == "Huge") {
      this.selectedBuildingLvl = 6;
    }
    else if (this.selectedLevel == "Mega") {
      this.selectedBuildingLvl = 7;
    }
    else {
      this.selectedBuildingLvl = parseInt(this.selectedLevel.split(' ')[1]);
    }



    if (this.selectedType == "Industry") {
      this.search(BUILDING.INDUSTRIAL, this.selectedBuildingLvl);
    }
    else if (this.selectedType == "Residential") {
      this.search(BUILDING.RESIDENTIAL, this.selectedBuildingLvl);
    }
    else if (this.selectedType == "Production") {
      this.search(BUILDING.PRODUCTION, this.selectedBuildingLvl);
    }
    else if (this.selectedType == "Commercial") {
      this.search(BUILDING.COMMERCIAL, this.selectedBuildingLvl);
    }
    else if (this.selectedType == "Municipal") {
      this.search(BUILDING.MUNICIPAL, this.selectedBuildingLvl);
    }
    else if (this.selectedType == "Energy") {
      this.search(BUILDING.ENERGY, this.selectedBuildingLvl);
    }
    else if (this.selectedType == "Office") {
      this.search(BUILDING.OFFICE, this.selectedBuildingLvl);
    }

    return;
  }

  public applyFilter = (value: string) => {

    this.activeTextFilter = value.trim().toLocaleLowerCase();
    this.dataSource.filter = value.trim().toLocaleLowerCase();

  }


  public hideHistory(componentVisible: boolean) {

    this.historyShow = !componentVisible;

  }

  showHistory(asset_id: number, pos_x: number, pos_y: number, building_type: number, ip_efficiency: number) {

    this.prodHistory.searchHistory(asset_id, pos_x, pos_y, building_type, false);
    this.historyShow = true;

  }

  // Called from building-filter component
  public filterBuilding(buildingIdList: number[]) {

    var filterBuildingList: BuildingDetail[] = new Array;
    this.activeChkbox.checked = false;
    this.toRentChkbox.checked = false;
    this.forSaleChkbox.checked = false;

    if (buildingIdList.length == 0) {
      this.dataSource = new MatTableDataSource<BuildingDetail>(this.buildingCollection.buildings);
      this.dataSource.sort = this.sort;
      this.dataSource.filter = this.activeTextFilter;
      this.viewBuildings = null;
    }
    else {

      for (var index = 0; index < this.buildingCollection.buildings.length; index++) {

        for (var findex = 0; findex < buildingIdList.length; findex++) {

          if (this.buildingCollection.buildings[index].bid == buildingIdList[findex]) {
            filterBuildingList.push(this.buildingCollection.buildings[index]);
          }
        }

      }

      this.dataSource = new MatTableDataSource<BuildingDetail>(filterBuildingList);
      this.dataSource.sort = this.sort;
      this.dataSource.filter = this.activeTextFilter;

      this.viewBuildings = filterBuildingList;
    }

  }

  filterActive(eventCheckbox: MatCheckboxChange) {

    var activeBuildings: BuildingDetail[] = new Array;

    // Use current building view with any applied filters
    if (this.viewBuildings == null) {
      this.viewBuildings = this.buildingCollection.buildings;
    }

    if (eventCheckbox.checked) {      
      this.toRentChkbox.checked = false;
      this.forSaleChkbox.checked = false;

      for (var index = 0; index < this.viewBuildings.length; index++) {

        if (this.viewBuildings[index].act) {
          activeBuildings.push(this.viewBuildings[index]);
        }

      }

      this.dataSource = new MatTableDataSource<BuildingDetail>(activeBuildings);
      this.dataSource.sort = this.sort;
      this.dataSource.filter = this.activeTextFilter;
    }
    else {

      this.dataSource = new MatTableDataSource<BuildingDetail>(this.viewBuildings);
      this.dataSource.sort = this.sort;
      this.dataSource.filter = this.activeTextFilter;
    }

    //this.dataSource.sort = this.sort;
    return;
  }

  filterToRent(eventCheckbox: MatCheckboxChange) {
    var toRentBuildings: BuildingDetail[] = new Array;

    // Use current building view with any applied filters
    if (this.viewBuildings == null) {
      this.viewBuildings = this.buildingCollection.buildings;
    }

    if (eventCheckbox.checked) {
      this.activeChkbox.checked = false;
      this.forSaleChkbox.checked = false;
      
      for (var index = 0; index < this.viewBuildings.length; index++) {
        if (this.viewBuildings[index].r_p > 0) {
          toRentBuildings.push(this.viewBuildings[index]);
        }
      }

      this.dataSource = new MatTableDataSource<BuildingDetail>(toRentBuildings);
      this.dataSource.sort = this.sort;
      this.dataSource.filter = this.activeTextFilter;
    }
    else {
      this.dataSource = new MatTableDataSource<BuildingDetail>(this.viewBuildings);
      this.dataSource.sort = this.sort;
      this.dataSource.filter = this.activeTextFilter;
    }

    //this.dataSource.sort = this.sort;
    return;
  }


  filterForSale(eventCheckbox: MatCheckboxChange) {

    var forSaleBuildings: BuildingDetail[] = new Array;

    // Use current building view with any applied filters
    if (this.viewBuildings == null) {
      this.viewBuildings = this.buildingCollection.buildings;
    }

    if (eventCheckbox.checked) {
      this.toRentChkbox.checked = false;
      this.activeChkbox.checked = false;

      for (var index = 0; index < this.viewBuildings.length; index++) {
        if (this.viewBuildings[index].price > 0) {
          forSaleBuildings.push(this.viewBuildings[index]);
        }
      }

      this.dataSource = new MatTableDataSource<BuildingDetail>(forSaleBuildings);
      this.dataSource.sort = this.sort;
      this.dataSource.filter = this.activeTextFilter;
    }
    else {
      this.dataSource = new MatTableDataSource<BuildingDetail>(this.viewBuildings);
      this.dataSource.sort = this.sort;
      this.dataSource.filter = this.activeTextFilter;
    }

    //this.dataSource.sort = this.sort;
    return;
  }

  showAlertChange(eventCheckbox: MatCheckboxChange) {
    if (eventCheckbox.checked) {
      this.showIPAlert = true;
      this.searchBlinkOnce = true;
    }
    else {
      this.showIPAlert = false;
      this.searchBlinkOnce = false;
    }

  }

  enableRankingAlert(row: BuildingDetail) {

    row.al = row.al == 1 ? 0 : 1;

    this.alert.updateAlert(this.globals.ownerAccount.matic_key, ALERT_TYPE.BUILDING_RANKING, row.id, row.al == 1 ? ALERT_ACTION.ADD : ALERT_ACTION.REMOVE);

  }

}
