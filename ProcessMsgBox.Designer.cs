namespace Android_ADB_Tool
{
    partial class ProcessMsgBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.msg = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // msg
            // 
            this.msg.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.msg.Location = new System.Drawing.Point(0, 45);
            this.msg.Name = "msg";
            this.msg.Size = new System.Drawing.Size(315, 23);
            this.msg.TabIndex = 0;
            this.msg.Text = "正在加载，请稍后...";
            this.msg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ProcessMsgBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(315, 114);
            this.ControlBox = false;
            this.Controls.Add(this.msg);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ProcessMsgBox";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "处理进度";
            this.Load += new System.EventHandler(this.ProcessMsgBox_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label msg;
    }
}