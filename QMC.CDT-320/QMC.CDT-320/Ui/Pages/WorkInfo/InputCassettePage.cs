using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    /// <summary>
    /// 작업정보 - INPUT CASSETTE: 리프터 축 + 슬롯 상태 + 맵/로딩/언로딩 액션.<br/>
    /// Stage 26 — 7개 버튼 모두 MachineController 의 로트포트 시퀀스에 연결.
    /// </summary>
    public class InputCassettePage : PageBase
    {
        // Stage 36 — InputCassette 16 슬롯 (SimCassetteDriver 와 동일)
        private const int   SLOT_COUNT_UI = 16;
        private readonly Label[] _slotLeds      = new Label[SLOT_COUNT_UI];
        private readonly Label[] _slotIndexLbls = new Label[SLOT_COUNT_UI];
        private Label _lifterPosLabel;
        private System.Windows.Forms.Timer _refreshTimer;

        public InputCassettePage()
        {
            Controls.Add(CreateSectionHeader("common.info"));

            // 상단 — Lifter Axis Z + Cassette Check 2개
            var top = new FlowLayoutPanel
            {
                Dock      = DockStyle.Top,
                Height    = 80,
                Padding   = new Padding(8),
                BackColor = UiTheme.MainBg
            };
            _lifterPosLabel = new Label
            {
                Location = new Point(0, 28), Size = new Size(220, 28),
                Text = "0.000 mm", BackColor = Color.White, ForeColor = Color.Black,
                Font = new Font("Consolas", 10F),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 6, 0),
                BorderStyle = BorderStyle.FixedSingle
            };
            top.Controls.Add(BuildAxisBlock("LIFTER AXIS Z", _lifterPosLabel));
            top.Controls.Add(BuildIoBlock("LIFTER CASSETTE CHECK #1"));
            top.Controls.Add(BuildIoBlock("LIFTER CASSETTE CHECK #2"));

            // 레전드
            var legend = BuildLegend();
            legend.Location = new Point(700, 8);

            // 좌측 슬롯 상태 패널
            var leftGrp = new Panel { Location = new Point(12, 90), Size = new Size(320, 180), BackColor = UiTheme.MainBg };
            var slotHdr = new Label
            {
                Dock = DockStyle.Top, Height = 26,
                Text = Lang.T("wi.slotState"), Tag = "i18n:wi.slotState",
                BackColor = Color.FromArgb(0x50, 0x50, 0x50), ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleCenter
            };
            leftGrp.Controls.Add(slotHdr);
            leftGrp.Controls.Add(InfoRows.Pair("wi.slotNo", "BIN 1").SetDockFill());
            var stateRow = InfoRows.Pair("common.state", "EMPTY");
            stateRow.Top = 54;
            leftGrp.Controls.Add(stateRow);

            var btnPrev  = new Button { Location = new Point(6, 96),  Size = new Size(140, 34), Text = Lang.T("common.prev"),    Tag = "i18n:common.prev",    FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            var btnNext  = new Button { Location = new Point(152, 96),Size = new Size(140, 34), Text = Lang.T("common.next"),    Tag = "i18n:common.next",    FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            var btnInit  = new Button { Location = new Point(6, 136), Size = new Size(140, 34), Text = Lang.T("wi.lifterInit"),  Tag = "i18n:wi.lifterInit",  FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            var btnReady = new Button { Location = new Point(152, 136),Size = new Size(140, 34), Text = Lang.T("wi.lifterReady"),Tag = "i18n:wi.lifterReady", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            leftGrp.Controls.Add(btnPrev); leftGrp.Controls.Add(btnNext);
            leftGrp.Controls.Add(btnInit); leftGrp.Controls.Add(btnReady);

            // ── Stage 26 — 버튼 콜백 연결 ───────────────────────────────
            btnPrev .Click += (s, e) => MoveSlotRel(-1);
            btnNext .Click += (s, e) => MoveSlotRel(+1);
            btnInit .Click += (s, e) => RunAction(LifterInitAsync);
            btnReady.Click += (s, e) => RunAction(LifterReadyAsync);

            // 우측 LIFTER — Stage 36: 16 슬롯 (높이 확장: 32 + 16*22 = 384)
            var lifterGrp = new Panel { Location = new Point(480, 90), Size = new Size(360, 410) };
            var lifterHdr = new Label
            {
                Dock = DockStyle.Top, Height = 26,
                Text = "LIFTER", BackColor = Color.Black, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleCenter
            };
            lifterGrp.Controls.Add(lifterHdr);
            // Stage 36 — 슬롯 row 높이 22px 로 압축 (16 슬롯 fit)
            for (int i = 0; i < SLOT_COUNT_UI; i++)
            {
                var row = new Panel { Location = new Point(0, 32 + i * 22), Size = new Size(360, 20) };
                var nameLbl = new Label { Location = new Point(0, 0),   Size = new Size(120, 20), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, Text = "SLOT-" + (i + 1).ToString("D2"), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Consolas", 8F) };
                var idxLbl  = new Label { Location = new Point(124, 0), Size = new Size(24, 20),  Text = (i + 1).ToString("D2"), Font = new Font("Consolas", 8F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
                var ledLbl  = new Label { Location = new Point(152, 0), Size = new Size(196, 20), BackColor = Color.LightGray };
                row.Controls.Add(nameLbl);
                row.Controls.Add(idxLbl);
                row.Controls.Add(ledLbl);
                _slotLeds[i]      = ledLbl;
                _slotIndexLbls[i] = idxLbl;
                lifterGrp.Controls.Add(row);
            }

            // 하단 ACTION 버튼들 — Stage 26 콜백 연결
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 96, Padding = new Padding(12),
                BackColor = UiTheme.MainBg, FlowDirection = FlowDirection.LeftToRight
            };
            var btnMap   = new ActionButton { Text = Lang.T("wi.liftWaferMapping"),   Tag = "i18n:wi.liftWaferMapping",   Width = 180, Margin = new Padding(6) };
            var btnLoad  = new ActionButton { Text = Lang.T("wi.liftWaferLoading"),   Tag = "i18n:wi.liftWaferLoading",   Width = 180, Margin = new Padding(6) };
            var btnUnld  = new ActionButton { Text = Lang.T("wi.liftWaferUnloading"), Tag = "i18n:wi.liftWaferUnloading", Width = 180, Margin = new Padding(6) };
            actions.Controls.Add(btnMap);
            actions.Controls.Add(btnLoad);
            actions.Controls.Add(btnUnld);
            btnMap .Click += (s, e) => RunAction(MapAsync);
            btnLoad.Click += (s, e) => RunAction(LoadAsync);
            btnUnld.Click += (s, e) => RunAction(UnloadAsync);

            Controls.Add(top);
            Controls.Add(legend);
            Controls.Add(leftGrp);
            Controls.Add(lifterGrp);
            Controls.Add(actions);
            Controls.SetChildIndex(top, 0);

            // ── Stage 26 — 200ms 폴링: 슬롯 LED + lifter 위치 갱신 ──────
            _refreshTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _refreshTimer.Tick += (s, e) => RefreshFromMachine();
            HandleCreated += (s, e) => _refreshTimer.Start();
            HandleDestroyed += (s, e) => _refreshTimer.Stop();
        }

        // ────────────────────────────────────────────────────────────────
        //  Helper - Form1 에서 Controller / Machine 획득
        // ────────────────────────────────────────────────────────────────

        private Form1 GetHost() => FindForm() as Form1;

        private async void RunAction(Func<Form1, Task> action)
        {
            var host = GetHost();
            if (host?.Controller == null) return;
            try { await action(host); }
            catch (Exception ex)
            {
                MessageBox.Show("LotPort error:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            RefreshFromMachine();
        }

        // ────────────────────────────────────────────────────────────────
        //  버튼 액션
        // ────────────────────────────────────────────────────────────────

        private async Task LifterInitAsync(Form1 host)
        {
            var loader = host.Machine.InputLoader;
            loader.ElevatorZ.ResetAlarm(); loader.ElevatorZ.ServoOn();
            loader.FeederY.ResetAlarm();   loader.FeederY.ServoOn();
            await loader.ElevatorZ.HomeSearchAsync();
            await loader.FeederY.HomeSearchAsync();
        }

        private Task LifterReadyAsync(Form1 host)
        {
            var loader = host.Machine.InputLoader;
            loader.ElevatorZ.ServoOn();
            loader.FeederY.ServoOn();
            return Task.CompletedTask;
        }

        private async Task MapAsync(Form1 host)
        {
            await host.Controller.ScanInputCassetteAsync();
        }

        private async Task LoadAsync(Form1 host)
        {
            await host.Controller.LoadNextWaferAsync();
        }

        private async Task UnloadAsync(Form1 host)
        {
            await host.Controller.RetractCurrentWaferAsync();
        }

        private void MoveSlotRel(int delta)
        {
            var host = GetHost();
            if (host?.Machine == null) return;
            var loader = host.Machine.InputLoader;
            double pitch = 6.0;
            _ = loader.ElevatorZ.MoveRelativeAsync(delta * pitch, 20.0);
        }

        // ────────────────────────────────────────────────────────────────
        //  UI 갱신
        // ────────────────────────────────────────────────────────────────

        private void RefreshFromMachine()
        {
            var host = GetHost();
            if (host?.Machine == null) return;
            var loader = host.Machine.InputLoader;
            var ctrl   = host.Controller;

            if (_lifterPosLabel != null)
                _lifterPosLabel.Text = loader.ElevatorZ.ActualPosition.ToString("F3") + " mm";

            // 슬롯 LED — WaferMap 상위 6 슬롯만 표시
            var map = loader.WaferMap;
            int curSlot = ctrl != null ? ctrl.CurrentInputSlot : -1;
            for (int i = 0; i < SLOT_COUNT_UI; i++)
            {
                Color c;
                if (map != null && i < map.Count)
                {
                    if (i == curSlot)        c = Color.Cyan;        // 작업 준비
                    else if (map[i])         c = Color.LimeGreen;   // 웨이퍼 있음
                    else                     c = Color.LightGray;   // 비어 있음
                }
                else                         c = Color.LightGray;
                if (_slotLeds[i].BackColor != c) _slotLeds[i].BackColor = c;
            }
        }

        // ────────────────────────────────────────────────────────────────
        //  헬퍼 빌드
        // ────────────────────────────────────────────────────────────────

        private static Control BuildAxisBlock(string title, Label valueLabel)
        {
            var p = new Panel { Width = 220, Height = 60, Margin = new Padding(4) };
            p.Controls.Add(new Label { Location = new Point(0, 0),  Size = new Size(220, 24), Text = title, BackColor = Color.Black, ForeColor = Color.White, Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0) });
            valueLabel.Location = new Point(0, 28);
            valueLabel.Size     = new Size(220, 28);
            p.Controls.Add(valueLabel);
            return p;
        }

        private static Control BuildIoBlock(string title)
        {
            var p = new Panel { Width = 200, Height = 30, Margin = new Padding(4) };
            p.Controls.Add(new IndicatorDot { Location = new Point(6, 8), Size = new Size(12, 12), OnColor = Color.LimeGreen });
            p.Controls.Add(new Label { Location = new Point(24, 2), Size = new Size(170, 26), Text = title, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), ForeColor = Color.Black, Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0) });
            return p;
        }

        private static Control BuildLegend()
        {
            var p = new Panel { Size = new Size(260, 70), BackColor = UiTheme.MainBg, BorderStyle = BorderStyle.FixedSingle };
            void Add(int x, int y, Color color, string text)
            {
                p.Controls.Add(new Label { Location = new Point(x, y), Size = new Size(28, 18), BackColor = color });
                p.Controls.Add(new Label { Location = new Point(x + 32, y), Size = new Size(90, 18), Text = text, Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft });
            }
            Add(8, 6,   Color.Cyan,      Lang.T("wi.legend.ready"));
            Add(130, 6, Color.LimeGreen, Lang.T("wi.legend.empty"));
            Add(8, 26,  Color.Orange,    Lang.T("wi.legend.working"));
            Add(130, 26,Color.Red,       Lang.T("wi.legend.finish"));
            Add(8, 46,  Color.Navy,      Lang.T("wi.legend.workReady"));
            return p;
        }
    }

    internal static class CtrlExt
    {
        public static T SetDockFill<T>(this T c) where T : Control { c.Dock = DockStyle.None; return c; }
    }
}
