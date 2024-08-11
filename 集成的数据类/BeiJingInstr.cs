using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 核素识别仪.集成的数据类
{
    /// <summary>
    /// 北京多道的指令集
    /// </summary>
    class BeiJingInstr
    {
        private string cmd_Measure = "FFAABBCC0000000200000000";
        /// <summary>
        /// 采集多道数据指令
        /// </summary>
        public string Cmd_Measure
        {
            get { return cmd_Measure; }
            set { cmd_Measure = value; }
        }

        /// <summary>
        /// 采集盖革管通道0的CPS的指令
        /// </summary>
        public string Cmd_MeasureGMCPS0 { get; set; } = "FFAABBCC0000002200000000";

        /// <summary>
        /// 采集盖革管通道1的CPS的指令
        /// </summary>
        public string Cmd_MeasureGMCPS1 { get; set; } = "FFAABBCC0000002200000002";

        private string cmd_Stop = "FFAABBCC0000000500000000";
        /// <summary>
        /// 停止指令
        /// </summary>
        public string Cmd_Stop
        {
            get { return cmd_Stop; }
            set { cmd_Stop = value; }
        }

        private string cmd_Clear = "FFAABBCC0000000700000000";
        /// <summary>
        /// 清除指令
        /// </summary>
        public string Cmd_Clear
        {
            get { return cmd_Clear; }
            set { cmd_Clear = value; }
        }

        private string cmd_Start = "FFAABBCC0000000400000000";
        /// <summary>
        /// 开始指令
        /// </summary>
        public string Cmd_Start
        {
            get { return cmd_Start; }
            set { cmd_Start = value; }
        }


    }
}
