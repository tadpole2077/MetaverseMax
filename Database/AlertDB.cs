﻿using MetaverseMax.BaseClass;
using Microsoft.EntityFrameworkCore;

namespace MetaverseMax.Database
{
    public class AlertDB : DatabaseBase
    {
        public AlertDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }

        // alertTriggerKey = 0, means alert can not be disabled - its automatically generated for all accounts
        public RETURN_CODE Add(string maticKey, string message, ALERT_ICON_TYPE iconType, ALERT_ICON_TYPE_CHANGE iconTypeChange, ALERT_TYPE alertType, int alertId, bool saveEvent)
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            Alert alert = null;
            bool dupFound = false;

            try
            {
                // CHECK if alert matching this type already exists then skip adding a dup - For example, this may occur on buildings with multiple plots
                if (alertType == ALERT_TYPE.NEW_BUILDING) {
                    dupFound = _context.alert.Where(x => x.alert_type == (int)ALERT_TYPE.NEW_BUILDING && x.alert_id == alertId && x.matic_key == maticKey && x.alert_delete == false).Count() > 0;
                }

                if (dupFound == false)
                {
                    alert = new Alert
                    {
                        matic_key = maticKey,
                        alert_message = message,
                        alert_delete = false,
                        owner_read = false,
                        icon_type = (short)iconType,
                        last_updated = DateTime.UtcNow,
                        owner_read_time = null,
                        icon_type_change = (short)iconTypeChange,
                        alert_type = (int)alertType,
                        alert_id = alertId
                    };

                    _context.alert.Add(alert);


                    if (saveEvent)
                    {
                        _context.SaveChanges();
                    }
                }
                returnCode = RETURN_CODE.SUCCESS;
            }
            catch (Exception ex)
            {
                // Remove poisoned record from EF pending save cache
                if (alert != null)
                {
                    _context.Entry(alert).State = EntityState.Detached;
                }

                logException(ex, String.Concat("AlertDB.Add() : Error with Adding Alert Message"));
            }

            return returnCode;
        }

        public List<Alert> GetRead(string ownerMatic)
        {

            List<Alert> alertList = null;
            try
            {                
                alertList = _context.alert.Where(x => x.matic_key == ownerMatic && x.alert_delete == false && x.owner_read == false).OrderByDescending(x=> x.last_updated).ToList();

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("AlertDB.GetRead() : Error getting unread owner alert with maticKey: ", ownerMatic));
            }

            return alertList;
        }

        public List<Alert> GetAll(string ownerMatic)
        {

            List<Alert> alertList = null;
            try
            {
                alertList = _context.alert.Where(x => x.matic_key == ownerMatic && x.alert_delete == false).OrderByDescending(x => x.last_updated).ToList();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("AlertDB.GetALL() : Error with getting ALL owner alerts with  maticKey: ", ownerMatic));
            }

            return alertList;
        }
        
        public RETURN_CODE DeleteByKey(string ownerMatic, int alertKey)
        {
            Alert alertPending = null;
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            try
            {
                alertPending = _context.alert.Where(x => x.matic_key == ownerMatic && x.alert_pending_key == alertKey).FirstOrDefault();

                //alertPending.last_updated = DateTime.UtcNow;
                if (alertPending != null)
                {
                    alertPending.alert_delete = true;
                    _context.SaveChanges();
                }
                returnCode = RETURN_CODE.SUCCESS;
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("AlertDB.DeleteByKey() : Error deleting alert with  maticKey: ", ownerMatic, " and alert_pending_key: ", alertKey));
            }

            return returnCode;
        }

        public RETURN_CODE DeleteByType(string ownerMatic, ALERT_ICON_TYPE alertType, bool saveEvent)
        {
            Alert alert = null;
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            try
            {
                alert = _context.alert.Where(x => x.matic_key == ownerMatic && x.icon_type == (short)alertType).FirstOrDefault();
                if (alert != null)
                {
                    _context.alert.Remove(alert);
                }

                if (saveEvent)
                {
                    _context.SaveChanges();
                }

                returnCode = RETURN_CODE.SUCCESS;
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("AlertDB.DeleteByType() : Error deleting alert with  maticKey: ", ownerMatic, " and icon_type: ", (short)alertType));
            }

            return returnCode;
        }

        public RETURN_CODE UpdateRead(string ownerMatic)
        {
            List<Alert> alertList = null;
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            try
            {
                alertList = _context.alert.Where(x => x.matic_key == ownerMatic && x.owner_read == false && x.alert_delete == false).ToList();

                for(int rowIndex=0; rowIndex < alertList.Count; rowIndex++)
                {
                    alertList[rowIndex].owner_read = true;

                    alertList[rowIndex].owner_read_time = DateTime.UtcNow;
                }                

                _context.SaveChanges();

                returnCode = RETURN_CODE.SUCCESS;
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("AlertDB.DeleteByKey() : Error updatding alerts with maticKey: ", ownerMatic));
            }

            return returnCode;
        }

        public int count(string ownerMatic)
        {
            int alertCount = 0;
            try
            {
                alertCount = _context.alert.Where(x => x.matic_key == ownerMatic && x.alert_delete == false).Count();

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("AlertDB.count() : Error with getting count of owner alerts with maticKey: ", ownerMatic));
            }

            return alertCount;
        }
    }
}
