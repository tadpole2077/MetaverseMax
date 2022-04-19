import { Component, Inject, ViewChild, Output, Input, EventEmitter, ChangeDetectorRef, AfterViewInit, ElementRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { Citizen, BUILDING, PRODUCT } from '../owner-data/owner-interface';


@Component({
  selector: 'app-citizen-building-table',
  templateUrl: './citizen-building-table.component.html',
  styleUrls: ['./citizen-building-table.component.css']
})
export class CitizenBuildingTableComponent {

  @Output() hideCitizenEvent = new EventEmitter<boolean>();
  @Input() index: number;
  @Input() buildingType: number;
  @Input() productType: number;

  public citizenList: Citizen[];
  public hidePaginator: boolean;

  httpClient: HttpClient;
  baseUrl: string;
  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator;  

  displayedColumns: string[] = ['token_id', 'name', 'trait_agility', 'trait_intelligence', 'trait_charisma', 'trait_endurance', 'trait_luck', 'trait_strength', 'trait_avg'];

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

    this.httpClient = http;
    this.baseUrl = baseUrl;

    this.citizenList = null;

  }

  public get width() {
    return window.innerWidth;
  }

  search(tokenId: number, productionDate: number) {

    // Mobile View - remove secondary columns
    if (this.width < 768) {
      this.displayedColumns = ['name', 'trait_avg']

      if (this.buildingType == BUILDING.INDUSTRIAL) {
        this.displayedColumns.push('trait_strength');
        this.displayedColumns.push('trait_endurance');
      }
      else if (this.buildingType == BUILDING.PRODUCTION) {
        this.displayedColumns.push('trait_agility');
        this.displayedColumns.push('trait_strength');
      }
      else if (this.buildingType == BUILDING.ENERGY) {
        this.displayedColumns.push('trait_endurance');
        this.displayedColumns.push('trait_agility');
      }
    }
    else {
      this.displayedColumns = ['token_id', 'name', 'trait_agility', 'trait_intelligence', 'trait_charisma', 'trait_endurance', 'trait_luck', 'trait_strength', 'trait_avg']
    }

    // Set default sort on efficiency column, add matching efficiency column to parent building.
    if (this.buildingType == BUILDING.INDUSTRIAL) {
      this.displayedColumns.push('efficiency_industry');

      // Only assign sort order once, as rows can be collapsed and expaned again causes a double sort and wrong direction.
      if (this.dataSource.sort != this.sort) {
        this.sort.sort({ id: 'efficiency_industry', start: 'desc', disableClear: true });
      }
    }
    else if (this.buildingType == BUILDING.PRODUCTION) {
      this.displayedColumns.push('efficiency_production');
      if (this.dataSource.sort != this.sort) {
        this.sort.sort({ id: 'efficiency_production', start: 'desc', disableClear: true });
      }
    }
    else if (this.buildingType == BUILDING.ENERGY) {
      this.displayedColumns.push('efficiency_energy');
      if (this.dataSource.sort != this.sort) {
        this.sort.sort({ id: 'efficiency_energy', start: 'desc', disableClear: true });
      }
    }


    let params = new HttpParams();
    params = params.append('token_id', tokenId.toString());
    params = params.append('production_date', productionDate.toString());

    this.httpClient.get<Citizen[]>(this.baseUrl + 'api/assetHistory/getCitizenHistory', { params: params })
      .subscribe((result: Citizen[]) => {

        this.citizenList = result;

        if (this.citizenList.length > 0) {

          this.dataSource = new MatTableDataSource<Citizen>(this.citizenList);
          this.hidePaginator = this.citizenList.length == 0 || this.citizenList.length < 5 ? true : false;

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

}
