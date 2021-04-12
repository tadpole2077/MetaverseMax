import { Component, Inject, ViewChild, Output, EventEmitter } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';

interface Detail {
  run_datetime: string;
  amount_produced: number;
  buildingProduct: number;
  efficiency: number;
}
interface BuildingHistory {
  startProduction: string;
  runCount: number;
  totalProduced: string[];
  detail: Detail[];
}

let HISTORY_ASSETS: Detail[] = null;


@Component({
  selector: 'app-prod-history',
  templateUrl: './prod-history.component.html',
  styleUrls: ['./prod-history.component.css']
})
export class ProdHistoryComponent {

  @Output() searchPlotEvent = new EventEmitter<any>();
  @Output() hideHistoryEvent = new EventEmitter<boolean>();

  public history: BuildingHistory;
  public hidePaginator: boolean;
  httpClient: HttpClient;
  baseUrl: string;
  dataSourceHistory = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator;

  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['amount_produced', 'buildingProduct', 'efficiency', 'run_datetime'];


  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

    this.httpClient = http;
    this.baseUrl = baseUrl;

    this.history = null;
    this.dataSourceHistory.paginator = this.paginator;
    //this.searchHistory(9187);
  }

  public searchHistory(asset_id: number) {

    let params = new HttpParams();
    params = params.append('asset_id', asset_id.toString());

    this.httpClient.get<BuildingHistory>(this.baseUrl + 'api/assethistory', { params: params })
      .subscribe((result: BuildingHistory) => {
        this.history = result;

        if (this.history.detail != null) {
          this.dataSourceHistory = new MatTableDataSource<Detail>(this.history.detail);
          this.hidePaginator = this.history.detail == null || this.history.detail.length < 5 ? true : false;

          this.dataSourceHistory.paginator = this.paginator;
          if (this.dataSourceHistory.paginator) {
            this.dataSourceHistory.paginator.firstPage();
          }
          this.dataSourceHistory.sort = this.sort;
        }
        else {
          this.dataSourceHistory = new MatTableDataSource<Detail>(null);
        }
        //plotPos.rotateEle.classList.remove("rotate");

      }, error => console.error(error));

    return;
  }

  setHide() {
    this.hideHistoryEvent.emit(true);
  }

  GetPlotData(plotPos) {

    plotPos.rotateEle = document.getElementById("searchIcon")
    plotPos.rotateEle.classList.add("rotate");
      
    this.searchPlotEvent.emit(plotPos);
  }



}
