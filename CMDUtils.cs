using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Android_ADB_Tool
{
    class CMDUtils
    {
        private Process p;

        public CMDUtils()
        {
            p = new Process();
        }


        public StreamReader RunCmd(string strCMD)
        {
            try
            {
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;//是否使用操作系统shell启动
                p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
                p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
                p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
                p.StartInfo.CreateNoWindow = true;//不显示程序窗口
                p.Start();

                p.StandardInput.WriteLine(strCMD + "&exit");

                p.StandardInput.AutoFlush = true;

                //string output = p.StandardOutput.ReadToEnd();
                StreamReader sr = p.StandardOutput;
                p.WaitForExit();
                p.Close();
                
                return sr;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n跟踪;" + ex.StackTrace);
            }
            return null;

        }

        public ResultInfo RunCmd2(string strCMD)
        {

            ResultInfo resultInfo = new ResultInfo();
            try
            {
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;//是否使用操作系统shell启动
                p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
                p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
                p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
                p.StartInfo.CreateNoWindow = true;//不显示程序窗口

                p.Start();

                p.StandardInput.WriteLine(strCMD + "&exit");
                p.StandardInput.AutoFlush = true;
                string s2 = p.StandardOutput.ReadLine();
                string s3 = p.StandardOutput.ReadToEnd();
                Console.WriteLine("DATA2:" + s2);
                Console.WriteLine("DATA3:" + s3);

                p.WaitForExit();
                p.Close();

                return resultInfo;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n跟踪;" + ex.StackTrace);
            }
            return null;

        }
    }

}
