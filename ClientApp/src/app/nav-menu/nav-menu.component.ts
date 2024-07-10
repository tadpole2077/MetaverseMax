import { HttpClient, HttpParams } from '@angular/common/http';
import { ChangeDetectorRef, Component, ElementRef, EventEmitter, Inject, NgZone, Output, ViewChild } from '@angular/core';
import { PRIMARY_OUTLET, UrlSegment, UrlSegmentGroup, UrlTree, ActivatedRoute, NavigationEnd, RouterEvent, Router, Params } from '@angular/router';
import { OwnerAccount, Application, WORLD } from '../common/global-var';
import { NavMenuWorldComponent } from '../nav-menu-world/nav-menu-world.component';
import { Location } from '@angular/common';


@Component({
    selector: 'app-nav-menu',
    templateUrl: './nav-menu.component.html',
    styleUrls: ['./nav-menu.component.css']
})
export class NavMenuComponent {

    private subscriptionRouterEvent: any;
    private httpClient: HttpClient;
    private baseUrl: string;
    private rootBaseUrl: string;
    public worldName: string;
    private that: any = this;

    isExpanded = false;
  @Output() selectWorldEvent = new EventEmitter<any>();
  @Output() darkModeChangeEvent = new EventEmitter<any>();
  @ViewChild(NavMenuWorldComponent, { static: true }) menuWorld: NavMenuWorldComponent;
  @ViewChild('menuOwner', { static: false }) menuOwner: ElementRef;


  constructor(private zone: NgZone, private cdf: ChangeDetectorRef, private location: Location, public globals: Application, public activatedRoute: ActivatedRoute, private router: Router, http: HttpClient, @Inject('BASE_URL') rootBaseUrl: string) {

      this.rootBaseUrl = rootBaseUrl;     // Unknow world type at this point, checkWorldFromURL will identify.
      this.httpClient = http;
      globals.menuCDF = cdf;

      this.checkWorldFromURL();

  }

  darkModeChange(modeEnabled: boolean) {

      this.darkModeChangeEvent.emit(modeEnabled);    // bubble event up to parent component
  }

  // Unknown world type - check URL to id world
  checkWorldFromURL() {

      // The router.url may not be ready when this nav-menu component is generated as its very early in the page render cycle, but the location.path is
      const routeTree: UrlTree = this.router.parseUrl(this.location.path());
      const routeSegmentGroup: UrlSegmentGroup = routeTree.root.children[PRIMARY_OUTLET];
      let segmentList: UrlSegment[] = [];
      let worldType: WORLD = WORLD.UNKNOWN;    

      if (routeSegmentGroup != undefined) {
          segmentList = routeSegmentGroup.segments;
      }

      for (let counter = 0; counter < segmentList.length; counter++) {

          const seg = segmentList[counter].path.toLowerCase();
          // Attempt to only find and eval path segment that match code size - 3 chars. NOT GREAT SOLUTION, but works for now - due to prod having additional segment of api as segment 1 vs DEV.
          if (seg == 'trx') {
              worldType = WORLD.TRON;
          }
          else if (seg == 'eth') {
              worldType = WORLD.ETH;
          }
          else if (seg == 'bnb') {
              worldType = WORLD.BNB;
          }
      }

      worldType = worldType == WORLD.UNKNOWN ? this.globals.selectedWorld : worldType;    // Use default world assigned in Globals if no world selected.    

      this.selectWorld(worldType);

      return;
  }

  selectWorld(worldId: number) {

      const worldCode = (worldId == WORLD.TRON ? 'trx' : worldId == WORLD.BNB ? 'bnb' : 'eth');
      this.baseUrl = this.rootBaseUrl + 'api/' + worldCode;
      let segmentList: UrlSegment[];
      const routeTree: UrlTree = this.router.parseUrl(this.location.path());
      let lastComponentName = '/';
      let hasWorldTypeChanged = false;

      hasWorldTypeChanged = this.globals.selectedWorld != worldId;
      segmentList = this.globals.extractPathComponents(this.location.path());

      // Change page URL to match world type
      if (segmentList != null) {
      
          lastComponentName = segmentList[segmentList.length - 1].path.toLowerCase();

          // CHECK if root home page - then ignore componentName as it may be either Empty or a World type - not an actual component name.
          lastComponentName = lastComponentName == 'trx' || lastComponentName == 'bnb' || lastComponentName == 'eth' ? '' : lastComponentName;

      //if (lastComponentName == "district-summary" && hasWorldTypeChanged) {
      //  lastComponentName = "district-list";      // redirect to district-list from summary page - as district wont match on world switch.
      //}
      }

      //console.log("first: " + firstRouteName);
      //console.log("last: " + lastComponentName);

      // Reset Browser URL to match selected world
      const navigateTo: string = '/' + worldCode + (lastComponentName != '' && lastComponentName != '/' ? '/' + lastComponentName : '');
      this.router.navigate([navigateTo], { queryParams: routeTree.queryParams });

      // Clear Account and reset check on wallet link
      const priorDarkModeStatus = this.globals.ownerAccount.dark_mode;      // retain existing mode
      this.globals.initAccount();
      this.globals.ownerAccount.dark_mode = priorDarkModeStatus;

      this.assignGlobalVar(worldCode);
      this.registerOwnerKey();    

      return;
  }

  assignGlobalVar(worldName: string)
  {
      switch (worldName) {
      case 'bnb': {
          this.globals.selectedWorld = WORLD.BNB;
          this.globals.worldURLPath = 'https://mcp3d.com/bsc/api/image/';
          this.globals.firstCitizen = 24;
          this.globals.worldCode = 'bnb';
          this.globals.worldName = 'BSC';
          break;
      }
      case 'eth': {
          this.globals.selectedWorld = WORLD.ETH;
          this.globals.worldURLPath = 'https://mcp3d.com/api/image/';
          this.globals.firstCitizen = 1;
          this.globals.worldCode = 'eth';
          this.globals.worldName = 'Ethereum';
          break;
      }
      case 'trx':
      default:
      {
          this.globals.selectedWorld = WORLD.TRON;
          this.globals.worldURLPath = 'https://mcp3d.com/tron/api/image/';
          this.globals.firstCitizen = 0;
          this.globals.worldCode = 'trx';
          this.globals.worldName = 'Tron';
          break;
      }
      }
  }

  registerOwnerKey()
  {
      this.globals.getProviders(this.globals.selectedWorld);

      if (this.globals.selectedWorld == WORLD.TRON) {
      
          this.globals.requestApprove = false;
          this.globals.getTronAccounts(this.baseUrl);
      
      }
      else if (this.globals.selectedWorld == WORLD.BNB || this.globals.selectedWorld == WORLD.ETH ) {

          this.globals.requestApprove = false;
          this.globals.getEthereumAccounts(this.baseUrl, false);      

      }

      //this.setEventListeners(this.globals.selectedWorld);
  }

  collapse() {
      this.isExpanded = false;
  }

  toggle() {
      this.isExpanded = !this.isExpanded;
  }

  // to avoid a "Navigation triggered outside Angular zone" error, due to newly rendered links from wallet site link, need to run navigation within zone.run()
  navMyPortfolio() {
      this.zone.run(() => {
          this.router.navigate(['/', this.globals.worldCode, 'owner-data'], { queryParams: { matic: 'myportfolio' }, });
      });
  }
  navRanking() {
      this.zone.run(() => {
          this.router.navigate(['/', this.globals.worldCode, 'building-ip'], { });
      });
  }

}
