import { Component, Inject, ViewChild, Output, Input, EventEmitter, ElementRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, MatSortable } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { ProdHistoryComponent } from '../production-history/prod-history.component';
import { BUILDING } from '../owner-data/owner-interface';


interface BuildingCollection {
  minIP: number;
  maxIP: number;
  avgIP: number;
  rangeIP: number;
  buildingIP_impact: number;
  sync_date: string;
  buildings: BuildingDetail[];
  total_produced: number[];
}
interface BuildingDetail {
  position: number;
  ip_efficiency: number;
  district_id: number;
  building_id: number;
  building_img: string;
  influence: number;
  influence_bonus: number;
  influence_info: number;
  total_ip: number;
  owner_avatar_id: number;
  owner_matic: number;
  owner_nickname: string;
  pos_x: number;
  pos_y: number;
  token_id: number;
  current_price: number;
  predict_eval: boolean;
  predict_eval_result: number;
  ip_warning: string;
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

  public typeList: string[] = ["Residential", "Industry", "Production","Energy", /*"Office", */"Commercial", "Municipal"];
  public levelList: string[] = ["Level 1","Level 2","Level 3","Level 4","Level 5","Huge","Mega"];

  httpClient: HttpClient;
  baseUrl: string;
  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator;
  @ViewChild(ProdHistoryComponent, { static: true }) prodHistory: ProdHistoryComponent;
  @ViewChild('progressIcon', { static: false }) progressIcon: ElementRef;

  displayedColumns: string[] = ['position', 'ip_efficiency', 'total_ip', 'influence', 'influence_bonus', 'owner_nickname', 'predict_eval_result', 'district_id', 'pos','building_id'];

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

    this.httpClient = http;
    this.baseUrl = baseUrl;
  
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

    this.httpClient.get<BuildingCollection>(this.baseUrl + 'api/plot/BuildingIPbyTypeGet', { params: params })
      .subscribe((result: BuildingCollection) => {

        this.buildingCollection = result;
        this.progressIcon.nativeElement.classList.remove("rotate");

        if (this.buildingCollection.buildings !=null && this.buildingCollection.buildings.length > 0) {

          this.dataSource = new MatTableDataSource<BuildingDetail>(this.buildingCollection.buildings);
          this.hidePaginator = this.buildingCollection.buildings.length == 0 || this.buildingCollection.buildings.length < 5 ? true : false;

          this.dataSource.paginator = this.paginator;
          if (this.dataSource.paginator) {
            this.dataSource.paginator.firstPage();
          }
          this.dataSource.sort = this.sort;

          this.sort.sort({ id: null, start: 'desc', disableClear: false }); //Clear any prior sort - reset sort arrows. best option to reset on each load.
          this.sort.sort(({ id: 'ip_efficiency', start: 'desc' }) as MatSortable);        // Default sort order on date

          // Add custom date column sort
          this.dataSource.sortingDataAccessor = (item: BuildingDetail, property) => {
            switch (property) {
              case 'predict_eval_result': return item.predict_eval_result == null ? 0 : item.predict_eval_result;
              default: return item[property];
            }
          };

        }
        else {
          this.dataSource = new MatTableDataSource<BuildingDetail>(null);
        }

      }, error => console.error(error));

    return;
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
    /*else if (building == "Office") {
      this.search(BUILDING.OFFICE, 1);
    }*/

    return;
  }

  public applyFilter = (value: string) => {
    this.dataSource.filter = value.trim().toLocaleLowerCase();
  }

  public hideHistory(componentVisible: boolean) {
    this.historyShow = !componentVisible;
  }

  showHistory(asset_id: number, pos_x: number, pos_y: number, building_type: number, ip_efficiency: number, ip_efficiency_doublebug: number) {

    this.prodHistory.searchHistory(asset_id, pos_x, pos_y, building_type, ip_efficiency, ip_efficiency_doublebug);
    this.historyShow = true;

  }

}
