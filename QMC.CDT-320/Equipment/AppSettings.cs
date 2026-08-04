using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

namespace QMC.CDT320
{
    /// <summary>Wafer 완료 후 자동 운전 처리 방식입니다.</summary>
    public enum WaferCompleteRunMode
    {
        /// <summary>완료 Wafer를 교체한 뒤 자동 운전을 계속합니다.</summary>
        Continue = 0,

        /// <summary>신규 Pick을 차단하고 Picker 보유 제품을 모두 배출한 뒤 READY로 종료합니다.</summary>
        StopAfterDrain = 1
    }

    /// <summary>애플리케이션 설정 데이터.</summary>
    [DataContract]
    public class AppSettings
    {
        [DataMember] public string Language         { get; set; } = "ko";
        [DataMember] public bool   BinArrayFile     { get; set; } = true;
        [DataMember] public bool   VisionMatchError { get; set; } = true;
        [DataMember] public string SimulatorHost    { get; set; } = "127.0.0.1";
        [DataMember] public int    SimulatorPort    { get; set; } = 7001;
        [DataMember] public string LastProject      { get; set; }
        [DataMember] public bool   SimulationMode   { get; set; } = true;
        [DataMember] public bool   DryRunMode       { get; set; } = false;
        [DataMember] public bool   DeveloperMode    { get; set; } = false;

        /// <summary>AJINEXTEK AXL 실보드 사용 여부. false 일 때는 Sim 모드.</summary>
        [DataMember] public bool   UseAjin          { get; set; } = false;
        /// <summary>AxlOpen IRQ 번호 (보드 설정에 맞춰 변경).</summary>
        [DataMember] public int    AjinIrqNo        { get; set; } = 7;

        // ── Vision link (CDT-310 매뉴얼 사양 — 6 communicators) ──
        /// <summary>QMC.Vision 프로세스 호스트.</summary>
        [DataMember] public string VisionHost           { get; set; } = "127.0.0.1";
        [DataMember] public int    VisionWaferPort      { get; set; } = 5100;
        [DataMember] public int    VisionInspectionPort { get; set; } = 5101;
        [DataMember] public int    VisionBinPort        { get; set; } = 5103;
        /// <summary>Stage 43 — 매뉴얼 추가: MainCommunicator (5104).</summary>
        [DataMember] public int    VisionMainPort       { get; set; } = 5104;
        /// <summary>Stage 43 — 매뉴얼 추가: FrontSide Inspection Vision (5105).</summary>
        [DataMember] public int    VisionFrontSidePort    { get; set; } = 5105;
        /// <summary>Stage 43 — 매뉴얼 추가: RearSide Inspection Vision (5106).</summary>
        [DataMember] public int    VisionRearSidePort { get; set; } = 5106;

        // ── Vision 뷰어(이미지 스트림) 포트 — 명령 채널과 별개. Vision측 GrabStreamServer가 listen. ──
        [DataMember] public int    VisionWaferViewerPort      { get; set; } = 5200;
        [DataMember] public int    VisionInspectionViewerPort { get; set; } = 5201; // Bottom
        [DataMember] public int    VisionBinViewerPort        { get; set; } = 5203;
        [DataMember] public int    VisionFrontSideViewerPort    { get; set; } = 5205;
        [DataMember] public int    VisionRearSideViewerPort { get; set; } = 5206;

        /// <summary>앱 시작 시 자동 연결 시도 여부.</summary>
        [DataMember] public bool   VisionAutoConnect    { get; set; } = true;

        /// <summary>비전 사용 여부. false 면 핸들러가 Vision PC 에 연결하지 않고, 자동 시퀀스의 GRAB/MATCH/INSPECT 는
        /// 통과 처리(bypass)하여 비전 없이도 동작한다. 기본 true.</summary>
        [DataMember] public bool   UseVision            { get; set; } = true;

