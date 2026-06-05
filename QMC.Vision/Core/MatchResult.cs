using System.Collections.Generic;

namespace QMC.Vision.Core
{
    /// <summary>Finder/Matcher 결과 1건.</summary>
    public class MatchInstance
    {
        public int    Index    { get; set; }
        public double CenterX  { get; set; }
        public double CenterY  { get; set; }
        public double AngleDeg { get; set; }     // R
        public double Score    { get; set; }     // 0.0 ~ 1.0
        public double Scale    { get; set; } = 1.0;
    }

    public class MatchResult
    {
        public string RoiName     { get; set; }
        public bool   Success     { get; set; }
        public string ErrorMessage{ get; set; }
        public List<MatchInstance> Instances { get; set; } = new List<MatchInstance>();

        public MatchInstance Best
        {
            get
            {
                MatchInstance best = null;
                foreach (var m in Instances)
                    if (best == null || m.Score > best.Score) best = m;
                return best;
            }
        }

        public static MatchResult Fail(string roi, string msg)
            => new MatchResult { RoiName = roi, Success = false, ErrorMessage = msg };
    }

    /// <summary>Inspector 결과 1건 (외관검사/배치검사).</summary>
    public class InspectionItem
    {
        public string Name   { get; set; }
        public string Value  { get; set; }
        public bool   IsPass { get; set; }
    }

    public class InspectionResult
    {
        public string RoiName { get; set; }
        public bool   IsPass  { get; set; }
        public List<InspectionItem> Items { get; set; } = new List<InspectionItem>();
        public string ErrorMessage { get; set; }
    }
}
