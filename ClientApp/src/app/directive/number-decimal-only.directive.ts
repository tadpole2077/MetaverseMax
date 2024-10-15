import { Directive, ElementRef, HostListener } from '@angular/core';

@Directive({
    standalone: true,
    selector: 'input[numbersDecimalOnly]'
})
export class NumberDecimalDirective {

    constructor(private _el: ElementRef) { }

  // Allow only numbers and decimal type enter into hooked input ctl
  // 0-9. chars allowed
  // start ^ and end $ regex deliminators not required
  //
  // Sequence : KeydownEvent >> inputEvent
  @HostListener('keydown', ['$event'])
  @HostListener('input', ['$event'])
    onInputChange(event) {

        switch (event.type) {
        case 'keydown': return this.keydownEvent(event);
        default: return this.inputEvent(event);
        }

    }


  inputEvent(event) {

      const initalValue = this._el.nativeElement.value;
      //const valueFourDecimalPlaces = 0;    

      const filteredValue = initalValue.replace(/[^0-9.]/g, '');

      // Enforce 4 decimal places
      if (filteredValue != null) {
          const fixed4Places = Number(filteredValue).toFixed(4).replace(/0+$/, '');
          this._el.nativeElement.value = fixed4Places;  
      }

      const splitValue = filteredValue.split('.');

      // 1) Support enter of 0.0, 1.00 - user may be trying to enter a small fraction, up to a max of 4 places
      // 2) Support one trailing decimal place entry - dont filter if user entered  ie 10.
      if (splitValue.length == 2 && splitValue[1].substring(splitValue[1].length -1) == '0' &&  splitValue[1].length <5) {
          this._el.nativeElement.value = filteredValue;
      } 
      else if (initalValue.indexOf('.') != initalValue.length - 1 ||
      initalValue.length == 0)
      {
          this._el.nativeElement.value = Number(this._el.nativeElement.value);    // remove any triling decimal point  eg 99.  
      }


      // If filtered value differs then original dont bubble event, meaning identify no change in number/decimal value.
      if (initalValue !== this._el.nativeElement.value) {
          event.stopPropagation();
      }
  }


  keydownEvent(event: KeyboardEvent) {
      // Is Key press a decimal point
      const decimalKeyPressed: boolean = event.key == '.';

      // CHECK Already a decimal point in input
      if (decimalKeyPressed && this._el.nativeElement.value.indexOf('.') != -1) {
          event.preventDefault();
          event.stopPropagation();
      }
  }


}
