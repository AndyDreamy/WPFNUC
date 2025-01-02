using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using 核素识别仪.其他功能类;
using static 核素识别仪.MainWindow;

namespace 核素识别仪.集成的数据类
{
    /// <summary>
    /// 从设备读取的、以及和多道相关的数据集合
    /// </summary>
    public class ReceDatas : INotifyPropertyChanged
    {
        #region 数据定义

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private MainWindow father;
        /// <summary>
        /// 主界面对象
        /// </summary>
        public MainWindow Father
        {
            get { return father; }
            set { father = value; }
        }

        #region 从仪器读取相关数据

        private double[] multiDatas = new double[2048];
        /// <summary>
        /// 多道数据
        /// </summary>
        public double[] MultiDatas
        {
            get { return multiDatas; }
            set
            {
                multiDatas = value;
            }
        }

        private double[] multiDatas_Smooth = new double[2048];
        /// <summary>
        /// 平滑后的多道数据
        /// </summary>
        public double[] MultiDatas_Smooth
        {
            get { return multiDatas_Smooth; }
            set
            {
                multiDatas_Smooth = value;
            }
        }

        private double cps_Multi;
        /// <summary>
        /// 计数率_多道数据算出的
        /// </summary>
        public double Cps_Multi
        {
            get { return cps_Multi; }
            set
            {
                cps_Multi = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Cps_Multi"));
            }
        }

        private double cps_GM;
        /// <summary>
        /// 计数率_盖革管测得的
        /// </summary>
        public double Cps_GM
        {
            get { return cps_GM; }
            set
            {
                cps_GM = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Cps_GM"));
            }
        }

        private double cps_Show;
        /// <summary>
        /// 计数率_根据档位选择，并且经过滑动平均用于显示的cps
        /// </summary>
        public double Cps_Show
        {
            get { return cps_Show; }
            set
            {
                cps_Show = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Cps_Show"));
            }
        }

        private double rate;
        /// <summary>
        /// 剂量率，单位总为u
        /// </summary>
        public double Rate
        {
            get { return rate; }
            set
            {
                rate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Rate"));

                //判断报警
                JudgeRateAlarm();
            }
        }

        private string rateStr = "0";
        /// <summary>
        /// 剂量率显示字符串，会根据数值大小设置单位
        /// </summary>
        public string P_rateStr
        {
            get { return rateStr; }
            set
            {
                rateStr = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("P_rateStr"));
            }
        }

