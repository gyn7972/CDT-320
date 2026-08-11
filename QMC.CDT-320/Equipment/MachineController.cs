using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.Common.Alarms;
using QMC.Common.Diagnostics.TactTime;
using QMC.CDT320.Bin;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Jobs;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Alarms;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.CDT320.Initialization;
using QMC.CDT320.Motion.SharedRailX;
using QMC.CDT320.Recipes;

namespace QMC.CDT320
{
    /// <summary>
    /// 작업 탭의 초기화/시작/정지/CYCLE RUN/STOP 버튼을 실제 장비 동작으로 연결하는 컨트롤러입니다.
    /// <para>
    /// CDT-320은 Input Loader/Stage, FRONT/REAR Picker, Output 라인의 각 유닛을
    /// CDT320_Machine 트리에서 순회하면서 Axis/DO/DI를 제어합니다.
    /// </para>
    /// </summary>
    public partial class MachineController
    {
        private const int MachineRuntimeStateSaveMergeIntervalMs = 1000;
        private const int MachineRuntimeStateSaveFailureRetryMs = 5000;
        private const int AlarmSequenceStopTimeoutMs = 10000;
        private const int AlarmSequenceStopPollIntervalMs = 20;

        private readonly CDT320_Machine _machine;
        private EquipmentStatus _status = EquipmentStatus.Idle;
        private CancellationTokenSource _autoCts;
        private Task _coordinatorTask;
        private QMC.CDT320.Sequencing.MachineSequenceContext _seqContext;
        private QMC.CDT320.Sequencing.AutoSequenceCoordinator _coordinator;
        private MemoryTactTimeSink _tactTimeMemorySink;
        private TactTimeRecorder _activeTactTimeRecorder;
        private readonly object _productionStatsLock = new object();
        private System.Diagnostics.Stopwatch _autoProductionStopwatch;
        private readonly object _outputReceiveTactLock = new object();
        private DateTime _lastOutputReceiveTactAt = DateTime.MinValue;
        private BinSide? _lastOutputReceiveTactSide;
        private readonly object _inspectionTactLock = new object();
        private readonly Dictionary<string, DateTime> _lastInspectionTactTimes =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        // 4개 유닛(INPUT/FRONT/REAR/OUTPUT) 동작 상태. UI 가 스냅샷으로 폴링한다. (앱 수명 동안 유지)
        private readonly QMC.CDT320.Sequencing.SequenceActivityMonitor _sequenceActivity =
            new QMC.CDT320.Sequencing.SequenceActivityMonitor();
        private int _manualBusyCount;
        private CancellationTokenSource _manualCts;
        private readonly object _inputStageRunReviewManualLock = new object();
        private bool _inputStageRunReviewManualActive;
        private string _inputStageRunReviewWaferId = string.Empty;
        private QMC.CDT320.Sequencing.MachineSequenceContext _inputStageRunReviewContext;
        private int _inputStageRunReviewActionBusyCount;
        private CancellationTokenSource _inputStageRunReviewActionCts;
        private bool _isMachineInitialized;
        private bool _isDeveloperReadyRestored;
        private MachineReadyProgress _readySequenceProgress =
            new MachineReadyProgress(MachineReadySequenceState.Idle, 0, 0, 0, "", "");
        private readonly AxisInterferenceMap _axisInterferenceMap = AxisInterferenceMap.CreateDefault();
        private readonly AxisInitializeInterlockService _axisInitializeInterlocks;
        private readonly AxisInitializeRuntime _axisInitializeRuntime;
        private readonly AxisInitializeExecutor _axisInitializeExecutor;
        private readonly AxisInitializeSequence _axisInitializeSequence;
        private readonly AxisInitializeProgressStore _axisInitializeProgressStore =
            new AxisInitializeProgressStore();
        private readonly SemaphoreSlim _axisInitializeOperationGate =
            new SemaphoreSlim(1, 1);
        private readonly object _axisInitializeCancellationLock = new object();
        private CancellationTokenSource _axisInitializeCts;
        private readonly SemaphoreSlim _alarmSequenceStopGate =
            new SemaphoreSlim(1, 1);
        private readonly object _machineRuntimeStateSaveLock = new object();
        private readonly object _machineRuntimeStateSaveRequestLock = new object();
        private bool _machineRuntimeStateSaveWorkerRunning;
        private bool _machineRuntimeStateSaveRequested;
        private bool _machineRuntimeStateDeferredSaveClosed;
        private string _pendingMachineRuntimeStateSaveReason = string.Empty;
        private int _pendingMachineRuntimeStateSaveRequestCount;
        private long _machineRuntimeStateSaveRequestVersion;
        private long _pendingMachineRuntimeStateSaveVersion;
        private long _machineRuntimeStateAuthoritativeVersion;
        private readonly object _operatorMessageLock = new object();
        private string _lastOperatorMessageKey = string.Empty;
        private DateTime _lastOperatorMessageTimeUtc = DateTime.MinValue;
        private readonly object _outputFullPreparationLock = new object();
        private bool _outputFullPreparationRequested = true;
        private string _outputFullPreparationReason = "InitialStart";
        private string _outputFullPreparationRecipeName = string.Empty;
        private readonly object _recipeRunGateLock = new object();
        private string _recipeRunGateVerifiedName = string.Empty;
        private DateTime _recipeRunGateVerifiedAtUtc = DateTime.MinValue;
        private readonly object _recipeOperationLock = new object();
        private bool _recipeApplyOperationActive;
        private CancellationTokenSource _recipeStartAttemptCts;
        private long _recipeConfigurationGeneration;
        public SharedRailXMotionService SharedRailX { get; private set; }

        public event Action<EquipmentStatus> StatusChanged;
        public event Action<string> LogMessage;
        public event Action StopRequested;
        public event Action<bool> MachineInitializedChanged;
        public event Action<AxisInitializeStepProgress> AxisInitializeStepProgressChanged;
        public event Action<MachineReadyProgress> ReadySequenceProgressChanged;
        public event Action<string, string> OperatorMessageRequested;
        public event Action<bool, string> InputStageRunReviewManualStateChanged;

        /// <summary>CDT-320 하드웨어 유닛 트리입니다.</summary>
        public CDT320_Machine Machine => _machine;

        public IReadOnlyList<TactTimeRecord> GetTactTimeSnapshot()
        {
            try
            {
                MemoryTactTimeSink sink = _tactTimeMemorySink;
                return sink != null ? sink.Snapshot() : new List<TactTimeRecord>().AsReadOnly();
            }
            catch
            {
                return new List<TactTimeRecord>().AsReadOnly();
            }
            finally
            {
            }
        }

        public Task<int> MoveSharedRailXAsync(SharedRailXMovePlan plan)
        {
            return SharedRailX != null ? SharedRailX.MoveAsync(plan) : Task.FromResult(-1);
        }

        public void ReloadSharedRailXConfig()
        {
            SharedRailX = new SharedRailXMotionService(_machine, CreateSharedRailXConfig());
            SharedRailXMotionRuntime.ServiceProvider = () => SharedRailX;
        }

        public EquipmentStatus Status => _status;
        public bool IsManualBusy => Volatile.Read(ref _manualBusyCount) > 0;
        public bool IsSequenceRunning => _coordinatorTask != null && !_coordinatorTask.IsCompleted;
        public bool IsInputStageRunReviewManualActive
        {
            get
            {
                lock (_inputStageRunReviewManualLock)
                {
                    return _inputStageRunReviewManualActive;
                }
            }
        }
        public bool IsInputStageRunReviewActionBusy
        {
            get
            {
                lock (_inputStageRunReviewManualLock)
                {
                    return _inputStageRunReviewActionBusyCount > 0;
                }
            }
        }
        public string CurrentInputStageRunReviewWaferId
        {
            get
            {
                lock (_inputStageRunReviewManualLock)
                {
                    return _inputStageRunReviewWaferId ?? string.Empty;
                }
            }
        }
        public CancellationToken InputStageRunReviewActionToken
        {
            get
            {
                lock (_inputStageRunReviewManualLock)
                {
                    CancellationTokenSource cts = _inputStageRunReviewActionCts;
                    return cts != null ? cts.Token : CancellationToken.None;
                }
            }
        }
        public CancellationToken ManualOperationToken
        {
            get
            {
                var cts = _manualCts;
                return cts != null ? cts.Token : CancellationToken.None;
            }
        }
        public bool IsMachineInitialized => _isMachineInitialized;
        public bool IsDeveloperReadyRestored => _isDeveloperReadyRestored;
        public MachineReadyProgress ReadySequenceProgress => _readySequenceProgress;
        public bool IsReadySequenceRunning => _readySequenceProgress != null && _readySequenceProgress.IsRunning;
        private bool IsAxisInitializeOperationRunning => _axisInitializeOperationGate.CurrentCount == 0;
        private bool IsRecipeApplyOperationActive
        {
            get
            {
                lock (_recipeOperationLock)
                    return _recipeApplyOperationActive;
            }
        }
        private bool IsRecipeStartAttemptActive
        {
            get
            {
                lock (_recipeOperationLock)
                    return _recipeStartAttemptCts != null;
            }
        }
        private bool HasActiveEquipmentOperation =>
            IsSequenceRunning ||
            IsManualBusy ||
            IsInputStageRunReviewManualActive ||
            IsInputStageRunReviewActionBusy ||
            IsReadySequenceRunning ||
            IsAxisInitializeOperationRunning;
        private bool HasActiveAlarmControlledOperation =>
            HasActiveEquipmentOperation ||
            IsRecipeApplyOperationActive ||
            IsRecipeStartAttemptActive;
        /// <summary>4개 유닛(INPUT/FRONT/REAR/OUTPUT) 시퀀스 동작 상태(공식 상태 객체). UI 표시용.</summary>
        public QMC.CDT320.Sequencing.SequenceActivityMonitor SequenceActivity => _sequenceActivity;
        public DateTime MachineInitializedAt { get; private set; }
        public string LastActionFailureMessage { get; private set; }
        public bool CanRunEquipment => IsMachineInitialized && _status != EquipmentStatus.Alarm && !IsSequenceRunning;

        /// <summary>
        /// 현재 장비가 자동 운전(Auto Run) 중인지 확인합니다.
        /// UI 버튼 차단과 화면 전환 제한은 이 판정을 단일 기준으로 사용합니다.
        /// </summary>
        /// <returns>자동 운전 중이면 <c>true</c>, 그 외 상태이면 <c>false</c></returns>
        public bool IsAutoRun()
        {
            return _status == EquipmentStatus.AutoRunning;
        }


        public QMC.CDT320.Sequencing.SequenceRunMode? ActiveSequenceRunMode { get; private set; }
        public string ActiveRecipeName { get; private set; } = string.Empty;
        public bool IsOutputFullPreparationRequested
        {
            get
            {
                lock (_outputFullPreparationLock)
                {
                    return _outputFullPreparationRequested;
                }
            }
        }
        // Wafer die map settings.
        /// <summary>다이 X 크기 [mm].</summary>
        public double DieSizeXMm { get; set; } = 8.12;
        /// <summary>다이 Y 크기 [mm].</summary>
        public double DieSizeYMm { get; set; } = 6.12;

