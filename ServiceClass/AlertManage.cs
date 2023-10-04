using MetaverseMax.Database;
using MetaverseMax.BaseClass;

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
            
            if (alertList != null)
            {
                historyCount = alertList == null ? 0 : alertList.Count;
                if (pendingAlert == ALERT_STATE.ALL)
                {
                    UpdateRead(ownerMatic);     // as history is a manual triggered process - mark new alerts as seen/read - dont auto show again in next interval check.
                }
                else if (pendingAlert == ALERT_STATE.UNREAD && alertList != null)
                {
                    alertList = alertList.Where(x => x.matic_key == ownerMatic && x.alert_delete == false && x.owner_read == false).ToList();
                }
            }
            return AlertWebFill(alertList, historyCount, ownerMatic); ;
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

        private AlertWeb AlertWebFill(List<Database.Alert> alertList, int historyCount, string ownerMatic)
        {
            AlertWeb alertWeb = new AlertWeb();
            AlertTriggerManager triggerManger = new(_context, worldType);
            List<AlertItem> alertItemList = new List<AlertItem>();
            List<AlertTrigger> alertTriggerList = triggerManger.GetAll(ownerMatic);

            if (alertList != null)
            {
                alertItemList = alertList.Select(alert =>
                {
                    return new AlertItem
                    {
                        alert_pending_key = alert.alert_pending_key,
                        alert_message = alert.alert_message,
                        alert_type = alert.alert_type,
                        icon_type = alert.icon_type,
                        icon_type_change = alert.icon_type_change,
                        last_updated = common.DateFormatStandard(alert.last_updated),
                        trigger_active = alertTriggerList.Where(x => x.key_type == alert.alert_type && x.id == getAlertId(alert.alert_id, (ALERT_TYPE)alert.alert_type)).Count() == 1,
                        alert_id = alert.alert_id,
                    };
                }).ToList();
            }
            alertWeb.alert = alertItemList;
            alertWeb.historyCount = historyCount;

            return alertWeb;
        }

        int getAlertId(int alertId, ALERT_TYPE alertType)
        {
            return alertType == ALERT_TYPE.NEW_BUILDING ? 0 : alertId;          // New Building Alert - store the image id, but the alert trigger used to generate them is generic for all new buildings.

        }

        public bool AddLowStaminaAlert(string maticKey, int citizenCount, bool saveEvent)
        {

            AlertDB alertDB = new(_context);

            alertDB.DeleteByType(maticKey, ALERT_ICON_TYPE.STAMINA, saveEvent);

            alertDB.Add(maticKey, ALERT_MESSAGE.LOW_STAMINA.Replace("#CIT_AMOUNT#", citizenCount.ToString()), ALERT_ICON_TYPE.STAMINA, ALERT_ICON_TYPE_CHANGE.NONE, ALERT_TYPE.NOT_USED, 0, saveEvent);

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


            alertDB.Add(ownerMaticKey, message, ALERT_ICON_TYPE.NEW_OFFER, ALERT_ICON_TYPE_CHANGE.NONE, ALERT_TYPE.NOT_USED, 0, true);

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


            alertDB.Add(bidderMaticKey, message, ALERT_ICON_TYPE.NEW_OFFER, ALERT_ICON_TYPE_CHANGE.NONE, ALERT_TYPE.NOT_USED, 0, true);

            return true;
        }

        public bool AddRankingAlert(string alertMaticKey, string ownerMaticKey, int plotTokenId, decimal oldRanking, decimal newRanking, int buildingLevel, string buildingType, ALERT_TYPE alertType)
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


            alertDB.Add(alertMaticKey, message, ALERT_ICON_TYPE.RANKING, newRanking > oldRanking ? ALERT_ICON_TYPE_CHANGE.INCREASE : ALERT_ICON_TYPE_CHANGE.DECREASE, alertType, plotTokenId, true);

            return true;
        }

        public bool AddNewBuildingAlert(string alertAccountMaticKey, string buildingOwnerMaticKey, int buildingTokenId, int districtId, int parcelInfoId, string buildingName)
        {

            AlertDB alertDB = new(_context);
            OwnerManage ownerManage = new(_context, worldType);
            string ownerName = ownerManage.FindOwnerNameByMatic(buildingOwnerMaticKey);
            ownerName = ownerName == string.Empty ? string.Concat(alertAccountMaticKey[..8], "..") : ownerName;


            //"New custom building(#BUILDING_TOKEN_ID#) in #DISTRICT_ID# district by #OWNER#."
            string message = ALERT_MESSAGE.NEW_BUILDING
                .Replace("#OWNER#", ownerName)
                .Replace("#BUILDING_NAME#", buildingName)
                .Replace("#DISTRICT_ID#", districtId.ToString());

            alertDB.Add(alertAccountMaticKey, message, ALERT_ICON_TYPE.NEW_BUILDING, ALERT_ICON_TYPE_CHANGE.NONE, ALERT_TYPE.NEW_BUILDING, parcelInfoId, true);

            return true;
        }

        public RETURN_CODE UnitTest_AddAlertNewBuilding()
        {
            try
            {
                AlertTriggerManager alertTrigger = new(_context, worldType);
                AlertManage alert = new(_context, worldType);
                List<AlertTrigger> allOwnerAlerts = new();
                
                allOwnerAlerts = alertTrigger.GetByType("ALL", ALERT_TYPE.NEW_BUILDING, 0);
                List<Plot> buildingPlots = _context.plot.Where(x => x.parcel_info_id > 0).ToList();

                buildingPlots.ForEach(p =>
                {
                    allOwnerAlerts.ForEach(x =>
                    {
                        alert.AddNewBuildingAlert(x.matic_key, p.owner_nickname == string.Empty ? p.owner_matic : p.owner_nickname, p.token_id, p.district_id, p.parcel_info_id, p.building_name);
                    });
                });
            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("Alert.UnitTest_AddAlertNewBuilding() : Error on running unit test"));
            }

            return RETURN_CODE.SUCCESS;
        }

        public RETURN_CODE UnitTest_AddAlertRankingChange()
        {
            try
            {
                PlotManage plotManage = new(_context, worldType);
                OwnerManage ownerManage = new(_context, worldType);
                Building building = new();
                AlertTriggerManager alertTrigger = new(_context, worldType);

                if (worldType == WORLD_TYPE.BNB || worldType == WORLD_TYPE.TRON)
                {
                    OwnerData ownerData = ownerManage.GetOwnerLands("0xb197dc47fcbe7d7734b60fa87fd3b0ba0acaf441", false, false);
                    OwnerLand ownerLand = ownerData.owner_land.Where(x => x.building_type == (int)BUILDING_TYPE.ENERGY).FirstOrDefault();

                    alertTrigger.UpdateOwnerAlert("0xb197dc47fcbe7d7734b60fa87fd3b0ba0acaf441", ALERT_TYPE.BUILDING_RANKING, ownerLand.token_id, ALERT_ACTION_TYPE.ENABLE);
                    AddRankingAlert("0xb197dc47fcbe7d7734b60fa87fd3b0ba0acaf441", "0xb197dc47fcbe7d7734b60fa87fd3b0ba0acaf441", ownerLand.token_id, 50, 75, ownerLand.building_level, building.BuildingType((int)BUILDING_TYPE.ENERGY, 0), ALERT_TYPE.BUILDING_RANKING);
                }
                else
                {                    
                    Plot plot = _context.plot.Where(x => x.building_type_id == (int)BUILDING_TYPE.ENERGY).FirstOrDefault();

                    alertTrigger.UpdateOwnerAlert("0xb197dc47fcbe7d7734b60fa87fd3b0ba0acaf441", ALERT_TYPE.BUILDING_RANKING, plot.token_id, ALERT_ACTION_TYPE.ENABLE);
                    AddRankingAlert("0xb197dc47fcbe7d7734b60fa87fd3b0ba0acaf441", plot.owner_matic, plot.token_id, 50, 75, plot.building_level, building.BuildingType((int)BUILDING_TYPE.ENERGY, 0), ALERT_TYPE.BUILDING_RANKING);
                }

            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("Alert.UnitTest_AddRankingChange() : Error on running unit test"));
            }

            return RETURN_CODE.SUCCESS;
        }

        public RETURN_CODE UnitTest_AddAlertTax()
        {
            try
            {
                DistrictTaxChangeDB districtTaxChangeDB = new(_context);
                PlotManage plotManage = new(_context, worldType);
                OwnerManage ownerManage = new(_context, worldType);
                Building building = new();
                AlertTriggerManager alertTrigger = new(_context, worldType);

                District district = _context.district.OrderByDescending(x=>x.district_key).FirstOrDefault();
                alertTrigger.UpdateOwnerAlert("0xb197dc47fcbe7d7734b60fa87fd3b0ba0acaf441", ALERT_TYPE.DISTRIBUTION, district.district_id, ALERT_ACTION_TYPE.ENABLE);

                district.distribution_period = district.distribution_period + 1;
                _context.SaveChanges();
                districtTaxChangeDB.UpdateTaxChanges();                

            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("Alert.UnitTest_AddRankingChange() : Error on running unit test"));
            }

            return RETURN_CODE.SUCCESS;
        }
    }
}
