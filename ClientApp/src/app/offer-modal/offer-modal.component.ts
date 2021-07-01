import { Component, Inject, ViewChild, Output, EventEmitter, ChangeDetectorRef, AfterViewInit } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { DragDrop } from '@angular/cdk/drag-drop';
import { Offer, OwnerLandData, OwnerData, PlotPosition, BUILDING } from '../owner-data/owner-interface';


@Component({
  selector: 'app-offer-modal',
  templateUrl: './offer-modal.component.html',
  styleUrls: ['./offer-modal.component.css']
})
export class OfferModalComponent implements AfterViewInit {

  @Output() searchPlotEvent = new EventEmitter<any>();
  @Output() hideOfferEvent = new EventEmitter<boolean>();

  public offers: Offer[];
  public hidePaginator: boolean;
  public plot: { x: number, y: number };

  httpClient: HttpClient;
  baseUrl: string;
  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator; 


  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  //displayedColumns: string[] = ['buyer_matic_key', 'buyer_matic_name', 'buyer_offer', 'token_type', 'offer_date', 'token_district', 'token_link'];
  displayedColumns: string[] = ['offer_date', 'buyer_matic_key', 'buyer_offer', 'token_type', 'token_district'];

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {//, cdr: ChangeDetectorRef) {

    this.httpClient = http;
    this.baseUrl = baseUrl;

    this.offers = null;
    this.plot = { x: 0, y: 0 };

  }

  // Paginator wont render until loaded in call to ngAfterViewInit, as its a  @ViewChild decalare
  // AfterViewInit called after the View has been rendered, hook to this method via the implements class hook
  ngAfterViewInit() {
    //this.cdr.detectChanges();
    //this.dataSourceHistory = new MatTableDataSource<Detail>(HISTORY_ASSETS);
    //this.dataSourceHistory.paginator = this.paginator;
  }

  public loadTable(offerList: Offer[]) {

    this.offers = offerList;

    if (this.offers != null) {
      this.dataSource = new MatTableDataSource<Offer>(this.offers);
      this.hidePaginator = this.offers == null || this.offers.length < 5 ? true : false;

      this.dataSource.paginator = this.paginator;
      if (this.dataSource.paginator) {
        this.dataSource.paginator.firstPage();
      }
      this.dataSource.sort = this.sort;
    }
    else {
      this.dataSource = new MatTableDataSource<Offer>(null);
    }        

    return;
  }

  setHide() {
    this.hideOfferEvent.emit(true);
  }


}
