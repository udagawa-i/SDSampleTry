using System;
using System.Text.RegularExpressions;


namespace SoundDesigner.Helper
{
    public static class TextUtil
    {
        public static bool ValidateHexString(string hexString, int lengthInBits)
        {
            if (lengthInBits % 4 != 0)
            {
                return false;
            }
            int maxSize = lengthInBits / 4; 
            bool isHex = Regex.IsMatch(hexString, @"\A\b[0-9a-fA-F]+\b\Z");
            if (!isHex || (hexString.Length > maxSize))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private static string[] SplitHexString(string hexString, int lengthInBits, int splitNumber)
        {
            int lengthInHexChars = lengthInBits / 4;
            int maxSizeInHexChars = lengthInHexChars / splitNumber;
            string[] splitString = new string[splitNumber];
            string segment = hexString;

            for (int i = 0; i < splitNumber; i++)
            {
                if (segment.Length > maxSizeInHexChars)
                {
                    splitString[i] = segment.Substring(segment.Length - maxSizeInHexChars, maxSizeInHexChars);
                    segment = segment.Substring(0, segment.Length - maxSizeInHexChars);
                }
                else if (segment.Length > 0)
                {
                    splitString[i] = segment;
                    segment = "";
                }
                else
                {
                    splitString[i] = "0";
                }
            }
            return splitString;
        }
        
        public static int[] ConvertHexStringToIntArray(string hexString, int lengthInBits, int splitNumber)
        {
            string[] splitString;
            int[] splitInt;
            if (hexString == null)
            {
                return null;
            }
            if (ValidateHexString(hexString, lengthInBits))
            {
                splitString = SplitHexString(hexString, lengthInBits, splitNumber);
                splitInt = new int[splitString.Length];
                for (int i = 0; i < splitString.Length; i++)
                {
                    splitInt[i] = Int32.Parse(splitString[i], System.Globalization.NumberStyles.HexNumber);
                }
                return splitInt;
            }
            else
            {
                return null;
            }
        }
    }
}
