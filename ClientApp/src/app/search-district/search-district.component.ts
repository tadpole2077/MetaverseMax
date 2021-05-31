import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, Output, EventEmitter, Inject } from '@angular/core';
import { NgbDropdown } from '@ng-bootstrap/ng-bootstrap';
import { Router, ActivatedRoute, Params } from '@angular/router';


@Component({
  selector: 'app-search-district',
  templateUrl: './search-district.component.html',
  styleUrls: ['./search-district.component.css']
})
export class SearchDistrictComponent {

  httpClient: HttpClient;
  baseUrl: string;
  public districtId_list: number[];

  @Output() searchDistrictEvent = new EventEmitter<any>();

  constructor(public router: Router, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

    this.httpClient = http;
    this.baseUrl = baseUrl;

    this.loadDistrictDropDown();
  }

  loadDistrictDropDown() {

    let params = new HttpParams();
    params = params.append('opened', 'true');

    this.httpClient.get<number[]>(this.baseUrl + 'api/district/getdistrictid_list', { params: params })
      .subscribe((result: number[]) => {

        this.districtId_list = result;
        //this.removeLinkHighlight();
        //plotPos.rotateEle.classList.remove("rotate");

      }, error => console.error(error));


    return;
  }

  GetDistrictData(districtId:number) {

    //plotPos.rotateEle = document.getElementById("searchIcon")
    //plotPos.rotateEle.classList.add("rotate");
          
    this.router.navigate(['/district-summary'], { queryParams: { district_id: districtId } });
    this.searchDistrictEvent.emit(districtId);
  }

}
