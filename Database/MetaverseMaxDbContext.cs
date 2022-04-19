using MetaverseMax.ServiceClass;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public partial class MetaverseMaxDbContext : DbContext
    {
        public virtual DbSet<BuildingTypeIP> buildingTypeIP { get; set; }
        public virtual DbSet<Plot> plot { get; set; }
        public virtual DbSet<PlotIP> plotIP { get; set; }
        public virtual DbSet<District> district { get; set; }
        public virtual DbSet<DistrictFund> districtFund { get; set; }
        public virtual DbSet<DistrictPerk> districtPerk { get; set; }
        public virtual DbSet<DistrictContent> districtContent { get; set; }
        public virtual DbSet<DistrictTaxChange> districtTaxChange { get; set; }

        //public virtual DbSet<DistrictUpdateInstance> districtUpdateInstance { get; set; }
        public virtual DbSet<OwnerSummaryDistrict> ownerSummaryDistrict { get; set; }
        public virtual DbSet<Owner> owner { get; set; }
        public virtual DbSet<OwnerOffer> ownerOffer { get; set; }
        public virtual DbSet<Citizen> citizen { get; set; }
        public virtual DbSet<OwnerCitizen> ownerCitizen { get; set; }
        public virtual DbSet<OwnerCitizenExt> OwnerCitizenExt { get; set; }
        public virtual DbSet<Pet> pet { get; set; }
        public virtual DbSet<EventLog> eventLog { get; set; }
        public virtual DbSet<ServicePerf> servicePerf { get; set; }

        public MetaverseMaxDbContext(DbContextOptions<MetaverseMaxDbContext> options)
        : base(options)
        {
        }

        public int LogEvent(string logDetail)
        {
            MetaverseMaxDbContext _contextEvent = null;

            // Event log should used a separate context in case problem thrown with a prior call to SaveChanges
            try
            {
                DbContextOptionsBuilder<MetaverseMaxDbContext> options = new();
                _contextEvent = new MetaverseMaxDbContext(options.UseSqlServer(Database.GetConnectionString()).Options);

                _contextEvent.eventLog.Add(new EventLog()
                {
                    detail = logDetail,
                    recorded_time = DateTime.Now
                });

                _contextEvent.SaveChanges();
            }
            catch (Exception ex)
            {
            }
            return 0;
        }

        public int ActionUpdate(ACTION_TYPE actionType)
        {
            MetaverseMaxDbContext _context = null;
            int result = 0;

            // Event log should used a separate context in case problem thrown with a prior call to SaveChanges
            try
            {                
                DbContextOptionsBuilder<MetaverseMaxDbContext> options = new();
                _context = new MetaverseMaxDbContext(options.UseSqlServer(Database.GetConnectionString()).Options);

                result = _context.Database.ExecuteSqlInterpolated($"UPDATE ActionTime set [last_update] = getDate() where [action_type] = {actionType.ToString("G")}");
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("MetaverseMaxDbContext.UpdateAction() : Error updating action datetime for ", actionType.ToString("G")));
                    _context.LogEvent(log);
                }
            }

            return 0;
        }

        public DateTime ActionTimeGet(ACTION_TYPE actionType)
        {
            MetaverseMaxDbContext _context = null;
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

                DbContextOptionsBuilder<MetaverseMaxDbContext> options = new();
                _context = new MetaverseMaxDbContext(options.UseSqlServer(Database.GetConnectionString()).Options);

                result = _context.Database.ExecuteSqlInterpolated($"Select {lastUpdated} = [last_update] from ActionTime where [action_type] = {actionType.ToString("G")}");
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("MetaverseMaxDbContext.UpdateAction() : Error updating action datetime for ", actionType.ToString("G")));
                    _context.LogEvent(log);
                }
            }

            return (DateTime)lastUpdated.Value;
        }

        // Define the decimal precision to match that of sql server column definition. 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DistrictFund>().Property(p => p.balance).HasPrecision(12, 6);           // Set decimal format to match defualt for C#
            modelBuilder.Entity<DistrictFund>().Property(p => p.distribution).HasPrecision(12, 6);
            modelBuilder.Entity<BuildingTypeIP>().Property(p => p.current_price).HasPrecision(18, 2);

            //modelBuilder.Entity<OwnerOffer>().HasKey(p => new { p.offer_id });      // Explicitly set the primary key, as using key from source and not db seed generated.
            modelBuilder.Entity<OwnerOffer>().Property(p => p.buyer_offer).HasPrecision(12, 6);

            // Setup Composite primary keys
            modelBuilder.Entity<Owner>().HasKey(o => new { o.owner_matic_key, o.active_tron }).HasName("PrimaryKey_Owner");

            modelBuilder.Entity<PlotIP>().HasNoKey();            

            base.OnModelCreating(modelBuilder);
        }
    }
}
