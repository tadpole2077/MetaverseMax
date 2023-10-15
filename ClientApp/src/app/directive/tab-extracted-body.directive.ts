import { Directive, ElementRef, HostListener, Input, TemplateRef, ViewContainerRef } from '@angular/core';

@Directive({
  selector: '[tab-extracted-body]',
})
export class TabExtractedBodyDirective {
  constructor() {}
  //constructor(public tabTemplate: TemplateRef<any>) {
  //constructor(public tabTemplate: ViewContainerRef) {
    //var x = 1;
    //console.log('test1');
  //}
}
