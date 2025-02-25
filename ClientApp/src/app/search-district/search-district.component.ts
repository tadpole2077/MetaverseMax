import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, Output, EventEmitter, Inject } from '@angular/core';
import { Router } from '@angular/router';
import { Application } from '../common/global-var';
import { MatButtonModule } from '@angular/material/button';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { CommonModule } from '@angular/common';


@Component({
    standalone: true,
    providers: [Application],
    imports: [CommonModule, MatButtonModule, NgbDropdownModule],
    selector: 'app-search-district',
    templateUrl: './search-district.component.html',
    styleUrls: ['./search-district.component.css']
})
export class SearchDistrictComponent {

    httpClient: HttpClient;
    baseUrl: string;
    public districtId_list: number[];

    @Output() searchDistrictEvent = new EventEmitter<any>();    // Need to insure only one event triggered - 2 buttons with this EventEmitter mapping on parent.

    constructor(public globals: Application, public router: Router, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

        this.httpClient = http;
        this.baseUrl = baseUrl + 'api/' + globals.worldCode;

        this.loadDistrictDropDown();
    }

    loadDistrictDropDown() {

        let params = new HttpParams();
        params = params.append('opened', 'true');

        this.httpClient.get<number[]>(this.baseUrl + '/district/getdistrictid_list', { params: params })
            .subscribe({
                next: (result) => {

                    this.districtId_list = result;
                },
                error: (error) => { console.error(error); }
            });


        return;
    }

    getDistrictData(districtId:number) {

        //plotPos.rotateEle = document.getElementById("searchIcon")
        //plotPos.rotateEle.classList.add("rotate");
          
        //this.router.navigate(['/district-summary'], { queryParams: { district_id: districtId } });
        this.searchDistrictEvent.emit(districtId);
    }

}