        private QMC.CDT320.DieMaps.DieMap _inputDieMap;
        /// <summary>현재 InputStage 매핑 결과로 적용된 Input die map입니다.</summary>
        public QMC.CDT320.DieMaps.DieMap InputDieMap => _inputDieMap;

        // Stage 61: Pickup sequence options + cached sequence.
        /// <summary>Input/Wafer die pickup sequence options.</summary>
        public QMC.CDT320.Recipes.PickupSubset PickupOptions { get; set; } =
            new QMC.CDT320.Recipes.PickupSubset();

        /// <summary>
        /// PickupOptions 기준으로 정렬된 입력 다이 픽업 순서입니다.
        /// Input die map 적용 또는 RebuildPickupSequence 호출 시 갱신됩니다.
        /// </summary>
        private List<QMC.CDT320.DieMaps.DieMapEntry> _inputPickupSequence
            = new List<QMC.CDT320.DieMaps.DieMapEntry>();

        public IReadOnlyList<QMC.CDT320.DieMaps.DieMapEntry> InputPickupSequence
            => _inputPickupSequence;

        /// <summary>PickupOptions 또는 _inputDieMap 변경 후 호출하여 픽업 순서를 재생성합니다.</summary>
        public void RebuildPickupSequence()
        {
            if (_inputDieMap == null) { _inputPickupSequence.Clear(); return; }
            QMC.CDT320.DieMaps.PickupSequenceGenerator.ApplySequenceNumbers(_inputDieMap, PickupOptions);
            _inputPickupSequence = QMC.CDT320.DieMaps.PickupSequenceGenerator.Build(
                _inputDieMap, PickupOptions);
            Log("[PICKSEQ] " + PickupOptions.StartCorner + " / " +
                PickupOptions.Direction + " / " + PickupOptions.Pattern +
                " 활성 다이 " + _inputPickupSequence.Count + "개 순서 결정");
        }

