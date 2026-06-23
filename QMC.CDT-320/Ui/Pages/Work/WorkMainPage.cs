using System;
using System.Windows.Forms;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT320.Stats;
using QMC.Common.Alarms;

namespace QMC.CDT_320.Ui.Pages.Work
{
    public partial class WorkMainPage : PageBase
    {
        // UI 적용 주기(250~500ms). 표시 갱신만 수행하며 저장/모션 루프와 분리된다.
        private const int RefreshIntervalMs = 500;

        private Timer _refresh;
        private ToolTip _workTimeToolTip;
        private bool _eventsHooked;

        public WorkMainPage()
        {
            InitializeComponent();
            WireEvents();
            InitializeWorkTimeToolTips();

            if (!IsDesignerMode())
            {
                HookStateEvents();

                _refresh = new Timer { Interval = RefreshIntervalMs };
                _refresh.Tick += (s, e) =>
                {
                    if (!ShouldRefreshVisible(this))
                    {
                        _refresh.Stop();
                        return;
                    }

                    RefreshAll();
                };
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_workTimeToolTip != null)
                {
                    _workTimeToolTip.Dispose();
                    _workTimeToolTip = null;
                }
            }

            base.Dispose(disposing);
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

            btnTestAlarm.Click += (s, e) =>
            {
                try
                {
                    QMC.Common.Logging.EventLogger.Write(
                        QMC.Common.Logging.EventKind.Event,
                        QMC.CDT_320.Ui.Security.UserSession.Name,
                        "TEST-ALARM",
                        "작업 메인 화면에서 테스트 알람 버튼을 눌렀습니다.");
                }
                catch { }

                AlarmManager.Raise(
                    AlarmSeverity.Critical,
                    "TEST-ALARM",
                    "WorkMainPage",
                    "테스트 알람이 발생했습니다. 오토/메뉴얼 시퀀스 정지 및 축 긴급 정지 응답을 확인하세요.");
            };
        }

