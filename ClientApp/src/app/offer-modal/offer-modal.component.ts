import { Component, Inject, ViewChild, Output, EventEmitter, ChangeDetectorRef, AfterViewInit } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { MatTabGroup } from '@angular/material/tabs';
import { DragDrop } from '@angular/cdk/drag-drop';
import { Offer } from '../owner-data/owner-interface';


@Component({
  selector: 'app-offer-modal',
  templateUrl: './offer-modal.component.html',
  styleUrls: ['./offer-modal.component.css']
})
export class OfferModalComponent implements AfterViewInit {

  @Output() hideOfferEvent = new EventEmitter<boolean>();

  public offers: Offer[];
  public offersSold: Offer[];
  public hidePaginator: boolean;
  public hidePaginatorSold: boolean;
  public lastUpdated: string = null;

  httpClient: HttpClient;
  baseUrl: string;
  dataSource = new MatTableDataSource(null);
  dataSourceSold = new MatTableDataSource(null);

  @ViewChild(MatTabGroup, { static: true }) tabGroup: MatTabGroup;
  @ViewChild("sorterOffer", { static: true }) sorterOffer: MatSort;
  @ViewChild("sorterSold", { static: true }) sorterSold: MatSort;
  @ViewChild("paginatorOffer", { static: false }) paginatorOffer: MatPaginator;
  @ViewChild("paginatorSold", { static: false }) paginatorSold: MatPaginator;


  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['offer_date', 'buyer_matic_key', 'buyer_offer', 'token_type', 'token_district'];

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {//, cdr: ChangeDetectorRef) {

    this.httpClient = http;
    this.baseUrl = baseUrl;

    this.offers = null;

  }

  // AfterViewInit called after the View has been rendered, hook to this method via the implements class hook
  ngAfterViewInit() {
    
  }

  public loadTable(offerList: Offer[], offerSoldList: Offer[], lastUpdated: string) {

    this.tabGroup.selectedIndex = 0;

    this.lastUpdated = lastUpdated;
    this.offers = offerList;
    this.offersSold = offerSoldList;

    if (this.offers != null) {
      this.dataSource = new MatTableDataSource<Offer>(this.offers);
      this.dataSourceSold = new MatTableDataSource<Offer>(this.offersSold);

      this.hidePaginator = this.offers == null || this.offers.length < 5 ? true : false;
      this.hidePaginatorSold = this.offersSold == null || this.offersSold.length < 5 ? true : false;

      this.dataSource.paginator = this.paginatorOffer;
      this.dataSourceSold.paginator = this.paginatorSold;

      if (this.dataSource.paginator) {
        this.dataSource.paginator.firstPage();
      }
      if (this.dataSourceSold.paginator) {
        this.dataSourceSold.paginator.firstPage();
      }

      this.dataSource.sort = this.sorterOffer;
      this.dataSourceSold.sort = this.sorterSold;

    }
    else {
      this.dataSource = new MatTableDataSource<Offer>(null);
      this.dataSourceSold = new MatTableDataSource<Offer>(null);
    }        

    this.dataSource.sortingDataAccessor = (item: Offer, property) => {
      switch (property) {
        case 'offer_date': return new Date(item.offer_date);
        default: return item[property];
      }
    };

    this.dataSourceSold.sortingDataAccessor = (item: Offer, property) => {
      switch (property) {
        case 'offer_date': return new Date(item.offer_date);
        default: return item[property];
      }
    };

    return;
  }

  setHide() {
    this.hideOfferEvent.emit(true);
  }

  sortData(sort: MatSort) {

    const data = this.offersSold.slice();

    //if (!sort.active || sort.direction === '') {
    //  this.sortedData = data;
    //  return;
    //}

    this.offersSold = this.offersSold.sort((a, b) => {
      const isAsc = sort.direction === 'asc';

      switch (sort.active) {
        case 'offer_date': return compareDate(a.offer_date, b.offer_date, isAsc);
        default: return 0;
      }    
    });
  }
 
}

function compareDate(a: string, b: string, isAsc: boolean) {

  return (convertDate(a) < convertDate(b) ? -1 : 1) * (isAsc ? 1 : -1);
}

function compare(a: number | string, b: number | string, isAsc: boolean) {
  return (a < b ? -1 : 1) * (isAsc ? 1 : -1);
}

function convertDate(sourceDate: string) {

  return sourceDate.replace("/Jan/", "01")
    .replace("/Feb/", "02")
    .replace("/Mar/", "03")
    .replace("/Apr/", "04")
    .replace("/May/", "05")
    .replace("/Jun/", "06")
    .replace("/Jul/", "07")
    .replace("/Aug/", "08")
    .replace("/Sep/", "09")
    .replace("/Oct/", "10")
    .replace("/Nov/", "11")
    .replace("/Dec/", "12");
}
