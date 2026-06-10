using QMC.Common;
using QMC.Vision.Core;
using System.Runtime.Serialization;

namespace QMC.Vision.Modules
{
    // ──────────────────────────────────────────────────────────────
    //  알고리즘 레벨 Setup / Config / Recipe
    //  Finder/Inspector 가 공통으로 쓰는 재사용 데이터 타입.
    //  알고리즘마다 StorageKey 가 달라 파일이 분리되므로, 스키마는 공유해도
    //  저장/불러오기/삭제/수정은 알고리즘별로 독립이다.
    //  특정 알고리즘이 전용 필드가 필요하면 이 타입을 상속하지 말고
    //  새 구체 타입을 만들어 AddFinder<...>/AddInspector<...> 에 지정한다.
    // ──────────────────────────────────────────────────────────────

    // ── Finder ────────────────────────────────────────────────────

    /// <summary>Finder Setup — 전원 OFF 후 유지되는 기구적 설정. 현재 항목 없음(학습 ROI/모델은 Recipe 로 이동, 2026-06-09).</summary>
    [DataContract]
    public class FinderAlgoSetup : ISetupData
    {
    }

    /// <summary>Finder Config — 백엔드 고정 사양.</summary>
    [DataContract]
    public class FinderAlgoConfig : IConfigData
    {
        /// <summary>최대 인스턴스 수.</summary>
        [DataMember] public int MaxInstances { get; set; }

        /// <summary>각도 탐색 사용 여부.</summary>
        [DataMember] public bool AngleEnabled { get; set; }

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        private void SetDefaults() { MaxInstances = 1; AngleEnabled = false; }
    }

    /// <summary>Finder Recipe — 제품/공정별 탐색·학습 파라미터(학습 ROI/모델 포함, 2026-06-09).</summary>
    [DataContract]
    public class FinderAlgoRecipe : IRecipeData
    {
        /// <summary>최소 허용 score (0.0~1.0).</summary>
        [DataMember] public double AcceptThreshold { get; set; }

        /// <summary>탐색 ROI.</summary>
        [DataMember] public Roi SearchRoi { get; set; }

        /// <summary>학습 ROI(패턴 추출 영역). 제품/패턴별로 변경되므로 Recipe.</summary>
        [DataMember] public Roi TrainRoi { get; set; }

        /// <summary>학습된 패턴 모델 파일 경로(향후 백엔드 모델 직렬화 연동용, 현재 POCO-only). 비어있으면 미학습. 제품별이므로 Recipe.</summary>
        [DataMember] public string TrainModelPath { get; set; }

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        private void SetDefaults() { AcceptThreshold = 0.7; TrainModelPath = string.Empty; }
    }

    // ── Inspector ─────────────────────────────────────────────────

    /// <summary>Inspector Setup — 전원 OFF 후 유지되는 캘리브.</summary>
    [DataContract]
    public class InspectorAlgoSetup : ISetupData
    {
        /// <summary>검사 캘리브 모델 경로. 비어있으면 미설정.</summary>
        [DataMember] public string CalibModelPath { get; set; }

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        private void SetDefaults() { CalibModelPath = string.Empty; }
    }

    /// <summary>Inspector Config — 검사 고정 사양.</summary>
    [DataContract]
    public class InspectorAlgoConfig : IConfigData
    {
        /// <summary>검사 사용 여부.</summary>
        [DataMember] public bool Enable { get; set; }

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        private void SetDefaults() { Enable = true; }
    }

    /// <summary>Inspector Recipe — 제품/공정별 검사 임계값.</summary>
    [DataContract]
    public class InspectorAlgoRecipe : IRecipeData
    {
        /// <summary>검사 ROI.</summary>
        [DataMember] public Roi InspectionRoi { get; set; }

        /// <summary>합/불 판정 임계값(Blob HardFixedThreshold, 0~255 그레이). 소비자(CognexInspector.Threshold)가 int.</summary>
        [DataMember] public int Threshold { get; set; }

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        private void SetDefaults() { Threshold = 128; }
    }
}
