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
using System.ComponentModel;
using 核素识别仪.其他功能类;
using static 核素识别仪.集成的数据类.Recognize;

namespace 核素识别仪.小窗口
{
    /// <summary>
    /// 设置核素库小窗口
    /// </summary>
    public partial class W_NucLib : Window, INotifyPropertyChanged
    {
        #region 公共

        public event PropertyChangedEventHandler PropertyChanged;

        AndyFileRW rw = new AndyFileRW();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private MainWindow father;
        /// <summary>
        /// 主界面的实例
        /// </summary>
        public MainWindow Father
        {
            get { return father; }
            set { father = value; }
        }


        #endregion

        #region 文件读回的大核素库
        class NucLibAllItem
        {
            private string nucName;
            /// <summary>
            /// 核素，名称
            /// </summary>
            public string P_nucName
            {
                get { return nucName; }
                set { nucName = value; }
            }

            private string halfLife;
            /// <summary>
            /// 该核素的半衰期
            /// </summary>
            public string P_halfLife
            {
                get { return halfLife; }
                set { halfLife = value; }
            }

            private List<double> L_energy = new List<double>();
            /// <summary>
            /// 所有的能量列表
            /// </summary>
            public List<double> P_L_energy
            {
                get { return L_energy; }
                set { L_energy = value; }
            }

            private List<double> L_branch = new List<double>();
            /// <summary>
            /// 与能量对应的分支比列表
            /// </summary>
            public List<double> P_L_branch
            {
                get { return L_branch; }
                set { L_branch = value; }
            }

            public NucLibAllItem()
            {
            }
        }

        /// <summary>
        /// 文件读回的大核素库
        /// </summary>
        List<NucLibAllItem> L_NucLibAll = new List<NucLibAllItem>();

        private string searchNucName = string.Empty;
        /// <summary>
        /// 查询的核素名称
        /// </summary>
        public string P_searchNucName
        {
            get { return searchNucName; }
            set
            {
                searchNucName = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_searchNucName"));
                }
                //根据查询的名称更新左边的核素库列表
                SearchNucName();
            }
        }

        #endregion

        #region 某个核素的能量列表

        /// <summary>
        /// 某个核素的一个能量信息，包含能量值和分支比
        /// </summary>
        class NucEnergy
        {
            private double energy;
            /// <summary>
            /// 核素能量
            /// </summary>
            public double P_energy
            {
                get { return energy; }
                set { energy = value; }
            }

            private double branch;
            /// <summary>
            /// 该能量的分支比
            /// </summary>
            public double P_branch
            {
                get { return branch; }
                set { branch = value; }
            }

        }

        /// <summary>
        /// 相当于是所有的能量列表，实际显示在dg上的是经过比较最小分支比后的一个局部变量
        /// </summary>
        List<NucEnergy> L_NucEnergy = new List<NucEnergy>();

