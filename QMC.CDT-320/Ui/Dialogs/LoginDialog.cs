using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class LoginDialog : Form
    {
        public LoginDialog()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            LoadAccounts();
            WireEvents();
        }

        private void ApplyRuntimeUi()
        {
            Text = Lang.T("dlg.login");
            lblTitle.Text = Text;
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

            MessageBox.Show(this, "Login failed.", Lang.T("dlg.login"));
            tbPassword.Clear();
            tbPassword.Focus();
        }
    }
}
