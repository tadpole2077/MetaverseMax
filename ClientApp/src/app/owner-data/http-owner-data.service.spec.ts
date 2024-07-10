import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Application } from '../common/global-var';

import { HttpOwnerDataService } from './http-owner-data.service';

describe('HttpOwnerDataService', () => {
    let service: HttpOwnerDataService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                { provide: Application, useValue: jasmine.createSpyObj('Application', ['worldCode']) },
                { provide: HttpClient, useValue: jasmine.createSpyObj('HttpClient', ['get']) },      
                { provide: 'BASE_URL', useValue: 'localhost:9876' }
            ]
        });
        service = TestBed.inject(HttpOwnerDataService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });
});
