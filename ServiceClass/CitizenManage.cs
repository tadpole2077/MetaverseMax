using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json.Linq;
using System.Text;
using MetaverseMax.Database;
using MetaverseMax.BaseClass;

namespace MetaverseMax.ServiceClass
{
    public class CitizenManage : ServiceBase
    {
        private Dictionary<int, List<CitizenAction>> petActionsList = new();
        private ServiceCommon common = new();
        private OwnerCitizenDB ownerCitizenDB;

        public CitizenManage(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext, worldTypeSelected)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;

            ownerCitizenDB = new(_context);
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
                ownerID = worldType switch
                {
                    WORLD_TYPE.ETH => "https://mcp3d.com/api/image/citizen/",
                    WORLD_TYPE.BNB => "https://mcp3d.com/bnb/api/image/citizen/",
                    WORLD_TYPE.TRON or _ => "https://mcp3d.com/tron/api/image/citizen/"
                }
                     + ownerID;
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

        // Min 15 stamina Energy
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

                    // Industry stanima must be >25 to run.
                    (int)BUILDING_TYPE.INDUSTRIAL =>
                        citizens.Where(
                        row => (row.Value<int?>("stamina") ?? 0) <= 30
                        ).Count(),

                    (int)BUILDING_TYPE.PRODUCTION =>
                         citizens.Where(
                         row => (row.Value<int?>("stamina") ?? 0) <= 100
                         ).Count(),

                    (int)BUILDING_TYPE.ENERGY =>
                        citizens.Where(
                        row => (row.Value<int?>("stamina") ?? 0) < 30
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
            if (citizens != null && citizens.Count > 0)
            {
                //*** Find minimum stamina of all cits (in building), then find image of first one (possible that more then 1 with same min stamina value)
                int minStamina = GetLowStamina(citizens);
                JToken minStaminaCitizen = citizens.Where(row => (row.Value<int?>("stamina") ?? 0) == minStamina).First();


                citizenUrl = string.Concat(
                    worldType switch
                    {
                        WORLD_TYPE.ETH => "https://mcp3d.com/api/image/citizen/",
                        WORLD_TYPE.BNB => "https://mcp3d.com/bnb/api/image/citizen/",
                        WORLD_TYPE.TRON or _ => "https://mcp3d.com/tron/api/image/citizen/"
                    }
                    ,
                    minStaminaCitizen.Value<int?>("id") ?? 0);
            }
            return citizenUrl;
        }

        public int GetLowStamina(JArray citizens)
        {
            int minStamina = 0;
            if (citizens != null && citizens.Count > 0)
            {
                //*** Find minimum stamina of all cits (in building)
                minStamina = citizens.Min(row => row.Value<int?>("stamina") ?? 0);
            }
            return minStamina;
        }

        public IEnumerable<CitizenWeb> GetCitizenHistory(int tokenId, long productionDate)
        {
            OwnerCitizenExtDB ownerCitizenExtDB = new(_context);
            List<CitizenWeb> citizenWebList = new();
            DateTime runDate = new();
            try
            {
                runDate = new DateTime(productionDate);
                citizenWebList = PopulateCitizenList(ownerCitizenExtDB.GetBuildingCitizenHistory(tokenId, runDate.AddSeconds(-(int)CITIZEN_HISTORY.CORRECTION_SECONDS)));
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("CitizenManage.GetCitizenHistory() : Error on WS calls for asset : ", tokenId));
                    _context.LogEvent(log);
                }
            }

