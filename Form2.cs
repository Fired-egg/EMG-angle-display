using System;
using System.Collections.Concurrent;
using System.Windows.Forms;
using System.Linq;
using System.Threading;
using ScottPlot.WinForms;
using ScottPlot.Plottables;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Drawing;
using ScottPlot;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using MathWorks.MATLAB.NET.Arrays;
using Angle;
using System.Windows.Markup;
using MathNet.Filtering;
using MathNet.Filtering.IIR;
using MathNet.Filtering.Butterworth;
using MathNet.Numerics;
using System.Globalization;
using Filter;
using static Test0524.Form2.DoubleRingBuffer;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;

namespace Test0524
{
    public partial class Form2 : Form
    {
        #region 变量声明
        // 刷新计时器，定期更新显示
        private System.Windows.Forms.Timer updateTimer;
        // 文本更新定时器
        private System.Windows.Forms.Timer textUpdateTimer;
        // 文本更新定时器
        private System.Windows.Forms.Timer refreshTimer;
        //控制位，为1开始，为0暂停
        public int status = 0;
        // 数据缓存队列，线程安全（用于接收数据并缓冲）
        private ConcurrentQueue<byte[]> dataQueue = new ConcurrentQueue<byte[]>();
        // 在Form2类中添加（与现有数据队列并列）
        private ConcurrentQueue<byte[]> function2Queue = new ConcurrentQueue<byte[]>();

        // 文本缓冲区
        private ConcurrentQueue<string> textBuffer = new ConcurrentQueue<string>();
        private List<double>[] channelData = new List<double>[8];
        //读入数据的通道数,初始为8
        private int channelnumber = 8;
        //读入数据的解析格式,2字节或4字节
        private int bytenumber = 4;
        // 每个通道的数据存储（使用列表）
        private List<double>[] channelDataLists = new List<double>[8];
        // ScottPlot 控件数组
        private FormsPlot[] plotControls;
        //label 控件数组
        private System.Windows.Forms.Label[] Mylabels;
        // 数据记录器数组（DataLogger）
        private DataLogger[] dataLoggers;
        // 数据计数器
        private int[] dataCounters = new int[8];
        // 当前显示的数据范围
        private int currentDisplayIndex = 0;
        // 最大显示点数
        private const int maxPointsPerChannel = 3000;
        private bool isStaticDataMode = false; // 标记当前是否为静态数据模式
        private double[,] loadedData; // 存储加载的完整数据

        // 添加数据锁（每个通道单独锁）
        private readonly object[] dataLocks = new object[8];
        private long totalFramesProcessed = 0;
        private bool isApplicationClosing = false;

        // 在类中添加以下成员变量
        private bool isRecording = false;
        private List<double>[] recordedChannelData = new List<double>[9]; // 8个主通道 + 通道9
        private Stopwatch recordingTimer = new Stopwatch();


        // 通道8数据存储（用于功能码0x02）
        private List<double> channel8Data = new List<double>();
        private DataLogger dataLogger9;
        private const int maxPointsChannel9 = 5000;

        // 每个通道的数据数组（环形缓冲区）
        private double[][] channelArrays;
        private int[] arrayHeads; // 每个数组的当前写入位置

        // 信号图对象
        private Signal[] channelSignals;
        private Signal channel9Signal;

        // 通道9的数据数组
        private double[] channel9Array;
        private int channel9Head;

        // 在每个通道的计数器下面添加总数据点数计数器
        private long[] totalDataCounts = new long[8]; // 记录每个通道的总数据点数
        private long totalChannel9Count = 0; // 通道9的总数据点数

        // 在类中添加以下成员变量
        private List<double> jointAngles = new List<double>(); // 存储关节角度值
        private double currentAngle = double.NaN; // 当前关节角度
        private bool _isCalibrated = false; // 校准状态标志

        // 添加类成员变量
        private Queue<double[]> function2DataBuffer = new Queue<double[]>(); // 存储功能2的完整数据帧
        private const int function2FrameSize = 18; // 每帧18个double数据

        // 添加类成员变量
        private byte[] incompleteFrameBuffer = null; // 通用帧缓冲区
        private byte[] incompleteFunc2Frame = null; // 专门用于功能2的帧缓冲区

        // 添加记录相关的成员变量
        private DateTime recordingStartTime;
        private int recordedPointCount;
        private readonly object recordLock = new object();

        // 添加用于取消异步计算的标记
        private CancellationTokenSource calculationCancellation = new CancellationTokenSource();
        //算法
        private angleClass Knee;
        // 滤波器
        private MovingAverageFilter[] movingAverageFilters;
        private FastNotch50Hz[] notchFilters50Hz;
        private SimpleButterworthBandpass[] bandpassFilters;

        // 确保在类级别声明这些变量
        private FileStream recordStream;
        private StreamWriter recordWriterEMG; // 肌电信号记录文件
        private StreamWriter recordWriterAngle; // 关节角度记录文件
        private int angleFrameCounter = 0; // 关节角度帧计数器

        // 确保有通道8的环形缓冲区
        private DoubleRingBuffer ringBufferChannel8;
        // 在类中新增计数器
        private int frameCounter = 0;
        //生物力学参数
        private double[] rmsValues = new double[4];
        private double[] iemgValues = new double[4];
        private double[] medianFreqValues = new double[4];
        private System.Windows.Forms.Timer featureUpdateTimer;

        #endregion

