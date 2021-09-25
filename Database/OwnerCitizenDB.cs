using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MetaverseMax.Database
{
    public class OwnerCitizenDB : DatabaseBase
    {

        public OwnerCitizenDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {     
        }

        public List<OwnerCitizenExt> GetCitizen(string ownerMatic)
        {
            List<OwnerCitizenExt> citizens = new();

            try
            {
                // Using string interpolation syntax to pull in parameters
                citizens = _context.OwnerCitizenExt.FromSqlInterpolated($"sp_owner_citizen_get {ownerMatic}").AsNoTracking().ToList();                

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerCitizenDB.GetCitizen() : Error with query to get all owner citizens with Matic key : ", ownerMatic));    
            }

            return citizens;
        }

        public int RemoveOwnerLink(string ownerMatic)
        {
            try
            {
                _context.ownerCitizen.RemoveRange(
                    _context.ownerCitizen.Where(x => x.owner_matic_key == ownerMatic && x.link_date == DateTime.Today.Date));

                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerCitizenDB.RemoveOwnerLink() : Error with query to remove OwnerCitizen links with Matic key : ", ownerMatic));
            }

            return 0;

        }

        public int AddorUpdate(OwnerCitizen ownerCitizen, bool saveFlag)
        {
            try
            {
                // Find if record already exists, if not add it.                
                _context.ownerCitizen.Add(ownerCitizen);

                if (saveFlag)
                {
                    _context.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerCitizenDB.AddorUpdate() : Error adding record to db with citizen token_id : ", ownerCitizen.citizen_token_id));             
            }

            return 0;
        }

        public int UpdateCitizenCount()
        {
            int result = 0;
            try
            {
                //exec sproc - update Owner table with count of pets per owner.
                result = _context.Database.ExecuteSqlRaw("EXEC dbo.sp_citizen_count");

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerCitizenDB::UpdateCitizenCount() : Error updating owner citizen count using sproc sp_citizen_count"));
            }

            return result;
        }

    }
}
