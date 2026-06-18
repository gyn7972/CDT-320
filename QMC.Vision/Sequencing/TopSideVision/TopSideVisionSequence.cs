using System.Collections.Generic;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// TopSideVision 시퀀스 — CDT-310 흐름: GRAB → 다이 외곽 → 표면 검사 → 치핑 검사 → 포커스.
    /// </summary>
    public sealed class TopSideVisionSequence : ModuleSequenceBase
    {
        public TopSideVisionSequence(VisionSequenceContext ctx)
            : base(ctx, SequenceModuleKind.TopSideVision, ctx?.Machine?.TopSideVision, "TopSideVision")
        {
        }

        protected override IEnumerable<KeyValuePair<string, string>> CycleSteps()
        {
            yield return Step("GRAB", null);
            yield return Step("MATCH",   "DieEdgeFinder");
            yield return Step("INSPECT", "TopSurfaceInspector");
            yield return Step("INSPECT", "TopChippingInspector");
            yield return Step("MATCH",   "FocusFinder");
        }
    }
}
