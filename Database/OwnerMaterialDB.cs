using MetaverseMax.ServiceClass;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace MetaverseMax.Database
{
    public class OwnerMaterialDB : DatabaseBase
    {
        public OwnerMaterialDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }
        
        public bool Add(OwnerMaterial ownerMaterial)
        {

            try
            {
                _context.OwnerMaterial.Add(ownerMaterial);

                _context.SaveChanges();
                
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerMaterialDB.Add() : Error adding record for owner : ", ownerMaterial.owner_matic_key));
                    _context.LogEvent(log);
                }
            }

            return true;
        }
    }
}
