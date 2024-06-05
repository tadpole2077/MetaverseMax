import { TestBed } from '@angular/core/testing';

import { OwnerDataFilterService } from './owner-data-filter.service';

describe('OwnerDataFilterService', () => {
  let service: OwnerDataFilterService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(OwnerDataFilterService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
