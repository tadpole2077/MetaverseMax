import { Component, Inject, ViewChild, Output, EventEmitter, ChangeDetectorRef, AfterViewInit, QueryList, ViewChildren } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, MatSortable, Sort } from '@angular/material/sort';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { TransferAssetComponent } from '../transfer-asset/transfer-asset.component';
import { IPack } from '../owner-data/owner-interface';
import { Clipboard } from '@angular/cdk/clipboard';
import { Application, WORLD } from '../common/global-var';
import { MapData } from '../common/map-data';
import { PRODUCT_IMG, PRODUCT_NAME, PRODUCT } from '../common/enum';


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
export class PackModalComponent {

  @Output() hidePackEvent = new EventEmitter<boolean>();

  public pack: IPack[];
  public hidePaginator: boolean;
  public forceClose = false;
  public expandedRow: IPack;

  httpClient: HttpClient;
  baseUrl: string;
  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator;

  /* Array of all subtable  TransferAsset components - one per row*/
  @ViewChildren(TransferAssetComponent) transferAssetList: QueryList<TransferAssetComponent>;

  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['pack_id', 'product_id', 'amount'];

  constructor(public globals: Application, public mapdata: MapData, http: HttpClient, @Inject('BASE_URL') baseUrl: string, private clipboard: Clipboard) {

      this.httpClient = http;
      this.baseUrl = baseUrl + 'api/' + globals.worldCode;

      this.pack = null;

  }

  // Event trigger by parent table PAGINATION click event - by setting the expandedRow var to null, this triggers the animation [@detailExpand]
  // whose value will now set to 'collapsed' via the html trigger expression which depends on expandedHistory having a row item  assigned.
  paginationCloseAllExpanded(pageEvent: PageEvent) {

      this.expandedRow = null;

  }

  refresh() {
      // Close any open child-table
      this.forceClose = true;
      this.expandedRow = null;

      // Show progress and start refresh process
      //this.progressIcon.nativeElement.classList.add("rotate");
      //this.searchHistory(this.assetId, this.plot.x, this.plot.y, this.historyBuildingType, true);

      return;
  }

  copyData() {
      let parseData = '';
      let counter = 0;
      const header = '';
      const copyDataset = this.pack;

      this.displayedColumns.forEach(function (key, value) {
          parseData += key + '\t';
      });
      parseData += String.fromCharCode(13) + String.fromCharCode(10);

      // Iterate though each table row
      for (counter = 0; counter < copyDataset.length; counter++) {

          //iterate though each field find match in datasource
          this.displayedColumns.forEach(function (key, value) {
              // find match and store
              for (const prop in copyDataset[counter]) {
                  if (prop == key) {
                      parseData += copyDataset[counter][prop] + '\t';
                  }
              }

          });
          parseData += String.fromCharCode(13) + String.fromCharCode(10);
      }

      this.clipboard.copy(parseData);

      return;
  }

  public loadPackList( packList: IPack[]) {

      this.expandedRow = null;      // init as may contain prior expanded row if Pack component show and expanded on prior usage.
      this.pack = packList;

      if (this.pack != null || this.pack.length > 0) {

          this.dataSource = new MatTableDataSource<IPack>(this.pack);

          this.dataSource.paginator = this.paginator;
          if (this.dataSource.paginator) {
              this.dataSource.paginator.firstPage();
          }

          this.dataSource.sort = this.sort;
          this.sort.sort(({ id: 'product_id', start: 'asc' }) as MatSortable);        // Default sort order on date
      }
      else {
          this.dataSource = new MatTableDataSource<IPack>(null);
      }

      this.hidePaginator = this.pack.length == 0 || this.pack.length < 5 ? true : false;


      return;
  }

  setHide() {
      this.hidePackEvent.emit(true);
  }

  showTransfer(row: IPack, rowIndex: number) {

      this.forceClose = false;    // Reset if previously set to true -  Used with "refresh" feature - to auto close any opened sub table (remove if not uses refresh)

      // Retrive loaded subtable component - index assigned to component within this components htrml directive on load, and assigned to child component.
      const transferAsset = this.transferAssetList.filter((element) => element.index === rowIndex)[0];

      //transferAsset.loadPlotData(row);

      return;
  }

}
