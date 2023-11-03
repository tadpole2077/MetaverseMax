using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace MetaverseMax.Database
{
    public class JobSettingDB : DatabaseBase
    {
        public JobSettingDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }

        public JobSetting AddOrUpdate(string settingName, int settingValue, string updateBy)
        {
            JobSetting jobSetting = null; 
            bool newRecord = false;

            try
            {
                jobSetting = _context.JobSetting.Where(x => x.setting_name == settingName).FirstOrDefault();

      
                if (jobSetting == null)
                {
                    newRecord = true;
                    jobSetting = new();
                    jobSetting.setting_name = settingName;
                }
                jobSetting.setting_value = settingValue;
                jobSetting.update_by = updateBy;
                jobSetting.last_update = DateTime.Now;

                if (newRecord) {
                    _context.JobSetting.Add(jobSetting);
                }
                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("JobSettingDB::AddOrUpdate() : Error adding/updating setting ", settingName));
            }

            return jobSetting;
        }

        public int GetSettingValue(string settingName)
        {
            JobSetting jobSetting = null;
            int value = 0;

            try
            {
                jobSetting = _context.JobSetting.Where(x => x.setting_name == settingName).FirstOrDefault();
                value = jobSetting.setting_value;

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("JobSettingDB::AddOrUpdate() : Error adding/updating setting ", settingName));
            }

            return value;
        }
    }
}
