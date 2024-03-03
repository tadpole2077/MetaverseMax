import { Component, Inject, ViewChild, Output, EventEmitter, ChangeDetectorRef, AfterViewInit, ElementRef, NgZone } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { DragDrop } from '@angular/cdk/drag-drop';
import { ICitizen, IPortfolioCitizen } from '../owner-data/owner-interface';
import { Clipboard } from '@angular/cdk/clipboard';
import { Globals, WORLD } from '../common/global-var';
import { interval, Subscription } from 'rxjs';

@Component({
  selector: 'app-citizen-modal',
  templateUrl: './citizen-modal.component.html',
  styleUrls: ['./citizen-modal.component.css']
})
export class CitizenModalComponent {

  @Output() hideCitizenEvent = new EventEmitter<boolean>();

  public portfolioCitizen: IPortfolioCitizen;
  public hidePaginator: boolean;
  private maticKey: string;
  public isMobileView: boolean = false;
  public notifySubscription: Subscription = null;
  public showingColumnsTraits: boolean = true;
  showTick: boolean = false;
  reset: any = null;
  refresh_state: string = "Refresh Citizens";
  processingActive: boolean = false;
  showFan: boolean = false;
  refreshActive: boolean = true;
  refreshVisible: boolean = false;

  httpClient: HttpClient;
  baseUrl: string;
  dataSource = new MatTableDataSource(null);

  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator;

  displayedColumnsTraits: string[] = ['current_price', 'token_id', 'name', 'sex', 'generation', 'breeding', 'trait_agility', 'trait_intelligence', 'trait_charisma', 'trait_endurance', 'trait_luck', 'trait_strength', 'trait_avg'];
  displayedColumnsEfficiency: string[] = ['current_price','token_id', 'name', 'sex', 'trait_avg', 'efficiency_industry', 'efficiency_production', 'efficiency_energy_water', 'efficiency_office', 'efficiency_commercial', 'efficiency_municipal', 'building_level'];
  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = this.displayedColumnsTraits;
  tableHeader: string = "Traits";

  constructor(public globals: Globals, http: HttpClient, @Inject('BASE_URL') baseUrl: string, private clipboard: Clipboard, private zone: NgZone) {

    this.httpClient = http;
    this.baseUrl = baseUrl + "api/" + globals.worldCode;

    this.portfolioCitizen = null;

    //const copiedData = JSON.stringify(this.dataSource.data);

    // Mobile View - remove secondary columns
    if (this.width < 768) {
      this.isMobileView = true;
      this.displayedColumnsTraits = ['current_price', 'name', 'sex', 'trait_agility', 'trait_intelligence', 'trait_charisma', 'trait_endurance', 'trait_luck', 'trait_strength', 'trait_avg'];
      this.displayedColumnsEfficiency = ['name', 'trait_avg', 'efficiency_industry', 'efficiency_production', 'efficiency_energy_water', 'efficiency_office', 'efficiency_commercial', 'efficiency_municipal', 'building_level'];
      this.displayedColumns = this.displayedColumnsTraits;
    }

  }

  ngAfterViewInit() {
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

  copyData() {

    let parseData: string = "";
    let counter: number = 0;
    let header: string = "";
    let copyDataset = this.portfolioCitizen.citizen;

    if (this.reset != null) {
      return;
    }

    // Control tick animation reset flag, supports reuse of animation.
    this.reset = interval(4000)
      .subscribe(
        async (val) => {
          this.showTick = false;
          this.reset.unsubscribe();
          this.reset = null;
        }
      );

    this.displayedColumns.forEach(function (key, value) {
      parseData += key + "\t";
    });

    if (this.showingColumnsTraits) {
      parseData += "trait_agility_with_pet \t";
      parseData += "trait_intelligence_with_pet \t";
      parseData += "trait_charisma_with_pet \t";
      parseData += "trait_endurance_with_pet \t";
      parseData += "trait_luck_with_pet \t";
      parseData += "trait_strength_with_pet \t";
      parseData += "trait_avg_with_pet \t";
    }
    else {
      parseData += "building_type \t";
    }
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

      if (this.showingColumnsTraits) {
        parseData += this.max10(copyDataset[counter].trait_agility, copyDataset[counter].trait_agility_pet) + "\t";
        parseData += this.max10(copyDataset[counter].trait_intelligence, copyDataset[counter].trait_intelligence_pet) + "\t";
        parseData += this.max10(copyDataset[counter].trait_charisma, copyDataset[counter].trait_charisma_pet) + "\t";
        parseData += this.max10(copyDataset[counter].trait_endurance, copyDataset[counter].trait_endurance_pet) + "\t";
        parseData += this.max10(copyDataset[counter].trait_luck, copyDataset[counter].trait_luck_pet) + "\t";
        parseData += this.max10(copyDataset[counter].trait_strength, copyDataset[counter].trait_strength_pet) + "\t";
        parseData += copyDataset[counter].trait_avg_pet + "\t";
      }
      else {
        parseData += copyDataset[counter].building_desc + "\t";
      }

      parseData += String.fromCharCode(13) + String.fromCharCode(10)
    }



    this.clipboard.copy(parseData);

    this.showTick = true;

    return;
  }

