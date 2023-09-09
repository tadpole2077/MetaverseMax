using MetaverseMax.BaseClass;

namespace MetaverseMax.Database
{
    public class SyncHistoryDB : DatabaseBase
    {

        public SyncHistoryDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
            worldType = _parentContext.worldTypeSelected;
        }

        public RETURN_CODE Add(int worldType, DateTime startTime, DateTime endTime)
        {
            RETURN_CODE response = RETURN_CODE.ERROR;
            try
            {
                _context.syncHistory.Add(new SyncHistory()
                {
                    type = string.Concat((WORLD_TYPE)worldType switch { WORLD_TYPE.ETH => "Eth", WORLD_TYPE.BNB => "BNB", _ or WORLD_TYPE.TRON => "Tron" }, " sync job"),
                    sync_start = startTime,
                    sync_end = endTime,
                    sync_duration = (endTime - startTime).ToString(@"hh\:mm\:ss")
                }
                );

                _context.SaveChanges();
                response = RETURN_CODE.SUCCESS;
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("SyncHistoryDB :: Add() : Error adding a sync history log entry"));
            }

            return response;
        }

    }
}