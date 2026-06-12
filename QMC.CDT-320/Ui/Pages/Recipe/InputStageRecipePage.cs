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
                case StagePositionKind.Avoid:
                    AddTeachingPosition(positions, axis, kind, set => set.AvoidPosition, (set, value) => set.AvoidPosition = value);
                    break;
                case StagePositionKind.Load:
                    AddTeachingPosition(positions, axis, kind, set => set.LoadPosition, (set, value) => set.LoadPosition = value);
                    break;
                case StagePositionKind.Process:
                    AddTeachingPosition(positions, axis, kind, set => set.ProcessPosition, (set, value) => set.ProcessPosition = value);
                    break;
                case StagePositionKind.Unload:
                    AddTeachingPosition(positions, axis, kind, set => set.UnloadPosition, (set, value) => set.UnloadPosition = value);
                    break;
                case StagePositionKind.Ready:
                    AddTeachingPosition(positions, axis, kind, set => set.ReadyPosition, (set, value) => set.ReadyPosition = value);
                    break;
                case StagePositionKind.Reticle:
                    AddTeachingPosition(positions, axis, kind, set => set.ReticlePosition, (set, value) => set.ReticlePosition = value);
                    break;
            }
        }

        private static string GetPositionLabel(StagePositionKind kind)
        {
            switch (kind)
            {
                case StagePositionKind.Avoid:
                    return "AVOID POSITION";
                case StagePositionKind.Load:
                    return "LOAD POSITION";
                case StagePositionKind.Process:
                    return "PROCESS POSITION";
                case StagePositionKind.Unload:
                    return "UNLOAD POSITION";
                case StagePositionKind.Ready:
                    return "READY POSITION";
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

        // 매뉴얼 액션 버튼(Designer 배치)의 Click 핸들러 — 각 위치 종류의 인터락 시퀀스로 이동
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

        // ===== AVOID 순차 이동 (단계별 인터락 확인, 실패 시 전체 중단 + 알람) =====
        // TODO(인터락): 픽커-인풋스테이지 XY 간섭영역 임계값. 우선 0(placeholder)로 두고 추후 실측값 반영.
        private const double PickerStageInterferenceZone = 0.0;

        private async Task<int> MoveAvoidSequenceAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;

                CDT320_Machine machine = FindMachine();
                string reason;

                // 원점복귀(homing) 완료 전에는 절대좌표 이동 금지 → 시작하지 않음 (RequireHomingForHomeLikeButtons로 on/off)
                if (RequireHomingForHomeLikeButtons && !CheckStageAxesHomed(out reason))
                    return AbortAvoid(reason);

                // 1) NEEDLE Z 하강(후퇴)
                if (await StepMoveAvoidAsync("NEEDLE Z") != 0)
                    return AbortAvoid("NEEDLE Z 이동 실패");

                // 2) EJECT PIN Z 하강(후퇴)
                if (await StepMoveAvoidAsync("EJECT PIN Z") != 0)
                    return AbortAvoid("EJECT PIN Z 이동 실패");

                // 3) VISION X (공유레일 충돌검증은 이동 경로에서 자동 적용)
                if (await StepMoveAvoidAsync("VISION X") != 0)
                    return AbortAvoid("VISION X 이동 실패");

                // 4) EXPANDER Z — 니들/이젝트핀 후퇴 완료 선행
                if (!IsNeedleRetracted(out reason))
                    return AbortAvoid("EXPANDER Z 전 " + reason);
                if (await StepMoveAvoidAsync("EXPANDER Z") != 0)
                    return AbortAvoid("EXPANDER Z 이동 실패");

                // 5) NEEDLE X — 니들/이젝트핀 후퇴 완료 선행
                if (!IsNeedleRetracted(out reason))
                    return AbortAvoid("NEEDLE X 전 " + reason);
                if (await StepMoveAvoidAsync("NEEDLE X") != 0)
                    return AbortAvoid("NEEDLE X 이동 실패");

                // 6) WAFER Y — 니들후퇴 + Front/Rear 픽커 Clear + 피더 Clear
                if (!CheckStagePlaneInterlock(machine, true, out reason))
                    return AbortAvoid("WAFER Y 전 " + reason);
                if (await StepMoveAvoidAsync("WAFER Y") != 0)
                    return AbortAvoid("WAFER Y 이동 실패");

                // 7) WAFER T — 6번과 동일 선행조건
                if (!CheckStagePlaneInterlock(machine, true, out reason))
                    return AbortAvoid("WAFER T 전 " + reason);
                if (await StepMoveAvoidAsync("WAFER T") != 0)
                    return AbortAvoid("WAFER T 이동 실패");

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

        private async Task<int> StepMoveAvoidAsync(string axisLabel)
        {
            StageTeachingPosition position = FindAvoidPosition(axisLabel);
            if (position == null)
                return -1;

            return await MoveByTeachingPositionAsync(position);
        }

        private static StageTeachingPosition FindAvoidPosition(string axisLabel)
        {
            foreach (StageTeachingPosition position in TeachingPositions)
            {
                if (position.Kind == StagePositionKind.Avoid &&
                    string.Equals(position.AxisLabel, axisLabel, StringComparison.OrdinalIgnoreCase))
                    return position;
            }

            return null;
        }

        // 니들 Z / 이젝트핀 Z 가 "상승(Process/이젝트) 위치가 아님" = 후퇴(하강) 상태인지 판정.
        // Avoid/Load/Unload/Ready/Reticle 의 니들 위치는 모두 '하강'이라 kind마다 값이 달라도 통과한다.
        // 실제 위험은 "니들이 올라가(이젝트) 있음"뿐이므로, Process 위치에만 있지 않으면 후퇴로 본다.
        private bool IsNeedleRetracted(out string reason)
        {
            reason = string.Empty;
            if (_InputStageUnit == null)
                return true;

            var u = _InputStageUnit;
            u.Recipe.EnsurePositionObjects();

            if (IsAxisAtPosition(u.NeedleZ, u.Recipe.NeedleZ.ProcessPosition))
            {
                reason = "NEEDLE Z 후퇴 미완료(상승/이젝트 위치)";
                return false;
            }
            if (IsAxisAtPosition(u.EjectPinZ, u.Recipe.EjectPinZ.ProcessPosition))
            {
                reason = "EJECT PIN Z 후퇴 미완료(상승/이젝트 위치)";
                return false;
            }

            return true;
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
            if (!IsNeedleRetracted(out reason))
                return false;

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

        private int AbortAvoid(string message)
        {
            return AbortStage("AVOID", message);
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

        // 원점복귀(homing) 완료 여부 — 미완료 축이 있으면 절대좌표 이동을 막는다.
        private static bool IsAxisHomed(BaseAxis axis, string label, out string reason)
        {
            reason = string.Empty;
            if (axis != null && !axis.IsHomeDone)
            {
                reason = label + " 원점복귀 필요";
                return false;
            }
            return true;
        }

        // 7축(AVOID/READY/PROCESS/RETICLE) 원점복귀 완료 확인
        private bool CheckStageAxesHomed(out string reason)
        {
            reason = string.Empty;
            if (_InputStageUnit == null)
                return true;
            var u = _InputStageUnit;
            return IsAxisHomed(u.NeedleZ, "NEEDLE Z", out reason)
                && IsAxisHomed(u.EjectPinZ, "EJECT PIN Z", out reason)
                && IsAxisHomed(u.ExpanderZ, "EXPANDER Z", out reason)
                && IsAxisHomed(u.CameraX, "VISION X", out reason)
                && IsAxisHomed(u.NeedleBlockX, "NEEDLE X", out reason)
                && IsAxisHomed(u.StageY, "WAFER Y", out reason)
                && IsAxisHomed(u.StageT, "WAFER T", out reason);
        }

        // LOAD/UNLOAD 4축 원점복귀 완료 확인
        private bool CheckLoadUnloadAxesHomed(out string reason)
        {
            reason = string.Empty;
            if (_InputStageUnit == null)
                return true;
            var u = _InputStageUnit;
            return IsAxisHomed(u.NeedleZ, "NEEDLE Z", out reason)
                && IsAxisHomed(u.ExpanderZ, "EXPANDER Z", out reason)
                && IsAxisHomed(u.StageT, "WAFER T", out reason)
                && IsAxisHomed(u.StageY, "WAFER Y", out reason);
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

        // 원점복귀(homing) 확인 on/off. 테스트 시 false 로 두면 AVOID/LOAD/UNLOAD가 원점복귀 미완료여도 진행. 운영 시 true.
        private static readonly bool RequireHomingForHomeLikeButtons = true;

        // 마지막 시퀀스 중단 사유 — 실행 래퍼(ConfirmAndRunAsync)의 실패 팝업에 합쳐서 표시
        private string _lastAbortReason;

        private int AbortStage(string title, string message)
        {
            // 상세 사유는 로그(EventLogger Alarm)에 기록하고, 팝업은 래퍼의 실패 팝업 하나로 합쳐 표시한다.
            EventLogger.Write(EventKind.Alarm, "UI", "INPUT-STAGE", title + " 시퀀스 중단: " + message);
            _lastAbortReason = message;
            return -1;
        }

        // ===== LOAD / UNLOAD (4축: NeedleZ → ExpanderZ → WaferT → WaferY) =====
        private async Task<int> MoveLoadUnloadSequenceAsync(StagePositionKind kind)
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;
                CDT320_Machine machine = FindMachine();
                string title = GetPositionLabel(kind);
                string reason;

                if (RequireHomingForHomeLikeButtons && !CheckLoadUnloadAxesHomed(out reason))
                    return AbortStage(title, reason);

                // 1) NEEDLE Z
                if (await StepMoveKindAsync(kind, "NEEDLE Z") != 0)
                    return AbortStage(title, "NEEDLE Z 이동 실패");

                // 2) EXPANDER Z — 니들후퇴 선행
                if (!IsNeedleRetracted(out reason))
                    return AbortStage(title, "EXPANDER Z 전 " + reason);
                if (await StepMoveKindAsync(kind, "EXPANDER Z") != 0)
                    return AbortStage(title, "EXPANDER Z 이동 실패");

                // 3) WAFER T — 니들후퇴 + 픽커Clear + 피더
                if (!CheckStagePlaneInterlock(machine, true, out reason))
                    return AbortStage(title, "WAFER T 전 " + reason);
                if (await StepMoveKindAsync(kind, "WAFER T") != 0)
                    return AbortStage(title, "WAFER T 이동 실패");

                // 4) WAFER Y — 동일
                if (!CheckStagePlaneInterlock(machine, true, out reason))
                    return AbortStage(title, "WAFER Y 전 " + reason);
                if (await StepMoveKindAsync(kind, "WAFER Y") != 0)
                    return AbortStage(title, "WAFER Y 이동 실패");

                return 0;
            }
            catch { throw; }
            finally { }
        }

        // ===== READY (7축, AVOID와 동일 틀) =====
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

                if (await StepMoveKindAsync(kind, "NEEDLE Z") != 0) return AbortStage(title, "NEEDLE Z 이동 실패");
                if (await StepMoveKindAsync(kind, "EJECT PIN Z") != 0) return AbortStage(title, "EJECT PIN Z 이동 실패");
                if (await StepMoveKindAsync(kind, "VISION X") != 0) return AbortStage(title, "VISION X 이동 실패");

                if (!IsNeedleRetracted(out reason)) return AbortStage(title, "EXPANDER Z 전 " + reason);
                if (await StepMoveKindAsync(kind, "EXPANDER Z") != 0) return AbortStage(title, "EXPANDER Z 이동 실패");

                if (!IsNeedleRetracted(out reason)) return AbortStage(title, "NEEDLE X 전 " + reason);
                if (await StepMoveKindAsync(kind, "NEEDLE X") != 0) return AbortStage(title, "NEEDLE X 이동 실패");

                if (!CheckStagePlaneInterlock(machine, true, out reason)) return AbortStage(title, "WAFER Y 전 " + reason);
                if (await StepMoveKindAsync(kind, "WAFER Y") != 0) return AbortStage(title, "WAFER Y 이동 실패");

                if (!CheckStagePlaneInterlock(machine, true, out reason)) return AbortStage(title, "WAFER T 전 " + reason);
                if (await StepMoveKindAsync(kind, "WAFER T") != 0) return AbortStage(title, "WAFER T 이동 실패");

                return 0;
            }
            catch { throw; }
            finally { }
        }

        // ===== PROCESS (7축, 수평 먼저 → 니들 상승 마지막) =====
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

                // 1) WAFER Y — 니들후퇴 + 피더 (픽커 미적용)
                if (!CheckStagePlaneInterlock(machine, false, out reason)) return AbortStage(title, "WAFER Y 전 " + reason);
                if (await StepMoveKindAsync(kind, "WAFER Y") != 0) return AbortStage(title, "WAFER Y 이동 실패");

                // 2) WAFER T — Wafer Y 정렬 완료 선행
                if (!CheckStagePlaneInterlock(machine, false, out reason)) return AbortStage(title, "WAFER T 전 " + reason);
                if (!IsAxisSettled(_InputStageUnit.StageY)) return AbortStage(title, "WAFER T 전 WAFER Y 정렬 미완료");
                if (await StepMoveKindAsync(kind, "WAFER T") != 0) return AbortStage(title, "WAFER T 이동 실패");

                // 3) NEEDLE X
                if (!IsNeedleRetracted(out reason)) return AbortStage(title, "NEEDLE X 전 " + reason);
                if (await StepMoveKindAsync(kind, "NEEDLE X") != 0) return AbortStage(title, "NEEDLE X 이동 실패");

                // 4) VISION X
                if (await StepMoveKindAsync(kind, "VISION X") != 0) return AbortStage(title, "VISION X 이동 실패");

                // 5) EXPANDER Z (하강/고정)
                if (await StepMoveKindAsync(kind, "EXPANDER Z") != 0) return AbortStage(title, "EXPANDER Z 이동 실패");

                // 6) NEEDLE Z 상승 — 스테이지 정렬 + Expander 고정 선행
                if (!CheckProcessNeedleUpReady(out reason)) return AbortStage(title, "NEEDLE Z 상승 전 " + reason);
                if (await StepMoveKindAsync(kind, "NEEDLE Z") != 0) return AbortStage(title, "NEEDLE Z 이동 실패");

                // 7) EJECT PIN Z 상승 — 동일
                if (!CheckProcessNeedleUpReady(out reason)) return AbortStage(title, "EJECT PIN Z 상승 전 " + reason);
                if (await StepMoveKindAsync(kind, "EJECT PIN Z") != 0) return AbortStage(title, "EJECT PIN Z 이동 실패");

                return 0;
            }
            catch { throw; }
            finally { }
        }

        // ===== RETICLE (7축, VISION X 마지막) =====
        private async Task<int> MoveReticleSequenceAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;
                CDT320_Machine machine = FindMachine();
                const StagePositionKind kind = StagePositionKind.Reticle;
                string title = GetPositionLabel(kind);
                string reason;

                if (await StepMoveKindAsync(kind, "NEEDLE Z") != 0) return AbortStage(title, "NEEDLE Z 이동 실패");
                if (await StepMoveKindAsync(kind, "EJECT PIN Z") != 0) return AbortStage(title, "EJECT PIN Z 이동 실패");
                if (await StepMoveKindAsync(kind, "EXPANDER Z") != 0) return AbortStage(title, "EXPANDER Z 이동 실패");

                if (!CheckStagePlaneInterlock(machine, false, out reason)) return AbortStage(title, "WAFER Y 전 " + reason);
                if (await StepMoveKindAsync(kind, "WAFER Y") != 0) return AbortStage(title, "WAFER Y 이동 실패");

                if (!CheckStagePlaneInterlock(machine, false, out reason)) return AbortStage(title, "WAFER T 전 " + reason);
                if (await StepMoveKindAsync(kind, "WAFER T") != 0) return AbortStage(title, "WAFER T 이동 실패");

                if (!IsNeedleRetracted(out reason)) return AbortStage(title, "NEEDLE X 전 " + reason);
                if (await StepMoveKindAsync(kind, "NEEDLE X") != 0) return AbortStage(title, "NEEDLE X 이동 실패");

                // 7) VISION X — 레티클 실린더 Clear + Wafer Y 경로 클리어 완료
                if (!IsReticleClear(machine, out reason)) return AbortStage(title, "VISION X 전 레티클 " + reason);
                if (!IsAxisSettled(_InputStageUnit.StageY)) return AbortStage(title, "VISION X 전 WAFER Y 경로클리어 미완료");
                if (await StepMoveKindAsync(kind, "VISION X") != 0) return AbortStage(title, "VISION X 이동 실패");

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
                
                items.Add(ParameterGridItem.Micron("SAFETY RADIUS", ParameterGridScope.Setup, () => unit.Setup.SafetyRadius, v => unit.Setup.SafetyRadius = Math.Max(0.0, v)));
                items.Add(ParameterGridItem.Int("BARCODE READ TIMEOUT", "ms", ParameterGridScope.Setup, () => unit.Setup.BarcodeReadTimeoutMs, v => unit.Setup.BarcodeReadTimeoutMs = Math.Max(0, v)));
                items.Add(ParameterGridItem.Int("ALIGN ITERATIONS", "count", ParameterGridScope.Config, () => unit.Config.MaxAlignIterations, v => unit.Config.MaxAlignIterations = Math.Max(1, v)));
                items.Add(ParameterGridItem.Double("ALIGN THRESHOLD", "deg", ParameterGridScope.Config, () => unit.Config.AlignConvergenceThresholdDeg, v => unit.Config.AlignConvergenceThresholdDeg = Math.Max(0.0, v)));
                items.Add(ParameterGridItem.Micron("PICK UP EJECT PIN OFFSET", ParameterGridScope.Config, () => unit.Config.PickUpEjectPinOffset, v => unit.Config.PickUpEjectPinOffset = v));
                items.Add(ParameterGridItem.Double("PICK UP EJECT PIN SPEED", "mm/s", ParameterGridScope.Config, () => unit.Config.PickUpEjectPinSpeed, v => unit.Config.PickUpEjectPinSpeed = Math.Max(0.0, v)));
                items.Add(ParameterGridItem.Double("PICK UP EJECT PIN ACC", "mm/s2", ParameterGridScope.Config, () => unit.Config.PickUpEjectPinAcc, v => unit.Config.PickUpEjectPinAcc = Math.Max(0.0, v)));
                items.Add(ParameterGridItem.Double("PICK UP EJECT PIN DEC", "mm/s2", ParameterGridScope.Config, () => unit.Config.PickUpEjectPinDec, v => unit.Config.PickUpEjectPinDec = Math.Max(0.0, v)));
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

        private static readonly StagePositionKind[] OptionKindOrder =
        {
            StagePositionKind.Avoid,
            StagePositionKind.Load,
            StagePositionKind.Unload,
            StagePositionKind.Ready,
            StagePositionKind.Process,
            StagePositionKind.Reticle
        };

        private static void AddStagePositions(List<ParameterGridItem> items, InputStageUnit unit)
        {
            foreach (StagePositionKind kind in OptionKindOrder)
            {
                string groupKey = "POS_" + kind;
                items.Add(ParameterGridItem.Header(GetPositionLabel(kind), groupKey));

                foreach (StageTeachingPosition position in TeachingPositions)
                {
                    if (position.Kind != kind)
                        continue;

                    StageTeachingPosition captured = position;
                    ParameterGridItem item = ParameterGridItem.Micron(captured.AxisLabel, ParameterGridScope.Recipe,
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
                    string detail = string.IsNullOrEmpty(_lastAbortReason) ? "" : Environment.NewLine + "사유: " + _lastAbortReason;
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

                int result = await MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.ProcessPosition, false);
                if (result != 0)
                    return result;

                await Task.Delay(100).ContinueWith(_ => { });
                return await MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.LoadPosition, false);
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
                    "Origin X : " + FormatUm(_InputStageUnit.OriginX) + Environment.NewLine +
                    "Origin Y : " + FormatUm(_InputStageUnit.OriginY) + Environment.NewLine +
                    "Pitch X  : " + FormatUm(_InputStageUnit.PitchX) + Environment.NewLine +
                    "Pitch Y  : " + FormatUm(_InputStageUnit.PitchY) + Environment.NewLine +
                    "Align X  : " + FormatUm(_InputStageUnit.WaferAlignOffsetX) + Environment.NewLine +
                    "Align Y  : " + FormatUm(_InputStageUnit.WaferAlignOffsetY) + Environment.NewLine +
                    "Needle Vacuum : " + OnOff(_InputStageUnit.NeedleVacuum != null && _InputStageUnit.NeedleVacuum.IsOn);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static string FormatUm(double value)
        {
            try
            {
                return (value * 1000.0).ToString("0.###", CultureInfo.InvariantCulture) + " um";
            }
            catch
            {
                return "0 um";
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
