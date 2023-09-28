using MetaverseMax.BaseClass;
using MetaverseMax.Database;

namespace MetaverseMax.Database
{
    public class CustomBuildingDB : DatabaseBase
    {
        public CustomBuildingDB(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldTypeSelected;
        }

        public CustomBuilding GetBuildingByInfoId(int parcelInfoId)
        {
            CustomBuilding customBuilding = null;
            try
            {
                customBuilding = _context.customBuilding.Where(x => x.parcel_info_id == parcelInfoId).FirstOrDefault();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("CustomBuildingDB:GetBuildingByInfoId() : Error getting building with infoId =", parcelInfoId));
            }

            return customBuilding;
        }

        public CustomBuilding Add(CustomBuilding customBuilding)
        {
            CustomBuilding customBuildingEntity = null;
            try
            {
                customBuilding = _context.customBuilding.Add(customBuilding).Entity;
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("CustomBuildingDB:Add() : Error adding new building with infoId =", customBuilding.parcel_info_id));
            }

            return customBuildingEntity;
        }

        public CustomBuilding Update(CustomBuilding customBuilding)
        {
            CustomBuilding customBuildingEntity = null;
            try
            {
                customBuilding = _context.customBuilding.Update(customBuilding).Entity;
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("CustomBuildingDB:Update() : Error updating building with infoId =", customBuilding.parcel_info_id));
            }

            return customBuildingEntity;
        }
    }
}
