using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.Ajin;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
using QMC.Common;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>
    /// Stage 59 — Axis Setup 페이지.
    /// Ajin 매핑 파일에 등록된 축을 기준으로 AxisSetup / AxisConfig 데이터를 표시 + 편집 + 저장.
    /// </summary>
    public partial class AxisSetupPage : PageBase
    {
        public class AxisRow
        {
            public int No { get; set; }
            public string Name { get; set; }
            public string Module { get; set; }
            public double Stroke { get; set; }
            public double SoftLimitNeg { get; set; }
            public double SoftLimitPos { get; set; }
            public string Unit { get; set; }   // mm/um/deg
            public bool SimulationMode { get; set; } = true;
            public int BoardNo { get; set; }
            public int ChannelNo { get; set; }
            /// <summary>AjinConfig 매핑에 사용할 키 (PickerComponent 의 BaseAxis.Name 과 일치).</summary>
            public string ConfigKey { get; set; }
        }

        private List<AxisRow> _items;
        private bool _gridLoading;
        private QMC.CDT_320.Ui.Dialogs.SharedRailXSetupDialog _sharedRailXDialog;

        public AxisSetupPage()
        {
            InitializeComponent();
            if (!DesignMode)
            {
                ApplyRuntimeUi();
                _items = LoadOrSeed();
                FillGrid();
            }
            //ApplyRuntimeUi();
            //_items = LoadOrSeed();
            //FillGrid();
        }

        /// <summary>
        /// Designer 에서 표현하기 어려운 UI 요소(PageBase.CreateSectionHeader, UiTheme 정적 색상 등)를 런타임에 적용한다.
        /// </summary>
        private void ApplyRuntimeUi()
        {
            // i18n 섹션 헤더 (PageBase 헬퍼 사용)
            Controls.Add(CreateSectionHeader("set.axisSetup"));

            // UiTheme 정적 색상은 디자이너에서 직접 표현되지 않으므로 런타임 적용
            lblSubHeader.BackColor = UiTheme.StatusBarBg;
            lblSubHeader.ForeColor = Color.White;
            lblSubHeader.Font = UiTheme.SectionFont;

            actionsPanel.BackColor = UiTheme.OptionPanelBg;
        }

        // ── Button click handlers (Designer 에서 연결) ────────────────
        private void OnSaveClick(object sender, EventArgs e) => DoSave();

        private void OnReloadClick(object sender, EventArgs e)
        {
            _items = LoadOrSeed();
            FillGrid();
        }

        private void OnResetClick(object sender, EventArgs e)
        {
            if (QMC.Common.MessageDialog.Show("기본값으로 초기화?", "Reset", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
            ResetRowsToDefaults(_items);
            FillGrid();
        }

        private void OnApplyClick(object sender, EventArgs e) => ApplyToAxes();

        private void OnSharedRailXClick(object sender, EventArgs e)
        {
            try
            {
                Form1 host = FindForm() as Form1;
                if (host == null || host.Controller == null)
                {
                    QMC.Common.MessageDialog.Show("MachineController 미초기화", "SharedRailX",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_sharedRailXDialog == null || _sharedRailXDialog.IsDisposed)
                {
                    _sharedRailXDialog = new QMC.CDT_320.Ui.Dialogs.SharedRailXSetupDialog(host.Controller);
                    _sharedRailXDialog.FormClosed += (s, args) => { _sharedRailXDialog = null; };
                    _sharedRailXDialog.Show(host);
                }
                else
                {
                    if (!_sharedRailXDialog.Visible)
                        _sharedRailXDialog.Show(host);
                    _sharedRailXDialog.Activate();
                    _sharedRailXDialog.BringToFront();
                }
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("SharedRailX dialog open failed: " + ex.Message,
                    "SharedRailX", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        // ── 메뉴얼 기준 37 axes seed ─────────────────────────────────
        public static List<AxisRow> SeedDefault()
        {
            var L = new List<AxisRow>();
            try
            {
                AjinConfig cfg = AjinConfigStore.Load();
                if (cfg?.Axes == null) return L;

                foreach (var item in cfg.Axes.OrderBy(x => x.Value != null ? x.Value.Axis : int.MaxValue))
                {
                    AxisMap map = item.Value;
                    if (map == null) continue;
                    string key = AjinAxisDefaults.ResolveName(item.Key);
                    AxisDefault axis = FindDefault(key);

                    L.Add(new AxisRow
                    {
                        No = map.Axis,
                        Module = axis != null ? axis.Module : string.Empty,
                        Name = axis != null ? axis.AxisName : key,
                        ConfigKey = key,
                        BoardNo = map.BoardNo,
                        ChannelNo = map.ChannelNo,
                        Stroke = axis != null ? axis.Stroke : 0.0,
                        SimulationMode = true,
                        Unit = NormalizeUnitForAxis(key, axis != null ? axis.Unit : AxisUnitConverter.Millimeter),
                        SoftLimitNeg = 0,
                        SoftLimitPos = axis != null ? axis.Stroke : 200.0
                    });
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-SEED", "Axis setup seed failed: " + ex.Message);
            }
            return L;
        }

        public static List<AxisRow> LoadConfiguredRows()
        {
            return LoadOrSeed();
        }

        // Persistence ──────────────────────────────────────────────
        private static List<AxisRow> LoadOrSeed()
        {
            try
            {
                AjinConfigStore.Load();
                AjinFactory.ReloadConfiguredAxes();

                BaseAxis[] axes = AjinFactory.AxisManager.GetAll();
                if (axes == null || axes.Length == 0) return SeedDefault();

                return axes
                    .Where(x => x != null)
                    .Select(ToAxisRow)
                    .OrderBy(x => x.No)
                    .ThenBy(x => x.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-LOAD", "Axis setup load failed: " + ex.Message);
                return SeedDefault();
            }
        }

        private static AxisRow ToAxisRow(BaseAxis axis)
        {
            AxisSetup setup = axis.Setup ?? new AxisSetup();
            AxisConfig config = axis.Config ?? new AxisConfig();
            AxisMap map = FindAxisMap(axis.Name);

            return new AxisRow
            {
                No = setup.AxisNo,
                Module = setup.UnitName,
                Name = string.IsNullOrWhiteSpace(setup.DisplayName) ? axis.Name : setup.DisplayName,
                ConfigKey = AjinAxisDefaults.ResolveName(axis.Name),
                BoardNo = setup.BoardNo,
                ChannelNo = map != null ? map.ChannelNo : 0,
                Stroke = setup.Stroke,
                SimulationMode = config.IsSimulationMode,
                Unit = NormalizeUnitForAxis(axis.Name, setup.Unit),
                SoftLimitNeg = setup.SoftLimitMinus,
                SoftLimitPos = setup.SoftLimitPlus
            };
        }

        private static AxisMap FindAxisMap(string axisName)
        {
            try
            {
                AjinConfig cfg = AjinConfigStore.Current ?? AjinConfigStore.Load();
                if (cfg?.Axes == null) return null;
                string key = AjinAxisDefaults.ResolveName(axisName);

                AxisMap map;
                if (!string.IsNullOrWhiteSpace(key) && cfg.Axes.TryGetValue(key, out map))
                    return map;

                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private static AxisDefault FindDefault(string axisName)
        {
            try
            {
                string key = AjinAxisDefaults.ResolveName(axisName);
                foreach (AxisDefault axis in AjinAxisDefaults.All)
                    if (string.Equals(axis.AxisName, key, StringComparison.OrdinalIgnoreCase))
                        return axis;
                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private static void ResetRowsToDefaults(IEnumerable<AxisRow> rows)
        {
            try
            {
                if (rows == null) return;
                foreach (AxisRow row in rows)
                {
                    AxisDefault axis = FindDefault(row.ConfigKey);
                    if (axis == null) continue;

                    row.Module = axis.Module;
                    row.Name = axis.AxisName;
                    row.Stroke = axis.Stroke;
                    row.Unit = NormalizeUnitForAxis(row.ConfigKey, axis.Unit);
                    row.SoftLimitNeg = 0;
                    row.SoftLimitPos = axis.Stroke;
                    row.SimulationMode = true;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-RESET", "Axis setup reset failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void DoSave()
        {
            try
            {
                int applied = ApplyRowsToAxes();
                AjinFactory.AxisManager.Save(MotionAxisStore.DefaultPath);
                QMC.Common.MessageDialog.Show("저장 완료.\n" + MotionAxisStore.DefaultPath + "\n적용 축: " + applied);
            }
            catch (Exception ex) { QMC.Common.MessageDialog.Show("실패: " + ex.Message); }
        }

        // ── Grid ─────────────────────────────────────────────────────
        private void FillGrid()
        {
            try
            {
                _gridLoading = true;
                grid.Rows.Clear();
                string lastMod = null;
                foreach (var it in _items)
                {
                    int idx = grid.Rows.Add();
                    PopulateGridRow(grid.Rows[idx], it, it.Module != lastMod);
                    if (it.Module != lastMod)
                        lastMod = it.Module;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-GRID", "Axis setup grid refresh failed: " + ex.Message);
            }
            finally
            {
                _gridLoading = false;
            }
        }

        private void PopulateGridRow(DataGridViewRow row, AxisRow it, bool isModuleStart)
        {
            try
            {
                row.Cells["NO"].Value = "#" + it.No.ToString("00");
                row.Cells["MODULE"].Value = it.Module;
                row.Cells["NAME"].Value = it.Name;
                row.Cells["BOARD"].Value = it.BoardNo.ToString();
                row.Cells["CH"].Value = it.ChannelNo.ToString("X");
                row.Cells["UNIT"].Value = it.Unit;
                row.Cells["STROKE"].Value = AxisUnitConverter.Format(AxisUnitConverter.ToDisplay(it.Stroke, it.Unit), it.Unit);
                row.Cells["SIM"].Value = it.SimulationMode ? "ON" : "OFF";
                row.Cells["SLN"].Value = AxisUnitConverter.Format(AxisUnitConverter.ToDisplay(it.SoftLimitNeg, it.Unit), it.Unit);
                row.Cells["SLP"].Value = AxisUnitConverter.Format(AxisUnitConverter.ToDisplay(it.SoftLimitPos, it.Unit), it.Unit);

                row.DefaultCellStyle.BackColor = Color.White;
                row.DefaultCellStyle.Font = grid.DefaultCellStyle.Font;
                ApplySimCellStyle(row.Cells["SIM"], it.SimulationMode);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-GRID-ROW", "Axis setup row refresh failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ApplySimCellStyle(DataGridViewCell cell, bool isOn)
        {
            try
            {
                if (cell == null) return;
                cell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                cell.Style.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
                cell.Style.ForeColor = isOn ? Color.FromArgb(0x10, 0x55, 0x2D) : Color.FromArgb(0x55, 0x55, 0x55);
                cell.Style.BackColor = isOn ? Color.FromArgb(0xD8, 0xF3, 0xDC) : Color.FromArgb(0xF5, 0xF5, 0xF5);
                cell.Style.SelectionForeColor = cell.Style.ForeColor;
                cell.Style.SelectionBackColor = isOn ? Color.FromArgb(0xB7, 0xE4, 0xC7) : Color.FromArgb(0xE6, 0xE6, 0xE6);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-SIM-STYLE", "Axis setup sim style failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void OnGridCellClick(object s, DataGridViewCellEventArgs e)
        {
            try
            {
                if (_gridLoading) return;
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                DataGridViewColumn col = grid.Columns[e.ColumnIndex];
                if (col == null || col.Name != "SIM") return;

                ToggleSimWithConfirm(e.RowIndex);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-SIM-CLICK", "Axis setup sim click failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void OnCellDoubleClick(object s, DataGridViewCellEventArgs e)
        {
            try
            {
                if (_gridLoading) return;
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

                DataGridViewColumn col = grid.Columns[e.ColumnIndex];
                if (!IsEditableColumn(col.Name)) return;
                if (col.Name == "SIM") return;

                DataGridViewCell cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                object nextValue;

                if (IsToggleColumn(col.Name))
                {
                    ToggleSimWithConfirm(e.RowIndex);
                    return;
                }
                else if (IsEnumColumn(col.Name))
                {
                    string picked = ShowEnumDialog(col.HeaderText, GetEnumOptions(col.Name), Convert.ToString(cell.Value) ?? string.Empty);
                    if (picked == null) return;
                    nextValue = picked;
                }
                else if (IsNumericColumn(col.Name))
                {
                    string current = Convert.ToString(cell.Value) ?? string.Empty;
                    string title = (grid.Rows[e.RowIndex].Cells["NAME"].Value?.ToString() ?? "AXIS") + " - " + col.HeaderText;
                    using (var dlg = new NumericKeypadDialog(title, current, string.Empty))
                    {
                        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                        nextValue = dlg.ValueText ?? string.Empty;
                    }
                }
                else
                {
                    return;
                }

                if (string.Equals(Convert.ToString(nextValue), Convert.ToString(cell.Value), StringComparison.Ordinal))
                    return;

                ApplyCellValue(e.RowIndex, col.Name, nextValue);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-CELL-EDIT", "Axis setup cell edit failed: " + ex.Message);
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Warning,
                    "UI-AXIS-SETUP",
                    "AxisSetupPage",
                    "Cell edit failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ToggleSimWithConfirm(int rowIndex)
        {
            try
            {
                if (rowIndex < 0 || rowIndex >= _items.Count) return;
                AxisRow row = _items[rowIndex];
                bool next = !row.SimulationMode;
                string axisName = string.IsNullOrWhiteSpace(row.Name) ? row.ConfigKey : row.Name;
                string message = axisName + " 축을 " + (next ? "SIM 모드로 변경할까요?" : "REAL 모드로 변경할까요?");
                if (QMC.Common.MessageDialog.Show(message, "SIM MODE", MessageBoxButtons.OKCancel) != DialogResult.OK)
                    return;

                ApplyCellValue(rowIndex, "SIM", next);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-SIM-TOGGLE", "Axis setup sim toggle failed: " + ex.Message);
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Warning,
                    "UI-AXIS-SETUP",
                    "AxisSetupPage",
                    "SIM toggle failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void OnColumnHeaderMouseDoubleClick(object s, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (_gridLoading) return;
                if (e.ColumnIndex < 0 || grid.Rows.Count == 0) return;

                DataGridViewColumn col = grid.Columns[e.ColumnIndex];
                if (!IsEditableColumn(col.Name)) return;

                object nextValue;
                object current = grid.Rows[0].Cells[e.ColumnIndex].Value;
                string title = "ALL AXES - " + col.HeaderText;

                if (IsToggleColumn(col.Name))
                {
                    string picked = ShowEnumDialog(title, new[] { "ON", "OFF" }, ToBool(current) ? "ON" : "OFF");
                    if (picked == null) return;
                    nextValue = string.Equals(picked, "ON", StringComparison.OrdinalIgnoreCase);
                    string message = "전체 축을 " + ((bool)nextValue ? "SIM 모드로 변경할까요?" : "REAL 모드로 변경할까요?");
                    if (QMC.Common.MessageDialog.Show(message, "SIM MODE", MessageBoxButtons.OKCancel) != DialogResult.OK)
                        return;
                }
                else if (IsEnumColumn(col.Name))
                {
                    string picked = ShowEnumDialog(title, GetEnumOptions(col.Name), Convert.ToString(current) ?? string.Empty);
                    if (picked == null) return;
                    nextValue = picked;
                }
                else if (IsNumericColumn(col.Name))
                {
                    string currentText = Convert.ToString(current) ?? string.Empty;
                    using (var dlg = new NumericKeypadDialog(title, currentText, string.Empty))
                    {
                        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                        nextValue = dlg.ValueText ?? string.Empty;
                    }
                }
                else
                {
                    return;
                }

                foreach (AxisRow it in _items)
                {
                    if (IsNumericColumn(col.Name) && IsDegreeRow(it))
                        continue;

                    ApplyItemValue(it, col.Name, nextValue);
                }

                ApplyRowsToAxes();
                FillGrid();
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-HEADER-EDIT", "Axis setup header edit failed: " + ex.Message);
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Warning,
                    "UI-AXIS-SETUP",
                    "AxisSetupPage",
                    "Header edit failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ApplyCellValue(int rowIndex, string col, object value)
        {
            try
            {
                if (rowIndex < 0 || rowIndex >= _items.Count) return;
                ApplyItemValue(_items[rowIndex], col, value);
                ApplyRowToMatchingAxes(_items[rowIndex]);
                if (rowIndex < grid.Rows.Count)
                    PopulateGridRow(grid.Rows[rowIndex], _items[rowIndex], IsModuleStart(rowIndex));
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-CELL", "Axis setup cell apply failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static bool IsEditableColumn(string col)
        {
            try
            {
                return IsNumericColumn(col) || IsToggleColumn(col) || IsEnumColumn(col);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool IsNumericColumn(string col)
        {
            try
            {
                return col == "STROKE" || col == "SLN" || col == "SLP";
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool IsToggleColumn(string col)
        {
            try
            {
                return col == "SIM";
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool IsEnumColumn(string col)
        {
            try
            {
                return col == "UNIT";
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static string[] GetEnumOptions(string col)
        {
            try
            {
                if (col == "UNIT") return AxisUnitConverter.SupportedUnits;
                return new string[0];
            }
            catch
            {
                return new string[0];
            }
            finally
            {
            }
        }

        private string ShowEnumDialog(string title, IEnumerable<string> options, string current)
        {
            try
            {
                using (var dlg = new EnumPickerDialog(title, options, current))
                {
                    if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return null;
                    return dlg.SelectedValue;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-ENUM", "Axis setup enum picker failed: " + ex.Message);
                return null;
            }
            finally
            {
            }
        }

        private bool IsModuleStart(int rowIndex)
        {
            try
            {
                if (rowIndex <= 0) return true;
                if (rowIndex >= _items.Count) return false;
                return !string.Equals(_items[rowIndex - 1].Module, _items[rowIndex].Module, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-GRID-STYLE", "Axis setup row style failed: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private static void ApplyItemValue(AxisRow it, string col, object value)
        {
            string txt = Convert.ToString(value) ?? "";
            switch (col)
            {
                // 축 Stroke 값 적용
                case "STROKE": if (TryReadDouble(txt, out var v1)) it.Stroke = AxisUnitConverter.FromDisplay(v1, it.Unit); break;
                // 축 Simulation 사용 여부 적용
                case "SIM": it.SimulationMode = ToBool(value); break;
                // 축 Minus Soft Limit 적용
                case "SLN": if (TryReadDouble(txt, out var v2)) it.SoftLimitNeg = AxisUnitConverter.FromDisplay(v2, it.Unit); break;
                // 축 Plus Soft Limit 적용
                case "SLP": if (TryReadDouble(txt, out var v3)) it.SoftLimitPos = AxisUnitConverter.FromDisplay(v3, it.Unit); break;
                // 축 표시 단위 적용
                case "UNIT":
                    ApplyUnitValue(it, txt);
                    break;
            }
        }

        private static void ApplyUnitValue(AxisRow row, string unit)
        {
            try
            {
                if (row == null) return;
                if (AjinAxisDefaults.IsThetaAxis(row.ConfigKey) || AjinAxisDefaults.IsThetaAxis(row.Name))
                {
                    row.Unit = AxisUnitConverter.Degree;
                    return;
                }

                string toUnit = AxisUnitConverter.Normalize(unit);
                if (!AxisUnitConverter.IsSupported(toUnit)) return;
                row.Unit = toUnit;
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-UNIT", "Axis unit apply failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static string NormalizeUnitForAxis(string axisName, string unit)
        {
            try
            {
                if (AjinAxisDefaults.IsThetaAxis(axisName))
                    return AxisUnitConverter.Degree;
                return AxisUnitConverter.Normalize(unit);
            }
            catch
            {
                return AxisUnitConverter.Normalize(unit);
            }
            finally
            {
            }
        }

        private static bool TryReadDouble(string text, out double value)
        {
            try
            {
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    return true;
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                    return true;
                value = 0;
                return false;
            }
            catch
            {
                value = 0;
                return false;
            }
            finally
            {
            }
        }

        private static bool ToBool(object value)
        {
            if (value is bool b) return b;
            string txt = (Convert.ToString(value) ?? "").Trim().ToUpper();
            return txt == "TRUE" || txt == "ON" || txt == "1" || txt == "YES";
        }

        private static bool IsDegreeRow(AxisRow row)
        {
            try
            {
                if (row == null) return false;
                if (AxisUnitConverter.IsDegree(row.Unit)) return true;
                if (AjinAxisDefaults.IsThetaAxis(row.ConfigKey)) return true;
                return AjinAxisDefaults.IsThetaAxis(row.Name);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private void OnGridDataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }

        /// <summary>AxisSetup / AxisConfig 값을 현재 등록된 축에 반영한다. Ajin 매핑 파일은 수정하지 않는다.</summary>
        private void ApplyToAxes()
        {
            try
            {
                int axisApplied = ApplyRowsToAxes();
                QMC.Common.MessageDialog.Show("Axis setup 적용 축: " + axisApplied);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-APPLY", "Axis setup apply failed: " + ex.Message);
                QMC.Common.MessageDialog.Show("Axis setup 적용 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private int ApplyRowsToAxes()
        {
            int count = 0;
            try
            {
                BaseAxis[] axes = GetRuntimeAxes();
                foreach (AxisRow row in _items)
                {
                    BaseAxis axis = axes.FirstOrDefault(x =>
                        x != null &&
                        string.Equals(AjinAxisDefaults.ResolveName(x.Name), AjinAxisDefaults.ResolveName(row.ConfigKey), StringComparison.OrdinalIgnoreCase));
                    if (axis == null) continue;

                    ApplyRowToAxis(row, axis);
                    count++;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-APPLY-ROWS", "Axis setup row apply failed: " + ex.Message);
            }
            finally
            {
            }
            return count;
        }

        private BaseAxis[] GetRuntimeAxes()
        {
            try
            {
                IEnumerable<BaseAxis> axes = AjinFactory.AxisManager.GetAll() ?? new BaseAxis[0];
                var host = FindForm() as Form1;
                if (host?.Machine != null)
                    axes = axes.Concat(EnumerateAxes(host.Machine));

                return axes
                    .Where(x => x != null)
                    .Distinct()
                    .ToArray();
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-RUNTIME", "Axis runtime enumerate failed: " + ex.Message);
                return AjinFactory.AxisManager.GetAll() ?? new BaseAxis[0];
            }
            finally
            {
            }
        }

        private int ApplyRowToMatchingAxes(AxisRow row)
        {
            int count = 0;
            try
            {
                if (row == null) return 0;
                BaseAxis[] axes = GetRuntimeAxes();
                foreach (BaseAxis axis in axes)
                {
                    if (axis == null) continue;
                    if (!string.Equals(AjinAxisDefaults.ResolveName(axis.Name), AjinAxisDefaults.ResolveName(row.ConfigKey), StringComparison.OrdinalIgnoreCase))
                        continue;

                    ApplyRowToAxis(row, axis);
                    count++;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-APPLY-ROW-RUNTIME", "Axis setup row immediate apply failed: " + ex.Message);
            }
            finally
            {
            }
            return count;
        }

        private static void ApplyRowToAxis(AxisRow row, BaseAxis axis)
        {
            try
            {
                if (row == null || axis == null) return;
                if (axis.Setup == null || axis.Config == null) return;

                axis.Setup.UnitName = row.Module ?? string.Empty;
                axis.Setup.DisplayName = row.Name ?? axis.Name;
                axis.Setup.Unit = NormalizeUnitForAxis(row.ConfigKey, row.Unit);
                axis.Setup.Stroke = row.Stroke;
                axis.Setup.SoftLimitMinus = row.SoftLimitNeg;
                axis.Setup.SoftLimitPlus = row.SoftLimitPos;

                axis.Config.IsSimulationMode = row.SimulationMode;

                // 모델 → 보드 동기화: AjinAxis 인 경우 즉시 보드에 setup 을 기록한다.
                QMC.CDT320.Ajin.AjinAxis ajin = axis as QMC.CDT320.Ajin.AjinAxis;
                if (ajin != null)
                    ajin.WriteSetupToBoard();
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "QMC", "AXIS-APPLY-ROW", "Axis setup row apply failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static IEnumerable<QMC.Common.Motion.BaseAxis> EnumerateAxes(QMC.CDT320.CDT320_Machine m)
        {
            foreach (var u in m.Units) foreach (var a in Rec(u)) yield return a;
        }
        private static IEnumerable<QMC.Common.Motion.BaseAxis> Rec(QMC.Common.BaseEquipmentNode node)
        {
            if (node is QMC.Common.Motion.BaseAxis ax) { yield return ax; yield break; }
            var prop = node.GetType().GetProperty("Components");
            if (prop != null && prop.GetValue(node) is System.Collections.IEnumerable comps)
                foreach (QMC.Common.BaseEquipmentNode c in comps) foreach (var a in Rec(c)) yield return a;
        }
    }
}

