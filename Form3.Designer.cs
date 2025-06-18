using System.Drawing;
using System.Windows.Forms;

namespace Test0524
{
    partial class Form3
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
            this.btnSerialsw = new System.Windows.Forms.Button();
            this.btnWavesw = new System.Windows.Forms.Button();
            this.pnlSw = new System.Windows.Forms.Panel();
            this.btnParm = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnSerialsw
            // 
            this.btnSerialsw.Font = new System.Drawing.Font("宋体", 11F);
            this.btnSerialsw.Location = new System.Drawing.Point(10, 9);
            this.btnSerialsw.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnSerialsw.Name = "btnSerialsw";
            this.btnSerialsw.Size = new System.Drawing.Size(187, 42);
            this.btnSerialsw.TabIndex = 0;
            this.btnSerialsw.Text = "串口收发";
            this.btnSerialsw.UseVisualStyleBackColor = true;
            this.btnSerialsw.Click += new System.EventHandler(this.btnSerialsw_Click);
            // 
            // btnWavesw
            // 
            this.btnWavesw.Font = new System.Drawing.Font("宋体", 11F);
            this.btnWavesw.Location = new System.Drawing.Point(222, 9);
            this.btnWavesw.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnWavesw.Name = "btnWavesw";
            this.btnWavesw.Size = new System.Drawing.Size(198, 42);
            this.btnWavesw.TabIndex = 1;
            this.btnWavesw.Text = "波形显示";
            this.btnWavesw.UseVisualStyleBackColor = true;
            this.btnWavesw.Click += new System.EventHandler(this.btnWavesw_Click);
            // 
            // pnlSw
            // 
            this.pnlSw.Location = new System.Drawing.Point(16, 55);
            this.pnlSw.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pnlSw.Name = "pnlSw";
            this.pnlSw.Size = new System.Drawing.Size(1692, 1300);
            this.pnlSw.TabIndex = 2;
            // 
            // btnParm
            // 
            this.btnParm.Font = new System.Drawing.Font("宋体", 11F);
            this.btnParm.Location = new System.Drawing.Point(441, 9);
            this.btnParm.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnParm.Name = "btnParm";
            this.btnParm.Size = new System.Drawing.Size(198, 42);
            this.btnParm.TabIndex = 3;
            this.btnParm.Text = "参数显示";
            this.btnParm.UseVisualStyleBackColor = true;
            this.btnParm.Click += new System.EventHandler(this.btnParm_Click);
            // 
            // Form3
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1757, 1287);
            this.Controls.Add(this.btnParm);
            this.Controls.Add(this.pnlSw);
            this.Controls.Add(this.btnWavesw);
            this.Controls.Add(this.btnSerialsw);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form3";
            this.Text = "肌电及关节角度监测系统";
            this.Load += new System.EventHandler(this.Form3_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private Button btnSerialsw;
        private Button btnWavesw;
        private Panel pnlSw;
        private Button btnParm;
    }
}