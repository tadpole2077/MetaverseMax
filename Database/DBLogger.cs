using MetaverseMax.ServiceClass;

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

            //CHECK if current db context is an active db connection.
            if (_context == null || _context.IsDisposed())
            {
                // Generate a new dbContext as a safety measure - insuring log is recorded.
                using (var _contextEvent = new MetaverseMaxDbContext(worldType))
                {
                    // Additional log entry if context was pased but found to be disposed already
                    if (noContextPassed == false)
                    {
                        _contextEvent.eventLog.Add(new EventLog() { detail = ("DBLogger::logException() : WARNING - DB Context lost & Recreated"), recorded_time = DateTime.UtcNow });
                    }

                    _contextEvent.eventLog.Add(new EventLog() { detail = primaryLogEntry, recorded_time = DateTime.UtcNow });
                    _contextEvent.eventLog.Add(new EventLog() { detail = log, recorded_time = DateTime.UtcNow });

                    _contextEvent.SaveChanges();
                }
            }
            else
            {
                _context.eventLog.Add(new EventLog() { detail = primaryLogEntry, recorded_time = DateTime.UtcNow });
                _context.eventLog.Add(new EventLog() { detail = log, recorded_time = DateTime.UtcNow });
                _context.SaveChanges();
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
