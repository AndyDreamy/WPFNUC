using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 核素识别仪.其他功能类.SQLite
{
    public class AndySQLite
    {
        #region Constructor

        public AndySQLite()
        {

        }

        #endregion

        #region Fields

        private readonly string dateTimeFormat = "yyyy-MM-dd HH:mm:ss.sss";

        #endregion

        #region 属性定义

        private string dbPath;
        /// <summary>
        /// 数据库的路径，需要在实例化AndySQLite前设置好
        /// </summary>
        public string P_dbPath
        {
            get { return dbPath; }
            set
            {
                dbPath = value;
                SetConnStr();//同步设置连接字符串
            }
        }

        /// <summary>
        /// 连接字符串，需要执行SetConnStr方法设置
        /// </summary>
        private string connStr;

        /// <summary>
        /// 是否可以成功连接数据库
        /// </summary>
        public bool CanConnect { get; set; } = false;

        #endregion

        #region 创建、删除方法

        /// <summary>
        /// 创建数据库
        /// </summary>
        /// <param name="DBName"></param>
        public void CreateDatabase(string DBName)
        {
            string filePath = $"{DBName}.db";
            if (!File.Exists(filePath))
            {
                SQLiteConnection.CreateFile(filePath);
            }
        }

        /// <summary>
        /// 创建一个数据表
        /// </summary>
        /// <param name="sqlStr">直接由外界提供语句执行吧</param>
        public void CreateDt(string tableName)
        {
            //CREATE DATABASE IF NOT EXISTS testdb

            //准备sql语句
            string sqlStr = $"CREATE DATABASE IF NOT EXISTS {tableName}";

            //连接并执行语句
            ExecuteQuery(sqlStr);
        }

        /// <summary>
        /// 删除一个数据表
        /// </summary>
        /// <param name="dtName">数据表名称</param>
        public void DeleteDt(string DBName)
        {
            // drop table student;

            //确保连接到了一个数据库
            if (false)
            {
                string e = "删除表";
                Console.WriteLine(e + "失败，SQLite未连接到一个数据库");
                return;
            }

            //准备sql语句
            string sqlStr = "drop table " + DBName + ";";

            //连接并执行语句
            ExecuteQuery(sqlStr);
        }

        #endregion

        #region 数据库连接方法

        /// <summary>
        /// 根据dbPath设置好connStr
        /// </summary>
        public void SetConnStr()
        {
            var sb = new SQLiteConnectionStringBuilder()
            {
                DataSource = dbPath,
                Version = 3,
            };
            connStr = sb.ToString();
        }

        /// <summary>
        /// 连接SQLite数据库
        /// </summary>
        public void ConnectDB(SQLiteConnection conn)
        {
            try
            {
                if (conn == null)
                {
                    Console.WriteLine("conn对象为null");
                    return;
                }

                //连接
                conn.Open();
                Console.WriteLine("已连接SQLite\r\n" + conn.ConnectionString);

                //连接成功后，把相关字段初始化好

            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("SQLite连接失败，详情：" + ex.Message);
                //switch (ex.ErrorCode)
                //{
                //    case 0:
                //        Console.WriteLine("Cannot connect to server");
                //        break;
                //    case 1045:
                //        Console.WriteLine("Invalid username/password,please try again");
                //        break;
                //}
            }
            catch (Exception e)
            {
                Console.WriteLine("SQLite连接失败，详情：" + e.Message);
            }
        }

        /// <summary>
        /// 断开SQLite数据库的连接，程序关闭时执行即可。
        /// </summary>
        public void DisconnectDB(SQLiteConnection conn)
        {
            if (conn == null)
            {
                Console.WriteLine("conn对象为null");
                return;
            }
            try
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
                Console.WriteLine("已断开与SQLite的连接");
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("SQLite断连失败，详情：" + ex.Message);
            }
        }

        /// <summary>
        /// 测试连接SQLite数据库
        /// </summary>
        /// <returns>是否连接成功</returns>
        public bool TestConnDB()
        {
            SetConnStr();
            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                //建立数据库连接
                ConnectDB(conn);

                //判断是否连接，若没有连接，则退出这个方法
                if (conn.State != ConnectionState.Open)
                {
                    Console.WriteLine("SQLite未连接到数据库");
                    return false;
                }

                //断开连接
                DisconnectDB(conn);
            }
            CanConnect = true;
            Console.WriteLine("测试连接SQLite成功！");
            return true;
        }

        #endregion

        #region 查、增、删数据方法

        /// <summary>
        /// 输入查询的SQL字符串，查询一系列数据，返回数据表
        /// </summary>
        /// <param name="sqlStr">SQL语句</param>
        /// <returns>存着数据的数据表。异常：若出现错误，则返回没有列的dt；若成功连接但没有数据，则返回的dt不包含数据</returns>
        public DataTable QueryDB(string sqlStr)
        {
            DataTable dt = new DataTable();

            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                //建立数据库连接
                ConnectDB(conn);

                //判断是否连接，若没有连接，则退出这个方法
                if (conn.State != ConnectionState.Open)
                {
                    Console.WriteLine("SQLite未连接到数据库，无法进行查询");
                    return dt;
                }

                //准备工具
                using (SQLiteCommand comm = new SQLiteCommand(sqlStr, conn))
                {
                    using (SQLiteDataAdapter adp = new SQLiteDataAdapter(comm))
                    {
                        try
                        {
                            adp.Fill(dt);
                            Console.WriteLine("成功查询到" + dt.Rows.Count + "行数据");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("查询失败" + ex.Message);
                        }
                    }
                }

                //断开连接
                DisconnectDB(conn);
            }

            return dt;
        }

        /// <summary>
        /// 查询一个数据表的所有数据
        /// </summary>
        /// <param name="tableName">数据表名</param>
        /// <returns></returns>
        public DataTable QueryAllFromTable(string tableName)
        {
            string sql = $"SELECT * FROM \"{tableName}\"";
            DataTable dt = QueryDB(sql);
            return dt;
        }

        /// <summary>
        /// 查询一段时间的数据
        /// </summary>
        /// <returns></returns>
        public DataTable QueryDataInPeriod(string tableName,string dateColumnName, DateTime startTime, DateTime endTime)
        {
            string sql = $"SELECT * FROM \"{tableName}\" " +
                $"WHERE {dateColumnName} BETWEEN \"{startTime.ToString(dateTimeFormat)}\" AND \"{endTime.ToString(dateTimeFormat)}\"";
            DataTable dt = QueryDB(sql);
            return dt;
        }

        /// <summary>
        /// 数据库插入数据方法。
        /// </summary>
        /// <param name="tbName">插入的数据表</param>
        /// <param name="columnNames">设置的列名集合</param>
        /// <param name="values">与列名集合中的列一一对应的设置值的字符串</param>
        public int Insert(string tbName, string[] columnNames, string[] values)
        {
            //成功插入的数量
            int insertCount = 0;

            #region 准备sql语句

            StringBuilder sb = new StringBuilder();
            //示例："INSERT INTO [NOI] ([Nuc_id], [Nuc_name], [Nuc_type]) VALUES (@Nuc_id, @Nuc_name,@Nuc_type);"
            sb.Append("INSERT INTO ");
            sb.Append($"\"{tbName}\"");
            sb.Append(" (");
            for (int i = 0; i < columnNames.Length; i++)
            {
                sb.Append($"\"{columnNames[i]}\"");
                sb.Append(',');
            }
            sb.Remove(sb.Length - 1, 1);//除去最后一个逗号
            sb.Append(") ");
            sb.Append("values");
            sb.Append(" (");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append('\"');
                sb.Append(values[i]);
                sb.Append('\"');
                sb.Append(',');
            }
            sb.Remove(sb.Length - 1, 1);//除去最后一个逗号
            sb.Append(");");
            string sqlStr = sb.ToString();

            #endregion

            //执行sql语句
            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                try
                {
                    //建立数据库连接
                    ConnectDB(conn);

                    //判断是否连接，若没有连接，则退出这个方法
                    if (conn.State != ConnectionState.Open)
                    {
                        Console.WriteLine("SQLite未连接到数据库，无法进行查询");
                        return 0;
                    }

                    using (SQLiteCommand comm = new SQLiteCommand(sqlStr, conn))
                    {
                        int count = comm.ExecuteNonQuery();
                        Console.WriteLine("成功插入" + count + "条数据");
                        insertCount = count;
                    }

                    //断开连接
                    DisconnectDB(conn);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("插入数据失败，详情" + ex.Message);
                    //throw;
                    //断开连接
                    DisconnectDB(conn);
                }

            }

            return insertCount;
        }

        /// <summary>
        /// 数据库插入数据方法。
        /// </summary>
        /// <param name="tbName">插入的数据表</param>
        /// <param name="columnNames">设置的列名集合</param>
        /// <param name="values">与列名集合中的列一一对应的设置值的字符串</param>
        public int InsertMany(string tbName, string[] columnNames, List<string[]> values)
        {
            //成功插入的数量
            int insertCount = 0;

            //用来存多条插入语句
            string sqlStr = "";

            //生成insert的sql语句
            for (int j = 0; j < values.Count; j++)
            {
                //values是一个字符串数据的list，list中每个元素都是单独的一条数据，这里遍历所有数据
                StringBuilder sb = new StringBuilder();
                //示例："INSERT INTO [NOI] ([Nuc_id], [Nuc_name], [Nuc_type]) VALUES (@Nuc_id, @Nuc_name,@Nuc_type);"
                sb.Append("INSERT INTO ");
                sb.Append(tbName);

                //这里如果是向所有的列插入数据，则可以省略列名
                //sb.Append(" (");
                //for (int i = 0; i < columnNames.Length; i++)
                //{
                //    sb.Append(columnNames[i]);
                //    sb.Append(',');
                //}
                //sb.Remove(sb.Length - 1, 1);//除去最后一个逗号
                //sb.Append(") ");

                sb.Append(" values");
                sb.Append(" (");
                for (int i = 0; i < values[j].Length; i++)
                {
                    //sb.Append('\'');//这里不要加单引号，而是由外界判断，是字符串的时候，才加单引号，否则不加
                    sb.Append(values[j][i]);
                    //sb.Append('\'');
                    sb.Append(',');
                }
                sb.Remove(sb.Length - 1, 1);//除去最后一个逗号
                sb.Append(");");

                sqlStr += sb.ToString();
            }

            //执行sql语句
            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                try
                {
                    //建立数据库连接
                    ConnectDB(conn);

                    //判断是否连接，若没有连接，则退出这个方法
                    if (conn.State != ConnectionState.Open)
                    {
                        Console.WriteLine("SQLite未连接到数据库，无法进行查询");
                        return 0;
                    }

                    using (SQLiteCommand comm = new SQLiteCommand(sqlStr, conn))
                    {
                        int count = comm.ExecuteNonQuery();
                        Console.WriteLine("成功插入" + count + "条数据");

                        insertCount = count;

                        ////遍历所有的sql语句，依次执行
                        //for (int k = 0; k < sqlStrs.Count; k++)
                        //{
                        //    comm.CommandText = sqlStrs[k];

                        //}
                    }

                    //断开连接
                    DisconnectDB(conn);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("插入数据失败，详情" + ex.Message);
                    //throw;
                    //断开连接
                    DisconnectDB(conn);
                }

            }

            return insertCount;
        }

        /// <summary>
        /// 插入一系列数据，但可以直接输入想要插入的dt，只要和要插入的表数据结构相同即可。
        /// </summary>
        /// <param name="tbName">要插入的数据表名</param>
        /// <param name="dt">要插入的数据，要求和数据表结构相同</param>
        /// <returns></returns>
        public int InsertManyFromDt(string tbName, DataTable dt)
        {
            int insertCount = 0;

            //获取所有列名coNames
            int coCount = dt.Columns.Count;
            string[] coNames = new string[coCount];
            for (int i = 0; i < coCount; i++)
                coNames[i] = dt.Columns[i].Caption;

            //准备设置的值
            List<string[]> values = new List<string[]>();
            for (int i = 0; i < dt.Rows.Count; i++)//遍历所有行数据
            {
                DataRow row = dt.Rows[i];
                string[] value = new string[coCount];
                for (int j = 0; j < coCount; j++)
                {
                    value[j] = row[j].ToString();
                    if (row[j].GetType() == typeof(string))//对于字符串，需要加单引号，其他数据则不用
                        value[j] = "'" + value[j] + "'";
                }
                values.Add(value);
            }

            //生成Insert语句并执行
            insertCount = InsertMany(tbName, coNames, values);

            return insertCount;
        }

        /// <summary>
        /// 清空数据表中的数据的方法。
        /// </summary>
        /// <param name="tbName">插入的数据表</param>
        public void ClearDt(string tbName)
        {

            StringBuilder sb = new StringBuilder();
            //示例："delete from student;"
            sb.Append("delete from ");
            sb.Append(tbName);
            sb.Append(";");

            string sqlStr = sb.ToString();

            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                try
                {
                    //建立数据库连接
                    ConnectDB(conn);

                    //判断是否连接，若没有连接，则退出这个方法
                    if (conn.State != ConnectionState.Open)
                    {
                        Console.WriteLine("SQLite未连接，无法进行查询");
                        return;
                    }

                    using (SQLiteCommand comm = new SQLiteCommand(sqlStr, conn))
                    {
                        int count = comm.ExecuteNonQuery();
                        Console.WriteLine("成功清空数据表" + tbName);
                    }

                    //断开连接
                    DisconnectDB(conn);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("清空失败，详情" + ex.Message);
                    //throw;
                    //断开连接
                    DisconnectDB(conn);
                }

            }


        }

        #endregion

        #region 基本方法

        /// <summary>
        /// 公用方法：执行一条sql指令
        /// </summary>
        /// <param name="sqlStr"></param>
        public void ExecuteQuery(string sqlStr)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                try
                {
                    //建立数据库连接
                    ConnectDB(conn);

                    //判断是否连接，若没有连接，则退出这个方法
                    if (conn.State != ConnectionState.Open)
                    {
                        Console.WriteLine("SQLite未连接");
                        return;
                    }

                    using (SQLiteCommand comm = new SQLiteCommand(sqlStr, conn))
                    {
                        comm.ExecuteNonQuery();
                        Console.WriteLine("执行成功，指令：" + sqlStr);
                    }

                    //断开连接
                    DisconnectDB(conn);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("执行失败，详情：" + ex.Message);
                }
            }
        }
        #endregion
    }
}
