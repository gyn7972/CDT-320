using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Pages.User
{
    /// <summary>
    /// 사용자 화면. 비로그인 시 로그인 카드(SessionControl)를 띄우고, 로그인되면 계정 목록을 보여준다.
    /// 계정 행을 더블클릭하면 그 행 바로 아래로 권한 매트릭스가 펼쳐진다(그 계정의 레벨 권한 편집, Admin 만).
    /// </summary>
    public partial class UserAccountPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private const string DetailTag = "DETAIL";
        private const int DetailRowHeight = 270;

        private SessionControl _login;

        // 권한 매트릭스(펼침) 컨트롤
        private Panel _permHost;
        private DataGridView _permGrid;
        private Panel _permBar;
        private Button _btnPermSave;
        private int _expandedRow = -1;          // 펼쳐진 "계정 행" 인덱스 (-1 = 없음)
        private UserLevel _expandedLevel;
        private bool _permLoading;

        public UserAccountPage()
        {
            InitializeComponent();
            BuildPermDetail();
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

            // 계정 행 더블클릭 = 그 계정 아래로 권한 펼침/접기. (계정 편집은 수정 버튼)
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) ToggleExpand(e.RowIndex); };
            grid.Scroll += (s, e) => RepositionDetail();
            grid.SizeChanged += (s, e) => RepositionDetail();
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
            Collapse();   // 상태 전환 시 펼침 초기화

            if (!loggedIn)
            {
                _login.Reset();
                _login.Visible = true;
                _login.BringToFront();
                return;
            }

            _login.Visible = false;
            bool admin = CanEdit();
            btnAdd.Enabled = btnEdit.Enabled = btnDelete.Enabled = admin;
            btnLogout.Enabled = true;
            LoadGrid();
        }

        private void LoadGrid()
        {
            Collapse();
            grid.Rows.Clear();
            foreach (var a in UserStore.All)
                grid.Rows.Add(a.Id, a.LevelEnum.ToString(), a.Enabled ? "Y" : "N", a.LastLogin ?? "", "펼치기 ▾");
        }

        // ──────────────────────────────────────────
        //  계정 추가 / 수정 / 삭제
        // ──────────────────────────────────────────

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
            if (CanEdit() && grid.CurrentRow != null && !IsDetail(grid.CurrentRow))
                EditAccount(grid.CurrentRow.Index);
        }

        private void EditAccount(int rowIndex)
        {
            if (!CanEdit()) return;
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return;
            if (IsDetail(grid.Rows[rowIndex])) return;

            string id = grid.Rows[rowIndex].Cells["colId"].Value as string;
            var acc = UserStore.Get(id);
            if (acc == null) return;

            using (var dlg = new UserAccountDialog(acc))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                acc.LevelEnum = dlg.ResultLevel;
                acc.Enabled   = dlg.ResultEnabled;
                if (!string.IsNullOrEmpty(dlg.ResultPassword))
                    acc.Hash = UserStore.Hash(dlg.ResultPassword);
                UserStore.Upsert(acc);
                LoadGrid();
            }
        }

        private void DeleteSelected()
        {
            if (!CanEdit() || grid.CurrentRow == null || IsDetail(grid.CurrentRow)) return;
            string id = grid.CurrentRow.Cells["colId"].Value as string;
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

        // ──────────────────────────────────────────
        //  권한 매트릭스 (행 아래로 펼침)
        // ──────────────────────────────────────────

        private static bool IsDetail(DataGridViewRow row) => row != null && DetailTag.Equals(row.Tag);

        // 펼침용 패널(_permHost) 을 계정 그리드 위에 얹어서 둔다.
        private void BuildPermDetail()
        {
            // 계정 그리드 끝에 "권한" 안내 컬럼 추가 — 더블클릭하면 권한 설정이 펼쳐진다는 표시.
            var colPerm = new DataGridViewTextBoxColumn
            {
                Name = "colPerm",
                HeaderText = "권한",
                ReadOnly = true,
                FillWeight = 95F,
                MinimumWidth = 120,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colPerm.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colPerm.DefaultCellStyle.ForeColor = Color.FromArgb(0, 121, 107);   // 안내 텍스트(틸)
            grid.Columns.Add(colPerm);

            _permGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = false,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                Font = new Font("맑은 고딕", 9.5F),
                ColumnHeadersHeight = 26
            };
            // 헤더/셀을 권한 패널 테마(틸)로 통일해 계정 그리드와 구분되게 한다.
            var hs = _permGrid.ColumnHeadersDefaultCellStyle;
            hs.BackColor = Color.FromArgb(0, 121, 107);   // 틸 헤더
            hs.ForeColor = Color.White;
            hs.Alignment = DataGridViewContentAlignment.MiddleCenter;
            hs.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            _permGrid.RowTemplate.Height = 24;
            _permGrid.ScrollBars = ScrollBars.None;                              // 항상 다 보이게(높이를 내용에 맞춤)
            _permGrid.GridColor = Color.FromArgb(178, 223, 219);                 // 옅은 틸 격자선
            // 선택 강조가 보이지 않도록 선택 색 = 평상시 셀 색 (열별로 아래에서 동일하게 지정)

            var colMenu = new DataGridViewTextBoxColumn { HeaderText = "메뉴", Name = "menu", ReadOnly = true, FillWeight = 75, MinimumWidth = 140, SortMode = DataGridViewColumnSortMode.NotSortable };
            colMenu.DefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
            colMenu.DefaultCellStyle.BackColor = Color.FromArgb(232, 245, 243);  // 메뉴열 옅은 틸
            colMenu.DefaultCellStyle.ForeColor = Color.FromArgb(38, 50, 56);
            colMenu.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 245, 243);  // 선택해도 색 변화 없음
            colMenu.DefaultCellStyle.SelectionForeColor = Color.FromArgb(38, 50, 56);
            var colAllow = new DataGridViewCheckBoxColumn { HeaderText = "허용", Name = "allow", ReadOnly = true, ThreeState = true, FillWeight = 25, MinimumWidth = 70, SortMode = DataGridViewColumnSortMode.NotSortable };
            colAllow.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colAllow.DefaultCellStyle.BackColor = Color.White;
            colAllow.DefaultCellStyle.SelectionBackColor = Color.White;          // 선택해도 색 변화 없음
            colAllow.DefaultCellStyle.SelectionForeColor = Color.Black;
            _permGrid.Columns.Add(colMenu);
            _permGrid.Columns.Add(colAllow);

            // 그룹(메뉴) 단위 허용 토글: 클릭 시 그 그룹 기능 전체를 허용/차단으로 번갈아 적용.
            _permGrid.CellClick += PermGrid_GroupToggle;

            _permBar = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = Color.FromArgb(236, 239, 241) };
            _btnPermSave = new Button
            {
                Text = "권한 저장",
                Width = 130,
                Height = 28,
                Top = 5,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 128, 87),
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnPermSave.FlatAppearance.BorderSize = 0;
            _btnPermSave.Click += (s, e) => SavePerm();
            _permBar.Controls.Add(_btnPermSave);
            _permBar.Resize += (s, e) => { _btnPermSave.Left = _permBar.Width - _btnPermSave.Width - 10; };

            var title = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Text = "권한 설정 (이 계정 레벨)",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Color.FromArgb(224, 242, 241),   // 옅은 틸 — 계정관리 슬레이트 헤더와 구분
                ForeColor = Color.FromArgb(0, 105, 92),      // 진한 틸 글씨
                Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold)
            };

            _permHost = new Panel { Visible = false, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            _permHost.Controls.Add(_permGrid);   // Fill (가장 마지막에 남은 공간)
            _permHost.Controls.Add(_permBar);    // Bottom
            _permHost.Controls.Add(title);       // Top
            grid.Controls.Add(_permHost);
        }

        private void ToggleExpand(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return;
            var row = grid.Rows[rowIndex];
            if (IsDetail(row)) return;   // 디테일 행 클릭은 무시

            bool sameRow = (_expandedRow == rowIndex);
            string id = row.Cells["colId"].Value as string;

            Collapse();                 // 여기서 디테일 행 제거 → 인덱스가 바뀔 수 있음
            if (sameRow) return;        // 같은 행을 다시 클릭 → 접기만

            // Collapse 후 인덱스가 밀릴 수 있으므로 ID 로 다시 찾는다.
            int idx = FindAccountRow(id);
            if (idx >= 0) Expand(idx);
        }

        private int FindAccountRow(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                if (IsDetail(grid.Rows[i])) continue;
                if (string.Equals(grid.Rows[i].Cells["colId"].Value as string, id, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private void Expand(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return;
            string lvlStr = grid.Rows[rowIndex].Cells["colLevel"].Value as string;
            if (!Enum.TryParse<UserLevel>(lvlStr, out var lvl)) return;

            int detailIdx = rowIndex + 1;
            grid.Rows.Insert(detailIdx, 1);
            var drow = grid.Rows[detailIdx];
            drow.Tag = DetailTag;
            drow.DefaultCellStyle.BackColor = Color.FromArgb(236, 239, 241);

            _expandedRow = rowIndex;
            _expandedLevel = lvl;
            grid.Rows[rowIndex].Cells["colPerm"].Value = "접기 ▴";

            PopulatePerm(lvl);   // 행 채우고 저장버튼(편집 가능) 여부 결정

            // 저장 버튼이 없으면(읽기전용 레벨) 하단 바도 숨겨 빈 공간을 없앤다.
            _permBar.Visible = _btnPermSave.Visible;

            // 디테일 행(=패널) 높이를 내용에 딱 맞춘다(아래로 늘어지는 빈 공간 제거, 스크롤바 없이 전부 표시).
            int rowsH = _permGrid.ColumnHeadersHeight + 4;
            foreach (DataGridViewRow pr in _permGrid.Rows) rowsH += pr.Height + 1;   // +1 = 행 격자선
            drow.Height = 26 /*제목*/ + rowsH + (_permBar.Visible ? _permBar.Height : 0) + 6 /*보더 여백*/;

            _permHost.Visible = true;
            _permHost.BringToFront();
            RepositionDetail();
        }

        private void Collapse()
        {
            if (_expandedRow >= 0 && _expandedRow < grid.Rows.Count)
            {
                var arow = grid.Rows[_expandedRow];
                if (!IsDetail(arow) && grid.Columns.Contains("colPerm"))
                    arow.Cells["colPerm"].Value = "펼치기 ▾";

                int detailIdx = _expandedRow + 1;
                if (detailIdx < grid.Rows.Count && IsDetail(grid.Rows[detailIdx]))
                    grid.Rows.RemoveAt(detailIdx);
            }
            _expandedRow = -1;
            if (_permHost != null) _permHost.Visible = false;
        }

        // 디테일 행 위치에 _permHost 를 정확히 겹쳐 놓는다(스크롤/리사이즈 시 재배치).
        private void RepositionDetail()
        {
            if (_expandedRow < 0 || _permHost == null) return;
            int detailIdx = _expandedRow + 1;
            if (detailIdx >= grid.Rows.Count) { _permHost.Visible = false; return; }

            Rectangle rc = grid.GetRowDisplayRectangle(detailIdx, false);
            if (rc.Height <= 0) { _permHost.Visible = false; return; }   // 화면 밖으로 스크롤됨

            _permHost.Bounds = new Rectangle(rc.X + 2, rc.Y + 2, Math.Max(200, rc.Width - 4), rc.Height - 4);
            _permHost.Visible = true;
            _permHost.BringToFront();
        }

        // 그룹(메뉴) 단위로 한 줄씩만 보여준다. 체크 = 전체 허용, 빈칸 = 전체 차단, 중간(▣) = 일부만 허용.
        private void PopulatePerm(UserLevel level)
        {
            _permLoading = true;
            try
            {
                _permGrid.Rows.Clear();
                bool editable = CanEdit() && level != UserLevel.Admin && level != UserLevel.None;

                foreach (var grp in AccessPolicy.Catalog.GroupBy(f => AccessPolicy.GroupLabel(f.Group)))
                {
                    int total = grp.Count();
                    int allowed = grp.Count(f => AccessPolicy.IsAllowed(f.Key, level));
                    CheckState st = allowed == 0 ? CheckState.Unchecked
                                  : allowed == total ? CheckState.Checked
                                  : CheckState.Indeterminate;
                    int r = _permGrid.Rows.Add(grp.Key, st);
                    _permGrid.Rows[r].Tag = grp.Key;       // 그룹 라벨
                }
                _btnPermSave.Visible = editable;
            }
            finally { _permLoading = false; }
        }

        // 그룹 행의 허용 칸 클릭 → 그 그룹 기능 전체를 허용/차단으로 토글.
        private void PermGrid_GroupToggle(object sender, DataGridViewCellEventArgs e)
        {
            if (_permLoading || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_permGrid.Columns[e.ColumnIndex].Name != "allow") return;
            if (!CanEdit() || _expandedLevel == UserLevel.Admin || _expandedLevel == UserLevel.None) return;

            string groupLabel = _permGrid.Rows[e.RowIndex].Tag as string;
            if (string.IsNullOrEmpty(groupLabel)) return;

            var features = AccessPolicy.Catalog.Where(f => AccessPolicy.GroupLabel(f.Group) == groupLabel).ToList();
            bool currentlyAll = features.Count > 0 && features.All(f => AccessPolicy.IsAllowed(f.Key, _expandedLevel));
            bool target = !currentlyAll;   // 전체 허용 ↔ 전체 차단
            foreach (var f in features)
                AccessPolicy.SetAllowed(f.Key, _expandedLevel, target);

            _permGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = target ? CheckState.Checked : CheckState.Unchecked;
        }

        private void SavePerm()
        {
            if (!CanEdit()) return;
            try
            {
                _permGrid.EndEdit();
                AccessPolicy.Save();                          // Config\permissions.json + Changed
                try { AccessControl.Apply(FindForm()); } catch { }  // 전 화면 즉시 재적용
                QMC.Common.MessageDialog.Show(this, "권한이 저장되었습니다.", "USER", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "USER", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { UserSession.UserChanged -= OnUserChanged; } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}
