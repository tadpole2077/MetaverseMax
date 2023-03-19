using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MetaverseMax.Database
{
    public class OwnerOfferDB : DatabaseBase
    {
        public OwnerOfferDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
            worldType = _parentContext.worldTypeSelected;
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
                logException(ex, String.Concat("OwnerOfferDB.SetOffersInactive() : Error executing Raw query "));
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
                logException(ex, String.Concat("OwnerOfferDB.SetOffersInactive() : Error executing Raw query "));
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
                    storedOffer.buyer_offer = ownerOffer.buyer_offer;           // Need to update due to ETH/BNB bug that dropped the price on earlier releases - can remove if needed after live data sync run
                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerOfferDB.AddorUpdate() : Error adding offer record to db with offer_id : ", ownerOffer.offer_id.ToString()));
            }

            return 0;
        }

        public int RemoveCancelledOffers(List<int> validOffers, string owner_matic_key)
        {
            int deleteCount = 0;
            try
            {
                deleteCount = _context.ownerOffer.Where(o => !validOffers.Contains(o.offer_id) && o.token_owner_matic_key == owner_matic_key)
                    .ExecuteDelete();       // EF 7 feature           
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerOfferDB.RemoveCancelledOffers() : Error removing any cancelled record from db with owner_matic_key : ", owner_matic_key));
            }

            return deleteCount;
        }
    }
}
