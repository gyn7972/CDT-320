using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using QMC.Vision.Core;
using QMC.Vision.Modules;

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
        private double _zoom;   // 0 = auto fit, 그 외 = 연속 배율. 중앙 기준 + 팬 오프셋.
        private float  _panX, _panY;     // 줌 상태에서의 화면 px 팬 오프셋
        private bool   _panning;         // 가운데 버튼 드래그 팬 중
        private Point  _panLast;

        // ── 측정(Measure) 상태 ──
        private bool _measuring, _haveA, _haveB, _measureDone;
        private PointF _measA, _measB;   // 이미지 좌표(줌/리사이즈 무관 기준)
        /// <summary>측정용 스케일(mm/pixel). 0 이면 px 로 표시.</summary>
        public double MmPerPixelX { get; set; }
        public double MmPerPixelY { get; set; }

        /// <summary>마지막 두 점 측정의 픽셀 거리(이미지 좌표 기준). 측정 완료 전 0.</summary>
        public double LastMeasurePixels { get; private set; }
        /// <summary>두 점 측정(둘째 점 클릭) 완료 시 픽셀 거리를 통지.</summary>
        public event Action<double> MeasureCompleted;

        // ── ROI 드래그 편집 상태 ──
        private bool   _editing;
        private string _editKind;          // "Search" | "Train"
        private Roi    _editRoi;
        private Point  _dragStart;
        private Point  _dragNow;

        /// <summary>ROI 드래그 종료 시 호출됨. (kind, newRoi).</summary>
        public event Action<string, Roi> RoiEdited;

        private bool _showCrosshair = true;
        /// <summary>십자선(CrossLine) 표시. 툴바 CrossLine 버튼과 동기화.</summary>
        public bool ShowCrosshair
        {
            get { return _showCrosshair; }
            set { _showCrosshair = value; if (_tbCross != null) _tbCross.Checked = value; Invalidate(); }
        }
        public bool ShowLiveLabel { get; set; } = true;
        public string InfoText    { get; set; } = "STAGE\r\nW:640 H:480";

        // ── 결과 오버레이(판정 OK/NG + 결과 라인) ──
        private string   _verdict;        // "OK"/"NG"/사용자 텍스트. null=미표시
        private bool     _verdictPass;
        private string[] _resultLines;    // 우측 하단 결과 라인

        /// <summary>판정 표시(우측 상단 큰 글자). pass=true 초록, false 빨강. null/빈문자=숨김.</summary>
        public void SetVerdict(string text, bool pass) { _verdict = text; _verdictPass = pass; Invalidate(); }
        /// <summary>결과 라인(우측 하단). null/빈배열=숨김.</summary>
        public void SetResultLines(string[] lines) { _resultLines = lines; Invalidate(); }
        /// <summary>판정/결과 오버레이 모두 지움.</summary>
        public void ClearResultOverlay() { _verdict = null; _resultLines = null; Invalidate(); }

        // ── 내장 툴바(공용) — Grab/Live/Stop/Save/Load/측정/맞춤. Show 계열 미포함 ──
        private ToolStrip       _tools;
        private ToolStripButton _tbGrab, _tbLive, _tbStop, _tbSave, _tbLoad, _tbMeasure, _tbFit, _tbCross;
        private ToolStripLabel  _tbMag;
        private IVisionModule   _source;          // Grab/Live 대상(모듈) — AttachModule 로 지정
        private Action<GrabResult> _liveHandler;  // 라이브 프레임 구독 핸들러
        private bool _live;

        /// <summary>내장 툴바 표시 여부. true 면 상단에 Grab/Live/Stop/Save/Load/측정/맞춤 툴바 노출.</summary>
        public bool ShowToolbar
        {
            get { return _tools != null && _tools.Visible; }
            set { if (_tools != null) { _tools.Visible = value; Invalidate(); } }
        }

        /// <summary>Grab/Live 대상 모듈 지정 — 페이지는 이 한 줄이면 툴바가 동작.</summary>
        public void AttachModule(IVisionModule module) { _source = module; }

        /// <summary>지정 모듈의 스케일(mm/px)을 측정 환산에 주입 — 측정값이 mm 로 표시되도록.</summary>
        private void ApplyModuleScale()
        {
            try
            {
                if (_source == null) return;
                var m = _source.ExportCameraMapping();
                MmPerPixelX = m.ScaleX; MmPerPixelY = m.ScaleY;
            }
            catch { }
        }

        public CameraView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable |
                     ControlStyles.UserPaint, true);
            InitializeComponent();   // Stage 90 — BackColor + 이벤트 named 핸들러 연결(.Designer.cs)
            BuildToolbar();
        }

        // ── 내장 툴바 구성/동작 ──
        private void BuildToolbar()
        {
            _tools = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                Visible = false,            // 기본 off — 페이지가 ShowToolbar=true 로 켠다
                Renderer = new ToolStripProfessionalRenderer(),
                ImageScalingSize = new Size(16, 16)
            };
            _tbGrab    = new ToolStripButton("Grab")  { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _tbLive    = new ToolStripButton("Live")  { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _tbStop    = new ToolStripButton("Stop")  { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _tbSave    = new ToolStripButton("Save")  { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _tbLoad    = new ToolStripButton("Load")  { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _tbMeasure = new ToolStripButton("측정")  { DisplayStyle = ToolStripItemDisplayStyle.Text, CheckOnClick = true };
            _tbFit     = new ToolStripButton("맞춤")  { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _tbCross   = new ToolStripButton("CrossLine") { DisplayStyle = ToolStripItemDisplayStyle.Text, CheckOnClick = true, Checked = _showCrosshair };
            _tbMag     = new ToolStripLabel("Mag. 1.00x");

            _tbGrab.Click    += (s, e) => DoToolbarGrab();
            _tbLive.Click    += (s, e) => DoToolbarLive();
            _tbStop.Click    += (s, e) => DoToolbarStop();
            _tbSave.Click    += (s, e) => DoToolbarSave();
            _tbLoad.Click    += (s, e) => DoToolbarLoad();
            _tbMeasure.Click += (s, e) => { if (_tbMeasure.Checked) { ApplyModuleScale(); BeginMeasure(); } else EndMeasure(); };
            _tbFit.Click     += (s, e) => ZoomFit();
            _tbCross.Click   += (s, e) => ShowCrosshair = _tbCross.Checked;

            _tools.Items.AddRange(new ToolStripItem[]
            {
                _tbGrab, _tbLive, _tbStop, new ToolStripSeparator(),
                _tbSave, _tbLoad, new ToolStripSeparator(),
                _tbMeasure, _tbFit, _tbCross, new ToolStripSeparator(), _tbMag
            });
            Controls.Add(_tools);
        }

        private void DoToolbarGrab()
        {
            if (_source == null) return;
            try { using (var g = _source.Grab()) { if (g != null && g.IsSuccess) SetFrame(g); } }
            catch { }
        }

        private void DoToolbarLive()
        {
            var cam = _source?.Camera;
            if (cam == null || _live) return;
            try
            {
                _liveHandler = r =>
                {
                    if (r == null || !r.IsSuccess || r.Image == null) return;
                    var bmp = (Bitmap)r.Image.Clone();
                    if (IsHandleCreated) BeginInvoke(new Action(() => { SetImage(bmp); bmp.Dispose(); }));
                    else bmp.Dispose();
                };
                cam.FrameReceived += _liveHandler;
                cam.TriggerMode = CameraTriggerMode.Continuous;
                cam.StartLive();
                _live = true;
            }
            catch { }
        }

        private void DoToolbarStop()
        {
            var cam = _source?.Camera;
            if (cam == null) { _live = false; return; }
            try { cam.StopLive(); } catch { }
            try { if (_liveHandler != null) cam.FrameReceived -= _liveHandler; } catch { }
            _liveHandler = null;
            _live = false;
        }

        private void DoToolbarSave()
        {
            if (_frame == null) return;
            using (var d = new SaveFileDialog { Filter = "PNG|*.png|BMP|*.bmp|JPEG|*.jpg", FileName = "frame_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png" })
                if (d.ShowDialog(this) == DialogResult.OK)
                    try { _frame.Save(d.FileName); } catch { }
        }

        private void DoToolbarLoad()
        {
            using (var d = new OpenFileDialog { Filter = "Image|*.png;*.bmp;*.jpg;*.jpeg;*.tif;*.tiff|All|*.*" })
                if (d.ShowDialog(this) == DialogResult.OK)
                    try { using (var b = new Bitmap(d.FileName)) SetImage(b); } catch { }
        }

        /// <summary>현재 배율(이미지px→화면px)을 툴바 Mag 라벨에 갱신.</summary>
        private void UpdateMagLabel()
        {
            if (_tbMag == null || _frame == null) return;
            var dst = DisplayRect();
            if (dst.Width <= 0) return;
            double scale = (double)dst.Width / _frame.Width;
            _tbMag.Text = "Mag. " + scale.ToString("0.00") + "x";
        }

        // Stage 90 — 기존 DoubleClick 람다 추출(인스턴스 필드만 참조 → closure 아님). 동작 동일.
        private void OnViewDoubleClick(object s, EventArgs e)
        {
            // 더블클릭 = 화면 맞춤(제자리 줌 초기화). 팝업 창 없음 — 줌/팬은 휠·가운데드래그로 제자리.
            if (_editing || _measuring) return;
            ZoomFit();
        }

        // ── ROI 드래그 편집 진입 ──
        public void BeginRoiDrag(string kind, Roi current)
        {
            _measuring = false;   // ROI 편집 진입 시 측정 모드 해제
            _editing  = true;
            _editKind = kind;
            _editRoi  = current?.Clone();
            Cursor    = Cursors.Cross;
            Invalidate();
        }

        private void OnMouseDownEditing(object s, MouseEventArgs e)
        {
            // 팬: 가운데 버튼(항상) 또는 좌클릭(측정/ROI편집 아닐 때) — 줌 상태에서만.
            if (_frame != null && _zoom > 0 &&
                (e.Button == MouseButtons.Middle ||
                 (e.Button == MouseButtons.Left && !_measuring && !_editing)))
            { _panning = true; _panLast = e.Location; Cursor = Cursors.SizeAll; return; }
            if (_measuring && e.Button == MouseButtons.Left && _frame != null) { HandleMeasureClick(e.Location); return; }
            if (!_editing || e.Button != MouseButtons.Left || _frame == null) return;
            _dragStart = e.Location;
            _dragNow   = e.Location;
        }
        private void OnMouseMoveEditing(object s, MouseEventArgs e)
        {
            if (_panning) { _panX += e.X - _panLast.X; _panY += e.Y - _panLast.Y; _panLast = e.Location; Invalidate(); return; }
            if (!_editing || e.Button != MouseButtons.Left || _frame == null) return;
            _dragNow = e.Location;
            Invalidate();
        }
        private void OnMouseUpEditing(object s, MouseEventArgs e)
        {
            if (_panning) { _panning = false; Cursor = _measuring ? Cursors.Cross : Cursors.Default; return; }
            if (!_editing || _frame == null) { return; }
            // 화면→이미지 좌표 변환 (줌 반영)
            var dst = DisplayRect();
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
            if (IsHandleCreated) BeginInvoke(new Action(() => { UpdateMagLabel(); Invalidate(); }));
        }

        public void SetOverlay(Roi roi, MatchResult result)
        {
            _overlayRoi    = roi;
            _overlayResult = result;
            Invalidate();
        }

        /// <summary>표시 배율 설정. 0=자동맞춤(Fit), 그 외=중앙 기준 확대. Fit 시 팬 초기화.</summary>
        public void SetZoom(double zoom) { _zoom = zoom < 0 ? 0 : zoom; if (_zoom <= 0) { _panX = _panY = 0; } UpdateMagLabel(); Invalidate(); }

        /// <summary>화면 맞춤(줌/팬 초기화) — 더블클릭/맞춤 버튼.</summary>
        public void ZoomFit() { _zoom = 0; _panX = _panY = 0; UpdateMagLabel(); Invalidate(); }

        /// <summary>리사이즈 시 배율 라벨 갱신(맞춤 상태에서 화면 크기에 따라 배율 변동).</summary>
        protected override void OnResize(EventArgs e) { base.OnResize(e); UpdateMagLabel(); }

        /// <summary>비트맵을 직접 표시(그랩 결과가 아닌 정적/라이브 이미지). 줌/팬은 유지(초기값=Fit).</summary>
        public void SetImage(Bitmap bmp)
        {
            _frame?.Dispose();
            _frame = bmp != null ? (Bitmap)bmp.Clone() : null;
            if (IsHandleCreated) BeginInvoke(new Action(() => { UpdateMagLabel(); Invalidate(); })); else Invalidate();
        }

        /// <summary>현재 표시 중인 프레임(원본 참조 — Dispose 금지). 없으면 null.</summary>
        public Bitmap CurrentFrame => _frame;

        /// <summary>툴바가 차지하는 상단 높이(툴바 숨김 시 0).</summary>
        private int TopInset => (_tools != null && _tools.Visible) ? _tools.Height : 0;

        /// <summary>현재 프레임이 그려지는 화면 사각형(줌+팬+툴바 오프셋 반영). 좌표 변환과 공유.</summary>
        private Rectangle DisplayRect()
        {
            if (_frame == null) return Rectangle.Empty;
            int top = TopInset;
            int availH = Math.Max(1, ClientSize.Height - top);
            if (_zoom <= 0)
            {
                var r = FitRect(_frame.Size, new Size(ClientSize.Width, availH));
                r.Y += top;
                return r;
            }
            int w = (int)(_frame.Width * _zoom);
            int h = (int)(_frame.Height * _zoom);
            int x = (ClientSize.Width - w) / 2 + (int)_panX;
            int y = top + (availH - h) / 2 + (int)_panY;
            return new Rectangle(x, y, w, h);
        }

        /// <summary>마우스 휠 = 커서 기준 확대/축소.</summary>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_frame == null) return;
            var before = DisplayRect();
            if (before.Width <= 0 || before.Height <= 0) return;
            double curScale = (double)before.Width / _frame.Width;
            double ix = (e.X - before.Left) / curScale;   // 커서 아래 이미지 좌표
            double iy = (e.Y - before.Top)  / curScale;

            double newScale = curScale * (e.Delta > 0 ? 1.25 : 1.0 / 1.25);
            if (newScale < 0.05) newScale = 0.05;
            if (newScale > 50)   newScale = 50;

            _zoom = newScale;
            int w = (int)(_frame.Width * _zoom), h = (int)(_frame.Height * _zoom);
            int top = TopInset, availH = Math.Max(1, ClientSize.Height - top);
            int centerX = (ClientSize.Width - w) / 2, centerY = top + (availH - h) / 2;
            // 커서 아래 이미지 좌표가 같은 화면 위치에 머물도록 팬 보정
            _panX = (float)(e.X - ix * newScale - centerX);
            _panY = (float)(e.Y - iy * newScale - centerY);
            UpdateMagLabel();
            Invalidate();
        }

        /// <summary>휠 수신을 위해 마우스 진입 시 포커스.</summary>
        protected override void OnMouseEnter(EventArgs e) { Focus(); base.OnMouseEnter(e); }

        // ── 측정(Measure) ──
        /// <summary>측정 모드 시작. 첫 클릭=시작점, 둘째 클릭=끝점(빨간 바 + mm 표시), 다시 클릭하면 재시작.</summary>
        public void BeginMeasure()
        {
            _measuring = true; _editing = false; _haveA = false; _haveB = false; _measureDone = false;
            Cursor = Cursors.Cross;
            Invalidate();
        }

        /// <summary>측정 모드 여부.</summary>
        public bool IsMeasuring => _measuring;

        /// <summary>측정 모드 종료(클릭이 다시 일반 동작으로).</summary>
        public void EndMeasure()
        {
            _measuring = false; _haveA = false; _haveB = false; _measureDone = false;
            Cursor = Cursors.Default;
            Invalidate();
        }

        private void HandleMeasureClick(Point scr)
        {
            if (_measureDone) return;   // 측정 완료 후엔 클릭 무시(다시 측정하려면 측정 버튼 재클릭)
            var dst = DisplayRect();
            if (dst.Width <= 0 || dst.Height <= 0) return;
            double sx = (double)_frame.Width / dst.Width, sy = (double)_frame.Height / dst.Height;
            var p = new PointF((float)((scr.X - dst.Left) * sx), (float)((scr.Y - dst.Top) * sy));
            if (!_haveA || _haveB) { _measA = p; _haveA = true; _haveB = false; }   // 첫 점
            else
            {
                _measB = p; _haveB = true;
                _measureDone = true;   // 둘째 점 → 완료(선 유지, 추가 클릭 무시)
                double dx = _measB.X - _measA.X, dy = _measB.Y - _measA.Y;
                LastMeasurePixels = Math.Sqrt(dx * dx + dy * dy);
                MeasureCompleted?.Invoke(LastMeasurePixels);
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            if (_frame != null)
            {
                var dst = DisplayRect();
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_frame, dst);
                DrawOverlays(g, dst);
            }
            else
            {
                // STAGE 정보(좌상단)와 겹치지 않게 우측 상단에 표시.
                using (var f  = new Font("Consolas", 12F))
                using (var br = new SolidBrush(UiTheme.VisionInfoFg))
                {
                    var sz = g.MeasureString("No image", f);
                    g.DrawString("No image", f, br, ClientSize.Width - sz.Width - 12, 8 + TopInset);
                }
            }
            // 좌상단 STAGE 정보 (툴바 높이만큼 내려 그려 가려지지 않게)
            if (!string.IsNullOrEmpty(InfoText))
                using (var br = new SolidBrush(UiTheme.VisionInfoFg))
                using (var f  = new Font("Consolas", 9F))
                    g.DrawString(InfoText, f, br, 8, 6 + TopInset);

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

            // 측정 바
            if (_measuring) DrawMeasure(g);

            // 판정(OK/NG) — 우측 상단 큰 글자
            if (!string.IsNullOrEmpty(_verdict))
                using (var f  = new Font("Segoe UI", 26F, FontStyle.Bold))
                using (var br = new SolidBrush(_verdictPass ? Color.LimeGreen : Color.Red))
                {
                    var sz = g.MeasureString(_verdict, f);
                    g.DrawString(_verdict, f, br, ClientSize.Width - sz.Width - 14, 6 + TopInset);
                }

            // 결과 라인 — 우측 하단(아래에서 위로 쌓기)
            if (_resultLines != null && _resultLines.Length > 0)
                using (var f  = new Font("맑은 고딕", 9F))
                using (var br = new SolidBrush(_verdictPass ? Color.FromArgb(120, 230, 120) : Color.FromArgb(255, 120, 120)))
                {
                    float y = ClientSize.Height - 6 - _resultLines.Length * 17;
                    foreach (var line in _resultLines)
                    {
                        if (string.IsNullOrEmpty(line)) { y += 17; continue; }
                        var sz = g.MeasureString(line, f);
                        g.DrawString(line, f, br, ClientSize.Width - sz.Width - 10, y);
                        y += 17;
                    }
                }

            // 외곽선
            using (var p = new Pen(Color.DimGray, 1f))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }

        private void DrawMeasure(Graphics g)
        {
            if (_frame == null || !_haveA) return;
            var dst = DisplayRect();
            if (dst.Width <= 0 || dst.Height <= 0) return;
            double rx = (double)dst.Width / _frame.Width, ry = (double)dst.Height / _frame.Height;
            Point A = new Point(dst.Left + (int)(_measA.X * rx), dst.Top + (int)(_measA.Y * ry));

            using (var br = new SolidBrush(Color.Red))
                g.FillEllipse(br, A.X - 4, A.Y - 4, 8, 8);

            if (!_haveB)
            {
                using (var br = new SolidBrush(Color.Red))
                using (var f = new Font("Consolas", 9F, FontStyle.Bold))
                    g.DrawString("둘째 점 클릭", f, br, A.X + 6, A.Y + 6);
                return;
            }

            Point B = new Point(dst.Left + (int)(_measB.X * rx), dst.Top + (int)(_measB.Y * ry));
            using (var pen = new Pen(Color.Red, 2.5f))
                g.DrawLine(pen, A, B);
            using (var br = new SolidBrush(Color.Red))
                g.FillEllipse(br, B.X - 4, B.Y - 4, 8, 8);

            // 거리(이미지 px) → mm
            double dxpx = _measB.X - _measA.X, dypx = _measB.Y - _measA.Y;
            string txt;
            if (MmPerPixelX > 0 || MmPerPixelY > 0)
            {
                double mmx = MmPerPixelX > 0 ? MmPerPixelX : MmPerPixelY;
                double mmy = MmPerPixelY > 0 ? MmPerPixelY : MmPerPixelX;
                double dmm = Math.Sqrt(dxpx * mmx * dxpx * mmx + dypx * mmy * dypx * mmy);
                txt = dmm.ToString("F3") + " mm";
            }
            else
            {
                txt = Math.Sqrt(dxpx * dxpx + dypx * dypx).ToString("F1") + " px";
            }

            using (var f = new Font("Consolas", 11F, FontStyle.Bold))
            {
                var sz = g.MeasureString(txt, f);
                using (var bg = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
                    g.FillRectangle(bg, B.X + 8, B.Y - 10, sz.Width + 6, sz.Height + 2);
                using (var br = new SolidBrush(Color.Red))
                    g.DrawString(txt, f, br, B.X + 11, B.Y - 9);
            }
        }

        private void DrawOverlays(Graphics g, Rectangle dst)
        {
            if (ShowCrosshair)
                using (var p = new Pen(Color.FromArgb(120, 255, 255, 255), 1f) { DashStyle = DashStyle.Dash })
                {
                    // 뷰(화면) 정중앙 고정 — 확대/이동과 무관(툴바 높이 보정).
                    int top = TopInset;
                    int cx = ClientSize.Width / 2;
                    int cy = top + (ClientSize.Height - top) / 2;
                    g.DrawLine(p, 0, cy, ClientSize.Width, cy);
                    g.DrawLine(p, cx, top, cx, ClientSize.Height);
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
