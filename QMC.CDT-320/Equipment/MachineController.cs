using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.Common.Alarms;
using QMC.CDT320.Bin;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Jobs;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Alarms;
using QMC.CDT320.Initialization;
using QMC.CDT320.Motion.SharedRailX;

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
        private int _manualBusyCount;
        private CancellationTokenSource _manualCts;
        private bool _isMachineInitialized;
        private bool _isDeveloperReadyRestored;
        private readonly AxisInterferenceMap _axisInterferenceMap = AxisInterferenceMap.CreateDefault();
        private readonly AxisInitializeInterlockService _axisInitializeInterlocks;
        private readonly object _initializeHomedAxisLock = new object();
        private HashSet<string> _initializeHomedAxisNames;
        private readonly object _axisInitializeStepStateLock = new object();
        private readonly Dictionary<string, AxisInitializeStepProgress> _axisInitializeStepStates =
            new Dictionary<string, AxisInitializeStepProgress>(StringComparer.OrdinalIgnoreCase);
        private bool _restoringAxisInitializeStepState;
        public SharedRailXMotionService SharedRailX { get; private set; }

        public event Action<EquipmentStatus> StatusChanged;
        public event Action<string> LogMessage;
        public event Action<int, int> CycleProgress;  // (done, total)
        public event Action<bool> MachineInitializedChanged;
        public event Action<AxisInitializeStepProgress> AxisInitializeStepProgressChanged;

        /// <summary>CDT-320 하드웨어 유닛 트리입니다.</summary>
        public CDT320_Machine Machine => _machine;

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

        // Stage 61 ??Pickup sequence options + cached sequence
        /// <summary>?⑥씠???ㅼ씠 ?쎌뾽 ?쒖꽌 ?듭뀡 (Recipe.Pickup ?먯꽌 set).</summary>
        public QMC.CDT320.Recipes.PickupSubset PickupOptions { get; set; } =
            new QMC.CDT320.Recipes.PickupSubset();

        /// <summary>?듭뀡 湲곕컲 ?뺣젹???쎌뾽 ?쒗??(?쒖꽦 ?ㅼ씠留? ?쒖꽌?濡?. EnsureDieMaps ?먮뒗 RebuildPickupSequence ?몄텧 ??媛깆떊.</summary>
        private List<QMC.CDT320.DieMaps.DieMapEntry> _inputPickupSequence
            = new List<QMC.CDT320.DieMaps.DieMapEntry>();

        // Pipelined wafer capture state.
        // Capture can overlap with the next inspect/place cycle.
        private Task<(double X, double Y)[]> _pendingCaptureTask;
        private int _pendingCaptureCycleIdx = -1;
        private int _pendingCapturePickers = 0;

        public IReadOnlyList<QMC.CDT320.DieMaps.DieMapEntry> InputPickupSequence
            => _inputPickupSequence;

        /// <summary>PickupOptions ?먮뒗 _inputDieMap 蹂寃????몄텧. ?쒗???ъ깮??</summary>
        public void RebuildPickupSequence()
        {
            if (_inputDieMap == null) { _inputPickupSequence.Clear(); return; }
            _inputPickupSequence = QMC.CDT320.DieMaps.PickupSequenceGenerator.Build(
                _inputDieMap, PickupOptions);
            Log("[PICKSEQ] " + PickupOptions.StartCorner + " / " +
                PickupOptions.Direction + " / " + PickupOptions.Pattern +
                " ???쒖꽦 ?ㅼ씠 " + _inputPickupSequence.Count + "媛??쒖꽌 寃곗젙");
        }

        /// <summary>Input/Output ?ㅼ씠留듭쓣 (?놁쑝硫? ?앹꽦. ?대? ?덉쑝硫??ъ궗??</summary>
        public void EnsureDieMaps()
        {
            if (_inputDieMap == null)
            {
                _inputDieMap = QMC.CDT320.DieMaps.DieMapGenerator.GenerateWafer(
                    WaferDiameterMm, DieSizeXMm, DieSizeYMm, InputGapMm, InputGapMm,
                    frameObjId: "INPUT");
                int active = 0;
                foreach (var e in _inputDieMap.Entries) if (e.IsTarget) active++;
                Log("[DIEMAP] Input wafer " + WaferDiameterMm + "mm 쨌 die " +
                    DieSizeXMm + "x" + DieSizeYMm + " 쨌 gap " + InputGapMm +
                    " ??grid " + _inputDieMap.GridX + "x" + _inputDieMap.GridY +
                    " 쨌 active=" + active);
                // UI ?쒓컖???꾪빐 LotStorage ???깅줉
                QMC.CDT320.Lots.LotStorage.ActiveInputDieMap = _inputDieMap;
            }
            if (_outputDieMap == null)
            {
                // Output ?ш컖 ?몃젅????Input ?쒖꽦 ?ㅼ씠 ???댁긽 ?섏슜
                int active = 0;
                foreach (var e in _inputDieMap.Entries) if (e.IsTarget) active++;
                // ?�사�?격자 root(N) ?�림
                int side = (int)System.Math.Ceiling(System.Math.Sqrt(active));
                _outputDieMap = QMC.CDT320.DieMaps.DieMapGenerator.GenerateRect(
                    side, side, DieSizeXMm, DieSizeYMm, OutputGapMm, OutputGapMm,
                    frameObjId: "OUTPUT");
                Log("[DIEMAP] Output tray die " + DieSizeXMm + "x" + DieSizeYMm +
                    " 쨌 gap " + OutputGapMm + " ??grid " + side + "x" + side +
                    " 쨌 slots=" + (side * side));
            }

            // WafersPerOutputBatch <= 0 ?대㈃ OutputDieMap ?щ’ ?섏뿉 ?먮룞 留욎땄
            if (WafersPerOutputBatch <= 0 && _outputDieMap != null)
            {
                WafersPerOutputBatch = _outputDieMap.Entries.Count;
                Log("[DIEMAP] WafersPerOutputBatch auto set = " + WafersPerOutputBatch + " (Output tray slot count)");
            }

            // Stage 61 ???쎌뾽 ?쒗???앹꽦 (?듭뀡 ?곸슜)
            RebuildPickupSequence();
        }

        // ??????????????????????????????????????????
        //  Stage 58 ???댁쁺 ?듦퀎 (Work Info / Work Time)
        //  : WorkMainPage / OperationPanelStatusPage ?먯꽌 ?대쭅.
        //  internal setter ??Cycle ?ㅽ뻾 以묒뿉 ?꾩쟻, Init ??由ъ뀑.
        // ??????????????????????????????????????????
        /// <summary>PICK ?ㅽ뙣 ?꾩쟻 ??(?ъ떆???꾩뿉???ㅽ뙣濡?移댁슫?몃릺??寃쎌슦).</summary>
        public int PickFailCount { get; internal set; }
        /// <summary>PLACE ?ㅽ뙣 ?꾩쟻 ??(Output ?щ’ placement vision NG).</summary>
        public int PlaceFailCount { get; internal set; }
        /// <summary>FRONT (LEFT ARM) Collet ?ъ슜 ?잛닔 ??Pick 1?뚮떦 +1.</summary>
        public int Collet1UseCount { get; internal set; }
        /// <summary>REAR (RIGHT ARM) Collet ?ъ슜 ?잛닔.</summary>
        public int Collet2UseCount { get; internal set; }
        /// <summary>EjectPin/Needle ?ъ슜 ?잛닔 (?ㅼ씠 1媛쒕떦 1??.</summary>
        public int NeedleUseCount { get; internal set; }
        /// <summary>?뚮엺 ?꾩쟻 諛쒖깮 ??</summary>
        public int ErrorCount { get; internal set; }
        /// <summary>?뺤긽 ?ㅼ슫(STOP/ECMG ??IDLE) ?꾩쟻 ?쒓컙.</summary>
        public TimeSpan NormalDownTime { get; internal set; } = TimeSpan.Zero;
        /// <summary>?뚮엺/?먮윭濡??명븳 ?ㅼ슫 ?꾩쟻 ?쒓컙.</summary>
        public TimeSpan ErrorDownTime { get; internal set; } = TimeSpan.Zero;
        /// <summary>?뚮엺 諛쒖깮 ??蹂듦뎄源뚯? 嫄몃┛ ?꾩쟻 ?쒓컙.</summary>
        public TimeSpan RecoveryTime { get; internal set; } = TimeSpan.Zero;
        /// <summary>Mean Time Between Failure (rolling).</summary>
        public TimeSpan Mtbf { get; internal set; } = TimeSpan.Zero;
        /// <summary>Mean Time To Recovery (rolling).</summary>
        public TimeSpan Mttr { get; internal set; } = TimeSpan.Zero;

        // ??????????????????????????????????????????
        //  濡쒗듃?ы듃 (Input Cassette) 吏꾪뻾 ?곹깭
        // ??????????????????????????????????????????

        /// <summary>?꾩옱 InputLoader 媛 泥섎━ 以묒씤 ?щ’ ?몃뜳??(0-base). -1 = 誘몄옣李??몃줈???곹깭.</summary>
        public int CurrentInputSlot { get; private set; } = -1;
        /// <summary>?꾩옱 ?щ’???⑥씠?쇨? InputStage 援먰솚 ?꾩튂源뚯? ?댁넚?섏뿀?붿? ?щ?.</summary>
        public bool InputWaferAtExchange { get; private set; } = false;
        /// <summary>濡쒗듃?ы듃 ?쒗??吏꾪뻾 ?쒖젏??諛쒗뻾 (UI 媛깆떊??.</summary>
        public event Action LotPortStateChanged;

        /// <summary>InitAsync ??移댁꽭???먮룞 留ㅽ븨 ?щ? (湲곕낯 false ??INIT ? 紐⑦꽣 珥덇린?붾쭔).
        /// 移댁꽭???ㅼ틪? ?묒뾽?뺣낫/Output ?섏씠吏??留ㅽ븨 踰꾪듉?쇰줈 蹂꾨룄 ?섑뻾.</summary>
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
                    AlarmManager.Raise(AlarmSeverity.Warning, "INIT-RESTORE-NO-STATE", "MachineController",
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
                    AlarmManager.Raise(AlarmSeverity.Warning, "INIT-RESTORE-FAIL", "MachineController",
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
                AlarmManager.Raise(AlarmSeverity.Warning, "INIT-RESTORE-EX", "MachineController",
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
                InitializeSteps = CaptureAxisInitializeStepRuntimeStates()
            };

            foreach (var ax in EnumerateAxes())
            {
                try { ax.UpdateStatus(); } catch { }
                state.Axes.Add(new MachineAxisRuntimeState
                {
                    Name = ax.Name,
                    IsServoOn = ax.IsServoOn,
                    IsAlarm = ax.IsAlarm,
                    IsHomeDone = ax.IsHomeDone,
                    IsInPosition = ax.IsInPosition,
                    ActualPosition = ax.ActualPosition,
                    CommandPosition = ax.CommandPosition,
                    AlarmCode = ax.AlarmCode
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

                LastActionFailureMessage = "장비 초기화가 완료되지 않았습니다. INIT 후 START를 수행하세요.";
                QMC.Common.Log.Write("Main", "SYSTEM", source,
                    "Run failed: machine is not initialized. - Failed");
                AlarmManager.Raise(AlarmSeverity.Warning, "START-NOT-INITIALIZED", "MachineController",
                    LastActionFailureMessage);
                Log("[START] failed: machine is not initialized");
                return false;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", source,
                    "Run initialized check failed: " + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Warning, "START-INIT-CHECK", "MachineController",
                    "장비 초기화 상태 확인 실패. " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        // ??????????????????????????????????????????
        //  濡쒗듃?ы듃 ?쒗???ы띁
        // ??????????????????????????????????????????

        /// <summary>
        /// InputLoader ???ㅼ쓬 ?⑥씠?쇰? InputStage 援먰솚 ?꾩튂源뚯? ?먮룞 吏꾪뻾.<br/>
        /// 1) WaferMap ??鍮꾩뼱 ?덉쑝硫?ScanCassetteAsync 濡?留ㅽ븨<br/>
        /// 2) <see cref="CurrentInputSlot"/> ?ㅼ쓬???⑥씠??蹂댁쑀 ?щ’?쇰줈 LifterZ ?대룞<br/>
        /// 3) MoveToExchangePositionAsync ?몄텧 (?쇰뜑 ?섍컯 ???대옩????Y ?꾩쭊)
        /// </summary>
        /// <returns>?ㅼ쓬 ?⑥씠???댁넚 ?깃났 ??true. 移댁꽭??鍮꾩뿀嫄곕굹 ?명꽣??李⑤떒 ??false.</returns>
        public async Task<bool> LoadNextWaferAsync()
        {
            var cassette = _machine.InputCassetteUnit;
            var feeder = _machine.InputFeederUnit;

            // 移댁꽭???덉갑 ?뺤씤
            if (!cassette.CassetteExistSensor.IsOn)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LOT-NOCASS",
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
                    AlarmManager.Raise(AlarmSeverity.Warning, "LOT-SCAN",
                        cassette.Name, "Input cassette scan failed.");
                    return false;
                }
                RaiseLotPortChanged();
            }

            // ?ㅼ쓬 ?⑥씠???щ’ ?먯깋
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

            // ?대룞 + 援먰솚 ?꾩튂 ?꾩쭊
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

            // Stage 34 ??Sim 紐⑤뱶: ?뚮퉬???щ’??false 濡?留덊궧 (UI LED ?뺥솗??
            //   Form1.CassetteDriver ??internal ?댁?留? ?숈씪 ?댁뀍釉붾━?대?濡?reflection ?놁씠 ?묎렐 媛??
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

            // Stage 28 ???곗뾽 ?먮쫫 諛섏쁺 InputStage handoff ?쒗??
            //   1. (?대? ?꾨즺) ?쇰뜑媛 ExchangePosition(150mm) ?쇰줈 ?꾩쭊 ???⑥씠??InputStage ?낃뎄
            //   2. InputStage.LoadAndPrepareWaferAsync ??ExpanderZ ?대옩?? Wafer 諛쏆쓬
            //      WaferLoaderAdapter ???쇰뜑 ??40mm ?대?濡?Safe ?먯젙
            //   3. (蹂꾨룄 ?몄텧) RetractFeeder ???쇰뜑 ??蹂듦?
            //   4. InputStage.VisionAlignAndSetupOriginAsync ???뺣젹 + Origin ?뺤젙
            try
            {
                Log("[LOTPORT] InputStage handoff (LoadAndPrepare) start...");
                Log("[LOTPORT] LoadAndPrepareWaferAsync is not active in InputStageUnit. Skip handoff call.");
                int handoff = 0;
                Log("[LOTPORT] InputStage handoff " + (handoff == 0 ? "OK" : "WARN"));

                // Stage 58 ??臾몄꽌 ?뺥빀: InputStage ?쒗???ㅽ뙣 ??AlarmManager.Raise 蹂닿컯.
                // (?댁쟾: Console.WriteLine 留???UI ?뚮엺 諛곕꼫/?덉뒪?좊━??誘몃컲??
                if (handoff != 0)
                {
                    AlarmManager.Raise(AlarmSeverity.Warning, "IS-LOAD",
                        _machine.InputStageUnit.Name,
                        "LoadAndPrepareWafer failed. Check feeder safe position, ExpanderZ, and barcode readiness.");
                    ErrorCount++;
                }

                if (handoff == 0)
                {
                    // ?쇰뜑 ?꾪눜 (?⑥씠?쇰뒗 ?대? InputStage 媛 ?↔퀬 ?덉쓬)
                    Log("[LOTPORT] Retract feeder. InputStage continues standalone work.");
                    await feeder.RetractFeederAsync();
                    InputWaferAtExchange = false;
                    RaiseLotPortChanged();

                    // VisionAlign + Origin ?뺤젙
                    Log("[INPUTSTAGE] VisionAlign start...");
                    Log("[INPUTSTAGE] VisionAlignAndSetupOriginAsync is not active in InputStageUnit. Skip align call.");
                    int aligned = 0;
                    Log("[INPUTSTAGE] VisionAlign " + (aligned == 0 ? "OK" : "WARN (simulation limit)"));

                    if (aligned != 0)
                    {
                        AlarmManager.Raise(AlarmSeverity.Warning, "IS-ALIGN",
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

        // ??????????????????????????????????????????
        //  Stage 28 ??InputStage ?ъ씠???듯빀 ?ы띁
        // ??????????????????????????????????????????

        /// <summary>?ㅼ씠 1媛??쎌뾽???꾪빐 StageY/CameraX 瑜??대룞.</summary>
        public async Task<bool> MoveInputStageToDieAsync(int row, int col)
        {
            try
            {
                var stage = _machine.InputStageUnit;
                // Stage 28 ??Origin + Pitch 媛 ?뺤젙?섏뿀?쇰㈃ ?뺥솗???대룞, ?꾨땲硫?異붿젙媛?
                double targetX = stage.OriginX + col * (stage.PitchX > 0 ? stage.PitchX : 0.15);
                double targetY = stage.OriginY + row * (stage.PitchY > 0 ? stage.PitchY : 0.15);
                bool xOk = await MoveAxisAsync(stage.CameraX, targetX, 100.0);
                bool yOk = await MoveAxisAsync(stage.StageY, targetY, 100.0);
                if (!xOk || !yOk)
                {
                    Log($"[INPUTSTAGE] Move to die [{row},{col}] blocked");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log("[INPUTSTAGE] MoveToDie exception: " + ex.Message);
                return false;
            }
        }

        /// <summary>InputStage ???⑥씠???몃줈???쒗??(?ъ씠??醫낅즺 ??.</summary>
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
        /// ?꾩옱 InputStage 援먰솚 ?꾩튂???쇰뜑瑜??꾪눜?쒖폒 鍮?移댁꽭???щ’??蹂듦?.<br/>
        /// ?ъ씠??醫낅즺 ?먮뒗 ?ㅼ쓬 ?щ’ 吏꾪뻾 ???몄텧.
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

        // ??????????????????????????????????????????
        //  Stage 27 - Output cassette/feeder transfer support.
        // ??????????????????????????????????????????

        /// <summary>?꾩옱 Output 移댁꽭?몃퀎 ?ㅼ쓬 ?곸옱 ?щ’ ?몃뜳??(0-base).</summary>
        public int OutputSlotNg { get; private set; } = 0;
        public int OutputSlotGood1 { get; private set; } = 0;
        public int OutputSlotGood2 { get; private set; } = 0;
        /// <summary>Completed wafer store request delegated to OutputCassette/OutputFeeder.</summary>
        /// 0 = EnsureDieMaps ?먯꽌 OutputDieMap ?щ’ ?섎줈 ?먮룞 ?ㅼ젙.</summary>
        public int WafersPerOutputBatch { get; set; } = 0;

        /// <summary>Place completed wafer into Output Cassette.</summary>
        public async Task<bool> StoreCompletedWaferAsync(bool isGood)
        {
            var cassette = _machine.OutputCassetteUnit;
            var feeder = _machine.OutputFeederUnit;
            // 카세???�정: Good ??Good1 ?�선, 가??차면 Good2; NG ??Ng
            QMC.CDT320.TargetCassette target;
            int slot;
            if (isGood)
            {
                if (OutputSlotGood1 < 25) { target = QMC.CDT320.TargetCassette.Good1; slot = OutputSlotGood1++; }
                else if (OutputSlotGood2 < 25) { target = QMC.CDT320.TargetCassette.Good2; slot = OutputSlotGood2++; }
                else
                {
                    // Stage 27 fix ??移댁꽭??媛??= ?ъ씠???먮룞 ?뺤?
                    AlarmManager.Raise(AlarmSeverity.Error, "OUT-FULL-GOOD",
                        cassette.Name, "Good output cassette is full. Cycle stop.");
                    Log("[FEEDER] Good cassette full. CycleStop.");
                    _cycleCts?.Cancel();
                    return false;
                }
            }
            else
            {
                if (OutputSlotNg < 25) { target = QMC.CDT320.TargetCassette.Ng; slot = OutputSlotNg++; }
                else
                {
                    // Stage 27 fix ??NG 移댁꽭??媛??= ?ъ씠???먮룞 ?뺤?
                    AlarmManager.Raise(AlarmSeverity.Error, "OUT-FULL-NG",
                        cassette.Name, "NG output cassette is full. Cycle stop.");
                    Log("[FEEDER] NG cassette full. CycleStop.");
                    _cycleCts?.Cancel();
                    return false;
                }
            }
            Log($"[FEEDER] StoreFullWafer target={target} Slot[{slot}]");
            try
            {
                bool ok = await cassette.StoreFullWaferAsync(feeder, target, slot);
                if (!ok)
                {
                    AlarmManager.Raise(AlarmSeverity.Error, "OUT-STORE",
                        cassette.Name, "StoreFullWafer failed.");
                    return false;
                }
                Log($"[FEEDER] OK. Stored target={target} Slot[{slot}]");

                // Stage 37 ??SimCassetteDriver Output ?щ’ ?낅뜲?댄듃 (UI LED ?숆린??
                try
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
                            driver.SetOutputSlotFilled(target, slot, true);
                            break;
                        }
                    }
                }
                catch { /* best-effort */ }

                return true;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "OUT-STORE-EX",
                    cassette.Name, ex.Message);
                return false;
            }
        }

        /// <summary>Output 3 카세트 매핑 (UI 버튼).</summary>
        public async Task<bool> ScanOutputCassettesAsync()
        {
            var cassette = _machine.OutputCassetteUnit;
            try
            {
                bool ok = await cassette.ScanAllCassettesAsync();
                if (ok) Log("[FEEDER] Output 3 cassette scan complete.");
                return ok;
            }
            catch (Exception ex)
            {
                Log("[FEEDER] OutputScan exception: " + ex.Message);
                return false;
            }
        }

        // ??????????????????????????????????????????

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

        // ??????????????????????????????????????????
        //  Stage 32 ???ㅻ퉬 ?섏? ?쇱씠?꾩궗?댄겢
        // ??????????????????????????????????????????

        /// <summary>?ㅻ퉬 ?뺤긽 醫낅즺 ?쒗?? ?ъ씠???뺤? + 異?Stop + Lot ?뺣━.</summary>
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

        /// <summary>RESET ALARM ??紐⑤뱺 異??뚮엺 由ъ뀑 + AlarmManager ?쒖꽦 ?뚮엺 ?댁젣.
        /// ?덉쟾 ?뺤씤 ???몄텧. ?뚮엺 ?곹깭???뚮쭔 ?섎? ?덉쓬.</summary>
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

                // ?뚮엺 ?곹깭??쇰㈃ Idle 濡?(?댁쁺 ?ш컻 ?꾪빐 INIT ?꾩슂)
                if (_status == EquipmentStatus.Alarm) SetStatus(EquipmentStatus.Idle);

                // Tower Lamp OFF (?뚮엺 ?댁젣)
                try { _machine.OpPanelUnit?.TowerLampOff(); } catch { }
            }
            catch (Exception ex)
            {
                Log("[RESET-ALARM] exception: " + ex.Message);
            }
            return Task.CompletedTask;
        }

        /// <summary>鍮꾩긽 ?뺤? ??紐⑤뱺 異?EStop + ?뚮엺 諛쒖깮.
        /// R4 ??TowerLamp ?쒖뼱 寃곌낵瑜?紐낆떆?곸쑝濡?濡쒓렇 (silent catch ?쒓굅).</summary>
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
                // E-STOP ?뺤콉: 紐⑤뱺 異?Servo OFF (?ъ슜???붽뎄 ??鍮꾩긽?뺤? ???덉슜???먮룞 OFF)
                foreach (var ax in EnumerateAxes())
                {
                    try { ax.ServoOff(); } catch { }
                }
                AlarmManager.Raise(AlarmSeverity.Error, "E-STOP", "Machine",
                    "Emergency stop was triggered by user safety operation.");
                SaveMachineRuntimeState("EmergencyStop");
                SetStatus(EquipmentStatus.Alarm);

                // Stage 45 ??Tower Lamp ?�람 (빨강 + 부?�). 명시??결과 기록.
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
                        AlarmManager.Raise(AlarmSeverity.Warning, "TOWER-FAIL", "OpPanel", lampEx.Message);
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

        // ??????????????????????????????????????????
        //  ?댁쁺 紐⑤뱶 (DryRun / StepRun) ??Stage 13
        // ??????????????????????????????????????????

        /// <summary>true 硫?紐⑥뀡 ?놁씠 吏꾪뻾留?(Recipe.DryRun ?곹뼢).</summary>
        public bool DryRun { get; set; } = false;
        public bool GlobalDryRun { get; set; } = false;
        /// <summary>true 硫??ㅼ씠 1媛쒕쭏???ъ슜???뺤씤 (Recipe.StepRun ?곹뼢). CycleMode.Step ? ?숈씪.</summary>
        public bool StepRun
        {
            get => CycleMode == CycleMode.Step;
            set => CycleMode = value ? CycleMode.Step : CycleMode.Auto;
        }
        /// <summary>R3 ???ъ씠???댁쁺 紐⑤뱶 (Auto/Manual/Step).</summary>
        public CycleMode CycleMode { get; set; } = CycleMode.Auto;
        /// <summary>StepRun 吏꾪뻾 ?좏샇 ???ъ슜??GUI 媛 肄쒕갚?쇰줈 ?ㅼ쓬 ?ㅼ씠 ?덉슜/李⑤떒.</summary>
        public event Func<int, bool> StepRunGate;

        /// <summary>?꾩옱 ?쒖꽦 RecipeProject ??DryRun/StepRun ?곸슜.</summary>
        public void ApplyRecipeMode(QMC.CDT320.Recipes.RecipeProject p)
        {
            if (p == null) return;
            DryRun = GlobalDryRun || p.DryRun;
            StepRun = p.StepRun;
            // Stage 33 ??Recipe ??ModuleSubset ?뚮씪誘명꽣瑜?Controller ?듭뀡??諛섏쁺
            if (p.Module != null)
            {
                if (p.Module.ColletCleanEnable && p.Module.ColletCleanInterval > 0)
                    DiesPerColletClean = p.Module.ColletCleanInterval;
                else if (!p.Module.ColletCleanEnable)
                    DiesPerColletClean = 0;
            }
            // Stage 54 ??Recipe.Output ?뚮씪誘명꽣 ?곸슜
            if (p.Output != null)
            {
                if (p.Output.DiesPerWafer > 0)
                    DiesPerWafer = p.Output.DiesPerWafer;
                if (p.Output.WafersPerOutputBatch > 0)
                    WafersPerOutputBatch = p.Output.WafersPerOutputBatch;
                // Stage 58 ??Plate MaxSlots 媛깆떊
                if (p.Output.GoodPlateMaxSlots > 0)
                    PlateRegistry.GoodPlate.MaxSlots = p.Output.GoodPlateMaxSlots;
                if (p.Output.NgPlateMaxSlots > 0)
                    PlateRegistry.NgPlate.MaxSlots = p.Output.NgPlateMaxSlots;
            }
            // Stage 61 ??Recipe.Pickup (StartCorner/Direction/Pattern) ?곸슜
            if (p.Pickup != null)
            {
                PickupOptions = p.Pickup;
                RebuildPickupSequence();
                Log($"[MODE] Pickup={p.Pickup.StartCorner}/{p.Pickup.Direction}/{p.Pickup.Pattern}");
            }
            Log($"[MODE] DryRun={DryRun}  StepRun={StepRun}  EbrMode={p.EbrMode}  ColletEvery={DiesPerColletClean}  DiesPerWafer={DiesPerWafer}");
        }

        // ??????????????????????????????????????????
        //  중앙??모션 (Interlock 검�????�제 ?�동)
        // ??????????????????????????????????????????

        /// <summary>
        /// ?명꽣濡?寃利????덈? ?꾩튂 ?대룞. 李⑤떒?섎㈃ false 諛섑솚 + ?뚮엺.
        /// 310 ??MotionInterlock.OnVerifyToMove ? ?숇벑.
        /// </summary>
        public async Task<bool> MoveAxisAsync(BaseAxis axis, double position, double velocity = 800.0)
        {
            if (axis == null) return false;
            if (!InterlockRegistry.VerifyMove(axis.Name, position, out string reason))
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "INTERLOCK", axis.Name,
                    $"Move blocked: target={position:F1}, reason={reason}");
                Log($"[INTERLOCK] {axis.Name} target={position:F1} blocked: {reason}");
                return false;
            }
            if (DryRun)
            {
                Log($"[DRYRUN] skip move {axis.Name} target={position:F1} (vel={velocity:F0})");
                return true;
            }
            await SharedRailXMotionRuntime.MoveAxisAsync(axis, position, velocity);
            return true;
        }

        // ??????????????????????????????????????????
        //  Wafer Alignment (3 ??비전 매칭 ??CoordinateMap)
        // ??????????????????????????????????????????

        /// <summary>
        /// 3 ??鍮꾩쟾 ?뺣젹 ??TopLeft / TopRight / BottomLeft 湲곗??먯뿉??鍮꾩쟾 留ㅼ묶?섏뿬
        /// CoordinateMap 媛깆떊 (310 ??DieTapeFrameAlignmentJob ?⑥닚??.
        /// </summary>
        /// <param name="motorPts">媛?湲곗??먯쓽 紐⑦꽣 醫뚰몴 [(mx,my) 횞3].</param>
        /// <param name="finder">留ㅼ묶???ъ슜??Finder ?대쫫 (湲곕낯 ReticleFinder).</param>
        public async Task<bool> AlignWaferAsync(
            (double mx, double my)[] motorPts,
            string finder = "ReticleFinder")
        {
            if (motorPts == null || motorPts.Length < 3)
            { Log("[ALIGN] need 3 motor points"); return false; }
            if (VisionComm.VisionHub.Wafer == null || !VisionComm.VisionHub.Wafer.IsConnected)
            { Log("[ALIGN] Wafer Vision not connected"); return false; }

            double[] px = new double[3], py = new double[3];
            double[] mx = new double[3], my = new double[3];
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    Log($"[ALIGN] point {i + 1}/3 move motor -> ({motorPts[i].mx:F2}, {motorPts[i].my:F2})");
                    // ?ㅼ젣 紐⑥뀡? ?댁쁺 ?섍꼍?먯꽌 異붽?; ?쒕??먯꽌??留ㅼ묶 ?몄텧留?
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
                    AlarmManager.Raise(AlarmSeverity.Warning, "INIT-AXIS-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                var axis = FindAxisByName(axisName);
                if (axis == null)
                {
                    LastActionFailureMessage = "초기화할 축을 찾을 수 없습니다. axis=" + axisName;
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxis",
                        "Axis initialize failed: axis not found. axis=" + axisName + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Warning, "INIT-AXIS-NOTFOUND", "MachineController", LastActionFailureMessage);
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
                    AlarmManager.Raise(AlarmSeverity.Warning, "INIT-GROUP-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                var plan = AxisInitializePlanStore.LoadOrCreateDefault(EnumerateAxes());
                var steps = ResolveInitializeStepsByGroup(plan, groupName);
                if (steps.Count == 0)
                {
                    LastActionFailureMessage = "초기화할 축 그룹을 찾을 수 없습니다. group=" + groupName;
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAxisGroup",
                        "Axis group initialize failed: initialize step group is empty. group=" + groupName + " - Failed");
                    AlarmManager.Raise(AlarmSeverity.Warning, "INIT-GROUP-EMPTY", "MachineController", LastActionFailureMessage);
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
                    AlarmManager.Raise(AlarmSeverity.Warning, "INIT-ALL-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                var allAxes = new List<BaseAxis>(EnumerateAxes());
                if (allAxes.Count == 0)
                {
                    LastActionFailureMessage = "초기화할 축 정보가 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "InitializeAllAxes",
                        "All axes initialize failed: axis list is empty. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Warning, "INIT-ALL-EMPTY", "MachineController", LastActionFailureMessage);
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
                    AlarmManager.Raise(AlarmSeverity.Warning, "INIT-PLAN-EMPTY", "MachineController", LastActionFailureMessage);
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
                    AlarmManager.Raise(AlarmSeverity.Warning, "INIT-STEP-RUNNING", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                var plan = GetAxisInitializePlan();
                var steps = plan != null && plan.Steps != null
                    ? plan.Steps.Where(x => x != null && x.Enabled && x.StepNo == stepNo).ToList()
                    : new List<AxisInitializeStep>();

                if (steps.Count == 0)
                {
                    LastActionFailureMessage = "실행할 초기화 Step을 찾을 수 없습니다. step=" + stepNo;
                    AlarmManager.Raise(AlarmSeverity.Warning, "INIT-STEP-NOTFOUND", "MachineController", LastActionFailureMessage);
                    return -1;
                }

                SetMachineInitialized(false, "InitializePlanStepStart:" + stepNo, false);
                SetStatus(EquipmentStatus.Initializing);

                lock (_initializeHomedAxisLock)
                {
                    _initializeHomedAxisNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                int result;
                if (stepNo == 71 || stepNo == 72 || stepNo == 73 || stepNo == 74)
                {
                    var sequenceSteps = plan.Steps.Where(x => x != null && x.Enabled && (x.StepNo == 71 || x.StepNo == 72 || x.StepNo == 73 || x.StepNo == 74)).ToList();
                    result = await ExecuteConditionalInitializeSequenceAsync(
                        sequenceSteps,
                        71,
                        new[] { 72, 73, 74 },
                        ShouldRunSharedRailBeforeInputFeederHome,
                        "InputFeeder/SharedRailX").ConfigureAwait(false);
                }
                else if (stepNo == 91 || stepNo == 92)
                {
                    var pairSteps = plan.Steps.Where(x => x != null && x.Enabled && (x.StepNo == 91 || x.StepNo == 92)).ToList();
                    result = await ExecuteConditionalInitializePairAsync(
                        pairSteps,
                        91,
                        92,
                        ShouldRunSharedRailBeforeOutputFeederHome,
                        "OutputFeeder/SharedRailX").ConfigureAwait(false);
                }
                else if (steps.Count == 1)
                {
                    result = await ExecuteInitializeSingleStepAsync(steps[0]).ConfigureAwait(false);
                }
                else
                {
                    int[] results = await Task.WhenAll(steps.Select(ExecuteInitializeSingleStepAsync)).ConfigureAwait(false);
                    result = results.FirstOrDefault(x => x != 0);
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

                int prepareHomeResult = await PrepareAxisHomeConditionAsync(axis).ConfigureAwait(false);
                if (prepareHomeResult != 0)
                    return prepareHomeResult;

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

                int completeHomeResult = await CompleteAxisHomeConditionAsync(axis).ConfigureAwait(false);
                if (completeHomeResult != 0)
                    return completeHomeResult;

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

        private async Task<int> PrepareAxisHomeConditionAsync(BaseAxis axis)
        {
            try
            {
                if (axis == null)
                    return 0;

                QMC.Common.Log.Write("Main", "SYSTEM", "PrepareAxisHome",
                    "Prepare axis home condition. axis=" + axis.Name + " - Start");

                switch (axis.Name)
                {
                    case "InputLifterZ":
                        return await PrepareInputLifterZHomeAsync(axis).ConfigureAwait(false);
                    case "OutputLifterZ":
                        return await PrepareOutputLifterZHomeAsync(axis).ConfigureAwait(false);
                    case "InputFeederY":
                        return await PrepareInputFeederYHomeByAxisAsync(axis).ConfigureAwait(false);
                    case "OutputFeederY":
                        return await PrepareOutputFeederYHomeByAxisAsync(axis).ConfigureAwait(false);
                    case "CameraX":
                    case "InputVisionX":
                    case "FrontPickerX":
                    case "RearPickerX":
                    case "OutputVisionX":
                        return await PrepareSharedRailXAxisHomeAsync(axis).ConfigureAwait(false);
                    case "InputExpandingZ":
                        return await PrepareInputExpandingZHomeAsync(axis).ConfigureAwait(false);
                    case "InputStageY":
                        return await PrepareInputStageYHomeAsync(axis).ConfigureAwait(false);
                    case "NeedleX":
                        return await PrepareNeedleXHomeAsync(axis).ConfigureAwait(false);
                    default:
                        return await PrepareDefaultAxisHomeAsync(axis).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                return FailInitializePreparation("축 HOME 사전 조건 준비 실패. axis=" +
                    (axis != null ? axis.Name : "-") + ", error=" + ex.Message);
            }
            finally
            {
                lock (_initializeHomedAxisLock)
                {
                    _initializeHomedAxisNames = null;
                }
            }
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
                    case "GoodStage_StageZ":
                        return await MoveOutputStageZToAvoidAsync().ConfigureAwait(false);
                    case "FrontPickerY":
                        return await MoveFrontPickerYToAvoidAfterHomeAsync().ConfigureAwait(false);
                    case "RearPickerY":
                        return await MoveRearPickerYToAvoidAfterHomeAsync().ConfigureAwait(false);
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

        private async Task<int> PrepareInputLifterZHomeAsync(BaseAxis axis)
        {
            return await PrepareInputLifterHomeAsync().ConfigureAwait(false);
        }

        private Task<int> PrepareOutputLifterZHomeAsync(BaseAxis axis)
        {
            // TODO: OutputLifterZ HOME 전 필요한 실린더/피더/도어 조건을 현장 기준으로 추가하세요.
            return Task.FromResult(0);
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

        private async Task<int> PrepareNeedleHomeAsync()
        {
            Log("[INIT] Prepare NeedleX home: NeedleZ Safe(Avoid) move.");

            var stage = _machine.InputStageUnit;
            if (stage == null)
                return 0;

            int result = await MoveInputNeedleZSafeToAvoidAsync().ConfigureAwait(false);
            if (result != 0)
                return result;

            //if (!stage.IsNeedleZInSafePosition())
            //{
            //    return FailInitializePreparation(
            //        "NeedleX HOME 불가: NeedleZ가 Avoid 위치에 있지 않습니다.");
            //}

            return 0;
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

                bool inputPairExecuted = false;
                bool outputPairExecuted = false;
                foreach (var batch in enabledSteps
                    .Where(x => x.StepNo != 71 && x.StepNo != 72 && x.StepNo != 73 && x.StepNo != 74 && x.StepNo != 91 && x.StepNo != 92)
                    .GroupBy(x => x.StepNo))
                {
                    if (!inputPairExecuted && batch.Key >= 80)
                    {
                        int conditionalResult = await ExecuteConditionalInitializeSequenceAsync(
                            enabledSteps,
                            71,
                            new[] { 72, 73, 74 },
                            ShouldRunSharedRailBeforeInputFeederHome,
                            "InputFeeder/SharedRailX").ConfigureAwait(false);
                        if (conditionalResult != 0)
                            return conditionalResult;

                        inputPairExecuted = true;
                    }

                    if (!outputPairExecuted && batch.Key >= 100)
                    {
                        int conditionalResult = await ExecuteConditionalInitializePairAsync(
                            enabledSteps,
                            91,
                            92,
                            ShouldRunSharedRailBeforeOutputFeederHome,
                            "OutputFeeder/SharedRailX").ConfigureAwait(false);
                        if (conditionalResult != 0)
                            return conditionalResult;

                        outputPairExecuted = true;
                    }

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
                            "Axis initialize parallel batch start. step=" + batch.Key +
                            ", groups=" + string.Join(",", batchSteps.Select(x => x.GroupName).ToArray()) + " - Start");

                        int[] results = await Task.WhenAll(batchSteps.Select(ExecuteInitializeSingleStepAsync)).ConfigureAwait(false);
                        for (int i = 0; i < results.Length; i++)
                        {
                            if (results[i] != 0)
                                return results[i];
                        }

                        QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeSteps",
                            "Axis initialize parallel batch completed. step=" + batch.Key + " - Ok");
                    }
                }

                if (!inputPairExecuted)
                {
                    int result = await ExecuteConditionalInitializeSequenceAsync(
                        enabledSteps,
                        71,
                        new[] { 72, 73, 74 },
                        ShouldRunSharedRailBeforeInputFeederHome,
                        "InputFeeder/SharedRailX").ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                if (!outputPairExecuted)
                {
                    int result = await ExecuteConditionalInitializePairAsync(
                        enabledSteps,
                        91,
                        92,
                        ShouldRunSharedRailBeforeOutputFeederHome,
                        "OutputFeeder/SharedRailX").ConfigureAwait(false);
                    if (result != 0)
                        return result;
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
                    AlarmManager.Raise(AlarmSeverity.Warning, "INIT-STEP-EMPTY", "MachineController", LastActionFailureMessage);
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

                int completeResult = await CompleteInitializeStepAsync(step).ConfigureAwait(false);
                if (completeResult != 0)
                {
                    RaiseAxisInitializeStepProgress(step, AxisInitializeStepStatus.Failed, LastActionFailureMessage);
                    return completeResult;
                }

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

            if (string.Equals(progress.Status, AxisInitializeStepStatus.Complete, StringComparison.OrdinalIgnoreCase))
            {
                string reason;
                if (!CheckAxisInitializeStepStillValid(step, out reason))
                {
                    progress.Status = AxisInitializeStepStatus.ReinitializeRequired;
                    progress.Message = reason;
                    SetAxisInitializeStepProgress(progress);
                }
            }

            return progress;
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

        private async Task<int> ExecuteConditionalInitializeSequenceAsync(
            IList<AxisInitializeStep> steps,
            int firstStepNo,
            int[] orderedSecondStepNos,
            Func<bool> shouldRunSecondFirst,
            string relationName)
        {
            try
            {
                if (steps == null)
                    return 0;

                AxisInitializeStep first = steps.FirstOrDefault(x => x != null && x.Enabled && x.StepNo == firstStepNo);
                var secondSteps = (orderedSecondStepNos ?? new int[0])
                    .SelectMany(stepNo => steps
                        .Where(x => x != null && x.Enabled && x.StepNo == stepNo)
                        .OrderBy(x => x.GroupName)
                        .ToList())
                    .ToList();

                if (first == null && secondSteps.Count == 0)
                    return 0;

                bool secondFirst = shouldRunSecondFirst != null && shouldRunSecondFirst();
                string secondOrder = string.Join("->", (orderedSecondStepNos ?? new int[0]).Select(x => x.ToString()).ToArray());
                QMC.Common.Log.Write("Main", "SYSTEM", "ExecuteInitializeConditionalSequence",
                    "Conditional initialize sequence order selected. relation=" + relationName +
                    ", order=" + (secondFirst ? secondOrder + "->" + firstStepNo : firstStepNo + "->" + secondOrder) +
                    " - Start");

                if (secondFirst)
                {
                    int secondResult = await ExecuteInitializeOrderedStepsAsync(secondSteps).ConfigureAwait(false);
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
                string groupName = step != null ? step.GroupName : "";
                if (string.Equals(groupName, "SharedRailX", StringComparison.OrdinalIgnoreCase))
                    return await MoveSharedRailAxesToAvoidAsync().ConfigureAwait(false);
                if (string.Equals(groupName, "RearPickerX", StringComparison.OrdinalIgnoreCase))
                    return await MoveSharedRailAxesToAvoidAsync().ConfigureAwait(false);
                if (string.Equals(groupName, "OutputStageZ", StringComparison.OrdinalIgnoreCase))
                    return await MoveOutputStageZToAvoidAsync().ConfigureAwait(false);

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
        private Task<int> PrepareInputLifterHomeAsync()
        {
            Log("[INIT] Prepare InputLifter home: check input cassette wafer protrusion.");

            var inputCassette = _machine.InputCassetteUnit;
            if (inputCassette == null)
                return Task.FromResult(0);

            if (inputCassette.IsWaferProtrusionDetected())
            {
                // Todo: Ring 감지 시 초기화 방법 재확인
                return Task.FromResult(FailInitializePreparation(
                    "InputLifter HOME 불가: Input Cassette 웨이퍼 돌출(Ring Jut)이 감지되었습니다."));
            }

            return Task.FromResult(0);
        }

        private Task<int> PrepareInputExpandingHomeAsync()
        {
            Log("[INIT] Prepare InputExpanding home: check wafer stage touch sensor.");

            var visionUnit = _machine.VisionUnit;
            if (visionUnit == null)
                return Task.FromResult(0);

            // Todo : Wafer 데이터로 확인
            if (visionUnit.IsVisionWaferStageTouchSensor(true))
            {
                return Task.FromResult(FailInitializePreparation(
                    "InputExpandingZ HOME 불가: Wafer Stage Touch Sensor가 감지되었습니다."));
            }

            return Task.FromResult(0);
        }

        private async Task<int> PrepareInputStageHomeAsync()
        {
            Log("[INIT] Prepare InputStageY home: NeedleZ Safe(Avoid) move.");

            var stage = _machine.InputStageUnit;
            if (stage == null)
                return 0;

            int result = await MoveInputNeedleZSafeToAvoidAsync().ConfigureAwait(false);
            if (result != 0)
                return result;

            //if (!stage.IsNeedleZInSafePosition())
            //{
            //    return FailInitializePreparation(
            //        "InputStageY HOME 불가: NeedleZ가 Safe 위치에 있지 않습니다.");
            //}

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

        private async Task<int> PrepareInputFeederHomeAsync()
        {
            Log("[INIT] Prepare InputFeeder home: X axes Avoid, feeder unclamp/up.");

            int result = await MoveInputSafeXAxesToAvoidAsync().ConfigureAwait(false);
            if (result != 0)
                return result;

            var feeder = _machine.InputFeederUnit;
            if (feeder == null)
                return 0;

            if (!feeder.IsWaferFeederUnclamp())
            {
                result = await feeder.SetWaferFeederClamp(false).ConfigureAwait(false);
                if (result != 0)
                    return FailInitializePreparation("InputFeeder unclamp failed. result=" + result);
            }

            if (feeder.IsWaferFeederUp())
            {
                result = await feeder.SetWaferFeederUpDown(false).ConfigureAwait(false);
                if (result != 0)
                    return FailInitializePreparation("InputFeeder lift up failed. result=" + result);
            }

            if (feeder.IsWaferFeederRingCheck())
            {
                // Todo: Ring 감지 시 초기화 방법 재확인
                return FailInitializePreparation("InputFeeder Ring Check.");
            }

            return 0;
        }

        private async Task<int> PrepareOutputFeederHomeAsync()
        {
            Log("[INIT] Prepare OutputFeeder home: OutputStage/Vision Avoid, feeder unclamp/up.");

            int result = await MoveOutputStageToAvoidForFeederHomeAsync().ConfigureAwait(false);
            if (result != 0)
                return result;

            var feeder = _machine.OutputFeederUnit;
            if (feeder == null)
                return 0;

            if (!feeder.IsFeederUnclamped())
            {
                result = await feeder.SetBinFeederClamp(false).ConfigureAwait(false);
                if (result != 0)
                    return FailInitializePreparation("OutputFeeder unclamp failed. result=" + result);
            }

            if (!feeder.IsFeederUp())
            {
                result = await feeder.SetBinFeederUpDown(true).ConfigureAwait(false);
                if (result != 0)
                    return FailInitializePreparation("OutputFeeder lift up failed. result=" + result);
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
                bool ok = await stage.GoodStage.MoveToAvoidPositionAsync().ConfigureAwait(false);
                if (!ok)
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
                    bool ok = await cylinder.MoveFwdAsync().ConfigureAwait(false);
                    return ok ? 0 : FailInitializePreparation("실린더 전진 실패. cylinder=" + cylinder.Name);
                }

                if (string.Equals(command, AxisInitializeActionCommand.CylinderBwd, StringComparison.OrdinalIgnoreCase))
                {
                    bool ok = await cylinder.MoveBwdAsync().ConfigureAwait(false);
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

        /// <summary>장비 START: Servo ON 후 현재 구성된 자동 시퀀스를 시작합니다.</summary>
        public async Task<int> StartAsync()
        {
            try
            {
                LastActionFailureMessage = "";

                if (_status == EquipmentStatus.Alarm)
                {
                    LastActionFailureMessage = "Alarm 상태에서는 START를 수행할 수 없습니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync", "Start failed: alarm status is active. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Warning, "START-ALARM", "MachineController", "Alarm 상태에서는 START를 수행할 수 없습니다.");
                    Log("[START] failed: alarm status is active");
                    return -1;
                }

                if (IsSequenceRunning)
                {
                    LastActionFailureMessage = "Sequence가 이미 실행 중입니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync", "Start failed: sequence is already running. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Warning, "START-RUNNING", "MachineController", "Sequence가 이미 실행 중입니다.");
                    Log("[START] failed: sequence is already running");
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("StartAsync"))
                    return -1;

                foreach (var ax in EnumerateAxes())
                    ax.ServoOn();

                Log("[START] Servo ON complete. Input auto sequence start.");
                QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync", "Input auto sequence start requested. - Ok");

                await StartSingleUnitAsync(
                    QMC.CDT320.Sequencing.SequenceUnitKind.InputLoader,
                    QMC.CDT320.Sequencing.SequenceRunMode.Auto).ConfigureAwait(false);

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

        /// <summary>?뺤?: ?꾩옱 ?숈옉 以묒씤 異??뺤? + ?쒕낫 ?곹깭???좎?.
        /// 吏꾪뻾 以?LOT ???덉쑝硫?誘몄쭊???ㅼ씠瑜?Skipped 移댁슫?몃줈 湲곕줉?섍퀬 LOT JSON ???</summary>
        public Task StopAsync()
        {
            CancelManualOperation();

            // 1) 紐⑤뱺 異??뺤? (?쒕낫???좎?)
            foreach (var ax in EnumerateAxes()) ax.Stop();

            // 2) ?�이?�이 진행 중이?�면 cancel ???�제 LOT 마무리는 CycleRunAsync ??
            //    catch (OperationCanceledException) ?먯꽌 SkippedCount ? ?④퍡 泥섎━??
            bool wasCycling = (_status == EquipmentStatus.AutoRunning);
            _cycleStopRequested = false;
            _cycleResumePending = false;
            _cycleCts?.Cancel();

            // 3) 吏꾪뻾 ?곹깭 濡쒓렇 媛뺥솕 + LOT 留덈Т由?(CloseLot)
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
            return Task.CompletedTask;
        }

        public IDisposable EnterManualOperation()
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

        /// <summary>CYCLE RUN: ?먮룞 ?ъ씠???쒖옉. ?ㅼ씠留?紐⑤뱶(UseDieMapMode=true)???뚮뒗
        /// Input ?�이맵의 IsTarget=true ?�이�?모두 처리. totalDies&lt;=0 ?�는 ?�이�?모드???�는
        /// EnsureDieMaps ???쒖꽦 ?ㅼ씠 ?섍? ?먮룞 ?곸슜??</summary>
        /// <summary>吏?뺥븳 ?듭뀡?쇰줈 蹂묐젹 ?쒗??Coordinator瑜??쒖옉?⑸땲??</summary>
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

                _autoCts = new CancellationTokenSource();
                var bus = new QMC.CDT320.Sequencing.SequenceSignalBus();
                _seqContext = new QMC.CDT320.Sequencing.MachineSequenceContext(this, bus);
                _coordinator = new QMC.CDT320.Sequencing.AutoSequenceCoordinator(_seqContext);

                _coordinator.Register(
                    QMC.CDT320.Sequencing.SequenceUnitKind.InputLoader,
                    () => new QMC.CDT320.Sequencing.InputSequence(_seqContext));

                _coordinator.Configure(options);
                ActiveSequenceRunMode = options.Mode;
                SetStatus(options.Mode == QMC.CDT320.Sequencing.SequenceRunMode.Auto
                    ? EquipmentStatus.AutoRunning
                    : EquipmentStatus.ManualRunning);
                Log("[SEQ] StartSequenceAsync units=" + options.Units + ", mode=" + options.Mode);
                QMC.Common.Log.Write("Main", "SYSTEM", "StartSequenceAsync",
                    "Sequence start. units=" + options.Units + ", mode=" + options.Mode + " - Ok");

                var coordinator = _coordinator;
                var cts = _autoCts;
                _coordinatorTask = Task.Run(async () =>
                {
                    try
                    {
                        await coordinator.RunAsync(cts.Token).ConfigureAwait(false);
                        if (!cts.IsCancellationRequested && _status != EquipmentStatus.Alarm)
                        {
                            Log("[SEQ] Complete");
                            SetStatus(EquipmentStatus.Ready);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Log("[SEQ] Canceled");
                    }
                    catch (Exception ex)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "StartSequenceAsync",
                            "Sequence failed: " + ex.Message + " - Failed");
                        AlarmManager.Raise(AlarmSeverity.Error, "SEQ-EX", "MachineController", "Sequence failed: " + ex.Message);
                        Log("[SEQ] failed: " + ex.Message);
                        SetStatus(EquipmentStatus.Alarm);
                    }
                    finally
                    {
                        if (_coordinator == coordinator)
                        {
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
                QMC.Common.Log.Write("Main", "SYSTEM", "StartSequenceAsync",
                    "Sequence start failed: " + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-START-EX", "MachineController", "Sequence start failed: " + ex.Message);
                Log("[SEQ] start failed: " + ex.Message);
                SetStatus(EquipmentStatus.Alarm);
                throw;
            }
            finally
            {
            }
        }

        /// <summary>?ㅽ뻾 以묒씤 蹂묐젹 ?쒗?ㅻ? 以묐떒?섍퀬 Coordinator 醫낅즺瑜??湲고빀?덈떎.</summary>
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
            QMC.CDT320.Sequencing.SequenceResumeStore.ClearAll();
            if (_autoCts != null)
            {
                _autoCts.Dispose();
                _autoCts = null;
            }

            if (_status == EquipmentStatus.ManualRunning || _status == EquipmentStatus.AutoRunning)
                SetStatus(EquipmentStatus.Stopped);
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

                QMC.Common.Log.Write("Main", "SYSTEM", "StopSequenceForAlarm",
                    "Sequence stopped by alarm. code=" + alarmCode + " - Ok");
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

        /// <summary>吏?뺥븳 ?좊떅?ㅼ쓣 Manual 紐⑤뱶濡??쒖옉?⑸땲??</summary>
        public Task StartManualAsync(QMC.CDT320.Sequencing.SequenceUnitKind units)
        {
            return StartSequenceAsync(new QMC.CDT320.Sequencing.SequenceRunOptions
            {
                Units = units,
                Mode = QMC.CDT320.Sequencing.SequenceRunMode.Manual
            });
        }

        /// <summary>吏?뺥븳 ?⑥씪 ?좊떅??吏???ㅽ뻾 紐⑤뱶濡??쒖옉?⑸땲??</summary>
        public Task StartSingleUnitAsync(
            QMC.CDT320.Sequencing.SequenceUnitKind unit,
            QMC.CDT320.Sequencing.SequenceRunMode mode)
        {
            return StartSequenceAsync(QMC.CDT320.Sequencing.SequenceRunOptions.Single(unit, mode));
        }

        /// <summary>Manual ?먮뒗 Step 紐⑤뱶?먯꽌 吏???좊떅??1?④퀎 吏꾪뻾?쒗궢?덈떎.</summary>
        public void ManualStep(QMC.CDT320.Sequencing.SequenceUnitKind unit)
        {
            if (_coordinator == null)
            {
                Log("[SEQ] ManualStep ignored: coordinator ?놁쓬");
                return;
            }

            _coordinator.StepUnit(unit);
        }

        public async Task CycleRunAsync(int totalDies = -1)
        {
            if (!EnsureMachineInitializedForRun("CycleRunAsync"))
                return;

            if (_status == EquipmentStatus.AutoRunning) { Log("[CYCLE] already running"); return; }
            // Ready/Running ?몄뿉??Stopped ?먯꽌 ?ъ떆???덉슜 (CYCLE STOP ???ш컻)
            if (_status != EquipmentStatus.Ready &&
                _status != EquipmentStatus.ManualRunning &&
                _status != EquipmentStatus.Stopped)
            {
                Log("[CYCLE] Init is required before run. status=" + _status);
                return;
            }
            if (_status == EquipmentStatus.Stopped)
            {
                Log("[CYCLE] Stopped ?곹깭?먯꽌 ?ш컻");
                // CYCLE STOP / STOP ? Servo OFF ?쒗궎吏 ?딆쓬 ??Servo ?ъ떆??遺덊븘??
            }

            bool resumeCycle = _cycleResumePending &&
                               CycleTotal > 0 &&
                               CycleDone > 0 &&
                               CycleDone < CycleTotal;

            // R3 ???ㅼ씠留?紐⑤뱶: Input ?ㅼ씠留듭쓽 ?쒖꽦 ?ㅼ씠 ?섎? totalDies 濡??ъ슜
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
            SetStatus(EquipmentStatus.AutoRunning);
            Log("[CYCLE] Start (total=" + totalDies + ", lot=" + lotId + ")");

            // Stage 41 ??SECS/HSMS ?ъ씠???쒖옉 ?대깽??
            try { SecsHost?.RaiseEvent("CycleStart", lotId, totalDies.ToString()); } catch { }

            // Stage 45 ??Tower Lamp ?뱀깋 (?댁쟾 以?
            try { _machine.OpPanelUnit?.TowerLampRunning(); } catch { }

            try
            {
                if (!resumeCycle)
                {
                    // ?ъ씠???쒖옉 ??泥??⑥씠??濡쒗듃?ы듃 吏꾩엯
                    bool loaded = await LoadNextWaferAsync();
                    if (!loaded)
                    {
                        Log("[CYCLE] First wafer load from lot port failed. Continue cycle in dry/default mode.");
                    }
                }

                // ???ъ씠??= PickersPerCycle 媛??ㅼ씠 ?숈떆 泥섎━ (4 picker ?숈떆)
                int pickers = System.Math.Min(System.Math.Max(PickersPerCycle, 1), 4);
                int totalCycles = (totalDies + pickers - 1) / pickers;
                Log("[CYCLE] " + totalCycles + " cycles 횞 " + pickers + " pickers = " + totalDies + " dies");

                int startCycle = resumeCycle ? System.Math.Max(0, CycleDone / pickers) : 0;
                for (int cyc = startCycle; cyc < totalCycles; cyc++)
                {
                    if (_cycleCts.Token.IsCancellationRequested) break;
                    int dieBase = cyc * pickers;
                    int diesInCycle = System.Math.Min(pickers, totalDies - dieBase);
                    await DoOneDieAsync(cyc, totalCycles, _cycleCts.Token);  // ?ъ씠???몃뜳??+ total ?꾨떖
                    CycleDone = System.Math.Min(totalDies, (cyc + 1) * pickers);
                    RaiseProgress();
                    // R3 ??Manual 紐⑤뱶: 1 ?ъ씠??泥섎━ ???먮룞 ?뺤?
                    if (CycleMode == CycleMode.Manual)
                    {
                        Log("[CYCLE] Manual mode. Stop after one die batch. done=" + CycleDone);
                        break;
                    }
                }

                // ?ъ씠??醫낅즺 ???쇰뜑 ?꾪눜
                await RetractCurrentWaferAsync();

                // Stage 28 ??InputStage ?⑥씠???몃줈??
                await UnloadInputStageWaferAsync();

                Log("[CYCLE] ?꾨즺 (good=" + GoodCount + ", ng=" + NgCount + ")");
                LotStorage.CloseLot(aborted: false);
                _cycleResumePending = false;
                _cycleStopRequested = false;
                // Stage 45 ??Tower Lamp OFF (?ъ씠???뺤긽 ?꾨즺)
                try { _machine.OpPanelUnit?.TowerLampOff(); } catch { }
                // Stage 41 ??SECS/HSMS ?ъ씠???꾨즺 ?대깽??(Yield ?ы븿)
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
                    // Skipped 移댁슫??= ?쒖옉 ??totalDies - ?ㅼ젣 泥섎━??ProcessedDies
                    int skipped = System.Math.Max(0, totalDies - CycleDone);
                    if (LotStorage.ActiveLot != null)
                    {
                        LotStorage.ActiveLot.SkippedCount = skipped;
                    }
                    Log("[CYCLE] 以묐떒 (good=" + GoodCount + ", ng=" + NgCount + ", skipped=" + skipped + ")");
                    // Tower Lamp OFF (?ъ씠??以묐떒)
                    try { _machine.OpPanelUnit?.TowerLampOff(); } catch { }
                    LotStorage.CloseLot(aborted: true);
                    SetStatus(EquipmentStatus.Stopped);
                }
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "CYCLE-EX", "MachineController", ex.Message);
                Log("[CYCLE ERROR] " + ex.Message);
                LotStorage.CloseLot(aborted: true);
                SetStatus(EquipmentStatus.Alarm);
            }
        }

        /// <summary>CYCLE STOP: ?꾩옱 ?ъ씠?대쭔 以묐떒. 媛쒕퀎 異??뺤? ?놁쓬.</summary>
        public Task CycleStopAsync()
        {
            if (_cycleCts != null)
            {
                _cycleStopRequested = true;
                _cycleResumePending = true;
                _cycleCts.Cancel();
                Log("[CYCLE STOP] requested");
            }
            return Task.CompletedTask;
        }

        // ??????????????????????????????????????????
        //  ?ъ씠??1??遺??숈옉 (媛꾨떒 ?쒕?)
        // ??????????????????????????????????????????

        /// <summary>???�이?�당 가공할 ?�이 ?? 1400 = 300mm ?�이??????처리 ???�음 ?�롯.</summary>
        public int DiesPerWafer { get; set; } = 1400;

        /// <summary>Stage 33 ??留?N ?ㅼ씠留덈떎 Collet Cleaning ?쒗??(0?대㈃ 鍮꾪솢??.</summary>
        public int DiesPerColletClean { get; set; } = 200;

        /// <summary>Stage 39 ????踰덉뿉 ?숈떆 ?쎌뾽??picker ??(1~4). 4 picker ?숈떆 泥섎━.</summary>
        public int PickersPerCycle { get; set; } = 4;

        /// <summary>Stage 40 ??Dual Arm 紐⑤뱶: 吏앹닔 ?ㅼ씠??LeftArm, ????ㅼ씠??RightArm ?쇰줈 援먮?.</summary>
        public bool DualArmMode { get; set; } = false;

        /// <summary>Stage 41 ??SecsHost 李몄“ (?ъ씠???대깽???≪떊??.</summary>
        public QMC.CDT320.Secs.SecsHost SecsHost { get; set; }

        /// <summary>
        /// ???ъ씠??= PickersPerCycle (default 4) 媛??ㅼ씠 ?숈떆 泥섎━.
        /// ?몄옄 cycleIdx ???ъ씠??踰덊샇 (0..totalCycles-1). ?ㅼ젣 ?ㅼ씠 踰덊샇??cycleIdx*pickers ~ cycleIdx*pickers+pickers-1.
        /// </summary>
        // ??????????????????????????????????????????
        //  Stage wafer vision capture helper.
        //  Capture dies for the current cycle while the arm stays clear.
        // ??????????????????????????????????????????
        private static double ResolveAxisDefaultVelocity(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.DefaultVelocity > 0.0
                ? axis.Config.DefaultVelocity
                : 100.0;
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

                // 移대찓??X / ?⑥씠??Stage Y ?숈떆 ?대룞 (媛?die ??X,Y ???뺣젹)
                //   CameraX  = CameraOriginX + WaferAlignOffsetX + die.X
                //   StageY   = StageYTeachPosition + WaferAlignOffsetY + die.Y
                stage.Recipe.EnsurePositionObjects();
                double camXTarget = stage.Recipe.VisionX.ReadyPosition
                                  + stage.WaferAlignOffsetX
                                  + d.X;
                double stageYTarget = stage.Recipe.WaferY.ReadyPosition
                                    + stage.WaferAlignOffsetY
                                    + d.Y;
                try
                {
                    await Task.WhenAll(
                        SharedRailXMotionRuntime.MoveAxisAsync(stage.CameraX, camXTarget, ResolveAxisDefaultVelocity(stage.CameraX)),
                        stage.StageY.MoveAbsoluteAsync(stageYTarget, ResolveAxisDefaultVelocity(stage.StageY))
                    );
                }
                catch (Exception ex) { Log($"[CAPTURE-XY p{p}] ex: " + ex.Message); }

                Log($"[CAPTURE p{p}] Die seq#{seqIdx} grid({d.GridX},{d.GridY}) " +
                    $"wafer({d.X:F2},{d.Y:F2}) ??CamX={camXTarget:F2}, StageY={stageYTarget:F2}");

                if (!wafer)
                {
                    offsets[p] = (0, 0);
                    // wafer 誘몄뿰寃곗씠?대룄 simulator ?뚮옒?쒕뒗 ?≪떊 (?쒓컖 ?뺤씤??
                    SimulatorBridge.Instance?.CameraExposeFlash("WAFER");
                    await Task.Delay(200, ct).ContinueWith(_ => { });
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

                // ?ㅼ쓬 ?ㅼ씠濡?媛湲???150ms ?湲????뚮옒?쒓? ?쒓컖?곸쑝濡?4踰?援щ텇?섎룄濡?
                await Task.Delay(150, ct).ContinueWith(_ => { });
            }

            Log($"[CAPTURE] Cycle {cycleIdx + 1}: capture complete.");
            return offsets;
        }

        private async Task DoOneDieAsync(int cycleIdx, int totalCycles, CancellationToken ct)
        {
            int pickers = System.Math.Min(System.Math.Max(PickersPerCycle, 1), 4);
            int dieBase = cycleIdx * pickers;
            int index = dieBase;   // 湲곗〈 肄붾뱶 ?명솚 (泥??ㅼ씠 踰덊샇)

            // StepRun 寃뚯씠?????ъ슜??肄쒕갚 false 硫??ъ씠??醫낅즺
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

            // Stage 26 ??�?DiesPerWafer 마다 ?�음 카세???�롯 진행
            //   cycleIdx == 0 ? CycleRunAsync ?먯꽌 LoadNextWaferAsync ?대? ?몄텧?덉쑝誘濡??ㅽ궢
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

            // ?? 4 ?ㅼ씠 媛앹껜 ?앹꽦 + 癒명꽣由ъ뼹 ?깅줉 + JobOrder ??
            var dies = new Die[pickers];
            var pickJobs = new JobOrder[pickers];
            for (int p = 0; p < pickers; p++)
            {
                dies[p] = new Die { Uid = "DIE-" + DateTime.Now.Ticks + "-" + (dieBase + p) };
                MaterialStorage.AddDie(dies[p]);
                pickJobs[p] = new JobOrder { Type = JobType.Pick, DieUid = dies[p].Uid };
                JobQueue.Enqueue(pickJobs[p]);
                JobQueue.MarkRunning(pickJobs[p]);
            }
            // ???1媛?(濡쒓렇/?듦퀎 ??
            var die = dies[0];
            var pickJob = pickJobs[0];

            // Stage 28/61 ??DieMap 紐⑤뱶 + ?쎌뾽 ?쒗???듭뀡 ?곸슜
            //   PickupSequenceGenerator 媛 留뚮뱺 ?뺣젹???쒗?ㅼ뿉??dieBase ~ dieBase+pickers-1 ?щ씪?댁뒪
            int row, col;
            if (UseDieMapMode && _inputDieMap != null)
            {
                // ?쒗?ㅺ? 鍮꾩뼱 ?덉쑝硫?(?듭뀡 蹂寃???RebuildPickupSequence 誘명샇異??? ?ъ깮??
                if (_inputPickupSequence == null || _inputPickupSequence.Count == 0)
                    RebuildPickupSequence();

                int seqLen = _inputPickupSequence?.Count ?? 0;
                for (int i = 0; i < pickers; i++)
                {
                    int seqIdx = dieBase + i;
                    if (seqIdx < 0 || seqIdx >= seqLen) break;
                    var e = _inputPickupSequence[seqIdx];
                    dies[i].WaferIndexX = e.GridX;
                    dies[i].WaferIndeY = e.GridY;
                    dies[i].X = e.X;
                    dies[i].Y = e.Y;
                }
                // InputStage ?대룞? ????ㅼ씠 (泥?picker) 湲곗?
                row = dies[0].WaferIndeY;
                col = dies[0].WaferIndexX;
            }
            else
            {
                int colsAssumed = 10;
                row = dieBase / colsAssumed;
                col = dieBase % colsAssumed;
            }
            await MoveInputStageToDieAsync(row, col);

            // Stage 40 ??Dual Arm 모드: 짝수 idx ??LeftArm, ?�??idx ??RightArm
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

            // Stage 58 ???댁쁺 ?듦퀎: Front collet + Needle 4 picker ?ъ슜
            Collet1UseCount += pickers;
            NeedleUseCount += pickers;

            // 蹂???좎뼵 + Servo ON
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

            // ?? Stage 61 ???뚯씠?꾨씪??wafer 鍮꾩쟾 寃곌낵 ?섏떊 ??????????????????
            //   1) ArmY ??AvoidPosition (wafer ?곸뿭 ?몃? ?湲?
            //   2) Pending capture ?덉쑝硫?await ???놁쑝硫??숆린 罹≪쿂
            //   3) await capture completion.
            //   4) ArmY ??PickupPosition + ArmX ??ArmInputPositionX ?숈떆 吏꾩엯
            try
            {
                await front.ArmY.MoveAbsoluteAsync(
                    front.Setup.ArmYAvoidPosition, front.Recipe.ArmYVelocity);
            }
            catch (Exception ex) { Log("[ARM-Y avoid] ex: " + ex.Message); }

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
            double pickX = front.Setup?.ArmInputPositionX ?? 300.0;
            try
            {
                await Task.WhenAll(
                    SharedRailXMotionRuntime.MoveAxisAsync(front.ArmX, pickX, front.Recipe?.ArmXVelocity ?? 2000.0),
                    front.ArmY.MoveAbsoluteAsync(front.Setup.ArmYPickupPosition,
                                                 front.Recipe.ArmYVelocity)
                );
            }
            catch (Exception ex)
            {
                Log("[ARM-XY entry] ex: " + ex.Message);
                JobQueue.MarkFailed(pickJob, "ArmX/Y entry exception");
                NgCount++; PickFailCount++;
                return;
            }
            ct.ThrowIfCancellationRequested();

            // ?? B. PICK 猷⑦봽 (picker 0?? ?쒖감) ????????????????????????????????
            for (int p = 0; p < pickers; p++)
            {
                if (!inspPass[p]) { pickupOk[p] = false; continue; }  // vision NG picker skip

                var d = dies[p];
                var picker = front.Pickers[p];
                var vo = visionOffsets[p];
                try
                {
                    // ??3異??숈떆 ?대룞
                    double armXTarget =
                        front.Setup.ArmInputPositionX
                        + p * front.Setup.PickerPitchX
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

                    await Task.WhenAll(
                        SharedRailXMotionRuntime.MoveAxisAsync(front.ArmX, armXTarget, front.Recipe.ArmXVelocity),
                        stage.StageY.MoveAbsoluteAsync(stageYTarget, ResolveAxisDefaultVelocity(stage.StageY)),
                        stage.NeedleBlockX.MoveAbsoluteAsync(needleXTarget, ResolveAxisDefaultVelocity(stage.NeedleBlockX))
                    );

                    // ??Picker Z??(PickupPosition) / Needle Cap Vacuum ON / Picker Vacuum ON ?숈떆
                    var pickerZTask = picker.PickerZ.MoveAbsoluteAsync(
                        picker.Setup.PickupPosition, picker.Recipe.ZVelocity);
                    stage.NeedleVacuum?.On();
                    picker.VacuumOn();
                    await pickerZTask;
                    await Task.Delay(picker.Recipe.VacuumSettleMs, ct);

                    // ??Needle Up / Picker Up (+PickLiftPosition) ?숈떆
                    double needleUpPos = stage.Recipe.EjectPinZ.ReadyPosition + picker.Recipe.PickLiftPosition;
                    double pickerUpPos = picker.Setup.PickupPosition + picker.Recipe.PickLiftPosition;
                    await Task.WhenAll(
                        ej.MoveAbsoluteAsync(needleUpPos, ResolveAxisDefaultVelocity(ej)),
                        picker.PickerZ.MoveAbsoluteAsync(pickerUpPos, picker.Recipe.ZVelocity)
                    );

                    // ??PickLiftWaitMs ?湲?
                    await Task.Delay(picker.Recipe.PickLiftWaitMs, ct);

                    // ??Picker Wait (WaitPosition) / Needle Down (NeedleDownPosition) ?숈떆
                    await Task.WhenAll(
                        picker.PickerZ.MoveAbsoluteAsync(picker.Setup.WaitPosition, picker.Recipe.ZVelocity),
                        ej.MoveAbsoluteAsync(stage.Recipe.EjectPinZ.ReadyPosition, ResolveAxisDefaultVelocity(ej))
                    );

                    pickupOk[p] = !picker.PickerZ.IsAlarm && !ej.IsAlarm;
                }
                catch (Exception ex)
                {
                    Log($"[PICK p{p}] ex: " + ex.Message);
                    pickupOk[p] = false;
                }
            }
            // Needle Cap Vacuum ? picker ?ㅼ씠 ?ㅼ씠瑜??≪? ?꾩뿉???꾩슂 ???ъ씠???앹뿉 OFF
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

            // PICK ????Pickup ?ㅽ뙣??picker 留?inspPass[p] = false 諛섏쁺
            for (int p = 0; p < pickers; p++)
                if (!pickupOk[p]) inspPass[p] = false;

            // ?? Stage 61 ??ArmY ??Place ?앸궇 ?뚭퉴吏 Pickup ?꾩튂 ?좎? ??
            //   (Place ?꾩뿉 紐낆떆?곸쑝濡?Avoid 濡??대룞 ???댁쟾??"PICK ??Avoid" ?쒓굅)
            //   wafer capture ??ArmX 媛 InspectionX 濡??대룞?섎뒗 ?숈븞 ?덉쟾?섍쾶 wafer ?곸뿭 ?묎렐 媛??

            // ?ㅼ쓬 ?ъ씠?댁씠 議댁옱?섎㈃ wafer capture ?쒖옉 (non-await ??Inspect/Place ? 蹂묐젹)
            int nextCycleIdx = cycleIdx + 1;
            if (nextCycleIdx < totalCycles)
            {
                int npickers = pickers;
                _pendingCaptureCycleIdx = nextCycleIdx;
                _pendingCapturePickers = npickers;
                _pendingCaptureTask = CaptureWaferForCycleAsync(nextCycleIdx, npickers, ct);
                Log($"[PIPELINE] Cycle {nextCycleIdx + 1}: background wafer capture start.");
            }

            // 14~18) Bottom+Side 蹂묐젹 ?뚯씠?꾨씪??(?⑥씪 ArmX ?뚯씠?꾨씪?????숈떆 珥ъ쁺)
            //   - 좌표 모델: picker N abs X = ArmX - N*PickerPitchX (Side1X = SideVision1X = 850)
            //   - Step 0~3 = Bottom Expose Picker 0~3 (ArmX = ArmInspectionPositionX + i*pitch)
            //   - Step 2~5 = Side sub-sequence Picker 0~3 (ArmX ?숈씪 ?꾩튂?먯꽌 picker[idx-2] 媛 Side X ?뺣젹)
            //   - 媛?picker Z????Bottom 吏곸쟾, Z????Side ?앹뿉??諛쒖깮.
            BottomVisionOffset[] bottomResults = null;
            SideVisionResult[] sideResults = null;
            bool visionConnected = VisionComm.VisionHub.Inspection != null
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

                        // Bottom 寃곌낵 ??picker offset / inspPass 諛섏쁺
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

                        // Side 寃곌낵 ??inspPass 諛섏쁺
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

            // 19) PLACE ?꾩튂 ?대룞 ??ArmX ?숈떆??4 picker Z ???湲??꾩튂 (Side 寃?????ㅼ뼱 ?щ┝)
            double placeArmX = front.Setup?.ArmOutputPositionX ?? 1200.0;
            try
            {
                var move19 = new System.Collections.Generic.List<Task>();
                move19.Add(SharedRailXMotionRuntime.MoveAxisAsync(front.ArmX, placeArmX,
                                                                  front.Recipe?.ArmXVelocity ?? 2000.0));
                for (int p = 0; p < pickers; p++)
                    move19.Add(front.Pickers[p].PickerZ.MoveAbsoluteAsync(
                        front.Pickers[p].Setup.WaitPosition,
                        front.Pickers[p].Recipe.ZVelocity));
                await Task.WhenAll(move19);
            }
            catch (Exception ex) { Log("[PLACE] 19) move ex: " + ex.Message); }
            ct.ThrowIfCancellationRequested();

            // ?�?� PlaceOnePickerAsync : picker p �?지??stage (Good/Ng) ??Place ?�?�
            async Task PlaceOnePickerAsync(int p, StageModule outStage)
            {
                var picker = front.Pickers[p];
                var bo = (bottomResults != null && p < bottomResults.Length) ? bottomResults[p] : null;
                double offX = bo?.OffsetX ?? 0.0;
                double offY = bo?.OffsetY ?? 0.0;
                double offT = bo?.OffsetT ?? 0.0;

                // ??ArmX (PlaceX + Bottom OffsetX) / Stage Y (HomeY + Bottom OffsetY) / PickerT (Bottom OffsetT) ?숈떆
                await Task.WhenAll(
                    SharedRailXMotionRuntime.MoveAxisAsync(front.ArmX, placeArmX + offX,
                                                           front.Recipe?.ArmXVelocity ?? 2000.0),
                    outStage.StageY.MoveAbsoluteAsync(outStage.Recipe.HomePositionY + offY,
                                                     ResolveAxisDefaultVelocity(outStage.StageY)),
                    picker.PickerT.MoveAbsoluteAsync(offT, picker.Recipe.ThetaVelocity)
                );

                // ??Picker Z ?ㅼ슫 (PlacePosition)
                await picker.PickerZ.MoveAbsoluteAsync(picker.Setup.PlacePosition,
                                                       picker.Recipe.ZVelocity);

                // ??Vacuum Off + Blow On
                picker.VacuumOff();
                picker.BlowOn();

                // ??Place Delay (Recipe)
                await Task.Delay(picker.Recipe.PlaceDelayMs, ct);

                // ??Blow Off
                picker.BlowOff();

                // ??Picker Up (WaitPosition)
                await picker.PickerZ.MoveAbsoluteAsync(picker.Setup.WaitPosition,
                                                       picker.Recipe.ZVelocity);
            }

            // 20) Good ?ㅼ씠 癒쇱? Place (GoodStage)
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

            // 21) NG ?ㅼ씠 Place ??Stage Z ?꾩튂 ?꾪솚 ??NgStage ??Place
            bool hasNg = false;
            for (int p = 0; p < pickers; p++) if (!inspPass[p]) { hasNg = true; break; }
            if (hasNg)
            {
                try
                {
                    // GoodStage Z 異⑸룎 ?뚰뵾濡??섍컯, NgStage Z ?묒뾽 ?꾩튂濡??곸듅
                    await Task.WhenAll(
                        _machine.OutputStageUnit.GoodStage.StageZ.MoveAbsoluteAsync(
                            _machine.OutputStageUnit.GoodStage.Recipe.AvoidPositionZ,
                            ResolveAxisDefaultVelocity(_machine.OutputStageUnit.GoodStage.StageZ)),
                        _machine.OutputStageUnit.NgStage.StageZ.MoveAbsoluteAsync(
                            _machine.OutputStageUnit.NgStage.Recipe.WorkPositionZ,
                            ResolveAxisDefaultVelocity(_machine.OutputStageUnit.NgStage.StageZ))
                    );
                }
                catch (Exception ex) { Log("[PLACE] Bin ?꾪솚 ex: " + ex.Message); }

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

            // ?? Stage 61 ??Place 醫낅즺 ??ArmY ??AvoidPosition ?뚰뵾 (?ㅼ쓬 ?ъ씠??capture ?덉쟾 ?곸뿭) ??
            try
            {
                await front.ArmY.MoveAbsoluteAsync(
                    front.Setup.ArmYAvoidPosition, front.Recipe.ArmYVelocity);
                Log($"[ARM-Y] Cycle {cycleIdx + 1}: Place complete. Return to Avoid position.");
            }
            catch (Exception ex) { Log("[ARM-Y avoid after PLACE] ex: " + ex.Message); }

            // 寃곌낵 諛섏쁺 ??4 ?ㅼ씠 媛곴컖 inspPass[p] 湲곕컲?쇰줈 Good/NG 遺꾨━ 湲곕줉
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
            }

            // Stage 27 ??留?WafersPerOutputBatch ?ㅼ씠留덈떎 Output 移댁꽭???곸옱
            //   ?ㅼ씠 踰좎씠??+ pickers 媛 WafersPerOutputBatch 寃쎄퀎 ?섏쑝硫??몃━嫄?
            int diesProcessedTotal = dieBase + pickers;
            if (WafersPerOutputBatch > 0 &&
                (diesProcessedTotal / WafersPerOutputBatch) > (dieBase / WafersPerOutputBatch))
            {
                Log($"[FEEDER] {WafersPerOutputBatch} dies complete. Store to output.");
                // ?ъ씠?댁쓽 ?ㅼ닔 寃곌낵濡?wafer ?곸옱 遺꾨쪟 (Good ?곗꽭 = Good)
                bool anyGood = false; foreach (var ip in inspPass) if (ip) { anyGood = true; break; }
                await StoreCompletedWaferAsync(anyGood);
            }

            // Stage 33 ??留?DiesPerColletClean ?ㅼ씠留덈떎 Collet Cleaning
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
            // Reject 분리 ??�??�이�?검??
            for (int p = 0; p < pickers; p++)
            {
                if (SubPortMaterialRejector.ShouldReject(dies[p], out double rxX, out double rxY))
                {
                    Log($"[DIE {dieBase + p + 1}] reject -> ({rxX:F1},{rxY:F1})  bin={dies[p].BinCode}");
                }
            }
            Log($"[CYCLE {cycleIdx + 1}] dies {dieBase + 1}~{dieBase + pickers}/{CycleTotal} good={goodInCycle} ng={ngInCycle}");
        }

        // ??????????????????????????????????????????
        //  ?몃━ ?쒗쉶
        // ??????????????????????????????????????????

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

        // ??????????????????????????????????????????
        //  ?곹깭/濡쒓렇
        // ??????????????????????????????????????????

        private void SetStatus(EquipmentStatus s)
        {
            if (_status == s) return;
            _status = s;
            var h = StatusChanged;
            if (h != null) try { h(s); } catch { }
        }

        private void Log(string msg)
        {
            var h = LogMessage;
            if (h != null) try { h(msg); } catch { }
        }

        /// <summary>?몃? ?쒗??怨꾩링?먯꽌 ?λ퉬 濡쒓렇瑜?湲곕줉?섍린 ?꾪븳 怨듦컻 濡쒓렇 釉뚮━吏?낅땲??</summary>
        public void LogPublic(string msg)
        {
            Log(msg);
        }

        private void RaiseProgress()
        {
            var h = CycleProgress;
            if (h != null) try { h(CycleDone, CycleTotal); } catch { }
        }
    }
}

