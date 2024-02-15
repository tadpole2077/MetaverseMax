using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using MetaverseMax.BaseClass;
using MetaverseMax.ServiceClass;

namespace MetaverseMax.Database
{
    public partial class MetaverseMaxDbContext : DbContext
    {
        public static string dbConnectionStringTron { get; set; }
        public static string dbConnectionStringBNB { get; set; }
        public static string dbConnectionStringETH { get; set; }
       
        public static int dbCommandTimeout { get; set; }

        public WORLD_TYPE worldTypeSelected { get; set; }

        public virtual DbSet<BuildingTypeIP> buildingTypeIP { get; set; }
        public virtual DbSet<Plot> plot { get; set; }        
        public virtual DbSet<PlotIP> plotIP { get; set; }
        public virtual DbSet<CustomBuilding> customBuilding { get; set; }
        public virtual DbSet<BuildingParcel> buildingParcel { get; set; }
        public virtual DbSet<Mission> mission { get; set; }
        public virtual DbSet<MissionActive> missionActive { get; set; }

        public virtual DbSet<District> district { get; set; }
        public virtual DbSet<DistrictFund> districtFund { get; set; }
        public virtual DbSet<DistrictPerk> districtPerk { get; set; }
        public virtual DbSet<DistrictContent> districtContent { get; set; }
        public virtual DbSet<DistrictTaxChange> districtTaxChange { get; set; }

        public virtual DbSet<OwnerSummaryDistrict> ownerSummaryDistrict { get; set; }
        public virtual DbSet<Owner> owner { get; set; }
        public virtual DbSet<OwnerEXT> ownerEXT { get; set; }
        public virtual DbSet<OwnerName> ownerName { get; set; }
        public virtual DbSet<OwnerOffer> ownerOffer { get; set; }
        public virtual DbSet<AlertTrigger> alertTrigger { get; set; }
        public virtual DbSet<Alert> alert { get; set; }
        public virtual DbSet<Citizen> citizen { get; set; }        
        public virtual DbSet<OwnerCitizen> ownerCitizen { get; set; }
        public virtual DbSet<OwnerCitizenExt> OwnerCitizenExt { get; set; }
        public virtual DbSet<OwnerMaterial> OwnerMaterial { get; set; }
        public virtual DbSet<Pet> pet { get; set; }
        public virtual DbSet<EventLog> eventLog { get; set; }
        public virtual DbSet<ServicePerf> servicePerf { get; set; }
        public virtual DbSet<Sync> sync { get; set; }
        public virtual DbSet<SyncHistory> syncHistory { get; set; }
        public virtual DbSet<JobSetting> JobSetting { get; set; }
        
        public virtual DbSet<BlockchainTransaction> BlockchainTransaction { get; set; }
        public virtual DbSet<PersonalSign> PersonalSign { get; set; }

        // options will be assigned on OnConfiguring()
        public MetaverseMaxDbContext() : base()
        {
            init();
        }
        public MetaverseMaxDbContext(WORLD_TYPE worldType) : base()
        {
            worldTypeSelected = worldType;
            init();
        }
        public MetaverseMaxDbContext(DbContextOptions<MetaverseMaxDbContext> options) : base(options)
        {
            init();
        }
        public MetaverseMaxDbContext(string dbConnectionString) : base(new DbContextOptionsBuilder<MetaverseMaxDbContext>().UseSqlServer(dbConnectionString).Options)
        {
            init();
        }

        private void init()
        {
            if (string.IsNullOrEmpty(dbConnectionStringTron))
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

                dbConnectionStringTron = configuration.GetConnectionString("DatabaseConnection");
                dbConnectionStringBNB = configuration.GetConnectionString("DatabaseConnectionBNB");
                dbConnectionStringETH = configuration.GetConnectionString("DatabaseConnectionETH");                
                dbCommandTimeout = (int)configuration.GetValue(typeof(int), "DBCommandTimeout");
            }
        }

