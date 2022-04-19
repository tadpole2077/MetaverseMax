import { Component, Inject, ViewChild, Output, EventEmitter, ChangeDetectorRef, AfterViewInit } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { DragDrop } from '@angular/cdk/drag-drop';
import { PortfolioPet, Pet } from '../owner-data/owner-interface';
import { Clipboard } from '@angular/cdk/clipboard';

//let HISTORY_ASSETS: Detail[] = null;


@Component({
  selector: 'app-pet-modal',
  templateUrl: './pet-modal.component.html',
  styleUrls: ['./pet-modal.component.css']
})
export class PetModalComponent implements AfterViewInit {

  @Output() hidePetEvent = new EventEmitter<boolean>();

  public portfolioPet: PortfolioPet;
  public hidePaginator: boolean;

  httpClient: HttpClient;
  baseUrl: string;
  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator; 

  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['token_id', 'name', 'trait', 'level'];

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string, private clipboard: Clipboard) {

    this.httpClient = http;
    this.baseUrl = baseUrl;

    this.portfolioPet = null;

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
    let copyDataset = this.portfolioPet.pet;

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

  public searchPets(maticKey: string) {

    let params = new HttpParams();
    params = params.append('owner_matic_key', maticKey);

    this.httpClient.get<PortfolioPet>(this.baseUrl + 'api/ownerdata/getpet', { params: params })
      .subscribe((result: PortfolioPet) => {

        this.portfolioPet = result;

        if (this.portfolioPet.pet_count > 0) {

          this.dataSource = new MatTableDataSource<Pet>(this.portfolioPet.pet);          

          this.dataSource.paginator = this.paginator;
          if (this.dataSource.paginator) {
            this.dataSource.paginator.firstPage();
          }

          this.dataSource.sort = this.sort;
        }
        else {
          this.dataSource = new MatTableDataSource<Pet>(null);
        }
        this.hidePaginator = this.portfolioPet.pet_count == 0 || this.portfolioPet.pet_count < 5 ? true : false;

      }, error => console.error(error));

    return;
  }

  setHide() {
    this.hidePetEvent.emit(true);
  }

}
