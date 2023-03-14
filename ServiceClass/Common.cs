﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class Common
    {
        public WORLD_TYPE IdentifyWorld(string url)
        {
            WORLD_TYPE identifiedWorld = WORLD_TYPE.TRON;

            if (url.ToLower().Contains("/api/trx/"))
            {
                identifiedWorld = WORLD_TYPE.TRON;
            }
            else if(url.ToLower().Contains("/api/bnb/"))
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
            
            DateTime? dtConvertedTime = TimeFormatStandardDT("", dtSourceTime);
            
            if (dtConvertedTime != null)
            {
                timeFormated = ((DateTime)dtConvertedTime).ToString("yyyy/MMM/dd");
            }

            return timeFormated;
        }

        public string TimeFormatStandard(string sourceTime, DateTime? dtSourceTime)
        {
            string timeFormated = string.Empty;
            DateTime? dtConvertedTime = null;

            if (sourceTime == null)
            {
                timeFormated = "Not Found";
            }
            else
            {
                dtConvertedTime = TimeFormatStandardDT(sourceTime, dtSourceTime);
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

        public DateTime? TimeFormatStandardDT(string sourceTime, DateTime? dtSourceTimeUTC)
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
                dateFormated = TimeFormatStandard("", dtDateTime.AddSeconds((double)unixTimeStamp));

            }
            else
            {
                dateFormated = noTime;
            }
            return dateFormated;
        }
    }
}
