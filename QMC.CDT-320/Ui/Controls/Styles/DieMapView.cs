using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Bin;
using QMC.CDT320.DieMaps;

namespace QMC.CDT320.Ui.Controls
{
    /// <summary>
    /// 다이 맵 시각화 컨트롤 — 격자 셀 색상 표시 + hover 정보 + 클릭 이벤트.
    /// 310 의 wafer map view 와 동등한 기능 (코드는 독자 작성).
    /// </summary>
    public class DieMapView : Control
    {
        private DieMap _map;
        private DieMapEntry _hover;
        private DieMapEntry _selected;
        private float _zoom = 1.0F;
        private float _panX;
        private float _panY;
        private bool _dragging;
        private bool _dragMoved;
        private Point _dragStart;
        private PointF _dragStartPan;

        public event Action<DieMapEntry> CellClicked;

        /// <summary>현재 표시 중인 다이 맵.</summary>
        public DieMap Map
        {
            get => _map;
            set { SetMap(value, true); }
        }

        /// <summary>좌상단 정보 라벨에 표시할 추가 텍스트.</summary>
        public string Caption { get; set; } = "Die Map";

        public Func<DieMapEntry, Color> CellColorResolver { get; set; }

        public Func<DieMapEntry, string> CellTextResolver { get; set; }

        public Func<DieMapEntry, string> CellStatusResolver { get; set; }

        public Func<Tuple<string, Color>[]> LegendItemsResolver { get; set; }

        public Func<DieMapEntry, bool> EntryVisibilityPredicate { get; set; }

        public bool CompactUsedBounds { get; set; }

        public bool ShowWaferOutline { get; set; }

        public DieMapEntry SelectedEntry
        {
            get { return _selected; }
            set
            {
                _selected = value;
                Invalidate();
            }
        }

        public DieMapView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            BackColor = Color.FromArgb(30, 30, 30);
            DoubleBuffered = true;

            MouseMove += OnMouseMoveEvt;
            MouseLeave += (s, e) => { _hover = null; Invalidate(); };
            MouseClick += OnMouseClick;
            MouseDown += OnMouseDownEvt;
            MouseUp += OnMouseUpEvt;
            MouseWheel += OnMouseWheelEvt;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            using (var pen = new Pen(Color.DimGray, 1f))
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

            Color textColor = ResolveOverlayTextColor();
            using (var br = new SolidBrush(textColor))
            using (var f  = new Font("Consolas", 10F, FontStyle.Bold))
                g.DrawString(Caption, f, br, 8, 6);

            if (_map == null || _map.DieMapX <= 0 || _map.DieMapY <= 0)
            {
                using (var br = new SolidBrush(Color.Gray))
                using (var f  = new Font("맑은 고딕", 14F))
                    g.DrawString("(no map)", f, br,
                        (Width - 100) / 2.0f, (Height - 30) / 2.0f);
                return;
            }

            RectangleF mapRect;
            CellMetrics cell;
            VisibleBounds bounds;
            GetMapLayout(out mapRect, out cell, out bounds);

            if (ShowWaferOutline)
            {
                float radius = Math.Max(mapRect.Width, mapRect.Height) / 2.0F;
                float cx = mapRect.Left + mapRect.Width / 2.0F;
                float cy = mapRect.Top + mapRect.Height / 2.0F;
                using (var pen = new Pen(Color.FromArgb(70, 130, 220), 1f))
                    g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2.0F, radius * 2.0F);
            }

