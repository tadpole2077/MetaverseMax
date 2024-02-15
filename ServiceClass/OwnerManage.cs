using Newtonsoft.Json.Linq;
using SimpleBase;
using System.Text;
using MetaverseMax.BaseClass;
using MetaverseMax.Database;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using Microsoft.IdentityModel.Tokens;

namespace MetaverseMax.ServiceClass
{

    public class OwnerManage : ServiceBase
    {
        private static Dictionary<string, OwnerAccount> ownersListTRON = new();
        private static Dictionary<string, OwnerAccount> ownersListETH = new();
        private static Dictionary<string, OwnerAccount> ownersListBNB = new();
        private static Dictionary<int, OwnerUni> ownersListUNI = new();

        public OwnerData ownerData = new() { plot_count = -1 };
        private ServiceCommon common = new();

        public OwnerManage(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext, worldTypeSelected)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;

            GetOwners(false); // Check if static dictionary has been loaded
        }

        // Dictionary is a ref type - return the reference so that it can be populated, and avoid use of local dictionary as it will create another layer reference to a reference.
        private ref Dictionary<string, OwnerAccount> GetOwnersListByWorldType()
        {
            switch (worldType)
            {
                case WORLD_TYPE.ETH:
                    return ref ownersListETH;
                case WORLD_TYPE.BNB:
                    return ref ownersListBNB;
                case WORLD_TYPE.TRON:
                default:
                    return ref ownersListTRON;
            };
        }


        public Dictionary<string, OwnerAccount> GetOwners(bool refresh)
        {
            ref Dictionary<string, OwnerAccount> ownersDict = ref GetOwnersListByWorldType();         // use the root dictionary to populate data.

            if (refresh)
            {
                ownersDict.Clear();
                ownersListUNI.Clear();
            }

            if (ownersDict.Count == 0)
            {
                OwnerDB ownerDB = new(_context);
                ownerDB.PopulateOwnersDicFromDB(ref ownersDict);          // Pass dictionary which is a reference type - called method will assign a new dictionary to this passed dict ref.
            }

            if (ownersListUNI.Count == 0)
            {
                using (MetaverseMaxDbContext_UNI _contextUni = new())
                {
                    OwnerUniDB ownerDB = new(_contextUni);
                    ownerDB.PopulateOwnersDicFromDB(ref ownersListUNI);          // Pass dictionary which is a reference type - called method will assign a new dictionary to this passed dict ref.
                }                    
            }

            return ownersDict;
        }      

        public List<OwnerNameWeb> GetOwnersWithName()
        {
            List<OwnerNameWeb> ownerNameList = new();
           
            foreach (KeyValuePair<string, OwnerAccount> owner in GetOwners(false))
            {
                if (owner.Value.name != null && owner.Value.name != string.Empty)
                {
                    ownerNameList.Add(new()
                    {
                        public_key = worldType switch { WORLD_TYPE.TRON => owner.Value.public_key, WORLD_TYPE.BNB => owner.Value.matic_key, WORLD_TYPE.ETH => owner.Value.matic_key, _ => owner.Value.matic_key },
                        name = owner.Value.name,
                        avatar_id = owner.Value.avatar_id,
                    });

                }
            }

            return ownerNameList.OrderBy(x => x.name).ToList();
        }

        public RETURN_CODE SyncOwner(List<OwnerChange> ownerChangeList)
        {
            using MetaverseMaxDbContext_UNI _contextUni = new();
            OwnerDB ownerDB = new(_context);
            OwnerNameDB ownerNameDB = new(_context);
            OwnerUniDB ownerUniDB = new(_contextUni);
            bool updated;

            ownerDB.SyncOwner();

            ownerChangeList.ForEach(x => {

                if (x.owner_matic_key.IsNullOrEmpty())
                {
                    _context.LogEvent(String.Concat("OwnerManage.SyncOwner() : Anomoly found - ownerChange entry with No owner_matic_key,  owner_name: ", x.owner_name));
                }
                else
                {
                    ownerNameDB.UpdateOwnerName(x, true);

                    updated = ownerUniDB.CheckLink(x, worldType);

                    // Defensive coding - check for missing world.owner.owner_uni_key
                    // May be used if a specific World DB is missing owner_uni_key mappings, dropped or deleted for some reason.
                    // Should not really occur, but useful as a backup process.
                    if (updated == false)
                    {
                        int localOwnerUnitID = GetOwnerUniIDByMatic(x.owner_matic_key);     // Pull from local cache should reflect database.
                        if (localOwnerUnitID == 0)
                        {
                            // Get from OwnerUni table
                            OwnerUni ownerUni = ownerUniDB.GetOwner(x.owner_matic_key, worldType);

                            // Update Local - if db world.owner.owner_uni_key mapping key found.
                            if (ownerUni != null)
                            {
                                ownerDB.UpdateOwner_UniID(x.owner_matic_key, ownerUni.owner_uni_id);
                            }
                        }
                    }
                }
            });

            _context.SaveChanges();

            return RETURN_CODE.SUCCESS;
        }

        // SyncOwner - Using local store ownerlist, complete full sync process.
        public RETURN_CODE SyncWorldOwnerAll()
        {
            List<OwnerChange> ownerChangeList = new List<OwnerChange>();
            Dictionary<string, OwnerAccount> ownersList = GetOwnersListByWorldType();

            foreach (var ownerAccount in ownersList)
            {
                ownerChangeList.Add(new OwnerChange()
                {
                    owner_matic_key = ownerAccount.Key,
                    owner_avatar_id = ownerAccount.Value.avatar_id,
                    owner_name = ownerAccount.Value.name
                });                
            }
            
            SyncOwner(ownerChangeList);

            GetOwners(true);          // Refresh LOCAL owner lists after nightly sync
            
            return RETURN_CODE.SUCCESS;
        }

    // Finding match on maticKey as this is the key used by MCP for ownership of assets in-world and within local db's
    public OwnerAccount FindOwnerByMatic(string maticKey, string walletPublicKey)
        {
            OwnerAccount ownerAccount = new();
            string dbWalletKey = string.Empty;
            ref Dictionary<string, OwnerAccount> ownersList = ref GetOwnersListByWorldType();

            if (ownersList.TryGetValue(maticKey, out ownerAccount))
            {
                // Matic found, but no public wallet key
                if (ownerAccount.public_key == string.Empty)
                {
                    ownerAccount.public_key = walletPublicKey;
                }
                ownerAccount.wallet_active_in_world = true;
            }
            else
            {
                ownerAccount = new();
                ownerAccount.matic_key = "";
                ownerAccount.pro_tools_enabled = false;
                ownerAccount.wallet_active_in_world = false;
            }

            ownerAccount.checked_matic_key = maticKey;

            return ownerAccount;
        }

