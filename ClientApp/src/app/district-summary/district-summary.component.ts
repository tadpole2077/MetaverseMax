import { Component, Inject, ViewChild, EventEmitter, ElementRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Router, ActivatedRoute, Params } from '@angular/router';
import { MatLegacyTableDataSource as MatTableDataSource } from '@angular/material/legacy-table';
import { MatSort } from '@angular/material/sort';
/*import { MatInputModule, MatExpansionModule, MatIconModule, MatCheckboxModule, MatCheckboxChange, MatCheckbox } from '@angular/material';*/
import { MatLegacyCheckbox as MatCheckbox, MatLegacyCheckboxChange as MatCheckboxChange } from '@angular/material/legacy-checkbox';
import { AfterViewInit } from '@angular/core';
import { NoteModalComponent } from '../note-modal/note-modal.component';
import { AlertMenuComponent } from '../alert-menu/alert-menu.component';
import { GraphTaxComponent } from '../graph-tax/graph-tax.component';
import { GraphFundComponent } from '../graph-fund/graph-fund.component';
import { TaxChangeComponent } from '../tax-change/tax-change.component';
import { OwnerSummary, District } from './data-district-interface';
import { MatExpansionPanel } from '@angular/material/expansion';
import { Globals, WORLD } from '../common/global-var';


@Component({
  selector: 'district-summary-data',
  templateUrl: './district-summary.component.html',
  styleUrls: ['./district-summary.component.css']
})
export class DistrictSummaryComponent implements AfterViewInit {

  httpClient: HttpClient;
  baseUrl: string;
  public worldCode: string;
  districtImgURL: string;
  DistrictInterface: any;

  public isMobileView: boolean = false;
  public requestDistrictId: number;
  public district: District;
  public fundtotal: number;
  public fundDaily: number;

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
  @ViewChild(AlertMenuComponent, { static: true }) alertMenu: AlertMenuComponent;
  @ViewChild("taxChange", { static: true }) taxChange: TaxChangeComponent;
  @ViewChild("taxChangePanel", { static: true }) taxChangePanel: MatExpansionPanel;

  @ViewChild("graphConstruct", { static: true }) childGraphConstruct: GraphTaxComponent;
  @ViewChild("graphProduce", { static: true }) childGraphProduce: GraphTaxComponent;
  @ViewChild("graphFund", { static: true }) childGraphFund: GraphFundComponent;
  @ViewChild("graphDistribute", { static: true }) childGraphDistribute: GraphFundComponent;
  @ViewChild("arrivalsWeek", { static: true } as any) arrivalsWeek: MatCheckbox;
  @ViewChild("arrivalsMonth", { static: true } as any) arrivalsMonth: MatCheckbox;

  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumnsOwners: string[] = ['owner_name', 'owned_plots', 'energy_count', 'industry_count', 'production_count', 'residential_count', 'office_count', 'poi_count', 'commercial_count', 'municipal_count'];

  constructor(public globals: Globals, private activedRoute: ActivatedRoute, private router: Router, http: HttpClient, @Inject('BASE_URL') baseUrl: string, private elem: ElementRef) {

    this.httpClient = http;
    this.worldCode = (globals.selectedWorld == WORLD.TRON ? "trx" : globals.selectedWorld == WORLD.BNB ? "bnb" : "eth")
    this.baseUrl = baseUrl + "api/" + this.worldCode;    

    this.district = {
      update_instance: 0,
      last_updateFormated: "",
      district_name: "",
      district_id: 0,
      owner_name: "Search for an Owner, Enter Plot X and Y position, click Find Owner.",
      owner_avatar_id: 0,
      owner_url: globals.worldURLPath +"citizen/" + globals.firstCitizen,
      owner_matic: "",
      active_from: "",
      land_count: 0,
      initial_land_price: 0,
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
    this.requestDistrictId = this.activedRoute.snapshot.queryParams["district_id"];
    if (this.requestDistrictId) {
      this.searchDistrict(this.requestDistrictId);
    }

    // Mobile View - remove secondary columns
    if (this.width < 768) {
      this.isMobileView = true;
    }

    this.districtImgURL = "https://play.mcp3d.com/assets/images/districts/" + globals.worldCode.toUpperCase() + "/" + this.requestDistrictId + ".png";

    // Monitor using service - when account status changes - active / inactive.
    this.globals.accountActive$.subscribe(active => {

      console.log("account status : " + active);
      if (this.district.district_id != 0) {
        this.alertMenu.getAlert(this.district.district_id);
      }

    })

  }

  ngAfterViewInit() {
    //this.dataSource.sort = this.sort;
  }
  public get width() {
    return window.innerWidth;
  }

  // Single parameter struct containing 1 element, pushed by component search-district
  searchDistrict(districtId: number) {

    let params = new HttpParams();
    params = params.append('district_id', districtId.toString());

    this.adShow = false;
    this.requestDistrictId = districtId;


    this.districtImgURL = "https://play.mcp3d.com/assets/images/districts/" + this.globals.worldCode.toUpperCase() + "/" + this.requestDistrictId + ".png";

    this.httpClient.get<District>(this.baseUrl + '/district', { params: params })
      .subscribe({
        next: (result) => {

          this.district = result;

          // CORNER CASE: Redirect back to list if no district found matching id  (Change World)
          if (this.district.district_id == 0) {
            let navigateTo: string = '/' + this.globals.worldCode + '/district-list';
            this.router.navigate([navigateTo]);
          }
          else {
            this.router.navigate(['/' + this.globals.worldCode + '/district-summary'], { queryParams: { district_id: districtId } });
          }

          this.searchOwnerSummaryDistrict(districtId, this.district.update_instance);

          this.arrivalsWeek.checked = false;
          this.arrivalsMonth.checked = false;

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
            let distribute = this.district.distributeHistory.graphColumns[0].series;
            this.fundtotal = fund[fund.length - 1].value;
            this.fundDaily = distribute[distribute.length - 1].value;
          }

          if (this.globals.ownerAccount.wallet_active_in_world) {
            this.alertMenu.getAlert(districtId);
          }
        },
        error: (error) => { console.error(error) }
      });


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
    this.taxChangePanel.close();
    
    this.httpClient.get<OwnerSummary[]>(this.baseUrl + '/ownersummary', { params: params })
      .subscribe({
        next: (result) => {          

          this.ownerSummary = result;
        
          this.dataSourceOwnerSummary = new MatTableDataSource<OwnerSummary>(this.ownerSummary);
          this.dataSourceOwnerSummary.sort = this.sort;                

          // Store new arrivals for use on filter
          this.ownerSummary.forEach(summary => {
            if (summary.new_owner == true) {
              this.ownerSummaryNewArrivals_Week.push(summary);
            }
            if (summary.new_owner_month == true) {
              this.ownerSummaryNewArrivals_Month.push(summary);
            }
          });

        },
        error: (error) => { console.error(error) }
      });

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

  loadTaxChange() {
    this.taxChange.getTaxChange(this.requestDistrictId);
  }

  wholenumber(fundtotal, distribution_period) {
    return Math.floor(fundtotal / distribution_period);
  }
}
