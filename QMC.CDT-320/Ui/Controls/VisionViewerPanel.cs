using QMC.CDT_320.Ui.Localization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Calibration;
using QMC.Common.Ui.Controls;
using QMC.CDT_320.Equipment.Vision;
using QMC.CDT320.VisionComm;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// Vision 뷰어(이미지 스트림) 1개를 표시하는 재사용 패널 — Vision PC 카메라 UI와 동일 구성.
    /// <para>
    /// UI(카메라뷰 + 제목/상태 바)는 Designer(.Designer.cs)에 있고, 여기에는 런타임 로직만 둔다.
    /// 사용 시 <see cref="Configure"/>로 host/포트/명령 채널을 주입한다(생성자는 매개변수 없음 — VS 디자이너 호환).
    /// 툴바 Grab 은 명령 채널로 Vision 에 촬상(EXPOSE)을 보내고(있으면) 결과 프레임을 표시한다(READY 게이트는 Vision 측).
    /// 메타(스케일/판정/결과/마크)는 오버레이로 표시. 수신/표시 전용 — 모션 무관.
    /// </para>
    /// </summary>
    public sealed partial class VisionViewerPanel : UserControl
    {
        private int _port;
        private VisionTcpClient _cmd;   // 툴바 Grab 시 Vision 에 촬상(EXPOSE) 명령. null 이면 수동 수신만.
        private VisionViewerSource _source;
        private bool _useSavedPixelScale;
        private double _savedPixelScaleX;
        private double _savedPixelScaleY;
        private double _savedWidthPixel;
        private double _savedHeightPixel;
        private int _grabImageUiPending;
        private bool _cameraCommandsEnabled = true;
        private bool _reviewVerificationFrameBusy;

        public bool AllowLive { get; set; }

        /// <summary>
        /// Vision 카메라 상태를 바꾸는 Viewer 토글과 Grab/Live 명령의 허용 여부입니다.
        /// false여도 수신된 이미지의 표시와 측정 기능은 계속 사용할 수 있습니다.
        /// </summary>
        public bool CameraCommandsEnabled
        {
            get { return _cameraCommandsEnabled; }
            set
            {
                _cameraCommandsEnabled = value;
                if (_cam != null)
                    _cam.CameraCommandsEnabled = value;
                if (_chkViewer != null)
                    _chkViewer.Enabled = value && _port > 0;
            }
        }

        public bool IsLive
        {
            get
            {
                return (_cam != null && _cam.IsLive) ||
                       (_chkViewer != null && _chkViewer.Checked && AllowLive);
            }
        }

        public VisionViewerPanel()
        {
            InitializeComponent();
            InitializeLanguageBindings();
        }

        /// <summary>편의 생성자 — UI 는 Designer(InitializeComponent)가 만들고, 런타임 인자는 즉시 Configure 한다. (기존 호출부 호환)</summary>
        public VisionViewerPanel(string host, int viewerPort, string title, VisionTcpClient commandClient = null) : this()
        {
            Configure(host, viewerPort, title, commandClient);
        }

        /// <summary>런타임 주입 — 뷰어 포트/제목/명령 채널 설정 후 소스 연결.</summary>
        public void Configure(string host, int viewerPort, string title, VisionTcpClient commandClient)
        {
            string h = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim();
            _port = viewerPort;
            _cmd = commandClient;
            if (string.IsNullOrWhiteSpace(title))
                Lang.BindFormat(_lblTitle, viewerPort > 0 ? "visionUi.viewer.defaultWithPort" : "visionUi.viewer.defaultNoPort", viewerPort);
            else
                Lang.BindFormat(_lblTitle, viewerPort > 0 ? "visionUi.viewer.titleWithPort" : "visionUi.viewer.titleNoPort", title, viewerPort);

            StopLive();
            WaitForCameraOperationsAsync().GetAwaiter().GetResult();
            try { if (_source != null) { _source.FrameMeta -= OnMeta; _source.Status -= OnStatus; _source.Dispose(); _source = null; } } catch { }

            if (_port > 0)
            {
                _source = new VisionViewerSource(h, _port, 2000, commandClient)
                {
                    LiveEnabled = AllowLive
                };
                _source.FrameMeta += OnMeta;
                _source.Status += OnStatus;
                _cam.AttachSource(_source);   // 툴바 Grab/Live/Stop이 이 소스를 제어(접속·촬상은 누를 때).
                _cam.ShowLiveLabel = false;
                {
                    if (AllowLive)
                        Lang.BindKey(_lblStat, "visionUi.visionViewerPanel._lblStat.text");
                    else
                        Lang.BindKey(_lblStat, "visionUi.visionViewerPanel._lblStat.state2");
                }
                RefreshSavedPixelScale();
                SetViewerToggle(false);       // 재구성 시 토글은 OFF(라이브 미시작)로 초기화
                _chkViewer.Enabled = _cameraCommandsEnabled;
                _cam.CameraCommandsEnabled = _cameraCommandsEnabled;
                if (!AllowLive)
                    StartGrabImageView();
            }
            else
            {
                Lang.BindKey(_lblStat, "visionUi.visionViewerPanel._lblStat.state3");
                SetViewerToggle(false);
                _chkViewer.Enabled = false;
            }
        }

        /// <summary>기존 호출 호환용. 명시적으로 허용된 화면에서만 Vision Live를 시작하고, 나머지는 Grab 이미지만 수신한다.</summary>
        public void StartLive()
        {
            if (AllowLive)
            {
                StartVisionLiveView();
                return;
            }

            StartGrabImageView();
        }

        /// <summary>기존 호출 호환용. Grab 이미지 수신을 정지한다.</summary>
        public void StopLive()
        {
            // CameraView 툴바의 Start/Grab이 Worker Queue에서 진행 중일 수 있으므로
            // 동일 Queue 뒤에 Stop을 등록하고, 직접 Viewer 토글로 시작한 수신도 즉시 정지합니다.
            try { if (_cam != null) _cam.StopLive(); } catch { }
            StopGrabImageView();
        }

        public System.Threading.Tasks.Task WaitForCameraOperationsAsync()
        {
            return _cam != null
                ? _cam.WaitForCameraOperationsAsync()
                : System.Threading.Tasks.Task.CompletedTask;
        }

        /// <summary>
        /// Review 안전 Scope와 정지한 XYT를 호출자가 유지한 상태에서 별도 EXPOSE 영상을 표시한다.
        /// 반환값은 표시 영수증이며 MATCH 요청 ID 또는 물리 Die 식별자가 아니다.
        /// </summary>
        public async System.Threading.Tasks.Task<string> ShowReviewVerificationFrameAsync(
            string host, int port, VisionTcpClient commandClient, CancellationToken token)
        {
            if (IsDisposed || Disposing || !IsHandleCreated || InvokeRequired || _cam == null || _cam.IsDisposed)
                throw new InvalidOperationException("검증 영상은 활성 Viewer의 UI 스레드에서 요청해야 합니다.");
            if (_reviewVerificationFrameBusy)
                throw new InvalidOperationException("이전 검증 영상 요청이 아직 끝나지 않았습니다.");
            if (IsLive || commandClient == null || !commandClient.IsConnected ||
                string.IsNullOrWhiteSpace(host) || port <= 0 || port > 65535 || port != _port)
                throw new InvalidOperationException("Live 정지와 현재 Viewer 포트/명령 연결을 확인한 뒤 검증 영상을 요청하세요.");

            token.ThrowIfCancellationRequested();
            VisionViewerSource expectedSource = _source;
            Bitmap frame = null;
            VisionFrameMeta metadata = null;
            string sourceStatus = string.Empty;
            _reviewVerificationFrameBusy = true;
            try
            {
                await WaitForCameraOperationsAsync().ConfigureAwait(true);
                token.ThrowIfCancellationRequested();
                if (IsDisposed || Disposing || IsLive || !ReferenceEquals(expectedSource, _source) ||
                    !commandClient.IsConnected)
                    throw new InvalidOperationException("검증 영상 요청 전에 Viewer 상태가 변경되었습니다.");

                // 기존 EXPOSE ACK/거부 및 단발 수신 경로를 사용한다. null 명령 채널의 캐시 영상은 사용하지 않는다.
                // STOP이 와도 짧은 촬상/수신 작업이 종료된 뒤 반환하여 호출자의 안전 Scope가 먼저 풀리지 않게 한다.
                using (var source = new VisionViewerSource(host.Trim(), port, 2000, commandClient))
                {
                    source.FrameMeta += value => metadata = value;
                    source.Status += value => sourceStatus = value;
                    frame = await System.Threading.Tasks.Task.Run(() => source.GrabFrame()).ConfigureAwait(true);
                }

                token.ThrowIfCancellationRequested();
                if (IsDisposed || Disposing || !IsHandleCreated || InvokeRequired || _cam.IsDisposed || IsLive ||
                    !ReferenceEquals(expectedSource, _source) || port != _port || !commandClient.IsConnected)
                    throw new InvalidOperationException("검증 영상 수신 중 Viewer 상태가 변경되어 영상을 폐기했습니다.");
                if (frame == null || frame.Width <= 0 || frame.Height <= 0)
                    throw new InvalidOperationException("검증용 새 촬상 영상을 표시할 수 없습니다. " + sourceStatus);

                string receipt = "EXPOSE-DISPLAY:" + Guid.NewGuid().ToString("N") +
                    "; UTC=" + DateTime.UtcNow.ToString("O") +
                    "; Image=" + frame.Width + "x" + frame.Height + "; Port=" + port;
                // 누적 평균이 켜져 있어도 이전 검증점 영상이 섞이지 않게 현재 프레임부터 새 버퍼를 시작한다.
                _cam.SetVerificationImage(frame);
                if (metadata != null)
                    ApplyMeta(metadata);
                // 별도 EXPOSE와 이전 MATCH의 대응은 보장되지 않으므로 과거 판정/마크를 식별 근거처럼 표시하지 않는다.
                _cam.SetResultLines(new string[0]);
                _cam.SetOverlay(RectangleF.Empty, null);
                _cam.SetVerdict(string.Empty, false);
                Lang.BindFormat(_lblStat, "visionUi.visionViewerPanel._lblStat.state4", (object)(frame.Width), (object)(frame.Height));
                _cam.Refresh();
                return receipt;
            }
            finally
            {
                if (frame != null)
                    frame.Dispose();
                _reviewVerificationFrameBusy = false;
            }
        }

        private void StartGrabImageView()
        {
            if (_port <= 0)
                return;

            try
            {
                if (_source == null)
                    return;

                _source.StartGrabImageStream(OnGrabImageFrame);
                SetViewerToggle(true);
                Lang.BindKey(_lblStat, "visionUi.visionViewerPanel._lblStat.state5");
            }
            catch (Exception ex)
            {
                SetViewerToggle(false);
                LogGrabImageViewFailed("Grab 이미지 수신 시작 실패: " + ex.Message);
            }
        }

        private void StartVisionLiveView()
        {
            if (_port <= 0)
                return;

            try
            {
                if (_source == null)
                    return;

                _source.StartLive(OnGrabImageFrame);
                SetViewerToggle(true);
                Lang.BindKey(_lblStat, "visionUi.visionViewerPanel._lblStat.state6");
            }
            catch (Exception ex)
            {
                SetViewerToggle(false);
                LogLiveBlocked("Vision Live 시작 실패: " + ex.Message);
            }
        }

        private void StopGrabImageView()
        {
            try { if (_source != null) _source.StopLive(); } catch { }
            SetViewerToggle(false);
        }

        private void OnGrabImageFrame(Bitmap bmp)
        {
            if (bmp == null)
                return;

            if (Interlocked.CompareExchange(ref _grabImageUiPending, 1, 0) != 0)
            {
                try { bmp.Dispose(); } catch { }
                return;
            }

            try
            {
                if (IsDisposed || !IsHandleCreated)
                {
                    try { bmp.Dispose(); } catch { }
                    Interlocked.Exchange(ref _grabImageUiPending, 0);
                    return;
                }

                BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (!IsDisposed)
                            _cam.SetImage(bmp);
                    }
                    finally
                    {
                        try { bmp.Dispose(); } catch { }
                        Interlocked.Exchange(ref _grabImageUiPending, 0);
                    }
                }));
            }
            catch
            {
                try { bmp.Dispose(); } catch { }
                Interlocked.Exchange(ref _grabImageUiPending, 0);
            }
        }

        // ── 뷰어 ON/OFF 토글 ──
        // 일반 카메라 뷰는 Vision Live 명령을 보내지 않고 Grab 이미지 수신만 사용한다.
        private void chkViewer_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_cameraCommandsEnabled)
                {
                    SetViewerToggle(false);
                    return;
                }
                if (_port <= 0) { Lang.BindKey(_chkViewer, "visionUi.visionViewerPanel._chkViewer.state3"); return; }

                if (_chkViewer.Checked)
                {
                    StartLive();
                }
                else
                {
                    StopGrabImageView();
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "VISION",
                    "ViewerToggle", "뷰어 토글 처리 실패: " + ex.Message);
                QMC.Common.MessageDialog.Show(
                    Lang.Format("visionUi.visionViewerPanel.message.text", (object)(ex.Message)),
                    Lang.T("visionUi.visionViewerPanel.message.state2"),
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }

        /// <summary>토글 체크/텍스트를 코드에서 설정(이벤트 재진입 방지). 실제 Start/Stop 은 호출측 책임.</summary>
        private void SetViewerToggle(bool on)
        {
            if (IsDisposed || _chkViewer == null || _chkViewer.IsDisposed) return;
            try
            {
                _chkViewer.CheckedChanged -= chkViewer_CheckedChanged;
                _chkViewer.Checked = on;
                string mode = AllowLive ? "LIVE VIEW" : "GRAB VIEW";
                {
                    if (on)
                        Lang.BindFormat(_chkViewer, "visionUi.visionViewerPanel._chkViewer.text", (object)(mode));
                    else
                        Lang.BindFormat(_chkViewer, "visionUi.visionViewerPanel._chkViewer.state2", (object)(mode));
                }
            }
            finally
            {
                _chkViewer.CheckedChanged += chkViewer_CheckedChanged;
            }
        }

        private void LogGrabImageViewFailed(string reason)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    "VISION",
                    "VISION-GRAB-VIEW",
                    "Vision Grab 이미지 수신 처리 실패. viewerPort=" + _port +
                    ", reason=" + reason);
            }
            catch { }
        }

        private void LogLiveBlocked(string reason)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    "VISION",
                    "VISION-LIVE-START",
                    "Vision 뷰어 Live 시작에 실패했습니다. viewerPort=" + _port +
                    ", reason=" + reason);
            }
            catch { }
        }

        /// <summary>결과 라인(우측하단 텍스트) 오버레이 — 내부 카메라뷰로 위임.</summary>
        public void SetResultLines(string[] lines) { if (IsDisposed) return; try { _cam.SetResultLines(lines); } catch { } }
        /// <summary>판정(OK/NG, 우측상단) 오버레이 — 내부 카메라뷰로 위임.</summary>
        public void SetVerdictText(string text, bool pass) { if (IsDisposed) return; try { _cam.SetVerdict(text, pass); } catch { } }

        public void RefreshSavedPixelScale()
        {
            try
            {
                VisionCameraPixelCalibration camera = ResolveSavedPixelCalibration();
                if (camera == null || camera.PixelToMmX == 0 || camera.PixelToMmY == 0)
                {
                    _useSavedPixelScale = false;
                    return;
                }

                _savedPixelScaleX = camera.PixelToMmX;
                _savedPixelScaleY = camera.PixelToMmY;
                _savedWidthPixel = camera.ImageWidthPixel;
                _savedHeightPixel = camera.ImageHeightPixel;
                _useSavedPixelScale = true;
                _cam.MmPerPixelX = _savedPixelScaleX;
                _cam.MmPerPixelY = _savedPixelScaleY;
            }
            catch
            {
                _useSavedPixelScale = false;
            }
        }

        // ── 소스 상태 메시지(촬상 OK / READY 거부 등) → 상태줄 ──
        private void OnStatus(string s)
        {
            if (IsDisposed || !IsHandleCreated || string.IsNullOrEmpty(s)) return;
            try { BeginInvoke(new Action(() => { Lang.BindFormat(_lblStat, "visionUi.literal", (object)(s)); })); } catch { }
        }

        // 백그라운드 스레드(메타) → UI 마샬링하여 오버레이 반영
        private void OnMeta(VisionFrameMeta meta)
        {
            if (meta == null) return;
            if (IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke(new Action(() => ApplyMeta(meta))); } catch { }
        }

        private void ApplyMeta(VisionFrameMeta meta)
        {
            try
            {
                // 저장 스케일은 보정 당시 해상도(_savedWidth/HeightPixel) 기준 mm/px 다.
                // 뷰어 표시 이미지는 다운스케일될 수 있으므로 실제 표시폭(meta.Width/Height)으로 정규화해야
                // Vision 측 측정값과 일치한다. 저장 스케일이 없으면 meta.ScaleX(이미 다운스케일 보정됨)를 쓴다.
                double scaleFactor = AppSettingsStore.Current != null ? AppSettingsStore.Current.ViewerMeasureScaleFactor : 1.0;
                double scaleX = _useSavedPixelScale
                    ? EffectiveSavedScale(_savedPixelScaleX, _savedWidthPixel, meta.Width, scaleFactor)
                    : meta.ScaleX;
                double scaleY = _useSavedPixelScale
                    ? EffectiveSavedScale(_savedPixelScaleY, _savedHeightPixel, meta.Height, scaleFactor)
                    : meta.ScaleY;
                _cam.MmPerPixelX = scaleX;
                _cam.MmPerPixelY = scaleY;

                int width = _useSavedPixelScale && _savedWidthPixel > 0 ? (int)Math.Round(_savedWidthPixel) : meta.Width;
                int height = _useSavedPixelScale && _savedHeightPixel > 0 ? (int)Math.Round(_savedHeightPixel) : meta.Height;
                _cam.InfoText = (meta.Module ?? "") + "\r\nW:" + width + " H:" + height;
                _cam.SetVerdict(meta.Verdict, meta.VerdictPass);
                _cam.SetResultLines(meta.ResultLines);
                _cam.SetOverlay(RoiOf(meta), MarksOf(meta));
            }
            catch { }
        }

        /// <summary>표시용 mm/px 산출 — 저장 스케일 × factor(다운스케일 계수).
        /// factor>0: 수동 계수(표시 mm/px = savedMmPerPx × factor, 예 5120→1600 이면 3.2).
        /// factor≤0: 자동 폴백(표시폭 displayPixel 기준 = savedMmPerPx × savedPixel / displayPixel). 정보 부족 시 저장값 그대로.</summary>
        private static double EffectiveSavedScale(double savedMmPerPx, double savedPixel, double displayPixel, double factor)
        {
            if (savedMmPerPx <= 0) return 0.0;
            if (factor > 0) return savedMmPerPx * factor;
            if (savedPixel > 0 && displayPixel > 0)
                return savedMmPerPx * savedPixel / displayPixel;
            return savedMmPerPx;
        }

        private VisionCameraPixelCalibration ResolveSavedPixelCalibration()
        {
            VisionCameraCalibrationData data = ResolveMachineCameraCalibration();
            if (data == null)
                return null;

            data.EnsureObjects();
            return ResolveSavedPixelCalibration(data);
        }

        private VisionCameraCalibrationData ResolveMachineCameraCalibration()
        {
            Form1 host = FindHostForm();
            if (host == null ||
                host.Machine == null ||
                host.Machine.VisionUnit == null ||
                host.Machine.VisionUnit.Config == null ||
                host.Machine.VisionUnit.Config.CalibrationData == null)
                return null;

            VisionCameraCalibrationData data = host.Machine.VisionUnit.Config.CalibrationData.Camera;
            data.EnsureObjects();
            return data;
        }

        private VisionCameraPixelCalibration ResolveSavedPixelCalibration(VisionCameraCalibrationData data)
        {
            if (_port == VisionViewerPorts.BottomInspection)
                return data.BottomCamera;
            if (_port == VisionViewerPorts.Wafer)
                return data.InputCamera;
            if (_port == VisionViewerPorts.Bin)
                return data.OutputCamera;
            if (_port == VisionViewerPorts.FrontSideVision)
                return data.FrontSideCamera;
            if (_port == VisionViewerPorts.RearSideVision)
                return data.RearSideCamera;

            return null;
        }

        private Form1 FindHostForm()
        {
            Control current = this;
            while (current != null)
            {
                Form1 host = current as Form1;
                if (host != null)
                    return host;
                current = current.Parent;
            }

            foreach (Form form in Application.OpenForms)
            {
                Form1 host = form as Form1;
                if (host != null)
                    return host;
            }

            return null;
        }

        /// <summary>메타의 ROI(검색/검사 영역, 이미지 좌표 top-left) → RectangleF. 없으면 Empty.</summary>
        private static System.Drawing.RectangleF RoiOf(VisionFrameMeta meta)
        {
            if (meta == null || meta.RoiW <= 0 || meta.RoiH <= 0) return System.Drawing.RectangleF.Empty;
            return new System.Drawing.RectangleF((float)meta.RoiX, (float)meta.RoiY, (float)meta.RoiW, (float)meta.RoiH);
        }

        private static List<OverlayMark> MarksOf(VisionFrameMeta meta)
        {
            if (meta?.Marks == null || meta.Marks.Length == 0) return null;
            var list = new List<OverlayMark>(meta.Marks.Length);
            // 각도/박스 크기까지 전달 → 핸들러 뷰어가 회전 박스(매칭/측면 결함)를 그린다.
            foreach (var m in meta.Marks) list.Add(new OverlayMark(m.X, m.Y, m.Score, m.Angle, m.BoxW, m.BoxH));
            return list;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { StopLive(); } catch { }
            try { if (_source != null) { _source.FrameMeta -= OnMeta; _source.Status -= OnStatus; _source.Dispose(); _source = null; } } catch { }
            base.OnHandleDestroyed(e);
        }
        // Keep Designer serialization declarative; register display resources after controls exist.
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(this._chkViewer, "visionUi.visionViewerPanel._chkViewer.state3");
            Lang.BindKey(this._lblTitle, "visionUi.visionViewerPanel._lblTitle.text");
            Lang.BindKey(this._lblStat, "visionUi.visionMonitorControl.lblStatus.state10");
        }
    }
}
