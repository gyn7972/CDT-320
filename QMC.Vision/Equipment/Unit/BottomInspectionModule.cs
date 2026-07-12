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
        public IPatternFinder ColletRotCenter  { get; }

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
            // 콜렛 회전 중심(COC) — 회전 누적 평균 영상의 대칭 중심 측정. 노출/조명 레시피 보유용 노드
            // (검출 자체는 ColletRotationCenterCore 가 수행, finder MATCH 는 사용하지 않음).
            ColletRotCenter = AddFinder <FinderAlgoSetup,    FinderAlgoConfig,    FinderAlgoRecipe>   (QMC.Vision.Core.ColletRotationCenterCore.ToolId);
        }
    }
}
