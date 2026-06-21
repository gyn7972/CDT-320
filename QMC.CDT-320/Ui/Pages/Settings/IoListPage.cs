using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT320.Ajin;
using QMC.Common.IO;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>Catalog-driven I/O list page.</summary>
    public partial class IoListPage : PageBase
    {
        private readonly string _i18nKey;
        private readonly string[] _columns;
        private readonly Func<string[][]> _rowProvider;
        private int _stateColumnIndex = -1;
        private int _simColumnIndex = -1;
        private AjinIoScanService _subscribedService;
        private bool _loadingRows;
        private bool _pendingCylinderPanelUpdate;

        // 스캔 스냅샷마다 그리드 전체를 훑지 않도록, 행을 주소/이름(또는 실린더 입력 모듈:비트)으로
        // 미리 인덱싱해 둔다. 백그라운드 스레드에서도 안전하게 읽을 수 있도록 통째로 교체(원자적 참조 대입)한다.
        private volatile Dictionary<string, List<DataGridViewRow>> _rowIndex =
            new Dictionary<string, List<DataGridViewRow>>(StringComparer.OrdinalIgnoreCase);

        public IoListPage(string i18nKey, string[] columns, string[][] seedRows)
            : this(i18nKey, columns, () => seedRows)
        {
        }

        public IoListPage(string i18nKey, string[] columns, Func<string[][]> rowProvider)
        {
            _i18nKey = i18nKey;
            _columns = columns;
            _rowProvider = rowProvider ?? (() => new string[0][]);

            InitializeComponent();
            ApplyRuntimeUi();
            WireEvents();
            BuildColumns();
            ConfigureCylinderTestPanel();
            LoadRows();

            HandleCreated += (s, e) => SubscribeIoScan();
            HandleDestroyed += (s, e) => UnsubscribeIoScan();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T(_i18nKey);
            lblHeader.Tag = "i18n:" + _i18nKey;
            lblHeader.BackColor = UiTheme.StatusBarBg;
            lblHeader.ForeColor = UiTheme.StatusBarFg;
            lblHeader.Font = UiTheme.SectionFont;

            lblSubHeader.Text = Lang.T(_i18nKey) + " LIST - AjinIoCatalog";
            lblSubHeader.Tag = "i18n:" + _i18nKey;
            lblSubHeader.BackColor = UiTheme.StatusBarBg;
            lblSubHeader.ForeColor = System.Drawing.Color.White;
            lblSubHeader.Font = UiTheme.SectionFont;
        }

        private void WireEvents()
        {
            btnSave.Visible = false;
            btnReload.Text = "REFRESH";
            btnReload.Click += (s, e) => LoadRows();
            btnAddRow.Visible = false;
            btnSave.Text = "SAVE";
            btnSave.Click += (s, e) => SaveIoSettings();
            btnCylinderApply.Click += (s, e) => ApplySelectedCylinderSettings(true);
            btnCylinderFwd.Click += async (s, e) => await RunCylinderTestAsync("FWD");
            btnCylinderBwd.Click += async (s, e) => await RunCylinderTestAsync("BWD");
            btnCylinderOff.Click += async (s, e) => await RunCylinderTestAsync("OFF");
            _grid.CellClick += OnGridCellClick;
            _grid.CellEnter += OnGridCellEnter;
            _grid.ColumnHeaderMouseClick += OnColumnHeaderMouseClick;
            _grid.SelectionChanged += (s, e) => ScheduleSelectedCylinderPanelUpdate();
            _grid.EditingControlShowing += OnGridEditingControlShowing;
            txtFwdLabel.TextChanged += (s, e) => SyncCylinderButtonText();
            txtBwdLabel.TextChanged += (s, e) => SyncCylinderButtonText();
            _grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_grid.IsCurrentCellDirty)
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _grid.DataError += (s, e) => { e.ThrowException = false; };
        }

        private void OnGridEditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            try
            {
                ComboBox combo = e.Control as ComboBox;
                if (combo == null) return;

                combo.MouseWheel -= Combo_MouseWheel;
                combo.MouseWheel += Combo_MouseWheel;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "GRID-COMBO-WHEEL", "Combo wheel guard setup failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void Combo_MouseWheel(object sender, MouseEventArgs e)
        {
            try
            {
                ((HandledMouseEventArgs)e).Handled = true;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void BuildColumns()
        {
            _grid.Columns.Clear();
            foreach (var column in _columns)
            {
                if (IsCylinderPage() && IsCylinderMapColumn(column))
                {
                    var combo = new DataGridViewComboBoxColumn();
                    combo.Name = column;
                    combo.HeaderText = column;
                    combo.FlatStyle = FlatStyle.Flat;
                    combo.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
                    combo.Items.AddRange(IsCylinderOutputColumn(column) ? OutputOptions() : InputOptions());
                    _grid.Columns.Add(combo);
                }
                else
                {
                    _grid.Columns.Add(column, column);
                }
            }
            _stateColumnIndex = Array.FindIndex(_columns, c => string.Equals(c, "STATE", StringComparison.OrdinalIgnoreCase));
            _simColumnIndex = Array.FindIndex(_columns, c => string.Equals(c, "SIM", StringComparison.OrdinalIgnoreCase));
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.ReadOnly = !IsCylinderPage();
            _grid.EditMode = DataGridViewEditMode.EditOnEnter;
            _grid.AllowUserToResizeColumns = true;
            _grid.AllowUserToResizeRows = false;
            btnSave.Visible = _simColumnIndex >= 0;

            foreach (DataGridViewColumn col in _grid.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
                col.Resizable = DataGridViewTriState.True;
                if (IsCylinderPage() && IsCylinderMapColumn(col.HeaderText))
                    col.ReadOnly = false;
                else if (IsCylinderPage())
                    col.ReadOnly = true;
            }
        }

        private void LoadRows()
        {
            string previousName = null;
            int previousColumn = -1;
            try
            {
                // 현재 선택 중인 행/열을 기억해 두었다가 재로드 후 복원한다.
                if (_grid.CurrentCell != null)
                    previousColumn = _grid.CurrentCell.ColumnIndex;
                if (_grid.CurrentRow != null && !_grid.CurrentRow.IsNewRow)
                    previousName = IoName(_grid.CurrentRow);

                _loadingRows = true;
                _grid.Rows.Clear();
                var rows = _rowProvider();
                if (rows == null) return;
                foreach (var row in rows)
                {
                    int rowIndex = _grid.Rows.Add(row);
                    if (_simColumnIndex >= 0)
                        ApplySimCellStyle(_grid.Rows[rowIndex].Cells[_simColumnIndex], IsSimOn(_grid.Rows[rowIndex]));
                }
            }
            finally
            {
                _loadingRows = false;
                RebuildRowIndex();
                RestoreSelectedRow(previousName, previousColumn);
                UpdateSelectedCylinderPanel();
            }
        }

        private void RestoreSelectedRow(string name, int columnIndex)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) || _grid.Rows.Count == 0) return;

                int targetRow = -1;
                for (int i = 0; i < _grid.Rows.Count; i++)
                {
                    DataGridViewRow row = _grid.Rows[i];
                    if (row.IsNewRow) continue;
                    if (string.Equals(IoName(row), name, StringComparison.OrdinalIgnoreCase))
                    {
                        targetRow = i;
                        break;
                    }
                }

                if (targetRow < 0) return;

                int targetCol = columnIndex;
                if (targetCol < 0 || targetCol >= _grid.Columns.Count)
                    targetCol = _grid.CurrentCell != null ? _grid.CurrentCell.ColumnIndex : 0;
                if (targetCol < 0 || targetCol >= _grid.Columns.Count)
                    targetCol = 0;

                _grid.ClearSelection();
                _grid.Rows[targetRow].Selected = true;
                _grid.CurrentCell = _grid.Rows[targetRow].Cells[targetCol];
            }
            catch
            {
            }
        }

        private void ConfigureCylinderTestPanel()
        {
            try
            {
                bool visible = IsCylinderPage();
                cylinderTestPanel.Visible = visible;
                if (rootLayout.RowStyles.Count > 3)
                    rootLayout.RowStyles[3].Height = visible ? 190F : 0F;
                lblCylinderResult.Text = visible ? "READY" : string.Empty;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void UpdateSelectedCylinderPanel()
        {
            try
            {
                _pendingCylinderPanelUpdate = false;
                if (!IsCylinderPage() || _loadingRows) return;
                string name = SelectedCylinderName();
                lblSelectedCylinder.Text = string.IsNullOrWhiteSpace(name) ? "-" : name;
                if (string.IsNullOrWhiteSpace(name)) return;

                CylinderItemSettings settings = CylinderSettingsStore.Get(name);
                nudFwdTimeout.Value = ClampDecimal(settings.FwdTimeoutMs, nudFwdTimeout.Minimum, nudFwdTimeout.Maximum);
                nudBwdTimeout.Value = ClampDecimal(settings.BwdTimeoutMs, nudBwdTimeout.Minimum, nudBwdTimeout.Maximum);
                chkSingleSolenoid.Checked = settings.IsSingleSolenoid;
                chkUseFwdSensor.Checked = settings.UseFwdInput;
                chkUseBwdSensor.Checked = settings.UseBwdInput;
                txtFwdLabel.Text = string.IsNullOrWhiteSpace(settings.FwdLabel) ? "FWD" : settings.FwdLabel;
                txtBwdLabel.Text = string.IsNullOrWhiteSpace(settings.BwdLabel) ? "BWD" : settings.BwdLabel;
                btnCylinderFwd.Text = txtFwdLabel.Text;
                btnCylinderBwd.Text = txtBwdLabel.Text;
                lblCylinderResult.Text = GetCylinderStateText(name);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "CYL-TEST-SELECT", "Cylinder test selection failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ScheduleSelectedCylinderPanelUpdate()
        {
            try
            {
                if (!IsCylinderPage() || _loadingRows || _pendingCylinderPanelUpdate) return;
                _pendingCylinderPanelUpdate = true;
                if (IsHandleCreated)
                    BeginInvoke(new Action(UpdateSelectedCylinderPanel));
                else
                    UpdateSelectedCylinderPanel();
            }
            catch
            {
                _pendingCylinderPanelUpdate = false;
                UpdateSelectedCylinderPanel();
            }
            finally
            {
            }
        }

        private void SyncCylinderButtonText()
        {
            try
            {
                if (!IsCylinderPage()) return;
                btnCylinderFwd.Text = string.IsNullOrWhiteSpace(txtFwdLabel.Text) ? "FWD" : txtFwdLabel.Text.Trim();
                btnCylinderBwd.Text = string.IsNullOrWhiteSpace(txtBwdLabel.Text) ? "BWD" : txtBwdLabel.Text.Trim();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "CYL-TEST-LABEL", "Cylinder button label sync failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ApplySelectedCylinderSettings(bool showResult)
        {
            try
            {
                string name = SelectedCylinderName();
                if (string.IsNullOrWhiteSpace(name)) return;

                CommitGridEdit();
                DataGridViewRow row = SelectedCylinderRow();
                if (row != null)
                {
                    SaveCylinderMapping(row);
                    AjinConfigStore.SaveOrThrow();
                    CylinderManager.ApplyMappings();
                    ApplyUnitCylinderMappings();
                    RebuildRowIndex();
                }

                CylinderItemSettings settings = CylinderSettingsStore.Get(name);
                settings.FwdTimeoutMs = Convert.ToInt32(nudFwdTimeout.Value);
                settings.BwdTimeoutMs = Convert.ToInt32(nudBwdTimeout.Value);
                settings.IsSingleSolenoid = chkSingleSolenoid.Checked;
                settings.UseFwdInput = chkUseFwdSensor.Checked;
                settings.UseBwdInput = chkUseBwdSensor.Checked;
                settings.FwdLabel = string.IsNullOrWhiteSpace(txtFwdLabel.Text) ? "FWD" : txtFwdLabel.Text.Trim();
                settings.BwdLabel = string.IsNullOrWhiteSpace(txtBwdLabel.Text) ? "BWD" : txtBwdLabel.Text.Trim();
                CylinderSettingsStore.Current.Cylinders[name] = settings;
                CylinderSettingsStore.Save();

                BaseCylinder cylinder = CylinderManager.Get(name);
                CylinderSettingsStore.Apply(cylinder);
                btnCylinderFwd.Text = settings.FwdLabel;
                btnCylinderBwd.Text = settings.BwdLabel;

                if (showResult)
                    lblCylinderResult.Text = "APPLIED";
                EventLogger.Write(EventKind.Event, "QMC", "CYL-TEST-APPLY",
                    "Cylinder settings and mapping applied: " + name);
            }
            catch (Exception ex)
            {
                lblCylinderResult.Text = "APPLY FAIL";
                EventLogger.Write(EventKind.Alarm, "QMC", "CYL-TEST-APPLY", "Cylinder test settings apply failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "CYLINDER TEST", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        private DataGridViewRow SelectedCylinderRow()
        {
            try
            {
                if (_grid.CurrentRow != null && !_grid.CurrentRow.IsNewRow)
                    return _grid.CurrentRow;
                if (_grid.SelectedRows.Count > 0 && !_grid.SelectedRows[0].IsNewRow)
                    return _grid.SelectedRows[0];
            }
            catch
            {
            }
            finally
            {
            }

            return null;
        }

        private void CommitGridEdit()
        {
            try
            {
                if (_grid == null)
                    return;

                if (_grid.IsCurrentCellDirty)
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);

                _grid.EndEdit();
                if (_grid.IsCurrentCellDirty)
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<int> RunCylinderTestAsync(string command)
        {
            try
            {
                string name = SelectedCylinderName();
                if (string.IsNullOrWhiteSpace(name)) return -1;

                ApplySelectedCylinderSettings(false);
                BaseCylinder cylinder = CylinderManager.Get(name);
                if (cylinder == null) return -1;

                SetCylinderTestButtons(false);
                lblCylinderResult.Text = command + " RUN";
                EventLogger.Write(EventKind.Event, "QMC", "CYL-TEST", "Cylinder test command: " + name + " " + command);

                int result = -1;
                if (string.Equals(command, "FWD", StringComparison.OrdinalIgnoreCase))
                    result = await MoveCylinderForTestAsync(cylinder, true);
                else if (string.Equals(command, "BWD", StringComparison.OrdinalIgnoreCase))
                    result = await MoveCylinderForTestAsync(cylinder, false);
                else
                    result = await OffCylinderForTestAsync(cylinder);

                string display = string.Equals(command, "FWD", StringComparison.OrdinalIgnoreCase)
                    ? btnCylinderFwd.Text
                    : string.Equals(command, "BWD", StringComparison.OrdinalIgnoreCase)
                        ? btnCylinderBwd.Text
                        : command;
                lblCylinderResult.Text = display + (result == 0 ? " OK" : " FAIL");
                RefreshCylinderRuntimeState(name);
                return result;
            }
            catch (Exception ex)
            {
                lblCylinderResult.Text = command + " ERROR";
                EventLogger.Write(EventKind.Alarm, "QMC", "CYL-TEST", "Cylinder test failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "CYLINDER TEST", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return -1;
            }
            finally
            {
                SetCylinderTestButtons(true);
            }
        }

        private async Task<int> MoveCylinderForTestAsync(BaseCylinder cylinder, bool forward)
        {
            try
            {
                if (cylinder == null) return -1;
                bool ok = forward ? await cylinder.MoveFwdAsync() : await cylinder.MoveBwdAsync();
                return ok ? 0 : -1;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private Task<int> OffCylinderForTestAsync(BaseCylinder cylinder)
        {
            try
            {
                if (cylinder == null) return Task.FromResult(-1);
                cylinder.OutFwd.Off();
                cylinder.OutBwd.Off();
                return Task.FromResult(0);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void SetCylinderTestButtons(bool enabled)
        {
            try
            {
                btnCylinderApply.Enabled = enabled;
                btnCylinderFwd.Enabled = enabled;
                btnCylinderBwd.Enabled = enabled;
                btnCylinderOff.Enabled = enabled;
            }
            finally
            {
            }
        }

        private string SelectedCylinderName()
        {
            try
            {
                if (_grid.CurrentRow != null && !_grid.CurrentRow.IsNewRow)
                    return IoName(_grid.CurrentRow);
                if (_grid.SelectedRows.Count > 0 && !_grid.SelectedRows[0].IsNewRow)
                    return IoName(_grid.SelectedRows[0]);
            }
            catch
            {
            }
            finally
            {
            }

            return string.Empty;
        }

        private string GetCylinderStateText(string name)
        {
            try
            {
                CylMap map;
                if (!AjinConfigStore.Current.Cylinders.TryGetValue(name, out map))
                    return "READY";
                return "STATE : " + CylinderState(map, name);
            }
            catch
            {
                return "READY";
            }
            finally
            {
            }
        }

        private static decimal ClampDecimal(int value, decimal min, decimal max)
        {
            decimal result = value;
            if (result < min) result = min;
            if (result > max) result = max;
            return result;
        }

        private void OnGridCellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (IsCylinderPage() && e.RowIndex >= 0)
                    ScheduleSelectedCylinderPanelUpdate();

                if (_stateColumnIndex >= 0 &&
                    e.RowIndex >= 0 &&
                    e.ColumnIndex == _stateColumnIndex)
                {
                    ToggleRuntimeIoState(e.RowIndex);
                    return;
                }

                if (_simColumnIndex < 0 || e.RowIndex < 0 || e.ColumnIndex != _simColumnIndex)
                    return;

                ToggleSimWithConfirm(e.RowIndex);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "IO-SIM-CLICK", "I/O sim click failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void OnGridCellEnter(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (IsCylinderPage() && e.RowIndex >= 0)
                    ScheduleSelectedCylinderPanelUpdate();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "CYL-TEST-ENTER", "Cylinder row enter failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void OnColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (_simColumnIndex < 0 || e.ColumnIndex != _simColumnIndex || _grid.Rows.Count == 0)
                    return;

                bool next = !IsSimOn(_grid.Rows[0]);
                string message = "전체 I/O를 " + (next ? "SIM 모드로 변경할까요?" : "REAL 모드로 변경할까요?");
                if (QMC.Common.MessageDialog.Show(message, "I/O SIM MODE", MessageBoxButtons.OKCancel) != DialogResult.OK)
                    return;

                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    ApplySim(row, next);
                }

                SaveIoSettings();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "IO-SIM-HEADER", "I/O sim header apply failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ToggleSimWithConfirm(int rowIndex)
        {
            try
            {
                if (rowIndex < 0 || rowIndex >= _grid.Rows.Count) return;
                DataGridViewRow row = _grid.Rows[rowIndex];
                bool next = !IsSimOn(row);
                string name = IoName(row);
                string message = name + " I/O를 " + (next ? "SIM 모드로 변경할까요?" : "REAL 모드로 변경할까요?");
                if (QMC.Common.MessageDialog.Show(message, "I/O SIM MODE", MessageBoxButtons.OKCancel) != DialogResult.OK)
                    return;

                ApplySim(row, next);
                SaveIoSettings();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "IO-SIM-TOGGLE", "I/O sim toggle failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ApplySim(DataGridViewRow row, bool isOn)
        {
            try
            {
                if (row == null || _simColumnIndex < 0) return;
                row.Cells[_simColumnIndex].Value = isOn ? "ON" : "OFF";
                ApplySimCellStyle(row.Cells[_simColumnIndex], isOn);
            }
            finally
            {
            }
        }

        private void ToggleRuntimeIoState(int rowIndex)
        {
            try
            {
                if (IsCylinderPage())
                {
                    RefreshCylinderRuntimeState(SelectedCylinderName());
                    return;
                }

                if (rowIndex < 0 || rowIndex >= _grid.Rows.Count)
                    return;

                DataGridViewRow row = _grid.Rows[rowIndex];
                if (row == null || row.IsNewRow)
                    return;

                if (!IsSimOn(row))
                {
                    QMC.Common.MessageDialog.Show(
                        this,
                        "SIM ON 상태에서만 I/O STATE를 직접 변경할 수 있습니다.",
                        "I/O SIM",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                string name = IoName(row);
                bool outputRow = IsOutputRow(row);
                bool current = string.Equals(Convert.ToString(row.Cells[_stateColumnIndex].Value), "ON", StringComparison.OrdinalIgnoreCase);
                bool next = !current;
                string target = outputRow ? "output" : "input";
                string message = name + " " + target + " state를 " + (next ? "ON" : "OFF") + " 으로 변경할까요?";

                if (QMC.Common.MessageDialog.Show(this, message, "I/O SIM", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                if (outputRow)
                    ApplyRuntimeOutputState(row, next);
                else
                    ApplyRuntimeInputState(row, next);

                row.Cells[_stateColumnIndex].Value = next ? "ON" : "OFF";
                EventLogger.Write(EventKind.Event, "QMC", "IO-SIM-STATE",
                    "Runtime I/O state changed. name=" + name + ", state=" + (next ? "ON" : "OFF"));
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "QMC", "IO-SIM-STATE",
                    "Runtime I/O state change failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "I/O SIM", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        private void ApplyRuntimeInputState(DataGridViewRow row, bool state)
        {
            BaseDigitalInput input = FindRuntimeInput(row);
            int module;
            int bit;
            ResolveRowPoint(row, false, out module, out bit);

            if (input == null)
            {
                if (module < 0 || bit < 0)
                    throw new InvalidOperationException(IoName(row) + " input port is not ready.");

                AjinIoScanService.SetSimulatedInput(IoName(row), module, bit, state, false);
                return;
            }

            if (input.Config == null || !input.Config.IsSimulationMode)
                input.Config.IsSimulationMode = true;

            input.SimulateInput(state);
        }

        private void ApplyRuntimeOutputState(DataGridViewRow row, bool state)
        {
            BaseDigitalOutput output = FindRuntimeOutput(row);
            int module;
            int bit;
            ResolveRowPoint(row, true, out module, out bit);

            if (output == null)
            {
                if (module < 0 || bit < 0)
                    throw new InvalidOperationException(IoName(row) + " output port is not ready.");

                AjinIoScanService.SetSimulatedOutput(IoName(row), module, bit, state, false);
                return;
            }

            if (output.Config == null || !output.Config.IsSimulationMode)
                output.Config.IsSimulationMode = true;

            output.Write(state);
        }

        private BaseDigitalInput FindRuntimeInput(DataGridViewRow row)
        {
            try
            {
                string name = IoName(row);
                int module;
                int bit;
                ResolveRowPoint(row, false, out module, out bit);

                var host = FindForm() as Form1;
                if (host != null && host.Machine != null)
                {
                    var collector = new IoControlPage.IoCollector();
                    collector.Scan(host.Machine);

                    foreach (var item in collector.Inputs)
                    {
                        if (item == null || item.Port == null)
                            continue;

                        if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                            return item.Port;

                        if (item.Module == module && item.Bit == bit)
                            return item.Port;
                    }
                }

                DioDefault catalog = AjinIoCatalog.FindInput(name);
                if (catalog != null)
                    return AjinFactory.CreateDigitalInput(catalog);
            }
            catch
            {
            }
            finally
            {
            }

            return null;
        }

        private BaseDigitalOutput FindRuntimeOutput(DataGridViewRow row)
        {
            try
            {
                string name = IoName(row);
                int module;
                int bit;
                ResolveRowPoint(row, true, out module, out bit);

                var host = FindForm() as Form1;
                if (host != null && host.Machine != null)
                {
                    var collector = new IoControlPage.IoCollector();
                    collector.Scan(host.Machine);

                    foreach (var item in collector.Outputs)
                    {
                        if (item == null || item.Port == null)
                            continue;

                        if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                            return item.Port;

                        if (item.Module == module && item.Bit == bit)
                            return item.Port;
                    }
                }

                DioDefault catalog = AjinIoCatalog.FindOutput(name);
                if (catalog != null)
                    return AjinFactory.CreateDigitalOutput(catalog);
            }
            catch
            {
            }
            finally
            {
            }

            return null;
        }

        private void ResolveRowPoint(DataGridViewRow row, bool output, out int module, out int bit)
        {
            module = -1;
            bit = -1;

            try
            {
                string name = IoName(row);
                DioDefault catalog = output ? AjinIoCatalog.FindOutput(name) : AjinIoCatalog.FindInput(name);
                if (catalog != null)
                {
                    module = catalog.Module;
                    bit = catalog.Bit;
                }

                string pointText = output ? CellText(row, "DO") : CellText(row, "DI");
                if (string.IsNullOrWhiteSpace(pointText))
                    return;

                DioMap map = ToDioMap(pointText, output);
                if (map != null)
                {
                    module = map.Module;
                    bit = map.Bit;
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void RefreshCylinderRuntimeState(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return;

                string state = CylinderState(null, name);
                lblCylinderResult.Text = "STATE : " + state;

                if (_stateColumnIndex < 0)
                    return;

                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    if (!string.Equals(IoName(row), name, StringComparison.OrdinalIgnoreCase))
                        continue;

                    row.Cells[_stateColumnIndex].Value = state;
                    break;
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "CYL-STATE-REFRESH",
                    "Cylinder runtime state refresh failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void SaveIoSettings()
        {
            try
            {
                if (_simColumnIndex < 0) return;
                CommitGridEdit();

                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    string name = IoName(row);
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    bool sim = IsSimOn(row);

                    if (IsCylinderPage())
                    {
                        SaveCylinderMapping(row);
                        CylinderSettingsStore.SetSimulation(name, sim);
                    }
                    else if (IsOutputRow(row))
                        IoSettingsStore.SetOutputSimulation(name, sim);
                    else
                        IoSettingsStore.SetInputSimulation(name, sim);
                }

                if (IsCylinderPage())
                {
                    AjinConfigStore.SaveOrThrow();
                    CylinderSettingsStore.Save();
                    CylinderManager.ApplyMappings();
                    ApplyUnitCylinderMappings();
                    RebuildRowIndex();
                    QMC.Common.MessageDialog.Show(
                        this,
                        "Cylinder I/O mapping saved and applied.\r\n" + AjinConfigStore.Path_,
                        "CYLINDER",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                    IoSettingsStore.Save();

                ApplyRuntimeIoSettings();
                EventLogger.Write(EventKind.Event, "QMC", "IO-SIM-SAVE", "I/O simulation settings saved.");
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "QMC", "IO-SIM-SAVE", "I/O simulation settings save failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "I/O SIM MODE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        private void ApplyRuntimeIoSettings()
        {
            try
            {
                var host = FindForm() as Form1;
                if (host == null || host.Machine == null) return;

                var collector = new IoControlPage.IoCollector();
                collector.Scan(host.Machine);

                foreach (var input in collector.Inputs)
                    AjinFactory.ApplyInputSimulation(input.Port, IoSettingsStore.InputSimulation(input.Name, !AjinFactory.IsRealBoardReady));

                foreach (var output in collector.Outputs)
                    AjinFactory.ApplyOutputSimulation(output.Port, IoSettingsStore.OutputSimulation(output.Name, !AjinFactory.IsRealBoardReady));

                foreach (var cylinder in collector.Cylinders)
                    CylinderSettingsStore.Apply(cylinder.Cylinder);

                CylinderManager.ApplySettings();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "IO-SIM-APPLY", "I/O simulation settings apply failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void ApplyUnitCylinderMappings()
        {
            try
            {
                var host = FindForm() as Form1;
                if (host == null || host.Machine == null)
                    return;

                if (host.Machine.OutputStageUnit != null)
                    host.Machine.OutputStageUnit.ApplyCylinderIoMappings();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "QMC", "CYL-MAP-UNIT",
                    "Unit cylinder mapping apply failed: " + ex.Message);
                throw;
            }
        }

        private bool IsCylinderPage()
        {
            return string.Equals(_i18nKey, "set.cylinder", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCylinderMapColumn(string column)
        {
            return string.Equals(column, "FWD DO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(column, "BWD DO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(column, "FWD DI", StringComparison.OrdinalIgnoreCase)
                || string.Equals(column, "BWD DI", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCylinderOutputColumn(string column)
        {
            return string.Equals(column, "FWD DO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(column, "BWD DO", StringComparison.OrdinalIgnoreCase);
        }

        private static object[] OutputOptions()
        {
            var items = new List<object>();
            items.Add(string.Empty);
            foreach (DioDefault item in AjinIoCatalog.DigitalOutputs)
                items.Add(FormatDioOption(item, true));
            return items.ToArray();
        }

        private static object[] InputOptions()
        {
            var items = new List<object>();
            items.Add(string.Empty);
            foreach (DioDefault item in AjinIoCatalog.DigitalInputs)
                items.Add(FormatDioOption(item, false));
            return items.ToArray();
        }

        private void SaveCylinderMapping(DataGridViewRow row)
        {
            try
            {
                if (row == null) return;
                string name = IoName(row);
                if (string.IsNullOrWhiteSpace(name)) return;

                CylMap map;
                if (!AjinConfigStore.Current.Cylinders.TryGetValue(name, out map) || map == null)
                    map = new CylMap();

                map.OutFwd = ToDioMap(CellText(row, "FWD DO"), true, name, "FWD DO", true);
                map.OutBwd = ToDioMap(CellText(row, "BWD DO"), true, name, "BWD DO", true);
                map.InFwd = ToDioMap(CellText(row, "FWD DI"), false, name, "FWD DI", false);
                map.InBwd = ToDioMap(CellText(row, "BWD DI"), false, name, "BWD DI", false);
                map.UseFwdInput = map.InFwd != null;
                map.UseBwdInput = map.InBwd != null;

                AjinConfigStore.Current.Cylinders[name] = map;

                CylinderItemSettings settings = CylinderSettingsStore.Get(name);
                settings.UseFwdInput = map.UseFwdInput;
                settings.UseBwdInput = map.UseBwdInput;
                CylinderSettingsStore.Current.Cylinders[name] = settings;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "QMC", "CYL-MAP-SAVE", "Cylinder mapping row save failed: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private string CellText(DataGridViewRow row, string column)
        {
            try
            {
                int index = Array.FindIndex(_columns, c => string.Equals(c, column, StringComparison.OrdinalIgnoreCase));
                if (index < 0 || row == null || index >= row.Cells.Count) return string.Empty;
                return Convert.ToString(row.Cells[index].Value) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
            }
        }

        private static DioMap ToDioMap(string text, bool isOutput)
        {
            return ToDioMap(text, isOutput, string.Empty, string.Empty, false);
        }

        private static DioMap ToDioMap(string text, bool isOutput, string cylinderName, string columnName, bool required)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    if (required)
                        throw new InvalidOperationException(cylinderName + " " + columnName + " is empty.");

                    return null;
                }

                DioDefault item = FindDioByDisplay(text, isOutput);
                if (item == null)
                    throw new InvalidOperationException(cylinderName + " " + columnName + " I/O not found: " + text);

                return new DioMap
                {
                    No = item.No,
                    Address = item.Address,
                    Module = item.Module,
                    Bit = item.Bit,
                    Nc = item.Nc
                };
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static DioDefault FindDioByDisplay(string text, bool isOutput)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text)) return null;
                string[] parts = text.Trim().Split(new[] { ' ' }, 2);
                string address = parts.Length > 0 ? parts[0] : text.Trim();
                DioDefault[] items = isOutput ? AjinIoCatalog.DigitalOutputs : AjinIoCatalog.DigitalInputs;
                foreach (DioDefault item in items)
                {
                    if (string.Equals(item.Address, address, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(item.Name, text, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(FormatDioOption(item, isOutput), text, StringComparison.OrdinalIgnoreCase))
                        return item;
                }
            }
            catch
            {
            }
            finally
            {
            }

            return null;
        }

        private static string FormatDioOption(DioDefault item, bool isOutput)
        {
            if (item == null) return string.Empty;
            string address = isOutput
                ? AjinIoCatalog.OutputAddress(item.Module, item.Bit)
                : AjinIoCatalog.InputAddress(item.Module, item.Bit);
            return address + " " + item.Name;
        }

        private bool IsOutputRow(DataGridViewRow row)
        {
            try
            {
                if (IsCylinderPage()) return false;
                string index = Convert.ToString(row.Cells[0].Value) ?? string.Empty;
                if (index.StartsWith("DO-", StringComparison.OrdinalIgnoreCase)) return true;
                if (index.StartsWith("DI-", StringComparison.OrdinalIgnoreCase)) return false;

                for (int i = 0; i < _columns.Length; i++)
                {
                    if (string.Equals(_columns[i], "DO", StringComparison.OrdinalIgnoreCase))
                        return true;

                    if (string.Equals(_columns[i], "DI", StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private string IoName(DataGridViewRow row)
        {
            try
            {
                if (row == null) return string.Empty;

                int nameIndex = Array.FindIndex(_columns, c => string.Equals(c, "DESCRIPTION", StringComparison.OrdinalIgnoreCase));
                if (nameIndex >= 0 && nameIndex < row.Cells.Count)
                    return Convert.ToString(row.Cells[nameIndex].Value) ?? string.Empty;

                nameIndex = Array.FindIndex(_columns, c => string.Equals(c, "NAME", StringComparison.OrdinalIgnoreCase));
                if (nameIndex >= 0 && nameIndex < row.Cells.Count)
                    return Convert.ToString(row.Cells[nameIndex].Value) ?? string.Empty;
            }
            catch
            {
            }
            finally
            {
            }

            return string.Empty;
        }

        private bool IsSimOn(DataGridViewRow row)
        {
            try
            {
                if (row == null || _simColumnIndex < 0) return false;
                return string.Equals(Convert.ToString(row.Cells[_simColumnIndex].Value), "ON", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static void ApplySimCellStyle(DataGridViewCell cell, bool isOn)
        {
            try
            {
                cell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                cell.Style.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
                cell.Style.BackColor = isOn ? Color.FromArgb(212, 245, 222) : Color.White;
                cell.Style.ForeColor = isOn ? Color.FromArgb(20, 120, 60) : Color.FromArgb(80, 80, 80);
                cell.Style.SelectionBackColor = isOn ? Color.FromArgb(160, 220, 180) : SystemColors.Highlight;
                cell.Style.SelectionForeColor = isOn ? Color.FromArgb(20, 80, 40) : SystemColors.HighlightText;
            }
            finally
            {
            }
        }

        private void SubscribeIoScan()
        {
            UnsubscribeIoScan();
            if (_stateColumnIndex < 0) return;

            _subscribedService = AjinIoScanService.Current;
            if (_subscribedService != null)
                _subscribedService.IoStatusUpdated += OnIoStatusUpdated;
        }

        private void UnsubscribeIoScan()
        {
            if (_subscribedService != null)
                _subscribedService.IoStatusUpdated -= OnIoStatusUpdated;
            _subscribedService = null;
        }

        private void OnIoStatusUpdated(AjinIoSnapshot snapshot)
        {
            if (snapshot == null || IsDisposed || _stateColumnIndex < 0) return;

            // 스캔 루프는 모든 I/O 포인트마다(수십~수백 개) 매 주기 이 이벤트를 발생시킨다.
            // 이 페이지와 무관한 스냅샷은 UI 스레드로 보내기 전에 백그라운드 스레드에서 즉시 버려야
            // BeginInvoke 폭주로 인한 UI 멈춤을 막을 수 있다.
            if (!IsRelevantSnapshot(snapshot)) return;

            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<AjinIoSnapshot>(OnIoStatusUpdated), snapshot); } catch { }
                return;
            }

            UpdateStateCells(snapshot);
        }

        private bool IsRelevantSnapshot(AjinIoSnapshot snapshot)
        {
            var index = _rowIndex;
            if (index == null || index.Count == 0) return false;

            if (string.Equals(_i18nKey, "set.cylinder", StringComparison.OrdinalIgnoreCase))
            {
                if (snapshot.IsOutput) return false;
                return index.ContainsKey(PointKey(snapshot.Module, snapshot.Bit));
            }

            string address = snapshot.IsOutput
                ? AjinIoCatalog.OutputAddress(snapshot.Module, snapshot.Bit)
                : AjinIoCatalog.InputAddress(snapshot.Module, snapshot.Bit);

            return (!string.IsNullOrEmpty(snapshot.Name) && index.ContainsKey(snapshot.Name))
                || (!string.IsNullOrEmpty(address) && index.ContainsKey(address));
        }

        private void UpdateStateCells(AjinIoSnapshot snapshot)
        {
            if (string.Equals(_i18nKey, "set.cylinder", StringComparison.OrdinalIgnoreCase))
            {
                UpdateCylinderRows(snapshot);
                return;
            }

            string address = snapshot.IsOutput
                ? AjinIoCatalog.OutputAddress(snapshot.Module, snapshot.Bit)
                : AjinIoCatalog.InputAddress(snapshot.Module, snapshot.Bit);
            string state = snapshot.IsOn ? "ON" : "OFF";

            foreach (DataGridViewRow row in MatchingRows(snapshot.Name, address))
            {
                if (row.IsNewRow) continue;
                var cell = row.Cells[_stateColumnIndex];
                if (!string.Equals(Convert.ToString(cell.Value), state, StringComparison.OrdinalIgnoreCase))
                    cell.Value = state;
            }
        }

        private void UpdateCylinderRows(AjinIoSnapshot snapshot)
        {
            if (snapshot.IsOutput) return;

            List<DataGridViewRow> rows;
            if (!_rowIndex.TryGetValue(PointKey(snapshot.Module, snapshot.Bit), out rows) || rows == null)
                return;

            foreach (DataGridViewRow row in rows)
            {
                if (row.IsNewRow || row.Cells.Count < 2) continue;
                string name = IoName(row);
                var cyl = AjinIoCatalog.FindCylinder(name);
                if (cyl == null) continue;
                CylMap map;
                AjinConfigStore.Current.Cylinders.TryGetValue(name, out map);

                string state = CylinderState(map, name);
                var cell = row.Cells[_stateColumnIndex];
                if (!string.Equals(Convert.ToString(cell.Value), state, StringComparison.OrdinalIgnoreCase))
                    cell.Value = state;
            }
        }

        // 행 인덱스를 다시 만든다. 그리드가 비어 있거나 STATE 컬럼이 없으면 빈 인덱스를 둔다.
        private void RebuildRowIndex()
        {
            var index = new Dictionary<string, List<DataGridViewRow>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (_stateColumnIndex >= 0)
                {
                    bool cylinder = string.Equals(_i18nKey, "set.cylinder", StringComparison.OrdinalIgnoreCase);
                    foreach (DataGridViewRow row in _grid.Rows)
                    {
                        if (row.IsNewRow) continue;
                        if (cylinder)
                            IndexCylinderRow(index, row);
                        else
                            IndexDioRow(index, row);
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "QMC", "IO-INDEX", "Row index rebuild failed: " + ex.Message);
            }
            _rowIndex = index;
        }

        private void IndexDioRow(Dictionary<string, List<DataGridViewRow>> index, DataGridViewRow row)
        {
            // DIO 행은 어떤 셀이든 이름 또는 주소와 정확히 일치하면 매칭 대상이 되므로,
            // 모든 셀 텍스트(및 "주소 이름" 형태의 첫 토큰)를 키로 등록해 둔다.
            for (int i = 0; i < row.Cells.Count; i++)
            {
                string value = Convert.ToString(row.Cells[i].Value);
                if (string.IsNullOrEmpty(value)) continue;
                AddIndex(index, value, row);

                int space = value.IndexOf(' ');
                if (space > 0)
                    AddIndex(index, value.Substring(0, space), row);
            }
        }

        private void IndexCylinderRow(Dictionary<string, List<DataGridViewRow>> index, DataGridViewRow row)
        {
            string name = IoName(row);
            CylMap map;
            if (!AjinConfigStore.Current.Cylinders.TryGetValue(name, out map) || map == null) return;

            if (map.UseFwdInput && map.InFwd != null)
                AddIndex(index, PointKey(map.InFwd.Module, map.InFwd.Bit), row);
            if (map.UseBwdInput && map.InBwd != null)
                AddIndex(index, PointKey(map.InBwd.Module, map.InBwd.Bit), row);
        }

        private static void AddIndex(Dictionary<string, List<DataGridViewRow>> index, string key, DataGridViewRow row)
        {
            if (string.IsNullOrEmpty(key)) return;
            List<DataGridViewRow> list;
            if (!index.TryGetValue(key, out list))
            {
                list = new List<DataGridViewRow>();
                index[key] = list;
            }
            if (!list.Contains(row))
                list.Add(row);
        }

        private IEnumerable<DataGridViewRow> MatchingRows(string name, string address)
        {
            var index = _rowIndex;
            var seen = new HashSet<DataGridViewRow>();
            var result = new List<DataGridViewRow>();
            List<DataGridViewRow> rows;
            if (!string.IsNullOrEmpty(name) && index.TryGetValue(name, out rows))
                foreach (var row in rows) if (seen.Add(row)) result.Add(row);
            if (!string.IsNullOrEmpty(address) && index.TryGetValue(address, out rows))
                foreach (var row in rows) if (seen.Add(row)) result.Add(row);
            return result;
        }

        private static string PointKey(int module, int bit)
        {
            return "P:" + module + ":" + bit;
        }

        private static string CylinderState(CylMap item, string cylinderName)
        {
            string fwdLabel = "FWD";
            string bwdLabel = "BWD";
            try
            {
                if (!string.IsNullOrWhiteSpace(cylinderName))
                {
                    CylinderItemSettings settings = CylinderSettingsStore.Get(cylinderName);
                    if (settings != null)
                    {
                        if (!string.IsNullOrWhiteSpace(settings.FwdLabel)) fwdLabel = settings.FwdLabel.Trim();
                        if (!string.IsNullOrWhiteSpace(settings.BwdLabel)) bwdLabel = settings.BwdLabel.Trim();
                    }
                }
            }
            catch
            {
            }

            try
            {
                BaseCylinder cylinder = CylinderManager.Get(cylinderName);
                if (cylinder != null)
                {
                    if (cylinder.IsFwd && !cylinder.IsBwd) return fwdLabel;
                    if (!cylinder.IsFwd && cylinder.IsBwd) return bwdLabel;
                    if (cylinder.IsFwd && cylinder.IsBwd) return fwdLabel + "/" + bwdLabel;
                }
            }
            catch
            {
            }

            if (item == null) return "OFF";

            bool hasFwd = item.UseFwdInput && item.InFwd != null;
            bool hasBwd = item.UseBwdInput && item.InBwd != null;

            if (!hasFwd && !hasBwd) return "OFF";

            bool fwd;
            bool bwd;
            if (hasFwd && hasBwd)
            {
                fwd = IsOn(item.InFwd);
                bwd = IsOn(item.InBwd);
            }
            else if (hasFwd)
            {
                fwd = IsOn(item.InFwd);
                bwd = !fwd;
            }
            else
            {
                bwd = IsOn(item.InBwd);
                fwd = !bwd;
            }

            if (fwd && !bwd) return fwdLabel;
            if (!fwd && bwd) return bwdLabel;
            if (fwd && bwd) return fwdLabel + "/" + bwdLabel;
            return "OFF";
        }

        private static bool IsOn(DioDefault item)
        {
            var service = AjinIoScanService.Current;
            if (service == null || item == null) return false;
            var snapshot = service.GetLatest(item.Module, item.Bit, false);
            return snapshot != null && snapshot.ErrorCode == 0 && snapshot.IsOn;
        }

        private static bool IsOn(DioMap item)
        {
            var service = AjinIoScanService.Current;
            if (service == null || item == null) return false;
            var snapshot = service.GetLatest(item.Module, item.Bit, false);
            return snapshot != null && snapshot.ErrorCode == 0 && snapshot.IsOn;
        }
    }
}
