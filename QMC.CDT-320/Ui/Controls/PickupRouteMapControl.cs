using System;
using QMC.CDT_320.Ui.Localization;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT_320.Ui.Common.WaferMaps;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>순서 표시 전용 맵. 클릭/재생/더블클릭에서 장비 동작을 요청하지 않는다.</summary>
    public sealed partial class PickupRouteMapControl : Control, ILocalizedView
    {
        private readonly Dictionary<string, int> _ranks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private PickupOrderDraft _draft;
        private int _selectedIndex;
        private int _visibleCount = 20;
        private float _zoom = 3.2F;
        private float _centerX;
        private float _centerY;
        private bool _dragging;
        private bool _dragMoved;
        private Point _dragStart;
        private PointF _dragCenter;
        private RectangleF _overviewRect;
        private static readonly Color RouteColor = Color.FromArgb(38, 113, 185);
        private static readonly Color RouteFill = Color.FromArgb(223, 238, 251);

        public event EventHandler SelectedSequenceChanged;
        public event EventHandler ViewChanged;
        public int SelectedIndex { get { return _selectedIndex; } }
        public float Zoom { get { return _zoom; } }
        public int VisibleCount
        {
            get { return _visibleCount; }
            set { _visibleCount = Math.Max(0, value); FocusSelection(); Invalidate(); }
        }

        public PickupRouteMapControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable, true);
        }

        public void ApplyLanguage()
        {
            Invalidate();
        }

        public void SetDraft(PickupOrderDraft draft)
        {
            _draft = draft;
            _ranks.Clear();
            if (draft != null)
                for (int i = 0; i < draft.Order.Count; i++)
                    _ranks[draft.Order[i].DieUid] = i;
            SelectSequence(0, true);
        }

        public void SelectSequence(int index, bool follow)
        {
            _selectedIndex = _draft == null || _draft.Order.Count == 0 ? 0 : Math.Max(0, Math.Min(index, _draft.Order.Count - 1));
            if (follow && _zoom > 1.01F)
                FocusSelection();
            Invalidate();
        }

        public void FitMap()
        {
            _zoom = 1F;
            _centerX = _centerY = 0F;
            NotifyViewChanged();
        }

        public void ZoomSelection()
        {
            _zoom = 3.2F;
            FocusSelection();
            NotifyViewChanged();
        }

        public void ZoomBy(float factor)
        {
            _zoom = Math.Max(1F, Math.Min(12F, _zoom * factor));
            FocusSelection();
            NotifyViewChanged();
        }

        public void GetVisibleRange(out int first, out int end)
        {
            int count = _draft != null ? _draft.Order.Count : 0;
            first = _visibleCount == 0 ? 0 : Math.Max(0, Math.Min(_selectedIndex - 3, count - _visibleCount));
            end = _visibleCount == 0 ? count : Math.Min(count, first + _visibleCount);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            if (_draft == null || _draft.Map.Entries.Count == 0)
            {
                TextRenderer.DrawText(g, Lang.T("diagram.route.empty"), Font, ClientRectangle, Color.DimGray,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = GetScale();
            float pitchX = ResolvePitch(_draft.Map.PitchX), pitchY = ResolvePitch(_draft.Map.PitchY);
            float dieX = ResolveSize(_draft.Map.DieSizeX, pitchX) * scale;
            float dieY = ResolveSize(_draft.Map.DieSizeY, pitchY) * scale;
            int first, end;
            GetVisibleRange(out first, out end);
            using (var outline = new Pen(Color.FromArgb(171, 187, 204)))
            {
                float diameter = GetDiameter() * scale;
                PointF center = ToScreen(PointF.Empty, scale);
                g.DrawEllipse(outline, center.X - diameter / 2, center.Y - diameter / 2, diameter, diameter);
            }
            using (var border = new Pen(Color.FromArgb(164, 183, 201), .65F))
            {
                foreach (DieMapEntry die in _draft.Map.Entries)
                {
                    PointF p = ToScreen(ToWorld(die), scale);
                    RectangleF rect = new RectangleF(p.X - dieX / 2, p.Y - dieY / 2, dieX, dieY);
                    if (!rect.IntersectsWith(ClientRectangle))
                        continue;
                    int rank;
                    bool ordered = _ranks.TryGetValue(die.DieUid, out rank);
                    Color fill = GetDieColor(die);
                    if (ordered && rank >= first && rank < end) fill = RouteFill;
                    if (ordered && rank == _selectedIndex) fill = RouteColor;
                    if (ordered && rank == 0) fill = WaferMapPalette.StartMarker;
                    using (var brush = new SolidBrush(fill)) g.FillRectangle(brush, rect);
                    if (dieX > 2 && dieY > 2) g.DrawRectangle(border, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
            using (var cap = new AdjustableArrowCap(dieY < 19 ? 1.3F : 3F, dieY < 19 ? 2F : 4F))
            using (var pathPen = new Pen(RouteColor, dieY < 19 ? .8F : 1.8F))
            using (var wrapPen = new Pen(Color.FromArgb(164, 95, 28), 1.8F))
            {
                pathPen.CustomEndCap = cap;
                wrapPen.CustomEndCap = cap;
                wrapPen.DashStyle = DashStyle.Dash;
                for (int i = first; i + 1 < end; i++)
                {
                    PointF a = ToScreen(ToWorld(_draft.Order[i]), scale), b = ToScreen(ToWorld(_draft.Order[i + 1]), scale);
                    float dx = b.X - a.X, dy = b.Y - a.Y, distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (distance < 1) continue;
                    float gap = Math.Min(distance * .3F, 9F);
                    a.X += dx / distance * gap; a.Y += dy / distance * gap;
                    b.X -= dx / distance * gap; b.Y -= dy / distance * gap;
                    g.DrawLine(i == _draft.WrapAfterIndex ? wrapPen : pathPen, a, b);
                }
            }
            using (var font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                for (int i = first; i < end; i++)
                {
                    PointF p = ToScreen(ToWorld(_draft.Order[i]), scale);
                    string text = (i + 1).ToString();
                    SizeF size = g.MeasureString(text, font);
                    if (dieY < 19 || size.Width + 2 > dieX || p.X < 0 || p.Y < 0 || p.X > Width || p.Y > Height) continue;
                    Color fill = i == 0 ? WaferMapPalette.StartMarker : i == _selectedIndex ? RouteColor : RouteFill;
                    using (var brush = new SolidBrush(fill)) g.FillRectangle(brush, p.X - size.Width / 2, p.Y - 8, size.Width, 16);
                    using (var brush = new SolidBrush(i == _selectedIndex && i != 0 ? Color.White : Color.FromArgb(27, 48, 69)))
                        g.DrawString(text, font, brush, new RectangleF(p.X - dieX / 2, p.Y - dieY / 2, dieX, dieY), format);
                }
            }
            if (_draft.Order.Count > 0)
            {
                PointF selected = ToScreen(ToWorld(_draft.Order[_selectedIndex]), scale);
                using (var pen = new Pen(RouteColor, 2F))
                    g.DrawRectangle(pen, selected.X - dieX / 2 - 3, selected.Y - dieY / 2 - 3, dieX + 6, dieY + 6);
            }
            DrawOverview(g, first, end);
        }

        private void DrawOverview(Graphics g, int first, int end)
        {
            float size = Math.Min(130F, Math.Min(Width, Height) * .3F);
            if (size < 65) return;
            bool right = _draft.Order.Count > 0 && ToWorld(_draft.Order[_selectedIndex]).X < 0;
            _overviewRect = new RectangleF(right ? Width - size - 10 : 10, Height - size - 30, size, size + 20);
            using (var brush = new SolidBrush(Color.White)) g.FillRectangle(brush, _overviewRect);
            using (var pen = new Pen(Color.FromArgb(170, 187, 204))) g.DrawRectangle(pen, _overviewRect.X, _overviewRect.Y, _overviewRect.Width, _overviewRect.Height);
            g.DrawString(Lang.T("diagram.route.overview"), Font, Brushes.DimGray, _overviewRect.X + 4, _overviewRect.Y + 2);
            RectangleF inner = new RectangleF(_overviewRect.X + 4, _overviewRect.Y + 22, size - 8, size - 8);
            float miniScale = Math.Min(inner.Width / GetSpanX(), inner.Height / GetSpanY());
            PointF center = new PointF(inner.Left + inner.Width / 2, inner.Top + inner.Height / 2);
            foreach (DieMapEntry die in _draft.Map.Entries)
            {
                PointF p = ToWorld(die);
                int rank;
                bool ordered = _ranks.TryGetValue(die.DieUid, out rank);
                Color fill = ordered && rank == 0 ? WaferMapPalette.StartMarker :
                    ordered && rank >= first && rank < end ? RouteColor : GetDieColor(die);
                using (var brush = new SolidBrush(fill))
                    g.FillRectangle(brush, center.X + (p.X - ResolvePitch(_draft.Map.PitchX) / 2) * miniScale,
                        center.Y + (p.Y - ResolvePitch(_draft.Map.PitchY) / 2) * miniScale,
                        Math.Max(1, ResolvePitch(_draft.Map.PitchX) * miniScale - .3F),
                        Math.Max(1, ResolvePitch(_draft.Map.PitchY) * miniScale - .3F));
            }
            float scale = GetScale();
            using (var pen = new Pen(RouteColor, 1.3F))
            {
                GraphicsState saved = g.Save();
                g.SetClip(inner);
                g.DrawRectangle(pen, center.X + (_centerX - Width / scale / 2) * miniScale,
                    center.Y + (_centerY - Height / scale / 2) * miniScale,
                    Width / scale * miniScale, Height / scale * miniScale);
                g.Restore(saved);
            }
        }

        private static Color GetDieColor(DieMapEntry die)
        {
            return die.Result == DieResult.Good ? WaferMapPalette.Good :
                die.Result == DieResult.NG ? WaferMapPalette.NgFallback :
                !die.IsTarget ? WaferMapPalette.Skip : WaferMapPalette.Wait;
        }

        private PointF ToWorld(DieMapEntry die)
        {
            return new PointF((die.DieMapX - (_draft.Map.DieMapX - 1) / 2F) * ResolvePitch(_draft.Map.PitchX),
                (die.DieMapY - (_draft.Map.DieMapY - 1) / 2F) * ResolvePitch(_draft.Map.PitchY));
        }

        private PointF ToScreen(PointF world, float scale)
        {
            return new PointF(Width / 2F + (world.X - _centerX) * scale, Height / 2F + (world.Y - _centerY) * scale);
        }

        private float GetScale() { return Math.Max(.0001F, Math.Min(Math.Max(1, Width - 24) / GetSpanX(), Math.Max(1, Height - 24) / GetSpanY()) * _zoom); }
        private float GetSpanX() { return _draft == null ? 1 : Math.Max(_draft.Map.DieMapX * ResolvePitch(_draft.Map.PitchX), GetDiameter()) + 12; }
        private float GetSpanY() { return _draft == null ? 1 : Math.Max(_draft.Map.DieMapY * ResolvePitch(_draft.Map.PitchY), GetDiameter()) + 12; }
        private float GetDiameter() { return _draft == null ? 1 : ResolveSize(_draft.Map.OuterDiameterMm, Math.Max(_draft.Map.DieMapX * ResolvePitch(_draft.Map.PitchX), _draft.Map.DieMapY * ResolvePitch(_draft.Map.PitchY))); }
        private static float ResolvePitch(double value) { return IsPositive(value) ? (float)value : 1F; }
        private static float ResolveSize(double value, float fallback) { return IsPositive(value) ? (float)value : fallback * .94F; }
        private static bool IsPositive(double value) { return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0 && value < 1000000; }

        private void FocusSelection()
        {
            if (_draft == null || _draft.Order.Count == 0) return;
            int count = Math.Min(20, _draft.Order.Count), first = Math.Max(0, Math.Min(_selectedIndex - 3, _draft.Order.Count - count));
            List<PointF> points = _draft.Order.Skip(first).Take(count).Select(ToWorld).ToList();
            _centerX = (points.Min(p => p.X) + points.Max(p => p.X)) / 2;
            _centerY = (points.Min(p => p.Y) + points.Max(p => p.Y)) / 2;
            // 직선 경로의 행 전환/앞부분 연결에서는 주변 경로가 길어도 선택 Die를 화면 안에 둔다.
            PointF selected = ToWorld(_draft.Order[_selectedIndex]);
            float scale = GetScale();
            float halfX = Math.Max(0, Width / scale / 2 - ResolvePitch(_draft.Map.PitchX));
            float halfY = Math.Max(0, Height / scale / 2 - ResolvePitch(_draft.Map.PitchY));
            _centerX = Math.Max(selected.X - halfX, Math.Min(selected.X + halfX, _centerX));
            _centerY = Math.Max(selected.Y - halfY, Math.Min(selected.Y + halfY, _centerY));
            ClampCenter();
        }

        private void ClampCenter()
        {
            float scale = GetScale(), x = Math.Max(0, GetSpanX() / 2 - Width / scale / 2), y = Math.Max(0, GetSpanY() / 2 - Height / scale / 2);
            _centerX = Math.Max(-x, Math.Min(x, _centerX));
            _centerY = Math.Max(-y, Math.Min(y, _centerY));
        }

        private void NotifyViewChanged()
        {
            Invalidate();
            if (ViewChanged != null) ViewChanged(this, EventArgs.Empty);
        }

        private void SelectAtPoint(Point point)
        {
            if (_draft == null || _draft.Order.Count == 0) return;
            float scale = GetScale();
            PointF world = new PointF(_centerX + (point.X - Width / 2F) / scale, _centerY + (point.Y - Height / 2F) / scale);
            bool overview = _overviewRect.Contains(point);
            if (overview)
            {
                RectangleF inner = new RectangleF(_overviewRect.X + 4, _overviewRect.Y + 22, _overviewRect.Width - 8, _overviewRect.Width - 8);
                float miniScale = Math.Min(inner.Width / GetSpanX(), inner.Height / GetSpanY());
                world = new PointF((point.X - inner.Left - inner.Width / 2) / miniScale, (point.Y - inner.Top - inner.Height / 2) / miniScale);
            }
            DieMapEntry hit = _draft.Order.OrderBy(d => DistanceSquared(ToWorld(d), world)).First();
            PointF p = ToWorld(hit);
            if (!overview && (Math.Abs(p.X - world.X) > ResolvePitch(_draft.Map.PitchX) / 2 || Math.Abs(p.Y - world.Y) > ResolvePitch(_draft.Map.PitchY) / 2)) return;
            if (overview) _zoom = Math.Max(3.2F, _zoom);
            SelectSequence(_ranks[hit.DieUid], overview);
            if (SelectedSequenceChanged != null) SelectedSequenceChanged(this, EventArgs.Empty);
            NotifyViewChanged();
        }

        private static float DistanceSquared(PointF a, PointF b) { float x = a.X - b.X, y = a.Y - b.Y; return x * x + y * y; }
        private void PickupRouteMapControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            Focus(); _dragging = true; _dragMoved = false; _dragStart = e.Location;
            _dragCenter = new PointF(_centerX, _centerY); Capture = true;
        }
        private void PickupRouteMapControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging || _overviewRect.Contains(_dragStart)) return;
            int dx = e.X - _dragStart.X, dy = e.Y - _dragStart.Y;
            if (Math.Abs(dx) + Math.Abs(dy) < 5 && !_dragMoved) return;
            _dragMoved = true; _centerX = _dragCenter.X - dx / GetScale(); _centerY = _dragCenter.Y - dy / GetScale();
            ClampCenter(); Invalidate();
        }
        private void PickupRouteMapControl_MouseUp(object sender, MouseEventArgs e)
        {
            bool select = _dragging && !_dragMoved && e.Button == MouseButtons.Left;
            _dragging = false; Capture = false;
            if (select) SelectAtPoint(e.Location);
        }
        private void PickupRouteMapControl_MouseCaptureChanged(object sender, EventArgs e) { if (!Capture) _dragging = false; }
        private void PickupRouteMapControl_MouseWheel(object sender, MouseEventArgs e) { ZoomBy(e.Delta > 0 ? 1.2F : 1 / 1.2F); }
        private void PickupRouteMapControl_Resize(object sender, EventArgs e) { if (_zoom > 1.01F) FocusSelection(); Invalidate(); }
    }
}
