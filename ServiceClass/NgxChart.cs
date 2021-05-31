using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class NgxChart
    {
        public string x_axis_label { get; set; }
        public string y_axis_label { get; set; }
        public string legend_title { get; set; } 
        public IEnumerable<string> domain { get; set; }              // domain: ['#7AA3E5', '#A8385D', '#A27EA8']
        public IEnumerable<int> view { get; set; } 
        public bool show_legend { get; set; } 
        public bool show_yaxis_label { get; set; }
        public bool show_xaxis_label { get; set; }
        public string y_axis_postappend { get; set; }   // example " Trx"
        public IEnumerable<NGXGraphColumns> graphColumns { get; set; }        

    }
}
