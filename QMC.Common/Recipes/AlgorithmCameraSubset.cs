using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace QMC.Common.Recipes
{
    /// <summary>
    /// 비전 알고리즘 이름 상수 — Handler 와 Vision 양쪽에서 동일 키 사용.
    /// 향후 모듈 추가 시 <see cref="All"/> 에 항목 추가.
    /// </summary>
    public static class VisionAlgorithm
    {
        public const string Wafer            = "Wafer";
        public const string Bin              = "Bin";
        public const string BottomInspection = "BottomInspection";
        public const string FrontSide        = "FrontSide";
        public const string RearSide         = "RearSide";

        public static readonly string[] All =
            { Wafer, Bin, BottomInspection, FrontSide, RearSide };

        /// <summary>UI 표시용 한글 라벨.</summary>
        public static string Label(string name)
        {
            switch (name)
            {
                case Wafer:            return "웨이퍼 비전";
                case Bin:              return "빈 비전";
                case BottomInspection: return "바텀 검사";
                case FrontSide:        return "앞쪽 측면 검사";
                case RearSide:         return "뒤쪽 측면 검사";
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

        // 카메라 파라미터
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

        // (C3b-1) 검사별 카메라 override 제거. (C3b-2) 검사별 조명(InspectionLights/InspectionLightOverride)도
        // 노드 Recipe(LightSettings) SSOT 로 대체되어 제거 → AlgorithmCameraMapping = 순수 카메라 DTO.

        public AlgorithmCameraMapping Clone()
            => new AlgorithmCameraMapping
            {
                Algorithm = Algorithm, CameraId = CameraId,
                ExposureUs = ExposureUs, Gain = Gain, FrameRate = FrameRate,
                TriggerMode = TriggerMode, PixelFormat = PixelFormat,
                DelayBeforeGrabMs = DelayBeforeGrabMs,
                RoiOffsetX = RoiOffsetX, RoiOffsetY = RoiOffsetY,
                RoiWidth = RoiWidth, RoiHeight = RoiHeight
            };
    }

    /// <summary>Stage 64 — 검사 ID → 한글 라벨 (알고리즘 스코프).</summary>
    public static class InspectionLabel
    {
        // (Algorithm, InspectionId) → 한글 라벨. 중복 InspectionId 는 알고리즘으로 구분.
        private static readonly Dictionary<string, string> _map =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Wafer (7)
            { "Wafer|EjectPinFinder",            "이젝트 핀" },
            { "Wafer|ReticleFinder",             "레티클" },
            { "Wafer|AlignDieFinder",            "얼라인 다이" },
            { "Wafer|FirstReferenceFinder",      "첫째 기준" },
            { "Wafer|SecondReferenceFinder",     "둘째 기준" },
            { "Wafer|DieFinder",                 "다이" },
            { "Wafer|ScaleFinder",               "스케일" },
            // Bin (4)
            { "Bin|ReticleFinder",               "레티클" },
            { "Bin|DieFinder",                   "다이" },
            { "Bin|PlacementInspector",          "안착 검사" },
            { "Bin|ScaleFinder",                 "스케일" },
            // BottomInspection (7)
            { "BottomInspection|ReticleFinder",         "레티클" },
            { "BottomInspection|ColletFinder",          "콜렛" },
            { "BottomInspection|DieFinder",             "다이" },
            { "BottomInspection|SurfaceInspector",      "표면" },
            { "BottomInspection|FocusFinder",           "포커스" },
            { "BottomInspection|ScaleFinder",           "스케일" },
            { "BottomInspection|DistortionCompensation","왜곡 보정" },
            // FrontSide (4) — 앞쪽 측면
            { "FrontSide|DieEdgeFinder",         "다이 에지" },
            { "FrontSide|TopSurfaceInspector",   "앞쪽 면" },
            { "FrontSide|TopChippingInspector",  "앞쪽 칩핑" },
            { "FrontSide|FocusFinder",           "포커스" },
            // RearSide (4) — 뒤쪽 측면
            { "RearSide|DieEdgeFinder",          "다이 에지" },
            { "RearSide|BottomSurfaceInspector", "뒤쪽 면" },
            { "RearSide|BottomChippingInspector","뒤쪽 칩핑" },
            { "RearSide|FocusFinder",            "포커스" },
        };

        public static string Get(string algorithm, string inspectionId)
        {
            if (_map.TryGetValue(algorithm + "|" + inspectionId, out var label)) return label;
            return inspectionId;   // 미등록 시 ID 그대로
        }

        /// <summary>알고리즘별 검사 ID 목록 (모듈 등록 순서와 동일). UI TreeView 자식 노드 생성용.</summary>
        public static string[] InspectionsOf(string algorithm)
        {
            switch (algorithm)
            {
                case VisionAlgorithm.Wafer:
                    return new[] { "EjectPinFinder", "ReticleFinder", "AlignDieFinder",
                                   "FirstReferenceFinder", "SecondReferenceFinder", "DieFinder", "ScaleFinder" };
                case VisionAlgorithm.Bin:
                    return new[] { "ReticleFinder", "DieFinder", "PlacementInspector", "ScaleFinder" };
                case VisionAlgorithm.BottomInspection:
                    return new[] { "ReticleFinder", "ColletFinder", "DieFinder", "SurfaceInspector",
                                   "FocusFinder", "ScaleFinder", "DistortionCompensation" };
                case VisionAlgorithm.FrontSide:
                    return new[] { "DieEdgeFinder", "TopSurfaceInspector", "TopChippingInspector", "FocusFinder" };
                case VisionAlgorithm.RearSide:
                    return new[] { "DieEdgeFinder", "BottomSurfaceInspector", "BottomChippingInspector", "FocusFinder" };
                default:
                    return new string[0];
            }
        }
    }

    /// <summary>
    /// Recipe 단위 비전 알고리즘 ↔ 카메라 매핑 집합.
    /// RecipeProject 에 직접 포함됨 (Handler 측 Recipe 파일에 저장).
    /// Vision 측은 전역 algorithm_camera.json 과 동일 형식으로 양쪽 사용 가능.
    /// </summary>
    [DataContract]
    public class AlgorithmCameraSubset
    {
        [DataMember] public List<AlgorithmCameraMapping> Items { get; set; } = new List<AlgorithmCameraMapping>();

        public AlgorithmCameraMapping Get(string algorithm)
            => Items?.FirstOrDefault(m => string.Equals(m.Algorithm, algorithm, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// 구버전 알고리즘 이름 자동 마이그레이션.
        /// TopSide → FrontSide, BottomSide → RearSide (Algorithm + Sim CameraId 값).
        /// 변경이 발생하면 true 반환 (호출자가 즉시 Save 하도록).
        /// </summary>
        public bool MigrateLegacyAlgorithmNames()
        {
            if (Items == null) return false;
            bool changed = false;
            foreach (var m in Items)
            {
                if (string.Equals(m.Algorithm, "TopSide", StringComparison.OrdinalIgnoreCase))
                {
                    m.Algorithm = VisionAlgorithm.FrontSide; changed = true;
                }
                else if (string.Equals(m.Algorithm, "BottomSide", StringComparison.OrdinalIgnoreCase))
                {
                    m.Algorithm = VisionAlgorithm.RearSide; changed = true;
                }
                // Sim fallback CameraId 만 변환 — 실 IP/실값은 건드리지 않음.
                if (m.CameraId == "Sim/TopSide")    { m.CameraId = "Sim/FrontSide"; changed = true; }
                if (m.CameraId == "Sim/BottomSide") { m.CameraId = "Sim/RearSide";  changed = true; }
            }
            // Migrate 후 동일 algorithm 이 중복되면 첫 항목만 유지.
            if (changed)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Items = Items.Where(m => seen.Add(m.Algorithm)).ToList();
            }
            return changed;
        }

        /// <summary>5 알고리즘 항목이 빠짐없이 존재하도록 보장 — 누락 항목은 Sim/* 로 채움.</summary>
        public void EnsureDefaults()
        {
            if (Items == null) Items = new List<AlgorithmCameraMapping>();
            foreach (var alg in VisionAlgorithm.All)
            {
                if (Items.Any(m => string.Equals(m.Algorithm, alg, StringComparison.OrdinalIgnoreCase)))
                    continue;
                string fallback;
                switch (alg)
                {
                    case VisionAlgorithm.Wafer:            fallback = "Sim/Wafer";       break;
                    case VisionAlgorithm.Bin:              fallback = "Sim/Bin";         break;
                    case VisionAlgorithm.BottomInspection: fallback = "Sim/BottomInsp";  break;
                    case VisionAlgorithm.FrontSide:        fallback = "Sim/FrontSide";   break;
                    case VisionAlgorithm.RearSide:         fallback = "Sim/RearSide";    break;
                    default:                               fallback = "Sim/0";           break;
                }
                Items.Add(new AlgorithmCameraMapping { Algorithm = alg, CameraId = fallback });
            }
        }

        public AlgorithmCameraSubset Clone()
        {
            var c = new AlgorithmCameraSubset();
            if (Items != null)
                foreach (var it in Items) c.Items.Add(it.Clone());
            return c;
        }

        // (C3b-2) Stage 70/81 구버전 조명 마이그(MigrateWiringPageToSettings/FillRecipeControllerPorts)는
        // InspectionLights 의존·참조 0(C3a 가 Form1 호출 제거)으로 제거 — 조명 SSOT 는 노드 Recipe.
    }
}
