using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class Citizen
    {
        // CITIZEN Business rules methods
        public string AssignDefaultOwnerImg(string ownerID)
        {
            if (ownerID.Equals("0"))
            {
                ownerID = "./images/MysteryOwner.png";
            }
            else
            {
                ownerID = "https://mcp3d.com/tron/api/image/citizen/" + ownerID;
            }

            return ownerID;
        }

        public int GetCitizenCount(JArray citizens)
        {
            int count = 0;
            if (citizens != null)
            {
                count = citizens.Count;
            }
            return count;
        }

        public bool CheckCitizenStamina(JArray citizens, int buildingType)
        {
            int count = 0;
            if (citizens != null)
            {
                count = buildingType switch
                {
                    (int)BUILDING_TYPE.RESIDENTIAL =>
                        citizens.Where(
                            row => (row.Value<int?>("stamina") ?? 0) <= 200
                            ).Count(),

                    (int)BUILDING_TYPE.OFFICE =>
                        citizens.Where(
                            row => (row.Value<int?>("stamina") ?? 0) <= 50
                            ).Count(),

                    (int)BUILDING_TYPE.COMMERCIAL =>
                        citizens.Where(
                        row => (row.Value<int?>("stamina") ?? 0) <= 10
                        ).Count(),

                    (int)BUILDING_TYPE.MUNICIPAL =>
                        citizens.Where(
                        row => (row.Value<int?>("stamina") ?? 0) <= 20
                        ).Count(),

                    (int)BUILDING_TYPE.INDUSTRIAL =>
                        citizens.Where(
                        row => (row.Value<int?>("stamina") ?? 0) <= 25
                        ).Count(),

                    (int)BUILDING_TYPE.PRODUCTION =>
                         citizens.Where(
                         row => (row.Value<int?>("stamina") ?? 0) <= 100
                         ).Count(),

                    (int)BUILDING_TYPE.ENERGY =>
                        citizens.Where(
                        row => (row.Value<int?>("stamina") ?? 0) <= 30
                        ).Count(),

                    _ => citizens.Where(
                       row => (row.Value<int?>("stamina") ?? 0) <= 100
                       ).Count(),
                };
            }

            return count > 0;
        }

        public string GetCitizenUrl(JArray citizens)
        {
            string citizenUrl = string.Empty;
            if (citizens != null && citizens.Count >0)
            {
                //*** Find minimum stamina of all cits (in building), then find image of first one (possible that more then 1 with same min stamina value)
                int minStamina = GetLowStamina(citizens);
                JToken minStaminaCitizen = citizens.Where(row => (row.Value<int?>("stamina") ?? 0) == minStamina).First();
                
                citizenUrl = string.Concat("https://mcp3d.com/tron/api/image/citizen/", minStaminaCitizen.Value<int?>("id") ?? 0 );
            }
            return citizenUrl;
        }

        public int GetLowStamina(JArray citizens)
        {
            int minStamina =0;
            if (citizens != null && citizens.Count > 0)
            {
                //*** Find minimum stamina of all cits (in building)
                minStamina = citizens.Min(row => row.Value<int?>("stamina") ?? 0);
            }
            return minStamina;
        }

    }
}
