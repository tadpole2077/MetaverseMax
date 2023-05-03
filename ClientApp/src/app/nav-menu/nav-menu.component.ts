import { HttpClient, HttpParams } from '@angular/common/http';
import { ChangeDetectorRef, Component, ElementRef, EventEmitter, Inject, NgZone, Output, ViewChild } from '@angular/core';
import { PRIMARY_OUTLET, UrlSegment, UrlSegmentGroup, UrlTree, ActivatedRoute, NavigationEnd, RouterEvent, Router, Params } from '@angular/router';
import { OwnerAccount, Globals, WORLD } from '../common/global-var';
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


  constructor(private zone: NgZone, private cdf: ChangeDetectorRef, private location: Location, public globals: Globals, public activatedRoute: ActivatedRoute, private router: Router, http: HttpClient, @Inject('BASE_URL') rootBaseUrl: string) {

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
    var segmentList: UrlSegment[] = [];
    let worldType: number = WORLD.UNKNOWN;    

    if (routeSegmentGroup != undefined) {
      segmentList = routeSegmentGroup.segments;
    }

    for (var counter = 0; counter < segmentList.length; counter++) {

      let seg = segmentList[counter].path.toLowerCase();
      // Attempt to only find and eval path segment that match code size - 3 chars. NOT GREAT SOLUTION, but works for now - due to prod having additional segment of api as segment 1 vs DEV.
      if (seg == "trx") {
        worldType = WORLD.TRON;
      }
      else if (seg == "eth") {
        worldType = WORLD.ETH;
      }
      else if (seg == "bnb") {
        worldType = WORLD.BNB;
      }
    }

    worldType = worldType == 0 ? WORLD.TRON : worldType;    // Default Tron if no world identified - using old URL with no world segment.    
    //worldType = WORLD.TRON;  -- force world use of tron during dev mode
    this.selectWorld(worldType);

    return;
  }

  selectWorld(worldId: number) {

    var worldCode = (worldId == WORLD.TRON ? "trx" : worldId == WORLD.BNB ? "bnb" : "eth");
    this.baseUrl = this.rootBaseUrl + "api/" + worldCode;
    let segmentList: UrlSegment[];
    const routeTree: UrlTree = this.router.parseUrl(this.location.path());
    let lastComponentName: string = "/";
    let hasWorldTypeChanged: boolean = false;

    hasWorldTypeChanged = this.globals.selectedWorld != worldId;
    segmentList = this.globals.extractPathComponents(this.location.path());

    // Change page URL to match world type
    if (segmentList != null) {
      
      lastComponentName = segmentList[segmentList.length - 1].path.toLowerCase();

      // CHECK if root home page - then ignore componentName as it may be either Empty or a World type - not an actual component name.
      lastComponentName = lastComponentName == "trx" || lastComponentName == "bnb" || lastComponentName == "eth" ? "" : lastComponentName

      //if (lastComponentName == "district-summary" && hasWorldTypeChanged) {
      //  lastComponentName = "district-list";      // redirect to district-list from summary page - as district wont match on world switch.
      //}
    }

    //console.log("first: " + firstRouteName);
    //console.log("last: " + lastComponentName);

    // Reset Browser URL to match selected world
    let navigateTo: string = '/' + worldCode + (lastComponentName != "" && lastComponentName != "/" ? '/' + lastComponentName : "");
    this.router.navigate([navigateTo], { queryParams: routeTree.queryParams });

    // Clear Account and reset check on wallet link
    this.globals.initAccount();
    this.assignGlobalVar(worldCode);
    this.registerOwnerKey();    

    return;
  }

  assignGlobalVar(worldName: string)
  {
    switch (worldName) {
      case "bnb": {
        this.globals.selectedWorld = WORLD.BNB;
        this.globals.worldURLPath = "https://mcp3d.com/bsc/api/image/";
        this.globals.firstCitizen = 24;
        this.globals.worldCode = "bnb";
        this.globals.worldName = "BSC";
        break;
      }
      case "eth": {
        this.globals.selectedWorld = WORLD.ETH;
        this.globals.worldURLPath = "https://mcp3d.com/api/image/";
        this.globals.firstCitizen = 1;
        this.globals.worldCode = "eth";
        this.globals.worldName = "Ethereum";
        break;
      }
      case "trx":
      default:
        {
          this.globals.selectedWorld = WORLD.TRON;
          this.globals.worldURLPath = "https://mcp3d.com/tron/api/image/";
          this.globals.firstCitizen = 0;
          this.globals.worldCode = "trx";
          this.globals.worldName = "Tron";
          break;
        }
    }
  }

  registerOwnerKey()
  {    
    if (this.globals.selectedWorld == WORLD.TRON) {

      this.globals.requestApprove = false;
      this.globals.getTronAccounts(this.httpClient, this.baseUrl);
      
    }
    else if (this.globals.selectedWorld == WORLD.BNB || this.globals.selectedWorld == WORLD.ETH ) {

      this.globals.requestApprove = false;
      this.globals.getEthereumAccounts(this.httpClient, this.baseUrl, false);      

    }

    this.setEventListeners(this.globals.selectedWorld);
  }

  // Only set once to avoid dups, remove listeners when switching worlds.
  setEventListeners(worldId: number) {

    if (this.globals.selectedWorld == WORLD.TRON) {

      window.removeEventListener("message", this.trxAccountsChanged);
      window.addEventListener("message", this.trxAccountsChanged);


      const ethereum = (window as any).ethereum;
      if (ethereum) {
        ethereum.removeListener("accountsChanged", this.ethAccountsChanged);     // ethereum obj using Node.js EventEmitter
      }
    }
    else if (this.globals.selectedWorld == WORLD.ETH || this.globals.selectedWorld == WORLD.BNB )
    {
      window.removeEventListener("message", this.trxAccountsChanged);           // Remove Tron event listener

      // On wallet account change - recheck linked account
      const ethereum = (window as any).ethereum;
      if (ethereum) {

        var that = this;

        // ethereum obj using Node.js EventEmitter tech
        ethereum.removeListener("accountsChanged", this.ethAccountsChanged);     // ensure only one instance of Eth event handler - remove any existing, might occur during a world change
        ethereum.on("accountsChanged", this.ethAccountsChanged);
      }
    }
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


  // Using named function var with [ES6 Arrow Function] - allows use of [this] pointing to the original caller class, otherwise the eventEmitter class will be used.
  private ethAccountsChanged = (accounts) => {
    console.log("Ethereum Account Changed");

    this.globals.initAccount();
    this.globals.getEthereumAccounts(this.httpClient, this.baseUrl, true);

  };

  // Using [ES6 Arrow Function], to support (a) using (component) this obj ref (b)support  window.removeEventListener()
  private trxAccountsChanged = (e) => {
    /*if (e.data.message && e.data.message.action == "setAccount") {
      console.log("setAccount event", e.data.message);
      console.log("current address:", e.data.message.data.address);

      this.globals.checkTronAccountKey(this.httpClient, this.baseUrl, this.cdf);
    }*/

    if (e.data.message && e.data.message.action == "accountsChanged") {
      console.log("Tron accountsChanged event", e.data.message);
      console.log("Tron current address:", e.data.message.data.address);

      this.globals.initAccount();
      this.globals.checkTronAccountKey(this.httpClient, this.baseUrl, true);
    }

  };
}