            // 셀 그리기
            foreach (var entry in _map.Entries)
            {
                if (entry == null)
                    continue;
                if (!IsEntryVisible(entry))
                    continue;

                float x = mapRect.Left + ToViewX(entry, bounds) * cell.Width;
                float y = mapRect.Top + ToViewY(entry, bounds) * cell.Height;
                if (x > Width || y > Height || x + cell.Width < 0 || y + cell.Height < 0)
                    continue;

                RectangleF dieRect = GetDieRect(x, y, cell);
                Color c = ResolveCellColor(entry);
                using (var br = new SolidBrush(c))
                    g.FillRectangle(br, dieRect);

                string cellText = CellTextResolver != null
                    ? CellTextResolver(entry)
                    : (entry.SequenceNo > 0 ? entry.SequenceNo.ToString() : "");
                float textCellSize = Math.Min(cell.DieWidth, cell.DieHeight);
                if (entry.IsTarget && !string.IsNullOrWhiteSpace(cellText) && textCellSize >= 16)
                {
                    using (var br = new SolidBrush(textColor))
                    using (var f = new Font("Consolas", Math.Max(6F, textCellSize * 0.32F), FontStyle.Regular))
                    {
                        SizeF size = g.MeasureString(cellText, f);
                        if (size.Width <= dieRect.Width - 2.0F && size.Height <= dieRect.Height - 2.0F)
                            g.DrawString(cellText, f, br, dieRect.Left + (dieRect.Width - size.Width) / 2.0F, dieRect.Top + (dieRect.Height - size.Height) / 2.0F);
                    }
                }
            }

            if (_selected != null && IsEntryVisible(_selected))
            {
                float x = mapRect.Left + ToViewX(_selected, bounds) * cell.Width;
                float y = mapRect.Top + ToViewY(_selected, bounds) * cell.Height;
                RectangleF dieRect = GetDieRect(x, y, cell);
                using (var pen = new Pen(Color.DeepSkyBlue, Math.Max(2.0F, Math.Min(4.0F, Math.Min(cell.DieWidth, cell.DieHeight) / 6.0F))))
                    g.DrawRectangle(pen, dieRect.X, dieRect.Y, Math.Max(1.0F, dieRect.Width - 1.0F), Math.Max(1.0F, dieRect.Height - 1.0F));
            }

            // hover 강조
            if (_hover != null && IsEntryVisible(_hover))
            {
                float x = mapRect.Left + ToViewX(_hover, bounds) * cell.Width;
                float y = mapRect.Top + ToViewY(_hover, bounds) * cell.Height;
                RectangleF dieRect = GetDieRect(x, y, cell);
                using (var pen = new Pen(Color.Yellow, 2f))
                    g.DrawRectangle(pen, dieRect.X, dieRect.Y, Math.Max(1.0F, dieRect.Width - 1.0F), Math.Max(1.0F, dieRect.Height - 1.0F));
            }

            // 좌상단 정보
            using (var br = new SolidBrush(textColor))
            using (var f  = new Font("Consolas", 9F))
            {
                string dieSizeInfo = FormatDieSizeInfo();
                string info = bounds.Compacted
                    ? $"{bounds.Width}×{bounds.Height} display={bounds.VisibleCount}  source={_map.DieMapX}×{_map.DieMapY}  pitch=({_map.PitchX:F2},{_map.PitchY:F2})mm  {dieSizeInfo}  zoom={_zoom * 100.0F:F0}%"
                    : $"{_map.DieMapX}×{_map.DieMapY}  pitch=({_map.PitchX:F2},{_map.PitchY:F2})mm  {dieSizeInfo}  total={_map.TotalCells}  zoom={_zoom * 100.0F:F0}%";
                g.DrawString(info, f, br, 8, 24);
                if (_hover != null && IsEntryVisible(_hover))
                {
                    string status = CellStatusResolver != null ? CellStatusResolver(_hover) : _hover.Result.ToString();
                    string h = $"seq={_hover.SequenceNo} [{ResolveEntryMapX(_hover)},{ResolveEntryMapY(_hover)}] xy=({_hover.PosX:F3},{_hover.PosY:F3}) state={status} bin={_hover.BinCode} uid={_hover.DieUid}";
                    g.DrawString(h, f, br, 8, Height - 18);
                }
            }

