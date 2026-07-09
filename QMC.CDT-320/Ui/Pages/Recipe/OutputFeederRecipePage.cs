using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT320;
using QMC.CDT320.Interlocks;
using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>Output Feeder 레시피에서 OutputFeederUnit을 조작하는 화면입니다.</summary>
    public partial class OutputFeederRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private sealed class OutputFeederTeachingPosition
        {
            public string DisplayName { get; private set; }
            public string PositionName { get; private set; }
            public Func<OutputFeederUnit, double> Getter { get; private set; }
            public Action<OutputFeederUnit, double> Setter { get; private set; }

            public OutputFeederTeachingPosition(string displayName, string positionName, Func<OutputFeederUnit, double> getter, Action<OutputFeederUnit, double> setter)
            {
                DisplayName = displayName;
                PositionName = positionName;
                Getter = getter;
                Setter = setter;
            }
        }

        private static readonly OutputFeederTeachingPosition[] TeachingPositions =
        {
            new OutputFeederTeachingPosition("AVOID POSITION", "Avoid", unit => unit.Recipe.AvoidPosition, (unit, value) => unit.Recipe.AvoidPosition = value),
            new OutputFeederTeachingPosition("GOOD CASSETTE LOAD POSITION", "GoodCassetteLoadPosition", unit => unit.Recipe.GoodCassetteLoadPosition, (unit, value) => unit.Recipe.GoodCassetteLoadPosition = value),
            new OutputFeederTeachingPosition("GOOD CASSETTE UNLOAD POSITION", "GoodCassetteUnloadPosition", unit => unit.Recipe.GoodCassetteUnloadPosition, (unit, value) => unit.Recipe.GoodCassetteUnloadPosition = value),
            new OutputFeederTeachingPosition("GOOD CASSETTE EXCHANGE POSITION", "GoodCassetteExchangePosition", unit => unit.Recipe.GoodCassetteExchangePosition, (unit, value) => unit.Recipe.GoodCassetteExchangePosition = value),
            new OutputFeederTeachingPosition("GOOD WAFER LOAD AVOID POSITION", "GoodWaferLoadAvoidPosition", unit => unit.Recipe.GoodWaferLoadAvoidPosition, (unit, value) => unit.Recipe.GoodWaferLoadAvoidPosition = value),
            new OutputFeederTeachingPosition("GOOD WAFER LOAD POSITION", "GoodWaferLoadPosition", unit => unit.Recipe.GoodWaferLoadPosition, (unit, value) => unit.Recipe.GoodWaferLoadPosition = value),
            new OutputFeederTeachingPosition("GOOD WAFER UNLOAD AVOID POSITION", "GoodWaferUnloadAvoidPosition", unit => unit.Recipe.GoodWaferUnloadAvoidPosition, (unit, value) => unit.Recipe.GoodWaferUnloadAvoidPosition = value),
            new OutputFeederTeachingPosition("GOOD WAFER UNLOAD POSITION", "GoodWaferUnloadPosition", unit => unit.Recipe.GoodWaferUnloadPosition, (unit, value) => unit.Recipe.GoodWaferUnloadPosition = value),
            new OutputFeederTeachingPosition("GOOD WAFER BARCODE POSITION", "GoodWaferBarcodePosition", unit => unit.Recipe.GoodWaferBarcodePosition, (unit, value) => unit.Recipe.GoodWaferBarcodePosition = value),
            new OutputFeederTeachingPosition("NG CASSETTE LOAD POSITION", "NGCassetteLoadPosition", unit => unit.Recipe.NGCassetteLoadPosition, (unit, value) => unit.Recipe.NGCassetteLoadPosition = value),
            new OutputFeederTeachingPosition("NG CASSETTE UNLOAD POSITION", "NGCassetteUnloadPosition", unit => unit.Recipe.NGCassetteUnloadPosition, (unit, value) => unit.Recipe.NGCassetteUnloadPosition = value),
            new OutputFeederTeachingPosition("NG CASSETTE EXCHANGE POSITION", "NGCassetteExchangePosition", unit => unit.Recipe.NGCassetteExchangePosition, (unit, value) => unit.Recipe.NGCassetteExchangePosition = value),
            new OutputFeederTeachingPosition("NG WAFER LOAD AVOID POSITION", "NGWaferLoadAvoidPosition", unit => unit.Recipe.NGWaferLoadAvoidPosition, (unit, value) => unit.Recipe.NGWaferLoadAvoidPosition = value),
            new OutputFeederTeachingPosition("NG WAFER LOAD POSITION", "NGWaferLoadPosition", unit => unit.Recipe.NGWaferLoadPosition, (unit, value) => unit.Recipe.NGWaferLoadPosition = value),
            new OutputFeederTeachingPosition("NG WAFER UNLOAD AVOID POSITION", "NGWaferUnloadAvoidPosition", unit => unit.Recipe.NGWaferUnloadAvoidPosition, (unit, value) => unit.Recipe.NGWaferUnloadAvoidPosition = value),
            new OutputFeederTeachingPosition("NG WAFER UNLOAD POSITION", "NGWaferUnloadPosition", unit => unit.Recipe.NGWaferUnloadPosition, (unit, value) => unit.Recipe.NGWaferUnloadPosition = value),
            new OutputFeederTeachingPosition("NG WAFER BARCODE POSITION", "NGWaferBarcodePosition", unit => unit.Recipe.NGWaferBarcodePosition, (unit, value) => unit.Recipe.NGWaferBarcodePosition = value)
        };

        private OutputFeederUnit _outputFeederUnit;
        private readonly Timer _refreshTimer = new Timer();
        private readonly List<ActionButton> _actionButtons = new List<ActionButton>();
        private string _titleI18n = "recipe.outputFeeder";

        public OutputFeederRecipePage() : this("recipe.outputFeeder")
        {
        }

        public OutputFeederRecipePage(string titleI18n)
        {
            try
            {
                _titleI18n = titleI18n;
                InitializeComponent();
                if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                    return;

                ApplyRecipeTheme();
                ConfigureRuntimeBehavior();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(ex.Message, "Output Feeder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);
                if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

                ResolveUnit();
                BindParameterGrids();
                BindIoPanel();
                BindJogPanel();
                RefreshView();
                if (ShouldRefreshVisible(this))
                    _refreshTimer.Start();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            try { if (ShouldRefreshVisible(this)) _refreshTimer.Start(); else _refreshTimer.Stop(); } catch { }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                _refreshTimer.Stop();
                if (jogAxisMoveControl != null)
                    jogAxisMoveControl.StopAllAsync(true).GetAwaiter().GetResult();
            }
            catch
            {
            }
            finally
            {
                base.OnHandleDestroyed(e);
            }
        }

        private void ConfigureRuntimeBehavior()
        {
            try
            {
                lblHeader.Tag = "i18n:" + _titleI18n;
                lblHeader.Text = Lang.T(_titleI18n);

                _refreshTimer.Interval = 250;
                _refreshTimer.Tick += RefreshTimer_Tick;

                optionParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
                waitParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;

                ConfigureActionButtons();
                BindParameterGridMenus();

                grpIo.ContextMenuStrip = new ContextMenuStrip();
                grpIo.ContextMenuStrip.Items.Add("Output feeder DI 상태를 다시 읽습니다.", null, IoRefresh_Click);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Configure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ConfigureActionButtons()
        {
            try
            {
                // 공용 MANUAL ACTION 판넬에 티칭 위치별 버튼 등록 (2열, 행 수 자동)
                var actions = new List<ManualActionItem>();
                foreach (OutputFeederTeachingPosition position in TeachingPositions)
                {
                    OutputFeederTeachingPosition captured = position;
                    actions.Add(ManualActionItem.Create(captured.DisplayName, () => ConfirmTeachingMoveAsync(captured)));
                }

                manualActionPanel.ColumnCount = 2;
                manualActionPanel.SetItems(actions);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-FEEDER", "ConfigureActionButtons failed: " + ex.Message);
            }
            finally
            {
            }
        }


        private void RegisterActionMoveButton(ActionButton button, OutputFeederTeachingPosition position)
        {
            if (button == null || position == null)
                return;

            button.Text = position.DisplayName;
            button.Tag = position;
            button.Cursor = Cursors.Hand;
            StyleActionButton(button);
            if (!_actionButtons.Contains(button))
                _actionButtons.Add(button);
        }

        private void StyleActionButton(ActionButton button)
        {
            button.BackColor = Color.FromArgb(88, 94, 103);
            button.ForeColor = Color.White;
            button.Font = new Font("Malgun Gothic", 8F, FontStyle.Bold);
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!ShouldRefreshVisible(this))
                {
                    _refreshTimer.Stop();
                    return;
                }

                RefreshView();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void IoRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                BindIoPanel();
                RefreshView();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder I/O", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ResolveUnit()
        {
            try
            {
                var machine = FindMachine();
                _outputFeederUnit = machine != null ? machine.OutputFeederUnit : null;
                SetEnabledState(_outputFeederUnit != null);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Resolve", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private CDT320_Machine FindMachine()
        {
            try
            {
                foreach (Form form in Application.OpenForms)
                {
                    var host = form as Form1;
                    if (host != null)
                        return host.Machine;
                }

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

        private Form1 FindHostForm()
        {
            try
            {
                foreach (Form form in Application.OpenForms)
                {
                    var host = form as Form1;
                    if (host != null)
                        return host;
                }

                return FindForm() as Form1;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private void SetEnabledState(bool enabled)
        {
            try
            {
                manualActionPanel.SetButtonsEnabled(enabled);

                jogPositionListControl.Enabled = enabled;
                jogAxisMoveControl.Enabled = enabled;
                jogSpeedControl.Enabled = enabled;
            }
            catch
            {
            }
            finally
            {
            }
        }

        // ===== ACTION 버튼 (OutputCassette와 동일 구성, Designer에서 Click 배선) =====
        private async void btnReadyMove_Click(object sender, EventArgs e)
        {
            await TeachingMoveButton_ClickAsync(sender);
        }

        private async void btnGoodLoadingMove_Click(object sender, EventArgs e)
        {
            await TeachingMoveButton_ClickAsync(sender);
        }

        private async void btnGoodUnloadingMove_Click(object sender, EventArgs e)
        {
            await TeachingMoveButton_ClickAsync(sender);
        }

        private async void btnGoodSlotStartMove_Click(object sender, EventArgs e)
        {
            await TeachingMoveButton_ClickAsync(sender);
        }

        private async void btnGoodSlotEndMove_Click(object sender, EventArgs e)
        {
            await TeachingMoveButton_ClickAsync(sender);
        }

        private async void btnNgLoadingMove_Click(object sender, EventArgs e)
        {
            await TeachingMoveButton_ClickAsync(sender);
        }

        private async void btnNgUnloadingMove_Click(object sender, EventArgs e)
        {
            await TeachingMoveButton_ClickAsync(sender);
        }

        private async void btnNgSlotStartMove_Click(object sender, EventArgs e)
        {
            await TeachingMoveButton_ClickAsync(sender);
        }

        private async void btnNgSlotEndMove_Click(object sender, EventArgs e)
        {
            await TeachingMoveButton_ClickAsync(sender);
        }

        private async void TeachingMoveButton_Click(object sender, EventArgs e)
        {
            await TeachingMoveButton_ClickAsync(sender);
        }

        private async Task TeachingMoveButton_ClickAsync(object sender)
        {
            try
            {
                var button = sender as Control;
                var position = button != null ? button.Tag as OutputFeederTeachingPosition : null;
                if (position == null)
                    return;

                await ConfirmTeachingMoveAsync(position);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async Task ConfirmTeachingMoveAsync(OutputFeederTeachingPosition position)
        {
            if (position == null)
                return;

            await ConfirmMoveAsync(
                position.DisplayName,
                () => _outputFeederUnit.MoveBinFeederYToTeachingPosition(
                    position.PositionName,
                    jogAxisMoveControl.SelectedSpeedType,
                    jogAxisMoveControl.GetSelectedSpeed(_outputFeederUnit.FeederY)));
        }

        private async Task ConfirmMoveAsync(string actionName, Func<Task<int>> move)
        {
            try
            {
                if (_outputFeederUnit == null) return;

                // 이동 대상 축(Output Feeder Y)의 HOME END(IsHomeDone) 미완료면 차단.
                if (_outputFeederUnit.FeederY != null && !_outputFeederUnit.FeederY.IsHomeDone)
                {
                    string homeMsg = actionName + " 불가: Output Feeder Y 축 HOME END(원점복귀)가 완료되지 않았습니다.";
                    QMC.Common.Alarms.AlarmManager.Raise(QMC.Common.Alarms.AlarmSeverity.Warning, "OUTPUT-FEEDER", "UI", homeMsg);
                    QMC.Common.MessageDialog.Show(this, homeMsg, "Output Feeder Move", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult result = QMC.Common.MessageDialog.Show(
                    this, actionName + " 진행하시겠습니까?", "Output Feeder Move", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    EventLogger.Write(EventKind.Event, "UI", "OUTPUT-FEEDER", actionName + " canceled.");
                    return;
                }

                await RunSafeAsync(async () =>
                {
                    int r = await move();
                    if (r != 0)
                        return r;

                    return await _outputFeederUnit.WaitBinFeederYMoveDone(_outputFeederUnit.FeederY.Setup.MoveTimeoutMs) ? 0 : -1;
                }, actionName);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, actionName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async Task RunSafeAsync(Func<Task<int>> action, string actionName)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                int result;
                using (MotionGuardRuntime.BeginManualSequenceProcessMove("OutputFeederRecipePage." + actionName))
                {
                    result = await action();
                }
                if (result != 0)
                {
                    string msg = _outputFeederUnit != null ? _outputFeederUnit.LastBinFeederMoveFailureMessage : null;
                    string detail = string.IsNullOrEmpty(msg) ? "" : Environment.NewLine + "사유 : " + msg;
                    QMC.Common.MessageDialog.Show(this, actionName + " 실패" + detail, "Output Feeder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, actionName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                RefreshView();
            }
        }

        private void BindParameterGridMenus()
        {
            try
            {
                // 우클릭 메뉴 대신, 티칭 포지션 행의 MOVE/TEACH 버튼으로 이동/티칭 수행
                optionParameterGrid.ParameterMoveRequested += OptionParameterGrid_MoveRequested;
                optionParameterGrid.ParameterTeachRequested += OptionParameterGrid_TeachRequested;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-FEEDER", "BindParameterGridMenus failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void OptionParameterGrid_MoveRequested(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                if (e == null || e.Item == null)
                    return;

                string positionName = GetSelectedTeachingPositionName();
                if (string.IsNullOrWhiteSpace(positionName))
                    return;

                if (ConfirmMoveToPositionSpeed("Output Feeder Move", e.Item.Key))
                    await MoveByPositionName(positionName);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-FEEDER", "Move button failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void OptionParameterGrid_TeachRequested(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                if (e == null || e.Item == null)
                    return;

                string positionName = GetSelectedTeachingPositionName();
                if (string.IsNullOrWhiteSpace(positionName))
                    return;

                TeachPosition(positionName);
                SaveCurrentRecipeData();
                RefreshView();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-FEEDER", "Teach button failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Teach", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private bool ConfirmMoveToPositionSpeed(string title, string actionName)
        {
            JogSpeedType speedType;
            if (!ManualMoveGuard.ConfirmMoveSpeed(this, title, actionName, out speedType))
            {
                EventLogger.Write(EventKind.Event, "UI", "OUTPUT-FEEDER", actionName + " canceled.");
                return false;
            }

            jogAxisMoveControl.SetSelectedSpeedType(speedType);
            return true;
        }

        private string GetSelectedTeachingPositionName()
        {
            try
            {
                var item = optionParameterGrid.SelectedItem;
                if (item == null)
                    return string.Empty;

                foreach (var position in TeachingPositions)
                {
                    if (string.Equals(item.Key, position.DisplayName, StringComparison.OrdinalIgnoreCase))
                        return position.PositionName;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-FEEDER", "GetSelectedTeachingPositionName failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
            finally
            {
            }
        }

        private async Task MoveByPositionName(string positionName)
        {
            try
            {
                if (_outputFeederUnit == null)
                    return;

                await RunSafeAsync(async () =>
                {
                    int moveResult = await _outputFeederUnit.MoveBinFeederYToTeachingPosition(
                        positionName,
                        jogAxisMoveControl.SelectedSpeedType,
                        jogAxisMoveControl.GetSelectedSpeed(_outputFeederUnit.FeederY));
                    if (moveResult != 0)
                        return moveResult;

                    return await _outputFeederUnit.WaitBinFeederYMoveDone(_outputFeederUnit.FeederY.Setup.MoveTimeoutMs) ? 0 : -1;
                }, positionName);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void TeachPosition(string positionName)
        {
            try
            {
                if (_outputFeederUnit == null)
                    return;

                _outputFeederUnit.TeachBinFeederYPosition(positionName);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Teach", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindParameterGrids()
        {
            try
            {
                if (_outputFeederUnit == null)
                    return;

                var unit = _outputFeederUnit;
                var items = new List<ParameterGridItem>();
                foreach (var position in TeachingPositions)
                {
                    OutputFeederTeachingPosition captured = position;
                    var teachItem = AxisDouble(captured.DisplayName, ParameterGridScope.Recipe, () => captured.Getter(unit), v => captured.Setter(unit, v));
                    teachItem.SupportsTeaching = true;   // 행에 MOVE/TEACH 버튼 표시(티칭 포지션)
                    items.Add(teachItem);
                }

                items.Add(ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => unit.Setup.IsSimulationMode, v => unit.Setup.IsSimulationMode = v));
                items.Add(ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v));
                optionParameterGrid.SetItems(items);

                waitParameterGrid.AutoFitParentGroupHeight = true;   // WAIT 그룹 높이를 내용에 맞춰 자동 조정 (스크롤 없이 전 항목 표시)
                waitParameterGrid.SetItems(new[]
                {
                    ParameterGridItem.Int("MOVE TIMEOUT", "ms", ParameterGridScope.Setup, () => unit.FeederY.Setup.MoveTimeoutMs, v => unit.FeederY.Setup.MoveTimeoutMs = Math.Max(0, v))
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-FEEDER", "BindParameterGrids failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Parameters", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindIoPanel()
        {
            try
            {
                if (_outputFeederUnit == null)
                    return;

                var unit = _outputFeederUnit;

                // 2열 열우선 배치 (접두사/CHECK 생략): [1열] RING CHECK + LIFT 세트, [2열] OVERLOAD + CLAMP 세트
                ioCylinderPanel.ColumnCount = 2;
                ioCylinderPanel.SetItems(new[]
                {
                    IoCylinderItem.Input("RING CHECK", () => unit.IsBinFeederRingCheck()),
                    IoCylinderItem.Input("UP", () => unit.IsFeederUp()),
                    IoCylinderItem.Input("DOWN", () => unit.IsFeederDown()),
                    IoCylinderItem.Cylinder("LIFT", unit.FeederUpDownCyl, "UP", "DOWN"),

                    IoCylinderItem.Input("OVERLOAD", () => unit.IsFeederOverload()),
                    IoCylinderItem.Input("CLAMP", () => unit.IsBinFeederClamp()),
                    IoCylinderItem.Input("UNCLAMP", () => unit.IsFeederUnclamped()),
                    IoCylinderItem.Cylinder("CLAMP", unit.FeederClampCyl, "CLAMP", "UNCLAMP")
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-FEEDER", "BindIoPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder I/O", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindJogPanel()
        {
            try
            {
                if (_outputFeederUnit == null)
                    return;

                var unit = _outputFeederUnit;

                JogAxisItem axisItem = JogAxisItem.Single("BinFeederY", unit.FeederY, AxisUnitConverter.DisplayUnitFor(unit.FeederY), 1.0, "Y+", "Y-").WithControlKind(JogAxisControlKind.Vertical);
                axisItem.StepMoveAsync = (item, direction, speedType, customSpeed, axisStepDistance) =>
                    unit.JogStepAsync(unit.FeederY, direction, speedType, customSpeed, axisStepDistance);
                axisItem.ContinuousMoveAsync = (item, direction, speedType, customSpeed) =>
                    unit.JogContinuousAsync(unit.FeederY, direction, speedType, customSpeed);
                axisItem.StopAsync = item =>
                    unit.StopJogAsync(unit.FeederY);

                jogAxisMoveControl.SpeedControl = jogSpeedControl;
                jogAxisMoveControl.LayoutMode = JogAxisMoveLayoutMode.AxisColumns;
                jogAxisMoveControl.ShowCurrentSpeedMode = true;
                jogAxisMoveControl.ButtonAreaMinHeight = 164;
                jogAxisMoveControl.ButtonAreaMaxHeight = 164;
                jogAxisMoveControl.ButtonAreaMinWidth = 222;
                jogAxisMoveControl.ButtonAreaMaxWidth = 222;
                jogAxisMoveControl.SetItems(new[] { axisItem });
                jogPositionListControl.SetItems(new[] { axisItem });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-FEEDER", "BindJogPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private ParameterGridItem AxisDouble(string displayName, ParameterGridScope scope, Func<double> getter, Action<double> setter)
        {
            ParameterGridItem item = ParameterGridItem.Double(
                displayName,
                AxisUnitConverter.DisplayUnitFor(_outputFeederUnit.FeederY),
                scope,
                () => AxisUnitConverter.ToDisplay(getter(), _outputFeederUnit.FeederY),
                v => setter(AxisUnitConverter.FromDisplay(v, _outputFeederUnit.FeederY)));
            item.UnitGetter = () => AxisUnitConverter.DisplayUnitFor(_outputFeederUnit.FeederY);
            return item;
        }

        private void ParameterGrid_ParameterValueChanged(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                if (e != null && e.Item != null && e.Item.Scope == ParameterGridScope.Recipe)
                    SaveCurrentRecipeData();
                else
                    SaveCurrentSettingsData();

                RefreshView();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-FEEDER", "Parameter save failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Parameter Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void SaveCurrentRecipeData()
        {
            try
            {
                var host = FindHostForm();
                if (host == null || string.IsNullOrWhiteSpace(host.CurrentRecipeName))
                    return;

                host.SaveMachineRecipe(host.CurrentRecipeName);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void SaveCurrentSettingsData()
        {
            try
            {
                var host = FindHostForm();
                host?.SaveMachineSettings();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void RefreshView()
        {
            try
            {
                if (_outputFeederUnit == null)
                    return;

                optionParameterGrid.RefreshValues();
                waitParameterGrid.RefreshValues();
                ioCylinderPanel.RefreshStates();
                jogPositionListControl.RefreshState();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ApplyRecipeTheme()
        {
            try
            {
                Color bg = Color.FromArgb(207, 210, 214);
                BackColor = bg;

                lblHeader.BackColor = Color.FromArgb(64, 64, 64);
                lblHeader.ForeColor = Color.White;
                lblHeader.Font = new Font("Malgun Gothic", 11F, FontStyle.Bold);

                foreach (var g in new[] { grpActions, grpIo, grpOptions, grpWait, grpJog, grpSpeed })
                {
                    g.BackColor = Color.FromArgb(245, 245, 245);
                    g.Font = new Font("Malgun Gothic", 10F, FontStyle.Bold);
                }

            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Theme", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }
    }
}
