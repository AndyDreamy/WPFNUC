using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 核素识别仪.其他功能类.SQLite
{
    public class SQLiteHelper
    {
        #region Instance

        private static Lazy<SQLiteHelper> _instance = new Lazy<SQLiteHelper>(() => new SQLiteHelper());
        public static SQLiteHelper Instance { get { return _instance.Value; } }

        #endregion

        #region Constructor

        private SQLiteHelper()
        {

        }

        #endregion

        #region Fields



        #endregion
    }
}
