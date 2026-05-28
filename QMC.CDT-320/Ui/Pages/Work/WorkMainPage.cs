using System;
using System.Windows.Forms;
using QMC.CDT320.Lots;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Work
{
    public partial class WorkMainPage : PageBase
    {
        private Timer _refresh;
        private readonly DateTime _runStart = DateTime.Now;
        private int _lastDone;
        private long _cycleAccMs;
        private int _cycleSamples;
        private long _lastCycleStartTicks;

        public WorkMainPage()
        {
            InitializeComponent();
            WireEvents();

            if (!IsDesignerMode())
            {
                _refresh = new Timer { Interval = 1000 };
                _refresh.Tick += (s, e) => RefreshAll();
                _refresh.Start();
                RefreshAll();
            }
        }

        private void WireEvents()
        {
            btnCcs.Click += (s, e) =>
            {
                try
                {
                    QMC.CDT320.Logging.EventLogger.Write(
                        QMC.CDT320.Logging.EventKind.Event,
                        QMC.CDT_320.Ui.Security.UserSession.Name,
                        "CCS-CHECK",
                        "CCS check button clicked.");
                }
                catch { }

                MessageBox.Show(
                    "CCS check page will be connected in the next work step.",
                    btnCcs.Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };
        }

        private void RefreshAll()
        {
            try
            {
                Form1 host = ParentForm as Form1 ?? FindForm() as Form1;
                var ctrl = host?.Controller;
                var lot = LotStorage.ActiveLot;

                int total = lot?.ProcessedDies ?? 0;
                int currBin = -1;
                if (lot?.BinDistribution != null && lot.BinDistribution.Count > 0)
                {
                    int max = -1;
                    foreach (var kv in lot.BinDistribution)
                    {
                        if (kv.Value > max)
                        {
                            max = kv.Value;
                            currBin = kv.Key;
                        }
                    }
                }

                lblTotalChip.Text = total.ToString();
                lblBinNum.Text = currBin >= 0 ? currBin.ToString() : "--";
                lblStageInfo.Text = "STAGE\r\nW : 640\r\nH : 480\r\nframe : " + total;
                lblLive.Text = ctrl == null ? "Idle" : "Live  [" + ctrl.Status + "]";

                string project = "--";
                try { project = host?.Machine?.Recipe?.ProductId ?? "--"; } catch { }
                if (lot != null && !string.IsNullOrEmpty(lot.RecipeName))
                {
                    project = lot.RecipeName;
                }
                else
                {
                    try
                    {
                        var list = RecipeStore.List();
                        if (list != null && list.Count > 0)
                            project = System.IO.Path.GetFileNameWithoutExtension(list[0]);
                    }
                    catch { }
                }

                lblProject.Text = project;
                lblPickFail.Text = (ctrl?.PickFailCount ?? 0) + " ea";
                lblPlaceFail.Text = (ctrl?.PlaceFailCount ?? 0) + " ea";
                lblBinQty.Text = (lot?.GoodCount ?? 0) + " ea";
                lblCollet1.Text = (ctrl?.Collet1UseCount ?? 0).ToString();
                lblCollet2.Text = (ctrl?.Collet2UseCount ?? 0).ToString();
                lblNeedle.Text = (ctrl?.NeedleUseCount ?? 0).ToString();

                TimeSpan upTime = lot != null ? lot.Duration : DateTime.Now - _runStart;
                TimeSpan normalDown = ctrl?.NormalDownTime ?? TimeSpan.Zero;
                TimeSpan errorDown = ctrl?.ErrorDownTime ?? TimeSpan.Zero;

                lblLoad.Text = FormatTs(upTime);
                lblUp.Text = FormatTs(upTime);
                lblContUp.Text = FormatTs(upTime);
                lblNormDown.Text = FormatTs(normalDown);
                lblErrDown.Text = FormatTs(errorDown);
                lblErrCnt.Text = (ctrl?.ErrorCount ?? 0) + " ea";
                lblRecovery.Text = FormatTs(ctrl?.RecoveryTime ?? TimeSpan.Zero);

                double uph = 0;
                if (lot != null && lot.GoodCount > 0 && upTime.TotalSeconds > 0.5)
                    uph = lot.GoodCount * 3600.0 / upTime.TotalSeconds;
                lblUph.Text = uph.ToString("F2");

                lblMtbf.Text = FormatTs(ctrl?.Mtbf ?? TimeSpan.Zero);
                lblMttr.Text = FormatTs(ctrl?.Mttr ?? TimeSpan.Zero);

                int doneNow = lot?.ProcessedDies ?? 0;
                if (doneNow > _lastDone)
                {
                    long now = DateTime.UtcNow.Ticks;
                    if (_lastCycleStartTicks > 0)
                    {
                        long ms = (now - _lastCycleStartTicks) / TimeSpan.TicksPerMillisecond;
                        int doneDelta = doneNow - _lastDone;
                        if (doneDelta > 0)
                        {
                            _cycleAccMs += ms;
                            _cycleSamples += doneDelta;
                        }
                    }

                    _lastCycleStartTicks = now;
                    _lastDone = doneNow;
                }

                lblCycle.Text = (_cycleSamples > 0 ? _cycleAccMs / _cycleSamples : 0) + " ms";

                double rate = lot != null && lot.ProcessedDies > 0
                    ? lot.GoodCount * 100.0 / lot.ProcessedDies
                    : 0;
                lblRate.Text = rate.ToString("F2") + " %";
                lblLot.Text = lot?.LotID ?? "(no lot)";
            }
            catch
            {
                // Ignore transient refresh failures while the form is closing or machine state is changing.
            }
        }

        private static string FormatTs(TimeSpan ts)
        {
            if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;
            return string.Format("{0:00}:{1:00}:{2:00}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                _refresh?.Stop();
                _refresh?.Dispose();
            }
            catch { }

            base.OnHandleDestroyed(e);
        }
    }
}
