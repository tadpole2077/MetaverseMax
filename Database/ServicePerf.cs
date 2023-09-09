using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("ServicePerf")]
    public class ServicePerf
    {
        [Key]
        [Column("service_key")]
        public long service_key { get; set; }

        [Column("service_url")]
        public string service_url { get; set; }

        [Column("start_time")]
        public DateTime start_time { get; set; }

        [Column("run_time")]
        public int run_time { get; set; }

        [Column("response_size")]
        public int response_size { get; set; }

        [Column("service_param")]
        public string service_param { get; set; }

    }
}
