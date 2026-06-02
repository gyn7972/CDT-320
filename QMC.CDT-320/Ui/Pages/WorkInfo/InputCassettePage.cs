using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320.Sequencing;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class InputCassettePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private const int SLOT_COUNT_UI = 16;
        private System.Windows.Forms.Timer _refreshTimer;
        private bool _manualSequenceRunning;

        public InputCassettePage()
        {
            InitializeComponent();
            BuildSlotRows();   // ← 누락된 초기화
            WireEvents();

            _refreshTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _refreshTimer.Tick += (s, e) => RefreshFromMachine();
            HandleCreated += (s, e) => _refreshTimer.Start();
            HandleDestroyed += (s, e) => _refreshTimer.Stop();
        }

        private void BuildSlotRows()
        {
            _slotLeds = new Label[SLOT_COUNT_UI];
            _slotIndexLbls = new Label[SLOT_COUNT_UI];
            _slotNameLbls = new Label[SLOT_COUNT_UI];

            lifterLayout.SuspendLayout();
            for (int i = 0; i < SLOT_COUNT_UI; i++)
            {
                _slotIndexLbls[i] = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = (i + 1).ToString(),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                _slotLeds[i] = new Label
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.LightGray,
                    BorderStyle = BorderStyle.FixedSingle
                };
                _slotNameLbls[i] = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = $"SLOT {i + 1}",
                    TextAlign = ContentAlignment.MiddleLeft
                };

                lifterLayout.Controls.Add(_slotIndexLbls[i], 0, i);
                lifterLayout.Controls.Add(_slotLeds[i],      1, i);
                lifterLayout.Controls.Add(_slotNameLbls[i],  2, i);
            }
            lifterLayout.ResumeLayout();
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
            if (_manualSequenceRunning) return;

            IDisposable manualScope = null;
            try
            {
                _manualSequenceRunning = true;
                SetActionButtonsEnabled(false);
                manualScope = host.Controller.EnterManualOperation();
                await action(host);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("LotPort error:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (manualScope != null)
                    manualScope.Dispose();
                _manualSequenceRunning = false;
                SetActionButtonsEnabled(true);
                RefreshFromMachine();
            }
        }

        private void SetActionButtonsEnabled(bool enabled)
        {
            btnPrev.Enabled = enabled;
            btnNext.Enabled = enabled;
            btnInit.Enabled = enabled;
            btnReady.Enabled = enabled;
            btnMap.Enabled = enabled;
            btnLoad.Enabled = enabled;
            btnUnload.Enabled = enabled;
        }

        private async Task LifterInitAsync(Form1 host)
        {
            var cassette = host.Machine.InputCassette;
            var feeder = host.Machine.InputFeeder;
            cassette.WaferLifterZ.ResetAlarm();
            cassette.WaferLifterZ.ServoOn();
            feeder.FeederY.ResetAlarm();
            feeder.FeederY.ServoOn();
            await cassette.WaferLifterZ.HomeSearchAsync();
            await feeder.FeederY.HomeSearchAsync();
        }

        private Task LifterReadyAsync(Form1 host)
        {
            var cassette = host.Machine.InputCassette;
            var feeder = host.Machine.InputFeeder;
            cassette.WaferLifterZ.ServoOn();
            feeder.FeederY.ServoOn();
            return Task.CompletedTask;
        }

        private async Task MapAsync(Form1 host)
        {
            var sequence = CreateManualInputSequence(host);
            await sequence.ExecuteMappingAsync(CancellationToken.None);
        }

        private async Task LoadAsync(Form1 host)
        {
            var sequence = CreateManualInputSequence(host);
            await sequence.ExecuteCassetteLoadingAsync(CancellationToken.None);
        }

        private async Task UnloadAsync(Form1 host)
        {
            var sequence = CreateManualInputSequence(host);
            await sequence.ExecuteCassetteUnloadingAsync(CancellationToken.None);
        }

        private InputSequence CreateManualInputSequence(Form1 host)
        {
            var ctx = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
            var sequence = new InputSequence(ctx);
            sequence.Configure(SequenceRunMode.Manual);
            return sequence;
        }

        private void MoveSlotRel(int delta)
        {
            var host = GetHost();
            if (host?.Machine == null) return;

            var loader = host.Machine.InputCassette;
            double pitch = 6.0;
            _ = loader.WaferLifterZ.MoveRelativeAsync(delta * pitch, 20.0);
        }

        private void RefreshFromMachine()
        {
            var host = GetHost();
            if (host?.Machine == null) return;

            var loader = host.Machine.InputCassette;
            var ctrl = host.Controller;

            if (_lifterPosLabel != null)
            {
                _lifterPosLabel.Text = AxisUnitConverter.FormatDisplay(loader.WaferLifterZ.ActualPosition, loader.WaferLifterZ, "0.###", true);
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


