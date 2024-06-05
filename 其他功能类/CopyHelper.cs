using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace 核素识别仪.其他功能类
{
    public class CopyHelper
    {

        /// <summary>
        /// 获取粘贴板最近一次的text
        /// </summary>
        public static string GetClipboardText()
        {
            try
            {
                string pasteText = Clipboard.GetText();//此函数就可以获得最近一次复制或粘贴的内容，是一个字符串，其中的回车就是\r\n，表格之间的间隔就是\t
                if (string.IsNullOrEmpty(pasteText))
                    return null;
                else
                    return pasteText;
            }
            catch (Exception)
            {
                return null;
            }

        }

        /// <summary>
        /// 将剪贴板文本按照表格(含有\r\n、\t)格式解析
        /// </summary>
        public static List<string[]> ResolveToDt()
        {
            try
            {
                string text = GetClipboardText();
                string[] lines = text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                List<string[]> result = new List<string[]>();//一个字符串数组的list，每个数组表示一行数据
                foreach (string line in lines)
                {
                    string[] cells = line.Split(new char[] { '\t' });
                    result.Add(cells);
                }
                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取某个DataGrid选中的第一个单元格的行列index，若未选中，则返回0
        /// </summary>
        /// <param name="dg_temp">DataGrid对象</param>
        /// <param name="c">列的index</param>
        /// <param name="r">行的index</param>
        public static void LocateDgFirstSelectedCell(DataGrid dg_temp, out int c, out int r)
        {
            if (dg_temp.SelectedCells.Count > 0)
            {
                c = dg_temp.SelectedCells[0].Column.DisplayIndex;
                r = dg_temp.Items.IndexOf(dg_temp.SelectedCells[0].Item);
            }
            else
                c = r = 0;
        }

        /// <summary>
        /// 将粘贴板上的第一个内容粘贴到一个dg上
        /// </summary>
        /// <param name="dt_temp">与dg绑定的dt</param>
        /// <param name="dg_temp">目标dg</param>
        public static void PasteToDg(DataTable dt_temp, DataGrid dg_temp)
        {
            //获得数据
            List<string[]> list = ResolveToDt();
            //获得当前点的单元格信息
            int c, r;//r为行index，c为列index
            LocateDgFirstSelectedCell(dg_temp, out c, out r);
            int cStart = c;//记录开始的列index

            //效果就是，从这个位置开始粘贴，行数不够就加，列数不够不要加
            foreach (string[] line in list)
            {
                if (r >= dt_temp.Rows.Count)//如果行index越界，则给dt新加一行
                {
                    dt_temp.Rows.Add();
                }
                foreach (string cell in line)
                {
                    //cell就是每个单元格要填的内容
                    if (c < dt_temp.Columns.Count)
                    {
                        dt_temp.Rows[r][c++] = cell;
                    }
                }
                r++;//一行结束，r自加一
                c = cStart;//指针c要返回开始的位置
            }
        }

        #region 其他相关的参考程序

        #region 给一个控件添加“粘贴”事件，就是出现光标后，按下Control+V会触发。可以参考着用一用
        public static void AddPastingEvent(DependencyObject control)
        {
            System.Windows.DataObject.AddPastingHandler(control, dawdaw);
        }

        /// <summary>
        /// 用于这个事件的委托对象
        /// </summary>
        private static void dawdaw(object sender, DataObjectPastingEventArgs e)
        {

        }
        #endregion

        /// <summary>
        /// dg的按键按下事件，主要用于捕捉按下Control+V
        /// </summary>
        private void dg_Keydown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            {

            }
        } 

        #endregion
    }
}
