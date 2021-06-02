using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Cryptography;
using System;
using System.Text;

namespace ATC.Framework
{
    public static class Utilities
    {
        /// <summary>
        /// Converts a value from one range to another.
        /// </summary>
        /// <param name="currValue">The current value.</param>
        /// <param name="currMax">The current value's maximum.</param>
        /// <param name="currMin">The current value's minimum.</param>
        /// <param name="newMax">The converted value's maximum.</param>
        /// <param name="newMin">The converted value's minimum.</param>
        /// <returns></returns>
        public static int RangeScaler(int currValue, int currMax, int currMin, int newMax, int newMin)
        {
            // check input
            if (currMin >= currMax || newMin >= newMax)
                return 0;

            // clamp values
            if (currValue < currMin)
                currValue = currMin;
            else if (currValue > currMax)
                currValue = currMax;

            // calculate new value
            int currDiff = currMax - currMin;
            int newDiff = newMax - newMin;
            int currAdj = currValue - currMin;
            float percentage = (float)currAdj / (float)currDiff;
            int newValue = (int)(percentage * newDiff + newMin);

            // check output bounds
            if (newValue < newMin)
                newValue = newMin;
            else if (newValue > newMax)
                newValue = newMax;

            return newValue;
        }

        /// <summary>
        /// Converts a value from one range to another.
        /// </summary>
        /// <param name="currValue">The current value.</param>
        /// <param name="currMax">The current value's maximum.</param>
        /// <param name="currMin">The current value's minimum.</param>
        /// <param name="newMax">The converted value's maximum.</param>
        /// <param name="newMin">The converted value's minimum.</param>
        /// <returns></returns>
        public static long RangeScaler(long currValue, long currMax, long currMin, long newMax, long newMin)
        {
            // check input
            if (currMin >= currMax || newMin >= newMax)
                return 0;

            // clamp values
            if (currValue < currMin)
                currValue = currMin;
            else if (currValue > currMax)
                currValue = currMax;

            // calculate new value
            long currDiff = currMax - currMin;
            long newDiff = newMax - newMin;
            long currAdj = currValue - currMin;
            float percentage = (float)currAdj / (float)currDiff;
            long newValue = (long)(percentage * newDiff + newMin);

            // check output bounds
            if (newValue < newMin)
                newValue = newMin;
            else if (newValue > newMax)
                newValue = newMax;

            return newValue;
        }

        /// <summary>
        /// Converts a value from one range to another.
        /// </summary>
        /// <param name="currValue">The current value.</param>
        /// <param name="currMax">The current value's maximum.</param>
        /// <param name="currMin">The current value's minimum.</param>
        /// <param name="newMax">The converted value's maximum.</param>
        /// <param name="newMin">The converted value's minimum.</param>
        /// <returns></returns>
        public static float RangeScaler(float currValue, float currMax, float currMin, float newMax, float newMin)
        {
            // check input
            if (currMin >= currMax || newMin >= newMax)
                return 0;

            // clamp values
            if (currValue < currMin)
                currValue = currMin;
            else if (currValue > currMax)
                currValue = currMax;

            // calculate new value
            float currDiff = currMax - currMin;
            float newDiff = newMax - newMin;
            float currAdj = currValue - currMin;
            double percentage = (double)currAdj / (double)currDiff;
            float newValue = (float)(percentage * newDiff + newMin);

            // check output bounds
            if (newValue < newMin)
                newValue = newMin;
            else if (newValue > newMax)
                newValue = newMax;

            return newValue;
        }

