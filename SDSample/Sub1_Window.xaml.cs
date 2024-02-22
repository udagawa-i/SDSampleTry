using SDLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SDSample
{
    /// <summary>
    /// Sub1_Window.xaml の相互作用ロジック
    /// </summary>
    public partial class Sub1_Window : Window
    {
        MainWindow m_Win;

        public Sub1_Window( MainWindow main )
        {
            InitializeComponent();
            m_Win = main;


            //情報表示用タイマー 500[msec]周期
            TimerHelper communicationStatusTimer;
            communicationStatusTimer = new TimerHelper(communicationStatusDispExecute);
            communicationStatusTimer.start(500);






        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            m_Win.sub1Win = null;
        }

        private void Test1Button_Click(object sender, RoutedEventArgs e)
        {
            InfoTextBox.Text = "Test";
         
            
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


                    //WirelessScan_id1.Text = StaticValues.WirelessDeviceName1;
                    //WirelessScan_id2.Text = StaticValues.WirelessDeviceName2;



                    //if (_pmg._connectionmanager?.GetConnection(CommunicationPort.kLeft) != null)
                    //{
                    //    AppCommunicationStatusLeft.Text = _pmg._connectionmanager.GetConnection(CommunicationPort.kLeft).Stage.ToString();
                    //}

                    //if (_pmg._connectionmanager?.GetConnection(CommunicationPort.kRight) != null)
                    //{
                    //    AppCommunicationStatusRight.Text = _pmg._connectionmanager.GetConnection(CommunicationPort.kRight).Stage.ToString(); ;
                    //}

                });
            }
            catch
            {

            }
        }









    }
}
