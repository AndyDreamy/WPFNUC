using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using 核素识别仪.其他功能类;
using static 核素识别仪.拟合助手_WPF.W_Fitting;
//using 拟合助手_WPF.其他功能类;
//using static 拟合助手_WPF.W_Fitting;

namespace 核素识别仪.拟合助手_WPF
{
    /// <summary>
    /// W_FittingChart.xaml 的交互逻辑
    /// </summary>
    public partial class W_FittingChart : Window
    {
        #region 公共


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private W_Fitting father;
        /// <summary>
        /// 主界面的实例
        /// </summary>
        public W_Fitting Father
        {
            get { return father; }
            set { father = value; }
        }


        #endregion

        public W_FittingChart()
        {
            InitializeComponent();
            init_Chart();

            //放大界面
            MainWindow.Instance.adc.ZoomWindow(this, 1.25);
        }

        #region Chart相关


        public AndyChart andyChart;

        /// <summary>
        /// 初始化chart相关
        /// </summary>
        void init_Chart()
        {
            //确定chart控件对象
            andyChart = new AndyChart(chart_FittingResult);

            //配置一些属性：
            andyChart.dataChart.canCaiYangPinLvChange = false;
            andyChart.dataChart.isAxisXTime = false;

            //正式初始化
            andyChart.init();

            //关闭图例
            andyChart.dataChart.UIChart.Legends[0].Enabled = false;

            //设置Series
            andyChart.ClearAllSeries(andyChart.dataChart);
            andyChart.AddSeries(andyChart.dataChart, "拟合曲线", System.Drawing.Color.DodgerBlue, SeriesChartType.Line);
            andyChart.dataChart.CSs[0].series.MarkerSize = 0;//设置为只有线没有点
            andyChart.AddSeries(andyChart.dataChart, "数据点", System.Drawing.Color.Red, SeriesChartType.Point);
            andyChart.AddSeries(andyChart.dataChart, "验算点", System.Drawing.Color.Orange, SeriesChartType.Point);
            andyChart.dataChart.CSs[2].series.MarkerStyle = MarkerStyle.Diamond;
        }

        #endregion

        /// <summary>
        /// 选择拟合段画图事件
        /// </summary>
        private void cb_ResultNum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_ResultNum.SelectedIndex < 0)
                return;

            //get主界面的拟合结果数据
            FittingResult fr = father.FittingResults[cb_ResultNum.SelectedIndex];

            //清除之前的所有点
            andyChart.ClearPointsOfAllCS();

            //画拟合曲线
            //获得X轴范围
            double xMax, xMin;
            xMax = fr.Xs.Max();
            xMin = fr.Xs.Min();
            //添加点
            double d = (xMax - xMin) / 500;//偏移
            for (int i = 0; i < 500; i++)
            {
                double x = xMin + d * i;
                andyChart.AddOnePoint(0, x, fr.A * x * x + fr.B * x + fr.C);
            }

            //画数据原始点
            andyChart.AddPoints(1, fr.Xs, fr.Ys_Ori);

            //画验算点
            andyChart.AddPoints(2, fr.Xs, fr.Ys_Cal);
        }
    }
}
