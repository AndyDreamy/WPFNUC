using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 核素识别仪.Utils
{
    /// <summary>
    /// 用于记录日志，一般使用Logger.Error和Logger.Info方法打印信息。
    /// 使用Info方法，会在生成目录的Log目录下，创建Log+日期.log文件记录信息；使用
    /// 引用NLog.dll
    /// 官网说明：https://github.com/NLog/NLog/wiki/Tutorial#configure-nlog-targets-for-output
    /// </summary>
    public class AndyLogger
    {
        private const string LogFileTargetName = "logFileTarget";
        private const string LogErrorFileTargetName = "logErrorFileTarget";
        private const string LogConsoleTargetName = "logConsoleTarget";
        private static Lazy<AndyLogger> instance = new Lazy<AndyLogger>(() => new AndyLogger());
        public static AndyLogger Ins => instance.Value;

        private Logger _logger;
        public Logger Logger => _logger;

        public AndyLogger()
        {
            //定义配置信息
            var config = new LoggingConfiguration();

            //定义Log Target
            var fileTarget = new FileTarget(LogFileTargetName)
            {
                FileName = "Log/Log${shortdate}.log",
                Layout = "[${longdate}] [${level}]--${message} ${exception}"
            };
            var errorFileTarget = new FileTarget(LogErrorFileTargetName)
            {
                FileName = "Log/Error${shortdate}.log",
                Layout = "[${longdate}] [${level}]--${message} ${exception}"
            };
            var consoleTarget = new ConsoleTarget(LogConsoleTargetName)
            {
                Layout = "[${longdate}] [${level}]--${message} ${exception}"
            };
            //异步Target封装，可提高性能：
            //var asyncTarget = new AsyncTargetWrapper(fileTarget);

            //定义Rules for mapping loggers to target
            config.AddRuleForAllLevels(fileTarget);
            config.AddRuleForOneLevel(LogLevel.Error, errorFileTarget);
            config.AddRuleForAllLevels(consoleTarget);

            //应用Config
            LogManager.Configuration = config;

            //初始化logger
            _logger = LogManager.GetCurrentClassLogger();
        }
    }

    /* 配置文件示例
     * <?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

  <targets>
    <target xsi:type="File" name="fileLog" fileName="logs/${shortdate}.log"
        layout="${longdate} ${level:uppercase=true} ${message} ${exception}" />
		<target xsi:type="ColoredConsole" name="consoleLog"
        layout="${longdate} ${level:uppercase=true} ${message} ${exception}" />
  </targets>
  
<rules>
  <logger name="*" minlevel="Info" writeTo="consoleLog" />
  <logger name="*" minlevel="Debug" writeTo="fileLog" />
</rules>
</nlog>
     */
}
