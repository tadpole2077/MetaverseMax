using MetaverseMax.BaseClass;

namespace MetaverseMax.Database
{
    public class AlertTriggerDB : DatabaseBase
    {
        public AlertTriggerDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }

        public RETURN_CODE UpdateAlert(string maticKey, ALERT_TYPE alertType, int id, ALERT_ACTION_TYPE action)
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            try
            {
                int triggerId = alertType == ALERT_TYPE.NEW_BUILDING ? 0 : id;          // New Building Alert - store the image id, but the alert trigger used to generate them is generic for all new buildings.

                AlertTrigger ownerAlert = _context.alertTrigger.Where(x => x.matic_key == maticKey && x.key_type == (int)alertType && x.id == triggerId).FirstOrDefault();
                if (ownerAlert == null && action == ALERT_ACTION_TYPE.ENABLE)
                {                
                    _context.alertTrigger.Add(new AlertTrigger
                    {
                        matic_key = maticKey,
                        key_type = (int)alertType,
                        id = id,
                        last_updated = DateTime.UtcNow
                    });
                }
                else if (ownerAlert != null && action == ALERT_ACTION_TYPE.DISABLE)
                {
                    _context.alertTrigger.Remove(ownerAlert);
                }

                _context.SaveChanges();
                returnCode = RETURN_CODE.SUCCESS;
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerAlertDB.UpdateAlert() : Error with alert update on id: ", id.ToString(), " action:", (int)action, "maticKey: ", maticKey));
            }

            return returnCode;
        }

        public List<AlertTrigger> GetALL(string ownerMatic)
        {

            List<AlertTrigger> ownerAlertList = null;
            try
            {
                ownerAlertList = _context.alertTrigger.Where(x => x.matic_key == ownerMatic).ToList();

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerAlertDB.GetALL() : Error all alert triggers for owner maticKey: ", ownerMatic));
            }

            return ownerAlertList;
        }

        public List<AlertTrigger> GetByDistrict(string ownerMatic, int id)
        {

            List<AlertTrigger> ownerAlertList = null;
            try
            {
                ownerAlertList = _context.alertTrigger.Where(x => 
                    x.matic_key == ownerMatic && 
                    x.id == id && 
                    (x.key_type == (int)ALERT_TYPE.DISTRIBUTION || x.key_type == (int)ALERT_TYPE.INITIAL_LAND_VALUE || x.key_type == (int)ALERT_TYPE.CONSTRUCTION_TAX || x.key_type == (int)ALERT_TYPE.PRODUCTION_TAX)
                ).ToList();
                
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerAlertDB.GetByDistrict() : Error with alert getting alert with id: ", id.ToString(), " maticKey: ", ownerMatic));
            }

            return ownerAlertList;
        }
        
        // Get Trigger alerts matching (a) Type (b) key id - eg plot token id (c) owner alert triggers
        public List<AlertTrigger> GetAlertByType(string ownerMatic, ALERT_TYPE alertType, int tokenId)
        {

            List<AlertTrigger> ownerAlertList = null;
            try
            {
                if (ownerMatic.Equals("ALL"))
                {
                    if (tokenId == 0)
                    {
                        ownerAlertList = _context.alertTrigger.Where(x => x.key_type == (int)alertType).ToList();
                    }
                    else
                    {
                        ownerAlertList = _context.alertTrigger.Where(x => x.key_type == (int)alertType && x.id == tokenId).ToList();
                    }
                }
                else
                {
                    ownerAlertList = _context.alertTrigger.Where(x => x.matic_key == ownerMatic && x.key_type == (int)alertType).ToList();
                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerAlertDB.GetAlertByType() : Error with alert getting alert with type: ", ((int)alertType).ToString(), " maticKey: ", ownerMatic));
            }

            return ownerAlertList;
        }
    }
}
