using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class CameraMappingPanel
    {
        private System.ComponentModel.IContainer components = null;

        private Label    _lblAlgorithm;
        private Panel    _body;
        private Label    _lblCamId;
        private ComboBox _cbCameraId;
        private Button   _btnDiscover;
        private Label    _lblExp;    private NumericUpDown _numExposure;
        private Label    _lblGain;   private NumericUpDown _numGain;
        private Label    _lblFps;    private NumericUpDown _numFps;
        private Label    _lblTrig;   private ComboBox _cbTrigger;
        private Label    _lblPix;    private ComboBox _cbPixel;
        private Label    _lblDelay;  private NumericUpDown _numDelay;
        private Label    _lblRoi;
        private Label    _lblOffX;   private NumericUpDown _numRoiX;
        private Label    _lblOffY;   private NumericUpDown _numRoiY;
        private Label    _lblW;      private NumericUpDown _numRoiW;
        private Label    _lblH;      private NumericUpDown _numRoiH;
        private CheckBox _chkMilDcf;
        private Label    _lblMil;    private TextBox _txtMilDcf;
        private Button   _btnSave, _btnCancel, _btnReset, _btnApply, _btnTestGrab;
        private Button   _btnConnect, _btnLiveStart, _btnLiveStop;
        private Label    _lblStatus;
        private PictureBox _picPreview;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { Disconnect(); } catch { }   // 원본 Disposed 핸들러 대체 (카메라 정리)
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._lblAlgorithm = new Label();
            this._body = new Panel();
            this._lblCamId = new Label(); this._cbCameraId = new ComboBox(); this._btnDiscover = new Button();
            this._lblExp = new Label(); this._numExposure = new NumericUpDown();
            this._lblGain = new Label(); this._numGain = new NumericUpDown();
            this._lblFps = new Label(); this._numFps = new NumericUpDown();
            this._lblTrig = new Label(); this._cbTrigger = new ComboBox();
            this._lblPix = new Label(); this._cbPixel = new ComboBox();
            this._lblDelay = new Label(); this._numDelay = new NumericUpDown();
            this._lblRoi = new Label();
            this._lblOffX = new Label(); this._numRoiX = new NumericUpDown();
            this._lblOffY = new Label(); this._numRoiY = new NumericUpDown();
            this._lblW = new Label(); this._numRoiW = new NumericUpDown();
            this._lblH = new Label(); this._numRoiH = new NumericUpDown();
            this._chkMilDcf = new CheckBox();
            this._lblMil = new Label(); this._txtMilDcf = new TextBox();
            this._btnSave = new Button(); this._btnCancel = new Button(); this._btnReset = new Button();
            this._btnApply = new Button(); this._btnTestGrab = new Button();
            this._btnConnect = new Button(); this._btnLiveStart = new Button(); this._btnLiveStop = new Button();
            this._lblStatus = new Label();
            this._picPreview = new PictureBox();
            this._body.SuspendLayout();
            ((ISupportInitialize)this._numExposure).BeginInit(); ((ISupportInitialize)this._numGain).BeginInit();
            ((ISupportInitialize)this._numFps).BeginInit(); ((ISupportInitialize)this._numDelay).BeginInit();
            ((ISupportInitialize)this._numRoiX).BeginInit(); ((ISupportInitialize)this._numRoiY).BeginInit();
            ((ISupportInitialize)this._numRoiW).BeginInit(); ((ISupportInitialize)this._numRoiH).BeginInit();
            ((ISupportInitialize)this._picPreview).BeginInit();
            this.SuspendLayout();

            this.BackColor = UiTheme.MainBg;

            // _lblAlgorithm
            this._lblAlgorithm.Dock = DockStyle.Top;
            this._lblAlgorithm.Height = 30;
            this._lblAlgorithm.Text = "카메라 매핑";
            this._lblAlgorithm.BackColor = UiTheme.StatusBarBg;
            this._lblAlgorithm.ForeColor = Color.White;
            this._lblAlgorithm.Font = UiTheme.SectionFont;
            this._lblAlgorithm.TextAlign = ContentAlignment.MiddleLeft;
            this._lblAlgorithm.Padding = new Padding(10, 0, 0, 0);

            // _body
            this._body.Dock = DockStyle.Fill;
            this._body.BackColor = UiTheme.MainBg;
            this._body.AutoScroll = true;
            this._body.Padding = new Padding(10, 12, 10, 10);

            // 카메라 ID (y=30)
            this._lblCamId.Location = new Point(20, 30); this._lblCamId.Size = new Size(160, 22);
            this._lblCamId.Text = "카메라 ID"; this._lblCamId.Font = UiTheme.ButtonFont;
            this._cbCameraId.Location = new Point(180, 28);
            this._cbCameraId.Size = new Size(360, 26);
            this._cbCameraId.Font = UiTheme.ValueFont;
            this._cbCameraId.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cbCameraId.SelectedIndexChanged += new System.EventHandler(this.OnAnyFieldChanged);
            this._btnDiscover.Location = new Point(550, 28); this._btnDiscover.Size = new Size(120, 28);
            this._btnDiscover.Text = "카메라 검색"; this._btnDiscover.FlatStyle = FlatStyle.Flat; this._btnDiscover.Font = UiTheme.ButtonFont;
            this._btnDiscover.BackColor = Color.White; this._btnDiscover.ForeColor = Color.Black;
            this._btnDiscover.Click += new System.EventHandler(this.OnDiscoverClick);

            // Exposure (y=66)
            this._lblExp.Location = new Point(20, 66); this._lblExp.Size = new Size(160, 22);
            this._lblExp.Text = "Exposure (μs)"; this._lblExp.Font = UiTheme.ButtonFont;
            this._numExposure.Location = new Point(180, 64); this._numExposure.Size = new Size(150, 26);
            this._numExposure.Minimum = 1m; this._numExposure.Maximum = 1000000m; this._numExposure.DecimalPlaces = 0; this._numExposure.Increment = 100m; this._numExposure.Font = UiTheme.ValueFont;
            this._numExposure.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);

            // Gain (y=102)
            this._lblGain.Location = new Point(20, 102); this._lblGain.Size = new Size(160, 22);
            this._lblGain.Text = "Gain (dB)"; this._lblGain.Font = UiTheme.ButtonFont;
            this._numGain.Location = new Point(180, 100); this._numGain.Size = new Size(150, 26);
            this._numGain.Minimum = 0m; this._numGain.Maximum = 48m; this._numGain.DecimalPlaces = 1; this._numGain.Increment = 0.5m; this._numGain.Font = UiTheme.ValueFont;
            this._numGain.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);

            // Fps (y=138)
            this._lblFps.Location = new Point(20, 138); this._lblFps.Size = new Size(160, 22);
            this._lblFps.Text = "Frame rate (fps)"; this._lblFps.Font = UiTheme.ButtonFont;
            this._numFps.Location = new Point(180, 136); this._numFps.Size = new Size(150, 26);
            this._numFps.Minimum = 1m; this._numFps.Maximum = 1000m; this._numFps.DecimalPlaces = 0; this._numFps.Increment = 1m; this._numFps.Font = UiTheme.ValueFont;
            this._numFps.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);

            // Trigger (y=174)
            this._lblTrig.Location = new Point(20, 174); this._lblTrig.Size = new Size(160, 22);
            this._lblTrig.Text = "Trigger mode"; this._lblTrig.Font = UiTheme.ButtonFont;
            this._cbTrigger.Location = new Point(180, 172);
            this._cbTrigger.Size = new Size(150, 26);
            this._cbTrigger.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cbTrigger.Font = UiTheme.ValueFont;
            this._cbTrigger.SelectedIndexChanged += new System.EventHandler(this.OnAnyFieldChanged);

            // Pixel (y=210)
            this._lblPix.Location = new Point(20, 210); this._lblPix.Size = new Size(160, 22);
            this._lblPix.Text = "Pixel format"; this._lblPix.Font = UiTheme.ButtonFont;
            this._cbPixel.Location = new Point(180, 208);
            this._cbPixel.Size = new Size(150, 26);
            this._cbPixel.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cbPixel.Font = UiTheme.ValueFont;
            this._cbPixel.SelectedIndexChanged += new System.EventHandler(this.OnAnyFieldChanged);

            // Delay (y=246)
            this._lblDelay.Location = new Point(20, 246); this._lblDelay.Size = new Size(160, 22);
            this._lblDelay.Text = "Delay before grab (ms)"; this._lblDelay.Font = UiTheme.ButtonFont;
            this._numDelay.Location = new Point(180, 244); this._numDelay.Size = new Size(150, 26);
            this._numDelay.Minimum = 0m; this._numDelay.Maximum = 60000m; this._numDelay.DecimalPlaces = 0; this._numDelay.Increment = 10m; this._numDelay.Font = UiTheme.ValueFont;
            this._numDelay.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);

            // ROI (y=282)
            this._lblRoi.Location = new Point(20, 282); this._lblRoi.Size = new Size(160, 22);
            this._lblRoi.Text = "ROI (0=full sensor)"; this._lblRoi.Font = UiTheme.ButtonFont;
            this._lblOffX.Location = new Point(180, 284); this._lblOffX.Size = new Size(56, 22);
            this._lblOffX.Text = "OffsetX"; this._lblOffX.Font = UiTheme.ButtonFont;
            this._numRoiX.Location = new Point(240, 280); this._numRoiX.Size = new Size(80, 26);
            this._numRoiX.Minimum = 0m; this._numRoiX.Maximum = 8000m; this._numRoiX.DecimalPlaces = 0; this._numRoiX.Increment = 1m; this._numRoiX.Font = UiTheme.ValueFont;
            this._lblOffY.Location = new Point(325, 284); this._lblOffY.Size = new Size(56, 22);
            this._lblOffY.Text = "OffsetY"; this._lblOffY.Font = UiTheme.ButtonFont;
            this._numRoiY.Location = new Point(385, 280); this._numRoiY.Size = new Size(80, 26);
            this._numRoiY.Minimum = 0m; this._numRoiY.Maximum = 8000m; this._numRoiY.DecimalPlaces = 0; this._numRoiY.Increment = 1m; this._numRoiY.Font = UiTheme.ValueFont;
            this._lblW.Location = new Point(470, 284); this._lblW.Size = new Size(16, 22);
            this._lblW.Text = "W"; this._lblW.Font = UiTheme.ButtonFont;
            this._numRoiW.Location = new Point(490, 280); this._numRoiW.Size = new Size(80, 26);
            this._numRoiW.Minimum = 0m; this._numRoiW.Maximum = 8000m; this._numRoiW.DecimalPlaces = 0; this._numRoiW.Increment = 1m; this._numRoiW.Font = UiTheme.ValueFont;
            this._lblH.Location = new Point(575, 284); this._lblH.Size = new Size(16, 22);
            this._lblH.Text = "H"; this._lblH.Font = UiTheme.ButtonFont;
            this._numRoiH.Location = new Point(595, 280); this._numRoiH.Size = new Size(80, 26);
            this._numRoiH.Minimum = 0m; this._numRoiH.Maximum = 8000m; this._numRoiH.DecimalPlaces = 0; this._numRoiH.Increment = 1m; this._numRoiH.Font = UiTheme.ValueFont;
            this._numRoiX.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);
            this._numRoiY.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);
            this._numRoiW.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);
            this._numRoiH.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);

            // MIL DCF (체크 y=318, 경로 y=354) — 기본 숨김
            this._chkMilDcf.Location = new Point(20, 320);
            this._chkMilDcf.Size = new Size(220, 22);
            this._chkMilDcf.Text = "MIL DCF 직접 지정";
            this._chkMilDcf.Font = UiTheme.ButtonFont;
            this._chkMilDcf.Visible = false;
            this._chkMilDcf.CheckedChanged += new System.EventHandler(this.OnMilDcfCheckedChanged);
            this._lblMil.Location = new Point(20, 354); this._lblMil.Size = new Size(160, 22);
            this._lblMil.Text = "MIL DCF 경로"; this._lblMil.Font = UiTheme.ButtonFont;
            this._lblMil.Visible = false;
            this._txtMilDcf.Location = new Point(180, 352);
            this._txtMilDcf.Size = new Size(360, 26);
            this._txtMilDcf.Font = UiTheme.ValueFont;
            this._txtMilDcf.Visible = false;
            this._txtMilDcf.TextChanged += new System.EventHandler(this.OnMilTextChanged);

            // 액션 버튼 (y=402)
            this._btnSave.Location = new Point(20, 402); this._btnSave.Size = new Size(110, 32);
            this._btnSave.Text = "저장"; this._btnSave.FlatStyle = FlatStyle.Flat; this._btnSave.Font = UiTheme.ButtonFont; this._btnSave.BackColor = UiTheme.Accent; this._btnSave.ForeColor = Color.White;
            this._btnSave.Click += new System.EventHandler(this.OnSaveClick);
            this._btnCancel.Location = new Point(140, 402); this._btnCancel.Size = new Size(90, 32);
            this._btnCancel.Text = "취소"; this._btnCancel.FlatStyle = FlatStyle.Flat; this._btnCancel.Font = UiTheme.ButtonFont; this._btnCancel.BackColor = Color.White; this._btnCancel.ForeColor = Color.Black;
            this._btnCancel.Click += new System.EventHandler(this.OnCancelClick);
            this._btnReset.Location = new Point(240, 402); this._btnReset.Size = new Size(120, 32);
            this._btnReset.Text = "기본값 복원"; this._btnReset.FlatStyle = FlatStyle.Flat; this._btnReset.Font = UiTheme.ButtonFont; this._btnReset.BackColor = Color.White; this._btnReset.ForeColor = Color.Black;
            this._btnReset.Click += new System.EventHandler(this.OnResetClick);
            this._btnApply.Location = new Point(370, 402); this._btnApply.Size = new Size(160, 32);
            this._btnApply.Text = "실행 모듈에 적용"; this._btnApply.FlatStyle = FlatStyle.Flat; this._btnApply.Font = UiTheme.ButtonFont; this._btnApply.BackColor = Color.White; this._btnApply.ForeColor = Color.Black;
            this._btnApply.Click += new System.EventHandler(this.OnApplyClick);
            this._btnTestGrab.Location = new Point(540, 402); this._btnTestGrab.Size = new Size(120, 32);
            this._btnTestGrab.Text = "테스트 그랩"; this._btnTestGrab.FlatStyle = FlatStyle.Flat; this._btnTestGrab.Font = UiTheme.ButtonFont; this._btnTestGrab.BackColor = Color.White; this._btnTestGrab.ForeColor = Color.Black;
            this._btnTestGrab.Click += new System.EventHandler(this.OnTestGrabClick);

            // Connect / Live (y=442)
            this._btnConnect.Location = new Point(20, 442); this._btnConnect.Size = new Size(110, 32);
            this._btnConnect.Text = "Connect"; this._btnConnect.FlatStyle = FlatStyle.Flat; this._btnConnect.Font = UiTheme.ButtonFont; this._btnConnect.BackColor = UiTheme.Accent; this._btnConnect.ForeColor = Color.White;
            this._btnConnect.Click += new System.EventHandler(this.OnConnectClick);
            this._btnLiveStart.Location = new Point(140, 442); this._btnLiveStart.Size = new Size(110, 32);
            this._btnLiveStart.Text = "Live Start"; this._btnLiveStart.FlatStyle = FlatStyle.Flat; this._btnLiveStart.Font = UiTheme.ButtonFont; this._btnLiveStart.BackColor = Color.White; this._btnLiveStart.ForeColor = Color.Black;
            this._btnLiveStart.Click += new System.EventHandler(this.OnLiveStartClick);
            this._btnLiveStop.Location = new Point(260, 442); this._btnLiveStop.Size = new Size(110, 32);
            this._btnLiveStop.Text = "Live Stop"; this._btnLiveStop.FlatStyle = FlatStyle.Flat; this._btnLiveStop.Font = UiTheme.ButtonFont; this._btnLiveStop.BackColor = Color.White; this._btnLiveStop.ForeColor = Color.Black;
            this._btnLiveStop.Click += new System.EventHandler(this.OnLiveStopClick);

            // _lblStatus (y=482), _picPreview (y=512)
            this._lblStatus.Location = new Point(20, 482);
            this._lblStatus.Size = new Size(900, 24);
            this._lblStatus.Font = UiTheme.ValueFont;
            this._lblStatus.ForeColor = Color.DarkSlateGray;
            this._picPreview.Location = new Point(20, 512);
            this._picPreview.Size = new Size(640, 360);
            this._picPreview.BorderStyle = BorderStyle.FixedSingle;
            this._picPreview.BackColor = Color.Black;
            this._picPreview.SizeMode = PictureBoxSizeMode.Zoom;

            // body 에 추가 (원본 순서)
            this._body.Controls.Add(this._lblCamId); this._body.Controls.Add(this._cbCameraId); this._body.Controls.Add(this._btnDiscover);
            this._body.Controls.Add(this._lblExp); this._body.Controls.Add(this._numExposure);
            this._body.Controls.Add(this._lblGain); this._body.Controls.Add(this._numGain);
            this._body.Controls.Add(this._lblFps); this._body.Controls.Add(this._numFps);
            this._body.Controls.Add(this._lblTrig); this._body.Controls.Add(this._cbTrigger);
            this._body.Controls.Add(this._lblPix); this._body.Controls.Add(this._cbPixel);
            this._body.Controls.Add(this._lblDelay); this._body.Controls.Add(this._numDelay);
            this._body.Controls.Add(this._lblRoi);
            this._body.Controls.Add(this._lblOffX); this._body.Controls.Add(this._numRoiX);
            this._body.Controls.Add(this._lblOffY); this._body.Controls.Add(this._numRoiY);
            this._body.Controls.Add(this._lblW); this._body.Controls.Add(this._numRoiW);
            this._body.Controls.Add(this._lblH); this._body.Controls.Add(this._numRoiH);
            this._body.Controls.Add(this._chkMilDcf);
            this._body.Controls.Add(this._lblMil); this._body.Controls.Add(this._txtMilDcf);
            this._body.Controls.Add(this._btnSave); this._body.Controls.Add(this._btnCancel); this._body.Controls.Add(this._btnReset);
            this._body.Controls.Add(this._btnApply); this._body.Controls.Add(this._btnTestGrab);
            this._body.Controls.Add(this._btnConnect); this._body.Controls.Add(this._btnLiveStart); this._body.Controls.Add(this._btnLiveStop);
            this._body.Controls.Add(this._lblStatus);
            this._body.Controls.Add(this._picPreview);

            // CameraMappingPanel (원본 추가순서: 헤더→body)
            this.Controls.Add(this._lblAlgorithm);
            this.Controls.Add(this._body);
            this.Name = "CameraMappingPanel";
            ((ISupportInitialize)this._numExposure).EndInit(); ((ISupportInitialize)this._numGain).EndInit();
            ((ISupportInitialize)this._numFps).EndInit(); ((ISupportInitialize)this._numDelay).EndInit();
            ((ISupportInitialize)this._numRoiX).EndInit(); ((ISupportInitialize)this._numRoiY).EndInit();
            ((ISupportInitialize)this._numRoiW).EndInit(); ((ISupportInitialize)this._numRoiH).EndInit();
            ((ISupportInitialize)this._picPreview).EndInit();
            this._body.ResumeLayout(false);
            this._body.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
