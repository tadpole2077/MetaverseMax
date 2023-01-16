import { HttpClient, HttpParams } from '@angular/common/http';
import { ChangeDetectorRef, Component, ElementRef, EventEmitter, Inject, Output, ViewChild } from '@angular/core';
import { PRIMARY_OUTLET, UrlSegment, UrlSegmentGroup, UrlTree, ActivatedRoute, NavigationEnd,RouterEvent, Router, Params } from '@angular/router';
import { Subscription } from 'rxjs';
import { Observable } from 'rxjs/Rx';
import { OwnerAccount, Globals, WORLD } from '../common/global-var';
import { NavMenuWorldComponent } from '../nav-menu-world/nav-menu-world.component';
import { Location } from '@angular/common';

import detectEthereumProvider from '@metamask/detect-provider';

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

  private subTron: Subscription;
  private attempts: number = 0;

  isExpanded = false;
  @Output() selectWorldEvent = new EventEmitter<any>();
  @ViewChild(NavMenuWorldComponent, { static: true }) menuWorld: NavMenuWorldComponent;
  @ViewChild('menuOwner', { static: false }) menuOwner: ElementRef;

  constructor(private cdf: ChangeDetectorRef, private location: Location, public globals: Globals, public activatedRoute: ActivatedRoute, private router: Router, http: HttpClient, @Inject('BASE_URL') rootBaseUrl: string) {

    this.rootBaseUrl = rootBaseUrl;
    this.httpClient = http;

    //let x = activatedRoute.snapshot;
    this.checkWorldFromURL();

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
    worldType = WORLD.TRON;
    this.selectWorld(worldType);

    return;
  }

  selectWorld(worldId: number) {

    var worldCode = (worldId == WORLD.TRON ? "trx" : worldId == WORLD.BNB ? "bnb" : "eth");
    this.baseUrl = this.rootBaseUrl + "api/" + worldCode;

    const routeTree: UrlTree = this.router.parseUrl(this.location.path());
    const routeSegmentGroup: UrlSegmentGroup = routeTree.root.children[PRIMARY_OUTLET];
    let segmentList: UrlSegment[];
    let lastComponentName: string = "/";
    let hasWorldTypeChanged: boolean = false;

    hasWorldTypeChanged = this.globals.selectedWorld != worldId;

    // Change page URL to match world type
    if (routeSegmentGroup != undefined) {
      segmentList = routeSegmentGroup.segments;
      lastComponentName = segmentList[segmentList.length - 1].path.toLowerCase();

      // CHECK if root home page - then ignore componentName as it may be either Empty or a World type - not an actual component name.
      lastComponentName = lastComponentName == "trx" || lastComponentName == "bnb" || lastComponentName == "eth" ? "" : lastComponentName

      //if (lastComponentName == "district-summary" && hasWorldTypeChanged) {
      //  lastComponentName = "district-list";      // redirect to district-list from summary page - as district wont match on world switch.
      //}
    }

    //console.log("first: " + firstRouteName);
    //console.log("last: " + lastComponentName);

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
        this.globals.firstCitizen = 1;
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

      this.globals.metamaskRequestApprove = false;

      // Delay check on Tron Widget load and init, must be a better way of hooking into it.  Try to find Tron account 5 times - 1 per second, on find run WS or end.
      this.subTron = Observable.interval(1000)
        .subscribe(
          (val) => {

            this.attempts++;
            
            if (this.globals.selectedWorld == WORLD.TRON) {

              const tronWeb = (window as any).tronWeb;
              
              if (tronWeb) {

                this.globals.CheckUserAccountKey(tronWeb.defaultAddress.base58, this.httpClient, this.baseUrl, this.cdf);
                this.subTron.unsubscribe();

              }
              else if (this.attempts >= 5) {

                this.subTron.unsubscribe();

              }
            }

          }
        );
    }
    else if (this.globals.selectedWorld == WORLD.BNB || this.globals.selectedWorld == WORLD.ETH ) {

      this.globals.getEthereumAccounts(this.httpClient, this.baseUrl, this.cdf);

      // On wallet accuont change - recheck linked account
      const ethereum = (window as any).ethereum;
      if (ethereum) {

        var that = this;

        ethereum.on('accountsChanged', function (accounts) {
          console.log(">>>Ethereum Account Changed<<<");
          that.globals.getEthereumAccounts(that.httpClient, that.baseUrl, this.cdf) ;
        });
      }

    }
  }

  collapse() {
    this.isExpanded = false;
  }

  toggle() {
    this.isExpanded = !this.isExpanded;
  }
}
