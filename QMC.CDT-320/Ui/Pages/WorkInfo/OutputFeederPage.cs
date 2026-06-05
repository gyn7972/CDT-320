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
            btnPick.Click += (s, e) => RunAction(host => host.Machine.OutputUnloader.SupplyEmptyWaferAsync(QMC.CDT320.TargetCassette.Good1, 0));
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
            var unloader = host.Machine.OutputUnloader;
            unloader.FeederY.ResetAlarm();
            unloader.FeederY.ServoOn();
            unloader.BinElevatorZ.ResetAlarm();
            unloader.BinElevatorZ.ServoOn();
            await unloader.FeederY.HomeSearchAsync();
            await unloader.BinElevatorZ.HomeSearchAsync();
        }

        private void RefreshData()
        {
            var host = GetHost();
            if (host?.Machine == null) return;

            var unloader = host.Machine.OutputUnloader;
            lblFeederPos.Text = AxisUnitConverter.FormatDisplay(unloader.FeederY.ActualPosition, unloader.FeederY, "0.###", true);
            lblElevatorPos.Text = AxisUnitConverter.FormatDisplay(unloader.BinElevatorZ.ActualPosition, unloader.BinElevatorZ, "0.###", true);
            lblClamp.Text = unloader.FeederClampCyl.IsFwd ? "CLAMPED" : (unloader.FeederClampCyl.IsBwd ? "OPEN" : "...");
            lblUpDown.Text = unloader.FeederUpDownCyl.IsFwd ? "DOWN" : (unloader.FeederUpDownCyl.IsBwd ? "UP" : "...");
            dotNg.IsOn = unloader.ExistSensor_NG.IsOn;
            dotGood1.IsOn = unloader.ExistSensor_Good1.IsOn;
            dotGood2.IsOn = unloader.ExistSensor_Good2.IsOn;
            dotProtrusion.IsOn = unloader.ProtrusionSensor.IsOn;
            dotDetect.IsOn = unloader.WaferDetectSensor.IsOn;
            dotClamped.IsOn = unloader.WaferClampedSensor.IsOn;
        }
    }
}


