using System;
using System.Collections.Generic;
using System.Data;
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
using 核素识别仪.其他功能类;
using static 核素识别仪.通用功能类.FittingFuntion;
//using 拟合助手_WPF.其他功能类;
//using 拟合助手_WPF.通用功能类;
//using static 拟合助手_WPF.通用功能类.FittingFuntion;

namespace 核素识别仪.拟合助手_WPF
{
    /// <summary>
    /// 数据拟合界面
    /// </summary>
    public partial class W_Fitting : Window
    {
        W_FittingChart w_Fitting;
        AndyCommon adc = new AndyCommon();

        /// <summary>
        /// 构造器
        /// </summary>
        public W_Fitting()
        {
            InitializeComponent();

            //初始化拟合模式的cb
            cb_FittingMode.ItemsSource = fittingTypes;
            if (cb_FittingMode.Items.Count > 0)
                cb_FittingMode.SelectedIndex = 1;

            //初始化两个dt的结构
            init_dt_FittingData_FittingResult();

            w_Fitting = new W_FittingChart();
            w_Fitting.Father = this;

            #region 因为用于核素识别仪的能量刻度而特殊设置的内容

            //放大界面
            MainWindow.Instance.adc.ZoomWindow(this, 1.25);

            //标题
            Title = "能量刻度";

            //表格列名称
            dg_FittingData.Columns[0].Header = "道址";
            dg_FittingData.Columns[1].Header = "能量(keV)";
            dg_FittingResult.Columns[0].Header = "道址";
            dg_FittingResult.Columns[1].Header = "能量(keV)";

            //先不用分段功能了
            dg_FittingData.Columns[2].Visibility = Visibility.Collapsed;

            //加一个保存拟合结果按钮功能，见方法：SaveFittingResult_Click

            //默认添加4个核素的点
            dt_FittingData.Rows.Add(62, 59.5412);
            dt_FittingData.Rows.Add(582, 661.66);
            dt_FittingData.Rows.Add(1009, 1173.23);
            dt_FittingData.Rows.Add(1141, 1332.49);

            #endregion
        }

        /// <summary>
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //关闭应用程序
            //System.Windows.Application.Current.Shutdown();

            e.Cancel = true;
            Hide();
        }

        #region 拟合相关数据

        //两个dt数据
        DataTable dt_FittingData = new DataTable();
        DataTable dt_FittingResult = new DataTable();

        /// <summary>
        /// 拟合结果数据类。
        /// 由于可能会分段拟合，所以会有多个FittingResult对象。
        /// </summary>
        public class FittingResult
        {
            /// <summary>
            /// 拟合结果因子（目前暂时只考虑一次、二次拟合）
            /// </summary>
            private double a, b, c;

            /// <summary>
            /// 拟合数据点的xs
            /// </summary>
            List<double> xs = new List<double>();

            /// <summary>
            /// 拟合数据点的ys
            /// </summary>
            List<double> ys_Ori = new List<double>();

            /// <summary>
            /// 由拟合结果算出的ys
            /// </summary>
            List<double> ys_Cal = new List<double>();

            public double A { get => a; set => a = value; }
            public double B { get => b; set => b = value; }
            public double C { get => c; set => c = value; }
            public List<double> Xs { get => xs; set => xs = value; }
            public List<double> Ys_Ori { get => ys_Ori; set => ys_Ori = value; }
            public List<double> Ys_Cal { get => ys_Cal; set => ys_Cal = value; }
        }

        /// <summary>
        /// 所有的拟合结果（可能有多段）
        /// </summary>
        private List<FittingResult> fittingResults = new List<FittingResult>();
        public List<FittingResult> FittingResults { get => fittingResults; set => fittingResults = value; }

        /// <summary>
        /// 表征准备拟合的类型
        /// </summary>
        FittingType fittingType = FittingType.一次拟合;

        /// <summary>
        /// 给ComboBox绑定用的
        /// </summary>
        FittingType[] fittingTypes = new FittingType[] { FittingType.一次拟合, FittingType.二次拟合, };

        #endregion

