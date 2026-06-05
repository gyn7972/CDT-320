using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    /// <summary>
    /// 작업정보 - INPUT FEEDER (Stage 27).<br/>
    /// 라이브 바인딩 + 5 액션 버튼.
    /// </summary>
    public class InputFeederPage : PageBase
    {
        private Label _lblFeederPos, _lblClampState, _lblUpDownState, _lblExist;
        private IndicatorDot _dotRing, _dotOverload;
        private System.Windows.Forms.Timer _timer;

        public InputFeederPage()
        {
            Controls.Add(CreateSectionHeader("tab.workInfo"));

            // 좌상단 — 작업 정보 (EXIST)
            var leftHdr = new Label
            {
                Location = new Point(8, 38), Size = new Size(440, 26),
                Text = Lang.T("tab.workInfo"), Tag = "i18n:tab.workInfo",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            _lblExist = MakeValueLabel("---");
            _lblExist.Location = new Point(8, 70);

            // 우상단 — FEEDER 실린더 정보
            var rightHdr = new Label
            {
                Location = new Point(500, 38), Size = new Size(440, 26),
                Text = "FEEDER 실린더 / 센서",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            var lblClamp = MakeKeyLabel("CLAMP");
            lblClamp.Location = new Point(500, 70);
            _lblClampState = MakeValueLabel("---");
            _lblClampState.Location = new Point(660, 70);
            var lblUd = MakeKeyLabel("UP/DOWN");
            lblUd.Location = new Point(500, 110);
            _lblUpDownState = MakeValueLabel("---");
            _lblUpDownState.Location = new Point(660, 110);

            // 하단 — Feeder Y 축 + 센서
            var infoHdr = new Label
            {
                Location = new Point(8, 160), Size = new Size(1000, 26),
                Text = "FEEDER AXIS Y / 센서",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            _lblFeederPos = new Label
            {
                Location = new Point(8, 196), Size = new Size(260, 28),
                Text = "0.000 mm", BackColor = Color.White, ForeColor = Color.Black,
                Font = new Font("Consolas", 11F),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 6, 0),
                BorderStyle = BorderStyle.FixedSingle
            };

            _dotRing = new IndicatorDot { Location = new Point(8, 240), Size = new Size(14, 14), OnColor = Color.LimeGreen };
            var lblRing = new Label { Location = new Point(28, 236), Size = new Size(220, 22), Text = "FEEDER RING CHECK", Font = new Font("맑은 고딕", 9F) };
            _dotOverload = new IndicatorDot { Location = new Point(8, 266), Size = new Size(14, 14), OnColor = Color.Red };
            var lblOl = new Label { Location = new Point(28, 262), Size = new Size(220, 22), Text = "FEEDER OVERLOAD CHECK", Font = new Font("맑은 고딕", 9F) };

            // 액션 버튼
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 96, Padding = new Padding(12),
                BackColor = UiTheme.MainBg, FlowDirection = FlowDirection.LeftToRight
            };
            var btnInit  = new ActionButton { Text = "FEEDER INIT",   Width = 150, Margin = new Padding(6) };
            var btnFwd   = new ActionButton { Text = "CYL FWD",       Width = 130, Margin = new Padding(6) };
            var btnBwd   = new ActionButton { Text = "CYL BWD",       Width = 130, Margin = new Padding(6) };
            var btnClamp = new ActionButton { Text = "CLAMP",         Width = 130, Margin = new Padding(6) };
            var btnUnclm = new ActionButton { Text = "UNCLAMP",       Width = 130, Margin = new Padding(6) };
            actions.Controls.Add(btnInit);
            actions.Controls.Add(btnFwd);
            actions.Controls.Add(btnBwd);
            actions.Controls.Add(btnClamp);
            actions.Controls.Add(btnUnclm);
            btnInit .Click += (s, e) => RunAction(host => InitFeederAsync(host));
            btnFwd  .Click += (s, e) => RunAction(host => host.Machine.InputLoader.FeederUpDownCyl.MoveFwdAsync());
            btnBwd  .Click += (s, e) => RunAction(host => host.Machine.InputLoader.FeederUpDownCyl.MoveBwdAsync());
            btnClamp.Click += (s, e) => RunAction(host => host.Machine.InputLoader.FeederClampCyl.MoveFwdAsync());
            btnUnclm.Click += (s, e) => RunAction(host => host.Machine.InputLoader.FeederClampCyl.MoveBwdAsync());

            Controls.Add(leftHdr);
            Controls.Add(_lblExist);
            Controls.Add(rightHdr);
            Controls.Add(lblClamp); Controls.Add(_lblClampState);
            Controls.Add(lblUd);    Controls.Add(_lblUpDownState);
            Controls.Add(infoHdr);
            Controls.Add(_lblFeederPos);
            Controls.Add(_dotRing); Controls.Add(lblRing);
            Controls.Add(_dotOverload); Controls.Add(lblOl);
            Controls.Add(actions);

            // 200ms Timer
            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) => RefreshFromMachine();
            HandleCreated   += (s, e) => _timer.Start();
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private Form1 GetHost() => FindForm() as Form1;

        private async void RunAction(Func<Form1, Task> action)
        {
            var host = GetHost();
            if (host?.Machine == null) return;
            try { await action(host); }
            catch (Exception ex)
            {
                MessageBox.Show("Feeder error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            RefreshFromMachine();
        }
        private async void RunAction(Func<Form1, Task<bool>> action)
        {
            var host = GetHost();
            if (host?.Machine == null) return;
            try { await action(host); }
            catch (Exception ex)
            {
                MessageBox.Show("Feeder error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            RefreshFromMachine();
        }

        private async Task InitFeederAsync(Form1 host)
        {
            var loader = host.Machine.InputLoader;
            loader.FeederY.ResetAlarm();
            loader.FeederY.ServoOn();
            await loader.FeederY.HomeSearchAsync();
        }

        private void RefreshFromMachine()
        {
            var host = GetHost();
            if (host?.Machine == null) return;
            var loader = host.Machine.InputLoader;

            _lblFeederPos.Text   = loader.FeederY.ActualPosition.ToString("F3") + " mm";
            _lblClampState.Text  = loader.FeederClampCyl.IsFwd ? "CLAMPED" : (loader.FeederClampCyl.IsBwd ? "OPEN" : "...");
            _lblClampState.ForeColor = loader.FeederClampCyl.IsFwd ? Color.LimeGreen : Color.Black;
            _lblUpDownState.Text = loader.FeederUpDownCyl.IsFwd ? "DOWN" : (loader.FeederUpDownCyl.IsBwd ? "UP" : "...");
            _lblExist.Text       = loader.WaferClampedSensor.IsOn ? "WAFER" : "EMPTY";

            _dotRing.IsOn     = loader.WaferDetectSensor.IsOn;
            _dotOverload.IsOn = loader.ProtrusionSensor.IsOn;
        }

        private static Label MakeKeyLabel(string text) => new Label
        {
            Size = new Size(150, 28),
            Text = text, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), ForeColor = Color.Black,
            Font = new Font("맑은 고딕", 9F),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            BorderStyle = BorderStyle.FixedSingle
        };
        private static Label MakeValueLabel(string text) => new Label
        {
            Size = new Size(220, 28),
            Text = text, BackColor = Color.White, ForeColor = Color.Black,
            Font = new Font("Consolas", 10F),
            TextAlign = ContentAlignment.MiddleCenter,
            BorderStyle = BorderStyle.FixedSingle
        };
    }
}
