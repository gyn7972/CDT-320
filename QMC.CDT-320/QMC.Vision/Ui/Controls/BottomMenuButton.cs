using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 하단 네비게이션 바 버튼 — 상단 원형 아이콘 + 한글 라벨.
    /// QMC.CDT_320 Handler 의 BottomMenuButton 과 동일 스타일.
    /// </summary>
    public class BottomMenuButton : Control
    {
        private bool _hover;
        private bool _selected;

        public string IconText { get; set; } = "●";
        public string Label    { get; set; } = "";

        public bool Selected
        {
            get => _selected;
            set { _selected = value; Invalidate(); }
        }

        public BottomMenuButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            Size      = new Size(110, 70);
            Cursor    = Cursors.Hand;
            Font      = UiTheme.BottomBtnFont;
            ForeColor = UiTheme.BottomBarFg;
            BackColor = UiTheme.BottomBarBg;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            Color bg = _selected ? UiTheme.Accent
                     : _hover    ? Color.FromArgb(0x45, 0x45, 0x48)
                                  : BackColor;
            using (var b = new SolidBrush(bg))
                g.FillRectangle(b, ClientRectangle);

            int ciD = 32;
            var cRect = new Rectangle((Width - ciD) / 2, 6, ciD, ciD);
            using (var p = new Pen(ForeColor, 1.3f))
                g.DrawEllipse(p, cRect);
            using (var fg = new SolidBrush(ForeColor))
            using (var iconFont = new Font(Font.FontFamily, 11F, FontStyle.Regular))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(IconText ?? "", iconFont, fg, cRect, sf);

                var lblRect = new Rectangle(0, cRect.Bottom + 2, Width, Height - cRect.Bottom - 4);
                g.DrawString(Label ?? "", Font, fg, lblRect, sf);
            }
        }
    }
}
