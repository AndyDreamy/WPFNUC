using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 核素识别仪.其他功能类
{
    /// <summary>
    /// 更新日期：2023年7月6日15:58:42
    /// </summary>
    public class AndySeekPeak : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private double minHeight = 4;
        /// <summary>
        /// 最小峰高
        /// </summary>
        public double P_minHeight
        {
            get { return minHeight; }
            set
            {
                minHeight = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_minHeight"));
                }
            }
        }

        private double maxWidth = 300d;
        /// <summary>
        /// 最大峰宽
        /// </summary>
        public double P_maxWidth
        {
            get { return maxWidth; }
            set
            {
                maxWidth = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_maxWidth"));
                }
            }
        }

        private double smallRatio = 0.4;
        /// <summary>
        /// 缩小比例，是指在平滑曲线后，某个峰位的最大值与这个峰范围内的原始数据最大值的比值。
        /// 这个值如果太小，就说明是那种突然冒出一个大的数据，而旁边数据很小的毛刺，这种平滑出来的峰需要剔除。
        /// </summary>
        public double P_smallRatio
        {
            get { return smallRatio; }
            set
            {
                smallRatio = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_smallRatio"));
                }
            }
        }

        private int juanNum = 10;
        /// <summary>
        /// 卷积向两边计算的数据个数
        /// </summary>
        public int P_juanNum
        {
            get { return juanNum; }
            set
            {
                juanNum = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_juanNum"));
                }
            }
        }

        private double sigma = 6;
        /// <summary>
        /// 高斯函数参数σ
        /// </summary>
        public double P_sigma
        {
            get { return sigma; }
            set
            {
                if (value != 0)
                    sigma = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_sigma"));
                }
            }
        }

        private int smoothTimes = 2;
        /// <summary>
        /// 进行平滑（高斯滤波）的次数
        /// </summary>
        public int P_smoothTimes
        {
            get { return smoothTimes; }
            set
            {
                smoothTimes = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_smoothTimes"));
                }
            }
        }

        private bool whichSmooth = true;
        /// <summary>
        /// true为高斯平滑，false为线性平滑
        /// </summary>
        public bool P_whichSmooth
        {
            get { return whichSmooth; }
            set
            {
                whichSmooth = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_whichSmooth"));
                }
            }
        }

        private bool ifShowSmooth = false;
        /// <summary>
        /// 谱图是否显示平滑后的曲线。true为显示平滑后曲线，false为显示原始数据
        /// </summary>
        public bool P_ifShowSmooth
        {
            get { return ifShowSmooth; }
            set
            {
                ifShowSmooth = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_ifShowSmooth"));
                }

                //画一次图
                MainWindow.Instance.DrawMultiData();
            }
        }

        #region 寻峰的3个基本方法

        //取差分
        private double[] oneDiff(double[] data)
        {
            double[] result = new double[data.Length - 1];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = data[i + 1] - data[i];
            }
            return result;
        }

        //取差分的符号
        private int[] trendSign(double[] data)
        {
            int[] sign = new int[data.Length];
            for (int i = 0; i < sign.Length; i++)
            {
                if (data[i] > 0) sign[i] = 1;
                else if (data[i] == 0) sign[i] = 0;
                else sign[i] = -1;
            }

            for (int i = sign.Length - 1; i >= 0; i--)
            {
                if (sign[i] == 0 && i == sign.Length - 1)
                {
                    sign[i] = 1;
                }
                else if (sign[i] == 0)
                {
                    if (sign[i + 1] >= 0)
                    {
                        sign[i] = 1;
                    }
                    else
                    {
                        sign[i] = -1;
                    }
                }
            }
            return sign;
        }

        //获得峰和谷的Index，正值表示峰顶的index，负值表示峰谷的index（取绝对值），必然是一个峰顶、一个峰谷交替。
        private List<int> getPeaksIndex(int[] diff)
        {
            List<int> data = new List<int>();
            for (int i = 0; i != diff.Length - 1; i++)
            {
                if (diff[i + 1] - diff[i] == -2)
                {
                    data.Add(i + 1);//用正数表示这是峰
                }
                if (diff[i + 1] - diff[i] == 2)
                {
                    data.Add(-i - 1);//用负数表示这是谷底
                }
            }
            return data;//相当于原数组的下标
        }

        #endregion

        #region 高斯滤波方法

        //上面是两个基本方法，实际使用直接用第三个即可

        /// <summary>
        /// 计算某个位置数据的高斯函数值。
        /// 高斯函数的a为1，直接输入差值x，不需要μ，σ又外界输入
        /// </summary>
        /// <param name="x">该数据点与参考点的Index的差值，x为0时，高斯值为1</param>
        /// <param name="sigma">参数σ，σ越大，高斯函数值就越大，高斯峰越胖，远处的点产生的影响就越大</param>
        /// <returns></returns>
        private double CalculateGaussian(double x, double sigma)
        {
            return Math.Exp(-1 * x * x / 2 / sigma / sigma);
        }

        /// <summary>
        /// 计算一系列高斯值（高斯核）。
        /// 输入一个个数num，就从距离为0开始，依次加num次1，计算高斯值
        /// </summary>
        /// <param name="num">要向外计算的个数</param>
        /// <param name="sigma">参数σ，σ越大，高斯函数值就越大，高斯峰越胖，远处的点产生的影响就越大</param>
        /// <returns>返回num+1个数据，分别为距离为0~距离为num的数据的高斯值（计算权重）</returns>
        private double[] CalculateGaussianList(int num, double sigma)
        {
            double[] result = new double[num + 1];
            for (int i = 0; i <= num; i++)
            {
                result[i] = CalculateGaussian(i, sigma);
            }

            //每个权重值除以总和，算出比例值，到时候直接乘就行
            double sum = result.Sum() * 2 - 1;//乘2是因为，实际是用两边的，但是乘2会多加一个x为0的值，也即1，故减去
            for (int i = 0; i < result.Length; i++)
            {
                result[i] /= sum;
            }

            return result;
        }

        /// <summary>
        /// 对一个数据（一维数组）进行高斯滤波，就是用高斯函数算出卷积核，进行卷积
        /// </summary>
        /// <param name="data">需要滤波的数据</param>
        /// <param name="num">卷积时，左右计算的数据个数</param>
        /// <param name="sigma">高斯函数的参数σ</param>
        /// <param name="gl">卷积核</param>
        /// <returns></returns>
        private double[] GuassianSmooth(double[] data, int num, double sigma, double[] gl)
        {
            //高斯卷积后的数据
            double[] result = new double[data.Length];

            //遍历所有数据点，对每个数据点都卷积核进行卷积
            for (int i = 0; i < data.Length; i++)
            {
                double juan = 0;//这个点卷积后的结果

                //这个数据点找附近num个数据，按卷积核乘权重值并累加
                for (int j = -num; j < num; j++)
                {
                    if (i + j >= 0 && i + j < data.Length)//判断一下，i+j越界的时候不要加
                        juan += data[i + j] * gl[Math.Abs(j)];
                }

                //保存卷积结果赋值给原来的值
                result[i] = juan;
            }

            return result;
        }
        public double[] GuassianSmooth(double[] data)
        {
            double[] gl = CalculateGaussianList(juanNum, sigma);

            for (int i = 0; i < smoothTimes; i++)
                data = GuassianSmooth(data, juanNum, sigma, gl);

            return data;
        }

        /**
         * 数据平滑函数(一次平滑)
         *
         * @param in_Buffer 数据
         * @param N         数据的个数
         * @return the double [ ]
         */
        public double[] linearSmooth3(double[] in_Buffer, int N)
        {
            double[] out_Buffer = new double[N];
            int i;
            if (N < 3)
            {
                for (i = 0; i <= N - 1; i++)
                {
                    out_Buffer[i] = in_Buffer[i];
                }
            }
            else
            {
                out_Buffer[0] = (5.0 * in_Buffer[0] + 2.0 * in_Buffer[1] - in_Buffer[2]) / 6.0;
                for (i = 1; i <= N - 2; i++)
                {
                    out_Buffer[i] = (in_Buffer[i - 1] + in_Buffer[i] + in_Buffer[i + 1]) / 3.0;
                }
                out_Buffer[N - 1] =
                        (5.0 * in_Buffer[N - 1] + 2.0 * in_Buffer[N - 2] - in_Buffer[N - 3]) / 6.0;
            }
            return out_Buffer;
        }

        #endregion

        /// <summary>
        /// 寻峰方法。从原始数据，进行平滑、取峰谷、筛选正确的峰，返回峰的Index（对应的道址为Index+1）
        /// </summary>
        /// <param name="data">需要寻峰的数据</param>
        /// <returns>峰的Index</returns>
        public List<int> SeekPeak(double[] data)
        {
            //进行平滑前，保存一份原始数据
            double[] oriData = (double[])data.Clone();

            //卷积核，从距离为0开始到距离为num的权重比例
            double[] gl = CalculateGaussianList(juanNum, sigma);

            if (whichSmooth)
            {
                //高斯滤波
                for (int i = 0; i < smoothTimes; i++)
                    data = GuassianSmooth(data, juanNum, sigma, gl);
                Console.WriteLine("高斯滤波");
            }
            else
            {
                //线性滤波
                for (int i = 0; i < 40; i++)
                {
                    data = linearSmooth3(data, data.Length);
                }
                Console.WriteLine("线性滤波");
            }

            //将平滑后的结果记录在MultiDatas_Smooth中
            MainWindow.Instance.receDatas.MultiDatas_Smooth = data;

            //找到峰值对应的index：
            List<int> index = getPeaksIndex(trendSign(oneDiff(data)));
            index.Insert(0, -1);//表示开头是一个谷底

            //遍历峰、谷的index，对所有的峰进行判断：和相邻的谷的差值要足够大，峰、谷的index之间的距离要足够小
            List<int> realIndex = new List<int>();//记录峰顶的index
            for (int i = 1; i < index.Count - 1; i++)//下面要用到i-1、i+1，所以晚一个开始，早一个结束
            {
                if (index[i] > 0)//判断，如果是峰顶
                {
                    //取峰高：峰顶向左右两边的峰谷计算高度差，取较小值
                    double height = Math.Min(data[index[i]] - data[-index[i - 1]], data[index[i]] - data[-index[i + 1]]);

                    //取峰宽的一半（大约）：峰顶向左右两边的峰谷计算宽度差，取较大值
                    double width = Math.Max(index[i] + index[i - 1], -index[i] - index[i + 1]);

                    //判断峰是否满足要求：峰高足够高、峰宽不要太宽
                    if (height > minHeight && width < maxWidth)
                    {
                        //对于大小合适的峰，再进行一项判断：
                        //取一下相邻两峰谷范围内的原始数据最大值，用于和平滑后峰的最大值进行对比，若相差过于大，则是很大的毛刺，应当剔除
                        double maxOriData = 0;
                        for (int j = Math.Abs(index[i - 1]); j <= Math.Abs(index[i + 1]); j++)
                        {
                            if (oriData[j] > maxOriData)
                                maxOriData = oriData[j];
                        }
                        double maxSmoothData = data[index[i]];//平滑后峰的最大值

                        if ((maxSmoothData / maxOriData) > smallRatio)//判断，若平滑后峰高缩小的比例没有太大，则添加这个峰
                            realIndex.Add(index[i]);
                    }
                }
            }

            Console.WriteLine("寻峰结果个数：" + realIndex.Count);

            return realIndex;
        }

        //endregion
    }
}
