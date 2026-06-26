using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>Bottom Inspection 모듈 — Reticle/Collet/Die/Surface/Focus/Scale/Distortion.</summary>
    public sealed class BottomInspectionModule
        : VisionModule<BottomInspectionSetup, BottomInspectionConfig, BottomInspectionRecipe>
    {
        public override string AlgorithmKey => QMC.Common.Recipes.VisionAlgorithm.BottomInspection;

        public IPatternFinder Reticle          { get; }
        public IPatternFinder Collet           { get; }
        public IPatternFinder Die              { get; }
        public IInspector     Surface          { get; }
        public IPatternFinder Focus            { get; }
        public IPatternFinder Scale            { get; }
        public IPatternFinder DistortionComp   { get; }

        public BottomInspectionModule(ICamera camera, IVisionBackend backend)
            : base("BottomInspection", camera, backend)
        {
            Reticle        = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("ReticleFinder");
            Collet         = AddFinder   <ColletFinderSetup,  ColletFinderConfig,  ColletFinderRecipe> ("ColletFinder");  // 전용 타입(플랫콜렛)
            Die            = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("DieFinder");
            Surface        = AddInspector<InspectorAlgoSetup, InspectorAlgoConfig, InspectorAlgoRecipe>("SurfaceInspector");
            Focus          = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("FocusFinder");
            Scale          = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("ScaleFinder");
            DistortionComp = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("DistortionCompensation");
        }
    }
}
