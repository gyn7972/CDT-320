using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class OutputFeederPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private Timer _timer;

        public OutputFeederPage()
        {
            InitializeComponent();
            WireEvents();

            _timer = new Timer { Interval = 200 };
            _timer.Tick += (s, e) => RefreshData();
            HandleCreated += (s, e) => _timer.Start();
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private Form1 GetHost() => FindForm() as Form1;

        private void WireEvents()
        {
            btnInit.Click += (s, e) => RunAction(host => InitFeederAsync(host));
            btnMap.Click += (s, e) => RunAction(host => host.Controller.ScanOutputCassettesAsync());
            btnPick.Click += (s, e) => RunAction(host => host.Machine.OutputCassetteUnit.SupplyEmptyWaferAsync(host.Machine.OutputFeederUnit, QMC.CDT320.TargetCassette.Good1, 0));
            btnPlace.Click += (s, e) => RunAction(host => host.Controller.StoreCompletedWaferAsync(false));
        }

        private async void RunAction(Func<Form1, Task<bool>> action)
        {
            var host = GetHost();
            if (host?.Machine == null) return;

            try { await action(host); }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("OutputFeeder error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            RefreshData();
        }

        private async void RunAction(Func<Form1, Task> action)
        {
            var host = GetHost();
            if (host?.Machine == null) return;

            try { await action(host); }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("OutputFeeder error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            RefreshData();
        }

        private async Task InitFeederAsync(Form1 host)
        {
            var feeder = host.Machine.OutputFeederUnit;
            var cassette = host.Machine.OutputCassetteUnit;
            feeder.FeederY.ResetAlarm();
            feeder.FeederY.ServoOn();
            cassette.OutputLifterZ.ResetAlarm();
            cassette.OutputLifterZ.ServoOn();
            await feeder.FeederY.HomeSearchAsync();
            await cassette.OutputLifterZ.HomeSearchAsync();
        }

        private void RefreshData()
        {
            var host = GetHost();
            if (host?.Machine == null) return;

            var feeder = host.Machine.OutputFeederUnit;
            var cassette = host.Machine.OutputCassetteUnit;
            lblFeederPos.Text = AxisUnitConverter.FormatDisplay(feeder.FeederY.ActualPosition, feeder.FeederY, "0.###", true);
            lblElevatorPos.Text = AxisUnitConverter.FormatDisplay(cassette.OutputLifterZ.ActualPosition, cassette.OutputLifterZ, "0.###", true);
            lblClamp.Text = feeder.FeederClampCyl.IsFwd ? "CLAMPED" : (feeder.FeederClampCyl.IsBwd ? "OPEN" : "...");
            lblUpDown.Text = feeder.FeederUpDownCyl.IsFwd ? "DOWN" : (feeder.FeederUpDownCyl.IsBwd ? "UP" : "...");
            dotNg.IsOn = cassette.NgBin8CassetteCheck0.IsOn;
            dotGood1.IsOn = cassette.GoodBin8CassetteCheck0.IsOn;
            dotGood2.IsOn = cassette.GoodBin8CassetteCheck1.IsOn;
            dotProtrusion.IsOn = cassette.ProtrusionSensor.IsOn;
            dotDetect.IsOn = cassette.WaferDetectSensor.IsOn;
            dotClamped.IsOn = feeder.WaferClampedSensor.IsOn;
        }
    }
}


