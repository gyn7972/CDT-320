using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QMC.Vision.Config
{
    /// <summary>어떤 비전 백엔드를 사용할지 선택.</summary>
    public enum VisionProvider { Sim, OpenCv, Cognex }

    [DataContract]
    public class VisionSettings
    {
        [DataMember] public VisionProvider Provider         { get; set; } = VisionProvider.Sim;
        [DataMember] public string         Language         { get; set; } = "ko";

        /// <summary>Cognex VisionPro DLL 폴더(직접 지정). 비우면 표준 설치 경로를 자동 탐색.
        /// 설치 폴더가 비표준일 때 이 값을 채우면 최우선으로 검색한다. (예: C:\Program Files\Cognex\VisionPro\CogPlus)</summary>
        [DataMember] public string         CognexBinPath    { get; set; } = "";

        /// <summary>EmguCV(OpenCV) DLL 폴더. 용량이 큰 네이티브(cvextern.dll)를 bin 에 두지 않고 별도 폴더에서 로드.
        /// 비우면 기본 <see cref="DefaultOpenCvBinPath"/>(D:\CDT-320\EmguCV)와 앱 폴더를 탐색.</summary>
        [DataMember] public string         OpenCvBinPath    { get; set; } = "";

        /// <summary>OpenCvBinPath 미지정 시 사용할 기본 EmguCV 폴더.</summary>
        public const string DefaultOpenCvBinPath = @"D:\CDT-320\EmguCV";

        /// <summary>Sim 모드에서 핸들러 TCP 없이 비전이 자체 시퀀스를 순차 실행(자동 실행). GENERAL 토글.</summary>
        [DataMember] public bool           SimAutoSequence      { get; set; } = false;
        /// <summary>Sim 자동 시퀀스 한 사이클 간격(ms).</summary>
        [DataMember] public int            SimSequenceIntervalMs{ get; set; } = 500;
        /// <summary>Sim 자체 시퀀스 구동 시 합성 chipUid를 발급해 실제(핸들러) 흐름과 동일하게
        /// MaterialTracker/이미지·데이터 로그까지 태운다. 알고리즘 검증용. 기본 false(로그 미기록).</summary>
        [DataMember] public bool           SimEmitChipUid       { get; set; } = false;
        /// <summary>마지막으로 적용/저장한 레시피(품목)명 — 재시작 시 복원.</summary>
        [DataMember] public string         LastRecipeName   { get; set; } = "";

        /// <summary>레시피/설비데이터 저장 루트(절대경로). 비우면 기본 <see cref="DefaultDataRoot"/> 사용.
        /// GENERAL 설정에서 변경하며, 변경은 재시작 후 반영(기동 시 DataPaths.Root 로 적용).</summary>
        [DataMember] public string         DataRootPath     { get; set; } = "";

        /// <summary>DataRootPath 미지정 시 사용할 고정 기본 루트.</summary>
        public const string DefaultDataRoot = @"D:\CDT-320";

        /// <summary>실제 적용할 데이터 루트(설정값 우선, 없으면 기본 고정 경로).</summary>
        public string EffectiveDataRoot
        {
            get { return string.IsNullOrWhiteSpace(DataRootPath) ? DefaultDataRoot : DataRootPath; }
        }

        // Stage 73 — 조명 컨트롤러 Sim 여부. 비전 백엔드(Provider)와 독립.
        // true(기본,안전) = 기동 시 SimLightController. 실제 점등 테스트는 조명 Setup 페이지의
        // '조명 연결' 버튼으로 실장비 재초기화 + 시리얼 Open. (Cognex 키 없이 Provider=Sim 이어도 조명은 실제 가능)
        [DataMember] public bool           LightUseSim      { get; set; } = false;   // Stage 89 — 실장비 기본. Sim 은 명시 opt-in("LightUseSim":true). (구 OnDeserializing 강제 true 제거)

        // 모듈별 TCP 포트 (CDT-310 매뉴얼 기준 기본값)
        [DataMember] public int WaferVisionPort             { get; set; } = 5100;
        [DataMember] public int InspectionVisionPort        { get; set; } = 5101;
        [DataMember] public int BinVisionPort               { get; set; } = 5103;
        // Stage 44 — 매뉴얼 호환 추가 채널. 핸들러 기준 모듈명 통일: TopSideVision(5105)/BottomSideVision(5106)
        [DataMember] public int TopSideVisionPort           { get; set; } = 5105;
        [DataMember] public int BottomSideVisionPort        { get; set; } = 5106;
        // 전역 통신(MainComm) 포트 — 핸들러 VisionHub.Main(5104) 과 짝. 레시피/전역 명령 수신.
        [DataMember] public int MainCommPort                { get; set; } = 5104;

        // 구버전 키 마이그레이션 (값 있으면 OnDeserialized 가 새 프로퍼티로 이전 후 0 으로 비움 → 다음 Save 시 사라짐)
        [DataMember(Name = "TopSideInspectionPort",    EmitDefaultValue = false)] public int LegacyTopSideInspectionPort    { get; set; }
        [DataMember(Name = "BottomSideInspectionPort", EmitDefaultValue = false)] public int LegacyBottomSideInspectionPort { get; set; }
        [DataMember(Name = "FrontSideInspectionPort",  EmitDefaultValue = false)] public int LegacyFrontSideInspectionPort  { get; set; }
        [DataMember(Name = "RearSideInspectionPort",   EmitDefaultValue = false)] public int LegacyRearSideInspectionPort   { get; set; }

        // DataContractJsonSerializer 는 생성자/프로퍼티 이니셜라이저를 실행하지 않는다.
        // 따라서 구버전 json 에 없는 뷰어 키는 기본값 대신 0/null 로 남는다 → 역직렬화 전에 기본값을 심어
        // "키가 있으면 덮어쓰고, 없으면 기본값 유지" 가 되도록 한다.
        [OnDeserializing]
        internal void OnDeserializing(StreamingContext ctx)
        {
            // Stage 89 — LightUseSim 강제 true 제거: JSON 값 그대로 반영(없으면 default false). 사용자가 false 저장하면 유지.
            // 신규 키(구 json 에 없음) 기본값 — DataContractJsonSerializer 는 이니셜라이저를 실행하지 않으므로 여기서 심는다.
            MainCommPort         = 5104;
            SimSequenceIntervalMs = 500;
            RemoteViewerEnable   = true;
            RemoteViewerSource   = "GrabImage";
            RemoteViewerFps      = 10;
            RemoteViewerQuality  = 60;
            WaferViewerPort      = 5200;
            InspectionViewerPort = 5201;
            BinViewerPort        = 5203;
            FrontSideViewerPort  = 5205;
            RearSideViewerPort   = 5206;
            MilDcfPath           = "";
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext ctx)
        {
            if (LegacyTopSideInspectionPort != 0)    { TopSideVisionPort    = LegacyTopSideInspectionPort;    LegacyTopSideInspectionPort    = 0; }
            if (LegacyBottomSideInspectionPort != 0) { BottomSideVisionPort = LegacyBottomSideInspectionPort; LegacyBottomSideInspectionPort = 0; }
            if (LegacyFrontSideInspectionPort != 0)  { TopSideVisionPort    = LegacyFrontSideInspectionPort;  LegacyFrontSideInspectionPort  = 0; }
            if (LegacyRearSideInspectionPort != 0)   { BottomSideVisionPort = LegacyRearSideInspectionPort;   LegacyRearSideInspectionPort   = 0; }

            // 뷰어 블록이 통째로 비어있는(=내 interim 빌드가 0 으로 저장했거나 구버전) 경우 자가 치유.
            bool viewerUnconfigured = string.IsNullOrEmpty(RemoteViewerSource)
                && WaferViewerPort == 0 && InspectionViewerPort == 0 && BinViewerPort == 0
                && FrontSideViewerPort == 0 && RearSideViewerPort == 0;
            if (viewerUnconfigured) RemoteViewerEnable = true;
            if (string.IsNullOrEmpty(RemoteViewerSource)) RemoteViewerSource = "GrabImage";
            if (RemoteViewerFps     <= 0) RemoteViewerFps     = 10;
            if (RemoteViewerQuality <= 0) RemoteViewerQuality = 60;
            if (WaferViewerPort      <= 0) WaferViewerPort      = 5200;
            if (InspectionViewerPort <= 0) InspectionViewerPort = 5201;
            if (BinViewerPort        <= 0) BinViewerPort        = 5203;
            if (FrontSideViewerPort  <= 0) FrontSideViewerPort  = 5205;
            if (RearSideViewerPort   <= 0) RearSideViewerPort   = 5206;
        }

        [DataMember] public string ImageLogPath             { get; set; } = @".\Log\Image";
        [DataMember] public bool   ImageLogEnable           { get; set; } = false;

        // ── Matrox MIL 카메라 (Camera Link / CoaXPress) ──
        /// <summary>MIL 디지타이저 DataFormat. 비우면 "M_DEFAULT"(CXP GenICam 자동). CameraLink 면 .dcf 경로.
        /// 시스템(보드)은 항상 M_SYSTEM_DEFAULT(MILConfig 기본) 사용 — 별도 설정 불필요.</summary>
        [DataMember] public string MilDcfPath               { get; set; } = "";

        // ── 원격 뷰어 (그랩 영상 송출) — Handler 등 외부에서 SP_RemoteViewer 호환 클라이언트로 수신 ──
        [DataMember] public bool   RemoteViewerEnable       { get; set; } = true;
        /// <summary>"GrabImage"(기본) 또는 "ScreenRegion".</summary>
        [DataMember] public string RemoteViewerSource       { get; set; } = "GrabImage";
        [DataMember] public int    RemoteViewerFps          { get; set; } = 10;
        [DataMember] public int    RemoteViewerQuality      { get; set; } = 60;
        // 모듈(스테이션)별 뷰어 포트 — 명령포트(5100~5106)와 분리.
        [DataMember] public int    WaferViewerPort          { get; set; } = 5200;
        [DataMember] public int    InspectionViewerPort     { get; set; } = 5201; // Bottom
        [DataMember] public int    BinViewerPort            { get; set; } = 5203;
        [DataMember] public int    FrontSideViewerPort      { get; set; } = 5205;
        [DataMember] public int    RearSideViewerPort       { get; set; } = 5206;
        // ScreenRegion 소스용 사각형 (0,0,0,0 = 주 모니터 전체)
        [DataMember] public int    RemoteViewerScreenX      { get; set; } = 0;
        [DataMember] public int    RemoteViewerScreenY      { get; set; } = 0;
        [DataMember] public int    RemoteViewerScreenW      { get; set; } = 0;
        [DataMember] public int    RemoteViewerScreenH      { get; set; } = 0;

        /// <summary>모듈별 카메라 ID (백엔드에 따라 의미 다름).</summary>
        [DataMember] public string WaferCameraId            { get; set; } = "Sim/Wafer";
        [DataMember] public string BinCameraId              { get; set; } = "Sim/Bin";
        [DataMember] public string BottomInspectionCameraId { get; set; } = "Sim/BottomInspection";

        // ── 좌표 변환 / 카메라 벡터 (310 이식) ──
        // [DEPRECATED/LEGACY] 아래 스케일/회전/반전/ReturnMmCoordinates 전역 필드는 더 이상 런타임에서 읽지 않는다.
        // 좌표 변환 SSOT = 모듈별 CameraConfig(ExportCameraMapping). 런타임 변환은 VisionCommandCore.Match 가
        // m.ExportCameraMapping() 값만 사용한다. 이 필드들은 구 json 호환을 위해 남겨둘 뿐(신규 사용 금지).
        /// <summary>[LEGACY 미사용] 스케일 X (mm/pixel). SSOT=모듈 CameraConfig.ScaleX.</summary>
        [DataMember] public double ScaleX                   { get; set; } = 1.0;
        /// <summary>[LEGACY 미사용] 스케일 Y (mm/pixel). SSOT=모듈 CameraConfig.ScaleY.</summary>
        [DataMember] public double ScaleY                   { get; set; } = 1.0;
        /// <summary>[LEGACY 미사용] 카메라 90° 회전. SSOT=모듈 CameraConfig.IsRotated.</summary>
        [DataMember] public bool   IsRotated                { get; set; } = false;
        /// <summary>[LEGACY 미사용] X 부호 반전. SSOT=모듈 CameraConfig.InvertedX.</summary>
        [DataMember] public bool   InvertedX                { get; set; } = false;
        /// <summary>[LEGACY 미사용] Y 부호 반전. SSOT=모듈 CameraConfig.InvertedY.</summary>
        [DataMember] public bool   InvertedY                { get; set; } = false;
        /// <summary>[LEGACY 미사용] 그랩 직전 지연(ms). SSOT=모듈 CameraConfig.DelayBeforeGrabMs([설정>카메라]에서 모듈별 설정).</summary>
        [DataMember] public int    DelayBeforeGrabMs        { get; set; } = 0;
        /// <summary>측면검사 위치 (None/Front/Back). SideInspection 결과 매핑에 사용.</summary>
        [DataMember] public string SideLocation             { get; set; } = "None";
        /// <summary>오토포커스 그랩 후 조명 자동 OFF.</summary>
        [DataMember] public bool   OffAfterGrabWhenAutoFocus{ get; set; } = false;
        /// <summary>[LEGACY 미사용] 응답 픽셀→mm 변환 포함 여부. SSOT=모듈 CameraConfig.ReturnMmCoordinates.</summary>
        [DataMember] public bool   ReturnMmCoordinates      { get; set; } = false;

        // ── 데이터 로그 ──
        [DataMember] public bool   DataLogEnable            { get; set; } = true;
        [DataMember] public string DataLogPath              { get; set; } = @".\Log\Data";

        // ── 리소스(CPU/메모리) 모니터 로그 — PC 사양 산정용. 켜면 1초 간격 CSV 기록.
        [DataMember] public bool   ResourceLogEnable        { get; set; } = false;
    }

    public static class VisionConfigStore
    {
        public static string Dir  { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string Path_ { get; } = System.IO.Path.Combine(Dir, "vision.json");

        public static VisionSettings Current { get; private set; } = new VisionSettings();

        static VisionConfigStore() { Directory.CreateDirectory(Dir); }

        public static VisionSettings Load()
        {
            if (!File.Exists(Path_)) { Current = new VisionSettings(); Save(); return Current; }
            try
            {
                using (var fs = File.OpenRead(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(VisionSettings));
                    Current = (VisionSettings)ser.ReadObject(fs);
                }
            }
            catch { Current = new VisionSettings(); }
            // Stage 63 — OnDeserialized 가 구 포트 키를 새 프로퍼티로 옮겼을 수 있음 → 정규화 재저장.
            Save();
            return Current;
        }

        public static void Save()
        {
            try
            {
                using (var fs = File.Create(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(VisionSettings));
                    ser.WriteObject(fs, Current);
                }
            }
            catch { }
        }
    }
}
