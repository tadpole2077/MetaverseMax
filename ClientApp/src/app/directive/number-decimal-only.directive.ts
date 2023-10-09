import { Directive, ElementRef, HostListener } from '@angular/core';

@Directive({
  selector: 'input[numbersDecimalOnly]'
})
export class NumberDecimalDirective {

  constructor(private _el: ElementRef) { }

  // Allow only numbers and decimal type enter into hooked input ctl
  // 0-9. chars allowed
  // start ^ and end $ regex deliminators not required
  //
  @HostListener('input', ['$event']) onInputChange(event) {
    
    const initalValue = this._el.nativeElement.value;

    this._el.nativeElement.value = initalValue.replace(/[^0-9.]/g, '');

    if (initalValue !== this._el.nativeElement.value) {
      event.stopPropagation();
    }

  }

}
