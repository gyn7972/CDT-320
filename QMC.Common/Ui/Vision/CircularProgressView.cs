using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QMC.Common.Ui.Controls
{
    /// <summary>진행 상태(공용). 시퀀스/초기화/작업 등 어디서나 사용한다.</summary>
    public enum ProgressState
    {
        Idle,
        Running,
        Completed,
        Failed,
        Canceled
    }

    /// <summary>
    /// 공용 원형 Progress 컨트롤(GDI+ 벡터, 외부 이미지 의존성 없음).
    /// <list type="bullet">
    ///   <item><description>진행 중: 결정형 진행 호 + 회전 스피너.</description></item>
    ///   <item><description>완료/실패: 중앙 체크(✓)/엑스(✗) 아이콘.</description></item>
    ///   <item><description>단계 점(dot): TotalSteps 만큼 표시하고 완료 단계를 채운다.</description></item>
    /// </list>
    /// </summary>
    public sealed class CircularProgressView : Control
    {
        private static readonly Color DefaultTrack = Color.FromArgb(228, 231, 236);
        private static readonly Color DefaultDotEmpty = Color.FromArgb(214, 218, 224);
        private static readonly Color DefaultText = Color.FromArgb(40, 44, 52);
        private static readonly Color DefaultArc = Color.FromArgb(33, 118, 210);

        private int _percent;
        private Color _arcColor = DefaultArc;
        private ProgressState _state = ProgressState.Idle;
        private int _completedSteps;
        private int _totalSteps;

        private readonly Timer _spinTimer;
        private float _spinAngle;

        public CircularProgressView()
        {
            DoubleBuffered = true;
            // 일반 Control 은 SupportsTransparentBackColor 없이는 투명 배경에서 예외가 난다.
            // 기본은 불투명 배경색을 사용하고, 호스트가 자신의 폼 배경색으로 맞추면 된다.
            BackColor = Color.FromArgb(248, 249, 251);
            ForeColor = DefaultText;

            _spinTimer = new Timer { Interval = 33 };
            _spinTimer.Tick += OnSpinTick;
        }

        /// <summary>진행 호가 그려지지 않는 바탕 링 색.</summary>
        public Color TrackColor { get; set; } = DefaultTrack;

        /// <summary>아직 완료되지 않은 단계 점 색.</summary>
        public Color DotEmptyColor { get; set; } = DefaultDotEmpty;

        /// <summary>진행 호/스피너/완료·실패 아이콘 색.</summary>
        public Color ArcColor
        {
            get { return _arcColor; }
            set
            {
                if (_arcColor == value)
                    return;
                _arcColor = value;
                Invalidate();
            }
        }

        /// <summary>상태/진행률/단계 수를 한 번에 설정한다. (진행 중일 때만 스피너 동작)</summary>
        public void SetState(ProgressState state, int percent, Color accent, int completedSteps, int totalSteps)
        {
            _state = state;
            _percent = Math.Max(0, Math.Min(100, percent));
            _arcColor = accent;
            _completedSteps = Math.Max(0, completedSteps);
            _totalSteps = Math.Max(0, totalSteps);

            bool shouldSpin = state == ProgressState.Running;
            if (shouldSpin && !_spinTimer.Enabled)
                _spinTimer.Start();
            else if (!shouldSpin && _spinTimer.Enabled)
                _spinTimer.Stop();

            Invalidate();
        }

        private void OnSpinTick(object sender, EventArgs e)
        {
            _spinAngle = (_spinAngle + 9f) % 360f;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int dotsArea = 26;
            int size = Math.Min(Width, Height - dotsArea) - 32;
            if (size < 40)
                return;

            Rectangle rect = new Rectangle(
                (Width - size) / 2,
                (Height - dotsArea - size) / 2 + 2,
                size,
                size);

            float thickness = Math.Max(10f, size * 0.085f);

            using (var basePen = new Pen(TrackColor, thickness))
            using (var progressPen = new Pen(_arcColor, thickness))
            {
                basePen.StartCap = LineCap.Round;
                basePen.EndCap = LineCap.Round;
                progressPen.StartCap = LineCap.Round;
                progressPen.EndCap = LineCap.Round;

                g.DrawArc(basePen, rect, -90, 360);

                if (_percent > 0)
                    g.DrawArc(progressPen, rect, -90, (float)(360.0 * _percent / 100.0));

                if (_state == ProgressState.Running)
                {
                    using (var spinPen = new Pen(Color.FromArgb(150, _arcColor), thickness))
                    {
                        spinPen.StartCap = LineCap.Round;
                        spinPen.EndCap = LineCap.Round;
                        g.DrawArc(spinPen, rect, _spinAngle, 42);
                    }
                }
            }

            DrawCenterContent(g, rect);
            DrawStepDots(g, dotsArea);
        }

        private void DrawCenterContent(Graphics g, Rectangle rect)
        {
            if (_state == ProgressState.Completed)
            {
                DrawCheck(g, rect, _arcColor);
                return;
            }
            if (_state == ProgressState.Failed)
            {
                DrawCross(g, rect, _arcColor);
                return;
            }

            string text = _percent + "%";
            using (var textBrush = new SolidBrush(ForeColor))
            using (var textFont = new Font("Malgun Gothic", rect.Height * 0.20f, FontStyle.Bold))
            {
                SizeF textSize = g.MeasureString(text, textFont);
                g.DrawString(text, textFont, textBrush,
                    rect.Left + (rect.Width - textSize.Width) / 2,
                    rect.Top + (rect.Height - textSize.Height) / 2);
            }
        }

        private static void DrawCheck(Graphics g, Rectangle rect, Color color)
        {
            float cx = rect.Left + rect.Width / 2f;
            float cy = rect.Top + rect.Height / 2f;
            float r = rect.Width * 0.22f;

            using (var pen = new Pen(color, Math.Max(4f, rect.Width * 0.045f)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                PointF p1 = new PointF(cx - r, cy + r * 0.05f);
                PointF p2 = new PointF(cx - r * 0.25f, cy + r * 0.7f);
                PointF p3 = new PointF(cx + r, cy - r * 0.6f);
                g.DrawLines(pen, new[] { p1, p2, p3 });
            }
        }

        private static void DrawCross(Graphics g, Rectangle rect, Color color)
        {
            float cx = rect.Left + rect.Width / 2f;
            float cy = rect.Top + rect.Height / 2f;
            float r = rect.Width * 0.2f;

            using (var pen = new Pen(color, Math.Max(4f, rect.Width * 0.045f)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, cx - r, cy - r, cx + r, cy + r);
                g.DrawLine(pen, cx + r, cy - r, cx - r, cy + r);
            }
        }

        private void DrawStepDots(Graphics g, int dotsArea)
        {
            if (_totalSteps <= 0 || _totalSteps > 40)
                return;

            float dot = 9f;
            float gap = 7f;
            float totalW = _totalSteps * dot + (_totalSteps - 1) * gap;
            float x = (Width - totalW) / 2f;
            float y = Height - dotsArea + (dotsArea - dot) / 2f;

            for (int i = 0; i < _totalSteps; i++)
            {
                Color c = i < _completedSteps ? _arcColor : DotEmptyColor;
                using (var br = new SolidBrush(c))
                    g.FillEllipse(br, x, y, dot, dot);
                x += dot + gap;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _spinTimer.Stop();
                    _spinTimer.Dispose();
                }
                catch
                {
                }
            }
            base.Dispose(disposing);
        }
    }
}
