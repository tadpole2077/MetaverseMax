using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
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


        public int Expire(string ownerMatic, JArray citizens)
        {
            try
            {
                bool match = false;
                List<OwnerCitizen> dbCitizenList = _context.ownerCitizen.Where(x => x.owner_matic_key == ownerMatic && x.valid_to_date == null).ToList();

                // find matching db cit in owners cit collection, if no match found then expire the db cit link 
                for (int index = 0; index < dbCitizenList.Count; index++)
                {
                    match = false;

                    for (int index2 = 0; index2 < citizens.Count; index2++)
                    {
                        if (dbCitizenList[index].citizen_token_id == (citizens[index2].Value<int?>("id") ?? 0))
                        {
                            match = true;
                            break;
                        }                        
                    }

                    if (match == false)
                    {
                        dbCitizenList[index].valid_to_date = DateTime.Now.AddDays(-1);
                    }

                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerCitizenDB.Expire() : Error with query to Expire sold/transfer citizens for owner Matic key : ", ownerMatic));
            }

            return 0;
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
                OwnerCitizen ownerCitizenExisting = _context.ownerCitizen.Where(r => r.citizen_token_id == ownerCitizen.citizen_token_id && r.valid_to_date == null).FirstOrDefault();

                // Find if record already exists, if not add it.
                if (ownerCitizenExisting == null)
                {
                    _context.ownerCitizen.Add(ownerCitizen);
                    
                }
                else if ( ownerCitizen.land_token_id != ownerCitizenExisting.land_token_id ||
                        ownerCitizen.pet_token_id != ownerCitizenExisting.pet_token_id ||
                        ownerCitizen.owner_matic_key != ownerCitizenExisting.owner_matic_key )
                {
                    // Mark prior record as expired but retain for use in Production history eval
                    ownerCitizenExisting.valid_to_date = DateTime.Now.AddDays(-1);

                    // Add new record
                    _context.ownerCitizen.Add(ownerCitizen);
                }

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
