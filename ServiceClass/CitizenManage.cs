using MetaverseMax.Database;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class CitizenManage
    {
        private readonly MetaverseMaxDbContext _context;

        public CitizenManage(MetaverseMaxDbContext _contextService)
        {
            _context = _contextService;
        
        }

        public CitizenManage()
        {           
        }

        // CITIZEN Business rules methods
        public string AssignDefaultOwnerImg(string ownerID)
        {
            if (ownerID.Equals("0"))
            {
                ownerID = "./images/MysteryOwner.png";
            }
            else
            {
                ownerID = "https://mcp3d.com/tron/api/image/citizen/" + ownerID;
            }

            return ownerID;
        }

        public int GetCitizenCount(JArray citizens)
        {
            int count = 0;
            if (citizens != null)
            {
                count = citizens.Count;
            }
            return count;
        }

        public bool CheckCitizenStamina(JArray citizens, int buildingType)
        {
            int count = 0;
            if (citizens != null)
            {
                count = buildingType switch
                {
                    (int)BUILDING_TYPE.RESIDENTIAL =>
                        citizens.Where(
                            row => (row.Value<int?>("stamina") ?? 0) <= 200
                            ).Count(),

                    (int)BUILDING_TYPE.OFFICE =>
                        citizens.Where(
                            row => (row.Value<int?>("stamina") ?? 0) <= 50
                            ).Count(),

                    (int)BUILDING_TYPE.COMMERCIAL =>
                        citizens.Where(
                        row => (row.Value<int?>("stamina") ?? 0) <= 10
                        ).Count(),

                    (int)BUILDING_TYPE.MUNICIPAL =>
                        citizens.Where(
                        row => (row.Value<int?>("stamina") ?? 0) <= 20
                        ).Count(),

                    (int)BUILDING_TYPE.INDUSTRIAL =>
                        citizens.Where(
                        row => (row.Value<int?>("stamina") ?? 0) <= 25
                        ).Count(),

                    (int)BUILDING_TYPE.PRODUCTION =>
                         citizens.Where(
                         row => (row.Value<int?>("stamina") ?? 0) <= 100
                         ).Count(),

                    (int)BUILDING_TYPE.ENERGY =>
                        citizens.Where(
                        row => (row.Value<int?>("stamina") ?? 0) <= 30
                        ).Count(),

                    _ => citizens.Where(
                       row => (row.Value<int?>("stamina") ?? 0) <= 100
                       ).Count(),
                };
            }

            return count > 0;
        }

        public string GetCitizenUrl(JArray citizens)
        {
            string citizenUrl = string.Empty;
            if (citizens != null && citizens.Count >0)
            {
                //*** Find minimum stamina of all cits (in building), then find image of first one (possible that more then 1 with same min stamina value)
                int minStamina = GetLowStamina(citizens);
                JToken minStaminaCitizen = citizens.Where(row => (row.Value<int?>("stamina") ?? 0) == minStamina).First();
                
                citizenUrl = string.Concat("https://mcp3d.com/tron/api/image/citizen/", minStaminaCitizen.Value<int?>("id") ?? 0 );
            }
            return citizenUrl;
        }

        public int GetLowStamina(JArray citizens)
        {
            int minStamina =0;
            if (citizens != null && citizens.Count > 0)
            {
                //*** Find minimum stamina of all cits (in building)
                minStamina = citizens.Min(row => row.Value<int?>("stamina") ?? 0);
            }
            return minStamina;
        }


        public IEnumerable<CitizenWeb> GetCitizen(string ownerMatic)
        {
            OwnerCitizenDB ownerCitizenDB = new(_context);
            List<OwnerCitizenExt> citizenList = new();
            List<CitizenWeb> citizenWebList = new();

            try
            {
                Building building = new();
                citizenList = ownerCitizenDB.GetCitizen(ownerMatic);

                foreach (OwnerCitizenExt cit in citizenList)
                {
                    citizenWebList.Add(new()
                    {
                        token_id = cit.token_id,
                        name = cit.name,
                        generation = cit.generation switch
                        {
                            1 => "A",
                            2 => "B",
                            3 => "C",
                            4 => "D",
                            5 => "E",
                            6 => "F",
                            7 => "G",
                            _ => "NA"
                        },
                        breeding = cit.breeding,
                        sex = (cit.sex ?? 1) == 1 ? "M" : "F",
                        on_sale = cit.on_sale,
                        max_stamina = cit.max_stamina,
                        trait_agility = cit.trait_agility,
                        trait_charisma = cit.trait_charisma,
                        trait_endurance = cit.trait_endurance,
                        trait_intelligence = cit.trait_intelligence,
                        trait_luck = cit.trait_luck,
                        trait_strength = cit.trait_strength,
                        trait_avg = Math.Round((cit.trait_agility + cit.trait_charisma + cit.trait_endurance + cit.trait_intelligence + cit.trait_luck + cit.trait_strength) / 6.0, 2),

                        efficiency_production = Math.Round(cit.efficiency_production, 2),
                        efficiency_commercial = Math.Round(cit.efficiency_commercial, 2),
                        efficiency_energy = Math.Round(cit.efficiency_energy, 2),
                        efficiency_industry = Math.Round(cit.efficiency_industry, 2),
                        efficiency_municipal = Math.Round(cit.efficiency_municipal, 2),
                        efficiency_office = Math.Round(cit.efficiency_office, 2),

                        building_img = building.BuildingImg(cit.building_type_id ?? 0, cit.building_id ?? 0, cit.building_level ?? 0),
                        building_desc = building.BuildingType(cit.building_type_id ?? 0, cit.building_id ?? 0),
                        district_id = cit.district_id ?? 0,
                        pos_x = cit.pos_x ?? 0,
                        pos_y = cit.pos_y ?? 0,
                        building_level = cit.building_level ?? 0,
                        building = string.Concat(cit.district_id.ToString()," - X:", cit.pos_x, " Y:", cit.pos_y)

                    });
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerMange.GetCitizen() : Error on WS calls for owner matic : ", ownerMatic));
                    _context.LogEvent(log);
                }
            }

            return citizenWebList;
        }

        // Get from MCP 3rd tier services
        public int GetCitizenMCP(string ownerMatic)
        {
            String content = string.Empty;
            OwnerCitizen ownerCitizen;
            OwnerCitizenDB ownerCitizenDB = new(_context);
            Citizen citizen;
            CitizenDB citizenDB = new(_context);
            int returnCode = 0;

            try
            {
                // POST from Land/Get REST WS
                byte[] byteArray = Encoding.ASCII.GetBytes("{\"address\": \"" + ownerMatic + "\"}");
                WebRequest request = WebRequest.Create("https://ws-tron.mcp3d.com/user/assets/citizens");
                request.Method = "POST";
                request.ContentType = "application/json";

                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                // Ensure correct dispose of WebRespose IDisposable class even if exception
                using (WebResponse response = request.GetResponse())
                {
                    StreamReader reader = new(response.GetResponseStream());
                    content = reader.ReadToEnd();
                }

                if (content.Length > 0)
                {
                    //JObject jsonContent = JObject.Parse(content);                    
                    JArray citizens = JArray.Parse(content);

                    // Defensive coding, remove any links already added with matching date = today
                    ownerCitizenDB.RemoveOwnerLink(ownerMatic);

                    // Expire any db cits not found within current owner cit collection.
                    ownerCitizenDB.Expire(ownerMatic, citizens);
                    _context.SaveChanges();

                    // Add 1 or 2 records per citizen owned, if citizen already exists then skip creating a new one, just create the link.
                    for (int index = 0; index < citizens.Count; index++)
                    {
                        citizen = new();
                        citizen.token_id = citizens[index].Value<int?>("id") ?? 0;
                        //citizen.matic_key = citizens[index].Value<string>("address");         // commented out as this is not the matic key of the citizen but the owner.
                        citizen.name = citizens[index].Value<string>("name");
                        citizen.name = citizen.name.Substring(0, citizen.name.Length > 100 ? 100 : citizen.name.Length);
                        citizen.generation = citizens[index].Value<int?>("generation") ?? 1;
                        citizen.breeding = citizens[index].Value<int?>("breedings") ?? 0;
                        citizen.sex = citizens[index].Value<short>("gender");

                        citizen.trait_agility = citizens[index].Value<int?>("agility") ?? 0;
                        citizen.trait_charisma = citizens[index].Value<int?>("charisma") ?? 0;
                        citizen.trait_endurance = citizens[index].Value<int?>("endurance") ?? 0;
                        citizen.trait_intelligence = citizens[index].Value<int?>("intelligence") ?? 0;
                        citizen.trait_luck = citizens[index].Value<int?>("luck") ?? 0;
                        citizen.trait_strength = citizens[index].Value<int?>("strength") ?? 0;

                        citizen.on_sale = citizens[index].Value<string>("on_sale") == "0" ? false : true;
                        citizen.max_stamina = citizens[index].Value<int?>("max_stamina") ?? 0;
                        citizen.create_date = DateTime.Now;

                        citizen.efficiency_industry = GetIndustryEfficiency(citizen);
                        citizen.efficiency_production = GetProductionEfficiency(citizen);
                        citizen.efficiency_office = GetOfficeEfficiency(citizen);
                        citizen.efficiency_commercial = GetCommercialEfficiency(citizen);
                        citizen.efficiency_municipal = GetMunicipalEfficiency(citizen);
                        citizen.efficiency_energy = GetEnergyEfficiency(citizen);

                        citizenDB.AddorUpdate(citizen, false);

                        ownerCitizen = new();
                        ownerCitizen.citizen_token_id = citizen.token_id;
                        ownerCitizen.land_token_id = citizens[index].Value<int?>("land_id") ?? 0;
                        ownerCitizen.pet_token_id = citizens[index].Value<int?>("pet_id") ?? 0;
                        ownerCitizen.owner_matic_key = ownerMatic;
                        ownerCitizen.link_date = DateTime.Now;

                        ownerCitizenDB.AddorUpdate(ownerCitizen, false);
                    }

                    _context.SaveChanges();         // Perf: Only Save records after all Cits and Links created on local
                    returnCode = citizens.Count;
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerMange.GetCitizenMCP() : Error on WS calls for owner matic : ", ownerMatic));
                    _context.LogEvent(log);
                }
            }

            return returnCode;
        }

        // IND	S - Trait #1	0.6	E - Trait #2	0.25	Trait #3(x4)	0.15
        private double GetIndustryEfficiency(Citizen citizen)
        {
            double efficiency = (double)((citizen.trait_strength * .6)
                + (citizen.trait_endurance * .25)
                + ((citizen.trait_agility + citizen.trait_charisma + citizen.trait_intelligence + citizen.trait_luck) / 4.0 * .15));

            return efficiency * 10;
        }

        // Prod	A - Trait #1	0.5	S - Trait #2	0.35	Trait #3(x4)	0.15
        private double GetProductionEfficiency(Citizen citizen)
        {
            double efficiency = (double)((citizen.trait_agility * .5)
                + (citizen.trait_strength * .35)
                + ((citizen.trait_endurance + citizen.trait_charisma + citizen.trait_intelligence + citizen.trait_luck) / 4.0 * .15));

            return efficiency * 10;
        }

        // OFF I - Trait #1	0.5	C - Trait #2	0.35	Trait #3(x4)	0.15
        private double GetOfficeEfficiency(Citizen citizen)
        {
            double efficiency = (double)((citizen.trait_intelligence * .5)
                + (citizen.trait_charisma * .35)
                + ((citizen.trait_agility + citizen.trait_endurance + citizen.trait_strength + citizen.trait_luck) / 4.0 * .15));

            return efficiency * 10;
        }

        // Comm	C - Trait #1	0.5	L - Trait #2	0.35	Trait #3(x4)	0.15
        private double GetCommercialEfficiency(Citizen citizen)
        {

            double efficiency = (double)((citizen.trait_charisma * .5)
                + (citizen.trait_luck * .35)
                + ((citizen.trait_agility + citizen.trait_intelligence + citizen.trait_endurance + citizen.trait_strength) / 4.0 * .15));

            return efficiency * 10;
        }

        // Municipal	L - Trait #1	0.5	I - Trait #2	0.35	Trait #3(x4)	0.15
        private double GetMunicipalEfficiency(Citizen citizen)
        {
            double efficiency = (double)((citizen.trait_luck * .5)
                + (citizen.trait_intelligence * .35)
                + ((citizen.trait_agility + citizen.trait_charisma + citizen.trait_endurance + citizen.trait_strength) / 4 * .15));

            return efficiency * 10;
        }

        // Energy	E - Trait #1	0.55	A - Trait #2	0.33	Trait #3(x4)	0.12 (from notssogood)
        private double GetEnergyEfficiency(Citizen citizen)
        {
            double efficiency = (double)((citizen.trait_endurance * .55)
                + (citizen.trait_agility * .33)
                + ((citizen.trait_intelligence + citizen.trait_charisma + citizen.trait_luck + citizen.trait_strength) / 4 * .12));

            return efficiency * 10;
        }
    }
}
