import { Component, Inject, ViewChild, Output, Input, EventEmitter, ChangeDetectorRef, AfterViewInit, ElementRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { ICitizen } from '../owner-data/owner-interface';
import { Application, WORLD } from '../common/global-var';
import { BUILDING, PRODUCT } from '../common/enum';


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

  public citizenList: ICitizen[];
  public hidePaginator: boolean;

  httpClient: HttpClient;
  baseUrl: string;
  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator;  

  displayedColumns: string[] = ['token_id', 'name', 'trait_agility', 'trait_intelligence', 'trait_charisma', 'trait_endurance', 'trait_luck', 'trait_strength', 'trait_avg'];

  constructor(public globals: Application, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

      this.httpClient = http;
      this.baseUrl = baseUrl + 'api/' + globals.worldCode;

      this.citizenList = null;

  }
  
  public get width() {
      return window.innerWidth;
  }

  search(tokenId: number, productionDate: number) {

      // Mobile View - remove secondary columns
      if (this.width < 768) {
          this.displayedColumns = ['name', 'trait_avg'];

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
          else if (this.buildingType == BUILDING.OFFICE) {
              this.displayedColumns.push('trait_intelligence');
              this.displayedColumns.push('trait_charisma');
          }
      }
      else {
          this.displayedColumns = ['token_id', 'name', 'trait_agility', 'trait_intelligence', 'trait_charisma', 'trait_endurance', 'trait_luck', 'trait_strength', 'trait_avg'];
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
      else if (this.buildingType == BUILDING.ENERGY && this.productType == PRODUCT.ENERGY) {
          this.displayedColumns.push('efficiency_energy_electric');
          if (this.dataSource.sort != this.sort) {
              this.sort.sort({ id: 'efficiency_energy_electric', start: 'desc', disableClear: true });
          }
      }
      else if (this.buildingType == BUILDING.ENERGY && this.productType == PRODUCT.WATER) {
          this.displayedColumns.push('efficiency_energy_water');
          if (this.dataSource.sort != this.sort) {
              this.sort.sort({ id: 'efficiency_energy_water', start: 'desc', disableClear: true });
          }
      }
      else if (this.buildingType == BUILDING.OFFICE) {
          this.displayedColumns.push('efficiency_office');
          if (this.dataSource.sort != this.sort) {
              this.sort.sort({ id: 'efficiency_office', start: 'desc', disableClear: true });
          }
      }

      let params = new HttpParams();
      params = params.append('token_id', tokenId.toString());
      params = params.append('production_date', productionDate.toString());

      this.httpClient.get<ICitizen[]>(this.baseUrl + '/assetHistory/getCitizenHistory', { params: params })
          .subscribe({
              next: (result) => {

                  this.citizenList = result;

                  if (this.citizenList.length > 0) {

                      this.dataSource = new MatTableDataSource<ICitizen>(this.citizenList);
                      this.hidePaginator = this.citizenList.length == 0 || this.citizenList.length < 5 ? true : false;

                      this.dataSource.paginator = this.paginator;
                      if (this.dataSource.paginator) {
                          this.dataSource.paginator.firstPage();
                      }
                      this.dataSource.sort = this.sort;

                  }
                  else {
                      this.dataSource = new MatTableDataSource<ICitizen>(null);
                  }       

              },
              error: (error) => { console.error(error); }
          });

      return;
  }

  setHide() {
      this.hideCitizenEvent.emit(true);
  }

}
