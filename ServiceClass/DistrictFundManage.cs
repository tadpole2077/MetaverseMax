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
    public class DistrictFundManage
    {
        private readonly MetaverseMaxDbContext _context;
        private Common common = new();

        private DistrictDB districtDB;
        private DistrictFundDB districtFundDB;
        public IEnumerable<DistrictFund> districtFund;

        public DistrictFundManage(MetaverseMaxDbContext _contextService)
        {
            _context = _contextService;
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

        public string UpdateFundAll(QueryParametersSecurity parameters)
        {
            String content = string.Empty;
            byte[] byteArray;
            WebRequest request;
            Stream dataStream;
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

                    // POST from User/Get REST WS
                    byteArray = Encoding.ASCII.GetBytes("{\"id\": \"" + districtId + "\",\"period\": " + period + " }");
                    request = WebRequest.Create("https://ws-tron.mcp3d.com/newspaper/district/info");
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

        public string UpdateFundPriorDay(int districtId)
        {
            String content = string.Empty;
            byte[] byteArray;
            WebRequest request;
            Stream dataStream;
            string period = "1";
            string status = string.Empty;

            try
            {
                districtFundDB = new(_context);

                // POST from User/Get REST WS
                byteArray = Encoding.ASCII.GetBytes("{\"id\": \"" + districtId + "\",\"period\": " + period + " }");
                request = WebRequest.Create("https://ws-tron.mcp3d.com/newspaper/district/info");
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
                    status = string.Concat("No Reponse from MCP service for district ID", districtId);
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

                y_axis_label = "Trx",

                y_axis_postappend = " Trx",

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
