using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private Panel pnlHeader;
        private Label lblLogo;
        private Label lblTitle;
        private Label lblVersion;
        private Panel pnlUserBox;
        private Label lblUserCaption;
        private Label lblUserValue;
        private Label lblTimeCaption;
        private Label lblTimeValue;
        private Label lblAvatar;
        private Button btnDoorToggle;
        private Button btnBuzzerStop;
        private Label lblStateBig;
        private Label lblSizeBtn;

        private Panel pnlStatusBar;
        private Label lblMapMode;
        private Label lblProjectCaption;
        private Label lblProjectValue;
        private Label lblBarcodeCaption;
        private Label lblBarcodeValue;
        private Label lblBinCaption;
        private Label lblBinValue;
        private IndicatorDot dotVision;
        private Label lblVision;
        private IndicatorDot dotPick;
        private Label lblPick;
        private IndicatorDot dotReference;
        private Label lblReference;

        private VerticalLabel lblMenuLeft;
        private VerticalLabel lblMenuRight;

        private Panel pnlBottomBar;
        private BottomMenuButton btnTabWork;
        private BottomMenuButton btnTabWorkInfo;
        private BottomMenuButton btnTabHistory;
        private BottomMenuButton btnTabRecipe;
        private BottomMenuButton btnAxisJog;
        private BottomMenuButton btnAxisPosition;
        private BottomMenuButton btnTabSettings;
        private BottomMenuButton btnTabUser;
        private BottomMenuButton btnTabExit;

        private Panel pnlContent;
        private AlarmBanner alarmBanner;
        private Timer timerClock;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblLogo = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.pnlUserBox = new System.Windows.Forms.Panel();
            this.lblAvatar = new System.Windows.Forms.Label();
            this.lblUserCaption = new System.Windows.Forms.Label();
            this.lblUserValue = new System.Windows.Forms.Label();
            this.lblTimeCaption = new System.Windows.Forms.Label();
            this.lblTimeValue = new System.Windows.Forms.Label();
            this.btnDoorToggle = new System.Windows.Forms.Button();
            this.btnBuzzerStop = new System.Windows.Forms.Button();
            this.lblStateBig = new System.Windows.Forms.Label();
            this.lblSizeBtn = new System.Windows.Forms.Label();
            this.pnlStatusBar = new System.Windows.Forms.Panel();
            this.lblMapMode = new System.Windows.Forms.Label();
            this.lblProjectCaption = new System.Windows.Forms.Label();
            this.lblProjectValue = new System.Windows.Forms.Label();
            this.lblBarcodeCaption = new System.Windows.Forms.Label();
            this.lblBarcodeValue = new System.Windows.Forms.Label();
            this.lblBinCaption = new System.Windows.Forms.Label();
            this.lblBinValue = new System.Windows.Forms.Label();
            this.dotVision = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblVision = new System.Windows.Forms.Label();
            this.dotPick = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblPick = new System.Windows.Forms.Label();
            this.dotReference = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblReference = new System.Windows.Forms.Label();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.alarmBanner = new QMC.CDT_320.Ui.Controls.AlarmBanner();
            this.pnlBottomBar = new System.Windows.Forms.Panel();
            this.btnTabWork = new QMC.CDT_320.Ui.Controls.BottomMenuButton();
            this.btnTabWorkInfo = new QMC.CDT_320.Ui.Controls.BottomMenuButton();
            this.btnTabHistory = new QMC.CDT_320.Ui.Controls.BottomMenuButton();
            this.btnTabRecipe = new QMC.CDT_320.Ui.Controls.BottomMenuButton();
            this.btnAxisJog = new QMC.CDT_320.Ui.Controls.BottomMenuButton();
            this.btnAxisPosition = new QMC.CDT_320.Ui.Controls.BottomMenuButton();
            this.btnTabSettings = new QMC.CDT_320.Ui.Controls.BottomMenuButton();
            this.btnTabUser = new QMC.CDT_320.Ui.Controls.BottomMenuButton();
            this.btnTabExit = new QMC.CDT_320.Ui.Controls.BottomMenuButton();
            this.timerClock = new System.Windows.Forms.Timer(this.components);
            this.lblMenuLeft = new QMC.CDT_320.Ui.Controls.VerticalLabel();
            this.lblMenuRight = new QMC.CDT_320.Ui.Controls.VerticalLabel();
            this.pnlHeader.SuspendLayout();
            this.pnlUserBox.SuspendLayout();
            this.pnlStatusBar.SuspendLayout();
            this.pnlContent.SuspendLayout();
            this.pnlBottomBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.pnlHeader.Controls.Add(this.lblLogo);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Controls.Add(this.lblVersion);
            this.pnlHeader.Controls.Add(this.pnlUserBox);
            this.pnlHeader.Controls.Add(this.btnDoorToggle);
            this.pnlHeader.Controls.Add(this.btnBuzzerStop);
            this.pnlHeader.Controls.Add(this.lblStateBig);
            this.pnlHeader.Controls.Add(this.lblSizeBtn);
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(1920, 70);
            this.pnlHeader.TabIndex = 4;
            // 
            // lblLogo
            // 
            this.lblLogo.BackColor = System.Drawing.Color.Transparent;
            this.lblLogo.Font = new System.Drawing.Font("Segoe UI Black", 28F, System.Drawing.FontStyle.Bold);
            this.lblLogo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this.lblLogo.Location = new System.Drawing.Point(18, 10);
            this.lblLogo.Name = "lblLogo";
            this.lblLogo.Size = new System.Drawing.Size(133, 50);
            this.lblLogo.TabIndex = 0;
            this.lblLogo.Text = "QMC";
            this.lblLogo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI Light", 28F);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(139, 14);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(168, 46);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "CDT-320";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVersion
            // 
            this.lblVersion.BackColor = System.Drawing.Color.Transparent;
            this.lblVersion.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this.lblVersion.Location = new System.Drawing.Point(309, 42);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(90, 18);
            this.lblVersion.TabIndex = 2;
            this.lblVersion.Text = "v0.1.0";
            this.lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlUserBox
            // 
            this.pnlUserBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.pnlUserBox.Controls.Add(this.lblAvatar);
            this.pnlUserBox.Controls.Add(this.lblUserCaption);
            this.pnlUserBox.Controls.Add(this.lblUserValue);
            this.pnlUserBox.Controls.Add(this.lblTimeCaption);
            this.pnlUserBox.Controls.Add(this.lblTimeValue);
            this.pnlUserBox.Location = new System.Drawing.Point(405, 10);
            this.pnlUserBox.Name = "pnlUserBox";
            this.pnlUserBox.Size = new System.Drawing.Size(360, 50);
            this.pnlUserBox.TabIndex = 3;
            // 
            // lblAvatar
            // 
            this.lblAvatar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(96)))), ((int)(((byte)(96)))));
            this.lblAvatar.Font = new System.Drawing.Font("Segoe UI", 20F);
            this.lblAvatar.ForeColor = System.Drawing.Color.Silver;
            this.lblAvatar.Location = new System.Drawing.Point(4, 4);
            this.lblAvatar.Name = "lblAvatar";
            this.lblAvatar.Size = new System.Drawing.Size(42, 42);
            this.lblAvatar.TabIndex = 0;
            this.lblAvatar.Text = "O";
            this.lblAvatar.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblUserCaption
            // 
            this.lblUserCaption.BackColor = System.Drawing.Color.Transparent;
            this.lblUserCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblUserCaption.ForeColor = System.Drawing.Color.Silver;
            this.lblUserCaption.Location = new System.Drawing.Point(52, 4);
            this.lblUserCaption.Name = "lblUserCaption";
            this.lblUserCaption.Size = new System.Drawing.Size(60, 18);
            this.lblUserCaption.TabIndex = 1;
            this.lblUserCaption.Text = "사용자";
            // 
            // lblUserValue
            // 
            this.lblUserValue.BackColor = System.Drawing.Color.Transparent;
            this.lblUserValue.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblUserValue.ForeColor = System.Drawing.Color.White;
            this.lblUserValue.Location = new System.Drawing.Point(118, 4);
            this.lblUserValue.Name = "lblUserValue";
            this.lblUserValue.Size = new System.Drawing.Size(230, 18);
            this.lblUserValue.TabIndex = 2;
            this.lblUserValue.Text = "NONE";
            this.lblUserValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTimeCaption
            // 
            this.lblTimeCaption.BackColor = System.Drawing.Color.Transparent;
            this.lblTimeCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblTimeCaption.ForeColor = System.Drawing.Color.Silver;
            this.lblTimeCaption.Location = new System.Drawing.Point(52, 26);
            this.lblTimeCaption.Name = "lblTimeCaption";
            this.lblTimeCaption.Size = new System.Drawing.Size(60, 18);
            this.lblTimeCaption.TabIndex = 3;
            this.lblTimeCaption.Text = "시간";
            // 
            // lblTimeValue
            // 
            this.lblTimeValue.BackColor = System.Drawing.Color.Transparent;
            this.lblTimeValue.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblTimeValue.ForeColor = System.Drawing.Color.White;
            this.lblTimeValue.Location = new System.Drawing.Point(118, 26);
            this.lblTimeValue.Name = "lblTimeValue";
            this.lblTimeValue.Size = new System.Drawing.Size(230, 18);
            this.lblTimeValue.TabIndex = 4;
            this.lblTimeValue.Text = "----";
            this.lblTimeValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnDoorToggle
            // 
            this.btnDoorToggle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(92)))), ((int)(((byte)(76)))));
            this.btnDoorToggle.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDoorToggle.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(150)))), ((int)(((byte)(136)))));
            this.btnDoorToggle.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(76)))), ((int)(((byte)(64)))));
            this.btnDoorToggle.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(108)))), ((int)(((byte)(90)))));
            this.btnDoorToggle.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDoorToggle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnDoorToggle.ForeColor = System.Drawing.Color.White;
            this.btnDoorToggle.Location = new System.Drawing.Point(1277, 14);
            this.btnDoorToggle.Name = "btnDoorToggle";
            this.btnDoorToggle.Size = new System.Drawing.Size(90, 42);
            this.btnDoorToggle.TabIndex = 4;
            this.btnDoorToggle.Text = "DOOR\r\nCLOSE";
            this.btnDoorToggle.UseVisualStyleBackColor = false;
            // 
            // btnBuzzerStop
            // 
            this.btnBuzzerStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(92)))), ((int)(((byte)(64)))), ((int)(((byte)(34)))));
            this.btnBuzzerStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBuzzerStop.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(140)))), ((int)(((byte)(48)))));
            this.btnBuzzerStop.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(52)))), ((int)(((byte)(28)))));
            this.btnBuzzerStop.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(76)))), ((int)(((byte)(40)))));
            this.btnBuzzerStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBuzzerStop.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnBuzzerStop.ForeColor = System.Drawing.Color.White;
            this.btnBuzzerStop.Location = new System.Drawing.Point(1374, 14);
            this.btnBuzzerStop.Name = "btnBuzzerStop";
            this.btnBuzzerStop.Size = new System.Drawing.Size(90, 42);
            this.btnBuzzerStop.TabIndex = 5;
            this.btnBuzzerStop.Text = "BUZZER\r\nSTOP";
            this.btnBuzzerStop.UseVisualStyleBackColor = false;
            // 
            // lblStateBig
            // 
            this.lblStateBig.BackColor = System.Drawing.Color.Transparent;
            this.lblStateBig.Font = new System.Drawing.Font("Segoe UI", 23F, System.Drawing.FontStyle.Bold);
            this.lblStateBig.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this.lblStateBig.Location = new System.Drawing.Point(1480, 10);
            this.lblStateBig.Name = "lblStateBig";
            this.lblStateBig.Size = new System.Drawing.Size(300, 50);
            this.lblStateBig.TabIndex = 6;
            this.lblStateBig.Text = "NONE";
            this.lblStateBig.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblSizeBtn
            // 
            this.lblSizeBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblSizeBtn.Font = new System.Drawing.Font("Segoe UI", 20F);
            this.lblSizeBtn.ForeColor = System.Drawing.Color.White;
            this.lblSizeBtn.Location = new System.Drawing.Point(1800, 10);
            this.lblSizeBtn.Name = "lblSizeBtn";
            this.lblSizeBtn.Size = new System.Drawing.Size(96, 50);
            this.lblSizeBtn.TabIndex = 7;
            this.lblSizeBtn.Text = "[]";
            this.lblSizeBtn.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pnlStatusBar
            // 
            this.pnlStatusBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlStatusBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
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
            this.pnlStatusBar.Location = new System.Drawing.Point(0, 70);
            this.pnlStatusBar.Name = "pnlStatusBar";
            this.pnlStatusBar.Size = new System.Drawing.Size(1920, 30);
            this.pnlStatusBar.TabIndex = 3;
            // 
            // lblMapMode
            // 
            this.lblMapMode.BackColor = System.Drawing.Color.Transparent;
            this.lblMapMode.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblMapMode.ForeColor = System.Drawing.Color.White;
            this.lblMapMode.Location = new System.Drawing.Point(12, 4);
            this.lblMapMode.Name = "lblMapMode";
            this.lblMapMode.Size = new System.Drawing.Size(80, 22);
            this.lblMapMode.TabIndex = 0;
            this.lblMapMode.Text = "빈 맵";
            this.lblMapMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblProjectCaption
            // 
            this.lblProjectCaption.BackColor = System.Drawing.Color.Transparent;
            this.lblProjectCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblProjectCaption.ForeColor = System.Drawing.Color.White;
            this.lblProjectCaption.Location = new System.Drawing.Point(110, 4);
            this.lblProjectCaption.Name = "lblProjectCaption";
            this.lblProjectCaption.Size = new System.Drawing.Size(120, 22);
            this.lblProjectCaption.TabIndex = 1;
            this.lblProjectCaption.Text = "Project Name :";
            // 
            // lblProjectValue
            // 
            this.lblProjectValue.BackColor = System.Drawing.Color.Transparent;
            this.lblProjectValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblProjectValue.ForeColor = System.Drawing.Color.White;
            this.lblProjectValue.Location = new System.Drawing.Point(230, 4);
            this.lblProjectValue.Name = "lblProjectValue";
            this.lblProjectValue.Size = new System.Drawing.Size(220, 22);
            this.lblProjectValue.TabIndex = 2;
            this.lblProjectValue.Text = "-";
            // 
            // lblBarcodeCaption
            // 
            this.lblBarcodeCaption.BackColor = System.Drawing.Color.Transparent;
            this.lblBarcodeCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblBarcodeCaption.ForeColor = System.Drawing.Color.White;
            this.lblBarcodeCaption.Location = new System.Drawing.Point(470, 4);
            this.lblBarcodeCaption.Name = "lblBarcodeCaption";
            this.lblBarcodeCaption.Size = new System.Drawing.Size(130, 22);
            this.lblBarcodeCaption.TabIndex = 3;
            this.lblBarcodeCaption.Text = "Barcode Name :";
            // 
            // lblBarcodeValue
            // 
            this.lblBarcodeValue.BackColor = System.Drawing.Color.Transparent;
            this.lblBarcodeValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblBarcodeValue.ForeColor = System.Drawing.Color.White;
            this.lblBarcodeValue.Location = new System.Drawing.Point(600, 4);
            this.lblBarcodeValue.Name = "lblBarcodeValue";
            this.lblBarcodeValue.Size = new System.Drawing.Size(260, 22);
            this.lblBarcodeValue.TabIndex = 4;
            this.lblBarcodeValue.Text = "-";
            // 
            // lblBinCaption
            // 
            this.lblBinCaption.BackColor = System.Drawing.Color.Transparent;
            this.lblBinCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblBinCaption.ForeColor = System.Drawing.Color.White;
            this.lblBinCaption.Location = new System.Drawing.Point(880, 4);
            this.lblBinCaption.Name = "lblBinCaption";
            this.lblBinCaption.Size = new System.Drawing.Size(60, 22);
            this.lblBinCaption.TabIndex = 5;
            this.lblBinCaption.Text = "1Bin :";
            // 
            // lblBinValue
            // 
            this.lblBinValue.BackColor = System.Drawing.Color.Transparent;
            this.lblBinValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblBinValue.ForeColor = System.Drawing.Color.White;
            this.lblBinValue.Location = new System.Drawing.Point(940, 4);
            this.lblBinValue.Name = "lblBinValue";
            this.lblBinValue.Size = new System.Drawing.Size(80, 22);
            this.lblBinValue.TabIndex = 6;
            this.lblBinValue.Text = "0";
            // 
            // dotVision
            // 
            this.dotVision.BackColor = System.Drawing.Color.Transparent;
            this.dotVision.IsOn = true;
            this.dotVision.Location = new System.Drawing.Point(1560, 10);
            this.dotVision.Name = "dotVision";
            this.dotVision.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotVision.OnColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(188)))), ((int)(((byte)(212)))));
            this.dotVision.Size = new System.Drawing.Size(12, 12);
            this.dotVision.TabIndex = 7;
            // 
            // lblVision
            // 
            this.lblVision.BackColor = System.Drawing.Color.Transparent;
            this.lblVision.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblVision.ForeColor = System.Drawing.Color.White;
            this.lblVision.Location = new System.Drawing.Point(1576, 6);
            this.lblVision.Name = "lblVision";
            this.lblVision.Size = new System.Drawing.Size(80, 22);
            this.lblVision.TabIndex = 8;
            this.lblVision.Text = "VISION";
            // 
            // dotPick
            // 
            this.dotPick.BackColor = System.Drawing.Color.Transparent;
            this.dotPick.IsOn = true;
            this.dotPick.Location = new System.Drawing.Point(1660, 10);
            this.dotPick.Name = "dotPick";
            this.dotPick.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotPick.OnColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(199)))), ((int)(((byte)(24)))));
            this.dotPick.Size = new System.Drawing.Size(12, 12);
            this.dotPick.TabIndex = 9;
            // 
            // lblPick
            // 
            this.lblPick.BackColor = System.Drawing.Color.Transparent;
            this.lblPick.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblPick.ForeColor = System.Drawing.Color.White;
            this.lblPick.Location = new System.Drawing.Point(1676, 6);
            this.lblPick.Name = "lblPick";
            this.lblPick.Size = new System.Drawing.Size(60, 22);
            this.lblPick.TabIndex = 10;
            this.lblPick.Text = "PICK";
            // 
            // dotReference
            // 
            this.dotReference.BackColor = System.Drawing.Color.Transparent;
            this.dotReference.Location = new System.Drawing.Point(1740, 10);
            this.dotReference.Name = "dotReference";
            this.dotReference.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotReference.OnColor = System.Drawing.Color.FromArgb(((int)(((byte)(189)))), ((int)(((byte)(189)))), ((int)(((byte)(189)))));
            this.dotReference.Size = new System.Drawing.Size(12, 12);
            this.dotReference.TabIndex = 11;
            // 
            // lblReference
            // 
            this.lblReference.BackColor = System.Drawing.Color.Transparent;
            this.lblReference.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblReference.ForeColor = System.Drawing.Color.White;
            this.lblReference.Location = new System.Drawing.Point(1756, 6);
            this.lblReference.Name = "lblReference";
            this.lblReference.Size = new System.Drawing.Size(150, 22);
            this.lblReference.TabIndex = 12;
            this.lblReference.Text = "REFERENCE";
            // 
            // pnlContent
            // 
            this.pnlContent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlContent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.pnlContent.Controls.Add(this.alarmBanner);
            this.pnlContent.Location = new System.Drawing.Point(16, 100);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Size = new System.Drawing.Size(1888, 900);
            this.pnlContent.TabIndex = 0;
            // 
            // alarmBanner
            // 
            this.alarmBanner.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(57)))), ((int)(((byte)(43)))));
            this.alarmBanner.Dock = System.Windows.Forms.DockStyle.Top;
            this.alarmBanner.Location = new System.Drawing.Point(0, 0);
            this.alarmBanner.Name = "alarmBanner";
            this.alarmBanner.Size = new System.Drawing.Size(1888, 36);
            this.alarmBanner.TabIndex = 0;
            this.alarmBanner.Visible = false;
            // 
            // pnlBottomBar
            // 
            this.pnlBottomBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlBottomBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.pnlBottomBar.Controls.Add(this.btnTabWork);
            this.pnlBottomBar.Controls.Add(this.btnTabWorkInfo);
            this.pnlBottomBar.Controls.Add(this.btnTabHistory);
            this.pnlBottomBar.Controls.Add(this.btnTabRecipe);
            this.pnlBottomBar.Controls.Add(this.btnAxisJog);
            this.pnlBottomBar.Controls.Add(this.btnAxisPosition);
            this.pnlBottomBar.Controls.Add(this.btnTabSettings);
            this.pnlBottomBar.Controls.Add(this.btnTabUser);
            this.pnlBottomBar.Controls.Add(this.btnTabExit);
            this.pnlBottomBar.Location = new System.Drawing.Point(0, 1000);
            this.pnlBottomBar.Name = "pnlBottomBar";
            this.pnlBottomBar.Size = new System.Drawing.Size(1920, 80);
            this.pnlBottomBar.TabIndex = 5;
            // 
            // btnTabWork
            // 
            this.btnTabWork.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnTabWork.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTabWork.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnTabWork.ForeColor = System.Drawing.Color.White;
            this.btnTabWork.IconText = "W";
            this.btnTabWork.Label = "작업";
            this.btnTabWork.Location = new System.Drawing.Point(60, 5);
            this.btnTabWork.Name = "btnTabWork";
            this.btnTabWork.Selected = false;
            this.btnTabWork.Size = new System.Drawing.Size(110, 70);
            this.btnTabWork.TabIndex = 0;
            // 
            // btnTabWorkInfo
            // 
            this.btnTabWorkInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnTabWorkInfo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTabWorkInfo.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnTabWorkInfo.ForeColor = System.Drawing.Color.White;
            this.btnTabWorkInfo.IconText = "i";
            this.btnTabWorkInfo.Label = "작업 정보";
            this.btnTabWorkInfo.Location = new System.Drawing.Point(180, 5);
            this.btnTabWorkInfo.Name = "btnTabWorkInfo";
            this.btnTabWorkInfo.Selected = false;
            this.btnTabWorkInfo.Size = new System.Drawing.Size(110, 70);
            this.btnTabWorkInfo.TabIndex = 1;
            // 
            // btnTabHistory
            // 
            this.btnTabHistory.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnTabHistory.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTabHistory.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnTabHistory.ForeColor = System.Drawing.Color.White;
            this.btnTabHistory.IconText = "H";
            this.btnTabHistory.Label = "이력";
            this.btnTabHistory.Location = new System.Drawing.Point(300, 5);
            this.btnTabHistory.Name = "btnTabHistory";
            this.btnTabHistory.Selected = false;
            this.btnTabHistory.Size = new System.Drawing.Size(110, 70);
            this.btnTabHistory.TabIndex = 2;
            // 
            // btnTabRecipe
            // 
            this.btnTabRecipe.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnTabRecipe.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTabRecipe.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnTabRecipe.ForeColor = System.Drawing.Color.White;
            this.btnTabRecipe.IconText = "R";
            this.btnTabRecipe.Label = "레시피";
            this.btnTabRecipe.Location = new System.Drawing.Point(420, 5);
            this.btnTabRecipe.Name = "btnTabRecipe";
            this.btnTabRecipe.Selected = false;
            this.btnTabRecipe.Size = new System.Drawing.Size(110, 70);
            this.btnTabRecipe.TabIndex = 3;
            // 
            // btnAxisJog
            // 
            this.btnAxisJog.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnAxisJog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnAxisJog.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAxisJog.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnAxisJog.ForeColor = System.Drawing.Color.White;
            this.btnAxisJog.IconText = "J";
            this.btnAxisJog.Label = "JOG";
            this.btnAxisJog.Location = new System.Drawing.Point(820, 5);
            this.btnAxisJog.Name = "btnAxisJog";
            this.btnAxisJog.Selected = false;
            this.btnAxisJog.Size = new System.Drawing.Size(110, 70);
            this.btnAxisJog.TabIndex = 4;
            // 
            // btnAxisPosition
            // 
            this.btnAxisPosition.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnAxisPosition.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnAxisPosition.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAxisPosition.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnAxisPosition.ForeColor = System.Drawing.Color.White;
            this.btnAxisPosition.IconText = "P";
            this.btnAxisPosition.Label = "POSITION";
            this.btnAxisPosition.Location = new System.Drawing.Point(940, 5);
            this.btnAxisPosition.Name = "btnAxisPosition";
            this.btnAxisPosition.Selected = false;
            this.btnAxisPosition.Size = new System.Drawing.Size(110, 70);
            this.btnAxisPosition.TabIndex = 5;
            // 
            // btnTabSettings
            // 
            this.btnTabSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTabSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnTabSettings.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTabSettings.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnTabSettings.ForeColor = System.Drawing.Color.White;
            this.btnTabSettings.IconText = "S";
            this.btnTabSettings.Label = "설정";
            this.btnTabSettings.Location = new System.Drawing.Point(1440, 5);
            this.btnTabSettings.Name = "btnTabSettings";
            this.btnTabSettings.Selected = false;
            this.btnTabSettings.Size = new System.Drawing.Size(110, 70);
            this.btnTabSettings.TabIndex = 6;
            // 
            // btnTabUser
            // 
            this.btnTabUser.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTabUser.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnTabUser.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTabUser.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnTabUser.ForeColor = System.Drawing.Color.White;
            this.btnTabUser.IconText = "U";
            this.btnTabUser.Label = "사용자";
            this.btnTabUser.Location = new System.Drawing.Point(1560, 5);
            this.btnTabUser.Name = "btnTabUser";
            this.btnTabUser.Selected = false;
            this.btnTabUser.Size = new System.Drawing.Size(110, 70);
            this.btnTabUser.TabIndex = 7;
            // 
            // btnTabExit
            // 
            this.btnTabExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTabExit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnTabExit.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTabExit.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnTabExit.ForeColor = System.Drawing.Color.White;
            this.btnTabExit.IconText = "X";
            this.btnTabExit.Label = "종료";
            this.btnTabExit.Location = new System.Drawing.Point(1680, 5);
            this.btnTabExit.Name = "btnTabExit";
            this.btnTabExit.Selected = false;
            this.btnTabExit.Size = new System.Drawing.Size(110, 70);
            this.btnTabExit.TabIndex = 8;
            // 
            // timerClock
            // 
            this.timerClock.Interval = 1000;
            this.timerClock.Tick += new System.EventHandler(this.TimerClock_Tick);
            // 
            // lblMenuLeft
            // 
            this.lblMenuLeft.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lblMenuLeft.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblMenuLeft.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblMenuLeft.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(144)))), ((int)(((byte)(144)))), ((int)(((byte)(144)))));
            this.lblMenuLeft.Location = new System.Drawing.Point(0, 100);
            this.lblMenuLeft.Name = "lblMenuLeft";
            this.lblMenuLeft.Size = new System.Drawing.Size(16, 900);
            this.lblMenuLeft.TabIndex = 1;
            this.lblMenuLeft.Text = "MENU";
            // 
            // lblMenuRight
            // 
            this.lblMenuRight.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMenuRight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblMenuRight.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblMenuRight.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(144)))), ((int)(((byte)(144)))), ((int)(((byte)(144)))));
            this.lblMenuRight.Location = new System.Drawing.Point(1904, 100);
            this.lblMenuRight.Name = "lblMenuRight";
            this.lblMenuRight.Size = new System.Drawing.Size(16, 900);
            this.lblMenuRight.TabIndex = 2;
            this.lblMenuRight.Text = "MENU";
            // 
            // Form1
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.ClientSize = new System.Drawing.Size(1920, 1080);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.lblMenuLeft);
            this.Controls.Add(this.lblMenuRight);
            this.Controls.Add(this.pnlStatusBar);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlBottomBar);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CDT-320";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.pnlHeader.ResumeLayout(false);
            this.pnlUserBox.ResumeLayout(false);
            this.pnlStatusBar.ResumeLayout(false);
            this.pnlContent.ResumeLayout(false);
            this.pnlBottomBar.ResumeLayout(false);
            this.ResumeLayout(false);

        }

    }
}
