using QMC.CDT_320.Ui.Localization;
using System;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>계정 추가/수정 입력 대화상자. 비밀번호는 평문으로 받되 저장은 호출측에서 해시한다.</summary>
    public partial class UserAccountDialog : Form, ILocalizedView
    {
        private readonly bool _isEdit;

        public string    ResultId       { get; private set; }
        public UserLevel ResultLevel    { get; private set; }
        public bool      ResultEnabled  { get; private set; }
        /// <summary>입력한 새 비밀번호. 수정 모드에서 비워두면 기존 비번 유지.</summary>
        public string    ResultPassword { get; private set; }

        public UserAccountDialog(UserAccount existing = null)
        {
            InitializeComponent();
            _isEdit = existing != null;

            // 계정 레벨은 Operator~Admin (None 은 비로그인 상태라 계정 레벨로 두지 않음)
            cmbLevel.Items.Clear();
            cmbLevel.Items.Add(UserLevel.Operator.ToString());
            cmbLevel.Items.Add(UserLevel.Engineer.ToString());
            cmbLevel.Items.Add(UserLevel.Maintenance.ToString());
            cmbLevel.Items.Add(UserLevel.Admin.ToString());

            if (_isEdit)
            {
                Lang.BindKey(this, "dialog.user.edit");
                txtId.Text = existing.Id;
                txtId.ReadOnly = true;
                cmbLevel.SelectedItem = existing.LevelEnum.ToString();
                chkEnabled.Checked = existing.Enabled;
                Lang.BindKey(lblPw, "dialog.user.changePassword");
            }
            else
            {
                Lang.BindKey(this, "dialog.user.add");
                Lang.BindKey(lblPw, "dialog.user.password");
                cmbLevel.SelectedItem = UserLevel.Operator.ToString();
            }
            if (cmbLevel.SelectedIndex < 0) cmbLevel.SelectedIndex = 0;

            Lang.BindKey(lblId, "dialog.user.id");
            Lang.BindKey(lblLevel, "dialog.user.level");
            Lang.BindKey(chkEnabled, "dialog.user.enabled");
            Lang.BindKey(btnOk, "common.ok");
            Lang.BindKey(btnCancel, "common.cancel");
            cmbLevel.DrawMode = DrawMode.OwnerDrawFixed;
            cmbLevel.DrawItem += cmbLevel_DrawItem;
            Load += (sender, e) => Lang.Apply(this);
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        }

        public void ApplyLanguage()
        {
            cmbLevel.Invalidate();
        }

        private void cmbLevel_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= cmbLevel.Items.Count) return;
            e.DrawBackground();
            string value = cmbLevel.Items[e.Index] as string;
            string key = value == "Operator" ? "dialog.user.operator" :
                value == "Engineer" ? "dialog.user.engineer" :
                value == "Maintenance" ? "dialog.user.maintenance" : "dialog.user.admin";
            TextRenderer.DrawText(e.Graphics, Lang.T(key), e.Font, e.Bounds, e.ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            e.DrawFocusRectangle();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            string id = (txtId.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(id))
            {
                QMC.Common.MessageDialog.Show(this, Lang.T("dialog.user.enterId"), Lang.T("dialog.user.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string pw = txtPw.Text ?? string.Empty;
            if (!_isEdit && string.IsNullOrEmpty(pw))
            {
                QMC.Common.MessageDialog.Show(this, Lang.T("dialog.user.enterPassword"), Lang.T("dialog.user.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UserLevel lv;
            if (!Enum.TryParse(cmbLevel.SelectedItem as string, out lv)) lv = UserLevel.Operator;

            ResultId       = id;
            ResultLevel    = lv;
            ResultEnabled  = chkEnabled.Checked;
            ResultPassword = pw;   // 빈 문자열이면 (수정 시) 기존 유지

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
