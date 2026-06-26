using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 검은 캔버스 이미지 뷰어 — 이미지 표시 + 검출 오버레이(녹색 박스/코너/판정/측정값).
    /// 마우스 휠 줌, 드래그 팬, 더블클릭 시 큰 팝업 창. 데이터 없으면 그냥 검은 창.
    /// 후속 검사 결과(BottomResult 등)를 SetImage/SetOverlay 로 바인딩한다.
    /// </summary>
    public class VisionImageView : Control
    {
        private Bitmap _img;
        private PointF[] _box;          // 검출 박스 코너(이미지 좌표)
        private string _verdict = "";
        private bool _pass = true;
        private string[] _lines;        // 좌상단 측정값 텍스트
        private PointF[] _marks;         // 칩핑/이물 마크(이미지 좌표)

        private float _zoom = 1f;
        private PointF _pan = PointF.Empty;
        private bool _dragging;
        private Point _dragStart;
        private PointF _panStart;

        // 마지막 그리기 변환(이미지→화면) 보관 — 오버레이 좌표 변환에 사용.
        private float _drawScale = 1f;
        private PointF _drawOrigin = PointF.Empty;

        private readonly Button _btnZoom = new Button();   // 우상단 확대 버튼
        private bool _crossline;                            // 크로스라인(십자선) 표시

        /// <summary>크로스라인(이미지 중심 십자선) 표시 ON/OFF.</summary>
        public bool Crossline
        {
            get => _crossline;
            set { _crossline = value; Invalidate(); }
        }

        public VisionImageView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Black;

            _btnZoom.Text = "⤢";
            _btnZoom.Size = new Size(26, 22);
            _btnZoom.FlatStyle = FlatStyle.Flat;
            _btnZoom.BackColor = Color.FromArgb(0x3A, 0x40, 0x4C);
            _btnZoom.ForeColor = Color.White;
            _btnZoom.Cursor = Cursors.Hand;
            _btnZoom.TabStop = false;
            _btnZoom.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnZoom.Click += (s, e) => ShowPopout();
            Controls.Add(_btnZoom);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            _btnZoom.Location = new Point(Width - _btnZoom.Width - 4, 4);
        }

        /// <summary>확대 버튼 숨김(팝업 인스턴스에서 중첩 방지).</summary>
        public void HideZoomButton() => _btnZoom.Visible = false;

        /// <summary>표시 이미지 설정(null 이면 검은 창). 호출 시 줌/팬 리셋.
        /// 외부(스토어)가 원본을 Dispose 해도 안전하도록 뷰 자체 복사본을 보관(동시접근 GDI 충돌 방지).
        /// 반환: 이미지가 실제로 표시됐으면 true. 클론 실패 시 기존 이미지를 유지(블랭크하지 않음)하고 false.</summary>
        public bool SetImage(Bitmap bmp)
        {
            if (bmp == null)
            {
                var o0 = _img; _img = null;
                if (o0 != null) { try { o0.Dispose(); } catch { } }
                _zoom = 1f; _pan = PointF.Empty; Invalidate();
                return false;
            }
            Bitmap copy;
            try { copy = (Bitmap)bmp.Clone(); }
            catch { return _img != null; }   // 클론 실패(동시 Dispose 등) → 기존 유지, 호출측이 재시도하도록 false
            var old = _img; _img = copy;
            if (old != null) { try { old.Dispose(); } catch { } }
            _zoom = 1f; _pan = PointF.Empty; Invalidate();
            return true;
        }

        /// <summary>검출 오버레이 설정 — 박스 코너/판정/측정 텍스트/마크(모두 이미지 좌표).</summary>
        public void SetOverlay(PointF[] boxCorners, bool pass, string verdict, string[] lines, PointF[] marks)
        {
            _box = boxCorners; _pass = pass; _verdict = verdict ?? "";
            _lines = lines; _marks = marks;
            Invalidate();
        }

        public void ClearOverlay()
        {
            _box = null; _marks = null; _lines = null; _verdict = "";
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.Black);
            if (_img == null)
            {
                TextRenderer.DrawText(g, "NO IMAGE", Font, ClientRectangle,
                    Color.FromArgb(0x55, 0x55, 0x55), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            // fit-to-window * zoom, 중앙 + 팬
            float fit = Math.Min((float)Width / _img.Width, (float)Height / _img.Height);
            float scale = fit * _zoom;
            _drawScale = scale;
            float dw = _img.Width * scale, dh = _img.Height * scale;
            float ox = (Width - dw) / 2f + _pan.X;
            float oy = (Height - dh) / 2f + _pan.Y;
            _drawOrigin = new PointF(ox, oy);

            g.InterpolationMode = _zoom >= 1f ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBilinear;
            // 외부 스레드가 비트맵을 교체/해제하는 순간과 겹치면 GDI 가 "개체 사용 중" 예외 → 한 프레임 건너뜀.
            try { g.DrawImage(_img, ox, oy, dw, dh); }
            catch { return; }

            // 크로스라인(이미지 중심 십자선) — ON 일 때
            if (_crossline)
            {
                PointF c = ToScreen(new PointF(_img.Width / 2f, _img.Height / 2f));
                using (var pen = new Pen(Color.FromArgb(0x33, 0xC8, 0xD8), 1f) { DashStyle = DashStyle.Dash })
                {
                    g.DrawLine(pen, c.X, 0, c.X, Height);
                    g.DrawLine(pen, 0, c.Y, Width, c.Y);
                }
            }

            // 검출 박스
            if (_box != null && _box.Length >= 2)
            {
                using (var pen = new Pen(_pass ? Color.FromArgb(0x4C, 0xE0, 0x6E) : Color.FromArgb(0xFF, 0x8A, 0x3A), 2f))
                {
                    PointF[] scr = ToScreen(_box);
                    g.DrawPolygon(pen, scr);
                }
            }
            // 마크(칩핑/이물)
            if (_marks != null)
            {
                using (var pen = new Pen(Color.FromArgb(0xFF, 0x5A, 0x4A), 2f))
                    foreach (PointF m in _marks)
                    {
                        PointF p = ToScreen(m);
                        g.DrawEllipse(pen, p.X - 9, p.Y - 9, 18, 18);
                    }
            }
            // 측정값 패널 — 불투명 검정 박스(반투명이면 제품이 비쳐 지저분), 텍스트 폭에 딱 맞춰 제품 가림 최소화.
            if (_lines != null && _lines.Length > 0)
            {
                int maxW = 0;
                foreach (string ln in _lines)
                {
                    int w = TextRenderer.MeasureText(g, ln, Font).Width;
                    if (w > maxW) maxW = w;
                }
                int boxW = maxW + 10, boxH = 6 + _lines.Length * 16;
                using (var bg = new SolidBrush(Color.Black))   // 완전 불투명
                    g.FillRectangle(bg, 6, 6, boxW, boxH);
                int y = 8;
                foreach (string ln in _lines)
                {
                    TextRenderer.DrawText(g, ln, Font, new Point(10, y), Color.White);
                    y += 16;
                }
            }
            // 판정
            if (!string.IsNullOrEmpty(_verdict))
            {
                using (var f = new Font("Segoe UI", 14F, FontStyle.Bold))
                    TextRenderer.DrawText(g, _verdict, f, new Point(Width - 120, 8),
                        _pass ? Color.FromArgb(0x4C, 0xE0, 0x6E) : Color.FromArgb(0xFF, 0x6B, 0x6B));
            }
        }

        private PointF[] ToScreen(PointF[] pts)
        {
            var r = new PointF[pts.Length];
            for (int i = 0; i < pts.Length; i++) r[i] = ToScreen(pts[i]);
            return r;
        }
        private PointF ToScreen(PointF p)
            => new PointF(_drawOrigin.X + p.X * _drawScale, _drawOrigin.Y + p.Y * _drawScale);

        // ── 줌/팬 ──
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_img == null) return;
            float old = _zoom;
            _zoom *= (e.Delta > 0) ? 1.15f : 1f / 1.15f;
            _zoom = Math.Max(1f, Math.Min(_zoom, 20f));
            // 커서 기준 줌 보정
            if (old != _zoom)
            {
                float k = _zoom / old;
                _pan.X = e.X - (e.X - _pan.X) * k;
                _pan.Y = e.Y - (e.Y - _pan.Y) * k;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true; _dragStart = e.Location; _panStart = _pan;
                Cursor = Cursors.SizeAll;
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                _pan = new PointF(_panStart.X + (e.X - _dragStart.X), _panStart.Y + (e.Y - _dragStart.Y));
                Invalidate();
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false; Cursor = Cursors.Default;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            // 더블클릭 = 휠 줌/드래그 이동 초기화
            _zoom = 1f; _pan = PointF.Empty;
            Invalidate();
        }

        /// <summary>현재 이미지/오버레이를 큰 창으로 띄운다.</summary>
        public void ShowPopout()
        {
            if (_img == null) return;
            try
            {
                var view = new VisionImageView { Dock = DockStyle.Fill };
                view.HideZoomButton();   // 팝업 안에서는 재팝업 버튼 숨김(더블클릭=리셋, 휠=줌)
                view.Crossline = _crossline;
                view.SetImage((Bitmap)_img.Clone());
                view.SetOverlay(_box, _pass, _verdict, _lines, _marks);
                var f = new Form
                {
                    Text = "Vision View — 확대 (휠 줌 / 드래그 이동 / 더블클릭 리셋)",
                    StartPosition = FormStartPosition.CenterParent,
                    Size = new Size(1100, 820),
                    BackColor = Color.Black
                };
                f.Controls.Add(view);
                f.Show(FindForm());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[VisionImageView] ShowPopout 실패: " + ex.Message); }
        }
    }
}
