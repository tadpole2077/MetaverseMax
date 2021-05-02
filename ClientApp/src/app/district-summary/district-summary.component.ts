import { Component, Inject, ViewChild, EventEmitter, Renderer, ElementRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Router, ActivatedRoute, Params } from '@angular/router';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { AfterViewInit } from '@angular/core';
import { NoteModalComponent } from '../note-modal/note-modal.component';

// Service Interfaces
export interface OwnerSummary {
  summary_id: number,
  district_id: number;
  owner_matic: string;
  owner_nickname: string;
  owner_avatar_id: number;
  owned_plots: number;
  energy_count: number;
  industry_count: number;
  residential_count: number;
  production_count: number;
  office_count: number;
  municipal_count: number;
  poi_count: number;
  empty_count: number;
  update_instance: string;
  commercial_count: number; 
}

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
  energy_tax: number;
  production_tax: number;
  commercial_tax: number;
  citizens_tax: number;
  promotion: string;
  promotion_start: string;
  promotion_end: string;
}


@Component({
  selector: 'district-summary-data',
  templateUrl: './district-summary.component.html',
  styleUrls: ['./district-summary.component.css']
})
export class DistrictSummaryComponent implements AfterViewInit {

  httpClient: HttpClient;
  baseUrl: string;

  public district: District;
  public ownerSummary: OwnerSummary[] = new Array();        //Holds Owner Summary collection used by table
  public adShow: boolean = false;

  dataSourceOwnerSummary = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(NoteModalComponent, { static: true }) child: NoteModalComponent;

  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumnsOwners: string[] = ['owner_nickname', 'owned_plots', 'energy_count', 'industry_count', 'production_count', 'residential_count', 'office_count', 'poi_count', 'commercial_count', 'municipal_count' ];
 
  constructor(private route: ActivatedRoute, http: HttpClient, @Inject('BASE_URL') baseUrl: string, private renderer: Renderer, private elem: ElementRef)
  {
    this.httpClient = http;
    this.baseUrl = baseUrl;
    this.district = {
      update_instance: 0,
      district_name:"",
      district_id: 0,
      owner_name: "Search for an Owner, Enter Plot X and Y position, click Find Owner.",
      owner_avatar_id: 0,
      owner_url: "https://mcp3d.com/tron/api/image/citizen/0",
      owner_matic: "",
      active_from: "",
      land_count: 0,
      energy_count: 0,
      industry_count: 0,
      production_count: 0,
      office_count: 0,
      commercial_count: 0,
      municipal_count: 0,
      energy_tax:0,
      production_tax: 0,
      commercial_tax: 0,
      citizens_tax: 0,
      building_count: 0,
      plots_claimed: 0,
      promotion: "",
      promotion_start: "",
      promotion_end:""
    };

    // CHECK request Parameter, search by districtId
    let requestDistrictId = this.route.snapshot.queryParams["district_id"];
    if (requestDistrictId) {
      this.searchDistrict(requestDistrictId);
    }
  }

  ngAfterViewInit() {
    //this.dataSource.sort = this.sort;
  }

  // Single parameter struct containing 2 members, pushed by component search-plot
  searchDistrict(district_id: number ) {

    let params = new HttpParams();
    params = params.append('district_id', district_id.toString());
    this.adShow = false;

    this.httpClient.get<District>(this.baseUrl + 'api/district', { params: params })
      .subscribe((result: District) => {

        this.district = result;
        this.searchOwnerSummaryDistrict(district_id, this.district.update_instance);
        this.removeLinkHighlight();
        //plotPos.rotateEle.classList.remove("rotate");

      }, error => console.error(error));

    
    return;
  }
  

  removeLinkHighlight() {

    // Highlight current filter, and remove prior link highlight
    let districtLinkElements = this.elem.nativeElement.querySelectorAll('.districtEleActive');

    if (districtLinkElements.length) {
      for (var index = 0, element; element = districtLinkElements[index]; index++) {
        element.classList.remove("districtEleActive");
      }
    }
    return;
  }


  // Single parameter struct containing 2 members, pushed by component search-plot
  searchOwnerSummaryDistrict(districtId: number, updateInstance:number) {

    let params = new HttpParams();
    params = params.append('district_id', districtId.toString());
    params = params.append('update_instance', updateInstance.toString());


    this.httpClient.get<OwnerSummary[]>(this.baseUrl + 'api/ownersummary', { params: params })
      .subscribe((result: OwnerSummary[]) => {
        this.ownerSummary = result;

        if (this.ownerSummary) {
          this.dataSourceOwnerSummary = new MatTableDataSource<OwnerSummary>(this.ownerSummary);

          this.dataSourceOwnerSummary.sort = this.sort;
        }

        this.removeLinkHighlight();
        //plotPos.rotateEle.classList.remove("rotate");

      }, error => console.error(error));

    return;
  }

  // District ad modal popup shown - user can close or move it
  showAd(promotionText: string, promotionStart: string, promotionEnd: string) {
    this.child.adShow(promotionText, promotionStart, promotionEnd);
    this.adShow = true;

    return;
  }

  hideAd(componentVisible: boolean) {
    this.adShow = !componentVisible;
  }
}
