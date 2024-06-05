using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace 核素识别仪.资源
{
    class AndyROI
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
            //tb_Note.Text = sb.ToString();
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


    /// <summary>
    /// 曲线拟合相关的功能类
    /// </summary>
    class FittingFuntion
    {

        /// <summary>
        /// 拟合功能种类
        /// </summary>
        public enum FittingType
        {
            一次拟合,
            二次拟合,
            对数拟合,
            幂级数拟合,
            指数拟合,
        }

        /// <summary>
        /// 根据x、y点拟合各种曲线
        /// </summary>
        /// <param name="x">存储x点的数组</param>
        /// <param name="y">存储y点的数组</param>
        /// <param name="function">1~5分别表示：一次拟合、二次拟合、对数拟合、幂级数拟合、指数拟合</param>
        public static double[] FittingCurve(double[] x, double[] y, FittingType type)
        {//string[] args

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            double[] result = new double[] { };//存储拟合结果的数组
            double[] yy = new double[y.Length];

            switch (type)
            {
                case FittingType.一次拟合:
                    #region 一次拟合
                    sw.Start();
                    result = Linear(y, x);
                    sw.Stop();
                    #endregion
                    break;
                case FittingType.二次拟合:
                    #region 二次拟合
                    //tbtb1.Text += "\r\n" + ("二次拟合：");
                    sw.Start();
                    result = TowTimesCurve(y, x);
                    sw.Stop();

                    //foreach (double num in result)
                    //{
                    //    //tbtb1.Text += "\r\n" + (num);
                    //}
                    //for (int i = 0; i < x.Length; i++)
                    //{
                    //    yy[i] = result[0] + result[1] * x[i] + result[2] * x[i] * x[i];
                    //}
                    //tbtb1.Text += "\r\n" + ("R²=: " + Pearson(y, yy) + "\r\n");
                    ////tbtb1.Text+= "\r\n"+("二次拟合计算时间：");
                    ////tbtb1.Text+= "\r\n"+(sw.ElapsedMilliseconds);
                    #endregion
                    break;
                case FittingType.对数拟合:
                    #region 对数拟合
                    //tbtb1.Text += "\r\n" + ("对数拟合计算时间：");
                    sw.Start();
                    result = LOGEST(y, x);
                    sw.Stop();

                    foreach (double num in result)
                    {

                        //tbtb1.Text += "\r\n" + (num);
                    }
                    for (int i = 0; i < x.Length; i++)
                    {

                        yy[i] = result[1] * Math.Log10(x[i]) + result[0];
                    }
                    //tbtb1.Text += "\r\n" + ("R²=: " + Pearson(y, yy) + "\r\n");
                    ////tbtb1.Text+= "\r\n"+("对数拟合计算时间：");
                    ////tbtb1.Text+= "\r\n"+(sw.ElapsedMilliseconds);
                    #endregion
                    break;
                case FittingType.幂级数拟合:
                    #region 幂级数拟合
                    //tbtb1.Text += "\r\n" + ("幂级数拟合：");
                    sw.Start();
                    result = PowEST(y, x);
                    sw.Stop();

                    foreach (double num in result)
                    {

                        //tbtb1.Text += "\r\n" + (num);
                    }
                    for (int i = 0; i < x.Length; i++)
                    {

                        yy[i] = result[0] * Math.Pow(x[i], result[1]);
                    }
                    //tbtb1.Text += "\r\n" + ("R²=: " + Pearson(y, yy) + "\r\n");
                    ////tbtb1.Text+= "\r\n"+("幂级数拟合计算时间：");
                    ////tbtb1.Text+= "\r\n"+(sw.ElapsedMilliseconds);
                    #endregion
                    break;
                case FittingType.指数拟合:
                    #region 指数拟合
                    //tbtb1.Text += "\r\n" + ("指数函数拟合：");
                    sw.Start();
                    result = IndexEST(y, x);
                    sw.Stop();
                    foreach (double num in result)
                    {

                        //tbtb1.Text += "\r\n" + (num);
                    }
                    for (int i = 0; i < x.Length; i++)
                    {

                        yy[i] = result[0] * Math.Exp(x[i] * result[1]);
                    }
                    //tbtb1.Text += "\r\n" + ("R²=: " + Pearson(y, yy));
                    ////tbtb1.Text+= "\r\n"+("指数函数拟合计算时间：");
                    ////tbtb1.Text+= "\r\n"+(sw.ElapsedMilliseconds);

                    //Console.ReadKey();
                    #endregion
                    break;
                default:
                    break;
            }

            return result;
        }

        #region 基本方法

        #region 多项式拟合函数,输出系数是y=a0+a1*x+a2*x*x+.........，按a0,a1,a2输出
        static public double[] Polyfit(double[] y, double[] x, int order)
        {

            double[,] guass = Get_Array(y, x, order);

            double[] ratio = Cal_Guass(guass, order + 1);

            return ratio;
        }
        #endregion

        #region 一次拟合函数，y=a0+a1*x,输出次序是a0,a1

        /// <summary>
        /// 
        /// </summary>
        /// <param name="y"></param>
        /// <param name="x"></param>
        /// <returns> k = result[1];b = result[0];</returns>
        static public double[] Linear(double[] y, double[] x)
        {

            double[] ratio = Polyfit(y, x, 1);
            return ratio;
        }
        #endregion

        #region 一次拟合函数，截距为0，y=a0x,输出次序是a0
        static public double[] LinearInterceptZero(double[] y, double[] x)
        {

            double divisor = 0; //除数
            double dividend = 0; //被除数
            for (int i = 0; i < x.Length; i++)
            {

                divisor += x[i] * x[i];
                dividend += x[i] * y[i];
            }
            if (divisor == 0)
            {
                throw (new Exception("除数不为0！"));
                //return null;
            }
            return new double[] { dividend / divisor };

        }
        #endregion

        #region 二次拟合函数，y=a0+a1*x+a2x²,输出次序是a0,a1,a2

        /// <summary>
        /// 
        /// </summary>
        /// <param name="y"></param>
        /// <param name="x"></param>
        /// <returns>a = result[2];b = result[1];c = result[0];</returns>
        static public double[] TowTimesCurve(double[] y, double[] x)
        {

            double[] result = Polyfit(y, x, 2);
            return result;
        }

        #endregion

        #region 对数拟合函数,.y= c*(ln x)+b,输出为b,c
        static public double[] LOGEST(double[] y, double[] x)
        {

            double[] lnX = new double[x.Length];

            for (int i = 0; i < x.Length; i++)
            {

                if (x[i] == 0 || x[i] < 0)
                {
                    throw (new Exception("正对非正数取对数！"));
                    //return null;
                }
                lnX[i] = Math.Log(x[i]);
            }

            return Linear(y, lnX);
        }
        #endregion

        #region 幂函数拟合模型, y=c*x^b,输出为c,b
        static public double[] PowEST(double[] y, double[] x)
        {

            double[] lnX = new double[x.Length];
            double[] lnY = new double[y.Length];
            double[] dlinestRet;

            for (int i = 0; i < x.Length; i++)
            {

                lnX[i] = Math.Log(x[i]);
                lnY[i] = Math.Log(y[i]);
            }

            dlinestRet = Linear(lnY, lnX);

            dlinestRet[0] = Math.Exp(dlinestRet[0]);

            return dlinestRet;
        }
        #endregion

        #region 指数函数拟合函数模型，公式为 y=c*m^x;输出为 c,m
        static public double[] IndexEST(double[] y, double[] x)
        {

            double[] lnY = new double[y.Length];
            double[] ratio;
            for (int i = 0; i < y.Length; i++)
            {

                lnY[i] = Math.Log(y[i]);
            }

            ratio = Linear(lnY, x);
            for (int i = 0; i < ratio.Length; i++)
            {

                if (i == 0)
                {

                    ratio[i] = Math.Exp(ratio[i]);
                }
            }
            return ratio;
        }
        #endregion

        #region 相关系数R²部分
        public static double Pearson(IEnumerable<double> dataA, IEnumerable<double> dataB)
        {

            int n = 0;
            double r = 0.0;

            double meanA = 0;
            double meanB = 0;
            double varA = 0;
            double varB = 0;
            int ii = 0;
            using (IEnumerator<double> ieA = dataA.GetEnumerator())
            using (IEnumerator<double> ieB = dataB.GetEnumerator())
            {

                while (ieA.MoveNext())
                {

                    if (!ieB.MoveNext())
                    {

                        //throw new ArgumentOutOfRangeException("dataB", Resources.ArgumentArraysSameLength);
                    }
                    ii++;
                    //Console.WriteLine("FF00::  " + ii + " --  " + meanA + " -- " + meanB + " -- " + varA + "  ---  " + varB);
                    double currentA = ieA.Current;
                    double currentB = ieB.Current;

                    double deltaA = currentA - meanA;
                    double scaleDeltaA = deltaA / ++n;

                    double deltaB = currentB - meanB;
                    double scaleDeltaB = deltaB / n;

                    meanA += scaleDeltaA;
                    meanB += scaleDeltaB;

                    varA += scaleDeltaA * deltaA * (n - 1);
                    varB += scaleDeltaB * deltaB * (n - 1);
                    r += (deltaA * deltaB * (n - 1)) / n;
                    //Console.WriteLine("FF00::  " + ii + " --  " + meanA + " -- " + meanB + " -- " + varA + "  ---  " + varB);
                }

                if (ieB.MoveNext())
                {

                    //throw new ArgumentOutOfRangeException("dataA", Resources.ArgumentArraysSameLength);
                }
            }
            return (r / Math.Sqrt(varA * varB)) * (r / Math.Sqrt(varA * varB));
        }
        #endregion

        #region 最小二乘法部分

        #region 计算增广矩阵
        static private double[] Cal_Guass(double[,] guass, int count)
        {

            double temp;
            double[] x_value;

            for (int j = 0; j < count; j++)
            {

                int k = j;
                double min = guass[j, j];

                for (int i = j; i < count; i++)
                {

                    if (Math.Abs(guass[i, j]) < min)
                    {

                        min = guass[i, j];
                        k = i;
                    }
                }

                if (k != j)
                {

                    for (int x = j; x <= count; x++)
                    {

                        temp = guass[k, x];
                        guass[k, x] = guass[j, x];
                        guass[j, x] = temp;
                    }
                }

                for (int m = j + 1; m < count; m++)
                {

                    double div = guass[m, j] / guass[j, j];
                    for (int n = j; n <= count; n++)
                    {

                        guass[m, n] = guass[m, n] - guass[j, n] * div;
                    }
                }

                /* System.Console.WriteLine("初等行变换：");
                 for (int i = 0; i < count; i++)
                 {
                     for (int m = 0; m < count + 1; m++)
                     {
                         System.Console.Write("{0,10:F6}", guass[i, m]);
                     }
                     Console.WriteLine();
                 }*/
            }
            x_value = Get_Value(guass, count);

            return x_value;

            /*if (x_value == null)
                Console.WriteLine("方程组无解或多解！");
            else
            {
                foreach (double x in x_value)
                {
                    Console.WriteLine("{0:F6}", x);
                }
            }*/
        }

        #endregion

        #region 回带计算X值
        static private double[] Get_Value(double[,] guass, int count)
        {

            double[] x = new double[count];
            double[,] X_Array = new double[count, count];
            int rank = guass.Rank;//秩是从0开始的

            for (int i = 0; i < count; i++)
                for (int j = 0; j < count; j++)
                    X_Array[i, j] = guass[i, j];

            if (X_Array.Rank < guass.Rank)//表示无解
            {

                return null;
            }

            if (X_Array.Rank < count - 1)//表示有多解
            {

                return null;
            }
            //回带计算x值
            x[count - 1] = guass[count - 1, count] / guass[count - 1, count - 1];
            for (int i = count - 2; i >= 0; i--)
            {

                double temp = 0;
                for (int j = i; j < count; j++)
                {

                    temp += x[j] * guass[i, j];
                }
                x[i] = (guass[i, count] - temp) / guass[i, i];
            }

            return x;
        }
        #endregion

        #region  得到数据的法矩阵,输出为发矩阵的增广矩阵
        static private double[,] Get_Array(double[] y, double[] x, int n)
        {

            double[,] result = new double[n + 1, n + 2];

            if (y.Length != x.Length)
            {

                throw (new Exception("两个输入数组长度不一！"));
                //return null;
            }

            for (int i = 0; i <= n; i++)
            {

                for (int j = 0; j <= n; j++)
                {

                    result[i, j] = Cal_sum(x, i + j);
                }
                result[i, n + 1] = Cal_multi(y, x, i);
            }

            return result;
        }

        #endregion

        #region 累加的计算
        static private double Cal_sum(double[] input, int order)
        {

            double result = 0;
            int length = input.Length;

            for (int i = 0; i < length; i++)
            {

                result += Math.Pow(input[i], order);
            }

            return result;
        }
        #endregion

        #region 计算∑(x^j)*y
        static private double Cal_multi(double[] y, double[] x, int order)
        {

            double result = 0;

            int length = x.Length;

            for (int i = 0; i < length; i++)
            {

                result += Math.Pow(x[i], order) * y[i];
            }

            return result;
        }
        #endregion

        #endregion 

        #endregion
    }
}
