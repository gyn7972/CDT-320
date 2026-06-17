using QMC.Common;
using QMC.Common.Recipes;
using QMC.Vision.Core;
using System.Collections.Generic;
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

    // ── 공통 base (C2: 조명 흡수) ─────────────────────────────────
    //  조명 결선(Setup)/레벨(Recipe)을 Finder/Inspector 노드에 1회 정의.
    //  타입은 기존 QMC.Common.Recipes 재사용(Common 무수정). 직렬화는 노드 구체
    //  타입(typeof(T))으로 수행되므로 base [DataMember] 가 자연 포함된다([KnownType] 불요).
    //  구 JSON 에 키가 없으면 로드 시 null → [OnDeserializing] 으로 빈 리스트 초기화(비파괴).

    /// <summary>알고리즘 Setup 공통 base — 검사 노드 고정 설정. (조명 컨트롤러/페이지 지정은 모듈 Setup 으로 이전)</summary>
    [DataContract]
    public abstract class AlgoSetupBase : ISetupData
    {
        // 조명 컨트롤러/페이지 지정(LightPages)은 모듈 Setup(VisionModuleSetupBase)으로 이전 — 카메라=조명 1:1 하드웨어 계층.
        // 검사 노드는 조명 레벨(Recipe.LightSettings)만 보유. 구 노드 json 의 LightPages 키는 로드 시 무시(비파괴, 구 파일 보존).
    }

    /// <summary>알고리즘 Recipe 공통 base — 검사 조명 레벨(제품별 값). 키 = (ControllerPort, Channel).</summary>
    [DataContract]
    public abstract class AlgoRecipeBase : IRecipeData
    {
        /// <summary>검사별 채널 레벨/점등/스트로브/페이지. 키 = (ControllerPort, Channel). 채널은 Setup.LightPages 지정 컨트롤러의 ChannelCount.</summary>
        [DataMember] public List<InspectionLightSetting> LightSettings { get; set; } = new List<InspectionLightSetting>();

        protected AlgoRecipeBase() { LightSettings = new List<InspectionLightSetting>(); }
        [OnDeserializing] private void OnDeserializingLight(StreamingContext ctx)
        { LightSettings = new List<InspectionLightSetting>(); }
    }

    // ── Finder ────────────────────────────────────────────────────

    /// <summary>Finder Setup — 전원 OFF 후 유지되는 기구적 설정 + 조명 결선(base). 그 외 항목 없음(학습 ROI/모델은 Recipe, 2026-06-09).</summary>
    [DataContract]
    public class FinderAlgoSetup : AlgoSetupBase
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

    /// <summary>Finder Recipe — 제품/공정별 탐색·학습 파라미터(학습 ROI/모델 포함, 2026-06-09) + 조명 레벨(base).</summary>
    [DataContract]
    public class FinderAlgoRecipe : AlgoRecipeBase
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

    /// <summary>Inspector Setup — 전원 OFF 후 유지되는 캘리브 + 조명 결선(base).</summary>
    [DataContract]
    public class InspectorAlgoSetup : AlgoSetupBase
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

    /// <summary>Inspector Recipe — 제품/공정별 검사 임계값 + 조명 레벨(base).</summary>
    [DataContract]
    public class InspectorAlgoRecipe : AlgoRecipeBase
    {
        /// <summary>검사 ROI.</summary>
        [DataMember] public Roi InspectionRoi { get; set; }

        /// <summary>합/불 판정 임계값(Blob HardFixedThreshold, 0~255 그레이). 소비자(CognexInspector.Threshold)가 int.</summary>
        [DataMember] public int Threshold { get; set; }

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        private void SetDefaults() { Threshold = 128; }
    }
}
