using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;
using System.Windows.Media;
using Color = System.Drawing.Color;
using FontFamily = System.Drawing.FontFamily;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using Control = System.Windows.Forms.Control;
using HitTestResult = System.Windows.Forms.DataVisualization.Charting.HitTestResult;
using Cursors = System.Windows.Forms.Cursors;
using ToolTipEventArgs = System.Windows.Forms.DataVisualization.Charting.ToolTipEventArgs;
using System.IO;
using System.Windows.Forms.Integration;

namespace 核素识别仪.其他功能类
{
    /// <summary>
    /// 每有一个chart就有一个AndyChart对象
    /// 更新时间：2023年6月21日15:28:23
    /// </summary>
    public class AndyChart
    {
        #region 全局变量

        /// <summary>
        /// 定义表示一个Series的结构体类型，以关联一个Series和对应的数据X（或T）、Y
        /// Xs、Ts、Ys的定义，优点是想要访问数据点数据时很方便，缺点是多占用了内存
        /// </summary>
        public struct ChartSeries
        {
            public Series series;
            public List<double> Xs;
            public List<double> Ys;
            public List<DateTime> Ts;

            /// <summary>
            /// 这个series颜色需要为透明的点的Index
            /// </summary>
            public List<int> transpIndex;
        }

        /// <summary>
        /// 定义一个Chart的类，里面围绕着一个chart定义了很多相关的变量，方便与某个chart链接起来
        /// </summary>
        public class MyChart
        {
            /// <summary>
            /// UI中的chart本身
            /// </summary>
            public Chart UIChart { get; set; }

            /// <summary>
            /// 此myChart对象对应的Chart对象，的Series集合对应的ChartSeries对象集合，主要用来访问某个Series对应的Xs、Ys
            /// </summary>
            public List<ChartSeries> CSs { get; set; }

            /// <summary>
            /// chart的Y轴显示起始位置
            /// </summary>
            public double YStartPosition { get; set; }

            /// <summary>
            /// chart的时间轴数据的最大跨度，
            /// </summary>
            public double xSize { get; set; }

            /// <summary>
            /// chart的T轴数据的最大跨度，double型，是一段时间跨度的总微秒数（这个取决于chart初始化时设置的横轴的IntervalType，也可为秒、分钟等）
            /// </summary>
            public double tSize { get; set; }

            /// <summary>
            /// chart的Y轴数据的最大跨度
            /// </summary>
            public double ySize { get; set; }

            #region 功能开关

            /// <summary>
            /// 控制X轴是否为时间轴，默认为false，想要在外界修改X轴类型，需要先修改此属性，再执行一下this.init()方法
            /// </summary>
            public bool isAxisXTime { get; set; }

            /// <summary>
            /// 采集周期对数据点数量调整的开关
            /// </summary>
            public bool canCaiYangPinLvChange { get; set; }

            /// <summary>
            /// 第一次画图时是否调整点数量
            /// </summary>
            private bool isAdjustPointsWhenFirstDraw = true;
            /// <summary>
            /// 第一次画图时是否调整点数量
            /// </summary>
            public bool IsAdjustPointsWhenFirstDraw
            {
                get { return isAdjustPointsWhenFirstDraw; }
                set { isAdjustPointsWhenFirstDraw = value; }
            }


            #endregion

            #region 一些常用的样式设置

            /// <summary>
            /// 定义主题的枚举
            /// </summary>
            public enum Themes
            {
                浅色,
                深色
            }

            private Themes theme = Themes.浅色;
            /// <summary>
            /// 标志选用哪个主题
            /// </summary>
            public Themes P_theme
            {
                get { return theme; }
                set { theme = value; }
            }

            private bool isGradiant = true;
            /// <summary>
            /// 是否渐变
            /// </summary>
            public bool P_isGradiant
            {
                get { return isGradiant; }
                set { isGradiant = value; }
            }

            private int makerSize = 5;
            /// <summary>
            /// 图线标记的大小。默认为5，若不想显示标记，则设为0。
            /// </summary>
            public int P_makerSize
            {
                get { return makerSize; }
                set { makerSize = value; }
            }

            #endregion

            #region 缩放改变点数量相关

            /// <summary>
            /// caiyangpinlv最小为0（当scaleLength小于shangxian时），caiyangpinlv为n，重画图时就隔n个才画一个，为0就是全部都画
            /// </summary>
            public int caiyangpinlv = 0;//采样频率就是取部分数据点的频率，scaleLength是现用现算，shangxian是常数取相同的即可
            public double scaleLength = 0;//用来存储当前X轴ScaleView的宽度
            //public double shangxian = 64;//让caiyangpinlv等于1时的scaleLength，也就是说，当视图在X轴上的宽度为64时，就让所有的点都显示
            /*caiyangpinlv = (int) (scaleLength / shangxian)  */
            public double shangxian = 0.7;//横轴为时间轴时，这个值要比较小

            #endregion

            /// <summary>
            /// 构造函数，赋值UIChart，初始化CSs
            /// </summary>
            /// <param name="chart">UI上对应的chart</param>
            public MyChart(Chart chart)//初始化时只要赋一个chart的值就行
            {
                this.UIChart = chart;
                this.CSs = new List<ChartSeries>();
                isAxisXTime = false;
                canCaiYangPinLvChange = false;
            }

            /// <summary>
            /// 克隆方法，返回一个属性相同的新的MyChart对象。但是其UIChart为null，CSs为空值
            /// </summary>
            /// <returns></returns>
            public MyChart Clone()
            {
                MyChart myChart = new MyChart(null);
                myChart.CSs = new List<ChartSeries>();
                myChart.canCaiYangPinLvChange = canCaiYangPinLvChange;
                myChart.isAxisXTime = isAxisXTime;
                myChart.IsAdjustPointsWhenFirstDraw = IsAdjustPointsWhenFirstDraw;
                myChart.P_isGradiant = P_isGradiant;
                myChart.P_makerSize = P_makerSize;
                myChart.P_theme = P_theme;
                myChart.shangxian = shangxian;
                return myChart;
            }
        }

        public MyChart dataChart;//一个MyChart变量对应着一个UI上的chart

