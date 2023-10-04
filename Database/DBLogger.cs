using MetaverseMax.BaseClass;

namespace MetaverseMax.Database
{
    public class DBLogger
    {
        public MetaverseMaxDbContext _context;
        private WORLD_TYPE worldType;
        private bool noContextPassed = false;

        // Protected base method, can only be accessed via code(methods) from same class or derived class. 
        public DBLogger(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected)
        {
            _context = _parentContext;
            worldType = worldTypeSelected;
        }
        public DBLogger(MetaverseMaxDbContext _parentContext)
        {
            _context = _parentContext;
            worldType = _parentContext.worldTypeSelected;
        }
        public DBLogger(WORLD_TYPE worldTypeSelected)
        {
            worldType = worldTypeSelected;
            noContextPassed = true;
        }

        public int logException(Exception ex, string primaryLogEntry)
        {

            string log = string.Concat(ex.Message, " Inner: ", ex.InnerException != null ? ex.InnerException.Message : "");
            log = log.Substring(0, log.Length > 500 ? 500 : log.Length);
            primaryLogEntry = primaryLogEntry.Substring(0, primaryLogEntry.Length > 500 ? 500 : primaryLogEntry.Length);

            // Always create a new context, existing context and may have an blocking issue [such as an error due to adding duplicate row] which would prevent the adding the exception record.
            // Generate a new dbContext as a safety measure - insuring log is recorded.
            using (var _contextEvent = new MetaverseMaxDbContext(worldType))
            {
                _contextEvent.eventLog.Add(new EventLog() { detail = primaryLogEntry, recorded_time = DateTime.UtcNow });
                _contextEvent.eventLog.Add(new EventLog() { detail = log, recorded_time = DateTime.UtcNow });

                _contextEvent.SaveChanges();
            }

            return 0;
        }

        public int logInfo(string primaryLogEntry)
        {
            string log = string.Concat("SYSTEM INFO: ", primaryLogEntry);
            log = log.Substring(0, log.Length > 500 ? 500 : log.Length);

            if (_context != null)
            {
                _context.LogEvent(log);
            }

            return 0;
        }
    }
}
