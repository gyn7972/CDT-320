using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>
    /// Stage 52 — FrontSide Inspection Vision 모듈 (CDT-310 매뉴얼 사양, port 5105).<br/>
    /// 다이의 앞쪽 측면 검사 — 칩핑 + 스크래치 + 오염 검사.
    /// (Stage 63 리네임: TopSide → FrontSide. inspector 내부 id 는 호환 위해 유지.)
    /// </summary>
    public class FrontSideInspectionModule : VisionModule
    {
        public IPatternFinder DieEdge        { get; }
        public IInspector     Surface        { get; }
        public IInspector     Chipping       { get; }
        public IPatternFinder Focus          { get; }

        public FrontSideInspectionModule(ICamera camera, IVisionBackend backend)
            : base("FrontSideInspection", camera, backend)
        {
            DieEdge   = AddFinder   ("DieEdgeFinder");
            Surface   = AddInspector("TopSurfaceInspector");
            Chipping  = AddInspector("TopChippingInspector");
            Focus     = AddFinder   ("FocusFinder");
        }
    }
}
