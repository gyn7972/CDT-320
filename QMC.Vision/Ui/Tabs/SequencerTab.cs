using System.ComponentModel;
using System.Windows.Forms;
using QMC.Vision.Ui.Pages;

namespace QMC.Vision.Ui.Tabs
{
    /// <summary>시퀀서 탭 — 단일 콘텐츠(SequencerPage). 사이드바 없이 페이지를 직접 호스트(WorkTab 정렬).</summary>
    public class SequencerTab : TabBase
    {
        public SequencerTab()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            PnlSidebar.Visible = false;
            var page = new SequencerPage { Dock = DockStyle.Fill };
            PnlContent.Controls.Add(page);
        }
    }
}
