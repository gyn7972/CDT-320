using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>
    /// Wafer Vision 모듈 — 메뉴얼의 WaferVision:
    /// EjectPin / Reticle / AlignDie / FirstRef / SecondRef / Die / Scale finder 포함.
    /// </summary>
    public class WaferVisionModule : VisionModule
    {
        public IPatternFinder EjectPin        { get; }
        public IPatternFinder Reticle         { get; }
        public IPatternFinder AlignDie        { get; }
        public IPatternFinder FirstReference  { get; }
        public IPatternFinder SecondReference { get; }
        public IPatternFinder Die             { get; }
        public IPatternFinder Scale           { get; }

        public WaferVisionModule(ICamera camera, IVisionBackend backend)
            : base("WaferVision", camera, backend)
        {
            EjectPin        = AddFinder("EjectPinFinder");
            Reticle         = AddFinder("ReticleFinder");
            AlignDie        = AddFinder("AlignDieFinder");
            FirstReference  = AddFinder("FirstReferenceFinder");
            SecondReference = AddFinder("SecondReferenceFinder");
            Die             = AddFinder("DieFinder");
            Scale           = AddFinder("ScaleFinder");
        }
    }
}
