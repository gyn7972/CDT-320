using System;
using System.Windows.Forms;
using QMC.CDT320.Lots;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Work
{
    public partial class WorkMainPage : PageBase
    {
        // UI 적용 주기(250~500ms). 표시 갱신만 수행하며 저장/모션 루프와 분리된다.
        private const int RefreshIntervalMs = 500;

        private Timer _refresh;
        private readonly DateTime _runStart = DateTime.Now;
        private int _lastDone;
        private long _cycleAccMs;
        private int _cycleSamples;
        private long _lastCycleStartTicks;
        private bool _eventsHooked;

        public WorkMainPage()
        {
            InitializeComponent();
            WireEvents();

            if (!IsDesignerMode())
            {
                HookStateEvents();

                _refresh = new Timer { Interval = RefreshIntervalMs };
                _refresh.Tick += (s, e) =>
                {
                    if (!ShouldRefreshVisible(this))
                        return;

                    RefreshAll();
                };
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
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        QMC.CDT_320.Ui.Security.UserSession.Name,
                        "CCS-CHECK",
                        "CCS check button clicked.");
                }
                catch { }

                QMC.Common.MessageDialog.Show(
                    "CCS check page will be connected in the next work step.",
                    btnCcs.Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };
        }

        private void HookStateEvents()
        {
            if (_eventsHooked)
                return;

            // Lot 변경은 비-UI 스레드에서 올 수 있으므로 BeginInvoke 로만 가볍게 표시 갱신을 예약한다.
            LotStorage.ActiveLotChanged += OnActiveLotChanged;
            _eventsHooked = true;
        }

        private void UnhookStateEvents()
        {
            if (!_eventsHooked)
                return;

            LotStorage.ActiveLotChanged -= OnActiveLotChanged;
            _eventsHooked = false;
        }

        private void OnActiveLotChanged(Lot lot)
        {
            try
            {
                if (!IsHandleCreated || IsDisposed)
                    return;

                BeginInvoke((Action)(() =>
                {
                    if (ShouldRefreshVisible(this))
                        RefreshAll();
                }));
            }
            catch
            {
            }
        }

        private void RefreshAll()
        {
            try
            {
                WorkMainDisplaySnapshot snapshot = BuildDisplaySnapshot();
                ApplyDisplaySnapshot(snapshot);
            }
            catch
            {
                // Ignore transient refresh failures while the form is closing or machine state is changing.
            }
        }

        /// <summary>
        /// Lot / Controller counter / Cycle 통계를 한 번에 읽어 표시 문자열 스냅샷을 만든다.
        /// (서비스 접근을 Build 한 곳에 모으고, 실제 컨트롤 반영은 Apply 에서 diff 로 처리한다.)
        /// </summary>
        private WorkMainDisplaySnapshot BuildDisplaySnapshot()
        {
            var snap = new WorkMainDisplaySnapshot();

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

            snap.TotalChip = total.ToString();
            snap.BinNum = currBin >= 0 ? currBin.ToString() : "--";
            snap.StageInfo = "STAGE\r\nW : 640\r\nH : 480\r\nframe : " + total;
            snap.Live = ctrl == null ? "Idle" : "Live  [" + ctrl.Status + "]";

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

            snap.Project = project;
            snap.PickFail = (ctrl?.PickFailCount ?? 0) + " ea";
            snap.PlaceFail = (ctrl?.PlaceFailCount ?? 0) + " ea";
            snap.BinQty = (lot?.GoodCount ?? 0) + " ea";
            snap.Collet1 = (ctrl?.Collet1UseCount ?? 0).ToString();
            snap.Collet2 = (ctrl?.Collet2UseCount ?? 0).ToString();
            snap.Needle = (ctrl?.NeedleUseCount ?? 0).ToString();

            TimeSpan upTime = lot != null ? lot.Duration : DateTime.Now - _runStart;
            TimeSpan normalDown = ctrl?.NormalDownTime ?? TimeSpan.Zero;
            TimeSpan errorDown = ctrl?.ErrorDownTime ?? TimeSpan.Zero;

            string upText = FormatTs(upTime);
            snap.Load = upText;
            snap.Up = upText;
            snap.ContUp = upText;
            snap.NormDown = FormatTs(normalDown);
            snap.ErrDown = FormatTs(errorDown);
            snap.ErrCnt = (ctrl?.ErrorCount ?? 0) + " ea";
            snap.Recovery = FormatTs(ctrl?.RecoveryTime ?? TimeSpan.Zero);

            double uph = 0;
            if (lot != null && lot.GoodCount > 0 && upTime.TotalSeconds > 0.5)
                uph = lot.GoodCount * 3600.0 / upTime.TotalSeconds;
            snap.Uph = uph.ToString("F2");

            snap.Mtbf = FormatTs(ctrl?.Mtbf ?? TimeSpan.Zero);
            snap.Mttr = FormatTs(ctrl?.Mttr ?? TimeSpan.Zero);

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

            snap.Cycle = (_cycleSamples > 0 ? _cycleAccMs / _cycleSamples : 0) + " ms";

            double rate = lot != null && lot.ProcessedDies > 0
                ? lot.GoodCount * 100.0 / lot.ProcessedDies
                : 0;
            snap.Rate = rate.ToString("F2") + " %";
            snap.Lot = lot?.LotID ?? "(no lot)";

            return snap;
        }

        /// <summary>스냅샷을 라벨에 반영한다. 값이 다를 때만 Text 를 바꿔 레이아웃 churn 을 줄인다.</summary>
        private void ApplyDisplaySnapshot(WorkMainDisplaySnapshot s)
        {
            if (s == null)
                return;

            SetText(lblTotalChip, s.TotalChip);
            SetText(lblBinNum, s.BinNum);
            SetText(lblStageInfo, s.StageInfo);
            SetText(lblLive, s.Live);
            SetText(lblProject, s.Project);
            SetText(lblPickFail, s.PickFail);
            SetText(lblPlaceFail, s.PlaceFail);
            SetText(lblBinQty, s.BinQty);
            SetText(lblCollet1, s.Collet1);
            SetText(lblCollet2, s.Collet2);
            SetText(lblNeedle, s.Needle);
            SetText(lblLoad, s.Load);
            SetText(lblUp, s.Up);
            SetText(lblContUp, s.ContUp);
            SetText(lblNormDown, s.NormDown);
            SetText(lblErrDown, s.ErrDown);
            SetText(lblErrCnt, s.ErrCnt);
            SetText(lblRecovery, s.Recovery);
            SetText(lblUph, s.Uph);
            SetText(lblMtbf, s.Mtbf);
            SetText(lblMttr, s.Mttr);
            SetText(lblCycle, s.Cycle);
            SetText(lblRate, s.Rate);
            SetText(lblLot, s.Lot);
        }

        private static void SetText(Control control, string text)
        {
            if (control == null)
                return;
            text = text ?? string.Empty;
            if (!string.Equals(control.Text, text, StringComparison.Ordinal))
                control.Text = text;
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
                UnhookStateEvents();
                _refresh?.Stop();
                _refresh?.Dispose();
            }
            catch { }

            base.OnHandleDestroyed(e);
        }

        /// <summary>작업 메인 화면 표시용 스냅샷 모델. (UI 표시 문자열만 보관)</summary>
        private sealed class WorkMainDisplaySnapshot
        {
            public string TotalChip;
            public string BinNum;
            public string StageInfo;
            public string Live;
            public string Project;
            public string PickFail;
            public string PlaceFail;
            public string BinQty;
            public string Collet1;
            public string Collet2;
            public string Needle;
            public string Load;
            public string Up;
            public string ContUp;
            public string NormDown;
            public string ErrDown;
            public string ErrCnt;
            public string Recovery;
            public string Uph;
            public string Mtbf;
            public string Mttr;
            public string Cycle;
            public string Rate;
            public string Lot;
        }
    }
}
