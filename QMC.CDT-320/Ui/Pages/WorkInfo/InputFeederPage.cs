using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class InputFeederPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private System.Windows.Forms.Timer _timer;

        public InputFeederPage()
        {
            InitializeComponent();
            WireEvents();

            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) => RefreshFromMachine();
            HandleCreated += (s, e) => _timer.Start();
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private void WireEvents()
        {
            btnInit.Click += (s, e) => RunAction(host => InitFeederAsync(host));
            btnFwd.Click += (s, e) => RunAction(host => host.Machine.InputLoader.FeederUpDownCyl.MoveFwdAsync());
            btnBwd.Click += (s, e) => RunAction(host => host.Machine.InputLoader.FeederUpDownCyl.MoveBwdAsync());
            btnClamp.Click += (s, e) => RunAction(host => host.Machine.InputLoader.FeederClampCyl.MoveFwdAsync());
            btnUnclamp.Click += (s, e) => RunAction(host => host.Machine.InputLoader.FeederClampCyl.MoveBwdAsync());
        }

        private Form1 GetHost() => FindForm() as Form1;

        private async void RunAction(Func<Form1, Task> action)
        {
            var host = GetHost();
            if (host?.Machine == null) return;

            try
            {
                await action(host);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Feeder error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            RefreshFromMachine();
        }

        private async void RunAction(Func<Form1, Task<bool>> action)
        {
            var host = GetHost();
            if (host?.Machine == null) return;

            try
            {
                await action(host);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Feeder error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            RefreshFromMachine();
        }

        private async Task InitFeederAsync(Form1 host)
        {
            var loader = host.Machine.InputLoader;
            loader.FeederY.ResetAlarm();
            loader.FeederY.ServoOn();
            await loader.FeederY.HomeSearchAsync();
        }

        private void RefreshFromMachine()
        {
            var host = GetHost();
            if (host?.Machine == null) return;
            var loader = host.Machine.InputLoader;

            _lblFeederPos.Text = loader.FeederY.ActualPosition.ToString("F3") + " mm";
            _lblClampState.Text = loader.FeederClampCyl.IsFwd ? "CLAMPED" : (loader.FeederClampCyl.IsBwd ? "OPEN" : "...");
            _lblClampState.ForeColor = loader.FeederClampCyl.IsFwd ? Color.LimeGreen : Color.Black;
            _lblUpDownState.Text = loader.FeederUpDownCyl.IsFwd ? "DOWN" : (loader.FeederUpDownCyl.IsBwd ? "UP" : "...");
            _lblExist.Text = loader.WaferClampedSensor.IsOn ? "WAFER" : "EMPTY";

            _dotRing.IsOn = loader.WaferDetectSensor.IsOn;
            _dotOverload.IsOn = loader.ProtrusionSensor.IsOn;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _timer?.Stop(); _timer?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}

