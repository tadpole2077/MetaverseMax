import { Component, Output, EventEmitter, ViewChild, ElementRef } from '@angular/core';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-search-plot',
  templateUrl: './search-plot.component.html',
  styleUrls: ['./search-plot.component.css']
})
export class SearchPlotComponent {

  @Output() searchPlotEvent = new EventEmitter<any>();
  public rotateActive: boolean = false;

  constructor() {
  }

  getPlotData(plotPos) {

    this.rotateActive = true;
      
    this.searchPlotEvent.emit(plotPos);
  }

}
