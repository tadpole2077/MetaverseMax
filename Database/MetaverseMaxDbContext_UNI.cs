using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using MetaverseMax.BaseClass;
using MetaverseMax.ServiceClass;

namespace MetaverseMax.Database
{
    // UNIVERSAL Database content - used regardless of selected world
    public partial class MetaverseMaxDbContext_UNI : DbContext
    {

        public static string dbConnectionStringUNI { get; set; }
        public static int dbCommandTimeout { get; set; }

        public WORLD_TYPE worldTypeSelected { get; set; }

        public virtual DbSet<EventLog> eventLog { get; set; }
        public virtual DbSet<Setting> setting { get; set; }
        public virtual DbSet<OwnerUni> ownerUni { get; set; }
        public virtual DbSet<MaticKeyLink> maticKeyLink { get; set; }

        public virtual DbSet<BlockchainTransaction> BlockchainTransaction { get; set; }

        // options will be assigned on OnConfiguring()
        public MetaverseMaxDbContext_UNI() : base()
        {
            init();
        }
        public MetaverseMaxDbContext_UNI(DbContextOptions<MetaverseMaxDbContext> options) : base(options)
        {
            init();
        }
        public MetaverseMaxDbContext_UNI(string dbConnectionString) : base(new DbContextOptionsBuilder<MetaverseMaxDbContext>().UseSqlServer(dbConnectionString).Options)
        {
            init();
        }

        private void init()
        {
            if (string.IsNullOrEmpty(dbConnectionStringUNI))
            {
                string appSettingFileName = "appsettings.json";

                if (ServiceCommon.isDevelopment)
                {
                    appSettingFileName = "appsettings.Development.json";
                }


                // Get Configuration Settings 
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(appSettingFileName)
                    .Build();

                dbConnectionStringUNI = configuration.GetConnectionString("DatabaseConnectionUNI");
                dbCommandTimeout = (int)configuration.GetValue(typeof(int), "DBCommandTimeout");
            }
        }

        // Triggered on first actual use of DbContext - such as _context.table.Where(..),  not triggered on creation of a new context unless specfically set via passed init options
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(dbConnectionStringUNI, sqlServerOptionsOptions => sqlServerOptionsOptions.CommandTimeout(dbCommandTimeout));               
            }
        }


        public RETURN_CODE SaveWithRetry(bool throwParent = false)
        {
            DBLogger dBLogger = new(WORLD_TYPE.UNIVERSE);
            int retryCount = 0;
            bool success = false;

            while (retryCount < 3 && success == false)
            {
                try
                {
                    retryCount++;
                    this.SaveChanges();
                    success = true;
                }
                catch (Exception ex)
                {
                    dBLogger.logException(ex, String.Concat("MetaverseMaxDbContext::SaveWithRetry() : Error Saving - likely deadlock/timeout - Retry Count ", retryCount));
                }
            }
            if (success == false && throwParent)
            {
                throw new Exception("Unable to Save");
            }

            return RETURN_CODE.SUCCESS;
        }

        public int LogEvent(string logDetail)
        {
            try
            {
                logDetail = logDetail[..(logDetail.Length > 500 ? 500 : logDetail.Length)];        // db field max length

                eventLog.Add(new EventLog()
                {
                    detail = logDetail,
                    recorded_time = DateTime.Now
                });

                SaveChanges();
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(WORLD_TYPE.UNIVERSE);
                dBLogger.logException(ex, String.Concat("MetaverseMaxDbContext::LogEvent() : Error during Event_Log() call storing message :", logDetail));
            }
            return 0;
        }


        // Define the decimal precision to match that of sql server column definition. 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        // Using Type: 5.0.16.0  EntityFrameworkCore.DbContext (confirm if working with any core library upgrades)
        public bool IsDisposed()
        {
            bool result = true;
            var typeDbContext = typeof(DbContext);
            var isDisposedTypeField = typeDbContext.GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);

            if (isDisposedTypeField != null)
            {
                result = (bool)isDisposedTypeField.GetValue(this);
            }

            return result;
        }
    }
}
