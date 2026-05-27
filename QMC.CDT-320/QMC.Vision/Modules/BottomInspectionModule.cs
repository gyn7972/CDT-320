using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>
    /// Bottom Inspection Vision 모듈 — Reticle/Collet/Die/Surface/Focus/Scale/Distortion.
    /// </summary>
    public class BottomInspectionModule : VisionModule
    {
        public IPatternFinder Reticle          { get; }
        public IPatternFinder Collet           { get; }
        public IPatternFinder Die              { get; }
        public IInspector     Surface          { get; }
        public IPatternFinder Focus            { get; }
        public IPatternFinder Scale            { get; }
        public IPatternFinder DistortionComp   { get; }

        public BottomInspectionModule(ICamera camera, IVisionBackend backend)
            : base("BottomInspection", camera, backend)
        {
            Reticle        = AddFinder   ("ReticleFinder");
            Collet         = AddFinder   ("ColletFinder");
            Die            = AddFinder   ("DieFinder");
            Surface        = AddInspector("SurfaceInspector");
            Focus          = AddFinder   ("FocusFinder");
            Scale          = AddFinder   ("ScaleFinder");
            DistortionComp = AddFinder   ("DistortionCompensation");
        }
    }
}
