using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Motion;
using QMC.CDT320.Alarms;
using QMC.CDT320.Bin;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Jobs;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;

namespace QMC.CDT320
{
    /// <summary>장비 운전 상태.</summary>
    public enum MachineStatus
    {
        /// <summary>대기.</summary>
        Idle,

        /// <summary>초기화(Home) 진행 중.</summary>
        Initializing,

        /// <summary>초기화 완료, 운전 준비 완료.</summary>
        Ready,

        /// <summary>수동 운전(시작) 중 — 개별 모션만 수행.</summary>
        Running,

        /// <summary>자동 CYCLE 진행 중.</summary>
        Cycling,

        /// <summary>정지 완료.</summary>
        Stopped,

        /// <summary>알람 발생.</summary>
        Alarm
    }

    /// <summary>
    /// 작업 탭의 초기화/시작/정지/CYCLE RUN/STOP 버튼을 실제 장비 동작으로 연결하는 컨트롤러.
    /// <para>
    /// CDT-320은 Input(Loader/Stage) → FRONT/REAR Picker(TransferPicker의 Left/Right Arm) → Output 파이프라인.
    /// 각 액션은 CDT320_Machine 트리를 순회하며 실제 Axis/DO/DI를 조작.
    /// </para>
    /// </summary>
    public class MachineController
    {
        private readonly CDT320_Machine _machine;
        private MachineStatus _status = MachineStatus.Idle;
        private CancellationTokenSource _cycleCts;
        private CancellationTokenSource _autoCts;
        private Task _coordinatorTask;
        private QMC.CDT320.Sequencing.MachineSequenceContext _seqContext;
        private QMC.CDT320.Sequencing.AutoSequenceCoordinator _coordinator;

        public event Action<MachineStatus> StatusChanged;
        public event Action<string>        LogMessage;
        public event Action<int, int>      CycleProgress;  // (done, total)

        /// <summary>CDT-320 하드웨어 유닛 트리입니다.</summary>
        public CDT320_Machine Machine => _machine;

        public MachineStatus Status => _status;
        public int CycleTotal  { get; private set; }
        public int CycleDone   { get; private set; }
        public int GoodCount   { get; private set; }
        public int NgCount     { get; private set; }

        // ──────────────────────────────────────────
        //  Wafer 다이맵 — Input(원형 300mm) / Output(사각 트레이)
        //  사이클 시작 시 EnsureDieMaps 로 한 번 생성, 이후 캐싱.
        // ──────────────────────────────────────────
        /// <summary>웨이퍼 직경 [mm].</summary>
        public double WaferDiameterMm { get; set; } = 300.0;
        /// <summary>다이 X 사이즈 [mm].</summary>
        public double DieSizeXMm { get; set; } = 8.12;
        /// <summary>다이 Y 사이즈 [mm].</summary>
        public double DieSizeYMm { get; set; } = 6.12;
        /// <summary>Input 다이맵 칩 간격 [mm] (웨이퍼 픽업 측).</summary>
        public double InputGapMm  { get; set; } = 0.05;
        /// <summary>Output 다이맵 칩 간격 [mm] (BIN 트레이 측).</summary>
        public double OutputGapMm { get; set; } = 0.30;

        private QMC.CDT320.DieMaps.DieMap _inputDieMap;
        private QMC.CDT320.DieMaps.DieMap _outputDieMap;
        /// <summary>Input 다이맵 (원형 300mm). null 이면 EnsureDieMaps() 호출 필요.</summary>
        public QMC.CDT320.DieMaps.DieMap InputDieMap  => _inputDieMap;
        /// <summary>Output 다이맵 (사각 트레이). null 이면 EnsureDieMaps() 호출 필요.</summary>
        public QMC.CDT320.DieMaps.DieMap OutputDieMap => _outputDieMap;
        /// <summary>true 면 사이클이 InputDieMap.IsTarget=true 다이 좌표를 사용.</summary>
        public bool UseDieMapMode { get; set; } = true;

        // Stage 61 — Pickup sequence options + cached sequence
        /// <summary>웨이퍼 다이 픽업 순서 옵션 (Recipe.Pickup 에서 set).</summary>
        public QMC.CDT320.Recipes.PickupSubset PickupOptions { get; set; } =
            new QMC.CDT320.Recipes.PickupSubset();

        /// <summary>옵션 기반 정렬된 픽업 시퀀스 (활성 다이만, 순서대로). EnsureDieMaps 또는 RebuildPickupSequence 호출 시 갱신.</summary>
        private List<QMC.CDT320.DieMaps.DieMapEntry> _inputPickupSequence
            = new List<QMC.CDT320.DieMaps.DieMapEntry>();

        // Stage 61 — Pipelined wafer capture state
        //   다음 사이클의 wafer vision 촬영을 현 사이클 Inspect/Place 와 병렬로 실행.
        //   완료 시 CameraZ 가 안전 위치까지 상승해 있어야 ArmY 가 픽업 영역 진입 가능.
        private Task<(double X, double Y)[]> _pendingCaptureTask;
        private int _pendingCaptureCycleIdx = -1;
        private int _pendingCapturePickers  = 0;

        public IReadOnlyList<QMC.CDT320.DieMaps.DieMapEntry> InputPickupSequence
            => _inputPickupSequence;

        /// <summary>PickupOptions 또는 _inputDieMap 변경 후 호출. 시퀀스 재생성.</summary>
        public void RebuildPickupSequence()
        {
            if (_inputDieMap == null) { _inputPickupSequence.Clear(); return; }
            _inputPickupSequence = QMC.CDT320.DieMaps.PickupSequenceGenerator.Build(
                _inputDieMap, PickupOptions);
            Log("[PICKSEQ] " + PickupOptions.StartCorner + " / " +
                PickupOptions.Direction + " / " + PickupOptions.Pattern +
                " → 활성 다이 " + _inputPickupSequence.Count + "개 순서 결정");
        }

        /// <summary>Input/Output 다이맵을 (없으면) 생성. 이미 있으면 재사용.</summary>
        public void EnsureDieMaps()
        {
            if (_inputDieMap == null)
            {
                _inputDieMap = QMC.CDT320.DieMaps.DieMapGenerator.GenerateWafer(
                    WaferDiameterMm, DieSizeXMm, DieSizeYMm, InputGapMm, InputGapMm,
                    frameObjId: "INPUT");
                int active = 0;
                foreach (var e in _inputDieMap.Entries) if (e.IsTarget) active++;
                Log("[DIEMAP] Input wafer " + WaferDiameterMm + "mm · die " +
                    DieSizeXMm + "x" + DieSizeYMm + " · gap " + InputGapMm +
                    " → grid " + _inputDieMap.GridX + "x" + _inputDieMap.GridY +
                    " · active=" + active);
                // UI 시각화 위해 LotStorage 에 등록
                QMC.CDT320.Lots.LotStorage.ActiveInputDieMap = _inputDieMap;
            }
            if (_outputDieMap == null)
            {
                // Output 사각 트레이 — Input 활성 다이 수 이상 수용
                int active = 0;
                foreach (var e in _inputDieMap.Entries) if (e.IsTarget) active++;
                // 정사각 격자 root(N) 올림
                int side = (int)System.Math.Ceiling(System.Math.Sqrt(active));
                _outputDieMap = QMC.CDT320.DieMaps.DieMapGenerator.GenerateRect(
                    side, side, DieSizeXMm, DieSizeYMm, OutputGapMm, OutputGapMm,
                    frameObjId: "OUTPUT");
                Log("[DIEMAP] Output tray die " + DieSizeXMm + "x" + DieSizeYMm +
                    " · gap " + OutputGapMm + " → grid " + side + "x" + side +
                    " · slots=" + (side * side));
            }

            // WafersPerOutputBatch <= 0 이면 OutputDieMap 슬롯 수에 자동 맞춤
            if (WafersPerOutputBatch <= 0 && _outputDieMap != null)
            {
                WafersPerOutputBatch = _outputDieMap.Entries.Count;
                Log("[DIEMAP] WafersPerOutputBatch 자동 설정 = " + WafersPerOutputBatch + " (Output 슬롯 수)");
            }

            // Stage 61 — 픽업 시퀀스 생성 (옵션 적용)
            RebuildPickupSequence();
        }

