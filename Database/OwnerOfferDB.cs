using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MetaverseMax.Database
{
    public class OwnerOfferDB
    {
        private readonly MetaverseMaxDbContext _context;
        public OwnerOfferDB(MetaverseMaxDbContext _parentContext)
        {
            _context = _parentContext;
        }

        public List<OwnerOffer> GetbyOwnerMatic(string ownerMaticKey)
        {         
            List<OwnerOffer> ownerOfferList = new();

            try
            {
                ownerOfferList = _context.ownerOffer.Where(x => x.token_owner_matic_key == ownerMaticKey)
                                                    .OrderByDescending(x => x.offer_date).ToList();
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("OwnerOfferDB.SetOffersInactive() : Error executing Raw query "));               
            }

            return ownerOfferList;
        }

        public int SetOffersInactive()
        {
            int result = 0;
            try
            {
                result = _context.Database.ExecuteSqlRaw("UPDATE OwnerOffer set active = 0");

                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("OwnerOfferDB.SetOffersInactive() : Error executing Raw query "));
            }

            return 0;
        }

        public int AddorUpdate(OwnerOffer ownerOffer)
        {
            try 
            {
                OwnerOffer storedOffer = _context.ownerOffer.Where(r => r.offer_id == ownerOffer.offer_id).FirstOrDefault();

                if (storedOffer == null)
                {
                    _context.ownerOffer.Add(ownerOffer);                    
                }
                else
                {
                    storedOffer.active = ownerOffer.active;
                    storedOffer.sold = ownerOffer.sold;
                    storedOffer.sold_date = ownerOffer.sold_date;
                }
                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("OwnerOfferDB.AddorUpdate() : Error adding offer record to db with offer_id : ", ownerOffer.offer_id.ToString()));
            }

            return 0;
        }
    }
}
