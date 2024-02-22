//using SoundDesigner.ControlPanel;

namespace SDSample
{
    using Microsoft.Win32;
    using NUnit.Framework;
    using SDLib;
    //using static SDSample.ProductManager;
    using SoundDesigner.Helper;
    //using SoundDesigner.ControlPanel;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using Visifire.Charts;
    using static SoundDesigner.Helper.ProductManager;

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Internal Types
        public enum comState
        {
            Disconnected,
            Detected,
            Initialized,
        }
        #endregion
        //

        #region Private Fields
        // SD関連のフィールド
        IProductManager productManager;
        ILibrary library;
        ICommunicationAdaptor communicationAdaptor;
        IDeviceInfo deviceInfo;

        //IProduct product;     //ProductManagerクラスで product_left/product_rightを管理する

        // GUI関連のフィールド
        readonly List<string> communicationInterfaceNames = new List<string>(0);
        comState _comState = comState.Disconnected; // 通信状態によるボタンコントロールの有効無効のステータス

        // グラフ表示関連のフィールド
        const int frequencyPointNumber = 120;
        List<int> graphFreuquencyPoints = new List<int>(frequencyPointNumber); // グラフに描画する補聴器応答曲線の周波数ポイントを格納する配列
        SortedList<int, DataSeries> dataSerise = new SortedList<int, DataSeries>()
            {// 入力音圧レベルのリスト
                { 40 ,new DataSeries()},
                { 50 ,new DataSeries()},
                { 60 ,new DataSeries()},
                { 70 ,new DataSeries()},
                { 80 ,new DataSeries()},
                { 90 ,new DataSeries()},
                { 100 ,new DataSeries()},
            };
        readonly double[] sdGraphXData = new double[frequencyPointNumber];
        readonly double[] sdGraphYData = new double[frequencyPointNumber];


        // グラフ表示関連のフィールド
        const int frequencyPointNumber2 = 120;
        List<int> graphFreuquencyPoints2 = new List<int>(frequencyPointNumber2); // グラフに描画する補聴器応答曲線の周波数ポイントを格納する配列
        SortedList<int, DataSeries> dataSerise2 = new SortedList<int, DataSeries>()
            {// 入力音圧レベルのリスト
                { 40 ,new DataSeries()},
                { 50 ,new DataSeries()},
                { 60 ,new DataSeries()},
                { 70 ,new DataSeries()},
                { 80 ,new DataSeries()},
                { 90 ,new DataSeries()},
                { 100 ,new DataSeries()},
            };
        readonly double[] sdGraphXData2 = new double[frequencyPointNumber2];
        readonly double[] sdGraphYData2 = new double[frequencyPointNumber2];



        const int frequencyPointNumber3 = 120;
        List<int> graphFreuquencyPoints3 = new List<int>(frequencyPointNumber3); // グラフに描画する補聴器応答曲線の周波数ポイントを格納する配列
        public List<List<double>> chirpToolFrequencyResult;     //チャープツール実行結果格納（周波数特性）
        SortedList<int, DataSeries> dataSerise3 = new SortedList<int, DataSeries>()
            {
                { 40 ,new DataSeries()},
            };


        CommunicationPort _port = new CommunicationPort();  //Append

        private readonly IProgress<int> _progress;







        //----------------------------------------------
        // 　private 変数
        //----------------------------------------------
        //メモリ　パラメータの種類　格納エリア
        private Dictionary<string, int> _uniqueArrayDictionary;         //配列要素を持つパラメータ
        private List<string> _uniqueParamList;

        //システム　パラメータの種類　格納エリア
        private Dictionary<string, int> _uniqueSystemArrayDictionary;   //配列要素を持つパラメータ
        private List<string> _uniqueSystemParamList;


        //paramList (ListBox)
        private ObservableCollection<string> ParamList = new ObservableCollection<string>();


        //選択されたパラメータ名
        private string SelectParamName;


        //
        public ProductManager _pmg;


        #endregion




        #region Public Methods
        /// <summary>
        /// コンストラクター
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            InitializeChart();
            InitializeChart2();


            try
            {
                var ci = Properties.Settings.Default.communicationInterface;

                // SDの大元の呼び出し(GetProductManagerInstance()で生成さるのはシングルトン)
                this.productManager = SDLib.SDLibMain.GetProductManagerInstance();

                _pmg = new ProductManager(productManager, library, null);

                //有線/無線、プログラマータイプが決まってから生成「接続ポート生成」ボタン
                //_pmg._connectionmanager = new ConnectionManager(productManager, Constants.Programmer);



                for (int i = 0; i < productManager.GetCommunicationInterfaceCount(); i++)
                {
                    communicationInterfaceNames.Add(productManager.GetCommunicationInterfaceString(i));

                    communicationInterfaceComboBox.Items.Add(communicationInterfaceNames[i]);

                    if (communicationInterfaceNames[i] == Properties.Settings.Default.communicationInterface)
                    {
                        communicationInterfaceComboBox.SelectedIndex = i;
                    }

                }
            }
            catch
            {
                throw;
            }


            this.ButtonStatusChange(comState.Disconnected);

            //左右ポート設定
            RadioLeft.IsChecked = true;
            RadioLeftWireless.IsChecked = true;


            WirelessDeviceName1Text.Text = Constants.WirelessDeviceName;
            WirelessDeviceName2Text.Text = Constants.WirelessDeviceName;


            //メモリ選択　システム
            ParamSystem.IsChecked = true;


            //メモリ選択
            SelectMemoryComboBox.Items.Add("0:MEM-A");
            SelectMemoryComboBox.Items.Add("1:MEM-B");
            SelectMemoryComboBox.Items.Add("2:MEM-C");
            SelectMemoryComboBox.Items.Add("3:MEM-D");
            SelectMemoryComboBox.Items.Add("4:MEM-E");
            SelectMemoryComboBox.Items.Add("5:MEM-F");
            SelectMemoryComboBox.Items.Add("6:MEM-G");
            SelectMemoryComboBox.Items.Add("7:MEM-H");


            //情報表示用タイマー 500[msec]周期
            TimerHelper communicationStatusTimer;
            communicationStatusTimer = new TimerHelper(communicationStatusDispExecute);
            communicationStatusTimer.start(500);

            //無線
            RadioComPortTypeWireless.IsChecked = true;


