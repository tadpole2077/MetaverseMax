﻿using MetaverseMax.ServiceClass;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

            try
            {
                ownerCitizenExisting = _context.ownerCitizen.Where(r => r.citizen_token_id == tokenId && r.valid_to_date == null).FirstOrDefault();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerCitizenDB.GetExistingOwnerCitizen() : Error with query to find existing OwnerCitizen link with cit token_id : ", tokenId));
            }

            return ownerCitizenExisting;

        }

        public EntityEntry<OwnerCitizen> AddByLinkDateTime(OwnerCitizen ownerCitizen, bool saveFlag)
        {
            EntityEntry<OwnerCitizen> ownerCitizenNew = null;
            bool foundMatch = false;
            long tickDiff;

            try
            {
                List<OwnerCitizen> ownerCitizenExistingList = _context.ownerCitizen.Where(r => r.citizen_token_id == ownerCitizen.citizen_token_id &&
                    r.land_token_id == ownerCitizen.land_token_id &&
                    r.pet_token_id == ownerCitizen.pet_token_id &&
                    r.owner_matic_key == ownerCitizen.owner_matic_key).ToList();

                // Need to bring back to C# possible list, as DB.Linq unable to find match with Datetime & miliseconds when an actual match exists, causes dup creation.
                foreach (OwnerCitizen ownerCitizenExisting in ownerCitizenExistingList)
                {
                    tickDiff = ((DateTime)ownerCitizenExisting.link_date).Ticks - ((DateTime)ownerCitizen.link_date).Ticks;
                    if (tickDiff < 30000 && tickDiff > -30000) // + or - 3 milisecond range is good.
                    {
                        foundMatch = true;
                        break;
                    }
                }
                    

                // Find if record already exists, if not add it.
                if (foundMatch == false)
                {
                    ownerCitizenNew = _context.ownerCitizen.Add(ownerCitizen);

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

            return ownerCitizenNew;
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
                else if ( ownerCitizen.land_token_id != ownerCitizenExisting.land_token_id ||
                        ownerCitizen.pet_token_id != ownerCitizenExisting.pet_token_id ||
                        ownerCitizen.owner_matic_key != ownerCitizenExisting.owner_matic_key )
                {
                    // Mark prior record as expired but retain for use in Production history eval -  previously used -1 date, changed to using current datetime due to refresh feature.
                    ownerCitizenExisting.valid_to_date = ownerCitizen.link_date;

                    // Add new record
                    _context.ownerCitizen.Add(ownerCitizen);
                }
                else
                {
                    ownerCitizenExisting.refreshed_last = DateTime.Now.ToUniversalTime();
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

        // Delete all history from last x day - used in special events where issue occured on prior data-sync that may incur only partial history eval (missing history actions)
        public int DeleteHistory(int tokenId, bool saveFlag)
        {
            try
            {
                _context.ownerCitizen.RemoveRange(
                    _context.ownerCitizen.Where(x => x.citizen_token_id == tokenId && (x.link_date == null || x.link_date >= DateTime.UtcNow.AddDays((int)CITIZEN_HISTORY.DAYS)) )
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