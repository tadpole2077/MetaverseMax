/* eslint-disable @typescript-eslint/no-inferrable-types */
import { Component, Inject, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { Location } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { NavigationEnd, RouterEvent, Router, ActivatedRoute } from '@angular/router';
import { interval, Subscription } from 'rxjs';
import { MatTableDataSource, MatTableDataSourcePaginator } from '@angular/material/table';
import { MatSort, MatSortable, Sort, SortDirection } from '@angular/material/sort';
import { AfterViewInit } from '@angular/core';
import { ProdHistoryComponent } from '../production-history/prod-history.component';
import { OfferModalComponent } from '../offer-modal/offer-modal.component';
import { PetModalComponent } from '../pet-modal/pet-modal.component';
import { PackModalComponent } from '../pack-modal/pack-modal.component';
import { CitizenModalComponent } from '../citizen-modal/citizen-modal.component';
import { MatButton } from '@angular/material/button';
import { IOwnerLandData, IOwnerData, IPlotPosition, IFilterCount } from './owner-interface';
import { Globals, WORLD } from '../common/global-var';
import { MapData } from '../common/map-data';
import { CUSTOM_BUILDING_CATEGORY, BUILDING, MIN_STAMINA } from '../common/enum';
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
  public owner: IOwnerData;
  public filterCount: IFilterCount;
  public filterLandByDistrict: IOwnerLandData[] = [];
  public hideEmptyFilter: boolean = true; hideIndFilter: boolean = true; hideProdFilter: boolean = true; hideEngFilter: boolean = true; hideOffFilter: boolean = true; hideResFilter: boolean = true; hideComFilter: boolean = true; hideMuniFilter: boolean = true; hideAOIFilter: boolean = true;  hideParcelFilter: boolean = true;
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
  public staminaView: boolean = false;

  // UI class flags
  public searchBlinkOnce: boolean = false;

  dataSource = new MatTableDataSource(null);    //MatTableDataSource<unknown, MatDatatableSourcePaginator>
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(ProdHistoryComponent, { static: true }) prodHistory: ProdHistoryComponent;
  @ViewChild(OfferModalComponent, { static: true }) offerModal: OfferModalComponent;
  @ViewChild(PetModalComponent, { static: true }) petModal: PetModalComponent;
  @ViewChild(PackModalComponent, { static: true }) packModal: PackModalComponent;
  @ViewChild(CitizenModalComponent, { static: true }) citizenModal: CitizenModalComponent;
  @ViewChild(SearchPlotComponent, { static: false }) searchPlotComponent: SearchPlotComponent;

  // ViewChild used for these elements to provide for rapid element attribute changes without need for scanning DOM and readability.
  @ViewChild('emptyPlotFilter', { static: false}) emptyPlotFilter: ElementRef;
  @ViewChild('lowStaminaBtn', { static: false }) lowStaminaBtn: MatButton;
  @ViewChild('offerDetailsBtn', { static: false }) offerDetailsBtn: MatButton;

  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['district_id', 'pos_x', 'pos_y', 'building_type', 'building_level', 'last_action', 'current_influence_rank', 'condition', 'plot_ip', 'citizen_count',/* 'token_id', */'alert'];
  displayedColumnsMobile: string[] = ['district_id', 'pos_x', 'building_type', 'building_level', 'last_action', 'current_influence_rank', 'condition', 'plot_ip', 'citizen_count',/* 'token_id', */'alert'];
 
  constructor(private cdf: ChangeDetectorRef, public globals: Globals, public mapdata: MapData, private location: Location, public router: Router, private route: ActivatedRoute, http: HttpClient, @Inject('BASE_URL') baseUrl: string, private elem: ElementRef)
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
        else {
          this.reset(true);
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
      this.reset(forceCDFrefresh);
    }

  }

  reset(forceCDFrefresh: boolean = false) {

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
      poi: 0,
      parcel: 0
    };
  }

  searchOwnerbyMatic(ownerMatic: string, forceCDFrefresh: boolean = false) {

    let params = new HttpParams();
    params = params.append('owner_matic_key', ownerMatic);

    const rotateEle = document.getElementById("searchIcon");
    rotateEle.classList.add("rotate");
    
    this.httpClient.get<IOwnerData>(this.baseUrl + '/ownerdata/getusingmatic', { params: params })
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

  // Search by Plot X and Y
  // Single parameter struct containing 2 members, pushed by component search-plot
  searchPlot(plotPos: IPlotPosition, loadBuildingHistory:boolean = false) {

    let params = new HttpParams();
    params = params.append('plotX', plotPos.plotX);
    params = params.append('plotY', plotPos.plotY);

    this.myPortfolioRequest = false;

    // Check if no X Y , then skip and blink instructions.
    if (plotPos.plotX == '' || plotPos.plotY == '') {

      this.searchBlinkOnce = true;
      this.searchPlotComponent.rotateActive = false;

      return;
    }

    //this.httpClient.get<OwnerData>(this.baseUrl + 'ownerdata/Get?plotX=' + encodeURIComponent(plotPos.plotX) + '&plotY=' + encodeURIComponent(plotPos.plotY))
    this.httpClient.get<IOwnerData>(this.baseUrl + '/OwnerData', { params: params })
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

  loadClientData(clientData: IOwnerData) {

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

      this.dataSource = new MatTableDataSource<IOwnerLandData>(this.owner.owner_land);

      this.sort.active = "alert";
      this.sort.direction = "desc";
      this.dataSource.sort = this.sort;
      // Add custom sort callback
      this.applySortDataAsccessor(this.dataSource);
      this.sort.sortChange.emit(this.sort);   // trigger default sort

      //this.sort.sort(({ id: 'last_action', start: 'desc' }) as MatSortable);        // Default sort order on date

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

  applySortDataAsccessor(targetDataSource: MatTableDataSource<unknown, MatTableDataSourcePaginator>) {

    this.staminaView = false;
    // Add custom date column sort
    targetDataSource.sortingDataAccessor = (item: IOwnerLandData, property) => {

      this.staminaView = false;

      switch (property) {
        case 'last_action': return item.last_action == "Empty Plot" ? new Date(0) : new Date(item.last_action);
        case 'building_type': return item.building_type * 10 + item.resource;
        case 'current_influence_rank': return item.building_type == 0 && this.sort.direction == "asc" ? 1000 : item.current_influence_rank;   // Only sort plots with buildings
        case 'condition': return item.building_type == 0 && this.sort.direction == "asc" ? 1000 : item.condition;   // Only sort plots with buildings
        case 'alert': return item.c_r == true ||  item.c_d >0 || item.c_h >0 || item.c_m >0  ? 1 - (item.c_d/7)  - (item.c_h/24/7) - (item.c_m/3600) : item.citizen_stamina_alert == true ? .001 : 0;
        default: return item[property];
      }
    };
  }

  sortData(sort: Sort) {
    //const data = this.owner;    
  }

  sortTableAlertShowingStaminaFirst() {

    // Only assign new sorted land array if alerts found.
    if (this.owner.stamina_alert_count > 0) {      

      // Remove any current filters
      this.filterTable(null, 0, BUILDING.NO_FILTER);

      // Generate a table list with all stamina alert buildings shown first.
      const sortbyAlert: IOwnerLandData[] = [];
     
      this.owner.owner_land.forEach(land => {
        if (land.citizen_stamina_alert == true) {
          sortbyAlert.push(land);          
        }
      });

      this.owner.owner_land.forEach(land => {
        if (land.citizen_stamina_alert == false) {
          sortbyAlert.push(land);
        }
      });
  
      this.dataSource = new MatTableDataSource<IOwnerLandData>(sortbyAlert);
      this.sort.active = "";
      this.dataSource.sort = this.sort;
      this.applySortDataAsccessor(this.dataSource);

      this.staminaView = true;

    }
  }

  sortTableStaminaOld() {

    this.sort.direction = 'desc';
    this.sort.active = 'alert';
    this.sort.sortChange.emit(this.sort);
    //this.sort.sort({ id: 'alert', start: 'desc', disableClear: true });

    return;
  }

  sortTableForSale() {
    this.sort.sort({ id: 'forsale', start: 'desc', disableClear: true });
  }
  
  // Filter By District, and By Building Type [Storing pior District filter and using if found]
  filterTable(event, filterValue: number, buildingType: number) {

    let filterbyMulti: IOwnerLandData[] = [];

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
      let filterbyType : IOwnerLandData[] = null;
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
    this.dataSource = new MatTableDataSource<IOwnerLandData>(filterbyMulti);
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
        const element = districtLinkElements[index];
        element.classList.remove("districtEleActive");
        element.classList.remove("activeFilter");
      }
    }
    return;
  }

  // Called to reset the building filter buttons to match the current search - typically called on a new search instance.
  hideBuildingFilter(ownerLand: IOwnerLandData[]) {

    this.initFilterCount();
    
    if (ownerLand !== null && ownerLand.length > 0) {

      // Reset all filter to hide - then enable/show applicable filters based on matching buildings/land
      this.hideEmptyFilter = this.hideIndFilter = this.hideProdFilter = this.hideEngFilter = this.hideOffFilter = this.hideResFilter = this.hideComFilter = this.hideMuniFilter = this.hideAOIFilter = this.hideParcelFilter = true;      
      //var buildingFilters = this.elem.nativeElement.querySelectorAll(".typeFilter div");
      //if (buildingFilters.length >0) {
      //  for (var index = 0, element; element = buildingFilters[index]; index++) {
      //    element.classList.add("hideFilter");
      //  }
      //}

      for (let index = 0; index < ownerLand.length; index++) {

        const element = ownerLand[index];

        switch (element.building_type) {
          case BUILDING.EMPTYPLOT: {
            this.hideEmptyFilter = false;
            //this.emptyPlotFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.empty++;
            break;
          } 
          case BUILDING.INDUSTRIAL: {
            this.hideIndFilter = false;
            this.filterCount.industry++;
            break;
          }
          case BUILDING.MUNICIPAL: {
            this.hideMuniFilter = false;
            this.filterCount.municipal++;
            break;
          }
          case BUILDING.PRODUCTION: {
            this.hideProdFilter = false;
            this.filterCount.production++;
            break;
          }
          case BUILDING.ENERGY: {
            this.hideEngFilter = false;
            this.filterCount.energy++;
            break;
          }
          case BUILDING.OFFICE: {
            this.hideOffFilter = false;
            this.filterCount.office++;
            break;
          }
          case BUILDING.RESIDENTIAL: {
            this.hideResFilter = false;
            this.filterCount.residential++;
            break;
          }
          case BUILDING.COMMERCIAL: {
            this.hideComFilter = false;
            this.filterCount.commercial++;
            break;
          }
          case BUILDING.AOI: {
            this.hideAOIFilter = false;
            this.filterCount.poi++;
            break;
          }
          case BUILDING.PARCEL: {
            this.hideParcelFilter = false;            
            this.filterCount.parcel++;
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
  getCustomCategoryName(categoryId: number) {
    return CUSTOM_BUILDING_CATEGORY[categoryId];
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

  getStaminaImg(citizenStamina: number, buildingType: number) {
    let staminaImg:string = "./assets/OutOfStamina.png";

    if (this.stoppedWork(citizenStamina, buildingType) ) {
      staminaImg = "./assets/stopped_work.png";
    }

    return staminaImg;
  }

  // Idenify if building contains ANY citizens with min stamina
  // TO_DO: potential some citizens can have min stamina and building can continue to work, only if min amount of citizens is also impacted.
  stoppedWork(citizenStamina: number, buildingType: number) {

    let stoppedWork = false;

    if ((citizenStamina < MIN_STAMINA.ENERGY && buildingType == BUILDING.ENERGY) ||
      (citizenStamina < MIN_STAMINA.INDUSTRIAL && buildingType == BUILDING.INDUSTRIAL) ||
      (citizenStamina < MIN_STAMINA.COMMERCIAL && buildingType == BUILDING.COMMERCIAL) ||
      (citizenStamina < MIN_STAMINA.MUNICIPAL && buildingType == BUILDING.MUNICIPAL) ||
      (citizenStamina < MIN_STAMINA.PRODUCTION && buildingType == BUILDING.PRODUCTION) ||
      (citizenStamina < MIN_STAMINA.RESIDENTIAL && buildingType == BUILDING.RESIDENTIAL) ||
      (citizenStamina < MIN_STAMINA.OFFICE && buildingType == BUILDING.OFFICE))
    {
      stoppedWork = true;
    }

    return stoppedWork;
  }
}
