import { Component, NgModule, ViewEncapsulation, Input } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { NgxChartsModule } from '@swimlane/ngx-charts';
//import { graphDataConstruct } from './data-construct';
//import { graphDataProduce } from './data-produce';
import { OwnerSummary, District } from '../district-summary/data-district-interface';
import { GraphData } from '../common/graph-interface';


@Component({
  selector: 'app-tax-graph',
  templateUrl: './tax-graph.component.html',
  styleUrls: ['./tax-graph.component.css'],
  encapsulation: ViewEncapsulation.None
})
export class TaxGraphComponent {

  @Input() graph_type: string;

  //graphDataConstruct: any;
  //graphDataProduce: any;

  multi: any[];
  view: any[];

  // options
  showXAxis: boolean = true;
  showYAxis: boolean = true;
  gradient: boolean = true;
  showLegend: boolean = true;
  showXAxisLabel: boolean = true;
  xAxisLabel: string;
  showYAxisLabel: boolean = true; 
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
    this.multi = graphTax.graphColumns;    

    Object.assign(this, this.multi);

  }

  setPrecentLabel(val) {
    return val.toLocaleString() + " %";
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