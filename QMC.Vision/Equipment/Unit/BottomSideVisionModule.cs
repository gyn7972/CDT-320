using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>BottomSideVision 모듈 (port 5106) — 뒤쪽 측면 칩핑/스크래치/오염 검사. (핸들러 모듈명: BottomSideVision)</summary>
    public sealed class BottomSideVisionModule
        : VisionModule<BottomSideVisionSetup, BottomSideVisionConfig, BottomSideVisionRecipe>
    {
        // 레시피 알고리즘 키는 데이터 호환을 위해 RearSide 유지 (TCP 모듈명과 별개).
        public override string AlgorithmKey => QMC.Common.Recipes.VisionAlgorithm.RearSide;

        public IPatternFinder DieEdge        { get; }
        public IInspector     Surface        { get; }
        public IInspector     Chipping       { get; }
        public IPatternFinder Focus          { get; }

        public BottomSideVisionModule(ICamera camera, IVisionBackend backend)
            : base("BottomSideVision", camera, backend)
        {
            DieEdge   = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("DieEdgeFinder");
            Surface   = AddInspector<InspectorAlgoSetup, InspectorAlgoConfig, InspectorAlgoRecipe>("BottomSurfaceInspector");
            Chipping  = AddInspector<InspectorAlgoSetup, InspectorAlgoConfig, InspectorAlgoRecipe>("BottomChippingInspector");
            Focus     = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("FocusFinder");
        }
    }
}
