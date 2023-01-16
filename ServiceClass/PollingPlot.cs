using MetaverseMax.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class PollingPlot
    {
        public string status { get; set; }

        public Plot last_plot_updated { get; set; }
    }

    public class WorldNameCollection
    {
        public IEnumerable<WorldName> world_name { get; set; }
    }

    public class WorldName
    {
        public int id { get; set; }
        public string name { get; set; }
    }
}
