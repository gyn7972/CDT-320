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
    public class MachineController
    {
        private readonly CDT320_Machine _machine;
        private EquipmentStatus _status = EquipmentStatus.Idle;
        private CancellationTokenSource _cycleCts;
        private bool _cycleStopRequested;
        private bool _cycleResumePending;
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
        private bool _isMachineInitialized;
        private bool _isDeveloperReadyRestored;
        private MachineReadyProgress _readySequenceProgress =
            new MachineReadyProgress(MachineReadySequenceState.Idle, 0, 0, 0, "", "");
        private readonly AxisInterferenceMap _axisInterferenceMap = AxisInterferenceMap.CreateDefault();
        private readonly AxisInitializeInterlockService _axisInitializeInterlocks;
        private readonly object _initializeHomedAxisLock = new object();
        private HashSet<string> _initializeHomedAxisNames;
        private readonly object _axisInitializeStepStateLock = new object();
        private readonly Dictionary<string, AxisInitializeStepProgress> _axisInitializeStepStates =
            new Dictionary<string, AxisInitializeStepProgress>(StringComparer.OrdinalIgnoreCase);
        private bool _restoringAxisInitializeStepState;
        private readonly object _operatorMessageLock = new object();
        private string _lastOperatorMessageKey = string.Empty;
        private DateTime _lastOperatorMessageTimeUtc = DateTime.MinValue;
        public SharedRailXMotionService SharedRailX { get; private set; }

        public event Action<EquipmentStatus> StatusChanged;
        public event Action<string> LogMessage;
        public event Action<int, int> CycleProgress;  // (done, total)
        public event Action StopRequested;
        public event Action<bool> MachineInitializedChanged;
        public event Action<AxisInitializeStepProgress> AxisInitializeStepProgressChanged;
        public event Action<MachineReadyProgress> ReadySequenceProgressChanged;
        public event Action<string, string> OperatorMessageRequested;

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
        /// <summary>4개 유닛(INPUT/FRONT/REAR/OUTPUT) 시퀀스 동작 상태(공식 상태 객체). UI 표시용.</summary>
        public QMC.CDT320.Sequencing.SequenceActivityMonitor SequenceActivity => _sequenceActivity;
        public DateTime MachineInitializedAt { get; private set; }
        public string LastActionFailureMessage { get; private set; }
        public bool CanRunEquipment => IsMachineInitialized && _status != EquipmentStatus.Alarm && !IsSequenceRunning;
        public QMC.CDT320.Sequencing.SequenceRunMode? ActiveSequenceRunMode { get; private set; }
        public int CycleTotal { get; private set; }
        public int CycleDone { get; private set; }
        public int GoodCount { get; private set; }
        public int NgCount { get; private set; }

        // Wafer die map settings.
        // Input uses a circular 300mm wafer map, Output uses a rectangular tray map.
        // The maps are created once by EnsureDieMaps and then cached.
        /// <summary>웨이퍼 직경 [mm].</summary>
        public double WaferDiameterMm { get; set; } = 300.0;
        /// <summary>다이 X 크기 [mm].</summary>
        public double DieSizeXMm { get; set; } = 8.12;
        /// <summary>다이 Y 크기 [mm].</summary>
        public double DieSizeYMm { get; set; } = 6.12;
        /// <summary>Input die map gap [mm].</summary>
        public double InputGapMm { get; set; } = 0.05;
        /// <summary>Output die map gap [mm].</summary>
        public double OutputGapMm { get; set; } = 0.30;

        private QMC.CDT320.DieMaps.DieMap _inputDieMap;
        private QMC.CDT320.DieMaps.DieMap _outputDieMap;
        /// <summary>Input die map. null이면 EnsureDieMaps 호출이 필요합니다.</summary>
        public QMC.CDT320.DieMaps.DieMap InputDieMap => _inputDieMap;
        /// <summary>Output die map. null이면 EnsureDieMaps 호출이 필요합니다.</summary>
        public QMC.CDT320.DieMaps.DieMap OutputDieMap => _outputDieMap;
        /// <summary>true이면 사이클에서 InputDieMap의 IsTarget=true 다이 좌표를 사용합니다.</summary>
        public bool UseDieMapMode { get; set; } = true;

        // Stage 61: Pickup sequence options + cached sequence.
        /// <summary>Input/Wafer die pickup sequence options.</summary>
        public QMC.CDT320.Recipes.PickupSubset PickupOptions { get; set; } =
            new QMC.CDT320.Recipes.PickupSubset();

        /// <summary>
        /// PickupOptions 기준으로 정렬된 입력 다이 픽업 순서입니다.
        /// EnsureDieMaps 또는 RebuildPickupSequence 호출 시 갱신됩니다.
        /// </summary>
        private List<QMC.CDT320.DieMaps.DieMapEntry> _inputPickupSequence
            = new List<QMC.CDT320.DieMaps.DieMapEntry>();

        // Pipelined wafer capture state.
        // Capture can overlap with the next inspect/place cycle.
        private Task<(double X, double Y)[]> _pendingCaptureTask;
        private int _pendingCaptureCycleIdx = -1;
        private int _pendingCapturePickers = 0;

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

        /// <summary>Input/Output 다이맵을 생성합니다. 이미 생성되어 있으면 기존 맵을 재사용합니다.</summary>
        public void EnsureDieMaps()
        {
            if (_inputDieMap == null)
            {
                _inputDieMap = QMC.CDT320.DieMaps.DieMapGenerator.GenerateWafer(
                    WaferDiameterMm, DieSizeXMm, DieSizeYMm, InputGapMm, InputGapMm,
                    frameObjId: "INPUT");
                int active = 0;
                foreach (var e in _inputDieMap.Entries) if (e.IsTarget) active++;
                Log("[DIEMAP] Input wafer " + WaferDiameterMm + "mm, die " +
                    DieSizeXMm + "x" + DieSizeYMm + ", gap " + InputGapMm +
                    ", grid " + _inputDieMap.DieMapX + "x" + _inputDieMap.DieMapY +
                    ", active=" + active);
                // UI 표시를 위해 LotStorage에도 등록합니다.
                QMC.CDT320.Lots.LotStorage.ActiveInputDieMap = _inputDieMap;
            }
            if (_outputDieMap == null)
            {
                // Output 사각 트레이는 Input 활성 다이 수 이상을 수용하도록 생성합니다.
                int active = 0;
                foreach (var e in _inputDieMap.Entries) if (e.IsTarget) active++;
                // 정사각 격자 root(N)을 올림 처리합니다.
                int side = (int)System.Math.Ceiling(System.Math.Sqrt(active));
                _outputDieMap = QMC.CDT320.DieMaps.DieMapGenerator.GenerateRect(
                    side, side, DieSizeXMm, DieSizeYMm, OutputGapMm, OutputGapMm,
                    frameObjId: "OUTPUT");
                Log("[DIEMAP] Output tray die " + DieSizeXMm + "x" + DieSizeYMm +
                    ", gap " + OutputGapMm + ", grid " + side + "x" + side +
                    ", slots=" + (side * side));
            }

            // WafersPerOutputBatch <= 0이면 OutputDieMap 슬롯 수에 자동으로 맞춥니다.
            if (WafersPerOutputBatch <= 0 && _outputDieMap != null)
            {
                WafersPerOutputBatch = _outputDieMap.Entries.Count;
                Log("[DIEMAP] WafersPerOutputBatch auto set = " + WafersPerOutputBatch + " (Output tray slot count)");
            }

            // Stage 61: 픽업 순서를 재생성합니다(PickupOptions 적용).
            RebuildPickupSequence();
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

        /// <summary>
        /// InitAsync에서 카세트 자동 매핑을 수행할지 여부입니다.
        /// 기본값 false는 INIT 때 모터 초기화만 수행합니다.
        /// 카세트 스캔은 작업정보/Output 페이지의 매핑 버튼으로 별도 수행합니다.
        /// </summary>
        public bool AutoScanCassetteOnInit { get; set; } = false;

        public MachineController(CDT320_Machine machine)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            SharedRailX = new SharedRailXMotionService(_machine, CreateSharedRailXConfig());
            SharedRailXMotionRuntime.ServiceProvider = () => SharedRailX;
            _axisInitializeInterlocks = new AxisInitializeInterlockService(_machine, EnumerateAxes);
            MotionGuardRuntime.ContextProvider = () =>
                new MotionGuardContext(_machine, EnumerateAxes(), QMC.CDT320.Ajin.CylinderManager.Items.Values);
            BaseAxis.MotionGuard = VerifyAxisMotionGuard;
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

            return MotionGuardRuntime.VerifyAxisMove(axis, targetPosition, out reason);
        }

        private static bool VerifyCylinderMotionGuard(
            QMC.Common.IO.BaseCylinder cylinder,
            bool moveFwd,
            out string reason)
        {
            return MotionGuardRuntime.VerifyCylinderMove(cylinder, moveFwd, out reason);
        }

        public void ApplyStartupMachineRuntimeState(AppSettings settings)
        {
            try
            {
                settings = settings ?? AppSettingsStore.Current ?? AppSettingsStore.Load();
                _isDeveloperReadyRestored = false;
                var state = MachineRuntimeStateStore.Load();
                if (state != null)
                {
                    RestorePickerOffsetRuntimeState(state);
                    RestorePickerWorkCounterRuntimeState(state);
                }

                if (!settings.DeveloperMode)
                {
                    RestoreCylinderRuntimeState(state, settings);
                    RestoreAxisInitializeStepRuntimeState(state);
                    SetMachineInitialized(false, "StartupNormalMode", true);
                    QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                        "Machine initialized state is false on normal startup. - Ok");
                    return;
                }

                if (settings.BypassHardware && state != null)
                    RestoreBypassAxisRuntimeState(state);
                else if (!settings.BypassHardware && state != null)
                    RestoreRealAxisHomeDoneState(state);
                if (state != null)
                    RestoreCylinderRuntimeState(state, settings);
                if (state != null)
                    RestoreAxisInitializeStepRuntimeState(state);

                if (state == null || !state.IsMachineInitialized)
                {
                    RestoreAxisInitializeStepRuntimeState(state);
                    SetMachineInitialized(false, "DeveloperModeNoSavedReady", true);
                    QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                        "Developer mode is on, but saved initialized state does not exist. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-RESTORE-NO-STATE", "MachineController",
                        "Developer Mode: 저장된 장비 초기화 상태가 없어 INIT이 필요합니다.");
                    return;
                }

                string reason;
                if (!ValidateDeveloperRuntimeState(settings, state, out reason))
                {
                    RestoreAxisInitializeStepRuntimeState(state);
                    SetMachineInitialized(false, "DeveloperModeRestoreFailed", true);
                    QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                        "Developer mode initialized state restore failed: " + reason + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-RESTORE-FAIL", "MachineController",
                        "Developer Mode: 장비 초기화 상태 복구 실패. " + reason);
                    return;
                }

                _isDeveloperReadyRestored = true;
                SetMachineInitialized(true, "DeveloperModeRestore", true);
                if (_status == EquipmentStatus.Idle || _status == EquipmentStatus.Stopped)
                    SetStatus(EquipmentStatus.Ready);

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Developer mode initialized state restored. file=" + MachineRuntimeStateStore.StatePath + " - Ok");
            }
            catch (Exception ex)
            {
                RestoreAxisInitializeStepRuntimeState(MachineRuntimeStateStore.Load());
                SetMachineInitialized(false, "StartupRestoreException", true);
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Machine initialized state restore failed: " + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-RESTORE-EX", "MachineController",
                    "장비 초기화 상태 복구 중 오류가 발생했습니다. " + ex.Message);
            }
            finally
            {
            }
        }

        private bool ValidateDeveloperRuntimeState(
            AppSettings settings,
            MachineRuntimeState state,
            out string reason)
        {
            reason = "";
            try
            {
                if (settings == null)
                {
                    reason = "settings is null";
                    return false;
                }

                if (state == null)
                {
                    reason = "saved state is null";
                    return false;
                }

                if (!state.DeveloperMode)
                {
                    reason = "saved state was not created in Developer Mode";
                    return false;
                }

                if (state.Axes == null || state.Axes.Count == 0)
                {
                    reason = "saved axis state is empty";
                    return false;
                }

                foreach (var ax in EnumerateAxes())
                {
                    try { ax.UpdateStatus(); } catch { }

                    MachineAxisRuntimeState saved = null;
                    foreach (var s in state.Axes)
                    {
                        if (s != null && string.Equals(s.Name, ax.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            saved = s;
                            break;
                        }
                    }

                    if (saved == null)
                    {
                        reason = "axis state is missing: " + ax.Name;
                        return false;
                    }

                    if (settings.BypassHardware)
                    {
                        if (!saved.IsServoOn || saved.IsAlarm || !saved.IsHomeDone)
                        {
                            reason = "saved axis is not ready: " + ax.Name;
                            return false;
                        }

                        continue;
                    }

                    if (!ax.IsServoOn)
                    {
                        reason = "axis servo is off: " + ax.Name;
                        return false;
                    }

                    if (ax.IsAlarm)
                    {
                        reason = "axis alarm is active: " + ax.Name;
                        return false;
                    }

                    if (!ax.IsHomeDone)
                    {
                        reason = "axis home is not done: " + ax.Name;
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

        private void SetMachineInitialized(bool initialized, string reason, bool saveState)
        {
            try
            {
                bool changed = _isMachineInitialized != initialized;
                _isMachineInitialized = initialized;
                if (initialized)
                    MachineInitializedAt = DateTime.Now;
                else
                    MachineInitializedAt = DateTime.MinValue;

                Log("[INIT-STATE] MachineInitialized=" + initialized + " reason=" + reason);
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineInitialized",
                    "Machine initialized=" + initialized + ", reason=" + reason + " - Ok");

                if (saveState)
                    SaveMachineRuntimeState(reason);

                if (changed)
                {
                    var h = MachineInitializedChanged;
                    if (h != null) try { h(initialized); } catch { }
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineInitialized",
                    "Machine initialized state update failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public bool SaveMachineRuntimeState(string reason)
        {
            try
            {
                var state = CaptureMachineRuntimeState(reason);
                bool ok = MachineRuntimeStateStore.Save(state);
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                    "Machine runtime state save. reason=" + reason + ", file=" + MachineRuntimeStateStore.StatePath +
                    (ok ? " - Ok" : " - Failed"));
                return ok;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                    "Machine runtime state save failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        public void SaveMachineRuntimeStateForApplicationClosing()
        {
            try
            {
                var settings = AppSettingsStore.Current ?? AppSettingsStore.Load();
                if (settings != null && settings.DeveloperMode)
                {
                    SaveMachineRuntimeState("ApplicationClosingDeveloperMode");
                    return;
                }

                SetMachineInitialized(false, "ApplicationClosingNormalMode", true);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeStateClose",
                    "Machine runtime state close save failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private MachineRuntimeState CaptureMachineRuntimeState(string reason)
        {
            var state = new MachineRuntimeState
            {
                IsMachineInitialized = _isMachineInitialized,
                DeveloperMode = AppSettingsStore.Current != null && AppSettingsStore.Current.DeveloperMode,
                SavedAt = DateTime.Now,
                SaveReason = reason ?? "",
                Status = _status.ToString(),
                MaterialSnapshotPath = QMC.CDT320.Materials.MaterialSnapshotStore.SnapshotPath,
                Axes = new List<MachineAxisRuntimeState>(),
                Cylinders = new List<MachineCylinderRuntimeState>(),
                PickerOffsets = CapturePickerOffsetRuntimeStates(),
                PickerWorkCounters = CapturePickerWorkCounterRuntimeStates(),
                InitializeSteps = CaptureAxisInitializeStepRuntimeStates()
            };

            foreach (var ax in EnumerateAxes())
            {
                try { ax.UpdateStatus(); } catch { }
                bool ignoreAxisAlarmForRuntimeState =
                    AppSettingsStore.Current != null &&
                    AppSettingsStore.Current.SimulationMode &&
                    ax.Config != null &&
                    ax.Config.IsSimulationMode;
                state.Axes.Add(new MachineAxisRuntimeState
                {
                    Name = ax.Name,
                    IsServoOn = ax.IsServoOn,
                    IsAlarm = ignoreAxisAlarmForRuntimeState ? false : ax.IsAlarm,
                    IsHomeDone = ax.IsHomeDone,
                    IsInPosition = ignoreAxisAlarmForRuntimeState ? true : ax.IsInPosition,
                    ActualPosition = ax.ActualPosition,
                    CommandPosition = ax.CommandPosition,
                    AlarmCode = ignoreAxisAlarmForRuntimeState ? 0 : ax.AlarmCode
                });
            }

            foreach (var cylinder in QMC.CDT320.Ajin.CylinderManager.Items.Values)
            {
                if (cylinder == null)
                    continue;

                try { cylinder.InFwd.UpdateStatus(); } catch { }
                try { cylinder.InBwd.UpdateStatus(); } catch { }
                try { cylinder.OutFwd.UpdateStatus(); } catch { }
                try { cylinder.OutBwd.UpdateStatus(); } catch { }

                state.Cylinders.Add(new MachineCylinderRuntimeState
                {
                    Name = cylinder.Name,
                    IsFwd = cylinder.IsFwd,
                    IsBwd = cylinder.IsBwd,
                    InFwdOn = cylinder.InFwd != null && cylinder.InFwd.IsOn,
                    InBwdOn = cylinder.InBwd != null && cylinder.InBwd.IsOn,
                    OutFwdOn = cylinder.OutFwd != null && cylinder.OutFwd.IsOn,
                    OutBwdOn = cylinder.OutBwd != null && cylinder.OutBwd.IsOn,
                    IsSingleSolenoid = cylinder.Setup != null && cylinder.Setup.IsSingleSolenoid,
                    IsSimulationMode = cylinder.Config != null && cylinder.Config.IsSimulationMode
                });
            }

            return state;
        }

        private List<MachinePickerOffsetRuntimeState> CapturePickerOffsetRuntimeStates()
        {
            var items = new List<MachinePickerOffsetRuntimeState>();
            try
            {
                CapturePickerOffsetRuntimeStates(items, "Front", _machine != null ? _machine.PickerFrontUnit : null);
                CapturePickerOffsetRuntimeStates(items, "Rear", _machine != null ? _machine.PickerRearUnit : null);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                    "Picker offset runtime state capture failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return items;
        }

        private static void CapturePickerOffsetRuntimeStates(
            List<MachinePickerOffsetRuntimeState> items,
            string side,
            PickerFrontUnit unit)
        {
            if (items == null || unit == null)
                return;

            unit.EnsureRuntimePickerOffsets();
            for (int i = 0; i < PickerFrontUnit.MaxPickerCount; i++)
                AddPickerOffsetRuntimeState(items, side, i, unit.GetRuntimePickerOffset(i));
        }

        private static void CapturePickerOffsetRuntimeStates(
            List<MachinePickerOffsetRuntimeState> items,
            string side,
            PickerRearUnit unit)
        {
            if (items == null || unit == null)
                return;

            unit.EnsureRuntimePickerOffsets();
            for (int i = 0; i < PickerRearUnit.MaxPickerCount; i++)
                AddPickerOffsetRuntimeState(items, side, i, unit.GetRuntimePickerOffset(i));
        }

        private static void AddPickerOffsetRuntimeState(
            List<MachinePickerOffsetRuntimeState> items,
            string side,
            int pickerIndex,
            PickerAlignOffset offset)
        {
            if (items == null || offset == null)
                return;

            items.Add(new MachinePickerOffsetRuntimeState
            {
                Side = side,
                PickerIndex = pickerIndex,
                AlignOffsetX = offset.AlignOffsetX,
                AlignOffsetY = offset.AlignOffsetY,
                AlignOffsetT = offset.AlignOffsetT
            });
        }

        private void RestorePickerOffsetRuntimeState(MachineRuntimeState state)
        {
            try
            {
                if (state == null || state.PickerOffsets == null || state.PickerOffsets.Count == 0)
                    return;

                int restored = 0;
                foreach (MachinePickerOffsetRuntimeState saved in state.PickerOffsets)
                {
                    if (saved == null)
                        continue;

                    PickerAlignOffset offset = new PickerAlignOffset
                    {
                        AlignOffsetX = saved.AlignOffsetX,
                        AlignOffsetY = saved.AlignOffsetY,
                        AlignOffsetT = saved.AlignOffsetT
                    };

                    if (string.Equals(saved.Side, "Front", StringComparison.OrdinalIgnoreCase) &&
                        _machine != null && _machine.PickerFrontUnit != null)
                    {
                        _machine.PickerFrontUnit.RestoreRuntimePickerOffset(saved.PickerIndex, offset);
                        restored++;
                    }
                    else if (string.Equals(saved.Side, "Rear", StringComparison.OrdinalIgnoreCase) &&
                             _machine != null && _machine.PickerRearUnit != null)
                    {
                        _machine.PickerRearUnit.RestoreRuntimePickerOffset(saved.PickerIndex, offset);
                        restored++;
                    }
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Picker offset runtime state restored. count=" + restored + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Picker offset runtime state restore failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private List<MachinePickerWorkCounterRuntimeState> CapturePickerWorkCounterRuntimeStates()
        {
            var items = new List<MachinePickerWorkCounterRuntimeState>();
            try
            {
                CapturePickerWorkCounterRuntimeState(items, "Front", _machine != null ? _machine.PickerFrontUnit : null);
                CapturePickerWorkCounterRuntimeState(items, "Rear", _machine != null ? _machine.PickerRearUnit : null);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                    "Picker work counter runtime state capture failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }

            return items;
        }

        private static void CapturePickerWorkCounterRuntimeState(
            List<MachinePickerWorkCounterRuntimeState> items,
            string side,
            PickerFrontUnit unit)
        {
            if (items == null || unit == null)
                return;

            items.Add(new MachinePickerWorkCounterRuntimeState
            {
                Side = side,
                ColletUseCounts = CopyCounterArray(unit.ColletUseCounts, PickerFrontUnit.MaxPickerCount),
                PickFailCount = unit.PickFailCount,
                PlaceFailCount = unit.PlaceFailCount
            });
        }

        private static void CapturePickerWorkCounterRuntimeState(
            List<MachinePickerWorkCounterRuntimeState> items,
            string side,
            PickerRearUnit unit)
        {
            if (items == null || unit == null)
                return;

            items.Add(new MachinePickerWorkCounterRuntimeState
            {
                Side = side,
                ColletUseCounts = CopyCounterArray(unit.ColletUseCounts, PickerRearUnit.MaxPickerCount),
                PickFailCount = unit.PickFailCount,
                PlaceFailCount = unit.PlaceFailCount
            });
        }

        private static int[] CopyCounterArray(int[] source, int count)
        {
            int[] result = new int[count];
            if (source == null)
                return result;

            int copyCount = Math.Min(count, source.Length);
            for (int i = 0; i < copyCount; i++)
                result[i] = Math.Max(0, source[i]);

            return result;
        }

        private void RestorePickerWorkCounterRuntimeState(MachineRuntimeState state)
        {
            try
            {
                if (state == null || state.PickerWorkCounters == null || state.PickerWorkCounters.Count == 0)
                    return;

                int restored = 0;
                foreach (MachinePickerWorkCounterRuntimeState saved in state.PickerWorkCounters)
                {
                    if (saved == null)
                        continue;

                    if (string.Equals(saved.Side, "Front", StringComparison.OrdinalIgnoreCase) &&
                        _machine != null && _machine.PickerFrontUnit != null)
                    {
                        _machine.PickerFrontUnit.RestoreWorkCounters(
                            saved.ColletUseCounts,
                            saved.PickFailCount,
                            saved.PlaceFailCount);
                        restored++;
                    }
                    else if (string.Equals(saved.Side, "Rear", StringComparison.OrdinalIgnoreCase) &&
                             _machine != null && _machine.PickerRearUnit != null)
                    {
                        _machine.PickerRearUnit.RestoreWorkCounters(
                            saved.ColletUseCounts,
                            saved.PickFailCount,
                            saved.PlaceFailCount);
                        restored++;
                    }
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Picker work counter runtime state restored. count=" + restored + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Picker work counter runtime state restore failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void RestoreBypassAxisRuntimeState(MachineRuntimeState state)
        {
            try
            {
                if (state == null || state.Axes == null)
                    return;

                int restored = 0;
                foreach (var ax in EnumerateAxes())
                {
                    MachineAxisRuntimeState saved = null;
                    foreach (var s in state.Axes)
                    {
                        if (s != null && string.Equals(s.Name, ax.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            saved = s;
                            break;
                        }
                    }

                    if (saved == null)
                        continue;

                    ax.RestoreRuntimeState(
                        saved.ActualPosition,
                        saved.CommandPosition,
                        saved.IsServoOn,
                        saved.IsHomeDone,
                        saved.IsInPosition,
                        saved.IsAlarm,
                        saved.AlarmCode);
                    restored++;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Bypass axis runtime state restored. count=" + restored + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Bypass axis runtime state restore failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void RestoreRealAxisHomeDoneState(MachineRuntimeState state)
        {
            try
            {
                if (state == null || state.Axes == null)
                    return;

                int restored = 0;
                foreach (var ax in EnumerateAxes())
                {
                    QMC.CDT320.Ajin.AjinAxis ajin = ax as QMC.CDT320.Ajin.AjinAxis;
                    if (ajin == null)
                        continue;

                    MachineAxisRuntimeState saved = null;
                    foreach (var s in state.Axes)
                    {
                        if (s != null && string.Equals(s.Name, ax.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            saved = s;
                            break;
                        }
                    }

                    if (saved == null)
                        continue;

                    // 실장비: 보드 HomeDone=off 보고를 무시하도록 저장된 초기화 신호를 latch 복원한다.
                    // 위치/서보는 보드 값을 그대로 따른다.
                    ajin.RestoreHomeDoneSignal(saved.IsHomeDone, saved.IsAlarm);
                    if (saved.IsHomeDone && !saved.IsAlarm)
                        restored++;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Real axis HomeDone signal restored. count=" + restored + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Real axis HomeDone signal restore failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void RestoreCylinderRuntimeState(MachineRuntimeState state, AppSettings settings)
        {
            try
            {
                if (QMC.CDT320.Ajin.CylinderManager.Items == null || QMC.CDT320.Ajin.CylinderManager.Items.Count == 0)
                    return;

                bool hasSavedState = state != null && state.Cylinders != null && state.Cylinders.Count > 0;
                int restored = 0;
                int defaulted = 0;
                foreach (var cylinder in QMC.CDT320.Ajin.CylinderManager.Items.Values)
                {
                    if (cylinder == null)
                        continue;

                    if (!CanRestoreSimulationCylinder(cylinder))
                        continue;

                    MachineCylinderRuntimeState saved = null;
                    if (hasSavedState)
                    {
                        foreach (var item in state.Cylinders)
                        {
                            if (item != null && string.Equals(item.Name, cylinder.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                saved = item;
                                break;
                            }
                        }
                    }

                    if (saved != null)
                    {
                        RestoreCylinderIoState(cylinder, saved);
                        restored++;
                    }

                    if (EnsureSimulationCylinderDisplayState(cylinder))
                        defaulted++;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Cylinder runtime state restored. count=" + restored + ", defaulted=" + defaulted + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Cylinder runtime state restore failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static bool CanRestoreSimulationCylinder(QMC.Common.IO.BaseCylinder cylinder)
        {
            if (cylinder == null || cylinder.Config == null || !cylinder.Config.IsSimulationMode)
                return false;

            return IsSimulationInput(cylinder.InFwd)
                && IsSimulationInput(cylinder.InBwd)
                && IsSimulationOutput(cylinder.OutFwd)
                && IsSimulationOutput(cylinder.OutBwd);
        }

        private static bool IsSimulationInput(QMC.Common.IO.BaseDigitalInput input)
        {
            return input != null && input.Config != null && input.Config.IsSimulationMode;
        }

        private static bool IsSimulationOutput(QMC.Common.IO.BaseDigitalOutput output)
        {
            return output != null && output.Config != null && output.Config.IsSimulationMode;
        }

        private static void RestoreCylinderIoState(QMC.Common.IO.BaseCylinder cylinder, MachineCylinderRuntimeState saved)
        {
            if (cylinder == null || saved == null)
                return;

            if (cylinder.OutFwd != null)
                cylinder.OutFwd.Write(saved.OutFwdOn);
            if (cylinder.OutBwd != null)
                cylinder.OutBwd.Write(saved.OutBwdOn);

            if (cylinder.InFwd != null)
                cylinder.InFwd.SimulateInput(saved.InFwdOn);
            if (cylinder.InBwd != null)
                cylinder.InBwd.SimulateInput(saved.InBwdOn);
        }

        private static bool EnsureSimulationCylinderDisplayState(QMC.Common.IO.BaseCylinder cylinder)
        {
            if (cylinder == null || !CanRestoreSimulationCylinder(cylinder))
                return false;

            bool inFwdOn = cylinder.InFwd != null && cylinder.InFwd.IsOn;
            bool inBwdOn = cylinder.InBwd != null && cylinder.InBwd.IsOn;
            if (inFwdOn != inBwdOn)
                return false;

            if (cylinder.OutFwd != null)
                cylinder.OutFwd.Write(false);
            if (cylinder.OutBwd != null)
                cylinder.OutBwd.Write(false);
            if (cylinder.InFwd != null)
                cylinder.InFwd.SimulateInput(false);
            if (cylinder.InBwd != null)
                cylinder.InBwd.SimulateInput(true);

            return true;
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

            // 카세트 존재 확인.
            if (!DryRun && !cassette.CassetteExistSensor.IsOn)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "LOT-NOCASS",
                    cassette.Name, "Input cassette is not detected.");
                Log("[LOTPORT] InputCassette absent. Load skipped.");
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
            double targetZ = cassette.Recipe.FirstSlotPosition + next * slotPitch;
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
        // Stage 28: legacy InputStage cycle helper
        // ------------------------------------------------------------------

        /// <summary>다이 1개 픽업을 위해 StageY/CameraX를 이동합니다.</summary>
        public async Task<int> MoveInputStageToDieAsync(int row, int col)
        {
            try
            {
                var stage = _machine.InputStageUnit;
                // Stage 28: Origin + Pitch가 설정되어 있으면 정확 좌표로 이동하고, 아니면 추정값을 사용합니다.
                double targetX = stage.OriginX + col * (stage.PitchX > 0 ? stage.PitchX : 0.15);
                double targetY = stage.OriginY + row * (stage.PitchY > 0 ? stage.PitchY : 0.15);

                int xResult = await MoveAxisAsync(stage.CameraX, targetX, 100.0).ConfigureAwait(false);
                if (xResult != 0)
                {
                    Log($"[INPUTSTAGE] Move to die [{row},{col}] CameraX failed. result={xResult}");
                    return xResult;
                }

                int yResult = await MoveAxisAsync(stage.StageY, targetY, 100.0).ConfigureAwait(false);
                if (yResult != 0)
                {
                    Log($"[INPUTSTAGE] Move to die [{row},{col}] StageY failed. result={yResult}");
                    return yResult;
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log("[INPUTSTAGE] MoveToDie exception: " + ex.Message);
                AlarmManager.Raise(AlarmSeverity.Error, "INPUT-STAGE-MOVE", "InputStage",
                    "다이 위치 이동 중 예외가 발생했습니다. row=" + row + ", col=" + col + ", error=" + ex.Message);
                return -1;
            }
            finally
            {
                Log($"[INPUTSTAGE] MoveToDie finished. row={row}, col={col}");
            }
        }

        /// <summary>InputStage의 웨이퍼 언로드 시퀀스입니다(사이클 종료 시).</summary>
        public async Task<bool> UnloadInputStageWaferAsync()
        {
            try
            {
                await Task.CompletedTask;
                Log("[INPUTSTAGE] UnloadWaferAsync is not active in InputStageUnit. Skip unload call.");
                return true;
            }
            catch (Exception ex)
            {
                Log("[INPUTSTAGE] UnloadWafer exception: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 현재 InputStage 교환 위치의 피더를 후퇴시켜 빈 카세트 슬롯으로 복귀합니다.<br/>
        /// 사이클 종료 또는 다음 슬롯 진행 전 호출합니다.
        /// </summary>
        public async Task<bool> RetractCurrentWaferAsync()
        {
            var feeder = _machine.InputFeederUnit;
            if (!InputWaferAtExchange)
            {
                Log("[LOTPORT] Retract skipped. Feeder is not at exchange position.");
                return true;
            }
            int ok = await feeder.RetractFeederAsync();
            if (ok != 0)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "LOT-RET",
                    feeder.Name, "Feeder retract failed.");
                return false;
            }
            InputWaferAtExchange = false;
            RaiseLotPortChanged();
            Log("[LOTPORT] RetractCurrentWafer OK");
            return true;
        }

        /// <summary>Completed wafer store request delegated to OutputCassette/OutputFeeder.</summary>
        /// 0 = EnsureDieMaps에서 OutputDieMap 슬롯 수로 자동 설정합니다.</summary>
        public int WafersPerOutputBatch { get; set; } = 0;

        /// <summary>Place completed wafer into Output Cassette.</summary>
        public async Task<bool> StoreCompletedWaferAsync(bool isGood)
        {
            try
            {
                DieGrade grade = isGood ? DieGrade.Good : DieGrade.Ng;
                Log("[OUTPUT] Store completed wafer requested. grade=" + grade);

                var ctx = new QMC.CDT320.Sequencing.MachineSequenceContext(
                    this,
                    _seqContext != null ? _seqContext.Bus : new QMC.CDT320.Sequencing.SequenceSignalBus());
                var sequence = new QMC.CDT320.Sequencing.OutputSequence(ctx);
                int result = await sequence.ExecuteStoreStageToCassetteAsync(
                    _cycleCts != null ? _cycleCts.Token : CancellationToken.None,
                    grade,
                    false,
                    0,
                    QMC.CDT320.Sequencing.SequenceStartMode.Resume).ConfigureAwait(false);
                if (result != 0)
                {
                    AlarmManager.Raise(AlarmSeverity.Error, "OUT-STORE",
                        _machine.OutputCassetteUnit != null ? _machine.OutputCassetteUnit.Name : "OutputCassette",
                        "Output store sequence failed. result=" + result);
                    return false;
                }

                Log("[OUTPUT] Store completed wafer OK. grade=" + grade);
                return true;
            }
            catch (OperationCanceledException)
            {
                Log("[OUTPUT] Store completed wafer canceled.");
                throw;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "OUT-STORE-EX",
                    _machine.OutputCassetteUnit != null ? _machine.OutputCassetteUnit.Name : "OutputCassette",
                    ex.Message);
                return false;
            }
        }

        /// <summary>Output 3 카세트 매핑 (UI 버튼).</summary>
        public async Task<bool> ScanOutputCassettesAsync()
        {
            try
            {
                var ctx = new QMC.CDT320.Sequencing.MachineSequenceContext(
                    this,
                    _seqContext != null ? _seqContext.Bus : new QMC.CDT320.Sequencing.SequenceSignalBus());
                var sequence = new QMC.CDT320.Sequencing.OutputSequence(ctx);
                int result = await sequence.ExecuteCassetteMappingAsync(
                    _cycleCts != null ? _cycleCts.Token : CancellationToken.None,
                    false,
                    0,
                    QMC.CDT320.Sequencing.SequenceStartMode.Resume).ConfigureAwait(false);
                bool ok = result == 0;
                if (ok) Log("[FEEDER] Output 3 cassette scan complete.");
                return ok;
            }
            catch (Exception ex)
            {
                Log("[FEEDER] OutputScan exception: " + ex.Message);
                return false;
            }
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
                _cycleCts?.Cancel();
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

                LotStorage.CloseLot(aborted: true);
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
        public Task ResetAlarmAsync()
        {
            Log("[RESET-ALARM] Alarm reset start...");
            int axisCount = 0, axisFail = 0;
            try
            {
                foreach (var ax in EnumerateAxes())
                {
                    try { ax.ResetAlarm(); axisCount++; }
                    catch { axisFail++; }
                }
                int activeBefore = AlarmManager.Active != null ? AlarmManager.Active.Count : 0;
                AlarmManager.ClearAll();
                Log("[RESET-ALARM] Complete (axis=" + axisCount + ", fail=" + axisFail +
                    ", active alarms cleared=" + activeBefore + ")");

                // 알람 해제 후에는 장비가 자동으로 대기/가동 상태가 된 것이 아니므로 Stopped로 둔다.
                if (_status == EquipmentStatus.Alarm) SetStatus(EquipmentStatus.Stopped);

                TryRecoverMachineInitializedFromAxisState("ResetAlarm");

                // Tower Lamp OFF(알람 해제).
                try { _machine.OpPanelUnit?.TowerLampOff(); } catch { }
            }
            catch (Exception ex)
            {
                Log("[RESET-ALARM] exception: " + ex.Message);
            }
            return Task.CompletedTask;
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
                SetMachineInitialized(false, "EmergencyStop", false);
                _cycleCts?.Cancel();
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
        // Operation mode (DryRun / StepRun) - Stage 13
        // ------------------------------------------------------------------

        /// <summary>true이면 모션 없이 진행만 수행합니다(Recipe.DryRun 영향).</summary>
        public bool DryRun { get; set; } = false;
        public bool GlobalDryRun { get; set; } = false;
        /// <summary>true이면 다이 1개마다 사용자 확인을 받습니다(Recipe.StepRun 영향). CycleMode.Step과 동일합니다.</summary>
        public bool StepRun
        {
            get => CycleMode == CycleMode.Step;
            set => CycleMode = value ? CycleMode.Step : CycleMode.Auto;
        }
        /// <summary>R3: 사이클 운영 모드(Auto/Manual/Step).</summary>
        public CycleMode CycleMode { get; set; } = CycleMode.Auto;
        /// <summary>StepRun 진행 신호입니다. GUI 콜백으로 다음 다이 진행 여부를 결정합니다.</summary>
        public event Func<int, bool> StepRunGate;

        /// <summary>현재 활성 RecipeProject의 DryRun/StepRun 설정을 적용합니다.</summary>
        public void ApplyRecipeMode(QMC.CDT320.Recipes.RecipeProject p)
        {
            if (p == null) return;
            DryRun = GlobalDryRun || p.DryRun;
            StepRun = p.StepRun;
            // Stage 33: Recipe ModuleSubset 파라미터를 Controller 옵션에 반영합니다.
            if (p.Module != null)
            {
                if (p.Module.ColletCleanEnable && p.Module.ColletCleanInterval > 0)
                    DiesPerColletClean = p.Module.ColletCleanInterval;
                else if (!p.Module.ColletCleanEnable)
                    DiesPerColletClean = 0;
            }
            // Stage 54: Recipe.Output 파라미터 적용.
            if (p.Output != null)
            {
                if (p.Output.DiesPerWafer > 0)
                    DiesPerWafer = p.Output.DiesPerWafer;
                if (p.Output.WafersPerOutputBatch > 0)
                    WafersPerOutputBatch = p.Output.WafersPerOutputBatch;
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
            Log($"[MODE] DryRun={DryRun}  StepRun={StepRun}  EbrMode={p.EbrMode}  ColletEvery={DiesPerColletClean}  DiesPerWafer={DiesPerWafer}");
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

                AxisMoveWaitResult waitResult = await WaitAxisMoveDoneInPositionAsync(axis, position).ConfigureAwait(false);
                if (!waitResult.Success)
                {
                    string message = "Move wait/in-position failed. " + AxisMoveWaiter.FormatResult(waitResult, axis.Name);
                    AlarmManager.Raise(AlarmSeverity.Error, AxisMoveWaiter.ResolveAlarmCode("MOVE-AXIS", waitResult), axis.Name, message);
                    Log("[MOVE] " + message);
                    return -1;
                }

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
            string finder = "ReticleFinder")
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
                    var m = await VisionComm.VisionHub.Wafer.MatchAsync(finder, i, 1500);
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

        // Common equipment button actions.


        // 홈잡을때 사용함.!
        public async Task<int> InitializeAxisAsync(string axisName)
        {
            try
            {
                LastActionFailureMessage = "";
                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Sequence 실행 중에는 축 초기화를 수행할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxis",
                        "Axis initialize failed: sequence is running. axis=" + axisName + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-AXIS-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                var axis = FindAxisByName(axisName);
                if (axis == null)
                {
                    LastActionFailureMessage = "초기화할 축을 찾을 수 없습니다. axis=" + axisName;
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxis",
                        "Axis initialize failed: axis not found. axis=" + axisName + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-AXIS-NOTFOUND", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                SetMachineInitialized(false, "InitializeAxisStart:" + axis.Name, false);
                SetStatus(EquipmentStatus.Initializing);
                int result = await InitializeAxisCoreAsync(axis).ConfigureAwait(false);
                if (result != 0)
                {
                    SetMachineInitialized(false, "InitializeAxisFailed:" + axis.Name, true);
                    SetStatus(EquipmentStatus.Alarm);
                    return result;
                }

                TryRecoverMachineInitializedFromAxisState("InitializeAxis:" + axis.Name);
                SaveMachineRuntimeState("InitializeAxis:" + axis.Name);
                SetStatus(EquipmentStatus.Idle);
                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "축 초기화 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxis",
                    "Axis initialize failed. axis=" + axisName + ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-AXIS-EX", "MachineController", LastActionFailureMessage);
                SetMachineInitialized(false, "InitializeAxisException", true);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        public async Task<int> InitializeAxisGroupAsync(string groupName)
        {
            try
            {
                LastActionFailureMessage = "";
                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Sequence 실행 중에는 축 그룹 초기화를 수행할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisGroup",
                        "Axis group initialize failed: sequence is running. group=" + groupName + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-GROUP-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                var plan = AxisInitializePlanStore.LoadOrCreateDefault(EnumerateAxes());
                var steps = ResolveInitializeStepsByGroup(plan, groupName);
                if (steps.Count == 0)
                {
                    LastActionFailureMessage = "초기화할 축 그룹을 찾을 수 없습니다. group=" + groupName;
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisGroup",
                        "Axis group initialize failed: initialize step group is empty. group=" + groupName + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-GROUP-EMPTY", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                SetMachineInitialized(false, "InitializeGroupStart:" + groupName, false);
                SetStatus(EquipmentStatus.Initializing);
                int initResult = await ExecuteInitializeStepsAsync(steps).ConfigureAwait(false);
                if (initResult != 0)
                {
                    SetMachineInitialized(false, "InitializeGroupFailed:" + groupName, true);
                    SetStatus(EquipmentStatus.Alarm);
                    return initResult;
                }

                TryRecoverMachineInitializedFromAxisState("InitializeAxisGroup:" + groupName);
                SaveMachineRuntimeState("InitializeAxisGroup:" + groupName);
                SetStatus(EquipmentStatus.Idle);
                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "축 그룹 초기화 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisGroup",
                    "Axis group initialize failed. group=" + groupName + ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-GROUP-EX", "MachineController", LastActionFailureMessage);
                SetMachineInitialized(false, "InitializeGroupException", true);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }


        // 여기 사용함
        public async Task<int> InitializeAllAxesAsync(bool markMachineReady)
        {
            try
            {
                LastActionFailureMessage = "";
                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Sequence 실행 중에는 전체 축 초기화를 수행할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAllAxes",
                        "All axes initialize failed: sequence is running. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-ALL-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                var allAxes = new List<BaseAxis>(EnumerateAxes());
                if (allAxes.Count == 0)
                {
                    LastActionFailureMessage = "초기화할 축 정보가 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAllAxes",
                        "All axes initialize failed: axis list is empty. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-ALL-EMPTY", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                var plan = AxisInitializePlanStore.LoadOrCreateDefault(allAxes);
                var steps = ResolveEnabledInitializeSteps(plan);
                if (steps.Count == 0)
                {
                    LastActionFailureMessage = "초기화 Plan Step 정보가 없습니다. file=" + AxisInitializePlanStore.PlanPath;
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAllAxes",
                        "All axes initialize failed: initialize plan has no enabled step. file=" +
                        AxisInitializePlanStore.PlanPath + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-PLAN-EMPTY", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                SetMachineInitialized(false, "InitializeAllAxesStart", false);
                SetStatus(EquipmentStatus.Initializing);
                Log("[INIT] Axis initialize plan start. file=" + AxisInitializePlanStore.PlanPath);

                int initResult = await ExecuteInitializeStepsAsync(steps).ConfigureAwait(false);
                if (initResult != 0)
                {
                    SetMachineInitialized(false, "InitializeAllAxesFailed", true);
                    SetStatus(EquipmentStatus.Alarm);
                    return initResult;
                }

                if (markMachineReady)
                {
                    SetMachineInitialized(true, "InitializeAllAxesComplete", true);
                    SetStatus(EquipmentStatus.Ready);
                }
                else
                {
                    SaveMachineRuntimeState("InitializeAllAxes");
                }

                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "전체 축 초기화 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAllAxes",
                    "All axes initialize failed: " + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-ALL-EX", "MachineController", LastActionFailureMessage);
                SetMachineInitialized(false, "InitializeAllAxesException", true);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }


        //홈잡을때 사용.
        public AxisInitializePlan GetAxisInitializePlan()
        {
            try
            {
                return AxisInitializePlanStore.LoadOrCreateDefault(EnumerateAxes());
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "GetAxisInitializePlan",
                    "Get initialize plan failed: " + ex.Message + " - Failed");
                return AxisInitializePlanStore.CreateDefault(EnumerateAxes());
            }
            finally
            {
            }
        }

        public IList<AxisInitializeStepProgress> GetAxisInitializeStepStatusSnapshot()
        {
            try
            {
                EnsureAxisInitializeStepStatesLoadedFromRuntimeState();

                AxisInitializePlan plan = GetAxisInitializePlan();
                if (plan == null || plan.Steps == null)
                    return new List<AxisInitializeStepProgress>();

                return plan.Steps
                    .Where(x => x != null)
                    .Select(GetValidatedAxisInitializeStepProgress)
                    .ToList();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "GetAxisInitializeStepStatusSnapshot",
                    "Get initialize step status snapshot failed: " + ex.Message + " - Failed");
                return new List<AxisInitializeStepProgress>();
            }
            finally
            {
            }
        }

        public async Task<int> InitializePlanStepAsync(int stepNo)
        {
            try
            {
                LastActionFailureMessage = "";
                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Sequence 실행 중에는 초기화 Step을 수행할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-STEP-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                var plan = GetAxisInitializePlan();
                var steps = plan != null && plan.Steps != null
                    ? plan.Steps.Where(x => x != null && x.Enabled && x.StepNo == stepNo).ToList()
                    : new List<AxisInitializeStep>();

                if (steps.Count == 0)
                {
                    LastActionFailureMessage = "실행할 초기화 Step을 찾을 수 없습니다. step=" + stepNo;
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-STEP-NOTFOUND", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                SetMachineInitialized(false, "InitializePlanStepStart:" + stepNo, false);
                SetStatus(EquipmentStatus.Initializing);

                lock (_initializeHomedAxisLock)
                {
                    _initializeHomedAxisNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                int result;
                if (steps.Count == 1)
                {
                    ResetAxisInitializeStepProgressForRun(steps);
                    result = await ExecuteInitializeSingleStepAsync(steps[0]).ConfigureAwait(false);
                }
                else
                {
                    ResetAxisInitializeStepProgressForRun(steps);
                    result = 0;
                    foreach (AxisInitializeStep step in steps.OrderBy(x => x.GroupName))
                    {
                        int stepResult = await ExecuteInitializeSingleStepAsync(step).ConfigureAwait(false);
                        if (stepResult != 0)
                        {
                            result = stepResult;
                            break;
                        }
                    }
                }

                SaveMachineRuntimeState("InitializePlanStep:" + stepNo);
                SetStatus(result == 0 ? EquipmentStatus.Idle : EquipmentStatus.Alarm);
                return result;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "초기화 Step 실행 실패: " + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-STEP-DIALOG-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
                lock (_initializeHomedAxisLock)
                {
                    _initializeHomedAxisNames = null;
                }
            }
        }

        public async Task<int> InitializeAllAxesForMonitorAsync()
        {
            return await InitializeAllAxesAsync(true).ConfigureAwait(false);
        }

        //홈잡을때 사용함.!
        private async Task<int> InitializeAxisCoreAsync(BaseAxis axis)
        {
            try
            {
                if (axis == null)
                {
                    LastActionFailureMessage = "초기화할 축 정보가 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                        "Axis initialize failed: axis is null. - Failed");
                    return -1;
                }

                if (IsAxisAlreadyHomedInCurrentInitialize(axis))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                        "Axis initialize skipped: already homed in current initialize sequence. axis=" + axis.Name + " - Ok");
                    return 0;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                    "Axis initialize requested. axis=" + axis.Name + " - Start");

                try
                {
                    var stopAxes = _axisInterferenceMap.ResolveInterferenceAxes(axis.Name);
                    await StopAxesAsync(stopAxes, false).ConfigureAwait(false);
                }
                catch (Exception stopEx)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                        "Axis interference stop failed before initialize. axis=" + axis.Name +
                        ", error=" + stopEx.Message + " - Failed");
                }

                axis.ServoOff();
                Thread.Sleep(500);

                axis.ResetAlarm();
                Thread.Sleep(500);

                axis.ServoOn();
                Thread.Sleep(500);

                if (!axis.IsServoOn)
                {
                    LastActionFailureMessage = axis.Name + " Servo ON 실패";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                        "Axis initialize failed: servo is off. axis=" + axis.Name + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-SERVO-" + axis.Name, axis.Name, LastActionFailureMessage);
                    return -1;
                }

                int homeResult = await axis.HomeSearchAsync().ConfigureAwait(false);
                if (homeResult != 0)
                {
                    LastActionFailureMessage = BuildAxisMotionFailureMessage(axis, "HOME 실패", homeResult);
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                        "Axis initialize failed: home search failed. axis=" + axis.Name +
                        ", result=" + homeResult + ", message=" + LastActionFailureMessage + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "HOME-" + axis.Name, axis.Name, LastActionFailureMessage);
                    return homeResult;
                }

                if (axis.IsAlarm)
                {
                    LastActionFailureMessage = axis.Name + " HOME 중 Alarm 발생. code=" + axis.AlarmCode;
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                        "Axis initialize failed: axis alarm. axis=" + axis.Name +
                        ", code=" + axis.AlarmCode + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "HOME-" + axis.Name, axis.Name, LastActionFailureMessage);
                    return -1;
                }

                if (!axis.IsHomeDone)
                {
                    LastActionFailureMessage = axis.Name + " HOME 완료 신호가 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                        "Axis initialize failed: home done is false. axis=" + axis.Name + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "HOME-NOTDONE-" + axis.Name, axis.Name, LastActionFailureMessage);
                    return -1;
                }


                // 홈 잡고 Avoid로 움직이는 부분 막자.
                // 홈 시컨스에서 제어 하도록 하고 메뉴얼로 할떄는 홈잡고 멈춰야한다!
                //int completeHomeResult = await CompleteAxisHomeConditionAsync(axis).ConfigureAwait(false);
                //if (completeHomeResult != 0)
                //    return completeHomeResult;

                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                    "Axis initialize completed. axis=" + axis.Name + " - Ok");
                MarkAxisHomedInCurrentInitialize(axis);
                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "축 초기화 실패: " + (axis != null ? axis.Name : "-") + " / " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisCore",
                    "Axis initialize failed. axis=" + (axis != null ? axis.Name : "-") +
                    ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-AXIS-CORE-EX", "MachineController", LastActionFailureMessage);
                return -1;
            }
            finally
            {
            }
        }

        private static string BuildAxisMotionFailureMessage(BaseAxis axis, string action, int result)
        {
            if (axis == null)
                return (action ?? "Motion") + " 실패. result=" + result;

            if (!string.IsNullOrWhiteSpace(axis.LastMotionFailureMessage))
                return axis.LastMotionFailureMessage;

            string reason;
            if (axis.IsAlarm)
                reason = "Axis alarm is ON. AlarmCode=0x" + axis.AlarmCode.ToString("X4");
            else if (!axis.IsServoOn)
                reason = "Servo is OFF.";
            else if (result == -11)
                reason = "Motion guard/interlock blocked.";
            else if (result == -2)
                reason = "Axis is not ready.";
            else
                reason = "Motion failed.";

            string unit = axis.Setup != null ? axis.Setup.Unit : string.Empty;
            return axis.Name + " " + (action ?? "Motion") +
                   ". result=" + result +
                   ", reason=" + reason +
                   ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") +
                   ", pos=" + axis.ActualPosition.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " " + unit;
        }

        private bool IsAxisAlreadyHomedInCurrentInitialize(BaseAxis axis)
        {
            try
            {
                if (axis == null || string.IsNullOrWhiteSpace(axis.Name))
                    return false;

                lock (_initializeHomedAxisLock)
                {
                    return _initializeHomedAxisNames != null &&
                           _initializeHomedAxisNames.Contains(axis.Name);
                }
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private void MarkAxisHomedInCurrentInitialize(BaseAxis axis)
        {
            try
            {
                if (axis == null || string.IsNullOrWhiteSpace(axis.Name))
                    return;

                lock (_initializeHomedAxisLock)
                {
                    if (_initializeHomedAxisNames != null)
                        _initializeHomedAxisNames.Add(axis.Name);
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private async Task<int> CompleteAxisHomeConditionAsync(BaseAxis axis)
        {
            try
            {
                if (axis == null)
                    return 0;

                switch (axis.Name)
                {
                    //case "InputLifterZ":
                    //    return await MoveInputLifterZToAvoidAfterHomeAsync().ConfigureAwait(false);
                    // 아웃풋 스테이지 Z축 HOME 후 Avoid 이동
                    case "GoodStage_StageZ":
                        return await MoveOutputStageZToAvoidAsync().ConfigureAwait(false);
                    // 프론트 피커 Y축 HOME 후 Avoid 이동
                    case "FrontPickerY":
                        return await MoveFrontPickerYToAvoidAfterHomeAsync().ConfigureAwait(false);
                    // 리어 피커 Y축 HOME 후 Avoid 이동
                    case "RearPickerY":
                        return await MoveRearPickerYToAvoidAfterHomeAsync().ConfigureAwait(false);
                    // 인풋 피더 HOME 후 리프트 다운
                    case "InputFeederY":
                    case "FeederY":
                        return await MoveInputFeederLiftDownAfterHomeAsync().ConfigureAwait(false);
                    // 인풋 스테이지 Y축 HOME 후 Avoid 이동
                    case "StageY":
                        return await MoveInputStageYToAvoidAfterHomeAsync().ConfigureAwait(false);
                    default:
                        return await CompleteDefaultAxisHomeAsync(axis).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                return FailInitializePreparation("축 HOME 후처리 실패. axis=" +
                    (axis != null ? axis.Name : "-") + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private Task<int> PrepareDefaultAxisHomeAsync(BaseAxis axis)
        {
            // TODO: 기본 축 HOME 전 조건이 있으면 여기에 공통 로직을 채우면 됩니다.
            return Task.FromResult(0);
        }

        private Task<int> CompleteDefaultAxisHomeAsync(BaseAxis axis)
        {
            // TODO: 기본 축 HOME 후 안전 위치 이동/상태 확인이 있으면 여기에 채우면 됩니다.
            return Task.FromResult(0);
        }

        private async Task<int> MoveInputLifterZToAvoidAfterHomeAsync()
        {
            try
            {
                Log("[INIT] Complete InputLifterZ home: move lifter to Avoid.");

                var cassette = _machine != null ? _machine.InputCassetteUnit : null;
                if (cassette == null)
                    return 0;

                if (cassette.IsWaferLifterZInAvoidPosition())
                    return 0;

                int result = await cassette.MoveToWaferCassetteAvoidPosition().ConfigureAwait(false);
                if (result != 0)
                    return FailInitializePreparation("InputLifterZ avoid move failed. result=" + result);

                AxisMoveWaitResult waitResult = await cassette.WaitWaferLifterZMoveDoneInPosition(
                    cassette.Recipe.AvoidPosition,
                    cassette.ResolveWaferLifterZMoveTimeoutMs()).ConfigureAwait(false);
                if (!waitResult.Success)
                    return FailInitializePreparation(
                        "InputLifterZ avoid move/in-position wait failed. " +
                        AxisMoveWaiter.FormatResult(waitResult, "InputLifterZ avoid"));

                return 0;
            }
            catch (Exception ex)
            {
                return FailInitializePreparation("InputLifterZ avoid after home exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> PrepareInputLifterZHomeAsync(BaseAxis axis)
        {
            return await PrepareInputLifterHomeAsync().ConfigureAwait(false);
        }

        private async Task<int> PrepareOutputLifterZHomeAsync(BaseAxis axis)
        {
            return await PrepareOutputLifterHomeAsync().ConfigureAwait(false);
        }

        private async Task<int> PrepareInputFeederYHomeByAxisAsync(BaseAxis axis)
        {
            return await PrepareInputFeederHomeAsync().ConfigureAwait(false);
        }

        private async Task<int> PrepareOutputFeederYHomeByAxisAsync(BaseAxis axis)
        {
            return await PrepareOutputFeederHomeAsync().ConfigureAwait(false);
        }

        private Task<int> PrepareSharedRailXAxisHomeAsync(BaseAxis axis)
        {
            // TODO: Shared Rail X축 HOME 전 Picker Z/가이드/비전 Reticle 상태 조건이 있으면 여기에 추가하세요.
            return Task.FromResult(0);
        }

        private async Task<int> PrepareInputExpandingZHomeAsync(BaseAxis axis)
        {
            return await PrepareInputExpandingHomeAsync().ConfigureAwait(false);
        }

        private async Task<int> PrepareInputStageYHomeAsync(BaseAxis axis)
        {
            return await PrepareInputStageHomeAsync().ConfigureAwait(false);
        }

        private async Task<int> PrepareNeedleXHomeAsync(BaseAxis axis)
        {
            return await PrepareNeedleHomeAsync().ConfigureAwait(false);
        }

        private Task<int> PrepareNeedleHomeAsync()
        {
            Log("[INIT] Prepare NeedleX home: NeedleZ Avoid check.");

            var stage = _machine.InputStageUnit;
            if (stage != null && !stage.IsNeedleZInSafePosition())
            {
                return Task.FromResult(FailInitializePreparation(
                    "NeedleX HOME 불가: NeedleZ가 Avoid 위치에 있지 않습니다."));
            }

            return Task.FromResult(0);
        }

        private async Task<int> PrepareInputVisionXHomeAsync(BaseAxis axis)
        {
            return await PrepareInputVisionHomeAsync().ConfigureAwait(false);
        }

        private Task<int> PrepareInputVisionHomeAsync()
        {
            Log("[INIT] Prepare InputVisionX home: InputFeederY Avoid check, feeder cylinder down.");

            var feeder = _machine.InputFeederUnit;
            if (feeder != null && !feeder.IsWaferFeederInAvoidPosition())
            {
                return Task.FromResult(FailInitializePreparation(
                    "InputVisionX HOME 불가: InputFeederY가 Avoid 위치에 있지 않습니다."));
            }

            if (feeder != null && !feeder.IsWaferFeederDown())
            {
                return Task.FromResult(FailInitializePreparation(
                    "InputVisionX HOME 불가: InputFeeder 실린더가 Down 위치에 있지 않습니다."));
            }

            return Task.FromResult(0);
        }

        private async Task<int> PrepareFrontPickerXHomeAsync(BaseAxis axis)
        {
            return await PrepareFrontPickerXHomeConditionAsync().ConfigureAwait(false);
        }

        private async Task<int> PrepareFrontPickerXHomeConditionAsync()
        {
            Log("[INIT] Check FrontPickerX home: InputVisionX / InputExpandingZ / FrontPickerY / FrontPickerZ0~Z3 / InputFeederY Avoid, feeder cylinder down.");

            var stage = _machine.InputStageUnit;
            if (stage != null && !stage.IsVisionXInAvoidPosition())
            {
                return FailInitializePreparation(
                    "FrontPickerX HOME 불가: InputVisionX가 Avoid 위치에 있지 않습니다.");
            }

            if (stage != null && !IsExpanderZHomeAvoidOrProcess(stage))
            {
                return FailInitializePreparation(
                    "FrontPickerX HOME 불가: InputExpandingZ가 Home(0), Avoid, Process 위치가 아닙니다.");
            }

            var front = _machine.PickerFrontUnit;
            if (front != null && !front.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
            {
                return FailInitializePreparation(
                    "FrontPickerX HOME 불가: FrontPickerY가 Avoid 위치에 있지 않습니다.");
            }

            int result = await CheckFrontPickerZAxesAvoidAsync().ConfigureAwait(false);
            if (result != 0)
                return result;

            var feeder = _machine.InputFeederUnit;
            if (feeder != null)
            {
                if (!feeder.IsWaferFeederYInAvoidPosition())
                {
                    return FailInitializePreparation(
                        "FrontPickerX HOME 불가: InputFeederY가 Avoid 위치에 있지 않습니다.");
                }

                if (!feeder.IsWaferFeederDown())
                {
                    return FailInitializePreparation(
                        "FrontPickerX HOME 불가: InputFeeder 실린더가 Down 위치에 있지 않습니다.");
                }
            }

            return 0;
        }

        private async Task<int> PrepareFrontPickerYHomeAsync(BaseAxis axis)
        {
            return await PrepareFrontPickerYHomeConditionAsync().ConfigureAwait(false);
        }

        private async Task<int> PrepareFrontPickerYHomeConditionAsync()
        {
            Log("[INIT] Check FrontPickerY home: FrontPickerZ0~Z3 Home(0) or Avoid.");

            return await CheckFrontPickerZAxesHomeOrAvoidAsync().ConfigureAwait(false);
        }

        private Task<int> CheckFrontPickerZAxesAvoidAsync()
        {
            var front = _machine.PickerFrontUnit;
            if (front != null)
            {
                PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
                foreach (PickerAxis zAxis in zAxes)
                {
                    if (!front.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    {
                        return Task.FromResult(FailInitializePreparation(
                            "FrontPickerY HOME 불가: Front" + zAxis + "가 Avoid 위치에 있지 않습니다."));
                    }
                }
            }

            return Task.FromResult(0);
        }

        private Task<int> CheckRearPickerZAxesAvoidAsync()
        {
            var rear = _machine.PickerRearUnit;
            if (rear != null)
            {
                PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
                foreach (PickerAxis zAxis in zAxes)
                {
                    if (!rear.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    {
                        return Task.FromResult(FailInitializePreparation(
                            "RearPickerY HOME 불가: Rear" + zAxis + "가 Avoid 위치에 있지 않습니다."));
                    }
                }
            }

            return Task.FromResult(0);
        }

        private Task<int> CheckFrontPickerZAxesHomeOrAvoidAsync()
        {
            var front = _machine.PickerFrontUnit;
            if (front != null)
            {
                PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
                foreach (PickerAxis zAxis in zAxes)
                {
                    BaseAxis axis = ResolveFrontPickerAxis(front, zAxis);
                    if (!IsAxisAtHomeOrTeachingAvoid(axis, () => front.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition")))
                    {
                        return Task.FromResult(FailInitializePreparation(
                            "FrontPickerY HOME 불가: Front" + zAxis + "가 Home(0) 또는 Avoid 위치에 있지 않습니다."));
                    }
                }
            }

            return Task.FromResult(0);
        }

        private Task<int> CheckRearPickerZAxesHomeOrAvoidAsync()
        {
            var rear = _machine.PickerRearUnit;
            if (rear != null)
            {
                PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
                foreach (PickerAxis zAxis in zAxes)
                {
                    BaseAxis axis = ResolveRearPickerAxis(rear, zAxis);
                    if (!IsAxisAtHomeOrTeachingAvoid(axis, () => rear.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition")))
                    {
                        return Task.FromResult(FailInitializePreparation(
                            "RearPickerY HOME 불가: Rear" + zAxis + "가 Home(0) 또는 Avoid 위치에 있지 않습니다."));
                    }
                }
            }

            return Task.FromResult(0);
        }

        private async Task<int> PrepareRearPickerYHomeAsync(BaseAxis axis)
        {
            return await PrepareRearPickerYHomeConditionAsync().ConfigureAwait(false);
        }

        private async Task<int> PrepareRearPickerYHomeConditionAsync()
        {
            Log("[INIT] Check RearPickerY home: RearPickerZ0~Z3 Home(0) or Avoid.");

            return await CheckRearPickerZAxesHomeOrAvoidAsync().ConfigureAwait(false);
        }

        private async Task<int> PrepareRearPickerXHomeAsync(BaseAxis axis)
        {
            return await PrepareRearPickerXHomeConditionAsync().ConfigureAwait(false);
        }

        private async Task<int> PrepareRearPickerXHomeConditionAsync()
        {
            Log("[INIT] Check RearPickerX home: InputVisionX / InputExpandingZ / FrontPickerY / RearPickerY / RearPickerZ0~Z3 Avoid.");

            var stage = _machine.InputStageUnit;
            if (stage != null && !stage.IsVisionXInAvoidPosition())
            {
                return FailInitializePreparation(
                    "RearPickerX HOME 불가: InputVisionX가 Avoid 위치에 있지 않습니다.");
            }

            if (stage != null && !IsExpanderZHomeAvoidOrProcess(stage))
            {
                return FailInitializePreparation(
                    "RearPickerX HOME 불가: InputExpandingZ가 Home(0), Avoid, Process 위치가 아닙니다.");
            }

            var front = _machine.PickerFrontUnit;
            if (front != null && !front.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
            {
                return FailInitializePreparation(
                    "RearPickerX HOME 불가: FrontPickerY가 Avoid 위치에 있지 않습니다.");
            }

            var rear = _machine.PickerRearUnit;
            if (rear != null && !rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
            {
                return FailInitializePreparation(
                    "RearPickerX HOME 불가: RearPickerY가 Avoid 위치에 있지 않습니다.");
            }

            int result = await CheckRearPickerZAxesAvoidAsync().ConfigureAwait(false);
            if (result != 0)
                return result;

            return 0;
        }

        private async Task<int> PrepareOutputVisionXHomeAsync(BaseAxis axis)
        {
            return await PrepareOutputVisionHomeAsync().ConfigureAwait(false);
        }

        private Task<int> PrepareOutputVisionHomeAsync()
        {
            Log("[INIT] Check OutputVisionX home: output feeder clear. PickerX clearance is checked by SharedRailX rules.");

            var outputFeeder = _machine.OutputFeederUnit;
            if (outputFeeder != null && !outputFeeder.IsBinFeederYInAvoidPosition())
            {
                return Task.FromResult(FailInitializePreparation(
                    "OutputVisionX HOME 불가: OutputFeederY가 Avoid 위치에 있지 않습니다."));
            }

            if (outputFeeder != null && !outputFeeder.IsFeederDown())
            {
                return Task.FromResult(FailInitializePreparation(
                    "OutputVisionX HOME 불가: OutputFeeder 실린더가 Down 위치에 있지 않습니다."));
            }

            return Task.FromResult(0);
        }

        private async Task<int> PrepareOutputGoodStageYHomeAsync(BaseAxis axis)
        {
            return await PrepareOutputGoodStageHomeAsync().ConfigureAwait(false);
        }

        private Task<int> PrepareOutputGoodStageHomeAsync()
        {
            Log("[INIT] Check OutputGoodStageY home: OutputGoodStageZ Avoid.");

            var outputStage = _machine.OutputStageUnit;
            if (outputStage != null && outputStage.GoodStage != null && !outputStage.GoodStage.IsAtAvoidPosition())
            {
                return Task.FromResult(FailInitializePreparation(
                    "OutputGoodStageY HOME 불가: OutputGoodStageZ가 Avoid 위치에 있지 않습니다."));
            }

            return Task.FromResult(0);
        }

        private async Task<int> PrepareOutputNgStageYHomeAsync(BaseAxis axis)
        {
            return await PrepareOutputNgStageHomeAsync().ConfigureAwait(false);
        }

        private Task<int> PrepareOutputNgStageHomeAsync()
        {
            Log("[INIT] Check OutputNgStageY home: GoodBinZ Avoid / GoodBinGuide Down / NgBinGuide Down / NgBinClamp Up.");

            var outputStage = _machine.OutputStageUnit;
            if (outputStage == null)
                return Task.FromResult(0);

            if (outputStage.GoodStage != null && !outputStage.GoodStage.IsAtAvoidPosition())
            {
                return Task.FromResult(FailInitializePreparation(
                    "OutputNgStageY HOME 불가: GoodBinZ(GoodStageZ)가 Avoid 위치에 있지 않습니다."));
            }

            if (!outputStage.IsBinGuideDown(BinSide.Good))
            {
                return Task.FromResult(FailInitializePreparation(
                    "OutputNgStageY HOME 불가: Good Bin Guide 실린더가 Down 상태가 아닙니다."));
            }

            if (!outputStage.IsBinGuideDown(BinSide.Ng))
            {
                return Task.FromResult(FailInitializePreparation(
                    "OutputNgStageY HOME 불가: NG Bin Guide 실린더가 Down 상태가 아닙니다."));
            }

            if (!outputStage.IsBinGuideClampLiftUp(BinSide.Ng))
            {
                return Task.FromResult(FailInitializePreparation(
                    "OutputNgStageY HOME 불가: NG Bin Clamp 실린더가 Up 상태가 아닙니다."));
            }

            return Task.FromResult(0);
        }

        private async Task<int> PrepareFrontPickerTHomeAsync(BaseAxis axis)
        {
            return await CheckFrontPickerTPairedZAvoidAsync(axis).ConfigureAwait(false);
        }

        private Task<int> CheckFrontPickerTPairedZAvoidAsync(BaseAxis axis)
        {
            Log("[INIT] Prepare FrontPickerT home: check paired FrontPickerZ Avoid.");

            var front = _machine.PickerFrontUnit;
            if (front == null || axis == null)
                return Task.FromResult(0);

            PickerAxis zAxis;
            switch (axis.Name)
            {
                // 프론트 T0축과 Z0축 Avoid 상태 연결
                case "FrontPickerT0": zAxis = PickerAxis.PickerZ0; break;
                // 프론트 T1축과 Z1축 Avoid 상태 연결
                case "FrontPickerT1": zAxis = PickerAxis.PickerZ1; break;
                // 프론트 T2축과 Z2축 Avoid 상태 연결
                case "FrontPickerT2": zAxis = PickerAxis.PickerZ2; break;
                // 프론트 T3축과 Z3축 Avoid 상태 연결
                case "FrontPickerT3": zAxis = PickerAxis.PickerZ3; break;
                default: return Task.FromResult(0);
            }

            if (!front.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
            {
                return Task.FromResult(FailInitializePreparation(
                    axis.Name + " HOME 불가: Front" + zAxis + "가 Avoid 위치에 있지 않습니다."));
            }

            return Task.FromResult(0);
        }

        private async Task<int> PrepareRearPickerTHomeAsync(BaseAxis axis)
        {
            return await CheckRearPickerTPairedZAvoidAsync(axis).ConfigureAwait(false);
        }

        private Task<int> CheckRearPickerTPairedZAvoidAsync(BaseAxis axis)
        {
            Log("[INIT] Prepare RearPickerT home: check paired RearPickerZ Avoid.");

            var rear = _machine.PickerRearUnit;
            if (rear == null || axis == null)
                return Task.FromResult(0);

            PickerAxis zAxis;
            switch (axis.Name)
            {
                // 리어 T0축과 Z0축 Avoid 상태 연결
                case "RearPickerT0": zAxis = PickerAxis.PickerZ0; break;
                // 리어 T1축과 Z1축 Avoid 상태 연결
                case "RearPickerT1": zAxis = PickerAxis.PickerZ1; break;
                // 리어 T2축과 Z2축 Avoid 상태 연결
                case "RearPickerT2": zAxis = PickerAxis.PickerZ2; break;
                // 리어 T3축과 Z3축 Avoid 상태 연결
                case "RearPickerT3": zAxis = PickerAxis.PickerZ3; break;
                default: return Task.FromResult(0);
            }

            if (!rear.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
            {
                return Task.FromResult(FailInitializePreparation(
                    axis.Name + " HOME 불가: Rear" + zAxis + "가 Avoid 위치에 있지 않습니다."));
            }

            return Task.FromResult(0);
        }

        private async Task<int> MoveFrontPickerYToAvoidAfterHomeAsync()
        {
            if (_machine.PickerFrontUnit == null ||
                _machine.PickerFrontUnit.PickerY == null ||
                _machine.PickerFrontUnit.Recipe == null ||
                _machine.PickerFrontUnit.Recipe.PickerY == null)
                return 0;

            return await MoveAxisTeachingAsync(
                _machine.PickerFrontUnit.PickerY,
                _machine.PickerFrontUnit.Recipe.PickerY.AvoidPosition,
                "FrontPickerY.Avoid").ConfigureAwait(false);
        }

        private async Task<int> MoveRearPickerYToAvoidAfterHomeAsync()
        {
            if (_machine.PickerRearUnit == null ||
                _machine.PickerRearUnit.PickerY == null ||
                _machine.PickerRearUnit.Recipe == null ||
                _machine.PickerRearUnit.Recipe.PickerY == null)
                return 0;

            return await MoveAxisTeachingAsync(
                _machine.PickerRearUnit.PickerY,
                _machine.PickerRearUnit.Recipe.PickerY.AvoidPosition,
                "RearPickerY.Avoid").ConfigureAwait(false);
        }

        private async Task<int> MoveInputStageYToAvoidAfterHomeAsync()
        {
            if (_machine.InputStageUnit == null ||
                _machine.InputStageUnit.StageY == null ||
                _machine.InputStageUnit.Recipe == null ||
                _machine.InputStageUnit.Recipe.WaferY == null)
                return 0;

            return await MoveAxisTeachingAsync(
                _machine.InputStageUnit.StageY,
                _machine.InputStageUnit.Recipe.WaferY.AvoidPosition,
                "StageY.Avoid").ConfigureAwait(false);
        }

        private async Task<int> ExecuteInitializeStepsAsync(IList<AxisInitializeStep> steps)
        {
            try
            {
                if (steps == null || steps.Count == 0)
                {
                    LastActionFailureMessage = "초기화 Step 정보가 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                        "Axis initialize failed: step list is empty. - Failed");
                    return -1;
                }

                lock (_initializeHomedAxisLock)
                {
                    _initializeHomedAxisNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                var enabledSteps = steps
                    .Where(x => x != null && x.Enabled)
                    .OrderBy(x => x.StepNo)
                    .ToList();
                ResetAxisInitializeStepProgressForRun(enabledSteps);

                foreach (var batch in enabledSteps.GroupBy(x => x.StepNo))
                {
                    var batchSteps = batch.ToList();
                    if (batchSteps.Count == 1)
                    {
                        int singleResult = await ExecuteInitializeSingleStepAsync(batchSteps[0]).ConfigureAwait(false);
                        if (singleResult != 0)
                            return singleResult;
                    }
                    else
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                            "Axis initialize same-step serial batch start. step=" + batch.Key +
                            ", groups=" + string.Join(",", batchSteps.Select(x => x.GroupName).ToArray()) + " - Start");

                        foreach (AxisInitializeStep batchStep in batchSteps)
                        {
                            int serialResult = await ExecuteInitializeSingleStepAsync(batchStep).ConfigureAwait(false);
                            if (serialResult != 0)
                                return serialResult;
                        }

                        QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                            "Axis initialize same-step serial batch completed. step=" + batch.Key + " - Ok");
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "초기화 Step 실행 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                    "Axis initialize step execution failed: " + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-STEP-EX", "MachineController", LastActionFailureMessage);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteInitializeSingleStepAsync(AxisInitializeStep step)
        {
            try
            {
                if (step == null || !step.Enabled)
                    return 0;

                var axes = ResolveAxesByNames(step.AxisNames);
                bool hasActions = HasEnabledInitializeActions(step);
                if (axes.Count == 0 && !hasActions)
                {
                    LastActionFailureMessage = "초기화 Step에 유효한 축이 없습니다. step=" + step.StepNo +
                        ", group=" + step.GroupName;
                    QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeStep",
                        "Axis initialize step failed: no valid axes. step=" + step.StepNo +
                        ", group=" + step.GroupName + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "INIT-STEP-EMPTY", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeStep",
                    "Axis initialize step start. step=" + step.StepNo +
                    ", group=" + step.GroupName +
                    ", mode=" + step.RunMode +
                    ", interlockGroup=" + step.InterlockGroup +
                    ", axes=" + string.Join(",", axes.Select(x => x.Name).ToArray()) + " - Start");
                RaiseAxisInitializeStepProgress(step, AxisInitializeStepStatus.Running, "");

                int prepareResult = await PrepareInitializeStepAsync(step).ConfigureAwait(false);
                if (prepareResult != 0)
                {
                    RaiseAxisInitializeStepProgress(step, AxisInitializeStepStatus.Failed, LastActionFailureMessage);
                    return prepareResult;
                }

                string interlockReason;
                if (_axisInitializeInterlocks != null &&
                    !_axisInitializeInterlocks.VerifyStep(step, out interlockReason))
                {
                    LastActionFailureMessage = interlockReason;
                    RaiseAxisInitializeStepProgress(step, AxisInitializeStepStatus.Failed, LastActionFailureMessage);
                    return -1;
                }

                await StopInitializeInterlockGroupAsync(step).ConfigureAwait(false);

                int preActionResult = await ExecuteInitializeActionsAsync(step, step.PreActions, "PreActions").ConfigureAwait(false);
                if (preActionResult != 0)
                {
                    RaiseAxisInitializeStepProgress(step, AxisInitializeStepStatus.Failed, LastActionFailureMessage);
                    return preActionResult;
                }

                int result = 0;
                if (axes.Count > 0)
                {
                    result = AxisInitializeRunMode.IsParallel(step.RunMode)
                        ? await ExecuteInitializeStepParallelAsync(step, axes).ConfigureAwait(false)
                        : await ExecuteInitializeStepSerialAsync(step, axes).ConfigureAwait(false);
                }

                if (result != 0)
                {
                    RaiseAxisInitializeStepProgress(step, AxisInitializeStepStatus.Failed, LastActionFailureMessage);
                    return result;
                }

                int postActionResult = await ExecuteInitializeActionsAsync(step, step.PostActions, "PostActions").ConfigureAwait(false);
                if (postActionResult != 0)
                {
                    RaiseAxisInitializeStepProgress(step, AxisInitializeStepStatus.Failed, LastActionFailureMessage);
                    return postActionResult;
                }

                // 제거 요망 함수.
                // 개별 축에서 문제된다고 판단. AxisInitializenPlan에 이동 함수 만들어서 사용.
                //int completeResult = await CompleteInitializeStepAsync(step).ConfigureAwait(false);
                //if (completeResult != 0)
                //{
                //    RaiseAxisInitializeStepProgress(step, AxisInitializeStepStatus.Failed, LastActionFailureMessage);
                //    return completeResult;
                //}

                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeStep",
                    "Axis initialize step completed. step=" + step.StepNo +
                    ", group=" + step.GroupName + " - Ok");
                RaiseAxisInitializeStepProgress(step, AxisInitializeStepStatus.Complete, "");
                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "초기화 Step 실행 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeStep",
                    "Axis initialize step execution failed. step=" + (step != null ? step.StepNo : 0) +
                    ", group=" + (step != null ? step.GroupName : "") +
                    ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-STEP-EX", "MachineController", LastActionFailureMessage);
                RaiseAxisInitializeStepProgress(step, AxisInitializeStepStatus.Failed, LastActionFailureMessage);
                return -1;
            }
            finally
            {
            }
        }

        private void RaiseAxisInitializeStepProgress(AxisInitializeStep step, string status, string message)
        {
            AxisInitializeStepProgress progress = AxisInitializeStepProgress.Create(step, status, message);
            SetAxisInitializeStepProgress(progress);

            var handler = AxisInitializeStepProgressChanged;
            if (handler == null)
                return;

            try
            {
                handler(progress);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ResetAxisInitializeStepProgressForRun(IEnumerable<AxisInitializeStep> steps)
        {
            if (steps == null)
                return;

            foreach (AxisInitializeStep step in steps.Where(x => x != null && x.Enabled))
            {
                AxisInitializeStepProgress progress = AxisInitializeStepProgress.Create(
                    step,
                    AxisInitializeStepStatus.Waiting,
                    "");
                SetAxisInitializeStepProgress(progress);

                var handler = AxisInitializeStepProgressChanged;
                if (handler == null)
                    continue;

                try
                {
                    handler(progress);
                }
                catch
                {
                }
                finally
                {
                }
            }
        }

        private AxisInitializeStepProgress GetValidatedAxisInitializeStepProgress(AxisInitializeStep step)
        {
            AxisInitializeStepProgress progress;
            lock (_axisInitializeStepStateLock)
            {
                _axisInitializeStepStates.TryGetValue(GetAxisInitializeStepStateKey(step), out progress);
            }

            if (progress == null)
            {
                if (_isMachineInitialized)
                {
                    string initializedReason;
                    if (CheckAxisInitializeStepStillValid(step, out initializedReason))
                    {
                        progress = AxisInitializeStepProgress.Create(step, AxisInitializeStepStatus.Complete, "");
                        SetAxisInitializeStepProgress(progress);
                        return progress;
                    }
                }

                progress = AxisInitializeStepProgress.Create(
                    step,
                    step != null && step.Enabled ? AxisInitializeStepStatus.Waiting : AxisInitializeStepStatus.Disabled,
                    "");
            }
            else
            {
                progress = new AxisInitializeStepProgress
                {
                    StepNo = progress.StepNo,
                    GroupName = progress.GroupName,
                    Status = progress.Status,
                    Message = progress.Message
                };
            }

            if (step == null || !step.Enabled)
                return progress;

            if (ShouldResolveInitializedStepStatus(progress.Status, _isMachineInitialized))
            {
                string reason;
                if (CheckAxisInitializeStepStillValid(step, out reason))
                {
                    if (!string.Equals(progress.Status, AxisInitializeStepStatus.Complete, StringComparison.OrdinalIgnoreCase))
                    {
                        progress.Status = AxisInitializeStepStatus.Complete;
                        progress.Message = "";
                        SetAxisInitializeStepProgress(progress);
                    }
                }
                else if (string.Equals(progress.Status, AxisInitializeStepStatus.Complete, StringComparison.OrdinalIgnoreCase))
                {
                    progress.Status = AxisInitializeStepStatus.ReinitializeRequired;
                    progress.Message = reason;
                    SetAxisInitializeStepProgress(progress);
                }
            }

            return progress;
        }

        private static bool ShouldResolveInitializedStepStatus(string status, bool machineInitialized)
        {
            if (string.Equals(status, AxisInitializeStepStatus.Complete, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(status, AxisInitializeStepStatus.ReinitializeRequired, StringComparison.OrdinalIgnoreCase))
                return true;

            return machineInitialized &&
                (string.IsNullOrWhiteSpace(status) ||
                 string.Equals(status, AxisInitializeStepStatus.Waiting, StringComparison.OrdinalIgnoreCase));
        }

        private void SetAxisInitializeStepProgress(AxisInitializeStepProgress progress)
        {
            SetAxisInitializeStepProgress(progress, true);
        }

        private void SetAxisInitializeStepProgress(AxisInitializeStepProgress progress, bool persist)
        {
            try
            {
                if (progress == null)
                    return;

                lock (_axisInitializeStepStateLock)
                {
                    _axisInitializeStepStates[GetAxisInitializeStepStateKey(progress.StepNo, progress.GroupName)] =
                        new AxisInitializeStepProgress
                        {
                            StepNo = progress.StepNo,
                            GroupName = progress.GroupName ?? "",
                            Status = progress.Status ?? "",
                            Message = progress.Message ?? ""
                        };
                }

                if (persist)
                    SaveMachineRuntimeState("InitializeStepProgress:" + progress.StepNo + ":" + (progress.GroupName ?? ""));
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void SaveAxisInitializeStepRuntimeStateIfNeeded(AxisInitializeStepProgress progress)
        {
            try
            {
                if (progress == null)
                    return;

                if (_restoringAxisInitializeStepState)
                    return;

                if (!string.Equals(progress.Status, AxisInitializeStepStatus.Complete, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(progress.Status, AxisInitializeStepStatus.Failed, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(progress.Status, AxisInitializeStepStatus.ReinitializeRequired, StringComparison.OrdinalIgnoreCase))
                    return;

                AppSettings settings = AppSettingsStore.Current ?? AppSettingsStore.Load();
                if (settings == null || !settings.DeveloperMode)
                    return;

                SaveMachineRuntimeState("InitializeStepStatus:" + progress.StepNo + ":" + progress.GroupName);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeStateSave",
                    "Initialize step status save failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private List<MachineInitializeStepRuntimeState> CaptureAxisInitializeStepRuntimeStates()
        {
            try
            {
                lock (_axisInitializeStepStateLock)
                {
                    return _axisInitializeStepStates.Values
                        .Where(x => x != null)
                        .Select(x => new MachineInitializeStepRuntimeState
                        {
                            StepNo = x.StepNo,
                            GroupName = x.GroupName ?? "",
                            Status = x.Status ?? "",
                            Message = x.Message ?? ""
                        })
                        .ToList();
                }
            }
            catch
            {
                return new List<MachineInitializeStepRuntimeState>();
            }
            finally
            {
            }
        }

        private void RestoreAxisInitializeStepRuntimeState(MachineRuntimeState state)
        {
            try
            {
                ClearAxisInitializeStepStates();
                if (state == null || state.InitializeSteps == null)
                    return;

                _restoringAxisInitializeStepState = true;
                foreach (MachineInitializeStepRuntimeState saved in state.InitializeSteps)
                {
                    if (saved == null)
                        continue;

                    SetAxisInitializeStepProgress(new AxisInitializeStepProgress
                    {
                        StepNo = saved.StepNo,
                        GroupName = saved.GroupName ?? "",
                        Status = saved.Status ?? "",
                        Message = saved.Message ?? ""
                    }, false);
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Initialize step runtime state restored. count=" + state.InitializeSteps.Count + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Initialize step runtime state restore failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void EnsureAxisInitializeStepStatesLoadedFromRuntimeState()
        {
            try
            {
                lock (_axisInitializeStepStateLock)
                {
                    if (_axisInitializeStepStates.Count > 0)
                        return;
                }

                AppSettings settings = AppSettingsStore.Current ?? AppSettingsStore.Load();
                if (settings == null || !settings.DeveloperMode)
                    return;

                MachineRuntimeState state = MachineRuntimeStateStore.Load();
                if (state == null || !state.DeveloperMode)
                    return;

                if (state.InitializeSteps != null && state.InitializeSteps.Count > 0)
                    RestoreAxisInitializeStepRuntimeState(state);
                else
                    RestoreAxisInitializeStepRuntimeStateFromSavedAxes(state);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Initialize step fallback restore failed: " + ex.Message + " - Failed");
            }
            finally
            {
                _restoringAxisInitializeStepState = false;
            }
        }

        private void RestoreAxisInitializeStepRuntimeStateFromSavedAxes(MachineRuntimeState state)
        {
            try
            {
                ClearAxisInitializeStepStates();
                if (state == null || state.Axes == null || state.Axes.Count == 0)
                    return;

                AxisInitializePlan plan = AxisInitializePlanStore.LoadOrCreateDefault(EnumerateAxes());
                if (plan == null || plan.Steps == null)
                    return;

                var savedAxes = state.Axes
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name))
                    .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

                _restoringAxisInitializeStepState = true;
                foreach (AxisInitializeStep step in plan.Steps.Where(x => x != null && x.Enabled))
                {
                    if (step.AxisNames == null || step.AxisNames.Count == 0)
                    {
                        if (state.IsMachineInitialized)
                            SetAxisInitializeStepProgress(
                                AxisInitializeStepProgress.Create(step, AxisInitializeStepStatus.Complete, ""),
                                false);
                        continue;
                    }

                    bool ready = true;
                    foreach (string axisName in step.AxisNames)
                    {
                        MachineAxisRuntimeState axisState;
                        if (string.IsNullOrWhiteSpace(axisName) ||
                            !savedAxes.TryGetValue(axisName, out axisState) ||
                            !axisState.IsServoOn ||
                            axisState.IsAlarm ||
                            !axisState.IsHomeDone)
                        {
                            ready = false;
                            break;
                        }
                    }

                    if (ready)
                    {
                        SetAxisInitializeStepProgress(
                            AxisInitializeStepProgress.Create(step, AxisInitializeStepStatus.Complete, ""),
                            false);
                    }
                }

                _restoringAxisInitializeStepState = false;
                SaveMachineRuntimeState("InitializeStepStatusRebuiltFromSavedAxes");

                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Initialize step runtime state rebuilt from saved axes. axisCount=" + savedAxes.Count + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MachineRuntimeRestore",
                    "Initialize step runtime state rebuild failed: " + ex.Message + " - Failed");
            }
            finally
            {
                _restoringAxisInitializeStepState = false;
            }
        }

        private void ClearAxisInitializeStepStates()
        {
            try
            {
                lock (_axisInitializeStepStateLock)
                {
                    _axisInitializeStepStates.Clear();
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private bool CheckAxisInitializeStepStillValid(AxisInitializeStep step, out string reason)
        {
            reason = "";
            try
            {
                if (step == null)
                {
                    reason = "초기화 Step 정보가 없습니다. 다시 초기화가 필요합니다.";
                    return false;
                }

                var axes = ResolveAxesByNames(step.AxisNames);
                foreach (BaseAxis axis in axes)
                {
                    if (axis == null)
                        continue;

                    try { axis.UpdateStatus(); } catch { }

                    if (axis.IsAlarm)
                    {
                        reason = axis.Name + " Alarm 발생. code=0x" + axis.AlarmCode.ToString("X4") +
                            ". 알람 해제 후 다시 초기화가 필요합니다.";
                        return false;
                    }

                    if (!axis.IsServoOn)
                    {
                        reason = axis.Name + " Servo OFF 상태입니다. Servo ON 후 다시 초기화가 필요합니다.";
                        return false;
                    }

                    if (!axis.IsHomeDone)
                    {
                        reason = axis.Name + " HOME 완료 상태가 해제되었습니다. 다시 초기화가 필요합니다.";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                reason = "초기화 완료 상태 확인 실패: " + ex.Message + ". 다시 초기화가 필요합니다.";
                QMC.Common.Log.Write("Main", "SYSTEM", "CheckAxisInitializeStepStillValid",
                    "Check initialize step status failed. step=" + (step != null ? step.StepNo : 0) +
                    ", group=" + (step != null ? step.GroupName : "") +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static string GetAxisInitializeStepStateKey(AxisInitializeStep step)
        {
            return step == null
                ? GetAxisInitializeStepStateKey(0, "")
                : GetAxisInitializeStepStateKey(step.StepNo, step.GroupName);
        }

        private static string GetAxisInitializeStepStateKey(int stepNo, string groupName)
        {
            return stepNo.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + (groupName ?? "");
        }

        private static bool IsInputSharedRailInitializeStep(AxisInitializeStep step)
        {
            if (step == null)
                return false;

            if (step.StepNo == 60 &&
                string.Equals(step.GroupName, "InputFeederLift", StringComparison.OrdinalIgnoreCase))
                return true;

            return step.StepNo == 71 || step.StepNo == 72 || step.StepNo == 73 || step.StepNo == 74;
        }

        private async Task<int> ExecuteConditionalInitializeSequenceAsync(
            IList<AxisInitializeStep> steps,
            int firstStepNo,
            int[] orderedSecondStepNos,
            Func<bool> shouldRunSecondFirst,
            string relationName,
            int[] firstPreStepNos = null)
        {
            try
            {
                if (steps == null)
                    return 0;

                AxisInitializeStep first = steps.FirstOrDefault(x => x != null && x.Enabled && x.StepNo == firstStepNo);
                var firstPreSteps = (firstPreStepNos ?? new int[0])
                    .SelectMany(stepNo => steps
                        .Where(x => x != null && x.Enabled && x.StepNo == stepNo)
                        .OrderBy(x => x.GroupName)
                        .ToList())
                    .ToList();
                var secondSteps = (orderedSecondStepNos ?? new int[0])
                    .SelectMany(stepNo => steps
                        .Where(x => x != null && x.Enabled && x.StepNo == stepNo)
                        .OrderBy(x => x.GroupName)
                        .ToList())
                    .ToList();

                if (first == null && firstPreSteps.Count == 0 && secondSteps.Count == 0)
                    return 0;

                bool secondFirst = shouldRunSecondFirst != null && shouldRunSecondFirst();
                string secondOrder = string.Join("->", (orderedSecondStepNos ?? new int[0]).Select(x => x.ToString()).ToArray());
                string firstOrder = string.Join("->", (firstPreStepNos ?? new int[0]).Concat(new[] { firstStepNo }).Select(x => x.ToString()).ToArray());
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeConditionalSequence",
                    "Conditional initialize sequence order selected. relation=" + relationName +
                    ", order=" + (secondFirst ? secondOrder + "->" + firstOrder : firstOrder + "->" + secondOrder) +
                    " - Start");

                if (secondFirst)
                {
                    int secondResult = await ExecuteInitializeOrderedStepsAsync(secondSteps).ConfigureAwait(false);
                    if (secondResult != 0)
                        return secondResult;

                    int preResult = await ExecuteInitializeOrderedStepsAsync(firstPreSteps).ConfigureAwait(false);
                    if (preResult != 0)
                        return preResult;

                    int firstResult = first != null ? await ExecuteInitializeSingleStepAsync(first).ConfigureAwait(false) : 0;
                    if (firstResult != 0)
                        return firstResult;
                }
                else
                {
                    int preResult = await ExecuteInitializeOrderedStepsAsync(firstPreSteps).ConfigureAwait(false);
                    if (preResult != 0)
                        return preResult;

                    int firstResult = first != null ? await ExecuteInitializeSingleStepAsync(first).ConfigureAwait(false) : 0;
                    if (firstResult != 0)
                        return firstResult;

                    int secondResult = await ExecuteInitializeOrderedStepsAsync(secondSteps).ConfigureAwait(false);
                    if (secondResult != 0)
                        return secondResult;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeConditionalSequence",
                    "Conditional initialize sequence completed. relation=" + relationName + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "Conditional initialize sequence failed: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeConditionalSequence",
                    "Conditional initialize sequence failed. relation=" + relationName +
                    ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-COND-SEQ-EX", "MachineController", LastActionFailureMessage);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteInitializeOrderedStepsAsync(IList<AxisInitializeStep> steps)
        {
            if (steps == null)
                return 0;

            foreach (AxisInitializeStep step in steps
                .Where(x => x != null && x.Enabled)
                .OrderBy(x => x.StepNo)
                .ThenBy(x => x.GroupName))
            {
                int result = await ExecuteInitializeSingleStepAsync(step).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            return 0;
        }

        private async Task<int> ExecuteConditionalInitializePairAsync(
            IList<AxisInitializeStep> steps,
            int firstStepNo,
            int secondStepNo,
            Func<bool> shouldRunSecondFirst,
            string relationName)
        {
            try
            {
                if (steps == null)
                    return 0;

                AxisInitializeStep first = steps.FirstOrDefault(x => x != null && x.Enabled && x.StepNo == firstStepNo);
                AxisInitializeStep second = steps.FirstOrDefault(x => x != null && x.Enabled && x.StepNo == secondStepNo);

                if (first == null && second == null)
                    return 0;

                bool secondFirst = shouldRunSecondFirst != null && shouldRunSecondFirst();
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeConditionalPair",
                    "Conditional initialize pair order selected. relation=" + relationName +
                    ", order=" + (secondFirst ? secondStepNo + "->" + firstStepNo : firstStepNo + "->" + secondStepNo) +
                    " - Start");

                if (secondFirst)
                {
                    int secondResult = second != null ? await ExecuteInitializeSingleStepAsync(second).ConfigureAwait(false) : 0;
                    if (secondResult != 0)
                        return secondResult;

                    int firstResult = first != null ? await ExecuteInitializeSingleStepAsync(first).ConfigureAwait(false) : 0;
                    if (firstResult != 0)
                        return firstResult;
                }
                else
                {
                    int firstResult = first != null ? await ExecuteInitializeSingleStepAsync(first).ConfigureAwait(false) : 0;
                    if (firstResult != 0)
                        return firstResult;

                    int secondResult = second != null ? await ExecuteInitializeSingleStepAsync(second).ConfigureAwait(false) : 0;
                    if (secondResult != 0)
                        return secondResult;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeConditionalPair",
                    "Conditional initialize pair completed. relation=" + relationName + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "조건부 초기화 순서 실행 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeConditionalPair",
                    "Conditional initialize pair failed. relation=" + relationName +
                    ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-COND-PAIR-EX", "MachineController", LastActionFailureMessage);
                return -1;
            }
            finally
            {
            }
        }

        private bool ShouldRunSharedRailBeforeInputFeederHome()
        {
            try
            {
                if (IsSharedRailAtInputStageSideForInitialize())
                    return true;

                InputFeederUnit feeder = _machine != null ? _machine.InputFeederUnit : null;
                if (feeder != null && !feeder.IsWaferFeederYInAvoidPosition())
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool ShouldRunSharedRailBeforeOutputFeederHome()
        {
            try
            {
                if (IsSharedRailAtOutputStageSideForInitialize())
                    return true;

                OutputFeederUnit feeder = _machine != null ? _machine.OutputFeederUnit : null;
                if (feeder != null && !feeder.IsBinFeederYInAvoidPosition())
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool IsSharedRailAtInputStageSideForInitialize()
        {
            // TODO: InputStage 쪽 위험 영역 좌표가 확정되면 CameraX/FrontPickerX/RearPickerX 위치로 판정하세요.
            return false;
        }

        private bool IsSharedRailAtOutputStageSideForInitialize()
        {
            // TODO: OutputStage 쪽 위험 영역 좌표가 확정되면 FrontPickerX/RearPickerX/OutputVisionX 위치로 판정하세요.
            return false;
        }

        private async Task<int> PrepareInitializeStepAsync(AxisInitializeStep step)
        {
            return 0;
            try
            {
                string groupName = step != null ? step.GroupName : "";
                if (string.Equals(groupName, "InputFeeder", StringComparison.OrdinalIgnoreCase))
                    return await PrepareInputFeederHomeAsync().ConfigureAwait(false);
                if (string.Equals(groupName, "OutputFeeder", StringComparison.OrdinalIgnoreCase))
                    return await PrepareOutputFeederHomeAsync().ConfigureAwait(false);

                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "초기화 Step 준비 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "PrepareInitializeStep",
                    "Initialize step prepare failed. step=" + (step != null ? step.StepNo : 0) +
                    ", group=" + (step != null ? step.GroupName : "") +
                    ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-PREP-EX", "MachineController", LastActionFailureMessage);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> CompleteInitializeStepAsync(AxisInitializeStep step)
        {
            try
            {
                await Task.Yield();
                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "초기화 Step 후처리 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "CompleteInitializeStep",
                    "Initialize step complete failed. step=" + (step != null ? step.StepNo : 0) +
                    ", group=" + (step != null ? step.GroupName : "") +
                    ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-COMPLETE-EX", "MachineController", LastActionFailureMessage);
                return -1;
            }
            finally
            {
            }
        }



        //CanHomeWaferLifterZ 옮겨서 InputCassetteInterlockRules로 이동
        //이 함수 확인 후 지워야함.
        private Task<int> PrepareInputLifterHomeAsync()
        {
            Log("[INIT] Prepare InputLifter home: check input cassette wafer protrusion.");

            var inputCassette = _machine.InputCassetteUnit;
            if (inputCassette == null)
                return Task.FromResult(0);

            return Task.FromResult(0);
        }

        //CanMoveBinLifterZ 옮겨서 OutputCassetteInterlockRules로 이동
        //이 함수 확인 후 지워야함.
        private Task<int> PrepareOutputLifterHomeAsync()
        {
            Log("[INIT] Prepare OutputLifter home: check output cassette bin protrusion.");

            var outputCassette = _machine.OutputCassetteUnit;
            if (outputCassette == null)
                return Task.FromResult(0);

            return Task.FromResult(0);
        }

        private Task<int> PrepareInputExpandingHomeAsync()
        {
            Log("[INIT] Prepare InputExpanding home: check wafer stage touch sensor.");

            var visionUnit = _machine.VisionUnit;
            if (visionUnit == null)
                return Task.FromResult(0);

            // Todo : Wafer 데이터로 확인
            //if (visionUnit.IsVisionWaferStageTouchSensor(true))
            //{
            //    return Task.FromResult(FailInitializePreparation(
            //        "InputExpandingZ HOME 불가: Wafer Stage Touch Sensor가 감지되었습니다."));
            //}

            return Task.FromResult(0);
        }

        private async Task<int> PrepareInputStageHomeAsync()
        {
            Log("[INIT] Prepare InputStageY home: NeedleZ / Front,RearPickerZ0~Z3 / InputFeederY Avoid check.");

            var stage = _machine.InputStageUnit;
            if (stage != null && !stage.IsNeedleZInSafePosition())
            {
                return FailInitializePreparation(
                    "InputStageY HOME 불가: NeedleZ가 Avoid 위치에 있지 않습니다.");
            }

            var feeder = _machine.InputFeederUnit;
            if (feeder != null && !feeder.IsWaferFeederInAvoidPosition())
            {
                return FailInitializePreparation(
                    "InputStageY HOME 불가: InputFeederY가 Avoid 위치에 있지 않습니다.");
            }

            int result = await CheckFrontPickerZAxesAvoidAsync().ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await CheckRearPickerZAxesAvoidAsync().ConfigureAwait(false);
            if (result != 0)
                return result;

            return 0;
        }

        private async Task<int> PrepareInputStageTHomeAsync(BaseAxis axis)
        {
            Log("[INIT] Prepare InputStageT home: NeedleZ / Front,RearPickerZ0~Z3 Avoid check.");

            var stage = _machine.InputStageUnit;
            if (stage != null && !stage.IsNeedleZInSafePosition())
            {
                return FailInitializePreparation(
                    "InputStageT HOME 불가: NeedleZ가 Avoid 위치에 있지 않습니다.");
            }

            int result = await CheckFrontPickerZAxesAvoidAsync().ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await CheckRearPickerZAxesAvoidAsync().ConfigureAwait(false);
            if (result != 0)
                return result;

            return 0;
        }

        private async Task<int> MoveInputNeedleZSafeToAvoidAsync()
        {
            var stage = _machine.InputStageUnit;
            if (stage == null ||
                stage.NeedleZ == null ||
                stage.Recipe == null ||
                stage.Recipe.NeedleZ == null)
                return 0;

            return await MoveAxisTeachingAsync(
                stage.NeedleZ,
                stage.Recipe.NeedleZ.AvoidPosition,
                "InputNeedleZ.Avoid").ConfigureAwait(false);
        }

        private async Task<int> MoveInputFeederYToAvoidAsync()
        {
            var feeder = _machine.InputFeederUnit;
            if (feeder == null ||
                feeder.FeederY == null ||
                feeder.Recipe == null)
                return 0;

            return await MoveAxisTeachingAsync(
                feeder.FeederY,
                feeder.Recipe.AvoidPosition,
                "InputFeederY.Avoid").ConfigureAwait(false);
        }

        private Task<int> PrepareInputFeederHomeAsync()
        {
            Log("[INIT] Prepare InputFeeder home: InputLifterZ Avoid / unclamp / overload / ring check.");

            //var cassette = _machine.InputCassetteUnit;
            //if (cassette != null && !cassette.IsWaferLifterZInAvoidPosition())
            //{
            //    return FailInitializePreparation(
            //        "InputFeeder HOME 불가: InputLifterZ가 Avoid 위치에 있지 않습니다.");
            //}

            string axisReason;
            var stage = _machine.InputStageUnit;
            if (stage != null && !IsAxisNotHomedOrAtHomePosition(stage.CameraX, "InputVisionX", out axisReason))
            {
                return Task.FromResult(FailInitializePreparation(
                    "InputFeeder HOME 불가: InputVisionX가 Home 전 상태가 아니고 Home 위치에도 없습니다. " + axisReason));
            }

            var front = _machine.PickerFrontUnit;
            if (front != null && !IsAxisNotHomedOrAtHomePosition(front.PickerX, "FrontPickerX", out axisReason))
            {
                return Task.FromResult(FailInitializePreparation(
                    "InputFeeder HOME 불가: FrontPickerX가 Home 전 상태가 아니고 Home 위치에도 없습니다. " + axisReason));
            }

            var rear = _machine.PickerRearUnit;
            if (rear != null && !IsAxisNotHomedOrAtHomePosition(rear.PickerX, "RearPickerX", out axisReason))
            {
                return Task.FromResult(FailInitializePreparation(
                    "InputFeeder HOME 불가: RearPickerX가 Home 전 상태가 아니고 Home 위치에도 없습니다. " + axisReason));
            }

            var feeder = _machine.InputFeederUnit;
            if (feeder == null)
                return Task.FromResult(0);

            if (feeder.IsWaferFeederOverload())
            {
                return Task.FromResult(FailInitializePreparation(
                    "InputFeeder HOME 불가: Overload 센서가 감지되었습니다."));
            }

            if (!feeder.IsWaferFeederSimulationOrDryRun())
            {
                if (!feeder.IsWaferFeederUnclamp())
                {
                    return Task.FromResult(FailInitializePreparation("InputFeeder unclamp failed."));
                }

                if (feeder.IsWaferFeederRingCheck())
                {
                    return Task.FromResult(FailInitializePreparation("InputFeeder Ring Check."));
                }
            }

            return Task.FromResult(0);
        }

        private async Task<int> MoveInputFeederLiftDownAfterHomeAsync()
        {
            try
            {
                Log("[INIT] Complete InputFeeder home: feeder lift down.");

                var feeder = _machine != null ? _machine.InputFeederUnit : null;
                if (feeder == null)
                    return 0;

                if (feeder.IsWaferFeederDown())
                    return 0;

                int result = await feeder.SetWaferFeederUpDown(false).ConfigureAwait(false);
                if (result != 0)
                    return FailInitializePreparation("InputFeeder lift down after home failed. result=" + result);

                if (!feeder.IsWaferFeederDown())
                    return FailInitializePreparation("InputFeeder lift down after home failed: Down sensor is not active.");

                return 0;
            }
            catch (Exception ex)
            {
                return FailInitializePreparation("InputFeeder lift down after home exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureInputFeederLiftDownForSharedRailHomeAsync(string sourceName)
        {
            try
            {
                var feeder = _machine != null ? _machine.InputFeederUnit : null;
                if (feeder == null)
                    return 0;

                if (feeder.IsWaferFeederDown())
                    return 0;

                Log("[INIT] Prepare " + sourceName + " home: feeder lift down command.");
                int result = await feeder.SetWaferFeederUpDown(false).ConfigureAwait(false);
                if (result != 0)
                {
                    return FailInitializePreparation(
                        sourceName + " HOME 불가: InputFeeder 실린더 Down 이동 실패. result=" + result);
                }

                if (!feeder.IsWaferFeederDown())
                {
                    return FailInitializePreparation(
                        sourceName + " HOME 불가: InputFeeder 실린더가 Down 위치에 있지 않습니다.");
                }

                return 0;
            }
            catch (Exception ex)
            {
                return FailInitializePreparation(
                    sourceName + " HOME 불가: InputFeeder 실린더 Down 확인 중 예외 발생. " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> PrepareOutputFeederHomeAsync()
        {
            Log("[INIT] Prepare OutputFeeder home: OutputLifterZ / OutputVisionX Avoid check, feeder unclamp/up.");

            var cassette = _machine.OutputCassetteUnit;
            if (cassette != null && !cassette.IsBinLifterZInAvoidPosition())
            {
                return FailInitializePreparation(
                    "OutputFeeder HOME 불가: OutputLifterZ가 Avoid 위치에 있지 않습니다.");
            }

            var outputStage = _machine.OutputStageUnit;
            if (outputStage != null && !outputStage.IsVisionXInAvoidPosition())
            {
                return FailInitializePreparation(
                    "OutputFeeder HOME 불가: OutputVisionX가 Avoid 위치에 있지 않습니다.");
            }

            if (outputStage != null && outputStage.GoodStage != null && !outputStage.GoodStage.IsAtAvoidPosition())
            {
                return FailInitializePreparation(
                    "OutputFeeder HOME 불가: GoodBinZ(GoodStageZ)가 Avoid 위치에 있지 않습니다.");
            }

            if (outputStage != null && !outputStage.IsBinGuideDown(BinSide.Good))
            {
                Log("[INIT] Prepare OutputFeeder home: Good Bin Guide Down.");

                int guideDownResult = await outputStage.EnsureBinGuideDownAsync(BinSide.Good, 3000, CancellationToken.None).ConfigureAwait(false);
                if (guideDownResult != 0)
                {
                    return FailInitializePreparation(
                        "OutputFeeder HOME 준비 실패: Good Bin Guide Down 명령 실패. result=" + guideDownResult + ", " +
                        outputStage.DescribeOutputStageInterlockState(BinSide.Good));
                }

                if (!outputStage.IsBinGuideDown(BinSide.Good))
                {
                    return FailInitializePreparation(
                        "OutputFeeder HOME 준비 실패: Good Bin Guide Down 최종 확인 실패. " +
                        outputStage.DescribeOutputStageInterlockState(BinSide.Good));
                }
            }

            var feeder = _machine.OutputFeederUnit;
            if (feeder == null)
                return 0;

            if (feeder.IsFeederOverload())
            {
                return FailInitializePreparation(
                    "OutputFeeder HOME 불가: Overload 센서가 감지되었습니다.");
            }

            if (!feeder.IsFeederUnclamped())
            {
                return FailInitializePreparation("OutputFeeder unclamp failed.");
            }

            if (!feeder.IsFeederUp())
            {
                return FailInitializePreparation("OutputFeeder lift up failed.");
            }

            if (!feeder.IsOutputFeederSimulationOrDryRun() && feeder.IsBinFeederRingCheck())
            {
                return FailInitializePreparation("OutputFeeder Ring Check.");
            }

            return 0;
        }

        private async Task<int> MoveSharedRailAxesToAvoidAsync()
        {
            int result = await MoveInputSafeXAxesToAvoidAsync().ConfigureAwait(false);
            if (result != 0)
                return result;

            return await MoveOutputVisionXToAvoidAsync().ConfigureAwait(false);
        }

        private async Task<int> MoveInputSafeXAxesToAvoidAsync()
        {
            int pickerResult = await MoveFrontRearPickerXToAvoidAsync().ConfigureAwait(false);
            if (pickerResult != 0)
                return pickerResult;

            if (_machine.InputStageUnit != null &&
                _machine.InputStageUnit.CameraX != null &&
                _machine.InputStageUnit.Recipe != null &&
                _machine.InputStageUnit.Recipe.VisionX != null)
            {
                int result = await MoveAxisTeachingAsync(
                    _machine.InputStageUnit.CameraX,
                    _machine.InputStageUnit.Recipe.VisionX.AvoidPosition,
                    "InputVisionX.Avoid").ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            return 0;
        }

        private async Task<int> MoveFrontRearPickerXToAvoidAsync()
        {
            BaseAxis frontAxis = _machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerX : null;
            BaseAxis rearAxis = _machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerX : null;
            bool canMoveFront = frontAxis != null &&
                frontAxis.IsHomeDone &&
                _machine.PickerFrontUnit.Recipe != null &&
                _machine.PickerFrontUnit.Recipe.PickerX != null;
            bool canMoveRear = rearAxis != null &&
                rearAxis.IsHomeDone &&
                _machine.PickerRearUnit.Recipe != null &&
                _machine.PickerRearUnit.Recipe.PickerX != null;

            if (canMoveFront && canMoveRear && SharedRailX != null)
            {
                double frontTarget = _machine.PickerFrontUnit.Recipe.PickerX.AvoidPosition;
                double rearTarget = _machine.PickerRearUnit.Recipe.PickerX.AvoidPosition;
                double velocity = Math.Min(ResolveAxisDefaultVelocity(frontAxis), ResolveAxisDefaultVelocity(rearAxis));
                if (velocity <= 0.0)
                    velocity = ResolveAxisDefaultVelocity(frontAxis);

                int result = await SharedRailX.MoveFrontAndRearPickerAsync(
                    frontTarget,
                    rearTarget,
                    velocity).ConfigureAwait(false);
                if (result != 0 || frontAxis.IsAlarm || rearAxis.IsAlarm)
                    return FailInitializePreparation("FrontPickerX/RearPickerX Avoid 이동 실패. result=" + result);

                return 0;
            }

            if (_machine.PickerFrontUnit != null &&
                _machine.PickerFrontUnit.PickerX != null &&
                _machine.PickerFrontUnit.Recipe != null &&
                _machine.PickerFrontUnit.Recipe.PickerX != null)
            {
                int result = await MoveAxisTeachingAsync(
                    _machine.PickerFrontUnit.PickerX,
                    _machine.PickerFrontUnit.Recipe.PickerX.AvoidPosition,
                    "FrontPickerX.Avoid").ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            if (_machine.PickerRearUnit != null &&
                _machine.PickerRearUnit.PickerX != null &&
                _machine.PickerRearUnit.Recipe != null &&
                _machine.PickerRearUnit.Recipe.PickerX != null)
            {
                int result = await MoveAxisTeachingAsync(
                    _machine.PickerRearUnit.PickerX,
                    _machine.PickerRearUnit.Recipe.PickerX.AvoidPosition,
                    "RearPickerX.Avoid").ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            return 0;
        }

        private async Task<int> MoveOutputVisionXToAvoidAsync()
        {
            if (_machine.OutputStageUnit == null ||
                _machine.OutputStageUnit.OutputCameraX == null ||
                _machine.OutputStageUnit.Recipe == null ||
                _machine.OutputStageUnit.Recipe.VisionX == null)
                return 0;

            return await MoveAxisTeachingAsync(
                _machine.OutputStageUnit.OutputCameraX,
                _machine.OutputStageUnit.Recipe.VisionX.AvoidPosition,
                "OutputVisionX.Avoid").ConfigureAwait(false);
        }

        private async Task<int> MoveOutputStageZToAvoidAsync()
        {
            var stage = _machine.OutputStageUnit;
            if (stage == null)
                return 0;

            if (stage.GoodStage != null)
            {
                int result = await stage.GoodStage.MoveToAvoidPositionAsync().ConfigureAwait(false);
                if (result != 0)
                    return FailInitializePreparation("GoodStage Z avoid move failed.");
            }

            return 0;
        }

        private async Task<int> MoveOutputStageToAvoidForFeederHomeAsync()
        {
            int result = await MoveOutputStageZToAvoidAsync().ConfigureAwait(false);
            if (result != 0)
                return result;

            var stage = _machine.OutputStageUnit;
            if (stage == null)
                return 0;

            if (stage.GoodStage != null &&
                stage.GoodStage.StageY != null &&
                stage.Recipe != null &&
                stage.Recipe.GoodStageY != null)
            {
                result = await MoveAxisTeachingAsync(
                    stage.GoodStage.StageY,
                    stage.Recipe.GoodStageY.AvoidPosition,
                    "OutputGoodStageY.Avoid").ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            if (stage.NgStage != null &&
                stage.NgStage.StageY != null &&
                stage.Recipe != null &&
                stage.Recipe.NGStageY != null)
            {
                result = await MoveAxisTeachingAsync(
                    stage.NgStage.StageY,
                    stage.Recipe.NGStageY.AvoidPosition,
                    "OutputNGStageY.Avoid").ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            return await MoveOutputVisionXToAvoidAsync().ConfigureAwait(false);
        }

        private async Task<int> MoveAxisTeachingAsync(BaseAxis axis, double targetPosition, string targetName)
        {
            try
            {
                if (axis == null)
                    return 0;

                if (!axis.IsHomeDone)
                    return -1;

                using (MotionGuardRuntime.BeginAxisTeachingMove(axis, targetPosition, targetName))
                {
                    int result = await SharedRailXMotionRuntime.MoveAxisAsync(
                        axis,
                        targetPosition,
                        ResolveAxisDefaultVelocity(axis)).ConfigureAwait(false);
                    if (result != 0 || axis.IsAlarm)
                    {
                        string message = BuildAxisMotionFailureMessage(axis, "Avoid 이동 실패", result);
                        return FailInitializePreparation(message);
                    }

                    AxisMoveWaitResult waitResult = await WaitAxisMoveDoneInPositionAsync(axis, targetPosition).ConfigureAwait(false);
                    if (!waitResult.Success)
                    {
                        string message = targetName + " move wait/in-position failed. " +
                            AxisMoveWaiter.FormatResult(waitResult, axis.Name);
                        return FailInitializePreparation(message);
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                return FailInitializePreparation("Avoid move exception. axis=" +
                    (axis != null ? axis.Name : "-") + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int FailInitializePreparation(string message)
        {
            LastActionFailureMessage = message;
            QMC.Common.Log.Write("Main", "SYSTEM", "InitializePreparation", message + " - Failed");
            AlarmManager.Raise(AlarmSeverity.Error, "INIT-PREP", "MachineController", message);
            return -1;
        }

        private async Task<int> StopInitializeInterlockGroupAsync(AxisInitializeStep step)
        {
            try
            {
                if (step == null || string.IsNullOrWhiteSpace(step.InterlockGroup))
                    return 0;

                var axes = ResolveAxesByGroup(step.InterlockGroup)
                    .Select(x => x.Name)
                    .ToList();

                if (axes.Count == 0)
                    axes = _axisInterferenceMap.ResolveInterferenceAxes(step.InterlockGroup).ToList();

                if (axes.Count == 0)
                    return 0;

                QMC.Common.Log.Write("Main", "SYSTEM", "StopInitializeInterlockGroup",
                    "Initialize interlock group stop requested. step=" + step.StepNo +
                    ", interlockGroup=" + step.InterlockGroup +
                    ", axes=" + string.Join(",", axes.ToArray()) + " - Start");

                return await StopAxesAsync(axes, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "StopInitializeInterlockGroup",
                    "Initialize interlock group stop failed. step=" + (step != null ? step.StepNo : 0) +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        private bool HasEnabledInitializeActions(AxisInitializeStep step)
        {
            try
            {
                return HasEnabledInitializeActions(step != null ? step.PreActions : null) ||
                       HasEnabledInitializeActions(step != null ? step.PostActions : null);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool HasEnabledInitializeActions(IList<AxisInitializeAction> actions)
        {
            try
            {
                if (actions == null)
                    return false;

                return actions.Any(x => x != null && x.Enabled);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteInitializeActionsAsync(
            AxisInitializeStep step,
            IList<AxisInitializeAction> actions,
            string phase)
        {
            try
            {
                if (actions == null || actions.Count == 0)
                    return 0;

                foreach (var action in actions)
                {
                    if (action == null || !action.Enabled)
                        continue;

                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAction",
                        "Initialize action start. step=" + (step != null ? step.StepNo : 0) +
                        ", group=" + (step != null ? step.GroupName : "") +
                        ", phase=" + phase +
                        ", target=" + action.TargetType + ":" + action.Name +
                        ", command=" + action.Command + " - Start");

                    int result = await ExecuteInitializeActionAsync(step, action, phase).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAction",
                        "Initialize action completed. step=" + (step != null ? step.StepNo : 0) +
                        ", group=" + (step != null ? step.GroupName : "") +
                        ", phase=" + phase +
                        ", target=" + action.TargetType + ":" + action.Name +
                        ", command=" + action.Command + " - Ok");
                }

                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "초기화 Action 실행 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAction",
                    "Initialize action failed. phase=" + phase + ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-ACTION-EX", "MachineController", LastActionFailureMessage);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteInitializeActionAsync(
            AxisInitializeStep step,
            AxisInitializeAction action,
            string phase)
        {
            try
            {
                string targetType = action.TargetType ?? "";
                string command = action.Command ?? "";

                if (string.Equals(targetType, AxisInitializeInterlockTarget.Cylinder, StringComparison.OrdinalIgnoreCase))
                    return await ExecuteInitializeCylinderActionAsync(action).ConfigureAwait(false);

                if (string.Equals(targetType, AxisInitializeInterlockTarget.Axis, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(command, AxisInitializeActionCommand.AxisTeachingMove, StringComparison.OrdinalIgnoreCase))
                    return await ExecuteInitializeAxisTeachingActionAsync(action).ConfigureAwait(false);

                if (string.Equals(command, AxisInitializeActionCommand.CustomHook, StringComparison.OrdinalIgnoreCase))
                    return await ExecuteCustomInitializeActionAsync(step, action, phase).ConfigureAwait(false);

                return FailInitializePreparation("지원하지 않는 초기화 Action입니다. target=" +
                    targetType + ":" + action.Name + ", command=" + command);
            }
            catch (Exception ex)
            {
                return FailInitializePreparation("초기화 Action 예외. target=" +
                    (action != null ? action.TargetType + ":" + action.Name : "-") +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteInitializeAxisTeachingActionAsync(AxisInitializeAction action)
        {
            try
            {
                if (action == null)
                    return FailInitializePreparation("축 티칭 이동 Action 정보가 없습니다.");

                BaseAxis axis = FindInitializeActionAxis(action.Name);
                if (axis == null)
                    return FailInitializePreparation("초기화 축 티칭 이동 대상을 찾을 수 없습니다. axis=" + action.Name);

                double targetPosition;
                string targetName;
                if (!TryResolveInitializeAxisTeachingPosition(axis, action, out targetPosition, out targetName))
                {
                    return FailInitializePreparation("초기화 축 티칭 위치를 찾을 수 없습니다. axis=" +
                        action.Name + ", position=" + action.PositionName);
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisTeachingAction",
                    "Initialize axis teaching action start. axis=" + axis.Name +
                    ", requested=" + action.Name +
                    ", position=" + targetName +
                    ", target=" + targetPosition.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                    " - Start");

                int result = await MoveAxisTeachingAsync(axis, targetPosition, targetName).ConfigureAwait(false);
                if (result != 0)
                    return result;

                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisTeachingAction",
                    "Initialize axis teaching action completed. axis=" + axis.Name +
                    ", position=" + targetName + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return FailInitializePreparation("축 티칭 이동 Action 예외. axis=" +
                    (action != null ? action.Name : "-") + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteInitializeCylinderActionAsync(AxisInitializeAction action)
        {
            try
            {
                BaseCylinder cylinder = FindCylinderByName(action.Name);
                if (cylinder == null)
                    return FailInitializePreparation("초기화 실린더를 찾을 수 없습니다. cylinder=" + action.Name);

                string command = action.Command ?? "";
                if (string.Equals(command, AxisInitializeActionCommand.CylinderFwd, StringComparison.OrdinalIgnoreCase))
                {
                    bool ok;
                    using (MotionGuardRuntime.BeginCylinderInitializeMove(cylinder, true, command))
                    {
                        ok = await cylinder.MoveFwdAsync().ConfigureAwait(false);
                    }
                    return ok ? 0 : FailInitializePreparation("실린더 전진 실패. cylinder=" + cylinder.Name);
                }

                if (string.Equals(command, AxisInitializeActionCommand.CylinderBwd, StringComparison.OrdinalIgnoreCase))
                {
                    bool ok;
                    using (MotionGuardRuntime.BeginCylinderInitializeMove(cylinder, false, command))
                    {
                        ok = await cylinder.MoveBwdAsync().ConfigureAwait(false);
                    }
                    return ok ? 0 : FailInitializePreparation("실린더 후진 실패. cylinder=" + cylinder.Name);
                }

                return FailInitializePreparation("지원하지 않는 실린더 Action입니다. cylinder=" +
                    cylinder.Name + ", command=" + command);
            }
            catch (Exception ex)
            {
                return FailInitializePreparation("실린더 Action 예외. cylinder=" +
                    (action != null ? action.Name : "-") + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private BaseAxis FindInitializeActionAxis(string axisName)
        {
            try
            {
                BaseAxis axis = FindAxisByName(axisName);
                if (axis != null)
                    return axis;

                string alias = ResolveInitializeAxisAlias(axisName);
                if (!string.IsNullOrWhiteSpace(alias))
                    return FindAxisByName(alias);

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

        private static string ResolveInitializeAxisAlias(string axisName)
        {
            if (string.IsNullOrWhiteSpace(axisName))
                return null;

            switch (axisName.Trim())
            {
                case "InputStageY":
                    return "StageY";
                case "InputStageT":
                    return "StageT";
                case "InputVisionX":
                    return "CameraX";
                case "OutputGoodStageY":
                    return "GoodStage_StageY";
                case "OutputGoodStageZ":
                    return "GoodStage_StageZ";
                case "GoodStage_StageZ":
                    return "OutputGoodStageZ";
                case "OutputNGStageY":
                    return "NgStage_StageY";
                case "NgStage_StageY":
                    return "OutputNGStageY";
                case "GoodStage_StageY":
                    return "OutputGoodStageY";
                default:
                    return null;
            }
        }

        private bool TryResolveInitializeAxisTeachingPosition(
            BaseAxis axis,
            AxisInitializeAction action,
            out double targetPosition,
            out string targetName)
        {
            targetPosition = 0.0;
            targetName = "";

            try
            {
                if (axis == null || action == null)
                    return false;

                string positionName = NormalizeInitializePositionName(action.PositionName);
                if (!string.Equals(positionName, "AvoidPosition", StringComparison.OrdinalIgnoreCase))
                    return false;

                string requestedName = !string.IsNullOrWhiteSpace(action.Name) ? action.Name.Trim() : axis.Name;
                string axisName = axis.Name ?? "";

                if (IsInitializeAxisName(requestedName, axisName, "FrontPickerY"))
                {
                    if (_machine.PickerFrontUnit == null ||
                        _machine.PickerFrontUnit.Recipe == null ||
                        _machine.PickerFrontUnit.Recipe.PickerY == null)
                        return false;

                    targetPosition = _machine.PickerFrontUnit.Recipe.PickerY.AvoidPosition;
                    targetName = "FrontPickerY.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "RearPickerY"))
                {
                    if (_machine.PickerRearUnit == null ||
                        _machine.PickerRearUnit.Recipe == null ||
                        _machine.PickerRearUnit.Recipe.PickerY == null)
                        return false;

                    targetPosition = _machine.PickerRearUnit.Recipe.PickerY.AvoidPosition;
                    targetName = "RearPickerY.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "FrontPickerX"))
                {
                    if (_machine.PickerFrontUnit == null ||
                        _machine.PickerFrontUnit.Recipe == null ||
                        _machine.PickerFrontUnit.Recipe.PickerX == null)
                        return false;

                    targetPosition = _machine.PickerFrontUnit.Recipe.PickerX.AvoidPosition;
                    targetName = "FrontPickerX.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "RearPickerX"))
                {
                    if (_machine.PickerRearUnit == null ||
                        _machine.PickerRearUnit.Recipe == null ||
                        _machine.PickerRearUnit.Recipe.PickerX == null)
                        return false;

                    targetPosition = _machine.PickerRearUnit.Recipe.PickerX.AvoidPosition;
                    targetName = "RearPickerX.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "InputStageY", "StageY"))
                {
                    if (_machine.InputStageUnit == null ||
                        _machine.InputStageUnit.Recipe == null ||
                        _machine.InputStageUnit.Recipe.WaferY == null)
                        return false;

                    targetPosition = _machine.InputStageUnit.Recipe.WaferY.AvoidPosition;
                    targetName = "InputStageY.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "InputVisionX", "CameraX"))
                {
                    if (_machine.InputStageUnit == null ||
                        _machine.InputStageUnit.Recipe == null ||
                        _machine.InputStageUnit.Recipe.VisionX == null)
                        return false;

                    targetPosition = _machine.InputStageUnit.Recipe.VisionX.AvoidPosition;
                    targetName = "InputVisionX.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "OutputVisionX"))
                {
                    if (_machine.OutputStageUnit == null ||
                        _machine.OutputStageUnit.Recipe == null ||
                        _machine.OutputStageUnit.Recipe.VisionX == null)
                        return false;

                    targetPosition = _machine.OutputStageUnit.Recipe.VisionX.AvoidPosition;
                    targetName = "OutputVisionX.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "OutputGoodStageY", "GoodStage_StageY"))
                {
                    if (_machine.OutputStageUnit == null ||
                        _machine.OutputStageUnit.Recipe == null ||
                        _machine.OutputStageUnit.Recipe.GoodStageY == null)
                        return false;

                    targetPosition = _machine.OutputStageUnit.Recipe.GoodStageY.AvoidPosition;
                    targetName = "OutputGoodStageY.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "OutputGoodStageZ", "GoodStage_StageZ"))
                {
                    if (_machine.OutputStageUnit == null ||
                        _machine.OutputStageUnit.Recipe == null ||
                        _machine.OutputStageUnit.Recipe.GoodStageZ == null)
                        return false;

                    targetPosition = _machine.OutputStageUnit.Recipe.GoodStageZ.AvoidPosition;
                    targetName = "OutputGoodStageZ.Avoid";
                    return true;
                }

                if (IsInitializeAxisName(requestedName, axisName, "OutputNGStageY", "NgStage_StageY"))
                {
                    if (_machine.OutputStageUnit == null ||
                        _machine.OutputStageUnit.Recipe == null ||
                        _machine.OutputStageUnit.Recipe.NGStageY == null)
                        return false;

                    targetPosition = _machine.OutputStageUnit.Recipe.NGStageY.AvoidPosition;
                    targetName = "OutputNGStageY.Avoid";
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisTeachingAction",
                    "Initialize axis teaching position resolve failed. axis=" +
                    (axis != null ? axis.Name : "-") +
                    ", action=" + (action != null ? action.Name : "-") +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static bool IsInitializeAxisName(string requestedName, string actualName, params string[] names)
        {
            if (names == null)
                return false;

            foreach (string name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (string.Equals(requestedName, name, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (string.Equals(actualName, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string NormalizeInitializePositionName(string positionName)
        {
            if (string.IsNullOrWhiteSpace(positionName))
                return "AvoidPosition";

            string value = positionName.Trim();
            if (string.Equals(value, "Avoid", StringComparison.OrdinalIgnoreCase))
                return "AvoidPosition";

            return value;
        }

        private Task<int> ExecuteCustomInitializeActionAsync(
            AxisInitializeStep step,
            AxisInitializeAction action,
            string phase)
        {
            try
            {
                // TODO: 축/유닛별로 특수 준비 동작이 필요하면 여기에서 action.Name 또는 action.Description 기준으로 채우면 됩니다.
                // 예: if (action.Name == "OpenSomeGuide") { ...; return Task.FromResult(0); }
                return Task.FromResult(0);
            }
            catch (Exception ex)
            {
                return Task.FromResult(FailInitializePreparation("Custom 초기화 Action 예외. action=" +
                    (action != null ? action.Name : "-") + ", error=" + ex.Message));
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteInitializeStepSerialAsync(AxisInitializeStep step, IList<BaseAxis> axes)
        {
            try
            {
                foreach (var axis in axes)
                {
                    int result = await InitializeAxisCoreAsync(axis).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "Serial 초기화 Step 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeStepSerial",
                    "Axis initialize serial step failed. step=" + (step != null ? step.StepNo : 0) +
                    ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-SERIAL-EX", "MachineController", LastActionFailureMessage);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteInitializeStepParallelAsync(AxisInitializeStep step, IList<BaseAxis> axes)
        {
            try
            {
                var tasks = axes.Select(axis => InitializeAxisCoreAsync(axis)).ToArray();
                int[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
                for (int i = 0; i < results.Length; i++)
                {
                    if (results[i] != 0)
                        return results[i];
                }

                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "Parallel 초기화 Step 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeStepParallel",
                    "Axis initialize parallel step failed. step=" + (step != null ? step.StepNo : 0) +
                    ", error=" + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-PARALLEL-EX", "MachineController", LastActionFailureMessage);
                return -1;
            }
            finally
            {
            }
        }

        private List<AxisInitializeStep> ResolveEnabledInitializeSteps(AxisInitializePlan plan)
        {
            try
            {
                if (plan == null || plan.Steps == null)
                    return new List<AxisInitializeStep>();

                return plan.Steps
                    .Where(x => x != null && x.Enabled)
                    .OrderBy(x => x.StepNo)
                    .ToList();
            }
            catch
            {
                return new List<AxisInitializeStep>();
            }
            finally
            {
            }
        }

        private List<AxisInitializeStep> ResolveInitializeStepsByGroup(AxisInitializePlan plan, string groupName)
        {
            try
            {
                if (plan == null || plan.Steps == null || string.IsNullOrWhiteSpace(groupName))
                    return new List<AxisInitializeStep>();

                return plan.Steps
                    .Where(x => x != null && x.Enabled &&
                        string.Equals(x.GroupName, groupName.Trim(), StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.StepNo)
                    .ToList();
            }
            catch
            {
                return new List<AxisInitializeStep>();
            }
            finally
            {
            }
        }

        private List<BaseAxis> ResolveAxesByNames(IEnumerable<string> axisNames)
        {
            var axes = new List<BaseAxis>();
            try
            {
                if (axisNames == null)
                    return axes;

                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var axisName in axisNames)
                {
                    if (string.IsNullOrWhiteSpace(axisName) || !visited.Add(axisName.Trim()))
                        continue;

                    var axis = FindAxisByName(axisName);
                    if (axis == null)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "ResolveAxesByNames",
                            "Axis resolve failed: axis not found. axis=" + axisName + " - Failed");
                        continue;
                    }

                    axes.Add(axis);
                }

                return axes;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ResolveAxesByNames",
                    "Axis resolve failed: " + ex.Message + " - Failed");
                return axes;
            }
            finally
            {
            }
        }

        private List<BaseAxis> ResolveAxesByGroup(string groupName)
        {
            var axes = new List<BaseAxis>();
            try
            {
                if (string.IsNullOrWhiteSpace(groupName))
                    return axes;

                foreach (var axis in EnumerateAxes())
                {
                    string unitName = axis.Setup != null ? axis.Setup.UnitName : "";
                    if (string.Equals(unitName, groupName.Trim(), StringComparison.OrdinalIgnoreCase))
                        axes.Add(axis);
                }

                return axes;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ResolveAxesByGroup",
                    "Resolve axes by group failed. group=" + groupName + ", error=" + ex.Message + " - Failed");
                return axes;
            }
            finally
            {
            }
        }

        // ------------------------------------------------------------------
        // Current equipment command API
        // 작업 화면의 초기화/READY/시작/정지/수동 시퀀스 버튼에서 사용하는 현재 제어 진입점입니다.
        // ------------------------------------------------------------------

        /// <summary>장비 전체 초기화: 초기화 Plan에 따라 전체 축 HOME을 수행하고 카운터/맵 상태를 준비합니다.</summary>
        public async Task<int> InitAsync()
        {
            try
            {
                LastActionFailureMessage = "";
                if (_status == EquipmentStatus.Initializing || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "장비가 이미 초기화 중이거나 자동 운전 중입니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitAsync",
                        "Machine init failed: status=" + _status + " - Failed");
                    return -1;
                }

                SetMachineInitialized(false, "InitStart", false);
                int axisInitResult = await InitializeAllAxesAsync(false).ConfigureAwait(false);
                if (axisInitResult != 0)
                    return axisInitResult;

                CycleDone = 0; CycleTotal = 0; GoodCount = 0; NgCount = 0;

                // Resource Sensors 사전 확인(CDA / Vacuum 라인 상태).
                try
                {
                    if (_machine.ResourcesUnit != null && !_machine.ResourcesUnit.AllOk)
                    {
                        Log("[INIT] Resource warning: CDA or Vacuum line is not OK. Simulation mode may allow this.");
                    }
                }
                catch { }

                // Init 시 카세트 자동 매핑 옵션.
                if (AutoScanCassetteOnInit)
                {
                    try
                    {
                        Log("[INIT] InputCassette auto mapping start...");
                        var ctx = new QMC.CDT320.Sequencing.MachineSequenceContext(
                            this,
                            new QMC.CDT320.Sequencing.SequenceSignalBus());
                        var sequence = new QMC.CDT320.Sequencing.InputSequence(ctx);
                        int mapResult = await sequence.ExecuteMappingAsync(CancellationToken.None);
                        if (mapResult == 0)
                        {
                            int n = 0;
                            foreach (var b in _machine.InputCassetteUnit.WaferMap) if (b) n++;
                            Log($"[INIT] WaferMap OK ({n} detected)");
                        }
                        else
                        {
                            Log("[INIT] WaferMap failed: cassette not detected or scan failed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("[INIT] WaferMap exception: " + ex.Message);
                    }

                    // Output 카세트 3개도 자동 스캔합니다.
                    // slot map은 25 슬롯 기준으로 초기화되어 StoreFullWafer에서 정상 기록되어야 합니다.
                    try
                    {
                        Log("[INIT] OutputCassette auto mapping start...");
                        bool oOk = await ScanOutputCassettesAsync();
                        Log("[INIT] OutputCassette 매핑 " + (oOk ? "OK" : "FAILED"));
                    }
                    catch (Exception ex)
                    {
                        Log("[INIT] OutputCassette exception: " + ex.Message);
                    }
                }

                // Input/Output die map 생성. 이미 존재하면 skip.
                try { EnsureDieMaps(); } catch (Exception dmEx) { Log("[INIT] DieMap create warning: " + dmEx.Message); }

                Log("[INIT] Complete. Ready.");
                SetMachineInitialized(true, "InitComplete", true);
                SetStatus(EquipmentStatus.Ready);
                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "장비 초기화 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "InitAsync",
                    "Machine init failed: " + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "INIT-EX", "MachineController", LastActionFailureMessage);
                SetMachineInitialized(false, "InitException", true);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        private static bool IsAutomaticStartTemporarilyDisabled()
        {
            return false;
        }

        private async Task<int> RunReadySequenceBeforeStartAsync()
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync",
                    "START 전 Ready 시퀀스를 자동 실행합니다. - Start");
                Log("[START] Ready sequence before auto start.");

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

                SaveMachineRuntimeState("ReadySequenceComplete");
                SetStatus(EquipmentStatus.Ready);
                SetReadySequenceProgress(MachineReadySequenceState.Completed, 100, totalSteps, totalSteps, "Ready", "Ready 시퀀스가 완료되었습니다.");
                Log("[READY] Ready sequence complete.");
                QMC.Common.Log.Write("Main", "SYSTEM", "RunReadySequenceAsync",
                    "Ready sequence complete. - Ok");
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

        /// <summary>장비 START: Servo ON 후 현재 구성된 자동 시퀀스를 시작합니다.</summary>
        public async Task<int> StartAsync()
        {
            try
            {
                if (IsAutomaticStartTemporarilyDisabled())
                    return -1;

                LastActionFailureMessage = "";

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
                    QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync", "Start failed: sequence is already running. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "START-RUNNING", "MachineController", "Sequence가 이미 실행 중입니다.");
                    Log("[START] failed: sequence is already running");
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("StartAsync"))
                    return -1;

                int readyResult = await RunReadySequenceBeforeStartAsync().ConfigureAwait(false);
                if (readyResult != 0)
                    return readyResult;

                if (!EnsureReticleAvoidForAutoStart("StartAsync"))
                    return -1;

                Log("[START] Process auto sequence start.");
                QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync", "Process auto sequence start requested. - Ok");

                await StartSequenceAsync(
                    QMC.CDT320.Sequencing.SequenceRunOptions.ProcessAuto()).ConfigureAwait(false);

                return 0;
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
            }
        }

        /// <summary>
        /// 일반 정지 요청입니다.
        /// 자동 시퀀스 중에는 축을 즉시 정지하지 않고 현재 동작 경계에서 정지하도록 요청합니다.
        /// 축 즉시 정지는 EMO/알람 응답 또는 수동 조작 정지에서만 수행합니다.
        /// </summary>
        public async Task StopAsync()
        {
            OnStopRequested();

            if (_coordinator != null && _coordinatorTask != null && !_coordinatorTask.IsCompleted)
            {
                await RequestCycleStopSequenceAsync().ConfigureAwait(false);
                Log("[STOP] 일반 정지 요청: 현재 동작 완료 후 시퀀스 경계에서 정지합니다.");
                return;
            }

            if (_cycleCts != null && _status == EquipmentStatus.AutoRunning)
            {
                _cycleStopRequested = true;
                _cycleResumePending = true;
                Log("[STOP] 일반 정지 요청: 현재 사이클 완료 후 정지합니다.");
                return;
            }

            CancelManualOperation();

            // 수동/조그 조작 중에는 운전자가 누른 정지 명령이므로 현재 움직이는 축을 정지한다.
            foreach (var ax in EnumerateAxes()) ax.Stop();

            bool wasCycling = (_status == EquipmentStatus.AutoRunning);
            _cycleResumePending = false;
            _cycleCts?.Cancel();

            var lot = QMC.CDT320.Lots.LotStorage.ActiveLot;
            if (wasCycling && lot != null)
            {
                int skipped = System.Math.Max(0, CycleTotal - CycleDone);
                lot.SkippedCount = skipped;
                Log("[STOP] LOT=" + lot.LotID + " finalize (done=" + CycleDone + "/" + CycleTotal +
                    ", good=" + GoodCount + ", ng=" + NgCount + ", skipped=" + skipped + ")");
                try { QMC.CDT320.Lots.LotStorage.CloseLot(aborted: true); } catch { }
                try { _machine.OpPanelUnit?.TowerLampOff(); } catch { }
            }
            else
            {
                Log("[STOP] Stopped.");
            }
            SetStatus(EquipmentStatus.Stopped);
        }

        private void OnStopRequested()
        {
            var handler = StopRequested;
            if (handler == null)
                return;

            try
            {
                handler();
            }
            catch
            {
            }
        }

        private IDisposable EnterManualOperation()
        {
            if (Interlocked.Increment(ref _manualBusyCount) == 1)
            {
                var old = Interlocked.Exchange(ref _manualCts, new CancellationTokenSource());
                if (old != null)
                    old.Dispose();
            }

            if (_status != EquipmentStatus.Alarm && _status != EquipmentStatus.AutoRunning)
                SetStatus(EquipmentStatus.ManualRunning);
            return new ManualOperationScope(this);
        }

        private IDisposable BeginManualMotionSpeedOnlyScope(string reason)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ManualSequenceSpeedScale",
                    "수동 동작 속도 스코프 적용. percent=" +
                    MotionSpeedScale.ManualSequencePercent.ToString("0.###") +
                    ", reason=" + reason + " - Start");
                return MotionSpeedScale.BeginManualSequenceScale();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ManualSequenceSpeedScale",
                    "수동 동작 속도 스코프 적용 실패. reason=" + reason +
                    ", error=" + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private IDisposable BeginReadySequenceSpeedScope(string reason)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ReadySequenceSpeedScale",
                    "READY 시퀀스 속도 스코프 적용. percent=" +
                    MotionSpeedScale.ReadySequencePercent.ToString("0.###") +
                    ", reason=" + reason + " - Start");
                return MotionSpeedScale.BeginReadySequenceScale();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ReadySequenceSpeedScale",
                    "READY 시퀀스 속도 스코프 적용 실패. reason=" + reason +
                    ", error=" + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private IDisposable BeginManualProcessSequenceScope(string reason)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ManualSequenceSpeedScale",
                    "수동 시컨스 속도/공정 인터락 스코프 적용. percent=" +
                    MotionSpeedScale.ManualSequencePercent.ToString("0.###") +
                    ", reason=" + reason + " - Start");
                IDisposable speedScope = null;
                IDisposable guardScope = null;
                try
                {
                    speedScope = MotionSpeedScale.BeginManualSequenceScale();
                    guardScope = MotionGuardRuntime.BeginManualSequenceProcessMove(reason);
                    return new CompositeManualSequenceScope(speedScope, guardScope);
                }
                catch
                {
                    if (guardScope != null)
                        guardScope.Dispose();
                    if (speedScope != null)
                        speedScope.Dispose();
                    throw;
                }
                finally
                {
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ManualSequenceSpeedScale",
                    "수동 시컨스 속도/공정 인터락 스코프 적용 실패. reason=" + reason +
                    ", error=" + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private IDisposable BeginManualMotionScope(ManualMotionScopeKind kind, string reason)
        {
            switch (kind)
            {
                case ManualMotionScopeKind.SpeedOnly:
                    return BeginManualMotionSpeedOnlyScope(reason);
                case ManualMotionScopeKind.ReadySequence:
                    return BeginReadySequenceSpeedScope(reason);
                case ManualMotionScopeKind.ProcessSequence:
                    return BeginManualProcessSequenceScope(reason);
                default:
                    throw new ArgumentOutOfRangeException(
                        "kind",
                        kind,
                        "지원하지 않는 수동 모션 스코프 종류입니다.");
            }
        }

        public IDisposable BeginManualActionScope(ManualMotionScopeKind kind, string reason)
        {
            try
            {
                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                    throw new InvalidOperationException(
                        "자동/시컨스 동작 중에는 수동 동작을 시작할 수 없습니다. reason=" + reason +
                        ", status=" + _status +
                        ", activeMode=" + (ActiveSequenceRunMode.HasValue ? ActiveSequenceRunMode.Value.ToString() : "-"));

                IDisposable manualScope = null;
                IDisposable motionScope = null;
                try
                {
                    manualScope = EnterManualOperation();
                    motionScope = BeginManualMotionScope(kind, reason);
                    return new ManualActionScope(manualScope, motionScope);
                }
                catch
                {
                    if (motionScope != null)
                        motionScope.Dispose();
                    if (manualScope != null)
                        manualScope.Dispose();
                    throw;
                }
                finally
                {
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ManualActionScope",
                    "수동 동작 스코프 시작 실패. reason=" + reason +
                    ", kind=" + kind +
                    ", error=" + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }

        private IDisposable BeginManualProcessSequenceScopeIfNeeded(QMC.CDT320.Sequencing.SequenceRunMode mode)
        {
            try
            {
                if (mode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    return null;

                return BeginManualProcessSequenceScope("Coordinator:" + mode);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public void CancelManualOperation()
        {
            var cts = _manualCts;
            if (cts != null && !cts.IsCancellationRequested)
                cts.Cancel();
        }

        private void LeaveManualOperation()
        {
            if (Interlocked.Decrement(ref _manualBusyCount) < 0)
                Interlocked.Exchange(ref _manualBusyCount, 0);

            if (!IsManualBusy)
            {
                var cts = Interlocked.Exchange(ref _manualCts, null);
                if (cts != null)
                    cts.Dispose();
            }

            if (!IsManualBusy && !IsSequenceRunning && _status == EquipmentStatus.ManualRunning)
                SetStatus(EquipmentStatus.Stopped);
        }

        private sealed class ManualOperationScope : IDisposable
        {
            private MachineController _owner;

            public ManualOperationScope(MachineController owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                var owner = Interlocked.Exchange(ref _owner, null);
                if (owner != null)
                    owner.LeaveManualOperation();
            }
        }

        private sealed class ManualActionScope : IDisposable
        {
            private IDisposable _manualScope;
            private IDisposable _motionScope;
            private bool _disposed;

            public ManualActionScope(IDisposable manualScope, IDisposable motionScope)
            {
                _manualScope = manualScope;
                _motionScope = motionScope;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                Exception first = null;
                try
                {
                    IDisposable motion = _motionScope;
                    _motionScope = null;
                    if (motion != null)
                        motion.Dispose();
                }
                catch (Exception ex)
                {
                    first = ex;
                }

                try
                {
                    IDisposable manual = _manualScope;
                    _manualScope = null;
                    if (manual != null)
                        manual.Dispose();
                }
                catch (Exception ex)
                {
                    if (first == null)
                        first = ex;
                }
                finally
                {
                    _disposed = true;
                }

                if (first != null)
                    throw first;
            }
        }

        private sealed class CompositeManualSequenceScope : IDisposable
        {
            private IDisposable _speedScope;
            private IDisposable _guardScope;
            private bool _disposed;

            public CompositeManualSequenceScope(IDisposable speedScope, IDisposable guardScope)
            {
                _speedScope = speedScope;
                _guardScope = guardScope;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                Exception first = null;
                try
                {
                    IDisposable guard = _guardScope;
                    _guardScope = null;
                    if (guard != null)
                        guard.Dispose();
                }
                catch (Exception ex)
                {
                    first = ex;
                }

                try
                {
                    IDisposable speed = _speedScope;
                    _speedScope = null;
                    if (speed != null)
                        speed.Dispose();
                }
                catch (Exception ex)
                {
                    if (first == null)
                        first = ex;
                }
                finally
                {
                    _disposed = true;
                }

                if (first != null)
                    throw first;
            }
        }

        public void RecordAutoDiePlacedForStats(BinSide side)
        {
            try
            {
                if (ActiveSequenceRunMode != QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    return;

                long cycleMs;
                lock (_productionStatsLock)
                {
                    if (_autoProductionStopwatch == null)
                        _autoProductionStopwatch = System.Diagnostics.Stopwatch.StartNew();

                    cycleMs = _autoProductionStopwatch.ElapsedMilliseconds;
                    if (cycleMs <= 0)
                        cycleMs = 1;
                    _autoProductionStopwatch.Restart();
                }

                int good = side == BinSide.Good ? 1 : 0;
                int ng = side == BinSide.Ng ? 1 : 0;
                Stats.OnCycleCompleted(1, good, ng, cycleMs);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("WorkStats", "SYSTEM", "RecordAutoDiePlacedForStats",
                    "작업 시간 통계 업데이트 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public void RecordOutputStageProductReceivedForTact(
            BinSide side,
            string dieId,
            int pickerNo,
            OutputStageReceiveTarget receiveTarget)
        {
            try
            {
                TactTimeRecorder recorder = _activeTactTimeRecorder;
                if (recorder == null || object.ReferenceEquals(recorder, NullTactTimeRecorder.Instance))
                    return;

                DateTime now = DateTime.Now;
                DateTime previousAt;
                BinSide? previousSide;
                lock (_outputReceiveTactLock)
                {
                    previousAt = _lastOutputReceiveTactAt;
                    previousSide = _lastOutputReceiveTactSide;
                    _lastOutputReceiveTactAt = now;
                    _lastOutputReceiveTactSide = side;
                }

                if (previousAt == DateTime.MinValue)
                    return;

                long elapsedMs = Math.Max(0, (long)(now - previousAt).TotalMilliseconds);
                string sideName = ResolveOutputReceiveTactSideName(side);
                string previousSideName = previousSide.HasValue
                    ? ResolveOutputReceiveTactSideName(previousSide.Value)
                    : "-";

                recorder.Record(new TactTimeRecord
                {
                    UnitName = "OutputStage",
                    SequenceName = "OutputReceiveTact",
                    ProcessName = "Output Receive TactTime",
                    StepName = sideName,
                    Category = TactTimeCategory.Process,
                    StartedAt = previousAt,
                    EndedAt = now,
                    ElapsedMs = elapsedMs,
                    Result = TactTimeResult.Ok,
                    Detail =
                        "OutputStage가 제품 1개를 받은 간격입니다. side=" + sideName +
                        ", previousSide=" + previousSideName +
                        ", die=" + (dieId ?? "") +
                        ", pickerNo=" + pickerNo +
                        ", outputWafer=" + (receiveTarget != null ? receiveTarget.OutputWaferId : "-") +
                        ", order=" + (receiveTarget != null ? receiveTarget.OrderIndex.ToString() : "-") +
                        ", mapX=" + (receiveTarget != null ? receiveTarget.DieMapX.ToString() : "-") +
                        ", mapY=" + (receiveTarget != null ? receiveTarget.DieMapY.ToString() : "-")
                });
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("WorkStats", "SYSTEM", "RecordOutputStageProductReceivedForTact",
                    "Output 수령 택타임 기록 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        public void RecordInspectionCheckpointForTact(
            string key,
            string processName,
            string stepName,
            string pickerSide,
            string dieId,
            int pickerNo,
            string detail)
        {
            try
            {
                TactTimeRecorder recorder = _activeTactTimeRecorder;
                if (recorder == null || object.ReferenceEquals(recorder, NullTactTimeRecorder.Instance))
                    return;

                key = string.IsNullOrWhiteSpace(key) ? processName : key;
                DateTime now = DateTime.Now;
                DateTime previousAt;
                lock (_inspectionTactLock)
                {
                    _lastInspectionTactTimes.TryGetValue(key, out previousAt);
                    _lastInspectionTactTimes[key] = now;
                }

                if (previousAt == DateTime.MinValue)
                    return;

                long elapsedMs = Math.Max(0, (long)(now - previousAt).TotalMilliseconds);
                recorder.Record(new TactTimeRecord
                {
                    UnitName = "Inspection",
                    SequenceName = "InspectionTact",
                    ProcessName = processName ?? "",
                    StepName = stepName ?? "",
                    Category = TactTimeCategory.Process,
                    StartedAt = previousAt,
                    EndedAt = now,
                    ElapsedMs = elapsedMs,
                    Result = TactTimeResult.Ok,
                    Detail =
                        "검사 완료 후 다음 제품 검사 완료까지의 간격입니다. key=" + key +
                        ", side=" + (pickerSide ?? "") +
                        ", die=" + (dieId ?? "") +
                        ", pickerNo=" + pickerNo +
                        (string.IsNullOrWhiteSpace(detail) ? "" : ", " + detail)
                });
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("WorkStats", "SYSTEM", "RecordInspectionCheckpointForTact",
                    "검사 택타임 기록 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void BeginAutoProductionStats()
        {
            try
            {
                string lotId = ResolveProductionStatsLotId();
                int totalDies = ResolveProductionStatsTotalDies();

                lock (_productionStatsLock)
                {
                    _autoProductionStopwatch = System.Diagnostics.Stopwatch.StartNew();
                }

                Stats.BeginLot(lotId, totalDies);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("WorkStats", "SYSTEM", "BeginAutoProductionStats",
                    "작업 시간 통계 시작 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void EndAutoProductionStats()
        {
            try
            {
                lock (_productionStatsLock)
                {
                    if (_autoProductionStopwatch != null)
                    {
                        _autoProductionStopwatch.Stop();
                        _autoProductionStopwatch = null;
                    }
                }

                Stats.EndLot();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("WorkStats", "SYSTEM", "EndAutoProductionStats",
                    "작업 시간 통계 종료 실패: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private string ResolveProductionStatsLotId()
        {
            try
            {
                if (LotStorage.ActiveLot != null && !string.IsNullOrEmpty(LotStorage.ActiveLot.LotID))
                    return LotStorage.ActiveLot.LotID;

                MaterialSnapshot state = MaterialStorage.State;
                if (state != null && !string.IsNullOrEmpty(state.LotId))
                    return state.LotId;
            }
            catch
            {
            }

            return "LOT-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        }

        private int ResolveProductionStatsTotalDies()
        {
            try
            {
                if (LotStorage.ActiveLot != null && LotStorage.ActiveLot.TotalDies > 0)
                    return LotStorage.ActiveLot.TotalDies;

                MaterialSnapshot state = MaterialStorage.State;
                if (state != null && state.Dies != null)
                {
                    int count = 0;
                    foreach (DieMaterial die in state.Dies)
                    {
                        if (die != null && die.IsInputTarget)
                            count++;
                    }

                    if (count > 0)
                        return count;
                }
            }
            catch
            {
            }

            return 0;
        }

        private TactTimeRecorder CreateTactTimeRecorder(QMC.CDT320.Sequencing.SequenceRunOptions options)
        {
            try
            {
                var memorySink = new MemoryTactTimeSink();
                _tactTimeMemorySink = memorySink;

                string root = System.IO.Path.Combine(QMC.Common.Logging.EventLogger.LogRoot, "TactTime");
                var sinks = new ITactTimeSink[]
                {
                    memorySink,
                    new CsvTactTimeSink(root)
                };

                return new TactTimeRecorder(
                    "CDT-320",
                    ResolveTactProjectName(),
                    ResolveTactLotId(),
                    options != null ? options.Mode.ToString() : "",
                    sinks);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "TactTime",
                    "택타임 계측기 생성 실패. Null 계측기로 대체합니다. error=" + ex.Message + " - Failed");
                return NullTactTimeRecorder.Instance;
            }
            finally
            {
            }
        }

        private void ResetOutputReceiveTactTimeState()
        {
            lock (_outputReceiveTactLock)
            {
                _lastOutputReceiveTactAt = DateTime.MinValue;
                _lastOutputReceiveTactSide = null;
            }
        }

        private void ResetInspectionTactTimeState()
        {
            lock (_inspectionTactLock)
            {
                _lastInspectionTactTimes.Clear();
            }
        }

        private static string ResolveOutputReceiveTactSideName(BinSide side)
        {
            return side == BinSide.Good ? "OK" : "NG";
        }

        private string ResolveTactProjectName()
        {
            try
            {
                string name = RecipeStore.GetLastProjectName();
                return string.IsNullOrWhiteSpace(name) ? "" : System.IO.Path.GetFileNameWithoutExtension(name);
            }
            catch
            {
                return "";
            }
            finally
            {
            }
        }

        private string ResolveTactLotId()
        {
            try
            {
                return ResolveProductionStatsLotId();
            }
            catch
            {
                return "LOT-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            }
            finally
            {
            }
        }

        /// <summary>
        /// 지정한 옵션으로 병렬 시퀀스 Coordinator를 시작합니다.
        /// 자동 운전의 기준 진입점이며, Unit/Mode/StartMode는 SequenceRunOptions로 결정합니다.
        /// </summary>
        public async Task StartSequenceAsync(QMC.CDT320.Sequencing.SequenceRunOptions options)
        {
            try
            {
                if (!EnsureMachineInitializedForRun("StartSequenceAsync"))
                    return;

                if (_coordinatorTask != null && !_coordinatorTask.IsCompleted)
                    await StopSequenceAsync().ConfigureAwait(false);

                if (options == null)
                    options = QMC.CDT320.Sequencing.SequenceRunOptions.FullAuto();

                if (options.Mode == QMC.CDT320.Sequencing.SequenceRunMode.Auto &&
                    !EnsureReticleAvoidForAutoStart("StartSequenceAsync"))
                    return;

                _autoCts = new CancellationTokenSource();
                var bus = new QMC.CDT320.Sequencing.SequenceSignalBus();
                TactTimeRecorder tact = CreateTactTimeRecorder(options);
                _activeTactTimeRecorder = tact;
                ResetOutputReceiveTactTimeState();
                ResetInspectionTactTimeState();
                // 새 시퀀스 시작: 4개 유닛 상태를 Idle 로 초기화하고 동일 ActivityMonitor 를 컨텍스트에 주입한다.
                _sequenceActivity.Reset();
                _seqContext = new QMC.CDT320.Sequencing.MachineSequenceContext(
                    this, bus, new QMC.CDT320.Sequencing.SequenceResourceManager(), _sequenceActivity, tact);
                _coordinator = new QMC.CDT320.Sequencing.AutoSequenceCoordinator(_seqContext);

                _coordinator.Register(
                    QMC.CDT320.Sequencing.SequenceUnitKind.InputLoader,
                    () => new QMC.CDT320.Sequencing.InputSequence(_seqContext));
                _coordinator.Register(
                    QMC.CDT320.Sequencing.SequenceUnitKind.OutputUnloader,
                    () => new QMC.CDT320.Sequencing.OutputSequence(_seqContext));
                _coordinator.Register(
                    QMC.CDT320.Sequencing.SequenceUnitKind.PickerFront,
                    () => new QMC.CDT320.Sequencing.FrontPickerSequence(_seqContext));
                _coordinator.Register(
                    QMC.CDT320.Sequencing.SequenceUnitKind.PickerRear,
                    () => new QMC.CDT320.Sequencing.RearPickerSequence(_seqContext));

                _coordinator.Configure(options);
                ActiveSequenceRunMode = options.Mode;
                if (options.Mode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    BeginAutoProductionStats();
                SetStatus(options.Mode == QMC.CDT320.Sequencing.SequenceRunMode.Auto
                    ? EquipmentStatus.AutoRunning
                    : EquipmentStatus.ManualRunning);
                Log("[SEQ] StartSequenceAsync units=" + options.Units + ", mode=" + options.Mode);
                QMC.Common.Log.Write("Main", "SYSTEM", "StartSequenceAsync",
                    "Sequence start. units=" + options.Units + ", mode=" + options.Mode + " - Ok");

                var coordinator = _coordinator;
                var cts = _autoCts;
                var runMode = options.Mode;
                _coordinatorTask = Task.Run(async () =>
                {
                    IDisposable sequenceScope = null;
                    try
                    {
                        sequenceScope = BeginManualProcessSequenceScopeIfNeeded(runMode);
                        await coordinator.RunAsync(cts.Token).ConfigureAwait(false);
                        if (!cts.IsCancellationRequested && _status != EquipmentStatus.Alarm)
                        {
                            Log("[SEQ] Complete");
                            if (ActiveSequenceRunMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                                EndAutoProductionStats();
                            SetStatus(EquipmentStatus.Ready);
                        }
                    }
                    catch (QMC.CDT320.Sequencing.SequenceStopException ex)
                    {
                        // 남아 있는 진행/대기 유닛을 정지 상태로 정리한다.
                        _sequenceActivity.SweepActiveTo(QMC.CDT320.Sequencing.SequenceActivityState.Stopped,
                            "시퀀스가 정지되었습니다.");
                        QMC.Common.Log.Write("Main", "SYSTEM", "StartSequenceAsync",
                            "Sequence stopped: " + ex.Message + " - Stopped");
                        Log("[SEQ] stopped: " + ex.Message);
                        if (_status != EquipmentStatus.Alarm)
                        {
                            if (_seqContext != null && _seqContext.IsCycleStopRequested)
                            {
                                QMC.CDT320.Sequencing.SequenceResumeStore.MarkCycleStopped(
                                    "AutoSequence",
                                    "",
                                    ex.Message);
                                SetStatus(EquipmentStatus.CycleStopped);
                            }
                            else
                            {
                                if (ActiveSequenceRunMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                                    EndAutoProductionStats();
                                SetStatus(EquipmentStatus.Stopped);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 남아 있는 진행/대기 유닛을 취소 상태로 정리한다.
                        _sequenceActivity.SweepActiveTo(QMC.CDT320.Sequencing.SequenceActivityState.Canceled,
                            "시퀀스가 취소되었습니다.");
                        Log("[SEQ] Canceled");
                        if (ActiveSequenceRunMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                            EndAutoProductionStats();
                    }
                    catch (Exception ex)
                    {
                        // 실패 유닛은 UnitSequenceBase 에서 이미 Alarm. 남은 진행/대기 유닛은 정지로 정리한다.
                        _sequenceActivity.SweepActiveTo(QMC.CDT320.Sequencing.SequenceActivityState.Stopped,
                            "다른 유닛 알람으로 정지되었습니다.");
                        QMC.Common.Log.Write("Main", "SYSTEM", "StartSequenceAsync",
                            "자동 시퀀스 실패: " + ex.Message + " - Failed");
                        AlarmManager.Raise(AlarmSeverity.Error, "SEQ-EX", "MachineController", "자동 시퀀스 실패: " + ex.Message);
                        Log("[SEQ] failed: " + ex.Message);
                        if (ActiveSequenceRunMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                            EndAutoProductionStats();
                        SetStatus(EquipmentStatus.Alarm);
                    }
                    finally
                    {
                        if (sequenceScope != null)
                            sequenceScope.Dispose();

                        if (_coordinator == coordinator)
                        {
                            if (_activeTactTimeRecorder != null &&
                                !object.ReferenceEquals(_activeTactTimeRecorder, NullTactTimeRecorder.Instance))
                            {
                                _activeTactTimeRecorder.Dispose();
                            }

                            _activeTactTimeRecorder = null;
                            _coordinator = null;
                            _seqContext = null;
                            _coordinatorTask = null;
                            ActiveSequenceRunMode = null;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                if (_activeTactTimeRecorder != null &&
                    !object.ReferenceEquals(_activeTactTimeRecorder, NullTactTimeRecorder.Instance))
                {
                    _activeTactTimeRecorder.Dispose();
                    _activeTactTimeRecorder = null;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "StartSequenceAsync",
                    "자동 시퀀스 시작 실패: " + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-START-EX", "MachineController", "자동 시퀀스 시작 실패: " + ex.Message);
                Log("[SEQ] start failed: " + ex.Message);
                SetStatus(EquipmentStatus.Alarm);
                throw;
            }
            finally
            {
            }
        }

        /// <summary>실행 중인 병렬 시퀀스를 중단하고 Coordinator 종료를 대기합니다.</summary>
        public async Task StopSequenceAsync()
        {
            var coordinator = _coordinator;
            var cts = _autoCts;
            var task = _coordinatorTask;

            if (coordinator != null)
                coordinator.AbortChildren();
            if (cts != null && !cts.IsCancellationRequested)
                cts.Cancel();

            if (task != null)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    Log("[SEQ] StopSequenceAsync canceled");
                }
            }

            _coordinatorTask = null;
            _coordinator = null;
            _seqContext = null;
            ActiveSequenceRunMode = null;
            if (_autoCts != null)
            {
                _autoCts.Dispose();
                _autoCts = null;
            }

            if (_status == EquipmentStatus.ManualRunning || _status == EquipmentStatus.AutoRunning)
                SetStatus(EquipmentStatus.Stopped);
        }

        /// <summary>현재 자동 시퀀스를 즉시 취소하지 않고 작업 경계에서 CYCLE STOP 되도록 요청합니다.</summary>
        public Task RequestCycleStopSequenceAsync()
        {
            try
            {
                LastActionFailureMessage = "";

                var coordinator = _coordinator;
                if (coordinator == null || _coordinatorTask == null || _coordinatorTask.IsCompleted)
                {
                    LastActionFailureMessage = "진행 중인 자동 시퀀스가 없어 CYCLE STOP 요청을 무시합니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "CycleStopSequence",
                        LastActionFailureMessage + " - Ignored");
                    Log("[SEQ] Cycle stop ignored: no active auto sequence");
                    return Task.CompletedTask;
                }

                coordinator.RequestCycleStop();
                QMC.Common.Log.Write("Main", "SYSTEM", "CycleStopSequence",
                    "Cycle stop requested. The sequence will stop at the next safe boundary. - Requested");
                Log("[SEQ] Cycle stop requested");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "CYCLE STOP 요청 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "CycleStopSequence",
                    LastActionFailureMessage + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-CYCLE-STOP-EX", "MachineController", LastActionFailureMessage);
                Log("[SEQ] Cycle stop request failed: " + ex.Message);
                return Task.CompletedTask;
            }
            finally
            {
            }
        }

        public async Task<int> StopSequenceForAlarmAsync(string alarmCode)
        {
            try
            {
                var coordinator = _coordinator;
                var cts = _autoCts;
                var task = _coordinatorTask;

                if (coordinator != null)
                    coordinator.AbortChildren();
                if (cts != null && !cts.IsCancellationRequested)
                    cts.Cancel();

                if (task != null)
                {
                    try
                    {
                        await task.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        Log("[SEQ] alarm stop canceled. code=" + alarmCode);
                    }
                }

                _coordinatorTask = null;
                _coordinator = null;
                _seqContext = null;
                ActiveSequenceRunMode = null;

                if (_autoCts != null)
                {
                    _autoCts.Dispose();
                    _autoCts = null;
                }

                if (_status == EquipmentStatus.ManualRunning || _status == EquipmentStatus.AutoRunning)
                    SetStatus(EquipmentStatus.Stopped);

                QMC.Common.Log.Write("Main", "SYSTEM", "StopSequenceForAlarm",
                    "Alarm response stopped active sequence. code=" + alarmCode +
                    ", status=" + _status + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "StopSequenceForAlarm",
                    "Sequence stop by alarm failed. code=" + alarmCode + ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        public async Task<int> StopAxesAsync(IEnumerable<string> axisNames, bool emergencyStop = false)
        {
            try
            {
                if (axisNames == null)
                    return 0;

                int total = 0;
                int failed = 0;
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var axisName in axisNames)
                {
                    if (string.IsNullOrWhiteSpace(axisName))
                        continue;
                    if (!visited.Add(axisName.Trim()))
                        continue;

                    var axis = FindAxisByName(axisName);
                    if (axis == null)
                    {
                        failed++;
                        QMC.Common.Log.Write("Main", "SYSTEM", "StopAxes",
                            "Axis stop failed: axis not found. axis=" + axisName + " - Failed");
                        continue;
                    }

                    total++;
                    try
                    {
                        if (emergencyStop)
                            axis.EStop();
                        else
                            axis.Stop();

                        QMC.Common.Log.Write("Main", "SYSTEM", "StopAxes",
                            "Axis stopped. axis=" + axis.Name + ", emergency=" + emergencyStop + " - Ok");
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        QMC.Common.Log.Write("Main", "SYSTEM", "StopAxes",
                            "Axis stop failed. axis=" + axis.Name + ", error=" + ex.Message + " - Failed");
                    }
                }

                await Task.Yield();
                return failed == 0 ? 0 : -1;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "StopAxes",
                    "Axis stop failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        public async Task<int> StopInterferenceGroupAsync(string sourceAxisName, bool emergencyStop = false)
        {
            try
            {
                var axes = _axisInterferenceMap.ResolveInterferenceAxes(sourceAxisName);
                QMC.Common.Log.Write("Main", "SYSTEM", "StopInterferenceGroup",
                    "Interference group stop requested. sourceAxis=" + sourceAxisName +
                    ", count=" + (axes != null ? axes.Count : 0) + ", emergency=" + emergencyStop + " - Start");
                return await StopAxesAsync(axes, emergencyStop).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "StopInterferenceGroup",
                    "Interference group stop failed. sourceAxis=" + sourceAxisName + ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        public async Task<int> StopAllAxesAsync(bool emergencyStop = false)
        {
            try
            {
                var axes = new List<string>();
                foreach (var axis in EnumerateAxes())
                    axes.Add(axis.Name);

                QMC.Common.Log.Write("Main", "SYSTEM", "StopAllAxes",
                    "All axis stop requested. count=" + axes.Count + ", emergency=" + emergencyStop + " - Start");
                return await StopAxesAsync(axes, emergencyStop).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "StopAllAxes",
                    "All axis stop failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        public string ResolveAxisNameFromAlarm(string alarmSource, string alarmCode)
        {
            try
            {
                string source = alarmSource ?? "";
                string code = alarmCode ?? "";

                foreach (var axis in EnumerateAxes())
                {
                    if (string.Equals(source, axis.Name, StringComparison.OrdinalIgnoreCase))
                        return axis.Name;

                    if (source.IndexOf(axis.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                        return axis.Name;

                    if (code.IndexOf(axis.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                        return axis.Name;
                }

                return "";
            }
            catch
            {
                return "";
            }
            finally
            {
            }
        }

        public void SetAlarmStateFromAlarmResponse(string alarmCode)
        {
            try
            {
                if (_status != EquipmentStatus.Alarm)
                    SetStatus(EquipmentStatus.Alarm);

                QMC.Common.Log.Write("Main", "SYSTEM", "SetAlarmStateFromAlarmResponse",
                    "Machine status set to Alarm by alarm response. code=" + alarmCode + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "SetAlarmStateFromAlarmResponse",
                    "Machine status alarm update failed. code=" + alarmCode + ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        /// <summary>지정한 유닛들을 Manual 모드로 시작합니다.</summary>
        public Task StartManualAsync(QMC.CDT320.Sequencing.SequenceUnitKind units)
        {
            return StartSequenceAsync(new QMC.CDT320.Sequencing.SequenceRunOptions
            {
                Units = units,
                Mode = QMC.CDT320.Sequencing.SequenceRunMode.Manual
            });
        }

        /// <summary>지정한 단일 유닛을 지정 실행 모드로 시작합니다.</summary>
        public Task StartSingleUnitAsync(
            QMC.CDT320.Sequencing.SequenceUnitKind unit,
            QMC.CDT320.Sequencing.SequenceRunMode mode)
        {
            return StartSequenceAsync(QMC.CDT320.Sequencing.SequenceRunOptions.Single(unit, mode));
        }

        /// <summary>Manual 또는 Step 모드에서 지정 유닛을 1단계 진행시킵니다.</summary>
        public void ManualStep(QMC.CDT320.Sequencing.SequenceUnitKind unit)
        {
            if (_coordinator == null)
            {
                Log("[SEQ] ManualStep ignored: coordinator 없음");
                return;
            }

            _coordinator.StepUnit(unit);
        }

        /// <summary>Manual 또는 Step 모드에서 활성 유닛 전체를 1단계 진행합니다.</summary>
        public void ManualStepAll()
        {
            if (_coordinator == null)
            {
                Log("[SEQ] ManualStepAll ignored: coordinator 없음");
                return;
            }

            _coordinator.StepAll();
        }

        /// <summary>Manual Sequence Dialog에서 지정 유닛을 Auto와 같은 데이터/시퀀스 컨텍스트로 1단계 진행합니다.</summary>
        public async Task<int> RunManualSequenceUnitStepAsync(QMC.CDT320.Sequencing.SequenceUnitKind unit)
        {
            try
            {
                LastActionFailureMessage = "";

                if (unit == QMC.CDT320.Sequencing.SequenceUnitKind.None)
                {
                    LastActionFailureMessage = "실행할 Manual 시퀀스 유닛이 선택되지 않았습니다.";
                    return -1;
                }

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 Manual 시퀀스를 실행할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunManualSequenceUnitStep",
                        LastActionFailureMessage + " unit=" + unit + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-STEP-ALARM", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("RunManualSequenceUnitStepAsync"))
                    return -1;

                if (IsManualBusy)
                {
                    LastActionFailureMessage = "다른 Manual 동작이 진행 중이라 Manual 시퀀스를 실행할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunManualSequenceUnitStep",
                        LastActionFailureMessage + " unit=" + unit + " - Failed");
                    return -1;
                }

                if (IsSequenceRunning)
                {
                    if (ActiveSequenceRunMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    {
                        LastActionFailureMessage = "Auto Sequence 실행 중에는 Manual 시퀀스를 실행할 수 없습니다.";
                        QMC.Common.Log.Write("Main", "SYSTEM", "RunManualSequenceUnitStep",
                            LastActionFailureMessage + " unit=" + unit + " - Failed");
                        AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-STEP-AUTO-RUNNING", "MachineController", LastActionFailureMessage);
                        return -1;
                    }

                    ManualStep(unit);
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunManualSequenceUnitStep",
                        "Manual sequence unit step gate released. unit=" + unit + " - Ok");
                    return 0;
                }

                if (_status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto Running 상태에서는 Manual 시퀀스를 시작할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunManualSequenceUnitStep",
                        LastActionFailureMessage + " unit=" + unit + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-STEP-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                await StartSequenceAsync(QMC.CDT320.Sequencing.SequenceRunOptions.ProcessStep()).ConfigureAwait(false);
                ManualStep(unit);

                QMC.Common.Log.Write("Main", "SYSTEM", "RunManualSequenceUnitStep",
                    "Manual sequence process step mode started. unit=" + unit + " - Ok");
                Log("[SEQ] Manual sequence step start. unit=" + unit);
                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "Manual 시퀀스 실행 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "RunManualSequenceUnitStep",
                    LastActionFailureMessage + " unit=" + unit + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-STEP-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>Manual Sequence Dialog에서 Picker 단위 공정을 Auto와 동일한 Material/DieMap 상태로 실행합니다.</summary>
        public async Task<int> RunManualPickerProcessAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            string processName)
        {
            try
            {
                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 Picker Manual 공정을 실행할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-ALARM", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (IsManualBusy)
                {
                    LastActionFailureMessage = "다른 Manual 동작이 진행 중이라 Picker Manual 공정을 실행할 수 없습니다.";
                    return -1;
                }

                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto/Manual 시퀀스가 실행 중일 때는 Picker Manual 공정을 새로 시작할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("RunManualPickerProcessAsync"))
                    return -1;

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                using (BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "ManualPickerProcess:" + side + ":" + processName))
                {
                    var bus = new QMC.CDT320.Sequencing.SequenceSignalBus();
                    var context = new QMC.CDT320.Sequencing.MachineSequenceContext(
                        this,
                        bus,
                        new QMC.CDT320.Sequencing.SequenceResourceManager(),
                        _sequenceActivity);
                    var options = QMC.CDT320.Sequencing.PickerSequenceOptions.Default();
                    options.RunMode = QMC.CDT320.Sequencing.SequenceRunMode.Manual;

                    string name = (processName ?? "").Trim();
                    int result;
                    if (string.Equals(name, "PickUp", StringComparison.OrdinalIgnoreCase))
                    {
                        result = await new QMC.CDT320.Sequencing.PickerPickUpSequence(context, side)
                            .RunAsync(ManualOperationToken, options).ConfigureAwait(false);
                    }
                    else if (string.Equals(name, "Bottom", StringComparison.OrdinalIgnoreCase))
                    {
                        result = await new QMC.CDT320.Sequencing.PickerBottomInspectionSequence(context, side)
                            .RunAsync(ManualOperationToken, options).ConfigureAwait(false);
                    }
                    else if (string.Equals(name, "Side", StringComparison.OrdinalIgnoreCase))
                    {
                        result = await new QMC.CDT320.Sequencing.PickerSideInspectionSequence(context, side)
                            .RunAsync(ManualOperationToken, options).ConfigureAwait(false);
                    }
                    else if (string.Equals(name, "Place", StringComparison.OrdinalIgnoreCase))
                    {
                        result = await new QMC.CDT320.Sequencing.PickerPlaceSequence(context, side)
                            .RunAsync(ManualOperationToken, options).ConfigureAwait(false);
                    }
                    else
                    {
                        LastActionFailureMessage = "지원하지 않는 Picker Manual 공정입니다. process=" + processName;
                        return -1;
                    }

                    if (result != 0)
                    {
                        LastActionFailureMessage = "Picker Manual 공정 실패. side=" + side + ", process=" + processName + ", result=" + result;
                        return result;
                    }

                    SaveMachineRuntimeState("ManualPickerProcess:" + side + ":" + processName);
                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = "Picker Manual 공정이 취소되었습니다.";
                return -1;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "Picker Manual 공정 실행 중 예외가 발생했습니다. " + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>Manual Sequence Dialog에서 PickUp Z 세부 모션만 단독 테스트합니다. Material/DieMap 상태는 변경하지 않습니다.</summary>
        public async Task<int> RunManualPickerPickUpZMotionTestAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            int pickerNo)
        {
            try
            {
                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 PickUp Z 단독 테스트를 실행할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-Z-ALARM", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (IsManualBusy)
                {
                    LastActionFailureMessage = "다른 Manual 동작이 진행 중이라 PickUp Z 단독 테스트를 실행할 수 없습니다.";
                    return -1;
                }

                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto/Manual 시퀀스가 실행 중일 때는 PickUp Z 단독 테스트를 새로 시작할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-Z-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("RunManualPickerPickUpZMotionTestAsync"))
                    return -1;

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                using (BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "ManualPickerPickUpZMotionTest:" + side + ":" + pickerNo))
                {
                    var bus = new QMC.CDT320.Sequencing.SequenceSignalBus();
                    var context = new QMC.CDT320.Sequencing.MachineSequenceContext(
                        this,
                        bus,
                        new QMC.CDT320.Sequencing.SequenceResourceManager(),
                        _sequenceActivity);
                    var options = QMC.CDT320.Sequencing.PickerSequenceOptions.Default();
                    options.RunMode = QMC.CDT320.Sequencing.SequenceRunMode.Manual;
                    options.PickerNo = pickerNo;

                    int result = await new QMC.CDT320.Sequencing.PickerPickUpSequence(context, side)
                        .RunManualZMotionOnlyAsync(pickerNo, ManualOperationToken, options).ConfigureAwait(false);
                    if (result != 0)
                    {
                        LastActionFailureMessage = "PickUp Z 단독 테스트 실패. side=" + side + ", pickerNo=" + pickerNo + ", result=" + result;
                        return result;
                    }

                    SaveMachineRuntimeState("ManualPickerPickUpZMotionTest:" + side + ":" + pickerNo);
                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = "PickUp Z 단독 테스트가 취소되었습니다.";
                return -1;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "PickUp Z 단독 테스트 중 예외가 발생했습니다. " + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-Z-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        private bool ShouldSimulatePickerVisionResult(QMC.CDT320.Sequencing.PickerSequenceSide side)
        {
            try
            {
                QMC.CDT320.AppSettings settings = QMC.CDT320.AppSettingsStore.Current;
                if (settings != null &&
                    (settings.SimulationMode || settings.BypassHardware || !settings.UseAjin))
                    return true;

                if (_machine == null)
                    return false;

                if (side == QMC.CDT320.Sequencing.PickerSequenceSide.Front)
                {
                    return _machine.PickerFrontUnit != null &&
                           ((_machine.PickerFrontUnit.Setup != null && _machine.PickerFrontUnit.Setup.IsSimulationMode) ||
                            (_machine.PickerFrontUnit.Config != null && _machine.PickerFrontUnit.Config.IsSimulationMode));
                }

                return _machine.PickerRearUnit != null &&
                       ((_machine.PickerRearUnit.Setup != null && _machine.PickerRearUnit.Setup.IsSimulationMode) ||
                        (_machine.PickerRearUnit.Config != null && _machine.PickerRearUnit.Config.IsSimulationMode));
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        /// <summary>선택한 InputStage Die를 대상으로 Manual PickUp 테스트를 실행합니다. 실제 Material 상태를 Picker 보유 상태로 갱신합니다.</summary>
        public async Task<int> RunManualPickerSelectedDiePickUpAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            int pickerNo,
            string dieId)
        {
            try
            {
                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 선택 Die PickUp 테스트를 실행할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-DIE-ALARM", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (IsManualBusy)
                {
                    LastActionFailureMessage = "다른 Manual 동작이 진행 중이라 선택 Die PickUp 테스트를 실행할 수 없습니다.";
                    return -1;
                }

                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto/Manual 시퀀스가 실행 중일 때는 선택 Die PickUp 테스트를 새로 시작할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-DIE-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (string.IsNullOrWhiteSpace(dieId))
                {
                    LastActionFailureMessage = "PickUp 테스트 대상 Die가 선택되지 않았습니다.";
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("RunManualPickerSelectedDiePickUpAsync"))
                    return -1;

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                using (BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "ManualPickerSelectedDiePickUp:" + side + ":" + pickerNo))
                {
                    var bus = new QMC.CDT320.Sequencing.SequenceSignalBus();
                    var context = new QMC.CDT320.Sequencing.MachineSequenceContext(
                        this,
                        bus,
                        new QMC.CDT320.Sequencing.SequenceResourceManager(),
                        _sequenceActivity);
                    var options = QMC.CDT320.Sequencing.PickerSequenceOptions.Default();
                    options.RunMode = QMC.CDT320.Sequencing.SequenceRunMode.Manual;
                    options.PickerNo = pickerNo;
                    options.SimulateVisionResult = ShouldSimulatePickerVisionResult(side);

                    int result = await new QMC.CDT320.Sequencing.PickerPickUpSequence(context, side)
                        .RunManualSelectedDiePickUpAsync(dieId, pickerNo, ManualOperationToken, options)
                        .ConfigureAwait(false);
                    if (result != 0)
                    {
                        LastActionFailureMessage = "선택 Die PickUp 테스트 실패. side=" + side +
                                                   ", pickerNo=" + pickerNo +
                                                   ", die=" + dieId +
                                                   ", result=" + result;
                        return result;
                    }

                    SaveMachineRuntimeState("ManualPickerSelectedDiePickUp:" + side + ":" + pickerNo + ":" + dieId);
                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = "선택 Die PickUp 테스트가 취소되었습니다.";
                return -1;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "선택 Die PickUp 테스트 중 예외가 발생했습니다. " + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-MANUAL-PICKER-DIE-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        public async Task<int> RunManualPickerSelectedDiePrepareAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            int pickerNo,
            string dieId)
        {
            return await RunManualPickerSelectedDieStepAsync(
                side,
                pickerNo,
                dieId,
                false,
                "선택 Die PickUp 준비",
                "ManualPickerSelectedDiePrepare",
                "SEQ-MANUAL-PICKER-DIE-PREPARE").ConfigureAwait(false);
        }

        public async Task<int> RunManualPickerPreparedDiePickZAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            int pickerNo,
            string dieId)
        {
            return await RunManualPickerSelectedDieStepAsync(
                side,
                pickerNo,
                dieId,
                true,
                "준비 Die Pick Z 테스트",
                "ManualPickerPreparedDiePickZ",
                "SEQ-MANUAL-PICKER-DIE-Z").ConfigureAwait(false);
        }

        public async Task<int> RunManualPickerPreparedDiePickZStepAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            int pickerNo,
            string dieId,
            QMC.CDT320.Sequencing.PickerPickUpZManualStep step)
        {
            return await RunManualPickerSelectedDieStepAsync(
                side,
                pickerNo,
                dieId,
                false,
                "준비 Die Pick Z Step 테스트",
                "ManualPickerPreparedDiePickZStep:" + step,
                "SEQ-MANUAL-PICKER-DIE-Z-STEP",
                step).ConfigureAwait(false);
        }

        private async Task<int> RunManualPickerSelectedDieStepAsync(
            QMC.CDT320.Sequencing.PickerSequenceSide side,
            int pickerNo,
            string dieId,
            bool pickZ,
            string label,
            string saveReason,
            string alarmCodePrefix,
            QMC.CDT320.Sequencing.PickerPickUpZManualStep? pickZStep = null)
        {
            try
            {
                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 " + label + "를 실행할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, alarmCodePrefix + "-ALARM", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (IsManualBusy)
                {
                    LastActionFailureMessage = "다른 Manual 동작이 진행 중이라 " + label + "를 실행할 수 없습니다.";
                    return -1;
                }

                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto/Manual 시퀀스가 실행 중일 때는 " + label + "를 새로 시작할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, alarmCodePrefix + "-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (string.IsNullOrWhiteSpace(dieId))
                {
                    LastActionFailureMessage = label + " 대상 Die가 선택되지 않았습니다.";
                    return -1;
                }

                if (!EnsureMachineInitializedForRun(saveReason))
                    return -1;

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                using (BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, saveReason + ":" + side + ":" + pickerNo))
                {
                    var bus = new QMC.CDT320.Sequencing.SequenceSignalBus();
                    var context = new QMC.CDT320.Sequencing.MachineSequenceContext(
                        this,
                        bus,
                        new QMC.CDT320.Sequencing.SequenceResourceManager(),
                        _sequenceActivity);
                    var options = QMC.CDT320.Sequencing.PickerSequenceOptions.Default();
                    options.RunMode = QMC.CDT320.Sequencing.SequenceRunMode.Manual;
                    options.PickerNo = pickerNo;
                    options.SimulateVisionResult = ShouldSimulatePickerVisionResult(side);

                    var sequence = new QMC.CDT320.Sequencing.PickerPickUpSequence(context, side);
                    int result;
                    if (pickZStep.HasValue)
                    {
                        result = await sequence.RunManualPreparedDiePickZStepAsync(
                            dieId,
                            pickerNo,
                            pickZStep.Value,
                            ManualOperationToken,
                            options).ConfigureAwait(false);
                    }
                    else
                    {
                        result = pickZ
                            ? await sequence.RunManualPreparedDiePickZAsync(dieId, pickerNo, ManualOperationToken, options).ConfigureAwait(false)
                            : await sequence.RunManualSelectedDiePrepareAsync(dieId, pickerNo, ManualOperationToken, options).ConfigureAwait(false);
                    }
                    if (result != 0)
                    {
                        LastActionFailureMessage = label + " 실패. side=" + side +
                                                   ", pickerNo=" + pickerNo +
                                                   ", die=" + dieId +
                                                   ", result=" + result;
                        return result;
                    }

                    SaveMachineRuntimeState(saveReason + ":" + side + ":" + pickerNo + ":" + dieId);
                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = label + "가 취소되었습니다.";
                return -1;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = label + " 중 예외가 발생했습니다. " + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, alarmCodePrefix + "-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        public Task<int> RunManualInputLoadAsync()
        {
            return RunManualUnitProcessAsync(
                "INPUT LOAD",
                "SEQ-MANUAL-IN-LOAD",
                async delegate (QMC.CDT320.Sequencing.MachineSequenceContext context, CancellationToken token)
                {
                    var sequence = new QMC.CDT320.Sequencing.InputSequence(context);
                    sequence.Configure(QMC.CDT320.Sequencing.SequenceRunMode.Manual);
                    return await sequence.ExecuteWaferLoadingAsync(token, false, 0, QMC.CDT320.Sequencing.SequenceStartMode.Resume)
                        .ConfigureAwait(false);
                });
        }

        public Task<int> RunManualInputUnloadAsync()
        {
            return RunManualUnitProcessAsync(
                "INPUT UNLOAD",
                "SEQ-MANUAL-IN-UNLOAD",
                async delegate (QMC.CDT320.Sequencing.MachineSequenceContext context, CancellationToken token)
                {
                    var sequence = new QMC.CDT320.Sequencing.InputSequence(context);
                    sequence.Configure(QMC.CDT320.Sequencing.SequenceRunMode.Manual);
                    return await sequence.ExecuteCurrentWaferUnloadingAsync(token, false, 0, QMC.CDT320.Sequencing.SequenceStartMode.Resume)
                        .ConfigureAwait(false);
                });
        }

        public Task<int> RunManualOutputLoadAsync()
        {
            return RunManualUnitProcessAsync(
                "OUTPUT LOAD",
                "SEQ-MANUAL-OUT-LOAD",
                async delegate (QMC.CDT320.Sequencing.MachineSequenceContext context, CancellationToken token)
                {
                    var sequence = new QMC.CDT320.Sequencing.OutputSequence(context);
                    sequence.Configure(QMC.CDT320.Sequencing.SequenceRunMode.Manual);
                    return await sequence.ExecuteNextOutputLoadAsync(token, false, 0, QMC.CDT320.Sequencing.SequenceStartMode.Resume)
                        .ConfigureAwait(false);
                });
        }

        public Task<int> RunManualOutputUnloadAsync()
        {
            return RunManualUnitProcessAsync(
                "OUTPUT UNLOAD",
                "SEQ-MANUAL-OUT-UNLOAD",
                async delegate (QMC.CDT320.Sequencing.MachineSequenceContext context, CancellationToken token)
                {
                    var sequence = new QMC.CDT320.Sequencing.OutputSequence(context);
                    sequence.Configure(QMC.CDT320.Sequencing.SequenceRunMode.Manual);
                    return await sequence.ExecuteNextOutputUnloadAsync(token, false, 0, QMC.CDT320.Sequencing.SequenceStartMode.Resume)
                        .ConfigureAwait(false);
                });
        }

        private async Task<int> RunManualUnitProcessAsync(
            string processLabel,
            string alarmCodePrefix,
            Func<QMC.CDT320.Sequencing.MachineSequenceContext, CancellationToken, Task<int>> action)
        {
            try
            {
                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 " + processLabel + " Manual 공정을 실행할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, alarmCodePrefix + "-ALARM", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (IsManualBusy)
                {
                    LastActionFailureMessage = "다른 Manual 동작이 진행 중이라 " + processLabel + " Manual 공정을 실행할 수 없습니다.";
                    return -1;
                }

                if (IsSequenceRunning || _status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto/Manual 시퀀스가 실행 중일 때는 " + processLabel + " Manual 공정을 새로 시작할 수 없습니다.";
                    AlarmManager.Raise(AlarmSeverity.Error, alarmCodePrefix + "-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("RunManualUnitProcessAsync:" + processLabel))
                    return -1;

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                using (BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "ManualUnitProcess:" + processLabel))
                {
                    var bus = new QMC.CDT320.Sequencing.SequenceSignalBus();
                    var context = new QMC.CDT320.Sequencing.MachineSequenceContext(
                        this,
                        bus,
                        new QMC.CDT320.Sequencing.SequenceResourceManager(),
                        _sequenceActivity);

                    int result = await action(context, ManualOperationToken).ConfigureAwait(false);
                    if (result != 0)
                    {
                        LastActionFailureMessage = processLabel + " Manual 공정 실패. result=" + result;
                        return result;
                    }

                    SaveMachineRuntimeState("ManualUnitProcess:" + processLabel);
                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = processLabel + " Manual 공정이 취소되었습니다.";
                return -1;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = processLabel + " Manual 공정 실행 중 예외가 발생했습니다. " + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, alarmCodePrefix + "-EX", "MachineController", LastActionFailureMessage);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>Work CYCLE RUN 버튼에서 공정 전체 시퀀스를 1단계 진행합니다.</summary>
        public async Task<int> RunProcessSequenceStepAsync()
        {
            try
            {
                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 CYCLE RUN을 수행할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunProcessSequenceStep",
                        "Process sequence step run failed: alarm status is active. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-PROCESS-STEP-ALARM", "MachineController", LastActionFailureMessage);
                    Log("[SEQ] Process step failed: alarm status is active");
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("RunProcessSequenceStepAsync"))
                    return -1;

                if (IsSequenceRunning)
                {
                    if (ActiveSequenceRunMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    {
                        LastActionFailureMessage = "Auto Sequence 실행 중에는 CYCLE RUN Step을 수행할 수 없습니다.";
                        QMC.Common.Log.Write("Main", "SYSTEM", "RunProcessSequenceStep",
                            "Process sequence step run failed: auto sequence is running. - Failed");
                        AlarmManager.Raise(AlarmSeverity.Error, "SEQ-PROCESS-STEP-AUTO-RUNNING", "MachineController", LastActionFailureMessage);
                        Log("[SEQ] Process step failed: auto sequence is running");
                        return -1;
                    }

                    ManualStepAll();
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunProcessSequenceStep",
                        "Process sequence step gate released. - Ok");
                    return 0;
                }

                if (_status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto Running 상태에서는 CYCLE RUN Step을 시작할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunProcessSequenceStep",
                        "Process sequence step run failed: equipment auto running. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-PROCESS-STEP-RUNNING", "MachineController", LastActionFailureMessage);
                    Log("[SEQ] Process step failed: equipment auto running");
                    return -1;
                }

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                await StartSequenceAsync(
                    QMC.CDT320.Sequencing.SequenceRunOptions.ProcessStep()).ConfigureAwait(false);

                ManualStepAll();
                QMC.Common.Log.Write("Main", "SYSTEM", "RunProcessSequenceStep",
                    "Process sequence step mode started and first gate released. - Ok");
                Log("[SEQ] Process step start");
                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "Process sequence step run failed: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "RunProcessSequenceStep",
                    LastActionFailureMessage + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-PROCESS-STEP-EX", "MachineController", LastActionFailureMessage);
                Log("[SEQ] Process step failed: " + ex.Message);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>Work CYCLE RUN 버튼에서 Input 시퀀스를 1단계만 진행합니다.</summary>
        public async Task<int> RunInputSequenceStepAsync()
        {
            try
            {
                if (IsAutomaticStartTemporarilyDisabled())
                    return -1;

                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 CYCLE RUN을 수행할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunInputSequenceStep",
                        "Input sequence step run failed: alarm status is active. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-STEP-ALARM", "MachineController", LastActionFailureMessage);
                    Log("[SEQ] Input step failed: alarm status is active");
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("RunInputSequenceStepAsync"))
                    return -1;

                if (IsSequenceRunning)
                {
                    if (ActiveSequenceRunMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    {
                        LastActionFailureMessage = "Auto Sequence 실행 중에는 CYCLE RUN Step을 수행할 수 없습니다.";
                        QMC.Common.Log.Write("Main", "SYSTEM", "RunInputSequenceStep",
                            "Input sequence step run failed: auto sequence is running. - Failed");
                        AlarmManager.Raise(AlarmSeverity.Error, "SEQ-STEP-AUTO-RUNNING", "MachineController", LastActionFailureMessage);
                        Log("[SEQ] Input step failed: auto sequence is running");
                        return -1;
                    }

                    ManualStep(QMC.CDT320.Sequencing.SequenceUnitKind.InputLoader);
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunInputSequenceStep",
                        "Input sequence step gate released. - Ok");
                    return 0;
                }

                if (_status == EquipmentStatus.AutoRunning)
                {
                    LastActionFailureMessage = "Auto Running 상태에서는 CYCLE RUN Step을 시작할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "RunInputSequenceStep",
                        "Input sequence step run failed: equipment auto running. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Error, "SEQ-STEP-RUNNING", "MachineController", LastActionFailureMessage);
                    Log("[SEQ] Input step failed: equipment auto running");
                    return -1;
                }

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                await StartSingleUnitAsync(
                    QMC.CDT320.Sequencing.SequenceUnitKind.InputLoader,
                    QMC.CDT320.Sequencing.SequenceRunMode.Step).ConfigureAwait(false);

                ManualStep(QMC.CDT320.Sequencing.SequenceUnitKind.InputLoader);
                QMC.Common.Log.Write("Main", "SYSTEM", "RunInputSequenceStep",
                    "Input sequence step mode started and first gate released. - Ok");
                Log("[SEQ] Input step start");
                return 0;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "Input sequence step run failed: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "RunInputSequenceStep",
                    LastActionFailureMessage + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-STEP-EX", "MachineController", LastActionFailureMessage);
                Log("[SEQ] Input step failed: " + ex.Message);
                SetStatus(EquipmentStatus.Alarm);
                return -1;
            }
            finally
            {
            }
        }

        // ------------------------------------------------------------------
        // Legacy cycle compatibility area
        // 현재 자동 운전은 StartSequenceAsync/AutoSequenceCoordinator 계열이 기준입니다.
        // 아래 CycleRunAsync/DoOneDieAsync 계열은 Form1 및 일부 구형 경로에서 아직 참조하므로
        // 바로 삭제하지 않고 아래쪽에 모아 둡니다. 참조 제거가 확인되면 삭제 검토 대상입니다.
        // ------------------------------------------------------------------

        public async Task CycleRunAsync(int totalDies = -1)
        {
            if (!EnsureMachineInitializedForRun("CycleRunAsync"))
                return;

            if (_status == EquipmentStatus.AutoRunning) { Log("[CYCLE] already running"); return; }
            // Ready/Running 외에도 Stopped에서 재시작을 허용합니다(CYCLE STOP 재개).
            if (_status != EquipmentStatus.Ready &&
                _status != EquipmentStatus.ManualRunning &&
                _status != EquipmentStatus.Stopped)
            {
                Log("[CYCLE] Init is required before run. status=" + _status);
                return;
            }
            if (_status == EquipmentStatus.Stopped)
            {
                Log("[CYCLE] Stopped 상태에서 재개");
                // CYCLE STOP / STOP은 Servo OFF를 하지 않으므로 Servo 재시작은 불필요합니다.
            }

            bool resumeCycle = _cycleResumePending &&
                               CycleTotal > 0 &&
                               CycleDone > 0 &&
                               CycleDone < CycleTotal;

            // R3 다이맵 모드: Input 다이맵의 활성 다이 수를 totalDies로 사용합니다.
            try { EnsureDieMaps(); } catch { }
            if (UseDieMapMode && _inputDieMap != null)
            {
                int active = 0;
                foreach (var e in _inputDieMap.Entries) if (e.IsTarget) active++;
                if (totalDies <= 0 || totalDies > active) totalDies = active;
                Log("[CYCLE] DieMap mode. Input wafer active dies=" + active + ", process=" + totalDies);
            }
            else if (totalDies <= 0)
            {
                totalDies = 10;   // legacy default
            }
            _cycleCts = new CancellationTokenSource();
            _cycleStopRequested = false;

            string lotId;
            if (resumeCycle)
            {
                totalDies = CycleTotal;
                lotId = LotStorage.ActiveLot != null
                    ? LotStorage.ActiveLot.LotID
                    : "LOT-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                if (LotStorage.ActiveLot == null)
                    LotStorage.OpenLot(lotId, "default", totalDies);
                _cycleResumePending = false;
                Log("[CYCLE] resume from done=" + CycleDone + "/" + CycleTotal + ", lot=" + lotId);
            }
            else
            {
                CycleTotal = totalDies; CycleDone = 0; GoodCount = 0; NgCount = 0;
                lotId = "LOT-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                LotStorage.OpenLot(lotId, "default", totalDies);
            }
            // 작업 시간/UPH 통계 엔진: 부하 시간 시작(사이클 Start 기준) + 작업 변수 리셋.
            try { Stats.BeginLot(lotId, totalDies); } catch { }
            SetStatus(EquipmentStatus.AutoRunning);
            Log("[CYCLE] Start (total=" + totalDies + ", lot=" + lotId + ")");

            // Stage 41: SECS/HSMS 사이클 시작 이벤트.
            try { SecsHost?.RaiseEvent("CycleStart", lotId, totalDies.ToString()); } catch { }

            // Stage 45: Tower Lamp 운전 상태 표시.
            try { _machine.OpPanelUnit?.TowerLampRunning(); } catch { }

            try
            {
                if (!resumeCycle)
                {
                    // 사이클 시작 시 첫 웨이퍼를 LotPort에서 진입시킵니다.
                    bool loaded = await LoadNextWaferAsync();
                    if (!loaded)
                    {
                        Log("[CYCLE] First wafer load from lot port failed. Continue cycle in dry/default mode.");
                    }
                }

                // 1 사이클 = PickersPerCycle개 다이 동시 처리(기본 4 picker).
                int pickers = System.Math.Min(System.Math.Max(PickersPerCycle, 1), 4);
                int totalCycles = (totalDies + pickers - 1) / pickers;
                Log("[CYCLE] " + totalCycles + " cycles 횞 " + pickers + " pickers = " + totalDies + " dies");

                int startCycle = resumeCycle ? System.Math.Max(0, CycleDone / pickers) : 0;
                // 작업 시간 통계: 1 사이클 소요 ms 측정용 Stopwatch (루프 진입 전 1회).
                var swCycle = System.Diagnostics.Stopwatch.StartNew();
                for (int cyc = startCycle; cyc < totalCycles; cyc++)
                {
                    if (_cycleStopRequested) break;
                    int dieBase = cyc * pickers;
                    int diesInCycle = System.Math.Min(pickers, totalDies - dieBase);
                    int goodBefore = GoodCount;
                    int ngBefore = NgCount;
                    await DoOneDieAsync(cyc, totalCycles, _cycleCts.Token);  // 사이클 인덱스와 전체 수 전달.
                    CycleDone = System.Math.Min(totalDies, (cyc + 1) * pickers);
                    // 통계 엔진에 1 사이클 결과를 먹인다(기존 카운트는 그대로 유지).
                    long cycleMs = swCycle.ElapsedMilliseconds;
                    swCycle.Restart();
                    int goodDelta = System.Math.Max(0, GoodCount - goodBefore);
                    int ngDelta = System.Math.Max(0, NgCount - ngBefore);
                    try { Stats.OnCycleCompleted(diesInCycle, goodDelta, ngDelta, cycleMs); } catch { }
                    RaiseProgress();
                    if (_cycleStopRequested)
                    {
                        Log("[CYCLE STOP] 현재 사이클 완료 후 정지합니다. done=" + CycleDone + "/" + CycleTotal);
                        break;
                    }
                    // R3 Manual 모드: 1 사이클 처리 후 자동 정지.
                    if (CycleMode == CycleMode.Manual)
                    {
                        Log("[CYCLE] Manual mode. Stop after one die batch. done=" + CycleDone);
                        break;
                    }
                }

                if (_cycleStopRequested)
                {
                    _cycleResumePending = true;
                    Log("[CYCLE STOP] paused at done=" + CycleDone + "/" + CycleTotal);
                    try { _machine.OpPanelUnit?.TowerLampOff(); } catch { }
                    SetStatus(EquipmentStatus.CycleStopped);
                    return;
                }

                // 사이클 종료 후 피더 후퇴.
                await RetractCurrentWaferAsync();

                // Stage 28: InputStage 웨이퍼 언로드.
                await UnloadInputStageWaferAsync();

                Log("[CYCLE] 완료 (good=" + GoodCount + ", ng=" + NgCount + ")");
                LotStorage.CloseLot(aborted: false);
                try { Stats.EndLot(); } catch { }
                _cycleResumePending = false;
                _cycleStopRequested = false;
                // Stage 45: Tower Lamp OFF(사이클 정상 완료).
                try { _machine.OpPanelUnit?.TowerLampOff(); } catch { }
                // Stage 41: SECS/HSMS 사이클 완료 이벤트(Yield 포함).
                try
                {
                    double yield = totalDies > 0 ? (double)GoodCount / totalDies * 100 : 0;
                    SecsHost?.RaiseEvent("CycleEnd", lotId, GoodCount.ToString(), NgCount.ToString(),
                        yield.ToString("F2"));
                }
                catch { }
                SetStatus(EquipmentStatus.Ready);
            }
            catch (OperationCanceledException)
            {
                if (_cycleStopRequested)
                {
                    _cycleResumePending = true;
                    Log("[CYCLE STOP] paused at done=" + CycleDone + "/" + CycleTotal);
                    try { _machine.OpPanelUnit?.TowerLampOff(); } catch { }
                    SetStatus(EquipmentStatus.Stopped);
                }
                else
                {
                    // Skipped 카운트 = 시작 totalDies - 실제 처리된 ProcessedDies.
                    int skipped = System.Math.Max(0, totalDies - CycleDone);
                    if (LotStorage.ActiveLot != null)
                    {
                        LotStorage.ActiveLot.SkippedCount = skipped;
                    }
                    Log("[CYCLE] 중단 (good=" + GoodCount + ", ng=" + NgCount + ", skipped=" + skipped + ")");
                    // Tower Lamp OFF(사이클 중단).
                    try { _machine.OpPanelUnit?.TowerLampOff(); } catch { }
                    LotStorage.CloseLot(aborted: true);
                    try { Stats.EndLot(); } catch { }
                    SetStatus(EquipmentStatus.Stopped);
                }
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "CYCLE-EX", "MachineController", ex.Message);
                Log("[CYCLE ERROR] " + ex.Message);
                LotStorage.CloseLot(aborted: true);
                try { Stats.EndLot(); } catch { }
                SetStatus(EquipmentStatus.Alarm);
            }
        }

        /// <summary>CYCLE STOP: 현재 사이클만 중단합니다. 개별 축 즉시 정지는 수행하지 않습니다.</summary>
        public Task CycleStopAsync()
        {
            if (_coordinator != null && _coordinatorTask != null && !_coordinatorTask.IsCompleted)
                return RequestCycleStopSequenceAsync();

            if (_cycleCts != null)
            {
                _cycleStopRequested = true;
                _cycleResumePending = true;
                Log("[CYCLE STOP] requested. 현재 사이클 완료 후 정지합니다.");
            }
            return Task.CompletedTask;
        }

        // ------------------------------------------------------------------
        // Legacy one-cycle work helpers
        // ------------------------------------------------------------------

        /// <summary>웨이퍼당 가공할 다이 수. 1400 = 300mm 웨이퍼 기본 처리 슬롯.</summary>
        public int DiesPerWafer { get; set; } = 1400;

        /// <summary>Stage 33: N개 다이마다 Collet Cleaning 시퀀스를 수행합니다(0이면 비활성).</summary>
        public int DiesPerColletClean { get; set; } = 200;

        /// <summary>Stage 39: 한 번에 동시 픽업할 picker 수(1~4). 기본은 4 picker 동시 처리.</summary>
        public int PickersPerCycle { get; set; } = 4;

        /// <summary>Stage 40: Dual Arm 모드. 짝수 다이는 LeftArm, 홀수 다이는 RightArm으로 분배합니다.</summary>
        public bool DualArmMode { get; set; } = false;

        /// <summary>Stage 41: SecsHost 참조(사이클 이벤트 송신용).</summary>
        public QMC.CDT320.Secs.SecsHost SecsHost { get; set; }

        /// <summary>
        /// 1 사이클 = PickersPerCycle(기본 4)개 다이 동시 처리.
        /// cycleIdx는 사이클 번호(0..totalCycles-1)이며 실제 다이 번호는 cycleIdx*pickers부터 시작합니다.
        /// </summary>
        // ------------------------------------------------------------------
        // Stage wafer vision capture helper
        // Arm이 안전 위치를 유지하는 동안 현재 사이클의 다이를 촬영합니다.
        // ------------------------------------------------------------------
        private static double ResolveAxisDefaultVelocity(BaseAxis axis)
        {
            // DefaultVelocity 기반 일반 이동 속도. 전체 퍼센트 스케일을 적용한다.
            return MotionSpeedScale.ApplyDefaultVelocityScale(
                axis != null && axis.Config != null && axis.Config.DefaultVelocity > 0.0
                    ? axis.Config.DefaultVelocity
                    : 100.0);
        }

        private static int ResolveAxisMoveTimeout(BaseAxis axis)
        {
            return axis != null && axis.Setup != null && axis.Setup.MoveTimeoutMs > 0
                ? axis.Setup.MoveTimeoutMs
                : 10000;
        }

        private static double ResolveAxisInPositionTolerance(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;
        }

        private static bool IsAxisNotHomedOrAtHomePosition(BaseAxis axis, string axisName, out string reason)
        {
            reason = string.Empty;
            if (axis == null)
                return true;

            if (axis.IsMoving)
            {
                reason = axisName + " is moving. actual=" + axis.ActualPosition.ToString("0.###");
                return false;
            }

            if (!axis.IsHomeDone)
                return true;

            const double homePosition = 0.0;
            double tolerance = ResolveAxisInPositionTolerance(axis);
            if (Math.Abs(axis.ActualPosition - homePosition) <= tolerance)
                return true;

            reason = axisName +
                     " homeDone=ON but not at Home position. target=0, actual=" +
                     axis.ActualPosition.ToString("0.###") +
                     ", tolerance=" + tolerance.ToString("0.###");
            return false;
        }

        private static bool IsAxisAtHomeOrTeachingAvoid(BaseAxis axis, Func<bool> isTeachingAvoid)
        {
            if (axis == null)
                return true;

            if (Math.Abs(axis.ActualPosition) <= ResolveAxisInPositionTolerance(axis))
                return true;

            return isTeachingAvoid != null && isTeachingAvoid();
        }

        private static bool IsExpanderZHomeAvoidOrProcess(InputStageUnit stage)
        {
            if (stage == null || stage.ExpanderZ == null)
                return true;

            double tolerance = ResolveAxisInPositionTolerance(stage.ExpanderZ);
            double actual = stage.ExpanderZ.ActualPosition;
            if (Math.Abs(actual) <= tolerance)
                return true;

            StageAxisPositions waferZ = stage.Recipe != null ? stage.Recipe.WaferZ : null;
            if (waferZ == null)
                return false;

            return Math.Abs(actual - waferZ.AvoidPosition) <= tolerance ||
                   Math.Abs(actual - waferZ.ProcessPosition) <= tolerance;
        }

        private static BaseAxis ResolveFrontPickerAxis(PickerFrontUnit picker, PickerAxis axis)
        {
            if (picker == null)
                return null;

            switch (axis)
            {
                case PickerAxis.PickerZ0: return picker.PickerZ0;
                case PickerAxis.PickerZ1: return picker.PickerZ1;
                case PickerAxis.PickerZ2: return picker.PickerZ2;
                case PickerAxis.PickerZ3: return picker.PickerZ3;
                case PickerAxis.PickerX: return picker.PickerX;
                case PickerAxis.PickerY: return picker.PickerY;
                case PickerAxis.PickerT0: return picker.PickerT0;
                case PickerAxis.PickerT1: return picker.PickerT1;
                case PickerAxis.PickerT2: return picker.PickerT2;
                case PickerAxis.PickerT3: return picker.PickerT3;
                default: return null;
            }
        }

        private static BaseAxis ResolveRearPickerAxis(PickerRearUnit picker, PickerAxis axis)
        {
            if (picker == null)
                return null;

            switch (axis)
            {
                case PickerAxis.PickerZ0: return picker.PickerZ0;
                case PickerAxis.PickerZ1: return picker.PickerZ1;
                case PickerAxis.PickerZ2: return picker.PickerZ2;
                case PickerAxis.PickerZ3: return picker.PickerZ3;
                case PickerAxis.PickerX: return picker.PickerX;
                case PickerAxis.PickerY: return picker.PickerY;
                case PickerAxis.PickerT0: return picker.PickerT0;
                case PickerAxis.PickerT1: return picker.PickerT1;
                case PickerAxis.PickerT2: return picker.PickerT2;
                case PickerAxis.PickerT3: return picker.PickerT3;
                default: return null;
            }
        }

        private static Task<AxisMoveWaitResult> WaitAxisMoveDoneInPositionAsync(BaseAxis axis, double target)
        {
            return AxisMoveWaiter.WaitMoveDoneInPositionAsync(
                axis,
                target,
                ResolveAxisInPositionTolerance(axis),
                ResolveAxisMoveTimeout(axis),
                0);
        }

        private async Task<int> MoveAxisCommandAndWaitAsync(BaseAxis axis, double target, double velocity, bool useSharedRailX)
        {
            try
            {
                if (axis == null)
                    return -1;

                if (DryRun)
                {
                    Log($"[DRYRUN] skip move {axis.Name} target={target:F3} (vel={velocity:F0})");
                    return 0;
                }

                int moveResult = useSharedRailX
                    ? await SharedRailXMotionRuntime.MoveAxisAsync(axis, target, velocity).ConfigureAwait(false)
                    : await axis.MoveAbsoluteAsync(target, velocity).ConfigureAwait(false);
                if (moveResult != 0 || axis.IsAlarm)
                {
                    Log("[MOVE] " + BuildAxisMotionFailureMessage(axis, "Move failed", moveResult));
                    return moveResult != 0 ? moveResult : -1;
                }

                AxisMoveWaitResult waitResult = await WaitAxisMoveDoneInPositionAsync(axis, target).ConfigureAwait(false);
                if (!waitResult.Success)
                {
                    Log("[MOVE] Move wait/in-position failed. " + AxisMoveWaiter.FormatResult(waitResult, axis.Name));
                    return -1;
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log("[MOVE] 축 이동 명령/완료 대기 중 예외가 발생했습니다. axis=" +
                    (axis != null ? axis.Name : "-") + ", target=" + target + ", error=" + ex.Message);
                return -1;
            }
            finally
            {
                Log("[MOVE] MoveAxisCommandAndWaitAsync finished. axis=" +
                    (axis != null ? axis.Name : "-") + ", target=" + target);
            }
        }

        private int CheckMoveResult(string description, int result)
        {
            try
            {
                if (result == 0)
                    return 0;

                string message = description + " 이동 실패. result=" + result;
                Log("[MOVE] " + message);
                AlarmManager.Raise(AlarmSeverity.Error, "MOVE-RESULT", "MachineController", message);
                return result != 0 ? result : -1;
            }
            catch (Exception ex)
            {
                Log("[MOVE] 이동 결과 확인 중 예외가 발생했습니다. description=" +
                    description + ", error=" + ex.Message);
                return -1;
            }
            finally
            {
                Log("[MOVE] CheckMoveResult finished. description=" + description);
            }
        }

        private int CheckMoveResults(string description, int[] results)
        {
            try
            {
                if (results == null || results.Length == 0)
                {
                    string emptyMessage = description + " 이동 결과가 없습니다.";
                    Log("[MOVE] " + emptyMessage);
                    AlarmManager.Raise(AlarmSeverity.Error, "MOVE-RESULT", "MachineController", emptyMessage);
                    return -1;
                }

                for (int i = 0; i < results.Length; i++)
                {
                    if (results[i] == 0)
                        continue;

                    string message = description + " 이동 실패. index=" + i + ", result=" + results[i];
                    Log("[MOVE] " + message);
                    AlarmManager.Raise(AlarmSeverity.Error, "MOVE-RESULT", "MachineController", message);
                    return results[i] != 0 ? results[i] : -1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Log("[MOVE] 병렬 이동 결과 확인 중 예외가 발생했습니다. description=" +
                    description + ", error=" + ex.Message);
                return -1;
            }
            finally
            {
                Log("[MOVE] CheckMoveResults finished. description=" + description);
            }
        }

        private void ThrowIfMoveFailed(string description, int result)
        {
            try
            {
                int checkedResult = CheckMoveResult(description, result);
                if (checkedResult != 0)
                    throw new InvalidOperationException(description + " 이동 실패. result=" + checkedResult);
            }
            catch
            {
                throw;
            }
            finally
            {
                Log("[MOVE] ThrowIfMoveFailed finished. description=" + description);
            }
        }

        private void ThrowIfMoveFailed(string description, int[] results)
        {
            try
            {
                int checkedResult = CheckMoveResults(description, results);
                if (checkedResult != 0)
                    throw new InvalidOperationException(description + " 이동 실패. result=" + checkedResult);
            }
            catch
            {
                throw;
            }
            finally
            {
                Log("[MOVE] ThrowIfMoveFailed finished. description=" + description);
            }
        }

        private async Task<(double X, double Y)[]> CaptureWaferForCycleAsync(
            int cycleIdx, int pickers, CancellationToken ct)
        {
            var stage = _machine.InputStageUnit;
            var offsets = new (double X, double Y)[pickers];
            int dieBase = cycleIdx * pickers;

            try { stage.CameraX?.ServoOn(); stage.StageY?.ServoOn(); } catch { }

            bool wafer = VisionComm.VisionHub.Wafer != null
                      && VisionComm.VisionHub.Wafer.IsConnected;
            int seqLen = _inputPickupSequence?.Count ?? 0;

            Log($"[CAPTURE] Cycle {cycleIdx + 1}: start capturing {pickers} dies (dieBase={dieBase})");

            for (int p = 0; p < pickers; p++)
            {
                if (ct.IsCancellationRequested) break;
                int seqIdx = dieBase + p;
                if (seqIdx < 0 || seqIdx >= seqLen)
                {
                    Log($"[CAPTURE p{p}] seqIdx {seqIdx} out of range (seqLen={seqLen}). Skip.");
                    offsets[p] = (0, 0);
                    continue;
                }
                var d = _inputPickupSequence[seqIdx];

                // CameraX / StageY 동시 이동(각 die의 X/Y 좌표로 정렬).
                //   CameraX  = CameraOriginX + WaferAlignOffsetX + die.X
                //   StageY   = StageYTeachPosition + WaferAlignOffsetY + die.Y
                stage.Recipe.EnsurePositionObjects();
                double camXTarget = stage.Recipe.VisionX.ReadyPosition
                                  + stage.WaferAlignOffsetX
                                  + d.PosX;
                double stageYTarget = stage.Recipe.WaferY.ReadyPosition
                                    + stage.WaferAlignOffsetY
                                    + d.PosY;
                try
                {
                    int[] moveResults = await Task.WhenAll(
                        MoveAxisCommandAndWaitAsync(stage.CameraX, camXTarget, ResolveAxisDefaultVelocity(stage.CameraX), true),
                        MoveAxisCommandAndWaitAsync(stage.StageY, stageYTarget, ResolveAxisDefaultVelocity(stage.StageY), false)
                    );
                    ThrowIfMoveFailed("Wafer capture CameraX/StageY", moveResults);
                }
                catch (Exception ex)
                {
                    Log($"[CAPTURE-XY p{p}] ex: " + ex.Message);
                    throw;
                }

                Log($"[CAPTURE p{p}] Die seq#{seqIdx} grid({d.DieMapX},{d.DieMapY}) " +
                    $"wafer({d.PosX:F2},{d.PosY:F2}) -> CamX={camXTarget:F2}, StageY={stageYTarget:F2}");

                if (!wafer)
                {
                    offsets[p] = (0, 0);
                    // wafer 미연결 상태에서도 simulator flash는 송신합니다(시각 확인용).
                    SimulatorBridge.Instance?.CameraExposeFlash("WAFER");
                    await Task.Delay(200, ct).ConfigureAwait(false);
                    continue;
                }

                try
                {
                    SimulatorBridge.Instance?.CameraExposeFlash("WAFER");
                    var m = await VisionComm.VisionHub.Wafer.MatchAsync(
                        "DieFinder", dieBase + p, 1500);
                    if (m.Success && m.Score >= 0.7)
                    {
                        offsets[p] = (0, 0);
                        Log($"[CAPTURE p{p}] OK score={m.Score:F2} offset=({offsets[p].X:F3},{offsets[p].Y:F3})mm");
                    }
                    else
                    {
                        offsets[p] = (0, 0);
                        Log($"[CAPTURE p{p}] NG match (score={m.Score:F2}). offset=0");
                    }
                }
                catch (Exception ex)
                {
                    offsets[p] = (0, 0);
                    Log($"[CAPTURE p{p}] vision ex: " + ex.Message);
                }

                // 다음 다이로 이동하기 전 150ms 대기하여 flash가 시각적으로 구분되도록 합니다.
                await Task.Delay(150, ct).ConfigureAwait(false);
            }

            Log($"[CAPTURE] Cycle {cycleIdx + 1}: capture complete.");
            return offsets;
        }

        private async Task DoOneDieAsync(int cycleIdx, int totalCycles, CancellationToken ct)
        {
            int pickers = System.Math.Min(System.Math.Max(PickersPerCycle, 1), 4);
            int dieBase = cycleIdx * pickers;
            int index = dieBase;   // 기존 코드 호환용 첫 다이 번호.

            // StepRun 게이트: 사용자 콜백이 false이면 사이클을 종료합니다.
            if (StepRun && StepRunGate != null)
            {
                Log($"[STEPRUN] waiting for user gate (cycle {cycleIdx + 1})...");
                bool proceed = false;
                try { proceed = StepRunGate.Invoke(cycleIdx); } catch { }
                if (!proceed)
                {
                    Log("[STEPRUN] user denied. Stopping cycle.");
                    throw new OperationCanceledException("StepRun gate denied");
                }
            }

            // Stage 26: DiesPerWafer마다 다음 카세트 슬롯으로 진행합니다.
            // cycleIdx == 0은 CycleRunAsync에서 LoadNextWaferAsync를 이미 호출했으므로 건너뜁니다.
            if (dieBase > 0 && DiesPerWafer > 0 && dieBase % DiesPerWafer == 0 && InputWaferAtExchange)
            {
                Log($"[LOTPORT] {DiesPerWafer} dies complete. Move to next slot.");
                await RetractCurrentWaferAsync();
                bool nextOk = await LoadNextWaferAsync();
                if (!nextOk)
                {
                    Log("[LOTPORT] No next wafer. Continue cycle in dry/default mode.");
                }
            }

            // 4개 다이 객체 생성 + Material 등록 + JobOrder 생성.
            var dies = new Die[pickers];
            var pickJobs = new JobOrder[pickers];
            var mapEntries = new QMC.CDT320.DieMaps.DieMapEntry[pickers];
            for (int p = 0; p < pickers; p++)
            {
                dies[p] = new Die { Uid = "DIE-" + DateTime.Now.Ticks + "-" + (dieBase + p) };
                MaterialStorage.AddDie(dies[p]);
                pickJobs[p] = new JobOrder { Type = JobType.Pick, DieUid = dies[p].Uid };
            }

            // Stage 28/61: DieMap 모드 + 픽업 순서 옵션 적용.
            // PickupSequenceGenerator가 만든 정렬 순서에서 dieBase ~ dieBase+pickers-1 범위를 사용합니다.
            int row, col;
            if (UseDieMapMode && _inputDieMap != null)
            {
                // 순서가 비어 있으면(옵션 변경 후 RebuildPickupSequence 미호출 등) 재생성합니다.
                if (_inputPickupSequence == null || _inputPickupSequence.Count == 0)
                    RebuildPickupSequence();

                int seqLen = _inputPickupSequence?.Count ?? 0;
                for (int i = 0; i < pickers; i++)
                {
                    int seqIdx = dieBase + i;
                    if (seqIdx < 0 || seqIdx >= seqLen) break;
                    var e = _inputPickupSequence[seqIdx];
                    mapEntries[i] = e;
                    if (!string.IsNullOrWhiteSpace(e.DieUid) &&
                        !string.Equals(dies[i].Uid, e.DieUid, StringComparison.OrdinalIgnoreCase))
                    {
                        MaterialStorage.RemoveDie(dies[i].Uid);
                        dies[i].Uid = e.DieUid;
                        MaterialStorage.AddDie(dies[i]);
                        pickJobs[i].DieUid = dies[i].Uid;
                    }
                    else if (string.IsNullOrWhiteSpace(e.DieUid))
                    {
                        e.DieUid = dies[i].Uid;
                    }
                    dies[i].WaferIndexX = e.DieMapX;
                    dies[i].WaferIndeY = e.DieMapY;
                    dies[i].X = e.PosX;
                    dies[i].Y = e.PosY;
                }
                // InputStage 이동은 첫 picker 대상 다이를 기준으로 합니다.
                row = dies[0].WaferIndeY;
                col = dies[0].WaferIndexX;
            }
            else
            {
                int colsAssumed = 10;
                row = dieBase / colsAssumed;
                col = dieBase % colsAssumed;
            }

            for (int p = 0; p < pickers; p++)
            {
                JobQueue.Enqueue(pickJobs[p]);
                JobQueue.MarkRunning(pickJobs[p]);
            }

            // 대표 1개 다이(로그/통계용).
            var die = dies[0];
            var pickJob = pickJobs[0];
            await MoveInputStageToDieAsync(row, col);

            // Stage 40: Dual Arm 모드. 짝수 idx는 LeftArm, 홀수 idx는 RightArm.
            dynamic front = (DualArmMode && (index % 2 == 1))
                        ? (object)_machine.PickerRearUnit
                        : _machine.PickerFrontUnit;

            front.ArmX.ServoOn();
            front.ArmY.ServoOn();
            // 4 picker 모두 ServoOn (PickerZ + PickerT)
            for (int p = 0; p < pickers; p++)
            {
                front.Pickers[p].PickerZ.ServoOn();
                front.Pickers[p].PickerT.ServoOn();
            }

            // Stage 58: 운영 통계. Front collet + Needle 4 picker 사용.
            Collet1UseCount += pickers;
            NeedleUseCount += pickers;

            // 변수 선언 + Servo ON.
            bool[] pickupOk = new bool[pickers];
            bool[] inspPass = new bool[pickers];
            var dieOffsets = new (double X, double Y)[pickers];
            var visionOffsets = new (double X, double Y)[pickers];
            for (int pi = 0; pi < pickers; pi++) inspPass[pi] = true;

            var stage = _machine.InputStageUnit;
            var ej = stage.EjectPinZ;
            ej?.ServoOn();
            stage.StageY?.ServoOn();
            stage.NeedleBlockX?.ServoOn();
            stage.CameraX?.ServoOn();

            // Stage 61: 파이프라인 wafer 비전 결과 수신.
            // 1) ArmY를 AvoidPosition으로 보내 wafer 영역 밖에서 대기합니다.
            // 2) Pending capture가 있으면 await, 없으면 동기 캡처합니다.
            //   3) await capture completion.
            // 4) ArmY PickupPosition + ArmX ArmInputPositionX 동시 진입.
            try
            {
                int armYAvoidResult = await MoveAxisCommandAndWaitAsync(
                    front.ArmY,
                    front.GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                    ResolveAxisDefaultVelocity(front.ArmY),
                    false);
                ThrowIfMoveFailed("FrontPickerY avoid before pickup", armYAvoidResult);
            }
            catch (Exception ex)
            {
                Log("[ARM-Y avoid] ex: " + ex.Message);
                PickFailCount++;
                return;
            }

            (double X, double Y)[] capturedOffsets;
            if (_pendingCaptureTask != null && _pendingCaptureCycleIdx == cycleIdx)
            {
                Log($"[PIPELINE] Cycle {cycleIdx + 1}: waiting for previous wafer capture...");
                capturedOffsets = await _pendingCaptureTask;
                _pendingCaptureTask = null;
                _pendingCaptureCycleIdx = -1;
            }
            else
            {
                Log($"[PIPELINE] Cycle {cycleIdx + 1}: capture wafer synchronously.");
                capturedOffsets = await CaptureWaferForCycleAsync(cycleIdx, pickers, ct);
            }

            for (int p = 0; p < pickers && p < capturedOffsets.Length; p++)
            {
                visionOffsets[p] = capturedOffsets[p];
                dieOffsets[p].X += visionOffsets[p].X;
                dieOffsets[p].Y += visionOffsets[p].Y;
            }

            // 8) ArmY pickup + ArmX input position.
            double pickX = front.GetPickerTeachingPosition(PickerAxis.PickerX, "PickPosition");
            double pickY = front.GetPickerTeachingPosition(PickerAxis.PickerY, "PickPosition");
            try
            {
                int[] entryResults = await Task.WhenAll(
                    MoveAxisCommandAndWaitAsync(front.ArmX, pickX, ResolveAxisDefaultVelocity(front.ArmX), true),
                    MoveAxisCommandAndWaitAsync(front.ArmY, pickY, ResolveAxisDefaultVelocity(front.ArmY), false)
                );
                ThrowIfMoveFailed("FrontPicker X/Y pickup entry", entryResults);
            }
            catch (Exception ex)
            {
                Log("[ARM-XY entry] ex: " + ex.Message);
                JobQueue.MarkFailed(pickJob, "ArmX/Y entry exception");
                NgCount++; PickFailCount++;
                return;
            }
            ct.ThrowIfCancellationRequested();

            // B. PICK 루프(picker 0부터 순차).
            for (int p = 0; p < pickers; p++)
            {
                if (!inspPass[p]) { pickupOk[p] = false; continue; }  // vision NG picker skip

                var d = dies[p];
                var picker = front.Pickers[p];
                var vo = visionOffsets[p];
                try
                {
                    front.Config.EnsureArrays();
                    PickerAlignOffset pickerOffset = front.GetRuntimePickerOffset(p) ?? new PickerAlignOffset();

                    // 3축 동시 이동.
                    double armXTarget =
                        front.GetPickerTeachingPosition(PickerAxis.PickerX, "PickPosition")
                        + pickerOffset.AlignOffsetX
                        + stage.WaferAlignOffsetX
                        + picker.Setup.ColletOffsetX
                        + d.X;
                    stage.Recipe.EnsurePositionObjects();
                    double stageYTarget =
                        stage.Recipe.WaferY.ReadyPosition
                        + stage.WaferAlignOffsetY
                        + d.Y
                        + vo.Y;
                    double needleXTarget =
                        stage.Recipe.NeedleX.ReadyPosition
                        + stage.WaferAlignOffsetX
                        + d.X
                        + vo.X;

                    int[] pickMoveResults = await Task.WhenAll(
                        MoveAxisCommandAndWaitAsync(front.ArmX, armXTarget, ResolveAxisDefaultVelocity(front.ArmX), true),
                        MoveAxisCommandAndWaitAsync(stage.StageY, stageYTarget, ResolveAxisDefaultVelocity(stage.StageY), false),
                        MoveAxisCommandAndWaitAsync(stage.NeedleBlockX, needleXTarget, ResolveAxisDefaultVelocity(stage.NeedleBlockX), false)
                    );
                    ThrowIfMoveFailed("Pick position PickerX/StageY/NeedleX", pickMoveResults);

                    // Picker Z Down(PickupPosition) / Needle Cap Vacuum ON / Picker Vacuum ON 동시 처리.
                    var pickerZTask = MoveAxisCommandAndWaitAsync(
                        picker.PickerZ, picker.Setup.PickupPosition, picker.Recipe.ZVelocity, false);
                    stage.NeedleVacuum?.On();
                    picker.VacuumOn();
                    int pickerZResult = await pickerZTask;
                    ThrowIfMoveFailed("PickerZ pickup down", pickerZResult);
                    await Task.Delay(picker.Recipe.VacuumSettleMs, ct);

                    // Needle Up / Picker Up(+PickLiftPosition) 동시 처리.
                    double needleUpPos = stage.Recipe.EjectPinZ.ReadyPosition + picker.Recipe.PickLiftPosition;
                    double pickerUpPos = picker.Setup.PickupPosition + picker.Recipe.PickLiftPosition;
                    int[] liftResults = await Task.WhenAll(
                        MoveAxisCommandAndWaitAsync(ej, needleUpPos, ResolveAxisDefaultVelocity(ej), false),
                        MoveAxisCommandAndWaitAsync(picker.PickerZ, pickerUpPos, picker.Recipe.ZVelocity, false)
                    );
                    ThrowIfMoveFailed("Needle/Eject and PickerZ lift after pickup", liftResults);

                    // PickLiftWaitMs 대기.
                    await Task.Delay(picker.Recipe.PickLiftWaitMs, ct);

                    // Picker WaitPosition / Needle DownPosition 동시 복귀.
                    int[] waitResults = await Task.WhenAll(
                        MoveAxisCommandAndWaitAsync(picker.PickerZ, picker.Setup.WaitPosition, picker.Recipe.ZVelocity, false),
                        MoveAxisCommandAndWaitAsync(ej, stage.Recipe.EjectPinZ.ReadyPosition, ResolveAxisDefaultVelocity(ej), false)
                    );
                    ThrowIfMoveFailed("PickerZ/Eject return after pickup", waitResults);

                    pickupOk[p] = !picker.PickerZ.IsAlarm && !ej.IsAlarm;
                }
                catch (Exception ex)
                {
                    Log($"[PICK p{p}] ex: " + ex.Message);
                    pickupOk[p] = false;
                }
            }
            // Needle Cap Vacuum은 picker들이 다이를 잡은 뒤 사이클 끝에서 OFF합니다.
            int okPick = 0; foreach (var ok in pickupOk) if (ok) okPick++;
            Log($"[TPU] Pickup {okPick}/{pickers} ok (cycle {cycleIdx + 1})");
            if (okPick == 0)
            {
                for (int p = 0; p < pickers; p++)
                    JobQueue.MarkFailed(pickJobs[p], "Pick failed");
                NgCount += pickers;
                PickFailCount += pickers;
                try { stage.NeedleVacuum?.Off(); } catch { }
                return;
            }

            // PICK 후 Pickup 실패 picker만 inspPass[p] = false로 반영합니다.
            for (int p = 0; p < pickers; p++)
                if (!pickupOk[p]) inspPass[p] = false;

            // Stage 61: Place가 끝날 때까지 ArmY는 Pickup 위치를 유지합니다.
            // Place 전에 명시적으로 Avoid 이동하여 이전 "PICK 후 Avoid" 동작을 제거합니다.
            // wafer capture 때 ArmX가 InspectionX로 이동하는 동안 wafer 영역 접근이 가능하도록 합니다.

            // 다음 사이클이 존재하면 wafer capture를 시작합니다(non-await, Inspect/Place와 병렬).
            int nextCycleIdx = cycleIdx + 1;
            if (nextCycleIdx < totalCycles)
            {
                int npickers = pickers;
                _pendingCaptureCycleIdx = nextCycleIdx;
                _pendingCapturePickers = npickers;
                _pendingCaptureTask = CaptureWaferForCycleAsync(nextCycleIdx, npickers, ct);
                Log($"[PIPELINE] Cycle {nextCycleIdx + 1}: background wafer capture start.");
            }

            // 14~18) Bottom+Side 병렬 파이프라인(단일 ArmX 파이프라인으로 동시 촬영).
            //   - 좌표 모델: picker N abs X = ArmX - N*PickerPitchX (Side1X = SideVision1X = 850)
            //   - Step 0~3 = Bottom Expose Picker 0~3 (ArmX = ArmInspectionPositionX + i*pitch)
            //   - Step 2~5 = Side sub-sequence Picker 0~3 (동일 ArmX 위치에서 picker[idx-2]를 Side X에 정렬)
            //   - 각 picker Z는 Bottom 직전, Z는 Side 끝에서 발생합니다.
            BottomVisionOffset[] bottomResults = null;
            SideVisionResult[] sideResults = null;
            // 비전 미사용(UseVision=false) 이면 Bottom/Side 검사를 수행하지 않고 PASS 처리(아래 else 분기로).
            bool visionUse = AppSettingsStore.Current == null || AppSettingsStore.Current.UseVision;
            bool visionConnected = visionUse
                                && VisionComm.VisionHub.Inspection != null
                                && VisionComm.VisionHub.Inspection.IsConnected;
            try
            {
                if (visionConnected)
                {
                    if (front.SideVisionY != null) front.SideVisionY.ServoOn();

                    Log("[VISION] Bottom+Side parallel pipeline start (4 picker)...");
                    var both = await front.InspectBottomAndSideAsync(DieSizeXMm, DieSizeYMm);
                    if (both != null)
                    {
                        bottomResults = both.Item1;
                        sideResults = both.Item2;

                        // Bottom 결과를 picker offset / inspPass에 반영합니다.
                        if (bottomResults != null)
                        {
                            for (int p = 0; p < pickers && p < bottomResults.Length; p++)
                            {
                                if (bottomResults[p] == null) continue;
                                dieOffsets[p].X += bottomResults[p].OffsetX;
                                dieOffsets[p].Y += bottomResults[p].OffsetY;
                                if (!bottomResults[p].IsOk) inspPass[p] = false;
                            }
                            int okCnt = 0;
                            foreach (var b in bottomResults) if (b != null && b.IsOk) okCnt++;
                            Log($"[VISION] Bottom {okCnt}/{pickers} ok");
                        }

                        // Side 결과를 inspPass에 반영합니다.
                        if (sideResults != null)
                        {
                            for (int p = 0; p < pickers && p < sideResults.Length; p++)
                            {
                                if (sideResults[p] == null) continue;
                                if (!sideResults[p].IsAllOk) inspPass[p] = false;
                            }
                            int okCnt = 0;
                            foreach (var s in sideResults) if (s != null && s.IsAllOk) okCnt++;
                            Log($"[VISION] Side {okCnt}/{pickers} ok");
                        }
                    }
                }
                else
                {
                    Log("[VISION] Inspection not connected. Bottom/Side check simulated as pass.");
                }
            }
            catch (Exception ex) { Log("[VISION] Bottom+Side ex: " + ex.Message); }

            // 19) PLACE 위치 이동 및 ArmX 이동과 4개 picker Z 대기 위치 복귀를 병렬 수행합니다.
            double placeArmX = front.GetPickerTeachingPosition(PickerAxis.PickerX, "PlacePosition");
            try
            {
                var move19 = new System.Collections.Generic.List<Task<int>>();
                move19.Add(MoveAxisCommandAndWaitAsync(front.ArmX, placeArmX,
                                                       ResolveAxisDefaultVelocity(front.ArmX),
                                                       true));
                for (int p = 0; p < pickers; p++)
                    move19.Add(MoveAxisCommandAndWaitAsync(
                        front.Pickers[p].PickerZ,
                        front.Pickers[p].Setup.WaitPosition,
                        front.Pickers[p].Recipe.ZVelocity,
                        false));
                int[] placeReadyResults = await Task.WhenAll(move19);
                ThrowIfMoveFailed("Place ready PickerX/Z", placeReadyResults);
            }
            catch (Exception ex)
            {
                Log("[PLACE] 19) move ex: " + ex.Message);
                PlaceFailCount += pickers;
                return;
            }
            ct.ThrowIfCancellationRequested();

            // PlaceOnePickerAsync: picker p를 지정 stage(Good/Ng)에 Place합니다.
            async Task PlaceOnePickerAsync(int p, StageModule outStage)
            {
                var picker = front.Pickers[p];
                var bo = (bottomResults != null && p < bottomResults.Length) ? bottomResults[p] : null;
                double offX = bo?.OffsetX ?? 0.0;
                double offY = bo?.OffsetY ?? 0.0;
                double offT = bo?.OffsetT ?? 0.0;

                // ArmX(PlaceX + Bottom OffsetX) / StageY(HomeY + Bottom OffsetY) / PickerT(Bottom OffsetT) 동시 이동.
                int[] placeMoveResults = await Task.WhenAll(
                    MoveAxisCommandAndWaitAsync(front.ArmX, placeArmX + offX,
                                                ResolveAxisDefaultVelocity(front.ArmX),
                                                true),
                    MoveAxisCommandAndWaitAsync(outStage.StageY, outStage.Recipe.HomePositionY + offY,
                                                ResolveAxisDefaultVelocity(outStage.StageY),
                                                false),
                    MoveAxisCommandAndWaitAsync(picker.PickerT, offT, picker.Recipe.ThetaVelocity, false)
                );
                ThrowIfMoveFailed("Place PickerX/StageY/PickerT", placeMoveResults);

                // Picker Z Down(PlacePosition).
                int placeDownResult = await MoveAxisCommandAndWaitAsync(
                    picker.PickerZ,
                    picker.Setup.PlacePosition,
                    picker.Recipe.ZVelocity,
                    false);
                ThrowIfMoveFailed("PickerZ place down", placeDownResult);

                // Vacuum Off + Blow On.
                picker.VacuumOff();
                picker.BlowOn();

                // Place Delay(Recipe).
                await Task.Delay(picker.Recipe.PlaceDelayMs, ct);

                // Blow Off.
                picker.BlowOff();

                // Picker Up(WaitPosition).
                int placeUpResult = await MoveAxisCommandAndWaitAsync(
                    picker.PickerZ,
                    picker.Setup.WaitPosition,
                    picker.Recipe.ZVelocity,
                    false);
                ThrowIfMoveFailed("PickerZ place up", placeUpResult);
            }

            // 20) Good 다이를 먼저 GoodStage에 Place합니다.
            int goodPlaced = 0, ngPlaced = 0;
            for (int p = 0; p < pickers; p++)
            {
                if (!inspPass[p]) continue;
                try
                {
                    await PlaceOnePickerAsync(p, _machine.OutputStageUnit.GoodStage);
                    goodPlaced++;
                }
                catch (Exception ex)
                {
                    Log($"[PLACE Good p{p}] ex: " + ex.Message);
                    PlaceFailCount++;
                }
            }

            // 21) NG 다이 Place 전 GoodStage Z 위치를 전환하고 NgStage에 Place합니다.
            bool hasNg = false;
            for (int p = 0; p < pickers; p++) if (!inspPass[p]) { hasNg = true; break; }
            if (hasNg)
            {
                try
                {
                    // NG Stage has no Z axis. Only GoodStage Z must be moved to avoid before NG place.
                    int goodStageZAvoidResult = await MoveAxisCommandAndWaitAsync(
                        _machine.OutputStageUnit.GoodStage.StageZ,
                        _machine.OutputStageUnit.GoodStage.Recipe.AvoidPositionZ,
                        ResolveAxisDefaultVelocity(_machine.OutputStageUnit.GoodStage.StageZ),
                        false);
                    ThrowIfMoveFailed("GoodStageZ avoid before NG place", goodStageZAvoidResult);
                }
                catch (Exception ex)
                {
                    Log("[PLACE] Bin 전환 ex: " + ex.Message);
                    PlaceFailCount++;
                    return;
                }

                for (int p = 0; p < pickers; p++)
                {
                    if (inspPass[p]) continue;
                    try
                    {
                        await PlaceOnePickerAsync(p, _machine.OutputStageUnit.NgStage);
                        ngPlaced++;
                    }
                    catch (Exception ex)
                    {
                        Log($"[PLACE Ng p{p}] ex: " + ex.Message);
                        PlaceFailCount++;
                    }
                }
            }
            Log($"[TPU] Place result: Good={goodPlaced} Ng={ngPlaced} (cycle {cycleIdx + 1})");

            // Stage 61: Place 완료 후 ArmY를 AvoidPosition으로 회피합니다(다음 사이클 capture 안전 영역).
            try
            {
                int armYAvoidAfterPlaceResult = await MoveAxisCommandAndWaitAsync(
                    front.ArmY,
                    front.GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                    ResolveAxisDefaultVelocity(front.ArmY),
                    false);
                ThrowIfMoveFailed("FrontPickerY avoid after place", armYAvoidAfterPlaceResult);
                Log($"[ARM-Y] Cycle {cycleIdx + 1}: Place complete. Return to Avoid position.");
            }
            catch (Exception ex)
            {
                Log("[ARM-Y avoid after PLACE] ex: " + ex.Message);
                PlaceFailCount++;
                return;
            }

            // 결과 반영: 4개 다이를 각각 inspPass[p] 기준으로 Good/NG 분리 기록합니다.
            int goodInCycle = 0, ngInCycle = 0;
            for (int p = 0; p < pickers; p++)
            {
                var d = dies[p];
                if (inspPass[p])
                {
                    d.Result = DieResult.Good;
                    d.BinCode = BinCodeMap.ConvertToBinCode(d);
                    JobQueue.MarkDone(pickJobs[p], "Good");
                    GoodCount++;
                    goodInCycle++;
                    try { PlateRegistry.RecordGoodDie(d.BinCode); } catch { }
                }
                else
                {
                    d.AddNG("InspFail");
                    d.BinCode = BinCodeMap.ConvertToBinCode(d);
                    JobQueue.MarkFailed(pickJobs[p], "InspFail");
                    NgCount++;
                    ngInCycle++;
                    try { PlateRegistry.RecordNgDie(d.BinCode); } catch { }
                }
                LotStorage.ActiveLot?.RecordDie(d.BinCode, inspPass[p]);
                if (mapEntries[p] != null)
                {
                    mapEntries[p].DieUid = d.Uid;
                    mapEntries[p].Result = d.Result;
                    mapEntries[p].BinCode = d.BinCode;
                }
            }

            // Stage 27: WafersPerOutputBatch 다이마다 Output 카세트 적재를 트리거합니다.
            // 다이 베이스 + pickers가 WafersPerOutputBatch 경계를 넘으면 실행합니다.
            int diesProcessedTotal = dieBase + pickers;
            if (WafersPerOutputBatch > 0 &&
                (diesProcessedTotal / WafersPerOutputBatch) > (dieBase / WafersPerOutputBatch))
            {
                Log($"[FEEDER] {WafersPerOutputBatch} dies complete. Store to output.");
                // 사이클의 다수 결과로 wafer 적재 등급을 분류합니다(Good 우세 = Good).
                bool anyGood = false; foreach (var ip in inspPass) if (ip) { anyGood = true; break; }
                await StoreCompletedWaferAsync(anyGood);
            }

            // Stage 33: DiesPerColletClean 다이마다 Collet Cleaning을 수행합니다.
            if (DiesPerColletClean > 0 &&
                (diesProcessedTotal / DiesPerColletClean) > (dieBase / DiesPerColletClean))
            {
                Log($"[COLLET] {DiesPerColletClean} dies complete. Start collet cleaning.");
                try
                {
                    bool clOk = await _machine.OutputStageUnit.PerformColletCleaningAsync();
                    Log("[COLLET] Cleaning " + (clOk ? "OK" : "WARN"));
                }
                catch (Exception ex) { Log("[COLLET] exception: " + ex.Message); }
            }
            // Reject 분리 대상 다이 검사.
            for (int p = 0; p < pickers; p++)
            {
                if (SubPortMaterialRejector.ShouldReject(dies[p], out double rxX, out double rxY))
                {
                    Log($"[DIE {dieBase + p + 1}] reject -> ({rxX:F1},{rxY:F1})  bin={dies[p].BinCode}");
                }
            }
            Log($"[CYCLE {cycleIdx + 1}] dies {dieBase + 1}~{dieBase + pickers}/{CycleTotal} good={goodInCycle} ng={ngInCycle}");
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

        private BaseCylinder FindCylinderByName(string cylinderName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cylinderName))
                    return null;

                BaseCylinder cylinder;
                if (QMC.CDT320.Ajin.CylinderManager.Items.TryGetValue(cylinderName.Trim(), out cylinder) &&
                    cylinder != null)
                    return cylinder;

                foreach (var item in QMC.CDT320.Ajin.CylinderManager.Items.Values)
                {
                    if (item != null && string.Equals(item.Name, cylinderName.Trim(), StringComparison.OrdinalIgnoreCase))
                        return item;
                }

                return QMC.CDT320.Ajin.CylinderManager.Get(cylinderName.Trim());
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

        private void RaiseProgress()
        {
            var h = CycleProgress;
            if (h != null) try { h(CycleDone, CycleTotal); } catch { }
        }
    }
}

