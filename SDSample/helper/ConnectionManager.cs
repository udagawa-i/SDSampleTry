using System;
using System.Threading.Tasks;
using System.Windows;
using NUnit.Framework;
using SDLib;
using SDSample;

namespace SoundDesigner.Helper
{
    public class ConnectionManager
    {
        private Connection _left;
        private Connection _right;
        private IProductManager _productmanager; // needed to create comm adaptors
        private string _programmer;

        public ConnectionManager(IProductManager productmanager, string programmer)
        {
            _productmanager = productmanager;
            _programmer = programmer;
        }

        public Connection GetConnection (CommunicationPort port)
        {
            return port == CommunicationPort.kLeft ? _left : _right;
        }

        public bool StillThere(CommunicationPort port)
        {
            return GetConnection(port).CommAdaptor.CheckDevice(); 
        }

        public async Task Close(CommunicationPort port)
        {
            await GetConnection(port).Close();
        }

        //
        //　変更　
        //    ConnectAsync()　第２引数　int scanMetod追加
        //
        //    ScanMethodプロパティ
        //    　　　　　＝0 　従来通り、スキャンし該当IDにコネクト
        //　　　　　　　＝1   スキャンのみ　スキャン期間中全IDを記録する

        public async Task<bool> ConnectAsync(CommunicationPort port, int scanMethod, IProgress<int> progress = null)
        {
            Connection connection; 
            connection = port == CommunicationPort.kLeft ? _left : _right;
            TestContext.Progress.WriteLine("Connecting");

            if ( connection != null)
            {
                if (connection.IsConnecting)
                {
                    return false;
                }
                await connection.Close();
            }
            else
            {
                Connection nconnection;
                nconnection = new Connection(port, _productmanager, programmer: _programmer,  progress: progress);
                if (port == CommunicationPort.kLeft)
                    _left = nconnection;
                else
                    _right = nconnection;
                connection = nconnection; 
            }

            //2024.2.14 追加　いきなりwirelessConnectに対応する為
            if (port == CommunicationPort.kLeft)
            {
                SetScanEvent(port, StaticValues.ScanEventLeft);
            }
            else
            {
                SetScanEvent(port, StaticValues.ScanEventRight);
            }

            try
            {
                connection.ScanMethod = scanMethod;  //プロパティ設定　Open()でのスキャン方法を指定

                await connection.Open();              
 
                //MessageBox.Show("Open() 終了、 connection.GetDeviceInformationBlocking())実行");

                TestContext.Progress.WriteLine("Getting device information"); 
                await Task.Run(() => connection.GetDeviceInformationBlocking());
                if (connection.ConnectedDeviceInfo.ParameterLockState)
                {
                    connection.UnlockParameters();
                }
                TestContext.Progress.WriteLine($"Connected device FW ID: {connection.ConnectedDeviceInfo.FirmwareId} Ver: {connection.ConnectedDeviceInfo.FirmwareVersion}");
                
                //MessageBox.Show("connection.GetDeviceInformationBlocking())完了");
            } 
            catch
            {
                if (scanMethod != 1)
                {
                    MessageBox.Show("connection.Open() 失敗の為、connectionをクローズします");
                }
                await connection.Close();
                throw; 
            }
            connection.IsConnected = true; 
            connection.IsConnecting = false;
            return true;
        }


        //
        // 追加
        //
        public void SetScanEvent( CommunicationPort port, ScanData scanData)
        {
            if( port  == CommunicationPort.kLeft)
            {
                _left.SetScanEvent(scanData);
            }
            else
            {
                _right.SetScanEvent(scanData);
            }
        }

    }
}
