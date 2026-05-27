using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    /// <summary>
    /// Stage 55 — Operation Panel Status Page (CDT-310 매뉴얼 호환).<br/>
    /// 운전 패널 (버튼/램프/신호탑/부저) + Resource Sensors (CDA/Vacuum) + Ionizer 라이브.
    /// </summary>
    public class OperationPanelStatusPage : PageBase
    {
        private IndicatorDot _dotStart, _dotStop, _dotReset, _dotEmgF, _dotEmgL, _dotEmgR;
        private IndicatorDot _ledStartLamp, _ledStopLamp, _ledResetLamp;
        private IndicatorDot _tlRed, _tlYellow, _tlGreen, _ledBuzzer;
        private IndicatorDot _dotCda1, _dotCda2, _dotVac1, _dotVac2, _dotVac3, _dotVac4;
        private IndicatorDot _dotIonizer;
        private System.Windows.Forms.Timer _timer;

        public OperationPanelStatusPage()
        {
            Controls.Add(CreateSectionHeader("wi.opPanelStatus"));

            // 좌상단 — Buttons (DI)
            BuildSection("운전자 버튼 (DI)", 8, 38);
            _dotStart = AddDot(20, 78, "START");
            _dotStop  = AddDot(20, 108,"STOP");
            _dotReset = AddDot(20, 138,"RESET");
            _dotEmgF  = AddDot(20, 168,"EMG Front", Color.Red);
            _dotEmgL  = AddDot(20, 198,"EMG Left",  Color.Red);
            _dotEmgR  = AddDot(20, 228,"EMG Rear",  Color.Red);

            // 중앙 — Lamps (DO)
            BuildSection("운전자 램프 (DO)", 350, 38);
            _ledStartLamp = AddDot(362, 78, "START LAMP");
            _ledStopLamp  = AddDot(362, 108,"STOP LAMP");
            _ledResetLamp = AddDot(362, 138,"RESET LAMP");

            // Tower Lamp
            BuildSection("Tower Lamp + Buzzer", 350, 168);
            _tlRed     = AddDot(362, 198, "RED",    Color.Red);
            _tlYellow  = AddDot(362, 228, "YELLOW", Color.Goldenrod);
            _tlGreen   = AddDot(362, 258, "GREEN",  Color.LimeGreen);
            _ledBuzzer = AddDot(362, 288, "BUZZER", Color.Magenta);

            // 우상단 — Resource Sensors
            BuildSection("Resource Sensors", 700, 38);
            _dotCda1 = AddDot(712, 78,  "MAIN CDA 1", Color.Cyan);
            _dotCda2 = AddDot(712, 108, "MAIN CDA 2", Color.Cyan);
            _dotVac1 = AddDot(712, 138, "VACUUM 1");
            _dotVac2 = AddDot(712, 168, "VACUUM 2");
            _dotVac3 = AddDot(712, 198, "VACUUM 3");
            _dotVac4 = AddDot(712, 228, "VACUUM 4");

            // Ionizer
            BuildSection("Ionizer", 700, 268);
            _dotIonizer = AddDot(712, 308, "IONIZER OK", Color.Cyan);

            // Timer
            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) => Refresh4();
            HandleCreated   += (s, e) => _timer.Start();
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private void BuildSection(string title, int x, int y)
        {
            Controls.Add(new Label
            {
                Location = new Point(x, y), Size = new Size(330, 26),
                Text = title,
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });
        }

        private IndicatorDot AddDot(int x, int y, string text, Color? color = null)
        {
            var dot = new IndicatorDot { Location = new Point(x, y), Size = new Size(14, 14) };
            if (color.HasValue) dot.OnColor = color.Value;
            else dot.OnColor = Color.LimeGreen;
            var lbl = new Label
            {
                Location = new Point(x + 20, y - 4), Size = new Size(220, 22),
                Text = text, Font = new Font("맑은 고딕", 9F)
            };
            Controls.Add(dot);
            Controls.Add(lbl);
            return dot;
        }

        private void Refresh4()
        {
            var host = FindForm() as Form1;
            if (host?.Machine == null) return;
            var op  = host.Machine.OpPanel;
            var res = host.Machine.Resources;
            var ion = host.Machine.Ionizer;

            if (op != null)
            {
                _dotStart.IsOn = op.StartButton.IsOn;
                _dotStop .IsOn = op.StopButton.IsOn;
                _dotReset.IsOn = op.ResetButton.IsOn;
                _dotEmgF .IsOn = op.EmgFront.IsOn;
                _dotEmgL .IsOn = op.EmgLeft.IsOn;
                _dotEmgR .IsOn = op.EmgRear.IsOn;

                _ledStartLamp.IsOn = op.StartLamp.IsOn;
                _ledStopLamp .IsOn = op.StopLamp.IsOn;
                _ledResetLamp.IsOn = op.ResetLamp.IsOn;

                _tlRed   .IsOn = op.TlRed.IsOn;
                _tlYellow.IsOn = op.TlYellow.IsOn;
                _tlGreen .IsOn = op.TlGreen.IsOn;
                _ledBuzzer.IsOn= op.Buzzer.IsOn;
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
    }
}
