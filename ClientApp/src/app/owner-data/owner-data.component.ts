/* eslint-disable @typescript-eslint/no-inferrable-types */
import { Component, Inject, ViewChild, EventEmitter, ElementRef, ChangeDetectorRef } from '@angular/core';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { Location } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { NavigationEnd, NavigationStart, RouterEvent, Router, ActivatedRoute, Params } from '@angular/router';
import { interval, Observable, Subscription } from 'rxjs';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, MatSortable, Sort } from '@angular/material/sort';
import { AfterViewInit } from '@angular/core';
import { ProdHistoryComponent } from '../production-history/prod-history.component';
import { OfferModalComponent } from '../offer-modal/offer-modal.component';
import { PetModalComponent } from '../pet-modal/pet-modal.component';
import { PackModalComponent } from '../pack-modal/pack-modal.component';
import { CitizenModalComponent } from '../citizen-modal/citizen-modal.component';
import { MatLegacyButton as MatButton } from '@angular/material/legacy-button';
import { OwnerLandData, OwnerData, PlotPosition, BUILDING, FilterCount } from './owner-interface';
import { Globals, WORLD } from '../common/global-var';
import { SearchPlotComponent } from '../search-plot/search-plot.component';


@Component({
  selector: 'app-owner-data',
  templateUrl: './owner-data.component.html',
  styleUrls: ['./owner-data.component.css']
})
export class OwnerDataComponent implements AfterViewInit {

  httpClient: HttpClient;
  baseUrl: string;

  readonly BUILDING = BUILDING;
  public owner: OwnerData;
  public filterCount: FilterCount;
  public filterLandByDistrict: OwnerLandData[] = [];
  public hideEmptyFilter: boolean = true; hideIndFilter: boolean = true; hideProdFilter: boolean = true; hideEngFilter: boolean = true; hideOffFilter: boolean = true; hideResFilter: boolean = true; hideComFilter: boolean = true; hideMuniFilter: boolean = true; hideAOIFilter: boolean = true;
  private currentDistrictFilter: number = 0;
  public buttonShowAll: boolean = false;
  public historyShow: boolean = false;
  public offerShow: boolean = false;
  public petShow: boolean = false;
  public packShow: boolean = false;
  public citizenShow: boolean = false;
  private subscriptionRouterEvent: Subscription;
  public notifySubscription: Subscription = null;
  private myPortfolioRequest: boolean = false;
  private checkInstance: number = 0;
  public mobileView: boolean = false;

  // UI class flags
  public searchBlinkOnce: boolean = false;

  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(ProdHistoryComponent, { static: true }) prodHistory: ProdHistoryComponent;
  @ViewChild(OfferModalComponent, { static: true }) offerModal: OfferModalComponent;
  @ViewChild(PetModalComponent, { static: true }) petModal: PetModalComponent;
  @ViewChild(PackModalComponent, { static: true }) packModal: PackModalComponent;
  @ViewChild(CitizenModalComponent, { static: true }) citizenModal: CitizenModalComponent;
  @ViewChild(SearchPlotComponent, { static: false }) searchPlotComponent: SearchPlotComponent;

  // ViewChild used for these elements to provide for rapid element attribute changes without need for scanning DOM and readability.
  @ViewChild('emptyPlotFilter', { static: false}) emptyPlotFilter: ElementRef;
  @ViewChild('industrialFilter',{ static: false }) industrialFilter: ElementRef;
  @ViewChild('municipalFilter',{ static: false }) municipalFilter: ElementRef;
  @ViewChild('productionFilter',{ static: false }) productionFilter: ElementRef;
  @ViewChild('energyFilter',{ static: false }) energyFilter: ElementRef;
  @ViewChild('officeFilter',{ static: false }) officeFilter: ElementRef;
  @ViewChild('residentialFilter', { static: false }) residentialFilter: ElementRef;
  @ViewChild('commercialFilter', { static: false }) commercialFilter: ElementRef;
  @ViewChild('aoiFilter', { static: false }) aoiFilter: ElementRef;

  @ViewChild('lowStaminaBtn', { static: false }) lowStaminaBtn: MatButton;
  @ViewChild('offerDetailsBtn', { static: false }) offerDetailsBtn: MatButton;
  


  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['district_id', 'pos_x', 'pos_y', 'building_type', 'building_level', 'last_action', 'current_influence_rank', 'condition', 'plot_ip', 'citizen_count',/* 'token_id', */'citizen_stamina_alert'];
  displayedColumnsMobile: string[] = ['district_id', 'pos_x', 'building_type', 'building_level', 'last_action', 'current_influence_rank', 'condition', 'plot_ip', 'citizen_count',/* 'token_id', */'citizen_stamina_alert'];
 
