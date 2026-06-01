using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT_320.Ui.Controls;
using QMC.Common.Alarms;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    /// <summary>Input Cassette 작업 정보 페이지. RecipePage 레이아웃을 따르며 운전 모니터링 + 주요 액션을 제공합니다.</summary>
    public partial class InputCassettePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private const int DEFAULT_SLOT_COUNT = 25;

        private InputCassetteUnit _unit;
        private readonly Timer _refreshTimer = new Timer();
        private int _builtSlotCount = -1;

        /// <summary>InputCassettePage를 생성합니다.</summary>
        public InputCassettePage()
        {
            try
            {
                InitializeComponent();
                ApplyTheme();
                ConfigureRuntimeBehavior();
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-CTOR", Name, ex.Message);
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
                BuildSlotRows(_unit != null ? Math.Max(1, _unit.Config.SlotCount) : DEFAULT_SLOT_COUNT);
                BindStatusGrid();
                BindIoPanel();
                BindJogPanel();
                RefreshFromMachine();
                _refreshTimer.Start();
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-LOAD", Name, ex.Message);
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
                try { jogAxisMoveControl.StopAllAsync(true).GetAwaiter().GetResult(); } catch { }
            }
            catch
            {
            }
            finally
            {
                base.OnHandleDestroyed(e);
            }
        }

        // ============================================================
        // Init / Theme / Wire
        // ============================================================

        private void ConfigureRuntimeBehavior()
        {
            try
            {
                _refreshTimer.Interval = 250;
                _refreshTimer.Tick += RefreshTimer_Tick;

                btnInit.Click += btnInit_Click;
                btnReady.Click += btnReady_Click;
                btnMap.Click += btnMap_Click;
                btnLoad.Click += btnLoad_Click;
                btnUnload.Click += btnUnload_Click;
                btnPrev.Click += (s, e) => MoveSlotRel(-1);
                btnNext.Click += (s, e) => MoveSlotRel(+1);
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-CFG", Name, ex.Message);
            }
            finally
            {
            }
        }

        private void ApplyTheme()
        {
            try
            {
                Color bg = Color.FromArgb(207, 210, 214);
                Color groupBg = Color.FromArgb(245, 245, 245);

                BackColor = bg;
                rootLayout.BackColor = bg;
                contentLayout.BackColor = bg;
                leftLayout.BackColor = bg;
                centerLayout.BackColor = bg;
                rightLayout.BackColor = bg;

                foreach (var group in new[] { grpActions, grpIo, grpStatus, grpSlotMap, grpJog, grpSpeed })
                {
                    group.BackColor = groupBg;
                    group.Font = new Font("Malgun Gothic", 10F, FontStyle.Bold);
                }

                lblHeader.Font = new Font("Malgun Gothic", 11F, FontStyle.Bold);
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-THEME", Name, ex.Message);
            }
            finally
            {
            }
        }

        // ============================================================
        // Resolve / Bind
        // ============================================================

        private void ResolveUnit()
        {
            try
            {
                var host = FindHostForm();
                _unit = host != null && host.Machine != null && host.Machine.InputLoader != null
                    ? host.Machine.InputLoader.InputCassette
                    : null;

                if (_unit != null)
                    _unit.EnsureSlotPositionBuffer();

                SetEnabledState(_unit != null);
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-RESOLVE", Name, ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Resolve", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void SetEnabledState(bool unitReady)
        {
            try
            {
                bool runtimeBusy = IsRuntimeBusy();
                bool actionEnabled = unitReady && !runtimeBusy;
                bool jogEnabled = unitReady && !runtimeBusy;

                btnInit.Enabled = actionEnabled;
                btnReady.Enabled = actionEnabled;
                btnMap.Enabled = actionEnabled;
                btnLoad.Enabled = actionEnabled;
                btnUnload.Enabled = actionEnabled;
                btnPrev.Enabled = jogEnabled;
                btnNext.Enabled = jogEnabled;

                jogPositionListControl.Enabled = jogEnabled;
                jogAxisMoveControl.Enabled = jogEnabled;
                jogSpeedControl.Enabled = jogEnabled;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-ENABLE", Name, ex.Message);
            }
            finally
            {
            }
        }

        private bool IsRuntimeBusy()
        {
            try
            {
                var host = FindHostForm();
                if (host == null || host.Controller == null) return false;

                // Controller 운전 상태 속성명이 확정되면 직접 호출로 교체.
                // 현재는 리플렉션으로 IsRunning / IsAuto / IsBusy 중 첫 번째를 사용.
                var type = host.Controller.GetType();
                foreach (var prop in new[] { "IsRunning", "IsAuto", "IsBusy" })
                {
                    var p = type.GetProperty(prop);
                    if (p != null && p.PropertyType == typeof(bool))
                        return (bool)p.GetValue(host.Controller);
                }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private void BindStatusGrid()
        {
            try
            {
                if (_unit == null)
                {
                    statusParameterGrid.SetItems(Enumerable.Empty<ParameterGridItem>());
                    return;
                }

                statusParameterGrid.SetItems(new[]
                {
                    ParameterGridItem.Double("LIFTER Z POSITION", "mm",   ParameterGridScope.Setup,  () => _unit.WaferLifterZ.ActualPosition,           null),
                    ParameterGridItem.Int   ("CURRENT SLOT",      "",     ParameterGridScope.Setup,  () => GetCurrentSlotOneBased(),                    null),
                    ParameterGridItem.Int   ("MAPPED COUNT",      "ea",   ParameterGridScope.Setup,  () => CountMapped(),                               null),
                    ParameterGridItem.Int   ("INCH",              "",     ParameterGridScope.Config, () => _unit.Config.InchSelect,                     null),
                    ParameterGridItem.Int   ("CASSETTE LEVEL",    "",     ParameterGridScope.Config, () => _unit.Config.SelectedCassetteLevel,          null),
                    ParameterGridItem.Double("SCAN/JOG VELOCITY", "mm/s", ParameterGridScope.Config, () => _unit.Config.ScanVelocity,                   null),
                    ParameterGridItem.Int   ("MOVE TIMEOUT",      "ms",   ParameterGridScope.Config, () => _unit.Config.ElevatorMoveTimeoutMs,          null),
                    ParameterGridItem.Bool  ("SIMULATION MODE",           ParameterGridScope.Setup,  () => _unit.Setup.IsSimulationMode,                null),
                    ParameterGridItem.Bool  ("DRY RUN",                   ParameterGridScope.Config, () => _unit.Config.bDryRun,                        null),
                });
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-STATUS", Name, ex.Message);
            }
            finally
            {
            }
        }

        private void BindIoPanel()
        {
            try
            {
                if (_unit == null) return;

                ioCylinderPanel.SetItems(new[]
                {
                    IoCylinderItem.Input("8 INCH CASSETTE",  () => _unit.IsWaferCassetteExist(8)),
                    IoCylinderItem.Input("12 INCH CASSETTE", () => _unit.IsWaferCassetteExist(12)),
                    IoCylinderItem.Input("WAFER PROTRUSION", () => _unit.IsWaferProtrusionDetected()),
                    IoCylinderItem.Input("WAFER MAPPING",    () => _unit.IsWaferMapping())
                });
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-IO", Name, ex.Message);
            }
            finally
            {
            }
        }

        private void BindJogPanel()
        {
            try
            {
                if (_unit == null) return;

                JogAxisItem axisItem = JogAxisItem.Single("AXIS Z", _unit.WaferLifterZ, "um", 1000.0, "Z+", "Z-");
                axisItem.StepMoveAsync = async (item, direction, speedType, customSpeed, axisStepDistance) =>
                {
                    try
                    {
                        double target = _unit.WaferLifterZ.ActualPosition + (direction * axisStepDistance);
                        int moveResult = await _unit.MoveWaferLifterZ(target, speedType, customSpeed);
                        if (moveResult != 0) return moveResult;
                        return await _unit.WaitWaferLifterZMoveDone(_unit.Config.ElevatorMoveTimeoutMs);
                    }
                    catch { throw; }
                    finally { }
                };
                axisItem.ContinuousMoveAsync = async (item, direction, speedType, customSpeed) =>
                {
                    try { return await _unit.ManualMoveWaferLifterZJog(direction, speedType, customSpeed); }
                    catch { throw; }
                    finally { }
                };
                axisItem.StopAsync = async item =>
                {
                    try { return await _unit.ManualStopWaferLifterZ(); }
                    catch { throw; }
                    finally { }
                };

                jogAxisMoveControl.SpeedControl = jogSpeedControl;
                jogAxisMoveControl.SetItems(new[] { axisItem });
                jogPositionListControl.SetItems(new[] { axisItem });
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-JOG", Name, ex.Message);
            }
            finally
            {
            }
        }

        // ============================================================
        // Slot Map (dynamic)
        // ============================================================

        /// <summary>Config.SlotCount에 맞춰 SLOT MAP 행을 동적으로 생성합니다.</summary>
        private void BuildSlotRows(int count)
        {
            try
            {
                if (count <= 0) count = DEFAULT_SLOT_COUNT;
                if (_builtSlotCount == count) return;

                lifterLayout.SuspendLayout();
                lifterLayout.Controls.Clear();
                lifterLayout.RowStyles.Clear();
                lifterLayout.RowCount = count;

                _slotIndexLbls = new Label[count];
                _slotLeds = new Label[count];
                _slotNameLbls = new Label[count];

                for (int i = 0; i < count; i++)
                {
                    lifterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));

                    _slotIndexLbls[i] = new Label
                    {
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0, 1, 4, 1),
                        Text = (i + 1).ToString(CultureInfo.InvariantCulture),
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = Color.FromArgb(208, 208, 208),
                        BorderStyle = BorderStyle.FixedSingle,
                        Font = new Font("Malgun Gothic", 9F, FontStyle.Bold)
                    };
                    _slotLeds[i] = new Label
                    {
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0, 1, 4, 1),
                        BackColor = Color.LightGray,
                        BorderStyle = BorderStyle.FixedSingle
                    };
                    _slotNameLbls[i] = new Label
                    {
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0, 1, 0, 1),
                        Text = "EMPTY",
                        TextAlign = ContentAlignment.MiddleLeft,
                        BackColor = Color.White,
                        BorderStyle = BorderStyle.FixedSingle,
                        Padding = new Padding(8, 0, 0, 0),
                        Font = new Font("Consolas", 9F)
                    };

                    lifterLayout.Controls.Add(_slotIndexLbls[i], 0, i);
                    lifterLayout.Controls.Add(_slotLeds[i], 1, i);
                    lifterLayout.Controls.Add(_slotNameLbls[i], 2, i);
                }

                lifterLayout.ResumeLayout();
                _builtSlotCount = count;
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-SLOTROWS", Name, ex.Message);
            }
            finally
            {
            }
        }

        // ============================================================
        // Refresh
        // ============================================================

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                RefreshFromMachine();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void RefreshFromMachine()
        {
            try
            {
                if (_unit == null)
                {
                    SetEnabledState(false);
                    return;
                }

                // SlotCount 변경 감지 시 행 재생성
                int desired = Math.Max(1, _unit.Config.SlotCount);
                if (_builtSlotCount != desired)
                {
                    BuildSlotRows(desired);
                    BindStatusGrid();
                }

                statusParameterGrid.RefreshValues();
                ioCylinderPanel.RefreshStates();
                jogPositionListControl.RefreshState();

                var map = _unit.WaferMap;
                int curSlot = GetCurrentSlotZeroBased();

                for (int i = 0; i < _builtSlotCount; i++)
                {
                    Color led;
                    string text;
                    if (map != null && i < map.Count)
                    {
                        if (i == curSlot) { led = Color.Cyan; text = "WORKING"; }
                        else if (map[i]) { led = Color.LimeGreen; text = "WAFER"; }
                        else { led = Color.LightGray; text = "EMPTY"; }
                    }
                    else
                    {
                        led = Color.LightGray;
                        text = "-";
                    }

                    if (_slotLeds[i].BackColor != led) _slotLeds[i].BackColor = led;
                    if (_slotNameLbls[i].Text != text) _slotNameLbls[i].Text = text;
                }

                SetEnabledState(true);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private int GetCurrentSlotZeroBased()
        {
            try
            {
                var host = FindHostForm();
                return host != null && host.Controller != null ? host.Controller.CurrentInputSlot : -1;
            }
            catch
            {
                return -1;
            }
            finally
            {
            }
        }

        private int GetCurrentSlotOneBased()
        {
            int s = GetCurrentSlotZeroBased();
            return s < 0 ? 0 : s + 1;
        }

        private int CountMapped()
        {
            try
            {
                var map = _unit != null ? _unit.WaferMap : null;
                if (map == null) return 0;
                int n = 0;
                for (int i = 0; i < map.Count; i++) if (map[i]) n++;
                return n;
            }
            catch
            {
                return 0;
            }
            finally
            {
            }
        }

        // ============================================================
        // Action handlers
        // ============================================================

        private async void btnInit_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmAction("LIFTER INIT 을(를) 진행하시겠습니까?")) return;
                await RunSafeAsync(LifterInitAsync, "INPUT-CASSETTE-INIT");
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-INIT", Name, ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Init", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshFromMachine();
            }
        }

        private async void btnReady_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmAction("LIFTER READY 를 진행하시겠습니까?")) return;
                await RunSafeAsync(LifterReadyAsync, "INPUT-CASSETTE-READY");
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-READY", Name, ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Ready", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshFromMachine();
            }
        }

        private async void btnMap_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmAction("MAP / SCAN 을 진행하시겠습니까?")) return;
                await RunSafeAsync(host => host.Controller.ScanInputCassetteAsync(), "INPUT-CASSETTE-MAP");
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-MAP", Name, ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshFromMachine();
            }
        }

        private async void btnLoad_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmAction("LOAD WAFER 를 진행하시겠습니까?")) return;
                await RunSafeAsync(host => host.Controller.LoadNextWaferAsync(), "INPUT-CASSETTE-LOAD");
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-LOAD", Name, ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshFromMachine();
            }
        }

        private async void btnUnload_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmAction("UNLOAD WAFER 를 진행하시겠습니까?")) return;
                await RunSafeAsync(host => host.Controller.RetractCurrentWaferAsync(), "INPUT-CASSETTE-UNLOAD");
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-UNLOAD", Name, ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette Unload", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshFromMachine();
            }
        }

        private void MoveSlotRel(int delta)
        {
            try
            {
                if (_unit == null) return;
                double pitch = _unit.Config.SlotPitch > 0 ? _unit.Config.SlotPitch : 6.0;
                double velocity = _unit.Config.ScanVelocity > 0 ? _unit.Config.ScanVelocity : 20.0;
                _ = _unit.WaferLifterZ.MoveRelativeAsync(delta * pitch, velocity);
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "UI-INPUT-CASSETTE-SLOTREL", Name, ex.Message);
            }
            finally
            {
                RefreshFromMachine();
            }
        }

        // ============================================================
        // Action helpers
        // ============================================================

        private async Task LifterInitAsync(Form1 host)
        {
            try
            {
                var loader = host.Machine.InputLoader;
                loader.WaferLifterZ.ResetAlarm();
                loader.WaferLifterZ.ServoOn();
                loader.FeederY.ResetAlarm();
                loader.FeederY.ServoOn();
                await loader.WaferLifterZ.HomeSearchAsync();
                await loader.FeederY.HomeSearchAsync();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private Task LifterReadyAsync(Form1 host)
        {
            try
            {
                var loader = host.Machine.InputLoader;
                loader.WaferLifterZ.ServoOn();
                loader.FeederY.ServoOn();
                return Task.CompletedTask;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task MapAsync(Form1 host)
        {
            try
            {
                var ctx = new QMC.CDT320.Sequencing.MachineSequenceContext(
                    host.Controller,
                    new QMC.CDT320.Sequencing.SequenceSignalBus());
                var sequence = new QMC.CDT320.Sequencing.InputLoaderSequence(ctx);
                await sequence.ExecuteMappingAsync(System.Threading.CancellationToken.None);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task LoadAsync(Form1 host)
        {
            await host.Controller.LoadNextWaferAsync();
        }

        private async Task UnloadAsync(Form1 host)
        {
            await host.Controller.RetractCurrentWaferAsync();
        }

        private bool ConfirmAction(string message)
        {
            try
            {
                DialogResult result = QMC.Common.MessageDialog.Show(
                    this, message, "Input Cassette",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                return result == DialogResult.Yes;
            }
            catch
            {
                return false;
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
                    if (host != null) return host;
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
    }
}