        // ──────────────────────────────────────────
        //  Stage 58 — 운영 통계 (Work Info / Work Time)
        //  : WorkMainPage / OperationPanelStatusPage 에서 폴링.
        //  internal setter — Cycle 실행 중에 누적, Init 시 리셋.
        // ──────────────────────────────────────────
        /// <summary>PICK 실패 누적 수 (재시도 후에도 실패로 카운트되는 경우).</summary>
        public int PickFailCount   { get; internal set; }
        /// <summary>PLACE 실패 누적 수 (Output 슬롯 placement vision NG).</summary>
        public int PlaceFailCount  { get; internal set; }
        /// <summary>FRONT (LEFT ARM) Collet 사용 횟수 — Pick 1회당 +1.</summary>
        public int Collet1UseCount { get; internal set; }
        /// <summary>REAR (RIGHT ARM) Collet 사용 횟수.</summary>
        public int Collet2UseCount { get; internal set; }
        /// <summary>EjectPin/Needle 사용 횟수 (다이 1개당 1회).</summary>
        public int NeedleUseCount  { get; internal set; }
        /// <summary>알람 누적 발생 수.</summary>
        public int ErrorCount      { get; internal set; }
        /// <summary>정상 다운(STOP/ECMG 외 IDLE) 누적 시간.</summary>
        public TimeSpan NormalDownTime { get; internal set; } = TimeSpan.Zero;
        /// <summary>알람/에러로 인한 다운 누적 시간.</summary>
        public TimeSpan ErrorDownTime  { get; internal set; } = TimeSpan.Zero;
        /// <summary>알람 발생 후 복구까지 걸린 누적 시간.</summary>
        public TimeSpan RecoveryTime   { get; internal set; } = TimeSpan.Zero;
        /// <summary>Mean Time Between Failure (rolling).</summary>
        public TimeSpan Mtbf           { get; internal set; } = TimeSpan.Zero;
        /// <summary>Mean Time To Recovery (rolling).</summary>
        public TimeSpan Mttr           { get; internal set; } = TimeSpan.Zero;

        // ──────────────────────────────────────────
        //  로트포트 (Input Cassette) 진행 상태
        // ──────────────────────────────────────────

        /// <summary>현재 InputLoader 가 처리 중인 슬롯 인덱스 (0-base). -1 = 미장착/언로드 상태.</summary>
        public int  CurrentInputSlot { get; private set; } = -1;
        /// <summary>현재 슬롯의 웨이퍼가 InputStage 교환 위치까지 이송되었는지 여부.</summary>
        public bool InputWaferAtExchange { get; private set; } = false;
        /// <summary>로트포트 시퀀스 진행 시점에 발행 (UI 갱신용).</summary>
        public event Action LotPortStateChanged;

        /// <summary>InitAsync 후 카세트 자동 매핑 여부 (기본 false — INIT 은 모터 초기화만).
        /// 카세트 스캔은 작업정보/Output 페이지의 매핑 버튼으로 별도 수행.</summary>
        public bool AutoScanCassetteOnInit { get; set; } = false;

        public MachineController(CDT320_Machine machine)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        }

        // ──────────────────────────────────────────
        //  로트포트 시퀀스 헬퍼
        // ──────────────────────────────────────────

