import { Injectable, ErrorHandler } from '@angular/core';


// Add data to error messages - before sending to console
// No need to refactor underlying code - provides details on error messages.
// Angular 15+  - angular team recomend not to use injectable just plain class service. Its already included in providors array of app.module.
//@Injectable({
//  providedIn: 'root',  
//})
//@Injectable()
export class ErrorMetadataService implements ErrorHandler{

    handleError(error: any): void {

        const date = new Date();

        console.error({
            timestamp: date.toISOString(),
            message: error.message,
            zone: error.zone
        });
    }

}
