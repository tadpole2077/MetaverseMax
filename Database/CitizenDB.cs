using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public class CitizenDB : DatabaseBase
    {
        public CitizenDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {            
        }

        public int AddorUpdate(Citizen citizen, bool saveFlag)
        {
            try
            {
                List<Citizen> citizenList = _context.citizen.Where(r => r.token_id == citizen.token_id).ToList();
                // Find if record already exists, if not add it.
                if (citizenList.Count == 0)
                {
                    _context.citizen.Add(citizen);

                    if (saveFlag)
                    {
                        _context.SaveChanges();
                    }
                }
                else if(citizenList[0].sex == null)
                {
                    citizenList[0].sex = citizen.sex;

                    if (saveFlag)
                    {
                        _context.SaveChanges();
                    }
                }

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("CitizenDB.AddorUpdate() : Error adding record to db with citizen token_id : ", citizen.token_id));
            }

            return 0;
        }

    }
}
