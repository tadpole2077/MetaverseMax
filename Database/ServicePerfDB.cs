using MetaverseMax.BaseClass;
using MetaverseMax.ServiceClass;

namespace MetaverseMax.Database
{
    public class ServicePerfDB : DatabaseBase
    {
        private static int counterETH { get; set; }
        private static int counterBNB { get; set; }
        private static int counterTRX { get; set; }

        public ServicePerfDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
            worldType = _parentContext.worldTypeSelected;
        }

        public RETURN_CODE AddServiceEntry(string serviceUrl, DateTime startTime, long runTime, int responseSize, string serviceParam)
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            int counter = 0;
            try
            {
                if (ServiceCommon.logServiceInfo == false)
                {
                    return RETURN_CODE.SUCCESS;
                }

                counter = worldType switch { WORLD_TYPE.ETH => ++counterETH, WORLD_TYPE.BNB => ++counterBNB, _ or WORLD_TYPE.TRON => ++counterTRX };

                // impose a range of 50 max chars on ServieEntry string.
                ServicePerf servicePerf = new()
                {
                    service_url = serviceUrl,
                    start_time = startTime,
                    run_time = (int)runTime,
                    response_size = responseSize,
                    service_param = serviceParam[0..(serviceParam.Length > 50 ? 50 : serviceParam.Length)]
                };

                _context.servicePerf.Add(servicePerf);

                if (counter > 50)
                {
                    _context.SaveChanges();

                    if (worldType == WORLD_TYPE.ETH)
                    {
                        counterETH = 0;
                    }
                    else if (worldType == WORLD_TYPE.BNB)
                    {
                        counterBNB = 0;
                    }
                    else if (worldType == WORLD_TYPE.TRON)
                    {
                        counterTRX = 0;
                    }
                }

                returnCode = RETURN_CODE.SUCCESS;
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("ServicePerfDB.AddServiceEntry() : Error adding Peformance record for service - ", serviceUrl));
            }

            return returnCode;
        }
    }
}
