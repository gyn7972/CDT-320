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
        /// <summary>Stage 43 — 매뉴얼 추가: TopSide Inspection Vision (5105).</summary>
        [DataMember] public int    VisionTopSidePort    { get; set; } = 5105;
        /// <summary>Stage 43 — 매뉴얼 추가: BottomSide Inspection Vision (5106).</summary>
        [DataMember] public int    VisionBottomSidePort { get; set; } = 5106;
        /// <summary>앱 시작 시 자동 연결 시도 여부.</summary>
        [DataMember] public bool   VisionAutoConnect    { get; set; } = true;

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
