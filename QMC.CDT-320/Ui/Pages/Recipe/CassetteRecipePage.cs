using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT320;
using QMC.CDT320.Ajin;
using QMC.Common.IO;
using QMC.Common.Motion;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>
    /// 레시피 - INPUT/OUTPUT CASSETTE 공용 화면.
    /// 좌측 동작 버튼, 중앙 옵션/대기시간, 실린더 I/O, 우측 조그/속도 영역을 표시한다.
    /// </summary>
    public partial class CassetteRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private string _titleI18n = "INPUT/OUTPUT CASSETTE";
        private bool _isOutputCassette;
        private InputCassetteUnit _waferCassette;
        private BinCassetteUnit _binCassette;
        private BaseAxis _activeAxis;
        private readonly Timer _refreshTimer = new Timer();
        private BaseDigitalOutput _ngBinLockOut;
        private BaseDigitalOutput _ngBinUnlockOut;

        public CassetteRecipePage() : this("INPUT/OUTPUT CASSETTE", null, null)
        {
        }

        public CassetteRecipePage(string titleI18n) : this(titleI18n, null, null)
        {
        }

        public CassetteRecipePage(string titleI18n, object unitConfig, string configSavePath)
        {
            InitializeComponent();
            ApplyRecipeTheme();

            _titleI18n = string.IsNullOrWhiteSpace(titleI18n) ? "INPUT/OUTPUT CASSETTE" : titleI18n;
            lblHeader.Text = _titleI18n;
            _isOutputCassette = _titleI18n.IndexOf("output", StringComparison.OrdinalIgnoreCase) >= 0;

            if (unitConfig != null)
                unitConfigGrid.BindConfig(unitConfig, configSavePath);

            ConfigureRuntimeBehavior();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            ResolveUnits();
            RefreshView();
            _refreshTimer.Start();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _refreshTimer.Stop(); } catch { }
            base.OnHandleDestroyed(e);
        }

        private async void btnLoadingMove_Click(object sender, EventArgs e)
        {
            if (_isOutputCassette)
                await MoveBinTo("Avoid");
            else
                await MoveWaferTo("Avoid");
        }

        private async void btnUnloadingMove_Click(object sender, EventArgs e)
        {
            if (_isOutputCassette)
                await MoveBinTo("NgFirstSlot");
            else
                await MoveWaferTo("FirstSlot");
        }

        private async void btnReadyMove_Click(object sender, EventArgs e)
        {
            if (_isOutputCassette)
                await MoveBinTo("Good1FirstSlot");
            else
                await MoveWaferTo("MappingStart");
        }

        private async void btnSlotLoadingMove_Click(object sender, EventArgs e)
        {
            if (_isOutputCassette)
                await MoveBinTo("Good2FirstSlot");
            else
                await MoveWaferTo("MappingEnd");
        }

        private async void btnSlotUnloadingMove_Click(object sender, EventArgs e)
        {
            if (_isOutputCassette)
                await RunSafeAsync(() => _binCassette.ScanAllCassettesAsync(), "Bin cassette scan");
            else
                await RunSafeAsync(() => _waferCassette.WaferScan(), "Wafer cassette scan");
        }

        private void btnJogPlus_Click(object sender, EventArgs e)
        {
            Jog(1);
        }

        private void btnJogMinus_Click(object sender, EventArgs e)
        {
            Jog(-1);
        }

        private void btnJogStop_Click(object sender, EventArgs e)
        {
            StopJog();
        }

        private void trkSpeed_ValueChanged(object sender, EventArgs e)
        {
            lblSpeedValue.Text = $"{trkSpeed.Value} %";
        }

        private void ConfigureRuntimeBehavior()
        {
            _refreshTimer.Interval = 250;
            _refreshTimer.Tick += (s, e) => RefreshView();

            btnLoadingMove.Text = "AVOID 이동";
            btnUnloadingMove.Text = _isOutputCassette ? "NG 1번 슬롯 이동" : "1번 슬롯 이동";
            btnReadyMove.Text = _isOutputCassette ? "GOOD1 1번 슬롯 이동" : "MAPPING START 이동";
            btnSlotLoadingMove.Text = _isOutputCassette ? "GOOD2 1번 슬롯 이동" : "MAPPING END 이동";
            btnSlotUnloadingMove.Text = "SCAN";

            lblSensor1.Text = _isOutputCassette ? "GOOD CASSETTE" : "8 INCH CASSETTE";
            lblSensor2.Text = _isOutputCassette ? "NG CASSETTE" : "12 INCH CASSETTE";
            lblProtrusion.Text = _isOutputCassette ? "BIN PROTRUSION" : "WAFER PROTRUSION";
            lblMapping.Text = _isOutputCassette ? "BIN MAPPING" : "WAFER MAPPING";
            axisJogLineControl.MoveOptions = jogMoveOptionsControl;
            axisJogLineControl.SpeedProvider = JogSpeed;

            AttachTeachMenu(lblOptLoadingZVal, "Avoid");
            AttachTeachMenu(lblOptUnloadingZVal, _isOutputCassette ? "NgFirstSlot" : "FirstSlot");
            AttachTeachMenu(lblOptReadyPosVal, _isOutputCassette ? "Good1FirstSlot" : "MappingStart");
            AttachTeachMenu(lblOptMappingZVal, _isOutputCassette ? "Good2FirstSlot" : "MappingEnd");
            AttachIoMenu();
        }

        private void ResolveUnits()
        {
            var machine = FindMachine();
            if (machine == null)
            {
                SetEnabledState(false);
                return;
            }

            _waferCassette = machine.InputLoader?.WaferCassette;
            _binCassette = machine.BinCassette;
            _activeAxis = _isOutputCassette
                ? (_binCassette != null ? _binCassette.BinLifterZ : null)
                : (_waferCassette != null ? _waferCassette.WaferLifterZ : null);
            BindJogAxes();

            if (_isOutputCassette)
            {
                _ngBinLockOut = AjinFactory.CreateDigitalOutput(AjinIoCatalog.FindOutput("NgBinCassetteLock"));
                _ngBinUnlockOut = AjinFactory.CreateDigitalOutput(AjinIoCatalog.FindOutput("NgBinCassetteUnlock"));
                if (_binCassette != null)
                    unitConfigGrid.BindConfig(_binCassette.Setup, null);
            }
            else if (_waferCassette != null)
            {
                unitConfigGrid.BindConfig(_waferCassette.Setup, null);
            }

            SetEnabledState(_activeAxis != null);
        }

        private void BindJogAxes()
        {
            if (axisJogLineControl == null)
                return;

            if (_isOutputCassette)
                axisJogLineControl.BindAxis("BIN\r\nLIFTER Z", _binCassette != null ? _binCassette.BinLifterZ : null);
            else
                axisJogLineControl.BindAxis("WAFER\r\nLIFTER Z", _waferCassette != null ? _waferCassette.WaferLifterZ : null);
        }

        private CDT320_Machine FindMachine()
        {
            foreach (Form form in Application.OpenForms)
            {
                var host = form as QMC.CDT_320.Form1;
                if (host != null)
                    return host.Machine;
            }

            return null;
        }

        private void SetEnabledState(bool enabled)
        {
            foreach (var button in new Control[] { btnLoadingMove, btnUnloadingMove, btnReadyMove, btnSlotLoadingMove, btnSlotUnloadingMove })
                button.Enabled = enabled;

            jogMoveOptionsControl.Enabled = enabled;
            axisJogLineControl.Enabled = enabled;
        }

        private void RefreshView()
        {
            if (_isOutputCassette)
                RefreshBinView();
            else
                RefreshWaferView();
        }

        private void RefreshWaferView()
        {
            if (_waferCassette == null)
                return;

            lblOptLoadingZKey.Text = "Avoid Position";
            lblOptUnloadingZKey.Text = "First Slot Position";
            lblOptReadyPosKey.Text = "Mapping Start Position";
            lblOptMappingZKey.Text = "Mapping End Position";
            lblOptSlotPitchKey.Text = "Slot Pitch";
            lblOptCassetteGapKey.Text = "Slot Count";
            lblOptInchKey.Text = "Actual Position";
            lblOptStageKey.Text = "Axis State";
            lblWaitKey.Text = "Move Timeout";

            lblOptLoadingZVal.Text = FormatMm(_waferCassette.Recipe.AvoidPosition);
            lblOptUnloadingZVal.Text = FormatMm(_waferCassette.Recipe.FirstSlotPosition);
            lblOptReadyPosVal.Text = FormatMm(_waferCassette.Recipe.MappingStartPosition);
            lblOptMappingZVal.Text = FormatMm(_waferCassette.Recipe.MappingEndPosition);
            lblOptSlotPitchVal.Text = FormatMm(_waferCassette.Config.SlotPitch);
            lblOptCassetteGapVal.Text = _waferCassette.Config.SlotCount.ToString();
            lblOptInchVal.Text = FormatMm(_waferCassette.WaferLifterZ.ActualPosition);
            lblOptStageVal.Text = AxisState(_waferCassette.WaferLifterZ);
            lblWaitVal.Text = _waferCassette.Config.ElevatorMoveTimeoutMs + " ms";

            dotSensor1.IsOn = _waferCassette.IsWaferCassetteExist(8);
            dotSensor2.IsOn = _waferCassette.IsWaferCassetteExist(12);
            dotProtrusion.IsOn = _waferCassette.IsWaferProtrusionDetected();
            dotMapping.IsOn = _waferCassette.IsWaferMapping();
        }

        private void RefreshBinView()
        {
            if (_binCassette == null)
                return;

            lblOptLoadingZKey.Text = "Avoid Position";
            lblOptUnloadingZKey.Text = "NG First Slot";
            lblOptReadyPosKey.Text = "Good1 First Slot";
            lblOptMappingZKey.Text = "Good2 First Slot";
            lblOptSlotPitchKey.Text = "Slot Pitch";
            lblOptCassetteGapKey.Text = "Slot Count";
            lblOptInchKey.Text = "Actual Position";
            lblOptStageKey.Text = "Axis State";
            lblWaitKey.Text = "Move Timeout";

            lblOptLoadingZVal.Text = FormatMm(_binCassette.Setup.AvoidPosition);
            lblOptUnloadingZVal.Text = FormatMm(_binCassette.Setup.NgFirstSlotPosition);
            lblOptReadyPosVal.Text = FormatMm(_binCassette.Setup.Good1FirstSlotPosition);
            lblOptMappingZVal.Text = FormatMm(_binCassette.Setup.Good2FirstSlotPosition);
            lblOptSlotPitchVal.Text = FormatMm(_binCassette.Setup.SlotPitch);
            lblOptCassetteGapVal.Text = _binCassette.Setup.SlotCount.ToString();
            lblOptInchVal.Text = FormatMm(_binCassette.BinLifterZ.ActualPosition);
            lblOptStageVal.Text = AxisState(_binCassette.BinLifterZ);
            lblWaitVal.Text = _binCassette.Recipe.ElevatorMoveTimeoutMs + " ms";

            dotSensor1.IsOn = _binCassette.IsBinCassetteExist(TargetCassette.Good1, 8) ||
                              _binCassette.IsBinCassetteExist(TargetCassette.Good2, 8);
            dotSensor2.IsOn = _binCassette.IsBinCassetteExist(TargetCassette.Ng, 8);
            dotProtrusion.IsOn = _binCassette.IsBinProtrusionDetected();
            dotMapping.IsOn = _binCassette.IsBinMapping();
        }

        private async Task MoveWaferTo(string positionName)
        {
            if (_waferCassette == null) return;
            await RunSafeAsync(() => _waferCassette.MoveToTeachingPositionAndVerify(positionName), "Wafer cassette move " + positionName);
        }

        private async Task MoveBinTo(string positionName)
        {
            if (_binCassette == null) return;
            await RunSafeAsync(() => _binCassette.MoveToTeachingPositionAndVerify(positionName), "Bin cassette move " + positionName);
        }

        private async Task RunSafeAsync(Func<Task<bool>> action, string actionName)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                bool ok = await action();
                if (!ok)
                    MessageBox.Show(this, actionName + " 실패", "Cassette", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void AttachTeachMenu(Label label, string positionName)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("해당 위치로 이동", null, async (s, e) =>
            {
                if (_isOutputCassette)
                    await MoveBinTo(positionName);
                else
                    await MoveWaferTo(positionName);
            });
            menu.Items.Add("현재 위치 티칭", null, (s, e) =>
            {
                TeachPosition(positionName);
                RefreshView();
            });
            label.ContextMenuStrip = menu;
            label.Cursor = Cursors.Hand;
            label.DoubleClick += async (s, e) =>
            {
                if (_isOutputCassette)
                    await MoveBinTo(positionName);
                else
                    await MoveWaferTo(positionName);
            };
        }

        private void AttachIoMenu()
        {
            var menu = new ContextMenuStrip();
            if (_isOutputCassette)
            {
                menu.Items.Add("NG Cassette Lock ON", null, (s, e) => { _ngBinLockOut?.On(); _ngBinUnlockOut?.Off(); });
                menu.Items.Add("NG Cassette Unlock ON", null, (s, e) => { _ngBinUnlockOut?.On(); _ngBinLockOut?.Off(); });
                menu.Items.Add("NG Cassette Lock/Unlock OFF", null, (s, e) => { _ngBinLockOut?.Off(); _ngBinUnlockOut?.Off(); });
            }
            else
            {
                menu.Items.Add("Input Cassette I/O는 DI 확인 전용입니다.", null, (s, e) => { });
            }

            ioSection.ContextMenuStrip = menu;
            lblIoTitle.ContextMenuStrip = menu;
            lblSensor1.ContextMenuStrip = menu;
            lblSensor2.ContextMenuStrip = menu;
            lblProtrusion.ContextMenuStrip = menu;
            lblMapping.ContextMenuStrip = menu;
        }

        private void TeachPosition(string positionName)
        {
            if (_isOutputCassette)
                TeachBinPosition(positionName);
            else
                TeachWaferPosition(positionName);
        }

        private void TeachWaferPosition(string positionName)
        {
            if (_waferCassette == null) return;
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase))
                _waferCassette.TeachWaferLifterZAvoidPosition();
            else if (string.Equals(positionName, "FirstSlot", StringComparison.OrdinalIgnoreCase))
                _waferCassette.TeachWaferLifterZPosition("FirstSlot");
            else if (string.Equals(positionName, "MappingStart", StringComparison.OrdinalIgnoreCase))
                _waferCassette.TeachWaferLifterZMappingStartPosition();
            else if (string.Equals(positionName, "MappingEnd", StringComparison.OrdinalIgnoreCase))
                _waferCassette.TeachWaferLifterZMappingEndPosition();
        }

        private void TeachBinPosition(string positionName)
        {
            if (_binCassette == null) return;
            if (string.Equals(positionName, "Avoid", StringComparison.OrdinalIgnoreCase))
                _binCassette.TeachBinLifterZAvoidPosition();
            else if (string.Equals(positionName, "NgFirstSlot", StringComparison.OrdinalIgnoreCase))
                _binCassette.TeachBinLifterZFirstSlotPosition(TargetCassette.Ng);
            else if (string.Equals(positionName, "Good1FirstSlot", StringComparison.OrdinalIgnoreCase))
                _binCassette.TeachBinLifterZFirstSlotPosition(TargetCassette.Good1);
            else if (string.Equals(positionName, "Good2FirstSlot", StringComparison.OrdinalIgnoreCase))
                _binCassette.TeachBinLifterZFirstSlotPosition(TargetCassette.Good2);
        }

        private void Jog(int direction)
        {
            if (_activeAxis == null) 
                return;

            try
            {
                _activeAxis.MoveJogContinuous(direction, JogSpeedType.Custom, JogSpeed());
            }
            catch (Exception ex)
            {
                //log.Write(ex);
                MessageBox.Show(this, ex.Message, "Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopJog()
        {
            try { _activeAxis?.StopJog(); } catch { }
        }

        private double JogSpeed()
        {
            return Math.Max(1.0, trkSpeed.Value);
        }

        private static string FormatMm(double value)
        {
            return value.ToString("F3") + " mm";
        }

        private static string AxisState(BaseAxis axis)
        {
            if (axis == null) return "-";
            if (axis.IsAlarm) return "ALARM";
            if (axis.IsMoving) return "MOVING";
            if (axis.IsInPosition) return "INPOS";
            return "READY";
        }

        private void ApplyRecipeTheme()
        {
            BackColor = UiTheme.MainBg;
            mainLayout.BackColor = UiTheme.MainBg;
            contentLayout.BackColor = UiTheme.MainBg;

            foreach (var panel in new[] { panelLeft, panelCenter, panelRight, actionSection, ioSection, optionRows, waitRows, jogSection, speedSection, ioSensor1Row, ioSensor2Row, ioProtrusionRow, ioMappingRow })
                panel.BackColor = UiTheme.OptionPanelBg;

            foreach (var label in new[] { lblHeader, lblActionTitle, lblIoTitle, lblWaitTitle, lblSpeedTitle })
            {
                label.BackColor = label == lblHeader ? UiTheme.StatusBarBg : UiTheme.OptionHeaderBg;
                label.ForeColor = label == lblHeader ? UiTheme.StatusBarFg : UiTheme.OptionHeaderFg;
                label.Font = UiTheme.SectionFont;
            }

            foreach (var button in new[] { btnLoadingMove, btnUnloadingMove, btnReadyMove, btnSlotLoadingMove, btnSlotUnloadingMove })
                button.Font = UiTheme.ButtonFont;

            ApplyLabelCellStyle();
        }

        private void ApplyLabelCellStyle()
        {
            var keyLabels = new[]
            {
                lblSensor1, lblSensor2, lblProtrusion, lblMapping,
                lblOptLoadingZKey, lblOptUnloadingZKey, lblOptReadyPosKey, lblOptMappingZKey,
                lblOptSlotPitchKey, lblOptCassetteGapKey, lblOptInchKey, lblOptStageKey,
                lblWaitKey
            };

            foreach (var label in keyLabels)
            {
                label.BackColor = Color.Gainsboro;
                label.BorderStyle = BorderStyle.FixedSingle;
                label.Dock = DockStyle.Fill;
                label.Font = UiTheme.ButtonFont;
                label.Margin = Padding.Empty;
                label.Padding = new Padding(6, 0, 6, 0);
                label.TextAlign = ContentAlignment.MiddleLeft;
            }

            var valueLabels = new[]
            {
                lblOptLoadingZVal, lblOptUnloadingZVal, lblOptReadyPosVal, lblOptMappingZVal,
                lblOptSlotPitchVal, lblOptCassetteGapVal, lblOptInchVal, lblOptStageVal,
                lblWaitVal, lblSpeedHigh, lblSpeedMid, lblSpeedLow, lblSpeedValue
            };

            foreach (var label in valueLabels)
            {
                label.BackColor = UiTheme.ValueBoxBg;
                label.BorderStyle = BorderStyle.FixedSingle;
                label.Dock = DockStyle.Fill;
                label.Font = UiTheme.ValueFont;
                label.ForeColor = UiTheme.ValueBoxFg;
                label.Margin = Padding.Empty;
                label.Padding = new Padding(6, 0, 6, 0);
                label.TextAlign = ContentAlignment.MiddleRight;
            }
        }
    }
}
