import { Component, Output, Input, EventEmitter, ViewChild } from '@angular/core';
import { MatInputModule } from '@angular/material/input';
import { Pack, PRODUCT } from '../owner-data/owner-interface';

@Component({
  selector: 'app-transfer-asset',
  templateUrl: './transfer-asset.component.html',
  styleUrls: ['./transfer-asset.component.css']
})
export class TransferAssetComponent {

  @Input() index: number;

  @Output() searchPlotEvent = new EventEmitter<any>();
  public rotateActive: boolean = false;

  constructor() {
  }

  loadPlotData(row: Pack) {

    //this.rotateActive = true;

    //this.searchPlotEvent.emit(plotPos);
  }

}
