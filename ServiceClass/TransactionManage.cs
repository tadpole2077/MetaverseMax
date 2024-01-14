using MetaverseMax.BaseClass;
using MetaverseMax.Database;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;
using System.Text;

namespace MetaverseMax.ServiceClass
{
    public class TransactionManage : ServiceBase
    {
        public TransactionManage(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext, worldTypeSelected)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;
        }

        public int InsertLog(string hash, string fromWallet, string toWallet, int unitType, int unitAmount, decimal value, int status, int blockchain, int transactionType, int tokenId)
        {


            return 0;
        }

        public OwnerTransactionWeb GetLogByMatic(string ownerMaticKey)
        {
            BlockchainTransactionDB blockchainTransactionDB = new(_context);
            ServiceCommon serviceCommon = new();
            OwnerTransactionWeb ownerTransactionWeb = new OwnerTransactionWeb();
            List<TransactionWeb> transactionList = new();

            try
            {
                List<BlockchainTransaction> ownerTransaction = blockchainTransactionDB.GetByOwnerMatic(ownerMaticKey);
            
                foreach (var transaction in ownerTransaction)
                {
                    transactionList.Add(new()
                    {
                        hash = transaction.hash,
                        action = transaction.action,
                        amount = transaction.amount,
                        event_recorded_gmt = serviceCommon.LocalTimeFormatStandardFromUTC(null, transaction.event_recorded_utc)
                    });
                }

                ownerTransactionWeb.transaction_list = transactionList;
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("TransactionManage::GetLogByMatic() : Error getting transaction log for owner matic key -  ", ownerMaticKey));
                    _context.LogEvent(log);
                }
            }
            return ownerTransactionWeb;
        }

        public string GetMCPEndpoint(string contractName)
        {            
            string address = string.Empty;

            try
            {
                JObject contractAddress = GetContractMCP().Result;
                address = contractAddress.Value<string>(contractName) ?? "";
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("TransactionManage::GetMCPEndpoint() : Error Getting MCP Contract Address for -  ", contractName));
                    _context.LogEvent(log);
                }
            }

            return new JObject( new JProperty("address", address)).ToString();
        }

        public async Task<JObject> GetContractMCP()
        {
            string content = string.Empty;
            JObject jsonContent = null;
            JObject contractAddress = new JObject();

            try
            {
                // POST REST WS
                serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.CONTRACT, WORLD_TYPE.BNB => BNB_WS.CONTRACT, WORLD_TYPE.ETH => ETH_WS.CONTRACT, _ => TRON_WS.CONTRACT};
                HttpResponseMessage response;
                using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                {
                    StringContent stringContent = new StringContent("{}", Encoding.UTF8, "application/json");

                    response = await client.PostAsync(
                        serviceUrl,
                        stringContent);

                    response.EnsureSuccessStatusCode(); // throws if not 200-299
                    content = await response.Content.ReadAsStringAsync();

                }
                watch.Stop();
                servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, string.Empty);

                if (content.Length > 0)
                {
                    jsonContent = JObject.Parse(content);

                    if (jsonContent != null)
                    {
                        JToken worldContracts = jsonContent.Value<JToken>(worldType switch { WORLD_TYPE.TRON => "Tron", WORLD_TYPE.BNB => "Bsc", WORLD_TYPE.ETH => "Eth", _ => "Eth"});
                        contractAddress = worldContracts.Value<JObject>("addresses");

                    }
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("TransactionManage::GetContractMCP() : Error Getting all MCP Contract Endpoints"));
                    _context.LogEvent(log);
                }
            }

            return contractAddress;
        }
    }
}
