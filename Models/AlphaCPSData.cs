using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 核素识别仪.Models
{
    /// <summary>
    /// αCPS数据模型
    /// </summary>
    public class AlphaCPSData
    {
        public DateTime Time { get; set; }
        public double CPS { get; set; }
    }
}
