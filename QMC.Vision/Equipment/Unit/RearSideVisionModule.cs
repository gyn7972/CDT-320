using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>RearSideVision 모듈 (port 5106) — 뒤쪽 측면 칩핑/스크래치/오염 검사. (핸들러 모듈명: RearSideVision)</summary>
    public sealed class RearSideVisionModule
        : VisionModule<RearSideVisionSetup, RearSideVisionConfig, RearSideVisionRecipe>
    {
        // 레시피 알고리즘 키는 데이터 호환을 위해 RearSide 유지 (TCP 모듈명과 별개).
        public override string AlgorithmKey => QMC.Common.Recipes.VisionAlgorithm.RearSide;

        public IPatternFinder DieEdge        { get; }
        public IInspector     Surface        { get; }
        public IInspector     Chipping       { get; }
        public IPatternFinder Focus          { get; }

        public RearSideVisionModule(ICamera camera, IVisionBackend backend)
            : base("RearSideVision", camera, backend)
        {
            DieEdge   = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("DieEdgeFinder");
            Surface   = AddInspector<InspectorAlgoSetup, InspectorAlgoConfig, InspectorAlgoRecipe>("RearSurfaceInspector");
            Chipping  = AddInspector<InspectorAlgoSetup, InspectorAlgoConfig, InspectorAlgoRecipe>("RearChippingInspector");
            Focus     = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("FocusFinder");
        }
    }
}
