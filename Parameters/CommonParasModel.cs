using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 核素识别仪.Models;

namespace 核素识别仪.Parameters
{
    /// <summary>
    /// 定义此应用公共的参数
    /// </summary>
    public class CommonParasModel
    {
        #region 语言

        public LanguageEnum Language { get; set; } = LanguageEnum.Chinese;

        #endregion
    }
}
