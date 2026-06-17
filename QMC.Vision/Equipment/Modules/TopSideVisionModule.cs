using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>TopSideVision 모듈 (port 5105) — 앞쪽 측면 칩핑/스크래치/오염 검사. (핸들러 모듈명: TopSideVision)</summary>
    public sealed class TopSideVisionModule
        : VisionModule<TopSideVisionSetup, TopSideVisionConfig, TopSideVisionRecipe>
    {
        // 레시피 알고리즘 키는 데이터 호환을 위해 FrontSide 유지 (TCP 모듈명과 별개).
        public override string AlgorithmKey => QMC.Common.Recipes.VisionAlgorithm.FrontSide;

        public IPatternFinder DieEdge        { get; }
        public IInspector     Surface        { get; }
        public IInspector     Chipping       { get; }
        public IPatternFinder Focus          { get; }

        public TopSideVisionModule(ICamera camera, IVisionBackend backend)
            : base("TopSideVision", camera, backend)
        {
            DieEdge   = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("DieEdgeFinder");
            Surface   = AddInspector<InspectorAlgoSetup, InspectorAlgoConfig, InspectorAlgoRecipe>("TopSurfaceInspector");
            Chipping  = AddInspector<InspectorAlgoSetup, InspectorAlgoConfig, InspectorAlgoRecipe>("TopChippingInspector");
            Focus     = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("FocusFinder");
        }
    }
}
