using Microsoft.EntityFrameworkCore;

namespace MetaverseMax.Database
{
    public class BuildingParcelDB : DatabaseBase
    {
        public BuildingParcelDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }

        public List<BuildingParcel> ParcelGet(int districtId)
        {
            List<BuildingParcel> buildingList = new();

            try
            {
                // do not track changes to the entity data, used for read-only scenarios, can not use SaveChanges(). min overhead on retriving and use of entity                
                buildingList = _context.buildingParcel.FromSqlInterpolated($"exec sp_parcel_get {districtId}").AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("BuildingParcelDB::ParcelGet() : Error executing sproc sp_parcel_get - districtId: ", districtId.ToString()));
            }

            return buildingList;
        }

        public List<BuildingParcel> ParcelGetByAccountMatic(string ownerMatic)
        {
            List<BuildingParcel> buildingList = new();

            try
            {
                // do not track changes to the entity data, used for read-only scenarios, can not use SaveChanges(). min overhead on retriving and use of entity                
                buildingList = _context.buildingParcel.FromSqlInterpolated($"exec sp_parcel_get_by_matic { ownerMatic }").AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("BuildingParcelDB::ParcelGetByAccountMatic() : Error executing sproc sp_parcel_get - districtId: ", ownerMatic));
            }

            return buildingList;
        }

    }
}
