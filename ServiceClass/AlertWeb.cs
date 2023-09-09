using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MetaverseMax.ServiceClass
{
    public class AlertWeb
    {
        public IEnumerable<AlertItem> alert { get; set; }
        public int historyCount { get; set; }
    }

    public class AlertItem
    {
        public int alert_pending_key { get; set; }      
        public string last_updated { get; set; }
        public string alert_message { get; set; }
        public int alert_type { get; set; }
        public int alert_id { get; set; }
        public short icon_type { get; set; }
        public short icon_type_change { get; set; }
        public bool trigger_active { get; set; }

    }
}
