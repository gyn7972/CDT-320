using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

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
    public abstract class InspectionParametersBase
    {
        [DataMember] public string Name { get; set; } = "";

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
    }
}
