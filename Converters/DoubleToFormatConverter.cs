using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace 核素识别仪.Converters
{
    /// <summary>
    /// 小数转化为不同形式数据或字符串的Converter
    /// </summary>
    public class DoubleToFormatConverter : IValueConverter
    {

        public enum Fun
        {
            scientific,
            holdXiaoShu,
            anyFormat
        }

        /// <summary>
        /// 转换功能
        /// </summary>
        public Fun Function { get; set; }

        private int holdNum = 2;
        /// <summary>
        /// 保留的小数位
        /// </summary>
        public int P_holdNum
        {
            get { return holdNum; }
            set { holdNum = value; }
        }

        private string stringFormat;
        /// <summary>
        /// 自定义的输出形式
        /// </summary>
        public string P_stringFormat
        {
            get { return stringFormat; }
            set { stringFormat = value; }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double f = (double)value;
            object result;

            switch (Function)
            {
                case Fun.scientific:
                    result = string.Format("{0:E2}", f);
                    break;
                case Fun.holdXiaoShu:
                    result = Math.Round(f, holdNum);
                    break;
                case Fun.anyFormat:
                    result = f.ToString(stringFormat);
                    //result = string.Format("{0:0.00}", f);
                    break;
                default:
                    result = null;
                    break;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
