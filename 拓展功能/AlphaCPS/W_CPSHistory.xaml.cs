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

namespace 核素识别仪.拓展功能.AlphaCPS
{
    /// <summary>
    /// W_CPSHistory.xaml 的交互逻辑
    /// </summary>
    public partial class W_CPSHistory : Window
    {
        public AlphaCPS AlphaCPS { get; set; } = AlphaCPS.Instance;

        public W_CPSHistory()
        {
            InitializeComponent();
            //this.DataContext = AlphaCPS.Instance;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
