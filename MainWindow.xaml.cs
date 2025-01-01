using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using 核素识别仪.Models;
using 核素识别仪.Servers.DataServer;
using 核素识别仪.Utils;
using 核素识别仪.其他功能类;
using 核素识别仪.其他功能类.SQLite;
using 核素识别仪.小窗口;
using 核素识别仪.拓展功能.AlphaCPS;
using 核素识别仪.拟合助手_WPF;
using 核素识别仪.自定义控件;
using 核素识别仪.通用功能类;
using 核素识别仪.集成的数据类;
using static 核素识别仪.其他功能类.AndyFileRW;
using static 核素识别仪.自定义控件.AndySerialPort;
using static 核素识别仪.集成的数据类.HuoDu;
using MethodInvoker = System.Windows.Forms.MethodInvoker;


namespace 核素识别仪
{
    /// <summary>
    /// 作者：谢延磊
    /// 创建时间：2023年1月15日17:07:59
    /// 最后更改时间：2024年8月11日16:10:56
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region 全局变量

        public static MainWindow Instance;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 通用方法
        /// </summary>
        public AndyCommon adc = new AndyCommon();

        /// <summary>
        /// 串口设置界面，从中获取串口
        /// </summary>
        private SerialPortSetting f_Sp = new SerialPortSetting();

        /// <summary>
        /// 串口对象，由f_Sp中的串口对象赋值
        /// </summary>
        public AndySerialPort andySP;

        /// <summary>
        /// 文件读写用的
        /// </summary>
        public AndyFileRW andyFileRW = new AndyFileRW();

        /// <summary>
        /// 提示小窗口
        /// </summary>
        public W_Note w_Note = new W_Note();

        /// <summary>
        /// CPS数据库检索界面
        /// </summary>
        private W_CPSHistory w_CPSHistory = new W_CPSHistory();

        /// <summary>
        /// 程序集名称
        /// </summary>
        public string AssemblyName { get { return Assembly.GetExecutingAssembly().GetName().Name; } }

        #region 多道相关数据

        /// <summary>
        /// 从设备读取的数据集合
        /// </summary>
        public ReceDatas receDatas;

        #endregion

        #region 北京多道相关数据

        /// <summary>
        /// 表征是否使用的是北京的多道板子
        /// </summary>
        //public bool isBeiJing = true;

        /// <summary>
        /// 北京多道的指令集
        /// </summary>
        BeiJingInstr beiJingInstr = new BeiJingInstr();

        #endregion

        #region 功能开关

