using System.ComponentModel;
using System.Windows.Forms;
using QMC.Vision.Ui.Pages;

namespace QMC.Vision.Ui.Tabs
{
    /// <summary>
    /// 사용자 탭 — 단일 콘텐츠(UserPage). 단일 화면이라 탭 사이드바 없이 페이지를 직접 호스트.
    /// </summary>
    public class UserTab : TabBase
    {
        public UserTab()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            PnlSidebar.Visible = false;
            var page = new UserPage { Dock = DockStyle.Fill };
            PnlContent.Controls.Add(page);
        }
    }
}