        /// <summary>
        /// 定义一个图名，根据实际情况使用
        /// </summary>
        private string chartName;
        public string ChartName
        {
            get { return chartName; }
            set { chartName = value; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 将UI的chart传入这个类的字段dataChart，把MyChart对象初始化好，让外界只需要传入一个Chart对象，就可以使用所有功能
        /// </summary>
        /// <param name="chart"></param>
        public AndyChart(Chart chart)
        {
            dataChart = new MyChart(chart);
        }

        /// <summary>
        /// 把初始化单独作为一个方法，而不放到构造函数中，是因为：
        /// 有时初始化AndyChart后，还想设置一些开关变量（如横轴是否为时间轴等），设置后再进行初始化
        /// </summary>
        public void init()
        {
            init_MyChart(dataChart); //将图表初始化
        }

        #endregion

        #region Chart初始化函数

        /// <summary>
        /// Chart外观样式相关设置，在函数里可以选择横轴是否为时间
        /// </summary>
        public void Chart通用样式设置(Chart chart1, bool isAxisXTime)
        {
            //防止某个chart忘记添加ChartArea、Legend，在这里补充一下
            if (chart1.ChartAreas.Count == 0)
            {
                chart1.ChartAreas.Add("Default");
            }
            if (chart1.Legends.Count == 0)
            {
                chart1.Legends.Add("Default");
            }

            //准备好主题色和边框色，方便修改
            Color themeColor = Color.LightSkyBlue;
            Color borderColor = Color.Gray;
            Color color_网格线 = Color.Brown;
            Color color_坐标轴 = Color.FromArgb(64, 64, 64, 64);
            Color color_坐标轴标签颜色 = Color.Black;

            //根据不同的主题，更改某些属性
            switch (dataChart.P_theme)
            {
                case MyChart.Themes.浅色:
                    themeColor = Color.LightSkyBlue;
                    borderColor = Color.Gray;
                    color_网格线 = Color.Brown;
                    color_坐标轴 = Color.FromArgb(64, 64, 64, 64);//灰色
                    color_坐标轴标签颜色 = Color.Black;
                    break;
                case MyChart.Themes.深色:
                    themeColor = Color.FromArgb(0x03, 0x59, 0xA0);//深蓝色
                    borderColor = Color.Gray;
                    color_网格线 = Color.Orange;
                    color_坐标轴 = Color.White;
                    color_坐标轴标签颜色 = Color.White;
                    break;
                default:
                    break;
            }

            #region 背景边框，图例，网格线

            //整个图（圆边矩形）背景边框样式
            chart1.BackColor = themeColor;
            if (dataChart.P_isGradiant)
                chart1.BackGradientStyle = GradientStyle.TopBottom;
            else
                chart1.BackGradientStyle = GradientStyle.None;
            chart1.BorderlineColor = borderColor;
            chart1.BorderlineDashStyle = ChartDashStyle.Solid;
            chart1.BorderlineWidth = 2;
            chart1.BorderSkin.SkinStyle = BorderSkinStyle.None;//如果设置某个边框风格，就会让图缩放起来很卡

            ////画图部分（中间的矩形）的背景、边框
            chart1.ChartAreas[0].BackColor = themeColor;
            if (dataChart.P_isGradiant)
                chart1.ChartAreas[0].BackGradientStyle = GradientStyle.TopBottom;
            else
                chart1.ChartAreas[0].BackGradientStyle = GradientStyle.None;
            chart1.ChartAreas[0].BackSecondaryColor = Color.White;
            chart1.ChartAreas[0].BorderColor = Color.FromArgb(64, 64, 64, 64);
            chart1.ChartAreas[0].BorderDashStyle = ChartDashStyle.Solid;
            chart1.ChartAreas[0].Name = "Default";
            chart1.ChartAreas[0].ShadowColor = Color.Black;

            //右上角的图例，设置了一个legend0的格式，似乎所有的series都用的这个格式
            chart1.Legends[0].BackColor = Color.Transparent;
            chart1.Legends[0].Enabled = true;
            chart1.Legends[0].Font = new Font("黑体", 10);
            chart1.Legends[0].IsTextAutoFit = false;
            chart1.Legends[0].Name = "Default";

            //平行于X、Y轴的网格线，设置是否有效、颜色和间距
            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = true;
            chart1.ChartAreas[0].AxisY.MajorGrid.LineColor = color_网格线;
            chart1.ChartAreas[0].AxisY2.MajorGrid.Enabled = true;
            chart1.ChartAreas[0].AxisY2.MajorGrid.LineColor = color_网格线;

            #endregion

            #region 决定坐标轴轴标签的格式和间隔，标签和刻度一一对应

            //让坐标标签向右偏移1，解决了标签从-1开始的问题
            chart1.ChartAreas[0].AxisX.IntervalOffset = 1;

            //x y轴刻度的字体
            chart1.ChartAreas[0].AxisX.LabelStyle.Font = new Font(new FontFamily("Trebuchet MS"), 8f);
            chart1.ChartAreas[0].AxisX.LabelStyle.ForeColor = color_坐标轴标签颜色;
            chart1.ChartAreas[0].AxisX.LineColor = color_坐标轴;
            chart1.ChartAreas[0].AxisY.LabelStyle.Font = new Font(new FontFamily("Trebuchet MS"), 8f);
            chart1.ChartAreas[0].AxisY.LabelStyle.ForeColor = color_坐标轴标签颜色;
            chart1.ChartAreas[0].AxisY.LineColor = color_坐标轴;
            //AxisY2是第二坐标轴
            chart1.ChartAreas[0].AxisY2.LabelStyle.Font = new Font(new FontFamily("Trebuchet MS"), 8f);
            chart1.ChartAreas[0].AxisY2.LabelStyle.ForeColor = color_坐标轴标签颜色;
            chart1.ChartAreas[0].AxisY2.LineColor = color_坐标轴;

            ////数据的x值类型——根据数据类型自动设置的，不设置也行
            //_series1.XValueType = ChartValueType.DateTime;

            //x轴标签格式
            if (isAxisXTime)//X轴为时间
                chart1.ChartAreas[0].AxisX.LabelStyle.Format = "yy/M/d\nHH:mm:ss";
            else//X轴为一般数据
                chart1.ChartAreas[0].AxisX.LabelStyle.Format = "{0.00}";
            chart1.ChartAreas[0].AxisX.LabelAutoFitStyle = LabelAutoFitStyles.StaggeredLabels;

            //y轴标签格式
            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "{0.00}";
            chart1.ChartAreas[0].AxisY.LabelAutoFitStyle = LabelAutoFitStyles.StaggeredLabels;
            chart1.ChartAreas[0].AxisY2.LabelStyle.Format = "{0.00}";
            chart1.ChartAreas[0].AxisY2.LabelAutoFitStyle = LabelAutoFitStyles.StaggeredLabels;

            //x轴分度值——类型、值
            chart1.ChartAreas[0].AxisX.Interval = 0;//标签的间隔，设为0似乎就是自动生成
            if (isAxisXTime)
                chart1.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Auto;
            //chart1.ChartAreas[0].AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;//默认为固定interval，也可以设置为自动根据轴长度调整间距

            //y轴分度值——类型、值
            chart1.ChartAreas[0].AxisY.Interval = 0;
            //_chart1.ChartAreas[0].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chart1.ChartAreas[0].AxisY2.Interval = 0;
            //_chart1.ChartAreas[0].AxisY2.IntervalAutoMode = IntervalAutoMode.VariableCount;

            #endregion

            #region 滚动条

            //设置轴显示长度的下限，可以由程序中根据实际情况决定，这里可以不设置
            if (isAxisXTime)
            {
                chart1.ChartAreas[0].AxisX.ScaleView.MinSizeType = DateTimeIntervalType.Milliseconds;
                chart1.ChartAreas[0].AxisX.ScaleView.MinSize = double.NaN;
            }

            // 设置自动放大与缩小的最小量——用于拖动滚动条
            if (isAxisXTime)
            {
                chart1.ChartAreas[0].AxisX.ScaleView.SmallScrollSizeType = DateTimeIntervalType.Milliseconds;
                chart1.ChartAreas[0].AxisX.ScaleView.SmallScrollMinSizeType = DateTimeIntervalType.Milliseconds;
            }
            chart1.ChartAreas[0].AxisX.ScaleView.SmallScrollSize = double.NaN;
            chart1.ChartAreas[0].AxisX.ScaleView.SmallScrollMinSize = double.NaN;//一次滚动的距离，设为NaN，应该就是最小了

            chart1.ChartAreas[0].AxisY.ScaleView.SmallScrollSize = double.NaN;
            chart1.ChartAreas[0].AxisY.ScaleView.SmallScrollMinSize = double.NaN;//一次滚动的距离，设为20比较丝滑

            chart1.ChartAreas[0].AxisY2.ScaleView.SmallScrollSize = double.NaN;
            chart1.ChartAreas[0].AxisY2.ScaleView.SmallScrollMinSize = double.NaN;//一次滚动的距离，设为20比较丝滑

            //以下为滚动条的样式设置：

            //chart1.ChartAreas[0].AxisX.ScrollBar.IsPositionedInside = false;//将滚动内嵌到坐标轴中,默认在里面
            chart1.ChartAreas[0].AxisX.ScrollBar.BackColor = Color.AliceBlue;
            chart1.ChartAreas[0].AxisX.ScrollBar.ButtonColor = Color.White;
            chart1.ChartAreas[0].AxisX.ScrollBar.LineColor = Color.Black;

            //chart1.ChartAreas[0].AxisY.ScrollBar.IsPositionedInside = false;
            chart1.ChartAreas[0].AxisY.ScrollBar.BackColor = Color.AliceBlue;
            chart1.ChartAreas[0].AxisY.ScrollBar.ButtonColor = Color.White;
            chart1.ChartAreas[0].AxisY.ScrollBar.LineColor = Color.Black;

            //chart1.ChartAreas[0].AxisY2.ScrollBar.IsPositionedInside = false;
            chart1.ChartAreas[0].AxisY2.ScrollBar.BackColor = Color.AliceBlue;
            chart1.ChartAreas[0].AxisY2.ScrollBar.ButtonColor = Color.White;
            chart1.ChartAreas[0].AxisY2.ScrollBar.LineColor = Color.Black;

            // 设置滚动条的大小
            chart1.ChartAreas[0].AxisX.ScrollBar.Size = 20;
            chart1.ChartAreas[0].AxisY.ScrollBar.Size = 20;
            chart1.ChartAreas[0].AxisY2.ScrollBar.Size = 20;

            // 设置滚动条的按钮的风格，下面代码是将所有滚动条上的按钮都显示出来
            chart1.ChartAreas[0].AxisX.ScrollBar.ButtonStyle = ScrollBarButtonStyles.All;//默认都显示
            chart1.ChartAreas[0].AxisX.ScrollBar.Enabled = true;
            chart1.ChartAreas[0].AxisY.ScrollBar.ButtonStyle = ScrollBarButtonStyles.All;//默认都显示
            chart1.ChartAreas[0].AxisY.ScrollBar.Enabled = true;
            chart1.ChartAreas[0].AxisY2.ScrollBar.ButtonStyle = ScrollBarButtonStyles.All;//默认都显示
            chart1.ChartAreas[0].AxisY2.ScrollBar.Enabled = true;

            #endregion

            #region 光标选择功能
            //x光标选择区域
            chart1.ChartAreas[0].CursorX.IsUserEnabled = true;//显示光标
            chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = false;//是否可以拖拽选择
            if (isAxisXTime)
            {
                chart1.ChartAreas[0].CursorX.Interval = 1;//光标的最小分辨大小
                chart1.ChartAreas[0].CursorX.IntervalType = DateTimeIntervalType.Minutes;
            }
            chart1.ChartAreas[0].CursorX.Interval = double.NaN;
            chart1.ChartAreas[0].CursorX.AxisType = AxisType.Primary;
            chart1.ChartAreas[0].CursorX.AutoScroll = true;
            chart1.ChartAreas[0].CursorX.LineColor = Color.Gold;
            chart1.ChartAreas[0].CursorX.LineWidth = 2;
            //y光标选择区域
            chart1.ChartAreas[0].CursorY.IsUserEnabled = true;
            chart1.ChartAreas[0].CursorY.IsUserSelectionEnabled = false;
            chart1.ChartAreas[0].CursorY.Interval = double.NaN;
            chart1.ChartAreas[0].CursorY.AxisType = AxisType.Primary;
            chart1.ChartAreas[0].CursorY.AutoScroll = true;
            chart1.ChartAreas[0].CursorY.LineColor = Color.Gold;
            chart1.ChartAreas[0].CursorY.LineWidth = 2;

            //允许框选缩放
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            chart1.ChartAreas[0].AxisY.ScaleView.Zoomable = true;

            #endregion
        }

        #region Series配置

        /// <summary>
        /// 给一个MyChart对象的CSs添加一个series
        /// </summary>
        /// <param name="mychart"></param>
        /// <param name="seriesName">该series的名称，会显示在图例上</param>
        /// <param name="color">该series图线的颜色</param>
        /// <param name="type">该series图线的类型</param>
        public void AddSeries(MyChart mychart, string seriesName, Color color, SeriesChartType type)
        {
            //添加一个新的CS;这里CSs必然是从空的开始添加的
            List<ChartSeries> CSs = mychart.CSs;
            CSs.Add(new ChartSeries() { series = new Series(), Xs = new List<double>(), Ys = new List<double>(), Ts = new List<DateTime>() });
            //初始化新的CS的series对象
            int li = CSs.Count - 1;
            init_Series(CSs[li].series, color, seriesName, type, mychart.isAxisXTime);//默认为蓝色折线，名称为“1#”，实际使用时，可以直接对series的Name等属性进行设置
                                                                                      //实际地将series添加到chart对象
            mychart.UIChart.Series.Add(CSs[li].series);
        }

        /// <summary>
        /// 给一个MyChart对象的CSs添加一个series，但它的Y轴是副轴，可实现双坐标轴显示。
        /// </summary>
        /// <param name="mychart"></param>
        /// <param name="seriesName">该series的名称，会显示在图例上</param>
        /// <param name="color">该series图线的颜色</param>
        /// <param name="type">该series图线的类型</param>
        public void AddSecondarySeries(MyChart mychart, string seriesName, Color color, SeriesChartType type)
        {
            //添加一个新的CS;这里CSs必然是从空的开始添加的
            List<ChartSeries> CSs = mychart.CSs;
            CSs.Add(new ChartSeries() { series = new Series() { YAxisType = AxisType.Secondary }, Xs = new List<double>(), Ys = new List<double>(), Ts = new List<DateTime>() });
            //初始化新的CS的series对象
            int li = CSs.Count - 1;
            init_Series(CSs[li].series, color, seriesName, type, mychart.isAxisXTime);//默认为蓝色折线，名称为“1#”，实际使用时，可以直接对series的Name等属性进行设置
                                                                                      //实际地将series添加到chart对象
            mychart.UIChart.Series.Add(CSs[li].series);
        }

        /// <summary>
        /// 删除所有的CS、series
        /// </summary>
        public void ClearAllSeries(MyChart mychart)
        {
            mychart.CSs.Clear();
            mychart.UIChart.Series.Clear();
        }
        public void ClearAllSeries()
        {
            dataChart.CSs.Clear();
            dataChart.UIChart.Series.Clear();
        }

        /// <summary>
        /// 为chart添加series后，设计series的名称、类型、样式
        /// </summary>
        /// <param name="Series1">要设计的Series变量名</param>
        /// <param name="plColor">线和点的颜色</param>
        /// <param name="seriesName">该Series的名称属性，用于显示在图例中</param>
        /// <param name="chartType">图线类型，输入格式为SeriesChartType.XX</param>
        public void init_Series(Series Series1, Color plColor, string seriesName, SeriesChartType chartType, bool isAxisXTime)
        {
            //外部新建Series并加入Chart，这个函数里只进行配置比较通用的内容，若需要微调，可以在外面进行
            Series1.Name = seriesName;
            Series1.ChartType = chartType;
            Series1.BorderColor = Color.FromArgb(180, 26, 59, 105);
            //Series1.BorderColor = Color.Yellow;
            Series1.BorderWidth = 5;//影响线粗细
            //Series1.ChartArea = "Default";
            //Series1.Legend = "Default";
            Series1.Color = plColor;// Color.FromArgb(220, 65, 140, 240);
            Series1.MarkerSize = dataChart.P_makerSize;//影响点大小
            Series1.MarkerStyle = MarkerStyle.Circle;
            Series1.MarkerColor = plColor;// Color.FromArgb(220, 65, 140, 240);
            Series1.ShadowColor = Color.Black;
            Series1.ShadowOffset = 1;

            //让Series添加一次点，再删除，这样画图时就会正常显示横坐标
            if (isAxisXTime)
                Series1.Points.AddXY(DateTime.Now, 0);//两种横坐标格式都考虑到
            else
                Series1.Points.AddXY(0, 0);
            Series1.Points.RemoveAt(0);
        }

        #endregion

        /// <summary>
        /// 初始化某个chart及其若干个series
        /// <paramref name="mychart">输入一个自定义的MyChart类对象</paramref>
        /// </summary>
        public void init_MyChart(MyChart mychart)
        {
            //引用一下chart控件
            Chart chart1 = mychart.UIChart;

            //引用一下mychart的CSs
            List<ChartSeries> CSs = mychart.CSs;

            #region 设计chart的样式
            Chart通用样式设置(chart1, mychart.isAxisXTime);
            #endregion

            #region 设计Series（颜色，名字），并添加到Chart

            CSs.Clear();//初始化某个myChart就清零掉

            //用CSs来访问某个chart的多个series，这样series、X、Y是关联着的。
            chart1.Series.Clear();//chart1.Series会默认有一个柱状图，给他删掉

            //这里默认添加1个series，用的时候可以手动添加更多
            for (int i = 0; i < 1; i++)//默认先设置上一个series
            {
                AddSeries(mychart, (i + 1).ToString() + "#", Color.DeepSkyBlue, SeriesChartType.Line);
            }

            #endregion

            #region chart相关的事件

            chart1.GetToolTipText += chart鼠标悬浮事件;
            chart1.MouseDown += chartMouseDown事件;
            chart1.MouseDoubleClick += Chart双击事件;
            chart1.MouseWheel += Chart滚轮事件;
            chart1.CursorPositionChanged += Chart放大镜功能关闭;
            chart1.PostPaint += Chart1_PostPaint;

            #endregion

        }

        #region 画阈值线

        /// <summary>
        /// 画一条Y轴或X轴上的界线（例如阈值线）
        /// </summary>
        /// <param name="text">显示的说明</param>
        /// <param name="color">线的颜色</param>
        /// <param name="lineWidth">线的宽度，一般取5即可</param>
        /// <param name="location">Y轴上的位置</param>
        /// <param name="isXAxis">true表示在X轴上画，否则在Y轴</param>
        public void DrawStripLine(string text, Color color, int lineWidth, double location, bool isXAxis)
        {
            //添加阈值线
            StripLine line = new StripLine();
            line.Text = text;
            line.ForeColor = System.Drawing.Color.Black;
            line.BackColor = color;
            line.StripWidth = 0;//这条线在坐标轴上的跨度
            line.TextAlignment = System.Drawing.StringAlignment.Far;

            line.Interval = 0;
            line.IntervalOffset = location;//在什么位置

            line.BorderWidth = lineWidth;
            line.BorderColor = color;
            line.BorderDashStyle = ChartDashStyle.Solid;

            if (isXAxis)
                dataChart.UIChart.ChartAreas[0].AxisX.StripLines.Add(line);
            else
                dataChart.UIChart.ChartAreas[0].AxisY.StripLines.Add(line);
        }

        /// <summary>
        /// 删除所有阈值线。
        /// </summary>
        /// <param name="isXAxis">true表示删除X轴的，否则删除Y轴的</param>
        public void ClearStripLine(bool isXAxis)
        {
            if (isXAxis)
                dataChart.UIChart.ChartAreas[0].AxisX.StripLines.Clear();
            else
                dataChart.UIChart.ChartAreas[0].AxisY.StripLines.Clear();
        }

        #endregion

        /// <summary>
        /// 设置一个坐标轴的样式。
        /// </summary>
        /// <param name="axis">坐标轴，通过访问Chart.ChartAreas[0].AxisY得到</param>
        /// <param name="title">坐标轴的标题</param>
        /// <param name="color">坐标轴标题的颜色</param>
        /// <param name="ali">标题对其方式，near就是靠左，far就是靠右</param>
        public void SetAxisStyle(Axis axis, string title, Color color, StringAlignment ali)
        {
            Font font = new Font("黑体", 10f, System.Drawing.FontStyle.Bold);

            axis.Title = title;
            axis.TitleForeColor = color;
            axis.TitleAlignment = ali;
            axis.TitleFont = font;
        }

        #endregion

        #region Chart缩放

        /// <summary>
        /// 根据所有数据点的最大值最小值，确定X和Y的最大跨度，将结果保存到MyChart对象的属性中
        /// </summary>
        public void GetXYSize(MyChart myChart)
        {
            //计算X、Y轴的最大跨度
            List<DateTime> TMM = new List<DateTime>();//存放T轴数据的最大值、最小值
            List<double> XMM = new List<double>();//存放X轴数据的最大值、最小值
            List<double> YMM = new List<double>();//存放Y轴数据的最大值、最小值

            foreach (ChartSeries item in myChart.CSs)//遍历这个chart的所有series的X、Y，找到最大值、最小值
            {
                if (item.Ts.Count != 0)
                {
                    TMM.Add(item.Ts.Max());
                    TMM.Add(item.Ts.Min());
                }
                if (item.Xs.Count != 0)
                {
                    XMM.Add(item.Xs.Max());
                    XMM.Add(item.Xs.Min());
                }
                if (item.Ys.Count != 0)
                {
                    YMM.Add(item.Ys.Max());
                    YMM.Add(item.Ys.Min());
                }
            }

            //记录横纵坐标的跨度、Y的开始位置
            try
            {
                if (myChart.isAxisXTime)
                    myChart.tSize = TMM.Max().Subtract(TMM.Min()).TotalMilliseconds;
                else
                    myChart.xSize = XMM.Max() - XMM.Min();
                myChart.ySize = YMM.Max() - YMM.Min();

                myChart.YStartPosition = YMM.Min();

                //这里要应对，只有
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                //tb_Note.Text = "526行出错 " + ex.Source + ":" + ex.Message;
            }
        }

        /// <summary>
        /// chart的鼠标滚轮事件，若按住Control，可分别X轴、Y轴缩放、调整线条大小；不按时，滚动条滚动
        /// </summary>
        public void Chart滚轮事件(object sender, MouseEventArgs e)
        {

            Chart chart1 = (Chart)sender;
            MyChart myChart = dataChart;

            //按control时，滚动可以X,Y轴缩放，以及调整图线的大小
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                HitTestResult hit = chart1.HitTest(e.X, e.Y);
                if (hit.ChartElementType == ChartElementType.AxisLabels || hit.ChartElementType == ChartElementType.Axis)//如果鼠标在X、Y轴
                {
                    //把xSize、tSize、ySize计算出来，下面缩放要用
                    GetXYSize(myChart);

                    Axis a = hit.Axis;//获取鼠标所在的轴

                    //每当ZoomReset后，都需要zoom一次，否则ScaleView为NaN，无法变大变小
                    if (a.ScaleView.Size.ToString() == "NaN")
                    {
                        if (a == chart1.ChartAreas[0].AxisX)//X轴缩放
                        {
                            if (myChart.isAxisXTime)
                                a.ScaleView.Zoom(0, myChart.tSize, DateTimeIntervalType.Milliseconds);
                            else
                                a.ScaleView.Zoom(0, myChart.xSize, DateTimeIntervalType.Auto);
                        }
                        else//Y轴缩放
                        {
                            a.ScaleView.Zoom(myChart.YStartPosition, myChart.ySize * 1.5, DateTimeIntervalType.Auto);
                        }
                    }

                    //当ScaleView不为NaN，就正常缩放
                    if (e.Delta > 0)
                    {
                        //if (a.ScaleView.Size >= 0)
                        a.ScaleView.Size = a.ScaleView.Size * 0.7;
                    }
                    else
                    {
                        //if (a.ScaleView.Size <= myChart.xSize * 53.3)
                        a.ScaleView.Size = a.ScaleView.Size * 1.3;
                    }

                    Chart调整点数量(myChart);
                }
                else if (hit.ChartElementType == ChartElementType.PlottingArea && false) //如果在图表区域操作，则调整线的粗细大小，暂时不用这个功能了
                {
                    if (e.Delta > 0)
                    {
                        foreach (ChartSeries item in myChart.CSs)
                        {
                            if (item.series.BorderWidth < 10)
                            {
                                item.series.BorderWidth += 1;//影响线粗细
                            }
                            if (item.series.MarkerSize < 12 && item.series.MarkerSize > 0)//若MarkerSize为0，则不要增大
                            {
                                item.series.MarkerSize += 1;//影响点大小
                            }

                        }
                    }
                    else
                    {
                        foreach (ChartSeries item in myChart.CSs)
                        {
                            if (item.series.BorderWidth > 1)
                            {
                                item.series.BorderWidth -= 1;//影响线粗细
                            }
                            if (item.series.MarkerSize > 3)
                            {
                                item.series.MarkerSize -= 1;//影响点大小
                            }
                        }
                    }
                }
            }

            //没有按control时，滚轮是让滚动条滚动的功能
            else
            {
                HitTestResult hit = chart1.HitTest(e.X, e.Y);
                double interval;//X、Y轴视野移动的单位，为当前视野乘0.1
                if (hit.ChartElementType == ChartElementType.AxisLabels || hit.ChartElementType == ChartElementType.Axis)
                {
                    Axis a = hit.Axis;//所在的轴
                    interval = a.ScaleView.Size * 0.1;
                    if (myChart.isAxisXTime && a == chart1.ChartAreas[0].AxisX)
                    {
                        //interval = 0.000001;//如果是时间轴，则加减的值要特别小（这个值似乎与横坐标的类型有关，若为秒，则设置为0.001较合适；若为微妙，则设置为0.000001）
                        //interval = 0.001;
                        interval = 0.01;
                    }
                    if (a.ScaleView.Position.ToString() != "NaN")
                    {
                        if (e.Delta > 0)
                        {
                            a.ScaleView.Position += interval;
                        }
                        else
                        {
                            a.ScaleView.Position -= interval;
                        }
                    }
                }
            }

        }

        /// <summary>
        /// chart双击事件，双击坐标轴会ZoomReset;
        /// 双击绘图区，就转化成放大镜模式
        /// </summary>
        public void Chart双击事件(object sender, MouseEventArgs e)//双击坐标轴ZoomReset,里面有判断是X轴还是Y轴的模板
        {
            Chart chart1 = (Chart)sender;
            MyChart myChart = dataChart;
            HitTestResult hit = chart1.HitTest(e.X, e.Y);//如果e本身没有e.HitTestResult.ChartElementType，就这样获得引起事件的点
            if (hit.ChartElementType == ChartElementType.Axis || hit.ChartElementType == ChartElementType.AxisLabels)//如果鼠标下是坐标轴或者坐标轴标签（标签很重要）
            {
                Axis a = hit.Axis;
                if (a == chart1.ChartAreas[0].AxisX)//X轴
                {
                    chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
                    Chart调整点数量(myChart);
                }
                else if (a == chart1.ChartAreas[0].AxisY)//Y轴
                {
                    chart1.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);
                }
                else if (a == chart1.ChartAreas[0].AxisY2)
                    chart1.ChartAreas[0].AxisY2.ScaleView.ZoomReset(0);
            }
            else if (hit.ChartElementType == ChartElementType.PlottingArea)//双击绘图区，就转化成放大镜模式
            {
                Chart打开放大镜模式();
            }

        }

