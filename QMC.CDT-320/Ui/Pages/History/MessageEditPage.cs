using System;
using System.Collections.Generic;
using System.Windows.Forms;
using QMC.Common.Logging;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.History
{
    public partial class MessageEditPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        // 화면 편집 모델. 그리드는 이 목록의 뷰이고, 저장 시 이 목록을 카탈로그에 반영한다.
        private readonly List<MessageDefinition> _working = new List<MessageDefinition>();

        public MessageEditPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            WireEvents();
            if (!IsDesignerMode()) LoadFromCatalog();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("hist.msgEdit");
            lblHeader.Tag = "i18n:hist.msgEdit";
            btnAdd.Text = Lang.T("common.add");
            btnEdit.Text = Lang.T("common.update");
            btnSave.Text = Lang.T("common.save");
            btnDelete.Text = Lang.T("common.delete");
        }

        private void WireEvents()
        {
            btnAdd.Click += (s, e) => AddEntry();
            btnEdit.Click += (s, e) => EditSelected();
            btnSave.Click += (s, e) => SaveCatalog();
            btnDelete.Click += (s, e) => DeleteSelected();
            btnImport.Click += (s, e) => ImportFromEvents();
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditEntry(e.RowIndex); };
        }

        // 선택된 행을 수정 대화상자로 연다.
        private void EditSelected()
        {
            int idx = -1;
            if (grid.CurrentRow != null) idx = grid.CurrentRow.Index;
            else if (grid.SelectedRows.Count > 0) idx = grid.SelectedRows[0].Index;
            if (idx >= 0) EditEntry(idx);
        }

        private void LoadFromCatalog()
        {
            _working.Clear();
            foreach (var d in MessageCatalog.Items)
            {
                _working.Add(new MessageDefinition { Code = d.Code, Ko = d.Ko, En = d.En, Kind = d.Kind });
            }
            RefreshGrid();
        }

        // 컬럼 순서: CODE, KIND, DESCRIPTION (KO), DESCRIPTION (EN)
        private void RefreshGrid()
        {
            // 데이터가 많아도 빠르게 채우기 위해 행을 먼저 만들어 두고,
            // 레이아웃/오토사이즈를 멈춘 상태에서 AddRange 로 한 번에 추가한다(이벤트 페이지와 동일).
            var rows = new List<DataGridViewRow>(_working.Count);
            foreach (var d in _working)
            {
                var row = new DataGridViewRow();
                row.CreateCells(grid,
                    d.Code,
                    d.Kind.ToString(),
                    d.Ko ?? string.Empty,
                    (d.En ?? string.Empty).ToUpperInvariant());
                rows.Add(row);
            }

            var prevAutoSize = grid.AutoSizeColumnsMode;
            grid.SuspendLayout();
            try
            {
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                grid.Rows.Clear();
                if (rows.Count > 0) grid.Rows.AddRange(rows.ToArray());
            }
            finally
            {
                grid.AutoSizeColumnsMode = prevAutoSize;
                grid.ResumeLayout();
            }
        }

        private void AddEntry()
        {
            using (var dlg = new MessageEntryDialog())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Result == null) return;

                // 같은 코드가 있으면 갱신, 없으면 추가
                int idx = _working.FindIndex(x => string.Equals(x.Code, dlg.Result.Code, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) _working[idx] = dlg.Result;
                else _working.Add(dlg.Result);
                RefreshGrid();
            }
        }

        private void EditEntry(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _working.Count) return;

            using (var dlg = new MessageEntryDialog(_working[rowIndex]))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Result == null) return;
                _working[rowIndex] = dlg.Result;
                RefreshGrid();
            }
        }

        private void DeleteSelected()
        {
            int idx = -1;
            if (grid.CurrentRow != null) idx = grid.CurrentRow.Index;
            else if (grid.SelectedRows.Count > 0) idx = grid.SelectedRows[0].Index;

            if (idx < 0 || idx >= _working.Count) return;

            _working.RemoveAt(idx);
            RefreshGrid();
        }

        // 오늘 이벤트 로그를 가져온다. 코드가 같아도 디스크립션이 다르면 모두 추가하고,
        // 디스크립션이 중복일 때만 제외한다(중복 판단 기준 = 디스크립션).
        private void ImportFromEvents()
        {
            try
            {
                // 이미 목록에 있는 문구(KO/EN)를 중복 기준으로 모아둔다.
                var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                foreach (var d in _working)
                {
                    if (!string.IsNullOrWhiteSpace(d.Ko)) seen.Add(d.Ko.Trim());
                    if (!string.IsNullOrWhiteSpace(d.En)) seen.Add(d.En.Trim());
                }

                int added = 0;
                foreach (var r in EventLogger.Read(DateTime.Today))
                {
                    string desc = (r.Description ?? string.Empty).Trim();
                    if (desc.Length == 0) continue;
                    if (seen.Contains(desc)) continue;   // 디스크립션이 중복일 때만 제외
                    seen.Add(desc);

                    var def = new MessageDefinition { Code = r.Code ?? string.Empty, Kind = r.Kind };
                    // 디스크립션에 한글이 있으면 KO, 아니면 EN 으로 넣는다.
                    if (HasKorean(desc)) def.Ko = desc;
                    else def.En = desc;
                    _working.Add(def);
                    added++;
                }

                RefreshGrid();
                QMC.Common.MessageDialog.Show(added + "개 항목을 가져왔습니다. (디스크립션 중복 제외)" + Environment.NewLine + "저장하려면 저장버튼을 누르세요.",
                    "MESSAGE", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "MESSAGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // 문자열에 한글(완성형/자모)이 포함되어 있는지.
        private static bool HasKorean(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s)
            {
                if ((c >= '가' && c <= '힣') || (c >= 'ᄀ' && c <= 'ᇿ') || (c >= '㄰' && c <= '㆏'))
                    return true;
            }
            return false;
        }

        private void SaveCatalog()
        {
            try
            {
                MessageCatalog.ReplaceAll(_working);
                MessageCatalog.Save();
                QMC.Common.MessageDialog.Show("저장되었습니다.", "MESSAGE", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "MESSAGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