        /// <summary>Simulation 모드에서도 연결된 외부 Vision PC와 실제 MATCH/INSPECT 프로토콜을 수행할지 여부.
        /// 모션과 IO의 Simulation 상태는 변경하지 않는다.</summary>
        [DataMember] public bool   UseRealVisionInSimulation { get; set; } = true;

        /// <summary>뷰어 측정 스케일 계수 — 표시 mm/px = 저장 스케일(mm/px) × 이 계수.
        /// Vision 이 뷰어 이미지를 다운스케일(원본→표시)하면 표시 1px 가 더 넓어지므로 계수=원본폭/표시폭(예 5120/1600=3.2)로 보정한다.
        /// 기본 1.0 = 저장 스케일 그대로(다운스케일 없음/풀해상도). 계수 ≤ 0 이면 자동(표시폭 meta.Width에서 산출). 향후 다운스케일 파라미터화 시 이 값에 반영.</summary>
        [DataMember] public double ViewerMeasureScaleFactor { get; set; } = 1.0;

        // ── Barcode link (CDT-310 매뉴얼 사양 — Serial Port 4/6) ──
        /// <summary>Input Wafer 로딩 중 바코드 읽기 사용 여부.</summary>
        [DataMember] public bool   UseInputWaferBarcode { get; set; } = false;
        /// <summary>Output Bin 로딩 중 바코드 읽기 사용 여부.</summary>
        [DataMember] public bool   UseOutputBinBarcode { get; set; } = false;
        /// <summary>Stage 43 — Wafer Barcode 시리얼 포트 번호.</summary>
        [DataMember] public int    WaferBarcodeSerialPort { get; set; } = 4;
        /// <summary>Stage 43 — Bin Barcode 시리얼 포트 번호.</summary>
        [DataMember] public int    BinBarcodeSerialPort   { get; set; } = 6;
        /// <summary>이전 설정 파일 호환용 공통 baudrate.</summary>
        [DataMember] public int    BarcodeSerialBaud      { get; set; } = 9600;
        [DataMember] public int    WaferBarcodeSerialBaud { get; set; } = 9600;
        [DataMember] public int    BinBarcodeSerialBaud { get; set; } = 9600;
        [DataMember] public int    InputBarcodeReadTimeoutMs { get; set; } = 3000;
        [DataMember] public int    OutputBarcodeReadTimeoutMs { get; set; } = 3000;
        [DataMember] public int    InputBarcodeRetryCount { get; set; } = 3;
        [DataMember] public int    OutputBarcodeRetryCount { get; set; } = 3;
        [DataMember] public double InputBarcodeRetryStepMm { get; set; } = 1.000;
        [DataMember] public double OutputBarcodeRetryStepMm { get; set; } = 1.000;
        /// <summary>NLV-5201 판독 시작 명령. 빈 문자열이면 기본 Z 명령을 사용합니다.</summary>
        [DataMember] public string InputBarcodeTriggerCommand { get; set; } = "";
        /// <summary>NLV-5201 판독 시작 명령. 빈 문자열이면 기본 Z 명령을 사용합니다.</summary>
        [DataMember] public string OutputBarcodeTriggerCommand { get; set; } = "";

        // ── Simulator link — auto connect ──
        [DataMember] public bool   SimulatorAutoConnect { get; set; } = false;

        // ── Motion speed scale ──
        /// <summary>
        /// 전체 공통 DefaultVelocity 퍼센트 스케일 [%]. 100 = 설정값 그대로, 10 = 설정값의 10% 속도.
        /// 자동 시퀀스 일반 이동(DefaultVelocity 기반)에만 적용된다. 안전 범위 1~100.
        /// </summary>
        [DataMember] public double DefaultVelocityScalePercent { get; set; } = 100.0;

