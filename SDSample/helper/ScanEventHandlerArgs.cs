using System;
using Newtonsoft.Json;


namespace SoundDesigner.Helper
{
    public class ScanEventRootobject
    {
        public ScanData[] Event { get; set; }
    }

    public class ScanData
    {
        public string DeviceName { get; set; }
        public string DeviceID { get; set; }
        public string RSSI { get; set; }
        public string ManufacturingData { get; set; }
    }

    public class ScanEventHandlerArgs : EventArgs
    {
        private readonly string _eventdata;

        public ScanEventHandlerArgs(string eventdata)
        {
            _eventdata = eventdata;
        }

        public string EventData
        {
            get { return _eventdata; }
        }

        public ScanData ParseEventArgs()
        {
            try
            {
                var sro = JsonConvert.DeserializeObject<ScanEventRootobject>(_eventdata);
                var retval = new ScanData();
                retval.DeviceName = sro.Event[0].DeviceName;
                retval.DeviceID = sro.Event[1].DeviceID;
                retval.RSSI = sro.Event[2].RSSI;
                retval.ManufacturingData = sro.Event[3].ManufacturingData;
                return retval; 
            }
            catch (Exception e)
            {
                throw new Exception($"Error parsing scan data: {e.Message}");
            }
        }

    }
}
