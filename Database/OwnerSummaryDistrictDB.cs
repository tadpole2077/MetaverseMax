using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public class OwnerSummaryDistrictDB
    {
        private readonly MetaverseMaxDbContext _context;
        public OwnerSummaryDistrictDB(MetaverseMaxDbContext _parentContext)
        {
            _context = _parentContext;
        }

        public List<OwnerSummaryDistrict> GetOwnerSummeryDistrict(int districtId, int updateInstance)
        {
            List<OwnerSummaryDistrict> ownerSummaryDistrictList = new();
            try
            {
                // Select type query using LINQ returning a collection of row matching condition - selecting first row.               
                ownerSummaryDistrictList = _context.ownerSummaryDistrict.Where(x => x.district_id == districtId && x.update_instance == updateInstance)
                    .OrderByDescending(x => x.update_instance).ThenByDescending(x => x.owned_plots).ToList();
            }
            catch (Exception ex)
            {
                string log = ex.Message;
            }

            return ownerSummaryDistrictList;
        }
    }
}
