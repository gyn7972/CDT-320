using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
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
    /// <summary>Output Stage 레시피에서 OutputStageUnit(Good/Ng 스테이지)을 조작하는 화면입니다.</summary>
    public partial class OutputStageRecipePage : PageBase
    {
        private const int ManualCylinderTimeoutMs = 5000;
        private readonly string _titleI18n;
        private readonly Timer _refreshTimer = new Timer();
        private OutputStageUnit _outputStageUnit;

        public OutputStageRecipePage() : this("recipe.outputStage")
        {
        }

        public OutputStageRecipePage(string titleI18n)
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
                QMC.Common.MessageDialog.Show(ex.Message, "Output Stage", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Layout", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                jogAxisMoveControl.LayoutMode = JogAxisMoveLayoutMode.OutputStagePad;
                jogAxisMoveControl.AxisColumnsPerRow = 2;
                jogAxisMoveControl.ShowCurrentSpeedMode = true;
                jogAxisMoveControl.ButtonAreaMinHeight = 420;
                jogAxisMoveControl.ButtonAreaMaxHeight = 460;
                jogAxisMoveControl.ButtonAreaMinWidth = 170;
                jogAxisMoveControl.ButtonAreaMaxWidth = 320;
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Configure", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                _outputStageUnit = machine != null ? machine.OutputStageUnit : null;
                if (_outputStageUnit != null)
                    _outputStageUnit.Recipe.EnsurePositionObjects();
                SetEnabledState(_outputStageUnit != null);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Resolve", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                if (_outputStageUnit == null)
                    return;

                _outputStageUnit.Recipe.EnsurePositionObjects();
                var unit = _outputStageUnit;
                var items = new List<ParameterGridItem>();

                // Recipe 위치 — 위치 종류별 접이식 그룹 (멤버 = GOOD Y/Z, NG Y, VISION X)
                AddKindGroup(items, "AVOID POSITION", "Avoid", true, true, true, true);
                AddKindGroup(items, "LOAD POSITION", "Load", true, true, true, false);
                AddKindGroup(items, "PROCESS POSITION", "Process", true, true, true, true);
                AddKindGroup(items, "UNLOAD POSITION", "Unload", true, true, true, false);
                AddKindGroup(items, "RETICLE POSITION", "Reticle", false, true, false, true);

                // 빈맵(원형) 형상은 BIN DIE MAP CREATE 페이지에서 레시피 맵으로 저장/관리한다.

                items.Add(ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => unit.Setup.IsSimulationMode, v => unit.Setup.IsSimulationMode = v));
                items.Add(ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v));

                optionParameterGrid.SetItems(items);

                waitParameterGrid.SetItems(new ParameterGridItem[0]);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", "BindParameterGrids failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Parameters", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        // 한 위치 종류(kind)를 헤더로 묶고, 그 종류를 가진 축들을 멤버로 추가
        private void AddKindGroup(List<ParameterGridItem> items, string kindLabel, string kind, bool goodY, bool goodZ, bool ng, bool vision)
        {
            string groupKey = "K_" + kind.ToUpperInvariant();
            ParameterGridItem header = ParameterGridItem.Header(kindLabel, groupKey);
            if (string.Equals(kind, "Reticle", StringComparison.OrdinalIgnoreCase))
                header.Description = "Vision Cal Position";
            items.Add(header);

            var recipe = _outputStageUnit.Recipe;
            if (goodY) items.Add(StageMember("GOOD Y", groupKey, kindLabel, kind, _outputStageUnit.GoodStage.StageY, () => recipe.GoodStageY));
            if (goodZ) items.Add(StageMember("GOOD Z", groupKey, kindLabel, kind, _outputStageUnit.GoodStage.StageZ, () => recipe.GoodStageZ));
            if (ng) items.Add(StageMember("NG Y", groupKey, kindLabel, kind, _outputStageUnit.NgStage.StageY, () => recipe.NGStageY));
            if (vision) items.Add(StageMember("VISION X", groupKey, kindLabel, kind, _outputStageUnit.OutputCameraX, () => recipe.VisionX));
        }

        private ParameterGridItem StageMember(string axisLabel, string groupKey, string kindLabel, string kind, BaseAxis axis, Func<StageAxisPositions> set)
        {
            Func<double> getter;
            Action<double> setter;
            switch (kind)
            {
                // Avoid 위치 레시피 연결
                case "Avoid": getter = () => set().AvoidPosition; setter = v => set().AvoidPosition = v; break;
                // Load 위치 레시피 연결
                case "Load": getter = () => set().LoadPosition; setter = v => set().LoadPosition = v; break;
                // Process 위치 레시피 연결
                case "Process": getter = () => set().ProcessPosition; setter = v => set().ProcessPosition = v; break;
                // Unload 위치 레시피 연결
                case "Unload": getter = () => set().UnloadPosition; setter = v => set().UnloadPosition = v; break;
                // Reticle 위치 레시피 연결
                case "Reticle": getter = () => set().ReticlePosition; setter = v => set().ReticlePosition = v; break;
                default: getter = () => 0.0; setter = v => { }; break;
            }

            var item = AxisDouble(axisLabel, ParameterGridScope.Recipe, axis, getter, setter);
            item.Key = axisLabel + " " + kindLabel;   // 이동/티칭 조회는 전체 이름(Key)으로 파싱
            item.GroupKey = groupKey;
            return item;
        }

        // 매뉴얼 액션 버튼(Designer 배치)의 Click 핸들러 — 좌측 GOOD / 우측 NG, 인터락 시퀀스로 이동
        private async void btnGoodAvoidPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("GOOD AVOID POSITION", () => MoveBinSequenceAsync(BinSide.Good, "Avoid"));
        }

        private async void btnNgAvoidPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("NG AVOID POSITION", () => MoveBinSequenceAsync(BinSide.Ng, "Avoid"));
        }

        private async void btnGoodLoadPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("GOOD LOAD POSITION", () => MoveBinSequenceAsync(BinSide.Good, "Load"));
        }

        private async void btnNgLoadPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("NG LOAD POSITION", () => MoveBinSequenceAsync(BinSide.Ng, "Load"));
        }

        private async void btnGoodProcessPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("GOOD PROCESS POSITION", () => MoveBinSequenceAsync(BinSide.Good, "Process"));
        }

        private async void btnNgProcessPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("NG PROCESS POSITION", () => MoveBinSequenceAsync(BinSide.Ng, "Process"));
        }

        private async void btnGoodUnloadPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("GOOD UNLOAD POSITION", () => MoveBinSequenceAsync(BinSide.Good, "Unload"));
        }

        private async void btnNgUnloadPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("NG UNLOAD POSITION", () => MoveBinSequenceAsync(BinSide.Ng, "Unload"));
        }

        private async void btnVisionAvoidPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("VISION AVOID POSITION", () => MoveVisionSequenceAsync("Avoid"));
        }

        private async void btnVisionProcessPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("VISION PROCESS POSITION", () => MoveVisionSequenceAsync("Process"));
        }

        private async void btnVisionReticlePosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("VISION RETICLE POSITION", () => MoveVisionSequenceAsync("Reticle"));
        }

        // ===================== 인터락 시퀀스 (OutputStage) =====================
        // 규칙: Y 이동 전 해당 빈 Z = Avoid · C(해당 빈 클램프리프트 Up) · P(Front/Rear 픽커 Z 전부 상승)
        // 홈게이트 = AVOID/LOAD/UNLOAD (PROCESS 제외) · 공유레일/알람은 이동 메서드 내부 자동검사
        // PROCESS는 VisionX 동반 이동 유지. NG의 Z 교시는 NgStage.Recipe(Work/AvoidPositionZ) 사용.

        // 마지막 시퀀스 중단 사유 — 실행 래퍼(ConfirmAndRunAsync)의 실패 팝업에 합쳐서 표시
        private string _lastAbortReason;

        private int AbortSeq(string title, string message)
        {
            // 상세 사유는 로그(EventLogger Alarm)에 기록하고, 팝업은 래퍼의 실패 팝업 하나로 합쳐 표시한다.
            EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", title + " 시퀀스 중단: " + message);
            _lastAbortReason = message;
            return -1;
        }

        private static BinStageAxis BinYAxis(BinSide side) { return side == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY; }
        private static BinStageAxis BinZAxis(BinSide side) { return side == BinSide.Ng ? BinStageAxis.NgBinZ : BinStageAxis.GoodBinZ; }

        private double GetBinZAvoidTarget(BinSide side)
        {
            return side == BinSide.Ng
                ? _outputStageUnit.NgStage.Recipe.AvoidPositionZ
                : _outputStageUnit.GetStageTeachingPosition(BinStageAxis.GoodBinZ, "Avoid");
        }

        // C: 해당 빈 클램프리프트 Up
        private bool CheckClampUp(BinSide side, out string reason)
        {
            reason = string.Empty;
            if (_outputStageUnit.IsBinGuideClampLiftUp(side))
                return true;
            reason = (side == BinSide.Ng ? "NG" : "GOOD") + " 클램프리프트 Up 미완료";
            return false;
        }

        // P: Front/Rear 픽커 Z 전부 상승(빈 위 간섭 차단)
        private bool CheckPickerZClear(out string reason)
        {
            reason = string.Empty;
            var machine = FindMachine();
            if (machine == null)
                return true;

            string block = machine.PickerFrontUnit != null ? machine.PickerFrontUnit.GetPickerZClearBlockReason() : null;
            if (block != null) { reason = "Front 픽커 " + block; return false; }

            block = machine.PickerRearUnit != null ? machine.PickerRearUnit.GetPickerZClearBlockReason() : null;
            if (block != null) { reason = "Rear 픽커 " + block; return false; }
            return true;
        }

        // VISION X(공유레일) 이동 전: Front/Rear 픽커가 Avoid 위치(Clear)인지 확인.
        // (InputStage의 CheckVisionXClear와 동일 기준 — 공유레일 충돌 방지)
        private bool CheckVisionXClear(out string reason)
        {
            reason = string.Empty;
            var machine = FindMachine();
            if (machine == null)
                return true;

            if (machine.PickerFrontUnit != null && !machine.PickerFrontUnit.IsPickerInAvoidPosition())
            { reason = "Front 픽커 Avoid 위치 아님"; return false; }
            if (machine.PickerRearUnit != null && !machine.PickerRearUnit.IsPickerInAvoidPosition())
            { reason = "Rear 픽커 Avoid 위치 아님"; return false; }
            return true;
        }

        // 홈게이트: 해당 빈 Y/Z 원점복귀 완료 (Z축이 없는 스테이지는 Z 생략 — 예: NG는 Y 전용)
        private bool CheckBinAxesHomed(BinSide side, out string reason)
        {
            reason = string.Empty;
            if (!_outputStageUnit.IsStageAxisHomeDone(BinYAxis(side))) { reason = BinYAxis(side) + " 원점복귀 필요"; return false; }
            if (_outputStageUnit.HasStageAxis(BinZAxis(side)) && !_outputStageUnit.IsStageAxisHomeDone(BinZAxis(side)))
            { reason = BinZAxis(side) + " 원점복귀 필요"; return false; }
            return true;
        }

        // GOOD/NG 빈 공통 시퀀스: 게이트 → Z→Avoid → Z확인 → Y→종류 → (종류별 Z) → (Process면 VisionX)
        private async Task<int> MoveBinSequenceAsync(BinSide side, string kind)
        {
            if (_outputStageUnit == null)
                return -1;

            string title = (side == BinSide.Ng ? "NG " : "GOOD ") + kind.ToUpperInvariant();
            string reason;

            // 이동 대상 축(해당 빈 Y/Z)의 HOME END(IsHomeDone) 미완료면 차단.
            if (!CheckBinAxesHomed(side, out reason))
                return AbortSeq(title, reason);
            if (!CheckClampUp(side, out reason))
                return AbortSeq(title, reason);
            if (!CheckPickerZClear(out reason))
                return AbortSeq(title, reason);

            // 이 스테이지에 Z축이 있는지 (예: NG는 Y 전용이라 Z축 없음 → Z 단계 전부 생략)
            bool hasZ = _outputStageUnit.HasStageAxis(BinZAxis(side));

            int r = 0;
            if (hasZ)
            {
                // 1) Z → Avoid (Y 이동 전 안전높이)
                double zAvoid = GetBinZAvoidTarget(side);
                r = await _outputStageUnit.MoveStageAxis(BinZAxis(side), zAvoid);
                if (r != 0) return AbortSeq(title, "Z Avoid 이동 실패");

                // 2) Z=Avoid 확인
                if (!_outputStageUnit.IsStageAxisAtPosition(BinZAxis(side), zAvoid))
                    return AbortSeq(title, "Y 이동 전 Z Avoid 미확인");
            }

            // 3) Y → 종류 위치
            r = await _outputStageUnit.MoveStageAxisToTeachingPosition(BinYAxis(side), kind);
            if (r != 0) return AbortSeq(title, "Y 이동 실패");

            // 4) 종류별 Z 마무리 (Z축 있을 때만)
            if (hasZ && string.Equals(kind, "Load", StringComparison.OrdinalIgnoreCase))
            {
                double zTarget = side == BinSide.Ng
                    ? _outputStageUnit.NgStage.Recipe.WorkPositionZ
                    : _outputStageUnit.GetStageTeachingPosition(BinStageAxis.GoodBinZ, "Load");
                r = await _outputStageUnit.MoveStageAxis(BinZAxis(side), zTarget);
                if (r != 0) return AbortSeq(title, "Z Load/Work 이동 실패");
            }
            else if (side == BinSide.Good &&
                     (string.Equals(kind, "Process", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(kind, "Unload", StringComparison.OrdinalIgnoreCase)))
            {
                r = await _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.GoodBinZ, kind);
                if (r != 0) return AbortSeq(title, "Z " + kind + " 이동 실패");
            }

            // 5) PROCESS: VisionX 동반 이동 (공유레일 → Front/Rear 픽커 Avoid 선행 확인)
            if (string.Equals(kind, "Process", StringComparison.OrdinalIgnoreCase))
            {
                if (!_outputStageUnit.IsStageAxisHomeDone(BinStageAxis.VisionX))
                    return AbortSeq(title, BinStageAxis.VisionX + " 원점복귀 필요");
                if (!CheckVisionXClear(out reason))
                    return AbortSeq(title, "VISION X 전 " + reason);
                r = await _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.VisionX, "Process");
                if (r != 0) return AbortSeq(title, "VISION X 이동 실패 (공유레일 확인)");
            }

            return 0;
        }

        // VISION 단독: AVOID/PROCESS는 바로, RETICLE은 레티클 실린더 Clear 선행
        private async Task<int> MoveVisionSequenceAsync(string kind)
        {
            if (_outputStageUnit == null)
                return -1;

            string title = "VISION " + kind.ToUpperInvariant();
            string reason;

            // 이동 대상 축(VISION X)의 HOME END(IsHomeDone) 미완료면 차단.
            if (!_outputStageUnit.IsStageAxisHomeDone(BinStageAxis.VisionX))
                return AbortSeq(title, BinStageAxis.VisionX + " 원점복귀 필요");

            if (string.Equals(kind, "Reticle", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsReticleClear(out reason))
                    return AbortSeq(title, "VISION X 전 레티클 " + reason);

                int zResult = await _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.GoodBinZ, "Reticle");
                if (zResult != 0)
                    return AbortSeq(title, "GOOD Z Reticle 이동 실패");
            }

            // 공유레일 → VISION X 이동 전 Front/Rear 픽커 Avoid 확인
            if (!CheckVisionXClear(out reason))
                return AbortSeq(title, "VISION X 전 " + reason);

            int r = await _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.VisionX, kind);
            if (r != 0) return AbortSeq(title, "VISION X 이동 실패 (공유레일 확인)");
            return 0;
        }

        // 레티클 실린더(승강/사이드슬라이드 전·후)가 모두 후퇴(Clear)인지 — 인풋스테이지와 동일 기준(공유 스테이션)
        private bool IsReticleClear(out string reason)
        {
            reason = string.Empty;
            var machine = FindMachine();
            if (machine == null || machine.VisionUnit == null)
                return true;

            var v = machine.VisionUnit;
            if (v.ReticleLift != null && v.ReticleLift.IsFwd) { reason = "ReticleLift 전개됨"; return false; }
            if (v.ReticleFrontSideSlide != null && v.ReticleFrontSideSlide.IsFwd) { reason = "ReticleSideSlideFront 전개됨"; return false; }
            if (v.ReticleRearSideSlide != null && v.ReticleRearSideSlide.IsFwd) { reason = "ReticleSideSlideRear 전개됨"; return false; }
            return true;
        }

        private void BindParameterGridMenus()
        {
            try
            {
                var menu = new ContextMenuStrip();
                menu.Items.Add("Move To Position", null, async (s, e) =>
                {
                    BinStageAxis axis;
                    string positionName;
                    if (TryGetSelectedTeachingPosition(out axis, out positionName))
                    {
                        // 이동 대상 축의 HOME END(IsHomeDone) 미완료면 차단.
                        if (!_outputStageUnit.IsStageAxisHomeDone(axis))
                        {
                            string homeMsg = optionParameterGrid.SelectedItem.Key + " 불가: " + axis + " 축 HOME END(원점복귀)가 완료되지 않았습니다.";
                            EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", homeMsg);
                            QMC.Common.MessageDialog.Show(this, homeMsg, "Output Stage Move", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        await ConfirmAndRunAsync(optionParameterGrid.SelectedItem.Key, () => _outputStageUnit.MoveStageAxisToTeachingPosition(axis, positionName, true));
                    }
                });
                menu.Items.Add("Teach Current Position", null, (s, e) =>
                {
                    BinStageAxis axis;
                    string positionName;
                    if (!TryGetSelectedTeachingPosition(out axis, out positionName))
                        return;

                    _outputStageUnit.TeachStageAxisPosition(axis, positionName);
                    SaveCurrentRecipeData();
                    RefreshView();
                });

                optionParameterGrid.ContextMenuStrip = menu;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", "BindParameterGridMenus failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private bool TryGetSelectedTeachingPosition(out BinStageAxis axis, out string positionName)
        {
            axis = BinStageAxis.GoodBinY;
            positionName = string.Empty;

            var item = optionParameterGrid.SelectedItem;
            string key = item != null ? item.Key : string.Empty;
            if (string.IsNullOrWhiteSpace(key) || !key.EndsWith(" POSITION", StringComparison.OrdinalIgnoreCase))
                return false;

            string name = key.Substring(0, key.Length - " POSITION".Length);
            if (name.StartsWith("GOOD Y ", StringComparison.OrdinalIgnoreCase))
            {
                axis = BinStageAxis.GoodBinY;
                positionName = name.Substring("GOOD Y ".Length);
                return true;
            }
            if (name.StartsWith("GOOD Z ", StringComparison.OrdinalIgnoreCase))
            {
                axis = BinStageAxis.GoodBinZ;
                positionName = name.Substring("GOOD Z ".Length);
                return true;
            }
            if (name.StartsWith("NG Y ", StringComparison.OrdinalIgnoreCase))
            {
                axis = BinStageAxis.NgBinY;
                positionName = name.Substring("NG Y ".Length);
                return true;
            }
            if (name.StartsWith("VISION X ", StringComparison.OrdinalIgnoreCase))
            {
                axis = BinStageAxis.VisionX;
                positionName = name.Substring("VISION X ".Length);
                return true;
            }

            return false;
        }

        private void BindIoPanel()
        {
            try
            {
                if (_outputStageUnit == null)
                    return;

                var unit = _outputStageUnit;
                ioCylinderPanel.SetItems(new[]
                {
                    // ===== 단독(묶이지 않은) 체크 센서 — 최상단 =====
                    IoCylinderItem.Input("GOOD BIN RING CHECK", () => IsOn(unit.GoodBinRingSensor)),
                    IoCylinderItem.Input("NG BIN RING CHECK", () => IsOn(unit.NgBinRingSensor)),

                    // ===== GOOD BIN : SET GUIDE (Up/Down 체크 센서 + Up/Down 출력 통합) =====
                    IoCylinderItem.Input("GOOD BIN GUIDE UP", () => IsOn(unit.GoodBinGuideUpSensor)),
                    IoCylinderItem.Input("GOOD BIN GUIDE DOWN", () => IsOn(unit.GoodBinGuideDownSensor)),
                    IoCylinderItem.Output("GOOD BIN GUIDE", () => unit.IsBinGuideUp(BinSide.Good),
                        on => SetBinGuideAsync(BinSide.Good, on), "UP", "DOWN"),

                    // ===== GOOD BIN : SET CLAMP LIFT (Up 체크 센서 + Up/Down 출력 통합) =====
                    IoCylinderItem.Input("GOOD BIN CLAMP LIFT UP", () => IsOn(unit.GoodBinClampUpSensor)),
                    IoCylinderItem.Output("GOOD BIN CLAMP LIFT", () => unit.IsBinGuideClampLiftUp(BinSide.Good),
                        on => SetBinClampLiftAsync(BinSide.Good, on), "UP", "DOWN"),

                    // ===== GOOD BIN : SET CLAMP (Unclamp 체크 센서 + Clamp/Unclamp 출력 통합) =====
                    IoCylinderItem.Input("GOOD BIN UNCLAMP", () => unit.IsBinGuideUnclamped(BinSide.Good)),
                    IoCylinderItem.Output("GOOD BIN CLAMP", () => unit.IsBinGuideClamped(BinSide.Good),
                        on => SetBinClampAsync(BinSide.Good, on), "CLAMP", "UNCLAMP"),

                    // ===== NG BIN : SET GUIDE (Up/Down 체크 센서 + Up/Down 출력 통합) =====
                    IoCylinderItem.Input("NG BIN GUIDE UP", () => IsOn(unit.NgBinGuideUpSensor)),
                    IoCylinderItem.Input("NG BIN GUIDE DOWN", () => IsOn(unit.NgBinGuideDownSensor)),
                    IoCylinderItem.Output("NG BIN GUIDE", () => unit.IsBinGuideUp(BinSide.Ng),
                        on => SetBinGuideAsync(BinSide.Ng, on), "UP", "DOWN"),

                    // ===== NG BIN : SET CLAMP LIFT (Up 체크 센서 + Up/Down 출력 통합) =====
                    IoCylinderItem.Input("NG BIN CLAMP LIFT UP", () => IsOn(unit.NgBinClampUpSensor)),
                    IoCylinderItem.Output("NG BIN CLAMP LIFT", () => unit.IsBinGuideClampLiftUp(BinSide.Ng),
                        on => SetBinClampLiftAsync(BinSide.Ng, on), "UP", "DOWN"),

                    // ===== NG BIN : SET CLAMP (Unclamp 체크 센서 + Clamp/Unclamp 출력 통합) =====
                    IoCylinderItem.Input("NG BIN UNCLAMP", () => unit.IsBinGuideUnclamped(BinSide.Ng)),
                    IoCylinderItem.Output("NG BIN CLAMP", () => unit.IsBinGuideClamped(BinSide.Ng),
                        on => SetBinClampAsync(BinSide.Ng, on), "CLAMP", "UNCLAMP"),

                    // ===== BOTTOM VISION BLOW (On/Off 출력 통합) =====
                    IoCylinderItem.Output("BOTTOM VISION BLOW", () => IsOn(unit.BottomVisionBlowOnOut),
                        on => GuardedPairOut("BottomVisionBlow", null, unit.BottomVisionBlowOnOut, unit.BottomVisionBlowOffOut, on), "ON", "OFF")
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", "BindIoPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage I/O", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private static Task<int> WritePairOut(QMC.Common.IO.BaseDigitalOutput forward, QMC.Common.IO.BaseDigitalOutput backward, bool forwardOn)
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

        // 실린더 DO pair 출력 전에 모션가드 Verify를 거친다.
        // cylinder가 있으면 실린더 모션가드(VerifyCylinderMove)로, 없으면 이름 기반 가드로 검사한다.
        private Task<int> GuardedPairOut(string movingName, QMC.Common.IO.BaseCylinder cylinder,
            QMC.Common.IO.BaseDigitalOutput forward, QMC.Common.IO.BaseDigitalOutput backward, bool forwardOn)
        {
            try
            {
                string reason;
                if (cylinder != null)
                {
                    // VerifyCylinderMove 내부에서 차단 시 Alarm/Log를 남긴다.
                    if (!MotionGuardRuntime.VerifyCylinderMove(cylinder, forwardOn, out reason))
                        return Task.FromResult(-1);
                }
                else
                {
                    if (!VerifyNamedCylinderMove(movingName, forwardOn, out reason))
                    {
                        EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", movingName + " output blocked by interlock: " + reason);
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

        // BaseCylinder가 없는 출력(예: 바텀비전 블로우)을 레지스트리 가드(CylinderMove)로 검사한다.
        private bool VerifyNamedCylinderMove(string movingName, bool forwardOn, out string reason)
        {
            var context = MotionGuardRuntime.ContextProvider != null ? MotionGuardRuntime.ContextProvider() : null;
            var request = new MotionGuardRuleContext(movingName, movingName, forwardOn ? 1.0 : 0.0,
                MotionGuardMoveKind.CylinderMove, string.Empty, null, context);
            return MotionGuardRuleRegistry.Verify(request, out reason);
        }

        private static Task<int> WriteOutAsync(QMC.Common.IO.BaseDigitalOutput output, bool on)
        {
            try
            {
                WriteOut(output, on);
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

        private Task<int> SetBinGuideAsync(BinSide side, bool on)
        {
            if (_outputStageUnit == null)
                return Task.FromResult(-1);

            return on
                ? _outputStageUnit.EnsureBinGuideUpAsync(side, ManualCylinderTimeoutMs)
                : _outputStageUnit.EnsureBinGuideDownAsync(side, ManualCylinderTimeoutMs);
        }

        private Task<int> SetBinClampLiftAsync(BinSide side, bool on)
        {
            if (_outputStageUnit == null)
                return Task.FromResult(-1);

            return on
                ? _outputStageUnit.EnsureBinGuideClampLiftUpAsync(side, ManualCylinderTimeoutMs)
                : _outputStageUnit.EnsureBinGuideClampLiftDownAsync(side, ManualCylinderTimeoutMs);
        }

        private Task<int> SetBinClampAsync(BinSide side, bool on)
        {
            if (_outputStageUnit == null)
                return Task.FromResult(-1);

            return on
                ? _outputStageUnit.EnsureBinGuideClampedAsync(side, ManualCylinderTimeoutMs)
                : _outputStageUnit.EnsureBinGuideUnclampedAsync(side, ManualCylinderTimeoutMs);
        }

        private void BindJogPanel()
        {
            try
            {
                if (_outputStageUnit == null)
                    return;

                var unit = _outputStageUnit;
                // 축 순서: GoodY/Z, NgY, VisionX
                var items = new List<JogAxisItem>
                {
                    BuildJogAxis("GOOD Y", unit.GoodStage.StageY, "Y+", "Y-", JogAxisControlKind.Vertical),
                    BuildJogAxis("GOOD Z", unit.GoodStage.StageZ, "Z+", "Z-", JogAxisControlKind.Vertical),
                    BuildJogAxis("NG Y", unit.NgStage.StageY, "Y+", "Y-", JogAxisControlKind.Vertical),
                    BuildJogAxis("VISION X", unit.OutputCameraX, "X+", "X-", JogAxisControlKind.Vertical)
                };

                jogPositionListControl.SetItems(items);
                jogAxisMoveControl.SetItems(items);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", "BindJogPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private JogAxisItem BuildJogAxis(string name, BaseAxis axis, string plus, string minus, JogAxisControlKind kind)
        {
            JogAxisItem item = JogAxisItem.Single(name, axis, AxisUnitConverter.DisplayUnitFor(axis), 1.0, plus, minus).WithControlKind(kind);
            item.StepMoveAsync = (it, direction, speedType, customSpeed, axisStepDistance) =>
                _outputStageUnit.JogStepAsync(axis, direction, speedType, customSpeed, axisStepDistance);
            item.ContinuousMoveAsync = (it, direction, speedType, customSpeed) =>
                _outputStageUnit.JogContinuousAsync(axis, direction, speedType, customSpeed);
            item.StopAsync = it => _outputStageUnit.StopJogAsync(axis);
            return item;
        }

        private async Task ConfirmAndRunAsync(string actionName, Func<Task<int>> action)
        {
            try
            {
                if (_outputStageUnit == null || action == null)
                    return;

                if (ManualMoveGuard.BlockIfNotReady(this, "Output Stage"))
                    return;

                DialogResult confirm = QMC.Common.MessageDialog.Show(this, actionName + " 진행하시겠습니까?", "Output Stage", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                Cursor = Cursors.WaitCursor;
                _lastAbortReason = null;
                int result = await action();
                EventLogger.Write(EventKind.Event, "UI", "OUTPUT-STAGE", actionName + " result=" + result);
                if (result != 0)
                {
                    string detail = string.IsNullOrEmpty(_lastAbortReason) ? "" : Environment.NewLine + "사유 : " + _lastAbortReason;
                    QMC.Common.MessageDialog.Show(this, actionName + " 실패" + detail, "Output Stage", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                if (_outputStageUnit == null)
                    return;

                var unit = _outputStageUnit;
                optionParameterGrid.RefreshValues();
                waitParameterGrid.RefreshValues();
                ioCylinderPanel.RefreshStates();
                jogPositionListControl.RefreshState();

                lblVisionInfo.Text =
                    "OUTPUT STAGE" + Environment.NewLine +
                    "GOOD Y : " + FormatAxis(unit.GoodStage.StageY.ActualPosition, unit.GoodStage.StageY) + Environment.NewLine +
                    "GOOD Z : " + FormatAxis(unit.GoodStage.StageZ.ActualPosition, unit.GoodStage.StageZ) + Environment.NewLine +
                    "NG Y   : " + FormatAxis(unit.NgStage.StageY.ActualPosition, unit.NgStage.StageY) + Environment.NewLine +
                    "BIN X  : " + FormatAxis(unit.OutputCameraX.ActualPosition, unit.OutputCameraX) + Environment.NewLine +
                    "G-AVOID: " + OnOff(unit.GoodStage.IsAtAvoidPosition()) + Environment.NewLine +
                    "N-AVOID: " + OnOff(unit.NgStage.IsAtAvoidPosition());
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
    }
}
