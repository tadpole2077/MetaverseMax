import { Component, Inject, ViewChild, EventEmitter, Renderer, ElementRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { AfterViewInit } from '@angular/core';
import { element } from 'protractor';
import { ProdHistoryComponent } from '../production-history/prod-history.component';

// Service Interfaces
export interface OwnerLandData {
  district_id: number;
  pos_x: number;
  pos_y: number;
  building_type: number;
  building_desc: string;
  building_img: string;
  last_action: string;
  plot_ip: number;
  ip_bonus: number;
  token_id: number;
  building_level: number;
  citizen_count: number;
  citizen_url: string;
  citizen_stamina: number;
  citizen_stamina_alert: boolean;
  forsale: boolean;
  forsale_price: number;
  alert: boolean;
}

interface OwnerData {
  owner_name: string;
  owner_url: string;
  owner_matic_key: string;
  last_action: string;
  registered_date: string;
  last_visit: string;
  plot_count: number;
  developed_plots: number;
  plots_for_sale: number;
  district_plots: DistrictPlot[];
  owner_land: OwnerLandData[];
}

interface  PlotPosition{
  plotX: string,
  plotY: string,
  rotateEle: Element
}

interface DistrictPlot {
  district: number[];
}


@Component({
  selector: 'app-owner-data',
  templateUrl: './owner-data.component.html',
  styleUrls: ['./owner-data.component.css']
})
export class OwnerDataComponent implements AfterViewInit {

  httpClient: HttpClient;
  baseUrl: string;

  public owner: OwnerData;
  public filterLand: OwnerLandData[] = null;
  private buttonShowAll: boolean = false;
  public historyShow: boolean = false;

  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(ProdHistoryComponent, { static: true }) child:ProdHistoryComponent;

  // Must match fieldname of source type for sorting to work, plus match the column matColumnDef
  displayedColumns: string[] = ['district_id', 'pos_x', 'pos_y', 'building_type', 'building_level', 'last_action', 'plot_ip', 'ip_bonus', 'citizen_count', 'token_id', 'citizen_stamina_alert' ];
 
  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string, private renderer: Renderer, private elem: ElementRef)
  {
    this.httpClient = http;
    this.baseUrl = baseUrl;
    this.owner = {
      owner_name: "Search for an Owner, Enter Plot X and Y position, click Find Owner.",
      owner_url: "https://mcp3d.com/tron/api/image/citizen/0",
      owner_matic_key: "",
      last_action: "",
      registered_date: "",
      last_visit:"",
      plot_count: 0,
      developed_plots: 0,
      plots_for_sale: 0,
      district_plots: null,
      owner_land: null      
    };
    //let plotPos: PlotPosition = { plotX: "0", plotY: "0"}; 
    //this.searchPlot(plotPos);
  }

  ngAfterViewInit() {
    //this.dataSource.sort = this.sort;
  }

  // Single parameter struct containing 2 members, pushed by component search-plot
  searchPlot(plotPos: PlotPosition ) {

    let params = new HttpParams();
    params = params.append('plotX', plotPos.plotX);
    params = params.append('plotY', plotPos.plotY);

    if (plotPos) {
      //wsParameters = JSON.stringify([{ plotX: plotPos.plotX, plotY: plotPos.plotY }])
    }

    //this.httpClient.get<OwnerData>(this.baseUrl + 'ownerdata/Get?plotX=' + encodeURIComponent(plotPos.plotX) + '&plotY=' + encodeURIComponent(plotPos.plotY))
    this.httpClient.get<OwnerData>(this.baseUrl + 'api/ownerdata', { params: params })
      .subscribe((result: OwnerData) => {
        this.owner = result;

        if (this.owner.owner_land) {
          this.dataSource = new MatTableDataSource<OwnerLandData>(this.owner.owner_land);

          this.dataSource.sort = this.sort;
        }

        this.buttonShowAll = false;
        this.removeLinkHighlight();

        plotPos.rotateEle.classList.remove("rotate");

      }, error => console.error(error));

    return;
  }
  //, requestLink: Element
  filterTable(event, filterValue: number) {

    this.filterLand = new Array();
    this.removeLinkHighlight();
    if (filterValue > 0) {

      this.owner.owner_land.forEach(land => {
        if (land.district_id == filterValue) {
          this.filterLand.push(land);
        }
      });

      this.buttonShowAll = true;
      event.srcElement.parentElement.parentElement.classList.add("districtEleActive");

    }
    else {
      this.filterLand = this.owner.owner_land;
      this.buttonShowAll = false;
    }
    
    
    // Assign filtered dataset
    this.dataSource = new MatTableDataSource<OwnerLandData>(this.filterLand);
    this.dataSource.sort = this.sort;
    //this.dataSource.filter = filterValue;

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

  showHistory(asset_id: number) {
    ProdHistoryComponent
    this.child.searchHistory(asset_id);
    this.historyShow = true;
  }

  hideHistory(componentVisible: boolean) {
    this.historyShow = !componentVisible;
  }
}
