using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Feature;
using MathWorks.MATLAB.NET.Arrays;
using ScottPlot.Statistics;
using ScottPlot.WinForms;
using System.Diagnostics;

namespace Test0524
{
    public partial class Form4: Form
    {
        // 通道数据存储
        private List<double> channel1 = new List<double>();
        private List<double> channel2 = new List<double>();
        private List<double> channel3 = new List<double>();
        private List<double> channel4 = new List<double>();
        private int[] SelectedChannels = new int[] { 0, 2, 4, 6 }; // Ch0, Ch2, Ch4, Ch6
        // 状态跟踪
        private string loadedFilePath = "";
        private int totalSamplesLoaded = 0;

        // 特征提取结果
        private FeatureResult featureResult;

        // 图表控件数组 
        private ScottPlot.WinForms.FormsPlot[] featurePlots;
        private const int maxPointsPerPlot = 5000; // 最大显示点数
        public Form4()
        {
            InitializeComponent();
            InitializeFeaturePlots();
        }
        private void InitializeFeaturePlots()
        {
            // 初始化12个图表控件
            featurePlots = new ScottPlot.WinForms.FormsPlot[12]
            {
            formsPlot1, formsPlot2, formsPlot3,
            formsPlot4, formsPlot5, formsPlot6,
            formsPlot7, formsPlot8, formsPlot9,
            formsPlot10, formsPlot11, formsPlot12
            };

            // 配置每个图表
            for (int i = 0; i < featurePlots.Length; i++)
            {
                var plot = featurePlots[i].Plot;
                plot.Axes.SetLimitsY(0, 1); // 初始Y轴范围
                plot.Axes.SetLimitsX(0, 50); // 初始X轴范围
                plot.Grid.IsVisible = true;

                // 添加交互支持
                plot.Axes.AutoScale();
                featurePlots[i].MouseDoubleClick += (s, e) => plot.Axes.AutoScale();

                // 添加上下文菜单
                var ctxMenu = new ContextMenuStrip();
                var resetItem = new ToolStripMenuItem("重置视图");
                resetItem.Click += (s, ev) => plot.Axes.AutoScale();
                ctxMenu.Items.Add(resetItem);
                featurePlots[i].ContextMenuStrip = ctxMenu;
            }
        }

