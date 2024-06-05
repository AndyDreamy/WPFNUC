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
using System.Windows.Navigation;
using System.Windows.Shapes;
using static 核素识别仪.集成的数据类.HuoDu;

namespace 核素识别仪.自定义控件
{
    /// <summary>
    /// CTr_NucFactors.xaml 的交互逻辑
    /// </summary>
    public partial class CTr_NucFactors : UserControl
    {
        //public MainWindow Father { get; set; }

        public NucActScaleFactor P_factor { get; set; }

        public CTr_NucFactors()
        {
            InitializeComponent();
        }

        public void Init()
        {
            if (P_factor != null)
            {
                //this.DataContext = null;
                this.DataContext = P_factor;
            }
        }
    }
}
