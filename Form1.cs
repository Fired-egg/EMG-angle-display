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
        //byte����������
        public Action<byte[]> OnDataReceived;
        public Action<byte[]> OnFunction2DataReceived;
        //���岿��
        byte[] Buffer;
        private int recNum = 0;
        private int sendNum = 0;
        private SerialPort sendCom = new SerialPort();
        private SerialPort recCom = new SerialPort();
        int intSendPosData;
        // �������Ա����
        private const int TargetBps = 921600; // ��ȷĿ������
        
        private DateTime lastSendTime;
        protected bool isComopen = false;
        private byte functionCode = 0x01; // Ĭ�Ϲ�����
        private List<byte> receiveBuffer = new List<byte>(4096);

        // ������±���
        private const int MaxDisplayLines = 1000; // �����ʾ����
        private readonly ConcurrentQueue<string> displayQueue = new ConcurrentQueue<string>();
        private int totalLinesAdded = 0;
        private System.Windows.Forms.Timer textRefreshTimer; // �ı�ˢ�¶�ʱ��
        private TextContainer _textContainer;

        private bool isProcessingPartialPacket = false;
        private List<byte> pendingBuffer = new List<byte>(512); // ����������
        //��ʼ��
        private void Form1_Load(object sender, EventArgs e)
        {
            
            // ���ø߾��ȶ�ʱ��
            timerSend.Interval = 50; // 50ms���
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass =
                System.Diagnostics.ProcessPriorityClass.High;
            // ���ý��ն�ʱ��
            timSum.Interval = 100; // 100msˢ�¼��
            // ����HEXģʽĬ��ѡ��
            rdoHEX.Checked = true;  // �ؼ��������
            rdoASCII.Checked = false;
            portButton.BackColor = Color.LawnGreen;//��ʼ����ɫ
            txtDatLength.Text = "65535";//���ݰ�Ĭ�ϳ���
            _textContainer = new TextContainer(txtRec);
            int[] _commonBaudRates =
                {
                    115200,9600,19200,38400, 57600,74880,230400,460800,921600
                };
            // ��ȡϵͳ���ô��ڲ���ӵ�������
            cboCom.Items.AddRange(SerialPort.GetPortNames());
            if (cboCom.Items.Count > 0) cboCom.SelectedIndex = 0;
            foreach (var rate in _commonBaudRates)
            {
                cboBotte.Items.Add(rate.ToString());
            }
            //����������
            // ����ֱ���ҵ�115200
            int preferredIndex = Array.IndexOf(_commonBaudRates, 115200);
            // ����Ĭ��ѡ������ѡ��115200��
            // ����ѡ���߼�
            if (preferredIndex != -1)
            {
                cboBotte.SelectedIndex = preferredIndex;
            }
            else if (cboBotte.Items.Count > 0)
            {
                cboBotte.SelectedIndex = 0;  // ����ѡ���һ��
            }
            // ����λ����
            int[] dataBitsOptions = { 8, 7, 6, 5 }; // ������˳������
            foreach (var bits in dataBitsOptions)
            {
                cboDataB.Items.Add(bits.ToString());
            }
            cboDataB.SelectedIndex = 0; // Ĭ��ѡ��8λ����λ

            // ֹͣλ����
            var stopBitsOptions = new (string, StopBits)[]
            {
                ("1", StopBits.One),          // ���
                ("1.5", StopBits.OnePointFive),
                ("2", StopBits.Two)
            };
            foreach (var option in stopBitsOptions)
            {
                cboStopB.Items.Add(option.Item1);
            }
            cboStopB.SelectedIndex = 0; // Ĭ��ѡ��1λֹͣλ

            //У��λ���� 
            var parityOptions = new (string, Parity)[]
            {
                ("��У��", Parity.None),     // ���
                ("��У��", Parity.Odd),
                ("żУ��", Parity.Even),
                ("���У��", Parity.Mark),
                ("�ո�У��", Parity.Space)
            };

            foreach (var option in parityOptions)
            {
                cboCheck.Items.Add(option.Item1);
            }
            cboCheck.SelectedIndex = 0; // Ĭ����У��

            // ��ʼ���ı�ˢ�¶�ʱ��
            textRefreshTimer = new System.Windows.Forms.Timer();
            textRefreshTimer.Interval = 300; // ÿ300����ˢ��һ��
            textRefreshTimer.Tick += TextRefreshTimer_Tick;
            textRefreshTimer.Start();
        }
        #region �򿪴�������
        private void btnOpenCOM_Click(object sender, EventArgs e)
        {
            if (isComopen == false)
            {
                if (cboCom.SelectedItem == null || cboBotte.SelectedItem == null ||
                cboDataB.SelectedItem == null || cboStopB.SelectedItem == null ||
                cboCheck.SelectedItem == null)
                {
                    MessageBox.Show("����������в������ã�");
                    return;
                }
                try
                {
                    // ���ô��ڲ���
                    sendCom.PortName = cboCom.SelectedItem.ToString();
                    recCom = sendCom;
                    // ������
                    int selectedBaudRate = int.Parse(cboBotte.SelectedItem.ToString());
                    sendCom.BaudRate = recCom.BaudRate = selectedBaudRate;
                    // �Ż�����������
                    sendCom.WriteBufferSize = 1024 * 1024; // 1MBд������
                    recCom.ReadBufferSize = 1024 * 1024;  // 1MB��������
                    recCom.DataBits = int.Parse(cboDataB.SelectedItem.ToString()); sendCom.DataBits = recCom.DataBits;
                    recCom.StopBits = cboStopB.SelectedItem.ToString() switch
                    {
                        "1" => StopBits.One,
                        "1.5" => StopBits.OnePointFive,
                        "2" => StopBits.Two,
                        _ => StopBits.One
                    };
                    sendCom.StopBits = recCom.StopBits;
                    // ��������У��λת��
                    recCom.Parity = cboCheck.SelectedItem.ToString() switch
                    {
                        "��У��" => Parity.None,
                        "��У��" => Parity.Odd,
                        "żУ��" => Parity.Even,
                        "���У��" => Parity.Mark,
                        "�ո�У��" => Parity.Space,

                        _ => Parity.None // Ĭ����У��
                    };
                    sendCom.Parity = recCom.Parity;
                    //serialPort.Open();
                    isComopen = true;
                    recCom.Open();
                    //�򿪴���
                    //����ɹ��򿪴���
                    portButton.BackColor = Color.Red;
                    portButton.Text = "�رմ���";
                    timSum.Enabled = true;
                }
                catch (Exception ex)
                {
                    isComopen = false;
                    MessageBox.Show($"���ڴ�ʧ�ܣ�{ex.Message}\n���飺\n1. �˿��Ƿ����\n2. �Ƿ���������ռ��",
                                    "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                isComopen = false;
                //�رմ���
                recCom.Close();
                sendCom.Close();
                portButton.BackColor = Color.LawnGreen;
                portButton.Text = "�򿪴���";
                timSum.Enabled = false;
            }
        }
        #endregion
        #region �������
        private byte[] BuildPacket(byte[] payload)
        {
            // ��ԭ������ת��Ϊ�¸�ʽ (0x01->0xF1, 0x02->0xF2)
            byte newFunctionCode = functionCode == 0x01 ? (byte)0xF1 : (byte)0xF2;

            // ʹ��2�ֽڱ�ʾ���� (���ֽ���ǰ)
            ushort payloadLength = (ushort)payload.Length;
            byte lengthHigh = (byte)(payloadLength >> 8);
            byte lengthLow = (byte)(payloadLength & 0xFF);

            // ��ͷ����: AA FF [������] [���ȸ��ֽ�] [���ȵ��ֽ�]
            byte[] header = new byte[] { 0xAA, 0xFF, newFunctionCode, lengthHigh, lengthLow };

            // �ϲ���ͷ+����
            byte[] packet = header.Concat(payload).ToArray();

            // ������У���� (SC��AC)
            byte sc = 0;
            byte ac = 0;
            foreach (byte b in packet)
            {
                sc = (byte)(sc + b);
                ac = (byte)(ac + sc);
            }

            // ���У���벢����
            return packet.Concat(new[] { sc, ac }).ToArray();
        }
        #endregion

        #region ���ݽ��
        private void ProcessPackets()
        {
            const int maxBufferSize = 1024 * 100; // 100KB��󻺳���

            // ����˫�ػ��������ϲ����������ݺ͵�ǰ����
            if (pendingBuffer.Count > 0)
            {
                receiveBuffer.InsertRange(0, pendingBuffer);
                pendingBuffer.Clear();
            }

            // ��黺������С
            if (receiveBuffer.Count > maxBufferSize)
            {
                // ���ǰ75%�����ݣ�������25%���ܰ�����ͷ������
                int keepCount = receiveBuffer.Count / 4;
                pendingBuffer = receiveBuffer.Skip(receiveBuffer.Count - keepCount).ToList();
                receiveBuffer = pendingBuffer.ToList();
                pendingBuffer.Clear();

                Debug.WriteLine($"�����������������������������{keepCount}�ֽ�");
                return;
            }

            // ��С��������Ϊ7 (5�ֽڰ�ͷ + 2�ֽ�У��)
            const int minPacketSize = 7;

            while (receiveBuffer.Count >= minPacketSize)
            {
                bool foundPacket = false;
                int headerStart = -1;
                bool isLastPacketComplete = false;

                // ��һ�飺ɨ��������
                for (int i = 0; i <= receiveBuffer.Count - 2; i++)
                {
                    if (receiveBuffer[i] == 0xAA && receiveBuffer[i + 1] == 0xFF)
                    {
                        headerStart = i;
                        isLastPacketComplete = false;

                        // ����ͷ������ݳ����Ƿ��㹻���� (������Ҫ3�ֽڣ�������+���ȸ�+���ȵ�)
                        if (receiveBuffer.Count - i < 5) // ��ͷ5�ֽ�
                        {
                            // ��ͷ������ݲ��㣬�ȴ���������
                            break;
                        }

                        // ��ȡ2�ֽڳ��� (���ֽ���ǰ)
                        int dataLength = (receiveBuffer[i + 3] << 8) | receiveBuffer[i + 4];

                        // ���������� = 5�ֽڰ�ͷ + ���ݳ��� + 2�ֽ�У��
                        int totalPacketLength = 5 + dataLength + 2;

                        if (receiveBuffer.Count - i >= totalPacketLength)
                        {
                            // �ҵ�������
                            foundPacket = true;
                            isLastPacketComplete = true;
                            ExtractAndProcessPacket(i, totalPacketLength);
                            receiveBuffer.RemoveRange(i, totalPacketLength);
                            break;
                        }
                        else
                        {
                            // ��⵽��ͷ�����ݲ�����
                            isLastPacketComplete = false;
                            headerStart = i;
                            break;
                        }
                    }
                }

                // �ڶ��飺����������
                if (!foundPacket && headerStart >= 0 && !isLastPacketComplete)
                {
                    // ���������İ�ͷ�ƶ�������������
                    pendingBuffer = receiveBuffer.Skip(headerStart).ToList();
                    receiveBuffer.RemoveRange(headerStart, receiveBuffer.Count - headerStart);
                    isProcessingPartialPacket = true;

                    Debug.WriteLine($"��⵽��������ͷ���ѱ���{pendingBuffer.Count}�ֽڵ���������");
                    break;
                }

                if (!foundPacket && headerStart == -1)
                {
                    // ��ȫû�а�ͷ�������ǰ������
                    Debug.WriteLine("δ�ҵ��κΰ�ͷ�����������");
                    receiveBuffer.Clear();
                    break;
                }
            }
        }
        private void ExtractAndProcessPacket(int startIndex, int totalPacketLength)
        {
            // ��ȡ�������ݰ�
            byte[] packet = receiveBuffer.Skip(startIndex).Take(totalPacketLength).ToArray();

            // ����У�� (SC��AC)
            byte sc = 0;
            byte ac = 0;
            for (int i = 0; i < packet.Length - 2; i++)
            {
                sc = (byte)(sc + packet[i]);
                ac = (byte)(ac + sc);
            }

            // ��֤У����
            if (packet[packet.Length - 2] != sc || packet[packet.Length - 1] != ac)
            {
                Debug.WriteLine($"У��ʧ��: Ԥ�� SC={sc}, AC={ac} ʵ�� SC={packet[packet.Length - 2]}, AC={packet[packet.Length - 1]}");
                return;
            }

            // ��ȡ������ (��2���ֽ�)
            byte functionCode = packet[2];

            // ��ȡ2�ֽ����ݳ��� (��3��4�ֽڣ����ֽ���ǰ)
            int dataLength = (packet[3] << 8) | packet[4];

            // ��ȡ��Ч�غ� (��5���ֽڿ�ʼ)
            byte[] payload = new byte[dataLength];
            Array.Copy(packet, 5, payload, 0, dataLength);

            // ������Ӧ�¼�
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
                // ��������������
            }

            Debug.WriteLine($"�ɹ����� {functionCode} ���ܰ�, ���ݳ���: {dataLength} �ֽ�");
        }
        #endregion
        //==================================================================================================
        //�ļ���������
        public Form1()
        {
            InitializeComponent();
            dlgOpenFile.Title = "ѡ�������ļ�";
            dlgOpenFile.Filter = "�����ļ�|*.*";
            dlgOpenFile.DefaultExt = " ";
            dlgOpenFile.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dlgOpenFile.CheckFileExists = true;
            dlgOpenFile.CheckPathExists = true;
        }

        private byte[] HexStringToByteArray(string hex)
        {
            hex = hex.Replace(" ", "").Replace("-", ""); // ���ݷָ���
            if (hex.Length % 2 != 0) throw new FormatException("HEX�ַ������ȱ���Ϊż��");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                string byteStr = hex.Substring(i, 2);
                if (!byte.TryParse(byteStr, NumberStyles.HexNumber, null, out bytes[i / 2]))
                    throw new FormatException($"�Ƿ�HEX�ַ���{byteStr}");
            }
            return bytes;
        }
        #region �����������
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSend.Text))
            {
                MessageBox.Show("�������ݲ���Ϊ�գ�");
                return;
            }

            if (!sendCom.IsOpen)
            {
                MessageBox.Show("���ȴ򿪴��ڣ�");
                return;
            }

            try
            {
                byte[] sendData;

                if (chkUse.Checked) // ʹ��Э���װ
                {
                    // ��ȡԭʼ����
                    byte[] payload = rdoHEX.Checked ?
                        HexStringToByteArray(txtSend.Text) :
                        Encoding.ASCII.GetBytes(txtSend.Text);

                    // �Զ��ضϳ���
                    int maxLength = 65535;
                    if (int.TryParse(txtDatLength.Text, out int userLength))
                        maxLength = Math.Min(userLength, 65535);

                    if (payload.Length > maxLength)
                        Array.Resize(ref payload, maxLength);

                    // ��װ���ݰ�
                    sendData = BuildPacket(payload);
                }
                else // ��ͨģʽ
                {
                    sendData = rdoHEX.Checked ?
                        HexStringToByteArray(txtSend.Text) :
                        Encoding.ASCII.GetBytes(txtSend.Text);
                }

                sendCom.Write(sendData, 0, sendData.Length);
                sendNum += sendData.Length;
                lblSend.Text = $"�������ݣ�{sendNum} �ֽ�";
            }
            catch (FormatException ex)
            {
                MessageBox.Show($"HEX��ʽ����{ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"����ʧ�ܣ�{ex.Message}");
            }
        }
        #endregion

        private void button2_Click(object sender, EventArgs e)
        {
            _textContainer.Clear(); // ʹ���������������
            recNum = 0;      // ���ý���ͳ��
            lblReceived.Text = "�������ݣ�0 �ֽ�";
        }

        private void btnFileOpen_Click(object sender, EventArgs e)
        {
            try
            {
                if (dlgOpenFile.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // ʹ��StreamReader��ȡ�ı���ʽECG�ļ�
                        using (var reader = new StreamReader(dlgOpenFile.FileName, Encoding.UTF8))
                        {
                            StringBuilder sb = new StringBuilder();
                            Buffer = File.ReadAllBytes(dlgOpenFile.FileName);

                            txtSend.Tag = dlgOpenFile.FileName; // �����ļ�·��������ʹ��
                            autoTransText.Text = $"{Path.GetFileName(dlgOpenFile.FileName)}";
                            MessageBox.Show($"�Ѽ����ļ���\n{dlgOpenFile.FileName}", "�ļ�ѡ��ɹ�", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        MessageBox.Show("û���ļ�����Ȩ�ޣ�",
                                      "Ȩ�޴���",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Warning);
                    }
                    catch (IOException ioEx)
                    {
                        MessageBox.Show($"�ļ���ȡ����{ioEx.Message}",
                                      "IO����",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�ļ�����ʧ�ܣ�{ex.Message}",
                              "����",
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

                // ��̬���㷢����
                TimeSpan elapsed = DateTime.Now - lastSendTime;
                int targetBytes = (int)(TargetBps * elapsed.TotalSeconds);

                // ����ÿ�η�����
                targetBytes = Math.Min(targetBytes, 4096);
                targetBytes = Math.Min(targetBytes, Buffer.Length - intSendPosData);
                if (targetBytes <= 0)
                {
                    if (intSendPosData >= Buffer.Length)
                    {
                        // ��ɷ���ʱ����UI
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            chkAuto.Checked = false;
                            MessageBox.Show("�ļ�������ɣ�");
                        });
                    }
                    return;
                }
                // ʵ�ʷ��Ͳ���
                sendCom.Write(Buffer, intSendPosData, targetBytes);
                intSendPosData += targetBytes;
                sendNum += targetBytes;
                lastSendTime = DateTime.Now;

                // ���·�����ʾ�����̰߳�ȫ��
                this.BeginInvoke((MethodInvoker)delegate
                {
                    lblSend.Text = $"�������ݣ�{sendNum} �ֽ�";
                });
            }
            catch (Exception ex)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    chkAuto.Checked = false;
                    MessageBox.Show($"����ʧ�ܣ�{ex.Message}");
                });
            }
        }
        private void chkAuto_CheckedChanged(object sender, EventArgs e)
        {
            // ȷ��ֻ���û�����ʱ��Ӧ
            if (chkAuto.Checked)
            {
                if (!sendCom.IsOpen)
                {
                    chkAuto.Checked = false; // ��Ҫ��ʽȡ��ѡ��
                    MessageBox.Show("���ȴ򿪴��ڣ�");
                    return;
                }

                if (Buffer == null || Buffer.Length == 0)
                {
                    chkAuto.Checked = false;
                    MessageBox.Show("���ȼ���Ҫ���͵��ļ���");
                    return;
                }

                // ����״̬
                intSendPosData = 0;
                sendNum = 0;
                lastSendTime = DateTime.Now;

                // ���ò�������ʱ��
                timerSend.Interval = 50; // ȷ����ʱ�����������ȷ
                timerSend.Enabled = true;
                timerSend.Start(); // ��ʽ������ʱ��
            }
            else
            {
                timerSend.Enabled = false;
                timerSend.Stop(); // ��ʽֹͣ��ʱ��
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {

        }

        private void TextRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (displayQueue.IsEmpty) return;

            // ÿ��ˢ����ദ��100��
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

                // ֻ�е�ǰ������������ʱ������
                if (txtRec.Lines.Length > MaxDisplayLines * 1.2)
                {
                    var lines = txtRec.Lines;
                    int keepCount = Math.Min(MaxDisplayLines, lines.Length);
                    var newLines = lines.Skip(lines.Length - keepCount).ToArray();
                    txtRec.Lines = newLines;
                    totalLinesAdded = keepCount;
                }

                // �Զ��������ײ�
                txtRec.SelectionStart = txtRec.TextLength;
                txtRec.ScrollToCaret();
            }
        }
        private void timSum_Tick(object sender, EventArgs e)
        {
            // ����ͳ����ʾ
            lblSend.Text = $"�������ݣ�{sendNum} �ֽ�";
            lblReceived.Text = $"�������ݣ�{recNum} �ֽ�";

            // �������ݴ���
            if (recCom.IsOpen && recCom.BytesToRead > 0)
            {
                try
                {
                    byte[] buffer = new byte[recCom.BytesToRead];
                    int bytesRead = recCom.Read(buffer, 0, buffer.Length);
                    recNum += bytesRead;

                    // ����ģʽ��������
                    if (chkUse.Checked)
                    {
                        // ʹ��Э��ģʽ����������ӵ�������������
                        receiveBuffer.AddRange(buffer);
                        ProcessPackets();
                    }
                    else
                    {
                        // �����¼���ԭʼ���ݣ�
                        OnDataReceived?.Invoke(buffer);

                        // ��ͨģʽ��ֱ�Ӵ���ԭʼ����
                        string displayText = rdoHEX.Checked ?
                            BitConverter.ToString(buffer).Replace("-", " ") :
                            Encoding.ASCII.GetString(buffer);

                        // ʹ���ı���������ֱ�Ӹ���UI
                        _textContainer.AddText(displayText);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"���մ���{ex.Message}");
                }
            }
        }

        // ����ر�ʱ�ͷ���Դ
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _textContainer?.Dispose(); // �ͷ��ı�����
            // ֹͣ���ͷ��ı�ˢ�¼�ʱ��
            if (textRefreshTimer != null)
            {
                textRefreshTimer.Stop();
                textRefreshTimer.Dispose();
            }

            // �����ر��߼�...
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
            private const int MAX_CHARACTERS = 200 * 1024; // 200KB�ַ�������

            public TextContainer(TextBox textBox)
            {
                _textBox = textBox;
                _updateTimer = new System.Windows.Forms.Timer();
                _updateTimer.Interval = 200; // 200msˢ�¼��
                _updateTimer.Tick += UpdateTextBox;
                _updateTimer.Start();
            }

            public void AddText(string text)
            {
                _currentQueue.Enqueue(text);

                // �ַ���ͳ��
                Interlocked.Add(ref _totalCharacters, text.Length);
            }

            public void Clear()
            {
                // �����¶����滻�ɶ��У������շ�ʽ��
                var newQueue = new ConcurrentQueue<string>();
                Interlocked.Exchange(ref _currentQueue, newQueue);
                Interlocked.Exchange(ref _totalCharacters, 0);

                // UI�߳�����ı���
                _textBox.Invoke(new Action(() => _textBox.Clear()));
            }

            private void UpdateTextBox(object sender, EventArgs e)
            {
                if (_currentQueue.IsEmpty) return;

                // �滻Ϊ�¶��У�ԭ�Ӳ�����
                var processQueue = _currentQueue;
                _currentQueue = new ConcurrentQueue<string>();
                Interlocked.Exchange(ref _totalCharacters, 0);

                StringBuilder sb = new StringBuilder();
                string item;
                int lineCount = 0;

                // ��������е���Ŀ
                while (processQueue.TryDequeue(out item))
                {
                    sb.AppendLine(item);
                    lineCount++;

                    // ���������ƣ�һ����ദ��500��
                    if (lineCount >= 500) break;
                }

                if (sb.Length == 0) return;

                // ����UI����UI�߳�ִ�У�
                _textBox.Invoke(new Action(() => {
                    // ��Ч׷���ı�
                    _textBox.AppendText(sb.ToString());

                    // ��̬�����������ⰺ���Lines���ԣ�
                    var textLength = _textBox.TextLength;
                    if (textLength > MAX_CHARACTERS)
                    {
                        // ����ɾ��ǰ1/3���ݣ����ķ�����
                        int removeUpTo = textLength / 3;
                        int firstNewLine = _textBox.Text.IndexOf('\n', removeUpTo);

                        if (firstNewLine > removeUpTo)
                        {
                            _textBox.Select(0, firstNewLine + 1);
                            _textBox.SelectedText = "";
                        }
                    }

                    // �Զ�����
                    _textBox.SelectionStart = _textBox.TextLength;
                    _textBox.ScrollToCaret();
                }));
            }

            public void Dispose() => _updateTimer?.Stop();
        }
        #region ����
        private void rdoHEX_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoHEX.Checked)
                txtSend.Tag = "HEX"; // ��ǵ�ǰ����ģʽ
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
