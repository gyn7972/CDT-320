using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// Handler(QMC.CDT_320.Ui.Controls.ActionButton) 비주얼 1:1 미러.
    /// 회색 바탕 + 좌상단 주황 "ACTION" 캡션 + 중앙 텍스트. hover/down 음영.
    /// 인페이지 액션 버튼을 페이지마다 제각각 그리지 않고 이 컨트롤로 통일한다.
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
            Size = new Size(150, 44);
            Cursor = Cursors.Hand;
            Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            ForeColor = Color.White;
            BackColor = Color.FromArgb(0x80, 0x80, 0x80);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            Color bg = Enabled
                ? (_down ? Color.FromArgb(0x5A, 0x5A, 0x5A)
                  : _hover ? Color.FromArgb(0x75, 0x75, 0x75)
                           : BackColor)
                : Color.FromArgb(0xC0, 0xC0, 0xC0);

            using (SolidBrush b = new SolidBrush(bg))
                g.FillRectangle(b, ClientRectangle);

            using (Font actFont = new Font("맑은 고딕", 7.5F, FontStyle.Bold))
            using (SolidBrush actBrush = new SolidBrush(Color.FromArgb(0xF5, 0xA6, 0x23)))
                g.DrawString("ACTION", actFont, actBrush, 6, 4);

            using (SolidBrush fg = new SolidBrush(Enabled ? ForeColor : Color.FromArgb(0x90, 0x90, 0x90)))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                Rectangle r = new Rectangle(4, 12, Math.Max(1, Width - 8), Math.Max(1, Height - 14));
                g.DrawString(DisplayText(), Font, fg, r, sf);
            }
        }

        private string DisplayText()
        {
            try
            {
                string text = Text ?? string.Empty;
                string[] lines = text.Replace("\r", string.Empty).Split('\n');
                if (lines.Length > 0 && string.Equals(lines[0].Trim(), "ACTION", StringComparison.OrdinalIgnoreCase))
                {
                    text = string.Join(" ", lines, 1, lines.Length - 1).Trim();
                    while (text.Contains("  "))
                        text = text.Replace("  ", " ");
                }

                return string.IsNullOrWhiteSpace(text) ? Text : text;
            }
            catch
            {
                return Text;
            }
        }
    }
}
