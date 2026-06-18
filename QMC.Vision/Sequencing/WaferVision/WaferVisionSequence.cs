using System.Collections.Generic;

namespace QMC.Vision.Sequencing
{
    /// <summary>
    /// WaferVision 시퀀스 — CDT-310 흐름: GRAB → 이젝트핀 → 레티클 → 1·2차 기준점(2점 정렬) → 얼라인다이 → 다이 → 스케일.
    /// </summary>
    public sealed class WaferVisionSequence : ModuleSequenceBase
    {
        public WaferVisionSequence(VisionSequenceContext ctx)
            : base(ctx, SequenceModuleKind.WaferVision, ctx?.Machine?.WaferVision, "WaferVision")
        {
        }

        protected override IEnumerable<KeyValuePair<string, string>> CycleSteps()
        {
            yield return Step("GRAB", null);
            yield return Step("MATCH", "EjectPinFinder");
            yield return Step("MATCH", "ReticleFinder");
            yield return Step("MATCH", "FirstReferenceFinder");   // 2점 정렬 기준 1
            yield return Step("MATCH", "SecondReferenceFinder");  // 2점 정렬 기준 2 → 각도/좌표 보정
            yield return Step("MATCH", "AlignDieFinder");
            yield return Step("MATCH", "DieFinder");
            yield return Step("MATCH", "ScaleFinder");
        }
    }
}
