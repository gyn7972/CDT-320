using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Vision.Core.Parameters;

namespace QMC.Vision.Core.Inspectors
{
    /// <summary>다이 색상 (검사 임계값 자동 결정용).  310 의 ChipType.</summary>
    public enum ChipType { White, Black }

    /// <summary>왜곡 보정 타깃 형상.</summary>
    public enum DistortionTargetSearch { CrossLine, Circle }

    /// <summary>측면 검사 부위 (다이 4면).</summary>
    public enum SideSurface { FrontWidth, BackWidth, FrontHeight, BackHeight }

    // ─────────────────────────────────────────────
    //  파라미터 베이스
    // ─────────────────────────────────────────────
    [DataContract]
    public abstract class InspectionParametersBase : IParameterProvider
    {
        [DataMember] public string Name { get; set; } = "";

        // P1 — SSOT 디스크립터. 인스펙터 연결(편집→실검사 반영)은 P3. 현재는 describe 만(orphan 유지).
        public abstract string ParameterTarget { get; }
        public abstract IEnumerable<ParameterDescriptor> DescribeParameters();

        public void SaveJson(string path)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                using (var fs = File.Create(path))
                {
                    var ser = new DataContractJsonSerializer(GetType());
                    ser.WriteObject(fs, this);
                }
            }
            catch { }
        }

        protected static T LoadJsonInternal<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;
            try
            {
                using (var fs = File.OpenRead(path))
                {
                    var ser = new DataContractJsonSerializer(typeof(T));
                    return (T)ser.ReadObject(fs);
                }
            }
            catch { return null; }
        }
    }

    // ─────────────────────────────────────────────
    //  Bottom Inspection
    // ─────────────────────────────────────────────
    [DataContract]
    public class BottomInspectionParameters : InspectionParametersBase
    {
        [DataMember] public ChipType ChipType                  { get; set; } = ChipType.Black;
        [DataMember] public int      Threshold                 { get; set; } = 128;
        [DataMember] public int      WidthMinimumColorGap      { get; set; } = 10;
        [DataMember] public int      WidthMaximumColorGap      { get; set; } = 200;
        [DataMember] public int      HeightMinimumColorGap     { get; set; } = 10;
        [DataMember] public int      HeightMaximumColorGap     { get; set; } = 200;
        [DataMember] public int      ChippingSize1MaximumColorGap { get; set; } = 50;
        [DataMember] public int      ChippingSize2MaximumColorGap { get; set; } = 80;
        [DataMember] public int      TopHatRadius              { get; set; } = 7;
        [DataMember] public int      MinForeignAreaFilterSize  { get; set; } = 3;
        [DataMember] public int      LinkDistance              { get; set; } = 5;

        [DataMember] public double   ChippingDepth             { get; set; } = 0.05;
        [DataMember] public double   ChippingLength            { get; set; } = 0.20;
        [DataMember] public double   ForeignObjectSize         { get; set; } = 0.005;
        [DataMember] public double   FirstPeekValueThreshold   { get; set; } = 0.30;
        [DataMember] public double   PeekValueThreshold        { get; set; } = 0.20;
        [DataMember] public double   Stdev                     { get; set; } = 1.5;
        [DataMember] public double   PortentialDefactMinSize   { get; set; } = 0.001;

        [DataMember] public float    ChipLowerSpecLimitWidth   { get; set; } = 0;
        [DataMember] public float    ChipUpperSpecLimitWidth   { get; set; } = 0;
        [DataMember] public float    ChipLowerSpecLimitHeight  { get; set; } = 0;
        [DataMember] public float    ChipUpperSpecLimitHeight  { get; set; } = 0;

        [DataMember] public bool     UseContaminationInspection{ get; set; } = false;
        [DataMember] public string   FileSavePath              { get; set; } = "";

        public static BottomInspectionParameters LoadJson(string path)
            => LoadJsonInternal<BottomInspectionParameters>(path) ?? new BottomInspectionParameters();

        // P1 — #9 매핑 확정 / #3 ChipSpecLimit=Recipe, FileSavePath=Setup
        public override string ParameterTarget => "BottomInspection/SurfaceInspector";
        public override IEnumerable<ParameterDescriptor> DescribeParameters()
        {
            string t = ParameterTarget;
            yield return ParameterDescriptor.Enum(t, "ChipType", "Chip Type", ParameterLayer.Recipe, () => ChipType, v => ChipType = v);
            yield return ParameterDescriptor.Int(t, "Threshold", "Threshold", "", ParameterLayer.Recipe, () => Threshold, v => Threshold = v, 0, 255);
            yield return ParameterDescriptor.Int(t, "WidthMinimumColorGap", "Width Min Color Gap", "", ParameterLayer.Recipe, () => WidthMinimumColorGap, v => WidthMinimumColorGap = v);
            yield return ParameterDescriptor.Int(t, "WidthMaximumColorGap", "Width Max Color Gap", "", ParameterLayer.Recipe, () => WidthMaximumColorGap, v => WidthMaximumColorGap = v);
            yield return ParameterDescriptor.Int(t, "HeightMinimumColorGap", "Height Min Color Gap", "", ParameterLayer.Recipe, () => HeightMinimumColorGap, v => HeightMinimumColorGap = v);
            yield return ParameterDescriptor.Int(t, "HeightMaximumColorGap", "Height Max Color Gap", "", ParameterLayer.Recipe, () => HeightMaximumColorGap, v => HeightMaximumColorGap = v);
            yield return ParameterDescriptor.Int(t, "ChippingSize1MaximumColorGap", "Chipping1 Max Color Gap", "", ParameterLayer.Recipe, () => ChippingSize1MaximumColorGap, v => ChippingSize1MaximumColorGap = v);
            yield return ParameterDescriptor.Int(t, "ChippingSize2MaximumColorGap", "Chipping2 Max Color Gap", "", ParameterLayer.Recipe, () => ChippingSize2MaximumColorGap, v => ChippingSize2MaximumColorGap = v);
            yield return ParameterDescriptor.Int(t, "TopHatRadius", "TopHat Radius", "px", ParameterLayer.Recipe, () => TopHatRadius, v => TopHatRadius = v);
            yield return ParameterDescriptor.Int(t, "MinForeignAreaFilterSize", "Min Foreign Area Filter", "px", ParameterLayer.Recipe, () => MinForeignAreaFilterSize, v => MinForeignAreaFilterSize = v);
            yield return ParameterDescriptor.Int(t, "LinkDistance", "Link Distance", "px", ParameterLayer.Recipe, () => LinkDistance, v => LinkDistance = v);
            yield return ParameterDescriptor.Double(t, "ChippingDepth", "Chipping Depth", "mm", ParameterLayer.Recipe, () => ChippingDepth, v => ChippingDepth = v);
            yield return ParameterDescriptor.Double(t, "ChippingLength", "Chipping Length", "mm", ParameterLayer.Recipe, () => ChippingLength, v => ChippingLength = v);
            yield return ParameterDescriptor.Double(t, "ForeignObjectSize", "Foreign Object Size", "mm²", ParameterLayer.Recipe, () => ForeignObjectSize, v => ForeignObjectSize = v);
            yield return ParameterDescriptor.Double(t, "FirstPeekValueThreshold", "First Peek Threshold", "", ParameterLayer.Recipe, () => FirstPeekValueThreshold, v => FirstPeekValueThreshold = v);
            yield return ParameterDescriptor.Double(t, "PeekValueThreshold", "Peek Threshold", "", ParameterLayer.Recipe, () => PeekValueThreshold, v => PeekValueThreshold = v);
            yield return ParameterDescriptor.Double(t, "Stdev", "Std Dev", "", ParameterLayer.Recipe, () => Stdev, v => Stdev = v);
            yield return ParameterDescriptor.Double(t, "PortentialDefactMinSize", "Potential Defect Min Size", "mm²", ParameterLayer.Recipe, () => PortentialDefactMinSize, v => PortentialDefactMinSize = v);
            yield return ParameterDescriptor.Double(t, "ChipLowerSpecLimitWidth", "Chip Lower Spec Width", "mm", ParameterLayer.Recipe, () => ChipLowerSpecLimitWidth, v => ChipLowerSpecLimitWidth = (float)v);
            yield return ParameterDescriptor.Double(t, "ChipUpperSpecLimitWidth", "Chip Upper Spec Width", "mm", ParameterLayer.Recipe, () => ChipUpperSpecLimitWidth, v => ChipUpperSpecLimitWidth = (float)v);
            yield return ParameterDescriptor.Double(t, "ChipLowerSpecLimitHeight", "Chip Lower Spec Height", "mm", ParameterLayer.Recipe, () => ChipLowerSpecLimitHeight, v => ChipLowerSpecLimitHeight = (float)v);
            yield return ParameterDescriptor.Double(t, "ChipUpperSpecLimitHeight", "Chip Upper Spec Height", "mm", ParameterLayer.Recipe, () => ChipUpperSpecLimitHeight, v => ChipUpperSpecLimitHeight = (float)v);
            yield return ParameterDescriptor.Bool(t, "UseContaminationInspection", "Use Contamination Inspection", ParameterLayer.Recipe, () => UseContaminationInspection, v => UseContaminationInspection = v);
            yield return ParameterDescriptor.Text(t, "FileSavePath", "File Save Path", ParameterLayer.Setup, () => FileSavePath, v => FileSavePath = v);
        }
    }

    // ─────────────────────────────────────────────
    //  Side Inspection
    // ─────────────────────────────────────────────
    [DataContract]
    public class SideInspectionParameters : InspectionParametersBase
    {
        [DataMember] public ChipType    ChipType        { get; set; } = ChipType.Black;
        [DataMember] public SideSurface Surface         { get; set; } = SideSurface.FrontWidth;
        [DataMember] public int         Threshold       { get; set; } = 128;

        [DataMember] public double      ChippingDepth   { get; set; } = 0.05;
        [DataMember] public double      ChippingLength  { get; set; } = 0.20;
        [DataMember] public double      ForeignObjectSize { get; set; } = 0.005;
        [DataMember] public double      BladeWidth      { get; set; } = 0.04;
        [DataMember] public double      ChipThickness   { get; set; } = 0.10;

        [DataMember] public float       ChipLowerSpecLimitWidth  { get; set; } = 0;
        [DataMember] public float       ChipUpperSpecLimitWidth  { get; set; } = 0;
        [DataMember] public float       ChipLowerSpecLimitHeight { get; set; } = 0;
        [DataMember] public float       ChipUpperSpecLimitHeight { get; set; } = 0;

        public static SideInspectionParameters LoadJson(string path)
            => LoadJsonInternal<SideInspectionParameters>(path) ?? new SideInspectionParameters();

        // P1 — #9 [확인 필요]: Surface(4면)↔인스턴스 매핑 불명 → 잠정 target, **인스펙터 미연결(orphan)**. P3 확정 시 바인딩.
        public override string ParameterTarget => "SideInspection/?";
        public override IEnumerable<ParameterDescriptor> DescribeParameters()
        {
            string t = ParameterTarget;
            yield return ParameterDescriptor.Enum(t, "ChipType", "Chip Type", ParameterLayer.Recipe, () => ChipType, v => ChipType = v);
            yield return ParameterDescriptor.Enum(t, "Surface", "Surface", ParameterLayer.Recipe, () => Surface, v => Surface = v);
            yield return ParameterDescriptor.Int(t, "Threshold", "Threshold", "", ParameterLayer.Recipe, () => Threshold, v => Threshold = v, 0, 255);
            yield return ParameterDescriptor.Double(t, "ChippingDepth", "Chipping Depth", "mm", ParameterLayer.Recipe, () => ChippingDepth, v => ChippingDepth = v);
            yield return ParameterDescriptor.Double(t, "ChippingLength", "Chipping Length", "mm", ParameterLayer.Recipe, () => ChippingLength, v => ChippingLength = v);
            yield return ParameterDescriptor.Double(t, "ForeignObjectSize", "Foreign Object Size", "mm²", ParameterLayer.Recipe, () => ForeignObjectSize, v => ForeignObjectSize = v);
            yield return ParameterDescriptor.Double(t, "BladeWidth", "Blade Width", "mm", ParameterLayer.Recipe, () => BladeWidth, v => BladeWidth = v);
            yield return ParameterDescriptor.Double(t, "ChipThickness", "Chip Thickness", "mm", ParameterLayer.Recipe, () => ChipThickness, v => ChipThickness = v);
            yield return ParameterDescriptor.Double(t, "ChipLowerSpecLimitWidth", "Chip Lower Spec Width", "mm", ParameterLayer.Recipe, () => ChipLowerSpecLimitWidth, v => ChipLowerSpecLimitWidth = (float)v);
            yield return ParameterDescriptor.Double(t, "ChipUpperSpecLimitWidth", "Chip Upper Spec Width", "mm", ParameterLayer.Recipe, () => ChipUpperSpecLimitWidth, v => ChipUpperSpecLimitWidth = (float)v);
            yield return ParameterDescriptor.Double(t, "ChipLowerSpecLimitHeight", "Chip Lower Spec Height", "mm", ParameterLayer.Recipe, () => ChipLowerSpecLimitHeight, v => ChipLowerSpecLimitHeight = (float)v);
            yield return ParameterDescriptor.Double(t, "ChipUpperSpecLimitHeight", "Chip Upper Spec Height", "mm", ParameterLayer.Recipe, () => ChipUpperSpecLimitHeight, v => ChipUpperSpecLimitHeight = (float)v);
        }
    }

    // ─────────────────────────────────────────────
    //  Die Gap Inspection
    // ─────────────────────────────────────────────
    [DataContract]
    public class DieGapInspectionParameters : InspectionParametersBase
    {
        [DataMember] public int    Threshold  { get; set; } = 128;
        [DataMember] public double UpperLimit { get; set; } = 0.05;
        [DataMember] public double LowerLimit { get; set; } = 0.005;

        public static DieGapInspectionParameters LoadJson(string path)
            => LoadJsonInternal<DieGapInspectionParameters>(path) ?? new DieGapInspectionParameters();

        // P1 — #9 [확인 필요]: 대응 인스펙터 없음 → 잠정 target, **orphan**. P3 실사용처 확정 시 바인딩.
        public override string ParameterTarget => "DieGapInspection/?";
        public override IEnumerable<ParameterDescriptor> DescribeParameters()
        {
            string t = ParameterTarget;
            yield return ParameterDescriptor.Int(t, "Threshold", "Threshold", "", ParameterLayer.Recipe, () => Threshold, v => Threshold = v, 0, 255);
            yield return ParameterDescriptor.Double(t, "UpperLimit", "Upper Limit", "mm", ParameterLayer.Recipe, () => UpperLimit, v => UpperLimit = v);
            yield return ParameterDescriptor.Double(t, "LowerLimit", "Lower Limit", "mm", ParameterLayer.Recipe, () => LowerLimit, v => LowerLimit = v);
        }
    }

    // ─────────────────────────────────────────────
    //  Distortion Compensation
    // ─────────────────────────────────────────────
    [DataContract]
    public class DistortionParameters : InspectionParametersBase
    {
        [DataMember] public ChipType                ChipType     { get; set; } = ChipType.Black;
        [DataMember] public DistortionTargetSearch  TargetSearch { get; set; } = DistortionTargetSearch.CrossLine;
        [DataMember] public int                     Threshold    { get; set; } = 128;
        [DataMember] public double                  PitchX       { get; set; } = 1.0;
        [DataMember] public double                  PitchY       { get; set; } = 1.0;

        public static DistortionParameters LoadJson(string path)
            => LoadJsonInternal<DistortionParameters>(path) ?? new DistortionParameters();

        // P1 — #9 매핑 확정 / #4 Pitch=Recipe
        public override string ParameterTarget => "BottomInspection/DistortionCompensation";
        public override IEnumerable<ParameterDescriptor> DescribeParameters()
        {
            string t = ParameterTarget;
            yield return ParameterDescriptor.Enum(t, "ChipType", "Chip Type", ParameterLayer.Recipe, () => ChipType, v => ChipType = v);
            yield return ParameterDescriptor.Enum(t, "TargetSearch", "Target Search", ParameterLayer.Recipe, () => TargetSearch, v => TargetSearch = v);
            yield return ParameterDescriptor.Int(t, "Threshold", "Threshold", "", ParameterLayer.Recipe, () => Threshold, v => Threshold = v, 0, 255);
            yield return ParameterDescriptor.Double(t, "PitchX", "Pitch X", "mm", ParameterLayer.Recipe, () => PitchX, v => PitchX = v);
            yield return ParameterDescriptor.Double(t, "PitchY", "Pitch Y", "mm", ParameterLayer.Recipe, () => PitchY, v => PitchY = v);
        }
    }

    // ─────────────────────────────────────────────
    //  Vision Scale Calibration
    // ─────────────────────────────────────────────
    [DataContract]
    public class VisionScaleParameters : InspectionParametersBase
    {
        [DataMember] public ChipType ChipType   { get; set; } = ChipType.Black;
        [DataMember] public int      Threshold  { get; set; } = 128;
        [DataMember] public double   ChipWidth  { get; set; } = 1.0;   // mm
        [DataMember] public double   ChipHeight { get; set; } = 1.0;   // mm

        public static VisionScaleParameters LoadJson(string path)
            => LoadJsonInternal<VisionScaleParameters>(path) ?? new VisionScaleParameters();

        // P1 — #9 [확인-ish]: 잠정 target. P3 에서 <module>/ScaleFinder 바인딩(보정 산출 ScaleX/Y 는 #5 Setup=VisionSettings).
        public override string ParameterTarget => "VisionScale";
        public override IEnumerable<ParameterDescriptor> DescribeParameters()
        {
            string t = ParameterTarget;
            yield return ParameterDescriptor.Enum(t, "ChipType", "Chip Type", ParameterLayer.Recipe, () => ChipType, v => ChipType = v);
            yield return ParameterDescriptor.Int(t, "Threshold", "Threshold", "", ParameterLayer.Recipe, () => Threshold, v => Threshold = v, 0, 255);
            yield return ParameterDescriptor.Double(t, "ChipWidth", "Chip Width", "mm", ParameterLayer.Recipe, () => ChipWidth, v => ChipWidth = v);
            yield return ParameterDescriptor.Double(t, "ChipHeight", "Chip Height", "mm", ParameterLayer.Recipe, () => ChipHeight, v => ChipHeight = v);
        }
    }
}
