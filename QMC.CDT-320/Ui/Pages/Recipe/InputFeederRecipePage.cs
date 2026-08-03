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
        private readonly Timer _refreshTimer = new Timer();
        private readonly ToolTip _toolTip = new ToolTip();
        private string _titleI18n = "recipe.inputFeeder";

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
                manualActionPanel.SetButtonsEnabled(enabled);

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
                // 공용 MANUAL ACTION 판넬에 티칭 위치별 버튼 등록 (2열, 행 수 자동)
                var actions = new List<ManualActionItem>();
                foreach (FeederTeachingPosition position in TeachingPositions)
                {
                    FeederTeachingPosition captured = position;
                    actions.Add(ManualActionItem.Create(captured.DisplayName, () => ConfirmTeachingMoveAsync(captured)));
                }

                manualActionPanel.ColumnCount = 2;
                manualActionPanel.SetItems(actions);
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

        private async Task ConfirmTeachingMoveAsync(FeederTeachingPosition position)
        {
            try
            {
                if (position == null)
                    return;

                await ConfirmFeederMoveAsync(
                    position.DisplayName,
                    () => _inputFeederUnit.MoveWaferFeederYToTeachingPosition(
                        position.PositionName,
                        jogAxisMoveControl.SelectedSpeedType,
                        jogAxisMoveControl.GetSelectedSpeed(_inputFeederUnit.FeederY)));
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
                if (!EnsureFeederYHomeDone(actionName)) return;

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

        // 이동 대상 축(Input Feeder Y)의 HOME END(IsHomeDone) 미완료면 차단.
        private bool EnsureFeederYHomeDone(string actionName)
        {
            if (_inputFeederUnit != null && _inputFeederUnit.FeederY != null && !_inputFeederUnit.FeederY.IsHomeDone)
            {
                string homeMsg = actionName + " 불가: Input Feeder Y 축 HOME END(원점복귀)가 완료되지 않았습니다.";
                QMC.Common.Alarms.AlarmManager.Raise(QMC.Common.Alarms.AlarmSeverity.Warning, "INPUT-FEEDER", "UI", homeMsg);
                QMC.Common.MessageDialog.Show(this, homeMsg, "Input Feeder Move", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private async Task RunSafeAsync(Func<Task<int>> action, string actionName)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                int result;
                using (MotionGuardRuntime.BeginManualSequenceProcessMove("InputFeederRecipePage." + actionName))
                {
                    result = await action();
                }
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
                // 우클릭 메뉴 대신, 티칭 포지션 행의 MOVE/TEACH 버튼으로 이동/티칭 수행
                optionParameterGrid.ParameterMoveRequested += OptionParameterGrid_MoveRequested;
                optionParameterGrid.ParameterTeachRequested += OptionParameterGrid_TeachRequested;
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

        private async void OptionParameterGrid_MoveRequested(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                if (e == null || e.Item == null)
                    return;

                string positionName = GetSelectedTeachingPositionName();
                if (string.IsNullOrWhiteSpace(positionName))
                    return;

                if (ConfirmMoveToPositionSpeed("Input Feeder Move", e.Item.Key))
                    await MoveByPositionName(positionName);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-FEEDER", "Move button failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                if (!ConfirmTeachPosition("Input Feeder Teach", e.Item.Key))
                    return;

                TeachPosition(positionName);
                SaveCurrentRecipeData();
                RefreshView();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-FEEDER", "Teach button failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Feeder Teach", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                EventLogger.Write(EventKind.Event, "UI", "INPUT-FEEDER", actionName + " canceled.");
                return false;
            }

            jogAxisMoveControl.SetSelectedSpeedType(speedType);
            return true;
        }

        private bool ConfirmTeachPosition(string title, string actionName)
        {
            string name = string.IsNullOrWhiteSpace(actionName) ? "Teach Position" : actionName;
            using (var dialog = new QMC.Common.MessageBoxYesNo())
            {
                dialog.ButtonGroupLabel = "TEACH";
                DialogResult result = dialog.ShowDialog(
                    title,
                    name + " 현재 위치로 티칭하시겠습니까?",
                    this,
                    new[] { "Yes", "No" });

                if (result == DialogResult.Yes)
                    return true;

                EventLogger.Write(EventKind.Event, "UI", "INPUT-FEEDER", name + " teach canceled.");
                return false;
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
                if (!EnsureFeederYHomeDone(positionName)) return;

                await RunSafeAsync(async () =>
                {
                    int moveResult = await _inputFeederUnit.MoveWaferFeederYToTeachingPosition(
                        positionName,
                        jogAxisMoveControl.SelectedSpeedType,
                        jogAxisMoveControl.GetSelectedSpeed(_inputFeederUnit.FeederY));
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
                    var teachItem = AxisDouble(captured.DisplayName, ParameterGridScope.Recipe, () => captured.Getter(_inputFeederUnit), v => captured.Setter(_inputFeederUnit, v));
                    teachItem.SupportsTeaching = true;   // 행에 MOVE/TEACH 버튼 표시(티칭 포지션)
                    items.Add(teachItem);
                }

                items.Add(ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => _inputFeederUnit.Setup.IsSimulationMode, v => _inputFeederUnit.Setup.IsSimulationMode = v));
                items.Add(ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => _inputFeederUnit.Config.bDryRun, v => _inputFeederUnit.Config.bDryRun = v));
                optionParameterGrid.SetItems(items);

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

                // 2열 열우선 배치 ("WAFER FEEDER" 접두사 생략):
                // [1열] RING/AVOID CHECK + LIFT 세트, [2열] OVERLOAD + CLAMP 세트
                ioCylinderPanel.ColumnCount = 2;
                ioCylinderPanel.AutoFitParentGroupHeight = true;   // 5개 행 높이에 맞추고 내부 스크롤을 제거한다.
                ioCylinderPanel.SetItems(new[]
                {
                    IoCylinderItem.Input("RING CHECK", () => _inputFeederUnit.IsWaferFeederRingDetected()),
                    IoCylinderItem.Input("LIFT UP", () => _inputFeederUnit.IsWaferFeederUp()),
                    IoCylinderItem.Input("LIFT DOWN", () => _inputFeederUnit.IsWaferFeederDown()),
                    IoCylinderItem.Cylinder("LIFT", _inputFeederUnit.InputFeederLift, "UP", "DOWN"),
                    IoCylinderItem.Input("AVOID CHECK", () => _inputFeederUnit.IsWaferFeederAvoidPositionCheck()),

                    IoCylinderItem.Input("OVERLOAD", () => _inputFeederUnit.IsWaferFeederOverload()),
                    IoCylinderItem.Input("CLAMP", () => _inputFeederUnit.IsWaferFeederClamp()),
                    IoCylinderItem.Input("UNCLAMP", () => _inputFeederUnit.IsWaferFeederUnclamp()),
                    IoCylinderItem.Cylinder("CLAMP", _inputFeederUnit.InputFeederClamp, "CLAMP", "UNCLAMP")
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
                axisItem.StepMoveAsync = (item, direction, speedType, customSpeed, axisStepDistance) =>
                    _inputFeederUnit.JogStepAsync(_inputFeederUnit.FeederY, direction, speedType, customSpeed, axisStepDistance);
                axisItem.ContinuousMoveAsync = (item, direction, speedType, customSpeed) =>
                    _inputFeederUnit.JogContinuousAsync(_inputFeederUnit.FeederY, direction, speedType, customSpeed);
                axisItem.StopAsync = item =>
                    _inputFeederUnit.StopJogAsync(_inputFeederUnit.FeederY);

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
                if (host == null || string.IsNullOrWhiteSpace(host.ActiveRecipeName))
                    throw new InvalidOperationException("활성 Recipe가 없어 Input Feeder Recipe 값을 저장할 수 없습니다.");

                if (!host.SaveMachineRecipe(host.ActiveRecipeName))
                    throw new InvalidOperationException(
                        "Input Feeder Recipe 저장에 실패했습니다. 현재 적용값과 저장 파일의 값이 다를 수 있으며, " +
                        "재시작하면 이전값으로 복원될 수 있습니다. Alarm/Event Log를 확인하십시오. recipe=" +
                        host.ActiveRecipeName);
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
                if (host == null)
                    throw new InvalidOperationException("Main 화면을 찾을 수 없어 Input Feeder Config/Setup 값을 저장할 수 없습니다.");

                if (!host.SaveMachineSettings())
                    throw new InvalidOperationException(
                        "Input Feeder Config/Setup 저장에 실패했습니다. 현재 적용값과 저장 파일의 값이 다를 수 있으며, " +
                        "재시작하면 이전값으로 복원될 수 있습니다. Alarm/Event Log를 확인하십시오.");
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

        private void ApplyRecipeTheme()
        {
            try
            {
                // 여기서는 고정 Key/Value 셀 라벨 스타일링(반복)만 유지한다.
                Color key = Color.FromArgb(208, 208, 208);
                Color value = Color.White;


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


       
    }
}


