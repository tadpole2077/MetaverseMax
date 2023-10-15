import { Component, ContentChild, ContentChildren, Input, QueryList, TemplateRef } from "@angular/core";
import { TabExtractedBodyDirective } from '../directive/tab-extracted-body.directive';

@Component({
  selector: 'app-tab-container-lazy',
  templateUrl: './tab-container-lazy.component.html',
  styleUrls: ['./tab-container-lazy.component.css']
})


export class TabContainerLazyComponent {
  
  @ContentChild(TabExtractedBodyDirective, { read: TemplateRef, static: false }) tabTemplateRef: TemplateRef<unknown>;
  @ContentChildren(TabExtractedBodyDirective) allDirectives: QueryList<TabExtractedBodyDirective>;
  
  @Input() isOpen: boolean = false;
}
