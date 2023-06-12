import { Component, Output, EventEmitter, ViewChild, ElementRef } from '@angular/core';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-transfer-asset',
  templateUrl: './transfer-asset.component.html',
  styleUrls: ['./transfer-asset.component.css']
})
export class TransferAssetComponent {

  @Output() searchPlotEvent = new EventEmitter<any>();
  public rotateActive: boolean = false;

  constructor() {
  }

  getPlotData(plotPos) {

    //this.rotateActive = true;

    //this.searchPlotEvent.emit(plotPos);
  }

}
