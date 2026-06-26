using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ColletFinder
{
    public partial class MainForm : Form
    {
        private Bitmap _source;            // 원본
        private Bitmap _result;            // 결과
        private Rectangle _roi = Rectangle.Empty; // 이미지 좌표계 ROI
        private DetectedRect _detected;    // 검출된 회전 사각형(최대 블랍)

        // ROI 드래그 상태 (표시 좌표계)
        private bool _selecting;
        private Point _selStart;
        private Point _selEnd;

        public MainForm()
        {
            InitializeComponent();

            btnLoad.Click += BtnLoad_Click;
            btnProcess.Click += BtnProcess_Click;
            btnSave.Click += BtnSave_Click;
            btnClearRoi.Click += BtnClearRoi_Click;

            picSource.MouseDown += PicSource_MouseDown;
            picSource.MouseMove += PicSource_MouseMove;
            picSource.MouseUp += PicSource_MouseUp;
            picSource.Paint += PicSource_Paint;
            picSource.Resize += (s, e) => picSource.Invalidate();

            // CUDA 가용 여부에 따라 체크박스 상태 설정
            if (StdDevFilter.CudaAvailable)
            {
                chkCuda.Enabled = true;
                chkCuda.Checked = true;
                chkCuda.Text = "CUDA 사용";
            }
            else
            {
                chkCuda.Enabled = false;
                chkCuda.Checked = false;
                chkCuda.Text = "CUDA 미지원";
            }

            UpdateStatus(HardwareInfo() + "  —  이미지를 불러오세요.");
        }

        /// <summary>감지된 GPU/CPU 정보 문자열.</summary>
        private static string HardwareInfo()
        {
            string gpu = StdDevFilter.CudaAvailable
                ? "GPU: " + StdDevFilter.CudaDeviceName
                : "GPU: 없음";
            return gpu + "  |  CPU: " + StdDevFilter.CpuThreads + " 스레드";
        }

        // ───────────────────────── 버튼 핸들러 ─────────────────────────

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "이미지 파일|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|모든 파일|*.*";
                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    // 파일 잠금을 피하기 위해 복사본으로 로드
                    Bitmap loaded;
                    using (var tmp = Image.FromFile(ofd.FileName))
                        loaded = new Bitmap(tmp);

                    _source?.Dispose();
                    _source = loaded;
                    picSource.Image = _source;

                    _roi = Rectangle.Empty;
                    _detected = default(DetectedRect);
                    _result?.Dispose();
                    _result = null;
                    picResult.Image = null;

                    picSource.Invalidate();
                    UpdateStatus($"불러옴: {Path.GetFileName(ofd.FileName)}  ({_source.Width}×{_source.Height})  —  ROI를 드래그하거나 바로 '처리 실행'(전체 처리)");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "이미지를 불러오지 못했습니다.\n" + ex.Message,
                        "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnProcess_Click(object sender, EventArgs e)
        {
            if (_source == null)
            {
                MessageBox.Show(this, "먼저 이미지를 불러오세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int block = (int)numBlock.Value;
            double thr = (double)numThreshold.Value;
            Rectangle roi = _roi.IsEmpty
                ? new Rectangle(0, 0, _source.Width, _source.Height)
                : _roi;
            try
            {
                Cursor = Cursors.WaitCursor;
                var sw = Stopwatch.StartNew();
                ComputeBackend used;
                byte[] mask;
                Bitmap res = StdDevFilter.Apply(_source, roi, block, thr, chkCuda.Checked, out used, out mask);

                // 중심/각도 검출:
                //  - 빠른 검출(모멘트): 라벨링 없이 전경 모멘트로 즉시 산출(노이즈 적은 단일 객체에 적합)
                //  - 정밀(블랍): 연결요소 라벨링으로 가장 큰 블랍만 골라 강건하게 산출
                _detected = chkFast.Checked
                    ? BlobRectFinder.FindByMoments(mask, _source.Width, _source.Height)
                    : BlobRectFinder.FindLargestRect(mask, _source.Width, _source.Height);
                if (_detected.Found)
                    DrawDetection(res, _detected);
                sw.Stop();

                _result?.Dispose();
                _result = res;
                picResult.Image = _result;
                picSource.Invalidate();

                string backend = used == ComputeBackend.Cuda
                    ? "GPU/CUDA(" + StdDevFilter.CudaDeviceName + ")"
                    : "CPU×" + StdDevFilter.CpuThreads + "스레드";
                string detail = _detected.Found
                    ? $"검출 ▶ 중심({_detected.Center.X:0.0}, {_detected.Center.Y:0.0})  각도 {_detected.AngleDeg:0.0}°  크기 {Math.Max(_detected.Width, _detected.Height):0}×{Math.Min(_detected.Width, _detected.Height):0}  면적 {_detected.Area:#,0}px"
                    : "블랍 없음";
                // 비트맵 입출력(그레이 변환·결과 비트맵 생성) 시간은 제외하고 순수 연산 시간만 표시
                string proc;
                if (used == ComputeBackend.Cuda) proc = "CUDA 처리";
                else if (chkCuda.Checked && StdDevFilter.CudaAvailable) proc = "CPU 처리(CUDA 실패→폴백)";
                else proc = "CPU 처리";
                string findLabel = chkFast.Checked ? "전경분석" : "블랍 찾기";
                string timing = $"{proc} {StdDevFilter.LastComputeMs}ms · {findLabel} {BlobRectFinder.LastLabelMs}ms · 각도 계산 {BlobRectFinder.LastRectMs}ms";
                UpdateStatus($"[{backend}]  {timing}  — {detail}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "처리 중 오류가 발생했습니다.\n" + ex.Message,
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_result == null)
            {
                MessageBox.Show(this, "저장할 결과가 없습니다. 먼저 '처리 실행'을 누르세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG|*.png|BMP|*.bmp|JPEG|*.jpg";
                sfd.FileName = "result.png";
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    ImageFormat fmt = ImageFormat.Png;
                    string ext = Path.GetExtension(sfd.FileName).ToLowerInvariant();
                    if (ext == ".bmp") fmt = ImageFormat.Bmp;
                    else if (ext == ".jpg" || ext == ".jpeg") fmt = ImageFormat.Jpeg;

                    _result.Save(sfd.FileName, fmt);
                    UpdateStatus($"저장됨: {sfd.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "저장에 실패했습니다.\n" + ex.Message,
                        "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnClearRoi_Click(object sender, EventArgs e)
        {
            _roi = Rectangle.Empty;
            _detected = default(DetectedRect);
            picSource.Invalidate();
            UpdateStatus("ROI 초기화됨 — '처리 실행' 시 전체 이미지를 처리합니다.");
        }

        // ───────────────────────── ROI 드래그 ─────────────────────────

        private void PicSource_MouseDown(object sender, MouseEventArgs e)
        {
            if (_source == null || e.Button != MouseButtons.Left) return;
            RectangleF disp = GetImageDisplayRect();
            if (disp.Width <= 0) return;

            _selecting = true;
            _selStart = ClampToRect(e.Location, disp);
            _selEnd = _selStart;
            picSource.Invalidate();
        }

        private void PicSource_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_selecting) return;
            RectangleF disp = GetImageDisplayRect();
            _selEnd = ClampToRect(e.Location, disp);

            Rectangle r = DisplaySelectionToImageRoi();
            UpdateStatus($"ROI 선택 중 — X:{r.X} Y:{r.Y}  W:{r.Width} H:{r.Height}");
            picSource.Invalidate();
        }

        private void PicSource_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_selecting) return;
            _selecting = false;

            Rectangle r = DisplaySelectionToImageRoi();
            if (r.Width >= 1 && r.Height >= 1)
            {
                _roi = r;
                UpdateStatus($"ROI 설정됨 — X:{r.X} Y:{r.Y}  W:{r.Width} H:{r.Height}  ('처리 실행'을 누르세요)");
            }
            else
            {
                // 너무 작으면 클릭으로 간주 → ROI 해제
                _roi = Rectangle.Empty;
                UpdateStatus("ROI 해제됨 — 전체 이미지를 처리합니다.");
            }
            picSource.Invalidate();
        }

        private void PicSource_Paint(object sender, PaintEventArgs e)
        {
            if (_source == null) return;
            RectangleF disp = GetImageDisplayRect();
            if (disp.Width <= 0) return;

            e.Graphics.SmoothingMode = SmoothingMode.None;

            // 확정된 ROI (빨강)
            if (!_roi.IsEmpty)
            {
                RectangleF d = ImageRectToDisplay(_roi, disp);
                using (var pen = new Pen(Color.Red, 2f))
                    e.Graphics.DrawRectangle(pen, d.X, d.Y, d.Width, d.Height);
            }

            // 드래그 중인 선택 (노랑 점선)
            if (_selecting)
            {
                var r = NormalizedDisplayRect(_selStart, _selEnd);
                using (var pen = new Pen(Color.Yellow, 1.5f) { DashStyle = DashStyle.Dash })
                    e.Graphics.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
            }

            // 검출된 사각형(최대 블랍) 오버레이
            if (_detected.Found && _detected.Corners != null)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var pts = new PointF[4];
                for (int i = 0; i < 4; i++)
                    pts[i] = ImagePointToDisplay(_detected.Corners[i], disp);
                using (var pen = new Pen(Color.Lime, 2f))
                    e.Graphics.DrawPolygon(pen, pts);

                PointF c = ImagePointToDisplay(_detected.Center, disp);
                using (var bc = new SolidBrush(Color.Red))
                    e.Graphics.FillEllipse(bc, c.X - 3.5f, c.Y - 3.5f, 7f, 7f);
                using (var pc = new Pen(Color.Red, 2f))
                {
                    e.Graphics.DrawLine(pc, c.X - 9, c.Y, c.X + 9, c.Y);
                    e.Graphics.DrawLine(pc, c.X, c.Y - 9, c.X, c.Y + 9);
                }

                string label = "중심(" + _detected.Center.X.ToString("0") + ", " + _detected.Center.Y.ToString("0") +
                               ")  각도 " + _detected.AngleDeg.ToString("0.0") + "°";
                DrawLabel(e.Graphics, label, c.X, c.Y + 12,
                          picSource.ClientSize.Width, picSource.ClientSize.Height, 13f);
            }
        }

        // ───────────────────────── 좌표 변환 ─────────────────────────

        /// <summary>Zoom 모드 PictureBox 안에서 이미지가 실제로 그려지는 사각형(표시 좌표).</summary>
        private RectangleF GetImageDisplayRect()
        {
            if (_source == null) return RectangleF.Empty;
            float cw = picSource.ClientSize.Width;
            float ch = picSource.ClientSize.Height;
            float iw = _source.Width;
            float ih = _source.Height;
            if (iw <= 0 || ih <= 0 || cw <= 0 || ch <= 0) return RectangleF.Empty;

            float scale = Math.Min(cw / iw, ch / ih);
            float dw = iw * scale;
            float dh = ih * scale;
            float ox = (cw - dw) / 2f;
            float oy = (ch - dh) / 2f;
            return new RectangleF(ox, oy, dw, dh);
        }

        private static Point ClampToRect(Point p, RectangleF r)
        {
            int x = (int)Math.Round(Math.Max(r.Left, Math.Min(r.Right, p.X)));
            int y = (int)Math.Round(Math.Max(r.Top, Math.Min(r.Bottom, p.Y)));
            return new Point(x, y);
        }

        private PointF DisplayToImage(Point p, RectangleF disp)
        {
            float scale = disp.Width / _source.Width;
            float ix = (p.X - disp.X) / scale;
            float iy = (p.Y - disp.Y) / scale;
            ix = Math.Max(0, Math.Min(_source.Width, ix));
            iy = Math.Max(0, Math.Min(_source.Height, iy));
            return new PointF(ix, iy);
        }

        private Rectangle DisplaySelectionToImageRoi()
        {
            RectangleF disp = GetImageDisplayRect();
            PointF a = DisplayToImage(_selStart, disp);
            PointF b = DisplayToImage(_selEnd, disp);

            int x0 = (int)Math.Round(Math.Min(a.X, b.X));
            int y0 = (int)Math.Round(Math.Min(a.Y, b.Y));
            int x1 = (int)Math.Round(Math.Max(a.X, b.X));
            int y1 = (int)Math.Round(Math.Max(a.Y, b.Y));

            x0 = Math.Max(0, Math.Min(_source.Width, x0));
            y0 = Math.Max(0, Math.Min(_source.Height, y0));
            x1 = Math.Max(0, Math.Min(_source.Width, x1));
            y1 = Math.Max(0, Math.Min(_source.Height, y1));

            return new Rectangle(x0, y0, x1 - x0, y1 - y0);
        }

        private RectangleF ImageRectToDisplay(Rectangle img, RectangleF disp)
        {
            float scale = disp.Width / _source.Width;
            float x = disp.X + img.X * scale;
            float y = disp.Y + img.Y * scale;
            return new RectangleF(x, y, img.Width * scale, img.Height * scale);
        }

        private static RectangleF NormalizedDisplayRect(Point a, Point b)
        {
            float x = Math.Min(a.X, b.X);
            float y = Math.Min(a.Y, b.Y);
            float w = Math.Abs(a.X - b.X);
            float h = Math.Abs(a.Y - b.Y);
            return new RectangleF(x, y, w, h);
        }

        /// <summary>검출된 사각형(4변)·중심을 결과 비트맵에 그려 넣는다.</summary>
        private static void DrawDetection(Bitmap bmp, DetectedRect d)
        {
            if (!d.Found || d.Corners == null) return;
            float lw = Math.Max(1.5f, bmp.Width / 500f);   // 이미지 크기에 비례한 선 두께
            float cs = Math.Max(6f, bmp.Width / 100f);      // 중심 마커 크기
            float fontPx = Math.Max(13f, bmp.Width / 45f);  // 라벨 글자 크기
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.Lime, lw))
                    g.DrawPolygon(pen, d.Corners);
                using (var brush = new SolidBrush(Color.Yellow))
                    foreach (var c in d.Corners)
                        g.FillEllipse(brush, c.X - lw * 1.5f, c.Y - lw * 1.5f, lw * 3f, lw * 3f);

                // 중심: 빨간 원 + 십자
                using (var bc = new SolidBrush(Color.Red))
                    g.FillEllipse(bc, d.Center.X - lw * 1.5f, d.Center.Y - lw * 1.5f, lw * 3f, lw * 3f);
                using (var pc = new Pen(Color.Red, lw))
                {
                    g.DrawLine(pc, d.Center.X - cs, d.Center.Y, d.Center.X + cs, d.Center.Y);
                    g.DrawLine(pc, d.Center.X, d.Center.Y - cs, d.Center.X, d.Center.Y + cs);
                }

                // 중심 좌표·각도 텍스트 라벨
                string label = "중심(" + d.Center.X.ToString("0") + ", " + d.Center.Y.ToString("0") +
                               ")  각도 " + d.AngleDeg.ToString("0.0") + "°";
                DrawLabel(g, label, d.Center.X, d.Center.Y + cs + 4, bmp.Width, bmp.Height, fontPx);
            }
        }

        /// <summary>가독성 있는 텍스트 라벨(반투명 배경)을 (cx,cy) 아래에 그린다. 범위 밖이면 안쪽으로 보정.</summary>
        private static void DrawLabel(Graphics g, string text, float cx, float cy,
                                      float clientW, float clientH, float fontPx)
        {
            using (var font = new Font("Segoe UI", fontPx, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                SizeF sz = g.MeasureString(text, font);
                float tx = cx - sz.Width / 2f;
                float ty = cy;
                if (tx < 2) tx = 2;
                if (tx + sz.Width > clientW - 2) tx = clientW - 2 - sz.Width;
                if (ty + sz.Height > clientH - 2) ty = cy - sz.Height - 8;
                if (ty < 2) ty = 2;
                using (var bg = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
                    g.FillRectangle(bg, tx - 3, ty - 2, sz.Width + 6, sz.Height + 4);
                using (var fg = new SolidBrush(Color.Yellow))
                    g.DrawString(text, font, fg, tx, ty);
            }
        }

        private PointF ImagePointToDisplay(PointF img, RectangleF disp)
        {
            float scale = disp.Width / _source.Width;
            return new PointF(disp.X + img.X * scale, disp.Y + img.Y * scale);
        }

        // ───────────────────────── 기타 ─────────────────────────

        private void UpdateStatus(string text)
        {
            lblStatus.Text = "  " + text;
        }

        private void btnProcess_Click_1(object sender, EventArgs e)
        {

        }
    }
}
