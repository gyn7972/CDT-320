using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// SPC 추세 차트 — 값 시리즈 + 상/하한(UpperLimit/LowerLimit) 점선.
    /// 마우스 휠 줌(+드래그 선택 줌), 우클릭 리셋, 우상단 "확대" 버튼 → 새 큰 창.
    /// 데이터는 SetData 로 주입(없으면 빈 차트).
    /// </summary>
    public class SpcTrendChart : UserControl
    {
        private readonly Chart _chart = new Chart();
        private readonly Button _btnZoom = new Button();
        private readonly Button _btnClear = new Button();

        private double[] _vals;
        private double _upper, _lower;
        private string _title = "";
        private Color _color = Color.DodgerBlue;

        public SpcTrendChart()
        {
            BackColor = Color.White;
            _chart.Dock = DockStyle.Fill;
            BuildArea(_chart, _title);
            _chart.MouseWheel += Chart_MouseWheel;
            _chart.MouseEnter += (s, e) => _chart.Focus();   // 휠 이벤트 수신
            _chart.DoubleClick += (s, e) => { ResetZoom(_chart); ClearCursor(_chart); }; // 더블클릭 = 줌/커서 초기화

            StyleTopButton(_btnZoom, "⤢");
            _btnZoom.Click += (s, e) => ShowPopout();
            StyleTopButton(_btnClear, "✕");                  // 클릭 커서(빨간선) 지우기
            _btnClear.Click += (s, e) => ClearCursor(_chart);

            Controls.Add(_btnClear);
            Controls.Add(_btnZoom);
            Controls.Add(_chart);
            _btnZoom.BringToFront();
            _btnClear.BringToFront();
            Resize += (s, e) => LayoutButtons();
            LayoutButtons();
        }

        private void LayoutButtons()
        {
            _btnZoom.Location  = new Point(Width - _btnZoom.Width - 4, 4);
            _btnClear.Location = new Point(Width - _btnZoom.Width - _btnClear.Width - 8, 4);
        }

        private static void StyleTopButton(Button b, string text)
        {
            b.Text = text;
            b.Size = new Size(26, 22);
            b.FlatStyle = FlatStyle.Flat;
            b.BackColor = Color.FromArgb(0x3A, 0x40, 0x4C);
            b.ForeColor = Color.White;
            b.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            b.Cursor = Cursors.Hand;
        }

        /// <summary>클릭으로 생긴 커서선(빨강) + 선택영역 제거.</summary>
        private static void ClearCursor(Chart chart)
        {
            try
            {
                var a = chart.ChartAreas[0];
                a.CursorX.SetCursorPosition(double.NaN);
                a.CursorY.SetCursorPosition(double.NaN);
                a.CursorX.SetSelectionPosition(double.NaN, double.NaN);
                a.CursorY.SetSelectionPosition(double.NaN, double.NaN);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[SpcTrendChart] ClearCursor 실패: " + ex.Message); }
        }

        /// <summary>차트 데이터 설정 — 값 배열 + 상/하한 + 제목 + 색.</summary>
        public void SetData(double[] values, double upper, double lower, string title, Color color)
        {
            _vals = values; _upper = upper; _lower = lower; _title = title ?? ""; _color = color;
            FillChart(_chart, _vals, _upper, _lower, _title, _color);
        }

        private static void BuildArea(Chart chart, string title)
        {
            chart.BackColor = Color.FromArgb(0x20, 0x24, 0x2B);
            var area = new ChartArea("a");
            area.BackColor = Color.FromArgb(0x16, 0x18, 0x1D);
            area.AxisX.LineColor = area.AxisY.LineColor = Color.FromArgb(0x66, 0x66, 0x66);
            area.AxisX.MajorGrid.LineColor = area.AxisY.MajorGrid.LineColor = Color.FromArgb(0x33, 0x38, 0x40);
            area.AxisX.LabelStyle.ForeColor = area.AxisY.LabelStyle.ForeColor = Color.FromArgb(0xAA, 0xAA, 0xAA);
            area.AxisY.LabelStyle.Format = "0.000";
            // 줌 활성화
            area.CursorX.IsUserEnabled = true;
            area.CursorX.IsUserSelectionEnabled = true;
            area.AxisX.ScaleView.Zoomable = true;
            area.CursorY.IsUserEnabled = true;
            area.CursorY.IsUserSelectionEnabled = true;
            area.AxisY.ScaleView.Zoomable = true;
            chart.ChartAreas.Add(area);
            chart.Titles.Clear();
            var t = chart.Titles.Add(title);
            t.ForeColor = Color.FromArgb(0xCC, 0xCC, 0xCC);
            t.Font = new Font("맑은 고딕", 8.5F, FontStyle.Regular);
            t.Alignment = ContentAlignment.TopLeft;
        }

        private static void FillChart(Chart chart, double[] vals, double upper, double lower, string title, Color color)
        {
            chart.Series.Clear();
            if (chart.ChartAreas.Count == 0) BuildArea(chart, title);
            if (chart.Titles.Count > 0) chart.Titles[0].Text = title;
            var area = chart.ChartAreas[0];
            area.AxisY.StripLines.Clear();

            var s = new Series("v")
            {
                ChartType = SeriesChartType.FastLine,
                Color = color,
                BorderWidth = 1,
                ChartArea = "a"
            };
            if (vals != null)
                for (int i = 0; i < vals.Length; i++) s.Points.AddXY(i + 1, vals[i]);
            chart.Series.Add(s);

            AddLimit(area, upper);
            AddLimit(area, lower);

            // Y 범위를 상/하한 바깥으로 약간 여유
            double margin = Math.Max(0.0001, (upper - lower) * 0.6);
            area.AxisY.Minimum = lower - margin;
            area.AxisY.Maximum = upper + margin;
        }

        private static void AddLimit(ChartArea area, double value)
        {
            var sl = new StripLine
            {
                IntervalOffset = value,
                StripWidth = 0,                       // 0 폭 → 선
                BorderColor = Color.FromArgb(0xE2, 0x4B, 0x4A),
                BorderWidth = 1,
                BorderDashStyle = ChartDashStyle.Dash
            };
            area.AxisY.StripLines.Add(sl);
        }

        private static void ResetZoom(Chart chart)
        {
            try
            {
                var a = chart.ChartAreas[0];
                a.AxisX.ScaleView.ZoomReset(0);
                a.AxisY.ScaleView.ZoomReset(0);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[SpcTrendChart] ResetZoom 실패: " + ex.Message); }
        }

        private void Chart_MouseWheel(object sender, MouseEventArgs e)
        {
            try
            {
                var area = _chart.ChartAreas[0];
                Axis ax = area.AxisX;
                double pos = ax.PixelPositionToValue(e.Location.X);
                double min = ax.ScaleView.IsZoomed ? ax.ScaleView.ViewMinimum : ax.Minimum;
                double max = ax.ScaleView.IsZoomed ? ax.ScaleView.ViewMaximum : ax.Maximum;
                if (double.IsNaN(min) || double.IsNaN(max) || max <= min) return;

                if (e.Delta > 0)
                {
                    double span = (max - min) / 3.0;
                    ax.ScaleView.Zoom(Math.Max(ax.Minimum, pos - span), Math.Min(ax.Maximum, pos + span));
                }
                else
                {
                    ax.ScaleView.ZoomReset();
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[SpcTrendChart] wheel zoom 실패: " + ex.Message); }
        }

        /// <summary>현재 데이터를 큰 창으로 띄운다(독립 줌 가능).</summary>
        public void ShowPopout()
        {
            try
            {
                var big = new Chart { Dock = DockStyle.Fill };
                BuildArea(big, _title);
                FillChart(big, _vals, _upper, _lower, _title, _color);
                big.MouseEnter += (s, e) => big.Focus();
                big.DoubleClick += (s, e) => { ResetZoom(big); ClearCursor(big); };
                big.MouseWheel += (s, e) =>
                {
                    try
                    {
                        var ax = big.ChartAreas[0].AxisX;
                        double pos = ax.PixelPositionToValue(e.Location.X);
                        double min = ax.ScaleView.IsZoomed ? ax.ScaleView.ViewMinimum : ax.Minimum;
                        double max = ax.ScaleView.IsZoomed ? ax.ScaleView.ViewMaximum : ax.Maximum;
                        if (max <= min) return;
                        if (e.Delta > 0) { double sp = (max - min) / 3.0; ax.ScaleView.Zoom(pos - sp, pos + sp); }
                        else ax.ScaleView.ZoomReset();
                    }
                    catch { }
                };
                var f = new Form
                {
                    Text = "SPC 차트 — 확대 (휠/드래그 줌, 우클릭 리셋)  ·  " + _title,
                    StartPosition = FormStartPosition.CenterParent,
                    Size = new Size(1100, 720)
                };
                f.Controls.Add(big);
                f.Show(FindForm());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[SpcTrendChart] ShowPopout 실패: " + ex.Message); }
        }
    }
}
