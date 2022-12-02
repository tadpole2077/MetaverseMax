using MetaverseMax.Database;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class DistrictFundManage : ServiceBase
    {
        private Common common = new();

        private DistrictDB districtDB;
        private DistrictFundDB districtFundDB;
        public IEnumerable<DistrictFund> districtFund;

        public DistrictFundManage(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
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
                    serviceUrl = "https://ws-tron.mcp3d.com/newspaper/district/info";
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

        public async Task<string> UpdateFundPriorDay(int districtId)
        {
            String content = string.Empty;
            string period = "1";
            string status = string.Empty;

            try
            {
                districtFundDB = new(_context);

                // POST REST WS
                serviceUrl = "https://ws-tron.mcp3d.com/newspaper/district/info";
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
                fundSeries.Add(new NGXChartSeries {
                    name = districtFund.update.ToString("dd-MMM-yyyy"),
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
                    name = districtFund.update.ToString("dd-MMM-yyyy"),
                    value = (int)districtFund.distribution
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
