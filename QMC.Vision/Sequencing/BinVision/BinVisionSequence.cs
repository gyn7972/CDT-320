using System.Collections.Generic;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// BinVision 시퀀스 — CDT-310 흐름: GRAB → 레티클 → 다이(정렬) → 안착(Placement) 검사 → 스케일.
    /// </summary>
    public sealed class BinVisionSequence : ModuleSequenceBase
    {
        public BinVisionSequence(VisionSequenceContext ctx)
            : base(ctx, SequenceModuleKind.BinVision, ctx?.Machine?.BinVision, "BinVision")
        {
        }

        protected override IEnumerable<KeyValuePair<string, string>> CycleSteps()
        {
            yield return Step("GRAB", null);
            yield return Step("MATCH",   "ReticleFinder");
            yield return Step("MATCH",   "DieFinder");
            yield return Step("INSPECT", "PlacementInspector");
            yield return Step("MATCH",   "ScaleFinder");
        }
    }
}
