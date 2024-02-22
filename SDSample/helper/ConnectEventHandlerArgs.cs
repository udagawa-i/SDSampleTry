using Newtonsoft.Json;
using System;

namespace SoundDesigner.Helper
{
    public class ConnectEventRootobject
    {
        public ConnectData[] Event { get; set; }
    }

    public class ConnectData
    {
        public string ConnectionState { get; set; }
        public string DeviceID { get; set; }
        public string ErrorDesc { get; set; }
    }
    public class ConnectEventHandlerArgs : EventArgs
    {
        private readonly string _eventdata;

        public ConnectEventHandlerArgs(string eventdata)
        {
            _eventdata = eventdata;
        }

        public string EventData
        {
            get { return _eventdata; }
        }

        public ConnectData ParseEventArgs()
        {
            try
            {
                var sro = JsonConvert.DeserializeObject<ConnectEventRootobject>(_eventdata);
                var retval = new ConnectData();
                retval.DeviceID = sro.Event[0].DeviceID;
                retval.ConnectionState = sro.Event[1].ConnectionState;
                retval.ErrorDesc = sro.Event[2].ErrorDesc;
                return retval;
            }
            catch (Exception e)
            {
                throw new Exception($"Error parsing connect data: {e.Message}");
            }
        }


    }
}
