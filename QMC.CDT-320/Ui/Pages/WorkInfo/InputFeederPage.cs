using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class InputFeederPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private System.Windows.Forms.Timer _timer;

        public InputFeederPage()
        {
            InitializeComponent();

            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) => RefreshFromMachine();
            HandleCreated += (s, e) => _timer.Start();
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private Form1 GetHost() => FindForm() as Form1;

        private void RefreshFromMachine()
        {
            var host = GetHost();
            if (host?.Machine == null) return;
            var loader = host.Machine.InputFeederUnit;

            _lblFeederPos.Text = AxisUnitConverter.FormatDisplay(loader.FeederY.ActualPosition, loader.FeederY, "0.###", true);
            _lblClampState.Text = loader.FeederClampCyl.IsFwd ? "CLAMP" : (loader.FeederClampCyl.IsBwd ? "UNCLAMP" : "ERROR");
            _lblClampState.ForeColor = loader.FeederClampCyl.IsFwd || loader.FeederClampCyl.IsBwd ? Color.Black : Color.Red;
            _lblUpDownState.Text = loader.FeederUpDownCyl.IsFwd ? "DOWN" : (loader.FeederUpDownCyl.IsBwd ? "UP" : "--");
            _lblUpDownState.ForeColor = Color.Black;
            _lblExist.Text = loader.WaferClampedSensor.IsOn ? "WAFER" : "--";

            _markRing.BackColor = loader.WaferFeederRingCheckSensor.IsOn ? Color.LimeGreen : Color.Black;
            _markOverload.BackColor = loader.WaferFeederOverloadSensor.IsOn ? Color.Red : Color.Black;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _timer?.Stop(); _timer?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}
