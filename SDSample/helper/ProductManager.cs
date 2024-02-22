using System;
using SDLib; 
using System.IO;
using System.Threading.Tasks; 
using System.Collections.Generic;
using NUnit.Framework;
using System.Globalization;
using SDSample;
using System.Windows;

namespace SoundDesigner.Helper
{
    public class ProductManager 
    { 
        public IProductManager _productmanager;
        public ILibrary _library;

        //public IProduct _product;
        public IProduct _product_left;  //変更
        public IProduct _product_right; //変更
        public CommunicationPort _selectPort;   //追加
        
        public IProductDefinitionList _productDefinitionList;
        public ConnectionManager _connectionmanager;
        uint _version;
        public enum ReadWriteMode
        {
            NVM,
            RAM,
            NVM_ALL,
            CANCELLED
        };
        public bool IsConfigured { get; private set; }
        public bool IsSimulated { get; private set; }
        public bool IsConnected { get; private set;  }


        //
        // コンストラクタ   SDFacadeにアクセスすることで、１回だけ呼び出される！
        //
        public ProductManager()
        {
            _productmanager = SDLibMain.GetProductManagerInstance();
            _version = _productmanager.Version; 
        }


        //
        // コンストラクタ　初期済の場合
        //
        public ProductManager(IProductManager p, ILibrary library, IProduct prod )
        {
            _productmanager = p;
            _version = (uint)(_productmanager?.Version);
            _library = library;

            
            //_product = prod;
            
            _selectPort = CommunicationPort.kLeft;
           
        }


        public void CloseProduct()
        {
            if (IsConnected)
                GetSelectProduct()?.CloseDevice();    //_product
        }

        /// <summary>
        /// Closes the comm adaptor and disconnects the wireless device
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task CloseWirelessConnection(CommunicationPort port)
        {
            await _connectionmanager.Close(port);
        }

        public IDeviceInfo GetConnectedDeviceInfo(CommunicationPort port)
        {   
            var conn = _connectionmanager?.GetConnection(port); 
            return conn?.ConnectedDeviceInfo;
        }


        /////// <summary>
        /////// Loads a library, gets the specified product and sets the current memory
        /////// </summary>
        /////// <param name="library"></param>
        /////// <param name="memory"></param>
        /////// <param name="programmer"></param>
        ////public void Initialize(string library, int memory, string product = null, string programmer = null) 
        ////{
        ////    _connectionmanager = new ConnectionManager(_productmanager, programmer);

        ////    var libpath = Path.Combine(Constants.ProductLibLocation, library ); 
        ////    TestContext.Progress.WriteLine($"Loading library from {Path.GetFullPath(libpath)}\nIf this path is inaccurate set it in Constants.cs");
        ////    var result = _productmanager.LoadLibraryFromFile(libpath);

        ////    // does not check for locked library.
        ////    _library = result;
        ////    _productDefinitionList = _library.Products;
        ////    IProductDefinition pd;
        ////    pd = GetSelectProduct(product);
        ////    _product = pd?.CreateProduct();

        ////    if (_product != null )
        ////        _product.CurrentMemory = (int)(ParameterSpace) memory; 
        ////    else 
        ////        throw new InvalidOperationException("Product not found");
        ////}


