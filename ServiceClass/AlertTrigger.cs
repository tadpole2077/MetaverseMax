using MetaverseMax.Database;

namespace MetaverseMax.ServiceClass
{
    public class AlertTrigger : ServiceBase
    {

        public AlertTrigger(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext, worldTypeSelected)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;
        }

        // OwnerAlert Management Methods - controls the create/deletion of new alerts.
        public RETURN_CODE UpdateOwnerAlert(string maticKey, int alertType, int id, ALERT_ACTION_TYPE action)
        {

            AlertTriggerDB alertDB = new AlertTriggerDB(_context);
            RETURN_CODE returnCode = alertDB.UpdateAlert(maticKey, alertType, id, action);

            SetActivated(maticKey, true);

            return returnCode;
        }

        public List<Database.AlertTrigger> Get(string ownerMatic, int districtId)
        {
            AlertTriggerDB alertTriggerDB = new AlertTriggerDB(_context);
            List<Database.AlertTrigger> alertList = new List<Database.AlertTrigger>();

            try
            {
                alertList = alertTriggerDB.Get(ownerMatic, districtId);

            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("AlertTrigger.Get() : Error on WS calls for owner matic : ", ownerMatic, " and district :", districtId));
            }

            return alertList;
        }

        public List<Database.AlertTrigger> GetByType(string ownerMatic, ALERT_TYPE alertType)
        {
            AlertTriggerDB alertTriggerDB = new AlertTriggerDB(_context);
            List<Database.AlertTrigger> alertList = new List<Database.AlertTrigger>();

            try
            {
                alertList = alertTriggerDB.GetAlertByType(ownerMatic, alertType);

            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("AlertTrigger.GetByType() : Error on WS calls for owner matic : ", ownerMatic, " and alert type :", (int)alertType));
            }

            return alertList;
        }

        public bool SetActivated(string maticKey, bool alertActivated)
        {
            bool priorState = false;
            AlertDB alertDB = new(_context);
            OwnerManage ownerManage = new(_context, worldType);

            OwnerAccount ownerAccount = ownerManage.FindOwnerByMatic(maticKey, string.Empty);
            priorState = ownerAccount.alert_activated;
            ownerAccount.alert_activated = alertActivated;      // Update local cache store of ownerAccount.

            if (priorState == false)
            {
                // Update db - update ownerAccount with matching public wallet key if not already stored.  (used for TRON where matic and public differ)
                OwnerDB ownerDB = new OwnerDB(_context);
                ownerDB.UpdateOwnerAlertActivated(maticKey, alertActivated);

                alertDB.Add(maticKey, ALERT_MESSAGE.INTRO, ALERT_ICON_TYPE.INFO, ALERT_ICON_TYPE_CHANGE.NONE);
            }

            return true;
        }


    }
}
