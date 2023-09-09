namespace MetaverseMax.ServiceClass
{
    public class WorldParcelWeb
    {
        public int parcel_count { get; set; }
        public int building_count { get; set; }

        public IEnumerable<ParcelWeb>  parcel_list { get; set; }
    }

    public class ParcelWeb
{
        public int parcel_id { get; set; }
        public int pos_x { get; set; }
        public int pos_y { get; set; }
        public int district_id { get; set; }
        public int unit_count { get; set; }
        public string owner_matic { get; set; }
        public string owner_name { get; set; }
        public int owner_avatar_id { get; set; }
        public bool forsale { get; set; }
        public decimal forsale_price { get; set; }
        public string last_action { get; set; }
        public double last_actionUx { get; set; }
        public string building_img { get; set; }
        public string building_name { get; set; }
        public int plot_count { get; set; }
        public int building_category_id { get; set; } 

    }
}