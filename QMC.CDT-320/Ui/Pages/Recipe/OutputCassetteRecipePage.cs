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
using QMC.Common.IO;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>Output Cassette 레시피에서 OutCassetteUnit을 조작하는 화면입니다.</summary>
    public partial class OutputCassetteRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private OutputCassetteUnit _OutCassetteUnit;
        private readonly Timer _refreshTimer = new Timer();
        private readonly ToolTip _toolTip = new ToolTip();
        /// <summary>OutputCassetteRecipePage를 생성합니다.</summary>
        public OutputCassetteRecipePage()
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

                grpIo.ContextMenuStrip = new ContextMenuStrip();
                grpIo.ContextMenuStrip.Items.Add("Output cassette DI 상태를 다시 읽습니다.", null, IoRefresh_Click);

                _toolTip.SetToolTip(lblRecipeLoadingVal, "더블 클릭하면 값을 um 단위로 변경합니다.");
                _toolTip.SetToolTip(lblRecipeUnloadingVal, "더블 클릭하면 값을 um 단위로 변경합니다.");
                _toolTip.SetToolTip(lblConfigSlotCountVal, "더블 클릭하면 슬롯 개수를 변경하고 SlotPosition 버퍼를 다시 맞춥니다.");
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
                btnGoodLoadingMove.Enabled = enabled;
                btnGoodUnloadingMove.Enabled = enabled;
                btnGoodSlotStartMove.Enabled = enabled;
                btnGoodSlotEndMove.Enabled = enabled;
                btnNgLoadingMove.Enabled = enabled;
                btnNgUnloadingMove.Enabled = enabled;
                btnNgSlotStartMove.Enabled = enabled;
                btnNgSlotEndMove.Enabled = enabled;
                btnReadyMove.Enabled = enabled;

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
                await RunSafeAsync(async () =>
                {
                    await _OutCassetteUnit.MoveBinLifterZ(target, IsFineMove());
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
                await RunSafeAsync(async () =>
                {
                    double target = _OutCassetteUnit.CalculateBinCassetteSlotTargetPosition(cassette, 0) + offset;
                    await _OutCassetteUnit.MoveBinLifterZ(target, IsFineMove());
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

        private async Task RunSafeAsync(Func<Task<int>> action, string actionName)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                int result = await action();
                if (result != 0)
                    QMC.Common.MessageDialog.Show(this, actionName + " 실패", "Output Cassette", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-CASSETTE", "BindParameterGridMenus failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Cassette Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                if (string.Equals(item.Key, "READY POSITION", StringComparison.OrdinalIgnoreCase))
                    return "Avoid";
                if (string.Equals(item.Key, "GOOD LOADING Z", StringComparison.OrdinalIgnoreCase))
                    return "GoodLoading";
                if (string.Equals(item.Key, "GOOD UNLOADING Z", StringComparison.OrdinalIgnoreCase))
                    return "GoodUnloading";
                if (string.Equals(item.Key, "GOOD FIRST SLOT", StringComparison.OrdinalIgnoreCase))
                    return "GoodFirstSlot";
                if (string.Equals(item.Key, "NG LOADING Z", StringComparison.OrdinalIgnoreCase))
                    return "NgLoading";
                if (string.Equals(item.Key, "NG UNLOADING Z", StringComparison.OrdinalIgnoreCase))
                    return "NgUnloading";
                if (string.Equals(item.Key, "NG FIRST SLOT", StringComparison.OrdinalIgnoreCase))
                    return "NgFirstSlot";
                if (string.Equals(item.Key, "MAPPING START Z", StringComparison.OrdinalIgnoreCase))
                    return "MappingStart";
                if (string.Equals(item.Key, "MAPPING END Z", StringComparison.OrdinalIgnoreCase))
                    return "MappingEnd";

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
                    ParameterGridItem.Micron("READY POSITION", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.AvoidPosition, v => _OutCassetteUnit.Recipe.AvoidPosition = v),
                    ParameterGridItem.Micron("GOOD LOADING Z POSITON", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.GoodLoaingPosition, v => _OutCassetteUnit.Recipe.GoodLoaingPosition = v),
                    ParameterGridItem.Micron("GOOD UNLOADING Z POSITON", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.GoodUnloadingPosition, v => _OutCassetteUnit.Recipe.GoodUnloadingPosition = v),
                    ParameterGridItem.Micron("GOOD FIRST SLOT POSITON", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.GoodFirstSlotPosition, v => _OutCassetteUnit.Recipe.GoodFirstSlotPosition = v),
                    ParameterGridItem.Micron("NG LOADING Z POSITON", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.NGLoaingPosition, v => _OutCassetteUnit.Recipe.NGLoaingPosition = v),
                    ParameterGridItem.Micron("NG UNLOADING Z POSITON", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.NGUnloadingPosition, v => _OutCassetteUnit.Recipe.NGUnloadingPosition = v),
                    ParameterGridItem.Micron("NG FIRST SLOT POSITON", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.NGFirstSlotPosition, v => _OutCassetteUnit.Recipe.NGFirstSlotPosition = v),
                    ParameterGridItem.Micron("MAPPING START Z POSITON", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.MappingStartPosition, v => _OutCassetteUnit.Recipe.MappingStartPosition = v),
                    ParameterGridItem.Micron("MAPPING END Z POSITON", ParameterGridScope.Recipe, () => _OutCassetteUnit.Recipe.MappingEndPosition, v => _OutCassetteUnit.Recipe.MappingEndPosition = v),
                    ParameterGridItem.Micron("LOADING OFFSET", ParameterGridScope.Config, () => _OutCassetteUnit.Config.LoadingPositionOffset, v => _OutCassetteUnit.Config.LoadingPositionOffset = v),
                    ParameterGridItem.Micron("UNLOADING OFFSET", ParameterGridScope.Config, () => _OutCassetteUnit.Config.UnloadingPositionOffset, v => _OutCassetteUnit.Config.UnloadingPositionOffset = v),
                    ParameterGridItem.Micron("LEVEL 2 OFFSET", ParameterGridScope.Config, () => _OutCassetteUnit.Config.Level2PositionOffset, v => _OutCassetteUnit.Config.Level2PositionOffset = Math.Max(0.0, v)),
                    ParameterGridItem.Micron("GOOD/NG OFFSET", ParameterGridScope.Config, () => _OutCassetteUnit.Config.GOODNGPositionOffset, v => _OutCassetteUnit.Config.GOODNGPositionOffset = v),
                    ParameterGridItem.Micron("SLOT PITCH", ParameterGridScope.Config, () => _OutCassetteUnit.Config.SlotPitch, v => _OutCassetteUnit.Config.SlotPitch = Math.Max(0.0, v)),
                    ParameterGridItem.Int("SLOT COUNT", "ea", ParameterGridScope.Config, () => _OutCassetteUnit.Config.SlotCount, v =>
                    {
                        _OutCassetteUnit.Config.SlotCount = Math.Max(0, v);
                        _OutCassetteUnit.Recipe.EnsureSlotPositionBuffers(_OutCassetteUnit.Config.SlotCount);
                    }),
                    ParameterGridItem.Double("SCAN/JOG VELOCITY", "mm/s", ParameterGridScope.Config, () => _OutCassetteUnit.Config.ScanVelocity, v => _OutCassetteUnit.Config.ScanVelocity = Math.Max(0.1, v)),
                    ParameterGridItem.Micron("IN POSITION TOL.", ParameterGridScope.Config, () => _OutCassetteUnit.OutputLifterZ.Config.InPositionTolerance, v => _OutCassetteUnit.OutputLifterZ.Config.InPositionTolerance = Math.Max(0.0, v)),
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
                    ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => _OutCassetteUnit.Config.bDryRun, v => _OutCassetteUnit.Config.bDryRun = v)
                });

                waitParameterGrid.SetItems(new[]
                {
                    ParameterGridItem.Int("SCAN SETTLE TIME", "ms", ParameterGridScope.Config, () => _OutCassetteUnit.Config.ScanSettleTimeMs, v => _OutCassetteUnit.Config.ScanSettleTimeMs = Math.Max(0, v)),
                    ParameterGridItem.Int("MOVE TIMEOUT", "ms", ParameterGridScope.Setup, () => _OutCassetteUnit.OutputLifterZ.Setup.MoveTimeoutMs, v => _OutCassetteUnit.OutputLifterZ.Setup.MoveTimeoutMs = Math.Max(0, v))
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

                ioCylinderPanel.SetItems(new[]
                {
                    IoCylinderItem.Input("GOOD BIN 8 INCH CASSETTE", () => _OutCassetteUnit.IsGoodBin(8) && !_OutCassetteUnit.IsNgBin(8) && !_OutCassetteUnit.IsNgBin(12) && !_OutCassetteUnit.IsGoodBin(12)),
                    IoCylinderItem.Input("GOOD BIN 12 INCH CASSETTE", () => _OutCassetteUnit.IsGoodBin(12) && !_OutCassetteUnit.IsNgBin(12) && !_OutCassetteUnit.IsNgBin(8) && !_OutCassetteUnit.IsGoodBin(8)),
                    IoCylinderItem.Input("NG BIN CASSETTE BW CASSETTE", () => _OutCassetteUnit.IsNgBinBW()),
                    IoCylinderItem.Input("NG BIN CASSETTE LOCK CASSETTE", () => _OutCassetteUnit.IsNgBinLock()),
                    IoCylinderItem.Input("NG BIN 8 INCH CASSETTE", () => !_OutCassetteUnit.IsGoodBin(8) && _OutCassetteUnit.IsNgBin(8) && !_OutCassetteUnit.IsNgBin(12) && !_OutCassetteUnit.IsGoodBin(12)),
                    IoCylinderItem.Input("NG BIN 12 INCH CASSETTE", () => !_OutCassetteUnit.IsGoodBin(12) && _OutCassetteUnit.IsNgBin(12) && !_OutCassetteUnit.IsNgBin(8) && !_OutCassetteUnit.IsGoodBin(8)),
                    IoCylinderItem.Input("BIN RING JUT CHECK", () => _OutCassetteUnit.IsBinProtrusionDetectionSensor()),
                    IoCylinderItem.Input("BIN MAPPING", () => _OutCassetteUnit.IsBinMapping()),

                    // ===== OUTPUT (DO) =====
                    IoCylinderItem.Output("NG BIN CASSETTE LOCK", () => _OutCassetteUnit.NgBinCassetteLockOut != null && _OutCassetteUnit.NgBinCassetteLockOut.IsOn, on => _OutCassetteUnit.SetNgBinCassetteLock(on)),
                    IoCylinderItem.Output("NG BIN CASSETTE UNLOCK", () => _OutCassetteUnit.NgBinCassetteUnlockOut != null && _OutCassetteUnit.NgBinCassetteUnlockOut.IsOn, on => _OutCassetteUnit.SetNgBinCassetteUnlock(on)),
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

                JogAxisItem axisItem = JogAxisItem.Single("AXIS Z", _OutCassetteUnit.OutputLifterZ, "um", 1000.0, "Z+", "Z-").WithControlKind(JogAxisControlKind.Vertical);
                axisItem.StepMoveAsync = async (item, direction, speedType, customSpeed, axisStepDistance) =>
                {
                    try
                    {
                        double target = _OutCassetteUnit.OutputLifterZ.ActualPosition + (direction * axisStepDistance);
                        await _OutCassetteUnit.MoveBinLifterZ(target, IsFineMove());
                        bool done = await _OutCassetteUnit.WaitBinLifterZMoveDone(_OutCassetteUnit.OutputLifterZ.Setup.MoveTimeoutMs);
                        return done ? 0 : -1;
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
                        _OutCassetteUnit.ManualMoveBinLifterZJog(direction, customSpeed);
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
                        _OutCassetteUnit.ManualStopBinLifterZ();
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
                AttachMicronEditor(lblRecipeLoadingVal, "LOADING Z", () => _OutCassetteUnit.Recipe.GoodLoaingPosition, v => _OutCassetteUnit.Recipe.GoodLoaingPosition = v, true);
                AttachMicronEditor(lblRecipeUnloadingVal, "UNLOADING Z", () => _OutCassetteUnit.Recipe.GoodUnloadingPosition, v => _OutCassetteUnit.Recipe.GoodUnloadingPosition = v, true);
                AttachMicronEditor(lblRecipeAvoidVal, "READY POSITION", () => _OutCassetteUnit.Recipe.AvoidPosition, v => _OutCassetteUnit.Recipe.AvoidPosition = v, true);
                AttachMicronEditor(lblRecipeFirstSlotVal, "FIRST SLOT", () => _OutCassetteUnit.Recipe.GoodFirstSlotPosition, v => _OutCassetteUnit.Recipe.GoodFirstSlotPosition = v, true);
                AttachMicronEditor(lblRecipeMappingStartVal, "MAPPING START Z", () => _OutCassetteUnit.Recipe.MappingStartPosition, v => _OutCassetteUnit.Recipe.MappingStartPosition = v, true);
                AttachMicronEditor(lblRecipeMappingEndVal, "MAPPING END Z", () => _OutCassetteUnit.Recipe.MappingEndPosition, v => _OutCassetteUnit.Recipe.MappingEndPosition = v, true);
                AttachMicronEditor(lblConfigLoadingOffsetVal, "LOADING OFFSET", () => _OutCassetteUnit.Config.LoadingPositionOffset, v => _OutCassetteUnit.Config.LoadingPositionOffset = v, false);
                AttachMicronEditor(lblConfigUnloadingOffsetVal, "UNLOADING OFFSET", () => _OutCassetteUnit.Config.UnloadingPositionOffset, v => _OutCassetteUnit.Config.UnloadingPositionOffset = v, false);
                AttachMicronEditor(lblConfigSlotPitchVal, "SLOT PITCH", () => _OutCassetteUnit.Config.SlotPitch, v => _OutCassetteUnit.Config.SlotPitch = v, false);
                AttachIntEditor(lblConfigSlotCountVal, "SLOT COUNT", () => _OutCassetteUnit.Config.SlotCount, v =>
                {
                    _OutCassetteUnit.Config.SlotCount = Math.Max(0, v);
                    _OutCassetteUnit.Recipe.EnsureSlotPositionBuffers(_OutCassetteUnit.Config.SlotCount);
                }, false);
                AttachDoubleEditor(lblConfigScanVelocityVal, "SCAN/JOG VELOCITY (mm/s)", () => _OutCassetteUnit.Config.ScanVelocity, v => _OutCassetteUnit.Config.ScanVelocity = Math.Max(0.1, v), "mm/s", false);
                AttachMicronEditor(lblSetupToleranceVal, "IN POSITION TOLERANCE", () => _OutCassetteUnit.OutputLifterZ.Config.InPositionTolerance, v => _OutCassetteUnit.OutputLifterZ.Config.InPositionTolerance = Math.Max(0.0, v), false);
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

        private void AttachMicronEditor(Label label, string name, Func<double> getter, Action<double> setter, bool isRecipeData)
        {
            try
            {
                label.DoubleClick += (s, e) =>
                {
                    try
                    {
                        if (_OutCassetteUnit == null) return;
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
                if (_OutCassetteUnit == null)
                    return;

                lblRecipeLoadingVal.Text = FormatUm(_OutCassetteUnit.Recipe.GoodLoaingPosition);
                lblRecipeUnloadingVal.Text = FormatUm(_OutCassetteUnit.Recipe.GoodUnloadingPosition);
                lblRecipeAvoidVal.Text = FormatUm(_OutCassetteUnit.Recipe.AvoidPosition);
                lblRecipeFirstSlotVal.Text = FormatUm(_OutCassetteUnit.Recipe.GoodFirstSlotPosition);
                lblRecipeMappingStartVal.Text = FormatUm(_OutCassetteUnit.Recipe.MappingStartPosition);
                lblRecipeMappingEndVal.Text = FormatUm(_OutCassetteUnit.Recipe.MappingEndPosition);
                lblConfigLoadingOffsetVal.Text = FormatUm(_OutCassetteUnit.Config.LoadingPositionOffset);
                lblConfigUnloadingOffsetVal.Text = FormatUm(_OutCassetteUnit.Config.UnloadingPositionOffset);
                lblConfigSlotPitchVal.Text = FormatUm(_OutCassetteUnit.Config.SlotPitch);
                lblConfigSlotCountVal.Text = _OutCassetteUnit.Config.SlotCount.ToString(CultureInfo.InvariantCulture);
                lblConfigScanVelocityVal.Text = FormatNumber(_OutCassetteUnit.Config.ScanVelocity) + " mm/s";
                lblSetupToleranceVal.Text = FormatUm(_OutCassetteUnit.OutputLifterZ.Config.InPositionTolerance);
                lblConfigInchVal.Text = _OutCassetteUnit.Config.InchSelect.ToString(CultureInfo.InvariantCulture);
                lblConfigLevelVal.Text = _OutCassetteUnit.Config.SelectedCassetteLevel.ToString(CultureInfo.InvariantCulture);
                lblSetupSimulationVal.Text = _OutCassetteUnit.Setup.IsSimulationMode.ToString();
                lblConfigDryRunVal.Text = _OutCassetteUnit.Config.bDryRun.ToString();
                lblWaitScanSettleVal.Text = _OutCassetteUnit.Config.ScanSettleTimeMs.ToString(CultureInfo.InvariantCulture) + " ms";
                lblWaitMoveTimeoutVal.Text = _OutCassetteUnit.OutputLifterZ.Setup.MoveTimeoutMs.ToString(CultureInfo.InvariantCulture) + " ms";
                optionParameterGrid.RefreshValues();
                waitParameterGrid.RefreshValues();
                ioCylinderPanel.RefreshStates();
                jogPositionListControl.RefreshState();

                dot8Inch.IsOn = _OutCassetteUnit.IsGoodBin(8) || _OutCassetteUnit.IsNgBin(8);
                dot12Inch.IsOn = _OutCassetteUnit.IsGoodBin(12) || _OutCassetteUnit.IsNgBin(12);
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

                Color groupHeaderBg = Color.FromArgb(245, 245, 245);
                Color groupHeaderFg = Color.FromArgb(64, 64, 64);
                Font actionFont = new Font("Malgun Gothic", 8F, FontStyle.Bold);
                Font groupFont = new Font("Malgun Gothic", 8.5F, FontStyle.Bold);

                foreach (var buttonControl in new[] { btnGoodLoadingMove, btnGoodUnloadingMove, btnGoodSlotStartMove, btnGoodSlotEndMove, btnNgLoadingMove, btnNgUnloadingMove, btnNgSlotStartMove, btnNgSlotEndMove, btnReadyMove })
                {
                    buttonControl.BackColor = actionButtonColor;
                    buttonControl.ForeColor = Color.White;
                    buttonControl.Font = actionFont;
                }

                lblGoodGroup.BackColor = groupHeaderBg;
                lblGoodGroup.ForeColor = groupHeaderFg;
                lblGoodGroup.Font = groupFont;
                lblNgGroup.BackColor = groupHeaderBg;
                lblNgGroup.ForeColor = groupHeaderFg;
                lblNgGroup.Font = groupFont;
                lblCommonGroup.BackColor = groupHeaderBg;
                lblCommonGroup.ForeColor = groupHeaderFg;
                lblCommonGroup.Font = groupFont;

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


    }
}
