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
        // 헤더 사용자/시간 박스 (Handler pnlUserBox 동일 구조 — 아바타 + 사용자 + 시간)
        private Panel pnlUserBox;
        private Label lblAvatar;
        private Label lblUserCaption;
        private Label lblUserValue;
        private Label lblTimeCaption;
        private Label lblTimeValue;      // 실제 시간 표시 (timerClock 이 갱신)

        // 오렌지 상태 바 (핸들러 정렬: 좌측 단일행 텍스트 + 우측 연결 동그라미)
        private Panel pnlStatusBar;
        private Label lblStatusL;
        private IndicatorDot dotCamera;
        private Label lblCamera;
        private IndicatorDot dotLight;
        private Label lblLight;
        private IndicatorDot dotVision;
        private Label lblVision;

        // 바텀 바 (Handler 와 동일 스타일 — BottomMenuButton)
        // 핸들러 정렬: 작업 · 이력 · 레시피 (좌) / 설정 · 사용자 · 종료 (우)
        private Panel pnlBottomBar;
        private BottomMenuButton btnWork;
        private BottomMenuButton btnHistory;
        private BottomMenuButton btnRecipe;
        private BottomMenuButton btnSettings;
        private BottomMenuButton btnUser;
        private BottomMenuButton btnExit;

        // 콘텐츠
        private Panel pnlContent;
        private Timer timerClock;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        // 디자이너 호환: 컨트롤 생성/속성/배치/이벤트 연결만(람다·지역변수·로직 호출 없음).
        // 런타임 레이아웃(LayoutBottomBar)·이벤트 처리는 Form1.cs.
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
            this.lblStateBig = new System.Windows.Forms.Label();
            this.pnlStatusBar = new System.Windows.Forms.Panel();
            this.lblStatusL = new System.Windows.Forms.Label();
            this.lblCamera = new System.Windows.Forms.Label();
            this.lblLight = new System.Windows.Forms.Label();
            this.lblVision = new System.Windows.Forms.Label();
            this.pnlBottomBar = new System.Windows.Forms.Panel();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.timerClock = new System.Windows.Forms.Timer(this.components);
            this.btnRecipe = new QMC.Vision.Ui.Controls.BottomMenuButton();
            this.btnWork = new QMC.Vision.Ui.Controls.BottomMenuButton();
            this.btnHistory = new QMC.Vision.Ui.Controls.BottomMenuButton();
            this.btnSettings = new QMC.Vision.Ui.Controls.BottomMenuButton();
            this.btnUser = new QMC.Vision.Ui.Controls.BottomMenuButton();
            this.btnExit = new QMC.Vision.Ui.Controls.BottomMenuButton();
            this.dotCamera = new QMC.Vision.Ui.Controls.IndicatorDot();
            this.dotLight = new QMC.Vision.Ui.Controls.IndicatorDot();
            this.dotVision = new QMC.Vision.Ui.Controls.IndicatorDot();
            this.pnlHeader.SuspendLayout();
            this.pnlUserBox.SuspendLayout();
            this.pnlStatusBar.SuspendLayout();
            this.pnlBottomBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.pnlHeader.Controls.Add(this.lblLogo);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Controls.Add(this.lblVersion);
            this.pnlHeader.Controls.Add(this.pnlUserBox);
            this.pnlHeader.Controls.Add(this.lblStateBig);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(1920, 70);
            this.pnlHeader.TabIndex = 3;
            // 
            // lblLogo
            // 
            this.lblLogo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this.lblLogo.Font = new System.Drawing.Font("Segoe UI Black", 20F, System.Drawing.FontStyle.Bold);
            this.lblLogo.ForeColor = System.Drawing.Color.White;
            this.lblLogo.Location = new System.Drawing.Point(18, 12);
            this.lblLogo.Name = "lblLogo";
            this.lblLogo.Size = new System.Drawing.Size(46, 46);
            this.lblLogo.TabIndex = 0;
            this.lblLogo.Text = "V";
            this.lblLogo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI Light", 22F);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(76, 8);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(380, 34);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "CDT-320  VISION";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVersion
            // 
            this.lblVersion.BackColor = System.Drawing.Color.Transparent;
            this.lblVersion.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(184)))), ((int)(((byte)(184)))), ((int)(((byte)(188)))));
            this.lblVersion.Location = new System.Drawing.Point(80, 42);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(380, 18);
            this.lblVersion.TabIndex = 2;
            this.lblVersion.Text = "듀얼 픽커 다이본더 비전 PC · v0.2.0";
            this.lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlUserBox
            // 
            this.pnlUserBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlUserBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.pnlUserBox.Controls.Add(this.lblAvatar);
            this.pnlUserBox.Controls.Add(this.lblUserCaption);
            this.pnlUserBox.Controls.Add(this.lblUserValue);
            this.pnlUserBox.Controls.Add(this.lblTimeCaption);
            this.pnlUserBox.Controls.Add(this.lblTimeValue);
            this.pnlUserBox.Location = new System.Drawing.Point(1544, 10);
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
            this.lblUserCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
            this.lblUserValue.Text = "admin";
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
            this.lblTimeCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
            // lblStateBig
            // 
            this.lblStateBig.BackColor = System.Drawing.Color.Transparent;
            this.lblStateBig.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblStateBig.Font = new System.Drawing.Font("Segoe UI Light", 22F);
            this.lblStateBig.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this.lblStateBig.Location = new System.Drawing.Point(1660, 0);
            this.lblStateBig.Name = "lblStateBig";
            this.lblStateBig.Padding = new System.Windows.Forms.Padding(0, 0, 20, 0);
            this.lblStateBig.Size = new System.Drawing.Size(260, 70);
            this.lblStateBig.TabIndex = 4;
            this.lblStateBig.Text = "NONE";
            this.lblStateBig.Visible = false;
            this.lblStateBig.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // pnlStatusBar
            // 
            this.pnlStatusBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.pnlStatusBar.Controls.Add(this.lblStatusL);
            this.pnlStatusBar.Controls.Add(this.dotCamera);
            this.pnlStatusBar.Controls.Add(this.lblCamera);
            this.pnlStatusBar.Controls.Add(this.dotLight);
            this.pnlStatusBar.Controls.Add(this.lblLight);
            this.pnlStatusBar.Controls.Add(this.dotVision);
            this.pnlStatusBar.Controls.Add(this.lblVision);
            this.pnlStatusBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlStatusBar.Location = new System.Drawing.Point(0, 70);
            this.pnlStatusBar.Name = "pnlStatusBar";
            this.pnlStatusBar.Size = new System.Drawing.Size(1920, 28);
            this.pnlStatusBar.TabIndex = 2;
            // 
            // lblStatusL
            // 
            this.lblStatusL.BackColor = System.Drawing.Color.Transparent;
            this.lblStatusL.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblStatusL.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblStatusL.ForeColor = System.Drawing.Color.White;
            this.lblStatusL.Location = new System.Drawing.Point(0, 0);
            this.lblStatusL.Name = "lblStatusL";
            this.lblStatusL.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.lblStatusL.Size = new System.Drawing.Size(1400, 28);
            this.lblStatusL.TabIndex = 0;
            this.lblStatusL.Text = "Backend: Sim";
            this.lblStatusL.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCamera
            // 
            this.lblCamera.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCamera.BackColor = System.Drawing.Color.Transparent;
            this.lblCamera.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblCamera.ForeColor = System.Drawing.Color.White;
            this.lblCamera.Location = new System.Drawing.Point(1516, 3);
            this.lblCamera.Name = "lblCamera";
            this.lblCamera.Size = new System.Drawing.Size(72, 22);
            this.lblCamera.TabIndex = 2;
            this.lblCamera.Text = "CAMERA";
            this.lblCamera.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLight
            // 
            this.lblLight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLight.BackColor = System.Drawing.Color.Transparent;
            this.lblLight.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblLight.ForeColor = System.Drawing.Color.White;
            this.lblLight.Location = new System.Drawing.Point(1626, 3);
            this.lblLight.Name = "lblLight";
            this.lblLight.Size = new System.Drawing.Size(56, 22);
            this.lblLight.TabIndex = 4;
            this.lblLight.Text = "LIGHT";
            this.lblLight.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVision
            // 
            this.lblVision.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblVision.BackColor = System.Drawing.Color.Transparent;
            this.lblVision.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblVision.ForeColor = System.Drawing.Color.White;
            this.lblVision.Location = new System.Drawing.Point(1716, 3);
            this.lblVision.Name = "lblVision";
            this.lblVision.Size = new System.Drawing.Size(72, 22);
            this.lblVision.TabIndex = 6;
            this.lblVision.Text = "VISION";
            this.lblVision.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlBottomBar
            // 
            this.pnlBottomBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.pnlBottomBar.Controls.Add(this.btnRecipe);
            this.pnlBottomBar.Controls.Add(this.btnWork);
            this.pnlBottomBar.Controls.Add(this.btnHistory);
            this.pnlBottomBar.Controls.Add(this.btnSettings);
            this.pnlBottomBar.Controls.Add(this.btnUser);
            this.pnlBottomBar.Controls.Add(this.btnExit);
            this.pnlBottomBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottomBar.Location = new System.Drawing.Point(0, 1000);
            this.pnlBottomBar.Name = "pnlBottomBar";
            this.pnlBottomBar.Size = new System.Drawing.Size(1920, 80);
            this.pnlBottomBar.TabIndex = 1;
            this.pnlBottomBar.SizeChanged += new System.EventHandler(this.pnlBottomBar_SizeChanged);
            // 
            // pnlContent
            // 
            this.pnlContent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(0, 98);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Size = new System.Drawing.Size(1920, 902);
            this.pnlContent.TabIndex = 0;
            // 
            // timerClock
            // 
            this.timerClock.Interval = 1000;
            this.timerClock.Tick += new System.EventHandler(this.TimerClock_Tick);
            // 
            // btnRecipe
            // 
            this.btnRecipe.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnRecipe.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRecipe.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnRecipe.ForeColor = System.Drawing.Color.White;
            this.btnRecipe.IconText = "R";
            this.btnRecipe.Label = "레시피";
            this.btnRecipe.Location = new System.Drawing.Point(300, 5);
            this.btnRecipe.Name = "btnRecipe";
            this.btnRecipe.Selected = false;
            this.btnRecipe.Size = new System.Drawing.Size(110, 70);
            this.btnRecipe.TabIndex = 2;
            this.btnRecipe.Click += new System.EventHandler(this.btnRecipe_Click);
            // 
            // btnWork
            // 
            this.btnWork.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnWork.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnWork.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnWork.ForeColor = System.Drawing.Color.White;
            this.btnWork.IconText = "W";
            this.btnWork.Label = "작업";
            this.btnWork.Location = new System.Drawing.Point(60, 5);
            this.btnWork.Name = "btnWork";
            this.btnWork.Selected = false;
            this.btnWork.Size = new System.Drawing.Size(110, 70);
            this.btnWork.TabIndex = 0;
            this.btnWork.Click += new System.EventHandler(this.btnWork_Click);
            // 
            // btnHistory
            // 
            this.btnHistory.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnHistory.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnHistory.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnHistory.ForeColor = System.Drawing.Color.White;
            this.btnHistory.IconText = "H";
            this.btnHistory.Label = "이력";
            this.btnHistory.Location = new System.Drawing.Point(180, 5);
            this.btnHistory.Name = "btnHistory";
            this.btnHistory.Selected = false;
            this.btnHistory.Size = new System.Drawing.Size(110, 70);
            this.btnHistory.TabIndex = 1;
            this.btnHistory.Click += new System.EventHandler(this.btnHistory_Click);
            // 
            // btnSettings
            // 
            this.btnSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnSettings.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSettings.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnSettings.ForeColor = System.Drawing.Color.White;
            this.btnSettings.IconText = "S";
            this.btnSettings.Label = "설정";
            this.btnSettings.Location = new System.Drawing.Point(1550, 5);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Selected = false;
            this.btnSettings.Size = new System.Drawing.Size(110, 70);
            this.btnSettings.TabIndex = 3;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // btnUser
            // 
            this.btnUser.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnUser.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUser.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnUser.ForeColor = System.Drawing.Color.White;
            this.btnUser.IconText = "U";
            this.btnUser.Label = "사용자";
            this.btnUser.Location = new System.Drawing.Point(1670, 5);
            this.btnUser.Name = "btnUser";
            this.btnUser.Selected = false;
            this.btnUser.Size = new System.Drawing.Size(110, 70);
            this.btnUser.TabIndex = 4;
            this.btnUser.Click += new System.EventHandler(this.btnUser_Click);
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.btnExit.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExit.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnExit.ForeColor = System.Drawing.Color.White;
            this.btnExit.IconText = "X";
            this.btnExit.Label = "종료";
            this.btnExit.Location = new System.Drawing.Point(1790, 5);
            this.btnExit.Name = "btnExit";
            this.btnExit.Selected = false;
            this.btnExit.Size = new System.Drawing.Size(110, 70);
            this.btnExit.TabIndex = 5;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // dotCamera
            // 
            this.dotCamera.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.dotCamera.BackColor = System.Drawing.Color.Transparent;
            this.dotCamera.Location = new System.Drawing.Point(1500, 8);
            this.dotCamera.Name = "dotCamera";
            this.dotCamera.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotCamera.OnColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(188)))), ((int)(((byte)(212)))));
            this.dotCamera.Size = new System.Drawing.Size(12, 12);
            this.dotCamera.TabIndex = 1;
            // 
            // dotLight
            // 
            this.dotLight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.dotLight.BackColor = System.Drawing.Color.Transparent;
            this.dotLight.Location = new System.Drawing.Point(1610, 8);
            this.dotLight.Name = "dotLight";
            this.dotLight.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotLight.OnColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(188)))), ((int)(((byte)(212)))));
            this.dotLight.Size = new System.Drawing.Size(12, 12);
            this.dotLight.TabIndex = 3;
            // 
            // dotVision
            // 
            this.dotVision.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.dotVision.BackColor = System.Drawing.Color.Transparent;
            this.dotVision.Location = new System.Drawing.Point(1700, 8);
            this.dotVision.Name = "dotVision";
            this.dotVision.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotVision.OnColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(188)))), ((int)(((byte)(212)))));
            this.dotVision.Size = new System.Drawing.Size(12, 12);
            this.dotVision.TabIndex = 5;
            // 
            // Form1
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.ClientSize = new System.Drawing.Size(1920, 1080);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlBottomBar);
            this.Controls.Add(this.pnlStatusBar);
            this.Controls.Add(this.pnlHeader);
            this.DoubleBuffered = true;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CDT-320 VISION";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.pnlHeader.ResumeLayout(false);
            this.pnlUserBox.ResumeLayout(false);
            this.pnlStatusBar.ResumeLayout(false);
            this.pnlBottomBar.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