            DrawLegend(g, (int)mapRect.Width, (int)mapRect.Left, (int)(mapRect.Bottom + 6.0F));
        }

        public void ResetView()
        {
            _zoom = 1.0F;
            _panX = 0.0F;
            _panY = 0.0F;
            Invalidate();
        }

        public void SetMap(DieMap map, bool resetView)
        {
            _map = map;
            _hover = null;
            _selected = null;
            if (resetView)
                ResetView();
            else
                Invalidate();
        }

        private void DrawLegend(Graphics g, int totalW, int x0, int y)
        {
            Color textColor = ResolveOverlayTextColor();
            using (var f = new Font("Consolas", 8F))
            {
                int sx = x0;
                int sw = 14;
                int gap = 80;
                Tuple<string, Color>[] items = LegendItemsResolver != null
                    ? LegendItemsResolver()
                    : new[]
                    {
                        Tuple.Create("Good", BinCodeMap.ConvertToBinCodeColor(BinCodeMap.GoodBin)),
                        Tuple.Create("Pre-NG", BinCodeMap.ConvertToBinCodeColor(110)),
                        Tuple.Create("Critical", BinCodeMap.ConvertToBinCodeColor(200)),
                        Tuple.Create("Unknown", Color.FromArgb(80, 80, 100)),
                        Tuple.Create("Skip", Color.FromArgb(60, 60, 60)),
                    };
                foreach (var it in items)
                {
                    using (var br = new SolidBrush(it.Item2))
                        g.FillRectangle(br, sx, y, sw, 12);
                    using (var br = new SolidBrush(textColor))
                        g.DrawString(it.Item1, f, br, sx + sw + 3, y - 1);
                    SizeF labelSize = g.MeasureString(it.Item1, f);
                    sx += Math.Max(gap, sw + 3 + (int)Math.Ceiling(labelSize.Width) + 16);
                }
            }
        }

        private DieMapEntry HitTest(int mouseX, int mouseY)
        {
            if (_map == null) return null;
            RectangleF mapRect;
            CellMetrics cell;
            VisibleBounds bounds;
            GetMapLayout(out mapRect, out cell, out bounds);

            int gx = (int)Math.Floor((mouseX - mapRect.Left) / cell.Width);
            int gy = (int)Math.Floor((mouseY - mapRect.Top) / cell.Height);
            if (gx < 0 || gx >= bounds.Width || gy < 0 || gy >= bounds.Height) return null;
            DieMapEntry hit = FindCellByMapIndex(bounds.MinX + gx, bounds.MinY + gy);
            return IsEntryVisible(hit) ? hit : null;
        }

        private void OnMouseMoveEvt(object s, MouseEventArgs e)
        {
            if (_dragging)
            {
                int dx = e.X - _dragStart.X;
                int dy = e.Y - _dragStart.Y;
                if (Math.Abs(dx) > 2 || Math.Abs(dy) > 2)
                    _dragMoved = true;

                _panX = _dragStartPan.X + dx;
                _panY = _dragStartPan.Y + dy;
                Invalidate();
                return;
            }

            var hit = HitTest(e.X, e.Y);
            if (hit != _hover) { _hover = hit; Invalidate(); }
        }

        private void OnMouseClick(object s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            if (_dragMoved)
                return;

            var hit = HitTest(e.X, e.Y);
            if (hit != null)
            {
                _selected = hit;
                Invalidate();
                try { CellClicked?.Invoke(hit); } catch { }
            }
        }

        private void OnMouseDownEvt(object s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Middle && e.Button != MouseButtons.Right)
                return;

            Focus();
            _dragging = true;
            _dragMoved = false;
            _dragStart = e.Location;
            _dragStartPan = new PointF(_panX, _panY);
            Cursor = Cursors.SizeAll;
        }

        private void OnMouseUpEvt(object s, MouseEventArgs e)
        {
            _dragging = false;
            Cursor = Cursors.Default;
        }

        private void OnMouseWheelEvt(object s, MouseEventArgs e)
        {
            if (_map == null)
                return;

            RectangleF beforeRect;
            CellMetrics beforeCell;
            GetMapLayout(out beforeRect, out beforeCell);
            float mapX = (e.X - beforeRect.Left) / beforeCell.Width;
            float mapY = (e.Y - beforeRect.Top) / beforeCell.Height;

            float factor = e.Delta > 0 ? 1.2F : 0.8333333F;
            _zoom = Math.Max(0.2F, Math.Min(30.0F, _zoom * factor));

            RectangleF afterRect;
            CellMetrics afterCell;
            GetMapLayout(out afterRect, out afterCell);
            _panX += e.X - (afterRect.Left + mapX * afterCell.Width);
            _panY += e.Y - (afterRect.Top + mapY * afterCell.Height);
            Invalidate();
        }

        private void GetMapLayout(out RectangleF mapRect, out CellMetrics cell)
        {
            VisibleBounds bounds;
            GetMapLayout(out mapRect, out cell, out bounds);
        }

        private void GetMapLayout(out RectangleF mapRect, out CellMetrics cell, out VisibleBounds bounds)
        {
            int margin = 30;
            int titleH = 48;
            int legendH = 28;
            bounds = CalculateVisibleBounds();
            int availableW = Math.Max(1, Width - margin * 2);
            int availableH = Math.Max(1, Height - titleH - legendH - margin);
            double physicalCellX = ResolvePhysicalCellX();
            double physicalCellY = ResolvePhysicalCellY();
            float baseScale = Math.Max(0.01F, Math.Min(
                availableW / Math.Max(1.0F, (float)(bounds.Width * physicalCellX)),
                availableH / Math.Max(1.0F, (float)(bounds.Height * physicalCellY))));
            float scale = Math.Max(0.01F, baseScale * _zoom);
            cell = BuildCellMetrics(physicalCellX, physicalCellY, scale);

            float totalW = cell.Width * bounds.Width;
            float totalH = cell.Height * bounds.Height;
            float x0 = (Width - totalW) / 2.0F + _panX;
            float y0 = titleH + (availableH - totalH) / 2.0F + _panY;
            mapRect = new RectangleF(x0, y0, totalW, totalH);
        }

        private CellMetrics BuildCellMetrics(double physicalCellX, double physicalCellY, float scale)
        {
            float cellWidth = Math.Max(1.0F, (float)(physicalCellX * scale));
            float cellHeight = Math.Max(1.0F, (float)(physicalCellY * scale));
            double pitchX = _map != null && _map.PitchX > 0.0 ? _map.PitchX : physicalCellX;
            double pitchY = _map != null && _map.PitchY > 0.0 ? _map.PitchY : physicalCellY;
            double dieSizeX = _map != null && _map.DieSizeX > 0.0 ? _map.DieSizeX : physicalCellX;
            double dieSizeY = _map != null && _map.DieSizeY > 0.0 ? _map.DieSizeY : physicalCellY;
            float dieWidth = cellWidth * (float)Clamp(dieSizeX / Math.Max(0.001, pitchX), 0.05, 1.0);
            float dieHeight = cellHeight * (float)Clamp(dieSizeY / Math.Max(0.001, pitchY), 0.05, 1.0);
            float gapX = cellWidth >= 3.0F ? Math.Min(1.0F, cellWidth * 0.08F) : 0.0F;
            float gapY = cellHeight >= 3.0F ? Math.Min(1.0F, cellHeight * 0.08F) : 0.0F;

            return new CellMetrics
            {
                Width = cellWidth,
                Height = cellHeight,
                DieWidth = Math.Max(1.0F, dieWidth - gapX),
                DieHeight = Math.Max(1.0F, dieHeight - gapY)
            };
        }

        private RectangleF GetDieRect(float cellX, float cellY, CellMetrics cell)
        {
            // 현재 기준: Map 격자는 Pitch 간격, 표시 다이는 DieSizeX/Y 비율로 셀 중앙에 그린다.
            float width = Math.Min(cell.Width, cell.DieWidth);
            float height = Math.Min(cell.Height, cell.DieHeight);
            return new RectangleF(
                cellX + (cell.Width - width) / 2.0F,
                cellY + (cell.Height - height) / 2.0F,
                width,
                height);
        }

        private double ResolvePhysicalCellX()
        {
            if (_map != null && _map.PitchX > 0.0)
                return _map.PitchX;
            if (_map != null && _map.DieSizeX > 0.0)
                return _map.DieSizeX;
            return 1.0;
        }

        private double ResolvePhysicalCellY()
        {
            if (_map != null && _map.PitchY > 0.0)
                return _map.PitchY;
            if (_map != null && _map.DieSizeY > 0.0)
                return _map.DieSizeY;
            return 1.0;
        }

        private string FormatDieSizeInfo()
        {
            double dieSizeX = _map != null && _map.DieSizeX > 0.0 ? _map.DieSizeX : ResolvePhysicalCellX();
            double dieSizeY = _map != null && _map.DieSizeY > 0.0 ? _map.DieSizeY : ResolvePhysicalCellY();
            return $"die=({dieSizeX:F2},{dieSizeY:F2})mm";
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private VisibleBounds CalculateVisibleBounds()
        {
            var bounds = new VisibleBounds
            {
                MinX = 0,
                MinY = 0,
                Width = Math.Max(1, _map != null ? _map.DieMapX : 1),
                Height = Math.Max(1, _map != null ? _map.DieMapY : 1),
                VisibleCount = 0,
                Compacted = false
            };

            if (_map == null || _map.Entries == null || !CompactUsedBounds)
                return bounds;

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;
            int count = 0;
            foreach (DieMapEntry entry in _map.Entries)
            {
                if (!IsEntryVisible(entry))
                    continue;

                int mapX = ResolveEntryMapX(entry);
                int mapY = ResolveEntryMapY(entry);
                minX = Math.Min(minX, mapX);
                minY = Math.Min(minY, mapY);
                maxX = Math.Max(maxX, mapX);
                maxY = Math.Max(maxY, mapY);
                count++;
            }

            if (count <= 0 || minX == int.MaxValue || minY == int.MaxValue)
                return bounds;

            bounds.MinX = minX;
            bounds.MinY = minY;
            bounds.Width = Math.Max(1, maxX - minX + 1);
            bounds.Height = Math.Max(1, maxY - minY + 1);
            bounds.VisibleCount = count;
            bounds.Compacted = true;
            return bounds;
        }

        private bool IsEntryVisible(DieMapEntry entry)
        {
            if (entry == null)
                return false;
            return EntryVisibilityPredicate == null || EntryVisibilityPredicate(entry);
        }

        private static int ToViewX(DieMapEntry entry, VisibleBounds bounds)
        {
            return entry != null ? ResolveEntryMapX(entry) - bounds.MinX : 0;
        }

        private static int ToViewY(DieMapEntry entry, VisibleBounds bounds)
        {
            return entry != null ? ResolveEntryMapY(entry) - bounds.MinY : 0;
        }

        private DieMapEntry FindCellByMapIndex(int mapX, int mapY)
        {
            if (_map == null || _map.Entries == null)
                return null;

            foreach (DieMapEntry entry in _map.Entries)
            {
                if (entry != null &&
                    ResolveEntryMapX(entry) == mapX &&
                    ResolveEntryMapY(entry) == mapY)
                    return entry;
            }

            return null;
        }

        private static int ResolveEntryMapX(DieMapEntry entry)
        {
            return DieMapGenerator.ResolveMapIndexX(entry);
        }

        private static int ResolveEntryMapY(DieMapEntry entry)
        {
            return DieMapGenerator.ResolveMapIndexY(entry);
        }

        private struct VisibleBounds
        {
            public int MinX;
            public int MinY;
            public int Width;
            public int Height;
            public int VisibleCount;
            public bool Compacted;
        }

        private struct CellMetrics
        {
            public float Width;
            public float Height;
            public float DieWidth;
            public float DieHeight;
        }

        private Color ResolveCellColor(DieMapEntry entry)
        {
            if (CellColorResolver != null)
                return CellColorResolver(entry);

            return !entry.IsTarget
                ? Color.FromArgb(60, 60, 60)
                : (entry.BinCode > 0
                    ? BinCodeMap.ConvertToBinCodeColor(entry.BinCode)
                    : Color.FromArgb(80, 80, 100));
        }

        private Color ResolveOverlayTextColor()
        {
            int brightness = BackColor.R + BackColor.G + BackColor.B;
            return brightness > 420 ? Color.FromArgb(0x33, 0x33, 0x33) : Color.WhiteSmoke;
        }
    }
}
