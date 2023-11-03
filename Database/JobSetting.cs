using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("JobSetting")]
    public class JobSetting
    {
        [Key]
        [Column("setting_name")]
        public string setting_name { get; set; }

        [Column("setting_value")]
        public int setting_value { get; set; }

        [Column("last_update")]
        public DateTime last_update { get; set; }

        [Column("update_by")]
        public string update_by { get; set; }
    }
}