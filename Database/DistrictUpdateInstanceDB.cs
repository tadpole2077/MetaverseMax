using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public class DistrictUpdateInstanceDB
    {
        /*
        private readonly MetaverseMaxDbContext _context;

        public DistrictUpdateInstanceDB(MetaverseMaxDbContext _parentContext)
        {
            _context = _parentContext;
        }

        public DbSet<DistrictUpdateInstance> districtUpdateInstance { get; set; }   // Links to a specific table in DB

        public IEnumerable<DistrictUpdateInstance> DistrictGetAll()
        {
            List<DistrictUpdateInstance> districtInstanceList;

            districtInstanceList = _context.districtUpdateInstance.GroupBy(x => x.district_id).FirstOrDefault().ToList();
                //OrderBy(x => x.district_id).ToList();

            return districtInstanceList.ToArray();
        }*/
    }
}
