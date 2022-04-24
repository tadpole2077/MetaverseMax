import { Component, Inject, ViewChild, Output, EventEmitter, ChangeDetectorRef, AfterViewInit, ElementRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { DragDrop } from '@angular/cdk/drag-drop';
import { Citizen, PortfolioCitizen } from '../owner-data/owner-interface';
import { Clipboard } from '@angular/cdk/clipboard';


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

  httpClient: HttpClient;
  baseUrl: string;
  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator;
  @ViewChild('progressIcon', { static: false }) progressIcon: ElementRef;

  displayedColumnsTraits: string[] = ['current_price','token_id', 'name', 'sex', 'generation', 'breeding', 'trait_agility', 'trait_intelligence', 'trait_charisma', 'trait_endurance', 'trait_luck', 'trait_strength', 'trait_avg'];
  displayedColumnsEfficiency: string[] = ['current_price','token_id', 'name', 'sex', 'trait_avg', 'efficiency_industry', 'efficiency_production', 'efficiency_energy_water', 'efficiency_office', 'efficiency_commercial', 'efficiency_municipal', 'building_level'];
  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = this.displayedColumnsTraits;
  tableHeader: string = "Traits";

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string, private clipboard: Clipboard) {

    this.httpClient = http;
    this.baseUrl = baseUrl;

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

    this.httpClient.get<PortfolioCitizen>(this.baseUrl + 'api/ownerdata/getcitizen', { params: params })
      .subscribe((result: PortfolioCitizen) => {

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
}
