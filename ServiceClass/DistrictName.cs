namespace MetaverseMax.ServiceClass
{
    public class DistrictName
    {
        public int district_id { get; set; }
        public string district_name { get; set; }
        public int building_cnt { get; set; }
        public int claimed_cnt { get; set; }
        public bool poi_activated { get; set; }
        public bool poi_deactivated { get; set; }
    }
}
