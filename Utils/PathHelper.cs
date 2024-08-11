using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 核素识别仪.Utils
{
    public class PathHelper
    {
        private static readonly Lazy<PathHelper> _instance = new Lazy<PathHelper>(() => new PathHelper());
        public static PathHelper Instance => _instance.Value;

        public string ExePath => Directory.GetCurrentDirectory();

        public string ResourcePath => Path.Combine(ExePath, "Resources");

        public string DBFolderPath => Path.Combine(ResourcePath, "Database");

    }
}
