using MetaverseMax.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class ServiceBase
    {
        protected readonly MetaverseMaxDbContext _context;
        
        public ServicePerfDB servicePerfDB { get; set; }
        public string serviceUrl { get; set; }
        public DateTime serviceStartTime { get; set; }
        public Stopwatch watch { get; set; }

        public ServiceBase(MetaverseMaxDbContext _contextService)
        {
            _context = _contextService;
            servicePerfDB = new ServicePerfDB(_context);
        }

        public async Task WaitPeriodAction(int waitPeriodMS)
        {
            await Task.Delay(waitPeriodMS);
            return;
        }

        public SocketsHttpHandler getSocketHandler()
        {
            // POST from User/Get REST WS
            SocketsHttpHandler socketsHandler = new();

            watch = Stopwatch.StartNew();
            serviceStartTime = DateTime.Now;


            socketsHandler.ConnectCallback = async (context, token) =>
            {
                var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                string connectionString = Startup.serverIP;
                if (Startup.serverIP != "")
                {
                    s.Bind(new IPEndPoint(IPAddress.Parse(Startup.serverIP), 0));
                }
                else
                {
                    s.Bind(new IPEndPoint(IPAddress.Any, 0));
                }

                await s.ConnectAsync(context.DnsEndPoint, token);

                s.NoDelay = true;

                return new NetworkStream(s, ownsSocket: true);
            };

            return socketsHandler;
        } 

    }
}
