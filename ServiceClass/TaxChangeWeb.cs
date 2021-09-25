using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class TaxChangeWeb
    {
        public int district_id { get; set; }

        public string change_date { get; set; }

        public int tax { get; set; }

        public string tax_type { get; set; }

        public string change_desc { get; set; }

        public string change_owner { get; set; }

        public int change_value { get; set; }

    }
}