  constructor(private cdf: ChangeDetectorRef, public globals: Globals, private location: Location, public router: Router, private route: ActivatedRoute, http: HttpClient, @Inject('BASE_URL') baseUrl: string, private elem: ElementRef)
  {
    this.httpClient = http;    
    this.baseUrl = baseUrl + "api/" + globals.worldCode;
    this.setInitVar();
    this.initFilterCount();    

    globals.ownerCDF = cdf;
    globals.ownerComponent = this;    

  }

  public get width() {
    return window.innerWidth;
  }

  // Need the Plot Search component loaded to change its flags
  ngAfterViewInit() {

    // Check on URL change due to movement between features
    this.subscriptionRouterEvent = this.router.events.subscribe((event: RouterEvent) => {
      //console.log('current route: ', router.url.toString());

      if (event instanceof NavigationEnd) {

        // CASE reset the search to empty when moving from My Portfolio to Owner Report
        if (this.router.url.indexOf("/owner-data?") > -1) {
          this.triggerSearchByMatic();
        }

      }
    });

    // CASE reset the search to empty when moving from My Portfolio to Owner Report
    if (this.router.url.indexOf("/owner-data?") > -1) {
      this.triggerSearchByMatic();
    }

  }

  ngOnDestroy() {
    //Prevent multi subscriptions relating to router change events
    if (this.subscriptionRouterEvent) {
      this.subscriptionRouterEvent.unsubscribe();
    }
    if (this.notifySubscription) {
      this.notifySubscription.unsubscribe();
    }
  }

  // Trigger used on page load, or on URL change - moving between My portfolio and Owner Report features
  triggerSearchByMatic(forceCDFrefresh: boolean = false) {
    
    this.offerShow = false;
    let resetFields: boolean = false;

    let requestOwnerMatic = this.route.snapshot.queryParams["matic"];
    const plotX = this.route.snapshot.queryParams["plotx"];
    const plotY = this.route.snapshot.queryParams["ploty"];
   
    if (requestOwnerMatic) {

      if (requestOwnerMatic.toLowerCase() == "myportfolio") {

        this.myPortfolioRequest = true;     // Control URL auto reformating 

        // Check if Account is checked, identified this wallet as a valid owner account,  max loop 5 instances
        if (this.globals.ownerAccount.checked == false && this.checkInstance < 4) {

          //Wait until account is checked before loading.
          if (this.notifySubscription == null) {

            this.notifySubscription = interval(1000).subscribe(x => {
              this.triggerSearchByMatic(forceCDFrefresh);
            });
          }

          this.checkInstance++;
          return;
        }

        if (this.notifySubscription) {
          this.notifySubscription.unsubscribe();
          this.notifySubscription = null;
        }
        this.checkInstance = 0;

        requestOwnerMatic = this.globals.ownerAccount.matic_key;
      }

      // Check if owner already loaded then dont reload, can occur due to initial URL change on First Search which triggers the subscriptionRouterEvent
      if (requestOwnerMatic == "Not Found") {
        resetFields = true;
      }
      else if (requestOwnerMatic != this.owner.owner_matic_key) {
        this.searchOwnerbyMatic(requestOwnerMatic, forceCDFrefresh);
      }

    }
    else if (plotX && plotY)
    {
      this.searchPlotComponent.rotateActive = true;
      this.searchPlot({ plotX: plotX, plotY: plotY }, true);
    }
    else {
      resetFields = true;
    }


    if (resetFields) {

      // CASE reset the search to empty when moving from MyPortfolio to [default] Owner Report
      this.setInitVar();
      this.dataSource = new MatTableDataSource(null);
      this.filterLandByDistrict = [];
      this.currentDistrictFilter = 0;
      this.buttonShowAll = false;
      this.hideBuildingFilter(this.owner.owner_land);
      this.prodHistory.setHide();

      // Corner Case: when owner component load tiggered by Wallet change - a cdf force is required to render page
      if (forceCDFrefresh) {
        this.cdf.detectChanges();
      }
    }
  }

  setInitVar() {

    this.owner = {
      owner_name: "",
      owner_url: this.globals.worldURLPath +"citizen/" + this.globals.firstCitizen,
      owner_matic_key: "",
      last_action: null,
      registered_date: "",
      last_visit: "",
      plot_count: -1,
      developed_plots: 0,
      plots_for_sale: 0,
      stamina_alert_count: 0,
      offer_count: 0,
      offer_sold_count: 0,
      offer_last_updated: "",
      owner_offer: null,
      owner_offer_sold: null,
      pet_count: 0,
      citizen_count: 0,      
      district_plots: null,
      owner_land: null,
      search_info: null,
      search_token: 0,
      pack_count: 0,
      pack: null
    };
  }

