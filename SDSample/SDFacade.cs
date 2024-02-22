using SDLib;
using SoundDesigner.Helper;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;


namespace SDSample
{
    public static class SDFacade
    {
        //============================================================================================
        // ProductManager クラス
        //============================================================================================

        //初期化も含めSDFacadeを使用する場合
        //public static ProductManager _pmg = new ProductManager();     //SDLibMain.GetProductManagerInstance();

        //既に初期化されておりSDFacadeを利用する場合
        public static ProductManager _pm;


        //既に初期化されておりSDFacadeを利用する場合の、SDFacadeクラスの初期化
        public static void SDFacadeInitial(IProductManager prodManager, ILibrary library, IProduct product, CommunicationPort port = CommunicationPort.kLeft)
        {
            _pm = new ProductManager(prodManager, library, product);
        }

        public static void SDFacadeInitial(ProductManager pmg)
        {
            _pm = pmg;
        }

        //public static bool PmLoadProductManager( CommunicationPort port )
        //{
        //    if( _pmg == null ) return false;

        //    return _pmg.LoadProductManager(port);

        //}

        public enum comState
        {
            Disconnected,
            Detected,
            Initialized,
        }


        public static void PmCloseProduct()
        {
            _pm.CloseProduct();
        }


        public static async Task PmCloseWirelessConnection(CommunicationPort port)
        {
            await _pm.CloseWirelessConnection(port);
        }

        public static IDeviceInfo PmGetConnectedDeviceInfo(CommunicationPort port)
        {
            return _pm.GetConnectedDeviceInfo(port);

        }

        /// <summary>
        /// DSP初期化　マキチエDSP対応（ロック解除）＆　全メモリ読み出し
        /// </summary>
        /// <param name="library">ライブラリ名</param>
        /// <param name="memory">メモリー番号</param>
        /// <param name="product">プロダクト名</param>
        /// <param name="programmer">プログラムIF</param>
        public static int PmInitialize(string library, int memory, CommunicationPort port, string programmer, string product = null)
        {
            try
            {
                int iRet = _pm.Initialize(library, memory, port, programmer, product: product);
                return iRet;
            }
            catch
            {
                MessageBox.Show("PmInitialize() Error");
                return -1;
            }
        }





        public static IProductDefinition PmGetProduct(string desc = null)
        {
            return _pm.GetProduct(desc);
        }

        public static async Task PmConnectAsync(CommunicationPort port)
        {
            await _pm.ConnectAsync(port);
        }


        ///
        //private static async Task<bool> UpdateDeviceAsync(CommunicationPort port, IProgress<int> progress = null)
        //{
        //    return true;
        //}

        //private static async Task UpdateDeviceBlocking(ICommunicationAdaptor ca, IProgress<int> progress = null)
        //{

        //}

        /// <summary>
        /// Initializes the product asynchronously
        /// </summary>
        /// <param name="port"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        //private async Task InitProductAsync(CommunicationPort port, IProgress<int> progress = null)
        //{

        //}

        //private static async Task<bool> InitializeProductBlocking(CommunicationPort port, IProgress<int> progress = null)
        //{

        //}

        public static async Task PmReadParametersAsync(CommunicationPort port, int memory, ProductManager.ReadWriteMode mode, IProgress<int> progress = null)
        {

            await _pm.ReadParametersAsync(port, memory, mode, progress);
        }


        /// <summary>
        /// Async Write operation 
        /// </summary>
        /// <param name="param"></param>
        public static async Task PmWriteParametersAsync(CommunicationPort port, int memory, ProductManager.ReadWriteMode mode, IProgress<int> progress = null)
        {
            await _pm.WriteParametersAsync(port, memory, mode, progress);
        }




        /// <summary>
        /// Read parameters
        /// </summary>
        /// <param name="steplist"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        //private static async Task ReadParametersBlocking(List<ParameterSpace> steplist, IProgress<int> progress = null)
        //{

        //}


        /// <summary>
        /// Read parameters
        /// </summary>
        /// <param name="steplist"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        //private static async Task WriteParametersBlocking(List<ParameterSpace> steplist, IProgress<int> progress = null)
        //{

        //}

        public static double[] PmGetGraphData(GraphId graphId, double[] xdata, int numberofpoints, int inputlevel)
        {
            return _pm.GetGraphData(graphId, xdata, numberofpoints, inputlevel);
        }

        public static IParameter PmFindParameter(string name, int memory = -1)
        {
            return _pm.FindParameter(name, memory);
        }

        public static IParameter PmFindSystemParameter(string name)
        {

            return _pm.FindSystemParameter(name);
        }

        public static IParameterMemory PmGetMemory(int mem)
        {

            return _pm.GetMemory(mem);
        }


        public static IParameterMemoryList PmGetMemoryList()
        {
            return _pm.GetMemoryList();
        }

        public static IParameterList PmGetSystemMemory()
        {
            return _pm.GetSystemMemory();
        }



