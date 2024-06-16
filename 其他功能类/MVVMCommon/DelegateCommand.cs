using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace 核素识别仪.其他功能类.MVVMCommon
{
    public class DelegateCommand : ICommand
    {
        /// <summary>
        /// 命令能否执行状态改变时触发的时间
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// 命令能否执行
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool CanExecute(object parameter)
        {
            if (this.CanExecuteFunc == null)
            {
                return true;
            }
            return this.CanExecuteFunc(parameter);
        }

        /// <summary>
        /// 命令执行的逻辑
        /// </summary>
        /// <param name="parameter"></param>
        public void Execute(object parameter)
        {
            if (this.ExecuteAction == null)
            {
                return;
            }
            this.ExecuteAction(parameter);//命令去执行这个委托所指向的方法
        }

        /// <summary>
        /// 指向一个方法，用于命令的执行
        /// </summary>
        public Action<object> ExecuteAction { get; set; }

        /// <summary>
        /// 指向一个方法，用于判断命令是否可以执行
        /// </summary>
        public Func<object, bool> CanExecuteFunc { get; set; }
    }
}
