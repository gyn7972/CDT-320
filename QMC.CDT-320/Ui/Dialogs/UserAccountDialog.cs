using System;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>계정 추가/수정 입력 대화상자. 비밀번호는 평문으로 받되 저장은 호출측에서 해시한다.</summary>
    public partial class UserAccountDialog : Form
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
                Text = "USER - EDIT";
                txtId.Text = existing.Id;
                txtId.ReadOnly = true;
                cmbLevel.SelectedItem = existing.LevelEnum.ToString();
                chkEnabled.Checked = existing.Enabled;
                lblPw.Text = "PASSWORD (변경 시)";
            }
            else
            {
                Text = "USER - ADD";
                cmbLevel.SelectedItem = UserLevel.Operator.ToString();
            }
            if (cmbLevel.SelectedIndex < 0) cmbLevel.SelectedIndex = 0;

            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            string id = (txtId.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(id))
            {
                MessageBox.Show(this, "ID 를 입력하세요.", "USER", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string pw = txtPw.Text ?? string.Empty;
            if (!_isEdit && string.IsNullOrEmpty(pw))
            {
                MessageBox.Show(this, "비밀번호를 입력하세요.", "USER", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
