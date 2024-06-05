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
using 核素识别仪.自定义控件;

namespace 核素识别仪.小窗口
{
    /// <summary>
    /// SerialPortSetting.xaml 的交互逻辑
    /// </summary>
    public partial class SerialPortSetting : Window
    {
        public SerialPortSetting()
        {
            InitializeComponent();

            //andySP.init_SerialPort();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
