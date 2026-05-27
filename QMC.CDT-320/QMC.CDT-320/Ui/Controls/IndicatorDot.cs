using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// ON/OFF 상태에 따라 색이 바뀌는 작은 원형 인디케이터.
    /// </summary>
    public class IndicatorDot : Control
    {
        private bool  _isOn;
        private Color _onColor  = Color.LimeGreen;
        private Color _offColor = Color.FromArgb(0x55, 0x55, 0x55);

        public IndicatorDot()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size      = new Size(14, 14);
        }

        [DefaultValue(false)]
        public bool IsOn
        {
            get => _isOn;
            set { _isOn = value; Invalidate(); }
        }

        public Color OnColor
        {
            get => _onColor;
            set { _onColor = value; Invalidate(); }
        }

        public Color OffColor
        {
            get => _offColor;
            set { _offColor = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = _isOn ? _onColor : _offColor;
            int pad = 1;
            var rect = new Rectangle(pad, pad, Width - pad * 2 - 1, Height - pad * 2 - 1);
            using (var b = new SolidBrush(fill))
                e.Graphics.FillEllipse(b, rect);
            using (var p = new Pen(Color.FromArgb(100, 0, 0, 0), 1f))
                e.Graphics.DrawEllipse(p, rect);
        }
    }
}
