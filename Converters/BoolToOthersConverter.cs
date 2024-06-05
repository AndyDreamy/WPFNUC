using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace 核素识别仪.Converters
{
    class BoolToOthersConverter : IValueConverter
    {

        public enum Fun
        {
            toBoolFei,
            toString,
            toColor,
            toVisibility,
            toInt,
        }

        private Fun function;
        /// <summary>
        /// 选择返回的数据类型
        /// </summary>
        public Fun Function
        {
            get { return function; }
            set { function = value; }
        }


        private string trueStr = "已开始";
        /// <summary>
        /// true对应的字符串
        /// </summary>
        public string P_trueStr
        {
            get { return trueStr; }
            set { trueStr = value; }
        }

        private string falseStr = "已停止";
        /// <summary>
        /// false对应的字符串
        /// </summary>
        public string P_falseStr
        {
            get { return falseStr; }
            set { falseStr = value; }
        }


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            object result = null;
            switch (function)
            {
                case Fun.toBoolFei:
                    result = !b;
                    break;
                case Fun.toString:
                    if (b)
                        result = trueStr;
                    else
                        result = falseStr;
                    break;
                case Fun.toColor:
                    if (b)
                        result = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGreen);
                    else
                        result = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xfe, 0x66, 0x66));
                    //result = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    break;
                case Fun.toVisibility:
                    if (b)
                        result = Visibility.Visible;
                    else
                        result = Visibility.Hidden;
                    break;
                case Fun.toInt:
                    if (b)
                        result = 0;
                    else
                        result = 1;
                    break;
                default:
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
