using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QMC.Vision.Config
{
    /// <summary>
    /// 비전 알고리즘 이름 — 5 모듈 (Wafer/Bin/BottomInspection/TopSide/BottomSide).
    /// 향후 모듈 추가 시 <see cref="AlgorithmCameraMapStore.EnsureDefaults"/> 에서 항목 추가.
    /// </summary>
    public static class VisionAlgorithm
    {
        public const string Wafer            = "Wafer";
        public const string Bin              = "Bin";
        public const string BottomInspection = "BottomInspection";
        public const string TopSide          = "TopSide";
        public const string BottomSide       = "BottomSide";

        public static readonly string[] All =
            { Wafer, Bin, BottomInspection, TopSide, BottomSide };

        /// <summary>UI 표시용 한글 라벨.</summary>
        public static string Label(string name)
        {
            switch (name)
            {
                case Wafer:            return "웨이퍼 비전";
                case Bin:              return "빈 비전";
                case BottomInspection: return "바텀 검사";
                case TopSide:          return "상면 검사";
                case BottomSide:       return "하면 검사";
                default:               return name;
            }
        }
    }

    /// <summary>알고리즘 1 개에 묶이는 카메라 + 카메라 파라미터.</summary>
    [DataContract]
    public class AlgorithmCameraMapping
    {
        [DataMember] public string Algorithm { get; set; } = "";
        /// <summary>CameraFactory.CreateById 가 인식하는 ID — "Sim/X" 또는 GigE IP.</summary>
        [DataMember] public string CameraId  { get; set; } = "";

        // 카메라 파라미터 (Form1.Load 가 Camera.Open 후 적용)
        [DataMember] public double ExposureUs        { get; set; } = 5000;
        [DataMember] public double Gain              { get; set; } = 1.0;
        [DataMember] public double FrameRate         { get; set; } = 30;
        [DataMember] public string TriggerMode       { get; set; } = "Software";
        [DataMember] public string PixelFormat       { get; set; } = "Mono8";
        [DataMember] public int    DelayBeforeGrabMs { get; set; } = 0;

        // Stage 62 — ROI (AOI). 0 = full sensor (Width 또는 Height 가 0 이하이면 미적용).
        [DataMember] public int RoiOffsetX { get; set; } = 0;
        [DataMember] public int RoiOffsetY { get; set; } = 0;
        [DataMember] public int RoiWidth   { get; set; } = 0;
        [DataMember] public int RoiHeight  { get; set; } = 0;

        public bool IsRoiFull => RoiWidth <= 0 || RoiHeight <= 0;
        public System.Drawing.Rectangle ToRectangle()
            => new System.Drawing.Rectangle(RoiOffsetX, RoiOffsetY, RoiWidth, RoiHeight);

        public AlgorithmCameraMapping Clone()
        {
            return new AlgorithmCameraMapping
            {
                Algorithm = Algorithm, CameraId = CameraId,
                ExposureUs = ExposureUs, Gain = Gain, FrameRate = FrameRate,
                TriggerMode = TriggerMode, PixelFormat = PixelFormat,
                DelayBeforeGrabMs = DelayBeforeGrabMs,
                RoiOffsetX = RoiOffsetX, RoiOffsetY = RoiOffsetY,
                RoiWidth = RoiWidth, RoiHeight = RoiHeight
            };
        }
    }

    [DataContract]
    public class AlgorithmCameraMap
    {
        [DataMember] public List<AlgorithmCameraMapping> Items { get; set; } = new List<AlgorithmCameraMapping>();

        public AlgorithmCameraMapping Get(string algorithm)
            => Items?.FirstOrDefault(m => string.Equals(m.Algorithm, algorithm, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>알고리즘-카메라 매핑 영속화 — Config\algorithm_camera.json.</summary>
    public static class AlgorithmCameraMapStore
    {
        public static string Dir  { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        public static string Path_ { get; } = System.IO.Path.Combine(Dir, "algorithm_camera.json");

        public static AlgorithmCameraMap Current { get; private set; } = new AlgorithmCameraMap();

        static AlgorithmCameraMapStore() { Directory.CreateDirectory(Dir); }

        public static AlgorithmCameraMap Load()
        {
            if (!File.Exists(Path_))
            {
                Current = new AlgorithmCameraMap();
                EnsureDefaults(Current);
                Save();
                return Current;
            }
            try
            {
                using (var fs = File.OpenRead(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(AlgorithmCameraMap));
                    Current = (AlgorithmCameraMap)ser.ReadObject(fs);
                }
            }
            catch { Current = new AlgorithmCameraMap(); }
            if (Current == null) Current = new AlgorithmCameraMap();
            EnsureDefaults(Current);
            return Current;
        }

        public static void Save()
        {
            try
            {
                using (var fs = File.Create(Path_))
                {
                    var ser = new DataContractJsonSerializer(typeof(AlgorithmCameraMap));
                    ser.WriteObject(fs, Current);
                }
            }
            catch { }
        }

        /// <summary>5 알고리즘 항목이 빠짐없이 존재하도록 보장.
        /// 누락 시 VisionSettings 의 기존 카메라 ID(있다면) 또는 Sim 으로 채움.</summary>
        public static void EnsureDefaults(AlgorithmCameraMap map)
        {
            if (map.Items == null) map.Items = new List<AlgorithmCameraMapping>();
            var legacy = VisionConfigStore.Current ?? new VisionSettings();

            foreach (var alg in VisionAlgorithm.All)
            {
                if (map.Items.Any(m => string.Equals(m.Algorithm, alg, StringComparison.OrdinalIgnoreCase)))
                    continue;

                string fallbackCam;
                switch (alg)
                {
                    case VisionAlgorithm.Wafer:            fallbackCam = legacy.WaferCameraId            ?? "Sim/Wafer";       break;
                    case VisionAlgorithm.Bin:              fallbackCam = legacy.BinCameraId              ?? "Sim/Bin";         break;
                    case VisionAlgorithm.BottomInspection: fallbackCam = legacy.BottomInspectionCameraId ?? "Sim/BottomInsp";  break;
                    case VisionAlgorithm.TopSide:          fallbackCam = "Sim/TopSide";    break;
                    case VisionAlgorithm.BottomSide:       fallbackCam = "Sim/BottomSide"; break;
                    default:                               fallbackCam = "Sim/0";          break;
                }

                map.Items.Add(new AlgorithmCameraMapping
                {
                    Algorithm = alg,
                    CameraId  = fallbackCam
                });
            }
        }
    }
}
