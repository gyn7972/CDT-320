using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// 300 스타일 로그인 다이얼로그 — 상단 오렌지 타이틀 바 + 좌측 계정 목록 + 우측 ID/PW/버튼.
    /// </summary>
    public class LoginDialog : Form
    {
        public LoginDialog()
        {
            Text            = Lang.T("dlg.login");
            ClientSize      = new Size(560, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MinimizeBox     = MaximizeBox = false;
            BackColor       = UiTheme.MainBg;
            Font            = UiTheme.ButtonFont;

            Build();
        }

        private void Build()
        {
            // 상단 오렌지 타이틀
            var title = new Label
            {
                Dock      = DockStyle.Top,
                Height    = 36,
                Text      = Lang.T("dlg.login"),
                Tag       = "i18n:dlg.login",
                BackColor = UiTheme.StatusBarBg,
                ForeColor = Color.White,
                Font      = new Font("맑은 고딕", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(title);

            // 좌측 계정 목록
            var lv = new ListView
            {
                Location  = new Point(12, 50),
                Size      = new Size(260, 300),
                View      = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false
            };
            // 사용자 보고: "로그인 안 됨" — ListView 의 Password 컬럼 값이 grade index(0/1/2/3) 였음.
            // 실제 비밀번호는 op/eng/mt/admin. Sim 환경 데모용으로 비밀번호 직접 표시.
            lv.Columns.Add("ID",       110);
            lv.Columns.Add("Grade",    100);
            lv.Columns.Add("Password",  60);
            lv.Items.Add(new ListViewItem(new[] { "operator", "Operator",    "op"    }));
            lv.Items.Add(new ListViewItem(new[] { "engineer", "Engineer",    "eng"   }));
            lv.Items.Add(new ListViewItem(new[] { "maint",    "Maintenance", "mt"    }));
            lv.Items.Add(new ListViewItem(new[] { "admin",    "Admin",       "admin" }));
            Controls.Add(lv);

            // 우측 ID/PW + 버튼
            var lblId = new Label { Location = new Point(290, 60), Size = new Size(90, 22), Text = "ID",       Font = UiTheme.ButtonFont };
            var tbId  = new TextBox{ Location = new Point(380, 56), Size = new Size(160, 26), Font = UiTheme.ButtonFont };
            var lblPw = new Label { Location = new Point(290, 96), Size = new Size(90, 22), Text = "PASSWORD", Font = UiTheme.ButtonFont };
            var tbPw  = new TextBox{ Location = new Point(380, 92), Size = new Size(160, 26), UseSystemPasswordChar = true, Font = UiTheme.ButtonFont };

            var btnOut = new Button { Location = new Point(290, 140), Size = new Size(120, 40), Text = "LOGOUT", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            var btnIn  = new Button { Location = new Point(420, 140), Size = new Size(120, 40), Text = "ENTER",  FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };

            btnIn.Click += (s, e) =>
            {
                if (UserSession.Login(tbId.Text.Trim(), tbPw.Text))
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show(this, "Login failed.", Lang.T("dlg.login"));
                    tbPw.Clear(); tbPw.Focus();
                }
            };
            btnOut.Click += (s, e) =>
            {
                UserSession.Logout();
                DialogResult = DialogResult.OK;
                Close();
            };

            lv.ItemSelectionChanged += (s, e) =>
            {
                if (e.IsSelected)
                {
                    tbId.Text = e.Item.SubItems[0].Text;
                    // 편의: 선택 시 비밀번호도 자동 채움 (Sim/데모 환경)
                    if (e.Item.SubItems.Count >= 3)
                    {
                        tbPw.Text = e.Item.SubItems[2].Text;
                    }
                }
            };
            // 편의: 더블클릭 시 즉시 로그인 시도
            lv.DoubleClick += (s, e) =>
            {
                if (lv.SelectedItems.Count == 0) return;
                var item = lv.SelectedItems[0];
                tbId.Text = item.SubItems[0].Text;
                tbPw.Text = item.SubItems[2].Text;
                btnIn.PerformClick();
            };

            // ADD & UPDATE 그룹 (Admin 전용)
            var grpAdd = new GroupBox
            {
                Location = new Point(290, 200), Size = new Size(260, 180),
                Text = "ADD & UPDATE", Font = UiTheme.SectionFont,
                Tag = "level:Admin"
            };
            var lblAId = new Label   { Location = new Point(12, 24), Size = new Size(60, 22),  Text = "ID" };
            var tbAId  = new TextBox { Location = new Point(80, 20), Size = new Size(160, 24) };
            var lblAPw = new Label   { Location = new Point(12, 54), Size = new Size(60, 22),  Text = "PASSWORD" };
            var tbAPw  = new TextBox { Location = new Point(80, 50), Size = new Size(160, 24), UseSystemPasswordChar = true };
            var btnMt  = new Button  { Location = new Point(12, 84), Size = new Size(110, 28), Text = "Maintenance", FlatStyle = FlatStyle.Flat };
            var btnOp  = new Button  { Location = new Point(130,84), Size = new Size(110, 28), Text = "Operator",    FlatStyle = FlatStyle.Flat };
            var btnAdd = new Button  { Location = new Point(12,118), Size = new Size(74, 28),  Text = "ADD",         FlatStyle = FlatStyle.Flat };
            var btnUp  = new Button  { Location = new Point(92,118), Size = new Size(74, 28),  Text = "UPDATE",      FlatStyle = FlatStyle.Flat };
            var btnDel = new Button  { Location = new Point(172,118),Size = new Size(74, 28),  Text = "DELETE",      FlatStyle = FlatStyle.Flat };
            grpAdd.Controls.Add(lblAId);
            grpAdd.Controls.Add(tbAId);
            grpAdd.Controls.Add(lblAPw);
            grpAdd.Controls.Add(tbAPw);
            grpAdd.Controls.Add(btnMt);
            grpAdd.Controls.Add(btnOp);
            grpAdd.Controls.Add(btnAdd);
            grpAdd.Controls.Add(btnUp);
            grpAdd.Controls.Add(btnDel);

            Controls.Add(lblId); Controls.Add(tbId);
            Controls.Add(lblPw); Controls.Add(tbPw);
            Controls.Add(btnOut); Controls.Add(btnIn);
            Controls.Add(grpAdd);

            AcceptButton = btnIn;

            Load += (s, e) =>
            {
                Lang.Apply(this);
                AccessControl.Apply(this);
                tbId.Focus();
            };
        }
    }
}
