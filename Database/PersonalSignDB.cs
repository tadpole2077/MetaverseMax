using MetaverseMax.BaseClass;
using Microsoft.EntityFrameworkCore;
using NBitcoin.Secp256k1;
using Newtonsoft.Json.Linq;

namespace MetaverseMax.Database
{
    public class PersonalSignDB : DatabaseBase
    {

        public PersonalSignDB(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;
        }

        public PersonalSign AddOrUpdate(PersonalSign personalSign)
        {
            PersonalSign storedSign = null;
            bool newSign = false;
            bool storedSignInLocal = false;

            try
            {
                storedSign = _context.PersonalSign.Where(x => x.matic_key == personalSign.matic_key && x.encode_byte == personalSign.encode_byte).FirstOrDefault();

                // Corner Case: Check if previously generated (only in local context) but not yet saved to db - can occur during building upgrades of Huge / Mega
                storedSignInLocal = _context.PersonalSign.Local.Any(e => e.matic_key == personalSign.matic_key && e.encode_byte == personalSign.encode_byte);

                if (personalSign != null)
                {

                    if (storedSign == null)
                    {
                        storedSign = new();
                        newSign = true;
                    }

                    storedSign.matic_key = personalSign.matic_key;
                    storedSign.encode_byte = personalSign.encode_byte;
                    storedSign.created = DateTime.UtcNow;
                    storedSign.salt = personalSign.salt;
                    storedSign.amount = personalSign.amount;
                    storedSign.signed_key = personalSign.signed_key;

 
                    if (newSign && storedSignInLocal == false)
                    {
                        // Remove any old sign - potential partial sign.  Each account should only have one active sign.
                        _context.PersonalSign.RemoveRange(
                            _context.PersonalSign.Where(x => x.matic_key == personalSign.matic_key));

                        storedSign = _context.PersonalSign.Add(storedSign).Entity;
                    }

                    _context.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PersonalSignDB::AddOrUpdate() : Error adding new PersonalSign for account using matic_key : ", personalSign.matic_key));

                // De-associate from the faulty row. Improves fault tolerance - allows SaveChange() to complete - context with other pending transactions
                if (storedSign != null)
                {
                    _context.Entry(storedSign).State = EntityState.Detached;
                }
            }

            return storedSign;
        }

        public PersonalSign GetUnsignedByMaticKey(string maticKey)
        {
            PersonalSign storedSign = null;
            
            try
            {
                storedSign = _context.PersonalSign.Where(x => x.matic_key == maticKey && x.signed_key == null).FirstOrDefault();

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PersonalSignDB::GetUnsignedByMaticKey() : Error getting PersonalSign for account using matic_key : ", maticKey));
            }

            return storedSign;
        }
    }
}