        public Form2()
        {
            InitializeComponent();
            for (int i = 0; i < 8; i++)
            {
                dataLocks[i] = new object();
            }

            InitializePlots();
            InitializePlot9();
            InitializeSignalPlots();
            InitializeMatlab();
            InitializeFilters();


            /// 初始化数据处理定时器 (高频率处理接收到的数据)
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 10; // 100 Hz (高频处理)
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            // 初始化图表刷新定时器 (中频率刷新图表)
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 100; // 10Hz
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();

            // 初始化文本更新定时器 (低频率更新文本)
            textUpdateTimer = new System.Windows.Forms.Timer();
            textUpdateTimer.Interval = 200; // 5 Hz
            textUpdateTimer.Tick += TextUpdateTimer_Tick;
            textUpdateTimer.Start();

            // 初始化特征更新定时器
            featureUpdateTimer = new System.Windows.Forms.Timer();
            featureUpdateTimer.Interval = 200; // 200ms = 5Hz
            featureUpdateTimer.Tick += FeatureUpdateTimer_Tick;
            featureUpdateTimer.Start();

            rdoChl4.Checked = true;
            rdo4.Checked = true;


            // 添加全局异常处理
            Application.ThreadException += (s, e) => HandleFatalException(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                HandleFatalException(e.ExceptionObject as Exception);
            // 初始化记录数据结构
            for (int i = 0; i < 9; i++)
            {
                recordedChannelData[i] = new List<double>();
            }
        }
        #region 图像初始化
        private void InitializePlots()
        {
            // 初始化控件数组
            plotControls = new FormsPlot[8]
            {
        formsPlot1, formsPlot2, formsPlot3, formsPlot4,
        formsPlot5, formsPlot6, formsPlot7, formsPlot8
            };
            Mylabels = new System.Windows.Forms.Label[8]
            {
        label1,label2,label3,label4,label5,label6,label7,label8
            };

            // 初始化通道数据存储（我们自己维护）
            channelData = new List<double>[8];
            // 添加环形缓冲区初始化
            ringBufferChannel8 = new DoubleRingBuffer(maxPointsChannel9);

            for (int i = 0; i < 8; i++)
            {
                if (i == 0||i==2||i==4||i==6)
                {
                    plotControls[i].Plot.Title($"Chl{(i/2)+1}");
                }
                else
                {
                    plotControls[i].Plot.Title("mV");
                }
                    
                // 初始化数据存储（我们自己维护）
                channelData[i] = new List<double>();

                // 设置标题和标签
                plotControls[i].Plot.XLabel("t/ms");
                plotControls[i].Plot.YLabel("Amplitude/mV");

                // 设置网格
                plotControls[i].Plot.Grid.IsVisible = true;

                // 初始设置轴范围
                plotControls[i].Plot.Axes.SetLimitsX(0, maxPointsPerChannel);
                plotControls[i].Plot.Axes.SetLimitsY(-5.5, +5.5);
            }
        }
        // 初始化通道9图表
        private void InitializePlot9()
        {
            // 设置
            formsPlot9.Plot.XLabel("t/10ms");
            formsPlot9.Plot.YLabel("Angle/°)");
            formsPlot9.Plot.Grid.IsVisible = true;
            formsPlot9.Plot.Axes.SetLimitsX(0, maxPointsChannel9);
            formsPlot9.Plot.Axes.SetLimitsY(-180, 180); // 假设角度范围在-180到180度之间

            // 添加数据记录器
            dataLogger9 = formsPlot9.Plot.Add.DataLogger();
            dataLogger9.Color = Colors.Red;
            dataLogger9.LineWidth = 1;
            dataLogger9.ViewSlide();
        }
        
        #region 信号图初始化
        private void InitializeSignalPlots()
        {
            // 初始化通道0-7
            plotControls = new FormsPlot[8]
            {
        formsPlot1, formsPlot2, formsPlot3, formsPlot4,
        formsPlot5, formsPlot6, formsPlot7, formsPlot8
            };

            Mylabels = new System.Windows.Forms.Label[8]
            {
        label1,label2,label3,label4,label5,label6,label7,label8
            };

            channelArrays = new double[8][];
            // 确保 arrayHeads 数组正确初始化
            arrayHeads = new int[8];
            for (int i = 0; i < 8; i++)
            {
                arrayHeads[i] = 0; // 初始化每个通道的计数器为0
            }
            channelSignals = new Signal[8];

            for (int i = 0; i < 8; i++)
            {
                // 创建固定大小的数组
                channelArrays[i] = new double[maxPointsPerChannel * 2];
                arrayHeads[i] = 0;

                // 添加信号图 - 确保在创建Signal对象后设置颜色
                channelSignals[i] = plotControls[i].Plot.Add.Signal(channelArrays[i]);

                // 初始设置轴范围
                plotControls[i].Plot.Axes.SetLimitsX(0, maxPointsPerChannel);
                plotControls[i].Plot.Axes.SetLimitsY(-1000, 1000);
            }
            // 初始化总数据点数计数器
            totalDataCounts = new long[8];
            for (int i = 0; i < 8; i++)
            {
                totalDataCounts[i] = 0;
            }
            totalChannel9Count = 0;

            // 初始化通道9的信号图
            if (formsPlot9 != null && formsPlot9.Plot != null)
            {
                formsPlot9.Plot.XLabel("");
                formsPlot9.Plot.YLabel("");
                formsPlot9.Plot.Grid.IsVisible = true;
                formsPlot9.Plot.Axes.SetLimitsX(0, maxPointsChannel9);
                formsPlot9.Plot.Axes.SetLimitsY(-1000, 1000);

                channel9Array = new double[maxPointsChannel9 * 2];
                channel9Head = 0;
                channel9Signal = formsPlot9.Plot.Add.Signal(channel9Array);
                channel9Signal.Color = Colors.Red;
                channel9Signal.LineWidth = 1;
            }
        }
        #endregion
        #endregion
        #region MATLAB函数
        private void InitializeMatlab()
        {
            Knee = new angleClass();
        }
        #endregion
        #region 数据处理方法
        public void OnFunction2DataReceived(byte[] data)
        {
            if (isApplicationClosing || status == 0) return;

            try
            {
                if (data == null || data.Length == 0) return;

                // 不再直接处理关节数据，而是放入队列
                // 这样处理可以在后台线程完成，避免阻塞UI
                function2Queue.Enqueue(data);
                textBuffer.Enqueue($"[{DateTime.Now:HH:mm:ss.fff}] 接收功能02: {data.Length}字节\n");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"接收功能02数据出错: {ex.Message}");
            }
        }
        public void OnDataReceived(byte[] data)
        {
            if (isApplicationClosing || status == 0) return;
            try
            {
                if (data == null || data.Length == 0) return;

                dataQueue.Enqueue(data); // 仅加入dataQueue
                textBuffer.Enqueue($"[{DateTime.Now:HH:mm:ss.fff}] 接收到功能01数据: {data.Length} 字节\n");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"接收数据时出错: {ex.Message}");
            }
        }
        #endregion
        #region 数组添加
        // 添加数据到通道数组 (0-7)
        private void AddSampleToChannelArray(int channel, double value)
        {
            if (channel < 0 || channel >= 8) return;
            double[] array = channelArrays[channel];
            int head = arrayHeads[channel];

            array[head] = value; // 写入新值
            head++;

            // 更新信号图的显示范围
            channelSignals[channel].MaxRenderIndex = head - 1;
            // 增加总数据点数
            totalDataCounts[channel]++;
            // 如果数组满了，移动后半段到开头
            if (head == array.Length)
            {
                int middle = array.Length / 2;
                int lengthToMove = head - middle;
                Array.Copy(array, middle, array, 0, lengthToMove);
                head = lengthToMove;
                channelSignals[channel].MaxRenderIndex = head - 1;
            }

            arrayHeads[channel] = head;
            // 如果正在记录，添加到记录缓冲区
            if (isRecording && channel >= 0 && channel < 8)
            {
                recordedChannelData[channel].Add(value);
            }
        }

