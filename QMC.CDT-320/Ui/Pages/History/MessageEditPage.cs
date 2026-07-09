using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Logging;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.History
{
    /// <summary>
    /// 메시지 번역 페이지. 코드/KIND 는 자동 수집되는 고정 항목이고(읽기 전용),
    /// 사용자는 DESCRIPTION 의 한글(KO)/영문(EN) 번역만 그리드에서 직접 편집한다.
    /// <para>
    /// 행 목록은 <b>번역 카탈로그(message_catalog.csv)</b> 를 그대로 읽어 만든다. 실시간으로 발생하는
    /// 메시지 종류는 EventLogger 가 카탈로그에 자동 등록하므로, 더 이상 로그 폴더 전체를 훑지 않는다
    /// (로그가 커져도 화면이 즉시 열린다). SAVE 시 편집 결과를 카탈로그로 기록한다.
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
            if (!IsDesignerMode()) LoadFromCatalog(false);
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("hist.msgEdit");
            lblHeader.Tag = "i18n:hist.msgEdit";
            BackColor = Color.White;
            rootLayout.BackColor = Color.White;
            actionLayout.BackColor = Color.White;
            grid.BackgroundColor = Color.White;
        }

        private void WireEvents()
        {
            btnSave.Click += (s, e) => SaveCatalog();
            btnImport.Click += (s, e) => LoadFromCatalog(true);   // REFRESH
            // KO/EN 셀 인라인 편집 결과를 편집 모델에 반영한다.
            grid.CellEndEdit += Grid_CellEndEdit;
            // KO/EN 셀을 더블클릭하면 긴 번역문을 큰 창에서 보고 편집한다(인라인 편집 대체).
            grid.CellDoubleClick += Grid_CellDoubleClick;
        }

        // KO/EN 셀 더블클릭 시 큰 창에서 전체 번역문을 보고 편집한다. 확인하면 셀과 편집 모델에 반영한다.
        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

                string colName = grid.Columns[e.ColumnIndex].Name;
                if (colName != "KO" && colName != "EN") return;   // 편집 대상(번역) 컬럼만

                var def = grid.Rows[e.RowIndex].Tag as MessageDefinition;
                if (def == null) return;

                // 더블클릭으로 시작된 인라인 편집을 취소하고 큰 창으로 대체한다.
                grid.CancelEdit();

                var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                string current = (cell.Value as string) ?? string.Empty;
                string title = colName == "KO" ? "DESCRIPTION (KO)" : "DESCRIPTION (EN)";

                using (var dlg = new QMC.CDT_320.Ui.Dialogs.TextViewerDialog(title, current, true))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;

                    string edited = dlg.TextValue ?? string.Empty;
                    if (colName == "EN") edited = edited.ToUpperInvariant();   // 영문은 항상 대문자
                    cell.Value = edited;
                    if (colName == "KO") def.Ko = edited; else def.En = edited;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "MESSAGE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // 번역 카탈로그(message_catalog.csv)를 화면 편집 모델로 복제한다. 실시간 발생 메시지는
        // EventLogger 가 카탈로그에 자동 등록하므로 여기서 로그 폴더를 훑지 않는다(즉시 로드).
        // 원본 객체를 직접 편집하지 않도록 새 인스턴스로 복사한다.
        private void LoadFromCatalog(bool announce)
        {
            try
            {
                _working.Clear();
                foreach (var s in MessageCatalog.Items)
                {
                    _working.Add(new MessageDefinition
                    {
                        Code = s.Code,
                        Kind = s.Kind,
                        Ko   = s.Ko,
                        En   = s.En
                    });
                }

                RefreshGrid();

                if (announce)
                    QMC.Common.MessageDialog.Show(_working.Count + "건을 불러왔습니다." + Environment.NewLine + "번역(KO/EN)을 입력한 뒤 SAVE 를 누르세요.",
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
