using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SDSample
{
    /// <summary>
    /// タイマー　Helperクラス
    /// </summary>
    public class TimerHelper
    {
        private Timer _timer;

        public TimerHelper(TimerCallback callback)
        {
            _timer = new Timer(callback);

        }

        public void start(int msec)
        {
            _timer.Change(0, msec);
        }

        public void stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }
}

////
//// タイマー　Helperクラス
////
//public class TimerHelper
//{
//    private Timer _timer;

//    public TimerHelper(TimerCallback callback)
//    {
//        _timer = new Timer(callback);

//    }

//    public void start(int msec)
//    {
//        _timer.Change(0, msec);
//    }

//    public  void stop()
//    {
//        _timer.Change(Timeout.Infinite, Timeout.Infinite);
//    }

//}