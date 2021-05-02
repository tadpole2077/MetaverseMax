import { Component, Inject, ViewChild, Output, EventEmitter, ChangeDetectorRef, AfterViewInit } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { DragDrop } from '@angular/cdk/drag-drop';

interface Detail {
  run_datetime: string;
  amount_produced: number;
  buildingProduct: number;
  efficiency_p: number;
  efficiency_m: number;
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
export class ProdHistoryComponent implements AfterViewInit {

  @Output() searchPlotEvent = new EventEmitter<any>();
  @Output() hideHistoryEvent = new EventEmitter<boolean>();

  public history: BuildingHistory;
  public hidePaginator: boolean;
  public plot: { x: number, y: number };

  httpClient: HttpClient;
  baseUrl: string;
  dataSourceHistory = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator; 

  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['amount_produced', 'buildingProduct', 'efficiency_p', 'efficiency_m', 'run_datetime'];

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {//, cdr: ChangeDetectorRef) {

    this.httpClient = http;
    this.baseUrl = baseUrl;

    this.history = null;
    this.plot = { x: 0, y: 0 };

  }

  // Paginator wont render until loaded in call to ngAfterViewInit, as its a  @ViewChild decalare
  // AfterViewInit called after the View has been rendered, hook to this method via the implements class hook
  ngAfterViewInit() {
    //this.cdr.detectChanges();
    //this.dataSourceHistory = new MatTableDataSource<Detail>(HISTORY_ASSETS);
    //this.dataSourceHistory.paginator = this.paginator;
  }

  public searchHistory(asset_id: number, pos_x: number, pos_y:number) {

    let params = new HttpParams();
    this.plot = {
      x: pos_x,
      y: pos_y
    };

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
        //this.cdr.markForCheck();
        //this.cdr.detectChanges();
        

      }, error => console.error(error));

    return;
  }

  setHide() {
    this.hideHistoryEvent.emit(true);
  }


}
