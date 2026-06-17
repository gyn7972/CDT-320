using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>
    /// Wafer Vision 모듈 — EjectPin/Reticle/AlignDie/FirstRef/SecondRef/Die/Scale finder 포함.
    /// 모듈별 고유 Setup/Config/Recipe(WaferVision*) 사용. 알고리즘별 데이터는 각 Finder 노드가 관리.
    /// </summary>
    public sealed class WaferVisionModule
        : VisionModule<WaferVisionSetup, WaferVisionConfig, WaferVisionRecipe>
    {
        public override string AlgorithmKey => QMC.Common.Recipes.VisionAlgorithm.Wafer;

        public IPatternFinder EjectPin        { get; }
        public IPatternFinder Reticle         { get; }
        public IPatternFinder AlignDie        { get; }
        public IPatternFinder FirstReference  { get; }
        public IPatternFinder SecondReference { get; }
        public IPatternFinder Die             { get; }
        public IPatternFinder Scale           { get; }

        public WaferVisionModule(ICamera camera, IVisionBackend backend)
            : base("WaferVision", camera, backend)
        {
            EjectPin        = AddFinder<FinderAlgoSetup, FinderAlgoConfig, FinderAlgoRecipe>("EjectPinFinder");
            Reticle         = AddFinder<FinderAlgoSetup, FinderAlgoConfig, FinderAlgoRecipe>("ReticleFinder");
            AlignDie        = AddFinder<FinderAlgoSetup, FinderAlgoConfig, FinderAlgoRecipe>("AlignDieFinder");
            FirstReference  = AddFinder<FinderAlgoSetup, FinderAlgoConfig, FinderAlgoRecipe>("FirstReferenceFinder");
            SecondReference = AddFinder<FinderAlgoSetup, FinderAlgoConfig, FinderAlgoRecipe>("SecondReferenceFinder");
            Die             = AddFinder<FinderAlgoSetup, FinderAlgoConfig, FinderAlgoRecipe>("DieFinder");
            Scale           = AddFinder<FinderAlgoSetup, FinderAlgoConfig, FinderAlgoRecipe>("ScaleFinder");
        }
    }
}