        // Finding match on maticKey as this is the key used by MCP for ownership of assets in-world and within local db's
        public string FindOwnerNameByMatic(string maticKey)
        {
            OwnerAccount ownerAccount = null;
            ref Dictionary<string, OwnerAccount> ownersList = ref GetOwnersListByWorldType();
            string accountName = string.Empty;

            if (ownersList.TryGetValue(maticKey.ToLower(), out ownerAccount))
            {                
                accountName = ownerAccount.name;
            }

            if (accountName == string.Empty) {  

                if (ownerAccount != null)
                {
                        accountName = ownerAccount.public_key.ToLower();
                }
                else
                {
                    accountName = maticKey.ToLower();
                }
            }

            return accountName;
        }

        public OwnerAccount GetOwnerAccountByMatic(string maticKey)
        {
            OwnerAccount ownerAccount = null;
            ref Dictionary<string, OwnerAccount> ownersList = ref GetOwnersListByWorldType();

            ownersList.TryGetValue(maticKey.ToLower(), out ownerAccount);

            return ownerAccount;
        }

        public OwnerAccountWeb GetOwnerAccountWebByMatic(string maticKey)
        {
            OwnerAccount ownerAccount = GetOwnerAccountByMatic(maticKey);
            
            ownersListUNI.TryGetValue(ownerAccount.ownerUniID, out OwnerUni ownerUni);

            return MapFieldForWeb(ownerAccount, ownerUni);
        }

        public int GetOwnerUniIDByMatic(string maticKey)
        {
            OwnerAccount ownerAccount = GetOwnerAccountByMatic(maticKey);

            return ownersListUNI.TryGetValue(ownerAccount.ownerUniID, out OwnerUni ownerUni) ? ownerUni.owner_uni_id : 0;            
        }

        public decimal GetOwnerBalanceByMatic(string maticKey)
        {
            OwnerAccountWeb ownerAccountWeb = GetOwnerAccountWebByMatic(maticKey);

            return ownerAccountWeb == null ? 0 : ownerAccountWeb.balance;
        }

        public bool CheckOwnerExistsByMatic(string maticKey)
        {
            ref Dictionary<string, OwnerAccount> ownersList = ref GetOwnersListByWorldType();

            return ownersList.TryGetValue(maticKey.ToLower(), out _);
        }


        public int GetSlowDown(string maticKey)
        {
            int slowDownSeconds = 0;
            OwnerAccount ownerAccount;

            if (maticKey == string.Empty)
            {
                slowDownSeconds = -1;
            }
            else
            {
                ownerAccount = FindOwnerByMatic(maticKey, string.Empty);
                if (ownerAccount.pro_tools_enabled)
                {
                    slowDownSeconds = (int)((ownerAccount.slowdown_end ?? DateTime.UtcNow) - DateTime.UtcNow).TotalSeconds;
                    slowDownSeconds = slowDownSeconds < 0 ? 0 : slowDownSeconds;
                }
            }

            return slowDownSeconds;
        }
        public bool SetSlowDown(string maticKey)
        {
            bool slowDownSet = false;
            OwnerAccount ownerAccount;

            if (maticKey == string.Empty)
            {
                slowDownSet = false;
            }
            else
            {
                ownerAccount = FindOwnerByMatic(maticKey, string.Empty);

                if (ownerAccount.pro_tools_enabled &&
                    (ownerAccount.slowdown_end == null || ownerAccount.slowdown_end <= DateTime.UtcNow))
                {
                    ownerAccount.slowdown_end = DateTime.UtcNow.AddMinutes(2);
                    slowDownSet = true;
                }
            }

            return slowDownSet;
        }

