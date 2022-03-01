using Android_ADB_Tool.Entity;
using Android_ADB_Tool.Utils;
using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace Android_ADB_Tool
{
    public partial class Form1 : Form
    {
        private const int MAX_CLOSE_TIME = 300;  //Xs若无操作则关闭adb连接
        private int adbCloseTimer = 0;

        private CMDUtils cmdUtils;
        private StreamReader sr = null;
        private ParkingInfo currentParkingInfo = null;
        private PortInfo currentPortInfo = null;

        public Form1()
        {
            InitializeComponent();
            cmdUtils = new CMDUtils();
            initList();
        }

        /**
         * 获取历史IP
         **/
        private void initList()

        {
            if (File.Exists(Util.PATH))
            {
                StreamReader sr = new StreamReader(Util.PATH, true);
                while (sr.Peek() > 0)
                {
                    comboBox2.Items.Add(sr.ReadLine());
                }
                sr.Close();
            }
            comboBox3.Items.Add("无人值守机器人App");
            comboBox3.Items.Add("端到云终端App");
            comboBox3.Items.Add("车位显示屏App");
            comboBox3.Items.Add("自助寻车机App");
            comboBox3.SelectedIndex = 0;
            comboBox4.Items.Add("端到云终端文件目录（/sdcard/.../files）");
            comboBox4.Items.Add("自助寻车机文件目录（/storage/sdcard/MapData）");
            comboBox4.SelectedIndex = 0;

        }

        private void setIPList(string ip)
        {
            if (comboBox2.Items.IndexOf(ip) == -1)
            {
                //ip不存在
                comboBox2.Items.Add(ip);
                if (!File.Exists(Util.PATH))
                {
                    File.Create(Util.PATH).Close();
                }
                StreamWriter sw = new StreamWriter(Util.PATH, true);
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
            Util.QueryDevices(cmdUtils, comboBox2, null, comboBox1);
            
        }



        /**
         * IP连接/断开
         */ 
        private void button1_Click(object sender, EventArgs e)
        {
            adbCloseTimer = 0;
            if (radioButton1.Checked)
            {
                Util.ConnectADB(cmdUtils, button1, comboBox2, null, timer2);
            }
            else
            {
                button1.Enabled = false;
                if (comboBox1.Items.Count != 0)
                {
                    button1.Text = "断开";
                    button1.BackColor = Color.Red;
                }
                else
                {
                    MessageBox.Show("未发现USB设备！", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                button1.Enabled = true;
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
            if (button1.Text.Equals("连接"))
            {
                MessageBox.Show("设备未连接！", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
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
                string appid = "";
                if (comboBox3.SelectedIndex == 0)
                {
                    appid = "com.ajb.smartparking.test";
                }else if (comboBox3.SelectedIndex == 1)
                {
                    appid = "com.ajb.smartparking.test";
                }
                else if (comboBox3.SelectedIndex == 2)
                {
                    appid = "com.ajb.guidescreen";
                }
                else if (comboBox3.SelectedIndex == 3)
                {
                    appid = "com.example.anjubao_reverseforcar";
                }
                sr = cmdUtils.RunCmd("adb -s " + getDevice() + " uninstall " + appid);
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

        /**
         * 选择文件
         **/
        private void button5_Click(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "资源文件|*.mp4;*.png;*.jpg|配置文件|*.properties|所有文件|*.*";
                string filePath = textBox3.Text;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;
                }
                textBox3.Text = filePath;
            
            }else if (radioButton4.Checked)
            {
                FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
                folderBrowserDialog.Description = "请选择配置文件所在文件夹";
                string filePath = textBox3.Text;
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = folderBrowserDialog.SelectedPath;
                }
                textBox3.Text = filePath;
            }

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
            string path1 = "";
            string path2 = "";
            if (comboBox4.SelectedIndex == 0)
            {
                path1 = "/sdcard/Android/data/com.ajb.smartparking.test/files";
            }
            else if (comboBox4.SelectedIndex == 1)
            {
                path1 = "/storage/sdcard/MapData";

            }
            if (radioButton3.Checked)
            {
                path2 = textBox3.Text;
            }
            else if (radioButton4.Checked)
            {
                path2 = textBox3.Text + "\\.";
            }
            sr = cmdUtils.RunCmd("adb -s " + getDevice() + " push " + path2 + " " + path1);
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

        public static bool IPCheck(string ip)
        {
            if (ip == null || ip == "")
                return false;
            return Regex.IsMatch(ip, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$");
        }

        /**
         * 通用选项页面
         */
        private void label_menu_general_Click(object sender, EventArgs e)
        {
            label_menu_config1.Visible = false;
            panel_config.Visible = false;
            label_menu_general1.Visible = true;
            panel_general.Visible = true;
        }

        /**
         * 配置选项页面
         */
        private void label_menu_config_Click(object sender, EventArgs e)
        {
            label_menu_general1.Visible = false;
            panel_general.Visible = false;
            label_menu_config1.Visible = true;
            panel_config.Visible = true;
        }

        /** 查询车场信息 */
        private void button_query_parking_Click(object sender, EventArgs e)
        {
            clearConfigUI();
            if (tb_parking_id.Text.Length < 11)
            {
                MessageBox.Show("请输入完整11位车场编号", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                button_query_parking.Text = "查询中..";
                if (backgroundWorker1.IsBusy)
                {
                    return;
                }
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            Hashtable ht = new Hashtable();
            ht.Add("parkCode", tb_parking_id.Text);
            e.Result = HttpUtils.QueryParking(ht);

        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            ResultInfo<ParkingInfo> resultInfo = e.Result as ResultInfo<ParkingInfo>;
            Console.WriteLine("查询结果：" + jss.Serialize(resultInfo));
            button_query_parking.Text = "查询";
            if (resultInfo.result == 0)
            {
                currentParkingInfo = resultInfo.data;
                label_ltdCode.Text = currentParkingInfo.ltdCode;
                label_parkName.Text = currentParkingInfo.parkName;
                comboBox_portName.Enabled = true;
                comboBox_portName.Items.Clear();
                foreach (PortInfo portInfo in currentParkingInfo.ports)
                {
                    comboBox_portName.Items.Add(portInfo.portName);
                }
                if (comboBox_portName.Items.Count != 0)
                {
                    comboBox_portName.SelectedIndex = 0;
                }

            }
            else
            {
                MessageBox.Show("请求失败：" + resultInfo.message, "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /** 选择通道 */
        private void comboBox_portName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (currentParkingInfo != null)
            {
                currentPortInfo = currentParkingInfo.ports[comboBox_portName.SelectedIndex];
                if (currentPortInfo != null)
                {
                    tabControl2.SelectedIndex = 0;
                    label_portId.Text = currentPortInfo.portId;
                    label_portTypeName.Text = currentPortInfo.portTypeName;
                    if (currentPortInfo.deviceCode == null || currentPortInfo.deviceCode.Length == 0)
                    {
                        label_deviceCode.Text = "未绑定设备";
                        label_deviceCode.ForeColor = Color.Red;
                        tb_deviceIp.Text = "192.168.9.101";
                        bt_device_bind.Visible = true;
                    }
                    else
                    {
                        label_deviceCode.Text = currentPortInfo.deviceCode;
                        label_deviceCode.ForeColor = Color.Black;
                        tb_deviceIp.Text = currentPortInfo.portIp;
                        bt_device_bind.Visible = false;
                    }
                    label_cameraIp.Text = currentPortInfo.cameraIp;
                    label_cameraIp2.Text = Util.valid(currentPortInfo.cameraIp2)? currentPortInfo.cameraIp2 : "--";
                    label_portIp.Text = currentPortInfo.portIp;
                    tb_deviceIp.Enabled = true;
                    radioButton5.Enabled = true;
                    bt_device_connect.Enabled = true;
                    label_portGatway.Text = currentPortInfo.portGateway;
                    label_portDns.Text = currentPortInfo.portDns;
                    label_deviceType.Text = currentPortInfo.deviceType;

                    if (Util.valid(currentPortInfo.robotIp))
                    {
                        tabPage_robot.Tag = false;
                        label_robotIp.Text = currentPortInfo.robotIp;
                        tb_robotIp.Enabled = true;
                        radioButton7.Enabled = true;
                        bt_robot_connect.Enabled = true;
                        label_robotGateway.Text = currentPortInfo.robotGateway;
                        label_robotDns.Text = currentPortInfo.robotDns;
                        label_robotType.Text = currentPortInfo.robotType;
                        label_portIp2.Text = currentPortInfo.portIp;
                    }
                    else
                    {
                        tabPage_robot.Tag = true; //标识没有机器人
                        tb_robotIp.Enabled = false;
                        radioButton7.Enabled = false;
                        bt_robot_connect.Enabled = false;
                        bt_robot_put.Enabled = false;
                    }
                }
                else
                {
                    Console.WriteLine("错误：currentPortInfo == null");
                }
            }
            else
            {
                Console.WriteLine("错误：currentParkingInfo == null");
            }
        }

        /**
         * 清除配置界面
         **/
        private void clearConfigUI()
        {
            label_ltdCode.Text = "--";
            label_parkName.Text = "--";
            comboBox_portName.Enabled = false;
            comboBox_portName.Items.Clear();

            tabControl2.SelectedIndex = 0;
            label_portId.Text = "--";
            label_portTypeName.Text = "--";
            label_deviceCode.Text = "--";
            label_deviceCode.ForeColor = Color.Black;
            tb_deviceIp.Text = "---";
            bt_device_bind.Visible = false;
            label_cameraIp.Text = "--";
            label_cameraIp2.Text = "--";
            label_portIp.Text = "--";
            tb_deviceIp.Text = "";
            tb_deviceIp.Enabled = false;
            radioButton5.Enabled = false;
            bt_device_connect.Enabled = false;
            label_portGatway.Text = "--";
            label_portDns.Text = "--";
            label_deviceType.Text = "--";

            tabPage_robot.Tag = false;
            label_robotIp.Enabled = false;
            radioButton7.Enabled = false;
            bt_robot_connect.Enabled = false;
            bt_robot_put.Enabled = false;
            tb_robotIp.Text = "192.168.9.102";
            label_robotGateway.Text = "--";
            label_robotDns.Text = "--";
            label_robotType.Text = "--";
            label_portIp2.Text = "--";

        }

        /** 连接设备*/
        private void bt_device_connect_Click(object sender, EventArgs e)
        {
            if (radioButton6.Checked)
            {
                adbCloseTimer = 0;
                int result = Util.ConnectADB(cmdUtils, bt_device_connect, null, tb_deviceIp, timer2);
                if (result == 0)
                {
                    //连接成功
                    tb_parking_id.Enabled = false;
                    button_query_parking.Enabled = false;
                    comboBox_portName.Enabled = false;
                    radioButton5.Enabled = false;
                    bt_device_put.Enabled = true;

                    Console.WriteLine("读取网络配置文件");
                    tb_deviceCode.Text = "";
                    Boolean isSuccess = false;
                    string line = "";
                    string netConfigPath = "netConfig.properties";
                    sr = cmdUtils.RunCmd("adb -s " + getDeviceId() + " pull sdcard/A3PlusEnd/netConfig.properties " + netConfigPath);
                    while ((line = sr.ReadLine()) != null)
                    {
                        Console.WriteLine(line);
                        if (line.Contains("1 file pulled."))
                        {
                            isSuccess = true;
                            PropertyOper po = new PropertyOper(netConfigPath);
                            tb_deviceCode.Text = po["deviceCode"].ToString();
                            break;
                        }
                    }
                    if (isSuccess)
                    {
                        if (Util.valid(currentPortInfo.deviceCode))
                        {
                            if (tb_deviceCode.Text.Equals(currentPortInfo.deviceCode))
                            {
                                lb_device_bind_result.Text = "已绑定";
                                lb_device_bind_result.ForeColor = Color.Green;
                            }
                            else
                            {
                                lb_device_bind_result.Text = "通道已绑定其他设备";
                                lb_device_bind_result.ForeColor = Color.Red;
                            }

                        }
                        else
                        {
                            lb_device_bind_result.Text = "未绑定";
                            lb_device_bind_result.ForeColor = Color.Red;
                            bt_device_bind.Visible = true;
                            bt_device_bind.Enabled = true;
                        }

                    }
                    else
                    {
                        bt_device_put.Enabled = false;
                        MessageBox.Show("获取设备号失败! 请检查该设备\n是否为端到云控制机。", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        lb_device_bind_result.Text = "获取设备号失败";
                        lb_device_bind_result.ForeColor = Color.Red;

                    }
                }
                else
                {
                    if (bt_robot_connect.Text.Equals("连接"))
                    {
                        tb_parking_id.Enabled = true;
                        button_query_parking.Enabled = true;
                        comboBox_portName.Enabled = true;
                    }
                    tb_deviceCode.Text = "";
                    lb_device_bind_result.Text = "---";
                    bt_device_put.Enabled = false;
                    bt_device_bind.Visible = false;
                    radioButton5.Enabled = true;

                }
            }
            else
            {
                //通过usb连接
                if (bt_device_connect.Text.Equals("连接"))
                {
                    bt_device_connect.Text = "断开";
                    bt_device_connect.BackColor = Color.Red;
                    tb_parking_id.Enabled = false;
                    button_query_parking.Enabled = false;
                    comboBox_portName.Enabled = false;
                    radioButton5.Enabled = false;
                    bt_device_put.Enabled = true;
                }
                else
                {
                    if (bt_robot_connect.Text.Equals("连接"))
                    {
                        tb_parking_id.Enabled = true;
                        button_query_parking.Enabled = true;
                        comboBox_portName.Enabled = true;
                    }
                    tb_deviceCode.Text = "";
                    lb_device_bind_result.Text = "---";
                    bt_device_put.Enabled = false;
                    bt_device_bind.Visible = false;
                    radioButton5.Enabled = true;

                }

            }
        }

        /**
         * 获取已连接的端到云设备
         **/
        private string getDeviceId()
        {
            if (radioButton6.Checked)
            {
                return tb_deviceIp.Text;
            }
            else
            {
                return cb_device_usb.SelectedItem.ToString();
            }
        }

        /**
         * 获取已连接的机器人设备
         **/
        private string getRobotId()
        {
            if (radioButton8.Checked)
            {
                return tb_robotIp.Text;
            }
            else
            {
                return cb_robot_usb.SelectedItem.ToString();
            }
        }

        /** 绑定设备*/
        private void bt_device_bind_Click(object sender, EventArgs e)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            Hashtable ht = new Hashtable();
            ht.Add("parkCode", tb_parking_id.Text);
            ht.Add("portId", currentPortInfo.portId);
            ht.Add("deviceCode", tb_deviceCode.Text);
            ProcessMsgBox processMsgBox = new ProcessMsgBox();
            processMsgBox.Show("正在绑定，请稍后...");
            ResultInfo<string> resultInfo = HttpUtils.BindPort(ht);
            Console.WriteLine("绑定结果：" + jss.Serialize(resultInfo));
            processMsgBox.Close();
            if (resultInfo.result == 0)
            {
                MessageBox.Show("绑定成功", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                lb_device_bind_result.Text = "已绑定";
                lb_device_bind_result.ForeColor = Color.Green;
                bt_device_bind.Visible = false;
                currentPortInfo.deviceCode = tb_deviceCode.Text;
                label_deviceCode.Text = currentPortInfo.deviceCode;
                label_deviceCode.ForeColor = Color.Black;
            }
            else
            {
                MessageBox.Show("请求失败：" + resultInfo.message, "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /** 配置设备*/
        private void bt_device_put_Click(object sender, EventArgs e)
        {

            if (MessageBox.Show("配置网络将重启android系统，是否配置？", "消息提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ProcessMsgBox processMsgBox = new ProcessMsgBox();
                processMsgBox.Show("正在配置，请稍后...");
                bt_device_put.Enabled = false;
                Boolean isSuccess = false;
                string line = "";
                sr = cmdUtils.RunCmd("adb -s " + getDeviceId() + " shell am broadcast -a com.ajb.a3plus.netconfig --es ip " + label_portIp.Text
                    + " --es gateway " + label_portGatway.Text + " --es dns " + label_portDns.Text);
                while ((line = sr.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                    if (line.Contains("act=com.ajb.a3plus.netconfig"))
                    {
                        bt_device_connect.Text = "连接";
                        bt_device_connect.BackColor = Color.Green;
                        tb_deviceIp.Enabled = true;
                        cb_device_usb.Enabled = true;
                        radioButton5.Enabled = true;
                        tb_deviceCode.Text = "";
                        lb_device_bind_result.Text = "---";

                        isSuccess = true;
                        processMsgBox.Close();
                        MessageBox.Show("配置成功，请等待终端启动", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                }
                sr.Close();
                if (!isSuccess)
                {
                    processMsgBox.Close();
                    MessageBox.Show("配置失败", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            bt_device_put.Enabled = true;

        }

        /**
         * 切换成IP连接
         */
        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton6.Checked)
            {
                return;
            }
            tb_deviceIp.Enabled = true;
            cb_device_usb.Enabled = false;

        }

        /**
         * 切换成USB连接
         */
        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton5.Checked)
            {
                return;
            }
            Util.QueryDevices(cmdUtils, null, tb_deviceIp, cb_device_usb);

        }

        private void tabControl2_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPageIndex == 1 && Convert.ToBoolean(e.TabPage.Tag))
            {
                Console.WriteLine("tabControl2_Selecting");
                MessageBox.Show("该通道未配置机器人", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
            }
        }

        /**
         * 连接机器人
         */
        private void bt_robot_connect_Click(object sender, EventArgs e)
        {
            if (radioButton8.Checked)
            {
                adbCloseTimer = 0;
                int result = Util.ConnectADB(cmdUtils, bt_robot_connect, null, tb_robotIp, timer2);
                if (result == 0)
                {
                    //连接成功
                    tb_parking_id.Enabled = false;
                    button_query_parking.Enabled = false;
                    comboBox_portName.Enabled = false;
                    radioButton7.Enabled = false;
                    bt_robot_put.Enabled = true;

                }
                else
                {
                    if (bt_device_connect.Text.Equals("连接"))
                    {
                        tb_parking_id.Enabled = true;
                        button_query_parking.Enabled = true;
                        comboBox_portName.Enabled = true;
                    }
                    bt_robot_put.Enabled = false;
                    radioButton7.Enabled = true;

                }

            }
            else
            {
                //通过usb连接
                if (bt_robot_connect.Text.Equals("连接"))
                {
                    bt_robot_connect.Text = "断开";
                    bt_robot_connect.BackColor = Color.Red;
                    tb_parking_id.Enabled = false;
                    button_query_parking.Enabled = false;
                    comboBox_portName.Enabled = false;
                    radioButton7.Enabled = false;
                    bt_robot_put.Enabled = true;
                }
                else
                {
                    if (bt_device_connect.Text.Equals("连接"))
                    {
                        tb_parking_id.Enabled = true;
                        button_query_parking.Enabled = true;
                        comboBox_portName.Enabled = true;

                    }
                    bt_robot_put.Enabled = false;
                    radioButton7.Enabled = true;

                }

            }
        }

        /**
         * 配置机器人
         */
        private void bt_robot_put_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("配置网络将重启机器人，是否配置？", "消息提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ProcessMsgBox processMsgBox = new ProcessMsgBox();
                processMsgBox.Show("正在配置，请稍后...");
                bt_robot_put.Enabled = false;
                Boolean isSuccess = false;
                string line = "";
                int productType = 1;
                switch(label_robotType.Text)
                {
                    case "AJB-NPR-18A":
                        productType = 0;
                        break;
                    case "AJB-NPR-19A":
                        productType = 1;
                        break;
                    case "AJB-NPR-20A":
                        productType = 2;
                        break;
                }
                sr = cmdUtils.RunCmd("adb -s " + getRobotId() + " shell am broadcast -a com.ajb.robot.netconfig --es ip " + label_robotIp.Text
                    + " --es gateway " + label_robotGateway.Text + " --es dns " + label_robotDns.Text + " --ei talkingType " + 1
                    + " --ei productType " + productType + " --es serverIp " + label_portIp2.Text);
                while ((line = sr.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                    if (line.Contains("act=com.ajb.robot.netconfig"))
                    {
                        bt_robot_connect.Text = "连接";
                        bt_robot_connect.BackColor = Color.Green;
                        tb_robotIp.Enabled = true;
                        cb_robot_usb.Enabled = true;
                        radioButton7.Enabled = true;

                        isSuccess = true;
                        processMsgBox.Close();
                        MessageBox.Show("配置成功，请等待终端启动", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }
                }
                sr.Close();
                if (!isSuccess)
                {
                    processMsgBox.Close();
                    MessageBox.Show("配置失败", "消息提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            bt_robot_put.Enabled = true;

        }

        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton8.Checked)
            {
                return;
            }
            tb_robotIp.Enabled = true;
            cb_robot_usb.Enabled = false;

        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton7.Checked)
            {
                return;
            }
            Util.QueryDevices(cmdUtils, null, tb_robotIp, cb_robot_usb);
        }

        private void tb_parking_id_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button_query_parking.PerformClick();
            }
        }
    }
}
