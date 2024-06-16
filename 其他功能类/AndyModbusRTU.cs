using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using 核素识别仪.自定义控件;

namespace 核素识别仪.其他功能类
{
    public class AndyModbusRTU
    {
        #region Constructor

        public AndyModbusRTU()
        {

        }

        #endregion

        #region 数据

        //private static Lazy<AndyModbusRTU> instance = new Lazy<AndyModbusRTU>(() => { return new AndyModbusRTU(); });
        //public static AndyModbusRTU Instance
        //{
        //    get
        //    {
        //        return instance.Value;
        //    }
        //}

        /// <summary>
        /// 用于Modbus传输的串口对象
        /// </summary>
        public AndySerialPort andySP;

        #endregion

        #region Modbus校验函数

        /// <summary>
        /// 校验modbus03、04功能读取回来的响应帧是否正确(包含检验从机地址、功能码、字节数、crc)
        /// </summary>
        /// <param name="send_ByteArray">请求帧字节数组</param>
        /// <param name="rece_ByteArray">响应帧字节数组</param>
        /// <returns></returns>
        public bool ModbusCheck0304(byte[] send_ByteArray, byte[] rece_ByteArray)
        {
            bool checkSucceeded = true;
            //校验接收字节长度（至少是5），才能进行之后的校验
            if (rece_ByteArray.Length >= 5)
            {
                //校验从机地址、功能码是否相符
                checkSucceeded &= rece_ByteArray[0] == send_ByteArray[0] && rece_ByteArray[1] == send_ByteArray[1];

                //校验字节数是否是寄存器数量的2倍
                byte[] tempBytes = new byte[2] { send_ByteArray[5], send_ByteArray[4] };
                int byteNum = BitConverter.ToUInt16(tempBytes, 0) * 2;
                checkSucceeded &= rece_ByteArray[2] == byteNum;

                //校验CRC
                byte[] crc = crc16(rece_ByteArray, rece_ByteArray.Length - 3);
                checkSucceeded &= crc[0] == rece_ByteArray[rece_ByteArray.Length - 2] && crc[1] == rece_ByteArray[rece_ByteArray.Length - 1];
                //校验接收字节总数是否正确
                checkSucceeded &= rece_ByteArray[2] == rece_ByteArray.Length - 5;
            }
            else
                checkSucceeded = false;
            return checkSucceeded;
        }

        /// <summary>
        /// 校验modbus03、04功能读取回来的响应帧是否正确(包含检验从机地址、功能码、字节数、crc)
        /// </summary>
        /// <param name="send_ByteArray">请求帧字节数组</param>
        /// <param name="rece_ByteArray">响应帧字节数组</param>
        /// <param name="data">接收到的数据部分</param>
        /// <returns></returns>
        public bool ModbusCheck0304(byte[] send_ByteArray, byte[] rece_ByteArray, out byte[] data)
        {
            bool checkSucceeded = true;
            //校验接收字节长度（至少是5），才能进行之后的校验
            if (rece_ByteArray.Length >= 5)
            {
                //校验从机地址、功能码是否相符
                checkSucceeded &= rece_ByteArray[0] == send_ByteArray[0] && rece_ByteArray[1] == send_ByteArray[1];

                //校验字节数是否是寄存器数量的2倍
                byte[] tempBytes = new byte[2] { send_ByteArray[5], send_ByteArray[4] };
                int byteNum = BitConverter.ToUInt16(tempBytes, 0) * 2;
                checkSucceeded &= rece_ByteArray[2] == byteNum;

                //根据字节数，摘取数据部分
                data = new byte[byteNum];
                if (rece_ByteArray.Length < byteNum + 5)//加5是因为，有5个字节是非数据部分，也即前3个字节、最后2个字节
                    Console.WriteLine("错误46848:接收数据长度不足");
                else
                    Array.Copy(rece_ByteArray, 3, data, 0, byteNum);

                //校验CRC
                byte[] crc = crc16(rece_ByteArray, rece_ByteArray.Length - 3);
                checkSucceeded &= crc[0] == rece_ByteArray[rece_ByteArray.Length - 2] && crc[1] == rece_ByteArray[rece_ByteArray.Length - 1];
                //校验接收字节总数是否正确
                checkSucceeded &= rece_ByteArray[2] == rece_ByteArray.Length - 5;
            }
            else
            {
                checkSucceeded = false;
                data = new byte[] { };
            }
            return checkSucceeded;
        }