        /// <summary>
        /// chart的CursorPositionChanged事件触发后，就停止放大镜功能
        /// </summary>
        public void Chart放大镜功能关闭(object sender, CursorEventArgs e)
        {
            Chart chart1 = (Chart)sender;
            MyChart myChart = dataChart;
            Chart调整点数量(myChart);
            chart1.ChartAreas[0].CursorY.IsUserSelectionEnabled = false;
            chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = false;
            chart1.Cursor = Cursors.Default;

            //Chart无缩放框选()会让横向缩放禁止，这里恢复一下
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            //chart1.ChartAreas[0].CursorX.SelectionColor = Color.LightGray;
        }

        /// <summary>
        /// 打开放大镜按钮事件，作为外界某个控件触发的事件
        /// </summary>
        public void Chart打开放大镜模式()
        {
            Chart chart1 = dataChart.UIChart;
            chart1.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;
            chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            chart1.Cursor = Cursors.Cross;
        }

        /// <summary>
        /// 执行后，光标变成十字，只可以横向框选，选中后不会放大。核素识别中计算ROI会用到
        /// </summary>
        public void Chart无缩放框选()
        {
            Chart chart1 = dataChart.UIChart;
            chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            //chart1.ChartAreas[0].CursorX.SelectionColor = Color.Yellow;
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = false;//实现只框选不放大
            chart1.Cursor = Cursors.Cross;
        }

