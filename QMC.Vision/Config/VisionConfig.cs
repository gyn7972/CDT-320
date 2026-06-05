using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Vision.Core;

namespace QMC.Vision.Config
{
    /// <summary>어떤 비전 백엔드를 사용할지 선택.</summary>
    public enum VisionProvider { Sim, OpenCv, Cognex }

    [DataContract]
    public class VisionSettings
    {
        [DataMember] public VisionProvider Provider         { get; set; } = VisionProvider.Sim;
        [DataMember] public string         Language         { get; set; } = "ko";

        // 모듈별 TCP 포트 (CDT-310 매뉴얼 기준 기본값)
        [DataMember] public int WaferVisionPort             { get; set; } = 5100;
        [DataMember] public int InspectionVisionPort        { get; set; } = 5101;
        [DataMember] public int BinVisionPort               { get; set; } = 5103;
        [DataMember] public int MainCommunicatorPort        { get; set; } = 5104;
        // Stage 44 — 매뉴얼 호환 추가 채널
        [DataMember] public int TopSideInspectionPort       { get; set; } = 5105;
        [DataMember] public int BottomSideInspectionPort    { get; set; } = 5106;

        [DataMember] public string ImageLogPath             { get; set; } = @".\Log\Image";
        [DataMember] public bool   ImageLogEnable           { get; set; } = false;

        /// <summary>모듈별 카메라 ID (백엔드에 따라 의미 다름).</summary>
        [DataMember] public string WaferCameraId            { get; set; } = "Sim/Wafer";
        [DataMember] public string BinCameraId              { get; set; } = "Sim/Bin";
        [DataMember] public string BottomInspectionCameraId { get; set; } = "Sim/BottomInspection";

        // ── 좌표 변환 / 카메라 벡터 (310 이식) ──
        /// <summary>스케일 X (mm/pixel). VisionScale 명령으로 자동 캘리브레이션 가능.</summary>
        [DataMember] public double ScaleX                   { get; set; } = 1.0;
        /// <summary>스케일 Y (mm/pixel).</summary>
        [DataMember] public double ScaleY                   { get; set; } = 1.0;
        /// <summary>카메라 90° 회전 장착 — 결과 X↔Y 스왑.</summary>
        [DataMember] public bool   IsRotated                { get; set; } = false;
        /// <summary>X 부호 반전.</summary>
        [DataMember] public bool   InvertedX                { get; set; } = false;
        /// <summary>Y 부호 반전.</summary>
        [DataMember] public bool   InvertedY                { get; set; } = false;
        /// <summary>그랩 직전 지연(ms). 일시적 진동 회피용.</summary>
        [DataMember] public int    DelayBeforeGrabMs        { get; set; } = 0;
        /// <summary>측면검사 위치 (None/Front/Back). SideInspection 결과 매핑에 사용.</summary>
        [DataMember] public string SideLocation             { get; set; } = "None";
        /// <summary>오토포커스 그랩 후 조명 자동 OFF.</summary>
        [DataMember] public bool   OffAfterGrabWhenAutoFocus{ get; set; } = false;
        /// <summary>응답에 픽셀→mm 변환 좌표를 포함할지.</summary>
        [DataMember] public bool   ReturnMmCoordinates      { get; set; } = false;

        // ── 데이터 로그 ──
        [DataMember] public bool   DataLogEnable            { get; set; } = true;
        [DataMember] public string DataLogPath              { get; set; } = @".\Log\Data";
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
            return Current;
        }

        public static void Save()
        {
            try
            {
                using (var fs = File.Create(Path_))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(VisionSettings), Current);
                }
            }
            catch { }
        }
    }
}