        /// <summary>
        /// Manual Sequence(수동/CYCLE RUN Step, 작업 정보 Action 버튼 포함) 전용 DefaultVelocity 퍼센트 [%].
        /// 전체 ScalePercent 와 독립 적용되며, 속도와 가감속에 같은 배율로 함께 적용된다. 안전 범위 1~100.
        /// </summary>
        [DataMember] public double ManualSequenceScalePercent { get; set; } =
            QMC.Common.Motion.MotionSpeedScale.DefaultManualSequencePercent;

        /// <summary>
        /// 작업 화면 READY 시퀀스 전용 DefaultVelocity 퍼센트 [%].
        /// 전체 ScalePercent 와 독립 적용되며, 속도와 가감속에 같은 배율로 함께 적용된다. 안전 범위 1~100.
        /// </summary>
        [DataMember] public double ReadySequenceScalePercent { get; set; } =
            QMC.Common.Motion.MotionSpeedScale.DefaultReadySequencePercent;

        /// <summary>
        /// 자동 시퀀스 테스트 중 Input/Output 카메라 X 이동과 비전 검사를 생략하고 Picker 모션만 확인한다.
        /// 실장비 생산용 안전 인터락은 우회하지 않는다.
        /// </summary>
        [DataMember] public bool   PickerMotionOnlyTestMode { get; set; } = false;

        /// <summary>
        /// Input 또는 Output Wafer 완료 시 자동 교체를 계속할지, Picker 보유 제품을 모두 배출한 뒤
        /// READY로 종료할지 선택합니다.
        /// </summary>
        [DataMember] public WaferCompleteRunMode WaferCompleteRunMode { get; set; } = WaferCompleteRunMode.Continue;

        /// <summary>
        /// 이력 탭의 로그(Event/시퀀스) 이력 화면 사용 여부. false 면 해당 페이지들은 안내만 표시한다.
        /// 로그 폭주 등으로 문제가 보일 때 빌드 없이 끌 수 있는 안전 스위치. 기본 true.
        /// </summary>
        [DataMember] public bool   FileLogHistoryEnabled { get; set; } = true;

        // ── 최소 로그 정책(LogPolicy) ──
        /// <summary>시작 시 DiagnosticVerbose 모드로 기동할지. 제한 시간(LogDiagnosticVerboseMinutes) 후 자동 복귀한다. 기본 false(ProductionMinimal).</summary>
        [DataMember] public bool   LogDiagnosticVerboseOnStart { get; set; } = false;
        /// <summary>DiagnosticVerbose 자동 종료 시간(분). 기본 60분.</summary>
        [DataMember] public int    LogDiagnosticVerboseMinutes { get; set; } = 60;
        /// <summary>알람 블랙박스(메모리 순환 버퍼) 최대 건수. 기본 20000.</summary>
        [DataMember] public int    LogBlackboxCapacity { get; set; } = 20000;
        /// <summary>AlarmContext 덤프에 포함할 직전 시간창(초). 기본 30초.</summary>
        [DataMember] public int    LogBlackboxWindowSeconds { get; set; } = 30;
        /// <summary>ProductionMinimal에서도 영구 저장할 이벤트 코드 접두사(쉼표 구분). 비우면 내장 기본값 사용.</summary>
        [DataMember] public string LogPersistCodePrefixes { get; set; } = "";
        /// <summary>Front/Rear 픽커 유휴 대기 폴 주기(ms). 기본 20ms, 안전 범위 1~500.</summary>
        [DataMember] public int    PickerIdlePollMs { get; set; } = 20;

        /// <summary>
        /// 압축 보관본(Log\Archive\*.zip) 보존일수. 14일이 지난 원본 로그는 자동으로 압축 보관되고(고정 규칙),
        /// 압축본은 이 일수가 지나면 최종 삭제된다(복구 불가). 0 = 무기한 보관(OFF). 기본 0.
        /// </summary>
        [DataMember] public int    ArchiveKeepDays { get; set; } = 0;

        /// <summary>로그 압축 사용 여부. true 면 <see cref="LogCompressDays"/> 지난 원본 로그를 zip 보관하고 원본을 지운다. 기본 true.</summary>
        [DataMember] public bool   LogCompressEnabled { get; set; } = true;

