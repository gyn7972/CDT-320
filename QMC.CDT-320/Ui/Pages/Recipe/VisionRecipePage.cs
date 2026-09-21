using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Equipment.Vision;
using QMC.CDT_320.Ui.Localization;
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
    /// <summary>Vision 레시피에서 VisionUnit(Front/Rear Side Vision Y축 · 레티클/니들 I/O)을 조작하는 화면입니다.</summary>
    public partial class VisionRecipePage : PageBase
    {
        private readonly string _titleI18n;
        private readonly Timer _refreshTimer = new Timer();
        private IDisposable bottomVisionPreview;
        private IDisposable sideVisionPreview;
        private VisionUnit _visionUnit;
        private OutputStageUnit _outputStageUnit;

        public VisionRecipePage() : this("recipe.inputVision")
        {
        }

        public VisionRecipePage(string titleI18n)
        {
            try
            {
                _titleI18n = titleI18n;
                InitializeComponent();
                InitializeLanguageBindings();
                if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                    return;

                InstallVisionPreview();
                ApplyTitle();
                ApplyRuntimeLayout();
                ConfigureRuntimeBehavior();
                ConfigureManualActions();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(ex.Message, Lang.T("visionUi.visionRecipePage.message.text"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("visionUi.visionRecipePage.message.state2"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                DisposeVisionPreview();
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

        private void InstallVisionPreview()
        {
            Lang.BindKey(tabBottom, "visionUi.visionRecipePage.tabBottom.text");
            Lang.BindKey(tabSide, "visionUi.visionRecipePage.tabSide.text");
            bottomVisionPreview = RecipeVisionPreview.ShowSingle(tabBottom, "BOTTOM VISION", VisionViewerPorts.BottomInspection);
            sideVisionPreview = RecipeVisionPreview.ShowVertical(
                tabSide,
                new RecipeVisionPreviewTile("FRONT SIDE VISION", VisionViewerPorts.FrontSideVision),
                new RecipeVisionPreviewTile("REAR SIDE VISION", VisionViewerPorts.RearSideVision));
        }

        private void DisposeVisionPreview()
        {
            try { if (bottomVisionPreview != null) bottomVisionPreview.Dispose(); } catch { }
            try { if (sideVisionPreview != null) sideVisionPreview.Dispose(); } catch { }
            bottomVisionPreview = null;
            sideVisionPreview = null;
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
                optionParameterGrid.BringToFront();
                waitParameterGrid.BringToFront();
                ioCylinderPanel.BringToFront();
                jogCommonLayout.BringToFront();
                jogSpeedControl.BringToFront();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("visionUi.visionRecipePage.message.state3"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                waitParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
                BindParameterGridMenus();

                jogAxisMoveControl.SpeedControl = jogSpeedControl;
                jogAxisMoveControl.LayoutMode = JogAxisMoveLayoutMode.AxisColumns;
                // Front Y / Rear Y 두 축을 한 행에 좌우로 나란히(2열) 배치하고, 두 열 사이 간격을 넓게 둔다.
                jogAxisMoveControl.AxisColumnsPerRow = 2;
                jogAxisMoveControl.AxisColumnGap = 60;
                jogAxisMoveControl.ShowCurrentSpeedMode = true;
                jogAxisMoveControl.ButtonAreaMinHeight = 420;
                jogAxisMoveControl.ButtonAreaMaxHeight = 460;
                jogAxisMoveControl.ButtonAreaMinWidth = 170;
                jogAxisMoveControl.ButtonAreaMaxWidth = 320;
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("visionUi.visionRecipePage.message.state4"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void ResolveUnit()
        {
            try
            {
                var machine = FindMachine();
                _visionUnit = machine != null ? machine.VisionUnit : null;
                _outputStageUnit = machine != null ? machine.OutputStageUnit : null;
                if (_visionUnit != null && _visionUnit.Recipe != null)
                    _visionUnit.Recipe.EnsurePositionObjects();
                SetEnabledState(_visionUnit != null);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("visionUi.visionRecipePage.message.state5"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void BindParameterGrids()
        {
            try
            {
                if (_visionUnit == null)
                    return;

                _visionUnit.Recipe.EnsurePositionObjects();
                var unit = _visionUnit;
                var items = new List<ParameterGridItem>();

                // Recipe 위치 — 위치 종류별 접이식 그룹 (멤버 = FRONT Y / REAR Y)
                AddKindGroup(items, "AVOID POSITION", "Avoid");
                AddKindGroup(items, "PROCESS POSITION (0°)", "Process0");
                AddKindGroup(items, "PROCESS POSITION (90°)", "Process90");

                items.Add(ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => unit.Setup.IsSimulationMode, v => unit.Setup.IsSimulationMode = v));
                items.Add(ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v));
                items.Add(ParameterGridItem.Selection<PickerInspectionPipelineMode>(
                    "수동/단계 검사 모드",
                    "mode",
                    ParameterGridScope.Config,
                    () => unit.Config.PickerInspectionMode,
                    v => unit.Config.PickerInspectionMode = v));

                optionParameterGrid.SetItems(items);

                // WAIT TIME — Vision Camera Calibration 촬영/응답 타임아웃
                waitParameterGrid.AutoFitParentGroupHeight = true;   // WAIT 그룹 높이를 내용에 맞춰 자동 조정 (스크롤 없이 전 항목 표시)
                waitParameterGrid.SetItems(new[]
                {
                    ParameterGridItem.Int("CAPTURE TIMEOUT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.CaptureTimeoutMs, v => unit.Recipe.CaptureTimeoutMs = v)
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "VISION", "BindParameterGrids failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("visionUi.visionRecipePage.message.state6"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        // 한 위치 종류(kind)를 헤더로 묶고, Front/Rear Side Vision Y축을 멤버로 추가
        private void AddKindGroup(List<ParameterGridItem> items, string kindLabel, string kind)
        {
            string groupKey = "K_" + kind.ToUpperInvariant();
            items.Add(ParameterGridItem.Header(kindLabel, groupKey));

            items.Add(VisionMember("FRONT Y", groupKey, kindLabel, kind, VisionSide.Front));
            items.Add(VisionMember("REAR Y", groupKey, kindLabel, kind, VisionSide.Rear));
        }

        // LoadRecipe/LoadSettings가 Recipe 객체를 교체하므로 positions를 지역 캡처하지 않고 매 호출 시 라이브 객체를 따라간다.
        private VisionAxisPositions ResolveLiveVisionPositions(VisionSide side)
        {
            return side == VisionSide.Front ? _visionUnit.Recipe.FrontSideVision : _visionUnit.Recipe.RearSideVision;
        }

        private ParameterGridItem VisionMember(string axisLabel, string groupKey, string kindLabel, string kind, VisionSide side)
        {
            BaseAxis axis = side == VisionSide.Front ? _visionUnit.FrontSideVisionY : _visionUnit.RearSideVisionY;

            Func<double> getter;
            Action<double> setter;
            switch (kind)
            {
                // Avoid 위치 레시피 연결
                case "Avoid": getter = () => ResolveLiveVisionPositions(side).AvoidPosition; setter = v => ResolveLiveVisionPositions(side).AvoidPosition = v; break;
                // Process 위치(0도) 레시피 연결
                case "Process0": getter = () => ResolveLiveVisionPositions(side).Process0Position; setter = v => ResolveLiveVisionPositions(side).Process0Position = v; break;
                // Process 위치(90도) 레시피 연결
                case "Process90": getter = () => ResolveLiveVisionPositions(side).Process90Position; setter = v => ResolveLiveVisionPositions(side).Process90Position = v; break;
                default: getter = () => 0.0; setter = v => { }; break;
            }

            var item = AxisDouble(axisLabel, ParameterGridScope.Recipe, axis, getter, setter);
            item.Key = axisLabel + " " + kindLabel;   // 이동/티칭 조회는 전체 이름(Key)으로 파싱
            item.GroupKey = groupKey;
            item.SupportsTeaching = true;             // 행에 MOVE/TEACH 버튼 표시(티칭 포지션)
            return item;
        }

        // ===================== 매뉴얼 액션 (Front/Rear 통합 단일 버튼) =====================
        private void ConfigureManualActions()
        {
            try
            {
                // 공용 MANUAL ACTION 판넬에 비전 이동 버튼 등록 (2열, 행 수 자동)
                manualActionPanel.ColumnCount = 2;
                manualActionPanel.SetItems(new[]
                {
                    ManualActionItem.Create("AVOID POSITION", () => ConfirmAndRunAsync("AVOID POSITION", MoveBothAvoidAsync, _visionUnit.FrontSideVisionY, _visionUnit.RearSideVisionY)),
                    ManualActionItem.Create("PROCESS POSITION (0°)", () => ConfirmAndRunAsync("PROCESS POSITION (0°)", MoveBothProcess0Async, _visionUnit.FrontSideVisionY, _visionUnit.RearSideVisionY)),
                    ManualActionItem.Create("PROCESS POSITION (90°)", () => ConfirmAndRunAsync("PROCESS POSITION (90°)", MoveBothProcess90Async, _visionUnit.FrontSideVisionY, _visionUnit.RearSideVisionY)),
                    ManualActionItem.Create("RETICLE 공정 위치", () => RunReticleActionAsync("RETICLE 공정 위치", true)),
                    ManualActionItem.Create("RETICLE 대기 위치", () => RunReticleActionAsync("RETICLE 대기 위치", false))
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "VISION-STAGE", "ConfigureManualActions failed: " + ex.Message);
            }
            finally
            {
            }
        }
        private async void btnAvoidPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("AVOID POSITION", MoveBothAvoidAsync, _visionUnit.FrontSideVisionY, _visionUnit.RearSideVisionY);
        }

        private async void btnProcessPosition0_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("PROCESS POSITION (0°)", MoveBothProcess0Async, _visionUnit.FrontSideVisionY, _visionUnit.RearSideVisionY);
        }

        private async void btnProcessPosition90_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("PROCESS POSITION (90°)", MoveBothProcess90Async, _visionUnit.FrontSideVisionY, _visionUnit.RearSideVisionY);
        }

        // Front/Rear 양측을 각자 Avoid 위치로 이동 — 현재 UI 속도 모드를 축별로 적용한다.
        private Task<int> MoveBothAvoidAsync()
        {
            if (_visionUnit == null)
                return Task.FromResult(-1);

            return MoveBothVisionAxesWithSelectedSpeedAsync(
                _visionUnit.Recipe.FrontSideVision.AvoidPosition,
                _visionUnit.Recipe.RearSideVision.AvoidPosition);
        }

        // Front/Rear 양측을 각자 Process 위치(0도)로 이동
        private async Task<int> MoveBothProcess0Async()
        {
            if (_visionUnit == null)
                return -1;

            return await MoveBothVisionAxesWithSelectedSpeedAsync(
                _visionUnit.Recipe.FrontSideVision.Process0Position,
                _visionUnit.Recipe.RearSideVision.Process0Position);
        }

        // Front/Rear 양측을 각자 Process 위치(90도)로 이동
        private async Task<int> MoveBothProcess90Async()
        {
            if (_visionUnit == null)
                return -1;

            return await MoveBothVisionAxesWithSelectedSpeedAsync(
                _visionUnit.Recipe.FrontSideVision.Process90Position,
                _visionUnit.Recipe.RearSideVision.Process90Position);
        }

        // 기존 양축 동시 이동(Task.WhenAll)은 유지하고, Current 속도는 각 축 Config 기준으로 따로 계산한다.
        private async Task<int> MoveBothVisionAxesWithSelectedSpeedAsync(double frontTarget, double rearTarget)
        {
            JogSpeedType speedType = jogAxisMoveControl.SelectedSpeedType;
            double frontSpeed = jogAxisMoveControl.GetSelectedSpeed(_visionUnit.FrontSideVisionY);
            double rearSpeed = jogAxisMoveControl.GetSelectedSpeed(_visionUnit.RearSideVisionY);

            int[] results = await Task.WhenAll(
                _visionUnit.MoveVisionAxis(VisionAxis.FrontSideVisionY, frontTarget, speedType, frontSpeed),
                _visionUnit.MoveVisionAxis(VisionAxis.RearSideVisionY, rearTarget, speedType, rearSpeed));

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i] != 0)
                    return results[i];
            }

            return 0;
        }

        // ===== RETICLE 공정/대기 위치 실린더 일괄 동작 =====
        // 공정 위치: Reticle 업 → 300ms → Rear Slide 전진 → 300ms
        // 대기 위치: Rear Slide 후진 → 300ms → Reticle 다운 → 300ms
        // 인터락: Front/Rear 픽커 Z 전 축이 Avoid 위치여야 하며, 하나라도 내려와 있으면 알람 후 차단한다.
        private async Task RunReticleActionAsync(string actionName, bool toProcessPosition)
        {
            string reason;
            if (!CheckAllPickerZAvoidForReticle(out reason))
            {
                string message = actionName + " 이동 불가: " + reason;
                QMC.Common.Alarms.AlarmManager.Raise(QMC.Common.Alarms.AlarmSeverity.Error, "VS-RETICLE-PICKER-Z", "UI", message);
                QMC.Common.MessageDialog.Show(this, message, Lang.T("visionUi.visionRecipePage.message.text"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await ConfirmAndRunAsync(
                actionName,
                toProcessPosition ? (Func<Task<int>>)MoveReticleToProcessPositionAsync : MoveReticleToStandbyPositionAsync);
        }

        private async Task<int> MoveReticleToProcessPositionAsync()
        {
            if (_visionUnit == null)
                return -1;

            // 확인창 이후 상태가 변했을 수 있으므로 실행 직전 픽커 Z Avoid를 재확인한다.
            string reason;
            if (!CheckAllPickerZAvoidForReticle(out reason))
            {
                QMC.Common.Alarms.AlarmManager.Raise(QMC.Common.Alarms.AlarmSeverity.Error, "VS-RETICLE-PICKER-Z", "UI",
                    "RETICLE 공정 위치 이동 불가: " + reason);
                return -1;
            }

            int result = await _visionUnit.SetReticleLiftUpAsync(true, System.Threading.CancellationToken.None);
            if (result != 0)
                return result;
            await Task.Delay(300);

            result = await _visionUnit.SetReticleRearSideForwardAsync(true, System.Threading.CancellationToken.None);
            if (result != 0)
                return result;
            await Task.Delay(300);

            return 0;
        }

        private async Task<int> MoveReticleToStandbyPositionAsync()
        {
            if (_visionUnit == null)
                return -1;

            // 확인창 이후 상태가 변했을 수 있으므로 실행 직전 픽커 Z Avoid를 재확인한다.
            string reason;
            if (!CheckAllPickerZAvoidForReticle(out reason))
            {
                QMC.Common.Alarms.AlarmManager.Raise(QMC.Common.Alarms.AlarmSeverity.Error, "VS-RETICLE-PICKER-Z", "UI",
                    "RETICLE 대기 위치 이동 불가: " + reason);
                return -1;
            }

            int result = await _visionUnit.SetReticleRearSideForwardAsync(false, System.Threading.CancellationToken.None);
            if (result != 0)
                return result;
            await Task.Delay(300);

            result = await _visionUnit.SetReticleLiftUpAsync(false, System.Threading.CancellationToken.None);
            if (result != 0)
                return result;
            await Task.Delay(300);

            return 0;
        }

        // Front/Rear 픽커 Z 전 축이 Avoid(상승) 위치인지 확인한다. 사유는 한국어로 반환한다.
        private bool CheckAllPickerZAvoidForReticle(out string reason)
        {
            reason = string.Empty;

            var machine = FindMachine();
            var frontPicker = machine != null ? machine.PickerFrontUnit : null;
            var rearPicker = machine != null ? machine.PickerRearUnit : null;
            if (frontPicker == null || rearPicker == null)
            {
                reason = "픽커 유닛을 찾을 수 없습니다.";
                return false;
            }

            string frontBlock = frontPicker.GetPickerZClearBlockReason();
            if (frontBlock != null)
            {
                reason = "Front Picker Z " + frontBlock;
                return false;
            }

            string rearBlock = rearPicker.GetPickerZClearBlockReason();
            if (rearBlock != null)
            {
                reason = "Rear Picker Z " + rearBlock;
                return false;
            }

            return true;
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
                EventLogger.Write(EventKind.Alarm, "UI", "VISION", "BindParameterGridMenus failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("visionUi.visionRecipePage.message.state7"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                VisionAxis axis;
                string positionName;
                if (!TryGetSelectedTeachingPosition(out axis, out positionName))
                    return;

                BaseAxis motionAxis = _visionUnit.ResolveVisionAxis(axis);
                await ConfirmAndRunMoveToPositionAsync(
                    e.Item.Key,
                    () => _visionUnit.MoveVisionAxisToTeachingPosition(
                        axis,
                        positionName,
                        jogAxisMoveControl.SelectedSpeedType,
                        jogAxisMoveControl.GetSelectedSpeed(motionAxis)),
                    motionAxis);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "VISION", "Move button failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("visionUi.visionRecipePage.message.state8"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                VisionAxis axis;
                string positionName;
                if (!TryGetSelectedTeachingPosition(out axis, out positionName))
                    return;

                if (!ConfirmTeachPosition("Vision Teach", e.Item.Key))
                    return;

                _visionUnit.TeachVisionAxisPosition(axis, positionName);
                SaveCurrentRecipeData();
                RefreshView();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "VISION", "Teach button failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("visionUi.visionRecipePage.message.state9"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
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

                EventLogger.Write(EventKind.Event, "UI", "VISION", name + " teach canceled.");
                return false;
            }
        }

        private bool TryGetSelectedTeachingPosition(out VisionAxis axis, out string positionName)
        {
            axis = VisionAxis.FrontSideVisionY;
            positionName = string.Empty;

            var item = optionParameterGrid.SelectedItem;
            if (item == null || item.IsGroupHeader)
                return false;

            string key = item.Key ?? string.Empty;
            bool isFront = key.StartsWith("FRONT Y ", StringComparison.OrdinalIgnoreCase);
            bool isRear = key.StartsWith("REAR Y ", StringComparison.OrdinalIgnoreCase);
            if (!isFront && !isRear)
                return false;

            axis = isFront ? VisionAxis.FrontSideVisionY : VisionAxis.RearSideVisionY;
            // 위치 종류는 그룹 키(K_AVOID / K_PROCESS0 / K_PROCESS90)로 식별한다.
            positionName = ResolvePositionName(item.GroupKey);
            return !string.IsNullOrEmpty(positionName);
        }

        private static string ResolvePositionName(string groupKey)
        {
            if (string.Equals(groupKey, "K_AVOID", StringComparison.OrdinalIgnoreCase)) return "AvoidPosition";
            if (string.Equals(groupKey, "K_PROCESS0", StringComparison.OrdinalIgnoreCase)) return "Process0Position";
            if (string.Equals(groupKey, "K_PROCESS90", StringComparison.OrdinalIgnoreCase)) return "Process90Position";
            return string.Empty;
        }

        private void BindIoPanel()
        {
            try
            {
                if (_visionUnit == null)
                    return;

                var unit = _visionUnit;
                var outputStageUnit = _outputStageUnit;
                var items = new List<IoCylinderItem>
                {
                    // ===== SET: RETICLE LIFT (Up/Down 체크 센서 + Up/Down 출력 통합 실린더) =====
                    IoCylinderItem.Input("RETICLE UP", () => IsOn(unit.ReticleUpSensor)),
                    IoCylinderItem.Input("RETICLE DOWN", () => IsOn(unit.ReticleDownSensor)),
                    IoCylinderItem.Cylinder("RETICLE LIFT", unit.ReticleLift, "UP", "DOWN"),

                    // ===== SET: RETICLE FRONT SLIDE (Fw/Bw 체크 센서 + Fw/Bw 출력 통합 실린더) =====
                    IoCylinderItem.Input("RETICLE FRONT FW", () => IsOn(unit.ReticleFrontSideFwSensor)),
                    IoCylinderItem.Input("RETICLE FRONT BW", () => IsOn(unit.ReticleFrontSideBwSensor)),
                    IoCylinderItem.Cylinder("RETICLE FRONT SLIDE", unit.ReticleFrontSideSlide, "FW", "BW"),

                    // ===== SET: RETICLE REAR SLIDE (Fw/Bw 체크 센서 + Fw/Bw 출력 통합 실린더) =====
                    IoCylinderItem.Input("RETICLE REAR FW", () => IsOn(unit.ReticleRearSideFwSensor)),
                    IoCylinderItem.Input("RETICLE REAR BW", () => IsOn(unit.ReticleRearSideBwSensor)),
                    IoCylinderItem.Cylinder("RETICLE REAR SLIDE", unit.ReticleRearSideSlide, "FW", "BW")
                };

                if (outputStageUnit != null)
                {
                    // 물리 출력의 소유 Unit은 유지하고, 수동 조작 위치만 Vision 화면으로 옮긴다.
                    items.Add(IoCylinderItem.Output("BTM VISION BLOW", () => IsOn(outputStageUnit.BottomVisionBlowOnOut),
                        on => GuardedPairOut("BottomVisionBlow", null,
                            outputStageUnit.BottomVisionBlowOnOut, outputStageUnit.BottomVisionBlowOffOut, on), "ON", "OFF"));
                }

                ioCylinderPanel.ColumnCount = 2;   // 2열 배치 (Front Head 기준)
                ioCylinderPanel.SetItems(items);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "VISION", "BindIoPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("visionUi.visionRecipePage.message.state10"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private static bool IsOn(QMC.Common.IO.BaseDigitalInput input)
        {
            try { input?.UpdateStatus(); } catch { }
            return input != null && input.IsOn;
        }

        private static bool IsOn(QMC.Common.IO.BaseDigitalOutput output)
        {
            try { output?.UpdateStatus(); } catch { }
            return output != null && output.IsOn;
        }

        private static void WriteOut(QMC.Common.IO.BaseDigitalOutput output, bool on)
        {
            if (output == null) return;
            if (on) output.On(); else output.Off();
        }

        private static Task<int> WritePairOut(QMC.Common.IO.BaseDigitalOutput forward,
            QMC.Common.IO.BaseDigitalOutput backward, bool forwardOn)
        {
            try
            {
                WriteOut(forward, forwardOn);
                WriteOut(backward, !forwardOn);
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

        // 기존 Output Stage 화면과 동일하게 모션가드 검증 후 DO pair를 조작한다.
        private Task<int> GuardedPairOut(string movingName, QMC.Common.IO.BaseCylinder cylinder,
            QMC.Common.IO.BaseDigitalOutput forward, QMC.Common.IO.BaseDigitalOutput backward, bool forwardOn)
        {
            try
            {
                string reason;
                if (cylinder != null)
                {
                    if (!MotionGuardRuntime.VerifyCylinderMove(cylinder, forwardOn, out reason))
                        return Task.FromResult(-1);
                }
                else
                {
                    if (!VerifyNamedCylinderMove(movingName, forwardOn, out reason))
                    {
                        QMC.Common.Alarms.AlarmManager.Raise(QMC.Common.Alarms.AlarmSeverity.Error,
                            "OUTPUT-STAGE", "UI", movingName + " output blocked by interlock: " + reason);
                        return Task.FromResult(-1);
                    }
                }

                return WritePairOut(forward, backward, forwardOn);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        // BaseCylinder가 없는 바텀비전 블로우를 이름 기반 레지스트리 가드로 검사한다.
        private bool VerifyNamedCylinderMove(string movingName, bool forwardOn, out string reason)
        {
            var context = MotionGuardRuntime.ContextProvider != null ? MotionGuardRuntime.ContextProvider() : null;
            var request = new MotionGuardRuleContext(movingName, movingName, forwardOn ? 1.0 : 0.0,
                MotionGuardMoveKind.CylinderMove, string.Empty, null, context);
            return MotionGuardRuleRegistry.Verify(request, out reason);
        }

        private void BindJogPanel()
        {
            try
            {
                if (_visionUnit == null)
                    return;

                var unit = _visionUnit;
                // 축 순서: Front Y, Rear Y
                var items = new List<JogAxisItem>
                {
                    BuildJogAxis("FRONT Y", unit.FrontSideVisionY, "Y+", "Y-", JogAxisControlKind.Vertical),
                    BuildJogAxis("REAR Y", unit.RearSideVisionY, "Y+", "Y-", JogAxisControlKind.Vertical)
                };

                jogPositionListControl.SetItems(items);
                jogAxisMoveControl.SetItems(items);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "VISION", "BindJogPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("visionUi.visionRecipePage.message.state11"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private JogAxisItem BuildJogAxis(string name, BaseAxis axis, string plus, string minus, JogAxisControlKind kind)
        {
            JogAxisItem item = JogAxisItem.Single(name, axis, AxisUnitConverter.DisplayUnitFor(axis), 1.0, plus, minus).WithControlKind(kind);
            item.StepMoveAsync = (it, direction, speedType, customSpeed, axisStepDistance) =>
                _visionUnit.JogStepAsync(axis, direction, speedType, customSpeed, axisStepDistance);
            item.ContinuousMoveAsync = (it, direction, speedType, customSpeed) =>
                _visionUnit.JogContinuousAsync(axis, direction, speedType, customSpeed);
            item.StopAsync = it => _visionUnit.StopJogAsync(axis);
            return item;
        }

        // 이동 대상 축의 HOME END(IsHomeDone) 확인 — 미완료면 로그+메시지로 차단.
        private bool EnsureVisionAxesHomeDone(string actionName, BaseAxis[] targetAxes)
        {
            if (targetAxes == null)
                return true;
            foreach (BaseAxis ax in targetAxes)
            {
                if (ax != null && !ax.IsHomeDone)
                {
                    string homeMsg = actionName + " 불가: " + ax.Name + " 축 HOME END(원점복귀)가 완료되지 않았습니다.";
                    QMC.Common.Alarms.AlarmManager.Raise(QMC.Common.Alarms.AlarmSeverity.Warning, "VISION", "UI", homeMsg);
                    QMC.Common.MessageDialog.Show(this, homeMsg, Lang.T("visionUi.visionRecipePage.message.text"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            return true;
        }

        private async Task ConfirmAndRunAsync(string actionName, Func<Task<int>> action, params BaseAxis[] targetAxes)
        {
            await ConfirmAndRunInternalAsync(actionName, action, false, targetAxes);
        }

        private async Task ConfirmAndRunMoveToPositionAsync(string actionName, Func<Task<int>> action, params BaseAxis[] targetAxes)
        {
            await ConfirmAndRunInternalAsync(actionName, action, true, targetAxes);
        }

        private async Task ConfirmAndRunInternalAsync(string actionName, Func<Task<int>> action, bool selectMoveSpeed, params BaseAxis[] targetAxes)
        {
            try
            {
                if (_visionUnit == null || action == null)
                    return;

                if (ManualMoveGuard.BlockIfNotReady(this, "Vision"))
                    return;

                // 이동 대상 축의 HOME END(IsHomeDone) 미완료면 차단.
                if (!EnsureVisionAxesHomeDone(actionName, targetAxes))
                    return;

                if (selectMoveSpeed)
                {
                    JogSpeedType speedType;
                    if (!ManualMoveGuard.ConfirmMoveSpeed(this, "Vision", actionName, out speedType))
                        return;

                    jogAxisMoveControl.SetSelectedSpeedType(speedType);
                }
                else
                {
                    DialogResult confirm = QMC.Common.MessageDialog.Show(this, Lang.Format("visionUi.visionRecipePage.message.state12", (object)(actionName)), Lang.T("visionUi.visionRecipePage.message.text"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (confirm != DialogResult.Yes)
                        return;
                }

                Cursor = Cursors.WaitCursor;
                int result;
                using (MotionGuardRuntime.BeginManualSequenceProcessMove("VisionRecipePage." + actionName))
                {
                    result = await action();
                }
                EventLogger.Write(EventKind.Event, "UI", "VISION", actionName + " result=" + result);
                if (result != 0)
                {
                    string msg = _visionUnit != null ? _visionUnit.LastVisionMoveFailureMessage : null;
                    string detail = string.IsNullOrEmpty(msg) ? "" : Environment.NewLine + "사유 : " + msg;
                    QMC.Common.MessageDialog.Show(this, Lang.Format("visionUi.visionRecipePage.message.state13", (object)(actionName), (object)(detail)), Lang.T("visionUi.visionRecipePage.message.text"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, Lang.T("visionUi.visionRecipePage.message.state14"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void SaveCurrentRecipeData()
        {
            var host = FindHostForm();
            if (host == null)
                throw new InvalidOperationException("Main 화면을 찾을 수 없어 설정을 저장할 수 없습니다.");
            host.QueueRecipeEditorSave(_visionUnit, true);
        }

        private void SaveCurrentSettingsData()
        {
            var host = FindHostForm();
            if (host == null)
                throw new InvalidOperationException("Main 화면을 찾을 수 없어 설정을 저장할 수 없습니다.");
            host.QueueRecipeEditorSave(_visionUnit, false);
        }

        private void RefreshView()
        {
            try
            {
                if (_visionUnit == null)
                    return;

                var unit = _visionUnit;
                optionParameterGrid.RefreshValues();
                waitParameterGrid.RefreshValues();
                ioCylinderPanel.RefreshStates();
                jogPositionListControl.RefreshState();

                Lang.BindFormat(lblVisionInfo, "visionUi.visionRecipePage.lblVisionInfo.text", (object)(Environment.NewLine), (object)(FormatAxis(unit.FrontSideVisionY.ActualPosition, unit.FrontSideVisionY)), (object)(Environment.NewLine), (object)(FormatAxis(unit.RearSideVisionY.ActualPosition, unit.RearSideVisionY)), (object)(Environment.NewLine), (object)(OnOff(unit.IsFrontSideVisionYInAvoidPosition())), (object)(Environment.NewLine), (object)(OnOff(unit.IsRearSideVisionYInAvoidPosition())), (object)(Environment.NewLine), (object)(OnOff(unit.IsFrontSideVisionYInProcessPosition())), (object)(Environment.NewLine), (object)(OnOff(unit.IsRearSideVisionYInProcessPosition())));
            }
            catch
            {
            }
            finally
            {
            }
        }

        private ParameterGridItem AxisDouble(string displayName, ParameterGridScope scope, BaseAxis axis, Func<double> getter, Action<double> setter)
        {
            ParameterGridItem item = ParameterGridItem.Double(
                displayName,
                AxisUnitConverter.DisplayUnitFor(axis),
                scope,
                () => AxisUnitConverter.ToDisplay(getter(), axis),
                v => setter(AxisUnitConverter.FromDisplay(v, axis)));
            item.UnitGetter = () => AxisUnitConverter.DisplayUnitFor(axis);
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
        // Keep Designer serialization declarative; register display resources after controls exist.
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(this.lblHeader, "visionUi.visionRecipePage.lblHeader.text");
            Lang.BindKey(this.grpVision, "visionUi.visionCameraScaleDialog.message.state2");
            Lang.BindKey(this.tabBottom, "visionUi.visionRecipePage.tabBottom.text");
            Lang.BindKey(this.lblVisionInfo, "visionUi.visionRecipePage.lblVisionInfo.state2");
            Lang.BindKey(this.tabSide, "visionUi.visionRecipePage.tabSide.text");
            Lang.BindKey(this.lblVisionInfo2, "visionUi.visionRecipePage.lblVisionInfo2.text");
            Lang.BindKey(this.lblVisionInfo3, "visionUi.visionRecipePage.lblVisionInfo3.text");
            Lang.BindKey(this.grpManual, "visionUi.visionRecipePage.grpManual.text");
            Lang.BindKey(this.grpOptions, "visionUi.visionRecipePage.grpOptions.text");
            Lang.BindKey(this.grpWait, "visionUi.visionRecipePage.grpWait.text");
            Lang.BindKey(this.grpIo, "visionUi.visionRecipePage.grpIo.text");
            Lang.BindKey(this.grpJog, "visionUi.visionRecipePage.grpJog.text");
            Lang.BindKey(this.grpSpeed, "visionUi.visionRecipePage.grpSpeed.text");
            Lang.BindKey(this.lblLoadingPositionKey, "visionUi.visionRecipePage.lblLoadingPositionKey.text");
            Lang.BindKey(this.lblLoadingPositionValue, "visionUi.visionRecipePage.lblLoadingPositionValue.text");
            Lang.BindKey(this.lblCenterPositionKey, "visionUi.visionRecipePage.lblCenterPositionKey.text");
            Lang.BindKey(this.lblCenterPositionValue, "visionUi.visionRecipePage.lblLoadingPositionValue.text");
            Lang.BindKey(this.lblNeedlePositionKey, "visionUi.visionRecipePage.lblNeedlePositionKey.text");
            Lang.BindKey(this.lblNeedlePositionValue, "visionUi.visionRecipePage.lblLoadingPositionValue.text");
            Lang.BindKey(this.lblTestBedPositionKey, "visionUi.visionRecipePage.lblTestBedPositionKey.text");
            Lang.BindKey(this.lblTestBedPositionValue, "visionUi.visionRecipePage.lblLoadingPositionValue.text");
            Lang.BindKey(this.lblBarcodePositionKey, "visionUi.visionRecipePage.lblBarcodePositionKey.text");
            Lang.BindKey(this.lblBarcodePositionValue, "visionUi.visionRecipePage.lblLoadingPositionValue.text");
            Lang.BindKey(this.lblVisionAlignKey, "visionUi.visionRecipePage.lblVisionAlignKey.text");
            Lang.BindKey(this.lblVisionAlignValue, "visionUi.visionRecipePage.lblLoadingPositionValue.text");
            Lang.BindKey(this.lblWorkRadiusKey, "visionUi.visionRecipePage.lblWorkRadiusKey.text");
            Lang.BindKey(this.lblWorkRadiusValue, "visionUi.visionRecipePage.lblLoadingPositionValue.text");
            Lang.BindKey(this.lblFirstDiePositionKey, "visionUi.visionRecipePage.lblFirstDiePositionKey.text");
            Lang.BindKey(this.lblFirstDiePositionValue, "visionUi.visionRecipePage.lblLoadingPositionValue.text");
            Lang.BindKey(this.lblNeedleMeasurePositionKey, "visionUi.visionRecipePage.lblNeedleMeasurePositionKey.text");
            Lang.BindKey(this.lblNeedleMeasurePositionValue, "visionUi.visionRecipePage.lblLoadingPositionValue.text");
            Lang.BindKey(this.lblNeedleUpWaitKey, "visionUi.visionRecipePage.lblNeedleUpWaitKey.text");
            Lang.BindKey(this.lblNeedleUpWaitValue, "visionUi.visionRecipePage.lblNeedleUpWaitValue.text");
            Lang.BindKey(this.lblVacuumOnWaitKey, "visionUi.visionRecipePage.lblVacuumOnWaitKey.text");
            Lang.BindKey(this.lblVacuumOnWaitValue, "visionUi.visionRecipePage.lblNeedleUpWaitValue.text");
            Lang.BindKey(this.lblNeedleDownWaitKey, "visionUi.visionRecipePage.lblNeedleDownWaitKey.text");
            Lang.BindKey(this.lblNeedleDownWaitValue, "visionUi.visionRecipePage.lblNeedleUpWaitValue.text");
            Lang.BindKey(this.lblVacuumOffWaitKey, "visionUi.visionRecipePage.lblVacuumOffWaitKey.text");
            Lang.BindKey(this.lblVacuumOffWaitValue, "visionUi.visionRecipePage.lblNeedleUpWaitValue.text");
            Lang.BindKey(this.lblMovingWaitKey, "visionUi.visionRecipePage.lblMovingWaitKey.text");
            Lang.BindKey(this.lblMovingWaitValue, "visionUi.visionRecipePage.lblNeedleUpWaitValue.text");
            Lang.BindKey(this.lblNeedleVacuumKey, "visionUi.visionRecipePage.lblNeedleVacuumKey.text");
            Lang.BindKey(this.lblNeedleVacuumValue, "visionUi.visionRecipePage.lblNeedleVacuumValue.text");
            Lang.BindKey(this.lblRingSensorKey, "visionUi.visionRecipePage.lblRingSensorKey.text");
            Lang.BindKey(this.lblRingSensorValue, "visionUi.visionRecipePage.lblRingSensorValue.text");
            Lang.BindKey(this.lblExpandCylinderKey, "visionUi.visionRecipePage.lblExpandCylinderKey.text");
            Lang.BindKey(this.lblExpandCylinderValue, "visionUi.visionRecipePage.lblExpandCylinderValue.text");
            Lang.BindKey(this.lblNeedleBlockKey, "visionUi.visionRecipePage.lblNeedleBlockKey.text");
            Lang.BindKey(this.lblNeedleBlockValue, "visionUi.visionRecipePage.lblExpandCylinderValue.text");
            Lang.BindKey(this.lblAxisXKey, "visionUi.visionRecipePage.lblAxisXKey.text");
            Lang.BindKey(this.lblAxisXValue, "visionUi.visionRecipePage.lblAxisXValue.text");
            Lang.BindKey(this.lblAxisYKey, "visionUi.visionRecipePage.lblAxisYKey.text");
            Lang.BindKey(this.lblAxisYValue, "visionUi.visionRecipePage.lblAxisXValue.text");
            Lang.BindKey(this.lblAxisTKey, "visionUi.visionRecipePage.lblAxisTKey.text");
            Lang.BindKey(this.lblAxisTValue, "visionUi.visionRecipePage.lblAxisTValue.text");
            Lang.BindKey(this.lblExpandZKey, "visionUi.visionRecipePage.lblExpandZKey.text");
            Lang.BindKey(this.lblExpandZValue, "visionUi.visionRecipePage.lblAxisXValue.text");
            Lang.BindKey(this.lblNeedleZKey, "visionUi.visionRecipePage.lblNeedleZKey.text");
            Lang.BindKey(this.lblNeedleZValue, "visionUi.visionRecipePage.lblAxisXValue.text");
            Lang.BindKey(this.lblNeedleBlockZKey, "visionUi.visionRecipePage.lblNeedleBlockZKey.text");
            Lang.BindKey(this.lblNeedleBlockZValue, "visionUi.visionRecipePage.lblAxisXValue.text");
            Lang.BindKey(this.lblExpandAxis, "visionUi.visionRecipePage.lblExpandAxis.text");
            Lang.BindKey(this.lblNeedleAxis, "visionUi.visionRecipePage.lblNeedleAxis.text");
            Lang.BindKey(this.lblBlockAxis, "visionUi.visionRecipePage.lblBlockAxis.text");
            Lang.BindFormat(this.lblSpeedValue, "visionUi.literal", (object)("50%"));
        }
    }
}
