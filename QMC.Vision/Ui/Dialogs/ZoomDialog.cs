using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Dialogs
{
    /// <summary>
    /// 이미지 줌/팬 다이얼로그.
    /// - 마우스 휠: 50% ~ 800% 줌
    /// - 좌클릭 드래그: 팬
    /// - 더블클릭: 1:1 리셋
    /// Stage 92 — Designer/Code 분리(디자이너 로드 가능). shell 은 .Designer.cs, 그리기/줌/팬 로직은 여기.
    /// </summary>
    public partial class ZoomDialog : Form
    {
        private readonly Bitmap _image;
        private float _zoom = 1.0f;
        private PointF _offset = new PointF(0, 0);
        private bool _dragging;
        private Point _lastDrag;

        // ── 측정(Measure) ──
        private bool _measuring, _haveA, _haveB;
        private PointF _mA, _mB;   // 이미지 좌표
        /// <summary>측정 mm/pixel. 0 이면 px 로 표시. 호출자가 ShowDialog 전에 설정.</summary>
        public double MmPerPixelX { get; set; }
        public double MmPerPixelY { get; set; }

        /// <summary>디자이너 전용 파라미터리스 생성자(이미지 없음). 런타임 호출자는 (Bitmap, title) 사용.</summary>
        public ZoomDialog() : this(null) { }

        public ZoomDialog(Bitmap image, string title = "Zoom")
        {
            _image = image != null ? new Bitmap(image) : null;
            InitializeComponent();
            _canvas.SetStyle();                 // 더블버퍼 (Panel.SetStyle protected → 확장)
            Text = title;                       // 런타임 제목
            _statusBar.Text = $"Image: {_image?.Width ?? 0} × {_image?.Height ?? 0}   Zoom: 100%   Wheel = zoom, Drag = pan, DoubleClick = 1:1";
        }

        // ── 이벤트 핸들러 (Designer 에서 named 연결) ──
        private void OnResetClick(object sender, EventArgs e) => ResetView();
        private void OnSaveClick(object sender, EventArgs e) => DoSave();
        private void OnCanvasDoubleClick(object sender, EventArgs e) => ResetView();
        private void OnCanvasMouseEnter(object sender, EventArgs e) => _canvas.Focus();   // 휠 이벤트 받도록

        private void ResetView()
        {
            _zoom = 1.0f; _offset = new PointF(0, 0);
            _canvas.Invalidate(); UpdateStatus();
        }

        private void OnPaintCanvas(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(_canvas.BackColor);
            if (_image == null) return;
            g.InterpolationMode = _zoom >= 1f ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBicubic;
            float w = _image.Width  * _zoom;
            float h = _image.Height * _zoom;
            float x = (_canvas.Width  - w) / 2f + _offset.X;
            float y = (_canvas.Height - h) / 2f + _offset.Y;
            g.DrawImage(_image, x, y, w, h);
            using (var p = new Pen(Color.LimeGreen, 1f) { DashStyle = DashStyle.Dash })
            {
                // 크로스헤어
                g.DrawLine(p, _canvas.Width / 2f, 0, _canvas.Width / 2f, _canvas.Height);
                g.DrawLine(p, 0, _canvas.Height / 2f, _canvas.Width, _canvas.Height / 2f);
            }
            if (_measuring) DrawMeasure(g);
        }

        private void DrawMeasure(Graphics g)
        {
            if (_image == null || !_haveA) return;
            PointF A = ImageToScreen(_mA);
            using (var br = new SolidBrush(Color.Red)) g.FillEllipse(br, A.X - 4, A.Y - 4, 8, 8);

            if (!_haveB)
            {
                using (var br = new SolidBrush(Color.Red))
                using (var f = new Font("Consolas", 10F, FontStyle.Bold))
                    g.DrawString("둘째 점 클릭", f, br, A.X + 6, A.Y + 6);
                return;
            }

            PointF B = ImageToScreen(_mB);
            using (var pen = new Pen(Color.Red, 2.5f)) g.DrawLine(pen, A, B);
            using (var br = new SolidBrush(Color.Red)) g.FillEllipse(br, B.X - 4, B.Y - 4, 8, 8);

            double dxpx = _mB.X - _mA.X, dypx = _mB.Y - _mA.Y;
            string txt;
            if (MmPerPixelX > 0 || MmPerPixelY > 0)
            {
                double mmx = MmPerPixelX > 0 ? MmPerPixelX : MmPerPixelY;
                double mmy = MmPerPixelY > 0 ? MmPerPixelY : MmPerPixelX;
                double dmm = Math.Sqrt(dxpx * mmx * dxpx * mmx + dypx * mmy * dypx * mmy);
                txt = dmm.ToString("F3") + " mm";
            }
            else txt = Math.Sqrt(dxpx * dxpx + dypx * dypx).ToString("F1") + " px";

            using (var f = new Font("Consolas", 12F, FontStyle.Bold))
            {
                var sz = g.MeasureString(txt, f);
                using (var bg = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                    g.FillRectangle(bg, B.X + 8, B.Y - 10, sz.Width + 6, sz.Height + 2);
                using (var br = new SolidBrush(Color.Red))
                    g.DrawString(txt, f, br, B.X + 11, B.Y - 9);
            }
        }

        private void OnWheel(object sender, MouseEventArgs e)
        {
            float old = _zoom;
            float delta = e.Delta > 0 ? 1.15f : 1f / 1.15f;
            _zoom = Math.Max(0.5f, Math.Min(8.0f, _zoom * delta));
            // 마우스 위치를 기준으로 줌 (cursor-anchored)
            float dx = e.X - _canvas.Width / 2f - _offset.X;
            float dy = e.Y - _canvas.Height / 2f - _offset.Y;
            _offset.X += dx * (1 - _zoom / old);
            _offset.Y += dy * (1 - _zoom / old);
            _canvas.Invalidate();
            UpdateStatus();
        }

        private void OnDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (_measuring && _image != null)
            {
                var p = ScreenToImage(e.Location);
                if (!_haveA || _haveB) { _mA = p; _haveA = true; _haveB = false; }
                else { _mB = p; _haveB = true; }
                _canvas.Invalidate();
                return;   // 측정 중엔 팬 안 함
            }
            _dragging = true; _lastDrag = e.Location;
            _canvas.Cursor = Cursors.Hand;
        }

        private void OnMeasureToggle(object sender, EventArgs e)
        {
            _measuring = !_measuring;
            if (_measuring)
            {
                _haveA = false; _haveB = false;
                _btnMeasure.BackColor = Color.MistyRose;
                _canvas.Cursor = Cursors.Cross;
                _statusBar.Text = "측정 모드: 첫 점, 둘째 점을 클릭 (다시 클릭하면 재시작) — '측정' 다시 누르면 해제";
            }
            else
            {
                _btnMeasure.BackColor = Color.White;
                _canvas.Cursor = Cursors.Default;
                UpdateStatus();
            }
            _canvas.Invalidate();
        }

        // 이미지↔화면 좌표 (현재 zoom/offset 기준)
        private PointF ImageToScreen(PointF img)
        {
            float w = _image.Width * _zoom, h = _image.Height * _zoom;
            float x = (_canvas.Width - w) / 2f + _offset.X;
            float y = (_canvas.Height - h) / 2f + _offset.Y;
            return new PointF(x + img.X * _zoom, y + img.Y * _zoom);
        }
        private PointF ScreenToImage(Point scr)
        {
            float w = _image.Width * _zoom, h = _image.Height * _zoom;
            float x = (_canvas.Width - w) / 2f + _offset.X;
            float y = (_canvas.Height - h) / 2f + _offset.Y;
            return new PointF((scr.X - x) / _zoom, (scr.Y - y) / _zoom);
        }
        private void OnMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            _offset.X += e.X - _lastDrag.X;
            _offset.Y += e.Y - _lastDrag.Y;
            _lastDrag = e.Location;
            _canvas.Invalidate();
        }
        private void OnUp(object sender, MouseEventArgs e)
        {
            _dragging = false; _canvas.Cursor = Cursors.Default;
        }

        private void UpdateStatus()
        {
            _statusBar.Text = $"Image: {_image?.Width ?? 0} × {_image?.Height ?? 0}   Zoom: {_zoom * 100:F0}%   offset=({_offset.X:F0},{_offset.Y:F0})";
        }

        private void DoSave()
        {
            if (_image == null) return;
            using (var dlg = new SaveFileDialog { Title = "Save image", Filter = "PNG|*.png|BMP|*.bmp|JPEG|*.jpg" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    ImageFormat fmt = ImageFormat.Png;
                    var ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
                    if (ext == ".bmp") fmt = ImageFormat.Bmp;
                    else if (ext == ".jpg" || ext == ".jpeg") fmt = ImageFormat.Jpeg;
                    _image.Save(dlg.FileName, fmt);
                    MessageBox.Show("Saved: " + dlg.FileName);
                }
                catch (Exception ex) { MessageBox.Show("Save fail: " + ex.Message); }
            }
        }
    }

    /// <summary>Panel.SetStyle 가 protected 라 호출하기 위한 확장.</summary>
    internal static class PanelStyleExt
    {
        public static void SetStyle(this Panel panel)
        {
            // 더블 버퍼링 켬
            typeof(Panel)
                .GetMethod("SetStyle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(panel, new object[]
                {
                    System.Windows.Forms.ControlStyles.AllPaintingInWmPaint |
                    System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer |
                    System.Windows.Forms.ControlStyles.UserPaint,
                    true
                });
        }
    }
}
