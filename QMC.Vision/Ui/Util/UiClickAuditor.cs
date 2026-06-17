using System.Windows.Forms;

namespace QMC.Vision.Ui.Util
{
    /// <summary>
    /// UI 점검(죽은 버튼 탐지) — 핸들러 QMC.CDT_320.Ui.Util.UiClickAuditor 정렬(경량 stub).
    /// 현재 Vision 미사용 — (0,0,0) 반환. 자동 클릭 점검이 필요해지면 구현한다.
    /// </summary>
    public static class UiClickAuditor
    {
        /// <summary>컨트롤 트리의 모든 버튼을 1회 클릭해 통계 반환. (stub: 0,0,0)</summary>
        public static (int tried, int success, int failed) PerformClickAll(Control root) => (0, 0, 0);
    }
}
