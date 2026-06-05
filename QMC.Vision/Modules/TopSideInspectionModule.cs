using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>
    /// Stage 52 — TopSide Inspection Vision 모듈 (CDT-310 매뉴얼 사양, port 5105).<br/>
    /// 다이의 상단 측면 검사 — 칩핑 + 스크래치 + 오염 검사.
    /// </summary>
    public class TopSideInspectionModule : VisionModule
    {
        public IPatternFinder DieEdge        { get; }
        public IInspector     Surface        { get; }
        public IInspector     Chipping       { get; }
        public IPatternFinder Focus          { get; }

        public TopSideInspectionModule(ICamera camera, IVisionBackend backend)
            : base("TopSideInspection", camera, backend)
        {
            DieEdge   = AddFinder   ("DieEdgeFinder");
            Surface   = AddInspector("TopSurfaceInspector");
            Chipping  = AddInspector("TopChippingInspector");
            Focus     = AddFinder   ("FocusFinder");
        }
    }
}