        /// <summary>로그 압축 유예일수(이 일수가 지난 원본을 압축 보관). 1~365. 기본 14.</summary>
        [DataMember] public int    LogCompressDays { get; set; } = 14;

        /// <summary>로그 저장 방식. false=전체(모든 종류를 <see cref="LogAllDir"/> 한 폴더에), true=종류별(각 종류 폴더). 기본 false(기존 동작).</summary>
        [DataMember] public bool   LogSplitByKind { get; set; } = false;

        /// <summary>전체 저장 모드일 때 모든 로그를 저장할 폴더. 비어있으면 기본(&lt;LogRoot&gt;\Event).</summary>
        [DataMember] public string LogAllDir { get; set; }

        /// <summary>종류별 저장 모드일 때 종류별 폴더 오버라이드. 키=EventKind 이름(Event/Warning/…), 값=폴더 경로.
        /// 비어있거나 키가 없으면 기본 경로(&lt;LogRoot&gt;\&lt;종류&gt;)를 사용한다.</summary>
        [DataMember] public Dictionary<string, string> LogKindPaths { get; set; }

        /// <summary>
        /// material_state.json 에 검사 측정값 상세(Measurements/Alignments)를 저장할지 여부. 기본 false(저장 안 함).
        /// true 로 두면 검사 1건마다 측정값 약 20쌍이 스냅샷에 함께 쌓여 파일이 웨이퍼 1장당 27MB 규모까지 커지고,
        /// 5초 주기 전체 재직렬화 비용이 그만큼 늘어난다(2026-07-27 실측: 10MB에서 저장 1회 0.3~1.2초).
        /// 측정값 상세는 InputWaferInspectionCsvSnapshotWriter / OutputWaferCsvSnapshotWriter /
        /// VisionInspectionResultFileWriter 가 이미 CSV로 남기며, 재개(resume)에는
        /// InspectionType/Result/Offset/NgCodes 만 있으면 되므로 기본은 저장하지 않는다.
        /// 비전 측정값을 스냅샷에서 직접 추적해야 하는 분석 상황에서만 켠다.
        /// </summary>
        [DataMember] public bool   SaveMaterialInspectionDetail { get; set; } = false;

