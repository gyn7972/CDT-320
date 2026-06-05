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
    /// </summary>
    public class ZoomDialog : Form
    {
        private readonly Bitmap _image;
        private float _zoom = 1.0f;
        private PointF _offset = new PointF(0, 0);
        private bool _dragging;
        private Point _lastDrag;
        private Panel _canvas;
        private Label _statusBar;
        private Button _btnSave, _btnReset, _btnClose;

        public ZoomDialog(Bitmap image, string title = "Zoom")
        {
            _image = image != null ? new Bitmap(image) : null;

            Text = title;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(640, 480);
            ClientSize = new Size(1024, 768);
            BackColor = Color.FromArgb(40, 40, 40);
            ShowIcon = false;

            BuildLayout();
        }

        private void BuildLayout()
        {
            _statusBar = new Label
            {
                Dock = DockStyle.Top, Height = 28,
                BackColor = Color.FromArgb(0xD9, 0x77, 0x06), ForeColor = Color.White,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                Text = $"Image: {_image?.Width ?? 0} × {_image?.Height ?? 0}   Zoom: 100%   Wheel = zoom, Drag = pan, DoubleClick = 1:1",
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(_statusBar);

            var bar = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = Color.FromArgb(45, 45, 48) };
            _btnReset = new Button { Location = new Point(10, 6), Size = new Size(100, 32), Text = "1:1",  FlatStyle = FlatStyle.Flat, BackColor = Color.White, Font = new Font("맑은 고딕", 10F) };
            _btnSave  = new Button { Location = new Point(116, 6), Size = new Size(100, 32), Text = "SAVE", FlatStyle = FlatStyle.Flat, BackColor = Color.White, Font = new Font("맑은 고딕", 10F) };
            _btnClose = new Button { Location = new Point(222, 6), Size = new Size(100, 32), Text = "CLOSE", FlatStyle = FlatStyle.Flat, BackColor = Color.White, Font = new Font("맑은 고딕", 10F), DialogResult = DialogResult.OK };
            _btnReset.Click += (s, e) => { _zoom = 1.0f; _offset = new PointF(0, 0); _canvas.Invalidate(); UpdateStatus(); };
            _btnSave .Click += (s, e) => DoSave();
            bar.Controls.Add(_btnReset); bar.Controls.Add(_btnSave); bar.Controls.Add(_btnClose);
            Controls.Add(bar);

            _canvas = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };
            _canvas.Paint     += OnPaintCanvas;
            _canvas.MouseWheel+= OnWheel;
            _canvas.MouseDown += OnDown;
            _canvas.MouseMove += OnMove;
            _canvas.MouseUp   += OnUp;
            _canvas.DoubleClick += (s, e) => { _zoom = 1.0f; _offset = new PointF(0, 0); _canvas.Invalidate(); UpdateStatus(); };
            _canvas.MouseEnter += (s, e) => _canvas.Focus();   // 휠 이벤트 받도록
            _canvas.SetStyle();
            Controls.Add(_canvas);
            Controls.SetChildIndex(_canvas, 0);
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
            _dragging = true; _lastDrag = e.Location;
            _canvas.Cursor = Cursors.Hand;
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
