using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class PickupSubsetPage : SubsetPageBase
    {
        private bool _loading;
        private bool _currentTargetIsOutput;

        public PickupSubsetPage() : base("recipe.pickupSubset")
        {
            InitializeComponent();
        }

        protected override void BuildEditor(Panel c)
        {
        }

        private void PickupRadio_CheckedChanged(object sender, System.EventArgs e)
        {
            if (previewPanel != null)
                previewPanel.Invalidate();
        }

        private void PickupTarget_CheckedChanged(object sender, System.EventArgs e)
        {
            if (_loading || _project == null)
                return;

            var rb = sender as RadioButton;
            if (rb != null && !rb.Checked)
                return;

            SaveVisibleToPickupSubset(_currentTargetIsOutput ? _project.OutputPickup : _project.InputPickup);
            _currentTargetIsOutput = _rbBin.Checked;
            LoadSelectedTargetFromRecipe();
        }

        protected override void LoadFromRecipe()
        {
            _loading = true;
            EnsurePickupSubsets();
            if (!_rbWafer.Checked && !_rbBin.Checked)
                _rbWafer.Checked = true;
            _currentTargetIsOutput = _rbBin.Checked;
            LoadSelectedTargetFromRecipe();
            _loading = false;
            LogPickupSetting("Load");
        }

        private void LoadSelectedTargetFromRecipe()
        {
            var p = ResolveSelectedPickupSubset();
            _rbTL.Checked = p.StartCorner == PickupStartCorner.TopLeft;
            _rbTR.Checked = p.StartCorner == PickupStartCorner.TopRight;
            _rbBL.Checked = p.StartCorner == PickupStartCorner.BottomLeft;
            _rbBR.Checked = p.StartCorner == PickupStartCorner.BottomRight;
            _rbHoriz.Checked = p.Direction == PickupDirection.Horizontal;
            _rbVert.Checked = p.Direction == PickupDirection.Vertical;
            _rbStraight.Checked = p.Pattern == PickupPattern.Straight;
            _rbZigZag.Checked = p.Pattern == PickupPattern.ZigZag;
            if (previewPanel != null)
                previewPanel.Invalidate();
        }

        // 현재 선택된 라디오(코너/방향/패턴)로부터 n×n 샘플 그리드의 픽업 순서를 계산한다. Point(X=열, Y=행).
        private List<Point> BuildPreviewOrder(int n)
        {
            var order = new List<Point>();
            if (n < 1)
                return order;

            bool startTop = _rbTL.Checked || _rbTR.Checked;
            bool startLeft = _rbTL.Checked || _rbBL.Checked;
            bool horizontal = !_rbVert.Checked;   // 기본 수평
            bool zigzag = _rbZigZag.Checked;

            var rowSeq = new int[n];
            var colSeq = new int[n];
            for (int i = 0; i < n; i++)
            {
                rowSeq[i] = startTop ? i : (n - 1 - i);
                colSeq[i] = startLeft ? i : (n - 1 - i);
            }

            if (horizontal)
            {
                for (int ri = 0; ri < n; ri++)
                    for (int ci = 0; ci < n; ci++)
                    {
                        int c = (zigzag && (ri % 2 == 1)) ? colSeq[n - 1 - ci] : colSeq[ci];
                        order.Add(new Point(c, rowSeq[ri]));
                    }
            }
            else
            {
                for (int ci = 0; ci < n; ci++)
                    for (int ri = 0; ri < n; ri++)
                    {
                        int r = (zigzag && (ci % 2 == 1)) ? rowSeq[n - 1 - ri] : rowSeq[ri];
                        order.Add(new Point(colSeq[ci], r));
                    }
            }
            return order;
        }

        // 픽업 경로 미리보기: 샘플 그리드 위에 실제 순서대로 선을 그리고 시작(S)/끝(E)을 표시한다.
        private void previewPanel_Paint(object sender, PaintEventArgs e)
        {
            const int n = 7;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = previewPanel.ClientSize.Width;
            int h = previewPanel.ClientSize.Height;
            int size = System.Math.Min(w, h);
            if (size < 40)
                return;

            int pad = System.Math.Max(16, size / 12);
            int ox = (w - size) / 2;
            int oy = (h - size) / 2;
            float step = (size - pad * 2) / (float)(n - 1);

            List<Point> order = BuildPreviewOrder(n);
            var pts = new PointF[order.Count];
            for (int i = 0; i < order.Count; i++)
                pts[i] = new PointF(ox + pad + order[i].X * step, oy + pad + order[i].Y * step);

            using (var dot = new SolidBrush(Color.FromArgb(0xC9, 0xCF, 0xD8)))
                for (int r = 0; r < n; r++)
                    for (int c = 0; c < n; c++)
                        g.FillEllipse(dot, ox + pad + c * step - 2.5f, oy + pad + r * step - 2.5f, 5f, 5f);

            if (pts.Length >= 2)
                using (var pen = new Pen(Color.FromArgb(0x37, 0x8A, 0xDD), 2f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLines(pen, pts);

            if (pts.Length > 0)
            {
                DrawEndpoint(g, pts[0], Color.FromArgb(0x1D, 0x9E, 0x75), "S");
                DrawEndpoint(g, pts[pts.Length - 1], Color.FromArgb(0xD8, 0x5A, 0x30), "E");
            }
        }

        private static void DrawEndpoint(Graphics g, PointF p, Color color, string text)
        {
            const float rad = 9f;
            using (var b = new SolidBrush(color))
                g.FillEllipse(b, p.X - rad, p.Y - rad, rad * 2, rad * 2);
            using (var f = new Font("맑은 고딕", 8f, FontStyle.Bold))
            using (var tb = new SolidBrush(Color.White))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(text, f, tb, p, sf);
        }

        protected override void SaveToRecipe()
        {
            EnsurePickupSubsets();
            if (_project == null)
                return;

            PickupSubset pickup = ResolveSelectedPickupSubset();
            SaveVisibleToPickupSubset(pickup);

            _project.Pickup = ClonePickupSubset(_project.InputPickup);

            LogPickupSetting("Save");
        }

        private void SaveVisibleToPickupSubset(PickupSubset p)
        {
            if (p == null)
                return;

            if (_rbTL.Checked) p.StartCorner = PickupStartCorner.TopLeft;
            else if (_rbTR.Checked) p.StartCorner = PickupStartCorner.TopRight;
            else if (_rbBL.Checked) p.StartCorner = PickupStartCorner.BottomLeft;
            else if (_rbBR.Checked) p.StartCorner = PickupStartCorner.BottomRight;
            p.Direction = _rbVert.Checked ? PickupDirection.Vertical : PickupDirection.Horizontal;
            p.Pattern = _rbZigZag.Checked ? PickupPattern.ZigZag : PickupPattern.Straight;
        }

        private void EnsurePickupSubsets()
        {
            if (_project == null)
                return;

            if (_project.Pickup == null)
                _project.Pickup = new PickupSubset();
            if (_project.InputPickup == null)
                _project.InputPickup = ClonePickupSubset(_project.Pickup);
            if (_project.OutputPickup == null)
                _project.OutputPickup = ClonePickupSubset(_project.Pickup);
        }

        private PickupSubset ResolveSelectedPickupSubset()
        {
            EnsurePickupSubsets();
            if (_project == null)
                return new PickupSubset();

            return _rbBin.Checked ? _project.OutputPickup : _project.InputPickup;
        }

        private static PickupSubset ClonePickupSubset(PickupSubset source)
        {
            if (source == null)
                return new PickupSubset();

            return new PickupSubset
            {
                StartCorner = source.StartCorner,
                Direction = source.Direction,
                Pattern = source.Pattern
            };
        }

        private void LogPickupSetting(string action)
        {
            try
            {
                if (_project == null)
                    return;

                PickupSubset input = _project.InputPickup;
                PickupSubset output = _project.OutputPickup;
                string inputText = FormatPickupSubset(input);
                string outputText = FormatPickupSubset(output);
                QMC.Common.Log.Write("Main", "RECIPE", "PickupSubset",
                    "Pickup subset " + action +
                    ". project=" + _project.FileName +
                    ", input=" + inputText +
                    ", output=" + outputText + " - Ok");
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static string FormatPickupSubset(PickupSubset pickup)
        {
            if (pickup == null)
                return "-";

            return pickup.StartCorner + "/" + pickup.Direction + "/" + pickup.Pattern;
        }
    }

    /// <summary>픽업 경로 미리보기 전용 패널. 커스텀 드로잉이라 더블버퍼링·리사이즈 재그리기를 켠다.</summary>
    internal sealed class PickupPreviewPanel : Panel
    {
        public PickupPreviewPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
    }

    /// <summary>부드러운 라운드 칩 토글(라디오). 선택 시 연한 하늘색 채움 + 파란 글씨/테두리로 자체 그린다.</summary>
    internal sealed class ChipToggle : RadioButton
    {
        private static readonly Color FillOn = Color.FromArgb(0xE6, 0xF1, 0xFB);
        private static readonly Color BorderOn = Color.FromArgb(0x2E, 0x86, 0xDE);
        private static readonly Color TextOn = Color.FromArgb(0x18, 0x5F, 0xA5);
        private static readonly Color FillOff = Color.White;
        private static readonly Color FillHover = Color.FromArgb(0xF4, 0xF7, 0xFB);
        private static readonly Color BorderOff = Color.FromArgb(0xC9, 0xCF, 0xD8);
        private static readonly Color TextOff = Color.FromArgb(0x55, 0x5E, 0x6A);
        private bool _hover;

        public ChipToggle()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            Appearance = Appearance.Button;
            AutoCheck = true;
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
        }

        protected override void OnCheckedChanged(System.EventArgs e) { base.OnCheckedChanged(e); Invalidate(); }
        protected override void OnMouseEnter(System.EventArgs e) { _hover = true; base.OnMouseEnter(e); Invalidate(); }
        protected override void OnMouseLeave(System.EventArgs e) { _hover = false; base.OnMouseLeave(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle r = ClientRectangle;
            r.Width -= 1;
            r.Height -= 1;
            const int d = 12;

            using (GraphicsPath path = RoundedRect(r, d))
            {
                Color fill = Checked ? FillOn : (_hover ? FillHover : FillOff);
                Color border = Checked ? BorderOn : BorderOff;
                Color fg = Checked ? TextOn : TextOff;

                using (var b = new SolidBrush(fill))
                    g.FillPath(b, path);
                using (var p = new Pen(border, Checked ? 1.6f : 1f))
                    g.DrawPath(p, path);

                TextRenderer.DrawText(g, Text, Font, r, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle r, int d)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