  initFilterCount() {
    this.filterCount = {
      industry: 0,
      office: 0,
      residential: 0,
      energy: 0,
      municipal: 0,
      production: 0,
      empty: 0,
      commercial: 0,
      poi: 0
    };
  }

  searchOwnerbyMatic(ownerMatic: string, forceCDFrefresh: boolean = false) {

    let params = new HttpParams();
    params = params.append('owner_matic_key', ownerMatic);

    const rotateEle = document.getElementById("searchIcon");
    rotateEle.classList.add("rotate");
    
    this.httpClient.get<OwnerData>(this.baseUrl + '/ownerdata/getusingmatic', { params: params })
      .subscribe({
        next: (result) => {

          this.loadClientData(result);

          if (forceCDFrefresh) {
            this.cdf.detectChanges();
          }
        },
        error: (error) => { console.error(error) }
      });

    return;
  }

  // Single parameter struct containing 2 members, pushed by component search-plot
  searchPlot(plotPos: PlotPosition, loadBuildingHistory:boolean = false) {

    let params = new HttpParams();
    params = params.append('plotX', plotPos.plotX);
    params = params.append('plotY', plotPos.plotY);

    // Check if no X Y , then skip and blink instructions.
    if (plotPos.plotX == '' || plotPos.plotY == '') {

      this.searchBlinkOnce = true;
      this.searchPlotComponent.rotateActive = false;

      return;
    }

    //this.httpClient.get<OwnerData>(this.baseUrl + 'ownerdata/Get?plotX=' + encodeURIComponent(plotPos.plotX) + '&plotY=' + encodeURIComponent(plotPos.plotY))
    this.httpClient.get<OwnerData>(this.baseUrl + '/OwnerData', { params: params })
      .subscribe({
        next: (result) => {

          this.loadClientData(result);

          // Reset the URL to reflect no account matic found
          if (this.owner.owner_matic_key === "") {
            this.router.navigate([]);
          }

          // Auto load building History if search on specific building by URL parameters
          if (loadBuildingHistory && result && result.owner_land) {

            const x: number = Number(plotPos.plotX);
            const y: number = Number(plotPos.plotY);
            let assetId: number = 0;
            let buildingType: number = 0;

            for (let index = 0; index < this.owner.owner_land.length; index++) {
              
              if (this.owner.owner_land[index].token_id == this.owner.search_token) {
                assetId = this.owner.owner_land[index].token_id;
                buildingType = this.owner.owner_land[index].building_type;
                break;
              }
            }

            if (buildingType == BUILDING.ENERGY || buildingType == BUILDING.INDUSTRIAL || buildingType == BUILDING.PRODUCTION || buildingType == BUILDING.OFFICE) {
              this.showHistory(assetId, x, y, buildingType, 0);
            }
          }

          this.searchPlotComponent.rotateActive = false;

        },
        error: (error) => { console.error(error) }
      });

    return;
  }

  loadClientData(clientData: OwnerData) {

    const rotateEle = document.getElementById("searchIcon");
    this.hideAll();

    this.owner = clientData;
    this.dataSource = null;

    this.hideBuildingFilter(this.owner.owner_land);

    // Mobile View - remove secondary columns
    if (this.width < 768) {
      this.displayedColumns = this.displayedColumnsMobile;
      this.mobileView = true;
    }

    if (this.owner.owner_land) {

      this.dataSource = new MatTableDataSource<OwnerLandData>(this.owner.owner_land);

      this.dataSource.sort = this.sort;

      // Add custom date column sort
      this.applySortDataAsccessor(this.dataSource);
      
      this.sort.sort(({ id: 'last_action', start: 'desc' }) as MatSortable);        // Default sort order on date

      // Reset the URL to reflect current account matic
      if (this.myPortfolioRequest == false) {
        this.router.navigate([], { queryParams: { matic: this.owner.owner_matic_key }, });
      }

    }

    if (this.owner.stamina_alert_count == 0) {
      this.lowStaminaBtn._elementRef.nativeElement.classList.remove("btnAlert");
    }
    else {
      this.lowStaminaBtn._elementRef.nativeElement.classList.add("btnAlert");
    }

    if (this.owner.offer_count == 0) {
      this.offerDetailsBtn._elementRef.nativeElement.classList.remove("btnAlert");
    }
    else {
      this.offerDetailsBtn._elementRef.nativeElement.classList.add("btnAlert");
    }

    this.buttonShowAll = false;
    this.removeLinkHighlight(".districtEleActive, .activeFilter");

    rotateEle.classList.remove("rotate");

    return;
  }