        /// <summary>
        /// 计算新的采样频率，调整显示的点数量
        /// </summary>
        public void Chart调整点数量(MyChart myChart)
        {
            //如果关闭了这个调整数据点个数的功能，则这里什么也不做
            if (!myChart.canCaiYangPinLvChange)
                return;

            //获取myChart中的变量
            Chart chart1 = myChart.UIChart;

            //计算当前X轴的ScaleView的宽度
            AxisScaleView scaleView = chart1.ChartAreas[0].AxisX.ScaleView;
            double scaleLength = scaleView.ViewMaximum - scaleView.ViewMinimum;

            //判断一下图上的点数
            int sum = 0;
            foreach (var item in chart1.Series)
            {
                sum += item.Points.Count;
            }
            if (sum < 1000)//如果总点数小于1000，则不重画
                return;

            if (myChart.caiyangpinlv != (int)(scaleLength / myChart.shangxian) && myChart.canCaiYangPinLvChange)//若canCiYangPinLvChange为false，则关闭调整点数量的功能
            {
                //根据当前X轴的ScaleView的宽度计算新的采样频率
                myChart.caiyangpinlv = (int)(scaleLength / myChart.shangxian);

                //textBox2.Text = (myChart.caiyangpinlv + 1).ToString();

                Series重画(myChart, myChart.caiyangpinlv);

            }
            //Cursor = Cursors.Default;
        }

