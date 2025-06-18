using System;
using System.Drawing;
using System.Windows.Forms;

namespace Test0524
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.portButton = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.cboCheck = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cboStopB = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cboDataB = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cboBotte = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cboCom = new System.Windows.Forms.ComboBox();
            this.autoSendTimer = new System.Windows.Forms.Timer(this.components);
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtRec = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btnClear = new System.Windows.Forms.Button();
            this.rdoHEX = new System.Windows.Forms.RadioButton();
            this.rdoASCII = new System.Windows.Forms.RadioButton();
            this.gbxSend = new System.Windows.Forms.GroupBox();
            this.transButt = new System.Windows.Forms.Button();
            this.txtSend = new System.Windows.Forms.TextBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.lblReceived = new System.Windows.Forms.Label();
            this.chkAuto = new System.Windows.Forms.CheckBox();
            this.btnOpenFile = new System.Windows.Forms.Button();
            this.lblSend = new System.Windows.Forms.Label();
            this.autoTransText = new System.Windows.Forms.TextBox();
            this.timerSend = new System.Windows.Forms.Timer(this.components);
            this.dlgOpenFile = new System.Windows.Forms.OpenFileDialog();
            this.timSum = new System.Windows.Forms.Timer(this.components);
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.btnSaveConfig = new System.Windows.Forms.Button();
            this.chkUse = new System.Windows.Forms.CheckBox();
            this.txtDatLength = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.timer3 = new System.Windows.Forms.Timer(this.components);
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.gbxSend.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.portButton);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.cboCheck);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.cboStopB);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.cboDataB);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.cboBotte);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.cboCom);
            this.groupBox1.Font = new System.Drawing.Font("宋体", 11F);
            this.groupBox1.Location = new System.Drawing.Point(20, 13);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox1.Size = new System.Drawing.Size(424, 338);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "串口设置：";
            // 
            // portButton
            // 
            this.portButton.Font = new System.Drawing.Font("宋体", 11F);
            this.portButton.Location = new System.Drawing.Point(132, 270);
            this.portButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.portButton.Name = "portButton";
            this.portButton.Size = new System.Drawing.Size(169, 45);
            this.portButton.TabIndex = 10;
            this.portButton.Text = "打开串口";
            this.portButton.UseVisualStyleBackColor = true;
            this.portButton.Click += new System.EventHandler(this.btnOpenCOM_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 217);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(116, 26);
            this.label5.TabIndex = 8;
            this.label5.Text = "校验位：";
            // 
            // cboCheck
            // 
            this.cboCheck.FormattingEnabled = true;
            this.cboCheck.Location = new System.Drawing.Point(132, 214);
            this.cboCheck.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cboCheck.Name = "cboCheck";
            this.cboCheck.Size = new System.Drawing.Size(260, 34);
            this.cboCheck.TabIndex = 9;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(14, 168);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(116, 26);
            this.label4.TabIndex = 6;
            this.label4.Text = "停止位：";
            // 
            // cboStopB
            // 
            this.cboStopB.FormattingEnabled = true;
            this.cboStopB.Location = new System.Drawing.Point(132, 165);
            this.cboStopB.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cboStopB.Name = "cboStopB";
            this.cboStopB.Size = new System.Drawing.Size(260, 34);
            this.cboStopB.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 124);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(116, 26);
            this.label3.TabIndex = 4;
            this.label3.Text = "数据位：";
            // 
            // cboDataB
            // 
            this.cboDataB.FormattingEnabled = true;
            this.cboDataB.Location = new System.Drawing.Point(132, 121);
            this.cboDataB.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cboDataB.Name = "cboDataB";
            this.cboDataB.Size = new System.Drawing.Size(260, 34);
            this.cboDataB.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 79);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(116, 26);
            this.label2.TabIndex = 2;
            this.label2.Text = "波特率：";
            // 
            // cboBotte
            // 
            this.cboBotte.FormattingEnabled = true;
            this.cboBotte.Location = new System.Drawing.Point(132, 76);
            this.cboBotte.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cboBotte.Name = "cboBotte";
            this.cboBotte.Size = new System.Drawing.Size(260, 34);
            this.cboBotte.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 34);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 26);
            this.label1.TabIndex = 1;
            this.label1.Text = "串口：";
            // 
            // cboCom
            // 
            this.cboCom.FormattingEnabled = true;
            this.cboCom.Location = new System.Drawing.Point(132, 32);
            this.cboCom.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cboCom.Name = "cboCom";
            this.cboCom.Size = new System.Drawing.Size(260, 34);
            this.cboCom.TabIndex = 1;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txtRec);
            this.groupBox2.Font = new System.Drawing.Font("宋体", 11F);
            this.groupBox2.Location = new System.Drawing.Point(479, 21);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox2.Size = new System.Drawing.Size(1172, 539);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "数据接收区：";
            // 
            // txtRec
            // 
            this.txtRec.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.txtRec.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtRec.Location = new System.Drawing.Point(3, 32);
            this.txtRec.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtRec.MaxLength = 255;
            this.txtRec.Multiline = true;
            this.txtRec.Name = "txtRec";
            this.txtRec.ReadOnly = true;
            this.txtRec.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtRec.Size = new System.Drawing.Size(1166, 513);
            this.txtRec.TabIndex = 0;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btnClear);
            this.groupBox3.Controls.Add(this.rdoHEX);
            this.groupBox3.Controls.Add(this.rdoASCII);
            this.groupBox3.Font = new System.Drawing.Font("宋体", 11F);
            this.groupBox3.Location = new System.Drawing.Point(20, 389);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox3.Size = new System.Drawing.Size(424, 275);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "接收设置：";
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(132, 203);
            this.btnClear.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(170, 53);
            this.btnClear.TabIndex = 2;
            this.btnClear.Text = "清接收区";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.button2_Click);
            // 
            // rdoHEX
            // 
            this.rdoHEX.AutoSize = true;
            this.rdoHEX.Font = new System.Drawing.Font("宋体", 11F);
            this.rdoHEX.Location = new System.Drawing.Point(108, 59);
            this.rdoHEX.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.rdoHEX.Name = "rdoHEX";
            this.rdoHEX.Size = new System.Drawing.Size(193, 30);
            this.rdoHEX.TabIndex = 1;
            this.rdoHEX.TabStop = true;
            this.rdoHEX.Text = "十六进制接收";
            this.rdoHEX.UseVisualStyleBackColor = true;
            this.rdoHEX.CheckedChanged += new System.EventHandler(this.rdoHEX_CheckedChanged);
            // 
            // rdoASCII
            // 
            this.rdoASCII.AutoSize = true;
            this.rdoASCII.Font = new System.Drawing.Font("宋体", 11F);
            this.rdoASCII.Location = new System.Drawing.Point(108, 131);
            this.rdoASCII.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.rdoASCII.Name = "rdoASCII";
            this.rdoASCII.Size = new System.Drawing.Size(154, 30);
            this.rdoASCII.TabIndex = 0;
            this.rdoASCII.TabStop = true;
            this.rdoASCII.Text = "ASCII接收";
            this.rdoASCII.UseVisualStyleBackColor = true;
            this.rdoASCII.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // gbxSend
            // 
            this.gbxSend.Controls.Add(this.transButt);
            this.gbxSend.Controls.Add(this.txtSend);
            this.gbxSend.Font = new System.Drawing.Font("宋体", 11F);
            this.gbxSend.Location = new System.Drawing.Point(482, 614);
            this.gbxSend.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.gbxSend.Name = "gbxSend";
            this.gbxSend.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.gbxSend.Size = new System.Drawing.Size(1166, 326);
            this.gbxSend.TabIndex = 3;
            this.gbxSend.TabStop = false;
            this.gbxSend.Text = "手动数据发送区：";
            // 
            // transButt
            // 
            this.transButt.Location = new System.Drawing.Point(940, 241);
            this.transButt.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.transButt.Name = "transButt";
            this.transButt.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.transButt.Size = new System.Drawing.Size(208, 60);
            this.transButt.TabIndex = 1;
            this.transButt.Text = "发送信息";
            this.transButt.UseVisualStyleBackColor = true;
            this.transButt.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // txtSend
            // 
            this.txtSend.Location = new System.Drawing.Point(14, 35);
            this.txtSend.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtSend.Multiline = true;
            this.txtSend.Name = "txtSend";
            this.txtSend.Size = new System.Drawing.Size(1134, 185);
            this.txtSend.TabIndex = 0;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.lblReceived);
            this.groupBox5.Controls.Add(this.chkAuto);
            this.groupBox5.Controls.Add(this.btnOpenFile);
            this.groupBox5.Controls.Add(this.lblSend);
            this.groupBox5.Controls.Add(this.autoTransText);
            this.groupBox5.Font = new System.Drawing.Font("宋体", 11F);
            this.groupBox5.Location = new System.Drawing.Point(479, 958);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox5.Size = new System.Drawing.Size(1174, 138);
            this.groupBox5.TabIndex = 4;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "自动发送设置";
            // 
            // lblReceived
            // 
            this.lblReceived.AutoSize = true;
            this.lblReceived.Location = new System.Drawing.Point(802, 102);
            this.lblReceived.Name = "lblReceived";
            this.lblReceived.Size = new System.Drawing.Size(90, 26);
            this.lblReceived.TabIndex = 6;
            this.lblReceived.Text = "已接收";
            // 
            // chkAuto
            // 
            this.chkAuto.AutoSize = true;
            this.chkAuto.Location = new System.Drawing.Point(14, 32);
            this.chkAuto.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chkAuto.Name = "chkAuto";
            this.chkAuto.Size = new System.Drawing.Size(142, 30);
            this.chkAuto.TabIndex = 4;
            this.chkAuto.Text = "自动发送";
            this.chkAuto.UseVisualStyleBackColor = true;
            this.chkAuto.CheckedChanged += new System.EventHandler(this.chkAuto_CheckedChanged);
            // 
            // btnOpenFile
            // 
            this.btnOpenFile.Location = new System.Drawing.Point(943, 22);
            this.btnOpenFile.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnOpenFile.Name = "btnOpenFile";
            this.btnOpenFile.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnOpenFile.Size = new System.Drawing.Size(208, 59);
            this.btnOpenFile.TabIndex = 2;
            this.btnOpenFile.Text = "打开文件";
            this.btnOpenFile.UseVisualStyleBackColor = true;
            this.btnOpenFile.Click += new System.EventHandler(this.btnFileOpen_Click);
            // 
            // lblSend
            // 
            this.lblSend.AutoSize = true;
            this.lblSend.Location = new System.Drawing.Point(537, 102);
            this.lblSend.Name = "lblSend";
            this.lblSend.Size = new System.Drawing.Size(90, 26);
            this.lblSend.TabIndex = 5;
            this.lblSend.Text = "已发送";
            // 
            // autoTransText
            // 
            this.autoTransText.Location = new System.Drawing.Point(178, 32);
            this.autoTransText.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.autoTransText.Name = "autoTransText";
            this.autoTransText.Size = new System.Drawing.Size(747, 37);
            this.autoTransText.TabIndex = 0;
            // 
            // timerSend
            // 
            this.timerSend.Interval = 16;
            this.timerSend.Tick += new System.EventHandler(this.sendtimer_Tick);
            // 
            // dlgOpenFile
            // 
            this.dlgOpenFile.FileName = "openFileDialog1";
            // 
            // timSum
            // 
            this.timSum.Tick += new System.EventHandler(this.timSum_Tick);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 200;
            this.timer1.Tick += new System.EventHandler(this.sendtimer_Tick);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.label7);
            this.groupBox4.Controls.Add(this.btnSaveConfig);
            this.groupBox4.Controls.Add(this.chkUse);
            this.groupBox4.Controls.Add(this.txtDatLength);
            this.groupBox4.Controls.Add(this.label6);
            this.groupBox4.Font = new System.Drawing.Font("宋体", 11F);
            this.groupBox4.Location = new System.Drawing.Point(38, 695);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox4.Size = new System.Drawing.Size(424, 329);
            this.groupBox4.TabIndex = 5;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "数据包格式配置";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(289, 75);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(129, 26);
            this.label7.TabIndex = 9;
            this.label7.Text = "(0-65535)";
            // 
            // btnSaveConfig
            // 
            this.btnSaveConfig.Location = new System.Drawing.Point(114, 251);
            this.btnSaveConfig.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnSaveConfig.Name = "btnSaveConfig";
            this.btnSaveConfig.Size = new System.Drawing.Size(170, 62);
            this.btnSaveConfig.TabIndex = 8;
            this.btnSaveConfig.Text = "保存设置";
            this.btnSaveConfig.UseVisualStyleBackColor = true;
            // 
            // chkUse
            // 
            this.chkUse.AutoSize = true;
            this.chkUse.Location = new System.Drawing.Point(14, 32);
            this.chkUse.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chkUse.Name = "chkUse";
            this.chkUse.Size = new System.Drawing.Size(168, 30);
            this.chkUse.TabIndex = 7;
            this.chkUse.Text = "使用数据包";
            this.chkUse.UseVisualStyleBackColor = true;
            // 
            // txtDatLength
            // 
            this.txtDatLength.Location = new System.Drawing.Point(126, 75);
            this.txtDatLength.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtDatLength.Name = "txtDatLength";
            this.txtDatLength.Size = new System.Drawing.Size(140, 37);
            this.txtDatLength.TabIndex = 1;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(14, 75);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(142, 26);
            this.label6.TabIndex = 0;
            this.label6.Text = "数据长度：";
            // 
            // timer3
            // 
            this.timer3.Tick += new System.EventHandler(this.timSum_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1672, 1236);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.gbxSend);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.Text = "串口收发";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.gbxSend.ResumeLayout(false);
            this.gbxSend.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);

        }

        private void label6_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void label7_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void label2_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void label5_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion

        private GroupBox groupBox1;
        private Label label5;
        private ComboBox cboCheck;
        private Label label4;
        private ComboBox cboStopB;
        private Label label3;
        private ComboBox cboDataB;
        private Label label2;
        private ComboBox cboBotte;
        private Label label1;
        private ComboBox cboCom;
        private System.Windows.Forms.Timer autoSendTimer;
        private Button portButton;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private GroupBox gbxSend;
        private TextBox txtRec;
        private RadioButton rdoHEX;
        private RadioButton rdoASCII;
        private Button transButt;
        private TextBox txtSend;
        private GroupBox groupBox5;
        private Button btnClear;
        private Button btnOpenFile;
        private TextBox autoTransText;
        private CheckBox chkAuto;
        private System.Windows.Forms.Timer timerSend;
        private Label lblReceived;
        private Label lblSend;
        private OpenFileDialog dlgOpenFile;
        private System.Windows.Forms.Timer timSum;
        private System.Windows.Forms.Timer timer1;
        private GroupBox groupBox4;
        private CheckBox chkUse;
        private Button btnSaveConfig;
        private TextBox txtDatLength;
        private Label label6;
        private Label label7;
        private Timer timer2;
        private Timer timer3;
    }
}
