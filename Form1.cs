using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Android_ADB_Tool
{
    public partial class Form1 : Form
    {
        private const int MAX_CLOSE_TIME = 300;  //Xs若无操作则关闭adb连接
        private const string PATH = @"iplist.ini";
        private int adbCloseTimer = 0;

        private CMDUtils cmdUtils;
        private StreamReader sr = null;

        public Form1()
        {
            InitializeComponent();
            cmdUtils = new CMDUtils();
            getIPList();
        }

        /**
         * 获取历史IP
         **/
        private void getIPList()
        {
            if (File.Exists(PATH))
            {
                StreamReader sr = new StreamReader(PATH, true);
                while (sr.Peek() > 0)
                {
                    comboBox2.Items.Add(sr.ReadLine());
                }
                sr.Close();
            }
        }

        private void setIPList(string ip)
        {
            if (comboBox2.Items.IndexOf(ip) == -1)
            {
                //ip不存在
                comboBox2.Items.Add(ip);
                if (!File.Exists(PATH))
                {
                    File.Create(PATH).Close();
                }
                StreamWriter sw = new StreamWriter(PATH, true);
                sw.WriteLine(ip);
                sw.Close();
            }
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            sr = cmdUtils.RunCmd("adb disconnect");
            timer1.Enabled = false;
            timer2.Enabled = false;
        }

        /**
         * IP连接 
         */
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton1.Checked)
            {
                return;
            }
            Console.WriteLine("radioButton1_CheckedChanged");
            button1.Enabled = true;
            comboBox2.Enabled = true;
            comboBox1.Enabled = false;
        }

        /**
         * USB连接 
         */
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if(!radioButton2.Checked)
            {
                return;
            }

            Console.WriteLine("radioButton2_CheckedChanged");
            button1.Enabled = false;
            comboBox2.Enabled = false;
            comboBox1.Enabled = true;
            comboBox1.Items.Clear();

            ProcessMsgBox processMsgBox = new ProcessMsgBox();
            processMsgBox.Show("正在查找，请稍后...");
            Boolean isSuccess = false;
            string line = "";
            sr = cmdUtils.RunCmd("adb devices");
            while ((line = sr.ReadLine()) != null)
            {
                Console.WriteLine(line);
                if (line.Contains("List of devices attached"))
                {
                    isSuccess = true;
                }
                if (line.Contains("device") && !line.Contains("devices") && !line.Contains(":5555"))
                {
                    comboBox1.Items.Add(line.Substring(0, line.IndexOf("device")).Trim());
                }
            }
            processMsgBox.Close();
            if (isSuccess)
            {
                if (comboBox1.Items.Count > 0)
                {
                    comboBox1.SelectedIndex = 0;
                }
            }
            else
            {
                MessageBox.Show("查找失败", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        /**
         * IP连接/断开
         */ 
        private void button1_Click(object sender, EventArgs e)
        {
            adbCloseTimer = 0;
            string line = "";
            if (button1.Text.Equals("连接"))
            {
                Console.WriteLine("连接");
                ProcessMsgBox processMsgBox = new ProcessMsgBox();
                processMsgBox.Show("正在连接，请稍后...");
                button1.Enabled = false;
                Boolean isSuccess = false;
                sr = cmdUtils.RunCmd("adb connect " + comboBox2.Text + ":5555");
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("connected"))
                    {
                        button1.Text = "断开";
                        button1.BackColor = Color.Red;
                        comboBox2.Enabled = false;
                        timer2.Interval = 1000;
                        timer2.Enabled = true;
                        isSuccess = true;
                        setIPList(comboBox2.Text);
                    }
                }
                processMsgBox.Close();
                button1.Enabled = true;
                if (!isSuccess)
                {
                    MessageBox.Show("连接失败", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (button1.Text.Equals("断开"))
            {
                Console.WriteLine("断开");
                sr = cmdUtils.RunCmd("adb disconnect");
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("disconnected"))
                    {
                        button1.Text = "连接";
                        button1.BackColor = Color.Green;
                        comboBox2.Enabled = true;
                        comboBox2.Enabled = true;
                        timer2.Enabled = false;
                    }
                }

            }
        }

        /**
         * 长时间不操作应用，则关闭连接 
         **/
        private void timer2_Tick(object sender, EventArgs e)
        {
            adbCloseTimer += 1;
            if (adbCloseTimer > MAX_CLOSE_TIME)
            {
                string line = "";
                sr = cmdUtils.RunCmd("adb disconnect");
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("disconnected"))
                    {
                        button1.Text = "连接";
                        button1.BackColor = Color.Green;
                        if (radioButton1.Checked)
                        {
                            comboBox2.Enabled = true;

                        }
                    }
                }
                timer2.Enabled = false;
            }
        }

        /**
         * 检查窗口是否关闭
         **/
        private Boolean isConnected()
        {
            adbCloseTimer = 0;
            if (radioButton1.Checked)
            {
                if (button1.Text.Equals("连接"))
                {
                    MessageBox.Show("设备未连接！", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            else
            {
                if (comboBox1.Items.Count == 0)
                {
                    MessageBox.Show("设备未连接！", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            return true;
        }

        private String getDevice()
        {
            if (radioButton1.Checked)
            {
                return comboBox2.Text;
            }
            else
            {
                return comboBox1.SelectedItem.ToString();
            }
        }

        /**
         * 重启
         **/
        private void button9_Click(object sender, EventArgs e)
        {

            Console.WriteLine("重启");
            if (!isConnected())
            {
                return;
            }
            if (MessageBox.Show("是否重启？", "消息提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ProcessMsgBox processMsgBox = new ProcessMsgBox();
                processMsgBox.Show("正在重启，请稍后...");
                button9.Enabled = false;
                Boolean isSuccess = false;
                string line = "";
                sr = cmdUtils.RunCmd("adb -s " + getDevice() + " reboot");
                int count = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    count++;
                    Console.WriteLine(line);
                }
                processMsgBox.Close();
                if (count <= 4)
                {
                    button1.Text = "连接";
                    button1.BackColor = Color.Green;
                    comboBox2.Enabled = true;
                    isSuccess = true;
                }
                if (!isSuccess)
                {
                    MessageBox.Show("重启失败", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
                button9.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "APK文件|*.apk";
            string filePath = textBox2.Text;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = openFileDialog.FileName;
            }
            textBox2.Text = filePath;
        }

        /**
         *  安装APK 
         **/
        private void button3_Click(object sender, EventArgs e)
        {

            Console.WriteLine("安装");
            if (!isConnected())
            {
                return;
            }
            ProcessMsgBox processMsgBox = new ProcessMsgBox();
            processMsgBox.Show("APK正在安装中，请稍后...");
            button3.Enabled = false;
            Boolean isSuccess = false;
            string line = "";
            sr = cmdUtils.RunCmd("adb -s " + getDevice() + " install -r -t " + textBox2.Text);
            while ((line = sr.ReadLine()) != null)
            {
                Console.WriteLine(line);
                if (line.Contains("Success"))
                {
                    processMsgBox.Close();
                    MessageBox.Show("安装成功", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    button3.Enabled = true;
                    isSuccess = true;
                }
            }
            if (!isSuccess)
            {
                processMsgBox.Close();
                MessageBox.Show("安装失败", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            }
            button3.Enabled = true;
        }

        private void button10_Click(object sender, EventArgs e)
        {

            Console.WriteLine("安装");
            if (!isConnected())
            {
                return;
            }

            if (MessageBox.Show("注意：卸载应用之后不能重启终端,否则adb无法连接。是否卸载？", "消息提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ProcessMsgBox processMsgBox = new ProcessMsgBox();
                processMsgBox.Show("APK正在卸载中，请稍后...");
                button10.Enabled = false;
                Boolean isSuccess = false;
                string line = "";
                sr = cmdUtils.RunCmd("adb -s " + getDevice() + " uninstall " + textBox7.Text);
                while ((line = sr.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                    if (line.Contains("Success"))
                    {
                        processMsgBox.Close();
                        MessageBox.Show("卸载成功", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        button10.Enabled = true;
                        isSuccess = true;
                    }
                }
                if (!isSuccess)
                {
                    processMsgBox.Close();
                    MessageBox.Show("卸载失败", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
                button10.Enabled = true;
            }

        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "资源文件|*.mp4;*.png;*.jpg|配置文件|*.properties";
            string filePath = textBox3.Text;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = openFileDialog.FileName;
            }
            textBox3.Text = filePath;

        }

        /**
         *  写入文件 
         **/
        private void button4_Click(object sender, EventArgs e)
        {

            Console.WriteLine("写入");
            if (!isConnected())
            {
                return;
            }
            ProcessMsgBox processMsgBox = new ProcessMsgBox();
            processMsgBox.Show("文件正在写入中，请稍后...");
            button4.Enabled = false;
            Boolean isSuccess = false;
            string line = "";
            sr = cmdUtils.RunCmd("adb -s " + getDevice() + " push " + textBox3.Text + " " + textBox5.Text);
            while ((line = sr.ReadLine()) != null)
            {
                Console.WriteLine(line);
                if (line.Contains("pushed"))
                {
                    processMsgBox.Close();
                    MessageBox.Show("写入成功", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    button4.Enabled = true;
                    isSuccess = true;
                }
            }
            if (!isSuccess)
            {
                processMsgBox.Close();
                MessageBox.Show("写入失败", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
            button4.Enabled = true;
        }

        /**
         *  读取文件 
         **/
        private void button6_Click(object sender, EventArgs e)
        {

            Console.WriteLine("读取");
            if (!isConnected())
            {
                return;
            }
            if (textBox6.Text.Trim().Equals(""))
            {
                MessageBox.Show("存储路径错误", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            ProcessMsgBox processMsgBox = new ProcessMsgBox();
            processMsgBox.Show("文件正在读取中，请稍后...");
            button6.Enabled = false;
            Boolean isSuccess = false;
            string line = "";
            sr = cmdUtils.RunCmd("adb -s " + getDevice() + " pull " + textBox4.Text + " " + textBox6.Text);
            while ((line = sr.ReadLine()) != null)
            {
                Console.WriteLine(line);
                if (line.Contains("pulled"))
                {
                    processMsgBox.Close();
                    MessageBox.Show("读取成功", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    button6.Enabled = true;
                    isSuccess = true;
                }
            }
            if (!isSuccess)
            {
                processMsgBox.Close();
                MessageBox.Show("读取失败", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
            button6.Enabled = true;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择保存路径";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox6.Text = dialog.SelectedPath;
            }
        }

        /**
         * 截图
         **/
        private void button7_Click(object sender, EventArgs e)
        {

            Console.WriteLine("截图");
            if (!isConnected())
            {
                return;
            }
            button7.Enabled = false;
            label4.Text = "请稍后..";
            label4.ForeColor = Color.Gray;
            Boolean isSuccess = false;
            string line = "";
            string pathName = "screencap.png";
            sr = cmdUtils.RunCmd("adb -s " + getDevice() + " exec-out screencap -p > " + pathName);
            int count = 0;
            while ((line = sr.ReadLine()) != null)
            {
                count++;
                Console.WriteLine(line);
            }
            if (count <=4)
            {
                isSuccess = true;
                pictureBox1.LoadAsync(pathName);
                label4.Text = "成功";
                label4.ForeColor = Color.Green;
                button7.Enabled = true;

            }
            if (!isSuccess)
            {
                label4.Text = "失败";
                label4.ForeColor = Color.Red;

            }
            button7.Enabled = true;
        }

        /**
         * 图片按键事件
         **/
        private int clickTime = 0;   //按键时间
        private MouseEventArgs mouseDownE = null;
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            Console.WriteLine("MouseDown");
            clickTime = 0;
            mouseDownE = e;
            timer1.Interval = 20;
            timer1.Enabled = true;

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            clickTime += 20;
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            timer1.Enabled = false;
            Console.WriteLine("MouseUp " + clickTime);
            if (!isConnected())
            {
                return;
            }
            if (mouseDownE != null)
            {
                if (mouseDownE.X == e.X && mouseDownE.Y == e.Y && clickTime < 500)
                {
                    Console.WriteLine("短按" + clickTime);
                    P_MouseClick(sender, e);
                }
                else
                {
                    Console.WriteLine("长按" + clickTime);
                    P_MouseLongClick(sender, mouseDownE, e, clickTime);
                }
            }
            mouseDownE = null;
        }

        private void P_MouseClick(object sender, MouseEventArgs e)
        {

            if (pictureBox1.Image == null)
            {
                Console.WriteLine("无图片资源");
                return;
            }

            Console.WriteLine("Click " + pictureBox1.Image.Height + "  " + pictureBox1.Image.Width + "  " + e.X + "  " + e.Y);

            //图片原点坐标
            int origin_X = 0;
            int origin_Y = 0;
            if (pictureBox1.Image.Height >= pictureBox1.Image.Width)
            {
                //竖屏
                origin_X = (pictureBox1.Width - ((pictureBox1.Image.Width * pictureBox1.Height) / pictureBox1.Image.Height)) / 2;
            }
            else
            {
                //横屏
                origin_Y = (pictureBox1.Height - ((pictureBox1.Image.Height * pictureBox1.Width) / pictureBox1.Image.Width)) / 2;
            }

            if (e.X < origin_X
                || e.X > (pictureBox1.Width - origin_X)
                || e.Y < origin_Y
                || e.Y > (pictureBox1.Height - origin_Y))
            {
                Console.WriteLine("不在图片范围内");
                return;
            }
            int bit_x = (pictureBox1.Image.Width * (e.X - origin_X)) / (pictureBox1.Width - (origin_X * 2));
            int bit_y = (pictureBox1.Image.Height * (e.Y - origin_Y)) / (pictureBox1.Height - (origin_Y * 2));
            label5.Text = "点击: " + "(" + bit_x + "," + bit_y + ")";
            Console.WriteLine("像素点为" + bit_x + "  " + bit_y);
            
            Boolean isSuccess = false;
            string line = "";
            sr = cmdUtils.RunCmd("adb -s " + getDevice() + " shell input tap " + bit_x + " " + bit_y);
            int count = 0;
            while ((line = sr.ReadLine()) != null)
            {
                count++;
                Console.WriteLine(line);
            }
            if (count <= 4)
            {
                isSuccess = true;
            }
            if (!isSuccess)
            {
                MessageBox.Show("点击失败，请重试", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine("点击失败");
                return;
            }
            button7_Click(null, null);

        }

        private void P_MouseLongClick(object sender, MouseEventArgs e1, MouseEventArgs e2, int clickTime)
        {

            if (pictureBox1.Image == null)
            {
                Console.WriteLine("无图片资源");
                return;
            }

            Console.WriteLine("Click " + e1.X + "  " + e1.Y + "  " + e2.X + "  " + e2.Y);


            //图片原点坐标
            int origin_X = 0;
            int origin_Y = 0;
            if (pictureBox1.Image.Height >= pictureBox1.Image.Width)
            {
                //竖屏
                origin_X = (pictureBox1.Width - ((pictureBox1.Image.Width * pictureBox1.Height) / pictureBox1.Image.Height)) / 2;
            }
            else
            {
                //横屏
                origin_Y = (pictureBox1.Height - ((pictureBox1.Image.Height * pictureBox1.Width) / pictureBox1.Image.Width)) / 2;
            }

            if (e1.X < origin_X
                || e1.X > (pictureBox1.Width - origin_X)
                || e1.Y < origin_Y
                || e1.Y > (pictureBox1.Height - origin_Y)
                || e2.X < origin_X
                || e2.X > (pictureBox1.Width - origin_X)
                || e2.Y < origin_Y
                || e2.Y > (pictureBox1.Height - origin_Y))
            {
                Console.WriteLine("不在图片范围内");
                return;
            }
            int bit_x1 = (pictureBox1.Image.Width * (e1.X - origin_X)) / (pictureBox1.Width - (origin_X * 2));
            int bit_y1 = (pictureBox1.Image.Height * (e1.Y - origin_Y)) / (pictureBox1.Height - (origin_Y * 2));
            int bit_x2 = (pictureBox1.Image.Width * (e2.X - origin_X)) / (pictureBox1.Width - (origin_X * 2));
            int bit_y2 = (pictureBox1.Image.Height * (e2.Y - origin_Y)) / (pictureBox1.Height - (origin_Y * 2));
            label5.Text = "长按: " + "(" + bit_x2 + "," + bit_y2 + ")";
            Console.WriteLine("像素点为" + bit_x2 + "  " + bit_y2);
            
            Boolean isSuccess = false;
            string line = "";
            sr = cmdUtils.RunCmd("adb -s " + getDevice() + " shell input swipe " + bit_x1 + " " + bit_y1 + " " + bit_x2 + " " + bit_y2 + " " + clickTime);
            int count = 0;
            while ((line = sr.ReadLine()) != null)
            {
                count++;
                Console.WriteLine(line);
            }
            if (count <= 4)
            {
                isSuccess = true;
            }
            if (!isSuccess)
            {
                MessageBox.Show("点击失败，请重试", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine("点击失败");
                return;
            }
            button7_Click(null, null);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            Console.WriteLine("呼叫");
            if (!isConnected())
            {
                return;
            }
            button11.Enabled = false;
            label6.Text = "请稍后..";
            label6.ForeColor = Color.Gray;
            Boolean isSuccess = false;
            string line = "";
            sr = cmdUtils.RunCmd("adb -s " + getDevice() + " shell am broadcast -a com.seavo.ALARMa");
            while ((line = sr.ReadLine()) != null)
            {
                Console.WriteLine(line);
                if (line.Contains("Broadcast completed: result=0"))
                {
                    isSuccess = true;
                    label6.Text = "成功";
                    label6.ForeColor = Color.Green;
                    button11.Enabled = true;
                }
            }
            if (!isSuccess)
            {
                label6.Text = "失败";
                label6.ForeColor = Color.Red;

            }
            button11.Enabled = true;

        }

        private void textBox4_DoubleClick(object sender, EventArgs e)
        {
            Console.WriteLine("Double Click");
            if (textBox4.ReadOnly)
            {
                textBox4.Text = "sdcard/Android/data/com.ajb.smartparking.test/cache/logs/my-log-latest.html";
                textBox4.ReadOnly = false;
            }
            else
            {
                textBox4.Text = "sdcard/Android/data/com.ajb.smartparking.test/cache";
                textBox4.ReadOnly = true;
            }
        }

        /**
         * 端到云读取
         **/
        private void button12_Click(object sender, EventArgs e)
        {
            Console.WriteLine("读取网络配置文件");
            if (!isConnected())
            {
                return;
            }
            button12.Enabled = false;
            label15.Text = "请稍后..";
            label15.ForeColor = Color.Gray;
            textBox8.Text = "";
            textBox9.Text = "";
            textBox10.Text = "";
            textBox1.Text = "";
            Boolean isSuccess = false;
            string line = "";
            string netConfigPath = "netConfig.properties";
            sr = cmdUtils.RunCmd("adb -s " + getDevice() + " pull sdcard/A3PlusEnd/netConfig.properties " + netConfigPath);
            while ((line = sr.ReadLine()) != null)
            {
                Console.WriteLine(line);
                if (line.Contains("1 file pulled."))
                {
                    isSuccess = true;
                    label15.Text = "成功";
                    label15.ForeColor = Color.Green;
                    button11.Enabled = true;
                    PropertyOper po = new PropertyOper(netConfigPath);
                    textBox8.Text = po["ip"].ToString();
                    textBox9.Text = po["gateway"].ToString();
                    textBox10.Text = po["dns"].ToString();
                    textBox1.Text = po["deviceCode"].ToString();
                    break;
                }
            }
            if (!isSuccess)
            {
                label15.Text = "失败";
                label15.ForeColor = Color.Red;

            }
            button12.Enabled = true;

        }

        private void button13_Click(object sender, EventArgs e)
        {
            Console.WriteLine("写入网络配置文件");
            if (!isConnected())
            {
                return;
            }
            if (!IPCheck(textBox8.Text) || !IPCheck(textBox9.Text))
            {
                MessageBox.Show("请填入正确的IP地址", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show("配置网络将重启android系统，是否配置？", "消息提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ProcessMsgBox processMsgBox = new ProcessMsgBox();
                processMsgBox.Show("正在配置，请稍后...");
                button13.Enabled = false;
                Boolean isSuccess = false;
                string line = "";
                sr = cmdUtils.RunCmd("adb -s " + getDevice() + " shell am broadcast -a com.ajb.a3plus.netconfig --es ip " + textBox8.Text
                    + " --es gateway " + textBox9.Text + " --es dns " + textBox10.Text);
                while ((line = sr.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                    if (line.Contains("act=com.ajb.a3plus.netconfig"))
                    {
                        button1.Text = "连接";
                        button1.BackColor = Color.Green;
                        comboBox2.Enabled = true;
                        isSuccess = true;
                        button11.Enabled = true;
                        processMsgBox.Close();
                        MessageBox.Show("配置成功，请等待终端启动", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                }
                if (!isSuccess)
                {
                    processMsgBox.Close();
                    MessageBox.Show("配置失败", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
            }
            button13.Enabled = true;

        }

        public static bool IPCheck(string ip)
        {
            if (ip == null || ip == "")
                return false;
            return Regex.IsMatch(ip, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$");
        }
    }
}
