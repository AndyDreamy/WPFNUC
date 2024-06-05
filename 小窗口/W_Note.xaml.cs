using System;
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
using System.Windows.Shapes;

namespace 核素识别仪.小窗口
{
    /// <summary>
    /// W_Note.xaml 的交互逻辑
    /// </summary>
    public partial class W_Note : Window
    {
        public W_Note()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 显示一段时间的自定义信息
        /// </summary>
        /// <param name="msg">显示的信息</param>
        /// <param name="time_ms">显示的时间(ms)</param>
        public void ShowNote(string msg, int time_ms)
        {
            OpenOneWindow(this);
            tb_Note.Text = msg;
            delay(time_ms);
            this.Hide();
        }

        /// <summary>
        /// 显示自定义信息，不自动消失
        /// </summary>
        /// <param name="msg">显示的信息</param>
        public void ShowNote(string msg )
        {
            OpenOneWindow(this);
            tb_Note.Text = msg;
        }

        /// <summary>
        /// 设置为true的话，就中断delay
        /// </summary>
        bool isInterrupt = false;

        private void delay(int n)
        {
            //WPF方法：（Winform也能用，只不过“System.Windows.Forms.”是多余的）
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < n)
            {
                if (isInterrupt)
                {
                    isInterrupt = false;
                    break;
                }

                System.Windows.Forms.Application.DoEvents();
            }

        }

        public void OpenOneWindow(Window window)
        {
            window.Hide();
            //window.WindowState = WindowState.Minimized;
            window.Show();
            window.WindowState = WindowState.Normal;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
