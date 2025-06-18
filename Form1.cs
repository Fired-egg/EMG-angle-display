using System.Collections.Concurrent;
using System.Drawing.Drawing2D;
using System.IO.Ports;
using System.Net.Sockets;
using System.Globalization;
using System.Text;
using System.Diagnostics;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
namespace Test0524
{
    public partial class Form1 : Form
    {
        //byte类型数据流
        public Action<byte[]> OnDataReceived;
        public Action<byte[]> OnFunction2DataReceived;
        //定义部分
        byte[] Buffer;
        private int recNum = 0;
        private int sendNum = 0;
        private SerialPort sendCom = new SerialPort();
        private SerialPort recCom = new SerialPort();
        int intSendPosData;
        // 新增类成员变量
        private const int TargetBps = 921600; // 明确目标速率
        
        private DateTime lastSendTime;
        protected bool isComopen = false;
        private byte functionCode = 0x01; // 默认功能码
        private List<byte> receiveBuffer = new List<byte>(4096);

        // 添加以下变量
        private const int MaxDisplayLines = 1000; // 最大显示行数
        private readonly ConcurrentQueue<string> displayQueue = new ConcurrentQueue<string>();
        private int totalLinesAdded = 0;
        private System.Windows.Forms.Timer textRefreshTimer; // 文本刷新定时器
        private TextContainer _textContainer;

