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
            set { _map = value; _hover = null; _selected = null; ResetView(); }
        }

        /// <summary>좌상단 정보 라벨에 표시할 추가 텍스트.</summary>
        public string Caption { get; set; } = "Die Map";

        public Func<DieMapEntry, Color> CellColorResolver { get; set; }

        public Func<DieMapEntry, string> CellTextResolver { get; set; }

        public Func<DieMapEntry, string> CellStatusResolver { get; set; }

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

            using (var br = new SolidBrush(Color.WhiteSmoke))
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
            float cellSize;
            GetMapLayout(out mapRect, out cellSize);

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

                float x = mapRect.Left + entry.DieMapX * cellSize;
                float y = mapRect.Top + entry.DieMapY * cellSize;
                if (x > Width || y > Height || x + cellSize < 0 || y + cellSize < 0)
                    continue;

                Color c = ResolveCellColor(entry);
                using (var br = new SolidBrush(c))
                    g.FillRectangle(br, x, y, Math.Max(1.0F, cellSize - 1.0F), Math.Max(1.0F, cellSize - 1.0F));

                string cellText = CellTextResolver != null
                    ? CellTextResolver(entry)
                    : (entry.SequenceNo > 0 ? entry.SequenceNo.ToString() : "");
                if (entry.IsTarget && !string.IsNullOrWhiteSpace(cellText) && cellSize >= 12)
                {
                    using (var br = new SolidBrush(Color.WhiteSmoke))
                    using (var f = new Font("Consolas", Math.Max(6F, cellSize * 0.32F), FontStyle.Regular))
                    {
                        SizeF size = g.MeasureString(cellText, f);
                        g.DrawString(cellText, f, br, x + (cellSize - size.Width) / 2.0F, y + (cellSize - size.Height) / 2.0F);
                    }
                }
            }

            if (_selected != null)
            {
                float x = mapRect.Left + _selected.DieMapX * cellSize;
                float y = mapRect.Top + _selected.DieMapY * cellSize;
                using (var pen = new Pen(Color.DeepSkyBlue, Math.Max(2.0F, Math.Min(4.0F, cellSize / 6.0F))))
                    g.DrawRectangle(pen, x, y, Math.Max(1.0F, cellSize - 1.0F), Math.Max(1.0F, cellSize - 1.0F));
            }

            // hover 강조
            if (_hover != null)
            {
                float x = mapRect.Left + _hover.DieMapX * cellSize;
                float y = mapRect.Top + _hover.DieMapY * cellSize;
                using (var pen = new Pen(Color.Yellow, 2f))
                    g.DrawRectangle(pen, x, y, Math.Max(1.0F, cellSize - 1.0F), Math.Max(1.0F, cellSize - 1.0F));
            }

            // 좌상단 정보
            using (var br = new SolidBrush(Color.WhiteSmoke))
            using (var f  = new Font("Consolas", 9F))
            {
                string info = $"{_map.DieMapX}×{_map.DieMapY}  pitch=({_map.PitchX:F2},{_map.PitchY:F2})mm  total={_map.TotalCells}  zoom={_zoom * 100.0F:F0}%";
                g.DrawString(info, f, br, 8, 24);
                if (_hover != null)
                {
                    string status = CellStatusResolver != null ? CellStatusResolver(_hover) : _hover.Result.ToString();
                    string h = $"seq={_hover.SequenceNo} [{_hover.DieMapX},{_hover.DieMapY}] xy=({_hover.PosX:F3},{_hover.PosY:F3}) state={status} bin={_hover.BinCode} uid={_hover.DieUid}";
                    g.DrawString(h, f, br, 8, Height - 18);
                }
            }

            // 범례 (간이): Good / NG / Unknown
            DrawLegend(g, (int)mapRect.Width, (int)mapRect.Left, (int)(mapRect.Bottom + 6.0F));
        }

        public void ResetView()
        {
            _zoom = 1.0F;
            _panX = 0.0F;
            _panY = 0.0F;
            Invalidate();
        }

        private void DrawLegend(Graphics g, int totalW, int x0, int y)
        {
            using (var f = new Font("Consolas", 8F))
            {
                int sx = x0;
                int sw = 14;
                int gap = 80;
                var items = new (string label, Color color)[]
                {
                    ("Good",     BinCodeMap.ConvertToBinCodeColor(BinCodeMap.GoodBin)),
                    ("Pre-NG",   BinCodeMap.ConvertToBinCodeColor(110)),
                    ("Critical", BinCodeMap.ConvertToBinCodeColor(200)),
                    ("Unknown",  Color.FromArgb(80, 80, 100)),
                    ("Skip",     Color.FromArgb(60, 60, 60)),
                };
                foreach (var it in items)
                {
                    using (var br = new SolidBrush(it.color))
                        g.FillRectangle(br, sx, y, sw, 12);
                    using (var br = new SolidBrush(Color.WhiteSmoke))
                        g.DrawString(it.label, f, br, sx + sw + 3, y - 1);
                    sx += gap;
                }
            }
        }

        private DieMapEntry HitTest(int mouseX, int mouseY)
        {
            if (_map == null) return null;
            RectangleF mapRect;
            float cellSize;
            GetMapLayout(out mapRect, out cellSize);

            int gx = (int)Math.Floor((mouseX - mapRect.Left) / cellSize);
            int gy = (int)Math.Floor((mouseY - mapRect.Top) / cellSize);
            if (gx < 0 || gx >= _map.DieMapX || gy < 0 || gy >= _map.DieMapY) return null;
            return _map.GetCell(gx, gy);
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
            float beforeCell;
            GetMapLayout(out beforeRect, out beforeCell);
            float mapX = (e.X - beforeRect.Left) / beforeCell;
            float mapY = (e.Y - beforeRect.Top) / beforeCell;

            float factor = e.Delta > 0 ? 1.2F : 0.8333333F;
            _zoom = Math.Max(0.2F, Math.Min(30.0F, _zoom * factor));

            RectangleF afterRect;
            float afterCell;
            GetMapLayout(out afterRect, out afterCell);
            _panX += e.X - (afterRect.Left + mapX * afterCell);
            _panY += e.Y - (afterRect.Top + mapY * afterCell);
            Invalidate();
        }

        private void GetMapLayout(out RectangleF mapRect, out float cellSize)
        {
            int margin = 30;
            int titleH = 30;
            int legendH = 28;
            int availableW = Math.Max(1, Width - margin * 2);
            int availableH = Math.Max(1, Height - titleH - legendH - margin);
            float baseCellSize = Math.Max(2.0F, Math.Min(
                availableW / Math.Max(1.0F, _map != null ? _map.DieMapX : 1.0F),
                availableH / Math.Max(1.0F, _map != null ? _map.DieMapY : 1.0F)));
            cellSize = Math.Max(1.0F, baseCellSize * _zoom);

            float totalW = cellSize * (_map != null ? _map.DieMapX : 1);
            float totalH = cellSize * (_map != null ? _map.DieMapY : 1);
            float x0 = (Width - totalW) / 2.0F + _panX;
            float y0 = titleH + (availableH - totalH) / 2.0F + _panY;
            mapRect = new RectangleF(x0, y0, totalW, totalH);
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
    }
}