        /// <summary>
        /// 根据新的采样频率重画一个series，数据点达到5万时，全部重画会有一点卡
        /// </summary>
        /// <param name="series1">哪个series</param>
        /// <param name="x">X点</param>
        /// <param name="y">Y点</param>
        /// <param name="pinlv">采样频率</param>
        /// <param name="transparent">透明点的Index</param>
        public void Series重画(Series series1, List<double> x, List<double> y, int pinlv, List<int> transparent)
        {
            //Cursor = Cursors.WaitCursor;

            //清掉重画
            series1.Points.Clear();

            int indexx = 0;

            /*根据采样频率遍历x、ylist：*/
            for (int i = 0; i < x.Count; i += pinlv + 1)
            {
                /*把遍历到的数据加点points里：*/
                series1.Points.AddXY(x[i], y[i]);

                #region 根据存储透明点index的list，生成断点
                if (transparent.Count > 0) //可能一个透明点也没有
                {
                    if (indexx < transparent.Count - 1) //当现在判断的是最后一个之前的透明index，就正常判断变透明，正常找下一个点
                    {
                        if (i >= transparent[indexx])
                        {
                            series1.Points[series1.Points.Count - 1].Color = Color.Transparent;

                            for (int j = indexx; j < transparent.Count; j++)
                            {
                                if (i < transparent[j])
                                {
                                    indexx = j;//去找下一个透明的index
                                    break;
                                }
                            }

                        }
                    }
                    if (indexx == transparent.Count - 1)//当现在判断的是最后一个透明index，就只变透明，不找下一个点，而且让indexx加一，之后的xlist的其他点就不会再考虑透明的情况了
                    {
                        if (i >= transparent[indexx])
                        {
                            series1.Points[series1.Points.Count - 1].Color = Color.Transparent;
                            indexx += 1;//加到了transparent.Count，则不会再执行画透明点的程序
                        }
                    }

                }
                #endregion

            }

            //Cursor = Cursors.Default;
        }
        public void Series重画(MyChart myChart, int pinlv)
        {
            //遍历每个series
            foreach (var CS in myChart.CSs)
            {
                //根据采样频率重新画
                if (myChart.isAxisXTime)
                {
                    List<DateTime> Ts_less = new List<DateTime>();
                    List<double> Ys_less = new List<double>();
                    for (int i = 0; i < CS.Ys.Count; i += pinlv + 1)
                    {
                        Ts_less.Add(CS.Ts[i]);
                        Ys_less.Add(CS.Ys[i]);
                    }
                    CS.series.Points.DataBindXY(Ts_less, Ys_less);
                }
                else
                {
                    List<double> Xs_less = new List<double>();
                    List<double> Ys_less = new List<double>();
                    for (int i = 0; i < CS.Ys.Count; i += pinlv)
                    {
                        Xs_less.Add(CS.Xs[i]);
                        Ys_less.Add(CS.Ys[i]);
                    }
                    CS.series.Points.DataBindXY(Xs_less, Ys_less);
                }
            }

        }

