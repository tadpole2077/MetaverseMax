using MetaverseMax.ServiceClass;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MetaverseMax.Database
{
    public class OwnerCitizenDB : DatabaseBase
    {
        public OwnerCitizenDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
            worldType = _parentContext.worldTypeSelected;
        }
        public OwnerCitizenDB(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;
        }

        public List<OwnerCitizen> GetOwnerCitizenByOwnerMatic(string ownerMatic, DateTime? validToDate)
        {
            List<OwnerCitizen> dbCitizenList = null;
            try
            {
                dbCitizenList = _context.ownerCitizen.Where(x => x.owner_matic_key == ownerMatic && x.valid_to_date == validToDate).ToList();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerCitizenDB.GetOwnerCitizenByOwnerMatic() : Error with query to get citizens by owner Matic key : ", ownerMatic));
            }

            return dbCitizenList;
        }

        // Defensive coding method - remove any OwnerCitizen links matching todays date (used if DataSync ran twice in one day)
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

        public OwnerCitizen GetExistingOwnerCitizen(int tokenId)
        {
            OwnerCitizen ownerCitizenExisting = null;
            List<OwnerCitizen> ownerCitizenExistingList = null;
            DateTime? validToDate;

            try
            {
                // Check if Data anomoly - multiple active citizen link - log and fix.
                ownerCitizenExistingList = _context.ownerCitizen.Where(r => r.citizen_token_id == tokenId && r.valid_to_date == null).OrderByDescending(x => x.link_date).ToList();
                ownerCitizenExisting = ownerCitizenExistingList.Count > 0 ? ownerCitizenExistingList[0] : null;

                if (ownerCitizenExistingList.Count > 1)
                {
                    validToDate = ownerCitizenExisting.link_date;
                    for (int i = 1; i < ownerCitizenExistingList.Count; i++)
                    {
                        ownerCitizenExistingList[i].valid_to_date = validToDate;
                        validToDate = ownerCitizenExistingList[i].link_date;

                        logInfo(String.Concat("OwnerCitizenDB.GetExistingOwnerCitizen() : Fix dup active OwnerCitizen record for citizen: ", ownerCitizenExistingList[i].citizen_token_id, " link_Key: ", ownerCitizenExistingList[i].link_key));
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerCitizenDB.GetExistingOwnerCitizen() : Error with query to find existing OwnerCitizen link with cit token_id : ", tokenId));
            }

            return ownerCitizenExisting;

        }

        public EntityEntry<OwnerCitizen> AddByLinkDateTime(OwnerCitizen ownerCitizen, bool saveFlag)
        {
            EntityEntry<OwnerCitizen> ownerCitizenMatch = null, newOwnerCitizen = null;
            long tickDiff;

            try
            {
                List<OwnerCitizen> ownerCitizenExistingList = _context.ownerCitizen.Where(r => r.citizen_token_id == ownerCitizen.citizen_token_id &&
                    r.land_token_id == ownerCitizen.land_token_id &&
                    r.pet_token_id == ownerCitizen.pet_token_id &&
                    r.owner_matic_key == ownerCitizen.owner_matic_key).ToList();

                // Need to bring back possible list, as DB.Linq unable to find match with Datetime & miliseconds when an actual match exists, causes dup creation.
                foreach (OwnerCitizen ownerCitizenExisting in ownerCitizenExistingList)
                {
                    tickDiff = ((DateTime)ownerCitizenExisting.link_date).Ticks - ((DateTime)ownerCitizen.link_date).Ticks;
                    if (tickDiff < 30000 && tickDiff > -30000) // + or - 3 milisecond range is good.
                    {
                        ownerCitizenMatch = _context.Entry(ownerCitizenExisting);
                        break;
                    }
                }


                // Find if record already exists, if not add it.
                if (ownerCitizenMatch == null)
                {
                    newOwnerCitizen = _context.ownerCitizen.Add(ownerCitizen);

                }

                if (saveFlag)
                {
                    _context.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerCitizenDB.AddByLinkDateTime() : Error adding record to db with citizen token_id : ", ownerCitizen.citizen_token_id));
            }

            return newOwnerCitizen;
        }

        // Add a new OwnerCitizen action if land,pet,owner change found, ownerCitizen is newer then last recorded (ownerCitizen.link_date = ownerCitizenExisting.valid_to_date).
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
                else if (ownerCitizen.land_token_id != ownerCitizenExisting.land_token_id ||
                        ownerCitizen.pet_token_id != ownerCitizenExisting.pet_token_id ||
                        ownerCitizen.owner_matic_key != ownerCitizenExisting.owner_matic_key)
                {
                    // Mark prior record as expired but retain for use in Production history eval -  previously used -1 date, changed to using current datetime due to refresh feature.
                    ownerCitizenExisting.valid_to_date = ownerCitizen.link_date;
                    ownerCitizenExisting.refreshed_last = DateTime.UtcNow;

                    // Add new record
                    _context.ownerCitizen.Add(ownerCitizen);

                }
                else
                {
                    // No change with Existing-Active-Stored record, but update refresh date to reflect eval
                    ownerCitizenExisting.refreshed_last = DateTime.UtcNow;
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
                //exec sproc - update Owner table with count of citizens per owner.
                result = _context.Database.ExecuteSqlRaw("EXEC dbo.sp_citizen_count");

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerCitizenDB::UpdateCitizenCount() : Error updating owner citizen count using sproc sp_citizen_count"));
            }

            return result;
        }

        // Delete all history from last x day - used in special events where issue occured on prior data-sync that may incur only partial history eval (missing history actions)
        public int DeleteHistory(int tokenId, bool saveFlag)
        {
            try
            {
                _context.ownerCitizen.RemoveRange(
                    _context.ownerCitizen.Where(x => x.citizen_token_id == tokenId && (x.link_date == null || x.link_date >= DateTime.UtcNow.AddDays((int)CITIZEN_HISTORY.DAYS)))
                    );


                if (saveFlag)
                {
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("CitizenDB.UpdateRefrshStatus() : Error adding record to db with citizen token_id : ", tokenId));
            }

            return 0;
        }
    }
}
