﻿using MetaverseMax.ServiceClass;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace MetaverseMax.Database
{

    public class OwnerNameDB : DatabaseBase
    {
        public OwnerNameDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }

        public int UpdateOwnerName(OwnerChange ownerChange)
        {
            int rowCountUpdated = 0;

            try
            {
                // The below Add ownerName (from plot) is now completed in sp_owner_sync, can be removed after future eval 2023-02.
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

                //TEMP CODE : Assign same name and avatar_id to all plots owned by this account -  May be removed if avatar_id and owner_name removed from plot enh (using account & accountName to store as single source)
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