using MetaverseMax.ServiceClass;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MetaverseMax.ServiceClass
{
    public class OwnerData
    {
        public string owner_name { get; set; }
        public string owner_url { get; set; }
        public string owner_matic_key { get; set; }
        public string wallet_public { get; set; }
        public string last_action { get; set; }
        public string registered_date { get; set; }
        public string last_visit { get; set; }
        public int plot_count { get; set; }
        public int developed_plots { get; set; }
        public int plots_for_sale { get; set; }
        public int stamina_alert_count { get; set; }
        public int offer_count { get; set; }
        public int offer_sold_count { get; set; }
        public IEnumerable<Offer> owner_offer { get; set; }
        public IEnumerable<Offer> owner_offer_sold { get; set; }

        public IEnumerable<DistrictPlot> district_plots { get; set; }

        public IEnumerable<OwnerLand> owner_land { get; set; }
    }

    public class DistrictPlot
    {
        public int[] district { get; set; }
    }
}
