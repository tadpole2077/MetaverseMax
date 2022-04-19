using MetaverseMax.ServiceClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public class ServicePerfDB : DatabaseBase
    {
        private static int counter { get; set; }

        public ServicePerfDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }

        public RETURN_CODE AddServiceEntry(string serviceUrl, DateTime startTime, long runTime, int responseSize, string serviceParam)
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            try
            {
                if (Startup.logServiceInfo == null || Startup.logServiceInfo == "0")
                {
                    return RETURN_CODE.SUCCESS;
                }

                counter++;

                ServicePerf servicePerf = new()
                {
                    service_url = serviceUrl,
                    start_time = startTime,
                    run_time = (int)runTime,
                    response_size = responseSize,
                    service_param = serviceParam
                };

                _context.servicePerf.Add(servicePerf);

                if (counter > 50)
                {
                    _context.SaveChanges();
                    counter = 0;
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
