using QMC.CDT_320.Ui.Localization;
using QMC.CDT320;
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
        private InputCassetteUnit _waferCassette;
        private readonly Timer _refreshTimer = new Timer();
        private readonly ToolTip _toolTip = new ToolTip();
        private bool _isJogging;
        private readonly Color _jogButtonNormalColor = Color.FromArgb(240, 240, 240);
        private readonly Color _jogButtonActiveColor = Color.FromArgb(255, 242, 153);

        /// <summary>InputCassetteRecipePage를 생성합니다.</summary>
        public InputCassetteRecipePage()
        {
            try
            {
                InitializeComponent();
                ApplyRecipeTheme();
                ConfigureRuntimeBehavior();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Input Cassette", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                RefreshView();
                _refreshTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                StopJog(true);
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
                rdoFine.Checked = true;
                rdoCoarse.Checked = false;
                rdoCurrent.Checked = false;

                BindEditableLabels();
                BindTeachingMenus();

                grpIo.ContextMenuStrip = new ContextMenuStrip();
                grpIo.ContextMenuStrip.Items.Add("Input cassette DI 상태를 다시 읽습니다.", null, IoRefresh_Click);

                _toolTip.SetToolTip(lblRecipeLoadingVal, "더블 클릭하면 값을 um 단위로 변경합니다.");
                _toolTip.SetToolTip(lblRecipeUnloadingVal, "더블 클릭하면 값을 um 단위로 변경합니다.");
                _toolTip.SetToolTip(lblConfigSlotCountVal, "더블 클릭하면 슬롯 개수를 변경하고 SlotPosition 버퍼를 다시 맞춥니다.");
                _toolTip.SetToolTip(btnJogPlus, "누르고 있는 동안 Z+ 방향으로 조그 이동합니다.");
                _toolTip.SetToolTip(btnJogMinus, "누르고 있는 동안 Z- 방향으로 조그 이동합니다.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Configure", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                _waferCassette = machine != null && machine.InputLoader != null ? machine.InputLoader.WaferCassette : null;

                if (_waferCassette != null)
                    _waferCassette.EnsureSlotPositionBuffer();

                SetEnabledState(_waferCassette != null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Resolve", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                grpJogMove.Enabled = enabled;
                grpJogMode.Enabled = enabled;
                jogPadPanel.Enabled = enabled;
                trkSpeed.Enabled = enabled;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Enable", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnLoadingMove_Click(object sender, EventArgs e)
        {
            try
            {
                if (_waferCassette == null) return;
                await MoveToTarget("LOADING Z", _waferCassette.Recipe.LoaingPosition);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnUnloadingMove_Click(object sender, EventArgs e)
        {
            try
            {
                if (_waferCassette == null) return;
                await MoveToTarget("UNLOADING Z", _waferCassette.Recipe.UnloadingPosition);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnReadyMove_Click(object sender, EventArgs e)
        {
            try
            {
                if (_waferCassette == null) return;
                await MoveToTarget("READY POSITION", _waferCassette.Recipe.AvoidPosition);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnSlotLoadingMove_Click(object sender, EventArgs e)
        {
            try
            {
                await MoveSlotWithOffset(_waferCassette != null ? _waferCassette.Config.LoadingPositionOffset : 0.0, "Input cassette slot loading move");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Slot Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async void btnSlotUnloadingMove_Click(object sender, EventArgs e)
        {
            try
            {
                await MoveSlotWithOffset(_waferCassette != null ? _waferCassette.Config.UnloadingPositionOffset : 0.0, "Input cassette slot unloading move");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Slot Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void trkSpeed_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                lblSpeedValue.Text = trkSpeed.Value + "%";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Jog Speed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void btnStep1_Click(object sender, EventArgs e)
        {
            try
            {
                numStepDistance.Value = 1000;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void btnStep01_Click(object sender, EventArgs e)
        {
            try
            {
                numStepDistance.Value = 100;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void btnStep001_Click(object sender, EventArgs e)
        {
            try
            {
                numStepDistance.Value = 10;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void btnStep0001_Click(object sender, EventArgs e)
        {
            try
            {
                numStepDistance.Value = 1;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void btnStepZero_Click(object sender, EventArgs e)
        {
            try
            {
                numStepDistance.Value = numStepDistance.Minimum;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private async void btnJogPlus_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                SetJogButtonActive(btnJogPlus);
                await StartJog(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Jog +", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (rdoStep.Checked)
                    ResetJogButtonColors();
            }
        }

        private async void btnJogMinus_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                SetJogButtonActive(btnJogMinus);
                await StartJog(-1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Jog -", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (rdoStep.Checked)
                    ResetJogButtonColors();
            }
        }

        private void btnJog_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                StopJog(false);
                ResetJogButtonColors();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void btnJog_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                StopJog(false);
                ResetJogButtonColors();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void btnJogStop_Click(object sender, EventArgs e)
        {
            try
            {
                StopJog(true);
                ResetJogButtonColors();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private async Task StartJog(int direction)
        {
            try
            {
                if (_waferCassette == null) 
                    return;

                StopJog(true);

                if (rdoStep.Checked)
                {
                    double stepUm = Convert.ToDouble(numStepDistance.Value, CultureInfo.InvariantCulture);
                    double stepMm = stepUm / 1000;
                    double target = _waferCassette.WaferLifterZ.ActualPosition + (direction * stepMm);

                    await RunSafeAsync(async () =>
                    {
                        await _waferCassette.MoveWaferLifterZ(target, GetJogSpeedType(), CurrentJogSpeed());
                        return await _waferCassette.WaitWaferLifterZMoveDone(_waferCassette.Config.ElevatorMoveTimeoutMs);
                    }, "Input cassette jog step");
                    return;
                }

                _isJogging = true;
                _waferCassette.ManualMoveWaferLifterZJog(direction, GetJogSpeedType(), CurrentJogSpeed());
                RefreshView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Jog Start", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void StopJog(bool force)
        {
            try
            {
                if (_waferCassette == null) return;
                if (!_isJogging && !force) return;

                bool wasJogging = _isJogging;
                _waferCassette.ManualStopWaferLifterZ();
                _isJogging = false;
                if (wasJogging)
                    RefreshView();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void SetJogButtonActive(Button activeButton)
        {
            try
            {
                ResetJogButtonColors();
                activeButton.BackColor = _jogButtonActiveColor;
                activeButton.UseVisualStyleBackColor = false;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ResetJogButtonColors()
        {
            try
            {
                foreach (var button in new[] { btnJogPlus, btnJogMinus })
                {
                    button.BackColor = _jogButtonNormalColor;
                    button.UseVisualStyleBackColor = false;
                    button.Invalidate();
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private async Task MoveToTarget(string actionName, double target)
        {
            try
            {
                if (_waferCassette == null) return;
                await RunSafeAsync(async () =>
                {
                    await _waferCassette.MoveWaferLifterZ(target, IsFineMove());
                    return await _waferCassette.WaitWaferLifterZMoveDone(_waferCassette.Config.ElevatorMoveTimeoutMs);
                }, actionName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, actionName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async Task MoveSlotWithOffset(double offset, string actionName)
        {
            try
            {
                if (_waferCassette == null) return;
                await RunSafeAsync(async () =>
                {
                    double target = _waferCassette.CalculateWaferCassetteSlotTargetPosition(0) + offset;
                    await _waferCassette.MoveWaferLifterZ(target, IsFineMove());
                    return await _waferCassette.WaitWaferLifterZMoveDone(_waferCassette.Config.ElevatorMoveTimeoutMs);
                }, actionName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, actionName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async Task RunSafeAsync(Func<Task<bool>> action, string actionName)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                bool ok = await action();
                if (!ok)
                    MessageBox.Show(this, actionName + " 실패", "Input Cassette", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, actionName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(this, ex.Message, "Input Cassette Teach Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    RefreshView();
                });

                label.ContextMenuStrip = menu;
                label.Cursor = Cursors.Hand;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Teach Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async Task MoveByPositionName(string positionName)
        {
            try
            {
                if (_waferCassette == null) return;

                if (string.Equals(positionName, "Loading", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("LOADING Z", _waferCassette.Recipe.LoaingPosition);
                else if (string.Equals(positionName, "Unloading", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("UNLOADING Z", _waferCassette.Recipe.UnloadingPosition);
                else if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("READY POSITION", _waferCassette.Recipe.AvoidPosition);
                else if (string.Equals(positionName, "FirstSlot", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("FIRST SLOT", _waferCassette.Recipe.FirstSlotPosition);
                else if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("MAPPING START Z", _waferCassette.Recipe.MappingStartPosition);
                else if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase))
                    await MoveToTarget("MAPPING END Z", _waferCassette.Recipe.MappingEndPosition);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void TeachPosition(string positionName)
        {
            try
            {
                if (_waferCassette == null) return;

                if (string.Equals(positionName, "Loading", StringComparison.OrdinalIgnoreCase))
                    _waferCassette.Recipe.LoaingPosition = _waferCassette.WaferLifterZ.ActualPosition;
                else if (string.Equals(positionName, "Unloading", StringComparison.OrdinalIgnoreCase))
                    _waferCassette.Recipe.UnloadingPosition = _waferCassette.WaferLifterZ.ActualPosition;
                else if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase))
                    _waferCassette.TeachWaferLifterZAvoidPosition();
                else if (string.Equals(positionName, "FirstSlot", StringComparison.OrdinalIgnoreCase))
                    _waferCassette.TeachWaferLifterZPosition("FirstSlot");
                else if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase))
                    _waferCassette.TeachWaferLifterZMappingStartPosition();
                else if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase))
                    _waferCassette.TeachWaferLifterZMappingEndPosition();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Teach", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindEditableLabels()
        {
            try
            {
                AttachMicronEditor(lblRecipeLoadingVal, "LOADING Z", () => _waferCassette.Recipe.LoaingPosition, v => _waferCassette.Recipe.LoaingPosition = v);
                AttachMicronEditor(lblRecipeUnloadingVal, "UNLOADING Z", () => _waferCassette.Recipe.UnloadingPosition, v => _waferCassette.Recipe.UnloadingPosition = v);
                AttachMicronEditor(lblRecipeAvoidVal, "READY POSITION", () => _waferCassette.Recipe.AvoidPosition, v => _waferCassette.Recipe.AvoidPosition = v);
                AttachMicronEditor(lblRecipeFirstSlotVal, "FIRST SLOT", () => _waferCassette.Recipe.FirstSlotPosition, v => _waferCassette.Recipe.FirstSlotPosition = v);
                AttachMicronEditor(lblRecipeMappingStartVal, "MAPPING START Z", () => _waferCassette.Recipe.MappingStartPosition, v => _waferCassette.Recipe.MappingStartPosition = v);
                AttachMicronEditor(lblRecipeMappingEndVal, "MAPPING END Z", () => _waferCassette.Recipe.MappingEndPosition, v => _waferCassette.Recipe.MappingEndPosition = v);
                AttachMicronEditor(lblConfigLoadingOffsetVal, "LOADING OFFSET", () => _waferCassette.Config.LoadingPositionOffset, v => _waferCassette.Config.LoadingPositionOffset = v);
                AttachMicronEditor(lblConfigUnloadingOffsetVal, "UNLOADING OFFSET", () => _waferCassette.Config.UnloadingPositionOffset, v => _waferCassette.Config.UnloadingPositionOffset = v);
                AttachMicronEditor(lblConfigSlotPitchVal, "SLOT PITCH", () => _waferCassette.Config.SlotPitch, v => _waferCassette.Config.SlotPitch = v);
                AttachIntEditor(lblConfigSlotCountVal, "SLOT COUNT", () => _waferCassette.Config.SlotCount, v =>
                {
                    _waferCassette.Config.SlotCount = Math.Max(0, v);
                    _waferCassette.EnsureSlotPositionBuffer();
                });
                AttachDoubleEditor(lblConfigScanVelocityVal, "SCAN/JOG VELOCITY (mm/s)", () => _waferCassette.Config.ScanVelocity, v => _waferCassette.Config.ScanVelocity = Math.Max(0.1, v), "mm/s");
                AttachMicronEditor(lblSetupToleranceVal, "IN POSITION TOLERANCE", () => _waferCassette.Setup.InPositionTolerance, v => _waferCassette.Setup.InPositionTolerance = Math.Max(0.0, v));
                AttachIntEditor(lblConfigInchVal, "INCH SELECT", () => _waferCassette.Config.InchSelect, v => _waferCassette.Config.InchSelect = v);
                AttachIntEditor(lblConfigLevelVal, "CASSETTE LEVEL", () => _waferCassette.Config.SelectedCassetteLevel, v => _waferCassette.Config.SelectedCassetteLevel = v);
                AttachBoolEditor(lblSetupSimulationVal, "SIMULATION MODE", () => _waferCassette.Setup.IsSimulationMode, v => _waferCassette.Setup.IsSimulationMode = v);
                AttachBoolEditor(lblConfigDryRunVal, "DRY RUN", () => _waferCassette.Config.bDryRun, v => _waferCassette.Config.bDryRun = v);
                AttachIntEditor(lblWaitScanSettleVal, "SCAN SETTLE TIME (ms)", () => _waferCassette.Config.ScanSettleTimeMs, v => _waferCassette.Config.ScanSettleTimeMs = Math.Max(0, v));
                AttachIntEditor(lblWaitMoveTimeoutVal, "MOVE TIMEOUT (ms)", () => _waferCassette.Config.ElevatorMoveTimeoutMs, v => _waferCassette.Config.ElevatorMoveTimeoutMs = Math.Max(0, v));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Edit Binding", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void AttachMicronEditor(Label label, string name, Func<double> getter, Action<double> setter)
        {
            try
            {
                label.DoubleClick += (s, e) =>
                {
                    try
                    {
                        if (_waferCassette == null) return;
                        string text = Prompt.Show(name + " 값을 입력하세요. (um)", FormatNumber(getter() * 1000.0));
                        if (text == null) return;
                        double value;
                        if (!TryParseDouble(text, out value))
                            throw new FormatException("숫자 값을 입력해야 합니다.");

                        setter(value / 1000.0);
                        RefreshView();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void AttachDoubleEditor(Label label, string name, Func<double> getter, Action<double> setter, string suffix)
        {
            try
            {
                label.DoubleClick += (s, e) =>
                {
                    try
                    {
                        if (_waferCassette == null) return;
                        string text = Prompt.Show(name + " 값을 입력하세요.", FormatNumber(getter()));
                        if (text == null) return;
                        double value;
                        if (!TryParseDouble(text, out value))
                            throw new FormatException("숫자 값을 입력해야 합니다.");

                        setter(value);
                        RefreshView();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void AttachIntEditor(Label label, string name, Func<int> getter, Action<int> setter)
        {
            try
            {
                label.DoubleClick += (s, e) =>
                {
                    try
                    {
                        if (_waferCassette == null) return;
                        string text = Prompt.Show(name + " 값을 입력하세요.", getter().ToString(CultureInfo.InvariantCulture));
                        if (text == null) return;
                        int value;
                        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) &&
                            !int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
                            throw new FormatException("정수 값을 입력해야 합니다.");

                        setter(value);
                        RefreshView();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void AttachBoolEditor(Label label, string name, Func<bool> getter, Action<bool> setter)
        {
            try
            {
                label.DoubleClick += (s, e) =>
                {
                    try
                    {
                        if (_waferCassette == null) return;
                        string text = Prompt.Show(name + " 값을 입력하세요. (true/false)", getter().ToString());
                        if (text == null) return;

                        bool value;
                        if (!TryParseBool(text, out value))
                            throw new FormatException("true/false, 1/0, on/off 중 하나로 입력해야 합니다.");

                        setter(value);
                        RefreshView();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void RefreshView()
        {
            try
            {
                if (_waferCassette == null)
                    return;

                lblRecipeLoadingVal.Text = FormatUm(_waferCassette.Recipe.LoaingPosition);
                lblRecipeUnloadingVal.Text = FormatUm(_waferCassette.Recipe.UnloadingPosition);
                lblRecipeAvoidVal.Text = FormatUm(_waferCassette.Recipe.AvoidPosition);
                lblRecipeFirstSlotVal.Text = FormatUm(_waferCassette.Recipe.FirstSlotPosition);
                lblRecipeMappingStartVal.Text = FormatUm(_waferCassette.Recipe.MappingStartPosition);
                lblRecipeMappingEndVal.Text = FormatUm(_waferCassette.Recipe.MappingEndPosition);
                lblConfigLoadingOffsetVal.Text = FormatUm(_waferCassette.Config.LoadingPositionOffset);
                lblConfigUnloadingOffsetVal.Text = FormatUm(_waferCassette.Config.UnloadingPositionOffset);
                lblConfigSlotPitchVal.Text = FormatUm(_waferCassette.Config.SlotPitch);
                lblConfigSlotCountVal.Text = _waferCassette.Config.SlotCount.ToString(CultureInfo.InvariantCulture);
                lblConfigScanVelocityVal.Text = FormatNumber(_waferCassette.Config.ScanVelocity) + " mm/s";
                lblSetupToleranceVal.Text = FormatUm(_waferCassette.Setup.InPositionTolerance);
                lblConfigInchVal.Text = _waferCassette.Config.InchSelect.ToString(CultureInfo.InvariantCulture);
                lblConfigLevelVal.Text = _waferCassette.Config.SelectedCassetteLevel.ToString(CultureInfo.InvariantCulture);
                lblSetupSimulationVal.Text = _waferCassette.Setup.IsSimulationMode.ToString();
                lblConfigDryRunVal.Text = _waferCassette.Config.bDryRun.ToString();
                lblActualPositionVal.Text = FormatUm(_waferCassette.WaferLifterZ.ActualPosition);
                lblWaitScanSettleVal.Text = _waferCassette.Config.ScanSettleTimeMs.ToString(CultureInfo.InvariantCulture) + " ms";
                lblWaitMoveTimeoutVal.Text = _waferCassette.Config.ElevatorMoveTimeoutMs.ToString(CultureInfo.InvariantCulture) + " ms";

                dot8Inch.IsOn = _waferCassette.IsWaferCassetteExist(8);
                dot12Inch.IsOn = _waferCassette.IsWaferCassetteExist(12);
                dotProtrusion.IsOn = _waferCassette.IsWaferProtrusionDetected();
                dotMapping.IsOn = _waferCassette.IsWaferMapping();
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
                return rdoFine.Checked;
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        private double JogSpeed()
        {
            try
            {
                return CurrentJogSpeed();
            }
            catch
            {
                return 1.0;
            }
            finally
            {
            }
        }

        private JogSpeedType GetJogSpeedType()
        {
            try
            {
                if (rdoCurrent.Checked)
                    return JogSpeedType.Custom;
                if (rdoCoarse.Checked)
                    return JogSpeedType.Coarse;

                return JogSpeedType.Fine;
            }
            catch
            {
                return JogSpeedType.Fine;
            }
            finally
            {
            }
        }

        private double CurrentJogSpeed()
        {
            try
            {
                if (_waferCassette == null)
                    return 1.0;

                double coarse = _waferCassette.WaferLifterZ.Config.JogCoarseVelocity;
                double ratio = Math.Max(1, trkSpeed.Value) / 100.0;
                return Math.Max(0.1, coarse * ratio);
            }
            catch
            {
                return 1.0;
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
                grpJogMove.BackColor = bg;
                grpJogMode.BackColor = bg;
                jogPadPanel.BackColor = Color.FromArgb(207, 211, 216);
                jogContainer.BackColor = Color.FromArgb(245, 245, 245);
                speedLayout.BackColor = Color.FromArgb(245, 245, 245);

                lblHeader.BackColor = header;
                lblHeader.ForeColor = Color.White;
                lblHeader.Font = new Font("Malgun Gothic", 11F, FontStyle.Bold);

                foreach (var group in new[] { grpActions, grpIo, grpOptions, grpWait, grpJog, grpSpeed, grpJogMove, grpJogMode })
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

                lblSpeedValue.BackColor = key;
                lblSpeedValue.ForeColor = Color.Black;
                lblSpeedValue.Font = new Font("Consolas", 9.5F, FontStyle.Bold);
                lblActualPositionKey.BackColor = key;
                lblActualPositionKey.BorderStyle = BorderStyle.FixedSingle;
                lblActualPositionKey.Font = new Font("Malgun Gothic", 8.5F, FontStyle.Bold);
                lblActualPositionKey.ForeColor = Color.Black;
                lblActualPositionKey.TextAlign = ContentAlignment.MiddleCenter;
                lblActualPositionVal.BackColor = value;
                lblActualPositionVal.BorderStyle = BorderStyle.FixedSingle;
                lblActualPositionVal.Font = new Font("Consolas", 9.5F, FontStyle.Bold);
                lblActualPositionVal.ForeColor = Color.Black;
                lblActualPositionVal.Padding = new Padding(0, 0, 6, 0);
                lblActualPositionVal.TextAlign = ContentAlignment.MiddleRight;

                foreach (var jogButton in new[] { btnJogPlus, btnJogStop, btnJogMinus, btnStep1, btnStep01, btnStep001, btnStep0001, btnStepZero })
                {
                    jogButton.FlatStyle = FlatStyle.Standard;
                    jogButton.Font = new Font("Malgun Gothic", jogButton == btnJogPlus || jogButton == btnJogMinus ? 13F : 9F, FontStyle.Bold);
                }

                ResetJogButtonColors();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Input Cassette Theme", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
