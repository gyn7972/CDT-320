using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace QMC.Vision.Ui.Dialogs
{
    /// <summary>
    /// CPU/GPU/메모리 부하 누적 차트 — 시퀀서 부하 체크(시작~완료) 동안 수집한 샘플로 구성.
    /// 샘플 = [경과초, CPU%, MEM MB, GPU%]. CPU%/GPU%는 좌측 Y축(%), MEM은 우측 Y2축(MB).
    /// GPU% 항목이 없는 옛 샘플(길이 3)도 호환된다.
    /// </summary>
    public partial class LoadChartDialog : Form
    {
        private readonly List<double[]> _samples;

        public LoadChartDialog(List<double[]> samples)
        {
            _samples = samples ?? new List<double[]>();
            InitializeComponent();
            BuildChart();
            BuildSummary();
        }

        private void BuildChart()
        {
            _chart.Series.Clear();
            _chart.ChartAreas.Clear();
            _chart.Legends.Clear();

            var area = new ChartArea("main");
            area.AxisX.Title = "경과 (초)";
            area.AxisX.MajorGrid.LineColor = Color.Gainsboro;
            area.AxisY.Title = "CPU/GPU (%)";
            area.AxisY.Minimum = 0;
            area.AxisY.MajorGrid.LineColor = Color.Gainsboro;
            area.AxisY2.Title = "MEM (MB)";
            area.AxisY2.Enabled = AxisEnabled.True;
            area.AxisY2.MajorGrid.Enabled = false;
            _chart.ChartAreas.Add(area);
            _chart.Legends.Add(new Legend("lg") { Docking = Docking.Top });

            var sCpu = new Series("CPU %")
            {
                ChartType = SeriesChartType.Line, BorderWidth = 2,
                Color = Color.FromArgb(220, 50, 50), XValueType = ChartValueType.Double, YAxisType = AxisType.Primary
            };
            var sGpu = new Series("GPU %")
            {
                ChartType = SeriesChartType.Line, BorderWidth = 2,
                Color = Color.FromArgb(40, 160, 80), XValueType = ChartValueType.Double, YAxisType = AxisType.Primary
            };
            var sMem = new Series("MEM MB")
            {
                ChartType = SeriesChartType.Line, BorderWidth = 2,
                Color = Color.FromArgb(33, 102, 172), XValueType = ChartValueType.Double, YAxisType = AxisType.Secondary
            };
            foreach (var s in _samples)
            {
                sCpu.Points.AddXY(s[0], s[1]);
                sMem.Points.AddXY(s[0], s[2]);
                sGpu.Points.AddXY(s[0], s.Length > 3 ? s[3] : 0);
            }
            _chart.Series.Add(sCpu);
            _chart.Series.Add(sGpu);
            _chart.Series.Add(sMem);
        }

        private void BuildSummary()
        {
            if (_samples.Count == 0) { _summary.Text = "데이터 없음"; return; }
            double dur  = _samples[_samples.Count - 1][0];
            double cpuAvg = _samples.Average(s => s[1]);
            double cpuMax = _samples.Max(s => s[1]);
            double gpuAvg = _samples.Average(s => s.Length > 3 ? s[3] : 0);
            double gpuMax = _samples.Max(s => s.Length > 3 ? s[3] : 0);
            double memAvg = _samples.Average(s => s[2]);
            double memMax = _samples.Max(s => s[2]);
            _summary.Text =
                $"수집 {_samples.Count}개 · {dur:F0}초    |    " +
                $"CPU 평균 {cpuAvg:F1}% · 최대 {cpuMax:F1}%    |    " +
                $"GPU 평균 {gpuAvg:F1}% · 최대 {gpuMax:F1}%    |    " +
                $"MEM 평균 {memAvg:F0}MB · 최대 {memMax:F0}MB";
        }

        private void OnSaveCsvClick(object sender, EventArgs e)
        {
            try
            {
                using (var dlg = new SaveFileDialog
                {
                    Filter = "CSV (*.csv)|*.csv",
                    FileName = "load_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv"
                })
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    var sb = new StringBuilder();
                    sb.AppendLine("elapsed_sec,cpu_pct,gpu_pct,mem_mb");
                    foreach (var s in _samples)
                        sb.AppendLine($"{s[0]:F1},{s[1]:F1},{(s.Length > 3 ? s[3] : 0):F1},{s[2]:F1}");
                    File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "저장 실패: " + ex.Message, "CSV 저장", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