        /// <summary>
        /// 初始化两个需要用的表格dt
        /// </summary>
        private void init_dt_FittingData_FittingResult()
        {
            dt_FittingData.Columns.Add("X");
            dt_FittingData.Columns.Add("Y");
            dt_FittingData.Columns.Add("分段点");
            dt_FittingData.Columns["分段点"].DataType = typeof(bool);
            //手动添加行时，如果不对某个列进行设置，则值为空。到时候要考虑判空。对于是否选为分段点，可以只判断是否为true，false和null都表示非分段点
            dg_FittingData.ItemsSource = dt_FittingData.DefaultView;

            dt_FittingResult.Columns.Add("X");
            dt_FittingResult.Columns.Add("Y");
            dt_FittingResult.Columns.Add("验算值");
            dt_FittingResult.Columns.Add("验算误差");
            //foreach (DataColumn column in dt_FittingResult.Columns)
            //{
            //    column.DataType = typeof(double);
            //}
            //这里还是不设置类型了，就保持默认的字符串形式，这样可以给dt上添加自己想要的内容
            dg_FittingResult.ItemsSource = dt_FittingResult.DefaultView;
        }

        /// <summary>
        /// 拟合方法，将拟合结果存在fittingResults
        /// </summary>
        private void DoFitting()
        {
            //每次拟合时，清除上一次的拟合结果
            fittingResults.Clear();

            int rowCount = dt_FittingData.Rows.Count;//数据点dt的总行数
            byte duanNum = 0;//记录当前计算的是第几段，用来显示在结果表里

            //暂存数据点的list
            List<double> x = new List<double>();
            List<double> y = new List<double>();

            //遍历所有数据点（dt的所有行）的循环
            for (int i = 0; i < rowCount; i++)
            {
                DataRow dataRow = dt_FittingData.Rows[i];

                #region 添加一个点

                bool isConvertSucceed = true;
                double ddx, ddy;
                isConvertSucceed &= double.TryParse(dataRow[0].ToString(), out ddx);//尝试将当前行的字符串转化为double
                isConvertSucceed &= double.TryParse(dataRow[1].ToString(), out ddy);

                if (isConvertSucceed)
                {
                    //转换成功才赋值，添加点
                    x.Add(ddx);
                    y.Add(ddy);
                }

                #endregion

                #region 拟合计算

                //以此条件作为进行拟合的条件
                if (dataRow["分段点"].ToString() == "True" || i == rowCount - 1 || !isConvertSucceed)
                {
                    //如果当前行对应的点是分段点或最后一行，则用这之前记录的数据进行一次拟合，否则就继续循环添加点。
                    //对于“!isConvertSucceed”，表示遇到空行，相当于是分段

                    //进行拟合，并将结果保存和显示
                    if (x.Count > 0)//多加一个判断，数据点个数为0的话不进行拟合计算
                    {
                        #region 算出abc
                        double a, b, c;
                        double[] factors = FittingCurve(x.ToArray(), y.ToArray(), fittingType);//二次函数的系数，0~2分别是c、b、a
                        if (factors.Length == 3)//进行二次拟合后才执行a=result2[2]，否则a赋值为0
                            a = factors[2];
                        else
                            a = 0;
                        b = factors[1];
                        c = factors[0];
                        #endregion

                        #region 将拟合结果反映在结果表中

                        DataRow row;
                        //在一段的开始写3行，显示abc的值，便于赋值到表格
                        row = dt_FittingResult.NewRow();
                        row["X"] = "第" + (++duanNum) + "段:";
                        row["验算值"] = "a";
                        row["验算误差"] = a;
                        dt_FittingResult.Rows.Add(row);
                        row = dt_FittingResult.NewRow();
                        row["验算值"] = "b";
                        row["验算误差"] = b;
                        dt_FittingResult.Rows.Add(row);
                        row = dt_FittingResult.NewRow();
                        row["验算值"] = "c";
                        row["验算误差"] = c;
                        dt_FittingResult.Rows.Add(row);

                        for (int k = 0; k < x.Count; k++)//遍历当前所有的数据点，填到结果表中
                        {
                            row = dt_FittingResult.NewRow();
                            row["X"] = x[k];
                            row["Y"] = y[k];
                            double yanSuanZhi = a * x[k] * x[k] + b * x[k] + c;
                            row["验算值"] = yanSuanZhi.ToString("f4");
                            row["验算误差"] = ((yanSuanZhi - y[k]) / y[k] * 100).ToString("f2") + "%";
                            dt_FittingResult.Rows.Add(row);
                        }

                        //添加一个空行
                        dt_FittingResult.Rows.Add(dt_FittingResult.NewRow());

                        #endregion

                        #region 将拟合结果写在结果list中

                        FittingResult result = new FittingResult();
                        result.A = a;
                        result.B = b;
                        result.C = c;
                        result.Xs.AddRange(x);
                        result.Ys_Ori.AddRange(y);
                        for (int j = 0; j < x.Count; j++)
                            result.Ys_Cal.Add(x[j] * x[j] * a + x[j] * b + c);

                        fittingResults.Add(result);

                        //cb_作图曲线选择.Items.Add(cb_作图曲线选择.Items.Count + 1);

                        #endregion

                        //delay(30);//不知道为什么，不延时的话滚动不到最后一行
                        //dg_FittingData.ScrollIntoView(dg_FittingData.Items[dg_FittingData.Items.Count - 1]);//滚动到最后一行
                    }

                    //清除x、y，以备下一段数据使用
                    if (isConvertSucceed)//如果转换失败，说明是用空行分档，则不把最后一个数据留着
                    {
                        double x1, y1;//保存一下最后一个点的数据，也就是分段点的数据
                        x1 = x[x.Count - 1];
                        y1 = y[y.Count - 1];
                        x.Clear();//把这段数据清掉，准备存储下一段的数据
                        y.Clear();
                        x.Add(x1);//把刚刚的分段点先加上
                        y.Add(y1);
                    }
                    else
                    {
                        x.Clear();//把这段数据清掉，准备存储下一段的数据
                        y.Clear();
                    }
                }

                #endregion
            }

            #region 全部拟合完毕，在最后写一个因子列表，比较方便去写参数

            //标题行
            var row0 = dt_FittingResult.NewRow();
            row0[0] = "结果列表";
            row0[1] = "a";
            row0[2] = "b";
            row0[3] = "c";
            dt_FittingResult.Rows.Add(row0);

            //每个拟合结果写一行a b c
            for (int i = 0; i < fittingResults.Count; i++)
            {
                FittingResult item = fittingResults[i];
                var row = dt_FittingResult.NewRow();
                row[0] = i.ToString();
                row[1] = item.A;
                row[2] = item.B;
                row[3] = item.C;
                dt_FittingResult.Rows.Add(row);
            }

            #endregion
        }