  applySortDataAsccessor(targetDataSource: any) {
    // Add custom date column sort
    targetDataSource.sortingDataAccessor = (item: OwnerLandData, property) => {
      switch (property) {
        case 'last_action': return item.last_action == "Empty Plot" ? new Date(0) : new Date(item.last_action);
        case 'building_type': return item.building_type * 10 + item.resource;
        case 'current_influence_rank': return item.building_type == 0 && this.sort.direction == "asc" ? 1000 : item.current_influence_rank;   // Only sort plots with buildings
        case 'condition': return item.building_type == 0 && this.sort.direction == "asc" ? 1000 : item.condition;   // Only sort plots with buildings
        default: return item[property];
      }
    };
  }
  sortData(sort: Sort) {
    //const data = this.owner;    
  }

  sortTableStamina() {

    this.sort.sort({ id: 'citizen_stamina_alert', start: 'desc', disableClear: true });

    return;
  }

  sortTableForSale() {
    this.sort.sort({ id: 'forsale', start: 'desc', disableClear: true });
  }
  
  // Filter By District, and By Building Type [Storing pior District filter and using if found]
  filterTable(event, filterValue: number, buildingType: number) {

    let filterbyMulti: OwnerLandData[] = [];

    // Remove ALL Highlights - prior filters when selecting new district
    if (buildingType === BUILDING.NO_FILTER) {
      this.removeLinkHighlight(".districtEleActive, .activeFilter");
    }


    // CHECK If filter is BuildingType and already active, then this click is to disable it
    if (buildingType > BUILDING.NO_FILTER && event.srcElement.closest("div").classList.contains("activeFilter")) {
      
      filterbyMulti = this.currentDistrictFilter == 0 ? this.owner.owner_land : this.filterLandByDistrict;
      event.srcElement.closest("div").classList.remove("activeFilter");
      //event.srcElement.classList.remove("activeFilter");

    }
    // Filter by District
    else if (filterValue > 0 && this.currentDistrictFilter != filterValue) {
      
      this.filterLandByDistrict.length = 0;     // Dont create new array, as reference to may be lost
      this.currentDistrictFilter = filterValue;

      this.owner.owner_land.forEach(land => {
        if (land.district_id == filterValue) {
          this.filterLandByDistrict.push(land);
        }
      });

      this.buttonShowAll = true;
      event.currentTarget.classList.add("districtEleActive");
      filterbyMulti = this.filterLandByDistrict;    

      this.hideBuildingFilter(filterbyMulti);
    }
    // FILTER by Building Type
    else if (buildingType != BUILDING.NO_FILTER) {

      // Remove Any Prior Building Highlight - but maintain district highlight if active
      this.removeLinkHighlight(".activeFilter");

      // Filter by Building Type      
      let filterbyType : OwnerLandData[] = null;
      filterbyType = this.filterLandByDistrict.length == 0 ? this.owner.owner_land : this.filterLandByDistrict;

      filterbyType.forEach(land => {
        if (land.building_type == buildingType) {
          filterbyMulti.push(land);
        }
      });

      this.buttonShowAll = true;
      event.srcElement.parentElement.parentElement.parentElement.classList.add("activeFilter");
    }
    // Reset All Filter - Select All
    else {
      
      this.filterLandByDistrict = [];
      this.currentDistrictFilter = 0;

      filterbyMulti = this.owner.owner_land;

      this.buttonShowAll = false;
      this.hideBuildingFilter(filterbyMulti);
    }
        
    // Assign filtered dataset
    this.dataSource = new MatTableDataSource<OwnerLandData>(filterbyMulti);
    this.dataSource.sort = this.sort;
    this.applySortDataAsccessor(this.dataSource);
    this.sort.sortChange.emit(this.sort);   // Apply current sort order (last used)    

    return;
  }

  removeLinkHighlight(matchElement: string) {

    // Find all element with highlight - may be both district and building
    const districtLinkElements = this.elem.nativeElement.querySelectorAll(matchElement);

    if (districtLinkElements.length) {
      for (let index = 0; index < districtLinkElements.length; index++) {
        let element = districtLinkElements[index];
        element.classList.remove("districtEleActive");
        element.classList.remove("activeFilter");
      }
    }
    return;
  }

