using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Ui;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // 헤더
        private Panel pnlHeader;
        private Label lblLogo;
        private Label lblTitle;
        private Label lblVersion;
        private Label lblStateBig;
        private Panel pnlTimeBox;        // 헤더 중앙 시간 박스 (Handler 동일 스타일)
        private Label lblTimeCaption;
        private Label lblClock;          // 실제 시간 표시 (timerClock 이 갱신)

        // 오렌지 상태 바
        private Panel pnlStatusBar;
        private Label lblStatusL;
        private Label lblStatusR;

        // 바텀 바 (Handler 와 동일 스타일 — BottomMenuButton)
        private Panel pnlBottomBar;
        private BottomMenuButton btnOperation;
        private BottomMenuButton btnConfiguration;
        private BottomMenuButton btnRecipe;
        private BottomMenuButton btnDataLog;
        private BottomMenuButton btnSettings;
        private BottomMenuButton btnExit;

        // 콘텐츠
        private Panel pnlContent;
        private Timer timerClock;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.AutoScaleMode  = AutoScaleMode.None;
            this.ClientSize     = new Size(1920, 1080);
            this.StartPosition  = FormStartPosition.CenterScreen;
            this.WindowState    = FormWindowState.Maximized;
            this.Text           = "CDT-320 VISION";
            this.BackColor      = UiTheme.MainBg;
            this.DoubleBuffered = true;
            this.Load          += new EventHandler(this.Form1_Load);

            // ─── 헤더 ───
            this.pnlHeader = new Panel { Dock = DockStyle.Top, Height = UiTheme.HeaderHeight, BackColor = UiTheme.HeaderBg };
            this.lblLogo = new Label
            {
                Location = new Point(18, 10), Size = new Size(140, 50),
                Text = "QMC", ForeColor = UiTheme.Accent,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Black", 28F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            };
            this.lblTitle = new Label
            {
                Location = new Point(170, 12), Size = new Size(380, 46),
                Text = "CDT-320  VISION", ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Light", 22F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.lblVersion = new Label
            {
                Location = new Point(260, 46), Size = new Size(100, 18),
                Text = "v0.2.0", ForeColor = UiTheme.Accent,
                BackColor = Color.Transparent,
                Font = new Font("맑은 고딕", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 헤더 중앙 시간 박스 (Handler 의 pnlUserBox 위치와 동일)
            this.pnlTimeBox = new Panel
            {
                Location  = new Point(760, 10), Size = new Size(400, 50),
                BackColor = Color.FromArgb(0x40, 0x40, 0x40),
                Anchor    = AnchorStyles.Top
            };
            this.lblTimeCaption = new Label
            {
                Location = new Point(20, 14), Size = new Size(60, 22),
                Text = "시간", ForeColor = Color.Silver, BackColor = Color.Transparent,
                Font = new Font("맑은 고딕", 10F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.lblClock = new Label
            {
                Location = new Point(85, 10), Size = new Size(300, 30),
                Text = "----", ForeColor = Color.White, BackColor = Color.Transparent,
                Font = new Font("Consolas", 14F),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.pnlTimeBox.Controls.Add(this.lblTimeCaption);
            this.pnlTimeBox.Controls.Add(this.lblClock);

            this.lblStateBig = new Label
            {
                Dock = DockStyle.Right, Width = 260, Text = "NONE",
                ForeColor = UiTheme.Accent, BackColor = Color.Transparent,
                Font = new Font("Segoe UI Light", 22F),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 20, 0)
            };
            this.pnlHeader.Controls.Add(this.lblLogo);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Controls.Add(this.lblVersion);
            this.pnlHeader.Controls.Add(this.pnlTimeBox);
            this.pnlHeader.Controls.Add(this.lblStateBig);

            // ─── 상태 바 ───
            this.pnlStatusBar = new Panel { Dock = DockStyle.Top, Height = UiTheme.StatusBarHeight, BackColor = UiTheme.StatusBarBg };
            this.lblStatusL = new Label
            {
                Dock = DockStyle.Left, Width = 900,
                Text = "Backend: Sim",
                ForeColor = Color.White, BackColor = Color.Transparent,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0)
            };
            this.lblStatusR = new Label
            {
                Dock = DockStyle.Right, Width = 600,
                Text = "TCP: Wafer=5100  Bin=5103  Bottom=5101",
                ForeColor = Color.White, BackColor = Color.Transparent,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 20, 0)
            };
            this.pnlStatusBar.Controls.Add(this.lblStatusL);
            this.pnlStatusBar.Controls.Add(this.lblStatusR);

            // ─── 바텀 바 — 좌측 4 + 우측 3 (Exit 는 최우측) ───
            this.pnlBottomBar = new Panel
            {
                Dock = DockStyle.Bottom, Height = UiTheme.BottomBarHeight, BackColor = UiTheme.BottomBarBg
            };

            int btnY = (UiTheme.BottomBarHeight - 70) / 2;

            // 좌측 3개 — Anchor Left (Stage 65: 정비 제거 → 운영/환경설정/레시피)
            this.btnOperation     = new BottomMenuButton { Location = new Point(60,   btnY), IconText = "▶", Label = "운영",       Anchor = AnchorStyles.Top | AnchorStyles.Left };
            this.btnConfiguration = new BottomMenuButton { Location = new Point(180,  btnY), IconText = "C", Label = "환경설정",   Anchor = AnchorStyles.Top | AnchorStyles.Left };
            this.btnRecipe        = new BottomMenuButton { Location = new Point(300,  btnY), IconText = "R", Label = "레시피",     Anchor = AnchorStyles.Top | AnchorStyles.Left };
            // 우측 3개 — Anchor 미사용 (Designer 시점의 폭이 부정확 → LayoutBottomBar 가 런타임 배치)
            this.btnDataLog       = new BottomMenuButton { Location = new Point(0, btnY), IconText = "L", Label = "데이터로그", Anchor = AnchorStyles.Top | AnchorStyles.Left };
            this.btnSettings      = new BottomMenuButton { Location = new Point(0, btnY), IconText = "S", Label = "설정",       Anchor = AnchorStyles.Top | AnchorStyles.Left };
            this.btnExit          = new BottomMenuButton { Location = new Point(0, btnY), IconText = "X", Label = "종료",       Anchor = AnchorStyles.Top | AnchorStyles.Left };

            this.btnOperation    .Click += (s, e) => ShowTab(Tab.Operation);
            this.btnConfiguration.Click += (s, e) => ShowTab(Tab.Configuration);
            this.btnRecipe       .Click += (s, e) => ShowTab(Tab.Recipe);
            this.btnDataLog      .Click += (s, e) => ShowTab(Tab.DataLog);
            this.btnSettings     .Click += (s, e) => ShowTab(Tab.Settings);
            this.btnExit         .Click += (s, e) => this.Close();

            this.pnlBottomBar.Controls.Add(this.btnOperation);
            this.pnlBottomBar.Controls.Add(this.btnConfiguration);
            this.pnlBottomBar.Controls.Add(this.btnRecipe);
            this.pnlBottomBar.Controls.Add(this.btnDataLog);
            this.pnlBottomBar.Controls.Add(this.btnSettings);
            this.pnlBottomBar.Controls.Add(this.btnExit);

            // ─── 콘텐츠 ───
            this.pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.MainBg };

            // 타이머
            this.timerClock = new Timer(this.components) { Interval = 1000 };
            this.timerClock.Tick += this.TimerClock_Tick;

            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlBottomBar);
            this.Controls.Add(this.pnlStatusBar);
            this.Controls.Add(this.pnlHeader);

            // 헤더 시간 박스를 폼 너비에 맞춰 중앙 정렬 (Resize 핸들러)
            this.Resize += (s, e) => CenterTimeBox();
            this.pnlHeader.Resize += (s, e) => CenterTimeBox();

            // ─── 하단바 우측 버튼 묶음 — 패널 실제 너비 기준 런타임 배치 ───
            // 디자이너에서 Anchor=Top|Right 로는 패널이 폼에 붙기 전 너비(기본 200)가 기록돼
            // Maximized 시 화면 오른쪽 바깥으로 밀려남. → SizeChanged 마다 우측 끝부터 역순 배치.
            this.pnlBottomBar.SizeChanged += (s, e) => LayoutBottomBar();
            this.Shown += (s, e) => LayoutBottomBar();
        }

        private void CenterTimeBox()
        {
            if (pnlTimeBox == null || pnlHeader == null) return;
            int x = (pnlHeader.Width - pnlTimeBox.Width) / 2;
            if (x < 0) x = 0;
            pnlTimeBox.Location = new Point(x, pnlTimeBox.Location.Y);
        }

        /// <summary>하단바 우측 버튼 (데이터로그/설정/종료) 을 패널 실제 너비 기준으로 배치.
        /// pnlBottomBar.SizeChanged + Form.Shown 양쪽에서 호출.</summary>
        private void LayoutBottomBar()
        {
            if (pnlBottomBar == null || btnExit == null || btnSettings == null || btnDataLog == null) return;
            int w      = pnlBottomBar.ClientSize.Width;
            int btnW   = btnExit.Width  > 0 ? btnExit.Width  : 110;
            int gap    = 10;
            int margin = 20;
            int y      = (UiTheme.BottomBarHeight - (btnExit.Height > 0 ? btnExit.Height : 70)) / 2;

            // 우측 끝부터 역순: 종료 → 설정 → 데이터로그
            int x = w - margin - btnW;
            btnExit     .Location = new Point(x, y); x -= (btnW + gap);
            btnSettings .Location = new Point(x, y); x -= (btnW + gap);
            btnDataLog  .Location = new Point(x, y);
        }
    }
}
