using MetaverseMax.Database;

namespace MetaverseMax.ServiceClass
{
    public class AlertManage : ServiceBase
    {
        private Common common = new Common();

        public AlertManage(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext, worldTypeSelected)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;
        }


        // alerts generated and shown - as defined by AlertTrigger data
        public AlertWeb Get(string ownerMatic, ALERT_STATE pendingAlert)
        {
            AlertDB alertDB = new(_context);
            List<Database.Alert> alertList = new List<Database.Alert>();
            int historyCount = 0;

            // Get all - then filter list based on request. Optimal to find the current total in history to update ticker on client side
            alertList = alertDB.GetAll(ownerMatic);
            historyCount = alertList.Count;

            if (pendingAlert == ALERT_STATE.ALL)
            {
                UpdateRead(ownerMatic);     // as history is a manual triggered process - mark new alerts as seen/read - dont auto show again in next interval check.
            }
            else if (pendingAlert == ALERT_STATE.UNREAD)
            {
                alertList = alertList.Where(x => x.matic_key == ownerMatic && x.alert_delete == false && x.owner_read == false).ToList();
            }

            return AlertWebFill(alertList, historyCount); ;
        }

        public RETURN_CODE UpdateRead(string ownerMatic)
        {
            AlertDB alertDB = new(_context);

            return alertDB.UpdateRead(ownerMatic);                           
        }

        public bool Delete(string ownerMatic, int alertKey)
        {
            AlertDB alertPendingDB = new(_context);

            alertPendingDB.DeleteByKey(ownerMatic, alertKey);

            return true;
        }

        private AlertWeb AlertWebFill(List<Database.Alert> alertList, int historyCount)
        {
            AlertWeb alertWeb = new AlertWeb();
            List<AlertItem> alertItemList = new List<AlertItem>();

            alertItemList = alertList.Select(alert =>
            {
                return new AlertItem
                {
                    alert_pending_key = alert.alert_pending_key,
                    alert_message = alert.alert_message,
                    icon_type = alert.icon_type,
                    icon_type_change = alert.icon_type_change,
                    last_updated = common.DateFormatStandard(alert.last_updated)
                };
            }).ToList();

            alertWeb.alert = alertItemList;
            alertWeb.historyCount = historyCount;

            return alertWeb;
        }

        public bool AddLowStaminaAlert(string maticKey, int citizenCount)
        {

            AlertDB alertDB = new(_context);

            alertDB.DeleteByType(maticKey, ALERT_ICON_TYPE.STAMINA);

            alertDB.Add(maticKey, ALERT_MESSAGE.LOW_STAMINA.Replace("#CIT_AMOUNT#", citizenCount.ToString()), ALERT_ICON_TYPE.STAMINA, ALERT_ICON_TYPE_CHANGE.NONE);


            return true;
        }

        public bool AddOfferAlert(string ownerMaticKey, string maticKey, int assetType, decimal price, int assetId)
        {

            AlertDB alertDB = new(_context);
            OwnerManage ownerManage = new(_context, worldType);            

            string message = ALERT_MESSAGE.NEW_OFFER
                .Replace("#BIDDER#", ownerManage.FindOwnerNameByMatic(maticKey))
                .Replace("#ASSET#", common.LookupTokenType(assetType))
                .Replace("#ASSET_ID#", assetId.ToString())
                .Replace("#PRICE#", price.ToString() + " " + worldType switch { WORLD_TYPE.ETH => "ETH", WORLD_TYPE.BNB => "BNB", _ or WORLD_TYPE.TRON => "TRX" });


            alertDB.Add(ownerMaticKey, message, ALERT_ICON_TYPE.NEW_OFFER, ALERT_ICON_TYPE_CHANGE.NONE);

            return true;
        }

        public bool AddOfferAcceptAlert(string bidderMaticKey, string ownerMaticKey, int assetType, decimal price, int assetId)
        {

            AlertDB alertDB = new(_context);
            OwnerManage ownerManage = new(_context, worldType);

            string message = ALERT_MESSAGE.OFFER_ACCEPTED_BY
                .Replace("#OWNER#", ownerManage.FindOwnerNameByMatic(ownerMaticKey))
                .Replace("#ASSET#", common.LookupTokenType(assetType))
                .Replace("#ASSET_ID#", assetId.ToString())
                .Replace("#PRICE#", price.ToString() + " " + worldType switch { WORLD_TYPE.ETH => "ETH", WORLD_TYPE.BNB => "BNB", _ or WORLD_TYPE.TRON => "TRX" });


            alertDB.Add(bidderMaticKey, message, ALERT_ICON_TYPE.NEW_OFFER, ALERT_ICON_TYPE_CHANGE.NONE);

            return true;
        }

        public bool AddRankingAlert(string alertMaticKey, string ownerMaticKey, int plotTokenId, decimal oldRanking, decimal newRanking, int buildingLevel, string buildingType)
        {

            AlertDB alertDB = new(_context);
            OwnerManage ownerManage = new(_context, worldType);

            string ownerName = ownerManage.FindOwnerNameByMatic(ownerMaticKey);

            string message = ALERT_MESSAGE.RANKING_CHANGE
                .Replace("#TOKEN_ID#", plotTokenId.ToString())
                .Replace("#LEVEL#", buildingLevel.ToString())
                .Replace("#BUILDING_TYPE#", buildingType)
                .Replace("#NEW_RANKING#", newRanking.ToString())
                .Replace("#OLD_RANKING#", oldRanking.ToString())
                .Replace("#OWNER#", ownerName != string.Empty ? "\nOwner: " + ownerName : "");


            alertDB.Add(alertMaticKey, message, ALERT_ICON_TYPE.RANKING, newRanking > oldRanking ? ALERT_ICON_TYPE_CHANGE.INCREASE : ALERT_ICON_TYPE_CHANGE.DECREASE);

            return true;
        }
    }
}
