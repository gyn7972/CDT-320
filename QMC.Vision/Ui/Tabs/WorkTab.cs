using System.ComponentModel;
using System.Windows.Forms;
using QMC.Vision.Ui.Pages;

namespace QMC.Vision.Ui.Tabs
{
    /// <summary>
    /// 작업(운영) 탭 — 단일 콘텐츠(OperationPage).
    /// Vision 은 핸들러 Work 의 다중 작업 기능(초기화/시작/사이클 등)이 없어 탭 사이드바를 두지 않고
    /// (중복 라벨 방지) 페이지를 직접 호스트한다.
    /// </summary>
    public class WorkTab : TabBase
    {
        public WorkTab()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            PnlSidebar.Visible = false;
            var page = new OperationPage { Dock = DockStyle.Fill };
            PnlContent.Controls.Add(page);
        }
    }
}
