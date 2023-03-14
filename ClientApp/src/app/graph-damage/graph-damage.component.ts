import { Component, NgModule, ViewEncapsulation, Input, ElementRef, ViewChild } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { curveMonotoneX } from 'd3-shape';   //https://github.com/d3/d3-shape#curves
import { GraphData } from '../common/graph-interface';
import { graphDataTest } from './data-damage-test';


@Component({
  selector: 'app-graph-damage',
  templateUrl: './graph-damage.component.html',
  styleUrls: ['./graph-damage.component.css'],
  encapsulation: ViewEncapsulation.None           // removes encapsulation alias from autobuild styles allowing easier access
})
export class GraphDamageComponent {

  // ViewChild used for these elements to provide for rapid element attribute changes without need for scanning DOM and readability.
  @ViewChild('GraphDamage', { static: false }) graphDamage: ElementRef;

  @Input() graph_type: string;

  graphDataStored: GraphData;

  multi: any[];
  view: any[];

  // options
  legend: boolean = true
  showLabels: boolean = true;
  animations: boolean = true;
  showXAxisLabel: boolean = false;
  showYAxisLabel: boolean = true;
  xAxis: boolean = true;
  yAxis: boolean = true;

  showXAxis: boolean = true;
  showYAxis: boolean = true;
  gradient: boolean = true;
  showLegend: boolean = true;
  xAxisLabel: string;
  yAxisLabel: string;
  //yScaleMin: 100000;
  legendTitle: string;
  showDataLabel = true;
  timeline: false;
  yAxisTickFormatting: any = this.setYaxisLabel;
  curve: any = curveMonotoneX; // curveBasis;

  //public yAxisTickFormattingFn = this.setPrecentLabel.bind(this);

  colorScheme = {
    domain: []
  };

  constructor(private elem: ElementRef) {
    //this.view = [ 500, 200 ]; // default sizing helps on page initial render.
    //this.loadGraph();
  }

  ngAfterViewChecked() {
    const el = document.querySelectorAll('g.line-series path')[2];
    if (el) {
      el.setAttribute('stroke-width', '10');
      el.setAttribute('stroke-linecap', 'round');
    }
  }

  // Databound Inputs passed by Parent comp are only accessible from OnInit stage.
  public loadGraph(graphData: GraphData) {

    if (graphData == null) {
      graphData = graphDataTest;
    }

    this.graphDataStored = graphData;
    this.xAxisLabel = graphData.x_axis_label;
    this.yAxisLabel = graphData.y_axis_label;
    this.showXAxisLabel = graphData.show_xaxis_label;
    this.showYAxisLabel = graphData.show_yaxis_label;

    this.legendTitle = graphData.legend_title;
    this.showLegend = graphData.show_legend;

    //this.view = graphData.view;
    this.colorScheme.domain = graphData.domain;

    this.multi = graphData.graphColumns;

    Object.assign(this, this.multi);

    this.graphDamage.nativeElement.classList.add("showTrans");

  }

  setYaxisLabel(val) {

    let graphPostAppend: string = "%";

    if (this.graphDataStored) {
      graphPostAppend = this.graphDataStored.y_axis_postappend;
    }

    return val.toLocaleString() + graphPostAppend;    // eg " Trx"
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
