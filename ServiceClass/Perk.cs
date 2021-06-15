using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class PerkList
    {
        public IEnumerable<Perk> perk { get; set; }
    }

    public class Perk
    {
        public int perk_id { get; set; }
        public string perk_name { get; set; }
        public string perk_desc { get; set; }
        public string level_Symbol { get; set; }
        public int[] level_values { get; set; }
        public int level_max { get; set; }
    }
}