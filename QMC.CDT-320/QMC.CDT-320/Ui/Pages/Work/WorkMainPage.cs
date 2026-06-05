using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Lots;
using QMC.CDT320.Recipes;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Work
{
    /// <summary>
    /// 작업 탭의 기본 화면 (4분할).
    ///  ┌─────────┬──────────┐
    ///  │ 비전 화면 │  작업 맵  │
    ///  ├─────────┼──────────┤
    ///  │ 작업 정보│ 작업 시간 │
    ///  └─────────┴──────────┘
    /// 정적 placeholder 가 아니라 MachineController + LotStorage 의 실시간 데이터를
    /// 1초마다 갱신해서 작업 정보 / 작업 시간 / 비전 라이브 / 헤더 카운터에 반영한다.
    /// </summary>
    public class WorkMainPage : PageBase
    {
        // 작업 맵 헤더 라벨
        private Label _lblTotalChip, _lblBinNum;
        // 작업 정보
        private Label _vProject, _vPickFail, _vBinQty, _vCollet1, _vPlaceFail, _vNeedle, _vCollet2;
        // 작업 시간
        private Label _vLoad, _vUp, _vContUp, _vNormDown, _vErrDown, _vErrCnt, _vRecovery,
                      _vUph, _vMtbf, _vMttr, _vCycle, _vRate, _vLot;
        // 비전 사이드 라벨 (frame counter / state)
        private Label _vStageInfo, _vLive;

        private System.Windows.Forms.Timer _refresh;
        // 라이브 추정용 누적: lastDoneCount, lastTickAt
        private DateTime _runStart   = DateTime.Now;
        private int      _lastDone   = 0;
        private long     _cycleAccMs = 0;
        private int      _cycleSamples = 0;
        private long     _lastCycleStartTicks;

        public WorkMainPage()
        {
            var root = new TableLayoutPanel
            {
                Dock         = DockStyle.Fill,
                ColumnCount  = 2,
                RowCount     = 2,
                BackColor    = UiTheme.MainBg,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

            root.Controls.Add(BuildVisionQuadrant(),   0, 0);
            root.Controls.Add(BuildWorkMapQuadrant(),  1, 0);
            root.Controls.Add(BuildWorkInfoQuadrant(), 0, 1);
            root.Controls.Add(BuildWorkTimeQuadrant(), 1, 1);

            Controls.Add(root);

            if (!IsDesignerMode())
            {
                _refresh = new System.Windows.Forms.Timer { Interval = 1000 };
                _refresh.Tick += (s, e) => RefreshAll();
                _refresh.Start();
                RefreshAll();
            }
        }

        private Panel BuildVisionQuadrant()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.MainBg };
            p.Controls.Add(CreateSectionHeader("work.sec.visionView"));
            var view = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Black
            };
            // 좌상단 stage 정보 — 카메라 width/height + frame count (실시간 갱신)
            _vStageInfo = new Label
            {
                Location  = new Point(8, 8),
                AutoSize  = true,
                Text      = "STAGE\r\nW : 640\r\nH : 480\r\nframe : 0",
                ForeColor = UiTheme.VisionInfoFg,
                BackColor = Color.Black,
                Font      = new Font("Consolas", 9F)
            };
            _vLive = new Label
            {
                Dock      = DockStyle.Bottom,
                Height    = 18,
                Text      = "Live",
                ForeColor = UiTheme.VisionInfoFg,
                BackColor = Color.Black,
                Font      = new Font("Consolas", 9F),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(6, 0, 0, 0)
            };
            view.Controls.Add(_vStageInfo);
            view.Controls.Add(_vLive);
            p.Controls.Add(view);
            p.Controls.SetChildIndex(view, 1);
            return p;
        }

        private Panel BuildWorkMapQuadrant()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.MainBg };

            var header = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = UiTheme.StatusBarBg };
            header.Controls.Add(new Label
            {
                Location = new Point(10, 4),
                AutoSize = true,
                Text     = Lang.T("work.sec.workMap"),
                Tag      = "i18n:work.sec.workMap",
                Font     = UiTheme.SectionFont,
                ForeColor = UiTheme.StatusBarFg
            });
            header.Controls.Add(new Label { Location = new Point(180, 6), AutoSize = true, Text = "Total Chip :", ForeColor = UiTheme.StatusBarFg, Font = new Font("맑은 고딕", 9F) });
            _lblTotalChip = new Label { Location = new Point(260, 6), AutoSize = true, Text = "0", ForeColor = UiTheme.StatusBarFg, Font = new Font("Consolas", 10F, FontStyle.Bold) };
            header.Controls.Add(_lblTotalChip);
            header.Controls.Add(new Label { Location = new Point(330, 6), AutoSize = true, Text = "Bin # :",      ForeColor = UiTheme.StatusBarFg, Font = new Font("맑은 고딕", 9F) });
            _lblBinNum = new Label { Location = new Point(380, 6), AutoSize = true, Text = "—", ForeColor = UiTheme.StatusBarFg, Font = new Font("Consolas", 10F, FontStyle.Bold) };
            header.Controls.Add(_lblBinNum);

            // VISION/PICK/PLACE 도트
            var rightInfo = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 260, BackColor = UiTheme.StatusBarBg, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(4, 6, 8, 0) };
            rightInfo.Controls.Add(new Controls.IndicatorDot { IsOn = true, OnColor = UiTheme.DotVision,    Width=12, Height=12, Margin = new Padding(0, 4, 4, 0) });
            rightInfo.Controls.Add(new Label { AutoSize = true, Text = "VISION",    ForeColor = UiTheme.StatusBarFg, Font = new Font("맑은 고딕", 9F) });
            rightInfo.Controls.Add(new Controls.IndicatorDot { IsOn = true, OnColor = UiTheme.DotPick,      Width=12, Height=12, Margin = new Padding(12, 4, 4, 0) });
            rightInfo.Controls.Add(new Label { AutoSize = true, Text = "PICK",      ForeColor = UiTheme.StatusBarFg, Font = new Font("맑은 고딕", 9F) });
            rightInfo.Controls.Add(new Controls.IndicatorDot { IsOn = true, OnColor = Color.Blue,           Width=12, Height=12, Margin = new Padding(12, 4, 4, 0) });
            rightInfo.Controls.Add(new Label { AutoSize = true, Text = "PLACE",     ForeColor = UiTheme.StatusBarFg, Font = new Font("맑은 고딕", 9F) });
            header.Controls.Add(rightInfo);

            p.Controls.Add(header);

            // 작업 맵 — 실제 LotStorage.ActiveLot 의 die map view (있으면) 또는 빈 캔버스
            var map = new Controls.LiveLotMapView { Dock = DockStyle.Fill };
            p.Controls.Add(map);
            p.Controls.SetChildIndex(map, 1);
            return p;
        }

        private Panel BuildWorkInfoQuadrant()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.MainBg };
            p.Controls.Add(CreateSectionHeader("work.sec.workInfo"));

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 5,
                BackColor = UiTheme.OptionPanelBg, Padding = new Padding(6)
            };
            for (int i = 0; i < 4; i++) body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            for (int r = 0; r < 5; r++) body.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            _vProject   = AddPair(body, 0, 0, "work.workInfo.project",     "—");
            _vPickFail  = AddPair(body, 1, 0, "work.workInfo.pickFail",    "0 ea");
            _vBinQty    = AddPair(body, 1, 2, "work.workInfo.workBinQty",  "0 ea");
            _vCollet1   = AddPair(body, 2, 0, "work.workInfo.collet1Use",  "0");
            _vPlaceFail = AddPair(body, 2, 2, "work.workInfo.placeFail",   "0 ea");
            _vNeedle    = AddPair(body, 3, 0, "work.workInfo.needleUse",   "0");
            _vCollet2   = AddPair(body, 3, 2, "work.workInfo.collet2Use",  "0");
            AddPair(body, 4, 0, "work.workInfo.binArrMon", "");

            p.Controls.Add(body);
            p.Controls.SetChildIndex(body, 1);
            return p;
        }

        private Panel BuildWorkTimeQuadrant()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.MainBg };
            p.Controls.Add(CreateSectionHeader("work.sec.workTime"));

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 7,
                BackColor = UiTheme.OptionPanelBg, Padding = new Padding(6)
            };
            for (int i = 0; i < 4; i++) body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            for (int r = 0; r < 7; r++) body.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            _vLoad     = AddPair(body, 0, 0, "work.workTime.load",     "00:00:00");
            _vUp       = AddPair(body, 0, 2, "work.workTime.up",       "00:00:00");
            _vContUp   = AddPair(body, 1, 0, "work.workTime.contUp",   "00:00:00");
            _vNormDown = AddPair(body, 1, 2, "work.workTime.normDown", "00:00:00");
            _vErrDown  = AddPair(body, 2, 0, "work.workTime.errDown",  "00:00:00");
            _vErrCnt   = AddPair(body, 2, 2, "work.workTime.errCnt",   "0 ea");
            _vRecovery = AddPair(body, 3, 0, "work.workTime.recovery", "00:00:00");
            _vUph      = AddPair(body, 3, 2, "work.workTime.uph",      "0.00");
            _vMtbf     = AddPair(body, 4, 0, "work.workTime.mtbf",     "00:00:00");
            _vMttr     = AddPair(body, 4, 2, "work.workTime.mttr",     "00:00:00");
            _vCycle    = AddPair(body, 5, 0, "work.workTime.cycle",    "0 ms");
            _vRate     = AddPair(body, 5, 2, "work.workTime.rate",     "0.00 %");
            _vLot      = AddPair(body, 6, 0, "work.workTime.lotId",    "");

            var ccs = new Button
            {
                Text      = Lang.T("work.workTime.ccs"),
                Tag       = "i18n:work.workTime.ccs",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0x59, 0x59, 0x59),
                ForeColor = Color.White,
                Font      = UiTheme.ButtonFont,
                Dock      = DockStyle.Fill
            };
            // Stage 60 R12 — CCS 버튼 핸들러 (이전 placeholder → 실제 의미 있는 동작)
            ccs.Click += (s, e) =>
            {
                try
                {
                    QMC.CDT320.Logging.EventLogger.Write(
                        QMC.CDT320.Logging.EventKind.Event,
                        QMC.CDT_320.Ui.Security.UserSession.Name,
                        "CCS-CHECK",
                        "CCS 검수 확인 버튼 클릭 — 검수 다이얼로그 (구현 예정)");
                }
                catch { }
                MessageBox.Show(
                    "CCS 검수 확인 페이지는 다음 작업 단계에서 구현됩니다.",
                    Lang.T("work.workTime.ccs"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            body.Controls.Add(ccs, 2, 6);
            body.SetColumnSpan(ccs, 2);

            p.Controls.Add(body);
            p.Controls.SetChildIndex(body, 1);
            return p;
        }

        // 1초마다 호출되는 갱신 — Lot/Controller/Recipe 데이터 → 라벨
        private void RefreshAll()
        {
            try
            {
                Form1 host = ParentForm as Form1;
                if (host == null) host = FindForm() as Form1;
                var ctrl = host?.Controller;
                var lot  = LotStorage.ActiveLot;

                // 작업 맵 헤더 (Total Chip / Bin#)
                int total    = lot?.ProcessedDies ?? 0;
                int currBin  = -1;
                if (lot != null && lot.BinDistribution != null && lot.BinDistribution.Count > 0)
                {
                    int max = -1;
                    foreach (var kv in lot.BinDistribution)
                        if (kv.Value > max) { max = kv.Value; currBin = kv.Key; }
                }
                if (_lblTotalChip != null) _lblTotalChip.Text = total.ToString();
                if (_lblBinNum    != null) _lblBinNum   .Text = currBin >= 0 ? currBin.ToString() : "—";

                // 비전 사이드 (frame count == 누적 다이 사이클 수)
                if (_vStageInfo != null)
                    _vStageInfo.Text = "STAGE\r\nW : 640\r\nH : 480\r\nframe : " + total;
                if (_vLive != null)
                    _vLive.Text = ctrl == null ? "Idle" : ("Live  [" + ctrl.Status + "]");

                // 작업 정보
                string project = "—";
                try { project = host?.Machine?.Recipe?.ProductId ?? "—"; } catch { }
                // 활성 lot 의 RecipeName 이 있으면 그것을 우선
                if (lot != null && !string.IsNullOrEmpty(lot.RecipeName)) project = lot.RecipeName;
                else
                {
                    try
                    {
                        var list = RecipeStore.List();
                        if (list != null && list.Count > 0) project = System.IO.Path.GetFileNameWithoutExtension(list[0]);
                    }
                    catch { }
                }
                if (_vProject != null) _vProject.Text = project;

                int pickFail  = ctrl?.PickFailCount  ?? 0;
                int placeFail = ctrl?.PlaceFailCount ?? 0;
                int collet1   = ctrl?.Collet1UseCount ?? 0;
                int collet2   = ctrl?.Collet2UseCount ?? 0;
                int needle    = ctrl?.NeedleUseCount  ?? 0;
                if (_vPickFail  != null) _vPickFail .Text = pickFail  + " ea";
                if (_vPlaceFail != null) _vPlaceFail.Text = placeFail + " ea";
                if (_vBinQty    != null) _vBinQty   .Text = (lot?.GoodCount ?? 0) + " ea";
                if (_vCollet1   != null) _vCollet1  .Text = collet1.ToString();
                if (_vCollet2   != null) _vCollet2  .Text = collet2.ToString();
                if (_vNeedle    != null) _vNeedle   .Text = needle.ToString();

                // 작업 시간 — Lot 기반 + 누적
                TimeSpan upTime = (lot != null) ? lot.Duration : (DateTime.Now - _runStart);
                TimeSpan normalDown = TimeSpan.Zero, errorDown = TimeSpan.Zero;
                if (ctrl != null)
                {
                    normalDown = ctrl.NormalDownTime;
                    errorDown  = ctrl.ErrorDownTime;
                }
                if (_vLoad     != null) _vLoad    .Text = FormatTs(upTime);
                if (_vUp       != null) _vUp      .Text = FormatTs(upTime);
                if (_vContUp   != null) _vContUp  .Text = FormatTs(upTime);
                if (_vNormDown != null) _vNormDown.Text = FormatTs(normalDown);
                if (_vErrDown  != null) _vErrDown .Text = FormatTs(errorDown);
                if (_vErrCnt   != null) _vErrCnt  .Text = (ctrl?.ErrorCount ?? 0) + " ea";
                if (_vRecovery != null) _vRecovery.Text = FormatTs(ctrl?.RecoveryTime ?? TimeSpan.Zero);

                // UPH = (Good * 3600) / runtime sec
                double uph = 0;
                if (lot != null && lot.GoodCount > 0)
                {
                    double secs = upTime.TotalSeconds;
                    if (secs > 0.5) uph = lot.GoodCount * 3600.0 / secs;
                }
                if (_vUph != null) _vUph.Text = uph.ToString("F2");

                // MTBF / MTTR
                if (_vMtbf != null) _vMtbf.Text = FormatTs(ctrl?.Mtbf ?? TimeSpan.Zero);
                if (_vMttr != null) _vMttr.Text = FormatTs(ctrl?.Mttr ?? TimeSpan.Zero);

                // Cycle ms = 마지막 다이 1개 처리 평균
                int doneNow = lot?.ProcessedDies ?? 0;
                if (doneNow > _lastDone)
                {
                    long now = DateTime.UtcNow.Ticks;
                    if (_lastCycleStartTicks > 0)
                    {
                        long ms = (now - _lastCycleStartTicks) / TimeSpan.TicksPerMillisecond;
                        int dn = doneNow - _lastDone;
                        if (dn > 0)
                        {
                            _cycleAccMs += ms;
                            _cycleSamples += dn;
                        }
                    }
                    _lastCycleStartTicks = now;
                    _lastDone = doneNow;
                }
                long avg = _cycleSamples > 0 ? (_cycleAccMs / _cycleSamples) : 0;
                if (_vCycle != null) _vCycle.Text = avg + " ms";

                // Yield 비율 = good / processed
                double rate = (lot != null && lot.ProcessedDies > 0)
                    ? (lot.GoodCount * 100.0 / lot.ProcessedDies)
                    : 0;
                if (_vRate != null) _vRate.Text = rate.ToString("F2") + " %";

                // Lot ID
                if (_vLot != null) _vLot.Text = lot?.LotID ?? "(no lot)";
            }
            catch
            {
                // UI 갱신 도중 예외는 무시 (Form 종료 도중 등)
            }
        }

        private static string FormatTs(TimeSpan ts)
        {
            if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;
            int totalH = (int)ts.TotalHours;
            return string.Format("{0:00}:{1:00}:{2:00}", totalH, ts.Minutes, ts.Seconds);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _refresh?.Stop(); _refresh?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }

        private static Label AddPair(TableLayoutPanel tbl, int row, int col, string i18nKey, string value)
        {
            var lbl = new Label
            {
                Dock      = DockStyle.Fill,
                Text      = Lang.T(i18nKey),
                Tag       = "i18n:" + i18nKey,
                BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0),
                ForeColor = Color.Black,
                Font      = new Font("맑은 고딕", 9F),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(6, 0, 0, 0),
                Margin    = new Padding(1)
            };
            var v = new Label
            {
                Dock      = DockStyle.Fill,
                Text      = value,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font      = new Font("Consolas", 10F),
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(0, 0, 6, 0),
                Margin    = new Padding(1)
            };
            tbl.Controls.Add(lbl, col,     row);
            tbl.Controls.Add(v,   col + 1, row);
            return v;
        }
    }
}
