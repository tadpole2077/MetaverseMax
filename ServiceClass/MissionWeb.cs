namespace MetaverseMax.ServiceClass
{
    public class WorldMissionWeb
    {        
        public IEnumerable<MissionWeb> mission_list { get; set; }

        public int mission_count { get; set; }
        public decimal mission_reward { get; set; }
        public int all_mission_count { get; set; }
        public int all_mission_available_count { get; set; }
        public decimal all_mission_reward { get; set; }
        public decimal all_mission_available_reward { get; set; }
        public decimal repeatable_daily_reward { get; set; }

    }

    public class MissionWeb
    {
        public int token_id { get; set; }
        public int pos_x { get; set; }
        public int pos_y { get; set; }
        public int district_id { get; set; }       
        public string owner_matic { get; set; }
        public string owner_name { get; set; }
        public int owner_avatar_id { get; set; }
        public int building_id { get; set; }
        public int building_level { get; set; }
        public int building_type_id { get; set; }
        public string building_img { get; set; }

        public int completed { get; set; }
        public int max { get; set; }
        public decimal reward { get; set; }
        public decimal reward_owner { get; set; }
        public bool available { get; set; }
        public decimal balance { get; set; }
        public string last_updated { get; set; }        
        public double last_updatedUx { get; set; }
        public int last_refresh { get; set; }

        public int c_d { get; set; }            // collect days
        public int c_h { get; set; }            // collect hours
        public bool c_r { get; set; }           // collect ready

    }
}