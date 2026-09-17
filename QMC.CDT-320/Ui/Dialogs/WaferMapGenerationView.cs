using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>생성 결과 전용 표시. 원본 mm 좌표를 사용하며 +Y는 화면 위쪽이다.</summary>
    public sealed class WaferMapGenerationView : Control
    {
        private readonly object _renderSync = new object();
        private GeneratedWaferMap _map;
        private GeneratedWaferDie _selectedDie;
        private Bitmap _bitmap;
        private RenderLayout _layout;
        private Dictionary<long, GeneratedWaferDie> _hitMap;
        private CancellationTokenSource _renderCancellation;
        private int _renderVersion;
        private bool _rendering;
        private bool _disposed;
        private double _zoom = 1.0;
        private string _renderError;
        private RenderResult _pendingRender;

        public event Action<GeneratedWaferDie> DieSelected;
        public event EventHandler RenderCompleted;

        public GeneratedWaferMap Map
        {
            get { return _map; }
            set
            {
                _map = value;
                _selectedDie = null;
                _hitMap = null;
                _zoom = 1.0;
                _renderError = null;
                DisposeBitmap();
                Action<GeneratedWaferDie> selected = DieSelected;
                if (selected != null) selected(null);
                RequestRender();
            }
        }

        public GeneratedWaferDie SelectedDie { get { return _selectedDie; } }
        public bool IsRendering { get { return _rendering; } }

        public WaferMapGenerationView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            BackColor = Color.Black;
            ForeColor = Color.Gainsboro;
            TabStop = true;
        }

        public void FitToView()
        {
            _zoom = 1.0;
            RequestRender();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RequestRender();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RequestRender();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_map == null) return;
            _zoom = Math.Max(1.0, Math.Min(20.0, _zoom * (e.Delta > 0 ? 1.2 : 1.0 / 1.2)));
            RequestRender();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button != MouseButtons.Left || _rendering || _map == null ||
                _map.Count == 0 || _layout == null || _hitMap == null) return;

            double x = (e.X - _layout.CenterX) / _layout.Scale;
            double y = (_layout.CenterY - e.Y) / _layout.Scale;
            GeneratedWaferDie anchor = _map.Dies[0];
            int column = anchor.Column + (int)Math.Round((x - (double)anchor.CenterXMm) / (double)_map.DisplayStepXMm);
            int row = anchor.Row + (int)Math.Round((y - (double)anchor.CenterYMm) / (double)_map.DisplayStepYMm);
            GeneratedWaferDie hit;
            if (!_hitMap.TryGetValue(AddressKey(column, row), out hit) ||
                Math.Abs(x - (double)hit.CenterXMm) > (double)_map.DisplayDieSizeXMm / 2.0 ||
                Math.Abs(y - (double)hit.CenterYMm) > (double)_map.DisplayDieSizeYMm / 2.0)
                hit = null;
            _selectedDie = hit;
            Action<GeneratedWaferDie> selected = DieSelected;
            if (selected != null) selected(hit);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(Color.Black);
            if (_bitmap != null)
            {
                e.Graphics.DrawImageUnscaled(_bitmap, Point.Empty);
                if (_selectedDie != null && _layout != null)
                {
                    RectangleF rectangle = DieRectangle(_map, _selectedDie, _layout);
                    using (var pen = new Pen(Color.FromArgb(255, 210, 72), 2F))
                        e.Graphics.DrawRectangle(pen, rectangle.X, rectangle.Y,
                            Math.Max(2F, rectangle.Width), Math.Max(2F, rectangle.Height));
                }
            }
            else
            {
                string message = _renderError ?? (_map == null
                    ? "사양을 입력하고 AUTO WAFER CREATE를 누르세요."
                    : "원형 미리보기를 그리는 중입니다...");
                TextRenderer.DrawText(e.Graphics, message, Font, ClientRectangle, ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            }
            if (_rendering && _bitmap != null)
                TextRenderer.DrawText(e.Graphics, "화면 갱신 중...", Font, new Point(10, 10), Color.White, Color.Black);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_renderSync)
                {
                    _disposed = true;
                    DisposePendingRender();
                }
                _renderVersion++;
                if (_renderCancellation != null)
                {
                    _renderCancellation.Cancel();
                    _renderCancellation.Dispose();
                    _renderCancellation = null;
                }
                DisposeBitmap();
            }
            base.Dispose(disposing);
        }

        private void RequestRender()
        {
            if (_disposed || IsDisposed || Disposing) return;
            int version = ++_renderVersion;
            if (_renderCancellation != null)
            {
                _renderCancellation.Cancel();
                _renderCancellation.Dispose();
                _renderCancellation = null;
            }
            lock (_renderSync) DisposePendingRender();
            if (_map == null)
            {
                _rendering = false;
                Invalidate();
                return;
            }
            _rendering = true;
            if (!IsHandleCreated || ClientSize.Width < 1 || ClientSize.Height < 1) return;

            GeneratedWaferMap map = _map;
            Size size = ClientSize;
            double zoom = _zoom;
            Dictionary<long, GeneratedWaferDie> hitMap = _hitMap;
            var cancellation = new CancellationTokenSource();
            _renderCancellation = cancellation;
            CancellationToken token = cancellation.Token;
            // 수십만 다이도 UI Paint에서 반복하지 않고 별도 비트맵으로 준비한다.
            Task.Run(() => RenderAndPost(map, size, zoom, hitMap, token, version));
            Invalidate();
        }

        private void RenderAndPost(GeneratedWaferMap map, Size size, double zoom,
            Dictionary<long, GeneratedWaferDie> hitMap, CancellationToken token, int version)
        {
            RenderResult result = null;
            try { result = BuildRender(map, size, zoom, hitMap, token); }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                result = new RenderResult { Error = "미리보기를 그리지 못했습니다: " + ex.Message };
                QMC.Common.Log.Write("Main", "UI", "WaferMapGenerationView", "웨이퍼 맵 표시 실패: " + ex);
            }
            lock (_renderSync)
            {
                if (_disposed || token.IsCancellationRequested || !IsHandleCreated)
                {
                    if (result.Bitmap != null) result.Bitmap.Dispose();
                    return;
                }
                // UI 콜백이 실행되기 전에 창이 닫혀도 Dispose에서 비트맵 소유권을 회수한다.
                DisposePendingRender();
                _pendingRender = result;
            }
            try
            {
                BeginInvoke((Action)(() =>
                {
                    lock (_renderSync)
                    {
                        if (!ReferenceEquals(_pendingRender, result)) return;
                        if (_disposed || IsDisposed || Disposing || version != _renderVersion)
                        {
                            DisposePendingRender();
                            return;
                        }
                        _pendingRender = null;
                    }
                    DisposeBitmap();
                    _bitmap = result.Bitmap;
                    _layout = result.Layout;
                    _hitMap = result.HitMap;
                    _renderError = result.Error;
                    _rendering = false;
                    Invalidate();
                    EventHandler completed = RenderCompleted;
                    if (completed != null) completed(this, EventArgs.Empty);
                }));
            }
            catch (InvalidOperationException)
            {
                lock (_renderSync)
                {
                    if (ReferenceEquals(_pendingRender, result)) DisposePendingRender();
                }
            }
        }

        private static RenderResult BuildRender(GeneratedWaferMap map, Size size, double zoom,
            Dictionary<long, GeneratedWaferDie> existingHitMap, CancellationToken token)
        {
            double radius = Math.Max((double)map.BoundaryRadiusMm, (double)map.Settings.OuterDiameterMm / 2.0);
            double halfWidth = radius;
            double halfHeight = radius;
            int boundsIndex = 0;
            foreach (GeneratedWaferDie die in map.Dies)
            {
                if ((boundsIndex++ & 1023) == 0) token.ThrowIfCancellationRequested();
                halfWidth = Math.Max(halfWidth, Math.Abs((double)die.CenterXMm) + (double)map.DisplayDieSizeXMm / 2.0);
                halfHeight = Math.Max(halfHeight, Math.Abs((double)die.CenterYMm) + (double)map.DisplayDieSizeYMm / 2.0);
            }
            var layout = new RenderLayout
            {
                CenterX = size.Width / 2F,
                CenterY = size.Height / 2F,
                Scale = Math.Max(0.000001, Math.Min(Math.Max(1, size.Width - 54) / (2.0 * halfWidth),
                    Math.Max(1, size.Height - 54) / (2.0 * halfHeight))) * zoom
            };
            var hitMap = existingHitMap ?? new Dictionary<long, GeneratedWaferDie>(map.Count);
            Bitmap bitmap = new Bitmap(size.Width, size.Height);
            try
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                using (var dieBrush = new SolidBrush(Color.FromArgb(42, 178, 108)))
                using (var outsideBrush = new SolidBrush(Color.FromArgb(230, 62, 55)))
                using (var gridPen = new Pen(Color.FromArgb(28, 85, 60)))
                using (var outsideGridPen = new Pen(Color.FromArgb(105, 28, 26)))
                using (var outlinePen = new Pen(Color.FromArgb(158, 180, 194), 1F))
                using (var allowedPen = new Pen(Color.FromArgb(245, 196, 66), 1.2F) { DashStyle = DashStyle.Dash })
                using (var axisPen = new Pen(Color.FromArgb(145, 150, 165, 180)))
                using (var axisBrush = new SolidBrush(Color.FromArgb(180, 194, 205)))
                using (var font = new Font("맑은 고딕", 9F))
                {
                    graphics.Clear(Color.Black);
                    graphics.SmoothingMode = SmoothingMode.None;
                    int index = 0;
                    foreach (GeneratedWaferDie die in map.Dies)
                    {
                        if ((index++ & 1023) == 0) token.ThrowIfCancellationRequested();
                        if (existingHitMap == null) hitMap.Add(AddressKey(die.Column, die.Row), die);
                        RectangleF rectangle = DieRectangle(map, die, layout);
                        if (rectangle.Right < 0 || rectangle.Bottom < 0 || rectangle.Left > size.Width || rectangle.Top > size.Height) continue;
                        bool inside = map.IsWithinBoundary(die);
                        graphics.FillRectangle(inside ? dieBrush : outsideBrush, rectangle);
                        if (rectangle.Width >= 4F && rectangle.Height >= 4F)
                            graphics.DrawRectangle(inside ? gridPen : outsideGridPen, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
                    }
                    token.ThrowIfCancellationRequested();
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    float waferSize = (float)((double)map.Settings.OuterDiameterMm * layout.Scale);
                    graphics.DrawEllipse(outlinePen, layout.CenterX - waferSize / 2F,
                        layout.CenterY - waferSize / 2F, waferSize, waferSize);
                    if (map.Settings.GenerationVersion >= 2 && map.BoundaryRadiusMm > 0m)
                    {
                        float allowedSize = (float)((double)map.BoundaryRadiusMm * 2.0 * layout.Scale);
                        graphics.DrawEllipse(allowedPen, layout.CenterX - allowedSize / 2F,
                            layout.CenterY - allowedSize / 2F, allowedSize, allowedSize);
                    }
                    graphics.DrawLine(axisPen, 10F, layout.CenterY, size.Width - 10F, layout.CenterY);
                    graphics.DrawLine(axisPen, layout.CenterX, 10F, layout.CenterX, size.Height - 10F);
                    graphics.DrawString("-X", font, axisBrush, 10F, layout.CenterY + 3F);
                    graphics.DrawString("+X", font, axisBrush, size.Width - 32F, layout.CenterY + 3F);
                    graphics.DrawString("+Y", font, axisBrush, layout.CenterX + 4F, 8F);
                    graphics.DrawString("-Y", font, axisBrush, layout.CenterX + 4F, size.Height - 26F);
                    graphics.DrawString("0,0", font, axisBrush, layout.CenterX + 4F, layout.CenterY + 3F);
                }
                return new RenderResult { Bitmap = bitmap, Layout = layout, HitMap = hitMap };
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }

        private static RectangleF DieRectangle(GeneratedWaferMap map, GeneratedWaferDie die, RenderLayout layout)
        {
            float width = (float)((double)map.DisplayDieSizeXMm * layout.Scale);
            float height = (float)((double)map.DisplayDieSizeYMm * layout.Scale);
            float centerX = layout.CenterX + (float)((double)die.CenterXMm * layout.Scale);
            float centerY = layout.CenterY - (float)((double)die.CenterYMm * layout.Scale);
            return new RectangleF(centerX - width / 2F, centerY - height / 2F, width, height);
        }

        private static long AddressKey(int column, int row)
        {
            return ((long)row << 32) | (uint)column;
        }

        private void DisposeBitmap()
        {
            Bitmap previous = _bitmap;
            _bitmap = null;
            _layout = null;
            if (previous != null) previous.Dispose();
        }

        private void DisposePendingRender()
        {
            RenderResult pending = _pendingRender;
            _pendingRender = null;
            if (pending != null && pending.Bitmap != null) pending.Bitmap.Dispose();
        }

        private sealed class RenderLayout
        {
            public float CenterX;
            public float CenterY;
            public double Scale;
        }

        private sealed class RenderResult
        {
            public Bitmap Bitmap;
            public RenderLayout Layout;
            public Dictionary<long, GeneratedWaferDie> HitMap;
            public string Error;
        }
    }
}