        private bool isProcessingPartialPacket = false;
        private List<byte> pendingBuffer = new List<byte>(512); // 待处理缓冲区
        //初始化
        private void Form1_Load(object sender, EventArgs e)
        {
            
            // 配置高精度定时器
            timerSend.Interval = 50; // 50ms间隔
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass =
                System.Diagnostics.ProcessPriorityClass.High;
            // 配置接收定时器
            timSum.Interval = 100; // 100ms刷新间隔
            // 新增HEX模式默认选中
            rdoHEX.Checked = true;  // 关键设置语句
            rdoASCII.Checked = false;
            portButton.BackColor = Color.LawnGreen;//初始化绿色
            txtDatLength.Text = "65535";//数据包默认长度
            _textContainer = new TextContainer(txtRec);
            int[] _commonBaudRates =
                {
                    115200,9600,19200,38400, 57600,74880,230400,460800,921600
                };
            // 获取系统可用串口并添加到下拉框
            cboCom.Items.AddRange(SerialPort.GetPortNames());
            if (cboCom.Items.Count > 0) cboCom.SelectedIndex = 0;
            foreach (var rate in _commonBaudRates)
            {
                cboBotte.Items.Add(rate.ToString());
            }
            //波特率设置
            // 尝试直接找到115200
            int preferredIndex = Array.IndexOf(_commonBaudRates, 115200);
            // 设置默认选择（优先选择115200）
            // 设置选择逻辑
            if (preferredIndex != -1)
            {
                cboBotte.SelectedIndex = preferredIndex;
            }
            else if (cboBotte.Items.Count > 0)
            {
                cboBotte.SelectedIndex = 0;  // 保底选择第一个
            }
            // 数据位配置
            int[] dataBitsOptions = { 8, 7, 6, 5 }; // 按常用顺序排列
            foreach (var bits in dataBitsOptions)
            {
                cboDataB.Items.Add(bits.ToString());
            }
            cboDataB.SelectedIndex = 0; // 默认选择8位数据位

            // 停止位配置
            var stopBitsOptions = new (string, StopBits)[]
            {
                ("1", StopBits.One),          // 最常用
                ("1.5", StopBits.OnePointFive),
                ("2", StopBits.Two)
            };
            foreach (var option in stopBitsOptions)
            {
                cboStopB.Items.Add(option.Item1);
            }
            cboStopB.SelectedIndex = 0; // 默认选择1位停止位

            //校验位配置 
            var parityOptions = new (string, Parity)[]
            {
                ("无校验", Parity.None),     // 最常用
                ("奇校验", Parity.Odd),
                ("偶校验", Parity.Even),
                ("标记校验", Parity.Mark),
                ("空格校验", Parity.Space)
            };

            foreach (var option in parityOptions)
            {
                cboCheck.Items.Add(option.Item1);
            }
            cboCheck.SelectedIndex = 0; // 默认无校验

            // 初始化文本刷新定时器
            textRefreshTimer = new System.Windows.Forms.Timer();
            textRefreshTimer.Interval = 300; // 每300毫秒刷新一次
            textRefreshTimer.Tick += TextRefreshTimer_Tick;
            textRefreshTimer.Start();
        }
        #region 打开串口设置
        private void btnOpenCOM_Click(object sender, EventArgs e)
        {
            if (isComopen == false)
            {
                if (cboCom.SelectedItem == null || cboBotte.SelectedItem == null ||
                cboDataB.SelectedItem == null || cboStopB.SelectedItem == null ||
                cboCheck.SelectedItem == null)
                {
                    MessageBox.Show("请先完成所有参数配置！");
                    return;
                }
                try
                {
                    // 配置串口参数
                    sendCom.PortName = cboCom.SelectedItem.ToString();
                    recCom = sendCom;
                    // 波特率
                    int selectedBaudRate = int.Parse(cboBotte.SelectedItem.ToString());
                    sendCom.BaudRate = recCom.BaudRate = selectedBaudRate;
                    // 优化缓冲区设置
                    sendCom.WriteBufferSize = 1024 * 1024; // 1MB写缓冲区
                    recCom.ReadBufferSize = 1024 * 1024;  // 1MB读缓冲区
                    recCom.DataBits = int.Parse(cboDataB.SelectedItem.ToString()); sendCom.DataBits = recCom.DataBits;
                    recCom.StopBits = cboStopB.SelectedItem.ToString() switch
                    {
                        "1" => StopBits.One,
                        "1.5" => StopBits.OnePointFive,
                        "2" => StopBits.Two,
                        _ => StopBits.One
                    };
                    sendCom.StopBits = recCom.StopBits;
                    // 内联处理校验位转换
                    recCom.Parity = cboCheck.SelectedItem.ToString() switch
                    {
                        "无校验" => Parity.None,
                        "奇校验" => Parity.Odd,
                        "偶校验" => Parity.Even,
                        "标记校验" => Parity.Mark,
                        "空格校验" => Parity.Space,

                        _ => Parity.None // 默认无校验
                    };
                    sendCom.Parity = recCom.Parity;
                    //serialPort.Open();
                    isComopen = true;
                    recCom.Open();
                    //打开串口
                    //假设成功打开串口
                    portButton.BackColor = Color.Red;
                    portButton.Text = "关闭串口";
                    timSum.Enabled = true;
                }
                catch (Exception ex)
                {
                    isComopen = false;
                    MessageBox.Show($"串口打开失败：{ex.Message}\n请检查：\n1. 端口是否存在\n2. 是否被其他程序占用",
                                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                isComopen = false;
                //关闭串口
                recCom.Close();
                sendCom.Close();
                portButton.BackColor = Color.LawnGreen;
                portButton.Text = "打开串口";
                timSum.Enabled = false;
            }
        }
        #endregion
        #region 封包方法
        private byte[] BuildPacket(byte[] payload)
        {
            // 将原功能码转换为新格式 (0x01->0xF1, 0x02->0xF2)
            byte newFunctionCode = functionCode == 0x01 ? (byte)0xF1 : (byte)0xF2;

            // 使用2字节表示长度 (高字节在前)
            ushort payloadLength = (ushort)payload.Length;
            byte lengthHigh = (byte)(payloadLength >> 8);
            byte lengthLow = (byte)(payloadLength & 0xFF);

            // 包头部分: AA FF [功能码] [长度高字节] [长度低字节]
            byte[] header = new byte[] { 0xAA, 0xFF, newFunctionCode, lengthHigh, lengthLow };

            // 合并包头+数据
            byte[] packet = header.Concat(payload).ToArray();

            // 计算新校验码 (SC和AC)
            byte sc = 0;
            byte ac = 0;
            foreach (byte b in packet)
            {
                sc = (byte)(sc + b);
                ac = (byte)(ac + sc);
            }

            // 添加校验码并返回
            return packet.Concat(new[] { sc, ac }).ToArray();
        }
        #endregion

        #region 数据解包
        private void ProcessPackets()
        {
            const int maxBufferSize = 1024 * 100; // 100KB最大缓冲区

            // 处理双重缓冲区：合并待处理数据和当前数据
            if (pendingBuffer.Count > 0)
            {
                receiveBuffer.InsertRange(0, pendingBuffer);
                pendingBuffer.Clear();
            }

            // 检查缓冲区大小
            if (receiveBuffer.Count > maxBufferSize)
            {
                // 清除前75%的数据，保留后25%可能包含包头的数据
                int keepCount = receiveBuffer.Count / 4;
                pendingBuffer = receiveBuffer.Skip(receiveBuffer.Count - keepCount).ToList();
                receiveBuffer = pendingBuffer.ToList();
                pendingBuffer.Clear();

                Debug.WriteLine($"缓冲区溢出保护：已缩减缓冲区至{keepCount}字节");
                return;
            }

            // 最小包长现在为7 (5字节包头 + 2字节校验)
            const int minPacketSize = 7;

            while (receiveBuffer.Count >= minPacketSize)
            {
                bool foundPacket = false;
                int headerStart = -1;
                bool isLastPacketComplete = false;

                // 第一遍：扫描完整包
                for (int i = 0; i <= receiveBuffer.Count - 2; i++)
                {
                    if (receiveBuffer[i] == 0xAA && receiveBuffer[i + 1] == 0xFF)
                    {
                        headerStart = i;
                        isLastPacketComplete = false;

                        // 检查包头后的数据长度是否足够解析 (至少需要3字节：功能码+长度高+长度低)
                        if (receiveBuffer.Count - i < 5) // 包头5字节
                        {
                            // 包头后的数据不足，等待更多数据
                            break;
                        }

                        // 读取2字节长度 (高字节在前)
                        int dataLength = (receiveBuffer[i + 3] << 8) | receiveBuffer[i + 4];

                        // 完整包长度 = 5字节包头 + 数据长度 + 2字节校验
                        int totalPacketLength = 5 + dataLength + 2;

                        if (receiveBuffer.Count - i >= totalPacketLength)
                        {
                            // 找到完整包
                            foundPacket = true;
                            isLastPacketComplete = true;
                            ExtractAndProcessPacket(i, totalPacketLength);
                            receiveBuffer.RemoveRange(i, totalPacketLength);
                            break;
                        }
                        else
                        {
                            // 检测到包头但数据不完整
                            isLastPacketComplete = false;
                            headerStart = i;
                            break;
                        }
                    }
                }

                // 第二遍：处理不完整包
                if (!foundPacket && headerStart >= 0 && !isLastPacketComplete)
                {
                    // 将不完整的包头移动到待处理缓冲区
                    pendingBuffer = receiveBuffer.Skip(headerStart).ToList();
                    receiveBuffer.RemoveRange(headerStart, receiveBuffer.Count - headerStart);
                    isProcessingPartialPacket = true;

                    Debug.WriteLine($"检测到不完整包头，已保存{pendingBuffer.Count}字节到待处理区");
                    break;
                }

                if (!foundPacket && headerStart == -1)
                {
                    // 完全没有包头，清除当前缓冲区
                    Debug.WriteLine("未找到任何包头，清除缓冲区");
                    receiveBuffer.Clear();
                    break;
                }
            }
        }
        private void ExtractAndProcessPacket(int startIndex, int totalPacketLength)
        {
            // 提取完整数据包
            byte[] packet = receiveBuffer.Skip(startIndex).Take(totalPacketLength).ToArray();

            // 计算校验 (SC和AC)
            byte sc = 0;
            byte ac = 0;
            for (int i = 0; i < packet.Length - 2; i++)
            {
                sc = (byte)(sc + packet[i]);
                ac = (byte)(ac + sc);
            }

            // 验证校验码
            if (packet[packet.Length - 2] != sc || packet[packet.Length - 1] != ac)
            {
                Debug.WriteLine($"校验失败: 预期 SC={sc}, AC={ac} 实际 SC={packet[packet.Length - 2]}, AC={packet[packet.Length - 1]}");
                return;
            }

            // 提取功能码 (第2个字节)
            byte functionCode = packet[2];

            // 读取2字节数据长度 (第3和4字节，高字节在前)
            int dataLength = (packet[3] << 8) | packet[4];

            // 提取有效载荷 (第5个字节开始)
            byte[] payload = new byte[dataLength];
            Array.Copy(packet, 5, payload, 0, dataLength);

            // 触发相应事件
            if (functionCode == 0xF1)
            {
                OnDataReceived?.Invoke(payload);
            }
            else if (functionCode == 0xF2)
            {
                OnFunction2DataReceived?.Invoke(payload);
            }
            else if (functionCode >= 0xF3 && functionCode <= 0xFA)
            {
                // 处理其他功能码
            }

            Debug.WriteLine($"成功处理 {functionCode} 功能包, 数据长度: {dataLength} 字节");
        }
        #endregion
        //==================================================================================================
        //文件保存设置
        public Form1()
        {
            InitializeComponent();
            dlgOpenFile.Title = "选择数据文件";
            dlgOpenFile.Filter = "所有文件|*.*";
            dlgOpenFile.DefaultExt = " ";
            dlgOpenFile.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dlgOpenFile.CheckFileExists = true;
            dlgOpenFile.CheckPathExists = true;
        }

        private byte[] HexStringToByteArray(string hex)
        {
            hex = hex.Replace(" ", "").Replace("-", ""); // 兼容分隔符
            if (hex.Length % 2 != 0) throw new FormatException("HEX字符串长度必须为偶数");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                string byteStr = hex.Substring(i, 2);
                if (!byte.TryParse(byteStr, NumberStyles.HexNumber, null, out bytes[i / 2]))
                    throw new FormatException($"非法HEX字符：{byteStr}");
            }
            return bytes;
        }
        #region 发送相关设置
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSend.Text))
            {
                MessageBox.Show("发送内容不能为空！");
                return;
            }

