import { TestBed } from '@angular/core/testing';

import { AlertManagerService } from './alert-manager.service';

describe('AlertManagerService', () => {
    let service: AlertManagerService;

    beforeEach(() => {
        TestBed.configureTestingModule({});
        service = TestBed.inject(AlertManagerService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });
});
