namespace SDSample
{

    public class Constants
    {
        // Required 
        //public const string ProductLibLocation = ".//..//..//..//..//..//..//..//..//products//";
        public const string ProductLibLocation = "C:\\Program Files (x86)\\ON Semiconductor\\SoundDesignerSDK\\products\\";

        //public static string ConnectedDevice = "E7160SL_V1.15";
        public const string ConnectedDevice = "SXx-Library3";   //"E7160SL" / "E7111V2" / "SXx-Library3"

        //public static string Programmer = "RSL10"; //"Noahlink" //"Communication Accelerator Adaptor" //"DSP3" //"HI-PRO" //"Promira"
        public static string Programmer = "DSP3"; //"Noahlink" //"Communication Accelerator Adaptor" //"DSP3" //"HI-PRO" //"Promira"

        // Required for Wireless
        public static bool IsProgrammerWireless = false;  // set to true if choosing the Noahlink or RSL10 
        public const string WirelessDeviceName = "7160SL_iOS*"; //"7160SL"; //"7160SL_iOS";
        public const int ScanTimeMs = 15000 / 3;
        public const int ConnectTimeMs = 5000 * 2;
        public const string DriverLocation = "%USERPROFILE%/.sounddesigner/nlw"; // Required for NoahLink
        public static string COMPort = "COM10"; // Required for RSL10 

        // Optional 
        public const string ParameterLockKey = "";
        public const bool UpgradeDevice = false;
        public const bool DowngradeDevice = false;
    }


    /*************************
    public class Constants
    {
        // Required 
        public const string ProductLibLocation = "C:\\udagawa\\■(7)Visual Studio .NET\\Visual C#\\products\\";
        //public const string ProductLibLocation = "C:\\udagawa\\VisualStudio\\Visual C#\\products\\";

        //public const string ConnectedDevice = "E7111V2";      //"E7160SL" / "E7111V2" / "SXx-Library3"
        public const string ConnectedDevice = "SXx-Library3";   //"E7160SL" / "E7111V2" / "SXx-Library3"
        //public const string Programmer = "Communication Accelerator Adaptor"; //"Noahlink" //"Communication Accelerator Adaptor" //"DSP3" //"HI-PRO" //"Promira"
        public const string Programmer = "DSP3"; //"Noahlink" //"Communication Accelerator Adaptor" //"DSP3" //"HI-PRO" //"Promira"

        // Required for Wireless
        public const bool IsProgrammerWireless = false;  // set to true if choosing the Noahlink or RSL10 
        public const string WirelessDeviceName = "LE-Bose Revolve+ II SoundL"; //"7160SL_iOS";
        public const int ScanTimeMs = 15000;
        public const int ConnectTimeMs = 5000;
        public const string DriverLocation = "%USERPROFILE%/.sounddesigner/nlw"; // Required for NoahLink
        public const string COMPort = "COM5"; // Required for RSL10 

        // Optional 
        public const string ParameterLockKey = "";
        public const bool UpgradeDevice = true;     //true　SXx-Lib3 -> E111V2 アップグレード対応
        public const bool DowngradeDevice = true;   //false  E7111V2 -> SXx-Lib3 ダウングレード対応 
    }
    ******************************/
}
