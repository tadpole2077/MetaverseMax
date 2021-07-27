using MetaverseMax.ServiceClass;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public partial class MetaverseMaxDbContext : DbContext
    {
        public virtual DbSet<Plot> plot { get; set; }

        public virtual DbSet<District> district { get; set; }

        public virtual DbSet<DistrictFund> districtFund { get; set; }

        public virtual DbSet<DistrictPerk> districtPerk { get; set; }

        public virtual DbSet<DistrictContent> districtContent { get; set; }

        //public virtual DbSet<DistrictUpdateInstance> districtUpdateInstance { get; set; }

        public virtual DbSet<OwnerSummaryDistrict> ownerSummaryDistrict { get; set; }

        public virtual DbSet<Owner> owner { get; set; }

        public virtual DbSet<OwnerOffer> ownerOffer { get; set; }

        public virtual DbSet<Pet> pet { get; set; }

        public virtual DbSet<EventLog> eventLog { get; set; }

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

        // Define the decimal precision to match that of sql server column definition. 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DistrictFund>().Property(p => p.balance).HasPrecision(12, 6);           // Set decimal format to match defualt for C#
            modelBuilder.Entity<DistrictFund>().Property(p => p.distribution).HasPrecision(12, 6);

            //modelBuilder.Entity<OwnerOffer>().HasKey(p => new { p.offer_id });      // Explicitly set the primary key, as using key from source and not db seed generated.
            modelBuilder.Entity<OwnerOffer>().Property(p => p.buyer_offer).HasPrecision(12, 6);

            base.OnModelCreating(modelBuilder);
        }
    }
}
