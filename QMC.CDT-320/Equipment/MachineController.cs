using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Motion;
using QMC.Common.Alarms;
using QMC.CDT320.Bin;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Jobs;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;

namespace QMC.CDT320
{
    /// <summary>
    /// ?묒뾽 ??쓽 珥덇린???쒖옉/?뺤?/CYCLE RUN/STOP 踰꾪듉???ㅼ젣 ?λ퉬 ?숈옉?쇰줈 ?곌껐?섎뒗 而⑦듃濡ㅻ윭.
    /// <para>
    /// CDT-320? Input(Loader/Stage) ??FRONT/REAR Picker(TransferPicker??Left/Right Arm) ??Output ?뚯씠?꾨씪??
    /// 媛??≪뀡? CDT320_Machine ?몃━瑜??쒗쉶?섎ŉ ?ㅼ젣 Axis/DO/DI瑜?議곗옉.
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

        public event Action<EquipmentStatus> StatusChanged;
        public event Action<string>        LogMessage;
        public event Action<int, int>      CycleProgress;  // (done, total)

        /// <summary>CDT-320 ?섎뱶?⑥뼱 ?좊떅 ?몃━?낅땲??</summary>
        public CDT320_Machine Machine => _machine;

        public EquipmentStatus Status => _status;
        public bool IsManualBusy => Volatile.Read(ref _manualBusyCount) > 0;
        public bool IsSequenceRunning => _coordinatorTask != null && !_coordinatorTask.IsCompleted;
        public QMC.CDT320.Sequencing.SequenceRunMode? ActiveSequenceRunMode { get; private set; }
        public int CycleTotal  { get; private set; }
        public int CycleDone   { get; private set; }
        public int GoodCount   { get; private set; }
        public int NgCount     { get; private set; }

        // ??????????????????????????????????????????
        //  Wafer ?ㅼ씠留???Input(?먰삎 300mm) / Output(?ш컖 ?몃젅??
        //  ?ъ씠???쒖옉 ??EnsureDieMaps 濡???踰??앹꽦, ?댄썑 罹먯떛.
        // ??????????????????????????????????????????
        /// <summary>?⑥씠??吏곴꼍 [mm].</summary>
        public double WaferDiameterMm { get; set; } = 300.0;
        /// <summary>?ㅼ씠 X ?ъ씠利?[mm].</summary>
        public double DieSizeXMm { get; set; } = 8.12;
        /// <summary>?ㅼ씠 Y ?ъ씠利?[mm].</summary>
        public double DieSizeYMm { get; set; } = 6.12;
        /// <summary>Input ?ㅼ씠留?移?媛꾧꺽 [mm] (?⑥씠???쎌뾽 痢?.</summary>
        public double InputGapMm  { get; set; } = 0.05;
        /// <summary>Output ?ㅼ씠留?移?媛꾧꺽 [mm] (BIN ?몃젅??痢?.</summary>
        public double OutputGapMm { get; set; } = 0.30;

        private QMC.CDT320.DieMaps.DieMap _inputDieMap;
        private QMC.CDT320.DieMaps.DieMap _outputDieMap;
        /// <summary>Input ?ㅼ씠留?(?먰삎 300mm). null ?대㈃ EnsureDieMaps() ?몄텧 ?꾩슂.</summary>
        public QMC.CDT320.DieMaps.DieMap InputDieMap  => _inputDieMap;
        /// <summary>Output ?ㅼ씠留?(?ш컖 ?몃젅??. null ?대㈃ EnsureDieMaps() ?몄텧 ?꾩슂.</summary>
        public QMC.CDT320.DieMaps.DieMap OutputDieMap => _outputDieMap;
        /// <summary>true 硫??ъ씠?댁씠 InputDieMap.IsTarget=true ?ㅼ씠 醫뚰몴瑜??ъ슜.</summary>
        public bool UseDieMapMode { get; set; } = true;

        // Stage 61 ??Pickup sequence options + cached sequence
        /// <summary>?⑥씠???ㅼ씠 ?쎌뾽 ?쒖꽌 ?듭뀡 (Recipe.Pickup ?먯꽌 set).</summary>
        public QMC.CDT320.Recipes.PickupSubset PickupOptions { get; set; } =
            new QMC.CDT320.Recipes.PickupSubset();

        /// <summary>?듭뀡 湲곕컲 ?뺣젹???쎌뾽 ?쒗??(?쒖꽦 ?ㅼ씠留? ?쒖꽌?濡?. EnsureDieMaps ?먮뒗 RebuildPickupSequence ?몄텧 ??媛깆떊.</summary>
        private List<QMC.CDT320.DieMaps.DieMapEntry> _inputPickupSequence
            = new List<QMC.CDT320.DieMaps.DieMapEntry>();

