import { ElementRef, Component, Inject, ViewChild, Output, EventEmitter, ChangeDetectorRef, AfterViewInit, QueryList, ViewChildren } from '@angular/core';
import { interval, Observable, Subscription } from 'rxjs';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { DragDrop } from '@angular/cdk/drag-drop';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { CitizenBuildingTableComponent } from '../citizen-building-table/citizen-building-table.component';
import { Globals, WORLD } from '../common/global-var';
import { GraphDamageComponent } from '../graph-damage/graph-damage.component';

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
export class ProdHistoryComponent implements AfterViewInit {

  @Output() hideHistoryEvent = new EventEmitter<boolean>();

  public history: BuildingHistory;
  public hidePaginator: boolean;
  public plot: { x: number, y: number };
  public historyBuildingType: number;
  public isMobileView: boolean = false;
  public expandedHistory: Detail;
  public showCalcDetail: boolean = false;
  public showCalcDetailBonus: boolean = false;
  public ipEfficiency: number = -1;
  public notifySubscription: Subscription = null;
  public forceClose: boolean = false;

  assetId: number;
  httpClient: HttpClient;
  baseUrl: string;
  dataSourceHistory = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator;
  @ViewChild('progressIcon', { static: false }) progressIcon: ElementRef;
  @ViewChild('progressFan', { static: false }) progressFanIcon: ElementRef;
  @ViewChild('predictControl', { static: false }) predictControl: ElementRef;
  //@ViewChild("graphDamage", { static: true }) graphDamage: GraphDamageComponent;

  @ViewChildren(CitizenBuildingTableComponent) citizenTables: QueryList<CitizenBuildingTableComponent>;


  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[];
  columnsStandard: string[] = ['amount_produced', 'building_product', 'efficiency_p', 'efficiency_m', 'efficiency_c', 'building_ip', 'run_datetime'];
  columnsStandardMobile: string[] = ['building_product', 'efficiency', 'building_ip', 'run_datetime'];
  columnsOffice: string[] = ['run_datetime', 'building_ip', 'efficiency_c'];
  columnsFactoryMobile: string[] =['building_product', 'efficiency_c', 'building_ip', 'run_datetime'];
  columnsFactory: string[] =['amount_produced', 'building_product', 'efficiency_c', 'building_ip', 'run_datetime'];
  

  constructor(public globals: Globals, public _elementRef: ElementRef, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {//, cdr: ChangeDetectorRef) {

    this.httpClient = http;
    this.baseUrl = baseUrl + "api/" + globals.worldCode;

    this.history = null;
    this.plot = { x: 0, y: 0 };
    this.historyBuildingType = 99;

    // Mobile View - remove secondary columns
    if (this.width < 768) {
      this.isMobileView = true;
      this.displayedColumns = this.columnsStandardMobile;
    }    
  }

  // Paginator wont render until loaded in call to ngAfterViewInit, as its a  @ViewChild decalare
  // AfterViewInit called after the View has been rendered, hook to this method via the implements class hook
  ngAfterViewInit() {

    //this.cdr.detectChanges();
    //this.dataSourceHistory = new MatTableDataSource<Detail>(HISTORY_ASSETS);
    //this.dataSourceHistory.paginator = this.paginator;
  }

  // Triggered on each contentcheck event.
  ngAfterViewChecked() {
    // Show/Hide the prediction link depending on Account Privilege
    // if (this.predictControl) {
    //  if (this.globals.ownerAccount.pro_tools_enabled) {
    //    this.predictControl.nativeElement.classList.remove("hideLink");
    //  }
    //  else {
    //    this.predictControl.nativeElement.classList.add("hideLink");
    //  }
    //}

    //this.checkRefresh();
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

    this.showCalcDetail = refresh;  // Reset on each search
    this.history = null;
    let params = new HttpParams();
    this.plot = {
      x: pos_x,
      y: pos_y
    };

    this.assetId = asset_id;
    this.historyBuildingType = building_type;
    this.ipEfficiency = 0;

    params = params.append('token_id', asset_id.toString());
    //params = params.append('ip_efficiency', ip_efficiency.toString());
    //params = params.append('ip_efficiency_bonus_bug', ip_efficiency_bonus_bug.toString());
    params = params.append('full_refresh', refresh ? "1" : "0");
    params = params.append('requester', this.globals.ownerAccount.matic_key);

    //this.graphDamage.loadGraph(null);

    this.httpClient.get<BuildingHistory>(this.baseUrl + '/assethistory', { params: params })
      .subscribe((result: BuildingHistory) => {

        //if (this.progressIcon) {
        //  this.progressIcon.nativeElement.classList.remove("rotate");
        //}

        this.history = result;        
        
        if (this.history.detail != null) {

          //OFFICE
          if (building_type == 6) {
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
            this.displayedColumns = this.columnsStandard
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
              case 'run_datetime': return item.run_datetime == "" ? new Date(0) : new Date(item.run_datetime);
              default: return item[property];
            }
          };

          this.sort.sort({ id: null, start: 'desc', disableClear: false });         // Clear any prior sort - reset sort arrows. best option to reset on each load.
          //this.sort.sort({ id: 'run_datetime', start: 'desc', disableClear: true });          
          
        }
        else {
          this.dataSourceHistory = new MatTableDataSource<Detail>(null);
        }
        //plotPos.rotateEle.classList.remove("rotate");
        //this.cdr.markForCheck();
        //this.cdr.detectChanges();
        setTimeout(() => this.checkRefresh());

      }, error => console.error(error));

    return;
  }

  checkRefresh() {

    if (this.history && this.history.slowdown >0) {

      this.progressFanIcon.nativeElement.classList.remove("hideLink");
      this.progressFanIcon.nativeElement.closest("a").classList.add("refreshDisable");

      //Showing fan, countdown controls when to remove cooldown period
      if (this.notifySubscription == null) {

        //this.notifySubscription = interval(this.history.slowdown).subscribe(x => {
        this.notifySubscription = interval(this.history.slowdown * 1000).subscribe(x => {

          if (this.progressFanIcon) {
            this.progressFanIcon.nativeElement.classList.add("hideLink");
            this.progressFanIcon.nativeElement.closest("a").classList.remove("refreshDisable");
          }
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
    this.checkRefresh();
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

    // Show progress and start refresh process
    this.progressIcon.nativeElement.classList.add("rotate");
    this.searchHistory(this.assetId, this.plot.x, this.plot.y, this.historyBuildingType, true);

    return;
  }
}