        /// <summary>
        /// chartX轴Y轴缩放重置，放到一个按钮上
        /// </summary>
        public void Chart缩放重置()
        {
            Chart chart1 = dataChart.UIChart;
            MyChart myChart = dataChart;
            switch (chart1.Tag)
            {
                case "myChart1":
                    myChart = dataChart;
                    break;
            }

            //Cursor = Cursors.WaitCursor;
            chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chart1.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);
            chart1.ChartAreas[0].AxisY2.ScaleView.ZoomReset(0);
            Chart调整点数量(myChart);

        }

        /// <summary>
        /// 标志位，用来控制是否要根据光标处的Y值，自动调整纵向视图。
        /// 会在缩放重置时赋值为true，在纵向缩放时赋值为false
        /// </summary>
        private bool isAutoAdjustYScale = false;

        /// <summary>
        /// 根据谱图数据、光标所在位置，自动调整Chart的Y方向ScaleView为合适的视图范围。
        /// 这个方法和纵向手动缩放是一个矛盾，故暂时先不用这个功能了。
        /// </summary>
        /// <param name="datas"></param>
        public void AutoAdjustYScaleView(List<double> datas)
        {
            Chart chart1 = dataChart.UIChart;

            //只有当Y轴没有被缩放时，才进行这个自动缩放。然而没有被缩放的时候，ScaleView的值为NaN，是无法调和的矛盾
            //if (!double.IsNaN(chart1.ChartAreas[0].AxisY.ScaleView.Size))
            //    return;
            if (!isAutoAdjustYScale)//判断标志位，若不允许自动调整，则什么也不做
                return;

            double YScaleView = 0;
            double x = chart1.ChartAreas[0].CursorX.Position;
            if (double.IsNaN(x) == false)//如果有光标，则根据光标的数据，设置Y方向的范围
            {
                YScaleView = datas[(int)x] * 1.2;//想要的Y轴范围，为当前坐标处数值的1.2倍，坐标选择哪里，就以哪里为基准，这里不要超过屏幕就行
            }
            //如果当前的范围太小，就设置为光标处数据的1.2倍；若范围很大，就不用了
            if (chart1.ChartAreas[0].AxisY.ScaleView.Size < YScaleView || double.IsNaN(chart1.ChartAreas[0].AxisY.ScaleView.Size))
            {
                chart1.ChartAreas[0].AxisY.ScaleView.Size = YScaleView;
            }
        }

        /// <summary>
        /// Chart缩放方法——按钮手动点击缩放
        /// </summary>
        /// <param name="isX">true表示X轴，false表示Y轴</param>
        /// <param name="isUp">true表示放大，false表示缩小</param>
        public void ZoomUpDownXY(bool isX, bool isUp)
        {
            Chart chart1 = dataChart.UIChart;

            //取一下X、Y轴的光标值
            double x = chart1.ChartAreas[0].CursorX.Position;
            double y = chart1.ChartAreas[0].CursorY.Position;

            //选择要操作的坐标轴
            Axis axis = null;
            //选择要用到的坐标值
            double cur = 0;
            if (isX)
            {
                cur = x;
                axis = chart1.ChartAreas[0].AxisX;
            }
            else
            {
                cur = y;
                axis = chart1.ChartAreas[0].AxisY;
            }

            if (axis == null)
                return;

            //判断一下是否点击了光标
            if (double.IsNaN(cur) == false)//若不为NaN，即用户点击了一个光标
            {
                #region 进行缩放

                #region 处理ScaleView为NaN的情况

                //把xSize、tSize、ySize计算出来，下面缩放要用
                GetXYSize(dataChart);

                //每当ZoomReset后，都需要zoom一次，否则ScaleView为NaN，无法变大变小
                if (double.IsNaN(axis.ScaleView.Size))
                {
                    if (axis == chart1.ChartAreas[0].AxisX)//X轴缩放
                    {
                        if (dataChart.isAxisXTime)
                            axis.ScaleView.Zoom(0, dataChart.tSize, DateTimeIntervalType.Milliseconds);
                        else
                            axis.ScaleView.Zoom(0, dataChart.xSize, DateTimeIntervalType.Auto);
                    }
                    else//Y轴缩放
                    {
                        //axis.ScaleView.Zoom(dataChart.YStartPosition, dataChart.ySize * 1.5, DateTimeIntervalType.Auto);
                        axis.ScaleView.Zoom(0, dataChart.ySize * 1.5, DateTimeIntervalType.Auto);
                    }
                }

                #endregion

                #region 当ScaleView不为NaN，就正常缩放

                //定义一个缩放倍数，放大时，它小于1，会让ScaleView变小；反之，大于1
                double zoomTimes = 1;
                if (isUp)
                    zoomTimes = 0.7;
                else
                    zoomTimes = 1.3;

                //视图是变化了，但我想让光标还在原来的位置
                axis.ScaleView.Size *= zoomTimes;

                //获取缩放时，ScaleView的开始位置
                double startLoc = axis.ScaleView.Position;

                //根据推导的公式，算出新的开始位置，以此为开始位置，可以让光标的相对位置不变
                double newLoc = cur + zoomTimes * (startLoc - cur);
                axis.ScaleView.Position = newLoc;

                //axis.ScaleView.Zoom(0, dataChart.xSize, DateTimeIntervalType.Auto); 

                #endregion

                #endregion
            }

        }