        public IEnumerable<Offer> GetOfferLocal(bool active, string ownerMaticKey)
        {
            OwnerDB ownerDB = new(_context);
            OwnerOfferDB ownerOfferDB = new(_context);
            List<OwnerOffer> ownerOfferList = new();
            List<Offer> offerList = new();
            bool sold = !active;

            try
            {
                ownerOfferList = ownerOfferDB.GetbyOwnerMatic(ownerMaticKey);

                foreach (OwnerOffer offer in ownerOfferList)
                {
                    if (offer.active == active && offer.sold == sold)
                    {
                        Owner owner = ownerDB.GetOwner(offer.buyer_matic_key);

                        offerList.Add(new Offer()
                        {
                            buyer_matic_key = offer.buyer_matic_key,
                            buyer_owner_name = owner == null ? "" : owner.owner_name,
                            buyer_avatar_id = (int)(owner == null || owner.avatar_id == null ? 0 : owner.avatar_id),
                            buyer_offer = offer.buyer_offer,
                            offer_date = offer.offer_date == null ? "" : ((DateTime)offer.offer_date).ToString("yyyy/MMM/dd"),
                            token_type_id = offer.token_type,
                            token_type = common.LookupTokenType(offer.token_type),
                            token_id = offer.token_id,
                            token_district = offer.plot_district,
                            token_pos_x = offer.plot_x,
                            token_pos_y = offer.plot_y,
                            sold = offer.sold,
                            sold_date = offer.sold_date == null ? "" : ((DateTime)offer.sold_date).ToString("yyyy/MMM/dd")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
            }

            return offerList.ToArray();
        }
        

        // Get from MCP 3rd tier services
        // LEGACY SYSTEM : 2019 offers WS response a few offers contain no root>sale_data>buyer_matic_key && root>sale_data>value (Buyer offer).  Skip these offers
        public async Task<string> GetOwnerOfferMCP(string maticKey)
        {
            String content = string.Empty;
            OwnerOffer ownerOffer = new();
            OwnerOfferDB ownerOfferDB = new(_context);
            AlertManage alert = new(_context, worldType);
            Building building = new();
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            string activitySummary = string.Empty;
            int retryCount = 0;
            int activeOfferCount = 0, soldOfferCount = 0, cancelledOfferRemovedCount = 0;
            List<int> validOffers = new();
            serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.SALES_OFFER, WORLD_TYPE.BNB => BNB_WS.SALES_OFFER, WORLD_TYPE.ETH => ETH_WS.SALES_OFFER, _ => TRON_WS.SALES_OFFER };

            while (returnCode == RETURN_CODE.ERROR && retryCount < 3)
            {
                try
                {
                    retryCount++;

                    // POST from Land/Get REST WS
                    HttpResponseMessage response;
                    using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                    {
                        StringContent stringContent = new StringContent("{\"address\": \"" + maticKey + "\"}", Encoding.UTF8, "application/json");

                        response = await client.PostAsync(
                            serviceUrl,
                            stringContent);

                        response.EnsureSuccessStatusCode(); // throws if not 200-299
                        content = await response.Content.ReadAsStringAsync();

                    }
                    // End timer
                    watch.Stop();
                    servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, maticKey);

                    if (content.Length > 0)
                    {
                        //JObject jsonContent = JObject.Parse(content);                    
                        JArray offers = JArray.Parse(content);

                        for (int index = 0; index < offers.Count; index++)
                        {
                            // In-active offers may have additional data such as Sold date
                            if (offers[index].Value<bool>("is_active"))
                            {
                                ownerOffer = new OwnerOffer();
                                ownerOffer.token_owner_matic_key = maticKey;
                                ownerOffer.active = true;

                                ownerOffer.offer_id = offers[index].Value<int?>("id") ?? 0;
                                ownerOffer.offer_date = common.TimeFormatStandardFromUTC(offers[index].Value<string>("event_time"), null);

                                ownerOffer.token_id = offers[index].Value<int?>("token_id") ?? 0;
                                ownerOffer.token_type = offers[index].Value<int?>("token_type") ?? 0;

                                JObject sale_data = offers[index].Value<JObject>("sale_data");
                                ownerOffer.buyer_matic_key = sale_data.Value<string>("buyer");
                                ownerOffer.buyer_offer = building.GetPrice(sale_data, worldType, "value", true);

                                JObject data = offers[index].Value<JObject>("data");
                                if (data != null && data.Count > 0)
                                {

                                    JObject token_info = data.Value<JObject>("token_info");
                                    if (token_info != null && token_info.Count > 0)
                                    {
                                        ownerOffer.plot_district = token_info.Value<int?>("region_id") ?? 0;
                                        ownerOffer.plot_x = token_info.Value<int?>("x") ?? 0;
                                        ownerOffer.plot_y = token_info.Value<int?>("y") ?? 0;
                                    }
                                }

                                if (!string.IsNullOrEmpty(ownerOffer.buyer_matic_key))
                                {
                                    ownerOfferDB.AddorUpdate(ownerOffer);
                                    activeOfferCount++;
                                    validOffers.Add(ownerOffer.offer_id);
                                }

                                if (ownerOffer.offer_date >= DateTime.UtcNow.AddDays(-1)) 
                                {
                                    alert.AddOfferAlert(ownerOffer.token_owner_matic_key, ownerOffer.buyer_matic_key, ownerOffer.token_type, ownerOffer.buyer_offer, ownerOffer.token_id);
                                }
                            }
                            else if (offers[index].Value<bool>("is_active") == false && (offers[index].Value<int?>("is_cancelled") ?? 0) == 0)
                            {
                                ownerOffer = new OwnerOffer();
                                ownerOffer.token_owner_matic_key = maticKey;
                                ownerOffer.active = false;

                                ownerOffer.offer_id = offers[index].Value<int?>("id") ?? 0;
                                ownerOffer.offer_date = common.TimeFormatStandardFromUTC(offers[index].Value<string>("event_time"), null);

                                ownerOffer.token_id = offers[index].Value<int?>("token_id") ?? 0;
                                ownerOffer.token_type = offers[index].Value<int?>("token_type") ?? 0;

                                JObject sale_data = offers[index].Value<JObject>("sale_data");
                                ownerOffer.buyer_matic_key = sale_data.Value<string>("buyer");
                                ownerOffer.buyer_offer = building.GetPrice(sale_data, worldType, "value", true);       //Return price even if sale is not active - PARA_4 = true

                                JObject data = offers[index].Value<JObject>("data");
                                if (data != null && data.Count > 0)
                                {

                                    JObject token_info = data.Value<JObject>("token_info");
                                    if (token_info != null && token_info.Count > 0)
                                    {
                                        ownerOffer.plot_district = token_info.Value<int?>("region_id") ?? 0;
                                        ownerOffer.plot_x = token_info.Value<int?>("x") ?? 0;
                                        ownerOffer.plot_y = token_info.Value<int?>("y") ?? 0;
                                    }
                                }
                                ownerOffer.sold = true;
                                ownerOffer.sold_date = common.TimeFormatStandardFromUTC(offers[index].Value<string>("sale_time"), null);

                                if (!string.IsNullOrEmpty(ownerOffer.buyer_matic_key))
                                {
                                    ownerOfferDB.AddorUpdate(ownerOffer);
                                    soldOfferCount++;
                                    validOffers.Add(ownerOffer.offer_id);
                                }

                                if (ownerOffer.sold_date >= DateTime.UtcNow.AddDays(-1))
                                {
                                    alert.AddOfferAcceptAlert(ownerOffer.buyer_matic_key, ownerOffer.token_owner_matic_key, ownerOffer.token_type, ownerOffer.buyer_offer, ownerOffer.token_id);
                                }
                            }
                        }

                        cancelledOfferRemovedCount = ownerOfferDB.RemoveCancelledOffers(validOffers, maticKey);      // Remove any legacy active offers that were cancelled
                    }

                    _context.SaveChanges();
                    activitySummary = string.Concat("Active Offers Updated: ", activeOfferCount, "  Sold Offers Updated: ", soldOfferCount, "  Cancelled offers removed(previously active): " + cancelledOfferRemovedCount);
                    returnCode = RETURN_CODE.SUCCESS;
                }
                catch (Exception ex)
                {
                    DBLogger dBLogger = new(_context.worldTypeSelected);
                    dBLogger.logException(ex, String.Concat("OwnerMange.GetOwnerOffer() : Error on WS calls for owner matic : ", maticKey));
                }
            }

            if (retryCount > 1 && returnCode == RETURN_CODE.SUCCESS)
            {
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerMange.GetOwnerOffer() : retry successful - no ", retryCount));
                }
            }

            return activitySummary;
        }

        public async Task<JArray> GetOwnerLandsMCP(string ownerMaticKey)
        {
            JArray lands = null;

            try
            {
                string content = string.Empty;
                serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.OWNER_LANDS, WORLD_TYPE.BNB => BNB_WS.OWNER_LANDS, WORLD_TYPE.ETH => ETH_WS.OWNER_LANDS, _ => TRON_WS.OWNER_LANDS };

                HttpResponseMessage response;
                using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                {
                    StringContent stringContent = new StringContent("{\"address\": \"" + ownerMaticKey + "\",\"short\": false}", Encoding.UTF8, "application/json");

                    response = await client.PostAsync(
                        serviceUrl,
                        stringContent);

                    response.EnsureSuccessStatusCode(); // throws if not 200-299
                    content = await response.Content.ReadAsStringAsync();

                }
                // End timer
                watch.Stop();
                servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, ownerMaticKey);

                JObject jsonContent = JObject.Parse(content);
                lands = jsonContent["items"] as JArray;
            }
            catch (Exception ex)
            {
                _context.LogEvent(String.Concat("OwnerMange.GetOwnerLands() : Error on WS calls for owner matic : ", ownerMaticKey));
                _context.LogEvent(ex.Message);
            }
            return lands;
        }

        public OwnerData GetOwnerLands(string ownerMaticKey, bool updatePlotflag, bool processFullUpdate)
        {
            try
            {
                List<Plot> localPlots = new();
                Building building = new();
                CitizenManage citizen = new(_context, worldType);
                BuildingManage buildingManage = new(_context, worldType);                

                PlotDB plotDB = new(_context, worldType);
                string landOwner;

                JArray lands = Task.Run(() => GetOwnerLandsMCP(ownerMaticKey)).Result;

                if (lands != null && lands.Count > 0)
                {
                    JToken land = lands.Children().First();

                    landOwner = land.Value<string>("owner") ?? "Not Found";
                    ownerData.plot_count = lands.Count;

                    if (lands.Any())
                    {
                        // CHECK owner buildings are recorded in local db - new plots recently purchased - token wont exist in local db : needs to exit to allow link of building to citizens
                        if (updatePlotflag)
                        {
                            CheckTokenExist(lands, processFullUpdate);
                        }
                        localPlots = plotDB.PlotsGet_ByOwnerMatic(ownerMaticKey).ToList();      // Only get stored plots after token check & ranking update.

                        ownerData.owner_land = lands.Select(landInstance =>
                        {
                            var plot = localPlots.Where(x => x.token_id == (landInstance.Value<int?>("token_id") ?? 0)).FirstOrDefault();

                            var citizenCount = citizen.GetCitizenCount(landInstance.Value<JArray>("citizens"));
                            var buildingTypeId = (BUILDING_TYPE)(landInstance.Value<int?>("building_type_id") ?? 0);
                            var buildingLevel = landInstance.Value<int?>("building_level") ?? 0;
                            var lastActionUx = landInstance.Value<double?>("last_action") ?? 0;
                            var buildingId = landInstance.Value<int?>("building_id") ?? 0;
                            var actionId = plot != null ? plot.action_id : 0;
                            ProductionCollection productionCollection = null;

                            // Find next collection details on active building : No additional WS calls required
                            if (buildingManage.CheckBuildingActive(buildingTypeId, citizenCount, buildingLevel) &&
                                buildingManage.CheckProductiveBuildingType(buildingTypeId))
                            {
                                productionCollection = buildingManage.CollectionEval(buildingTypeId, buildingLevel, lastActionUx);
                            }

                            return new OwnerLand
                            {
                                district_id = landInstance.Value<int?>("region_id") ?? 0,
                                pos_x = landInstance.Value<int?>("x") ?? 0,
                                pos_y = landInstance.Value<int?>("y") ?? 0,
                                plot_ip = landInstance.Value<int?>("influence") ?? 0,
                                ip_info = plot == null ? 0 : plot.influence_info ?? 0,
                                ip_bonus = (landInstance.Value<int?>("influence") ?? 0) * (landInstance.Value<int?>("influence_bonus") ?? 0) / 100,
                                building_type = (int)buildingTypeId,
                                building_category = -1,
                                building_desc = building.BuildingType((int)buildingTypeId, buildingId),
                                building_img = building.GetBuildingImg((BUILDING_TYPE)buildingTypeId, buildingId, buildingLevel, worldType),
                                last_actionUx = lastActionUx,
                                last_action = common.UnixTimeStampUTCToDateTimeString(lastActionUx, "Empty Plot"),
                                action_type = (int)EVENT_TYPE.UNKNOWN,
                                c_r = productionCollection == null ? false : productionCollection.ready,
                                c_d = productionCollection == null ? 0 : productionCollection.day,
                                c_h = productionCollection == null ? 0 : productionCollection.hour,
                                token_id = landInstance.Value<int?>("token_id") ?? 0,
                                building_level = buildingLevel,
                                resource = landInstance.Value<int?>("abundance") ?? 0,
                                citizen_count = citizenCount,
                                citizen_url = citizen.GetCitizenUrl(landInstance.Value<JArray>("citizens")),
                                citizen_stamina = citizen.GetLowStamina(landInstance.Value<JArray>("citizens")),
                                citizen_stamina_alert = citizen.CheckCitizenStamina(landInstance.Value<JArray>("citizens"), (int)buildingTypeId),
                                forsale_price = building.GetSalePrice(landInstance.Value<JToken>("sale_data"), worldType),
                                forsale = (landInstance.Value<string>("on_sale") ?? "False") == "False" ? false : true,
                                rented = landInstance.Value<string>("renter") != null,
                                current_influence_rank = CheckInfluenceRank(plot),
                                condition = landInstance.Value<int?>("condition") ?? 0,
                                active = 1, // buildingManage.CheckBuildingActive((BUILDING_TYPE)(landInstance.Value<int?>("building_type_id") ?? 0), citizen.GetCitizenCount(landInstance.Value<JArray>("citizens")), landInstance.Value<int?>("building_level") ?? 0) == true ? 1 : 0                                
                                product_id = BuildingManage.GetBuildingProduct((int)buildingTypeId, buildingId, actionId, worldType)
                        };
                                
                         
                        })
                        .ToArray()
                        .OrderBy(row => row.district_id).ThenBy(row => row.pos_x).ThenBy(row => row.pos_y);

                        AddOwnerParcel(ownerMaticKey, ownerData.owner_land, localPlots);

                        ownerData.developed_plots = ownerData.owner_land.Where(
                            row => row.last_action != "Empty Plot"
                            ).Count();

                        // Get Last Action across all lands for target player
                        ownerData.last_action = string.Concat(common.UnixTimeStampUTCToDateTimeString(ownerData.owner_land.Max(row => row.last_actionUx), "No Lands"), " GMT");

                        ownerData.plots_for_sale = ownerData.owner_land.Where(
                            row => row.forsale_price > 0
                            ).Count();

                        ownerData.district_plots = building.DistrictPlots(ownerData.owner_land);

                        ownerData.stamina_alert_count = ownerData.owner_land.Where(
                            row => row.citizen_stamina_alert == true
                            ).Count();

                    }
                }
                else
                {
                    ownerData.owner_land = null;
                    ownerData.developed_plots = 0;
                    ownerData.last_action = "";
                    ownerData.plots_for_sale = 0;
                    ownerData.district_plots = null;
                    ownerData.stamina_alert_count = 0;

                    if (string.Equals(ownerMaticKey, "Owner not Found"))
                    {
                        ownerData.search_info = "Unclaimed Plot, available for purchase!";
                    }
                    else
                    {
                        ownerData.last_action = "This player owns no land plots in " +
                             worldType switch { WORLD_TYPE.TRON => "Tron", WORLD_TYPE.BNB => "Binance", WORLD_TYPE.ETH => "Ethereum", _ => "Tron" }
                            + " World";
                    }
                }

            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("OwnerManage.GetOwnerLands() : Error on WS calls for owner matic : ", ownerMaticKey));
            }

            return ownerData;
        }

        private RETURN_CODE AddOwnerParcel(string ownerMaticKey, IEnumerable<OwnerLand> ownerBuildingsMCP, List<Plot> storedOwnerPlotList)
        {
            Building building = new();
            BuildingParcelDB buildingParcelDB = new(_context);
            List<BuildingParcel> buildingParcelList = null;
            //List<int> parcelIdList = storedOwnerPlotList.Where(x => x.parcel_id > 0)
            //    .Select(r => r.parcel_id)
            //     .DistinctBy(r => ((uint)r))
            //    .ToList();
                
            // Return only first plot within parcel (building) plot set.
            if (storedOwnerPlotList.Where(x => x.parcel_id > 0).Count() > 0)
            {
                buildingParcelList = buildingParcelDB.ParcelGetByAccountMatic(ownerMaticKey);

                foreach (BuildingParcel parcel in buildingParcelList)
                {
                    //Plot parcel = storedOwnerPlotList.Where(x => x.parcel_id == parcelId).FirstOrDefault();

                    ownerData.owner_land = ownerData.owner_land.Append(                        
                        new OwnerLand
                            {
                                district_id = parcel.district_id,
                                pos_x = parcel.pos_x,
                                pos_y = parcel.pos_y,
                                plot_ip = 0,
                                ip_info = 0,
                                ip_bonus = 0,
                                building_type = (int)BUILDING_TYPE.PARCEL,
                                building_category = parcel.building_category_id ?? 0,
                                building_desc = parcel.building_name == string.Empty || parcel.building_name == null ? string.Concat("Parcel - ", parcel.parcel_id) : parcel.building_name,
                                building_img = building.GetBuildingImg(BUILDING_TYPE.PARCEL, 0, 0, worldType, parcel.parcel_info_id ?? 0, parcel.parcel_id),
                                last_actionUx = ((DateTimeOffset)parcel.last_updated).ToUnixTimeSeconds(),
                                last_action = common.LocalTimeFormatStandardFromUTC(string.Empty, parcel.last_updated),
                                action_type = parcel.last_action_type,
                                token_id = storedOwnerPlotList.Where(x => x.parcel_id == parcel.parcel_id).FirstOrDefault().token_id,
                                building_level = 0,
                                resource = 0,
                                citizen_count = 0,
                                citizen_url = "",
                                citizen_stamina = 0,
                                citizen_stamina_alert = false,
                                forsale_price = parcel.current_price,
                                forsale = parcel.on_sale,
                                rented = false,
                                current_influence_rank = 0,
                                condition = 100,
                                active = 0,
                                unit = parcel.parcel_unit_count ?? 0
                            }
                    );
                }

                ownerData.owner_land = ownerData.owner_land.OrderBy(row => row.district_id).ThenBy(row => row.pos_x).ThenBy(row => row.pos_y);
            }

            return RETURN_CODE.SUCCESS;
        }


        // SCENARIO :  Mega Building Demolished : 3 new tokens generated for 3x (now) empty plots && 1x plot inherits original token.
        //                  3 x Plots handed by AddOrUpdatePlot() - Full Process as no matching plot token found in local database
        //                  1 x Plot [inherits original token] handled within UpdatePlotPartial(), but IP change also found as its an now an Empty plot has no IP - triggering a Full Process run (async call).
        private int CheckTokenExist(JArray ownerLandList, Boolean processFullUpdate)
        {
            int plotsUpdatedCount = 0, buildingTypeId;
            PlotDB plotDB = new PlotDB(_context, worldType);
            PlotManage plotManage = new PlotManage(_context, worldType);
            List<PlotCord> plotFullUpdateList = new();
            PlotCord currentPlotFullUpdate = null;
            bool refreshMission = false;

            for (int i = 0; i < ownerLandList.Count; i++)
            {
                buildingTypeId = ownerLandList[i].Value<int?>("building_type_id") ?? 0;

                // Dont update POI/Monument - as addtional evaluation needed on state chanage and ip impact on nearby plots. Nighly sync controls POI/Monument updates of plot.last_updated.
                if (buildingTypeId != (int)BUILDING_TYPE.POI)
                {
                    // ADDITIONAL EVAL NEEDED - As not saving to db until all account is updated - potential here for incorrct IPRanking calc - as prior buildings in set may change max-min for that league table. Not sure if EntityFramework using mix both local (context) and db records
                    // Set to save to db as a batch later due to increased Performance.
                    // KNOWN WEAKNESS - Does not update/reEval all other buildings IP Ranking - this building may impact the max-min which would then impact ranking for all other buidlings in that league level (but the performance hit means its not currently worth it - potential solution -  a ranking async task to update ranking changes on all building in respective league - if new min or max change identified)
                    currentPlotFullUpdate = plotManage.UpdatePlotPartial(ownerLandList[i], false, refreshMission);

                    if (currentPlotFullUpdate != null && currentPlotFullUpdate.fullUpdateRequired)
                    {
                        plotFullUpdateList.Add(currentPlotFullUpdate);
                    }
                    plotsUpdatedCount++;
                }
            }

            if (plotsUpdatedCount > 0)
            {
                _context.SaveChanges();
            }

            // Check if FullUpdate flag enabled, and any plots identified that require a full update (due to specific attribute change found that partial update cant handle)
            // Async call will, 1 second delay between calls.
            if (processFullUpdate && plotFullUpdateList.Count > 0)
            {
                Task.Run(() => plotManage.UpdateBuildingAsyncFull(plotFullUpdateList));
            }

            return plotsUpdatedCount;
        }

        private decimal CheckInfluenceRank(Plot plot)
        {
            return plot == null ? 0 : plot.current_influence_rank ?? 0;
        }

        public async Task<int> GetFromLandCoordMCP(int posX, int posY)
        {
            String content = string.Empty;
            int returnCode = 0, parcelId = 0;
            JObject jsonContent;            

            try
            {
                serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.LAND_GET, WORLD_TYPE.BNB => BNB_WS.LAND_GET, WORLD_TYPE.ETH => ETH_WS.LAND_GET, _ => TRON_WS.LAND_GET };

                HttpResponseMessage response;
                using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                {
                    StringContent stringContent = new StringContent("{\"x\": \"" + posX + "\",\"y\": \"" + posY + "\"}", Encoding.UTF8, "application/json");

                    response = await client.PostAsync(
                        serviceUrl,
                        stringContent);

                    response.EnsureSuccessStatusCode(); // throws if not 200-299
                    content = await response.Content.ReadAsStringAsync();

                }
                // End timer
                watch.Stop();
                servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, "{\"x\": \"" + posX + "\",\"y\": \"" + posY + "\"}");


                if (content.Length == 0)
                {
                    ownerData.search_info = "Plot does not exist";
                    returnCode = -1;
                }
                else
                {
                    jsonContent = JObject.Parse(content);

                    parcelId = jsonContent.Value<int?>("parcel_id") ?? 0;
                    if (parcelId > 0)
                    {
                        PlotManage plotManage = new PlotManage(_context, worldType);
                        // NOTE : Parcal plots have owner_avatar_id=0 and owner_nickname = ''  [Rule Handled in OwnerManage sproc's and code]
                        JObject jsonContentParcel = Task.Run(() => plotManage.GetParcelMCP(parcelId)).Result;
                        ownerData.owner_matic_key = jsonContentParcel.Value<string>("address");
                    }
                    else
                    {
                        ownerData.owner_matic_key = jsonContent.Value<string>("owner") ?? "";
                    }
                    
                    ownerData.search_token = jsonContent.Value<int?>("token_id") ?? 0;          // building token - matching X and Y plot - note multiple plots may comprise a single building/token

                    returnCode = GetFromMaticKeyMCP(ownerData.owner_matic_key).Result;
                }
            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("OwnerMange.GetFromLandCoord() : Error on WS calls for Pos_X : ", posX, " Pos_Y", posY));
            }

            return returnCode;
        }

        // See multi Nic IP use with HttpWebRequest https://github.com/dotnet/runtime/issues/23267
        public async Task<int> GetFromMaticKeyMCP(string ownerMaticKey)
        {
            String content = string.Empty;
            CitizenManage citizen = new(_context, worldType);
            int returnCode = 0;
            serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.USER_GET, WORLD_TYPE.BNB => BNB_WS.USER_GET, WORLD_TYPE.ETH => ETH_WS.USER_GET, _ => TRON_WS.USER_GET };

            try
            {
                OwnerOfferDB ownerOfferDB = new(_context);
                OwnerDB ownerDB = new(_context);
                Owner owner = ownerDB.GetOwner(ownerMaticKey);

                //ownerData.wallet_public = WalletConvert(jsonContent.Value<string>("owner") ?? string.Empty);
                if (string.IsNullOrEmpty(ownerMaticKey) || ownerMaticKey.Equals("Not Found"))
                {
                    AssignUnknownOwner();
                    returnCode = -1;
                }
                else
                {
                    HttpResponseMessage response;
                    using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                    {
                        StringContent stringContent = new StringContent("{\"address\": \"" + ownerMaticKey + "\",\"dapper\": false,\"sign\": null }", Encoding.UTF8, "application/json");

                        response = await client.PostAsync(
                            serviceUrl,
                            stringContent);

                        response.EnsureSuccessStatusCode(); // throws if not 200-299
                        content = await response.Content.ReadAsStringAsync();

                    }

                    // End timer
                    watch.Stop();
                    servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, ownerMaticKey);

                    if (content.Length == 0 || JObject.Parse(content).Count == 0)
                    {
                        AssignUnknownOwner(string.Concat("Owner not Found matching Matic key ", ownerMaticKey));
                        ownerData.owner_matic_key = ownerMaticKey;
                        ownerData.search_info = "";
                        returnCode = -1;
                    }
                    else
                    {
                        ownerData.owner_matic_key = ownerMaticKey;
                        JObject jsonContent = JObject.Parse(content);

                        ownerData.owner_name = jsonContent.Value<string>("avatar_name") ?? "Not Found";
                        ownerData.owner_name = ownerData.owner_name == string.Empty ? owner.discord_name ?? string.Empty : ownerData.owner_name;

                        ownerData.owner_url = citizen.AssignDefaultOwnerImg(jsonContent.Value<string>("avatar_id") ?? "");

                        ownerData.registered_date = common.LocalTimeFormatStandardFromUTC(jsonContent.Value<string>("registered"), null);
                        ownerData.last_visit = common.LocalTimeFormatStandardFromUTC(jsonContent.Value<string>("last_visited"), null);

                        ownerData.owner_offer = GetOfferLocal(true, ownerMaticKey);
                        ownerData.offer_count = ownerData.owner_offer == null ? 0 : ownerData.owner_offer.Count();

                        ownerData.owner_offer_sold = GetOfferLocal(false, ownerMaticKey);
                        ownerData.offer_sold_count = ownerData.owner_offer_sold == null ? 0 : ownerData.owner_offer_sold.Count();

                        ownerData.offer_last_updated = common.LocalTimeFormatStandardFromUTC(string.Empty, _context.ActionTimeGet(ACTION_TYPE.OFFER));

                        if (owner != null)
                        {
                            ownerData.pet_count = owner.pet_count ?? 0;
                            ownerData.citizen_count = owner.citizen_count ?? 0;
                        }
                        
                        ownerData.pack = GetPacksMCP(ownerMaticKey).Result;
                        ownerData.pack_count = ownerData.pack.Count();
                    }
                }
            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("OwnerMange.GetFromMaticKey() : Error on WS calls for owner matic : ", ownerMaticKey));
            }

            return returnCode;
        }

        public async Task<List<Pack>> GetPacksMCP(string ownerMaticKey)
        {
            string content = string.Empty;
            JArray packJSON = null;
            List<Pack> packList = null;
            serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.USER_PACKS_GET, WORLD_TYPE.BNB => BNB_WS.USER_PACKS_GET, WORLD_TYPE.ETH => ETH_WS.USER_PACKS_GET, _ => TRON_WS.USER_PACKS_GET };

            try
            {

                if (string.IsNullOrEmpty(ownerMaticKey) || ownerMaticKey.Equals("Not Found"))
                {
                    packList = new();
                }
                else
                {
                    HttpResponseMessage response;
                    using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                    {
                        StringContent stringContent = new StringContent("{\"address\": \"" + ownerMaticKey + "\",\"filter\": { \"type_id\": 0}}", Encoding.UTF8, "application/json");

                        response = await client.PostAsync(
                            serviceUrl,
                            stringContent);

                        response.EnsureSuccessStatusCode(); // throws if not 200-299
                        content = await response.Content.ReadAsStringAsync();
                    }

                    if (content.Length > 0)
                    {
                        packJSON = JArray.Parse(content);                        

                        packList = packJSON.Select(pack =>
                        {

                            return new Pack
                            {
                                pack_id = pack.Value<int?>("resource_id") ?? 0,
                                amount = pack.Value<int?>("amount") ?? 0,
                                product_id = pack.Value<int?>("kind") ?? 0
                            };
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("OwnerMange.GetPacksMCP() : Error on WS calls for owner matic : ", ownerMaticKey));
            }

            return packList;
        }

        private int AssignUnknownOwner(string ownerName = "")
        {
            CitizenManage citizen = new(_context, worldType);

            ownerData.owner_name = string.IsNullOrEmpty(ownerName) ? "Owner not Found" : ownerName;
            ownerData.search_info = "Unclaimed Plot, available for purchase!";
            ownerData.owner_url = citizen.AssignDefaultOwnerImg("0");
            ownerData.plot_count = -1;

            return 0;
        }

        // Used by Key Service on initial load of Page to identify Account holder
        public OwnerAccountWeb MatchOwner(string publicKey)
        {
            string maticKeyFormated = string.Empty;
            OwnerAccountWeb ownerAccountWeb = new();
            OwnerAccount ownerAccount;
            AlertDB alertDB = new AlertDB(_context);

            if (worldType == WORLD_TYPE.TRON)
            {
                // Check if passed string is valid Tron key
                if (publicKey == "false")
                {
                    ownerAccountWeb.wallet_active_in_world = false;
                    ownerAccountWeb.matic_key = "";
                    return ownerAccountWeb;
                }                

                // Base58 Public Tron to Hex Conversion.
                // Span<byte> is analogous to byte[] in usage but allows the library
                // to avoid unnecessary memory copy operations unless needed.
                // you can also use "Ripple" or "Flickr" as decoder flavors
                if (!publicKey.Contains("0x"))
                {
                    Span<byte> result = Base58.Bitcoin.Decode(publicKey);
                    Span<byte> resultParsed = result;
                    resultParsed = resultParsed.Slice(1, result.Length - 5);
                    maticKeyFormated = string.Concat("0x", Convert.ToHexString(resultParsed)).ToLower();
                }
            }
            else
            {
                maticKeyFormated = publicKey;
            }

            ownerAccount = FindOwnerByMatic(maticKeyFormated, publicKey);

            // Update db - update ownerAccount : last_used, matching public wallet key if not already stored(on TRON matic and public differ)
            if (ownerAccount.wallet_active_in_world)
            {
                OwnerDB ownerDB = new OwnerDB(_context);
                ownerDB.UpdateOwner(maticKeyFormated, publicKey, ownerAccount);

                ownerAccount.alert_count = alertDB.count(ownerAccount.matic_key);
            }

            ServiceCommon serviceCommon = new();
            ownerAccount.app_shutdown_warning_alert = serviceCommon.CheckPendingShutdownSetting();

            ownersListUNI.TryGetValue(ownerAccount.ownerUniID, out OwnerUni ownerUni);
            
            return MapFieldForWeb(ownerAccount, ownerUni);
        }

        public static OwnerAccountWeb MapFieldForWeb(OwnerAccount ownerAccount, OwnerUni ownerUni)
        {

            return new OwnerAccountWeb()
            {
                alert_activated = ownerAccount.alert_activated,
                allow_link = ownerUni == null ? false : ownerUni.allow_link,
                app_shutdown_warning_alert = ownerAccount.app_shutdown_warning_alert,
                avatar_id = ownerAccount.avatar_id,
                alert_count = ownerAccount.alert_count,
                balance = ownerUni == null ? 0 : ownerUni.balance ?? 0,
                balance_visible = ownerUni == null ? false : ownerUni.balance_visible ?? false,
                checked_matic_key = ownerAccount.checked_matic_key,
                dark_mode = ownerAccount.dark_mode,
                discord_name = ownerAccount.discord_name,
                matic_key = ownerAccount.matic_key,
                name = ownerAccount.name,
                pro_expiry_days = ownerAccount.pro_expiry_days,
                pro_tools_enabled = ownerAccount.pro_tools_enabled,
                public_key = ownerAccount.public_key,
                slowdown_end = ownerAccount.slowdown_end,
                wallet_active_in_world = ownerAccount.wallet_active_in_world
            };
        }


        public bool UpdateDarkMode(string maticKey, bool darkMode)
        {
            OwnerDB ownerDB = new(_context);
            ownerDB.UpdateOwnerDarkMode(maticKey, darkMode);

            OwnerAccount ownerAccount = FindOwnerByMatic(maticKey, string.Empty);
            ownerAccount.dark_mode = darkMode;      // Update local cache store of ownerAccount.

            return true;
        }

        public bool UpdateBalanceVisible(string maticKey, bool visible)
        {
            OwnerAccount ownerAccount = FindOwnerByMatic(maticKey, string.Empty);

            using (MetaverseMaxDbContext_UNI _contextUni = new()) {
                OwnerUniDB ownerUniDB = new(_contextUni);
                ownerUniDB.UpdateBalanceVisible(ownerAccount.ownerUniID, visible);                
            }
            
            if (ownersListUNI.TryGetValue(ownerAccount.ownerUniID, out OwnerUni ownerUni))
            {
                ownerUni.balance_visible = visible;      // Update local cache store of ownerAccount.
            }

            return true;
        }

        // Used to refresh local cache for single Owner - No Balance change
        public bool UpdateOwnerBalanceFromDB(string maticKey)
        {
            OwnerUni ownerUni;            
            bool updated = false;

            using (MetaverseMaxDbContext_UNI _contextUni = new())
            {
                OwnerUniDB ownerUniDB = new(_contextUni);
                ownerUni = ownerUniDB.GetOwner(maticKey, worldType);
            }
            
            // Update local ownerUni Cache
            if (ownersListUNI.TryGetValue(ownerUni.owner_uni_id, out OwnerUni ownerUniAccount))
            {
                ownerUniAccount.balance = ownerUni.balance ?? 0;
                updated = true;
            }            
            
            return updated;
        }

        // Use to (a) create new owner account if not pre existing,  (b) Change balance of account
        public bool UpdateBalance(string maticKey, decimal amount)
        {
            MetaverseMaxDbContext_UNI _contextUni = new();
            OwnerUniDB ownerUniDB = new(_contextUni);            
            decimal balance;

            OwnerAccount ownerAccount = FindOwnerByMatic(maticKey, string.Empty);

            // Corner Case: Check if Owner Exists (found in local owner dic) - if not create owner record + ownername record
            if (ownerAccount.matic_key == string.Empty)
            {  
                // 1: Create Owner Record on target World
                OwnerNameDB ownerNameDB = new(_context);
                ownerNameDB.UpdateOwnerName(new OwnerChange()
                {
                    owner_matic_key = maticKey,
                    owner_avatar_id = 0,
                    owner_name = string.Empty
                }, false);

                _context.SaveChanges();

                OwnerChange ownerChange = new() { owner_avatar_id = 0, owner_matic_key = maticKey, owner_name = string.Empty };

                // 2: Create OwnerUni Record if no existing use of matching matic_key [otherwise use existing OwnerUNI record]
                ownerUniDB.CheckLink(ownerChange, worldType);

                // 3: Refresh local dic of owners
                GetOwners(true);                
                ownerAccount = FindOwnerByMatic(maticKey, string.Empty);                
            }

            // Update DB
            balance = ownerUniDB.UpdateOwnerBalance(amount, ownerAccount.ownerUniID);

            // Update local cache store of ownerAccount.
            if (ownersListUNI.TryGetValue(ownerAccount.ownerUniID, out OwnerUni ownerUni))
            {
                ownerUni.balance = balance;  
            }                

            _contextUni.Dispose();

            return true;
        }

        public int GetOwnerOrCreate(string maticKey)
        {
            OwnerAccount ownerAccount = FindOwnerByMatic(maticKey, string.Empty);

            // Corner Case: Check if Owner Exists (found in local owner dic) - if not create owner record + ownername record
            if (ownerAccount.matic_key == string.Empty)
            {
                OwnerNameDB ownerNameDB = new(_context);

                OwnerChange ownerChange = new()
                {
                    owner_matic_key = maticKey,
                    owner_avatar_id = 0,
                    owner_name = string.Empty
                };

                ownerNameDB.UpdateOwnerName(ownerChange, false);
                _context.SaveChanges();

                using (MetaverseMaxDbContext_UNI _contextUni = new())
                {
                    OwnerUniDB ownerUniDB = new(_contextUni);
                    ownerUniDB.CheckLink(ownerChange, worldType);
                }

                GetOwners(true);                // Refresh local dic of owners
            }            

            return GetOwnerUniIDByMatic(maticKey);
        }


        public List<OwnerSummaryDistrict> GetOwnerSummaryDistrict(int districtId, int instanceNo)
        {

            OwnerSummaryDistrictDB ownerSummaryDistrictDB = new(_context);
            List<OwnerSummaryDistrict> ownerSummaryDistrictList = ownerSummaryDistrictDB.GetOwnerSummeryDistrict(districtId, instanceNo);

            // Using Stored World owner account, assign latest avatar_id and ownerName used by account.
            ownerSummaryDistrictList.ForEach(o =>
            {
                OwnerAccount ownerAccount = FindOwnerByMatic(o.owner_matic, string.Empty);
                o.owner_avatar_id = ownerAccount.avatar_id;
                o.owner_name = ownerAccount.name;
            });

            return ownerSummaryDistrictList;
        }


        public int GetMaterialAllMatic()
        {
            Dictionary<string, OwnerAccount> ownersList = GetOwnersListByWorldType();
            OwnerMaterial ownerMaterial = null;
            OwnerMaterialDB ownerMaterialDB = new OwnerMaterialDB(_context);

            foreach (string maticKey in ownersList.Keys)
            {
                ownerMaterial = GetMaterialFromMatic(maticKey).Result;
                ownerMaterialDB.Add(ownerMaterial);
            }
            return 0;
        }

        public async Task<OwnerMaterial> GetMaterialFromMatic(string ownerMaticKey)
        {
            HttpResponseMessage response;
            String content = string.Empty;          
            OwnerMaterial ownerMaterial = new();
            ServiceCommon common = new();

            serviceUrl = MATIC_WS.ACCOUNT_MATERIAL_GET;

            try
            {
                using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                {
                    StringContent stringContent = new StringContent(string.Concat(
                        "{\"jsonrpc\": \"2.0\",",
                        "\"id\": 825,",
                        "\"method\": \"eth_call\",",
                        "\"params\": [{",
                        "\"data\": \"0xc3df5bf1000000000000000000000000" + ownerMaticKey.Substring(2),
                        "0000000000000000000000000000000000000000000000000000000000000040000000000000000000000000000000000000000000000000000000000000000f000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000000020000000000000000000000000000000000000000000000000000000000000003000000000000000000000000000000000000000000000000000000000000000400000000000000000000000000000000000000000000000000000000000000050000000000000000000000000000000000000000000000000000000000000006000000000000000000000000000000000000000000000000000000000000000700000000000000000000000000000000000000000000000000000000000000080000000000000000000000000000000000000000000000000000000000000009000000000000000000000000000000000000000000000000000000000000000a000000000000000000000000000000000000000000000000000000000000000b000000000000000000000000000000000000000000000000000000000000000c000000000000000000000000000000000000000000000000000000000000000d000000000000000000000000000000000000000000000000000000000000000e000000000000000000000000000000000000000000000000000000000000000f\",",
                        "\"to\": \"0x5e4f2dc880295a1296699bad6d471c1e2bdbb4e3\"},",
                        "\"latest\"]}"
                        ),
                        Encoding.UTF8, "application/json");

                    response = await client.PostAsync(
                        serviceUrl,
                        stringContent);

                    response.EnsureSuccessStatusCode(); // throws if not 200-299
                    content = await response.Content.ReadAsStringAsync();

                }

                // End timer
                watch.Stop();
                servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, ownerMaticKey);

                if (content.Length == 0 || JObject.Parse(content).Count == 0)
                {
                }
                else
                {
                    JObject jsonContent = JObject.Parse(content);
                    string result = jsonContent.Value<string>("result");
                    if (result != null && result.Length > 0)
                    {
                        result = result.Substring(2);  // remove leading 0x chars
                        // 64 char-byte chunks - 17 chunks
                        ownerMaterial.wood = (int)common.Extract64HexChunkToNumeric(result, 2);
                        ownerMaterial.sand = (int)common.Extract64HexChunkToNumeric(result, 3);
                        ownerMaterial.stone = (int)common.Extract64HexChunkToNumeric(result, 4);
                        ownerMaterial.metal = (int)common.Extract64HexChunkToNumeric(result, 5);
                        ownerMaterial.brick = (int)common.Extract64HexChunkToNumeric(result, 6);
                        ownerMaterial.glass = (int)common.Extract64HexChunkToNumeric(result, 7);
                        ownerMaterial.water = (int)common.Extract64HexChunkToNumeric(result, 8);
                        ownerMaterial.energy = (int)common.Extract64HexChunkToNumeric(result, 9);
                        ownerMaterial.steel = (int)common.Extract64HexChunkToNumeric(result, 10);
                        ownerMaterial.concrete = (int)common.Extract64HexChunkToNumeric(result, 11);
                        ownerMaterial.plastic = (int)common.Extract64HexChunkToNumeric(result, 12);
                        ownerMaterial.glue = (int)common.Extract64HexChunkToNumeric(result, 13);
                        ownerMaterial.mixes = (int)common.Extract64HexChunkToNumeric(result, 14);
                        ownerMaterial.composites = (int)common.Extract64HexChunkToNumeric(result, 15);
                        ownerMaterial.paper = (int)common.Extract64HexChunkToNumeric(result, 16);

                        ownerMaterial.owner_matic_key = ownerMaticKey;
                        ownerMaterial.last_updated = DateTime.UtcNow;
                    }
                }
                
            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("OwnerMange.GetMaterialFromMatic() : Error on WS calls for owner matic : ", ownerMaticKey));
            }

            return ownerMaterial;
        }
    }

}
