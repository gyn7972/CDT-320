using QMC.Vision.Core;

namespace QMC.Vision.Modules
{
    /// <summary>
    /// Stage 52 — RearSide Inspection Vision 모듈 (CDT-310 매뉴얼 사양, port 5106).<br/>
    /// 다이의 뒤쪽 측면 검사 — 칩핑 + 스크래치 + 오염 검사.
    /// (Stage 63 리네임: BottomSide → RearSide. inspector 내부 id 는 호환 위해 유지.)
    /// </summary>
    public class RearSideInspectionModule : VisionModule
    {
        public IPatternFinder DieEdge        { get; }
        public IInspector     Surface        { get; }
        public IInspector     Chipping       { get; }
        public IPatternFinder Focus          { get; }

        public RearSideInspectionModule(ICamera camera, IVisionBackend backend)
            : base("RearSideInspection", camera, backend)
        {
            DieEdge   = AddFinder   ("DieEdgeFinder");
            Surface   = AddInspector("BottomSurfaceInspector");
            Chipping  = AddInspector("BottomChippingInspector");
            Focus     = AddFinder   ("FocusFinder");
        }
    }
}
