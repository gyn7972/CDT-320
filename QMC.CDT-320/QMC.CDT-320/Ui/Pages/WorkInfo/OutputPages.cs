using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    /// <summary>
    /// OUTPUT STAGE — 슬롯/Z축/CHECK 등 OUTPUT 쪽 상태.
    /// </summary>
    public class OutputStagePage : PageBase
    {
        public OutputStagePage()
        {
            Controls.Add(CreateSectionHeader("wi.outputStage"));
            int y = 38;

            var axZ = InfoRows.Pair("wi.outStageZ", "0 um", labelW: 200, valueW: 220); axZ.Location = new Point(8, y); Controls.Add(axZ); y += 36;
            var good = InfoRows.Pair("wi.outGoodCount", "0 ea", labelW: 200, valueW: 220); good.Location = new Point(8, y); Controls.Add(good); y += 36;
            var ng   = InfoRows.Pair("wi.outNgCount",   "0 ea", labelW: 200, valueW: 220); ng.Location   = new Point(8, y); Controls.Add(ng);   y += 36;

            var act = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 90, Padding = new Padding(12),
                BackColor = UiTheme.MainBg, FlowDirection = FlowDirection.LeftToRight
            };
            act.Controls.Add(new ActionButton { Text = Lang.T("wi.stageInit"), Tag = "i18n:wi.stageInit", Width = 160, Margin = new Padding(6) });
            act.Controls.Add(new ActionButton { Text = Lang.T("wi.stageReady"),Tag = "i18n:wi.stageReady",Width = 160, Margin = new Padding(6) });
            Controls.Add(act);
        }
    }

    /// <summary>
    /// OUTPUT FEEDER (Stage 27) — OutputUnloader 의 FeederY/실린더/3카세트 센서 라이브 표시 + 액션.
    /// </summary>
    public class OutputFeederPage : PageBase
    {
        private Label _lblFeederPos, _lblElevatorPos, _lblClamp, _lblUpDown;
        private IndicatorDot _dotNg, _dotGood1, _dotGood2, _dotProtrusion, _dotDetect, _dotClamped;
        private System.Windows.Forms.Timer _timer;

        public OutputFeederPage()
        {
            Controls.Add(CreateSectionHeader("wi.outputFeeder"));

            // 좌상단 — 카세트 안착 LED
            var cassHdr = new Label
            {
                Location = new Point(8, 38), Size = new Size(440, 26),
                Text = "OUTPUT 카세트 안착",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            _dotNg    = new IndicatorDot { Location = new Point(20, 78), Size = new Size(14, 14), OnColor = Color.Red };
            _dotGood1 = new IndicatorDot { Location = new Point(20, 108), Size = new Size(14, 14), OnColor = Color.LimeGreen };
            _dotGood2 = new IndicatorDot { Location = new Point(20, 138), Size = new Size(14, 14), OnColor = Color.LimeGreen };
            var lblNg    = new Label { Location = new Point(40, 74), Size = new Size(150, 22), Text = "NG CASSETTE",    Font = new Font("맑은 고딕", 9F) };
            var lblG1    = new Label { Location = new Point(40, 104), Size = new Size(150, 22), Text = "GOOD1 CASSETTE", Font = new Font("맑은 고딕", 9F) };
            var lblG2    = new Label { Location = new Point(40, 134), Size = new Size(150, 22), Text = "GOOD2 CASSETTE", Font = new Font("맑은 고딕", 9F) };

            // 우상단 — 실린더 + 안전 센서
            var cylHdr = new Label
            {
                Location = new Point(500, 38), Size = new Size(440, 26),
                Text = "FEEDER 실린더 / 안전 센서",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            var lblClamp = MakeKey("CLAMP");      lblClamp.Location  = new Point(500, 70);
            _lblClamp    = MakeVal("---");         _lblClamp.Location = new Point(660, 70);
            var lblUd    = MakeKey("UP/DOWN");    lblUd.Location     = new Point(500, 110);
            _lblUpDown   = MakeVal("---");         _lblUpDown.Location= new Point(660, 110);

            _dotProtrusion = new IndicatorDot { Location = new Point(500, 152), Size = new Size(14, 14), OnColor = Color.Red };
            var lblProt    = new Label { Location = new Point(520, 148), Size = new Size(180, 22), Text = "PROTRUSION SENSOR", Font = new Font("맑은 고딕", 9F) };
            _dotDetect     = new IndicatorDot { Location = new Point(500, 178), Size = new Size(14, 14), OnColor = Color.LimeGreen };
            var lblDet     = new Label { Location = new Point(520, 174), Size = new Size(180, 22), Text = "WAFER DETECT",      Font = new Font("맑은 고딕", 9F) };
            _dotClamped    = new IndicatorDot { Location = new Point(500, 204), Size = new Size(14, 14), OnColor = Color.Cyan };
            var lblClp     = new Label { Location = new Point(520, 200), Size = new Size(180, 22), Text = "WAFER CLAMPED",     Font = new Font("맑은 고딕", 9F) };

            // 하단 — 축 위치
            var axisHdr = new Label
            {
                Location = new Point(8, 250), Size = new Size(1000, 26),
                Text = "AXES — Y / Z",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            var lblFy = MakeKey("FEEDER Y");   lblFy.Location  = new Point(8, 286);
            _lblFeederPos = MakeVal("0.000 mm"); _lblFeederPos.Location = new Point(168, 286);
            var lblEz = MakeKey("ELEVATOR Z"); lblEz.Location  = new Point(8, 318);
            _lblElevatorPos = MakeVal("0.000 mm"); _lblElevatorPos.Location = new Point(168, 318);

            // 액션 버튼
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 96, Padding = new Padding(12),
                BackColor = UiTheme.MainBg, FlowDirection = FlowDirection.LeftToRight
            };
            var btnInit  = new ActionButton { Text = "FEEDER INIT",   Width = 150, Margin = new Padding(6) };
            var btnMap   = new ActionButton { Text = "MAP CASSETTES", Width = 170, Margin = new Padding(6) };
            var btnPick  = new ActionButton { Text = "PICKUP @ STAGE",Width = 180, Margin = new Padding(6) };
            var btnPlace = new ActionButton { Text = "PLACE @ NG",    Width = 160, Margin = new Padding(6) };
            actions.Controls.Add(btnInit);
            actions.Controls.Add(btnMap);
            actions.Controls.Add(btnPick);
            actions.Controls.Add(btnPlace);
            btnInit .Click += (s, e) => RunAction(host => InitFeederAsync(host));
            btnMap  .Click += (s, e) => RunAction(host => host.Controller.ScanOutputCassettesAsync());
            btnPick .Click += (s, e) => RunAction(host => host.Machine.OutputUnloader.SupplyEmptyWaferAsync(QMC.CDT320.TargetCassette.Good1, 0));
            btnPlace.Click += (s, e) => RunAction(host => host.Controller.StoreCompletedWaferAsync(false));

            Controls.Add(cassHdr);
            Controls.Add(_dotNg);    Controls.Add(lblNg);
            Controls.Add(_dotGood1); Controls.Add(lblG1);
            Controls.Add(_dotGood2); Controls.Add(lblG2);
            Controls.Add(cylHdr);
            Controls.Add(lblClamp);  Controls.Add(_lblClamp);
            Controls.Add(lblUd);     Controls.Add(_lblUpDown);
            Controls.Add(_dotProtrusion); Controls.Add(lblProt);
            Controls.Add(_dotDetect);     Controls.Add(lblDet);
            Controls.Add(_dotClamped);    Controls.Add(lblClp);
            Controls.Add(axisHdr);
            Controls.Add(lblFy); Controls.Add(_lblFeederPos);
            Controls.Add(lblEz); Controls.Add(_lblElevatorPos);
            Controls.Add(actions);

            // 200ms Timer
            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) => Refresh2();
            HandleCreated   += (s, e) => _timer.Start();
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private Form1 GetHost() => FindForm() as Form1;

        private async void RunAction(Func<Form1, Task<bool>> action)
        {
            var host = GetHost();
            if (host?.Machine == null) return;
            try { await action(host); }
            catch (Exception ex)
            {
                MessageBox.Show("OutputFeeder error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Refresh2();
        }
        private async void RunAction(Func<Form1, Task> action)
        {
            var host = GetHost();
            if (host?.Machine == null) return;
            try { await action(host); }
            catch (Exception ex)
            {
                MessageBox.Show("OutputFeeder error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Refresh2();
        }

        private async Task InitFeederAsync(Form1 host)
        {
            var u = host.Machine.OutputUnloader;
            u.FeederY.ResetAlarm(); u.FeederY.ServoOn();
            u.ElevatorZ.ResetAlarm(); u.ElevatorZ.ServoOn();
            await u.FeederY.HomeSearchAsync();
            await u.ElevatorZ.HomeSearchAsync();
        }

        private void Refresh2()
        {
            var host = GetHost();
            if (host?.Machine == null) return;
            var u = host.Machine.OutputUnloader;
            _lblFeederPos.Text   = u.FeederY.ActualPosition.ToString("F3") + " mm";
            _lblElevatorPos.Text = u.ElevatorZ.ActualPosition.ToString("F3") + " mm";
            _lblClamp.Text  = u.FeederClampCyl.IsFwd ? "CLAMPED" : (u.FeederClampCyl.IsBwd ? "OPEN" : "...");
            _lblUpDown.Text = u.FeederUpDownCyl.IsFwd ? "DOWN" : (u.FeederUpDownCyl.IsBwd ? "UP" : "...");
            _dotNg.IsOn         = u.ExistSensor_NG.IsOn;
            _dotGood1.IsOn      = u.ExistSensor_Good1.IsOn;
            _dotGood2.IsOn      = u.ExistSensor_Good2.IsOn;
            _dotProtrusion.IsOn = u.ProtrusionSensor.IsOn;
            _dotDetect.IsOn     = u.WaferDetectSensor.IsOn;
            _dotClamped.IsOn    = u.WaferClampedSensor.IsOn;
        }

        private static Label MakeKey(string text) => new Label
        {
            Size = new Size(150, 28),
            Text = text, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), ForeColor = Color.Black,
            Font = new Font("맑은 고딕", 9F),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            BorderStyle = BorderStyle.FixedSingle
        };
        private static Label MakeVal(string text) => new Label
        {
            Size = new Size(220, 28),
            Text = text, BackColor = Color.White, ForeColor = Color.Black,
            Font = new Font("Consolas", 10F),
            TextAlign = ContentAlignment.MiddleCenter,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    /// <summary>OUTPUT CASSETTE.</summary>
    /// <summary>
    /// Stage 37 — OutputCassettePage 라이브 (3 카세트 NG/Good1/Good2 × 25 슬롯 LED 가시화).
    /// </summary>
    public class OutputCassettePage : PageBase
    {
        private const int SLOTS_PER_CASSETTE = 25;
        private readonly Label[] _ngLeds    = new Label[SLOTS_PER_CASSETTE];
        private readonly Label[] _good1Leds = new Label[SLOTS_PER_CASSETTE];
        private readonly Label[] _good2Leds = new Label[SLOTS_PER_CASSETTE];
        private Label _lblElevatorPos;
        private System.Windows.Forms.Timer _timer;

        public OutputCassettePage()
        {
            Controls.Add(CreateSectionHeader("wi.outputCassette"));

            // 상단 — ElevatorZ 위치
            var lblZ = new Label
            {
                Location = new Point(8, 38), Size = new Size(360, 26),
                Text = "ELEVATOR Z",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            _lblElevatorPos = new Label
            {
                Location = new Point(8, 68), Size = new Size(360, 28),
                Text = "0.000 mm", BackColor = Color.White, ForeColor = Color.Black,
                Font = new Font("Consolas", 11F),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 6, 0),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 3 카세트 헤더
            int yStart = 110;
            int colW = 280;
            BuildCassetteColumn("NG CASSETTE",    Color.LightCoral,    8, yStart, _ngLeds);
            BuildCassetteColumn("GOOD1 CASSETTE", Color.LightGreen, 8 + colW, yStart, _good1Leds);
            BuildCassetteColumn("GOOD2 CASSETTE", Color.LightGreen, 8 + colW * 2, yStart, _good2Leds);

            Controls.Add(lblZ);
            Controls.Add(_lblElevatorPos);

            // 액션 버튼
            var act = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 96, Padding = new Padding(12),
                BackColor = UiTheme.MainBg, FlowDirection = FlowDirection.LeftToRight
            };
            var btnMap = new ActionButton { Text = "MAP CASSETTES",   Width = 180, Margin = new Padding(6) };
            var btnLoad = new ActionButton { Text = Lang.T("wi.liftWaferLoading"),   Tag = "i18n:wi.liftWaferLoading",   Width = 180, Margin = new Padding(6) };
            var btnUnld = new ActionButton { Text = Lang.T("wi.liftWaferUnloading"), Tag = "i18n:wi.liftWaferUnloading", Width = 180, Margin = new Padding(6) };
            act.Controls.Add(btnMap);
            act.Controls.Add(btnLoad);
            act.Controls.Add(btnUnld);
            btnMap.Click  += async (s, e) =>
            {
                var host = FindForm() as Form1;
                if (host?.Controller == null) return;
                try { await host.Controller.ScanOutputCassettesAsync(); } catch { }
                Refresh3();
            };
            Controls.Add(act);

            // 200ms Timer
            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) => Refresh3();
            HandleCreated   += (s, e) => _timer.Start();
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private void BuildCassetteColumn(string title, Color baseColor, int x, int y, Label[] leds)
        {
            var hdr = new Label
            {
                Location = new Point(x, y), Size = new Size(260, 26),
                Text = title, BackColor = Color.Black, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(hdr);
            for (int i = 0; i < SLOTS_PER_CASSETTE; i++)
            {
                int rowY = y + 30 + i * 16;
                var idxLbl = new Label
                {
                    Location = new Point(x, rowY), Size = new Size(40, 14),
                    Text = (i + 1).ToString("D2"),
                    Font = new Font("Consolas", 8F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var ledLbl = new Label
                {
                    Location = new Point(x + 42, rowY), Size = new Size(218, 14),
                    BackColor = Color.LightGray,
                    BorderStyle = BorderStyle.FixedSingle
                };
                Controls.Add(idxLbl);
                Controls.Add(ledLbl);
                leds[i] = ledLbl;
            }
        }

        private void Refresh3()
        {
            var host = FindForm() as Form1;
            if (host?.Machine == null) return;
            var u = host.Machine.OutputUnloader;
            _lblElevatorPos.Text = u.ElevatorZ.ActualPosition.ToString("F3") + " mm";

            // SimCassetteDriver 의 슬롯 맵 읽기
            var driverProp = host.GetType().GetProperty("CassetteDriver",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var driver = driverProp?.GetValue(host) as QMC.CDT320.Sim.SimCassetteDriver;

            UpdateLeds(_ngLeds,    driver?.OutputNgSlots,    Color.LightCoral);
            UpdateLeds(_good1Leds, driver?.OutputGood1Slots, Color.LimeGreen);
            UpdateLeds(_good2Leds, driver?.OutputGood2Slots, Color.LimeGreen);
        }

        private static void UpdateLeds(Label[] leds, bool[] state, Color filledColor)
        {
            for (int i = 0; i < leds.Length; i++)
            {
                bool filled = (state != null && i < state.Length && state[i]);
                Color c = filled ? filledColor : Color.LightGray;
                if (leds[i].BackColor != c) leds[i].BackColor = c;
            }
        }
    }

    /// <summary>
    /// LOGIC — 6 Unit 의 실시간 상태 그리드 + TIMECHART (cycle 누적 분포).
    /// Stage 58 — placeholder 라벨 제거, LogicDetailPage 와 동일한 라이브 데이터 사용.
    /// 자체 헤더는 LogicDetailPage 가 그리므로 여기서는 inner 만 dock 한다.
    /// </summary>
    public class LogicPage : PageBase
    {
        public LogicPage()
        {
            // 내부적으로 LogicDetailPage 와 동일한 구현체를 사용 — 단지 두 군데에서 같은 화면을 노출.
            var inner = new LogicDetailPage();
            inner.Dock = DockStyle.Fill;
            Controls.Add(inner);
        }
    }
}
