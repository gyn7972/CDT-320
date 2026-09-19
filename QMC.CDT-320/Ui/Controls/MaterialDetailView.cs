using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Controls
{
    public sealed partial class MaterialDetailView : UserControl, ILocalizedView
    {
        public event EventHandler<MaterialDetailEditEventArgs> EditRequested;
        public event EventHandler CreateDataRequested;
        public event EventHandler ClearDataRequested;
        public event EventHandler ClearAllDataRequested;
        public event EventHandler CreateProcessTestDataRequested;
        /// <summary>[검사 재실행 2026-08-27] Bottom/Side 검사 데이터 삭제 요청(픽커 헤드 화면 전용 버튼).</summary>
        public event EventHandler ClearInspectionDataRequested;

        // 직전에 표시한 내용의 signature. 동일하면 그리드 전체 재구성을 생략한다.
        private string _lastSignature;
        private string _displayTitle = "MATERIAL";

        public MaterialDetailView()
        {
            InitializeComponent();
            Lang.Bind(btnCreateData, "DATA CREATE");
            Lang.Bind(btnCreateProcessTestData, "PROCESS TEST DATA");
            Lang.Bind(btnClearData, "DATA CLEAR");
            Lang.Bind(btnClearAllData, "DATA ALL CLEAR");
            Lang.Bind(btnClearInspectionData, "BOTTOM/SIDE CLEAR");
            ApplyLanguage();
            Clear();
        }

        public void ApplyLanguage()
        {
            grpMaterialDetail.Text = Lang.Display(_displayTitle);
            colName.HeaderText = Lang.Display("Property");
            colValue.HeaderText = Lang.Display("Value");
            gridMaterial.Invalidate();
        }

        private void gridMaterial_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // 이름 열의 표시만 번역한다. 편집 이벤트에서 사용하는 원본 행과 Key/Value는 보존한다.
            if (e.RowIndex >= 0 && e.ColumnIndex == colName.Index && e.Value is string name)
            {
                e.Value = Lang.Display(name);
                e.FormattingApplied = true;
            }
        }

        public bool ShowProcessTestDataButton
        {
            get { return btnCreateProcessTestData != null && btnCreateProcessTestData.Visible; }
            set
            {
                if (btnCreateProcessTestData != null)
                    btnCreateProcessTestData.Visible = value;
            }
        }

        /// <summary>[검사 재실행 2026-08-27] BOTTOM/SIDE 검사 데이터 삭제 버튼 표시 여부(기본 false —
        /// 픽커 헤드 화면에서만 켠다).</summary>
        public bool ShowInspectionClearButton
        {
            get { return btnClearInspectionData != null && btnClearInspectionData.Visible; }
            set
            {
                if (btnClearInspectionData != null)
                    btnClearInspectionData.Visible = value;
            }
        }

        public void Clear()
        {
            SetRows("MATERIAL", new MaterialDetailRow[0]);
        }

        public void SetRows(string title, IEnumerable<MaterialDetailRow> rows)
        {
            string normalizedTitle = string.IsNullOrEmpty(title) ? "MATERIAL" : title;

            // 표시 내용이 직전과 같으면(주기적 Refresh 시 대부분) 전체 재구성을 생략한다.
            // → 매 Tick Rows.Clear/Add 로 인한 깜빡임과 CPU 부하 제거. (값이 바뀐 경우에만 재구성)
            List<MaterialDetailRow> rowList = null;
            if (rows != null)
            {
                rowList = new List<MaterialDetailRow>();
                foreach (var r in rows)
                {
                    if (r != null)
                        rowList.Add(r);
                }
            }

            string signature = BuildSignature(normalizedTitle, rowList);
            if (string.Equals(signature, _lastSignature, StringComparison.Ordinal))
                return;
            _lastSignature = signature;

            _displayTitle = normalizedTitle;
            grpMaterialDetail.Text = Lang.Display(_displayTitle);

            string selectedKey = "";
            string selectedName = "";
            int selectedColumn = 0;
            int firstDisplayedRow = -1;

            if (gridMaterial.CurrentCell != null)
            {
                selectedColumn = gridMaterial.CurrentCell.ColumnIndex;
                var currentRow = gridMaterial.Rows[gridMaterial.CurrentCell.RowIndex];
                var currentMaterialRow = currentRow.Tag as MaterialDetailRow;
                if (currentMaterialRow != null)
                {
                    selectedKey = currentMaterialRow.Key ?? "";
                    selectedName = currentMaterialRow.Name ?? "";
                }
            }

            try
            {
                firstDisplayedRow = gridMaterial.FirstDisplayedScrollingRowIndex;
            }
            catch
            {
                firstDisplayedRow = -1;
            }

            gridMaterial.Rows.Clear();

            if (rowList == null)
                return;

            foreach (var row in rowList)
            {
                if (row == null)
                    continue;

                int index = gridMaterial.Rows.Add(Normalize(row.Name), Normalize(row.Value));
                gridMaterial.Rows[index].Tag = row;
                gridMaterial.Rows[index].ReadOnly = true;
                gridMaterial.Rows[index].DefaultCellStyle.ForeColor = row.Editable
                    ? System.Drawing.Color.Black
                    : System.Drawing.Color.DimGray;
            }

            RestoreSelection(selectedKey, selectedName, selectedColumn, firstDisplayedRow);
        }

        private void gridMaterial_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= gridMaterial.Rows.Count)
                return;

            var row = gridMaterial.Rows[e.RowIndex].Tag as MaterialDetailRow;
            if (row == null || !row.Editable)
                return;

            var handler = EditRequested;
            if (handler != null)
                handler(this, new MaterialDetailEditEventArgs(row));
        }

        private void btnCreateData_Click(object sender, EventArgs e)
        {
            var handler = CreateDataRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void btnCreateProcessTestData_Click(object sender, EventArgs e)
        {
            var handler = CreateProcessTestDataRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void btnClearData_Click(object sender, EventArgs e)
        {
            var handler = ClearDataRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void btnClearAllData_Click(object sender, EventArgs e)
        {
            var handler = ClearAllDataRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void btnClearInspectionData_Click(object sender, EventArgs e)
        {
            var handler = ClearInspectionDataRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }

        /// <summary>제목 + 모든 행(Name/Value/Editable)으로 표시 내용 signature 를 만든다.</summary>
        private static string BuildSignature(string title, List<MaterialDetailRow> rows)
        {
            var sb = new StringBuilder();
            sb.Append(title ?? string.Empty).Append((char)1);
            if (rows != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    MaterialDetailRow row = rows[i];
                    if (row == null)
                        continue;
                    sb.Append(Normalize(row.Name)).Append((char)2)
                      .Append(Normalize(row.Value)).Append((char)2)
                      .Append(row.Editable ? '1' : '0').Append((char)3);
                }
            }
            return sb.ToString();
        }

        private void RestoreSelection(string key, string name, int columnIndex, int firstDisplayedRow)
        {
            if (gridMaterial.Rows.Count == 0)
                return;

            int targetRow = -1;
            for (int i = 0; i < gridMaterial.Rows.Count; i++)
            {
                var row = gridMaterial.Rows[i].Tag as MaterialDetailRow;
                if (row == null)
                    continue;

                bool keyMatch = !string.IsNullOrEmpty(key) && string.Equals(row.Key, key, StringComparison.Ordinal);
                bool nameMatch = string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(name) && string.Equals(row.Name, name, StringComparison.Ordinal);
                if (keyMatch || nameMatch)
                {
                    targetRow = i;
                    break;
                }
            }

            if (targetRow < 0)
            {
                gridMaterial.ClearSelection();
                return;
            }

            int targetColumn = columnIndex >= 0 && columnIndex < gridMaterial.Columns.Count ? columnIndex : 0;
            gridMaterial.ClearSelection();
            gridMaterial.CurrentCell = gridMaterial.Rows[targetRow].Cells[targetColumn];
            gridMaterial.Rows[targetRow].Selected = true;

            if (firstDisplayedRow >= 0 && firstDisplayedRow < gridMaterial.Rows.Count)
            {
                try
                {
                    gridMaterial.FirstDisplayedScrollingRowIndex = firstDisplayedRow;
                }
                catch
                {
                }
            }
        }
    }

    public sealed class MaterialDetailRow
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public bool Editable { get; set; }
    }

    public sealed class MaterialDetailEditEventArgs : EventArgs
    {
        public MaterialDetailEditEventArgs(MaterialDetailRow row)
        {
            Row = row;
        }

        public MaterialDetailRow Row { get; private set; }
    }
}
