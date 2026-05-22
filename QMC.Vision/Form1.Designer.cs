using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Ui;

namespace QMC.Vision
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private Panel   pnlHeader;
        private Label   lblLogo;
        private Label   lblTitle;
        private Label   lblVersion;
        private Label   lblStateBig;
        private Label   lblClock;

        private Panel   pnlStatusBar;
        private Label   lblStatusL;
        private Label   lblStatusR;

        private Panel   pnlBottomBar;
        private Button  btnOperation;
        private Button  btnConfiguration;
        private Button  btnMaintenance;
        private Button  btnRecipe;
        private Button  btnDataLog;
        private Button  btnExit;

        private Panel   pnlContent;
        private Timer   timerClock;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.AutoScaleMode  = AutoScaleMode.None;
            this.ClientSize     = new Size(1280, 900);
            this.StartPosition  = FormStartPosition.CenterScreen;
            this.WindowState    = FormWindowState.Maximized;
            this.Text           = "CDT-320 VISION";
            this.BackColor      = UiTheme.MainBg;
            this.DoubleBuffered = true;
            this.Load          += new EventHandler(this.Form1_Load);

            // ─── 헤더 ───
            this.pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = UiTheme.HeaderBg };
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
                Text = "v0.1.0", ForeColor = UiTheme.Accent,
                BackColor = Color.Transparent,
                Font = new Font("맑은 고딕", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.lblStateBig = new Label
            {
                Dock = DockStyle.Right, Width = 260, Text = "NONE",
                ForeColor = UiTheme.Accent, BackColor = Color.Transparent,
                Font = new Font("Segoe UI Light", 22F),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 20, 0)
            };
            this.lblClock = new Label
            {
                Location = new Point(780, 22), Size = new Size(220, 28),
                Text = "----", ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Consolas", 12F),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.pnlHeader.Controls.Add(this.lblLogo);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Controls.Add(this.lblVersion);
            this.pnlHeader.Controls.Add(this.lblClock);
            this.pnlHeader.Controls.Add(this.lblStateBig);

            // ─── 오렌지 상태 바 ───
            this.pnlStatusBar = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = UiTheme.StatusBarBg };
            this.lblStatusL = new Label
            {
                Dock = DockStyle.Left, Width = 700,
                Text = "Backend: Sim",
                ForeColor = Color.White, BackColor = Color.Transparent,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0)
            };
            this.lblStatusR = new Label
            {
                Dock = DockStyle.Right, Width = 500,
                Text = "TCP: Wafer=5100  Bin=5103  Bottom=5101",
                ForeColor = Color.White, BackColor = Color.Transparent,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 20, 0)
            };
            this.pnlStatusBar.Controls.Add(this.lblStatusL);
            this.pnlStatusBar.Controls.Add(this.lblStatusR);

            // ─── 바텀 바 ───
            this.pnlBottomBar = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = UiTheme.HeaderBg };
            int bx = 40; int bh = 70; int bw = 160;
            this.btnOperation     = MakeNavButton("Operation",     bx + 0 * (bw + 4), bh, bw);
            this.btnConfiguration = MakeNavButton("Configuration", bx + 1 * (bw + 4), bh, bw);
            this.btnMaintenance   = MakeNavButton("Maintenance",   bx + 2 * (bw + 4), bh, bw);
            this.btnRecipe        = MakeNavButton("Recipe",        bx + 3 * (bw + 4), bh, bw);
            this.btnDataLog       = MakeNavButton("Data Log",      bx + 4 * (bw + 4), bh, bw);
            this.btnExit          = MakeNavButton("Exit",          bx + 5 * (bw + 4), bh, bw);
            this.btnOperation    .Click += (s, e) => ShowTab(Tab.Operation);
            this.btnConfiguration.Click += (s, e) => ShowTab(Tab.Configuration);
            this.btnMaintenance  .Click += (s, e) => ShowTab(Tab.Maintenance);
            this.btnRecipe       .Click += (s, e) => ShowTab(Tab.Recipe);
            this.btnDataLog      .Click += (s, e) => ShowTab(Tab.DataLog);
            this.btnExit         .Click += (s, e) => this.Close();
            this.pnlBottomBar.Controls.Add(this.btnOperation);
            this.pnlBottomBar.Controls.Add(this.btnConfiguration);
            this.pnlBottomBar.Controls.Add(this.btnMaintenance);
            this.pnlBottomBar.Controls.Add(this.btnRecipe);
            this.pnlBottomBar.Controls.Add(this.btnDataLog);
            this.pnlBottomBar.Controls.Add(this.btnExit);

            // 우측 바텀 시각
            var bottomClock = new Label
            {
                Dock = DockStyle.Right, Width = 220,
                Text = "", ForeColor = Color.Silver, BackColor = Color.Transparent,
                Font = new Font("Consolas", 11F),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 20, 0)
            };
            this.pnlBottomBar.Controls.Add(bottomClock);
            this.lblClock = bottomClock;

            // ─── 콘텐츠 ───
            this.pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.MainBg };

            // 타이머
            this.timerClock = new Timer(this.components) { Interval = 1000 };
            this.timerClock.Tick += this.TimerClock_Tick;

            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlBottomBar);
            this.Controls.Add(this.pnlStatusBar);
            this.Controls.Add(this.pnlHeader);
        }

        private static Button MakeNavButton(string text, int x, int h, int w)
            => new Button
            {
                Location  = new Point(x, (80 - h) / 2),
                Size      = new Size(w, h),
                Text      = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.HeaderBg,
                ForeColor = Color.White,
                Font      = new Font("맑은 고딕", 11F, FontStyle.Bold),
                FlatAppearance = { BorderColor = Color.FromArgb(0x45, 0x45, 0x48), BorderSize = 1 }
            };
    }
}
