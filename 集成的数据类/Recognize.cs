using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using 核素识别仪.其他功能类;
using System.Data;

namespace 核素识别仪.集成的数据类
{
    /// <summary>
    /// 核素识别相关的数据
    /// </summary>
    public class Recognize : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private MainWindow father;
        /// <summary>
        /// 主界面对象
        /// </summary>
        public MainWindow Father
        {
            get { return father; }
            set { father = value; }
        }

        AndyFileRW rw = new AndyFileRW();

        #region 核素识别用到的数据

        private List<int> peakIndexes = new List<int>();
        /// <summary>
        /// 寻到的峰的index的List
        /// </summary>
        public List<int> P_peakIndexes
        {
            get { return peakIndexes; }
            set
            {
                peakIndexes = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_peakIndexes"));
                }
            }
        }

        private double recoError = 0.05;
        /// <summary>
        /// 核素识别的误差，如果计算能量和标准能量误差在recoError之内，则认为成功识别该核素
        /// </summary>
        public double P_recoError
        {
            get { return recoError; }
            set
            {
                recoError = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_recoError"));
                }
            }
        }

        private double recoDiff = 10;
        /// <summary>
        /// 核素识别的能量绝对差值，单位为keV，如果计算能量和标准能量的绝对差值在recoDiff之内，则认为成功识别该核素
        /// </summary>
        public double P_recoDiff
        {
            get { return recoDiff; }
            set
            {
                recoDiff = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_recoDiff"));
                }
            }
        }

        private double energyDifference = 40;
        /// <summary>
        /// 计算置信度的能量差异。这个值越大，置信度就越接近1
        /// </summary>
        public double P_energyDifference
        {
            get { return energyDifference; }
            set
            {
                energyDifference = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_energyDifference"));
                }
            }
        }

        private bool isPeaksShown = true;
        /// <summary>
        /// 表征是否显示未识别的核素
        /// </summary>
        public bool P_isPeaksShown
        {
            get { return isPeaksShown; }
            set
            {
                isPeaksShown = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_isPeaksShown"));
                }
            }
        }

        #endregion

        #region 稳峰相关的数据

        //class WenFengData : INotifyPropertyChanged
        //{
        //    public event PropertyChangedEventHandler PropertyChanged;

        //    private double factor_WenFeng = 1;
        //    /// <summary>
        //    /// 稳峰因子
        //    /// </summary>
        //    public double P_factor_WenFeng
        //    {
        //        get { return factor_WenFeng; }
        //        set
        //        {
        //            factor_WenFeng = value;
        //            if (PropertyChanged != null)
        //            {
        //                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_factor_WenFeng"));
        //            }
        //        }
        //    }

        //    private int KChannel_Scale = 1250;
        //    /// <summary>
        //    /// K40在刻度时测得的标准道址
        //    /// </summary>
        //    public int P_KChannel_Scale
        //    {
        //        get { return KChannel_Scale; }
        //        set
        //        {
        //            KChannel_Scale = value;
        //            if (PropertyChanged != null)
        //            {
        //                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_KChannel_Scale"));
        //            }
        //        }
        //    }

        //    private int KChannel_Real = 1250;
        //    /// <summary>
        //    /// 稳峰时获得的K40道址(也可以从文件读取)
        //    /// </summary>
        //    public int P_KChannel_Real
        //    {
        //        get { return KChannel_Real; }
        //        set
        //        {
        //            KChannel_Real = value;
        //            if (PropertyChanged != null)
        //            {
        //                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_KChannel_Real"));
        //            }
        //        }
        //    }

        //    private int KJudgeMax = 1400;
        //    /// <summary>
        //    /// 稳峰K40道址的判断上限
        //    /// </summary>
        //    public int P_KJudgeMax
        //    {
        //        get { return KJudgeMax; }
        //        set
        //        {
        //            KJudgeMax = value;
        //            if (PropertyChanged != null)
        //            {
        //                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_KJudgeMax"));
        //            }
        //        }
        //    }

        //    private int KJudgeMin = 1000;
        //    /// <summary>
        //    /// 稳峰K40道址的判断下限
        //    /// </summary>
        //    public int P_KJudgeMin
        //    {
        //        get { return KJudgeMin; }
        //        set
        //        {
        //            KJudgeMin = value;
        //            if (PropertyChanged != null)
        //            {
        //                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_KJudgeMin"));
        //            }
        //        }
        //    }

        //    private int time_WenFeng = 200;
        //    /// <summary>
        //    /// 稳峰时间
        //    /// </summary>
        //    public int P_time_WenFeng
        //    {
        //        get { return time_WenFeng; }
        //        set
        //        {
        //            time_WenFeng = value;
        //            if (PropertyChanged != null)
        //            {
        //                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_time_WenFeng"));
        //            }
        //        }
        //    }

        //    private int time_WarmUp = 200;
        //    /// <summary>
        //    /// 预热时间
        //    /// </summary>
        //    public int P_time_WarmUp
        //    {
        //        get { return time_WarmUp; }
        //        set
        //        {
        //            time_WarmUp = value;
        //            if (PropertyChanged != null)
        //            {
        //                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_time_WarmUp"));
        //            }
        //        }
        //    }
        //}

        ///// <summary>
        ///// 稳峰相关的参数
        ///// </summary>
        //WenFengData wenFengData = new WenFengData();

        #endregion

        #region 核素识别结果数据

        /// <summary>
        /// 核素识别结果数据
        /// </summary>
        public class RecoResult : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private int channel;
            /// <summary>
            /// 寻峰的道址
            /// </summary>
            public int P_channel
            {
                get { return channel; }
                set
                {
                    channel = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_channel"));
                    }
                }
            }

            private string nucName = string.Empty;
            /// <summary>
            /// 识别出的核素名称，若未识别，则为Empty
            /// </summary>
            public string P_nucName
            {
                get { return nucName; }
                set
                {
                    nucName = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_nucName"));
                    }
                }
            }

            private double peakEnergy;
            /// <summary>
            /// 由寻峰道址计算出的能量
            /// </summary>
            public double P_peakEnergy
            {
                get { return peakEnergy; }
                set
                {
                    peakEnergy = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_peakEnergy"));
                    }
                }
            }

            private double realEnergy;
            /// <summary>
            /// 识别出的核素真实能量
            /// </summary>
            public double P_realEnergy
            {
                get { return realEnergy; }
                set
                {
                    realEnergy = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_realEnergy"));
                    }
                }
            }

            private double error;
            /// <summary>
            /// 2个能量的误差
            /// </summary>
            public double P_error
            {
                get { return error; }
                set
                {
                    error = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_error"));
                    }
                }
            }

            private double confidence;
            /// <summary>
            /// 核素识别的置信度
            /// </summary>
            public double P_confidence
            {
                get { return confidence; }
                set
                {
                    confidence = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_confidence"));
                    }
                }
            }

            public RecoResult(int channel, string nucName, double peakEnergy, double realEnergy, double error, double confidence)
            {
                this.channel = channel;
                this.nucName = nucName;
                this.peakEnergy = peakEnergy;
                this.realEnergy = realEnergy;
                this.error = error;
                this.confidence = confidence;
            }

            public RecoResult()
            {

            }
        }

        /// <summary>
        /// 存放所有识别结果的list
        /// </summary>
        List<RecoResult> recoResults = new List<RecoResult>();
        public List<RecoResult> RecoResults
        {
            get { return recoResults; }
            set
            {
                recoResults = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("RecoResults"));
                }
            }
        }


        #endregion

        #region 用于识别的核素库数据

        public class NucInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private string nucName;
            /// <summary>
            /// 核素名称
            /// </summary>
            public string P_nucName
            {
                get { return nucName; }
                set
                {
                    nucName = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_nucName"));
                    }
                }
            }

            private double energy;
            /// <summary>
            /// 核素标准能量
            /// </summary>
            public double P_energy
            {
                get { return energy; }
                set
                {
                    energy = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_energy"));
                    }
                }
            }

            private string libName;
            /// <summary>
            /// 核素库名称
            /// </summary>
            public string P_libName
            {
                get { return libName; }
                set
                {
                    libName = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_libName"));
                    }
                }
            }

        }

        /// <summary>
        /// 核素识别列表
        /// </summary>
        List<NucInfo> L_nucToReco = new List<NucInfo>();
        /// <summary>
        /// 核素识别列表
        /// </summary>
        public List<NucInfo> P_L_nucToReco { get => L_nucToReco; set => L_nucToReco = value; }

        /// <summary>
        /// 分类核素库的库名，例如“裂变核素库等”
        /// </summary>
        private List<string> L_libName = new List<string>();
        /// <summary>
        /// 分类核素库的库名，例如“裂变核素库等”
        /// </summary>
        public List<string> P_L_libName { get => L_libName; set => L_libName = value; }

        #endregion

        /// <summary>
        /// 构造器
        /// </summary>
        public Recognize()
        {
            //从文件读取之前保存的识别核素列表信息
            GetNucToRecoFromFile();

            //如果文件中没有信息，则添加一些基本的：
            if (L_nucToReco.Count == 0)
            {
                L_nucToReco.Add(new NucInfo() { P_nucName = "Am-241", P_energy = 59.5412, P_libName = "default" });
                L_nucToReco.Add(new NucInfo() { P_nucName = "Cs-137", P_energy = 661.66, P_libName = "default" });
                L_nucToReco.Add(new NucInfo() { P_nucName = "Co-60", P_energy = 1173.23, P_libName = "default" });
                L_nucToReco.Add(new NucInfo() { P_nucName = "Co-60", P_energy = 1332.49, P_libName = "default" });
            }

            //更新核素库名称L_libName
            RefreshL_libName();
        }

        /// <summary>
        /// 寻峰，将结果存在字段reco.P_peakIndexes中。在自动运行线程中执行
        /// </summary>
        public void SeekPeakIndexes()
        {
            //获得峰位对应的Index
            P_peakIndexes = father.andySeekPeak.SeekPeak(father.receDatas.MultiDatas);
        }

        /// <summary>
        /// 根据当前的reco.P_peakIndexes值，标注寻峰结果
        /// </summary>
        public void MarkPeaks()
        {
            //画寻峰结果的CS：
            AndyChart.ChartSeries CS = father.andyChart.dataChart.CSs[1];

            #region 原来旧的

            ////把峰值那个数据点，也在series[1]中画出来，虽然没有颜色，但可以影响label的位置
            //for (int i = 0; i < P_peakIndexes.Count; i++)
            //{
            //    int peakIndex = P_peakIndexes[i];
            //    CS.Ys[peakIndex] = father.receDatas.MultiDatas[peakIndex];
            //    //这里的赋值会保留一些旧数据，理应删除，但是这里就不频繁地创建数组了，而是在清除按钮请一次就行
            //}
            //father.andyChart.RefreshSeries(1);//这一步会刷新掉之前的Label

            ////在相应的点上画一个label
            //for (int i = 0; i < P_peakIndexes.Count; i++)
            //{
            //    int peakIndex = P_peakIndexes[i];
            //    RecoResult res = recoResults.Find(x => x.P_channel == peakIndex + 1);//根据道址从核素识别结果中
            //    if (res == null)
            //        break;
            //    //CS.series.Points[peakIndex].Label = "▲" + (peakIndex + 1) + ","  + father.receDatas.CalculateEnergy(peakIndex + 1);//这是显示道址和能量
            //    //CS.series.Points[peakIndex].Label = "▲" + res.P_channel + "," + res.P_peakEnergy;//这是显示道址和能量
            //    CS.series.Points[peakIndex].Label = "▲" + res.P_channel + "," + res.P_nucName;//这是显示道址和核素名称
            //} 

            #endregion

            //每次标注前，都应该清除之前所有的标签
            father.andyChart.RefreshSeries(1);//这一步会刷新掉之前的Label，同时赋值的Point值也清零了

            //遍历核素识别结果，标注峰位信息
            for (int i = 0; i < recoResults.Count; i++)
            {
                RecoResult res = recoResults[i];
                int index = res.P_channel - 1;
                //double count = father.receDatas.MultiDatas[index];
                double count = father.receDatas.MultiDatas_Smooth[index];//这里应该用平滑过的数据的峰顶值，作为Y值，这样标签不会乱跑了

                //把峰值那个数据点，也在series[1]中画出来，虽然没有颜色，但可以影响label的位置
                //CS.Ys[res.P_channel - 1] = count;
                CS.series.Points[index].YValues = new double[] { count };//这里就直接设置Point的Y值

                //在该点上画一个label
                CS.series.Points[index].Label = "▲" + res.P_channel + "," + res.P_nucName;//这是显示道址和核素名称
            }
        }

        /// <summary>
        /// 核素识别数据处理，最后将结果存在recoResults中
        /// </summary>
        public void RecognizeNuc()
        {
            //每次识别前，清空上次的结果
            recoResults.Clear();

            //遍历寻到的峰的道址
            for (int i = 0; i < peakIndexes.Count; i++)
            {
                //获取寻峰寻到的道址
                int channel = peakIndexes[i] + 1;

                //除以稳峰因子后的道址，仅用于计算能量
                double channel_WF = channel / Father.aStabPeak.P_factor_StabPeak;

                //计算能量
                double peakEnergy = father.receDatas.CalculateEnergy(channel_WF);

                /**定义置信度，在每个峰值判断时，都让置信度清零。若之后成功识别出核素，则置信度不为0；
			        * 若识别核素结束后，置信度仍为0则说明这个峰未识别出核素*/
                double confidence = 0;

                //遍历核素库
                for (int j = 0; j < L_nucToReco.Count; j++)
                {
                    NucInfo nuc = L_nucToReco[j];

                    //当前核素的标准能量
                    double realEnergy = nuc.P_energy;

                    //计算2个能量的误差，判断是否成功识别核素
                    double diff = Math.Abs(realEnergy - peakEnergy);//绝对的差值
                    double error = diff / realEnergy;
                    error = Math.Round(error, 4) * 100;//这里的误差取百分比
                    //if (error < recoError)
                    if (diff <= recoDiff)//原来是根据相对误差判断是否识别，现在使用绝对误差判断
                    {
                        //计算置信度
                        confidence = (float)Math.Exp(-0.16 * Math.Pow(peakEnergy - realEnergy, 2) / energyDifference);

                        //将此核素识别结果添加到list中
                        recoResults.Add(new RecoResult(channel, nuc.P_nucName, peakEnergy, realEnergy, error, Math.Round(confidence, 4)));

                        //break的话，识别到1个结果就停止识别，否则，能量相近的也会识别
                        break;
                    }

                    //若误差很大，没有识别到，就继续循环，查询核素库其他核素

                }

                //若识别核素结束后，置信度仍为0，则说明这个峰未识别出核素
                if (confidence == 0 && isPeaksShown)
                {
                    //若isPeaksShown为true，则把这个未识别的结果也填进去
                    recoResults.Add(new RecoResult(channel, Properties.Resources.Res_未识别, peakEnergy, 0, 0, 0));
                }

            }

            //所有寻到的峰都识别了一遍
            Console.WriteLine("核素识别个数：" + recoResults.Count);

        }

        /// <summary>
        /// 由核素识别列表P_L_nucToReco更新核素库名P_L_libName的方法
        /// </summary>
        public void RefreshL_libName()
        {
            P_L_libName.Clear();
            foreach (var item in P_L_nucToReco)
            {
                if (!P_L_libName.Contains(item.P_libName))
                    P_L_libName.Add(item.P_libName);
            }
        }

        /// <summary>
        /// 从文件读取之前保存的识别核素列表信息
        /// </summary>
        private void GetNucToRecoFromFile()
        {
            string[] lines = rw.ReadFileInResources("核素库\\核素识别列表.txt");
            foreach (var line in lines)
            {
                string[] strs = line.Split('=');
                if (strs.Length == 3)
                {
                    string nucName = strs[0];
                    double energy;
                    double.TryParse(strs[1], out energy);
                    string libName = strs[2];
                    L_nucToReco.Add(new NucInfo() { P_nucName = nucName, P_energy = energy, P_libName = libName });
                }
            }
        }

        /// <summary>
        /// 识别核素列表信息保存到文件中，在“编辑核素库界面”的保存按钮中执行
        /// </summary>
        public void SaveNucToRecoToFile()
        {
            List<string> lines = new List<string>();
            lines.Add("这里存放需要识别的核素列表信息，每个核素3个内容，分别为核素名、能量、所在核素库名。用'='隔开。");
            for (int i = 0; i < L_nucToReco.Count; i++)
            {
                NucInfo nuc = L_nucToReco[i];
                lines.Add(nuc.P_nucName + "=" + nuc.P_energy + "=" + nuc.P_libName);
            }
            rw.WriteFileInResources("核素库\\核素识别列表.txt", lines.ToArray());
        }

    }
}
