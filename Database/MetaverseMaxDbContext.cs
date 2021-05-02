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

        public virtual DbSet<DistrictContent> districtContent { get; set; }

        public virtual DbSet<OwnerSummaryDistrict> ownerSummaryDistrict { get; set; }

        public virtual DbSet<EventLog> eventLog { get; set; }

        public MetaverseMaxDbContext(DbContextOptions<MetaverseMaxDbContext> options)
        : base(options)
        {
        }

        public int LogEvent(string logDetail)
        {
            try
            {
                eventLog.Add(new EventLog()
                {
                    detail = logDetail,
                    recorded_time = DateTime.Now
                });

                SaveChanges();
            }
            catch (Exception ex)
            {
            }
            return 0;
        }
            
    }
}
