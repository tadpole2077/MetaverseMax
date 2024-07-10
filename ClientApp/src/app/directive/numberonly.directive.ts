import { Directive, ElementRef, HostListener, Input } from '@angular/core';
import { NgControl } from '@angular/forms';

@Directive({
    selector: 'input[numbersOnly]'
})
export class NumberDirective {

    constructor(private _el: ElementRef) { }

  // Allow only numbers type enter into hooked input ctl
  @HostListener('input', ['$event']) onInputChange(event) {

        const initalValue = this._el.nativeElement.value;

        this._el.nativeElement.value = initalValue.replace(/[^0-9]*/g, '');

        if (initalValue !== this._el.nativeElement.value) {
            event.stopPropagation();
        }

    }

}