        #endregion

        #region 图上点显示信息相关的事件

        DataPoint dp = new DataPoint();

        /// <summary>
        /// //鼠标悬浮显示详情
        /// </summary>
        public void chart鼠标悬浮事件(object sender, ToolTipEventArgs e)
        {
            try
            {
                Chart chart1 = (Chart)sender;
                if (chart1.Cursor != Cursors.Cross)//在使用放大镜功能时，以下功能都失效
                {
                    if (e.HitTestResult.ChartElementType == ChartElementType.DataPoint)//如果鼠标下面是数据点，突出显示点，并显示信息
                    {
                        int i = e.HitTestResult.PointIndex;
                        dp = e.HitTestResult.Series.Points[i];
                        string seriesName = e.HitTestResult.Series.Name;
                        //dp.MarkerBorderColor = Color.Black;
                        try
                        {
                            if (dataChart.isAxisXTime)//如果横轴为时间
                            {
                                DateTime dt = DateTime.FromOADate(dp.XValue);//将double的xvalue转换为datetime形式
                                                                             //分别显示x轴和y轴的数值，其中{1:F2},表示显示的是float类型，精确到小数点后2位。  
                                e.Text = string.Format("{0}\n{1:0.000}\n" + seriesName, dt, dp.YValues[0]);
                            }
                            else
                                e.Text = string.Format("{0:0.000},{1:0.000}\n" + seriesName, dp.XValue, dp.YValues[0]);

                        }
                        catch (Exception)
                        {
                        }
                        chart1.Cursor = Cursors.Hand;//每次鼠标悬浮于点上时，将鼠标样式变成手，该点的边框变成黄色
                        dp.MarkerBorderColor = Color.Yellow;
                    }
                    else //如果鼠标下面不是数据点
                    {
                        chart1.Cursor = Cursors.Default;//每次鼠标悬浮于非点上时，将鼠标样式变成一般，上次悬浮的点的边框变成黑色
                        dp.MarkerBorderColor = Color.Black;


                        //if (f_Handle.check_Handle_Start.Checked == true || f_Handle.check_Handle_End.Checked == true || f_Delete.check_Dele_Start.Checked == true || f_Delete.check_Dele_End.Checked == true)
                        //    //如果鼠标下面不是数据点且正在选择开始结束时间
                        //    e.Text = "点击数据点以选择时间，右键取消";
                    }

                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// chart鼠标按下事件，里面可以判断是不是数据点
        /// </summary>
        public void chartMouseDown事件(object sender, MouseEventArgs e)//鼠标单机点显示详情
        {
            Chart chart1 = (Chart)sender;
            //MyChart myChart = DataChart;
            //switch (chart1.Tag)
            //{
            //    case "myChart1":
            //        myChart = DataChart;
            //        break;
            //}
            HitTestResult hit = chart1.HitTest(e.X, e.Y);//e本身没有e.HitTestResult.ChartElementType，靠这样获得引起事件的点
            if (hit.ChartElementType == ChartElementType.DataPoint)
            {
                try
                {
                    int i = hit.PointIndex;
                    DataPoint dp = hit.Series.Points[i];

                    //DateTime dt = DateTime.FromOADate(dp.XValue);//将double的xvalue转换为datetime形式


                    //分别显示x轴和y轴的数值，其中{1:F2},表示显示的是float类型，精确到小数点后2位。  
                    //MessageBox.Show(string.Format("时间:{0}\nCPS:{1:F2} ", dt, dp.YValues[0]));

                    #region 可以实现点一个加一个
                    //方法是新建，添加进某个容器中，不添加的话，只是有这个变量，却不显示；另外再放到最上层
                    //GroupBox newgb = new GroupBox();
                    //tabPage2.Controls.Add(newgb);
                    //newgb.BackColor = Color.White;
                    //newgb.Controls.Add(this.bt_详情关闭);
                    //newgb.Controls.Add(this.bt_SetEnd);
                    //newgb.Controls.Add(this.lb_info);
                    //lb_info.Text = string.Format("{0}\r\nCPS:{1:F0} ", dt, dp.YValues[0]);
                    //newgb.Controls.Add(this.bt_SetStart);
                    //newgb.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                    //newgb.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(134)));
                    //newgb.Location = new Point(e.X, e.Y + 110);
                    //newgb.Size = new Size(199, 168);
                    //newgb.TabIndex = 15;
                    //newgb.TabStop = false;
                    //newgb.Text = "详情";
                    //newgb.BringToFront();//放到最上层
                    //newgb.MouseDown += Gb_info_MouseDown;
                    #endregion


                    //gb_info.Visible = true;
                    //lb_info.Text = string.Format("{0}\r\nCPS:{1:0.000} ", dt, dp.YValues[0]);
                    //gb_info.Location = new Point(e.X - 54, e.Y + 90);
                    //gb_info.MouseDown += Gb_info_MouseDown;
                }
                catch (Exception)
                {
                }
            }

        }

        //public void Gb_info_MouseDown(object sender, MouseEventArgs e)//右键关闭详情框
        //{
        //    if (e.Button == MouseButtons.Right)
        //    {
        //        GroupBox gb1 = (GroupBox)sender;
        //        gb1.Visible = false;
        //    }
        //}
        //public void bt_详情关闭_Click(object sender, EventArgs e)//关闭详情框
        //{
        //    gb_info.Visible = false;
        //}

        #endregion

        #region 画图

        /*画图经验总结
         * 画图一定要让CSs的Xs、Ys同步更新，这两个list会用于缩放功能
         * 可以单个加点，但直接绑定也问题不大。绑定的时候很统一，那就是某个CSs自己绑定自己——CS的series的Points绑定到CS的Xs、Ys
         */

        /// <summary>
        /// 给一个CS添加一个数据点
        /// </summary>
        /// <param name="CS"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void AddPointToCS(int CSIndex, double x, double y)
        {
            ChartSeries CS = dataChart.CSs[CSIndex];
            CS.Xs.Add(x);
            CS.Ys.Add(y);
        }

        /// <summary>
        /// 对一个myChart对象的ChartSeries进行操作，让其series绑定到自己的Xs、Ys上
        /// </summary>
        /// <param name="CS">想要更新的series</param>
        /// <param name="isAxisTime">横轴是否为时间轴</param>
        public void RefreshSeries(int CSIndex)
        {
            ChartSeries CS = dataChart.CSs[CSIndex];
            if (dataChart.isAxisXTime)
                CS.series.Points.DataBindXY(CS.Ts, CS.Ys);//Series自己和自己对应的Xs、Ys绑定
            else
                CS.series.Points.DataBindXY(CS.Xs, CS.Ys);//Series自己和自己对应的Xs、Ys绑定
        }

        /// <summary>
        /// 给某个ChartSeries对象添加一个点
        /// </summary>
        public void AddOnePoint(int CSIndex, double x, double y)
        {
            ChartSeries CS = dataChart.CSs[CSIndex];
            CS.Xs.Add(x);
            CS.Ys.Add(y);
            CS.series.Points.AddXY(x, y);
        }
        public void AddOnePoint(int CSIndex, DateTime t, double y)
        {
            ChartSeries CS = dataChart.CSs[CSIndex];
            CS.Ts.Add(t);
            CS.Ys.Add(y);
            CS.series.Points.AddXY(t, y);
        }

        /// <summary>
        /// 给某个ChartSeries对象添加多个点
        /// </summary>
        public void AddPoints(int CSIndex, List<double> x, List<double> y)
        {
            ChartSeries CS = dataChart.CSs[CSIndex];
            CS.Xs.AddRange(x);
            CS.Ys.AddRange(y);
            CS.series.Points.DataBindXY(CS.Xs, CS.Ys);
        }
        public void AddPoints(int CSIndex, List<DateTime> t, List<double> y)
        {
            ChartSeries CS = dataChart.CSs[CSIndex];
            CS.Ts.AddRange(t);
            CS.Ys.AddRange(y);
            CS.series.Points.DataBindXY(CS.Ts, CS.Ys);
        }

        /// <summary>
        /// 给一个series绑定数据。
        /// 外来的数据是谁都无所谓，最终绑定的还是CS中的list。
        /// </summary>
        public void BindPoints(int CSIndex, List<double> x, List<double> y)
        {
            ChartSeries CS = dataChart.CSs[CSIndex];
            CS.Xs.Clear();
            CS.Ys.Clear();
            CS.Xs.AddRange(x);
            CS.Ys.AddRange(y);
            CS.series.Points.DataBindXY(CS.Xs, CS.Ys);
        }
        public void BindPoints(int CSIndex, List<DateTime> t, List<double> y)
        {
            ChartSeries CS = dataChart.CSs[CSIndex];
            CS.Ts.Clear();
            CS.Ys.Clear();
            CS.Ts.AddRange(t);
            CS.Ys.AddRange(y);
            CS.series.Points.DataBindXY(CS.Ts, CS.Ys);
        }

        /// <summary>
        /// 清除某个ChartSeries对象的所有数据点
        /// </summary>
        public void ClearPoints(int CSIndex)
        {
            ChartSeries CS = dataChart.CSs[CSIndex];
            CS.Xs.Clear();
            CS.Ts.Clear();
            CS.Ys.Clear();
            CS.series.Points.Clear();
        }

        /// <summary>
        /// 让某个ChartSeries对象的所有数据点的Y值为0
        /// </summary>
        public void ClearPointsData(int CSIndex)
        {
            ChartSeries CS = dataChart.CSs[CSIndex];
            double[] ys = new double[CS.Ys.Count];
            CS.Ys.Clear();
            CS.Ys.AddRange(ys);//把Y值都清零

            if (dataChart.isAxisXTime)
                CS.series.Points.DataBindXY(CS.Ts, CS.Ys);
            else
                CS.series.Points.DataBindXY(CS.Xs, CS.Ys);
        }

        /// <summary>
        /// 清除所有ChartSeries对象的所有数据点
        /// </summary>
        public void ClearPointsOfAllCS()
        {
            foreach (var CS in dataChart.CSs)
            {
                CS.Xs.Clear();
                CS.Ts.Clear();
                CS.Ys.Clear();
                CS.series.Points.Clear();
            }
        }

        /// <summary>
        /// 从text文件中读取数据，再绑定到某个chart的某个series上
        /// 先在Excel准备两列数据，再全部复制到text文件中
        /// </summary>
        /// <param name="path">text文件的路径</param>
        /// <param name="series">要把数据绑定到的series</param>
        public void ReadData(string path, Series series)
        {
            //path = "D:\\12644\\Desktop\\数据1.txt";
            string[] strs = File.ReadAllLines(path);

            List<double> Xs = new List<double>();
            List<double> Ys = new List<double>();
            for (int i = 0; i < strs.Length; i++)
            {
                string[] strings = strs[i].Split(new string[] { "\t", " " }, StringSplitOptions.None);
                Xs.Add(double.Parse(strings[0]));
                Ys.Add(double.Parse(strings[1]));
            }
            //把数据画到图上两种方法，一是放到X、Y中，再一次性地DataBindXY；二是对Points进行Add操作
            //this.chart1.Series[0].Points.DataBindXY(Xs, Ys);
            series.Points.DataBindXY(Xs, Ys);
        }

        /// <summary>
        /// 绘制图表时触发的事件
        /// </summary>
        private void Chart1_PostPaint(object sender, ChartPaintEventArgs e)
        {
            if (dataChart.IsAdjustPointsWhenFirstDraw)
            {
                Chart调整点数量(dataChart);
            }

        }

        #endregion

        #region 对于WindowsFormsHost导致的放缩问题解决

        //解决方法就是重新设置一下WindowsFormsHost的宽高

        /// <summary>
        /// 重置一下WindowsFormsHost的尺寸，让它适应周围
        /// </summary>
        public void ResetWindowsFormsHostSize(WindowsFormsHost w)
        {
            //先设置成最小
            w.Width = 0;
            w.Height = 0;

            //中间还得等一小会儿
            delay(5);

            //再设置成自动，就能自适应了
            w.Width = double.NaN;
            w.Height = double.NaN;
        }

        private void delay(int n)
        {
            //WPF方法：（Winform也能用，只不过“System.Windows.Forms.”是多余的）
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < n)
            { System.Windows.Forms.Application.DoEvents(); }

        }

        #endregion

        /// <summary>
        /// 使用示例
        /// </summary>
        void example()
        {
            Chart chart0 = new Chart();
            Chart chart1 = new Chart();

            //确定chart控件对象
            AndyChart ac0 = new AndyChart(chart0);

            //配置一些属性：
            ac0.dataChart.canCaiYangPinLvChange = true;
            ac0.dataChart.isAxisXTime = true;

            //正式初始化
            ac0.init();

            //根据需求添加Series
            ac0.AddSeries(ac0.dataChart, "series2", System.Drawing.Color.AliceBlue, SeriesChartType.Point);


            AndyChart ac1 = new AndyChart(chart1);
            ac1.init();
            List<double> X, Y;
            X = new List<double>();
            Y = new List<double>();
            for (int i = 0; i < 10; i++)
            {
                X.Add(i);
                Y.Add(i * 2 + 1);
            }

            //根据series名称，给某个series添加数据
            string seriesName = "1612";
            chart1.Series.Add(new Series(seriesName));
            chart1.Series.FindByName(seriesName).Points.DataBindXY(X, Y);
            chart1.Series[1].Points.ToList();
        }

        /*注意事项：
         使用时，需要添加引用：System.Windows.Forms、System.Windows.Forms.DataVisualization、System.Drawing、WindowsFormsIntegration
        XAML中，不能直接定义Chart控件，需要：
        <WindowsFormsHost Margin="2">
                <Charting:Chart x:Name="chart_NowData"/>
            </WindowsFormsHost>
        其中，xmlns:Charting="clr-namespace:System.Windows.Forms.DataVisualization.Charting;assembly=System.Windows.Forms.DataVisualization"
         */
    }
}
