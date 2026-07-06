using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

namespace QMC.CDT320
{
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
        /// 통과 처리(bypass)하여 비전 없이도 동작한다(시뮬/DryRun 바이패스와 동일 경로). 기본 true.</summary>
        [DataMember] public bool   UseVision            { get; set; } = true;

        /// <summary>뷰어 측정 스케일 계수 — 표시 mm/px = 저장 스케일(mm/px) × 이 계수.
        /// Vision 이 뷰어 이미지를 다운스케일(원본→표시)하면 표시 1px 가 더 넓어지므로 계수=원본폭/표시폭(예 5120/1600=3.2)로 보정한다.
        /// 기본 1.0 = 저장 스케일 그대로(다운스케일 없음/풀해상도). 계수 ≤ 0 이면 자동(표시폭 meta.Width에서 산출). 향후 다운스케일 파라미터화 시 이 값에 반영.</summary>
        [DataMember] public double ViewerMeasureScaleFactor { get; set; } = 1.0;

        // ── Barcode link (CDT-310 매뉴얼 사양 — Serial Port 4/6) ──
        /// <summary>Stage 43 — Wafer Barcode 시리얼 포트 번호.</summary>
        [DataMember] public int    WaferBarcodeSerialPort { get; set; } = 4;
        /// <summary>Stage 43 — Bin Barcode 시리얼 포트 번호.</summary>
        [DataMember] public int    BinBarcodeSerialPort   { get; set; } = 6;
        /// <summary>Stage 43 — 시리얼 baudrate (기본 9600).</summary>
        [DataMember] public int    BarcodeSerialBaud      { get; set; } = 9600;

        // ── Simulator link — auto connect ──
        [DataMember] public bool   SimulatorAutoConnect { get; set; } = false;

        // ── Motion speed scale ──
        /// <summary>
        /// 전체 공통 DefaultVelocity 퍼센트 스케일 [%]. 100 = 설정값 그대로, 10 = 설정값의 10% 속도.
        /// 자동 시퀀스 일반 이동(DefaultVelocity 기반)에만 적용된다. 안전 범위 1~100.
        /// </summary>
        [DataMember] public double DefaultVelocityScalePercent { get; set; } = 100.0;

        /// <summary>
        /// 자동 시퀀스 테스트 중 Input/Output 카메라 X 이동과 비전 검사를 생략하고 Picker 모션만 확인한다.
        /// 실장비 생산용 안전 인터락은 우회하지 않는다.
        /// </summary>
        [DataMember] public bool   PickerMotionOnlyTestMode { get; set; } = false;

        /// <summary>
        /// 이력 탭의 로그(Event/시퀀스) 이력 화면 사용 여부. false 면 해당 페이지들은 안내만 표시한다.
        /// 로그 폭주 등으로 문제가 보일 때 빌드 없이 끌 수 있는 안전 스위치. 기본 true.
        /// </summary>
        [DataMember] public bool   FileLogHistoryEnabled { get; set; } = true;

        /// <summary>
        /// 압축 보관본(Log\Archive\*.zip) 보존일수. 14일이 지난 원본 로그는 자동으로 압축 보관되고(고정 규칙),
        /// 압축본은 이 일수가 지나면 최종 삭제된다(복구 불가). 0 = 무기한 보관(OFF). 기본 0.
        /// </summary>
        [DataMember] public int    ArchiveKeepDays { get; set; } = 0;

        // DataContractJsonSerializer 는 필드 이니셜라이저를 실행하지 않으므로, 구 settings.json 에 없는
        // 신규 키는 여기서 기본값을 심는다(없으면 false 로 로드되어 의도치 않게 비전이 꺼지는 문제 방지).
        [OnDeserializing]
        internal void OnDeserializing(StreamingContext ctx)
        {
            UseVision = true;
            ViewerMeasureScaleFactor = 1.0;   // 구 settings.json 에 키 없으면 0 으로 로드되는 것 방지(기본=저장 스케일 그대로)
            FileLogHistoryEnabled = true;   // 구 settings.json 에 키가 없으면 false 로 로드되어 이력 화면이 꺼지는 문제 방지
            // ArchiveKeepDays 는 키가 없으면 0(무기한 보관)으로 로드되며, 이는 기본값과 같아 별도 처리가 필요 없다.
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

        static AppSettingsStore() { Directory.CreateDirectory(Dir); }

        public static AppSettings Load()
        {
            if (!File.Exists(Path_))
            {
                Current = new AppSettings();
                QMC.Common.Motion.MotionSpeedScale.ScalePercent = Current.DefaultVelocityScalePercent;
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
            return Current;
        }

        public static void Save()
        {
            try
            {
                using (var fs = File.Create(Path_))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(AppSettings), Current);
                }
            }
            catch { }
        }
    }
}
