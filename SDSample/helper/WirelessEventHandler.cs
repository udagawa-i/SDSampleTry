using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SDLib;
using SDSample;

namespace SoundDesigner.Helper
{

    public class WirelessEventHandler
    {
        private bool _keepeventhandlerrunning;
        private readonly IEventHandler _sdeventhandler; 
        public event EventHandler<ScanEventHandlerArgs> ScanEvent;
        public event EventHandler<ConnectEventHandlerArgs> ConnectionEvent; 
    
        public void Start () 
        { 
            _keepeventhandlerrunning = true; 
            Task.Run(() => RunEventHandler());
        }

        public void Stop () 
        { 
            _keepeventhandlerrunning = false; 
        }

        private async Task RunEventHandler()
        { 
            while ( _keepeventhandlerrunning ) 
            {
                await Task.Delay(50); 
                var sdevent  =  _sdeventhandler.GetEvent();

                TestContext.Progress.WriteLine($"SD event rx: { sdevent.Data}");
                StaticValues.EventInfoData += $"SD event rx: {sdevent.Data}\n";     //Append By I.U

                switch (sdevent.Type)
                {
                    case EventType.kUnknownEvent:
                        break;
                    case EventType.kVolumeEvent:
                        break;
                    case EventType.kScanEvent:
                        ScanEvent?.Invoke(this, new ScanEventHandlerArgs(sdevent.Data)); 
                        break;
                    case EventType.kConnectionEvent:
                        ConnectionEvent?.Invoke(this, new ConnectEventHandlerArgs(sdevent.Data));
                        break;
                    case EventType.kActiveEvent:
                        break;
                    case EventType.kMemoryEvent:
                        break;
                    case EventType.kBatteryEvent:
                        break;
                    case EventType.kAuxAttenuationEvent:
                        break;
                    case EventType.kMicAttenuationEvent:
                        break;
                }
            }
        }

        public WirelessEventHandler(IEventHandler sdeventhandler)
        {
            _sdeventhandler = sdeventhandler; 
        }
    }
}
