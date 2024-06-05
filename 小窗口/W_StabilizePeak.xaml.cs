using System;
using System.ComponentModel;
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
using 核素识别仪.其他功能类;

namespace 核素识别仪.小窗口
{
    /// <summary>
    /// W_StabilizePeak.xaml 的交互逻辑
    /// </summary>
    public partial class W_StabilizePeak : Window, INotifyPropertyChanged
    {
        #region 数据

        public MainWindow Father { get; set; }

        /// <summary>
        /// 这个界面可以给3种情况使用，分别为：稳峰、本底计数率测量、预热
        /// </summary>
        public enum Fun
        {
            stabilizePeak,
            benDiCPS,
            warmUp
        }

        /// <summary>
        /// 设置本界面的功能
        /// </summary>
        public Fun P_whichFun { get; set; }

        private string title;
        /// <summary>
        /// 界面标题
        /// </summary>
        public string P_title
        {
            get { return title; }
            set
            {
                title = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_title"));
                }
            }
        }

        private string state;
        /// <summary>
        /// 当前状态，比如"正在稳峰: 2/100"
        /// </summary>
        public string P_state
        {
            get { return state; }
            set
            {
                state = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_state"));
                }
            }
        }

        /// <summary>
        /// 控制此窗口能否关闭（隐藏）。稳峰过程中设置为false，不能关闭；稳峰结束后恢复true
        /// </summary>
        public bool canHide = true;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Window相关

        public W_StabilizePeak()
        {
            InitializeComponent();
        }

        public void Init()
        {
            switch (P_whichFun)
            {
                case Fun.stabilizePeak:
                    P_title = "稳峰";
                    bt_Stop.Content = "停止稳峰";
                    break;
                case Fun.benDiCPS:
                    P_title = "本底测量";
                    bt_Stop.Content = "停止测量";
                    break;
                case Fun.warmUp:
                    P_title = "预热";
                    bt_Stop.Content = "停止预热";
                    break;
                default:
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (canHide)
                this.Hide();
        } 

        #endregion

        /// <summary>
        /// 停止稳峰按钮
        /// </summary>
        private void bt_StopStabilize_Click(object sender, RoutedEventArgs e)
        {
            switch (P_whichFun)
            {
                case Fun.stabilizePeak:
                    MessageBoxResult res = MessageBox.Show("停止稳峰可能道址核素识别结果不准确，是否停止稳峰？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (res == MessageBoxResult.Yes)
                        Father.aStabPeak.P_interrupt = true;
                    break;
                case Fun.benDiCPS:
                    MessageBoxResult res3 = MessageBox.Show("确认停止本底测量？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (res3 == MessageBoxResult.Yes)
                        Father.huoDu.P_interrupt = true;
                    break;
                case Fun.warmUp:
                    MessageBoxResult res2 = MessageBox.Show("确认停止预热？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (res2 == MessageBoxResult.Yes)
                        Father.aStabPeak.P_interrupt = true;
                    break;
                default:
                    break;
            }

        }
    }
}
