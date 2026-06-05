using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>
    /// Bin Vision 모듈 — Reticle/Die/Placement/Scale 포함.
    /// </summary>
    public class BinVisionModule : VisionModule
    {
        public IPatternFinder Reticle           { get; }
        public IPatternFinder Die               { get; }
        public IInspector     PlacementInspector{ get; }
        public IPatternFinder Scale             { get; }

        public BinVisionModule(ICamera camera, IVisionBackend backend)
            : base("BinVision", camera, backend)
        {
            Reticle            = AddFinder   ("ReticleFinder");
            Die                = AddFinder   ("DieFinder");
            PlacementInspector = AddInspector("PlacementInspector");
            Scale              = AddFinder   ("ScaleFinder");
        }
    }
}
