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
        public Pet GetPetDetail(int tokenId)
        {
            Pet petDetail = new();

            try
            {
                petDetail = _context.pet.Where(r => r.token_id == tokenId)
                                      .FirstOrDefault();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PetDB.GetPetDetails() : Error gettings Pet record with token ID : ", tokenId));
            }

            return petDetail;
        }

        // Most usage of this method will have no writes on db unless a pet was sold or bought, previously all pet where deleted and recreated = slow perf
        public int AddorUpdate(List<Pet> petList, string maticKey)
        {
            try
            {

                IEnumerable<Pet> petEntitiesLegacy = _context.pet.Where(r => r.token_owner_matic_key.ToLower() == maticKey.ToLower()).ToArray();

                //  Check if any Pets on this account transferred/sold
                var newPetTokens = petList.Select(x => x.token_id).ToArray();
                List<Pet> soldPets = petEntitiesLegacy.Where(x => !newPetTokens.Contains(x.token_id)).ToList();
                for (int index =0; index < soldPets.Count(); index++)
                {
                    soldPets[index].last_update = DateTime.Now;
                    soldPets[index].token_owner_matic_key = string.Empty;
                }

                // Check for new Pets, bought or transfered pets.
                for (int index = 0; index < petList.Count; index++)
                {
                    Pet existingPet = _context.pet.Where(r => r.token_id == petList[index].token_id).FirstOrDefault();

                    // Not found in DB then add, else update to match current owner (transfer/sale)
                    if (existingPet == null)
                    {
                        _context.pet.Add(petList[index]);
                    }
                    else if (existingPet.token_owner_matic_key != maticKey)
                    {
                        existingPet.token_owner_matic_key = maticKey;
                        existingPet.last_update = DateTime.Now;
                    }
                }

                if (petList.Count > 0 || soldPets.Count > 0)
                {
                    _context.SaveChanges();
                }

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