        private void InitializeWorkTimeToolTips()
        {
            try
            {
                _workTimeToolTip = new ToolTip
                {
                    AutoPopDelay = 30000,
                    InitialDelay = 400,
                    ReshowDelay = 100,
                    ShowAlways = true
                };

                SetMetricToolTip(
                    lblUphCaption,
                    lblUph,
                    "UPH(Units Per Hour)\r\n" +
                    "최근 Cycle Time 기준으로 1시간 동안 처리 가능한 Die 수를 환산합니다.\r\n" +
                    "통계 엔진에 실제 처리 수량이 기록된 경우에만 표시합니다.");

                SetMetricToolTip(
                    lblCycleCaption,
                    lblCycle,
                    "Cycle Time\r\n" +
                    "최근 20개 Die의 처리 시간을 다이당 ms로 평균낸 값입니다.\r\n" +
                    "처리 기록이 없으면 0 ms로 표시합니다.");

                SetMetricToolTip(
                    lblMtbfCaption,
                    lblMtbf,
                    "MTBF(Mean Time Between Failures)\r\n" +
                    "가동시간을 이상 정지 횟수로 나눈 평균 고장 간격입니다.\r\n" +
                    "이상 정지 횟수가 없으면 00:00:00으로 표시합니다.");

                SetMetricToolTip(
                    lblMttrCaption,
                    lblMttr,
                    "MTTR(Mean Time To Repair)\r\n" +
                    "이상 정지 시간을 이상 정지 횟수로 나눈 평균 복구 시간입니다.\r\n" +
                    "알람/이상정지 기준으로 누적됩니다.");

                SetMetricToolTip(
                    lblRateCaption,
                    lblRate,
                    "가동률\r\n" +
                    "가동시간 / 부하시간 x 100 으로 계산합니다.\r\n" +
                    "부하시간은 가동 + 정상정지 + 이상정지 시간을 기준으로 보정합니다.");
            }
            catch
            {
            }
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

            // 작업 시간/UPH 통계는 엔진 스냅샷 1회 읽기로 끝낸다(계산은 엔진이 수행, UI는 표시만).
            ProductionStatsSnapshot stats = ctrl?.Stats?.GetSnapshot() ?? ProductionStatsSnapshot.Empty;
            MaterialDisplaySnapshot material = BuildMaterialDisplaySnapshot();
            bool useMaterialCounters = stats.ProcessedDies <= 0 && material.HasMaterial;

            int total = useMaterialCounters ? material.ProcessedCount : stats.ProcessedDies;
            int good = useMaterialCounters ? material.GoodCount : stats.GoodCount;
            int ng = useMaterialCounters ? material.NgCount : stats.NgCount;
            if (material.ProcessedCount > total)
                total = material.ProcessedCount;
            if (material.GoodCount > good)
                good = material.GoodCount;
            if (material.NgCount > ng)
                ng = material.NgCount;

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
            if (currBin < 0 && material.CurrentBinCode > 0)
                currBin = material.CurrentBinCode;

            snap.TotalChip = material.TargetCount > 0
                ? total + " / " + material.TargetCount
                : total.ToString();
            snap.BinNum = currBin >= 0 ? currBin.ToString() : "--";
            snap.StageInfo =
                "STAGE\r\nTOTAL : " + total +
                "\r\nGOOD : " + good +
                "\r\nNG : " + ng +
                "\r\nPICK : " + material.PickedCount;
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
            snap.BinQty = good + " ea";
            snap.Collet1 = (ctrl?.Collet1UseCount ?? 0).ToString();
            snap.Collet2 = (ctrl?.Collet2UseCount ?? 0).ToString();
            snap.Needle = (ctrl?.NeedleUseCount ?? 0).ToString();

            // 화면 수량은 Material 복구값을 참고할 수 있지만, UPH/Cycle은 통계 엔진 값만 사용한다.
            // 복구된 Material 누적 수량과 방금 시작한 통계 시간을 섞으면 UPH가 비정상적으로 커진다.
            int statsTotal = stats.ProcessedDies;
            int statsGood = stats.GoodCount;

            // 가동/정지 시간은 엔진이 확정한 초 값을 hh:mm:ss로 포맷한다.
            double upSeconds = stats.UpSeconds;
            double measuredLoadSeconds = upSeconds + stats.NormalDownSeconds + stats.ErrorDownSeconds;
            double loadSeconds = stats.LoadSeconds > 0 ? stats.LoadSeconds : measuredLoadSeconds;
            if (measuredLoadSeconds > loadSeconds)
                loadSeconds = measuredLoadSeconds;
            if (loadSeconds < upSeconds)
                loadSeconds = upSeconds;
            snap.Load = FormatTs(TimeSpan.FromSeconds(loadSeconds));
            snap.Up = FormatTs(TimeSpan.FromSeconds(upSeconds));
            snap.ContUp = FormatTs(TimeSpan.FromSeconds(stats.ContUpSeconds));
            snap.NormDown = FormatTs(TimeSpan.FromSeconds(stats.NormalDownSeconds));
            snap.ErrDown = FormatTs(TimeSpan.FromSeconds(stats.ErrorDownSeconds));
            snap.ErrCnt = stats.ErrorCount + " ea";
            snap.Recovery = FormatTs(TimeSpan.FromSeconds(stats.RecoverySeconds));

            // UPH 화면 기본값은 순간 UPH(실효 UPH는 엔진 스냅샷에 함께 보관됨).
            double uph = 0.0;
            if (statsTotal > 0 || statsGood > 0)
                uph = stats.UphInstant > 0 ? stats.UphInstant : stats.UphEffective;
            snap.Uph = uph.ToString("F2");

            snap.Mtbf = FormatTs(TimeSpan.FromSeconds(stats.MtbfSeconds));
            snap.Mttr = FormatTs(TimeSpan.FromSeconds(stats.MttrSeconds));

            // CYCLE TIME = 다이당 ms (최근 20 다이 Rolling 평균).
            double cycleMs = statsTotal > 0 ? stats.CycleMsPerDieRolling : 0.0;
            snap.Cycle = ((int)Math.Round(cycleMs)) + " ms";

            // 가동률(%) = 가동시간 / 부하시간 × 100 (수율이 아님).
            double uptimeRate = loadSeconds > 0 ? upSeconds / loadSeconds * 100.0 : 0.0;
            snap.Rate = uptimeRate.ToString("F2") + " %";
            snap.Lot = ResolveDisplayLotId(stats, lot, material);

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

        private void SetMetricToolTip(Control caption, Control value, string text)
        {
            try
            {
                if (_workTimeToolTip == null)
                    return;

                if (caption != null)
                    _workTimeToolTip.SetToolTip(caption, text);
                if (value != null)
                    _workTimeToolTip.SetToolTip(value, text);
            }
            catch
            {
            }
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

        private static string ResolveDisplayLotId(ProductionStatsSnapshot stats, Lot lot, MaterialDisplaySnapshot material)
        {
            if (!string.IsNullOrEmpty(stats.ActiveLotId))
                return stats.ActiveLotId;
            if (lot != null && !string.IsNullOrEmpty(lot.LotID))
                return lot.LotID;
            if (!string.IsNullOrEmpty(material.LotId))
                return material.LotId;
            return "(no lot)";
        }

        private static MaterialDisplaySnapshot BuildMaterialDisplaySnapshot()
        {
            var display = new MaterialDisplaySnapshot();

            try
            {
                MaterialSnapshot state = MaterialStorage.State;
                if (state == null)
                    return display;

                display.LotId = state.LotId ?? string.Empty;

                if (state.Dies != null)
                {
                    foreach (DieMaterial die in state.Dies)
                    {
                        if (die == null || !die.IsInputTarget)
                            continue;

                        display.HasMaterial = true;
                        display.TargetCount++;

                        if (die.Result == DieResult.Good)
                        {
                            display.GoodCount++;
                            display.ProcessedCount++;
                        }
                        else if (die.Result == DieResult.NG)
                        {
                            display.NgCount++;
                            display.ProcessedCount++;
                        }
                        else if (IsOutputLocation(die.CurrentLocation))
                        {
                            display.ProcessedCount++;
                        }

                        if (IsPickerLocation(die.CurrentLocation))
                            display.PickedCount++;

                        if (die.Output_BinCode > 0)
                            display.CurrentBinCode = die.Output_BinCode;
                    }
                }

                ApplyOutputReceiveSlotFallback(state, display);
            }
            catch
            {
                // 화면 표시 보강 실패는 생산 로직에 영향을 주지 않는다.
            }
            finally
            {
            }

            return display;
        }

        private static void ApplyOutputReceiveSlotFallback(MaterialSnapshot state, MaterialDisplaySnapshot display)
        {
            if (state == null || state.Wafers == null)
                return;

            int slotProcessed = 0;
            int slotGood = 0;
            int slotNg = 0;

            foreach (WaferMaterial wafer in state.Wafers)
            {
                if (wafer == null || !IsOutputWafer(wafer.CurrentLocation))
                    continue;

                display.HasMaterial = true;
                if (!string.IsNullOrEmpty(wafer.CassetteLotId) && string.IsNullOrEmpty(display.LotId))
                    display.LotId = wafer.CassetteLotId;

                int waferReceived = Math.Max(0, wafer.OutputReceiveNextIndex);
                int slotDetected = 0;

                if (wafer.OutputReceiveSlots == null)
                {
                    slotProcessed += waferReceived;
                    continue;
                }

                for (int i = 0; i < wafer.OutputReceiveSlots.Count; i++)
                {
                    OutputReceiveSlotMaterial slot = wafer.OutputReceiveSlots[i];
                    if (slot == null)
                        continue;

                    bool received = !string.IsNullOrEmpty(slot.DieUid) ||
                                    (wafer.OutputReceiveNextIndex > 0 && i < wafer.OutputReceiveNextIndex);
                    if (received)
                        slotDetected++;

                    if (slot.Result == DieResult.Good)
                        slotGood++;
                    else if (slot.Result == DieResult.NG)
                        slotNg++;

                    if (slot.BinCode > 0)
                        display.CurrentBinCode = slot.BinCode;
                }

                if (slotDetected > waferReceived)
                    waferReceived = slotDetected;
                slotProcessed += waferReceived;
            }

            if (slotGood > display.GoodCount)
                display.GoodCount = slotGood;
            if (slotNg > display.NgCount)
                display.NgCount = slotNg;
            if (slotProcessed > display.ProcessedCount)
                display.ProcessedCount = slotProcessed;
        }

        private static bool IsOutputLocation(MaterialLocation location)
        {
            if (location == null)
                return false;

            MaterialLocationKind kind = location.Kind;
            return kind == MaterialLocationKind.OutputStageGood ||
                   kind == MaterialLocationKind.OutputStageNg ||
                   kind == MaterialLocationKind.OutputFeeder ||
                   kind == MaterialLocationKind.OutputCassette;
        }

        private static bool IsOutputWafer(MaterialLocation location)
        {
            return IsOutputLocation(location);
        }

        private static bool IsPickerLocation(MaterialLocation location)
        {
            if (location == null)
                return false;

            return location.Kind == MaterialLocationKind.PickerFront ||
                   location.Kind == MaterialLocationKind.PickerRear;
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

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateRefreshTimer();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateRefreshTimer();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            UpdateRefreshTimer();
        }

        private void UpdateRefreshTimer()
        {
            try
            {
                if (_refresh == null || IsDisposed)
                    return;

                if (ShouldRefreshVisible(this))
                {
                    RefreshAll();
                    if (!_refresh.Enabled)
                        _refresh.Start();
                }
                else if (_refresh.Enabled)
                {
                    _refresh.Stop();
                }
            }
            catch
            {
            }
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

        private sealed class MaterialDisplaySnapshot
        {
            public bool HasMaterial;
            public int TargetCount;
            public int ProcessedCount;
            public int GoodCount;
            public int NgCount;
            public int PickedCount;
            public int CurrentBinCode = -1;
            public string LotId = string.Empty;
        }
    }
}
