using System;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 위치별(Index X=열, Index Y=행) 사각 격자 히트맵 — 실제 장비 운영뷰의 Map 1칸.
    /// 값(0~1, 0=흰색·1=적색)을 셀 색으로 표시하고 NaN=빈칸. 상단에 캡션 1줄.
    /// 웨이퍼 원형(WaferMapPanel)과 달리 기판 격자 그대로(직사각) — 레퍼런스 4-맵 구성용.
    /// </summary>
    public class PositionMapPanel : Control
    {
        private double[,] _v;     // [row, col] 0~1, NaN=빈칸
        private string _caption = "";

        private static readonly Color BgCol  = Color.FromArgb(0x1A, 0x1A, 0x1E);
        private static readonly Color White  = Color.FromArgb(0xF6, 0xF6, 0xF6);
        private static readonly Color Red     = Color.FromArgb(0xD0, 0x2A, 0x2A);
        private static readonly Color GridCol = Color.FromArgb(0x33, 0x33, 0x3A);

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
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BgCol);

            const int capH = 16;
            using (var cf = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (var cb = new SolidBrush(Color.Gainsboro))
                g.DrawString(_caption, cf, cb, 3, 1);

            if (_v == null) return;
            int rows = _v.GetLength(0), cols = _v.GetLength(1);
            if (rows == 0 || cols == 0) return;

            float top = capH + 2;
            float availW = Width - 6, availH = Height - top - 4;
            if (availW <= 2 || availH <= 2) return;
            float cw = availW / cols, ch = availH / rows;
            float ox = 3, oy = top;

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    double val = _v[r, c];
                    float x = ox + c * cw, y = oy + r * ch;
                    if (double.IsNaN(val))
                    {
                        using (var bp = new Pen(GridCol)) g.DrawRectangle(bp, x, y, cw - 1, ch - 1);
                        continue;
                    }
                    using (var br = new SolidBrush(Lerp(White, Red, Clamp01(val))))
                        g.FillRectangle(br, x, y, cw - 0.5f, ch - 0.5f);
                }
        }

        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

        private static Color Lerp(Color a, Color b, double t)
            => Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
    }
}
