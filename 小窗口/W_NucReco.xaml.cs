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
using static 核素识别仪.集成的数据类.Recognize;

namespace 核素识别仪.小窗口
{
    /// <summary>
    /// WD_NucReco.xaml 的交互逻辑
    /// </summary>
    public partial class W_NucReco : Window
    {
        #region 公共

        private MainWindow father;
        /// <summary>
        /// 主界面的实例
        /// </summary>
        public MainWindow Father
        {
            get { return father; }
            set { father = value; }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
        #endregion

        /// <summary>
        /// 构造器
        /// </summary>
        public W_NucReco()
        {
            InitializeComponent();

            //放大界面
            MainWindow.Instance.adc.ZoomWindow(this, 1.25);
        }

        /// <summary>
        /// 加载后的方法。设置dg的数据源。
        /// 不放在构造器是因为，会出现对象为null
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //更新两个dg的内容，以及一个cb

            //更新左边核素识别结果dg
            RefreshDgRecoResult();

            //先更新核素库名
            RefreshCbNucLibNames();

            //更新右边核素识别列表dg
            RefreshDgNucToReco();

            //设置数据源
            cb_ifShowPeaks.DataContext = MainWindow.Instance.reco;
        }

        /// <summary>
        /// 更新核素识别结果方法。外部调用此方法，可以将最新的核素识别结果更新到dg上
        /// </summary>
        public void RefreshDgRecoResult()
        {
            dg_RecoResult.ItemsSource = null;//必须的先清除，才能更新
            dg_RecoResult.ItemsSource = father.reco.RecoResults;
        }

        /// <summary>
        /// 更新识别核素列表方法。外部调用此方法，可以将最新的识别核素列表更新到dg上
        /// </summary>
        public void RefreshDgNucToReco()
        {
            List<NucInfo> items;
            string libName = (string)cb_NucLibNames.SelectedItem;
            if (libName == null || libName.Equals(string.Empty))
                items = father.reco.P_L_nucToReco;
            else
                items = father.reco.P_L_nucToReco.FindAll(x => x.P_libName.Equals(libName));

            dg_NucToReco.ItemsSource = null;//必须的先清除，才能更新
            dg_NucToReco.ItemsSource = items;
        }

        /// <summary>
        /// 更新核素库名称列表
        /// </summary>
        public void RefreshCbNucLibNames()
        {
            //保存上次选择的结果
            object o = cb_NucLibNames.SelectedItem;

            cb_NucLibNames.ItemsSource = null;
            cb_NucLibNames.ItemsSource = father.reco.P_L_libName;

            //如果新的列表仍包含上次选择的，则选择之，否则选择第一个
            if (cb_NucLibNames.Items.Contains(o))
                cb_NucLibNames.SelectedItem = o;
            else if (cb_NucLibNames.Items.Count > 0)
                cb_NucLibNames.SelectedIndex = 0;

        }

        /// <summary>
        /// 选择核素库事件，更改dg的ItemsSource
        /// </summary>
        private void cb_NucLibNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshDgNucToReco();
        }

        /// <summary>
        /// 手动识别按钮
        /// </summary>
        private void bt_ManualReco_Click(object sender, RoutedEventArgs e)
        {
            //这里要手动进行一次核素识别，虽然数据没有更新，但为了可以执行一次，就强制赋成true
            MainWindow.Instance.autoRun.P_isDataNew = true;
            MainWindow.Instance.DrawAndReco();
        }
    }
}
