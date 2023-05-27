using MetaverseMax.ServiceClass;
using System;
using System.Collections.Generic;
using static System.Collections.Specialized.BitVector32;

namespace MetaverseMax.Database
{
    public class AlertTriggerDB : DatabaseBase
    {
        public AlertTriggerDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }

        public RETURN_CODE UpdateAlert(string maticKey, int alertType, int id, ALERT_ACTION_TYPE action)
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            try
            {
                AlertTrigger ownerAlert = _context.alertTrigger.Where(x => x.matic_key == maticKey && x.key_type == alertType && x.id == id).FirstOrDefault();
                if (ownerAlert == null && action == ALERT_ACTION_TYPE.ENABLE)
                {                
                    _context.alertTrigger.Add(new AlertTrigger
                    {
                        matic_key = maticKey,
                        key_type = alertType,
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

        public List<AlertTrigger> Get(string ownerMatic, int id)
        {

            List<AlertTrigger> ownerAlertList = null;
            try
            {
                ownerAlertList = _context.alertTrigger.Where(x => x.matic_key == ownerMatic && x.id == id).ToList();
                
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerAlertDB.GetAlert() : Error with alert getting alert with id: ", id.ToString(), " maticKey: ", ownerMatic));
            }

            return ownerAlertList;
        }
        
        public List<AlertTrigger> GetAlertByType(string ownerMatic, ALERT_TYPE alertType)
        {

            List<AlertTrigger> ownerAlertList = null;
            try
            {
                if (ownerMatic.Equals("ALL"))
                {
                    ownerAlertList = _context.alertTrigger.Where(x => x.key_type == (int)alertType).ToList();
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
