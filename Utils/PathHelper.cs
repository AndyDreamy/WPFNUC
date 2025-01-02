using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 核素识别仪.Utils
{
    public static class PathHelper
    {
        public static string ExePath => Directory.GetCurrentDirectory();
               
        public static string ResourcePath => Path.Combine(ExePath, "Resources");
               
        public static string DBFolderPath => Path.Combine(ResourcePath, "Database");

    }
}
