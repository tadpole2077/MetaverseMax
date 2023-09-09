using System.Globalization;
using MetaverseMax.BaseClass;

namespace MetaverseMax.ServiceClass
{
    public class Common
    {
        public static bool isDevelopment { get; set; }
        public static string serverIP { get; set; }
        public static bool logServiceInfo { get; set; }
        public static bool showPrediction { get; set; }
        public static int jobFundRepeatCount { get; set; }
        public static string dbConnectionStringTron { get; set; }
        public static string dbConnectionStringBNB { get; set; }
        public static string dbConnectionStringETH { get; set; }
        public static int dbCommandTimeout { get; set; }

        public static void AssignSetting(bool isDevelopmentFlag)
        {
            string appSettingFileName = "appsettings.json";
            isDevelopment = isDevelopmentFlag;
            if (isDevelopment)
            {
                appSettingFileName = "appsettings.Development.json";
            }


            // Get Configuration Settings 
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(appSettingFileName)
                .Build();

            // Hook into the appsettings.json file to pull database and app settings used by services - within published site pulling from web.config
            serverIP = configuration["ServerIP"];
            logServiceInfo = configuration["logServiceInfo"] == "1";
            showPrediction = configuration["showPrediction"] == "1";
            jobFundRepeatCount = Convert.ToInt32(configuration["jobFundRepeatCount"]);
            dbConnectionStringTron = configuration.GetConnectionString("DatabaseConnection");
            dbConnectionStringBNB = configuration.GetConnectionString("DatabaseConnectionBNB");
            dbConnectionStringETH = configuration.GetConnectionString("DatabaseConnectionETH");
            dbCommandTimeout = (int)configuration.GetValue(typeof(int), "DBCommandTimeout");
        }

        public WORLD_TYPE IdentifyWorld(string url)
        {
            WORLD_TYPE identifiedWorld = WORLD_TYPE.TRON;

            if (url.ToLower().Contains("/api/trx/"))
            {
                identifiedWorld = WORLD_TYPE.TRON;
            }
            else if (url.ToLower().Contains("/api/bnb/"))
            {
                identifiedWorld = WORLD_TYPE.BNB;
            }
            else if (url.ToLower().Contains("/api/eth/"))
            {
                identifiedWorld = WORLD_TYPE.ETH;
            }

            return identifiedWorld;
        }
        public string DateFormatStandard(DateTime? dtSourceTime)
        {
            string timeFormated = string.Empty;

            DateTime? dtConvertedTime = TimeFormatStandardFromUTC("", dtSourceTime);

            if (dtConvertedTime != null)
            {
                timeFormated = ((DateTime)dtConvertedTime).ToString("yyyy/MMM/dd");
            }

            return timeFormated;
        }

        public string LocalTimeFormatStandardFromUTC(string sourceTime, DateTime? dtSourceTime)
        {
            string timeFormated = string.Empty;
            DateTime? dtConvertedTime = null;

            if (sourceTime == null)
            {
                timeFormated = "Not Found";
            }
            else
            {
                dtConvertedTime = TimeFormatStandardFromUTC(sourceTime, dtSourceTime);
                if (dtConvertedTime != null)
                {
                    timeFormated = ((DateTime)dtConvertedTime).ToString("yyyy/MMM/dd HH:mm:ss");
                }
            }

            return timeFormated;
        }

        public string DateStandard(DateTime? dtSource)
        {
            string timeFormated = string.Empty;

            if (dtSource != null)
            {
                timeFormated = ((DateTime)dtSource).ToString("yyyy/MMM/dd");
            }

            return timeFormated;
        }

        public DateTime? TimeFormatStandardFromUTC(string sourceTime, DateTime? dtSourceTimeUTC)
        {
            DateTime? dateTimeUTC = null;
            DateTime? gmtDateTime = null;

            if (!string.IsNullOrEmpty(sourceTime))
            {
                dateTimeUTC = ConvertDateTimeUTC(sourceTime);
            }
            else if (dtSourceTimeUTC == null)  // No Datetime passed, or string time - return null - assign null to db as safest option. 
            {
                dateTimeUTC = null;
            }
            else
            {
                dateTimeUTC = DateTime.SpecifyKind((DateTime)dtSourceTimeUTC, DateTimeKind.Utc);
            }

            // Convert from UTC to Local GMT, and format.
            if (dateTimeUTC != null)
            {
                string gmtTimeZoneKey = "GMT Standard Time";
                TimeZoneInfo gmtTimeZone = TimeZoneInfo.FindSystemTimeZoneById(gmtTimeZoneKey);
                gmtDateTime = TimeZoneInfo.ConvertTimeFromUtc((DateTime)dateTimeUTC, gmtTimeZone);
            }

            return gmtDateTime;
        }

        public DateTime? ConvertDateTimeUTC(string sourceTime)
        {
            DateTime? dateTimeUTC = null;

            if (!string.IsNullOrEmpty(sourceTime))
            {
                dateTimeUTC = DateTime.ParseExact(sourceTime,
                    "MM/dd/yyyy HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            }

            return dateTimeUTC;
        }



        public string WalletConvert(string maticKey)
        {
            string walletPublicKey = maticKey;

            if (maticKey.Length > 0)
            {
                walletPublicKey = HexToASCII(maticKey.Substring(4));
            }

            return walletPublicKey;
        }

        public long Extract64HexChunkToNumeric(string source, int chunkSeqNum)
        {
            string chunk = source.Substring(64 * chunkSeqNum, 64);
            return Convert.ToInt64(chunk,16);
        }

        public static string HexToASCII(string hex)
        {
            // initialize the ASCII code string as empty.
            string ascii = "";

            for (int i = 0; i < hex.Length; i += 2)
            {

                // extract two characters from hex string
                string part = hex.Substring(i, 2);

                // change it into base 16 and 
                // typecast as the character
                char ch = (char)Convert.ToInt32(part, 16); ;

                // add this char to final ASCII string
                ascii = ascii + ch;
            }
            return ascii;
        }

        public string UnixTimeStampUTCToDateTime(double? unixTimeStamp, String noTime)
        {
            string dateFormated;

            // Unix timestamp is seconds past epoch
            if (unixTimeStamp != null)
            {
                System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                dateFormated = LocalTimeFormatStandardFromUTC("", dtDateTime.AddSeconds((double)unixTimeStamp));

            }
            else
            {
                dateFormated = noTime;
            }
            return dateFormated;
        }

        public string LookupTokenType(int typeId)
        {
            string tokenType = string.Empty;

            tokenType = typeId switch
            {
                (int)TOKEN_TYPE.PLOT => "Plot",
                (int)TOKEN_TYPE.APPLICATION => "Application",
                (int)TOKEN_TYPE.CAR => "Car",
                (int)TOKEN_TYPE.CITIZEN => "Citizen",
                (int)TOKEN_TYPE.DISTRICT => "District",
                (int)TOKEN_TYPE.PET => "Pet",
                (int)TOKEN_TYPE.RESOURCE => "Resource",
                _ => "Unknown",
            };

            return tokenType;
        }
    }
}
