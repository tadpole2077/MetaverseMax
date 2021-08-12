import { Component, Inject, ViewChild, EventEmitter, ElementRef, ViewChildren } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Router, ActivatedRoute, Params } from '@angular/router';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { MatFormFieldModule } from '@angular/material/form-field';
/*import { MatInputModule, MatExpansionModule, MatIconModule, MatCheckboxModule, MatCheckboxChange, MatCheckbox } from '@angular/material';*/
import { MatCheckbox, MatCheckboxChange } from '@angular/material/checkbox';
import { AfterViewInit } from '@angular/core';
import { NoteModalComponent } from '../note-modal/note-modal.component';
import { TaxGraphComponent } from '../tax-graph/tax-graph.component';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { OwnerSummary, District } from './data-district-interface';
import { GraphData } from '../common/graph-interface';

@Component({
  selector: 'district-summary-data',
  templateUrl: './district-summary.component.html',
  styleUrls: ['./district-summary.component.css']
})
export class DistrictSummaryComponent implements AfterViewInit {

  httpClient: HttpClient;
  baseUrl: string;

  DistrictInterface: any;

  public district: District;
  public fundtotal: number;

  public ownerSummary: OwnerSummary[] = new Array();        //Holds Owner Summary collection used by table
  public ownerSummaryNewArrivals_Week: OwnerSummary[] = new Array();
  public ownerSummaryNewArrivals_Month: OwnerSummary[] = new Array();

  public adShow: boolean = false;
  public showArrivalMonth: boolean = true;
  public CONSTRUCT = "CONSTRUCT";
  public PRODUCE = "PRODUCE";

  dataSourceOwnerSummary = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(NoteModalComponent, { static: true }) childNote: NoteModalComponent;

  @ViewChild("graphConstruct", { static: true }) childGraphConstruct: TaxGraphComponent;
  @ViewChild("graphProduce", { static: true }) childGraphProduce: TaxGraphComponent;
  @ViewChild("graphFund", { static: true }) childGraphFund: TaxGraphComponent;
  @ViewChild("graphDistribute", { static: true }) childGraphDistribute: TaxGraphComponent;
  @ViewChild("arrivalsWeek", { static: true } as any) arrivalsWeek: MatCheckbox;
  @ViewChild("arrivalsMonth", { static: true } as any) arrivalsMonth: MatCheckbox;


  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumnsOwners: string[] = ['owner_nickname', 'owned_plots', 'energy_count', 'industry_count', 'production_count', 'residential_count', 'office_count', 'poi_count', 'commercial_count', 'municipal_count'];

  constructor(private route: ActivatedRoute, http: HttpClient, @Inject('BASE_URL') baseUrl: string, private elem: ElementRef) {
    this.httpClient = http;
    this.baseUrl = baseUrl;
    this.district = {
      update_instance: 0,
      last_updateFormated: "",
      district_name: "",
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
      energy_tax: 0,
      production_tax: 0,
      commercial_tax: 0,
      citizens_tax: 0,
      building_count: 0,
      plots_claimed: 0,
      promotion: "",
      promotion_start: "",
      promotion_end: "",
      distribution_period: 0,
      produceTax: null,
      constructTax: null,
      fundHistory: null,
      distributeHistory: null,
      perkSchema: null,
      districtPerk: null
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
  searchDistrict(district_id: number) {

    let params = new HttpParams();
    params = params.append('district_id', district_id.toString());
    this.adShow = false;
    
    this.httpClient.get<District>(this.baseUrl + 'api/district', { params: params })
      .subscribe((result: District) => {        

        this.district = result;
        this.searchOwnerSummaryDistrict(district_id, this.district.update_instance);

        this.arrivalsWeek.checked = false;
        this.arrivalsMonth.checked = false;
        this.removeLinkHighlight();

        if (this.district.district_id != 0) {
          this.childGraphConstruct.loadGraph(this.district.constructTax);
          this.childGraphProduce.loadGraph(this.district.produceTax);
          this.childGraphFund.loadGraph(this.district.fundHistory);
          this.childGraphDistribute.loadGraph(this.district.distributeHistory);
        }
        //plotPos.rotateEle.classList.remove("rotate");

        // Extract last fund total amount and display
        if (this.district.fundHistory) {
          let fund = this.district.fundHistory.graphColumns[0].series;
          this.fundtotal = fund[fund.length - 1].value;
        }

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
    this.ownerSummary = new Array();
    this.ownerSummaryNewArrivals_Week = new Array();
    this.ownerSummaryNewArrivals_Month = new Array();

    this.httpClient.get<OwnerSummary[]>(this.baseUrl + 'api/ownersummary', { params: params })
      .subscribe((result: OwnerSummary[]) => {

        this.ownerSummary = result;

        if (this.ownerSummary) {
          this.dataSourceOwnerSummary = new MatTableDataSource<OwnerSummary>(this.ownerSummary);

          this.dataSourceOwnerSummary.sort = this.sort;
        }

        this.removeLinkHighlight();
        //plotPos.rotateEle.classList.remove("rotate");

        // Store new arrivals for use on filter
        this.ownerSummary.forEach(summary => {
          if (summary.new_owner == true) {
            this.ownerSummaryNewArrivals_Week.push(summary);
          }
          if (summary.new_owner_month == true) {
            this.ownerSummaryNewArrivals_Month.push(summary);
          }          
        });

      }, error => console.error(error));

    return;
  }

  filterArrivalsWeek(eventCheckbox: MatCheckboxChange) {

    this.arrivalsMonth.checked = false;

    if (eventCheckbox.checked) {
      this.dataSourceOwnerSummary = new MatTableDataSource<OwnerSummary>(this.ownerSummaryNewArrivals_Week);
    }
    else {
      this.dataSourceOwnerSummary = new MatTableDataSource<OwnerSummary>(this.ownerSummary);
    }
    this.dataSourceOwnerSummary.sort = this.sort;
  }

  filterArrivalsMonth(eventCheckbox: MatCheckboxChange) {

    this.arrivalsWeek.checked = false;

    if (eventCheckbox.checked) {
      this.dataSourceOwnerSummary = new MatTableDataSource<OwnerSummary>(this.ownerSummaryNewArrivals_Month);
      //this.showArrivalMonth = true;
    }
    else {
      this.dataSourceOwnerSummary = new MatTableDataSource<OwnerSummary>(this.ownerSummary);
      //this.showArrivalMonth = false;
    }
    this.dataSourceOwnerSummary.sort = this.sort;
  }

  // District ad modal popup shown - user can close or move it
  showAd(promotionText: string, promotionStart: string, promotionEnd: string) {
    this.childNote.adShow(promotionText, promotionStart, promotionEnd);
    this.adShow = true;

    return;
  }

  hideAd(componentVisible: boolean) {
    this.adShow = !componentVisible;
  }

  public applyFilter = (value: string) => {
    this.dataSourceOwnerSummary.filter = value.trim().toLocaleLowerCase();
  }
}
