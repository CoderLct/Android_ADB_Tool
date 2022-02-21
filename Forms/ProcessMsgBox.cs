using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Android_ADB_Tool
{
    public partial class ProcessMsgBox : Form
    {
        public ProcessMsgBox()
        {
            InitializeComponent();
        }

        private void ProcessMsgBox_Load(object sender, EventArgs e)
        {

        }

        public void Show(String msgStr)
        {
            msg.Text = msgStr;
            Show();
        }
    }
}