        // Triggered on first actual use of DbContext - such as _context.table.Where(..),  not triggered on creation of a new context unless specfically set via passed init options
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            if (!optionsBuilder.IsConfigured)
            {
                _ = worldTypeSelected switch
                {
                    WORLD_TYPE.TRON => optionsBuilder.UseSqlServer(dbConnectionStringTron, sqlServerOptionsOptions => sqlServerOptionsOptions.CommandTimeout(dbCommandTimeout)),
                    WORLD_TYPE.BNB => optionsBuilder.UseSqlServer(dbConnectionStringBNB, sqlServerOptionsOptions => sqlServerOptionsOptions.CommandTimeout(dbCommandTimeout)),
                    WORLD_TYPE.ETH => optionsBuilder.UseSqlServer(dbConnectionStringETH, sqlServerOptionsOptions => sqlServerOptionsOptions.CommandTimeout(dbCommandTimeout)),
                    _ => optionsBuilder.UseSqlServer(dbConnectionStringTron, sqlServerOptionsOptions => sqlServerOptionsOptions.CommandTimeout(dbCommandTimeout))
                };
            }
        }


        public RETURN_CODE SaveWithRetry(bool throwParent = false)
        {
            DBLogger dBLogger = new(this, worldTypeSelected);
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
                logDetail = logDetail.Substring(0, logDetail.Length > 500 ? 500 : logDetail.Length);        // db field max length

                eventLog.Add(new EventLog()
                {
                    detail = logDetail,
                    recorded_time = DateTime.Now
                });

                SaveChanges();
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(this, worldTypeSelected);
                dBLogger.logException(ex, String.Concat("MetaverseMaxDbContext::LogEvent() : Error during Event_Log() call storing message :", logDetail));
            }
            return 0;
        }

        public int ActionUpdate(ACTION_TYPE actionType)
        {
            int result = 0;

            // Event log should used a separate context in case problem thrown with a prior call to SaveChanges
            try
            {
                result = Database.ExecuteSqlInterpolated($"UPDATE ActionTime set [last_update] = getDate() where [action_type] = {actionType.ToString("G")}");
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                LogEvent(String.Concat("MetaverseMaxDbContext.UpdateAction() : Error updating action datetime for ", actionType.ToString("G")));
            }

            return 0;
        }

        public DateTime ActionTimeGet(ACTION_TYPE actionType)
        {
            int result = 0;
            SqlParameter lastUpdated = null;

            // Event log should used a separate context in case problem thrown with a prior call to SaveChanges
            try
            {
                //SqlParameter requires Microsoft.Data.SqlClient lib or thows error
                lastUpdated = new SqlParameter
                {
                    ParameterName = "@last_updated",
                    SqlDbType = System.Data.SqlDbType.DateTime,
                    Direction = System.Data.ParameterDirection.Output,
                };

                result = Database.ExecuteSqlInterpolated($"Select {lastUpdated} = [last_update] from ActionTime where [action_type] = {actionType.ToString("G")}");

                if (lastUpdated.Value == DBNull.Value)
                {
                    LogEvent(String.Concat("MetaverseMaxDbContext.ActionTimeGet() : Error no action record found for ", actionType.ToString("G")));
                }

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                LogEvent(String.Concat("MetaverseMaxDbContext.UpdateAction() : Error updating action datetime for ", actionType.ToString("G")));
                LogEvent(log);
            }

            return (DateTime)lastUpdated.Value;
        }

        // Define the decimal precision to match that of sql server column definition. 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Note precision can also be defined within column metadata >> [Column("for_rent", TypeName = "decimal(16, 4)")]
            modelBuilder.Entity<DistrictFund>().Property(p => p.balance).HasPrecision(14, 7);           // Set decimal format to match defualt for C#
            modelBuilder.Entity<DistrictFund>().Property(p => p.distribution).HasPrecision(14, 7);
            modelBuilder.Entity<BuildingTypeIP>().Property(p => p.current_price).HasPrecision(16, 4);

            //modelBuilder.Entity<OwnerOffer>().HasKey(p => new { p.offer_id });      // Explicitly set the primary key, as using key from source and not db seed generated.
            modelBuilder.Entity<OwnerOffer>().Property(p => p.buyer_offer).HasPrecision(12, 6);

            // Setup Composite primary keys
            // modelBuilder.Entity<Owner>().HasKey(o => new { o.owner_matic_key, o.public_key }).HasName("PrimaryKey_Owner");

            // Tag Entity's with no key.
            //modelBuilder.Entity<Owner>().HasNoKey();
            modelBuilder.Entity<PlotIP>().HasNoKey();

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