        /// <summary>
        /// 校验modbus05、10功能读取回来的响应帧是否正确(包含检验从机地址、功能码、字节数、crc)
        /// </summary>
        /// <param name="send_ByteArray">请求帧字节数组</param>
        /// <param name="rece_ByteArray">响应帧字节数组</param>
        /// <returns></returns>
        public bool ModbusCheck0510(byte[] send_ByteArray, byte[] rece_ByteArray)
        {
            bool checkSucceeded = true;
            //校验接收字节长度（固定为8）
            if (rece_ByteArray.Length == 8)
            {
                //校验前6个字节是否相同
                for (int i = 0; i < 6; i++)
                {
                    checkSucceeded &= rece_ByteArray[i] == send_ByteArray[i];
                }
                //校验CRC
                byte[] crc = crc16(rece_ByteArray, rece_ByteArray.Length - 3);
                checkSucceeded &= crc[0] == rece_ByteArray[rece_ByteArray.Length - 2] && crc[1] == rece_ByteArray[rece_ByteArray.Length - 1];
            }
            else
                checkSucceeded = false;
            return checkSucceeded;
        }


        /// <summary>
        /// 计算crc16
        /// </summary>
        /// <param name="data">要计算的字节数组</param>
        /// <param name="num">要算到的最大下标（字节数减1）</param>
        /// <returns>返回2字节数组，低地址存crc16低字节，高存高，一般要先加低字节</returns>
        private byte[] crc16(byte[] data, int num)
        {
            //函数：计算一个字符串的crc16，字符串按字节装入data[]里，左边的字符装在数组低地址；num表示要处理到的最大下标，为所计算的字节数减一
            //string strr = "";
            long CrcJ;
            int i, j;
            CrcJ = 65535;
            for (i = 0; i <= num; i++)
            {
                CrcJ = CrcJ ^ data[i];
                for (j = 0; j <= 7; j++)
                {
                    if (CrcJ % 2 == 1)
                    {
                        CrcJ = CrcJ / 2;
                        CrcJ = CrcJ ^ 40961;
                    }
                    else
                        CrcJ = CrcJ / 2;
                }
            }
            ////strr = Convert.ToInt64(CrcJ, 0x10).ToString();
            //strr = CrcJ.ToString("X2");
            //for (int k = strr.Length; k < 4; k++)
            //    strr = "0" + strr;
            //strr = strr.Substring(2, 2) + strr.Substring(0, 2);
            //return strr;
            byte[] crc = BitConverter.GetBytes((ushort)CrcJ);//直接返回2个字节的数组，crc16low在低字节，crc16high在高字节
            //其实返回的是8个字节数组，但只用低2位即可，想返回两位需要给GetBytes函数一个short型的参数，而计算crc函数中CrcJ定义为long
            return crc;
        }

        /// <summary>
        /// CRC校验，到时候在串口中断里先判断从机地址，然后crc校验，校验成功，就认为是Modbus的帧
        /// </summary>
        /// <param name="byteArray">需要校验的帧的字节数组</param>
        /// <returns>返回是否校验成功</returns>
        bool ModbusCheckCRC(byte[] byteArray)
        {
            bool checkSucceeded = true;
            byte[] crc = crc16(byteArray, byteArray.Length - 3);
            checkSucceeded &= crc[0] == byteArray[byteArray.Length - 2] && crc[1] == byteArray[byteArray.Length - 1];
            return checkSucceeded;
        }

        /// <summary>
        /// 给一帧数据添加CRC16校验位
        /// </summary>
        /// <param name="byteList">输入需要校验的list</param>
        void ModbusAddCRC(List<byte> byteList)
        {
            byte[] crc = crc16(byteList.ToArray(), byteList.Count - 1);
            byteList.Add(crc[0]);
            byteList.Add(crc[1]);
        }

        #endregion

        #region 作为从机，对保持寄存器的配置

        #region modbus保持寄存器读写流程总结

        /*电脑存了两个文件，一个是“Modbus保持寄存器变量信息”，存放着所有变量的起始地址、名称、寄存器数、数据类型，这个文件的内容的生成方法：
         * Excel中按照这个顺序编辑好寄存器表，直接复制到txt文件中（会包含\t字符）；
         * 只会在初始化时读一次，根据它配置modbusDatas这个list。除了这里读一次，就再也不会用到这个文件
         * 
         * 另外一个文件是“Modbus保持寄存器数据”，存放着保持寄存器的数据，会在初始化程序中读一次，然后添加到临时内存的holdingRegister这个list中；
         * 另外就是会在多种情况下，把内存中的保持寄存器保存到文件中，多种情况包括：程序结束时、处理06、10功能码时。
         * 
         * 内存中的holdingRegister得到文件中的数据后，就是保持寄存器数据的暂存处了。它会用于：更新modbusDatas的value字段、03、06、10功能码的处理
         * 
         * 另外，内存中的两个变量：modbusDatas、holdingReg，他们之间是有关系的，知道其中一个，另一个就是应该确定了的
         * 
         * modbusDatas变量，存储了所有数据的值，可以方便地读或写变量的值
         * 
         * 应对modbus数据地址不从0开始以及数据间断：保持寄存器不一定要从地址0开始存modbus数据
         */

