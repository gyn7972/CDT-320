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

        public bool AllowLive { get; set; }

        public VisionViewerPanel()
        {
            InitializeComponent();
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
            _lblTitle.Text = (string.IsNullOrWhiteSpace(title) ? "이미지" : title) +
                             (viewerPort > 0 ? "  (뷰어 " + viewerPort + ")" : "  (뷰어 없음)");

            StopLive();
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
                _lblStat.Text = AllowLive ? "대기 — Vision Live/Grab 준비" : "대기 — Grab 이미지 수신 준비";
                RefreshSavedPixelScale();
                SetViewerToggle(false);       // 재구성 시 토글은 OFF(라이브 미시작)로 초기화
                _chkViewer.Enabled = true;
                if (!AllowLive)
                    StartGrabImageView();
            }
            else
            {
                _lblStat.Text = "뷰어 포트 없음";
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
            StopGrabImageView();
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
                _lblStat.Text = "Grab 이미지 수신 중";
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
                _lblStat.Text = "Vision Live 수신 중";
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
                if (_port <= 0) { _chkViewer.Text = "뷰어 OFF"; return; }

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
                    "뷰어 전환에 실패했습니다.\r\n" + ex.Message,
                    "뷰어 ON/OFF",
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
                _chkViewer.Text = on ? mode + " ON" : mode + " OFF";
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
            try { BeginInvoke(new Action(() => { _lblStat.Text = s; })); } catch { }
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
    }
}
