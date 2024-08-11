using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace 核素识别仪.自定义控件
{
    public partial class DateTimePicker_Andy : UserControl, INotifyPropertyChanged
    {
        #region 时分秒属性定义

        public event PropertyChangedEventHandler PropertyChanged;

        private int hour;
        public int Hour
        {
            get { return hour; }
            set
            {
                if (value < 24)//写属性时，可以约束设置值的范围
                {
                    hour = value;
                }
                else
                {
                    hour = 23;
                }
                UpdateResult();
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Hour"));//set的时候，事件被激发
                }
            }
        }
        private int minute;
        public int Minute
        {
            get { return minute; }
            set
            {
                if (value < 60)
                {
                    minute = value;
                }
                else
                {
                    minute = 59;
                }
                UpdateResult();
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Minute"));//set的时候，事件被激发
                }
            }
        }
        private int second;
        public int Second
        {
            get { return second; }
            set
            {
                if (value < 60)
                {
                    second = value;
                }
                else
                {
                    second = 59;
                }
                UpdateResult();
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Second"));//set的时候，事件被激发
                }
            }
        }

        private DateTime selectedDate;
        /// <summary>
        /// DateTimePicker选择的Date值
        /// </summary>
        public DateTime SelectedDate
        {
            get { return selectedDate; }
            set
            {
                //SelectedDate被设置的时候，只选择年月日部分
                selectedDate = new DateTime(value.Year, value.Month, value.Day);
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDate)));
                UpdateResult();
            }
        }

        #endregion

        /// <summary>
        /// 可以绑定的依赖属性结果
        /// </summary>
        public DateTime DateTimeResult
        {
            get
            {
                return (DateTime)GetValue(DateTimeResultProperty);
            }
            set
            {
                SetValue(DateTimeResultProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for DateTimeResult.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DateTimeResultProperty =
            DependencyProperty.Register("DateTimeResult", typeof(DateTime), typeof(DateTimePicker_Andy), new PropertyMetadata(DateTime.MinValue));

        /// <summary>
        /// 综合的DateTime结果
        /// </summary>
        public DateTime CompositiveDateTime
        {
            get
            {
                DateTime dt = SelectedDate;
                dt = dt.AddHours(hour);
                dt = dt.AddMinutes(minute);
                dt = dt.AddSeconds(second);//直接取字段的值，而不用访问UI
                return dt;
            }
            set
            {
                SelectedDate = value;//这一步是只获取年月日，不要时分秒，给DateTimePicker
                Hour = value.Hour;
                Minute = value.Minute;
                Second = value.Second;
            }
        }

        public DateTimePicker_Andy()
        {
            InitializeComponent();

            //初始化时间显示为当前时间
            CompositiveDateTime = DateTime.Now;

            //DateTime now = DateTime.Now;
            //SelectedDate = now;
            //Hour = now.Hour;
            //Minute = now.Minute;
            //Second = now.Second;

        }

        private void UpdateResult()
        {
            DateTimeResult = CompositiveDateTime;
        }

        /// <summary>
        /// 按键按下时的事件，作用是只接收键入数字，对输入内容做一个限制
        /// </summary>
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox tb = sender as TextBox;

            bool condition = false;//可以键入的条件，如果包含一下情况中的一种，就可以键入
            condition |= e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9;//小数字键盘
            condition |= e.Key >= Key.D0 && e.Key <= Key.D9;//数字键
            //condition |= e.Key == Key.Decimal;//句点
            //condition |= e.Key == Key.OemPeriod;//句号

            if (condition)
            {
                e.Handled = false;
            }
            else
                e.Handled = true;
        }

        /// <summary>
        /// 按键抬起时触发的时间，用于约束整数的最大值，不过这里没有用到，因为属性的Binding已经约束了
        /// </summary>
        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            //TextBox tb = sender as TextBox;
            //int maxNum = 1;
            //int.TryParse(tb.Tag.ToString(), out maxNum);

            //int num = 0;
            //int.TryParse(tb.Text, out num);

            //if (num < maxNum)
            //{
            //    e.Handled = false;
            //}
            //else
            //{
            //    e.Handled = true;
            //    tb.Text = maxNum.ToString();
            //}

        }
    }
}
