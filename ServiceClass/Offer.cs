namespace MetaverseMax.ServiceClass
{
    public class Offer
    {
        public string buyer_matic_key { get; set; }

        public string buyer_owner_name { get; set; }

        public int buyer_avatar_id { get; set; }

        public decimal buyer_offer { get; set; }

        public string offer_date { get; set; }

        public int token_id { get; set; }

        public int token_type_id { get; set; }

        public string token_type { get; set; }

        public int token_district { get; set; }

        public int token_pos_x { get; set; }

        public int token_pos_y { get; set; }

        public bool sold { get; set; }

        public string sold_date { get; set; }

    }
}