        /// <summary>
        /// InputLoader 의 다음 웨이퍼를 InputStage 교환 위치까지 자동 진행.<br/>
        /// 1) WaferMap 이 비어 있으면 ScanCassetteAsync 로 매핑<br/>
        /// 2) <see cref="CurrentInputSlot"/> 다음의 웨이퍼 보유 슬롯으로 ElevatorZ 이동<br/>
        /// 3) MoveToExchangePositionAsync 호출 (피더 하강 → 클램프 → Y 전진)
        /// </summary>
        /// <returns>다음 웨이퍼 이송 성공 시 true. 카세트 비었거나 인터락 차단 시 false.</returns>
        public async Task<bool> LoadNextWaferAsync()
        {
            var loader = _machine.InputLoader;

            // 카세트 안착 확인
            if (!loader.CassetteExistSensor.IsOn)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LOT-NOCASS",
                    loader.Name, "Input 카세트가 감지되지 않습니다.");
                Log("[LOTPORT] InputCassette absent — load skipped");
                return false;
            }

            // 매핑 미수행 시 스캔
            if (loader.WaferMap == null || loader.WaferMap.Count == 0)
            {
                Log("[LOTPORT] WaferMap empty → scan cassette");
                bool scanned = (await loader.ScanCassetteAsync(16, 6.0)) == 0;
                if (!scanned)
                {
                    AlarmManager.Raise(AlarmSeverity.Warning, "LOT-SCAN",
                        loader.Name, "카세트 스캔 실패.");
                    return false;
                }
                RaiseLotPortChanged();
            }

            // 다음 웨이퍼 슬롯 탐색
            int next = -1;
            for (int s = CurrentInputSlot + 1; s < loader.WaferMap.Count; s++)
            {
                if (loader.WaferMap[s]) { next = s; break; }
            }
            if (next < 0)
            {
                Log("[LOTPORT] No more wafer in input cassette");
                return false;
            }

            // 이동 + 교환 위치 전진
            double slotPitch = 6.0;
            //double targetZ = loader.Setup.FirstSlotPosition + next * slotPitch;
            double targetZ = loader.IndexCassette.Recipe.FirstSlotPosition + next * slotPitch;
            Log($"[LOTPORT] Move to slot {next} (Z={targetZ:F2}mm)");
            try
            {
                int moveResult = await loader.MoveToTargetSlotAsync(targetZ);
                if (moveResult != 0)
                    return false;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "LOT-MOVE",
                    loader.Name, "슬롯 이동 실패: " + ex.Message);
                Log("[LOTPORT ERROR] " + ex.Message);
                return false;
            }

            bool ex2 = await loader.MoveToExchangePositionAsync();
            if (!ex2)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "LOT-EX",
                    loader.Name, "교환 위치 이송 실패.");
                return false;
            }

            CurrentInputSlot     = next;
            InputWaferAtExchange = true;
            RaiseLotPortChanged();
            Log($"[LOTPORT] LoadNextWafer OK — slot={next}");

            // Stage 34 — Sim 모드: 소비된 슬롯을 false 로 마킹 (UI LED 정확도)
            //   Form1.CassetteDriver 는 internal 이지만, 동일 어셈블리이므로 reflection 없이 접근 가능
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

            // Stage 28 — 산업 흐름 반영 InputStage handoff 시퀀스:
            //   1. (이미 완료) 피더가 ExchangePosition(150mm) 으로 전진 — 웨이퍼 InputStage 입구
            //   2. InputStage.LoadAndPrepareWaferAsync — ExpanderZ 클램프, Wafer 받음
            //      WaferLoaderAdapter 는 피더 ≥140mm 이므로 Safe 판정
            //   3. (별도 호출) RetractFeeder — 피더 홈 복귀
            //   4. InputStage.VisionAlignAndSetupOriginAsync — 정렬 + Origin 확정
            try
            {
                Log("[LOTPORT] InputStage handoff (LoadAndPrepare) 시작...");
                bool handoff = await _machine.InputStage.LoadAndPrepareWaferAsync();
                Log("[LOTPORT] InputStage handoff " + (handoff ? "OK" : "WARN"));

                // Stage 58 — 문서 정합: InputStage 시퀀스 실패 시 AlarmManager.Raise 보강.
                // (이전: Console.WriteLine 만 — UI 알람 배너/히스토리에 미반영)
                if (!handoff)
                {
                    AlarmManager.Raise(AlarmSeverity.Warning, "IS-LOAD",
                        _machine.InputStage.Name,
                        "LoadAndPrepareWafer 실패 (피더 안전위치/ExpanderZ/바코드/맵 중 하나).");
                    ErrorCount++;
                }

                if (handoff)
                {
                    // 피더 후퇴 (웨이퍼는 이미 InputStage 가 잡고 있음)
                    Log("[LOTPORT] 피더 후퇴 (InputStage 단독 작업으로 전환)...");
                    await loader.RetractFeederAsync();
                    InputWaferAtExchange = false;
                    RaiseLotPortChanged();

                    // VisionAlign + Origin 확정
                    Log("[INPUTSTAGE] VisionAlign 시작...");
                    bool aligned = await _machine.InputStage.VisionAlignAndSetupOriginAsync();
                    Log("[INPUTSTAGE] VisionAlign " + (aligned ? "OK" : "WARN (sim 한계)"));

                    if (!aligned)
                    {
                        AlarmManager.Raise(AlarmSeverity.Warning, "IS-ALIGN",
                            _machine.InputStage.Name,
                            "VisionAlignAndSetupOrigin 실패 (Vision 통신 또는 StageT 알람).");
                        ErrorCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("[LOTPORT] InputStage handoff exception: " + ex.Message);
                AlarmManager.Raise(AlarmSeverity.Error, "IS-EXCEPTION",
                    _machine.InputStage.Name, ex.Message);
                ErrorCount++;
            }

            return true;
        }

        // ──────────────────────────────────────────
        //  Stage 28 — InputStage 사이클 통합 헬퍼
        // ──────────────────────────────────────────

        /// <summary>다이 1개 픽업을 위해 StageY/CameraX 를 이동.</summary>
        public async Task<bool> MoveInputStageToDieAsync(int row, int col)
        {
            try
            {
                var stage = _machine.InputStage;
                // Stage 28 — Origin + Pitch 가 확정되었으면 정확히 이동, 아니면 추정값
                double targetX = stage.OriginX + col * (stage.PitchX > 0 ? stage.PitchX : 0.15);
                double targetY = stage.OriginY + row * (stage.PitchY > 0 ? stage.PitchY : 0.15);
                bool xOk = await MoveAxisAsync(stage.CameraX, targetX, 100.0);
                bool yOk = await MoveAxisAsync(stage.StageY,  targetY, 100.0);
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

        /// <summary>InputStage 의 웨이퍼 언로드 시퀀스 (사이클 종료 시).</summary>
        public async Task<bool> UnloadInputStageWaferAsync()
        {
            try
            {
                Log("[INPUTSTAGE] UnloadWafer 시작...");
                bool ok = await _machine.InputStage.UnloadWaferAsync();
                Log("[INPUTSTAGE] UnloadWafer " + (ok ? "OK" : "WARN"));
                return ok;
            }
            catch (Exception ex)
            {
                Log("[INPUTSTAGE] UnloadWafer exception: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 현재 InputStage 교환 위치의 피더를 후퇴시켜 빈 카세트 슬롯에 복귀.<br/>
        /// 사이클 종료 또는 다음 슬롯 진행 전 호출.
        /// </summary>
        public async Task<bool> RetractCurrentWaferAsync()
        {
            var loader = _machine.InputLoader;
            if (!InputWaferAtExchange)
            {
                Log("[LOTPORT] Retract skipped — feeder not at exchange position");
                return true;
            }
            bool ok = await loader.RetractFeederAsync();
            if (!ok)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "LOT-RET",
                    loader.Name, "피더 후퇴 실패.");
                return false;
            }
            InputWaferAtExchange = false;
            RaiseLotPortChanged();
            Log("[LOTPORT] RetractCurrentWafer OK");
            return true;
        }

        // ──────────────────────────────────────────
        //  Stage 27 — OutputUnloader Feeder 시퀀스
        // ──────────────────────────────────────────

        /// <summary>현재 Output 카세트별 다음 적재 슬롯 인덱스 (0-base).</summary>
        public int OutputSlotNg    { get; private set; } = 0;
        public int OutputSlotGood1 { get; private set; } = 0;
        public int OutputSlotGood2 { get; private set; } = 0;
        /// <summary>몇 다이당 OutputUnloader.StoreFullWafer 호출 (배치 크기).
        /// 0 = EnsureDieMaps 에서 OutputDieMap 슬롯 수로 자동 설정.</summary>
        public int WafersPerOutputBatch { get; set; } = 0;

        /// <summary>Place 완료된 웨이퍼를 Output Cassette 의 적절한 슬롯에 적재.</summary>
        public async Task<bool> StoreCompletedWaferAsync(bool isGood)
        {
            var unloader = _machine.OutputUnloader;
            // 카세트 선정: Good → Good1 우선, 가득 차면 Good2; NG → Ng
            QMC.CDT320.TargetCassette target;
            int slot;
            if (isGood)
            {
                if (OutputSlotGood1 < 25) { target = QMC.CDT320.TargetCassette.Good1; slot = OutputSlotGood1++; }
                else if (OutputSlotGood2 < 25) { target = QMC.CDT320.TargetCassette.Good2; slot = OutputSlotGood2++; }
                else
                {
                    // Stage 27 fix — 카세트 가득 = 사이클 자동 정지
                    AlarmManager.Raise(AlarmSeverity.Error, "OUT-FULL-GOOD",
                        unloader.Name, "Good 카세트 모두 가득 — 사이클 자동 정지.");
                    Log("[FEEDER] Good cassette full → CycleStop");
                    _cycleCts?.Cancel();
                    return false;
                }
            }
            else
            {
                if (OutputSlotNg < 25) { target = QMC.CDT320.TargetCassette.Ng; slot = OutputSlotNg++; }
                else
                {
                    // Stage 27 fix — NG 카세트 가득 = 사이클 자동 정지
                    AlarmManager.Raise(AlarmSeverity.Error, "OUT-FULL-NG",
                        unloader.Name, "NG 카세트 가득 — 사이클 자동 정지.");
                    Log("[FEEDER] NG cassette full → CycleStop");
                    _cycleCts?.Cancel();
                    return false;
                }
            }
            Log($"[FEEDER] StoreFullWafer → {target} Slot[{slot}]");
            try
            {
                bool ok = await unloader.StoreFullWaferAsync(target, slot);
                if (!ok)
                {
                    AlarmManager.Raise(AlarmSeverity.Error, "OUT-STORE",
                        unloader.Name, "StoreFullWafer 실패.");
                    return false;
                }
                Log($"[FEEDER] OK — {target} Slot[{slot}] 적재 완료");

                // Stage 37 — SimCassetteDriver Output 슬롯 업데이트 (UI LED 동기화)
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
                    unloader.Name, ex.Message);
                return false;
            }
        }

        /// <summary>Output 3 카세트 슬롯 매핑 (UI 버튼용).</summary>
        public async Task<bool> ScanOutputCassettesAsync()
        {
            var unloader = _machine.OutputUnloader;
            try
            {
                bool ok = await unloader.ScanAllCassettesAsync();
                if (ok) Log("[FEEDER] Output 3 카세트 스캔 완료");
                return ok;
            }
            catch (Exception ex)
            {
                Log("[FEEDER] OutputScan exception: " + ex.Message);
                return false;
            }
        }

        // ──────────────────────────────────────────

        /// <summary>InputLoader 의 카세트 슬롯 매핑만 수행 (UI 버튼용).</summary>
        public async Task<bool> ScanInputCassetteAsync()
        {
            var loader = _machine.InputLoader;
            if (!loader.CassetteExistSensor.IsOn)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LOT-NOCASS",
                    loader.Name, "Input 카세트가 감지되지 않습니다.");
                return false;
            }
            bool ok = (await loader.ScanCassetteAsync(16, 6.0)) == 0;
            if (ok)
            {
                CurrentInputSlot = -1;  // 매핑 후 슬롯 재탐색 위해 리셋
                // Stage 46 — SlotMapperRegistry 갱신
                try
                {
                    var arr = new bool[loader.WaferMap.Count];
                    for (int i = 0; i < arr.Length; i++) arr[i] = loader.WaferMap[i];
                    SlotMapperRegistry.Update("InputCassette", arr);
                }
                catch { }
                RaiseLotPortChanged();
            }
            return ok;
        }

        private void RaiseLotPortChanged()
        {
            var h = LotPortStateChanged;
            if (h != null) try { h(); } catch { }
        }

        // ──────────────────────────────────────────
        //  Stage 32 — 설비 수준 라이프사이클
        // ──────────────────────────────────────────

        /// <summary>설비 정상 종료 시퀀스. 사이클 정지 + 축 Stop + Lot 정리.</summary>
        public async Task ShutdownAsync()
        {
            Log("[SHUTDOWN] 설비 정상 종료 시퀀스 시작...");
            try
            {
                _cycleCts?.Cancel();
                await Task.Delay(500);
                foreach (var ax in EnumerateAxes()) ax.Stop();
                // Shutdown 정책: 모든 축 Servo OFF (사용자 요구 — 프로그램 종료 시 허용된 자동 OFF)
                foreach (var ax in EnumerateAxes())
                {
                    try { ax.ServoOff(); } catch { }
                }
                LotStorage.CloseLot(aborted: true);
                AppSettingsStore.Save();
                SetStatus(MachineStatus.Stopped);
                Log("[SHUTDOWN] 설비 종료 완료.");
            }
            catch (Exception ex)
            {
                Log("[SHUTDOWN] exception: " + ex.Message);
            }
        }

        /// <summary>RESET ALARM — 모든 축 알람 리셋 + AlarmManager 활성 알람 해제.
        /// 안전 확인 후 호출. 알람 상태일 때만 의미 있음.</summary>
        public Task ResetAlarmAsync()
        {
            Log("[RESET-ALARM] 알람 해제 시퀀스 시작...");
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
                Log("[RESET-ALARM] 완료 (축=" + axisCount + ", 실패=" + axisFail +
                    ", 활성알람=" + activeBefore + " 해제)");

                // 알람 상태였으면 Idle 로 (운영 재개 위해 INIT 필요)
                if (_status == MachineStatus.Alarm) SetStatus(MachineStatus.Idle);

                // Tower Lamp OFF (알람 해제)
                try { _machine.OpPanel?.TowerLampOff(); } catch { }
            }
            catch (Exception ex)
            {
                Log("[RESET-ALARM] exception: " + ex.Message);
            }
            return Task.CompletedTask;
        }

        /// <summary>비상 정지 — 모든 축 EStop + 알람 발생.
        /// R4 — TowerLamp 제어 결과를 명시적으로 로그 (silent catch 제거).</summary>
        public Task EmergencyStopAsync()
        {
            Log("[E-STOP] 비상 정지 시퀀스 시작...");
            try
            {
                _cycleCts?.Cancel();
                int axTotal = 0, axFail = 0;
                foreach (var ax in EnumerateAxes())
                {
                    try { ax.EStop(); axTotal++; }
                    catch (Exception axEx) { axFail++; Log("[E-STOP] 축 EStop 실패: " + axEx.Message); }
                }
                // E-STOP 정책: 모든 축 Servo OFF (사용자 요구 — 비상정지 시 허용된 자동 OFF)
                foreach (var ax in EnumerateAxes())
                {
                    try { ax.ServoOff(); } catch { }
                }
                AlarmManager.Raise(AlarmSeverity.Error, "E-STOP", "Machine",
                    "사용자/안전회로 비상 정지");
                SetStatus(MachineStatus.Alarm);

                // Stage 45 — Tower Lamp 알람 (빨강 + 부저). 명시적 결과 기록.
                if (_machine.OpPanel != null)
                {
                    try
                    {
                        _machine.OpPanel.TowerLampAlarm();
                        Log("[E-STOP] TowerLamp ALARM OK (red + buzzer)");
                    }
                    catch (Exception lampEx)
                    {
                        Log("[E-STOP] TowerLamp 제어 실패: " + lampEx.Message);
                        AlarmManager.Raise(AlarmSeverity.Warning, "TOWER-FAIL", "OpPanel", lampEx.Message);
                    }
                }
                else
                {
                    Log("[E-STOP] OpPanel 미연결 — TowerLamp 제어 생략");
                }
                Log("[E-STOP] 비상 정지 완료 (축=" + axTotal + ", 실패=" + axFail +
                    "). 안전 확인 후 RESET ALARM + INIT 필요.");
            }
            catch (Exception ex)
            {
                Log("[E-STOP] exception: " + ex.Message);
            }
            return Task.CompletedTask;
        }

        // ──────────────────────────────────────────
        //  운영 모드 (DryRun / StepRun) — Stage 13
        // ──────────────────────────────────────────

        /// <summary>true 면 모션 없이 진행만 (Recipe.DryRun 영향).</summary>
        public bool DryRun { get; set; } = false;
        /// <summary>true 면 다이 1개마다 사용자 확인 (Recipe.StepRun 영향). CycleMode.Step 와 동일.</summary>
        public bool StepRun
        {
            get => CycleMode == CycleMode.Step;
            set => CycleMode = value ? CycleMode.Step : CycleMode.Auto;
        }
        /// <summary>R3 — 사이클 운영 모드 (Auto/Manual/Step).</summary>
        public CycleMode CycleMode { get; set; } = CycleMode.Auto;
        /// <summary>StepRun 진행 신호 — 사용자 GUI 가 콜백으로 다음 다이 허용/차단.</summary>
        public event Func<int, bool> StepRunGate;

        /// <summary>현재 활성 RecipeProject 의 DryRun/StepRun 적용.</summary>
        public void ApplyRecipeMode(QMC.CDT320.Recipes.RecipeProject p)
        {
            if (p == null) return;
            DryRun  = p.DryRun;
            StepRun = p.StepRun;
            // Stage 33 — Recipe 의 ModuleSubset 파라미터를 Controller 옵션에 반영
            if (p.Module != null)
            {
                if (p.Module.ColletCleanEnable && p.Module.ColletCleanInterval > 0)
                    DiesPerColletClean = p.Module.ColletCleanInterval;
                else if (!p.Module.ColletCleanEnable)
                    DiesPerColletClean = 0;
            }
            // Stage 54 — Recipe.Output 파라미터 적용
            if (p.Output != null)
            {
                if (p.Output.DiesPerWafer > 0)
                    DiesPerWafer = p.Output.DiesPerWafer;
                if (p.Output.WafersPerOutputBatch > 0)
                    WafersPerOutputBatch = p.Output.WafersPerOutputBatch;
                // Stage 58 — Plate MaxSlots 갱신
                if (p.Output.GoodPlateMaxSlots > 0)
                    PlateRegistry.GoodPlate.MaxSlots = p.Output.GoodPlateMaxSlots;
                if (p.Output.NgPlateMaxSlots > 0)
                    PlateRegistry.NgPlate.MaxSlots = p.Output.NgPlateMaxSlots;
            }
            // Stage 61 — Recipe.Pickup (StartCorner/Direction/Pattern) 적용
            if (p.Pickup != null)
            {
                PickupOptions = p.Pickup;
                RebuildPickupSequence();
                Log($"[MODE] Pickup={p.Pickup.StartCorner}/{p.Pickup.Direction}/{p.Pickup.Pattern}");
            }
            Log($"[MODE] DryRun={DryRun}  StepRun={StepRun}  EbrMode={p.EbrMode}  ColletEvery={DiesPerColletClean}  DiesPerWafer={DiesPerWafer}");
        }

        // ──────────────────────────────────────────
        //  중앙화 모션 (Interlock 검증 후 실제 이동)
        // ──────────────────────────────────────────

        /// <summary>
        /// 인터록 검증 후 절대 위치 이동. 차단되면 false 반환 + 알람.
        /// 310 의 MotionInterlock.OnVerifyToMove 와 동등.
        /// </summary>
        public async Task<bool> MoveAxisAsync(BaseAxis axis, double position, double velocity = 800.0)
        {
            if (axis == null) return false;
            if (!InterlockRegistry.VerifyMove(axis.Name, position, out string reason))
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "INTERLOCK", axis.Name,
                    $"Move blocked: target={position:F1} — {reason}");
                Log($"[INTERLOCK] {axis.Name} → {position:F1} blocked: {reason}");
                return false;
            }
            if (DryRun)
            {
                Log($"[DRYRUN] skip move {axis.Name} → {position:F1} (vel={velocity:F0})");
                return true;
            }
            await axis.MoveAbsoluteAsync(position, velocity);
            return true;
        }

        // ──────────────────────────────────────────
        //  Wafer Alignment (3 점 비전 매칭 → CoordinateMap)
        // ──────────────────────────────────────────

        /// <summary>
        /// 3 점 비전 정렬 — TopLeft / TopRight / BottomLeft 기준점에서 비전 매칭하여
        /// CoordinateMap 갱신 (310 의 DieTapeFrameAlignmentJob 단순화).
        /// </summary>
        /// <param name="motorPts">각 기준점의 모터 좌표 [(mx,my) ×3].</param>
        /// <param name="finder">매칭에 사용할 Finder 이름 (기본 ReticleFinder).</param>
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
                    Log($"[ALIGN] point {i+1}/3 — move motor → ({motorPts[i].mx:F2}, {motorPts[i].my:F2})");
                    // 실제 모션은 운영 환경에서 추가; 시뮬에서는 매칭 호출만
                    var m = await VisionComm.VisionHub.Wafer.MatchAsync(finder, i, 1500);
                    if (!m.Success)
                    {
                        Log($"[ALIGN] point {i+1} match failed: {m.RawError}");
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
                Log($"[ALIGN] OK — scaleX={sx:F4} scaleY={sy:F4} rot={rot:F3}deg  ({coord})");
                return true;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "ALIGN-EX", "MachineController", ex.Message);
                Log("[ALIGN ERROR] " + ex.Message);
                return false;
            }
        }

        // ──────────────────────────────────────────
        //  공용 버튼 액션
        // ──────────────────────────────────────────

        /// <summary>초기화: 모든 축 서보 ON + HOME + 카운터 초기화.</summary>
        public async Task InitAsync()
        {
            if (_status == MachineStatus.Initializing || _status == MachineStatus.Cycling) return;
            SetStatus(MachineStatus.Initializing);
            Log("[INIT] All axes servo ON + HOME search...");

            foreach (var ax in EnumerateAxes())
            {
                ax.ResetAlarm();
                ax.ServoOn();
            }

            // 간단 시뮬: 순차적으로 HomeSearch. 각 Unit의 주요 축부터.
            var axes = new List<BaseAxis>(EnumerateAxes());
            foreach (var ax in axes)
            {
                try { await ax.HomeSearchAsync(); }
                catch (Exception ex)
                {
                    AlarmManager.Raise(AlarmSeverity.Error, "HOME-" + ax.Name, ax.Name, "HOME 실패: " + ex.Message);
                    Log("[ALARM] " + ax.Name + " HOME 실패: " + ex.Message);
                    SetStatus(MachineStatus.Alarm);
                    return;
                }
                if (ax.IsAlarm)
                {
                    AlarmManager.Raise(AlarmSeverity.Error, "HOME-" + ax.Name, ax.Name, "HOME 후 알람 코드 " + ax.AlarmCode);
                    Log("[ALARM] " + ax.Name + " HOME 알람 (code=" + ax.AlarmCode + ")");
                    SetStatus(MachineStatus.Alarm);
                    return;
                }
            }

            CycleDone = 0; CycleTotal = 0; GoodCount = 0; NgCount = 0;

            // Stage 46 — Resource Sensors 사전 검사 (CDA / Vacuum 라인 정상 여부)
            try
            {
                if (_machine.Resources != null && !_machine.Resources.AllOk)
                {
                    Log("[INIT] Resource 경고 — CDA 또는 Vacuum 라인 비정상 (sim 모드에서는 OK 가정)");
                }
            }
            catch { }

            // Stage 26 — Init 후 로트포트 자동 매핑 (옵션)
            if (AutoScanCassetteOnInit)
            {
                try
                {
                    Log("[INIT] InputCassette 자동 매핑 시작...");
                    bool mapped = await ScanInputCassetteAsync();
                    if (mapped)
                    {
                        int n = 0;
                        foreach (var b in _machine.InputLoader.WaferMap) if (b) n++;
                        Log($"[INIT] WaferMap OK ({n}장 감지)");
                    }
                    else
                    {
                        Log("[INIT] WaferMap 미수행 — 카세트 미감지 또는 스캔 실패");
                    }
                }
                catch (Exception ex)
                {
                    Log("[INIT] WaferMap 예외: " + ex.Message);
                }

                // Stage 27 fix — Output 카세트 3개도 자동 스캔
                //   _slotMap 을 25 슬롯으로 사이즈 초기화하여 StoreFullWafer 의
                //   slotMap[target][slotIndex] = true 가 정상 기록되도록 함.
                try
                {
                    Log("[INIT] OutputCassette 자동 매핑 시작...");
                    bool oOk = await ScanOutputCassettesAsync();
                    Log("[INIT] OutputCassette 매핑 " + (oOk ? "OK" : "FAILED"));
                }
                catch (Exception ex)
                {
                    Log("[INIT] OutputCassette 예외: " + ex.Message);
                }
            }

            // R3 — Input/Output 다이맵 생성 (이미 있으면 skip)
            try { EnsureDieMaps(); } catch (Exception dmEx) { Log("[INIT] DieMap 생성 경고: " + dmEx.Message); }

            Log("[INIT] 초기화 완료. Ready.");
            SetStatus(MachineStatus.Ready);
        }

        /// <summary>시작: 서보 ON + 운전 준비 상태로 전환 (개별 모션은 수동 메뉴에서).</summary>
        public Task StartAsync()
        {
            foreach (var ax in EnumerateAxes()) ax.ServoOn();
            Log("[START] 운전 준비.");
            SetStatus(MachineStatus.Running);
            return Task.CompletedTask;
        }

        /// <summary>정지: 현재 동작 중인 축 정지 + 서보 상태는 유지.
        /// 진행 중 LOT 이 있으면 미진행 다이를 Skipped 카운트로 기록하고 LOT JSON 저장.</summary>
        public Task StopAsync()
        {
            // 1) 모든 축 정지 (서보는 유지)
            foreach (var ax in EnumerateAxes()) ax.Stop();

            // 2) 사이클이 진행 중이라면 cancel — 실제 LOT 마무리는 CycleRunAsync 의
            //    catch (OperationCanceledException) 에서 SkippedCount 와 함께 처리됨
            bool wasCycling = (_status == MachineStatus.Cycling);
            _cycleCts?.Cancel();

            // 3) 진행 상태 로그 강화 + LOT 마무리 (CloseLot)
            var lot = QMC.CDT320.Lots.LotStorage.ActiveLot;
            if (wasCycling && lot != null)
            {
                int skipped = System.Math.Max(0, CycleTotal - CycleDone);
                lot.SkippedCount = skipped;
                Log("[STOP] LOT=" + lot.LotID + " 마무리 중 (done=" + CycleDone + "/" + CycleTotal +
                    ", good=" + GoodCount + ", ng=" + NgCount + ", skipped=" + skipped + ")");
                try { QMC.CDT320.Lots.LotStorage.CloseLot(aborted: true); } catch { }
                try { _machine.OpPanel?.TowerLampOff(); } catch { }
            }
            else
            {
                Log("[STOP] 정지.");
            }
            SetStatus(MachineStatus.Stopped);
            return Task.CompletedTask;
        }

        /// <summary>CYCLE RUN: 자동 사이클 시작. 다이맵 모드(UseDieMapMode=true)일 때는
        /// Input 다이맵의 IsTarget=true 다이를 모두 처리. totalDies&lt;=0 또는 다이맵 모드일 때는
        /// EnsureDieMaps 의 활성 다이 수가 자동 적용됨.</summary>
        /// <summary>지정한 옵션으로 병렬 시퀀스 Coordinator를 시작합니다.</summary>
        public async Task StartSequenceAsync(QMC.CDT320.Sequencing.SequenceRunOptions options)
        {
            if (_coordinatorTask != null && !_coordinatorTask.IsCompleted)
                await StopSequenceAsync();

            if (options == null)
                options = QMC.CDT320.Sequencing.SequenceRunOptions.FullAuto();

            // Stage 01 — 시퀀스 실행 컨텍스트와 Coordinator를 새 실행 단위로 구성한다.
            _autoCts = new CancellationTokenSource();
            var bus = new QMC.CDT320.Sequencing.SequenceSignalBus();
            _seqContext = new QMC.CDT320.Sequencing.MachineSequenceContext(this, bus);
            _coordinator = new QMC.CDT320.Sequencing.AutoSequenceCoordinator(_seqContext);

            _coordinator.Register(
                QMC.CDT320.Sequencing.SequenceUnitKind.InputLoader,
                () => new QMC.CDT320.Sequencing.InputLoaderSequence(_seqContext));

            _coordinator.Configure(options);
            Log("[SEQ] StartSequenceAsync units=" + options.Units + ", mode=" + options.Mode);

            _coordinatorTask = Task.Run(async () =>
            {
                await _coordinator.RunAsync(_autoCts.Token).ConfigureAwait(false);
            });
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
            if (_autoCts != null)
            {
                _autoCts.Dispose();
                _autoCts = null;
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

        public async Task CycleRunAsync(int totalDies = -1)
        {
            if (_status == MachineStatus.Cycling) { Log("[CYCLE] 이미 실행 중"); return; }
            // Ready/Running 외에도 Stopped 에서 재시작 허용 (CYCLE STOP 후 재개)
            if (_status != MachineStatus.Ready &&
                _status != MachineStatus.Running &&
                _status != MachineStatus.Stopped)
            {
                Log("[CYCLE] 실행 전 초기화 필요 (status=" + _status + ")");
                return;
            }
            if (_status == MachineStatus.Stopped)
            {
                Log("[CYCLE] Stopped 상태에서 재개");
                // CYCLE STOP / STOP 은 Servo OFF 시키지 않음 — Servo 재시작 불필요
            }

            // R3 — 다이맵 모드: Input 다이맵의 활성 다이 수를 totalDies 로 사용
            try { EnsureDieMaps(); } catch { }
            if (UseDieMapMode && _inputDieMap != null)
            {
                int active = 0;
                foreach (var e in _inputDieMap.Entries) if (e.IsTarget) active++;
                if (totalDies <= 0 || totalDies > active) totalDies = active;
                Log("[CYCLE] DieMap 모드 — Input wafer 활성 다이 " + active + " 중 " + totalDies + "개 처리");
            }
            else if (totalDies <= 0)
            {
                totalDies = 10;   // legacy default
            }
            _cycleCts = new CancellationTokenSource();
            CycleTotal = totalDies; CycleDone = 0; GoodCount = 0; NgCount = 0;
            // Lot 자동 시작
            string lotId = "LOT-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            LotStorage.OpenLot(lotId, "default", totalDies);
            SetStatus(MachineStatus.Cycling);
            Log("[CYCLE] 시작 (total=" + totalDies + ", lot=" + lotId + ")");

            // Stage 41 — SECS/HSMS 사이클 시작 이벤트
            try { SecsHost?.RaiseEvent("CycleStart", lotId, totalDies.ToString()); } catch { }

            // Stage 45 — Tower Lamp 녹색 (운전 중)
            try { _machine.OpPanel?.TowerLampRunning(); } catch { }

            try
            {
                // 사이클 시작 — 첫 웨이퍼 로트포트 진입
                bool loaded = await LoadNextWaferAsync();
                if (!loaded)
                {
                    Log("[CYCLE] 로트포트 첫 웨이퍼 로드 실패 — 사이클 진행 (텔레포트 모드)");
                }

                // 한 사이클 = PickersPerCycle 개 다이 동시 처리 (4 picker 동시)
                int pickers = System.Math.Min(System.Math.Max(PickersPerCycle, 1), 4);
                int totalCycles = (totalDies + pickers - 1) / pickers;
                Log("[CYCLE] " + totalCycles + " cycles × " + pickers + " pickers = " + totalDies + " dies");

                for (int cyc = 0; cyc < totalCycles; cyc++)
                {
                    if (_cycleCts.Token.IsCancellationRequested) break;
                    int dieBase = cyc * pickers;
                    int diesInCycle = System.Math.Min(pickers, totalDies - dieBase);
                    await DoOneDieAsync(cyc, totalCycles, _cycleCts.Token);  // 사이클 인덱스 + total 전달
                    CycleDone = System.Math.Min(totalDies, (cyc + 1) * pickers);
                    RaiseProgress();
                    // R3 — Manual 모드: 1 사이클 처리 후 자동 정지
                    if (CycleMode == CycleMode.Manual)
                    {
                        Log("[CYCLE] Manual 모드 — 1 사이클 처리 후 정지 (done=" + CycleDone + ")");
                        break;
                    }
                }

                // 사이클 종료 — 피더 후퇴
                await RetractCurrentWaferAsync();

                // Stage 28 — InputStage 웨이퍼 언로드
                await UnloadInputStageWaferAsync();

                Log("[CYCLE] 완료 (good=" + GoodCount + ", ng=" + NgCount + ")");
                LotStorage.CloseLot(aborted: false);
                // Stage 45 — Tower Lamp OFF (사이클 정상 완료)
                try { _machine.OpPanel?.TowerLampOff(); } catch { }
                // Stage 41 — SECS/HSMS 사이클 완료 이벤트 (Yield 포함)
                try
                {
                    double yield = totalDies > 0 ? (double)GoodCount / totalDies * 100 : 0;
                    SecsHost?.RaiseEvent("CycleEnd", lotId, GoodCount.ToString(), NgCount.ToString(),
                        yield.ToString("F2"));
                }
                catch { }
                SetStatus(MachineStatus.Ready);
            }
            catch (OperationCanceledException)
            {
                // Skipped 카운트 = 시작 시 totalDies - 실제 처리된 ProcessedDies
                int skipped = System.Math.Max(0, totalDies - CycleDone);
                if (LotStorage.ActiveLot != null)
                {
                    LotStorage.ActiveLot.SkippedCount = skipped;
                }
                Log("[CYCLE] 중단 (good=" + GoodCount + ", ng=" + NgCount + ", skipped=" + skipped + ")");
                // Tower Lamp OFF (사이클 중단)
                try { _machine.OpPanel?.TowerLampOff(); } catch { }
                LotStorage.CloseLot(aborted: true);
                SetStatus(MachineStatus.Stopped);
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "CYCLE-EX", "MachineController", ex.Message);
                Log("[CYCLE ERROR] " + ex.Message);
                LotStorage.CloseLot(aborted: true);
                SetStatus(MachineStatus.Alarm);
            }
        }

        /// <summary>CYCLE STOP: 현재 사이클만 중단. 개별 축 정지 없음.</summary>
        public Task CycleStopAsync()
        {
            if (_cycleCts != null)
            {
                _cycleCts.Cancel();
                Log("[CYCLE STOP] 요청됨");
            }
            return Task.CompletedTask;
        }

        // ──────────────────────────────────────────
        //  사이클 1회 분 동작 (간단 시뮬)
        // ──────────────────────────────────────────

        /// <summary>한 웨이퍼당 가공할 다이 수. 1400 = 300mm 웨이퍼 한 장 처리 후 다음 슬롯.</summary>
        public int DiesPerWafer { get; set; } = 1400;

        /// <summary>Stage 33 — 매 N 다이마다 Collet Cleaning 시퀀스 (0이면 비활성).</summary>
        public int DiesPerColletClean { get; set; } = 200;

        /// <summary>Stage 39 — 한 번에 동시 픽업할 picker 수 (1~4). 4 picker 동시 처리.</summary>
        public int PickersPerCycle { get; set; } = 4;

        /// <summary>Stage 40 — Dual Arm 모드: 짝수 다이는 LeftArm, 홀수 다이는 RightArm 으로 교대.</summary>
        public bool DualArmMode { get; set; } = false;

        /// <summary>Stage 41 — SecsHost 참조 (사이클 이벤트 송신용).</summary>
        public QMC.CDT320.Secs.SecsHost SecsHost { get; set; }

        /// <summary>
        /// 한 사이클 = PickersPerCycle (default 4) 개 다이 동시 처리.
        /// 인자 cycleIdx 는 사이클 번호 (0..totalCycles-1). 실제 다이 번호는 cycleIdx*pickers ~ cycleIdx*pickers+pickers-1.
        /// </summary>
        // ──────────────────────────────────────────
        //  Stage 61 — 파이프라인 wafer 비전 촬영 헬퍼
        //  cycleIdx 의 4 다이를 카메라로 촬영, 마지막에 CameraZ 안전 위치 상승.
        //  ArmY 가 픽업 영역 외부에 있을 때 (= 다른 사이클의 Inspect/Place 진행 중) 호출.
        // ──────────────────────────────────────────
        private async Task<(double X, double Y)[]> CaptureWaferForCycleAsync(
            int cycleIdx, int pickers, CancellationToken ct)
        {
            var stage = _machine.InputStage;
            var offsets = new (double X, double Y)[pickers];
            int dieBase = cycleIdx * pickers;

            try { stage.CameraX?.ServoOn(); stage.CameraZ?.ServoOn(); stage.StageY?.ServoOn(); } catch { }

            // 1) CameraZ → 포커스 위치 (다이 표면)
            try
            {
                await stage.CameraZ.MoveAbsoluteAsync(
                    stage.Setup.CameraFocusZ, stage.Recipe.MoveVelocity);
            }
            catch (Exception ex) { Log("[CAPTURE-Z] focus move ex: " + ex.Message); }

            bool wafer = VisionComm.VisionHub.Wafer != null
                      && VisionComm.VisionHub.Wafer.IsConnected;
            int seqLen = _inputPickupSequence?.Count ?? 0;

            Log($"[CAPTURE] Cycle {cycleIdx + 1} — {pickers} die 촬영 시작 (dieBase={dieBase})");

            for (int p = 0; p < pickers; p++)
            {
                if (ct.IsCancellationRequested) break;
                int seqIdx = dieBase + p;
                if (seqIdx < 0 || seqIdx >= seqLen)
                {
                    Log($"[CAPTURE p{p}] seqIdx {seqIdx} out of range (seqLen={seqLen}) — skip");
                    offsets[p] = (0, 0);
                    continue;
                }
                var d = _inputPickupSequence[seqIdx];

                // 카메라 X / 웨이퍼 Stage Y 동시 이동 (각 die 의 X,Y 에 정렬)
                //   CameraX  = CameraOriginX + WaferAlignOffsetX + die.X
                //   StageY   = StageYTeachPosition + WaferAlignOffsetY + die.Y
                double camXTarget = stage.Setup.CameraOriginX
                                  + stage.WaferAlignOffsetX
                                  + d.X;
                double stageYTarget = stage.Setup.StageYTeachPosition
                                    + stage.WaferAlignOffsetY
                                    + d.Y;
                try
                {
                    await Task.WhenAll(
                        stage.CameraX.MoveAbsoluteAsync(camXTarget, stage.Recipe.MoveVelocity),
                        stage.StageY.MoveAbsoluteAsync(stageYTarget, stage.Recipe.MoveVelocity)
                    );
                }
                catch (Exception ex) { Log($"[CAPTURE-XY p{p}] ex: " + ex.Message); }

                Log($"[CAPTURE p{p}] Die seq#{seqIdx} grid({d.GridX},{d.GridY}) " +
                    $"wafer({d.X:F2},{d.Y:F2}) → CamX={camXTarget:F2}, StageY={stageYTarget:F2}");

                if (!wafer)
                {
                    offsets[p] = (0, 0);
                    // wafer 미연결이어도 simulator 플래시는 송신 (시각 확인용)
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
                        double pxMm = stage.Setup.VisionPixelToMm;
                        offsets[p] = ((m.X - 320) * pxMm, (m.Y - 240) * pxMm);
                        Log($"[CAPTURE p{p}] OK score={m.Score:F2} offset=({offsets[p].X:F3},{offsets[p].Y:F3})mm");
                    }
                    else
                    {
                        offsets[p] = (0, 0);
                        Log($"[CAPTURE p{p}] NG match (score={m.Score:F2}) — offset 0");
                    }
                }
                catch (Exception ex)
                {
                    offsets[p] = (0, 0);
                    Log($"[CAPTURE p{p}] vision ex: " + ex.Message);
                }

                // 다음 다이로 가기 전 150ms 대기 — 플래시가 시각적으로 4번 구분되도록
                await Task.Delay(150, ct).ContinueWith(_ => { });
            }

            // 2) CameraZ → 안전 위치 (이 위치까지 상승해야 ArmY 가 진입 가능)
            try
            {
                await stage.CameraZ.MoveAbsoluteAsync(
                    stage.Setup.CameraSafeZ, stage.Recipe.MoveVelocity);
            }
            catch (Exception ex) { Log("[CAPTURE-Z] safe retract ex: " + ex.Message); }

            Log($"[CAPTURE] Cycle {cycleIdx + 1} 촬영 완료 — CameraZ 안전 위치 도달");
            return offsets;
        }

        private async Task DoOneDieAsync(int cycleIdx, int totalCycles, CancellationToken ct)
        {
            int pickers = System.Math.Min(System.Math.Max(PickersPerCycle, 1), 4);
            int dieBase = cycleIdx * pickers;
            int index = dieBase;   // 기존 코드 호환 (첫 다이 번호)

            // StepRun 게이트 — 사용자 콜백 false 면 사이클 종료
            if (StepRun && StepRunGate != null)
            {
                Log($"[STEPRUN] waiting for user gate (cycle {cycleIdx + 1})...");
                bool proceed = false;
                try { proceed = StepRunGate.Invoke(cycleIdx); } catch { }
                if (!proceed)
                {
                    Log($"[STEPRUN] user denied — stopping cycle");
                    throw new OperationCanceledException("StepRun gate denied");
                }
            }

            // Stage 26 — 매 DiesPerWafer 마다 다음 카세트 슬롯 진행
            //   cycleIdx == 0 은 CycleRunAsync 에서 LoadNextWaferAsync 이미 호출했으므로 스킵
            if (dieBase > 0 && DiesPerWafer > 0 && dieBase % DiesPerWafer == 0 && InputWaferAtExchange)
            {
                Log($"[LOTPORT] {DiesPerWafer} 다이 완료 — 다음 슬롯으로 진행");
                await RetractCurrentWaferAsync();
                bool nextOk = await LoadNextWaferAsync();
                if (!nextOk)
                {
                    Log("[LOTPORT] 다음 웨이퍼 없음 — 사이클은 텔레포트로 계속");
                }
            }

            // ── 4 다이 객체 생성 + 머터리얼 등록 + JobOrder ──
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
            // 대표 1개 (로그/통계 용)
            var die = dies[0];
            var pickJob = pickJobs[0];

            // Stage 28/61 — DieMap 모드 + 픽업 시퀀스 옵션 적용
            //   PickupSequenceGenerator 가 만든 정렬된 시퀀스에서 dieBase ~ dieBase+pickers-1 슬라이스
            int row, col;
            if (UseDieMapMode && _inputDieMap != null)
            {
                // 시퀀스가 비어 있으면 (옵션 변경 후 RebuildPickupSequence 미호출 등) 재생성
                if (_inputPickupSequence == null || _inputPickupSequence.Count == 0)
                    RebuildPickupSequence();

                int seqLen = _inputPickupSequence?.Count ?? 0;
                for (int i = 0; i < pickers; i++)
                {
                    int seqIdx = dieBase + i;
                    if (seqIdx < 0 || seqIdx >= seqLen) break;
                    var e = _inputPickupSequence[seqIdx];
                    dies[i].GridX = e.GridX;
                    dies[i].GridY = e.GridY;
                    dies[i].X     = e.X;
                    dies[i].Y     = e.Y;
                }
                // InputStage 이동은 대표 다이 (첫 picker) 기준
                row = dies[0].GridY;
                col = dies[0].GridX;
            }
            else
            {
                int colsAssumed = 10;
                row = dieBase / colsAssumed;
                col = dieBase % colsAssumed;
            }
            await MoveInputStageToDieAsync(row, col);

            // Stage 40 — Dual Arm 모드: 짝수 idx 는 LeftArm, 홀수 idx 는 RightArm
            var front = (DualArmMode && (index % 2 == 1))
                        ? _machine.TransferPicker.RightArm
                        : _machine.TransferPicker.LeftArm;

            front.ArmX.ServoOn();
            front.ArmY.ServoOn();
            // 4 picker 모두 ServoOn (PickerZ + PickerT)
            for (int p = 0; p < pickers; p++)
            {
                front.Pickers[p].PickerZ.ServoOn();
                front.Pickers[p].PickerT.ServoOn();
            }

            // Stage 58 — 운영 통계: Front collet + Needle 4 picker 사용
            Collet1UseCount += pickers;
            NeedleUseCount  += pickers;

            // 변수 선언 + Servo ON
            bool[] pickupOk = new bool[pickers];
            bool[] inspPass = new bool[pickers];
            var dieOffsets    = new (double X, double Y)[pickers];
            var visionOffsets = new (double X, double Y)[pickers];
            for (int pi = 0; pi < pickers; pi++) inspPass[pi] = true;

            var stage = _machine.InputStage;
            var ej = stage.EjectPinZ;
            ej?.ServoOn();
            stage.StageY?.ServoOn();
            stage.NeedleBlockX?.ServoOn();
            stage.CameraX?.ServoOn();
            stage.CameraZ?.ServoOn();

            // ── Stage 61 — 파이프라인 wafer 비전 결과 수신 ──────────────────
            //   1) ArmY → AvoidPosition (wafer 영역 외부 대기)
            //   2) Pending capture 있으면 await — 없으면 동기 캡처
            //   3) await 끝 시점 = 촬영 완료 + CameraZ 안전 위치 도달
            //   4) ArmY → PickupPosition + ArmX → ArmInputPositionX 동시 진입
            try
            {
                await front.ArmY.MoveAbsoluteAsync(
                    front.Setup.ArmYAvoidPosition, front.Recipe.ArmYVelocity);
            }
            catch (Exception ex) { Log("[ARM-Y avoid] ex: " + ex.Message); }

            (double X, double Y)[] capturedOffsets;
            if (_pendingCaptureTask != null && _pendingCaptureCycleIdx == cycleIdx)
            {
                Log($"[PIPELINE] Cycle {cycleIdx + 1} — 사전 wafer capture 대기 중...");
                capturedOffsets = await _pendingCaptureTask;
                _pendingCaptureTask = null;
                _pendingCaptureCycleIdx = -1;
            }
            else
            {
                Log($"[PIPELINE] Cycle {cycleIdx + 1} — 동기 wafer capture (첫 사이클 또는 desync)");
                capturedOffsets = await CaptureWaferForCycleAsync(cycleIdx, pickers, ct);
            }

            for (int p = 0; p < pickers && p < capturedOffsets.Length; p++)
            {
                visionOffsets[p] = capturedOffsets[p];
                dieOffsets[p].X += visionOffsets[p].X;
                dieOffsets[p].Y += visionOffsets[p].Y;
            }

            // 8) ArmY → Pickup + ArmX → ArmInputPositionX (CameraZ 가 안전 위치에 있는 시점)
            double pickX = front.Setup?.ArmInputPositionX ?? 300.0;
            try
            {
                await Task.WhenAll(
                    front.ArmX.MoveAbsoluteAsync(pickX, front.Recipe?.ArmXVelocity ?? 2000.0),
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

            // ── B. PICK 루프 (picker 0→3 순차) ────────────────────────────────
            for (int p = 0; p < pickers; p++)
            {
                if (!inspPass[p]) { pickupOk[p] = false; continue; }  // vision NG picker skip

                var d      = dies[p];
                var picker = front.Pickers[p];
                var vo     = visionOffsets[p];
                try
                {
                    // ① 3축 동시 이동
                    double armXTarget =
                        front.Setup.ArmInputPositionX
                        + p * front.Setup.PickerPitchX
                        + stage.WaferAlignOffsetX
                        + picker.Setup.ColletOffsetX
                        + d.X;
                    double stageYTarget =
                        stage.Setup.StageYTeachPosition
                        + stage.WaferAlignOffsetY
                        + d.Y
                        + vo.Y;
                    double needleXTarget =
                        stage.Setup.NeedleTeachX
                        + stage.WaferAlignOffsetX
                        + d.X
                        + vo.X;

                    await Task.WhenAll(
                        front.ArmX.MoveAbsoluteAsync(armXTarget, front.Recipe.ArmXVelocity),
                        stage.StageY.MoveAbsoluteAsync(stageYTarget, stage.Recipe.MoveVelocity),
                        stage.NeedleBlockX.MoveAbsoluteAsync(needleXTarget, stage.Recipe.MoveVelocity)
                    );

                    // ② Picker Z↓ (PickupPosition) / Needle Cap Vacuum ON / Picker Vacuum ON 동시
                    var pickerZTask = picker.PickerZ.MoveAbsoluteAsync(
                        picker.Setup.PickupPosition, picker.Recipe.ZVelocity);
                    stage.NeedleVacuum?.On();
                    picker.VacuumOn();
                    await pickerZTask;
                    await Task.Delay(picker.Recipe.VacuumSettleMs, ct);

                    // ③ Needle Up / Picker Up (+PickLiftPosition) 동시
                    double needleUpPos = stage.Setup.NeedleDownPosition + picker.Recipe.PickLiftPosition;
                    double pickerUpPos = picker.Setup.PickupPosition    + picker.Recipe.PickLiftPosition;
                    await Task.WhenAll(
                        ej.MoveAbsoluteAsync(needleUpPos, stage.Recipe.NeedleVelocity),
                        picker.PickerZ.MoveAbsoluteAsync(pickerUpPos, picker.Recipe.ZVelocity)
                    );

                    // ④ PickLiftWaitMs 대기
                    await Task.Delay(picker.Recipe.PickLiftWaitMs, ct);

                    // ⑤ Picker Wait (WaitPosition) / Needle Down (NeedleDownPosition) 동시
                    await Task.WhenAll(
                        picker.PickerZ.MoveAbsoluteAsync(picker.Setup.WaitPosition, picker.Recipe.ZVelocity),
                        ej.MoveAbsoluteAsync(stage.Setup.NeedleDownPosition, stage.Recipe.NeedleVelocity)
                    );

                    pickupOk[p] = !picker.PickerZ.IsAlarm && !ej.IsAlarm;
                }
                catch (Exception ex)
                {
                    Log($"[PICK p{p}] ex: " + ex.Message);
                    pickupOk[p] = false;
                }
            }
            // Needle Cap Vacuum 은 picker 들이 다이를 잡은 후에도 필요 — 사이클 끝에 OFF
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

            // PICK 후 — Pickup 실패한 picker 만 inspPass[p] = false 반영
            for (int p = 0; p < pickers; p++)
                if (!pickupOk[p]) inspPass[p] = false;

            // ── Stage 61 — ArmY 는 Place 끝날 때까지 Pickup 위치 유지 ──
            //   (Place 후에 명시적으로 Avoid 로 이동 — 이전의 "PICK 후 Avoid" 제거)
            //   wafer capture 는 ArmX 가 InspectionX 로 이동하는 동안 안전하게 wafer 영역 접근 가능.

            // 다음 사이클이 존재하면 wafer capture 시작 (non-await — Inspect/Place 와 병렬)
            int nextCycleIdx = cycleIdx + 1;
            if (nextCycleIdx < totalCycles)
            {
                int npickers = pickers;
                _pendingCaptureCycleIdx = nextCycleIdx;
                _pendingCapturePickers  = npickers;
                _pendingCaptureTask = CaptureWaferForCycleAsync(nextCycleIdx, npickers, ct);
                Log($"[PIPELINE] Cycle {nextCycleIdx + 1} wafer capture 백그라운드 시작");
            }

            // 14~18) Bottom+Side 병렬 파이프라인 (단일 ArmX 파이프라인 내 동시 촬영)
            //   - 좌표 모델: picker N abs X = ArmX - N*PickerPitchX (Side1X = SideVision1X = 850)
            //   - Step 0~3 = Bottom Expose Picker 0~3 (ArmX = ArmInspectionPositionX + i*pitch)
            //   - Step 2~5 = Side sub-sequence Picker 0~3 (ArmX 동일 위치에서 picker[idx-2] 가 Side X 정렬)
            //   - 각 picker Z↓ 는 Bottom 직전, Z↑ 는 Side 끝에서 발생.
            BottomVisionOffset[] bottomResults = null;
            SideVisionResult[]   sideResults   = null;
            bool visionConnected = VisionComm.VisionHub.Inspection != null
                                && VisionComm.VisionHub.Inspection.IsConnected;
            try
            {
                if (visionConnected)
                {
                    if (front.SideVisionY != null) front.SideVisionY.ServoOn();

                    Log("[VISION] Bottom+Side 병렬 파이프라인 시작 (4 picker)...");
                    var both = await front.InspectBottomAndSideAsync(DieSizeXMm, DieSizeYMm);
                    if (both != null)
                    {
                        bottomResults = both.Item1;
                        sideResults   = both.Item2;

                        // Bottom 결과 → picker offset / inspPass 반영
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

                        // Side 결과 → inspPass 반영
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
                    Log("[VISION] Inspection 미연결 — Bottom/Side 검사 sim 패스");
                }
            }
            catch (Exception ex) { Log("[VISION] Bottom+Side ex: " + ex.Message); }

            // 19) PLACE 위치 이동 — ArmX 동시에 4 picker Z 도 대기 위치 (Side 검사 후 들어 올림)
            double placeArmX = front.Setup?.ArmOutputPositionX ?? 1200.0;
            try
            {
                var move19 = new System.Collections.Generic.List<Task>();
                move19.Add(front.ArmX.MoveAbsoluteAsync(placeArmX,
                                                        front.Recipe?.ArmXVelocity ?? 2000.0));
                for (int p = 0; p < pickers; p++)
                    move19.Add(front.Pickers[p].PickerZ.MoveAbsoluteAsync(
                        front.Pickers[p].Setup.WaitPosition,
                        front.Pickers[p].Recipe.ZVelocity));
                await Task.WhenAll(move19);
            }
            catch (Exception ex) { Log("[PLACE] 19) move ex: " + ex.Message); }
            ct.ThrowIfCancellationRequested();

            // ── PlaceOnePickerAsync : picker p 를 지정 stage (Good/Ng) 에 Place ──
            async Task PlaceOnePickerAsync(int p, StageModule outStage)
            {
                var picker = front.Pickers[p];
                var bo = (bottomResults != null && p < bottomResults.Length) ? bottomResults[p] : null;
                double offX = bo?.OffsetX ?? 0.0;
                double offY = bo?.OffsetY ?? 0.0;
                double offT = bo?.OffsetT ?? 0.0;

                // ① ArmX (PlaceX + Bottom OffsetX) / Stage Y (HomeY + Bottom OffsetY) / PickerT (Bottom OffsetT) 동시
                await Task.WhenAll(
                    front.ArmX.MoveAbsoluteAsync(placeArmX + offX,
                                                 front.Recipe?.ArmXVelocity ?? 2000.0),
                    outStage.StageY.MoveAbsoluteAsync(outStage.Setup.HomePositionY + offY,
                                                     outStage.Recipe?.YVelocity ?? 500.0),
                    picker.PickerT.MoveAbsoluteAsync(offT, picker.Recipe.ThetaVelocity)
                );

                // ② Picker Z 다운 (PlacePosition)
                await picker.PickerZ.MoveAbsoluteAsync(picker.Setup.PlacePosition,
                                                       picker.Recipe.ZVelocity);

                // ③ Vacuum Off + Blow On
                picker.VacuumOff();
                picker.BlowOn();

                // ④ Place Delay (Recipe)
                await Task.Delay(picker.Recipe.PlaceDelayMs, ct);

                // ⑤ Blow Off
                picker.BlowOff();

                // ⑥ Picker Up (WaitPosition)
                await picker.PickerZ.MoveAbsoluteAsync(picker.Setup.WaitPosition,
                                                       picker.Recipe.ZVelocity);
            }

            // 20) Good 다이 먼저 Place (GoodStage)
            int goodPlaced = 0, ngPlaced = 0;
            for (int p = 0; p < pickers; p++)
            {
                if (!inspPass[p]) continue;
                try
                {
                    await PlaceOnePickerAsync(p, _machine.OutputStage.GoodStage);
                    goodPlaced++;
                }
                catch (Exception ex)
                {
                    Log($"[PLACE Good p{p}] ex: " + ex.Message);
                    PlaceFailCount++;
                }
            }

            // 21) NG 다이 Place — Stage Z 위치 전환 후 NgStage 에 Place
            bool hasNg = false;
            for (int p = 0; p < pickers; p++) if (!inspPass[p]) { hasNg = true; break; }
            if (hasNg)
            {
                try
                {
                    // GoodStage Z 충돌 회피로 하강, NgStage Z 작업 위치로 상승
                    await Task.WhenAll(
                        _machine.OutputStage.GoodStage.StageZ.MoveAbsoluteAsync(
                            _machine.OutputStage.GoodStage.Setup.AvoidPositionZ,
                            _machine.OutputStage.GoodStage.Recipe?.ZVelocity ?? 100.0),
                        _machine.OutputStage.NgStage.StageZ.MoveAbsoluteAsync(
                            _machine.OutputStage.NgStage.Setup.WorkPositionZ,
                            _machine.OutputStage.NgStage.Recipe?.ZVelocity ?? 100.0)
                    );
                }
                catch (Exception ex) { Log("[PLACE] Bin 전환 ex: " + ex.Message); }

                for (int p = 0; p < pickers; p++)
                {
                    if (inspPass[p]) continue;
                    try
                    {
                        await PlaceOnePickerAsync(p, _machine.OutputStage.NgStage);
                        ngPlaced++;
                    }
                    catch (Exception ex)
                    {
                        Log($"[PLACE Ng p{p}] ex: " + ex.Message);
                        PlaceFailCount++;
                    }
                }
            }
            Log($"[TPU] Place — Good={goodPlaced} Ng={ngPlaced} (cycle {cycleIdx + 1})");

            // ── Stage 61 — Place 종료 후 ArmY → AvoidPosition 회피 (다음 사이클 capture 안전 영역) ──
            try
            {
                await front.ArmY.MoveAbsoluteAsync(
                    front.Setup.ArmYAvoidPosition, front.Recipe.ArmYVelocity);
                Log($"[ARM-Y] Cycle {cycleIdx + 1} Place 완료 → Avoid 위치 복귀");
            }
            catch (Exception ex) { Log("[ARM-Y avoid after PLACE] ex: " + ex.Message); }

            // 결과 반영 → 4 다이 각각 inspPass[p] 기반으로 Good/NG 분리 기록
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

            // Stage 27 — 매 WafersPerOutputBatch 다이마다 Output 카세트 적재
            //   다이 베이스 + pickers 가 WafersPerOutputBatch 경계 넘으면 트리거
            int diesProcessedTotal = dieBase + pickers;
            if (WafersPerOutputBatch > 0 &&
                (diesProcessedTotal / WafersPerOutputBatch) > (dieBase / WafersPerOutputBatch))
            {
                Log($"[FEEDER] {WafersPerOutputBatch} 다이 완료 — Output 적재");
                // 사이클의 다수 결과로 wafer 적재 분류 (Good 우세 = Good)
                bool anyGood = false; foreach (var ip in inspPass) if (ip) { anyGood = true; break; }
                await StoreCompletedWaferAsync(anyGood);
            }

            // Stage 33 — 매 DiesPerColletClean 다이마다 Collet Cleaning
            if (DiesPerColletClean > 0 &&
                (diesProcessedTotal / DiesPerColletClean) > (dieBase / DiesPerColletClean))
            {
                Log($"[COLLET] {DiesPerColletClean} 다이 완료 — Collet Cleaning 시작");
                try
                {
                    bool clOk = await _machine.OutputStage.PerformColletCleaningAsync();
                    Log("[COLLET] Cleaning " + (clOk ? "OK" : "WARN"));
                }
                catch (Exception ex) { Log("[COLLET] exception: " + ex.Message); }
            }
            // Reject 분리 — 각 다이별 검사
            for (int p = 0; p < pickers; p++)
            {
                if (SubPortMaterialRejector.ShouldReject(dies[p], out double rxX, out double rxY))
                {
                    Log($"[DIE {dieBase + p + 1}] reject → ({rxX:F1},{rxY:F1})  bin={dies[p].BinCode}");
                }
            }
            Log($"[CYCLE {cycleIdx + 1}] dies {dieBase + 1}~{dieBase + pickers}/{CycleTotal} good={goodInCycle} ng={ngInCycle}");
        }

        // ──────────────────────────────────────────
        //  트리 순회
        // ──────────────────────────────────────────

        private IEnumerable<BaseAxis> EnumerateAxes()
        {
            foreach (var u in _machine.Units)
                foreach (var ax in EnumerateAxesRec(u))
                    yield return ax;
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

        // ──────────────────────────────────────────
        //  상태/로그
        // ──────────────────────────────────────────

        private void SetStatus(MachineStatus s)
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

        /// <summary>외부 시퀀스 계층에서 장비 로그를 기록하기 위한 공개 로그 브리지입니다.</summary>
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
