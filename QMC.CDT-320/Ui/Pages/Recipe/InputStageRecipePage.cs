using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Localization;
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
    public partial class InputStageRecipePage : PageBase
    {
        private enum StagePositionKind
        {
            Avoid,
            Load,
            Process,
            Unload,
            Ready,
            Reticle
        }

        private sealed class StageTeachingPosition
        {
            public StageTeachingPosition(
                string axisLabel,
                StagePositionKind kind,
                Func<InputStageUnit, BaseAxis> axisGetter,
                Func<InputStageUnit, StageAxisPositions> positionSetGetter,
                Func<StageAxisPositions, double> getter,
                Action<StageAxisPositions, double> setter)
            {
                AxisLabel = axisLabel;
                Kind = kind;
                DisplayName = GetPositionLabel(kind) + " - " + axisLabel;
                AxisGetter = axisGetter;
                PositionSetGetter = positionSetGetter;
                Getter = getter;
                Setter = setter;
            }

            public string AxisLabel { get; private set; }
            public StagePositionKind Kind { get; private set; }
            public string DisplayName { get; private set; }
            public Func<InputStageUnit, BaseAxis> AxisGetter { get; private set; }
            public Func<InputStageUnit, StageAxisPositions> PositionSetGetter { get; private set; }
            public Func<StageAxisPositions, double> Getter { get; private set; }
            public Action<StageAxisPositions, double> Setter { get; private set; }
        }

        private static readonly StageTeachingPosition[] TeachingPositions = CreateTeachingPositions();

        private readonly string _titleI18n;
        private readonly Timer _refreshTimer = new Timer();
        private readonly ToolTip _toolTip = new ToolTip();
        private InputStageUnit _InputStageUnit;

        private static StageTeachingPosition[] CreateTeachingPositions()
        {
            var axes = new List<StageTeachingAxis>();
            AddTeachingAxis(axes, "WAFER Y", unit => unit.StageY, unit => unit.Recipe.WaferY);
            AddTeachingAxis(axes, "WAFER T", unit => unit.StageT, unit => unit.Recipe.WaferT);
            AddTeachingAxis(axes, "EXPANDER Z", unit => unit.ExpanderZ, unit => unit.Recipe.WaferZ);
            AddTeachingAxis(axes, "VISION X", unit => unit.CameraX, unit => unit.Recipe.VisionX, false, false);
            AddTeachingAxis(axes, "NEEDLE X", unit => unit.NeedleBlockX, unit => unit.Recipe.NeedleX, false, false);
            AddTeachingAxis(axes, "NEEDLE Z", unit => unit.NeedleZ, unit => unit.Recipe.NeedleZ);
            AddTeachingAxis(axes, "EJECT PIN Z", unit => unit.EjectPinZ, unit => unit.Recipe.EjectPinZ, false, false);

            var positions = new List<StageTeachingPosition>();
            foreach (StagePositionKind kind in new[] { StagePositionKind.Avoid, StagePositionKind.Load, StagePositionKind.Process, StagePositionKind.Unload, StagePositionKind.Ready, StagePositionKind.Reticle })
            {
                foreach (StageTeachingAxis axis in axes)
                {
                    if (!axis.Supports(kind))
                        continue;
                    if (kind == StagePositionKind.Reticle &&
                        !string.Equals(axis.AxisLabel, "VISION X", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(axis.AxisLabel, "EXPANDER Z", StringComparison.OrdinalIgnoreCase))
                        continue;

                    AddTeachingPosition(positions, axis, kind);
                }
            }

            return positions.ToArray();
        }

        private sealed class StageTeachingAxis
        {
            public StageTeachingAxis(
                string axisLabel,
                Func<InputStageUnit, BaseAxis> axisGetter,
                Func<InputStageUnit, StageAxisPositions> positionSetGetter,
                bool includeLoad,
                bool includeUnload)
            {
                AxisLabel = axisLabel;
                AxisGetter = axisGetter;
                PositionSetGetter = positionSetGetter;
                IncludeLoad = includeLoad;
                IncludeUnload = includeUnload;
            }

            public string AxisLabel { get; private set; }
            public Func<InputStageUnit, BaseAxis> AxisGetter { get; private set; }
            public Func<InputStageUnit, StageAxisPositions> PositionSetGetter { get; private set; }
            public bool IncludeLoad { get; private set; }
            public bool IncludeUnload { get; private set; }

            public bool Supports(StagePositionKind kind)
            {
                if (kind == StagePositionKind.Load)
                    return IncludeLoad;
                if (kind == StagePositionKind.Unload)
                    return IncludeUnload;
                return true;
            }
        }

        private static void AddTeachingAxis(
            List<StageTeachingAxis> axes,
            string axisLabel,
            Func<InputStageUnit, BaseAxis> axisGetter,
            Func<InputStageUnit, StageAxisPositions> positionSetGetter,
            bool includeLoad = true,
            bool includeUnload = true)
        {
            axes.Add(new StageTeachingAxis(axisLabel, axisGetter, positionSetGetter, includeLoad, includeUnload));
        }

        private static void AddTeachingPosition(
            List<StageTeachingPosition> positions,
            StageTeachingAxis axis,
            StagePositionKind kind,
            Func<StageAxisPositions, double> getter,
            Action<StageAxisPositions, double> setter)
        {
            positions.Add(new StageTeachingPosition(axis.AxisLabel, kind, axis.AxisGetter, axis.PositionSetGetter, getter, setter));
        }

        private static void AddTeachingPosition(
            List<StageTeachingPosition> positions,
            StageTeachingAxis axis,
            StagePositionKind kind)
        {
            switch (kind)
            {
                // Avoid 위치 레시피 항목 추가
                case StagePositionKind.Avoid:
                    AddTeachingPosition(positions, axis, kind, set => set.AvoidPosition, (set, value) => set.AvoidPosition = value);
                    break;
                // Load 위치 레시피 항목 추가
                case StagePositionKind.Load:
                    AddTeachingPosition(positions, axis, kind, set => set.LoadPosition, (set, value) => set.LoadPosition = value);
                    break;
                // Process 위치 레시피 항목 추가
                case StagePositionKind.Process:
                    AddTeachingPosition(positions, axis, kind, set => set.ProcessPosition, (set, value) => set.ProcessPosition = value);
                    break;
                // Unload 위치 레시피 항목 추가
                case StagePositionKind.Unload:
                    AddTeachingPosition(positions, axis, kind, set => set.UnloadPosition, (set, value) => set.UnloadPosition = value);
                    break;
                // Ready 위치 레시피 항목 추가
                case StagePositionKind.Ready:
                    AddTeachingPosition(positions, axis, kind, set => set.ReadyPosition, (set, value) => set.ReadyPosition = value);
                    break;
                // Reticle 위치 레시피 항목 추가
                case StagePositionKind.Reticle:
                    if (string.Equals(axis.AxisLabel, "EXPANDER Z", StringComparison.OrdinalIgnoreCase))
                        AddTeachingPosition(positions, axis, kind, set => set.ProcessPosition, (set, value) => set.ProcessPosition = value);
                    else
                        AddTeachingPosition(positions, axis, kind, set => set.ReticlePosition, (set, value) => set.ReticlePosition = value);
                    break;
            }
        }

        private static string GetPositionLabel(StagePositionKind kind)
        {
            switch (kind)
            {
                // Avoid 위치 라벨 반환
                case StagePositionKind.Avoid:
                    return "AVOID POSITION";
                // Load 위치 라벨 반환
                case StagePositionKind.Load:
                    return "LOAD POSITION";
                // Process 위치 라벨 반환
                case StagePositionKind.Process:
                    return "PROCESS POSITION";
                // Unload 위치 라벨 반환
                case StagePositionKind.Unload:
                    return "UNLOAD POSITION";
                // Ready 위치 라벨 반환
                case StagePositionKind.Ready:
                    return "READY POSITION";
                // Reticle 위치 라벨 반환
                case StagePositionKind.Reticle:
                    return "RETICLE POSITION";
                default:
                    return kind.ToString().ToUpperInvariant() + " POSITION";
            }
        }

        public InputStageRecipePage() : this("recipe.inputStage")
        {
        }

        public InputStageRecipePage(string titleI18n)
        {
            try
            {
                _titleI18n = titleI18n;
                InitializeComponent();
                if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                    return;

                ApplyTitle();
                ApplyRuntimeLayout();
                ConfigureRuntimeBehavior();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(ex.Message, "Input Stage", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void ApplyTitle()
        {
            try
            {
                lblHeader.Tag = "i18n:" + _titleI18n;
                lblHeader.Text = Lang.T(_titleI18n);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void ApplyRuntimeLayout()
        {
            try
            {
                optionLayout.Visible = false;
                waitLayout.Visible = false;
                ioLayout.Visible = false;
                jogLayout.Visible = false;
                jogCommonLayout.Visible = true;
                speedLayout.Visible = false;

                optionParameterGrid.BringToFront();
                waitParameterGrid.BringToFront();
                ioCylinderPanel.BringToFront();
                jogCommonLayout.BringToFront();
                jogSpeedControl.BringToFront();

                BackColor = Color.FromArgb(207, 210, 214);
                rootLayout.BackColor = BackColor;
                contentLayout.BackColor = BackColor;
                lblHeader.BackColor = Color.FromArgb(64, 64, 64);
                lblHeader.ForeColor = Color.White;
                lblHeader.Font = new Font("Malgun Gothic", 11F, FontStyle.Bold);
                foreach (var group in new[] { grpOptions, grpWait, grpManual, grpIo, grpVision, grpJog, grpSpeed })
                    group.Font = new Font("Malgun Gothic", 10F, FontStyle.Bold);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Layout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ConfigureRuntimeBehavior()
        {
            try
            {
                _refreshTimer.Interval = 250;
                _refreshTimer.Tick += RefreshTimer_Tick;
                optionParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
                optionParameterGrid.ParameterRowDoubleClicked += OptionParameterGrid_RowDoubleClicked;
                waitParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;

                jogAxisMoveControl.SpeedControl = jogSpeedControl;
                jogAxisMoveControl.LayoutMode = JogAxisMoveLayoutMode.InputStagePad;
                jogAxisMoveControl.ShowCurrentSpeedMode = true;
                jogAxisMoveControl.ButtonAreaMinHeight = 520;
                jogAxisMoveControl.ButtonAreaMaxHeight = 620;
                jogAxisMoveControl.ButtonAreaMinWidth = 300;
                jogAxisMoveControl.ButtonAreaMaxWidth = 460;

                ConfigureActionButtons();
                BindParameterGridMenus();

                grpIo.ContextMenuStrip = new ContextMenuStrip();
                grpIo.ContextMenuStrip.Items.Add("Input stage I/O 상태를 다시 읽습니다.", null, IoRefresh_Click);

                _toolTip.SetToolTip(optionParameterGrid, "이름 셀 더블클릭: 현재 위치 티칭, 값 셀 더블클릭: 직접 편집");
                _toolTip.SetToolTip(waitParameterGrid, "Input Stage 대기 관련 파라미터를 표시합니다.");
                _toolTip.SetToolTip(ioCylinderPanel, "Input Stage 출력 상태를 확인하고 제어합니다.");
                _toolTip.SetToolTip(jogAxisMoveControl, "Input Stage 축을 Unit jog 경로로 이동합니다.");
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Configure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ConfigureActionButtons()
        {
            manualScrollPanel.AutoScroll = true;
            manualScrollPanel.HorizontalScroll.Enabled = false;
            manualScrollPanel.HorizontalScroll.Visible = false;

            manualLayout.Dock = DockStyle.Top;
        }

        // 매뉴얼 액션 버튼(Designer 배치)의 Click 핸들러 — 각 위치 종류의 시퀀스로 이동
        private async void btnAvoidPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("AVOID POSITION", MoveAvoidSequenceAsync);
        }

        private async void btnLoadPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("LOAD POSITION", () => MoveLoadUnloadSequenceAsync(StagePositionKind.Load));
        }

        private async void btnUnloadPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("UNLOAD POSITION", () => MoveLoadUnloadSequenceAsync(StagePositionKind.Unload));
        }

        private async void btnReadyPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("READY POSITION", MoveReadySequenceAsync);
        }

        private async void btnProcessPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("PROCESS POSITION", MoveProcessSequenceAsync);
        }

        private async void btnReticlePosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("RETICLE POSITION", MoveReticleSequenceAsync);
        }

        private async void btnPickTest_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("PICK TEST", PickTestAsync);
        }

        private void BindTeachingActionButton(ActionButton button, string displayName)
        {
            StageTeachingPosition position = FindTeachingPosition(displayName);
            if (position == null)
                return;

            BindActionButton(button, position.DisplayName, () => MoveByTeachingPositionAsync(position));
        }

        private void BindActionButton(ActionButton button, string text, Func<Task<int>> action)
        {
            if (button == null)
                return;

            button.Text = text;
            button.Click += async (s, e) => await ConfirmAndRunAsync(text, action);
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

        private void ResolveUnit()
        {
            try
            {
                var machine = FindMachine();
                _InputStageUnit = machine != null ? machine.InputStageUnit : null;
                SetEnabledState(_InputStageUnit != null);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Resolve", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                foreach (Control control in new Control[] { grpOptions, grpWait, grpManual, grpIo, grpVision, grpJog, grpSpeed })
                    control.Enabled = enabled;
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage I/O", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    StageTeachingPosition position = GetSelectedTeachingPosition();
                    if (position != null)
                        await ConfirmAndRunAsync(position.DisplayName, () => MoveByTeachingPositionAsync(position));
                });
                menu.Items.Add("Teach Current Position", null, (s, e) =>
                {
                    StageTeachingPosition position = GetSelectedTeachingPosition();
                    if (position == null)
                        return;

                    TeachPosition(position);
                    SaveCurrentRecipeData();
                    RefreshView();
                });

                optionParameterGrid.ContextMenuStrip = menu;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-STAGE", "BindParameterGridMenus failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void OptionParameterGrid_RowDoubleClicked(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                if (e == null || e.Item == null)
                    return;

                StageTeachingPosition position = FindTeachingPosition(e.Item.Key);
                if (position == null)
                    return;

                TeachPosition(position);
                SaveCurrentRecipeData();
                RefreshView();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-STAGE", "Option double click teach failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Teach", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private StageTeachingPosition GetSelectedTeachingPosition()
        {
            try
            {
                var item = optionParameterGrid.SelectedItem;
                return item != null ? FindTeachingPosition(item.Key) : null;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-STAGE", "GetSelectedTeachingPosition failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            finally
            {
            }
        }

        private StageTeachingPosition FindTeachingPosition(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            foreach (StageTeachingPosition position in TeachingPositions)
            {
                if (string.Equals(displayName, position.DisplayName, StringComparison.OrdinalIgnoreCase))
                    return position;
            }

            return null;
        }

        private async Task<int> MoveByTeachingPositionAsync(StageTeachingPosition position)
        {
            try
            {
                if (_InputStageUnit == null || position == null)
                    return -1;

                StageAxisPositions positionSet = position.PositionSetGetter(_InputStageUnit);
                BaseAxis axis = position.AxisGetter(_InputStageUnit);
                if (positionSet == null || axis == null)
                    return -1;

                return await MoveAxisAsync(axis, position.Getter(positionSet), false);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<int> MoveByKindAsync(StagePositionKind kind)
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;

                var tasks = new List<Task<int>>();
                foreach (StageTeachingPosition position in TeachingPositions)
                {
                    if (position.Kind != kind)
                        continue;

                    tasks.Add(MoveByTeachingPositionAsync(position));
                }

                if (tasks.Count == 0)
                    return 0;

                int[] results = await Task.WhenAll(tasks);
                int finalResult = 0;
                foreach (int result in results)
                {
                    if (result != 0)
                        finalResult = result;
                }

                return finalResult;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private const double PickerStageInterferenceZone = 0.0;

        // AVOID: Z축 선행 체크 없이 Z축을 먼저 후퇴(Avoid)시키고 → T → Y → X 순으로 이동
        private async Task<int> MoveAvoidSequenceAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;

                CDT320_Machine machine = FindMachine();
                const StagePositionKind kind = StagePositionKind.Avoid;
                string title = GetPositionLabel(kind);
                string reason;
                int r;

                // 1) NeedleZ/EjectPinZ Avoid로 후퇴 (VISION X와 무관) — 선행 체크 없음
                if ((r = await MoveNeedleAndEjectZAsync(kind, title)) != 0)
                    return r;

                // 2) ExpanderZ Avoid로 후퇴 (이동 직전 VISION X를 Avoid로 보장)
                if ((r = await MoveExpanderZAsync(kind, title, machine)) != 0)
                    return r;

                // 3) WAFER T — 픽커/피더 Clear 확인
                if (!CheckStagePlaneInterlock(machine, true, out reason))
                    return AbortStage(title, "WAFER T 전 " + reason);
                if (await StepMoveKindAsync(kind, "WAFER T") != 0)
                    return AbortStage(title, "WAFER T 이동 실패");

                // 4) WAFER Y — 동일 선행조건
                if (!CheckStagePlaneInterlock(machine, true, out reason))
                    return AbortStage(title, "WAFER Y 전 " + reason);
                if (await StepMoveKindAsync(kind, "WAFER Y") != 0)
                    return AbortStage(title, "WAFER Y 이동 실패");

                // 5) X축(VISION X→NEEDLE X) — VISION X 전 픽커 Avoid 확인 (ExpanderZ는 이미 이동 완료)
                if ((r = await MoveStageXAxesAsync(kind, title, machine, false)) != 0)
                    return r;

                return 0;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static bool IsAxisAtPosition(BaseAxis axis, double target)
        {
            if (axis == null)
                return false;
            double tol = (axis.Config != null && axis.Config.InPositionTolerance >= 0.0) ? axis.Config.InPositionTolerance : 0.05;
            return System.Math.Abs(axis.ActualPosition - target) <= tol;
        }

        // 스테이지 평면 이동(Wafer Y/T) 선행 인터락: 니들후퇴 + Front/Rear 픽커 Clear + 피더 Clear
        private bool CheckStagePlaneInterlock(CDT320_Machine machine, bool checkPicker, out string reason)
        {
            if (machine != null)
            {
                // 픽커 Clear = Picker Z(0~3)가 상승(Avoid) + 정상. (X/Y/T는 보지 않음)
                // (B) 픽커 XY 간섭영역 임계값 = PickerStageInterferenceZone(0, placeholder) — 추후 실측값 반영 예정
                // 막는 첫 Z축과 사유(위치/알람)를 받아 메시지에 표시. (PROCESS/RETICLE은 픽커 미적용)
                if (checkPicker)
                {
                    if (machine.PickerFrontUnit != null)
                    {
                        string block = machine.PickerFrontUnit.GetPickerZClearBlockReason();
                        if (block != null)
                        {
                            reason = "Front 픽커 Clear 아님 — " + block;
                            return false;
                        }
                    }
                    if (machine.PickerRearUnit != null)
                    {
                        string block = machine.PickerRearUnit.GetPickerZClearBlockReason();
                        if (block != null)
                        {
                            reason = "Rear 픽커 Clear 아님 — " + block;
                            return false;
                        }
                    }
                }

                InputFeederUnit feeder = machine.InputFeederUnit;
                if (feeder != null &&
                    !feeder.IsWaferFeederYInAvoidPosition() &&
                    !feeder.IsWaferFeederYInExchangePosition() &&
                    !feeder.IsWaferFeederYInHomePosition())
                {
                    reason = "Input Feeder Y Clear 아님(Avoid/Exchange/Home 아님)";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        // ===== 공통 순차 스텝 / 헬퍼 =====
        private async Task<int> StepMoveKindAsync(StagePositionKind kind, string axisLabel)
        {
            StageTeachingPosition position = FindKindPosition(kind, axisLabel);
            if (position == null)
                return -1;
            return await MoveByTeachingPositionAsync(position);
        }

        private static StageTeachingPosition FindKindPosition(StagePositionKind kind, string axisLabel)
        {
            foreach (StageTeachingPosition position in TeachingPositions)
            {
                if (position.Kind == kind &&
                    string.Equals(position.AxisLabel, axisLabel, StringComparison.OrdinalIgnoreCase))
                    return position;
            }
            return null;
        }

        private static bool IsAxisSettled(BaseAxis axis)
        {
            return axis != null && !axis.IsMoving && axis.IsInPosition;
        }

        // PROCESS 니들 상승 전제: 스테이지 정렬(WaferY/T·NeedleX) + Expander 고정(In-Position)
        private bool CheckProcessNeedleUpReady(out string reason)
        {
            reason = string.Empty;
            if (_InputStageUnit == null)
                return true;
            if (!IsAxisSettled(_InputStageUnit.StageY)) { reason = "WAFER Y 정렬 미완료"; return false; }
            if (!IsAxisSettled(_InputStageUnit.StageT)) { reason = "WAFER T 정렬 미완료"; return false; }
            if (!IsAxisSettled(_InputStageUnit.NeedleBlockX)) { reason = "NEEDLE X 정렬 미완료"; return false; }
            if (!IsAxisSettled(_InputStageUnit.ExpanderZ)) { reason = "EXPANDER Z 고정 미완료"; return false; }
            return true;
        }

        // RETICLE: 레티클 실린더가 전개(Fwd)돼 있으면 VISION X 충돌 → 후퇴(Clear) 상태여야 함
        private bool IsReticleClear(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            if (machine == null || machine.VisionUnit == null)
                return true;
            var v = machine.VisionUnit;
            if (IsCylinderDeployed(v.ReticleLift)) { reason = "ReticleLift 전개됨"; return false; }
            if (IsCylinderDeployed(v.ReticleFrontSideSlide)) { reason = "ReticleSideSlideFront 전개됨"; return false; }
            if (IsCylinderDeployed(v.ReticleRearSideSlide)) { reason = "ReticleSideSlideRear 전개됨"; return false; }
            return true;
        }

        private static bool IsCylinderDeployed(QMC.Common.IO.BaseCylinder cylinder)
        {
            return cylinder != null && cylinder.IsFwd;
        }

        // 마지막 시퀀스 중단 사유 — 실행 래퍼(ConfirmAndRunAsync)의 실패 팝업에 합쳐서 표시
        private string _lastAbortReason;

        private int AbortStage(string title, string message)
        {
            // 상세 사유는 로그(EventLogger Alarm)에 기록하고, 팝업은 래퍼의 실패 팝업 하나로 합쳐 표시한다.
            EventLogger.Write(EventKind.Alarm, "UI", "INPUT-STAGE", title + " 시퀀스 중단: " + message);
            _lastAbortReason = message;
            return -1;
        }

        // 해당 kind에 axisLabel 파라미터가 있으면 이동, 없으면 스킵(0). (옵션레이아웃에 정의된 축만 이동)
        private async Task<int> StepMoveKindIfPresentAsync(StagePositionKind kind, string axisLabel)
        {
            StageTeachingPosition position = FindKindPosition(kind, axisLabel);
            if (position == null)
                return 0;
            return await MoveByTeachingPositionAsync(position);
        }

        // NeedleZ → EjectPinZ 순으로 해당 kind 위치로 이동 (있는 축만).
        // 이 두 축은 VISION X와 간섭이 없어 단독으로 이동해도 된다.
        private async Task<int> MoveNeedleAndEjectZAsync(StagePositionKind kind, string title)
        {
            if (await StepMoveKindIfPresentAsync(kind, "NEEDLE Z") != 0) return AbortStage(title, "NEEDLE Z 이동 실패");
            if (await StepMoveKindIfPresentAsync(kind, "EJECT PIN Z") != 0) return AbortStage(title, "EJECT PIN Z 이동 실패");
            return 0;
        }

        // ExpanderZ 이동 전제(저수준 인터락): VISION X가 Avoid 위치에 있어야 한다 → 아니면 먼저 Avoid로 후퇴
        private async Task<int> EnsureVisionXAtAvoidAsync(string title, CDT320_Machine machine)
        {
            if (_InputStageUnit.IsVisionXInAvoidPosition())
                return 0;

            string reason;
            if (!CheckVisionXClear(machine, out reason))
                return AbortStage(title, "VISION X Avoid 후퇴 전 " + reason);
            if (await StepMoveKindAsync(StagePositionKind.Avoid, "VISION X") != 0)
                return AbortStage(title, "VISION X Avoid 후퇴 실패");
            return 0;
        }

        // ExpanderZ를 해당 kind 위치로 이동 (있을 때만). ExpanderZ는 VISION X가 Avoid일 때만 움직일 수 있으므로
        // 이동 직전 VISION X를 Avoid로 보장한다. (NeedleZ/EjectPinZ는 VISION X와 무관)
        private async Task<int> MoveExpanderZAsync(StagePositionKind kind, string title, CDT320_Machine machine)
        {
            if (FindKindPosition(kind, "EXPANDER Z") == null)
                return 0;

            int r;
            if ((r = await EnsureVisionXAtAvoidAsync(title, machine)) != 0)
                return r;
            if (await StepMoveKindAsync(kind, "EXPANDER Z") != 0)
                return AbortStage(title, "EXPANDER Z 이동 실패");
            return 0;
        }

        // X축: VISION X → NEEDLE X 순으로 해당 kind 위치로 이동 (있는 축만).
        // VISION X 이동 전에는 공유레일 충돌 방지를 위해 픽커 Avoid(+레티클 Clear)를 확인한다.
        private async Task<int> MoveStageXAxesAsync(StagePositionKind kind, string title, CDT320_Machine machine, bool requireReticleClear)
        {
            if (FindKindPosition(kind, "VISION X") != null)
            {
                string reason;
                if (!CheckVisionXClear(machine, out reason))
                    return AbortStage(title, "VISION X 전 " + reason);
                if (requireReticleClear && !IsReticleClear(machine, out reason))
                    return AbortStage(title, "VISION X 전 레티클 " + reason);
                if (await StepMoveKindAsync(kind, "VISION X") != 0)
                    return AbortStage(title, "VISION X 이동 실패");
            }
            if (await StepMoveKindIfPresentAsync(kind, "NEEDLE X") != 0)
                return AbortStage(title, "NEEDLE X 이동 실패");
            return 0;
        }

        // Z축(NeedleZ/EjectPinZ/ExpanderZ)이 모두 Avoid 위치에 있는지 확인
        private bool CheckStageZAxesAtAvoid(out string reason)
        {
            if (!IsAxisAtPosition(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.AvoidPosition))
            { reason = "NEEDLE Z가 Avoid 위치 아님"; return false; }
            if (!IsAxisAtPosition(_InputStageUnit.EjectPinZ, _InputStageUnit.Recipe.EjectPinZ.AvoidPosition))
            { reason = "EJECT PIN Z가 Avoid 위치 아님"; return false; }
            if (!IsAxisAtPosition(_InputStageUnit.ExpanderZ, _InputStageUnit.Recipe.WaferZ.AvoidPosition))
            { reason = "EXPANDER Z가 Avoid 위치 아님"; return false; }
            reason = string.Empty;
            return true;
        }

        // Z축(NeedleZ/ExpanderZ)이 Load 또는 Unload 위치에 있는지 확인. (EjectPinZ는 Load/Unload 파라미터 없음 → 제외)
        private bool CheckStageZAxesAtLoadOrUnload(out string reason)
        {
            if (!IsAxisAtPosition(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.LoadPosition) &&
                !IsAxisAtPosition(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.UnloadPosition))
            { reason = "NEEDLE Z가 Load/Unload 위치 아님"; return false; }
            if (!IsAxisAtPosition(_InputStageUnit.ExpanderZ, _InputStageUnit.Recipe.WaferZ.LoadPosition) &&
                !IsAxisAtPosition(_InputStageUnit.ExpanderZ, _InputStageUnit.Recipe.WaferZ.UnloadPosition))
            { reason = "EXPANDER Z가 Load/Unload 위치 아님"; return false; }
            reason = string.Empty;
            return true;
        }

        // VISION X 이동 전 공유레일 충돌 방지: Front/Rear 픽커가 Avoid 위치(Clear)인지 확인
        private bool CheckVisionXClear(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            if (machine == null)
                return true;
            if (machine.PickerFrontUnit != null && !machine.PickerFrontUnit.IsPickerInAvoidPosition())
            { reason = "Front 픽커 Avoid 위치 아님"; return false; }
            if (machine.PickerRearUnit != null && !machine.PickerRearUnit.IsPickerInAvoidPosition())
            { reason = "Rear 픽커 Avoid 위치 아님"; return false; }
            return true;
        }

        // ===== LOAD / UNLOAD: Z축 Avoid 확인 → X → Y → T → Z(체결) =====
        // (Load/Unload 그룹은 옵션레이아웃에 WaferY/WaferT/ExpanderZ/NeedleZ만 정의되어 X는 자동 스킵)
        private async Task<int> MoveLoadUnloadSequenceAsync(StagePositionKind kind)
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;
                CDT320_Machine machine = FindMachine();
                string title = GetPositionLabel(kind);
                string reason;
                int r;

                // 0) 선행: Z축들이 Avoid 위치에 있는지 확인
                if (!CheckStageZAxesAtAvoid(out reason))
                    return AbortStage(title, "Z축 " + reason);

                // 1) X (Load/Unload는 X 파라미터 없음 → 자동 스킵)
                if ((r = await MoveStageXAxesAsync(kind, title, machine, false)) != 0)
                    return r;

                // 2) WAFER Y
                if (!CheckStagePlaneInterlock(machine, true, out reason))
                    return AbortStage(title, "WAFER Y 전 " + reason);
                if (await StepMoveKindAsync(kind, "WAFER Y") != 0)
                    return AbortStage(title, "WAFER Y 이동 실패");

                // 3) WAFER T
                if (!CheckStagePlaneInterlock(machine, true, out reason))
                    return AbortStage(title, "WAFER T 전 " + reason);
                if (await StepMoveKindAsync(kind, "WAFER T") != 0)
                    return AbortStage(title, "WAFER T 이동 실패");

                // 4) ExpanderZ 체결 (이동 직전 VISION X를 Avoid로 보장)
                if ((r = await MoveExpanderZAsync(kind, title, machine)) != 0)
                    return r;

                // 5) NeedleZ 체결 (EjectPinZ는 Load/Unload 파라미터 없어 스킵)
                if ((r = await MoveNeedleAndEjectZAsync(kind, title)) != 0)
                    return r;

                return 0;
            }
            catch { throw; }
            finally { }
        }

        // ===== READY (AVOID와 동일: Z축 체크 없이 Z 먼저 후퇴 → T → Y → X) =====
        private async Task<int> MoveReadySequenceAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;
                CDT320_Machine machine = FindMachine();
                const StagePositionKind kind = StagePositionKind.Ready;
                string title = GetPositionLabel(kind);
                string reason;
                int r;

                // 1) NeedleZ/EjectPinZ Ready로 후퇴 (VISION X와 무관) — 선행 체크 없음
                if ((r = await MoveNeedleAndEjectZAsync(kind, title)) != 0)
                    return r;

                // 2) ExpanderZ Ready로 후퇴 (이동 직전 VISION X를 Avoid로 보장)
                if ((r = await MoveExpanderZAsync(kind, title, machine)) != 0)
                    return r;

                // 3) WAFER T
                if (!CheckStagePlaneInterlock(machine, true, out reason))
                    return AbortStage(title, "WAFER T 전 " + reason);
                if (await StepMoveKindAsync(kind, "WAFER T") != 0)
                    return AbortStage(title, "WAFER T 이동 실패");

                // 4) WAFER Y
                if (!CheckStagePlaneInterlock(machine, true, out reason))
                    return AbortStage(title, "WAFER Y 전 " + reason);
                if (await StepMoveKindAsync(kind, "WAFER Y") != 0)
                    return AbortStage(title, "WAFER Y 이동 실패");

                // 5) X축(VISION X→NEEDLE X) — VISION X 전 픽커 Avoid 확인 (ExpanderZ는 이미 이동 완료)
                if ((r = await MoveStageXAxesAsync(kind, title, machine, false)) != 0)
                    return r;

                return 0;
            }
            catch { throw; }
            finally { }
        }

        // ===== PROCESS: Z축 Load/Unload 확인 → X → Y → T → Z(Expander 고정 → 니들 상승) =====
        private async Task<int> MoveProcessSequenceAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;
                CDT320_Machine machine = FindMachine();
                const StagePositionKind kind = StagePositionKind.Process;
                string title = GetPositionLabel(kind);
                string reason;
                int r;

                // 0) 선행: Z축들이 Load 또는 Unload 위치에 있는지 확인 (웨이퍼 로드 상태 전제)
                if (!CheckStageZAxesAtLoadOrUnload(out reason))
                    return AbortStage(title, "Z축 " + reason);

                // 1) EXPANDER Z 고정 — ExpanderZ는 VISION X가 Avoid일 때만 이동 가능하므로 VISION X를
                //    process로 보내기 전에 먼저 수행한다. (헬퍼가 직전 VISION X Avoid 보장 → 이후 VISION X는
                //    process로 이동해 그대로 머문다. ExpanderZ를 뒤에 두면 VISION X가 다시 Avoid로 끌려감)
                if ((r = await MoveExpanderZAsync(kind, title, machine)) != 0)
                    return r;

                // 2) X축(VISION X→NEEDLE X) — VISION X 전 픽커 Avoid 확인
                if ((r = await MoveStageXAxesAsync(kind, title, machine, false)) != 0)
                    return r;

                // 3) WAFER Y — 피더 Clear (픽커 미적용)
                if (!CheckStagePlaneInterlock(machine, false, out reason)) return AbortStage(title, "WAFER Y 전 " + reason);
                if (await StepMoveKindAsync(kind, "WAFER Y") != 0) return AbortStage(title, "WAFER Y 이동 실패");

                // 4) WAFER T — Wafer Y 정렬 완료 선행
                if (!CheckStagePlaneInterlock(machine, false, out reason)) return AbortStage(title, "WAFER T 전 " + reason);
                if (!IsAxisSettled(_InputStageUnit.StageY)) return AbortStage(title, "WAFER T 전 WAFER Y 정렬 미완료");
                if (await StepMoveKindAsync(kind, "WAFER T") != 0) return AbortStage(title, "WAFER T 이동 실패");

                // 5) NEEDLE Z 상승 — 스테이지 정렬 + Expander 고정 선행
                if (!CheckProcessNeedleUpReady(out reason)) return AbortStage(title, "NEEDLE Z 상승 전 " + reason);
                if (await StepMoveKindAsync(kind, "NEEDLE Z") != 0) return AbortStage(title, "NEEDLE Z 이동 실패");

                // 6) EJECT PIN Z 상승 — 동일
                if (!CheckProcessNeedleUpReady(out reason)) return AbortStage(title, "EJECT PIN Z 상승 전 " + reason);
                if (await StepMoveKindAsync(kind, "EJECT PIN Z") != 0) return AbortStage(title, "EJECT PIN Z 이동 실패");

                return 0;
            }
            catch { throw; }
            finally { }
        }

        // ===== RETICLE: ExpanderZ(Process 기준) → VisionX만 이동. 다른 축은 현재 상태를 유지한다. =====
        private async Task<int> MoveReticleSequenceAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;
                CDT320_Machine machine = FindMachine();
                const StagePositionKind kind = StagePositionKind.Reticle;
                string title = GetPositionLabel(kind);
                int r;

                // 1) EXPANDER Z를 Process 기준 위치로 이동한다.
                //    ExpanderZ는 VISION X가 Avoid일 때만 움직일 수 있으므로 MoveExpanderZAsync 내부에서 보장한다.
                if ((r = await MoveExpanderZAsync(kind, title, machine)) != 0)
                    return r;

                // 2) VISION X만 Reticle 위치로 이동한다. WAFER Y/T, Needle, EjectPin은 현재 위치를 유지한다.
                if ((r = await MoveStageXAxesAsync(kind, title, machine, true)) != 0)
                    return r;

                return 0;
            }
            catch { throw; }
            finally { }
        }

        private void TeachPosition(StageTeachingPosition position)
        {
            try
            {
                if (_InputStageUnit == null || position == null)
                    return;

                StageAxisPositions positionSet = position.PositionSetGetter(_InputStageUnit);
                BaseAxis axis = position.AxisGetter(_InputStageUnit);
                if (positionSet == null || axis == null)
                    return;

                position.Setter(positionSet, axis.ActualPosition);
                EventLogger.Write(EventKind.Event, "UI", "INPUT-STAGE", position.DisplayName + " taught=" + axis.ActualPosition.ToString("F3", CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Teach", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindParameterGrids()
        {
            try
            {
                if (_InputStageUnit == null)
                    return;

                optionParameterGrid.SetItems(BuildOptionItems());
                waitParameterGrid.SetItems(BuildWaitItems());
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Parameters", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private IEnumerable<ParameterGridItem> BuildOptionItems()
        {
            try
            {
                var unit = _InputStageUnit;
                unit.Recipe.EnsurePositionObjects();

                var items = new List<ParameterGridItem>();
                AddStagePositions(items, unit);
                
                items.Add(AxisDouble("WORK AREA RADIUS", ParameterGridScope.Setup, unit.StageY, () => unit.Setup.WorkAreaRadius, v => unit.Setup.WorkAreaRadius = Math.Max(0.0, v)));
                items.Add(AxisDouble("NEEDLE WORK AREA RADIUS", ParameterGridScope.Setup, unit.StageY, () => unit.Setup.NeedleWorkAreaRadius, v => unit.Setup.NeedleWorkAreaRadius = Math.Max(0.0, v)));
                items.Add(AxisDouble("VISION WORK AREA CENTER X", ParameterGridScope.Setup, unit.CameraX, () => unit.Setup.WorkAreaCenterX, v => unit.Setup.WorkAreaCenterX = v));
                items.Add(AxisDouble("VISION WORK AREA CENTER Y", ParameterGridScope.Setup, unit.StageY, () => unit.Setup.WorkAreaCenterY, v => unit.Setup.WorkAreaCenterY = v));
                items.Add(AxisDouble("NEEDLE WORK AREA CENTER X", ParameterGridScope.Setup, unit.NeedleBlockX, () => unit.Setup.NeedleWorkAreaCenterX, v => unit.Setup.NeedleWorkAreaCenterX = v));
                items.Add(AxisDouble("NEEDLE WORK AREA CENTER Y", ParameterGridScope.Setup, unit.StageY, () => unit.Setup.NeedleWorkAreaCenterY, v => unit.Setup.NeedleWorkAreaCenterY = v));
                items.Add(AxisDouble("NEEDLE X TO VISION X OFFSET", ParameterGridScope.Setup, unit.CameraX, () => unit.Setup.NeedleXToVisionXOffset, v => unit.Setup.NeedleXToVisionXOffset = v));
                items.Add(ParameterGridItem.Int("BARCODE READ TIMEOUT", "ms", ParameterGridScope.Setup, () => unit.Setup.BarcodeReadTimeoutMs, v => unit.Setup.BarcodeReadTimeoutMs = Math.Max(0, v)));
                items.Add(ParameterGridItem.Int("ALIGN ITERATIONS", "count", ParameterGridScope.Config, () => unit.Config.MaxAlignIterations, v => unit.Config.MaxAlignIterations = Math.Max(1, v)));
                items.Add(ParameterGridItem.Double("ALIGN THRESHOLD", "deg", ParameterGridScope.Config, () => unit.Config.AlignConvergenceThresholdDeg, v => unit.Config.AlignConvergenceThresholdDeg = Math.Max(0.0, v)));
                AddNeedlePickUpSettingItems(items, unit);
                items.Add(ParameterGridItem.Bool("CONFIG DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v));
                items.Add(ParameterGridItem.Bool("SETUP SIMULATION MODE", ParameterGridScope.Setup, () => unit.Setup.IsSimulationMode, v => unit.Setup.IsSimulationMode = v));
                return items;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void AddNeedlePickUpSettingItems(List<ParameterGridItem> items, InputStageUnit unit)
        {
            const string groupKey = "NEEDLE_PICKUP_SETTING";
            unit.Config.EnsurePickUpMotionDefaults();
            items.Add(ParameterGridItem.Header("NEEDLE PICKUP SETTING", groupKey));
            items.Add(InGroup(AxisDouble("EJECT PIN PICK OFFSET", ParameterGridScope.Config, unit.EjectPinZ, () => unit.Config.PickUpEjectPinOffset, v => unit.Config.PickUpEjectPinOffset = v), groupKey));
            items.Add(InGroup(AxisDouble("EJECT PIN PICK VELOCITY", ParameterGridScope.Config, unit.EjectPinZ, () => unit.Config.PickUpEjectPinSpeed, v => unit.Config.PickUpEjectPinSpeed = Math.Max(0.0, v), "/s"), groupKey));
            items.Add(InGroup(AxisDouble("EJECT PIN PICK ACC", ParameterGridScope.Config, unit.EjectPinZ, () => unit.Config.PickUpEjectPinAcc, v => unit.Config.PickUpEjectPinAcc = Math.Max(0.0, v), "/s2"), groupKey));
            items.Add(InGroup(AxisDouble("EJECT PIN PICK DEC", ParameterGridScope.Config, unit.EjectPinZ, () => unit.Config.PickUpEjectPinDec, v => unit.Config.PickUpEjectPinDec = Math.Max(0.0, v), "/s2"), groupKey));
            items.Add(InGroup(AxisDouble("NEEDLE SYNC LIFT DISTANCE", ParameterGridScope.Config, unit.NeedleZ, () => unit.Config.PickUpNeedleSyncLiftDistance, v => unit.Config.PickUpNeedleSyncLiftDistance = Math.Max(0.0, v)), groupKey));
            items.Add(InGroup(AxisDouble("NEEDLE SYNC LIFT VELOCITY", ParameterGridScope.Config, unit.NeedleZ, () => unit.Config.PickUpNeedleSyncLiftVelocity, v => unit.Config.PickUpNeedleSyncLiftVelocity = Math.Max(0.0, v), "/s"), groupKey));
            items.Add(InGroup(AxisDouble("NEEDLE SYNC LIFT ACC", ParameterGridScope.Config, unit.NeedleZ, () => unit.Config.PickUpNeedleSyncLiftAcc, v => unit.Config.PickUpNeedleSyncLiftAcc = Math.Max(0.0, v), "/s2"), groupKey));
            items.Add(InGroup(AxisDouble("NEEDLE SYNC LIFT DEC", ParameterGridScope.Config, unit.NeedleZ, () => unit.Config.PickUpNeedleSyncLiftDec, v => unit.Config.PickUpNeedleSyncLiftDec = Math.Max(0.0, v), "/s2"), groupKey));
            items.Add(InGroup(AxisDouble("NEEDLE SEPARATE DISTANCE", ParameterGridScope.Config, unit.NeedleZ, () => unit.Config.PickUpNeedleSeparateDistance, v => unit.Config.PickUpNeedleSeparateDistance = Math.Max(0.0, v)), groupKey));
            items.Add(InGroup(ParameterGridItem.Double("NEEDLE SEPARATE SPEED", "%", ParameterGridScope.Config, () => unit.Config.PickUpNeedleSeparateSpeedPercent, v => unit.Config.PickUpNeedleSeparateSpeedPercent = PickerPickUpMotionConfig.NormalizePercent(v, 1.0)), groupKey));
        }

        private static ParameterGridItem InGroup(ParameterGridItem item, string groupKey)
        {
            item.GroupKey = groupKey;
            return item;
        }

        private static readonly StagePositionKind[] OptionKindOrder =
        {
            StagePositionKind.Avoid,
            StagePositionKind.Load,
            StagePositionKind.Unload,
            StagePositionKind.Ready,
            StagePositionKind.Process,
            StagePositionKind.Reticle
        };

        private void AddStagePositions(List<ParameterGridItem> items, InputStageUnit unit)
        {
            foreach (StagePositionKind kind in OptionKindOrder)
            {
                string groupKey = "POS_" + kind;
                ParameterGridItem header = ParameterGridItem.Header(GetPositionLabel(kind), groupKey);
                if (kind == StagePositionKind.Reticle)
                    header.Description = "Vision Cal Position";
                items.Add(header);

                foreach (StageTeachingPosition position in TeachingPositions)
                {
                    if (position.Kind != kind)
                        continue;

                    StageTeachingPosition captured = position;
                    BaseAxis axis = captured.AxisGetter(unit);
                    ParameterGridItem item = AxisDouble(captured.AxisLabel, ParameterGridScope.Recipe, axis,
                        () => captured.Getter(captured.PositionSetGetter(unit)),
                        v => captured.Setter(captured.PositionSetGetter(unit), v));
                    item.Key = captured.DisplayName;   // 더블클릭/메뉴 티칭 조회는 원래 이름(DisplayName)으로 매칭
                    item.GroupKey = groupKey;
                    items.Add(item);
                }
            }
        }

        private IEnumerable<ParameterGridItem> BuildWaitItems()
        {
            try
            {
                return new ParameterGridItem[0];
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void BindIoPanel()
        {
            try
            {
                if (_InputStageUnit == null)
                    return;

                ioCylinderPanel.SetItems(new[]
                {
                    // ===== INPUT (DI) — 3개 =====
                    IoCylinderItem.Input("WAFER STAGE 8\" RING CHECK", () => _InputStageUnit.WaferStage8RingCheckSensor != null && _InputStageUnit.WaferStage8RingCheckSensor.IsOn),
                    IoCylinderItem.Input("WAFER STAGE 12\" RING CHECK", () => _InputStageUnit.WaferStage12RingCheckSensor != null && _InputStageUnit.WaferStage12RingCheckSensor.IsOn),
                    IoCylinderItem.Input("WAFER STAGE TOUCH SENSOR", () => _InputStageUnit.WaferStageTouchSensor != null && _InputStageUnit.WaferStageTouchSensor.IsOn),

                    // ===== OUTPUT (DO) — 3개 =====
                    IoCylinderItem.Output("IONIZER ON", () => _InputStageUnit.Ionizer != null && _InputStageUnit.Ionizer.IsOn, WriteIonizerAsync),
                    IoCylinderItem.Output("NEEDLE VACUUM", () => _InputStageUnit.NeedleVacuum != null && _InputStageUnit.NeedleVacuum.IsOn, WriteNeedleVacuumAsync),
                    IoCylinderItem.Output("NEEDLE BLOW", () => _InputStageUnit.NeedleBlow != null && _InputStageUnit.NeedleBlow.IsOn, WriteNeedleBlowAsync)
                });
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage I/O", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindJogPanel()
        {
            try
            {
                if (_InputStageUnit == null)
                    return;

                var items = new List<JogAxisItem>
                {
                    BuildJogAxis("StageY", _InputStageUnit.StageY, "Y+", "Y-", JogAxisControlKind.CrossWithT),
                    BuildJogAxis("StageT", _InputStageUnit.StageT, "T+", "T-", JogAxisControlKind.CrossWithT),
                    BuildJogAxis("ExpanderZ", _InputStageUnit.ExpanderZ, "Z+", "Z-", JogAxisControlKind.Vertical),
                    BuildJogAxis("CameraX", _InputStageUnit.CameraX, "X+", "X-", JogAxisControlKind.CrossWithT),
                    BuildJogAxis("NeedleX", _InputStageUnit.NeedleBlockX, "X+", "X-", JogAxisControlKind.Horizontal),
                    BuildJogAxis("NeedleZ", _InputStageUnit.NeedleZ, "Z+", "Z-", JogAxisControlKind.Vertical),
                    BuildJogAxis("EjectPinZ", _InputStageUnit.EjectPinZ, "Z+", "Z-", JogAxisControlKind.Vertical)
                };

                jogPositionListControl.SetItems(items);
                jogAxisMoveControl.SetItems(items);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private JogAxisItem BuildJogAxis(string name, BaseAxis axis, string plus, string minus, JogAxisControlKind kind)
        {
            JogAxisItem item = JogAxisItem.Single(name, axis, string.Empty, 1.0, plus, minus).WithControlKind(kind);
            item.StepMoveAsync = (it, direction, speedType, customSpeed, axisStepDistance) =>
                _InputStageUnit.JogStepAsync(axis, direction, speedType, customSpeed, axisStepDistance);
            item.ContinuousMoveAsync = (it, direction, speedType, customSpeed) =>
                _InputStageUnit.JogContinuousAsync(axis, direction, speedType, customSpeed);
            item.StopAsync = it => _InputStageUnit.StopJogAsync(axis);
            return item;
        }

        private async Task<int> WriteNeedleVacuumAsync(bool value)
        {
            try
            {
                if (_InputStageUnit == null || _InputStageUnit.NeedleVacuum == null)
                    return -1;

                _InputStageUnit.NeedleVacuum.Write(value);
                await Task.CompletedTask;
                return 0;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-STAGE", "Needle vacuum write failed: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> WriteNeedleBlowAsync(bool value)
        {
            try
            {
                if (_InputStageUnit == null || _InputStageUnit.NeedleBlow == null)
                    return -1;

                _InputStageUnit.NeedleBlow.Write(value);
                await Task.CompletedTask;
                return 0;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-STAGE", "Needle blow write failed: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> WriteIonizerAsync(bool value)
        {
            try
            {
                if (_InputStageUnit == null || _InputStageUnit.Ionizer == null)
                    return -1;

                _InputStageUnit.Ionizer.Write(value);
                await Task.CompletedTask;
                return 0;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-STAGE", "Ionizer write failed: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private void ParameterGrid_ParameterValueChanged(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                if (e == null || e.Item == null)
                    return;

                if (e.Item.Scope == ParameterGridScope.Recipe)
                    SaveCurrentRecipeData();
                else
                    SaveCurrentSettingsData();

                RefreshView();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        
        private async Task ConfirmAndRunAsync(string actionName, Func<Task<int>> action)
        {
            try
            {
                if (_InputStageUnit == null || action == null)
                    return;

                if (ManualMoveGuard.BlockIfNotReady(this, "Input Stage"))
                    return;

                DialogResult confirm = QMC.Common.MessageDialog.Show(this, actionName + " 진행하시겠습니까?", "Input Stage", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                Cursor = Cursors.WaitCursor;
                _lastAbortReason = null;
                int result = await action();
                EventLogger.Write(EventKind.Event, "UI", "INPUT-STAGE", actionName + " result=" + result);
                if (result != 0)
                {
                    string detail = string.IsNullOrEmpty(_lastAbortReason) ? "" : Environment.NewLine + "사유 : " + _lastAbortReason;
                    QMC.Common.MessageDialog.Show(this, actionName + " 실패" + detail, "Input Stage", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private async Task<int> MoveAxisAsync(BaseAxis axis, double target, bool bFine)
        {
            try
            {
                if (_InputStageUnit == null || axis == null)
                    return -1;

                WaferStageAxis stageAxis;
                if (TryResolveStageAxis(axis, out stageAxis))
                    return await _InputStageUnit.MoveInputStageAxis(stageAxis, target, bFine);

                return await axis.MoveAbsoluteAsync(target, bFine ? ResolveAxisFineVelocity(axis) : ResolveAxisVelocity(axis));
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private bool TryResolveStageAxis(BaseAxis axis, out WaferStageAxis stageAxis)
        {
            stageAxis = WaferStageAxis.WaferY;
            if (_InputStageUnit == null || axis == null)
                return false;

            if (ReferenceEquals(axis, _InputStageUnit.StageY))
            {
                stageAxis = WaferStageAxis.WaferY;
                return true;
            }

            if (ReferenceEquals(axis, _InputStageUnit.StageT))
            {
                stageAxis = WaferStageAxis.WaferT;
                return true;
            }

            if (ReferenceEquals(axis, _InputStageUnit.ExpanderZ))
            {
                stageAxis = WaferStageAxis.WaferExpandingZ;
                return true;
            }

            if (ReferenceEquals(axis, _InputStageUnit.CameraX))
            {
                stageAxis = WaferStageAxis.VisionX;
                return true;
            }

            if (ReferenceEquals(axis, _InputStageUnit.NeedleBlockX))
            {
                stageAxis = WaferStageAxis.NeedleX;
                return true;
            }

            if (ReferenceEquals(axis, _InputStageUnit.NeedleZ))
            {
                stageAxis = WaferStageAxis.NeedleZ;
                return true;
            }

            if (ReferenceEquals(axis, _InputStageUnit.EjectPinZ))
            {
                stageAxis = WaferStageAxis.EjectPinZ;
                return true;
            }

            return false;
        }

        private async Task<int> MoveCenterAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;

                Task<int> moveY = MoveAxisAsync(_InputStageUnit.StageY, _InputStageUnit.Recipe.WaferY.ReadyPosition, false);
                Task<int> moveX = MoveAxisAsync(_InputStageUnit.CameraX, _InputStageUnit.Recipe.VisionX.ReadyPosition, false);
                int[] results = await Task.WhenAll(moveY, moveX);
                return results[0] != 0 ? results[0] : results[1];
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<int> MoveCameraOriginAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;

                return await MoveAxisAsync(_InputStageUnit.CameraX, _InputStageUnit.Recipe.VisionX.ReadyPosition, false);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<int> MoveFirstDieAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;

                double targetX = Math.Abs(_InputStageUnit.OriginX) > 0.000001 ? _InputStageUnit.OriginX : _InputStageUnit.Recipe.VisionX.ReadyPosition;
                double targetY = Math.Abs(_InputStageUnit.OriginY) > 0.000001 ? _InputStageUnit.OriginY : _InputStageUnit.Recipe.WaferY.ReadyPosition;

                Task<int> moveY = MoveAxisAsync(_InputStageUnit.StageY, targetY, false);
                Task<int> moveX = MoveAxisAsync(_InputStageUnit.CameraX, targetX, false);
                int[] results = await Task.WhenAll(moveY, moveX);
                return results[0] != 0 ? results[0] : results[1];
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<int> PickTestAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;

                // NeedleZ: Load → Process → Avoid 경유 → Unload (Process→Unload 직접 전이는
                // 인터락(비공정 이동 전 NeedleZ Avoid 필요)에 막히므로 중간에 Avoid를 경유한다)
                int result = await MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.LoadPosition, false);
                if (result != 0)
                    return result;

                await Task.Delay(100);
                result = await MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.ProcessPosition, false);
                if (result != 0)
                    return result;

                await Task.Delay(100);
                result = await MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.AvoidPosition, false);
                if (result != 0)
                    return result;

                await Task.Delay(100);
                return await MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.UnloadPosition, false);
            }
            catch
            {
                throw;
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
                if (_InputStageUnit == null)
                    return;

                optionParameterGrid.RefreshValues();
                waitParameterGrid.RefreshValues();
                ioCylinderPanel.RefreshStates();
                jogPositionListControl.RefreshState();

                lblVisionInfo.Text =
                    "VISION VIEW" + Environment.NewLine +
                    "Origin X : " + FormatAxis(_InputStageUnit.OriginX, _InputStageUnit.CameraX) + Environment.NewLine +
                    "Origin Y : " + FormatAxis(_InputStageUnit.OriginY, _InputStageUnit.StageY) + Environment.NewLine +
                    "Pitch X  : " + FormatAxis(_InputStageUnit.PitchX, _InputStageUnit.CameraX) + Environment.NewLine +
                    "Pitch Y  : " + FormatAxis(_InputStageUnit.PitchY, _InputStageUnit.StageY) + Environment.NewLine +
                    "Align X  : " + FormatAxis(_InputStageUnit.WaferAlignOffsetX, _InputStageUnit.CameraX) + Environment.NewLine +
                    "Align Y  : " + FormatAxis(_InputStageUnit.WaferAlignOffsetY, _InputStageUnit.StageY) + Environment.NewLine +
                    "Needle Vacuum : " + OnOff(_InputStageUnit.NeedleVacuum != null && _InputStageUnit.NeedleVacuum.IsOn);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private ParameterGridItem AxisDouble(string displayName, ParameterGridScope scope, BaseAxis axis, Func<double> getter, Action<double> setter, string unitSuffix = "")
        {
            ParameterGridItem item = ParameterGridItem.Double(
                displayName,
                AxisUnitConverter.DisplayUnitFor(axis) + unitSuffix,
                scope,
                () => AxisUnitConverter.ToDisplay(getter(), axis),
                v => setter(AxisUnitConverter.FromDisplay(v, axis)));
            item.UnitGetter = () => AxisUnitConverter.DisplayUnitFor(axis) + unitSuffix;
            return item;
        }

        private static string FormatAxis(double value, BaseAxis axis)
        {
            try
            {
                return AxisUnitConverter.FormatDisplay(value, axis, "0.###", true);
            }
            catch
            {
                return "0 " + AxisUnitConverter.DisplayUnitFor(axis);
            }
            finally
            {
            }
        }

        private static double ResolveAxisVelocity(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.DefaultVelocity > 0.0
                ? axis.Config.DefaultVelocity
                : 100.0;
        }

        private static double ResolveAxisFineVelocity(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.JogFineVelocity > 0.0
                ? axis.Config.JogFineVelocity
                : ResolveAxisVelocity(axis);
        }

        private static string OnOff(bool value)
        {
            try
            {
                return value ? "ON" : "OFF";
            }
            catch
            {
                return "OFF";
            }
            finally
            {
            }
        }
    }
}
