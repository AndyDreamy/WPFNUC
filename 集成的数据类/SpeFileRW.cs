using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 核素识别仪.集成的数据类
{
    /// <summary>
    /// Spe文件读写相关类
    /// </summary>
    class SpeFileRW
    {
        double liveTime = 0;//缺少活时间
        string startTimeStr = MainWindow.Instance.autoRun.P_startTimeStr;
        DateTime startTime = MainWindow.Instance.autoRun.P_startTime;
        double msTime = MainWindow.Instance.receDatas.P_measuredTime;
        double[] datas = MainWindow.Instance.receDatas.MultiDatas;

        /// <summary>
        /// 根据当前的多道数据、开始时间、活时间、实时间准备好spe文件的内容
        /// </summary>
        /// <returns></returns>
        public string GenerateSpeFile()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("$SPEC_ID:" + "\r\n" +
                    "DEMO3.SPC IS A FULLY CALIBRATED SPECTRUM, CONTAINING BOTH" + "\r\n" +
                    "$SPEC_REM:" + "\r\n" +
                    "DET# 12592" + "\r\n" +
                    "DETDESC# EG&G ORTEC GMX 10175-PLUS TRANSISTOR RESET PREAMPLIFIER" + "\r\n" +
                    "AP# GammaVision Version 6.07" + "\r\n" +
                    "$DATE_MEA:" + "\r\n" +
                    startTimeStr.Replace('\n', ' ') + "\r\n" +
                    "$MEAS_TIM:" + "\r\n" +
                    liveTime + " " + msTime + "\r\n" +
                    "$DATA:" + "\r\n" +
                    "0 " + "2047" + "\r\n"
            );

            for (int b = 0; b < datas.Length; b++)
            {
                sb.Append("        ").Append(datas[b]).Append("\r\n");
            }


            sb.Append("$ROI:" + "\r\n" +
                    "0" + "\r\n" +
                    "$PRESETS:" + "\r\n" +
                    "None" + "\r\n" +
                    "0" + "\r\n" +
                    "0" + "\r\n" +
                    "$ENER_FIT:" + "\r\n" +
                    "14.397512 0.494317" + "\r\n" +
                    "$MCA_CAL:" + "\r\n" +
                    "3" + "\r\n" +
                    "1.439751E+001 4.943165E-001 0.000000E+000 keV " + "\r\n" +
                    "$SHAPE_CAL:" + "\r\n" +
                    "3" + "\r\n" +
                    "2.116154E+000 5.110860E-004 0.000000E+000"
            );
            return sb.ToString();
        }

        //读取spe文件中的内容（List<String>），更新多道数据、开始时间、测量时间、活时间
        public void LoadSpeFile(List<string> lines)
        {
            try
            {
                //开始时间
                DateTime.TryParse(lines[7], out startTime);
                MainWindow.Instance.autoRun.P_startTime = startTime;

                String[] strs = lines[9].Split(new char[] { ' ' });
                //活时间
                double.TryParse(strs[0], out liveTime);

                //测量时间
                double.TryParse(strs[1], out msTime);
                MainWindow.Instance.receDatas.P_measuredTime = msTime;


                //多道数据，从自己找一下
                int dataIndex = -1;//"$DATA:"的Index
                int ROIIndex = -1;//找一下ROI的Index，在两个位置之间就是数据
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Equals("$DATA:"))
                    {
                        dataIndex = i;
                    }
                    if (lines[i].Equals("$ROI:"))
                    {
                        ROIIndex = i;
                    }
                }
                if (dataIndex != -1 && ROIIndex != -1 && (ROIIndex - dataIndex - 2) <= datas.Length)
                {
                    //如果找到了"$DATA:"、"$ROI :"，且多道数据总数小于backupData的总数
                    for (int i = dataIndex + 2; i < ROIIndex; i++)
                    {
                        double.TryParse(lines[i].Replace(" ", ""), out datas[i - dataIndex - 2]);
                    }
                }

                #region 画图、核素识别

                //这里加载spe文件，更新了多道数据，则置位数据更新标志位
                MainWindow.Instance.autoRun.P_isDataNew = true;

                //进行画图和识别
                MainWindow.Instance.DrawAndReco();

                #endregion

                MainWindow.Instance.w_Note.ShowNote("加载成功", 1500);
            }
            catch (Exception e)
            {
                MainWindow.Instance.w_Note.ShowNote("加载失败，详情："+e.Message, 1500);
            }


        }

    }
}
