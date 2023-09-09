import { Component, Inject, ViewChild, EventEmitter, ElementRef } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Router, ActivatedRoute, Params } from '@angular/router';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { AfterViewInit } from '@angular/core';
import { interval, Observable, Subscription } from 'rxjs';
import { Globals, WORLD } from '../common/global-var';

declare const testNotificationClick: any;


interface District {
  update_instance: number;
  district_id: number;
  district_name: string;
  land_count: number;
  building_count: number;
  plots_claimed: number;
  energy_count: number;
  industry_count: number;
  production_count: number;
  office_count: number;
  commercial_count: number;
  municipal_count: number;
  owner_name: string;
  owner_avatar_id: number;
  owner_url: string;
  owner_matic: string;
  active_from: string;
  energy_tax: number;
  production_tax: number;
  commercial_tax: number;
  citizens_tax: number;
  promotion: string;
  promotion_start: string;
  promotion_end: string;
}


@Component({
  selector: 'district-notification-data',
  templateUrl: './district-notification.component.html',
  styleUrls: ['./district-notification.component.css']
})
export class DistrictNotificationComponent implements AfterViewInit {

  httpClient: HttpClient;
  baseUrl: string;

  public loopSwitch = false;
  public notifySubscription: Subscription;
  public district: District = null;

  dataSource = new MatTableDataSource(null);
  @ViewChild(MatSort, { static: true }) sort: MatSort;


  constructor(public globals: Globals, public router: Router, http: HttpClient, @Inject('BASE_URL') baseUrl: string, private elem: ElementRef)
  {
    this.httpClient = http;
    this.baseUrl = baseUrl + "api/" + globals.worldCode;

  }

  ngAfterViewInit() {
    //this.dataSource.sort = this.sort;
  }

  ngOnInit() {
    //testNotificationClick();
  }

  startLoop() {

    //in 10 seconds do something
    if (this.loopSwitch == false) {
      document.getElementById("searchIcon").classList.add("rotate");

      this.notifySubscription = interval(20000).subscribe(x => {

        // Animate the Process icon, change rotation direction per process.
        const originalIcon = document.getElementById("searchIcon");
        if (originalIcon.classList.contains("rotate"))
        {
          originalIcon.classList.remove("rotate");
          const clonedIcon = originalIcon.cloneNode(true) as HTMLElement;

          originalIcon.parentNode.insertBefore(clonedIcon, originalIcon);
          originalIcon.remove();

          clonedIcon.classList.add("rotateReverse");
        }
        else {
       
          originalIcon.classList.remove("rotateReverse");
          const clonedIcon = originalIcon.cloneNode(true) as HTMLElement;

          originalIcon.parentNode.insertBefore(clonedIcon, originalIcon);
          originalIcon.remove();

          clonedIcon.classList.add("rotate");
        }

        this.searchDistrict((document.getElementById("districtId") as HTMLInputElement).value);
      });

      this.loopSwitch = true;
    }
    else {

      this.loopSwitch = false;
      this.notifySubscription.unsubscribe();

      document.getElementById("searchIcon").classList.remove("rotate");

    }

  }

  // Single parameter struct containing 2 members, pushed by component search-plot
  searchDistrict(district_id: string) {

    let params = new HttpParams();
    //params = params.append('district_id', district_id);
    params = params.append('id', 'OrdersQuery');
    params = params.append('id', 'OrdersQuery');


    this.httpClient.get<District>(this.baseUrl + '/district/GetMCP', { params: params })
      .subscribe({
        next: (result) => {

          this.district = result;
          if (this.district.active_from != "" || this.district.owner_name != "") {
            testNotificationClick();

          }
        },
        error: (error) => { console.error(error) }
      });


    return;
  }


}
