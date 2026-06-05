using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT320;
using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>Input Cassette 레시피에서 InputCassetteUnit을 조작하는 화면입니다.</summary>
    public partial class InputFeederRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private InputCassetteUnit _InputCassetteUnit;
        private InputFeederUnit _inputFeederUnit;
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
                QMC.Common.MessageDialog.Show(ex.Message, "Input Cassette", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                _refreshTimer.Start();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
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
                BindParameterGridMenus();

                grpIo.ContextMenuStrip = new ContextMenuStrip();
                grpIo.ContextMenuStrip.Items.Add("Input cassette DI 상태를 다시 읽습니다.", null, IoRefresh_Click);

                _toolTip.SetToolTip(lblRecipeLoadingVal, "더블 클릭하면 값을 um 단위로 변경합니다.");
                _toolTip.SetToolTip(lblRecipeUnloadingVal, "더블 클릭하면 값을 um 단위로 변경합니다.");
                _toolTip.SetToolTip(lblConfigSlotCountVal, "더블 클릭하면 슬롯 개수를 변경하고 SlotPosition 버퍼를 다시 맞춥니다.");
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Configure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
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
                _InputCassetteUnit = machine != null ? machine.InputCassette : null;
                _inputFeederUnit = machine != null ? machine.InputFeeder : null;

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

                jogPositionListControl.Enabled = enabled;
                jogAxisMoveControl.Enabled = enabled;
                jogSpeedControl.Enabled = enabled;
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Enable", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnLoadingMove_Click(object sender, EventArgs e)
        {
            await ConfirmFeederMoveAsync("FEEDER LOAD", () => _inputFeederUnit.MoveToWaferFeederCassetteLoadPosition(0));
        }

        private async void btnUnloadingMove_Click(object sender, EventArgs e)
        {
            await ConfirmFeederMoveAsync("FEEDER UNLOAD", () => _inputFeederUnit.MoveToWaferFeederCassetteUnloadPosition(0));
        }

        private async void btnReadyMove_Click(object sender, EventArgs e)
        {
            await ConfirmFeederMoveAsync("AVOID MOVE", () => _inputFeederUnit.MoveToWaferFeederAvoidPosition());
        }

        private async void btnSlotLoadingMove_Click(object sender, EventArgs e)
        {
            await ConfirmFeederMoveAsync("STAGE LOAD", () => _inputFeederUnit.MoveToWaferFeederStageLoadPosition());
        }

        private async void btnSlotUnloadingMove_Click(object sender, EventArgs e)
        {
            await ConfirmFeederMoveAsync("STAGE UNLOAD", () => _inputFeederUnit.MoveToWaferFeederStageUnloadPosition());
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
                    QMC.Common.MessageDialog.Show(this, actionName + " 실패", "Input Cassette", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void BindTeachingMenus()
        {
            try
            {
                AttachTeachMenu(lblRecipeLoadingVal, "Loading");
                AttachTeachMenu(lblRecipeUnloadingVal, "Unloading");
                AttachTeachMenu(lblRecipeAvoidVal, "Avoid");
                AttachTeachMenu(lblRecipeFirstSlotVal, "FirstSlot");
                AttachTeachMenu(lblRecipeMappingStartVal, "MappingStart");
                AttachTeachMenu(lblRecipeMappingEndVal, "MappingEnd");
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Teach Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
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
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-CASSETTE", "BindParameterGridMenus failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                if (string.Equals(item.Key, "AVOID POSITION", StringComparison.OrdinalIgnoreCase))
                    return "Avoid";
                if (string.Equals(item.Key, "CASSETTE LOAD", StringComparison.OrdinalIgnoreCase))
                    return "CassetteLoad";
                if (string.Equals(item.Key, "CASSETTE UNLOAD", StringComparison.OrdinalIgnoreCase))
                    return "CassetteUnload";
                if (string.Equals(item.Key, "CASSETTE EXCHANGE", StringComparison.OrdinalIgnoreCase))
                    return "CassetteExchange";
                if (string.Equals(item.Key, "WAFER LOAD AVOID", StringComparison.OrdinalIgnoreCase))
                    return "WaferLoadAvoid";
                if (string.Equals(item.Key, "WAFER LOAD", StringComparison.OrdinalIgnoreCase))
                    return "WaferLoad";
                if (string.Equals(item.Key, "WAFER UNLOAD AVOID", StringComparison.OrdinalIgnoreCase))
                    return "WaferUnloadAvoid";

                return string.Empty;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-CASSETTE", "GetSelectedTeachingPositionName failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
            finally
            {
            }
        }

        private void AttachTeachMenu(Label label, string positionName)
        {
            try
            {
                var menu = label.ContextMenuStrip ?? new ContextMenuStrip();
                menu.Items.Add("해당 위치로 이동", null, async (s, e) => await MoveByPositionName(positionName));
                menu.Items.Add("현재 위치 티칭", null, (s, e) =>
                {
                    TeachPosition(positionName);
                    SaveCurrentRecipeData();
                    RefreshView();
                });

                label.ContextMenuStrip = menu;
                label.Cursor = Cursors.Hand;
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Teach Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                optionParameterGrid.SetItems(new[]
                {
                    ParameterGridItem.Micron("AVOID POSITION", ParameterGridScope.Recipe, () => _inputFeederUnit.Recipe.AvoidPosition, v => _inputFeederUnit.Recipe.AvoidPosition = v),
                    ParameterGridItem.Micron("CASSETTE LOAD POSITION", ParameterGridScope.Recipe, () => _inputFeederUnit.Recipe.CassetteLoadPosition, v => _inputFeederUnit.Recipe.CassetteLoadPosition = v),
                    ParameterGridItem.Micron("CASSETTE UNLOAD POSITION", ParameterGridScope.Recipe, () => _inputFeederUnit.Recipe.CassetteUnloadPosition, v => _inputFeederUnit.Recipe.CassetteUnloadPosition = v),
                    ParameterGridItem.Micron("CASSETTE EXCHANGE", ParameterGridScope.Recipe, () => _inputFeederUnit.Recipe.CassetteExchangePosition, v => _inputFeederUnit.Recipe.CassetteExchangePosition = v),
                    ParameterGridItem.Micron("WAFER LOAD AVOID POSITION", ParameterGridScope.Recipe, () => _inputFeederUnit.Recipe.WaferLoadAvoidPosition, v => _inputFeederUnit.Recipe.WaferLoadAvoidPosition = v),
                    ParameterGridItem.Micron("WAFER LOAD POSITION", ParameterGridScope.Recipe, () => _inputFeederUnit.Recipe.WaferLoadPosition, v => _inputFeederUnit.Recipe.WaferLoadPosition = v),
                    ParameterGridItem.Micron("WAFER UNLOAD AVOID POSITION", ParameterGridScope.Recipe, () => _inputFeederUnit.Recipe.WaferUnloadAvoidPosition, v => _inputFeederUnit.Recipe.WaferUnloadAvoidPosition = v),
                    ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => _inputFeederUnit.Setup.IsSimulationMode, v => _inputFeederUnit.Setup.IsSimulationMode = v),
                    ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => _inputFeederUnit.Config.bDryRun, v => _inputFeederUnit.Config.bDryRun = v)
                });

                waitParameterGrid.SetItems(new[]
                {
                    ParameterGridItem.Int("MOVE TIMEOUT", "ms", ParameterGridScope.Setup, () => _inputFeederUnit.FeederY.Setup.MoveTimeoutMs, v => _inputFeederUnit.FeederY.Setup.MoveTimeoutMs = Math.Max(0, v))
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-CASSETTE", "BindParameterGrids failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Parameters", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    IoCylinderItem.Input("WAFER FEEDER UP", () => _inputFeederUnit.IsWaferFeederUp()),
                    IoCylinderItem.Input("WAFER FEEDER DOWN", () => _inputFeederUnit.IsWaferFeederDown()),
                    IoCylinderItem.Input("WAFER FEEDER UNCLAMP", () => _inputFeederUnit.IsWaferFeederUnclamp()),
                    IoCylinderItem.Input("WAFER FEEDER RING CHECK", () => _inputFeederUnit.IsWaferFeederRingDetected()),
                    IoCylinderItem.Output("WAFER FEEDER UP", () => _inputFeederUnit.InputFeederLiftUpCyl != null && _inputFeederUnit.InputFeederLiftUpCyl.IsOn, on => _inputFeederUnit.SetWaferFeederLiftUpOutput(on)),
                    IoCylinderItem.Output("WAFER FEEDER DOWN", () => _inputFeederUnit.InputFeederLiftDownCyl != null && _inputFeederUnit.InputFeederLiftDownCyl.IsOn, on => _inputFeederUnit.SetWaferFeederLiftDownOutput(on)),
                    IoCylinderItem.Output("WAFER FEEDER CLAMP", () => _inputFeederUnit.InputFeederClampCyl != null && _inputFeederUnit.InputFeederClampCyl.IsOn, on => _inputFeederUnit.SetWaferFeederClampOutput(on)),
                    IoCylinderItem.Output("WAFER FEEDER UNCLAMP", () => _inputFeederUnit.InputFeederUnClampCyl != null && _inputFeederUnit.InputFeederUnClampCyl.IsOn, on => _inputFeederUnit.SetWaferFeederUnclampOutput(on))
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-CASSETTE", "BindIoPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette I/O", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                JogAxisItem axisItem = JogAxisItem.Single("FEEDER Y", _inputFeederUnit.FeederY, "um", 1000.0, "Y+", "Y-").WithControlKind(JogAxisControlKind.Vertical);
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
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-CASSETTE", "BindJogPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ParameterGrid_ParameterValueChanged(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                SaveEditedData(e != null && e.Scope == ParameterGridScope.Recipe);
                RefreshView();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-CASSETTE", "Parameter save failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Parameter Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindEditableLabels()
        {
            try
            {
                AttachMicronEditor(lblRecipeLoadingVal, "LOADING Z", () => _InputCassetteUnit.Recipe.LoaingPosition, v => _InputCassetteUnit.Recipe.LoaingPosition = v, true);
                AttachMicronEditor(lblRecipeUnloadingVal, "UNLOADING Z", () => _InputCassetteUnit.Recipe.UnloadingPosition, v => _InputCassetteUnit.Recipe.UnloadingPosition = v, true);
                AttachMicronEditor(lblRecipeAvoidVal, "READY POSITION", () => _InputCassetteUnit.Recipe.AvoidPosition, v => _InputCassetteUnit.Recipe.AvoidPosition = v, true);
                AttachMicronEditor(lblRecipeFirstSlotVal, "FIRST SLOT", () => _InputCassetteUnit.Recipe.FirstSlotPosition, v => _InputCassetteUnit.Recipe.FirstSlotPosition = v, true);
                AttachMicronEditor(lblRecipeMappingStartVal, "MAPPING START Z", () => _InputCassetteUnit.Recipe.MappingStartPosition, v => _InputCassetteUnit.Recipe.MappingStartPosition = v, true);
                AttachMicronEditor(lblRecipeMappingEndVal, "MAPPING END Z", () => _InputCassetteUnit.Recipe.MappingEndPosition, v => _InputCassetteUnit.Recipe.MappingEndPosition = v, true);
                AttachMicronEditor(lblConfigLoadingOffsetVal, "LOADING OFFSET", () => _InputCassetteUnit.Config.LoadingPositionOffset, v => _InputCassetteUnit.Config.LoadingPositionOffset = v, false);
                AttachMicronEditor(lblConfigUnloadingOffsetVal, "UNLOADING OFFSET", () => _InputCassetteUnit.Config.UnloadingPositionOffset, v => _InputCassetteUnit.Config.UnloadingPositionOffset = v, false);
                AttachMicronEditor(lblConfigSlotPitchVal, "SLOT PITCH", () => _InputCassetteUnit.Config.SlotPitch, v => _InputCassetteUnit.Config.SlotPitch = v, false);
                AttachIntEditor(lblConfigSlotCountVal, "SLOT COUNT", () => _InputCassetteUnit.Config.SlotCount, v =>
                {
                    _InputCassetteUnit.Config.SlotCount = Math.Max(0, v);
                    _InputCassetteUnit.EnsureSlotPositionBuffer();
                }, false);
                AttachDoubleEditor(lblConfigScanVelocityVal, "SCAN/JOG VELOCITY (mm/s)", () => _InputCassetteUnit.Config.ScanVelocity, v => _InputCassetteUnit.Config.ScanVelocity = Math.Max(0.1, v), "mm/s", false);
                AttachMicronEditor(lblSetupToleranceVal, "IN POSITION TOLERANCE", () => _InputCassetteUnit.ResolveWaferLifterZInPositionTolerance(), v => _InputCassetteUnit.WaferLifterZ.Config.InPositionTolerance = Math.Max(0.0, v), false);
                AttachIntEditor(lblConfigInchVal, "INCH SELECT", () => _InputCassetteUnit.Config.InchSelect, v => _InputCassetteUnit.Config.InchSelect = v, false);
                AttachIntEditor(lblConfigLevelVal, "CASSETTE LEVEL", () => _InputCassetteUnit.Config.SelectedCassetteLevel, v => _InputCassetteUnit.Config.SelectedCassetteLevel = v, false);
                AttachBoolEditor(lblSetupSimulationVal, "SIMULATION MODE", () => _InputCassetteUnit.Setup.IsSimulationMode, v => _InputCassetteUnit.Setup.IsSimulationMode = v, false);
                AttachBoolEditor(lblConfigDryRunVal, "DRY RUN", () => _InputCassetteUnit.Config.bDryRun, v => _InputCassetteUnit.Config.bDryRun = v, false);
                AttachIntEditor(lblWaitScanSettleVal, "SCAN SETTLE TIME (ms)", () => _InputCassetteUnit.Config.ScanSettleTimeMs, v => _InputCassetteUnit.Config.ScanSettleTimeMs = Math.Max(0, v), false);
                AttachIntEditor(lblWaitMoveTimeoutVal, "MOVE TIMEOUT (ms)", () => _InputCassetteUnit.ResolveWaferLifterZMoveTimeoutMs(), v => _InputCassetteUnit.WaferLifterZ.Setup.MoveTimeoutMs = Math.Max(0, v), false);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Edit Binding", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void AttachMicronEditor(Label label, string name, Func<double> getter, Action<double> setter, bool isRecipeData)
        {
            try
            {
                label.DoubleClick += (s, e) =>
                {
                    try
                    {
                        if (_InputCassetteUnit == null) return;
                        string text = Prompt.Show(name + " 값을 입력하세요. (um)", FormatNumber(getter() * 1000.0));
                        if (text == null) return;
                        double value;
                        if (!TryParseDouble(text, out value))
                            throw new FormatException("숫자 값을 입력해야 합니다.");

                        setter(value / 1000.0);
                        SaveEditedData(isRecipeData);
                        RefreshView();
                    }
                    catch (Exception ex)
                    {
                        QMC.Common.MessageDialog.Show(this, ex.Message, name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                    }
                };
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void AttachDoubleEditor(Label label, string name, Func<double> getter, Action<double> setter, string suffix, bool isRecipeData)
        {
            try
            {
                label.DoubleClick += (s, e) =>
                {
                    try
                    {
                        if (_InputCassetteUnit == null) return;
                        string text = Prompt.Show(name + " 값을 입력하세요.", FormatNumber(getter()));
                        if (text == null) return;
                        double value;
                        if (!TryParseDouble(text, out value))
                            throw new FormatException("숫자 값을 입력해야 합니다.");

                        setter(value);
                        SaveEditedData(isRecipeData);
                        RefreshView();
                    }
                    catch (Exception ex)
                    {
                        QMC.Common.MessageDialog.Show(this, ex.Message, name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                    }
                };
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void AttachIntEditor(Label label, string name, Func<int> getter, Action<int> setter, bool isRecipeData)
        {
            try
            {
                label.DoubleClick += (s, e) =>
                {
                    try
                    {
                        if (_InputCassetteUnit == null) return;
                        string text = Prompt.Show(name + " 값을 입력하세요.", getter().ToString(CultureInfo.InvariantCulture));
                        if (text == null) return;
                        int value;
                        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) &&
                            !int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
                            throw new FormatException("정수 값을 입력해야 합니다.");

                        setter(value);
                        SaveEditedData(isRecipeData);
                        RefreshView();
                    }
                    catch (Exception ex)
                    {
                        QMC.Common.MessageDialog.Show(this, ex.Message, name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                    }
                };
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void AttachBoolEditor(Label label, string name, Func<bool> getter, Action<bool> setter, bool isRecipeData)
        {
            try
            {
                label.DoubleClick += (s, e) =>
                {
                    try
                    {
                        if (_InputCassetteUnit == null) return;
                        string text = Prompt.Show(name + " 값을 입력하세요. (true/false)", getter().ToString());
                        if (text == null) return;

                        bool value;
                        if (!TryParseBool(text, out value))
                            throw new FormatException("true/false, 1/0, on/off 중 하나로 입력해야 합니다.");

                        setter(value);
                        SaveEditedData(isRecipeData);
                        RefreshView();
                    }
                    catch (Exception ex)
                    {
                        QMC.Common.MessageDialog.Show(this, ex.Message, name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                    }
                };
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void SaveEditedData(bool isRecipeData)
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private static bool TryParseDouble(string text, out double value)
        {
            try
            {
                text = (text ?? string.Empty).Replace("um", string.Empty).Replace("mm/s", string.Empty).Replace("ms", string.Empty).Trim();
                return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                       double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
            }
            catch
            {
                value = 0.0;
                return false;
            }
            finally
            {
            }
        }

        private static bool TryParseBool(string text, out bool value)
        {
            try
            {
                string normalized = (text ?? string.Empty).Trim().ToLowerInvariant();
                if (normalized == "true" || normalized == "1" || normalized == "on" || normalized == "yes" || normalized == "y")
                {
                    value = true;
                    return true;
                }

                if (normalized == "false" || normalized == "0" || normalized == "off" || normalized == "no" || normalized == "n")
                {
                    value = false;
                    return true;
                }

                return bool.TryParse(text, out value);
            }
            catch
            {
                value = false;
                return false;
            }
            finally
            {
            }
        }

        private static string FormatUm(double value)
        {
            try
            {
                return FormatNumber(value * 1000.0) + " um";
            }
            catch
            {
                return "0 um";
            }
            finally
            {
            }
        }

        private static string FormatNumber(double value)
        {
            try
            {
                return value.ToString("0.###", CultureInfo.InvariantCulture);
            }
            catch
            {
                return "0";
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
                Color actionButtonColor = Color.FromArgb(88, 94, 103);
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

                foreach (var buttonControl in new[] { btnLoadingMove, btnUnloadingMove, btnReadyMove, btnSlotLoadingMove, btnSlotUnloadingMove })
                {
                    buttonControl.BackColor = actionButtonColor;
                    buttonControl.ForeColor = Color.White;
                    buttonControl.Font = new Font("Malgun Gothic", 8F, FontStyle.Bold);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Theme", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

       
    }
}


