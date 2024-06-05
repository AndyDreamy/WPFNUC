using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DataTable = System.Data.DataTable;
using Excel = Microsoft.Office.Interop.Excel;
using Button = System.Windows.Controls.Button;

namespace 核素识别仪.其他功能类
{
    /// <summary>
    /// 更新时间：2023年6月16日11:35:15
    /// </summary>
    public class AndyFileRW
    {
        #region 集成的读写参数相关

        /// <summary>
        /// 需要保存的参数的数据类型
        /// SavingPara  是要保存的数据的类型，里面有名称、类型、数据信息，是通用的
        /// 它的Value可以绑定到别的地方，从而影响其他值，或者被影响
        /// </summary>
        public class SavingPara : DependencyObject, INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            /// 名称属性，用于在列表中查找，也会是保存在文件中的参数名称
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// 对象的类型，可以为：int double string bool 
            /// </summary>
            public string ParaType { get; set; }

            /// <summary>
            /// 给这个类的对象准备一个SetBinding方法
            /// </summary>
            public BindingExpressionBase SetBinding(DependencyProperty dp, BindingBase binding)
            {
                return BindingOperations.SetBinding(this, dp, binding);
            }

            /// <summary>
            /// 定义依赖属性ValueProperty的CLR属性
            /// </summary>
            public object Value
            {
                get { return (object)GetValue(ValueProperty); }
                set
                {
                    SetValue(ValueProperty, value);
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Value"));
                    }
                }
            }

            // 定义依赖属性ValueProperty
            public static readonly DependencyProperty ValueProperty =
                DependencyProperty.Register("Value", typeof(object), typeof(SavingPara));

