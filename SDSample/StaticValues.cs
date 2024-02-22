using SoundDesigner.Helper;
using System.Collections.Generic;

namespace SDSample
{
    public static class StaticValues
    {
        public static string WirelessDeviceName1 = "";
        public static string WirelessDeviceName2 = "";
        public static List<(string, string)> ScanList = new List<(string, string)>();      //Append
        public static string EventInfoData = "";
        public static string EventInfoData2 = "";
        public static string EventInfoData3 = "";

        //Append
        public static bool WirelessScanFlag = true;
        public static List<ScanData> ScanDatas = new List<ScanData>();
        public static ScanData ScanEventLeft = new ScanData();
        public static ScanData ScanEventRight = new ScanData();



        public static int Clear()
        {

            WirelessDeviceName1 = "";
            WirelessDeviceName1 = "";
            ScanList.Clear();

            EventInfoData = "";
            EventInfoData2 = "";
            EventInfoData3 = "";

            ScanDatas.Clear();
            ScanEventLeft = new ScanData();
            ScanEventRight = new ScanData();

            return 0;
        }
    }
}
