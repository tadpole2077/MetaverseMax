import { Component, ViewEncapsulation, Input, ElementRef, ViewChild, ChangeDetectionStrategy } from '@angular/core';
import { graphDataFundTest } from './data-fund-test';
import { GraphData } from '../common/graph-interface';
import { ChangeDetectorRef } from '@angular/core';

// Browser warning in Brave - due to loading this graph type (both Fund & Distributoin period) : Example Eth - district 152
// The animation trigger "animationState" is attempting to animate the following not animatable properties: strokeDashoffset
// (to check the list of all animatable properties visit https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_animated_properties)


@Component({
    selector: 'app-graph-fund',
    templateUrl: './graph-fund.component.html',
    styleUrls: ['./graph-fund.component.css'],
    encapsulation: ViewEncapsulation.None,           // removes encapsulation alias from autobuild styles allowing easier access
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GraphFundComponent {

  // ViewChild used for these elements to provide for rapid element attribute changes without need for scanning DOM and readability.
  @ViewChild('GraphFund', { static: false }) graphFund: ElementRef;

  @Input() graph_type: string;

  graphDataStored: GraphData;

  multi: any[];
  view: any[];

  // options
  legend = true;
  showLabels = true;
  animations = true;
  showXAxisLabel = false;
  showYAxisLabel = true;
  xAxis = true;
  yAxis = true;

  showXAxis = true;
  showYAxis = true;
  gradient = true;
  showLegend = true;
  xAxisLabel: string;
  yAxisLabel: string;
  //yScaleMin: 100000;
  legendTitle: string;
  showDataLabel = true;
  timeline: false;
  yAxisTickFormatting: any = this.setYaxisLabel;

  //public yAxisTickFormattingFn = this.setPrecentLabel.bind(this);

  colorScheme = {
      domain: []
  };

  constructor( private elem: ElementRef, private cdf: ChangeDetectorRef) {
      //this.view = [ 500, 200 ]; // default sizing helps on page initial render.
      //this.loadGraph();
  }

  // Databound Inputs passed by Parent comp are only accessible from OnInit stage.
  public loadGraph(graphData: GraphData) {

      if (graphData == null) {
          graphData = graphDataFundTest;
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
    
      this.graphFund.nativeElement.classList.add('showTrans');

      this.cdf.detectChanges();     // Push detection

  }

  setYaxisLabel(val) {

      let graphPostAppend = ' MEGA';

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
