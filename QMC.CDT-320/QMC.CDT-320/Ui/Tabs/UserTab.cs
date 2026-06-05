using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Tabs
{
    /// <summary>사용자 탭 — 현재 세션 표시 + 로그인 다이얼로그 호출.</summary>
    public partial class UserTab : TabBase
    {
        private Label _lblCurrent;

        public UserTab()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            SetSidebarHeader("tab.user");
            AddSidebarButton("tab.user", UserLevel.None, () => null);

            BuildCenter();
            UpdateCurrentLabel();
            UserSession.UserChanged += UpdateCurrentLabel;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) UserSession.UserChanged -= UpdateCurrentLabel;
            base.Dispose(disposing);
        }

        private void BuildCenter()
        {
            var btn = new Button
            {
                Location  = new Point(40, 40),
                Size      = new Size(260, 60),
                Text      = Lang.T("dlg.login"),
                Tag       = "i18n:dlg.login",
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("맑은 고딕", 14F, FontStyle.Bold)
            };
            btn.Click += (s, e) =>
            {
                using (var dlg = new LoginDialog()) dlg.ShowDialog(FindForm());
            };
            PnlContent.Controls.Add(btn);

            _lblCurrent = new Label
            {
                Location = new Point(40, 120),
                AutoSize = true,
                Font     = new Font("맑은 고딕", 12F),
                ForeColor = Color.DarkSlateGray
            };
            PnlContent.Controls.Add(_lblCurrent);
        }

        private void UpdateCurrentLabel()
        {
            if (InvokeRequired) { BeginInvoke(new Action(UpdateCurrentLabel)); return; }
            if (_lblCurrent != null)
                _lblCurrent.Text = "Current: " + UserSession.Name + "  (" + UserSession.Level + ")";
        }
    }
}
