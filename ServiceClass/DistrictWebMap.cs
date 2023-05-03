using MetaverseMax.Database;

namespace MetaverseMax.ServiceClass
{
    public class DistrictWebMap : ServiceBase
    {
        private DistrictDB districtDB;
        private Common common = new();

        public DistrictWebMap(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext, worldTypeSelected)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;

            districtDB = new(_context);
        }

        public IEnumerable<TaxChangeWeb> GetTaxChange(int districtId)
        {
            List<DistrictTaxChange> taxChangeList = new();
            DistrictTaxChangeDB districtTaxChangeDB = new(_context);
            List<TaxChangeWeb> taxChangeWebList = new();

            try
            {
                taxChangeList = districtTaxChangeDB.GetTaxChange(districtId);
                foreach (DistrictTaxChange taxChange in taxChangeList)
                {
                    taxChangeWebList.Add(new TaxChangeWeb()
                    {
                        district_id = taxChange.district_id,
                        tax_type = taxChange.tax_type,
                        tax = taxChange.tax,
                        change_date = common.DateFormatStandard(taxChange.change_date),
                        change_desc = taxChange.change_desc,
                        change_owner = taxChange.change_owner,
                        change_value = taxChange.change_value ?? 0
                    });
                }

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("DistrictWebMap::GetTaxChange() : Error for district ", districtId));
                    _context.LogEvent(log);
                }
            }

            return taxChangeWebList.ToArray();
        }

        public IEnumerable<int> GetDistrictIdList(bool isOpened)
        {
            List<int> districtIDList = new();

            try
            {
                districtIDList = districtDB.DistrictId_GetList().ToList();
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("DistrictWebMap.GetDistrictIDList() : Error "));
                    _context.LogEvent(log);
                }
            }

            return districtIDList.ToArray();
        }

        public DistrictWeb MapData_DistrictWeb(District district, District districtHistory_1Mth, bool perksDetail, bool includeTaxHistory)
        {
            DistrictWeb districtWeb, districtWebHistory = new();
            DistrictContent districtContent = new();
            TaxGraph districtTaxGraph;
            DistrictFundManage districtFundManage;
            DistrictPerkManage districtPerkManage = new(_context, worldType);
            PerkSchema perkSchema = new();
            int historyDayCount = 365;
            DateTime startGraphDate = new DateTime(2022, 9, 8);

            districtWeb = MapData_DistrictWebAttributes(district);
            districtWebHistory = MapData_DistrictWebAttributes(districtHistory_1Mth ?? district);

            districtContent = districtDB.DistrictContentGet(district.district_id);

            if (districtContent != null)
            {
                districtWeb.district_name = districtContent.district_name;
                districtWeb.promotion = districtContent.district_promotion;
                districtWeb.promotion_start = common.TimeFormatStandard("", districtContent.district_promotion_start);
                districtWeb.promotion_end = common.TimeFormatStandard("", districtContent.district_promotion_end);
            }

            // Distric Tax Current & Historic
            if (includeTaxHistory)
            {
                districtTaxGraph = new(districtWeb, districtWebHistory);
                districtFundManage = new(_context, worldType);

                districtWeb.constructTax = districtTaxGraph.Construct();
                districtWeb.produceTax = districtTaxGraph.Produce();

                // Find amount of days from start of MEGA conversion.
                if (DateTime.Today.Year == 2022)
                {
                    historyDayCount = DateTime.Today.DayOfYear - new DateTime(2022, 9, 8).DayOfYear;
                }
                else
                {
                    historyDayCount = (365 - startGraphDate.DayOfYear) + DateTime.Today.DayOfYear;
                }
                historyDayCount = historyDayCount > 365 ? 365 : historyDayCount;                // Limit Fund history graph to max of 365 days

                IEnumerable<DistrictFund> districtFundList = districtFundManage.GetHistory(district.district_id, historyDayCount);
                districtWeb.fundHistory = districtFundManage.FundChartData(districtFundList);
                districtWeb.distributeHistory = districtFundManage.DistributeChartData(districtFundList);
            }

            // District Perks
            if (perksDetail)
            {
                districtWeb.perkSchema = PerkSchema.perkList.perk;
            }
            districtWeb.districtPerk = districtPerkManage.GetPerks(district.district_id, district.update_instance);

            return districtWeb;
        }

        private DistrictWeb MapData_DistrictWebAttributes(District district)
        {
            DistrictWeb districtWeb = new();
            CitizenManage citizen = new(_context, worldType);

            districtWeb.update_instance = district.update_instance;
            districtWeb.last_update = district.last_update;
            districtWeb.last_updateFormated = common.TimeFormatStandard("", district.last_update);
            districtWeb.district_id = district.district_id;
            districtWeb.owner_name = district.owner_name ?? "Not Found";
            districtWeb.owner_avatar_id = district.owner_avatar_id ?? 0;
            districtWeb.owner_url = citizen.AssignDefaultOwnerImg(district.owner_avatar_id.ToString() ?? "0");
            districtWeb.owner_matic = district.owner_matic ?? "Not Found";

            districtWeb.active_from = common.TimeFormatStandard("", district.active_from);
            districtWeb.plots_claimed = district.plots_claimed;
            districtWeb.building_count = district.building_count;
            districtWeb.land_count = district.land_count;

            districtWeb.distribution_period = district.distribution_period ?? 0;
            districtWeb.energy_count = district.energy_plot_count ?? 0;
            districtWeb.industry_count = district.industry_plot_count ?? 0;
            districtWeb.production_count = district.production_plot_count ?? 0;
            districtWeb.office_count = district.office_plot_count ?? 0;
            districtWeb.commercial_count = district.commercial_plot_count ?? 0;
            districtWeb.residential_count = district.residential_plot_count ?? 0;
            districtWeb.municipal_count = district.municipal_plot_count ?? 0;
            districtWeb.poi_count = district.poi_plot_count ?? 0;

            districtWeb.construction_energy_tax = district.construction_energy_tax ?? 0;
            districtWeb.construction_industry_production_tax = district.construction_industry_production_tax ?? 0;
            districtWeb.construction_commercial_tax = district.construction_commercial_tax ?? 0;
            districtWeb.construction_municipal_tax = district.construction_municipal_tax ?? 0;
            districtWeb.construction_residential_tax = district.construction_residential_tax ?? 0;


            districtWeb.energy_tax = district.energy_tax ?? 0;
            districtWeb.production_tax = district.production_tax ?? 0;
            districtWeb.commercial_tax = district.commercial_tax ?? 0;
            districtWeb.citizen_tax = district.citizen_tax ?? 0;

            return districtWeb;
        }


        // Get all districts from local db : used by district_list component
        public IEnumerable<DistrictWeb> GetDistrictAll(bool isOpened, bool includeTaxHistory)
        {
            List<District> districtList = new();
            List<DistrictWeb> districtWebList = new();
            DistrictWebMap districtWebMap = new(_context, worldType);
            bool perksDetail = false;

            try
            {
                districtList = districtDB.DistrictGetAll_Latest().ToList();
                foreach (District district in districtList)
                {

                    districtWebList.Add(districtWebMap.MapData_DistrictWeb(district, null, perksDetail, includeTaxHistory));
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("DistrictWebMap::GetDistrictAll() : Error "));
                    _context.LogEvent(log);
                }
            }

            return districtWebList.ToArray();
        }
    }
}
