using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public class PetDB : DatabaseBase
    {
        public PetDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
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
                logException(ex, String.Concat("PetDB.GetOwnerPet() : Error gettings records with owner matic key : ", maticKey));
            }

            return petList;
        }

        // Most usage of this method will have no writes on db unless a pet was sold or bought, previously all pet where deleted and recreated = slow perf
        public int AddorUpdate(List<Pet> petList, string maticKey)
        {
            try
            {
                // First remove pior pet records for this owner.
                IEnumerable<Pet> petEntitiesLegacy = _context.pet.Where(r => r.token_owner_matic_key == maticKey).ToArray();
                //_context.pet.RemoveRange(petEntitiesDelete);
                //_context.SaveChanges();

                // PERF Improved - only delete if needed (if sold or transfered)
                var newPetTokens = petList.Select(x => x.token_id).ToArray();
                var soldPets = petEntitiesLegacy.Where(x => !newPetTokens.Contains(x.token_id));
                if (soldPets.Count() > 0)
                {
                    _context.pet.RemoveRange(soldPets);
                }

                // Check for new Pets
                for (int index = 0; index < petList.Count; index++)
                {
                    if (petEntitiesLegacy.Where(r => r.token_id == petList[index].token_id).Count() == 0)
                    {
                        _context.pet.Add(petList[index]);
                    }
                }
                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PetDB.AddorUpdate() : Error adding record to db with owner matic key : ", maticKey));
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
                logException(ex, String.Concat("PetDB::UpdatePetCount() : Error updating owner pet count using sproc sp_pet_count"));
            }

            return result;
        }
    }
}