        public static void PmSetValueFromFile(IParameter p, string s)
        {
            _pm.SetValueFromFile(p, s);
        }


        //-----------------------------------------------------------------------
        // 追加
        //-----------------------------------------------------------------------
        public static int PmGetCommunicationInterfaceCount()
        {
            return _pm.GetCommunicationInterfaceCount();
        }

        public static string PmGetCommunicationInterfaceString(int index)
        {
            return _pm.GetCommunicationInterfaceString(index);
        }

        /// <summary>
        /// アクティブメモリをAに設定
        /// DSP内にある全てのパラメータをPCへ読み込み
        /// </summary>
        public static void PmReadParameters()
        {
            //    _pmg.ReadParameters();
        }


        //
        //
        //
        public static void PmSetCommunicationInterface(string comName, CommunicationPort port)
        {
            _pm.SetCommunicationInterface(comName, port);
        }


        /********************************************************

        /// <summary>
        /// チャープツール本体
        /// </summary>
        private static async Task ChirpToolExecute()
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
                var chirp_result = product.BeginRunChirpTool();

                while (!chirp_result.IsFinished)
                {
                    var pr = chirp_result.GetProgressValue();
                    ChirpProgressBar.Value = pr;
                    await Task.Delay(50);
                }

                ChirpProgressBar.Value = 100;

                var chirp_impulse = product.ChirpToolImpulseResponse;
                var chirp_Frequency = product.ChirpToolFrequencyResponse;

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

        ********************************************************************************/


        /// <summary>
        /// 　PCのメモリに格納されているパラメータの名前を解析し結果を返却
        /// </summary>
        /// <param name="paramNameSystem">Systemパラメータの名前リスト</param>
        /// <param name="paramName">A～Hパラメータの名前リスト</param>
        /// <param name="paramNameSystemIndex">配列要素を持つSystemパラメータの添え字最大値</param>
        /// <param name="paramNameIndex">配列要素を持つA～Hパラメータの添え字最大値</param>
        public static void PmParameterNameAnalysis(
                out List<string> paramNameSystem,                   // 
                out List<string> paramName,                         //
                out Dictionary<string, int> paramNameSystemIndex,   //
                out Dictionary<string, int> paramNameIndex          //                              
                )
        {
            //------------------------------------------------------------
            //      MEM A～H 
            //------------------------------------------------------------
            //ハッシュセット生成
            HashSet<string> uniqueList = new HashSet<string>();

            //配列要素を持つパラメータ
            paramNameIndex = new Dictionary<string, int>();

            foreach (IParameterMemory mem in PmGetMemoryList())      //8回
            {
                foreach (IParameter pm in mem.Parameters)                     //592回
                {
                    string currentParam = pm.Id;  //取得したパラメータ文字列

                    // 正規表現を使用して"[数字]"の部分を除去
                    string strippedString = Regex.Replace(currentParam, @"\[\d+\]", "");

                    // "[数字]"がある場合、数字を取り出し、最大値を更新
                    Match match = Regex.Match(currentParam, @"\[(\d+)\]");
                    if (match.Success)
                    {
                        int number = int.Parse(match.Groups[1].Value);
                        if (paramNameIndex.ContainsKey(strippedString))
                        {
                            int currentMax = paramNameIndex[strippedString];
                            paramNameIndex[strippedString] = Math.Max(currentMax, number);
                        }
                        else
                        {
                            paramNameIndex.Add(strippedString, number);
                        }

                        strippedString += "*";  //[n]の場合は＊を追加
                    }

                    // パラメータ文字列を uniqueList に追加
                    uniqueList.Add(strippedString);

                }
            }

            //重複を排除した項目を名前順でソート　➞ List<string> list に格納
            paramName = new List<string>(uniqueList);
            paramName.Sort();


            //------------------------------------------------------------
            //      SYSTEM 
            //------------------------------------------------------------
            //ハッシュセット生成
            HashSet<string> uniqueSystemList = new HashSet<string>();
            //配列要素を持つパラメータ
            paramNameSystemIndex = new Dictionary<string, int>();

            foreach (IParameter pm in PmGetSystemMemory())
            {

                string currentParam = pm.Id;  //取得したパラメータ文字列

                // 正規表現を使用して"[数字]"の部分を除去
                string strippedString = Regex.Replace(currentParam, @"\[\d+\]", "");

                // "[数字]"がある場合、数字を取り出し、最大値を更新
                Match match = Regex.Match(currentParam, @"\[(\d+)\]");
                if (match.Success)
                {
                    int number = int.Parse(match.Groups[1].Value);
                    if (paramNameSystemIndex.ContainsKey(strippedString))
                    {
                        int currentMax = paramNameSystemIndex[strippedString];
                        paramNameSystemIndex[strippedString] = Math.Max(currentMax, number);
                    }
                    else
                    {
                        paramNameSystemIndex.Add(strippedString, number);
                    }

                    strippedString += "*";  //[n]の場合は＊を追加
                }

                // パラメータ文字列を uniqueList に追加
                uniqueSystemList.Add(strippedString);

            }

            //重複を排除した項目を名前順でソート　➞ List<string> list に格納
            paramNameSystem = new List<string>(uniqueSystemList);
            paramNameSystem.Sort();
        }

