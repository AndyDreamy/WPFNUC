using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 核素识别仪.小窗口;
using 核素识别仪.Models;
using System.Threading;
using System.Runtime.Remoting.Channels;

namespace 核素识别仪.其他功能类
{
    /// <summary>
    /// 多道数据稳峰相关
    /// </summary>
    public class AndyStabilizePeak : INotifyPropertyChanged
    {
        #region 属性

        /// <summary>
        /// 主界面
        /// </summary>
        public MainWindow Father { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private double factor_StabPeak = 1;
        /// <summary>
        /// 稳峰因子
        /// </summary>
        public double P_factor_StabPeak
        {
            get { return factor_StabPeak; }
            set
            {
                factor_StabPeak = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_factor_StabPeak"));
                }
            }
        }

        private double KChannel_Scale = 1250;
        /// <summary>
        /// 钾40刻度时的标准道址
        /// </summary>
        public double P_KChannel_Scale
        {
            get { return KChannel_Scale; }
            set
            {
                KChannel_Scale = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_KChannel_Scale"));
                }
            }
        }

        private double KChannel_Real = 1250;
        /// <summary>
        /// 钾40稳峰时测得的实际道址（若没有测得，则从文件读取）
        /// </summary>
        public double P_KChannel_Real
        {
            get { return KChannel_Real; }
            set
            {
                KChannel_Real = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_KChannel_Real"));
                }
            }
        }

        private double KJudgeMax = 1400;
        /// <summary>
        /// 钾40稳峰结果判断上限
        /// </summary>
        public double P_KJudgeMax
        {
            get { return KJudgeMax; }
            set
            {
                KJudgeMax = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_KJudgeMax"));
                }
            }
        }

        private double KJudgeMin = 1100;
        /// <summary>
        /// 钾40稳峰结果判断下限
        /// </summary>
        public double P_KJudgeMin
        {
            get { return KJudgeMin; }
            set
            {
                KJudgeMin = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_KJudgeMin"));
                }
            }
        }

        private double time_StabPeak = 200;
        /// <summary>
        /// 稳峰时间（s）
        /// </summary>
        public double P_time_StabPeak
        {
            get { return time_StabPeak; }
            set
            {
                time_StabPeak = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_time_StabPeak"));
                }
            }
        }

        private double time_WarmUp = 200;
        /// <summary>
        /// 预热时间（s）
        /// </summary>
        public double P_time_WarmUp
        {
            get { return time_WarmUp; }
            set
            {
                time_WarmUp = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_time_WarmUp"));
                }
            }
        }

        private bool interrupt = false;
        /// <summary>
        /// 中断稳峰标志位，若设置为true，则会中断当前的稳峰循环
        /// </summary>
        public bool P_interrupt
        {
            get { return interrupt; }
            set { interrupt = value; }
        }

        private string currentState = "正在稳峰:";
        /// <summary>
        /// 当前稳峰状态，用于在UI上提示
        /// </summary>
        public string P_currentState
        {
            get { return currentState; }
            set
            {
                currentState = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_currentState"));
                }
            }
        }

        /// <summary>
        /// 稳峰组列表
        /// </summary>
        private List<StaPeakInfoModel> listStaPeakGroup = new List<StaPeakInfoModel>();

        #endregion

        #region 方法

        /// <summary>
        /// 开启稳峰的方法。稳峰的时候正常采集即可，只是需要关闭画图功能
        /// </summary>
        public void StartStabilizePeak()
        {
            //判断串口
            if (!Father.andySP._serialPort.IsOpen)
            {
                MessageBox.Show("请先打开串口", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            //判断是否已经开始一个测量
            if (Father.autoRun.P_isReading)
            {
                MessageBox.Show("已开启了一次稳峰", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            W_StabilizePeak w_temp = Father.w_StabilizePeak;

            //关闭画图
            Father.autoRun.P_isDrawingOn = false;
            //隐藏Chart
            //GrandFather.wfhost_Chart.Visibility = System.Windows.Visibility.Hidden;
            //打开小窗口
            Father.adc.OpenOneWindow(w_temp);
            //UI提示
            w_temp.P_state = "正在稳峰:";
            //不让稳峰小界面关闭
            w_temp.canHide = false;

            //开始前清空稳峰组列表
            listStaPeakGroup.Clear();

            //开始采集
            Father.开始测量_Click(this, null);

            //连续采集一段时间——稳峰时间
            w_temp.progress_StabPeak.Maximum = time_StabPeak;
            for (int i = 0; i < time_StabPeak && !interrupt; i++)//这里，如果interrupt被设置为true，则会中断稳峰
            {
                //进度条值更新
                w_temp.progress_StabPeak.Value = i + 1;

                //取一下识别峰位结果
                foreach (int peakChannel in MainWindow.Instance.reco.P_peakIndexes)
                {
                    if (peakChannel >= KJudgeMin && peakChannel <= KJudgeMax)
                    {
                        int groupChannel = peakChannel / 10 * 10;
                        StaPeakInfoModel group = listStaPeakGroup.Find(p => p.PeakGroupChannel == groupChannel);
                        if (group != null)
                        {
                            group.FindCount++;
                            group.PeadAverageChannel = (group.PeadAverageChannel + peakChannel) / 2d;
                        }
                        else
                        {
                            listStaPeakGroup.Add(new StaPeakInfoModel()
                            {
                                FindCount = 1,
                                PeadAverageChannel = peakChannel,
                                PeakGroupChannel = groupChannel,
                            });
                        }
                    }
                }

                //延时1s
                Father.adc.delay(1000);
            }

            //停止采集
            Father.停止测量_Click(this, null);
            Father.adc.delay(400);
            Father.清空测量_Click(this, null);

            string situation = "(未找到)";//表征稳峰完成情况，共有3种：1.成功寻到K的峰;2.稳峰未中断，但未找到K的峰;3.稳峰中断了

            //获得稳峰结果
            if (!interrupt)//如果不是异常中断稳峰，才从寻到的峰中找K40的峰，否则就什么也不管，以文件中的保存至作为K40的峰位
            {
                //listStaPeakGroup个数不为0，说明出现过在查询范围内的道址
                if (listStaPeakGroup.Count > 0)
                {
                    int findCount = 0;
                    double findChannel = 0;//找到次数最多的道址
                    foreach (var gro in listStaPeakGroup)
                    {
                        if (gro.FindCount > findCount)
                        {
                            findCount = gro.FindCount;
                            findChannel = gro.PeadAverageChannel;
                        }
                    }

                    //记录稳峰结果
                    KChannel_Real = findChannel;
                    situation = "(新)";
                }
            }
            else
                situation = "(中断)";

            //每次稳峰停止后且使用完后，都让interrupt复位
            interrupt = false;

            /**对于所有情况:1.成功寻到K的峰;2.寻峰未中断，但未找到K的峰;3.寻峰中断了，最终都是这样计算稳峰因子。
		    它们的区别就在于：只有情况1会更新KChannel_Real的值，情况2、3下KChannel_Real仍然保持从sp文件读取的值，也就是上次的稳峰结果*/
            factor_StabPeak = KChannel_Real / KChannel_Scale;

            //UI提示
            w_temp.P_state = "稳峰结束" + situation + ": " + KChannel_Real + "/" + KChannel_Scale + "=" + factor_StabPeak.ToString("f2");
            Father.adc.delay(1000);//显示1s再关闭

            //开启画图
            Father.autoRun.P_isDrawingOn = true;
            //恢复Chart
            //GrandFather.wfhost_Chart.Visibility = System.Windows.Visibility.Visible;
            //允许稳峰小界面关闭
            w_temp.canHide = true;
        }

        /// <summary>
        /// 开启预热的方法。就和稳峰写在一起吧
        /// </summary>
        public void StartWarmUp()
        {
            //判断串口
            if (!Father.andySP._serialPort.IsOpen)
            {
                MessageBox.Show("请先打开串口", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            //判断是否已经开始一个测量
            if (Father.autoRun.P_isReading)
            {
                MessageBox.Show("已开启了一次预热", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            W_StabilizePeak w_temp = Father.w_Warmup;

            //关闭画图
            Father.autoRun.P_isDrawingOn = false;
            //隐藏Chart
            //GrandFather.wfhost_Chart.Visibility = System.Windows.Visibility.Hidden;
            //打开小窗口
            Father.adc.OpenOneWindow(w_temp);
            //UI提示
            w_temp.P_state = "正在预热:";
            //不让稳峰小界面关闭
            w_temp.canHide = false;

            //开始采集——预热不要采集
            //Father.开始测量_Click(this, null);

            //连续采集一段时间——稳峰时间
            w_temp.progress_StabPeak.Maximum = time_WarmUp;
            for (int i = 0; i < time_WarmUp && !interrupt; i++)//这里，如果interrupt被设置为true，则会中断稳峰
            {
                //进度条值更新
                w_temp.progress_StabPeak.Value = i + 1;
                Father.adc.delay(1000);
            }

            //停止采集
            //Father.停止测量_Click(this, null);
            //Father.adc.delay(400);
            //Father.清空测量_Click(this, null);

            //每次停止后且使用完后，都让interrupt复位
            interrupt = false;

            //UI提示
            w_temp.P_state = "预热结束";
            Father.adc.delay(1000);//显示1s再关闭

            //开启画图
            Father.autoRun.P_isDrawingOn = true;
            //恢复Chart
            //GrandFather.wfhost_Chart.Visibility = System.Windows.Visibility.Visible;
            //允许稳峰小界面关闭
            w_temp.canHide = true;

            //预热结束后直接关闭即可
            w_temp.Hide();
        }

        #endregion
    }
}