        private bool sw_StabilizePeak = false;
        /// <summary>
        /// 开机自动稳峰的开关
        /// </summary>
        public bool P_sw_StabilizePeak
        {
            get { return sw_StabilizePeak; }
            set
            {
                sw_StabilizePeak = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_sw_StabilizePeak"));
                }
            }
        }

        private bool sw_WarmUp = false;
        /// <summary>
        /// 开机自动预热的开关
        /// </summary>
        public bool P_sw_WarmUp
        {
            get { return sw_WarmUp; }
            set
            {
                sw_WarmUp = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_sw_WarmUp"));
                }
            }
        }

        private bool sw_BenDiCPS = false;
        /// <summary>
        /// 开机自动测量本底计数率的开关
        /// </summary>
        public bool P_sw_BenDiCPS
        {
            get { return sw_BenDiCPS; }
            set
            {
                sw_BenDiCPS = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_sw_BenDiCPS"));
                }
            }
        }

        /// <summary>
        /// 用于控制右键菜单是否显示
        /// 存在“功能开关.txt”文件中
        /// </summary>
        private bool isShowRightMenu = false;
        public bool P_isShowRightMenu
        {
            get { return isShowRightMenu; }
            set
            {
                isShowRightMenu = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_isShowRightMenu"));
                }
            }
        }

        /// <summary>
        /// 表示所连接的对象是哪个，目前有：龙哥FPGA、北京多道、强哥多道
        /// </summary>
        public enum MCUType
        {
            龙,//龙哥多道
            北京,//北京多道
            强//强哥多道
        }

        private MCUType MCU = MCUType.龙;
        /// <summary>
        /// 所连接的MCU是哪一个
        /// </summary>
        public MCUType P_MCU
        {
            get { return MCU; }
            set
            {
                MCU = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_MCU"));
                }

                P_MCUIndex = (int)P_MCU;//这里必须执行一次P_MCUIndex的set，否则保存参数的对象的Value不更新
            }
        }

        /// <summary>
        /// 所选MCU枚举对应的整数，主要用于在文件保存所选MCU信息
        /// </summary>
        public int P_MCUIndex
        {
            get { return (int)P_MCU; }
            set
            {
                if (P_MCU != (MCUType)value)//如果相等就不执行，否则会无限循环
                {
                    P_MCU = (MCUType)value;
                }

                //有这个，set时才会让保存参数对象的Value变化
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_MCUIndex"));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_MCU"));
                }
            }
        }

        /// <summary>
        /// 用来切换模式：是否是计算活度模式
        /// </summary>
        private bool isHuoDu = false;
        public bool P_isHuoDu
        {
            get { return isHuoDu; }
            set
            {
                isHuoDu = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_isHuoDu"));
                }
            }
        }

        private bool isParaEnglishName = true;
        /// <summary>
        /// 控制保存参数时，使用中文参数名还是英文。默认为英文，对外隐蔽性好。
        /// 这个要慎用，因为修改后，之前所有的参数都会被清除。如果想修改，一定要备份参数。
        /// 目前只能由程序改
        /// </summary>
        public bool P_isParaEnglishName
        {
            get { return isParaEnglishName; }
            set
            {
                isParaEnglishName = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_isParaEnglishName"));
                }
            }
        }

        private bool isUISimple = true;
        /// <summary>
        /// 控制是否简化界面，true表示简化界面，会隐藏掉一些不想让用户看到的。暂时不用
        /// </summary>
        public bool P_isUISimple
        {
            get { return isUISimple; }
            set
            {
                isUISimple = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_isUISimple"));
                }
            }
        }

        private bool isHideForLiuTao = false;
        /// <summary>
        /// 控制是否为刘涛隐藏一部分内容，选择false表示去隐藏，true表示不隐藏。
        /// 这个只能在“功能开关”文件里修改，它主要就是隐藏了一部分按钮和右键菜单
        /// </summary>
        public bool P_isHideForLiuTao
        {
            get { return isHideForLiuTao; }
            set
            {
                isHideForLiuTao = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_isHideForLiuTao"));
                }
            }
        }

        /// <summary>
        /// 控制是否隐藏一些内容，用于401马工。这个只能在程序里改
        /// </summary>
        private bool isHideFor401Ma = true;

        /// <summary>
        /// 控制是否在显示串口设置
        /// </summary>
        private bool isShow串口设置 = false;

        #endregion

        #endregion

        #region 构造器

        public MainWindow()
        {
            Console.WriteLine(DateTime.Now.ToString("G") + ": " + "InitializeComponent");
            InitializeComponent();
            Instance = this;

            #region 界面初始状态配置

            //隐藏设置界面
            grid_Settings.Visibility = Visibility.Collapsed;

            #endregion

            #region XAML资源查找

            autoRun = (AutoRun)FindResource("autoRun");
            receDatas = (ReceDatas)FindResource("receDatas");
            receDatas.Init();
            huoDu = (HuoDu)FindResource("huoDu");
            andySeekPeak = (AndySeekPeak)FindResource("andySeekPeak");
            reco = (Recognize)FindResource("reco");

            #endregion

            #region 初始化小窗口，放在Instance配置之后

            w_NucReco = new W_NucReco();
            w_NucLib = new W_NucLib();

            #endregion

            #region 推广this

            receDatas.Father = this;
            reco.Father = this;
            w_NucReco.Father = this;
            w_NucLib.Father = this;
            huoDu.Father = this;

            #endregion

            #region 绑定相关

            //绑定好选择MCU类型的ComboBox，需要在读取参数之前
            cb_MCUType.ItemsSource = Enum.GetNames(typeof(MCUType));
            if (cb_MCUType.Items.Count > 0)
                cb_MCUType.SelectedIndex = 0;

            #endregion

            Console.WriteLine(DateTime.Now.ToString("G") + ": " + "初始化Chart");
            init_Chart();

            Console.WriteLine(DateTime.Now.ToString("G") + ": " + "初始化串口");
            init_SerialPort();

            Console.WriteLine(DateTime.Now.ToString("G") + ": " + "初始化AutoRead");
            init_AutoRead();

            //稳峰相关对象初始化
            Console.WriteLine(DateTime.Now.ToString("G") + ": " + "初始化稳峰");
            Init_StabPeak();

            //活度测量相关的，一般核素识别仪不用
            Init_HuoDu();

            //初始化ROI计算相关
            Init_ROI();

            //初始化能量刻度相关
            Init_Fitting();

            //读取参数，需要在数据对象初始化后再执行
            InitReadParas();

            //初始化读取AlphaCPS页面相关
            Init_AlphaCPS();

            //初始化αCPS数据库相关
            AlphaCPSDataManager.Instance.Init();

            #region ★根据实际需求进行一些界面配置，需要在读取参数之后

            #region 设置手否要给刘涛隐藏一部分内容，只需要设置P_isHideForLiuTao即可
            if (isHideForLiuTao == true)
            {
                //隐藏一些按钮：
                bt_打开文件.Visibility = Visibility.Hidden;
                bt_保存文件.Visibility = Visibility.Hidden;
                bt_手动稳峰.Visibility = Visibility.Hidden;
                bt_核素库.Visibility = Visibility.Hidden;

            }
            #endregion

            #region 401马工用的界面隐藏

            if (isHideFor401Ma)
            {
                bt_核素识别.Visibility = Visibility.Collapsed;
                bt_核素库.Visibility = Visibility.Collapsed;
                bt_手动稳峰.Visibility = Visibility.Collapsed;
                tab_测量活度.Visibility = Visibility.Collapsed;

                //显示一个串口设置按钮在外面
                isShow串口设置 = true;

                ////更改标题
                //this.Title = "Csl测试仪";
                //this.Title = "紫外荧光获取探测器系统";

                tab_AlphaCPS.Visibility = Visibility.Visible;
            }

            #endregion

            //是否显示右键菜单
            if (isShowRightMenu)
                menu_Window.Visibility = Visibility.Visible;
            else
                menu_Window.Visibility = Visibility.Hidden;

            //是否显示串口设置
            if (isShow串口设置)
                bt_串口设置.Visibility = Visibility.Visible;
            else
                bt_串口设置.Visibility = Visibility.Collapsed;

            #endregion

            //隐藏控制台
            ConsoleHelper.HideConsole();
#if DEBUG
            ConsoleHelper.ShowConsole();
#endif

        }
        #endregion

        #region 与MainWindow相关的事件

        /// <summary>
        ///★程序关闭事件
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("确认关闭应用程序？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.No)
                e.Cancel = true;
            else
            {
                ConsoleHelper.ShowConsole();
                Console.WriteLine(DateTime.Now.ToString("G") + ": " + "正在关闭应用，请稍候...");

                //关闭自动读取的线程
                autoRun.P_isThreadOn = false;

                //保存参数
                Console.WriteLine(DateTime.Now.ToString("G") + ": " + "保存数据");
                SaveParas();

                //强制关闭子线程
                //Console.WriteLine(DateTime.Now.ToString("G") + ": " + "关闭线程");
                System.Environment.Exit(0);//实测这个可以关闭没有结束的子线程

                //关机
                Console.WriteLine(DateTime.Now.ToString("G") + ": " + "关闭进程");
                System.Windows.Application.Current.Shutdown();
            }

        }

        /// <summary>
        /// 整个界面Window大小改变事件，让所有内容都进行等比例放缩
        /// </summary>
        private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Window设计的时候的尺寸，需要根据实际情况改：
            const int DesignWidth = 1292;
            const int DesignHeight = 780;
            //double ratio_HW = DesignHeight / DesignWidth;

            //计算大小变化后的变化比例
            double xRatio = e.NewSize.Width / DesignWidth;
            double yRatio = e.NewSize.Height / DesignHeight;

            //不能缩小到比设计时还要小
            if (xRatio < 1)
            {
                xRatio = 1;
            }
            if (yRatio < 1)
            {
                yRatio = 1;
            }

            //取两个比例的较小值
            double minRatio = Math.Min(xRatio, yRatio);

            //设置缩放
            ScaleTransform scale = new ScaleTransform();
            //X、Y方向都按照相同比例缩放，这样内容就不会被拉高或拉宽
            scale.ScaleX = minRatio;
            scale.ScaleY = minRatio;
            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(scale);

            //grid_All.RenderTransformOrigin = new Point(0.5, 0.5);
            //grid_All.LayoutTransform = transformGroup;

            //让部分内容跟着缩放
            //FrameworkElement[] elements = new FrameworkElement[] { grid_Main, grid_Interface2 };
            //foreach (var item in elements)
            //{
            //    item.RenderTransformOrigin = new Point(0.5, 0.5);
            //    item.LayoutTransform = transformGroup;
            //}
        }

        /// <summary>
        /// MainWindow加载完成后的方法
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Show();

            //开机的自动配置，需要在读取参数之后执行
            AutoConfig();
        }

        /// <summary>
        /// MainWindow的属性修改事件
        /// </summary>
        private void MainWindow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        /// <summary>
        /// Window的右键菜单
        /// </summary>
        private void menu_WindowRightButton(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = e.Source as MenuItem;
            ContextMenu menu = (ContextMenu)menuItem.Parent;
            switch (menuItem.Header)
            {
                case "测试1":
                    break;
                case "测试2":

                    break;
                case "测试3":

                    break;
                case "设置":
                    if (grid_Settings.Visibility == Visibility.Visible)
                        grid_Settings.Visibility = Visibility.Collapsed;
                    else
                        grid_Settings.Visibility = Visibility.Visible;
                    andyChart.ResetWindowsFormsHostSize(wfhost_Chart);
                    break;
                case "隐藏控制台":
                    ConsoleHelper.HideConsole();
                    break;
                case "显示控制台":
                    ConsoleHelper.ShowConsole();
                    break;
            }
        }

        /// <summary>
        /// 记录上次响应窗口SizeChanged事件的时间，用来控制响应此事件的频率
        /// </summary>
        DateTime lastSizeChangedTime = new DateTime();

        /// <summary>
        /// 窗口大小变化事件
        /// </summary>
        private void _this_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if ((DateTime.Now - lastSizeChangedTime).TotalSeconds > 1)
            {
                Console.WriteLine("响应一次窗口SizeChanged事件");

                andyChart.ResetWindowsFormsHostSize(wfhost_Chart);
                lastSizeChangedTime = DateTime.Now;
            }

        }

        #endregion

        #region 串口收发

        /// <summary>
        /// 初始化串口的一些内容
        /// </summary>
        void init_SerialPort()
        {
            andySP = f_Sp.andySP;
            andySP.Father = this;
        }

        /// <summary>
        /// 打开串口设置界面按钮
        /// </summary>
        private void 打开串口设置界面_Click(object sender, RoutedEventArgs e)
        {
            adc.OpenOneWindow(f_Sp);
        }

        #region 串口热插拔更新串口号

        //改写
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            hwndSource?.AddHook(new HwndSourceHook(WndProc));

        }

        /// <summary>
        /// 串口热插拔事件上次发生的时间
        /// </summary>
        DateTime lastTime = new DateTime();
        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            //此函数是USB插拔事件处理方法
            if (msg == 0x219)//WM_DEVICECHANGE值为0x219，表示USB插拔发生
            {
                #region 测试用

                //Console.WriteLine(hwnd.ToInt64().ToString());
                //Console.WriteLine(msg.ToString());
                //Console.WriteLine(wparam.ToInt64().ToString());
                //Console.WriteLine(lparam.ToInt64().ToString());
                //Console.WriteLine(handled.ToString());
                //Console.WriteLine(); 

                #endregion

                #region 插入时执行

                if (wparam.ToInt64() == 32768)//插入USB
                {
                    Console.WriteLine("串口插入事件");

                    //只用于这个软件的：串口接上的时候就去自动配置一遍
                    AutoConfig();
                }

                #endregion

                #region 拔出时执行

                else if (wparam.ToInt64() == 32772)//拔出USB
                {
                    Console.WriteLine("串口拔出事件");
                }

                #endregion

                #region 插拔时都执行

                TimeSpan ts = DateTime.Now - lastTime;
                //判断现在距上次响应此事件的时间，有没有超过一定时间。
                //如果太短，则不响应这次事件；若间隔够一段时间，则响应事件，并更新lastTime
                if (ts.TotalMilliseconds > 900)
                {
                    lastTime = DateTime.Now;

                    andySP.串口热插拔时要做的事情();

                    Console.WriteLine("串口插拔事件");

                }

                #endregion
            }
            return IntPtr.Zero;
        }

        #endregion

        #endregion

        #region Chart相关

        /// <summary>
        /// 多道谱图
        /// </summary>
        public AndyChart andyChart;

        /// <summary>
        /// 初始化chart相关
        /// </summary>
        void init_Chart()
        {
            //确定chart控件对象
            andyChart = new AndyChart(chart_Putu);

            //配置一些属性：
            andyChart.dataChart.canCaiYangPinLvChange = false;
            andyChart.dataChart.isAxisXTime = false;

            //正式初始化
            andyChart.init();

            //根据需求添加Series
            andyChart.ClearAllSeries(andyChart.dataChart);
            andyChart.AddSeries(andyChart.dataChart, "pulse", System.Drawing.Color.DodgerBlue, SeriesChartType.StackedArea);
            /*Line也还可以  SetpLine也还行 FastLine贼快  Column很卡 StackedColumn好一点
            *StackedArea效果很好 Area效果次之**/

            //再来一个用于显示峰值的series
            andyChart.AddSeries(andyChart.dataChart, "peak", System.Drawing.Color.Transparent, SeriesChartType.Point);
            double[] XYs = new double[2048];
            andyChart.AddPoints(1, XYs.ToList(), XYs.ToList());

            //添加第3个图线，用来画ROI分析时的辅助线
            andyChart.AddSeries(andyChart.dataChart, "高斯拟合结果", System.Drawing.Color.Red, SeriesChartType.Line);
            andyChart.AddSeries(andyChart.dataChart, "原始数据", System.Drawing.Color.Green, SeriesChartType.Line);
            andyChart.AddSeries(andyChart.dataChart, "扣除本底数据", System.Drawing.Color.Blue, SeriesChartType.Line);
            andyChart.AddSeries(andyChart.dataChart, "本底数据", System.Drawing.Color.Purple, SeriesChartType.Line);

            //取个临时变量
            Chart chart1 = andyChart.dataChart.UIChart;

            #region 特殊样式设置

            //关闭图例
            chart1.Legends[0].Enabled = false;
            //标签格式
            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "{0}";
            //标签间隔
            chart1.ChartAreas[0].AxisX.Interval = 0;//标签的间隔，设为0似乎就是自动生成

            //Y轴光标选择最小分辨值
            chart1.ChartAreas[0].CursorY.Interval = 10;

            #region 谱图series的设置
            Series series1 = chart1.Series[0];
            series1.BorderColor = System.Drawing.Color.FromArgb(180, 26, 59, 105);
            series1.BorderWidth = 0;//影响线粗细
            series1.MarkerSize = 2;//标记点的大小 
            #endregion

            #region 第二个用于画寻峰结果的series设置

            series1 = chart1.Series[1];
            series1.LabelBackColor = System.Drawing.Color.Transparent;
            series1.BorderWidth = 0;//影响线粗细
            //设置数据点的样式
            series1.MarkerColor = System.Drawing.Color.Transparent;
            series1.MarkerSize = 0;//标记点的大小，干脆不要了，看不到点，只能看到标签
            series1.MarkerStyle = MarkerStyle.Triangle;
            series1.MarkerBorderWidth = 0;
            //设置数据点标签的样式
            series1.Color = System.Drawing.Color.Transparent;
            series1.Font = new System.Drawing.Font("宋体", 13, System.Drawing.FontStyle.Bold);
            //series1.Font = new System.Drawing.Font("微软雅黑", 13, System.Drawing.FontStyle.Regular);
            series1.LabelForeColor = System.Drawing.Color.OrangeRed;
            //series1.LabelBackColor = System.Drawing.Color.White;
            //series1.LabelBorderColor = System.Drawing.Color.Blue;
            //series1.LabelBorderWidth = 1;
            series1.SmartLabelStyle.IsOverlappedHidden = false;//这个设置成false就不会隐藏label了
            series1.SmartLabelStyle.MovingDirection = LabelAlignmentStyles.Center;//设置这个后就不会乱跑，标签只显示在数据点中心
            //series1.LabelAngle = 90; 

            #endregion

            //禁止横轴缩放，这样框选就可以只框而不放大了
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = true;

            #endregion

            //加个鼠标点击事件
            chart1.MouseDown += Chart_Putu_MouseDown;
            chart1.SelectionRangeChanged += Chart1_SelectionRangeChanged;
        }

        /// <summary>
        /// 这个未来用于ROI吧
        /// </summary>
        private void Chart1_SelectionRangeChanged(object sender, CursorEventArgs e)
        {
            Console.WriteLine("触发Chart1_SelectionRangeChanged");
            double start = e.NewSelectionStart;
            double end = e.NewSelectionEnd;

            if (w_ROIResult.isDoingROI)//通过这个判断，就不会在框选放大时计算ROI
            {
                w_ROIResult.P_left = (int)start;
                w_ROIResult.P_right = (int)end;
                w_ROIResult.CalculateROI();
            }
        }

        /// <summary>
        /// 谱图图表点击事件，得到所选的index
        /// </summary>
        private void Chart_Putu_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
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
                receDatas.SelectedChannel = (int)Math.Round(dd, 0);//四舍五入后就是所选的道址
            }
            catch (Exception)
            {
            }

            //receDatas.SelectedChannel = hit.PointIndex + 1;
        }

        /// <summary>
        /// 画一次多道谱图
        /// </summary>
        public void DrawMultiData()
        {
            List<double> datas;
            if (andySeekPeak.P_ifShowSmooth)//判断画平滑数据还是原始数据
                datas = receDatas.MultiDatas_Smooth.ToList();
            else
                datas = receDatas.MultiDatas.ToList();
            andyChart.BindPoints(0, Xs, datas);

            #region 画完图，调整一下chart的视图

            //Chart chart1 = andyChart.dataChart.UIChart;
            //double YScaleView = 0;
            //double x = chart1.ChartAreas[0].CursorX.Position;
            //if (double.IsNaN(x) == false)//如果有光标，则根据光标的数据，设置Y方向的范围
            //{
            //    YScaleView = datas[(int)x] * 1.2;//想要的Y轴范围，为当前坐标处数值的1.2倍，坐标选择哪里，就以哪里为基准，这里不要超过屏幕就行
            //}
            ////如果当前的范围太小，就设置为光标处数据的1.2倍；若范围很大，就不用了
            //if (chart1.ChartAreas[0].AxisY.ScaleView.Size < YScaleView)
            //{
            //    chart1.ChartAreas[0].AxisY.ScaleView.Size = YScaleView;
            //}
            andyChart.AutoAdjustYScaleView(datas);

            #endregion
        }

        #region 缩放相关

        /// <summary>
        /// 放大镜功能按钮
        /// </summary>
        private void bt_ChartZoom_Click(object sender, RoutedEventArgs e)
        {
            andyChart.Chart打开放大镜模式();
        }

        /// <summary>
        /// 缩放重置
        /// </summary>
        private void bt_ChartZoomReset_Click(object sender, RoutedEventArgs e)
        {
            andyChart.Chart缩放重置();
        }

        /// <summary>
        /// 缩放按钮——X放大
        /// </summary>
        private void bt_XZoomUp_Click(object sender, RoutedEventArgs e)
        {
            andyChart.ZoomUpDownXY(true, true);
        }

        /// <summary>
        /// 缩放按钮——X缩小
        /// </summary>
        private void bt_XZoomDown_Click(object sender, RoutedEventArgs e)
        {
            andyChart.ZoomUpDownXY(true, false);
        }

        /// <summary>
        /// 缩放按钮——Y放大
        /// </summary>
        private void bt_YZoomUp_Click(object sender, RoutedEventArgs e)
        {
            andyChart.ZoomUpDownXY(false, true);
        }

        /// <summary>
        /// 缩放按钮——Y缩小
        /// </summary>
        private void bt_YZoomDown_Click(object sender, RoutedEventArgs e)
        {
            andyChart.ZoomUpDownXY(false, false);
        }
        #endregion

        #endregion

        #region ★核心自动流程

        #region 相关数据类

        /// <summary>
        /// 与自动运行相关的数据集合
        /// </summary>
        //public AutoRun autoRun = new AutoRun();
        public AutoRun autoRun;

        /// <summary>
        /// 谱图横坐标的数据，是一个固定的
        /// </summary>
        List<double> Xs = new List<double>();

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化自动运行相关
        /// </summary>
        void init_AutoRead()
        {
            //横坐标数据
            for (int i = 0; i < 2048; i++)
            {
                Xs.Add(i + 1);
            }

            //单纯计时的定时器初始化
            timer_MeasureTime.Interval = 100;
            timer_MeasureTime.Elapsed += Timer_MeasureTime_Elapsed;

            //自动读取数据、更新图表的Timer
            timer_Auto.Interval = 1000;
            timer_Auto.Elapsed += Timer_Auto_Elapsed;

        }

        #endregion

        #region 自动读取数据线程、定时器

        #region 单纯计时间用的定时器

        /// <summary>
        /// 用于计测量时间的定时器。点击开始按钮，打开；关闭按钮，停止
        /// </summary>
        System.Timers.Timer timer_MeasureTime = new System.Timers.Timer();

        /// <summary>
        /// 单纯计测量时间的定时器触发事件
        /// </summary>
        private void Timer_MeasureTime_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            receDatas.P_measuredTime += 0.1;
        }

        ///// <summary>
        ///// 苏佳洁相关类
        ///// </summary>
        //class Su
        //{
        //    /// <summary>
        //    /// 讨厌列表
        //    /// </summary>
        //    List<string> hates = new List<string>();

        //    public Su()
        //    {
        //        hates.Add("行测");
        //        hates.Add("公基");
        //        hates.Add("高数");
        //    }
        //}


        #endregion

        #region 自动运行的全部内容（读取、解析数据、寻峰识别、更新图表）

        /// <summary>
        /// 自动采集的定时器
        /// </summary>
        public System.Timers.Timer timer_Auto = new System.Timers.Timer();

        /// <summary>
        /// ★自动运行的方法
        /// 开启定时器后，等待一个Interval后才会执行第一次
        /// </summary>
        private void Timer_Auto_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            #region 测量时间累加

            Dispatcher.Invoke(new MethodInvoker(delegate { receDatas.P_measuredTime += timer_Auto.Interval / 1000; }));

            #endregion

            #region 自动判断串口

            if (!andySP._serialPort.IsOpen)
            {
                autoRun.P_isThreadOn = false;
                Console.WriteLine("自动流程：串口中断");

                //自动停止定时器线程
                timer_Auto.Stop();
                autoRun.P_isReading = false;

                MessageBox.Show(Properties.Resources.Res_串口已关闭, Properties.Resources.Res_提示, MessageBoxButton.OK, MessageBoxImage.Error);

                goto end;
            }

            #endregion

            #region 读取并解析数据

            if (MCU == MCUType.北京)//北京多道数据采集
            {
                //发送采集指令
                andySP.SPSend(beiJingInstr.Cmd_Measure, false);
                adc.delay(100);

                Console.WriteLine("开始一次读取");

                //读取数据
                byte[] bufferAll = (byte[])andySP.SPReceive(AndySerialPort.SPReceiveType.Byte);

                //判断接收数据是否正常
                if (bufferAll.Length == 8212)
                {
                    //解析北京多道数据
                    receDatas.AnalyseMultiData(bufferAll);

                    Console.WriteLine("解析数据一次");
                }

                #region 采集CPS数据

                #region 通道0的CPS，是GM管的CPS

                //发送采集指令
                andySP.SPSend(beiJingInstr.Cmd_MeasureGMCPS0, false);
                adc.delay(10);

                //读取数据
                bufferAll = (byte[])andySP.SPReceive(AndySerialPort.SPReceiveType.Byte);

                //判断接收数据是否正常
                int cps0 = 0;
                if (bufferAll.Length == 12)
                {
                    //解析CPS数据
                    cps0 += (bufferAll[8 + 3] & 0xFF);
                    cps0 += (bufferAll[8 + 2] & 0xFF) << 8;
                    cps0 += (bufferAll[8 + 1] & 0xFF) << 16;
                    cps0 += (bufferAll[8 + 0] & 0xFF) << 24;
                }

                #endregion

                #region 通道1的CPS，作为中子的CPS

                //发送采集指令
                andySP.SPSend(beiJingInstr.Cmd_MeasureGMCPS1, false);
                adc.delay(10);

                //读取数据
                bufferAll = (byte[])andySP.SPReceive(AndySerialPort.SPReceiveType.Byte);

                //判断接收数据是否正常
                int cps1 = 0;
                if (bufferAll.Length == 12)
                {
                    //解析CPS数据
                    cps1 += (bufferAll[8 + 3] & 0xFF);
                    cps1 += (bufferAll[8 + 2] & 0xFF) << 8;
                    cps1 += (bufferAll[8 + 1] & 0xFF) << 16;
                    cps1 += (bufferAll[8 + 0] & 0xFF) << 24;
                }

                #endregion

                //设置GM CPS
                receDatas.Cps_GM = cps0;

                #endregion
            }
            else if (MCU == MCUType.龙)///Long多道数据采集
            {
                andySP.SPSend("FF01AA", false);
                byte[] bufferAll = (byte[])andySP.WaitSPRece(AndySerialPort.SPReceiveType.Byte, 600);

                if (bufferAll != null && bufferAll[0] == 0xFF && bufferAll[bufferAll.Length - 1] == 0xAA && bufferAll.Length > 6100)
                {
                    //解析多道数据
                    receDatas.AnalyseMultiData(bufferAll);

                    Console.WriteLine("解析数据一次");
                }

                //adc.delay(100);

                //Console.WriteLine("开始一次读取");

                ////读取数据
                //byte[] bufferAll = (byte[])andySP.SPReceive(AndySerialPort.SPReceiveType.Byte);

            }
            else if (MCU == MCUType.强)
            {
                //发送采集指令
                andySP.SPSend("010401001000FC36", false);

                Console.WriteLine("开始一次读取");

                //读取数据
                byte[] bufferAll = (byte[])andySP.WaitSPRece(SPReceiveType.Byte, 900);

                //判断接收数据是否正常
                if (bufferAll != null && bufferAll.Length == 8192)
                {
                    //解析北京多道数据
                    receDatas.AnalyseMultiData(bufferAll);

                    Console.WriteLine("解析数据一次");
                }
            }

            //上面解析完多道数据、CPS数据、时间等参数后，计算CPS和Rate
            receDatas.UpdateCPSRate();

            #endregion

            #region 画图、寻峰、核素识别

            DrawAndReco();

            #region 原来分散开的

            //#region 寻峰、核素识别的数据处理

            //reco.SeekPeakIndexes();
            //reco.RecognizeNuc();

            //#endregion

            //#region 主线程更新UI，画图、显示识别结果

            //Dispatcher.Invoke(new MethodInvoker(delegate
            //{
            //    //判断数据是否更新
            //    if (autoRun.P_isDataNew && autoRun.P_isDrawingOn)
            //    {
            //        //画图
            //        //if (andySeekPeak.P_ifShowSmooth)//判断画平滑数据还是原始数据
            //        //    andyChart.BindPoints(0, Xs, receDatas.MultiDatas_Smooth.ToList());
            //        //else
            //        //    andyChart.BindPoints(0, Xs, receDatas.MultiDatas.ToList());
            //        DrawMultiData();

            //        autoRun.P_isDataNew = false;
            //        Console.WriteLine("画图一次");

            //        //标注寻峰结果
            //        reco.MarkPeaks();

            //        //将新的核素识别结果手动更新到dg
            //        w_NucReco.RefreshDgRecoResult();
            //    }

            //}));

            //#endregion  

            #endregion

            #endregion

            #region 一次处理结束

            Console.WriteLine("定时器完成一次");
            end:
            if (!timer_Auto.Enabled)
                Console.WriteLine("定时器关闭");

            #endregion
        }

        /// <summary>
        /// 对当前的多道数据进行寻峰、识别、画图、标注全过程。
        /// </summary>
        public void DrawAndReco()
        {
            #region 寻峰、核素识别的数据处理

            reco.SeekPeakIndexes();
            reco.RecognizeNuc();

            #endregion

            #region 主线程更新UI，画图、显示识别结果

            Dispatcher.Invoke(new MethodInvoker(delegate
            {
                //判断数据是否更新
                if (autoRun.P_isDataNew && autoRun.P_isDrawingOn)
                {
                    //画图
                    //if (andySeekPeak.P_ifShowSmooth)//判断画平滑数据还是原始数据
                    //    andyChart.BindPoints(0, Xs, receDatas.MultiDatas_Smooth.ToList());
                    //else
                    //    andyChart.BindPoints(0, Xs, receDatas.MultiDatas.ToList());
                    DrawMultiData();

                    autoRun.P_isDataNew = false;
                    Console.WriteLine("画图一次");

                    //标注寻峰结果
                    reco.MarkPeaks();

                    //将新的核素识别结果手动更新到dg
                    w_NucReco.RefreshDgRecoResult();
                }

            }));

            #endregion 
        }

        #endregion

        #endregion

        #region 用户使用的控件

        /// <summary>
        /// 开始测量按钮
        /// </summary>
        public void 开始测量_Click(object sender, RoutedEventArgs e)
        {
            //判断串口
            if (!andySP._serialPort.IsOpen)
            {
                MessageBox.Show("请先打开串口", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            //如果是北京的多道，则需要单独发送一个开始采集指令
            if (MCU == MCUType.北京)
            {
                //发送一个开始采集指令
                f_Sp.andySP.SPSend(beiJingInstr.Cmd_Start, false);
            }
            else if (MCU == MCUType.龙)//Long多道，发送一下配置信息，现在不用了，开机自动配置中已发送
            {
                //f_Sp.andySP.SPSend("55006428000a004000120303e8008c00a501090087095555001407fe040501040000ef", false);
            }
            else if (MCU == MCUType.强)
            {
                //不需要向强哥单片机发送开始采集内容，只需要打开定时器，发送采集指令即可。
            }

            //判断是否为新的开始
            if (autoRun.P_isCleared)
            {
                //如果是一次新的开始，则记录开始时间；否则是继续上一次，不能重置开始时间
                autoRun.P_startTime = DateTime.Now;
                autoRun.P_isCleared = false;
            }

            //开线程和画图定时器
            timer_Auto.Start();
            autoRun.P_isReading = true;

            #region 原来旧的不用的
            //if (!autoRun.P_isReading)//判断autoRun.P_isReading为false的话，说明线程是真的结束了，只有当之前的线程结束，才能开启一个新的线程，以及定时器
            //{
            //    //开定时器
            //    timer_Drawing.Enabled = true;
            //    timer_MeasureTime.Start();

            //    //设置标志位，保证线程正常开始
            //    autoRun.P_isThreadOn = true;
            //    Thread thread_AutoRead = new Thread(ThreadTask_AutoRead);
            //    thread_AutoRead.Start();
            //} 
            #endregion

            #region 对于计算活度模式附加的

            if (isHuoDu && !isAutoConfig)//活度计算模式打开，且这个开始测量不是自动配置中的
            {
                //设置定时器间隔为设置的测量时间
                timer_HuoDuAutoStop.Interval = huoDu.P_msTime_S * 1000 + timer_Auto.Interval * 0.7;//加时间是为了不要中断最后一次测量

                //开始定时器，到时会自动停止测量
                timer_HuoDuAutoStop.Start();
            }

            #endregion

        }

        /// <summary>
        /// 停止测量按钮
        /// </summary>
        public void 停止测量_Click(object sender, RoutedEventArgs e)
        {
            //停定时器——我认为，不论串口是否打开，都要让定时器停下来
            if (autoRun.P_isReading == true)
            {
                timer_Auto.Stop();
                autoRun.P_isReading = false;
            }

            //如果是活度测量模式，则需要停止活度计时的定时器
            if (isHuoDu)
                timer_HuoDuAutoStop.Stop();

            //不管有没有报警，都让报警声音关闭
            receDatas.StopAlarmSound();

            //判断串口
            if (!andySP._serialPort.IsOpen)
            {
                MessageBox.Show("请先打开串口", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            //发送指令
            if (MCU == MCUType.北京)
            {
                andySP.SPSend(beiJingInstr.Cmd_Stop, false);
            }
            else if (MCU == MCUType.龙)//Long多道，需要发送停止指令
            {
                andySP.SPSend("FF03AA", false);
            }
            else if (MCU == MCUType.强)
            {
                //不需要向强哥单片机发送停止采集指令
            }

            #region 原来旧的不用了

            ////停止线程
            //autoRun.P_isThreadOn = false;

            ////停止定时器
            //timer_Drawing.Enabled = false;
            //timer_MeasureTime.Stop();

            #endregion
        }

        /// <summary>
        /// 清空测量数据按钮
        /// </summary>
        public void 清空测量_Click(object sender, RoutedEventArgs e)
        {
            //清空数据点
            for (int i = 0; i < andyChart.dataChart.UIChart.Series.Count; i++)
            {
                if (i < 2)//对于前两个整个谱图的数据，只是清零其Y值，但数据点不要丢
                    andyChart.ClearPointsData(i);
                else//其他的ROI辅助图线就直接清除
                    andyChart.ClearPoints(i);
            }
            //andyChart.ClearPointsData(0);
            //andyChart.ClearPointsData(1);
            autoRun.P_isDataNew = false;//把这个标志位置为false，之前的数据就不要了，下次画的图为最新解析的多道数据   

            //这一步可以去除ROI框选的痕迹
            chart_Putu.ChartAreas[0].CursorX.SetSelectionPosition(0, 0);

            //清零一些测量数据
            receDatas.ClearDatas();

            //计时清零
            receDatas.P_measuredTime = 0;
            receDatas.P_deadTime = 0;
            receDatas.P_liveTime = 0;

            //开始时间复位
            autoRun.P_startTime = DateTime.MinValue;

            //标志位：已清零赋为true，下次点击开始按钮时，是一个新的开始
            autoRun.P_isCleared = true;

            //判断串口，上面的清空软件数据不需要串口打开，所以一定会执行
            if (!andySP._serialPort.IsOpen)
            {
                MessageBox.Show("请先打开串口", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            //发送清除指令
            if (MCU == MCUType.北京)
            {
                andySP.SPSend(beiJingInstr.Cmd_Clear, false);
            }
            else if (MCU == MCUType.龙)//Long多道，需要发送清空指令
            {
                andySP.SPSend("FFFFAA", false);
            }
            else if (MCU == MCUType.强)
            {
                //不需要发送清空指令
            }
        }

        #endregion

        #endregion

        #region 寻峰、核素识别

        /// <summary>
        /// 包含寻峰方法的对象
        /// </summary>
        public AndySeekPeak andySeekPeak;

        /// <summary>
        /// 寻峰、核素识别相关的数据
        /// </summary>
        public Recognize reco;

        /// <summary>
        /// 核素识别结果小窗口
        /// </summary>
        public W_NucReco w_NucReco;

        /// <summary>
        /// 设置核素库小窗口
        /// </summary>
        public W_NucLib w_NucLib;

        /// <summary>
        /// 打开核素识别结果按钮
        /// </summary>
        private void 打开核素识别结果_Click(object sender, RoutedEventArgs e)
        {
            adc.OpenOneWindow(w_NucReco);
        }

        /// <summary>
        /// 打开设置核素库界面按钮
        /// </summary>
        private void 打开设置核素库界面_Click(object sender, RoutedEventArgs e)
        {
            adc.OpenOneWindow(w_NucLib);
        }

        #endregion

        #region 稳峰

        /// <summary>
        /// 稳峰小窗口
        /// </summary>
        public W_StabilizePeak w_StabilizePeak;

        /// <summary>
        /// 预热小窗口
        /// </summary>
        public W_StabilizePeak w_Warmup;

        /// <summary>
        /// 本底测量小窗口
        /// </summary>
        public W_StabilizePeak w_BenDiCPS;

        /// <summary>
        /// 稳峰相关数据方法
        /// </summary>
        public AndyStabilizePeak aStabPeak;

        /// <summary>
        /// 稳峰相关初始化。
        /// 预热和本底测量2个小界面也仪器初始化
        /// </summary>
        private void Init_StabPeak()
        {
            w_StabilizePeak = new W_StabilizePeak() { Father = this };
            w_StabilizePeak.P_whichFun = W_StabilizePeak.Fun.stabilizePeak;
            w_StabilizePeak.Init();
            //w_StabilizePeak.Show();
            //w_StabilizePeak.Hide();

            aStabPeak = new AndyStabilizePeak() { Father = this };

            w_Warmup = new W_StabilizePeak() { Father = this };
            w_Warmup.P_whichFun = W_StabilizePeak.Fun.warmUp;
            w_Warmup.Init();
            //w_Warmup.Show();
            //w_Warmup.Hide();

            w_BenDiCPS = new W_StabilizePeak() { Father = this };
            w_BenDiCPS.P_whichFun = W_StabilizePeak.Fun.benDiCPS;
            w_BenDiCPS.Init();
            //w_BenDiCPS.Show();
            //w_BenDiCPS.Hide();
        }

        /// <summary>
        /// 执行一次稳峰操作
        /// </summary>
        public void StartStabilizePeak()
        {
            Console.WriteLine("开始一次稳峰操作");
            aStabPeak.StartStabilizePeak();
        }

        /// <summary>
        /// 手动稳峰按钮
        /// </summary>
        private void bt_ManualStabPeak_Click(object sender, RoutedEventArgs e)
        {
            StartStabilizePeak();
        }

        #endregion

        #region 开机自动配置

        /// <summary>
        /// 表征是否正在进行开机自动配置
        /// </summary>
        bool isAutoConfig = false;

        /// <summary>
        /// 打开软件自动配置的内容（或者连上串口就自动配置）
        /// </summary>
        public void AutoConfig()
        {
            isAutoConfig = true;

            //表征AutoConfig流程中，之前的配置是否成功，来决定是否进行下面的配置
            bool configOK = true;

            //自动尝试连接串口
            string comName = otherParaToSave.P_comName;
            if (!comName.Equals(""))
            {
                #region 设置波特率

                if (MCU == MCUType.北京)
                {
                    //andySP.cb_BaudRate.SelectedIndex = 5;//921600
                    andySP.cb_BaudRate.Text = "921600";
                    andySP._serialPort.ReadBufferSize = 9000;
                }
                else if (MCU == MCUType.龙)
                {
                    //andySP.cb_BaudRate.SelectedIndex = 4;//460800
                    andySP.cb_BaudRate.Text = "460800";
                    andySP._serialPort.ReadBufferSize = 7000;
                }
                else if (MCU == MCUType.强)
                {
                    andySP.cb_BaudRate.Text = "115200";
                    andySP._serialPort.ReadBufferSize = 9000;
                }

                #endregion

                f_Sp.andySP.cb_SerialPortNumber.Text = comName;
                f_Sp.andySP.switchSerialPort_Click(this, null);
                if (f_Sp.andySP._serialPort.IsOpen)
                {
                    Console.WriteLine(Properties.Resources.Res_串口已打开 + comName);
                    w_Note.ShowNote(Properties.Resources.Res_串口已打开 + comName, 1500);
                    configOK &= true;
                }
                else
                {
                    Console.WriteLine(Properties.Resources.Res_串口已关闭 + comName);
                    //MessageBox.Show("串口未连接", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    configOK &= false;
                }
            }
            else
            {
                Console.WriteLine(Properties.Resources.Res_串口已关闭 + comName);
                MessageBox.Show(Properties.Resources.Res_串口已关闭, Properties.Resources.Res_提示, MessageBoxButton.OK, MessageBoxImage.Error);
                configOK &= false;
            }

            //尝试发送配置信息(LongFPGA才发)
            if (f_Sp.andySP._serialPort.IsOpen && MCU == MCUType.龙)
            {
                f_Sp.andySP.SPSend(otherParaToSave.P_FPGAConfigStr, false);
                Console.WriteLine("已发送配置信息");
                configOK &= true;
            }

            //预热
            if (configOK && sw_WarmUp)
            {
                Console.WriteLine("开始预热");
                aStabPeak.StartWarmUp();
            }

            //稳峰
            if (configOK && sw_StabilizePeak)
            {
                StartStabilizePeak();
            }

            //本底测量
            if (configOK && sw_BenDiCPS)
            {
                Console.WriteLine("开始本底测量");
                huoDu.StartBenDiCPS();
            }

            isAutoConfig = false;
        }

        /// <summary>
        /// 手动本底测量按钮
        /// </summary>
        private void bt_ManualBenDiMS_Click(object sender, RoutedEventArgs e)
        {
            清空测量_Click(this, null);
            Console.WriteLine("开始本底测量");
            huoDu.StartBenDiCPS();
        }

        #endregion

        #region 文件读写参数保存

        /// <summary>
        /// 稳峰相关存储因子
        /// </summary>
        List<SavingPara> savingParas_StabPeak = new List<SavingPara>();

        /// <summary>
        /// 活度计算需要的因子存储
        /// </summary>
        List<SavingPara> savingParas_HuoDu = new List<SavingPara>();

        /// <summary>
        /// this的需要保存的属性
        /// </summary>
        List<SavingPara> savingParas_This = new List<SavingPara>();

        #region 其他需要存的因子

        List<SavingPara> savingParas_Other = new List<SavingPara>();

        class OtherParaToSave
        {
            private string comName = "COM3";
            /// <summary>
            /// 串口名，应为“COMn”
            /// </summary>
            public string P_comName
            {
                get { return comName; }
                set { comName = value; }
            }

            private string FPGAConfigStr = "55006428000a004000120303e8008c00a501090087095555001407fe040501040000ef";
            /// <summary>
            /// Long的FPGA的配置信息字符串
            /// </summary>
            public string P_FPGAConfigStr
            {
                get { return FPGAConfigStr; }
                set { FPGAConfigStr = value; }
            }

        }

        OtherParaToSave otherParaToSave = new OtherParaToSave();

        #endregion

        /// <summary>
        /// 寻峰相关因子
        /// </summary>
        List<SavingPara> savingParas_SeekPeak = new List<SavingPara>();

        /// <summary>
        /// 核素识别相关因子
        /// </summary>
        List<SavingPara> savingParas_Reco = new List<SavingPara>();

        /// <summary>
        /// 数据采集相关因子
        /// </summary>
        List<SavingPara> savingParas_ReceData = new List<SavingPara>();

        /// <summary>
        /// 初始化读取参数，并从文件读取参数
        /// </summary>
        private void InitReadParas()
        {
            //局部临时变量
            SavingPara para;
            string sn, pn, type, fileName;
            string[] propertyNames;
            string[] saveNames;
            string[] types;

            #region 稳峰相关因子

            propertyNames = new string[] {
                "P_KChannel_Scale",
                "P_KChannel_Real",
                "P_KJudgeMax",
                "P_KJudgeMin",
                "P_time_StabPeak",
                "P_time_WarmUp",
            };
            saveNames = new string[] {
                "K40刻度道址",
                "K40真实道址",
                "K40稳峰结果查找上限",
                "K40稳峰结果查找下限",
                "稳峰时间(s)",
                "预热时间(s)",
            };
            if (isParaEnglishName)
            {
                saveNames = new string[] {
                "P_KChannel_Scale",
                "P_KChannel_Real",
                "P_KJudgeMax",
                "P_KJudgeMin",
                "P_time_StabPeak",
                "P_time_WarmUp",
            };
            }

            for (int i = 0; i < saveNames.Length; i++)
            {
                sn = saveNames[i];
                pn = propertyNames[i];
                //根据阈值名称创建一个新的SavingPara对象
                para = new SavingPara() { Name = sn, ParaType = "double" };

                //将其Value属性绑定到某个数据的属性上，属性名就与name相同
                para.ConnectValue(aStabPeak, pn);

                //这一步绑定时，如果对象没有这个属性，则这个SavingPara对象的Value会为null，之后可能会出错
                savingParas_StabPeak.Add(para);
            }

            //只在初始化的时候，从文件读一次参数
            andyFileRW.ReadPara(savingParas_StabPeak, "稳峰相关因子.txt");

            #endregion

            #region 其他因子

            propertyNames = new string[] {
                "P_comName",
                "P_FPGAConfigStr",
            };
            saveNames = new string[] {
                "串口名称(COMn)",
                "FPGA配置字符串",
            };
            if (isParaEnglishName)
            {
                saveNames = new string[] {
                "P_comName",
                "P_FPGAConfigStr",
            };
            }

            for (int i = 0; i < saveNames.Length; i++)
            {
                sn = saveNames[i];
                pn = propertyNames[i];
                //根据阈值名称创建一个新的SavingPara对象
                para = new SavingPara() { Name = sn, ParaType = "string" };

                //将其Value属性绑定到某个数据的属性上，属性名就与name相同
                para.ConnectValue(otherParaToSave, pn);

                //这一步绑定时，如果对象没有这个属性，则这个SavingPara对象的Value会为null，之后可能会出错
                savingParas_Other.Add(para);
            }

            //只在初始化的时候，从文件读一次参数
            andyFileRW.ReadPara(savingParas_Other);

            #endregion

            #region 活度计算相关因子

            propertyNames = new string[] {
                "P_volume",
                "P_msTime_S",
                "P_unit_CPS",
                "P_unit_Act",
                "P_time_BenDi",
                "P_offset_CPS",
            };
            saveNames = new string[] {
                "活度计算体积(L)",
                "活度单次测量时间(s)",
                "计数率单位(小写cps或cpm)",
                "活度测量模式(小写bq或bq/l)",
                "本底测量时间(s)",
                "计算计数率的左右偏移道址(整数)",
            };
            if (isParaEnglishName)
            {
                saveNames = new string[] {
                "P_volume",
                "P_msTime_S",
                "P_unit_CPS",
                "P_unit_Act",
                "P_time_BenDi",
                "P_offset_CPS",
            };
            }

            //设置两个对象：
            object connectObject = huoDu;//所保存的属性所在的对象
            List<SavingPara> savingParaList = savingParas_HuoDu;//自己定义的相应的savingPara对象

            for (int i = 0; i < saveNames.Length; i++)
            {
                sn = saveNames[i];
                pn = propertyNames[i];

                //设置一下每个属性的type
                type = "string";
                if (pn.Equals("P_volume") || pn.Contains("msTime"))
                    type = "double";
                if (pn.Contains("BenDi") || pn.Contains("offset"))
                    type = "int";
                if (pn.Equals("P_offset_CPS"))
                    type = "int";
                if (pn.Contains("unit"))
                    type = "string";

                //根据阈值名称创建一个新的SavingPara对象
                para = new SavingPara() { Name = sn, ParaType = type };

                //将其Value属性绑定到某个数据的属性上，属性名就与name相同
                para.ConnectValue(connectObject, pn);

                //这一步绑定时，如果对象没有这个属性，则这个SavingPara对象的Value会为null，之后可能会出错
                savingParaList.Add(para);
            }

            //只在初始化的时候，从文件读一次参数
            andyFileRW.ReadPara(savingParaList, "活度计算相关\\活度计算相关因子.txt");

            #endregion

            #region this中的属性，往往是一些开关

            propertyNames = new string[] {
                "P_isShowRightMenu",
                "P_isHuoDu",
                "P_sw_BenDiCPS",
                "P_sw_StabilizePeak",
                "P_sw_WarmUp",
                "P_isHideForLiuTao",
                "P_MCUIndex",
            };
            saveNames = new string[] {
                "是否为北京多道",
                "是否测量活度",
                "开机自动本底测量开关",
                "开机自动稳峰开关",
                "开机自动预热开关",
                "P_isHideForLiuTao",
                "所连接的MCU对象",
            };
            if (isParaEnglishName)
            {
                saveNames = new string[] {
                "P_isShowRightMenu",
                "P_isHuoDu",
                "P_sw_BenDiCPS",
                "P_sw_StabilizePeak",
                "P_sw_WarmUp",
                "P_isHideForLiuTao",
                "P_MCUIndex"};
            }

            types = new string[] {
                "bool",
                "bool",
                "bool",
                "bool",
                "bool",
                "bool",
                "int",
            };

            for (int i = 0; i < saveNames.Length; i++)
            {
                sn = saveNames[i];
                pn = propertyNames[i];
                type = types[i];
                //根据阈值名称创建一个新的SavingPara对象
                para = new SavingPara() { Name = sn, ParaType = type };

                //将其Value属性绑定到某个数据的属性上，属性名就与name相同。这里第一个参数要设置为想要保存的属性的对象
                para.ConnectValue(this, pn);

                //这一步绑定时，如果对象没有这个属性，则这个SavingPara对象的Value会为null，之后可能会出错
                savingParas_This.Add(para);
            }

            //只在初始化的时候，从文件读一次参数
            andyFileRW.ReadPara(savingParas_This, "功能开关.txt");

            #endregion

            #region 寻峰相关因子

            propertyNames = new string[] {
                "P_minHeight",
                "P_maxWidth",
                "P_juanNum",
                "P_sigma",
                "P_smoothTimes",
                "P_ifShowSmooth",
            };
            saveNames = new string[] {
                "最小峰高",
                "最大峰宽",
                "卷积向两边计算的数据个数",
                "高斯函数参数σ",
                "高斯滤波进行平滑的次数",
                "是否显示平滑曲线",
            };
            if (isParaEnglishName)
            {
                saveNames = new string[] {
                "P_minHeight",
                "P_maxWidth",
                "P_juanNum",
                "P_sigma",
                "P_smoothTimes",
                "P_ifShowSmooth",
            };
            }
            types = new string[]
            {
                "double",
                "double",
                "int",
                "double",
                "int",
                "bool",
            };
            fileName = "寻峰相关因子.txt";

            //设置两个对象：
            connectObject = andySeekPeak;//所保存的属性所在的对象
            savingParaList = savingParas_SeekPeak;//自己定义的相应的savingPara对象

            #region 这部分是一样的，进行初始化和绑定

            for (int i = 0; i < saveNames.Length; i++)
            {
                sn = saveNames[i];
                pn = propertyNames[i];
                type = types[i];

                //根据阈值名称创建一个新的SavingPara对象
                para = new SavingPara() { Name = sn, ParaType = type };

                //将其Value属性绑定到某个数据的属性上，属性名就与name相同
                para.ConnectValue(connectObject, pn);

                //这一步绑定时，如果对象没有这个属性，则这个SavingPara对象的Value会为null，之后可能会出错
                savingParaList.Add(para);
            }

            //只在初始化的时候，从文件读一次参数
            andyFileRW.ReadPara(savingParaList, fileName);

            #endregion

            #endregion

            #region 核素识别相关因子

            //设置两个对象：
            connectObject = reco;//所保存的属性所在的对象
            savingParaList = savingParas_Reco;//自己定义的相应的savingPara对象

            //设置属性名、类型等信息
            propertyNames = new string[] {
                "P_isPeaksShown",
                "P_energyDifference",
                "P_recoDiff",
            };
            saveNames = new string[] {
                "是否显示未识别的核素",
                "能量差异(值越大，置信度就越接近1)",
                "核素识别的能量绝对差值(keV)",
            };
            if (isParaEnglishName)
            {
                saveNames = new string[] {
                "P_isPeaksShown",
                "P_energyDifference",
                "P_recoDiff",
            };
            }
            types = new string[]
            {
                "bool",
                "double",
                "double",
            };
            fileName = "核素识别相关因子.txt";

            #region 这部分是一样的，进行初始化和绑定

            for (int i = 0; i < saveNames.Length; i++)
            {
                sn = saveNames[i];
                pn = propertyNames[i];
                type = types[i];

                //根据阈值名称创建一个新的SavingPara对象
                para = new SavingPara() { Name = sn, ParaType = type };

                //将其Value属性绑定到某个数据的属性上，属性名就与name相同
                para.ConnectValue(connectObject, pn);

                //这一步绑定时，如果对象没有这个属性，则这个SavingPara对象的Value会为null，之后可能会出错
                savingParaList.Add(para);
            }

            //只在初始化的时候，从文件读一次参数
            andyFileRW.ReadPara(savingParaList, fileName);

            #endregion

            #endregion

            #region 数据测量相关因子

            //设置两个对象：
            connectObject = receDatas;//所保存的属性所在的对象
            savingParaList = savingParas_ReceData;//自己定义的相应的savingPara对象

            //设置属性名、类型等信息
            propertyNames = new string[] {
                "P_num_MA",
                "LeftCutOff",
                "P_threshold1",
                "P_threshold2",
                "P_isAlarmSound",
            };
            saveNames = new string[] {
                "滑动平均数组个数",
                "谱图截止道址数",
                "一级阈值",
                "二级阈值",
                "是否开启声音报警",
            };
            if (isParaEnglishName)
            {
                saveNames = new string[] {
                "P_num_MA",
                "LeftCutOff",
                "P_threshold1",
                "P_threshold2",
                "P_isAlarmSound",
            };
            }
            types = new string[]
            {
                "int",
                "int",
                "double",
                "double",
                "bool",
            };
            fileName = "数据采集相关因子.txt";

            #region 这部分是一样的，进行初始化和绑定

            for (int i = 0; i < saveNames.Length; i++)
            {
                sn = saveNames[i];
                pn = propertyNames[i];
                type = types[i];

                //根据阈值名称创建一个新的SavingPara对象
                para = new SavingPara() { Name = sn, ParaType = type };

                //将其Value属性绑定到某个数据的属性上，属性名就与name相同
                para.ConnectValue(connectObject, pn);

                //这一步绑定时，如果对象没有这个属性，则这个SavingPara对象的Value会为null，之后可能会出错
                savingParaList.Add(para);
            }

            //只在初始化的时候，从文件读一次参数
            andyFileRW.ReadPara(savingParaList, fileName);



            #endregion

            #endregion

        }

        /// <summary>
        /// 保存所有参数，在程序关闭时执行一次
        /// </summary>
        private void SaveParas()
        {
            andyFileRW.WritePara(savingParas_StabPeak, "稳峰相关因子.txt");
            andyFileRW.WritePara(savingParas_Other);
            andyFileRW.WritePara(savingParas_This, "功能开关.txt");
            andyFileRW.WritePara(savingParas_HuoDu, "活度计算相关\\活度计算相关因子.txt");
            andyFileRW.WritePara(savingParas_SeekPeak, "寻峰相关因子.txt");
            andyFileRW.WritePara(savingParas_Reco, "核素识别相关因子.txt");
            andyFileRW.WritePara(savingParas_ReceData, "数据采集相关因子.txt");
        }

        #endregion

        #region Spe文件读写

        /// <summary>
        /// 保存到Spe文件按钮
        /// </summary>
        private void bt_WriteSpe_Click(object sender, RoutedEventArgs e)
        {
            SpeFileRW spe = new SpeFileRW();
            string lines = spe.GenerateSpeFile();
            andyFileRW.SaveLinesToCommonFile("SPE文件(*.spe)|*.spe", lines);
        }

        /// <summary>
        /// 加载Spe文件按钮
        /// </summary>
        private void bt_ReadSpe_Click(object sender, RoutedEventArgs e)
        {
            string[] lines = andyFileRW.ReadCommonFileToLines("SPE文件(*.spe)|*.spe");
            SpeFileRW spe = new SpeFileRW();
            if (lines.Length > 0)
                spe.LoadSpeFile(lines.ToList());
            //else
            //    MessageBox.Show("文件内容为空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            //w_Note.ShowNote("文件内容为空", 1500);
        }

        #endregion

        #region 有关活度测量的，一般的核素识别仪不用

        /// <summary>
        /// 活度相关数据和方法
        /// </summary>
        public HuoDu huoDu;

        /// <summary>
        /// 用来切换模式：是否是计算活度模式
        /// </summary>
        //private bool isHuoDu = false;

        /// <summary>
        /// 初始化活度计算相关，其实就是加载刻度因子
        /// </summary>
        private void Init_HuoDu()
        {
            //huoDu = new HuoDu() { Father = this };
            //加载活度计算刻度因子
            huoDu.Init_ScaleFactors();
            //初始化核素活度因子界面相关
            Init_W_NucActFactors();

            //加载一下道址预设值
            huoDu.Init_NucPeakChannelToSet();

            timer_HuoDuAutoStop.AutoReset = false;//只引发一次Elapse事件
            timer_HuoDuAutoStop.Elapsed += Timer_HuoDuAutoStop_Elapsed;
        }

        //考虑活度计算下的采集，就是原来的采集，但是要自动停止，可添加一个定时器，到时就按一次停止按钮。timer只在开始测量的时候，Start一次
        System.Timers.Timer timer_HuoDuAutoStop = new System.Timers.Timer();

        /// <summary>
        /// 在计算活度模式下，到时见就自动按一下停止按钮，只执行一次
        /// </summary>
        private void Timer_HuoDuAutoStop_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (autoRun.P_isReading)//正在读取时，才去按停止按钮；若未处于读取模式，应该是因为手动停止了，这里也就不停止了
                停止测量_Click(this, null);

            //停止后，正好在这里执行一下计算活度的过程
            huoDu.CalculateAct();

            timer_HuoDuAutoStop.Stop();//自动停止
        }

        #region 活度刻度因子界面

        public W_NucActFactors w_NucActFactors = new W_NucActFactors();

        void Init_W_NucActFactors()
        {
            w_NucActFactors.Father = this;
            w_NucActFactors.Init();
        }

        /// <summary>
        /// 打开核素活度刻度因子界面按钮
        /// </summary>
        private void 打开核素活度刻度因子界面_Click(object sender, RoutedEventArgs e)
        {
            adc.OpenOneWindow(w_NucActFactors);
        }

        /// <summary>
        /// 打开选择人为设定核素选择界面按钮
        /// </summary>
        private void 打开选择人为设定核素选择界面_Click(object sender, RoutedEventArgs e)
        {
            if (gb_SelectSetChannelNuc.Visibility == Visibility.Visible)
            {
                gb_SelectSetChannelNuc.Visibility = Visibility.Hidden;
            }
            else
            {
                gb_SelectSetChannelNuc.Visibility = Visibility.Visible;
            }
        }


        #endregion

        #endregion

        #region ROI计算

        ///// <summary>
        ///// 用来表征是否正在进行一次ROI计算
        ///// </summary>
        //private bool isDoingROI = false;

        ///// <summary>
        ///// 用来表征是否显示ROI分析辅助线（本底线、减本底值、高斯拟合值等）
        ///// </summary>
        //private bool isShowROILines = false;

        /// <summary>
        /// 给ROI计算显示结果用的小窗口
        /// </summary>
        W_ROIResult w_ROIResult = new W_ROIResult();

        /// <summary>
        /// 初始化ROI相关
        /// </summary>
        private void Init_ROI()
        {
            w_ROIResult = new W_ROIResult();
        }

        /// <summary>
        /// 开启选择ROI的状态
        /// </summary>
        private void bt_SelectROI_Click(object sender, RoutedEventArgs e)
        {
            adc.OpenOneWindow(w_ROIResult);//点这个按钮就把ROI界面呼出来
        }

        ///// <summary>
        ///// 计算ROI的方法
        ///// </summary>
        ///// <param name="left">框选的左边界值</param>
        ///// <param name="right">框选的右边界值</param>
        //public void CalculateROI(int left, int right)
        //{
        //    Console.WriteLine("开始计算一次ROI");

        //    //计算数据总个数
        //    int count = right - left + 1;

        //    //对总数有个下限限制
        //    if (count < 5)
        //        return;

        //    //生成XY数组，用于拟合
        //    double[] Xs = new double[count];
        //    double[] Ys = new double[count];//平滑后的数据
        //    double[] Ys_Ori = new double[count];//最原始的数据

        //    //从多道数据摘取ROI区域的数据
        //    for (int i = left; i <= right; i++)
        //    {
        //        Xs[i - left] = i;
        //        Ys[i - left] = receDatas.MultiDatas_Smooth[i - 1];//这里的i是道址，取数据减个1

        //        //这里记录滤波前的原始数据
        //        Ys_Ori[i - left] = receDatas.MultiDatas[i - 1];

        //        //应该来说，用原始数据和平滑数据效果差不多
        //        //这里就应该用平滑后的数据进行处理，如果用平滑前参差不齐的数据，在扣完本底后会出现脉冲数为负值

        //    }

        //    //取一下所选范围数据的总计数，存到sum_All
        //    double sum_All = 0;
        //    //double sum_All_Ori = 0;
        //    for (int i = 0; i < Ys.Length; i++)
        //    {
        //        //sum_All += Ys[i];
        //        sum_All += Ys_Ori[i];
        //        //sum_All_Ori += Ys_Ori[i];
        //    }
        //    double cps_All = sum_All / receDatas.P_measuredTime;

        //    //计算扣除本底后的数据，存在Ys_SubBG
        //    double[] Ys_SubBG = SubtractBackground(Xs, Ys);

        //    //取一下所选范围数据的净计数，存到sum_All
        //    double sum_Net = 0;
        //    for (int i = 0; i < Ys.Length; i++)
        //    {
        //        sum_Net += Ys_SubBG[i];
        //    }
        //    double cps_Net = sum_Net / receDatas.P_measuredTime;

        //    #region 这个数据，由于人手选的范围，扣除本底后两边可能会出现负值，这里要截取到最后一个大于零的数据

        //    //下面两个list记录小于等于0的数据的index
        //    List<int> indexes_Left = new List<int>();//在峰位左侧的
        //    List<int> indexes_Right = new List<int>();//在峰位右侧的
        //    for (int i = 0; i < Ys_SubBG.Length; i++)
        //    {
        //        if (Ys_SubBG[i] <= 0)
        //        {
        //            if (i < (Ys_SubBG.Length / 2))//如果index在中点左边
        //                indexes_Left.Add(i);
        //            else//如果index在中点右边
        //                indexes_Right.Add(i);
        //        }
        //    }

        //    //index可能很多个，我们要找，左侧的最大index和右侧的最小index 
        //    int leftMax = 0;
        //    int rightMin = Ys_SubBG.Length - 1;
        //    if (indexes_Left.Count > 0)
        //        leftMax = indexes_Left.Max();
        //    if (indexes_Right.Count > 0)
        //        rightMin = indexes_Right.Min();

        //    //取正数部分的数据
        //    List<double> Xs_Zheng = new List<double>();
        //    List<double> Ys_Zheng = new List<double>();
        //    for (int i = leftMax + 1; i <= rightMin - 1; i++)
        //    {
        //        Xs_Zheng.Add(Xs[i]);
        //        Ys_Zheng.Add(Ys_SubBG[i]);
        //    }
        //    //就用截取出来的正数部分数据进行高斯拟合

        //    #endregion

        //    //高斯拟合
        //    double[] res = FittingGauss(Xs_Zheng.ToArray(), Ys_Zheng.ToArray());//就用截取出来的正数部分数据进行高斯拟合
        //    double A = res[0];
        //    double u = res[1];
        //    double sigma = res[2];
        //    double[] Ys_Cal = new double[Ys.Length];//根据拟合结果算出的Y值
        //    for (int i = 0; i < Ys_Cal.Length; i++)
        //    {
        //        //公式：y=A*exp(-(x-u)^2/(2*sigma^2))
        //        Ys_Cal[i] = A * Math.Exp(-1 * Math.Pow(Xs[i] - u, 2) / (2 * sigma * sigma));
        //    }

        //    //画图显示一下
        //    if (isShowROILines)
        //    {
        //        andyChart.BindPoints(2, Xs.ToList(), Ys_Cal.ToList());//拟合高斯曲线
        //        andyChart.BindPoints(3, Xs.ToList(), Ys.ToList());//未处理过的原数据曲线
        //        andyChart.BindPoints(4, Xs.ToList(), Ys_SubBG.ToList());//原数据减本底的曲线
        //    }

        //    //计算半高宽
        //    double FWHM = 2 * Math.Sqrt(2 * Math.Log(2)) * sigma;

        //    //峰位就取u，分辨率：
        //    double resolutionRatio = FWHM / u;

        //    #region 计算完毕，UI显示结果

        //    //准备结果显示
        //    StringBuilder sb = new StringBuilder();
        //    sb.AppendLine("ROI结果：");
        //    sb.AppendLine("峰位：" + Math.Round(u, 2));
        //    sb.AppendLine("半高宽：" + Math.Round(FWHM, 2));
        //    sb.AppendLine("分辨率：" + Math.Round(resolutionRatio, 4) * 100 + "%");
        //    sb.AppendLine("总计数：" + Math.Round(sum_All, 2));
        //    sb.AppendLine("净计数：" + Math.Round(sum_Net, 2));
        //    sb.AppendLine("总计数率：" + Math.Round(cps_All, 2));
        //    sb.AppendLine("净计数率：" + Math.Round(cps_Net, 2));

        //    //显示ROI界面
        //    w_ROIResult.tb_Note.Text=sb.ToString();
        //    adc.OpenOneWindow(w_ROIResult);
        //    isDoingROI = false;

        //    #endregion

        //    //这一步可以去除框线的痕迹
        //    //chart_Putu.ChartAreas[0].CursorX.SetSelectionPosition(0, 0);
        //}

        ///// <summary>
        ///// 高斯函数拟合方法
        ///// </summary>
        ///// <returns>数组从低到高依次是参数：A,u,sigma</returns>
        //public double[] FittingGauss(double[] Xs, double[] Ys)
        //{
        //    //将Y值取ln
        //    double[] Ys_ln = new double[Ys.Length];
        //    for (int i = 0; i < Ys.Length; i++)
        //    {
        //        //这里如果Y值为0，就会出现取ln得到NaN
        //        if (Ys[i] == 0)
        //            Ys_ln[i] = Math.Log(0.1);
        //        else
        //            Ys_ln[i] = Math.Log(Ys[i]);
        //    }

        //    //以ln(y)值和x值作为拟合数据点，进行二次拟合，得到abc
        //    double[] result = FittingFuntion.FittingCurve(Xs, Ys_ln, FittingFuntion.FittingType.二次拟合);
        //    double a, b, c;
        //    a = result[2]; b = result[1]; c = result[0];

        //    //根据得到的二次函数因子，算出高斯函数因子:y=A*exp(-(x-u)^2/(2*sigma^2))
        //    double A;//高度
        //    double u;//均值
        //    double sigma;//方差
        //    A = Math.Exp(c - (b * b) / (4 * a));
        //    sigma = Math.Sqrt(-1 / a / 2);
        //    u = -b / 2 / a;

        //    //返回高斯函数的因子
        //    double[] res = new double[]
        //    {
        //        A,u,sigma
        //    };
        //    return res;
        //}

        ///// <summary>
        ///// ROI区数据扣除本底方法。就是两段连线往下的计数减掉即可。
        ///// </summary>
        ///// <returns>返回扣除本底后的数据。若失败，返回空数组</returns>
        //public double[] SubtractBackground(double[] Xs, double[] Ys)
        //{
        //    int count = Xs.Length;

        //    if (count <= 0)
        //        return new double[] { };

        //    //获取左右两个点的坐标
        //    Point pl = new Point(Xs[0], Ys[0]);
        //    Point pr = new Point(Xs[count - 1], Ys[count - 1]);

        //    //这两个点连成直线，得到一次函数，计算k b
        //    double k = (pr.Y - pl.Y) / (pr.X - pl.X);
        //    double b = pl.Y - k * pl.X;

        //    //本底数据
        //    double[] Ys_BG = new double[count];
        //    for (int i = 0; i < count; i++)
        //    {
        //        Ys_BG[i] = k * Xs[i] + b;
        //    }

        //    //画图看一下
        //    if (isShowROILines)
        //        andyChart.BindPoints(5, Xs.ToList(), Ys_BG.ToList());

        //    //对Ys处理，遍历，依次减去相应位置的本底数据，本底数据为X值代入一次函数算出的Y值
        //    double[] Ys_SubBG = new double[Ys.Length];//扣除本底的Y值数组
        //    for (int i = 0; i < Ys.Length; i++)
        //    {
        //        Ys_SubBG[i] = Ys[i] - Ys_BG[i];

        //        ////可能减出负值
        //        //if(Ys_SubBG[i]<0&&)
        //    }

        //    //减去本底后，第一个值和最后一个值会为0，这样在高斯拟合时，去ln会出问题，所以这里做近似处理，这两个值等于其相邻的值
        //    Ys_SubBG[0] = Ys_SubBG[1];
        //    Ys_SubBG[count - 1] = Ys_SubBG[count - 2];

        //    return Ys_SubBG;
        //}

        #endregion

        #region 能量刻度

        W_Fitting w_Fitting;

        /// <summary>
        /// 初始化能量刻度相关
        /// </summary>
        void Init_Fitting()
        {
            w_Fitting = new W_Fitting();
        }

        /// <summary>
        /// 打开拟合助手界面
        /// </summary>
        private void bt_OpenFittingWindow_Click(object sender, RoutedEventArgs e)
        {
            adc.OpenOneWindow(w_Fitting);
        }

        #endregion

        #region AlphaCPS采集相关

        public AlphaCPS DataContextAlphaCPS { get; set; } = AlphaCPS.Instance;

        /// <summary>
        /// 初始化读取AlphaCPS页面相关
        /// </summary>
        private void Init_AlphaCPS()
        {
            AlphaCPS.Instance.Init();
        }

        private void 打开CPS数据库检索界面_Click(object sender, RoutedEventArgs e)
        {
            adc.OpenOneWindow(w_CPSHistory);
        }

        #endregion
    }

}
