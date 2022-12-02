using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MetaverseMax.Database;
using Newtonsoft.Json.Linq;
using SimpleBase;
using Microsoft.EntityFrameworkCore;

namespace MetaverseMax.ServiceClass
{
    
    public class OwnerManage : ServiceBase
    {
        private static Dictionary<string, string> ownersList = new();
        public OwnerData ownerData = new() { plot_count = -1 };
        private Common common = new();

        public OwnerManage(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {            
            GetOwners(false); // Check if static dictionary has been loaded
        }

        public Dictionary<string, string> GetOwners(bool refresh)
        {   
            if (refresh)
            {
                ownersList.Clear();
            }

            if (ownersList.Count == 0)
            {
                OwnerDB ownerDB = new OwnerDB(_context);
                ownersList = ownerDB.GetOwners(WORLD_TYPE.TRON);
            }

            return ownersList;
        }

        public int SyncOwner()
        {
            OwnerDB ownerDB = new OwnerDB(_context);
            return ownerDB.SyncOwner();
        }

        public OwnerAccount FindOwnerByMatic(string maticKey, string tronKey)
        {
            OwnerAccount ownerAccount = new();
            string dbTronKey = string.Empty;

            if (ownersList.TryGetValue(maticKey, out dbTronKey))
            {
                // Matic found, but no Tron key
                if (dbTronKey == string.Empty)
                {
                    dbTronKey = tronKey;
                }

                OwnerDB ownerDB = new OwnerDB(_context);
                ownerDB.UpdateOwner(maticKey, dbTronKey);

                ownersList[maticKey] = tronKey;                

                ownerAccount.matic_key = maticKey;
                ownerAccount.tron_key = tronKey;
            }
            else
            {
                ownerAccount.matic_key = "Not Found";
                ownerAccount.checked_matic_key = maticKey;
            }

            return ownerAccount;
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
                            token_type = LookupTokenType(offer.token_type),
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

        private string LookupTokenType(int typeId)
        {
            string tokenType = string.Empty;

            tokenType = typeId switch
            {
                (int)TOKEN_TYPE.PLOT => "Plot",
                (int)TOKEN_TYPE.APPLICATION => "Application",
                (int)TOKEN_TYPE.CAR => "Car",
                (int)TOKEN_TYPE.CITIZEN => "Citizen",
                (int)TOKEN_TYPE.DISTRICT => "District",
                (int)TOKEN_TYPE.PET => "Pet",
                (int)TOKEN_TYPE.RESOURCE => "Resource",
                _ => "Unknown",
            };

            return tokenType;
        }
        
        // Get from MCP 3rd tier services
        public async Task<RETURN_CODE> GetOwnerOffer(bool activeOffer, string maticKey)
        {
            String content = string.Empty;
            OwnerOffer ownerOffer = new();
            OwnerOfferDB ownerOfferDB = new(_context);
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            int retryCount = 0;
            serviceUrl = "https://ws-tron.mcp3d.com/sales/offers";

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
                            if (offers[index].Value<bool>("is_active"))
                            {
                                ownerOffer = new OwnerOffer();
                                ownerOffer.token_owner_matic_key = maticKey;
                                ownerOffer.active = true;

                                ownerOffer.offer_id = offers[index].Value<int?>("id") ?? 0;
                                ownerOffer.offer_date = common.TimeFormatStandardDT(offers[index].Value<string>("event_time"), null);

                                ownerOffer.token_id = offers[index].Value<int?>("token_id") ?? 0;
                                ownerOffer.token_type = offers[index].Value<int?>("token_type") ?? 0;

                                JObject sale_data = offers[index].Value<JObject>("sale_data");
                                ownerOffer.buyer_matic_key = sale_data.Value<string>("buyer");
                                ownerOffer.buyer_offer = sale_data.Value<decimal>("value") / 1000000; //Tron to Trx

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

                                ownerOfferDB.AddorUpdate(ownerOffer);
                                returnCode++;
                            }
                            else if (offers[index].Value<bool>("is_active") == false && (offers[index].Value<int?>("is_cancelled") ?? 0) == 0)
                            {
                                ownerOffer = new OwnerOffer();
                                ownerOffer.token_owner_matic_key = maticKey;
                                ownerOffer.active = false;

                                ownerOffer.offer_id = offers[index].Value<int?>("id") ?? 0;
                                ownerOffer.offer_date = common.TimeFormatStandardDT(offers[index].Value<string>("event_time"), null);

                                ownerOffer.token_id = offers[index].Value<int?>("token_id") ?? 0;
                                ownerOffer.token_type = offers[index].Value<int?>("token_type") ?? 0;

                                JObject sale_data = offers[index].Value<JObject>("sale_data");
                                ownerOffer.buyer_matic_key = sale_data.Value<string>("buyer");
                                ownerOffer.buyer_offer = sale_data.Value<decimal>("value") / 1000000; //Tron to Trx

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
                                ownerOffer.sold_date = common.TimeFormatStandardDT(offers[index].Value<string>("sale_time"), null);

                                ownerOfferDB.AddorUpdate(ownerOffer);
                            }
                        }
                    }

                    returnCode = RETURN_CODE.SUCCESS;
                }
                catch (Exception ex)
                {
                    string log = ex.Message;
                    if (_context != null)
                    {
                        _context.LogEvent(String.Concat("OwnerMange.GetOwnerOffer() : Error on WS calls for owner matic : ", maticKey));
                        _context.LogEvent(log);
                    }
                }
            }

