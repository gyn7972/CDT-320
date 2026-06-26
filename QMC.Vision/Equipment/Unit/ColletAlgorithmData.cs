using System.Runtime.Serialization;

namespace QMC.Vision.Modules
{
    // ──────────────────────────────────────────────────────────────
    //  콜렛(ColletFinder) 전용 Setup / Config / Recipe
    //  공용 FinderAlgo* 를 수정하지 않고(다른 finder 영향 차단), 콜렛에만 필요한
    //  '플랫콜렛' 검출 파라미터를 추가한다(AlgorithmData.cs 규칙: 전용 필드 = 새 구체 타입).
    //  FinderAlgo* 를 상속하므로 Search/Train ROI·AcceptThreshold·Angle 등 기존
    //  저장/로드/UI 바인딩은 그대로 동작하고, 아래 플랫 필드만 추가로 직렬화된다.
    //  (구 BottomInspection.ColletFinder.recipe.json 은 플랫 키가 없으므로
    //   로드 시 OnDeserializing 기본값으로 채워진다 — 비파괴 호환.)
    //
    //  일반 콜렛 검사  : UseFlatCollet=false → 기존 패턴매치 finder(_finder.Match) 사용.
    //  플랫 콜렛 파인더: UseFlatCollet=true  → FlatColletFinder(std-dev→blob→min-area-rect) 사용.
    // ──────────────────────────────────────────────────────────────

    /// <summary>콜렛 Setup — 공용 Finder Setup(조명 결선·시뮬 이미지)과 동일. 전용 항목 없음.</summary>
    [DataContract]
    public sealed class ColletFinderSetup : FinderAlgoSetup
    {
    }

    /// <summary>콜렛 Config — 공용 Finder Config + 플랫콜렛 검출 고정 사양(블록크기·백엔드).</summary>
    [DataContract]
    public sealed class ColletFinderConfig : FinderAlgoConfig
    {
        /// <summary>플랫콜렛 std-dev 필터 블록 크기(px, 홀수 권장). 클수록 평탄·느림(기본 7).</summary>
        [DataMember] public int FlatBlockSize { get; set; }

        /// <summary>플랫콜렛 검출 CUDA 가속 사용. 미설치/실패 시 CPU 폴백.</summary>
        [DataMember] public bool FlatUseCuda { get; set; }

        /// <summary>플랫콜렛 고속 모드(다운샘플 등 정밀도↓·속도↑).</summary>
        [DataMember] public bool FlatFastMode { get; set; }

        public ColletFinderConfig() { SetColletDefaults(); }
        [OnDeserializing] private void OnDeserializingCollet(StreamingContext ctx) => SetColletDefaults();
        private void SetColletDefaults() { FlatBlockSize = 7; FlatUseCuda = false; FlatFastMode = false; }
    }

    /// <summary>콜렛 Recipe — 공용 Finder Recipe + 플랫콜렛 사용 여부·임계값(제품/공정별).</summary>
    [DataContract]
    public sealed class ColletFinderRecipe : FinderAlgoRecipe
    {
        /// <summary>플랫콜렛 사용 유무 — true 면 일반 콜렛(패턴매치) 대신 플랫콜렛 파인더로 검출.</summary>
        [DataMember] public bool UseFlatCollet { get; set; }

        /// <summary>플랫콜렛 std-dev 임계값 — 이 표준편차 이상 화소를 콜렛(텍스처/에지)로 본다(기본 12).</summary>
        [DataMember] public double FlatStdDevThreshold { get; set; }

        /// <summary>플랫콜렛 최소 블롭 면적(px^2) — 이보다 작은 블롭은 노이즈로 무시(기본 500).</summary>
        [DataMember] public double FlatMinAreaPx { get; set; }

        public ColletFinderRecipe() { SetColletDefaults(); }
        [OnDeserializing] private void OnDeserializingCollet(StreamingContext ctx) => SetColletDefaults();
        private void SetColletDefaults() { UseFlatCollet = false; FlatStdDevThreshold = 12.0; FlatMinAreaPx = 500.0; }
    }
}
