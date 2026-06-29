using System.Collections.Generic;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// RearSideVision 시퀀스 — CDT-310 흐름: GRAB → 다이 외곽 → 표면 검사 → 치핑 검사 → 포커스.
    /// </summary>
    public sealed class RearSideVisionSequence : ModuleSequenceBase
    {
        public RearSideVisionSequence(VisionSequenceContext ctx)
            : base(ctx, SequenceModuleKind.RearSideVision, ctx?.Machine?.RearSideVision, "RearSideVision")
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
