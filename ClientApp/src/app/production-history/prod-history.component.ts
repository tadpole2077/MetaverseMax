import { ElementRef, Component, Inject, ViewChild, Output, EventEmitter, QueryList, ViewChildren, OnDestroy } from '@angular/core';
import { interval, Subscription } from 'rxjs';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { CitizenBuildingTableComponent } from '../citizen-building-table/citizen-building-table.component';
import { Application } from '../common/global-var';
import { BUILDING } from '../common/enum';

interface Detail {
  run_datetime: string;
  amount_produced: number;
  building_product: string;
  building_product_id: number;
  efficiency_p: number;
  efficiency_m: number;
  efficiency_c: number;
  building_ip: number;
  run_datetimeDT: number;
}
interface BuildingHistory {
  startProduction: string;
  runCount: number;
  totalProduced: string[];
  detail: Detail[];
  changes_last_run: string[];
  prediction: object;
  damage: number;
  damage_eff: number;
  current_building_id: number;
  slowdown: number;
}

@Component({
    selector: 'app-prod-history',
    templateUrl: './prod-history.component.html',
    styleUrls: ['./prod-history.component.css'],
    animations: [
        trigger('detailExpand', [
            state('collapsed', style({ height: '0px', minHeight: '0' })),
            state('expanded', style({ height: '*' })),
            transition('expanded <=> collapsed', animate('225ms cubic-bezier(0.4, 0.0, 0.2, 1)')),
        ]),
        trigger('predictionDetailExpand', [
            state('collapsed', style({ height: '0px', minHeight: '0', margin:'0' })),
            state('expanded', style({ height: '*', visibility:'visible' })),
            transition('expanded <=> collapsed', animate('225ms cubic-bezier(0.4, 0.0, 0.2, 1)')),
        ]),
        trigger('predictionDetailBonusExpand', [
            state('collapsed', style({ height: '0px', minHeight: '0' })),
            state('expanded', style({ height: '*', visibility: 'visible' })),
            transition('expanded <=> collapsed', animate('225ms cubic-bezier(0.4, 0.0, 0.2, 1)')),
        ]),    
    ],
})
export class ProdHistoryComponent implements OnDestroy {

  @Output() hideHistoryEvent = new EventEmitter<boolean>();

  public history: BuildingHistory;
  public hidePaginator: boolean;
  public plot: { x: number, y: number };
  public historyBuildingType: number;
  public isMobileView = false;
  public expandedHistory: Detail;
  public showCalcDetail = false;
  public showCalcDetailBonus = false;
  public ipEfficiency = -1;
  public notifySubscription: Subscription = null;
  public forceClose = false;
  citizensOnlyView = false;

  refresh_state = 'Refresh Prediction';
  processingActive = false;
  showFan = false;
  refreshActive = true;
  refreshVisible = false;

  assetId: number;
  httpClient: HttpClient;
  baseUrl: string;
  dataSourceHistory = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator;
  @ViewChild('predictControl', { static: false }) predictControl: ElementRef;
  //@ViewChild("graphDamage", { static: true }) graphDamage: GraphDamageComponent;

  @ViewChildren(CitizenBuildingTableComponent) citizenTables: QueryList<CitizenBuildingTableComponent>;


  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[];
  columnsStandard: string[] = ['amount_produced', 'building_product', 'efficiency_p', 'efficiency_m', 'efficiency_c', 'building_ip', 'run_datetime'];
  columnsStandardMobile: string[] = ['building_product', 'efficiency', 'building_ip', 'run_datetime'];
  columnsOffice: string[] = ['run_datetime', 'building_ip', 'building_lvl', 'efficiency_c'];
  columnsFactoryMobile: string[] =['building_product', 'efficiency_c', 'building_ip', 'run_datetime'];
  columnsFactory: string[] =['amount_produced', 'building_product', 'efficiency_c', 'building_ip', 'run_datetime'];
  

