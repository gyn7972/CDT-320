using System.Collections.Generic;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// BinVision 시퀀스 — CDT-310 흐름: GRAB → 레티클 → 다이 안착(Placement=DieGapInspect) 검사 → 스케일.
    /// (구 MATCH DieFinder 단계는 잘못된 구현이라 제거 — 다이는 패턴매치가 아니라 안착 갭 검사, 2026-07-11.)
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
            yield return Step("INSPECT", "PlacementInspector");
            yield return Step("MATCH",   "ScaleFinder");
        }
    }
}
