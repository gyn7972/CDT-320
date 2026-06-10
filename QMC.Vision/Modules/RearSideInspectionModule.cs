using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>RearSide Inspection 모듈 (port 5106) — 뒤쪽 측면 칩핑/스크래치/오염 검사.</summary>
    public sealed class RearSideInspectionModule
        : VisionModule<RearSideInspectionSetup, RearSideInspectionConfig, RearSideInspectionRecipe>
    {
        public override string AlgorithmKey => QMC.Common.Recipes.VisionAlgorithm.RearSide;

        public IPatternFinder DieEdge        { get; }
        public IInspector     Surface        { get; }
        public IInspector     Chipping       { get; }
        public IPatternFinder Focus          { get; }

        public RearSideInspectionModule(ICamera camera, IVisionBackend backend)
            : base("RearSideInspection", camera, backend)
        {
            DieEdge   = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("DieEdgeFinder");
            Surface   = AddInspector<InspectorAlgoSetup, InspectorAlgoConfig, InspectorAlgoRecipe>("BottomSurfaceInspector");
            Chipping  = AddInspector<InspectorAlgoSetup, InspectorAlgoConfig, InspectorAlgoRecipe>("BottomChippingInspector");
            Focus     = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("FocusFinder");
        }
    }
}
