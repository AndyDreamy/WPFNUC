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
using 核素识别仪.通用功能类;

namespace 核素识别仪.小窗口
{
    public partial class W_ROIResult : Window
    {

        #region 数据

        /// <summary>
        /// 用来表征是否正在进行一次ROI计算
        /// </summary>
        public bool isDoingROI = false;

        /// <summary>
        /// 用来表征是否显示ROI分析辅助线（本底线、减本底值、高斯拟合值等）
        /// </summary>
        private bool isShowROILines = false;

        private int left;
        /// <summary>
        /// ROI区域左道址
        /// </summary>
        public int P_left
        {
            get { return left; }
            set { left = value; }
        }

        private int right;
        /// <summary>
        /// ROI区域右道址
        /// </summary>
        public int P_right
        {
            get { return right; }
            set { right = value; }
        }


        #endregion

        #region 构造器
        public W_ROIResult()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
        #endregion

        #region 控件事件

        /// <summary>
        /// 计算ROI按钮
        /// </summary>
        private void bt_CalculateROI_Click(object sender, RoutedEventArgs e)
        {
            CalculateROI();
        }

        /// <summary>
        /// 选择区域按钮
        /// </summary>
        private void bt_SelectROI_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.andyChart.Chart无缩放框选();
            isDoingROI = true;
        }

        #endregion

        /// <summary>
        /// 计算ROI的方法
        /// </summary>
        /// <param name="left">框选的左边界值</param>
        /// <param name="right">框选的右边界值</param>
        public void CalculateROI()
        {
            Console.WriteLine("开始计算一次ROI");

            //计算数据总个数
            int count = right - left + 1;

            //对总数有个下限限制
            if (count < 5)
                return;

            //生成XY数组，用于拟合
            double[] Xs = new double[count];
            double[] Ys = new double[count];//平滑后的数据
            double[] Ys_Ori = new double[count];//最原始的数据

            //从多道数据摘取ROI区域的数据
            for (int i = left; i <= right; i++)
            {
                Xs[i - left] = i;
                Ys[i - left] = MainWindow.Instance.receDatas.MultiDatas_Smooth[i - 1];//这里的i是道址，取数据减个1

                //这里记录滤波前的原始数据
                Ys_Ori[i - left] = MainWindow.Instance.receDatas.MultiDatas[i - 1];

                //应该来说，用原始数据和平滑数据效果差不多
                //这里就应该用平滑后的数据进行处理，如果用平滑前参差不齐的数据，在扣完本底后会出现脉冲数为负值

            }

            //取一下所选范围数据的总计数，存到sum_All
            double sum_All = 0;
            //double sum_All_Ori = 0;
            for (int i = 0; i < Ys.Length; i++)
            {
                //sum_All += Ys[i];
                sum_All += Ys_Ori[i];
                //sum_All_Ori += Ys_Ori[i];
            }
            double cps_All = sum_All / MainWindow.Instance.receDatas.P_measuredTime;

            //计算扣除本底后的数据，存在Ys_SubBG
            double[] Ys_SubBG = SubtractBackground(Xs, Ys);

            //取一下所选范围数据的净计数，存到sum_Net
            double sum_Net = 0;
            for (int i = 0; i < Ys.Length; i++)
            {
                sum_Net += Ys_SubBG[i];
            }
            double cps_Net = sum_Net / MainWindow.Instance.receDatas.P_measuredTime;

            #region 这个数据，由于人手选的范围，扣除本底后两边可能会出现负值，这里要截取到最后一个大于零的数据

            //下面两个list记录小于等于0的数据的index
            List<int> indexes_Left = new List<int>();//在峰位左侧的
            List<int> indexes_Right = new List<int>();//在峰位右侧的
            for (int i = 0; i < Ys_SubBG.Length; i++)
            {
                if (Ys_SubBG[i] <= 0)
                {
                    if (i < (Ys_SubBG.Length / 2))//如果index在中点左边
                        indexes_Left.Add(i);
                    else//如果index在中点右边
                        indexes_Right.Add(i);
                }
            }

            //index可能很多个，我们要找，左侧的最大index和右侧的最小index 
            int leftMax = 0;
            int rightMin = Ys_SubBG.Length - 1;
            if (indexes_Left.Count > 0)
                leftMax = indexes_Left.Max();
            if (indexes_Right.Count > 0)
                rightMin = indexes_Right.Min();

            //取正数部分的数据
            List<double> Xs_Zheng = new List<double>();
            List<double> Ys_Zheng = new List<double>();
            for (int i = leftMax + 1; i <= rightMin - 1; i++)
            {
                Xs_Zheng.Add(Xs[i]);
                Ys_Zheng.Add(Ys_SubBG[i]);
            }
            //就用截取出来的正数部分数据进行高斯拟合

            #endregion

            //高斯拟合
            double[] res = FittingGauss(Xs_Zheng.ToArray(), Ys_Zheng.ToArray());//就用截取出来的正数部分数据进行高斯拟合
            double A = res[0];
            double u = res[1];
            double sigma = res[2];
            double[] Ys_Cal = new double[Ys.Length];//根据拟合结果算出的Y值
            for (int i = 0; i < Ys_Cal.Length; i++)
            {
                //公式：y=A*exp(-(x-u)^2/(2*sigma^2))
                Ys_Cal[i] = A * Math.Exp(-1 * Math.Pow(Xs[i] - u, 2) / (2 * sigma * sigma));
            }

            //画图显示一下
            if (isShowROILines)
            {
                MainWindow.Instance.andyChart.BindPoints(2, Xs.ToList(), Ys_Cal.ToList());//拟合高斯曲线
                MainWindow.Instance.andyChart.BindPoints(3, Xs.ToList(), Ys.ToList());//未处理过的原数据曲线
                MainWindow.Instance.andyChart.BindPoints(4, Xs.ToList(), Ys_SubBG.ToList());//原数据减本底的曲线
            }

            //计算半高宽
            double FWHM = 2 * Math.Sqrt(2 * Math.Log(2)) * sigma;

            //峰位就取u，分辨率：
            double resolutionRatio = FWHM / u;

            #region 计算完毕，UI显示结果

            //准备结果显示
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ROI结果：");
            sb.AppendLine("峰位：" + Math.Round(u, 2));
            sb.AppendLine("半高宽：" + Math.Round(FWHM, 2));
            sb.AppendLine("分辨率：" + Math.Round(resolutionRatio, 4) * 100 + "%");
            sb.AppendLine("总计数：" + Math.Round(sum_All, 2));
            sb.AppendLine("净计数：" + Math.Round(sum_Net, 2));
            sb.AppendLine("总计数率：" + Math.Round(cps_All, 2));
            sb.AppendLine("净计数率：" + Math.Round(cps_Net, 2));

            //显示ROI界面
            tb_Note.Text = sb.ToString();
            //adc.OpenOneWindow(w_ROIResult);
            isDoingROI = false;

            #endregion

            //这一步可以去除框线的痕迹
            //chart_Putu.ChartAreas[0].CursorX.SetSelectionPosition(0, 0);
        }

