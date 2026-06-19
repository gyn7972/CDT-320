using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    // R1 — Handler(QMC.CDT_320.Ui.Controls.ParameterGridControl) public API 1:1 미러.
    // 동적 행 채움(SetItems/RebuildRows)은 런타임. Designer IC 엔 정적 grid+4컬럼만.
    // 의존성 치환: EventLogger.Write → Debug.WriteLine, QMC.Common.MessageDialog → MessageBox.
    public partial class ParameterGridControl : UserControl
    {
        private readonly List<ParameterGridItem> _items = new List<ParameterGridItem>();
        private bool _isRefreshing;

        public event EventHandler<ParameterGridChangedEventArgs> ParameterValueChanged;
        public event EventHandler<ParameterGridChangedEventArgs> ParameterRowDoubleClicked;

        public ParameterGridItem SelectedItem
        {
            get
            {
                try
                {
                    if (grid.CurrentRow == null) return null;
                    return grid.CurrentRow.Tag as ParameterGridItem;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[PARAM-GRID] SelectedItem failed: " + ex.Message);
                    return null;
                }
            }
        }

        /// <summary>현재 행을 모두 표시하는 데 필요한 높이(헤더 + 전체 행). 호스트가 행 높이 고정에 사용.</summary>
        public int PreferredGridHeight
        {
            get
            {
                int h = grid.ColumnHeadersVisible ? grid.ColumnHeadersHeight : 0;
                foreach (System.Windows.Forms.DataGridViewRow r in grid.Rows) h += r.Height;
                return h + 6;
            }
        }

        public ParameterGridControl()
        {
            InitializeComponent();
        }

        public void SetItems(IEnumerable<ParameterGridItem> items)
        {
            try
            {
                _items.Clear();
                if (items != null) _items.AddRange(items);
                RebuildRows();
            }
            catch (Exception ex)
            {
                string message = "Parameter grid set failed: " + Name + Environment.NewLine + ex.Message;
                Debug.WriteLine("[PARAM-GRID] " + message);
                MessageBox.Show(this, message, "Parameter Grid", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void RefreshValues()
        {
            try
            {
                _isRefreshing = true;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    var item = row.Tag as ParameterGridItem;
                    if (item == null) continue;

                    SetValueCellText(row, item, FormatValue(item));
                    row.Cells[colUnit.Index].Value = item.Unit ?? string.Empty;
                    row.Cells[colScope.Index].Value = item.Scope.ToString();
                }
            }
            catch (Exception ex)
            {
                string message = "Parameter grid refresh failed: " + Name + Environment.NewLine + ex.Message;
                Debug.WriteLine("[PARAM-GRID] " + message);
                MessageBox.Show(this, message, "Parameter Grid", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private void RebuildRows()
        {
            try
            {
                _isRefreshing = true;
                // 편집(콤보 드롭다운 등) 중에 Rows.Clear() 시 예외 → 재빌드 중단. 먼저 편집모드 해제.
                try { grid.EndEdit(); grid.CurrentCell = null; } catch { }
                grid.Rows.Clear();

                foreach (var item in _items)
                    AddParameterRow(item);

                ApplyScopeStyles();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private void AddParameterRow(ParameterGridItem item)
        {
            int index = -1;
            try
            {
                if (item == null) return;

                index = grid.Rows.Add();
                DataGridViewRow row = grid.Rows[index];
                row.Tag = item;
                row.Cells[colValue.Index] = CreateValueCell(item);
                row.Cells[colName.Index].ReadOnly = true;
                row.Cells[colValue.Index].ReadOnly = item.ValueType != ParameterGridValueType.Selection;
                row.Cells[colScope.Index].ReadOnly = true;
                row.Cells[colUnit.Index].ReadOnly = true;
                row.Cells[colName.Index].Value = item.DisplayName;
                row.Cells[colUnit.Index].Value = item.Unit ?? string.Empty;
                row.Cells[colScope.Index].Value = item.Scope.ToString();
                SetValueCellText(row, item, FormatValue(item));
            }
            catch (Exception ex)
            {
                if (index >= 0 && index < grid.Rows.Count)
                    grid.Rows.RemoveAt(index);

                string name = item != null ? item.DisplayName : string.Empty;
                string message = "Parameter row add failed: " + name + Environment.NewLine + ex.Message;
                Debug.WriteLine("[PARAM-GRID] " + message);
                MessageBox.Show(this, message, "Parameter Grid", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataGridViewCell CreateValueCell(ParameterGridItem item)
        {
            if (item != null && item.ValueType == ParameterGridValueType.Selection)
            {
                var cell = new DataGridViewComboBoxCell();
                cell.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
                cell.FlatStyle = FlatStyle.Flat;
                foreach (var option in item.Options)
                    cell.Items.Add(option.Text);
                return cell;
            }

            if (item != null && item.ValueType == ParameterGridValueType.Action)
            {
                var btn = new DataGridViewButtonCell();
                btn.FlatStyle = FlatStyle.Flat;
                btn.UseColumnTextForButtonValue = false;
                return btn;
            }

            return new DataGridViewTextBoxCell();
        }

        private void BrowseFolderRow(DataGridViewRow row)
        {
            try
            {
                var item = row != null ? row.Tag as ParameterGridItem : null;
                if (item == null) return;
                using (var d = new FolderBrowserDialog())
                {
                    string cur = item.Getter != null ? Convert.ToString(item.Getter()) : string.Empty;
                    if (!string.IsNullOrWhiteSpace(cur) && System.IO.Directory.Exists(cur)) d.SelectedPath = cur;
                    if (d.ShowDialog(this) == DialogResult.OK) CommitValue(row, d.SelectedPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PARAM-GRID] BrowseFolder failed: " + ex.Message);
            }
        }

        private void BrowseFileRow(DataGridViewRow row)
        {
            try
            {
                var item = row != null ? row.Tag as ParameterGridItem : null;
                if (item == null) return;
                using (var d = new OpenFileDialog())
                {
                    d.Filter = string.IsNullOrEmpty(item.FileFilter) ? "모든 파일 (*.*)|*.*" : item.FileFilter;
                    d.Title = item.DisplayName;
                    string cur = item.Getter != null ? Convert.ToString(item.Getter()) : string.Empty;
                    if (!string.IsNullOrWhiteSpace(cur))
                    {
                        try
                        {
                            string dir = System.IO.Path.GetDirectoryName(cur);
                            if (!string.IsNullOrEmpty(dir) && System.IO.Directory.Exists(dir)) d.InitialDirectory = dir;
                            if (System.IO.File.Exists(cur)) d.FileName = System.IO.Path.GetFileName(cur);
                        }
                        catch { }
                    }
                    if (d.ShowDialog(this) == DialogResult.OK) CommitValue(row, d.FileName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PARAM-GRID] BrowseFile failed: " + ex.Message);
            }
        }

        private void ShowSliderRow(DataGridViewRow row)
        {
            try
            {
                var item = row != null ? row.Tag as ParameterGridItem : null;
                if (item == null) return;

                int cur = 0;
                try { cur = Convert.ToInt32(item.Getter()); } catch { }
                int min = item.SliderMin;
                int max = item.SliderMax < min ? min : item.SliderMax;
                if (cur < min) cur = min;
                if (cur > max) cur = max;

                using (var dlg = new Form
                {
                    Text = item.DisplayName,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    ClientSize = new Size(320, 120),
                    MinimizeBox = false,
                    MaximizeBox = false,
                    ShowInTaskbar = false
                })
                {
                    var bar = new TrackBar { Minimum = min, Maximum = max, Value = cur, TickStyle = TickStyle.None, Left = 10, Top = 14, Width = 300 };
                    var lbl = new Label { Left = 10, Top = 56, Width = 300, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Consolas", 11F), Text = cur.ToString() };
                    bar.ValueChanged += (s, ev) => lbl.Text = bar.Value.ToString();
                    var ok = new Button { Text = "확인", DialogResult = DialogResult.OK, Left = 150, Top = 84, Width = 74 };
                    var cancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel, Left = 234, Top = 84, Width = 74 };
                    dlg.Controls.Add(bar); dlg.Controls.Add(lbl); dlg.Controls.Add(ok); dlg.Controls.Add(cancel);
                    dlg.AcceptButton = ok; dlg.CancelButton = cancel;
                    if (dlg.ShowDialog(this) == DialogResult.OK) CommitValue(row, bar.Value);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PARAM-GRID] Slider failed: " + ex.Message);
            }
        }

        private void SetValueCellText(DataGridViewRow row, ParameterGridItem item, string text)
        {
            if (row == null) return;

            var comboCell = row.Cells[colValue.Index] as DataGridViewComboBoxCell;
            if (comboCell != null && !comboCell.Items.Contains(text))
                comboCell.Items.Add(text);

            row.Cells[colValue.Index].Value = text;
            ApplyValueStyle(row, item);
        }

        private void ApplyValueStyle(DataGridViewRow row, ParameterGridItem item)
        {
            try
            {
                if (row == null || item == null) return;

                DataGridViewCell cell = row.Cells[colValue.Index];
                cell.Style.BackColor = Color.White;
                cell.Style.ForeColor = Color.FromArgb(25, 29, 34);
                cell.Style.SelectionBackColor = Color.FromArgb(221, 235, 255);
                cell.Style.SelectionForeColor = Color.FromArgb(25, 29, 34);

                if (item.ValueType != ParameterGridValueType.Bool || item.Getter == null)
                    return;

                bool value = Convert.ToBoolean(item.Getter());
                if (!value) return;

                cell.Style.BackColor = Color.FromArgb(219, 246, 226);
                cell.Style.ForeColor = Color.FromArgb(20, 115, 55);
                cell.Style.SelectionBackColor = Color.FromArgb(177, 231, 190);
                cell.Style.SelectionForeColor = Color.FromArgb(16, 86, 42);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PARAM-GRID] ApplyValueStyle failed: " + ex.Message);
            }
        }

        private void ApplyScopeStyles()
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                var item = row.Tag as ParameterGridItem;
                if (item == null) continue;

                Color scopeColor = Color.FromArgb(241, 243, 246);
                if (item.Scope == ParameterGridScope.Recipe)
                    scopeColor = Color.FromArgb(232, 240, 255);
                else if (item.Scope == ParameterGridScope.Setup)
                    scopeColor = Color.FromArgb(238, 246, 240);

                row.Cells[colScope.Index].Style.BackColor = scopeColor;
                row.Cells[colScope.Index].Style.SelectionBackColor = Color.FromArgb(221, 235, 255);
            }
        }

        private string FormatValue(ParameterGridItem item)
        {
            try
            {
                if (item == null || item.Getter == null) return string.Empty;

                object raw = item.Getter();
                if (raw == null) return string.Empty;

                if (item.ValueType == ParameterGridValueType.Bool)
                    return Convert.ToBoolean(raw) ? "True" : "False";

                if (item.ValueType == ParameterGridValueType.Selection)
                    return FormatSelectionValue(item, raw);

                if (item.ValueType == ParameterGridValueType.Int)
                    return Convert.ToInt32(raw).ToString(CultureInfo.InvariantCulture);

                if (item.ValueType == ParameterGridValueType.Double)
                {
                    double value = Convert.ToDouble(raw, CultureInfo.InvariantCulture) * item.DisplayScale;
                    return value.ToString("0.###", CultureInfo.InvariantCulture);
                }

                return Convert.ToString(raw, CultureInfo.InvariantCulture);
            }
            catch
            {
                return string.Empty;
            }
        }

        private string FormatSelectionValue(ParameterGridItem item, object raw)
        {
            try
            {
                if (item == null) return string.Empty;

                foreach (var option in item.Options)
                {
                    if (object.Equals(option.Value, raw))
                        return option.Text;

                    if (option.Value != null && raw != null &&
                        string.Equals(Convert.ToString(option.Value, CultureInfo.InvariantCulture), Convert.ToString(raw, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
                        return option.Text;
                }

                return Convert.ToString(raw, CultureInfo.InvariantCulture);
            }
            catch
            {
                return string.Empty;
            }
        }

        private object ParseValue(ParameterGridItem item, string text)
        {
            text = (text ?? string.Empty).Trim();
            if (item.ValueType == ParameterGridValueType.Bool)
            {
                string normalized = text.ToLowerInvariant();
                if (normalized == "true" || normalized == "1" || normalized == "on" || normalized == "yes" || normalized == "y")
                    return true;
                if (normalized == "false" || normalized == "0" || normalized == "off" || normalized == "no" || normalized == "n")
                    return false;
                throw new FormatException("Boolean value is invalid.");
            }

            if (item.ValueType == ParameterGridValueType.Int)
            {
                int intValue;
                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue) &&
                    !int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out intValue))
                    throw new FormatException("Integer value is invalid.");
                return intValue;
            }

            if (item.ValueType == ParameterGridValueType.Double)
            {
                double doubleValue;
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue) &&
                    !double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out doubleValue))
                    throw new FormatException("Number value is invalid.");
                return doubleValue;
            }

            if (item.ValueType == ParameterGridValueType.Selection)
            {
                foreach (var option in item.Options)
                {
                    if (string.Equals(option.Text, text, StringComparison.OrdinalIgnoreCase))
                        return option.Value;
                }
                throw new FormatException("Selection value is invalid.");
            }

            return text;
        }

        private void CommitValue(DataGridViewRow row, object value)
        {
            try
            {
                if (_isRefreshing || row == null) return;

                var item = row.Tag as ParameterGridItem;
                if (item == null || item.Setter == null) return;

                if (item.Validator != null && !item.Validator(value))
                    throw new InvalidOperationException(item.DisplayName + " value is out of range.");

                item.Setter(value);
                SetValueCellText(row, item, FormatValue(item));
                OnParameterValueChanged(item);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PARAM-GRID] CommitValue failed: " + ex.Message);
                MessageBox.Show(this, ex.Message, "Parameter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshValues();
            }
        }

        private void CommitRow(DataGridViewRow row)
        {
            try
            {
                if (_isRefreshing || row == null) return;

                var item = row.Tag as ParameterGridItem;
                if (item == null || item.Setter == null) return;

                object value = ParseValue(item, Convert.ToString(row.Cells[colValue.Index].Value, CultureInfo.InvariantCulture));
                CommitValue(row, value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PARAM-GRID] Commit failed: " + ex.Message);
                MessageBox.Show(this, ex.Message, "Parameter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshValues();
            }
        }

        private void OnParameterValueChanged(ParameterGridItem item)
        {
            try { ParameterValueChanged?.Invoke(this, new ParameterGridChangedEventArgs(item)); }
            catch (Exception ex) { Debug.WriteLine("[PARAM-GRID] ValueChanged failed: " + ex.Message); }
        }

        private void OnParameterRowDoubleClicked(ParameterGridItem item)
        {
            try { ParameterRowDoubleClicked?.Invoke(this, new ParameterGridChangedEventArgs(item)); }
            catch (Exception ex) { Debug.WriteLine("[PARAM-GRID] RowDoubleClicked failed: " + ex.Message); }
        }

        private void ToggleBoolRow(DataGridViewRow row)
        {
            try
            {
                if (row == null) return;

                var item = row.Tag as ParameterGridItem;
                if (item == null || item.ValueType != ParameterGridValueType.Bool) return;

                bool current = false;
                if (item.Getter != null)
                    current = Convert.ToBoolean(item.Getter());

                bool next = !current;
                string message = item.DisplayName + " 값을 " + (next ? "True" : "False") + "로 변경하시겠습니까?";
                DialogResult result = MessageBox.Show(this, message, "Parameter Change", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;

                CommitValue(row, next);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PARAM-GRID] ToggleBool failed: " + ex.Message);
            }
        }

        private void ShowNumericEditor(DataGridViewRow row)
        {
            try
            {
                if (row == null) return;

                var item = row.Tag as ParameterGridItem;
                if (item == null || (item.ValueType != ParameterGridValueType.Double && item.ValueType != ParameterGridValueType.Int))
                    return;

                string currentText = FormatValue(item);
                using (var dialog = new NumericKeypadDialog(item.DisplayName, currentText, item.Unit))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;

                    object value = ParseValue(item, dialog.ValueText);
                    CommitValue(row, value);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PARAM-GRID] Numeric editor failed: " + ex.Message);
                MessageBox.Show(this, ex.Message, "Parameter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshValues();
            }
        }

        private void ShowSelectionEditor(DataGridViewRow row)
        {
            try
            {
                if (row == null) return;

                var item = row.Tag as ParameterGridItem;
                if (item == null || item.ValueType != ParameterGridValueType.Selection) return;

                grid.CurrentCell = row.Cells[colValue.Index];
                grid.BeginEdit(true);

                var editingCombo = grid.EditingControl as ComboBox;
                if (editingCombo != null)
                    editingCombo.DroppedDown = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PARAM-GRID] Selection editor failed: " + ex.Message);
            }
        }

        private void grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex != colValue.Index) return;

                var item = grid.Rows[e.RowIndex].Tag as ParameterGridItem;
                if (item == null) return;

                if (item.ValueType == ParameterGridValueType.Bool)
                    ToggleBoolRow(grid.Rows[e.RowIndex]);
                else if (item.ValueType == ParameterGridValueType.Selection)
                    ShowSelectionEditor(grid.Rows[e.RowIndex]);
                else if (item.ValueType == ParameterGridValueType.Action)
                    item.OnAction?.Invoke();
                else if (item.ValueType == ParameterGridValueType.FolderPath)
                    BrowseFolderRow(grid.Rows[e.RowIndex]);
                else if (item.ValueType == ParameterGridValueType.FilePath)
                    BrowseFileRow(grid.Rows[e.RowIndex]);
                else if (item.ValueType == ParameterGridValueType.Slider)
                    ShowSliderRow(grid.Rows[e.RowIndex]);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PARAM-GRID] CellClick failed: " + ex.Message);
            }
        }

        private void grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return;

                if (e.ColumnIndex == colValue.Index)
                    ShowNumericEditor(grid.Rows[e.RowIndex]);
                else
                    OnParameterRowDoubleClicked(grid.Rows[e.RowIndex].Tag as ParameterGridItem);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PARAM-GRID] DoubleClick failed: " + ex.Message);
            }
        }

        private void grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex != colValue.Index) return;

                var item = grid.Rows[e.RowIndex].Tag as ParameterGridItem;
                if (item != null && item.ValueType == ParameterGridValueType.Text)
                    CommitRow(grid.Rows[e.RowIndex]);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PARAM-GRID] CellEndEdit failed: " + ex.Message);
            }
        }

        private void grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            try
            {
                if (grid.IsCurrentCellDirty)
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PARAM-GRID] DirtyState failed: " + ex.Message);
            }
        }

        private void grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (_isRefreshing || e.RowIndex < 0 || e.ColumnIndex != colValue.Index) return;

                var item = grid.Rows[e.RowIndex].Tag as ParameterGridItem;
                if (item == null || item.ValueType != ParameterGridValueType.Selection) return;

                object value = ParseValue(item, Convert.ToString(grid.Rows[e.RowIndex].Cells[colValue.Index].Value, CultureInfo.InvariantCulture));
                CommitValue(grid.Rows[e.RowIndex], value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PARAM-GRID] CellValueChanged failed: " + ex.Message);
                RefreshValues();
            }
        }

        private void grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex != colScope.Index) return;

                var item = grid.Rows[e.RowIndex].Tag as ParameterGridItem;
                if (item == null || e.Value == null) return;

                e.Value = item.Scope.ToString().ToUpperInvariant();
                e.FormattingApplied = true;
            }
            catch
            {
            }
        }

        private void grid_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

                grid.CurrentCell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                grid.Rows[e.RowIndex].Selected = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PARAM-GRID] MouseDown failed: " + ex.Message);
            }
        }

        private void grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }
    }
}
