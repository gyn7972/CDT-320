using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT320;
using QMC.CDT320.Sequencing;
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
    public partial class InputCassetteRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private InputCassetteUnit _InputCassetteUnit;
        private readonly Timer _refreshTimer = new Timer();
        private readonly ToolTip _toolTip = new ToolTip();
        /// <summary>InputCassetteRecipePage를 생성합니다.</summary>
        public InputCassetteRecipePage()
        {
            try
            {
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

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            try { if (Visible) _refreshTimer.Start(); else _refreshTimer.Stop(); } catch { }
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
                lblHeader.Tag = "i18n:recipe.inputCassette";
                lblHeader.Text = Lang.T("recipe.inputCassette");

                _refreshTimer.Interval = 250;
                _refreshTimer.Tick += RefreshTimer_Tick;
                optionParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
                waitParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
                BindParameterGridMenus();

                grpIo.ContextMenuStrip = new ContextMenuStrip();
                grpIo.ContextMenuStrip.Items.Add("Input cassette DI 상태를 다시 읽습니다.", null, IoRefresh_Click);

                _toolTip.SetToolTip(lblRecipeLoadingVal, "더블 클릭하면 현재 축 표시 단위로 값을 변경합니다.");
                _toolTip.SetToolTip(lblRecipeUnloadingVal, "더블 클릭하면 현재 축 표시 단위로 값을 변경합니다.");
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
                if (!ShouldRefreshVisible(this))
                    return;

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
                _InputCassetteUnit = machine != null ? machine.InputCassetteUnit : null;

                if (_InputCassetteUnit != null)
                    _InputCassetteUnit.EnsureSlotPositionBuffer();

                SetEnabledState(_InputCassetteUnit != null);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Resolve", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            try
            {
                if (_InputCassetteUnit == null) return;
                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    "Loading Move를 진행하시겠습니까?",
                    "Input Cassette Move",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "UI",
                        "INPUT-CASSETTE",
                        "btnLoadingMove_Click canceled.");
                    return;
                }

                await MoveToTarget("LOADING Z", _InputCassetteUnit.Recipe.LoaingPosition);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnUnloadingMove_Click(object sender, EventArgs e)
        {
            try
            {
                if (_InputCassetteUnit == null) return;
                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    "이동하시겠습니까?",
                    "Input Cassette Move",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "UI",
                        "INPUT-CASSETTE",
                        "btnUnloadingMove_Click canceled.");
                    return;
                }
                await MoveToTarget("UNLOADING Z", _InputCassetteUnit.Recipe.UnloadingPosition);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnReadyMove_Click(object sender, EventArgs e)
        {
            try
            {
                if (_InputCassetteUnit == null) return;
                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    "이동하시겠습니까?",
                    "Input Cassette Move",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "UI",
                        "INPUT-CASSETTE",
                        "btnReadyMove_Click canceled.");
                    return;
                }
                await MoveToTarget("READY POSITION", _InputCassetteUnit.Recipe.AvoidPosition);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnSlotLoadingMove_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    "이동하시겠습니까?",
                    "Input Cassette Move",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "UI",
                        "INPUT-CASSETTE",
                        "btnSlotLoadingMove_Click canceled.");
                    return;
                }

                if (_InputCassetteUnit == null) return;
                await MoveToTarget("MAPPING START Z", _InputCassetteUnit.Recipe.MappingStartPosition);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Slot Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnSlotUnloadingMove_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    "이동하시겠습니까?",
                    "Input Cassette Move",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "UI",
                        "INPUT-CASSETTE",
                        "btnSlotUnloadingMove_Click canceled.");
                    return;
                }

                if (_InputCassetteUnit == null) return;
                await MoveToTarget("MAPPING END Z", _InputCassetteUnit.Recipe.MappingEndPosition);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Slot Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async Task MoveToTarget(string actionName, double target)
        {
            try
            {
                if (_InputCassetteUnit == null) return;
                await RunSafeAsync(async () =>
                {
                    int moveResult = await _InputCassetteUnit.MoveWaferLifterZ(target, IsFineMove());
                    if (moveResult != 0)
                        return moveResult;

                    return await _InputCassetteUnit.WaitWaferLifterZMoveDone(_InputCassetteUnit.ResolveWaferLifterZMoveTimeoutMs());
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

                if (string.Equals(item.Key, "LOADING Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "Loading";
                if (string.Equals(item.Key, "UNLOADING Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "Unloading";
                if (string.Equals(item.Key, "READY POSITION", StringComparison.OrdinalIgnoreCase))
                    return "Avoid";
                if (string.Equals(item.Key, "FIRST SLOT POSITION", StringComparison.OrdinalIgnoreCase))
                    return "FirstSlot";
                if (string.Equals(item.Key, "MAPPING START Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "MappingStart";
                if (string.Equals(item.Key, "MAPPING END Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "MappingEnd";

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

        private async Task MoveByPositionName(string positionName)
        {
            try
            {
                if (_InputCassetteUnit == null) return;

                if (string.Equals(positionName, "Loading", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("LOADING Z POSITION", _InputCassetteUnit.Recipe.LoaingPosition);
                else if (string.Equals(positionName, "Unloading", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("UNLOADING Z POSITION", _InputCassetteUnit.Recipe.UnloadingPosition);
                else if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("READY POSITION", _InputCassetteUnit.Recipe.AvoidPosition);
                else if (string.Equals(positionName, "FirstSlot", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("FIRST SLOT POSITION", _InputCassetteUnit.Recipe.FirstSlotPosition);
                else if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("MAPPING START Z POSITION", _InputCassetteUnit.Recipe.MappingStartPosition);
                else if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("MAPPING END Z POSITION", _InputCassetteUnit.Recipe.MappingEndPosition);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void TeachPosition(string positionName)
        {
            try
            {
                if (_InputCassetteUnit == null) return;

                if (string.Equals(positionName, "Loading", StringComparison.OrdinalIgnoreCase))
                    _InputCassetteUnit.Recipe.LoaingPosition = _InputCassetteUnit.InputLifterZ.ActualPosition;
                else if (string.Equals(positionName, "Unloading", StringComparison.OrdinalIgnoreCase))
                    _InputCassetteUnit.Recipe.UnloadingPosition = _InputCassetteUnit.InputLifterZ.ActualPosition;
                else if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase))
                    _InputCassetteUnit.TeachWaferLifterZAvoidPosition();
                else if (string.Equals(positionName, "FirstSlot", StringComparison.OrdinalIgnoreCase))
                    _InputCassetteUnit.TeachWaferLifterZPosition("FirstSlot");
                else if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase))
                    _InputCassetteUnit.TeachWaferLifterZMappingStartPosition();
                else if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase))
                    _InputCassetteUnit.TeachWaferLifterZMappingEndPosition();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Teach", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindParameterGrids()
        {
            try
            {
                if (_InputCassetteUnit == null)
                    return;

                optionParameterGrid.SetItems(new[]
                {
                    AxisDouble("LOADING Z POSITION", ParameterGridScope.Recipe, () => _InputCassetteUnit.Recipe.LoaingPosition, v => _InputCassetteUnit.Recipe.LoaingPosition = v),
                    AxisDouble("UNLOADING Z POSITION", ParameterGridScope.Recipe, () => _InputCassetteUnit.Recipe.UnloadingPosition, v => _InputCassetteUnit.Recipe.UnloadingPosition = v),
                    AxisDouble("READY POSITION", ParameterGridScope.Recipe, () => _InputCassetteUnit.Recipe.AvoidPosition, v => _InputCassetteUnit.Recipe.AvoidPosition = v),
                    AxisDouble("FIRST SLOT POSITION", ParameterGridScope.Recipe, () => _InputCassetteUnit.Recipe.FirstSlotPosition, v => _InputCassetteUnit.Recipe.FirstSlotPosition = v),
                    AxisDouble("MAPPING START Z POSITION", ParameterGridScope.Recipe, () => _InputCassetteUnit.Recipe.MappingStartPosition, v => _InputCassetteUnit.Recipe.MappingStartPosition = v),
                    AxisDouble("MAPPING END Z POSITION", ParameterGridScope.Recipe, () => _InputCassetteUnit.Recipe.MappingEndPosition, v => _InputCassetteUnit.Recipe.MappingEndPosition = v),
                    AxisDouble("LOADING OFFSET", ParameterGridScope.Config, () => _InputCassetteUnit.Config.LoadingPositionOffset, v => _InputCassetteUnit.Config.LoadingPositionOffset = v),
                    AxisDouble("UNLOADING OFFSET", ParameterGridScope.Config, () => _InputCassetteUnit.Config.UnloadingPositionOffset, v => _InputCassetteUnit.Config.UnloadingPositionOffset = v),
                    AxisDouble("LEVEL 2 OFFSET", ParameterGridScope.Config, () => _InputCassetteUnit.Config.Level2PositionOffset, v => _InputCassetteUnit.Config.Level2PositionOffset = Math.Max(0.0, v)),
                    AxisDouble("SLOT PITCH", ParameterGridScope.Config, () => _InputCassetteUnit.Config.SlotPitch, v => _InputCassetteUnit.Config.SlotPitch = Math.Max(0.0, v)),
                    ParameterGridItem.Int("SLOT COUNT", "ea", ParameterGridScope.Config, () => _InputCassetteUnit.Config.SlotCount, v =>
                    {
                        _InputCassetteUnit.Config.SlotCount = Math.Max(0, v);
                        _InputCassetteUnit.EnsureSlotPositionBuffer();
                    }),
                    AxisDouble("SCAN/JOG VELOCITY", ParameterGridScope.Config, () => _InputCassetteUnit.Config.ScanVelocity, v => _InputCassetteUnit.Config.ScanVelocity = Math.Max(0.1, v), "/s"),
                    AxisDouble("IN POSITION TOL.", ParameterGridScope.Config, () => _InputCassetteUnit.ResolveWaferLifterZInPositionTolerance(), v => _InputCassetteUnit.InputLifterZ.Config.InPositionTolerance = Math.Max(0.0, v)),
                    ParameterGridItem.Selection("INCH SELECT", "Inch", ParameterGridScope.Config, () => _InputCassetteUnit.Config.InchSelect, v => _InputCassetteUnit.Config.InchSelect = Convert.ToInt32(v), new[]
                    {
                        new ParameterGridOption("8", 8),
                        new ParameterGridOption("12", 12)
                    }),
                    ParameterGridItem.Selection("CASSETTE LEVEL", "단", ParameterGridScope.Config, () => _InputCassetteUnit.Config.SelectedCassetteLevel, v => _InputCassetteUnit.Config.SelectedCassetteLevel = Convert.ToInt32(v), new[]
                    {
                        new ParameterGridOption("1", 1),
                        new ParameterGridOption("2", 2)
                    }),
                    ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => _InputCassetteUnit.Setup.IsSimulationMode, v => _InputCassetteUnit.Setup.IsSimulationMode = v),
                    ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => _InputCassetteUnit.Config.bDryRun, v => _InputCassetteUnit.Config.bDryRun = v)
                });

                waitParameterGrid.SetItems(new[]
                {
                    ParameterGridItem.Int("SCAN SETTLE TIME", "ms", ParameterGridScope.Config, () => _InputCassetteUnit.Config.ScanSettleTimeMs, v => _InputCassetteUnit.Config.ScanSettleTimeMs = Math.Max(0, v)),
                    ParameterGridItem.Int("MOVE TIMEOUT", "ms", ParameterGridScope.Setup, () => _InputCassetteUnit.ResolveWaferLifterZMoveTimeoutMs(), v => _InputCassetteUnit.InputLifterZ.Setup.MoveTimeoutMs = Math.Max(0, v))
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
                if (_InputCassetteUnit == null)
                    return;

                ioCylinderPanel.SetItems(new[]
                {
                    IoCylinderItem.Input("8 INCH CASSETTE", () => _InputCassetteUnit.IsWaferCassetteExist(8)),
                    IoCylinderItem.Input("12 INCH CASSETTE", () => _InputCassetteUnit.IsWaferCassetteExist(12)),
                    IoCylinderItem.Input("WAFER PROTRUSION", () => _InputCassetteUnit.IsWaferProtrusionDetected()),
                    IoCylinderItem.Input("WAFER MAPPING", () => _InputCassetteUnit.IsWaferMapping())
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
                if (_InputCassetteUnit == null)
                    return;

                JogAxisItem axisItem = JogAxisItem.Single("AXIS Z", _InputCassetteUnit.InputLifterZ, AxisUnitConverter.DisplayUnitFor(_InputCassetteUnit.InputLifterZ), 1.0, "Z+", "Z-").WithControlKind(JogAxisControlKind.Vertical);
                axisItem.StepMoveAsync = async (item, direction, speedType, customSpeed, axisStepDistance) =>
                {
                    try
                    {
                        double target = _InputCassetteUnit.InputLifterZ.ActualPosition + (direction * axisStepDistance);
                        int moveResult = await _InputCassetteUnit.MoveWaferLifterZ(target, speedType, customSpeed);
                        if (moveResult != 0)
                            return moveResult;

                        return await _InputCassetteUnit.WaitWaferLifterZMoveDone(_InputCassetteUnit.ResolveWaferLifterZMoveTimeoutMs());
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                    }
                };
                axisItem.ContinuousMoveAsync = async (item, direction, speedType, customSpeed) =>
                {
                    try
                    {
                        return await _InputCassetteUnit.ManualMoveWaferLifterZJog(direction, speedType, customSpeed);
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                    }
                };
                axisItem.StopAsync = async item =>
                {
                    try
                    {
                        return await _InputCassetteUnit.ManualStopWaferLifterZ();
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
                if (_InputCassetteUnit == null)
                    return;

                lblRecipeLoadingVal.Text = FormatAxis(_InputCassetteUnit.Recipe.LoaingPosition);
                lblRecipeUnloadingVal.Text = FormatAxis(_InputCassetteUnit.Recipe.UnloadingPosition);
                lblRecipeAvoidVal.Text = FormatAxis(_InputCassetteUnit.Recipe.AvoidPosition);
                lblRecipeFirstSlotVal.Text = FormatAxis(_InputCassetteUnit.Recipe.FirstSlotPosition);
                lblRecipeMappingStartVal.Text = FormatAxis(_InputCassetteUnit.Recipe.MappingStartPosition);
                lblRecipeMappingEndVal.Text = FormatAxis(_InputCassetteUnit.Recipe.MappingEndPosition);
                lblConfigLoadingOffsetVal.Text = FormatAxis(_InputCassetteUnit.Config.LoadingPositionOffset);
                lblConfigUnloadingOffsetVal.Text = FormatAxis(_InputCassetteUnit.Config.UnloadingPositionOffset);
                lblConfigSlotPitchVal.Text = FormatAxis(_InputCassetteUnit.Config.SlotPitch);
                lblConfigSlotCountVal.Text = _InputCassetteUnit.Config.SlotCount.ToString(CultureInfo.InvariantCulture);
                lblConfigScanVelocityVal.Text = FormatAxis(_InputCassetteUnit.Config.ScanVelocity, "/s");
                lblSetupToleranceVal.Text = FormatAxis(_InputCassetteUnit.ResolveWaferLifterZInPositionTolerance());
                lblConfigInchVal.Text = _InputCassetteUnit.Config.InchSelect.ToString(CultureInfo.InvariantCulture);
                lblConfigLevelVal.Text = _InputCassetteUnit.Config.SelectedCassetteLevel.ToString(CultureInfo.InvariantCulture);
                lblSetupSimulationVal.Text = _InputCassetteUnit.Setup.IsSimulationMode.ToString();
                lblConfigDryRunVal.Text = _InputCassetteUnit.Config.bDryRun.ToString();
                lblWaitScanSettleVal.Text = _InputCassetteUnit.Config.ScanSettleTimeMs.ToString(CultureInfo.InvariantCulture) + " ms";
                lblWaitMoveTimeoutVal.Text = _InputCassetteUnit.ResolveWaferLifterZMoveTimeoutMs().ToString(CultureInfo.InvariantCulture) + " ms";
                optionParameterGrid.RefreshValues();
                waitParameterGrid.RefreshValues();
                ioCylinderPanel.RefreshStates();
                jogPositionListControl.RefreshState();

                dot8Inch.IsOn = _InputCassetteUnit.IsWaferCassetteExist(8);
                dot12Inch.IsOn = _InputCassetteUnit.IsWaferCassetteExist(12);
                dotProtrusion.IsOn = _InputCassetteUnit.IsWaferProtrusionDetected();
                dotMapping.IsOn = _InputCassetteUnit.IsWaferMapping();
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

        private ParameterGridItem AxisDouble(string displayName, ParameterGridScope scope, Func<double> getter, Action<double> setter, string unitSuffix = "")
        {
            ParameterGridItem item = ParameterGridItem.Double(
                displayName,
                AxisUnitConverter.DisplayUnitFor(_InputCassetteUnit.InputLifterZ) + unitSuffix,
                scope,
                () => AxisUnitConverter.ToDisplay(getter(), _InputCassetteUnit.InputLifterZ),
                v => setter(AxisUnitConverter.FromDisplay(v, _InputCassetteUnit.InputLifterZ)));
            item.UnitGetter = () => AxisUnitConverter.DisplayUnitFor(_InputCassetteUnit.InputLifterZ) + unitSuffix;
            return item;
        }

        private string FormatAxis(double value, string unitSuffix = "")
        {
            try
            {
                return AxisUnitConverter.FormatDisplay(value, _InputCassetteUnit.InputLifterZ, "0.###", true) + unitSuffix;
            }
            catch
            {
                return "0 " + AxisUnitConverter.DisplayUnitFor(_InputCassetteUnit != null ? _InputCassetteUnit.InputLifterZ : null) + unitSuffix;
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


