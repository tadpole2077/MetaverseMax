import { Component, NgModule, ViewEncapsulation, Input } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { NgxChartsModule } from '@swimlane/ngx-charts';
//import { graphDataConstruct } from './data-construct';
//import { graphDataProduce } from './data-produce';
import { OwnerSummary, District } from '../district-summary/data-district-interface';
import { GraphData } from '../common/graph-interface';


@Component({
    selector: 'app-graph-tax',
    templateUrl: './graph-tax.component.html',
    styleUrls: ['./graph-tax.component.css'],
    encapsulation: ViewEncapsulation.None
})
export class GraphTaxComponent {

  @Input() graph_type: string;

  //graphDataConstruct: any;
  //graphDataProduce: any;

  public multi: any[];
  view: any[];

  // options
  showXAxis = true;
  showYAxis = true;
  gradient = true;
  showLegend = true;
  showXAxisLabel = true;
  xAxisLabel: string;
  showYAxisLabel = true; 
  yAxisLabel: string;
  legendTitle: string;
  showDataLabel = true;
  yAxisTickFormatting: any = this.setPrecentLabel;

  public yAxisTickFormattingFn = this.setPrecentLabel.bind(this);

  colorScheme = {
      domain: []
  };

  constructor() {
  }

  // Databound Inputs passed by Parent comp are only accessible from OnInit stage.
  public loadGraph(graphTax: GraphData) {

      this.xAxisLabel = graphTax.x_axis_label;
      this.yAxisLabel = graphTax.y_axis_label;
      this.legendTitle = graphTax.legend_title;
      this.showLegend = graphTax.show_legend;
      this.showYAxisLabel = graphTax.show_yaxis_label;
      //this.view = graphTax.view;
      this.colorScheme.domain = graphTax.domain;
      const multiData = this.multi = graphTax.graphColumns;

      Object.assign(this, { multiData });

  }

  setPrecentLabel(val) {
      return val.toLocaleString() + ' %';
  }

  onSelect(data): void {
      //console.log('Item clicked', JSON.parse(JSON.stringify(data)));   
  }

  onActivate(data): void {
      //console.log('Activate', JSON.parse(JSON.stringify(data)));
  }

  onDeactivate(data): void {
      //console.log('Deactivate', JSON.parse(JSON.stringify(data)));
  }
}
