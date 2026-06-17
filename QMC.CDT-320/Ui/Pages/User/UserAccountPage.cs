using System;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Pages.User
{
    /// <summary>
    /// 사용자 화면. 비로그인 상태면 로그인 카드(SessionControl)를 띄우고,
    /// 로그인되면 계정 목록을 보여준다. 추가/수정/삭제는 Admin 만 가능(그 외는 보기 전용).
    /// </summary>
    public partial class UserAccountPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private SessionControl _login;

        public UserAccountPage()
        {
            InitializeComponent();
            WireEvents();

            // 로그인 오버레이(비로그인 시 전체를 덮는 로그인 카드)
            _login = new SessionControl { Dock = DockStyle.Fill, Visible = false };
            Controls.Add(_login);

            if (!IsDesignerMode())
            {
                UserSession.UserChanged += OnUserChanged;
                RefreshState();
            }
        }

        private void WireEvents()
        {
            btnAdd.Click += (s, e) => AddAccount();
            btnEdit.Click += (s, e) => EditSelected();
            btnDelete.Click += (s, e) => DeleteSelected();
            btnLogout.Click += (s, e) => UserSession.Logout();
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && CanEdit()) EditAccount(e.RowIndex); };
        }

        private static bool CanEdit() => UserSession.Has(UserLevel.Admin);

        private void OnUserChanged()
        {
            if (InvokeRequired) { try { BeginInvoke(new Action(OnUserChanged)); } catch { } return; }
            RefreshState();
        }

        // 로그인 상태에 따라 로그인 카드 / 계정 목록 전환.
        private void RefreshState()
        {
            bool loggedIn = UserSession.Level != UserLevel.None;

            if (!loggedIn)
            {
                _login.Reset();
                _login.Visible = true;
                _login.BringToFront();
                return;
            }

            _login.Visible = false;

            // 편집은 Admin 만, 로그아웃은 항상 가능.
            bool admin = CanEdit();
            btnAdd.Enabled = btnEdit.Enabled = btnDelete.Enabled = admin;
            btnLogout.Enabled = true;

            LoadGrid();
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            foreach (var a in UserStore.All)
            {
                grid.Rows.Add(a.Id, a.LevelEnum.ToString(), a.Enabled ? "Y" : "N", a.LastLogin ?? "");
            }
        }

        private void AddAccount()
        {
            if (!CanEdit()) return;
            using (var dlg = new UserAccountDialog())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                if (UserStore.Get(dlg.ResultId) != null)
                {
                    MessageBox.Show(this, "이미 존재하는 ID 입니다.", "USER", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var acc = new UserAccount
                {
                    Id = dlg.ResultId,
                    LevelEnum = dlg.ResultLevel,
                    Enabled = dlg.ResultEnabled,
                    Hash = UserStore.Hash(dlg.ResultPassword)
                };
                UserStore.Upsert(acc);
                LoadGrid();
            }
        }

        private void EditSelected()
        {
            if (CanEdit() && grid.CurrentRow != null) EditAccount(grid.CurrentRow.Index);
        }

        private void EditAccount(int rowIndex)
        {
            if (!CanEdit()) return;
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return;
            string id = grid.Rows[rowIndex].Cells[0].Value as string;
            var acc = UserStore.Get(id);
            if (acc == null) return;

            using (var dlg = new UserAccountDialog(acc))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                acc.LevelEnum = dlg.ResultLevel;
                acc.Enabled   = dlg.ResultEnabled;
                if (!string.IsNullOrEmpty(dlg.ResultPassword))  // 비워두면 기존 비번 유지
                    acc.Hash = UserStore.Hash(dlg.ResultPassword);

                UserStore.Upsert(acc);
                LoadGrid();
            }
        }

        private void DeleteSelected()
        {
            if (!CanEdit() || grid.CurrentRow == null) return;
            string id = grid.CurrentRow.Cells[0].Value as string;
            if (string.IsNullOrEmpty(id)) return;

            if (string.Equals(id, UserSession.Name, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "현재 로그인한 계정은 삭제할 수 없습니다.", "USER", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var target = UserStore.Get(id);
            if (target != null && target.LevelEnum == UserLevel.Admin
                && UserStore.All.Count(a => a.LevelEnum == UserLevel.Admin) <= 1)
            {
                MessageBox.Show(this, "마지막 관리자 계정은 삭제할 수 없습니다.", "USER", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (QMC.Common.MessageDialog.Show(this, id + " 계정을 삭제할까요?", "USER", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                return;

            UserStore.Remove(id);
            LoadGrid();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { UserSession.UserChanged -= OnUserChanged; } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}
