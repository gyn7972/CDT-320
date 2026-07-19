using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using QMC.Common.Diagnostics.TactTime;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    internal enum TactTimeChartViewMode
    {
        Trend,
        Timeline
    }

    internal sealed class TactTimeRecordSelectedEventArgs : EventArgs
    {
        public TactTimeRecordSelectedEventArgs(TactTimeRecord record)
        {
            Record = record;
        }

        public TactTimeRecord Record { get; private set; }
    }

    internal sealed class TactTimeChartControl : Control
    {
        private const int MaxTimelineBars = 15000;
        private const int HeaderHeight = 54;
        private const int AxisHeight = 32;
        private const int TimelineLabelWidth = 178;
        private const double MinimumViewSpan = 0.002;

        private readonly List<TactTimeRecord> _records = new List<TactTimeRecord>();
        private readonly List<ChartHitArea> _hitAreas = new List<ChartHitArea>();
        private readonly ToolTip _toolTip = new ToolTip();
        private Rectangle _lastPlotBounds;
        private DateTime _rangeStart = DateTime.MinValue;
        private DateTime _rangeEnd = DateTime.MinValue;
        private double _viewStartRatio;
        private double _viewEndRatio = 1.0;
        private bool _dragging;
        private int _dragStartX;
        private double _dragStartViewStart;
        private double _dragStartViewEnd;
        private TactTimeRecord _selectedRecord;
        private TactTimeRecord _hoverRecord;
        private TactTimeChartViewMode _viewMode = TactTimeChartViewMode.Trend;
        private string _emptyMessage = "표시할 택타임 기록이 없습니다.";

        public TactTimeChartControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.UserPaint,
                true);
            BackColor = Color.White;
            TabStop = true;
            _toolTip.AutoPopDelay = 12000;
            _toolTip.InitialDelay = 250;
            _toolTip.ReshowDelay = 100;
        }

        public event EventHandler<TactTimeRecordSelectedEventArgs> RecordSelected;

        public TactTimeChartViewMode ViewMode
        {
            get { return _viewMode; }
            set
            {
                if (_viewMode == value)
                    return;

                _viewMode = value;
                ResetView();
            }
        }

        public string EmptyMessage
        {
            get { return _emptyMessage; }
            set
            {
                _emptyMessage = string.IsNullOrWhiteSpace(value)
                    ? "표시할 택타임 기록이 없습니다."
                    : value;
                Invalidate();
            }
        }

        public void SetRecords(IEnumerable<TactTimeRecord> records)
        {
            UpdateRecords(records, false);
        }

        public void UpdateRecords(IEnumerable<TactTimeRecord> records, bool preserveView)
        {
            DateTime previousViewStart = DateTime.MinValue;
            DateTime previousViewEnd = DateTime.MinValue;
            bool hadRange = _records.Count > 0 && _rangeEnd > _rangeStart;
            if (hadRange)
                ResolveViewTimes(out previousViewStart, out previousViewEnd);
            double previousSpan = _viewEndRatio - _viewStartRatio;
            TactTimeRecord previousSelection = _selectedRecord;

            _records.Clear();
            if (records != null)
            {
                foreach (TactTimeRecord record in records)
                {
                    if (record == null || record.StartedAt == DateTime.MinValue || record.EndedAt == DateTime.MinValue)
                        continue;
                    _records.Add(record);
                }
            }

            _records.Sort((a, b) => a.StartedAt.CompareTo(b.StartedAt));
            ResolveFullRange();
            _selectedRecord = preserveView && previousSelection != null && _records.Contains(previousSelection)
                ? previousSelection
                : null;
            _hoverRecord = null;

            if (!preserveView || !hadRange || previousSpan >= 0.999 || _records.Count == 0)
            {
                ResetView();
                return;
            }

            double totalTicks = Math.Max(1.0, (_rangeEnd - _rangeStart).Ticks);
            double start = (previousViewStart - _rangeStart).Ticks / totalTicks;
            double end = (previousViewEnd - _rangeStart).Ticks / totalTicks;
            SetViewRange(start, end);
        }

        public void SetSelectedRecord(TactTimeRecord record, bool ensureVisible)
        {
            _selectedRecord = record;
            if (ensureVisible && record != null)
                EnsureRecordVisible(record);
            Invalidate();
        }

        public void ResetView()
        {
            _viewStartRatio = 0.0;
            _viewEndRatio = 1.0;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            graphics.Clear(BackColor);
            _hitAreas.Clear();

            Rectangle bounds = ClientRectangle;
            if (_records.Count == 0 || bounds.Width < 160 || bounds.Height < 120)
            {
                DrawEmptyState(graphics, bounds);
                return;
            }

            if (_viewMode == TactTimeChartViewMode.Timeline)
                DrawTimeline(graphics, bounds);
            else
                DrawTrend(graphics, bounds);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_records.Count == 0 || !_lastPlotBounds.Contains(e.Location))
                return;

            double span = _viewEndRatio - _viewStartRatio;
            double factor = e.Delta > 0 ? 0.75 : 1.3333333333;
            double newSpan = Math.Max(MinimumViewSpan, Math.Min(1.0, span * factor));
            double cursor = (e.X - _lastPlotBounds.Left) / (double)Math.Max(1, _lastPlotBounds.Width);
            double anchor = _viewStartRatio + span * cursor;
            double newStart = anchor - newSpan * cursor;
            SetViewRange(newStart, newStart + newSpan);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            ChartHitArea hit = FindHit(e.Location);
            if (hit != null)
            {
                SelectRecord(hit.Record);
                return;
            }

            if (e.Button == MouseButtons.Left && _lastPlotBounds.Contains(e.Location))
            {
                _dragging = true;
                _dragStartX = e.X;
                _dragStartViewStart = _viewStartRatio;
                _dragStartViewEnd = _viewEndRatio;
                Cursor = Cursors.Hand;
                Capture = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_dragging)
            {
                double span = _dragStartViewEnd - _dragStartViewStart;
                double delta = (_dragStartX - e.X) / (double)Math.Max(1, _lastPlotBounds.Width) * span;
                SetViewRange(_dragStartViewStart + delta, _dragStartViewEnd + delta);
                return;
            }

            ChartHitArea hit = FindHit(e.Location);
            TactTimeRecord record = hit != null ? hit.Record : null;
            if (!object.ReferenceEquals(_hoverRecord, record))
            {
                _hoverRecord = record;
                _toolTip.SetToolTip(this, record != null ? BuildToolTip(record) : "");
            }

            Cursor = record != null ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!_dragging)
                return;

            _dragging = false;
            Cursor = Cursors.Default;
            Capture = false;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!_dragging)
                Cursor = Cursors.Default;
            _hoverRecord = null;
            _toolTip.SetToolTip(this, "");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _toolTip.Dispose();
            base.Dispose(disposing);
        }

        private void DrawEmptyState(Graphics graphics, Rectangle bounds)
        {
            using (var titleFont = new Font("맑은 고딕", 12F, FontStyle.Bold))
            using (var bodyFont = new Font("맑은 고딕", 9F))
            using (var titleBrush = new SolidBrush(Color.FromArgb(64, 73, 82)))
            using (var bodyBrush = new SolidBrush(Color.FromArgb(118, 126, 134)))
            {
                string title = _viewMode == TactTimeChartViewMode.Timeline ? "장비 타임라인" : "택타임 추이";
                graphics.DrawString(title, titleFont, titleBrush, 18, 16);
                graphics.DrawString(_emptyMessage, bodyFont, bodyBrush, 18, 48);
            }
        }

        private void DrawTimeline(Graphics graphics, Rectangle bounds)
        {
            List<string> lanes = BuildOrderedLanes();
            Rectangle plot = new Rectangle(
                TimelineLabelWidth,
                HeaderHeight,
                Math.Max(1, bounds.Width - TimelineLabelWidth - 18),
                Math.Max(1, bounds.Height - HeaderHeight - AxisHeight));
            _lastPlotBounds = plot;

            DateTime viewStart;
            DateTime viewEnd;
            ResolveViewTimes(out viewStart, out viewEnd);
            double viewMs = Math.Max(1.0, (viewEnd - viewStart).TotalMilliseconds);

            DrawChartHeader(graphics, bounds, "장비 타임라인", viewStart, viewEnd);
            DrawTimelineBackground(graphics, plot, lanes);
            DrawTimeAxis(graphics, plot, viewStart, viewEnd);

            int laneHeight = Math.Max(46, plot.Height / Math.Max(1, lanes.Count));
            int visibleCount = CountVisibleRecords(viewStart, viewEnd);
            int stride = Math.Max(1, (int)Math.Ceiling(visibleCount / (double)MaxTimelineBars));
            int visibleIndex = 0;
            var renderRecords = new List<TactTimeRecord>(Math.Min(visibleCount, MaxTimelineBars));

            for (int i = 0; i < _records.Count; i++)
            {
                TactTimeRecord record = _records[i];
                if (record.EndedAt < viewStart || record.StartedAt > viewEnd)
                    continue;

                bool mustRender = record.Result != TactTimeResult.Ok || object.ReferenceEquals(record, _selectedRecord);
                if (!mustRender && visibleIndex++ % stride != 0)
                    continue;

                renderRecords.Add(record);
            }

            Dictionary<TactTimeRecord, TimelineSlot> slots = BuildTimelineSlots(renderRecords, lanes);

            Region previousClip = graphics.Clip;
            graphics.SetClip(plot);
            try
            {
                for (int i = 0; i < renderRecords.Count; i++)
                {
                    TactTimeRecord record = renderRecords[i];
                    string laneName = ResolveLaneName(record);
                    int laneIndex = lanes.IndexOf(laneName);
                    if (laneIndex < 0)
                        continue;

                    TimelineSlot slot;
                    if (!slots.TryGetValue(record, out slot))
                        slot = new TimelineSlot { RowIndex = 0, RowCount = 1 };

                    int availableHeight = Math.Max(4, laneHeight - 8);
                    int rowTop = availableHeight * slot.RowIndex / Math.Max(1, slot.RowCount);
                    int rowBottom = availableHeight * (slot.RowIndex + 1) / Math.Max(1, slot.RowCount);
                    int barHeight = Math.Max(2, Math.Min(14, rowBottom - rowTop - 1));
                    int y = plot.Top + laneIndex * laneHeight + 4 + rowTop;
                    int x = TimeToX(record.StartedAt, viewStart, viewMs, plot);
                    int endX = TimeToX(record.EndedAt, viewStart, viewMs, plot);
                    int width = Math.Max(3, endX - x);
                    var bar = new Rectangle(x, y, width, barHeight);
                    DrawRecordBar(graphics, record, bar);
                }
            }
            finally
            {
                graphics.Clip = previousClip;
                previousClip.Dispose();
            }

            if (stride > 1)
                DrawSimplifiedBadge(graphics, bounds, renderRecords.Count, visibleCount);
        }

        private void DrawTrend(Graphics graphics, Rectangle bounds)
        {
            Rectangle plot = new Rectangle(
                74,
                HeaderHeight,
                Math.Max(1, bounds.Width - 94),
                Math.Max(1, bounds.Height - HeaderHeight - AxisHeight));
            _lastPlotBounds = plot;

            DateTime viewStart;
            DateTime viewEnd;
            ResolveViewTimes(out viewStart, out viewEnd);
            double viewMs = Math.Max(1.0, (viewEnd - viewStart).TotalMilliseconds);
            List<TactTimeRecord> visible = _records
                .Where(x => x.EndedAt >= viewStart && x.StartedAt <= viewEnd)
                .ToList();

            if (visible.Count == 0)
            {
                DrawChartHeader(graphics, bounds, "택타임 추이", viewStart, viewEnd);
                DrawPlotEmpty(graphics, plot, "선택한 시간 구간에 기록이 없습니다.");
                return;
            }

            List<TactTimeRecord> trendRecords = ResolveTrendMetricRecords(visible);
            bool containersExcluded = trendRecords.Count != visible.Count;
            DrawChartHeader(
                graphics,
                bounds,
                containersExcluded ? "택타임 추이 · Run/Unit 중첩 제외" : "택타임 추이",
                viewStart,
                viewEnd);

            double maxMs = Math.Max(1.0, trendRecords.Max(x => Math.Max(0, x.ElapsedMs)));
            double average = trendRecords.Average(x => (double)Math.Max(0, x.ElapsedMs));
            double p95 = CalculateNearestRankPercentile(trendRecords, 0.95);
            bool mixedSeries = trendRecords.Select(ResolveTrendSeriesName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Skip(1)
                .Any();
            string mixedSuffix = mixedSeries ? " (혼합)" : "";
            maxMs = Math.Max(maxMs, p95) * 1.12;

            DrawTrendBackground(graphics, plot, maxMs, viewStart, viewEnd);
            DrawReferenceLine(graphics, plot, average, maxMs, "AVG" + mixedSuffix + " " + FormatDuration(average), Color.FromArgb(37, 126, 196));
            DrawReferenceLine(graphics, plot, p95, maxMs, "P95" + mixedSuffix + " " + FormatDuration(p95), Color.FromArgb(205, 91, 52));

            List<IGrouping<string, TactTimeRecord>> series = SelectTrendSeries(trendRecords, 8);
            Color[] palette = GetTrendPalette();

            Region previousClip = graphics.Clip;
            graphics.SetClip(plot);
            try
            {
                for (int seriesIndex = 0; seriesIndex < series.Count; seriesIndex++)
                {
                    List<TactTimeRecord> renderPoints = BuildTrendRenderPoints(
                        series[seriesIndex].OrderBy(x => x.EndedAt).ToList(),
                        plot.Width,
                        viewStart,
                        viewEnd);
                    Color seriesColor = palette[seriesIndex % palette.Length];
                    DrawTrendSeries(graphics, plot, renderPoints, viewStart, viewMs, maxMs, seriesColor);
                }
            }
            finally
            {
                graphics.Clip = previousClip;
                previousClip.Dispose();
            }

            DrawTrendLegend(graphics, bounds, series, palette);
        }

        private void DrawChartHeader(Graphics graphics, Rectangle bounds, string title, DateTime viewStart, DateTime viewEnd)
        {
            using (var titleFont = new Font("맑은 고딕", 11F, FontStyle.Bold))
            using (var detailFont = new Font("맑은 고딕", 8.5F))
            using (var titleBrush = new SolidBrush(Color.FromArgb(48, 57, 66)))
            using (var detailBrush = new SolidBrush(Color.FromArgb(105, 113, 121)))
            {
                graphics.DrawString(title, titleFont, titleBrush, 14, 10);
                string range = viewStart.ToString("HH:mm:ss.fff") + " ~ " + viewEnd.ToString("HH:mm:ss.fff") +
                               "  (" + FormatDuration((viewEnd - viewStart).TotalMilliseconds) + ")";
                graphics.DrawString(range, detailFont, detailBrush, 14, 33);

                string help = "휠: 확대/축소   드래그: 이동   막대/점: 상세 선택";
                SizeF size = graphics.MeasureString(help, detailFont);
                graphics.DrawString(help, detailFont, detailBrush, Math.Max(14, bounds.Width - size.Width - 14), 12);
            }
        }

        private void DrawTimelineBackground(Graphics graphics, Rectangle plot, List<string> lanes)
        {
            int laneHeight = Math.Max(46, plot.Height / Math.Max(1, lanes.Count));
            using (var labelFont = new Font("맑은 고딕", 8.5F, FontStyle.Bold))
            using (var labelBrush = new SolidBrush(Color.FromArgb(55, 63, 71)))
            using (var linePen = new Pen(Color.FromArgb(221, 225, 229)))
            {
                for (int i = 0; i < lanes.Count; i++)
                {
                    int y = plot.Top + i * laneHeight;
                    if (i % 2 == 0)
                    {
                        using (var bandBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                            graphics.FillRectangle(bandBrush, 0, y, ClientSize.Width, laneHeight);
                    }

                    graphics.DrawLine(linePen, 0, y + laneHeight, ClientSize.Width, y + laneHeight);
                    graphics.DrawString(lanes[i], labelFont, labelBrush, 10, y + Math.Max(4, laneHeight / 2 - 9));
                }

                graphics.DrawLine(linePen, plot.Left - 1, plot.Top, plot.Left - 1, plot.Bottom);
            }

            DrawCategoryLegend(graphics, plot.Right - 480, plot.Top - 24);
        }

        private void DrawTimeAxis(Graphics graphics, Rectangle plot, DateTime viewStart, DateTime viewEnd)
        {
            using (var gridPen = new Pen(Color.FromArgb(224, 228, 232)))
            using (var axisPen = new Pen(Color.FromArgb(151, 159, 167)))
            using (var font = new Font("맑은 고딕", 8F))
            using (var brush = new SolidBrush(Color.FromArgb(89, 97, 105)))
            {
                graphics.DrawRectangle(axisPen, plot);
                for (int i = 0; i <= 8; i++)
                {
                    int x = plot.Left + (int)Math.Round(plot.Width * i / 8.0);
                    graphics.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
                    DateTime time = viewStart.AddTicks((long)((viewEnd - viewStart).Ticks * i / 8.0));
                    string label = time.ToString("HH:mm:ss.fff");
                    SizeF size = graphics.MeasureString(label, font);
                    graphics.DrawString(label, font, brush, x - size.Width / 2, plot.Bottom + 5);
                }
            }
        }

        private void DrawTrendBackground(Graphics graphics, Rectangle plot, double maxMs, DateTime viewStart, DateTime viewEnd)
        {
            using (var gridPen = new Pen(Color.FromArgb(226, 230, 234)))
            using (var axisPen = new Pen(Color.FromArgb(151, 159, 167)))
            using (var font = new Font("맑은 고딕", 8F))
            using (var brush = new SolidBrush(Color.FromArgb(89, 97, 105)))
            {
                graphics.DrawRectangle(axisPen, plot);
                for (int i = 0; i <= 5; i++)
                {
                    int y = plot.Bottom - (int)Math.Round(plot.Height * i / 5.0);
                    graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);
                    string label = FormatDuration(maxMs * i / 5.0);
                    SizeF size = graphics.MeasureString(label, font);
                    graphics.DrawString(label, font, brush, plot.Left - size.Width - 7, y - size.Height / 2);
                }

                for (int i = 0; i <= 8; i++)
                {
                    int x = plot.Left + (int)Math.Round(plot.Width * i / 8.0);
                    graphics.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
                    DateTime time = viewStart.AddTicks((long)((viewEnd - viewStart).Ticks * i / 8.0));
                    string label = time.ToString("HH:mm:ss");
                    SizeF size = graphics.MeasureString(label, font);
                    graphics.DrawString(label, font, brush, x - size.Width / 2, plot.Bottom + 5);
                }
            }
        }

        private void DrawReferenceLine(Graphics graphics, Rectangle plot, double value, double maxMs, string label, Color color)
        {
            int y = ValueToY(value, maxMs, plot);
            using (var pen = new Pen(color, 1.4F))
            using (var font = new Font("맑은 고딕", 8F, FontStyle.Bold))
            using (var brush = new SolidBrush(color))
            {
                pen.DashStyle = DashStyle.Dash;
                graphics.DrawLine(pen, plot.Left, y, plot.Right, y);
                SizeF size = graphics.MeasureString(label, font);
                graphics.DrawString(label, font, brush, plot.Right - size.Width - 4, y - size.Height - 1);
            }
        }

        private void DrawTrendSeries(
            Graphics graphics,
            Rectangle plot,
            List<TactTimeRecord> points,
            DateTime viewStart,
            double viewMs,
            double maxMs,
            Color color)
        {
            if (points.Count == 0)
                return;

            using (var pen = new Pen(color, 1.8F))
            using (var pointBrush = new SolidBrush(color))
            {
                Point? previous = null;
                for (int i = 0; i < points.Count; i++)
                {
                    TactTimeRecord record = points[i];
                    int x = TimeToX(record.EndedAt, viewStart, viewMs, plot);
                    int y = ValueToY(Math.Max(0, record.ElapsedMs), maxMs, plot);
                    var current = new Point(x, y);
                    if (previous.HasValue)
                        graphics.DrawLine(pen, previous.Value, current);

                    Color pointColor = ResolveResultColor(record.Result, color);
                    int radius = object.ReferenceEquals(record, _selectedRecord) ? 5 : (points.Count <= 400 ? 3 : 2);
                    using (var resultBrush = new SolidBrush(pointColor))
                        graphics.FillEllipse(resultBrush, x - radius, y - radius, radius * 2, radius * 2);
                    if (object.ReferenceEquals(record, _selectedRecord))
                        graphics.DrawEllipse(Pens.Black, x - radius - 2, y - radius - 2, (radius + 2) * 2, (radius + 2) * 2);

                    _hitAreas.Add(new ChartHitArea
                    {
                        Bounds = new Rectangle(x - 6, y - 6, 12, 12),
                        Record = record
                    });
                    previous = current;
                }
            }
        }

        private void DrawRecordBar(Graphics graphics, TactTimeRecord record, Rectangle bar)
        {
            Color color = ResolveCategoryColor(record);
            using (var brush = new SolidBrush(color))
                graphics.FillRectangle(brush, bar);

            using (var borderPen = new Pen(object.ReferenceEquals(record, _selectedRecord) ? Color.Black : Darken(color),
                object.ReferenceEquals(record, _selectedRecord) ? 2F : 1F))
                graphics.DrawRectangle(borderPen, bar);

            if (bar.Width >= 88 && bar.Height >= 10)
            {
                string label = ResolveRecordLabel(record) + "  " + FormatDuration(record.ElapsedMs);
                using (var font = new Font("맑은 고딕", 7.5F, FontStyle.Bold))
                using (var brush = new SolidBrush(ResolveTextColor(color)))
                {
                    Rectangle textBounds = new Rectangle(bar.X + 3, bar.Y, bar.Width - 6, bar.Height);
                    TextRenderer.DrawText(graphics, label, font, textBounds, brush.Color,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                }
            }

            _hitAreas.Add(new ChartHitArea { Bounds = bar, Record = record });
        }

        private void DrawCategoryLegend(Graphics graphics, int x, int y)
        {
            var items = new[]
            {
                new LegendItem("Unit", ResolveCategoryColor(TactTimeCategory.Unit, TactTimeResult.Ok)),
                new LegendItem("Process", ResolveCategoryColor(TactTimeCategory.Process, TactTimeResult.Ok)),
                new LegendItem("Vision", ResolveCategoryColor(TactTimeCategory.Vision, TactTimeResult.Ok)),
                new LegendItem("Motion", ResolveCategoryColor(TactTimeCategory.Motion, TactTimeResult.Ok)),
                new LegendItem("Wait", ResolveCategoryColor(TactTimeCategory.Wait, TactTimeResult.Ok)),
                new LegendItem("Fail", ResolveCategoryColor(TactTimeCategory.Process, TactTimeResult.Failed))
            };

            using (var font = new Font("맑은 고딕", 7.5F))
            using (var brush = new SolidBrush(Color.FromArgb(75, 83, 91)))
            {
                int currentX = Math.Max(TimelineLabelWidth, x);
                for (int i = 0; i < items.Length; i++)
                {
                    using (var colorBrush = new SolidBrush(items[i].Color))
                        graphics.FillRectangle(colorBrush, currentX, y + 4, 11, 8);
                    graphics.DrawString(items[i].Text, font, brush, currentX + 14, y);
                    currentX += 68;
                }
            }
        }

        private void DrawTrendLegend(
            Graphics graphics,
            Rectangle bounds,
            List<IGrouping<string, TactTimeRecord>> series,
            Color[] palette)
        {
            using (var font = new Font("맑은 고딕", 7.5F))
            using (var textBrush = new SolidBrush(Color.FromArgb(75, 83, 91)))
            {
                int x = 330;
                int y = 32;
                int maxX = bounds.Right - 12;
                for (int i = 0; i < series.Count; i++)
                {
                    string name = Shorten(series[i].Key, 24);
                    int width = TextRenderer.MeasureText(name, font).Width + 24;
                    if (x + width > maxX)
                        break;

                    using (var colorBrush = new SolidBrush(palette[i % palette.Length]))
                        graphics.FillEllipse(colorBrush, x, y + 3, 9, 9);
                    graphics.DrawString(name, font, textBrush, x + 12, y);
                    x += width;
                }
            }
        }

        private void DrawSimplifiedBadge(Graphics graphics, Rectangle bounds, int rendered, int visible)
        {
            string text = "표시 최적화: " + rendered.ToString("N0") + " / " + visible.ToString("N0") + "건";
            using (var font = new Font("맑은 고딕", 8F, FontStyle.Bold))
            {
                Size size = TextRenderer.MeasureText(text, font);
                var badge = new Rectangle(bounds.Right - size.Width - 22, bounds.Bottom - 27, size.Width + 12, 21);
                using (var brush = new SolidBrush(Color.FromArgb(238, 244, 249)))
                    graphics.FillRectangle(brush, badge);
                graphics.DrawRectangle(Pens.LightSlateGray, badge);
                TextRenderer.DrawText(graphics, text, font, badge, Color.FromArgb(70, 80, 90),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawPlotEmpty(Graphics graphics, Rectangle plot, string message)
        {
            using (var brush = new SolidBrush(Color.FromArgb(116, 124, 132)))
            using (var font = new Font("맑은 고딕", 9F))
                graphics.DrawString(message, font, brush, plot.Left + 12, plot.Top + 12);
        }

        private List<string> BuildOrderedLanes()
        {
            return _records
                .Select(ResolveLaneName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(ResolveLaneOrder)
                .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static Dictionary<TactTimeRecord, TimelineSlot> BuildTimelineSlots(
            List<TactTimeRecord> records,
            List<string> lanes)
        {
            var result = new Dictionary<TactTimeRecord, TimelineSlot>();
            for (int laneIndex = 0; laneIndex < lanes.Count; laneIndex++)
            {
                string lane = lanes[laneIndex];
                List<TactTimeRecord> laneRecords = records
                    .Where(x => string.Equals(ResolveLaneName(x), lane, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var laneAssignments = new List<TimelineAssignment>();
                int rowOffset = 0;

                foreach (IGrouping<int, TactTimeRecord> band in laneRecords
                    .GroupBy(x => ResolveTimelineBand(x.Category))
                    .OrderBy(x => x.Key))
                {
                    var trackEnds = new List<DateTime>();
                    foreach (TactTimeRecord record in band
                        .OrderBy(x => x.StartedAt)
                        .ThenByDescending(x => x.EndedAt))
                    {
                        int track = -1;
                        for (int i = 0; i < trackEnds.Count; i++)
                        {
                            if (trackEnds[i] <= record.StartedAt)
                            {
                                track = i;
                                break;
                            }
                        }

                        if (track < 0)
                        {
                            track = trackEnds.Count;
                            trackEnds.Add(record.EndedAt);
                        }
                        else
                        {
                            trackEnds[track] = record.EndedAt;
                        }

                        laneAssignments.Add(new TimelineAssignment
                        {
                            Record = record,
                            RowIndex = rowOffset + track
                        });
                    }

                    rowOffset += Math.Max(1, trackEnds.Count);
                }

                int rowCount = Math.Max(1, rowOffset);
                for (int i = 0; i < laneAssignments.Count; i++)
                {
                    TimelineAssignment assignment = laneAssignments[i];
                    result[assignment.Record] = new TimelineSlot
                    {
                        RowIndex = assignment.RowIndex,
                        RowCount = rowCount
                    };
                }
            }

            return result;
        }

        private static List<TactTimeRecord> ResolveTrendMetricRecords(List<TactTimeRecord> records)
        {
            List<TactTimeRecord> details = records
                .Where(x => x.Category != TactTimeCategory.Run && x.Category != TactTimeCategory.Unit)
                .ToList();
            return details.Count > 0 ? details : records;
        }

        private static List<IGrouping<string, TactTimeRecord>> SelectTrendSeries(
            List<TactTimeRecord> records,
            int limit)
        {
            List<IGrouping<string, TactTimeRecord>> groups = records
                .GroupBy(ResolveTrendSeriesName)
                .ToList();
            var selected = groups
                .OrderByDescending(x => x.Count())
                .Take(Math.Min(5, limit))
                .ToList();

            IGrouping<string, TactTimeRecord> maximumGroup = groups
                .OrderByDescending(x => x.Max(y => Math.Max(0, y.ElapsedMs)))
                .FirstOrDefault();
            if (maximumGroup != null && selected.Count < limit &&
                !selected.Any(x => string.Equals(x.Key, maximumGroup.Key, StringComparison.OrdinalIgnoreCase)))
                selected.Add(maximumGroup);

            foreach (IGrouping<string, TactTimeRecord> group in groups
                .OrderByDescending(x => x.Any(y => y.Result != TactTimeResult.Ok))
                .ThenByDescending(x => x.Max(y => Math.Max(0, y.ElapsedMs))))
            {
                if (selected.Count >= limit)
                    break;
                if (selected.Any(x => string.Equals(x.Key, group.Key, StringComparison.OrdinalIgnoreCase)))
                    continue;
                selected.Add(group);
            }

            return selected;
        }

        private List<TactTimeRecord> BuildTrendRenderPoints(
            List<TactTimeRecord> records,
            int plotWidth,
            DateTime viewStart,
            DateTime viewEnd)
        {
            List<TactTimeRecord> visible = records
                .Where(x => x.EndedAt >= viewStart && x.EndedAt <= viewEnd)
                .ToList();
            int limit = Math.Max(100, plotWidth * 2);
            if (visible.Count <= limit)
                return visible;

            double totalMs = Math.Max(1.0, (viewEnd - viewStart).TotalMilliseconds);
            var buckets = new Dictionary<int, TrendBucket>();
            for (int i = 0; i < visible.Count; i++)
            {
                TactTimeRecord record = visible[i];
                int bucketIndex = (int)Math.Max(0, Math.Min(plotWidth - 1,
                    (record.EndedAt - viewStart).TotalMilliseconds / totalMs * plotWidth));
                TrendBucket bucket;
                if (!buckets.TryGetValue(bucketIndex, out bucket))
                {
                    bucket = new TrendBucket { Minimum = record, Maximum = record };
                    buckets.Add(bucketIndex, bucket);
                }
                else
                {
                    if (record.ElapsedMs < bucket.Minimum.ElapsedMs)
                        bucket.Minimum = record;
                    if (record.ElapsedMs > bucket.Maximum.ElapsedMs)
                        bucket.Maximum = record;
                }
            }

            var result = new List<TactTimeRecord>(buckets.Count * 2);
            foreach (TrendBucket bucket in buckets.OrderBy(x => x.Key).Select(x => x.Value))
            {
                if (bucket.Minimum.EndedAt <= bucket.Maximum.EndedAt)
                {
                    result.Add(bucket.Minimum);
                    if (!object.ReferenceEquals(bucket.Minimum, bucket.Maximum))
                        result.Add(bucket.Maximum);
                }
                else
                {
                    result.Add(bucket.Maximum);
                    if (!object.ReferenceEquals(bucket.Minimum, bucket.Maximum))
                        result.Add(bucket.Minimum);
                }
            }

            return result;
        }

        private void ResolveFullRange()
        {
            if (_records.Count == 0)
            {
                _rangeStart = DateTime.MinValue;
                _rangeEnd = DateTime.MinValue;
                return;
            }

            _rangeStart = _records.Min(x => x.StartedAt);
            _rangeEnd = _records.Max(x => x.EndedAt);
            if (_rangeEnd <= _rangeStart)
                _rangeEnd = _rangeStart.AddMilliseconds(1);
        }

        private void ResolveViewTimes(out DateTime start, out DateTime end)
        {
            long ticks = Math.Max(1, (_rangeEnd - _rangeStart).Ticks);
            start = _rangeStart.AddTicks((long)(ticks * _viewStartRatio));
            end = _rangeStart.AddTicks((long)(ticks * _viewEndRatio));
            if (end <= start)
                end = start.AddMilliseconds(1);
        }

        private void SetViewRange(double start, double end)
        {
            double span = Math.Max(MinimumViewSpan, Math.Min(1.0, end - start));
            if (start < 0.0)
                start = 0.0;
            if (start + span > 1.0)
                start = 1.0 - span;

            _viewStartRatio = Math.Max(0.0, Math.Min(1.0 - span, start));
            _viewEndRatio = _viewStartRatio + span;
            Invalidate();
        }

        private void EnsureRecordVisible(TactTimeRecord record)
        {
            if (record == null || _rangeStart == DateTime.MinValue || _rangeEnd <= _rangeStart)
                return;

            double fullMs = (_rangeEnd - _rangeStart).TotalMilliseconds;
            double center = ((record.StartedAt - _rangeStart).TotalMilliseconds +
                             (record.EndedAt - _rangeStart).TotalMilliseconds) / 2.0 / fullMs;
            if (center >= _viewStartRatio && center <= _viewEndRatio)
                return;

            double span = _viewEndRatio - _viewStartRatio;
            SetViewRange(center - span / 2.0, center + span / 2.0);
        }

        private void SelectRecord(TactTimeRecord record)
        {
            if (record == null)
                return;

            _selectedRecord = record;
            Invalidate();
            EventHandler<TactTimeRecordSelectedEventArgs> handler = RecordSelected;
            if (handler != null)
                handler(this, new TactTimeRecordSelectedEventArgs(record));
        }

        private ChartHitArea FindHit(Point point)
        {
            for (int i = _hitAreas.Count - 1; i >= 0; i--)
            {
                if (_hitAreas[i].Bounds.Contains(point))
                    return _hitAreas[i];
            }

            return null;
        }

        private int CountVisibleRecords(DateTime start, DateTime end)
        {
            int count = 0;
            for (int i = 0; i < _records.Count; i++)
            {
                if (_records[i].EndedAt >= start && _records[i].StartedAt <= end)
                    count++;
            }

            return count;
        }

        private static int TimeToX(DateTime time, DateTime viewStart, double viewMs, Rectangle plot)
        {
            double ratio = (time - viewStart).TotalMilliseconds / viewMs;
            return plot.Left + (int)Math.Round(ratio * plot.Width);
        }

        private static int ValueToY(double value, double maxValue, Rectangle plot)
        {
            double ratio = maxValue <= 0.0 ? 0.0 : Math.Max(0.0, Math.Min(1.0, value / maxValue));
            return plot.Bottom - (int)Math.Round(ratio * plot.Height);
        }

        private static int ResolveTimelineBand(TactTimeCategory category)
        {
            switch (category)
            {
                case TactTimeCategory.Run:
                case TactTimeCategory.Unit:
                    return 0;
                case TactTimeCategory.Process:
                case TactTimeCategory.Vision:
                    return 1;
                case TactTimeCategory.Motion:
                case TactTimeCategory.IO:
                    return 2;
                case TactTimeCategory.Step:
                    return 3;
                default:
                    return 4;
            }
        }

        private static string ResolveLaneName(TactTimeRecord record)
        {
            if (record == null)
                return "기타";
            if (record.Category == TactTimeCategory.Run)
                return "Machine / Run";

            string unit = record.UnitName ?? "";
            string sequence = record.SequenceName ?? "";
            string combined = unit + " " + sequence;
            if (combined.IndexOf("FrontPicker", StringComparison.OrdinalIgnoreCase) >= 0)
                return "FrontPicker";
            if (combined.IndexOf("RearPicker", StringComparison.OrdinalIgnoreCase) >= 0)
                return "RearPicker";
            if (combined.IndexOf("Input", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Input";
            if (combined.IndexOf("Output", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Output";
            if (combined.IndexOf("Vision", StringComparison.OrdinalIgnoreCase) >= 0 || record.Category == TactTimeCategory.Vision)
                return "Vision";
            if (!string.IsNullOrWhiteSpace(unit))
                return unit;
            if (!string.IsNullOrWhiteSpace(sequence))
                return sequence;
            return record.Category.ToString();
        }

        private static int ResolveLaneOrder(string lane)
        {
            if (string.Equals(lane, "Machine / Run", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(lane, "Input", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(lane, "FrontPicker", StringComparison.OrdinalIgnoreCase)) return 2;
            if (string.Equals(lane, "RearPicker", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(lane, "Output", StringComparison.OrdinalIgnoreCase)) return 4;
            if (string.Equals(lane, "Vision", StringComparison.OrdinalIgnoreCase)) return 5;
            return 10;
        }

        private static string ResolveTrendSeriesName(TactTimeRecord record)
        {
            if (record == null)
                return "기타";
            if (!string.IsNullOrWhiteSpace(record.ProcessName))
                return record.ProcessName;
            if (!string.IsNullOrWhiteSpace(record.StepName))
                return record.StepName;
            if (!string.IsNullOrWhiteSpace(record.SequenceName))
                return record.SequenceName;
            return record.Category.ToString();
        }

        private static string ResolveRecordLabel(TactTimeRecord record)
        {
            if (record == null)
                return "-";
            if (!string.IsNullOrWhiteSpace(record.ProcessName))
                return record.ProcessName;
            if (!string.IsNullOrWhiteSpace(record.StepName))
                return record.StepName;
            if (!string.IsNullOrWhiteSpace(record.SequenceName))
                return record.SequenceName;
            return record.Category.ToString();
        }

        private static string BuildToolTip(TactTimeRecord record)
        {
            return
                "시간: " + record.StartedAt.ToString("HH:mm:ss.fff") + " ~ " + record.EndedAt.ToString("HH:mm:ss.fff") + Environment.NewLine +
                "소요: " + record.ElapsedMs.ToString("N0") + " ms (" + FormatDuration(record.ElapsedMs) + ")" + Environment.NewLine +
                "경로: " + Safe(record.UnitName) + " / " + Safe(record.SequenceName) + " / " +
                Safe(record.ProcessName) + " / " + Safe(record.StepName) + Environment.NewLine +
                "결과: " + record.Result +
                (string.IsNullOrWhiteSpace(record.AlarmCode) ? "" : " / " + record.AlarmCode) +
                (string.IsNullOrWhiteSpace(record.Detail) ? "" : Environment.NewLine + "상세: " + record.Detail);
        }

        private static double CalculateNearestRankPercentile(List<TactTimeRecord> records, double percentile)
        {
            if (records == null || records.Count == 0)
                return 0.0;

            long[] values = records.Select(x => Math.Max(0, x.ElapsedMs)).OrderBy(x => x).ToArray();
            int rank = (int)Math.Ceiling(Math.Max(0.0, Math.Min(1.0, percentile)) * values.Length);
            int index = Math.Max(0, Math.Min(values.Length - 1, rank - 1));
            return values[index];
        }

        private static string FormatDuration(double milliseconds)
        {
            if (milliseconds < 1000.0)
                return Math.Max(0.0, milliseconds).ToString("0") + " ms";
            if (milliseconds < 60000.0)
                return (milliseconds / 1000.0).ToString("0.###") + " s";
            return TimeSpan.FromMilliseconds(milliseconds).ToString(@"hh\:mm\:ss\.fff");
        }

        private static Color ResolveCategoryColor(TactTimeRecord record)
        {
            if (record == null)
                return Color.Gray;
            return ResolveCategoryColor(record.Category, record.Result);
        }

        private static Color ResolveCategoryColor(TactTimeCategory category, TactTimeResult result)
        {
            if (result == TactTimeResult.Failed)
                return Color.FromArgb(220, 76, 70);
            if (result == TactTimeResult.Stopped || result == TactTimeResult.Canceled)
                return Color.FromArgb(222, 166, 69);
            if (result == TactTimeResult.Skipped)
                return Color.FromArgb(175, 181, 187);

            switch (category)
            {
                case TactTimeCategory.Run: return Color.FromArgb(72, 116, 178);
                case TactTimeCategory.Unit: return Color.FromArgb(62, 160, 190);
                case TactTimeCategory.Process: return Color.FromArgb(54, 177, 126);
                case TactTimeCategory.Step: return Color.FromArgb(111, 122, 133);
                case TactTimeCategory.Vision: return Color.FromArgb(132, 92, 190);
                case TactTimeCategory.Motion: return Color.FromArgb(218, 145, 54);
                case TactTimeCategory.Wait: return Color.FromArgb(100, 149, 181);
                case TactTimeCategory.Resource: return Color.FromArgb(95, 170, 174);
                case TactTimeCategory.IO: return Color.FromArgb(144, 153, 162);
                default: return Color.FromArgb(106, 143, 166);
            }
        }

        private static Color ResolveResultColor(TactTimeResult result, Color normal)
        {
            if (result == TactTimeResult.Failed)
                return Color.FromArgb(220, 76, 70);
            if (result == TactTimeResult.Stopped || result == TactTimeResult.Canceled)
                return Color.FromArgb(222, 166, 69);
            return normal;
        }

        private static Color Darken(Color color)
        {
            return Color.FromArgb(color.A,
                Math.Max(0, color.R - 45),
                Math.Max(0, color.G - 45),
                Math.Max(0, color.B - 45));
        }

        private static Color ResolveTextColor(Color background)
        {
            double luminance = 0.299 * background.R + 0.587 * background.G + 0.114 * background.B;
            return luminance > 160 ? Color.FromArgb(36, 42, 48) : Color.White;
        }

        private static Color[] GetTrendPalette()
        {
            return new[]
            {
                Color.FromArgb(55, 125, 189),
                Color.FromArgb(60, 169, 124),
                Color.FromArgb(139, 95, 191),
                Color.FromArgb(218, 145, 54),
                Color.FromArgb(211, 88, 88),
                Color.FromArgb(67, 164, 181),
                Color.FromArgb(112, 121, 131),
                Color.FromArgb(185, 104, 151)
            };
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        private static string Shorten(string value, int maxLength)
        {
            value = value ?? "";
            return value.Length <= maxLength ? value : value.Substring(0, Math.Max(1, maxLength - 1)) + "…";
        }

        private sealed class TimelineAssignment
        {
            public TactTimeRecord Record { get; set; }
            public int RowIndex { get; set; }
        }

        private sealed class TimelineSlot
        {
            public int RowIndex { get; set; }
            public int RowCount { get; set; }
        }

        private sealed class ChartHitArea
        {
            public Rectangle Bounds { get; set; }
            public TactTimeRecord Record { get; set; }
        }

        private sealed class TrendBucket
        {
            public TactTimeRecord Minimum { get; set; }
            public TactTimeRecord Maximum { get; set; }
        }

        private sealed class LegendItem
        {
            public LegendItem(string text, Color color)
            {
                Text = text;
                Color = color;
            }

            public string Text { get; private set; }
            public Color Color { get; private set; }
        }
    }
}
