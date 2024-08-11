using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace 核素识别仪.Converters
{
    public class CPSThresholdToColorMultiConverter : IMultiValueConverter
    {
        private static Lazy<CPSThresholdToColorMultiConverter> instance = new Lazy<CPSThresholdToColorMultiConverter>(() => new CPSThresholdToColorMultiConverter());
        public static CPSThresholdToColorMultiConverter Instance { get { return instance.Value; } }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double cps && values[1] is double threshold)
            {
                if (cps >= threshold)
                {
                    return new SolidColorBrush(Colors.Red);
                }
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