            return citizenWebList;
        }

        public CitizenWebCollection GetCitizen(string ownerMatic, string requester)
        {
            OwnerCitizenExtDB ownerCitizenExtDB = new(_context);
            CitizenWebCollection citizenCollection = new();
            OwnerManage ownerManage = new(_context, worldType);

            try
            {
                List<OwnerCitizenExt> ownerCitizens = ownerCitizenExtDB.GetCitizen(ownerMatic);
                citizenCollection.citizen = PopulateCitizenList(ownerCitizens).ToArray();

                citizenCollection.last_updated = common.LocalTimeFormatStandardFromUTC(string.Empty, ownerCitizens.Max(x => x.refreshed_last));

                citizenCollection.slowdown = ownerManage.GetSlowDown(requester);
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("CitizenManage.GetCitizen() : Error on WS calls for owner matic : ", ownerMatic));
            }

            return citizenCollection;
        }        

        private List<CitizenWeb> PopulateCitizenList(List<OwnerCitizenExt> citizenList)
        {
            Building building = new();
            List<CitizenWeb> citizenWebList = new();

            for (int rowIndex = 0; rowIndex < citizenList.Count; rowIndex++)
            {
                OwnerCitizenExt cit = citizenList[rowIndex];

                cit.trait_agility_pet_bonus = GetPetBonus(cit.pet_token_id, cit.pet_bonus_id, cit.pet_bonus_level, PET_BONUS_TYPE.AGILITY);
                cit.trait_charisma_pet_bonus = GetPetBonus(cit.pet_token_id, cit.pet_bonus_id, cit.pet_bonus_level, PET_BONUS_TYPE.CHARISMA);
                cit.trait_endurance_pet_bonus = GetPetBonus(cit.pet_token_id, cit.pet_bonus_id, cit.pet_bonus_level, PET_BONUS_TYPE.ENDURANCE);
                cit.trait_intelligence_pet_bonus = GetPetBonus(cit.pet_token_id, cit.pet_bonus_id, cit.pet_bonus_level, PET_BONUS_TYPE.INTEL);
                cit.trait_luck_pet_bonus = GetPetBonus(cit.pet_token_id, cit.pet_bonus_id, cit.pet_bonus_level, PET_BONUS_TYPE.LUCK);
                cit.trait_strength_pet_bonus = GetPetBonus(cit.pet_token_id, cit.pet_bonus_id, cit.pet_bonus_level, PET_BONUS_TYPE.STRENGTH);

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
                    current_price = cit.current_price ?? 0,
                    max_stamina = cit.max_stamina,

                    trait_agility = cit.trait_agility,
                    trait_agility_pet = cit.trait_agility_pet_bonus,

                    trait_charisma = cit.trait_charisma,
                    trait_charisma_pet = cit.trait_charisma_pet_bonus,

                    trait_endurance = cit.trait_endurance,
                    trait_endurance_pet = cit.trait_endurance_pet_bonus,

                    trait_intelligence = cit.trait_intelligence,
                    trait_intelligence_pet = cit.trait_intelligence_pet_bonus,

                    trait_luck = cit.trait_luck,
                    trait_luck_pet = cit.trait_luck_pet_bonus,

                    trait_strength = cit.trait_strength,
                    trait_strength_pet = cit.trait_strength_pet_bonus,

                    trait_avg = Math.Round((cit.trait_agility + cit.trait_charisma + cit.trait_endurance + cit.trait_intelligence + cit.trait_luck + cit.trait_strength) / 6.0, 2),
                    trait_avg_pet = Math.Round((cit.trait_agility + cit.trait_charisma + cit.trait_endurance + cit.trait_intelligence + cit.trait_luck + cit.trait_strength +
                        cit.trait_agility_pet_bonus + cit.trait_charisma_pet_bonus + cit.trait_endurance_pet_bonus + cit.trait_intelligence_pet_bonus + cit.trait_luck_pet_bonus + cit.trait_strength_pet_bonus) / 6.0, 2),

                    building_img = building.GetBuildingImg((BUILDING_TYPE)(cit.building_type_id ?? 0), cit.building_id ?? 0, cit.building_level ?? 0, worldType),
                    building_desc = building.BuildingType(cit.building_type_id ?? 0, cit.building_id ?? 0),
                    district_id = cit.district_id ?? 0,
                    pos_x = cit.pos_x ?? 0,
                    pos_y = cit.pos_y ?? 0,
                    building_level = cit.building_level ?? 0,
                    building = string.Concat(cit.district_id.ToString(), " - X:", cit.pos_x, " Y:", cit.pos_y),

                    pet_token_id = cit.pet_token_id,

                    // Need to find efficiency % as it was at the time of this production run. The stored efficiency rates are using latest state/current assigned pet.
                    efficiency_production = Math.Round(GetProductionEfficiency(cit), 2, MidpointRounding.AwayFromZero),
                    efficiency_commercial = Math.Round(GetCommercialEfficiency(cit), 2, MidpointRounding.AwayFromZero),
                    efficiency_energy_water = Math.Round(GetEnergyWaterEfficiency(cit), 2, MidpointRounding.AwayFromZero),
                    efficiency_energy_electric = Math.Round(GetEnergyElectricEfficiency(cit), 2, MidpointRounding.AwayFromZero),
                    efficiency_industry = Math.Round(GetIndustryEfficiency(cit), 2, MidpointRounding.AwayFromZero),
                    efficiency_municipal = Math.Round(GetMunicipalEfficiency(cit), 2, MidpointRounding.AwayFromZero),
                    efficiency_office = Math.Round(GetOfficeEfficiency(cit), 2, MidpointRounding.AwayFromZero)
                });
            }

            return citizenWebList;
        }

        // Get from MCP 3rd tier services
        public async Task<RETURN_CODE> GetOwnerCitizenCollectionMCP(string ownerMatic)
        {
            string content = string.Empty;

            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            int retryCount = 0, changeCount = 0;
            CitizenChange citizenChange;
            serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.ASSETS_CITIZENS, WORLD_TYPE.BNB => BNB_WS.ASSETS_CITIZENS, WORLD_TYPE.ETH => ETH_WS.ASSETS_CITIZENS, _ => TRON_WS.ASSETS_CITIZENS };

            while (returnCode == RETURN_CODE.ERROR && retryCount < 3)
            {
                try
                {
                    retryCount++;

                    // POST REST WS
                    HttpResponseMessage response;
                    using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                    {
                        StringContent stringContent = new StringContent("{\"address\": \"" + ownerMatic + "\"}", Encoding.UTF8, "application/json");

                        response = await client.PostAsync(
                            serviceUrl,
                            stringContent);

                        response.EnsureSuccessStatusCode(); // throws if not 200-299
                        content = await response.Content.ReadAsStringAsync();

                    }
                    // End timer
                    watch.Stop();
                    servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, ownerMatic);

                    if (content.Length > 0)
                    {
                        //JObject jsonContent = JObject.Parse(content);                    
                        JArray citizens = JArray.Parse(content);

                        // Defensive coding, remove all OwnerCitizen links already added with matching link_date = today for this account
                        //ownerCitizenDB.RemoveOwnerLink(ownerMatic);

                        // Expire any db cits not found within current owner cit collection.  Citizens sold/transfered are handled here - citizens that are reassigned to another building handled later.
                        Expire(ownerMatic, citizens);

                        // Add 1+ records per citizen owned, if citizen already exists then skip creating a new one, just create the link record(s) if change (pet, owner, dates, land) found.
                        for (int index = 0; index < citizens.Count; index++)
                        {
                            citizenChange = UpdateCitizen(citizens[index], ownerMatic, null);
                            if (citizenChange.updateFound == true)
                            {
                                changeCount++;
                            }
                        }

                        if (changeCount > 0)
                        {
                            _context.SaveChanges();
                        }

                        returnCode = RETURN_CODE.SUCCESS;
                    }
                }
                catch (Exception ex)
                {
                    DBLogger dBLogger = new(_context.worldTypeSelected);
                    dBLogger.logException(ex, String.Concat("CitizenManage.GetOwnerCitizenCollectionMCP() : Error on WS calls for owner matic : ", ownerMatic));
                }
            }

            if (retryCount > 1 && returnCode == RETURN_CODE.SUCCESS)
            {
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("CitizenManage.GetOwnerCitizenCollectionMCP() : retry successful - no ", retryCount));
                }
            }

            return returnCode;
        }

        // Get from MCP 3rd tier services
        public async Task<RETURN_CODE> GetCitizenMCP(int tokenId, bool saveToDB, DateTime checkHistoryFrom)
        {
            string content = string.Empty;

            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            int retryCount = 0;
            CitizenChange citizenChange;
            serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.CITIZEN_GET, WORLD_TYPE.BNB => BNB_WS.CITIZEN_GET, WORLD_TYPE.ETH => ETH_WS.CITIZEN_GET, _ => TRON_WS.CITIZEN_GET };

            while (returnCode == RETURN_CODE.ERROR && retryCount < 3)
            {
                try
                {
                    retryCount++;

                    // POST REST WS
                    HttpResponseMessage response;
                    using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                    {
                        StringContent stringContent = new StringContent("{\"id\": \"" + tokenId.ToString() + "\"}", Encoding.UTF8, "application/json");

                        response = await client.PostAsync(
                            serviceUrl,
                            stringContent);

                        response.EnsureSuccessStatusCode(); // throws if not 200-299
                        content = await response.Content.ReadAsStringAsync();

                    }
                    // End timer
                    watch.Stop();
                    servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, tokenId.ToString());

                    if (content.Length > 0)
                    {
                        JObject citizen = JObject.Parse(content);

                        // Add 1+ records per citizen owned, if citizen already exists then skip creating a new one, just create the link record(s) if change (pet, owner, dates, land) found.
                        citizenChange = UpdateCitizen(citizen, citizen.Value<string>("address").ToLower() ?? "", checkHistoryFrom);

                        if (saveToDB)
                        {
                            _context.SaveChanges();
                        }

                        returnCode = RETURN_CODE.SUCCESS;
                    }
                }
                catch (Exception ex)
                {
                    DBLogger dBLogger = new(_context.worldTypeSelected);
                    dBLogger.logException(ex, String.Concat("CitizenManage.GetCitizenMCP() : Error on WS calls for owner matic : ", tokenId));
                }
            }

            if (retryCount > 1 && returnCode == RETURN_CODE.SUCCESS)
            {
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("CitizenManage.GetCitizenMCP() : retry successful - no ", retryCount));
                }
            }

            return returnCode;
        }

        // Expire existing OwnerCitizen links where citizen is not found within passed array (newly retrieved from MCP WS)
        public int Expire(string ownerMatic, JArray citizens)
        {
            bool match = false;
            List<OwnerCitizen> dbCitizenList = ownerCitizenDB.GetOwnerCitizenByOwnerMatic(ownerMatic, null);
            int count = 0;

            // find matching db cit in owners cit collection, if no match found then expire the db cit link 
            for (int index = 0; index < dbCitizenList.Count; index++)
            {
                match = false;
                // Find match of Citizen in local database linked to this account.
                for (int index2 = 0; index2 < citizens.Count; index2++)
                {
                    if (dbCitizenList[index].citizen_token_id == (citizens[index2].Value<int?>("id") ?? 0))
                    {
                        match = true;
                        break;
                    }
                }

                // Not Found = Citizen sold/transfered to another owner
                if (match == false)
                {
                    // Find the Citizen History events since the last refresh event (this maybe 1 day or more if last sync failed, or less if updates occured from IP ranking features
                    CitizenUpdateEvents(dbCitizenList[index].link_key, (DateTime)dbCitizenList[index].refreshed_last, ownerMatic);
                    count++;
                    //dbCitizenList[index].valid_to_date = DateTime.Now;          // Might need to review this - as cit transfer/sale would have occured any time between last sync run and now.
                }
            }

            // Save changes if any Citizens found transferred or sold. Needed for later sync type jobs
            if (count > 0)
            {
                _context.SaveChanges();
            }

            return count;
        }

        // Scenario : Cit pet assigned(missing), then moved to new building(incorrect link date when assigned), then building produce run,  Cit pet removed,  nightly sync. Local DB shows cit assigned to correct building with no pet.
        //            No pet included in produce run calc, prediction mismatch.   With this function fills in missing citizen action gaps important for use in produce prediction and production history - cits
        //            Avoid adding dup CitizenOwner records (same pet, land, owner)
        // Key:  This missing records need to slot into the history timeline, retaining the latest (already added) with correct link and valid_to dates.
        public int CitizenUpdateEvents(int targetCitizenLinkKey, DateTime eventDate, string ownerMatic)
        {
            OwnerCitizenDB ownerCitizenDB = new(_context);
            DateTime? startAction, endAction = null;
            int citizenTokenId;

            // Note this (target)OwnerCitizen may not be currently active, occuring in the part, and cit may have moved to another building or unassigned since then.
            OwnerCitizen targetOwnerCitizen = _context.ownerCitizen.Where(x => x.link_key == targetCitizenLinkKey).FirstOrDefault();  // Need to get active OwnerCitizen  to update valid_to_date or split it.
            citizenTokenId = targetOwnerCitizen.citizen_token_id;

            // Get all Actions  from 24hrs before the target production run to NOW (eventDate) - some of these actions may already be in db so check to avoid dups.
            List<CitizenAction> citizenAction = GetCitizenHistoryMCP(citizenTokenId, eventDate.AddDays(-1), false).Result;

            if (citizenAction.Count > 0)
            {
                // Change existing OwnerCitizen link record, to support insertion of new actions(link records),  depending on new actions - may require a new end link record.
                // This existing record is typically split into 2 parts,  one positioned before, and 2nd (new one) after the inserted new actions.
                // Potentially the 2nd new one will not be required if has matching properties to last action.
                startAction = citizenAction[0].action_datetime;
                endAction = citizenAction[citizenAction.Count - 1].action_datetime;

                // CHECK if existing OwnerCitizen record occured prior to first found action. if it is then deactivate link (set valid_to_date)
                if (CompareDateTime(targetOwnerCitizen.link_date, startAction) < 0)
                {
                    targetOwnerCitizen.valid_to_date = startAction;      // Set vali_to_date last citizen action in local db to next oldest history action start date
                }

                ProcessCitizenActions(citizenAction, citizenTokenId, targetOwnerCitizen, ownerMatic);
            }

            return citizenAction.Count;
        }

        public int CompareDateTime(DateTime? date1, DateTime? date2)
        {
            int compare = -1;
            long tickDiff;

            tickDiff = ((DateTime)date1).Ticks - ((DateTime)date2).Ticks;
            if (tickDiff < 30000 && tickDiff > -30000) // + or - 3 milisecond range is good.
            {
                compare = 0;
            }
            else if (tickDiff > 30000)
            {
                compare = 1;
            }

            return compare;
        }

        public CitizenChange UpdateCitizen(JToken citizenMCP, string ownerMatic, DateTime? checkHistoryFrom)
        {
            OwnerCitizen ownerCitizenCurrent;
            int petId, petBonusId, petBonusLevel;
            JArray ArrayTraits;
            Citizen citizen = new();
            List<int> traits = new() { 0, 0, 0, 0, 0, 0 };
            CitizenDB citizenDB = new(_context);
            CitizenChange citizenChange = new() { updateFound = false };
            List<CitizenAction> citizenAction = null;
            int storedOnSaleKey = 0;
            decimal storedSalePrice = 0;
            DateTime? startingFrom;
            bool skipPriceCheck = false;

            try
            {
                citizen.token_id = citizenMCP.Value<int?>("id") ?? 0;
                Citizen storedCitizen = citizenDB.GetCitizen(citizen.token_id);
                if (storedCitizen != null)
                {
                    storedOnSaleKey = storedCitizen.on_sale_key;
                    storedSalePrice = storedCitizen.current_price ?? 0;
                }

                //citizen.matic_key = citizenMCP.Value<string>("address");         // commented out as this is not the matic key of the citizen but the owner.
                citizen.name = citizenMCP.Value<string>("name");
                citizen.name = citizen.name.Substring(0, citizen.name.Length > 100 ? 100 : citizen.name.Length);
                citizen.generation = citizenMCP.Value<int?>("generation") ?? 1;
                citizen.breeding = citizenMCP.Value<int?>("breedings") ?? 0;
                citizen.sex = citizenMCP.Value<short>("gender");
                citizen.stamina = citizenMCP.Value<int?>("stamina") ?? 0;

                ArrayTraits = citizenMCP.Value<JArray>("special");
                if (ArrayTraits.Any())
                {
                    traits = ArrayTraits.Select(trait => trait.Value<int?>() ?? 0).ToList();
                }

                petId = citizenMCP.Value<int?>("pet_id") ?? 0;
                petBonusId = citizenMCP.Value<int?>("pet_bonus_id") ?? 0;
                petBonusLevel = citizenMCP.Value<int?>("pet_bonus_level") ?? 0;

                citizen.trait_agility = traits[(int)TRAIT_INDEX.AGILITY];
                citizen.trait_agility_pet_bonus = GetPetBonus(petId, petBonusId, petBonusLevel, PET_BONUS_TYPE.AGILITY);
                //citizen.trait_agility_pet_bonus = citizenMCP.Value<int?>("agility") ?? 0 - citizen.trait_agility;
                citizen.trait_charisma = traits[(int)TRAIT_INDEX.CHARISMA];
                citizen.trait_charisma_pet_bonus = GetPetBonus(petId, petBonusId, petBonusLevel, PET_BONUS_TYPE.CHARISMA);
                //citizen.trait_charisma_pet_bonus = citizenMCP.Value<int?>("charisma") ?? 0 - citizen.trait_charisma;
                citizen.trait_endurance = traits[(int)TRAIT_INDEX.ENDURANCE];
                citizen.trait_endurance_pet_bonus = GetPetBonus(petId, petBonusId, petBonusLevel, PET_BONUS_TYPE.ENDURANCE);
                //citizen.trait_endurance_pet_bonus = citizenMCP.Value<int?>("endurance") ?? 0 - citizen.trait_endurance;
                citizen.trait_intelligence = traits[(int)TRAIT_INDEX.INTELLIGENCE];
                citizen.trait_intelligence_pet_bonus = GetPetBonus(petId, petBonusId, petBonusLevel, PET_BONUS_TYPE.INTEL);
                //citizen.trait_intelligence_pet_bonus = citizenMCP.Value<int?>("intelligence") ?? 0 - citizen.trait_intelligence;
                citizen.trait_luck = traits[(int)TRAIT_INDEX.LUCK];
                citizen.trait_luck_pet_bonus = GetPetBonus(petId, petBonusId, petBonusLevel, PET_BONUS_TYPE.LUCK);
                //citizen.trait_luck_pet_bonus = citizenMCP.Value<int?>("luck") ?? 0 - citizen.trait_luck;
                citizen.trait_strength = traits[(int)TRAIT_INDEX.STRENGTH];
                citizen.trait_strength_pet_bonus = GetPetBonus(petId, petBonusId, petBonusLevel, PET_BONUS_TYPE.STRENGTH);
                //citizen.trait_strength_pet_bonus = citizenMCP.Value<int?>("strength") ?? 0 - citizen.trait_strength;

                // WS Citizen/Get does not provide on_sale or any price data, but is fast targeted WS.  WS 'user/assets/citizen' does provide on_sale data.
                if (citizenMCP.Value<string>("on_sale") == null)
                {
                    skipPriceCheck = true;
                    citizen.on_sale = false;            // default - skipped on any existing stored Cit
                    citizen.current_price = 0;          // default - skipped on any existing stored Cit

                }
                else
                {
                    citizen.on_sale = citizenMCP.Value<string>("on_sale") == "0" ? false : true;
                    citizen.on_sale_key = citizen.on_sale == false ? 0 : int.Parse(citizenMCP.Value<string>("on_sale").Substring(1));
                    citizen.current_price = Task.Run(() => CheckSalePrice(storedOnSaleKey, storedSalePrice, citizen.on_sale, citizen.token_id, citizen.on_sale_key)).Result;
                }
                citizen.max_stamina = citizenMCP.Value<int?>("max_stamina") ?? 0;

                citizen.efficiency_industry = GetIndustryEfficiency(citizen);
                citizen.efficiency_production = GetProductionEfficiency(citizen);
                citizen.efficiency_office = GetOfficeEfficiency(citizen);
                citizen.efficiency_commercial = GetCommercialEfficiency(citizen);
                citizen.efficiency_municipal = GetMunicipalEfficiency(citizen);
                citizen.efficiency_energy_water = GetEnergyWaterEfficiency(citizen);
                citizen.efficiency_energy_electric = GetEnergyElectricEfficiency(citizen);
                citizen.refresh_history = false;

                citizenChange = citizenDB.AddorUpdate(citizen, storedCitizen, false, skipPriceCheck);

                // Remove existing OwnerCitizen records and refresh if problem flag recorded on last attempt.
                if (citizenChange.historyRefresh == true)
                {
                    ownerCitizenDB.DeleteHistory(citizen.token_id, true);
                }


                // OwnerCitizen Record creation - Handles land, pet and Owner changes.
                ownerCitizenCurrent = new();
                ownerCitizenCurrent.citizen_token_id = citizen.token_id;
                ownerCitizenCurrent.land_token_id = citizenMCP.Value<int?>("land_id") ?? 0;
                ownerCitizenCurrent.pet_token_id = citizenMCP.Value<int?>("pet_id") ?? 0;
                ownerCitizenCurrent.owner_matic_key = ownerMatic;
                ownerCitizenCurrent.link_date = DateTime.UtcNow;      // DEFAULT date - IMPROVE - will limit cit assigned to production runs if set to current dt.
                ownerCitizenCurrent.refreshed_last = DateTime.UtcNow;


                // Scenarios:
                // A) Citizen created - new to db
                // b) Citizen existing - transfered to new owner
                // c) Citizen reassigned to new building
                // d) Citizen pet change
                // e) Citizen combo - reassigned to new building + pet change
                // find exact date & time of event. Needed for production run eval.
                OwnerCitizen ownerCitizenExisting = ownerCitizenDB.GetExistingOwnerCitizen(citizen.token_id);

                // CHECK if Citizen - changed Pet or land since last sync - PROBLEM - may have changed and changed back since last sync - like temp pet assign for a prd run.
                // Timing: this call typically takes 1 to 2 seconds, hence it cant be done each nighly sync on ALL citizens only on subset selected cits causing incorrect production predictions due to temp pet usage.
                // Smart Update:
                //   1) Only check history if Citizen Pet or land has changed since last sync ( all actions since active OwnerCitizen.refreshed_last )
                //   2) Only check history if account has production runs during prior day that do not match prediction(indicating possible pet use)
                if (ownerCitizenExisting == null ||
                        ownerCitizenCurrent.land_token_id != ownerCitizenExisting.land_token_id ||
                        ownerCitizenCurrent.pet_token_id != ownerCitizenExisting.pet_token_id ||
                        ownerCitizenCurrent.owner_matic_key != ownerCitizenExisting.owner_matic_key ||
                        checkHistoryFrom != null)
                {
                    // Find date to start Citizen.HistoryCheck From, can explicitly stated by passed datetime, or derived from existing stored record
                    if (checkHistoryFrom != null)
                    {
                        startingFrom = ownerCitizenExisting == null || checkHistoryFrom < ownerCitizenExisting.refreshed_last ? checkHistoryFrom : ownerCitizenExisting.refreshed_last; // take the older date - possible nighly data sync down 
                    }
                    else
                    {
                        startingFrom = ownerCitizenExisting != null ? ownerCitizenExisting.refreshed_last : DateTime.Now.AddDays((int)CITIZEN_HISTORY.DAYS);
                    }

                    // Get all Citizen action events since last refresh (sync), OR if no existing review Cit history from -40 days ago and populate.
                    citizenAction = GetCitizenHistoryMCP(
                        ownerCitizenCurrent.citizen_token_id,
                        startingFrom,
                        ownerCitizenExisting == null).Result;

                    // At least 1x Action should be found for existing Citizen (found in local store) - causing change of stored data(pet, owner, building)
                    if (citizenAction.Count > 0)
                    {
                        OwnerCitizen ownerCitizenActive = ProcessCitizenActions(citizenAction, citizen.token_id, ownerCitizenExisting, ownerMatic);         // With No SaveChanges()
                        if (ownerCitizenActive != null && ownerCitizenActive.db_update_pending == true)
                        {
                            citizenChange.updateFound = true;       // Record that DB changes are pending on local store.
                        }

                        // CORNER CASE - PET unpaired not recorded within Citizen History due to transfer before unpair.   POTENTIAL MISSING CASE - No Pet Transfer/Unpair action recorded in PET History
                        if (ownerCitizenCurrent.pet_token_id == 0 && ownerCitizenActive != null && ownerCitizenActive.pet_token_id != 0)
                        {
                            // FIND when Pet was unpaired and record missing action.
                            CitizenAction petAction = FindPetAction(ownerCitizenActive);
                            if (petAction != null)
                            {
                                ownerCitizenActive = ProcessCitizenActions(new List<CitizenAction> { petAction }, citizen.token_id, ownerCitizenActive, ownerMatic);
                                if (ownerCitizenActive.db_update_pending == true)
                                {
                                    citizenChange.updateFound = true;       // Record that DB changes are pending on local store.
                                }
                            }
                        }

                    }
                    // Defensive Coding - Check if no actions in history add a default OwnerCitizen, Check if last action does not match the currect active-stored citizen land/pet/owner
                    // CORNER CASE: Dont add default record (new cit) if a prior history record found - can cause a dup record
                    else if (citizenAction.Count == 0)
                    {
                        ownerCitizenDB.AddorUpdate(ownerCitizenCurrent, false);
                        citizenChange.updateFound = true;       // Record that DB changes are pending on local store.
                    }

                }
                else
                {
                    // NO Action to process, but Citizen was checked so updated refresh datetime
                    ownerCitizenExisting.refreshed_last = DateTime.UtcNow;
                }

            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("CitizenManage.UpdateCitizen() : Error on Updating Citizen - ", citizen.token_id));
            }

            return citizenChange;
        }

        public CitizenAction FindPetAction(OwnerCitizen ownerCitizenActive)
        {
            CitizenAction matchingAction = null;
            try
            {
                if (!petActionsList.ContainsKey(ownerCitizenActive.pet_token_id))
                {
                    petActionsList.Add(ownerCitizenActive.pet_token_id, GetPetHistory(ownerCitizenActive).Result);
                }

                List<CitizenAction> petActions = petActionsList[ownerCitizenActive.pet_token_id];
                matchingAction = petActions.Where(x =>
                    x.owner_matic_key == ownerCitizenActive.owner_matic_key &&
                    x.action_type == (int)ACTION_TYPE.PET_NEW_OWNER &&
                    x.action_datetime > ownerCitizenActive.link_date)
                    .OrderBy(x => x.action_datetime).FirstOrDefault();

            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("CitizenManage.FindPetAction() : Error retrieving Pet Action Record with token_id : ", ownerCitizenActive.pet_token_id, " and Citizen token_id :", ownerCitizenActive.citizen_token_id));
            }

            return matchingAction;
        }

        public async Task<List<CitizenAction>> GetPetHistory(OwnerCitizen ownerCitizenActive)
        {
            List<CitizenAction> citizenAction = new();
            string content = string.Empty;

            try
            {
                serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.ASSETS_HISTORY, WORLD_TYPE.BNB => BNB_WS.ASSETS_HISTORY, WORLD_TYPE.ETH => ETH_WS.ASSETS_HISTORY, _ => TRON_WS.ASSETS_HISTORY };
                // POST REST WS
                HttpResponseMessage response;
                using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                {
                    StringContent stringContent = new StringContent("{\"token_id\": \"" + ownerCitizenActive.pet_token_id + "\", \"token_type\": " + (int)HISTORY_TYPE.PET + "}", Encoding.UTF8, "application/json");

                    response = await client.PostAsync(
                        serviceUrl,
                        stringContent);

                    response.EnsureSuccessStatusCode(); // throws if not 200-299
                    content = await response.Content.ReadAsStringAsync();

                }
                // End timer
                watch.Stop();
                servicePerfDB.AddServiceEntry("PET - " + serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, ownerCitizenActive.pet_token_id.ToString());

                if (content.Length > 0)
                {
                    //JObject jsonContent = JObject.Parse(content);                    
                    JArray historyList = JArray.Parse(content);
                    for (int index = 0; index < historyList.Count; index++)
                    {
                        //Check any citizen change action has occured since check
                        DateTime actionDateTime = DateTime.SpecifyKind(historyList[index].Value<DateTime>("event_time"), DateTimeKind.Utc);

                        // Only Record actions from the 3 weeks - should be sufficient to cover most missing transfers
                        if (actionDateTime < DateTime.Now.AddDays(-21))
                        {
                            break;
                        }

                        string historyType = historyList[index].Value<string>("type");

                        // Add or Remove Pet - History.token_id = pet token id,  history.token_type = pet type.
                        if (historyType.StartsWith("set/pet"))
                        {
                            citizenAction.Add(new CitizenAction()
                            {
                                action_datetime = actionDateTime,
                                action_type = (int)ACTION_TYPE.ASSIGN_PET,
                                citizen_token_id = historyList[index].Value<int?>("sub_token_id") ?? 0,
                                pet_token_id = ownerCitizenActive.pet_token_id,
                                owner_matic_key = historyList[index].Value<string>("to_address").ToLower()
                            });
                        }
                        else if (historyType.StartsWith("remove/pet"))
                        {
                            citizenAction.Add(new CitizenAction()
                            {
                                action_datetime = actionDateTime,
                                action_type = (int)ACTION_TYPE.REMOVE_PET,
                                citizen_token_id = historyList[index].Value<int?>("sub_token_id") ?? 0,
                                pet_token_id = ownerCitizenActive.pet_token_id,
                                owner_matic_key = historyList[index].Value<string>("to_address").ToLower()
                            });
                        }
                        else if (historyType.StartsWith("erc721/pet/transfer"))
                        {
                            // Corner Case - Hander for - If transfer to a 3rd party occured before pet is unpaired from prior owners cit.
                            JToken historyData = historyList[index].Value<JToken>("data");
                            if (!historyData.Value<string>("to").ToLower().Equals(historyData.Value<string>("from").ToLower()))
                            {
                                citizenAction.Add(new CitizenAction()
                                {
                                    action_datetime = actionDateTime,
                                    action_type = (int)ACTION_TYPE.PET_NEW_OWNER,
                                    citizen_token_id = historyList[index].Value<int?>("sub_token_id") ?? 0,
                                    pet_token_id = ownerCitizenActive.pet_token_id,
                                    owner_matic_key = historyData.Value<string>("from").ToLower(),
                                    new_owner_key = historyData.Value<string>("to").ToLower()
                                });
                            }
                        }


                    }
                }
                citizenAction = citizenAction.OrderBy(x => x.action_datetime).ToList();
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("CitizenManage.GetPetHistory() : Error on WS calls for pet token_id : ", ownerCitizenActive.pet_token_id));
            }

            return citizenAction;
        }

        public OwnerCitizen ProcessCitizenActions(List<CitizenAction> citizenAction, int citizenTokenID, OwnerCitizen ownerCitizenExisting, string ownerMatic)
        {
            OwnerCitizen ownerCitizenAction;
            //EntityEntry<OwnerCitizen> ownerCitizenActionLast = ownerCitizenExisting != null ? _context.Entry(ownerCitizenExisting) : null;
            OwnerCitizen ownerCitizenActionDBPrior = null, ownerCitizenActionDBNext = null;
            EntityEntry<OwnerCitizen> ownerCitizenActionLAST = null;
            bool updatePending = false;

            List<OwnerCitizen> storedActions = _context.ownerCitizen.Where(oc => oc.citizen_token_id == citizenTokenID)
                .OrderByDescending(oc => oc.link_date)
                .ToList();


            // Actions are ordered oldest to latest, process oldest first.
            // Each action is handled fully - meaning checks against db-store and action list, slotting the action into the record - adjusting existing action record dates to fit it in.
            for (int actionCounter = 0; actionCounter < citizenAction.Count; actionCounter++)
            {
                ownerCitizenAction = new();

                ownerCitizenAction.citizen_token_id = citizenTokenID;
                ownerCitizenAction.link_date = citizenAction[actionCounter].action_datetime ?? DateTime.UtcNow;


                // FROM LOCAL DB - Get next older action prior to current action being processed                
                ownerCitizenActionDBPrior = storedActions.Where(oc => oc.link_date <= ownerCitizenAction.link_date)
                    .OrderByDescending(oc => oc.link_date)
                    .FirstOrDefault();

                // FROM LOCAL DB - Get next newer action prior to current action being processed                
                ownerCitizenActionDBNext = storedActions.Where(oc => oc.link_date > ownerCitizenAction.link_date)
                    .OrderBy(oc => oc.link_date)
                    .FirstOrDefault();

                // SKIP - This action was previously recorded, as having identical link_date  (which is not derived date like valid_to_date)
                if (ownerCitizenActionDBPrior != null && CompareDateTime(ownerCitizenActionDBPrior.link_date, ownerCitizenAction.link_date) == 0)
                {
                    continue;
                }


                // Assign New Action Data
                ownerCitizenAction.land_token_id = citizenAction[actionCounter].action_type switch
                {
                    (int)ACTION_TYPE.NEW_OWNER => 0,        // Citizen Transfer, reset land to new owner
                    (int)ACTION_TYPE.REMOVE_CITIZEN => 0,
                    (int)ACTION_TYPE.ASSIGN_CITIZEN => citizenAction[actionCounter].land_token_id,
                    _ => ownerCitizenActionDBPrior != null ? ownerCitizenActionDBPrior.land_token_id : 0
                };

                ownerCitizenAction.pet_token_id = citizenAction[actionCounter].action_type switch
                {
                    (int)ACTION_TYPE.NEW_OWNER => 0,        // Citizen Transfer, reset Pet to none
                    (int)ACTION_TYPE.REMOVE_PET => 0,
                    (int)ACTION_TYPE.PET_NEW_OWNER => 0,
                    (int)ACTION_TYPE.ASSIGN_PET => citizenAction[actionCounter].pet_token_id,
                    _ => ownerCitizenActionDBPrior != null ? ownerCitizenActionDBPrior.pet_token_id : 0
                };

                ownerCitizenAction.owner_matic_key = citizenAction[actionCounter].action_type switch
                {
                    (int)ACTION_TYPE.NEW_OWNER => citizenAction[actionCounter].new_owner_key,
                    _ => ownerCitizenActionDBPrior != null ? ownerCitizenActionDBPrior.owner_matic_key : ownerMatic
                };

                ownerCitizenAction.refreshed_last = DateTime.UtcNow;

                // World transfer action cleanup - set pet and land to 0
                if (ownerCitizenAction.owner_matic_key == "WORLD_TRANSFER")
                {
                    ownerCitizenAction.pet_token_id = 0;
                    ownerCitizenAction.land_token_id = 0;
                }

                // CHECK - Only add if different then existing Record :
                //    if LAST recorded action record matches data in current history action, MCP sometimes logs dup events - such as when transfering cit.
                if (ownerCitizenActionDBPrior == null ||
                    ownerCitizenAction.land_token_id != ownerCitizenActionDBPrior.land_token_id ||
                    ownerCitizenAction.pet_token_id != ownerCitizenActionDBPrior.pet_token_id ||
                    ownerCitizenAction.owner_matic_key != ownerCitizenActionDBPrior.owner_matic_key)
                {
                    // Mark prior record has expired but retain for use in Production history eval. exact date time used when cit was reassigned.
                    if (ownerCitizenActionDBPrior != null)
                    {
                        EntityEntry<OwnerCitizen> changePrior = _context.Entry(ownerCitizenActionDBPrior);
                        changePrior.Entity.valid_to_date = ownerCitizenAction.link_date;
                        changePrior.Entity.refreshed_last = DateTime.UtcNow;
                    }
                    if (ownerCitizenActionDBNext != null)
                    {
                        // Use next (known) oldest action to find valid_to_date on current action..
                        // NOTE this date may change on next iteration - such as another newly identified action not in db, occuring before db recorded action.
                        ownerCitizenAction.valid_to_date = ownerCitizenActionDBNext.link_date;
                    }

                    // Add new record - dont commit to db(PERF)
                    // Ocassionally, an existing ownerCitizen will get though due to C# milisecond skip, AddByLinkDateTime will catch it and wont add a dup.
                    ownerCitizenActionLAST = ownerCitizenDB.AddByLinkDateTime(ownerCitizenAction, false);

                    // Add the new record to the storedActions list - for use in next iternation.
                    if (ownerCitizenActionLAST != null)
                    {
                        storedActions.Add(ownerCitizenActionLAST.Entity);
                    }

                    updatePending = true;
                }
            }

            if (ownerCitizenActionLAST != null)
            {
                ownerCitizenActionLAST.Entity.db_update_pending = updatePending;
            }

            return ownerCitizenActionLAST != null ? ownerCitizenActionLAST.Entity : null;
        }


        // Used to identify and backfill any action actions related to last Production run or new cits
        public async Task<List<CitizenAction>> GetCitizenHistoryMCP(int tokenId, DateTime? startingDateTime, bool newCitizen)
        {
            List<CitizenAction> citizenAction = new();
            string content = string.Empty;

            try
            {
                serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.ASSETS_HISTORY, WORLD_TYPE.BNB => BNB_WS.ASSETS_HISTORY, WORLD_TYPE.ETH => ETH_WS.ASSETS_HISTORY, _ => TRON_WS.ASSETS_HISTORY };

                // POST REST WS
                HttpResponseMessage response;
                using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                {
                    StringContent stringContent = new StringContent("{\"token_id\": \"" + tokenId + "\", \"token_type\": " + (int)HISTORY_TYPE.CITIZEN + "}", Encoding.UTF8, "application/json");

                    response = await client.PostAsync(
                        serviceUrl,
                        stringContent);

                    response.EnsureSuccessStatusCode(); // throws if not 200-299
                    content = await response.Content.ReadAsStringAsync();

                }
                // End timer
                watch.Stop();
                servicePerfDB.AddServiceEntry("Citizen - " + serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, tokenId.ToString());

                if (content.Length > 0)
                {
                    //JObject jsonContent = JObject.Parse(content);                    
                    JArray historyList = JArray.Parse(content);
                    for (int index = 0; index < historyList.Count; index++)
                    {
                        //Check any citizen change action has occured since check
                        DateTime actionDateTime = DateTime.SpecifyKind(historyList[index].Value<DateTime>("event_time"), DateTimeKind.Utc);

                        //IF action occured before CheckTime then break out of loop, dont get next action, dont record this action. 
                        if (startingDateTime != null && startingDateTime > actionDateTime && newCitizen == false)
                        {
                            break;
                        }


                        string historyType = historyList[index].Value<string>("type");

                        // Add or Remove Pet - History.token_id = pet token id,  history.token_type = pet type.
                        if (historyType.StartsWith("management/set_citizen"))
                        {
                            newCitizen = false;            // For new cits - Min need to find when the citizen was assigned to the current building, regardless of when action occured.
                            citizenAction.Add(new CitizenAction()
                            {
                                action_datetime = actionDateTime,
                                action_type = (int)ACTION_TYPE.ASSIGN_CITIZEN,
                                land_token_id = historyList[index].Value<int?>("token_id") ?? 0
                            });
                        }
                        else if (historyType.StartsWith("management/remove_citizen"))
                        {
                            citizenAction.Add(new CitizenAction()
                            {
                                action_datetime = actionDateTime,
                                action_type = (int)ACTION_TYPE.REMOVE_CITIZEN,
                                land_token_id = historyList[index].Value<int?>("token_id") ?? 0
                            });
                        }
                        else if (historyType.StartsWith("set/pet"))
                        {
                            citizenAction.Add(new CitizenAction()
                            {
                                action_datetime = actionDateTime,
                                action_type = (int)ACTION_TYPE.ASSIGN_PET,
                                pet_token_id = historyList[index].Value<int?>("token_id") ?? 0
                            });
                        }
                        else if (historyType.StartsWith("remove/pet"))
                        {
                            citizenAction.Add(new CitizenAction()
                            {
                                action_datetime = actionDateTime,
                                action_type = (int)ACTION_TYPE.REMOVE_PET,
                                pet_token_id = historyList[index].Value<int?>("token_id") ?? 0

                            });
                        }
                        else if (historyType.StartsWith("erc721/citizen/transfer"))
                        {
                            // cant use the 'to_address' due to MCP bug - it can incorrectly contain the seller's(from) matic key. Correct form/to addresses found in {data.xyz}
                            JToken historyData = historyList[index].Value<JToken>("data");

                            citizenAction.Add(new CitizenAction()
                            {
                                action_datetime = actionDateTime,
                                action_type = (int)ACTION_TYPE.NEW_OWNER,
                                owner_matic_key = historyData.Value<string>("from").ToLower(),
                                new_owner_key = historyData.Value<string>("to").ToLower()
                            });
                        }
                        else if (historyType.StartsWith("airport/citizen/from"))
                        {
                            // cant use the 'to_address' due to MCP bug - it can incorrectly contain the seller's(from) matic key. Correct form/to addresses found in {data.xyz}
                            JToken historyData = historyList[index].Value<JToken>("data");

                            citizenAction.Add(new CitizenAction()
                            {
                                action_datetime = actionDateTime,
                                action_type = (int)ACTION_TYPE.NEW_OWNER,
                                owner_matic_key = historyData.Value<string>("from").ToLower(),
                                new_owner_key = "WORLD_TRANSFER",
                                pet_token_id = 0,
                                land_token_id = 0
                            });
                        }

                    }
                }
                citizenAction = citizenAction.OrderBy(x => x.action_datetime).ToList();
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("CitizenManage.GetCitizenHistoryMCP() : Error on WS calls for citizen token_id : ", tokenId));

                // Add flag to refresh history (40 days prior) on next full sync - this clear the prior 40 day history and recreates it.
                CitizenDB citizenDB = new(_context);
                citizenDB.UpdateRefreshHistory(tokenId, true, false);
            }

            return citizenAction;
        }

        public int GetPetBonus(int petTokenId, int? petBonusId, int? petBonusLevel, PET_BONUS_TYPE matchingType)
        {
            int petTraitBonus = 0;

            if (petTokenId > 0 && petBonusId == (int)matchingType)
            {
                petTraitBonus = petBonusLevel ?? 0;
            }

            return petTraitBonus;
        }

        public OwnerPet GetPortfolioPets(string ownerMaticKey)
        {
            OwnerPet ownerPet = new();
            PetDB petDB = new(_context);
            List<Pet> petList;
            List<PetWeb> petWeb = new();

            try
            {
                petList = petDB.GetOwnerPet(ownerMaticKey);

                ownerPet.pet_count = petList.Count;
                ownerPet.last_updated = common.LocalTimeFormatStandardFromUTC(string.Empty, _context.ActionTimeGet(ACTION_TYPE.PET));

                foreach (Pet pet in petList)
                {
                    petWeb.Add(new PetWeb
                    {
                        token_id = pet.token_id,
                        level = pet.bonus_level,
                        trait = pet.bonus_id switch
                        {
                            (int)PET_BONUS_TYPE.AGILITY => "Agility",
                            (int)PET_BONUS_TYPE.CHARISMA => "Charisma",
                            (int)PET_BONUS_TYPE.ENDURANCE => "Endurance",
                            (int)PET_BONUS_TYPE.INTEL => "Intel",
                            (int)PET_BONUS_TYPE.LUCK => "Luck",
                            (int)PET_BONUS_TYPE.STRENGTH => "Strength",
                            _ => "Unknown"
                        },
                        name = pet.pet_look switch
                        {
                            1 => "Bulldog",
                            2 => "Corgi Dog",
                            3 => "Labrador",
                            4 => "Mastiff Dog",
                            5 => "Whippet Dog",
                            6 => "Parrot",
                            12 => "Lion",
                            15 => "Red Dragon",
                            16 => "Chameleon",
                            254 => "Beetle",
                            _ => "Unknown"
                        }
                    });
                }

                ownerPet.pet = petWeb;
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("OwnerMange.GetPet() : Error on WS calls for owner matic : ", ownerMaticKey));
            }

            return ownerPet;
        }

        public async Task<RETURN_CODE> GetPetAllMCP()
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            OwnerManage ownerManage = new(_context, worldType);

            // Iterate all distinct OwnerMatic keys found in local db - update pets on all owners
            foreach (KeyValuePair<string, OwnerAccount> owner in ownerManage.GetOwners(true))
            {
                await GetPetMCP(owner.Key);
            }

            return returnCode;
        }

        public async Task<RETURN_CODE> GetPetMCP(string ownerMaticKey)
        {
            PetDB petDB = new(_context);
            OwnerDB ownerDB = new(_context);
            String content = string.Empty;
            List<Pet> petList = new();
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            int retryCount = 0;

            while (returnCode == RETURN_CODE.ERROR && retryCount < 3)
            {
                try
                {
                    retryCount++;
                    serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.ASSETS_PETS, WORLD_TYPE.BNB => BNB_WS.ASSETS_PETS, WORLD_TYPE.ETH => ETH_WS.ASSETS_PETS, _ => TRON_WS.ASSETS_PETS };

                    // POST from Land/Get REST WS
                    HttpResponseMessage response;
                    using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                    {
                        StringContent stringContent = new StringContent("{\"address\": \"" + ownerMaticKey + "\",\"filter\": {\"qualifications\":0}}", Encoding.UTF8, "application/json");

                        response = await client.PostAsync(
                            serviceUrl,
                            stringContent);

                        response.EnsureSuccessStatusCode(); // throws if not 200-299
                        content = await response.Content.ReadAsStringAsync();

                    }
                    // End timer
                    watch.Stop();
                    servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, ownerMaticKey);

                    if (content.Length != 0)
                    {
                        JArray pets = JArray.Parse(content);

                        for (int index = 0; index < pets.Count; index++)
                        {
                            // Found dups within MCP response (9 dups found on 13/02/2022_Tron reported to MCP) - filtering out dups
                            if (!petList.Exists(x => x.token_id == (pets[index].Value<int?>("pet_id") ?? 0)))
                            {
                                petList.Add(new Pet()
                                {
                                    token_owner_matic_key = ownerMaticKey,
                                    token_id = pets[index].Value<int?>("pet_id") ?? 0,
                                    bonus_id = pets[index].Value<int?>("bonus_id") ?? 0,
                                    bonus_level = pets[index].Value<int?>("bonus_level") ?? 0,
                                    pet_look = pets[index].Value<int?>("look") ?? 0,
                                    last_update = DateTime.Now
                                });
                            }
                        }


                        petDB.AddorUpdate(petList, ownerMaticKey);
                    }

                    returnCode = RETURN_CODE.SUCCESS;
                }
                catch (Exception ex)
                {
                    DBLogger dBLogger = new(_context.worldTypeSelected);
                    dBLogger.logException(ex, String.Concat("OwnerMange.GetPetMCP() : Error on WS calls for owner matic : ", ownerMaticKey));
                }
            }

            if (retryCount > 1 && returnCode == RETURN_CODE.SUCCESS)
            {
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerMange.GetPetMCP() : retry successful - no ", retryCount));
                }
            }

            return returnCode;
        }

        public PetUsage GetPetUsage(DateTime? runDate, List<OwnerCitizenExt> citizens)
        {
            // Find citizen set used within run date period, then find all Pets in use during that run.  Note using -30 sec on source date due to Issue with unbinding pets before run complete
            List<OwnerCitizenExt> filterCitizens = citizens.Where(x => (x.valid_to_date >= runDate || x.valid_to_date is null)
                && x.link_date < ((DateTime)runDate).AddSeconds((int)CITIZEN_HISTORY.CORRECTION_SECONDS)).ToList();

            PetUsage petUsage = new();

            for (int count = 0; count < filterCitizens.Count; count++)
            {
                if (filterCitizens[count].pet_bonus_id != null)
                {
                    petUsage.agility += (int)filterCitizens[count].trait_agility_pet_bonus;
                    petUsage.charisma += (int)filterCitizens[count].trait_charisma_pet_bonus;
                    petUsage.endurance += (int)filterCitizens[count].trait_endurance_pet_bonus;
                    petUsage.intelligence += (int)filterCitizens[count].trait_intelligence_pet_bonus;
                    petUsage.luck += (int)filterCitizens[count].trait_luck_pet_bonus;
                    petUsage.strength += (int)filterCitizens[count].trait_strength_pet_bonus;
                }
            }

            return petUsage;
        }

        private async Task<decimal> CheckSalePrice(int storedOnSaleKey, decimal storedSalePrice, bool onSale, int tokenId, int onSaleKey)
        {
            string content = string.Empty;
            decimal salePrice = 0;
            decimal salePriceLarge = 0;

            try
            {
                // CHECK if MCP version is onSale, then check if stored matches (a) on sale (b) price  -  the OnSaleKey is a reference identifier number that indicates if item is on sale but does not reflect the current price.
                if (onSale == true)
                {
                    if (onSaleKey == storedOnSaleKey)
                    {
                        return storedSalePrice;
                    }
                    else
                    {
                        serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.SALES_INFO, WORLD_TYPE.BNB => BNB_WS.SALES_INFO, WORLD_TYPE.ETH => ETH_WS.SALES_INFO, _ => TRON_WS.SALES_INFO };

                        // POST REST WS
                        HttpResponseMessage response;
                        using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                        {
                            StringContent stringContent = new StringContent("{\"token_id\": \"" + tokenId + "\", \"token_type\": 4 }", Encoding.UTF8, "application/json");

                            response = await client.PostAsync(
                                serviceUrl,
                                stringContent);

                            response.EnsureSuccessStatusCode(); // throws if not 200-299
                            content = await response.Content.ReadAsStringAsync();
                        }

                        watch.Stop();
                        servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, tokenId.ToString());

                        if (content.Length > 0)
                        {
                            JObject jsonContent = JObject.Parse(content);
                            JToken saleData = jsonContent.Value<JToken>("sale_data");

                            if (saleData != null && saleData.HasValues && (saleData.Value<bool?>("active") ?? false))
                            {
                                salePriceLarge = saleData.Value<Decimal?>("sellPrice") ?? 0;

                                salePrice = worldType switch
                                {
                                    WORLD_TYPE.TRON => salePriceLarge / 1000000,                   // 6 places back
                                    WORLD_TYPE.BNB => salePriceLarge / 1000000000000000000,        // 18 places back
                                    WORLD_TYPE.ETH => salePriceLarge / 1000000000000000000,        // 18 places back
                                    _ => salePrice / 1000000
                                };
                            }

                        }
                        Task.Run(async () => { await WaitPeriodAction(100); }).Wait();         //Wait set period required reduce load on MCP services - min 100ms
                    }
                }
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("CitizenManage.CheckSalePrice() : Error on WS calls for token_id : ", tokenId));
            }

            return salePrice;
        }


        // IND	S - Trait #1 @ 1/2  	E - Trait #2 @ 1/3	Trait #3(rest x4)	@ 1/6  = 100%
        private double GetIndustryEfficiency(CitizenTrait citizen)
        {
            // Combine trait and pet(if assigned), checking for max of 10 - pet bonus may cause > max trait
            int trait_strength = citizen.trait_strength + citizen.trait_strength_pet_bonus > 10 ? 10 : citizen.trait_strength + citizen.trait_strength_pet_bonus;
            int trait_endurance = citizen.trait_endurance + citizen.trait_endurance_pet_bonus > 10 ? 10 : citizen.trait_endurance + citizen.trait_endurance_pet_bonus;
            int trait_agility = citizen.trait_agility + citizen.trait_agility_pet_bonus > 10 ? 10 : citizen.trait_agility + citizen.trait_agility_pet_bonus;
            int trait_charisma = citizen.trait_charisma + citizen.trait_charisma_pet_bonus > 10 ? 10 : citizen.trait_charisma + citizen.trait_charisma_pet_bonus;
            int trait_intelligence = citizen.trait_intelligence + citizen.trait_intelligence_pet_bonus > 10 ? 10 : citizen.trait_intelligence + citizen.trait_intelligence_pet_bonus;
            int trait_luck = citizen.trait_luck + citizen.trait_luck_pet_bonus > 10 ? 10 : citizen.trait_luck + citizen.trait_luck_pet_bonus;

            double efficiency = (double)(
                (trait_strength / 2.0)
                + (trait_endurance / 3.0)
                + ((trait_agility + trait_charisma + trait_intelligence + trait_luck) / 24.0)
                );

            return efficiency * 10;
        }

        // Prod	A - Trait #1 @ 1/2  	S - Trait #2 @ 1/3	Trait #3(rest x4)	@ 1/6  = 100%
        private double GetProductionEfficiency(CitizenTrait citizen)
        {
            // Combine trait and pet(if assigned), checking for max of 10 - pet bonus may cause > max trait
            int trait_strength = citizen.trait_strength + citizen.trait_strength_pet_bonus > 10 ? 10 : citizen.trait_strength + citizen.trait_strength_pet_bonus;
            int trait_endurance = citizen.trait_endurance + citizen.trait_endurance_pet_bonus > 10 ? 10 : citizen.trait_endurance + citizen.trait_endurance_pet_bonus;
            int trait_agility = citizen.trait_agility + citizen.trait_agility_pet_bonus > 10 ? 10 : citizen.trait_agility + citizen.trait_agility_pet_bonus;
            int trait_charisma = citizen.trait_charisma + citizen.trait_charisma_pet_bonus > 10 ? 10 : citizen.trait_charisma + citizen.trait_charisma_pet_bonus;
            int trait_intelligence = citizen.trait_intelligence + citizen.trait_intelligence_pet_bonus > 10 ? 10 : citizen.trait_intelligence + citizen.trait_intelligence_pet_bonus;
            int trait_luck = citizen.trait_luck + citizen.trait_luck_pet_bonus > 10 ? 10 : citizen.trait_luck + citizen.trait_luck_pet_bonus;

            double efficiency = (double)(
                (trait_agility / 2.0)
                + (trait_strength / 3.0)
                + ((trait_endurance + trait_charisma + trait_intelligence + trait_luck) / 24.0)
                );

            return efficiency * 10;
        }

        // OFF I - Trait #1	0.5	C - Trait #2 @ 1/3	    Trait #3(x4)	@ 1/6
        private double GetOfficeEfficiency(CitizenTrait citizen)
        {
            // Combine trait and pet(if assigned), checking for max of 10 - pet bonus may cause > max trait
            int trait_strength = citizen.trait_strength + citizen.trait_strength_pet_bonus > 10 ? 10 : citizen.trait_strength + citizen.trait_strength_pet_bonus;
            int trait_endurance = citizen.trait_endurance + citizen.trait_endurance_pet_bonus > 10 ? 10 : citizen.trait_endurance + citizen.trait_endurance_pet_bonus;
            int trait_agility = citizen.trait_agility + citizen.trait_agility_pet_bonus > 10 ? 10 : citizen.trait_agility + citizen.trait_agility_pet_bonus;
            int trait_charisma = citizen.trait_charisma + citizen.trait_charisma_pet_bonus > 10 ? 10 : citizen.trait_charisma + citizen.trait_charisma_pet_bonus;
            int trait_intelligence = citizen.trait_intelligence + citizen.trait_intelligence_pet_bonus > 10 ? 10 : citizen.trait_intelligence + citizen.trait_intelligence_pet_bonus;
            int trait_luck = citizen.trait_luck + citizen.trait_luck_pet_bonus > 10 ? 10 : citizen.trait_luck + citizen.trait_luck_pet_bonus;

            double efficiency = (double)(
                (trait_intelligence / 2.0)
                + (trait_charisma / 3.0)
                + ((trait_agility + trait_endurance + trait_strength + trait_luck) / 24.0)
                );

            return efficiency * 10;
        }

        // Comm	C - Trait #1	0.5	L - Trait #2 @ 1/3  	Trait #3(x4) @ 1/6
        private double GetCommercialEfficiency(CitizenTrait citizen)
        {
            // Combine trait and pet(if assigned), checking for max of 10 - pet bonus may cause > max trait
            int trait_strength = citizen.trait_strength + citizen.trait_strength_pet_bonus > 10 ? 10 : citizen.trait_strength + citizen.trait_strength_pet_bonus;
            int trait_endurance = citizen.trait_endurance + citizen.trait_endurance_pet_bonus > 10 ? 10 : citizen.trait_endurance + citizen.trait_endurance_pet_bonus;
            int trait_agility = citizen.trait_agility + citizen.trait_agility_pet_bonus > 10 ? 10 : citizen.trait_agility + citizen.trait_agility_pet_bonus;
            int trait_charisma = citizen.trait_charisma + citizen.trait_charisma_pet_bonus > 10 ? 10 : citizen.trait_charisma + citizen.trait_charisma_pet_bonus;
            int trait_intelligence = citizen.trait_intelligence + citizen.trait_intelligence_pet_bonus > 10 ? 10 : citizen.trait_intelligence + citizen.trait_intelligence_pet_bonus;
            int trait_luck = citizen.trait_luck + citizen.trait_luck_pet_bonus > 10 ? 10 : citizen.trait_luck + citizen.trait_luck_pet_bonus;

            double efficiency = (double)(
                (trait_charisma / 2.0)
                + (trait_luck / 3.0)
                + ((trait_agility + trait_intelligence + trait_endurance + trait_strength) / 24.0)
                );

            return efficiency * 10;
        }

        // Municipal	L - Trait #1	0.5	I - Trait #2 @1/3	    Trait #3(x4) @ 1/6
        private double GetMunicipalEfficiency(CitizenTrait citizen)
        {
            // Combine trait and pet(if assigned), checking for max of 10 - pet bonus may cause > max trait
            int trait_strength = citizen.trait_strength + citizen.trait_strength_pet_bonus > 10 ? 10 : citizen.trait_strength + citizen.trait_strength_pet_bonus;
            int trait_endurance = citizen.trait_endurance + citizen.trait_endurance_pet_bonus > 10 ? 10 : citizen.trait_endurance + citizen.trait_endurance_pet_bonus;
            int trait_agility = citizen.trait_agility + citizen.trait_agility_pet_bonus > 10 ? 10 : citizen.trait_agility + citizen.trait_agility_pet_bonus;
            int trait_charisma = citizen.trait_charisma + citizen.trait_charisma_pet_bonus > 10 ? 10 : citizen.trait_charisma + citizen.trait_charisma_pet_bonus;
            int trait_intelligence = citizen.trait_intelligence + citizen.trait_intelligence_pet_bonus > 10 ? 10 : citizen.trait_intelligence + citizen.trait_intelligence_pet_bonus;
            int trait_luck = citizen.trait_luck + citizen.trait_luck_pet_bonus > 10 ? 10 : citizen.trait_luck + citizen.trait_luck_pet_bonus;

            double efficiency = (double)(
                (trait_luck / 2.0)
                + (trait_intelligence / 3.0)
                + ((trait_agility + trait_charisma + trait_endurance + trait_strength) / 24.0)
                );

            return efficiency * 10;
        }

        // Energy:Water	E - Trait #1 @ 1/2      A - Trait #2 @ 1/3	    Trait #3(x4) @ 1/6
        private double GetEnergyWaterEfficiency(CitizenTrait citizen)
        {
            // Combine trait and pet(if assigned), checking for max of 10 - pet bonus may cause > max trait
            int trait_strength = citizen.trait_strength + citizen.trait_strength_pet_bonus > 10 ? 10 : citizen.trait_strength + citizen.trait_strength_pet_bonus;
            int trait_endurance = citizen.trait_endurance + citizen.trait_endurance_pet_bonus > 10 ? 10 : citizen.trait_endurance + citizen.trait_endurance_pet_bonus;
            int trait_agility = citizen.trait_agility + citizen.trait_agility_pet_bonus > 10 ? 10 : citizen.trait_agility + citizen.trait_agility_pet_bonus;
            int trait_charisma = citizen.trait_charisma + citizen.trait_charisma_pet_bonus > 10 ? 10 : citizen.trait_charisma + citizen.trait_charisma_pet_bonus;
            int trait_intelligence = citizen.trait_intelligence + citizen.trait_intelligence_pet_bonus > 10 ? 10 : citizen.trait_intelligence + citizen.trait_intelligence_pet_bonus;
            int trait_luck = citizen.trait_luck + citizen.trait_luck_pet_bonus > 10 ? 10 : citizen.trait_luck + citizen.trait_luck_pet_bonus;

            double efficiency = (double)(
                (trait_endurance / 2.0)
                + (trait_agility / 3.0)
                + ((trait_intelligence + trait_charisma + trait_luck + trait_strength) / 24.0)
                );

            return efficiency * 10;
        }

        // Energy:Electric	E - Trait #1 @ 5/9      A - Trait #2 @ 3/9	    Trait #3(x4) @ 1/9  ( Endurance:25%  Agility:15%   Rest:5%  Resource:25%, Influance:35%)  
        private double GetEnergyElectricEfficiency(CitizenTrait citizen)
        {
            // Combine trait and pet(if assigned), checking for max of 10 - pet bonus may cause > max trait
            int trait_strength = citizen.trait_strength + citizen.trait_strength_pet_bonus > 10 ? 10 : citizen.trait_strength + citizen.trait_strength_pet_bonus;
            int trait_endurance = citizen.trait_endurance + citizen.trait_endurance_pet_bonus > 10 ? 10 : citizen.trait_endurance + citizen.trait_endurance_pet_bonus;
            int trait_agility = citizen.trait_agility + citizen.trait_agility_pet_bonus > 10 ? 10 : citizen.trait_agility + citizen.trait_agility_pet_bonus;
            int trait_charisma = citizen.trait_charisma + citizen.trait_charisma_pet_bonus > 10 ? 10 : citizen.trait_charisma + citizen.trait_charisma_pet_bonus;
            int trait_intelligence = citizen.trait_intelligence + citizen.trait_intelligence_pet_bonus > 10 ? 10 : citizen.trait_intelligence + citizen.trait_intelligence_pet_bonus;
            int trait_luck = citizen.trait_luck + citizen.trait_luck_pet_bonus > 10 ? 10 : citizen.trait_luck + citizen.trait_luck_pet_bonus;

            double efficiency = (double)(
                ((trait_endurance / 9.0) * 5.0)
                + (trait_agility / 3.0)
                + ((trait_intelligence + trait_charisma + trait_luck + trait_strength) / 36.0)
                );

            return efficiency * 10;
        }
    }
}
