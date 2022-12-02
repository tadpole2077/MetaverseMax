using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MetaverseMax.Database
{
    public class DistrictTaxChangeDB
    {
        private readonly MetaverseMaxDbContext _context;

        public DistrictTaxChangeDB(MetaverseMaxDbContext _parentContext)
        {
            _context = _parentContext;
        }

        public int UpdateTaxChanges()
        {
            try
            {
                //exec sproc - refresh tax change records matching district table
                _context.Database.SetCommandTimeout(300);
                int result = _context.Database.ExecuteSqlRaw("EXEC dbo.sp_update_tax_change_history");    

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("DistrictTaxChangeDB::UpdateTaxChanges() : Error executing sproc sp_update_tax_change_history"));
                _context.LogEvent(log);
            }

            return 0;
        }

        public List<DistrictTaxChange> GetTaxChange(int districtId)
        {
            List<DistrictTaxChange> changeList = new();

            try
            {
                changeList = _context.districtTaxChange.Where(r => r.district_id == districtId && r.change_desc != null)
                                      .OrderByDescending(r => r.change_date)
                                      .ToList();
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("DistrictTaxChangeDB.GetTaxChange() : Error gettings records for district id : ", districtId));
                    _context.LogEvent(log);
                }
            }

            return changeList;
        }
    }

}
