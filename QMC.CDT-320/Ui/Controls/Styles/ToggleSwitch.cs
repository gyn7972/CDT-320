using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// ON/OFF 토글 스위치 모양 인디케이터. 출력/실린더처럼 사용자가 클릭해 조작 가능한 IO에 사용해
    /// 읽기전용 입력(IndicatorDot)과 시각적으로 구분한다.
    /// </summary>
    public class ToggleSwitch : Control
    {
        private bool  _isOn;
        private Color _onColor   = Color.LimeGreen;
        private Color _offColor  = Color.FromArgb(0x55, 0x55, 0x55);
        private Color _knobColor = Color.White;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size      = new Size(28, 16);
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

        public Color KnobColor
        {
            get => _knobColor;
            set { _knobColor = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Width - 1;
            int h = Height - 1;
            Color track = _isOn ? _onColor : _offColor;

            using (var path = BuildPill(0, 0, w, h))
            {
                using (var b = new SolidBrush(track))
                    e.Graphics.FillPath(b, path);
                using (var p = new Pen(Color.FromArgb(100, 0, 0, 0), 1f))
                    e.Graphics.DrawPath(p, path);
            }

            int knobD = h - 3;
            int knobX = _isOn ? (w - knobD - 1) : 2;
            var knobRect = new Rectangle(knobX, 2, knobD, knobD);
            using (var b = new SolidBrush(_knobColor))
                e.Graphics.FillEllipse(b, knobRect);
            using (var p = new Pen(Color.FromArgb(90, 0, 0, 0), 1f))
                e.Graphics.DrawEllipse(p, knobRect);
        }

        private static GraphicsPath BuildPill(int x, int y, int w, int h)
        {
            var path = new GraphicsPath();
            int d = h;
            path.AddArc(x, y, d, d, 90f, 180f);
            path.AddArc(x + w - d, y, d, d, 270f, 180f);
            path.CloseFigure();
            return path;
        }
    }
}
