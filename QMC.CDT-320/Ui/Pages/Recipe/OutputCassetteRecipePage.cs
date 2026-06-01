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
    /// <summary>구현 설명 주석입니다.</summary>
    public partial class OutputCassetteRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private OutCassetteUnit _binCassette;
        private BaseDigitalOutput _ngBinLockOut;
        private BaseDigitalOutput _ngBinUnlockOut;
        private readonly Timer _refreshTimer = new Timer();

        /// <summary>구현 설명 주석입니다.</summary>
        public OutputCassetteRecipePage()
        {
            InitializeComponent();
            ApplyRecipeTheme();
            ConfigureRuntimeBehavior();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            ResolveUnit();
            RefreshView();
            _refreshTimer.Start();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _refreshTimer.Stop(); } catch { }
            base.OnHandleDestroyed(e);
        }

        private void ConfigureRuntimeBehavior()
        {
            lblHeader.Tag = "i18n:recipe.outputCassette";
            lblHeader.Text = Lang.T("recipe.outputCassette");

            _refreshTimer.Interval = 250;
            _refreshTimer.Tick += (s, e) => RefreshView();


            AttachTeachMenu(lblOptAvoidVal, "Avoid");
            AttachTeachMenu(lblOptNgSlotVal, "NgFirstSlot");
            AttachTeachMenu(lblOptGood1SlotVal, "Good1FirstSlot");
            AttachTeachMenu(lblOptGood2SlotVal, "Good2FirstSlot");
            AttachIoMenu();
        }

        private void ResolveUnit()
        {
            var machine = FindMachine();
            _binCassette = machine != null ? machine.BinCassette : null;

            _ngBinLockOut = AjinFactory.CreateDigitalOutput(AjinIoCatalog.FindOutput("NgBinCassetteLock"));
            _ngBinUnlockOut = AjinFactory.CreateDigitalOutput(AjinIoCatalog.FindOutput("NgBinCassetteUnlock"));

            if (_binCassette != null)
            {
            }

            SetEnabledState(_binCassette != null);
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
            btnAvoidMove.Enabled = enabled;
            btnNgSlotMove.Enabled = enabled;
            btnGood1SlotMove.Enabled = enabled;
            btnGood2SlotMove.Enabled = enabled;
            btnScan.Enabled = enabled;
        }

        private async void btnAvoidMove_Click(object sender, EventArgs e)
        {
            await MoveTo("Avoid");
        }

        private async void btnNgSlotMove_Click(object sender, EventArgs e)
        {
            await MoveTo("NgFirstSlot");
        }

        private async void btnGood1SlotMove_Click(object sender, EventArgs e)
        {
            await MoveTo("Good1FirstSlot");
        }

        private async void btnGood2SlotMove_Click(object sender, EventArgs e)
        {
            await MoveTo("Good2FirstSlot");
        }

        private async void btnScan_Click(object sender, EventArgs e)
        {
            if (_binCassette == null) return;
            await RunSafeAsync(() => _binCassette.ScanAllCassettesAsync(), "Bin cassette scan");
        }

        private void trkSpeed_ValueChanged(object sender, EventArgs e)
        {
            lblSpeedValue.Text = trkSpeed.Value + " %";
        }

        private async Task MoveTo(string positionName)
        {
        }

        private async Task RunSafeAsync(Func<Task<bool>> action, string actionName)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                bool ok = await action();
                if (!ok)
                    QMC.Common.MessageDialog.Show(this, actionName + " ?ㅽ뙣", "Output Cassette", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void AttachTeachMenu(Label label, string positionName)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("해당 위치로 이동", null, async (s, e) => await MoveTo(positionName));
            menu.Items.Add("현재 위치 저장", null, (s, e) =>
            {
                TeachPosition(positionName);
                RefreshView();
            });

            label.ContextMenuStrip = menu;
            label.Cursor = Cursors.Hand;
            label.DoubleClick += async (s, e) => await MoveTo(positionName);
        }

        private void AttachIoMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("NG Cassette Lock ON", null, (s, e) => { _ngBinLockOut?.On(); _ngBinUnlockOut?.Off(); });
            menu.Items.Add("NG Cassette Unlock ON", null, (s, e) => { _ngBinUnlockOut?.On(); _ngBinLockOut?.Off(); });
            menu.Items.Add("NG Cassette Lock/Unlock OFF", null, (s, e) => { _ngBinLockOut?.Off(); _ngBinUnlockOut?.Off(); });

            ioSection.ContextMenuStrip = menu;
            lblGoodCassette.ContextMenuStrip = menu;
            lblNgCassette.ContextMenuStrip = menu;
            lblBinProtrusion.ContextMenuStrip = menu;
            lblBinMapping.ContextMenuStrip = menu;
        }

        private void TeachPosition(string positionName)
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

        private void RefreshView()
        {
            if (_binCassette == null)
                return;

            lblOptAvoidVal.Text = FormatAxis(_binCassette.Setup.AvoidPosition, _binCassette.BinLifterZ);
            lblOptNgSlotVal.Text = FormatAxis(_binCassette.Setup.NgFirstSlotPosition, _binCassette.BinLifterZ);
            lblOptGood1SlotVal.Text = FormatAxis(_binCassette.Setup.Good1FirstSlotPosition, _binCassette.BinLifterZ);
            lblOptGood2SlotVal.Text = FormatAxis(_binCassette.Setup.Good2FirstSlotPosition, _binCassette.BinLifterZ);
            lblOptSlotPitchVal.Text = FormatAxis(_binCassette.Setup.SlotPitch, _binCassette.BinLifterZ);
            lblOptSlotCountVal.Text = _binCassette.Setup.SlotCount.ToString();
            lblOptActualVal.Text = FormatAxis(_binCassette.BinLifterZ.ActualPosition, _binCassette.BinLifterZ);
            lblOptAxisStateVal.Text = AxisState(_binCassette.BinLifterZ);
            lblWaitVal.Text = _binCassette.Recipe.ElevatorMoveTimeoutMs + " ms";

            dotGoodCassette.IsOn = _binCassette.IsBinCassetteExist(TargetCassette.Good1, 8) ||
                                   _binCassette.IsBinCassetteExist(TargetCassette.Good2, 8);
            dotNgCassette.IsOn = _binCassette.IsBinCassetteExist(TargetCassette.Ng, 8);
            dotBinProtrusion.IsOn = _binCassette.IsBinProtrusionDetected();
            dotBinMapping.IsOn = _binCassette.IsBinMapping();
        }

        private double JogSpeed()
        {
            return Math.Max(1.0, trkSpeed.Value);
        }

        private static string FormatAxis(double value, BaseAxis axis)
        {
            return AxisUnitConverter.FormatDisplay(value, axis, "0.###", true);
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

            foreach (var panel in new Control[] { panelLeft, panelCenter, panelRight, actionSection, ioSection, optionRows, waitRows, jogSection, speedSection })
                panel.BackColor = UiTheme.OptionPanelBg;

            foreach (var label in new[] { lblHeader, lblActionTitle, lblIoTitle, lblOptionTitle, lblWaitTitle, lblSpeedTitle })
            {
                label.BackColor = label == lblHeader ? UiTheme.StatusBarBg : UiTheme.OptionHeaderBg;
                label.ForeColor = label == lblHeader ? UiTheme.StatusBarFg : UiTheme.OptionHeaderFg;
                label.Font = UiTheme.SectionFont;
            }

            foreach (var button in new[] { btnAvoidMove, btnNgSlotMove, btnGood1SlotMove, btnGood2SlotMove, btnScan })
                button.Font = UiTheme.ButtonFont;

            ApplyLabelCellStyle();
        }

        private void ApplyLabelCellStyle()
        {
            foreach (var label in new[]
            {
                lblGoodCassette, lblNgCassette, lblBinProtrusion, lblBinMapping,
                lblOptAvoidKey, lblOptNgSlotKey, lblOptGood1SlotKey, lblOptGood2SlotKey,
                lblOptSlotPitchKey, lblOptSlotCountKey, lblOptActualKey, lblOptAxisStateKey, lblWaitKey
            })
            {
                label.BackColor = Color.Gainsboro;
                label.BorderStyle = BorderStyle.FixedSingle;
                label.Dock = DockStyle.Fill;
                label.Font = UiTheme.ButtonFont;
                label.Margin = Padding.Empty;
                label.Padding = new Padding(6, 0, 6, 0);
                label.TextAlign = ContentAlignment.MiddleLeft;
            }

            foreach (var label in new[]
            {
                lblOptAvoidVal, lblOptNgSlotVal, lblOptGood1SlotVal, lblOptGood2SlotVal,
                lblOptSlotPitchVal, lblOptSlotCountVal, lblOptActualVal, lblOptAxisStateVal,
                lblWaitVal, lblSpeedHigh, lblSpeedMid, lblSpeedLow, lblSpeedValue
            })
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

