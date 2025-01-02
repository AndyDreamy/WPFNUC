using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 核素识别仪.Models
{
    /// <summary>
    /// 表征在稳峰过程中找到的一组峰位信息。
    /// 每10道分一组，找到的道址落在这一组，就给这一组的FindCount加一，并影响到这一组的峰位均值。
    /// 最终，满足要求的峰位组中找到次数最多的，取其均值认为是稳峰结果道址
    /// </summary>
    public class StaPeakInfoModel
    {
        /// <summary>
        /// 峰位组别道址，精确到10位
        /// </summary>
        public double PeakGroupChannel { get; set; }

        /// <summary>
        /// 这一组的平均道址。为所有找到的峰位的均值
        /// </summary>
        public double PeadAverageChannel { get; set; }

        /// <summary>
        /// 找到这组峰位的次数
        /// </summary>
        public int FindCount { get; set; }
    }
}
