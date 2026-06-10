using QMC.Common;
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

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        protected virtual void SetDefaults() { IsSimulationMode = false; }
    }

    /// <summary>모듈 Config 공통 — 카메라/그랩 고정 사양.</summary>
    [DataContract]
    public abstract class VisionModuleConfigBase : IConfigData
    {
        [DataMember] public string CameraId          { get; set; }
        [DataMember] public double Gain              { get; set; }
        [DataMember] public double FrameRate         { get; set; }
        [DataMember] public string TriggerMode       { get; set; }
        [DataMember] public string PixelFormat       { get; set; }
        [DataMember] public int    DelayBeforeGrabMs { get; set; }
        [DataMember] public int    RoiOffsetX        { get; set; }
        [DataMember] public int    RoiOffsetY        { get; set; }
        [DataMember] public int    RoiWidth          { get; set; }
        [DataMember] public int    RoiHeight         { get; set; }

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        protected virtual void SetDefaults()
        {
            CameraId = string.Empty;
            Gain = 1.0;
            FrameRate = 30.0;
            TriggerMode = string.Empty;
            PixelFormat = string.Empty;
            DelayBeforeGrabMs = 0;
            RoiOffsetX = 0; RoiOffsetY = 0; RoiWidth = 0; RoiHeight = 0;
        }
    }

    /// <summary>모듈 Recipe 공통 — 제품/공정별 노출.</summary>
    [DataContract]
    public abstract class VisionModuleRecipeBase : IRecipeData
    {
        [DataMember] public double Exposure { get; set; }

        [OnDeserializing] private void OnDeserializing(StreamingContext ctx) => SetDefaults();
        protected virtual void SetDefaults() { Exposure = 5000; }
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

    // ── FrontSideInspection ───────────────────────────────────────
    [DataContract] public sealed class FrontSideInspectionSetup  : VisionModuleSetupBase { }
    [DataContract] public sealed class FrontSideInspectionConfig : VisionModuleConfigBase { }
    [DataContract]
    public sealed class FrontSideInspectionRecipe : VisionModuleRecipeBase
    {
        [DataMember] public double ChippingThreshold { get; set; }
        protected override void SetDefaults() { base.SetDefaults(); ChippingThreshold = 0.05; }
    }

    // ── RearSideInspection ────────────────────────────────────────
    [DataContract] public sealed class RearSideInspectionSetup  : VisionModuleSetupBase { }
    [DataContract] public sealed class RearSideInspectionConfig : VisionModuleConfigBase { }
    [DataContract]
    public sealed class RearSideInspectionRecipe : VisionModuleRecipeBase
    {
        [DataMember] public double ChippingThreshold { get; set; }
        protected override void SetDefaults() { base.SetDefaults(); ChippingThreshold = 0.05; }
    }
}
