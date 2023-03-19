namespace MetaverseMax.ServiceClass
{
    public class OwnerLand
    {
        public int district_id { get; set; }
        public int pos_x { get; set; }
        public int pos_y { get; set; }
        public int building_type { get; set; }
        public string building_desc { get; set; }
        public string building_img { get; set; }
        public int building_level { get; set; }
        public int resource { get; set; }
        public string last_action { get; set; }
        public double last_actionUx { get; set; }
        public int plot_ip { get; set; }
        public int ip_info { get; set; }
        public int ip_bonus { get; set; }
        public int token_id { get; set; }
        public int citizen_count { get; set; }
        public string citizen_url { get; set; }
        public int citizen_stamina { get; set; }
        public bool citizen_stamina_alert { get; set; }
        public bool forsale { get; set; }
        public decimal forsale_price { get; set; }
        public bool alert { get; set; }
        public bool rented { get; set; }
        public decimal current_influence_rank { get; set; }
        public int condition { get; set; }
    }
}
