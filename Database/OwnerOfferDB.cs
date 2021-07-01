﻿using System;
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
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerOfferDB.SetOffersInactive() : Error executing Raw query "));
                    _context.LogEvent(log);
                }
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
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerOfferDB.SetOffersInactive() : Error executing Raw query "));
                    _context.LogEvent(log);
                }
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
                    storedOffer.active = true;
                }
                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerOfferDB.AddorUpdate() : Error adding offer record to db with offer_id : ", ownerOffer.offer_id.ToString()));
                    _context.LogEvent(log);
                }
            }

            return 0;
        }
    }
}
