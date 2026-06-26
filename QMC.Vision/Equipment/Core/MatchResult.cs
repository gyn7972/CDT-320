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
        // 검출된 박스(이미지 px) — 0 이면 미지정(소비측이 Train ROI 크기로 폴백).
        // 플랫 콜렛 파인더는 실제 검출 사각형 크기를 채워 콜렛 전용 오버레이로 그린다.
        public double BoxW     { get; set; }
        public double BoxH     { get; set; }
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

    /// <summary>개별 결함 1개(이미지 px) — 검출된 모든 결함을 영상 오버레이로 표시하는 데 사용.</summary>
    public class DefectMark
    {
        public double X;        // 중심 X(이미지 px)
        public double Y;        // 중심 Y(이미지 px)
        public double Width;    // 바운딩 박스 가로(px). 0 이면 면적으로 추정.
        public double Height;   // 바운딩 박스 세로(px)
        public double Area;     // 결함 면적(px^2)
        public string Type;     // 결함 종류("Chipping"/"Foreign"/"Size" 등) — 오버레이 NG(종류) 표기용
    }

    public class InspectionResult
    {
        public string RoiName { get; set; }
        public bool   IsPass  { get; set; }
        public List<InspectionItem> Items { get; set; } = new List<InspectionItem>();
        /// <summary>검출된 모든 결함(개별) — 핸들러/뷰어가 영상에 모두 오버레이.</summary>
        public List<DefectMark> Defects { get; set; } = new List<DefectMark>();
        public string ErrorMessage { get; set; }
    }
}
