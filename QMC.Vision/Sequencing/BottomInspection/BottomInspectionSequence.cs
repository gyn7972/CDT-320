using System.Collections.Generic;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// BottomInspection 시퀀스 — CDT-310 흐름: GRAB → 레티클 → 콜렛 → 다이(외곽) → 표면/치핑 검사 → 포커스 → 스케일 → 왜곡보정.
    /// </summary>
    public sealed class BottomInspectionSequence : ModuleSequenceBase
    {
        public BottomInspectionSequence(VisionSequenceContext ctx)
            : base(ctx, SequenceModuleKind.BottomInspection, ctx?.Machine?.BottomInspection, "BottomInspection")
        {
        }

        protected override IEnumerable<KeyValuePair<string, string>> CycleSteps()
        {
            yield return Step("GRAB", null);
            yield return Step("MATCH",   "ReticleFinder");
            yield return Step("MATCH",   "ColletFinder");
            yield return Step("MATCH",   "DieFinder");
            yield return Step("INSPECT", "SurfaceInspector");
            yield return Step("MATCH",   "FocusFinder");
            yield return Step("MATCH",   "ScaleFinder");
            yield return Step("MATCH",   "DistortionCompensation");
        }
    }
}
