using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class InputCassettePage : PageBase
    {
        private const int SLOT_COUNT_UI = 16;
        private System.Windows.Forms.Timer _refreshTimer;

        public InputCassettePage()
        {
            InitializeComponent();
            WireEvents();

            _refreshTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _refreshTimer.Tick += (s, e) => RefreshFromMachine();
            HandleCreated += (s, e) => _refreshTimer.Start();
            HandleDestroyed += (s, e) => _refreshTimer.Stop();
        }

        private void WireEvents()
        {
            btnPrev.Click += (s, e) => MoveSlotRel(-1);
            btnNext.Click += (s, e) => MoveSlotRel(+1);
            btnInit.Click += (s, e) => RunAction(LifterInitAsync);
            btnReady.Click += (s, e) => RunAction(LifterReadyAsync);
            btnMap.Click += (s, e) => RunAction(MapAsync);
            btnLoad.Click += (s, e) => RunAction(LoadAsync);
            btnUnload.Click += (s, e) => RunAction(UnloadAsync);
        }

        private Form1 GetHost() => FindForm() as Form1;

        private async void RunAction(Func<Form1, Task> action)
        {
            var host = GetHost();
            if (host?.Controller == null) return;

            try
            {
                await action(host);
            }
            catch (Exception ex)
            {
                MessageBox.Show("LotPort error:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            RefreshFromMachine();
        }

        private async Task LifterInitAsync(Form1 host)
        {
            var loader = host.Machine.InputLoader;
            loader.ElevatorZ.ResetAlarm();
            loader.ElevatorZ.ServoOn();
            loader.FeederY.ResetAlarm();
            loader.FeederY.ServoOn();
            await loader.ElevatorZ.HomeSearchAsync();
            await loader.FeederY.HomeSearchAsync();
        }

        private Task LifterReadyAsync(Form1 host)
        {
            var loader = host.Machine.InputLoader;
            loader.ElevatorZ.ServoOn();
            loader.FeederY.ServoOn();
            return Task.CompletedTask;
        }

        private async Task MapAsync(Form1 host)
        {
            await host.Controller.ScanInputCassetteAsync();
        }

        private async Task LoadAsync(Form1 host)
        {
            await host.Controller.LoadNextWaferAsync();
        }

        private async Task UnloadAsync(Form1 host)
        {
            await host.Controller.RetractCurrentWaferAsync();
        }

        private void MoveSlotRel(int delta)
        {
            var host = GetHost();
            if (host?.Machine == null) return;

            var loader = host.Machine.InputLoader;
            double pitch = 6.0;
            _ = loader.ElevatorZ.MoveRelativeAsync(delta * pitch, 20.0);
        }

        private void RefreshFromMachine()
        {
            var host = GetHost();
            if (host?.Machine == null) return;

            var loader = host.Machine.InputLoader;
            var ctrl = host.Controller;

            if (_lifterPosLabel != null)
            {
                _lifterPosLabel.Text = loader.ElevatorZ.ActualPosition.ToString("F3") + " mm";
            }

            var map = loader.WaferMap;
            int curSlot = ctrl != null ? ctrl.CurrentInputSlot : -1;
            for (int i = 0; i < SLOT_COUNT_UI; i++)
            {
                Color c;
                if (map != null && i < map.Count)
                {
                    if (i == curSlot) c = Color.Cyan;
                    else if (map[i]) c = Color.LimeGreen;
                    else c = Color.LightGray;
                }
                else
                {
                    c = Color.LightGray;
                }

                if (_slotLeds[i].BackColor != c) _slotLeds[i].BackColor = c;
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _refreshTimer?.Stop(); _refreshTimer?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}
