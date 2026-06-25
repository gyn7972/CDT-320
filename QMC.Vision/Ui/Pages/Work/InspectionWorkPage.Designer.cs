using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Ui;

namespace QMC.Vision.Ui.Pages
{
    partial class InspectionWorkPage
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // 상단 공용 헤더(모든 모드에서 유지)
        private Panel  _header;
        private Label  _lblHeader;     // "작업 — 모니터링" + 레시피/Lot/Wafer/Judge
        private Button _btnRun;        // RUN/STOP
        private Button _btnReady;      // READY

        // 메인 표시 영역 + 우측 세로 버튼바
        private Panel        _contentHost;
        private Panel        _btnBar;
        private FlowLayoutPanel _btnFlow;
        private Button _btnWork;
        private Button _btnSide;
        private Button _btnDie;

        private static readonly Color BtnNav       = Color.FromArgb(0x3A, 0x3A, 0x3E);
        private static readonly Color BtnNavActive = Color.FromArgb(0xD9, 0x77, 0x06);

        private Button MakeNavButton(string text)
        {
            var b = new Button
            {
                Width = 80,
                Height = 54,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold),
                ForeColor = UiTheme.BottomBarFg,
                BackColor = BtnNav,
                Margin = new Padding(0, 0, 0, 12),   // 버튼끼리 세로 간격
                Text = text,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(0x55, 0x55, 0x5A);
            return b;
        }

        private Button MakeHeaderButton(string text)
        {
            return new Button
            {
                Dock = DockStyle.Right,
                Width = 92,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0x55, 0x55, 0x55),
                Margin = new Padding(0),
                Text = text,
                UseVisualStyleBackColor = false
            };
        }

        private void InitializeComponent()
        {
            _header      = new Panel();
            _lblHeader   = new Label();
            _btnRun      = MakeHeaderButton("RUN");
            _btnReady    = MakeHeaderButton("READY");
            _contentHost = new Panel();
            _btnBar      = new Panel();
            _btnFlow     = new FlowLayoutPanel();
            _btnWork     = MakeNavButton("작업");
            _btnSide     = MakeNavButton("Side");
            _btnDie      = MakeNavButton("Die gap");

            SuspendLayout();

            // ── 상단 공용 헤더(풀폭) ──
            _header.Dock = DockStyle.Top;
            _header.Height = 40;
            _header.BackColor = UiTheme.StatusBarBg;   // 주황(작업—모니터링 헤더 색)
            _header.Margin = new Padding(0);
            _header.Name = "_header";
            // 추가 순서: Fill 라벨을 마지막에 → RUN/READY(Right)가 먼저 자리 차지
            _header.Controls.Add(_lblHeader);
            _header.Controls.Add(_btnRun);
            _header.Controls.Add(_btnReady);

            _lblHeader.Dock = DockStyle.Fill;
            _lblHeader.ForeColor = UiTheme.StatusBarFg;
            _lblHeader.Font = new Font("맑은 고딕", 11F, FontStyle.Bold);
            _lblHeader.TextAlign = ContentAlignment.MiddleLeft;
            _lblHeader.Padding = new Padding(12, 0, 0, 0);
            _lblHeader.Text = "작업 — 모니터링        레시피 —     Lot ID —     Wafer ID —     Judge —";

            // ── 메인 표시 영역(나머지 채움) — 먼저 추가(뒤) ──
            _contentHost.Dock = DockStyle.Fill;
            _contentHost.BackColor = UiTheme.MainBg;
            _contentHost.Name = "_contentHost";

            // ── 우측 세로 버튼바(하단바 색상, 버튼은 아래 정렬) ──
            _btnBar.Dock = DockStyle.Right;
            _btnBar.Width = 96;
            _btnBar.BackColor = UiTheme.BottomBarBg;   // 하단 메뉴바와 동일 색
            _btnBar.Name = "_btnBar";
            _btnBar.Controls.Add(_btnFlow);

            _btnFlow.Dock = DockStyle.Bottom;            // 버튼 그룹을 바 하단에 배치
            _btnFlow.FlowDirection = FlowDirection.TopDown;
            _btnFlow.WrapContents = false;
            _btnFlow.AutoSize = true;
            _btnFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _btnFlow.Padding = new Padding(8, 0, 8, 12);
            _btnFlow.Controls.Add(_btnWork);
            _btnFlow.Controls.Add(_btnSide);
            _btnFlow.Controls.Add(_btnDie);

            _btnWork.Click += btnWork_Click;
            _btnSide.Click += btnSide_Click;
            _btnDie.Click  += btnDie_Click;

            // 추가 순서: content(뒤) → nav(우, 헤더 아래) → header(앞, 풀폭 상단)
            Controls.Add(_contentHost);
            Controls.Add(_btnBar);
            Controls.Add(_header);

            Name = "InspectionWorkPage";
            Size = new Size(1280, 800);

            ResumeLayout(false);
        }
    }
}
