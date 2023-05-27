
namespace MetaverseMax.ServiceClass
{
    public class OwnerAccount
    {
        public string matic_key { get; set; }
        public string checked_matic_key { get; set; }
        public string public_key { get; set; }
        public string name { get; set; }
        public int avatar_id { get; set; }
        public bool pro_tools_enabled { get; set; }
        public int pro_expiry_days { get; set; }
        public DateTime? slowdown_end { get; set; }
        public bool wallet_active_in_world { get; set; }
        public bool dark_mode { get; set; }
        public bool alert_activated { get; set; }
        public int alert_count { get; set; }
    }
}
