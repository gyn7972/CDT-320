using System.Collections.Generic;
using System.Drawing;

namespace QMC.Vision.Core
{
    /// <summary>에지 검출 + 거리 측정 (Cognex CogCaliperTool 추상화).</summary>
    public interface IEdgeFinder
    {
        string Id { get; }
        Roi MeasureRoi { get; set; }
        double EdgeThreshold { get; set; }
        EdgeMeasurement Measure(Bitmap image);
    }

    public class EdgeMeasurement
    {
        public bool Success { get; set; }
        public double WidthPixels { get; set; }
        public double HeightPixels { get; set; }
        public double EdgeStrength { get; set; }
        public List<Point> EdgePoints { get; set; } = new List<Point>();
        public string ErrorMessage { get; set; }
    }

    /// <summary>밝기 히스토그램 통계 (Cognex CogHistogramTool 추상화).</summary>
    public interface IHistogramAnalyzer
    {
        string Id { get; }
        Roi AnalysisRoi { get; set; }
        HistogramResult Analyze(Bitmap image);
    }

    public class HistogramResult
    {
        public bool Success { get; set; }
        public double Mean { get; set; }
        public double Stdev { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        public int[] Distribution { get; set; } = new int[256];
        public string ErrorMessage { get; set; }
    }

    /// <summary>컬러 매칭 (Cognex CogColorMatchTool 추상화).</summary>
    public interface IColorMatcher
    {
        string Id { get; }
        Color TargetColor { get; set; }
        int Tolerance { get; set; }
        ColorMatchResult Match(Bitmap image);
    }

    public class ColorMatchResult
    {
        public bool Success { get; set; }
        public double MatchPercent { get; set; }
        public int MatchedPixels { get; set; }
        public int TotalPixels { get; set; }
        public string ErrorMessage { get; set; }
    }
}