  max10(trait: number, pet: number) {
    return trait+pet > 10 ? 10 : trait+pet
  }

  search(maticKey: string, refresh: boolean) {

    if (this.portfolioCitizen && refresh == true) {
      // Prevent repeat processing while current refresh search active
      if (this.refreshActive == false) {
        return;
      }

      this.portfolioCitizen.slowdown = 120;   // apply hard 2 minute slowdown on click.
      this.checkRefresh();
    }

    this.maticKey = maticKey;
    let params = new HttpParams();
    params = params.append('owner_matic_key', maticKey);
    params = params.append('refresh', refresh == true ? "true" : "false");
    params = params.append('requester', this.globals.ownerAccount.matic_key);

    this.httpClient.get<IPortfolioCitizen>(this.baseUrl + '/ownerdata/getcitizen', { params: params })
      .subscribe({
        next: (result) => {

          // Only show the refresh link after first load of citizen table, and Pro Tools enabled.
          if (this.globals.ownerAccount.pro_tools_enabled) {
            this.refreshVisible = true;
          }
          else {            
            this.refreshVisible = false;
          }

          this.portfolioCitizen = result;

          if (this.portfolioCitizen.citizen.length > 0) {

            this.dataSource = new MatTableDataSource<ICitizen>(this.portfolioCitizen.citizen);
            this.hidePaginator = this.portfolioCitizen.citizen.length == 0 || this.portfolioCitizen.citizen.length < 10 ? true : false;

            this.dataSource.paginator = this.paginator;
            if (this.dataSource.paginator) {
              this.dataSource.paginator.firstPage();
            }
            this.dataSource.sort = this.sort;

          }
          else {
            this.dataSource = new MatTableDataSource<ICitizen>(null);
          }

          if (refresh == true) {
            this.refresh_state = "Completed - Cooldown 2 mins";
            this.showFan = true;
            this.processingActive = false;
          }

        },
        error: (error) => { console.error(error) }
        })

    return;
  }

  setHide() {
    this.showTick = false;
    this.hideCitizenEvent.emit(true);
  }

  onTableViewChange(viewType: string) {

    this.showTick = false;

    if (viewType == "traits") {
      this.displayedColumns = this.displayedColumnsTraits;
      this.showingColumnsTraits = true;
      this.tableHeader = "Traits";
    }
    else {
      this.displayedColumns = this.displayedColumnsEfficiency;
      this.showingColumnsTraits = false;
      this.tableHeader = "Efficiency %";
    }

  }

  refresh() {

    this.showTick = false;
    this.search(this.maticKey, true);

    return;
  }

  roundUp(source:number) {  
    return this.isMobileView ? Math.round(source) : source;
  }

  checkRefresh() {
    
    if (this.portfolioCitizen && this.portfolioCitizen.slowdown > 0) {

      this.refresh_state = "Processing ...";
      this.refreshActive = false;
      this.processingActive = true;

      // Showing fan, countdown controls when to remove cooldown period
      if (this.notifySubscription == null) {

        this.notifySubscription = interval(this.portfolioCitizen.slowdown * 1000).subscribe(x => {

          this.showFan = false;
          this.refreshActive = true;

          this.refresh_state = "Refresh Citizens";
          this.portfolioCitizen.slowdown = 0;

          this.notifySubscription.unsubscribe();
          this.notifySubscription = null;
        });
      }
    }

  }
}
