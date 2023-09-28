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
        public int action_type { get; set; }
        public double last_actionUx { get; set; }
        public string building_img { get; set; }
        public string building_name { get; set; }
        public int plot_count { get; set; }
        public int floor_count { get; set; }
        public int building_category_id { get; set; } 
        public int unit_forsale_count { get; set; }
        public decimal unit_price_low_mega { get; set; }
        public decimal unit_price_high_mega { get; set; }
        public decimal unit_price_low_coin { get; set; }
        public decimal unit_price_high_coin { get; set; }
        public decimal unit_sale_smallest_size { get; set; }
        public decimal unit_sale_largest_size { get; set; }

    }
}