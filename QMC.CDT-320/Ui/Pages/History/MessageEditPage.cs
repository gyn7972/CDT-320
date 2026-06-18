using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using QMC.Common.Logging;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.History
{
    /// <summary>
    /// 메시지 번역 페이지. 코드/KIND 는 로그에서 수집되는 고정 항목이고(읽기 전용),
    /// 사용자는 DESCRIPTION 의 한글(KO)/영문(EN) 번역만 그리드에서 직접 편집한다.
    /// <para>
    /// 행 목록은 <b>D:\CDT-320\Log\Event 폴더의 모든 CSV</b> 를 훑어 디스크립션 중복을 제외해 만든다.
    /// 이미 저장된 번역(message_catalog.csv)은 디스크립션으로 매칭해 보존하고, SAVE 시 카탈로그로 기록한다.
    /// </para>
    /// </summary>
    public partial class MessageEditPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        // 화면 편집 모델. 그리드는 이 목록의 뷰이고, 저장 시 이 목록을 카탈로그에 반영한다.
        private readonly List<MessageDefinition> _working = new List<MessageDefinition>();

        public MessageEditPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            WireEvents();
            if (!IsDesignerMode()) LoadFromLogs(false);
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("hist.msgEdit");
            lblHeader.Tag = "i18n:hist.msgEdit";
        }

        private void WireEvents()
        {
            btnSave.Click += (s, e) => SaveCatalog();
            btnImport.Click += (s, e) => LoadFromLogs(true);   // REFRESH
            // KO/EN 셀 인라인 편집 결과를 편집 모델에 반영한다.
            grid.CellEndEdit += Grid_CellEndEdit;
        }

        // D:\CDT-320\Log\Event 폴더의 모든 CSV 를 훑어 디스크립션 중복을 제외하고 행을 구성한다.
        // 한글이면 KO, 아니면 EN 에 원문을 넣고, 저장된 카탈로그 번역이 있으면 반대 언어를 채워 보존한다.
        private void LoadFromLogs(bool announce)
        {
            try
            {
                // 저장된 번역을 디스크립션(KO/EN) → 항목으로 색인해 둔다.
                var savedByDesc = new Dictionary<string, MessageDefinition>(StringComparer.Ordinal);
                foreach (var s in MessageCatalog.Items)
                {
                    if (!string.IsNullOrWhiteSpace(s.Ko)) savedByDesc[s.Ko.Trim()] = s;
                    if (!string.IsNullOrWhiteSpace(s.En)) savedByDesc[s.En.Trim()] = s;
                }

                _working.Clear();
                var seen = new HashSet<string>(StringComparer.Ordinal);

                string dir = EventLogger.LogDir;
                if (Directory.Exists(dir))
                {
                    // 파일명(날짜) 순으로 정렬해 과거 → 최신 순으로 수집한다.
                    var files = Directory.GetFiles(dir, "*.csv");
                    Array.Sort(files, StringComparer.OrdinalIgnoreCase);

                    foreach (var path in files)
                    {
                        foreach (var r in EventLogger.ReadFile(path))
                        {
                            string desc = (r.Description ?? string.Empty).Trim();
                            if (desc.Length == 0) continue;
                            if (!seen.Add(desc)) continue;   // 디스크립션 중복 제외

                            var def = new MessageDefinition { Code = r.Code ?? string.Empty, Kind = r.Kind };
                            if (HasKorean(desc)) def.Ko = desc; else def.En = desc;

                            // 저장된 번역이 있으면 비어 있는 언어를 채운다(번역 보존).
                            MessageDefinition saved;
                            if (savedByDesc.TryGetValue(desc, out saved))
                            {
                                if (string.IsNullOrEmpty(def.Ko)) def.Ko = saved.Ko;
                                if (string.IsNullOrEmpty(def.En)) def.En = saved.En;
                            }
                            _working.Add(def);
                        }
                    }
                }

                RefreshGrid();

                if (announce)
                    QMC.Common.MessageDialog.Show(_working.Count + "건을 불러왔습니다. (디스크립션 중복 제외)" + Environment.NewLine + "번역(KO/EN)을 입력한 뒤 SAVE 를 누르세요.",
                        "MESSAGE", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "MESSAGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
                row.Tag = d;   // 정렬돼도 행↔모델 매핑이 유지되도록 모델 참조를 보관
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

        // KO/EN 셀 편집 종료 시 편집 모델에 반영(코드/KIND 는 읽기전용이라 들어오지 않음).
        private void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // 정렬 시 행 인덱스 != 모델 인덱스가 되므로 행에 보관한 모델 참조를 사용한다.
            var def = grid.Rows[e.RowIndex].Tag as MessageDefinition;
            if (def == null) return;

            var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            string value = (cell.Value as string) ?? string.Empty;
            string colName = grid.Columns[e.ColumnIndex].Name;

            if (colName == "KO")
            {
                def.Ko = value;
            }
            else if (colName == "EN")
            {
                value = value.ToUpperInvariant();   // 영문은 항상 대문자
                cell.Value = value;                 // 그리드 표시도 대문자로 정리
                def.En = value;
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
                // 편집 중인 셀이 있으면 먼저 커밋한다.
                grid.EndEdit();
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