        // DataContractJsonSerializer 는 필드 이니셜라이저를 실행하지 않으므로, 구 settings.json 에 없는
        // 신규 키는 여기서 기본값을 심는다(없으면 false 로 로드되어 의도치 않게 비전이 꺼지는 문제 방지).
        [OnDeserializing]
        internal void OnDeserializing(StreamingContext ctx)
        {
            UseVision = true;
            UseRealVisionInSimulation = true;
            UseInputWaferBarcode = false;
            UseOutputBinBarcode = false;
            WaferBarcodeSerialPort = 4;
            BinBarcodeSerialPort = 6;
            BarcodeSerialBaud = 9600;
            // 구 설정 파일의 공통 BarcodeSerialBaud를 OnDeserialized에서 채울 수 있도록 0으로 시작합니다.
            WaferBarcodeSerialBaud = 0;
            BinBarcodeSerialBaud = 0;
            InputBarcodeReadTimeoutMs = 3000;
            OutputBarcodeReadTimeoutMs = 3000;
            InputBarcodeRetryCount = 3;
            OutputBarcodeRetryCount = 3;
            InputBarcodeRetryStepMm = 1.000;
            OutputBarcodeRetryStepMm = 1.000;
            InputBarcodeTriggerCommand = "";
            OutputBarcodeTriggerCommand = "";
            WaferCompleteRunMode = WaferCompleteRunMode.Continue;
            ViewerMeasureScaleFactor = 1.0;   // 구 settings.json 에 키 없으면 0 으로 로드되는 것 방지(기본=저장 스케일 그대로)
            FileLogHistoryEnabled = true;   // 구 settings.json 에 키가 없으면 false 로 로드되어 이력 화면이 꺼지는 문제 방지
            // ArchiveKeepDays 는 키가 없으면 0(무기한 보관)으로 로드되며, 이는 기본값과 같아 별도 처리가 필요 없다.
            LogCompressEnabled = true;      // 구 settings.json 에 키 없으면 압축이 꺼지는 것 방지(기존 동작=항상 압축)
            LogCompressDays = 14;           // 구 settings.json 에 키 없으면 0 으로 로드되는 것 방지(기존 고정값 14)
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext ctx)
        {
            int legacyBaud = BarcodeSerialBaud > 0 ? BarcodeSerialBaud : 9600;
            if (WaferBarcodeSerialBaud <= 0) WaferBarcodeSerialBaud = legacyBaud;
            if (BinBarcodeSerialBaud <= 0) BinBarcodeSerialBaud = legacyBaud;
            if (WaferBarcodeSerialPort <= 0) WaferBarcodeSerialPort = 4;
            if (BinBarcodeSerialPort <= 0) BinBarcodeSerialPort = 6;
            InputBarcodeReadTimeoutMs = Math.Max(100, Math.Min(60000, InputBarcodeReadTimeoutMs));
            OutputBarcodeReadTimeoutMs = Math.Max(100, Math.Min(60000, OutputBarcodeReadTimeoutMs));
            InputBarcodeRetryCount = Math.Max(0, Math.Min(20, InputBarcodeRetryCount));
            OutputBarcodeRetryCount = Math.Max(0, Math.Min(20, OutputBarcodeRetryCount));
            if (double.IsNaN(InputBarcodeRetryStepMm) || double.IsInfinity(InputBarcodeRetryStepMm) || InputBarcodeRetryStepMm <= 0)
                InputBarcodeRetryStepMm = 1.000;
            if (double.IsNaN(OutputBarcodeRetryStepMm) || double.IsInfinity(OutputBarcodeRetryStepMm) || OutputBarcodeRetryStepMm <= 0)
                OutputBarcodeRetryStepMm = 1.000;
            InputBarcodeRetryStepMm = Math.Min(100.000, InputBarcodeRetryStepMm);
            OutputBarcodeRetryStepMm = Math.Min(100.000, OutputBarcodeRetryStepMm);
            InputBarcodeTriggerCommand = InputBarcodeTriggerCommand ?? "";
            OutputBarcodeTriggerCommand = OutputBarcodeTriggerCommand ?? "";
        }

        public bool BypassHardware => SimulationMode;
    }

    /// <summary>
    /// AppSettings 영속화. <c>./Config/settings.json</c>.
    /// 앱 시작 시 <see cref="Load"/>, 변경 시 <see cref="Save"/>.
    /// </summary>
    public static class AppSettingsStore
    {
        public static string Dir { get; } =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string Path_ { get; } =
            System.IO.Path.Combine(Dir, "settings.json");

        public static AppSettings Current { get; private set; } = new AppSettings();
        private static readonly object HybridModeSync = new object();
        private static bool _hybridModeSnapshotReady;
        private static bool _lastSimulationMode;
        private static bool _lastDryRunMode;
        private static bool _lastUseAjin;
        private static bool _lastUseVision;
        private static bool _lastUseRealVisionInSimulation;

        static AppSettingsStore()
        {
            Directory.CreateDirectory(Dir);
            RefreshHybridModeSnapshot(false);
        }

