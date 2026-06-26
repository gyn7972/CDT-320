using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QMC.Common.Ui.Controls
{
    /// <summary>
    /// 범용 카메라 출력 표시 컨트롤 + 오버레이(ROI 사각형, 결과 마크, 크로스헤어, STAGE 정보, 측정, 판정/결과 라인).
    /// 프로젝트 비의존: 이미지는 <see cref="Bitmap"/>, ROI 는 이미지좌표 <see cref="RectangleF"/>, 결과는 <see cref="OverlayMark"/>,
    /// 그랩/라이브 소스는 <see cref="ICameraViewSource"/> 로만 다룬다. 비전 전용 동작은 파생 클래스에서 오버로드/override.
    /// </summary>
    public class CameraViewBase : Control
    {
        private Bitmap _frame;        // 표시(방향 적용 후) 프레임 — 그리기/오버레이/좌표변환의 기준.
        private Bitmap _srcFrame;     // 원본(방향 미적용) 프레임 — 방향 토글 시 _frame 재생성용.
        private RotateFlipType _displayOrientation = RotateFlipType.RotateNoneFlipNone;
        private RectangleF _overlayRoi;
        private bool _hasOverlayRoi;
        private OverlayMark[] _overlayMarks;
        private double _zoom;   // 0 = auto fit, 그 외 = 연속 배율. 중앙 기준 + 팬 오프셋.
        private float  _panX, _panY;
        private bool   _panning;
        private Point  _panLast;

        // ── 측정(Measure) 상태 ──
        private bool _measuring, _haveA, _haveB, _measureDone;
        private PointF _measA, _measB;   // 이미지 좌표
        private PointF _measMouse;       // 둘째 점 클릭 전 마우스 위치(이미지 좌표) — 미리보기용
        private bool   _haveMouse;       // _measMouse 유효 여부
        /// <summary>완료된 측정 세그먼트(이미지 좌표). 측정 클리어 전까지(측정 해제해도) 영구 오버레이.</summary>
        private readonly List<MeasureSeg> _measSegs = new List<MeasureSeg>();
        private struct MeasureSeg
        {
            public PointF A, B;
            public MeasureSeg(PointF a, PointF b) { A = a; B = b; }
        }
        /// <summary>측정용 스케일(mm/pixel). 0 이면 px 로 표시.</summary>
        public double MmPerPixelX { get; set; }
        public double MmPerPixelY { get; set; }

        /// <summary>마지막 두 점 측정의 픽셀 거리(이미지 좌표 기준). 측정 완료 전 0.</summary>
        public double LastMeasurePixels { get; private set; }
        /// <summary>두 점 측정(둘째 점 클릭) 완료 시 픽셀 거리를 통지.</summary>
        public event Action<double> MeasureCompleted;

        // ── ROI 드래그 편집 상태 ──
        private bool   _editing;
        private string _editKind;          // "Search" | "Train" 등(파생 클래스 정의)
        private Point  _dragStart;
        private Point  _dragNow;

        /// <summary>ROI 드래그 종료 시 호출됨 — (kind, 이미지좌표 사각형).</summary>
        public event Action<string, RectangleF> RoiEdited;

        private bool _showCrosshair = true;
        /// <summary>십자선(CrossLine) 표시. 툴바 CrossLine 버튼과 동기화.</summary>
        public bool ShowCrosshair
        {
            get { return _showCrosshair; }
            set { _showCrosshair = value; if (_tbCross != null) _tbCross.Checked = value; Invalidate(); }
        }
        public bool ShowLiveLabel { get; set; } = true;
        public string InfoText    { get; set; } = "STAGE\r\nW:640 H:480";

        /// <summary>STAGE/No image/Live 글자색(프로젝트별 테마). 기본 LightGreen.</summary>
        public Color InfoForeColor { get; set; } = Color.LightGreen;

        /// <summary>표시 방향 변환(X반전/Y반전/90° 회전). 그랩·라이브로 들어온 원본 프레임에 적용해
        /// 화면에 보이는 이미지를 회전/반전한다. 기본 <see cref="RotateFlipType.RotateNoneFlipNone"/>(무변환).
        /// 변경 시 현재 표시 중인 프레임도 즉시 재변환한다.</summary>
        public RotateFlipType DisplayOrientation
        {
            get { return _displayOrientation; }
            set
            {
                if (_displayOrientation == value) return;
                _displayOrientation = value;
                RebuildOrientedFrame();
                RaiseFrameChangedIfSizeChanged();
                if (IsHandleCreated) BeginInvoke(new Action(() => { UpdateMagLabel(); Invalidate(); }));
                else Invalidate();
            }
        }

        /// <summary>원본(_srcFrame) → 방향 적용 표시 프레임(_frame) 재생성.
        /// 무변환이면 _frame 은 _srcFrame 을 그대로 참조(추가 메모리 없음).</summary>
        private void RebuildOrientedFrame()
        {
            // 직전 oriented 복사본(원본과 다른 인스턴스)만 해제 — 원본은 별도 관리.
            if (_frame != null && !ReferenceEquals(_frame, _srcFrame))
            {
                try { _frame.Dispose(); } catch { }
            }
            if (_srcFrame == null) { _frame = null; return; }
            if (_displayOrientation == RotateFlipType.RotateNoneFlipNone) { _frame = _srcFrame; return; }
            try
            {
                var clone = (Bitmap)_srcFrame.Clone();
                clone.RotateFlip(_displayOrientation);
                _frame = clone;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[CameraViewBase] RotateFlip 실패: " + ex.Message);
                _frame = _srcFrame;   // 변환 실패 시 원본 표시(빈 화면 방지)
            }
        }

        /// <summary>표시 프레임의 크기가 바뀌면 발생(그랩/로드/첫 라이브 프레임). 페이지가 STAGE/ROI 자동맞춤에 사용.</summary>
        public event Action FrameChanged;

        /// <summary>검사기별 전용 오버레이 그리기 콜백. (Graphics, 이미지px→화면px 변환). null 이면 미사용.</summary>
        public Action<System.Drawing.Graphics, Func<System.Drawing.PointF, System.Drawing.PointF>> CustomOverlayPaint;
        private int _lastFrameW = -1, _lastFrameH = -1;
        private void RaiseFrameChangedIfSizeChanged()
        {
            int w = _frame?.Width ?? 0, h = _frame?.Height ?? 0;
            if (w == _lastFrameW && h == _lastFrameH) return;
            _lastFrameW = w; _lastFrameH = h;
            try { FrameChanged?.Invoke(); } catch { }
        }

        // ── 결과 오버레이(판정 OK/NG + 결과 라인) ──
        private string   _verdict;
        private bool     _verdictPass;
        private string[] _resultLines;
        private Color[]  _resultLineColors;   // 줄별 색(null 또는 Empty 면 판정색 사용)

        public void SetVerdict(string text, bool pass) { _verdict = text; _verdictPass = pass; Invalidate(); }
        public void SetResultLines(string[] lines) { _resultLines = lines; _resultLineColors = null; Invalidate(); }
        /// <summary>줄별 색 지정(일반=초록, 에러=빨강 등). colors[i].A==0 이면 그 줄은 판정색.</summary>
        public void SetResultLines(string[] lines, Color[] colors) { _resultLines = lines; _resultLineColors = colors; Invalidate(); }
        public void ClearResultOverlay() { _verdict = null; _resultLines = null; Invalidate(); }

        // ── 내장 툴바 ──
        private ToolStrip       _tools;
        private ToolStripButton _tbGrab, _tbLive, _tbStop, _tbSave, _tbLoad, _tbMeasure, _tbMeasClear, _tbFit, _tbCross;
        private ToolStripLabel  _tbMag;
        private ICameraViewSource _source;
        private bool _live;

        public bool ShowToolbar
        {
            get { return _tools != null && _tools.Visible; }
            set { if (_tools != null) { _tools.Visible = value; Invalidate(); } }
        }

        /// <summary>Grab/Live 영상 소스 지정. null 이면 툴바 Grab/Live 비활성.</summary>
        public void AttachSource(ICameraViewSource source) { _source = source; }

        /// <summary>라이브 시작/정지/단발 그랩을 코드에서 직접 제어(툴바 없이도 사용). 소스가 있어야 동작.</summary>
        public void StartLive() { DoToolbarLive(); }
        public void StopLive()  { DoToolbarStop(); }
        public void GrabOnce()  { DoToolbarGrab(); }
        public bool IsLive => _live;

        /// <summary>측정 시작 직전 호출(스케일 갱신 훅). 기본 no-op — 파생 클래스가 모듈 스케일 주입 등에 사용.</summary>
        protected virtual void RefreshMeasureScale() { }

        public CameraViewBase()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable |
                     ControlStyles.UserPaint, true);
            BackColor = Color.Black;
            MouseDown   += OnMouseDownEditing;
            MouseMove   += OnMouseMoveEditing;
            MouseUp     += OnMouseUpEditing;
            DoubleClick += OnViewDoubleClick;
            BuildToolbar();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_frame != null && !ReferenceEquals(_frame, _srcFrame)) { try { _frame.Dispose(); } catch { } }
                try { _srcFrame?.Dispose(); } catch { }
                _frame = null; _srcFrame = null;
            }
            base.Dispose(disposing);
        }

        // ── 내장 툴바 구성/동작 ──
        private void BuildToolbar()
        {
            _tools = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                Visible = false,
                Renderer = new ToolStripProfessionalRenderer(),
                ImageScalingSize = new Size(16, 16)
            };
            _tbGrab    = new ToolStripButton("Grab")  { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _tbLive    = new ToolStripButton("Live")  { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _tbStop    = new ToolStripButton("Stop")  { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _tbSave    = new ToolStripButton("Save")  { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _tbLoad    = new ToolStripButton("Load")  { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _tbMeasure = new ToolStripButton("측정")  { DisplayStyle = ToolStripItemDisplayStyle.Text, CheckOnClick = true };
            _tbMeasClear = new ToolStripButton("측정 클리어") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _tbFit     = new ToolStripButton("맞춤")  { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _tbCross   = new ToolStripButton("CrossLine") { DisplayStyle = ToolStripItemDisplayStyle.Text, CheckOnClick = true, Checked = _showCrosshair };
            _tbMag     = new ToolStripLabel("Mag. 1.00x");

            _tbGrab.Click    += (s, e) => DoToolbarGrab();
            _tbLive.Click    += (s, e) => DoToolbarLive();
            _tbStop.Click    += (s, e) => DoToolbarStop();
            _tbSave.Click    += (s, e) => DoToolbarSave();
            _tbLoad.Click    += (s, e) => DoToolbarLoad();
            _tbMeasure.Click += (s, e) => { if (_tbMeasure.Checked) { RefreshMeasureScale(); BeginMeasure(); } else EndMeasure(); };
            _tbMeasClear.Click += (s, e) => ClearMeasurements();
            _tbFit.Click     += (s, e) => ZoomFit();
            _tbCross.Click   += (s, e) => ShowCrosshair = _tbCross.Checked;

            _tools.Items.AddRange(new ToolStripItem[]
            {
                _tbGrab, _tbLive, _tbStop, new ToolStripSeparator(),
                _tbSave, _tbLoad, new ToolStripSeparator(),
                _tbMeasure, _tbMeasClear, _tbFit, _tbCross, new ToolStripSeparator(), _tbMag
            });
            Controls.Add(_tools);
        }

        private void DoToolbarGrab()
        {
            if (_source == null) return;
            try { var b = _source.GrabFrame(); if (b != null) { SetImage(b); b.Dispose(); } }
            catch { }
        }

        private void DoToolbarLive()
        {
            if (_source == null || _live || !_source.SupportsLive) return;
            try
            {
                _source.StartLive(bmp =>
                {
                    if (bmp == null) return;
                    try
                    {
                        if (IsHandleCreated && !IsDisposed)
                            BeginInvoke(new Action(() => { SetImage(bmp); bmp.Dispose(); }));
                        else bmp.Dispose();
                    }
                    catch { try { bmp.Dispose(); } catch { } }
                });
                _live = true;
                if (_tbLive != null) _tbLive.Checked = true;   // 라이브 중 버튼 활성(눌림) 표시
            }
            catch { }
        }

        private void DoToolbarStop()
        {
            try { _source?.StopLive(); } catch { }
            _live = false;
            if (_tbLive != null) _tbLive.Checked = false;   // 라이브 종료 → 활성 표시 해제
        }

        /// <summary>컨트롤/폼 종료 시 라이브를 반드시 정지 — 종료 레이스 방지.</summary>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { if (_live) DoToolbarStop(); } catch { }
            base.OnHandleDestroyed(e);
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

        private void UpdateMagLabel()
        {
            if (_tbMag == null || _frame == null) return;
            var dst = DisplayRect();
            if (dst.Width <= 0) return;
            double scale = (double)dst.Width / _frame.Width;
            _tbMag.Text = "Mag. " + scale.ToString("0.00") + "x";
        }

        private void OnViewDoubleClick(object s, EventArgs e)
        {
            if (_editing || _measuring) return;
            ZoomFit();
        }

        // ── ROI 드래그 편집 진입 (이미지좌표 사각형) ──
        public void BeginRoiDrag(string kind, RectangleF current)
        {
            _measuring = false;
            _editing  = true;
            _editKind = kind;
            Cursor    = Cursors.Cross;
            Invalidate();
        }

        private void OnMouseDownEditing(object s, MouseEventArgs e)
        {
            bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            // 측정/편집 중에도 Ctrl+좌클릭(또는 휠클릭) 드래그로 이미지 이동(팬) 허용 — 확대 후 다른 영역으로 이동해 측정하기 위함.
            if (_frame != null && _zoom > 0 &&
                (e.Button == MouseButtons.Middle ||
                 (e.Button == MouseButtons.Left && ctrl) ||
                 (e.Button == MouseButtons.Left && !_measuring && !_editing)))
            { _panning = true; _panLast = e.Location; Cursor = Cursors.SizeAll; return; }
            if (_measuring && e.Button == MouseButtons.Left && _frame != null)
            { HandleMeasureClick(e.Location, (Control.ModifierKeys & Keys.Shift) == Keys.Shift); return; }
            if (!_editing || e.Button != MouseButtons.Left || _frame == null) return;
            _dragStart = e.Location;
            _dragNow   = e.Location;
        }
        private void OnMouseMoveEditing(object s, MouseEventArgs e)
        {
            if (_panning) { _panX += e.X - _panLast.X; _panY += e.Y - _panLast.Y; _panLast = e.Location; Invalidate(); return; }
            // 측정 중 첫 점만 찍힌 상태면 마우스 위치를 추적해 미리보기 라인(+Shift 직선 스냅) 표시.
            if (_measuring && _haveA && !_haveB && _frame != null)
            {
                var dstm = DisplayRect();
                if (dstm.Width > 0 && dstm.Height > 0)
                {
                    double mx = (double)_frame.Width / dstm.Width, my = (double)_frame.Height / dstm.Height;
                    _measMouse = new PointF((float)((e.X - dstm.Left) * mx), (float)((e.Y - dstm.Top) * my));
                    _haveMouse = true;
                    Invalidate();
                }
                return;
            }
            if (!_editing || e.Button != MouseButtons.Left || _frame == null) return;
            _dragNow = e.Location;
            Invalidate();
        }
        private void OnMouseUpEditing(object s, MouseEventArgs e)
        {
            if (_panning) { _panning = false; Cursor = _measuring ? Cursors.Cross : Cursors.Default; return; }
            if (!_editing || _frame == null) { return; }
            var dst = DisplayRect();
            int x1 = Math.Min(_dragStart.X, _dragNow.X);
            int y1 = Math.Min(_dragStart.Y, _dragNow.Y);
            int x2 = Math.Max(_dragStart.X, _dragNow.X);
            int y2 = Math.Max(_dragStart.Y, _dragNow.Y);
            if (x2 - x1 < 4 || y2 - y1 < 4) { _editing = false; Cursor = Cursors.Default; Invalidate(); return; }

            double sx = (double)_frame.Width  / dst.Width;
            double sy = (double)_frame.Height / dst.Height;
            // 소수점 유지(정수 변환 안 함) — ROI 미세 조정/서브픽셀 정밀도 보존.
            double dix1 = (x1 - dst.Left) * sx;
            double diy1 = (y1 - dst.Top)  * sy;
            double dix2 = (x2 - dst.Left) * sx;
            double diy2 = (y2 - dst.Top)  * sy;
            dix1 = Math.Max(0, Math.Min(_frame.Width  - 1, dix1));
            dix2 = Math.Max(0, Math.Min(_frame.Width  - 1, dix2));
            diy1 = Math.Max(0, Math.Min(_frame.Height - 1, diy1));
            diy2 = Math.Max(0, Math.Min(_frame.Height - 1, diy2));

            var rect = new RectangleF((float)dix1, (float)diy1, (float)(dix2 - dix1), (float)(diy2 - diy1));
            _editing = false;
            Cursor   = Cursors.Default;
            Invalidate();
            try { RoiEdited?.Invoke(_editKind, rect); } catch { }
        }

        public void SetOverlay(RectangleF roiImageRect, IList<OverlayMark> marks)
        {
            _overlayRoi    = roiImageRect;
            _hasOverlayRoi = roiImageRect.Width > 0 && roiImageRect.Height > 0;
            _overlayMarks  = (marks != null && marks.Count > 0) ? System.Linq.Enumerable.ToArray(marks) : null;
            Invalidate();
        }

        public void SetZoom(double zoom) { _zoom = zoom < 0 ? 0 : zoom; if (_zoom <= 0) { _panX = _panY = 0; } UpdateMagLabel(); Invalidate(); }
        public void ZoomFit() { _zoom = 0; _panX = _panY = 0; UpdateMagLabel(); Invalidate(); }
        protected override void OnResize(EventArgs e) { base.OnResize(e); UpdateMagLabel(); }

        /// <summary>비트맵을 표시(내부 복제 — 원본은 호출자가 Dispose 가능).
        /// <see cref="DisplayOrientation"/> 가 설정돼 있으면 그 방향변환을 적용해 표시한다.</summary>
        public void SetImage(Bitmap bmp)
        {
            // 이전 oriented 복사본(원본과 다른 인스턴스)부터 해제 → 그 다음 원본 해제(이중 해제 방지).
            if (_frame != null && !ReferenceEquals(_frame, _srcFrame))
            {
                try { _frame.Dispose(); } catch { }
            }
            _frame = null;
            try { _srcFrame?.Dispose(); } catch { }
            _srcFrame = bmp != null ? (Bitmap)bmp.Clone() : null;
            RebuildOrientedFrame();
            RaiseFrameChangedIfSizeChanged();
            if (IsHandleCreated) BeginInvoke(new Action(() => { UpdateMagLabel(); Invalidate(); })); else Invalidate();
        }

        /// <summary>현재 표시 중인 프레임(원본 참조 — Dispose 금지). 없으면 null.</summary>
        public Bitmap CurrentFrame => _frame;

        private int TopInset => (_tools != null && _tools.Visible) ? _tools.Height : 0;

        protected Rectangle DisplayRect()   // 파생(비전 CameraView)에서 마우스→이미지 좌표 변환에 사용
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

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_frame == null) return;
            var before = DisplayRect();
            if (before.Width <= 0 || before.Height <= 0) return;
            double curScale = (double)before.Width / _frame.Width;
            double ix = (e.X - before.Left) / curScale;
            double iy = (e.Y - before.Top)  / curScale;

            // 편집 중인 드래그 박스 모서리를 이미지 좌표로 보존(줌 후 화면좌표로 복원) → ROI 가 줌을 따라가게.
            double ds_ix = (_dragStart.X - before.Left) / curScale, ds_iy = (_dragStart.Y - before.Top) / curScale;
            double dn_ix = (_dragNow.X   - before.Left) / curScale, dn_iy = (_dragNow.Y   - before.Top) / curScale;

            double newScale = curScale * (e.Delta > 0 ? 1.25 : 1.0 / 1.25);
            if (newScale < 0.05) newScale = 0.05;
            if (newScale > 50)   newScale = 50;

            _zoom = newScale;
            int w = (int)(_frame.Width * _zoom), h = (int)(_frame.Height * _zoom);
            int top = TopInset, availH = Math.Max(1, ClientSize.Height - top);
            int centerX = (ClientSize.Width - w) / 2, centerY = top + (availH - h) / 2;
            _panX = (float)(e.X - ix * newScale - centerX);
            _panY = (float)(e.Y - iy * newScale - centerY);

            if (_editing)
            {
                var after = DisplayRect();
                double asc = (double)after.Width / _frame.Width;
                _dragStart = new Point((int)(after.Left + ds_ix * asc), (int)(after.Top + ds_iy * asc));
                _dragNow   = new Point((int)(after.Left + dn_ix * asc), (int)(after.Top + dn_iy * asc));
            }

            UpdateMagLabel();
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { Focus(); base.OnMouseEnter(e); }

        // ── 측정(Measure) ──
        public void BeginMeasure()
        {
            _measuring = true; _editing = false; _haveA = false; _haveB = false; _measureDone = false; _haveMouse = false;
            Cursor = Cursors.Cross;
            Invalidate();
        }

        public bool IsMeasuring => _measuring;

        public void EndMeasure()
        {
            _measuring = false; _haveA = false; _haveB = false; _measureDone = false; _haveMouse = false;
            Cursor = Cursors.Default;
            Invalidate();
        }

        private void HandleMeasureClick(Point scr, bool shift)
        {
            var dst = DisplayRect();
            if (dst.Width <= 0 || dst.Height <= 0) return;
            double sx = (double)_frame.Width / dst.Width, sy = (double)_frame.Height / dst.Height;
            var p = new PointF((float)((scr.X - dst.Left) * sx), (float)((scr.Y - dst.Top) * sy));
            // 첫 점이 없거나 직전 측정이 끝난 상태(_haveB/_measureDone)면 새 측정을 시작 — 추가 클릭으로 연속 측정.
            if (!_haveA || _haveB || _measureDone)
            {
                _measA = p; _haveA = true; _haveB = false; _measureDone = false; _haveMouse = false;
            }
            else
            {
                // Shift 누르면 둘째 점을 수평/수직/45° 직선으로 스냅.
                _measB = shift ? ConstrainStraight(_measA, p) : p;
                double dx = _measB.X - _measA.X, dy = _measB.Y - _measA.Y;
                LastMeasurePixels = Math.Sqrt(dx * dx + dy * dy);
                // 완료 세그먼트를 리스트에 누적(클리어 전까지 보존) → 클릭할 때마다 사라지지 않고 계속 쌓임.
                _measSegs.Add(new MeasureSeg(_measA, _measB));
                MeasureCompleted?.Invoke(LastMeasurePixels);
                // 활성 점 초기화 → 다음 클릭이 새 측정의 첫 점이 됨.
                _haveA = false; _haveB = false; _measureDone = false; _haveMouse = false;
            }
            Invalidate();
        }

        /// <summary>누적된 측정 오버레이를 모두 지우고 진행 중 상태도 초기화.</summary>
        public void ClearMeasurements()
        {
            _measSegs.Clear();
            _haveA = false; _haveB = false; _measureDone = false; _haveMouse = false;
            Invalidate();
        }

        /// <summary>앵커 a 기준으로 점 p 를 가장 가까운 45° 방향(수평/수직/대각) 직선 위로 스냅.</summary>
        private static PointF ConstrainStraight(PointF a, PointF p)
        {
            double dx = p.X - a.X, dy = p.Y - a.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6) return p;
            double step = Math.PI / 4.0;                       // 45°
            double snapped = Math.Round(Math.Atan2(dy, dx) / step) * step;
            return new PointF((float)(a.X + len * Math.Cos(snapped)),
                              (float)(a.Y + len * Math.Sin(snapped)));
        }

        private void DrawFrameFast(Graphics g, Rectangle dst)
        {
            if (_frame == null || dst.Width <= 0 || dst.Height <= 0) return;

            var view    = new Rectangle(0, TopInset, ClientSize.Width, Math.Max(1, ClientSize.Height - TopInset));
            var visible = Rectangle.Intersect(dst, view);
            if (visible.Width <= 0 || visible.Height <= 0) return;

            double scale = (double)dst.Width / _frame.Width;
            if (scale <= 0) return;

            float sx = (float)((visible.X - dst.X) / scale);
            float sy = (float)((visible.Y - dst.Y) / scale);
            float sw = (float)(visible.Width  / scale);
            float sh = (float)(visible.Height / scale);
            if (sx < 0) { sw += sx; sx = 0; }
            if (sy < 0) { sh += sy; sy = 0; }
            if (sx + sw > _frame.Width)  sw = _frame.Width  - sx;
            if (sy + sh > _frame.Height) sh = _frame.Height - sy;
            if (sw <= 0 || sh <= 0) return;

            g.InterpolationMode = scale >= 1.0 ? InterpolationMode.NearestNeighbor : InterpolationMode.Bilinear;
            g.PixelOffsetMode   = PixelOffsetMode.Half;
            // 배경 스레드가 프레임을 교체/해제하는 순간과 겹치면 GDI "개체 사용 중" 예외 → 한 프레임 건너뜀.
            try { g.DrawImage(_frame, visible, sx, sy, sw, sh, GraphicsUnit.Pixel); }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            if (_frame != null)
            {
                var dst = DisplayRect();
                DrawFrameFast(g, dst);
                DrawOverlays(g, dst);

                // 검사기별 전용 오버레이(이미지px→화면px 변환 제공). 줌/팬에 따라 정확히 따라간다.
                if (CustomOverlayPaint != null)
                {
                    var fr = _frame;
                    Func<PointF, PointF> toScreen = pp => new PointF(
                        dst.Left + (float)(pp.X * dst.Width  / (double)fr.Width),
                        dst.Top  + (float)(pp.Y * dst.Height / (double)fr.Height));
                    try { CustomOverlayPaint(g, toScreen); } catch { }
                }
            }
            else
            {
                using (var f  = new Font("Consolas", 12F))
                using (var br = new SolidBrush(InfoForeColor))
                {
                    var sz = g.MeasureString("No image", f);
                    g.DrawString("No image", f, br, ClientSize.Width - sz.Width - 12, 8 + TopInset);
                }
            }
            if (!string.IsNullOrEmpty(InfoText))
                using (var br = new SolidBrush(InfoForeColor))
                using (var f  = new Font("Consolas", 9F))
                    g.DrawString(InfoText, f, br, 8, 6 + TopInset);

            if (ShowLiveLabel && _frame != null)
                using (var br = new SolidBrush(InfoForeColor))
                using (var f  = new Font("Consolas", 9F))
                    g.DrawString("Live", f, br, 8, Height - 22);

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
                using (var br = new SolidBrush(Color.Yellow))
                using (var f  = new Font("Segoe UI", 11F, FontStyle.Bold))
                    g.DrawString($"-- Editing {_editKind} ROI: drag a rectangle --", f, br, 10, Height - 26);
            }

            DrawMeasure(g);

            if (!string.IsNullOrEmpty(_verdict))
                using (var f  = new Font("Segoe UI", 26F, FontStyle.Bold))
                using (var br = new SolidBrush(_verdictPass ? Color.LimeGreen : Color.Red))
                {
                    var sz = g.MeasureString(_verdict, f);
                    g.DrawString(_verdict, f, br, ClientSize.Width - sz.Width - 14, 6 + TopInset);
                }

            if (_resultLines != null && _resultLines.Length > 0)
                using (var f = new Font("맑은 고딕", 9F))
                {
                    Color def = _verdictPass ? Color.FromArgb(120, 230, 120) : Color.FromArgb(255, 120, 120);
                    float y = ClientSize.Height - 6 - _resultLines.Length * 17;
                    for (int i = 0; i < _resultLines.Length; i++)
                    {
                        string line = _resultLines[i];
                        if (string.IsNullOrEmpty(line)) { y += 17; continue; }
                        Color c = (_resultLineColors != null && i < _resultLineColors.Length && _resultLineColors[i].A != 0)
                                  ? _resultLineColors[i] : def;
                        var sz = g.MeasureString(line, f);
                        using (var br = new SolidBrush(c))
                            g.DrawString(line, f, br, ClientSize.Width - sz.Width - 10, y);
                        y += 17;
                    }
                }

            using (var p = new Pen(Color.DimGray, 1f))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }

        private void DrawMeasure(Graphics g)
        {
            if (_frame == null) return;
            var dst = DisplayRect();
            if (dst.Width <= 0 || dst.Height <= 0) return;
            double rx = (double)dst.Width / _frame.Width, ry = (double)dst.Height / _frame.Height;

            // 1) 완료된 측정 세그먼트(클리어 전까지 영구 — 측정 모드 해제와 무관하게 표시).
            for (int i = 0; i < _measSegs.Count; i++)
                DrawMeasureSegment(g, dst, rx, ry, _measSegs[i].A, _measSegs[i].B);

            // 2) 진행 중(측정 모드에서 첫 점만 찍힌 상태)인 미리보기.
            if (!_measuring || !_haveA || _haveB) return;
            Point A = new Point(dst.Left + (int)(_measA.X * rx), dst.Top + (int)(_measA.Y * ry));
            using (var br = new SolidBrush(Color.Red))
                g.FillEllipse(br, A.X - 4, A.Y - 4, 8, 8);

            if (_haveMouse)
            {
                bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
                var pm = shift ? ConstrainStraight(_measA, _measMouse) : _measMouse;
                Point M = new Point(dst.Left + (int)(pm.X * rx), dst.Top + (int)(pm.Y * ry));
                using (var pen = new Pen(Color.FromArgb(180, 255, 0, 0), 1.5f) { DashStyle = DashStyle.Dash })
                    g.DrawLine(pen, A, M);
                string pv = FormatMeasureText(pm.X - _measA.X, pm.Y - _measA.Y);
                using (var f = new Font("Consolas", 10F, FontStyle.Bold))
                {
                    var sz = g.MeasureString(pv, f);
                    using (var bg = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
                        g.FillRectangle(bg, M.X + 8, M.Y - 10, sz.Width + 6, sz.Height + 2);
                    using (var br = new SolidBrush(Color.Red))
                        g.DrawString(pv, f, br, M.X + 11, M.Y - 9);
                }
            }
            using (var br = new SolidBrush(Color.Red))
            using (var f = new Font("Consolas", 9F, FontStyle.Bold))
                g.DrawString("둘째 점 클릭 (Shift: 직선)", f, br, A.X + 6, A.Y + 6);
        }

        /// <summary>완료된 측정 1건(두 점 + 직선 + 거리 라벨)을 화면에 그린다.</summary>
        private void DrawMeasureSegment(Graphics g, Rectangle dst, double rx, double ry, PointF a, PointF b)
        {
            Point A = new Point(dst.Left + (int)(a.X * rx), dst.Top + (int)(a.Y * ry));
            Point B = new Point(dst.Left + (int)(b.X * rx), dst.Top + (int)(b.Y * ry));
            using (var pen = new Pen(Color.Red, 2.5f))
                g.DrawLine(pen, A, B);
            using (var br = new SolidBrush(Color.Red))
            {
                g.FillEllipse(br, A.X - 4, A.Y - 4, 8, 8);
                g.FillEllipse(br, B.X - 4, B.Y - 4, 8, 8);
            }
            string txt = FormatMeasureText(b.X - a.X, b.Y - a.Y);
            using (var f = new Font("Consolas", 11F, FontStyle.Bold))
            {
                var sz = g.MeasureString(txt, f);
                using (var bg = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
                    g.FillRectangle(bg, B.X + 8, B.Y - 10, sz.Width + 6, sz.Height + 2);
                using (var br = new SolidBrush(Color.Red))
                    g.DrawString(txt, f, br, B.X + 11, B.Y - 9);
            }
        }

        /// <summary>두 점 간 픽셀 변위(dxpx,dypx)를 거리 문자열로. 스케일 있으면 mm, 없으면 px.</summary>
        private string FormatMeasureText(double dxpx, double dypx)
        {
            if (MmPerPixelX > 0 || MmPerPixelY > 0)
            {
                double mmx = MmPerPixelX > 0 ? MmPerPixelX : MmPerPixelY;
                double mmy = MmPerPixelY > 0 ? MmPerPixelY : MmPerPixelX;
                double dmm = Math.Sqrt(dxpx * mmx * dxpx * mmx + dypx * mmy * dypx * mmy);
                return dmm.ToString("F3") + " mm";
            }
            return Math.Sqrt(dxpx * dxpx + dypx * dypx).ToString("F1") + " px";
        }

        private void DrawOverlays(Graphics g, Rectangle dst)
        {
            if (ShowCrosshair)
                using (var p = new Pen(Color.FromArgb(120, 255, 255, 255), 1f) { DashStyle = DashStyle.Dash })
                {
                    int top = TopInset;
                    int cx = ClientSize.Width / 2;
                    int cy = top + (ClientSize.Height - top) / 2;
                    g.DrawLine(p, 0, cy, ClientSize.Width, cy);
                    g.DrawLine(p, cx, top, cx, ClientSize.Height);
                }

            if (_hasOverlayRoi && _frame != null)
            {
                var sx = (double)dst.Width  / _frame.Width;
                var sy = (double)dst.Height / _frame.Height;
                var rr = new Rectangle(
                    dst.Left + (int)Math.Round(_overlayRoi.X * sx), dst.Top + (int)Math.Round(_overlayRoi.Y * sy),
                    (int)Math.Round(_overlayRoi.Width * sx),        (int)Math.Round(_overlayRoi.Height * sy));
                using (var p = new Pen(Color.Yellow, 1f)) g.DrawRectangle(p, rr);
            }

            if (_overlayMarks != null && _frame != null)
            {
                var sx = (double)dst.Width  / _frame.Width;
                var sy = (double)dst.Height / _frame.Height;
                // 라벨은 줌과 무관하게 고정 화면 크기(확대해도 안 작아짐) + 원본을 덜 가리도록
                // 컴팩트하게: 인덱스 숫자만, 작은 폰트·반투명 배경, 마커도 소형.
                using (var f   = new Font("Consolas", 8.25F, FontStyle.Bold))
                using (var bg  = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
                {
                    int idx = 0;
                    foreach (var m in _overlayMarks)
                    {
                        Color col = (m.Color.A == 0) ? Color.LimeGreen : m.Color;   // 결함=마크색(Red), 매칭=기본 초록
                        using (var p   = new Pen(col, 1.5f))
                        using (var brT = new SolidBrush(col))
                        {
                        int cx = dst.Left + (int)(m.CenterX * sx);
                        int cy = dst.Top  + (int)(m.CenterY * sy);
                        // 매칭 박스(검출 각도로 회전) — 크기 지정 시 표시. 위치+회전 시각화.
                        if (m.BoxW > 0 && m.BoxH > 0)
                        {
                            double rad = m.AngleDeg * System.Math.PI / 180.0;
                            double ca = System.Math.Cos(rad), sa = System.Math.Sin(rad);
                            double hw = m.BoxW / 2.0, hh = m.BoxH / 2.0;
                            double[,] o = { { -hw, -hh }, { hw, -hh }, { hw, hh }, { -hw, hh } };
                            var corners = new PointF[4];
                            for (int k = 0; k < 4; k++)
                            {
                                double ox = o[k, 0], oy = o[k, 1];
                                double ix = m.CenterX + (ox * ca - oy * sa);
                                double iy = m.CenterY + (ox * sa + oy * ca);
                                corners[k] = new PointF((float)(dst.Left + ix * sx), (float)(dst.Top + iy * sy));
                            }
                            g.DrawPolygon(p, corners);
                        }
                        // 소형 십자 마커(원본 가림 최소화).
                        g.DrawLine(p, cx - 6, cy, cx + 6, cy);
                        g.DrawLine(p, cx, cy - 6, cx, cy + 6);

                        // 인덱스 + 각도(°). 점수는 MATCH RESULT 그리드에서 확인. 컴팩트 라벨.
                        string txt = idx.ToString() + " " + m.AngleDeg.ToString("F1") + "°";
                        var ts = g.MeasureString(txt, f);
                        float tx = cx + 7, ty = cy - ts.Height - 1;
                        if (tx + ts.Width + 2 > ClientSize.Width)  tx = cx - ts.Width - 7;
                        if (ty < 0)                                ty = cy + 7;
                        g.FillRectangle(bg, tx - 1, ty, ts.Width + 2, ts.Height);
                        g.DrawString(txt, f, brT, tx, ty);
                        }
                        idx++;
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
