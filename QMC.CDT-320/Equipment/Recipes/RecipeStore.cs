using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using QMC.Common.Data.Store;

namespace QMC.CDT320.Recipes
{
    /// <summary>
    /// 간단한 레시피(JSON) 영속화.
    /// 파일: <c>./Recipes/&lt;ProjectName&gt;.Project</c>
    /// </summary>
    public static class RecipeStore
    {
        public static string Dir { get; } =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes");

        static RecipeStore()
        {
            Directory.CreateDirectory(Dir);
            // 기본 프로젝트 몇 개 시드 — 다양한 다이 사이즈/웨이퍼 격자 (Stage 21)
            if (List().Count == 0)
            {
                // GM1SP-T150-G300 — 1mm 다이, 8inch 웨이퍼 (기본 시리즈)
                Save(new RecipeProject
                {
                    FileName     = "GM1SP-T150-G300",
                    MapFormat    = "SEC",
                    MapDirection = "표준(0도)",
                    ChipThickness       = 150,
                    MasterChipThickness = 150,
                    TapeThickness       = 100,
                    BinSortNumber       = 1,
                    MachineNumber       = "DS530",
                    CassetteFlow        = "Mapping",
                    LotId               = "Y482CB1",
                    PartId              = "N8SP03-F3X9-YC9RCA",
                    Die = new DieSubset
                    {
                        DieSpecName = "GM1SP-1mm",
                        WidthMm = 1.0, HeightMm = 1.0, ThicknessMm = 0.150,
                        ChipLowerSpecLimitWidth = -0.05, ChipUpperSpecLimitWidth = 0.05,
                        ChipLowerSpecLimitHeight = -0.05, ChipUpperSpecLimitHeight = 0.05,
                        ChippingDepthMax = 0.05, ChippingLengthMax = 0.20, ForeignSizeMax = 0.005,
                    },
                    Frame = new TapeFrameSubset
                    {
                        FrameSpecName = "8inch_50x50",
                        GridX = 50, GridY = 50, PitchX = 1.0, PitchY = 1.0,
                        OuterDiameterMm = 200, Rotate = "None",
                    }
                });

                // SMALL-DIE — 0.5mm 다이, 12inch
                Save(new RecipeProject
                {
                    FileName = "SMALL-DIE-0.5mm",
                    MapFormat = "SEC", ChipThickness = 80, TapeThickness = 80,
                    BinSortNumber = 1, MachineNumber = "DS530",
                    Die = new DieSubset
                    {
                        DieSpecName = "Small-0.5mm",
                        WidthMm = 0.5, HeightMm = 0.5, ThicknessMm = 0.08,
                        ChipLowerSpecLimitWidth = -0.02, ChipUpperSpecLimitWidth = 0.02,
                        ChipLowerSpecLimitHeight = -0.02, ChipUpperSpecLimitHeight = 0.02,
                        ChippingDepthMax = 0.02, ChippingLengthMax = 0.10, ForeignSizeMax = 0.002,
                    },
                    Frame = new TapeFrameSubset
                    {
                        FrameSpecName = "12inch_100x100",
                        GridX = 100, GridY = 100, PitchX = 0.5, PitchY = 0.5,
                        OuterDiameterMm = 300, Rotate = "None",
                    }
                });

                // LARGE-DIE — 3mm 다이, 8inch
                Save(new RecipeProject
                {
                    FileName = "LARGE-DIE-3mm",
                    MapFormat = "SEC", ChipThickness = 200, TapeThickness = 100,
                    BinSortNumber = 1, MachineNumber = "DS530",
                    Die = new DieSubset
                    {
                        DieSpecName = "Large-3mm",
                        WidthMm = 3.0, HeightMm = 3.0, ThicknessMm = 0.20,
                        ChipLowerSpecLimitWidth = -0.10, ChipUpperSpecLimitWidth = 0.10,
                        ChipLowerSpecLimitHeight = -0.10, ChipUpperSpecLimitHeight = 0.10,
                        ChippingDepthMax = 0.10, ChippingLengthMax = 0.40, ForeignSizeMax = 0.020,
                    },
                    Frame = new TapeFrameSubset
                    {
                        FrameSpecName = "8inch_15x15",
                        GridX = 15, GridY = 15, PitchX = 3.0, PitchY = 3.0,
                        OuterDiameterMm = 200, Rotate = "None",
                    }
                });

                // SAMPLE-DEMO — 5x5 (개발용)
                Save(new RecipeProject
                {
                    FileName = "SAMPLE-DEMO",
                    Die = new DieSubset { DieSpecName = "Default", WidthMm = 1.0, HeightMm = 1.0 },
                    Frame = new TapeFrameSubset { FrameSpecName = "Demo_5x5", GridX = 5, GridY = 5, PitchX = 1.0, PitchY = 1.0 },
                });
            }
        }

