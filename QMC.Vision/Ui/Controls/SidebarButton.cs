using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>세팅 상태점 — 미설정/설정완료/변경됨.</summary>
    public enum SidebarStatus { Off, Done, Dirty }

    /// <summary>
    /// R2c — Handler(QMC.CDT_320.Ui.Controls.SidebarButton) 비주얼 1:1 미러(결합 제외).
    /// 선택=흰배경/진한글씨, hover=#707070, 기본=#595959/흰글씨, 하단 1px 구분선.
    /// + 상태점(좌측 원, 페인트로 직접 그림 — Button 자식 Panel 렌더누락 해소).
    /// </summary>
    public class SidebarButton : Control
    {
        private bool _selected;
        private bool _hover;
        private SidebarStatus _status = SidebarStatus.Off;
        private bool _showDot = true;

        public bool Selected
        {
            get => _selected;
            set { _selected = value; Invalidate(); }
        }

        public SidebarStatus Status
        {
            get => _status;
            set { _status = value; Invalidate(); }
        }

        /// <summary>상태점 표시 여부(기본 true). false 면 점 없이 텍스트만.</summary>
        public bool ShowStatusDot
        {
            get => _showDot;
            set { _showDot = value; Invalidate(); }
        }

        public SidebarButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            Size = new Size(202, 46);
            Cursor = Cursors.Hand;
            Font = UiTheme.ButtonFont;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            Color bg = _selected ? UiTheme.SidebarBtnSelBg
                                 : (_hover ? Color.FromArgb(0x70, 0x70, 0x70) : UiTheme.SidebarBtnBg);
            Color fg = _selected ? UiTheme.SidebarBtnSelFg : UiTheme.SidebarBtnFg;

            using (var b = new SolidBrush(bg))
                g.FillRectangle(b, ClientRectangle);

            // 하단 1px 구분선 (Handler 동일)
            using (var p = new Pen(Color.FromArgb(0x40, 0x40, 0x40), 1f))
                g.DrawLine(p, 0, Height - 1, Width, Height - 1);

            // 상태점 (좌측 원) — 페인트로 직접
            int textLeft = 6;
            if (_showDot)
            {
                Color dot;
                switch (_status)
                {
                    case SidebarStatus.Done:  dot = Color.FromArgb(0x2E, 0x7D, 0x32); break;
                    case SidebarStatus.Dirty: dot = Color.FromArgb(0xE8, 0x85, 0x1A); break;
                    default:                  dot = Color.FromArgb(0x8C, 0x8C, 0x8C); break;
                }
                int d = 10;
                int cy = (Height - d) / 2;
                using (var db = new SolidBrush(dot))
                    g.FillEllipse(db, 8, cy, d, d);
                textLeft = 24;
            }

            using (var fgb = new SolidBrush(fg))
            {
                var rect = new Rectangle(textLeft, 0, Width - textLeft - 4, Height);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(Text, Font, fgb, rect, sf);
            }
        }
    }
}
