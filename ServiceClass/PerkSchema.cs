
namespace MetaverseMax.ServiceClass
{
    public class PerkSchema
    {
        public static PerkList perkList = null;

        public PerkSchema()
        {
            if (perkList == null)
            {
                SetPerks();
            }
        }

        public PerkList SetPerks()
        {
            List<Perk> perks = new();

            perks.Add(new Perk()
            {
                perk_id = 1,
                perk_name = "Twins Residential",
                perk_desc = "Twins - a chance to receive extra Citizen token with the same avatar and qualifications in a Residential buildings",
                level_Symbol = "%",
                level_max = 3,
                level_values = new int[] { 2, 4, 7 }
            });

            perks.Add(new Perk()
            {
                perk_id = 2,
                perk_name = "Faster Production Industry",
                perk_desc = "Faster production - less time required to finish a production cycle",
                level_Symbol = "%",
                level_max = 3,
                level_values = new int[] { 5, 12, 25 }
            });

            perks.Add(new Perk()
            {
                perk_id = 3,
                perk_name = "Faster Production Production",
                perk_desc = "Faster production - less time required to finish a production cycle",
                level_Symbol = "%",
                level_max = 3,
                level_values = new int[] { 5, 12, 25 }
            });

            perks.Add(new Perk()
            {
                perk_id = 4,
                perk_name = "Faster Production Energy",
                perk_desc = "Faster production - less time required to finish a production cycle",
                level_Symbol = "%",
                level_max = 3,
                level_values = new int[] { 5, 12, 25 }
            });

            perks.Add(new Perk()
            {
                perk_id = 5,
                perk_name = "Double Collect Industrial",
                perk_desc = "Double collect - a chance to receive double of the amount a production cycle",
                level_Symbol = "%",
                level_max = 3,
                level_values = new int[] { 3, 5, 10 }
            });

            perks.Add(new Perk()
            {
                perk_id = 6,
                perk_name = "Double Collect Production",
                perk_desc = "Double collect - a chance to receive double of the amount a production cycle",
                level_Symbol = "%",
                level_max = 3,
                level_values = new int[] { 3, 5, 10 }
            });

            perks.Add(new Perk()
            {
                perk_id = 7,
                perk_name = "Double Collect Energy",
                perk_desc = "Double collect - a chance to receive double of the amount a production cycle",
                level_Symbol = "%",
                level_max = 3,
                level_values = new int[] { 3, 5, 10 }
            });

            perks.Add(new Perk()
            {
                perk_id = 8,
                perk_name = "Increase POI range Office",
                perk_desc = "Increase POI range - bigger territories covered by bonuses of all the POIs of a certain type in a District",
                level_Symbol = "+",
                level_max = 3,
                level_values = new int[] { 1, 2, 3 }
            });

            perks.Add(new Perk()
            {
                perk_id = 9,
                perk_name = "Increase POI range Municipal",
                perk_desc = "Increase POI range - bigger territories covered by bonuses of all the POIs of a certain type in a District",
                level_Symbol = "+",
                level_max = 3,
                level_values = new int[] { 1, 2, 3 }
            });

            perks.Add(new Perk()
            {
                perk_id = 10,
                perk_name = "Increase POI range Industry",
                perk_desc = "Increase POI range - bigger territories covered by bonuses of all the POIs of a certain type in a District",
                level_Symbol = "+",
                level_max = 3,
                level_values = new int[] { 1, 2, 3 }
            });

            perks.Add(new Perk()
            {
                perk_id = 11,
                perk_name = "Increase POI range Production",
                perk_desc = "Increase POI range - bigger territories covered by bonuses of all the POIs of a certain type in a District",
                level_Symbol = "+",
                level_max = 3,
                level_values = new int[] { 1, 2, 3 }
            });

            perks.Add(new Perk()
            {
                perk_id = 12,
                perk_name = "Increase POI range Commercial",
                perk_desc = "Increase POI range - bigger territories covered by bonuses of all the POIs of a certain type in a District",
                level_Symbol = "+",
                level_max = 3,
                level_values = new int[] { 1, 2, 3 }
            });

            perks.Add(new Perk()
            {
                perk_id = 13,
                perk_name = "Increase POI range Residential",
                perk_desc = "Increase POI range - bigger territories covered by bonuses of all the POIs of a certain type in a District",
                level_Symbol = "+",
                level_max = 3,
                level_values = new int[] { 1, 2, 3 }
            });

            perks.Add(new Perk()
            {
                perk_id = 14,
                perk_name = "Increase POI range Energy",
                perk_desc = "Increase POI range - bigger territories covered by bonuses of all the POIs of a certain type in a District",
                level_Symbol = "+",
                level_max = 3,
                level_values = new int[] { 1, 2, 3 }
            });

            perks.Add(new Perk()
            {
                perk_id = 15,
                perk_name = "All Buildings - Extra App Slot",
                perk_desc = "Extra Slots for Appliances in all buildings",
                level_Symbol = "+",
                level_max = 2,
                level_values = new int[] { 1, 2 }
            });

            perks.Add(new Perk()
            {
                perk_id = 16,
                perk_name = "Extra Stamina All Buildings",
                perk_desc = "Additional Stamina Points for appointed Citizens",
                level_Symbol = "%",
                level_max = 3,
                level_values = new int[] { 10, 20, 30 }
            });

            perkList = new PerkList()
            {
                perk = perks.ToArray()
            };

            return perkList;
        }

    }
}
