using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class CitizenWeb
    {
        public int token_id { get; set; }
        public string name { get; set; }
        public string generation { get; set; }
        public int breeding { get; set; }
        public string sex { get; set; }
        public int trait_agility { get; set; }
        public int trait_intelligence { get; set; }
        public int trait_charisma { get; set; }
        public int trait_endurance { get; set; }
        public int trait_luck { get; set; }
        public int trait_strength { get; set; }
        public double trait_avg { get; set; }
        public int max_stamina { get; set; }
        public bool on_sale { get; set; }
        public double efficiency_industry { get; set; }
        public double efficiency_production { get; set; }
        public double efficiency_energy { get; set; }
        public double efficiency_office { get; set; }
        public double efficiency_commercial { get; set; }
        public double efficiency_municipal { get; set; }

        public int district_id { get; set; }
        public int pos_x { get; set; }
        public int pos_y { get; set; }
        public string building_img { get; set; }
        public string building_desc { get; set; }
        public int building_level { get; set; }
        public string building { get; set; }

    }
}
