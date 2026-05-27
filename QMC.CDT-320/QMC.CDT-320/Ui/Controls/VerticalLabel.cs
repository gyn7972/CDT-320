using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// 세로 방향으로 텍스트를 출력하는 라벨 (좌/우 MENU 기둥).
    /// </summary>
    public class VerticalLabel : Control
    {
        public VerticalLabel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode   = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            using (var bg = new SolidBrush(BackColor))
                g.FillRectangle(bg, ClientRectangle);

            // 상단 → 하단으로 글자 쌓기
            using (var fg = new SolidBrush(ForeColor))
            {
                var sf = new StringFormat
                {
                    Alignment     = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                float charH = Font.Size * 1.4f;
                float y = (Height - charH * Text.Length) / 2f;
                foreach (char c in Text)
                {
                    var rect = new RectangleF(0, y, Width, charH);
                    g.DrawString(c.ToString(), Font, fg, rect, sf);
                    y += charH;
                }
            }
        }
    }
}
