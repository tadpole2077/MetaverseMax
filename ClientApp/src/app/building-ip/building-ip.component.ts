import { Component, Inject, ViewChild, Output, Input, EventEmitter, ElementRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource, MatTable } from '@angular/material/table';
import { MatSort, MatSortable } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { MatCheckbox, MatCheckboxChange } from '@angular/material/checkbox';
import { ProdHistoryComponent } from '../production-history/prod-history.component';
import { BUILDING } from '../owner-data/owner-interface';
import { Globals, WORLD } from '../common/global-var';

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
}
interface BuildingDetail {
  pos: number;  
  dis: number;
  id: number;
  img: string;
  pos_x: number;
  pos_y: number;

  rank: number;
  bon: number;
  ip_b: number;
  ip_t: number;
  name_id: number;
  name_m: number;
  name: string;
    
  price: number;  
  pre: number;
  warn: string;
  con: number;
  act: boolean;
  r_p: number;
}



@Component({
  selector: 'app-building-ip',
  templateUrl: './building-ip.component.html',
  styleUrls: ['./building-ip.component.css']
})
export class BuildingIPComponent {
 
  public hidePaginator: boolean;
  public buildingCollection: BuildingCollection = null;
  public historyShow: boolean = false;
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
  @ViewChild('progressIcon', { static: false }) progressIcon: ElementRef;
  @ViewChild("activeChkbox", { static: true } as any) activeChkbox: MatCheckbox;
  @ViewChild("toRentChkbox", { static: true } as any) toRentChkbox: MatCheckbox;
  @ViewChild("forSaleChkbox", { static: true } as any) forSaleChkbox: MatCheckbox;

  displayedColumns: string[] = ['pos', 'rank', 'ip_t', 'ip_b', 'bon', 'name', 'con', 'dis', 'pos_x', 'id'];
  displayedColumnsPredict: string[] = ['pos', 'rank', 'ip_t', 'ip_b', 'bon', 'name', 'con', 'pre', 'dis', 'pos_x', 'id']
  displayedColumnsOffice: string[] = ['pos', 'ip_t', 'ip_b', 'bon', 'name', 'con', 'pre', 'dis', 'pos_x', 'id'];
  isLoadingResults: boolean;
  resultsLength: any;

  constructor(public globals: Globals, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

    this.httpClient = http;
    this.baseUrl = baseUrl + "api/" + globals.worldCode;
  
  }

  public get width() {
    return window.innerWidth;
  }

  public search(type: number, level: number) {

    this.buildingType = type;
    this.buildingCollection = null;

    // Mobile View - remove secondary columns
    if (this.width < 768) {
      
    }
    else {
    }

    this.progressIcon.nativeElement.classList.add("rotate");


    let params = new HttpParams();
    params = params.append('type', type.toString());
    params = params.append('level', level.toString());

    this.httpClient.get<BuildingCollection>(this.baseUrl + '/plot/BuildingIPbyTypeGet', { params: params })
      .subscribe((result: BuildingCollection) => {

        this.buildingCollection = result;
        this.progressIcon.nativeElement.classList.remove("rotate");

        if (this.buildingCollection.show_prediction) {          
          this.displayedColumns = this.displayedColumnsPredict;
        }

        if (type == BUILDING.OFFICE) {
          this.displayedColumns = this.displayedColumnsOffice;
        }

        if (this.buildingCollection.buildings !=null && this.buildingCollection.buildings.length > 0) {
          this.loadBuildingData();                   
        }
        else {
          this.dataSource = new MatTableDataSource<BuildingDetail>(null);
        }

        this.activeChkbox.checked = false;
        this.toRentChkbox.checked = false;
        this.forSaleChkbox.checked = false;

      }, error => console.error(error));

    return;
  }

  loadBuildingData(): void {

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

    // Add custom sort
    //this.dataSource.sortingDataAccessor = (item: BuildingDetail, property) => {
    //  switch (property) {
    //    case 'predict_eval_result': return item.predict_eval_result == null ? 0 : item.predict_eval_result;
    //    default: return item[property];
    //  }
    //};
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
  
  filterActive(eventCheckbox: MatCheckboxChange) {
    var activeBuildings: BuildingDetail[] = new Array;

    if (eventCheckbox.checked) {      
      this.toRentChkbox.checked = false;
      this.forSaleChkbox.checked = false;

      for (var index = 0; index < this.buildingCollection.buildings.length; index++) {
        if (this.buildingCollection.buildings[index].act) {
          activeBuildings.push(this.buildingCollection.buildings[index]);
        }
      }

      this.dataSource = new MatTableDataSource<BuildingDetail>(activeBuildings);
      this.dataSource.sort = this.sort;
      this.dataSource.filter = this.activeTextFilter;
    }
    else {
      this.loadBuildingData();
    }

    //this.dataSource.sort = this.sort;
    return;
  }

  filterToRent(eventCheckbox: MatCheckboxChange) {
    var toRentBuildings: BuildingDetail[] = new Array;

    if (eventCheckbox.checked) {
      this.activeChkbox.checked = false;
      this.forSaleChkbox.checked = false;
      
      for (var index = 0; index < this.buildingCollection.buildings.length; index++) {
        if (this.buildingCollection.buildings[index].r_p > 0) {
          toRentBuildings.push(this.buildingCollection.buildings[index]);
        }
      }

      this.dataSource = new MatTableDataSource<BuildingDetail>(toRentBuildings);
      this.dataSource.sort = this.sort;
      this.dataSource.filter = this.activeTextFilter;
    }
    else {
      this.loadBuildingData();
    }

    //this.dataSource.sort = this.sort;
    return;
  }


  filterForSale(eventCheckbox: MatCheckboxChange) {
    var forSaleBuildings: BuildingDetail[] = new Array;

    if (eventCheckbox.checked) {
      this.toRentChkbox.checked = false;
      this.activeChkbox.checked = false;

      for (var index = 0; index < this.buildingCollection.buildings.length; index++) {
        if (this.buildingCollection.buildings[index].price > 0) {
          forSaleBuildings.push(this.buildingCollection.buildings[index]);
        }
      }

      this.dataSource = new MatTableDataSource<BuildingDetail>(forSaleBuildings);
      this.dataSource.sort = this.sort;
      this.dataSource.filter = this.activeTextFilter;
    }
    else {
      this.loadBuildingData();
    }

    //this.dataSource.sort = this.sort;
    return;
  }

}
