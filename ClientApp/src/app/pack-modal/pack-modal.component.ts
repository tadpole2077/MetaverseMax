import { Component, Inject, ViewChild, Output, EventEmitter, ChangeDetectorRef, AfterViewInit } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, MatSortable, Sort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { Pack, PRODUCT } from '../owner-data/owner-interface';
import { Clipboard } from '@angular/cdk/clipboard';
import { Globals, WORLD } from '../common/global-var';
import { PRODUCT_IMG, PRODUCT_NAME } from '../common/enum';


@Component({
  selector: 'app-pack-modal',
  templateUrl: './pack-modal.component.html',
  styleUrls: ['./pack-modal.component.css'],
  animations: [
    trigger('detailExpand', [
      state('collapsed', style({ height: '0px', minHeight: '0' })),
      state('expanded', style({ height: '*' })),
      transition('expanded <=> collapsed', animate('225ms cubic-bezier(0.4, 0.0, 0.2, 1)')),
    ]),
  ],
})
export class PackModalComponent implements AfterViewInit {

  @Output() hidePackEvent = new EventEmitter<boolean>();

  public pack: Pack[];
  public hidePaginator: boolean;
  public forceClose: boolean = false;
  public expandedRow: Pack;

  httpClient: HttpClient;
  baseUrl: string;
  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator;

  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['pack_id', 'product_id', 'amount'];

  constructor(public globals: Globals, http: HttpClient, @Inject('BASE_URL') baseUrl: string, private clipboard: Clipboard) {

    this.httpClient = http;
    this.baseUrl = baseUrl + "api/" + globals.worldCode;

    this.pack = null;

    const copiedData = JSON.stringify(this.dataSource.data);
  }

  // Paginator wont render until loaded in call to ngAfterViewInit, as its a  @ViewChild decalare
  // AfterViewInit called after the View has been rendered, hook to this method via the implements class hook
  ngAfterViewInit() {
    //this.cdr.detectChanges();
    //this.dataSourceHistory = new MatTableDataSource<Detail>(HISTORY_ASSETS);
    //this.dataSourceHistory.paginator = this.paginator;
  }

  copyData() {
    let parseData: string = "";
    let counter: number = 0;
    let header: string = "";
    let copyDataset = this.pack;

    this.displayedColumns.forEach(function (key, value) {
      parseData += key + "\t";
    });
    parseData += String.fromCharCode(13) + String.fromCharCode(10);

    // Iterate though each table row
    for (counter = 0; counter < copyDataset.length; counter++) {

      //iterate though each field find match in datasource
      this.displayedColumns.forEach(function (key, value) {
        // find match and store
        for (var prop in copyDataset[counter]) {
          if (prop == key) {
            parseData += copyDataset[counter][prop] + "\t";
          }
        }

      });
      parseData += String.fromCharCode(13) + String.fromCharCode(10)
    }

    this.clipboard.copy(parseData);

    return;
  }

  public loadPackList( packList: Pack[]) {

    this.pack = packList;

    if (this.pack != null || this.pack.length > 0) {

      this.dataSource = new MatTableDataSource<Pack>(this.pack);

      this.dataSource.paginator = this.paginator;
      if (this.dataSource.paginator) {
        this.dataSource.paginator.firstPage();
      }

      this.dataSource.sort = this.sort;
      this.sort.sort(({ id: 'product_id', start: 'asc' }) as MatSortable);        // Default sort order on date
    }
    else {
      this.dataSource = new MatTableDataSource<Pack>(null);
    }

    this.hidePaginator = this.pack.length == 0 || this.pack.length < 5 ? true : false;


    return;
  }

  getProductName(productId: number) {
    return  PRODUCT_NAME[productId];
  }

  getImageName(productId: number) {

    let productImg: string;

    switch (productId) {
      case PRODUCT.WOOD:
        productImg = PRODUCT_IMG.WOOD;
        break;      
      case PRODUCT.SAND:
        productImg = PRODUCT_IMG.SAND;
        break;
      case PRODUCT.METAL:
        productImg = PRODUCT_IMG.METAL;
        break;
      case PRODUCT.STONE:
        productImg = PRODUCT_IMG.STONE;
        break;
      case PRODUCT.BRICK:
        productImg = PRODUCT_IMG.BRICK;
        break;
      case PRODUCT.GLASS:
        productImg = PRODUCT_IMG.GLASS;
        break;
      case PRODUCT.CONCRETE:
        productImg = PRODUCT_IMG.CONCRETE;
        break;
      case PRODUCT.PLASTIC:
        productImg = PRODUCT_IMG.PLASTIC;
        break;
      case PRODUCT.PAPER:
        productImg = PRODUCT_IMG.PAPER;
        break;
      case PRODUCT.COMPOSITE:
        productImg = PRODUCT_IMG.COMPOSITE;
        break;
      case PRODUCT.GLUE:
        productImg = PRODUCT_IMG.GLUE;
        break;
      case PRODUCT.MIXES:
        productImg = PRODUCT_IMG.MIXES;
        break;
      case PRODUCT.ENERGY:
        productImg = PRODUCT_IMG.ENERGY;
        break;
      case PRODUCT.WATER:
        productImg = PRODUCT_IMG.WATER;
        break;
      default:
        productImg = "";
        break;
    }

    return productImg;
    
  }

  setHide() {
    this.hidePackEvent.emit(true);
  }

  showTransfer(row: Pack, rowIndex: number) {

    this.forceClose = false;    // Reset if previously set to true - due to use of refresh with auto closes

    return;
  }

}
