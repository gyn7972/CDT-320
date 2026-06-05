using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class PlateStatusPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private const int SLOTS_PER_PLATE = 25;
        private System.Windows.Forms.Timer _timer;

        public PlateStatusPage()
        {
            InitializeComponent();
            btnReset.Click += (s, e) => ResetPlates();

            _timer = new System.Windows.Forms.Timer { Interval = 300 };
            _timer.Tick += (s, e) => Refresh5();
            HandleCreated += (s, e) => _timer.Start();
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private void ResetPlates()
        {
            if (QMC.Common.MessageDialog.Show(
                "Plate loaded data will be reset. Continue?",
                "Plate Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            PlateRegistry.Reset();
            Refresh5();
        }

        private void Refresh5()
        {
            UpdatePlate(PlateRegistry.NgPlate, _ngSlots, _lblNgCount, Color.LightCoral);
            UpdatePlate(PlateRegistry.GoodPlate, _goodSlots, _lblGoodCount, Color.LightGreen);
        }

        private static void UpdatePlate(Plate p, Label[] slotLbls, Label countLbl, Color filledColor)
        {
            countLbl.Text = $"{p.FilledCount} / {p.MaxSlots}";
            for (int i = 0; i < slotLbls.Length; i++)
            {
                int code = (i < p.Slots.Length) ? p.Slots[i] : 0;
                Color c = code > 0 ? filledColor : Color.LightGray;
                if (slotLbls[i].BackColor != c) slotLbls[i].BackColor = c;

                string txt = code > 0 ? code.ToString() : (i + 1).ToString("D2");
                if (slotLbls[i].Text != txt) slotLbls[i].Text = txt;
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _timer?.Stop(); _timer?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}


