import { ErrorHandler } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Console, error } from 'console';

import { ErrorMetadataService } from './error-metadata.service';

// Testing a singleton class - meaning only one ErrorHandler override ErrorMetadataServicce exists
describe('ErrorMetadataService', () => {

    it('create an instance', () => {
        const errorMetaDataService = new ErrorMetadataService();
        expect(errorMetaDataService).toBeTruthy();
    });

    it('create console error', () => {

        const errorMetaDataService = new ErrorMetadataService();

        // Create a console spy that can be checked if console error event has occured.
        const consoleError = spyOn(globalThis.console, 'error');

        errorMetaDataService.handleError(new globalThis.Error('test error'));

        expect(consoleError).toHaveBeenCalled();

    });

    /*let mockService = {};
  let service: ErrorMetadataService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        { provider: ErrorMetadataService, useClass: ErrorHandler}
      ]
    });
    service = TestBed.inject(ErrorMetadataService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });*/
});
