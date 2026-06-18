using System.Collections.Generic;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// BottomSideVision 시퀀스 — CDT-310 흐름: GRAB → 다이 외곽 → 표면 검사 → 치핑 검사 → 포커스.
    /// </summary>
    public sealed class BottomSideVisionSequence : ModuleSequenceBase
    {
        public BottomSideVisionSequence(VisionSequenceContext ctx)
            : base(ctx, SequenceModuleKind.BottomSideVision, ctx?.Machine?.BottomSideVision, "BottomSideVision")
        {
        }

        protected override IEnumerable<KeyValuePair<string, string>> CycleSteps()
        {
            yield return Step("GRAB", null);
            yield return Step("MATCH",   "DieEdgeFinder");
            yield return Step("INSPECT", "BottomSurfaceInspector");
            yield return Step("INSPECT", "BottomChippingInspector");
            yield return Step("MATCH",   "FocusFinder");
        }
    }
}
