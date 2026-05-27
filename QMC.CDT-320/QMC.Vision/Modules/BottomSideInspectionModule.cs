using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>
    /// Stage 52 — BottomSide Inspection Vision 모듈 (CDT-310 매뉴얼 사양, port 5106).<br/>
    /// 다이의 하단 측면 검사 — 칩핑 + 스크래치 + 오염 검사.
    /// </summary>
    public class BottomSideInspectionModule : VisionModule
    {
        public IPatternFinder DieEdge        { get; }
        public IInspector     Surface        { get; }
        public IInspector     Chipping       { get; }
        public IPatternFinder Focus          { get; }

        public BottomSideInspectionModule(ICamera camera, IVisionBackend backend)
            : base("BottomSideInspection", camera, backend)
        {
            DieEdge   = AddFinder   ("DieEdgeFinder");
            Surface   = AddInspector("BottomSurfaceInspector");
            Chipping  = AddInspector("BottomChippingInspector");
            Focus     = AddFinder   ("FocusFinder");
        }
    }
}
