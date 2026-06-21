using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// 우측 사이드바에 쓰는 사각 버튼.
    /// 선택 시 흰색 배경 + 진한 텍스트, 비선택 시 다크 + 흰 텍스트.
    /// </summary>
    public class SidebarButton : Control
    {
        private bool _selected;
        private bool _hover;
        private Color? _stateBackColor;
        private Color? _stateForeColor;

        public bool Selected
        {
            get => _selected;
            set { _selected = value; Invalidate(); }
        }

        public Color? StateBackColor
        {
            get { return _stateBackColor; }
            set { _stateBackColor = value; Invalidate(); }
        }

        public Color? StateForeColor
        {
            get { return _stateForeColor; }
            set { _stateForeColor = value; Invalidate(); }
        }

        public SidebarButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            Size   = new Size(180, 46);
            Cursor = Cursors.Hand;
            Font   = UiTheme.ButtonFont;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            Color bg = _selected ? UiTheme.SidebarBtnSelBg
                                 : (_stateBackColor.HasValue ? _stateBackColor.Value
                                                             : (_hover ? Color.FromArgb(0x70, 0x70, 0x70) : UiTheme.SidebarBtnBg));
            Color fg = _selected ? UiTheme.SidebarBtnSelFg
                                 : (_stateForeColor.HasValue ? _stateForeColor.Value : UiTheme.SidebarBtnFg);

            using (var b = new SolidBrush(bg))
                g.FillRectangle(b, ClientRectangle);

            // 얇은 구분선
            using (var p = new Pen(Color.FromArgb(0x40, 0x40, 0x40), 1f))
                g.DrawLine(p, 0, Height - 1, Width, Height - 1);

            using (var fgb = new SolidBrush(fg))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(Text, Font, fgb, ClientRectangle, sf);
            }
        }
    }
}
