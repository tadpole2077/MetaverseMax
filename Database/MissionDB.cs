﻿using MetaverseMax.BaseClass;
using MetaverseMax.Database;
using Microsoft.EntityFrameworkCore;
using Nethereum.Contracts.Standards.ERC20.TokenList;
using Newtonsoft.Json.Linq;

namespace MetaverseMax.Database
{
    public class MissionDB : DatabaseBase
    {

        public MissionDB(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;
        }

        public bool CheckHasMission(int tokenId)
        {
            bool hasMission = false;
            try
            {
                hasMission = _context.mission.Where(x => x.token_id == tokenId && x.balance > 0 && x.available).Any();

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("MissionDB::CheckHasMission() : Error checking if existing mission for token_id : ", tokenId));
            }

            return hasMission;
        }


        public Mission AddOrUpdate(JObject missionData, int tokenId, decimal balance)
        {
            Mission mission = null;
            bool newMission = false;
            bool alreadyProcessedMission = false;

            try
            {
                mission = _context.mission.Where(x => x.token_id == tokenId).FirstOrDefault();

                // Corner Case: Check if Mission previously generated (only in local context) but not yet saved to db - can occur during building upgrades of Huge / Mega
                alreadyProcessedMission = _context.mission.Local.Any(e => e.token_id == tokenId);

                if (missionData != null) {
                 
                    if (mission == null) {                        
                        mission = new();
                        newMission = true;
                    }

                    mission.token_id = tokenId;
                    mission.completed = missionData.Value<int?>("missions_count") ?? 0;
                    mission.max = missionData.Value<int?>("missions_max") ?? 0;
                    mission.last_updated = DateTime.UtcNow;
                    mission.available = missionData.Value<bool?>("missions_available") ?? false;
                    mission.reward = missionData.Value<decimal?>("missions_reward") ?? 0;
                    mission.reward_owner = missionData.Value<decimal?>("missions_reward_owner") ?? 0;
                    mission.balance = balance;

                    if (newMission && alreadyProcessedMission == false)
                    {
                        mission = _context.mission.Add(mission).Entity;
                    }
                }

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("MissionDB::AddOrUpdate() : Error adding new Mission for Plot using token_id : ", tokenId));

                // De-associate from the faulty row. Improves fault tolerance - allows SaveChange() to complete - context with other pending transactions
                if (mission != null)
                {
                    _context.Entry(mission).State = EntityState.Detached;
                }
            }

            return mission;
        }

        public IEnumerable<MissionActive> MissionActiveGet()
        {
            List<MissionActive> missionList = new();

            try
            {
                // do not track changes to the entity data, used for read-only scenarios, can not use SaveChanges(). min overhead on retriving and use of entity                
                missionList = _context.missionActive.FromSqlInterpolated($"exec sp_missions_active_get").AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("MissionDB::MissionActiveGet() : Error executing sproc sp_missions_active_get "));
            }

            return missionList.ToArray();
        }

    }
}