            /// <summary>
            /// 将此SavingPara的Value绑定到某个对象的某属性上
            /// </summary>
            /// <param name="target">绑定目标</param>
            public void ConnectValue(object target, string targetPathName)
            {
                //这个地方，必须得显式地设置Mode=BindingMode.TwoWay，否则它默认的是Target数据变化时不更新Source的数据，这个依赖属性的默认Mode为单向的
                Binding binding = new Binding(targetPathName) { Source = target, Mode = BindingMode.TwoWay };
                this.SetBinding(SavingPara.ValueProperty, binding);
            }

        }

        /// <summary>
        /// 从文件读取参数方法
        /// 只在初始化的时候，从文件读一次参数
        /// </summary>
        /// <param name="savingParas">要保存的参数信息List</param>
        public void ReadPara(List<SavingPara> savingParas)
        {
            try
            {
                string filePath = Directory.GetCurrentDirectory() + "\\Resources\\需保存的参数.txt";
                string[] lines = File.ReadAllLines(filePath);
                string[] strs;
                foreach (string line in lines)
                {
                    strs = line.Split(new char[] { '=' });//拆解一行的内容，strs[0]为参数名，strs[1]为参数值
                    string paraName = strs[0];
                    string valueStr = strs[1];

                    SavingPara para = savingParas.Find(x => x.Name.Equals(paraName));
                    if (para != null)//没找到就不管了
                    {
                        switch (para.ParaType)
                        {
                            case "bool":
                                if (bool.TryParse(valueStr, out bool b))
                                    para.Value = b;
                                break;
                            case "int":
                                if (int.TryParse(valueStr, out int i))
                                    para.Value = i;
                                break;
                            case "double":
                                if (double.TryParse(valueStr, out double d))
                                    para.Value = d;
                                break;
                            case "string":
                                para.Value = valueStr;
                                break;
                        }
                    }

                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 向文件写参数方法
        /// 只在程序关闭的时候，向文件写一次参数
        /// </summary>
        /// <param name="savingParas">要保存的参数信息List</param>
        public void WritePara(List<SavingPara> savingParas)
        {
            try
            {
                //遍历文件中所有行
                string filePath = Directory.GetCurrentDirectory() + "\\Resources\\需保存的参数.txt";
                string[] lines = new string[savingParas.Count];
                for (int i = 0; i < lines.Length; i++)
                {
                    SavingPara paraInfo = savingParas[i];
                    //这里的paraInfo.Value可能为null
                    if (paraInfo.Value == null)
                        lines[i] = paraInfo.Name + '=';
                    else
                        lines[i] = paraInfo.Name + '=' + paraInfo.Value.ToString();
                }

                //如果上面操作有误，则不能重写内容，否则可能会丢失内容
                File.WriteAllLines(filePath, lines);

            }
            catch (Exception ex)
            {
                Console.WriteLine("保存数据失败，详情：" + ex.Message);
            }
        }

        /// <summary>
        /// 从文件读取参数方法
        /// 只在初始化的时候，从文件读一次参数
        /// </summary>
        /// <param name="savingParas">要保存的参数信息List</param>
        /// <param name="fileName">自定义的文件名，需要加后缀</param>
        public void ReadPara(List<SavingPara> savingParas, string fileName)
        {
            try
            {
                string filePath = Directory.GetCurrentDirectory() + "\\Resources\\" + fileName;
                string[] lines = File.ReadAllLines(filePath);
                string[] strs;
                foreach (string line in lines)
                {
                    strs = line.Split(new char[] { '=' });//拆解一行的内容，strs[0]为参数名，strs[1]为参数值
                    string paraName = strs[0];
                    string valueStr = strs[1];

                    SavingPara para = savingParas.Find(x => x.Name.Equals(paraName));
                    if (para != null)//没找到就不管了
                    {
                        switch (para.ParaType)
                        {
                            case "bool":
                                if (bool.TryParse(valueStr, out bool b))
                                    para.Value = b;
                                break;
                            case "int":
                                if (int.TryParse(valueStr, out int i))
                                    para.Value = i;
                                break;
                            case "double":
                                if (double.TryParse(valueStr, out double d))
                                    para.Value = d;
                                break;
                            case "string":
                                para.Value = valueStr;
                                break;
                        }
                    }

                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 向文件写参数方法
        /// 只在程序关闭的时候，向文件写一次参数
        /// </summary>
        /// <param name="savingParas">要保存的参数信息List</param>
        /// /// <param name="fileName">自定义的文件名，需要加后缀</param>
        public void WritePara(List<SavingPara> savingParas, string fileName)
        {
            try
            {
                //遍历文件中所有行
                string filePath = Directory.GetCurrentDirectory() + "\\Resources\\" + fileName;
                string[] lines = new string[savingParas.Count];
                for (int i = 0; i < lines.Length; i++)
                {
                    SavingPara paraInfo = savingParas[i];
                    //这里的paraInfo.Value可能为null
                    if (paraInfo.Value == null)
                        lines[i] = paraInfo.Name + '=';
                    else
                        lines[i] = paraInfo.Name + '=' + paraInfo.Value.ToString();
                }

                //如果上面操作有误，则不能重写内容，否则可能会丢失内容
                File.WriteAllLines(filePath, lines);

            }
            catch (Exception ex)
            {
                Console.WriteLine("保存数据失败，详情：" + ex.Message);
            }
        }

        /* 使用步骤：
         * 如果有需要保存的参数，一般这个参数都是一个对象的属性，那么，明确这个对象、属性名(string)，
         * 然后新建一个SavingPara对象，设置其Name和Type，再执行其ConnectValue方法，就都准备好了
         * 在外界执行WritePara和ReadPara方法，就可以在文件读写这个参数，读的时候会更新到那个对象的属性，
         * 写的时候会根据那个对象的当前属性值写
         * 
         * 只需要引用AndyFileRW类即可，无需创建其实例，因为这里面的方法都是static的
         * 
         * 使用示例：
        
         

     #region 寻峰相关因子

            propertyNames = new string[] {
                "P_minHeight",
                "P_maxWidth",
                "P_juanNum",
                "P_sigma",
                "P_smoothTimes",
            };
            saveNames = new string[] {
                "最小峰高",
                "最大峰宽",
                "卷积向两边计算的数据个数",
                "高斯函数参数σ",
                "高斯滤波进行平滑的次数",
            };
            types = new string[]
            {
                "double",
                "double",
                "int",
                "double",
                "int",
            };
            fileName = "寻峰相关因子.txt";

            //设置两个对象：
            connectObject = andySeekPeak;//所保存的属性所在的对象
            savingParaList = savingParas_SeekPeak;//自己定义的相应的savingPara对象

            #region 这部分是一样的，进行初始化和绑定

            for (int i = 0; i < saveNames.Length; i++)
            {
                sn = saveNames[i];
                pn = propertyNames[i];
                type = types[i];

                //根据阈值名称创建一个新的SavingPara对象
                para = new SavingPara() { Name = sn, ParaType = type };

                //将其Value属性绑定到某个数据的属性上，属性名就与name相同
                para.ConnectValue(connectObject, pn);

                //这一步绑定时，如果对象没有这个属性，则这个SavingPara对象的Value会为null，之后可能会出错
                savingParaList.Add(para);
            }

            //只在初始化的时候，从文件读一次参数
            andyFileRW.ReadPara(savingParaList, fileName); 

            #endregion

        }

        /// <summary>
        /// 保存所有参数，在程序关闭时执行一次
        /// </summary>
        private void SaveParas()
        {
            andyFileRW.WritePara(savingParas_StabPeak, "稳峰相关因子.txt");
            andyFileRW.WritePara(savingParas_Other);
            andyFileRW.WritePara(savingParas_This, "Window因子.txt");
            andyFileRW.WritePara(savingParas_HuoDu, "活度计算相关\\活度计算相关因子.txt");
        }

        #endregion


         */

        #endregion

        #region Excel读写

        /// <summary>
        /// 导出为Excel功能，输入Datatable和文件名称
        /// </summary>
        /// <param name="table">存储数据的DataTable</param>
        /// <param name="fileName">保存路径和文件名</param>
        public void DataTableToCsv(DataTable table, string fileName)

        {
            string title = "";
            //string loca = Directory.GetCurrentDirectory() + @"\";
            FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);//字符串前加@表示取消字符串里\的转义

            //FileStream fs1 = File.Open(file, FileMode.Open, FileAccess.Read);
            StreamWriter sw = new StreamWriter(new BufferedStream(fs), Encoding.Default);

            for (int i = 0; i < table.Columns.Count; i++)
            {
                title += table.Columns[i].ColumnName + "\t"; //栏位：自动跳到下一单元格
            }

            title = title.Substring(0, title.Length - 1) + "\n";

            sw.Write(title);

            foreach (DataRow row in table.Rows)
            {
                string line = "";
                for (int i = 0; i < table.Columns.Count; i++)

                {
                    //if (i == 0 && row[1].ToString().Length < 4)//不太懂原来这个长度小于4有啥用
                    if (i == 0)//一般把dt第一列设置为时间，对于时间要进行如下处理：
                        line += string.Format("{0:yyyy/MM/dd_HH:mm:ss}", row[i].ToString()) + "   t  " + "\t";//给时间加个其他字符，破坏其日期的格式，表格里显示就好了

                    else
                        line += row[i].ToString().Trim() + "\t";//内容：自动跳到下一单元格
                }
                line = line.Substring(0, line.Length - 1) + "\n";
                sw.Write(line);
            }

            sw.Close();
            fs.Close();

            //System.Diagnostics.Process.Start(loca); //打开文件夹
            MessageBoxResult result = MessageBox.Show("导出成功，是否打开文件？", "成功", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    System.Diagnostics.Process.Start(fileName); //打开excel文件
                }
                catch (Exception error)
                {
                    string.Format("打开失败！\r\n错误详情：{0}", error.Message);
                }
            }

        }

        /// <summary>
        /// 从Excel读取所有内容，存在一个DataTable里并返回
        /// </summary>
        /// <param name="whichSheet">填入一个整数n的字符串，表示读取的是sheetn</param>
        /// <returns>若加载失败，会返回null</returns>
        public DataTable GetDataFromExcel(string whichSheet)
        {
            //打开文件
            System.Windows.Forms.OpenFileDialog file = new System.Windows.Forms.OpenFileDialog();
            file.Filter = "Excel(*.xlsx)|*.xlsx|Excel(*.xls)|*.xls";
            file.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            file.Multiselect = false;
            if (file.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return null;

            //判断文件后缀
            var path = file.FileName;
            string fileSuffix = System.IO.Path.GetExtension(path);
            if (string.IsNullOrEmpty(fileSuffix))
                return null;

            using (DataTable dt1 = new DataTable())
            {
                //判断Excel文件是2003版本还是2007版本

                string connString = "";

                if (fileSuffix == ".xls")
                    connString = "Provider=Microsoft.Jet.OLEDB.4.0;" + "Data Source=" + path + ";" + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1\"";
                else if (fileSuffix == ".xlsx")
                    connString = "Provider=Microsoft.ACE.OLEDB.12.0;" + "Data Source=" + path + ";" + ";Extended Properties=\"Excel 12.0;HDR=YES;IMEX=1\"";
                else
                    return null;

                //读取文件
                //string strr = "[Sheet" + whichSheet + "$]";
                string strr = "[" + whichSheet + "$]";
                string sql_select = " SELECT * FROM " + strr;

                using (OleDbConnection conn = new OleDbConnection(connString))

                using (OleDbDataAdapter cmd = new OleDbDataAdapter(sql_select, conn))
                {
                    conn.Open();

                    cmd.Fill(dt1);

                    conn.Close();
                }

                if (dt1 == null) return null;

                return dt1;

            }

        }

        /// <summary>
        /// 从Csv读取所有内容，存在一个DataTable里并返回。
        /// </summary>
        /// <returns>若加载失败，会返回null</returns>
        public DataTable GetDataFromCsv()
        {
            //打开文件
            System.Windows.Forms.OpenFileDialog file = new System.Windows.Forms.OpenFileDialog();
            file.Filter = "Csv(*.csv)|*.csv";
            file.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            file.Multiselect = false;
            if (file.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return null;

            //判断文件后缀
            var path = file.FileName;
            string fileSuffix = System.IO.Path.GetExtension(path);
            if (string.IsNullOrEmpty(fileSuffix))
                return null;

            string[] strs = path.Split(new char[] { '\\' });
            string fileName = strs[strs.Length - 1];//文件名
            path = path.Replace(fileName, "");

            //路径是path，文件名是fileName

            using (DataTable dt1 = new DataTable())
            {
                //判断Excel文件是2003版本还是2007版本

                string connString = "";

                if (fileSuffix == ".xls")
                    connString = "Provider=Microsoft.Jet.OLEDB.4.0;" + "Data Source=" + path + ";" + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1\"";
                else if (fileSuffix == ".xlsx")
                    connString = "Provider=Microsoft.ACE.OLEDB.12.0;" + "Data Source=" + path + ";" + ";Extended Properties=\"Excel 12.0;HDR=YES;IMEX=1\"";
                else if (fileSuffix == ".csv")
                    connString = "Provider=Microsoft.ACE.OLEDB.12.0;" + "Data Source=" + path + ";" + ";Extended Properties=\"text;HDR=YES;FMT=Delimited\"";
                else
                    return null;

                //读取文件
                string sql_select = " SELECT * FROM " + fileName;

                using (OleDbConnection conn = new OleDbConnection(connString))

                using (OleDbDataAdapter cmd = new OleDbDataAdapter(sql_select, conn))

                {
                    conn.Open();

                    cmd.Fill(dt1);

                    conn.Close();
                }

                if (dt1 == null) return null;

                return dt1;

            }

        }

        /// <summary>
        /// 导出为Excel按钮公用函数，用按钮名称区分
        /// </summary>
        private void bt_ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                Button btt = (Button)sender;
                System.Windows.Forms.SaveFileDialog save = new System.Windows.Forms.SaveFileDialog();
                //save.Filter = "Excel文件 | *.xlsx";
                save.Filter = "Excel(*.xlsx)|*.xlsx|Excel(*.xls)|*.xls";
                save.Title = "保存数据处理结果";
                save.RestoreDirectory = true;

                if (save.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    DataTable dt_temp = new DataTable();
                    //根据按钮名称，确定是哪个dt需要导出
                    //switch (btt.Name)
                    //{
                    //    case "bt_Excel_DDS":
                    //        {
                    //            dt_temp = dt_DDSData.Copy();
                    //            #region 读取数据表——剂量率值单位处理

                    //            for (int i = 0; i < dt_temp.Rows.Count; i++)
                    //            {
                    //                string strr = dt_temp.Rows[i]["剂量率"].ToString();//取到这一条记录的剂量率字符串

                    //                if (strr.IndexOf("u") > 0)
                    //                {
                    //                    dt_temp.Rows[i]["剂量率"] = strr.Substring(0, strr.IndexOf("u"));
                    //                }
                    //                else if (strr.IndexOf("m") > 0)
                    //                {
                    //                    strr = strr.Substring(0, strr.IndexOf("m"));
                    //                    float ff;
                    //                    //尝试转化为浮点型，若转化成功，则赋值；若不成功，说明原来的内容格式不对
                    //                    if (float.TryParse(strr, out ff))
                    //                        dt_temp.Rows[i]["剂量率"] = ff * 1000;
                    //                    else
                    //                        dt_temp.Rows[i]["剂量率"] = "剂量率格式错误";
                    //                }
                    //                else
                    //                {
                    //                    float ff;
                    //                    //尝试转化为浮点型，若转化成功，则赋值；若不成功，说明原来的内容格式不对
                    //                    if (float.TryParse(strr, out ff))
                    //                        dt_temp.Rows[i]["剂量率"] = Convert.ToSingle(strr) * 1000000;
                    //                    else
                    //                        dt_temp.Rows[i]["剂量率"] = "剂量率格式错误";
                    //                }
                    //            }

                    //            #endregion
                    //            break;
                    //        }

                    //    case "bt_Excel_LongCPS":
                    //        {
                    //            dt_temp = dt_LongCPS.Copy();
                    //            break;
                    //        }
                    //    case "bt_Modbus_ExportExcel":
                    //        {
                    //            dt_temp = dt_ModbusRead.Copy();
                    //            break;
                    //        }

                    //}



                    DataTableToCsv(dt_temp, save.FileName); //导出Excel
                }

            }
            catch (Exception er)
            {
                MessageBox.Show(er.Message, "失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 从Excel读取数据，存到dt中，这个方法不需要OleDb。默认读取第一个表的内容。
        /// 使用时，需要在引用中添加自带的Microsoft Excel 16.0 Object Library
        /// </summary>
        /// <returns></returns>
        public DataTable ReadExcel()
        {
            //打开文件
            System.Windows.Forms.OpenFileDialog file = new System.Windows.Forms.OpenFileDialog();
            file.Filter = "Excel(*.xlsx)|*.xlsx|Excel(*.xls)|*.xls";
            file.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            file.Multiselect = false;
            if (file.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return null;

            //判断文件后缀
            var path = file.FileName;
            string fileSuffix = System.IO.Path.GetExtension(path);
            if (string.IsNullOrEmpty(fileSuffix))
                return null;

            //读取Excel
            Excel.Application app = new Excel.Application();
            Workbook wbk = app.Workbooks.Add(path);
            var sh = wbk.Sheets[1];//默认读取第一个表的内容。
            sh.Activate();
            var usedRange = sh.UsedRange.CurrentRegion;
            int rowCount = usedRange.Rows.Count;
            int coCount = usedRange.Columns.Count;
            DataTable dt = new DataTable();
            for (int i = 0; i < rowCount; i++)
            {
                DataRow newRow = dt.NewRow();
                for (int j = 0; j < coCount; j++)
                {
                    string s = sh.Cells[i + 1, j + 1].Text;
                    if (i == 0)//把列标题添加一下
                    {
                        dt.Columns.Add(s);
                    }
                    else//添加数据
                    {
                        newRow[j] = s;
                    }
                }
                dt.Rows.Add(newRow);
            }

            //把添加的第一个空行去掉
            dt.Rows.RemoveAt(0);

            wbk.Close();
            app.Quit();

            return dt;
        }

        /// <summary>
        /// 导出为Excel功能，输入Datatable和文件名称。
        /// 这个方法不需要OleDb。
        /// </summary>
        /// <param name="table">存储数据的DataTable</param>
        /// <param name="fileName">保存路径和文件名</param>
        public void WriteExcel(DataTable table, string fileName)
        {
            Excel.Application app = new Excel.Application();
            Workbook wbk = app.Workbooks.Add(true);
            var sh = wbk.Sheets[1];//默认读取第一个表的内容。
            sh.Activate();

            //填写一下列标题
            for (int i = 0; i < table.Columns.Count; i++)
            {
                string caption = table.Columns[i].Caption;
                sh.Cells[1, i + 1] = caption;
            }

            //填写数据
            for (int i = 0; i < table.Rows.Count; i++)
            {
                DataRow row = table.Rows[i];
                for (int j = 0; j < table.Columns.Count; j++)
                {
                    if (j == 0)//一般把dt第一列设置为时间，对于时间要进行如下处理：
                        sh.Cells[i + 1 + 1, j + 1] = string.Format("{0:yyyy/MM/dd_HH:mm:ss}", row[j].ToString());//给时间加个其他字符，破坏其日期的格式，表格里显示就好了
                                                                                                                 //sh.Cells[i + 1, j + 1] = string.Format("{0:yyyy/MM/dd_HH:mm:ss}", row[j].ToString()) + "   t  ";//给时间加个其他字符，破坏其日期的格式，表格里显示就好了
                    else
                        sh.Cells[i + 1 + 1, j + 1] = row[j].ToString().Trim();//内容：自动跳到下一单元格
                }
            }

            wbk.SaveAs(fileName);
            wbk.Close();
            app.Quit();

            //System.Diagnostics.Process.Start(loca); //打开文件夹
            MessageBoxResult result = MessageBox.Show("导出成功，是否打开文件？", "成功", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    System.Diagnostics.Process.Start(fileName); //打开excel文件
                }
                catch (Exception error)
                {
                    string.Format("打开失败！\r\n错误详情：{0}", error.Message);
                }
            }

        }

        #endregion

        #region 各类文件弹窗方法实例

        /// <summary>
        /// 保存文件弹窗
        /// </summary>
        private void ShowSaveFileDialog()
        {
            try
            {
                using (System.Windows.Forms.SaveFileDialog save = new System.Windows.Forms.SaveFileDialog())
                {
                    save.Title = "保存结果";
                    save.RestoreDirectory = true;
                    save.Filter = "Excel(*.xlsx)|*.xlsx|Excel(*.xls)|*.xls|Word(*.doc)|*.doc";
                    save.FileName = "";//初始文件名

                    if (save.ShowDialog() == System.Windows.Forms.DialogResult.OK)//点击确定后执行：
                    {
                        //File.WriteAllLines(save.FileName, new string[] { "Bs=" + devCom.P_Bs, "Bd=" + devCom.P_Bd });
                        //这里面执行一个写文件的操作
                    }
                }

            }
            catch (Exception er)
            {
                MessageBox.Show(er.Message, "失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 打开文件弹窗
        /// </summary>
        void ShowOpenFileDialog()
        {
            using (System.Windows.Forms.OpenFileDialog file = new System.Windows.Forms.OpenFileDialog())
            {
                file.Filter = "本底测量结果文件(*.bg)|*.bg";
                file.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                file.Multiselect = false;
                if (file.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string[] lines = File.ReadAllLines(file.FileName);
                    //foreach (string line in lines)
                    //{
                    //    string[] strs = line.Split(new char[] { '=' });
                    //    if (double.TryParse(strs[1], out double d))
                    //    {
                    //        if (strs[0].Equals("Bs"))
                    //            devCom.P_Bs = d;
                    //        if (strs[0].Equals("Bd"))
                    //            devCom.P_Bd = d;
                    //    }
                    //}

                }
            }
        }

        /// <summary>
        /// 打开一个文件，读取内容，返回多行数据
        /// </summary>
        /// <param name="filter">对文件格式的限制（XX文件(*.bg)|*.bg）</param>
        /// <returns>若读取失败，则返回空数组</returns>
        public string[] ReadCommonFileToLines(string filter)
        {
            string[] lines = new string[] { };
            using (System.Windows.Forms.OpenFileDialog file = new System.Windows.Forms.OpenFileDialog())
            {
                try
                {
                    if (!filter.Equals(""))
                        file.Filter = filter;
                    file.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    file.Multiselect = false;
                    if (file.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        lines = File.ReadAllLines(file.FileName);
                    }
                    Console.WriteLine("读取文件成功");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("读取文件失败，详情" + ex.Message);
                }
            }
            return lines;
        }

        /// <summary>
        /// 将数据保存到一个一般的文件
        /// </summary>
        /// <param name="filter">对文件格式的限制（XX文件(*.bg)|*.bg）</param>
        public void SaveLinesToCommonFile(string filter, string msg)
        {
            try
            {
                using (System.Windows.Forms.SaveFileDialog save = new System.Windows.Forms.SaveFileDialog())
                {
                    save.Title = "保存";
                    save.RestoreDirectory = true;
                    if (!filter.Equals(""))
                        save.Filter = filter;
                    save.FileName = "";//初始文件名

                    if (save.ShowDialog() == System.Windows.Forms.DialogResult.OK)//点击确定后执行：
                    {
                        //File.WriteAllLines(save.FileName, );
                        File.WriteAllText(save.FileName, msg);
                        MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

            }
            catch (Exception er)
            {
                MessageBox.Show("保存失败，详情" + er.Message, "失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 将数据保存到一个一般的文件
        /// </summary>
        /// <param name="filter">对文件格式的限制（XX文件(*.bg)|*.bg）</param>
        public void SaveLinesToCommonFile(string filter, string[] lines)
        {
            try
            {
                using (System.Windows.Forms.SaveFileDialog save = new System.Windows.Forms.SaveFileDialog())
                {
                    save.Title = "保存";
                    save.RestoreDirectory = true;
                    if (!filter.Equals(""))
                        save.Filter = filter;
                    save.FileName = "";//初始文件名

                    if (save.ShowDialog() == System.Windows.Forms.DialogResult.OK)//点击确定后执行：
                    {
                        File.WriteAllLines(save.FileName, lines);
                        //File.WriteAllText(save.FileName, msg);
                        MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

            }
            catch (Exception er)
            {
                MessageBox.Show("保存失败，详情" + er.Message, "失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region 读取Resources文件夹的文件

        /// <summary>
        /// 从exe所在路径的Resources文件夹中读取一个文件，返回所有行字符串
        /// </summary>
        /// <param name="fileName">带后缀的文件名</param>
        /// <returns></returns>
        public string[] ReadFileInResources(string fileName)
        {
            try
            {
                string filePath = Directory.GetCurrentDirectory() + "\\Resources\\" + fileName;
                string[] lines = File.ReadAllLines(filePath);
                return lines;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new string[] { };
            }
        }

        /// <summary>
        /// 向exe所在路径的Resources文件夹中的一个文件，写入多行数据
        /// </summary>
        /// <param name="fileName">带后缀的文件名</param>
        /// <returns></returns>
        public void WriteFileInResources(string fileName, string[] lines)
        {
            string filePath = Directory.GetCurrentDirectory() + "\\Resources\\" + fileName;
            File.WriteAllLines(filePath, lines);
        }

        #endregion

    }
}
