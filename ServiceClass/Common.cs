using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class Common
    {
        public string TimeFormatStandard(string sourceTime)
        {
            string timeFormated;
            if (sourceTime == null)
            {
                timeFormated = "Not Found";
            }
            else
            {
                DateTime dtDateTime = new DateTime(Convert.ToInt32(sourceTime.Substring(6, 4)), Convert.ToInt32(sourceTime.Substring(0, 2)), Convert.ToInt32(sourceTime.Substring(3, 2)));
                timeFormated = string.Concat(dtDateTime.ToString("yyyy/MMM/dd"), " ", sourceTime[12..]);
                //timeFormated = (DateTime)sourceTime).ToString("yyyy/MMM/dd HH:mm:ss");
            }

            return timeFormated;
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

        public string UnixTimeStampToDateTime(double? unixTimeStamp, String noTime)
        {
            string dateFormated;

            // Unix timestamp is seconds past epoch
            if (unixTimeStamp != null)
            {
                System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddSeconds((double)unixTimeStamp).ToLocalTime();
                dateFormated = dtDateTime.ToString("yyyy/MMM/dd HH:mm:ss");
            }
            else
            {
                dateFormated = noTime;
            }
            return dateFormated;
        }
    }
}
