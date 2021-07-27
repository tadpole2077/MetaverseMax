using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MetaverseMax.Database;
using Newtonsoft.Json.Linq;

namespace MetaverseMax.ServiceClass
{
    
    public class OwnerManage
    {
        private readonly MetaverseMaxDbContext _context;
        private static Dictionary<string, string> ownersList = new();
        public OwnerData ownerData = new();
        private Common common = new();

        public OwnerManage(MetaverseMaxDbContext _contextService)
        {
            _context = _contextService;

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

        public int UpdateAllOffers()
        {            
            int returnCode = 0;
            OwnerOfferDB ownerOfferDB = new(_context);

            try
            {
                // Refresh list after nightly sync                
                GetOwners(true);

                ownerOfferDB.SetOffersInactive();

                foreach (string maticKey in ownersList.Keys)
                {
                    GetOwnerOffer(true, maticKey);
                }                
            }
            catch (Exception ex)
            {
                string log = ex.Message;
            }

            return returnCode;
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

        public int GetOwnerOffer(bool activeOffer, string maticKey)
        {
            String content = string.Empty;
            OwnerOffer ownerOffer = new();
            OwnerOfferDB ownerOfferDB = new(_context);
            int returnCode = 0;

            try
            {
                // POST from Land/Get REST WS
                byte[] byteArray = Encoding.ASCII.GetBytes("{\"address\": \"" + maticKey + "\"}");
                WebRequest request = WebRequest.Create("https://ws-tron.mcp3d.com/sales/offers");
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
                if (content.Length > 0) {
                    
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

            return returnCode;            
        }

        public OwnerData GetOwnerLands(string ownerMaticKey)
        {
            string content = string.Empty;            

            byte[] byteArray = Encoding.ASCII.GetBytes("{\"address\": \"" + ownerMaticKey + "\",\"short\": false}");
            Building building = new();
            Citizen citizen = new();

            WebRequest request = WebRequest.Create("https://ws-tron.mcp3d.com/user/assets/lands");
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
            JObject jsonContent = JObject.Parse(content);
            JArray lands = jsonContent["items"] as JArray;
            if (lands != null && lands.Count > 0)
            {
                JToken land = lands.Children().First();

                ownerData.owner_matic_key = land.Value<string>("owner") ?? "Not Found";
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
                        last_action = common.UnixTimeStampToDateTime(landInstance.Value<double?>("last_action"), "Empty Plot"),
                        token_id = landInstance.Value<int?>("token_id") ?? 0,
                        building_level = landInstance.Value<int?>("building_level") ?? 0,
                        citizen_count = citizen.GetCitizenCount(landInstance.Value<JArray>("citizens")),
                        citizen_url = citizen.GetCitizenUrl(landInstance.Value<JArray>("citizens")),
                        citizen_stamina = citizen.GetLowStamina(landInstance.Value<JArray>("citizens")),
                        citizen_stamina_alert = citizen.CheckCitizenStamina(landInstance.Value<JArray>("citizens"), landInstance.Value<int?>("building_type_id") ?? 0),
                        forsale_price = building.GetSalePrice(landInstance.Value<JToken>("sale_data"))
                    })
                        .OrderBy(row => row.district_id).ThenBy(row => row.pos_x).ThenBy(row => row.pos_y);


                    ownerData.developed_plots = ownerData.owner_land.Where(
                        row => row.last_action != "Empty Plot"
                        ).Count();

                    // Get Last Action across all lands for target player
                    ownerData.last_action = string.Concat(common.UnixTimeStampToDateTime(ownerData.owner_land.Max(row => row.last_actionUx), "No Lands"), " GMT");

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
                if (string.Equals(ownerMaticKey, "Owner not Found"))
                {
                    ownerData.last_action = "Empty Plot, It could be Yours today!";
                }
                else
                {
                    ownerData.last_action = "This player owns no land plots in Tron World";
                }
            }

            return ownerData;
        }
        
        public int GetFromLandCoord(int posX, int posY)
        {
            String content = string.Empty;
            Citizen citizen = new();
            int returnCode = 0;

            try
            {
                // POST from Land/Get REST WS
                byte[] byteArray = Encoding.ASCII.GetBytes("{\"x\": \"" + posX + "\",\"y\": \"" + posY + "\"}");
                WebRequest request = WebRequest.Create("https://ws-tron.mcp3d.com/land/get");
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
                if (content.Length == 0)
                {
                    ownerData.owner_name = "Plot does not exist";
                    returnCode = -1;
                }
                else
                {
                    JObject jsonContent = JObject.Parse(content);
                    ownerData.owner_matic_key = jsonContent.Value<string>("owner") ?? "";

                    returnCode = GetFromMaticKey(ownerData.owner_matic_key);
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
            }

            return returnCode;
        }

        public int GetFromMaticKey(string ownerMaticKey)
        {
            String content = string.Empty;
            Citizen citizen = new();
            byte[] byteArray;
            WebRequest request;
            Stream dataStream;
            int returnCode = 0;

            try
            {
                OwnerOfferDB ownerOfferDB = new(_context);

                //ownerData.wallet_public = WalletConvert(jsonContent.Value<string>("owner") ?? string.Empty);
                if (string.IsNullOrEmpty(ownerMaticKey) || ownerMaticKey.Equals("Not Found"))
                {
                    AssignUnknownOwner();
                    returnCode = -1;
                }
                else
                {
                    // POST from User/Get REST WS
                    byteArray = Encoding.ASCII.GetBytes("{\"address\": \"" + ownerMaticKey + "\",\"dapper\": false,\"sign\": null }");
                    request = WebRequest.Create("https://ws-tron.mcp3d.com/user/get");
                    request.Method = "POST";
                    request.ContentType = "application/json";

                    dataStream = request.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();

                    // Ensure correct dispose of WebRespose IDisposable class even if exception
                    using (WebResponse response = request.GetResponse())
                    {
                        StreamReader reader = new(response.GetResponseStream());
                        content = reader.ReadToEnd();
                    }

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

                    }
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
            }

            return returnCode;
        }

        public IEnumerable<Pet> GetPetMCP(string ownerMaticKey)
        {
            PetDB petDB = new(_context);
            String content = string.Empty;
            int returnCode = 0;
            List<Pet> petList = new();

            try
            {
                // POST from Land/Get REST WS
                byte[] byteArray = Encoding.ASCII.GetBytes("{\"address\": \"" + ownerMaticKey + "\",\"filter\": {\"qualifications\":0}}");
                WebRequest request = WebRequest.Create("https://ws-tron.mcp3d.com/user/assets/pets");
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

                if (content.Length == 0)
                {
                    returnCode = -1;
                }
                else
                {
                    JArray pets = JArray.Parse(content);

                    for (int index = 0; index < pets.Count; index++)
                    {
                        petList.Add(new Pet() {
                            token_owner_matic_key = ownerMaticKey,
                            pet_id = pets[index].Value<int?>("pet_id") ?? 0,
                            bonus_id = pets[index].Value<int?>("bonus_id") ?? 0,
                            bonus_level = pets[index].Value<int?>("bonus_level") ?? 0,
                            pet_look = pets[index].Value<int?>("look") ?? 0
                        });
                    }

                    petDB.AddorUpdate(petList, ownerMaticKey);
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerMange.GetPet() : Error on WS calls for owner matic : ", ownerMaticKey));
                    _context.LogEvent(log);
                }
            }

            return petList.ToArray();
        }

        private int AssignUnknownOwner()
        {
            Citizen citizen = new();

            ownerData.owner_name = "Owner not Found";
            ownerData.last_action = "Empty Plot, It could be Yours today!";
            ownerData.owner_url = citizen.AssignDefaultOwnerImg("0");
            ownerData.plot_count = -1;

            return 0;
        }
    }
   
}
