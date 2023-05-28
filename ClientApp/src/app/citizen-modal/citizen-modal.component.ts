import { Component, Inject, ViewChild, Output, EventEmitter, ChangeDetectorRef, AfterViewInit, ElementRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatLegacyTableDataSource as MatTableDataSource } from '@angular/material/legacy-table';
import { MatSort } from '@angular/material/sort';
import { MatLegacyPaginator as MatPaginator } from '@angular/material/legacy-paginator';
import { DragDrop } from '@angular/cdk/drag-drop';
import { Citizen, PortfolioCitizen } from '../owner-data/owner-interface';
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

  public portfolioCitizen: PortfolioCitizen;
  public hidePaginator: boolean;
  private maticKey: string;
  public isMobileView: boolean = false;
  public notifySubscription: Subscription = null;

  httpClient: HttpClient;
  baseUrl: string;
  dataSource = new MatTableDataSource(null);

  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator;
  @ViewChild('progressIcon', { static: false }) progressIcon: ElementRef;
  @ViewChild('refreshLink', { static: false }) refreshLink: ElementRef;
  @ViewChild('progressFan', { static: false }) progressFanIcon: ElementRef;

  displayedColumnsTraits: string[] = ['current_price','token_id', 'name', 'sex', 'generation', 'breeding', 'trait_agility', 'trait_intelligence', 'trait_charisma', 'trait_endurance', 'trait_luck', 'trait_strength', 'trait_avg'];
  displayedColumnsEfficiency: string[] = ['current_price','token_id', 'name', 'sex', 'trait_avg', 'efficiency_industry', 'efficiency_production', 'efficiency_energy_water', 'efficiency_office', 'efficiency_commercial', 'efficiency_municipal', 'building_level'];
  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = this.displayedColumnsTraits;
  tableHeader: string = "Traits";

  constructor(public globals: Globals, http: HttpClient, @Inject('BASE_URL') baseUrl: string, private clipboard: Clipboard) {

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

  search(maticKey: string, refresh: boolean) {

    this.maticKey = maticKey;
    let params = new HttpParams();
    params = params.append('owner_matic_key', maticKey);
    params = params.append('refresh', refresh == true ? "true" : "false");
    params = params.append('requester', this.globals.ownerAccount.matic_key);

    this.httpClient.get<PortfolioCitizen>(this.baseUrl + '/ownerdata/getcitizen', { params: params })
      .subscribe((result: PortfolioCitizen) => {

        if (this.globals.ownerAccount.pro_tools_enabled && this.refreshLink) {
          this.refreshLink.nativeElement.classList.remove("hideLink");
        }

        this.progressIcon.nativeElement.classList.remove("rotate");
        this.portfolioCitizen = result;

        if (this.portfolioCitizen.citizen.length > 0) {

          this.dataSource = new MatTableDataSource<Citizen>(this.portfolioCitizen.citizen);
          this.hidePaginator = this.portfolioCitizen.citizen.length == 0 || this.portfolioCitizen.citizen.length < 10 ? true : false;

          this.dataSource.paginator = this.paginator;
          if (this.dataSource.paginator) {
            this.dataSource.paginator.firstPage();
          }
          this.dataSource.sort = this.sort;

        }
        else {
          this.dataSource = new MatTableDataSource<Citizen>(null);
        }

        setTimeout(() => this.checkRefresh());

      }, error => console.error(error));

    return;
  }

  setHide() {
    this.hideCitizenEvent.emit(true);
  }

  onTableViewChange(viewType:string) {

    if (viewType == "traits") {
      this.displayedColumns = this.displayedColumnsTraits;
      this.tableHeader = "Traits";
    }
    else {
      this.displayedColumns = this.displayedColumnsEfficiency;
      this.tableHeader = "Efficiency %";
    }

  }

  refresh() {

    this.progressIcon.nativeElement.classList.add("rotate");
    this.search(this.maticKey, true);

    return;
  }

  roundUp(source:number) {  
    return this.isMobileView ? Math.round(source) : source;
  }

  checkRefresh() {

    if (this.portfolioCitizen && this.portfolioCitizen.slowdown > 0) {

      this.progressFanIcon.nativeElement.classList.remove("hideLink");
      this.progressFanIcon.nativeElement.closest("a").classList.add("refreshDisable");

      //Showing fan, countdown controls when to remove cooldown period
      if (this.notifySubscription == null) {

        //this.notifySubscription = interval(this.history.slowdown).subscribe(x => {
        this.notifySubscription = interval(this.portfolioCitizen.slowdown * 1000).subscribe(x => {

          if (this.progressFanIcon) {
            this.progressFanIcon.nativeElement.classList.add("hideLink");
            this.progressFanIcon.nativeElement.closest("a").classList.remove("refreshDisable");
          }
          this.portfolioCitizen.slowdown = 0;

          this.notifySubscription.unsubscribe();
          this.notifySubscription = null;
        });
      }
    }

  }
}
