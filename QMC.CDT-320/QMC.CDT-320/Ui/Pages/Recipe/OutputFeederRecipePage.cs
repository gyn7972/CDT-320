using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT320;
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
            _actionButtons.Clear();

            RegisterActionMoveButton(btnReadyMove, TeachingPositions[0]);
            RegisterActionMoveButton(btnGoodLoadingMove, TeachingPositions[1]);
            RegisterActionMoveButton(btnGoodUnloadingMove, TeachingPositions[2]);
            RegisterActionMoveButton(btnGoodCassetteExchangeMove, TeachingPositions[3]);
            RegisterActionMoveButton(btnGoodWaferLoadAvoidMove, TeachingPositions[4]);
            RegisterActionMoveButton(btnGoodSlotStartMove, TeachingPositions[5]);
            RegisterActionMoveButton(btnGoodWaferUnloadAvoidMove, TeachingPositions[6]);
            RegisterActionMoveButton(btnGoodSlotEndMove, TeachingPositions[7]);
            RegisterActionMoveButton(btnGoodWaferBarcodeMove, TeachingPositions[8]);
            RegisterActionMoveButton(btnNgLoadingMove, TeachingPositions[9]);
            RegisterActionMoveButton(btnNgUnloadingMove, TeachingPositions[10]);
            RegisterActionMoveButton(btnNgCassetteExchangeMove, TeachingPositions[11]);
            RegisterActionMoveButton(btnNgWaferLoadAvoidMove, TeachingPositions[12]);
            RegisterActionMoveButton(btnNgSlotStartMove, TeachingPositions[13]);
            RegisterActionMoveButton(btnNgWaferUnloadAvoidMove, TeachingPositions[14]);
            RegisterActionMoveButton(btnNgSlotEndMove, TeachingPositions[15]);
            RegisterActionMoveButton(btnNgWaferBarcodeMove, TeachingPositions[16]);
        }

        private IEnumerable<ActionButton> GetActionButtons()
        {
            if (_actionButtons.Count > 0)
                return _actionButtons;

            return new[] { btnGoodLoadingMove, btnGoodUnloadingMove, btnGoodSlotStartMove, btnGoodSlotEndMove, btnNgLoadingMove, btnNgUnloadingMove, btnNgSlotStartMove, btnNgSlotEndMove, btnReadyMove };
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
                foreach (var button in GetActionButtons())
                    button.Enabled = enabled;

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

            await ConfirmMoveAsync(position.DisplayName, () => _outputFeederUnit.MoveBinFeederYToTeachingPosition(position.PositionName, IsFineMove()));
        }

        private async Task ConfirmMoveAsync(string actionName, Func<Task<int>> move)
        {
            try
            {
                if (_outputFeederUnit == null) return;

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
                int result = await action();
                if (result != 0)
                    QMC.Common.MessageDialog.Show(this, actionName + " 실패", "Output Feeder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                var menu = new ContextMenuStrip();
                menu.Items.Add("Move To Position", null, async (s, e) =>
                {
                    string positionName = GetSelectedTeachingPositionName();
                    if (!string.IsNullOrWhiteSpace(positionName))
                        await MoveByPositionName(positionName);
                });
                menu.Items.Add("Teach Current Position", null, (s, e) =>
                {
                    string positionName = GetSelectedTeachingPositionName();
                    if (string.IsNullOrWhiteSpace(positionName))
                        return;

                    TeachPosition(positionName);
                    SaveCurrentRecipeData();
                    RefreshView();
                });

                optionParameterGrid.ContextMenuStrip = menu;
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
                    int moveResult = await _outputFeederUnit.MoveBinFeederYToTeachingPosition(positionName, IsFineMove());
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
                    items.Add(ParameterGridItem.Micron(captured.DisplayName, ParameterGridScope.Recipe, () => captured.Getter(unit), v => captured.Setter(unit, v)));
                }

                items.Add(ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => unit.Setup.IsSimulationMode, v => unit.Setup.IsSimulationMode = v));
                items.Add(ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v));
                optionParameterGrid.SetItems(items);

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

                ioCylinderPanel.SetItems(new[]
                {
                    IoCylinderItem.Input("FEEDER UP CHECK", () => unit.IsFeederUp()),
                    IoCylinderItem.Input("FEEDER DOWN CHECK", () => unit.IsFeederDown()),
                    IoCylinderItem.Input("FEEDER UNCLAMP CHECK", () => unit.IsFeederUnclamped()),
                    IoCylinderItem.Input("FEEDER RING CHECK", () => unit.IsBinFeederRingCheck()),
                    IoCylinderItem.Input("FEEDER OVERLOAD CHECK", () => unit.IsFeederOverload()),
                    IoCylinderItem.Cylinder("FEEDER LIFT", unit.FeederUpDownCyl),
                    IoCylinderItem.Cylinder("FEEDER CLAMP", unit.FeederClampCyl)
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

                JogAxisItem axisItem = JogAxisItem.Single("BinFeederY", unit.FeederY, "um", 1000.0, "Y+", "Y-").WithControlKind(JogAxisControlKind.Vertical);
                axisItem.StepMoveAsync = async (item, direction, speedType, customSpeed, axisStepDistance) =>
                {
                    double target = unit.FeederY.ActualPosition + (direction * axisStepDistance);
                    int r = await unit.MoveBinFeederY(target, false);
                    if (r != 0)
                        return r;

                    return await unit.WaitBinFeederYMoveDone(unit.FeederY.Setup.MoveTimeoutMs) ? 0 : -1;
                };
                axisItem.ContinuousMoveAsync = (item, direction, speedType, customSpeed) =>
                {
                    unit.ManualMoveBinFeederYJog(direction, customSpeed);
                    return Task.FromResult(0);
                };
                axisItem.StopAsync = item =>
                {
                    unit.ManualStopBinFeederY();
                    return Task.FromResult(0);
                };

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

        private bool IsFineMove()
        {
            try
            {
                return true;
            }
            catch
            {
                return true;
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
                rootLayout.BackColor = bg;
                contentLayout.BackColor = bg;
                leftLayout.BackColor = bg;
                centerLayout.BackColor = bg;
                rightLayout.BackColor = bg;

                lblHeader.BackColor = Color.FromArgb(64, 64, 64);
                lblHeader.ForeColor = Color.White;
                lblHeader.Font = new Font("Malgun Gothic", 11F, FontStyle.Bold);

                foreach (var g in new[] { grpActions, grpIo, grpOptions, grpWait, grpJog, grpSpeed })
                {
                    g.BackColor = Color.FromArgb(245, 245, 245);
                    g.Font = new Font("Malgun Gothic", 10F, FontStyle.Bold);
                }

                Color actionColor = Color.FromArgb(88, 94, 103);
                foreach (var b in new[] { btnGoodLoadingMove, btnGoodUnloadingMove, btnGoodSlotStartMove, btnGoodSlotEndMove, btnNgLoadingMove, btnNgUnloadingMove, btnNgSlotStartMove, btnNgSlotEndMove, btnReadyMove })
                {
                    b.BackColor = actionColor;
                    b.ForeColor = Color.White;
                    b.Font = new Font("Malgun Gothic", 8F, FontStyle.Bold);
                }

                Color groupHeaderBg = Color.FromArgb(245, 245, 245);
                Color groupHeaderFg = Color.FromArgb(64, 64, 64);
                Font groupFont = new Font("Malgun Gothic", 8.5F, FontStyle.Bold);
                foreach (var lbl in new[] { lblGoodGroup, lblNgGroup, lblCommonGroup })
                {
                    lbl.BackColor = groupHeaderBg;
                    lbl.ForeColor = groupHeaderFg;
                    lbl.Font = groupFont;
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
