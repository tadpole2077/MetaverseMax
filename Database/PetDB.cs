using Microsoft.EntityFrameworkCore;
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

        public List<Pet> GetOwnerPet(string maticKey)
        {
            List<Pet> petList = new();

            try
            {                
                petList = _context.pet.Where(r => r.token_owner_matic_key == maticKey)
                                      .OrderByDescending(r => r.bonus_level)
                                      .ToList();                
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("PetDB.GetOwnerPet() : Error gettings records with owner matic key : ", maticKey));
                    _context.LogEvent(log);
                }
            }

            return petList;
        }

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
                    _context.LogEvent(String.Concat("PetDB.AddorUpdate() : Error adding record to db with owner matic key : ", maticKey));
                    _context.LogEvent(log);
                }
            }

            return 0;
        }

        public int UpdatePetCount()
        {
            int result = 0;
            try
            {
                //exec sproc - update Owner table with count of pets per owner.
                result = _context.Database.ExecuteSqlRaw("EXEC dbo.sp_pet_count");

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("PetDB::UpdatePetCount() : Error updating owner pet count using sproc sp_pet_count "));
                _context.LogEvent(log);
            }

            return result;
        }
    }
}
