using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace 核素识别仪.其他功能类
{
    /// <summary>
    /// 更新时间：2023年6月19日10:14:09
    /// </summary>
    public class AndyCommon
    {

        #region 全局变量

        /// <summary>
        /// 字段的Map，可以存一些字段对象，就可以方便查找
        /// </summary>
        Dictionary<string, object> fieldMap = new Dictionary<string, object>();

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

        /// <summary>
        /// 时间单位枚举
        /// </summary>
        public enum TimeUnit
        {
            ms,
            s,
            min,
            hour,
            day,
            mon,
            year
        }

        /// <summary>
        /// 当前线程挂起一段时间
        /// </summary>
        public void sleep(double n, TimeUnit unit)
        {
            switch (unit)
            {
                case TimeUnit.ms:
                    Thread.Sleep((int)n);
                    break;
                case TimeUnit.s:
                    Thread.Sleep((int)(n * 1000));
                    break;
                case TimeUnit.min:
                    Thread.Sleep((int)(n * 1000 * 60));
                    break;
                case TimeUnit.hour:
                    Thread.Sleep((int)(n * 1000 * 60 * 60));
                    break;
                case TimeUnit.day:
                    Thread.Sleep((int)(n * 1000 * 60 * 60 * 24));
                    break;
                case TimeUnit.mon:
                    break;
                case TimeUnit.year:
                    break;
                default:
                    break;
            }


        }

        /// <summary>
        /// 这个sleep可以强行中断，最小时间单位为s
        /// </summary>
        /// <param name="n"></param>
        /// <param name="unit"></param>
        /// <param name="run">为false的话，就强制结束</param>
        public void sleep(double n, TimeUnit unit, bool run)
        {
            int times = 0;//等待的总秒数
            int go = 0;//当前等待了的秒数
            switch (unit)
            {
                case TimeUnit.s:
                    times = (int)n;
                    break;
                case TimeUnit.min:
                    times = (int)(n * 60);
                    break;
                case TimeUnit.hour:
                    times = (int)(n * 60 * 60);
                    break;
                case TimeUnit.day:
                    times = (int)(n * 60 * 60 * 24);
                    break;
                default:
                    break;
            }
            while (go++ < times && run)
            {
                Thread.Sleep(1000);
            }


        }

        #region Excel读写功能

        /// <summary>
        /// 从Excel读取所有内容，存在一个DataTable里并返回
        /// </summary>
        /// <param name="whichSheet">填入一个整数n的字符串，表示读取的是sheetn</param>
        /// <returns></returns>
        public DataTable getData(string whichSheet)
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

                else

                    connString = "Provider=Microsoft.ACE.OLEDB.12.0;" + "Data Source=" + path + ";" + ";Extended Properties=\"Excel 12.0;HDR=YES;IMEX=1\"";

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
        /// 导出为Excel按钮公用函数
        /// </summary>
        /// <param name="dt_Export">输入要导出的dt</param>
        private void ExportExcel(DataTable dt_Export)
        {
            try
            {

                System.Windows.Forms.SaveFileDialog save = new System.Windows.Forms.SaveFileDialog();
                //save.Filter = "Excel文件 | *.xlsx";
                save.Filter = "Excel(*.xlsx)|*.xlsx|Excel(*.xls)|*.xls";
                save.Title = "保存数据处理结果";
                save.RestoreDirectory = true;

                if (save.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    dataTableToCsv(dt_Export, save.FileName); //导出Excel
                }

            }
            catch (Exception er)
            {
                MessageBox.Show(er.Message, "失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 导出为Excel功能，输入Datatable和文件名称
        /// </summary>
        /// <param name="table">存储数据的DataTable</param>
        /// <param name="fileName">保存路径和文件名</param>
        private void dataTableToCsv(DataTable table, string fileName)

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

        #endregion

        #region 不同数据类型互相转化

        /// <summary>
        /// 将字节list转化为等长度的字节数组——人家本来就有.ToArray函数，我没必要定义这个函数
        /// </summary>
        private byte[] ListToArray(List<byte> list)
        {
            byte[] returnByte = new byte[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                returnByte[i] = list[i];
            }
            return returnByte;
        }

        /// <summary>
        /// 将十六进制字符串转化为字节数组，要求传入的字符串是正确的格式，否则会返回null；若字符个数为奇数，则会在最后补一个0
        /// </summary>
        public byte[] HexStringToArray(string hexStr)
        {
            try
            {
                if (hexStr.Length % 2 == 1)
                {
                    hexStr += "0";
                }

                byte[] buffer = new byte[hexStr.Length / 2];
                for (int i = 0; i < hexStr.Length / 2; i++)
                {
                    string str = hexStr.Substring(i * 2, 2);
                    buffer[i] = Convert.ToByte(str, 16);
                }
                return buffer;
            }
            catch (Exception)
            {
                return null;//如果转化失败，则返回空
            }

        }

        #endregion

        /// <summary>
        /// dg的按键按下事件，主要用于捕捉按下Control+V
        /// </summary>
        private void Keydown_Paste(object sender, KeyEventArgs e)
        {
            #region 粘贴表格内容
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            {
                try
                {
                    DataGrid dg_temp = e.Source as DataGrid;//这里用e.Source，就可以在Window触发这个事件时，找到触发的DataGrid了
                    if (dg_temp == null)
                    {
                        return;
                    }
                    string dtName = dg_temp.Name.Replace("dg", "dt");
                    DataTable dt_temp = fieldMap[dtName] as DataTable;
                    CopyHelper.PasteToDg(dt_temp, dg_temp);
                }
                catch (Exception)
                {
                }
            }
            #endregion
        }

        #region 校验方法

        /// <summary>
        /// 异或校验
        /// </summary>
        /// <param name="msg">需要异或校验的字符串</param>
        /// <returns>异或校验结果，1个字节16进制数据的字符串</returns>
        public string GetXorCheckSum(string msg)
        {
            //char[] charArray = msg.ToCharArray();
            if (msg.Length < 1)
                return "";
            ASCIIEncoding asciiEncoding = new ASCIIEncoding();
            byte[] byteArray = asciiEncoding.GetBytes(msg);
            byte crc = byteArray[0];
            for (int i = 1; i < byteArray.Length; i++)
            {
                crc ^= byteArray[i];
            }
            return crc.ToString("X2");
        }

        #endregion

        #region 打开一个小窗口的方法

        public void OpenOneWindow(Window window)
        {
            window.Hide();
            //window.WindowState = WindowState.Minimized;
            window.Show();
            window.WindowState = WindowState.Normal;
        }

        #endregion

        #region dt相关的通用方法

        /// <summary>
        /// 将一个dt复制到另一个dt，不论是结构还是其中的数据
        /// </summary>
        /// <param name="sourceDt">源dt</param>
        /// <param name="destDt">目标dt</param>
        private void CopyDataTable(DataTable sourceDt, DataTable destDt)
        {
            destDt = sourceDt.Clone();//先复制格式

            //遍历数据，依次填入
            if (sourceDt.Columns.Count == destDt.Columns.Count)//列数一样，就认为结构一样
            {
                for (int i = 0; i < sourceDt.Rows.Count; i++)
                {
                    DataRow newRow = destDt.NewRow();
                    for (int j = 0; j < sourceDt.Columns.Count; j++)
                    {
                        newRow[j] = sourceDt.Rows[i][j];
                    }
                    destDt.Rows.Add(newRow);
                }
            }
        }

        #endregion

        #region 让一个界面整体缩放的方法

        /// <summary>
        /// 让一个界面等比例缩放的方法。
        /// 可以保持原来的布局，缺点是界面不会从中央出现。
        /// </summary>
        /// <param name="w">要缩放的Window对象</param>
        /// <param name="ratio">缩放的比例</param>
        public void ZoomWindow(Window w, double ratio)
        {
            try
            {
                w.Height *= ratio;
                w.Width *= ratio;

                ScaleTransform scale = new ScaleTransform();
                //X、Y方向都按照相同比例缩放，这样内容就不会被拉高或拉宽
                scale.ScaleX = ratio;
                scale.ScaleY = ratio;
                TransformGroup transformGroup = new TransformGroup();
                transformGroup.Children.Add(scale);

                FrameworkElement content = (FrameworkElement)w.Content;
                content.RenderTransformOrigin = new Point(0.5, 0.5);
                content.LayoutTransform = transformGroup;
            }
            catch (Exception ex)
            {
                Console.WriteLine("缩放失败，详情:" + ex.Message);
            }
        }

        #endregion
    }
}
