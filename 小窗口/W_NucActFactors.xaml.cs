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
using static 核素识别仪.集成的数据类.HuoDu;

namespace 核素识别仪.小窗口
{
    /// <summary>
    /// W_NucActFactors.xaml 的交互逻辑
    /// </summary>
    public partial class W_NucActFactors : Window
    {
        public MainWindow Father { get; set; }

        /// <summary>
        /// 本界面定义的因子列表
        /// </summary>
        List<NucActScaleFactor> list_Factors = new List<NucActScaleFactor>();

        public W_NucActFactors()
        {
            InitializeComponent();
            sp_AddNuc.Visibility = Visibility.Hidden;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        /// <summary>
        /// 初始化，将真实使用的因子复制到本界面定义的因子列表
        /// </summary>
        public void Init()
        {
            //这个界面建立一个新的list，将实际使用的因子们复制进来，再本界面修改因子，不会影响到实际使用的因子，除非点击保存按钮
            list_Factors.Clear();
            foreach (var item in Father.huoDu.factors)
            {
                list_Factors.Add(item.Clone());
            }

            //更新到cb上
            RefreshNucNameCb();
        }

        /// <summary>
        /// 根据本界面的核素因子信息，更新ComboBox的选项
        /// </summary>
        private void RefreshNucNameCb()
        {
            //加载完毕后，给cb添加item
            cb_NucSelect.Items.Clear();
            foreach (var item in list_Factors)
            {
                cb_NucSelect.Items.Add(item.P_nucName);
            }

            if (cb_NucSelect.Items.Count > 0)
                cb_NucSelect.SelectedIndex = 0;
        }

        /// <summary>
        /// 显示计数率到活度的刻度因子时，选择核素事件。
        /// 创建一个新的CTr_NucFactors控件，根据所选核素名称找到数据并显示
        /// </summary>
        private void cb_NucSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_NucSelect.SelectedItem == null)
                return;

            CTr_NucFactors cTr_NucFactors = new CTr_NucFactors();
            string selectedItem = cb_NucSelect.SelectedItem.ToString();
            NucActScaleFactor fac = list_Factors.Find(x => x.P_nucName.Equals(selectedItem));
            if (fac != null)
            {
                cTr_NucFactors.P_factor = fac;
                cTr_NucFactors.Init();
                sp_Factors.Children.Clear();
                sp_Factors.Children.Add(cTr_NucFactors);
            }

        }

        /// <summary>
        /// 保存按钮。将当前界面的因子列表复制到主界面的实际因子列表
        /// </summary>
        private void bt_Save_Click(object sender, RoutedEventArgs e)
        {
            Father.huoDu.factors.Clear();
            foreach (var item in list_Factors)
            {
                Father.huoDu.factors.Add(item.Clone());
            }

            //还需要保存到文件
            Father.huoDu.SaveScaleFactorsToFile();
        }

        /// <summary>
        /// 重置按钮。重置设置值，重新从主界面获取因子数据，更新
        /// </summary>
        private void bt_Reset_Click(object sender, RoutedEventArgs e)
        {
            Init();
            cb_NucSelect_SelectionChanged(this, null);
        }

        #region 添加删除核素功能

        /// <summary>
        /// 添加核素按钮
        /// </summary>
        private void bt_AddNuc_Click(object sender, RoutedEventArgs e)
        {
            if (sp_AddNuc.Visibility == Visibility.Visible)
            {
                sp_AddNuc.Visibility = Visibility.Hidden;
            }
            else
            {
                sp_AddNuc.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 添加核素小界面，输入核素库名称确定按钮
        /// </summary>
        private void bt_NucNameOK_Click(object sender, RoutedEventArgs e)
        {
            string nucName = tb_AddNucName.Text;
            if (nucName.Equals(""))
            {
                MessageBox.Show("核素名不能为空", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information);
                return;
            }
            else
            {
                list_Factors.Add(new NucActScaleFactor() { P_nucName = nucName });
            }

            //更新
            RefreshNucNameCb();//更新到cb
            cb_NucSelect_SelectionChanged(this, null);//选一下cb

            //同样关闭小界面
            sp_AddNuc.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 添加核素小界面，输入核素库名称取消按钮
        /// </summary>
        private void bt_NucNameCancel_Click(object sender, RoutedEventArgs e)
        {
            sp_AddNuc.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 删除核素按钮
        /// </summary>
        private void bt_DeleteNuc_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show("确定删除当前核素？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (res == MessageBoxResult.Yes)
            {
                NucActScaleFactor nuc = list_Factors.Find(x => x.P_nucName.Equals(cb_NucSelect.Text));
                if (nuc != null)
                    list_Factors.Remove(nuc);

                //更新
                RefreshNucNameCb();//更新到cb
                cb_NucSelect_SelectionChanged(this, null);//选一下cb
            }
        }

        #endregion
    }
}