        /// <summary>
        /// 切换拟合模式事件
        /// </summary>
        private void cb_FittingMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            fittingType = (FittingType)cb_FittingMode.SelectedItem;
        }

        /// <summary>
        /// 拟合按钮
        /// </summary>
        private void DoFitting_Click(object sender, RoutedEventArgs e)
        {
            //进行拟合
            DoFitting();

            //更新画图界面的cb
            w_Fitting.cb_ResultNum.Items.Clear();
            for (int i = 0; i < fittingResults.Count; i++)
            {
                w_Fitting.cb_ResultNum.Items.Add("第" + (i + 1) + "段");
            }
            //默认选第一个
            if (w_Fitting.cb_ResultNum.Items.Count > 0)
                w_Fitting.cb_ResultNum.SelectedIndex = 0;
        }

        /// <summary>
        /// 拟合数据表的keydown事件，用于Control+V粘贴内容
        /// </summary>
        private void dg_FittingData_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            {
                try
                {
                    DataGrid dg_temp = e.Source as DataGrid;//这里用e.Source，就可以在Window触发这个事件时，找到触发的DataGrid了
                    if (dg_temp == null)
                    {
                        return;
                    }
                    DataTable dt_temp = dt_FittingData;
                    CopyHelper.PasteToDg(dt_temp, dg_temp);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// 清空拟合结果dg的内容
        /// </summary>
        private void ClearFittingResult_Click(object sender, RoutedEventArgs e)
        {
            dt_FittingResult.Rows.Clear();
        }

        /// <summary>
        /// 打开拟合图表窗口按钮
        /// </summary>
        private void OpenFittingChart_Click(object sender, RoutedEventArgs e)
        {
            adc.OpenOneWindow(w_Fitting);
        }

        /// <summary>
        /// 保存拟合结果按钮
        /// </summary>
        private void SaveFittingResult_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //获取一下软件中存储能量刻度因子的对象
                集成的数据类.ReceDatas.FactorEnergy fac = MainWindow.Instance.receDatas.factorEnergy;

                //从拟合结果中得到abc
                foreach (var item in fittingResults)//对于能量刻度，只会有一个结果
                {
                    fac.a = item.A;
                    fac.b = item.B;
                    fac.c = item.C;
                }

                //存到文件中
                string[] lines = new string[5];
                lines[0] = "分别在等号后设置二次函数因子的值。可以放多组abc，放到后面的为最终使用的。";
                lines[2] = "a=" + fac.a;
                lines[3] = "b=" + fac.b;
                lines[4] = "c=" + fac.c;
                MainWindow.Instance.andyFileRW.WriteFileInResources("\\刻度因子\\能量刻度因子.txt", lines);

                MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败，详情："+ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }
    }
}
