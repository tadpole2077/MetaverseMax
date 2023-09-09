using Microsoft.EntityFrameworkCore;
using MetaverseMax.BaseClass;

namespace MetaverseMax.Database
{
    public class OwnerCitizenExtDB : DatabaseBase
    {

        public OwnerCitizenExtDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }

        public List<OwnerCitizenExt> GetCitizen(string ownerMatic)
        {
            List<OwnerCitizenExt> citizens = new();

            try
            {
                // Using string interpolation syntax to pull in parameters
                citizens = _context.OwnerCitizenExt.FromSqlInterpolated($"sp_owner_citizen_get {ownerMatic}").AsNoTracking().ToList();

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerCitizenExtDB.GetCitizen() : Error with query to get all owner citizens with Matic key : ", ownerMatic));
            }

            return citizens;
        }

        // GET all citizen assigned to a building during a specific datetime.
        public List<OwnerCitizenExt> GetBuildingCitizenHistory(int assetId, DateTime rundate)
        {
            List<OwnerCitizenExt> citizens = new();

            try
            {
                // Using string interpolation syntax to pull in parameters
                citizens = _context.OwnerCitizenExt.FromSqlInterpolated($"sp_asset_citizen_get {assetId}, {rundate}").AsNoTracking().ToList();

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerCitizenExtDB.GetBuildingCitizenHistory() : Error with query to get all asset citizens with token id : ", assetId));
            }

            return citizens;
        }

        // GET all citizens actions + history related to a building.  May cause issues when db grows REEVAL use.
        public int GetBuildingCitizen(int landTokenId, ref List<OwnerCitizenExt> citizens)
        {
            try
            {
                // Using string interpolation syntax to pull in parameters
                citizens = _context.OwnerCitizenExt.FromSqlInterpolated($"sp_building_citizen_get {landTokenId}").AsNoTracking().ToList();

                // Assign any pet bonus parts to Cit attributes.
                for (int counter = 0; counter < citizens.Count; counter++)
                {
                    if (citizens[counter].pet_token_id > 0)
                    {
                        citizens[counter].trait_strength_pet_bonus = citizens[counter].pet_bonus_id == (int)PET_BONUS_TYPE.STRENGTH ? CheckMaxTrait(citizens[counter].pet_bonus_level, citizens[counter].trait_strength) : 0;
                        citizens[counter].trait_endurance_pet_bonus = citizens[counter].pet_bonus_id == (int)PET_BONUS_TYPE.ENDURANCE ? CheckMaxTrait(citizens[counter].pet_bonus_level, citizens[counter].trait_endurance) : 0;
                        citizens[counter].trait_intelligence_pet_bonus = citizens[counter].pet_bonus_id == (int)PET_BONUS_TYPE.INTEL ? CheckMaxTrait(citizens[counter].pet_bonus_level, citizens[counter].trait_intelligence) : 0;
                        citizens[counter].trait_luck_pet_bonus = citizens[counter].pet_bonus_id == (int)PET_BONUS_TYPE.LUCK ? CheckMaxTrait(citizens[counter].pet_bonus_level, citizens[counter].trait_luck) : 0;
                        citizens[counter].trait_charisma_pet_bonus = citizens[counter].pet_bonus_id == (int)PET_BONUS_TYPE.CHARISMA ? CheckMaxTrait(citizens[counter].pet_bonus_level, citizens[counter].trait_charisma) : 0;
                        citizens[counter].trait_agility_pet_bonus = citizens[counter].pet_bonus_id == (int)PET_BONUS_TYPE.AGILITY ? CheckMaxTrait(citizens[counter].pet_bonus_level, citizens[counter].trait_agility) : 0;
                    }
                }

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerCitizenExtDB.GetBuildingCitizen() : Error with query to get all building citizens with land token_id : ", landTokenId));
            }

            return citizens.Count;
        }

        private int CheckMaxTrait(int? petBonusLevel, int traitStrength)
        {
            return (petBonusLevel ?? 0) + traitStrength > 10 ? 10 - traitStrength : petBonusLevel ?? 0;
        }
    }
}
