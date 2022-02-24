using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Android_ADB_Tool.Utils
{
    class Util
    {
        public static string PATH = @"iplist.ini";

        public static Boolean valid(string value)
        {
            return value != null && value.Length != 0;
        }

        /**
         * 连接设备 0：连接成功，1：连接失败，2：断开成功，3：断开失败
         **/
        public static int connectADB(CMDUtils cmdUtils, 
            Button button, 
            ComboBox comboBox, 
            TextBox textBox, 
            Timer timer)
        {

            string line = "";
            Boolean isSuccess = false;
            StreamReader sr = null;
            if (button.Text.Equals("连接"))
            {
                Console.WriteLine("连接");
                ProcessMsgBox processMsgBox = new ProcessMsgBox();
                processMsgBox.Show("正在连接，请稍后...");
                button.Enabled = false;
                if (comboBox != null)
                {
                    sr = cmdUtils.RunCmd("adb connect " + comboBox.Text + ":5555");
                }else if (textBox != null)
                {
                    sr = cmdUtils.RunCmd("adb connect " + textBox.Text + ":5555");
                }
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("connected"))
                    {
                        button.Text = "断开";
                        button.BackColor = Color.Red;
                        if (comboBox != null)
                        {
                            comboBox.Enabled = false;
                        }
                        else if (textBox != null)
                        {
                            textBox.Enabled = false;
                        }
                        timer.Interval = 1000;
                        timer.Enabled = true;
                        isSuccess = true;
                        if (comboBox != null)
                        {
                            setIPList(comboBox);
                        }
                    }
                }
                sr.Close();
                processMsgBox.Close();
                button.Enabled = true;
                if (isSuccess)
                {
                    return 0;
                }
                else
                {
                    MessageBox.Show("连接失败", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 1;
                }
            }
            else
            {
                Console.WriteLine("断开");
                sr = cmdUtils.RunCmd("adb disconnect");
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("disconnected"))
                    {
                        button.Text = "连接";
                        button.BackColor = Color.Green;
                        if (comboBox != null)
                        {
                            comboBox.Enabled = true;
                        }
                        else if (textBox != null)
                        {
                            textBox.Enabled = true;
                        }
                        timer.Enabled = false;
                        isSuccess = true;
                    }
                }
                sr.Close();
                if (isSuccess)
                {
                    return 2;

                }
                else
                {
                    return 3;
                }

            }
        }


        public static void setIPList(ComboBox comboBox)
        {
            string ip = comboBox.Text;
            if (comboBox.Items.IndexOf(ip) == -1)
            {
                //ip不存在
                comboBox.Items.Add(ip);
                if (!File.Exists(PATH))
                {
                    File.Create(PATH).Close();
                }
                StreamWriter sw = new StreamWriter(PATH, true);
                sw.WriteLine(ip);
                sw.Close();
            }
        }
    }
}
