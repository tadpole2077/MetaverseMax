using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using MetaverseMax.Database;
using MetaverseMax.BaseClass;
using System.Net.Security;

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
            // Option to skip SSL cert check - invalid certs accepted (debug or backend api cert fault)
            // https://dev.to/tswiftma/switching-from-httpclienthandler-to-socketshttphandler-17h3
            var sslOptions = new SslClientAuthenticationOptions
            {
                // Leave certs unvalidated for debugging
                RemoteCertificateValidationCallback = delegate { return true; },
            };

            // POST from User/Get REST WS
            SocketsHttpHandler socketsHandler = new() { 
                SslOptions = ServiceCommon.endpointSSLDisabled ? sslOptions : null , 
            };

            watch = Stopwatch.StartNew();
            serviceStartTime = DateTime.Now;


            socketsHandler.ConnectCallback = async (context, token) =>
            {
                var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                string connectionString = ServiceCommon.serverIP;
                if (ServiceCommon.serverIP != "")
                {
                    s.Bind(new IPEndPoint(IPAddress.Parse(ServiceCommon.serverIP), 0));
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
