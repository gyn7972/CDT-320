using System;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class OperationPanelStatusPage : PageBase
    {
        private System.Windows.Forms.Timer _timer;

        public OperationPanelStatusPage()
        {
            InitializeComponent();
            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) => Refresh4();
            HandleCreated += (s, e) => _timer.Start();
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private void Refresh4()
        {
            var host = FindForm() as Form1;
            if (host?.Machine == null) return;
            var op = host.Machine.OpPanel;
            var res = host.Machine.Resources;
            var ion = host.Machine.Ionizer;

            if (op != null)
            {
                _dotStart.IsOn = op.StartButton.IsOn;
                _dotStop.IsOn = op.StopButton.IsOn;
                _dotReset.IsOn = op.ResetButton.IsOn;
                _dotEmgF.IsOn = op.EmgFront.IsOn;
                _dotEmgL.IsOn = op.EmgLeft.IsOn;
                _dotEmgR.IsOn = op.EmgRear.IsOn;

                _ledStartLamp.IsOn = op.StartLamp.IsOn;
                _ledStopLamp.IsOn = op.StopLamp.IsOn;
                _ledResetLamp.IsOn = op.ResetLamp.IsOn;

                _tlRed.IsOn = op.TlRed.IsOn;
                _tlYellow.IsOn = op.TlYellow.IsOn;
                _tlGreen.IsOn = op.TlGreen.IsOn;
                _ledBuzzer.IsOn = op.Buzzer.IsOn;
            }

            if (res != null)
            {
                _dotCda1.IsOn = res.MainCda1Check.IsOn;
                _dotCda2.IsOn = res.MainCda2Check.IsOn;
                _dotVac1.IsOn = res.MainVacuum1Check.IsOn;
                _dotVac2.IsOn = res.MainVacuum2Check.IsOn;
                _dotVac3.IsOn = res.MainVacuum3Check.IsOn;
                _dotVac4.IsOn = res.MainVacuum4Check.IsOn;
            }

            if (ion != null)
            {
                _dotIonizer.IsOn = ion.IsHealthy;
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _timer?.Stop(); _timer?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}
