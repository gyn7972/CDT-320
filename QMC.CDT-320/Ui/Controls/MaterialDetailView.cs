using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
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

    public sealed partial class MaterialDetailView : UserControl
    {
        public event EventHandler<MaterialDetailEditEventArgs> EditRequested;
        public event EventHandler CreateDataRequested;
        public event EventHandler ClearDataRequested;

        public MaterialDetailView()
        {
            InitializeComponent();
            Clear();
        }

        public void Clear()
        {
            SetRows("MATERIAL", new MaterialDetailRow[0]);
        }

        public void SetRows(string title, IEnumerable<MaterialDetailRow> rows)
        {
            grpMaterialDetail.Text = string.IsNullOrEmpty(title) ? "MATERIAL" : title;

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

            if (rows == null)
                return;

            foreach (var row in rows)
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

        private void btnClearData_Click(object sender, EventArgs e)
        {
            var handler = ClearDataRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrEmpty(value) ? "-" : value;
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
}