  constructor(public globals: Application, public _elementRef: ElementRef, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {//, cdr: ChangeDetectorRef) {

      this.httpClient = http;
      this.baseUrl = baseUrl + 'api/' + globals.worldCode;

      this.history = null;
      this.plot = { x: 0, y: 0 };
      this.historyBuildingType = 99;

      // Mobile View - remove secondary columns
      if (this.width < 768) {
          this.isMobileView = true;
          this.displayedColumns = this.columnsStandardMobile;
      }    
  }

  ngOnDestroy() {
      //Prevent multi subscriptions relating to router change events
      if (this.notifySubscription) {
          this.notifySubscription.unsubscribe();
      }
  }

  public get width() {
      return window.innerWidth;
  }

  public searchHistory(asset_id: number, pos_x: number, pos_y: number, building_type: number, refresh: boolean) {

      if (this.history && refresh == true) {
      // Prevent repeat processing while current refresh search active
          if (this.refreshActive == false) {
              return;
          }

          this.history.slowdown = 120;   // apply hard 2 minute slowdown on click.
          this.checkRefresh();
      }


      this.showCalcDetail = refresh;  // Reset on each search
      this.history = null;
      let params = new HttpParams();
      this.plot = {
          x: pos_x,
          y: pos_y
      };

      this.assetId = asset_id;
      this.historyBuildingType = building_type;

      this.citizensOnlyView = building_type == BUILDING.OFFICE || building_type == BUILDING.COMMERCIAL ? true : false;
      this.ipEfficiency = 0;

      params = params.append('token_id', asset_id.toString());
      params = params.append('full_refresh', refresh ? '1' : '0');
      params = params.append('requester', this.globals.ownerAccount.matic_key);

      //this.graphDamage.loadGraph(null);

      this.httpClient.get<BuildingHistory>(this.baseUrl + '/assethistory', { params: params })
          .subscribe({
              next: (result) => {
          
                  // Only show the refresh link after first load of table, and Pro Tools enabled.
                  if (this.globals.ownerAccount.pro_tools_enabled) {
                      this.refreshVisible = true;
                  }
                  else {            
                      this.refreshVisible = false;
                  }     

                  this.history = result;        
        
                  if (this.history.detail != null) {

                      //OFFICE
                      if (this.citizensOnlyView ) {
                          this.displayedColumns = this.columnsOffice;
                      }
                      //FACTORY
                      else if (this.history.current_building_id == 10) {
                          this.displayedColumns = this.isMobileView == true ? this.columnsFactoryMobile : this.columnsFactory;
                      }
                      else if (this.isMobileView){
                          this.displayedColumns = this.columnsStandardMobile;
                      }
                      else{
                          this.displayedColumns = this.columnsStandard;
                      }

                      this.dataSourceHistory = new MatTableDataSource<Detail>(this.history.detail);
                      this.hidePaginator = this.history.detail == null || this.history.detail.length < 5 ? true : false;

                      this.dataSourceHistory.paginator = this.paginator;
                      if (this.dataSourceHistory.paginator) {
                          this.dataSourceHistory.paginator.firstPage();
                      }

                      this.dataSourceHistory.sort = this.sort;

                      // Add custom date column sort
                      this.dataSourceHistory.sortingDataAccessor = (item: Detail, property) => {
                          switch (property) {
                          case 'run_datetime': return item.run_datetime == '' ? new Date(0) : new Date(item.run_datetime);
                          default: return item[property];
                          }
                      };

                      this.sort.sort({ id: null, start: 'desc', disableClear: false });         // Clear any prior sort - reset sort arrows. best option to reset on each load.
                      //this.sort.sort({ id: 'run_datetime', start: 'desc', disableClear: true });          
          
                  }
                  else {
                      this.dataSourceHistory = new MatTableDataSource<Detail>(null);
                  }
         
                  if (refresh == true) {
                      this.refresh_state = 'Completed - Cooldown 2 mins';
                      this.showFan = true;
                      this.processingActive = false;
                  }

              },
              error: (error) => { console.error(error); }
          });

      return;
  }

  checkRefresh() {

      if (this.history && this.history.slowdown >0) {

          this.refresh_state = 'Processing ...';
          this.refreshActive = false;
          this.processingActive = true;

          //Showing fan, countdown controls when to remove cooldown period
          if (this.notifySubscription == null) {

              //this.notifySubscription = interval(this.history.slowdown).subscribe(x => {
              this.notifySubscription = interval(this.history.slowdown * 1000).subscribe(x => {

                  this.showFan = false;
                  this.refreshActive = true;

                  this.refresh_state = 'Refresh Prediction';
                  this.history.slowdown = 0;

                  this.notifySubscription.unsubscribe();
                  this.notifySubscription = null;
              });
          }      
      }

  }

  setHide() {    
      this.hideHistoryEvent.emit(true);
      this.showCalcDetailBonus = false;
      this.showCalcDetail = false;
  }

  getCitizenData(historyItem: Detail, rowIndex: number) {

      this.forceClose = false;    // Reset if previously se to true - due to use of refresh with auto closes any opened cit child table
      const subTable = this.citizenTables.filter((element) => element.index === rowIndex)[0];
      subTable.search(this.assetId, historyItem.run_datetimeDT);

      return;
  }

  // Event trigger by parent table pagination click event - by setting the expandedHistory var to null, this triggers the animation [@detailExpand]
  // whose value will now set to 'collapsed' via the html trigger expression which depends on expandedHistory having a row item  assigned.
  paginationCloseAllExpanded(pageEvent: PageEvent) {

      this.expandedHistory = null;

  }

  toggleDetail(event: Event) {

      this.showCalcDetail = !this.showCalcDetail;
      //this.checkRefresh();
      //console.log('value:', (event.target as HTMLAnchorElement).innerHTML);
    
      //(event.target as HTMLAnchorElement).innerHTML = this.showCalcDetail ? "Hide Calculation" : "Show Calculation";
      return;
  }

  toggleDetailBonus(event: Event) {

      this.showCalcDetailBonus = !this.showCalcDetailBonus;

      return;
  }

  refresh() {
      // Close any open citizen child-table
      this.forceClose = true;
      this.expandedHistory = null;
    
      this.searchHistory(this.assetId, this.plot.x, this.plot.y, this.historyBuildingType, true);

      return;
  }
}
