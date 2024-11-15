import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Application } from '../common/global-var';
import { ICoordinates, IOwnerData } from '../owner-data/owner-interface';


// REST Service handler, segregated for easier unit testing with mock service and improved decoupling.
//  ability to centrally change REST to other Service type.
@Injectable({
    providedIn: 'root'
})
export class HttpOwnerDataService {

    constructor(public app: Application, private http: HttpClient, @Inject('BASE_URL') private baseUrl: string)
    {

    }

    // Return observable of async get promise
    searchPlot(plotPos: ICoordinates): Observable<IOwnerData> {

        let params = new HttpParams();
        params = params.append('plotX', plotPos.pos_x);
        params = params.append('plotY', plotPos.pos_y);

        const url = this.baseUrl + 'api/' + this.app.worldCode;        // need to rebuild url incase of world change, service obj is reused.

        return this.http.get<IOwnerData>(url + '/OwnerData', { params: params });

    }

    searchOwnerbyMatic(ownerMatic: string): Observable<IOwnerData> {

        let params = new HttpParams();
        params = params.append('owner_matic_key', ownerMatic);

        const url = this.baseUrl + 'api/' + this.app.worldCode;        // need to rebuild url incase of world change, service obj is reused.

        return this.http.get<IOwnerData>(url + '/ownerdata/getusingmatic', { params: params });
        //return this.http.get<IOwnerData>('/mock/api/');
    }

}