        // Stage 61 ??Pipelined wafer capture state
        //   ?ㅼ쓬 ?ъ씠?댁쓽 wafer vision 珥ъ쁺?????ъ씠??Inspect/Place ? 蹂묐젹濡??ㅽ뻾.
        //   ?꾨즺 ??CameraZ 媛 ?덉쟾 ?꾩튂源뚯? ?곸듅???덉뼱??ArmY 媛 ?쎌뾽 ?곸뿭 吏꾩엯 媛??
        private Task<(double X, double Y)[]> _pendingCaptureTask;
        private int _pendingCaptureCycleIdx = -1;
        private int _pendingCapturePickers  = 0;

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
                // ?뺤궗媛?寃⑹옄 root(N) ?щ┝
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
                Log("[DIEMAP] WafersPerOutputBatch ?먮룞 ?ㅼ젙 = " + WafersPerOutputBatch + " (Output ?щ’ ??");
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
        public int PickFailCount   { get; internal set; }
        /// <summary>PLACE ?ㅽ뙣 ?꾩쟻 ??(Output ?щ’ placement vision NG).</summary>
        public int PlaceFailCount  { get; internal set; }
        /// <summary>FRONT (LEFT ARM) Collet ?ъ슜 ?잛닔 ??Pick 1?뚮떦 +1.</summary>
        public int Collet1UseCount { get; internal set; }
        /// <summary>REAR (RIGHT ARM) Collet ?ъ슜 ?잛닔.</summary>
        public int Collet2UseCount { get; internal set; }
        /// <summary>EjectPin/Needle ?ъ슜 ?잛닔 (?ㅼ씠 1媛쒕떦 1??.</summary>
        public int NeedleUseCount  { get; internal set; }
        /// <summary>?뚮엺 ?꾩쟻 諛쒖깮 ??</summary>
        public int ErrorCount      { get; internal set; }
        /// <summary>?뺤긽 ?ㅼ슫(STOP/ECMG ??IDLE) ?꾩쟻 ?쒓컙.</summary>
        public TimeSpan NormalDownTime { get; internal set; } = TimeSpan.Zero;
        /// <summary>?뚮엺/?먮윭濡??명븳 ?ㅼ슫 ?꾩쟻 ?쒓컙.</summary>
        public TimeSpan ErrorDownTime  { get; internal set; } = TimeSpan.Zero;
        /// <summary>?뚮엺 諛쒖깮 ??蹂듦뎄源뚯? 嫄몃┛ ?꾩쟻 ?쒓컙.</summary>
        public TimeSpan RecoveryTime   { get; internal set; } = TimeSpan.Zero;
        /// <summary>Mean Time Between Failure (rolling).</summary>
        public TimeSpan Mtbf           { get; internal set; } = TimeSpan.Zero;
        /// <summary>Mean Time To Recovery (rolling).</summary>
        public TimeSpan Mttr           { get; internal set; } = TimeSpan.Zero;

        // ??????????????????????????????????????????
        //  濡쒗듃?ы듃 (Input Cassette) 吏꾪뻾 ?곹깭
        // ??????????????????????????????????????????

