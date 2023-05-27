import { Component, Inject, ViewChild, EventEmitter, ElementRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Router, ActivatedRoute, Params } from '@angular/router';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { AfterViewInit } from '@angular/core';
import { Globals, WORLD } from '../common/global-var';


interface District {
  update_instance: number;
  district_id: number;
  district_name: string;
  land_count: number;
  building_count: number;
  plots_claimed: number;
  energy_count: number;
  industry_count: number;
  production_count: number;
  office_count: number;
  commercial_count: number;
  municipal_count: number;
  owner_name: string;
  owner_avatar_id: number;
  owner_url: string;
  owner_matic: string;
  active_from: string;

  construction_energy_tax: number;
  construction_industry_production_tax: number;
  construction_commercial_tax: number;
  construction_municipal_tax: number;
  construction_residential_tax: number;

  energy_tax: number;
  production_tax: number;
  commercial_tax: number;
  citizen_tax: number;

  promotion: string;
  promotion_start: string;
  promotion_end: string;

}



@Component({
  selector: 'district-list-data',
  templateUrl: './district-list.component.html',
  styleUrls: ['./district-list.component.css']
})
export class DistrictListComponent implements AfterViewInit {

  httpClient: HttpClient;
  baseUrl: string;
  public districtList: District [] = new Array();

  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;

  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['district_id', 'district_name', 'owner_name', 'land_count', 'plots_claimed', 'construction_energy_tax', 'construction_industry_production_tax', 'construction_commercial_tax', 'construction_municipal_tax', 'construction_residential_tax', 'energy_tax', 'production_tax', 'commercial_tax', 'citizen_tax'];
 
  constructor(public globals: Globals, public router: Router, http: HttpClient, @Inject('BASE_URL') baseUrl: string, private elem: ElementRef)
  {
    this.httpClient = http; 
    this.baseUrl = baseUrl + "api/" + globals.worldCode;

    this.searchAllDistrict();
  }

  ngAfterViewInit() {
    //this.dataSource.sort = this.sort;
  }

  // 
  searchAllDistrict() {

    let params = new HttpParams();
    params = params.append('opened', 'true');
    params = params.append('includeTaxHistory', 'false');

    this.httpClient.get<District[]>(this.baseUrl + '/district/get_all', { params: params })
      .subscribe((result: District[]) => {

        this.districtList = result;

        if (this.districtList) {
          this.dataSource = new MatTableDataSource<District>(this.districtList);

          this.dataSource.sort = this.sort;
        }
        //this.removeLinkHighlight();
        //plotPos.rotateEle.classList.remove("rotate");

      }, error => console.error(error));

    
    return;
  }  

  // Single parameter struct containing 2 members, pushed by component search-plot
  searchDistrict(district_id: number) {
    this.router.navigate(['/' + this.globals.worldCode + '/district-summary'], { queryParams: { district_id: district_id } });
  }

}
