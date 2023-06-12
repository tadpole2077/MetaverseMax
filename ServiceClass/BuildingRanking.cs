using MetaverseMax.Database;
using System.ComponentModel;

namespace MetaverseMax.ServiceClass
{
    public class BuildingRanking
    {
        public int CalcMaxIP(List<Plot> buildingList) {

            return (int)Math.Round((double)buildingList.Max(x => x.influence * (1 + (x.influence_bonus / 100.0))), 0, MidpointRounding.AwayFromZero);            

        }

        public int CalcMaxIP(List<BuildingTypeIP> buildingList)
        {

            return (int)Math.Round((double)buildingList.Max(x => x.influence * (1 + (x.influence_bonus / 100.0))), 0, MidpointRounding.AwayFromZero);

        }

        // 2023_05_07 Min IP - Rule only check buildings that have a calcualted influence_info >=0
        // Case: Energy buildings may have a negative IP, these buildings are not included in max - min IP check.
        public int CalcMinIP(List<Plot> buildingList)
        {

            return buildingList.Where(x => x.influence_info < 0).Count() == buildingList.Count ? 0 :
                        (int)Math.Round(
                            buildingList.Where(x => x.influence_info >= 0).Min(x => x.influence ?? 0 * (1 + (x.influence_bonus ?? 0 / 100.0)))
                            , 0, MidpointRounding.AwayFromZero);

        }
        public int CalcMinIP(List<BuildingTypeIP> buildingList)
        {
            return buildingList.Where(x => x.influence_info < 0).Count() == buildingList.Count ? 0 :
                        (int)Math.Round(
                            buildingList.Where(x => x.influence_info >= 0).Min(x => x.influence * (1 + (x.influence_bonus / 100.0)))
                            , 0, MidpointRounding.AwayFromZero);
        }

        public double CalcAvgIP(List<BuildingTypeIP> buildingList)
        {
            return  Math.Round(buildingList.Average(x => (x.influence < 0 ? 0 : x.influence) * (1 + ((x.influence_bonus) / 100.0))), 0, MidpointRounding.AwayFromZero);
        }

        // MCP rule: Min IP must be a value >0, impacts energy buildings, shown on newspaper building report.
        public decimal GetIPEfficiency(int totalIP, int rangeIP, int minIP, MetaverseMaxDbContext _context)
        {
            decimal efficiency = 1;

            if (totalIP <= minIP)
            {
                efficiency = 0;
            }
            else if (rangeIP != 0)
            {
                efficiency = (totalIP - minIP) / (decimal)rangeIP;
            }

            if (efficiency > 1)
            {
                _context.LogEvent(String.Concat("BuildingMange::GetIPEfficiency() Unexpected Efficiency >1 : ", efficiency.ToString(), " totalIP: ", totalIP.ToString(), " minIP: ", minIP.ToString()));

            }

            return Math.Round(efficiency * 100, 2); ;
        }
    }
}