        /// <summary>디렉토리의 *.Project 파일 이름 목록.</summary>
        public static List<string> List()
        {
            var r = new List<string>();
            try
            {
                foreach (var f in Directory.GetFiles(Dir, "*.Project"))
                    r.Add(Path.GetFileName(f));
                r.Sort();
            }
            catch { }
            return r;
        }

        public static RecipeProject Load(string fileName)
        {
            if (!fileName.EndsWith(".Project", StringComparison.OrdinalIgnoreCase))
                fileName += ".Project";
            var path = Path.Combine(Dir, fileName);
            if (!File.Exists(path)) return null;
            try
            {
                using (var fs = File.OpenRead(path))
                {
                    var ser = new DataContractJsonSerializer(typeof(RecipeProject));
                    return (RecipeProject)ser.ReadObject(fs);
                }
            }
            catch { return null; }
        }

        public static void Save(RecipeProject p)
        {
            if (p == null || string.IsNullOrEmpty(p.FileName)) return;
            var name = p.FileName + ".Project";
            var path = Path.Combine(Dir, name);
            try
            {
                using (var fs = File.Create(path))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(RecipeProject), p);
                }
            }
            catch { }
        }

        public static bool Delete(string fileName)
        {
            if (!fileName.EndsWith(".Project", StringComparison.OrdinalIgnoreCase))
                fileName += ".Project";
            var path = Path.Combine(Dir, fileName);
            if (!File.Exists(path)) return false;
            try { File.Delete(path); return true; }
            catch { return false; }
        }

        // ──────────────────────────────────────────
        //  Last loaded project 영속화 — 재시작 시 자동 로드용
        // ──────────────────────────────────────────

        private static string LastProjectMarkerPath
            => Path.Combine(Dir, ".last_project");

        /// <summary>현재 로드된 프로젝트를 "마지막 프로젝트"로 기록 (다음 재시작 시 자동 로드).</summary>
        public static void SaveLastProjectName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            if (fileName.EndsWith(".Project", StringComparison.OrdinalIgnoreCase))
                fileName = fileName.Substring(0, fileName.Length - ".Project".Length);
            try { File.WriteAllText(LastProjectMarkerPath, fileName, Encoding.UTF8); }
            catch { }
        }

        /// <summary>마지막 로드된 프로젝트 파일명 (확장자 제외). 없으면 null.</summary>
        public static string GetLastProjectName()
        {
            try
            {
                if (!File.Exists(LastProjectMarkerPath)) return null;
                var name = File.ReadAllText(LastProjectMarkerPath, Encoding.UTF8).Trim();
                if (string.IsNullOrEmpty(name)) return null;
                if (Load(name) == null) return null;   // 파일 사라졌으면 null
                return name;
            }
            catch { return null; }
        }

        /// <summary>마지막 로드된 프로젝트를 자동 로드. 없으면 첫 번째 *.Project 파일을 fallback.</summary>
        public static RecipeProject LoadLastOrDefault()
        {
            var name = GetLastProjectName();
            if (name != null) return Load(name);
            var list = List();
            if (list.Count == 0) return null;
            return Load(list[0]);
        }
    }

    /// <summary>1개의 프로젝트(레시피) — 300의 PROJECT 페이지에 대응.</summary>
    [DataContract]
    public class RecipeProject
    {
        [DataMember] public string FileName           { get; set; } = "NEW";
        [DataMember] public string MachineNumber      { get; set; } = "DS530";
        [DataMember] public string CassetteFlow       { get; set; } = "Mapping";
        [DataMember] public bool   DryRun             { get; set; } = false;
        [DataMember] public bool   StepRun            { get; set; } = false;
        [DataMember] public bool   XmlSave            { get; set; } = true;
        [DataMember] public bool   ReDt               { get; set; } = false;
        [DataMember] public bool   EbrMode            { get; set; } = true;
        [DataMember] public bool   AlignConfirmEnable { get; set; } = true;
        [DataMember] public bool   NeedleCheckMode    { get; set; } = false;
        [DataMember] public double AutoPositionDeviationLimit { get; set; } = 50.0;

        [DataMember] public string MapFormat          { get; set; } = "SEC";
        [DataMember] public string MapDirection       { get; set; } = "표준(0도)";
        [DataMember] public double ChipThickness      { get; set; } = 150;
        [DataMember] public double MasterChipThickness{ get; set; } = 150;
        [DataMember] public double TapeThickness      { get; set; } = 100;
        [DataMember] public int    BinSortNumber      { get; set; } = 1;

        [DataMember] public string LotId              { get; set; }
        [DataMember] public string PartId             { get; set; }
        [DataMember] public string InputCassetteId    { get; set; }
        [DataMember] public string OutputCassetteId   { get; set; }
        [DataMember] public int    InputCassetteLevelCount { get; set; } = 1;
        [DataMember] public int    GoodCassetteLevelCount  { get; set; } = 1;
        [DataMember] public string ColletModelNum     { get; set; }
        [DataMember] public string ColletLotNum       { get; set; }
        [DataMember] public string XmlPath            { get; set; }

        // ── 310 Union Recipe 이식 — SubsetRecipe 4 종 ──
        [DataMember] public DieSubset             Die           { get; set; } = new DieSubset();
        [DataMember] public TapeFrameSubset       Frame         { get; set; } = new TapeFrameSubset();
        [DataMember] public LoadTapeFrameSubset   LoadFrame     { get; set; } = new LoadTapeFrameSubset();
        [DataMember] public UnloadTapeFrameSubset UnloadFrame   { get; set; } = new UnloadTapeFrameSubset();
        [DataMember] public ModuleSubset          Module        { get; set; } = new ModuleSubset();
        // Stage 51 — Vision Inspection Subsets (Top/Bottom/Side)
        [DataMember] public InspectionSubset      BottomInsp    { get; set; } = new InspectionSubset();
        [DataMember] public InspectionSubset      TopSideInsp   { get; set; } = new InspectionSubset();
        [DataMember] public InspectionSubset      BottomSideInsp { get; set; } = new InspectionSubset();
        // Stage 54 — Output Subset (NG/Good Plate 사양)
        [DataMember] public OutputSubset          Output        { get; set; } = new OutputSubset();
        // Stage 61 — Pickup Sequence Subset (시작 코너 + 방향 + 지그재그/직선)
        [DataMember] public PickupSubset          Pickup        { get; set; } = new PickupSubset();
    }

    // ─── Stage 61 — Pickup Sequence 옵션 enums ──────────────────────
    public enum PickupStartCorner { TopLeft = 0, BottomLeft = 1, TopRight = 2, BottomRight = 3 }
    public enum PickupDirection   { Horizontal = 0, Vertical = 1 }
    public enum PickupPattern     { Straight = 0, ZigZag = 1 }

    /// <summary>웨이퍼 다이 픽업 순서 옵션 (시작 코너 + 가로/세로 + 지그재그/직선).</summary>
    [DataContract]
    public class PickupSubset
    {
        [DataMember] public PickupStartCorner StartCorner { get; set; } = PickupStartCorner.TopLeft;
        [DataMember] public PickupDirection   Direction   { get; set; } = PickupDirection.Horizontal;
        [DataMember] public PickupPattern     Pattern     { get; set; } = PickupPattern.Straight;
    }

    /// <summary>Stage 54 — Output Plate / Cassette 사양 Subset.</summary>
    [DataContract]
    public class OutputSubset
    {
        [DataMember] public int    GoodPlateMaxSlots   { get; set; } = 25;
        [DataMember] public int    NgPlateMaxSlots     { get; set; } = 25;
        /// <summary>한 웨이퍼당 가공할 다이 수. 300mm 원형 wafer 기준 1400.</summary>
        [DataMember] public int    DiesPerWafer         { get; set; } = 1400;
        /// <summary>Output 카세트 적재 트리거 주기 [다이 수].
        /// 0 또는 음수 = OutputDieMap 슬롯 수에 자동 맞춤 (1444). 양수 = 명시값.</summary>
        [DataMember] public int    WafersPerOutputBatch { get; set; } = 0;
        [DataMember] public bool   AutoBinTransition    { get; set; } = true;
        [DataMember] public bool   AlarmOnFull          { get; set; } = true;
        [DataMember] public string DefaultGoodCassette  { get; set; } = "Good1";
    }

    /// <summary>Stage 51 — Vision Inspection 공통 파라미터 Subset.</summary>
    [DataContract]
    public class InspectionSubset
    {
        [DataMember] public bool   Enable                { get; set; } = true;
        [DataMember] public int    ExposureMs            { get; set; } = 500;
        [DataMember] public double LightIntensity        { get; set; } = 0.5;
        [DataMember] public double ChippingDepthMaxMm    { get; set; } = 0.05;
        [DataMember] public double ChippingLengthMaxMm   { get; set; } = 0.10;
        [DataMember] public double ScratchAreaMaxMm2     { get; set; } = 0.005;
        [DataMember] public double ContaminationMaxMm2   { get; set; } = 0.010;
        [DataMember] public double MinDieCenterScore     { get; set; } = 0.7;
    }

    // ── Subset Recipes ────────────────────────────────────
    /// <summary>다이 사양 Subset (310 의 DieSubsetRecipe).</summary>
    [DataContract]
    public class DieSubset
    {
        [DataMember] public string DieSpecName { get; set; } = "Default";
        [DataMember] public double WidthMm     { get; set; } = 1.0;
        [DataMember] public double HeightMm    { get; set; } = 1.0;
        [DataMember] public double ThicknessMm { get; set; } = 0.1;
        // 검사 임계값 (Vision)
        [DataMember] public double ChipLowerSpecLimitWidth  { get; set; } = -0.05;
        [DataMember] public double ChipUpperSpecLimitWidth  { get; set; } = 0.05;
        [DataMember] public double ChipLowerSpecLimitHeight { get; set; } = -0.05;
        [DataMember] public double ChipUpperSpecLimitHeight { get; set; } = 0.05;
        [DataMember] public double ChippingDepthMax         { get; set; } = 0.05;
        [DataMember] public double ChippingLengthMax        { get; set; } = 0.20;
        [DataMember] public double ForeignSizeMax           { get; set; } = 0.005;
    }

    /// <summary>웨이퍼(Tape Frame) 사양 Subset (310 의 DieTapeFrameSubsetRecipe).</summary>
    [DataContract]
    public class TapeFrameSubset
    {
        [DataMember] public string FrameSpecName { get; set; } = "8inch_5x5";
        [DataMember] public int    GridX  { get; set; } = 5;
        [DataMember] public int    GridY  { get; set; } = 5;
        [DataMember] public double PitchX { get; set; } = 1.0;
        [DataMember] public double PitchY { get; set; } = 1.0;
        [DataMember] public string Rotate { get; set; } = "None";
        [DataMember] public double OuterDiameterMm { get; set; } = 200;
    }

    /// <summary>로드 웨이퍼 (310 의 LoadDieTapeFrameSubsetRecipe).</summary>
    [DataContract]
    public class LoadTapeFrameSubset
    {
        [DataMember] public string Role             { get; set; } = "Load";
        [DataMember] public bool   AutoBarcodeRead  { get; set; } = true;
        [DataMember] public bool   AutoAlignment    { get; set; } = true;
        [DataMember] public int    AlignmentPoints  { get; set; } = 3;
    }

    /// <summary>언로드 웨이퍼 (310 의 UnloadDieTapeFrameSubsetRecipe).</summary>
    [DataContract]
    public class UnloadTapeFrameSubset
    {
        [DataMember] public string Role            { get; set; } = "GoodUnload";
        [DataMember] public bool   GapInspection   { get; set; } = true;
        [DataMember] public double GapUpperLimit   { get; set; } = 0.05;
        [DataMember] public double GapLowerLimit   { get; set; } = 0.005;
    }

    /// <summary>모듈 동작 파라미터 (310 의 PickAndPlaceDieTransferModuleSubsetRecipe).</summary>
    [DataContract]
    public class ModuleSubset
    {
        [DataMember] public int    PickRetryCount      { get; set; } = 3;
        [DataMember] public int    PickDelayMs         { get; set; } = 80;
        [DataMember] public int    PlaceDelayMs        { get; set; } = 60;
        [DataMember] public bool   ColletCleanEnable   { get; set; } = true;
        [DataMember] public int    ColletCleanInterval { get; set; } = 1000; // 다이 N개마다
        [DataMember] public bool   BottomInspectionEnable { get; set; } = true;
        [DataMember] public bool   PlacementInspectionEnable { get; set; } = true;
    }
}
