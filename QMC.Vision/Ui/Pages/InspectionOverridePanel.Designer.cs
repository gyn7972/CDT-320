using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class InspectionOverridePanel
    {
        private System.ComponentModel.IContainer components = null;

        private Label _lblHeader;
        private Panel _body;
        private Label _lblCamera;
        private Label _lblToggleHdr;
        private CheckBox _ckExposure, _ckGain, _ckFps, _ckTrigger, _ckPixel, _ckDelay, _ckRoi;
        private Label _lblExposure, _lblGain, _lblFps, _lblTrigger, _lblPixel, _lblDelay, _lblRoi;
        private NumericUpDown _numExposure, _numGain, _numFps, _numDelay;
        private ComboBox _cbTrigger, _cbPixel;
        private NumericUpDown _numRoiX, _numRoiY, _numRoiW, _numRoiH;
        private Button _btnSave, _btnReset, _btnCancel, _btnTest;
        private Label _lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._lblHeader = new Label();
            this._body = new Panel();
            this._lblCamera = new Label();
            this._lblToggleHdr = new Label();
            this._ckExposure = new CheckBox(); this._lblExposure = new Label(); this._numExposure = new NumericUpDown();
            this._ckGain = new CheckBox(); this._lblGain = new Label(); this._numGain = new NumericUpDown();
            this._ckFps = new CheckBox(); this._lblFps = new Label(); this._numFps = new NumericUpDown();
            this._ckTrigger = new CheckBox(); this._lblTrigger = new Label(); this._cbTrigger = new ComboBox();
            this._ckPixel = new CheckBox(); this._lblPixel = new Label(); this._cbPixel = new ComboBox();
            this._ckDelay = new CheckBox(); this._lblDelay = new Label(); this._numDelay = new NumericUpDown();
            this._ckRoi = new CheckBox(); this._lblRoi = new Label();
            this._numRoiX = new NumericUpDown(); this._numRoiY = new NumericUpDown(); this._numRoiW = new NumericUpDown(); this._numRoiH = new NumericUpDown();
            this._btnSave = new Button(); this._btnReset = new Button(); this._btnCancel = new Button(); this._btnTest = new Button();
            this._lblStatus = new Label();
            this._body.SuspendLayout();
            ((ISupportInitialize)this._numExposure).BeginInit();
            ((ISupportInitialize)this._numGain).BeginInit();
            ((ISupportInitialize)this._numFps).BeginInit();
            ((ISupportInitialize)this._numDelay).BeginInit();
            ((ISupportInitialize)this._numRoiX).BeginInit();
            ((ISupportInitialize)this._numRoiY).BeginInit();
            ((ISupportInitialize)this._numRoiW).BeginInit();
            ((ISupportInitialize)this._numRoiH).BeginInit();
            this.SuspendLayout();

            this.BackColor = UiTheme.MainBg;

            // _lblHeader
            this._lblHeader.Dock = DockStyle.Top;
            this._lblHeader.Height = 30;
            this._lblHeader.Text = "검사 카메라 오버라이드";
            this._lblHeader.BackColor = UiTheme.StatusBarBg;
            this._lblHeader.ForeColor = Color.White;
            this._lblHeader.Font = UiTheme.SectionFont;
            this._lblHeader.TextAlign = ContentAlignment.MiddleLeft;
            this._lblHeader.Padding = new Padding(10, 0, 0, 0);

            // _body
            this._body.Dock = DockStyle.Fill;
            this._body.BackColor = UiTheme.MainBg;
            this._body.AutoScroll = true;
            this._body.Padding = new Padding(10);

            // _lblCamera (y=10)
            this._lblCamera.Location = new Point(20, 10);
            this._lblCamera.Size = new Size(560, 22);
            this._lblCamera.Font = UiTheme.ValueFont;
            this._lblCamera.ForeColor = Color.DarkSlateGray;
            this._lblCamera.Text = "카메라: -";

            // _lblToggleHdr (y=20)
            this._lblToggleHdr.Location = new Point(20, 20);
            this._lblToggleHdr.Size = new Size(560, 20);
            this._lblToggleHdr.Text = "─ 필드별 토글 (체크 = 알고리즘 기본값 상속) ─";
            this._lblToggleHdr.Font = UiTheme.ButtonFont;
            this._lblToggleHdr.ForeColor = Color.Gray;

            // Exposure (y=44)
            InitCheck(this._ckExposure, 20, 44);
            InitLbl(this._lblExposure, "Exposure (μs)", 150, 46);
            InitNum(this._numExposure, 320, 42, 150, 1m, 1000000m, 0, 100m);
            this._ckExposure.CheckedChanged += new System.EventHandler(this.OnCkExposure);

            // Gain (y=78)
            InitCheck(this._ckGain, 20, 78);
            InitLbl(this._lblGain, "Gain (dB)", 150, 80);
            InitNum(this._numGain, 320, 76, 150, 0m, 48m, 1, 0.5m);
            this._ckGain.CheckedChanged += new System.EventHandler(this.OnCkGain);

            // Fps (y=112)
            InitCheck(this._ckFps, 20, 112);
            InitLbl(this._lblFps, "Frame rate (fps)", 150, 114);
            InitNum(this._numFps, 320, 110, 150, 1m, 1000m, 0, 1m);
            this._ckFps.CheckedChanged += new System.EventHandler(this.OnCkFps);

            // Trigger (y=146)
            InitCheck(this._ckTrigger, 20, 146);
            InitLbl(this._lblTrigger, "Trigger mode", 150, 148);
            this._cbTrigger.Location = new Point(320, 144);
            this._cbTrigger.Size = new Size(150, 26);
            this._cbTrigger.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cbTrigger.Font = UiTheme.ValueFont;
            this._ckTrigger.CheckedChanged += new System.EventHandler(this.OnCkTrigger);

            // Pixel (y=180)
            InitCheck(this._ckPixel, 20, 180);
            InitLbl(this._lblPixel, "Pixel format", 150, 182);
            this._cbPixel.Location = new Point(320, 178);
            this._cbPixel.Size = new Size(150, 26);
            this._cbPixel.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cbPixel.Font = UiTheme.ValueFont;
            this._ckPixel.CheckedChanged += new System.EventHandler(this.OnCkPixel);

            // Delay (y=214)
            InitCheck(this._ckDelay, 20, 214);
            InitLbl(this._lblDelay, "Delay before grab (ms)", 150, 216);
            InitNum(this._numDelay, 320, 212, 150, 0m, 60000m, 0, 10m);
            this._ckDelay.CheckedChanged += new System.EventHandler(this.OnCkDelay);

            // ROI (y=248)
            InitCheck(this._ckRoi, 20, 248);
            InitLbl(this._lblRoi, "ROI (X/Y/W/H 묶음)", 150, 250);
            InitNum(this._numRoiX, 320, 246, 70, 0m, 8000m, 0, 1m);
            InitNum(this._numRoiY, 398, 246, 70, 0m, 8000m, 0, 1m);
            InitNum(this._numRoiW, 476, 246, 70, 0m, 8000m, 0, 1m);
            InitNum(this._numRoiH, 554, 246, 70, 0m, 8000m, 0, 1m);
            this._ckRoi.CheckedChanged += new System.EventHandler(this.OnCkRoi);

            // 버튼 (y=294)
            InitBtn(this._btnSave, "저장", 20, 294, 110, UiTheme.Accent, Color.White);
            this._btnSave.Click += new System.EventHandler(this.OnSaveClick);
            InitBtn(this._btnReset, "기본값 복원", 140, 294, 120, Color.White, Color.Black);
            this._btnReset.Click += new System.EventHandler(this.OnResetClick);
            InitBtn(this._btnCancel, "취소", 270, 294, 90, Color.White, Color.Black);
            this._btnCancel.Click += new System.EventHandler(this.OnCancelClick);
            InitBtn(this._btnTest, "테스트 그랩", 370, 294, 120, Color.White, Color.Black);
            this._btnTest.Click += new System.EventHandler(this.OnTestClick);

            // _lblStatus (y=338)
            this._lblStatus.Location = new Point(20, 338);
            this._lblStatus.Size = new Size(640, 24);
            this._lblStatus.Text = "";
            this._lblStatus.Font = UiTheme.ValueFont;
            this._lblStatus.ForeColor = Color.DarkSlateGray;

            this._body.Controls.Add(this._lblCamera);
            this._body.Controls.Add(this._lblToggleHdr);
            this._body.Controls.Add(this._ckExposure); this._body.Controls.Add(this._lblExposure); this._body.Controls.Add(this._numExposure);
            this._body.Controls.Add(this._ckGain); this._body.Controls.Add(this._lblGain); this._body.Controls.Add(this._numGain);
            this._body.Controls.Add(this._ckFps); this._body.Controls.Add(this._lblFps); this._body.Controls.Add(this._numFps);
            this._body.Controls.Add(this._ckTrigger); this._body.Controls.Add(this._lblTrigger); this._body.Controls.Add(this._cbTrigger);
            this._body.Controls.Add(this._ckPixel); this._body.Controls.Add(this._lblPixel); this._body.Controls.Add(this._cbPixel);
            this._body.Controls.Add(this._ckDelay); this._body.Controls.Add(this._lblDelay); this._body.Controls.Add(this._numDelay);
            this._body.Controls.Add(this._ckRoi); this._body.Controls.Add(this._lblRoi);
            this._body.Controls.Add(this._numRoiX); this._body.Controls.Add(this._numRoiY); this._body.Controls.Add(this._numRoiW); this._body.Controls.Add(this._numRoiH);
            this._body.Controls.Add(this._btnSave); this._body.Controls.Add(this._btnReset); this._body.Controls.Add(this._btnCancel); this._body.Controls.Add(this._btnTest);
            this._body.Controls.Add(this._lblStatus);

            // InspectionOverridePanel (원본 추가순서: 헤더→body)
            this.Controls.Add(this._lblHeader);
            this.Controls.Add(this._body);
            this.Name = "InspectionOverridePanel";
            ((ISupportInitialize)this._numExposure).EndInit();
            ((ISupportInitialize)this._numGain).EndInit();
            ((ISupportInitialize)this._numFps).EndInit();
            ((ISupportInitialize)this._numDelay).EndInit();
            ((ISupportInitialize)this._numRoiX).EndInit();
            ((ISupportInitialize)this._numRoiY).EndInit();
            ((ISupportInitialize)this._numRoiW).EndInit();
            ((ISupportInitialize)this._numRoiH).EndInit();
            this._body.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        // 디자이너 내부 초기화 헬퍼 (정적 속성만; 동작 로직 아님)
        private void InitCheck(CheckBox c, int x, int y)
        { c.Location = new Point(x, y); c.Size = new Size(120, 24); c.Text = "기본값"; c.Checked = true; c.Font = UiTheme.ButtonFont; }
        private void InitLbl(Label l, string text, int x, int y)
        { l.Location = new Point(x, y); l.Size = new Size(160, 22); l.Text = text; l.Font = UiTheme.ButtonFont; }
        private void InitNum(NumericUpDown n, int x, int y, int w, decimal min, decimal max, int dp, decimal inc)
        { n.Location = new Point(x, y); n.Size = new Size(w, 26); n.Minimum = min; n.Maximum = max; n.DecimalPlaces = dp; n.Increment = inc; n.Font = UiTheme.ValueFont; }
        private void InitBtn(Button b, string text, int x, int y, int w, Color bg, Color fg)
        { b.Location = new Point(x, y); b.Size = new Size(w, 32); b.Text = text; b.FlatStyle = FlatStyle.Flat; b.Font = UiTheme.ButtonFont; b.BackColor = bg; b.ForeColor = fg; }
    }
}
