using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 核素识别仪.Models;
using 核素识别仪.Utils;
using 核素识别仪.其他功能类.SQLite;

namespace 核素识别仪.Servers.DataServer
{
    /// <summary>
    /// 管理αCPS数据的数据库操作
    /// </summary>
    public class AlphaCPSDataManager
    {
        private static readonly Lazy<AlphaCPSDataManager> _instance = new Lazy<AlphaCPSDataManager>(() => new AlphaCPSDataManager());
        public static AlphaCPSDataManager Instance => _instance.Value;

        public AlphaCPSDataManager()
        {
            columnsOfCPSHistory = new string[] { dateColumnName, cpsColumnName };
        }

        #region Fields

        /// <summary>
        /// 用于读写CPS数据的AndySQLite实例
        /// </summary>
        private readonly AndySQLite cpsSQLite = new AndySQLite();

        /// <summary>
        /// 存储CPS历史的数据表名称
        /// </summary>
        private readonly string cpsDBName = "CPSDB.db";

        /// <summary>
        /// 存储CPS历史的数据表名称
        /// </summary>
        private readonly string cpsTableName = "CPSHistory";

        /// <summary>
        /// 存储时间这一列的列名
        /// </summary>
        private readonly string dateColumnName = "Time";

        /// <summary>
        /// 存储CPS这一列的列名
        /// </summary>
        private readonly string cpsColumnName = "CPS";

        /// <summary>
        /// CPS历史数据表的列名称
        /// </summary>
        private readonly string[] columnsOfCPSHistory;

        #endregion

        #region Properties

        /// <summary>
        /// 当前查询的CPS数据列表
        /// </summary>
        public ObservableCollection<AlphaCPSData> CPSDatasFromDB { get; set; } = new ObservableCollection<AlphaCPSData>();

        #endregion

        #region Methods

        /// <summary>
        /// 初始化CPS数据库相关，设置数据库路径并测试连接
        /// </summary>
        public void Init()
        {
            cpsSQLite.P_dbPath = Path.Combine(PathHelper.DBFolderPath, cpsDBName);
            cpsSQLite.TestConnDB();
        }

        /// <summary>
        /// 向数据库插入一条CPS数据
        /// </summary>
        /// <param name="cps"></param>
        public void InsertOneCPS(AlphaCPSData cps)
        {
            string[] values = new string[] { $"datetime('{cps.Time.ToString(cpsSQLite.dateTimeFormat)}')", cps.CPS.ToString() };
            cpsSQLite.Insert(cpsTableName, columnsOfCPSHistory, values);
        }

        /// <summary>
        /// 查询一段时间的CPS数据
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void QueryDataInPeriod(DateTime start, DateTime end)
        {
            DataTable dataTable = cpsSQLite.QueryDataInPeriod(cpsTableName, dateColumnName, start, end);
            LoadCPSDataFromDBTable(dataTable);
        }

        /// <summary>
        /// 查询所有CPS数据
        /// </summary>
        public void QueryAllData()
        {
            #region 测试代码

            ////添加一些数据
            //DateTime now = DateTime.Now;
            //int count = 100;
            //Random r = new Random();
            //for (int i = 0; i < count; i++)
            //{
            //    DateTime thisTime = now.AddMinutes(count - i);
            //    double d = r.Next(100, 3000) / 100d;
            //    InsertOneCPS(new AlphaCPSData() { CPS = d, Time = thisTime });
            //}

            #endregion
            DataTable dataTable = cpsSQLite.QueryAllFromTable(cpsTableName);
            LoadCPSDataFromDBTable(dataTable);
        }

        /// <summary>
        /// 从数据表加载CPS数据
        /// </summary>
        private void LoadCPSDataFromDBTable(DataTable dt)
        {
            CPSDatasFromDB.Clear();
            foreach (DataRow row in dt.Rows)
            {
                DateTime.TryParse(row[dateColumnName].ToString(), out DateTime date);
                double cps = (double)row[cpsColumnName];
                CPSDatasFromDB.Add(new AlphaCPSData() { CPS = cps, Time = date });
            }
        }

        #endregion
    }
}
