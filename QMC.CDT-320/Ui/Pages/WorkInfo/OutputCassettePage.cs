using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT_320.Ui.Controls;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class OutputCassettePage : QMC.CDT_320.Ui.Pages.PageBase
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

            var OutputCassette = host.Machine.OutputCassetteUnit;
            lblElevatorPos.Text = AxisUnitConverter.FormatDisplay(OutputCassette.OutputLifterZ.ActualPosition, OutputCassette.OutputLifterZ, "0.###", true);

            var driver = host.CassetteDriver;
            int slotCount = OutputCassette != null && OutputCassette.Config != null && OutputCassette.Config.SlotCount > 0
                ? OutputCassette.Config.SlotCount
                : 0;

            UpdateView(_ngCassetteView, slotCount, ResolveSlots(OutputCassette, TargetCassette.Ng, driver != null ? driver.OutputNgSlots : null), Color.LightCoral);
            UpdateView(_good1CassetteView, slotCount, ResolveSlots(OutputCassette, TargetCassette.Good1, driver != null ? driver.OutputGood1Slots : null), Color.LimeGreen);
            UpdateView(_good2CassetteView, slotCount, ResolveSlots(OutputCassette, TargetCassette.Good2, driver != null ? driver.OutputGood2Slots : null), Color.LimeGreen);
        }

        private static IReadOnlyList<bool> ResolveSlots(OutputCassetteUnit unit, TargetCassette cassette, bool[] fallback)
        {
            if (unit != null && unit.SlotMap != null)
            {
                bool[] map;
                if (unit.SlotMap.TryGetValue(cassette, out map) && map != null && map.Length > 0)
                    return map;
            }

            return fallback;
        }

        private static void UpdateView(CassetteSlotView view, int slotCount, IReadOnlyList<bool> state, Color filledColor)
        {
            if (view == null)
                return;

            if (slotCount <= 0 && state != null)
                slotCount = state.Count;

            view.SetSlotCount(slotCount);
            view.UpdateSlots(state, -1, filledColor, Color.Cyan);
        }
    }
}
