using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class OutputCassettePage : PageBase
    {
        private Timer _timer;

        public OutputCassettePage()
        {
            InitializeComponent();
            WireEvents();

            _timer = new Timer { Interval = 200 };
            _timer.Tick += (s, e) => RefreshData();
            HandleCreated += (s, e) => _timer.Start();
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private void WireEvents()
        {
            btnMap.Click += async (s, e) =>
            {
                var host = FindForm() as Form1;
                if (host?.Controller == null) return;

                try { await host.Controller.ScanOutputCassettesAsync(); }
                catch { }

                RefreshData();
            };
        }

        private void RefreshData()
        {
            var host = FindForm() as Form1;
            if (host?.Machine == null) return;

            var unloader = host.Machine.OutputUnloader;
            lblElevatorPos.Text = unloader.ElevatorZ.ActualPosition.ToString("F3") + " mm";

            var driverProp = host.GetType().GetProperty("CassetteDriver",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var driver = driverProp?.GetValue(host) as QMC.CDT320.Sim.SimCassetteDriver;

            UpdateLeds(ngLeds, driver?.OutputNgSlots, Color.LightCoral);
            UpdateLeds(good1Leds, driver?.OutputGood1Slots, Color.LimeGreen);
            UpdateLeds(good2Leds, driver?.OutputGood2Slots, Color.LimeGreen);
        }

        private static void UpdateLeds(Label[] leds, bool[] state, Color filledColor)
        {
            for (int i = 0; i < leds.Length; i++)
            {
                bool filled = state != null && i < state.Length && state[i];
                Color color = filled ? filledColor : Color.LightGray;
                if (leds[i].BackColor != color) leds[i].BackColor = color;
            }
        }
    }
}
