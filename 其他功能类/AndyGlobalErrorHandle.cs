using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using 核素识别仪.Utils;

namespace 核素识别仪.其他功能类
{
    /// <summary>
    /// 更新时间：2023年6月19日11:43:36
    /// </summary>
    public class AndyGlobalErrorHandle
    {
        /// <summary>
        /// tast线程内未捕获异常处理事件
        /// </summary>
        public void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            string type = "Task线程";

            //string msg = GetErrorMsg(ex, type);

            //SaveErrorMsg(msg);
            AndyLogger.Ins.Logger.Error(ex, $"全局异常处理_{type}");
        }

        /// <summary>
        /// 非UI线程未捕获异常处理事件
        /// </summary>
        public void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                string type = "非UI子线程";

                //string msg = GetErrorMsg(ex, type);

                //SaveErrorMsg(msg);
                AndyLogger.Ins.Logger.Error(ex, $"全局异常处理_{type}");
            }
        }

        /// <summary>
        /// UI线程未捕获异常处理事件
        /// </summary>
        public void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Exception ex = e.Exception.InnerException;
            if (ex == null)
                ex = e.Exception;

            string type = "UI主线程";

            //string msg = GetErrorMsg(ex, type);

            //SaveErrorMsg(msg);
            AndyLogger.Ins.Logger.Error(ex, $"全局异常处理_{type}");
        }

        /// <summary>
        /// 将一个ex的错误信息整理出来。
        /// </summary>
        /// <param name="ex">Exception对象</param>
        /// <param name="type">异常种类，自定义一个字符串</param>
        /// <returns>错误信息</returns>
        public string GetErrorMsg(Exception ex, string type)
        {
            if (ex == null)
                return "ex为null";

            StringBuilder sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("G"));
            sb.Append(" 全局异常处理_");
            sb.Append(type);
            sb.Append("\r\n");
            string[] strs = ex.StackTrace.Split(new char[] { '\r', '\n' });

            sb.AppendLine(String.Format("{0}\n{1}\n\n", ex.Message, strs[0]));
            return sb.ToString();
        }

        /// <summary>
        /// 保存信息到ErrorLog.el文件
        /// </summary>
        public void SaveErrorMsg(string msg)
        {

            Console.WriteLine(msg);
            //try
            //{
            //    Console.ReadKey();
            //}
            //catch (Exception)
            //{
            //}

            string fileName = "ErrorLog.el";
            string filePath = Directory.GetCurrentDirectory() + "\\Resources\\" + fileName;
            try
            {
                File.AppendAllText(filePath, msg);
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
        }

        #region 使用示例

        //在App.xaml.cs中，添加如下代码：

        //AndyGlobalErrorHandle andyGEH = new AndyGlobalErrorHandle();

        ///// <summary>
        ///// 应用开启时触发的事件，这里配置好异常处理事件。
        ///// </summary>
        //private void Application_Startup(object sender, StartupEventArgs e)
        //{
        //    //UI界面的异常处理
        //    this.DispatcherUnhandledException += andyGEH.App_DispatcherUnhandledException;

        //    //非UI线程
        //    AppDomain.CurrentDomain.UnhandledException += andyGEH.CurrentDomain_UnhandledException;

        //    //Task线程内未捕获异常处理事件
        //    TaskScheduler.UnobservedTaskException += andyGEH.TaskScheduler_UnobservedTaskException; ;

        //}

        //然后在App.xaml中，设置App的Startup事件的侦听器为这个方法
        //若App已经重写了OnStartup方法，则直接将上面这部分初始化放到OnStartup方法中即可。

        #endregion
    }
}
