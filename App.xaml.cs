using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using 核素识别仪.Parameters;
using 核素识别仪.其他功能类;

namespace 核素识别仪
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        AndyGlobalErrorHandle andyGEH = new AndyGlobalErrorHandle();
        protected override void OnStartup(StartupEventArgs e)
        {
            Console.WriteLine(DateTime.Now.ToString("G") + ": " + "正在启动程序，请稍候...");

            Console.WriteLine(DateTime.Now.ToString("G") + ": " + "Startup");
            FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty = false;

            //UI界面的异常处理
            this.DispatcherUnhandledException += andyGEH.App_DispatcherUnhandledException;

            //非UI线程
            AppDomain.CurrentDomain.UnhandledException += andyGEH.CurrentDomain_UnhandledException;

            //Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += andyGEH.TaskScheduler_UnobservedTaskException;

            //新版参数系统，读参数
            ParaDataManager.Instance.ReadAllParas();

            //语言选择
            if (ParaDataManager.Instance.CommonParas.Language == Models.LanguageEnum.Chinese)
                核素识别仪.Properties.Resources.Culture = new CultureInfo("zh-CN");
            else
                核素识别仪.Properties.Resources.Culture = new CultureInfo("en-US");
        }
    }

}