        /// <summary>
        /// 高斯函数拟合方法
        /// </summary>
        /// <returns>数组从低到高依次是参数：A,u,sigma</returns>
        public double[] FittingGauss(double[] Xs, double[] Ys)
        {
            //将Y值取ln
            double[] Ys_ln = new double[Ys.Length];
            for (int i = 0; i < Ys.Length; i++)
            {
                //这里如果Y值为0，就会出现取ln得到NaN
                if (Ys[i] == 0)
                    Ys_ln[i] = Math.Log(0.1);
                else
                    Ys_ln[i] = Math.Log(Ys[i]);
            }

            //以ln(y)值和x值作为拟合数据点，进行二次拟合，得到abc
            double[] result = FittingFuntion.FittingCurve(Xs, Ys_ln, FittingFuntion.FittingType.二次拟合);
            double a, b, c;
            a = result[2]; b = result[1]; c = result[0];

            //根据得到的二次函数因子，算出高斯函数因子:y=A*exp(-(x-u)^2/(2*sigma^2))
            double A;//高度
            double u;//均值
            double sigma;//方差
            A = Math.Exp(c - (b * b) / (4 * a));
            sigma = Math.Sqrt(-1 / a / 2);
            u = -b / 2 / a;

            //返回高斯函数的因子
            double[] res = new double[]
            {
                A,u,sigma
            };
            return res;
        }

        /// <summary>
        /// ROI区数据扣除本底方法。就是两段连线往下的计数减掉即可。
        /// </summary>
        /// <returns>返回扣除本底后的数据。若失败，返回空数组</returns>
        public double[] SubtractBackground(double[] Xs, double[] Ys)
        {
            int count = Xs.Length;

            if (count <= 0)
                return new double[] { };

            //获取左右两个点的坐标
            Point pl = new Point(Xs[0], Ys[0]);
            Point pr = new Point(Xs[count - 1], Ys[count - 1]);

            //这两个点连成直线，得到一次函数，计算k b
            double k = (pr.Y - pl.Y) / (pr.X - pl.X);
            double b = pl.Y - k * pl.X;

            //本底数据
            double[] Ys_BG = new double[count];
            for (int i = 0; i < count; i++)
            {
                Ys_BG[i] = k * Xs[i] + b;
            }

            //画图看一下
            if (isShowROILines)
                MainWindow.Instance.andyChart.BindPoints(5, Xs.ToList(), Ys_BG.ToList());

            //对Ys处理，遍历，依次减去相应位置的本底数据，本底数据为X值代入一次函数算出的Y值
            double[] Ys_SubBG = new double[Ys.Length];//扣除本底的Y值数组
            for (int i = 0; i < Ys.Length; i++)
            {
                Ys_SubBG[i] = Ys[i] - Ys_BG[i];

                ////可能减出负值
                //if(Ys_SubBG[i]<0&&)
            }

            //减去本底后，第一个值和最后一个值会为0，这样在高斯拟合时，去ln会出问题，所以这里做近似处理，这两个值等于其相邻的值
            Ys_SubBG[0] = Ys_SubBG[1];
            Ys_SubBG[count - 1] = Ys_SubBG[count - 2];

            return Ys_SubBG;
        }
    }
}
