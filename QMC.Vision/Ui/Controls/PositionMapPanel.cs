using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 위치별(Index X=열, Index Y=행) 사각 격자 히트맵 — 실제 장비 운영뷰의 Map 1칸.
    /// 값(0~1, 0=흰색·1=적색)을 셀 색으로 표시하고 NaN=빈칸. 상단에 캡션 1줄.
    ///
    /// 성능: 셀 격자는 데이터/크기 변경 시 <b>Bitmap 캐시</b>에 1회만 렌더하고, 매 프레임(확대·팬·호버)에는
    ///       변환된 <see cref="Graphics.DrawImage(Image, RectangleF)"/> 한 번만 그린다. 셀 개수와 무관하게
    ///       페인트 비용이 일정 → 휠 줌/드래그/호버가 가볍다.
    ///
    /// UX: 휠=커서 기준 확대/축소, 드래그=팬(확대 상태), 더블클릭=원위치+선택 해제,
    ///     좌클릭=셀 선택(같은 셀 재클릭·빈 곳 클릭=해제), 우클릭=선택 해제,
    ///     호버 시 <see cref="CellInfoProvider"/> 텍스트를 툴팁으로 표시.
    ///     확대/선택 상태는 <see cref="ViewChanged"/>/<see cref="SelectionChanged"/> 로 알려 4맵을 동기화한다.
    /// </summary>
    public class PositionMapPanel : Control
    {
        private double[,] _v;     // [row, col] 0~1, NaN=빈칸
        private string _caption = "";

        private static readonly Color BgCol  = Color.FromArgb(0x1A, 0x1A, 0x1E);
        private static readonly Color White  = Color.FromArgb(0xF6, 0xF6, 0xF6);
        private static readonly Color Red     = Color.FromArgb(0xD0, 0x2A, 0x2A);
        private static readonly Color GridCol = Color.FromArgb(0x33, 0x33, 0x3A);
        private static readonly Color SelCol  = Color.FromArgb(0xF2, 0xC8, 0x2A);   // 선택 셀 테두리(노랑)
        private static readonly Color TipBg   = Color.FromArgb(0xE6, 0x10, 0x10, 0x16);
        private static readonly Color TipEdge = Color.FromArgb(0x66, 0x66, 0x70);

        private const int CapH   = 16;
        private const float MinScale = 1f;
        private const float MaxScale = 40f;

        // 뷰(확대/팬) 상태 — 화면좌표 = off + scale * 논리좌표
        private float _scale = 1f, _offX = 0f, _offY = 0f;
        // 드래그(팬) 상태
        private bool _dragging;
        private Point _lastPt, _downPt;
        // 호버/선택 상태
        private int _hoverCol = -1, _hoverRow = -1;
        private int _selCol = -1, _selRow = -1;
        private Point _mousePt;

        // 셀 격자 캐시(데이터/크기 변경 시에만 재생성)
        private Bitmap _cache;
        private bool _cacheDirty = true;

        /// <summary>셀 클릭(선택) 시 (IndexX=col, IndexY=row) 전달.</summary>
        public event Action<int, int> CellClicked;
        /// <summary>사용자 입력으로 뷰(scale, offX, offY)가 바뀔 때 발생(동기화용).</summary>
        public event Action<float, float, float> ViewChanged;
        /// <summary>선택 셀이 바뀔 때 (col, row) 발생. (-1, -1)=선택 해제.</summary>
        public event Action<int, int> SelectionChanged;

        /// <summary>호버 셀(col,row)의 툴팁 텍스트를 반환. null 반환 시 툴팁 미표시.</summary>
        public Func<int, int, string> CellInfoProvider { get; set; }

        public PositionMapPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = BgCol;
        }

        /// <summary>caption=상단 제목, grid[row,col]=0~1 정규화 값(NaN=빈칸).</summary>
        public void SetData(string caption, double[,] grid)
        {
            _caption = caption ?? "";
            _v = grid;
            _hoverCol = _hoverRow = -1;
            _cacheDirty = true;      // 데이터가 바뀌면 격자 캐시 재생성
            Invalidate();
        }

        // ── 외부 동기화 API ────────────────────────────────────────────────
        /// <summary>ViewChanged 재발생 없이 뷰(확대/팬) 상태를 적용(4맵 동기화용).</summary>
        public void SetView(float scale, float offX, float offY)
        {
            _scale = Clamp(scale, MinScale, MaxScale);
            _offX = offX; _offY = offY;
            Invalidate();
        }

        /// <summary>SelectionChanged 재발생 없이 선택 셀을 적용(4맵 동기화용). -1=해제.</summary>
        public void SetSelection(int col, int row)
        {
            _selCol = col; _selRow = row;
            Invalidate();
        }

        /// <summary>뷰 초기화 + 선택 해제(더블클릭과 동일). 이벤트도 발생시킨다.</summary>
        public void ResetView()
        {
            _scale = 1f; _offX = 0f; _offY = 0f;
            _selCol = _selRow = -1;
            Invalidate();
            ViewChanged?.Invoke(_scale, _offX, _offY);
            SelectionChanged?.Invoke(-1, -1);
        }

        // ── 렌더링 ─────────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BgCol);

            using (var cf = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (var cb = new SolidBrush(Color.Gainsboro))
                g.DrawString(_caption, cf, cb, 3, 1);

            if (!TryGeometry(out int rows, out int cols, out float ox, out float oy,
                             out float cw, out float ch, out float availW, out float availH))
                return;

            EnsureCache(rows, cols);

            float top = CapH + 2;
            g.SetClip(new RectangleF(0, top - 1, Width, Height - (top - 1)));

            if (_cache != null)
            {
                var dst = new RectangleF(_offX + _scale * ox, _offY + _scale * oy,
                                         _scale * availW, _scale * availH);
                var oldInt = g.InterpolationMode; var oldPx = g.PixelOffsetMode;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(_cache, dst);
                g.InterpolationMode = oldInt; g.PixelOffsetMode = oldPx;
            }

            // 선택 하이라이트 — 화면좌표에서 일정 두께로
            if (_selCol >= 0 && _selCol < cols && _selRow >= 0 && _selRow < rows)
            {
                float sx = _offX + _scale * (ox + _selCol * cw);
                float sy = _offY + _scale * (oy + _selRow * ch);
                using (var sp = new Pen(SelCol, 2f))
                    g.DrawRectangle(sp, sx, sy, _scale * cw, _scale * ch);
            }
            g.ResetClip();

            DrawTooltip(g);
        }

        /// <summary>셀 격자를 캐시 비트맵에 렌더(데이터/크기 변경 시에만). 셀별 GDI 할당은 여기서 1회.</summary>
        private void EnsureCache(int rows, int cols)
        {
            float top = CapH + 2;
            int iw = Math.Max(1, (int)Math.Round(Width - 6f));
            int ih = Math.Max(1, (int)Math.Round(Height - top - 4f));
            if (_cache != null && !_cacheDirty && _cache.Width == iw && _cache.Height == ih) return;

            var bmp = new Bitmap(iw, ih);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(BgCol);
                float cw = iw / (float)cols, ch = ih / (float)rows;
                using (var gp = new Pen(GridCol) { Width = 0 })
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                        {
                            double val = _v[r, c];
                            float x = c * cw, y = r * ch;
                            if (double.IsNaN(val)) { g.DrawRectangle(gp, x, y, cw - 1, ch - 1); continue; }
                            using (var br = new SolidBrush(Lerp(White, Red, Clamp01(val))))
                                g.FillRectangle(br, x, y, cw - 0.5f, ch - 0.5f);
                        }
            }
            var old = _cache; _cache = bmp; old?.Dispose();
            _cacheDirty = false;
        }

        private void DrawTooltip(Graphics g)
        {
            if (_hoverCol < 0 || _hoverRow < 0) return;
            var provider = CellInfoProvider;
            if (provider == null) return;
            string text = provider(_hoverCol, _hoverRow);
            if (string.IsNullOrEmpty(text)) return;

            using (var tf = new Font("Segoe UI", 8f))
            {
                SizeF sz = g.MeasureString(text, tf);
                float pad = 5f;
                float bw = sz.Width + pad * 2, bh = sz.Height + pad * 2;
                float bx = _mousePt.X + 14, by = _mousePt.Y + 14;
                if (bx + bw > Width) bx = _mousePt.X - bw - 6;
                if (by + bh > Height) by = _mousePt.Y - bh - 6;
                if (bx < 0) bx = 0; if (by < CapH) by = CapH;
                using (var bg = new SolidBrush(TipBg)) g.FillRectangle(bg, bx, by, bw, bh);
                using (var ep = new Pen(TipEdge)) g.DrawRectangle(ep, bx, by, bw, bh);
                using (var tb = new SolidBrush(Color.Gainsboro)) g.DrawString(text, tf, tb, bx + pad, by + pad);
            }
        }

        // ── 지오메트리/히트테스트 ──────────────────────────────────────────
        private bool TryGeometry(out int rows, out int cols, out float ox, out float oy,
                                 out float cw, out float ch, out float availW, out float availH)
        {
            rows = cols = 0; ox = oy = cw = ch = availW = availH = 0;
            if (_v == null) return false;
            rows = _v.GetLength(0); cols = _v.GetLength(1);
            if (rows == 0 || cols == 0) return false;
            float top = CapH + 2;
            availW = Width - 6; availH = Height - top - 4;
            if (availW <= 2 || availH <= 2) return false;
            cw = availW / cols; ch = availH / rows; ox = 3; oy = top;
            return true;
        }

        /// <summary>화면 점 → 셀(col,row). 격자 밖이면 false.</summary>
        private bool HitCell(Point p, out int col, out int row)
        {
            col = row = -1;
            if (!TryGeometry(out int rows, out int cols, out float ox, out float oy,
                             out float cw, out float ch, out float aw, out float ah))
                return false;
            float lx = (p.X - _offX) / _scale, ly = (p.Y - _offY) / _scale;
            int c = (int)Math.Floor((lx - ox) / cw), r = (int)Math.Floor((ly - oy) / ch);
            if (c < 0 || c >= cols || r < 0 || r >= rows) return false;
            col = c; row = r; return true;
        }

        // ── 입력 처리 ─────────────────────────────────────────────────────
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            float factor = e.Delta > 0 ? 1.15f : 1f / 1.15f;
            float ns = Clamp(_scale * factor, MinScale, MaxScale);
            if (Math.Abs(ns - _scale) < 1e-4f) return;
            if (ns <= MinScale) { _scale = MinScale; _offX = 0f; _offY = 0f; }
            else
            {
                _offX = e.X - (ns / _scale) * (e.X - _offX);
                _offY = e.Y - (ns / _scale) * (e.Y - _offY);
                _scale = ns;
            }
            Invalidate();
            ViewChanged?.Invoke(_scale, _offX, _offY);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button == MouseButtons.Left)
            {
                _downPt = e.Location; _lastPt = e.Location;
                _dragging = _scale > MinScale;   // 확대 상태에서만 팬
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _mousePt = e.Location;

            if (_dragging && e.Button == MouseButtons.Left)
            {
                _offX += e.X - _lastPt.X; _offY += e.Y - _lastPt.Y;
                _lastPt = e.Location;
                Invalidate();
                ViewChanged?.Invoke(_scale, _offX, _offY);
                return;
            }

            HitCell(e.Location, out int hc, out int hr);
            bool tipVisible = hc >= 0 && CellInfoProvider != null;
            if (hc != _hoverCol || hr != _hoverRow) { _hoverCol = hc; _hoverRow = hr; Invalidate(); }
            else if (tipVisible) Invalidate();   // 툴팁이 커서를 따라오도록(캐시 덕에 저비용)
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            bool wasDragging = _dragging;
            _dragging = false;

            if (e.Button == MouseButtons.Right) { ClearSelection(); return; }   // 우클릭=선택 해제
            if (e.Button != MouseButtons.Left) return;

            int dx = Math.Abs(e.X - _downPt.X), dy = Math.Abs(e.Y - _downPt.Y);
            bool isClick = dx <= 3 && dy <= 3;   // 이동 거의 없으면 클릭
            if (wasDragging && !isClick) return; // 팬 종료

            if (HitCell(e.Location, out int col, out int row))
            {
                if (col == _selCol && row == _selRow) { ClearSelection(); return; }  // 같은 셀 재클릭=해제
                _selCol = col; _selRow = row;
                Invalidate();
                SelectionChanged?.Invoke(col, row);
                CellClicked?.Invoke(col, row);
            }
            else ClearSelection();   // 빈 곳 클릭=해제
        }

        /// <summary>선택 표시(노란 테두리) 해제 + 전파.</summary>
        private void ClearSelection()
        {
            if (_selCol < 0 && _selRow < 0) { Invalidate(); return; }
            _selCol = _selRow = -1;
            Invalidate();
            SelectionChanged?.Invoke(-1, -1);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            ResetView();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (CanFocus && !Focused) Focus();   // 클릭 없이 휠 확대 가능하도록
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverCol >= 0 || _hoverRow >= 0) { _hoverCol = _hoverRow = -1; Invalidate(); }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            _cacheDirty = true;   // 크기 바뀌면 격자 캐시 재생성
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _cache?.Dispose(); _cache = null; }
            base.Dispose(disposing);
        }

        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

        private static Color Lerp(Color a, Color b, double t)
            => Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
    }
}