        /// <summary>
        /// 
        /// Loads a library, gets the specified product and sets the current memory
        /// 
        /// マキチエ対応　ロック解除　DSP全メモリ読み出し　
        /// 
        /// 
        /// </summary>
        /// <param name="library"></param>
        /// <param name="memory"></param>
        /// <param name="programmer"></param>
        public int Initialize(string library,
                                int memory,
                                CommunicationPort port,
                                string programmer,
                                string product = null)
        {
            try
            {
                //【 DetectDevice 】
                _connectionmanager = new ConnectionManager(_productmanager, programmer);    //プログラマ選択 　クラスnew()のみ

                //_communicationAdaptor　（_productmanagerから取得）
                //SetCommunicationInterface(programmer, port);            //左・右選択 　NVRAM:W/Readチェック,Write中:Mute 

                //_deviceInfo   （_communicationAdaptorから取得）
                string strDevInfo = DetectDevice(port);         //デバイス基本情報取得 ＆ マキチエ用(ロック解除）

                _library = _productmanager.LoadLibraryFromFile(library);          //libraryファイル読み込み

                var productID = GetConnectedDeviceInfo(port).ProductId;   // 検出された物理的に繋がっているDSPに書き込まれているproductID
                _productDefinitionList = _library.Products;


                var productDefinition = _productDefinitionList.GetById(productID);
                var dc = productDefinition.GetDeviceCompatibility(GetCommAdaptor(port));


                //_product = productDefinition?.CreateProduct();    //_product
                IProduct prd = GetSelectProduct();
                prd = productDefinition?.CreateProduct();

                //【 InitializeDevice 】
                if (!CheckInitializeDevice(port))
                {
                    return -1;
                    //InitializeDevice();
                }

                //ReadParameters();   //全メモリ読みだし　PC←DSP

                if (GetSelectProduct() != null)   //_product
                    GetSelectProduct().CurrentMemory = (int)(ParameterSpace)memory;   //_product
                else
                    throw new InvalidOperationException("Product not found");

                return 0;   //OK 
            }
            catch
            {
                throw new InvalidOperationException("Initialize() Error!");
                //throw;
            }

        }


        /// <summary>
        /// Gets a Product definition from the product list matching the specified description. 
        /// </summary>
        /// <param name="desc"></param>
        /// <returns></returns>
        public IProductDefinition GetProduct(string desc = null)
        {
            foreach (IProductDefinition pd in _productDefinitionList)
            {
                if (pd.Description == desc || desc == null)
                {
                    return pd;
                }
            }
            return null;
        }

        /// <summary>
        /// Connect, update and initialize the device asynchronously
        /// </summary>
        /// <param name="port"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task ConnectAsync(CommunicationPort port)
        {
            IsConnected = false;
            var progressHandler = new Progress<int>(value =>
            {
                try { 
                    //TestContext.Progress.Write(value); 
                    Console.WriteLine( value.ToString() +"です");
                }
                catch { }
            });
            var commcheck = await _connectionmanager.ConnectAsync(port, 0, progressHandler);    //変更　0で従来通りの動き
            if (!commcheck)
            {
                throw new InvalidOperationException("Unable to connect to the device");
            }
            commcheck = await UpdateDeviceAsync(port, progressHandler);
            if (!commcheck)
            {
                throw new InvalidOperationException("Incompatible device");
            }
            await InitProductAsync(port, progressHandler);
        }

