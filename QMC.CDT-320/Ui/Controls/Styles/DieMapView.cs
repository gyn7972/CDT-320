using System;
using System.Collections.Generic;
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
        private readonly List<DieMapEntry> _selectedEntries = new List<DieMapEntry>();
        private bool _rectangleSelecting;
        private Point _selectionStart;
        private Point _selectionEnd;

        public event Action<DieMapEntry> CellClicked;
        public event Action<DieMapEntry> CellDoubleClicked;
        public event Action<IReadOnlyList<DieMapEntry>> SelectionRectangleCompleted;

        /// <summary>현재 표시 중인 다이 맵.</summary>
        public DieMap Map
        {
            get => _map;
            set { SetMap(value, true); }
        }

        /// <summary>좌상단 정보 라벨에 표시할 추가 텍스트.</summary>
        public string Caption { get; set; } = "Die Map";

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<DieMapEntry, Color> CellColorResolver { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<DieMapEntry, string> CellTextResolver { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<DieMapEntry, string> CellStatusResolver { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<Tuple<string, Color>[]> LegendItemsResolver { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<DieMapEntry, bool> EntryVisibilityPredicate { get; set; }

        public bool CompactUsedBounds { get; set; }

        public bool ShowWaferOutline { get; set; }

        /// <summary>웨이퍼 중심 (0,0)과 장비 좌표 방향(+X 우측, +Y 위쪽)을 표시한다.</summary>
        public bool ShowEquipmentAxes { get; set; }

        public bool EnableRectangleSelection { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IReadOnlyList<DieMapEntry> SelectedEntries
        {
            get { return _selectedEntries.AsReadOnly(); }
        }

        // ─── 스타일 훅 (기본값 = 기존 룩). 파생 뷰에서 override 하여 부드러운 팔레트 적용. ───
        /// <summary>외곽 테두리 색.</summary>
        protected virtual Color MapBorderColor => Color.DimGray;
        /// <summary>외곽 테두리 두께(px).</summary>
        protected virtual float MapBorderWidth => 1f;
        /// <summary>외곽 테두리를 가장자리에서 안쪽으로 들여쓰는 정도(px). 0 = 컨트롤 가장자리.</summary>
        protected virtual int MapBorderInset => 0;
        /// <summary>웨이퍼 외곽 원 색.</summary>
        protected virtual Color WaferOutlineColor => Color.FromArgb(70, 130, 220);
        /// <summary>캡션/정보/범례 등 오버레이 텍스트 폰트 패밀리.</summary>
        protected virtual string OverlayFontFamily => "Consolas";
        /// <summary>격자 크기·pitch·zoom 등 기술 정보 라인 표시 여부.</summary>
        protected virtual bool ShowTechnicalInfoLine => true;

        public DieMapEntry SelectedEntry
        {
            get { return _selected; }
            set
            {
                _selected = value;
                _selectedEntries.Clear();
                if (value != null)
                    _selectedEntries.Add(value);
                Invalidate();
            }
        }

        public DieMapView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint |
                     ControlStyles.StandardClick |
                     ControlStyles.StandardDoubleClick, true);
            BackColor = Color.FromArgb(30, 30, 30);
            DoubleBuffered = true;

            MouseMove += OnMouseMoveEvt;
            MouseLeave += (s, e) => { _hover = null; Invalidate(); };
            MouseClick += OnMouseClick;
            MouseDoubleClick += OnMouseDoubleClick;
            MouseDown += OnMouseDownEvt;
            MouseUp += OnMouseUpEvt;
            MouseWheel += OnMouseWheelEvt;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(BackColor);
            using (var pen = new Pen(MapBorderColor, MapBorderWidth))
            {
                int ins = MapBorderInset;
                g.DrawRectangle(pen, ins, ins,
                    Math.Max(1, Width - 1 - ins * 2), Math.Max(1, Height - 1 - ins * 2));
            }

            Color textColor = ResolveOverlayTextColor();
            using (var br = new SolidBrush(textColor))
            using (var f  = new Font(OverlayFontFamily, 10F, FontStyle.Bold))
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
            RectangleF contentRect;
            CellMetrics cell;
            VisibleBounds bounds;
            GetMapLayout(out mapRect, out contentRect, out cell, out bounds);

            // 셀 그리기
            foreach (var entry in _map.Entries)
            {
                if (entry == null)
                    continue;
                if (!IsEntryVisible(entry))
                    continue;

                float x = mapRect.Left + ToViewX(entry, bounds) * cell.Width;
                float y = mapRect.Top + ToViewY(entry, bounds) * cell.Height;
                RectangleF dieRect = GetDieRect(x, y, cell);
                if (dieRect.Left > Width || dieRect.Top > Height || dieRect.Right < 0 || dieRect.Bottom < 0)
                    continue;
                Color c = ResolveCellColor(entry);
                using (var br = new SolidBrush(c))
                    g.FillRectangle(br, dieRect);
                if (dieRect.Width >= 2.0F && dieRect.Height >= 2.0F)
                {
                    using (var gridPen = new Pen(Color.FromArgb(75, ResolveOverlayTextColor()), 0.7F))
                        g.DrawRectangle(gridPen, dieRect.X, dieRect.Y,
                            Math.Max(0.1F, dieRect.Width - 0.5F),
                            Math.Max(0.1F, dieRect.Height - 0.5F));
                }

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

            // 실제 외경은 셀 위에 그려야 Target 셀이 외곽선을 덮지 않는다.
            if (ShowWaferOutline)
                DrawPhysicalWaferOutline(g, mapRect, cell, bounds);

            if (ShowEquipmentAxes)
                DrawEquipmentAxes(g, mapRect, contentRect, cell, bounds);

            DrawSelectedEntries(g, mapRect, cell, bounds);

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

            if (_rectangleSelecting && EnableRectangleSelection)
                DrawSelectionRectangle(g);

            // 좌상단 정보
            using (var br = new SolidBrush(textColor))
            using (var f  = new Font(OverlayFontFamily, 9F))
            {
                if (ShowTechnicalInfoLine)
                {
                    string dieSizeInfo = FormatDieSizeInfo();
                    string waferInfo = _map.OuterDiameterMm > 0.0
                        ? $"wafer={_map.OuterDiameterMm:F2}mm"
                        : "wafer=(not set)";
                    string info = bounds.Compacted
                        ? $"{bounds.Width}×{bounds.Height} display={bounds.VisibleCount}  source={_map.DieMapX}×{_map.DieMapY}  step=({_map.PitchX:F3},{_map.PitchY:F3})mm  {dieSizeInfo}  {waferInfo}  zoom={_zoom * 100.0F:F0}%"
                        : $"{_map.DieMapX}×{_map.DieMapY}  step=({_map.PitchX:F3},{_map.PitchY:F3})mm  {dieSizeInfo}  {waferInfo}  total={_map.TotalCells}  zoom={_zoom * 100.0F:F0}%";
                    if (ShowEquipmentAxes)
                        info += "  center=(0,0) X:L-/R+ Y:D-/U+";
                    g.DrawString(info, f, br, 8, 24);
                }
                if (_hover != null && IsEntryVisible(_hover))
                {
                    string status = CellStatusResolver != null ? CellStatusResolver(_hover) : _hover.Result.ToString();
                    int rawX = _hover.OriginalMapX >= 0 ? _hover.OriginalMapX : _hover.DieMapX;
                    int rawY = _hover.OriginalMapY >= 0 ? _hover.OriginalMapY : _hover.DieMapY;
                    string h = $"seq={_hover.SequenceNo} local=[{_hover.DieMapX},{_hover.DieMapY}] raw=[{rawX},{rawY}] grid=({_hover.EquipmentGridX:F1},{_hover.EquipmentGridY:F1}) axis=({_hover.PosX:F3},{_hover.PosY:F3}) state={status} bin={_hover.BinCode}";
                    g.DrawString(h, f, br, 8, Height - 18);
                }
            }

            DrawLegend(g, (int)contentRect.Width, (int)contentRect.Left, (int)(contentRect.Bottom + 6.0F));
        }

        private void DrawPhysicalWaferOutline(Graphics g, RectangleF mapRect, CellMetrics cell, VisibleBounds bounds)
        {
            if (_map == null)
                return;

            double pitchX = ResolvePhysicalCellX();
            double pitchY = ResolvePhysicalCellY();
            float scaleX = cell.Width / (float)Math.Max(0.000001, pitchX);
            float scaleY = cell.Height / (float)Math.Max(0.000001, pitchY);
            float scale = Math.Min(scaleX, scaleY);
            double diameterMm = ResolveWaferDiameterMm();
            if (diameterMm <= 0.0)
                return;

            float diameter = (float)(diameterMm * scale);
            float centerX = mapRect.Left + (float)(Math.Max(0, _map.DieMapX - 1) / 2.0 - bounds.MinX + 0.5) * cell.Width;
            float centerY = mapRect.Top + (float)(Math.Max(0, _map.DieMapY - 1) / 2.0 - bounds.MinY + 0.5) * cell.Height;
            using (var pen = new Pen(WaferOutlineColor, 1.4F))
                g.DrawEllipse(pen, centerX - diameter / 2.0F, centerY - diameter / 2.0F, diameter, diameter);
        }

        private void DrawEquipmentAxes(Graphics g, RectangleF mapRect, RectangleF contentRect, CellMetrics cell, VisibleBounds bounds)
        {
            if (_map == null)
                return;

            double centerX = Math.Max(0, _map.DieMapX - 1) / 2.0;
            double centerY = Math.Max(0, _map.DieMapY - 1) / 2.0;
            float zeroX = mapRect.Left + (float)(centerX - bounds.MinX + 0.5) * cell.Width;
            float zeroY = mapRect.Top + (float)(centerY - bounds.MinY + 0.5) * cell.Height;
            Color axisColor = Color.FromArgb(190, 255, 215, 0);
            using (var pen = new Pen(axisColor, 1.2F))
            using (var brush = new SolidBrush(axisColor))
            using (var font = new Font(OverlayFontFamily, 8.5F, FontStyle.Bold))
            {
                g.DrawLine(pen, contentRect.Left, zeroY, contentRect.Right, zeroY);
                g.DrawLine(pen, zeroX, contentRect.Bottom, zeroX, contentRect.Top);
                g.DrawString("-X", font, brush, contentRect.Left - 2F, zeroY + 2F);
                g.DrawString("+X", font, brush, contentRect.Right - 20F, zeroY + 2F);
                g.DrawString("+Y", font, brush, zeroX + 3F, contentRect.Top - 16F);
                g.DrawString("-Y", font, brush, zeroX + 3F, contentRect.Bottom + 1F);
                g.DrawString("0,0", font, brush, zeroX + 3F, zeroY + 2F);
            }
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
            _selectedEntries.Clear();
            if (resetView)
                ResetView();
            else
                Invalidate();
        }

        public void SetSelectedEntries(IEnumerable<DieMapEntry> entries)
        {
            _selectedEntries.Clear();
            _selected = null;

            if (entries != null)
            {
                foreach (DieMapEntry entry in entries)
                {
                    if (entry == null || !IsEntryVisible(entry))
                        continue;
                    if (ContainsEntry(_selectedEntries, entry))
                        continue;

                    _selectedEntries.Add(entry);
                    if (_selected == null)
                        _selected = entry;
                }
            }

            Invalidate();
        }

        private void DrawLegend(Graphics g, int totalW, int x0, int y)
        {
            Color textColor = ResolveOverlayTextColor();
            using (var f = new Font(OverlayFontFamily, 8.5F))
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

            DieMapEntry hit = null;
            double nearestDistance = double.MaxValue;
            foreach (DieMapEntry entry in _map.Entries)
            {
                if (!IsEntryVisible(entry))
                    continue;

                float x = mapRect.Left + ToViewX(entry, bounds) * cell.Width;
                float y = mapRect.Top + ToViewY(entry, bounds) * cell.Height;
                RectangleF dieRect = GetDieRect(x, y, cell);
                if (!dieRect.Contains(mouseX, mouseY))
                    continue;

                double dx = mouseX - (dieRect.Left + dieRect.Width / 2.0F);
                double dy = mouseY - (dieRect.Top + dieRect.Height / 2.0F);
                double distance = dx * dx + dy * dy;
                if (distance < nearestDistance)
                {
                    hit = entry;
                    nearestDistance = distance;
                }
            }

            return hit;
        }

        private void OnMouseMoveEvt(object s, MouseEventArgs e)
        {
            if (_rectangleSelecting)
            {
                int dx = e.X - _selectionStart.X;
                int dy = e.Y - _selectionStart.Y;
                if (Math.Abs(dx) > 2 || Math.Abs(dy) > 2)
                    _dragMoved = true;

                _selectionEnd = e.Location;
                Invalidate();
                return;
            }

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
                if ((ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    ToggleSelectedEntry(hit);
                    try { SelectionRectangleCompleted?.Invoke(_selectedEntries.AsReadOnly()); } catch { }
                    return;
                }

                _selected = hit;
                _selectedEntries.Clear();
                _selectedEntries.Add(hit);
                Invalidate();
                try { CellClicked?.Invoke(hit); } catch { }
            }
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _dragMoved)
                return;

            DieMapEntry hit = HitTest(e.X, e.Y);
            if (hit == null)
                return;

            _selected = hit;
            _selectedEntries.Clear();
            _selectedEntries.Add(hit);
            Invalidate();
            try { CellDoubleClicked?.Invoke(hit); } catch { }
        }

        private void ToggleSelectedEntry(DieMapEntry entry)
        {
            if (entry == null || !IsEntryVisible(entry))
                return;

            for (int i = _selectedEntries.Count - 1; i >= 0; i--)
            {
                if (IsSameEntry(_selectedEntries[i], entry))
                {
                    _selectedEntries.RemoveAt(i);
                    _selected = _selectedEntries.Count > 0 ? _selectedEntries[0] : null;
                    Invalidate();
                    return;
                }
            }

            _selectedEntries.Add(entry);
            if (_selected == null)
                _selected = entry;
            Invalidate();
        }

        private void OnMouseDownEvt(object s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Middle && e.Button != MouseButtons.Right)
                return;

            Focus();
            if (EnableRectangleSelection && e.Button == MouseButtons.Left)
            {
                _rectangleSelecting = true;
                _dragMoved = false;
                _selectionStart = e.Location;
                _selectionEnd = e.Location;
                Cursor = Cursors.Cross;
                return;
            }

            _dragging = true;
            _dragMoved = false;
            _dragStart = e.Location;
            _dragStartPan = new PointF(_panX, _panY);
            Cursor = Cursors.SizeAll;
        }

        private void OnMouseUpEvt(object s, MouseEventArgs e)
        {
            if (_rectangleSelecting)
            {
                _rectangleSelecting = false;
                _selectionEnd = e.Location;
                Cursor = Cursors.Default;

                if (_dragMoved)
                {
                    List<DieMapEntry> entries = HitTestRectangle(NormalizeRectangle(_selectionStart, _selectionEnd));
                    SetSelectedEntries(entries);
                    if (entries.Count > 0)
                    {
                        try { SelectionRectangleCompleted?.Invoke(_selectedEntries.AsReadOnly()); } catch { }
                    }
                    else
                    {
                        Invalidate();
                    }
                }

                return;
            }

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
            RectangleF contentRect;
            VisibleBounds bounds;
            GetMapLayout(out mapRect, out contentRect, out cell, out bounds);
        }

        private void GetMapLayout(out RectangleF mapRect, out CellMetrics cell, out VisibleBounds bounds)
        {
            RectangleF contentRect;
            GetMapLayout(out mapRect, out contentRect, out cell, out bounds);
        }

        private void GetMapLayout(
            out RectangleF mapRect,
            out RectangleF contentRect,
            out CellMetrics cell,
            out VisibleBounds bounds)
        {
            int margin = 30;
            int titleH = 48;
            int legendH = 28;
            bounds = CalculateVisibleBounds();
            int availableW = Math.Max(1, Width - margin * 2);
            int availableH = Math.Max(1, Height - titleH - legendH - margin);
            double physicalCellX = ResolvePhysicalCellX();
            double physicalCellY = ResolvePhysicalCellY();
            PhysicalBounds physicalBounds = CalculatePhysicalContentBounds(bounds, physicalCellX, physicalCellY);
            float baseScale = Math.Min(
                availableW / Math.Max(0.000001F, (float)physicalBounds.Width),
                availableH / Math.Max(0.000001F, (float)physicalBounds.Height));
            if (float.IsNaN(baseScale) || float.IsInfinity(baseScale) || baseScale <= 0.0F)
                baseScale = 1.0F;
            float scale = Math.Max(0.000001F, baseScale * _zoom);
            cell = BuildCellMetrics(physicalCellX, physicalCellY, scale);

            float contentW = (float)(physicalBounds.Width * scale);
            float contentH = (float)(physicalBounds.Height * scale);
            float contentX = (Width - contentW) / 2.0F + _panX;
            float contentY = titleH + (availableH - contentH) / 2.0F + _panY;
            contentRect = new RectangleF(contentX, contentY, contentW, contentH);

            float mapX = contentX - (float)(physicalBounds.MinX * scale);
            float mapY = contentY - (float)(physicalBounds.MinY * scale);
            mapRect = new RectangleF(
                mapX,
                mapY,
                (float)(bounds.Width * physicalCellX * scale),
                (float)(bounds.Height * physicalCellY * scale));
        }

        private CellMetrics BuildCellMetrics(double physicalCellX, double physicalCellY, float scale)
        {
            float cellWidth = Math.Max(0.000001F, (float)(physicalCellX * scale));
            float cellHeight = Math.Max(0.000001F, (float)(physicalCellY * scale));
            double dieSizeX = _map != null && _map.DieSizeX > 0.0 ? _map.DieSizeX : physicalCellX;
            double dieSizeY = _map != null && _map.DieSizeY > 0.0 ? _map.DieSizeY : physicalCellY;

            return new CellMetrics
            {
                Width = cellWidth,
                Height = cellHeight,
                DieWidth = Math.Max(0.5F, (float)(dieSizeX * scale)),
                DieHeight = Math.Max(0.5F, (float)(dieSizeY * scale))
            };
        }

        private RectangleF GetDieRect(float cellX, float cellY, CellMetrics cell)
        {
            // 현재 기준: Map 격자는 Pitch 간격, 표시 다이는 DieSizeX/Y 비율로 셀 중앙에 그린다.
            float width = cell.DieWidth;
            float height = cell.DieHeight;
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

        private double ResolveWaferDiameterMm()
        {
            if (_map == null || double.IsNaN(_map.OuterDiameterMm) ||
                double.IsInfinity(_map.OuterDiameterMm) || _map.OuterDiameterMm <= 0.0)
                return 0.0;
            return _map.OuterDiameterMm;
        }

        private PhysicalBounds CalculatePhysicalContentBounds(
            VisibleBounds bounds,
            double pitchX,
            double pitchY)
        {
            double dieSizeX = _map != null && _map.DieSizeX > 0.0 ? _map.DieSizeX : pitchX;
            double dieSizeY = _map != null && _map.DieSizeY > 0.0 ? _map.DieSizeY : pitchY;
            var result = new PhysicalBounds
            {
                MinX = pitchX / 2.0 - dieSizeX / 2.0,
                MinY = pitchY / 2.0 - dieSizeY / 2.0,
                MaxX = (bounds.Width - 0.5) * pitchX + dieSizeX / 2.0,
                MaxY = (bounds.Height - 0.5) * pitchY + dieSizeY / 2.0
            };

            double diameter = ShowWaferOutline ? ResolveWaferDiameterMm() : 0.0;
            if (diameter > 0.0 && _map != null)
            {
                double waferCenterX =
                    (Math.Max(0, _map.DieMapX - 1) / 2.0 - bounds.MinX + 0.5) * pitchX;
                double waferCenterY =
                    (Math.Max(0, _map.DieMapY - 1) / 2.0 - bounds.MinY + 0.5) * pitchY;
                double radius = diameter / 2.0;
                result.MinX = Math.Min(result.MinX, waferCenterX - radius);
                result.MinY = Math.Min(result.MinY, waferCenterY - radius);
                result.MaxX = Math.Max(result.MaxX, waferCenterX + radius);
                result.MaxY = Math.Max(result.MaxY, waferCenterY + radius);
            }

            if (result.MaxX <= result.MinX)
                result.MaxX = result.MinX + Math.Max(0.000001, pitchX);
            if (result.MaxY <= result.MinY)
                result.MaxY = result.MinY + Math.Max(0.000001, pitchY);
            return result;
        }

        private string FormatDieSizeInfo()
        {
            double dieSizeX = _map != null && _map.DieSizeX > 0.0 ? _map.DieSizeX : ResolvePhysicalCellX();
            double dieSizeY = _map != null && _map.DieSizeY > 0.0 ? _map.DieSizeY : ResolvePhysicalCellY();
            return $"die=({dieSizeX:F2},{dieSizeY:F2})mm";
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
            int geometryCount = 0;
            int visibleCount = 0;
            foreach (DieMapEntry entry in _map.Entries)
            {
                if (entry == null)
                    continue;

                int mapX = ResolveEntryMapX(entry);
                int mapY = ResolveEntryMapY(entry);
                if (mapX < 0 || mapY < 0)
                    continue;
                minX = Math.Min(minX, mapX);
                minY = Math.Min(minY, mapY);
                maxX = Math.Max(maxX, mapX);
                maxY = Math.Max(maxY, mapY);
                geometryCount++;
                if (IsEntryVisible(entry))
                    visibleCount++;
            }

            if (geometryCount <= 0 || minX == int.MaxValue || minY == int.MaxValue)
                return bounds;

            bounds.MinX = minX;
            bounds.MinY = minY;
            bounds.Width = Math.Max(1, maxX - minX + 1);
            bounds.Height = Math.Max(1, maxY - minY + 1);
            bounds.VisibleCount = visibleCount;
            bounds.Compacted = true;
            return bounds;
        }

        private bool IsEntryVisible(DieMapEntry entry)
        {
            if (entry == null)
                return false;
            return EntryVisibilityPredicate == null || EntryVisibilityPredicate(entry);
        }

        private void DrawSelectedEntries(Graphics g, RectangleF mapRect, CellMetrics cell, VisibleBounds bounds)
        {
            if (_selectedEntries == null || _selectedEntries.Count <= 0)
                return;

            using (var pen = new Pen(Color.LimeGreen, Math.Max(1.5F, Math.Min(3.0F, Math.Min(cell.DieWidth, cell.DieHeight) / 8.0F))))
            {
                for (int i = 0; i < _selectedEntries.Count; i++)
                {
                    DieMapEntry entry = _selectedEntries[i];
                    if (entry == null || !IsEntryVisible(entry))
                        continue;

                    float x = mapRect.Left + ToViewX(entry, bounds) * cell.Width;
                    float y = mapRect.Top + ToViewY(entry, bounds) * cell.Height;
                    RectangleF dieRect = GetDieRect(x, y, cell);
                    g.DrawRectangle(pen, dieRect.X, dieRect.Y, Math.Max(1.0F, dieRect.Width - 1.0F), Math.Max(1.0F, dieRect.Height - 1.0F));
                }
            }
        }

        private void DrawSelectionRectangle(Graphics g)
        {
            Rectangle rect = NormalizeRectangle(_selectionStart, _selectionEnd);
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            using (var fill = new SolidBrush(Color.FromArgb(45, Color.DeepSkyBlue)))
                g.FillRectangle(fill, rect);
            using (var pen = new Pen(Color.DeepSkyBlue, 1.4F))
                g.DrawRectangle(pen, rect);
        }

        private List<DieMapEntry> HitTestRectangle(Rectangle screenRect)
        {
            var result = new List<DieMapEntry>();
            if (_map == null || _map.Entries == null || screenRect.Width <= 0 || screenRect.Height <= 0)
                return result;

            RectangleF mapRect;
            CellMetrics cell;
            VisibleBounds bounds;
            GetMapLayout(out mapRect, out cell, out bounds);

            foreach (DieMapEntry entry in _map.Entries)
            {
                if (entry == null || !IsEntryVisible(entry))
                    continue;

                float x = mapRect.Left + ToViewX(entry, bounds) * cell.Width;
                float y = mapRect.Top + ToViewY(entry, bounds) * cell.Height;
                RectangleF dieRect = GetDieRect(x, y, cell);
                if (screenRect.IntersectsWith(Rectangle.Round(dieRect)))
                    result.Add(entry);
            }

            return result;
        }

        private static Rectangle NormalizeRectangle(Point a, Point b)
        {
            int left = Math.Min(a.X, b.X);
            int top = Math.Min(a.Y, b.Y);
            int right = Math.Max(a.X, b.X);
            int bottom = Math.Max(a.Y, b.Y);
            return new Rectangle(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        }

        private static bool ContainsEntry(List<DieMapEntry> entries, DieMapEntry target)
        {
            if (entries == null || target == null)
                return false;

            int targetX = ResolveEntryMapX(target);
            int targetY = ResolveEntryMapY(target);
            string targetUid = target.DieUid ?? "";
            for (int i = 0; i < entries.Count; i++)
            {
                DieMapEntry entry = entries[i];
                if (entry == null)
                    continue;
                if (ResolveEntryMapX(entry) == targetX &&
                    ResolveEntryMapY(entry) == targetY &&
                    string.Equals(entry.DieUid ?? "", targetUid, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool IsSameEntry(DieMapEntry left, DieMapEntry right)
        {
            if (left == null || right == null)
                return false;

            return ResolveEntryMapX(left) == ResolveEntryMapX(right) &&
                   ResolveEntryMapY(left) == ResolveEntryMapY(right) &&
                   string.Equals(left.DieUid ?? "", right.DieUid ?? "", StringComparison.OrdinalIgnoreCase);
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

        private struct PhysicalBounds
        {
            public double MinX;
            public double MinY;
            public double MaxX;
            public double MaxY;
            public double Width => Math.Max(0.000001, MaxX - MinX);
            public double Height => Math.Max(0.000001, MaxY - MinY);
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
