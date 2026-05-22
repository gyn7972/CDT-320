using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using QMC.Vision.Config;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// SPC X-bar 차트 페이지 — DataLogSaver CSV 의 다이별 검사 항목을 시계열 X-bar 로 표시.
    /// 310 의 XBarChartForm 동등 기능 (코드 독자 작성).
    /// </summary>
    public class SpcChartPage : UserControl
    {
        // 310 DataLogSaver.InspectionItems 15종 (사용자 선택 가능)
        private static readonly string[] Items = {
            "Die_Width", "Die_Height",
            "Back_Chipping_Top_Size", "Back_Chipping_Right_Size",
            "Back_Chipping_Bottom_Size", "Back_Chipping_Left_Size",
            "Back_Chipping_Length", "Back_Foreign_Size",
            "Side_Chipping_Top", "Side_Chipping_Right",
            "Side_Chipping_Bottom", "Side_Chipping_Left",
            "Post_Place_Top_Gap_Avg", "Post_Place_Bottom_Gap_Avg",
            "Post_Place_Left_Gap_Avg", "Post_Place_Right_Gap_Avg",
            "ForeignObjectSize",
        };

        private ComboBox     _cbItem;
        private NumericUpDown _nLsl, _nUsl;
        private Chart        _chart;
        private Label        _lblStats;
        private DateTimePicker _dpDate;

        public SpcChartPage()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildLayout();
            ReloadCsvAndPlot();
        }

        private void BuildLayout()
        {
            var hdr = new Label
            {
                Dock = DockStyle.Top, Height = 30, Text = "SPC X-bar Chart",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(hdr);

            var top = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.WhiteSmoke };
            top.Controls.Add(new Label { Location = new Point(8, 8), AutoSize = true, Text = "Item:", Font = UiTheme.ButtonFont });
            _cbItem = new ComboBox
            {
                Location = new Point(60, 4), Size = new Size(280, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.ButtonFont
            };
            foreach (var s in Items) _cbItem.Items.Add(s);
            _cbItem.SelectedIndex = 0;
            _cbItem.SelectedIndexChanged += (s, e) => Plot();
            top.Controls.Add(_cbItem);

            top.Controls.Add(new Label { Location = new Point(360, 8), AutoSize = true, Text = "Date:", Font = UiTheme.ButtonFont });
            _dpDate = new DateTimePicker
            {
                Location = new Point(410, 4), Size = new Size(160, 28),
                Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd",
                Font = UiTheme.ButtonFont, Value = DateTime.Today
            };
            _dpDate.ValueChanged += (s, e) => ReloadCsvAndPlot();
            top.Controls.Add(_dpDate);

            top.Controls.Add(new Label { Location = new Point(8, 44), AutoSize = true, Text = "LSL:", Font = UiTheme.ButtonFont });
            _nLsl = new NumericUpDown
            {
                Location = new Point(60, 40), Size = new Size(120, 28),
                Minimum = -1000, Maximum = 1000, DecimalPlaces = 4, Increment = 0.001m,
                Font = UiTheme.ValueFont, Value = -0.05m
            };
            _nLsl.ValueChanged += (s, e) => Plot();
            top.Controls.Add(_nLsl);
            top.Controls.Add(new Label { Location = new Point(200, 44), AutoSize = true, Text = "USL:", Font = UiTheme.ButtonFont });
            _nUsl = new NumericUpDown
            {
                Location = new Point(240, 40), Size = new Size(120, 28),
                Minimum = -1000, Maximum = 1000, DecimalPlaces = 4, Increment = 0.001m,
                Font = UiTheme.ValueFont, Value = 0.05m
            };
            _nUsl.ValueChanged += (s, e) => Plot();
            top.Controls.Add(_nUsl);

            var btnReload = new Button
            {
                Location = new Point(580, 4), Size = new Size(120, 28),
                Text = "Reload CSV", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont,
                BackColor = Color.White
            };
            btnReload.Click += (s, e) => ReloadCsvAndPlot();
            top.Controls.Add(btnReload);

            _lblStats = new Label
            {
                Location = new Point(380, 40), Size = new Size(540, 28),
                Text = "(no data)", Font = UiTheme.ValueFont,
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
            };
            top.Controls.Add(_lblStats);
            Controls.Add(top);

            _chart = new Chart { Dock = DockStyle.Fill };
            var ca = new ChartArea("main")
            {
                BackColor = Color.White,
                AxisX = { Title = "Sample", MajorGrid = { LineColor = Color.LightGray } },
                AxisY = { Title = "Value", MajorGrid = { LineColor = Color.LightGray } }
            };
            _chart.ChartAreas.Add(ca);
            _chart.Legends.Add(new Legend("legend") { Docking = Docking.Top });
            Controls.Add(_chart);
        }

        private string[] _columns;
        private string[][] _rows = new string[0][];

        private void ReloadCsvAndPlot()
        {
            _columns = null; _rows = new string[0][];

            try
            {
                var cfg = VisionConfigStore.Current ?? new VisionSettings();
                string root = string.IsNullOrEmpty(cfg.DataLogPath) ? @".\Log\Data" : cfg.DataLogPath;
                string fn = "vision_" + _dpDate.Value.ToString("yyyyMMdd") + ".csv";
                string path = Path.Combine(root, fn);
                if (!File.Exists(path))
                {
                    _lblStats.Text = "No CSV: " + path;
                    Plot();
                    return;
                }
                var lines = File.ReadAllLines(path);
                if (lines.Length < 2) { _lblStats.Text = "CSV empty"; Plot(); return; }
                _columns = lines[0].Split(',');
                _rows = lines.Skip(1)
                             .Where(l => !string.IsNullOrWhiteSpace(l))
                             .Select(l => SplitCsv(l))
                             .ToArray();
            }
            catch (Exception ex) { _lblStats.Text = "Load fail: " + ex.Message; }
            Plot();
        }

        private void Plot()
        {
            _chart.Series.Clear();
            if (_columns == null || _rows.Length == 0)
            {
                _lblStats.Text = "(no data)";
                return;
            }

            string item = _cbItem.SelectedItem as string;
            int colIdx = Array.IndexOf(_columns, item);
            if (colIdx < 0) { _lblStats.Text = "column not found: " + item; return; }

            var values = _rows
                .Select(r => colIdx < r.Length ? r[colIdx] : "")
                .Select(s => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? (double?)v : null)
                .Where(v => v.HasValue)
                .Select(v => v.Value).ToArray();
            if (values.Length == 0) { _lblStats.Text = "no numeric data for " + item; return; }

            // 데이터 시리즈
            var s1 = new Series("Sample") { ChartType = SeriesChartType.Line, BorderWidth = 2, Color = Color.SteelBlue, MarkerStyle = MarkerStyle.Circle, MarkerSize = 6 };
            for (int i = 0; i < values.Length; i++) s1.Points.AddXY(i + 1, values[i]);
            _chart.Series.Add(s1);

            // Avg
            double avg = values.Average();
            var sAvg = new Series("Avg") { ChartType = SeriesChartType.Line, BorderWidth = 1, Color = Color.Black, BorderDashStyle = ChartDashStyle.Dash };
            sAvg.Points.AddXY(1, avg); sAvg.Points.AddXY(values.Length, avg);
            _chart.Series.Add(sAvg);

            // LSL / USL
            double lsl = (double)_nLsl.Value, usl = (double)_nUsl.Value;
            var sLsl = new Series("LSL") { ChartType = SeriesChartType.Line, BorderWidth = 2, Color = Color.OrangeRed };
            sLsl.Points.AddXY(1, lsl); sLsl.Points.AddXY(values.Length, lsl);
            _chart.Series.Add(sLsl);
            var sUsl = new Series("USL") { ChartType = SeriesChartType.Line, BorderWidth = 2, Color = Color.OrangeRed };
            sUsl.Points.AddXY(1, usl); sUsl.Points.AddXY(values.Length, usl);
            _chart.Series.Add(sUsl);

            // 통계
            double stdev = Stdev(values, avg);
            double max = values.Max(), min = values.Min();
            int oos = values.Count(v => v < lsl || v > usl);
            _lblStats.Text = $"n={values.Length}  avg={avg:F4}  stdev={stdev:F4}  max={max:F4}  min={min:F4}  out-of-spec={oos}";
        }

        private static double Stdev(double[] arr, double mean)
        {
            if (arr.Length < 2) return 0;
            double s = 0;
            foreach (var v in arr) s += (v - mean) * (v - mean);
            return Math.Sqrt(s / (arr.Length - 1));
        }

        // CSV 분리 (간이 — 따옴표 미지원)
        private static string[] SplitCsv(string line)
        {
            return line.Split(',');
        }
    }
}