  // Called to reset the building filter buttons to match the current search - typically called on a new search instance.
  hideBuildingFilter(ownerLand: OwnerLandData[]) {

    this.initFilterCount();
    
    if (ownerLand !== null && ownerLand.length > 0) {

      // Reset all filter to hide - then enable/show applicable filters based on matching buildings/land
      this.hideEmptyFilter = this.hideIndFilter = this.hideProdFilter = this.hideEngFilter = this.hideOffFilter = this.hideResFilter = this.hideComFilter = this.hideMuniFilter = this.hideAOIFilter = true;      
      //var buildingFilters = this.elem.nativeElement.querySelectorAll(".typeFilter div");
      //if (buildingFilters.length >0) {
      //  for (var index = 0, element; element = buildingFilters[index]; index++) {
      //    element.classList.add("hideFilter");
      //  }
      //}

      for (let index = 0; index < ownerLand.length; index++) {

        let element = ownerLand[index];

        switch (element.building_type) {
          case BUILDING.EMPTYPLOT: {
            this.hideEmptyFilter = false;
            //this.emptyPlotFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.empty++;
            break;
          } 
          case BUILDING.INDUSTRIAL: {
            this.hideIndFilter = false;
            //this.industrialFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.industry++;
            break;
          }
          case BUILDING.MUNICIPAL: {
            this.hideMuniFilter = false;
            //this.municipalFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.municipal++;
            break;
          }
          case BUILDING.PRODUCTION: {
            this.hideProdFilter = false;
            //this.productionFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.production++;
            break;
          }
          case BUILDING.ENERGY: {
            this.hideEngFilter = false;
            //this.energyFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.energy++;
            break;
          }
          case BUILDING.OFFICE: {
            this.hideOffFilter = false;
            //this.officeFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.office++;
            break;
          }
          case BUILDING.RESIDENTIAL: {
            this.hideResFilter = false;
            //this.residentialFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.residential++;
            break;
          }
          case BUILDING.COMMERCIAL: {
            this.hideComFilter = false;
            //this.commercialFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.commercial++;
            break;
          }
          case BUILDING.AOI: {
            this.hideAOIFilter = false;
            //this.aoiFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.poi++;
            break;
          }
          default: {         
            break;
          }
        }
  
      }
    }
    return;
  }

  showHistory(asset_id: number, pos_x: number, pos_y: number, building_type: number, ip_efficiency: number) {
    
    this.prodHistory.searchHistory(asset_id, pos_x, pos_y, building_type, false);
    this.historyShow = true;

    return;
  }

  hideHistory(componentVisible: boolean) {
    this.historyShow = !componentVisible;

    return;
  }

  showPet() {

    this.hideAll();

    if (this.petShow == true) { // || this.owner.offer_count == 0) {
      this.petShow = false;
    }
    else {
      this.petModal.searchPets(this.owner.owner_matic_key);
      this.petShow = true;
    }

    return;
  }

  showCitizen() {
    if (this.citizenShow == true) { // || this.owner.offer_count == 0) {
      this.citizenShow = false;
    }
    else {
      this.hideAll();
      this.citizenModal.search(this.owner.owner_matic_key, false);
      this.citizenShow = true;
    }
  }

  showOffer() {

    if (this.offerShow == true) { // || this.owner.offer_count == 0) {
      this.offerShow = false;
    }
    else {
      this.hideAll();
      this.offerModal.loadTable(this.owner.owner_offer, this.owner.owner_offer_sold, this.owner.offer_last_updated);
      this.offerShow = true;
    }

  }

  showPack() {
    this.hideAll();

    if (this.packShow == true) { // || this.owner.offer_count == 0) {
      this.packShow = false;
    }
    else {
      this.packModal.loadPackList(this.owner.pack);
      this.packShow = true;
    }

    return;
  }

  hideOffer(componentVisible: boolean) {
    this.offerShow = !componentVisible;
  }

  hidePet(componentVisible: boolean) {
    this.petShow = !componentVisible;
  }

  hidePack(componentVisible: boolean) {
    this.packShow = !componentVisible;
  }

  hideCitizen(componentVisible: boolean) {
    this.citizenShow = !componentVisible;

    if (this.citizenModal.portfolioCitizen != null && this.citizenModal.portfolioCitizen.citizen != null) {
      this.owner.citizen_count = this.citizenModal.portfolioCitizen.citizen.length;
    }
  }

  hideAll() {
    this.offerShow = false;
    this.petShow = false;
    this.citizenShow = false;
    this.historyShow = false;
  }

}