        /// <summary>
        /// Converts a string to hexadecimal representation format.
        /// </summary>
        /// <param name="s">The string to convert.</param>
        /// <returns>Hexadecimal converted string.</returns>
        public static string HexString(string s)
        {
            var sb = new StringBuilder();

            foreach (char c in s)
            {
                sb.AppendFormat("\\x{0:X2}", Convert.ToInt32(c));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts a char to hexadecimal representation format.
        /// </summary>
        /// <param name="c">The char to convert.</param>
        /// <returns>Hexadecimal converted string.</returns>
        public static string HexString(char c)
        {
            return string.Format("\\x{0:X2}", c);
        }

        /// <summary>
        /// Converts a string to hexadecimal representation format.
        /// </summary>
        /// <param name="s">The string to convert.</param>
        /// <returns>Hexadecimal converted string.</returns>
        public static string ToHexString(this string s)
        {
            return HexString(s);
        }

        /// <summary>
        /// Converts a string with control characters into a printable string with hex representation of control characters
        /// </summary>
        /// <param name="s">The string to convert.</param>
        /// <returns>String with control codes represented in human readable form.</returns>
        public static string ControlCodeString(string s)
        {
            var sb = new StringBuilder();

            foreach (char c in s)
            {
                if (Char.IsControl(c))
                    sb.AppendFormat("\\x{0:X2}", Convert.ToInt32(c));
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts a string with control characters into a printable string with hex representation of control characters
        /// </summary>
        /// <param name="s">The string to convert.</param>
        /// <returns>String with control codes represented in human readable form.</returns>
        public static string ToControlCodeString(this string s)
        {
            return ControlCodeString(s);
        }

        /// <summary>
        /// Returns the working directory of the running application.
        /// </summary>
        /// <returns></returns>
        public static string ApplicationDirectory()
        {
            return Directory.GetApplicationDirectory();
        }

        /// <summary>
        /// Returns any string between found between the start and end characters.
        /// </summary>
        /// <param name="value">The string to process</param>
        /// <param name="startChar">The starting character (not included in output)</param>
        /// <param name="endChar">The ending character (not included in output)</param>
        public static string GetInnerString(string value, char startChar, char endChar)
        {
            // validate input
            if (string.IsNullOrEmpty(value) || value.Length <= 2)
                throw new ArgumentException("Invalid arguments");

            // get start index
            var startIndex = value.IndexOf(startChar);
            if (startIndex == -1)
                throw new ArgumentException("Start character not found.", "startChar");

            // get end index
            var endIndex = value.LastIndexOf(endChar);
            if (endIndex == -1)
                throw new ArgumentException("End character not found.", "endChar");

            var length = endIndex - startIndex;
            var innerString = value.Substring(startIndex + 1, length - 1);

            return innerString;
        }

        /// <summary>
        /// Returns the control systems IP address in string format from the adapter that corresponds to LAN adapter.
        /// </summary>
        public static string GetControllerLanIpAddress()
        {
            return CrestronEthernetHelper.GetEthernetParameter(
                CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS,
                CrestronEthernetHelper.GetAdapterdIdForSpecifiedAdapterType(EthernetAdapterType.EthernetLANAdapter));
        }


        /// <summary>
        /// Calculate an MD5 hash for the input string and return a hexadecimal string.
        /// </summary>
        /// <param name="input">The input string to hash.</param>
        /// <returns>Hexadecimal MD5 hashed string.</returns>
        public static string CalculateMD5(string input)
        {
            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // covert byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                    sb.Append(hashBytes[i].ToString("X2"));

                return sb.ToString().ToLower(); // convert hash to lower case hex string
            }
        }

        /// <summary>
        /// Convert an integer into hexadecimal string representing the number.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="stringLength">Will pad with preceeding zeroes up to this length</param>
        public static string ToHex(this int value, int length)
        {
            return ToHex(value, length, false);
        }

        /// <summary>
        /// Convert an integer into hexadecimal string representing the number.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="stringLength">Will pad with preceeding zeroes up to this length</param>
        /// <param name="forceTruncateToLength">If false, allows results longer than the specified length, otherwise larger values will have the big end truncated, and the little end will be kept</param>
        public static string ToHex(this int value, int length, bool forceTruncateToLength)
        {
            byte[] bytes = BitConverter.GetBytes(value);                                            // Get the value as bytes
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);                                  // If the BitConverter is working in Little Endian mode, reverse the array

            int start = 0;                                                                          // Start at the left end by default
            if (forceTruncateToLength && (bytes.Length > length)) start = bytes.Length - length;    // If the converted result is longer than the specified length, discard the big end (MSBs)

            int count = bytes.Length;                                                               // Set the number of bytes we will encode
            if (forceTruncateToLength && (count > length)) count = length;                          // If the count is greater than the specified length, limit it at that

            string result = Encoding.GetEncoding("ISO-8859-1").GetString(bytes, start, count);      // Convert the byte array into a string with full 8 bit encoding (not 7 bit UTF8, as that only goes to 127, 8 bit goes to 255)

            // We need to check the result length, even if we're not force truncating to the specified length, as the BitConverter always adds preceeding zeros anyway up to the byte lenght of the type that is supplied to it (eg int will be 4 bytes long).
            if ((!forceTruncateToLength) && (result.Length > length))                               // If we're NOT truncating to length, but the result is longer than the specified length
            {
                while (result.Length > length)                                                      // While we're still longer than the specified lenght
                {
                    if (result[0] == '\x00')                                                        // Check for and remove any preceeding \x00 bytes only
                        result = result.Substring(1, result.Length - 1);
                    else
                        break;                                                                      // If a non \x00 byte is found, exit regardless of whether we're longer than the 
                }
            }

            if (result.Length < length)                                                             // If the result is less than the specified length
            {
                count = length - result.Length;                                                     // Detect how much padding is needed
                result = new string('\x00', count) + result;                                        // Pad 0 bytes in front of the converted string
            }

            return result;
        }
    }
}
