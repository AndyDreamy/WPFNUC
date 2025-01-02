using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;

namespace 核素识别仪.Utils
{
    public class FileHelp
    {
        public static T DeserializeFromXml<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new ArgumentNullException(filePath + " not Exists");

                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                {
                    return (T)serializer.Deserialize(fileStream);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void SerializeToXmlFile<T>(T obj, string filePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (XmlWriter writer = XmlWriter.Create(filePath, new XmlWriterSettings() { Indent = true }))
                {
                    serializer.Serialize(writer, obj);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// 将某个类型的数据集合写入Csv文件。
        /// 类型的所有公共属性会依次作为列标题呈现，数据集合的每项数据会作为一行数据呈现。
        /// </summary>
        /// <typeparam name="T">对象数据类型</typeparam>
        /// <param name="items">对象数据集合</param>
        /// <param name="filePath">保存文件的全路径</param>
        public static void WriteCsv<T>(IEnumerable<T> items, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                //获取公共属性信息，并根据CsvOrderAttribute排序
                var allPropInfos = typeof(T).GetProperties().OrderBy(p => p.GetCustomAttribute<CsvOrderAttribute>()?.Order ?? int.MaxValue);
                var propInfos = allPropInfos.Where((p) => !Attribute.IsDefined(p, typeof(CsvIgnoreAttribute))).ToArray();

                //写标题行
                for (int i = 0; i < propInfos.Length; i++)
                {
                    var prop = propInfos[i];
                    writer.Write(prop.Name);
                    if (i < propInfos.Length - 1)
                        writer.Write(",");
                }
                writer.WriteLine();

                //遍历对象写入每一行
                foreach (var item in items)
                {
                    for (int i = 0; i < propInfos.Length; i++)
                    {
                        var prop = propInfos[i];
                        writer.Write(prop.GetValue(item, null)?.ToString() ?? "");//若数据为null，则填写空字符串
                        if (i < propInfos.Length - 1)
                            writer.Write(",");
                    }
                    writer.WriteLine();
                }
            }
        }

        /// <summary>
        /// 从Csv文件读取某个类型的数据对象集合。
        /// 读取失败将抛出异常
        /// </summary>
        /// <typeparam name="T">对象数据类型</typeparam>
        /// <param name="filePath">保存文件的全路径</param>
        /// <returns>读取的对象集合</returns>
        public static List<T> ReadCsv<T>(string filePath) where T : new()
        {
            List<T> items = new List<T>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                // 解析标题行
                string titleLine = reader.ReadLine();
                string[] titles = titleLine.Split(',');
                Dictionary<string, int> dicTitleIndex = new Dictionary<string, int>();//创建列标题对应Index的字典
                for (int i = 0; i < titles.Length; i++)
                {
                    dicTitleIndex[titles[i]] = i;
                }

                while ((line = reader.ReadLine()) != null)
                {
                    string[] fields = line.Split(',');
                    T item = new T();
                    var allPropInfos = typeof(T).GetProperties();
                    var propInfos = allPropInfos.Where((p) => !Attribute.IsDefined(p, typeof(CsvIgnoreAttribute))).ToArray();

                    for (int i = 0; i < propInfos.Length; i++)
                    {
                        try
                        {
                            PropertyInfo prop = propInfos[i];
                            string thisField = fields[dicTitleIndex[prop.Name]];

                            if (prop.CanWrite)
                            {
                                // 尝试转换字段值到相应的属性类型
                                Type type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                                /* 解释一下这一句：
                                 * 正常就是想获取property.PropertyType，也就是这个属性的类型
                                 * 但要应对一种情况，就是可空类型，例如property的类型是int?，那么property.PropertyType就是int?，但数据转换方法不支持转换为int?
                                 * 故，通过Nullable.GetUnderlyingType得到这种可空类型int?的具体类型int。
                                 * 如果对非可空类型，Nullable.GetUnderlyingType(prop.PropertyType)为null，则类型就直接取property.PropertyType
                                 */

                                object value;
                                if (type.Name == "Type")//Type类型的属性比较特殊，字符串需要写全，例如float类型的字符串为System.Single，不建议保存Type类型的属性
                                {
                                    value = Type.GetType(thisField);
                                }
                                else
                                {
                                    value = Convert.ChangeType(thisField, type);
                                }
                                prop.SetValue(item, value, null);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }
                    items.Add(item);
                }
            }
            return items;
        }

        /// <summary>
        /// 拷贝文件夹，若目标已存在，则会替换
        /// </summary>
        /// <param name="sourceFolder">源目录</param>
        /// <param name="destinationFolder">目标目录</param>
        public static void CopyFolder(string sourceFolder, string destinationFolder)
        {
            try
            {
                // 检查目标文件夹是否存在，如果不存在则创建
                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                // 获取源文件夹中所有文件（包括子文件夹中的文件）
                var files = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    // 构建目标文件路径
                    string destFile = file.Replace(sourceFolder, destinationFolder);

                    // 获取目标文件的目录路径
                    string directoryName = Path.GetDirectoryName(destFile);

                    // 如果目标目录不存在，则创建它
                    if (!Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    // 拷贝文件
                    File.Copy(file, destFile, true); // true表示如果目标文件已存在，则覆盖它
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

    /// <summary>
    /// 用于读写Csv文件时忽略的属性。
    /// 若某个属性添加了这个标签，则在读写Csv时将忽略这个属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CsvIgnoreAttribute : Attribute
    {

    }

    /// <summary>
    /// 用于设置在Csv文件根据属性生成列时的排序。按照Order值从小到大顺序排列
    /// </summary>
    public class CsvOrderAttribute : Attribute
    {
        public int Order { get; private set; }

        public CsvOrderAttribute(int order)
        {
            Order = order;
        }
    }
}
