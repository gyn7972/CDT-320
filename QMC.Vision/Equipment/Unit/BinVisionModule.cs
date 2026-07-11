using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>Bin Vision 모듈 — Reticle/Placement(다이 안착 갭)/Scale 포함.
    /// '다이' 항목은 패턴 매칭이 아니라 안착 갭 검사(CDTInspector.DieGapInspect)다 —
    /// 구 DieFinder(패턴매치)는 잘못된 구현이라 제거(2026-07-11). 핸들러 와이어는 INSPECT PlacementInspector 그대로.</summary>
    public sealed class BinVisionModule
        : VisionModule<BinVisionSetup, BinVisionConfig, BinVisionRecipe>
    {
        public override string AlgorithmKey => QMC.Common.Recipes.VisionAlgorithm.Bin;

        public IPatternFinder Reticle           { get; }
        public IInspector     PlacementInspector{ get; }
        public IPatternFinder Scale             { get; }

        public BinVisionModule(ICamera camera, IVisionBackend backend)
            : base("BinVision", camera, backend)
        {
            Reticle            = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("ReticleFinder");
            PlacementInspector = AddInspector<InspectorAlgoSetup, InspectorAlgoConfig, InspectorAlgoRecipe>("PlacementInspector");
            Scale              = AddFinder   <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   ("ScaleFinder");
        }
    }
}
