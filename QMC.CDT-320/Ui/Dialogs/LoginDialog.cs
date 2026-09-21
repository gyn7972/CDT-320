using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class LoginDialog : Form, ILocalizedView
    {
        public LoginDialog()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            LoadAccounts();
            WireEvents();
            ApplyLanguage();
        }

        private void ApplyRuntimeUi()
        {
            Lang.BindKey(this, "dlg.login");
            Lang.BindKey(lblTitle, "dlg.login");
            Lang.BindKey(lblId, "dialog.user.id");
            Lang.BindKey(lblPassword, "dialog.user.password");
            Lang.BindKey(lblAdminId, "dialog.user.id");
            Lang.BindKey(lblAdminPassword, "dialog.user.password");
            Lang.BindKey(btnLogout, "dialog.login.logout");
            Lang.BindKey(btnEnter, "dialog.login.enter");
            Lang.BindKey(grpAddUpdate, "dialog.login.addUpdate");
            Lang.BindKey(btnMaintenance, "dialog.user.maintenance");
            Lang.BindKey(btnOperator, "dialog.user.operator");
            Lang.BindKey(btnAdd, "common.add");
            Lang.BindKey(btnUpdate, "common.update");
            Lang.BindKey(btnDelete, "common.delete");
        }

        public void ApplyLanguage()
        {
            colId.Text = Lang.T("dialog.user.id");
            colGrade.Text = Lang.T("dialog.user.level");
            colPassword.Text = Lang.T("dialog.user.password");
            // 인증에서 읽는 아이디/비밀번호 열은 유지하고 등급 표시 열만 갱신한다.
            string[] keys = { "dialog.user.operator", "dialog.user.engineer", "dialog.user.maintenance", "dialog.user.admin" };
            for (int i = 0; i < lvAccounts.Items.Count && i < keys.Length; i++)
                lvAccounts.Items[i].SubItems[1].Text = Lang.T(keys[i]);
        }

        private void LoadAccounts()
        {
            lvAccounts.Items.Clear();
            lvAccounts.Items.Add(new ListViewItem(new[] { "operator", "Operator", "op" }));
            lvAccounts.Items.Add(new ListViewItem(new[] { "engineer", "Engineer", "eng" }));
            lvAccounts.Items.Add(new ListViewItem(new[] { "maint", "Maintenance", "mt" }));
            lvAccounts.Items.Add(new ListViewItem(new[] { "admin", "Admin", "admin" }));
        }

        private void WireEvents()
        {
            btnEnter.Click += (s, e) => TryLogin();
            btnLogout.Click += (s, e) =>
            {
                UserSession.Logout();
                DialogResult = DialogResult.OK;
                Close();
            };

            lvAccounts.ItemSelectionChanged += (s, e) =>
            {
                if (!e.IsSelected) return;
                tbId.Text = e.Item.SubItems[0].Text;
                if (e.Item.SubItems.Count >= 3) tbPassword.Text = e.Item.SubItems[2].Text;
            };

            lvAccounts.DoubleClick += (s, e) =>
            {
                if (lvAccounts.SelectedItems.Count == 0) return;
                var item = lvAccounts.SelectedItems[0];
                tbId.Text = item.SubItems[0].Text;
                tbPassword.Text = item.SubItems[2].Text;
                TryLogin();
            };

            Load += (s, e) =>
            {
                Lang.Apply(this);
                AccessControl.Apply(this);
                tbId.Focus();
            };
        }

        private void TryLogin()
        {
            if (UserSession.Login(tbId.Text.Trim(), tbPassword.Text))
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            QMC.Common.MessageDialog.Show(this, Lang.T("dialog.login.failed"), Lang.T("dlg.login"));
            tbPassword.Clear();
            tbPassword.Focus();
        }
    }
}
