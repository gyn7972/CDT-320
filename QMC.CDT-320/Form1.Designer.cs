using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui;
using QMC.CDT_320.Ui.Controls;

// AlarmBanner 는 QMC.CDT_320.Ui.Controls 네임스페이스에 있음

namespace QMC.CDT_320
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // 상단 헤더
        private Panel     pnlHeader;
        private Label     lblLogo;
        private Label     lblTitle;
        private Label     lblVersion;
        private Panel     pnlUserBox;
        private Label     lblUserCaption;
        private Label     lblUserValue;
        private Label     lblTimeCaption;
        private Label     lblTimeValue;
        private Label     lblStateBig;
        private Label     lblSizeBtn;

        // 주황 상태 바
        private Panel     pnlStatusBar;
        private Label     lblMapMode;
        private Label     lblProjectCaption;
        private Label     lblProjectValue;
        private Label     lblBarcodeCaption;
        private Label     lblBarcodeValue;
        private Label     lblBinCaption;
        private Label     lblBinValue;
        private IndicatorDot dotVision;
        private Label     lblVision;
        private IndicatorDot dotPick;
        private Label     lblPick;
        private IndicatorDot dotReference;
        private Label     lblReference;

        // 양쪽 MENU 세로 라벨
        private VerticalLabel lblMenuLeft;
        private VerticalLabel lblMenuRight;

        // 하단 바
        private Panel     pnlBottomBar;
        private BottomMenuButton btnTabWork;
        private BottomMenuButton btnTabWorkInfo;
        private BottomMenuButton btnTabHistory;
        private BottomMenuButton btnTabRecipe;
        private BottomMenuButton btnAxisJog;
        private BottomMenuButton btnAxisPosition;
        private BottomMenuButton btnTabSettings;
        private BottomMenuButton btnTabUser;
        private BottomMenuButton btnTabExit;

        // 메인 콘텐츠 영역
        private Panel     pnlContent;

        // 알람 배너
        private AlarmBanner alarmBanner;

        // 타이머
        private Timer     timerClock;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // ──────────────────────────────────────
            //  Form
            // ──────────────────────────────────────
            this.AutoScaleMode  = AutoScaleMode.None;
            this.ClientSize     = new Size(UiTheme.DesignWidth, UiTheme.DesignHeight);
            this.StartPosition  = FormStartPosition.CenterScreen;
            this.FormBorderStyle= FormBorderStyle.Sizable;
            this.WindowState    = FormWindowState.Maximized;
            this.Text           = "CDT-320";
            this.BackColor      = UiTheme.MainBg;
            this.Font           = UiTheme.ButtonFont;
            this.DoubleBuffered = true;
            this.Load          += new System.EventHandler(this.Form1_Load);

            // ──────────────────────────────────────
            //  상단 헤더
            // ──────────────────────────────────────
            this.pnlHeader = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(UiTheme.DesignWidth, UiTheme.HeaderHeight),
                BackColor = UiTheme.HeaderBg,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            this.lblLogo = new Label
            {
                Location  = new Point(18, 10),
                Size      = new Size(110, 50),
                Text      = "QMC",
                ForeColor = UiTheme.LogoOrange,
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI Black", 28F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            this.lblTitle = new Label
            {
                Location  = new Point(145, 12),
                Size      = new Size(260, 46),
                Text      = "CDT-320",
                ForeColor = UiTheme.HeaderFg,
                BackColor = Color.Transparent,
                Font      = UiTheme.TitleFont,
                TextAlign = ContentAlignment.MiddleLeft
            };

            this.lblVersion = new Label
            {
                Location  = new Point(260, 45),
                Size      = new Size(90, 18),
                Text      = "v0.1.0",
                ForeColor = UiTheme.LogoOrange,
                BackColor = Color.Transparent,
                Font      = UiTheme.HeaderInfoFont,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 사용자/시간 박스 (중앙)
            this.pnlUserBox = new Panel
            {
                Location  = new Point(840, 10),
                Size      = new Size(360, 50),
                BackColor = Color.FromArgb(0x40, 0x40, 0x40)
            };

            this.lblUserCaption = new Label
            {
                Location  = new Point(52, 4),
                Size      = new Size(60, 18),
                Text      = "사용자",
                ForeColor = Color.Silver,
                BackColor = Color.Transparent,
                Font      = UiTheme.HeaderInfoFont
            };
            this.lblUserValue = new Label
            {
                Location  = new Point(118, 4),
                Size      = new Size(230, 18),
                Text      = "NONE",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font      = UiTheme.HeaderInfoFont,
                TextAlign = ContentAlignment.MiddleRight
            };
            this.lblTimeCaption = new Label
            {
                Location  = new Point(52, 26),
                Size      = new Size(60, 18),
                Text      = "시간",
                ForeColor = Color.Silver,
                BackColor = Color.Transparent,
                Font      = UiTheme.HeaderInfoFont
            };
            this.lblTimeValue = new Label
            {
                Location  = new Point(118, 26),
                Size      = new Size(230, 18),
                Text      = "----",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font      = UiTheme.HeaderInfoFont,
                TextAlign = ContentAlignment.MiddleRight
            };

            // 사용자 박스 안 아바타 자리 (원형 placeholder)
            var lblAvatar = new Label
            {
                Location  = new Point(4, 4),
                Size      = new Size(42, 42),
                Text      = "☺",
                ForeColor = Color.Silver,
                BackColor = Color.FromArgb(0x60, 0x60, 0x60),
                Font      = new Font("Segoe UI", 20F),
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.pnlUserBox.Controls.Add(lblAvatar);
            this.pnlUserBox.Controls.Add(this.lblUserCaption);
            this.pnlUserBox.Controls.Add(this.lblUserValue);
            this.pnlUserBox.Controls.Add(this.lblTimeCaption);
            this.pnlUserBox.Controls.Add(this.lblTimeValue);

            // 상태 큰 라벨 (우측, "NONE" 등)
            this.lblStateBig = new Label
            {
                Location  = new Point(1520, 10),
                Size      = new Size(260, 50),
                Text      = "NONE",
                ForeColor = UiTheme.LogoOrange,
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI Light", 26F),
                TextAlign = ContentAlignment.MiddleRight
            };

            this.lblSizeBtn = new Label
            {
                Location  = new Point(1800, 10),
                Size      = new Size(96, 50),
                Text      = "▯",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0x40, 0x40, 0x40),
                Font      = new Font("Segoe UI", 20F),
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.pnlHeader.Controls.Add(this.lblLogo);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Controls.Add(this.lblVersion);
            this.pnlHeader.Controls.Add(this.pnlUserBox);
            this.pnlHeader.Controls.Add(this.lblStateBig);
            this.pnlHeader.Controls.Add(this.lblSizeBtn);

            // ──────────────────────────────────────
            //  주황 상태 바
            // ──────────────────────────────────────
            this.pnlStatusBar = new Panel
            {
                Location  = new Point(0, UiTheme.HeaderHeight),
                Size      = new Size(UiTheme.DesignWidth, UiTheme.StatusBarHeight),
                BackColor = UiTheme.StatusBarBg,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            this.lblMapMode = new Label
            {
                Location  = new Point(12, 4),
                Size      = new Size(80, 22),
                Text      = "빈 맵",
                ForeColor = UiTheme.StatusBarFg,
                BackColor = Color.Transparent,
                Font      = UiTheme.StatusBarFont,
                TextAlign = ContentAlignment.MiddleLeft
            };

            this.lblProjectCaption = new Label { Location = new Point(110, 4), Size = new Size(120, 22), Text = "Project Name :", ForeColor = UiTheme.StatusBarFg, BackColor = Color.Transparent, Font = UiTheme.StatusBarFont };
            this.lblProjectValue   = new Label { Location = new Point(230, 4), Size = new Size(220, 22), Text = "-",                ForeColor = UiTheme.StatusBarFg, BackColor = Color.Transparent, Font = UiTheme.StatusBarFont };
            this.lblBarcodeCaption = new Label { Location = new Point(470, 4), Size = new Size(130, 22), Text = "Barcode Name :", ForeColor = UiTheme.StatusBarFg, BackColor = Color.Transparent, Font = UiTheme.StatusBarFont };
            this.lblBarcodeValue   = new Label { Location = new Point(600, 4), Size = new Size(260, 22), Text = "-",                ForeColor = UiTheme.StatusBarFg, BackColor = Color.Transparent, Font = UiTheme.StatusBarFont };
            this.lblBinCaption     = new Label { Location = new Point(880, 4), Size = new Size(60, 22),  Text = "1Bin :",           ForeColor = UiTheme.StatusBarFg, BackColor = Color.Transparent, Font = UiTheme.StatusBarFont };
            this.lblBinValue       = new Label { Location = new Point(940, 4), Size = new Size(80, 22),  Text = "0",                ForeColor = UiTheme.StatusBarFg, BackColor = Color.Transparent, Font = UiTheme.StatusBarFont };

            // VISION/PICK/REFERENCE dots
            this.dotVision    = new IndicatorDot { Location = new Point(1560, 10), Size = new Size(12,12), IsOn = true,  OnColor = UiTheme.DotVision };
            this.lblVision    = new Label { Location = new Point(1576, 6), Size = new Size(80, 22), Text = "VISION",       ForeColor = UiTheme.StatusBarFg, BackColor = Color.Transparent, Font = UiTheme.StatusBarFont };
            this.dotPick      = new IndicatorDot { Location = new Point(1660, 10), Size = new Size(12,12), IsOn = true,  OnColor = UiTheme.DotPick };
            this.lblPick      = new Label { Location = new Point(1676, 6), Size = new Size(60, 22), Text = "PICK",         ForeColor = UiTheme.StatusBarFg, BackColor = Color.Transparent, Font = UiTheme.StatusBarFont };
            this.dotReference = new IndicatorDot { Location = new Point(1740, 10), Size = new Size(12,12), IsOn = false, OnColor = UiTheme.DotReference };
            this.lblReference = new Label { Location = new Point(1756, 6), Size = new Size(150, 22), Text = "REFERENCE",  ForeColor = UiTheme.StatusBarFg, BackColor = Color.Transparent, Font = UiTheme.StatusBarFont };

            this.pnlStatusBar.Controls.Add(this.lblMapMode);
            this.pnlStatusBar.Controls.Add(this.lblProjectCaption);
            this.pnlStatusBar.Controls.Add(this.lblProjectValue);
            this.pnlStatusBar.Controls.Add(this.lblBarcodeCaption);
            this.pnlStatusBar.Controls.Add(this.lblBarcodeValue);
            this.pnlStatusBar.Controls.Add(this.lblBinCaption);
            this.pnlStatusBar.Controls.Add(this.lblBinValue);
            this.pnlStatusBar.Controls.Add(this.dotVision);
            this.pnlStatusBar.Controls.Add(this.lblVision);
            this.pnlStatusBar.Controls.Add(this.dotPick);
            this.pnlStatusBar.Controls.Add(this.lblPick);
            this.pnlStatusBar.Controls.Add(this.dotReference);
            this.pnlStatusBar.Controls.Add(this.lblReference);

            // ──────────────────────────────────────
            //  좌/우 MENU 세로 라벨
            // ──────────────────────────────────────
            int contentTop    = UiTheme.HeaderHeight + UiTheme.StatusBarHeight;
            int contentBottom = UiTheme.DesignHeight - UiTheme.BottomBarHeight;
            int contentH      = contentBottom - contentTop;

            this.lblMenuLeft = new VerticalLabel
            {
                Location  = new Point(0, contentTop),
                Size      = new Size(UiTheme.MenuLabelWidth, contentH),
                Text      = "MENU",
                Font      = UiTheme.MenuLabelFont,
                ForeColor = UiTheme.MenuLabelFg,
                BackColor = UiTheme.MenuLabelBg,
                Anchor    = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };
            this.lblMenuRight = new VerticalLabel
            {
                Location  = new Point(UiTheme.DesignWidth - UiTheme.MenuLabelWidth, contentTop),
                Size      = new Size(UiTheme.MenuLabelWidth, contentH),
                Text      = "MENU",
                Font      = UiTheme.MenuLabelFont,
                ForeColor = UiTheme.MenuLabelFg,
                BackColor = UiTheme.MenuLabelBg,
                Anchor    = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right
            };

            // ──────────────────────────────────────
            //  콘텐츠 영역 (현재 탭 UserControl 호스팅)
            // ──────────────────────────────────────
            this.pnlContent = new Panel
            {
                Location  = new Point(UiTheme.MenuLabelWidth, contentTop),
                Size      = new Size(UiTheme.ShellContentWidth, contentH),
                BackColor = UiTheme.MainBg,
                Anchor    = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // 알람 배너 (content 상단에 docked)
            this.alarmBanner = new AlarmBanner { Dock = DockStyle.Top };
            this.pnlContent.Controls.Add(this.alarmBanner);

            // ──────────────────────────────────────
            //  하단 바
            // ──────────────────────────────────────
            this.pnlBottomBar = new Panel
            {
                Location  = new Point(0, UiTheme.DesignHeight - UiTheme.BottomBarHeight),
                Size      = new Size(UiTheme.DesignWidth, UiTheme.BottomBarHeight),
                BackColor = UiTheme.BottomBarBg,
                Anchor    = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            int btnY = (UiTheme.BottomBarHeight - 70) / 2;
            // 좌측 4개 — Anchor.Top|Left
            this.btnTabWork      = new BottomMenuButton { Location = new Point(60,   btnY), IconText = "W",  Label = "작업",       Anchor = AnchorStyles.Top | AnchorStyles.Left };
            this.btnTabWorkInfo  = new BottomMenuButton { Location = new Point(180,  btnY), IconText = "i",  Label = "작업 정보",  Anchor = AnchorStyles.Top | AnchorStyles.Left };
            this.btnTabHistory   = new BottomMenuButton { Location = new Point(300,  btnY), IconText = "H",  Label = "이력",       Anchor = AnchorStyles.Top | AnchorStyles.Left };
            this.btnTabRecipe    = new BottomMenuButton { Location = new Point(420,  btnY), IconText = "R",  Label = "레시피",     Anchor = AnchorStyles.Top | AnchorStyles.Left };
            this.btnAxisJog      = new BottomMenuButton { Location = new Point(820,  btnY), IconText = "J",  Label = "JOG",        Anchor = AnchorStyles.Top };
            this.btnAxisPosition = new BottomMenuButton { Location = new Point(940,  btnY), IconText = "P",  Label = "POSITION",   Anchor = AnchorStyles.Top };
            // 우측 3개 — Anchor.Top|Right (해상도 변경에도 우측 정렬 유지)
            this.btnTabSettings  = new BottomMenuButton { Location = new Point(1440, btnY), IconText = "S",  Label = "설정",       Anchor = AnchorStyles.Top | AnchorStyles.Right };
            this.btnTabUser      = new BottomMenuButton { Location = new Point(1560, btnY), IconText = "U",  Label = "사용자",     Anchor = AnchorStyles.Top | AnchorStyles.Right };
            this.btnTabExit      = new BottomMenuButton { Location = new Point(1680, btnY), IconText = "X",  Label = "종료",       Anchor = AnchorStyles.Top | AnchorStyles.Right };

            this.btnTabWork     .Click += (s, e) => this.ShowTab(MainTab.Work);
            this.btnTabWorkInfo .Click += (s, e) => this.ShowTab(MainTab.WorkInfo);
            this.btnTabHistory  .Click += (s, e) => this.ShowTab(MainTab.History);
            this.btnTabRecipe   .Click += (s, e) => this.ShowTab(MainTab.Recipe);
            this.btnAxisJog     .Click += (s, e) => this.ShowOrRestoreJogPopup(this);
            this.btnAxisPosition.Click += (s, e) => this.ShowOrRestoreAxisPositionPopup(this);
            this.btnTabSettings .Click += (s, e) => this.ShowTab(MainTab.Settings);
            this.btnTabUser     .Click += (s, e) => this.ShowTab(MainTab.User);
            this.btnTabExit     .Click += (s, e) => this.RequestApplicationExit();

            this.pnlBottomBar.Controls.Add(this.btnTabWork);
            this.pnlBottomBar.Controls.Add(this.btnTabWorkInfo);
            this.pnlBottomBar.Controls.Add(this.btnTabHistory);
            this.pnlBottomBar.Controls.Add(this.btnTabRecipe);
            this.pnlBottomBar.Controls.Add(this.btnAxisJog);
            this.pnlBottomBar.Controls.Add(this.btnAxisPosition);
            this.pnlBottomBar.Controls.Add(this.btnTabSettings);
            this.pnlBottomBar.Controls.Add(this.btnTabUser);
            this.pnlBottomBar.Controls.Add(this.btnTabExit);

            // ──────────────────────────────────────
            //  시계 타이머
            // ──────────────────────────────────────
            this.timerClock = new Timer(this.components) { Interval = 1000 };
            this.timerClock.Tick += this.TimerClock_Tick;

            // ──────────────────────────────────────
            //  Form에 붙이기 (Z-order 주의)
            // ──────────────────────────────────────
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.lblMenuLeft);
            this.Controls.Add(this.lblMenuRight);
            this.Controls.Add(this.pnlStatusBar);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlBottomBar);
        }
    }
}
