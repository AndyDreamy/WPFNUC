using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace 核素识别仪.自定义控件
{
    /// <summary>
    /// 更新时间：2023年6月16日10:54:16
    /// </summary>
    public partial class AndySerialPort : UserControl
    {
        #region 全局变量

        /// <summary>
        /// 使用者
        /// </summary>
        public MainWindow Father { get; set; }

        /// <summary>
        /// 用来显示串口提示的tb，想用的话可以在外面对它赋值
        /// </summary>
        public TextBox tb_Log = new TextBox();

        #endregion

        public AndySerialPort()
        {
            InitializeComponent();
            init_SerialPort();
        }

        /*先添加一个串口，名称为_serialPort；按照myChart中的串口模块设计画面，控件名称设置为一样的，然后直接使用以下函数（除了串口接收中断函数和串口助手部分）*/

        public SerialPort _serialPort = new SerialPort();

        //定义一个全局的bool，用来判断是否需要在串口接收框显示接收的数据
        public bool isSPReceRecord = false;
        public bool IsSPReceRecord
        {
            get { return isSPReceRecord; }
            set { isSPReceRecord = value; }
        }


        /// <summary>
        /// 添加电脑已连接的串口号，添加波特率的选项
        /// </summary>
        public void init_SerialPort()
        {
            int i = 0;//循环变量，每个循环使用前都需要清零，使用后无需清零
            string[] ports = SerialPort.GetPortNames();//定义数组的方法、获取串口号的函数

            #region 串口设计界面ComboBox的初始化

            //串口号
            for (i = 0; i < ports.Length; i++)
                cb_SerialPortNumber.Items.Add(ports[i]);
            cb_SerialPortNumber.SelectedIndex = i - 1;

            //波特率
            cb_BaudRate.Items.Add("4800");
            cb_BaudRate.Items.Add("9600");
            cb_BaudRate.Items.Add("19200");
            cb_BaudRate.Items.Add("115200");
            cb_BaudRate.Items.Add("460800");
            cb_BaudRate.Items.Add("921600");
            cb_BaudRate.SelectedIndex = 4;

            //停止位
            cb_StopBits.Items.Add("None");
            cb_StopBits.Items.Add("One");
            cb_StopBits.Items.Add("Two");
            cb_StopBits.Items.Add("OnePointFive");
            cb_StopBits.SelectedIndex = 1;

            //数据位
            cb_DataBits.Items.Add("8");
            cb_DataBits.Items.Add("7");
            cb_DataBits.Items.Add("6");
            cb_DataBits.Items.Add("5");
            cb_DataBits.SelectedIndex = 0;

            //校验位
            cb_Parity.Items.Add("None");
            cb_Parity.Items.Add("Odd");
            cb_Parity.Items.Add("Even");
            //cb_Parity.Items.Add("Mark");
            //cb_Parity.Items.Add("Space");
            cb_Parity.SelectedIndex = 0;

            #endregion

            _serialPort.ReadBufferSize = 4096;//决定了串口一次最多接收的字节数
            _serialPort.ReceivedBytesThreshold = 1;//决定接收缓存区有至少多少字节时才触发串口中断

            //自动发送定时器
            //timer_SPAutoSend.Tick += Timer_SPAutoSend_Tick;

            //添加串口接收中断
            //_serialPort.DataReceived += _serialPort_DataReceived;
            //让外界设置这个中断处理方法

        }

        /// <summary>
        /// 打开串口前，对串口参数进行设置
        /// </summary>
        public void setPara_SerialPort()
        {
            _serialPort.PortName = cb_SerialPortNumber.Text;
            _serialPort.BaudRate = Convert.ToInt32(cb_BaudRate.Text);
            _serialPort.DataBits = Convert.ToInt32(cb_DataBits.Text);

            switch (cb_Parity.Text)
            {
                case "None":
                    {
                        _serialPort.Parity = Parity.None;
                        break;
                    }
                case "Odd":
                    {
                        _serialPort.Parity = Parity.Odd;
                        break;
                    }
                case "Even":
                    {
                        _serialPort.Parity = Parity.Even;
                        break;
                    }
                default:
                    break;
            }

            switch (cb_StopBits.Text)
            {
                case "None":
                    {
                        _serialPort.StopBits = StopBits.None;
                        break;
                    }
                case "One":
                    {
                        _serialPort.StopBits = StopBits.One;
                        break;
                    }
                case "Two":
                    {
                        _serialPort.StopBits = StopBits.Two;
                        break;
                    }
                case "OnePointFive":
                    {
                        _serialPort.StopBits = StopBits.OnePointFive;
                        break;
                    }
                default:
                    break;
            }

            _serialPort.Encoding = Encoding.Default;
        }

        /// <summary>
        /// 扫描串口，将电脑连接的串口显示到“串口号
        /// </summary>
        public void bt_SearchSerial_Click(object sender, RoutedEventArgs e)
        {
            int i;
            string[] ports = SerialPort.GetPortNames();//定义数组的方法、获取串口号的函数
            cb_SerialPortNumber.Items.Clear();
            for (i = 0; i < ports.Length; i++)
                cb_SerialPortNumber.Items.Add(ports[i]);
            cb_SerialPortNumber.SelectedIndex = i - 1;
        }

        /// <summary>
        /// 打开或关闭串口按钮
        /// </summary>
        public void switchSerialPort_Click(object sender, RoutedEventArgs e)
        {
            string str_SerialSwitch = bt_SerialSwitch.Content.ToString();
            if (str_SerialSwitch == "打开串口")
            {
                try
                {
                    setPara_SerialPort();//打开串口时设置各类参数
                    _serialPort.Open();
                    //_serialPort.DataReceived += _serialPort_DataReceived;//添加接受事件
                    bt_SerialSwitch.Content = "关闭串口";
                    tb_switchStatus.Text = "串口已打开";
                    e_status.Fill = new SolidColorBrush(Colors.LightGreen);
                    #region 打开串口后，禁止修改串口参数
                    cb_BaudRate.IsEnabled = false;
                    cb_SerialPortNumber.IsEnabled = false;
                    cb_StopBits.IsEnabled = false;
                    cb_Parity.IsEnabled = false;
                    cb_DataBits.IsEnabled = false;
                    bt_SearchSerial.IsEnabled = false;
                    cb_SerialPortNumber.ToolTip = "打开串口后才能修改";
                    #endregion

                }
                catch (Exception)
                {
                    MessageBox.Show("串口不存在或被占用！", "失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                //_serialPort.DataReceived -= _serialPort_DataReceived;//删除接收事件
                _serialPort.Close();
                bt_SerialSwitch.Content = "打开串口";
                tb_switchStatus.Text = "串口已关闭";
                e_status.Fill = new SolidColorBrush(Colors.Red);
                #region 关闭串口后，允许修改串口参数
                cb_BaudRate.IsEnabled = true;
                cb_SerialPortNumber.IsEnabled = true;
                cb_StopBits.IsEnabled = true;
                cb_Parity.IsEnabled = true;
                cb_DataBits.IsEnabled = true;
                bt_SearchSerial.IsEnabled = true;
                cb_SerialPortNumber.ToolTip = null;
                #endregion
            }
        }

        /// <summary>
        /// 串口发送函数
        /// </summary>
        /// <param name="send_str">发送内容的字符串形式</param>
        /// <param name="bystring">true表示字符串发送，false表示字节发送</param>
        public void SPSend(string send_str, bool bystring)
        {
            if (_serialPort.IsOpen == true)
            {
                if (bystring == true)//字符串发送
                {
                    _serialPort.Write(send_str);
                    Console.WriteLine("串口发送字节数:" + send_str.Length / 2);
                }
                else//字节发送
                {
                    try
                    {
                        if (send_str.Length % 2 == 1)//若为奇数个字符，则在最前面补个0
                        {
                            send_str = "0" + send_str;
                        }
                        int send_len = send_str.Length / 2;
                        byte[] send_buffer = new byte[send_len];
                        //循环转化到字节数组
                        for (int i = 0; i < send_len; i++)
                        {
                            send_buffer[i] = Convert.ToByte(send_str.Substring(i * 2, 2), 16);//16表示十六进制
                        }
                        //发送数组
                        _serialPort.Write(send_buffer, 0, send_len);
                        Console.WriteLine("串口发送字节数:" + send_buffer.Length);
                    }
                    catch (Exception)
                    {
                        //MessageBox.Show("串口发送失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);//因为这个函数要循环执行，所以不能弹窗
                        tb_Log.Text += "\r\n" + DateTime.Now.ToString() + "串口发送失败";

                    }


                    //接收时，接收数组低字节放字符串左边的内容，低字节先接收
                    //那么发送的时候，也应该数组低字节放左边的，且先发左边的

                }

                //以下是把每次发送的都写在发送框里，最后不用可以注释
                //if (bystring == true)
                //    tb_SendingData.Text += "“" + send_str + "” ";
                //else
                //    tb_SendingData.Text += send_str + " ";

                //发送完成，在右上角消息处给个提示
                //tb_Log.Text+=DateTime.Now.ToString()+ "串口发送完成";

            }
            else
            {
                tb_Log.Text += "\r\n" + DateTime.Now.ToString() + "串口未打开，无法发送";
            }
        }
        public void SPSend(byte[] bytes)
        {
            if (_serialPort.IsOpen == true)
            {
                _serialPort.Write(bytes, 0, bytes.Length);
                Console.WriteLine("串口发送字节数:" + bytes.Length);
            }
            else
            {
                tb_Log.Text += "\r\n" + DateTime.Now.ToString() + "串口未打开，无法发送";
            }
        }

        /// <summary>
        /// 串口接收中断函数，最完整版本，里面可以根据bool型变量选择使用哪些功能
        /// </summary>
        public void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (isSPReceRecord != true)
                {
                    return;
                }
                //delay(1);//等待设备发送数据——这里似乎不用等待也可以接收完全

                #region 串口接收到的第一手数据

                int rece_len = _serialPort.BytesToRead;//读取字节数
                byte[] receBuffer = new byte[rece_len];//接收字节数组
                //if (rece_len<5)//可以通过这种方式，让串口接收中断触发了，但先不读取，等到缓存区字节数到某个值后才读取
                //{
                //    return;
                //}
                _serialPort.Read(receBuffer, 0, rece_len);//如果不执行Read指令，则串口接收缓存区会不断地积累数据；执行Read后，会将读取到的数据从缓存区清除
                string rece_String = BitConverter.ToString(receBuffer, 0, rece_len).Replace("-", "");//接收字节数组显示为字符串
                string rece_ASCIIString = System.Text.Encoding.Default.GetString(receBuffer, 0, rece_len);//ASCII转化后的字符串

                #endregion

                #region 暂时不用的

                #region 以下为将串口接收的内容写在接收框里

                //if (isSPReceRecord)
                //{
                //    //串口读取时一定是按照字节读，但可以选择按照字节显示为字符串或按照ASCII显示为字符串
                //    string stringtosee = "";

                //    //WPF方法：
                //    bool bystring = false;
                //    Dispatcher.Invoke(new MethodInvoker(delegate
                //    {
                //        if (cb_SPReceFormat.SelectedIndex == 0)//16进制
                //            bystring = false;
                //        else//字符串
                //            bystring = true;
                //    }));
                //    if (bystring == true)
                //        stringtosee = rece_ASCIIString;
                //    else
                //        stringtosee = rece_string;
                //    Dispatcher.Invoke(new MethodInvoker(delegate { this.tb_SPReceData.Text += DateTime.Now.ToString() + "-------------------\r\n" + stringtosee + "\r\n"; }));

                //    ////Winforms方法：
                //    //if (check_stringRece.Checked == true)//似乎，在中断线程可以直接读取CheckBox的值
                //    //    stringtosee = rece_ASCIIString;
                //    //else
                //    //    stringtosee = rece_string;
                //    //Invoke(new EventHandler(delegate { this.tb_ReceivedData.Text += DateTime.Now.ToString() + "-------------------\r\n" + stringtosee + "\r\n"; }));
                //    ////这是中断线程中申请修改主线程TextBox的指令Invoke

                //}

                #endregion

                #region 靳第一个软件总结的串口经验

                ///*对于串口发、收、处理这个流程：正常发，发完到数据处理之前，要等待想要的数据都从发到串口缓存区了，这里需要一个延时，延时
                // * 时间主要取决于想要的数据长度，不要循环等待字节数正确，容易出现死循环，就用延时最好；然后进行处理，处理前进行校验，可以
                // 先看看字节数对不对，然后对帧的内容进行校验，若失败，可以放弃数据，若成功，则处理数据。下一次把数据处理做成一个函数，实打实
                //地放在发送数据的延时后面，明确数据处理完成后再进行其他操作，而不用等待数据处理这个过程了，不要用串口接收事件了。
                //功能码可以沿用，这样一个数据处理就涵盖了所有的功能，而且可以用功能码是否为0来标志数据处理是否成功*/

                #endregion

                #endregion
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new System.Windows.Forms.MethodInvoker(delegate
                {
                    tb_Log.Text += "\r\n" + DateTime.Now.ToString() + "串口接收错误，详情：" + ex.Message + "\r\n";
                }));

            }
        }

        /// <summary>
        /// 串口接收函数，返回的是收到的字符串
        /// </summary>
        public string SPReceive_ASCIIString()
        {
            //try

            #region 串口接收到的第一手数据
            int rece_len = _serialPort.BytesToRead;//读取字节数
            byte[] rece_buffer = new byte[rece_len];//接收字节数组
            _serialPort.Read(rece_buffer, 0, rece_len);
            string rece_string = BitConverter.ToString(rece_buffer, 0, rece_len).Replace("-", "");//接收字节数组显示为字符串
            string rece_ASCIIString = Encoding.Default.GetString(rece_buffer, 0, rece_len);//ASCII转化后的字符串

            return rece_ASCIIString;
            #endregion
        }

        /// <summary>
        /// 串口接收函数，返回的是收到的字节数组，数组长度即为串口接收字节长度
        /// </summary>
        public byte[] SPReceive_Byte()
        {
            //try

            #region 串口接收到的第一手数据
            int rece_len = _serialPort.BytesToRead;//读取字节数
            byte[] rece_buffer = new byte[rece_len];//接收字节数组
            _serialPort.Read(rece_buffer, 0, rece_len);
            string rece_string = BitConverter.ToString(rece_buffer, 0, rece_len).Replace("-", "");//接收字节数组显示为字符串
            string rece_ASCIIString = System.Text.Encoding.ASCII.GetString(rece_buffer, 0, rece_len);//ASCII转化后的字符串

            return rece_buffer;
            #endregion
        }

        /// <summary>
        /// 串口接收函数，返回的是收到的原本字节形式的字符串
        /// </summary>
        public string SPReceive_ByteString()
        {
            //try

            #region 串口接收到的第一手数据
            int rece_len = _serialPort.BytesToRead;//读取字节数
            byte[] rece_buffer = new byte[rece_len];//接收字节数组
            _serialPort.Read(rece_buffer, 0, rece_len);
            string rece_string = BitConverter.ToString(rece_buffer, 0, rece_len).Replace("-", "");//接收字节数组显示为字符串
            string rece_ASCIIString = System.Text.Encoding.ASCII.GetString(rece_buffer, 0, rece_len);//ASCII转化后的字符串

            return rece_string;
            #endregion
        }

        /// <summary>
        /// 串口接收数据的种类
        /// </summary>
        public enum SPReceiveType
        {
            Byte,
            ASCIIString,
            ByteString
        }
        /// <summary>
        /// 串口接收数据，根据输入的类型，返回不同类型的数据。
        /// 若失败，则返回null。
        /// 若没有读到数据，则返回空数组或空字符串
        /// </summary>
        /// <param name="type">想要的数据类型</param>
        /// <returns></returns>
        public object SPReceive(SPReceiveType type)
        {
            object result;

            if (!_serialPort.IsOpen)//串口未打开，返回空
                return null;

            try
            {
                //串口接收到的第一手数据
                int rece_len = _serialPort.BytesToRead;//读取字节数
                byte[] rece_buffer = new byte[rece_len];//接收字节数组
                _serialPort.Read(rece_buffer, 0, rece_len);
                string rece_string = BitConverter.ToString(rece_buffer, 0, rece_len).Replace("-", "");//接收字节数组显示为字符串
                string rece_ASCIIString = System.Text.Encoding.ASCII.GetString(rece_buffer, 0, rece_len);//ASCII转化后的字符串

                Console.WriteLine("接收完成，字节数：" + rece_buffer.Length);

                switch (type)
                {
                    case SPReceiveType.Byte:
                        result = rece_buffer;
                        break;
                    case SPReceiveType.ASCIIString:
                        result = rece_ASCIIString;
                        break;
                    case SPReceiveType.ByteString:
                        result = rece_string;
                        break;
                    default:
                        result = null;
                        break;
                }
            }
            catch (Exception)
            {
                result = null;
            }

            return result;
        }

        /// <summary>
        /// 等待串口接收，直到有新数据来并且数据接收完全，最多等5s
        /// </summary>
        /// <param name="type">串口接收的形式</param>
        /// <param name="timeOutMs">超时时间（ms）</param>
        /// <returns>若接收失败，则返回null</returns>
        public object WaitSPRece(SPReceiveType type, int timeOutMs)
        {
            //读取间隔(ms)
            const int ReadInterval = 25;

            if (!_serialPort.IsOpen)
                return null;

            object result = null;

            int lastNum = -1;

            int noChangeTimes = 0;//接收字节数不变的次数

            DateTime start = DateTime.Now;
            Console.WriteLine("正在读取数据...");
            while ((DateTime.Now - start).TotalMilliseconds < timeOutMs && _serialPort.IsOpen)//这个循环中，有可能会串口中断
                                                                                              //while (_serialPort.IsOpen)//这个循环中，有可能会串口中断
            {
                if (_serialPort.BytesToRead > lastNum)
                {
                    if (_serialPort.BytesToRead != 0)//如果当前接收字节数大于之前的，并且不等于0，则记录这个接收数，进行下一次判断
                        lastNum = _serialPort.BytesToRead;
                    //System.Windows.Forms.Application.DoEvents();
                    //Console.WriteLine("接收中：" + lastNum);

                    noChangeTimes = 0;//重新计次

                    //这里需要有一些延时，才能分辨出下一次接收缓冲区的不同
                    delay(ReadInterval);//经过测试，10ms合适
                    //delay(Father.P_tempInt);//测试程序
                }
                else//如果当前接收字节数等于之前的，则认为收到了，并且收完了，就停止
                {
                    if (noChangeTimes < 3)//判断连续3次，接收字节数不变，则认为接收完成。这部分挺有用的
                    {
                        noChangeTimes++;
                        delay(5);
                        Console.WriteLine("串口接收不变次数：" + noChangeTimes);
                        continue;
                    }
                    else
                    {
                        //接收完全了
                        Console.WriteLine("用时：" + (DateTime.Now - start).TotalMilliseconds + "ms");
                        result = SPReceive(type);
                        //if (type == SPReceiveType.Byte)
                        //{
                        //    byte[] b = (byte[])result;
                        //    Console.WriteLine("接收完成，字节数：" + b.Length);
                        //}

                        break;
                    }
                }
            }
            Console.WriteLine("循环结束");

            return result;
        }

        #region 串口热插拔更新串口号

        /**使用说明：
         如果想使用串口热插拔事件，就需要把下面region内的全部复制到MainWindow，再解除注释，这个串口对象命名为andySP*/
       
        //#region 串口热插拔更新串口号
        //protected override void OnSourceInitialized(EventArgs e)
        //{
        //    base.OnSourceInitialized(e);

        //    var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        //    hwndSource?.AddHook(new HwndSourceHook(WndProc));

        //}


        ///// <summary>
        ///// 串口热插拔事件上次发生的时间
        ///// </summary>
        //DateTime lastTime = new DateTime();
        //public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        //{
        //    //此函数是USB插拔事件处理方法
        //    if (msg == 0x219)//WM_DEVICECHANGE值为0x219，表示USB插拔发生
        //    {
        //        //Console.WriteLine(hwnd.ToInt64().ToString());
        //        //Console.WriteLine(msg.ToString());
        //        //Console.WriteLine(wparam.ToInt64().ToString());
        //        //Console.WriteLine(lparam.ToInt64().ToString());
        //        //Console.WriteLine(handled.ToString());
        //        //Console.WriteLine();
        //        #region 插入时执行

        //        if (wparam.ToInt64() == 32768)//插入USB
        //        {
        //            Console.WriteLine("串口插入事件");

        //            //只用于这个软件的：串口接上的时候就去自动配置一遍
        //            AutoConfig();
        //        }

        //        #endregion

        //        #region 拔出时执行

        //        else if (wparam.ToInt64() == 32772)//拔出USB
        //        {
        //            Console.WriteLine("串口拔出事件");
        //        }

        //        #endregion

        //        #region 插拔时都执行

        //        TimeSpan ts = DateTime.Now - lastTime;
        //        //判断现在距上次响应此事件的时间，有没有超过一定时间。
        //        //如果太短，则不响应这次事件；若间隔够一段时间，则响应事件，并更新lastTime
        //        if (ts.TotalMilliseconds > 900)
        //        {
        //            lastTime = DateTime.Now;

        //            andySP.串口热插拔时要做的事情();

        //            Console.WriteLine("串口插拔事件");

        //        }

        //        #endregion
        //    }
        //    return IntPtr.Zero;
        //}

        //#endregion

        //#endregion

        /// <summary>
        /// 串口发生热插拔时要做的事情
        /// </summary>
        public void 串口热插拔时要做的事情()
        {
            /*拔下来的时候，如果拔的恰好是打开的串口，或者因为通讯故障而激发了
             * 拔出事件，都会导致当前串口自动关闭（不会报错）。此时应该把串口界面
             变成未连接状态*/

            //不论是否引起串口断开，都更新串口号，这只是修改了一个combobox的items，不会影响串口状态：

            #region 更新串口号

            int i;
            string[] ports = SerialPort.GetPortNames();//定义数组的方法、获取串口号的函数
            cb_SerialPortNumber.Items.Clear();
            for (i = 0; i < ports.Length; i++)
                cb_SerialPortNumber.Items.Add(ports[i]);
            cb_SerialPortNumber.SelectedIndex = cb_SerialPortNumber.Items.Count - 1;//先把显示的串口号设置为最后一个

            #endregion

            /*判断是否引起了断开，可以直接判断此时串口是否打开，若关闭，
             * 可能是本来就关闭，也可能是拔线引起的断开。不管那种情况，都可以
             直接让串口设置界面变成未连接状态，不设置SelectedItem，ComboBox就会为最后一个*/
            if (_serialPort.IsOpen == false)
            {
                #region 让串口设置界面变成未连接状态

                bt_SerialSwitch.Content = "打开串口";
                tb_switchStatus.Text = "串口已关闭";
                e_status.Fill = new SolidColorBrush(Colors.Red);
                #region 关闭串口后，允许修改串口参数
                cb_BaudRate.IsEnabled = true;
                cb_SerialPortNumber.IsEnabled = true;
                cb_StopBits.IsEnabled = true;
                cb_Parity.IsEnabled = true;
                cb_DataBits.IsEnabled = true;
                bt_SearchSerial.IsEnabled = true;
                cb_SerialPortNumber.ToolTip = null;
                #endregion

                #endregion
            }
            else//如果此时串口还开着，那就只要把selectedItem设置为正在打开的串口名
            {
                foreach (var item in cb_SerialPortNumber.Items)
                {
                    if (item.ToString() == _serialPort.PortName)
                    {
                        cb_SerialPortNumber.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        #endregion

        #region 串口助手功能（只有UI上有相关控件才能用）

        ///// <summary>
        ///// 串口手动发送按钮
        ///// </summary>
        //public void btSPSend_Click(object sender, RoutedEventArgs e)
        //{
        //    if (_serialPort.IsOpen == true)
        //    {
        //        string send_str = tb_SPSendData.Text.Replace(" ", "");
        //        if (cb_SPSendFormat.SelectedIndex == 1)//字符串发送
        //        {
        //            SPSend(send_str, true);
        //        }
        //        else//字节发送
        //        {
        //            SPSend(send_str, false);
        //        }
        //    }
        //    else
        //        MessageBox.Show("串口未打开", "提示");
        //}

        ///// <summary>
        ///// 清除发送框按钮
        ///// </summary>
        //public void 清除发送(object sender, RoutedEventArgs e)
        //{
        //    tb_SPSendData.Text = "";
        //}

        ///// <summary>
        ///// 清除接收框按钮
        ///// </summary>
        //public void 清除接收(object sender, RoutedEventArgs e)
        //{
        //    tb_SPReceData.Text = "";
        //}

        ///// <summary>
        ///// 开启读取按钮
        ///// </summary>
        //public void bt_SPReadRecordOn_Click(object sender, RoutedEventArgs e)
        //{
        //    if (bt_SPReadRecordOn.Content.ToString() == "开启读取")//打开
        //    {
        //        bt_SPReadRecordOn.Content = "停止读取";

        //        if (bt_LongStart.Content.ToString() == "开始记录")//说明在长杆那里没有添加过_serialPort_DataReceived，这里才能添加
        //            _serialPort.DataReceived += _serialPort_DataReceived;

        //        isSPReceRecord = true;
        //    }
        //    else//关闭
        //    {
        //        bt_SPReadRecordOn.Content = "开启读取";

        //        if (bt_LongStart.Content.ToString() == "开始记录")//说明在长杆那里不需要使用_serialPort_DataReceived，这里才能移除
        //            _serialPort.DataReceived -= _serialPort_DataReceived;

        //        isSPReceRecord = false;
        //    }
        //}

        //Timer timer_SPAutoSend = new Timer();
        ///// <summary>
        ///// 串口自动发送开关CheckBox
        ///// </summary>
        //public void check_SPAutoSend_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (int.TryParse(tb_SPSendCircle.Text, out int circle))
        //    {
        //        timer_SPAutoSend.Interval = circle;
        //        timer_SPAutoSend.Enabled = (bool)check_SPAutoSend.IsChecked;
        //        tb_SPSendCircle.IsEnabled = !(bool)check_SPAutoSend.IsChecked;
        //    }
        //    else
        //        MessageBox.Show("发送周期格式设置错误", "失败", MessageBoxButton.OK, MessageBoxImage.Error);
        //}

        ///// <summary>
        ///// 串口自动发送定时器
        ///// </summary>
        //public void Timer_SPAutoSend_Tick(object sender, EventArgs e)
        //{
        //    string send_str = tb_SPSendData.Text.Replace(" ", "");
        //    if (cb_SPSendFormat.SelectedIndex == 1)//字符串发送
        //    {
        //        SPSend(send_str, true);
        //    }
        //    else//字节发送
        //    {
        //        SPSend(send_str, false);
        //    }
        //}

        #endregion

        /// <summary>
        /// 延时n ms
        /// </summary>
        /// <param name="n">延时时间，单位ms</param>
        public void delay(int n)
        {
            //WPF方法：（Winform也能用，只不过“System.Windows.Forms.”是多余的）
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < n)
            { System.Windows.Forms.Application.DoEvents(); }

        }
    }
}