        #endregion

        #region 数据

        /// <summary>
        /// 自定义类，用来记录modbus读写过程中的数据，包含名称、起始地址、长度、数据类型
        /// </summary>
        public class ModbusDataInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public MainWindow Father { get; set; }

            public string name;
            public ushort address;
            public ushort registerLength;
            /// <summary>
            /// 数据类型，可以为“整型”、“浮点型”、“二进制”、“字符串”、“日期”
            /// </summary>
            public string dataType;
            private object valuee;//变量的值


            /// <summary>
            /// 定义属性，用于绑定到UI。这个值使用时，记得先判空。
            /// </summary>
            public object Valuee
            {
                get { return valuee; }
                set
                {
                    valuee = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Valuee"));
                    }
                }
            }

        }

        //定义一个list，存储保持寄存器的数据信息，可以从txt文件加载，这个其实就是和DCS通信的数据信息
        List<ModbusDataInfo> modbusDatas_DCS = new List<ModbusDataInfo>();
        //定义一个list，存储保持寄存器的数据
        List<byte> holdingReg = new List<byte>();

        #endregion

        /// <summary>
        /// 从文件读取modbus数据信息+从文件读取保持寄存器的保存值
        /// </summary>
        void init_HoldingRegister()
        {
            //try
            //{
            //    //根据文件，获取保持寄存器的数据信息，存在modbusDatas中备用
            //    string filePath = exepath + "\\Resources\\Modbus保持寄存器变量信息.txt";//这个文件只会读，不会写。
            //    string[] lines = File.ReadAllLines(filePath);
            //    foreach (string line in lines)
            //    {
            //        string[] strs = line.Split(new string[] { "\t", " " }, StringSplitOptions.RemoveEmptyEntries);//用单元格分隔符或空格切开一行字符串

            //        ushort add = ushort.Parse(strs[0], System.Globalization.NumberStyles.HexNumber);//获得初始地址
            //        ushort len = ushort.Parse(strs[2]);//获得寄存器长度

            //        object initValue = 0;//用于存放初始值，初始值存在了文件里
            //        if (strs.Length > 4)//如果有初始值，才进行如下赋值，否则赋值为0
            //        {
            //            int length = len * 2;
            //            //根据变量的数据类型，给value赋初值，初值为
            //            switch (strs[3])
            //            {
            //                case "整型":
            //                    switch (length)
            //                    {
            //                        case 2:
            //                            short.TryParse(strs[4], out short ss);
            //                            initValue = ss;
            //                            break;
            //                        case 4:
            //                            int.TryParse(strs[4], out int ii);
            //                            initValue = ii;
            //                            break;
            //                        case 8:
            //                            long.TryParse(strs[4], out long ll);
            //                            initValue = ll;
            //                            break;
            //                    }
            //                    break;
            //                case "浮点型":
            //                    switch (length)
            //                    {
            //                        case 4:
            //                            float.TryParse(strs[4], out float ff);
            //                            initValue = ff;
            //                            break;
            //                        case 8:
            //                            double.TryParse(strs[4], out double dd);
            //                            initValue = dd;
            //                            break;
            //                    }
            //                    break;
            //                case "二进制":
            //                    break;
            //            }
            //        }

            //        modbusDatas_DCS.Add(new ModbusDataInfo() { address = add, name = strs[1], registerLength = len, dataType = strs[3], Valuee = initValue });
            //    }

            //    #region 读取保存保持寄存器数据的文件，获得保持寄存器的数据（字节数组newBytes）；若文件中数据存的不足，则取变量的默认值转化为字节数据

            //    //从文件读取保持寄存器的字节数据
            //    string filePath2 = exepath + "\\Resources\\Modbus保持寄存器数据.txt";
            //    byte[] holdRegOld = File.ReadAllBytes(filePath2);//从文件读回来的之前保存的保持寄存器数据
            //    byte[] holdRegNew = VariableSyncToHoldReg();//这里要根据初始化的value数据设置一个新的保持寄存器数据，它可能会比读回来的要多

            //    Array.Copy(holdRegOld, holdRegNew, holdRegOld.Length);//如果文件中原本存着数据，则取出上次保存好的数据，覆盖到新的字节数组，而不用初始化数据

            //    //有可能这个文件里还没有数据，或字节数比保持寄存器的少，则用新生成的数组填充文件
            //    if (holdRegOld.Length < holdRegNew.Length)
            //    {
            //        File.WriteAllBytes(filePath2, holdRegNew);//把新的字节数组写进去
            //    }

            //    #endregion

            //    //newBytes的字节数没有问题，把它更新到字段holdingReg中
            //    holdingReg.AddRange(holdRegNew);

            //    //保持寄存器数据读取完成后，再让变量的值（即modbusDatas中所有元素的value字段）跟着更新一遍
            //    HoldRegSyncToVariable(true);

            //}

            //catch (Exception ex)
            //{
            //    tb_Log.AppendText("\r\n" + DateTime.Now.ToString() + "加载Modbus变量失败，详情：" + ex.Message);
            //}
        }

        /// <summary>
        /// 将保持寄存器的数据存到二进制文件中
        /// </summary>
        void SaveHoldingRegister()//目前用于三个地方：开机初始化保持寄存器读取数据后，06、10命令保持寄存器更改数据后
        {
            //string filePath2 = exepath + "\\Resources\\Modbus保持寄存器数据.txt";
            //File.WriteAllBytes(filePath2, holdingReg.ToArray());
        }

        #region 保持寄存器和Modbus变量之间的互换

        /// <summary>
        /// 保持寄存器的数据更新到变量（修改modbusDatas的元素的value字段），在保持寄存器数据变化时执行
        /// </summary>
        /// <param name="allOrNot">是否全部更新，true表示全部更新，false表示只更新参数，不更新读取数据</param>
        void HoldRegSyncToVariable(bool allOrNot)//目前用于三个地方：开机初始化保持寄存器读取数据后，06、10命令保持寄存器更改数据后
        {
            //这里将所有变量根据变量信息，从HoldReg读取并转化数据
            if (allOrNot)
            {
                ModbusDataInfo firstData = modbusDatas_DCS[0];//取第一个Modbus变量

                for (int i = 0; i < modbusDatas_DCS.Count; i++)//遍历modbus数据信息集合，从保持寄存器读取数据，并存到每个modbus数据的value里
                {
                    //先把变量所需的所有字节取出来
                    int start = modbusDatas_DCS[i].address * 2 - firstData.address * 2;//数据在保持寄存器中的起始地址
                    int length = modbusDatas_DCS[i].registerLength * 2;//数据的字节长度

                    byte[] data = new byte[length];
                    for (int j = 0; j < length; j++)
                    {
                        data[j] = holdingReg[start + length - 1 - j];//逆序取出字节数据
                    }

                    //数据转换，同时赋值
                    switch (modbusDatas_DCS[i].dataType)
                    {
                        case "整型":
                            switch (length)
                            {
                                case 1:
                                    //SetModbusDataValue(i, data);
                                    modbusDatas_DCS[i].Valuee = data[0];
                                    break;
                                case 2:
                                    short ss = BitConverter.ToInt16(data, 0);
                                    //SetModbusDataValue(i, ss);
                                    modbusDatas_DCS[i].Valuee = ss;
                                    break;
                                case 4:
                                    int ii = BitConverter.ToInt32(data, 0);
                                    //SetModbusDataValue(i, ii);
                                    modbusDatas_DCS[i].Valuee = ii;
                                    break;
                                case 8:
                                    long ll = BitConverter.ToInt64(data, 0);
                                    //SetModbusDataValue(i, ll);
                                    modbusDatas_DCS[i].Valuee = ll;
                                    break;
                            }
                            break;
                        case "浮点型":
                            switch (length)
                            {
                                case 4:
                                    float ff = BitConverter.ToSingle(data, 0);
                                    //modbusDatas_DCS[i].Valuee = ff;
                                    modbusDatas_DCS[i].Valuee = (double)(decimal)ff;//即使是float型，也在程序中存储为double型，会比较方便

                                    break;
                                case 8:
                                    double dd = BitConverter.ToDouble(data, 0);
                                    //SetModbusDataValue(i, dd);
                                    modbusDatas_DCS[i].Valuee = dd;
                                    break;
                            }
                            break;
                        case "二进制":
                            break;
                    }

                }
            }
        }

        /// <summary>
        /// 根据modbusDatas的value，生成对应的字节List，就是新的HoldReg，用于设置holdReg的值
        /// </summary>
        byte[] VariableSyncToHoldReg()
        {

            //根据变量信息，算出保持寄存器需要的字节数，字节数应为最大的起始地址加该变量的字节长度
            ModbusDataInfo firstData = modbusDatas_DCS[0];//取第一个Modbus变量
            ModbusDataInfo lastData = modbusDatas_DCS[modbusDatas_DCS.Count - 1];//取最后一个Modbus变量，一般来说是按照Modbus地址顺序排列的

            //算出最多需要多少个字节，应该是最后一个地址减第一个地址，再加上最后一个变量的长度
            int neededLength = (lastData.address - firstData.address + lastData.registerLength) * 2;

            byte[] results = new byte[neededLength];
            List<byte> result = new List<byte>();

            for (int i = 0; i < modbusDatas_DCS.Count; i++)//遍历modbus数据信息集合
            {
                //先把变量所需的所有字节取出来
                int start = modbusDatas_DCS[i].address * 2 - firstData.address * 2;//数据在保持寄存器中的起始地址
                int length = modbusDatas_DCS[i].registerLength * 2;//数据的字节长度
                object value = modbusDatas_DCS[i].Valuee;

                byte[] tempBytes = new byte[] { };

                //把value数据转换字节，添加上去
                switch (modbusDatas_DCS[i].dataType)
                {
                    case "整型":
                        switch (length)
                        {
                            case 1:
                                tempBytes = new byte[] { (byte)value };
                                break;
                            case 2:
                                tempBytes = BitConverter.GetBytes((short)value);
                                //result.AddRange(tempBytes.Reverse());
                                break;
                            case 4:
                                tempBytes = BitConverter.GetBytes((int)value);
                                //result.AddRange(tempBytes.Reverse());
                                break;
                            case 8:
                                tempBytes = BitConverter.GetBytes((long)value);
                                //result.AddRange(tempBytes.Reverse());
                                break;
                        }
                        break;
                    case "浮点型":
                        switch (length)
                        {
                            case 4:
                                if (value.GetType() == typeof(float))
                                {
                                    tempBytes = BitConverter.GetBytes((float)value);
                                }
                                else if (value.GetType() == typeof(double))
                                {

                                    float ff = Convert.ToSingle(value);
                                    tempBytes = BitConverter.GetBytes(ff);
                                }

                                break;
                            case 8:
                                tempBytes = BitConverter.GetBytes((double)value);
                                break;
                        }
                        break;
                    case "二进制":
                        tempBytes = new byte[1];
                        break;
                }

                //向数组的指定位置设置数据
                Array.Copy(tempBytes.Reverse().ToArray(), 0, results, start, length);

            }

            return results;
        }

        /// <summary>
        /// 用于modbusDatas_DCS的四个数据（I131当量、总惰气浓度、总γ剂量率、Np浓度）被设置后，将HoldReg更新
        /// </summary>
        void FourDataSettedToHoldReg()
        {
            byte[] newReg = VariableSyncToHoldReg();
            holdingReg.Clear();
            holdingReg.AddRange(newReg);
        }

        #endregion

        #region 处理主机请求帧的方法

        /// <summary>
        /// 判断modbus请求帧是否有“非法数据地址”错误
        /// </summary>
        /// <returns>若有错，则返回list的count不为0，返回返回list；否则，没有错误</returns>
        List<byte> ModbusJudgeError02(byte fCode, ushort startAdd, ushort registerLength, ushort firstAdd, ushort lastAdd)
        {
            List<byte> result = new List<byte>();
            if (startAdd < firstAdd || startAdd + registerLength - 1 > lastAdd || registerLength * 2 > holdingReg.Count)
            {
                result.Add(1);//这里要根据实际情况改从机地址
                result.Add((byte)(fCode + 0x80));
                result.Add(2);//信息码2，表示字节长度错误
                ModbusAddCRC(result);
            }
            return result;
        }

        /// <summary>
        /// 处理03功能码的请求帧
        /// </summary>
        /// <param name="byteArray">请求帧内容</param>
        /// <returns>响应帧，帧字节数错误，则返回null；若访问字节数超出保持寄存器长度，则返回018302XX</returns>
        List<byte> ModbusDeel03(byte[] byteArray)
        {
            List<byte> result = new List<byte>();

            //判断一下请求帧的字节数是否正确——到时候放到外面
            if (byteArray.Length != 8)
            {
                return null;
            }

            //已经判断过从机地址、功能码、crc校验，这里直接取起始地址、寄存器长度即可。
            byte[] bytes2 = new byte[2];
            //读取起始地址
            bytes2[0] = byteArray[3];
            bytes2[1] = byteArray[2];
            UInt16 startAdd = BitConverter.ToUInt16(bytes2, 0);
            //读取寄存器长度
            bytes2[0] = byteArray[5];
            bytes2[1] = byteArray[4];
            UInt16 registerLength = BitConverter.ToUInt16(bytes2, 0);

            //本地保持寄存器数据的最开始地址和最后地址
            ushort firstAdd = modbusDatas_DCS[0].address;
            ushort lastAdd = (ushort)(modbusDatas_DCS[modbusDatas_DCS.Count - 1].address + modbusDatas_DCS[modbusDatas_DCS.Count - 1].registerLength);

            //判断访问的字节数是否超过保持寄存器的长度
            List<byte> errorSendList = ModbusJudgeError02(0x03, startAdd, registerLength, firstAdd, lastAdd);
            if (errorSendList.Count > 0)
                return errorSendList;

            //从保持寄存器获取数据，准备好响应帧
            result.Add(byteArray[0]);
            result.Add(byteArray[1]);
            result.Add((byte)(registerLength * 2));
            for (int i = 0; i < registerLength * 2; i++)
            {
                result.Add(holdingReg[(startAdd - firstAdd) * 2 + i]);
            }
            ModbusAddCRC(result);

            return result;
        }

        /// <summary>
        /// 处理06功能码的请求帧
        /// </summary>
        /// <param name="byteArray">请求帧内容</param>
        /// <returns>响应帧，不会出现异常情况</returns>
        List<byte> ModbusDeel06(byte[] byteArray)
        {
            List<byte> result = new List<byte>();

            //已经判断过从机地址、功能码、crc校验，这里直接取起始地址、寄存器长度即可。
            byte[] bytes2 = new byte[2];
            //读取起始地址
            bytes2[0] = byteArray[3];
            bytes2[1] = byteArray[2];
            UInt16 startAdd = BitConverter.ToUInt16(bytes2, 0);

            //本地保持寄存器数据的最开始地址和最后地址
            ushort firstAdd = modbusDatas_DCS[0].address;
            ushort lastAdd = (ushort)(modbusDatas_DCS[modbusDatas_DCS.Count - 1].address + modbusDatas_DCS[modbusDatas_DCS.Count - 1].registerLength);

            //将请求帧的设置值更新到保持寄存器
            holdingReg[(startAdd - firstAdd) * 2] = byteArray[4];
            holdingReg[(startAdd - firstAdd) * 2 + 1] = byteArray[5];

            //将保持寄存器的数据更新到对应变量
            HoldRegSyncToVariable(true);

            //更改的数据保存到二进制文件中（在程序正常关闭时也会保存）
            SaveHoldingRegister();//06功能码

            //准备响应帧
            result.Add(byteArray[0]);
            result.Add(byteArray[1]);
            result.Add(byteArray[2]);
            result.Add(byteArray[3]);
            result.Add(byteArray[4]);
            result.Add(byteArray[5]);
            ModbusAddCRC(result);

            return result;
        }

        /// <summary>
        /// 处理10功能码的请求帧
        /// </summary>
        /// <param name="byteArray">请求帧内容</param>
        /// <returns>响应帧，若字节长度不等于寄存器数量的2倍，返回null；若访问字节数超出保持寄存器长度，则返回019002XX</returns></returns>
        List<byte> ModbusDeel10(byte[] byteArray)
        {
            List<byte> result = new List<byte>();

            //已经判断过从机地址、功能码、crc校验，这里直接取起始地址、寄存器长度即可。
            byte[] bytes2 = new byte[2];
            //读取起始地址
            bytes2[0] = byteArray[3];
            bytes2[1] = byteArray[2];
            UInt16 startAdd = BitConverter.ToUInt16(bytes2, 0);
            //读取寄存器长度
            bytes2[0] = byteArray[5];
            bytes2[1] = byteArray[4];
            UInt16 registerLength = BitConverter.ToUInt16(bytes2, 0);
            //读取字节长度
            byte length = byteArray[6];

            //判断字节长度是否正确
            if (length != registerLength * 2)
            {
                return null;
            }

            //本地保持寄存器数据的最开始地址和最后地址
            ushort firstAdd = modbusDatas_DCS[0].address;
            ushort lastAdd = (ushort)(modbusDatas_DCS[modbusDatas_DCS.Count - 1].address + modbusDatas_DCS[modbusDatas_DCS.Count - 1].registerLength);

            //判断访问的字节数是否超过保持寄存器的长度
            List<byte> errorSendList = ModbusJudgeError02(0x10, startAdd, registerLength, firstAdd, lastAdd);
            if (errorSendList.Count > 0)
                return errorSendList;

            //将请求帧的设置值更新到保持寄存器
            for (int i = 0; i < length; i++)
            {
                holdingReg[(startAdd - firstAdd) * 2 + i] = byteArray[7 + i];
            }
            //将保持寄存器的数据更新到对应变量
            HoldRegSyncToVariable(true);

            //更改的数据保存到二进制文件中（在程序正常关闭时也会保存）
            SaveHoldingRegister();//10功能码

            //准备响应帧
            result.Add(byteArray[0]);
            result.Add(byteArray[1]);
            result.Add(byteArray[2]);
            result.Add(byteArray[3]);
            result.Add(byteArray[4]);
            result.Add(byteArray[5]);
            ModbusAddCRC(result);

            return result;
        }

        #endregion

        #endregion

        #region 请求帧生成方法

        /// <summary>
        /// 0304请求帧生成方法
        /// </summary>
        /// <param name="slaveAdd">从机号</param>
        /// <param name="funCode">功能码，只能为03或04</param>
        /// <param name="startAdd">起始地址</param>
        /// <param name="regLength">寄存器长度</param>
        /// <returns></returns>
        public byte[] Get0304Request(byte slaveAdd, byte funCode, ushort startAdd, ushort regLength)
        {
            try
            {
                byte[] tempBytes;

                List<byte> sendBytes = new List<byte>();//存发送的字节

                sendBytes.Add(slaveAdd);
                sendBytes.Add(funCode);

                tempBytes = BitConverter.GetBytes(startAdd);
                sendBytes.Add(tempBytes[1]);
                sendBytes.Add(tempBytes[0]);

                tempBytes = BitConverter.GetBytes(regLength);
                sendBytes.Add(tempBytes[1]);
                sendBytes.Add(tempBytes[0]);

                ModbusAddCRC(sendBytes);

                return sendBytes.ToArray();
            }
            catch (Exception)
            {
                return new byte[] { };
            }
        }

        /// <summary>
        /// 生成设置保持寄存器的请求帧。
        /// </summary>
        /// <param name="slaveAdd">从机地址</param>
        /// <param name="funCode">功能码10</param>
        /// <param name="startAdd">起始地址</param>
        /// <param name="regLength">寄存器长度</param>
        /// <param name="datas">要设置的modbus数据，必须为地址连续的modbus变量</param>
        /// <returns></returns>
        public byte[] Get10Request(byte slaveAdd, byte funCode, ushort startAdd, ushort regLength, List<ModbusDataInfo> datas)
        {
            try
            {
                byte[] tempBytes;
                List<byte> sendBytes = new List<byte>();//存发送的字节

                #region 请求帧固定内容

                sendBytes.Add(slaveAdd);
                sendBytes.Add(funCode);

                tempBytes = BitConverter.GetBytes(startAdd);
                sendBytes.Add(tempBytes[1]);
                sendBytes.Add(tempBytes[0]);

                tempBytes = BitConverter.GetBytes(regLength);
                sendBytes.Add(tempBytes[1]);
                sendBytes.Add(tempBytes[0]);

                sendBytes.Add((byte)(regLength * 2));

                #endregion

                #region 设置的数据内容

                //临时数组
                byte[] ba = new byte[] { };

                for (int i = 0; i < datas.Count; i++)
                {
                    ModbusDataInfo data = datas[i];
                    ushort byteNum = (ushort)(data.registerLength * 2);
                    object setValue = data.Valuee;

                    //设置内容：
                    switch (data.dataType)
                    {
                        case "整型":
                            switch (byteNum)
                            {
                                case 2:
                                    ushort ui2 = Convert.ToUInt16(setValue);
                                    ba = BitConverter.GetBytes(ui2);
                                    break;
                                case 4:
                                    uint ui4 = Convert.ToUInt32(setValue);
                                    ba = BitConverter.GetBytes(ui4);
                                    break;
                                case 8:
                                    ulong ui8 = Convert.ToUInt64(setValue);
                                    ba = BitConverter.GetBytes(ui8);
                                    break;
                            }
                            break;
                        case "浮点型":
                            switch (byteNum)
                            {
                                case 4:
                                    float ff = Convert.ToSingle(setValue);
                                    ba = BitConverter.GetBytes(ff);
                                    break;
                                case 8:
                                    double dd = Convert.ToDouble(setValue);
                                    ba = BitConverter.GetBytes(dd);
                                    break;
                            }
                            break;
                        case "字符串":
                            byte[] bytes = Encoding.Default.GetBytes((string)setValue);
                            int numIn = bytes.Length;//用户输入的字符长度
                            int numOut = Convert.ToInt32(byteNum);//准备发送的字符长度
                            int numMin = Math.Min(numIn, numOut);//取二者的较小值用于赋值
                            ba = new byte[numOut];
                            //有可能会凑不满字节数，也可能会多出去。若凑不满要发送字节数，则不够的就保持为0；若超出要发送的字节数，就舍去多出来的字符
                            Array.Copy(bytes, ba, numMin);
                            ba = ba.Reverse().ToArray();
                            break;
                    }

                    //将准备好的数组反着加到list中
                    for (int j = ba.Length - 1; j >= 0; j--)
                    {
                        sendBytes.Add(ba[j]);
                    }
                }

                #endregion

                ModbusAddCRC(sendBytes);

                return sendBytes.ToArray();
            }
            catch (Exception)
            {
                return new byte[] { };
            }
        }

        #endregion

        #region 数据解析方法

        /// <summary>
        /// 提供一个modbus数据信息，以及包含其数据的一组Modbus返回数据，解析出它的值方法。
        /// </summary>
        /// <param name="mData">modbus数据信息</param>
        /// <param name="dataPart">包含此modbus数据的响应帧的数据部分（不带开头0103XX这3个字节）</param>
        /// <param name="startAddAll">包含此modbus数据的响应帧的总的起始地址</param>
        /// <returns></returns>
        public object AnalyseModbusData(ModbusDataInfo mData, byte[] dataPart, ushort startAddAll)
        {
            object result = null;

            //变量的字节数
            int byteNumber = mData.registerLength * 2;

            //获取每个变量数据在读取数据串中所在的开始字节Index：自己的地址减去Modbus开始地址，再乘2
            int startByteIndex = (mData.address - startAddAll) * 2;

            //把数据从响应帧中摘出来
            byte[] bb = new byte[byteNumber];
            Array.Copy(dataPart, startByteIndex, bb, 0, byteNumber);

            //根据不同的数据类型进行解析
            result = AnalyseOneData(mData.dataType, byteNumber, bb);

            mData.Valuee = result;

            return result;
        }

        /// <summary>
        /// 输入一个数据的类型、二进制数组、字节长度，返回该数据的值
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="byteNumber">数据字节长度</param>
        /// <param name="bb">数据对应的二进制数组</param>
        /// <returns>返回object型的结果，可根据dataType强制转化；失败的话返回null</returns>
        public object AnalyseOneData(string dataType, int byteNumber, byte[] bb)
        {
            object result = null;
            try
            {
                switch (dataType)
                {
                    case "整型":
                        {
                            switch (byteNumber)
                            {
                                case 2:
                                    {
                                        ushort ui16 = BitConverter.ToUInt16(bb.Reverse().ToArray(), 0);
                                        result = ui16;
                                        break;
                                    }
                                case 4:
                                    {
                                        uint ui32 = BitConverter.ToUInt32(bb.Reverse().ToArray(), 0);
                                        result = ui32;
                                        break;
                                    }
                                case 8:
                                    {
                                        ulong ui64 = BitConverter.ToUInt64(bb.Reverse().ToArray(), 0);
                                        result = ui64;
                                        break;
                                    }
                            }
                            break;
                        }
                    case "浮点型":
                        {
                            //考虑8个字节和4个字节的情况
                            switch (byteNumber)
                            {
                                case 4:
                                    {
                                        float ff = BitConverter.ToSingle(bb.Reverse().ToArray(), 0);
                                        result = ff;
                                        break;
                                    }
                                case 8:
                                    {
                                        double dd = BitConverter.ToDouble(bb.Reverse().ToArray(), 0);
                                        result = dd;
                                        break;
                                    }
                            }
                            break;
                        }
                    case "字符串":
                        string resultStr = Encoding.ASCII.GetString(bb);//ASCII转化后的字符串
                        result = resultStr.Replace("\0", "");//除去某些字符串结尾的\0，否则不方便从表格复制
                        break;
                    case "日期":
                        try//这个日期可能传回来的是错的，例如月份超过了12。这种情况就返回一个默认值
                        {
                            DateTime date = new DateTime(bb[0] + 2000, bb[1], bb[2], bb[3], bb[4], bb[5]);
                            result = date;
                        }
                        catch (Exception ex)
                        {
                            _ = ex.Message;
                            result = new DateTime();
                        }
                        break;
                }
                return result;
            }
            catch (Exception ex)
            {
                _ = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// 发送Modbus请求帧，并读取数据、校验。
        /// </summary>
        /// <param name="request">请求帧</param>
        /// <param name="timeOutMs">等待读取的超时时间</param>
        /// <returns>若校验成功，则返回数据部分，否则返回null</returns>
        public byte[] ModbusSendAndRece(byte[] request, int timeOutMs)
        {
            if (andySP == null || !andySP._serialPort.IsOpen)
                return null;

            //发送请求帧并等待接收
            andySP._serialPort.DiscardInBuffer();//这里，准备采集数据前，先把串口的接收区清空，防止存在的字节数影响之后的读取
            andySP.SPSend(request);
            byte[] receBytes = (byte[])andySP.WaitSPRece(AndySerialPort.SPReceiveType.Byte, timeOutMs);
            if (receBytes == null)
                return null;

            //校验一下，并摘除数据部分
            byte[] data = new byte[] { };
            bool ok;
            //自动判断功能码，执行相应的校验方法
            if (request[1] == 0x03 || request[1] == 0x04)
                ok = ModbusCheck0304(request, receBytes, out data);
            else if (request[1] == 0x10 || request[1] == 0x05)
                ok = ModbusCheck0510(request, receBytes);
            else
                ok = false;

            if (ok)//如果校验成功，则返回数组，即使是空数组，也是校验成功，对于05 10指令校验成功就是返回空数组
                return data;
            else//如果校验成功，则返回null
                return null;
        }

        #endregion
    }
}
