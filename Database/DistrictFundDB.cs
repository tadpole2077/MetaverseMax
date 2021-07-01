using MetaverseMax.ServiceClass;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{

    public class DistrictFundDB
    {
        private readonly MetaverseMaxDbContext _context;

        public DistrictFundDB(MetaverseMaxDbContext _parentContext)
        {
            _context = _parentContext;
        }

        public IEnumerable<DistrictFund> GetHistory(int districtId, int historyDays)
        {
            IEnumerable<DistrictFund> districtFund = Array.Empty<DistrictFund>();
            DistrictDB distritDB = new DistrictDB(_context);
            District district;
            DateTime activeFrom;

            try
            {
                district = distritDB.DistrictGet(districtId);
                activeFrom = district.active_from ?? DateTime.MinValue;

                districtFund = _context.districtFund.Where(row => row.district_id == districtId && row.update > DateTime.Today.AddDays(-historyDays) && row.update > activeFrom)
                    .OrderBy(row => row.update).ToList();
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("DistrictFundDB::GetHistory() : Error getting Funds for : ", districtId.ToString()));
                    _context.LogEvent(log);
                }
            }

            return districtFund;
        }

        public bool UpdateDistrictFundByToken(JToken districtToken, int districtId)
        {
            DistrictFund districtFund = new();
            
            try
            {
                districtFund.district_id = districtId;
                districtFund.update = DateTime.Parse( districtToken.Value<string>("date") );

                districtFund.balance = districtToken.Value<decimal>("balance");
                districtFund.distribution = districtToken.Value<decimal>("distribution_part");
                
                _context.districtFund.Add(districtFund);

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("DistrictFundDB.UpdateDistrictFundByToken() : Error Token data : ", districtToken.ToString()));
                    _context.LogEvent(log);
                }
            }

            return true;
        }

        // After each nightly sync of Plots for each District, the funds for the prior day is also recorded.
        public bool AddOrUpdateDistrictFundByToken(JToken districtToken, int districtId)
        {
            DistrictFund districtFund = new();

            try
            {
                if (_context.districtFund.Where(r => r.district_id == districtId && r.update == DateTime.Parse(districtToken.Value<string>("date"))).ToList().Count == 0) { 
                
                    districtFund.district_id = districtId;
                    districtFund.update = DateTime.Parse(districtToken.Value<string>("date"));

                    districtFund.balance = districtToken.Value<decimal>("balance");
                    districtFund.distribution = districtToken.Value<decimal>("distribution_part");

                    _context.districtFund.Add(districtFund);

                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("DistrictFundDB.AddOrUpdateDistrictFundByToken() : Error Token data : ", districtToken.ToString()));
                    _context.LogEvent(log);
                }
            }

            return true;
        }

    }
}
