﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public class CitizenDB : DatabaseBase
    {
        public CitizenDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }

        public Citizen GetCitizen(int tokenId)
        {
            Citizen citizen = null;
            try
            {
                citizen = _context.citizen.Where(r => r.token_id == tokenId).FirstOrDefault();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("CitizenDB.GetCitizen() : Error adding record to db with citizen token_id : ", tokenId));
            }

            return citizen;
        }

        // ADD new Citizen record if none found, or UPDATE if key attributes changed (traits, name, onsale, price) - due to assignment or removal of PET
        public bool AddorUpdate(Citizen citizen, Citizen storedCitizen, bool saveFlag)
        {
            bool citizenHistoryRefresh = false;
            try
            {
                // Extract flag used to indicate if a complete refresh of citizen history actions (over prior 40 days) is required - due to prior WS REST fault
                if (storedCitizen != null)
                {
                    citizenHistoryRefresh = storedCitizen.refresh_history;
                }

                // Find if record already exists, if not add it.
                if (storedCitizen == null)
                {
                    citizen.last_update = DateTime.Now.ToUniversalTime();
                    citizen.create_date = DateTime.Now.ToUniversalTime();

                    _context.citizen.Add(citizen);

                    if (saveFlag)
                    {
                        _context.SaveChanges();
                    }
                }
                else if (storedCitizen.breeding != citizen.breeding ||
                    storedCitizen.name != citizen.name ||
                    storedCitizen.on_sale != citizen.on_sale ||
                    storedCitizen.on_sale_key != citizen.on_sale_key ||
                    storedCitizen.current_price != citizen.current_price ||
                    storedCitizen.trait_agility != citizen.trait_agility ||
                    storedCitizen.trait_strength != citizen.trait_strength ||
                    storedCitizen.trait_endurance != citizen.trait_endurance ||
                    storedCitizen.trait_charisma != citizen.trait_charisma ||
                    storedCitizen.trait_luck != citizen.trait_luck ||
                    storedCitizen.efficiency_commercial != citizen.efficiency_commercial ||
                    storedCitizen.efficiency_energy_electric != citizen.efficiency_energy_electric ||
                    storedCitizen.efficiency_energy_water != citizen.efficiency_energy_water ||
                    storedCitizen.efficiency_industry != citizen.efficiency_industry ||
                    storedCitizen.efficiency_municipal != citizen.efficiency_municipal ||
                    storedCitizen.efficiency_office != citizen.efficiency_office ||
                    storedCitizen.efficiency_production != citizen.efficiency_production ||
                    storedCitizen.refresh_history != citizen.refresh_history
                    )     // changed attributes
                {

                    storedCitizen.breeding = citizen.breeding;
                    storedCitizen.name = citizen.name;
                    storedCitizen.current_price = citizen.current_price;
                    storedCitizen.on_sale = citizen.on_sale;
                    storedCitizen.on_sale_key = citizen.on_sale_key;

                    storedCitizen.trait_agility = citizen.trait_agility;
                    storedCitizen.trait_strength = citizen.trait_strength;
                    storedCitizen.trait_endurance = citizen.trait_endurance;
                    storedCitizen.trait_charisma = citizen.trait_charisma;
                    storedCitizen.trait_luck = citizen.trait_luck;
                    storedCitizen.trait_intelligence = citizen.trait_intelligence;

                    storedCitizen.efficiency_commercial = citizen.efficiency_commercial;
                    storedCitizen.efficiency_energy_electric = citizen.efficiency_energy_electric;
                    storedCitizen.efficiency_energy_water = citizen.efficiency_energy_water;
                    storedCitizen.efficiency_industry = citizen.efficiency_industry;
                    storedCitizen.efficiency_production = citizen.efficiency_production;
                    storedCitizen.efficiency_office = citizen.efficiency_office;
                    storedCitizen.efficiency_municipal = citizen.efficiency_municipal;

                    storedCitizen.last_update = DateTime.Now.ToUniversalTime();
                    storedCitizen.refresh_history = citizen.refresh_history;

                    if (saveFlag)
                    {
                        _context.SaveChanges();
                    }
                }


            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("CitizenDB.AddorUpdate() : Error adding record to db with citizen token_id : ", citizen.token_id));
            }

            return citizenHistoryRefresh;
        }

        public int UpdateRefreshHistory(int tokenId, bool refreshHistory, bool saveFlag)
        {
            try
            {
                _context.SaveChanges();   // Citizen record may not be saved to db yet

                Citizen storedCitizen = GetCitizen(tokenId);
                storedCitizen.last_update = DateTime.Now.ToUniversalTime();
                storedCitizen.refresh_history = refreshHistory;

                if (saveFlag)
                {
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("CitizenDB.UpdateRefreshStatus() : Error adding record to db with citizen token_id : ", tokenId));
            }

            return 0;
        }

    }
}