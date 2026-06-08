using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Motion.SharedRailX;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class SharedRailXSetupDialog : Form
    {
        private readonly MachineController _controller;
        private readonly ToolTip _toolTip;
        private SharedRailXConfigDocument _document;
        private bool _running;

        public SharedRailXSetupDialog(MachineController controller)
        {
            _controller = controller;
            InitializeComponent();
            _toolTip = new ToolTip();
            InitializeHelpText();
        }

        private void SharedRailXSetupDialog_Load(object sender, EventArgs e)
        {
            ReloadDocument();
            InitializeHeadSubSelectors();
            RefreshStatus();
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            if (_running) return;
            ReloadDocument();
            RefreshStatus();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_running) return;
            if (!ReadGridToDocument()) return;
            SharedRailXConfigStore.SaveDocument(_document);
            lblStatus.Text = "Saved: " + SharedRailXConfigStore.Path_;
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (_running) return;
            if (!ReadGridToDocument()) return;
            SharedRailXConfigStore.SaveDocument(_document);
            if (_controller != null)
                _controller.ReloadSharedRailXConfig();
            lblStatus.Text = "Applied to runtime.";
            RefreshStatus();
        }

        private void btnValidate_Click(object sender, EventArgs e)
        {
            if (_running) return;
            if (!ReadGridToDocument()) return;
            SharedRailXConfigStore.SaveDocument(_document);
            if (_controller != null)
                _controller.ReloadSharedRailXConfig();

            int checkedCount = 0;
            foreach (DataGridViewRow row in grid.Rows)
            {
                SharedRailXAxis axis;
                if (!TryGetRowAxis(row, out axis))
                    continue;

                string reason;
                bool ok = ValidateTarget(axis, ReadDouble(row.Cells[colTarget.Index].Value, 0.0), out reason);
                SetRowStatus(row, ok ? "OK" : "BLOCK", ok ? Color.FromArgb(220, 245, 225) : Color.FromArgb(255, 225, 225), reason);
                checkedCount++;
            }

            lblStatus.Text = "Validated " + checkedCount + " axis targets.";
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            ShowSharedRailXHelp();
        }

        private async void btnMoveSelected_Click(object sender, EventArgs e)
        {
            await RunSelectedMoveAsync();
        }

        private async void btnMoveAll_Click(object sender, EventArgs e)
        {
            await RunAllMoveAsync();
        }

        private async void btnMoveHeadSub_Click(object sender, EventArgs e)
        {
            await RunHeadSubMoveAsync();
        }

        private async void btnHomeSelected_Click(object sender, EventArgs e)
        {
            await RunSelectedHomeAsync();
        }

        private async void btnHomeAll_Click(object sender, EventArgs e)
        {
            await RunAllHomeAsync();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshStatus();
        }

        private void cboHeadAxis_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyHeadSubRules();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void InitializeHelpText()
        {
            grid.ShowCellToolTips = true;

            colBodyMin.ToolTipText = "축의 실제 몸체가 Rail 좌표에서 축 중심보다 얼마나 왼쪽/마이너스 방향으로 더 차지하는지(mm)입니다.";
            colBodyMax.ToolTipText = "축의 실제 몸체가 Rail 좌표에서 축 중심보다 얼마나 오른쪽/플러스 방향으로 더 차지하는지(mm)입니다.";
            colOrigin.ToolTipText = "축 좌표 0이 SharedRail 기준 좌표에서 어디에 있는지 나타내는 기준 오프셋(mm)입니다.";
            colScale.ToolTipText = "축 좌표를 SharedRail 좌표로 환산할 때 곱하는 배율입니다. 일반 X축은 보통 1입니다.";
            colSafety.ToolTipText = "이 축에만 적용할 최소 이격 거리(mm)입니다. 비우면 Default Safety 값을 사용합니다.";
            colVelocity.ToolTipText = "이 창에서 테스트 이동할 때 사용할 속도(mm/sec)입니다. 저장하면 파일에 기록되고 이동 실행 시 이 값을 사용합니다.";

            _toolTip.SetToolTip(cboHeadAxis, "주 축입니다. Move Head/Sub 실행 시 항상 이동 대상에 포함됩니다.");
            _toolTip.SetToolTip(chkSubInputVision, "Head 축과 같이 이동할 Sub 축입니다. Head 선택에 따라 선택 가능 여부가 달라집니다.");
            _toolTip.SetToolTip(chkSubOutputVision, "Head 축과 같이 이동할 Sub 축입니다. Head 선택에 따라 선택 가능 여부가 달라집니다.");
            _toolTip.SetToolTip(chkSubFrontPicker, "Head 축과 같이 이동할 Sub 축입니다. Head 선택에 따라 선택 가능 여부가 달라집니다.");
            _toolTip.SetToolTip(chkSubRearPicker, "Head 축과 같이 이동할 Sub 축입니다. Head 선택에 따라 선택 가능 여부가 달라집니다.");
            _toolTip.SetToolTip(btnHelp, "SharedRailX 설정값 설명을 표시합니다.");
        }

        private void ShowSharedRailXHelp()
        {
            string message =
                "SharedRailX 설정값 설명\r\n\r\n" +
                "Body Min\r\n" +
                "- 축의 중심 위치 기준으로 장비 몸체가 마이너스 방향으로 얼마나 튀어나와 있는지 나타냅니다.\r\n" +
                "- 예: Body Min = -100 이면, 축 중심보다 왼쪽/마이너스 방향으로 100mm까지 몸체가 있다고 봅니다.\r\n\r\n" +
                "Body Max\r\n" +
                "- 축의 중심 위치 기준으로 장비 몸체가 플러스 방향으로 얼마나 튀어나와 있는지 나타냅니다.\r\n" +
                "- 예: Body Max = 100 이면, 축 중심보다 오른쪽/플러스 방향으로 100mm까지 몸체가 있다고 봅니다.\r\n\r\n" +
                "Rail Origin\r\n" +
                "- 해당 축의 0 위치가 SharedRail 공통 좌표에서 어디에 있는지 나타내는 기준 위치입니다.\r\n" +
                "- 같은 축 좌표 0이라도 Rail Origin이 다르면 SharedRail 위 실제 위치는 다르게 계산됩니다.\r\n" +
                "- 예: FrontPickerX Rail Origin = 300 이면 FrontPickerX 축 0은 SharedRail 좌표 300mm 위치로 봅니다.\r\n\r\n" +
                "Scale\r\n" +
                "- 축 좌표를 SharedRail 공통 좌표로 변환할 때 곱하는 값입니다.\r\n" +
                "- 일반적으로 mm 단위 X축은 1을 사용합니다.\r\n" +
                "- 축 방향이 반대라면 -1 같은 값이 필요할 수 있지만, 실제 장비 좌표계 확인 후 설정해야 합니다.\r\n\r\n" +
                "Velocity\r\n" +
                "- 이 창에서 테스트 이동할 때 사용할 속도입니다. 단위는 mm/sec입니다.\r\n" +
                "- 저장하면 shared_rail_x.json 파일에 남고, 이동 버튼을 누를 때도 먼저 파일에 저장한 뒤 이 값을 사용합니다.\r\n\r\n" +
                "계산 개념\r\n" +
                "- Rail 중심 좌표 = Rail Origin + (축 위치 * Scale)\r\n" +
                "- 몸체 점유 범위 = Rail 중심 좌표 + Body Min ~ Rail 중심 좌표 + Body Max\r\n" +
                "- 두 축의 몸체 점유 범위가 Safety 거리보다 가까우면 이동을 막습니다.\r\n\r\n" +
                "주의\r\n" +
                "- 이 값은 충돌 인터락 계산 기준입니다. 실제 기구 치수와 축 좌표 방향이 맞지 않으면 정상 이동도 막히거나, 위험한 이동이 허용될 수 있습니다.";

            QMC.Common.MessageDialog.Show(this, message, "SharedRailX 설정값 설명",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ReloadDocument()
        {
            _document = SharedRailXConfigStore.LoadDocumentOrCreateDefault();
            chkPathCheck.Checked = _document.EnablePathCheck;
            chkSameVelocity.Checked = _document.RequireSameVelocityForGroupMove;
            txtDefaultSafety.Text = FormatDouble(_document.DefaultSafetyDistance);
            LoadGrid();
            lblPath.Text = SharedRailXConfigStore.Path_;
            lblStatus.Text = "Loaded.";
        }

        private void LoadGrid()
        {
            grid.Rows.Clear();
            if (_document == null || _document.Axes == null)
                return;

            foreach (SharedRailXAxisGeometryRow item in _document.Axes.OrderBy(x => AxisOrder(x.Axis)))
            {
                int index = grid.Rows.Add(
                    item.Axis,
                    "",
                    FormatDouble(item.TestTargetPosition),
                    FormatDouble(item.TestVelocity),
                    FormatDouble(item.BodyOffsetMin),
                    FormatDouble(item.BodyOffsetMax),
                    FormatDouble(item.RailOriginOffset),
                    FormatDouble(item.PositionScale),
                    item.SafetyDistance.HasValue ? FormatDouble(item.SafetyDistance.Value) : "",
                    "Waiting",
                    "");
                grid.Rows[index].Tag = item;
            }
        }

        private bool ReadGridToDocument()
        {
            try
            {
                if (_document == null)
                    _document = SharedRailXConfigStore.CreateDefaultDocument();

                _document.EnablePathCheck = chkPathCheck.Checked;
                _document.RequireSameVelocityForGroupMove = chkSameVelocity.Checked;
                _document.DefaultSafetyDistance = ReadDouble(txtDefaultSafety.Text, 10.0);

                foreach (DataGridViewRow row in grid.Rows)
                {
                    SharedRailXAxisGeometryRow item = row.Tag as SharedRailXAxisGeometryRow;
                    if (item == null)
                        continue;

                    item.TestTargetPosition = ReadDouble(row.Cells[colTarget.Index].Value, item.TestTargetPosition);
                    item.TestVelocity = ReadDouble(row.Cells[colVelocity.Index].Value, item.TestVelocity);
                    item.BodyOffsetMin = ReadDouble(row.Cells[colBodyMin.Index].Value, item.BodyOffsetMin);
                    item.BodyOffsetMax = ReadDouble(row.Cells[colBodyMax.Index].Value, item.BodyOffsetMax);
                    item.RailOriginOffset = ReadDouble(row.Cells[colOrigin.Index].Value, item.RailOriginOffset);
                    item.PositionScale = ReadDouble(row.Cells[colScale.Index].Value, item.PositionScale);

                    string safetyText = Convert.ToString(row.Cells[colSafety.Index].Value);
                    item.SafetyDistance = string.IsNullOrWhiteSpace(safetyText)
                        ? (double?)null
                        : ReadDouble(safetyText, _document.DefaultSafetyDistance);
                }

                return true;
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "SharedRailX setting read failed:\n" + ex.Message,
                    "SharedRailX", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            finally
            {
            }
        }

        private void RefreshStatus()
        {
            try
            {
                if (_controller == null || _controller.SharedRailX == null)
                    return;

                IReadOnlyList<SharedRailXAxisSetting> settings = _controller.SharedRailX.GetAxisSettings();
                foreach (DataGridViewRow row in grid.Rows)
                {
                    SharedRailXAxis axis;
                    if (!TryGetRowAxis(row, out axis))
                        continue;

                    SharedRailXAxisSetting setting = settings.FirstOrDefault(x => x.RailAxis == axis);
                    if (setting == null || setting.Axis == null)
                        continue;

                    row.Cells[colCurrent.Index].Value = FormatDouble(setting.Axis.ActualPosition);
                    row.Cells[colStatus.Index].Value = setting.Axis.IsAlarm ? "Alarm" :
                        setting.Axis.IsMoving ? "Moving" :
                        setting.Axis.IsHomeDone ? "HomeDone" : "Ready";
                    row.Cells[colMessage.Index].Value = "servo=" + (setting.Axis.IsServoOn ? "ON" : "OFF");
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private async Task RunSelectedMoveAsync()
        {
            DataGridViewRow row = grid.CurrentRow;
            if (row == null)
                return;

            SharedRailXAxis axis;
            if (!TryGetRowAxis(row, out axis))
                return;

            await RunAsync("Move " + axis, async () =>
            {
                if (!ReadGridToDocument()) return -1;
                SaveDocumentAndReloadRuntime();

                double target = ReadDouble(row.Cells[colTarget.Index].Value, 0.0);
                double velocity = ReadVelocity(row);
                int result = await _controller.SharedRailX.MoveAsync(axis, target, velocity);
                SetRowStatus(row, result == 0 ? "Done" : "Failed", result == 0 ? Color.FromArgb(220, 245, 225) : Color.FromArgb(255, 225, 225),
                    result == 0 ? "" : "result=" + result);
                return result;
            });
        }

        private async Task RunAllMoveAsync()
        {
            await RunAsync("Move all SharedRailX", async () =>
            {
                if (!ReadGridToDocument()) return -1;
                SaveDocumentAndReloadRuntime();

                var plan = SharedRailXMovePlan.Create("SharedRailX setup dialog move all", ReadDefaultVelocity())
                    .UseMode(SharedRailXMoveMode.AjinMultiPosition);

                foreach (DataGridViewRow row in grid.Rows)
                {
                    SharedRailXAxis axis;
                    if (!TryGetRowAxis(row, out axis))
                        continue;
                    plan.Add(axis, ReadDouble(row.Cells[colTarget.Index].Value, 0.0), ReadVelocity(row));
                }

                int result = await _controller.MoveSharedRailXAsync(plan);
                foreach (DataGridViewRow row in grid.Rows)
                    SetRowStatus(row, result == 0 ? "Done" : "Failed", result == 0 ? Color.FromArgb(220, 245, 225) : Color.FromArgb(255, 225, 225),
                        result == 0 ? "" : "result=" + result);
                return result;
            });
        }

        private async Task RunHeadSubMoveAsync()
        {
            await RunAsync("Move Head/Sub SharedRailX axes", async () =>
            {
                if (!ReadGridToDocument()) return -1;
                SaveDocumentAndReloadRuntime();

                List<DataGridViewRow> rows = GetHeadSubRows();
                if (rows.Count == 0)
                {
                    QMC.Common.MessageDialog.Show(this, "Head/Sub 선택된 축이 없습니다.", "SharedRailX",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 0;
                }

                var plan = SharedRailXMovePlan.Create("SharedRailX setup dialog move Head/Sub", ReadDefaultVelocity())
                    .UseMode(rows.Count >= 2 ? SharedRailXMoveMode.AjinMultiPosition : SharedRailXMoveMode.SoftwareParallel);

                foreach (DataGridViewRow row in rows)
                {
                    SharedRailXAxis axis;
                    if (!TryGetRowAxis(row, out axis))
                        continue;

                    plan.Add(axis, ReadDouble(row.Cells[colTarget.Index].Value, 0.0), ReadVelocity(row));
                    SetRowStatus(row, "Moving", Color.FromArgb(255, 245, 205), "");
                }

                int result = await _controller.MoveSharedRailXAsync(plan);
                foreach (DataGridViewRow row in rows)
                    SetRowStatus(row, result == 0 ? "Done" : "Failed",
                        result == 0 ? Color.FromArgb(220, 245, 225) : Color.FromArgb(255, 225, 225),
                        result == 0 ? "" : "result=" + result);

                return result;
            });
        }

        private async Task RunSelectedHomeAsync()
        {
            DataGridViewRow row = grid.CurrentRow;
            if (row == null)
                return;

            SharedRailXAxis axis;
            if (!TryGetRowAxis(row, out axis))
                return;

            await RunAsync("Home " + axis, async () =>
            {
                int result = await _controller.InitializeAxisAsync(AxisName(axis));
                SetRowStatus(row, result == 0 ? "Home Done" : "Home Failed", result == 0 ? Color.FromArgb(220, 245, 225) : Color.FromArgb(255, 225, 225),
                    result == 0 ? "" : _controller.LastActionFailureMessage);
                return result;
            });
        }

        private async Task RunAllHomeAsync()
        {
            await RunAsync("Home all SharedRailX axes", async () =>
            {
                int firstFail = 0;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    SharedRailXAxis axis;
                    if (!TryGetRowAxis(row, out axis))
                        continue;

                    SetRowStatus(row, "Homing", Color.FromArgb(255, 245, 205), "");
                    int result = await _controller.InitializeAxisAsync(AxisName(axis));
                    SetRowStatus(row, result == 0 ? "Home Done" : "Home Failed",
                        result == 0 ? Color.FromArgb(220, 245, 225) : Color.FromArgb(255, 225, 225),
                        result == 0 ? "" : _controller.LastActionFailureMessage);
                    if (firstFail == 0 && result != 0)
                        firstFail = result;
                    if (result != 0)
                        break;
                }

                return firstFail;
            });
        }

        private async Task RunAsync(string actionName, Func<Task<int>> action)
        {
            if (_running || action == null || _controller == null || _controller.SharedRailX == null)
                return;

            try
            {
                _running = true;
                SetButtonsEnabled(false);
                lblStatus.Text = actionName + " running...";
                int result = await action();
                RefreshStatus();
                lblStatus.Text = actionName + (result == 0 ? " complete." : " failed. result=" + result);
            }
            catch (Exception ex)
            {
                lblStatus.Text = actionName + " error.";
                QMC.Common.MessageDialog.Show(this, actionName + " failed:\n" + ex.Message,
                    "SharedRailX", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _running = false;
                SetButtonsEnabled(true);
            }
        }

        private bool ValidateTarget(SharedRailXAxis axis, double target, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (_controller == null || _controller.SharedRailX == null)
                {
                    reason = "Controller is not ready.";
                    return false;
                }

                BaseAxis baseAxis = ResolveAxis(axis);
                if (baseAxis == null)
                {
                    reason = "Axis is not mapped.";
                    return false;
                }

                return _controller.SharedRailX.VerifySingleAxisMove(baseAxis, target, out reason);
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private void SaveDocumentAndReloadRuntime()
        {
            SharedRailXConfigStore.SaveDocument(_document);
            if (_controller != null)
                _controller.ReloadSharedRailXConfig();
            lblStatus.Text = "Saved: " + SharedRailXConfigStore.Path_;
        }

        private double ReadVelocity(DataGridViewRow row)
        {
            double velocity = row != null
                ? ReadDouble(row.Cells[colVelocity.Index].Value, 5.0)
                : 5.0;
            return velocity > 0.0 ? velocity : 5.0;
        }

        private double ReadDefaultVelocity()
        {
            foreach (DataGridViewRow row in grid.Rows)
                return ReadVelocity(row);
            return 5.0;
        }

        private BaseAxis ResolveAxis(SharedRailXAxis axis)
        {
            if (_controller == null || _controller.Machine == null)
                return null;

            CDT320_Machine machine = _controller.Machine;
            switch (axis)
            {
                case SharedRailXAxis.InputVisionX:
                    return machine.InputStageUnit != null ? machine.InputStageUnit.CameraX : null;
                case SharedRailXAxis.FrontPickerX:
                    return machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerX : null;
                case SharedRailXAxis.RearPickerX:
                    return machine.PickerRearUnit != null ? machine.PickerRearUnit.PickerX : null;
                case SharedRailXAxis.OutputVisionX:
                    return machine.OutputStageUnit != null ? machine.OutputStageUnit.OutputCameraX : null;
                default:
                    return null;
            }
        }

        private static string AxisName(SharedRailXAxis axis)
        {
            switch (axis)
            {
                case SharedRailXAxis.InputVisionX: return "InputVisionX";
                case SharedRailXAxis.FrontPickerX: return "FrontPickerX";
                case SharedRailXAxis.RearPickerX: return "RearPickerX";
                case SharedRailXAxis.OutputVisionX: return "OutputVisionX";
                default: return axis.ToString();
            }
        }

        private static bool TryGetRowAxis(DataGridViewRow row, out SharedRailXAxis axis)
        {
            axis = SharedRailXAxis.InputVisionX;
            if (row == null)
                return false;
            object value = row.Cells["colAxis"].Value;
            return value != null && Enum.TryParse(value.ToString(), true, out axis);
        }

        private void InitializeHeadSubSelectors()
        {
            if (cboHeadAxis.SelectedIndex < 0 && cboHeadAxis.Items.Count > 0)
                cboHeadAxis.SelectedIndex = 0;

            ApplyHeadSubRules();
        }

        private void ApplyHeadSubRules()
        {
            SharedRailXAxis head = GetHeadAxis();
            ConfigureSubCheckBox(chkSubInputVision, head, SharedRailXAxis.InputVisionX, IsAllowedSubAxis(head, SharedRailXAxis.InputVisionX));
            ConfigureSubCheckBox(chkSubOutputVision, head, SharedRailXAxis.OutputVisionX, IsAllowedSubAxis(head, SharedRailXAxis.OutputVisionX));
            ConfigureSubCheckBox(chkSubFrontPicker, head, SharedRailXAxis.FrontPickerX, IsAllowedSubAxis(head, SharedRailXAxis.FrontPickerX));
            ConfigureSubCheckBox(chkSubRearPicker, head, SharedRailXAxis.RearPickerX, IsAllowedSubAxis(head, SharedRailXAxis.RearPickerX));
        }

        private static void ConfigureSubCheckBox(CheckBox checkBox, SharedRailXAxis head, SharedRailXAxis axis, bool allowed)
        {
            checkBox.Enabled = allowed && axis != head;
            if (!checkBox.Enabled)
                checkBox.Checked = false;
        }

        private static bool IsAllowedSubAxis(SharedRailXAxis head, SharedRailXAxis sub)
        {
            if (head == sub)
                return false;

            switch (head)
            {
                case SharedRailXAxis.InputVisionX:
                case SharedRailXAxis.OutputVisionX:
                    return sub == SharedRailXAxis.FrontPickerX || sub == SharedRailXAxis.RearPickerX;
                case SharedRailXAxis.FrontPickerX:
                    return sub == SharedRailXAxis.InputVisionX ||
                        sub == SharedRailXAxis.RearPickerX ||
                        sub == SharedRailXAxis.OutputVisionX;
                case SharedRailXAxis.RearPickerX:
                    return sub == SharedRailXAxis.InputVisionX ||
                        sub == SharedRailXAxis.FrontPickerX ||
                        sub == SharedRailXAxis.OutputVisionX;
                default:
                    return false;
            }
        }

        private SharedRailXAxis GetHeadAxis()
        {
            SharedRailXAxis axis;
            string text = Convert.ToString(cboHeadAxis.SelectedItem);
            if (Enum.TryParse(text, true, out axis))
                return axis;
            return SharedRailXAxis.InputVisionX;
        }

        private List<DataGridViewRow> GetHeadSubRows()
        {
            var selected = new HashSet<SharedRailXAxis>();
            selected.Add(GetHeadAxis());

            AddSubAxisIfChecked(selected, chkSubInputVision, SharedRailXAxis.InputVisionX);
            AddSubAxisIfChecked(selected, chkSubOutputVision, SharedRailXAxis.OutputVisionX);
            AddSubAxisIfChecked(selected, chkSubFrontPicker, SharedRailXAxis.FrontPickerX);
            AddSubAxisIfChecked(selected, chkSubRearPicker, SharedRailXAxis.RearPickerX);

            var rows = new List<DataGridViewRow>();
            foreach (SharedRailXAxis axis in selected)
            {
                DataGridViewRow row = FindRow(axis);
                if (row != null)
                    rows.Add(row);
            }

            return rows;
        }

        private static void AddSubAxisIfChecked(HashSet<SharedRailXAxis> selected, CheckBox checkBox, SharedRailXAxis axis)
        {
            if (checkBox.Enabled && checkBox.Checked)
                selected.Add(axis);
        }

        private DataGridViewRow FindRow(SharedRailXAxis targetAxis)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                SharedRailXAxis axis;
                if (TryGetRowAxis(row, out axis) && axis == targetAxis)
                    return row;
            }

            return null;
        }

        private static int AxisOrder(string axis)
        {
            SharedRailXAxis parsed;
            if (!Enum.TryParse(axis, true, out parsed))
                return 99;
            return (int)parsed;
        }

        private static double ReadDouble(object value, double fallback)
        {
            string text = Convert.ToString(value);
            double parsed;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                return parsed;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
                return parsed;
            return fallback;
        }

        private static string FormatDouble(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static void SetRowStatus(DataGridViewRow row, string status, Color color, string message)
        {
            if (row == null)
                return;
            row.Cells[row.DataGridView.Columns["colStatus"].Index].Value = status;
            row.Cells[row.DataGridView.Columns["colMessage"].Index].Value = message ?? string.Empty;
            row.DefaultCellStyle.BackColor = color;
        }

        private void SetButtonsEnabled(bool enabled)
        {
            btnReload.Enabled = enabled;
            btnSave.Enabled = enabled;
            btnApply.Enabled = enabled;
            btnValidate.Enabled = enabled;
            btnHelp.Enabled = enabled;
            btnMoveSelected.Enabled = enabled;
            btnMoveHeadSub.Enabled = enabled;
            btnMoveAll.Enabled = enabled;
            btnHomeSelected.Enabled = enabled;
            btnHomeAll.Enabled = enabled;
            btnRefresh.Enabled = enabled;
            btnClose.Enabled = enabled;
        }
    }
}
