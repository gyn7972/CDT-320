using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using QMC.CDT320.Bin;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// Wafer Align/Die Mapping 완료 후 작업자가 맵과 픽업 시작 조건을 검토하는 화면입니다.
    /// 이 Form은 장비를 직접 구동하지 않고, 화면에서 발생한 요청을 이벤트로 전달합니다.
    /// </summary>
    public sealed partial class InputStageRunReviewDialog : Form
    {
        private readonly Dictionary<DieMapEntry, int> _previewSequence =
            new Dictionary<DieMapEntry, int>();
        private readonly List<DieMapEntry> _baseOrder = new List<DieMapEntry>();
        private readonly List<DieMapEntry> _previewOrder = new List<DieMapEntry>();
        private readonly List<DieMapEntry> _selectedDies = new List<DieMapEntry>();
        private DieMap _dieMap;
        private DieMapEntry _selectedDie;
        private DieMapEntry _startDie;
        private InputStageRunReviewMode _mode;
        private bool _busy;
        private bool _synchronizingSelection;
        private bool _pickupOrderApplied;
        private bool _alignComplete;
        private bool _mappingComplete;
        private bool _reviewValid;
        private bool _manualFallbackThetaRequired;
        private bool _manualFallbackThetaDone;
        private bool _readOnlyPreview;
        private bool _autoReviewMode;
        private bool _decisionSubmitted;
        private bool _sequenceCloseRequested;
        private DialogResult _submittedDialogResult = DialogResult.None;
        private string _waferId = string.Empty;
        private string _mappingRevision = string.Empty;
        private string _lastDieGridSignature = string.Empty;
        private bool _dieGridRebindRequired;
        private Button _activeJogButton;
        private Timer _encoderRefreshTimer;
        private Func<double[]> _axisPositionProvider;
        private ComboBox _cmbJogMode;
        private ComboBox _cmbJogStep;
        private bool _waferVisionControlActive;
        // Wafer Vision 명령 채널 연결 상태. Live/Grab은 이 값이 true일 때만 실제로 동작하므로
        // 버튼 활성 조건에 포함한다(미연결에서 눌리면 거짓 Live 상태가 남는다).
        private bool _waferVisionLinkConnected;
        private bool _waferVisionMoveBusy;
        private string _waferVisionHost = "127.0.0.1";
        private int _waferVisionPort;
        private VisionTcpClient _waferVisionCommandClient;

        public InputStageRunReviewDialog()
        {
            InitializeComponent();
            InitializeJogModeControls();
            mapView.EmptyAreaClicked += MapView_EmptyAreaClicked;
            ConfigureMapView();
            SetMode(InputStageRunReviewMode.MappingReview);
            SetWorkflowState("-", "-", false, false, "-", false, "REVIEW REQUIRED");
            SetAxisPositions(0.0, 0.0, 0.0);
            SetStatus("Wafer Align / Die Mapping 결과를 불러오는 중입니다.");

            if (System.ComponentModel.LicenseManager.UsageMode !=
                System.ComponentModel.LicenseUsageMode.Designtime)
            {
                // Review가 안전 Scope를 확보하기 전에는 Vision 명령 채널을 연결하지 않고,
                // Grab 결과 영상 수신과 화면 측정만 가능한 상태로 구성합니다.
                ConfigureWaferVision(
                    VisionHub.Host,
                    VisionViewerPorts.Wafer,
                    VisionHub.Wafer);
            }

            _encoderRefreshTimer = new Timer();
            _encoderRefreshTimer.Interval = 200;
            _encoderRefreshTimer.Tick += EncoderRefreshTimer_Tick;
        }

        /// <summary>
        /// Jog Mode(Continuous/Step)와 Step 거리 선택 콤보를 Jog 레이아웃 첫 행에 추가합니다.
        /// Designer 구조 변경 없이 코드에서만 배치합니다.
        /// </summary>
        private void InitializeJogModeControls()
        {
            _cmbJogMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            _cmbJogMode.Items.AddRange(new object[] { "Continuous", "Step" });
            // 기본 선택(팀장님 지시 2026-08-18): Step
            _cmbJogMode.SelectedIndex = 1;
            _cmbJogMode.SelectedIndexChanged += delegate { UpdateActionAvailability(); };

            _cmbJogStep = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            _cmbJogStep.Items.AddRange(new object[] { "0.001", "0.005", "0.01", "0.05", "0.1", "0.5", "1.0" });
            // 기본 선택(팀장님 지시 2026-08-18): 1.0
            _cmbJogStep.SelectedIndex = 6;

            // 기본 선택(팀장님 지시 2026-08-18): Coarse(코스).
            // Designer에는 기본 선택이 없어 비어 있었고, 그 상태의 실제 적용값은 Fine이었다.
            if (cmbJogSpeed != null && cmbJogSpeed.Items.Count > 0)
                cmbJogSpeed.SelectedItem = "Coarse";

            jogLayout.SetColumnSpan(cmbJogSpeed, 1);
            jogLayout.Controls.Add(_cmbJogMode, 2, 0);
            jogLayout.Controls.Add(_cmbJogStep, 3, 0);
        }

        private bool IsStepJogMode
        {
            get { return _cmbJogMode != null && string.Equals(_cmbJogMode.Text, "Step", StringComparison.OrdinalIgnoreCase); }
        }

        private double SelectedJogStepDistance
        {
            get
            {
                double value;
                if (_cmbJogStep != null &&
                    double.TryParse(_cmbJogStep.Text, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out value) &&
                    value > 0.0)
                    return value;
                return 0.01;
            }
        }

        /// <summary>
        /// 실시간 Encoder 표시용 위치 provider를 설정합니다.
        /// provider는 UI thread에서 호출되므로 반드시 캐시된 snapshot만 사용해야 하며
        /// [VisionX, WaferY, WaferT] 순서의 배열 또는 null을 반환합니다.
        /// </summary>
        public void SetAxisPositionProvider(Func<double[]> provider)
        {
            _axisPositionProvider = provider;
            UpdateEncoderRefreshTimerState();
        }

        private void EncoderRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (IsDisposed || !Visible)
                return;

            Func<double[]> provider = _axisPositionProvider;
            if (provider == null)
                return;

            try
            {
                double[] positions = provider();
                if (positions != null && positions.Length >= 3 &&
                    !double.IsNaN(positions[0]) && !double.IsInfinity(positions[0]) &&
                    !double.IsNaN(positions[1]) && !double.IsInfinity(positions[1]) &&
                    !double.IsNaN(positions[2]) && !double.IsInfinity(positions[2]))
                {
                    SetAxisPositions(positions[0], positions[1], positions[2]);
                }
            }
            catch
            {
                // 표시 전용 경로: 실패해도 마지막 표시값을 유지한다.
            }
        }

        private void UpdateEncoderRefreshTimerState()
        {
            if (_encoderRefreshTimer == null || IsDisposed)
                return;
            bool shouldRun = Visible && _axisPositionProvider != null;
            if (shouldRun && !_encoderRefreshTimer.Enabled)
                _encoderRefreshTimer.Start();
            else if (!shouldRun && _encoderRefreshTimer.Enabled)
                _encoderRefreshTimer.Stop();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateEncoderRefreshTimerState();
        }

        public event EventHandler AlignRetryRequested;
        public event EventHandler MappingRetryRequested;
        public event EventHandler MappingSetupRequested;
        public event EventHandler VisionTestRequested;
        public event EventHandler WaferVisionControlStartRequested;
        public event EventHandler WaferVisionControlStopRequested;
        public event EventHandler ThetaCorrectionRequested;
        public event EventHandler DieDetectionRequested;
        public event EventHandler OffsetApplyRequested;
        public event EventHandler SelectedDieMoveRequested;
        public event EventHandler StartRunRequested;
        public event EventHandler AbortAutoRequested;
        public event EventHandler BuzzerStopRequested;
        public event EventHandler JogStopRequested;
        public event EventHandler ReviewActionStopRequested;
        public event EventHandler<InputStageReviewJogEventArgs> JogRequested;
        public event EventHandler<InputStageReviewDieStateEventArgs> DieStateApplyRequested;
        public event EventHandler<InputStageReviewPickupOrderEventArgs> PickupOrderApplyRequested;

        public DieMapEntry SelectedDie { get { return _selectedDie; } }
        public IReadOnlyList<DieMapEntry> SelectedDies { get { return _selectedDies.AsReadOnly(); } }
        public DieMapEntry StartDie { get { return chkUseSelectedStart.Checked ? _startDie : null; } }
        public IReadOnlyList<DieMapEntry> PreviewOrder { get { return _previewOrder.AsReadOnly(); } }
        public string WaferId { get { return _waferId; } }
        public string MappingRevision { get { return _mappingRevision; } }
        public string StartDieUid
        {
            get
            {
                DieMapEntry start = StartDie;
                return start != null ? start.DieUid ?? string.Empty : string.Empty;
            }
        }
        public IReadOnlyList<string> OrderedDieIds
        {
            get
            {
                return _previewOrder
                    .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.DieUid))
                    .Select(entry => entry.DieUid)
                    .ToList()
                    .AsReadOnly();
            }
        }
        public IReadOnlyList<string> OrderedDieUids { get { return OrderedDieIds; } }
        public DieMap DraftDieMap { get { return _dieMap; } }
        public PickupSubset ReviewPickupOptions { get { return BuildPickupOptions(); } }
        public bool IsWaferVisionControlActive { get { return _waferVisionControlActive; } }

        /// <summary>
        /// Review 화면의 Wafer 영상 연결 정보를 설정합니다.
        /// 이 시점에는 명령 채널을 Viewer에 전달하지 않아 Live/Grab이 장비 상태를 바꾸지 않습니다.
        /// </summary>
        public void ConfigureWaferVision(
            string host,
            int viewerPort,
            VisionTcpClient commandClient)
        {
            _waferVisionHost = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim();
            _waferVisionPort = viewerPort;
            _waferVisionCommandClient = commandClient;
            ConfigureWaferVisionViewer(false);
        }

        /// <summary>
        /// Form1이 기존 Review Work Scope를 확보하거나 반환한 뒤 호출하는 상태 반영 함수입니다.
        /// 실제 Live/Grab 명령은 Scope가 살아 있는 active 상태에서만 허용합니다.
        /// </summary>
        public bool SetWaferVisionControlActive(bool active, string status)
        {
            if (IsDisposed || Disposing)
                return false;

            _waferVisionControlActive = active && !_readOnlyPreview;
            if (!_waferVisionControlActive)
                _waferVisionMoveBusy = false;
            if (!ConfigureWaferVisionViewer(_waferVisionControlActive))
            {
                _waferVisionControlActive = false;
                _waferVisionMoveBusy = false;
                UpdateActionAvailability();
                return false;
            }
            ApplyWaferVisionStateLabel();
            btnWaferVisionControl.Text = _waferVisionControlActive
                ? "비전 사용 종료"
                : "비전 사용 시작";
            if (!string.IsNullOrWhiteSpace(status))
                SetStatus(status);
            UpdateActionAvailability();
            return true;
        }

        /// <summary>
        /// Wafer Vision Live 영상을 시작합니다(안전 Scope는 이미 확보된 상태여야 합니다).
        /// 화면 오픈 자동 Live 및 DIE DETECTION 후 Live 복귀에서 사용합니다.
        /// 명령 채널이 미연결이거나 Scope가 없으면 아무 동작도 하지 않고 false를 반환합니다.
        /// </summary>
        public bool StartWaferVisionLive()
        {
            try
            {
                if (IsDisposed || Disposing ||
                    waferVisionViewer == null || waferVisionViewer.IsDisposed)
                    return false;
                if (!_waferVisionControlActive || !_waferVisionLinkConnected || _readOnlyPreview || _decisionSubmitted)
                    return false;
                if (waferVisionViewer.IsLive)
                    return true;

                waferVisionViewer.StartLive();
                ApplyWaferVisionStateLabel();
                return waferVisionViewer.IsLive;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>현재 Wafer Vision Live 수신 중인지 여부입니다.</summary>
        public bool IsWaferVisionLive
        {
            get
            {
                try
                {
                    return waferVisionViewer != null &&
                           !waferVisionViewer.IsDisposed &&
                           waferVisionViewer.IsLive;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// DIE DETECTION처럼 Grab을 직접 사용하는 동작 전에 Live만 잠시 멈춥니다.
        /// 안전 Scope는 유지하므로(반환하지 않음) 동작 후 <see cref="StartWaferVisionLive"/>로 즉시 복귀할 수 있습니다.
        /// 반환값은 "멈추기 전에 Live 중이었는지"이며, 호출자는 이 값이 true일 때만 복귀시킵니다.
        /// </summary>
        public bool PauseWaferVisionLiveForAction()
        {
            try
            {
                if (IsDisposed || Disposing ||
                    waferVisionViewer == null || waferVisionViewer.IsDisposed)
                    return false;
                if (!waferVisionViewer.IsLive)
                    return false;

                waferVisionViewer.StopLive();
                // Grab/Live는 Worker Queue에서 실행되므로 완료를 확인한 뒤 검출 Grab을 시작해야 합니다.
                waferVisionViewer.WaitForCameraOperationsAsync().GetAwaiter().GetResult();
                ApplyWaferVisionStateLabel();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sequence 종료/STOP에서는 Viewer 재구성 없이 즉시 CAM_SWITCH OFF와 수신 Thread 정지만 수행합니다.
        /// 안전 Scope는 호출한 Form1이 이 함수 실행 후 반환합니다.
        /// </summary>
        public void StopWaferVision()
        {
            try
            {
                waferVisionViewer.CameraCommandsEnabled = false;
                waferVisionViewer.StopLive();
                // Toolbar Grab은 Worker Queue에서 실행되므로 완료를 확인한 뒤 Review Lease를 반환해야 합니다.
                waferVisionViewer.WaitForCameraOperationsAsync().GetAwaiter().GetResult();
            }
            catch { }
            if (lblWaferVisionState != null && !lblWaferVisionState.IsDisposed)
            {
                lblWaferVisionState.Text = "비전 영상 정지";
                lblWaferVisionState.ForeColor = Color.DimGray;
            }
        }

        private bool ConfigureWaferVisionViewer(bool commandMode)
        {
            if (waferVisionViewer == null || waferVisionViewer.IsDisposed)
                return false;

            try
            {
                // AllowLive는 VisionViewerSource 생성 전에 적용해야 실제 CAM_SWITCH Live 지원 여부가 반영됩니다.
                waferVisionViewer.StopLive();
                waferVisionViewer.CameraCommandsEnabled = false;
                waferVisionViewer.AllowLive = commandMode;
                waferVisionViewer.Configure(
                    _waferVisionHost,
                    _waferVisionPort,
                    "Wafer Image",
                    commandMode ? _waferVisionCommandClient : null);
                waferVisionViewer.CameraCommandsEnabled =
                    commandMode && !_readOnlyPreview && !_decisionSubmitted;
                return true;
            }
            catch (Exception ex)
            {
                waferVisionViewer.CameraCommandsEnabled = false;
                lblWaferVisionState.Text = "Wafer Vision 연결 실패: " + ex.Message;
                lblWaferVisionState.ForeColor = Color.Firebrick;
                return false;
            }
        }

        public void SetMode(InputStageRunReviewMode mode)
        {
            _mode = mode;
            lblDialogMode.Text = mode == InputStageRunReviewMode.AlignRecovery
                ? "ALIGN RECOVERY"
                : "MAPPING REVIEW";
            lblDialogMode.BackColor = mode == InputStageRunReviewMode.AlignRecovery
                ? Color.FromArgb(192, 80, 64)
                : Color.FromArgb(38, 113, 82);
            UpdateActionAvailability();
        }

        public void SetDieMap(DieMap map)
        {
            _dieMap = map;
            _selectedDie = null;
            _startDie = null;
            _selectedDies.Clear();
            _pickupOrderApplied = false;
            // 새 Map 객체를 받으면 표시값이 같아도 Grid Row.Tag를 새 Entry 객체로 다시 연결해야 한다.
            _lastDieGridSignature = string.Empty;
            _dieGridRebindRequired = true;
            mapView.SetMap(map, true);
            RefreshPickupPreview();
            RefreshMapInformation();
            SetStatus(map == null
                ? "표시할 Input Die Map이 없습니다."
                : "Input Die Map을 불러왔습니다. 시작 Die와 픽업 경로를 확인하세요.");
        }

        public void SetWorkflowState(
            string waferId,
            string recipeName,
            bool visionConnected,
            bool alignComplete,
            string mappingRevision,
            bool mappingComplete,
            string reviewState)
        {
            _waferId = waferId ?? string.Empty;
            _mappingRevision = mappingRevision ?? string.Empty;
            lblWaferValue.Text = string.IsNullOrWhiteSpace(waferId) ? "-" : waferId;
            lblRecipeValue.Text = string.IsNullOrWhiteSpace(recipeName) ? "-" : recipeName;
            // 연결 상태를 필드로 보관해 Live/Grab 버튼 활성 조건에 반영한다(표시만 하고 버리면
            // 미연결 상태에서 Grab/Live가 눌려 거짓 Live 상태가 된다).
            _waferVisionLinkConnected = visionConnected;
            ApplyVisionConnectionLabel(visionConnected);
            // 개창 시 비전 패널 라벨도 실제 상태로 갱신한다(Designer 초기 문자열이 남지 않도록).
            ApplyWaferVisionStateLabel();
            _alignComplete = alignComplete;
            _mappingComplete = mappingComplete;
            _reviewValid = false;
            lblAlignValue.Text = alignComplete ? "COMPLETE" : "REQUIRED";
            lblAlignValue.ForeColor = alignComplete ? Color.LightGreen : Color.Khaki;
            lblMappingValue.Text = mappingComplete ? "COMPLETE" : "REQUIRED";
            lblMappingValue.ForeColor = mappingComplete ? Color.LightGreen : Color.Khaki;
            lblMappingRevisionValue.Text = string.IsNullOrWhiteSpace(mappingRevision) ? "-" : mappingRevision;
            lblReviewValue.Text = string.IsNullOrWhiteSpace(reviewState) ? "REVIEW REQUIRED" : reviewState;
            UpdateActionAvailability();

            // 창 진입/상태 전환 시점을 남긴다(어떤 웨이퍼를 어떤 상태로 검토했는지 추적).
            LogReviewAction("STATE",
                "Review 상태 반영: recipe=" + (recipeName ?? "-") +
                ", vision=" + (visionConnected ? "CONNECTED" : "DISCONNECTED") +
                ", align=" + alignComplete +
                ", mapping=" + mappingComplete +
                ", revision=" + (mappingRevision ?? "-") +
                ", review=" + (reviewState ?? "-"));
        }

        /// <summary>
        /// 수동 폴백 얼라인(센터 다이 미검출) 웨이퍼: T 보정을 완료하기 전까지 확정(START RUN)을 잠근다.
        /// </summary>
        public void SetManualAlignFallbackThetaRequired(bool required)
        {
            _manualFallbackThetaRequired = required;
            _manualFallbackThetaDone = false;
            if (required)
            {
                lblAlignValue.Text = "MANUAL(T REQUIRED)";
                lblAlignValue.ForeColor = Color.Khaki;
                SetStatus("수동 얼라인 웨이퍼입니다. Jog로 정렬 후 [T 보정]을 완료해야 확정할 수 있습니다.");
                LogReviewAction("MANUAL-FALLBACK", "수동 폴백 얼라인 — T 보정 완료 전 확정 차단");
            }
            UpdateActionAvailability();
        }

        /// <summary>Form1이 Review T 보정 저장 성공 후 호출해 확정 잠금을 해제한다.</summary>
        public void NotifyThetaCorrectionCompleted()
        {
            if (!_manualFallbackThetaRequired || _manualFallbackThetaDone)
                return;

            _manualFallbackThetaDone = true;
            lblAlignValue.Text = "MANUAL(T OK)";
            lblAlignValue.ForeColor = Color.LightGreen;
            LogReviewAction("MANUAL-FALLBACK", "T 보정 완료 — 확정 허용");
            UpdateActionAvailability();
        }

        /// <summary>
        /// T 보정 저장으로 Mapping이 무효화된 뒤 재매핑 결정을 자동 제출한다.
        /// RUN DIE MAPPING 버튼과 완전히 같은 결정 경로를 타며, 제출이 거부되면(수동 모드,
        /// 이동/Jog 진행 중 등) false를 반환해 호출자가 수동 안내로 폴백한다.
        /// </summary>
        public bool TrySubmitAutoMappingRetryAfterThetaCorrection()
        {
            if (!_autoReviewMode || _decisionSubmitted)
                return false;

            LogReviewAction("AUTO-RETRY-MAPPING",
                "T 보정 저장으로 Mapping이 무효화되어 Die Mapping 재실행을 자동 제출합니다.");
            SubmitAutoReviewDecision(MappingRetryRequested, DialogResult.Retry,
                "AUTO RUN DIE MAPPING(ThetaCorrection)");
            return _decisionSubmitted;
        }

        /// <summary>
        /// Wafer Vision 연결 상태를 창이 열려 있는 동안에도 갱신한다.
        /// (예전에는 개창 시 스냅샷만 표시해, 이후 끊겨도 CONNECTED로 남고 Live/Grab이 계속 활성이었다.)
        /// </summary>
        public void SetWaferVisionConnectionState(bool connected)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<bool>(SetWaferVisionConnectionState), connected); }
                catch { }
                return;
            }

            if (_waferVisionLinkConnected == connected)
                return;

            _waferVisionLinkConnected = connected;
            ApplyVisionConnectionLabel(connected);
            ApplyWaferVisionStateLabel();
            UpdateActionAvailability();
            LogReviewAction("VISION-LINK", "Wafer Vision 연결 상태 변경: " +
                (connected ? "CONNECTED" : "DISCONNECTED"));
        }

        private void ApplyVisionConnectionLabel(bool connected)
        {
            lblVisionValue.Text = connected ? "CONNECTED" : "DISCONNECTED";
            lblVisionValue.ForeColor = connected ? Color.LightGreen : Color.LightSalmon;
        }

        /// <summary>
        /// 비전 패널 상태 라벨을 "안전 스코프 보유 여부 × 실제 연결 여부"로 구성한다.
        /// Designer 초기 문자열("영상 수신 대기 (측정 가능)")이 그대로 남거나,
        /// 미연결인데 "Live/Grab/측정 가능"으로 표시되던 문제를 함께 없앤다.
        /// </summary>
        private void ApplyWaferVisionStateLabel()
        {
            if (lblWaferVisionState == null || lblWaferVisionState.IsDisposed)
                return;

            string text;
            Color color;
            if (_readOnlyPreview)
            {
                text = _waferVisionLinkConnected
                    ? "읽기 전용 - 영상 확인/측정만 가능"
                    : "읽기 전용 - 비전 미연결(영상 없음)";
                color = Color.DimGray;
            }
            else if (!_waferVisionLinkConnected)
            {
                text = _waferVisionControlActive
                    ? "안전 영역 사용 중 - 비전 미연결로 Live/Grab/측정 불가"
                    : "비전 미연결 - 영상/측정 불가(설정에서 비전 연결 확인)";
                color = Color.Firebrick;
            }
            else if (_waferVisionControlActive)
            {
                text = "비전 안전 영역 사용 중 - Live/Grab/측정 가능";
                color = Color.SeaGreen;
            }
            else
            {
                text = "영상 확인/측정 가능 - Live/Grab은 비전 사용 시작 후 가능";
                color = Color.DimGray;
            }

            lblWaferVisionState.Text = text;
            lblWaferVisionState.ForeColor = color;
        }

        public void SetReviewValid(bool valid, string reviewState)
        {
            _reviewValid = valid;
            lblReviewValue.Text = string.IsNullOrWhiteSpace(reviewState)
                ? (valid ? "READY TO START" : "REVIEW REQUIRED")
                : reviewState;
            lblReviewValue.ForeColor = valid ? Color.LightGreen : Color.Khaki;
            UpdateActionAvailability();
        }

        public void SetPickupOptions(PickupSubset options)
        {
            PickupSubset source = options ?? new PickupSubset();
            rbCornerTopLeft.Checked = source.StartCorner == PickupStartCorner.TopLeft;
            rbCornerBottomLeft.Checked = source.StartCorner == PickupStartCorner.BottomLeft;
            rbCornerBottomRight.Checked = source.StartCorner == PickupStartCorner.BottomRight;
            rbCornerTopRight.Checked = source.StartCorner == PickupStartCorner.TopRight;
            rbDirectionHorizontal.Checked = source.Direction == PickupDirection.Horizontal;
            rbDirectionVertical.Checked = source.Direction == PickupDirection.Vertical;
            rbPatternStraight.Checked = source.Pattern == PickupPattern.Straight;
            rbPatternZigZag.Checked = source.Pattern == PickupPattern.ZigZag;
            RefreshPickupPreview();
        }

        public void SetReadOnlyPreview(bool readOnly)
        {
            _readOnlyPreview = readOnly;
            if (readOnly)
            {
                _waferVisionControlActive = false;
                ConfigureWaferVisionViewer(false);
                lblWaferVisionState.Text = "읽기 전용 - 영상 확인/측정만 가능";
            }
            UpdateActionAvailability();
            if (readOnly)
                SetStatus("현재 Stage Wafer/DieMap의 읽기 전용 화면입니다. 모션 및 데이터 변경 기능은 연결되지 않았습니다.");
        }

        public void SetAutoReviewMode(bool enabled)
        {
            _autoReviewMode = enabled;
            _decisionSubmitted = false;
            _sequenceCloseRequested = false;
            _submittedDialogResult = DialogResult.None;
            if (enabled)
                _readOnlyPreview = false;

            if (enabled && chkUseSelectedStart.Checked && _startDie == null && _baseOrder.Count > 0)
            {
                _startDie = _baseOrder[0];
                RefreshPickupPreview();
                RefreshSelectedDieInformation();
            }

            _pickupOrderApplied = enabled;
            btnClose.Visible = !enabled;
            UpdateActionAvailability();
            if (enabled)
                SetStatus("Manual Review 기능이 활성화되었습니다. 확인 시 Auto 공정을 계속하고, 취소 시 센터 검출/T Align부터 다시 수행합니다.");
        }

        public void CloseFromSequence()
        {
            _decisionSubmitted = true;
            _sequenceCloseRequested = true;
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(CloseFromSequence));
                }
                catch
                {
                }
                return;
            }

            // modeless 창은 DialogResult 설정만으로 닫히지 않으므로
            // 제출된 DialogResult를 기록한 뒤 반드시 Close()를 호출한다.
            if (_submittedDialogResult != DialogResult.None)
                DialogResult = _submittedDialogResult;
            Close();
        }

        public void RestoreAfterDecisionFailure(string status)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(RestoreAfterDecisionFailure), status);
                return;
            }

            _decisionSubmitted = false;
            _sequenceCloseRequested = false;
            _submittedDialogResult = DialogResult.None;
            _busy = false;
            DialogResult = DialogResult.None;
            // 시퀀스가 결정을 거부하고 되돌린 경우다(알람/Stop 요청 등). 원인 추적을 위해 남긴다.
            LogReviewBlocked("DECISION-RESTORED",
                "시퀀스가 결정을 거부해 화면을 원복했습니다. reason=" +
                (string.IsNullOrWhiteSpace(status) ? "-" : status));
            SetStatus(string.IsNullOrWhiteSpace(status)
                ? "요청 처리에 실패했습니다. 상태를 확인한 뒤 다시 시도하세요."
                : status);
            UpdateActionAvailability();
        }

        public void SetFailureDetail(string alarmCode, string detail)
        {
            txtFailureDetail.Text = string.IsNullOrWhiteSpace(alarmCode)
                ? (detail ?? string.Empty)
                : alarmCode + Environment.NewLine + (detail ?? string.Empty);
        }

        public void SetAxisPositions(double visionX, double waferY, double waferT)
        {
            lblVisionXValue.Text = visionX.ToString("F3");
            lblWaferYValue.Text = waferY.ToString("F3");
            lblWaferTValue.Text = waferT.ToString("F4");
            lblInputCameraXValue.Text = visionX.ToString("F3");
            lblInputStageYValue.Text = waferY.ToString("F3");
        }

        public void SetBusy(bool busy, string status)
        {
            _busy = busy;
            if (!string.IsNullOrWhiteSpace(status))
                SetStatus(status);
            UpdateActionAvailability();
        }

        public void SetWaferVisionMoveBusy(bool busy, string status)
        {
            _waferVisionMoveBusy = busy && _waferVisionControlActive;
            if (!string.IsNullOrWhiteSpace(status))
                SetStatus(status);
            UpdateActionAvailability();
        }
        public int StartDieIndex
        {
            get
            {
                DieMapEntry start = StartDie;
                int index = start != null
                    ? _baseOrder.FindIndex(entry => IsSameEntry(entry, start))
                    : -1;
                return index >= 0 ? index + 1 : 0;
            }
        }

        public bool ApplyDraftCoordinateOffset(double offsetX, double offsetY, out string reason)
        {
            reason = string.Empty;
            if (_dieMap == null || _dieMap.Entries == null ||
                double.IsNaN(offsetX) || double.IsInfinity(offsetX) ||
                double.IsNaN(offsetY) || double.IsInfinity(offsetY))
            {
                reason = "Review Draft Die Map 또는 Offset 값이 유효하지 않습니다.";
                return false;
            }

            _dieMap.OriginX += offsetX;
            _dieMap.OriginY += offsetY;
            foreach (DieMapEntry entry in _dieMap.Entries)
            {
                if (entry == null)
                    continue;
                entry.PosX += offsetX;
                entry.PosY += offsetY;
            }

            RefreshPickupPreview();
            RefreshMapInformation();
            RefreshSelectedDieInformation();
            reason = "Review Draft 전체 Die 좌표에 Offset을 적용했습니다. X=" +
                     offsetX.ToString("F6") + ", Y=" + offsetY.ToString("F6");
            SetStatus(reason);
            return true;
        }

        private void ConfigureMapView()
        {
            mapView.Caption = "INPUT WAFER MAP";
            mapView.ShowWaferOutline = true;
            mapView.ShowEquipmentAxes = true;
            mapView.CompactUsedBounds = true;
            mapView.EnableRectangleSelection = true;
            mapView.CellColorResolver = ResolveMapCellColor;
            mapView.CellTextResolver = ResolveMapCellText;
            mapView.CellStatusResolver = BuildMapCellStatus;
            mapView.LegendItemsResolver = BuildLegendItems;
            dieGrid.MultiSelect = true;
            foreach (Button jogButton in new[]
            {
                btnVisionXMinus, btnVisionXPlus,
                btnWaferYMinus, btnWaferYPlus,
                btnWaferTMinus, btnWaferTPlus
            })
            {
                jogButton.MouseCaptureChanged += JogButton_MouseCaptureChanged;
            }
        }

        private Color ResolveMapCellColor(DieMapEntry entry)
        {
            if (entry == null)
                return Color.DimGray;
            if (chkUseSelectedStart.Checked && ReferenceEquals(entry, _startDie))
                return QMC.CDT_320.Ui.Controls.WaferMapPalette.StartMarker;
            if (!entry.IsTarget)
                return QMC.CDT_320.Ui.Controls.WaferMapPalette.Skip;
            if (entry.Result == DieResult.Good)
                return BinCodeMap.ConvertToBinCodeColor(BinCodeMap.GoodBin);
            if (entry.Result == DieResult.NG)
                return QMC.CDT_320.Ui.Controls.WaferMapPalette.NgFallback;
            // [픽업 BIN 색표시 2026-08-27 팀장님 지시] 검사 전(WAIT) 다이는 BIN별 색으로 구분한다 —
            // BIN 1은 기존 하늘색 유지, 그 외 BIN은 고정 팔레트(값 기준 안정 배정). 검사 결과가
            // 나오면 GOOD/NG 상태색이 우선한다(위 분기 유지).
            return ResolveWaitBinColor(entry.BinCode);
        }

        // BIN별 WAIT 색 팔레트 — BIN 1=기존 WAIT 하늘색, 그 외는 BIN 값으로 고정 순환 배정
        // (같은 BIN은 항상 같은 색, 맵/세션 무관 안정). 상태색(GOOD 녹/NG 적/SKIP 회/START 주황)과
        // 겹치지 않는 계열로 구성.
        private static readonly Color[] WaitBinPalette =
        {
            Color.FromArgb(196, 156, 222),   // 보라
            Color.FromArgb(242, 196, 140),   // 살구
            Color.FromArgb(148, 214, 212),   // 청록
            Color.FromArgb(232, 172, 196),   // 분홍
            Color.FromArgb(180, 202, 138),   // 연올리브
            Color.FromArgb(160, 176, 236),   // 연보라파랑
            Color.FromArgb(226, 214, 128),   // 겨자
            Color.FromArgb(178, 156, 136)    // 갈색빛 회
        };

        private static Color ResolveWaitBinColor(int binCode)
        {
            if (binCode <= 1)
                return QMC.CDT_320.Ui.Controls.WaferMapPalette.Wait;   // BIN 1(및 미기록 0) = 기존 WAIT 하늘색

            return WaitBinPalette[(binCode - 2) % WaitBinPalette.Length];
        }

        private string ResolveMapCellText(DieMapEntry entry)
        {
            if (entry == null)
                return string.Empty;
            if (chkUseSelectedStart.Checked && ReferenceEquals(entry, _startDie))
                return "S";

            int sequence;
            return _previewSequence.TryGetValue(entry, out sequence)
                ? sequence.ToString()
                : string.Empty;
        }

        private string BuildMapCellStatus(DieMapEntry entry)
        {
            if (entry == null)
                return string.Empty;

            int sequence;
            _previewSequence.TryGetValue(entry, out sequence);
            return "Sequence=" + (sequence > 0 ? sequence.ToString() : "-") +
                   ", Map=(" + entry.DieMapX + "," + entry.DieMapY + ")" +
                   ", EquipmentGrid=(" + FormatGrid(entry.EquipmentGridX) + "," + FormatGrid(entry.EquipmentGridY) + ")" +
                   ", Position=(" + entry.PosX.ToString("F3") + "," + entry.PosY.ToString("F3") + ")" +
                   ", UID=" + (entry.DieUid ?? "");
        }

        private Tuple<string, Color>[] BuildLegendItems()
        {
            // [픽업 BIN 색표시 2026-08-27] 맵에 실제 존재하는 BIN만 WAIT 색 범례로 동적 표시한다
            // (예: WAIT B1, B125). BIN이 1종뿐이면 기존 "WAIT" 단일 표기를 유지한다.
            var legend = new List<Tuple<string, Color>>();
            var waitBins = new SortedSet<int>();
            if (_dieMap != null && _dieMap.Entries != null)
            {
                foreach (DieMapEntry entry in _dieMap.Entries)
                {
                    if (entry != null && entry.IsTarget)
                        waitBins.Add(entry.BinCode <= 1 ? 1 : entry.BinCode);
                }
            }

            if (waitBins.Count <= 1)
            {
                legend.Add(Tuple.Create("WAIT", QMC.CDT_320.Ui.Controls.WaferMapPalette.Wait));
            }
            else
            {
                foreach (int bin in waitBins)
                    legend.Add(Tuple.Create("WAIT B" + bin, ResolveWaitBinColor(bin)));
            }

            legend.Add(Tuple.Create("START", QMC.CDT_320.Ui.Controls.WaferMapPalette.StartMarker));
            legend.Add(Tuple.Create("GOOD", BinCodeMap.ConvertToBinCodeColor(BinCodeMap.GoodBin)));
            legend.Add(Tuple.Create("NG", QMC.CDT_320.Ui.Controls.WaferMapPalette.NgFallback));
            legend.Add(Tuple.Create("SKIP", QMC.CDT_320.Ui.Controls.WaferMapPalette.Skip));
            return legend.ToArray();
        }

        private void RefreshPickupPreview()
        {
            _baseOrder.Clear();
            _previewOrder.Clear();
            _previewSequence.Clear();

            if (_dieMap != null)
            {
                List<DieMapEntry> generated = PickupSequenceGenerator.Build(_dieMap, BuildPickupOptions());
                foreach (DieMapEntry entry in generated)
                {
                    if (IsPickableEntry(entry))
                        _baseOrder.Add(entry);
                }

                List<DieMapEntry> ordered = new List<DieMapEntry>(_baseOrder);
                if (chkUseSelectedStart.Checked && _startDie != null)
                    ordered = RotateOrderAtStartDie(ordered, _startDie);

                for (int i = 0; i < ordered.Count; i++)
                {
                    DieMapEntry entry = ordered[i];
                    if (entry == null)
                        continue;
                    _previewOrder.Add(entry);
                    _previewSequence[entry] = _previewOrder.Count;
                }
            }

            decimal maximum = Math.Max(1, _baseOrder.Count);
            numStartIndex.Maximum = maximum;
            int selectedStartIndex = StartDieIndex;
            if (selectedStartIndex > 0)
                numStartIndex.Value = Math.Min(maximum, selectedStartIndex);
            else if (numStartIndex.Value > maximum)
                numStartIndex.Value = maximum;

            if (_dieMap != null && _dieMap.Entries != null)
            {
                foreach (DieMapEntry entry in _dieMap.Entries)
                {
                    if (entry != null)
                        entry.SequenceNo = 0;
                }
                for (int i = 0; i < _previewOrder.Count; i++)
                    _previewOrder[i].SequenceNo = i + 1;
            }

            RefreshDieGrid();
            RefreshProgress();
            mapView.Invalidate();
            _pickupOrderApplied = false;
            UpdateActionAvailability();
        }

        private static bool IsPickableEntry(DieMapEntry entry)
        {
            return entry != null &&
                   entry.IsTarget &&
                   entry.Result != DieResult.Good &&
                   entry.Result != DieResult.NG;
        }

        private PickupSubset BuildPickupOptions()
        {
            var options = new PickupSubset();
            if (rbCornerTopLeft.Checked)
                options.StartCorner = PickupStartCorner.TopLeft;
            else if (rbCornerBottomLeft.Checked)
                options.StartCorner = PickupStartCorner.BottomLeft;
            else if (rbCornerBottomRight.Checked)
                options.StartCorner = PickupStartCorner.BottomRight;
            else
                options.StartCorner = PickupStartCorner.TopRight;

            options.Direction = rbDirectionVertical.Checked
                ? PickupDirection.Vertical
                : PickupDirection.Horizontal;
            options.Pattern = rbPatternStraight.Checked
                ? PickupPattern.Straight
                : PickupPattern.ZigZag;
            return options;
        }

        private static List<DieMapEntry> RotateOrderAtStartDie(List<DieMapEntry> source, DieMapEntry startDie)
        {
            if (source == null || source.Count == 0 || startDie == null)
                return source ?? new List<DieMapEntry>();

            int startIndex = source.FindIndex(entry => IsSameEntry(entry, startDie));
            if (startIndex <= 0)
                return source;

            var rotated = new List<DieMapEntry>(source.Count);
            for (int i = startIndex; i < source.Count; i++)
                rotated.Add(source[i]);
            for (int i = 0; i < startIndex; i++)
                rotated.Add(source[i]);
            return rotated;
        }

        private void RefreshDieGrid()
        {
            List<DieMapEntry> entries = BuildDieGridEntries();
            string signature = BuildDieGridSignature(entries);
            if (!_dieGridRebindRequired &&
                string.Equals(signature, _lastDieGridSignature, StringComparison.Ordinal))
                return;

            _synchronizingSelection = true;
            try
            {
                // 행 구성이 같으면 Rows.Clear/Add를 하지 않고 값만 갱신해 선택과 스크롤을 보존한다.
                if (!_dieGridRebindRequired && TryRefreshDieGridValues(entries))
                {
                    _lastDieGridSignature = signature;
                    return;
                }

                // 행 수 또는 순서가 달라 부분 갱신할 수 없을 때만 전체 행을 안전하게 다시 구성한다.
                dieGrid.Rows.Clear();
                foreach (DieMapEntry entry in entries)
                {
                    int rowIndex = dieGrid.Rows.Add(BuildDieGridRowValues(entry));
                    dieGrid.Rows[rowIndex].Tag = entry;
                }

                dieGrid.ClearSelection();
                int firstSelectedRow = -1;
                foreach (DataGridViewRow row in dieGrid.Rows)
                {
                    DieMapEntry rowEntry = row.Tag as DieMapEntry;
                    bool selected = ContainsSameEntry(_selectedDies, rowEntry);
                    row.Selected = selected;
                    if (selected && firstSelectedRow < 0)
                        firstSelectedRow = row.Index;
                }

                if (firstSelectedRow >= 0)
                    dieGrid.FirstDisplayedScrollingRowIndex = firstSelectedRow;

                _lastDieGridSignature = signature;
                _dieGridRebindRequired = false;
            }
            finally
            {
                _synchronizingSelection = false;
            }
        }

        private List<DieMapEntry> BuildDieGridEntries()
        {
            IEnumerable<DieMapEntry> entries = _dieMap != null && _dieMap.Entries != null
                ? _dieMap.Entries
                : Enumerable.Empty<DieMapEntry>();

            return entries
                .Where(item => item != null)
                .OrderBy(item => ResolvePreviewSequence(item) <= 0 ? int.MaxValue : ResolvePreviewSequence(item))
                .ThenBy(item => item.DieMapY)
                .ThenBy(item => item.DieMapX)
                .ToList();
        }

        private string BuildDieGridSignature(IList<DieMapEntry> entries)
        {
            var signature = new StringBuilder();
            signature.Append(entries != null ? entries.Count : 0).Append('|');
            if (entries == null)
                return signature.ToString();

            foreach (DieMapEntry entry in entries)
            {
                string dieUid = entry != null ? entry.DieUid ?? string.Empty : string.Empty;
                signature
                    .Append(ResolvePreviewSequence(entry)).Append(':')
                    .Append(entry != null ? entry.DieMapX : 0).Append(':')
                    .Append(entry != null ? entry.DieMapY : 0).Append(':')
                    .Append(entry != null ? entry.OriginalMapX : 0).Append(':')
                    .Append(entry != null ? entry.OriginalMapY : 0).Append(':')
                    .Append(entry != null && entry.IsTarget ? '1' : '0').Append(':')
                    .Append(entry != null ? (int)entry.Result : 0).Append(':')
                    .Append(entry != null ? entry.BinCode : 0).Append(':')
                    .Append(entry != null ? BitConverter.DoubleToInt64Bits(entry.EquipmentGridX) : 0L).Append(':')
                    .Append(entry != null ? BitConverter.DoubleToInt64Bits(entry.EquipmentGridY) : 0L).Append(':')
                    .Append(entry != null ? BitConverter.DoubleToInt64Bits(entry.PosX) : 0L).Append(':')
                    .Append(entry != null ? BitConverter.DoubleToInt64Bits(entry.PosY) : 0L).Append(':')
                    .Append(dieUid.Length).Append('#').Append(dieUid).Append('|');
            }

            return signature.ToString();
        }

        private bool TryRefreshDieGridValues(IList<DieMapEntry> entries)
        {
            if (entries == null || dieGrid.Rows.Count != entries.Count)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                DieMapEntry rowEntry = dieGrid.Rows[i].Tag as DieMapEntry;
                DieMapEntry updatedEntry = entries[i];
                if (rowEntry == null || updatedEntry == null ||
                    rowEntry.DieMapX != updatedEntry.DieMapX ||
                    rowEntry.DieMapY != updatedEntry.DieMapY)
                {
                    return false;
                }
            }

            dieGrid.SuspendLayout();
            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    DieMapEntry entry = entries[i];
                    DataGridViewRow row = dieGrid.Rows[i];
                    object[] values = BuildDieGridRowValues(entry);
                    for (int columnIndex = 0; columnIndex < values.Length; columnIndex++)
                        row.Cells[columnIndex].Value = values[columnIndex];
                    row.Tag = entry;
                }
            }
            finally
            {
                dieGrid.ResumeLayout();
            }

            return true;
        }

        private object[] BuildDieGridRowValues(DieMapEntry entry)
        {
            return new object[]
            {
                ResolvePreviewSequence(entry) > 0 ? ResolvePreviewSequence(entry).ToString() : "-",
                entry.DieMapX,
                entry.DieMapY,
                FormatGrid(entry.EquipmentGridX),
                FormatGrid(entry.EquipmentGridY),
                entry.OriginalMapX >= 0 ? entry.OriginalMapX.ToString() : "-",
                entry.OriginalMapY >= 0 ? entry.OriginalMapY.ToString() : "-",
                ResolveDieStateText(entry),
                entry.Result,
                entry.BinCode,
                entry.PosX.ToString("F4"),
                entry.PosY.ToString("F4"),
                entry.DieUid ?? string.Empty
            };
        }

        private void RefreshProgress()
        {
            int total = _dieMap != null && _dieMap.Entries != null
                ? _dieMap.Entries.Count(entry => entry != null && entry.IsTarget)
                : 0;
            int complete = _dieMap != null && _dieMap.Entries != null
                ? _dieMap.Entries.Count(entry => entry != null && entry.IsTarget &&
                    (entry.Result == DieResult.Good || entry.Result == DieResult.NG))
                : 0;
            lblMapGridValue.Text = _dieMap == null ? "-" : _dieMap.DieMapX + " x " + _dieMap.DieMapY;
            lblMapProgressValue.Text = complete + " / " + total;
            lblTargetCountValue.Text = total.ToString();
        }

        private void RefreshMapInformation()
        {
            lblDieSizeXValue.Text = _dieMap != null ? _dieMap.DieSizeX.ToString("F4") : "-";
            lblDieSizeYValue.Text = _dieMap != null ? _dieMap.DieSizeY.ToString("F4") : "-";
            lblPitchGapXValue.Text = _dieMap != null ? (_dieMap.PitchX - _dieMap.DieSizeX).ToString("F4") : "-";
            lblPitchGapYValue.Text = _dieMap != null ? (_dieMap.PitchY - _dieMap.DieSizeY).ToString("F4") : "-";
            lblWaferDiameterValue.Text = _dieMap != null ? _dieMap.OuterDiameterMm.ToString("F3") : "-";
            lblMappingOriginValue.Text = _dieMap != null
                ? _dieMap.OriginX.ToString("F3") + " / " + _dieMap.OriginY.ToString("F3")
                : "-";
            RefreshSelectedDieInformation();
        }

        private void RefreshSelectedDieInformation()
        {
            DieMapEntry entry = _selectedDie;
            lblSelectedDieValue.Text = entry != null
                ? (string.IsNullOrWhiteSpace(entry.DieUid) ? "-" : entry.DieUid)
                : "-";
            lblEquipmentGridValue.Text = entry != null
                ? FormatGrid(entry.EquipmentGridX) + " / " + FormatGrid(entry.EquipmentGridY)
                : "-";
            lblOriginalMapValue.Text = entry != null
                ? (entry.OriginalMapX >= 0 ? entry.OriginalMapX.ToString() : "-") + " / " +
                  (entry.OriginalMapY >= 0 ? entry.OriginalMapY.ToString() : "-")
                : "-";
            lblSelectedPositionValue.Text = entry != null
                ? entry.PosX.ToString("F3") + " / " + entry.PosY.ToString("F3")
                : "-";
            lblSelectedSequenceValue.Text = entry != null && ResolvePreviewSequence(entry) > 0
                ? ResolvePreviewSequence(entry).ToString()
                : "-";
            DieMapEntry start = StartDie;
            lblStartDieValue.Text = start != null
                ? (string.IsNullOrWhiteSpace(start.DieUid)
                    ? "Map " + start.DieMapX + "," + start.DieMapY
                    : start.DieUid)
                : "NOT SET";
        }

        private int ResolvePreviewSequence(DieMapEntry entry)
        {
            int sequence;
            return entry != null && _previewSequence.TryGetValue(entry, out sequence) ? sequence : 0;
        }

        private static string ResolveDieStateText(DieMapEntry entry)
        {
            if (entry == null || !entry.IsTarget)
                return "SKIP";
            if (entry.Result == DieResult.Good)
                return "GOOD";
            if (entry.Result == DieResult.NG)
                return "NG";
            return "WAIT";
        }

        private static bool IsSameEntry(DieMapEntry left, DieMapEntry right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null)
                return false;
            if (!string.IsNullOrWhiteSpace(left.DieUid) && !string.IsNullOrWhiteSpace(right.DieUid))
                return string.Equals(left.DieUid, right.DieUid, StringComparison.OrdinalIgnoreCase);
            return left.DieMapX == right.DieMapX && left.DieMapY == right.DieMapY;
        }

        private static bool ContainsSameEntry(IEnumerable<DieMapEntry> entries, DieMapEntry target)
        {
            if (entries == null || target == null)
                return false;

            foreach (DieMapEntry entry in entries)
            {
                if (IsSameEntry(entry, target))
                    return true;
            }
            return false;
        }

        private DieMapEntry ResolveDraftEntry(DieMapEntry entry)
        {
            if (entry == null || _dieMap == null || _dieMap.Entries == null)
                return null;

            return _dieMap.Entries.FirstOrDefault(item => IsSameEntry(item, entry));
        }

        private static string FormatGrid(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? "-" : value.ToString("0.###");
        }

        private void SelectDie(DieMapEntry entry, bool selectGrid)
        {
            SetSelectedDies(new[] { entry }, entry, true, selectGrid);
        }

        private void SetSelectedDies(
            IEnumerable<DieMapEntry> entries,
            DieMapEntry primary,
            bool synchronizeMap,
            bool synchronizeGrid)
        {
            _selectedDies.Clear();
            if (entries != null)
            {
                foreach (DieMapEntry source in entries)
                {
                    DieMapEntry entry = ResolveDraftEntry(source);
                    if (entry != null && !ContainsSameEntry(_selectedDies, entry))
                        _selectedDies.Add(entry);
                }
            }

            _selectedDie = ResolveDraftEntry(primary);
            if (_selectedDie == null || !ContainsSameEntry(_selectedDies, _selectedDie))
                _selectedDie = _selectedDies.Count > 0 ? _selectedDies[0] : null;

            if (synchronizeMap)
                mapView.SetSelectedEntries(_selectedDies);

            RefreshSelectedDieInformation();

            if (!synchronizeGrid)
                return;

            _synchronizingSelection = true;
            try
            {
                dieGrid.ClearSelection();
                foreach (DataGridViewRow row in dieGrid.Rows)
                {
                    DieMapEntry rowEntry = row.Tag as DieMapEntry;
                    bool selected = ContainsSameEntry(_selectedDies, rowEntry);
                    row.Selected = selected;
                    if (selected && IsSameEntry(rowEntry, _selectedDie))
                        dieGrid.FirstDisplayedScrollingRowIndex = Math.Max(0, row.Index);
                }
            }
            finally
            {
                _synchronizingSelection = false;
            }
        }

        private void MapView_CellClicked(DieMapEntry entry)
        {
            SelectDie(entry, true);
        }

        private void MapView_EmptyAreaClicked()
        {
            SetSelectedDies(Enumerable.Empty<DieMapEntry>(), null, false, true);
        }

        private void MapView_CellDoubleClicked(DieMapEntry entry)
        {
            SelectDie(entry, true);
            if (_readOnlyPreview)
                return;
            if (_waferVisionMoveBusy)
            {
                SetStatus("선택 Die 이동이 진행 중입니다. 완료 또는 STOP 후 다시 실행하세요.");
                return;
            }
            if (_busy && !_waferVisionControlActive)
            {
                SetStatus("다른 Review 수동 동작이 진행 중입니다. 완료 또는 STOP 후 다시 실행하세요.");
                return;
            }

            // 맵 더블클릭은 버튼과 동일하게 실제 축을 움직이는 경로다. 반드시 이력을 남긴다.
            LogReviewAction("MOVE-DIE",
                "선택 Die 이동 요청(맵 더블클릭): pos=(" +
                (entry != null ? entry.PosX.ToString("F4") : "-") + "," +
                (entry != null ? entry.PosY.ToString("F4") : "-") + ")" +
                ", mappingComplete=" + _mappingComplete);
            RaiseSimpleEvent(SelectedDieMoveRequested);
        }

        private void MapView_SelectionRectangleCompleted(IReadOnlyList<DieMapEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                SetSelectedDies(Enumerable.Empty<DieMapEntry>(), null, false, true);
                return;
            }
            SetSelectedDies(entries, entries[0], true, true);
            SetStatus(entries.Count + "개 Die가 선택되었습니다. 상태 변경 시 전체 선택 대상에 적용됩니다.");
        }

        private void DieGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (_synchronizingSelection)
                return;

            if (dieGrid.SelectedRows.Count == 0)
            {
                SetSelectedDies(Enumerable.Empty<DieMapEntry>(), null, true, false);
                return;
            }

            List<DieMapEntry> entries = dieGrid.SelectedRows
                .Cast<DataGridViewRow>()
                .OrderBy(row => row.Index)
                .Select(row => row.Tag as DieMapEntry)
                .Where(entry => entry != null)
                .ToList();
            DieMapEntry primary = dieGrid.CurrentRow != null
                ? dieGrid.CurrentRow.Tag as DieMapEntry
                : entries[0];
            SetSelectedDies(entries, primary, true, false);
        }

        private void PickupOption_CheckedChanged(object sender, EventArgs e)
        {
            var radio = sender as RadioButton;
            if (radio != null && !radio.Checked)
                return;
            _pickupOrderApplied = false;
            RefreshPickupPreview();
            SetStatus("픽업 경로 설정이 변경되었습니다. PREVIEW를 확인한 뒤 APPLY PICKUP ORDER를 실행하세요.");
        }

        private void ChkUseSelectedStart_CheckedChanged(object sender, EventArgs e)
        {
            _pickupOrderApplied = false;
            RefreshPickupPreview();
        }

        private void BtnSetStartDie_Click(object sender, EventArgs e)
        {
            if (_selectedDie == null)
            {
                SetStatus("시작할 Die를 Wafer Map 또는 목록에서 먼저 선택하세요.");
                return;
            }
            if (!IsPickableEntry(_selectedDie))
            {
                SetStatus("SKIP/GOOD/NG Die는 시작 Die로 설정할 수 없습니다.");
                return;
            }

            LogReviewAction("START-DIE",
                "시작 Die 지정: " + DescribeEntryForLog(_selectedDie));
            _startDie = _selectedDie;
            _pickupOrderApplied = false;
            if (!chkUseSelectedStart.Checked)
                chkUseSelectedStart.Checked = true;
            else
                RefreshPickupPreview();
            int startIndex = StartDieIndex;
            if (startIndex > 0)
                numStartIndex.Value = Math.Min(numStartIndex.Maximum, startIndex);
            RefreshSelectedDieInformation();
            SetStatus("선택 Die를 시작점으로 설정했습니다. UID=" + (_startDie.DieUid ?? "") +
                      ", Map=(" + _startDie.DieMapX + "," + _startDie.DieMapY + ")");
        }

        private void BtnSetStartIndex_Click(object sender, EventArgs e)
        {
            int requested = (int)numStartIndex.Value;
            DieMapEntry entry = requested > 0 && requested <= _baseOrder.Count
                ? _baseOrder[requested - 1]
                : null;
            if (entry == null)
            {
                SetStatus("입력한 1-base 순번에 해당하는 Pickable Die가 없습니다. sequence=" + requested);
                return;
            }

            SelectDie(entry, true);
            BtnSetStartDie_Click(sender, e);
        }

        private void BtnPreviewPath_Click(object sender, EventArgs e)
        {
            RefreshPickupPreview();
            SetStatus("픽업 경로 미리보기를 갱신했습니다. Target=" + _previewOrder.Count +
                      ", Start=" + (StartDie != null ? lblStartDieValue.Text : "Recipe Corner"));
        }

        private void BtnApplyPickupOrder_Click(object sender, EventArgs e)
        {
            if (_dieMap == null || _dieMap.Entries == null)
            {
                SetStatus("적용할 Review Draft Die Map이 없습니다.");
                return;
            }
            if (_previewOrder.Count == 0 && chkUseSelectedStart.Checked)
                chkUseSelectedStart.Checked = false;
            if (chkUseSelectedStart.Checked && _startDie == null)
            {
                SetStatus("선택 시작 Die 사용이 켜져 있지만 시작 Die가 지정되지 않았습니다.");
                return;
            }

            try
            {
                var handler = PickupOrderApplyRequested;
                if (handler != null)
                    handler(this, new InputStageReviewPickupOrderEventArgs(
                        BuildPickupOptions(),
                        StartDie,
                        new List<DieMapEntry>(_previewOrder).AsReadOnly()));
                _pickupOrderApplied = true;
                PickupSubset appliedOptions = BuildPickupOptions();
                LogReviewAction("PICKUP-ORDER",
                    "픽업 순서 적용: target=" + _previewOrder.Count +
                    ", corner=" + appliedOptions.StartCorner +
                    ", direction=" + appliedOptions.Direction +
                    ", pattern=" + appliedOptions.Pattern +
                    ", useSelectedStart=" + chkUseSelectedStart.Checked);
                SetStatus(_previewOrder.Count > 0
                    ? "픽업 경로 Draft를 적용했습니다. Pickable Target=" + _previewOrder.Count
                    : "Pickable Target이 0개인 빈 픽업 경로 Draft를 적용했습니다.");
                UpdateActionAvailability();
            }
            catch (Exception ex)
            {
                _pickupOrderApplied = false;
                SetStatus("픽업 경로 적용 요청에 실패했습니다. " + ex.Message);
                UpdateActionAvailability();
            }
        }

        private void BtnApplyDieState_Click(object sender, EventArgs e)
        {
            List<DieMapEntry> entries = _selectedDies
                .Where(item => item != null)
                .ToList();
            if (entries.Count == 0 && _selectedDie != null)
                entries.Add(_selectedDie);
            if (entries.Count == 0)
            {
                SetStatus("상태를 변경할 Die를 선택하세요.");
                return;
            }

            InputStageReviewDieState state = rbDieStateGood.Checked
                ? InputStageReviewDieState.Good
                : rbDieStateNg.Checked
                    ? InputStageReviewDieState.Ng
                    : rbDieStateSkip.Checked
                        ? InputStageReviewDieState.Skip
                        : InputStageReviewDieState.Wait;

            // Die 상태 변경은 픽업 대상 집합을 바꾸는 조작이므로 대상 UID까지 남긴다.
            LogReviewAction("DIE-STATE",
                "Die 상태 변경: state=" + state +
                ", count=" + entries.Count +
                ", uids=" + BuildEntryUidListForLog(entries));

            foreach (DieMapEntry entry in entries)
                ApplyDraftDieState(entry, state);

            _pickupOrderApplied = false;

            if (_startDie != null && !IsPickableEntry(_startDie))
                _startDie = null;

            RefreshPickupPreview();
            RefreshSelectedDieInformation();
            mapView.Invalidate();

            try
            {
                var handler = DieStateApplyRequested;
                if (handler != null)
                    handler(this, new InputStageReviewDieStateEventArgs(entries.AsReadOnly(), state));
                SetStatus(entries.Count + "개 Die 상태를 Review Draft에 적용했습니다: " + state);
            }
            catch (Exception ex)
            {
                SetStatus("Die 상태는 Draft에 반영되었지만 외부 알림 처리에 실패했습니다. " + ex.Message);
            }
        }

        private static void ApplyDraftDieState(DieMapEntry entry, InputStageReviewDieState state)
        {
            if (entry == null)
                return;

            switch (state)
            {
                case InputStageReviewDieState.Good:
                    entry.IsTarget = true;
                    entry.Result = DieResult.Good;
                    entry.BinCode = BinCodeMap.GoodBin;
                    break;
                case InputStageReviewDieState.Ng:
                    entry.IsTarget = true;
                    entry.Result = DieResult.NG;
                    entry.BinCode = BinCodeMap.MaxBin;
                    break;
                case InputStageReviewDieState.Skip:
                    entry.IsTarget = false;
                    entry.Result = DieResult.Unknown;
                    entry.BinCode = 0;
                    entry.SequenceNo = 0;
                    break;
                case InputStageReviewDieState.Wait:
                default:
                    entry.IsTarget = true;
                    entry.Result = DieResult.Unknown;
                    entry.BinCode = 0;
                    break;
            }
        }

        private void JogButton_MouseDown(object sender, MouseEventArgs e)
        {
            // [Live 중 Jog 허용 2026-08-17, 팀장님 지시] 기존 조건: _busy면 무조건 return.
            //   Wafer Vision Live Scope를 잡으면 SetBusy(true)로 _busy=true가 되므로,
            //   UpdateActionAvailability가 버튼을 활성으로 그려도 이 가드에서 조용히 막혀
            //   "버튼은 눌리는데 아무 일도 안 일어나는" 상태였다.
            //   현재 기준: UpdateActionAvailability와 동일 정책 —
            //   Live 보유(_waferVisionControlActive)는 "대기 중"이라 Jog를 막지 않고,
            //   실제 이동 중(_waferVisionMoveBusy)일 때만 중복 실행을 막는다.
            bool busyBlocksJog = (_busy && !_waferVisionControlActive) || _waferVisionMoveBusy;
            if (busyBlocksJog || _readOnlyPreview || e.Button != MouseButtons.Left)
                return;

            Button button = sender as Button;
            if (button == null || !(button.Tag is InputStageReviewJogAxis))
                return;

            int direction = button.Name.EndsWith("Minus", StringComparison.Ordinal) ? -1 : 1;
            var handler = JogRequested;

            if (IsStepJogMode)
            {
                // Step 모드는 버튼 유지와 무관한 one-shot 이동이므로 activeJogButton을 잡지 않는다.
                LogReviewAction("JOG-STEP",
                    "Step Jog 요청: axis=" + button.Tag +
                    ", dir=" + direction +
                    ", speed=" + DescribeJogSpeedForLog() +
                    ", step=" + SelectedJogStepDistance.ToString("0.###"));
                if (handler != null)
                    handler(this, new InputStageReviewJogEventArgs(
                        (InputStageReviewJogAxis)button.Tag,
                        direction,
                        cmbJogSpeed.Text,
                        true,
                        SelectedJogStepDistance));
                SetStatus(button.Text + " Step Jog(" + SelectedJogStepDistance.ToString("0.###") + ") 요청 중입니다.");
                return;
            }

            LogReviewAction("JOG-START",
                "연속 Jog 시작: axis=" + button.Tag +
                ", dir=" + direction +
                ", speed=" + DescribeJogSpeedForLog());
            _activeJogButton = button;
            UpdateActionAvailability();
            if (handler != null)
                handler(this, new InputStageReviewJogEventArgs(
                    (InputStageReviewJogAxis)button.Tag,
                    direction,
                    cmbJogSpeed.Text));
            else
                _activeJogButton = null;
            SetStatus(button.Text + " Jog 요청 중입니다. 버튼을 놓으면 정지 요청합니다.");
        }

        private void JogButton_MouseUp(object sender, MouseEventArgs e)
        {
            Button button = sender as Button;
            if (_activeJogButton == null || !ReferenceEquals(_activeJogButton, button))
                return;
            _activeJogButton = null;
            UpdateActionAvailability();
            LogReviewAction("JOG-STOP", "연속 Jog 정지 요청(버튼 놓음)");
            RaiseSimpleEvent(JogStopRequested);
            SetStatus("Jog 정지를 요청했습니다.");
        }

        private void JogButton_MouseCaptureChanged(object sender, EventArgs e)
        {
            Button button = sender as Button;
            if (button != null && ReferenceEquals(_activeJogButton, button) && !button.Capture)
            {
                _activeJogButton = null;
                UpdateActionAvailability();
                // 마우스 캡처 상실은 안전 정지 경로이므로 원인 추적을 위해 남긴다(빈도 낮음).
                LogReviewAction("JOG-STOP-CAPTURE", "연속 Jog 정지 요청(마우스 캡처 상실)");
                RaiseSimpleEvent(JogStopRequested);
            }
        }

        private void BtnJogStop_Click(object sender, EventArgs e)
        {
            _activeJogButton = null;
            UpdateActionAvailability();
            LogReviewAction("ACTION-STOP", "Review 수동 동작 정지 요청(STOP 버튼)");
            RaiseSimpleEvent(ReviewActionStopRequested);
            SetStatus("Review 수동 동작 정지를 요청했습니다.");
        }

        private void BtnMoveSelectedDie_Click(object sender, EventArgs e)
        {
            if (_selectedDie == null)
            {
                LogReviewBlocked("MOVE-DIE-BLOCKED", "선택 Die 이동 거부: 선택된 Die 없음");
                SetStatus("이동할 Die를 먼저 선택하세요.");
                return;
            }
            LogReviewAction("MOVE-DIE",
                "선택 Die 이동 요청(버튼): pos=(" +
                _selectedDie.PosX.ToString("F4") + "," + _selectedDie.PosY.ToString("F4") + ")");
            RaiseSimpleEvent(SelectedDieMoveRequested);
        }

        private void BtnRetryAlign_Click(object sender, EventArgs e)
        {
            SubmitAutoReviewDecision(AlignRetryRequested, DialogResult.Retry, "RETRY ALIGN(RetryAlign)");
        }

        private void BtnRetryMapping_Click(object sender, EventArgs e)
        {
            SubmitAutoReviewDecision(MappingRetryRequested, DialogResult.Retry, "RUN DIE MAPPING(RetryMapping)");
        }
        private void BtnMappingSetup_Click(object sender, EventArgs e)
        {
            LogReviewAction("MAPPING-SETUP", "MAPPING SETUP 요청");
            RaiseSimpleEvent(MappingSetupRequested);
        }

        private void BtnVisionTest_Click(object sender, EventArgs e)
        {
            LogReviewAction("VISION-TEST", "VISION TEST 창 요청");
            RaiseSimpleEvent(VisionTestRequested);
        }
        private void BtnWaferVisionControl_Click(object sender, EventArgs e)
        {
            if (_readOnlyPreview)
            {
                SetStatus("읽기 전용 화면에서는 Live/Grab 명령을 사용할 수 없습니다.");
                return;
            }

            bool stopping = _waferVisionControlActive;
            EventHandler handler = stopping
                ? WaferVisionControlStopRequested
                : WaferVisionControlStartRequested;
            if (handler == null)
            {
                LogReviewBlocked("VISION-SCOPE-NO-HANDLER",
                    "비전 안전 제어 요청 거부: 구독자 없음. stopping=" + stopping);
                SetStatus("Wafer Vision 안전 제어 연결이 없습니다.");
                return;
            }
            LogReviewAction("VISION-SCOPE",
                stopping ? "비전 사용 종료 요청" : "비전 사용 시작 요청");
            handler(this, EventArgs.Empty);
        }
        private void BtnThetaCorrection_Click(object sender, EventArgs e)
        {
            // T 보정은 영구 저장 + Die Mapping 무효화를 유발하므로 반드시 이력을 남긴다.
            LogReviewAction("THETA-CORRECTION", "T CORRECTION 요청(영구 저장/Mapping 무효화 유발)");
            RaiseSimpleEvent(ThetaCorrectionRequested);
        }

        private void BtnDieDetection_Click(object sender, EventArgs e)
        {
            LogReviewAction("DIE-DETECTION", "DIE DETECTION 요청");
            RaiseSimpleEvent(DieDetectionRequested);
        }

        private void BtnOffsetApply_Click(object sender, EventArgs e)
        {
            LogReviewAction("OFFSET-APPLY", "APPLY OFFSET 요청(Draft 전체 좌표 평행이동)");
            RaiseSimpleEvent(OffsetApplyRequested);
        }

        private void BtnStartRun_Click(object sender, EventArgs e)
        {
            SubmitAutoReviewDecision(StartRunRequested, DialogResult.OK, "CONFIRM/CONTINUE AUTO(ConfirmAndContinue)");
        }

        private void BtnAbortAuto_Click(object sender, EventArgs e)
        {
            SubmitAutoReviewDecision(AbortAutoRequested, DialogResult.Cancel, "CANCEL/RETRY T ALIGN(RetryAlign)");
        }

        private void BtnBuzzerStop_Click(object sender, EventArgs e)
        {
            LogReviewAction("BUZZER-STOP", "부저 정지 요청");
            RaiseSimpleEvent(BuzzerStopRequested);
            SetStatus("부저 정지를 요청했습니다. 확인 또는 취소를 선택하세요.");
        }

        // ── Review 조작 이력 ────────────────────────────────────────────────────
        // 이 창은 운전자가 Die 상태/픽업 순서/좌표를 덮어쓰고 Auto 진행을 확정하는 화면이므로
        // 모든 조작을 이벤트 로그로 남긴다(누가 어떤 Die를 어떻게 바꿨는지 추적).
        // EventLogger.Write는 큐 적재 후 백그라운드 기록이라 UI 스레드를 붙잡지 않는다.
        // 고빈도 경로(200ms 축 위치 타이머, 프레임 수신, MouseCaptureChanged)에는 넣지 않는다.

        /// <summary>정상 조작 이력.</summary>
        private void LogReviewAction(string code, string detail)
        {
            WriteReviewLog(EventKind.Event, code, detail);
        }

        /// <summary>거부/차단된 조작(가드에 걸린 경우) 이력.</summary>
        private void LogReviewBlocked(string code, string detail)
        {
            WriteReviewLog(EventKind.Warning, code, detail);
        }

        private void WriteReviewLog(EventKind kind, string code, string detail)
        {
            try
            {
                EventLogger.Write(kind, "UI", "IN-REVIEW-" + code, "InputStageRunReview",
                    (detail ?? string.Empty) + BuildReviewLogContext());
            }
            catch
            {
                // 이력 기록 실패가 조작을 막아서는 안 된다.
            }
        }

        /// <summary>모든 Review 이력에 공통으로 붙는 문맥(웨이퍼/모드/선택 상태).</summary>
        private string BuildReviewLogContext()
        {
            try
            {
                return " [wafer=" + (_waferId ?? "-") +
                       ", mode=" + _mode +
                       (_readOnlyPreview ? ", readOnly" : string.Empty) +
                       (_autoReviewMode ? ", autoReview" : string.Empty) +
                       (_busy ? ", busy" : string.Empty) +
                       (_decisionSubmitted ? ", decided" : string.Empty) +
                       (_waferVisionControlActive ? ", visionScope" : string.Empty) +
                       ", selected=" + DescribeEntryForLog(_selectedDie) +
                       ", selectedCount=" + _selectedDies.Count +
                       ", start=" + DescribeEntryForLog(_startDie) +
                       ", pickupApplied=" + _pickupOrderApplied + "]";
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string DescribeEntryForLog(DieMapEntry entry)
        {
            if (entry == null)
                return "-";

            return (entry.DieUid ?? "-") +
                   "(map=" + entry.DieMapX + "," + entry.DieMapY +
                   " seq=" + entry.SequenceNo + ")";
        }

        /// <summary>조그 속도 표기. 콤보가 비어 있으면 실제 적용값(Fine)이 드러나도록 남긴다.</summary>
        private string DescribeJogSpeedForLog()
        {
            string text = cmbJogSpeed != null ? cmbJogSpeed.Text : null;
            return string.IsNullOrWhiteSpace(text) ? "(미선택→Fine)" : text;
        }

        /// <summary>변경 대상 Die UID 목록. 대량 선택 시 로그가 비대해지지 않도록 앞 20개만 남긴다.</summary>
        private static string BuildEntryUidListForLog(IList<DieMapEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return "-";

            const int MaxLogged = 20;
            var builder = new StringBuilder();
            int count = Math.Min(MaxLogged, entries.Count);
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    builder.Append('|');
                DieMapEntry entry = entries[i];
                builder.Append(entry != null ? (entry.DieUid ?? "-") : "-");
            }

            if (entries.Count > count)
                builder.Append("|...+").Append(entries.Count - count);

            return builder.ToString();
        }

        private void SubmitAutoReviewDecision(EventHandler handler, DialogResult result, string decisionName)
        {
            if (_decisionSubmitted)
                return;
            if (_waferVisionControlActive)
            {
                // [사용자 확정 2026-08-17] 기존 조건: Live 보유 중이면 무조건 거부 —
                //   화면 오픈 시 자동 Live가 켜지므로 CONFIRM이 항상 거부되어
                //   작업자가 STOP으로 라인을 세우는 사고가 났다(15:32 실사례 4회 거부).
                // 현재 기준: 실제 동작(이동/Jog) 중이 아니면 Live를 자동 종료하고 결정을 진행한다.
                if (_waferVisionMoveBusy || _activeJogButton != null)
                {
                    LogReviewBlocked("DECISION-BLOCKED",
                        "결정 거부: Review 이동/Jog 진행 중. decision=" + decisionName);
                    SetStatus("진행 중인 이동/Jog를 정지(STOP/버튼 놓기)한 뒤 다시 선택하세요.");
                    return;
                }

                LogReviewAction("DECISION-VISION-AUTO-STOP",
                    "결정 진행을 위해 Wafer Vision 사용을 자동 종료합니다. decision=" + decisionName);
                RaiseSimpleEvent(WaferVisionControlStopRequested);
                if (_waferVisionControlActive)
                {
                    LogReviewBlocked("DECISION-BLOCKED",
                        "결정 거부: Wafer Vision 자동 종료 실패. decision=" + decisionName);
                    SetStatus("비전 사용을 종료하지 못했습니다. '비전 사용 종료'를 직접 누른 뒤 다시 선택하세요.");
                    return;
                }
            }

            if (handler == null)
            {
                LogReviewBlocked("DECISION-NO-HANDLER",
                    "결정 거부: 장비 연결(구독자) 없음. decision=" + decisionName);
                SetStatus("사용자 확인 요청을 처리할 장비 연결이 없습니다.");
                return;
            }

            try
            {
                _decisionSubmitted = true;
                _submittedDialogResult = result;
                _busy = true;
                LogReviewAction("DECISION", "운전자 결정 제출: " + decisionName +
                    ", targetCount=" + _previewOrder.Count);
                SetStatus(result == DialogResult.OK
                    ? "확인 요청을 처리하고 있습니다. Sequence 완료 응답을 기다립니다."
                    : "재실행/취소 요청을 처리하고 있습니다. Sequence 완료 응답을 기다립니다.");
                UpdateActionAvailability();
                handler(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _decisionSubmitted = false;
                _submittedDialogResult = DialogResult.None;
                _busy = false;
                LogReviewBlocked("DECISION-EX",
                    "결정 처리 예외: decision=" + decisionName + ", error=" + ex.Message);
                SetStatus("사용자 확인 처리에 실패했습니다. " + ex.Message);
                UpdateActionAvailability();
            }
        }

        private void RaiseSimpleEvent(EventHandler handler)
        {
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void UpdateActionAvailability()
        {
            // [라이브 중 버튼 사용 2026-08-17, 팀장님 지시] 기존에는 Wafer Vision Live Scope를 잡으면
            // SetBusy(true)가 걸려 _busy=true → 액션 버튼이 전부 비활성이었다.
            // 현재 기준: Live 보유 상태(_waferVisionControlActive)는 "동작 중"이 아니라 "대기 중"이므로
            // 버튼을 잠그지 않는다. 실제 동작 중 중복 실행 차단은 _waferVisionMoveBusy가 담당한다
            // (기존 MOVE SELECTED DIE의 Scope 재사용 경로와 동일한 정책).
            bool busyBlocksActions = _busy && !_waferVisionControlActive;
            bool enabled = !busyBlocksActions;
            bool actionEnabled = enabled && !_readOnlyPreview && !_waferVisionMoveBusy;
            grpDieState.Enabled = actionEnabled && _mode == InputStageRunReviewMode.MappingReview;
            grpStartDie.Enabled = actionEnabled && _mappingComplete;
            grpJog.Enabled = !_readOnlyPreview;
            cmbJogSpeed.Enabled = actionEnabled && _activeJogButton == null;
            if (_cmbJogMode != null)
                _cmbJogMode.Enabled = actionEnabled && _activeJogButton == null;
            if (_cmbJogStep != null)
                _cmbJogStep.Enabled = actionEnabled && _activeJogButton == null && IsStepJogMode;
            btnVisionXMinus.Enabled = actionEnabled || ReferenceEquals(_activeJogButton, btnVisionXMinus);
            btnVisionXPlus.Enabled = actionEnabled || ReferenceEquals(_activeJogButton, btnVisionXPlus);
            btnWaferYMinus.Enabled = actionEnabled || ReferenceEquals(_activeJogButton, btnWaferYMinus);
            btnWaferYPlus.Enabled = actionEnabled || ReferenceEquals(_activeJogButton, btnWaferYPlus);
            btnWaferTMinus.Enabled = actionEnabled || ReferenceEquals(_activeJogButton, btnWaferTMinus);
            btnWaferTPlus.Enabled = actionEnabled || ReferenceEquals(_activeJogButton, btnWaferTPlus);
            grpActions.Enabled = actionEnabled;
            btnMoveSelectedDie.Enabled = actionEnabled && _mappingComplete;
            btnThetaCorrection.Enabled = actionEnabled && _alignComplete;
            btnDieDetection.Enabled = actionEnabled && _alignComplete && _mappingComplete;
            btnOffsetApply.Enabled = actionEnabled && _mappingComplete;
            btnVisionTest.Enabled = actionEnabled;
            // 미연결 상태에서는 Live/Grab이 no-op이면서 버튼만 눌린 상태로 남으므로 명령 자체를 잠근다.
            bool waferVisionCommandEnabled = _waferVisionControlActive &&
                                              _waferVisionLinkConnected &&
                                              !_readOnlyPreview &&
                                              !_decisionSubmitted &&
                                              !_waferVisionMoveBusy;
            waferVisionViewer.CameraCommandsEnabled = waferVisionCommandEnabled;
            btnWaferVisionControl.Enabled = !_readOnlyPreview &&
                                            (!_busy || _waferVisionControlActive) &&
                                            !_decisionSubmitted &&
                                            !_waferVisionMoveBusy;
            grpPickupRoute.Enabled = actionEnabled && _mappingComplete;
            btnPreviewPath.Enabled = actionEnabled && _mappingComplete;
            btnApplyPickupOrder.Enabled = actionEnabled && _mappingComplete;
            btnRetryAlign.Enabled = actionEnabled;
            btnRetryMapping.Enabled = actionEnabled && _alignComplete;
            btnMappingSetup.Enabled = actionEnabled && !_autoReviewMode;
            btnStartRun.Enabled = (actionEnabled || (enabled && _autoReviewMode)) &&
                                  _mode == InputStageRunReviewMode.MappingReview &&
                                  _alignComplete &&
                                  _mappingComplete &&
                                  _reviewValid &&
                                  _pickupOrderApplied &&
                                  (!_manualFallbackThetaRequired || _manualFallbackThetaDone) &&
                                  (_previewOrder.Count == 0 || !chkUseSelectedStart.Checked || _startDie != null);
            btnAbortAuto.Enabled = actionEnabled || (enabled && _autoReviewMode);
            btnBuzzerStop.Enabled = enabled;
            btnClose.Enabled = enabled && !_autoReviewMode;
            btnJogStop.Enabled = !_readOnlyPreview;
        }

        /// <summary>외부(Form1)에서 상태 문구만 갱신합니다(버튼 활성 상태는 변경하지 않습니다).</summary>
        public void SetStatusMessage(string message)
        {
            if (IsDisposed || Disposing)
                return;
            SetStatus(message);
        }

        private void SetStatus(string message)
        {
            lblStatus.Text = string.IsNullOrWhiteSpace(message) ? "-" : message;
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        protected override void OnDeactivate(EventArgs e)
        {
            if (_activeJogButton != null)
            {
                _activeJogButton = null;
                UpdateActionAvailability();
                RaiseSimpleEvent(JogStopRequested);
            }
            base.OnDeactivate(e);
        }

        private void InputStageRunReviewDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_activeJogButton != null)
            {
                _activeJogButton = null;
                LogReviewAction("JOG-STOP-CLOSE", "창 닫힘으로 연속 Jog 정지 요청");
                RaiseSimpleEvent(JogStopRequested);
            }
            // Form.Close()는 CloseReason.UserClosing으로 보고되므로
            // Sequence 종료 요청은 사용자 차단 로직보다 먼저 통과시킨다.
            if (_sequenceCloseRequested)
            {
                LogReviewAction("CLOSE", "시퀀스 요청으로 Review 창 종료. reason=" + e.CloseReason);
                StopWaferVision();
                DisposeEncoderRefreshTimer();
                return;
            }

            if (_autoReviewMode && !_decisionSubmitted && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                LogReviewBlocked("CLOSE-BLOCKED",
                    "Auto 대기 중 사용자 창 닫기 차단(결정 미제출)");
                SetStatus("Auto 대기 중에는 창을 직접 닫을 수 없습니다. 확인 또는 취소/T ALIGN 재시작을 선택하세요.");
                return;
            }

            if (_busy && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                LogReviewBlocked("CLOSE-BLOCKED", "동작 진행 중 사용자 창 닫기 차단");
                SetStatus("동작 진행 중에는 화면을 닫을 수 없습니다. 먼저 STOP 또는 작업 완료를 확인하세요.");
                return;
            }

            LogReviewAction("CLOSE", "Review 창 종료. reason=" + e.CloseReason);

            DisposeEncoderRefreshTimer();
            StopWaferVision();

            if (!_readOnlyPreview &&
                e.CloseReason == CloseReason.UserClosing &&
                !_sequenceCloseRequested &&
                !_decisionSubmitted &&
                DialogResult != DialogResult.OK)
                RaiseSimpleEvent(AbortAutoRequested);
        }

        private void DisposeEncoderRefreshTimer()
        {
            if (_encoderRefreshTimer != null)
            {
                try
                {
                    _encoderRefreshTimer.Stop();
                    _encoderRefreshTimer.Dispose();
                }
                catch
                {
                }
                _encoderRefreshTimer = null;
            }
            _axisPositionProvider = null;
        }
    }

    public enum InputStageRunReviewMode
    {
        AlignRecovery,
        MappingReview
    }

    public enum InputStageReviewDieState
    {
        Wait,
        Good,
        Ng,
        Skip
    }

    public enum InputStageReviewJogAxis
    {
        VisionX,
        WaferY,
        WaferT
    }

    public sealed class InputStageReviewJogEventArgs : EventArgs
    {
        public InputStageReviewJogEventArgs(InputStageReviewJogAxis axis, int direction, string speed)
            : this(axis, direction, speed, false, 0.0)
        {
        }

        public InputStageReviewJogEventArgs(
            InputStageReviewJogAxis axis,
            int direction,
            string speed,
            bool isStepMode,
            double stepDistance)
        {
            Axis = axis;
            Direction = direction < 0 ? -1 : 1;
            Speed = speed ?? "Fine";
            IsStepMode = isStepMode;
            StepDistance = Math.Abs(stepDistance);
        }

        public InputStageReviewJogAxis Axis { get; private set; }
        public int Direction { get; private set; }
        public string Speed { get; private set; }
        public bool IsStepMode { get; private set; }
        public double StepDistance { get; private set; }
    }

    public sealed class InputStageReviewDieStateEventArgs : EventArgs
    {
        public InputStageReviewDieStateEventArgs(IReadOnlyList<DieMapEntry> entries, InputStageReviewDieState state)
        {
            Entries = entries ?? new List<DieMapEntry>().AsReadOnly();
            State = state;
        }

        public IReadOnlyList<DieMapEntry> Entries { get; private set; }
        public InputStageReviewDieState State { get; private set; }
    }

    public sealed class InputStageReviewPickupOrderEventArgs : EventArgs
    {
        public InputStageReviewPickupOrderEventArgs(
            PickupSubset options,
            DieMapEntry startDie,
            IReadOnlyList<DieMapEntry> orderedEntries)
        {
            Options = options ?? new PickupSubset();
            StartDie = startDie;
            OrderedEntries = orderedEntries ?? new List<DieMapEntry>().AsReadOnly();
        }

        public PickupSubset Options { get; private set; }
        public DieMapEntry StartDie { get; private set; }
        public IReadOnlyList<DieMapEntry> OrderedEntries { get; private set; }
    }
}
