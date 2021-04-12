import { Component, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-search-plot',
  templateUrl: './search-plot.component.html',
  styleUrls: ['./search-plot.component.css']
})
export class SearchPlotComponent {

  @Output() searchPlotEvent = new EventEmitter<any>();

  constructor() {
  }

  GetPlotData(plotPos) {

    plotPos.rotateEle = document.getElementById("searchIcon")
    plotPos.rotateEle.classList.add("rotate");
      
    this.searchPlotEvent.emit(plotPos);
  }

}
