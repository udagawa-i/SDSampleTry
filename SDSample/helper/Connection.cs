using NUnit.Framework;
using SDLib;
using SDSample;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SoundDesigner.Helper
{
    public class Connection
    {
        private IProductManager _productmanager;
        private CommunicationPort _port;
        private string _programmer;
        private readonly IProgress<int> _progress;
        private readonly CancellationToken _canceltoken;
        private WirelessEventHandler _eventhandler;
        private IEventHandler _sdeventhandler;
        private ScanData ScanEvent;

        public int ScanMethod = 0;     //追加　0:従来通り　1:スキャンのみ

        enum WirelessConnectionState
        {
            disconnecting, disconnected, connecting, connected, error
        }

        public ICommunicationAdaptor CommAdaptor { get; set; }

        public IDeviceInfo ConnectedDeviceInfo { get; set; }

        public bool ParametersUnlocked { get; set; }

        public enum WirelessConnectStage
        {
            Scan, ScanCompleted, Connecting, Connected, Cancelled, Disconnected, Disconnecting
        }

        public WirelessConnectStage Stage { get; set; }

        public bool IsConnected { get; set; }

        public bool IsConnecting { get; set; }

        public bool VerifyWrites { get; set; }

        public CancellationToken Canceltoken => _canceltoken;

        public async Task CloseWirelessConnection()
        {
            if (IsConnected)
            {
                try
                {
                    TestContext.Progress.WriteLine($"Disconnecting {ScanEvent.DeviceName}..");
                    Stage = WirelessConnectStage.Disconnecting;
                    _eventhandler.ConnectionEvent += _eventhandler_ConnectEvent;
                    CommAdaptor.Disconnect();
                    var stopwatch = Stopwatch.StartNew();
                    while (stopwatch.ElapsedMilliseconds < Constants.ConnectTimeMs)
                    {
                        await Task.Delay(200);
                        if (Stage == WirelessConnectStage.Disconnected || Stage == WirelessConnectStage.Cancelled)
                        {
                            break;
                        }
                    }

                    if (Stage != WirelessConnectStage.Disconnected)
                    {
                        TestContext.Progress.WriteLine($"Disconnect timed out, time elapsed: {stopwatch.ElapsedMilliseconds} ms");
                        throw new Exception($"Device not disconnected: {Constants.WirelessDeviceName}");
                    }
                    _eventhandler.ConnectionEvent -= _eventhandler_ConnectEvent;
                    stopwatch.Stop();
                    TestContext.Progress.WriteLine($"Wireless device disconnected");
                }
                catch (Exception e)
                {
                    _eventhandler.ConnectionEvent -= _eventhandler_ConnectEvent;
                    if (e is ApplicationException)
                        throw new Exception($"Failure to disconnect: {GetDetailedErrorStringFromName(e.Message)}");
                    else
                        throw new Exception($"Failure to disconnect: {e.Message}");
                }
            }
            _eventhandler.Stop();
        }

        public async Task Close()
        {

#pragma warning disable CS0162
            if (Constants.IsProgrammerWireless)
            {
                await CloseWirelessConnection();
                //await CloseWirelessCommAdapter();
            }

            CommAdaptor?.CloseDevice(); // does not currently disconnect wireless, but do not call if product.closedevice is called first.
                                        // this code calls product.closedevice in the teardown of all tests 

            IsConnected = false;
            IsConnecting = false;
#pragma warning restore CS0162
        }


        public async Task<bool> Open()
        {
            CommAdaptor = null;
#pragma warning disable CS0162
            if (Constants.IsProgrammerWireless)
            {
                if (_programmer == "Noahlink")
                {
                    var test = Environment.ExpandEnvironmentVariables(Constants.DriverLocation);
                    _productmanager.BLEDriverPath = Path.GetFullPath(test);
                    TestContext.Progress.WriteLine($"Specified BLE driver location: {_productmanager.BLEDriverPath}");
                }

                _eventhandler = new WirelessEventHandler(_sdeventhandler = _productmanager.GetEventHandler());
                _eventhandler.Start();                  //イベント

                if (StaticValues.WirelessScanFlag)
                {
                    await Task.Run(() => Scan());       //検索スキャン
                }

                await Task.Run(() => WirelessConnect());    //ワイヤレス接続

                return true;
            }
            else
            {
                try
                {
                    CommAdaptor = _productmanager.CreateCommunicationInterface(_programmer, _port, "");
                    CommAdaptor.VerifyNvmWrites = VerifyWrites;
                }
                catch (Exception e)
                {
                    throw new Exception($"Failure to create an interface for the specified programmer: {_programmer} {e.Message}");
                }
                return true;
            }
#pragma warning restore CS0162
        }





        public async Task WirelessConnect()
        {
            try
            {
                Stage = WirelessConnectStage.Connecting;
                _eventhandler.ConnectionEvent += _eventhandler_ConnectEvent;


                //if (_port == CommunicationPort.kLeft)
                //{
                //    ScanEvent = new ScanData();
                //    ScanEvent.DeviceID = StaticValues.WirelessDeviceName1;  //Left
                //}
                //else
                //{
                //    ScanEvent = new ScanData();
                //    ScanEvent.DeviceID = StaticValues.WirelessDeviceName2;  //Right
                //}

                if (ScanEvent.DeviceID == null)
                {
                    throw new Exception($"Device ID is null1");
                }
                TestContext.Progress.WriteLine($"Creating comm interface with {ScanEvent.DeviceName} using id: {ScanEvent.DeviceID}");
                var commAdaptor = _productmanager.CreateWirelessCommunicationInterface(ScanEvent.DeviceID);

                commAdaptor.SetEventHandler(_sdeventhandler);
                TestContext.Progress.WriteLine($"Connecting..");


                commAdaptor.Connect();

                var stopwatch = Stopwatch.StartNew();
                while (stopwatch.ElapsedMilliseconds < Constants.ConnectTimeMs)
                {
                    await Task.Delay(200);

                    if (Stage == WirelessConnectStage.Connected || Stage == WirelessConnectStage.Cancelled)
                    {

                        if (Stage == WirelessConnectStage.Cancelled) { MessageBox.Show("Cancelled 要求の為中止"); }    //Append
                        break;
                    }
                }
                _eventhandler.ConnectionEvent -= _eventhandler_ConnectEvent;

                stopwatch.Stop();

                if (Stage != WirelessConnectStage.Connected)
                {
                    MessageBox.Show("WirelessConnect タイムアウト中止" + Constants.ConnectTimeMs.ToString() + "ms");  //Append
                    TestContext.Progress.WriteLine($"Connect timed out, time elapsed: {stopwatch.ElapsedMilliseconds} ms");
                    throw new Exception($"Device not connected: {Constants.WirelessDeviceName}");
                }

                CommAdaptor = commAdaptor;
                CommAdaptor.VerifyNvmWrites = VerifyWrites;
                TestContext.Progress.WriteLine($"Connection established");
            }
            catch (Exception e)
            {
                _eventhandler.ConnectionEvent -= _eventhandler_ConnectEvent;

                if (e is ApplicationException)
                    throw new Exception($"Failure to connect: {GetDetailedErrorStringFromName(e.Message)}");
                else
                    throw new Exception($"Failure to connect: {e.Message}");
            }

        }

        private void _eventhandler_ConnectEvent(object sender, ConnectEventHandlerArgs e)
        {
            //------------------------------------------------
            var eax = (ConnectEventHandlerArgs)e;
            var seventx = eax.ParseEventArgs();
            StaticValues.EventInfoData3 += $"\nConnection event rx {seventx.ConnectionState} from {seventx.DeviceID} : {Stage}{seventx.ErrorDesc}";
            //-------------------------------------------------

            if (Stage == WirelessConnectStage.Connecting)
            {
                var ea = (ConnectEventHandlerArgs)e;
                var sevent = ea.ParseEventArgs();

                if (sevent.DeviceID == ScanEvent.DeviceID)
                {
                    TestContext.Progress.WriteLine($"Connection event rx {sevent.ConnectionState} from {sevent.DeviceID} (expected)");
                    var cs = (WirelessConnectionState)Convert.ToInt32(sevent.ConnectionState);
                    switch (cs)
                    {
                        case WirelessConnectionState.connecting:
                            //expected
                            break;
                        case WirelessConnectionState.connected:
                            Stage = WirelessConnectStage.Connected;
                            break;

                        //---------------------------------- ここから
                        case WirelessConnectionState.error:
                            Stage = WirelessConnectStage.Cancelled;
                            break;
                        case WirelessConnectionState.disconnecting:
                            Stage = WirelessConnectStage.Cancelled;
                            break;
                        case WirelessConnectionState.disconnected:
                            Stage = WirelessConnectStage.Cancelled;
                            break;
                        default:
                            // all unexpected 
                            Stage = WirelessConnectStage.Cancelled;
                            break;
                            //-------------------------------------　まで　共通
                    }
                    if (Stage == WirelessConnectStage.Cancelled)
                        throw new Exception($"Unexpected disconnect/error {sevent.ErrorDesc}");
                }
            }
            else if (Stage == WirelessConnectStage.Connected)
            {
                // check for disconnected
                var ea = (ConnectEventHandlerArgs)e;
                var sevent = ea.ParseEventArgs();
                if (sevent.DeviceID == ScanEvent.DeviceID)
                {
                    TestContext.Progress.WriteLine($"Connection event rx {sevent.ConnectionState} from {sevent.DeviceID}");
                    var cs = (WirelessConnectionState)Convert.ToInt32(sevent.ConnectionState);
                    switch (cs)
                    {
                        case WirelessConnectionState.connecting:
                        case WirelessConnectionState.connected:
                            // don't care
                            break;
                        case WirelessConnectionState.error:
                        case WirelessConnectionState.disconnecting:
                        case WirelessConnectionState.disconnected:
                        default:
                            // all unexpected 
                            Stage = WirelessConnectStage.Disconnected;
                            break;
                    }
                }

                if (Stage == WirelessConnectStage.Disconnected)
                {
                    TestContext.Progress.WriteLine($"Connection event rx {sevent.ConnectionState} from {sevent.DeviceID} (unexpected)");
                    throw new Exception($"Unexpected disconnect {sevent.ErrorDesc}");
                }
            }
            else if (Stage == WirelessConnectStage.Disconnecting)
            {
                var ea = (ConnectEventHandlerArgs)e;
                var sevent = ea.ParseEventArgs();
                if (sevent.DeviceID == ScanEvent.DeviceID)
                {
                    TestContext.Progress.WriteLine($"Connection event rx {sevent.ConnectionState} from {sevent.DeviceID}");
                    var cs = (WirelessConnectionState)Convert.ToInt32(sevent.ConnectionState);
                    switch (cs)
                    {
                        case WirelessConnectionState.disconnecting:
                            // don't care
                            break;
                        case WirelessConnectionState.disconnected:
                            Stage = WirelessConnectStage.Disconnected;
                            break;
                        case WirelessConnectionState.connecting:
                        case WirelessConnectionState.connected:
                        case WirelessConnectionState.error:
                        default:
                            // all unexpected
                            Stage = WirelessConnectStage.Cancelled;
                            break;
                    }
                }
                if (Stage == WirelessConnectStage.Cancelled)
                {
                    TestContext.Progress.WriteLine($"Connection event rx {sevent.ConnectionState} from {sevent.DeviceID} (unexpected)");
                    throw new Exception($"Unexpected disconnect {sevent.ErrorDesc}");
                }
            }
        }

        public async Task Scan()
        {
            try
            {
                Stage = WirelessConnectStage.Scan;
                _eventhandler.ScanEvent += _eventhandler_ScanEvent;
                TestContext.Progress.WriteLine($"Starting scan");

                WirelessProgrammerType wirelessProgrammer;
                string wirelessProgrammerComPort = "";

                if (_programmer == "RSL10")
                {
                    wirelessProgrammer = WirelessProgrammerType.kRSL10;
                    wirelessProgrammerComPort = Constants.COMPort;
                }
                else
                {
                    wirelessProgrammer = WirelessProgrammerType.kNoahlinkWireless;
                }

                var monitor = _productmanager.BeginScanForWirelessDevices(wirelessProgrammer, wirelessProgrammerComPort, _port, "", false);  //Changed

                var stopwatch = Stopwatch.StartNew();
                while (stopwatch.ElapsedMilliseconds < Constants.ScanTimeMs)
                {
                    await Task.Delay(200);
                    if (Stage == WirelessConnectStage.ScanCompleted)
                    {
                        break;
                    }
                }
                _eventhandler.ScanEvent -= _eventhandler_ScanEvent;
                stopwatch.Stop();

                if (Stage != WirelessConnectStage.ScanCompleted)
                {
                    //スキャンタイムオーバー
                    TestContext.Progress.WriteLine($"Scan timed out, time elapsed: {stopwatch.ElapsedMilliseconds} ms");
                    _productmanager.EndScanForWirelessDevices(monitor);

                    if (ScanMethod == 1)
                    {    //追加 スキャンのみ
                        throw new Exception("Device not found: {Constants.WirelessDeviceName}");
                    }
                    else
                    {
                        //従来
                        MessageBox.Show($"{_port.ToString()}該当のID検索タイムオーバー");
                        throw new Exception($"Device not found: {Constants.WirelessDeviceName}");
                    }
                }


                monitor.GetResult();
                _productmanager.EndScanForWirelessDevices(monitor); // no need to get the scan list as we monitored scan events. 
            }
            catch (Exception e)
            {
                _eventhandler.ScanEvent -= _eventhandler_ScanEvent;
                if (e is ApplicationException)
                    throw new Exception($"Failure to scan: {GetDetailedErrorStringFromName(e.Message)}");
                else
                    throw new Exception($"Failure to scan: {e.Message}");
            }
        }

        //--------------------------------------------------------------------
        // スキャンし、検出されたデバイス
        //--------------------------------------------------------------------
        // _eventhandler_ScanEvent()イベント
        //　変更内容
        //    ScanMethodプロパティを追加し
        //    　　　　　＝0 　従来通り、スキャンし該当IDにコネクト
        //　　　　　　　＝1   スキャンのみ　スキャン期間中全IDを記録する
        private void _eventhandler_ScanEvent(object sender, EventArgs e)
        {
            if (Stage == WirelessConnectStage.Scan)
            {
                // find the device we are looking for 
                var ea = (ScanEventHandlerArgs)e;
                var sevent = ea.ParseEventArgs();

                TestContext.Progress.WriteLine($"Scan event rx {sevent.DeviceName}");

                StaticValues.EventInfoData2 += $"\nScan event rx {sevent.DeviceName}[{sevent.DeviceID}][{sevent.RSSI}]";

                if (ScanMethod == 1)
                {
                    //スキャンの時だけ更新
                    StaticValues.ScanList.Add((sevent.DeviceName, sevent.DeviceID));        //スキャンリストに追加
                    StaticValues.ScanDatas.Add(sevent);
                }

                //if (sevent.DeviceName == Constants.WirelessDeviceName)
                //{
                //    ScanEvent = sevent;
                //    Stage = WirelessConnectStage.ScanCompleted;
                //}

                //ScanMethod＝１の場合はスキャンのみ、該当IDにコネクトしたくないので　ScanCompletedしない
                if (ScanMethod != 1)
                {
                    if (_port == CommunicationPort.kLeft)
                    {
                        if ((/*sevent.DeviceName + */sevent.DeviceID) == StaticValues.WirelessDeviceName1)   //==Constants.WirelessDeviceName
                        {
                            ScanEvent = sevent;
                            Stage = WirelessConnectStage.ScanCompleted;
                            StaticValues.EventInfoData2 += $"\nLeft:Find!! {sevent.DeviceName}[{sevent.DeviceID}][{sevent.RSSI}]";

                        }
                    }
                    else
                    {
                        if ((/*sevent.DeviceName + */sevent.DeviceID) == StaticValues.WirelessDeviceName2)  //==Constants.WirelessDeviceName
                        {
                            ScanEvent = sevent;
                            Stage = WirelessConnectStage.ScanCompleted;
                            StaticValues.EventInfoData2 += $"\nRight:Find!! {sevent.DeviceName}[{sevent.DeviceID}][{sevent.RSSI}]";

                        }
                    }
                }

            }
        }

        public void UpdateProgress(int pval)
        {
            _progress?.Report(pval);
        }

        public async Task GetDeviceInformationBlocking()
        {

            try
            {
                int iCount = 0; //監視用

                var monitor = CommAdaptor.BeginDetectDevice();
                var lastpr = -1;
                while (!monitor.IsFinished)
                {
                    await Task.Delay(300);
                    var maxsteps = monitor.ProgressMaximum;
                    var pr = monitor.GetProgressValue();
                    if (maxsteps > 0 && lastpr != pr)
                    {
                        lastpr = pr;
                        UpdateProgress((int)((((double)pr / maxsteps) * ((double)100))));
                    }

                    //監視
                    iCount++;
                    if (iCount > 30)
                    {
                        MessageBox.Show("CommAdaptor.BeginDetectDevice()が完了できません,アプリ再起動してください");
                        //throw new Exception("CommAdaptor.BeginDetectDevice()が完了できません");
                    }
                }
                monitor.GetResult(); // error check
                ConnectedDeviceInfo = CommAdaptor.EndDetectDevice(monitor);
            }
            catch
            {
                ConnectedDeviceInfo = null;
                throw;
            }
        }

        public void UnlockParameters()
        {
            TestContext.Progress.WriteLine("Parameters are locked, unlocking with key in Constants.cs");
            var key = TextUtil.ConvertHexStringToIntArray(Constants.ParameterLockKey, 96, 4);
            if (key != null)
            {
                // TODO: The SDK is missing the async version of this call
                CommAdaptor.UnlockParameterAccess(key[0], key[1], key[2], key[3]); // will throw exception if incorrect
            }
            else
            {
                throw new InvalidOperationException("Improperly formatted Parameter Lock Key");
            }
        }

        public string GetDetailedErrorStringFromName(string errorName)
        {
            return _productmanager.GetDetailedErrorStringFromName(errorName);
        }

        public Connection(CommunicationPort port, IProductManager productmanager, string programmer = null,
                          CancellationToken canceltoken = default(CancellationToken), IProgress<int> progress = null)
        {
            _programmer = programmer;
            _productmanager = productmanager;
            _port = port;
            IsConnected = false;
            IsConnecting = false;
            ParametersUnlocked = false;
            VerifyWrites = false;
            Stage = WirelessConnectStage.Disconnected;
            _progress = progress;
            _canceltoken = canceltoken;
        }


        //
        // 追加
        //
        public void SetScanEvent(ScanData sData)
        {
            ScanEvent = sData;
        }

        public async Task<bool> CloseWirelessCommAdapter()
        {
            if (Stage == WirelessConnectStage.Connected)
            {
                bool err = true;
                try
                {
                    //logger.Debug("Disconnecting " + Connection.LocatedScanEventData.DeviceName + "..");
                    Stage = WirelessConnectStage.Disconnecting;
                    CommAdaptor.Disconnect();
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    while (stopwatch.ElapsedMilliseconds < 5000)
                    {
                        await Task.Delay(200);
                        if (Stage == WirelessConnectStage.Disconnected || Stage == WirelessConnectStage.Cancelled)
                        {
                            break;
                        }
                    }
                    if (Stage != WirelessConnectStage.Disconnected)
                    {
                        err = false;
                        //ThrowError($"Disconnect timed out, time elapsed: {stopwatch.ElapsedMilliseconds} ms");
                        TestContext.Progress.WriteLine($"Disconnect timed out, time elapsed: 5000 ms");
                        throw new Exception($"Disconnect timed out, time elapsed: 5000 ms");
                    }
                    stopwatch.Stop();
                    //logger.Debug("Wireless device disconnected");
                }
                catch (Exception ex)
                {
                    if (ex is ApplicationException)
                    {
                        //ThrowError("Failure to disconnect: " + ai.GetDetailedErrorStringFromName(ex.Message));
                        throw new Exception($"Failure to disconnect: {GetDetailedErrorStringFromName(ex.Message)}");
                    }
                    else
                    {
                        //ThrowError("Failure to disconnect");
                        throw new Exception($"Failure to disconnect");
                        //logger.Debug("Failure to disconnect: " + ex.Message);
                    }
                    err = false;
                }
                RemoveConnectEventHandler(_eventhandler_ConnectEvent);
                StopWirelessEventHandler();
                return err;
            }

            //throw new Exception("Cannot close wireless connection, it is not connected");
            return false;
        }

        public void RemoveConnectEventHandler(EventHandler<ConnectEventHandlerArgs> cnEventHandler)
        {
            _eventhandler.ConnectionEvent -= cnEventHandler;
        }

        public void StopWirelessEventHandler()
        {
            _eventhandler?.Stop();
        }


    }

}
