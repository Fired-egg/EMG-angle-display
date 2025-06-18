using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Test0524
{
    
    public partial class Form3 : Form
    {
        Form1 serialform = new Form1();
        Form2 waveform = new Form2();
        Form4 parmform = new Form4();
        public Form3()
        {
            InitializeComponent();
            // 订阅 Form1 的 OnDataReceived 事件，将数据传递给 Form2
            serialform.OnDataReceived += waveform.OnDataReceived;
            serialform.OnFunction2DataReceived += waveform.OnFunction2DataReceived;
        }

        private async void Form3_Load(object sender, EventArgs e)
        {
            // 同时初始化两个子窗体
            InitializeSubForm(waveform, pnlSw);
            InitializeSubForm(serialform, pnlSw);
            InitializeSubForm(parmform, pnlSw);
            serialform.BringToFront();
        }
        private void InitializeSubForm(Form form, Control container)
        {
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            if (!container.Controls.Contains(form))
            {
                container.Controls.Add(form);
            }
            form.Show();
            form.BringToFront();
        }
        private void btnSerialsw_Click(object sender, EventArgs e)
        {
            //显示串口
            serialform.BringToFront();
        }

        private void btnWavesw_Click(object sender, EventArgs e)
        {
                waveform.BringToFront();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnParm_Click(object sender, EventArgs e)
        {
            parmform.BringToFront();
        }
    }
}
