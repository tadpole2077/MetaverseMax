interface GraphData {
  x_axis_label: string;
  y_axis_label: string;
  legend_title: string;
  domain: string[];
  view: number[];
  show_legend: boolean;
  show_xaxis_label: boolean;
  show_yaxis_label: boolean;
  y_axis_postappend: string;
  graphColumns: GraphColumn[];
}

interface GraphColumn {
  name: string;
  series: GraphSeries[]
}
interface GraphSeries {
  name: string | number;
  value: number;
}

export {
    GraphData
};
