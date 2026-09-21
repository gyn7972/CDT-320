using QMC.CDT320.Diagnostics;
using System;
using QMC.CDT_320.Ui.Localization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace QMC.CDT320.Ui.Controls
{
    /// <summary>
    /// 실시간 Cycle Time 간트 — CycleTimeStore 를 그린다 (비전 원본 이식, 모터 세그먼트 방식).
    /// 보기 모드: 픽커(8행 F1~F4/R1~R4) / 유닛(5행+픽커 서브레인) / 사이클(최근 목록+모터별 상세).
    /// 페인트 전용(운전 경로 무영향), 숨김 시 갱신 정지.
    /// </summary>
    public partial class CycleTimeGanttControl : UserControl, ILocalizedView
    {
        private enum ViewMode { Picker, Unit, Cycle }

        private const int RefreshMs = 500;
        private static readonly string[] PickerRows = { "F1", "F2", "F3", "F4", "R1", "R2", "R3", "R4" };
        // 행 = Entry.Unit 값과 정확히 일치해야 함.
        private static readonly string[] UnitRows = { "INPUTVISION", "PICKUP", "BOTTOM", "SIDE", "PLACE" };
        private static readonly int[] WindowMsChoices = { 5000, 10000, 30000, 60000, 300000 };

        private static readonly Color GridLine = Color.FromArgb(0x2a, 0x31, 0x3a);
        private static readonly Color RowLabelFg = Color.FromArgb(0x9f, 0xb2, 0xc8);
        private static readonly Color AxisFg = Color.FromArgb(0x6b, 0x77, 0x86);

        // 모터(축) 세그먼트 팔레트 — 축 이름 해시로 안정적으로 배정.
        private static readonly Color[] MotionPalette =
        {
            Color.FromArgb(0x4f, 0xc3, 0xf7),
            Color.FromArgb(0xff, 0xb7, 0x4d),
            Color.FromArgb(0x81, 0xc7, 0x84),
            Color.FromArgb(0xba, 0x68, 0xc8),
            Color.FromArgb(0x4d, 0xd0, 0xe1),
            Color.FromArgb(0xff, 0x8a, 0x65),
            Color.FromArgb(0xdc, 0xe7, 0x75),
            Color.FromArgb(0xf0, 0x62, 0x92),
            Color.FromArgb(0xa1, 0x88, 0x7f),
            Color.FromArgb(0x90, 0xa4, 0xae)
        };

        private sealed class BarHit
        {
            public Rectangle Rect;
            public CycleTimeEntry Entry;
        }

        private Timer _timer;
        private ToolTip _tip;
        private ViewMode _mode = ViewMode.Picker;
        private int _windowMs = 60000;
        private bool _paused;
        private long _pauseTick;
        private readonly List<BarHit> _hits = new List<BarHit>();
        private readonly Dictionary<long, CycleTimeEntry> _listedEntries = new Dictionary<long, CycleTimeEntry>();
        private CycleTimeEntry _hoverEntry;
        private long _selectedCycleSeq = -1;
        private long _lastListRefreshSeq = -1;
        // 구간 측정(클릭→클릭) — 절대 시각(tick)으로 보관해 차트가 흘러도 마커가 데이터에 붙어 있다.
        private long _markStartTick = -1;
        private long _markEndTick = -1;
        private Rectangle _lastPlot = Rectangle.Empty;

        public CycleTimeGanttControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Lang.BindKey(_lblTitle, "diagram.cycle.title");
            Lang.BindKey(_btnModePicker, "diagram.cycle.picker");
            Lang.BindKey(_btnModeUnit, "diagram.cycle.unit");
            Lang.BindKey(_btnModeCycle, "diagram.cycle.cycle");
            _cmbWindow.DrawMode = DrawMode.OwnerDrawFixed;
            _cmbWindow.DrawItem += cmbWindow_DrawItem;
            _cmbWindow.SelectedIndex = 3;   // 60초 기본
            _tip = new ToolTip { InitialDelay = 200, ReshowDelay = 100 };
            _timer = new Timer { Interval = RefreshMs };
            _timer.Tick += OnTimerTick;
            VisibleChanged += (s, e) => { if (_timer != null) _timer.Enabled = Visible; };
            MouseMove += OnCanvasMouseMove;
            MouseDown += OnCanvasMouseDown;
            MouseLeave += (s, e) => HideTip();
            ApplyLanguage();
        }

        public void ApplyLanguage()
        {
            _colTime.Text = Lang.T("diagram.cycle.time");
            _colUnit.Text = Lang.T("diagram.cycle.unit");
            _colDie.Text = Lang.T("diagram.cycle.die");
            _colPicker.Text = Lang.T("diagram.cycle.picker");
            _colTotal.Text = Lang.T("diagram.cycle.totalColumn");
            _colState.Text = Lang.T("diagram.cycle.state");
            _btnPause.Text = _paused ? Lang.T("diagram.cycle.resume") : Lang.T("diagram.cycle.pause");
            UpdateMeasureText();
            // 목록의 모델/Tag/선택은 유지하고 표시 문자열만 교체합니다.
            foreach (ListViewItem item in _lstCycles.Items)
            {
                if (item.Tag is long seq && _listedEntries.TryGetValue(seq, out var entry))
                {
                    item.SubItems[1].Text = UnitDisplay(entry.Unit);
                    item.SubItems[5].Text = ResultDisplay(entry.Failed);
                }
            }
            _cmbWindow.Invalidate();
            HideTip();
            Invalidate();
        }

        private void UpdateMeasureText()
        {
            _lblMeasure.Text = _markStartTick < 0 ? Lang.T("diagram.cycle.measureHelp")
                : (_markEndTick < 0 ? Lang.T("diagram.cycle.measureEnd") : Lang.Format("diagram.cycle.measureResult", Math.Abs(TickToMs(_markEndTick - _markStartTick)).ToString("0.0")));
        }

        private void cmbWindow_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0 || e.Index >= WindowMsChoices.Length) return;
            string text = e.Index == 4 ? Lang.Format("diagram.cycle.minutes", WindowMsChoices[e.Index] / 60000) : Lang.Format("diagram.cycle.seconds", WindowMsChoices[e.Index] / 1000);
            TextRenderer.DrawText(e.Graphics, text, e.Font, e.Bounds, e.ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            e.DrawFocusRectangle();
        }

        private static string UnitDisplay(string unit)
        {
            switch (unit)
            {
                case "INPUTVISION": return Lang.T("diagram.cycle.unit.inputvision");
                case "PICKUP": return Lang.T("diagram.cycle.unit.pickup");
                case "BOTTOM": return Lang.T("diagram.cycle.unit.bottom");
                case "SIDE": return Lang.T("diagram.cycle.unit.side");
                case "PLACE": return Lang.T("diagram.cycle.unit.place");
                default: return unit ?? string.Empty;
            }
        }

        private static string ResultDisplay(bool failed)
        {
            return failed ? Lang.T("diagram.cycle.failed") : Lang.T("diagram.cycle.ok");
        }

        /// <summary>보기 모드 전환(테스트/외부 제어용). 0=픽커, 1=유닛, 2=사이클.</summary>
        public void SetViewMode(int mode)
        {
            _mode = mode == 1 ? ViewMode.Unit : (mode == 2 ? ViewMode.Cycle : ViewMode.Picker);
            _lstCycles.Visible = _mode == ViewMode.Cycle;
            if (_mode == ViewMode.Cycle) { _lastListRefreshSeq = -1; RefreshCycleList(); }
            HighlightModeButtons();
            Invalidate();
        }

        /// <summary>테스트용 즉시 렌더 준비 — 리스트 갱신 포함.</summary>
        public void RefreshNow()
        {
            if (_mode == ViewMode.Cycle) RefreshCycleList();
            Invalidate();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (!Visible || _paused) return;
            if (_mode == ViewMode.Cycle) RefreshCycleList();
            Invalidate();
        }

        private void btnModePicker_Click(object sender, EventArgs e) { SetViewMode(0); }
        private void btnModeUnit_Click(object sender, EventArgs e) { SetViewMode(1); }
        private void btnModeCycle_Click(object sender, EventArgs e) { SetViewMode(2); }

        private void cmbWindow_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = _cmbWindow.SelectedIndex;
            if (index >= 0 && index < WindowMsChoices.Length) _windowMs = WindowMsChoices[index];
            Invalidate();
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            _paused = !_paused;
            _pauseTick = Stopwatch.GetTimestamp();
            _btnPause.BackColor = _paused ? Color.FromArgb(0xd9, 0x77, 0x06) : Color.FromArgb(0x3a, 0x3a, 0x3e);
            _btnPause.Text = _paused ? Lang.T("diagram.cycle.resume") : Lang.T("diagram.cycle.pause");
            Invalidate();
        }

        private void lstCycles_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedCycleSeq = -1;
            if (_lstCycles.SelectedItems.Count > 0 && _lstCycles.SelectedItems[0].Tag is long)
                _selectedCycleSeq = (long)_lstCycles.SelectedItems[0].Tag;
            Invalidate();
        }

        private void HighlightModeButtons()
        {
            Color on = Color.FromArgb(0xd9, 0x77, 0x06), off = Color.FromArgb(0x3a, 0x3a, 0x3e);
            _btnModePicker.BackColor = _mode == ViewMode.Picker ? on : off;
            _btnModeUnit.BackColor = _mode == ViewMode.Unit ? on : off;
            _btnModeCycle.BackColor = _mode == ViewMode.Cycle ? on : off;
        }

        // ── 색상 ──
        internal static Color UnitColor(string unit)
        {
            switch (unit)
            {
                case "INPUTVISION": return Color.FromArgb(0x4f, 0xc3, 0xf7);
                case "PICKUP": return Color.FromArgb(0xff, 0xb7, 0x4d);
                case "BOTTOM": return Color.FromArgb(0xff, 0xd5, 0x4f);
                case "SIDE": return Color.FromArgb(0x81, 0xc7, 0x84);
                case "PLACE": return Color.FromArgb(0xba, 0x68, 0xc8);
                default: return Color.Gray;
            }
        }

        private static Color PickerColor(int picker)
        {
            // 유닛 모드에서 바 색 = 픽커별(F1~F4 난색, R1~R4 한색).
            switch (picker)
            {
                case 1: return Color.FromArgb(0xff, 0x8a, 0x65);
                case 2: return Color.FromArgb(0xff, 0xb7, 0x4d);
                case 3: return Color.FromArgb(0xff, 0xd5, 0x4f);
                case 4: return Color.FromArgb(0xdc, 0xe7, 0x75);
                case 5: return Color.FromArgb(0x4f, 0xc3, 0xf7);
                case 6: return Color.FromArgb(0x4d, 0xd0, 0xe1);
                case 7: return Color.FromArgb(0x81, 0xc7, 0x84);
                case 8: return Color.FromArgb(0xba, 0x68, 0xc8);
                default: return Color.Gray;
            }
        }

        internal static Color MotionColor(string axis)
        {
            if (string.IsNullOrEmpty(axis)) return Color.Gray;
            int hash = 17;
            for (int i = 0; i < axis.Length; i++)
                hash = hash * 31 + char.ToUpperInvariant(axis[i]);
            int index = hash % MotionPalette.Length;
            if (index < 0) index += MotionPalette.Length;
            return MotionPalette[index];
        }

        private static Color Dim(Color color, double factor)
        {
            return Color.FromArgb(color.A, (int)(color.R * factor), (int)(color.G * factor), (int)(color.B * factor));
        }

        private static string PickerLabel(int picker)
        {
            if (picker >= 1 && picker <= 4) return "F" + picker;
            if (picker >= 5 && picker <= 8) return "R" + (picker - 4);
            return "-";
        }

        // ── 페인트 ──
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            try
            {
                Rectangle canvas = ClientRectangle;
                canvas.Y += _toolbar.Height;
                canvas.Height -= _toolbar.Height;
                if (_mode == ViewMode.Cycle && _lstCycles.Visible)
                {
                    canvas.X += _lstCycles.Width;
                    canvas.Width -= _lstCycles.Width;
                }
                if (canvas.Width < 80 || canvas.Height < 60) return;

                List<CycleTimeEntry> entries = CycleTimeStore.Snapshot();
                if (_mode == ViewMode.Cycle) DrawCycleDetail(e.Graphics, canvas, entries);
                else DrawGantt(e.Graphics, canvas, entries);
            }
            catch { /* 페인트 예외는 무해 처리 — 다음 틱에 재시도 */ }
        }

        private void DrawGantt(Graphics g, Rectangle canvas, List<CycleTimeEntry> entries)
        {
            _hits.Clear();
            string[] rows = _mode == ViewMode.Picker ? PickerRows : UnitRows;
            int labelW = _mode == ViewMode.Picker ? 56 : 98;
            const int statW = 150;
            const int axisH = 20;
            const int legendH = 22;
            Rectangle plot = new Rectangle(canvas.X + labelW, canvas.Y + legendH,
                canvas.Width - labelW - statW, canvas.Height - axisH - legendH);
            if (plot.Width < 40 || plot.Height < 40) return;

            long nowTick = _paused ? _pauseTick : Stopwatch.GetTimestamp();
            double pxPerMs = plot.Width / (double)_windowMs;
            float rowH = plot.Height / (float)rows.Length;
            _lastPlot = plot;   // 클릭 → 시간 환산용

            // 유닛 모드는 행 내부를 픽커별 8레인으로 분리.
            int laneCount = _mode == ViewMode.Unit ? 8 : 1;

            using (var fontRow = new Font("맑은 고딕", 9F, FontStyle.Bold))
            using (var fontSmall = new Font("맑은 고딕", 7.5F))
            using (var gridPen = new Pen(GridLine))
            {
                // 범례(유닛 색)
                int lx = plot.X;
                foreach (string unit in UnitRows)
                {
                    using (var brush = new SolidBrush(UnitColor(unit)))
                        g.FillRectangle(brush, lx, canvas.Y + 5, 10, 10);
                    TextRenderer.DrawText(g, UnitDisplay(unit), fontSmall, new Point(lx + 13, canvas.Y + 3), RowLabelFg);
                    lx += 13 + TextRenderer.MeasureText(UnitDisplay(unit), fontSmall).Width + 12;
                }

                // 세로 시간 그리드(6분할) + 축 라벨
                for (int i = 0; i <= 6; i++)
                {
                    int x = plot.X + (int)(plot.Width * i / 6.0);
                    g.DrawLine(gridPen, x, plot.Y, x, plot.Bottom);
                    double secAgo = _windowMs * (6 - i) / 6.0 / 1000.0;
                    string label = i == 6 ? Lang.T("diagram.cycle.now") : "-" + secAgo.ToString("0") + "s";
                    TextRenderer.DrawText(g, label, fontSmall, new Point(x - 12, plot.Bottom + 3), AxisFg);
                }

                // 행 라벨/구분선 + 통계
                var lastMs = new double[rows.Length];
                var sumMs = new double[rows.Length];
                var cntMs = new int[rows.Length];
                var lastSeq = new long[rows.Length];
                for (int r = 0; r < rows.Length; r++) { lastMs[r] = -1; lastSeq[r] = -1; }

                foreach (CycleTimeEntry entry in entries)
                {
                    int row = RowOf(entry, rows);
                    if (row < 0) continue;
                    double ageMs = TickToMs(nowTick - entry.StartTick);
                    if (ageMs > _windowMs + 60000) continue;
                    if (entry.IsCompleted)
                    {
                        double endAge = ageMs - entry.TotalMs;
                        if (endAge <= _windowMs)
                        {
                            sumMs[row] += entry.TotalMs; cntMs[row]++;
                            if (entry.Seq > lastSeq[row]) { lastSeq[row] = entry.Seq; lastMs[row] = entry.TotalMs; }
                        }
                    }
                    DrawBar(g, plot, pxPerMs, rowH, laneCount, row, ageMs, entry);
                }

                for (int r = 0; r < rows.Length; r++)
                {
                    float y = plot.Y + r * rowH;
                    g.DrawLine(gridPen, canvas.X, y, plot.Right + statW, y);
                    TextRenderer.DrawText(g, _mode == ViewMode.Unit ? UnitDisplay(rows[r]) : rows[r], fontRow,
                        new Rectangle(canvas.X, (int)y, labelW - 6, (int)rowH),
                        RowLabelFg, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                    if (laneCount > 1)
                    {
                        // 픽커 레인 안내 — 희미한 구분선 + F1~R4 미니 라벨
                        float laneH = rowH / laneCount;
                        using (var lanePen = new Pen(Color.FromArgb(0x1c, 0x21, 0x28)))
                        using (var laneFont = new Font("맑은 고딕", 6.5F))
                        {
                            for (int lane = 0; lane < laneCount; lane++)
                            {
                                float ly = y + lane * laneH;
                                if (lane > 0) g.DrawLine(lanePen, plot.X, ly, plot.Right, ly);
                                TextRenderer.DrawText(g, PickerLabel(lane + 1), laneFont,
                                    new Point(plot.X + 2, (int)(ly + laneH / 2 - 6)),
                                    Color.FromArgb(0x55, 0x62, 0x72));
                            }
                        }
                    }
                    string stat = lastMs[r] >= 0
                        ? Lang.Format("diagram.cycle.statistics", lastMs[r].ToString("0"), cntMs[r] > 0 ? (sumMs[r] / cntMs[r]).ToString("0") : "-")
                        : "-";
                    TextRenderer.DrawText(g, stat, fontSmall,
                        new Rectangle(plot.Right + 6, (int)y, statW - 8, (int)rowH),
                        RowLabelFg, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                }
                g.DrawLine(gridPen, plot.X, plot.Bottom, plot.Right, plot.Bottom);

                DrawMeasureMarkers(g, plot, pxPerMs, nowTick, fontRow);

                if (entries.Count == 0)
                    TextRenderer.DrawText(g, Lang.T("diagram.cycle.empty"), fontRow, plot,
                        AxisFg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        // ── 구간 측정 마커(클릭→클릭) — 세로선 2개 + 반투명 밴드 + Δms 표시 ──
        private void DrawMeasureMarkers(Graphics g, Rectangle plot, double pxPerMs, long nowTick, Font font)
        {
            if (_markStartTick < 0 && _markEndTick < 0) return;
            float x1 = MarkerX(plot, pxPerMs, nowTick, _markStartTick);
            float x2 = MarkerX(plot, pxPerMs, nowTick, _markEndTick);
            if (_markStartTick >= 0 && _markEndTick >= 0)
            {
                float left = Math.Max(plot.X, Math.Min(x1, x2));
                float right = Math.Min(plot.Right, Math.Max(x1, x2));
                if (right > left)
                    using (var band = new SolidBrush(Color.FromArgb(0x28, 0xd9, 0x77, 0x06)))
                        g.FillRectangle(band, left, plot.Y, right - left, plot.Height);
                double deltaMs = Math.Abs(TickToMs(_markEndTick - _markStartTick));
                string text = deltaMs.ToString("0.0") + " ms";
                var size = TextRenderer.MeasureText(text, font);
                float tx = Math.Max(plot.X, Math.Min(plot.Right - size.Width, (left + right) / 2 - size.Width / 2));
                TextRenderer.DrawText(g, text, font, new Point((int)tx, plot.Y + 2), Color.FromArgb(0xff, 0xb1, 0x4d));
            }
            using (var pen = new Pen(Color.FromArgb(0xd9, 0x77, 0x06), 1.5f) { DashStyle = DashStyle.Dash })
            {
                if (_markStartTick >= 0 && x1 >= plot.X && x1 <= plot.Right) g.DrawLine(pen, x1, plot.Y, x1, plot.Bottom);
                if (_markEndTick >= 0 && x2 >= plot.X && x2 <= plot.Right) g.DrawLine(pen, x2, plot.Y, x2, plot.Bottom);
            }
        }

        private float MarkerX(Rectangle plot, double pxPerMs, long nowTick, long markTick)
        {
            if (markTick < 0) return float.MinValue;
            return plot.Right - (float)(TickToMs(nowTick - markTick) * pxPerMs);
        }

        private void OnCanvasMouseDown(object sender, MouseEventArgs e)
        {
            if (_mode == ViewMode.Cycle) return;
            if (e.Button == MouseButtons.Right)
            {
                _markStartTick = -1; _markEndTick = -1;
                UpdateMeasureText();
                _lblMeasure.ForeColor = Color.FromArgb(0x9f, 0xb2, 0xc8);
                Invalidate();
                return;
            }
            if (e.Button != MouseButtons.Left) return;
            if (_lastPlot.IsEmpty || !_lastPlot.Contains(e.Location)) return;
            long nowTick = _paused ? _pauseTick : Stopwatch.GetTimestamp();
            double ageMs = (_lastPlot.Right - e.X) * _windowMs / (double)_lastPlot.Width;
            long clickTick = nowTick - (long)(ageMs / 1000.0 * Stopwatch.Frequency);
            if (_markStartTick < 0 || _markEndTick >= 0)
            {
                _markStartTick = clickTick;   // 새 구간 시작(기존 구간은 리셋)
                _markEndTick = -1;
                UpdateMeasureText();
                _lblMeasure.ForeColor = Color.FromArgb(0x9f, 0xb2, 0xc8);
            }
            else
            {
                _markEndTick = clickTick;
                UpdateMeasureText();
                _lblMeasure.ForeColor = Color.FromArgb(0xff, 0xb1, 0x4d);
            }
            Invalidate();
        }

        private int RowOf(CycleTimeEntry entry, string[] rows)
        {
            if (_mode == ViewMode.Picker)
            {
                int picker = entry.Picker;
                return picker >= 1 && picker <= 8 ? picker - 1 : -1;
            }
            return Array.IndexOf(rows, entry.Unit);
        }

        private void DrawBar(Graphics g, Rectangle plot, double pxPerMs, float rowH, int laneCount,
            int row, double ageMs, CycleTimeEntry entry)
        {
            // 60초 넘게 미완료인 고아 항목(계측 누락 등)은 화면을 덮는 거대 바가 되므로 표시하지 않는다.
            if (!entry.IsCompleted && ageMs > 60000) return;
            double durMs = entry.IsCompleted ? entry.TotalMs : ageMs;
            if (durMs < 0) return;
            float xEnd = plot.Right - (float)((ageMs - durMs) * pxPerMs);
            float xStart = plot.Right - (float)(ageMs * pxPerMs);
            if (xEnd <= plot.X || xStart >= plot.Right) return;
            xStart = Math.Max(xStart, plot.X);
            xEnd = Math.Min(xEnd, plot.Right);
            float width = Math.Max(2f, xEnd - xStart);
            float y, h;
            if (laneCount > 1 && entry.Picker >= 1 && entry.Picker <= laneCount)
            {
                // 픽커별 서브레인 — 같은 행에서도 픽커마다 Y가 다르다.
                float laneH = rowH / laneCount;
                y = plot.Y + row * rowH + (entry.Picker - 1) * laneH + laneH * 0.15f;
                h = laneH * 0.70f;
            }
            else
            {
                y = plot.Y + row * rowH + rowH * 0.22f;
                h = rowH * 0.56f;
            }
            Color baseColor = _mode == ViewMode.Unit ? PickerColor(entry.Picker) : UnitColor(entry.Unit);
            var barRect = new RectangleF(xStart, y, width, h);
            using (var dimBrush = new SolidBrush(Dim(baseColor, 0.35)))
                g.FillRectangle(dimBrush, barRect);

            // 모터(축) 세그먼트 — 축별 색으로 동작 시작~종료 구간을 표시 (사용자 지시 3).
            for (int i = 0; i < entry.Motions.Count; i++)
            {
                CycleMotionSegment seg = entry.Motions[i];
                if (seg == null) continue;
                double toMs = seg.EndMs >= 0 ? seg.EndMs : durMs;   // 진행 중 세그먼트는 현재까지
                DrawSegment(g, plot, pxPerMs, ageMs, y, h, seg.StartMs, toMs, MotionColor(seg.Axis));
            }

            if (entry.Failed)
                using (var pen = new Pen(Color.Red, 1.5f))
                    g.DrawRectangle(pen, barRect.X, barRect.Y, barRect.Width, barRect.Height);
            _hits.Add(new BarHit
            {
                Rect = Rectangle.Round(new RectangleF(barRect.X, barRect.Y - 2, barRect.Width, barRect.Height + 4)),
                Entry = entry
            });
        }

        private void DrawSegment(Graphics g, Rectangle plot, double pxPerMs, double ageMs,
            float y, float h, double fromMs, double toMs, Color color)
        {
            if (fromMs < 0 || toMs < 0 || toMs <= fromMs) return;
            float x1 = plot.Right - (float)((ageMs - fromMs) * pxPerMs);
            float x2 = plot.Right - (float)((ageMs - toMs) * pxPerMs);
            x1 = Math.Max(x1, plot.X);
            x2 = Math.Min(x2, plot.Right);
            if (x2 <= x1) return;
            using (var brush = new SolidBrush(color))
                g.FillRectangle(brush, x1, y + h * 0.18f, Math.Max(1.5f, x2 - x1), h * 0.64f);
        }

        // ── 사이클(개별) 모드 ──
        private void RefreshCycleList()
        {
            List<CycleTimeEntry> entries = CycleTimeStore.Snapshot();
            long newestSeq = entries.Count > 0 ? entries[entries.Count - 1].Seq : 0;
            if (newestSeq == _lastListRefreshSeq) return;
            _lastListRefreshSeq = newestSeq;
            var recent = entries.Where(x => x.IsCompleted).OrderByDescending(x => x.Seq).Take(100).ToList();
            _lstCycles.BeginUpdate();
            try
            {
                _lstCycles.Items.Clear();
                _listedEntries.Clear();
                foreach (CycleTimeEntry entry in recent)
                {
                    _listedEntries[entry.Seq] = entry;
                    var item = new ListViewItem(entry.StartUtc.ToLocalTime().ToString("HH:mm:ss.f"));
                    item.SubItems.Add(UnitDisplay(entry.Unit));
                    item.SubItems.Add(entry.DieIndex.ToString());
                    item.SubItems.Add(PickerLabel(entry.Picker));
                    item.SubItems.Add(entry.TotalMs.ToString("0"));
                    item.SubItems.Add(ResultDisplay(entry.Failed));
                    item.ForeColor = entry.Failed ? Color.FromArgb(0xff, 0x6b, 0x6b) : Color.Gainsboro;
                    item.Tag = entry.Seq;
                    _lstCycles.Items.Add(item);
                }
            }
            finally { _lstCycles.EndUpdate(); }
        }

        private void DrawCycleDetail(Graphics g, Rectangle canvas, List<CycleTimeEntry> entries)
        {
            _hits.Clear();
            CycleTimeEntry entry = null;
            for (int i = entries.Count - 1; i >= 0; i--)
                if (entries[i].Seq == _selectedCycleSeq) { entry = entries[i]; break; }
            using (var fontHead = new Font("맑은 고딕", 10F, FontStyle.Bold))
            using (var fontRow = new Font("맑은 고딕", 9F))
            {
                if (entry == null)
                {
                    TextRenderer.DrawText(g, Lang.T("diagram.cycle.selectDetail"), fontHead,
                        canvas, AxisFg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    return;
                }
                int x = canvas.X + 18, y = canvas.Y + 14;
                TextRenderer.DrawText(g,
                    Lang.Format("diagram.cycle.detail", UnitDisplay(entry.Unit), entry.Motion, entry.DieIndex, PickerLabel(entry.Picker), ResultDisplay(entry.Failed), entry.TotalMs.ToString("0.0")),
                    fontHead, new Point(x, y), Color.White);
                y += 34;
                // 모터 세그먼트 스택 바
                double total = Math.Max(1.0, entry.TotalMs);
                float barW = canvas.Width - 36;
                float barH = 34;
                Color baseColor = UnitColor(entry.Unit);
                using (var back = new SolidBrush(Dim(baseColor, 0.2)))
                    g.FillRectangle(back, x, y, barW, barH);
                for (int i = 0; i < entry.Motions.Count; i++)
                {
                    CycleMotionSegment seg = entry.Motions[i];
                    if (seg == null) continue;
                    double toMs = seg.EndMs >= 0 ? seg.EndMs : entry.TotalMs;
                    float sx = x + (float)(seg.StartMs / total * barW);
                    float sw = (float)((toMs - seg.StartMs) / total * barW);
                    if (sw <= 0) continue;
                    using (var brush = new SolidBrush(MotionColor(seg.Axis)))
                        g.FillRectangle(brush, sx, y, Math.Max(1.5f, sw), barH);
                }
                y += (int)barH + 16;
                // 모터별 표 (동작 순서대로)
                for (int i = 0; i < entry.Motions.Count; i++)
                {
                    CycleMotionSegment seg = entry.Motions[i];
                    if (seg == null) continue;
                    double toMs = seg.EndMs >= 0 ? seg.EndMs : entry.TotalMs;
                    using (var brush = new SolidBrush(MotionColor(seg.Axis)))
                        g.FillRectangle(brush, x, y + 3, 10, 10);
                    string durText = (toMs - seg.StartMs).ToString("0.0") + " ms"
                        + "   (" + seg.StartMs.ToString("0.0") + " → " + toMs.ToString("0.0") + ")"
                        + (seg.EndMs < 0 ? Lang.T("diagram.cycle.unfinished") : "");
                    TextRenderer.DrawText(g, (seg.Axis ?? "-").PadRight(16) + durText, fontRow, new Point(x + 16, y), Color.Gainsboro);
                    y += 22;
                    if (y > canvas.Bottom - 60) break;   // 표가 화면을 넘으면 중단
                }
                TextRenderer.DrawText(g, Lang.Format("diagram.cycle.request", entry.RequestId), fontRow, new Point(x, y + 6), AxisFg);
                TextRenderer.DrawText(g, Lang.Format("diagram.cycle.motion", entry.Motion), fontRow, new Point(x, y + 26), AxisFg);
            }
        }

        private static double TickToMs(long ticks)
        {
            return ticks * 1000.0 / Stopwatch.Frequency;
        }

        // ── 툴팁 ──
        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            CycleTimeEntry hit = null;
            for (int i = _hits.Count - 1; i >= 0; i--)
                if (_hits[i].Rect.Contains(e.Location)) { hit = _hits[i].Entry; break; }
            if (ReferenceEquals(hit, _hoverEntry)) return;
            _hoverEntry = hit;
            if (hit == null) { HideTip(); return; }
            var text = new System.Text.StringBuilder();
            text.Append(Lang.Format("diagram.cycle.tooltipHeading", UnitDisplay(hit.Unit), hit.DieIndex, PickerLabel(hit.Picker)));
            text.Append("\n").Append(Lang.Format("diagram.cycle.total", hit.IsCompleted ? hit.TotalMs.ToString("0.0") + "ms" : Lang.T("diagram.cycle.running")));
            int shown = 0;
            for (int i = 0; i < hit.Motions.Count && shown < 6; i++)
            {
                CycleMotionSegment seg = hit.Motions[i];
                if (seg == null || seg.EndMs < 0) continue;
                text.Append('\n').Append(seg.Axis).Append(' ')
                    .Append((seg.EndMs - seg.StartMs).ToString("0.0")).Append("ms");
                shown++;
            }
            if (hit.Failed) text.Append("\n").Append(Lang.T("diagram.cycle.errorState"));
            text.Append("\n").Append(Lang.Format("diagram.cycle.request", hit.RequestId));
            _tip.Show(text.ToString(), this, e.X + 14, e.Y + 18, 4000);
        }

        private void HideTip()
        {
            _hoverEntry = null;
            try { _tip.Hide(this); } catch { }
        }
    }
}
