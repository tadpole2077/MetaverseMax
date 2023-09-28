using MetaverseMax.BaseClass;
using MetaverseMax.Database;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Text;

namespace MetaverseMax.ServiceClass
{
    public class TokenHistory : ServiceBase
    {
        public TokenHistory(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext, worldTypeSelected)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;
        }


        public TokenLastAction GetLastAction(int tokenId, TOKEN_TYPE tokenType)
        {
            TokenLastAction action = new();
            JArray history = Task.Run(() => GetMCP(tokenId, tokenType)).Result;

            if (history.Count > 0)
            {
                JToken historyItem = history[0];
                action.eventTime = historyItem.Value<DateTime?>("event_time") ?? DateTime.UtcNow;
                string eventType = historyItem.Value<string>("type") ?? string.Empty;

                JObject itemData = historyItem.Value<JObject>("data");
                string formerOwner = historyItem.Value<string>("from") ?? string.Empty;

                action.eventType = eventType switch {
                    "" => EVENT_TYPE.UNKNOWN,
                    "market/create" => EVENT_TYPE.LISTED_ON_MARKET,
                    "erc721/parcel/transfer" => formerOwner == "0x0000000000000000000000000000000000000000" ? EVENT_TYPE.CREATED : EVENT_TYPE.TRANSFERED_TO_NEW_OWNER,
                    "units/minted" => EVENT_TYPE.UNITS_MINTED,
                    "custom/build" => EVENT_TYPE.CUSTOM_BUILD,
                    _ => EVENT_TYPE.UNKNOWN,
                };
            }

            return action;
        }

        public async Task<JArray> GetMCP(int tokenId, TOKEN_TYPE tokenType)
        {
            JArray history = null;
            HttpResponseMessage response;
            string content = string.Empty;
            string serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.ASSETS_HISTORY, WORLD_TYPE.BNB => BNB_WS.ASSETS_HISTORY, WORLD_TYPE.ETH => ETH_WS.ASSETS_HISTORY, _ => TRON_WS.ASSETS_HISTORY };

            try
            {
                using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                {
                    StringContent stringContent = new StringContent("{\"token_id\": " + tokenId + ",\"token_type\": " + (int)tokenType + "}", Encoding.UTF8, "application/json");

                    response = await client.PostAsync(
                        serviceUrl,
                        stringContent);

                    response.EnsureSuccessStatusCode(); // throws if not 200-299
                    content = await response.Content.ReadAsStringAsync();

                }
                watch.Stop();

                servicePerfDB.AddServiceEntry(tokenType switch { TOKEN_TYPE.PLOT => "Building - ", _ => "Token History - " } + serviceUrl, 
                    serviceStartTime, 
                    watch.ElapsedMilliseconds, 
                    content.Length, 
                    tokenId.ToString());

                history = JArray.Parse(content);

            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("TokenHistory.GetMCP() : Error on WS calls for asset id : ", tokenId, " token_type : ", (int)tokenType));
            }

            return history;
        }
    }
}
