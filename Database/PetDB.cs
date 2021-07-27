using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public class PetDB
    {
        private readonly MetaverseMaxDbContext _context;

        public PetDB(MetaverseMaxDbContext _parentContext)
        {
            _context = _parentContext;
        }

        public DbSet<Pet> pet { get; set; }   // Links to a specific table in DB

        public int AddorUpdate(List<Pet> petList, string maticKey)
        {
            try
            {
                // Fist remove pior pet records for this owner.
                IEnumerable<Pet> petEntitiesDelete = _context.pet.Where(r => r.token_owner_matic_key == maticKey).ToArray();
                _context.pet.RemoveRange(petEntitiesDelete);
                _context.SaveChanges();

                for (int index = 0; index < petList.Count; index++)
                {
                    _context.pet.Add(petList[index]);                                        
                }
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("PetDB.AddorUpdate() : Error adding offer record to db with owner matic key : ", maticKey));
                    _context.LogEvent(log);
                }
            }

            return 0;
        }
    }
}
