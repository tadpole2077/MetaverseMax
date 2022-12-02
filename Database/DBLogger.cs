using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MetaverseMax.Database
{
    public class DBLogger
    {
        public MetaverseMaxDbContext _context;
        public readonly string dbConnectionString;

        // Protected base method, can only be accessed via code(methods) from same class or derived class. 
        public DBLogger(MetaverseMaxDbContext _parentContext)
        {
            _context = _parentContext;
            dbConnectionString = _context.Database.GetConnectionString();
        }

        public int logException(Exception ex, string primaryLogEntry)
        {

            string log = string.Concat(ex.Message, " Inner: ", ex.InnerException != null ? ex.InnerException.Message : "");
            log = log.Substring(0, log.Length > 500 ? 500 : log.Length);

            if (_context == null)
            {
                DbContextOptionsBuilder<MetaverseMaxDbContext> options = new();
                _context = new MetaverseMaxDbContext(options.UseSqlServer(dbConnectionString).Options);
                _context.LogEvent(String.Concat("DBLogger::logException() : WARNING - DB Context lost & Recreated"));
            }

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
