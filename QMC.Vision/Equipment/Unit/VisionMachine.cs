using QMC.Common;

namespace QMC.Vision.Modules
{
    /// <summary>
    /// Vision PC 설비 루트 — 핸들러 <c>CDT320Machine</c> 정렬.
    /// <see cref="Machine{TSetup,TConfig,TRecipe}"/> 루트로, 5개 비전 모듈(Unit)을 <c>Units</c> 로 소유한다.
    /// 계층: Machine → Unit(Module) → Component(Camera) → Algorithm.
    /// Save/Load/Recipe 는 Composite 로 전 모듈에 연쇄된다(레시피 동기화 단일 진입점).
    /// </summary>
    public sealed class VisionMachine : Machine<VisionMachineSetup, VisionMachineConfig, VisionMachineRecipe>
    {
        public WaferVisionModule      WaferVision      { get; }
        public BinVisionModule        BinVision        { get; }
        public BottomInspectionModule BottomInspection { get; }
        public TopSideVisionModule    TopSideVision    { get; }
        public BottomSideVisionModule BottomSideVision { get; }

        /// <summary>현재 활성 레시피 명칭(핸들러 수신 = Recipe.RecipeName). 미설정 시 "default".</summary>
        public string CurrentRecipeName =>
            string.IsNullOrWhiteSpace(Recipe?.RecipeName) ? "default" : Recipe.RecipeName;

        public VisionMachine(
            WaferVisionModule wafer,
            BinVisionModule bin,
            BottomInspectionModule bottom,
            TopSideVisionModule topSide,
            BottomSideVisionModule bottomSide)
            : base("CDT-320-VISION")
        {
            WaferVision      = wafer;
            BinVision        = bin;
            BottomInspection = bottom;
            TopSideVision    = topSide;
            BottomSideVision = bottomSide;

            // Composite: 핸들러 CDT320Machine 과 동일하게 하위 Unit 을 Units 에 등록.
            if (wafer      != null) Units.Add(wafer);
            if (bin        != null) Units.Add(bin);
            if (bottom     != null) Units.Add(bottom);
            if (topSide    != null) Units.Add(topSide);
            if (bottomSide != null) Units.Add(bottomSide);
        }
    }
}
