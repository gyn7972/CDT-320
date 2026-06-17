using QMC.Common;
using QMC.Common.Recipes;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QMC.Vision.Modules
{
    // ──────────────────────────────────────────────────────────────
    //  모듈 레벨 Setup / Config / Recipe
    //  공통 필드는 base 에 두고, 모듈별로 distinct 한 구체 타입을 둔다.
    //  (각 구체 타입은 제네릭 TSetup/TConfig/TRecipe 로 그 타입 그대로 직렬화되므로
    //   상속 필드 포함에 문제가 없다 — 다형성 직렬화는 사용하지 않는다.)
    // ──────────────────────────────────────────────────────────────

    /// <summary>모듈 Setup 공통 — 전원 OFF 후에도 유지되는 기구/모드 설정.</summary>
    [DataContract]
    public abstract class VisionModuleSetupBase : ISetupData
    {
        [DataMember] public bool IsSimulationMode { get; set; }

        /// <summary>이 모듈(카메라=조명 1:1)이 구동하는 조명 (컨트롤러, 페이지) 지정. 채널 열거=ChannelCount, 사용여부=레벨(검사 Recipe).</summary>
        [DataMember] public List<LightPageRef> LightPages { get; set; } = new List<LightPageRef>();

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        protected virtual void SetDefaults() { IsSimulationMode = false; LightPages = new List<LightPageRef>(); }
    }

    /// <summary>모듈 Config 공통 — 카메라/그랩 고정 사양은 VisionCamera(Component) 로 이전됨.
    /// (모듈 레벨 비카메라 Config 확장 지점)</summary>
    [DataContract]
    public abstract class VisionModuleConfigBase : IConfigData
    {
    }

    /// <summary>모듈 Recipe 공통 — 노출(Exposure)은 VisionCamera(Recipe) 로 이전됨.
    /// 모듈별 공정 파라미터는 파생 타입에 둔다.</summary>
    [DataContract]
    public abstract class VisionModuleRecipeBase : IRecipeData
    {
        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        protected virtual void SetDefaults() { }
    }

    // ── WaferVision ───────────────────────────────────────────────
    [DataContract] public sealed class WaferVisionSetup  : VisionModuleSetupBase { }
    [DataContract] public sealed class WaferVisionConfig : VisionModuleConfigBase { }
    [DataContract]
    public sealed class WaferVisionRecipe : VisionModuleRecipeBase
    {
        [DataMember] public int AlignTimeoutMs { get; set; }
        protected override void SetDefaults() { base.SetDefaults(); AlignTimeoutMs = 10000; }
    }

    // ── BinVision ─────────────────────────────────────────────────
    [DataContract] public sealed class BinVisionSetup  : VisionModuleSetupBase { }
    [DataContract] public sealed class BinVisionConfig : VisionModuleConfigBase { }
    [DataContract]
    public sealed class BinVisionRecipe : VisionModuleRecipeBase
    {
        [DataMember] public double PlacementToleranceMm { get; set; }
        protected override void SetDefaults() { base.SetDefaults(); PlacementToleranceMm = 0.05; }
    }

    // ── BottomInspection ──────────────────────────────────────────
    [DataContract] public sealed class BottomInspectionSetup  : VisionModuleSetupBase { }
    [DataContract] public sealed class BottomInspectionConfig : VisionModuleConfigBase { }
    [DataContract]
    public sealed class BottomInspectionRecipe : VisionModuleRecipeBase
    {
        [DataMember] public double SurfaceThreshold { get; set; }
        protected override void SetDefaults() { base.SetDefaults(); SurfaceThreshold = 0.7; }
    }

    // ── TopSideVision ───────────────────────────────────────
    [DataContract] public sealed class TopSideVisionSetup  : VisionModuleSetupBase { }
    [DataContract] public sealed class TopSideVisionConfig : VisionModuleConfigBase { }
    [DataContract]
    public sealed class TopSideVisionRecipe : VisionModuleRecipeBase
    {
        [DataMember] public double ChippingThreshold { get; set; }
        protected override void SetDefaults() { base.SetDefaults(); ChippingThreshold = 0.05; }
    }

    // ── BottomSideVision ────────────────────────────────────────
    [DataContract] public sealed class BottomSideVisionSetup  : VisionModuleSetupBase { }
    [DataContract] public sealed class BottomSideVisionConfig : VisionModuleConfigBase { }
    [DataContract]
    public sealed class BottomSideVisionRecipe : VisionModuleRecipeBase
    {
        [DataMember] public double ChippingThreshold { get; set; }
        protected override void SetDefaults() { base.SetDefaults(); ChippingThreshold = 0.05; }
    }
}