        public void ApplyInputDieMap(QMC.CDT320.DieMaps.DieMap map, string reason)
        {
            try
            {
                if (map == null)
                    return;

                QMC.CDT320.DieMaps.PickupSequenceGenerator.ApplySequenceNumbers(map, PickupOptions);
                QMC.CDT320.DieMaps.DieMapGenerator.Normalize(map);
                _inputDieMap = map;
                QMC.CDT320.Lots.LotStorage.ActiveInputDieMap = map;
                RebuildPickupSequence();
                int targetCount = 0;
                if (map.Entries != null)
                {
                    foreach (var entry in map.Entries)
                    {
                        if (entry != null && entry.IsTarget)
                            targetCount++;
                    }
                }

                Log("[PICKSEQ] Input DieMap applied. reason=" + (reason ?? "") +
                    ", frame=" + (map.FrameObjId ?? "") +
                    ", target=" + targetCount +
                    ", sequence=" + (_inputPickupSequence != null ? _inputPickupSequence.Count : 0));
            }
            catch (Exception ex)
            {
                Log("[PICKSEQ] Input DieMap apply failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public void ClearInputDieMap(string reason)
        {
            try
            {
                _inputDieMap = null;
                _inputPickupSequence.Clear();
                QMC.CDT320.Lots.LotStorage.ActiveInputDieMap = null;
                Log("[PICKSEQ] Input DieMap cleared. reason=" + (reason ?? ""));
            }
            catch (Exception ex)
            {
                Log("[PICKSEQ] Input DieMap clear failed: " + ex.Message);
            }
            finally
            {
            }
        }

        // ------------------------------------------------------------------
        // Stage 58: 운영 통계(Work Info / Work Time)
        // WorkMainPage / StatePage에서 읽습니다.
        // internal setter는 Cycle 실행 중 누적하고 Init에서 리셋합니다.
        // ------------------------------------------------------------------
        /// <summary>PICK 실패 누적 수량(재시도 후 최종 실패로 카운트되는 경우).</summary>
        public int PickFailCount { get; internal set; }
        /// <summary>PLACE 실패 누적 수량(Output 슬롯 placement vision NG).</summary>
        public int PlaceFailCount { get; internal set; }
        /// <summary>FRONT (LEFT ARM) Collet 사용 횟수. Pick 1회당 +1.</summary>
        public int Collet1UseCount { get; internal set; }
        /// <summary>REAR (RIGHT ARM) Collet 사용 횟수.</summary>
        public int Collet2UseCount { get; internal set; }
        /// <summary>EjectPin/Needle 사용 횟수(다이 1개당 1회).</summary>
        public int NeedleUseCount { get; internal set; }
        /// <summary>알람 누적 발생 수.</summary>
        public int ErrorCount { get; internal set; }
        /// <summary>정상 다운(STOP/IDLE 등) 누적 시간.</summary>
        public TimeSpan NormalDownTime { get; internal set; } = TimeSpan.Zero;
        /// <summary>알람/에러로 인한 다운 누적 시간.</summary>
        public TimeSpan ErrorDownTime { get; internal set; } = TimeSpan.Zero;
        /// <summary>알람 발생 후 복구까지 걸린 누적 시간.</summary>
        public TimeSpan RecoveryTime { get; internal set; } = TimeSpan.Zero;
        /// <summary>Mean Time Between Failure (rolling).</summary>
        public TimeSpan Mtbf { get; internal set; } = TimeSpan.Zero;
        /// <summary>Mean Time To Recovery (rolling).</summary>
        public TimeSpan Mttr { get; internal set; } = TimeSpan.Zero;

        /// <summary>작업 시간/UPH 통계 엔진. 사이클/상태 이벤트를 먹이면 lock-free 스냅샷을 발행한다.</summary>
        public QMC.CDT320.Stats.ProductionStatsEngine Stats { get; } = new QMC.CDT320.Stats.ProductionStatsEngine();

        /// <summary>현재 InputLoader가 처리 중인 슬롯 인덱스(0-base). -1 = 미장착/미로드 상태.</summary>
        public int CurrentInputSlot { get; private set; } = -1;
        /// <summary>현재 슬롯의 웨이퍼가 InputStage 교환 위치까지 이송되었는지 여부.</summary>
        public bool InputWaferAtExchange { get; private set; } = false;
        /// <summary>LotPort 상태가 변경되는 시점에 발생합니다(UI 갱신용).</summary>
        public event Action LotPortStateChanged;

        public MachineController(CDT320_Machine machine)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            SharedRailX = new SharedRailXMotionService(_machine, CreateSharedRailXConfig());
            SharedRailXMotionRuntime.ServiceProvider = () => SharedRailX;
            _axisInitializeInterlocks = new AxisInitializeInterlockService(
                _machine,
                EnumerateAxes,
                () => !IsSequenceRunning && _status != EquipmentStatus.AutoRunning,
                () => !IsManualBusy);
            _axisInitializeRuntime = new AxisInitializeRuntime(
                _machine,
                _axisInterferenceMap,
                EnumerateAxes,
                _axisInitializeInterlocks);
            _axisInitializeExecutor = new AxisInitializeExecutor(_axisInitializeRuntime);
            // 전체 초기화 순서는 Sequence가, 선택된 한 Step의 실제 HOME은 Executor가 담당합니다.
            _axisInitializeSequence = new AxisInitializeSequence(
                _axisInitializeExecutor,
                _axisInitializeRuntime,
                _machine);
            _axisInitializeExecutor.StepProgressChanged += OnAxisInitializeExecutorStepProgressChanged;
            MotionGuardRuntime.ContextProvider = () =>
                new MotionGuardContext(_machine, EnumerateAxes(), QMC.CDT320.Ajin.CylinderManager.Items.Values);
            BaseAxis.MotionGuard = VerifyAxisMotionGuard;
            // 알람 발생 시 AlarmContext 파일에 포함할 장비 스냅샷(상태/모드/축) 제공자 등록.
            QMC.Common.Logging.LogPolicy.EquipmentSnapshotProvider = BuildAlarmContextEquipmentSnapshot;
            QMC.Common.IO.BaseCylinder.MotionGuard = VerifyCylinderMotionGuard;
        }

        private static SharedRailXConfig CreateSharedRailXConfig()
        {
            return SharedRailXConfigStore.LoadOrCreateDefault();
        }

        private static bool VerifyAxisMotionGuard(
            BaseAxis axis,
            double targetPosition,
            AxisMotionGuardKind moveKind,
            out string reason)
        {
            if (moveKind == AxisMotionGuardKind.Home)
                return MotionGuardRuntime.VerifyAxisHome(axis, out reason);

            if (moveKind == AxisMotionGuardKind.JogContinuous)
                return MotionGuardRuntime.VerifyAxisContinuousJog(axis, targetPosition, "ContinuousJog", out reason);

            if (moveKind == AxisMotionGuardKind.JogStep)
                return MotionGuardRuntime.VerifyAxisStepJog(axis, targetPosition, "StepJog", out reason);

            return MotionGuardRuntime.VerifyAxisMove(axis, targetPosition, out reason);
        }

        private static bool VerifyCylinderMotionGuard(
            QMC.Common.IO.BaseCylinder cylinder,
            bool moveFwd,
            out string reason)
        {
            return MotionGuardRuntime.VerifyCylinderMove(cylinder, moveFwd, out reason);
        }

        private bool EnsureMachineInitializedForRun(string source)
        {
            try
            {
                LastActionFailureMessage = "";
                if (_isMachineInitialized)
                    return true;

                if (TryRecoverMachineInitializedFromAxisState(source))
                    return true;

                LastActionFailureMessage = "장비 초기화가 완료되지 않았습니다. INIT 후 START를 수행하세요.";
                QMC.Common.Log.Write("Main", "SYSTEM", source,
                    "Run failed: machine is not initialized. - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "START-NOT-INITIALIZED", "MachineController",
                    LastActionFailureMessage);
                Log("[START] failed: machine is not initialized");
                return false;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", source,
                    "Run initialized check failed: " + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "START-INIT-CHECK", "MachineController",
                    "장비 초기화 상태 확인 실패. " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        /// <summary>
        /// READY 모션과 Auto Coordinator 시작 전에 Handler/Material/marker/Vision Recipe를 동일하게 맞춘다.
        /// 이름이 하나라도 다르거나 현재 MainComm의 ACK가 없으면 움직이지 않고 Alarm으로 차단한다.
        /// </summary>
        private async Task<bool> EnsureRecipeReadyForAutoStartAsync(
            string source,
            bool forceVisionRefresh,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                string activeRecipeName = NormalizeRecipeIdentity(ActiveRecipeName);
                if (string.IsNullOrWhiteSpace(activeRecipeName))
                {
                    return FailRecipeRunGate(
                        source,
                        "START-RECIPE-ACTIVE-EMPTY",
                        "활성 Recipe가 없어 자동 운전을 시작할 수 없습니다.");
                }

                string markerRecipeName = NormalizeRecipeIdentity(RecipeStore.GetLastProjectName());
                if (!string.Equals(
                        activeRecipeName,
                        markerRecipeName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return FailRecipeRunGate(
                        source,
                        "START-RECIPE-MARKER-MISMATCH",
                        "활성 Recipe와 .last_project가 다릅니다. active=" +
                        activeRecipeName + ", lastProject=" +
                        (string.IsNullOrWhiteSpace(markerRecipeName) ? "(없음)" : markerRecipeName));
                }

                MaterialSnapshot materialState = MaterialStateService.State;
                string materialRecipeName = materialState != null
                    ? NormalizeRecipeIdentity(materialState.RecipeName)
                    : string.Empty;
                if (materialState == null ||
                    !string.Equals(
                        activeRecipeName,
                        materialRecipeName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    string materialDetail;
                    string ignoredRecipeName;
                    HasInMachineMaterial(out ignoredRecipeName, out materialDetail);
                    return FailRecipeRunGate(
                        source,
                        "START-RECIPE-MATERIAL-MISMATCH",
                        "활성 Recipe와 Material Recipe가 다릅니다. 자동으로 덮어쓰지 않습니다. " +
                        "active=" + activeRecipeName +
                        ", materialRecipe=" +
                        (string.IsNullOrWhiteSpace(materialRecipeName) ? "(없음)" : materialRecipeName) +
                        ", material=" + materialDetail);
                }

                if (QMC.CDT320.VisionComm.VisionHub.IsRecipeSynchronizationBypassed)
                {
                    QMC.Common.Log.Write(
                        "Main",
                        "SYSTEM",
                        source,
                        "Vision 미사용/가상 검사 설정으로 Recipe ACK Gate를 명시적으로 우회합니다. " +
                        "recipe=" + activeRecipeName + " - Bypassed");
                    MarkRecipeRunGateVerified(activeRecipeName);
                    return true;
                }

                bool controllerGateFresh = IsRecipeRunGateFresh(activeRecipeName);
                string ackReason;
                bool visionAckFresh =
                    QMC.CDT320.VisionComm.VisionHub.IsRecipeAcknowledged(
                        activeRecipeName,
                        TimeSpan.FromMinutes(2),
                        out ackReason);

                if (forceVisionRefresh || !controllerGateFresh || !visionAckFresh)
                {
                    bool acknowledged =
                        await QMC.CDT320.VisionComm.VisionHub.BroadcastRecipeAsync(
                            activeRecipeName,
                            ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    if (!acknowledged ||
                        !QMC.CDT320.VisionComm.VisionHub.IsRecipeAcknowledged(
                            activeRecipeName,
                            TimeSpan.FromMinutes(2),
                            out ackReason))
                    {
                        return FailRecipeRunGate(
                            source,
                            "START-RECIPE-VISION-NO-ACK",
                            "현재 Vision MainComm에서 활성 Recipe ACK를 받지 못했습니다. " +
                            "recipe=" + activeRecipeName +
                            ", detail=" + (ackReason ?? string.Empty));
                    }
                }

                ct.ThrowIfCancellationRequested();
                string activeRecipeAfter = NormalizeRecipeIdentity(ActiveRecipeName);
                string markerRecipeAfter = NormalizeRecipeIdentity(RecipeStore.GetLastProjectName());
                MaterialSnapshot materialStateAfter = MaterialStateService.State;
                string materialRecipeAfter = materialStateAfter != null
                    ? NormalizeRecipeIdentity(materialStateAfter.RecipeName)
                    : string.Empty;
                if (!string.Equals(activeRecipeName, activeRecipeAfter, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(activeRecipeName, markerRecipeAfter, StringComparison.OrdinalIgnoreCase) ||
                    materialStateAfter == null ||
                    !string.Equals(activeRecipeName, materialRecipeAfter, StringComparison.OrdinalIgnoreCase))
                {
                    return FailRecipeRunGate(
                        source,
                        "START-RECIPE-CHANGED-DURING-CHECK",
                        "START Recipe 확인 중 Recipe/Material 상태가 변경되었습니다. " +
                        "activeBefore=" + activeRecipeName +
                        ", activeAfter=" + activeRecipeAfter +
                        ", lastProject=" + markerRecipeAfter +
                        ", materialRecipe=" + materialRecipeAfter);
                }

                MarkRecipeRunGateVerified(activeRecipeName);
                QMC.Common.Log.Write(
                    "Main",
                    "SYSTEM",
                    source,
                    "Auto START Recipe Gate 통과. recipe=" + activeRecipeName +
                    ", materialRecipe=" + materialRecipeName +
                    ", lastProject=" + markerRecipeName +
                    ", visionAck=" +
                    QMC.CDT320.VisionComm.VisionHub.AcknowledgedRecipeName +
                    " - Ok");
                return true;
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = "START Recipe 정합성 확인이 STOP/Alarm 요청으로 취소되었습니다.";
                QMC.Common.Log.Write(
                    "Main",
                    "SYSTEM",
                    source,
                    LastActionFailureMessage + " - Canceled");
                return false;
            }
            catch (Exception ex)
            {
                return FailRecipeRunGate(
                    source,
                    "START-RECIPE-CHECK-EX",
                    "START 전 Recipe 정합성 확인 중 예외가 발생했습니다. " + ex.Message);
            }
        }

        private bool FailRecipeRunGate(string source, string alarmCode, string message)
        {
            LastActionFailureMessage = message;
            QMC.Common.Log.Write(
                "Main",
                "SYSTEM",
                source,
                "Auto START Recipe Gate 실패. " + message + " - Failed");
            AlarmManager.Raise(
                AlarmSeverity.Error,
                alarmCode,
                "Recipe",
                message);
            Log("[START] Recipe Gate failed. code=" + alarmCode + ", detail=" + message);
            SetStatus(EquipmentStatus.Alarm);
            InvalidateRecipeRunReadiness();
            return false;
        }

        private bool IsRecipeRunGateFresh(string recipeName)
        {
            lock (_recipeRunGateLock)
            {
                return
                    string.Equals(
                        _recipeRunGateVerifiedName,
                        recipeName,
                        StringComparison.OrdinalIgnoreCase) &&
                    DateTime.UtcNow - _recipeRunGateVerifiedAtUtc <= TimeSpan.FromMinutes(2);
            }
        }

        private void MarkRecipeRunGateVerified(string recipeName)
        {
            lock (_recipeRunGateLock)
            {
                _recipeRunGateVerifiedName = recipeName ?? string.Empty;
                _recipeRunGateVerifiedAtUtc = DateTime.UtcNow;
            }
        }

        private static string NormalizeRecipeIdentity(string recipeName)
        {
            if (string.IsNullOrWhiteSpace(recipeName))
                return string.Empty;

            return System.IO.Path.GetFileNameWithoutExtension(recipeName.Trim());
        }

        /// <summary>
        /// Ready 모션 전에 InputStage/InputFeeder의 활성 wafer와 원본 Cassette/Slot Material 정합성을 확인한다.
        /// 원본을 식별할 수 없는 상태에서 자동 시퀀스를 시작하면 Input/Picker가 서로 완료 신호만 기다릴 수 있으므로
        /// 자동 복구하거나 임의 데이터를 만들지 않고 Error Alarm으로 시작을 차단한다.
        /// </summary>
        private bool EnsureActiveInputWaferSourceForAutoStart(string source)
        {
            try
            {
                string reason;
                if (MaterialStateService.TryValidateActiveInputWaferSourceState(out reason))
                    return true;

                LastActionFailureMessage =
                    "InputStage/InputFeeder 제품의 원본 Cassette/Slot 정보를 확인할 수 없습니다. " +
                    reason +
                    " Material 데이터를 실제 장비 상태와 일치시킨 후 다시 START 하십시오.";
                QMC.Common.Log.Write(
                    "Main",
                    "SYSTEM",
                    source,
                    "Auto start blocked by active Input wafer source Material mismatch. " +
                    LastActionFailureMessage + " - Failed");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "START-IN-MATERIAL-SOURCE-NOT-READY",
                    "Material",
                    LastActionFailureMessage);
                Log("[START] failed: active Input wafer source Material mismatch. " + reason);
                return false;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage =
                    "START 전 활성 Input wafer 원본 Material 확인 중 예외가 발생했습니다. " + ex.Message;
                QMC.Common.Log.Write(
                    "Main",
                    "SYSTEM",
                    source,
                    LastActionFailureMessage + " - Failed");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "START-IN-MATERIAL-SOURCE-CHECK-EX",
                    "Material",
                    LastActionFailureMessage);
                Log("[START] failed: active Input wafer source Material check exception. " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 자동 운전 시작 전 활성 LOT 확인.
        /// [사용자 확정 2026-07-27] 활성 LOT이 없으면 자동 운전을 시작하지 않는다.
        /// 임시 LOT을 자동 생성하지 않는다 — 어떤 LOT으로 생산했는지 불명확해지는 것을 막기 위함.
        /// 알람이 아니라 사용자 안내로 처리한다(설비 이상이 아니라 작업 절차 누락이므로 Alarm 상태로 만들지 않는다).
        /// Manual / Step 모드는 이 검사를 적용하지 않는다.
        /// </summary>
        private bool EnsureActiveLotForAutoStart(string source)
        {
            try
            {
                if (QMC.CDT320.Lots.LotSessionService.IsLotActive)
                    return true;

                LastActionFailureMessage =
                    "자동 운전 시작 불가: 진행 중인 LOT이 없습니다. " +
                    "[작업 → 메인 화면 → 작업 정보]에서 LOT ID를 입력하고 [LOT 시작]을 누른 뒤 다시 START 하세요.";
                QMC.Common.Log.Write("Main", "SYSTEM", source, LastActionFailureMessage + " - Failed");
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning, "LOT", "LOT-START-REQUIRED", LastActionFailureMessage);
                Log("[START] blocked: no active lot.");
                return false;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "자동 운전 시작 전 LOT 확인 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", source, LastActionFailureMessage + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private bool EnsureReticleAvoidForAutoStart(string source)
        {
            try
            {
                string reason;
                if (IsReticleAvoidForAutoStart(out reason))
                    return true;

                LastActionFailureMessage = "자동 운전 시작 불가: " + reason;
                QMC.Common.Log.Write("Main", "SYSTEM", source, LastActionFailureMessage + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "START-RETICLE-AVOID", "MachineController", LastActionFailureMessage);
                Log("[START] failed: reticle is not avoid. " + reason);
                SetStatus(EquipmentStatus.Alarm);
                return false;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "자동 운전 시작 전 Reticle 안전 상태 확인 실패. " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", source, LastActionFailureMessage + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "START-RETICLE-CHECK-EX", "MachineController", LastActionFailureMessage);
                Log("[START] failed: reticle check exception. " + ex.Message);
                SetStatus(EquipmentStatus.Alarm);
                return false;
            }
            finally
            {
            }
        }

        private bool IsReticleAvoidForAutoStart(out string reason)
        {
            reason = string.Empty;

            VisionUnit vision = _machine != null ? _machine.VisionUnit : null;
            if (vision == null)
            {
                reason = "VisionUnit을 찾을 수 없어 Reticle 안전 상태를 확인할 수 없습니다.";
                return false;
            }

            if (MotionGuardRuleHelpers.IsReticleRetracted(_machine))
                return true;

            reason = "오토 시작 전 Reticle은 반드시 안전 복귀 상태여야 합니다. " +
                     MotionGuardRuleHelpers.BuildReticleStateDetail(vision);
            return false;
        }

        private bool EnsureCalibrationReadyForAutoStart(string source)
        {
            try
            {
                string reason;
                if (IsCalibrationReadyForAutoStart(out reason))
                    return true;

                LastActionFailureMessage = "자동 운전 시작 불가: " + reason;
                QMC.Common.Log.Write("Main", "SYSTEM", source, LastActionFailureMessage + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "START-CALIBRATION-NOT-READY", "CalibrationData", LastActionFailureMessage);
                Log("[START] failed: calibration data is not ready. " + reason);
                SetStatus(EquipmentStatus.Alarm);
                return false;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "자동 운전 시작 전 CalibrationData 확인 실패. " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", source, LastActionFailureMessage + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "START-CALIBRATION-CHECK-EX", "CalibrationData", LastActionFailureMessage);
                Log("[START] failed: calibration check exception. " + ex.Message);
                SetStatus(EquipmentStatus.Alarm);
                return false;
            }
            finally
            {
            }
        }

        private bool IsCalibrationReadyForAutoStart(out string reason)
        {
            reason = string.Empty;
            try
            {
                CalibrationData data = CalibrationCoordinateService.ResolveData(_machine);
                if (data == null)
                {
                    reason = "CalibrationData가 준비되지 않았습니다.";
                    return false;
                }

                data.EnsureObjects();
                // Camera Calibration check disabled for auto start.
                // if (data.Camera == null || !data.Camera.Valid)
                // {
                //     reason = "Camera Calibration이 유효하지 않습니다. Bottom/Input/Output 카메라 Reticle 캘리브레이션을 완료하세요.";
                //     return false;
                // }

                if (data.Needle == null || !data.Needle.Valid)
                {
                    reason = "Needle Calibration이 유효하지 않습니다. Input 카메라와 NeedleX 캘리브레이션을 완료하세요.";
                    return false;
                }

                if (!CalibrationCoordinateService.AreAllColletsValid(_machine))
                {
                    reason = "Collet Calibration이 유효하지 않습니다. Front/Rear Collet 1~4 전체 캘리브레이션을 완료하세요.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "CalibrationData 유효성 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool ShouldBypassCameraCalibrationForAutoStart()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings != null &&
                    (settings.SimulationMode ||
                     settings.DryRunMode ||
                     settings.BypassHardware ||
                     !settings.UseAjin ||
                     !settings.UseVision))
                    return true;

                return DryRun || GlobalDryRun;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool TryRecoverMachineInitializedFromAxisState(string reason)
        {
            try
            {
                if (_isMachineInitialized)
                    return true;

                string notReadyReason;
                if (!AreAllAxesInitializedAndReady(out notReadyReason))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "MachineInitializedRecover",
                        "Machine initialized state recover skipped. reason=" + reason +
                        ", notReady=" + notReadyReason + " - Check");
                    return false;
                }

                SetMachineInitialized(true, "RecoveredByAxisState:" + reason, true);
                if (_status == EquipmentStatus.Stopped || _status == EquipmentStatus.Idle)
                    SetStatus(EquipmentStatus.Ready);

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineInitializedRecover",
                    "Machine initialized state recovered by axis state. reason=" + reason + " - Ok");
                return true;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineInitializedRecover",
                    "Machine initialized state recover failed. reason=" + reason +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private bool AreAllAxesInitializedAndReady(out string reason)
        {
            reason = string.Empty;
            try
            {
                foreach (var axis in EnumerateAxes())
                {
                    if (axis == null)
                        continue;

                    try { axis.UpdateStatus(); } catch { }

                    if (!axis.IsServoOn)
                    {
                        reason = axis.Name + " Servo OFF";
                        return false;
                    }

                    if (axis.IsAlarm)
                    {
                        reason = axis.Name + " Alarm ON";
                        return false;
                    }

                    if (!axis.IsHomeDone)
                    {
                        reason = axis.Name + " Home 미완료";
                        return false;
                    }

                    if (axis.IsMoving)
                    {
                        reason = axis.Name + " 이동 중";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private bool VerifyInitializeCompletionSafety(out string reason)
        {
            reason = string.Empty;
            try
            {
                if (!AreAllAxesInitializedAndReady(out reason))
                    return false;

                if (!MotionGuardRuleHelpers.IsReticleRetracted(_machine))
                {
                    reason = "Reticle이 Down 및 Front/Rear Slide Bwd 안전 복귀 상태가 아닙니다.";
                    return false;
                }

                OutputStageUnit outputStage = _machine != null ? _machine.OutputStageUnit : null;
                if (outputStage == null)
                {
                    reason = "OutputStageUnit을 찾을 수 없습니다.";
                    return false;
                }

                if (!OutputStageInterlockRules.VerifyNgClampSafeForStageMove(
                    outputStage,
                    "InitializeComplete",
                    out reason))
                    return false;

                if (!outputStage.IsBinGuideDown(BinSide.Good))
                {
                    reason = "Good Bin Guide Lift가 Down 상태가 아닙니다.";
                    return false;
                }

                if (!PickerZoneInterlockRules.VerifyPickerXGlobalMachineClearance(
                    _machine,
                    "InitializeComplete",
                    out reason))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                reason = "전체 초기화 완료 안전 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        /// <summary>전 축의 운전 준비 상태(ServoOn·!Alarm·HOME END(IsHomeDone)·!Moving)를 라이브로 확인한다.
        /// UI 등 외부 게이트에서 호출하는 공개 래퍼. 준비 안 됐으면 false + 사유.</summary>
        public bool AreAllAxesHomeReady(out string reason)
        {
            return AreAllAxesInitializedAndReady(out reason);
        }

        // ------------------------------------------------------------------
        // LotPort / legacy loader helper
        // ------------------------------------------------------------------

        /// <summary>
        /// InputLoader의 다음 웨이퍼를 InputStage 교환 위치까지 자동 이송합니다.<br/>
        /// 1) WaferMap이 비어 있으면 ScanCassetteAsync로 매핑<br/>
        /// 2) <see cref="CurrentInputSlot"/> 다음 웨이퍼 보유 슬롯으로 LifterZ 이동<br/>
        /// 3) MoveToExchangePositionAsync 호출(피더 하강 후 클램프, Y 전진)
        /// </summary>
        /// <returns>다음 웨이퍼 이송 성공 시 true. 카세트가 비었거나 인터락 차단 시 false.</returns>
        public async Task<bool> LoadNextWaferAsync()
        {
            var cassette = _machine.InputCassetteUnit;
            var feeder = _machine.InputFeederUnit;

            // 선택 규격의 카세트 센서 두 점이 모두 감지되어야 로딩을 허용한다.
            int cassetteSize = cassette.Config != null
                ? MaterialStateService.ResolveWaferSizeInch(cassette.Config.InchSelect)
                : 0;
            if (!DryRun && !cassette.IsWaferCassettePresentAll(cassetteSize))
            {
                AlarmManager.Raise(AlarmSeverity.Error, "LOT-NOCASS",
                    cassette.Name, "Input cassette is not fully detected. Both cassette sensors must be ON.");
                Log("[LOTPORT] InputCassette is not fully detected. Load skipped.");
                return false;
            }

            // 매핑 미수행 시 카세트 스캔
            if (cassette.WaferMap == null || cassette.WaferMap.Count == 0)
            {
                Log("[LOTPORT] WaferMap is empty. Scan cassette.");
                bool scanned = (await cassette.ScanCassetteAsync(16, 6.0)) == 0;
                if (!scanned)
                {
                    AlarmManager.Raise(AlarmSeverity.Error, "LOT-SCAN",
                        cassette.Name, "Input cassette scan failed.");
                    return false;
                }
                RaiseLotPortChanged();
            }

            // 다음 웨이퍼 슬롯 검색.
            int next = -1;
            for (int s = CurrentInputSlot + 1; s < cassette.WaferMap.Count; s++)
            {
                if (cassette.WaferMap[s]) { next = s; break; }
            }
            if (next < 0)
            {
                Log("[LOTPORT] No more wafer in input cassette");
                return false;
            }

            // 이동 + 교환 위치 전진.
            double slotPitch = 6.0;
            //double targetZ = loader.Setup.FirstSlotPosition + next * slotPitch;
            // To do: [레벨 분리 스캔] FirstSlotPosition이 레벨별로 분리되어 2단 값으로 참조 변경(레거시 LOTPORT 경로).
            double targetZ = cassette.Recipe.Level2FirstSlotPosition + next * slotPitch;
            Log($"[LOTPORT] Move to slot {next} (Z={targetZ:F2}mm)");
            try
            {
                int moveResult = await cassette.MoveToTargetSlotAsync(targetZ);
                if (moveResult != 0)
                    return false;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "LOT-MOVE",
                    cassette.Name, "Slot move failed: " + ex.Message);
                Log("[LOTPORT ERROR] " + ex.Message);
                return false;
            }

            int ex2 = await feeder.MoveToExchangePositionAsync();
            if (ex2 != 0)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "LOT-EX",
                    feeder.Name, "Exchange position move failed.");
                return false;
            }

            CurrentInputSlot = next;
            InputWaferAtExchange = true;
            RaiseLotPortChanged();
            Log($"[LOTPORT] LoadNextWafer OK. slot={next}");

            // Stage 34: Sim 모드에서는 소비된 슬롯을 false로 마킹합니다(UI LED 정확도).
            // Form1.CassetteDriver는 internal이지만 같은 어셈블리이므로 reflection 없이 접근 가능합니다.
            try
            {
                var hostType = Type.GetType("QMC.CDT_320.Form1, QMC.CDT-320");
                if (hostType != null)
                {
                    var hostInstances = System.Windows.Forms.Application.OpenForms;
                    foreach (System.Windows.Forms.Form f in hostInstances)
                    {
                        var driverProp = f.GetType().GetProperty("CassetteDriver",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (driverProp == null) continue;
                        var driver = driverProp.GetValue(f) as QMC.CDT320.Sim.SimCassetteDriver;
                        if (driver != null)
                        {
                            driver.SetInputSlotWafer(next, false);
                            break;
                        }
                    }
                }
            }
            catch { /* best-effort */ }

            // Stage 28: 작업 흐름 반영 InputStage handoff 시퀀스.
            // 1. 피더가 ExchangePosition으로 전진해 웨이퍼를 InputStage 입구로 보냅니다.
            // 2. InputStage.LoadAndPrepareWaferAsync에서 ExpanderZ 클램프로 Wafer를 받습니다.
            // 3. RetractFeeder로 피더를 복귀합니다.
            // 4. InputStage.VisionAlignAndSetupOriginAsync에서 정렬 + Origin을 설정합니다.
            try
            {
                Log("[LOTPORT] InputStage handoff (LoadAndPrepare) start...");
                Log("[LOTPORT] LoadAndPrepareWaferAsync is not active in InputStageUnit. Skip handoff call.");
                int handoff = 0;
                Log("[LOTPORT] InputStage handoff " + (handoff == 0 ? "OK" : "WARN"));

                // Stage 58 문서 정합: InputStage 시퀀스 실패 시 AlarmManager.Raise를 보강합니다.
                // 이전에는 Console.WriteLine만 있어 UI 알람 배너/히스토리에 반영되지 않았습니다.
                if (handoff != 0)
                {
                    AlarmManager.Raise(AlarmSeverity.Error, "IS-LOAD",
                        _machine.InputStageUnit.Name,
                        "LoadAndPrepareWafer failed. Check feeder safe position, ExpanderZ, and barcode readiness.");
                    ErrorCount++;
                }

                if (handoff == 0)
                {
                    // 피더 후퇴(웨이퍼는 이미 InputStage가 잡고 있습니다).
                    Log("[LOTPORT] Retract feeder. InputStage continues standalone work.");
                    await feeder.RetractFeederAsync();
                    InputWaferAtExchange = false;
                    RaiseLotPortChanged();

                    // VisionAlign + Origin 설정.
                    Log("[INPUTSTAGE] VisionAlign start...");
                    Log("[INPUTSTAGE] VisionAlignAndSetupOriginAsync is not active in InputStageUnit. Skip align call.");
                    int aligned = 0;
                    Log("[INPUTSTAGE] VisionAlign " + (aligned == 0 ? "OK" : "WARN (simulation limit)"));

                    if (aligned != 0)
                    {
                        AlarmManager.Raise(AlarmSeverity.Error, "IS-ALIGN",
                            _machine.InputStageUnit.Name,
                            "VisionAlignAndSetupOrigin failed. Check vision communication and StageT alarm.");
                        ErrorCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("[LOTPORT] InputStage handoff exception: " + ex.Message);
                AlarmManager.Raise(AlarmSeverity.Error, "IS-EXCEPTION",
                    _machine.InputStageUnit.Name, ex.Message);
                ErrorCount++;
            }

            return true;
        }

        // ------------------------------------------------------------------
        // LotPort state notification
        // ------------------------------------------------------------------

        public void ApplyInputCassetteMappingCompleted()
        {
            try
            {
                CurrentInputSlot = -1;
                RaiseLotPortChanged();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void RaiseLotPortChanged()
        {
            var h = LotPortStateChanged;
            if (h != null) try { h(); } catch { }
        }

        // ------------------------------------------------------------------
        // Stage 32: equipment maintenance lifecycle
        // ------------------------------------------------------------------

        /// <summary>설비 정상 종료 시퀀스입니다. 사이클 정지, 축 Stop, Lot 정리를 수행합니다.</summary>
        public async Task ShutdownAsync()
        {
            Log("[SHUTDOWN] Normal equipment shutdown start...");
            try
            {
                SetMachineInitialized(false, "Shutdown", false);
                await Task.Delay(500);
                foreach (var ax in EnumerateAxes())
                {
                    ax.Stop();
                }

                // 프로그램 종료시 서보 OFF 해야하나? 우선 막자.
                //foreach (var ax in EnumerateAxes())
                //{
                //    try { ax.ServoOff(); } catch { }
                //}

                // [LOT 관리 2026-07-27] 프로그램 종료는 LOT 종료가 아니다.
                // 예전에는 여기서 CloseLot(aborted:true) 로 LOT을 강제 중단시켰다.
                // LOT은 여러 카세트에 걸치고 완료는 작업자가 [LOT 완료]를 누를 때만 발생하므로,
                // 종료 시에는 진행 카운터만 파일에 남기고 Running 상태를 유지한다.
                LotStorage.SaveActiveLotProgress();
                AppSettingsStore.Save();
                SaveMachineRuntimeState("Shutdown");
                SetStatus(EquipmentStatus.Stopped);
                Log("[SHUTDOWN] Equipment shutdown complete.");
            }
            catch (Exception ex)
            {
                Log("[SHUTDOWN] exception: " + ex.Message);
            }
        }

        /// <summary>RESET ALARM: 모든 축 알람 리셋 + AlarmManager 활성 알람 해제.
        /// 알람 해제 후 전체 축이 정상 상태이면 총괄 초기화 상태를 복구한다.</summary>
        public async Task ResetAlarmAsync()
        {
            Log("[RESET-ALARM] Alarm reset start...");
            int axisCount = 0, axisFail = 0;
            int alarmGenerationAtRequest = GetLatestAlarmRecordId();
            IReadOnlyList<AlarmRecord> activeAlarmsAtRequest = AlarmManager.Active;
            List<int> alarmIdsAtRequest = activeAlarmsAtRequest != null
                ? activeAlarmsAtRequest.Select(x => x.Id).ToList()
                : new List<int>();
            bool alarmStopGateEntered = false;
            try
            {
                await _alarmSequenceStopGate.WaitAsync().ConfigureAwait(false);
                alarmStopGateEntered = true;

                if (HasActiveAlarmControlledOperation)
                {
                    Task runningTask = _coordinatorTask;
                    string taskStatus = runningTask != null ? runningTask.Status.ToString() : "null";
                    string activeState = BuildAlarmControlledOperationState();
                    Log("[RESET-ALARM] Active operation cancellation wait start. taskStatus=" +
                        taskStatus + ", " + activeState);
                    QMC.Common.Log.Write("Main", "SYSTEM", "ResetAlarm",
                        "Alarm reset waits for active operation cancellation. taskStatus=" +
                        taskStatus + ", " + activeState + " - Wait");

                    int stopResult = await StopSequenceForAlarmCoreAsync("RESET-ALARM").ConfigureAwait(false);
                    if (stopResult != 0 || HasActiveAlarmControlledOperation)
                    {
                        activeState = BuildAlarmControlledOperationState();
                        LastActionFailureMessage = "알람 리셋 전 실행 중인 동작을 완전히 정지하지 못했습니다.";
                        Log("[RESET-ALARM] " + LastActionFailureMessage +
                            " result=" + stopResult + ", " + activeState);
                        QMC.Common.Log.Write("Main", "SYSTEM", "ResetAlarm",
                            LastActionFailureMessage + " result=" + stopResult +
                            ", " + activeState + " - Failed");
                        return;
                    }

                    Log("[RESET-ALARM] Active operation cancellation wait complete.");
                    QMC.Common.Log.Write("Main", "SYSTEM", "ResetAlarm",
                        "Active operations terminated before alarm reset. - Ok");
                }

                if (GetLatestAlarmRecordId() > alarmGenerationAtRequest)
                {
                    LastActionFailureMessage =
                        "알람 리셋 요청 이후 새 알람이 발생하여 리셋을 차단했습니다.";
                    Log("[RESET-ALARM] " + LastActionFailureMessage);
                    QMC.Common.Log.Write("Main", "SYSTEM", "ResetAlarm",
                        LastActionFailureMessage + " - Failed");
                    return;
                }

                foreach (var ax in EnumerateAxes())
                {
                    try { ax.ResetAlarm(); axisCount++; }
                    catch { axisFail++; }
                }

                if (HasActiveAlarmControlledOperation ||
                    GetLatestAlarmRecordId() > alarmGenerationAtRequest)
                {
                    LastActionFailureMessage =
                        "알람 리셋 완료 전 새 알람 또는 실행 중인 동작이 확인되어 알람 해제를 차단했습니다.";
                    Log("[RESET-ALARM] " + LastActionFailureMessage + " " +
                        BuildAlarmControlledOperationState());
                    QMC.Common.Log.Write("Main", "SYSTEM", "ResetAlarm",
                        LastActionFailureMessage + " " +
                        BuildAlarmControlledOperationState() + " - Failed");
                    return;
                }

                int activeBefore = alarmIdsAtRequest.Count;
                foreach (int alarmId in alarmIdsAtRequest)
                    AlarmManager.Clear(alarmId);

                if (AlarmManager.HasActive)
                {
                    LastActionFailureMessage =
                        "알람 리셋 중 새 알람이 발생하여 Alarm 상태를 유지합니다.";
                    SetStatus(EquipmentStatus.Alarm);
                    Log("[RESET-ALARM] " + LastActionFailureMessage);
                    QMC.Common.Log.Write("Main", "SYSTEM", "ResetAlarm",
                        LastActionFailureMessage + " - Failed");
                    return;
                }

                Log("[RESET-ALARM] Complete (axis=" + axisCount + ", fail=" + axisFail +
                    ", active alarms cleared=" + activeBefore + ")");

                // 알람 해제 후에는 장비가 자동으로 대기/가동 상태가 된 것이 아니므로 Stopped로 둔다.
                if (_status == EquipmentStatus.Alarm) SetStatus(EquipmentStatus.Stopped);

                if (AlarmManager.HasActive ||
                    GetLatestAlarmRecordId() > alarmGenerationAtRequest)
                {
                    LastActionFailureMessage =
                        "알람 리셋 직후 새 알람이 확인되어 Alarm 상태를 복구했습니다.";
                    SetStatus(EquipmentStatus.Alarm);
                    Log("[RESET-ALARM] " + LastActionFailureMessage);
                    QMC.Common.Log.Write("Main", "SYSTEM", "ResetAlarm",
                        LastActionFailureMessage + " - Failed");
                    return;
                }

                TryRecoverMachineInitializedFromAxisState("ResetAlarm");

                // Tower Lamp OFF(알람 해제).
                if (!AlarmManager.HasActive)
                {
                    try { _machine.OpPanelUnit?.TowerLampOff(); } catch { }
                }
            }
            catch (Exception ex)
            {
                Log("[RESET-ALARM] exception: " + ex.Message);
            }
            finally
            {
                if (alarmStopGateEntered)
                    _alarmSequenceStopGate.Release();
            }
        }

        /// <summary>
        /// 비상 정지 요청입니다. 모든 축 EStop 후 알람을 발생시킵니다.
        /// TowerLamp 제어 결과를 명시적으로 로그에 남깁니다.
        /// </summary>
        public Task EmergencyStopAsync()
        {
            Log("[E-STOP] Emergency stop start...");
            try
            {
                CancelRecipeStartAttempt("EmergencyStop");
                MotionGuardRuntime.CancelPickerYCollisionRecoveryJog(null);
                SetMachineInitialized(false, "EmergencyStop", false);
                int axTotal = 0, axFail = 0;
                foreach (var ax in EnumerateAxes())
                {
                    try { ax.EStop(); axTotal++; }
                    catch (Exception axEx) { axFail++; Log("[E-STOP] Axis EStop failed: " + axEx.Message); }
                }
                // E-STOP 정책: 모든 축 Servo OFF. 사용자가 복구 또는 재초기화 후 다시 운전한다.
                foreach (var ax in EnumerateAxes())
                {
                    try { ax.ServoOff(); } catch { }
                }
                AlarmManager.Raise(AlarmSeverity.Critical, "E-STOP", "Machine",
                    "Emergency stop was triggered by user safety operation.");
                SaveMachineRuntimeState("EmergencyStop");
                SetStatus(EquipmentStatus.Alarm);

                // Stage 45: Tower Lamp 알람(빨강 + 부저). 결과를 명시적으로 기록합니다.
                if (_machine.OpPanelUnit != null)
                {
                    try
                    {
                        _machine.OpPanelUnit.TowerLampAlarm();
                        Log("[E-STOP] TowerLamp ALARM OK (red + buzzer)");
                    }
                    catch (Exception lampEx)
                    {
                        Log("[E-STOP] TowerLamp control failed: " + lampEx.Message);
                        AlarmManager.Raise(AlarmSeverity.Error, "TOWER-FAIL", "OpPanel", lampEx.Message);
                    }
                }
                else
                {
                    Log("[E-STOP] OpPanel is not connected. TowerLamp control skipped.");
                }
                Log("[E-STOP] Emergency stop complete (axis=" + axTotal + ", fail=" + axFail +
                    "). Safety check, RESET ALARM, and INIT are required.");
            }
            catch (Exception ex)
            {
                Log("[E-STOP] exception: " + ex.Message);
            }
            return Task.CompletedTask;
        }

        // ------------------------------------------------------------------
        // Operation mode (DryRun) - Stage 13
        // ------------------------------------------------------------------

        /// <summary>true이면 모션 없이 진행만 수행합니다(Recipe.DryRun 영향).</summary>
        public bool DryRun { get; set; } = false;
        public bool GlobalDryRun { get; set; } = false;

        /// <summary>현재 활성 RecipeProject의 운전 설정을 적용합니다.</summary>
        public void ApplyRecipeMode(QMC.CDT320.Recipes.RecipeProject p)
        {
            if (p == null) return;
            DryRun = GlobalDryRun || p.DryRun;
            // Stage 54: Recipe.Output 파라미터 적용.
            if (p.Output != null)
            {
                // Stage 58: Plate MaxSlots 갱신.
                if (p.Output.GoodPlateMaxSlots > 0)
                    PlateRegistry.GoodPlate.MaxSlots = p.Output.GoodPlateMaxSlots;
                if (p.Output.NgPlateMaxSlots > 0)
                    PlateRegistry.NgPlate.MaxSlots = p.Output.NgPlateMaxSlots;
            }
            // Stage 61 - Input/Wafer pickup sequence options.
            QMC.CDT320.Recipes.PickupSubset inputPickup = p.InputPickup ?? p.Pickup;
            if (inputPickup != null)
            {
                PickupOptions = inputPickup;
                RebuildPickupSequence();
                Log($"[MODE] InputPickup={inputPickup.StartCorner}/{inputPickup.Direction}/{inputPickup.Pattern}");
            }
            Log($"[MODE] DryRun={DryRun}  EbrMode={p.EbrMode}");
        }

        // ------------------------------------------------------------------
        // Central motion helper (interlock verification + real move)
        // ------------------------------------------------------------------

        /// <summary>
        /// 인터락 검증 후 축 위치 이동을 수행합니다.
        /// 차단되면 실패 코드를 반환하고 알람을 발생시킵니다.
        /// MotionInterlock.OnVerifyToMove와 같은 역할입니다.
        /// </summary>
        public async Task<int> MoveAxisAsync(BaseAxis axis, double position, double velocity = 800.0)
        {
            try
            {
                if (axis == null)
                    return -1;

                if (!InterlockRegistry.VerifyMove(axis.Name, position, out string reason))
                {
                    AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK", axis.Name,
                        $"이동 인터락 차단. target={position:F1}, reason={reason}");
                    Log($"[INTERLOCK] {axis.Name} target={position:F1} blocked: {reason}");
                    return -1;
                }

                if (DryRun)
                {
                    Log($"[DRYRUN] skip move {axis.Name} target={position:F1} (vel={velocity:F0})");
                    return 0;
                }

                int moveResult = await SharedRailXMotionRuntime.MoveAxisAsync(axis, position, velocity).ConfigureAwait(false);
                if (moveResult != 0 || axis.IsAlarm)
                {
                    string message = BuildAxisMotionFailureMessage(axis, "Move failed", moveResult);
                    AlarmManager.Raise(AlarmSeverity.Error, "MOVE-AXIS", axis.Name, message);
                    Log("[MOVE] " + message);
                    return moveResult != 0 ? moveResult : -1;
                }

                // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string axisName = axis != null ? axis.Name : "-";
                string message = "축 이동 중 예외가 발생했습니다. axis=" + axisName +
                    ", target=" + position + ", error=" + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "MOVE-AXIS-EXCEPTION", axisName, message);
                Log("[MOVE] " + message);
                return -1;
            }
            finally
            {
                Log("[MOVE] MoveAxisAsync finished. axis=" + (axis != null ? axis.Name : "-") +
                    ", target=" + position);
            }
        }

        // ------------------------------------------------------------------
        // Wafer Alignment: 3점 비전 매칭 및 CoordinateMap 갱신
        // ------------------------------------------------------------------

        /// <summary>
        /// TopLeft / TopRight / BottomLeft 3개 기준점에서 비전 매칭을 수행하고
        /// CoordinateMap을 갱신합니다(DieTapeFrameAlignmentJob 단순판).
        /// </summary>
        /// <param name="motorPts">각 기준점의 모터 좌표 [(mx,my) x3].</param>
        /// <param name="finder">매칭에 사용할 Finder 이름(기본 ReticleFinder).</param>
        public async Task<bool> AlignWaferAsync(
            (double mx, double my)[] motorPts,
            string finder = VisionComm.VisionToolIds.Wafer.ReticleFinder)
        {
            if (motorPts == null || motorPts.Length < 3)
            { Log("[ALIGN] need 3 motor points"); return false; }
            // 비전 미사용(UseVision=false) — 정렬은 비전 작업이므로 수행하지 않고 건너뛴다(기존 좌표맵 유지).
            if (AppSettingsStore.Current != null && !AppSettingsStore.Current.UseVision)
            { Log("[ALIGN] vision disabled (UseVision=false) - skip wafer align"); return true; }
            if (VisionComm.VisionHub.Wafer == null || !VisionComm.VisionHub.Wafer.IsConnected)
            { Log("[ALIGN] Wafer Vision not connected"); return false; }

            double[] px = new double[3], py = new double[3];
            double[] mx = new double[3], my = new double[3];
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    Log($"[ALIGN] point {i + 1}/3 move motor -> ({motorPts[i].mx:F2}, {motorPts[i].my:F2})");
                    // 실제 모션은 운영 환경에서 추가합니다. 현재는 매칭 호출만 수행합니다.
                    var m = await VisionComm.AutoVisionRequestService.MatchAsync(
                        VisionComm.AutoVisionChannel.Wafer,
                        finder,
                        i,
                        1500,
                        CancellationToken.None).ConfigureAwait(false);
                    if (!m.Success)
                    {
                        Log($"[ALIGN] point {i + 1} match failed: {m.RawError}");
                        return false;
                    }
                    px[i] = m.X; py[i] = m.Y;
                    mx[i] = motorPts[i].mx; my[i] = motorPts[i].my;
                }
                var coord = VisionComm.AlignmentSolver.Solve3Point(px, py, mx, my, out string err);
                if (coord == null)
                {
                    Log("[ALIGN] solver failed: " + err);
                    return false;
                }
                VisionComm.CoordinateMapStore.Save(coord);
                var (sx, sy, rot) = VisionComm.AlignmentSolver.ExtractRotationScale(coord);
                Log($"[ALIGN] OK scaleX={sx:F4} scaleY={sy:F4} rot={rot:F3}deg ({coord})");
                return true;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "ALIGN-EX", "MachineController", ex.Message);
                Log("[ALIGN ERROR] " + ex.Message);
                return false;
            }
        }

        // ------------------------------------------------------------------
        // Current equipment command API
        // 작업 화면의 초기화/READY/시작/정지/수동 시퀀스 버튼에서 사용하는 현재 제어 진입점입니다.
        // ------------------------------------------------------------------

        private async Task<int> RunReadySequenceBeforeStartAsync()
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync",
                    "START 전 Ready 시퀀스를 자동 실행합니다. - Start");
                Log("[START] Ready sequence before auto start.");
                LogMachineAxisSnapshot("StartBeforeReady");

                int result = await RunReadySequenceAsync().ConfigureAwait(false);
                if (result != 0)
                {
                    string reason = string.IsNullOrWhiteSpace(LastActionFailureMessage)
                        ? "START 전 Ready 시퀀스가 실패했습니다."
                        : LastActionFailureMessage;
                    LastActionFailureMessage = reason;
                    QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync",
                        "START 전 Ready 시퀀스 실패. result=" + result + ", reason=" + reason + " - Failed");
                    return result;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync",
                    "START 전 Ready 시퀀스가 완료되었습니다. - Ok");
                Log("[START] Ready sequence before auto start complete.");
                LogMachineAxisSnapshot("StartAfterReadyBeforeAuto");
                return 0;
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = "START 전 Ready 시퀀스가 정지되었습니다.";
                QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync",
                    LastActionFailureMessage + " - Stopped");
                return -1;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "START 전 Ready 시퀀스 실행 중 예외 발생: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync",
                    LastActionFailureMessage + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "START-READY-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>장비 READY: 초기화된 장비의 주요 모션을 안전한 Avoid 위치로 복귀합니다.</summary>
        public async Task<int> RunReadySequenceAsync()
        {
            IDisposable actionScope = null;

            try
            {
                LastActionFailureMessage = string.Empty;

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 READY 시퀀스를 수행할 수 없습니다. 알람을 해제한 뒤 다시 실행하세요.";
                    SetReadySequenceProgress(MachineReadySequenceState.Failed, 0, 0, 0, "Ready", LastActionFailureMessage);
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunReadySequenceAsync",
                        "Ready sequence failed: alarm status is active. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "READY-ALARM", "MachineController", LastActionFailureMessage);
                    Log("[READY] failed: alarm status is active");
                    return -1;
                }

                if (IsReadySequenceRunning)
                {
                    LastActionFailureMessage = "READY 시퀀스가 이미 진행 중입니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunReadySequenceAsync",
                        "Ready sequence failed: ready sequence is already running. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "READY-RUNNING", "MachineController", LastActionFailureMessage);
                    Log("[READY] failed: ready sequence is already running");
                    return -1;
                }

                if (IsSequenceRunning)
                {
                    LastActionFailureMessage = "Sequence 실행 중에는 READY 시퀀스를 수행할 수 없습니다.";
                    SetReadySequenceProgress(MachineReadySequenceState.Failed, 0, 0, 0, "Ready", LastActionFailureMessage);
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunReadySequenceAsync",
                        "Ready sequence failed: sequence is already running. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "READY-RUNNING", "MachineController", LastActionFailureMessage);
                    Log("[READY] failed: sequence is already running");
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("RunReadySequenceAsync"))
                {
                    SetReadySequenceProgress(MachineReadySequenceState.Failed, 0, 0, 0, "Ready", LastActionFailureMessage);
                    return -1;
                }

                // Ready는 안전 위치 복귀 시퀀스이므로 공정 인터락 스코프를 열지 않고 Ready 전용 저속 스코프만 적용한다.
                actionScope = BeginManualActionScope(ManualMotionScopeKind.ReadySequence, "ReadySequence");

                var sequence = new QMC.CDT320.Sequencing.MachineReadySequence(_machine, SetReadySequenceProgress);
                int totalSteps = sequence.TotalStepCount;
                SetReadySequenceProgress(MachineReadySequenceState.Running, 0, 0, totalSteps, "Ready", "Ready 시퀀스를 시작합니다.");

                Log("[READY] Ready sequence start.");
                QMC.Common.Log.Write("Main", "SYSTEM", "RunReadySequenceAsync",
                    "Ready sequence start. - Start");
                LogMachineAxisSnapshot("ReadyStart");

                int result = await sequence.RunAsync(ManualOperationToken).ConfigureAwait(false);
                if (result != 0)
                {
                    LastActionFailureMessage = string.IsNullOrEmpty(sequence.LastErrorMessage)
                        ? "READY 시퀀스 실패."
                        : sequence.LastErrorMessage;
                    SetReadySequenceProgress(MachineReadySequenceState.Failed, ReadySequenceProgress.Percent,
                        ReadySequenceProgress.CompletedSteps, ReadySequenceProgress.TotalSteps, ReadySequenceProgress.CurrentStepName, LastActionFailureMessage);
                    SetStatus(EquipmentStatus.Alarm);
                    return result;
                }

                if (!TrySynchronizeInputCassetteSlotProjection(
                    "ReadySequenceComplete",
                    "READY-INPUT-CST-SLOT-SYNC"))
                {
                    SetReadySequenceProgress(
                        MachineReadySequenceState.Failed,
                        100,
                        totalSteps,
                        totalSteps,
                        "InputCassetteSlotSync",
                        LastActionFailureMessage);
                    SetStatus(EquipmentStatus.Alarm);
                    return -1;
                }

                // READY가 최종 성공하기 전에는 로딩/언로딩 재개 정보를 폐기하지 않는다.
                // Slot projection 검증이 실패하면 장비는 Alarm으로 끝나므로, 이 경우 기존
                // 복구 문맥을 보존해야 다음 READY/수동 복구에서 현재 상태를 다시 판단할 수 있다.
                ClearLoaderTransportResumeStatesAfterReady();
                SaveMachineRuntimeState("ReadySequenceComplete");
                SetStatus(EquipmentStatus.Ready);
                SetReadySequenceProgress(MachineReadySequenceState.Completed, 100, totalSteps, totalSteps, "Ready", "Ready 시퀀스가 완료되었습니다.");
                Log("[READY] Ready sequence complete.");
                QMC.Common.Log.Write("Main", "SYSTEM", "RunReadySequenceAsync",
                    "Ready sequence complete. - Ok");
                LogMachineAxisSnapshot("ReadyComplete");
                return 0;
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = "READY 시퀀스가 정지되었습니다.";
                SetReadySequenceProgress(MachineReadySequenceState.Canceled, ReadySequenceProgress.Percent,
                    ReadySequenceProgress.CompletedSteps, ReadySequenceProgress.TotalSteps, ReadySequenceProgress.CurrentStepName, LastActionFailureMessage);
                QMC.Common.Log.Write("Main", "SYSTEM", "RunReadySequenceAsync",
                    "Ready sequence canceled. - Stopped");
                Log("[READY] canceled");
                if (_status != EquipmentStatus.Alarm && !AlarmManager.HasActive)
                    SetStatus(EquipmentStatus.Stopped);
                return -1;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "READY 시퀀스 실패: " + ex.Message;
                SetReadySequenceProgress(MachineReadySequenceState.Failed, ReadySequenceProgress.Percent,
                    ReadySequenceProgress.CompletedSteps, ReadySequenceProgress.TotalSteps, ReadySequenceProgress.CurrentStepName, LastActionFailureMessage);
                QMC.Common.Log.Write("Main", "SYSTEM", "RunReadySequenceAsync",
                    "Ready sequence failed: " + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "READY-EX", "MachineController", LastActionFailureMessage);
                Log("[READY] failed: " + ex.Message);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
                if (actionScope != null)
                    actionScope.Dispose();
            }
        }

        /// <summary>
        /// READY 완료 후 로딩/언로딩 물류의 저장된 중간 Step만 폐기합니다.
        /// 다음 실행 방향은 기존 Material 기반 Dispatcher가 다시 결정하며,
        /// Align/DieMapping/검사/Picker/Vision 재개 상태는 유지합니다.
        /// </summary>
        private static void ClearLoaderTransportResumeStatesAfterReady()
        {
            string[] stateNames =
            {
                "InputFeederSequence.LoadFromCassette",
                "InputFeederSequence.LoadToStage",
                "InputFeederSequence.UnloadFromStage",
                "InputFeederSequence.UnloadToCassette",
                "InputFeederSequence.Exchange",
                "InputFeederSequence.Recover",
                "InputStageSequence.PrepareLoad",
                "InputStageSequence.PrepareUnload",
                "InputStageSequence.MoveAvoid",
                "InputCassetteSequence.Loading",
                "InputCassetteSequence.Unloading",
                "OutputCassetteSequence.Loading",
                "OutputCassetteSequence.Unloading",
                "OutputCassetteSequence.MoveSlot"
            };

            foreach (string stateName in stateNames)
                QMC.CDT320.Sequencing.SequenceResumeStore.Clear(stateName);

            string[] outputFeederKinds =
            {
                "LoadFromCassette",
                "LoadToStage",
                "UnloadFromStage",
                "UnloadToCassette",
                "Exchange",
                "Recover"
            };
            string[] outputStageKinds =
            {
                "PrepareLoad",
                "PrepareUnload",
                "MoveAvoid"
            };

            foreach (BinSide side in new[] { BinSide.Good, BinSide.Ng })
            {
                foreach (string kind in outputFeederKinds)
                {
                    QMC.CDT320.Sequencing.SequenceResumeStore.Clear(
                        "OutputFeederSequence." + kind + "." + side);
                }

                foreach (string kind in outputStageKinds)
                {
                    QMC.CDT320.Sequencing.SequenceResumeStore.Clear(
                        "OutputStageSequence." + kind + "." + side);
                }
            }

            QMC.Common.Log.Write(
                "Main",
                "SYSTEM",
                "ClearLoaderTransportResumeStatesAfterReady",
                "READY 완료 후 Input/Output 로딩·언로딩 중간 Step을 초기화했습니다. 다음 작업은 현재 Material 상태에서 최초 안전검사부터 시작합니다. - Ok");
        }

        private bool TrySynchronizeInputCassetteSlotProjection(
            string reason,
            string alarmCode)
        {
            InputCassetteUnit cassette =
                _machine != null ? _machine.InputCassetteUnit : null;
            if (cassette == null)
            {
                LastActionFailureMessage =
                    "Input Cassette slot projection 동기화에 필요한 Unit이 없습니다. reason=" +
                    reason;
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    alarmCode,
                    "MachineController",
                    LastActionFailureMessage);
                return false;
            }

            string summary;
            if (!cassette.TrySynchronizeSlotProjectionFromMaterialState(out summary))
            {
                LastActionFailureMessage =
                    "Input Cassette slot projection 동기화에 실패했습니다. reason=" +
                    reason + ", detail=" + summary;
                QMC.Common.Log.Write(
                    "Main",
                    "SYSTEM",
                    "SynchronizeInputCassetteSlotProjection",
                    LastActionFailureMessage + " - Failed");
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    alarmCode,
                    cassette.Name,
                    LastActionFailureMessage);
                return false;
            }

            QMC.Common.Log.Write(
                "Main",
                "SYSTEM",
                "SynchronizeInputCassetteSlotProjection",
                "Input Cassette slot projection 동기화 완료. reason=" +
                reason + ", " + summary + " - Ok");
            return true;
        }

        /// <summary>장비 START: Servo ON 후 현재 구성된 자동 시퀀스를 시작합니다.</summary>
        public bool IsRuntimeAutoFocusOnStartEnabled()
        {
            try
            {
                if (Machine == null || Machine.VisionUnit == null || Machine.VisionUnit.Config == null)
                    return false;

                Machine.VisionUnit.Config.EnsureCalibrationObjects();
                VisionFocusCalibrationData data = Machine.VisionUnit.Config.FocusCalibration;
                return data != null && data.BottomDieScan != null && data.BottomDieScan.AutoFocusOnStartEnabled;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> StartAsync(RuntimeAutoFocusScanMode? startupAutoFocusMode = null)
        {
            CancellationTokenSource startAttemptCts = null;
            long recipeGeneration = 0;
            try
            {
                LastActionFailureMessage = "";

                string startAdmissionReason;
                if (!TryBeginRecipeStartAttempt(
                        "StartAsync",
                        out startAttemptCts,
                        out recipeGeneration,
                        out startAdmissionReason))
                {
                    LastActionFailureMessage = startAdmissionReason;
                    QMC.Common.Log.Write(
                        "Main",
                        "SYSTEM",
                        "StartAsync",
                        "START 진입 차단. " + startAdmissionReason + " - Blocked");
                    return -1;
                }

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 START를 수행할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync", "Start failed: alarm status is active. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "START-ALARM", "MachineController", "Alarm 상태에서는 START를 수행할 수 없습니다.");
                    Log("[START] failed: alarm status is active");
                    return -1;
                }

                if (IsSequenceRunning)
                {
                    LastActionFailureMessage = "Sequence가 이미 실행 중입니다.";
                    Task runningTask = _coordinatorTask;
                    string runningDetail = "taskStatus=" +
                        (runningTask != null ? runningTask.Status.ToString() : "null") +
                        ", coordinator=" + (_coordinator != null) +
                        ", cycleStopRequested=" + (_seqContext != null && _seqContext.IsCycleStopRequested) +
                        ", equipmentStatus=" + _status;
                    QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync",
                        "Start failed: sequence is already running. " + runningDetail + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "START-RUNNING", "MachineController", "Sequence가 이미 실행 중입니다.");
                    Log("[START] failed: sequence is already running. " + runningDetail);
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("StartAsync"))
                    return -1;

                // Recipe/Vision 오류는 READY 축을 움직이기 전에 차단한다.
                if (!await EnsureRecipeReadyForAutoStartAsync(
                        "StartAsync",
                        true,
                        startAttemptCts.Token).ConfigureAwait(false))
                {
                    return -1;
                }

                string startAttemptReason;
                if (!IsRecipeStartAttemptValid(
                        startAttemptCts,
                        recipeGeneration,
                        "StartAsync",
                        out startAttemptReason))
                {
                    LastActionFailureMessage = startAttemptReason;
                    return -1;
                }

                // Stage/Feeder에 남은 Input wafer의 원본 Cassette 정보를 식별할 수 없으면
                // Ready 모션 전에 Alarm으로 차단한다. START에서 Material을 임의 생성/복원하지 않는다.
                if (!EnsureActiveInputWaferSourceForAutoStart("StartAsync"))
                    return -1;

                // Ready 시퀀스(모션)보다 먼저 확인한다 — LOT이 없으면 축을 움직이지 않고 바로 막는다.
                if (!EnsureActiveLotForAutoStart("StartAsync"))
                    return -1;

                int readyResult = await RunReadySequenceBeforeStartAsync().ConfigureAwait(false);
                if (readyResult != 0)
                    return readyResult;

                if (!IsRecipeStartAttemptValid(
                        startAttemptCts,
                        recipeGeneration,
                        "StartAsync.AfterReady",
                        out startAttemptReason))
                {
                    LastActionFailureMessage = startAttemptReason;
                    return -1;
                }

                if (!EnsureReticleAvoidForAutoStart("StartAsync"))
                    return -1;

                ConfigureRuntimeAutoFocusForStart(startupAutoFocusMode);

                //if (!EnsureCalibrationReadyForAutoStart("StartAsync"))
                //    return -1;

                Log("[START] Process auto sequence start.");
                QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync", "Process auto sequence start requested. - Ok");

                int sequenceStartResult = await StartSequenceCoreAsync(
                    QMC.CDT320.Sequencing.SequenceRunOptions.ProcessAuto(),
                    startAttemptCts,
                    recipeGeneration).ConfigureAwait(false);
                if (sequenceStartResult != 0)
                    return sequenceStartResult;

                // 콜렛 클리닝 "Auto 시작" 트리거 리셋은 StartSequenceCoreAsync 안에서 수행한다
                // (StartSequenceAsync 등 다른 Auto 진입점도 함께 커버하고, 코디네이터 기동 전에 처리하기 위해).

                return 0;
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = "START 준비가 STOP/Alarm 요청으로 취소되었습니다.";
                QMC.Common.Log.Write(
                    "Main",
                    "SYSTEM",
                    "StartAsync",
                    LastActionFailureMessage + " - Canceled");
                return -1;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "Start failed: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync", "Start failed: " + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "START-EX", "MachineController", "Start failed: " + ex.Message);
                Log("[START] failed: " + ex.Message);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
                EndRecipeStartAttempt(startAttemptCts, "StartAsync");
            }
        }

        private void ConfigureRuntimeAutoFocusForStart(RuntimeAutoFocusScanMode? requestedMode)
        {
            try
            {
                if (Machine == null || Machine.VisionUnit == null || Machine.VisionUnit.Config == null)
                    return;

                Machine.VisionUnit.Config.EnsureCalibrationObjects();
                VisionFocusCalibrationData data = Machine.VisionUnit.Config.FocusCalibration;
                if (data == null || data.BottomDieScan == null)
                    return;

                RuntimeAutoFocusScanMode mode = RuntimeAutoFocusScanMode.None;
                if (data.BottomDieScan.AutoFocusOnStartEnabled)
                {
                    mode = requestedMode.HasValue
                        ? requestedMode.Value
                        : RuntimeAutoFocusScanMode.RoughAndFine;
                }

                data.SetStartupAutoFocusMode(mode);
                QMC.Common.Log.Write("Main", "SYSTEM", "RuntimeAutoFocusStart",
                    "생산 시작 AutoFocus 선택을 반영했습니다. enabled=" +
                    data.BottomDieScan.AutoFocusOnStartEnabled + ", mode=" + mode + " - Check");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "RuntimeAutoFocusStart",
                    "생산 시작 AutoFocus 선택 반영 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
        }

        /// <summary>
        /// 일반 정지 요청입니다.
        /// 자동 시퀀스 중에는 축을 즉시 정지하지 않고 현재 동작 경계에서 정지하도록 요청합니다.
        /// 축 즉시 정지는 EMO/알람 응답 또는 수동 조작 정지에서만 수행합니다.
        /// </summary>
        public async Task StopAsync()
        {
            if (IsInputStageRunReviewManualActive)
            {
                CancelInputStageRunReviewAction();
                StopInputStageRunReviewAxes();
                InputStageUnit reviewStage = _machine != null ? _machine.InputStageUnit : null;
                if (reviewStage != null)
                {
                    reviewStage.ConfirmFromUi(new UserConfirmResult
                    {
                        IsConfirmed = false,
                        Decision = InputStageRunReviewDecision.Stop,
                        WaferId = CurrentInputStageRunReviewWaferId
                    });
                }
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStageRunReviewManual",
                    "STOP 요청으로 Review 수동 동작을 취소하고 InputCameraX/StageY/StageT를 우선 정지했습니다. - Stop");
            }

            OnStopRequested();

            if (_coordinator != null && _coordinatorTask != null && !_coordinatorTask.IsCompleted)
            {
                await RequestCycleStopSequenceAsync().ConfigureAwait(false);
                Log("[STOP] 일반 정지 요청: 현재 동작 완료 후 시퀀스 경계에서 정지합니다.");
                return;
            }

            CancelManualOperation();

            // 수동/조그 조작 중에는 운전자가 누른 정지 명령이므로 현재 움직이는 축을 정지한다.
            foreach (var ax in EnumerateAxes()) ax.Stop();

            Log("[STOP] Stopped.");
            SetStatus(EquipmentStatus.Stopped);
        }

        // ------------------------------------------------------------------
        // Tree traversal helpers
        // ------------------------------------------------------------------

        private IEnumerable<BaseAxis> EnumerateAxes()
        {
            foreach (var u in _machine.Units)
                foreach (var ax in EnumerateAxesRec(u))
                    yield return ax;
        }

        /// <summary>
        /// 알람 발생 시 AlarmContext 파일에 포함되는 장비 스냅샷 텍스트를 만든다.<br/>
        /// 장비 상태/모드/Recipe와 전체 축의 Command/Actual/Servo/Alarm/Moving/InPosition을 담는다.
        /// (축 Target은 전역 추적 값이 없어 CommandPosition으로 대체 — v1 제한 사항)
        /// 알람 처리 경로에서 호출되므로 어떤 예외도 밖으로 내지 않는다.
        /// </summary>
        private string BuildAlarmContextEquipmentSnapshot()
        {
            try
            {
                var sb = new System.Text.StringBuilder(8 * 1024);
                sb.Append("Status=").Append(_status)
                  .Append(", SequenceRunning=").Append(IsSequenceRunning)
                  .Append(", ManualBusy=").Append(IsManualBusy)
                  .Append(", Recipe=").Append(ActiveRecipeName ?? "-")
                  .AppendLine();

                try
                {
                    if (_seqContext != null && _seqContext.PickerPhases != null)
                        sb.Append("PickerPhases=").Append(_seqContext.PickerPhases.GetSnapshot()).AppendLine();
                }
                catch { }

                foreach (BaseAxis axis in EnumerateAxes())
                {
                    if (axis == null)
                        continue;
                    sb.Append(axis.Name)
                      .Append(" cmd=").Append(axis.CommandPosition.ToString("F4"))
                      .Append(" act=").Append(axis.ActualPosition.ToString("F4"))
                      .Append(" servo=").Append(axis.IsServoOn ? "ON" : "OFF")
                      .Append(" alarm=").Append(axis.IsAlarm ? "ON" : "OFF")
                      .Append(" moving=").Append(axis.IsMoving ? "Y" : "N")
                      .Append(" inpos=").Append(axis.IsInPosition ? "Y" : "N")
                      .AppendLine();
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "equipment snapshot failed: " + ex.Message;
            }
        }

        private BaseAxis FindAxisByName(string axisName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(axisName))
                    return null;

                foreach (var axis in EnumerateAxes())
                {
                    if (string.Equals(axis.Name, axisName.Trim(), StringComparison.OrdinalIgnoreCase))
                        return axis;
                }

                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private static IEnumerable<BaseAxis> EnumerateAxesRec(BaseEquipmentNode node)
        {
            if (node is BaseAxis ax) { yield return ax; yield break; }
            var prop = node.GetType().GetProperty("Components");
            if (prop != null && prop.GetValue(node) is System.Collections.IEnumerable comps)
                foreach (BaseEquipmentNode c in comps)
                    foreach (var a in EnumerateAxesRec(c))
                        yield return a;
        }

        // ------------------------------------------------------------------
        // Status / log helpers
        // ------------------------------------------------------------------

        private void SetStatus(EquipmentStatus s)
        {
            // Alarm record가 살아 있는 동안 늦게 끝난 READY/초기화 Task가 상태를 덮지 못하게 합니다.
            if (s != EquipmentStatus.Alarm && AlarmManager.HasActive)
                s = EquipmentStatus.Alarm;

            if (_status == s) return;
            EquipmentStatus old = _status;
            _status = s;
            try { Stats.OnStateChanged(old, s, DateTime.UtcNow); } catch { }
            var h = StatusChanged;
            if (h != null) try { h(s); } catch { }
        }

        private void SetReadySequenceProgress(MachineReadyProgress progress)
        {
            if (progress == null)
                return;

            _readySequenceProgress = progress;
            var h = ReadySequenceProgressChanged;
            if (h != null) try { h(progress); } catch { }
        }

        private void SetReadySequenceProgress(
            MachineReadySequenceState state,
            int percent,
            int completedSteps,
            int totalSteps,
            string currentStepName,
            string message)
        {
            SetReadySequenceProgress(new MachineReadyProgress(
                state,
                percent,
                completedSteps,
                totalSteps,
                currentStepName,
                message));
        }

        private void Log(string msg)
        {
            var h = LogMessage;
            if (h != null) try { h(msg); } catch { }
        }

        /// <summary>외부 시퀀스 계층에서 장비 로그를 기록하기 위한 공개 로그 브리지입니다.</summary>
        public void LogPublic(string msg)
        {
            Log(msg);
        }

        public void RequestOperatorMessage(string title, string message)
        {
            try
            {
                title = string.IsNullOrWhiteSpace(title) ? "작업자 확인" : title;
                message = string.IsNullOrWhiteSpace(message) ? "작업자 확인이 필요합니다." : message;

                string key = title + "|" + message;
                lock (_operatorMessageLock)
                {
                    DateTime now = DateTime.UtcNow;
                    if (string.Equals(_lastOperatorMessageKey, key, StringComparison.Ordinal) &&
                        (now - _lastOperatorMessageTimeUtc).TotalSeconds < 5.0)
                        return;

                    _lastOperatorMessageKey = key;
                    _lastOperatorMessageTimeUtc = now;
                }

                var handler = OperatorMessageRequested;
                if (handler != null)
                    handler(title, message);

                Log("[OPERATOR-MESSAGE] " + title + " - " + message);
            }
            catch (Exception ex)
            {
                Log("[OPERATOR-MESSAGE] request failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static double ResolveAxisInPositionTolerance(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;
        }

    }
}