        private double minBranch = 0;
        /// <summary>
        /// 查看能量的最小分支比
        /// </summary>
        public double P_minBranch
        {
            get { return minBranch; }
            set
            {
                minBranch = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_minBranch"));
                }
                MinBranchControl();
            }
        }

        private string selectedNucName = "核素名称";
        /// <summary>
        /// 当前所选的的核素名称，显示在中间那部分
        /// </summary>
        public string P_selectedNucName
        {
            get { return selectedNucName; }
            set
            {
                selectedNucName = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("P_selectedNucName"));
                }
            }
        }

        /// <summary>
        /// 当前所选的能量，在点击中间能量列表的“添加”按钮时，赋值为所选能量。再右边的添加新核素时，设置为0
        /// </summary>
        private double selectedEnergy = 0;

        #endregion

        /// <summary>
        /// 构造器
        /// </summary>
        public W_NucLib()
        {
            InitializeComponent();

            // 从文件加载所有的核素信息列表
            GetNucLibAllFromFile();

            //显示的核素信息默认为所有的
            P_searchNucName = string.Empty;

            //放大界面
            MainWindow.Instance.adc.ZoomWindow(this, 1.25);

        }

        /// <summary>
        /// Loaded方法，在构造器方法中，father为null，这里的Father就不是null了
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //这个方法也只会进行一次，就是在第一次Show这个界面的时候

            //取一下reco.NucLib，加到临时识别核素列表，并显示到dg
            ResetNuc_Click(this, null);
        }

        #region 左

        /// <summary>
        /// 从文件加载所有的核素信息列表
        /// </summary>
        private void GetNucLibAllFromFile()
        {
            try
            {
                string[] lines = rw.ReadFileInResources("核素库\\nuclides_lib.txt");
                string[] strs;
                for (int i = 0; i < lines.Length; i++)
                {
                    try
                    {
                        strs = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        //判断拆解的字符串数量
                        if (strs.Length == 3)
                        {
                            //说明是核素名等信息，则增加一个新的核素库项目信息
                            L_NucLibAll.Add(new NucLibAllItem() { P_nucName = strs[0], P_halfLife = strs[1] });
                        }
                        else if (strs.Length == 2)
                        {
                            //说明是刚增加核素的能量信息，则向最新的核素信息增加能量信息
                            NucLibAllItem item = L_NucLibAll[L_NucLibAll.Count - 1];

                            double dd;

                            //添加分支比
                            double.TryParse(strs[0], out dd);
                            item.P_L_branch.Add(dd);

                            //添加能量
                            double.TryParse(strs[1], out dd);
                            item.P_L_energy.Add(dd * 1000);//*1000是为了把单位换成keV
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("核素库加载失败，详情：\r\n" + e.Message);
            }
        }

        /// <summary>
        /// 根据查询的名称，更新dg_NucLibAll
        /// </summary>
        private void SearchNucName()
        {
            List<NucLibAllItem> items;
            if (searchNucName.Equals(string.Empty))//如果输入的查询名称为空，则加载所有的核素
                items = L_NucLibAll;
            else//否则，找到包含搜索内容的核素
                items = L_NucLibAll.FindAll(x => x.P_nucName.Contains(searchNucName) || x.P_nucName.ToLower().Contains(searchNucName));

            //更新dg
            dg_NucLibAll.ItemsSource = null;
            dg_NucLibAll.ItemsSource = items;
        }

        /// <summary>
        /// 核素库点击某个核素的事件
        /// </summary>
        private void dg_NucLibAll_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            DataGrid dg = (DataGrid)sender;
            //找到所选的核素对象
            var item = (NucLibAllItem)dg.SelectedItem;
            if (item == null)
                return;

            //显示所选的核素名称
            P_selectedNucName = item.P_nucName;

            //将这个核素的信息添加到L_NucEnergy
            L_NucEnergy.Clear();
            for (int i = 0; i < item.P_L_branch.Count; i++)
            {
                L_NucEnergy.Add(new NucEnergy() { P_energy = item.P_L_energy[i], P_branch = item.P_L_branch[i] });
            }

            //按P_branch降序排序。如果想反序，就更换return那一行x1 x2的位置
            L_NucEnergy.Sort((x1, x2) => { return x2.P_branch.CompareTo(x1.P_branch); });

            //更新dg
            MinBranchControl();
        }

        #endregion

        #region 中

        /// <summary>
        /// 最小分支比控制能量显示内容——就是更新dg_NucEnergies的方法
        /// </summary>
        private void MinBranchControl()
        {
            List<NucEnergy> items = L_NucEnergy.FindAll(x => x.P_branch >= minBranch);
            //更新dg
            dg_NucEnergies.ItemsSource = null;
            dg_NucEnergies.ItemsSource = items;
        }

        /// <summary>
        /// 添加一个能量按钮
        /// </summary>
        private void AddEnergy_Click_Click(object sender, RoutedEventArgs e)
        {
            var item = (NucEnergy)dg_NucEnergies.SelectedItem;
            if (item == null)//如果没有选择任何项，则什么也不干
                return;

            //把所选的能量记录一下
            selectedEnergy = item.P_energy;

            //添加前，让用户选一下核素库，或者自定义核素库。只打开选择核素库小界面，在小界面执行真正的添加操作
            border_SelectLibName.Visibility = Visibility.Visible;

            //temp_NucLib.Add(new NucInfo() { P_nucName = selectedNucName, P_energy = item.P_energy });
            //RefreshNucLib();
        }

        /// <summary>
        /// 选择核素库名确定按钮
        /// </summary>
        private void bt_SelectLibNameOK_Click(object sender, RoutedEventArgs e)
        {
            //获取用户选的核素库名
            string libName = tb_SelectedLibName.Text;
            if (libName.Equals(""))
                libName = "默认核素库";

            //添加一个新的核素
            temp_NucLib.Add(new NucInfo() { P_nucName = selectedNucName, P_energy = selectedEnergy, P_libName = libName });
            RefreshNucLib();

            border_SelectLibName.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 选择核素库名取消按钮
        /// </summary>
        private void bt_SelectLibNameCancel_Click(object sender, RoutedEventArgs e)
        {
            border_SelectLibName.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 根据临时的核素识别列表temp_NucLib，更新存核素库名的cb
        /// </summary>
        private void Refresh_cb_LibNames()
        {
            cb_LibNames.Items.Clear();
            foreach (var item in temp_NucLib)
            {
                if (!cb_LibNames.Items.Contains(item.P_libName))
                    cb_LibNames.Items.Add(item.P_libName);
            }
        }

        #endregion

        #region 右

        /// <summary>
        /// 一个临时的识别核素List，可以由dg随意更改，最终是否更新到reco.NucLib，需要按钮控制
        /// </summary>
        List<NucInfo> temp_NucLib = new List<NucInfo>();

        /// <summary>
        /// 根据识别核素列表，更新dg
        /// </summary>
        private void RefreshNucLib()
        {
            dg_NucLib.ItemsSource = null;
            dg_NucLib.ItemsSource = temp_NucLib;

            //把更新cb放到更新dg的方法里吧，它俩总是一起执行
            Refresh_cb_LibNames();
        }

        /// <summary>
        /// 添加核素按钮
        /// </summary>
        private void AddNewNuc_Click(object sender, RoutedEventArgs e)
        {
            //设置默认值
            selectedNucName = "新核素";
            selectedEnergy = 0;

            //添加前，让用户选一下核素库，或者自定义核素库。只打开选择核素库小界面，在小界面执行真正的添加操作
            border_SelectLibName.Visibility = Visibility.Visible;

            //temp_NucLib.Add(new NucInfo() { P_nucName = "new nuclide", P_energy = 0 });
            //RefreshNucLib();
        }

        /// <summary>
        /// 删除核素按钮，可以一下删除多个
        /// </summary>
        private void DeleteNuc_Click(object sender, RoutedEventArgs e)
        {
            System.Collections.IList items = dg_NucLib.SelectedItems;
            foreach (var item in items)
            {
                if (item != null)
                {
                    temp_NucLib.Remove((NucInfo)item);
                }
            }
            RefreshNucLib();

        }

        /// <summary>
        /// 重置核素按钮，从reco.NucLib重载识别核素列表，到临时的temp_NucLib
        /// </summary>
        private void ResetNuc_Click(object sender, RoutedEventArgs e)
        {
            temp_NucLib.Clear();
            foreach (var item in father.reco.P_L_nucToReco)
            {
                temp_NucLib.Add(new NucInfo() { P_libName = item.P_libName, P_energy = item.P_energy, P_nucName = item.P_nucName });
            }

            //更新dg
            RefreshNucLib();
        }

        /// <summary>
        /// 保存识别核素列表按钮
        /// </summary>
        private void SaveNuc_Click(object sender, RoutedEventArgs e)
        {
            //提示一下是否确认保存
            if (!(MessageBox.Show("确认保存修改？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK))
                return;

            //将当前设置的核素识别列表，更新到reco.NucLib对象
            father.reco.P_L_nucToReco.Clear();
            father.reco.P_L_nucToReco.AddRange(temp_NucLib);

            //识别核素列表变化后，以此更新核素库名列表
            father.reco.RefreshL_libName();

            //更新到核素识别界面的dg和cb上
            father.w_NucReco.RefreshDgNucToReco();
            father.w_NucReco.RefreshCbNucLibNames();

            //将识别核素列表信息保存到文件
            father.reco.SaveNucToRecoToFile();

            MessageBox.Show("保存成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        #endregion
    }
}
