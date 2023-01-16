import { Component, Inject, ViewChild, EventEmitter, ElementRef } from '@angular/core';
import { Location } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { NavigationEnd, NavigationStart, RouterEvent, Router, ActivatedRoute, Params } from '@angular/router';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, MatSortable, Sort } from '@angular/material/sort';
import { AfterViewInit } from '@angular/core';
import { ProdHistoryComponent } from '../production-history/prod-history.component';
import { OfferModalComponent } from '../offer-modal/offer-modal.component';
import { PetModalComponent } from '../pet-modal/pet-modal.component';
import { CitizenModalComponent } from '../citizen-modal/citizen-modal.component';
import { MatButton } from '@angular/material/button';
import { OwnerLandData, OwnerData, PlotPosition, BUILDING, FilterCount } from './owner-interface';
import { Globals, WORLD } from '../common/global-var';


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
  public filterLandByDistrict: OwnerLandData[] = new Array();
  private currentDistrictFilter: number = 0;
  public buttonShowAll: boolean = false;
  public historyShow: boolean = false;
  public offerShow: boolean = false;
  public petShow: boolean = false;
  public citizenShow: boolean = false;
  private subscriptionRouterEvent: any;

  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(ProdHistoryComponent, { static: true }) prodHistory: ProdHistoryComponent;
  @ViewChild(OfferModalComponent, { static: true }) offerModal: OfferModalComponent;
  @ViewChild(PetModalComponent, { static: true }) petModal: PetModalComponent;
  @ViewChild(CitizenModalComponent, { static: true }) citizenModal: CitizenModalComponent;

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
  @ViewChild('searchComponent', { static: false }) searchComponent: ElementRef;


  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['district_id', 'pos_x', 'pos_y', 'building_type', 'building_level', 'last_action', 'current_influence_rank', 'condition', 'ip_info', 'citizen_count',/* 'token_id', */'citizen_stamina_alert' ];
 
  constructor(public globals: Globals, private location: Location, public router: Router, private route: ActivatedRoute, http: HttpClient, @Inject('BASE_URL') baseUrl: string, private elem: ElementRef)
  {
    this.httpClient = http;    
    this.baseUrl = baseUrl + "api/" + globals.worldCode;
    this.setInitVar();
    this.initFilterCount();

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

  }

  ngAfterViewInit() {

  }

  ngOnDestroy() {
    //Prevent multi subscriptions relating to router change events
    if (this.subscriptionRouterEvent) {
      this.subscriptionRouterEvent.unsubscribe();
    }
  }

  // Trigger used on page load, or on URL change - moving between My portfolio and Owner Report features
  triggerSearchByMatic() {

    this.prodHistory.setHide();
    this.offerShow = false;

    let requestOwnerMatic = this.route.snapshot.queryParams["matic"];
   
    if (requestOwnerMatic) {

      // Check if owner already loaded then dont reload, can occur due to initial URL change on First Search which triggers the subscriptionRouterEvent
      if (requestOwnerMatic != this.owner.owner_matic_key) {
        this.searchOwnerbyMatic(requestOwnerMatic);
      }

    }
    else {
      // CASE reset the search to empty when moving from My Portfolio to Owner Report
      this.setInitVar();
      this.dataSource = new MatTableDataSource(null);
      this.filterLandByDistrict = new Array();
      this.currentDistrictFilter = 0;
      this.buttonShowAll = false;
      this.hideBuildingFilter(this.owner.owner_land);

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
      search_info: null
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

  searchOwnerbyMatic(ownerMatic: string) {

    let params = new HttpParams();
    params = params.append('owner_matic_key', ownerMatic);

    var rotateEle = document.getElementById("searchIcon");
    rotateEle.classList.add("rotate");
    
    this.httpClient.get<OwnerData>(this.baseUrl + '/ownerdata/getusingmatic', { params: params })
      .subscribe((result: OwnerData) => {

        this.loadClientData(result);     

      }, error => console.error(error));

    return;
  }

  // Single parameter struct containing 2 members, pushed by component search-plot
  searchPlot(plotPos: PlotPosition ) {

    let params = new HttpParams();
    params = params.append('plotX', plotPos.plotX);
    params = params.append('plotY', plotPos.plotY);

    // Check if no X Y , then skip and blink instructions.
    if (plotPos.plotX == '' || plotPos.plotY == '') {

      this.searchComponent.nativeElement.classList.remove("blink");
      plotPos.rotateEle.classList.remove("rotate");
      this.searchComponent.nativeElement.classList.add("blink");

      return;
    }

    //this.httpClient.get<OwnerData>(this.baseUrl + 'ownerdata/Get?plotX=' + encodeURIComponent(plotPos.plotX) + '&plotY=' + encodeURIComponent(plotPos.plotY))
    this.httpClient.get<OwnerData>(this.baseUrl + '/ownerdata', { params: params })
      .subscribe((result: OwnerData) => {

        this.loadClientData(result);

        // Reset the URL to reflect no account matic found
        if (this.owner.owner_matic_key === "") {
          this.router.navigate([]);
        }

        plotPos.rotateEle.classList.remove("rotate");        

      },
      error => {
        console.error(error)
      }
     );

    return;
  }

  loadClientData(clientData: OwnerData) {

    var rotateEle = document.getElementById("searchIcon");
    this.hideAll();

    this.owner = clientData;
    this.dataSource = null;

    this.hideBuildingFilter(this.owner.owner_land);

    if (this.owner.owner_land) {

      this.dataSource = new MatTableDataSource<OwnerLandData>(this.owner.owner_land);

      this.dataSource.sort = this.sort;

      // Add custom date column sort
      this.applySortDataAsccessor(this.dataSource);
      
      this.sort.sort(({ id: 'last_action', start: 'desc' }) as MatSortable);        // Default sort order on date

      // Reset the URL to reflect current account matic
      this.router.navigate([], { queryParams: { matic: this.owner.owner_matic_key }, });

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
  
  // Filter By District, and By Building Type [Storing pior District filter and using if found]
  filterTable(event, filterValue: number, buildingType: number) {

    var filterbyMulti: OwnerLandData[] = new Array();

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
      
      this.filterLandByDistrict = new Array();
      this.currentDistrictFilter = 0;

      filterbyMulti = this.owner.owner_land;

      this.buttonShowAll = false;
      this.hideBuildingFilter(filterbyMulti);
    }
        
    // Assign filtered dataset
    this.dataSource = new MatTableDataSource<OwnerLandData>(filterbyMulti);
    this.dataSource.sort = this.sort;
    this.applySortDataAsccessor(this.dataSource);


    return;
  }

  removeLinkHighlight(matchElement: string) {

    // Find all element with highlight - may be both district and building
    let districtLinkElements = this.elem.nativeElement.querySelectorAll(matchElement);

    if (districtLinkElements.length) {
      for (var index = 0, element; element = districtLinkElements[index]; index++) {
        element.classList.remove("districtEleActive");
        element.classList.remove("activeFilter");
      }
    }
    return;
  }

  // Called to reset the building filter buttons to match the current search - typically called on a new search instance.
  hideBuildingFilter(ownerLand: OwnerLandData[]) {

    this.initFilterCount();
    
    if (ownerLand !==null && ownerLand.length >0) {

      var buildingFilters = this.elem.nativeElement.querySelectorAll(".typeFilter div");
      if (buildingFilters.length >0) {
        for (var index = 0, element; element = buildingFilters[index]; index++) {
          element.classList.add("hideFilter");
        }
      }

      for (var index = 0, element; element = ownerLand[index]; index++) {
        switch (element.building_type) {
          case BUILDING.EMPTYPLOT: {
            this.emptyPlotFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.empty++;
            break;
          } 
          case BUILDING.INDUSTRIAL: {
            this.industrialFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.industry++;
            break;
          }
          case BUILDING.MUNICIPAL: {
            this.municipalFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.municipal++;
            break;
          }
          case BUILDING.PRODUCTION: {
            this.productionFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.production++;
            break;
          }
          case BUILDING.ENERGY: {
            this.energyFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.energy++;
            break;
          }
          case BUILDING.OFFICE: {
            this.officeFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.office++;
            break;
          }
          case BUILDING.RESIDENTIAL: {
            this.residentialFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.residential++;
            break;
          }
          case BUILDING.COMMERCIAL: {
            this.commercialFilter.nativeElement.classList.remove("hideFilter");
            this.filterCount.commercial++;
            break;
          }
          case BUILDING.AOI: {
            this.aoiFilter.nativeElement.classList.remove("hideFilter");
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

  hideOffer(componentVisible: boolean) {
    this.offerShow = !componentVisible;
  }

  hidePet(componentVisible: boolean) {
    this.petShow = !componentVisible;
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