        public async Task<bool> UpdateDeviceAsync(CommunicationPort port, IProgress<int> progress = null)
        {
            var conn = _connectionmanager.GetConnection(port); 
            DeviceCompatibility upgradeavailable;
            if (!_connectionmanager.StillThere(port))
            {
                throw new InvalidOperationException("Check device failed");
            }
            upgradeavailable = GetSelectProduct().Definition.GetDeviceCompatibility(conn.CommAdaptor);    //_product
            TestContext.Progress.WriteLine($"Firmware version in Library: {GetSelectProduct().Definition.UpdateFirmwareVersion}");    //_product
            TestContext.Progress.WriteLine($"Device compatibility: {upgradeavailable}");
            if (upgradeavailable == DeviceCompatibility.kUnknownCompatibility || upgradeavailable == DeviceCompatibility.kIncompatible)
            {
                MessageBox.Show($"デバイス不適合です (Library:{GetSelectProduct().Definition.UpdateFirmwareVersion}) Device:{upgradeavailable}");  //Append _product
                return false;
            }
            if (upgradeavailable != DeviceCompatibility.kCompatibleUpToDate)
            {

                if ( !Constants.IsProgrammerWireless && 
                     ( (upgradeavailable == DeviceCompatibility.kCompatibleOlder && Constants.UpgradeDevice) ||
                       (upgradeavailable == DeviceCompatibility.kCompatibleNewer && Constants.DowngradeDevice) )
                    )
                {
                    //Append-----------------------------------------------------------------------------------
                    if (upgradeavailable == DeviceCompatibility.kCompatibleOlder)
                    {
                        MessageBox.Show("このデバイスは古いので、更新（up）が必要で実施します。");
                    }

                    if (upgradeavailable == DeviceCompatibility.kCompatibleNewer)
                    {
                        MessageBox.Show("このデバイスは新しいので、更新（down）が必要で実施します。");
                    }
                    //----------------------------------------------------------------------------------------


                    TestContext.Progress.WriteLine("Updating the device");
                    await Task.Run(() => UpdateDeviceBlocking(conn.CommAdaptor, progress));

                    // The line below is used only to get the new Device Info
                    var commcheck = await _connectionmanager.ConnectAsync(port, 0, progress); //変更　第2引数=０で従来通りの動き
                    if (!commcheck)
                    {
                        throw new InvalidOperationException("Unable to connect to the device");
                    }

                    MessageBox.Show("このデバイスの更新が完了しました。");   //Append
                    return true;
                }
                else
                {
#pragma warning disable CS0162
                    if (Constants.IsProgrammerWireless)
                    {

                        TestContext.Progress.WriteLine("Device needs updating but cannot do with a wireless programmer");
                        MessageBox.Show("デバイスの更新が必要だが、ワイヤレスプログラマでは出来ませんでした\n" +
                            " _product.Definition.GetDeviceCompatibility(conn.CommAdaptor)");
                    }
                    else
                    {
                        TestContext.Progress.WriteLine("Device not updated as indicated in Constants.cs");
                    }
                    #pragma warning restore CS0162

                }
                return false; //device is not up to date 
            }
            return true; // device is up to date
        }

        public async Task UpdateDeviceBlocking(ICommunicationAdaptor ca, IProgress<int> progress = null)
        {
            var monitor = GetSelectProduct().Definition.BeginUpdateDevice(ca);    //_product
            var lastpr = -1; 
            while (!monitor.IsFinished)
            {
                // wait 
                await Task.Delay(4000);
                var maxsteps = monitor.ProgressMaximum;
                var pr = monitor.GetProgressValue();
                if (maxsteps > 0 && lastpr != pr )
                {
                    lastpr = pr; 
                    progress?.Report((int)((((double)pr / maxsteps) * ((double)100))));
                }
            }
            monitor.GetResult(); // throw up any errors 
        }

        /// <summary>
        /// Initializes the product asynchronously
        /// </summary>
        /// <param name="port"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task InitProductAsync(CommunicationPort port, IProgress<int> progress = null)
        {
            IsSimulated = false;
            IsConfigured = false;
            TestContext.Progress.WriteLine("Initializing the product");
            var initsuccess = await Task.Run(() => InitializeProductBlocking(port, progress));
            if (initsuccess)
            {
                IsConnected = true;
                //if (IsConfigured == false && !IsSimulated)
                //   throw new InvalidOperationException( "The connected device is not configured with the product.");
            }
        }

        public async Task<bool> InitializeProductBlocking(CommunicationPort port, IProgress<int> progress = null)
        {
            var commadapter = _connectionmanager.GetConnection(port).CommAdaptor; 
            if (commadapter != null)
            {
                try
                {
                    if (!_connectionmanager.StillThere(port))
                    {
                        throw new InvalidOperationException("Check device failed");
                    }
                    
                    var monitor = GetSelectProduct().BeginInitializeDevice(commadapter);      //_product
                    var lastpr = -1;

                    while (!monitor.IsFinished)
                    {
                        // wait 
                        await Task.Delay(300);
                        var maxsteps = monitor.ProgressMaximum;
                        var pr = monitor.GetProgressValue();
                        if (maxsteps > 0 && lastpr != pr)
                        {
                            lastpr = pr; 
                            progress?.Report(
                                    (int)((((double)pr / maxsteps) * ((double)100))));
                        }
                    }
                    monitor.GetResult();
                    IsConfigured = GetSelectProduct().EndInitializeDevice(monitor);           //_product
                }
                catch {
                    IsSimulated = true;
                    throw; 
                }
            }
            else
                IsSimulated = true;
            return true;
        }

