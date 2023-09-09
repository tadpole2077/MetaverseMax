using Newtonsoft.Json.Linq;
using MetaverseMax.BaseClass;

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
            DistrictDB districtDB = new DistrictDB(_context);
            District district;
            DateTime activeFrom;

            try
            {
                district = districtDB.DistrictGet(districtId);
                activeFrom = district.active_from ?? DateTime.MinValue;

                districtFund = _context.districtFund.Where(row => row.district_id == districtId && row.last_updated > DateTime.Today.AddDays(-historyDays) && row.last_updated > activeFrom)
                    .OrderBy(row => row.last_updated).ToList();
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
                districtFund.last_updated = DateTime.Parse(districtToken.Value<string>("date"));

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

        public bool UpdateDistrictFundByValue(int districtId, decimal balance, DISTRIBUTE_ACTION distributeAction)
        {
            DistrictFund districtFund = new();
            bool delayRepeat = false;

            try
            {
                districtFund.district_id = districtId;

                balance = _context.worldTypeSelected switch
                {
                    WORLD_TYPE.TRON => balance / 1000000000000000000,       // 18 places back
                    WORLD_TYPE.BNB => balance / 1000000000000000000,        // 18 places back
                    WORLD_TYPE.ETH => balance / 1000000000000000000,        // 18 places back
                    _ => balance / 1000000
                };                

                if (distributeAction == DISTRIBUTE_ACTION.GET_DISTRICT_FUND || distributeAction == DISTRIBUTE_ACTION.GET_GLOBAL_FUND)
                {
                    districtFund.last_updated = DateTime.UtcNow;
                    districtFund.distribution = 0;
                    districtFund.balance = balance;

                    _context.districtFund.Add(districtFund);
                }
                else if (distributeAction == DISTRIBUTE_ACTION.REFRESH_DISTRICT_FUND || distributeAction == DISTRIBUTE_ACTION.REFRESH_GLOBAL_FUND)
                {
                    districtFund = _context.districtFund.Where(x => x.district_id == districtId).OrderByDescending(x => x.fund_key).FirstOrDefault();

                    // Add check if other world updated first before target world - then refresh should be skipped on this fund update to not to reset funds to distributed value
                    // - wait until (a) Bnb distribute processed then (b) bnb world distribute processed then finally (c) eth world, achived  be checking balance is greater then last stored.
                    if (districtFund != null && (long)balance >= (long)districtFund.balance)        
                    {
                        districtFund.last_updated = DateTime.UtcNow;
                        districtFund.distribution = 0;
                        districtFund.balance = balance;

                        _context.districtFund.Update(districtFund);
                    }
                }
                else if (distributeAction == DISTRIBUTE_ACTION.CALC_DISTRICT_DISTRIBUTION)
                {                    
                    districtFund = _context.districtFund.Where(x => x.district_id == districtId).OrderByDescending(x => x.fund_key).FirstOrDefault();
                    
                    if (districtFund != null)
                    {
                        districtFund.last_updated = DateTime.UtcNow;
                        districtFund.distribution = districtFund.balance - balance;
                        districtFund.balance = balance;


                        _context.districtFund.Update(districtFund);
                    }                   
                }
                else if (distributeAction == DISTRIBUTE_ACTION.CALC_GLOBAL_DISTRIBUTION)
                {
                    districtFund = _context.districtFund.Where(x => x.district_id == districtId).OrderByDescending(x => x.fund_key).FirstOrDefault();

                    // if new balance is less then prior balance then distribution has occured - record distribution.
                    if (districtFund != null && (long)districtFund.balance > (long)balance)
                    {
                        districtFund.last_updated = DateTime.UtcNow;
                        districtFund.distribution = districtFund.balance - balance;
                        districtFund.balance = balance;


                        _context.districtFund.Update(districtFund);
                    }
                    else
                    {
                        delayRepeat = true;
                    }
                }

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("DistrictFundDB.UpdateDistrictFundByValue() : Error ocurred with District : ", districtId.ToString()));
                    _context.LogEvent(log);
                }
            }

            return delayRepeat;
        }

        // After each nightly sync of Plots for each District, the funds for the prior day is also recorded.
        public bool AddOrUpdateDistrictFundByToken(JToken districtToken, int districtId)
        {
            DistrictFund districtFund = new();

            try
            {
                if (_context.districtFund.Where(r => r.district_id == districtId && r.last_updated == DateTime.Parse(districtToken.Value<string>("date"))).ToList().Count == 0)
                {

                    districtFund.district_id = districtId;
                    districtFund.last_updated = DateTime.Parse(districtToken.Value<string>("date"));

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
