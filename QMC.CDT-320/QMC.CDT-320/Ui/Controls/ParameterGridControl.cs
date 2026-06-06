using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Controls
{
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
                    if (grid.CurrentRow == null)
                        return null;

                    return grid.CurrentRow.Tag as ParameterGridItem;
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "SelectedItem failed: " + ex.Message);
                    return null;
                }
                finally
                {
                }
            }
        }

        public ParameterGridControl()
        {
            try
            {
                InitializeComponent();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public void SetItems(IEnumerable<ParameterGridItem> items)
        {
            try
            {
                _items.Clear();
                if (items != null)
                    _items.AddRange(items);

                RebuildRows();
            }
            catch (Exception ex)
            {
                string message = "Parameter grid set failed: " + Name + Environment.NewLine + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", message);
                QMC.Common.MessageDialog.Show(this, message, "Parameter Grid", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
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
                    if (item == null)
                        continue;

                    SetValueCellText(row, item, FormatValue(item));
                    row.Cells[colUnit.Index].Value = item.Unit ?? string.Empty;
                    row.Cells[colScope.Index].Value = item.Scope.ToString();
                }
            }
            catch (Exception ex)
            {
                string message = "Parameter grid refresh failed: " + Name + Environment.NewLine + ex.Message;
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", message);
                QMC.Common.MessageDialog.Show(this, message, "Parameter Grid", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                grid.Rows.Clear();

                foreach (var item in _items)
                    AddParameterRow(item);

                ApplyScopeStyles();
            }
            catch
            {
                throw;
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
                if (item == null)
                    return;

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
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", message);
                QMC.Common.MessageDialog.Show(this, message, "Parameter Grid", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private DataGridViewCell CreateValueCell(ParameterGridItem item)
        {
            try
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

                var textCell = new DataGridViewTextBoxCell();
                return textCell;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void SetValueCellText(DataGridViewRow row, ParameterGridItem item, string text)
        {
            try
            {
                if (row == null)
                    return;

                var comboCell = row.Cells[colValue.Index] as DataGridViewComboBoxCell;
                if (comboCell != null && !comboCell.Items.Contains(text))
                    comboCell.Items.Add(text);

                row.Cells[colValue.Index].Value = text;
                ApplyValueStyle(row, item);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void ApplyValueStyle(DataGridViewRow row, ParameterGridItem item)
        {
            try
            {
                if (row == null || item == null)
                    return;

                DataGridViewCell cell = row.Cells[colValue.Index];
                cell.Style.BackColor = Color.White;
                cell.Style.ForeColor = Color.FromArgb(25, 29, 34);
                cell.Style.SelectionBackColor = Color.FromArgb(221, 235, 255);
                cell.Style.SelectionForeColor = Color.FromArgb(25, 29, 34);

                if (item.ValueType != ParameterGridValueType.Bool || item.Getter == null)
                    return;

                bool value = Convert.ToBoolean(item.Getter());
                if (!value)
                    return;

                cell.Style.BackColor = Color.FromArgb(219, 246, 226);
                cell.Style.ForeColor = Color.FromArgb(20, 115, 55);
                cell.Style.SelectionBackColor = Color.FromArgb(177, 231, 190);
                cell.Style.SelectionForeColor = Color.FromArgb(16, 86, 42);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "ApplyValueStyle failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ApplyScopeStyles()
        {
            try
            {
                foreach (DataGridViewRow row in grid.Rows)
                {
                    var item = row.Tag as ParameterGridItem;
                    if (item == null)
                        continue;

                    Color scopeColor = Color.FromArgb(241, 243, 246);
                    if (item.Scope == ParameterGridScope.Recipe)
                        scopeColor = Color.FromArgb(232, 240, 255);
                    else if (item.Scope == ParameterGridScope.Setup)
                        scopeColor = Color.FromArgb(238, 246, 240);

                    row.Cells[colScope.Index].Style.BackColor = scopeColor;
                    row.Cells[colScope.Index].Style.SelectionBackColor = Color.FromArgb(221, 235, 255);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private string FormatValue(ParameterGridItem item)
        {
            try
            {
                if (item == null || item.Getter == null)
                    return string.Empty;

                object raw = item.Getter();
                if (raw == null)
                    return string.Empty;

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
            finally
            {
            }
        }

        private string FormatSelectionValue(ParameterGridItem item, object raw)
        {
            try
            {
                if (item == null)
                    return string.Empty;

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
            finally
            {
            }
        }

        private object ParseValue(ParameterGridItem item, string text)
        {
            try
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
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void CommitValue(DataGridViewRow row, object value)
        {
            try
            {
                if (_isRefreshing || row == null)
                    return;

                var item = row.Tag as ParameterGridItem;
                if (item == null || item.Setter == null)
                    return;

                if (item.Validator != null && !item.Validator(value))
                    throw new InvalidOperationException(item.DisplayName + " value is out of range.");

                item.Setter(value);
                SetValueCellText(row, item, FormatValue(item));
                OnParameterValueChanged(item);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "CommitValue failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Parameter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshValues();
            }
            finally
            {
            }
        }

        private void CommitRow(DataGridViewRow row)
        {
            try
            {
                if (_isRefreshing || row == null)
                    return;

                var item = row.Tag as ParameterGridItem;
                if (item == null || item.Setter == null)
                    return;

                object value = ParseValue(item, Convert.ToString(row.Cells[colValue.Index].Value, CultureInfo.InvariantCulture));
                CommitValue(row, value);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "Commit failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Parameter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshValues();
            }
            finally
            {
            }
        }

        private void OnParameterValueChanged(ParameterGridItem item)
        {
            try
            {
                ParameterValueChanged?.Invoke(this, new ParameterGridChangedEventArgs(item));
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "ValueChanged failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void OnParameterRowDoubleClicked(ParameterGridItem item)
        {
            try
            {
                ParameterRowDoubleClicked?.Invoke(this, new ParameterGridChangedEventArgs(item));
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "RowDoubleClicked failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ToggleBoolRow(DataGridViewRow row)
        {
            try
            {
                if (row == null)
                    return;

                var item = row.Tag as ParameterGridItem;
                if (item == null || item.ValueType != ParameterGridValueType.Bool)
                    return;

                bool current = false;
                if (item.Getter != null)
                    current = Convert.ToBoolean(item.Getter());

                bool next = !current;
                string message = item.DisplayName + " 값을 " + (next ? "True" : "False") + "로 변경하시겠습니까?";
                DialogResult result = QMC.Common.MessageDialog.Show(this, message, "Parameter Change", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                    return;

                CommitValue(row, next);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "ToggleBool failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ShowNumericEditor(DataGridViewRow row)
        {
            try
            {
                if (row == null)
                    return;

                var item = row.Tag as ParameterGridItem;
                if (item == null || (item.ValueType != ParameterGridValueType.Double && item.ValueType != ParameterGridValueType.Int))
                    return;

                string currentText = FormatValue(item);
                using (var dialog = new NumericKeypadDialog(item.DisplayName, currentText, item.Unit))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return;

                    object value = ParseValue(item, dialog.ValueText);
                    CommitValue(row, value);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "Numeric editor failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Parameter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshValues();
            }
            finally
            {
            }
        }

        private void ShowSelectionEditor(DataGridViewRow row)
        {
            try
            {
                if (row == null)
                    return;

                var item = row.Tag as ParameterGridItem;
                if (item == null || item.ValueType != ParameterGridValueType.Selection)
                    return;

                grid.CurrentCell = row.Cells[colValue.Index];
                grid.BeginEdit(true);

                var editingCombo = grid.EditingControl as ComboBox;
                if (editingCombo != null)
                    editingCombo.DroppedDown = true;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "Selection editor failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex != colValue.Index)
                    return;

                var item = grid.Rows[e.RowIndex].Tag as ParameterGridItem;
                if (item == null)
                    return;

                if (item.ValueType == ParameterGridValueType.Bool)
                    ToggleBoolRow(grid.Rows[e.RowIndex]);
                else if (item.ValueType == ParameterGridValueType.Selection)
                    ShowSelectionEditor(grid.Rows[e.RowIndex]);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "CellClick failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0)
                    return;

                if (e.ColumnIndex == colValue.Index)
                    ShowNumericEditor(grid.Rows[e.RowIndex]);
                else
                    OnParameterRowDoubleClicked(grid.Rows[e.RowIndex].Tag as ParameterGridItem);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "DoubleClick failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex != colValue.Index)
                    return;

                var item = grid.Rows[e.RowIndex].Tag as ParameterGridItem;
                if (item != null && item.ValueType == ParameterGridValueType.Text)
                    CommitRow(grid.Rows[e.RowIndex]);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "CellEndEdit failed: " + ex.Message);
            }
            finally
            {
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
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "DirtyState failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (_isRefreshing || e.RowIndex < 0 || e.ColumnIndex != colValue.Index)
                    return;

                var item = grid.Rows[e.RowIndex].Tag as ParameterGridItem;
                if (item == null || item.ValueType != ParameterGridValueType.Selection)
                    return;

                object value = ParseValue(item, Convert.ToString(grid.Rows[e.RowIndex].Cells[colValue.Index].Value, CultureInfo.InvariantCulture));
                CommitValue(grid.Rows[e.RowIndex], value);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "CellValueChanged failed: " + ex.Message);
                RefreshValues();
            }
            finally
            {
            }
        }

        private void grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex != colScope.Index)
                    return;

                var item = grid.Rows[e.RowIndex].Tag as ParameterGridItem;
                if (item == null || e.Value == null)
                    return;

                e.Value = item.Scope.ToString().ToUpperInvariant();
                e.FormattingApplied = true;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void grid_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0)
                    return;

                grid.CurrentCell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                grid.Rows[e.RowIndex].Selected = true;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "MouseDown failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            try
            {
                e.ThrowException = false;
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}