        // 添加数据到通道9数组
        private async Task AddSampleToChannel9ArrayAsync(double[] adcValues, CancellationToken token)
        {
            if (adcValues == null || adcValues.Length != 18)
                return;

            try
            {
                // 在后台线程执行MATLAB计算
                double angle = await Task.Run(() =>
                {
                    // 检查是否取消
                    token.ThrowIfCancellationRequested();

                    // 创建MATLAB输入数组
                    MWNumericArray matlabInput = new MWNumericArray(1, 18, adcValues);

                    // 调用DLL计算角度
                    MWNumericArray result = (MWNumericArray)Knee.Knee(matlabInput);
                    return result.ToScalarDouble();
                }, token);
                // 修改记录条件：每帧都记录
                if (isRecording && recordWriterAngle != null && !double.IsNaN(angle))
                {
                    lock (recordLock)
                    {
                        angleFrameCounter++;
                        recordWriterAngle.WriteLine($"{angleFrameCounter},{angle:F2}");

                        // 仅添加这行保证写入最新的角度值
                        currentAngle = angle;
                    }
                }
                // 确保得到有效的角度值
                if (!double.IsNaN(angle))
                {
                    // 将角度值添加到通道9的环形缓冲区
                    channel9Array[channel9Head] = angle;
                    channel9Head++;
                    channel9Signal.MaxRenderIndex = channel9Head - 1;
                    totalChannel9Count++;

                    // 处理环形缓冲区的移动（当缓冲区满时）
                    if (channel9Head == channel9Array.Length)
                    {
                        int middle = channel9Array.Length / 2;
                        int lengthToMove = channel9Head - middle;
                        Array.Copy(channel9Array, middle, channel9Array, 0, lengthToMove);
                        channel9Head = lengthToMove;
                        channel9Signal.MaxRenderIndex = channel9Head - 1;
                    }

                    // 更新标签显示当前角度（确保在UI线程执行）
                    this.BeginInvoke(new Action(() =>
                    {
                        lblAngle.Text = $"{angle:F2}°"; // 直接显示最新角度
                    }));
                    // 添加到环形缓冲区
                    ringBufferChannel8.Add(angle);
                    
                }
                else
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        lblAngle.Text = "请等待";
                    }));
                }
            }
            catch (OperationCanceledException)
            {
                // 计算被取消，不做处理
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"计算关节角度失败: {ex.Message}");

                // 在界面上显示错误信息
                this.BeginInvoke(new Action(() =>
                {
                    lblAngle.Text = "计算错误";
                }));
            }
        }

        
        #endregion
        #region 异常处理
        private void HandleFatalException(Exception ex)
        {
            if (ex == null) return;

            try
            {
                string log = $"[{DateTime.Now}] 致命错误:\n{ex.GetType().Name}\n" +
                            $"消息: {ex.Message}\n堆栈:\n{ex.StackTrace}\n\n";
                File.AppendAllText("crash_log.txt", log);

                MessageBox.Show($"程序发生致命错误: {ex.Message}\n详细错误信息已保存到crash_log.txt",
                              "系统错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isApplicationClosing = true;
                this.Close();
            }
        }
        #endregion
        #region 滤波器初始化
        private void InitializeFilters()
        {
            double fs = 1000; // 采样率

            movingAverageFilters = new MovingAverageFilter[8];
            notchFilters50Hz = new FastNotch50Hz[8];      // 改名50Hz滤波器
            bandpassFilters = new SimpleButterworthBandpass[8];

            for (int i = 0; i < 8; i++)
            {
                movingAverageFilters[i] = new MovingAverageFilter(5);
                notchFilters50Hz[i] = new FastNotch50Hz(fs);    // 50Hz陷波
                bandpassFilters[i] = new SimpleButterworthBandpass(fs, 260, 480);
            }
        }

        #endregion
        private void UpdatePlotVisibility()
        {
            for (int i = 0; i < 8; i++)
            {
                if (channelnumber == 8)
                {
                    // 8通道模式：全部显示
                    plotControls[i].Visible = true;
                }
                else if (channelnumber == 4)
                {
                    // 4通道模式：仅显示1、3、5、7号图（索引0、2、4、6）
                    plotControls[i].Visible = (i == 0 || i == 2 || i == 4 || i == 6);
                }
            }
        }

        // 主更新定时器处理
        private async void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (isApplicationClosing || status == 0) return;
            try
            {
                if (status == 0) return;

                int processed = 0;
                const int maxProcess = 100; // 每次最多处理1000帧
                int pointSize = bytenumber; // 每个数据点的字节数
                int frameSize = channelnumber * pointSize; // 每帧字节数

                while (processed < maxProcess && dataQueue.TryDequeue(out byte[] data))
                {
                    try
                    {
                        // 临时缓冲区处理不完整帧
                        if (incompleteFrameBuffer != null)
                        {
                            byte[] combined = new byte[incompleteFrameBuffer.Length + data.Length];
                            Buffer.BlockCopy(incompleteFrameBuffer, 0, combined, 0, incompleteFrameBuffer.Length);
                            Buffer.BlockCopy(data, 0, combined, incompleteFrameBuffer.Length, data.Length);
                            data = combined;
                            incompleteFrameBuffer = null;
                        }
                        // 处理完整帧
                        int frameCount = data.Length / frameSize;
                        int remainingBytes = data.Length % frameSize;
                        // 存储不完整的帧
                        if (remainingBytes > 0)
                        {
                            incompleteFrameBuffer = new byte[remainingBytes];
                            Buffer.BlockCopy(data, data.Length - remainingBytes,
                                            incompleteFrameBuffer, 0, remainingBytes);
                            data = data.Take(data.Length - remainingBytes).ToArray();
                            frameCount = data.Length / frameSize;
                        }
                        if (frameSize == 0 || data.Length < frameSize) continue;
                        for (int frame = 0; frame < frameCount; frame++)
                        {
                            // 创建帧数据数组（用于记录）
                            double?[] frameData = new double?[8];
                            // 处理每个通道的数据
                            for (int chIndex = 0; chIndex < channelnumber; chIndex++)
                            {
                                // 计算数据位置
                                int byteIndex = frame * frameSize + chIndex * pointSize;

                                // 确保不会超出范围
                                if (byteIndex + pointSize > data.Length) break;

                                // 解析数据
                                double rawValue = ParseSampleBigEndian(data, byteIndex, bytenumber);
                                double transformedValue = rawValue * 5.0*1000 / (16777216.0); // 16777216 = 2^24 24是放大倍数
                                // 根据通道模式计算正确索引
                                int displayChannel;
                                if (channelnumber == 8)
                                {
                                    // 8通道：直接分配所有通道
                                    displayChannel = chIndex;
                                }
                                else // 4通道模式
                                {
                                    // 为可见控件0,2,4,6分配数据
                                    // 公式：0->0, 1->2, 2->4, 3->6
                                    displayChannel = chIndex * 2;
                                }
                                double filteredValue = ApplyFullFiltering(displayChannel, transformedValue);
                                // 存储帧数据
                                frameData[displayChannel] = filteredValue;
                                // 安全边界检查
                                if (displayChannel < 0 || displayChannel >= 8) continue;
                                AddSampleToChannelArray(displayChannel, filteredValue);
                                // 获取最新的关节角度值（如果有）
                                double? ch9Value = ringBufferChannel8.Count > 0 ?
                                    ringBufferChannel8.GetLastValue() : (double?)null;
                            }
                            
                            frameCounter++; // 增加帧计数器

                            if (isRecording)
                            {
                                RecordDataFrame(frameCounter, frameData);
                            }
                            processed++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"数据处理错误: {ex.Message}");
                    }
                }
                // 处理功能码0x02的数据
                while (processed < maxProcess && function2Queue.TryDequeue(out byte[] func2Data))
                {
                    try
                    {
                        // 检查数据长度：每帧36字节（18个double值，每个值2字节）
                        if (func2Data.Length % 36 != 0)
                        {
                            // 处理不完整帧
                            HandleIncompleteFrame(func2Data, 36);
                            continue;
                        }

                        // 处理所有完整的36字节帧
                        int frameCount = func2Data.Length / 36;
                        for (int frame = 0; frame < frameCount; frame++)
                        {
                            double[] adcValues = new double[18];
                            int offset = frame * 36;

                            for (int i = 0; i < 18; i++)
                            {
                                int byteIndex = offset + i * 2;
                                double value = ParseSampleBigEndian(func2Data, byteIndex, 2);
                                adcValues[i] = value;
                            }

                            // 异步处理这18个值
                            await AddSampleToChannel9ArrayAsync(adcValues, calculationCancellation.Token);
                        }

                        processed++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"关节数据处理错误: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"数据更新定时器错误: {ex.Message}");
            }

        }
        private void FeatureUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (isApplicationClosing) return;

            // 计算各通道特征值
            CalculateChannelFeatures(0, 0); // 通道1 -> 显示通道0 
            CalculateChannelFeatures(2, 1); // 通道2 -> 显示通道1
            CalculateChannelFeatures(4, 2); // 通道3 -> 显示通道2
            CalculateChannelFeatures(6, 3); // 通道4 -> 显示通道3

            // 更新UI
            this.BeginInvoke(new Action(() =>
            {
                // 通道1: Label10-12
                label10.Text = $"RMS: {rmsValues[0]:F4}";
                label11.Text = $"iEMG: {iemgValues[0]:F0}";
                label12.Text = $"MF: {medianFreqValues[0]:F2} Hz";

                // 通道2: Label14-16
                label14.Text = $"RMS: {rmsValues[1]:F4}";
                label15.Text = $"iEMG: {iemgValues[1]:F0}";
                label16.Text = $"MF: {medianFreqValues[1]:F2} Hz";

                // 通道3: Label18-20
                label18.Text = $"RMS: {rmsValues[2]:F4}";
                label19.Text = $"iEMG: {iemgValues[2]:F0}";
                label20.Text = $"MF: {medianFreqValues[2]:F2} Hz";

                // 通道4: Label22-24
                label22.Text = $"RMS: {rmsValues[3]:F4}";
                label23.Text = $"iEMG: {iemgValues[3]:F0}";
                label24.Text = $"MF: {medianFreqValues[3]:F2} Hz";
            }));
        }
        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (isApplicationClosing) return;
            try
            {
                // 静态数据模式不需要定时刷新
                if (isStaticDataMode || status == 0) return;
                if (status == 0) return;

                bool anyUpdate = false;

                // 更新每个通道的图表
                for (int controlIndex = 0; controlIndex < plotControls.Length; controlIndex++)
                {
                    // 跳过不可见的控件
                    if (!plotControls[controlIndex].Visible) continue;

                    // 获取与此视图对应的数据索引
                    // 控件索引直接对应数据索引（0->0, 1->1, ...7->7）
                    int dataIndex = controlIndex;
                    int head = arrayHeads[dataIndex];
                    if (head <= 0) continue;

                    // 标记需要更新
                    anyUpdate = true;

                    // 获取数据点数量（我们自己维护）
                    int dataCount = channelData[dataIndex].Count;

                    // 计算显示范围
                    double endX = head;
                    double startX = Math.Max(0, endX - maxPointsPerChannel);
                    plotControls[controlIndex].Plot.Axes.SetLimitsX(startX, endX);

                    // 计算Y轴范围（只计算显示范围内的数据）
                    double[] data = channelArrays[dataIndex];
                    double minY = double.MaxValue;
                    double maxY = double.MinValue;

                    int startIndex = (int)startX;
                    int endIndex = Math.Min(head, (int)endX);
                    try
                    {
                        for (int j = startIndex; j < endIndex; j++)
                        {
                            double value = data[j];
                            if (value < minY) minY = value;
                            if (value > maxY) maxY = value;
                        }

                        // 设置Y轴范围
                        if (minY != double.MaxValue)
                        {
                            if (minY == maxY)
                            {
                                minY -= 50;
                                maxY += 50;
                            }
                            double padding = Math.Max(1, (maxY - minY) * 0.1);
                            plotControls[controlIndex].Plot.Axes.SetLimitsY(minY - padding, maxY + padding);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"计算Y值错误: {ex.Message}");
                    }


                }

                if (channel9Head > 0)
                {
                    anyUpdate = true;

                    // 计算当前有效数据点数量
                    int validPointCount = channel9Head;

                    // 计算显示范围 (当前需要显示的起始和结束位置)
                    double endX = validPointCount;
                    double startX = Math.Max(0, endX - maxPointsChannel9);

                    // 设置X轴范围
                    formsPlot9.Plot.Axes.SetLimitsX(startX, endX);

                    // 计算Y轴范围 - 仅计算当前显示区域内的数据
                    double minY = double.MaxValue;
                    double maxY = double.MinValue;
                    bool foundValues = false;

                    // 计算当前显示区域的起始索引
                    int startIndex = (int)startX;
                    int endIndex = Math.Min(validPointCount, (int)endX);

                    try
                    {
                        for (int j = startIndex; j < endIndex; j++)
                        {
                            double value = channel9Array[j];
                            if (value < minY) minY = value;
                            if (value > maxY) maxY = value;
                            foundValues = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"计算通道9 Y值错误: {ex.Message}");
                    }

                    // 设置Y轴范围（如果找到有效值）
                    if (foundValues)
                    {
                        // 确保有合理的值范围
                        if (minY == maxY)
                        {
                            minY -= 0.1;
                            maxY += 0.1;
                        }

                        // 添加边距
                        double padding = Math.Max(0.01, (maxY - minY) * 0.05);
                        formsPlot9.Plot.Axes.SetLimitsY(minY - padding, maxY + padding);
                    }
                }
                // 只有有数据更新时才刷新图表
                if (anyUpdate)
                {
                    // 批量刷新所有可见的图表
                    for (int i = 0; i < plotControls.Length; i++)
                    {
                        if (plotControls[i].Visible)
                        {
                            plotControls[i].Refresh();
                        }
                    }
                    // 刷新通道9图表
                    if (channel9Head > 0)
                    {
                        formsPlot9.Refresh();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"刷新定时器错误: {ex.Message}");
            }

        }
        // 文本更新定时器处理
        private void TextUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (isApplicationClosing) return;
            try
            {
                const int maxLines = 500; // 最大显示行数

                if (textBuffer.IsEmpty) return;

                StringBuilder sb = new StringBuilder();
                int count = 0;

                // 批量取出缓冲区内容（每次最多处理50条）
                while (count < 50 && textBuffer.TryDequeue(out string text))
                {
                    sb.Append(text);
                    count++;
                }

                if (sb.Length > 0)
                {
                    // 使用BeginInvoke避免阻塞UI线程
                    BeginInvoke(new Action(() =>
                    {
                        // 追加新文本
                        txtRec.AppendText(sb.ToString());

                        // 检查并限制最大行数
                        if (txtRec.Lines.Length > maxLines * 1.2) // 只有超出20%缓冲时才处理
                        {
                            // 计算需要保留的行数
                            var lines = txtRec.Lines;
                            int startIndex = Math.Max(0, lines.Length - maxLines);
                            var newLines = lines.Skip(startIndex).ToArray();

                            // 保留最近500行
                            txtRec.Lines = newLines;

                        }
                        // 更新所有通道的总数据点数
                        for (int i = 0; i < 8; i++)
                        {
                            if (i < Mylabels.Length && Mylabels[i] != null)
                            {
                                // 显示总数据点数而不是环形缓冲区的当前计数
                                Mylabels[i].Text = totalDataCounts[i].ToString();
                            }
                        }
                        // 实时更新角度显示
                        if (totalChannel9Count > 0)
                        {
                            // 显示最新的关节角度值
                            double latestAngle = channel9Array[channel9Head - 1];
                            lblAngle.Text = $"{latestAngle:F2}°";
                        }
                        // 自动滚动到底部
                        txtRec.SelectionStart = txtRec.TextLength;
                        txtRec.ScrollToCaret();
                    }));
                }
                else // 即使没有新的文本数据，也要更新计数
                {
                    BeginInvoke(new Action(() =>
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            Mylabels[i].Text = arrayHeads[i].ToString();
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"文本更新定时器错误: {ex.Message}");
            }
            

        }
        // 新增方法：处理功能2数据

        private double ParseSampleBigEndian(byte[] data, int index, int byteCount)
        {
            ////大端
            //switch (byteCount)
            //{
            //    case 2: // 2字节大端
            //        return (short)((data[index] << 8) | data[index + 1]);

            //    case 4: // 4字节大端
            //        return (int)((data[index] << 24) |
            //                     (data[index + 1] << 16) |
            //                     (data[index + 2] << 8) |
            //                     data[index + 3]);

            //    default:
            //        throw new ArgumentException("不支持的字节数: " + byteCount);
            //}
            //小端
            switch (byteCount)
            {
                case 2: // 2字节大端
                    return (short)((data[index + 1] << 8) | data[index]);

                case 4: // 4字节大端
                    return (int)((data[index + 3] << 24) |
                                 (data[index + 2] << 16) |
                                 (data[index + 1] << 8) |
                                 data[index + 0]);

                default:
                    throw new ArgumentException("不支持的字节数: " + byteCount);
            }
        }
        
        private void btnStart_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "实时模式";
            TextChange();
        }


        #region 文本改变TextChange() 以及标志位设置
        // 修改TextChange方法，确保状态切换时清空队列
        private void TextChange()
        {
            if (btnStart.Text == "开始")
            {
                btnStart.Text = "暂停";
                status = 1;
            }
            else
            {
                btnStart.Text = "开始";
                status = 0;

                // 暂停时清空数据队列
                while (dataQueue.TryDequeue(out _)) { }
                while (function2Queue.TryDequeue(out _)) { }
                incompleteFrameBuffer = null;
                incompleteFunc2Frame = null;

                textBuffer.Enqueue($"[{DateTime.Now:HH:mm:ss.fff}] 已暂停接收数据\n");
            }
        }

        #endregion
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 首先停止所有计时器
            updateTimer.Stop();
            refreshTimer.Stop();
            textUpdateTimer.Stop();
            //memoryMonitorTimer.Stop();

            // 确保关闭记录器
            try
            {
                lock (recordLock)
                {
                    if (isRecording)
                    {
                        isRecording = false;
                        recordWriterEMG?.Flush();
                        recordWriterEMG?.Close();
                        recordWriterAngle?.Flush();
                        recordWriterAngle?.Close();
                    }
                    recordWriterEMG?.Dispose();
                    recordWriterEMG = null;
                    recordWriterAngle?.Dispose();
                    recordWriterAngle = null;
                }
                featureUpdateTimer?.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"关闭记录器时出错: {ex.Message}");
            }
            // 释放其他资源
            recordStream?.Dispose();
            recordStream = null;

            base.OnFormClosing(e);
        } 
        #region rdo改变控件尺寸及通道数
        private void rdoChl4_CheckedChanged(object sender, EventArgs e)
        {
            if (!rdoChl4.Checked) return;

            channelnumber = 4;

            // 显示图表1、3、5、7（视图通道0、2、4、6）
            formsPlot2.Visible = formsPlot4.Visible = formsPlot6.Visible = formsPlot8.Visible = false;
            formsPlot1.Visible = formsPlot3.Visible = formsPlot5.Visible = formsPlot7.Visible = true;

            // 调整大小
            formsPlot1.Size = formsPlot3.Size = formsPlot5.Size = formsPlot7.Size = new Size(1000, 300);
            UpdatePlotVisibility(); // 更新图表可见性
        }

        private void rdoChl8_CheckedChanged(object sender, EventArgs e)
        {
            if (!rdoChl8.Checked) return;

            channelnumber = 8;

            // 显示所有图表
            formsPlot1.Visible = formsPlot2.Visible = formsPlot3.Visible = formsPlot4.Visible =
                formsPlot5.Visible = formsPlot6.Visible = formsPlot7.Visible = formsPlot8.Visible = true;

            // 调整大小
            formsPlot1.Size = formsPlot2.Size = formsPlot3.Size = formsPlot4.Size =
                formsPlot5.Size = formsPlot6.Size = formsPlot7.Size = formsPlot8.Size = new Size(1000, 140);
            UpdatePlotVisibility(); // 更新图表可见性
        }

        private void rdo2_CheckedChanged(object sender, EventArgs e)
        {
            bytenumber = 2;
        }

        private void rdo4_CheckedChanged(object sender, EventArgs e)
        {
            bytenumber = 4;
        }
        #endregion
        #region 文件保存
        private void btnSave_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveDialogEMG = new SaveFileDialog())
            using (SaveFileDialog saveDialogAngle = new SaveFileDialog())
            {
                saveDialogEMG.Filter = "肌电信号文件|*.csv";
                saveDialogEMG.Title = "保存肌电信号数据";
                saveDialogEMG.DefaultExt = "csv";
                saveDialogEMG.AddExtension = true;

                saveDialogAngle.Filter = "关节角度文件|*.csv";
                saveDialogAngle.Title = "保存关节角度数据";
                saveDialogAngle.DefaultExt = "csv";
                saveDialogAngle.AddExtension = true;

                if (!isRecording)
                {
                    if (saveDialogEMG.ShowDialog() == DialogResult.OK &&
                        saveDialogAngle.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            // 创建肌电信号文件
                            recordWriterEMG = new StreamWriter(saveDialogEMG.FileName);
                            recordWriterEMG.WriteLine("帧数,Ch0,Ch1,Ch2,Ch3,Ch4,Ch5,Ch6,Ch7");

                            // 创建关节角度文件
                            recordWriterAngle = new StreamWriter(saveDialogAngle.FileName);
                            recordWriterAngle.WriteLine("帧数,关节角度");

                            isRecording = true;
                            angleFrameCounter = 0;
                            frameCounter = 0;
                            recordingStartTime = DateTime.Now;

                            btnSave.Text = "停止记录";
                            lblStatus.Text = $"记录中: 肌电->{Path.GetFileName(saveDialogEMG.FileName)}, 角度->{Path.GetFileName(saveDialogAngle.FileName)}";
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"创建记录文件失败: {ex.Message}", "错误",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);

                            // 确保关闭可能已经打开的文件
                            recordWriterEMG?.Dispose();
                            recordWriterAngle?.Dispose();
                            recordWriterEMG = null;
                            recordWriterAngle = null;

                            isRecording = false;
                            btnSave.Text = "开始记录";
                        }
                    }
                }
                else
                {
                    // 停止记录
                    try
                    {
                        isRecording = false;

                        lock (recordLock)
                        {
                            // 刷新并关闭肌电信号文件
                            recordWriterEMG?.Flush();
                            recordWriterEMG?.Close();
                            recordWriterEMG?.Dispose();
                            recordWriterEMG = null;

                            // 刷新并关闭关节角度文件
                            recordWriterAngle?.Flush();
                            recordWriterAngle?.Close();
                            recordWriterAngle?.Dispose();
                            recordWriterAngle = null;
                        }

                        btnSave.Text = "开始记录";

                        // 显示成功信息
                        if (!string.IsNullOrEmpty(saveDialogEMG.FileName) && File.Exists(saveDialogEMG.FileName) &&
                            !string.IsNullOrEmpty(saveDialogAngle.FileName) && File.Exists(saveDialogAngle.FileName))
                        {
                            FileInfo fiEMG = new FileInfo(saveDialogEMG.FileName);
                            FileInfo fiAngle = new FileInfo(saveDialogAngle.FileName);
                            MessageBox.Show($"记录完成!\n肌电文件: {saveDialogEMG.FileName}\n大小: {fiEMG.Length / 1024} KB\n" +
                                            $"角度文件: {saveDialogAngle.FileName}\n大小: {fiAngle.Length / 1024} KB",
                                            "记录状态",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Information);
                        }

                        lblStatus.Text = status == 1 ? "实时模式" : "暂停模式";
                        recordingStartTime = DateTime.MinValue;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"停止记录失败: {ex.Message}", "错误",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void RecordDataFrame(int frameIndex, double?[] frameData)
        {
            if (!isRecording || recordWriterEMG == null) return;

            lock (recordLock)
            {
                if (recordWriterEMG == null) return;

                recordWriterEMG.Write(frameIndex.ToString());

                for (int i = 0; i < 8; i++)
                {
                    recordWriterEMG.Write(",");
                    if (frameData[i].HasValue)
                    {
                        double value = frameData[i].Value;
                        if (double.IsNaN(value) || double.IsInfinity(value))
                        {
                            recordWriterEMG.Write("0"); // 无效值替换为0
                        }
                        else
                        {
                            recordWriterEMG.Write(value.ToString("F6"));
                        }
                    }
                    else
                    {
                        recordWriterEMG.Write("0"); // 无值写0
                    }
                }
                recordWriterEMG.WriteLine();

                // 每100帧刷新一次缓冲区
                if (frameIndex % 100 == 0)
                {
                    recordWriterEMG.Flush();
                }
            }
        }
        #endregion
        #region 文件读取
        private void btnLoad_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "数据文件|*.csv|所有文件|*.*";
                openDialog.Title = "加载数据文件";

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileType = DetectFileType(openDialog.FileName);

                    switch (fileType)
                    {
                        case "EMG":
                            LoadEMGData(openDialog.FileName);
                            break;
                        case "Angle":
                            LoadAngleData(openDialog.FileName);
                            break;
                        default:
                            MessageBox.Show("无法识别的文件格式");
                            break;
                    }
                }
            }
        }
        private string DetectFileType(string filePath)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    string header = reader.ReadLine();

                    if (header.Contains("Ch0") && header.Contains("Ch7"))
                        return "EMG";

                    if (header.Contains("关节角度"))
                        return "Angle";

                    // 增强检测：分析数据格式
                    string firstDataLine = reader.ReadLine();
                    if (!string.IsNullOrEmpty(firstDataLine))
                    {
                        string[] values = firstDataLine.Split(',');

                        // 肌电文件：至少9列（帧号+8通道）
                        if (values.Length >= 9) return "EMG";

                        // 角度文件：2列（帧号+角度值）
                        if (values.Length == 2) return "Angle";
                    }
                }
            }
            catch { }
            return "Unknown";
        }
        private void LoadEMGData(string filePath)
        {
            try
            {
                status = 0;
                updateTimer.Stop();
                refreshTimer.Stop();
                ClearAllData();

                List<double>[] channelDataList = new List<double>[8];
                for (int i = 0; i < 8; i++)
                    channelDataList[i] = new List<double>();

                int lineCount = 0;
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string header = reader.ReadLine();
                    // 只检查肌电信号通道
                    if (!header.Contains("Ch0") || !header.Contains("Ch7"))
                    {
                        MessageBox.Show("文件格式错误：不支持的肌电信号格式", "格式错误");
                        return;
                    }

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] values = line.Split(',');

                        // 帧数+8通道=9列
                        if (values.Length >= 9)
                        {
                            // 跳过帧号列(索引0)，读取8个通道
                            for (int i = 0; i < 8; i++)
                            {
                                // 更健壮的解析方式
                                if (double.TryParse(values[i + 1],
                                    NumberStyles.Float | NumberStyles.AllowThousands,
                                    CultureInfo.InvariantCulture,
                                    out double value))
                                {
                                    channelDataList[i].Add(value);
                                }
                                else
                                {
                                    // 解析失败时使用默认值
                                    channelDataList[i].Add(0);
                                    Debug.WriteLine($"解析失败: 行 {lineCount}, 通道 {i}, 值 '{values[i + 1]}'");
                                }
                            }
                            lineCount++;
                        }
                    }
                }

                if (lineCount > 0)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        foreach (double value in channelDataList[i])
                        {
                            AddSampleToChannelArray(i, value);
                        }
                        totalDataCounts[i] = channelDataList[i].Count;
                    }

                    CreateStaticPlots();
                    isStaticDataMode = true;

                    this.BeginInvoke(new Action(() =>
                    {
                        btnStart.Enabled = false;
                        lblStatus.Text = $"静态模式: 已加载 {lineCount} 肌电信号帧";
                        UpdateCountLabels();
                    }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载肌电信号失败: {ex.Message}", "错误",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadAngleData(string filePath)
        {
            try
            {
                // 仅清除关节角度数据
                Array.Clear(channel9Array, 0, channel9Array.Length);
                channel9Head = 0;
                totalChannel9Count = 0;
                jointAngles.Clear();

                List<double> angles = new List<double>();
                List<int> angleFrames = new List<int>();

                using (StreamReader reader = new StreamReader(filePath))
                {
                    string header = reader.ReadLine(); // 跳过标题行

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] values = line.Split(',');

                        if (values.Length >= 2 &&
                            int.TryParse(values[0], out int frame) &&
                            double.TryParse(values[1], out double angle))
                        {
                            angles.Add(angle);
                            angleFrames.Add(frame);
                        }
                    }
                }

                if (angles.Count == 0) return;

                // 计算最小帧数和最大帧数（用于归一化时间轴）
                int minFrame = angleFrames.Min();
                int maxFrame = angleFrames.Max();

                // 将帧数转换为时间（秒）或保持为相对帧数
                double frameRate = 100.0; // 关节角度采样率100Hz

                for (int i = 0; i < angles.Count; i++)
                {
                    // 方法1：直接使用帧数（相对时间）
                    // double time = angleFrames[i] / frameRate;

                    // 方法2：归一化为0开始的时间轴
                    double time = (angleFrames[i] - minFrame) / frameRate;

                    // 添加到显示缓存
                    if (channel9Head < channel9Array.Length)
                    {
                        channel9Array[channel9Head] = angles[i];
                        channel9Head++;
                        totalChannel9Count++;
                    }
                }

                // 更新图表
                formsPlot9.Plot.Clear();
                channel9Signal = formsPlot9.Plot.Add.Signal(channel9Array.Take(channel9Head).ToArray());
                channel9Signal.Color = Colors.Red;
                channel9Signal.LineWidth = 1;
                channel9Signal.MaxRenderIndex = channel9Head - 1;

                formsPlot9.Plot.Axes.AutoScale();
                formsPlot9.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载关节角度失败: {ex.Message}", "错误",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // 在Form2类中添加UI更新方法
        private void UpdateCountLabels()
        {
            for (int i = 0; i < 8; i++)
            {
                if (i < Mylabels.Length && Mylabels[i] != null)
                {
                    Mylabels[i].Text = totalDataCounts[i].ToString();
                }
            }
        }
        // 创建静态数据图表
        private void CreateStaticPlots()
        {
            // 清除所有现有图形
            for (int i = 0; i < 8; i++)
            {
                plotControls[i].Plot.Clear();

                // 添加静态信号图
                var sig = plotControls[i].Plot.Add.Signal(channelArrays[i]);
                sig.Color = Colors.Blue;
                sig.LineWidth = 1;
                sig.MaxRenderIndex = arrayHeads[i] - 1;

                // 设置网格
                plotControls[i].Plot.Grid.IsVisible = true;
                plotControls[i].Plot.Axes.AutoScale();
            }

            // 添加通道9静态图
            formsPlot9.Plot.Clear();
            var sig9 = formsPlot9.Plot.Add.Signal(channel9Array);
            sig9.Color = Colors.Red;
            sig9.LineWidth = 1;
            sig9.MaxRenderIndex = channel9Head - 1;
            formsPlot9.Plot.Grid.IsVisible = true;
            formsPlot9.Plot.Axes.AutoScale();

            // 刷新所有图表
            RefreshAllPlots();

            // 启用交互功能
            EnableDefaultInteractions();
        }
        // 启用默认交互功能（平移和缩放）
        private void EnableDefaultInteractions()
        {
            for (int i = 0; i < 8; i++)
            {
                // 添加双击重置功能
                plotControls[i].DoubleClick += (s, e) =>
                {
                    plotControls[i].Plot.Axes.AutoScale();
                    plotControls[i].Refresh();
                };

                // 添加右键菜单重置功能
                ContextMenuStrip ctxMenu = new ContextMenuStrip();
                var resetItem = new ToolStripMenuItem("重置视图");
                resetItem.Click += (s, e) =>
                {
                    plotControls[i].Plot.Axes.AutoScale();
                    plotControls[i].Refresh();
                };
                ctxMenu.Items.Add(resetItem);
                plotControls[i].ContextMenuStrip = ctxMenu;
            }
        }
        // 清除所有数据
        private void ClearAllData()
        {
            // 取消所有正在进行的计算
            calculationCancellation.Cancel();
            calculationCancellation = new CancellationTokenSource(); // 重置令牌
            for (int i = 0; i < 8; i++)
            {
                Array.Clear(channelArrays[i], 0, channelArrays[i].Length);
                arrayHeads[i] = 0; // 重置计数器为0
                channelSignals[i].MaxRenderIndex = -1;
                totalDataCounts[i] = 0; // 重置总计数器
            }
            // 通道9清除
            Array.Clear(channel9Array, 0, channel9Array.Length);
            channel9Head = 0; // 重置写入位置
            channel9Signal.MaxRenderIndex = -1; // 重置渲染范围
            totalChannel9Count = 0;
            // 重置关节角度相关数据
            jointAngles.Clear();
            currentAngle = double.NaN;
            _isCalibrated = false;
            // 添加以下刷新操作
            formsPlot9.Refresh();
            // 清除文本区域
            txtRec.Clear();

            // 清空数据队列
            while (dataQueue.TryDequeue(out _)) { }
            while (textBuffer.TryDequeue(out _)) { }

            // 重置状态
            isStaticDataMode = false;
        }
        #endregion

        // 添加恢复实时采集的功能
        private void btnResume_Click(object sender, EventArgs e)
        {
            // 无论当前是什么状态，都执行以下操作：
            // 1. 停止数据接收处理
            status = 0;

            // 2. 清除所有数据
            ClearAllData();

            // 3. 重置图表
            ResetPlots();

            // 4. 更新UI状态
            isStaticDataMode = false;
            btnStart.Enabled = true;
            btnStart.Text = "开始"; // 确保按钮文本重置
            lblStatus.Text = "实时模式";

            // 5. 重启定时器
            updateTimer.Start();
            refreshTimer.Start();

            // 6. 刷新所有图表
            RefreshAllPlots();
        }
        private void ResetPlots()
        {
            for (int i = 0; i < 8; i++)
            {
                // 仅清除并重置图表范围
                plotControls[i].Plot.Clear();
                channelSignals[i] = plotControls[i].Plot.Add.Signal(channelArrays[i]);
                channelSignals[i].Color = Colors.Blue;
                channelSignals[i].LineWidth = 1;
                plotControls[i].Plot.Axes.SetLimitsX(0, maxPointsPerChannel);
                plotControls[i].Plot.Axes.SetLimitsY(-1000, 1000);
            }

            // 通道9重置
            formsPlot9.Plot.Clear(); // 清除所有图形

            // 创建新的空信号图（而非复用旧数组）
            double[] newChannel9Array = new double[maxPointsChannel9 * 2];
            channel9Array = newChannel9Array;
            channel9Head = 0;

            channel9Signal = formsPlot9.Plot.Add.Signal(channel9Array);
            channel9Signal.Color = Colors.Red;
            channel9Signal.LineWidth = 1;
            channel9Signal.MaxRenderIndex = -1; // 初始无数据

            formsPlot9.Plot.Axes.SetLimitsX(0, maxPointsChannel9);
            formsPlot9.Plot.Axes.SetLimitsY(-1000, 1000);

            formsPlot9.Refresh(); // 强制刷新
        }
        // 刷新所有图表
        private void RefreshAllPlots()
        {
            // 批量刷新所有可见的图表
            for (int i = 0; i < plotControls.Length; i++)
            {
                if (plotControls[i].Visible)
                {
                    plotControls[i].Refresh();
                }
            }
        }
        private void HandleIncompleteFrame(byte[] data, int frameSize)
        {
            // 如果已有不完整帧缓冲区，合并数据
            if (incompleteFunc2Frame != null)
            {
                byte[] combined = new byte[incompleteFunc2Frame.Length + data.Length];
                Buffer.BlockCopy(incompleteFunc2Frame, 0, combined, 0, incompleteFunc2Frame.Length);
                Buffer.BlockCopy(data, 0, combined, incompleteFunc2Frame.Length, data.Length);
                data = combined;
                incompleteFunc2Frame = null;
            }

            // 检查是否能组成完整帧
            if (data.Length >= frameSize)
            {
                int completeFrames = data.Length / frameSize;
                int remaining = data.Length % frameSize;

                // 处理完整帧
                for (int i = 0; i < completeFrames; i++)
                {
                    byte[] frameData = new byte[frameSize];
                    Buffer.BlockCopy(data, i * frameSize, frameData, 0, frameSize);
                    function2Queue.Enqueue(frameData); // 重新加入队列
                }

                // 保存剩余的不完整数据
                if (remaining > 0)
                {
                    incompleteFunc2Frame = new byte[remaining];
                    Buffer.BlockCopy(data, data.Length - remaining, incompleteFunc2Frame, 0, remaining);
                }
            }
            else
            {
                // 保存不完整数据
                incompleteFunc2Frame = data;
            }
        }

        #region 高效的滤波实现

        // 高性能的移动平均滤波器（滑动窗口平均）
        public class MovingAverageFilter
        {
            private double[] _window;
            private double _sum;
            private int _index;
            private int _count;

            public int WindowSize => _window.Length;

            public MovingAverageFilter(int windowSize)
            {
                _window = new double[windowSize];
                _index = 0;
                _count = 0;
                _sum = 0;
            }

            public double Process(double input)
            {
                _sum = _sum - _window[_index] + input;
                _window[_index] = input;
                _index = (_index + 1) % _window.Length;

                if (_count < _window.Length)
                    _count++;

                return _sum / _count;
            }
        }

        // 高性能的带通滤波器（二阶巴特沃斯）
        public class SimpleButterworthBandpass
        {
            // 二阶带通滤波系数
            private double a0, a1, a2;
            private double b0, b1, b2;

            // 状态变量
            private double x1, x2, y1, y2;

            public SimpleButterworthBandpass(double fs, double centerFreq, double bandwidth)
            {
                double w0 = 2 * Math.PI * centerFreq / fs;
                double bw = bandwidth / fs;
                double Q = w0 / bw;

                double alpha = Math.Sin(w0) / (2 * Q);

                b0 = alpha;
                b1 = 0;
                b2 = -alpha;
                a0 = 1 + alpha;
                a1 = -2 * Math.Cos(w0);
                a2 = 1 - alpha;
            }

            public double Process(double input)
            {
                double output = (b0 * input + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2) / a0;

                // 更新状态
                x2 = x1;
                x1 = input;
                y2 = y1;
                y1 = output;

                return output;
            }
        }

        // 高效的50Hz陷波滤波器（固定频率，避免复杂计算）
        public class FastNotch50Hz
        {
            // 二阶陷波系数
            private double a0, a1, a2;
            private double b0, b1, b2;

            // 状态变量
            private double x1, x2, y1, y2;

            public FastNotch50Hz(double fs)
            {
                double f0 = 50.0; // 50Hz工频干扰
                double w0 = 2 * Math.PI * f0 / fs;
                double Q = 100; // 高品质因数

                // 固定系数（避免运行时计算）
                double alpha = Math.Sin(w0) / (2 * Q);
                double cosw0 = Math.Cos(w0);

                // 陷波滤波器系数
                b0 = 1;
                b1 = -2 * cosw0;
                b2 = 1;
                a0 = 1 + alpha;
                a1 = -2 * cosw0;
                a2 = 1 - alpha;
            }

            public double Process(double input)
            {
                double output = (b0 * input + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2) / a0;

                // 更新状态
                x2 = x1;
                x1 = input;
                y2 = y1;
                y1 = output;

                return output;
            }
        }
        
        #endregion
        private double ApplyFullFiltering(int channel, double sample)
        {
            if (channel < 0 || channel >= 8) return sample;

            // 滤波顺序：50Hz陷波 → 100Hz陷波 → 带通 → 移动平均
            double filtered = sample;
            filtered = notchFilters50Hz[channel].Process(filtered);   // 50Hz陷波
            filtered = bandpassFilters[channel].Process(filtered);         // 带通滤波
            filtered = movingAverageFilters[channel].Process(filtered);        // 移动平均
            return filtered;
        }
        #region 生物力学计算
        private void CalculateChannelFeatures(int channelIndex, int displayChannel)
        {
            if (channelIndex < 0 || channelIndex >= 8) return;

            try
            {
                // 获取最后200个点（如果可用）
                int pointCount = Math.Min(200, arrayHeads[channelIndex]);
                if (pointCount == 0) return;

                double[] data = new double[pointCount];

                // 从环形缓冲区获取数据
                int head = arrayHeads[channelIndex];
                for (int i = 0; i < pointCount; i++)
                {
                    int index = (head - pointCount + i + channelArrays[channelIndex].Length) % channelArrays[channelIndex].Length;
                    data[i] = channelArrays[channelIndex][index];
                }

                // 计算RMS（均方根值）
                double sumSquares = 0;
                for (int i = 0; i < pointCount; i++)
                {
                    sumSquares += data[i] * data[i];
                }
                rmsValues[displayChannel] = Math.Sqrt(sumSquares / pointCount);

                // 计算iEMG（积分肌电值）
                double sumAbsolute = 0;
                for (int i = 0; i < pointCount; i++)
                {
                    sumAbsolute += Math.Abs(data[i]);
                }
                iemgValues[displayChannel] = sumAbsolute;

                // 计算中值频率（采样率1000 Hz）
                medianFreqValues[displayChannel] = CalculateMedianFrequency(data, 1000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"通道{channelIndex}特征计算错误: {ex.Message}");
            }
        }
        private double CalculateMedianFrequency(double[] data, double sampleRate)
        {
            // 生成汉宁窗
            double[] window = Window.Hann(data.Length);

            // 将数据与窗函数相乘
            double[] windowed = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                windowed[i] = data[i] * window[i];
            }

            // 创建复数数组进行FFT
            var complexData = new Complex32[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                complexData[i] = new Complex32((float)windowed[i], 0);
            }

            // 执行FFT
            Fourier.Forward(complexData, FourierOptions.Default);

            // 计算功率谱（取前一半）
            int halfLength = data.Length / 2;
            double[] powerSpectrum = new double[halfLength];
            for (int i = 0; i < halfLength; i++)
            {
                powerSpectrum[i] = Math.Pow(complexData[i].Magnitude, 2);
            }

            // 计算总功率和累积功率
            double totalPower = powerSpectrum.Sum();
            double cumulativePower = 0;

            // 找到累积功率达到50%的频率点
            for (int i = 0; i < halfLength; i++)
            {
                cumulativePower += powerSpectrum[i];
                if (cumulativePower >= totalPower * 0.5)
                {
                    return i * (sampleRate / 2) / halfLength;
                }
            }

            return 0;
        }
        #endregion
        public class DoubleRingBuffer
        {
            private readonly double[] _buffer;
            private int _count;
            private int _startIndex;
            private readonly object _lock = new object();
            private double? _cachedMin = null;
            private double? _cachedMax = null;

            public int Capacity { get; }
            public int Count => _count;

            public DoubleRingBuffer(int capacity)
            {
                Capacity = capacity > 0 ? capacity : 1;
                _buffer = new double[Capacity];
            }

            public void Add(double item)
            {
                lock (_lock)
                {
                    if (_count < Capacity)
                    {
                        _buffer[(_startIndex + _count) % Capacity] = item;
                        _count++;
                    }
                    else
                    {
                        double oldValue = _buffer[_startIndex]; // 保存将被覆盖的值
                        _buffer[_startIndex] = item;
                        _startIndex = (_startIndex + 1) % Capacity;

                        // 如果覆盖的值是最小值或最大值，清除缓存
                        if (oldValue.Equals(_cachedMin) || oldValue.Equals(_cachedMax))
                        {
                            _cachedMin = null;
                            _cachedMax = null;
                        }
                    }

                    // 更新缓存极值
                    if (!_cachedMin.HasValue || item < _cachedMin) _cachedMin = item;
                    if (!_cachedMax.HasValue || item > _cachedMax) _cachedMax = item;
                }
            }

            public double[] ToArray()
            {
                lock (_lock)
                {
                    double[] result = new double[_count];
                    for (int i = 0; i < _count; i++)
                    {
                        result[i] = _buffer[(_startIndex + i) % Capacity];
                    }
                    return result;
                }
            }

            public (double min, double max) GetMinMax()
            {
                lock (_lock)
                {
                    if (_count == 0) return (double.NaN, double.NaN);

                    // 如果有缓存直接返回
                    if (_cachedMin.HasValue && _cachedMax.HasValue)
                        return (_cachedMin.Value, _cachedMax.Value);

                    // 缓存无效时重新计算
                    double min = _buffer[_startIndex];
                    double max = _buffer[_startIndex];

                    for (int i = 1; i < _count; i++)
                    {
                        double value = _buffer[(_startIndex + i) % Capacity];
                        if (value < min) min = value;
                        if (value > max) max = value;
                    }

                    _cachedMin = min;
                    _cachedMax = max;
                    return (min, max);
                }
            }

            public double GetLastValue()
            {
                lock (_lock)
                {
                    if (_count == 0) return double.NaN;
                    return _buffer[(_startIndex + _count - 1) % Capacity];
                }
            }

            public double[] Last(int count)
            {
                lock (_lock)
                {
                    if (count > _count) count = _count;
                    double[] result = new double[count];
                    int start = _count - count;

                    for (int i = 0; i < count; i++)
                    {
                        result[i] = _buffer[(_startIndex + start + i) % Capacity];
                    }
                    return result;
                }
            }

            public void Clear()
            {
                lock (_lock)
                {
                    _count = 0;
                    _startIndex = 0;
                    _cachedMin = null;
                    _cachedMax = null;
                }
            }
            // 在Form2类中添加新的滤波类和方法
            #region 高效的滤波实现

            // 高性能的移动平均滤波器（滑动窗口平均）
            public class MovingAverageFilter
            {
                private double[] _window;
                private double _sum;
                private int _index;
                private int _count;

                public int WindowSize => _window.Length;

                public MovingAverageFilter(int windowSize)
                {
                    _window = new double[windowSize];
                    _index = 0;
                    _count = 0;
                    _sum = 0;
                }

                public double Process(double input)
                {
                    _sum = _sum - _window[_index] + input;
                    _window[_index] = input;
                    _index = (_index + 1) % _window.Length;

                    if (_count < _window.Length)
                        _count++;

                    return _sum / _count;
                }
            }

            // 高性能的带通滤波器（二阶巴特沃斯）
            public class SimpleButterworthBandpass
            {
                // 二阶带通滤波系数
                private double a0, a1, a2;
                private double b0, b1, b2;

                // 状态变量
                private double x1, x2, y1, y2;

                public SimpleButterworthBandpass(double fs, double centerFreq, double bandwidth)
                {
                    double w0 = 2 * Math.PI * centerFreq / fs;
                    double bw = bandwidth / fs;
                    double Q = w0 / bw;

                    double alpha = Math.Sin(w0) / (2 * Q);

                    b0 = alpha;
                    b1 = 0;
                    b2 = -alpha;
                    a0 = 1 + alpha;
                    a1 = -2 * Math.Cos(w0);
                    a2 = 1 - alpha;
                }

                public double Process(double input)
                {
                    double output = (b0 * input + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2) / a0;

                    // 更新状态
                    x2 = x1;
                    x1 = input;
                    y2 = y1;
                    y1 = output;

                    return output;
                }
            }

            // 高效的50Hz陷波滤波器（固定频率，避免复杂计算）
            public class FastNotch50Hz
            {
                // 二阶陷波系数
                private double a0, a1, a2;
                private double b0, b1, b2;

                // 状态变量
                private double x1, x2, y1, y2;

                public FastNotch50Hz(double fs)
                {
                    double f0 = 50.0; // 50Hz工频干扰
                    double w0 = 2 * Math.PI * f0 / fs;
                    double Q = 30.0; // 高品质因数

                    // 固定系数（避免运行时计算）
                    double alpha = Math.Sin(w0) / (2 * Q);
                    double cosw0 = Math.Cos(w0);

                    // 陷波滤波器系数
                    b0 = 1;
                    b1 = -2 * cosw0;
                    b2 = 1;
                    a0 = 1 + alpha;
                    a1 = -2 * cosw0;
                    a2 = 1 - alpha;
                }

                public double Process(double input)
                {
                    double output = (b0 * input + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2) / a0;

                    // 更新状态
                    x2 = x1;
                    x1 = input;
                    y2 = y1;
                    y1 = output;

                    return output;
                }
            }

            #endregion
        }
        #region 垃圾
        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void splitContainerMain_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void splitContainerMain_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }
        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {

        }

        #endregion
    }
}