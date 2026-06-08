using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using QMC.Vision.Core;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 카메라 출력 표시 컨트롤 + 오버레이(ROI, Match 결과, 크로스헤어, STAGE 정보).
    /// </summary>
    public partial class CameraView : Control
    {
        private Bitmap _frame;
        private Roi _overlayRoi;
        private MatchResult _overlayResult;

        // ── ROI 드래그 편집 상태 ──
        private bool   _editing;
        private string _editKind;          // "Search" | "Train"
        private Roi    _editRoi;
        private Point  _dragStart;
        private Point  _dragNow;

        /// <summary>ROI 드래그 종료 시 호출됨. (kind, newRoi).</summary>
        public event Action<string, Roi> RoiEdited;

        public bool ShowCrosshair { get; set; } = true;
        public bool ShowLiveLabel { get; set; } = true;
        public string InfoText    { get; set; } = "STAGE\r\nW:640 H:480";

        public CameraView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            InitializeComponent();   // Stage 90 — BackColor + 이벤트 named 핸들러 연결(.Designer.cs)
        }

        // Stage 90 — 기존 DoubleClick 람다 추출(인스턴스 필드만 참조 → closure 아님). 동작 동일.
        private void OnViewDoubleClick(object s, EventArgs e)
        {
            // ROI 편집 중이 아닐 때만 줌 다이얼로그
            if (_editing) return;
            if (_frame == null) return;
            using (var d = new QMC.Vision.Ui.Dialogs.ZoomDialog((Bitmap)_frame.Clone(), "CameraView Zoom"))
                d.ShowDialog(this);
        }

        // ── ROI 드래그 편집 진입 ──
        public void BeginRoiDrag(string kind, Roi current)
        {
            _editing  = true;
            _editKind = kind;
            _editRoi  = current?.Clone();
            Cursor    = Cursors.Cross;
            Invalidate();
        }

        private void OnMouseDownEditing(object s, MouseEventArgs e)
        {
            if (!_editing || e.Button != MouseButtons.Left || _frame == null) return;
            _dragStart = e.Location;
            _dragNow   = e.Location;
        }
        private void OnMouseMoveEditing(object s, MouseEventArgs e)
        {
            if (!_editing || e.Button != MouseButtons.Left || _frame == null) return;
            _dragNow = e.Location;
            Invalidate();
        }
        private void OnMouseUpEditing(object s, MouseEventArgs e)
        {
            if (!_editing || _frame == null) { return; }
            // 화면→이미지 좌표 변환
            var dst = FitRect(_frame.Size, ClientSize);
            int x1 = Math.Min(_dragStart.X, _dragNow.X);
            int y1 = Math.Min(_dragStart.Y, _dragNow.Y);
            int x2 = Math.Max(_dragStart.X, _dragNow.X);
            int y2 = Math.Max(_dragStart.Y, _dragNow.Y);
            if (x2 - x1 < 4 || y2 - y1 < 4) { _editing = false; Cursor = Cursors.Default; Invalidate(); return; }

            double sx = (double)_frame.Width  / dst.Width;
            double sy = (double)_frame.Height / dst.Height;
            int ix1 = (int)((x1 - dst.Left) * sx);
            int iy1 = (int)((y1 - dst.Top)  * sy);
            int ix2 = (int)((x2 - dst.Left) * sx);
            int iy2 = (int)((y2 - dst.Top)  * sy);
            // 이미지 영역 클램프
            ix1 = Math.Max(0, Math.Min(_frame.Width  - 1, ix1));
            ix2 = Math.Max(0, Math.Min(_frame.Width  - 1, ix2));
            iy1 = Math.Max(0, Math.Min(_frame.Height - 1, iy1));
            iy2 = Math.Max(0, Math.Min(_frame.Height - 1, iy2));

            var newRoi = new Roi
            {
                Name     = (_editRoi?.Name) ?? _editKind,
                CenterX  = (ix1 + ix2) / 2.0,
                CenterY  = (iy1 + iy2) / 2.0,
                Width    = ix2 - ix1,
                Height   = iy2 - iy1,
                AngleDeg = _editRoi?.AngleDeg ?? 0
            };
            _editing = false;
            Cursor   = Cursors.Default;
            Invalidate();
            try { RoiEdited?.Invoke(_editKind, newRoi); } catch { }
        }

        public void SetFrame(GrabResult r)
        {
            if (r == null) return;
            _frame?.Dispose();
            _frame = r.Image != null ? (Bitmap)r.Image.Clone() : null;
            if (IsHandleCreated) BeginInvoke(new Action(Invalidate));
        }

        public void SetOverlay(Roi roi, MatchResult result)
        {
            _overlayRoi    = roi;
            _overlayResult = result;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            if (_frame != null)
            {
                var dst = FitRect(_frame.Size, ClientSize);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_frame, dst);
                DrawOverlays(g, dst);
            }
            else
            {
                using (var br = new SolidBrush(UiTheme.VisionInfoFg))
                    g.DrawString("No image", new Font("Consolas", 12F), br, 10, 10);
            }
            // 좌상단 STAGE 정보
            if (!string.IsNullOrEmpty(InfoText))
                using (var br = new SolidBrush(UiTheme.VisionInfoFg))
                using (var f  = new Font("Consolas", 9F))
                    g.DrawString(InfoText, f, br, 8, 6);

            // Live 라벨
            if (ShowLiveLabel && _frame != null)
                using (var br = new SolidBrush(UiTheme.VisionInfoFg))
                using (var f  = new Font("Consolas", 9F))
                    g.DrawString("Live", f, br, 8, Height - 22);

            // ROI 편집 중 — 드래그 사각형 미리보기
            if (_editing && _frame != null)
            {
                int x1 = Math.Min(_dragStart.X, _dragNow.X);
                int y1 = Math.Min(_dragStart.Y, _dragNow.Y);
                int w  = Math.Abs(_dragNow.X - _dragStart.X);
                int h  = Math.Abs(_dragNow.Y - _dragStart.Y);
                if (w > 0 && h > 0)
                {
                    var color = _editKind == "Search" ? Color.OrangeRed : Color.DeepSkyBlue;
                    using (var pen = new Pen(color, 2f) { DashStyle = DashStyle.DashDotDot })
                        g.DrawRectangle(pen, x1, y1, w, h);
                    using (var br = new SolidBrush(color))
                    using (var f  = new Font("Consolas", 9F, FontStyle.Bold))
                        g.DrawString($"{_editKind} ROI  {w}x{h}", f, br, x1, Math.Max(0, y1 - 16));
                }
                // 안내 텍스트
                using (var br = new SolidBrush(Color.Yellow))
                using (var f  = new Font("Segoe UI", 11F, FontStyle.Bold))
                    g.DrawString($"-- Editing {_editKind} ROI: drag a rectangle --", f, br, 10, Height - 26);
            }

            // 외곽선
            using (var p = new Pen(Color.DimGray, 1f))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }

        private void DrawOverlays(Graphics g, Rectangle dst)
        {
            if (ShowCrosshair)
                using (var p = new Pen(Color.FromArgb(120, 255, 255, 255), 1f) { DashStyle = DashStyle.Dash })
                {
                    g.DrawLine(p, dst.Left,  dst.Top + dst.Height / 2, dst.Right, dst.Top + dst.Height / 2);
                    g.DrawLine(p, dst.Left + dst.Width / 2, dst.Top, dst.Left + dst.Width / 2, dst.Bottom);
                }

            if (_overlayRoi != null && _frame != null)
            {
                var sx = (double)dst.Width  / _frame.Width;
                var sy = (double)dst.Height / _frame.Height;
                var r  = _overlayRoi.BoundingBox;
                var rr = new Rectangle(
                    dst.Left + (int)(r.X * sx), dst.Top + (int)(r.Y * sy),
                    (int)(r.Width * sx),        (int)(r.Height * sy));
                using (var p = new Pen(Color.Yellow, 1f)) g.DrawRectangle(p, rr);
            }

            if (_overlayResult?.Instances != null && _frame != null)
            {
                var sx = (double)dst.Width  / _frame.Width;
                var sy = (double)dst.Height / _frame.Height;
                using (var p = new Pen(Color.LimeGreen, 2f))
                using (var br = new SolidBrush(Color.LimeGreen))
                using (var f  = new Font("Consolas", 9F))
                {
                    foreach (var m in _overlayResult.Instances)
                    {
                        int cx = dst.Left + (int)(m.CenterX * sx);
                        int cy = dst.Top  + (int)(m.CenterY * sy);
                        g.DrawEllipse(p, cx - 8, cy - 8, 16, 16);
                        g.DrawLine   (p, cx - 10, cy, cx + 10, cy);
                        g.DrawLine   (p, cx, cy - 10, cx, cy + 10);
                        g.DrawString($"{m.Score:F2}", f, br, cx + 10, cy - 16);
                    }
                }
            }
        }

        private static Rectangle FitRect(Size src, Size dst)
        {
            double rx = (double)dst.Width  / src.Width;
            double ry = (double)dst.Height / src.Height;
            double r  = Math.Min(rx, ry);
            int w = (int)(src.Width  * r);
            int h = (int)(src.Height * r);
            return new Rectangle((dst.Width - w) / 2, (dst.Height - h) / 2, w, h);
        }
    }
}
