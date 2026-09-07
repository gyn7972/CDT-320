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
        private readonly HashSet<string> _collapsedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _isRefreshing;
        private ParameterGridItem _descriptionItem;
        private string _descriptionText;
        private bool _showParameterDescriptions;

        /// <summary>주기적인 값 갱신과 독립적으로 행 설명을 표시한다. 필요한 화면에서만 활성화한다.</summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool ShowParameterDescriptions
        {
            get { return _showParameterDescriptions; }
            set
            {
                _showParameterDescriptions = value;
                if (!value)
                    HideParameterDescription();
            }
        }

        public event EventHandler<ParameterGridChangedEventArgs> ParameterValueChanged;
        public event EventHandler<ParameterGridChangedEventArgs> ParameterRowDoubleClicked;

        /// <summary>티칭 포지션 행의 MOVE 버튼 클릭 시 발생. 구독 페이지가 기존 이동 메서드(가드 경유)를 그대로 호출한다.</summary>
        public event EventHandler<ParameterGridChangedEventArgs> ParameterMoveRequested;

        /// <summary>티칭 포지션 행의 TEACH 버튼 클릭 시 발생. 구독 페이지가 기존 티칭 메서드를 그대로 호출한다.</summary>
        public event EventHandler<ParameterGridChangedEventArgs> ParameterTeachRequested;

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

        /// <summary>그리드 스크롤바 표시 방식. 항목 수가 고정인 그리드(WAIT TIME 등)는 None으로 두면
        /// 경계 픽셀 차이로 인한 불필요한 스크롤바를 막을 수 있다.</summary>
        public ScrollBars GridScrollBars
        {
            get { return grid.ScrollBars; }
            set { grid.ScrollBars = value; }
        }

        private bool _autoFitParentGroupHeight;

        /// <summary>true면 SetItems 후 부모 GroupBox 높이를 그리드 내용(표시 행 전체)에 맞춰 자동 조정하고
        /// 그리드 스크롤바를 끈다. 높이가 항상 내용에 맞춰지므로 스크롤 없이 전 항목이 보인다.
        /// 항목 수가 고정인 그룹(WAIT TIME 등) 전용.</summary>
        public bool AutoFitParentGroupHeight
        {
            get { return _autoFitParentGroupHeight; }
            set
            {
                _autoFitParentGroupHeight = value;
                if (value)
                    grid.ScrollBars = ScrollBars.None;   // 높이 자동맞춤이 보장되므로 경계 픽셀로 인한 스크롤바 잔상 제거
            }
        }

        private void FitParentGroupHeight()
        {
            try
            {
                if (!AutoFitParentGroupHeight)
                    return;

                GroupBox group = Parent as GroupBox;
                if (group == null || !IsHandleCreated)
                    return;

                int contentHeight = grid.ColumnHeadersHeight + grid.Rows.GetRowsHeight(DataGridViewElementStates.Visible) + 6;
                int chrome = group.Height - Height;   // 그룹 헤더 + 패딩 (현재 레이아웃 기준 실측)
                if (chrome < 0)
                    chrome = 24;
                group.Height = contentHeight + chrome;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "PARAM-GRID", "FitParentGroupHeight failed: " + ex.Message);
            }
            finally
            {
            }
        }

        public ParameterGridControl()
        {
            try
            {
                InitializeComponent();
                grid.ShowCellToolTips = true;
                grid.Paint += Grid_ActionHeaderPaint;
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

                // 처음 표시할 때는 모든 접이식 그룹을 접은 상태로 시작
                _collapsedGroups.Clear();
                foreach (var headerItem in _items)
                {
                    if (headerItem != null && headerItem.IsGroupHeader && !string.IsNullOrEmpty(headerItem.GroupKey))
                        _collapsedGroups.Add(headerItem.GroupKey);
                }

                RebuildRows();
                FitParentGroupHeight();
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
                    if (item == null || item.IsGroupHeader)
                        continue;

                    SetValueCellText(row, item, FormatValue(item));
                    row.Cells[colUnit.Index].Value = item.GetUnit();
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
                HideParameterDescription();
                grid.Rows.Clear();

                ApplyTeachColumnLayout();

                foreach (var item in _items)
                    AddParameterRow(item);

                ApplyScopeStyles();
                ApplyGroupVisibility();
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

        /// <summary>티칭 포지션 항목이 하나라도 있으면 MOVE/TEACH 버튼 열을 표시하고 열 폭 비율을 조정한다.
        /// (unit:scope:value:name = 1:5:4:0 비율로만 줄여 Name 폭은 보존 → 최장 파라미터 이름이 잘리지 않음)
        /// 티칭 항목이 없으면 버튼 열을 숨기고 원래 폭 비율로 되돌린다.</summary>
        private void ApplyTeachColumnLayout()
        {
            try
            {
                if (colMove == null || colTeach == null)
                    return;

                bool anyTeach = false;
                foreach (var it in _items)
                {
                    if (it != null && it.SupportsTeaching)
                    {
                        anyTeach = true;
                        break;
                    }
                }

                colMove.Visible = anyTeach;
                colTeach.Visible = anyTeach;

                if (anyTeach)
                {
                    // Unit/Scope는 고정폭 → 그리드 폭이 변해도(스크롤바 등) 헤더/값이 절대 안 잘린다.
                    // Name/Value만 Fill로 남는 폭을 324:119 비율로 나눠 가진다(Name 최대한 보존).
                    colUnit.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    colUnit.Width = 44;                       // count / "UNIT" 헤더가 딱 들어갈 만큼
                    colScope.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    colScope.Width = 54;                      // 대문자 "SCOPE" 헤더 + "CONFIG" 값 + 그룹 화살표 여유
                    colName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    colValue.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    colName.FillWeight = 324F;
                    colValue.FillWeight = 119F;

                    // ACTION 헤더는 colMove+colTeach 두 열에 걸쳐 Paint 오버레이(Grid_ActionHeaderPaint)로 가운데 그린다.
                    // → 열 헤더 텍스트는 비워서 버튼 폭을 헤더 폭에 구애받지 않고 최소로 유지.
                    colMove.HeaderText = string.Empty;
                    colTeach.HeaderText = string.Empty;
                }
                else
                {
                    colUnit.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
                    colScope.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
                    colName.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
                    colValue.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
                    colName.FillWeight = 52F;
                    colValue.FillWeight = 23F;
                    colUnit.FillWeight = 10F;
                    colScope.FillWeight = 15F;
                    colMove.HeaderText = string.Empty;
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "PARAM-GRID", "ApplyTeachColumnLayout failed: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>티칭 포지션이 아닌 행(헤더·Bool·Config 등)의 MOVE/TEACH 칸을 빈 셀로 대체해 버튼이 그려지지 않게 한다.</summary>
        private void SetTeachCellsBlank(DataGridViewRow row, Color back)
        {
            try
            {
                if (row == null || colMove == null || colTeach == null)
                    return;

                var moveCell = new DataGridViewTextBoxCell { Value = string.Empty };
                var teachCell = new DataGridViewTextBoxCell { Value = string.Empty };
                row.Cells[colMove.Index] = moveCell;
                row.Cells[colTeach.Index] = teachCell;
                moveCell.ReadOnly = true;
                teachCell.ReadOnly = true;
                moveCell.Style.BackColor = back;
                moveCell.Style.SelectionBackColor = back;
                teachCell.Style.BackColor = back;
                teachCell.Style.SelectionBackColor = back;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "PARAM-GRID", "SetTeachCellsBlank failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void AddParameterRow(ParameterGridItem item)
        {
            int index = -1;
            try
            {
                if (item == null)
                    return;

                if (item.IsGroupHeader)
                {
                    AddGroupHeaderRow(item);
                    return;
                }

                index = grid.Rows.Add();
                DataGridViewRow row = grid.Rows[index];
                row.Tag = item;
                row.Cells[colValue.Index] = CreateValueCell(item);
                row.Cells[colName.Index].ReadOnly = true;
                // [픽업 BIN 필터 2026-08-27] Text 항목도 값 셀 인라인 편집 허용 — 커밋은 기존
                // grid_CellEndEdit의 Text 분기(CommitRow)가 처리한다(그동안 Text 실사용 항목 0건).
                row.Cells[colValue.Index].ReadOnly =
                    item.ValueType != ParameterGridValueType.Selection &&
                    item.ValueType != ParameterGridValueType.Text;
                row.Cells[colScope.Index].ReadOnly = true;
                row.Cells[colUnit.Index].ReadOnly = true;
                // 접이식 그룹에 속한 멤버는 들여쓰기 + 글머리 기호 → 그룹 없는(원래) 항목과 명확히 구분
                bool isGroupMember = !string.IsNullOrEmpty(item.GroupKey);
                row.Cells[colName.Index].Value = isGroupMember ? "     ·   " + item.DisplayName : item.DisplayName;
                row.Cells[colUnit.Index].Value = item.GetUnit();
                row.Cells[colScope.Index].Value = item.Scope.ToString();
                SetValueCellText(row, item, FormatValue(item));
                // 티칭 포지션 행만 MOVE/TEACH 버튼 유지, 그 외에는 버튼 칸을 빈 셀로
                if (!item.SupportsTeaching)
                    SetTeachCellsBlank(row, Color.White);
                ApplyDescriptionToolTip(row, item);
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

        private string BuildHeaderText(ParameterGridItem header, bool collapsed)
        {
            int count = 0;
            if (header != null)
            {
                foreach (var it in _items)
                {
                    if (it != null && !it.IsGroupHeader && string.Equals(it.GroupKey, header.GroupKey, StringComparison.OrdinalIgnoreCase))
                        count++;
                }
            }

            string arrow = collapsed ? "▶" : "▼";
            string name = header != null ? (header.DisplayName ?? string.Empty) : string.Empty;
            return arrow + " " + name + " (" + count + ")";
        }

        private void AddGroupHeaderRow(ParameterGridItem item)
        {
            int index = -1;
            try
            {
                index = grid.Rows.Add();
                DataGridViewRow row = grid.Rows[index];
                row.Tag = item;

                bool collapsed = !string.IsNullOrEmpty(item.GroupKey) && _collapsedGroups.Contains(item.GroupKey);
                row.Cells[colValue.Index] = new DataGridViewTextBoxCell();
                row.Cells[colName.Index].Value = BuildHeaderText(item, collapsed);
                row.Cells[colValue.Index].Value = string.Empty;
                row.Cells[colUnit.Index].Value = string.Empty;
                row.Cells[colScope.Index].Value = collapsed ? "▾" : "▴";
                foreach (DataGridViewCell cell in row.Cells)
                    cell.ReadOnly = true;

                Color headerBg = Color.FromArgb(208, 220, 236);
                Color headerFg = Color.FromArgb(35, 55, 85);
                row.DefaultCellStyle.BackColor = headerBg;
                row.DefaultCellStyle.ForeColor = headerFg;
                row.DefaultCellStyle.SelectionBackColor = headerBg;
                row.DefaultCellStyle.SelectionForeColor = headerFg;
                row.DefaultCellStyle.Font = new Font(grid.Font.FontFamily, Math.Max(7F, grid.Font.Size - 0.5F), FontStyle.Bold);
                row.Cells[colScope.Index].Style.Font = new Font(grid.Font.FontFamily, Math.Max(7F, grid.Font.Size - 1F), FontStyle.Bold);
                SetTeachCellsBlank(row, headerBg);
                ApplyDescriptionToolTip(row, item);
            }
            catch (Exception ex)
            {
                if (index >= 0 && index < grid.Rows.Count)
                    grid.Rows.RemoveAt(index);
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "Group header add failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (!ShowParameterDescriptions)
                return;

            try
            {
                var hit = grid.HitTest(e.X, e.Y);
                var item = hit.RowIndex >= 0 && hit.ColumnIndex >= 0 && !grid.IsCurrentCellInEditMode
                    ? grid.Rows[hit.RowIndex].Tag as ParameterGridItem
                    : null;
                string description = item != null ? item.Description : null;
                if (string.IsNullOrWhiteSpace(description))
                {
                    HideParameterDescription();
                    return;
                }

                // 같은 행에서 움직이거나 값이 갱신되어도 표시 시간을 계속 초기화하지 않는다.
                if (ReferenceEquals(_descriptionItem, item) && _descriptionText == description)
                    return;

                HideParameterDescription();
                _descriptionItem = item;
                _descriptionText = description;
                grid.ShowCellToolTips = false;
                parameterDescriptionToolTip.ToolTipTitle = item.DisplayName ?? string.Empty;
                parameterDescriptionToolTip.Show(description, grid, e.X + 16, e.Y + 20, 15000);
            }
            catch (Exception ex)
            {
                HideParameterDescription();
                EventLogger.Write(EventKind.Warning, "UI", "PARAM-GRID",
                    "설정 항목 설명 표시 실패: " + ex.Message);
            }
        }

        private void grid_DescriptionDismissed(object sender, EventArgs e)
        {
            HideParameterDescription();
        }

        private void HideParameterDescription()
        {
            if (_descriptionItem == null)
                return;

            parameterDescriptionToolTip.Hide(grid);
            _descriptionItem = null;
            _descriptionText = null;
            grid.ShowCellToolTips = true;
        }

        private void ApplyDescriptionToolTip(DataGridViewRow row, ParameterGridItem item)
        {
            try
            {
                if (row == null || item == null)
                    return;

                string description = item.Description ?? string.Empty;
                foreach (DataGridViewCell cell in row.Cells)
                    cell.ToolTipText = description;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "ApplyDescriptionToolTip failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ApplyGroupVisibility()
        {
            try
            {
                foreach (DataGridViewRow row in grid.Rows)
                {
                    var item = row.Tag as ParameterGridItem;
                    if (item == null || item.IsGroupHeader || string.IsNullOrEmpty(item.GroupKey))
                        continue;

                    bool collapsed = _collapsedGroups.Contains(item.GroupKey);
                    if (collapsed && grid.CurrentCell != null && grid.CurrentCell.RowIndex == row.Index)
                        grid.CurrentCell = null;
                    row.Visible = !collapsed;
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "ApplyGroupVisibility failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ToggleGroup(string groupKey)
        {
            try
            {
                if (string.IsNullOrEmpty(groupKey))
                    return;

                bool nowCollapsed = !_collapsedGroups.Contains(groupKey);
                if (nowCollapsed)
                    _collapsedGroups.Add(groupKey);
                else
                    _collapsedGroups.Remove(groupKey);

                foreach (DataGridViewRow row in grid.Rows)
                {
                    var item = row.Tag as ParameterGridItem;
                    if (item == null || !string.Equals(item.GroupKey, groupKey, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (item.IsGroupHeader)
                    {
                        row.Cells[colName.Index].Value = BuildHeaderText(item, nowCollapsed);
                        row.Cells[colScope.Index].Value = nowCollapsed ? "▾" : "▴";
                    }
                    else
                    {
                        if (nowCollapsed && grid.CurrentCell != null && grid.CurrentCell.RowIndex == row.Index)
                            grid.CurrentCell = null;
                        row.Visible = !nowCollapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "ToggleGroup failed: " + ex.Message);
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
                    if (item == null || item.IsGroupHeader)
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

                object previous = item.Getter != null ? item.Getter() : null;
                if (item.Getter != null && object.Equals(previous, value))
                {
                    SetValueCellText(row, item, FormatValue(item));
                    return;
                }

                item.Setter(value);
                SetValueCellText(row, item, FormatValue(item));
                if (item.Getter != null && object.Equals(previous, item.Getter()))
                    return;
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

        private void OnParameterMoveRequested(ParameterGridItem item)
        {
            try
            {
                ParameterMoveRequested?.Invoke(this, new ParameterGridChangedEventArgs(item));
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "MoveRequested failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void OnParameterTeachRequested(ParameterGridItem item)
        {
            try
            {
                ParameterTeachRequested?.Invoke(this, new ParameterGridChangedEventArgs(item));
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "PARAM-GRID", "TeachRequested failed: " + ex.Message);
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
                if (item == null ||
                    (item.ValueType != ParameterGridValueType.Double &&
                     item.ValueType != ParameterGridValueType.Int &&
                     item.ValueType != ParameterGridValueType.Text))
                    return;

                // [픽업 BIN 필터 2026-08-27] Text 항목도 더블클릭 시 키패드로 입력한다(터치 환경 —
                // 자유 텍스트 모드: 수식 평가 없이 입력 그대로 커밋, 유효성은 항목 setter가 정규화).
                bool freeText = item.ValueType == ParameterGridValueType.Text;
                string currentText = FormatValue(item);
                using (var dialog = new NumericKeypadDialog(item.DisplayName, currentText, item.GetUnit(), freeText))
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
                if (e.RowIndex < 0)
                    return;

                var headerItem = grid.Rows[e.RowIndex].Tag as ParameterGridItem;
                if (headerItem != null && headerItem.IsGroupHeader)
                {
                    ToggleGroup(headerItem.GroupKey);
                    return;
                }

                // MOVE / TEACH 버튼 열 클릭 → 티칭 포지션 행일 때만 이벤트 발생(이동 로직은 구독 페이지가 담당)
                if (colMove != null && (e.ColumnIndex == colMove.Index || e.ColumnIndex == colTeach.Index))
                {
                    var teachItem = grid.Rows[e.RowIndex].Tag as ParameterGridItem;
                    if (teachItem != null && teachItem.SupportsTeaching && !teachItem.IsGroupHeader)
                    {
                        if (e.ColumnIndex == colMove.Index)
                            OnParameterMoveRequested(teachItem);
                        else
                            OnParameterTeachRequested(teachItem);
                    }
                    return;
                }

                if (e.ColumnIndex != colValue.Index)
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

                var headerItem = grid.Rows[e.RowIndex].Tag as ParameterGridItem;
                if (headerItem != null && headerItem.IsGroupHeader)
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
                if (item == null || item.IsGroupHeader || e.Value == null)
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
                if (e.Button == MouseButtons.Right && (e.RowIndex < 0 || e.ColumnIndex < 0))
                {
                    grid.ClearSelection();
                    grid.CurrentCell = null;
                    return;
                }

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

        /// <summary>MOVE/TEACH 두 열의 헤더 자리에 걸쳐 "ACTION" 텍스트를 가운데로 그린다.
        /// (DataGridView는 헤더 셀 병합을 지원하지 않으므로, 두 열 헤더는 빈 상태로 두고 여기서 오버레이로 그린다.)
        /// 덕분에 버튼 열 폭을 헤더 글자폭에 구애받지 않고 최소로 유지할 수 있다.</summary>
        private void Grid_ActionHeaderPaint(object sender, PaintEventArgs e)
        {
            try
            {
                if (colMove == null || colTeach == null || !colMove.Visible)
                    return;

                Rectangle rMove = grid.GetCellDisplayRectangle(colMove.Index, -1, true);
                Rectangle rTeach = grid.GetCellDisplayRectangle(colTeach.Index, -1, true);

                Rectangle union;
                if (rMove.Width > 0 && rTeach.Width > 0)
                    union = Rectangle.Union(rMove, rTeach);
                else if (rMove.Width > 0)
                    union = rMove;
                else if (rTeach.Width > 0)
                    union = rTeach;
                else
                    return;

                DataGridViewCellStyle headerStyle = grid.ColumnHeadersDefaultCellStyle;
                TextRenderer.DrawText(e.Graphics, "ACTION", headerStyle.Font, union, headerStyle.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "PARAM-GRID", "ActionHeaderPaint failed: " + ex.Message);
            }
            finally
            {
            }
        }
    }
}


