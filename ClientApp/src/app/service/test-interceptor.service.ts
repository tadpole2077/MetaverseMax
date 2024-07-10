import { HttpRequest, HttpHandler, HttpEvent, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { stringify } from 'querystring';
import { Observable } from 'rxjs';
import { mockOwnerData } from '../mocks/mock-owner-data';
import { IOwnerData } from '../owner-data/owner-interface';

// Intercepter Service - Mock REST Service request - response - using mock data.

@Injectable({
    providedIn: 'root'
})
export class TestInterceptorService {

    private readonly API_URL = '/mock/api/';                  // mock REST url
    private readonly STORAGE_key = 'mock_api_filter';         // key used to store data in local storage

    constructor() { }

    // Para1 : current request
    // Para2 : handles response back to request. HttpHandler - used to dispach a request in a stream of requests.
    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {

        console.log('intercept test');

        if (request.url.startsWith(this.API_URL) && request.method === 'GET') {
            return this.getOwnerData();
        }
        if (request.url === this.API_URL && request.method === 'PUT'){
            return this.setFilter(request.body);
        }
    
        // if request does  not match the url, or is not a Get or Put type then just return the http handler - execute as normal.
        return next.handle(request);
    }

    // The mock services are using the browser local storage to store and retrieve the filter key.  
    private getOwnerData(): Observable<HttpEvent<any>>{

        // Return a mock http response event.
        return new Observable(observer => {

            observer.next(new HttpResponse<IOwnerData>({
                status: 200,
                body: mockOwnerData
            }));
      
            observer.complete();    // release the observer and automatically unsubscribe ALL subscribers 
        });

    }

    private setFilter(body:string): Observable<HttpEvent<IOwnerData>>{

        window.localStorage.setItem(this.STORAGE_key, body);
        return this.getOwnerData();

    }
}
