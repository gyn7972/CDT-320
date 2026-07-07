using System.Runtime.Serialization;

namespace QMC.Vision.Modules
{
    // ──────────────────────────────────────────────────────────────
    //  Cognex(코그넥스) 전용 패턴매칭 파라미터 그룹
    //  공용 Finder 파라미터(FinderAlgoRecipe)와 분리해 별도 관리한다.
    //  FinderAlgoRecipe.Cognex 로 보관되고, 백엔드가 Cognex 일 때만
    //  CognexPatternFinder(IAlgoParamSync)가 소비한다(OpenCv/Sim 은 무시).
    //  레시피 UI 에서는 Expert(Engineer 이상) 권한일 때만 노출한다.
    //  0/미설정 값 = "공용 파라미터/기본값 사용"으로 해석해 이중 구동을 피한다.
    // ──────────────────────────────────────────────────────────────

    /// <summary>Cognex CogPMAlignTool 패턴매칭 알고리즘 종류.</summary>
    public enum CognexPatternAlgorithm
    {
        /// <summary>PatMax — 정밀 기하 매칭(기본).</summary>
        PatMax = 0,
        /// <summary>PatQuick — 고속 매칭.</summary>
        PatQuick = 1,
        /// <summary>PatFlex — 유연(변형 허용) 매칭.</summary>
        PatFlex = 2,
    }

    /// <summary>Cognex 전용 고급 패턴매칭 파라미터 그룹(Expert 노출·별도 관리).</summary>
    [DataContract]
    public sealed class CognexPatternParams
    {
        /// <summary>패턴매칭 알고리즘(PatMax/PatQuick/PatFlex). 학습(Train) 시 pattern.Algorithm 에 반영.</summary>
        [DataMember] public CognexPatternAlgorithm Algorithm { get; set; }

        /// <summary>최소 허용 score(0~1). 0 이하 = 공용 AcceptThreshold 사용.</summary>
        [DataMember] public double AcceptThreshold { get; set; }

        /// <summary>대비 임계값(RunParams.ContrastThreshold). 0 이하 = 미설정(엔진 기본).</summary>
        [DataMember] public double ContrastThreshold { get; set; }

        /// <summary>찾을 대략 개수(ApproximateNumberToFind). 0 이하 = 공용 MaxInstances 사용.</summary>
        [DataMember] public int ApproxNumToFind { get; set; }

        /// <summary>회전 탐색 시작각(deg). AngleExtentDeg 가 0 이하이면 공용 Angle 설정 사용.</summary>
        [DataMember] public double AngleStartDeg { get; set; }

        /// <summary>회전 탐색 범위(deg). 0 이하 = 공용 Angle 설정 사용.</summary>
        [DataMember] public double AngleExtentDeg { get; set; }

        /// <summary>스케일 탐색 시작 배율(RunParams.ScaleStart). 기본 1.0.</summary>
        [DataMember] public double ScaleStart { get; set; }

        /// <summary>스케일 탐색 범위(RunParams.ScaleExtent). 0 이하 = 스케일 미탐색.</summary>
        [DataMember] public double ScaleExtent { get; set; }

        /// <summary>극성 무시(RunParams.IgnorePolarity) — 밝기 반전 패턴도 매칭.</summary>
        [DataMember] public bool IgnorePolarity { get; set; }

        /// <summary>실행 타임아웃(ms). 0 이하 = 미설정(엔진 기본).</summary>
        [DataMember] public double TimeoutMs { get; set; }

        public CognexPatternParams() { SetDefaults(); }
        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();

        private void SetDefaults()
        {
            Algorithm = CognexPatternAlgorithm.PatMax;
            AcceptThreshold = 0.0;
            ContrastThreshold = 0.0;
            ApproxNumToFind = 0;
            AngleStartDeg = 0.0;
            AngleExtentDeg = 0.0;
            ScaleStart = 1.0;
            ScaleExtent = 0.0;
            IgnorePolarity = false;
            TimeoutMs = 0.0;
        }
    }
}