        /// <summary>
        /// Async Read operation 
        /// </summary>
        /// <param name="param"></param>
        public async Task ReadParametersAsync(CommunicationPort port, int memory, ReadWriteMode mode, IProgress<int> progress = null)
        {
            if (!_connectionmanager.StillThere(port))
            {
                throw new InvalidOperationException("Check device failed");
            }
            var steplist = new List<ParameterSpace>();
            switch (mode)
            {
                case ReadWriteMode.NVM:
                    // Restore Current Memory from NVM"
                    steplist.Add((ParameterSpace) memory);
                    steplist.Add(ParameterSpace.kSystemNvmMemory);
                    break;
                case ReadWriteMode.RAM:
                    // Read RAM
                    steplist.Add(ParameterSpace.kActiveMemory);
                    steplist.Add(ParameterSpace.kSystemActiveMemory);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await Task.Run(() => ReadParametersBlocking(steplist, progress)); 

        }


        /// <summary>
        /// Async Write operation 
        /// </summary>
        /// <param name="param"></param>
        public async Task WriteParametersAsync(CommunicationPort port, int memory, ReadWriteMode mode, IProgress<int> progress = null)
        {
            if (!_connectionmanager.StillThere(port))
            {
                throw new InvalidOperationException("Check device failed");
            }
            var steplist = new List<ParameterSpace>();
            switch (mode)
            {
                case ReadWriteMode.NVM:
                    // Restore Current Memory from NVM"
                    steplist.Add((ParameterSpace)memory);
                    steplist.Add(ParameterSpace.kSystemNvmMemory);
                    break;
                case ReadWriteMode.RAM:
                    // Read RAM
                    steplist.Add(ParameterSpace.kActiveMemory);
                    steplist.Add(ParameterSpace.kSystemActiveMemory);
                    break;
                case ReadWriteMode.NVM_ALL:
                    // Restore Current Memory from NVM"
                    steplist.Add(ParameterSpace.kNvmMemory0);
                    steplist.Add(ParameterSpace.kNvmMemory1);
                    steplist.Add(ParameterSpace.kNvmMemory2);
                    steplist.Add(ParameterSpace.kNvmMemory3);
                    steplist.Add(ParameterSpace.kNvmMemory4);
                    steplist.Add(ParameterSpace.kNvmMemory5);
                    steplist.Add(ParameterSpace.kNvmMemory6);
                    steplist.Add(ParameterSpace.kNvmMemory7);
                    steplist.Add(ParameterSpace.kSystemNvmMemory);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await Task.Run(() => WriteParametersBlocking(steplist, progress));
        }


        /// <summary>
        /// Read parameters
        /// </summary>
        /// <param name="steplist"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task ReadParametersBlocking(List<ParameterSpace> steplist, IProgress<int> progress = null)
        {
            for (int i = 0; i < steplist.Count; i++)
            {
                var step = steplist[i];
                var monitor = GetSelectProduct().BeginReadParameters(step);   //_product
                int lastreportedpr = 0;

                while (!monitor.IsFinished)
                {
                    // wait 
                    await Task.Delay(50);
                    var maxsteps = monitor.ProgressMaximum;
                    var pr = monitor.GetProgressValue();
                    if (maxsteps > 0)
                    {
                        if (lastreportedpr != pr)
                        {
                            var diff = pr - lastreportedpr;
                            lastreportedpr = pr;
                            progress?.Report(
                            (int)((((double)pr / maxsteps) * ((double)100 / steplist.Count)) + ((double)i * 100 / steplist.Count)));
                        }
                    }
                }
                //monitor.GetResult(); // will throw error if needed
      
            }
        }

        /// <summary>
        /// Begin write params started flag for test purposes
        /// </summary>
        public bool bwpstarted = false;
        /// <summary>
        /// Read parameters
        /// </summary>
        /// <param name="steplist"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task WriteParametersBlocking(List<ParameterSpace> steplist, IProgress<int> progress = null)
        {
            for (int i = 0; i < steplist.Count; i++)
            {
                var step = steplist[i];
                var monitor = GetSelectProduct().BeginWriteParameters(step);      //_product
                bwpstarted = true;
                int lastreportedpr = 0;

                while (!monitor.IsFinished)
                {
                    // wait 
                    await Task.Delay(50);
                    var maxsteps = monitor.ProgressMaximum;
                    var pr = monitor.GetProgressValue();
                    if (maxsteps > 0)
                    {
                        if (lastreportedpr != pr)
                        {
                            var diff = pr - lastreportedpr;
                            lastreportedpr = pr;
                            progress?.Report(
                            (int)((((double)pr / maxsteps) * ((double)100 / steplist.Count)) + ((double)i * 100 / steplist.Count)));
                        }
                    }
                }
                bwpstarted = false; 
                //monitor.GetResult(); // will throw error if needed 
            }
        }

        public double[] GetGraphData (GraphId graphId, double[] xdata, int numberofpoints, int inputlevel ) 
        { 
            var ydata =  new double[numberofpoints];
            foreach (IGraphDefinition id in GetSelectProduct().Graphs)    //_product
            {
                if (id.Id == graphId)
                {
                    var Graph = id.CreateGraph();
                    Graph.GraphSettings.GetById("InputLevel").DoubleValue = inputlevel;
                    Graph.SetDomain(numberofpoints, xdata);
                    Graph.CalculatePoints(numberofpoints, ydata);       
                    return ydata; 
                }
            }
            return ydata; // array of zeros 
        }

        public IParameter FindParameter(string name, int memory = -1)
        {
            if ( memory == -1 ) 
                memory = (int)GetSelectProduct().CurrentMemory;      //product

            var mems = GetMemory(memory);
            return mems.Parameters.GetById(name);
        }
        
        public IParameter FindSystemParameter(string name)
        {
            var mems = GetSelectProduct().SystemMemory;       //_product
            return mems.Parameters.GetById(name);
        }

        public IParameterMemory GetMemory (int mem)
        {
            var listEnumerator = GetSelectProduct().Memories.GetEnumerator();     //_product
            for (var i = 0; listEnumerator.MoveNext() == true; i++)
            {
                if (i==mem)
                 return listEnumerator.Current as IParameterMemory; 
            }
            return null;
        }

        public void SetValueFromFile(IParameter p, string s)
        {
            try
            {
                switch (p.Type)
                {
                    case ParameterType.kBoolean:
                        p.BooleanValue = Convert.ToBoolean(s);
                        break;
                    case ParameterType.kByte:
                    case ParameterType.kInteger:
                    case ParameterType.kIndexedList:
                    case ParameterType.kIndexedTextList:
                        p.Value = Convert.ToInt32(s);
                        break;
                    case ParameterType.kDouble:
                        p.DoubleValue =  Math.Round(Convert.ToDouble(s, CultureInfo.InvariantCulture), 4);
                        break;
                    case ParameterType.kUnknownType:
                        break;
                }
            }
            catch
            {
                throw new InvalidOperationException("Unexpected Parameter file value");

            }
        }

        //---------------------------------------------------------------------------------------------
        //  追加　
        //---------------------------------------------------------------------------------------------
        public double GetBatteryAverageVoltagey()
        {
         
            double d = GetSelectProduct().SampleRate;     //_product
            

            return d;
        }


        /// <summary>
        /// Makichie デイバイスを検出する。
        /// DSPにパスワードでロックがかかっている場合は解除もする。
        /// </summary>
        public string DetectDevice(CommunicationPort port)
        {

            // 通信オブジェクト
            try
            {
                //SetCommunicationInterface("Communication Accelerator Adaptor", CommunicationPort.kLeft);
                var con = _connectionmanager?.GetConnection(port);
                
                con.ConnectedDeviceInfo = con?.CommAdaptor.DetectDevice(); // デバイスの基本情報を読み込む

                if (con.ConnectedDeviceInfo.ParameterLockState)
                {
                    // Parameter Lock されている場合はunlockのためにキーを設定して再度Detectしなおす必要ある
                    List<List<int>> unlockParameterList = new List<List<int>>
                    {
                        new List<int>() { 0x1DFACE , 0xB0 , 0, 0} , // パスワード: 0xB01DFACE
                        new List<int>() { 0xADBE9, 0 , 0, 0} ,      // パスワード: 0xADBE9                    
                    };

                    foreach (var unlockParameter in unlockParameterList)
                    {
                        con?.CommAdaptor.UnlockParameterAccess(
                            unlockParameter[0],
                            unlockParameter[1],
                            unlockParameter[2],
                            unlockParameter[3]);

                        con.ConnectedDeviceInfo = con?.CommAdaptor.DetectDevice();

                        if (!con.ConnectedDeviceInfo.ParameterLockState)
                        {
                            break;
                        }
                    }

                    if (con.ConnectedDeviceInfo.ParameterLockState)
                    {
                        throw new Exception("DSPのLockが解除できませんでした。");
                    }
                }
            }
            catch
            {
                throw new Exception("DetectDevice() Error");
            }

            IDeviceInfo _deviceInfo = GetConnectedDeviceInfo(port);
          
            // 検出されたDeviceInfoの各プロパティーを表示
            var di =
                "FirmwareVersion: " + _deviceInfo.FirmwareVersion.ToString() + Environment.NewLine +
                "FirmwareId: " + _deviceInfo.FirmwareId.ToString() + Environment.NewLine +
                "IsValid: " + _deviceInfo.IsValid + Environment.NewLine +
                "ParameterLockState: " + _deviceInfo.ParameterLockState.ToString() + Environment.NewLine +
                "ChipVersion: " + _deviceInfo.ChipVersion.ToString() + Environment.NewLine +
                "ChipId: " + _deviceInfo.ChipId.ToString() + Environment.NewLine +
                "HybridId: " + _deviceInfo.HybridId.ToString() + Environment.NewLine +
                "HybridRevision: " + _deviceInfo.HybridRevision.ToString() + Environment.NewLine +
                "HybridSerial: " + _deviceInfo.HybridSerial.ToString() + Environment.NewLine +
                "LibraryId: " + _deviceInfo.LibraryId.ToString("x8") + Environment.NewLine +
                "ProductId: " + _deviceInfo.ProductId.ToString() + Environment.NewLine +
                "SerialId: " + _deviceInfo.SerialId.ToString() + Environment.NewLine +
                "RadioApplicationVersion: " + _deviceInfo.RadioApplicationVersion.ToString() + Environment.NewLine +
                "RadioBootloaderVersion: " + _deviceInfo.RadioBootloaderVersion.ToString() + Environment.NewLine +
                "RadioSoftDeviceVersion: " + _deviceInfo.RadioSoftDeviceVersion.ToString() + Environment.NewLine;

            //DeviceInfoDisplay.Text = di;

            return di;

        }



        /// <summary>
        /// デバイスの初期化　要否をチェック
        /// </summary>
        /// <returns></returns>
        public bool CheckInitializeDevice(CommunicationPort port)
        {
            try
            {
                var isInitialized = GetSelectProduct().InitializeDevice(GetCommAdaptor(port));    //_product
                return isInitialized;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// デバイスの初期化を実施し、通信可能状態に移行する。
        /// </summary>
        public void InitializeDevice()
        {
            try
            {
                GetSelectProduct().ConfigureDevice();     //_product

                // 補聴器のハウリングがうるさい場合があるので通信確立直後に一旦自動的にミュートにしておく          
                GetSelectProduct().MuteDevice(true);          //_product
            }
            catch
            {
                throw;
            }
        }


        ICommunicationAdaptor GetCommAdaptor(CommunicationPort port) 
        {
            return (_connectionmanager?.GetConnection(port).CommAdaptor);
        }

      

        //8-592
        public IParameterMemoryList GetMemoryList()
        {
            return GetSelectProduct().Memories;       //_product

        }


        public IParameterList GetSystemMemory()
        {
            return GetSelectProduct().SystemMemory.Parameters;    //_product
        }



        public int GetCommunicationInterfaceCount()
        {
            return _productmanager.GetCommunicationInterfaceCount();
        }

        public string GetCommunicationInterfaceString(int index)
        {
            return _productmanager.GetCommunicationInterfaceString(index);
        }



        public void SetCommunicationInterface(string communicationInterfaceName, CommunicationPort port)
        {
            //////ICommunicationAdaptor _communicationAdaptor;
            //ICommunicationAdaptor comAdp = GetCommAdaptor(port);

            //ICommunicationAdaptor comAdp = _productmanager.CreateCommunicationInterface(communicationInterfaceName, port, "");
            _connectionmanager = new ConnectionManager(_productmanager, communicationInterfaceName);
            _connectionmanager.GetConnection(port).CommAdaptor = 
                _productmanager.CreateCommunicationInterface(communicationInterfaceName, port, "");


            ////if (port == CommunicationPort.kLeft)
            ////{    
            ////    if (this._communicationAdaptor_left == null)
            ////    {
            ////        _communicationAdaptor_left = _productmanager.CreateCommunicationInterface(communicationInterfaceName, port, "");
            ////        _communicationAdaptor_left.VerifyNvmWrites = true; // NVMに書き込んだときにReadしなおして書き込み内容をチェックする機能を有効
            ////        _communicationAdaptor_left.MuteDuringCommunication = true; // Read/Write中に補聴器をミュートする設定
            ////    }
            ////    _communicationAdaptor = this._communicationAdaptor_left;
            ////}
            ////else
            ////{
            ////    if (this._communicationAdaptor_right == null)
            ////    {
            ////        _communicationAdaptor_right = _productmanager.CreateCommunicationInterface(communicationInterfaceName, port, "");
            ////        _communicationAdaptor_right.VerifyNvmWrites = true; // NVMに書き込んだときにReadしなおして書き込み内容をチェックする機能を有効
            ////        _communicationAdaptor_right.MuteDuringCommunication = true; // Read/Write中に補聴器をミュートする設定
            ////    }
            ////    _communicationAdaptor = this._communicationAdaptor_right;
            ////}

            //////_communicationAdaptor = _productmanager.CreateCommunicationInterface(communicationInterfaceName, port, "");
            //////_communicationAdaptor.VerifyNvmWrites = true; // NVMに書き込んだときにReadしなおして書き込み内容をチェックする機能を有効
            //////_communicationAdaptor.MuteDuringCommunication = true; // Read/Write中に補聴器をミュートする設定
        }


        //Append
        public void SetSelectPort(CommunicationPort port)
        {
            _selectPort = port;
        }

        public CommunicationPort GetSelectPort()
        {
            return _selectPort;
        }

       public void SetProduct( IProduct product)
        {
            if( GetSelectPort() == CommunicationPort.kLeft) 
            {
                _product_left = product;
            }
            else
            {
                _product_right = product;
            }

       }


        public IProduct GetSelectProduct()
        {
            return GetSelectPort() == CommunicationPort.kLeft ? _product_left : _product_right; 
        }



    }

}