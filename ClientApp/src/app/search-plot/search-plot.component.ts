import { Component, Output, EventEmitter } from '@angular/core';
import { ICoordinates } from '../owner-data/owner-interface';

@Component({
    selector: 'app-search-plot',
    templateUrl: './search-plot.component.html',
    styleUrls: ['./search-plot.component.css']
})
export class SearchPlotComponent {

  @Output() searchPlotEvent = new EventEmitter<ICoordinates>();
  public rotateActive = false;
  public valueY: string;
  public valueX: string;

  constructor() {
  }

  getPlotData(posX: number, posY: number) {

      this.rotateActive = true;
      const selectedCoord: ICoordinates = { pos_x: Number(posX), pos_y: Number(posY) };
      
      this.searchPlotEvent.emit(selectedCoord);
  }

}
