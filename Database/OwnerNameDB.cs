using MetaverseMax.BaseClass;
using MetaverseMax.ServiceClass;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace MetaverseMax.Database
{

    public class OwnerNameDB : DatabaseBase
    {
        public OwnerNameDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }

        // The Add ownerName (from plot) is now completed in sp_owner_sync..  But cases for this call remain.
        // [2023/10/29]  This code is still needed imo, as plots still have owner_name/owner_avatar_id - and only a subset of plots are updated on sync or owner portfolio load.
        //               The sproc sp_owner_sync would need to be amended, as it pulls new owner_names from db.plots.owner_name.  whereas this call pulls the ownername from ownerChange collection (which is itself populated on nighly sync)
        public int UpdateOwnerName(OwnerChange ownerChange)
        {
            int rowCountUpdated = 0;            

            try
            {
                
                OwnerName lastOwnerName = _context.ownerName.Where(o => o.owner_matic_key == ownerChange.owner_matic_key)
                    .OrderByDescending(x => x.created_date)
                    .FirstOrDefault();

                // Add new record if (a) new account (b) new avatar used for account (c) new name assigned
                if (lastOwnerName == null || lastOwnerName.avatar_id != ownerChange.owner_avatar_id || lastOwnerName.owner_name != ownerChange.owner_name)
                {
                    _context.ownerName.Add(
                        new OwnerName()
                        {
                            owner_matic_key = ownerChange.owner_matic_key,
                            avatar_id = ownerChange.owner_avatar_id,
                            owner_name = ownerChange.owner_name,
                            created_date = DateTime.UtcNow
                        }
                    );
                }

                // Check if owner is new (not found in database).  This may occur if owner owns no plots, but owns other tracked assets such as a District.
                if (_context.ownerName.Where(o => o.owner_matic_key == ownerChange.owner_matic_key).Count() == 0)
                {
                    JobSettingDB jobSettingDB = new(_context);

                    int freeDays = jobSettingDB.GetSettingValue(JOB_SETTING_CODE.NEW_ACCOUNT_PRO_TOOLS_FREE_DAYS);

                    _context.owner.Add(
                    new Owner()
                    {
                        owner_matic_key = ownerChange.owner_matic_key,
                        public_key = "",
                        type = 1,
                        tool_active = false,
                        owner_lookup_count = 0,
                        district_lookup_count = 0,
                        player_key = 0,
                        pet_count = 0,
                        citizen_count = 0,
                        dark_mode = false,
                        alert_activated = false,
                        created_date = DateTime.Now,
                        pro_access_expiry = DateTime.Now.AddDays(freeDays),
                        pro_access_renew_code = "test"
                    });
                }


                //TEMP CODE - Future Enh : Assign same name and avatar_id to all plots owned by this account -  May be removed if avatar_id and owner_name removed from plot enh (using account & accountName to store as single source)
                rowCountUpdated = _context.plot.Where(x => x.owner_matic == ownerChange.owner_matic_key)
                    .ExecuteUpdate(p => p
                        .SetProperty(x => x.owner_avatar_id, x => ownerChange.owner_avatar_id)
                        .SetProperty(x => x.owner_nickname, x => ownerChange.owner_name));

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerDB::UpdateOwnerName() : Error updating OwnerName with MaticKey = ", ownerChange.owner_matic_key, "and owner_name = ", ownerChange.owner_name));
            }

            return rowCountUpdated;
        }
    }
}
