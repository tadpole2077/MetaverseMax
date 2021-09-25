import { Component, Inject, ViewChild, Output, EventEmitter, ChangeDetectorRef, AfterViewInit } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { DragDrop } from '@angular/cdk/drag-drop';
import { Citizen } from '../owner-data/owner-interface';
import { Clipboard } from '@angular/cdk/clipboard';


@Component({
  selector: 'app-citizen-modal',
  templateUrl: './citizen-modal.component.html',
  styleUrls: ['./citizen-modal.component.css']
})
export class CitizenModalComponent {

  @Output() hideCitizenEvent = new EventEmitter<boolean>();

  public citizenList: Citizen[];
  public hidePaginator: boolean;

  httpClient: HttpClient;
  baseUrl: string;
  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator; 

  displayedColumnsTraits: string[] = ['token_id', 'name', 'sex', 'generation', 'breeding', 'sex', 'trait_agility', 'trait_intelligence', 'trait_charisma', 'trait_endurance', 'trait_luck', 'trait_strength', 'trait_avg'];
  displayedColumnsEfficiency: string[] = ['token_id', 'name', 'sex', 'trait_avg', 'efficiency_industry', 'efficiency_production', 'efficiency_energy', 'efficiency_office', 'efficiency_commercial', 'efficiency_municipal', 'building_level'];
  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = this.displayedColumnsTraits;
  tableHeader: string = "Traits";

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string, private clipboard: Clipboard) {

    this.httpClient = http;
    this.baseUrl = baseUrl;

    this.citizenList = null;

    const copiedData = JSON.stringify(this.dataSource.data);
  }

  copyData() {

    let parseData: string = "";
    let counter: number = 0;
    let header: string = "";
    let copyDataset = this.citizenList;

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

  search(maticKey: string) {

    let params = new HttpParams();
    params = params.append('owner_matic_key', maticKey);

    this.httpClient.get<Citizen[]>(this.baseUrl + 'api/ownerdata/getcitizen', { params: params })
      .subscribe((result: Citizen[]) => {

        this.citizenList = result;

        if (this.citizenList.length > 0) {

          this.dataSource = new MatTableDataSource<Citizen>(this.citizenList);
          this.hidePaginator = this.citizenList.length == 0 || this.citizenList.length < 10 ? true : false;

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

}
