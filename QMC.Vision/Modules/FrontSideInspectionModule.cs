using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>FrontSide Inspection 모듈 (port 5105) — 앞쪽 측면 칩핑/스크래치/오염 검사.</summary>
    public sealed class FrontSideInspectionModule
        : VisionModule<FrontSideInspectionSetup, FrontSideInspectionConfig, FrontSideInspectionRecipe>
    {
        public override string AlgorithmKey => QMC.Common.Recipes.VisionAlgorithm.FrontSide;

        public IPatternFinder DieEdge        { get; }
        public IInspector     Surface        { get; }
        public IInspector     Chipping       { get; }
        public IPatternFinder Focus          { get; }

        public FrontSideInspectionModule(ICamera camera, IVisionBackend backend)
            : base("FrontSideInspection", camera, backend)
        {
            DieEdge   = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("DieEdgeFinder");
            Surface   = AddInspector<InspectorAlgoSetup, InspectorAlgoConfig, InspectorAlgoRecipe>("TopSurfaceInspector");
            Chipping  = AddInspector<InspectorAlgoSetup, InspectorAlgoConfig, InspectorAlgoRecipe>("TopChippingInspector");
            Focus     = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("FocusFinder");
        }
    }
}
