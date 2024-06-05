using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace 核素识别仪.其他功能类
{
    class AndyPlaySound
    {
        SoundPlayer soundPlayer = new SoundPlayer();

        /// <summary>
        /// 播放声音方法
        /// </summary>
        /// <param name="soundFileName">在exe目录下的Sounds文件夹下放音频文件，这里输入音频文件的文件名，带后缀</param>
        public void AlarmPlayStart(string soundFileName)
        {
            string filePath = System.IO.Directory.GetCurrentDirectory() + "\\Resources\\Sounds\\" + soundFileName;
            soundPlayer.SoundLocation = filePath;
            soundPlayer.PlayLooping();
        }
        public void AlarmPlayStop()
        {
            soundPlayer.Stop();
        }
    }
}
