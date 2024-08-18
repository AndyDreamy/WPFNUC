using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using 核素识别仪.其他功能类.MVVMCommon;
using 核素识别仪.小窗口;
using 核素识别仪.自定义控件;
using 核素识别仪.其他功能类;
using static 核素识别仪.其他功能类.AndyModbusRTU;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows;
using 核素识别仪.Models;
using 核素识别仪.Servers.DataServer;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace 核素识别仪.拓展功能.AlphaCPS
{
    /// <summary>
    /// 为401马工增加的测量Alpha射线CPS的功能
    /// </summary>
    public class AlphaCPS : INotifyPropertyChanged
    {
        #region Instance

        private static Lazy<AlphaCPS> _instance = new Lazy<AlphaCPS>(() => { return new AlphaCPS(); });
        public static AlphaCPS Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        #endregion

        #region Data

        /// <summary>
        /// 存储读取的CPS数据
        /// </summary>
        List<AlphaCPSData> listCPSData = new List<AlphaCPSData>();

        private double _averageCPS;
        /// <summary>
        /// CPS数据的均值
        /// </summary>
        public double AverageCPS
        {
            get { return _averageCPS; }
            set
            {
                _averageCPS = value;
                OnPropertyChanged(nameof(AverageCPS));
            }
        }

        private double _cpsThreshold = 30;
        /// <summary>
        /// CPS报警阈值
        /// </summary>
        public double CpsThreshold
        {
            get { return _cpsThreshold; }
            set
            {
                _cpsThreshold = value;
                OnPropertyChanged(nameof(CpsThreshold));

                //属性值更新时触发绘制：
                DrawThresholdAndAverageLine();
            }
        }


        #endregion

        #region States

        private bool _isCollecting = false;
        /// <summary>
        /// 当前是否处于采集中，true表示采集中
        /// </summary>
        public bool IsCollecting
        {
            get { return _isCollecting; }
            set
            {
                _isCollecting = value;
                OnPropertyChanged(nameof(IsCollecting));
            }
        }


        #endregion

        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propName));
            }
        }

        private DateTime startTime = DateTime.MinValue;
        /// <summary>
        /// 采集开始时间
        /// </summary>
        public DateTime StartTime
        {
            get { return startTime; }
            set
            {
                startTime = value;
                OnPropertyChanged(nameof(StartTime));
                OnPropertyChanged(nameof(P_startTimeStr));
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

        private double measuredTime = 0;
        /// <summary>
        /// 已测量的时间，单位s（可以由设备读回，但现在先用外面的定时器去赋值）
        /// </summary>
        public double P_measuredTime
        {
            get { return measuredTime; }
            set
            {
                measuredTime = value;
                if (PropertyChanged != null)
                {
                    OnPropertyChanged(nameof(P_measuredTime));
                }
            }
        }

        /// <summary>
        /// 查询数据库时用到的CPS阈值
        /// </summary>
        public double QueryCPSThreshold
        {
            get => queryCPSThreshold; set
            {
                queryCPSThreshold = value;
                OnPropertyChanged(nameof(QueryCPSThreshold));
            }
        }
        private DateTime dBQueryStartTime = DateTime.Now;
        private DateTime dBQueryEndTime = DateTime.Now;

        /// <summary>
        /// 数据库检索时的开始时间
        /// </summary>
        public DateTime DBQueryStartTime
        {
            get => dBQueryStartTime; set
            {
                dBQueryStartTime = value;
                OnPropertyChanged(nameof(DBQueryStartTime));
            }
        }

        /// <summary>
        /// 数据库检索时的结束时间
        /// </summary>
        public DateTime DBQueryEndTime
        {
            get => dBQueryEndTime; set
            {
                dBQueryEndTime = value;
                OnPropertyChanged(nameof(DBQueryEndTime));
            }
        }

        /// <summary>
        /// 从数据库读取的CPS数据列表
        /// </summary>
        public ObservableCollection<AlphaCPSData> CPSDatasFromDB { get { return AlphaCPSDataManager.Instance.CPSDatasFromDB; } }

        #endregion

        #region Timer

        /// <summary>
        /// 用于采集AlphaCPS的定时器
        /// </summary>
        Timer timer_ReadAlphaCPS = new Timer(1000);

        private void Init_Timer_ReadAlphaCPS()
        {
            timer_ReadAlphaCPS.Elapsed += Timer_ReadAlphaCPS_Elapsed;
        }

        private void Timer_ReadAlphaCPS_Elapsed(object sender, ElapsedEventArgs e)
        {
            //定时器操作要求串口连接，若串口中断，则自动停止采集
            if (andySP._serialPort.IsOpen)
            {
                //采集CPS数据
                CollectCPS();

                //更新时间
                P_measuredTime += timer_ReadAlphaCPS.Interval / 1000;

                //更新图表
                RefreshChart();

            }
            else
            {
                //停止定时器
                timer_ReadAlphaCPS.Stop();

                //停止采集
                IsCollecting = false;

                //提示
                MessageBox.Show("串口中断，已自动停止采集", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Chart

        private AndyChart andyChart;

        /// <summary>
        /// 用于图表绘制的时间list
        /// </summary>
        private List<DateTime> listDateTime = new List<DateTime>();

        /// <summary>
        /// 用于图表绘制的CPS数据list
        /// </summary>
        private List<double> listCPSValue = new List<double>();

        /// <summary>
        /// 初始化图表
        /// </summary>
        private void Init_Chart()
        {
            //确定chart控件对象
            andyChart = new AndyChart(MainWindow.Instance.chart_AlphaCPS);

            //配置一些属性：
            andyChart.dataChart.canCaiYangPinLvChange = false;
            andyChart.dataChart.isAxisXTime = true;

            //正式初始化
            andyChart.init();

            //根据需求添加Series
            andyChart.ClearAllSeries(andyChart.dataChart);
            andyChart.AddSeries(andyChart.dataChart, "CPS", System.Drawing.Color.DodgerBlue, SeriesChartType.Line);
            /*Line也还可以  SetpLine也还行 FastLine贼快  Column很卡 StackedColumn好一点
            *StackedArea效果很好 Area效果次之**/

            //取个临时变量
            Chart chart1 = andyChart.dataChart.UIChart;

            #region 特殊样式设置

            //关闭图例
            chart1.Legends[0].Enabled = false;

            #region CPS折线series的设置
            Series series1 = chart1.Series[0];
            series1.BorderWidth = 4;//影响线粗细
            series1.MarkerSize = 4;//标记点的大小 
            #endregion

            #endregion

            //加个鼠标点击事件
            chart1.MouseDown += Chart1_MouseDown;

        }

        /// <summary>
        /// 谱图图表点击事件，得到所选的index
        /// </summary>
        private void Chart1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            try
            {
                Chart chart1 = (Chart)sender;
                System.Windows.Forms.DataVisualization.Charting.HitTestResult hit;
                hit = chart1.HitTest(e.X, e.Y);//e本身没有e.HitTestResult.ChartElementType，靠这样获得引起事件的点

                if (hit.ChartArea == null)
                    return;

                double dd = hit.ChartArea.CursorX.Position * 2048;//计算出点击位置大概对应的道址
                dd = hit.ChartArea.CursorX.Position;//出点击位置大概对应的道址
            }
            catch (Exception)
            {
            }

            //receDatas.SelectedChannel = hit.PointIndex + 1;
        }

        /// <summary>
        /// 更新CPS折线图
        /// </summary>
        private void RefreshChart()
        {
            //准备图表数据
            foreach (var cpsData in listCPSData)
            {
                listDateTime.Add(cpsData.Time);
                listCPSValue.Add(cpsData.CPS);
            }

            //更新均值
            AverageCPS = Math.Round(listCPSValue.Average(), 2);
            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                //更新CPS折线
                andyChart.BindPoints(0, listDateTime, listCPSValue);

                //画阈值线和均值线
                DrawThresholdAndAverageLine();

                //清空复用的list
                listDateTime.Clear();
                listDateTime.TrimExcess();
                listCPSValue.Clear();
                listCPSValue.TrimExcess();
            });
        }

        /// <summary>
        /// 更新阈值线和平均值线的绘制
        /// </summary>
        private void DrawThresholdAndAverageLine()
        {
            andyChart.ClearStripLine(false);
            andyChart.DrawStripLine($"CPS Average: {_averageCPS}", System.Drawing.Color.Orange, 4, _averageCPS, false);
            andyChart.DrawStripLine($"CPS Threshold: {_cpsThreshold}", System.Drawing.Color.Red, 4, _cpsThreshold, false);
        }

        /// <summary>
        /// 清空图表内容
        /// </summary>
        private void ClearChart()
        {
            andyChart.ClearPointsOfAllCS();
        }

        #region 缩放相关方法

        /// <summary>
        /// 放大镜功能按钮
        /// </summary>
        private void ChartZoom_Click(object sender)
        {
            andyChart.Chart打开放大镜模式();
        }

        /// <summary>
        /// 缩放重置
        /// </summary>
        private void ChartZoomReset_Click(object sender)
        {
            andyChart.Chart缩放重置();
        }

        /// <summary>
        /// 缩放按钮——X放大
        /// </summary>
        private void XZoomUp_Click(object sender)
        {
            andyChart.ZoomUpDownXY(true, true);
        }

        /// <summary>
        /// 缩放按钮——X缩小
        /// </summary>
        private void XZoomDown_Click(object sender)
        {
            andyChart.ZoomUpDownXY(true, false);
        }

        /// <summary>
        /// 缩放按钮——Y放大
        /// </summary>
        private void YZoomUp_Click(object sender)
        {
            andyChart.ZoomUpDownXY(false, true);
        }

        /// <summary>
        /// 缩放按钮——Y缩小
        /// </summary>
        private void YZoomDown_Click(object sender)
        {
            andyChart.ZoomUpDownXY(false, false);
        }
        #endregion

        #endregion

        #region SerialPort

        /// <summary>
        /// 串口设置界面，从中获取串口
        /// </summary>
        private SerialPortSetting f_Sp = new SerialPortSetting();

        /// <summary>
        /// 串口对象，由f_Sp中的串口对象赋值
        /// </summary>
        private AndySerialPort andySP;

        /// <summary>
        /// 用于实现CPS采集的Modbus助手
        /// </summary>
        private AndyModbusRTU _andyModbusRTU = new AndyModbusRTU();
        private double queryCPSThreshold = 30;

        private void Init_SerialPort()
        {
            andySP = f_Sp.andySP;
            andySP._serialPort.BaudRate = 115200;
            andySP.cb_BaudRate.Text = "115200";
            _andyModbusRTU.andySP = this.andySP;
        }

        #endregion

        #region Command

        /// <summary>
        /// 开始采集指令
        /// </summary>
        public DelegateCommand Cmd_StartCollecting { get; set; }

        /// <summary>
        /// 停止采集指令
        /// </summary>
        public DelegateCommand Cmd_StopCollecting { get; set; }

        /// <summary>
        /// 清除指令
        /// </summary>
        public DelegateCommand Cmd_ClearCPSData { get; set; }

        /// <summary>
        /// 打开串口设置界面指令
        /// </summary>
        public DelegateCommand Cmd_OpenSerialPortSetting { get; set; }

        #region Chart缩放相关
        public DelegateCommand Cmd_ChartZoom { get; set; }
        public DelegateCommand Cmd_ChartZoomReset { get; set; }
        public DelegateCommand Cmd_XZoomUp { get; set; }
        public DelegateCommand Cmd_XZoomDown { get; set; }
        public DelegateCommand Cmd_YZoomUp { get; set; }
        public DelegateCommand Cmd_YZoomDown { get; set; }
        #endregion

        #region 数据库相关

        /// <summary>
        /// 查询数据库的CPS数据命令
        /// </summary>
        public ICommand Cmd_QueryCPSDBData { get; set; }

        #endregion

        /// <summary>
        /// 初始化命令逻辑
        /// </summary>
        private void Init_Cmds()
        {
            Cmd_StartCollecting = new DelegateCommand
            {
                ExecuteAction = new Action<object>(StartCollecting)
            };

            Cmd_StopCollecting = new DelegateCommand
            {
                ExecuteAction = new Action<object>(StopCollecting)
            };

            Cmd_ClearCPSData = new DelegateCommand
            {
                ExecuteAction = new Action<object>(ClearCPSData)
            };

            Cmd_OpenSerialPortSetting = new DelegateCommand
            {
                ExecuteAction = new Action<object>(OpenSerialPortSetting)
            };

            #region Chart缩放相关

            Cmd_ChartZoom = new DelegateCommand { ExecuteAction = new Action<object>(ChartZoom_Click) };
            Cmd_ChartZoomReset = new DelegateCommand { ExecuteAction = new Action<object>(ChartZoomReset_Click) };
            Cmd_XZoomUp = new DelegateCommand { ExecuteAction = new Action<object>(XZoomUp_Click) };
            Cmd_XZoomDown = new DelegateCommand { ExecuteAction = new Action<object>(XZoomDown_Click) };
            Cmd_YZoomUp = new DelegateCommand { ExecuteAction = new Action<object>(YZoomUp_Click) };
            Cmd_YZoomDown = new DelegateCommand { ExecuteAction = new Action<object>(YZoomDown_Click) };

            #endregion

            Cmd_QueryCPSDBData = new DelegateCommand() { ExecuteAction = new Action<object>(QueryCPSDBDataMethod) };
        }

        #endregion

        #region Method

        /// <summary>
        /// 开始采集方法，开定时器，发指令
        /// </summary>
        private void StartCollecting(object para)
        {
            //测试代码
            //timer_ReadAlphaCPS.Interval = 1000;
            //CollectCPS();

            if (!andySP._serialPort.IsOpen)
            {
                MessageBox.Show("请先连接串口", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StartTime = DateTime.Now;
            IsCollecting = true;
            timer_ReadAlphaCPS.Start();
        }

        /// <summary>
        /// 停止采集方法，关定时器，发指令
        /// </summary>
        private void StopCollecting(object para)
        {
            if (!andySP._serialPort.IsOpen)
            {
                MessageBox.Show("请先连接串口", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsCollecting = false;
            timer_ReadAlphaCPS.Stop();
        }

        /// <summary>
        /// 清除数据方法，清空所有数据和UI
        /// </summary>
        private void ClearCPSData(object para)
        {
            //清空时间
            P_measuredTime = 0;
            StartTime = DateTime.MinValue;

            //清空数据
            listCPSData.Clear();
            listCPSData.TrimExcess();

            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                //清空图表
                ClearChart();
            });
        }

        /// <summary>
        /// 执行一次采集CPS
        /// CPS数据的信息：地址=0009,寄存器数量=2,类型=4字节Float
        /// </summary>
        private void CollectCPS()
        {
            #region 测试代码

#if DEBUG
            //模拟读取数据
            if (false)
            {
                Random rd = new Random();

                //记录此数据
                AlphaCPSData cps0 = new AlphaCPSData() { CPS = rd.Next(10, 1000) / 100d, Time = DateTime.Now };
                listCPSData.Add(cps0);

                //从开头遍历数据，如果时间超过5分钟以前，则去除
                int deleteNum = 0;
                foreach (AlphaCPSData cpsData in listCPSData)
                {
                    if ((DateTime.Now - cpsData.Time).TotalSeconds > 20)
                    {
                        deleteNum++;
                    }
                    else
                    {
                        break;
                    }
                }
                for (int i = 0; i < deleteNum; i++)
                {
                    listCPSData.RemoveAt(0);
                }

                //Insert到数据库
                AlphaCPSDataManager.Instance.InsertOneCPS(cps0);

                return;
            }
#endif

            #endregion

            double cpsResult;

            //定义CPS数据的Modbus信息
            ModbusDataInfo dataInfo = new ModbusDataInfo()
            {
                address = 9,
                dataType = "浮点型",
                registerLength = 2
            };

            //获取Modbus指令，发送、收取、解析数据
            byte[] request = _andyModbusRTU.Get0304Request(0x01, 0x04, dataInfo.address, dataInfo.registerLength);
            if (request?.Length > 0)
            {
                //发送和接收
                byte[] resultData = _andyModbusRTU.ModbusSendAndRece(request, 1000);

                //判断有效性，解析
                if (resultData != null && resultData.Length > 0)
                {
                    object res = _andyModbusRTU.AnalyseOneData(dataInfo.dataType, dataInfo.registerLength * 2, resultData);
                    if (res != null && res.GetType() == typeof(float))
                    {
                        //读取CPS成功
                        cpsResult = (float)res;

                        //记录此数据
                        AlphaCPSData cps = new AlphaCPSData() { CPS = cpsResult, Time = DateTime.Now };
                        listCPSData.Add(cps);

                        //从开头遍历数据，如果时间超过5分钟以前，则去除
                        int deleteNum = 0;
                        foreach (AlphaCPSData cpsData in listCPSData)
                        {
                            if ((DateTime.Now - cpsData.Time).TotalSeconds > 5 * 60)
                            {
                                deleteNum++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        for (int i = 0; i < deleteNum; i++)
                        {
                            listCPSData.RemoveAt(0);
                        }

                        //Insert到数据库
                        AlphaCPSDataManager.Instance.InsertOneCPS(cps);

                        Console.WriteLine($"listCPS.Count: {listCPSData.Count}");
                    }
                }
            }
        }

        /// <summary>
        /// 打开串口设置界面
        /// </summary>
        /// <param name="o"></param>
        private void OpenSerialPortSetting(object o)
        {
            MainWindow.Instance.adc.OpenOneWindow(f_Sp);
        }

        /// <summary>
        /// 查询数据库的CPS数据
        /// </summary>
        /// <param name="o"></param>
        private void QueryCPSDBDataMethod(object o)
        {
            //AlphaCPSDataManager.Instance.QueryAllData();
            AlphaCPSDataManager.Instance.QueryDataInPeriod(DBQueryStartTime, DBQueryEndTime);
        }

        #endregion

        #region Constructor

        private AlphaCPS()
        {
            //初始化命令
            Init_Cmds();
        }

        /// <summary>
        /// 需要主页面初始化完才能初始化的内容
        /// </summary>
        public void Init()
        {
            //初始化串口
            Init_SerialPort();

            //初始化定时器
            Init_Timer_ReadAlphaCPS();

            //初始化图表
            Init_Chart();
        }

        #endregion
    }
}
