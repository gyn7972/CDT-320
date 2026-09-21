using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT320;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.IO;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>Output Cassette 레시피에서 OutCassetteUnit을 조작하는 화면입니다.</summary>
    public partial class OutputCassetteRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private const int NgBinManualFeedbackTimeoutMs = 3000;

        private OutputCassetteUnit _OutCassetteUnit;
        private readonly Timer _refreshTimer = new Timer();
        private readonly ToolTip _toolTip = new ToolTip();
        /// <summary>OutputCassetteRecipePage를 생성합니다.</summary>
        public OutputCassetteRecipePage()
        {
            try
            {
                InitializeComponent();
                InitializeRecipeLanguageBindings();
                if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                    return;

                ApplyRecipeTheme();
                ConfigureRuntimeBehavior();
                ConfigureManualActions();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(ex.Message, "Output Cassette", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                lblHeader.Tag = "i18n:recipe.outputCassette";
                lblHeader.Text = Lang.T("recipe.outputCassette");
                _refreshTimer.Interval = 250;
                _refreshTimer.Tick += RefreshTimer_Tick;
                optionParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
                waitParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
                BindParameterGridMenus();
                BindTeachingMenus();

                grpIo.ContextMenuStrip = new ContextMenuStrip();
                Lang.BindKey(grpIo.ContextMenuStrip.Items.Add("Output cassette DI 상태를 다시 읽습니다.", null, IoRefresh_Click), "recipeUi.menu.outputCassette");

                Lang.BindKey(_toolTip, lblRecipeLoadingVal, "recipeUi.tooltip.1");
                Lang.BindKey(_toolTip, lblRecipeUnloadingVal, "recipeUi.tooltip.1");
                Lang.BindKey(_toolTip, lblConfigSlotCountVal, "recipeUi.tooltip.2");
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Configure", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                _OutCassetteUnit = machine != null ? machine.OutputCassetteUnit : null;

                if (_OutCassetteUnit != null)
                    _OutCassetteUnit.Recipe.EnsureSlotPositionBuffers(_OutCassetteUnit.Config.SlotCount);

                SetEnabledState(_OutCassetteUnit != null);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Resolve", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Enable", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ConfigureManualActions()
        {
            try
            {
                // 공용 MANUAL ACTION 판넬에 GOOD/NG 리프터 이동 버튼 등록 (2열: GOOD | NG, READY는 마지막)
                manualActionPanel.ColumnCount = 2;
                manualActionPanel.SetItems(new[]
                {
                    ManualActionItem.Create("GOOD LOADING MOVE", () => ConfirmManualMoveAsync("GOOD LOADING", () => MoveToTarget("GOOD LOADING Z", _OutCassetteUnit.Recipe.GoodLoaingPosition))),
                    ManualActionItem.Create("NG LOADING MOVE", () => ConfirmManualMoveAsync("NG LOADING", () => MoveToTarget("NG LOADING Z", _OutCassetteUnit.Recipe.NGLoaingPosition))),
                    ManualActionItem.Create("GOOD UNLOADING MOVE", () => ConfirmManualMoveAsync("GOOD UNLOADING", () => MoveToTarget("GOOD UNLOADING Z", _OutCassetteUnit.Recipe.GoodUnloadingPosition))),
                    ManualActionItem.Create("NG UNLOADING MOVE", () => ConfirmManualMoveAsync("NG UNLOADING", () => MoveToTarget("NG UNLOADING Z", _OutCassetteUnit.Recipe.NGUnloadingPosition))),
                    ManualActionItem.Create("GOOD SLOT START", () => ConfirmManualMoveAsync("GOOD SLOT START", () => MoveCassetteSlot(TargetCassette.Good1, _OutCassetteUnit != null ? _OutCassetteUnit.Config.LoadingPositionOffset : 0.0, "Output cassette good slot start move"))),
                    ManualActionItem.Create("NG SLOT START", () => ConfirmManualMoveAsync("NG SLOT START", () => MoveCassetteSlot(TargetCassette.Ng, _OutCassetteUnit != null ? _OutCassetteUnit.Config.LoadingPositionOffset : 0.0, "Output cassette ng slot start move"))),
                    ManualActionItem.Create("GOOD SLOT END", () => ConfirmManualMoveAsync("GOOD SLOT END", () => MoveCassetteSlot(TargetCassette.Good1, _OutCassetteUnit != null ? _OutCassetteUnit.Config.UnloadingPositionOffset : 0.0, "Output cassette good slot end move"))),
                    ManualActionItem.Create("NG SLOT END", () => ConfirmManualMoveAsync("NG SLOT END", () => MoveCassetteSlot(TargetCassette.Ng, _OutCassetteUnit != null ? _OutCassetteUnit.Config.UnloadingPositionOffset : 0.0, "Output cassette ng slot end move"))),
                    ManualActionItem.Create("READY MOVE", () => ConfirmManualMoveAsync("READY", () => MoveToTarget("READY POSITION", _OutCassetteUnit.Recipe.AvoidPosition)))
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-CASSETTE", "ConfigureManualActions failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task ConfirmManualMoveAsync(string targetName, Func<Task> moveAsync)
        {
            if (_OutCassetteUnit == null || moveAsync == null)
                return;

            DialogResult result = QMC.Common.MessageDialog.Show(
                this,
                targetName + " 위치로 이동하시겠습니까?",
                "Output Cassette Move",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;

            await moveAsync();
        }

        private async void btnGoodLoadingMove_Click(object sender, EventArgs e)
        {
            try
            {
                if (_OutCassetteUnit == null) return;

                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    "GOOD Loading Move를 진행하시겠습니까?",
                    "Output Cassette Move",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "UI",
                        "OUTPUT-CASSETTE",
                        "btnGoodLoadingMove_Click canceled.");
                    return;
                }

                await MoveToTarget("GOOD LOADING Z", _OutCassetteUnit.Recipe.GoodLoaingPosition);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnGoodUnloadingMove_Click(object sender, EventArgs e)
        {
            try
            {
                if (_OutCassetteUnit == null) return;
                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    "GOOD Unloading Move를 진행하시겠습니까?",
                    "Output Cassette Move",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "UI",
                        "OUTPUT-CASSETTE",
                        "btnGoodUnloadingMove_Click canceled.");
                    return;
                }
                await MoveToTarget("GOOD UNLOADING Z", _OutCassetteUnit.Recipe.GoodUnloadingPosition);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnGoodSlotStartMove_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    "GOOD 슬롯 START 위치로 이동하시겠습니까?",
                    "Output Cassette Move",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "UI",
                        "OUTPUT-CASSETTE",
                        "btnGoodSlotStartMove_Click canceled.");
                    return;
                }

                await MoveCassetteSlot(TargetCassette.Good1, _OutCassetteUnit != null ? _OutCassetteUnit.Config.LoadingPositionOffset : 0.0, "Output cassette good slot start move");
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Slot Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnGoodSlotEndMove_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    "GOOD 슬롯 END 위치로 이동하시겠습니까?",
                    "Output Cassette Move",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "UI",
                        "OUTPUT-CASSETTE",
                        "btnGoodSlotEndMove_Click canceled.");
                    return;
                }

                await MoveCassetteSlot(TargetCassette.Good1, _OutCassetteUnit != null ? _OutCassetteUnit.Config.UnloadingPositionOffset : 0.0, "Output cassette good slot end move");
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Slot Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnNgLoadingMove_Click(object sender, EventArgs e)
        {
            try
            {
                if (_OutCassetteUnit == null) return;

                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    "NG Loading Move를 진행하시겠습니까?",
                    "Output Cassette Move",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "UI",
                        "OUTPUT-CASSETTE",
                        "btnNgLoadingMove_Click canceled.");
                    return;
                }

                await MoveToTarget("NG LOADING Z", _OutCassetteUnit.Recipe.NGLoaingPosition);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnNgUnloadingMove_Click(object sender, EventArgs e)
        {
            try
            {
                if (_OutCassetteUnit == null) return;
                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    "NG Unloading Move를 진행하시겠습니까?",
                    "Output Cassette Move",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "UI",
                        "OUTPUT-CASSETTE",
                        "btnNgUnloadingMove_Click canceled.");
                    return;
                }
                await MoveToTarget("NG UNLOADING Z", _OutCassetteUnit.Recipe.NGUnloadingPosition);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnNgSlotStartMove_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    "NG 슬롯 START 위치로 이동하시겠습니까?",
                    "Output Cassette Move",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "UI",
                        "OUTPUT-CASSETTE",
                        "btnNgSlotStartMove_Click canceled.");
                    return;
                }

                await MoveCassetteSlot(TargetCassette.Ng, _OutCassetteUnit != null ? _OutCassetteUnit.Config.LoadingPositionOffset : 0.0, "Output cassette ng slot start move");
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Slot Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnNgSlotEndMove_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    "NG 슬롯 END 위치로 이동하시겠습니까?",
                    "Output Cassette Move",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "UI",
                        "OUTPUT-CASSETTE",
                        "btnNgSlotEndMove_Click canceled.");
                    return;
                }

                await MoveCassetteSlot(TargetCassette.Ng, _OutCassetteUnit != null ? _OutCassetteUnit.Config.UnloadingPositionOffset : 0.0, "Output cassette ng slot end move");
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Slot Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnReadyMove_Click(object sender, EventArgs e)
        {
            try
            {
                if (_OutCassetteUnit == null) return;
                DialogResult result = QMC.Common.MessageDialog.Show(
                    this,
                    "이동하시겠습니까?",
                    "Output Cassette Move",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        "UI",
                        "OUTPUT-CASSETTE",
                        "btnReadyMove_Click canceled.");
                    return;
                }
                await MoveToTarget("READY POSITION", _OutCassetteUnit.Recipe.AvoidPosition);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async Task MoveToTarget(string actionName, double target)
        {
            try
            {
                if (_OutCassetteUnit == null) return;
                if (!EnsureOutputLifterZHomeDone(actionName)) return;
                await RunSafeAsync(async () =>
                {
                    await _OutCassetteUnit.MoveBinLifterZ(
                        target,
                        jogAxisMoveControl.SelectedSpeedType,
                        jogAxisMoveControl.GetSelectedSpeed(_OutCassetteUnit.OutputLifterZ));
                    bool done = await _OutCassetteUnit.WaitBinLifterZMoveDone(_OutCassetteUnit.OutputLifterZ.Setup.MoveTimeoutMs);
                    return done ? 0 : -1;
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

        private async Task MoveCassetteSlot(TargetCassette cassette, double offset, string actionName)
        {
            try
            {
                if (_OutCassetteUnit == null) return;
                if (!EnsureOutputLifterZHomeDone(actionName)) return;
                await RunSafeAsync(async () =>
                {
                    double target = _OutCassetteUnit.CalculateBinCassetteSlotTargetPosition(cassette, 0) + offset;
                    await _OutCassetteUnit.MoveBinLifterZ(
                        target,
                        jogAxisMoveControl.SelectedSpeedType,
                        jogAxisMoveControl.GetSelectedSpeed(_OutCassetteUnit.OutputLifterZ));
                    bool done = await _OutCassetteUnit.WaitBinLifterZMoveDone(_OutCassetteUnit.OutputLifterZ.Setup.MoveTimeoutMs);
                    return done ? 0 : -1;
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

        // 이동 대상 축(Output Lifter Z)의 HOME END(IsHomeDone) 미완료면 차단.
        private bool EnsureOutputLifterZHomeDone(string actionName)
        {
            if (_OutCassetteUnit != null && _OutCassetteUnit.OutputLifterZ != null && !_OutCassetteUnit.OutputLifterZ.IsHomeDone)
            {
                string homeMsg = actionName + " 불가: Output Lifter Z 축 HOME END(원점복귀)가 완료되지 않았습니다.";
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-CASSETTE", homeMsg);
                QMC.Common.MessageDialog.Show(this, homeMsg, actionName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                using (MotionGuardRuntime.BeginManualSequenceProcessMove("OutputCassetteRecipePage." + actionName))
                {
                    result = await action();
                }
                if (result != 0)
                {
                    string msg = _OutCassetteUnit != null ? _OutCassetteUnit.LastBinLifterMoveFailureMessage : null;
                    string detail = string.IsNullOrEmpty(msg) ? "" : Environment.NewLine + "사유 : " + msg;
                    QMC.Common.MessageDialog.Show(this, actionName + " 실패" + detail, "Output Cassette", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void BindTeachingMenus()
        {
            try
            {
                AttachTeachMenu(lblRecipeLoadingVal, "GoodLoading");
                AttachTeachMenu(lblRecipeUnloadingVal, "GoodUnloading");
                AttachTeachMenu(lblRecipeAvoidVal, "Avoid");
                AttachTeachMenu(lblRecipeFirstSlotVal, "GoodFirstSlot");
                AttachTeachMenu(lblRecipeMappingStartVal, "MappingStart");
                AttachTeachMenu(lblRecipeMappingEndVal, "MappingEnd");
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Teach Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
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
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-CASSETTE", "BindParameterGridMenus failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                if (ConfirmMoveToPositionSpeed("Output Cassette Move", e.Item.Key))
                    await MoveByPositionName(positionName);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-CASSETTE", "Move button failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                if (!ConfirmTeachPosition("Output Cassette Teach", e.Item.Key))
                    return;

                TeachPosition(positionName);
                SaveCurrentRecipeData();
                RefreshView();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-CASSETTE", "Teach button failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Teach", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                EventLogger.Write(EventKind.Event, "UI", "OUTPUT-CASSETTE", actionName + " canceled.");
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

                EventLogger.Write(EventKind.Event, "UI", "OUTPUT-CASSETTE", name + " teach canceled.");
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

                if (string.Equals(item.Key, "READY POSITION", StringComparison.OrdinalIgnoreCase))
                    return "Avoid";
                if (string.Equals(item.Key, "GOOD LOADING Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "GoodLoading";
                if (string.Equals(item.Key, "GOOD UNLOADING Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "GoodUnloading";
                if (string.Equals(item.Key, "GOOD FIRST SLOT POSITION", StringComparison.OrdinalIgnoreCase))
                    return "GoodFirstSlot";
                if (string.Equals(item.Key, "NG LOADING Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "NgLoading";
                if (string.Equals(item.Key, "NG UNLOADING Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "NgUnloading";
                if (string.Equals(item.Key, "NG FIRST SLOT POSITION", StringComparison.OrdinalIgnoreCase))
                    return "NgFirstSlot";
                if (string.Equals(item.Key, "MAPPING START Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "MappingStart";
                if (string.Equals(item.Key, "MAPPING END Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "MappingEnd";
                // To do: [존 분리 스캔] 존별 티칭 키 매핑.
                if (string.Equals(item.Key, "GOOD2 FIRST SLOT POSITION", StringComparison.OrdinalIgnoreCase))
                    return "Good2FirstSlot";
                if (string.Equals(item.Key, "NG MAPPING START Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "NgMappingStart";
                if (string.Equals(item.Key, "NG MAPPING END Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "NgMappingEnd";
                if (string.Equals(item.Key, "GOOD1 MAPPING START Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "Good1MappingStart";
                if (string.Equals(item.Key, "GOOD1 MAPPING END Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "Good1MappingEnd";
                if (string.Equals(item.Key, "GOOD2 MAPPING START Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "Good2MappingStart";
                if (string.Equals(item.Key, "GOOD2 MAPPING END Z POSITION", StringComparison.OrdinalIgnoreCase))
                    return "Good2MappingEnd";

                return string.Empty;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-CASSETTE", "GetSelectedTeachingPositionName failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Teach Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async Task MoveByPositionName(string positionName)
        {
            try
            {
                if (_OutCassetteUnit == null) return;

                if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("READY POSITION", _OutCassetteUnit.Recipe.AvoidPosition);
                else if (string.Equals(positionName, "GoodLoading", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("GOOD LOADING Z", _OutCassetteUnit.Recipe.GoodLoaingPosition);
                else if (string.Equals(positionName, "GoodUnloading", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("GOOD UNLOADING Z", _OutCassetteUnit.Recipe.GoodUnloadingPosition);
                else if (string.Equals(positionName, "GoodFirstSlot", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("GOOD FIRST SLOT", _OutCassetteUnit.Recipe.GoodFirstSlotPosition);
                else if (string.Equals(positionName, "NgLoading", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("NG LOADING Z", _OutCassetteUnit.Recipe.NGLoaingPosition);
                else if (string.Equals(positionName, "NgUnloading", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("NG UNLOADING Z", _OutCassetteUnit.Recipe.NGUnloadingPosition);
                else if (string.Equals(positionName, "NgFirstSlot", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("NG FIRST SLOT", _OutCassetteUnit.Recipe.NGFirstSlotPosition);
                else if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("MAPPING START Z", _OutCassetteUnit.Recipe.MappingStartPosition);
                else if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("MAPPING END Z", _OutCassetteUnit.Recipe.MappingEndPosition);
                // To do: [존 분리 스캔] 존별 티칭 위치 이동.
                else if (string.Equals(positionName, "Good2FirstSlot", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("GOOD2 FIRST SLOT", _OutCassetteUnit.Recipe.Good2FirstSlotPosition);
                else if (string.Equals(positionName, "NgMappingStart", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("NG MAPPING START Z", _OutCassetteUnit.Recipe.NgMappingStartPosition);
                else if (string.Equals(positionName, "NgMappingEnd", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("NG MAPPING END Z", _OutCassetteUnit.Recipe.NgMappingEndPosition);
                else if (string.Equals(positionName, "Good1MappingStart", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("GOOD1 MAPPING START Z", _OutCassetteUnit.Recipe.Good1MappingStartPosition);
                else if (string.Equals(positionName, "Good1MappingEnd", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("GOOD1 MAPPING END Z", _OutCassetteUnit.Recipe.Good1MappingEndPosition);
                else if (string.Equals(positionName, "Good2MappingStart", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("GOOD2 MAPPING START Z", _OutCassetteUnit.Recipe.Good2MappingStartPosition);
                else if (string.Equals(positionName, "Good2MappingEnd", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("GOOD2 MAPPING END Z", _OutCassetteUnit.Recipe.Good2MappingEndPosition);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void TeachPosition(string positionName)
        {
            try
            {
                if (_OutCassetteUnit == null) return;

                if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.TeachBinLifterZAvoidPosition();
                else if (string.Equals(positionName, "GoodLoading", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.Recipe.GoodLoaingPosition = _OutCassetteUnit.OutputLifterZ.ActualPosition;
                else if (string.Equals(positionName, "GoodUnloading", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.Recipe.GoodUnloadingPosition = _OutCassetteUnit.OutputLifterZ.ActualPosition;
                else if (string.Equals(positionName, "GoodFirstSlot", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.TeachBinLifterZFirstSlotPosition(TargetCassette.Good1);
                else if (string.Equals(positionName, "NgLoading", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.Recipe.NGLoaingPosition = _OutCassetteUnit.OutputLifterZ.ActualPosition;
                else if (string.Equals(positionName, "NgUnloading", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.Recipe.NGUnloadingPosition = _OutCassetteUnit.OutputLifterZ.ActualPosition;
                else if (string.Equals(positionName, "NgFirstSlot", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.TeachBinLifterZFirstSlotPosition(TargetCassette.Ng);
                else if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.TeachBinLifterZMappingStartPosition();
                else if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.TeachBinLifterZMappingEndPosition();
                // To do: [존 분리 스캔] 존별 티칭.
                else if (string.Equals(positionName, "Good2FirstSlot", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.TeachBinLifterZFirstSlotPosition(TargetCassette.Good2);
                else if (string.Equals(positionName, "NgMappingStart", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.TeachBinLifterZZoneMappingStartPosition(TargetCassette.Ng);
                else if (string.Equals(positionName, "NgMappingEnd", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.TeachBinLifterZZoneMappingEndPosition(TargetCassette.Ng);
                else if (string.Equals(positionName, "Good1MappingStart", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.TeachBinLifterZZoneMappingStartPosition(TargetCassette.Good1);
                else if (string.Equals(positionName, "Good1MappingEnd", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.TeachBinLifterZZoneMappingEndPosition(TargetCassette.Good1);
                else if (string.Equals(positionName, "Good2MappingStart", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.TeachBinLifterZZoneMappingStartPosition(TargetCassette.Good2);
                else if (string.Equals(positionName, "Good2MappingEnd", StringComparison.OrdinalIgnoreCase))
                    _OutCassetteUnit.TeachBinLifterZZoneMappingEndPosition(TargetCassette.Good2);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Teach", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindParameterGrids()
        {
            try
            {
                if (_OutCassetteUnit == null)
                    return;

                optionParameterGrid.SetItems(new[]
                {
                    AxisDouble("READY POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.AvoidPosition, v => _OutCassetteUnit.Recipe.AvoidPosition = v),
                    AxisDouble("GOOD LOADING Z POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.GoodLoaingPosition, v => _OutCassetteUnit.Recipe.GoodLoaingPosition = v),
                    AxisDouble("GOOD UNLOADING Z POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.GoodUnloadingPosition, v => _OutCassetteUnit.Recipe.GoodUnloadingPosition = v),
                    AxisDouble("GOOD FIRST SLOT POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.GoodFirstSlotPosition, v => _OutCassetteUnit.Recipe.GoodFirstSlotPosition = v),
                    AxisDouble("NG LOADING Z POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.NGLoaingPosition, v => _OutCassetteUnit.Recipe.NGLoaingPosition = v),
                    AxisDouble("NG UNLOADING Z POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.NGUnloadingPosition, v => _OutCassetteUnit.Recipe.NGUnloadingPosition = v),
                    AxisDouble("NG FIRST SLOT POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.NGFirstSlotPosition, v => _OutCassetteUnit.Recipe.NGFirstSlotPosition = v),
                    // To do: [존 분리 스캔] 존별 스캔 구간 + Good2 스타트 포지션. 기존 단일 MAPPING START/END 행은 레거시로 제외.
                    AxisDouble("GOOD2 FIRST SLOT POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.Good2FirstSlotPosition, v => _OutCassetteUnit.Recipe.Good2FirstSlotPosition = v),
                    AxisDouble("NG MAPPING START Z POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.NgMappingStartPosition, v => _OutCassetteUnit.Recipe.NgMappingStartPosition = v),
                    AxisDouble("NG MAPPING END Z POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.NgMappingEndPosition, v => _OutCassetteUnit.Recipe.NgMappingEndPosition = v),
                    AxisDouble("GOOD1 MAPPING START Z POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.Good1MappingStartPosition, v => _OutCassetteUnit.Recipe.Good1MappingStartPosition = v),
                    AxisDouble("GOOD1 MAPPING END Z POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.Good1MappingEndPosition, v => _OutCassetteUnit.Recipe.Good1MappingEndPosition = v),
                    AxisDouble("GOOD2 MAPPING START Z POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.Good2MappingStartPosition, v => _OutCassetteUnit.Recipe.Good2MappingStartPosition = v),
                    AxisDouble("GOOD2 MAPPING END Z POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.Good2MappingEndPosition, v => _OutCassetteUnit.Recipe.Good2MappingEndPosition = v),
                    //AxisDouble("MAPPING START Z POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.MappingStartPosition, v => _OutCassetteUnit.Recipe.MappingStartPosition = v),
                    //AxisDouble("MAPPING END Z POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.MappingEndPosition, v => _OutCassetteUnit.Recipe.MappingEndPosition = v),
                    AxisDouble("LOADING OFFSET", ParameterGridScope.Config, () => _OutCassetteUnit.Config.LoadingPositionOffset, v => _OutCassetteUnit.Config.LoadingPositionOffset = v),
                    AxisDouble("UNLOADING OFFSET", ParameterGridScope.Config, () => _OutCassetteUnit.Config.UnloadingPositionOffset, v => _OutCassetteUnit.Config.UnloadingPositionOffset = v),
                    AxisDouble(
                        "UNLOAD RELEASE LIFT DISTANCE",
                        ParameterGridScope.Config,
                        () => _OutCassetteUnit.Config.UnloadReleaseLiftDistance,
                        v => _OutCassetteUnit.Config.UnloadReleaseLiftDistance =
                            Math.Min(
                                OutputCassetteUnit.MaxUnloadReleaseLiftDistanceMm,
                                Math.Max(
                                    OutputCassetteUnit.MinUnloadReleaseLiftDistanceMm,
                                    v))),
                    AxisDouble("LEVEL 2 OFFSET", ParameterGridScope.Config, () => _OutCassetteUnit.Config.Level2PositionOffset, v => _OutCassetteUnit.Config.Level2PositionOffset = Math.Max(0.0, v)),
                    AxisDouble("GOOD/NG OFFSET", ParameterGridScope.Config, () => _OutCassetteUnit.Config.GOODNGPositionOffset, v => _OutCassetteUnit.Config.GOODNGPositionOffset = v),
                    AxisDouble("SLOT PITCH", ParameterGridScope.Config, () => _OutCassetteUnit.Config.SlotPitch, v => _OutCassetteUnit.Config.SlotPitch = Math.Max(0.0, v)),
                    ParameterGridItem.Int("SLOT COUNT", "ea", ParameterGridScope.Config, () => _OutCassetteUnit.Config.SlotCount, v =>
                    {
                        _OutCassetteUnit.Config.SlotCount = Math.Max(0, v);
                        _OutCassetteUnit.Recipe.EnsureSlotPositionBuffers(_OutCassetteUnit.Config.SlotCount);
                    }),
                    AxisDouble("SCAN/JOG VELOCITY", ParameterGridScope.Config, () => _OutCassetteUnit.Config.ScanVelocity, v => _OutCassetteUnit.Config.ScanVelocity = Math.Max(0.1, v), "/s"),
                    ParameterGridItem.Selection("INCH SELECT", "Inch", ParameterGridScope.Config, () => _OutCassetteUnit.Config.InchSelect, v => _OutCassetteUnit.Config.InchSelect = Convert.ToInt32(v), new[]
                    {
                        new ParameterGridOption("8", 8),
                        new ParameterGridOption("12", 12)
                    }),
                    ParameterGridItem.Selection("CASSETTE LEVEL", "단", ParameterGridScope.Config, () => _OutCassetteUnit.Config.SelectedCassetteLevel, v => _OutCassetteUnit.Config.SelectedCassetteLevel = Convert.ToInt32(v), new[]
                    {
                        new ParameterGridOption("1", 1),
                        new ParameterGridOption("2", 2)
                    }),
                    ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => _OutCassetteUnit.Setup.IsSimulationMode, v => _OutCassetteUnit.Setup.IsSimulationMode = v),
                    ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => _OutCassetteUnit.Config.bDryRun, v => _OutCassetteUnit.Config.bDryRun = v),
                    // To do: [NG 스킵] NG 카세트 사용 여부 - false면 오토가 NG 공급/맵핑 요구를 건너뛴다.
                    //        변경 즉시 Ng1 Material IsEnabled에도 동기화해 플래너 판단과 일치시킨다.
                    ParameterGridItem.Bool("USE NG CASSETTE", ParameterGridScope.Config, () => _OutCassetteUnit.Config.UseNgCassette, v =>
                    {
                        _OutCassetteUnit.Config.UseNgCassette = v;
                        MaterialStateService.SetCassetteEnabled(CassetteMaterialRole.Ng1, v);
                    })
                });

                waitParameterGrid.AutoFitParentGroupHeight = true;   // WAIT 그룹 높이를 내용에 맞춰 자동 조정 (스크롤 없이 전 항목 표시)
                waitParameterGrid.SetItems(new[]
                {
                    ParameterGridItem.Int("SCAN SETTLE TIME", "ms", ParameterGridScope.Config, () => _OutCassetteUnit.Config.ScanSettleTimeMs, v => _OutCassetteUnit.Config.ScanSettleTimeMs = Math.Max(0, v))
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-CASSETTE", "BindParameterGrids failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Parameters", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindIoPanel()
        {
            try
            {
                if (_OutCassetteUnit == null)
                    return;

                ioCylinderPanel.ColumnCount = 2;   // 2열 배치 (Input Feeder 기준)
                ioCylinderPanel.SetItems(new[]
                {
                    // ===== 단독(묶이지 않은) 체크 센서 — 최상단 =====
                    //IoCylinderItem.Input("GOOD BIN 8 INCH CASSETTE", () => _OutCassetteUnit.IsGoodBin(8) && !_OutCassetteUnit.IsNgBin(8) && !_OutCassetteUnit.IsNgBin(12) && !_OutCassetteUnit.IsGoodBin(12)),
                    //IoCylinderItem.Input("GOOD BIN 12 INCH CASSETTE", () => _OutCassetteUnit.IsGoodBin(12) && !_OutCassetteUnit.IsNgBin(12) && !_OutCassetteUnit.IsNgBin(8) && !_OutCassetteUnit.IsGoodBin(8)),
                    //IoCylinderItem.Input("NG BIN 8 INCH CASSETTE", () => !_OutCassetteUnit.IsGoodBin(8) && _OutCassetteUnit.IsNgBin(8) && !_OutCassetteUnit.IsNgBin(12) && !_OutCassetteUnit.IsGoodBin(12)),
                    //IoCylinderItem.Input("NG BIN 12 INCH CASSETTE", () => !_OutCassetteUnit.IsGoodBin(12) && _OutCassetteUnit.IsNgBin(12) && !_OutCassetteUnit.IsNgBin(8) && !_OutCassetteUnit.IsGoodBin(8)),
                    IoCylinderItem.Input("GOOD BIN 8 INCH CASSETTE", () => _OutCassetteUnit.IsBinCassettePresentAll(TargetCassette.Good1, 8)),
                    IoCylinderItem.Input("GOOD BIN 12 INCH CASSETTE", () => _OutCassetteUnit.IsBinCassettePresentAll(TargetCassette.Good1, 12)),
                    IoCylinderItem.Input("NG BIN 8 INCH CASSETTE", () => _OutCassetteUnit.IsBinCassettePresentAll(TargetCassette.Ng, 8)),
                    IoCylinderItem.Input("NG BIN 12 INCH CASSETTE", () => _OutCassetteUnit.IsBinCassettePresentAll(TargetCassette.Ng, 12)),
                    IoCylinderItem.Input("BIN RING JUT CHECK", () => _OutCassetteUnit.IsBinProtrusionDetectionSensor()),
                    IoCylinderItem.Input("BIN MAPPING", () => _OutCassetteUnit.IsBinMapping()),

                    // ===== SET: NG BIN LOCK (Lock/Bw 체크 센서 + Lock/Unlock 출력 통합) =====
                    IoCylinderItem.Input("NG BIN LOCK CHECK", () => _OutCassetteUnit.IsNgBinLock()),
                    // X082 원신호 설정은 유지하고 B접점 의미값인 Unlock Check로 표시한다.
                    IoCylinderItem.Input("NG BIN UNLOCK CHECK", () => _OutCassetteUnit.IsNgBinUnlockCheck()),
                    IoCylinderItem.Output("NG BIN LOCK",
                        () => _OutCassetteUnit.NgBinCassetteLockOut != null && _OutCassetteUnit.NgBinCassetteLockOut.IsOn,
                        async on =>
                        {
                            // 수동 복구 조작은 센서 완료 여부로 출력을 선차단하지 않는다.
                            // Y042/Y043를 먼저 전환한 뒤 X083/X082 완료 신호를 확인한다.
                            bool completed = await _OutCassetteUnit.NGBinLockCylinder(
                                on,
                                NgBinManualFeedbackTimeoutMs).ConfigureAwait(true);
                            if (!completed)
                            {
                                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-CASSETTE",
                                    "NG BIN " + (on ? "LOCK" : "UNLOCK") + " feedback timeout.");
                                return -1;
                            }

                            return 0;
                        },
                        "LOCK", "UNLOCK")
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-CASSETTE", "BindIoPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette I/O", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindJogPanel()
        {
            try
            {
                if (_OutCassetteUnit == null)
                    return;

                JogAxisItem axisItem = JogAxisItem.Single("AXIS Z", _OutCassetteUnit.OutputLifterZ, AxisUnitConverter.DisplayUnitFor(_OutCassetteUnit.OutputLifterZ), 1.0, "Z+", "Z-").WithControlKind(JogAxisControlKind.Vertical);
                axisItem.StepMoveAsync = (item, direction, speedType, customSpeed, axisStepDistance) =>
                    _OutCassetteUnit.JogStepAsync(_OutCassetteUnit.OutputLifterZ, direction, speedType, customSpeed, axisStepDistance);
                axisItem.ContinuousMoveAsync = (item, direction, speedType, customSpeed) =>
                    _OutCassetteUnit.JogContinuousAsync(_OutCassetteUnit.OutputLifterZ, direction, speedType, customSpeed);
                axisItem.StopAsync = item =>
                    _OutCassetteUnit.StopJogAsync(_OutCassetteUnit.OutputLifterZ);

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
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-CASSETTE", "BindJogPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ParameterGrid_ParameterValueChanged(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                SaveEditedData(e != null && e.Item != null && e.Item.Scope == ParameterGridScope.Recipe);
                RefreshView();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-CASSETTE", "Parameter save failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Parameter Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindEditableLabels()
        {
            try
            {
                AttachAxisEditor(lblRecipeLoadingVal, "LOADING Z", () => _OutCassetteUnit.Recipe.GoodLoaingPosition, v => _OutCassetteUnit.Recipe.GoodLoaingPosition = v, true);
                AttachAxisEditor(lblRecipeUnloadingVal, "UNLOADING Z", () => _OutCassetteUnit.Recipe.GoodUnloadingPosition, v => _OutCassetteUnit.Recipe.GoodUnloadingPosition = v, true);
                AttachAxisEditor(lblRecipeAvoidVal, "READY POSITION", () => _OutCassetteUnit.Recipe.AvoidPosition, v => _OutCassetteUnit.Recipe.AvoidPosition = v, true);
                AttachAxisEditor(lblRecipeFirstSlotVal, "FIRST SLOT", () => _OutCassetteUnit.Recipe.GoodFirstSlotPosition, v => _OutCassetteUnit.Recipe.GoodFirstSlotPosition = v, true);
                AttachAxisEditor(lblRecipeMappingStartVal, "MAPPING START Z", () => _OutCassetteUnit.Recipe.MappingStartPosition, v => _OutCassetteUnit.Recipe.MappingStartPosition = v, true);
                AttachAxisEditor(lblRecipeMappingEndVal, "MAPPING END Z", () => _OutCassetteUnit.Recipe.MappingEndPosition, v => _OutCassetteUnit.Recipe.MappingEndPosition = v, true);
                AttachAxisEditor(lblConfigLoadingOffsetVal, "LOADING OFFSET", () => _OutCassetteUnit.Config.LoadingPositionOffset, v => _OutCassetteUnit.Config.LoadingPositionOffset = v, false);
                AttachAxisEditor(lblConfigUnloadingOffsetVal, "UNLOADING OFFSET", () => _OutCassetteUnit.Config.UnloadingPositionOffset, v => _OutCassetteUnit.Config.UnloadingPositionOffset = v, false);
                AttachAxisEditor(lblConfigSlotPitchVal, "SLOT PITCH", () => _OutCassetteUnit.Config.SlotPitch, v => _OutCassetteUnit.Config.SlotPitch = v, false);
                AttachIntEditor(lblConfigSlotCountVal, "SLOT COUNT", () => _OutCassetteUnit.Config.SlotCount, v =>
                {
                    _OutCassetteUnit.Config.SlotCount = Math.Max(0, v);
                    _OutCassetteUnit.Recipe.EnsureSlotPositionBuffers(_OutCassetteUnit.Config.SlotCount);
                }, false);
                AttachAxisEditor(lblConfigScanVelocityVal, "SCAN/JOG VELOCITY", () => _OutCassetteUnit.Config.ScanVelocity, v => _OutCassetteUnit.Config.ScanVelocity = Math.Max(0.1, v), false, "/s");
                AttachAxisEditor(lblSetupToleranceVal, "IN POSITION TOLERANCE", () => _OutCassetteUnit.OutputLifterZ.Config.InPositionTolerance, v => _OutCassetteUnit.OutputLifterZ.Config.InPositionTolerance = Math.Max(0.0, v), false);
                AttachIntEditor(lblConfigInchVal, "INCH SELECT", () => _OutCassetteUnit.Config.InchSelect, v => _OutCassetteUnit.Config.InchSelect = v, false);
                AttachIntEditor(lblConfigLevelVal, "CASSETTE LEVEL", () => _OutCassetteUnit.Config.SelectedCassetteLevel, v => _OutCassetteUnit.Config.SelectedCassetteLevel = v, false);
                AttachBoolEditor(lblSetupSimulationVal, "SIMULATION MODE", () => _OutCassetteUnit.Setup.IsSimulationMode, v => _OutCassetteUnit.Setup.IsSimulationMode = v, false);
                AttachBoolEditor(lblConfigDryRunVal, "DRY RUN", () => _OutCassetteUnit.Config.bDryRun, v => _OutCassetteUnit.Config.bDryRun = v, false);
                AttachIntEditor(lblWaitScanSettleVal, "SCAN SETTLE TIME (ms)", () => _OutCassetteUnit.Config.ScanSettleTimeMs, v => _OutCassetteUnit.Config.ScanSettleTimeMs = Math.Max(0, v), false);
                AttachIntEditor(lblWaitMoveTimeoutVal, "MOVE TIMEOUT (ms)", () => _OutCassetteUnit.OutputLifterZ.Setup.MoveTimeoutMs, v => _OutCassetteUnit.OutputLifterZ.Setup.MoveTimeoutMs = Math.Max(0, v), false);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Edit Binding", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void AttachAxisEditor(Label label, string name, Func<double> getter, Action<double> setter, bool isRecipeData, string unitSuffix = "")
        {
            try
            {
                label.DoubleClick += (s, e) =>
                {
                    try
                    {
                        if (_OutCassetteUnit == null) return;
                        string unit = AxisUnitConverter.DisplayUnitFor(_OutCassetteUnit.OutputLifterZ) + unitSuffix;
                        string text = Prompt.Show(name + " 값을 입력하세요. (" + unit + ")", FormatNumber(AxisUnitConverter.ToDisplay(getter(), _OutCassetteUnit.OutputLifterZ)));
                        if (text == null) return;
                        double value;
                        if (!TryParseDouble(text, out value))
                            throw new FormatException("숫자 값을 입력해야 합니다.");

                        setter(AxisUnitConverter.FromDisplay(value, _OutCassetteUnit.OutputLifterZ));
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        if (_OutCassetteUnit == null) return;
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        if (_OutCassetteUnit == null) return;
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        if (_OutCassetteUnit == null) return;
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            host.QueueRecipeEditorSave(_OutCassetteUnit, true);
        }

        private void SaveCurrentSettingsData()
        {
            var host = FindHostForm();
            if (host == null)
                throw new InvalidOperationException("Main 화면을 찾을 수 없어 설정을 저장할 수 없습니다.");
            host.QueueRecipeEditorSave(_OutCassetteUnit, false);
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
                if (_OutCassetteUnit == null)
                    return;

                Lang.BindFormat(lblRecipeLoadingVal, "recipeUi.literal", (object)(FormatAxis(_OutCassetteUnit.Recipe.GoodLoaingPosition)));
                Lang.BindFormat(lblRecipeUnloadingVal, "recipeUi.literal", (object)(FormatAxis(_OutCassetteUnit.Recipe.GoodUnloadingPosition)));
                Lang.BindFormat(lblRecipeAvoidVal, "recipeUi.literal", (object)(FormatAxis(_OutCassetteUnit.Recipe.AvoidPosition)));
                Lang.BindFormat(lblRecipeFirstSlotVal, "recipeUi.literal", (object)(FormatAxis(_OutCassetteUnit.Recipe.GoodFirstSlotPosition)));
                Lang.BindFormat(lblRecipeMappingStartVal, "recipeUi.literal", (object)(FormatAxis(_OutCassetteUnit.Recipe.MappingStartPosition)));
                Lang.BindFormat(lblRecipeMappingEndVal, "recipeUi.literal", (object)(FormatAxis(_OutCassetteUnit.Recipe.MappingEndPosition)));
                Lang.BindFormat(lblConfigLoadingOffsetVal, "recipeUi.literal", (object)(FormatAxis(_OutCassetteUnit.Config.LoadingPositionOffset)));
                Lang.BindFormat(lblConfigUnloadingOffsetVal, "recipeUi.literal", (object)(FormatAxis(_OutCassetteUnit.Config.UnloadingPositionOffset)));
                Lang.BindFormat(lblConfigSlotPitchVal, "recipeUi.literal", (object)(FormatAxis(_OutCassetteUnit.Config.SlotPitch)));
                Lang.BindFormat(lblConfigSlotCountVal, "recipeUi.literal", (object)(_OutCassetteUnit.Config.SlotCount.ToString(CultureInfo.InvariantCulture)));
                Lang.BindFormat(lblConfigScanVelocityVal, "recipeUi.literal", (object)(FormatAxis(_OutCassetteUnit.Config.ScanVelocity, "/s")));
                Lang.BindFormat(lblSetupToleranceVal, "recipeUi.literal", (object)(FormatAxis(_OutCassetteUnit.OutputLifterZ.Config.InPositionTolerance)));
                Lang.BindFormat(lblConfigInchVal, "recipeUi.literal", (object)(_OutCassetteUnit.Config.InchSelect.ToString(CultureInfo.InvariantCulture)));
                Lang.BindFormat(lblConfigLevelVal, "recipeUi.literal", (object)(_OutCassetteUnit.Config.SelectedCassetteLevel.ToString(CultureInfo.InvariantCulture)));
                Lang.BindFormat(lblSetupSimulationVal, "recipeUi.literal", (object)(_OutCassetteUnit.Setup.IsSimulationMode.ToString()));
                Lang.BindFormat(lblConfigDryRunVal, "recipeUi.literal", (object)(_OutCassetteUnit.Config.bDryRun.ToString()));
                Lang.BindFormat(lblWaitScanSettleVal, "recipeUi.inputCassetteRecipePage.lblWaitScanSettleVal.text", (object)(_OutCassetteUnit.Config.ScanSettleTimeMs.ToString(CultureInfo.InvariantCulture)));
                Lang.BindFormat(lblWaitMoveTimeoutVal, "recipeUi.inputCassetteRecipePage.lblWaitScanSettleVal.text", (object)(_OutCassetteUnit.OutputLifterZ.Setup.MoveTimeoutMs.ToString(CultureInfo.InvariantCulture)));
                optionParameterGrid.RefreshValues();
                waitParameterGrid.RefreshValues();
                ioCylinderPanel.RefreshStates();
                jogPositionListControl.RefreshState();

                dot8Inch.IsOn = _OutCassetteUnit.IsBinCassettePresentAll(TargetCassette.Good1, 8) ||
                                    _OutCassetteUnit.IsBinCassettePresentAll(TargetCassette.Ng, 8);
                dot12Inch.IsOn = _OutCassetteUnit.IsBinCassettePresentAll(TargetCassette.Good1, 12) ||
                                     _OutCassetteUnit.IsBinCassettePresentAll(TargetCassette.Ng, 12);
                dotProtrusion.IsOn = _OutCassetteUnit.IsBinProtrusionDetected();
                dotMapping.IsOn = _OutCassetteUnit.IsBinMapping();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static bool TryParseDouble(string text, out double value)
        {
            try
            {
                text = (text ?? string.Empty)
                    .Replace("mm/s2", string.Empty)
                    .Replace("um/s2", string.Empty)
                    .Replace("deg/s2", string.Empty)
                    .Replace("mm/s", string.Empty)
                    .Replace("um/s", string.Empty)
                    .Replace("deg/s", string.Empty)
                    .Replace("um", string.Empty)
                    .Replace("mm", string.Empty)
                    .Replace("deg", string.Empty)
                    .Replace("ms", string.Empty)
                    .Trim();
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

        private ParameterGridItem AxisDouble(string displayName, ParameterGridScope scope, Func<double> getter, Action<double> setter, string unitSuffix = "")
        {
            ParameterGridItem item = ParameterGridItem.Double(
                displayName,
                AxisUnitConverter.DisplayUnitFor(_OutCassetteUnit.OutputLifterZ) + unitSuffix,
                scope,
                () => AxisUnitConverter.ToDisplay(getter(), _OutCassetteUnit.OutputLifterZ),
                v => setter(AxisUnitConverter.FromDisplay(v, _OutCassetteUnit.OutputLifterZ)));
            item.UnitGetter = () => AxisUnitConverter.DisplayUnitFor(_OutCassetteUnit.OutputLifterZ) + unitSuffix;
            if (scope == ParameterGridScope.Recipe)
                item.SupportsTeaching = true;   // Recipe scope = 티칭 포지션 → 행에 MOVE/TEACH 버튼 표시
            return item;
        }

        private string FormatAxis(double value, string unitSuffix = "")
        {
            try
            {
                return AxisUnitConverter.FormatDisplay(value, _OutCassetteUnit.OutputLifterZ, "0.###", true) + unitSuffix;
            }
            catch
            {
                return "0 " + AxisUnitConverter.DisplayUnitFor(_OutCassetteUnit != null ? _OutCassetteUnit.OutputLifterZ : null) + unitSuffix;
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Theme", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        // Keep Designer declarations intact; language bindings only affect displayed captions.
        private void InitializeRecipeLanguageBindings()
        {
            Lang.BindKey(this.lblHeader, "recipeUi.outputCassetteRecipePage.lblHeader.text");
            Lang.BindKey(this.grpActions, "recipeUi.forceControlPage.grpActions.text");
            Lang.BindKey(this.grpIo, "recipeUi.frontPickerRecipePage.grpIo.text");
            Lang.BindKey(this.grpWait, "recipeUi.frontPickerRecipePage.grpWait.text");
            Lang.BindKey(this.grpOptions, "recipeUi.frontPickerRecipePage.grpOptions.text");
            Lang.BindKey(this.grpJog, "recipeUi.frontPickerRecipePage.grpJog.text");
            Lang.BindKey(this.grpSpeed, "recipeUi.frontPickerRecipePage.grpSpeed.text");
            Lang.BindKey(this.lbl8Inch, "recipeUi.inputCassetteRecipePage.lbl8Inch.text");
            Lang.BindKey(this.lbl12Inch, "recipeUi.inputCassetteRecipePage.lbl12Inch.text");
            Lang.BindKey(this.lblProtrusion, "recipeUi.inputCassetteRecipePage.lblProtrusion.text");
            Lang.BindKey(this.lblMapping, "recipeUi.inputCassetteRecipePage.lblMapping.text");
            Lang.BindKey(this.lblRecipeLoadingKey, "recipeUi.inputCassetteRecipePage.lblRecipeLoadingKey.text");
            Lang.BindKey(this.lblRecipeLoadingVal, "recipeUi.inputCassetteRecipePage.lblRecipeLoadingVal.text");
            Lang.BindKey(this.lblRecipeUnloadingKey, "recipeUi.inputCassetteRecipePage.lblRecipeUnloadingKey.text");
            Lang.BindKey(this.lblRecipeUnloadingVal, "recipeUi.inputCassetteRecipePage.lblRecipeLoadingVal.text");
            Lang.BindKey(this.lblRecipeAvoidKey, "recipeUi.inputCassetteRecipePage.lblRecipeAvoidKey.text");
            Lang.BindKey(this.lblRecipeAvoidVal, "recipeUi.inputCassetteRecipePage.lblRecipeAvoidVal.text");
            Lang.BindKey(this.lblRecipeFirstSlotKey, "recipeUi.inputCassetteRecipePage.lblRecipeFirstSlotKey.text");
            Lang.BindKey(this.lblRecipeFirstSlotVal, "recipeUi.inputCassetteRecipePage.lblRecipeFirstSlotVal.text");
            Lang.BindKey(this.lblRecipeMappingStartKey, "recipeUi.inputCassetteRecipePage.lblRecipeMappingStartKey.text");
            Lang.BindKey(this.lblRecipeMappingStartVal, "recipeUi.inputCassetteRecipePage.lblRecipeMappingStartVal.text");
            Lang.BindKey(this.lblRecipeMappingEndKey, "recipeUi.inputCassetteRecipePage.lblRecipeMappingEndKey.text");
            Lang.BindKey(this.lblRecipeMappingEndVal, "recipeUi.inputCassetteRecipePage.lblRecipeMappingEndVal.text");
            Lang.BindKey(this.lblConfigLoadingOffsetKey, "recipeUi.inputCassetteRecipePage.lblConfigLoadingOffsetKey.text");
            Lang.BindKey(this.lblConfigLoadingOffsetVal, "recipeUi.inputCassetteRecipePage.lblRecipeAvoidVal.text");
            Lang.BindKey(this.lblConfigUnloadingOffsetKey, "recipeUi.inputCassetteRecipePage.lblConfigUnloadingOffsetKey.text");
            Lang.BindKey(this.lblConfigUnloadingOffsetVal, "recipeUi.inputCassetteRecipePage.lblRecipeAvoidVal.text");
            Lang.BindKey(this.lblConfigSlotPitchKey, "recipeUi.inputCassetteRecipePage.lblConfigSlotPitchKey.text");
            Lang.BindKey(this.lblConfigSlotPitchVal, "recipeUi.inputCassetteRecipePage.lblRecipeMappingStartVal.text");
            Lang.BindKey(this.lblConfigSlotCountKey, "recipeUi.inputCassetteRecipePage.lblConfigSlotCountKey.text");
            Lang.BindKey(this.lblConfigScanVelocityKey, "recipeUi.inputCassetteRecipePage.lblConfigScanVelocityKey.text");
            Lang.BindKey(this.lblConfigScanVelocityVal, "recipeUi.inputCassetteRecipePage.lblConfigScanVelocityVal.text");
            Lang.BindKey(this.lblSetupToleranceKey, "recipeUi.inputCassetteRecipePage.lblSetupToleranceKey.text");
            Lang.BindKey(this.lblSetupToleranceVal, "recipeUi.inputCassetteRecipePage.lblSetupToleranceVal.text");
            Lang.BindKey(this.lblConfigInchKey, "recipeUi.inputCassetteRecipePage.lblConfigInchKey.text");
            Lang.BindKey(this.lblConfigLevelKey, "recipeUi.inputCassetteRecipePage.lblConfigLevelKey.text");
            Lang.BindKey(this.lblSetupSimulationKey, "recipeUi.inputCassetteRecipePage.lblSetupSimulationKey.text");
            Lang.BindKey(this.lblSetupSimulationVal, "recipeUi.inputCassetteRecipePage.lblSetupSimulationVal.text");
            Lang.BindKey(this.lblConfigDryRunKey, "recipeUi.inputCassetteRecipePage.lblConfigDryRunKey.text");
            Lang.BindKey(this.lblConfigDryRunVal, "recipeUi.inputCassetteRecipePage.lblSetupSimulationVal.text");
            Lang.BindKey(this.lblWaitScanSettleKey, "recipeUi.inputCassetteRecipePage.lblWaitScanSettleKey.text");
            Lang.BindKey(this.lblWaitScanSettleVal, "recipeUi.inputCassetteRecipePage.lblWaitScanSettleVal.state2");
            Lang.BindKey(this.lblWaitMoveTimeoutKey, "recipeUi.inputCassetteRecipePage.lblWaitMoveTimeoutKey.text");
            Lang.BindKey(this.lblWaitMoveTimeoutVal, "recipeUi.inputCassetteRecipePage.lblWaitMoveTimeoutVal.text");
        }
    }
}
