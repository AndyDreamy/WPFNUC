using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 核素识别仪.小窗口;
using System.Windows.Controls;
using System.Data;
using static 核素识别仪.集成的数据类.Recognize;
using System.Windows.Threading;
using System.Windows.Data;

namespace 核素识别仪.集成的数据类
{
    /// <summary>
    /// 与活度计算相关的数据
    /// </summary>
    public class HuoDu : INotifyPropertyChanged
    {
        public MainWindow Father { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        #region 用户设置的参数

        private double volume = 1;
        /// <summary>
        /// 用户设置的体积，用于计算比活度，单位为L
        /// </summary>
        public double P_volume
        {
            get { return volume; }
            set
            {
                if (value <= 0)//体积必须为正数
                    volume = 1;
                else
                    volume = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_volume"));
                }
            }
        }

        private double msTime_S = 10;
        /// <summary>
        /// 用户设置的测量时间，单位为s。到时间就自动停
        /// </summary>
        public double P_msTime_S
        {
            get { return msTime_S; }
            set
            {
                msTime_S = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_msTime_S"));
                }
            }
        }

        private string unit_CPS = "cps";
        /// <summary>
        /// 用户设置的计数率的单位，可能为"cps"、"cpm"。
        /// 计数率属性的值肯定永远是cps，单位为cpm时，只改变显示的值
        /// </summary>
        public string P_unit_CPS
        {
            get { return unit_CPS; }
            set
            {
                unit_CPS = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_unit_CPS"));
                }
                RefreshUnitOnDg();
            }
        }

        private string unit_Act = "bq";
        /// <summary>
        /// 用户设置的比活度的单位，可能为"bq"、"bq/l"。也相当于测量模式，如果选择bq，则活度属性值为活度值；
        /// 否则，活度属性值为除以体积后的比活度值
        /// </summary>
        public string P_unit_Act
        {
            get { return unit_Act; }
            set
            {
                unit_Act = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_unit_Act"));
                }
                RefreshUnitOnDg();
            }
        }

        /// <summary>
        /// 当用户设置新的单位时，更新dg的列名
        /// </summary>
        private void RefreshUnitOnDg()
        {
            if (Father == null)
                return;

            DataGrid dg = Father.dg_HuoDuResult;
            foreach (var co in dg.Columns)
            {
                if (co.Header.ToString().Contains("计数率"))
                    co.Header = "计数率(" + unit_CPS + ")";

                if (co.Header.ToString().Contains("活度"))
                {
                    if (unit_Act.Contains("/"))
                        co.Header = "比活度(" + unit_Act + ")";
                    else
                        co.Header = "活度(" + unit_Act + ")";
                }
            }
        }

        #endregion

        #region 本底计数率测量

        //就是在没有源的情况下测量一段时间，记录好结束时的多道数据，再除以实时间，得到本底多道计数率

        #region 数据

        private int time_BenDi = 100;
        /// <summary>
        /// 本底测量的时间，单位s
        /// </summary>
        public int P_time_BenDi
        {
            get { return time_BenDi; }
            set
            {
                time_BenDi = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_time_BenDi"));
                }
            }
        }

        private double[] multiCPS = new double[2048];
        /// <summary>
        /// 本地测量计数率结果存在这里
        /// </summary>
        public double[] P_multiCPS
        {
            get { return multiCPS; }
            set { multiCPS = value; }
        }

        private bool interrupt = false;
        /// <summary>
        /// 中断标志位，若设置为true，则会中断当前的循环
        /// </summary>
        public bool P_interrupt
        {
            get { return interrupt; }
            set { interrupt = value; }
        }

        #endregion

        #region 方法

        /// <summary>
        /// 开启本底测量的方法。正常采集即可，只是需要关闭画图功能
        /// </summary>
        public void StartBenDiCPS()
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
                MessageBox.Show("已开启了一次本底测量", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            W_StabilizePeak w_temp = Father.w_BenDiCPS;

            //关闭画图
            Father.autoRun.P_isDrawingOn = false;
            //隐藏Chart
            //GrandFather.wfhost_Chart.Visibility = System.Windows.Visibility.Hidden;
            //打开小窗口
            Father.adc.OpenOneWindow(w_temp);
            //UI提示
            w_temp.P_state = "正在本底测量:";
            //不让稳峰小界面关闭
            w_temp.canHide = false;

            //开始采集
            Father.开始测量_Click(this, null);

            //连续采集一段时间——稳峰时间
            w_temp.progress_StabPeak.Maximum = time_BenDi;
            for (int i = 0; i < time_BenDi && !interrupt; i++)//这里，如果interrupt被设置为true，则会中断稳峰
            {
                //进度条值更新
                w_temp.progress_StabPeak.Value = i + 1;
                Father.adc.delay(1000);
            }

            //停止采集
            Father.停止测量_Click(this, null);
            Father.adc.delay(400);
            //Father.清空测量_Click(this, null);//这里不能清空，否则会影响本底数据的保存

            //如果正常结束，则把多道数据除以时间得到本底计数率数据，并保存
            if (!interrupt)
            {
                double[] datas = Father.receDatas.MultiDatas;
                double time = Father.receDatas.P_measuredTime;
                for (int i = 0; i < datas.Length; i++)
                {
                    multiCPS[i] = datas[i] / time;
                }

                //把新的本底测量数据保存到文件
                SaveBenDiCPSToFile();
            }
            else//若中断了
            {
                //从文件加载上次的本底测量数据
                GetBenDiCPSFromFile();
            }

            //本底数据保存后，再清空
            Father.清空测量_Click(this, null);

            //每次停止后且使用完后，都让interrupt复位
            interrupt = false;

            //UI提示
            w_temp.P_state = "本底测量结束";
            Father.adc.delay(1000);//显示1s再关闭

            //开启画图
            Father.autoRun.P_isDrawingOn = true;
            //恢复Chart
            //GrandFather.wfhost_Chart.Visibility = System.Windows.Visibility.Visible;
            //允许稳峰小界面关闭
            w_temp.canHide = true;

            //结束后自动关闭即可
            w_temp.Hide();
        }

        /// <summary>
        /// 将本底测量的计数率存到文件。只存一份数据，新保存一次就把上一次的替换。
        /// </summary>
        public void SaveBenDiCPSToFile()
        {
            List<string> lines = new List<string>();
            //添加时间信息
            lines.Add("Time=" + DateTime.Now.ToString("G"));

            //添加数据，格式为道址数=计数率
            for (int i = 0; i < multiCPS.Length; i++)
            {
                lines.Add((i + 1).ToString() + "=" + multiCPS[i].ToString());
            }

            //写进文件
            MainWindow.Instance.andyFileRW.WriteFileInResources("活度计算相关\\本底测量计数率.bd", lines.ToArray());

            Console.WriteLine("成功保存一组本底测量数据到文件");
        }

        /// <summary>
        /// 从文件加载本底测量计数率数据
        /// </summary>
        public void GetBenDiCPSFromFile()
        {
            try
            {
                string[] lines = MainWindow.Instance.andyFileRW.ReadFileInResources("活度计算相关\\本底测量计数率.bd");

                string dateInfo = "";

                foreach (var line in lines)
                {
                    string[] strs = line.Split(new char[] { '=' });

                    if (strs.Length >= 2)//正常是分为2部分
                    {
                        //时间信息
                        if (strs[0].Equals("Time"))
                            dateInfo = strs[1];
                        else//数据信息
                        {
                            int.TryParse(strs[0], out int channel);//道址
                            double.TryParse(strs[1], out double cps);//计数率
                            if (channel > 0 && cps > 0)
                            {
                                multiCPS[channel - 1] = cps;
                            }
                        }
                    }
                }
                ////加载完成，提示
                //MessageBox.Show("")
                Console.WriteLine("已加载文件中的本底测量数据，数据记录时间：" + dateInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine("加载文件中的本底测量数据失败，详情：" + ex.Message);
            }
        }

        #endregion

        #endregion

        #region 从多道数据计算核素的脉冲数，再计算活度

        //正常测量、核素识别，拿到核素识别列表，分别计算各个核素
        //的计数率。若没有想要的核素，则从nucPeakSet中查活度到能量的因子

        #region 数据

        /*计算Cs、Co的计数率的方法：
        正常来说算法是：寻到Cs、Co的道址（也可以有其他核素），向两边取计数（用一个参数控制即可，向左向右偏移一样的长度，不论哪种核素偏移都一样），除以时间得到计数率，还需要减去本底的计数率；
        本底计数率的获取：开机进行本底测量，没有源的情况下，得到每一道的计数率，简单。
        在此基础上，如果说测量时间内没有识别出核素，就取一个提前设置值作为核素识别道址（这个道址也要参与稳峰）*/

        private int offset_CPS = 30;
        /// <summary>
        /// 用于计算某个核素计数率的偏移量。用一个参数控制即可，向左向右偏移一样的长度，不论哪种核素偏移都一样
        /// </summary>
        public int P_offset_CPS
        {
            get { return offset_CPS; }
            set
            {
                offset_CPS = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_offset_CPS"));
                }
            }
        }

        /// <summary>
        /// 自定义数据类型，自己设置的核素峰位。在测量时间结束时没有寻到某个核素的峰时，
        /// 可以用这个设置值作为寻到的峰位
        /// </summary>
        public class NucPeakSet
        {
            /// <summary>
            /// 核素名称
            /// </summary>
            public string NucName { get; set; }

            /// <summary>
            /// 峰位设置值
            /// </summary>
            public double PeakChannel { get; set; }

            private bool isUsed = false;
            /// <summary>
            /// 表征是否被使用于，识别不出核素时，直接以这个道址作为识别结果
            /// </summary>
            public bool P_isUsed
            {
                get { return isUsed; }
                set { isUsed = value; }
            }

        }

        private List<NucPeakSet> nucPeakSet = new List<NucPeakSet>();
        /// <summary>
        /// 设置的峰位列表。若在测量时间结束时没有寻到某个核素的峰时，
        /// 可以在这个表中寻到有没有想要识别的峰，有的话用其PeakChannel作为寻到的峰位
        /// </summary>
        public List<NucPeakSet> P_nucPeakSet
        {
            get { return nucPeakSet; }
            set { nucPeakSet = value; }
        }

        /// <summary>
        /// 自定义的一个核素的活度相关数据
        /// </summary>
        public class NucHuoDuData
        {
            private string nucName;
            /// <summary>
            /// 核素名称
            /// </summary>
            public string P_nucName
            {
                get { return nucName; }
                set { nucName = value; }
            }

            private int channel;
            /// <summary>
            /// 核素识别出的道址。这个道址不要被稳峰因子修正，而是用数据本身寻到的位置
            /// </summary>
            public int P_channel
            {
                get { return channel; }
                set { channel = value; }
            }

            private double cps;
            /// <summary>
            /// 该核素算出的计数率
            /// </summary>
            public double P_cps
            {
                get { return cps; }
                set
                {
                    cps = value;
                }
            }

            private double act;
            /// <summary>
            /// 由计数率算出的活度值
            /// </summary>
            public double P_act
            {
                get { return act; }
                set
                {
                    act = value;
                }
            }

        }

        /// <summary>
        /// 记录了所有要计算计数率、活度的核素信息，最终会显示到UI上
        /// </summary>
        public List<NucHuoDuData> nucHuoDuDatas = new List<NucHuoDuData>();

        /// <summary>
        /// 核素计数率到活度的拟合因子数据
        /// </summary>
        public class NucActScaleFactor : INotifyPropertyChanged
        {
            //private string nucName;

            //public string P_nucName
            //{
            //    get { return nucName; }
            //    set
            //    {
            //        nucName = value;
            //        if (PropertyChanged != null)
            //        {
            //            PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_nucName"));
            //        }
            //    }
            //}

            //private double A0;

            //public double P_A0
            //{
            //    get { return A0; }
            //    set
            //    {
            //        A0 = value;
            //        if (PropertyChanged != null)
            //        {
            //            PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_A0"));
            //        }
            //    }
            //}

            public string P_nucName { get; set; }
            public double P_A0 { get; set; }
            public double P_B0 { get; set; }
            public double P_C0 { get; set; }
            public double P_A1 { get; set; }
            public double P_B1 { get; set; }
            public double P_C1 { get; set; }
            public double P_A2 { get; set; }
            public double P_B2 { get; set; }
            public double P_C2 { get; set; }
            /// <summary>
            /// 1档到2档的分段点
            /// </summary>
            public double P_1To2 { get; set; }
            /// <summary>
            /// 2档到3档的分段点
            /// </summary>
            public double P_2To3 { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            /// 返回一个数值完全相同的对象
            /// </summary>
            /// <returns></returns>
            public NucActScaleFactor Clone()
            {
                NucActScaleFactor n = new NucActScaleFactor();
                n.P_A0 = P_A0;
                n.P_B0 = P_B0;
                n.P_C0 = P_C0;
                n.P_A1 = P_A1;
                n.P_B1 = P_B1;
                n.P_C1 = P_C1;
                n.P_A2 = P_A2;
                n.P_B2 = P_B2;
                n.P_C2 = P_C2;
                n.P_1To2 = P_1To2;
                n.P_2To3 = P_2To3;
                n.P_nucName = P_nucName;
                return n;
            }
        }

        /// <summary>
        /// 存储所有核素的计算活度刻度因子
        /// </summary>
        public List<NucActScaleFactor> factors = new List<NucActScaleFactor>();

        #endregion

        #region 方法

        /// <summary>
        /// 初始化计数率到活度的拟合因子
        /// </summary>
        public void Init_ScaleFactors()
        {
            try
            {
                string[] lines = Father.andyFileRW.ReadFileInResources("活度计算相关\\核素计数率到活度刻度因子.txt");
                foreach (var line in lines)
                {
                    string[] strs = line.Split(new char[] { '#', '=' });
                    if (line.Contains("#"))//找到一个核素信息
                    {
                        factors.Add(new NucActScaleFactor() { P_nucName = strs[1] });
                    }
                    else if (line.Contains("="))//factors中最后一个元素的因子信息
                    {
                        NucActScaleFactor fac = factors[factors.Count - 1];
                        string factorName = strs[0];
                        double.TryParse(strs[1], out double value);
                        switch (factorName)
                        {
                            case "A0":
                                fac.P_A0 = value;
                                break;
                            case "B0":
                                fac.P_B0 = value;
                                break;
                            case "C0":
                                fac.P_C0 = value;
                                break;
                            case "A1":
                                fac.P_A1 = value;
                                break;
                            case "B1":
                                fac.P_B1 = value;
                                break;
                            case "C1":
                                fac.P_C1 = value;
                                break;
                            case "A2":
                                fac.P_A2 = value;
                                break;
                            case "B2":
                                fac.P_B2 = value;
                                break;
                            case "C2":
                                fac.P_C2 = value;
                                break;
                            case "一二档分段点":
                                fac.P_1To2 = value;
                                break;
                            case "二三档分段点":
                                fac.P_2To3 = value;
                                break;

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("核素计数率到活度刻度因子读取失败，详情：" + ex.Message);
            }
        }

        /// <summary>
        /// 保存刻度因子到文件的方法
        /// </summary>
        public void SaveScaleFactorsToFile()
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add("说明：用井号表示一个核素的信息的开始，每个核素有多项内容，分别为核素名称、A0、B0、C0、A1、B1、C1、A2、B2、C2、一二档分段点、二三档分段点");
                lines.Add("");
                foreach (var item in factors)
                {
                    lines.Add("#" + item.P_nucName);
                    lines.Add("A0=" + item.P_A0);
                    lines.Add("B0=" + item.P_B0);
                    lines.Add("C0=" + item.P_C0);
                    lines.Add("A1=" + item.P_A1);
                    lines.Add("B1=" + item.P_B1);
                    lines.Add("C1=" + item.P_C1);
                    lines.Add("A2=" + item.P_A2);
                    lines.Add("B2=" + item.P_B2);
                    lines.Add("C2=" + item.P_C2);
                    lines.Add("一二档分段点=" + item.P_1To2);
                    lines.Add("二三档分段点=" + item.P_2To3);
                    lines.Add("");
                }

                Father.andyFileRW.WriteFileInResources("\\活度计算相关\\核素计数率到活度刻度因子.txt", lines.ToArray());
                MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败，详情" + ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 读取一下人为设定的核素识别道址
        /// </summary>
        public void Init_NucPeakChannelToSet()
        {
            try
            {
                string[] lines = Father.andyFileRW.ReadFileInResources("活度计算相关\\核素道址人为设定值.txt");
                foreach (var line in lines)
                {
                    string[] strs = line.Split(new char[] { '=' });

                    if (strs.Length >= 2)
                    {
                        //获取核素名和道址
                        string nucName = strs[0];
                        double.TryParse(strs[1], out double channel);

                        //添加在nucPeakSet
                        nucPeakSet.Add(new NucPeakSet() { NucName = nucName, PeakChannel = channel });//（是否使用的标志位默认为false）
                    }

                }

                //加载完nucPeakSet后，在选择这些核素是否生效的界面中，动态生成相应的CheckBox
                UIElementCollection children = MainWindow.Instance.sp_SelectSetChannelNuc.Children;
                foreach (var item in nucPeakSet)
                {
                    CheckBox cb = new CheckBox() { Content = item.NucName };
                    cb.Margin = new Thickness(0, 0, 0, 10);
                    cb.VerticalContentAlignment = VerticalAlignment.Center;
                    cb.SetBinding(CheckBox.IsCheckedProperty, new Binding("P_isUsed") { Source = item });
                    children.Add(cb);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("核素道址预设值读取失败，详情：" + ex.Message);
            }
        }

        /// <summary>
        /// ★测量完成后，计算计数率、活度值，存到nucHuoDuDatas。
        /// 会在线程中执行
        /// </summary>
        public void CalculateAct()
        {
            //假设现在测量完固定时间了，获取核素识别结果
            List<RecoResult> res = Father.reco.RecoResults;

            #region 这里对Cs、Co识别不出来的情况做一个处理 

            //找一下识别结果，是否有Cs、Co，若没有，则用nucPeakSet中的道址信息（但是要受稳峰因子的影响），手动添加两个识别结果信息

            double factor = Father.aStabPeak.P_factor_StabPeak;//获取稳峰因子
            string[] names = new string[] { "Cs-137", "Co-60" };
            foreach (var name in names)
            {
                if (res.Find(x => x.P_nucName.Equals(name)) == null)//在res中找找，如果没有：
                {
                    NucPeakSet nuc = nucPeakSet.Find(x => x.NucName.Equals(name));//在nucPeakSet中找到该核素
                    if (nuc != null && nuc.P_isUsed)//这里判断一下P_isUsed，只有为true，也就是用户选中了这个核素，才会使用
                        res.Add(new RecoResult()
                        {
                            P_nucName = nuc.NucName,
                            P_channel = (int)(nuc.PeakChannel / factor),

                        });
                }
            }

            #endregion

            //遍历核素识别结果，取某个核素所在道址左右偏移offset_CPS道的总计数率，同时减去本底相同位置的计数率
            nucHuoDuDatas.Clear();
            for (int i = 0; i < res.Count; i++)
            {
                RecoResult reco = res[i];//取一个识别结果，只用到核素名和道址两个属性

                #region 计算一个范围内的计数率

                double sumCPS = 0;
                //遍历偏移范围内的道址
                for (int j = -1 * offset_CPS; j <= offset_CPS; j++)
                {
                    int index = reco.P_channel + j;//峰位道址加上偏移后的道址
                    if (index > 0 && index <= 2048)//判断道址不越界
                    {
                        //多道数据找到这一道的计数值，除以测量时间得到cps
                        double cps = Father.receDatas.MultiDatas[index - 1] / Father.receDatas.P_measuredTime;

                        //净计数率就是多道实测计数率减去相同位置的本底测量计数率
                        sumCPS += cps - multiCPS[index - 1];
                    }
                }

                //这里算完，因为减本底，可能会出现负值
                if (sumCPS < 0)
                    sumCPS = 0;

                #endregion

                #region 根据刻度公式计算，要从factors中找到这个核素的因子信息

                NucActScaleFactor fac = factors.Find(x => x.P_nucName.Equals(reco.P_nucName));
                if (fac != null)//有可能会找不到这个核素的刻度因子信息
                {
                    //根据因子计算
                    double act = 0;

                    //确定所处的档位
                    if (sumCPS > fac.P_2To3)
                        act = fac.P_A2 * sumCPS * sumCPS + fac.P_B2 * sumCPS + fac.P_C2;
                    else if (sumCPS > fac.P_1To2)
                        act = fac.P_A1 * sumCPS * sumCPS + fac.P_B1 * sumCPS + fac.P_C1;
                    else
                        act = fac.P_A0 * sumCPS * sumCPS + fac.P_B0 * sumCPS + fac.P_C0;

                    //计算完成将结果存到nucHuoDuDatas
                    NucHuoDuData data = new NucHuoDuData()
                    {
                        P_nucName = reco.P_nucName,
                        P_channel = reco.P_channel,
                        P_cps = Math.Round(sumCPS, 2),
                        P_act = Math.Round(act, 2)
                    };

                    #region 这里根据用户所选的单位，修改值

                    if (unit_Act.Equals("bq/l"))
                    {
                        //需要除以体积
                        if (volume > 0)
                        {
                            data.P_act = Math.Round(data.P_act / volume, 2);
                        }
                    }

                    if (unit_CPS.Equals("cpm"))
                    {
                        //转换为1min的计数值
                        data.P_cps *= 60;
                    }

                    #endregion

                    nucHuoDuDatas.Add(data);
                }
                else
                {
                    Console.WriteLine("活度计算：未找到" + reco.P_nucName + "的刻度因子信息");
                }

                #endregion
            }

            //将nucHuoDuDatas中的结果显示
            Father.Dispatcher.Invoke(new System.Windows.Forms.MethodInvoker(delegate
            {
                Father.dg_HuoDuResult.ItemsSource = null;
                Father.dg_HuoDuResult.ItemsSource = nucHuoDuDatas;
            }));

        }


        #endregion

        #endregion
    }
}
