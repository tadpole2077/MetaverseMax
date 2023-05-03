using MetaverseMax.Database;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Text;

namespace MetaverseMax.ServiceClass
{
    public class DistrictFundManage : ServiceBase
    {
        private Common common = new();

        private DistrictDB districtDB;
        private DistrictFundDB districtFundDB;
        public IEnumerable<DistrictFund> districtFund;

        public DistrictFundManage(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext, worldTypeSelected)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;
        }

        public IEnumerable<DistrictFund> GetHistory(int districtId, int daysHistory)
        {
            IEnumerable<DistrictFund> districtFundList = null;
            districtFund = Array.Empty<DistrictFund>();
            try
            {
                districtFundDB = new(_context);
                districtFundList = districtFundDB.GetHistory(districtId, daysHistory);
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("DistrictFundManage::GetHistory() : Error updating district_id = ", districtId));
                _context.LogEvent(log);
            }


            return districtFundList;
        }

        public async Task<string> UpdateFundAll(QueryParametersSecurity parameters)
        {
            String content = string.Empty;
            string period = "365";
            List<int> districtIDList;
            int districtId = 0;
            string status = string.Empty;

            try
            {
                // As this service could be abused as a DDOS a security token is needed.
                if (parameters.secure_token == null || !parameters.secure_token.Equals("JUST_SIMPLE_CHECK123"))
                {
                    return status;
                }

                districtFundDB = new(_context);
                districtDB = new(_context);
                districtIDList = districtDB.DistrictId_GetList().ToList();

                for (int listIndex = 0; listIndex < districtIDList.Count; listIndex++)
                {
                    districtId = districtIDList[listIndex];

                    // POST REST WS                    
                    serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.DISTRICT_INFO, WORLD_TYPE.BNB => BNB_WS.DISTRICT_INFO, WORLD_TYPE.ETH => ETH_WS.DISTRICT_INFO, _ => TRON_WS.DISTRICT_INFO };

                    HttpResponseMessage response;
                    using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                    {
                        StringContent stringContent = new StringContent("{\"id\": \"" + districtId + "\",\"period\": " + period + " }", Encoding.UTF8, "application/json");

                        response = await client.PostAsync(
                            serviceUrl,
                            stringContent);

                        response.EnsureSuccessStatusCode(); // throws if not 200-299
                        content = await response.Content.ReadAsStringAsync();

                    }
                    watch.Stop();
                    servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, districtId.ToString());

                    if (content.Length == 0)
                    {
                        status = string.Concat("No Reponse from MCP service for district ID", districtId);
                    }
                    else
                    {
                        JObject jsonContent = JObject.Parse(content);
                        JArray districtFund = jsonContent.Value<JArray>("fund");

                        for (int index = 0; index < districtFund.Count; index++)
                        {
                            districtFundDB.UpdateDistrictFundByToken(districtFund[index], districtId);
                        }
                        _context.SaveChanges();
                    }
                }
                status = string.Concat("Update All funding for ", districtIDList.Count, " districts");
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("DistrictFund::UpdateFundAll() : Error updating district_id = ", districtId));
                _context.LogEvent(log);
            }

            return status;
        }

        public int UpdateTaxChanges()
        {
            int returnCode = 0;
            try
            {
                DistrictTaxChangeDB districtTaxChangeDB = new(_context);
                returnCode = districtTaxChangeDB.UpdateTaxChanges();
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("DistrictFund::UpdateTaxChanges() : Error occured "));
                _context.LogEvent(log);
            }

            return returnCode;
        }

        public int DistributionUpdateMaster(string secureToken, int interval, DISTRIBUTE_ACTION distributeAction)
        {
            int totalProcessed = 0;
            MetaverseMaxDbContext processContext = null;
            bool delayRepeat = false;

            try
            {               
                // As this service could be abused as a DDOS a security token is needed.
                if (!secureToken.Equals("JUST_SIMPLE_CHECK123"))
                {
                    return totalProcessed;
                }

                if (distributeAction == DISTRIBUTE_ACTION.GET_DISTRICT_FUND)
                {
                    DistrubutionUpdatePerWorld(interval, distributeAction, WORLD_TYPE.TRON, ref totalProcessed);
                    DistrubutionUpdatePerWorld(interval, distributeAction, WORLD_TYPE.BNB, ref totalProcessed);
                    DistrubutionUpdatePerWorld(interval, distributeAction, WORLD_TYPE.ETH, ref totalProcessed);
                }
                else if (distributeAction == DISTRIBUTE_ACTION.CALC_DISTRICT_DISTRIBUTION)
                {
                    delayRepeat = DistributionUpdateCalc(interval, WORLD_TYPE.TRON, ref totalProcessed);
                    if (!delayRepeat)
                    {
                        delayRepeat = DistributionUpdateCalc(interval, WORLD_TYPE.BNB, ref totalProcessed);
                    }
                    if (!delayRepeat) {
                        delayRepeat = DistributionUpdateCalc(interval, WORLD_TYPE.ETH, ref totalProcessed);
                    }
                }

                processContext = new MetaverseMaxDbContext(WORLD_TYPE.TRON);
                processContext.LogEvent(String.Concat("DistrictFund::DistributionUpdate() : total updated :", totalProcessed));
                processContext.Dispose();
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("DistrictFund::DistributionUpdate() : Error occured "));
                _context.LogEvent(log);
            }

            return totalProcessed;
        }

        public bool DistributionUpdateCalc(int interval, WORLD_TYPE worldTypeToProcess, ref int totalProcessed)
        {
            bool delayRepeat = false;
            int repeatInstance = 0;
            JToken districtFund = null;
            MetaverseMaxDbContext processContext = null;            
            do
            {
                worldType = worldTypeToProcess;         // needed by WS call to select correct L3 service set
                base._context.worldTypeSelected = worldTypeToProcess;
                processContext = new MetaverseMaxDbContext(worldTypeToProcess);
                districtFundDB = new(processContext);
                servicePerfDB = new(processContext);

                // Process Gobal fund totals
                // Check funds have changed on CALC_GLOBAL_DISTRIBUTION event, if not repeat with delay of 1 minute, after 10 attempts quit without update
                districtFund = GetBalanceMCP(0, interval).Result;

                if (districtFund != null)
                {
                    if (districtFund.Value<decimal>("global") == 0) {
                        delayRepeat = true;
                    }
                    else
                    {
                        delayRepeat = districtFundDB.UpdateDistrictFundByValue(0, districtFund.Value<decimal>("global"), DISTRIBUTE_ACTION.CALC_GLOBAL_DISTRIBUTION);
                    }
                }

                // No distribution found - refresh all current worlds { districts and global } wait and repeat. 4hr window - 10 minute intervals
                if (delayRepeat)
                {
                    totalProcessed = 0;
                    // Refresh stored fund totals for all districts and world across all worlds
                    if (worldTypeToProcess == WORLD_TYPE.TRON)
                    {
                        DistrubutionUpdatePerWorld(interval, DISTRIBUTE_ACTION.REFRESH, WORLD_TYPE.TRON, ref totalProcessed);
                        DistrubutionUpdatePerWorld(interval, DISTRIBUTE_ACTION.REFRESH, WORLD_TYPE.BNB, ref totalProcessed);
                        DistrubutionUpdatePerWorld(interval, DISTRIBUTE_ACTION.REFRESH, WORLD_TYPE.ETH, ref totalProcessed);
                    }
                    else if (worldTypeToProcess == WORLD_TYPE.BNB)
                    {
                        DistrubutionUpdatePerWorld(interval, DISTRIBUTE_ACTION.REFRESH, WORLD_TYPE.BNB, ref totalProcessed);
                        DistrubutionUpdatePerWorld(interval, DISTRIBUTE_ACTION.REFRESH, WORLD_TYPE.ETH, ref totalProcessed);
                    }
                    else if (worldTypeToProcess == WORLD_TYPE.ETH)
                    {
                        DistrubutionUpdatePerWorld(interval, DISTRIBUTE_ACTION.REFRESH, WORLD_TYPE.ETH, ref totalProcessed);
                    }

                    repeatInstance++;
                    processContext.LogEvent(String.Concat("DistrictFund::DistrubutionUpdatePerWorld() : distribute calc [delay-repeat] event instance : ", repeatInstance, " total refreshed :", totalProcessed));
                    processContext.SaveChanges();
                    processContext.Dispose();

                    Task.Delay(900000).Wait();      // Wait 15 minutes                    
                }
                else
                {
                    totalProcessed = 0;
                    DistrubutionUpdatePerWorld(interval, DISTRIBUTE_ACTION.CALC_DISTRICT_DISTRIBUTION, worldTypeToProcess, ref totalProcessed);
                    totalProcessed++;
                }
            }
            while (delayRepeat && repeatInstance < Common.jobFundRepeatCount);

            if (!processContext.IsDisposed())
            {
                processContext.SaveChanges();
                processContext.Dispose();
            }

            return delayRepeat;
        }


        public bool DistrubutionUpdatePerWorld(int interval, DISTRIBUTE_ACTION distributeAction, WORLD_TYPE worldTypeToProcess, ref int totalProcessed)
        {
            List<int> districtIDList;
            MetaverseMaxDbContext processContext = null;
            int districtId;
            JToken districtFund = null;
            bool delayRepeat = false;            

            try
            {
                worldType = worldTypeToProcess;
                base._context.worldTypeSelected = worldTypeToProcess;
                processContext = new MetaverseMaxDbContext(worldTypeToProcess);

                districtFundDB = new(processContext);
                districtDB = new(processContext);
                servicePerfDB = new(processContext);

                districtIDList = districtDB.DistrictId_GetList().ToList();

                // Process Global only on Get & Refresh not on Calc.
                if (distributeAction == DISTRIBUTE_ACTION.GET_DISTRICT_FUND || distributeAction == DISTRIBUTE_ACTION.REFRESH)
                {
                    // Process Global fund totals
                    districtFund = GetBalanceMCP(0, interval).Result;
                    if (districtFund != null)
                    {
                        districtFundDB.UpdateDistrictFundByValue(0, districtFund.Value<decimal>("global"), distributeAction == DISTRIBUTE_ACTION.REFRESH ? DISTRIBUTE_ACTION.REFRESH_GLOBAL_FUND : distributeAction);
                        totalProcessed++;
                    }
                }                

                // Process district funds
                for (int listIndex = 0; listIndex < districtIDList.Count; listIndex++)
                {
                    districtId = districtIDList[listIndex];

                    districtFund = GetBalanceMCP(districtId, interval).Result;

                    if (districtFund != null)
                    {
                        districtFundDB.UpdateDistrictFundByValue(districtId, districtFund.Value<decimal>("region"), distributeAction == DISTRIBUTE_ACTION.REFRESH ? DISTRIBUTE_ACTION.REFRESH_DISTRICT_FUND : distributeAction); 
                        totalProcessed++;
                    }
                }

                processContext.SaveChanges();
                processContext.Dispose();
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("DistrictFund::DistrubutionUpdatePerWorld() : Error occured "));
                _context.LogEvent(log);

                if (processContext != null && processContext.IsDisposed() == false)
                {
                    processContext.Dispose();
                }
            }

            return delayRepeat;
        }

        public async Task<JToken> GetBalanceMCP(int districtId, int interval)
        {
            String content = string.Empty;
            JObject jsonContent = null;
            JToken districtFund = null;
            StringContent stringContent;

            // POST REST WS                    
            serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.BALANCES, WORLD_TYPE.BNB => BNB_WS.BALANCES, WORLD_TYPE.ETH => ETH_WS.BALANCES, _ => TRON_WS.BALANCES };

            HttpResponseMessage response;
            using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
            {
                if (districtId > 0)
                {
                    stringContent = new StringContent("{\"region_id\": \"" + districtId + "\" }", Encoding.UTF8, "application/json");
                }
                else
                {
                    stringContent = new StringContent("{}", Encoding.UTF8, "application/json");
                }

                response = await client.PostAsync(
                    serviceUrl,
                    stringContent);

                response.EnsureSuccessStatusCode(); // throws if not 200-299
                content = await response.Content.ReadAsStringAsync();

            }
            watch.Stop();
            servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, districtId.ToString());

            if (content.Length == 0)
            {
                _context.LogEvent(string.Concat("DistrubutionUpdatePerWorld : No Reponse from MCP Balances service for district ID ", districtId));
            }
            else
            {
                jsonContent = JObject.Parse(content);
                districtFund = jsonContent.Value<JToken>("banks");
            }

            await Task.Delay(interval);

            return districtFund;
        }

        public async Task<string> UpdateFundPriorDay(int districtId)
        {
            String content = string.Empty;
            string period = "1";
            string status = string.Empty;

            try
            {
                districtFundDB = new(_context);

                // POST REST WS                
                serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.DISTRICT_INFO, WORLD_TYPE.BNB => BNB_WS.DISTRICT_INFO, WORLD_TYPE.ETH => ETH_WS.DISTRICT_INFO, _ => TRON_WS.DISTRICT_INFO };

                HttpResponseMessage response;
                using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                {
                    StringContent stringContent = new StringContent("{\"id\": \"" + districtId + "\",\"period\": " + period + " }", Encoding.UTF8, "application/json");

                    response = await client.PostAsync(
                        serviceUrl,
                        stringContent);

                    response.EnsureSuccessStatusCode(); // throws if not 200-299
                    content = await response.Content.ReadAsStringAsync();

                }
                watch.Stop();
                servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, districtId.ToString());

                if (content.Length == 0)
                {
                    status = string.Concat("No Response from MCP service for district ID", districtId);
                }
                else
                {
                    JObject jsonContent = JObject.Parse(content);
                    JArray districtFund = jsonContent.Value<JArray>("fund");

                    for (int index = 0; index < districtFund.Count; index++)
                    {
                        districtFundDB.AddOrUpdateDistrictFundByToken(districtFund[index], districtId);
                    }
                    _context.SaveChanges();
                }

                status = string.Concat("Update Prior Day funding for ", districtId, " districts");
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("DistrictFund::UpdateFundPriorDay() : Error updating district_id = ", districtId));
                _context.LogEvent(log);
            }

            return status;
        }


        public NgxChart FundChartData(IEnumerable<DistrictFund> districtFundList)
        {
            NgxChart ngxChart = new()
            {

                domain = new string[] { "#7AA3E5", "#A8385D", "#A27EA8" },

                legend_title = "Local",

                x_axis_label = "Date",

                y_axis_label = "MEGA",

                y_axis_postappend = " MEGA",

                view = new int[] { 500, 200 },

                show_legend = false,

                show_yaxis_label = false,

                show_xaxis_label = false
            };


            List<NGXChartSeries> fundSeries = new();

            foreach (DistrictFund districtFund in districtFundList)
            {
                fundSeries.Add(new NGXChartSeries
                {
                    name = districtFund.last_updated.ToString("dd-MMM-yyyy"),
                    value = (int)districtFund.balance
                });
            }

            ngxChart.graphColumns = new NGXGraphColumns[1]
            {
                new NGXGraphColumns(){
                    name = "Fund",
                    series = fundSeries
                }
            };

            return ngxChart;
        }

        public NgxChart DistributeChartData(IEnumerable<DistrictFund> districtFundList)
        {
            NgxChart ngxChart = new()
            {

                domain = new string[] { "#A8385D", "#7AA3E5", "#A27EA8" },

                legend_title = "Fund",

                x_axis_label = "Date",

                y_axis_label = "Trx",

                y_axis_postappend = " Trx1",

                view = new int[] { 500, 200 },

                show_legend = false,

                show_yaxis_label = false,

                show_xaxis_label = false
            };


            List<NGXChartSeries> DistributionSeries = new();

            foreach (DistrictFund districtFund in districtFundList)
            {
                DistributionSeries.Add(new NGXChartSeries
                {
                    name = districtFund.last_updated.ToString("dd-MMM-yyyy"),
                    value = (decimal)Math.Round(districtFund.distribution, 1, MidpointRounding.AwayFromZero)
                });
            }

            ngxChart.graphColumns = new NGXGraphColumns[1]
            {
                new NGXGraphColumns()
                {
                    name = "Distribution",
                    series = DistributionSeries
                }
            };

            return ngxChart;
        }
    }
}