            if (retryCount > 1 && returnCode == RETURN_CODE.SUCCESS)
            {
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerMange.GetOwnerOffer() : retry successful - no ", retryCount));
                }
            }

            return returnCode;            
        }

        public async Task<OwnerData> GetOwnerLands(string ownerMaticKey)
        {
            try
            {
                string content = string.Empty;
                List<Plot> localPlots = new();
                Building building = new();
                CitizenManage citizen = new(_context);
                PlotDB plotDB = new(_context);
                string landOwner;
                serviceUrl = "https://ws-tron.mcp3d.com/user/assets/lands";

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
                JArray lands = jsonContent["items"] as JArray;
                if (lands != null && lands.Count > 0)
                {
                    localPlots = plotDB.PlotsGet_ByOwnerMatic(ownerMaticKey).ToList();

                    JToken land = lands.Children().First();

                    landOwner = land.Value<string>("owner") ?? "Not Found";
                    ownerData.plot_count = lands.Count;

                    if (lands.Any())
                    {
                        ownerData.owner_land = lands.Select(landInstance => new OwnerLand
                        {
                            district_id = landInstance.Value<int?>("region_id") ?? 0,
                            pos_x = landInstance.Value<int?>("x") ?? 0,
                            pos_y = landInstance.Value<int?>("y") ?? 0,
                            plot_ip = landInstance.Value<int?>("influence") ?? 0,
                            ip_bonus = (landInstance.Value<int?>("influence") ?? 0) * (landInstance.Value<int?>("influence_bonus") ?? 0) / 100,
                            building_type = landInstance.Value<int?>("building_type_id") ?? 0,
                            building_desc = building.BuildingType(landInstance.Value<int?>("building_type_id") ?? 0, landInstance.Value<int?>("building_id") ?? 0),
                            building_img = building.BuildingImg(landInstance.Value<int?>("building_type_id") ?? 0, landInstance.Value<int?>("building_id") ?? 0, landInstance.Value<int?>("building_level") ?? 0),
                            last_actionUx = landInstance.Value<double?>("last_action") ?? 0,
                            last_action = common.UnixTimeStampUTCToDateTime(landInstance.Value<double?>("last_action"), "Empty Plot"),
                            token_id = landInstance.Value<int?>("token_id") ?? 0,
                            building_level = landInstance.Value<int?>("building_level") ?? 0,
                            resource = landInstance.Value<int?>("abundance") ?? 0,
                            citizen_count = citizen.GetCitizenCount(landInstance.Value<JArray>("citizens")),
                            citizen_url = citizen.GetCitizenUrl(landInstance.Value<JArray>("citizens")),
                            citizen_stamina = citizen.GetLowStamina(landInstance.Value<JArray>("citizens")),
                            citizen_stamina_alert = citizen.CheckCitizenStamina(landInstance.Value<JArray>("citizens"), landInstance.Value<int?>("building_type_id") ?? 0),
                            forsale_price = building.GetSalePrice(landInstance.Value<JToken>("sale_data")),
                            forsale = (landInstance.Value<string>("on_sale") ?? "False") == "False" ? false : true,
                            rented = landInstance.Value<string>("renter") != null,
                            current_influence_rank = CheckInfluenceRank( localPlots.Where(x => x.token_id == (landInstance.Value<int?>("token_id") ?? 0)).FirstOrDefault() ),
                            condition = landInstance.Value<int?>("condition") ?? 0,
                        })
                        .OrderBy(row => row.district_id).ThenBy(row => row.pos_x).ThenBy(row => row.pos_y);


                        ownerData.developed_plots = ownerData.owner_land.Where(
                            row => row.last_action != "Empty Plot"
                            ).Count();

                        // Get Last Action across all lands for target player
                        ownerData.last_action = string.Concat(common.UnixTimeStampUTCToDateTime(ownerData.owner_land.Max(row => row.last_actionUx), "No Lands"), " GMT");

                        ownerData.plots_for_sale = ownerData.owner_land.Where(
                            row => row.forsale_price > 0
                            ).Count();

                        ownerData.district_plots = building.DistrictPlots(ownerData.owner_land);

                        ownerData.stamina_alert_count = ownerData.owner_land.Where(
                            row => row.citizen_stamina_alert == true
                            ).Count();

                        // CHECK owner buildings are recorded in local db - new plots recently purchased - token wont exist causing issues with linking to citizens
                        _ = Task.Run(() => OwnerManage.CheckLandTokensExist(lands, _context.Database.GetConnectionString()));
                    }
                }
                else
                {
                    if (string.Equals(ownerMaticKey, "Owner not Found"))
                    {
                        ownerData.search_info = "Unclaimed Plot, available for purchase!";
                    }
                    else
                    {
                        ownerData.last_action = "This player owns no land plots in Tron World";
                    }
                }

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerMange.GetOwnerLands() : Error on WS calls for owner matic : ", ownerMaticKey));
                    _context.LogEvent(log);
                }
            }

            return ownerData;
        }

        private static int CheckLandTokensExist(JArray ownerLandList, string dbConnectionString)
        {
            MetaverseMaxDbContext _contextTemp;
            _contextTemp = new MetaverseMaxDbContext(dbConnectionString);

            // CHECK land token exist - it may have been recently created and not in local db
            PlotDB plotDB = new PlotDB(_contextTemp);

            for(int i = 0; i < ownerLandList.Count; i++)
            {                               
                plotDB.UpdatePlot(ownerLandList[i], true);                                    
            }

            _contextTemp.SaveChanges();
            return 0;
        }


        private decimal CheckInfluenceRank(Plot plot) {
            return plot == null ? 0 : plot.current_influence_rank ?? 0;
        }

        public async Task<int> GetFromLandCoord(int posX, int posY)
        {
            String content = string.Empty;
            int returnCode = 0;
            
            try
            {
                serviceUrl = "https://ws-tron.mcp3d.com/land/get";

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
                    JObject jsonContent = JObject.Parse(content);
                    ownerData.owner_matic_key = jsonContent.Value<string>("owner") ?? "";

                    returnCode = GetFromMaticKey(ownerData.owner_matic_key).Result;
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerMange.GetFromLandCoord() : Error on WS calls for Pos_X : ", posX, " Pos_Y", posY));
                    _context.LogEvent(log);
                }
            }

            return returnCode;
        }


        // See multi Nic IP use with HttpWebRequest https://github.com/dotnet/runtime/issues/23267
        public async Task<int> GetFromMaticKey(string ownerMaticKey)
        {
            String content = string.Empty;
            CitizenManage citizen = new(_context);
            int returnCode = 0;
            serviceUrl = "https://ws-tron.mcp3d.com/user/get";            

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

                    if (content.Length == 0)
                    {
                        ownerData.owner_name = string.Concat("Owner not Found matching Matic key ", ownerMaticKey);
                        returnCode = -1;
                    }
                    else
                    {
                        ownerData.owner_matic_key = ownerMaticKey;
                        JObject jsonContent = JObject.Parse(content);

                        ownerData.owner_name = jsonContent.Value<string>("avatar_name") ?? "Not Found";
                        ownerData.owner_url = citizen.AssignDefaultOwnerImg(jsonContent.Value<string>("avatar_id") ?? "");

                        ownerData.registered_date = common.TimeFormatStandard(jsonContent.Value<string>("registered"), null);
                        ownerData.last_visit = common.TimeFormatStandard(jsonContent.Value<string>("last_visited"), null);

                        ownerData.owner_offer = GetOfferLocal(true, ownerMaticKey);
                        ownerData.offer_count = ownerData.owner_offer == null ? 0 : ownerData.owner_offer.Count();

                        ownerData.owner_offer_sold = GetOfferLocal(false, ownerMaticKey);
                        ownerData.offer_sold_count = ownerData.owner_offer_sold == null ? 0 : ownerData.owner_offer_sold.Count();

                        ownerData.offer_last_updated = common.TimeFormatStandard(string.Empty, _context.ActionTimeGet(ACTION_TYPE.OFFER));

                        if (owner != null)
                        {
                            ownerData.pet_count = owner.pet_count ?? 0;
                            ownerData.citizen_count = owner.citizen_count ?? 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerMange.GetFromMaticKey() : Error on WS calls for owner matic : ", ownerMaticKey));
                    _context.LogEvent(log);
                }
            }

            return returnCode;
        }

        private int AssignUnknownOwner()
        {
            CitizenManage citizen = new(_context);

            ownerData.owner_name = "Owner not Found";
            ownerData.search_info = "Unclaimed Plot, available for purchase!";
            ownerData.owner_url = citizen.AssignDefaultOwnerImg("0");
            ownerData.plot_count = -1;

            return 0;
        }

        public OwnerAccount CheckLocalDB_OwnerTron(string tronPublic)
        {
            string maticKey = string.Empty;
            OwnerAccount ownerAccount = new();            

            // Check if passed string is valid Tron key
            if (tronPublic == "false")
            {
                ownerAccount.matic_key = "Not Found";
                return ownerAccount;
            }

            // Base58 Public Tron to Hex Conversion.
            // Span<byte> is analogous to byte[] in usage but allows the library
            // to avoid unnecessary memory copy operations unless needed.
            // you can also use "Ripple" or "Flickr" as decoder flavors            
            Span<byte> result = Base58.Bitcoin.Decode(tronPublic);
            Span<byte> resultParsed = result;
            resultParsed = resultParsed.Slice(1, result.Length - 5);
            ownerAccount.checked_matic_key = string.Concat("0x", Convert.ToHexString(resultParsed)).ToLower();

            ownerAccount = FindOwnerByMatic(ownerAccount.checked_matic_key, tronPublic);

            return ownerAccount;
        }
       
    }
   
}
