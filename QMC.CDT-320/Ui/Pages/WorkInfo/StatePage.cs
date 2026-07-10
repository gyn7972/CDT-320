using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Bin;
using QMC.CDT320.Lots;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class StatePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private const int SLOTS_PER_PLATE = 25;
        private System.Windows.Forms.Timer _refresh;

        // NG/GOOD plate 슬롯 라벨. 디자이너가 컨트롤 배열을 직렬화하지 못하므로
        // (CassetteSlotView와 동일하게) 코드에서 런타임 생성한다.
        private Label[] _ngSlots;
        private Label[] _goodSlots;

        public StatePage()
        {
            InitializeComponent();
            BuildPlateSlots();

            if (!IsDesignerMode())
            {
                LotStorage.ActiveLotChanged += OnActiveLotChanged;

                _refresh = new System.Windows.Forms.Timer { Interval = 300 };
                _refresh.Tick += (s, e) =>
                {
                    if (!ShouldRefreshVisible(this))
                        return;

                    RefreshAll();
                };
                VisibleChanged += (s, e) => { if (Visible) _refresh.Start(); else _refresh.Stop(); };
                RefreshAll();
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            ResetPlates();
        }

        private void OnActiveLotChanged(Lot lot)
        {
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnActiveLotChanged(lot))); } catch { }
                return;
            }

            RefreshLot();
        }

        private void RefreshAll()
        {
            RefreshLot();
            RefreshPlates();
            RefreshSensors();
        }

        // ───────────────────────────── ACTIVE LOT ─────────────────────────────

        private void RefreshLot()
        {
            var lot = LotStorage.ActiveLot;
            if (lot == null)
            {
                _lblId.Text = _lblRecipe.Text = _lblState.Text = _lblStart.Text = "(no active lot)";
                _lblProcessed.Text = "0 / 0";
                _lblGood.Text = _lblNg.Text = "0";
                _lblYield.Text = "--";
                _binPanel.Invalidate();
                return;
            }

            _lblId.Text = lot.LotID;
            _lblRecipe.Text = lot.RecipeName;
            _lblState.Text = lot.State.ToString();
            _lblStart.Text = lot.StartedAt.ToString("yyyy-MM-dd HH:mm:ss");
            _lblProcessed.Text = $"{lot.ProcessedDies} / {lot.TotalDies}";
            _lblGood.Text = lot.GoodCount.ToString();
            _lblNg.Text = lot.NgCount.ToString();
            _lblYield.Text = $"{lot.YieldPercent:F1} %";
            _binPanel.Invalidate();
        }

        private void OnPaintBin(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White);
            var lot = LotStorage.ActiveLot;
            if (lot == null || lot.BinDistribution.Count == 0)
            {
                using (var br = new SolidBrush(Color.Gray))
                using (var f = new Font("맑은 고딕", 14F))
                {
                    g.DrawString("(no bin data)", f, br, _binPanel.Width / 2f - 80, _binPanel.Height / 2f - 16);
                }
                return;
            }

            var bins = lot.BinDistribution.OrderBy(kv => kv.Key).ToList();
            int max = bins.Max(kv => kv.Value);
            if (max < 1) max = 1;

            int bw = Math.Max(8, _binPanel.Width / (bins.Count + 2));
            int x = 20;
            int barAreaH = _binPanel.Height - 60;

            using (var labelF = new Font("Consolas", 9F))
            {
                foreach (var kv in bins)
                {
                    int h = (int)(kv.Value * 1.0 / max * barAreaH);
                    int y = _binPanel.Height - 40 - h;
                    var color = BinCodeMap.ConvertToBinCodeColor(kv.Key);
                    using (var br = new SolidBrush(color))
                    {
                        g.FillRectangle(br, x, y, bw - 4, h);
                    }
                    using (var b2 = new SolidBrush(Color.Black))
                    {
                        g.DrawString("b" + kv.Key, labelF, b2, x, _binPanel.Height - 36);
                    }
                    using (var b3 = new SolidBrush(Color.DimGray))
                    {
                        g.DrawString(kv.Value.ToString(), labelF, b3, x, _binPanel.Height - 20);
                    }
                    x += bw;
                }
            }
        }

        // ───────────────────────────── PLATE (NG / GOOD) ─────────────────────────────

        private void BuildPlateSlots()
        {
            _ngSlots = CreateSlots(ngSlotLayout);
            _goodSlots = CreateSlots(goodSlotLayout);
        }

        private static Label[] CreateSlots(TableLayoutPanel host)
        {
            var slots = new Label[SLOTS_PER_PLATE];
            host.SuspendLayout();
            for (int i = 0; i < SLOTS_PER_PLATE; i++)
            {
                var lbl = new Label
                {
                    BackColor = Color.LightGray,
                    BorderStyle = BorderStyle.FixedSingle,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 8F, FontStyle.Bold),
                    ForeColor = Color.Black,
                    Margin = new Padding(1),
                    Text = (i + 1).ToString("D2"),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                slots[i] = lbl;
                host.Controls.Add(lbl, i % 5, i / 5);
            }
            host.ResumeLayout();
            return slots;
        }

        private void RefreshPlates()
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
            RefreshPlates();
        }

        // ───────────────────────────── OPERATION PANEL SENSORS ─────────────────────────────

        private void RefreshSensors()
        {
            var host = FindForm() as Form1;
            if (host?.Machine == null) return;
            var op = host.Machine.OpPanelUnit;
            var res = host.Machine.ResourcesUnit;
            var ion = host.Machine.IonizerUnit;

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
            try { LotStorage.ActiveLotChanged -= OnActiveLotChanged; } catch { }
            try { _refresh?.Stop(); _refresh?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}