        private void btnExtractFeatures_Click(object sender, EventArgs e)
        {
            if (channel1.Count == 0)
            {
                MessageBox.Show("请先加载EMG数据", "数据缺失",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 创建二维数组存储所有通道数据
                double[,] allChannelsData = new double[channel1.Count, 4];

                // 填充数据
                for (int i = 0; i < channel1.Count; i++)
                {
                    allChannelsData[i, 0] = channel1[i];
                    allChannelsData[i, 1] = channel2[i];
                    allChannelsData[i, 2] = channel3[i];
                    allChannelsData[i, 3] = channel4[i];
                }

                // 采样率 (根据实际情况调整)
                double samplingRate = 1000;

                // 调用MATLAB特征提取
                featureResult = ExtractFeatures(allChannelsData, samplingRate);

                // 更新状态
                UpdateStatus("特征提取完成");

                // 绘制特征图表
                PlotFeatures();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"特征提取失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private FeatureResult ExtractFeatures(double[,] emgData, double samplingRate)
        {
            // 创建MATLAB数组
            MWNumericArray inputSignal = new MWNumericArray(emgData);
            MWNumericArray inputFs = new MWNumericArray(samplingRate);

            // 创建MATLAB组件实例 (使用您的实际类名)
            emgFeature featureExtractor = new emgFeature();

            // 调用函数
            object result = featureExtractor.ExtractEMGFeature(
                (MWArray)inputSignal,
                (MWArray)inputFs
            );

            // 解析结果
            return ParseFeatureResult(result);
        }
        private void PlotFeatures()
        {
            if (featureResult == null) return;

            // 通道1特征
            PlotChannelFeatures(0, featurePlots[0], featurePlots[1], featurePlots[2]);

            // 通道2特征
            PlotChannelFeatures(1, featurePlots[3], featurePlots[4], featurePlots[5]);

            // 通道3特征
            PlotChannelFeatures(2, featurePlots[6], featurePlots[7], featurePlots[8]);

            // 通道4特征
            PlotChannelFeatures(3, featurePlots[9], featurePlots[10], featurePlots[11]);
        }
        private FeatureResult ParseFeatureResult(object matlabResult)
        {
            MWStructArray features = (MWStructArray)matlabResult;

            return new FeatureResult
            {
                // 将二维数组结果转换为C#二维数组
                IEMG_values = (double[,])features["IEMG_values"].ToArray(),
                RMS_values = (double[,])features["RMS_values"].ToArray(),
                medianFreqs = (double[,])features["medianFreqs"].ToArray(),

                // 将向量结果转换为C#一维数组
                windowTimes = ConvertToDoubleArray(features["windowTimes"]),
                mfWindowTimes = ConvertToDoubleArray(features["mfWindowTimes"])
            };
        }

        // 辅助方法：将MWArray转换为double[]
        private double[] ConvertToDoubleArray(MWArray array)
        {
            // 获取原始数据
            Array rawData = array.ToArray();

            // 处理不同维度的结果
            if (rawData is double[,] matrix)
            {
                // 如果是二维数组，转换为向量
                int rows = matrix.GetLength(0);
                int cols = matrix.GetLength(1);
                double[] result = new double[rows * cols];

                Buffer.BlockCopy(matrix, 0, result, 0, matrix.Length * sizeof(double));
                return result;
            }
            else if (rawData is double[] vector)
            {
                // 如果已经是一维数组，直接返回
                return vector;
            }

            throw new InvalidCastException($"无法转换类型: {rawData.GetType().Name}");
        }

        private void PlotChannelFeatures(int channelIndex,
            ScottPlot.WinForms.FormsPlot iemgPlot,
            ScottPlot.WinForms.FormsPlot rmsPlot,
            ScottPlot.WinForms.FormsPlot mfPlot)
        {
            // 获取通道数据
            double[] iemgData = GetChannelFeatureData(featureResult.IEMG_values, channelIndex);
            double[] rmsData = GetChannelFeatureData(featureResult.RMS_values, channelIndex);
            double[] mfData = GetChannelFeatureData(featureResult.medianFreqs, channelIndex);

            // 设置IEMG图表 - 使用ScottPlot的Color类型
            SetFeaturePlot(iemgPlot, iemgData, featureResult.windowTimes,
                          $"Chl {channelIndex + 1} - IEMG", new ScottPlot.Color(0, 0, 255)); // 蓝色

            // 设置RMS图表
            SetFeaturePlot(rmsPlot, rmsData, featureResult.windowTimes,
                          $"Chl {channelIndex + 1} - RMS", new ScottPlot.Color(0, 128, 0)); // 绿色

            // 设置中值频率图表
            SetFeaturePlot(mfPlot, mfData, featureResult.mfWindowTimes,
                          $"Chl {channelIndex + 1} - cenfre", new ScottPlot.Color(255, 0, 0)); // 红色
        }

        private void SetFeaturePlot(ScottPlot.WinForms.FormsPlot plotControl,
                          double[] featureData, double[] timeValues,
                          string title, ScottPlot.Color color)
        {
            var plot = plotControl.Plot;
            plot.Clear();

            // 确保时间和数据有相同的长度
            if (featureData.Length != timeValues.Length)
            {
                Debug.WriteLine($"警告：特征数据长度({featureData.Length})与时间值长度({timeValues.Length})不匹配");
                return;
            }

            // 创建信号图
            var signal = plot.Add.SignalXY(timeValues, featureData);
            signal.Color = color;
            signal.LineWidth = 2;

            // 设置标题
            plot.Title(title);
            plot.XLabel("t (s)");

            // 设置轴范围
            if (featureData.Length > 0)
            {
                double minY = featureData.Min();
                double maxY = featureData.Max();
                double padding = (maxY - minY) * 0.1;

                // 设置Y轴范围
                plot.Axes.SetLimitsY(minY - padding, maxY + padding);

                // 自动设置X轴范围
                plot.Axes.AutoScaleX();
            }
            else
            {
                plot.Axes.SetLimits(0, 1, -1, 1); // 默认范围
            }

            plotControl.Refresh();
        }

        private double[] GetChannelFeatureData(double[,] allData, int channelIndex)
        {
            int dataCount = allData.GetLength(0);
            double[] channelData = new double[dataCount];

            for (int i = 0; i < dataCount; i++)
            {
                channelData[i] = allData[i, channelIndex];
            }

            return channelData;
        }

        private void UpdateStatus(string message)
        {
            lblStatus.Text = message;
            txtStatus.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
        private void btnLoad_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "CSV文件|*.csv";
                openDialog.Title = "加载特征提取数据";

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadChannelData(openDialog.FileName);
                    // 自动触发特征提取
                    btnExtractFeatures.PerformClick();
                }
            }
        }
        private void LoadChannelData(string filePath)
        {
            try
            {
                // 清空现有数据
                channel1.Clear();
                channel2.Clear();
                channel3.Clear();
                channel4.Clear();

                // 读取CSV文件
                using (StreamReader reader = new StreamReader(filePath))
                {
                    // 跳过标题行
                    string header = reader.ReadLine();
                    if (!header.Contains("Ch0") || !header.Contains("Ch3"))
                    {
                        MessageBox.Show("文件格式错误：缺少必需的通道数据", "格式错误");
                        return;
                    }

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] values = line.Split(',');

                        if (values.Length >= 9) // 检查是否有效肌电数据行
                        {
                            // 按预设选择特定通道（Ch0, Ch2, Ch4, Ch6）
                            channel1.Add(double.Parse(values[SelectedChannels[0] + 1]));
                            channel2.Add(double.Parse(values[SelectedChannels[1] + 1]));
                            channel3.Add(double.Parse(values[SelectedChannels[2] + 1]));
                            channel4.Add(double.Parse(values[SelectedChannels[3] + 1]));
                        }
                    }
                }

                loadedFilePath = filePath;
                totalSamplesLoaded = channel1.Count;
                UpdateStatus($"已加载数据: {filePath} | 样本数: {totalSamplesLoaded}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载数据失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadEMGData(string filePath)
        {
            // 重置数据存储
            channel1.Clear();
            channel2.Clear();
            channel3.Clear();
            channel4.Clear();
            totalSamplesLoaded = 0;
            loadedFilePath = filePath;

            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    // 跳过标题行
                    reader.ReadLine();

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        // 解析数据行
                        ProcessEMGLine(line);
                    }
                }

                // 加载完成，显示统计信息
                DisplayChannelStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件时出错:\n{ex.Message}", "文件错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ProcessEMGLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            string[] values = line.Split(',');

            // 验证数据格式
            if (values.Length < 9)
            {
                Console.WriteLine($"无效数据行: {line}");
                return;
            }

            // 解析并存储通道数据（跳过第一列帧号）
            if (TryParseDouble(values[1], out double ch1)) channel1.Add(ch1);
            if (TryParseDouble(values[3], out double ch2)) channel2.Add(ch2);
            if (TryParseDouble(values[5], out double ch3)) channel3.Add(ch3);
            if (TryParseDouble(values[7], out double ch4)) channel4.Add(ch4);

            totalSamplesLoaded++;
        }

