import { Component, Inject, ViewChild, EventEmitter, ElementRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { NavigationEnd, NavigationStart, RouterEvent, Router, ActivatedRoute, Params } from '@angular/router';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { AfterViewInit } from '@angular/core';
import { element } from 'protractor';
import { ProdHistoryComponent } from '../production-history/prod-history.component';
import { OfferModalComponent } from '../offer-modal/offer-modal.component';
import { PetModalComponent } from '../pet-modal/pet-modal.component';
import { MatButton } from '@angular/material/button';
import { OwnerLandData, OwnerData, PlotPosition, BUILDING } from './owner-interface';


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
  public filterLandByDistrict: OwnerLandData[] = new Array();
  private currentDistrictFilter: number = 0;
  public buttonShowAll: boolean = false;
  public historyShow: boolean = false;
  public offerShow: boolean = false;
  public petShow: boolean = false;
  private subscriptionRouterEvent: any;

  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(ProdHistoryComponent, { static: true }) prodHistory: ProdHistoryComponent;
  @ViewChild(OfferModalComponent, { static: true }) offerModal: OfferModalComponent;
  @ViewChild(PetModalComponent, { static: true }) petModal: PetModalComponent;  

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
  @ViewChild('lowStaminaBtn', { static: false }) lowStaminaBtn: ElementRef;
  @ViewChild('offerDetailsBtn', { static: false }) offerDetailsBtn: ElementRef;
  @ViewChild('ownerSection', { static: false }) ownerSection: ElementRef;


  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['district_id', 'pos_x', 'pos_y', 'building_type', 'building_level', 'last_action', 'plot_ip', 'ip_bonus', 'citizen_count', 'token_id', 'citizen_stamina_alert' ];
 
  constructor(public router: Router, private route: ActivatedRoute, http: HttpClient, @Inject('BASE_URL') baseUrl: string, private elem: ElementRef)
  {
    this.httpClient = http;
    this.baseUrl = baseUrl;
    this.setInitVar();       

    // Check on URL change due to movement between features
    this.subscriptionRouterEvent = this.router.events.subscribe((event: RouterEvent) => {
      //console.log('current route: ', router.url.toString());

      if (event instanceof NavigationEnd) {
        // CASE reset the search to empty when moving from My Portfolio to Owner Report
        if (this.router.url.startsWith("/owner-data")) {
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
      this.searchOwnerbyMatic(requestOwnerMatic);
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
      owner_name: "Search for an Owner, Enter Plot X and Y position, click Find Owner.",
      owner_url: "https://mcp3d.com/tron/api/image/citizen/0",
      owner_matic_key: "",
      last_action: "",
      registered_date: "",
      last_visit: "",
      plot_count: -1,
      developed_plots: 0,
      plots_for_sale: 0,
      stamina_alert_count: 0,
      offer_count: 0,
      offer_sold_count: 0,
      owner_offer: null,
      owner_offer_sold: null,
      pet_count: 0,
      citizen_count: 0,
      district_plots: null,
      owner_land: null
    };
  }

  searchOwnerbyMatic(ownerMatic: string) {

    let params = new HttpParams();
    params = params.append('owner_matic_key', ownerMatic);

    var rotateEle = document.getElementById("searchIcon");
    rotateEle.classList.add("rotate");
    
    this.httpClient.get<OwnerData>(this.baseUrl + 'api/ownerdata/getusingmatic', { params: params })
      .subscribe((result: OwnerData) => {
        this.owner = result;

        this.hideBuildingFilter(this.owner.owner_land);

        if (this.owner.owner_land) {
          this.dataSource = new MatTableDataSource<OwnerLandData>(this.owner.owner_land);

          this.dataSource.sort = this.sort;

          // Add custom date column sort
          this.dataSource.sortingDataAccessor = (item: OwnerLandData, property) => {
            switch (property) {
              case 'last_action': return item.last_action == "Empty Plot" ? new Date(0) : new Date(item.last_action);
              default: return item[property];
            }
          };
        }

        if (this.owner.stamina_alert_count == 0) {
          this.lowStaminaBtn.nativeElement.classList.remove("btnAlert");
        }
        else {
          this.lowStaminaBtn.nativeElement.classList.add("btnAlert");
        }

        if (this.owner.offer_count == 0) {
          this.offerDetailsBtn.nativeElement.classList.remove("btnAlert");
        }
        else {
          this.offerDetailsBtn.nativeElement.classList.add("btnAlert");
        }          

        this.buttonShowAll = false;
        this.removeLinkHighlight(".districtEleActive, .activeFilter");
        rotateEle.classList.remove("rotate");

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
      this.ownerSection.nativeElement.classList.remove("blink");      
      plotPos.rotateEle.classList.remove("rotate");
      this.ownerSection.nativeElement.classList.add("blink");
      return;
    }

    //this.httpClient.get<OwnerData>(this.baseUrl + 'ownerdata/Get?plotX=' + encodeURIComponent(plotPos.plotX) + '&plotY=' + encodeURIComponent(plotPos.plotY))
    this.httpClient.get<OwnerData>(this.baseUrl + 'api/ownerdata', { params: params })
      .subscribe((result: OwnerData) => {

        this.owner = result;

        this.hideBuildingFilter(this.owner.owner_land);

        if (this.owner.owner_land) {
          this.dataSource = new MatTableDataSource<OwnerLandData>(this.owner.owner_land);

          this.dataSource.sort = this.sort;

          // Update URL to show friendly url to this player report
          this.router.navigate(['/owner-data'], { queryParams: { matic: this.owner.owner_matic_key } });
        }

        this.buttonShowAll = false;
        this.removeLinkHighlight(".districtEleActive, .activeFilter");

        plotPos.rotateEle.classList.remove("rotate");        

      },
      error => {
        console.error(error)
      }
     );

    return;
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
    if (buildingType > BUILDING.NO_FILTER && event.srcElement.parentElement.parentElement.classList.contains("activeFilter")) {
      
      filterbyMulti = this.currentDistrictFilter == 0 ? this.owner.owner_land : this.filterLandByDistrict;
      event.srcElement.parentElement.parentElement.classList.remove("activeFilter");

    }
    // Filter by District
    else if (filterValue > 0) {

      this.filterLandByDistrict = new Array();
      this.currentDistrictFilter = filterValue;

      this.owner.owner_land.forEach(land => {
        if (land.district_id == filterValue) {
          this.filterLandByDistrict.push(land);
        }
      });

      this.buttonShowAll = true;
      event.srcElement.parentElement.parentElement.classList.add("districtEleActive");
      filterbyMulti = this.filterLandByDistrict;

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
      event.srcElement.parentElement.parentElement.classList.add("activeFilter");
    }
    // Reset All Filter - Select All
    else {
      
      this.filterLandByDistrict = new Array();
      this.currentDistrictFilter = 0;

      filterbyMulti = this.owner.owner_land;

      this.buttonShowAll = false;
    }
        
    // Assign filtered dataset
    this.dataSource = new MatTableDataSource<OwnerLandData>(filterbyMulti);
    this.dataSource.sort = this.sort;

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

    this.filterLandByDistrict = new Array();  // Reset so that next search does not pull in pior search data

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
            break;
          } 
          case BUILDING.INDUSTRIAL: {
            this.industrialFilter.nativeElement.classList.remove("hideFilter");
            break;
          }
          case BUILDING.MUNICIPAL: {
            this.municipalFilter.nativeElement.classList.remove("hideFilter");
            break;
          }
          case BUILDING.PRODUCTION: {
            this.productionFilter.nativeElement.classList.remove("hideFilter");
            break;
          }
          case BUILDING.ENERGY: {
            this.energyFilter.nativeElement.classList.remove("hideFilter");
            break;
          }
          case BUILDING.OFFICE: {
            this.officeFilter.nativeElement.classList.remove("hideFilter");
            break;
          }
          case BUILDING.RESIDENTIAL: {
            this.residentialFilter.nativeElement.classList.remove("hideFilter");
            break;
          }
          case BUILDING.COMMERCIAL: {
            this.commercialFilter.nativeElement.classList.remove("hideFilter");
            break;
          }
          case BUILDING.AOI: {
            this.aoiFilter.nativeElement.classList.remove("hideFilter");            
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

  showHistory(asset_id: number, pos_x: number, pos_y: number) {
    
    this.prodHistory.searchHistory(asset_id, pos_x, pos_y);
    this.historyShow = true;
  }

  hideHistory(componentVisible: boolean) {
    this.historyShow = !componentVisible;
  }

  hidePet(componentVisible: boolean) {
    this.petShow = !componentVisible;
  }  

  showPet() {
    if (this.petShow == true) { // || this.owner.offer_count == 0) {
      this.petShow = false;
    }
    else {
      this.petModal.searchPets(this.owner.owner_matic_key);
      this.petShow = true;
    }
  }

  showCitizen() {

  }

  showOffer() {

    if (this.offerShow == true) { // || this.owner.offer_count == 0) {
      this.offerShow = false;
    }
    else {
      this.offerModal.loadTable(this.owner.owner_offer, this.owner.owner_offer_sold);
      this.offerShow = true;
    }

  }

  hideOffer(componentVisible: boolean) {
    this.offerShow = !componentVisible;
  }

}
