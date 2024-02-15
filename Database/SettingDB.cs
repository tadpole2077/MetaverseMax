using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace MetaverseMax.Database
{
    public class SettingDB : DatabaseBase
    {
        public new MetaverseMaxDbContext_UNI _context;

        public SettingDB(MetaverseMaxDbContext_UNI _parentContext) : base()
        {
            _context = _parentContext;
        }

        public Setting AddOrUpdate(string settingName, int settingValue, string updateBy)
        {
            Setting setting = null; 
            bool newRecord = false;

            try
            {
                setting = _context.setting.Where(x => x.setting_name == settingName).FirstOrDefault();

      
                if (setting == null)
                {
                    newRecord = true;
                    setting = new();
                    setting.setting_name = settingName;
                }
                setting.setting_value = settingValue;
                setting.update_by = updateBy;
                setting.last_update = DateTime.Now;

                if (newRecord) {
                    _context.setting.Add(setting);
                }
                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("SettingDB::AddOrUpdate() : Error adding/updating setting ", settingName));
            }

            return setting;
        }

        public int GetSettingValue(string settingName)
        {
            Setting setting = null;
            int value = 0;

            try
            {
                setting = _context.setting.Where(x => x.setting_name == settingName).FirstOrDefault();
                value = setting.setting_value;

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("SettingDB::AddOrUpdate() : Error adding/updating setting ", settingName));
            }

            return value;
        }
    }
}
