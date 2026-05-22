using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.Bin;
using QMC.CDT320.Lots;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    /// <summary>현재 활성 Lot 의 통계 + Bin 분포 패널.</summary>
    public class ActiveLotPage : PageBase
    {
        private Label _lblId, _lblRecipe, _lblState, _lblStart, _lblProcessed,
                      _lblGood, _lblNg, _lblYield;
        private Panel _binPanel;
        private System.Windows.Forms.Timer _refresh;

        public ActiveLotPage()
        {
            Controls.Add(CreateSectionHeader("wi.activeLot"));
            BuildBody();

            if (!IsDesignerMode())
            {
                LotStorage.ActiveLotChanged += OnActiveLotChanged;
                _refresh = new System.Windows.Forms.Timer { Interval = 1000 };
                _refresh.Tick += (s, e) => RefreshAll();
                _refresh.Start();
                RefreshAll();
            }
        }

        private void BuildBody()
        {
            var grp = new GroupBox
            {
                Location = new Point(20, 50), Size = new Size(620, 360),
                Text = "Active Lot", Font = UiTheme.SectionFont,
                BackColor = UiTheme.OptionPanelBg
            };

            int yy = 30;
            grp.Controls.Add(MkLabel("Lot ID",      yy));         _lblId        = MkValue(yy); grp.Controls.Add(_lblId);        yy += 36;
            grp.Controls.Add(MkLabel("Recipe",      yy));         _lblRecipe    = MkValue(yy); grp.Controls.Add(_lblRecipe);    yy += 36;
            grp.Controls.Add(MkLabel("State",       yy));         _lblState     = MkValue(yy); grp.Controls.Add(_lblState);     yy += 36;
            grp.Controls.Add(MkLabel("Started",     yy));         _lblStart     = MkValue(yy); grp.Controls.Add(_lblStart);     yy += 36;
            grp.Controls.Add(MkLabel("Processed / Total", yy));   _lblProcessed = MkValue(yy); grp.Controls.Add(_lblProcessed); yy += 36;
            grp.Controls.Add(MkLabel("Good",        yy));         _lblGood      = MkValue(yy); grp.Controls.Add(_lblGood);      yy += 36;
            grp.Controls.Add(MkLabel("NG",          yy));         _lblNg        = MkValue(yy); grp.Controls.Add(_lblNg);        yy += 36;
            grp.Controls.Add(MkLabel("Yield %",     yy));         _lblYield     = MkValue(yy); grp.Controls.Add(_lblYield);
            Controls.Add(grp);

            var bgrp = new GroupBox
            {
                Location = new Point(660, 50), Size = new Size(800, 360),
                Text = "Bin distribution", Font = UiTheme.SectionFont,
                BackColor = UiTheme.OptionPanelBg
            };
            _binPanel = new Panel
            {
                Location = new Point(8, 28), Size = new Size(784, 320),
                BackColor = Color.White
            };
            _binPanel.Paint += OnPaintBin;
            bgrp.Controls.Add(_binPanel);
            Controls.Add(bgrp);
        }

        private Label MkLabel(string text, int yy)
            => new Label
            {
                Location = new Point(16, yy), Size = new Size(180, 26),
                Text = text, Font = UiTheme.ButtonFont, ForeColor = Color.DarkSlateGray
            };
        private Label MkValue(int yy)
            => new Label
            {
                Location = new Point(200, yy - 2), Size = new Size(380, 30),
                Text = "(none)", Font = new Font("Consolas", 12F, FontStyle.Bold),
                ForeColor = Color.Black, BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
            };

        private void OnActiveLotChanged(Lot lot)
        {
            if (InvokeRequired) { try { BeginInvoke(new Action(() => OnActiveLotChanged(lot))); } catch { } return; }
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
                _lblYield.Text = "—";
                _binPanel.Invalidate();
                return;
            }
            _lblId       .Text = lot.LotID;
            _lblRecipe   .Text = lot.RecipeName;
            _lblState    .Text = lot.State.ToString();
            _lblStart    .Text = lot.StartedAt.ToString("yyyy-MM-dd HH:mm:ss");
            _lblProcessed.Text = $"{lot.ProcessedDies} / {lot.TotalDies}";
            _lblGood     .Text = lot.GoodCount.ToString();
            _lblNg       .Text = lot.NgCount.ToString();
            _lblYield    .Text = $"{lot.YieldPercent:F1} %";
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
                using (var f = new Font("Segoe UI", 14F))
                    g.DrawString("(no bin data)", f, br, _binPanel.Width / 2f - 80, _binPanel.Height / 2f - 16);
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
                        g.FillRectangle(br, x, y, bw - 4, h);
                    using (var b2 = new SolidBrush(Color.Black))
                        g.DrawString("b" + kv.Key, labelF, b2, x, _binPanel.Height - 36);
                    using (var b3 = new SolidBrush(Color.DimGray))
                        g.DrawString(kv.Value.ToString(), labelF, b3, x, _binPanel.Height - 20);
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