            WirelessDeviceName1Text.Text = "";      // "60:C0:BF:14:9F:A1";
            WirelessDeviceName2Text.Text = "";      //"60:C0:BF:4C:D8:2D";


        }

        //----------------------------------------------
        // 　無線通信ポート ステータス状態表示
        //----------------------------------------------
        void communicationStatusDispExecute(object o)
        {
            try
            {
                // UI スレッド上での処理を要求
                Application.Current.Dispatcher.Invoke(() =>
                {

                    //
                    // イベントデータに更新があれば更新する
                    //
                    // EventInfoBox ← StaticValues.EventInfoData
                    if (StaticValues.EventInfoData.Length != EventInfoBox.Text.Length)
                    {
                        EventInfoBox.Text = StaticValues.EventInfoData;
                        EventInfoBox.SelectionStart = EventInfoBox.Text.Length;
                        EventInfoBox.ScrollToEnd();
                    }

                    // EventInfoBox2 ← StaticValues.EventInfoData
                    if (StaticValues.EventInfoData2.Length != EventInfoBox2.Text.Length)
                    {
                        EventInfoBox2.Text = StaticValues.EventInfoData2;
                        EventInfoBox2.SelectionStart = EventInfoBox2.Text.Length;
                        EventInfoBox2.ScrollToEnd();
                    }

                    // EventInfoBox ← StaticValues.EventInfoData
                    if (StaticValues.EventInfoData3.Length != EventInfoBox3.Text.Length)
                    {
                        EventInfoBox3.Text = StaticValues.EventInfoData3;
                        EventInfoBox3.SelectionStart = EventInfoBox3.Text.Length;
                        EventInfoBox3.ScrollToEnd();
                    }


                    WirelessScan_id1.Text = StaticValues.WirelessDeviceName1;
                    WirelessScan_id2.Text = StaticValues.WirelessDeviceName2;



                    if (_pmg._connectionmanager?.GetConnection(CommunicationPort.kLeft) != null)
                    {
                        AppCommunicationStatusLeft.Text = _pmg._connectionmanager.GetConnection(CommunicationPort.kLeft).Stage.ToString();
                    }

                    if (_pmg._connectionmanager?.GetConnection(CommunicationPort.kRight) != null)
                    {
                        AppCommunicationStatusRight.Text = _pmg._connectionmanager.GetConnection(CommunicationPort.kRight).Stage.ToString(); ;
                    }

                });
            }
            catch
            {

            }
        }





        #endregion

        #region Private Methods
        #region SDライブラリーを操作するメソッド
        /// <summary>
        /// デイバイスを検出する。
        /// DSPにパスワードでロックがかかっている場合は解除もする。
        /// </summary>

        void DetectDevice(string libraryName)
        {
            #region CommunicationInterface関連

            // 耳用の通信オブジェクト
            try
            {
                DeviceInfoDisplay.Text = "";

                //deviceInfo = communicationAdaptor.DetectDevice(); // デバイスの基本情報を読み込む
                deviceInfo = _pmg.GetConnectedDeviceInfo(_pmg.GetSelectPort()); // デバイスの基本情報を読み込む

                if (deviceInfo.ParameterLockState)
                {// Parameter Lock されている場合はunlockのためにキーを設定して再度Detectしなおす必要ある

                    List<List<int>> unlockParameterList = new List<List<int>>
                        {
                            new List<int>() { 0x1DFACE , 0xB0 , 0, 0} ,// パスワード: 0xB01DFACE// 引数の順番を気をつける
                            new List<int>() { 0xADBE9, 0 , 0, 0} ,// パスワード: 0xADBE9                    
                        };

                    foreach (var unlockParameter in unlockParameterList)
                    {
                        //communicationAdaptor.UnlockParameterAccess(
                        ICommunicationAdaptor commAdpt = _pmg._connectionmanager.GetConnection(_pmg.GetSelectPort()).CommAdaptor;
                        commAdpt.UnlockParameterAccess(
                            unlockParameter[0],
                            unlockParameter[1],
                            unlockParameter[2],
                            unlockParameter[3]);

                        //deviceInfo = communicationAdaptor.DetectDevice();
                        deviceInfo = commAdpt.DetectDevice();

                        if (!deviceInfo.ParameterLockState)
                        {
                            break;
                        }
                    }

                    if (deviceInfo.ParameterLockState)
                    {
                        throw new Exception("DSPのlockが解除できませんでした。");
                    }
                }
            }
            catch
            {
                throw;
            }

            // 検出されたDeviceInfoの各プロパティーを表示
            var di =
                "FirmwareVersion: " + deviceInfo.FirmwareVersion.ToString() + Environment.NewLine +
                "FirmwareId: " + deviceInfo.FirmwareId.ToString() + Environment.NewLine +
                "IsValid: " + deviceInfo.IsValid + Environment.NewLine +
                "ParameterLockState: " + deviceInfo.ParameterLockState.ToString() + Environment.NewLine +
                "ChipVersion: " + deviceInfo.ChipVersion.ToString() + Environment.NewLine +
                "ChipId: " + deviceInfo.ChipId.ToString() + Environment.NewLine +
                "HybridId: " + deviceInfo.HybridId.ToString() + Environment.NewLine +
                "HybridRevision: " + deviceInfo.HybridRevision.ToString() + Environment.NewLine +
                "HybridSerial: " + deviceInfo.HybridSerial.ToString() + Environment.NewLine +
                "LibraryId: " + deviceInfo.LibraryId.ToString("x8") + Environment.NewLine +
                "ProductId: " + deviceInfo.ProductId.ToString() + Environment.NewLine +
                "SerialId: " + deviceInfo.SerialId.ToString() + Environment.NewLine +
                "RadioApplicationVersion: " + deviceInfo.RadioApplicationVersion.ToString() + Environment.NewLine +
                "RadioBootloaderVersion: " + deviceInfo.RadioBootloaderVersion.ToString() + Environment.NewLine +
                "RadioSoftDeviceVersion: " + deviceInfo.RadioSoftDeviceVersion.ToString() + Environment.NewLine;

            DeviceInfoDisplay.Text = di;
            #endregion

            #region Product Library関連
            try
            {
                //　libraryファイルは固定値にしたが、deviceInfoのlibraryid とlibraryオブジェクトのlibraryidを比較することで自動選択も可能
                //this.library = productManager.LoadLibraryFromFile("SXx-Library3.library");
                //this.library = productManager.LoadLibraryFromFile("E7160SL.library");
                this.library = productManager.LoadLibraryFromFile(libraryName);

            }
            catch
            {
                throw;
            }

            var productID = deviceInfo.ProductId; // 検出された物理的に繋がっているDSPに書き込まれているproductID
            var productDefinitionList = this.library.Products;
            try
            {
                var productDefinition = productDefinitionList.GetById(productID);
                var dc = productDefinition.GetDeviceCompatibility(communicationAdaptor);
                //product = productDefinition.CreateProduct();
                _pmg.SetProduct(productDefinition.CreateProduct());

            }
            catch
            {
                throw;
            }

            #endregion
        }

        /// <summary>
        /// 通信インターフェイスの選択 (Hi-Pro, CAA等)
        /// </summary>
        /// <param name="communicationInterfaceName"></param>
        private void SetCommunicationInterface(string communicationInterfaceName)
        {

            //communicationAdaptor = productManager.CreateCommunicationInterface(communicationInterfaceName, _port, "");
            ICommunicationAdaptor commAdpt = _pmg._connectionmanager.GetConnection(_pmg.GetSelectPort()).CommAdaptor;
            commAdpt = productManager.CreateCommunicationInterface(communicationInterfaceName, _pmg.GetSelectPort(), "");

            //communicationAdaptor.VerifyNvmWrites = true; // NVMに書き込んだときにReadしなおして書き込み内容をチェックする機能を有効
            commAdpt.VerifyNvmWrites = true; // NVMに書き込んだときにReadしなおして書き込み内容をチェックする機能を有効

            //communicationAdaptor.MuteDuringCommunication = true; // Read/Write中に補聴器をミュートする設定
            commAdpt.MuteDuringCommunication = true; // Read/Write中に補聴器をミュートする設定
        }

        ////        /// <summary>
        ////        /// デバイスとの通信を初期化し通信可能状態に移行する。
        ////        /// </summary>
        ////        void InitializeDevice()
        ////        {
        ////            try
        ////            {
        ////                for (; ; )
        ////                {
        ////                    var isInitialized = this.product.InitializeDevice(this.communicationAdaptor);

        ////                    if (!isInitialized)
        ////                    {
        ////                        var result = MessageBox.Show("This device is not configured. Do you configure the device?", "", MessageBoxButton.OKCancel);

        ////                        if (result == MessageBoxResult.OK)
        ////                        {
        ////                            this.product.ConfigureDevice();
        ////                            break;
        ////                        }
        ////                        else
        ////                        {
        ////                            throw new Exception("This device is not configured.");
        ////                        }
        ////                    }
        ////                    else
        ////                    {
        ////                        break;
        ////                    }
        ////                }

        ////                this.product.MuteDevice(true); // 補聴器のハウリングがうるさい場合があるので通信確立直後に一旦自動的にミュートにしておく
        ////            }
        ////            catch
        ////            {
        ////                throw;
        ////            }
        ////        }

        ////        /// <summary>
        ////        /// デバイスとの通信可能状態を終了する。
        ////        /// </summary>
        ////        void CloseDevice()
        ////        {
        ////            try
        ////            {
        ////                this.product.CloseDevice();
        ////            }
        ////            catch
        ////            {
        ////                throw;
        ////            }
        ////        }

        /// <summary>
        /// デバイスの全てのパラメーターの値を読み取る。
        /// </summary>
        async Task ReadParameters()
        {
            try
            {
                // 補聴器から全てのパラメータ値を読取る
                //this.product.CurrentMemory = 0; // メモリーAをアクティブメモリーにセット //_product 
                _pmg.GetSelectProduct().CurrentMemory = 0;

                /* "CurrentMemory"プロパティーの説明 「onsemi Ezairo Sound Designer SDK Programmer’s Guide」の「6.7 PARAMETER READING AND WRITING」より:
The current active memory of the device can be set using the int Product.CurrentMemory property.
However, the active memory of the device is not switched until you call either 
Product::ReadParameters(ParameterSpace memory) or Product::WriteParameters(ParameterSpace memory).

NOTE: Product.CurrentMemory is effectively a write-only parameter. As an offline state variable in
the SDK, it is never updated from the device,. 
Since the user can change the active memory of the device via a push-button on the hearing aid or a wireless remote control command,
you must set Product.CurrentMemory explicitly before calling 
Product::ReadParameters(ParameterSpace memory) or Product::WriteParameters(ParameterSpace memory).
 */
                IProgress<int> progress = new Progress<int>(onProgressChangedInt);

                _pmg.GetSelectProduct().ReadParameters(ParameterSpace.kNvmMemory0); progress.Report((int)(1 / 9.0 * 100));
                _pmg.GetSelectProduct().ReadParameters(ParameterSpace.kNvmMemory1); progress.Report((int)(2 / 9.0 * 100));
                _pmg.GetSelectProduct().ReadParameters(ParameterSpace.kNvmMemory2); progress.Report((int)(3 / 9.0 * 100));
                _pmg.GetSelectProduct().ReadParameters(ParameterSpace.kNvmMemory3); progress.Report((int)(4 / 9.0 * 100));
                _pmg.GetSelectProduct().ReadParameters(ParameterSpace.kNvmMemory4); progress.Report((int)(5 / 9.0 * 100));
                _pmg.GetSelectProduct().ReadParameters(ParameterSpace.kNvmMemory5); progress.Report((int)(6 / 9.0 * 100));
                _pmg.GetSelectProduct().ReadParameters(ParameterSpace.kNvmMemory6); progress.Report((int)(7 / 9.0 * 100));
                _pmg.GetSelectProduct().ReadParameters(ParameterSpace.kNvmMemory7); progress.Report((int)(8 / 9.0 * 100));

                _pmg.GetSelectProduct().ReadParameters(ParameterSpace.kSystemNvmMemory); progress.Report((int)(9 / 9.0 * 100));
                //this.product.ReadParameters(ParameterSpace.kActiveMemory);      progress.Report((int)(10 / 11.0 * 100));  //書き込んだらいけない！
                //this.product.ReadParameters(ParameterSpace.kSystemActiveMemory);progress.Report((int)(11 / 11.0 * 100));  //書き込んだらいけない！

                /*************************************************************************
                // パラメータをコンソールに表示　　
                //
                Console.WriteLine("#System Paramters");
                foreach (IParameter systemParameter in this.product.SystemMemory.Parameters)
                {
                    Console.Write(systemParameter.Id + "\t");
                    Console.Write(systemParameter.Name + "\t");
                    Console.Write(systemParameter.Units + "\t");
                    switch (systemParameter.Type)
                    {
                        case ParameterType.kByte:
                        case ParameterType.kInteger:
                        case ParameterType.kIndexedTextList:
                        case ParameterType.kIndexedList:
                            Console.Write(systemParameter.Min + "\t");
                            Console.Write(systemParameter.Max + "\t");
                            Console.Write(systemParameter.DefaultValue + "\t");
                            break;
                        case ParameterType.kBoolean:
                            Console.Write(systemParameter.BooleanDefaultValue + "\t");
                            break;
                        case ParameterType.kDouble:
                            Console.Write(systemParameter.DoubleMin + "\t");
                            Console.Write(systemParameter.DoubleMax + "\t");
                            Console.Write(systemParameter.DoubleDefaultValue + "\t");
                            break;
                    }

                    switch (systemParameter.Type)
                    {
                        case ParameterType.kBoolean:
                            Console.Write(systemParameter.BooleanValue);
                            break;
                        case ParameterType.kDouble:
                            Console.Write(systemParameter.DoubleValue);
                            break;
                        case ParameterType.kByte:
                        case ParameterType.kInteger:
                            Console.Write(systemParameter.Value);
                            break;
                        case ParameterType.kIndexedList:
                            Console.Write(systemParameter.Value);
                            break;
                        case ParameterType.kIndexedTextList:
                            Console.Write(systemParameter.Value);
                            break;
                    }

                    Console.WriteLine("\t(" + systemParameter.Description + ")");

                }


                int memoryNumber = 0;
                Console.WriteLine("#Profile Paramters");
                foreach (IParameterMemory profileMemory in this.product.Memories)
                {
                    ++memoryNumber;
                    Console.WriteLine("#Profile " + memoryNumber.ToString());

                    foreach (IParameter profileParameter in profileMemory.Parameters)
                    {
                        Console.Write(profileParameter.Id + "\t");

                        Console.Write(profileParameter.Name + "\t");

                        Console.Write(profileParameter.Units + "\t");
                        switch (profileParameter.Type)
                        {
                            case ParameterType.kByte:
                            case ParameterType.kInteger:
                            case ParameterType.kIndexedTextList:
                            case ParameterType.kIndexedList:
                                Console.Write(profileParameter.Min + "\t");
                                Console.Write(profileParameter.Max + "\t");
                                Console.Write(profileParameter.DefaultValue + "\t");
                                break;
                            case ParameterType.kBoolean:
                                Console.Write(profileParameter.BooleanDefaultValue + "\t");
                                break;
                            case ParameterType.kDouble:
                                Console.Write(profileParameter.DoubleMin + "\t");
                                Console.Write(profileParameter.DoubleMax + "\t");
                                Console.Write(profileParameter.DoubleDefaultValue + "\t");
                                break;
                        }

                        switch (profileParameter.Type)
                        {
                            case ParameterType.kBoolean:
                                Console.Write(profileParameter.BooleanValue);
                                break;
                            case ParameterType.kDouble:
                                Console.Write(profileParameter.DoubleValue);
                                break;
                            case ParameterType.kByte:
                            case ParameterType.kInteger:
                                Console.Write(profileParameter.Value);
                                break;
                            case ParameterType.kIndexedList:
                                Console.Write(profileParameter.Value);
                                break;
                            case ParameterType.kIndexedTextList:
                                Console.Write(profileParameter.Value);
                                break;
                        }

                        Console.WriteLine("\t(" + profileParameter.Description + ")");
                    }

                }
                *************************************************************/
            }
            catch
            {
                throw;
            }
        }

        void WriteParamters()
        {

            try
            {
                int i = 0;

                IProgress<int> progress = new Progress<int>(onProgressChangedInt);


                _pmg.GetSelectProduct().CurrentMemory = 0; // カレントメモリーをメモリーAに設定

                _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kActiveMemory); progress.Report((int)(1 / 10.0 * 100));

                //メモリ
                _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kNvmMemory0); progress.Report((int)(2 / 10.0 * 100));
                _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kNvmMemory1); progress.Report((int)(3 / 10.0 * 100));
                _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kNvmMemory2); progress.Report((int)(4 / 10.0 * 100));
                _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kNvmMemory3); progress.Report((int)(5 / 10.0 * 100));
                _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kNvmMemory4); progress.Report((int)(6 / 10.0 * 100));
                _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kNvmMemory5); progress.Report((int)(7 / 10.0 * 100));
                _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kNvmMemory6); progress.Report((int)(8 / 10.0 * 100));
                _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kNvmMemory7); progress.Report((int)(9 / 10.0 * 100));

                ////システム（追加）
                //if (SDFacade.PmCheckE7160WriteSystemParameter(deviceInfo))
                //{
                //this.product.WriteParameters(ParameterSpace.kSystemActiveMemory); progress.Report((int)(10 / 10.0 * 100));
                _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kSystemNvmMemory); progress.Report((int)(10 / 10.0 * 100));
                //}
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// デバイスをミュート又はミュートの解除を行う。
        /// </summary>
        /// <param name="doMute"></param>
        private void MuteDevice(bool doMute)
        {
            try
            {
                _pmg.GetSelectProduct().MuteDevice(doMute);
            }
            catch
            {
                throw;
            }
        }

        private void PlayAlert()
        {
            _pmg.GetSelectProduct().PlayAcousticIndicator("Low Battery");
            _pmg.GetSelectProduct().PlayAcousticIndicator("Volume Up");
            _pmg.GetSelectProduct().PlayAcousticIndicator("Volume Down");
        }
        #endregion


        #region GUI 関連のメソッド
        private void detectDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.DetectDevice("SXx-Library3.library");
            }
            catch (Exception ex)
            {
                ButtonStatusChange(comState.Disconnected);
                MessageBox.Show(ex.Message);
                return;
            }

            ButtonStatusChange(comState.Detected);
        }

        private void initializeButton_Click(object sender, RoutedEventArgs e)
        {
            //////try
            //////{
            ////////////    this.InitializeDevice();



            //////    SDFacade.SDFacadeInitial(productManager,library,product);


            //////    string strTemp  = "【Load Library名】" + SDFacade._pmg._library.Description;
            //////    strTemp  += " 【Product名】" + SDFacade._pmg._product.Definition.Description;   //DSPに書き込まれているProduct名
            //////    AppLoadLibraryNameText.Text = strTemp;


            //////    ParameterNameInfoCheck();

            //////    ParameterInfoDisp();

            //////}
            //////catch (Exception ex)
            //////{
            //////    ButtonStatusChange(comState.Disconnected);
            //////    MessageBox.Show(ex.Message);
            //////    return;
            //////}

            //////ButtonStatusChange(comState.Initialized);
        }


        /// <summary>
        /// 現在PCのメモリに格納されているパラメータ名称を解析し、結果をリストに格納
        /// ParameterNameAnalysis
        /// </summary>
        /// 
        private void ParameterNameInfoCheck()
        {
            List<string> paramNameSystem;
            List<string> paramName;
            Dictionary<string, int> paramNameSystemIndex;
            Dictionary<string, int> paramNameIndex;

            SDFacade.PmParameterNameAnalysis(out paramNameSystem, out paramName, out paramNameSystemIndex, out paramNameIndex);

            _uniqueArrayDictionary = paramNameIndex;
            _uniqueParamList = paramName;

            _uniqueSystemArrayDictionary = paramNameSystemIndex;
            _uniqueSystemParamList = paramNameSystem;

        }

        private void ParameterInfoDisp()
        {
            if (ParamSystem.IsChecked == true)
            {
                ParamListBox.ItemsSource = _uniqueSystemParamList;  //システム
            }
            else
            {
                ParamListBox.ItemsSource = _uniqueParamList;        //メモリ
            }
        }


        /// <summary>
        /// 選択されたパラメータ文字列(SelectParamName)の情報を取得して
        /// DataTableに表示する。
        /// </summary>
        private void MakeParamDataTable()
        {
            if (SelectParamName == null) return;


            //選択されたパラメータ文字列は、SelectParamNameに格納されている！
            DataTable table = new DataTable();

            string inputParamName = SelectParamName;    //パラメータ名
            int iColumnCount = 1;

            if (ParamSystem.IsChecked == true)
            {
                //
                // システムメモリ
                //
                if (SelectParamName.EndsWith("*"))
                {
                    inputParamName = SelectParamName.Substring(0, SelectParamName.Length - 1);
                    iColumnCount = _uniqueSystemArrayDictionary[inputParamName];
                }

                //
                // 列を定義（指定されたパラメータにより列数は異なる）
                // 　MEM | Data0 | Data1 | Data2 | ・・・
                //
                table.Columns.Add("MEM", typeof(string));            //システム
                for (int i = 0; i < iColumnCount; i++)                  //[n]
                {
                    table.Columns.Add($"Data{i}", typeof(string));
                }

                // 
                // 表示する１行分のデータを作成（メモリA～H毎）
                //
                int iCount = 0;
                IParameter param = null;

                DataRow row = table.NewRow();   //行
                row["MEM"] = "SYS";


                if (SelectParamName.EndsWith("*"))
                {
                    for (int i = 0; i < iColumnCount; i++)
                    {
                        param = SDFacade.PmFindSystemParameter(inputParamName + "[" + i.ToString() + "]");
                        row[$"Data{i}"] = SDFacade.PmGetStringValue(param);
                    }
                }
                else
                {
                    param = SDFacade.PmFindSystemParameter(SelectParamName);
                    row["Data0"] = SDFacade.PmGetStringValue(param);
                }

                //データを追加
                table.Rows.Add(row);

                //GUIに情報を表示
                DispParamInfo(param);
            }
            else
            {
                //
                // メモリ（A～H）
                //
                if (SelectParamName.EndsWith("*"))
                {
                    inputParamName = SelectParamName.Substring(0, SelectParamName.Length - 1);
                    iColumnCount = _uniqueArrayDictionary[inputParamName];
                }

                //
                // 列を定義（指定されたパラメータにより列数は異なる）
                // 　MEM | Data0 | Data1 | Data2 | ・・・
                //
                table.Columns.Add("MEM", typeof(string));           //メモリ
                for (int i = 0; i < iColumnCount; i++)              //[n]
                {
                    table.Columns.Add($"Data{i}", typeof(string));
                }

                // 
                // 表示する１行分のデータを作成（メモリA～H毎）
                //
                int iCount = 0;
                IParameter param = null;

                foreach (IParameterMemory paramMemory in SDFacade.PmGetMemoryList())      //A～H
                {
                    DataRow row = table.NewRow();       //１行

                    row["MEM"] = "メモリ" + ((char)('A' + iCount++)).ToString();

                    if (SelectParamName.EndsWith("*"))
                    {
                        for (int i = 0; i < iColumnCount; i++)
                        {
                            param = paramMemory.Parameters.GetById(inputParamName + "[" + i.ToString() + "]");  //xxxx[i]
                            row[$"Data{i}"] = SDFacade.PmGetStringValue(param);
                        }
                    }
                    else
                    {
                        param = paramMemory.Parameters.GetById(SelectParamName);
                        row["Data0"] = SDFacade.PmGetStringValue(param);
                    }

                    //データを追加
                    table.Rows.Add(row);
                }

                //GUIに情報を表示
                DispParamInfo(param);

            }

            //
            //DataGridに表示　ParamTable XAML側(DataGrid)
            //
            ParamDataGrid.ItemsSource = table.DefaultView;
        }



        /// <summary>
        /// IParameter param より最小・最大・デフォルト値を取得する
        /// </summary>
        /// <param name="param">IParameter param</param>
        /// <param name="strMin">最小値</param>
        /// <param name="strMax">最大値</param>
        /// <param name="strDefault">デフォルト値</param>
        private void GetStringInfoValue(IParameter param, ref string strMin, ref string strMax, ref string strDefault)
        {
            switch (param.Type)
            {
                case ParameterType.kByte:
                case ParameterType.kInteger:
                case ParameterType.kIndexedTextList:
                case ParameterType.kIndexedList:
                    strMin = param.Min.ToString();
                    strMax = param.Max.ToString();
                    strDefault = param.DefaultValue.ToString();

                    break;

                case ParameterType.kBoolean:
                    strDefault = param.BooleanDefaultValue.ToString();
                    strMin = strMax = "";
                    break;

                case ParameterType.kDouble:
                    strMin = param.DoubleMin.ToString();
                    strMax = param.DoubleMax.ToString();
                    strDefault = param.DoubleDefaultValue.ToString();
                    break;
            }
        }



        /// <summary>
        /// 受け取ったparamの各情報を、GUIへ表示する
        /// </summary>
        /// <param name="param"></param>
        private void DispParamInfo(IParameter param)
        {

            //GUIに情報を表示

            SelectParamCommentText.Text = param.Description;
            SelectParamText.Text = param.Id;

            string strMin = "";
            string strMax = "";
            string strDefault = "";


            GetStringInfoValue(param, ref strMin, ref strMax, ref strDefault);

            SelectParamMinText.Text = strMin;
            SelectParamMaxText.Text = strMax;
            SelectParamDefaultText.Text = strDefault;

            SelectParamTypeText.Text = param.Type.ToString();
            if (param.Type == ParameterType.kIndexedList)
            {
                //タイプが　kIndexedList　の時
                List<double> ParamTable = new List<double>(param.ListValues.Cast<double>());
                SelectParamTypeText.Text += ("=>(" + ParamTable[param.Value].ToString() + ")");
            }

            if (param.Type == ParameterType.kIndexedTextList)
            {
                //タイプが　kIndexedTextList の時
                List<string> ParamTable = new List<string>(param.TextListValues.Cast<string>());
                SelectParamTypeText.Text += ("=>(" + ParamTable[param.Value] + ")");
            }

            SelectParamTypeText.Text += param.Units;


            SelectCurrentMemoryText.Text = _pmg.GetSelectProduct().CurrentMemory.ToString();


            //DSP 情報 
            if (deviceInfo.FirmwareId.Contains("E7111"))
            {
                //有線接続

            }
            else if (deviceInfo.FirmwareId.Contains("E7160"))
            {
                //無線接続
                ////string strmac = _pmg.GetSelectProduct().DeviceMACAddress;    //MACアドレス取得　遅い
                ////WirelessDeviceMacText.Text = strmac;

                string strDeviceName = SDFacade.PmGetWirelessDeviceName();
                WirelessDeviceNameText.Text = strDeviceName;
            }
        }



        //--------------------------------------------------------------------------------
        // DataGridの値を、メモリにも書き込む
        //--------------------------------------------------------------------------------
        public void MemoryWriteParam()
        {
            if (SelectParamName == null) return;

            //
            //ParamDataGrid
            //

            string inputParamName = SelectParamName;    //パラメータ名
            int iColumnCount = 1;

            if (ParamSystem.IsChecked == true)
            {
                //
                // システムメモリ
                //
                if (SelectParamName.EndsWith("*"))
                {
                    inputParamName = SelectParamName.Substring(0, SelectParamName.Length - 1);
                    iColumnCount = _uniqueSystemArrayDictionary[inputParamName];
                }


                // 
                // 表示する１行分のデータを作成
                //
                int iCount = 0;
                IParameter param = null;


                if (SelectParamName.EndsWith("*"))
                {
                    for (int i = 1; i < iColumnCount; i++)
                    {
                        param = SDFacade.PmFindSystemParameter(inputParamName + "[" + i.ToString() + "]");
                        string temp = (string)(ParamDataGrid.Items[0] as DataRowView).Row[i];   //[1][2]・・・
                        SDFacade.PmSetStringValue(param, temp);   //データの書き込み
                    }
                }
                else
                {
                    param = SDFacade.PmFindSystemParameter(SelectParamName);
                    string temp = (string)(ParamDataGrid.Items[0] as DataRowView).Row[1];       //[1]                    
                    SDFacade.PmSetStringValue(param, temp);   //データの書き込み
                }
            }
            else
            {
                //
                // メモリ（A～H）
                //
                if (SelectParamName.EndsWith("*"))
                {
                    inputParamName = SelectParamName.Substring(0, SelectParamName.Length - 1); //パラメータ名から"＊"除去
                    iColumnCount = _uniqueArrayDictionary[inputParamName];
                }


                // 
                // 表示する１行分のデータを作成（メモリA～H毎）
                //
                int iCount = 0;
                IParameter param = null;

                foreach (IParameterMemory paramMemory in SDFacade.PmGetMemoryList())      //A～H
                {
                    if (SelectParamName.EndsWith("*"))
                    {
                        for (int i = 0; i < iColumnCount; i++)
                        {
                            param = paramMemory.Parameters.GetById(inputParamName + "[" + i.ToString() + "]");
                            string temp = (string)(ParamDataGrid.Items[iCount] as DataRowView).Row[i + 1];      //[1][2]・・・
                            SDFacade.PmSetStringValue(param, temp);   //データの書き込み
                        }
                    }
                    else
                    {
                        param = paramMemory.Parameters.GetById(SelectParamName);
                        string temp = (string)(ParamDataGrid.Items[iCount] as DataRowView).Row[1];      //[1]
                        SDFacade.PmSetStringValue(param, temp);    //データの書き込み
                    }

                    iCount++;
                }

            }

            //SDFacade._pmg._product.WriteParameters(ParameterSpace.kSystemActiveMemory);

        }




        //
        // デバイスクローズ
        //
        private void closeDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            //////try
            //////{
            //////    this.CloseDevice();
            //////}
            //////catch (Exception ex)
            //////{
            //////    ButtonStatusChange(comState.Disconnected);
            //////    MessageBox.Show(ex.Message);
            //////    return;
            //////}

            //////ButtonStatusChange(comState.Disconnected);
        }

        private async void readParamters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Task task = Task.Run(() => this.ReadParameters());

                await task;

                RefreshDisplay();   //再描画（パラメータ）
            }
            catch (Exception ex)
            {
                ButtonStatusChange(comState.Disconnected);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private async void writeParamters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //if (SDFacade.PmCheckE7160WriteSystemParameter(deviceInfo) == false)
                //{
                //    MessageBox.Show("E7160の場合、現在のWireless Disableの為、Enableにしてから実行してください！");
                //    return;
                //}


                Task task = Task.Run(() => this.WriteParamters());

                await task;
            }
            catch (Exception ex)
            {
                ButtonStatusChange(comState.Disconnected);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void communicationInterfaceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //////var communicationInterfaceName = this.communicationInterfaceComboBox.SelectedValue.ToString();

            //////try
            //////{
            //////    this.SetCommunicationInterface(communicationInterfaceName);
            //////}
            //////catch (Exception ex)
            //////{
            //////    MessageBox.Show(ex.ToString());
            //////    return;
            //////}
        }

        private void muteCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;

            try
            {
                MuteDevice(cb.IsChecked == true);
            }
            catch (Exception ex)
            {
                ButtonStatusChange(comState.Disconnected);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void muteCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            try
            {
                MuteDevice(cb.IsChecked == true);
            }
            catch (Exception ex)
            {
                ButtonStatusChange(comState.Disconnected);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void playAlertButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.muteCheckBox.IsChecked = false; // DSPの警告を鳴らすにはデバイスのミュート解除が必要
                this.PlayAlert();
            }
            catch (Exception ex)
            {
                ButtonStatusChange(comState.Disconnected);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void UpdateGraph_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Graph();
                GraphTabControl.SelectedIndex = 0;
            }
            catch
            {
                throw;
            }
        }

        //チャープ周波数特性CSV読みだし＆グラフ表示
        private void MenuFileOpen(object sender, RoutedEventArgs e)
        {
            ReadChirpCSVFile();
        }



        /// <summary>
        /// 通信の状態遷移によって操作可能なボタンを有効化する。
        /// </summary>
        /// <param name="comState"></param>
        /// 
        private void ButtonStatusChange(comState comState)
        {
            this._comState = comState;

            switch (this._comState)
            {
                case comState.Disconnected:
                    //this.detectDeviceButton.IsEnabled = true;
                    //this.initializeButton.IsEnabled = true;    //false
                    //this.closeDeviceButton.IsEnabled = false;
                    this.readParamters.IsEnabled = false;
                    this.writeParamters.IsEnabled = false;
                    this.muteCheckBox.IsEnabled = false;
                    this.playAlertButton.IsEnabled = false;
                    this.UpdateGraph.IsEnabled = false;
                    //Append
                    this.FrequencyResponseGainGraph.IsEnabled = false;
                    this.ChirpToolGraph.IsEnabled = false;
                    this.ComEarPort.IsEnabled = true;

                    initConnectionButton.IsEnabled = true;
                    break;

                case comState.Detected:
                    //this.detectDeviceButton.IsEnabled = true;
                    //this.initializeButton.IsEnabled = true;
                    //this.closeDeviceButton.IsEnabled = false;
                    this.readParamters.IsEnabled = false;
                    this.writeParamters.IsEnabled = false;
                    this.muteCheckBox.IsEnabled = false;
                    this.playAlertButton.IsEnabled = false;
                    this.UpdateGraph.IsEnabled = true;
                    //Append
                    this.FrequencyResponseGainGraph.IsEnabled = true;
                    this.ChirpToolGraph.IsEnabled = false;
                    this.ComEarPort.IsEnabled = false;

                    break;

                case comState.Initialized:
                    //this.detectDeviceButton.IsEnabled = true;
                    //this.initializeButton.IsEnabled = true;
                    //this.closeDeviceButton.IsEnabled = true;
                    this.readParamters.IsEnabled = true;
                    this.writeParamters.IsEnabled = true;
                    this.muteCheckBox.IsEnabled = true;
                    this.muteCheckBox.IsChecked = true;
                    this.playAlertButton.IsEnabled = true;
                    this.UpdateGraph.IsEnabled = true;

                    //Append
                    this.FrequencyResponseGainGraph.IsEnabled = true;
                    this.ChirpToolGraph.IsEnabled = true;
                    this.ComEarPort.IsEnabled = true;
                    initConnectionButton.IsEnabled = true;
                    break;
            }
        }
        #endregion

        #region グラフ関連
        /// <summary>
        /// グラフ(Visifire.chart)の初期設定
        /// </summary>
        void InitializeChart()
        {
            //グラフ初期化　Append
            chart.Titles.Clear();
            chart.AxesX.Clear();
            chart.AxesY.Clear();
            chart.Series.Clear();
            graphFreuquencyPoints.Clear();


            chart.Titles.Add(new Visifire.Charts.Title() { Text = "Frequency Response Curves" });
            // X軸の設定
            chart.AxesX.Add(new Axis() { AxisMinimum = 62.5, AxisMaximum = 12000, Logarithmic = true, LogarithmBase = 2 }); // X軸の範囲と対数グラフ(底は2)
            chart.AxesX[0].AxisLabels = new AxisLabels() { Interval = 1 };
            chart.AxesX[0].Ticks.Add(new Ticks() { Interval = 1 });
            chart.AxesX[0].Grids.Add(new Visifire.Charts.ChartGrid() { Interval = 1 });

            // Y軸の設定
            chart.AxesY.Add(new Axis() { AxisMinimum = 0, AxisMaximum = 120 }); // Y軸の範囲 0 - 120 [dB]
            chart.AxesY[0].AxisLabels = new AxisLabels() { Interval = 10 }; // 10[dB]間隔で軸ラベル
            chart.AxesY[0].Ticks.Add(new Ticks() { Interval = 1 }); // チックスは1[dB]刻み
            chart.AxesY[0].Grids.Add(new Visifire.Charts.ChartGrid() { Interval = 5 }); // 格子線は5[dB]刻み

            // 念のためVisifireのグラフ(chartオブジェクト)の保持するグラフデータ(Series)を初期化
            //chart.Series.Clear();

            for (int i = 0; i < frequencyPointNumber; i++)
            {
                int fp = (i + 1) * 100; // 100[Hz]からグラフの計算をしたいので、i + 1として最小の周波数100[Hz]から
                graphFreuquencyPoints.Add(fp); // Visifireのグラフライブラリーに周波数ポイントを渡すための配列の初期化
                sdGraphXData[i] = fp; // 上記と同じ数値だがSDのグラフライブラリーに周波数ポイントを渡すための型の配列を初期化
            }

            foreach (var inputLevel in dataSerise.Keys) // 補聴器への入力音圧レベル 40 - 100 [dB]毎に周波数レスポンス曲線の計算&描画
            {
                dataSerise[inputLevel].DataPoints.Clear();  //Append

                for (int i = 0; i < graphFreuquencyPoints.Count(); ++i)
                {
                    var dp = new DataPoint()
                    {
                        XValue = graphFreuquencyPoints[i],
                        YValue = -100/*グラフの初期状態は-100[dB]で仮にセット*/
                    };

                    dataSerise[inputLevel].DataPoints.Add(dp); // Visifireのグラフのデータをストアしている配列(dataSerise)に値をセット
                }

                dataSerise[inputLevel].RenderAs = RenderAs.QuickLine; // グラフの種類(SplineとかStepとかPointとか様々選べます)

                dataSerise[inputLevel].LegendText = inputLevel.ToString(); // グラフの凡例のテキスト、ここでは入力音圧レベル

                chart.Series.Add(dataSerise[inputLevel]); // グラフオブジェクト(chart)に上記で用意した設定(dataSeriesオブジェクトのプロパティ)をセット
            }

            chart.ScrollingEnabled = false; //  trueにするとデータの範囲が広い場合グラフのスクロールが可能だが、全てのデータをスクロール無しで表示させたいのでスクロールを無効
        }

        //
        // グラフフォーマット　周波数ゲイン用
        //
        void InitializeChart2()
        {
            //グラフ初期化　Append
            chart2.Titles.Clear();
            chart2.AxesX.Clear();
            chart2.AxesY.Clear();
            chart2.Series.Clear();
            graphFreuquencyPoints2.Clear();


            chart2.Titles.Add(new Visifire.Charts.Title() { Text = "Frequency Response(Gain) Curves" });
            // X軸の設定
            chart2.AxesX.Add(new Axis() { AxisMinimum = 62.5, AxisMaximum = 12000, Logarithmic = true, LogarithmBase = 2 }); // X軸の範囲と対数グラフ(底は2)
            chart2.AxesX[0].AxisLabels = new AxisLabels() { Interval = 1 };
            chart2.AxesX[0].Ticks.Add(new Ticks() { Interval = 1 });
            chart2.AxesX[0].Grids.Add(new Visifire.Charts.ChartGrid() { Interval = 1 });

            // Y軸の設定
            chart2.AxesY.Add(new Axis() { AxisMinimum = -50, AxisMaximum = 50 }); // Y軸の範囲 -50 - +50 [dB]
            chart2.AxesY[0].AxisLabels = new AxisLabels() { Interval = 10 }; // 10[dB]間隔で軸ラベル
            chart2.AxesY[0].Ticks.Add(new Ticks() { Interval = 1 }); // チックスは1[dB]刻み
            chart2.AxesY[0].Grids.Add(new Visifire.Charts.ChartGrid() { Interval = 5 }); // 格子線は5[dB]刻み

            // 念のためVisifireのグラフ(chartオブジェクト)の保持するグラフデータ(Series)を初期化
            //chart.Series.Clear();

            for (int i = 0; i < frequencyPointNumber2; i++)
            {
                int fp = (i + 1) * 100; // 100[Hz]からグラフの計算をしたいので、i + 1として最小の周波数100[Hz]から
                graphFreuquencyPoints2.Add(fp); // Visifireのグラフライブラリーに周波数ポイントを渡すための配列の初期化
                sdGraphXData2[i] = fp; // 上記と同じ数値だがSDのグラフライブラリーに周波数ポイントを渡すための型の配列を初期化
            }

            foreach (var inputLevel in dataSerise2.Keys) // 補聴器への入力音圧レベル 40 - 100 [dB]毎に周波数レスポンス曲線の計算&描画
            {
                dataSerise2[inputLevel].DataPoints.Clear();  //Append

                for (int i = 0; i < graphFreuquencyPoints2.Count(); ++i)
                {
                    var dp = new DataPoint() { XValue = graphFreuquencyPoints2[i], YValue = -100/*グラフの初期状態は-100[dB]で仮にセット*/ };

                    dataSerise2[inputLevel].DataPoints.Add(dp); // Visifireのグラフのデータをストアしている配列(dataSerise)に値をセット
                }

                dataSerise2[inputLevel].RenderAs = RenderAs.QuickLine; // グラフの種類(SplineとかStepとかPointとか様々選べます)

                dataSerise2[inputLevel].LegendText = inputLevel.ToString(); // グラフの凡例のテキスト、ここでは入力音圧レベル

                chart2.Series.Add(dataSerise2[inputLevel]); // グラフオブジェクト(chart)に上記で用意した設定(dataSeriesオブジェクトのプロパティ)をセット
            }

            chart2.ScrollingEnabled = false; //  trueにするとデータの範囲が広い場合グラフのスクロールが可能だが、全てのデータをスクロール無しで表示させたいのでスクロールを無効  
        }


        //
        // グラフフォーマット　チャープ用
        //
        void InitializeChart3()
        {
            //グラフ初期化　Append
            chart3.Titles.Clear();
            chart3.AxesX.Clear();
            chart3.AxesY.Clear();
            chart3.Series.Clear();
            graphFreuquencyPoints3.Clear();


            chart3.Titles.Add(new Visifire.Charts.Title() { Text = "ChirpTool Response Curves" });
            // X軸の設定
            chart3.AxesX.Add(new Axis() { AxisMinimum = 100, AxisMaximum = 12000, Logarithmic = true, LogarithmBase = 2 }); // X軸の範囲と対数グラフ(底は2)
            chart3.AxesX[0].AxisLabels = new AxisLabels() { Interval = 1 };
            chart3.AxesX[0].Ticks.Add(new Ticks() { Interval = 1 });
            chart3.AxesX[0].Grids.Add(new Visifire.Charts.ChartGrid() { Interval = 1 });

            // Y軸の設定
            chart3.AxesY.Add(new Axis() { AxisMinimum = -80, AxisMaximum = 10 }); // Y軸の範囲 -50 - +50 [dB]
            chart3.AxesY[0].AxisLabels = new AxisLabels() { Interval = 10 }; // 10[dB]間隔で軸ラベル
            chart3.AxesY[0].Ticks.Add(new Ticks() { Interval = 1 }); // チックスは1[dB]刻み
            chart3.AxesY[0].Grids.Add(new Visifire.Charts.ChartGrid() { Interval = 5 }); // 格子線は5[dB]刻み


            double[] sdGraphXData3 = chirpToolFrequencyResult[0].ToArray();
            int frequencyPointNumber3 = chirpToolFrequencyResult[0].Count();

            foreach (var inputLevel in dataSerise3.Keys) // 補聴器への入力音圧レベル 40 - 100 [dB]毎に周波数レスポンス曲線の計算&描画
            {
                dataSerise3[inputLevel].DataPoints.Clear();  //Append

                for (int i = 0; i < frequencyPointNumber3; ++i)
                {
                    double xx = chirpToolFrequencyResult[0][i];
                    if (xx == 0) xx = 0.00001;    //?? なぜか？　要調査！！！

                    var dp = new DataPoint() { XValue = xx, YValue = -100/*グラフの初期状態は-100[dB]で仮にセット*/ };

                    dataSerise3[inputLevel].DataPoints.Add(dp); // Visifireのグラフのデータをストアしている配列(dataSerise)に値をセット
                }

                dataSerise3[inputLevel].RenderAs = RenderAs.QuickLine; // グラフの種類(SplineとかStepとかPointとか様々選べます)

                dataSerise3[inputLevel].LegendText = inputLevel.ToString(); // グラフの凡例のテキスト、ここでは入力音圧レベル

                chart3.Series.Add(dataSerise3[inputLevel]); // グラフオブジェクト(chart)に上記で用意した設定(dataSeriesオブジェクトのプロパティ)をセット
            }

            chart3.ScrollingEnabled = false; //  trueにするとデータの範囲が広い場合グラフのスクロールが可能だが、全てのデータをスクロール無しで表示させたいのでスクロールを無効     

        }


        /// <summary>
        /// グラフ表示の更新
        /// </summary>
        private void Graph()
        {
            InitializeChart();  //Append

            var graphDefinitionList = _pmg.GetSelectProduct().Graphs; // SDのグラフを計算するための元になるオブジェクト

            var graphDefinition = graphDefinitionList.GetById(GraphId.kFrequencyResponseGraph);// 周波数レスポンス曲線のグラフを取得。I/Oやサウンドジェネレータなどの他の種類のグラフもここで選ぶ事が可能

            IGraph sdGraphObj;
            if (graphDefinition == null)
            {
                return;
            }

            sdGraphObj = graphDefinition.CreateGraph();
            sdGraphObj.SetDomain(sdGraphXData.Count(), sdGraphXData);

            for (int i = 0; i < this.dataSerise.Count; i++)
            {
                var inputLevel = this.dataSerise.Keys[i];

                var sdGraphSettings_InputLevel = sdGraphObj.GraphSettings.GetById("InputLevel"); // SDのグラフ計算時の設定オブジェクトを取得。ここでは入力音圧レベル

                sdGraphSettings_InputLevel.DoubleValue = inputLevel; // グラフのライン毎(40 - 100 dB)

                sdGraphObj.CalculatePoints(sdGraphYData.Count(), sdGraphYData);

                for (int j = 0; j < this.graphFreuquencyPoints.Count; j++)
                {
                    chart.Series[i].DataPoints[j].XValue = sdGraphXData[j];
                    chart.Series[i].DataPoints[j].YValue = sdGraphYData[j];
                }
            }
        }
        #endregion

        #region その他
        /// <summary>
        /// MainWindowをクローズするときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.communicationInterface = this.communicationInterfaceComboBox.SelectedItem.ToString();// 選択されたプログラマーを取得

            Properties.Settings.Default.Save();// アプリケーションの設定を保存 (保存場所フォルダー: AppData/Local/Makichie/SDSample???/)
        }
        #endregion

        #endregion


        #region 追加
        /// <summary>
        /// グラフ表示 周波数応答ゲイン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrequencyResponseGainGraph_Click(object sender, RoutedEventArgs e)
        {
            GraphTabControl.SelectedIndex = 1;

            InitializeChart2();  //Append


            var graphDefinitionList = _pmg.GetSelectProduct().Graphs; // SDのグラフを計算するための元になるオブジェクト

            var graphDefinition = graphDefinitionList.GetById(GraphId.kFrequencyResponseGainGraph);// 周波数レスポンス曲線のグラフを取得。I/Oやサウンドジェネレータなどの他の種類のグラフもここで選ぶ事が可能

            IGraph sdGraphObj;
            if (graphDefinition == null)
            {
                return;
            }

            sdGraphObj = graphDefinition.CreateGraph();
            sdGraphObj.SetDomain(sdGraphXData.Count(), sdGraphXData2);

            for (int i = 0; i < this.dataSerise2.Count; i++)
            {
                var inputLevel = this.dataSerise2.Keys[i];

                var sdGraphSettings_InputLevel = sdGraphObj.GraphSettings.GetById("InputLevel"); // SDのグラフ計算時の設定オブジェクトを取得。ここでは入力音圧レベル

                sdGraphSettings_InputLevel.DoubleValue = inputLevel; // グラフのライン毎(40 - 100 dB)

                sdGraphObj.CalculatePoints(sdGraphYData2.Count(), sdGraphYData2);

                for (int j = 0; j < this.graphFreuquencyPoints2.Count; j++)
                {
                    chart2.Series[i].DataPoints[j].XValue = sdGraphXData2[j];
                    chart2.Series[i].DataPoints[j].YValue = sdGraphYData2[j];
                }
            }


        }


        /// <summary>
        /// チャープツール
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChirpToolGraph_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Task task = ChirpToolExecute();
                GraphTabControl.SelectedIndex = 2;

            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// チャープツール本体
        /// </summary>
        private async Task ChirpToolExecute()
        {
            List<double> copyFreqResp = new List<double>();
            List<double> copyImpulseResp = new List<double>();
            List<double> modifiedFreqData = new List<double>();
            double[] xPointsFreq;

            //ChirpToolGraph.IsEnabled = false;

            try
            {
                //チャープツール　パラメータ取得＆設定
                double dEndFrequency = Convert.ToDouble(ChirpEndFreq.Text);
                int iMaxSequenceNumber = (int)ChirpPreAvgSlider.Value;
                int iChirpLength = (int)ChirpSignalLenSlider.Value;
                int iLevelIndex = (int)ChirpSignalLevelSlider.Value;
                double dStartFrequency = Convert.ToDouble(ChirpStartFreq.Text);

                ChirpToolParamSet(dEndFrequency, iMaxSequenceNumber, iChirpLength, iLevelIndex, dStartFrequency);


                //チャープツール実行
                var chirp_result = _pmg.GetSelectProduct().BeginRunChirpTool();

                while (!chirp_result.IsFinished)
                {
                    var pr = chirp_result.GetProgressValue();
                    ChirpProgressBar.Value = pr;
                    await Task.Delay(50);
                }

                ChirpProgressBar.Value = 100;

                //追加　実行結果確認
                if (_pmg.GetSelectProduct().ChirpToolStatus != ChirpToolStatusType.kChirpOK)
                {
                    if (_pmg.GetSelectProduct().ChirpToolStatus == ChirpToolStatusType.kChirpLimit)
                    {
                        MessageBox.Show(@"フィードバック経路の測定信号がリミッターによって制限されています。\n出力制限を引き上げるか、チャープ信号のレベルを減少させてください");
                    }

                    if (_pmg.GetSelectProduct().ChirpToolStatus == ChirpToolStatusType.kChirpSaturated)
                    {
                        MessageBox.Show(@"フィードバック経路の測定信号が入力で飽和しています。\nチャープ信号のレベルを減少させてください");

                    }
                }


                var chirp_impulse = _pmg.GetSelectProduct().ChirpToolImpulseResponse;
                var chirp_Frequency = _pmg.GetSelectProduct().ChirpToolFrequencyResponse;

                int num = 0;

                //Get Impulse data
                double[] chirpImpulseData = new double[chirp_impulse.Count];
                num = 0;
                foreach (double d in chirp_impulse)
                {
                    chirpImpulseData[num++] = d;
                }

                //Get Frequency Data
                double[] chirpFrequencyData = new double[chirp_Frequency.Count];
                num = 0;

                foreach (double d in chirp_Frequency)
                {
                    chirpFrequencyData[num++] = d;
                }

                //List<double> convertedImpulseData = new List<double>(product.ChirpToolImpulseResponse.Cast<double>());
                //List<double> convertedFrequencyData = new List<double>(product.ChirpToolFrequencyResponse.Cast<double>());

                ConvertToDouble(chirp_Frequency, ref copyFreqResp);     //List<double> copyFreqRespにデータを格納
                ConvertToDouble(chirp_impulse, ref copyImpulseResp);    //List<double> copyImpulseRespにデータを格納

                modifiedFreqData = new List<double>(copyFreqResp);      //modifiedFreqData ← copyFreqResp コピー
                xPointsFreq = new double[chirp_Frequency.Count];        //double xPointsFreq[]エリア確保
                MakeXPointsFreq(ref xPointsFreq);                       //xPointsFreq ← 周波数計算し配列に格納
                ConvertFreqToDB(ref modifiedFreqData, copyFreqResp);    //modifiedFreqData←copyFreqRespをDB変換し格納


                List<double> freqDbList = new List<double>(xPointsFreq);//xPointsFreqをList<double>に変換

                chirpToolFrequencyResult = new List<List<double>>();    //
                chirpToolFrequencyResult.Add(freqDbList);               //追加　周波数を格納
                chirpToolFrequencyResult.Add(modifiedFreqData);         //追加　DB値を格納


                WriteArrayToCsv2<double>(@"c:\temp\frequency.csv", chirpToolFrequencyResult); //周波数特性をファイルに保存
                WriteArrayToCsv(@"c:\temp\Impulse.csv", chirpImpulseData);



                ChirpToolFrequencyResponseGraphDisplay();               //グラフ表示

            }
            catch (Exception ex)
            {

            }
            finally
            {

                //ChirpToolGraph.IsEnabled = true;
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="dEndFrequency"></param>
        /// <param name="iMaxSequenceNumber"></param>
        /// <param name="iChirpLength"></param>
        /// <param name="iLevelIndex"></param>
        /// <param name="dStartFrequency"></param>
        private void ChirpToolParamSet(double dEndFrequency, int iMaxSequenceNumber, int iChirpLength, int iLevelIndex, double dStartFrequency)
        {
            try
            {

                //if (SDFacade.PmCheckE7160WriteSystemParameter(deviceInfo) == false)
                //{
                //    MessageBox.Show("E7160の場合、現在のWireless Disableの為、Enableにしてから実行してください！");
                //    return;
                //}

                var param1 = _pmg.GetSelectProduct().SystemMemory.Parameters.GetById("X_CT_EndFrequency");
                var param2 = _pmg.GetSelectProduct().SystemMemory.Parameters.GetById("X_CT_MaxSequenceNumber");
                var param3 = _pmg.GetSelectProduct().SystemMemory.Parameters.GetById("X_CT_ChirpLength");
                var param4 = _pmg.GetSelectProduct().SystemMemory.Parameters.GetById("X_CT_LevelIndex");
                var param5 = _pmg.GetSelectProduct().SystemMemory.Parameters.GetById("X_CT_StartFrequency");

                if (param1 != null)
                {
                    param1.DoubleValue = dEndFrequency;
                }

                if (param2 != null)
                {
                    param2.Value = iMaxSequenceNumber;
                }

                if (param3 != null)
                {
                    param3.Value = iChirpLength;
                }

                if (param4 != null)
                {
                    param4.Value = iLevelIndex;
                }

                if (param5 != null)
                {
                    param5.DoubleValue = dStartFrequency;
                }

                _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kSystemNvmMemory);

            }
            catch (Exception ex)
            {
                throw ex;
            }


        }


        private void ChirpToolFrequencyResponseGraphDisplay()
        {

            InitializeChart3();  //Append

            for (int i = 0; i < this.dataSerise3.Count; i++)
            {
                var inputLevel = this.dataSerise3.Keys[i];


                double[] sdGraphXData2 = chirpToolFrequencyResult[0].ToArray();

                for (int j = 0; j < chirpToolFrequencyResult[0].Count(); j++)
                {
                    double xx = chirpToolFrequencyResult[0][j];
                    double yy = chirpToolFrequencyResult[1][j];
                    if (j == 0)
                    {
                        continue;
                    }

                    chart3.Series[i].DataPoints[j].XValue = xx;
                    chart3.Series[i].DataPoints[j].YValue = yy;
                }
            }

        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="list">List<T>のサイズは必ず同サイズであること</param>
        static void WriteArrayToCsv2<T>(string filePath, List<List<T>> list, int decimalPoint = 2)
        {
            try
            {
                // StreamWriterを作成
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    for (int i = 0; i < list[0].Count; i++)
                    {
                        for (int j = 0; j < list.Count; j++)
                        {
                            var value = list[j][i];
                            if ((typeof(T) == typeof(double)) || (typeof(T) == typeof(float)))
                            {
                                // <T> が少数なら丸める
                                double dTemp = Convert.ToDouble(value);
                                dTemp = Math.Round(dTemp, decimalPoint);
                                writer.Write(dTemp.ToString());
                            }
                            else
                            {
                                writer.Write(value.ToString());
                            }

                            if (j < list.Count - 1)
                            {
                                writer.Write(",");
                            }
                        }
                        writer.Write("\n");
                    }
                }

                Console.WriteLine("CSVファイルに書き込みました: " + filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("エラーが発生しました: " + ex.Message);
            }
        }

        static void WriteArrayToCsv(string filePath, double[] values)
        {
            try
            {
                // StreamWriterを作成
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // CSV形式でファイルに書き込む
                    foreach (double value in values)
                    {
                        writer.Write(value);
                        writer.Write("\n"); // 
                    }
                }

                Console.WriteLine("CSVファイルに書き込みました: " + filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("エラーが発生しました: " + ex.Message);
            }
        }


        static IEnumerable<T> ReadCsvFile<T>(string filePath, int itemCount, Func<string[], T> func)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    List<string> lines = new List<string>();

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        lines.Add(line);
                    }

                    List<T> entities = new List<T>();

                    // １ライン処理
                    foreach (string line in lines)
                    {
                        var items = line.Split(',');
                        if (items.Length != itemCount)
                        {
                            throw new Exception("読み込むCSVファイルの形式が異なります");
                        }

                        var entity = func(items);
                        entities.Add(entity);
                    }
                    return entities;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CSVファイルの読み込み中にエラーが発生しました: {ex.Message}");
                return null;
            }

        }

        //チャープツール　周波数特性用
        private ChirpFreqCSV funcFreqCSV(string[] items)
        {
            var entity = new ChirpFreqCSV();

            entity.freq = Convert.ToDouble(items[0]);
            entity.data = Convert.ToDouble(items[1]);

            return entity;
        }

        //チャープツール　周波数特性用形式
        private class ChirpFreqCSV
        {
            public double freq;
            public double data;
        }


        void ReadChirpCSVFile()
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "チャープ出力周波数特性ファイル (*.csv)|*.csv|すべてのファイル (*.*)|*.*";

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                // CSVファイルが選択された
                string selectedFilePath = openFileDialog.FileName;

                IEnumerable<ChirpFreqCSV> ls = ReadCsvFile<ChirpFreqCSV>(selectedFilePath, 2, funcFreqCSV);
                try
                {
                    List<double> freqList = ls.Select(x => x.freq).ToList();
                    List<double> dataList = ls.Select(x => x.data).ToList();

                    chirpToolFrequencyResult = new List<List<double>>();    //クリア
                    chirpToolFrequencyResult.Add(freqList);                 //追加　周波数を格納
                    chirpToolFrequencyResult.Add(dataList);                 //追加　DB値を格納

                    ChirpToolFrequencyResponseGraphDisplay();               //グラフ表示        
                }
                catch
                {
                    MessageBox.Show("読み込むCSVファイルの形式が異なります");
                    //throw new Exception("読み込むCSVファイルの形式が異なります");
                }

            }

        }





        private void ChirpPreAvgSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int iValue = (int)ChirpPreAvgSlider.Value;
            int iPreAvgNum = (int)Math.Pow(2, iValue);
            ChirpPreAvg.Text = iPreAvgNum.ToString();
        }

        private void ChirpSignalLenSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int iValue = (int)ChirpSignalLenSlider.Value;
            int iSignalLen = 1024;

            for (int i = 0; i < iValue; i++)
            {
                iSignalLen = iSignalLen * 2;
            }
            ChirpSignalLen.Text = iSignalLen.ToString();
        }

        private void ChirpSignalLevelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int iValue = (int)ChirpSignalLevelSlider.Value;
            int iSignalLevel = -(iValue * 2);
            ChirpSignalLevel.Text = iSignalLevel.ToString();
        }


        //=================================================================================
        // Helper
        //=================================================================================
        private void ConvertToDouble(IChirpToolResultList data, ref List<double> convertedData)
        {
            convertedData = new List<double>(data.Cast<double>());
        }

        private void ConvertFreqToDB(ref List<double> convertedData, List<double> rawData)
        {
            for (int i = 0; i < _pmg.GetSelectProduct().ChirpToolFrequencyResponse.Count; i++)
            {
                convertedData[i] = 20.0 * Math.Log10(rawData[i]);
            }
        }

        private void MakeXPointsFreq(ref double[] xPoints)
        {
            double sampleRate = _pmg.GetSelectProduct().SampleRate;
            xPoints[0] = 0.0;
            int iFreqCount = _pmg.GetSelectProduct().ChirpToolFrequencyResponse.Count;

            xPoints[iFreqCount - 1] = sampleRate / 2.0;
            double spaceBetween = sampleRate / 2.0 / (double)(iFreqCount - 1);
            for (int i = 1; i < iFreqCount - 1; i++)
            {
                xPoints[i] = xPoints[i - 1] + spaceBetween;
            }
        }



        #endregion

        private void RLSetting_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void RLSetting_Unchecked(object sender, RoutedEventArgs e)
        {

        }



        //=============================================//
        string SelectedComPortItem = "RSL10";

        //public const string COMPort = "COM5"; // Required for RSL10 
        // public const string ParameterLockKey = "";
        //public const string WirelessDeviceName = "LE-Bose Revolve+ II SoundL"; //"7160SL_iOS";
        //===============================================//
        //private void RLS10_Click_1(object sender, RoutedEventArgs e)
        //{

        //    try
        //    {
        //        RLS10_Click(null, null);    //自動接続
        //    }
        //    catch(Exception ex)
        //    {
        //        MessageBox.Show("エラー：" + ex.Message);
        //    }
        //}


        //
        // 【自動接続】ボタン押下
        //
        private void RLS10_AutoClick(object sender, RoutedEventArgs e)
        {
            RLS10_Click(null, null);
        }


        //========================================================================================
        //   自動接続
        //
        //========================================================================================
        // ① スキャン
        // ② コネクト
        // ③ デバイスDetect
        // ④ libraryファイル読み込み　_pmg._libraryに格納　（共通）
        // ⑤ Detectしたものとコンパチのproductを作成し、現在指定中のポート側に保存
        // ⑥ デバイス更新チェック
        // ⑦ product初期化
        //=========================================================================================
        private async void RLS10_Click(object sender, RoutedEventArgs e)
        {
            //DSP初期化ライブラリ
            //string libName = @"C:\Program Files (x86)\ON Semiconductor\SoundDesignerSDK\products\SXx-Library2.library";
            //string libName = @"C:\Program Files (x86)\ON Semiconductor\SoundDesignerSDK\products\SXx-Library3.library";
            //string libName = @"C:\Program Files (x86)\ON Semiconductor\SoundDesignerSDK\products\E7111V2.library";
            //string libName = @"C:\Program Files (x86)\ON Semiconductor\SoundDesignerSDK\products\E7160SL.library";
            //string libName = @"C:\Program Files (x86)\ON Semiconductor\SoundDesignerSDK\products\E7160SL_V1.15.library";
            string libName = "";

            if (Constants.IsProgrammerWireless)
            {
                //libName = @"C:\Program Files (x86)\ON Semiconductor\SoundDesignerSDK\products\E7160SL_V1.15.library";
                //libName = @"E7160SL_V1.15.library";
                libName = @"E7160SL_V1.152.library";
            }
            else
            {
                //libName = @"C:\Program Files (x86)\ON Semiconductor\SoundDesignerSDK\products\SXx-Library3.library";
                libName = @"SXx-Library3.library";
            }


            Progress<int> progress = new Progress<int>(onProgressChangedInt);


            StaticValues.EventInfoData = "";
            StaticValues.EventInfoData2 = "";
            StaticValues.EventInfoData3 = "";

            //StaticValues.ScanList.Clear();      //スキャン取得結果クリア
            //StaticValues.ScanDatas.Clear();
            //ScanDeviceComboBox.Items.Clear();


            //------------------------------------------------------------------------------------------------------------------------
            //  BT検索＆接続確立
            try
            {

                CommonProgressBarText.Content = "スキャン＆無線接続＆Detect中";
                int scanMetod = 0;  //＝０　従来通りスキャンし該当ID検出後コネクト

                var commcheck1 = await _pmg._connectionmanager.ConnectAsync(_port, scanMetod, progress);

                if (!commcheck1)
                {
                    //throw new InvalidOperationException("Unable to connect to the device");
                    throw new InvalidOperationException("デバイスに接続できませんでした");
                }

                CommonProgressBarText.Content = "無線接続完了";

            }
            catch (Exception ex)
            {
                CommonProgressBarText.Content = "スキャンエラー";

                MessageBox.Show("検出出来ません：" + ex.Message);
                return;


                //MessageBoxResult result = MessageBox.Show("スキャンエラーですが、強制実行しますか？",
                //    "メッセージボックス",
                //    MessageBoxButton.YesNo);

                //if (result == MessageBoxResult.Yes)
                //{
                //    // 「はい」ボタン
                //}
                //else if (result == MessageBoxResult.No)
                //{
                //    // 「いいえ」ボタン
                //    return;
                //}

            }



            //------------------------------------------------------------------------------------------------------------------------
            //  接続デバイス検出

            CommonProgressBarText.Content = "接続デバイス情報取得";



            try
            {
                ////MessageBox.Show("デバイスの基本情報を読み込ます");
                deviceInfo = _pmg._connectionmanager.GetConnection(_port).CommAdaptor.DetectDevice();    // デバイスの基本情報を読み込む


                // 検出されたDeviceInfoの各プロパティーを表示
                var di =
                    "FirmwareVersion: " + deviceInfo.FirmwareVersion.ToString() + Environment.NewLine +
                    "FirmwareId: " + deviceInfo.FirmwareId.ToString() + Environment.NewLine +
                    "IsValid: " + deviceInfo.IsValid + Environment.NewLine +
                    "ParameterLockState: " + deviceInfo.ParameterLockState.ToString() + Environment.NewLine +
                    "ChipVersion: " + deviceInfo.ChipVersion.ToString() + Environment.NewLine +
                    "ChipId: " + deviceInfo.ChipId.ToString() + Environment.NewLine +
                    "HybridId: " + deviceInfo.HybridId.ToString() + Environment.NewLine +
                    "HybridRevision: " + deviceInfo.HybridRevision.ToString() + Environment.NewLine +
                    "HybridSerial: " + deviceInfo.HybridSerial.ToString() + Environment.NewLine +
                    "LibraryId: " + deviceInfo.LibraryId.ToString("x8") + Environment.NewLine +
                    "ProductId: " + deviceInfo.ProductId.ToString() + Environment.NewLine +
                    "SerialId: " + deviceInfo.SerialId.ToString() + Environment.NewLine +
                    "RadioApplicationVersion: " + deviceInfo.RadioApplicationVersion.ToString() + Environment.NewLine +
                    "RadioBootloaderVersion: " + deviceInfo.RadioBootloaderVersion.ToString() + Environment.NewLine +
                    "RadioSoftDeviceVersion: " + deviceInfo.RadioSoftDeviceVersion.ToString() + Environment.NewLine;

                DeviceInfoDisplay.Text = di;
            }
            catch (Exception ex)
            {

                MessageBox.Show("_pmg._connectionmanager.GetConnection(_port).CommAdaptor.DetectDevice() で失敗しました");
                return;
                //throw new InvalidOperationException("エラー：" + ex.Message);  
            }


            //------------------------------------------------------------------------------------------------------------------------
            // libraryファイルの読み込み
            try
            {
                CommonProgressBarText.Content = "libraryファイル読み込み";
                //　libraryファイルは固定値にしたが、deviceInfoのlibraryid とlibraryオブジェクトのlibraryidを比較することで自動選択も可能
                //this.library = productManager.LoadLibraryFromFile("SXx-Library3.library");
                //this.library = productManager.LoadLibraryFromFile("E7160SL.library");
                this.library = productManager.LoadLibraryFromFile(libName);
                _pmg._library = library;
            }
            catch (Exception ex)
            {
                MessageBox.Show("libraryファイル読み込みに失敗しました");
                return;
                //throw new InvalidOperationException("エラー：" + ex.Message, ex);
                //return;
            }


            //-------------------------------------------------------------------------------------------------------------------------
            //　検出されたデバイスと、読込んだlibraryファイルのチェック
            var productID = deviceInfo.ProductId; // 検出された物理的に繋がっているDSPに書き込まれているproductID
            var productDefinitionList = this.library.Products;
            try
            {
                CommonProgressBarText.Content = "product作成中";

                var productDefinition = productDefinitionList.GetById(productID);
                var dc = productDefinition.GetDeviceCompatibility(_pmg._connectionmanager.GetConnection(_port).CommAdaptor);

                //product = productDefinition.CreateProduct();
                //_pmg._product = product;      //_product
                _pmg.SetProduct(productDefinition.CreateProduct()); 　//生成したproductを保存
            }
            catch
            {
                MessageBox.Show(" product作成 エラー  productDefinition.GetDeviceCompatibility()");
                //throw;
                return;
            }


            //------------------------------------------------------------------------------------------------------------------------
            // デバイス更新チェック
            try
            {
                ////MessageBox.Show("デバイス更新チェックを実施します");
                CommonProgressBarText.Content = "デバイス更新チェック中";

                var commcheck2 = await _pmg.UpdateDeviceAsync(_port, progress);
                if (!commcheck2)
                {
                    //MessageBox.Show("デバイス更新チェック不適合ですが継続します　_pmg.UpdateDeviceAsync(_port, progress)");
                    //throw new InvalidOperationException("Incompatible device");
                }
            }
            catch
            {
                MessageBox.Show("デバイス更新チェック エラー _pmg.UpdateDeviceAsync(_port, progress)");
                return;

                //throw new InvalidOperationException("デバイス更新チェック エラー");  //終了
                //throw;
            }


            //------------------------------------------------------------------------------------------------------------------------
            // Product初期化（必須）
            try
            {

                ////MessageBox.Show("Productイニシャルを実施します");

                CommonProgressBarText.Content = "Productイニシャル中";
                await _pmg.InitProductAsync(_port, progress);
            }
            catch (Exception ex)
            {
                //throw;
                MessageBox.Show("エラー：" + ex.Message);
            }


            CommonProgressBarText.Content = "DSP接続完了";



            //------------------------------------------------------------------------------------------------------------------------
            // パラメータ解析
            //SDFacade.SDFacadeInitial( productManager, library, null, _port);
            SDFacade.SDFacadeInitial(_pmg);

            string strTemp = "【Load Library名】" + SDFacade._pm._library.Description;
            //strTemp += " 【Product名】" + SDFacade._pm._product.Definition.Description;   //DSPに書き込まれているProduct名
            strTemp += " 【Product名】" + _pmg.GetSelectProduct().Definition.Description;   //DSPに書き込まれているProduct名

            AppLoadLibraryNameText.Text = strTemp;

            ParameterNameInfoCheck();

            ParameterInfoDisp();

            //
            ButtonStatusChange(comState.Initialized);


            MessageBox.Show($"{_port.ToString()} ポートの接続に、成功しました！！");
        }

        //
        // プログレスバー変更イベント
        //
        void onProgressChangedInt(int percent)
        {
            Dispatcher.Invoke(delegate ()
            {
                CommonProgressBar.Value = percent;
            });

        }

        public void onEventInfoChanged(string strMessage)
        {
            Dispatcher.Invoke(delegate ()
            {
                EventInfoBox.Text += "\n" + strMessage;
            });

        }



        private void RadioButton_Checked_Left(object sender, RoutedEventArgs e)
        {
            var check = sender as CheckBox;
            if (communicationInterfaceComboBox != null)
            {
                _port = CommunicationPort.kLeft;
                _pmg.SetSelectPort(_port);          //Product Change
                //_pmg.SetCommunicationInterface(communicationInterfaceComboBox.Text,_port);
                WirelessRSL10_Detect(null, null);   //Detect
            }
        }
        private void RadioButton_Checked_Right(object sender, RoutedEventArgs e)
        {
            var check = sender as CheckBox;
            if (communicationInterfaceComboBox != null)
            {
                _port = CommunicationPort.kRight;
                _pmg.SetSelectPort(_port);          //Product Change
                //_pmg.SetCommunicationInterface(communicationInterfaceComboBox.Text,_port);
                WirelessRSL10_Detect(null, null);   //Detect
            }
        }

        //ワイヤレス時　左ポート設定
        private void RadioButton_Checked_Left_Wireless(object sender, RoutedEventArgs e)
        {
            var check = sender as CheckBox;

            _port = CommunicationPort.kLeft;
            _pmg.SetSelectPort(_port);          //Product Change

            WirelessRSL10_Detect(null, null);   //Detect

            //SDFacade.PmLoadProductManager(_port);
        }

        //ワイヤレス時　右ポート設定
        private void RadioButton_Checked_Right_Wireless(object sender, RoutedEventArgs e)
        {
            var check = sender as CheckBox;

            _port = CommunicationPort.kRight;

            _pmg.SetSelectPort(_port);          //Product Change

            WirelessRSL10_Detect(null, null);   //Detect

            //SDFacade.PmLoadProductManager(_port);
        }


        //スキャンし検出されたものを選択する
        //
        private void ScanDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var ss = StaticValues.ScanList;

                ComboBox cb = sender as ComboBox;

                int iSelectNo = cb.SelectedIndex;
                int iScanListCount = ss.Count;
                if (iSelectNo < 0 || iSelectNo >= iScanListCount) return;

                if (RadioLeftWireless.IsChecked == true)
                {
                    WirelessDeviceName1Text.Text = /*ss[iSelectNo].Item1 + */ss[iSelectNo].Item2;   //Append Item2
                    StaticValues.ScanEventLeft = StaticValues.ScanDatas[iSelectNo];    //左側の結果を保持
                }
                else
                {
                    WirelessDeviceName2Text.Text = /*ss[iSelectNo].Item1 + */ss[iSelectNo].Item2;   //Append Item2
                    StaticValues.ScanEventRight = StaticValues.ScanDatas[iSelectNo];    //右側の結果を保持
                }
            }
            catch
            {

            }
        }

        private void detectDeviceRS10Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //this.DetectDevice("E7160SL.library"); 
                //無線の場合
                WirelessRSL10_Detect(null, null);
            }
            catch (Exception ex)
            {
                ButtonStatusChange(comState.Disconnected);
                MessageBox.Show(ex.Message);
                return;
            }

            ButtonStatusChange(comState.Detected);

        }

        //
        //-------------------------------------------------------------------------------------------------------------------------
        //
        //

        private async void connect_and_read_device_info_Click(object sender, RoutedEventArgs e)
        {
            ProductManager _productmanager;

            _productmanager = new ProductManager();

            var lib = "C:\\Program Files (x86)\\ON Semiconductor\\SoundDesignerSDK\\products\\E7160SL_V1.15.library";
            TestContext.Progress.WriteLine($"\nTEST connect_and_read_device_info, library: {lib}");

            //public const string ProductLibLocation = "C:\\Program Files (x86)\\ON Semiconductor\\SoundDesignerSDK\\products\\";
            //public const string ConnectedDevice = "E7160SL_V1.15";


            _productmanager.Initialize(lib, 0, _port, programmer: "RSL10");

            await _productmanager.ConnectAsync(_port);

            var cdi = _productmanager.GetConnectedDeviceInfo(_port);

            string strDeviceInfo = (
                $"LibraryId                {cdi?.LibraryId}\n" +
                $"ProductId                {cdi?.ProductId}\n" +
                $"ChipId                   {cdi?.ChipId}\n" +
                $"ChipVersion              {cdi?.ChipVersion}\n" +
                $"HybridId                 {cdi?.HybridId}\n" +
                $"FirmwareId               {cdi?.FirmwareId}\n" +
                $"FirmwareVersion          {cdi?.FirmwareVersion}\n" +
                $"SerialId                 {cdi?.SerialId}\n" +
                $"IsValid                  {cdi?.IsValid}\n" +
                $"ParameterLockState       {cdi?.ParameterLockState}\n" +
                $"RadioApplicationVersion  {cdi?.RadioApplicationVersion}\n" +
                $"RadioBootloaderVersion   {cdi?.RadioBootloaderVersion}\n" +
                $"RadioSoftDeviceVersion   {cdi?.RadioSoftDeviceVersion}\n" +
                $"HybridSerial             {cdi?.HybridSerial}\n" +
                $"HybridRevision           {cdi?.HybridRevision}");

            if (Constants.IsProgrammerWireless)
                await _productmanager.CloseWirelessConnection(_port);

            _productmanager?.CloseProduct();
        }

        private async void read_all_params_Click(object sender, RoutedEventArgs e)
        {
            ProductManager _productmanager;

            _productmanager = new ProductManager();

            var lib = "C:\\Program Files (x86)\\ON Semiconductor\\SoundDesignerSDK\\products\\E7160SL_V1.15.library";
            TestContext.Progress.WriteLine($"\nTEST connect_and_read_device_info, library: {lib}");


            _productmanager.Initialize(lib, 0, _port, programmer: "RSL10");



            await _productmanager.ConnectAsync(CommunicationPort.kLeft);
            await _productmanager.ReadParametersAsync(CommunicationPort.kLeft, 0, ReadWriteMode.NVM, _progress);
            if (Constants.IsProgrammerWireless)
                await _productmanager.CloseWirelessConnection(CommunicationPort.kLeft);
        }

        private async void write_all_params_Click(object sender, RoutedEventArgs e)
        {
            /**************************************
            ProductManager _productmanager;

            _productmanager = new ProductManager();

            var lib = "C:\\Program Files (x86)\\ON Semiconductor\\SoundDesignerSDK\\products\\E7160SL_V1.15.library";
            TestContext.Progress.WriteLine($"\nTEST connect_and_read_device_info, library: {lib}");

            //public const string ProductLibLocation = "C:\\Program Files (x86)\\ON Semiconductor\\SoundDesignerSDK\\products\\";
            //public const string ConnectedDevice = "E7160SL_V1.15";


            _productmanager.Initialize(lib, 0, programmer: "RSL10");
            await _productmanager.ConnectAsync(programmerport);
            await _productmanager.WriteParametersAsync(programmerport, memory, mode);
            if (Constants.IsProgrammerWireless)
                await _productmanager.CloseWirelessConnection(programmerport);
            **************************************/
        }

        private async void write_param_file_Click(object sender, RoutedEventArgs e)
        {
            /**************************
            var lib = product + ".library";
            TestContext.Progress.WriteLine($"\nTEST write_param_file, library: {lib}");

            var parampath = Path.Combine(Constants.ProductLibLocation, product + ".param");
            TestContext.Progress.WriteLine($"Loading param from {Path.GetFullPath(parampath)}\nIf this path is inaccurate set it in Constants.cs");
            _productmanager.Initialize(lib, memory, programmer: programmer);

            var paramdata = Param.OpenParamFile(parampath);
            paramdata.memory.ForEach(m => m.param.ForEach(p => _productmanager.SetValueFromFile(_productmanager.FindParameter(p.name, m.id), p.value)));
            paramdata.system.param.ForEach(p => _productmanager.SetValueFromFile(_productmanager.FindSystemParameter(p.name), p.value));

            await _productmanager.ConnectAsync(programmerport);

            await _productmanager.WriteParametersAsync(programmerport, memory, mode);
            if (Constants.IsProgrammerWireless)
                await _productmanager.CloseWirelessConnection(programmerport);
            ***********/
        }

        private void get_frequencyGain_model_Click(object sender, RoutedEventArgs e)
        {

            double[] SixthOctaves = new double[]{
                125     ,
                140     ,
                157     ,
                177     ,
                198     ,
                223     ,
                250     ,
                281     ,
                315     ,
                354     ,
                397     ,
                445     ,
                500     ,
                561     ,
                630     ,
                707     ,
                794     ,
                891     ,
                1000    ,
                1122    ,
                1260    ,
                1414    ,
                1587    ,
                1782    ,
                2000    ,
                2245    ,
                2520    ,
                2828    ,
                3175    ,
                3564    ,
                4000    ,
                4490    ,
                5040    ,
                5657    ,
                6350    ,
                7127    ,
                8000    ,
                8980    ,
                10079   ,
                11314 };


            ProductManager _productmanager;

            _productmanager = new ProductManager();

            var lib = "C:\\Program Files (x86)\\ON Semiconductor\\SoundDesignerSDK\\products\\E7160SL_V1.15.library";


            //TestContext.Progress.WriteLine($"\nTEST get_frequencyGain_model, library: {lib}, product {product}");
            _productmanager.Initialize(lib, 0, _port, programmer: Constants.Programmer);



            var INPUT_LEVEL = 70;
            var data = _productmanager.GetGraphData(GraphId.kFrequencyResponseGainGraph, SixthOctaves, SixthOctaves.Length, INPUT_LEVEL);

            //Assert.False(data[SixthOctaves.Length - 1] == 0, "Freq Gain Data is not valid");
            TestContext.Progress.WriteLine($"Some points: data[5]={Math.Round(data[5], 3)},data[10]={Math.Round(data[10], 3)},data[15]={Math.Round(data[15], 3)},data[20]={Math.Round(data[20], 3)},data[15]={Math.Round(data[25], 3)}");
        }


        private void WirelessDeviceName1Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            StaticValues.WirelessDeviceName1 = WirelessDeviceName1Text.Text;
        }

        private void WirelessDeviceName2Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            StaticValues.WirelessDeviceName2 = WirelessDeviceName2Text.Text;
        }


        //Wireless　Close
        private async void Wireless_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Constants.IsProgrammerWireless)
                {
                    //await _productmanager.CloseWirelessConnection(programmerport);
                    await _pmg.CloseWirelessConnection(_pmg.GetSelectPort());

                    MessageBox.Show(_pmg.GetSelectPort().ToString() + "ポートをクローズしました");
                }
                return;
            }
            catch
            {
                MessageBox.Show(_pmg.GetSelectPort().ToString() + "ポートのクローズに失敗しました");
            }
        }


        private void ParamSystem_Click(object sender, RoutedEventArgs e)
        {
            ParameterInfoDisp();
        }

        private void ParamMemory_Checked(object sender, RoutedEventArgs e)
        {
            ParameterInfoDisp();

        }


        //--------------------------------------------------------------------------------
        // イベント  LISTBOXに表示されているパラメータリストが選択された時
        //--------------------------------------------------------------------------------
        private void ParamListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string ss = ParamListBox.SelectedItem?.ToString();
            SelectParamName = ss;

            RefreshDisplay();   //再描画（パラメータ）
        }

        //再描画
        private void RefreshDisplay()
        {
            MakeParamDataTable();       //(SelectParamName)の情報をDataTableに表示する。
            //GraphDispButtonExecute();   //　グラフ１　表示
            //GraphDispButton2Execute();  //　グラフ２　表示
        }

        private void WriteParamButton_Click(object sender, RoutedEventArgs e)
        {
            MemoryWriteParam();         // DataGrid の内容をメモリに書き込み保存する（メモリ上）

            MakeParamDataTable();       // 画面に反映
        }

        private void SelectMemoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //メモリA～H切り替え時の処理
            int memNo = SelectMemoryComboBox.SelectedIndex;

            SDFacade.PmSetCurrntMemory(memNo);

            RefreshDisplay();
        }

        //
        //メニュ　File - Open - Sound Designer - Liblary File
        //
        private void SDLibraryFileOpen(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // ダイアログで選択できるファイルの種類を設定（例：テキストファイル）
            openFileDialog.Filter = "SDライブラリーファイル (*.library)|*.library|すべてのファイル (*.*)|*.*";


            // ダイアログを表示し、ユーザーがファイルを選択したかどうかをチェック
            if (openFileDialog.ShowDialog() == true)
            {

                // ユーザーが選択したファイルのパスを取得
                string selectedFilePath = openFileDialog.FileName;

                //------------------------------------------------------------------------------------------------------------------------
                // libraryファイルの読み込み
                try
                {
                    CommonProgressBarText.Content = "libraryファイル読み込み";
                    this.library = productManager.LoadLibraryFromFile(selectedFilePath);
                    MessageBox.Show("ファイル名：" + selectedFilePath + "を正常に読み込みました！");
                }
                catch (Exception ex)
                {
                    //throw;
                    //throw new InvalidOperationException("エラー：" + ex.Message, ex);
                    MessageBox.Show("エラーが発生しました：" + ex.Message);
                    return;
                }

            }

        }

        //
        //メニュ　File - Open - Sound Designer - Parameter File 
        //
        private void SDParamFileOpen(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // ダイアログで選択できるファイルの種類を設定（例：テキストファイル）
            openFileDialog.Filter = "SDパラメーターファイル (*.param)|*.param|すべてのファイル (*.*)|*.*";


            // ダイアログを表示し、ユーザーがファイルを選択したかどうかをチェック
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // ユーザーが選択したファイルのパスを取得
                    string selectedFilePath = openFileDialog.FileName;

                    //paramファイルを読み込み
                    var paramdata = Param.OpenParamFile(selectedFilePath);
                    paramdata.memory.ForEach(m => m.param.ForEach(p => SDFacade._pm.SetValueFromFile(SDFacade._pm.FindParameter(p.name, m.id), p.value)));
                    paramdata.system.param.ForEach(p => SDFacade._pm.SetValueFromFile(SDFacade._pm.FindSystemParameter(p.name), p.value));

                    MessageBox.Show("paramファイル名：" + selectedFilePath + "を正常に読み込みました！");

                    RefreshDisplay();   //再描画
                }
                catch (Exception ex)
                {
                    // エラー処理（ファイルが見つからないなど）
                    MessageBox.Show("エラーが発生しました: " + ex.Message);
                }
            }

        }

        //1　【デバイス検索ボタン】　
        private async void WirelessRSL10_Connect(object sender, RoutedEventArgs e)
        {
            Progress<int> progress = new Progress<int>(onProgressChangedInt);


            if (_pmg._connectionmanager == null)
            {

                MessageBox.Show("無線☑を選択し【接続ポート初期化】で初期化してからデバイス検索をして下さい");
                return;
            }


            if ((bool)WirelessScan_check.IsChecked == false)
            {
                MessageBox.Show("WirelessScan☑を選択してからデバイス検索をして下さい");
                return;
            }


            //------------------------------------------------------------------------------------------------------------------------
            //  BT検索＆接続確立
            try
            {
                CommonProgressBarText.Content = "スキャン＆無線接続＆Detect中";
                StaticValues.EventInfoData = "";
                StaticValues.EventInfoData2 = "";
                StaticValues.EventInfoData3 = "";
                StaticValues.ScanList.Clear();      //スキャン取得結果クリア
                StaticValues.ScanDatas.Clear();

                ScanDeviceComboBox.Items.Clear();   //スキャン取得結果コンボBOXクリア

                //
                // ConnectAsync()  Connection.cs で実装 
                // ここで、Scanを実行し、IDを収集
                //
                int scanMetod = 1;  //スキャンのみ実施
                var commcheck1 = await _pmg._connectionmanager.ConnectAsync(_port, scanMetod, progress);
                if (!commcheck1)
                {
                    //throw new InvalidOperationException("Unable to connect to the device");
                    throw new InvalidOperationException("デバイスに接続できませんでした");
                }

            }
            catch (Exception ex)
            {
                //スキャンのみの場合、ここに飛んでくるが、エラーではないので、気にしない！

                MessageBox.Show("デバイススキャン終了　コンボボックスBOXに結果格納");
            }
            finally
            {
                //
                //コンボBOXにスキャン結果を格納
                //ScanDeviceComboBox.Items.Clear();

                foreach (var scanDevice in StaticValues.ScanList)
                {
                    ScanDeviceComboBox.Items.Add(scanDevice.Item1 + "[" + scanDevice.Item2 + "]");
                }

                CommonProgressBarText.Content = "デバイススキャン終了";
            }

        }

        //2
        private void WirelessRSL10_Detect(object sender, RoutedEventArgs e)
        {
            //------------------------------------------------------------------------------------------------------------------------
            //  接続デバイス検出

            CommonProgressBarText.Content = "接続デバイス情報取得";
            DeviceInfoDisplay.Text = "";

            //StaticValues.ScanList.Clear();  //スキャンリストクリア

            try
            {
                deviceInfo = _pmg._connectionmanager?.GetConnection(_port).CommAdaptor.DetectDevice();    // デバイスの基本情報を読み込む


                // 検出されたDeviceInfoの各プロパティーを表示
                var di =
                    "FirmwareVersion: " + deviceInfo.FirmwareVersion.ToString() + Environment.NewLine +
                    "FirmwareId: " + deviceInfo.FirmwareId.ToString() + Environment.NewLine +
                    "IsValid: " + deviceInfo.IsValid + Environment.NewLine +
                    "ParameterLockState: " + deviceInfo.ParameterLockState.ToString() + Environment.NewLine +
                    "ChipVersion: " + deviceInfo.ChipVersion.ToString() + Environment.NewLine +
                    "ChipId: " + deviceInfo.ChipId.ToString() + Environment.NewLine +
                    "HybridId: " + deviceInfo.HybridId.ToString() + Environment.NewLine +
                    "HybridRevision: " + deviceInfo.HybridRevision.ToString() + Environment.NewLine +
                    "HybridSerial: " + deviceInfo.HybridSerial.ToString() + Environment.NewLine +
                    "LibraryId: " + deviceInfo.LibraryId.ToString("x8") + Environment.NewLine +
                    "ProductId: " + deviceInfo.ProductId.ToString() + Environment.NewLine +
                    "SerialId: " + deviceInfo.SerialId.ToString() + Environment.NewLine +
                    "RadioApplicationVersion: " + deviceInfo.RadioApplicationVersion.ToString() + Environment.NewLine +
                    "RadioBootloaderVersion: " + deviceInfo.RadioBootloaderVersion.ToString() + Environment.NewLine +
                    "RadioSoftDeviceVersion: " + deviceInfo.RadioSoftDeviceVersion.ToString() + Environment.NewLine;

                DeviceInfoDisplay.Text = di;
            }
            catch (Exception ex)
            {
                return;
                //throw new InvalidOperationException("エラー：" + ex.Message);  
            }
        }

        //3
        private async void WirelessRSL10_InfoCheck(object sender, RoutedEventArgs e)
        {
            Progress<int> progress = new Progress<int>(onProgressChangedInt);

            //-------------------------------------------------------------------------------------------------------------------------
            //　検出されたデバイスと、読込んだlibraryファイルのチェック
            try
            {
                var productID = deviceInfo.ProductId; // 検出された物理的に繋がっているDSPに書き込まれているproductID
                var productDefinitionList = this.library.Products;

                CommonProgressBarText.Content = "product作成中";

                var productDefinition = productDefinitionList.GetById(productID);
                var dc = productDefinition.GetDeviceCompatibility(_pmg._connectionmanager.GetConnection(_port).CommAdaptor);

                //product = productDefinition.CreateProduct();      //_product
                _pmg.SetProduct(productDefinition.CreateProduct());

            }
            catch
            {
                MessageBox.Show("Productの作成に失敗しました。正常な libraryファイルを読み込んで再実行して下さい！");
                //throw;
            }


            //------------------------------------------------------------------------------------------------------------------------
            // デバイス更新チェック
            try
            {


                var commcheck2 = await _pmg.UpdateDeviceAsync(_port, progress);
                if (!commcheck2)
                {
                    throw new InvalidOperationException("Incompatible device");
                }

                CommonProgressBarText.Content = "デバイス更新チェック完了";

            }
            catch
            {
                //throw;
            }


        }

        //4
        private async void WirelessRSL10_Initialize(object sender, RoutedEventArgs e)
        {
            Progress<int> progress = new Progress<int>(onProgressChangedInt);

            //------------------------------------------------------------------------------------------------------------------------
            // Product初期化（必須）
            try
            {
                CommonProgressBarText.Content = "Productイニシャル中";

                await _pmg.InitProductAsync(_port, progress);

                CommonProgressBarText.Content = "Productイニシャル完了";
            }
            catch (Exception ex)
            {
                //throw;
                MessageBox.Show("エラー： Product初期化" + ex.Message);
            }



            //------------------------------------------------------------------------------------------------------------------------
            // パラメータ解析
            try
            {
                //SDFacade.SDFacadeInitial(productManager, library, null);
                SDFacade.SDFacadeInitial(_pmg);

                string strTemp = "【Load Library名】" + SDFacade._pm._library.Description;
                //strTemp += " 【Product名】" + SDFacade._pm._product.Definition.Description;   //DSPに書き込まれているProduct名 _product
                strTemp += " 【Product名】" + _pmg.GetSelectProduct().Definition.Description;   //DSPに書き込まれているProduct名 


                AppLoadLibraryNameText.Text = strTemp;

                ParameterNameInfoCheck();

                ParameterInfoDisp();

                //
                ButtonStatusChange(comState.Initialized);   //ボタン操作
            }
            catch (Exception ex)
            {
                MessageBox.Show("エラー：パラメータ解析" + ex.Message);
            }
        }

        private void WirelessRSL10_ConnectClose(object sender, RoutedEventArgs e)
        {
            Wireless_Click(null, null);
        }



        /// <summary>
        /// 【デバイス検索ボタン】　
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchDeviceRSL10_Click(object sender, RoutedEventArgs e)
        {
            WirelessRSL10_Connect(null, null);
        }



        //kActiveMemory
        private void WirelessRSL10_Write_kActiveMemory(object sender, RoutedEventArgs e)
        {
            _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kActiveMemory);
        }

        //kSystemNvmMemory
        private void WirelessRSL10_Write_kSystemNvmMemory(object sender, RoutedEventArgs e)
        {

            //if (SDFacade.PmCheckE7160WriteSystemParameter(deviceInfo) == false)
            //{
            //    MessageBox.Show("E7160の場合、現在のWireless Disableの為、Enableにしてから実行してください！");
            //    return;
            //}

            _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kSystemNvmMemory);

        }

        //kSystemActiveMemory
        private void WirelessRSL10_Write_kSystemActiveMemory(object sender, RoutedEventArgs e)
        {
            _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kSystemActiveMemory);
        }

        //kNvmMemory0
        private void WirelessRSL10_Write_kNvmMemory0(object sender, RoutedEventArgs e)
        {
            _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kNvmMemory0);
        }

        //kNvmMemory1
        private void WirelessRSL10_Write_kNvmMemory1(object sender, RoutedEventArgs e)
        {
            _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kNvmMemory1);
        }

        //kNvmMemory2
        private void WirelessRSL10_Write_kNvmMemory2(object sender, RoutedEventArgs e)
        {
            _pmg.GetSelectProduct().WriteParameters(ParameterSpace.kNvmMemory2);
        }


        //
        // WirelessConｔrol
        //
        private void WirelessControlButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IWirelessControl wirelessControl = SDFacade._pm._productmanager.GetWirelessControl();   //wirelessControlsy取得

                wirelessControl.SetCommunicationAdaptor(_pmg._connectionmanager.GetConnection(_port).CommAdaptor);

                IConnectedDeviceList result = SDFacade._pm._productmanager.WirelessGetConnectedDevices();


                int batteryLevel = wirelessControl.BatteryLevel;     //=0

                wirelessControl.ChangeVolume(true);     //OK 

                wirelessControl.ChangeMemory(true);     //OK

                int volume = wirelessControl.Volume;    //=94

                InfoMsgBox.Text += $"BatteryLevel={batteryLevel.ToString()}\n\rVolume={volume.ToString()}";
            }
            catch
            {
                MessageBox.Show("Error!");
            }

        }

        private void ProductControlButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IProduct product = _pmg.GetSelectProduct();

                //product?.ResetDevice();    //リセット　NG NG 

                double vvol = (double)product?.BatteryAverageVoltage;       //OK バッテリー電圧　 =1.284 =1.308
                bool fb = _pmg.GetSelectProduct().Rebooted;                 //OK リブート？　 =false
                int ear = _pmg.GetSelectProduct().Ear;                      //OK 耳左／右　 =0  X_FWK_Ear  0:Left 1:Right




                //DSP 情報 
                if (deviceInfo.FirmwareId.Contains("E7111"))
                {
                    //有線接続

                }
                else if (deviceInfo.FirmwareId.Contains("E7160"))
                {
                    //無線接続
                    string strmac = _pmg.GetSelectProduct().DeviceMACAddress;    //MACアドレス 0x60C0BF149FA1

                    var param = SDFacade.PmFindSystemParameter("X_RF_Enable");
                    string strParam = SDFacade.PmGetStringValue(param);         //0:Disable 1:Enable



                    string strDeviceName = SDFacade.PmGetWirelessDeviceName();
                    WirelessDeviceNameText.Text = strDeviceName;


                    if (SDFacade.PmCheckE7160WriteSystemParameter(deviceInfo))
                    {
                        //書き込みOK！
                        MessageBox.Show("カレントデータが Wireless Enableなので書き込み可能");
                    }
                    else
                    {
                        //書き込みNG
                        MessageBox.Show("カレントデータが Wireless Disableなので書き込み 不可!!");
                    }
                }


            }
            catch
            {
            }
        }

        private void ProductManager_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _pmg?.CloseProduct();


                // SDの大元の呼び出し(GetProductManagerInstance()で生成さるのはシングルトン)
                this.productManager = SDLib.SDLibMain.GetProductManagerInstance();
                _pmg = new ProductManager(productManager, library, null);



                ButtonStatusChange(comState.Disconnected);


                DeviceInfoDisplay.Text = "";
                ParamListBox.ItemsSource = null;
                ParamDataGrid.ItemsSource = null;

                MessageBox.Show("ProductManagerをクローズしました");
            }
            catch
            {
                MessageBox.Show("ProductManagerのクローズに失敗しました");
            }

            //_pmg._connectionmanager.GetConnection(_port).IsConnected = false;
            //_pmg._connectionmanager.GetConnection(_port).IsConnecting = false;



            //DeviceInfoDisplay.Text = "";
            //ParamListBox.ItemsSource = null;
            //ParamDataGrid.ItemsSource = null;

            //MessageBox.Show(_port + "ポートを正常にクローズしました");
        }

        private void initConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            string strProgramer = "";
            if (Constants.IsProgrammerWireless)
            {
                strProgramer = "RSL10";
            }
            else
            {
                strProgramer = communicationInterfaceComboBox.Text;
            }

            _pmg._connectionmanager = new ConnectionManager(productManager, strProgramer);


            StaticValues.Clear();   //Append
        }

        private void RadioButton_TypeWireless(object sender, RoutedEventArgs e)
        {
            Constants.IsProgrammerWireless = true;      //無線
        }

        private void RadioButton_TypeWire(object sender, RoutedEventArgs e)
        {
            Constants.IsProgrammerWireless = false;     //有線
        }

        //
        private void ProductClose_Click(object sender, RoutedEventArgs e)
        {
            //await _pmg.CloseWirelessConnection(_pmg.GetSelectPort());
            try
            {
                _pmg.CloseProduct();    //内部で選択されている方のproductをCloseDevice()

                DeviceInfoDisplay.Text = "";
                ParamListBox.ItemsSource = null;
                ParamDataGrid.ItemsSource = null;

                MessageBox.Show(_pmg.GetSelectPort().ToString() + "のプロダクトをCloseDevice（）しました");

            }
            catch
            {
                MessageBox.Show(_pmg.GetSelectPort().ToString() + "のプロダクトをCloseDevice（）しました");
            }

        }


        //COMポート切り替え　デフォルトはCOM10
        private void MenuItem_ComPort5(object sender, RoutedEventArgs e)
        {
            Constants.COMPort = "COM5";
        }

        private void MenuItem_ComPort6(object sender, RoutedEventArgs e)
        {
            Constants.COMPort = "COM6";
        }

        private void MenuItem_ComPort7(object sender, RoutedEventArgs e)
        {
            Constants.COMPort = "COM7";
        }

        private void MenuItem_ComPort8(object sender, RoutedEventArgs e)
        {
            Constants.COMPort = "COM8";
        }

        private void MenuItem_ComPort9(object sender, RoutedEventArgs e)
        {
            Constants.COMPort = "COM9";
        }

        private void MenuItem_ComPort10(object sender, RoutedEventArgs e)
        {
            Constants.COMPort = "COM10";

        }

        private void wirelessScan_Checked(object sender, RoutedEventArgs e)
        {
            StaticValues.WirelessScanFlag = true;
        }

        private void wirelessScan_Unchecked(object sender, RoutedEventArgs e)
        {
            StaticValues.WirelessScanFlag = false;
        }



        //表示ウィンドウ
        public Sub1_Window sub1Win;     //共有メモリ表示

        private void MenuTool1_Click(object sender, RoutedEventArgs e)
        {
            if (sub1Win != null) return;

            sub1Win = new Sub1_Window(this);

            sub1Win.Topmost = true;
            sub1Win.Owner = this;
            sub1Win.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            sub1Win.Show();

            //sub1Win.InfoTextBox.Text = "12345";
        }
    }

}
