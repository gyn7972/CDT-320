using System.ComponentModel;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Tabs
{
    public partial class UserTab : TabBase
    {
        public UserTab()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            SetSidebarHeader("tab.user");
            RegisterSidebarButton(BtnUser, "tab.user", UserLevel.None, () => null);
        }
    }
}
