using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.Bin;
using QMC.CDT320.Lots;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class ActiveLotPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private System.Windows.Forms.Timer _refresh;

        public ActiveLotPage()
        {
            InitializeComponent();

            if (!IsDesignerMode())
            {
                LotStorage.ActiveLotChanged += OnActiveLotChanged;
                _refresh = new System.Windows.Forms.Timer { Interval = 1000 };
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

        private void OnActiveLotChanged(Lot lot)
        {
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnActiveLotChanged(lot))); } catch { }
                return;
            }

            RefreshAll();
        }

        private void RefreshAll()
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

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { LotStorage.ActiveLotChanged -= OnActiveLotChanged; } catch { }
            try { _refresh?.Stop(); _refresh?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}

