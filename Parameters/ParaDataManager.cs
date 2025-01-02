using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 核素识别仪.Utils;

namespace 核素识别仪.Parameters
{
    /// <summary>
    /// 管理参数数据
    /// </summary>
    public class ParaDataManager
    {
        private static readonly Lazy<ParaDataManager> _instance = new Lazy<ParaDataManager>(() => new ParaDataManager());
        public static ParaDataManager Instance { get => _instance.Value; }
        private ParaDataManager()
        {

        }

        #region ParaData

        private string commonParasFilePath = Path.Combine(PathHelper.ResourcePath, "CommonParas.xml");
        private CommonParasModel commonParas;
        public CommonParasModel CommonParas => commonParas;

        #endregion

        #region Public Methods

        public bool ReadAllParas()
        {
            bool readOK = false;

            try
            {
                commonParas = FileHelp.DeserializeFromXml<CommonParasModel>(commonParasFilePath);
                readOK = true;
            }
            catch (Exception ex)
            {
                AndyLogger.Ins.Logger.Error(ex, $"读取参数失败，详情：{ex.Message}");
            }

            return readOK;
        }

        public bool WriteAllParas()
        {
            bool writeOK = false;

            try
            {
                FileHelp.SerializeToXmlFile(commonParas, commonParasFilePath);
                writeOK = true;
            }
            catch (Exception ex)
            {
                AndyLogger.Ins.Logger.Error(ex, $"保存参数失败，详情：{ex.Message}");
            }

            return writeOK;
        }

        #endregion
    }
}