        private bool TryParseDouble(string value, out double result)
        {
            // 处理科学计数法和其他格式
            return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out result);
        }

        private void DisplayChannelStats()
        {
            StringBuilder stats = new StringBuilder();
            stats.AppendLine($"文件: {Path.GetFileName(loadedFilePath)}");
            stats.AppendLine($"总样本数: {totalSamplesLoaded}");
            stats.AppendLine($"通道1数据点: {channel1.Count}");
            stats.AppendLine($"通道2数据点: {channel2.Count}");
            stats.AppendLine($"通道3数据点: {channel3.Count}");
            stats.AppendLine($"通道4数据点: {channel4.Count}");

            // 在UI上显示统计信息
            txtStatus.Text = stats.ToString();
        }

        // 示例：获取通道数据的公共方法
        public double[] GetChannel1Data() => channel1.ToArray();
        public double[] GetChannel2Data() => channel2.ToArray();
        public double[] GetChannel3Data() => channel3.ToArray();
        public double[] GetChannel4Data() => channel4.ToArray();
        // 特征结果容器类
        public class FeatureResult
        {
            public double[,] IEMG_values { get; set; }
            public double[,] RMS_values { get; set; }
            public double[,] medianFreqs { get; set; }
            public double[] windowTimes { get; set; }
            public double[] mfWindowTimes { get; set; }
        }
    }


}
