using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public class DBLogger
    {
        private readonly MetaverseMaxDbContext _context;

        // Protected base method, can only be accessed via code(methods) from same class or derived class. 
        public DBLogger(MetaverseMaxDbContext _parentContext)
        {
            _context = _parentContext;
        }

        public int logException(Exception ex, string primaryLogEntry)
        {

            string log = string.Concat(ex.Message, " Inner: ", ex.InnerException != null ? ex.InnerException.Message : "");
            log = log.Substring(0, log.Length > 500 ? 500 : log.Length);

            if (_context != null)
            {
                _context.LogEvent(primaryLogEntry);
                _context.LogEvent(log);
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
