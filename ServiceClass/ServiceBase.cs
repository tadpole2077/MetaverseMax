using MetaverseMax.Database;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace MetaverseMax.ServiceClass
{
    public class ServiceBase
    {
        protected readonly MetaverseMaxDbContext _context;

        public WORLD_TYPE worldType { get; set; }
        public ServicePerfDB servicePerfDB { get; set; }
        public string serviceUrl { get; set; }
        public DateTime serviceStartTime { get; set; }
        public Stopwatch watch { get; set; }

        public ServiceBase(MetaverseMaxDbContext _contextService, WORLD_TYPE worldType)
        {
            _context = _contextService;
            _context.worldTypeSelected = worldType;

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

                string connectionString = Common.serverIP;
                if (Common.serverIP != "")
                {
                    s.Bind(new IPEndPoint(IPAddress.Parse(Common.serverIP), 0));
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
