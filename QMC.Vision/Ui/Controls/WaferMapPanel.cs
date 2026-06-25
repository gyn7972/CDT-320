using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 웨이퍼 맵 — 원형 웨이퍼 위에 die 격자를 그리고, 각 die 를 편차비(0~1)로 채색한다.
    /// 0(규격 중앙선)=흰색 → 1(상/하한 적색선 근접)=붉은색. 칩핑 등 지표 분포 표시용.
    /// SetData(double[,]) 의 값은 0~1 편차비, NaN = die 없음(웨이퍼 밖/미측정).
    /// </summary>
    public class WaferMapPanel : Control
    {
        private double[,] _v;   // [row, col] 편차비 0~1, NaN=빈칸

        private static readonly Color BgCol   = Color.FromArgb(0x1A, 0x1A, 0x1E);
        private static readonly Color White   = Color.FromArgb(0xF2, 0xF2, 0xF2);
        private static readonly Color Red      = Color.FromArgb(0xE2, 0x4B, 0x4A);
        private static readonly Color RingCol  = Color.FromArgb(0x66, 0x66, 0x70);

        public WaferMapPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = BgCol;
        }

        public void SetData(double[,] devRatio)
        {
            _v = devRatio;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BgCol);
            if (_v == null) return;

            int rows = _v.GetLength(0), cols = _v.GetLength(1);
            if (rows == 0 || cols == 0) return;

            float side = Math.Min(Width, Height) - 8;
            float ox = (Width - side) / 2f, oy = (Height - side) / 2f;
            float cw = side / cols, ch = side / rows;
            float cx = Width / 2f, cy = Height / 2f;
            float radius = side / 2f;

            // 웨이퍼 외곽(원 + 하단 플랫)
            using (var ring = new Pen(RingCol, 1.5f))
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(ox, oy, side, side);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawPath(ring, path);
                // 하단 플랫
                g.DrawLine(ring, cx - radius * 0.5f, oy + side - 2, cx + radius * 0.5f, oy + side - 2);
                g.SmoothingMode = SmoothingMode.None;
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double val = _v[r, c];
                    if (double.IsNaN(val)) continue;
                    float px = ox + (c + 0.5f) * cw;
                    float py = oy + (r + 0.5f) * ch;
                    float dx = px - cx, dy = py - cy;
                    if (dx * dx + dy * dy > radius * radius) continue;   // 웨이퍼 밖
                    using (var br = new SolidBrush(Lerp(White, Red, Clamp01(val))))
                        g.FillRectangle(br, ox + c * cw + 0.5f, oy + r * ch + 0.5f, cw - 1f, ch - 1f);
                }
            }
        }

        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

        private static Color Lerp(Color a, Color b, double t)
        {
            int R = (int)(a.R + (b.R - a.R) * t);
            int G = (int)(a.G + (b.G - a.G) * t);
            int B = (int)(a.B + (b.B - a.B) * t);
            return Color.FromArgb(R, G, B);
        }
    }
}
