using System.ComponentModel;
using QMC.CDT_320.Ui.Pages.User;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Tabs
{
    public partial class UserTab : TabBase
    {
        public UserTab()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            // 사용자 탭 콘텐츠 배경 + 사이드바 '사용자' 헤더를 흰색으로.
            PnlContent.BackColor = System.Drawing.Color.White;
            this.BackColor = System.Drawing.Color.White;

            SetSidebarHeader("tab.user");
            LblSidebarHeader.BackColor = System.Drawing.Color.White;
            // 누구나 진입 가능. 비로그인 시 로그인 카드가 뜨고, 로그인하면 계정 화면(Admin이면 편집)으로 전환.
            RegisterSidebarButton(BtnUser, "user.accounts", UserLevel.None, () => new UserAccountPage());
        }
    }
}