        /// <summary>
        /// カレント/アクティブメモリを変更する
        /// </summary>
        public static void PmSetCurrntMemory(int no)
        {
            if ((no < 0) || (no > 8)) return;

            if (_pm.GetSelectProduct() != null)
            {
                _pm.GetSelectProduct().CurrentMemory = (int)(ParameterSpace)no;            //カレントメモリを変更
                _pm.GetSelectProduct().WriteParameters(ParameterSpace.kActiveMemory); //アクティブメモリへ書き込み
            }

        }

        /// <summary>
        /// E7160SLのSystemParameter をWriteする前に、
        /// 変更してはいけない重要パラメーターをチェックする
        /// 
        /// X_RF_Enable=1 (Enable)であること！
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool PmCheckE7160WriteSystemParameter(IDeviceInfo deviceInfo)
        {
            //deviceInfo = communicationAdaptor.DetectDevice();       //DSP 情報取得 

            if (deviceInfo.FirmwareId.Contains("E7111"))
            {
                //有線接続なので関係ない
                return true;
            }
            else if (deviceInfo.FirmwareId.Contains("E7160"))
            {
                //無線接続
                var param = SDFacade.PmFindSystemParameter("X_RF_Enable");
                string strParam = SDFacade.PmGetStringValue(param);         //0:Disable 1:Enable
                if (strParam == "1")
                {
                    return true;
                }
            }
            return false;
        }



        //松崎さん　コード
        public static string PmDetectDevice2()
        {
            //string strInfo = _pmg.DetectDevice2();
            //return strInfo;
            return null;
        }

        //松崎さん　コード
        public static void PmInitializeDevice2()
        {
            //_pmg.InitializeDevice2();
        }



        /// <summary>
        /// IParameter param より値を文字列で取得する
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        /// 
        public static string PmGetStringValue(IParameter param)
        {
            string strRetValue = "";

            switch (param.Type)
            {
                case ParameterType.kBoolean:
                    strRetValue = param.BooleanValue.ToString();
                    break;

                case ParameterType.kDouble:
                    strRetValue = param.DoubleValue.ToString();
                    break;

                case ParameterType.kByte:
                case ParameterType.kInteger:
                    strRetValue = param.Value.ToString();
                    break;

                case ParameterType.kIndexedList:
                    strRetValue = param.Value.ToString();
                    break;

                case ParameterType.kIndexedTextList:
                    strRetValue = param.Value.ToString();
                    break;

                default:
                    strRetValue = "未知データ";
                    break;
            }

            return strRetValue;
        }


        /// <summary>
        /// IParameter param より値を文字列で取得する
        /// </summary>
        /// <param name="param">IParameter param</param>
        /// <param name="strValue">書込データ文字列</param>
        /// <returns></returns>
        public static bool PmSetStringValue(IParameter param, string strValue)
        {
            try
            {
                switch (param.Type)
                {
                    case ParameterType.kBoolean:
                        param.BooleanValue = Convert.ToBoolean(strValue);
                        break;

                    case ParameterType.kDouble:
                        param.DoubleValue = Convert.ToDouble(strValue);
                        break;

                    case ParameterType.kByte:
                    case ParameterType.kInteger:
                        param.Value = Convert.ToInt32(strValue);
                        break;

                    case ParameterType.kIndexedList:
                        param.Value = Convert.ToInt32(strValue);    //?
                        break;

                    case ParameterType.kIndexedTextList:
                        param.Value = Convert.ToInt32(strValue);    //?
                        break;

                    default:
                        //"未知データ";
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("エラー:" + ex.Message);
                return false;
            }
        }



        public static string PmGetWirelessDeviceName()
        {
            //----------------------------------------
            // ワイヤレス デバイス名の取得
            //----------------------------------------
            string resultString = "";   //デバイス名

            for (int i = 0; i < 8; i++)
            {
                var parName = PmFindSystemParameter("X_RF_DeviceName" + i.ToString());
                string strName = PmGetStringValue(parName);

                try
                {
                    int number = int.Parse(strName);

                    // 数値を16進数文字列に変換
                    string hexString = number.ToString("X");


                    // 16進数文字列を1バイトずつに分割して、その後文字に変換

                    for (int j = 0; j < hexString.Length; j += 2)
                    {
                        string byteString = hexString.Substring(j, 2);
                        byte byteValue = Convert.ToByte(byteString, 16);
                        char charValue = (char)byteValue;

                        if (charValue == 0)
                        {
                            return resultString;
                        }

                        resultString += charValue;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(" ワイヤレス デバイス名が 異常です");
                    //Console.WriteLine("数値に変換できませんでした。");
                    break;
                }

            }

            return resultString;
        }

    }

}