        private static void RefreshHybridModeSnapshot(bool invalidateSessionOnChange)
        {
            AppSettings settings = Current ?? new AppSettings();
            bool changed;
            lock (HybridModeSync)
            {
                changed = _hybridModeSnapshotReady &&
                    (_lastSimulationMode != settings.SimulationMode ||
                     _lastDryRunMode != settings.DryRunMode ||
                     _lastUseAjin != settings.UseAjin ||
                     _lastUseVision != settings.UseVision ||
                     _lastUseRealVisionInSimulation != settings.UseRealVisionInSimulation);

                _lastSimulationMode = settings.SimulationMode;
                _lastDryRunMode = settings.DryRunMode;
                _lastUseAjin = settings.UseAjin;
                _lastUseVision = settings.UseVision;
                _lastUseRealVisionInSimulation = settings.UseRealVisionInSimulation;
                _hybridModeSnapshotReady = true;
            }

            if (changed && invalidateSessionOnChange)
                QMC.CDT320.Materials.InputStageHybridResultSession.Clear();
        }

        public static AppSettings Load()
        {
            if (!File.Exists(Path_))
            {
                Current = new AppSettings();
                RefreshHybridModeSnapshot(true);
                QMC.Common.Motion.MotionSpeedScale.ScalePercent = Current.DefaultVelocityScalePercent;
                ApplySequenceSpeedScaleSettings(Current);
                ApplyLogPolicySettings(Current);
                return Current;
            }
            try
            {
                using (var fs = File.OpenRead(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(AppSettings));
                    Current = (AppSettings)ser.ReadObject(fs);
                }

                if (!Current.UseAjin && !Current.SimulationMode && !Current.DryRunMode)
                    Current.SimulationMode = true;
            }
            catch { Current = new AppSettings(); }

            // 저장된 전체 DefaultVelocity 퍼센트를 안전 범위로 보정한 뒤 모션 레이어 공통 스케일에 동기화한다.
            Current.DefaultVelocityScalePercent =
                QMC.Common.Motion.MotionSpeedScale.ClampPercent(Current.DefaultVelocityScalePercent);
            QMC.Common.Motion.MotionSpeedScale.ScalePercent = Current.DefaultVelocityScalePercent;
            ApplySequenceSpeedScaleSettings(Current);
            ApplyLogPolicySettings(Current);
            RefreshHybridModeSnapshot(true);
            return Current;
        }

        /// <summary>
        /// 저장된 Manual/READY 시퀀스 전용 퍼센트를 안전 범위로 보정한 뒤 모션 레이어에 동기화한다.
        /// 두 값 모두 EffectiveScaleFactor를 통해 속도와 가감속에 같은 배율로 적용된다.
        /// </summary>
        private static void ApplySequenceSpeedScaleSettings(AppSettings settings)
        {
            if (settings == null)
                return;

            settings.ManualSequenceScalePercent =
                QMC.Common.Motion.MotionSpeedScale.ClampPercent(settings.ManualSequenceScalePercent);
            settings.ReadySequenceScalePercent =
                QMC.Common.Motion.MotionSpeedScale.ClampPercent(settings.ReadySequenceScalePercent);

            QMC.Common.Motion.MotionSpeedScale.ManualSequencePercent = settings.ManualSequenceScalePercent;
            QMC.Common.Motion.MotionSpeedScale.ReadySequencePercent = settings.ReadySequenceScalePercent;
        }

        // 최소 로그 정책과 픽커 유휴 폴 주기를 로드된 설정으로 적용한다.
        private static void ApplyLogPolicySettings(AppSettings settings)
        {
            try
            {
                if (settings == null)
                    return;

                QMC.Common.Logging.LogPolicy.Configure(
                    settings.LogBlackboxCapacity,
                    settings.LogBlackboxWindowSeconds,
                    settings.LogPersistCodePrefixes);

                if (settings.LogDiagnosticVerboseOnStart)
                    QMC.Common.Logging.LogPolicy.EnableDiagnosticVerbose(settings.LogDiagnosticVerboseMinutes, "SETTINGS");

                QMC.CDT320.Sequencing.PickerSequenceIdlePolicy.Configure(settings.PickerIdlePollMs);
            }
            catch
            {
            }
        }

        public static void Save()
        {
            try
            {
                RefreshHybridModeSnapshot(true);
                using (var fs = File.Create(Path_))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(AppSettings), Current);
                }
            }
            catch { }
        }
    }
}
