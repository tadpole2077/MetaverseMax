import {AbstractControl, ValidationErrors, ValidatorFn} from '@angular/forms';

// Check if entered value is less then balance
export function maxBalanceValidator(): ValidatorFn {
    return (control:AbstractControl) : ValidationErrors | null => {

        const value = control.value;

        // check for no user entered number
        if (!value) {
            return null;
        }


      return value < 100 ? { lessMaxBalance: true } : null ;
    }
}
