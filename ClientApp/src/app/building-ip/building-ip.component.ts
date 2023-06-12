import { Component, Inject, ViewChild, Output, Input, EventEmitter, ElementRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource, MatTable } from '@angular/material/table';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSort, MatSortable } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { MatLegacyCheckbox as MatCheckbox, MatLegacyCheckboxChange as MatCheckboxChange } from '@angular/material/legacy-checkbox';
import { ProdHistoryComponent } from '../production-history/prod-history.component';
import { BuildingFilterComponent } from '../building-filter/building-filter.component';
import { BUILDING } from '../owner-data/owner-interface';
import { Globals, WORLD } from '../common/global-var';
import { Router } from '@angular/router';
import { ALERT_TYPE, ALERT_ACTION } from '../common/enum'

interface OfficeGlobalIp {
  totalIP: number;
  globalFund: number;
  maxDailyDistribution: number;
  maxDailyDistributionPerIP: number;
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

  public hidePaginator: boolean;
  public historyShow: boolean = false;
  public buildingFilterShow: boolean = false;
  public showIPAlert: boolean = false;

  public buildingType: number = 0;
  public selectedType: string = "Select Type";
  public selectedLevel: string = "Level 1";
  public activeTextFilter: string = "";


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

  constructor(public globals: Globals, private router: Router, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

    this.httpClient = http;
    this.baseUrl = baseUrl + "api/" + globals.worldCode;
  
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

    // Mobile View - remove secondary columns
    if (this.width < 415) {
      
    }
    else {
    }

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

    var buildingLvl = 1;

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
      buildingLvl = 6;
    }
    else if (this.selectedLevel == "Mega") {
      buildingLvl = 7;
    }
    else {
      buildingLvl = parseInt(this.selectedLevel.split(' ')[1]);
    }



    if (this.selectedType == "Industry") {
      this.search(BUILDING.INDUSTRIAL, buildingLvl);
    }
    else if (this.selectedType == "Residential") {
      this.search(BUILDING.RESIDENTIAL, buildingLvl);
    }
    else if (this.selectedType == "Production") {
      this.search(BUILDING.PRODUCTION, buildingLvl);
    }
    else if (this.selectedType == "Commercial") {
      this.search(BUILDING.COMMERCIAL, buildingLvl);
    }
    else if (this.selectedType == "Municipal") {
      this.search(BUILDING.MUNICIPAL, buildingLvl);
    }
    else if (this.selectedType == "Energy") {
      this.search(BUILDING.ENERGY, buildingLvl);
    }
    else if (this.selectedType == "Office") {
      this.search(BUILDING.OFFICE, buildingLvl);
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
    }
    else {
      this.showIPAlert = false;
    }

  }

  enableRankingAlert(row: BuildingDetail) {

    row.al = row.al == 1 ? 0 : 1;

    this.updateAlert(this.globals.ownerAccount.matic_key, ALERT_TYPE.BUILDING_RANKING, row.id, row.al == 1 ? ALERT_ACTION.ADD : ALERT_ACTION.REMOVE);

  }

  updateAlert(maticKey: string, alertType: number, tokenId: number, action: number) {

    let params = new HttpParams();
    params = params.append('matic_key', maticKey);
    params = params.append('alert_type', alertType);
    params = params.append('id', tokenId);
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
}
