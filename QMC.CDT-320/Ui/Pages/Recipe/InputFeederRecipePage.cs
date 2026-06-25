using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT320;
using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>Input Feeder 레시피에서 InputFeederUnit을 조작하는 화면입니다.</summary>
    public partial class InputFeederRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private sealed class FeederTeachingPosition
        {
            public string DisplayName { get; private set; }
            public string PositionName { get; private set; }
            public Func<InputFeederUnit, double> Getter { get; private set; }
            public Action<InputFeederUnit, double> Setter { get; private set; }

            public FeederTeachingPosition(string displayName, string positionName, Func<InputFeederUnit, double> getter, Action<InputFeederUnit, double> setter)
            {
                DisplayName = displayName;
                PositionName = positionName;
                Getter = getter;
                Setter = setter;
            }
        }

        private static readonly FeederTeachingPosition[] TeachingPositions =
        {
            new FeederTeachingPosition("AVOID POSITION", "Avoid", unit => unit.Recipe.AvoidPosition, (unit, value) => unit.Recipe.AvoidPosition = value),
            new FeederTeachingPosition("CASSETTE LOAD POSITION", "CassetteLoad", unit => unit.Recipe.CassetteLoadPosition, (unit, value) => unit.Recipe.CassetteLoadPosition = value),
            new FeederTeachingPosition("CASSETTE UNLOAD POSITION", "CassetteUnload", unit => unit.Recipe.CassetteUnloadPosition, (unit, value) => unit.Recipe.CassetteUnloadPosition = value),
            new FeederTeachingPosition("CASSETTE EXCHANGE POSITION", "CassetteExchange", unit => unit.Recipe.CassetteExchangePosition, (unit, value) => unit.Recipe.CassetteExchangePosition = value),
            new FeederTeachingPosition("WAFER LOAD AVOID POSITION", "WaferLoadAvoid", unit => unit.Recipe.WaferLoadAvoidPosition, (unit, value) => unit.Recipe.WaferLoadAvoidPosition = value),
            new FeederTeachingPosition("WAFER LOAD POSITION", "WaferLoad", unit => unit.Recipe.WaferLoadPosition, (unit, value) => unit.Recipe.WaferLoadPosition = value),
            new FeederTeachingPosition("WAFER UNLOAD AVOID POSITION", "WaferUnloadAvoid", unit => unit.Recipe.WaferUnloadAvoidPosition, (unit, value) => unit.Recipe.WaferUnloadAvoidPosition = value),
            new FeederTeachingPosition("WAFER UNLOAD POSITION", "WaferUnload", unit => unit.Recipe.WaferUnloadPosition, (unit, value) => unit.Recipe.WaferUnloadPosition = value),
            new FeederTeachingPosition("WAFER BARCODE POSITION", "WaferBarcode", unit => unit.Recipe.WaferBarcodePosition, (unit, value) => unit.Recipe.WaferBarcodePosition = value)
        };

        private InputFeederUnit _inputFeederUnit;
        private readonly List<ActionButton> _teachingMoveButtons = new List<ActionButton>();
        private readonly Timer _refreshTimer = new Timer();
        private readonly ToolTip _toolTip = new ToolTip();
        private string _titleI18n = "recipe.inputFeeder";
        /// <summary>InputFeederRecipePage를 생성합니다.</summary>
        public InputFeederRecipePage() : this("recipe.inputFeeder")
        {
        }

        /// <summary>제목 i18n 키를 지정하여 InputFeederRecipePage를 생성합니다.</summary>
        public InputFeederRecipePage(string titleI18n)
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
                QMC.Common.MessageDialog.Show(ex.Message, "Input Feeder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        /// <summary>화면 로드 시 Unit을 연결하고 화면 갱신을 시작합니다.</summary>
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        /// <summary>핸들이 해제될 때 타이머와 조그 동작을 정지합니다.</summary>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                _refreshTimer.Stop();
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
                ConfigureActionMoveButtons();
                BindParameterGridMenus();

                grpIo.ContextMenuStrip = new ContextMenuStrip();
                grpIo.ContextMenuStrip.Items.Add("Input feeder DI 상태를 다시 읽습니다.", null, IoRefresh_Click);

                _toolTip.SetToolTip(optionParameterGrid, "Input Feeder Y 티칭 위치를 설정합니다.");
                _toolTip.SetToolTip(waitParameterGrid, "Input Feeder Y 이동 대기 시간을 설정합니다.");
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Configure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
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
                RefreshView();
            }
            catch
            {
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
                _inputFeederUnit = machine != null ? machine.InputFeederUnit : null;

                SetEnabledState(_inputFeederUnit != null);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Resolve", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    var host = form as QMC.CDT_320.Form1;
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

        private void SetEnabledState(bool enabled)
        {
            try
            {
                btnLoadingMove.Enabled = enabled;
                btnUnloadingMove.Enabled = enabled;
                btnReadyMove.Enabled = enabled;
                btnSlotLoadingMove.Enabled = enabled;
                btnSlotUnloadingMove.Enabled = enabled;
                foreach (var button in _teachingMoveButtons)
                    button.Enabled = enabled;

                jogPositionListControl.Enabled = enabled;
                jogAxisMoveControl.Enabled = enabled;
                jogSpeedControl.Enabled = enabled;
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Enable", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ConfigureActionMoveButtons()
        {
            try
            {
                _teachingMoveButtons.Clear();

                RegisterActionMoveButton(btnReadyMove, TeachingPositions[0]);
                RegisterActionMoveButton(btnLoadingMove, TeachingPositions[1]);
                RegisterActionMoveButton(btnUnloadingMove, TeachingPositions[2]);
                RegisterActionMoveButton(btnSlotLoadingMove, TeachingPositions[3]);
                RegisterActionMoveButton(btnSlotUnloadingMove, TeachingPositions[4]);
                RegisterActionMoveButton(btnWaferLoadPositionMove, TeachingPositions[5]);
                RegisterActionMoveButton(btnWaferUnloadAvoidPositionMove, TeachingPositions[6]);
                RegisterActionMoveButton(btnWaferUnloadPositionMove, TeachingPositions[7]);
                RegisterActionMoveButton(btnWaferBarcodePositionMove, TeachingPositions[8]);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-FEEDER", "ConfigureActionMoveButtons failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Action", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void RegisterActionMoveButton(ActionButton button, FeederTeachingPosition position)
        {
            try
            {
                if (button == null || position == null)
                    return;

                button.Text = position.DisplayName;
                button.Tag = position;
                button.Cursor = Cursors.Hand;
                ApplyActionButtonStyle(button);
                if (!_teachingMoveButtons.Contains(button))
                    _teachingMoveButtons.Add(button);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async void TeachingMoveButton_Click(object sender, EventArgs e)
        {
            try
            {
                var button = sender as Control;
                var position = button != null ? button.Tag as FeederTeachingPosition : null;
                if (position == null)
                    return;

                await ConfirmTeachingMoveAsync(position);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async Task ConfirmTeachingMoveAsync(FeederTeachingPosition position)
        {
            try
            {
                if (position == null)
                    return;

                await ConfirmFeederMoveAsync(position.DisplayName, () => _inputFeederUnit.MoveWaferFeederYToTeachingPosition(position.PositionName, IsFineMove()));
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task ConfirmFeederMoveAsync(string actionName, Func<Task<int>> move)
        {
            try
            {
                if (_inputFeederUnit == null) return;

                DialogResult result = QMC.Common.MessageDialog.Show(
                    this, actionName + " 진행하시겠습니까?", "Input Feeder Move", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    EventLogger.Write(EventKind.Event, "UI", "INPUT-FEEDER", actionName + " canceled.");
                    return;
                }

                await RunSafeAsync(async () =>
                {
                    int moveResult = await move();
                    if (moveResult != 0)
                        return moveResult;

                    return await _inputFeederUnit.WaitWaferFeederYMoveDone(_inputFeederUnit.FeederY.Setup.MoveTimeoutMs) ? 0 : -1;
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
                {
                    string msg = _inputFeederUnit != null ? _inputFeederUnit.LastWaferFeederMoveFailureMessage : null;
                    string detail = string.IsNullOrEmpty(msg) ? "" : Environment.NewLine + "사유 : " + msg;
                    QMC.Common.MessageDialog.Show(this, actionName + " 실패" + detail, "Input Feeder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-FEEDER", "BindParameterGridMenus failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-FEEDER", "GetSelectedTeachingPositionName failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                if (_inputFeederUnit == null) return;

                await RunSafeAsync(async () =>
                {
                    int moveResult = await _inputFeederUnit.MoveWaferFeederYToTeachingPosition(positionName, IsFineMove());
                    if (moveResult != 0)
                        return moveResult;

                    return await _inputFeederUnit.WaitWaferFeederYMoveDone(_inputFeederUnit.FeederY.Setup.MoveTimeoutMs) ? 0 : -1;
                }, positionName);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void TeachPosition(string positionName)
        {
            try
            {
                if (_inputFeederUnit == null) return;

                _inputFeederUnit.TeachWaferFeederYPosition(positionName);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Teach", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindParameterGrids()
        {
            try
            {
                if (_inputFeederUnit == null)
                    return;

                var items = new List<ParameterGridItem>();
                foreach (var position in TeachingPositions)
                {
                    FeederTeachingPosition captured = position;
                    items.Add(AxisDouble(captured.DisplayName, ParameterGridScope.Recipe, () => captured.Getter(_inputFeederUnit), v => captured.Setter(_inputFeederUnit, v)));
                }

                items.Add(ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => _inputFeederUnit.Setup.IsSimulationMode, v => _inputFeederUnit.Setup.IsSimulationMode = v));
                items.Add(ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => _inputFeederUnit.Config.bDryRun, v => _inputFeederUnit.Config.bDryRun = v));
                optionParameterGrid.SetItems(items);

                waitParameterGrid.SetItems(new[]
                {
                    ParameterGridItem.Int("MOVE TIMEOUT", "ms", ParameterGridScope.Setup, () => _inputFeederUnit.FeederY.Setup.MoveTimeoutMs, v => _inputFeederUnit.FeederY.Setup.MoveTimeoutMs = Math.Max(0, v))
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-FEEDER", "BindParameterGrids failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Parameters", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindIoPanel()
        {
            try
            {
                if (_inputFeederUnit == null)
                    return;

                ioCylinderPanel.SetItems(new[]
                {
                    // ===== 단독 체크 센서 (세트 구성 아님) — 최상단 =====
                    IoCylinderItem.Input("WAFER FEEDER RING CHECK", () => _inputFeederUnit.IsWaferFeederRingDetected()),
                    IoCylinderItem.Input("WAFER FEEDER OVERLOAD", () => _inputFeederUnit.IsWaferFeederOverload()),

                    // ===== SET 1: WAFER FEEDER LIFT (Up/Down 체크 센서 + Up/Down 출력 통합 실린더) =====
                    IoCylinderItem.Input("WAFER FEEDER UP", () => _inputFeederUnit.IsWaferFeederUp()),
                    IoCylinderItem.Input("WAFER FEEDER DOWN", () => _inputFeederUnit.IsWaferFeederDown()),
                    IoCylinderItem.Cylinder("WAFER FEEDER LIFT", _inputFeederUnit.InputFeederLift, "UP", "DOWN"),

                    // ===== SET 2: WAFER FEEDER CLAMP (Clamp/Unclamp 체크 센서 + Clamp/Unclamp 출력 통합 실린더) =====
                    IoCylinderItem.Input("WAFER FEEDER CLAMP", () => _inputFeederUnit.IsWaferFeederClamp()),
                    IoCylinderItem.Input("WAFER FEEDER UNCLAMP", () => _inputFeederUnit.IsWaferFeederUnclamp()),
                    IoCylinderItem.Cylinder("WAFER FEEDER CLAMP", _inputFeederUnit.InputFeederClamp, "CLAMP", "UNCLAMP")
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-FEEDER", "BindIoPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder I/O", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindJogPanel()
        {
            try
            {
                if (_inputFeederUnit == null)
                    return;

                JogAxisItem axisItem = JogAxisItem.Single("FEEDER Y", _inputFeederUnit.FeederY, AxisUnitConverter.DisplayUnitFor(_inputFeederUnit.FeederY), 1.0, "Y+", "Y-").WithControlKind(JogAxisControlKind.Vertical);
                axisItem.StepMoveAsync = async (item, direction, speedType, customSpeed, axisStepDistance) =>
                {
                    try
                    {
                        double target = _inputFeederUnit.FeederY.ActualPosition + (direction * axisStepDistance);
                        int moveResult = await _inputFeederUnit.MoveWaferFeederY(target, false);
                        if (moveResult != 0)
                            return moveResult;

                        return await _inputFeederUnit.WaitWaferFeederYMoveDone(_inputFeederUnit.FeederY.Setup.MoveTimeoutMs) ? 0 : -1;
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                    }
                };
                axisItem.ContinuousMoveAsync = (item, direction, speedType, customSpeed) =>
                {
                    try
                    {
                        _inputFeederUnit.ManualMoveWaferFeederYJog(direction, customSpeed);
                        return Task.FromResult(0);
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                    }
                };
                axisItem.StopAsync = item =>
                {
                    try
                    {
                        _inputFeederUnit.ManualStopWaferFeederY();
                        return Task.FromResult(0);
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                    }
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
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-FEEDER", "BindJogPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private ParameterGridItem AxisDouble(string displayName, ParameterGridScope scope, Func<double> getter, Action<double> setter)
        {
            ParameterGridItem item = ParameterGridItem.Double(
                displayName,
                AxisUnitConverter.DisplayUnitFor(_inputFeederUnit.FeederY),
                scope,
                () => AxisUnitConverter.ToDisplay(getter(), _inputFeederUnit.FeederY),
                v => setter(AxisUnitConverter.FromDisplay(v, _inputFeederUnit.FeederY)));
            item.UnitGetter = () => AxisUnitConverter.DisplayUnitFor(_inputFeederUnit.FeederY);
            return item;
        }

        private void ParameterGrid_ParameterValueChanged(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                SaveParameterData(e != null && e.Scope == ParameterGridScope.Recipe);
                RefreshView();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-FEEDER", "Parameter save failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Parameter Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void SaveParameterData(bool isRecipeData)
        {
            try
            {
                if (isRecipeData)
                    SaveCurrentRecipeData();
                else
                    SaveCurrentSettingsData();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void RefreshView()
        {
            try
            {
                if (_inputFeederUnit == null)
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
                Color header = Color.FromArgb(64, 64, 64);
                Color key = Color.FromArgb(208, 208, 208);
                Color value = Color.White;

                BackColor = bg;
                rootLayout.BackColor = bg;
                contentLayout.BackColor = bg;
                leftLayout.BackColor = bg;
                centerLayout.BackColor = bg;
                rightLayout.BackColor = bg;
                grpActions.BackColor = Color.FromArgb(245, 245, 245);
                grpIo.BackColor = Color.FromArgb(245, 245, 245);
                grpOptions.BackColor = Color.FromArgb(245, 245, 245);
                grpWait.BackColor = Color.FromArgb(245, 245, 245);
                grpJog.BackColor = Color.FromArgb(245, 245, 245);
                grpSpeed.BackColor = Color.FromArgb(245, 245, 245);
                actionLayout.BackColor = Color.FromArgb(245, 245, 245);
                ioLayout.BackColor = Color.FromArgb(245, 245, 245);
                optionRows.BackColor = bg;
                waitRows.BackColor = bg;
                lblHeader.BackColor = header;
                lblHeader.ForeColor = Color.White;
                lblHeader.Font = new Font("Malgun Gothic", 11F, FontStyle.Bold);

                foreach (var group in new[] { grpActions, grpIo, grpOptions, grpWait, grpJog, grpSpeed })
                    group.Font = new Font("Malgun Gothic", 10F, FontStyle.Bold);

                foreach (var buttonControl in new[]
                {
                    btnReadyMove, btnLoadingMove, btnUnloadingMove, btnSlotLoadingMove, btnSlotUnloadingMove,
                    btnWaferLoadPositionMove, btnWaferUnloadAvoidPositionMove, btnWaferUnloadPositionMove, btnWaferBarcodePositionMove
                })
                {
                    ApplyActionButtonStyle(buttonControl);
                }

                foreach (var label in new[]
                {
                    lbl8Inch, lbl12Inch, lblProtrusion, lblMapping,
                    lblRecipeLoadingKey, lblRecipeUnloadingKey, lblRecipeAvoidKey, lblRecipeFirstSlotKey,
                    lblRecipeMappingStartKey, lblRecipeMappingEndKey, lblConfigLoadingOffsetKey, lblConfigUnloadingOffsetKey,
                    lblConfigSlotPitchKey, lblConfigSlotCountKey, lblConfigScanVelocityKey, lblSetupToleranceKey,
                    lblConfigInchKey, lblConfigLevelKey, lblSetupSimulationKey, lblConfigDryRunKey,
                    lblWaitScanSettleKey, lblWaitMoveTimeoutKey
                })
                {
                    ApplyCellLabel(label, key, ContentAlignment.MiddleLeft);
                }

                foreach (var label in new[]
                {
                    lblRecipeLoadingVal, lblRecipeUnloadingVal, lblRecipeAvoidVal, lblRecipeFirstSlotVal,
                    lblRecipeMappingStartVal, lblRecipeMappingEndVal, lblConfigLoadingOffsetVal, lblConfigUnloadingOffsetVal,
                    lblConfigSlotPitchVal, lblConfigSlotCountVal, lblConfigScanVelocityVal, lblSetupToleranceVal,
                    lblConfigInchVal, lblConfigLevelVal, lblSetupSimulationVal, lblConfigDryRunVal,
                    lblWaitScanSettleVal, lblWaitMoveTimeoutVal
                })
                {
                    ApplyCellLabel(label, value, ContentAlignment.MiddleRight);
                    label.Cursor = Cursors.Hand;
                }

            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Theme", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private static void ApplyCellLabel(Label label, Color backColor, ContentAlignment alignment)
        {
            try
            {
                label.BackColor = backColor;
                label.BorderStyle = BorderStyle.FixedSingle;
                label.Dock = DockStyle.Fill;
                label.Font = new Font("Malgun Gothic", 8F, FontStyle.Bold);
                label.ForeColor = Color.Black;
                label.Margin = Padding.Empty;
                label.Padding = new Padding(8, 0, 8, 0);
                label.TextAlign = alignment;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void ApplyActionButtonStyle(ActionButton button)
        {
            try
            {
                if (button == null)
                    return;

                button.BackColor = Color.FromArgb(88, 94, 103);
                button.ForeColor = Color.White;
                button.Font = new Font("Malgun Gothic", 8F, FontStyle.Bold);
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


