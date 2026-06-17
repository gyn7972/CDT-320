using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>Bin Vision 모듈 — Reticle/Die/Placement/Scale 포함.</summary>
    public sealed class BinVisionModule
        : VisionModule<BinVisionSetup, BinVisionConfig, BinVisionRecipe>
    {
        public override string AlgorithmKey => QMC.Common.Recipes.VisionAlgorithm.Bin;

        public IPatternFinder Reticle           { get; }
        public IPatternFinder Die               { get; }
        public IInspector     PlacementInspector{ get; }
        public IPatternFinder Scale             { get; }

        public BinVisionModule(ICamera camera, IVisionBackend backend)
            : base("BinVision", camera, backend)
        {
            Reticle            = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("ReticleFinder");
            Die                = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("DieFinder");
            PlacementInspector = AddInspector<InspectorAlgoSetup, InspectorAlgoConfig, InspectorAlgoRecipe>("PlacementInspector");
            Scale              = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("ScaleFinder");
        }
    }
}
