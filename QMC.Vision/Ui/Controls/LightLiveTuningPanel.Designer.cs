using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    partial class LightLiveTuningPanel
    {
        // ─── 컨트롤 필드 ───
        private NumericUpDown _numPeriod;
        private Button        _btnToggleLive;
        private Button        _btnAllOn;
        private Button        _btnAllOff;
        private Label         _lblCount;
        private Label         _lblLast;
        private Label         _lblStatus;

        // ─── Dispose ───
        private IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { _timer?.Stop(); _timer?.Dispose(); } catch { }
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }

        // ─── InitializeComponent — 컨트롤 배치 + 이벤트 named handler 연결만 ───
        private void InitializeComponent()
        {
            this.BackColor   = Color.WhiteSmoke;
            this.BorderStyle = BorderStyle.FixedSingle;

            int y = 10;

            // 헤더 (임시 표시 — 빨간 배경)
            var hdr = new Label
            {
                Location = new Point(0, 0), Size = new Size(220, 28),
                Text = "라이브 튜닝 (임시)",
                BackColor = Color.FromArgb(0xB3, 0x1B, 0x1B), ForeColor = Color.White,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(hdr);
            y = 38;

            // 주기 라벨
            var lblPeriod = new Label
            {
                Location = new Point(8, y + 4), Size = new Size(70, 20),
                Text = "주기 (ms)"
            };
            this.Controls.Add(lblPeriod);

            _numPeriod = new NumericUpDown
            {
                Location = new Point(82, y), Size = new Size(80, 24),
                Minimum = 10, Maximum = 1000, Value = 50, Increment = 10
            };
            _numPeriod.ValueChanged += OnPeriodChanged;
            this.Controls.Add(_numPeriod);
            y += 36;

            // 라이브 토글
            _btnToggleLive = new Button
            {
                Location = new Point(8, y), Size = new Size(200, 38),
                Text = "▶ 라이브 시작",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(0x2E, 0x7D, 0x32),
                ForeColor = Color.White
            };
            _btnToggleLive.Click += OnToggleLiveClick;
            this.Controls.Add(_btnToggleLive);
            y += 48;

            // 전체 ON / OFF 단발
            _btnAllOn = new Button
            {
                Location = new Point(8, y), Size = new Size(96, 32),
                Text = "전체 ON", FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            _btnAllOn.Click += OnAllOnClick;
            this.Controls.Add(_btnAllOn);

            _btnAllOff = new Button
            {
                Location = new Point(112, y), Size = new Size(96, 32),
                Text = "전체 OFF", FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            _btnAllOff.Click += OnAllOffClick;
            this.Controls.Add(_btnAllOff);
            y += 42;

            // 카운터
            _lblCount = new Label
            {
                Location = new Point(8, y), Size = new Size(200, 20),
                Text = "송신: 0회"
            };
            this.Controls.Add(_lblCount);
            y += 22;

            _lblLast = new Label
            {
                Location = new Point(8, y), Size = new Size(200, 20),
                Text = "마지막: --:--:--"
            };
            this.Controls.Add(_lblLast);
            y += 26;

            // 상태
            _lblStatus = new Label
            {
                Location = new Point(8, y), Size = new Size(204, 40),
                Text = "상태: 대기",
                Font = new Font("맑은 고딕", 9F),
                ForeColor = Color.DarkSlateGray
            };
            this.Controls.Add(_lblStatus);
        }
    }
}