        private double rateThreshold = 200;
        /// <summary>
        /// 剂量率报警阈值
        /// </summary>
        public double P_rateThreshold
        {
            get { return rateThreshold; }
            set
            {
                rateThreshold = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("P_rateThreshold"));
            }
        }

        private bool rateAlarm;
        /// <summary>
        /// 剂量率报警信号，暂时不用了
        /// </summary>
        public bool P_rateAlarm
        {
            get { return rateAlarm; }
            set
            {
                rateAlarm = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("P_rateAlarm"));
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("P_measuredTime"));
            }
        }

        private double deadTime = 0;
        /// <summary>
        /// 读回来的累计死时间，单位s
        /// </summary>
        public double P_deadTime
        {
            get { return deadTime; }
            set
            {
                deadTime = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("P_deadTime"));
            }
        }

        /// <summary>
        /// 死时间滑动平均列表，采集5次连续的死时间增量，取平均再除以采集的时间，得到实时死时间率，单位s
        /// </summary>
        private List<double> deadTimeList = new List<double>(5);

        private double deadTimeRatio = 0;
        /// <summary>
        /// 死时间实时比率
        /// </summary>
        public double DeadTimeRatio
        {
            get => deadTimeRatio = 0;
            set
            {
                deadTimeRatio = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DeadTimeRatio)));
            }
        }

        private double liveTime;
        /// <summary>
        /// 活时间
        /// </summary>
        public double P_liveTime
        {
            get { return liveTime; }
            set
            {
                liveTime = value;
                OnPropertyChanged(nameof(P_liveTime));
            }
        }


        #endregion

        #region 换挡相关数据（剂量率刻度数据）

        private byte currentGear = 1;
        /// <summary>
        /// 当前档位，取值1~4分别表示1~4档
        /// </summary>
        public byte P_currentGear
        {
            get { return currentGear; }
            set { currentGear = value; }
        }

        /// <summary>
        /// 每个档都有3个因子和档高档低值
        /// </summary>
        class GearInfo
        {
            public double switchUp;
            public double switchDown;
            public double factorA;
            public double factorB;
            public double factorC;
        }

        /// <summary>
        /// 4个档位信息，Index0~3分别表示1~4档
        /// </summary>
        GearInfo[] gears = new GearInfo[4];

        #endregion

        #region 能量刻度相关数据

        public class FactorEnergy
        {
            public double a = 0.0000358431461598145;
            public double b = 1.16704499450733;
            public double c = -10.1729357573879;
        }

        /// <summary>
        /// 能量刻度因子
        /// </summary>
        public FactorEnergy factorEnergy = new FactorEnergy();

        /// <summary>
        /// 计算所选道址的能量
        /// </summary>
        public double CalculateEnergy(double channel)
        {
            double c = channel;
            double result = c * c * factorEnergy.a + c * factorEnergy.b + factorEnergy.c;
            if (result < 0)
                result = 0;
            return Math.Round(result, 2);
        }
        private double CalculateEnergy()
        {
            return CalculateEnergy(selectedChannel);
        }

        #endregion

        #region CPS滑动平均相关的数据

        /// <summary>
        /// cps的滑动平均List
        /// </summary>
        private List<double> cpsMAList = new List<double>();

        /// <summary>
        /// 滑动平均数
        /// </summary>
        private int num_MA = 10;

        public int P_num_MA
        {
            get { return num_MA; }
            set
            {
                num_MA = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("P_num_MA"));
            }
        }


        /// <summary>
        /// 记录CPS滑动平均满足跳变条件的次数
        /// </summary>
        private byte times_ShouldJump = 0;

        /// <summary>
        /// CPS滑动平均跳变次数条件，times_ShouldJump达到这个次数就跳变
        /// </summary>
        const int num_CanJump = 3;

        #endregion

        #region 谱图控制相关数据

        private int selectedChannel = 1;
        /// <summary>
        /// 当前所选的道址
        /// </summary>
        public int SelectedChannel
        {
            get { return selectedChannel; }
            set
            {
                selectedChannel = value;
                if (selectedChannel <= 1)
                    selectedChannel = 1;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SelectedChannel"));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SelectedIndex"));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SelectedCount"));
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SelectedEnergy"));
                }
            }
        }

        /// <summary>
        /// 当前所选的Index，为道址数-1，修改后，也触发所选道址更改的事件
        /// </summary>
        public int SelectedIndex
        {
            get { return selectedChannel - 1; }
        }

        /// <summary>
        /// 当前所选道址的脉冲数
        /// </summary>
        public double SelectedCount
        {
            get
            {
                double count = multiDatas[selectedChannel - 1];
                if (count.ToString().Contains('.'))//如果脉冲数是小数（比如平滑后的数据），则保留2位小数
                    count = Math.Round(count, 2);
                return count;
            }
        }

        /// <summary>
        /// 当前所选道址的能量
        /// </summary>
        public double SelectedEnergy
        {
            get { return CalculateEnergy(); }
        }

        private int leftCutOff = 60;
        /// <summary>
        /// 左边截止的道址数
        /// </summary>
        public int LeftCutOff
        {
            get { return leftCutOff; }
            set
            {
                leftCutOff = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LeftCutOff"));
            }
        }

        #endregion

        #region 报警相关数据

        private double threshold1 = 10;
        /// <summary>
        /// 1级报警阈值，单位为u
        /// </summary>
        public double P_threshold1
        {
            get { return threshold1; }
            set
            {
                threshold1 = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("P_threshold1"));
            }
        }

        private double threshold2 = 100;
        /// <summary>
        /// 2级报警阈值，单位为u
        /// </summary>
        public double P_threshold2
        {
            get { return threshold2; }
            set
            {
                threshold2 = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("P_threshold2"));
            }
        }

        private SolidColorBrush rateColor = new SolidColorBrush(Colors.Black);
        /// <summary>
        /// 剂量率显示的颜色，可以方便地在线程中设置，从而修改颜色
        /// </summary>
        public SolidColorBrush P_rateColor
        {
            get { return rateColor; }
            set
            {
                rateColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("P_rateColor"));
            }
        }

        //注意：不能在线程中定义new SolidColorBrush，否则会报错：必须在与DependencyObject相同的线程上创建DependencySource
        SolidColorBrush color_Red = new SolidColorBrush(Colors.Red);
        SolidColorBrush color_Orange = new SolidColorBrush(Colors.Orange);
        SolidColorBrush color_Black = new SolidColorBrush(Colors.Black);

        private bool isAlarmSound = true;
        /// <summary>
        /// 开关，是否在报警时发出声音
        /// </summary>
        public bool P_isAlarmSound
        {
            get { return isAlarmSound; }
            set
            {
                isAlarmSound = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("P_isAlarmSound"));

                //当用户关闭声音报警时，强制关闭报警声音
                StopAlarmSound();
            }
        }


        #endregion

        #endregion

        #region 构造器

        public ReceDatas()
        {

        }

        public void Init()
        {
            init_Gears();
            init_FactorEnergy();
        }

        #endregion

        #region 接收数据解析分析方法

        /// <summary>
        /// 输入北京多道或Long多道返回的数据，解析多道数据，并保存到MultiDatas[]中；同时计算CPS、Rate等信息
        /// </summary>
        /// <param name="bufferAll"></param>
        public void AnalyseMultiData(byte[] bufferAll)
        {
            if (father.P_MCU == MCUType.北京)
            {
                #region 解析出多道数据

                int pulse = 0;//存储某一道的脉冲数
                for (int i = 0; i < 2048; i++)
                {
                    if (i >= leftCutOff)
                    {
                        pulse = 0;
                        pulse += bufferAll[20 + i * 4] & 0xFF;
                        pulse += (bufferAll[20 + i * 4 + 1] & 0xFF) << 8;
                        pulse += (bufferAll[20 + i * 4 + 2] & 0xFF) << 16;
                        pulse += (bufferAll[20 + i * 4 + 3] & 0xFF) << 24;
                        MultiDatas[i] = pulse;
                    }
                    else
                        MultiDatas[i] = 0;
                }

                //对256整数倍的道址的数据进行平滑。2024年8月11日
                for (int i = 255; i < 2048; i += 256)
                {
                    if (i == 2047)//最后一道取前一道数据
                    {
                        MultiDatas[i] = MultiDatas[i - 1];
                    }
                    else
                    {
                        //取左右各一道数据取平均
                        MultiDatas[i] = (MultiDatas[i] + MultiDatas[i]) / 2;
                    }
                }

                #endregion

                #region 解析出时间

                //测量时间解析
                //暂时不用这个时间，而是由外界更改
                if (false)
                {
#pragma warning disable CS0162 // 检测到无法访问的代码
                    double time = 0;
#pragma warning restore CS0162 // 检测到无法访问的代码
                    int m = 8;
                    time += (bufferAll[m++] & 0xFF);
                    time += (bufferAll[m++] & 0xFF) << 8;
                    time += (bufferAll[m++] & 0xFF) << 16;
                    time += (bufferAll[m++] & 0xFF) << 24;
                    time /= 10;
                    P_measuredTime = time;
                }

                //死时间解析，ns为单位的固定时间，为最近一秒内的死时间
                double deadTime = 0;
                int m0 = 12;
                deadTime += (bufferAll[m0++] & 0xFF);
                deadTime += (bufferAll[m0++] & 0xFF) << 8;
                deadTime += (bufferAll[m0++] & 0xFF) << 16;
                deadTime += (bufferAll[m0++] & 0xFF) << 24;
                P_deadTime += deadTime / 1e+09;//累加死时间

                //滑动平均死时间
                if (deadTimeList.Count >= 5)
                {
                    deadTimeList.RemoveAt(0);
                }
                deadTimeList.Add(deadTime / 1e+09);

                //计算实时死时间比率
                DeadTimeRatio = deadTimeList.Average() / Instance.LoopTime_S;

                //计算活时间
                P_liveTime = P_measuredTime * (1 - DeadTimeRatio);

                #endregion
            }
            else if (father.P_MCU == MCUType.龙)//Long多道返回的数据解析，bufferAll是6157字节数据
            {
                #region 解析出多道数据

                int pulse = 0;//存储某一道的脉冲数
                for (int i = 0; i < 2047; i++)
                {
                    //似乎最后一道数据有异常，就只解析到2047道，最后一道为0即可
                    if (i >= leftCutOff)
                    {
                        pulse = 0;
                        //pulse += bufferAll[1 + i * 3] & 0xFF;//这里都加上1，就是把首位校验去掉
                        //pulse += (bufferAll[1 + i * 3 + 1] & 0xFF) << 8;
                        //pulse += (bufferAll[1 + i * 3 + 2] & 0xFF) << 16;

                        pulse += bufferAll[1 + i * 3];//这里都加上1，就是把首位校验去掉
                        pulse += (bufferAll[1 + i * 3 + 1]) << 8;
                        pulse += (bufferAll[1 + i * 3 + 2]) << 16;
                        MultiDatas[i] = pulse;
                        //MultiDatas[i] = bufferAll[1 + 3 * i] + (bufferAll[1 + 3 * i + 1] << 8) + (bufferAll[1 + 3 * i + 2] << 16);
                    }
                    else
                        MultiDatas[i] = 0;
                }

                #endregion

                #region Long多道其他参数解析

                /**获取末尾的参数部分*/
                byte[] paraPart = new byte[14];

                //就取最后14个字节
                int startIndex = bufferAll.Length - 14 - 1 - 1;
                Array.Copy(bufferAll, startIndex, paraPart, 0, 14);

                ///**获取高压*/
                //HV = 0;
                //HV += paraPart[10] & 0xFF;
                //HV += (paraPart[11] & 0xFF) << 8;
                //HV = HV * 1100 / 4096 * 1.0002f + 2.2012f;

                ///**获取温度*/
                //temperature = 0;
                //temperature += (paraPart[9] & 0xFF);
                //temperature += (paraPart[8] & 0xFF) << 8;
                //if (temperature > 0 && temperature <= 65530)
                //    temperature = temperature / 65535 * 175 - 45;
                //else
                //    temperature = 0;

                ///**死时间计算*/
                //countFastSlow = 0;
                //countFastSlow += (paraPart[4] & 0xFF);
                //countFastSlow += (paraPart[5] & 0xFF) << 8;
                //countFastSlow += (paraPart[6] & 0xFF) << 16;
                //countFastSlow += (paraPart[7] & 0xFF) << 24;
                //if (countFastSlow == 0)
                //    deadTime = 0;
                //else
                //{
                //    /**上升时间和平顶时间在参数配置的方法中就赋值好了
                //     *try {
                //     *                 mRiseTime = Float.parseFloat(MCUConfigActivity.riseTime);
                //     *                 mFlatTime = Float.parseFloat(MCUConfigActivity.flatTime);
                //     *             } catch (Exception e) {//如果转化失败，也就是说没有打开过参数配置，则设置为默认值：
                //     *                 mRiseTime = 1f;
                //     *                 mFlatTime = 0.8f;
                //     *             } */

                //    float shapingTime = (2 * (2 * mRiseTime + mFlatTime) - mRiseTime) / 1000000;
                //    deadTime = countFastSlow * shapingTime / loopCircle * 1000;
                //}//这里只负责把死时间的数据更新，活时间的计算、活时间死时间的显示放在更新时间的定时器中

                ///**获取GM管的CPS*/
                //cps2 = 0;
                //cps2 += paraPart[12] & 0xFF;
                //cps2 += (paraPart[13] & 0xFF) << 8;
                //cps2 = cps2 / (loopCircle / 1000d);
                ////从接收数据获得的最后两个字节数据是这一段时间的总脉冲数，需要除以采集周期才是计数率

                #endregion
            }
            else if (father.P_MCU == MCUType.强)
            {
                //解析多道数据
                int count = bufferAll.Length / 4;//道址总数
                byte[] temp = new byte[4];
                for (int i = 0; i < count; i++)
                {
                    if (i >= leftCutOff)
                    {
                        //每4个字节是一道的脉冲数，为4字节整数
                        for (int j = 0; j < 4; j++)//把4个字节存到字节数组
                        {
                            //temp[3 - j] = datas[i * 4 + j];
                            temp[j] = bufferAll[i * 4 + j];
                        }
                        uint pulse = BitConverter.ToUInt32(temp, 0);
                        MultiDatas[i] = pulse;
                    }
                    else
                        MultiDatas[i] = 0;
                }

                //对256整数倍的道址的数据进行平滑。2024年8月18日
                for (int i = 255; i < 2048; i += 256)
                {
                    if (i == 2047)//最后一道取前一道数据
                    {
                        MultiDatas[i] = MultiDatas[i - 1];
                    }
                    else
                    {
                        //取左右各一道数据取平均
                        MultiDatas[i] = (MultiDatas[i] + MultiDatas[i]) / 2;
                    }
                }
            }

            ////解析完后，设置一下SelectedChannel，用来触发脉冲数和能量的更新
            //SelectedChannel = selectedChannel;

            //解析完了数据，多道数据更新了，这里就更新一下所选道址的脉冲数
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SelectedCount"));

            //通过仪器读回数据，更新了多道数据，更新标志位
            MainWindow.Instance.autoRun.P_isDataNew = true;
        }

        /// <summary>
        /// 在解析完多道数据、CPS数据、时间等参数后，计算CPS和Rate
        /// </summary>
        public void UpdateCPSRate()
        {
            GetCPS();
            GetRate();
        }

        /// <summary>
        /// 上一次的总计数，用来计算每次采集的计数值之差
        /// </summary>
        double countLast = 0;

        /// <summary>
        /// 解析接收的数据后，执行这个方法，可以计算所有的CPS数据
        /// </summary>
        private void GetCPS()
        {
            //计算由多道数据得到的计数率cps
            double count = multiDatas.Sum();
            //double totalSeconds = (DateTime.Now - father.autoRun.P_startTime).TotalSeconds;//计算时间为当前时间减去开始测量时间
            double totalSeconds = measuredTime;// 测量时间
            cps_Multi = count / totalSeconds;
            cps_Multi = 0;//现在不用总脉冲数除以总时间计算cps

            //cps的另一种算法：算每次测量增加的值，算出来的是实时的cps，现在就采用这种算法
            if (countLast != 0)//如果上次计数值为0，则不以此计算cps，否则可能会出现非常大的值
            {
                double diff = count - countLast;
                double time = Instance.LoopTime_S;
                cps_Multi = diff / time;
            }
            //用完上次的总计数，就赋值为新的总计数
            countLast = count;

            //往往刚开始的第一次，这个时间太小，算出的cps太大，所以把这一次剔除
            if (totalSeconds < 1)
                return;

            //cps_GM已在解析方法中获得

            //根据档位选择哪个cps：
            double cps;
            if (currentGear < 3)
                cps = cps_Multi;//低档选择多道出来的数据
            else
                cps = cps_GM;//高档选择GM管的数据

            #region cps滑动平均

            //先判断是否满足跳变条件：
            if (cps > 3 && cpsMAList.Count > 0 && (cps > cpsMAList.Average() * 2 || cps < cpsMAList.Average() / 2))
            {
                times_ShouldJump++;

                if (times_ShouldJump >= num_CanJump - 1)//满足跳变的次数达到，则清空List
                {
                    times_ShouldJump = 0;
                    cpsMAList.Clear();
                }
            }

            //如果小于10个数据，则给list添加，否则，先除去第一个，再添加
            if (cpsMAList.Count < num_MA)
            {
                cpsMAList.Add(cps);
            }
            else
            {
                cpsMAList.RemoveAt(0);
                cpsMAList.Add(cps);
            }

            //赋值到用于显示的CPS
            Cps_Show = cpsMAList.Average();

            #endregion
        }

        /// <summary>
        /// 解析接收的数据后，执行这个方法，可以计算Rate并判断报警
        /// </summary>
        private void GetRate()
        {
            #region 换挡逻辑

            //由于不同情况下，判断升降档的cps不同，所以这里定义用于判断换挡的cps，可根据不同情况赋不同值
            double cpsJudging = cps_Multi;//这里的赋值无所谓

            //升档部分：
            for (int i = 0; i < 3; i++)
            {//升档循环上限为3次

                //只要cps_GM>40，就强制到3挡往上，以防止出现，本来cps_GM很高，剂量率很高，但NaI堵塞，cps_Multi值很小，就升不上去档的情况
                if (cps_GM > 40 && currentGear < 3)
                {
                    currentGear = 3;
                    cpsMAList.Clear();
                }

                //选择用cps_Multi还是cps_GM判断升档条件
                if (currentGear == 1)
                {//如果1挡升2挡，则用cps_Multi判断
                    cpsJudging = cps_Multi;
                }
                else
                {//否则，2升3、3升4是用cps_GM判断
                    cpsJudging = cps_GM;
                }

                //判断换挡条件
                if (cpsJudging > gears[currentGear - 1].switchUp && currentGear < 4)
                {
                    //如果cps高于当前的档高值，则升档；另外，已经是4档时就不升了
                    currentGear++;
                    cpsMAList.Clear();//若成功换挡，则剂量率计算公式会瞬间改变，而cps经过滑动平均还没有及时改变，所以换挡后需要清除滑动平均
                }
                else
                {
                    //如果成功升档了，则继续进行升档判断；如果没有升档，则停止当前的循环，往后进行
                    break;
                }

                //UI上显示一下档位信息：
                Console.WriteLine("当前档位：" + currentGear);

            }

            //降档部分：
            for (int i = 0; i < 3; i++)
            {//降档循环上限为3次

                //选择用cps_Multi还是cps_GM判断升档条件
                if (currentGear == 2)
                {//如果2挡降1挡，则用cps_Multi判断
                    cpsJudging = cps_Multi;
                }
                else
                {//否则用cps_GM判断
                    cpsJudging = cps_GM;
                }

                //判断换挡条件
                if (cpsJudging <= gears[currentGear - 1].switchDown && currentGear > 1)
                {
                    //如果cps低于当前的档高值，则降档；另外，已经是1档时就不降了
                    currentGear--;
                    cpsMAList.Clear();
                }
                else
                {
                    //如果成功降档了，则继续进行降档判断；如果没有升档，则停止当前的循环，往后进行
                    break;
                }

                //UI上显示一下档位信息：
                Console.WriteLine("当前档位：" + currentGear);
            }

            #endregion

            #region 用当前档位的因子计算剂量率

            if (cpsMAList.Count != 0)//这里判断一下滑动平均是否为空，若为空，则说明换过档，那这一次的cps数据是错的，不能计算剂量率
            {
                Rate = gears[currentGear - 1].factorA * cps_Show * cps_Show +
                        gears[currentGear - 1].factorB * cps_Show +
                        gears[currentGear - 1].factorC;
                if (Rate < 0)
                {
                    Rate = 0;
                }
            }

            #endregion

            #region 判断剂量率报警，放到别处去判断了

            //if (rate > rateThreshold)
            //    P_rateAlarm = true;
            //else
            //    P_rateAlarm = false;

            #endregion

            #region 生成剂量率带单位的字符串

            string unit;
            double showedRate;
            if (rate < 1000)
            {
                unit = "uSv/h";
                showedRate = rate;
            }
            else if (rate < 1000000)
            {
                showedRate = rate / 1000;
                unit = "mSv/h";
            }
            else
            {
                showedRate = rate / 1000000;
                unit = "Sv/h";
            }
            if (true)//单位后是否添加档位信息
                unit += "(" + currentGear + ")";
            P_rateStr = showedRate.ToString("0.00") + unit;

            #endregion
        }

        #endregion

        #region 刻度因子初始化方法

        /// <summary>
        /// 初始化档位信息，也即剂量率刻度信息
        /// </summary>
        private void init_Gears()
        {
            for (int i = 0; i < gears.Length; i++)
            {
                gears[i] = new GearInfo();
            }

            try
            {
                string filePath = Directory.GetCurrentDirectory() + "\\Resources\\刻度因子\\剂量率刻度因子及切换点.txt";
                string[] lines = File.ReadAllLines(filePath);
                string[] strs;
                int gearNum = 1;//标志数据是那一档，取值为1~4
                foreach (string line in lines)
                {
                    strs = line.Split(new char[] { '=', '#' });//拆解一行的内容

                    if (strs.Length == 2)
                    {
                        if (strs[1].Equals("档"))//遇到档位标识
                        {
                            gearNum = int.Parse(strs[0]);
                        }
                        else//遇到参数值
                        {
                            string paraName = strs[0];
                            string valueStr = strs[1];
                            double.TryParse(valueStr, out double dValue);
                            switch (paraName)
                            {
                                case "a":
                                    gears[gearNum - 1].factorA = dValue;
                                    break;
                                case "b":
                                    gears[gearNum - 1].factorB = dValue;
                                    break;
                                case "c":
                                    gears[gearNum - 1].factorC = dValue;
                                    break;
                                case "down":
                                    gears[gearNum - 1].switchDown = dValue;
                                    break;
                                case "up":
                                    gears[gearNum - 1].switchUp = dValue;
                                    break;
                                default:
                                    break;
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("剂量率因子加载失败，详情：\r\n" + ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 从文件读取能量刻度因子
        /// </summary>
        private void init_FactorEnergy()
        {
            try
            {
                string filePath = Directory.GetCurrentDirectory() + "\\Resources\\刻度因子\\能量刻度因子.txt";
                string[] lines = File.ReadAllLines(filePath);
                string[] strs;
                foreach (string line in lines)
                {
                    strs = line.Split(new char[] { '=' });//拆解一行的内容

                    if (strs.Length == 2)
                    {
                        string paraName = strs[0];
                        string valueStr = strs[1];
                        double.TryParse(valueStr, out double dValue);
                        switch (paraName)
                        {
                            case "a":
                                factorEnergy.a = dValue;
                                break;
                            case "b":
                                factorEnergy.b = dValue;
                                break;
                            case "c":
                                factorEnergy.c = dValue;
                                break;
                            default:
                                break;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("能量刻度因子加载失败，详情：\r\n" + ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region 剂量率报警方法

        AndyPlaySound sound = new AndyPlaySound();

        /// <summary>
        /// 判断剂量率的报警状态。在剂量率被改变时执行。
        /// </summary>
        public void JudgeRateAlarm()
        {
            //判断是几级报警，并设置颜色
            if (rate >= threshold2)
                P_rateColor = color_Red;
            else if (rate >= threshold1)
                P_rateColor = color_Orange;
            else
                P_rateColor = color_Black;

            //若报警，则播放声音
            if (P_rateColor != color_Black && isAlarmSound)
                sound.AlarmPlayStart("报警声.wav");
            else
                sound.AlarmPlayStop();
        }

        /// <summary>
        /// 直接停止报警声音，用于：停止测量时、关闭报警声音时
        /// </summary>
        public void StopAlarmSound()
        {
            sound.AlarmPlayStop();
        }

        #endregion

        #region 其他方法

        /// <summary>
        /// 点清除按钮时，清空相关测量数据
        /// </summary>
        public void ClearDatas()
        {
            Cps_Multi = 0;
            Cps_GM = 0;
            Cps_Show = 0;
            Rate = 0;
            P_rateStr = "0";
            //P_rateAlarm = false;//报警清掉了

            //多道数据不用清，下次画图的时候就会用新数据了
        }

        #endregion
    }
}
