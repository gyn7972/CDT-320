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
            BeginInitNums();
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
            InitLbl(this._lblCamId, "카메라 ID", 20, 30, 160);
            this._cbCameraId.Location = new Point(180, 28);
            this._cbCameraId.Size = new Size(360, 26);
            this._cbCameraId.Font = UiTheme.ValueFont;
            this._cbCameraId.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cbCameraId.SelectedIndexChanged += new System.EventHandler(this.OnAnyFieldChanged);
            InitBtn(this._btnDiscover, "카메라 검색", 550, 28, 120, 28, Color.White, Color.Black);
            this._btnDiscover.Click += new System.EventHandler(this.OnDiscoverClick);

            // Exposure (y=66)
            InitLbl(this._lblExp, "Exposure (μs)", 20, 66, 160);
            InitNum(this._numExposure, 180, 64, 150, 1m, 1000000m, 0, 100m);
            this._numExposure.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);

            // Gain (y=102)
            InitLbl(this._lblGain, "Gain (dB)", 20, 102, 160);
            InitNum(this._numGain, 180, 100, 150, 0m, 48m, 1, 0.5m);
            this._numGain.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);

            // Fps (y=138)
            InitLbl(this._lblFps, "Frame rate (fps)", 20, 138, 160);
            InitNum(this._numFps, 180, 136, 150, 1m, 1000m, 0, 1m);
            this._numFps.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);

            // Trigger (y=174)
            InitLbl(this._lblTrig, "Trigger mode", 20, 174, 160);
            this._cbTrigger.Location = new Point(180, 172);
            this._cbTrigger.Size = new Size(150, 26);
            this._cbTrigger.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cbTrigger.Font = UiTheme.ValueFont;
            this._cbTrigger.SelectedIndexChanged += new System.EventHandler(this.OnAnyFieldChanged);

            // Pixel (y=210)
            InitLbl(this._lblPix, "Pixel format", 20, 210, 160);
            this._cbPixel.Location = new Point(180, 208);
            this._cbPixel.Size = new Size(150, 26);
            this._cbPixel.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cbPixel.Font = UiTheme.ValueFont;
            this._cbPixel.SelectedIndexChanged += new System.EventHandler(this.OnAnyFieldChanged);

            // Delay (y=246)
            InitLbl(this._lblDelay, "Delay before grab (ms)", 20, 246, 160);
            InitNum(this._numDelay, 180, 244, 150, 0m, 60000m, 0, 10m);
            this._numDelay.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);

            // ROI (y=282)
            InitLbl(this._lblRoi, "ROI (0=full sensor)", 20, 282, 160);
            InitLbl(this._lblOffX, "OffsetX", 180, 284, 56);
            InitNum(this._numRoiX, 240, 280, 80, 0m, 8000m, 0, 1m);
            InitLbl(this._lblOffY, "OffsetY", 325, 284, 56);
            InitNum(this._numRoiY, 385, 280, 80, 0m, 8000m, 0, 1m);
            InitLbl(this._lblW, "W", 470, 284, 16);
            InitNum(this._numRoiW, 490, 280, 80, 0m, 8000m, 0, 1m);
            InitLbl(this._lblH, "H", 575, 284, 16);
            InitNum(this._numRoiH, 595, 280, 80, 0m, 8000m, 0, 1m);
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
            InitLbl(this._lblMil, "MIL DCF 경로", 20, 354, 160);
            this._lblMil.Visible = false;
            this._txtMilDcf.Location = new Point(180, 352);
            this._txtMilDcf.Size = new Size(360, 26);
            this._txtMilDcf.Font = UiTheme.ValueFont;
            this._txtMilDcf.Visible = false;
            this._txtMilDcf.TextChanged += new System.EventHandler(this.OnMilTextChanged);

            // 액션 버튼 (y=402)
            InitBtn(this._btnSave, "저장", 20, 402, 110, 32, UiTheme.Accent, Color.White);
            this._btnSave.Click += new System.EventHandler(this.OnSaveClick);
            InitBtn(this._btnCancel, "취소", 140, 402, 90, 32, Color.White, Color.Black);
            this._btnCancel.Click += new System.EventHandler(this.OnCancelClick);
            InitBtn(this._btnReset, "기본값 복원", 240, 402, 120, 32, Color.White, Color.Black);
            this._btnReset.Click += new System.EventHandler(this.OnResetClick);
            InitBtn(this._btnApply, "실행 모듈에 적용", 370, 402, 160, 32, Color.White, Color.Black);
            this._btnApply.Click += new System.EventHandler(this.OnApplyClick);
            InitBtn(this._btnTestGrab, "테스트 그랩", 540, 402, 120, 32, Color.White, Color.Black);
            this._btnTestGrab.Click += new System.EventHandler(this.OnTestGrabClick);

            // Connect / Live (y=442)
            InitBtn(this._btnConnect, "Connect", 20, 442, 110, 32, UiTheme.Accent, Color.White);
            this._btnConnect.Click += new System.EventHandler(this.OnConnectClick);
            InitBtn(this._btnLiveStart, "Live Start", 140, 442, 110, 32, Color.White, Color.Black);
            this._btnLiveStart.Click += new System.EventHandler(this.OnLiveStartClick);
            InitBtn(this._btnLiveStop, "Live Stop", 260, 442, 110, 32, Color.White, Color.Black);
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
            EndInitNums();
            ((ISupportInitialize)this._picPreview).EndInit();
            this._body.ResumeLayout(false);
            this._body.PerformLayout();
            this.ResumeLayout(false);
        }

        private void BeginInitNums()
        {
            ((ISupportInitialize)this._numExposure).BeginInit(); ((ISupportInitialize)this._numGain).BeginInit();
            ((ISupportInitialize)this._numFps).BeginInit(); ((ISupportInitialize)this._numDelay).BeginInit();
            ((ISupportInitialize)this._numRoiX).BeginInit(); ((ISupportInitialize)this._numRoiY).BeginInit();
            ((ISupportInitialize)this._numRoiW).BeginInit(); ((ISupportInitialize)this._numRoiH).BeginInit();
        }
        private void EndInitNums()
        {
            ((ISupportInitialize)this._numExposure).EndInit(); ((ISupportInitialize)this._numGain).EndInit();
            ((ISupportInitialize)this._numFps).EndInit(); ((ISupportInitialize)this._numDelay).EndInit();
            ((ISupportInitialize)this._numRoiX).EndInit(); ((ISupportInitialize)this._numRoiY).EndInit();
            ((ISupportInitialize)this._numRoiW).EndInit(); ((ISupportInitialize)this._numRoiH).EndInit();
        }

        private void InitLbl(Label l, string text, int x, int y, int w)
        { l.Location = new Point(x, y); l.Size = new Size(w, 22); l.Text = text; l.Font = UiTheme.ButtonFont; }
        private void InitNum(NumericUpDown n, int x, int y, int w, decimal min, decimal max, int dp, decimal inc)
        { n.Location = new Point(x, y); n.Size = new Size(w, 26); n.Minimum = min; n.Maximum = max; n.DecimalPlaces = dp; n.Increment = inc; n.Font = UiTheme.ValueFont; }
        private void InitBtn(Button b, string text, int x, int y, int w, int h, Color bg, Color fg)
        { b.Location = new Point(x, y); b.Size = new Size(w, h); b.Text = text; b.FlatStyle = FlatStyle.Flat; b.Font = UiTheme.ButtonFont; b.BackColor = bg; b.ForeColor = fg; }
    }
}
