using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// 300 스타일 "ACTION" 버튼 — 상단 좌측에 작은 오렌지 "ACTION" 라벨 + 중앙 큰 한글 텍스트.
    /// 회색 배경, 평면.
    /// </summary>
    public class ActionButton : Control
    {
        private bool _hover;
        private bool _down;

        public ActionButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            Size      = new Size(150, 64);
            Cursor    = Cursors.Hand;
            Font      = new Font("맑은 고딕", 12F, FontStyle.Bold);
            ForeColor = Color.White;
            BackColor = Color.FromArgb(0x80, 0x80, 0x80);
        }

        protected override void OnMouseEnter(System.EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(System.EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e)    { _down = true;   Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e)      { _down = false;  Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            Color bg = Enabled
                ? (_down ? Color.FromArgb(0x5A, 0x5A, 0x5A)
                  : _hover ? Color.FromArgb(0x75, 0x75, 0x75)
                           : BackColor)
                : Color.FromArgb(0xC0, 0xC0, 0xC0);

            using (var b = new SolidBrush(bg)) g.FillRectangle(b, ClientRectangle);

            using (var actFont = new Font("맑은 고딕", 8F, FontStyle.Bold))
            using (var actBrush = new SolidBrush(Color.FromArgb(0xF5, 0xA6, 0x23)))
                g.DrawString("ACTION", actFont, actBrush, 6, 4);

            using (var fg = new SolidBrush(Enabled ? ForeColor : Color.FromArgb(0x90, 0x90, 0x90)))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var r  = new Rectangle(0, 14, Width, Height - 14);
                g.DrawString(Text, Font, fg, r, sf);
            }
        }
    }
}
