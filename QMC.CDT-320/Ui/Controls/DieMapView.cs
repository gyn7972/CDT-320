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

        public event Action<DieMapEntry> CellClicked;

        /// <summary>현재 표시 중인 다이 맵.</summary>
        public DieMap Map
        {
            get => _map;
            set { _map = value; _hover = null; Invalidate(); }
        }

        /// <summary>좌상단 정보 라벨에 표시할 추가 텍스트.</summary>
        public string Caption { get; set; } = "Die Map";

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

            if (_map == null || _map.GridX <= 0 || _map.GridY <= 0)
            {
                using (var br = new SolidBrush(Color.Gray))
                using (var f  = new Font("Segoe UI", 14F))
                    g.DrawString("(no map)", f, br,
                        (Width - 100) / 2.0f, (Height - 30) / 2.0f);
                return;
            }

            // 셀 크기 산정
            int margin = 30;
            int titleH = 30;
            int legendH = 28;
            int availableW = Width - margin * 2;
            int availableH = Height - titleH - legendH - margin;
            int cellSize = Math.Max(2, Math.Min(availableW / _map.GridX, availableH / _map.GridY));

            int totalW = cellSize * _map.GridX;
            int totalH = cellSize * _map.GridY;
            int x0 = (Width - totalW) / 2;
            int y0 = titleH + (availableH - totalH) / 2;

            // 셀 그리기
            foreach (var entry in _map.Entries)
            {
                int x = x0 + entry.GridX * cellSize;
                int y = y0 + entry.GridY * cellSize;
                Color c = !entry.IsTarget
                    ? Color.FromArgb(60, 60, 60)
                    : (entry.BinCode > 0
                        ? BinCodeMap.ConvertToBinCodeColor(entry.BinCode)
                        : Color.FromArgb(80, 80, 100));   // unknown
                using (var br = new SolidBrush(c))
                    g.FillRectangle(br, x, y, cellSize - 1, cellSize - 1);
            }

            // hover 강조
            if (_hover != null)
            {
                int x = x0 + _hover.GridX * cellSize;
                int y = y0 + _hover.GridY * cellSize;
                using (var pen = new Pen(Color.Yellow, 2f))
                    g.DrawRectangle(pen, x, y, cellSize - 1, cellSize - 1);
            }

            // 좌상단 정보
            using (var br = new SolidBrush(Color.WhiteSmoke))
            using (var f  = new Font("Consolas", 9F))
            {
                string info = $"{_map.GridX}×{_map.GridY}  pitch=({_map.PitchX:F2},{_map.PitchY:F2})mm  total={_map.TotalCells}";
                g.DrawString(info, f, br, 8, 24);
                if (_hover != null)
                {
                    string h = $"[{_hover.GridX},{_hover.GridY}] result={_hover.Result} bin={_hover.BinCode}";
                    g.DrawString(h, f, br, 8, Height - 18);
                }
            }

            // 범례 (간이): Good / NG / Unknown
            DrawLegend(g, totalW, x0, y0 + totalH + 6);
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
            int margin = 30;
            int titleH = 30;
            int legendH = 28;
            int availableW = Width - margin * 2;
            int availableH = Height - titleH - legendH - margin;
            int cellSize = Math.Max(2, Math.Min(availableW / _map.GridX, availableH / _map.GridY));
            int totalW = cellSize * _map.GridX;
            int totalH = cellSize * _map.GridY;
            int x0 = (Width - totalW) / 2;
            int y0 = titleH + (availableH - totalH) / 2;

            int gx = (mouseX - x0) / cellSize;
            int gy = (mouseY - y0) / cellSize;
            if (gx < 0 || gx >= _map.GridX || gy < 0 || gy >= _map.GridY) return null;
            return _map.GetCell(gx, gy);
        }

        private void OnMouseMoveEvt(object s, MouseEventArgs e)
        {
            var hit = HitTest(e.X, e.Y);
            if (hit != _hover) { _hover = hit; Invalidate(); }
        }

        private void OnMouseClick(object s, MouseEventArgs e)
        {
            var hit = HitTest(e.X, e.Y);
            if (hit != null) try { CellClicked?.Invoke(hit); } catch { }
        }
    }
}
