using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 核素识别仪.集成的数据类
{
    public class AutoRun : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isReading = false;
        /// <summary>
        /// 标志是否正在读取，打开线程后自动赋值为true，线程结束后自动赋值为false
        /// </summary>
        public bool P_isReading
        {
            get { return isReading; }
            set
            {
                isReading = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_isReading"));
                }
            }
        }

        private bool isCleared = true;
        /// <summary>
        /// 标志是否清空了数据。在清空时赋true，在开始采集后，赋false。
        /// 用来判断：开始采集是继续还是重新的一次
        /// </summary>
        public bool P_isCleared
        {
            get { return isCleared; }
            set
            {
                isCleared = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_isCleared"));
                }
            }
        }

        private bool isThreadOn = false;
        /// <summary>
        /// 表征自动读取数据的线程是否打开。
        /// 在打开线程时，主动赋为true，保证线程正常开始；它是线程的循环条件；从外界主动赋为false，用来停止线程。
        /// 目前暂时没啥用了，现在停止定时器线程需要Stop定时器。
        /// </summary>
        public bool P_isThreadOn
        {
            get { return isThreadOn; }
            set { isThreadOn = value; }
        }

        private bool isDataNew = false;
        /// <summary>
        /// 表征多道数据是否更新。每次解析了新数据，就赋值为true，每次画完图，就赋值为false
        /// </summary>
        public bool P_isDataNew
        {
            get { return isDataNew; }
            set { isDataNew = value; }
        }

        private bool isDrawingOn = true;
        /// <summary>
        /// 定义一个是否画图的开关
        /// </summary>
        public bool P_isDrawingOn
        {
            get { return isDrawingOn; }
            set { isDrawingOn = value; }
        }

        private DateTime startTime = DateTime.MinValue;
        /// <summary>
        /// 开始测量的时间
        /// </summary>
        public DateTime P_startTime
        {
            get
            {
                return startTime;
            }
            set
            {
                startTime = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_startTime"));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_startTimeStr"));
                }
            }
        }

        public string P_startTimeStr
        {
            get
            {
                if (startTime.Equals(DateTime.MinValue))
                    return string.Empty;
                else
                    return startTime.ToString("G");
            }
        }



    }
}
