using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using QMC.Common.Alarms;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    ///
    ///
    /// </summary>
    public class AlarmBanner : Panel
    {
        private readonly Label  _icon;
        private readonly Label  _msg;
        private readonly Button _btnClear;
        private Timer _blinkTimer;
        private bool  _blinkOn;

        public event EventHandler ClearRequested;

        public AlarmBanner()
        {
            Height = 36;
            Visible = false;
            BackColor = Color.FromArgb(0xC0, 0x39, 0x2B);
            Dock = DockStyle.Top;

            _icon = new Label
            {
                Dock = DockStyle.Left, Width = 40,
                Text = "!", ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Symbol", 18F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _msg = new Label
            {
                Dock = DockStyle.Fill, Text = "",
                ForeColor = Color.White, BackColor = Color.Transparent,
                Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            _btnClear = new Button
            {
                Dock = DockStyle.Right, Width = 100,
                Text = "CLEAR ALL",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0x7A, 0x1F, 0x1A),
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
            };
            _btnClear.FlatAppearance.BorderColor = Color.White;
            _btnClear.FlatAppearance.BorderSize  = 1;
            _btnClear.Click += OnClearClicked;

            Controls.Add(_msg);
            Controls.Add(_btnClear);
            Controls.Add(_icon);

            AlarmManager.AlarmRaised  += OnAlarmChanged;
            AlarmManager.AlarmCleared += OnAlarmChanged;

            _blinkTimer = new Timer { Interval = 500 };
            _blinkTimer.Tick += (s, e) =>
            {
                _blinkOn = !_blinkOn;
                var sev = AlarmManager.HighestActiveSeverity;
                if (sev == AlarmSeverity.Critical)
                    BackColor = _blinkOn ? Color.FromArgb(0xC0, 0x39, 0x2B) : Color.FromArgb(0x7A, 0x1F, 0x1A);
            };

            Refresh();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            AlarmManager.AlarmRaised  -= OnAlarmChanged;
            AlarmManager.AlarmCleared -= OnAlarmChanged;
            _blinkTimer?.Stop();
            base.OnHandleDestroyed(e);
        }

        private void OnAlarmChanged(AlarmRecord rec) => Refresh();

        private void OnClearClicked(object sender, EventArgs e)
        {
            try
            {
                var handler = ClearRequested;
                if (handler != null)
                    handler(this, EventArgs.Empty);
                else
                    AlarmManager.ClearAll();
            }
            catch
            {
                AlarmManager.ClearAll();
            }
            finally
            {
            }
        }

        public new void Refresh()
        {
            if (InvokeRequired) { BeginInvoke(new Action(Refresh)); return; }

            var sev = AlarmManager.HighestActiveSeverity;
            if (sev == null)
            {
                Visible = false;
                _blinkTimer?.Stop();
                return;
            }

            // 구현 보조 주석입니다.
            switch (sev.Value)
            {
                case AlarmSeverity.Warning:  BackColor = Color.FromArgb(0xD9, 0x77, 0x06); _btnClear.BackColor = Color.FromArgb(0x99, 0x54, 0x04); break;
                case AlarmSeverity.Error:    BackColor = Color.FromArgb(0xC0, 0x39, 0x2B); _btnClear.BackColor = Color.FromArgb(0x7A, 0x1F, 0x1A); break;
                case AlarmSeverity.Critical: BackColor = Color.FromArgb(0xC0, 0x39, 0x2B); _btnClear.BackColor = Color.FromArgb(0x7A, 0x1F, 0x1A); break;
            }

            var active = AlarmManager.Active;
            if (active.Count > 0)
            {
                var top = active[active.Count - 1];
                _msg.Text = $"[{top.Severity}] {top.Code} | {top.Source} | {top.Message}"
                            + (active.Count > 1 ? $"   (+ {active.Count - 1} more)" : "");
            }

            Visible = true;
            if (sev == AlarmSeverity.Critical) _blinkTimer.Start();
            else                                _blinkTimer.Stop();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var p = new Pen(Color.FromArgb(60, 0, 0, 0), 1f))
                e.Graphics.DrawLine(p, 0, Height - 1, Width, Height - 1);
        }
    }
}