            if (!sendCom.IsOpen)
            {
                MessageBox.Show("请先打开串口！");
                return;
            }

            try
            {
                byte[] sendData;

                if (chkUse.Checked) // 使用协议封装
                {
                    // 获取原始数据
                    byte[] payload = rdoHEX.Checked ?
                        HexStringToByteArray(txtSend.Text) :
                        Encoding.ASCII.GetBytes(txtSend.Text);

                    // 自动截断长度
                    int maxLength = 65535;
                    if (int.TryParse(txtDatLength.Text, out int userLength))
                        maxLength = Math.Min(userLength, 65535);

                    if (payload.Length > maxLength)
                        Array.Resize(ref payload, maxLength);

                    // 封装数据包
                    sendData = BuildPacket(payload);
                }
                else // 普通模式
                {
                    sendData = rdoHEX.Checked ?
                        HexStringToByteArray(txtSend.Text) :
                        Encoding.ASCII.GetBytes(txtSend.Text);
                }

                sendCom.Write(sendData, 0, sendData.Length);
                sendNum += sendData.Length;
                lblSend.Text = $"发送数据：{sendNum} 字节";
            }
            catch (FormatException ex)
            {
                MessageBox.Show($"HEX格式错误：{ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送失败：{ex.Message}");
            }
        }
        #endregion

        private void button2_Click(object sender, EventArgs e)
        {
            _textContainer.Clear(); // 使用容器的清除方法
            recNum = 0;      // 重置接收统计
            lblReceived.Text = "接收数据：0 字节";
        }

        private void btnFileOpen_Click(object sender, EventArgs e)
        {
            try
            {
                if (dlgOpenFile.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // 使用StreamReader读取文本格式ECG文件
                        using (var reader = new StreamReader(dlgOpenFile.FileName, Encoding.UTF8))
                        {
                            StringBuilder sb = new StringBuilder();
                            Buffer = File.ReadAllBytes(dlgOpenFile.FileName);

                            txtSend.Tag = dlgOpenFile.FileName; // 保存文件路径供后续使用
                            autoTransText.Text = $"{Path.GetFileName(dlgOpenFile.FileName)}";
                            MessageBox.Show($"已加载文件：\n{dlgOpenFile.FileName}", "文件选择成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        MessageBox.Show("没有文件访问权限！",
                                      "权限错误",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Warning);
                    }
                    catch (IOException ioEx)
                    {
                        MessageBox.Show($"文件读取错误：{ioEx.Message}",
                                      "IO错误",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"文件操作失败：{ex.Message}",
                              "错误",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }

        private void lblRec_Click(object sender, EventArgs e)
        {

        }

        private void sendtimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!sendCom.IsOpen || !chkAuto.Checked) return;

                // 动态计算发送量
                TimeSpan elapsed = DateTime.Now - lastSendTime;
                int targetBytes = (int)(TargetBps * elapsed.TotalSeconds);

                // 限制每次发送量
                targetBytes = Math.Min(targetBytes, 4096);
                targetBytes = Math.Min(targetBytes, Buffer.Length - intSendPosData);
                if (targetBytes <= 0)
                {
                    if (intSendPosData >= Buffer.Length)
                    {
                        // 完成发送时更新UI
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            chkAuto.Checked = false;
                            MessageBox.Show("文件发送完成！");
                        });
                    }
                    return;
                }
                // 实际发送操作
                sendCom.Write(Buffer, intSendPosData, targetBytes);
                intSendPosData += targetBytes;
                sendNum += targetBytes;
                lastSendTime = DateTime.Now;

                // 更新发送显示（跨线程安全）
                this.BeginInvoke((MethodInvoker)delegate
                {
                    lblSend.Text = $"发送数据：{sendNum} 字节";
                });
            }
            catch (Exception ex)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    chkAuto.Checked = false;
                    MessageBox.Show($"发送失败：{ex.Message}");
                });
            }
        }
        private void chkAuto_CheckedChanged(object sender, EventArgs e)
        {
            // 确保只在用户操作时响应
            if (chkAuto.Checked)
            {
                if (!sendCom.IsOpen)
                {
                    chkAuto.Checked = false; // 需要显式取消选中
                    MessageBox.Show("请先打开串口！");
                    return;
                }

                if (Buffer == null || Buffer.Length == 0)
                {
                    chkAuto.Checked = false;
                    MessageBox.Show("请先加载要发送的文件！");
                    return;
                }

                // 重置状态
                intSendPosData = 0;
                sendNum = 0;
                lastSendTime = DateTime.Now;

                // 配置并启动定时器
                timerSend.Interval = 50; // 确保定时器间隔设置正确
                timerSend.Enabled = true;
                timerSend.Start(); // 显式启动定时器
            }
            else
            {
                timerSend.Enabled = false;
                timerSend.Stop(); // 显式停止定时器
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {

        }

        private void TextRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (displayQueue.IsEmpty) return;

            // 每次刷新最多处理100条
            int maxProcess = 100000;
            StringBuilder sb = new StringBuilder();

            while (maxProcess-- > 0 && displayQueue.TryDequeue(out string text))
            {
                sb.AppendLine(text);
                totalLinesAdded++;
            }

            if (sb.Length > 0)
            {
                txtRec.AppendText(sb.ToString());

                // 只有当前行数超过限制时才清理
                if (txtRec.Lines.Length > MaxDisplayLines * 1.2)
                {
                    var lines = txtRec.Lines;
                    int keepCount = Math.Min(MaxDisplayLines, lines.Length);
                    var newLines = lines.Skip(lines.Length - keepCount).ToArray();
                    txtRec.Lines = newLines;
                    totalLinesAdded = keepCount;
                }

                // 自动滚动到底部
                txtRec.SelectionStart = txtRec.TextLength;
                txtRec.ScrollToCaret();
            }
        }
        private void timSum_Tick(object sender, EventArgs e)
        {
            // 更新统计显示
            lblSend.Text = $"发送数据：{sendNum} 字节";
            lblReceived.Text = $"接收数据：{recNum} 字节";

            // 接收数据处理
            if (recCom.IsOpen && recCom.BytesToRead > 0)
            {
                try
                {
                    byte[] buffer = new byte[recCom.BytesToRead];
                    int bytesRead = recCom.Read(buffer, 0, buffer.Length);
                    recNum += bytesRead;

                    // 根据模式处理数据
                    if (chkUse.Checked)
                    {
                        // 使用协议模式：将数据添加到缓冲区并处理
                        receiveBuffer.AddRange(buffer);
                        ProcessPackets();
                    }
                    else
                    {
                        // 触发事件（原始数据）
                        OnDataReceived?.Invoke(buffer);

                        // 普通模式：直接处理原始数据
                        string displayText = rdoHEX.Checked ?
                            BitConverter.ToString(buffer).Replace("-", " ") :
                            Encoding.ASCII.GetString(buffer);

                        // 使用文本容器而非直接更新UI
                        _textContainer.AddText(displayText);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"接收错误：{ex.Message}");
                }
            }
        }

        // 窗体关闭时释放资源
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _textContainer?.Dispose(); // 释放文本容器
            // 停止并释放文本刷新计时器
            if (textRefreshTimer != null)
            {
                textRefreshTimer.Stop();
                textRefreshTimer.Dispose();
            }

            // 其他关闭逻辑...
            if (recCom.IsOpen)
            {
                recCom.Close();
            }
            if (sendCom.IsOpen)
            {
                sendCom.Close();
            }
            base.OnFormClosing(e);
        }
        public class TextContainer : IDisposable
        {
            private const int MaxDisplayLines = 1000;
            private readonly TextBox _textBox;
            private readonly System.Windows.Forms.Timer _updateTimer;
            private ConcurrentQueue<string> _currentQueue = new ConcurrentQueue<string>();
            private long _totalCharacters = 0;
            private const int MAX_CHARACTERS = 200 * 1024; // 200KB字符缓冲区

            public TextContainer(TextBox textBox)
            {
                _textBox = textBox;
                _updateTimer = new System.Windows.Forms.Timer();
                _updateTimer.Interval = 200; // 200ms刷新间隔
                _updateTimer.Tick += UpdateTextBox;
                _updateTimer.Start();
            }

            public void AddText(string text)
            {
                _currentQueue.Enqueue(text);

                // 字符数统计
                Interlocked.Add(ref _totalCharacters, text.Length);
            }

            public void Clear()
            {
                // 创建新队列替换旧队列（最快清空方式）
                var newQueue = new ConcurrentQueue<string>();
                Interlocked.Exchange(ref _currentQueue, newQueue);
                Interlocked.Exchange(ref _totalCharacters, 0);

                // UI线程清空文本框
                _textBox.Invoke(new Action(() => _textBox.Clear()));
            }

            private void UpdateTextBox(object sender, EventArgs e)
            {
                if (_currentQueue.IsEmpty) return;

                // 替换为新队列（原子操作）
                var processQueue = _currentQueue;
                _currentQueue = new ConcurrentQueue<string>();
                Interlocked.Exchange(ref _totalCharacters, 0);

                StringBuilder sb = new StringBuilder();
                string item;
                int lineCount = 0;

                // 处理队列中的项目
                while (processQueue.TryDequeue(out item))
                {
                    sb.AppendLine(item);
                    lineCount++;

                    // 保护性限制：一次最多处理500行
                    if (lineCount >= 500) break;
                }

                if (sb.Length == 0) return;

                // 更新UI（在UI线程执行）
                _textBox.Invoke(new Action(() => {
                    // 高效追加文本
                    _textBox.AppendText(sb.ToString());

                    // 动态行数管理（避免昂贵的Lines属性）
                    var textLength = _textBox.TextLength;
                    if (textLength > MAX_CHARACTERS)
                    {
                        // 快速删除前1/3内容（最快的方法）
                        int removeUpTo = textLength / 3;
                        int firstNewLine = _textBox.Text.IndexOf('\n', removeUpTo);

                        if (firstNewLine > removeUpTo)
                        {
                            _textBox.Select(0, firstNewLine + 1);
                            _textBox.SelectedText = "";
                        }
                    }

                    // 自动滚动
                    _textBox.SelectionStart = _textBox.TextLength;
                    _textBox.ScrollToCaret();
                }));
            }

            public void Dispose() => _updateTimer?.Stop();
        }
        #region 垃圾
        private void rdoHEX_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoHEX.Checked)
                txtSend.Tag = "HEX"; // 标记当前编码模式
        }
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoASCII.Checked)
                txtSend.Tag = "ASCII";
        }
        private void txtRec_TextChanged(object sender, EventArgs e)
        {

        }
        #endregion
    }
}