        /// <summary>?꾩옱 InputLoader 媛 泥섎━ 以묒씤 ?щ’ ?몃뜳??(0-base). -1 = 誘몄옣李??몃줈???곹깭.</summary>
        public int  CurrentInputSlot { get; private set; } = -1;
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
        }

        // ??????????????????????????????????????????
        //  濡쒗듃?ы듃 ?쒗???ы띁
        // ??????????????????????????????????????????

        /// <summary>
        /// InputLoader ???ㅼ쓬 ?⑥씠?쇰? InputStage 援먰솚 ?꾩튂源뚯? ?먮룞 吏꾪뻾.<br/>
        /// 1) WaferMap ??鍮꾩뼱 ?덉쑝硫?ScanCassetteAsync 濡?留ㅽ븨<br/>
        /// 2) <see cref="CurrentInputSlot"/> ?ㅼ쓬???⑥씠??蹂댁쑀 ?щ’?쇰줈 ElevatorZ ?대룞<br/>
        /// 3) MoveToExchangePositionAsync ?몄텧 (?쇰뜑 ?섍컯 ???대옩????Y ?꾩쭊)
        /// </summary>
        /// <returns>?ㅼ쓬 ?⑥씠???댁넚 ?깃났 ??true. 移댁꽭??鍮꾩뿀嫄곕굹 ?명꽣??李⑤떒 ??false.</returns>
        public async Task<bool> LoadNextWaferAsync()
        {
            var cassette = _machine.InputCassette;
            var feeder = _machine.InputFeeder;

            // 移댁꽭???덉갑 ?뺤씤
            if (!cassette.CassetteExistSensor.IsOn)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "LOT-NOCASS",
                    cassette.Name, "Input cassette is not detected.");
                Log("[LOTPORT] InputCassette absent ??load skipped");
                return false;
            }

            // 留ㅽ븨 誘몄닔?????ㅼ틪
            if (cassette.WaferMap == null || cassette.WaferMap.Count == 0)
            {
                Log("[LOTPORT] WaferMap empty ??scan cassette");
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

            CurrentInputSlot     = next;
            InputWaferAtExchange = true;
            RaiseLotPortChanged();
            Log($"[LOTPORT] LoadNextWafer OK ??slot={next}");

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
                Log("[LOTPORT] InputStage handoff (LoadAndPrepare) ?쒖옉...");
                int handoff = await _machine.InputStage.LoadAndPrepareWaferAsync();
                Log("[LOTPORT] InputStage handoff " + (handoff == 0 ? "OK" : "WARN"));

                // Stage 58 ??臾몄꽌 ?뺥빀: InputStage ?쒗???ㅽ뙣 ??AlarmManager.Raise 蹂닿컯.
                // (?댁쟾: Console.WriteLine 留???UI ?뚮엺 諛곕꼫/?덉뒪?좊━??誘몃컲??
                if (handoff != 0)
                {
                    AlarmManager.Raise(AlarmSeverity.Warning, "IS-LOAD",
                        _machine.InputStage.Name,
                        "LoadAndPrepareWafer ?ㅽ뙣 (?쇰뜑 ?덉쟾?꾩튂/ExpanderZ/諛붿퐫??留?以??섎굹).");
                    ErrorCount++;
                }

                if (handoff == 0)
                {
                    // ?쇰뜑 ?꾪눜 (?⑥씠?쇰뒗 ?대? InputStage 媛 ?↔퀬 ?덉쓬)
                    Log("[LOTPORT] ?쇰뜑 ?꾪눜 (InputStage ?⑤룆 ?묒뾽?쇰줈 ?꾪솚)...");
                    await feeder.RetractFeederAsync();
                    InputWaferAtExchange = false;
                    RaiseLotPortChanged();

                    // VisionAlign + Origin ?뺤젙
                    Log("[INPUTSTAGE] VisionAlign ?쒖옉...");
                    int aligned = await _machine.InputStage.VisionAlignAndSetupOriginAsync();
                    Log("[INPUTSTAGE] VisionAlign " + (aligned == 0 ? "OK" : "WARN (sim ?쒓퀎)"));

                    if (aligned != 0)
                    {
                        AlarmManager.Raise(AlarmSeverity.Warning, "IS-ALIGN",
                            _machine.InputStage.Name,
                            "VisionAlignAndSetupOrigin ?ㅽ뙣 (Vision ?듭떊 ?먮뒗 StageT ?뚮엺).");
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

        // ??????????????????????????????????????????
        //  Stage 28 ??InputStage ?ъ씠???듯빀 ?ы띁
        // ??????????????????????????????????????????

        /// <summary>?ㅼ씠 1媛??쎌뾽???꾪빐 StageY/CameraX 瑜??대룞.</summary>
        public async Task<bool> MoveInputStageToDieAsync(int row, int col)
        {
            try
            {
                var stage = _machine.InputStage;
                // Stage 28 ??Origin + Pitch 媛 ?뺤젙?섏뿀?쇰㈃ ?뺥솗???대룞, ?꾨땲硫?異붿젙媛?
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

        /// <summary>InputStage ???⑥씠???몃줈???쒗??(?ъ씠??醫낅즺 ??.</summary>
        public async Task<bool> UnloadInputStageWaferAsync()
        {
            try
            {
                Log("[INPUTSTAGE] UnloadWafer ?쒖옉...");
                int ok = await _machine.InputStage.UnloadWaferAsync();
                Log("[INPUTSTAGE] UnloadWafer " + (ok == 0 ? "OK" : "WARN"));
                return ok == 0;
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
            var feeder = _machine.InputFeeder;
            if (!InputWaferAtExchange)
            {
                Log("[LOTPORT] Retract skipped ??feeder not at exchange position");
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
        //  Stage 27 ??OutputUnloader Feeder ?쒗??
        // ??????????????????????????????????????????

        /// <summary>?꾩옱 Output 移댁꽭?몃퀎 ?ㅼ쓬 ?곸옱 ?щ’ ?몃뜳??(0-base).</summary>
        public int OutputSlotNg    { get; private set; } = 0;
        public int OutputSlotGood1 { get; private set; } = 0;
        public int OutputSlotGood2 { get; private set; } = 0;
        /// <summary>紐??ㅼ씠??OutputUnloader.StoreFullWafer ?몄텧 (諛곗튂 ?ш린).
        /// 0 = EnsureDieMaps ?먯꽌 OutputDieMap ?щ’ ?섎줈 ?먮룞 ?ㅼ젙.</summary>
        public int WafersPerOutputBatch { get; set; } = 0;

        /// <summary>Place ?꾨즺???⑥씠?쇰? Output Cassette ???곸젅???щ’???곸옱.</summary>
        public async Task<bool> StoreCompletedWaferAsync(bool isGood)
        {
            var unloader = _machine.OutputUnloader;
            // 移댁꽭???좎젙: Good ??Good1 ?곗꽑, 媛??李⑤㈃ Good2; NG ??Ng
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
                        unloader.Name, "Good 移댁꽭??紐⑤몢 媛?????ъ씠???먮룞 ?뺤?.");
                    Log("[FEEDER] Good cassette full ??CycleStop");
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
                        unloader.Name, "NG 移댁꽭??媛?????ъ씠???먮룞 ?뺤?.");
                    Log("[FEEDER] NG cassette full ??CycleStop");
                    _cycleCts?.Cancel();
                    return false;
                }
            }
            Log($"[FEEDER] StoreFullWafer ??{target} Slot[{slot}]");
            try
            {
                bool ok = await unloader.StoreFullWaferAsync(target, slot);
                if (!ok)
                {
                    AlarmManager.Raise(AlarmSeverity.Error, "OUT-STORE",
                        unloader.Name, "StoreFullWafer ?ㅽ뙣.");
                    return false;
                }
                Log($"[FEEDER] OK ??{target} Slot[{slot}] ?곸옱 ?꾨즺");

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
                    unloader.Name, ex.Message);
                return false;
            }
        }

        /// <summary>Output 3 移댁꽭???щ’ 留ㅽ븨 (UI 踰꾪듉??.</summary>
        public async Task<bool> ScanOutputCassettesAsync()
        {
            var unloader = _machine.OutputUnloader;
            try
            {
                bool ok = await unloader.ScanAllCassettesAsync();
                if (ok) Log("[FEEDER] Output 3 移댁꽭???ㅼ틪 ?꾨즺");
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
            Log("[SHUTDOWN] ?ㅻ퉬 ?뺤긽 醫낅즺 ?쒗???쒖옉...");
            try
            {
                _cycleCts?.Cancel();
                await Task.Delay(500);
                foreach (var ax in EnumerateAxes()) ax.Stop();
                // Shutdown ?뺤콉: 紐⑤뱺 異?Servo OFF (?ъ슜???붽뎄 ???꾨줈洹몃옩 醫낅즺 ???덉슜???먮룞 OFF)
                foreach (var ax in EnumerateAxes())
                {
                    try { ax.ServoOff(); } catch { }
                }
                LotStorage.CloseLot(aborted: true);
                AppSettingsStore.Save();
                SetStatus(EquipmentStatus.Stopped);
                Log("[SHUTDOWN] ?ㅻ퉬 醫낅즺 ?꾨즺.");
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
            Log("[RESET-ALARM] ?뚮엺 ?댁젣 ?쒗???쒖옉...");
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
                Log("[RESET-ALARM] ?꾨즺 (異?" + axisCount + ", ?ㅽ뙣=" + axisFail +
                    ", ?쒖꽦?뚮엺=" + activeBefore + " ?댁젣)");

                // ?뚮엺 ?곹깭??쇰㈃ Idle 濡?(?댁쁺 ?ш컻 ?꾪빐 INIT ?꾩슂)
                if (_status == EquipmentStatus.Alarm) SetStatus(EquipmentStatus.Idle);

                // Tower Lamp OFF (?뚮엺 ?댁젣)
                try { _machine.OpPanel?.TowerLampOff(); } catch { }
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
            Log("[E-STOP] 鍮꾩긽 ?뺤? ?쒗???쒖옉...");
            try
            {
                _cycleCts?.Cancel();
                int axTotal = 0, axFail = 0;
                foreach (var ax in EnumerateAxes())
                {
                    try { ax.EStop(); axTotal++; }
                    catch (Exception axEx) { axFail++; Log("[E-STOP] 異?EStop ?ㅽ뙣: " + axEx.Message); }
                }
                // E-STOP ?뺤콉: 紐⑤뱺 異?Servo OFF (?ъ슜???붽뎄 ??鍮꾩긽?뺤? ???덉슜???먮룞 OFF)
                foreach (var ax in EnumerateAxes())
                {
                    try { ax.ServoOff(); } catch { }
                }
                AlarmManager.Raise(AlarmSeverity.Error, "E-STOP", "Machine",
                    "?ъ슜???덉쟾?뚮줈 鍮꾩긽 ?뺤?");
                SetStatus(EquipmentStatus.Alarm);

                // Stage 45 ??Tower Lamp ?뚮엺 (鍮④컯 + 遺?). 紐낆떆??寃곌낵 湲곕줉.
                if (_machine.OpPanel != null)
                {
                    try
                    {
                        _machine.OpPanel.TowerLampAlarm();
                        Log("[E-STOP] TowerLamp ALARM OK (red + buzzer)");
                    }
                    catch (Exception lampEx)
                    {
                        Log("[E-STOP] TowerLamp ?쒖뼱 ?ㅽ뙣: " + lampEx.Message);
                        AlarmManager.Raise(AlarmSeverity.Warning, "TOWER-FAIL", "OpPanel", lampEx.Message);
                    }
                }
                else
                {
                    Log("[E-STOP] OpPanel 誘몄뿰寃???TowerLamp ?쒖뼱 ?앸왂");
                }
                Log("[E-STOP] 鍮꾩긽 ?뺤? ?꾨즺 (異?" + axTotal + ", ?ㅽ뙣=" + axFail +
                    "). ?덉쟾 ?뺤씤 ??RESET ALARM + INIT ?꾩슂.");
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
            DryRun  = GlobalDryRun || p.DryRun;
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
        //  以묒븰??紐⑥뀡 (Interlock 寃利????ㅼ젣 ?대룞)
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
                    $"Move blocked: target={position:F1} ??{reason}");
                Log($"[INTERLOCK] {axis.Name} ??{position:F1} blocked: {reason}");
                return false;
            }
            if (DryRun)
            {
                Log($"[DRYRUN] skip move {axis.Name} ??{position:F1} (vel={velocity:F0})");
                return true;
            }
            await axis.MoveAbsoluteAsync(position, velocity);
            return true;
        }

        // ??????????????????????????????????????????
        //  Wafer Alignment (3 ??鍮꾩쟾 留ㅼ묶 ??CoordinateMap)
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
                    Log($"[ALIGN] point {i+1}/3 ??move motor ??({motorPts[i].mx:F2}, {motorPts[i].my:F2})");
                    // ?ㅼ젣 紐⑥뀡? ?댁쁺 ?섍꼍?먯꽌 異붽?; ?쒕??먯꽌??留ㅼ묶 ?몄텧留?
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
                Log($"[ALIGN] OK ??scaleX={sx:F4} scaleY={sy:F4} rot={rot:F3}deg  ({coord})");
                return true;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "ALIGN-EX", "MachineController", ex.Message);
                Log("[ALIGN ERROR] " + ex.Message);
                return false;
            }
        }

        // ??????????????????????????????????????????
        //  怨듭슜 踰꾪듉 ?≪뀡
        // ??????????????????????????????????????????

        /// <summary>珥덇린?? 紐⑤뱺 異??쒕낫 ON + HOME + 移댁슫??珥덇린??</summary>
        public async Task InitAsync()
        {
            if (_status == EquipmentStatus.Initializing || _status == EquipmentStatus.AutoRunning) return;
            SetStatus(EquipmentStatus.Initializing);
            Log("[INIT] All axes servo ON + HOME search...");

            foreach (var ax in EnumerateAxes())
            {
                ax.ResetAlarm();
                ax.ServoOn();
            }

            // 媛꾨떒 ?쒕?: ?쒖감?곸쑝濡?HomeSearch. 媛?Unit??二쇱슂 異뺣???
            var axes = new List<BaseAxis>(EnumerateAxes());
            foreach (var ax in axes)
            {
                try { await ax.HomeSearchAsync(); }
                catch (Exception ex)
                {
                    AlarmManager.Raise(AlarmSeverity.Error, "HOME-" + ax.Name, ax.Name, "HOME ?ㅽ뙣: " + ex.Message);
                    Log("[ALARM] " + ax.Name + " HOME ?ㅽ뙣: " + ex.Message);
                    SetStatus(EquipmentStatus.Alarm);
                    return;
                }
                if (ax.IsAlarm)
                {
                    AlarmManager.Raise(AlarmSeverity.Error, "HOME-" + ax.Name, ax.Name, "HOME ???뚮엺 肄붾뱶 " + ax.AlarmCode);
                    Log("[ALARM] " + ax.Name + " HOME ?뚮엺 (code=" + ax.AlarmCode + ")");
                    SetStatus(EquipmentStatus.Alarm);
                    return;
                }
            }

            CycleDone = 0; CycleTotal = 0; GoodCount = 0; NgCount = 0;

            // Stage 46 ??Resource Sensors ?ъ쟾 寃??(CDA / Vacuum ?쇱씤 ?뺤긽 ?щ?)
            try
            {
                if (_machine.Resources != null && !_machine.Resources.AllOk)
                {
                    Log("[INIT] Resource 寃쎄퀬 ??CDA ?먮뒗 Vacuum ?쇱씤 鍮꾩젙??(sim 紐⑤뱶?먯꽌??OK 媛??");
                }
            }
            catch { }

            // Stage 26 ??Init ??濡쒗듃?ы듃 ?먮룞 留ㅽ븨 (?듭뀡)
            if (AutoScanCassetteOnInit)
            {
                try
                {
                    Log("[INIT] InputCassette ?먮룞 留ㅽ븨 ?쒖옉...");
                    var ctx = new QMC.CDT320.Sequencing.MachineSequenceContext(
                        this,
                        new QMC.CDT320.Sequencing.SequenceSignalBus());
                    var sequence = new QMC.CDT320.Sequencing.InputSequence(ctx);
                    int mapResult = await sequence.ExecuteMappingAsync(CancellationToken.None);
                    if (mapResult == 0)
                    {
                        int n = 0;
                        foreach (var b in _machine.InputCassette.WaferMap) if (b) n++;
                        Log($"[INIT] WaferMap OK ({n}??媛먯?)");
                    }
                    else
                    {
                        Log("[INIT] WaferMap 誘몄닔????移댁꽭??誘멸컧吏 ?먮뒗 ?ㅼ틪 ?ㅽ뙣");
                    }
                }
                catch (Exception ex)
                {
                    Log("[INIT] WaferMap ?덉쇅: " + ex.Message);
                }

                // Stage 27 fix ??Output 移댁꽭??3媛쒕룄 ?먮룞 ?ㅼ틪
                //   _slotMap ??25 ?щ’?쇰줈 ?ъ씠利?珥덇린?뷀븯??StoreFullWafer ??
                //   slotMap[target][slotIndex] = true 媛 ?뺤긽 湲곕줉?섎룄濡???
                try
                {
                    Log("[INIT] OutputCassette ?먮룞 留ㅽ븨 ?쒖옉...");
                    bool oOk = await ScanOutputCassettesAsync();
                    Log("[INIT] OutputCassette 留ㅽ븨 " + (oOk ? "OK" : "FAILED"));
                }
                catch (Exception ex)
                {
                    Log("[INIT] OutputCassette ?덉쇅: " + ex.Message);
                }
            }

            // R3 ??Input/Output ?ㅼ씠留??앹꽦 (?대? ?덉쑝硫?skip)
            try { EnsureDieMaps(); } catch (Exception dmEx) { Log("[INIT] DieMap ?앹꽦 寃쎄퀬: " + dmEx.Message); }

            Log("[INIT] 珥덇린???꾨즺. Ready.");
            SetStatus(EquipmentStatus.Ready);
        }

        /// <summary>장비 START: Servo ON 후 현재 구성된 자동 시퀀스를 시작합니다.</summary>
        public async Task<int> StartAsync()
        {
            try
            {
                if (_status == EquipmentStatus.Alarm)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync", "Start failed: alarm status is active. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Warning, "START-ALARM", "MachineController", "Alarm 상태에서는 START를 수행할 수 없습니다.");
                    Log("[START] failed: alarm status is active");
                    return -1;
                }

                if (IsSequenceRunning)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "StartAsync", "Start failed: sequence is already running. - Failed");
                    AlarmManager.Raise(AlarmSeverity.Warning, "START-RUNNING", "MachineController", "Sequence가 이미 실행 중입니다.");
                    Log("[START] failed: sequence is already running");
                    return -1;
                }

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
            // 1) 紐⑤뱺 異??뺤? (?쒕낫???좎?)
            foreach (var ax in EnumerateAxes()) ax.Stop();

            // 2) ?ъ씠?댁씠 吏꾪뻾 以묒씠?쇰㈃ cancel ???ㅼ젣 LOT 留덈Т由щ뒗 CycleRunAsync ??
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
                Log("[STOP] LOT=" + lot.LotID + " 留덈Т由?以?(done=" + CycleDone + "/" + CycleTotal +
                    ", good=" + GoodCount + ", ng=" + NgCount + ", skipped=" + skipped + ")");
                try { QMC.CDT320.Lots.LotStorage.CloseLot(aborted: true); } catch { }
                try { _machine.OpPanel?.TowerLampOff(); } catch { }
            }
            else
            {
                Log("[STOP] ?뺤?.");
            }
            SetStatus(EquipmentStatus.Stopped);
            return Task.CompletedTask;
        }

        public IDisposable EnterManualOperation()
        {
            Interlocked.Increment(ref _manualBusyCount);
            if (_status != EquipmentStatus.Alarm && _status != EquipmentStatus.AutoRunning)
                SetStatus(EquipmentStatus.ManualRunning);
            return new ManualOperationScope(this);
        }

        private void LeaveManualOperation()
        {
            if (Interlocked.Decrement(ref _manualBusyCount) < 0)
                Interlocked.Exchange(ref _manualBusyCount, 0);

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
        /// Input ?ㅼ씠留듭쓽 IsTarget=true ?ㅼ씠瑜?紐⑤몢 泥섎━. totalDies&lt;=0 ?먮뒗 ?ㅼ씠留?紐⑤뱶???뚮뒗
        /// EnsureDieMaps ???쒖꽦 ?ㅼ씠 ?섍? ?먮룞 ?곸슜??</summary>
        /// <summary>吏?뺥븳 ?듭뀡?쇰줈 蹂묐젹 ?쒗??Coordinator瑜??쒖옉?⑸땲??</summary>
        public async Task StartSequenceAsync(QMC.CDT320.Sequencing.SequenceRunOptions options)
        {
            try
            {
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
            if (_status == EquipmentStatus.AutoRunning) { Log("[CYCLE] already running"); return; }
            // Ready/Running ?몄뿉??Stopped ?먯꽌 ?ъ떆???덉슜 (CYCLE STOP ???ш컻)
            if (_status != EquipmentStatus.Ready &&
                _status != EquipmentStatus.ManualRunning &&
                _status != EquipmentStatus.Stopped)
            {
                Log("[CYCLE] ?ㅽ뻾 ??珥덇린???꾩슂 (status=" + _status + ")");
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
                Log("[CYCLE] DieMap 紐⑤뱶 ??Input wafer ?쒖꽦 ?ㅼ씠 " + active + " 以?" + totalDies + "媛?泥섎━");
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
            Log("[CYCLE] ?쒖옉 (total=" + totalDies + ", lot=" + lotId + ")");

            // Stage 41 ??SECS/HSMS ?ъ씠???쒖옉 ?대깽??
            try { SecsHost?.RaiseEvent("CycleStart", lotId, totalDies.ToString()); } catch { }

            // Stage 45 ??Tower Lamp ?뱀깋 (?댁쟾 以?
            try { _machine.OpPanel?.TowerLampRunning(); } catch { }

            try
            {
                if (!resumeCycle)
                {
                    // ?ъ씠???쒖옉 ??泥??⑥씠??濡쒗듃?ы듃 吏꾩엯
                    bool loaded = await LoadNextWaferAsync();
                    if (!loaded)
                    {
                        Log("[CYCLE] 濡쒗듃?ы듃 泥??⑥씠??濡쒕뱶 ?ㅽ뙣 ???ъ씠??吏꾪뻾 (?붾젅?ы듃 紐⑤뱶)");
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
                        Log("[CYCLE] Manual 紐⑤뱶 ??1 ?ъ씠??泥섎━ ???뺤? (done=" + CycleDone + ")");
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
                try { _machine.OpPanel?.TowerLampOff(); } catch { }
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
                    try { _machine.OpPanel?.TowerLampOff(); } catch { }
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
                    try { _machine.OpPanel?.TowerLampOff(); } catch { }
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

        /// <summary>???⑥씠?쇰떦 媛怨듯븷 ?ㅼ씠 ?? 1400 = 300mm ?⑥씠??????泥섎━ ???ㅼ쓬 ?щ’.</summary>
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
        //  Stage 61 ???뚯씠?꾨씪??wafer 鍮꾩쟾 珥ъ쁺 ?ы띁
        //  cycleIdx ??4 ?ㅼ씠瑜?移대찓?쇰줈 珥ъ쁺, 留덉?留됱뿉 CameraZ ?덉쟾 ?꾩튂 ?곸듅.
        //  ArmY 媛 ?쎌뾽 ?곸뿭 ?몃????덉쓣 ??(= ?ㅻⅨ ?ъ씠?댁쓽 Inspect/Place 吏꾪뻾 以? ?몄텧.
        // ??????????????????????????????????????????
        private async Task<(double X, double Y)[]> CaptureWaferForCycleAsync(
            int cycleIdx, int pickers, CancellationToken ct)
        {
            var stage = _machine.InputStage;
            var offsets = new (double X, double Y)[pickers];
            int dieBase = cycleIdx * pickers;

            try { stage.CameraX?.ServoOn(); stage.CameraZ?.ServoOn(); stage.StageY?.ServoOn(); } catch { }

            // 1) CameraZ ???ъ빱???꾩튂 (?ㅼ씠 ?쒕㈃)
            try
            {
                await stage.CameraZ.MoveAbsoluteAsync(
                    stage.Setup.CameraFocusZ, stage.Recipe.MoveVelocity);
            }
            catch (Exception ex) { Log("[CAPTURE-Z] focus move ex: " + ex.Message); }

            bool wafer = VisionComm.VisionHub.Wafer != null
                      && VisionComm.VisionHub.Wafer.IsConnected;
            int seqLen = _inputPickupSequence?.Count ?? 0;

            Log($"[CAPTURE] Cycle {cycleIdx + 1} ??{pickers} die 珥ъ쁺 ?쒖옉 (dieBase={dieBase})");

            for (int p = 0; p < pickers; p++)
            {
                if (ct.IsCancellationRequested) break;
                int seqIdx = dieBase + p;
                if (seqIdx < 0 || seqIdx >= seqLen)
                {
                    Log($"[CAPTURE p{p}] seqIdx {seqIdx} out of range (seqLen={seqLen}) ??skip");
                    offsets[p] = (0, 0);
                    continue;
                }
                var d = _inputPickupSequence[seqIdx];

                // 移대찓??X / ?⑥씠??Stage Y ?숈떆 ?대룞 (媛?die ??X,Y ???뺣젹)
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
                        double pxMm = stage.Setup.VisionPixelToMm;
                        offsets[p] = ((m.X - 320) * pxMm, (m.Y - 240) * pxMm);
                        Log($"[CAPTURE p{p}] OK score={m.Score:F2} offset=({offsets[p].X:F3},{offsets[p].Y:F3})mm");
                    }
                    else
                    {
                        offsets[p] = (0, 0);
                        Log($"[CAPTURE p{p}] NG match (score={m.Score:F2}) ??offset 0");
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

            // 2) CameraZ ???덉쟾 ?꾩튂 (???꾩튂源뚯? ?곸듅?댁빞 ArmY 媛 吏꾩엯 媛??
            try
            {
                await stage.CameraZ.MoveAbsoluteAsync(
                    stage.Setup.CameraSafeZ, stage.Recipe.MoveVelocity);
            }
            catch (Exception ex) { Log("[CAPTURE-Z] safe retract ex: " + ex.Message); }

            Log($"[CAPTURE] Cycle {cycleIdx + 1} 珥ъ쁺 ?꾨즺 ??CameraZ ?덉쟾 ?꾩튂 ?꾨떖");
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
                    Log($"[STEPRUN] user denied ??stopping cycle");
                    throw new OperationCanceledException("StepRun gate denied");
                }
            }

            // Stage 26 ??留?DiesPerWafer 留덈떎 ?ㅼ쓬 移댁꽭???щ’ 吏꾪뻾
            //   cycleIdx == 0 ? CycleRunAsync ?먯꽌 LoadNextWaferAsync ?대? ?몄텧?덉쑝誘濡??ㅽ궢
            if (dieBase > 0 && DiesPerWafer > 0 && dieBase % DiesPerWafer == 0 && InputWaferAtExchange)
            {
                Log($"[LOTPORT] {DiesPerWafer} ?ㅼ씠 ?꾨즺 ???ㅼ쓬 ?щ’?쇰줈 吏꾪뻾");
                await RetractCurrentWaferAsync();
                bool nextOk = await LoadNextWaferAsync();
                if (!nextOk)
                {
                    Log("[LOTPORT] ?ㅼ쓬 ?⑥씠???놁쓬 ???ъ씠?댁? ?붾젅?ы듃濡?怨꾩냽");
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
                    dies[i].X     = e.X;
                    dies[i].Y     = e.Y;
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

            // Stage 40 ??Dual Arm 紐⑤뱶: 吏앹닔 idx ??LeftArm, ???idx ??RightArm
            var front = (DualArmMode && (index % 2 == 1))
                        ? _machine.TransferPicker.RightArm
                        : _machine.TransferPicker.LeftArm;

            front.ArmX.ServoOn();
            front.ArmY.ServoOn();
            // 4 picker 紐⑤몢 ServoOn (PickerZ + PickerT)
            for (int p = 0; p < pickers; p++)
            {
                front.Pickers[p].PickerZ.ServoOn();
                front.Pickers[p].PickerT.ServoOn();
            }

            // Stage 58 ???댁쁺 ?듦퀎: Front collet + Needle 4 picker ?ъ슜
            Collet1UseCount += pickers;
            NeedleUseCount  += pickers;

            // 蹂???좎뼵 + Servo ON
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

            // ?? Stage 61 ???뚯씠?꾨씪??wafer 鍮꾩쟾 寃곌낵 ?섏떊 ??????????????????
            //   1) ArmY ??AvoidPosition (wafer ?곸뿭 ?몃? ?湲?
            //   2) Pending capture ?덉쑝硫?await ???놁쑝硫??숆린 罹≪쿂
            //   3) await ???쒖젏 = 珥ъ쁺 ?꾨즺 + CameraZ ?덉쟾 ?꾩튂 ?꾨떖
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
                Log($"[PIPELINE] Cycle {cycleIdx + 1} ???ъ쟾 wafer capture ?湲?以?..");
                capturedOffsets = await _pendingCaptureTask;
                _pendingCaptureTask = null;
                _pendingCaptureCycleIdx = -1;
            }
            else
            {
                Log($"[PIPELINE] Cycle {cycleIdx + 1} ???숆린 wafer capture (泥??ъ씠???먮뒗 desync)");
                capturedOffsets = await CaptureWaferForCycleAsync(cycleIdx, pickers, ct);
            }

            for (int p = 0; p < pickers && p < capturedOffsets.Length; p++)
            {
                visionOffsets[p] = capturedOffsets[p];
                dieOffsets[p].X += visionOffsets[p].X;
                dieOffsets[p].Y += visionOffsets[p].Y;
            }

            // 8) ArmY ??Pickup + ArmX ??ArmInputPositionX (CameraZ 媛 ?덉쟾 ?꾩튂???덈뒗 ?쒖젏)
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

            // ?? B. PICK 猷⑦봽 (picker 0?? ?쒖감) ????????????????????????????????
            for (int p = 0; p < pickers; p++)
            {
                if (!inspPass[p]) { pickupOk[p] = false; continue; }  // vision NG picker skip

                var d      = dies[p];
                var picker = front.Pickers[p];
                var vo     = visionOffsets[p];
                try
                {
                    // ??3異??숈떆 ?대룞
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

                    // ??Picker Z??(PickupPosition) / Needle Cap Vacuum ON / Picker Vacuum ON ?숈떆
                    var pickerZTask = picker.PickerZ.MoveAbsoluteAsync(
                        picker.Setup.PickupPosition, picker.Recipe.ZVelocity);
                    stage.NeedleVacuum?.On();
                    picker.VacuumOn();
                    await pickerZTask;
                    await Task.Delay(picker.Recipe.VacuumSettleMs, ct);

                    // ??Needle Up / Picker Up (+PickLiftPosition) ?숈떆
                    double needleUpPos = stage.Setup.NeedleDownPosition + picker.Recipe.PickLiftPosition;
                    double pickerUpPos = picker.Setup.PickupPosition    + picker.Recipe.PickLiftPosition;
                    await Task.WhenAll(
                        ej.MoveAbsoluteAsync(needleUpPos, stage.Recipe.NeedleVelocity),
                        picker.PickerZ.MoveAbsoluteAsync(pickerUpPos, picker.Recipe.ZVelocity)
                    );

                    // ??PickLiftWaitMs ?湲?
                    await Task.Delay(picker.Recipe.PickLiftWaitMs, ct);

                    // ??Picker Wait (WaitPosition) / Needle Down (NeedleDownPosition) ?숈떆
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
                _pendingCapturePickers  = npickers;
                _pendingCaptureTask = CaptureWaferForCycleAsync(nextCycleIdx, npickers, ct);
                Log($"[PIPELINE] Cycle {nextCycleIdx + 1} wafer capture 諛깃렇?쇱슫???쒖옉");
            }

            // 14~18) Bottom+Side 蹂묐젹 ?뚯씠?꾨씪??(?⑥씪 ArmX ?뚯씠?꾨씪?????숈떆 珥ъ쁺)
            //   - 醫뚰몴 紐⑤뜽: picker N abs X = ArmX - N*PickerPitchX (Side1X = SideVision1X = 850)
            //   - Step 0~3 = Bottom Expose Picker 0~3 (ArmX = ArmInspectionPositionX + i*pitch)
            //   - Step 2~5 = Side sub-sequence Picker 0~3 (ArmX ?숈씪 ?꾩튂?먯꽌 picker[idx-2] 媛 Side X ?뺣젹)
            //   - 媛?picker Z????Bottom 吏곸쟾, Z????Side ?앹뿉??諛쒖깮.
            BottomVisionOffset[] bottomResults = null;
            SideVisionResult[]   sideResults   = null;
            bool visionConnected = VisionComm.VisionHub.Inspection != null
                                && VisionComm.VisionHub.Inspection.IsConnected;
            try
            {
                if (visionConnected)
                {
                    if (front.SideVisionY != null) front.SideVisionY.ServoOn();

                    Log("[VISION] Bottom+Side 蹂묐젹 ?뚯씠?꾨씪???쒖옉 (4 picker)...");
                    var both = await front.InspectBottomAndSideAsync(DieSizeXMm, DieSizeYMm);
                    if (both != null)
                    {
                        bottomResults = both.Item1;
                        sideResults   = both.Item2;

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
                    Log("[VISION] Inspection 誘몄뿰寃???Bottom/Side 寃??sim ?⑥뒪");
                }
            }
            catch (Exception ex) { Log("[VISION] Bottom+Side ex: " + ex.Message); }

            // 19) PLACE ?꾩튂 ?대룞 ??ArmX ?숈떆??4 picker Z ???湲??꾩튂 (Side 寃?????ㅼ뼱 ?щ┝)
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

            // ?? PlaceOnePickerAsync : picker p 瑜?吏??stage (Good/Ng) ??Place ??
            async Task PlaceOnePickerAsync(int p, StageModule outStage)
            {
                var picker = front.Pickers[p];
                var bo = (bottomResults != null && p < bottomResults.Length) ? bottomResults[p] : null;
                double offX = bo?.OffsetX ?? 0.0;
                double offY = bo?.OffsetY ?? 0.0;
                double offT = bo?.OffsetT ?? 0.0;

                // ??ArmX (PlaceX + Bottom OffsetX) / Stage Y (HomeY + Bottom OffsetY) / PickerT (Bottom OffsetT) ?숈떆
                await Task.WhenAll(
                    front.ArmX.MoveAbsoluteAsync(placeArmX + offX,
                                                 front.Recipe?.ArmXVelocity ?? 2000.0),
                    outStage.StageY.MoveAbsoluteAsync(outStage.Setup.HomePositionY + offY,
                                                     outStage.Recipe?.YVelocity ?? 500.0),
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
                    await PlaceOnePickerAsync(p, _machine.OutputStage.GoodStage);
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
                        _machine.OutputStage.GoodStage.StageZ.MoveAbsoluteAsync(
                            _machine.OutputStage.GoodStage.Setup.AvoidPositionZ,
                            _machine.OutputStage.GoodStage.Recipe?.ZVelocity ?? 100.0),
                        _machine.OutputStage.NgStage.StageZ.MoveAbsoluteAsync(
                            _machine.OutputStage.NgStage.Setup.WorkPositionZ,
                            _machine.OutputStage.NgStage.Recipe?.ZVelocity ?? 100.0)
                    );
                }
                catch (Exception ex) { Log("[PLACE] Bin ?꾪솚 ex: " + ex.Message); }

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
            Log($"[TPU] Place ??Good={goodPlaced} Ng={ngPlaced} (cycle {cycleIdx + 1})");

            // ?? Stage 61 ??Place 醫낅즺 ??ArmY ??AvoidPosition ?뚰뵾 (?ㅼ쓬 ?ъ씠??capture ?덉쟾 ?곸뿭) ??
            try
            {
                await front.ArmY.MoveAbsoluteAsync(
                    front.Setup.ArmYAvoidPosition, front.Recipe.ArmYVelocity);
                Log($"[ARM-Y] Cycle {cycleIdx + 1} Place ?꾨즺 ??Avoid ?꾩튂 蹂듦?");
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
                Log($"[FEEDER] {WafersPerOutputBatch} ?ㅼ씠 ?꾨즺 ??Output ?곸옱");
                // ?ъ씠?댁쓽 ?ㅼ닔 寃곌낵濡?wafer ?곸옱 遺꾨쪟 (Good ?곗꽭 = Good)
                bool anyGood = false; foreach (var ip in inspPass) if (ip) { anyGood = true; break; }
                await StoreCompletedWaferAsync(anyGood);
            }

            // Stage 33 ??留?DiesPerColletClean ?ㅼ씠留덈떎 Collet Cleaning
            if (DiesPerColletClean > 0 &&
                (diesProcessedTotal / DiesPerColletClean) > (dieBase / DiesPerColletClean))
            {
                Log($"[COLLET] {DiesPerColletClean} ?ㅼ씠 ?꾨즺 ??Collet Cleaning ?쒖옉");
                try
                {
                    bool clOk = await _machine.OutputStage.PerformColletCleaningAsync();
                    Log("[COLLET] Cleaning " + (clOk ? "OK" : "WARN"));
                }
                catch (Exception ex) { Log("[COLLET] exception: " + ex.Message); }
            }
            // Reject 遺꾨━ ??媛??ㅼ씠蹂?寃??
            for (int p = 0; p < pickers; p++)
            {
                if (SubPortMaterialRejector.ShouldReject(dies[p], out double rxX, out double rxY))
                {
                    Log($"[DIE {dieBase + p + 1}] reject ??({rxX:F1},{rxY:F1})  bin={dies[p].BinCode}");
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

