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

            // (IC 직렬화 가능 — 헬퍼 호출 인라인. ck: Size(120,24) Text="기본값" Checked=true / lbl: Size(160,22) / num: Size(w,26) / btn: Size(w,32))
            // Exposure (y=44)
            this._ckExposure.Location = new Point(20, 44); this._ckExposure.Size = new Size(120, 24); this._ckExposure.Text = "기본값"; this._ckExposure.Checked = true; this._ckExposure.Font = UiTheme.ButtonFont;
            this._lblExposure.Location = new Point(150, 46); this._lblExposure.Size = new Size(160, 22); this._lblExposure.Text = "Exposure (μs)"; this._lblExposure.Font = UiTheme.ButtonFont;
            this._numExposure.Location = new Point(320, 42); this._numExposure.Size = new Size(150, 26); this._numExposure.Minimum = 1m; this._numExposure.Maximum = 1000000m; this._numExposure.DecimalPlaces = 0; this._numExposure.Increment = 100m; this._numExposure.Font = UiTheme.ValueFont;
            this._ckExposure.CheckedChanged += new System.EventHandler(this.OnCkExposure);

            // Gain (y=78)
            this._ckGain.Location = new Point(20, 78); this._ckGain.Size = new Size(120, 24); this._ckGain.Text = "기본값"; this._ckGain.Checked = true; this._ckGain.Font = UiTheme.ButtonFont;
            this._lblGain.Location = new Point(150, 80); this._lblGain.Size = new Size(160, 22); this._lblGain.Text = "Gain (dB)"; this._lblGain.Font = UiTheme.ButtonFont;
            this._numGain.Location = new Point(320, 76); this._numGain.Size = new Size(150, 26); this._numGain.Minimum = 0m; this._numGain.Maximum = 48m; this._numGain.DecimalPlaces = 1; this._numGain.Increment = 0.5m; this._numGain.Font = UiTheme.ValueFont;
            this._ckGain.CheckedChanged += new System.EventHandler(this.OnCkGain);

            // Fps (y=112)
            this._ckFps.Location = new Point(20, 112); this._ckFps.Size = new Size(120, 24); this._ckFps.Text = "기본값"; this._ckFps.Checked = true; this._ckFps.Font = UiTheme.ButtonFont;
            this._lblFps.Location = new Point(150, 114); this._lblFps.Size = new Size(160, 22); this._lblFps.Text = "Frame rate (fps)"; this._lblFps.Font = UiTheme.ButtonFont;
            this._numFps.Location = new Point(320, 110); this._numFps.Size = new Size(150, 26); this._numFps.Minimum = 1m; this._numFps.Maximum = 1000m; this._numFps.DecimalPlaces = 0; this._numFps.Increment = 1m; this._numFps.Font = UiTheme.ValueFont;
            this._ckFps.CheckedChanged += new System.EventHandler(this.OnCkFps);

            // Trigger (y=146)
            this._ckTrigger.Location = new Point(20, 146); this._ckTrigger.Size = new Size(120, 24); this._ckTrigger.Text = "기본값"; this._ckTrigger.Checked = true; this._ckTrigger.Font = UiTheme.ButtonFont;
            this._lblTrigger.Location = new Point(150, 148); this._lblTrigger.Size = new Size(160, 22); this._lblTrigger.Text = "Trigger mode"; this._lblTrigger.Font = UiTheme.ButtonFont;
            this._cbTrigger.Location = new Point(320, 144);
            this._cbTrigger.Size = new Size(150, 26);
            this._cbTrigger.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cbTrigger.Font = UiTheme.ValueFont;
            this._ckTrigger.CheckedChanged += new System.EventHandler(this.OnCkTrigger);

            // Pixel (y=180)
            this._ckPixel.Location = new Point(20, 180); this._ckPixel.Size = new Size(120, 24); this._ckPixel.Text = "기본값"; this._ckPixel.Checked = true; this._ckPixel.Font = UiTheme.ButtonFont;
            this._lblPixel.Location = new Point(150, 182); this._lblPixel.Size = new Size(160, 22); this._lblPixel.Text = "Pixel format"; this._lblPixel.Font = UiTheme.ButtonFont;
            this._cbPixel.Location = new Point(320, 178);
            this._cbPixel.Size = new Size(150, 26);
            this._cbPixel.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cbPixel.Font = UiTheme.ValueFont;
            this._ckPixel.CheckedChanged += new System.EventHandler(this.OnCkPixel);

            // Delay (y=214)
            this._ckDelay.Location = new Point(20, 214); this._ckDelay.Size = new Size(120, 24); this._ckDelay.Text = "기본값"; this._ckDelay.Checked = true; this._ckDelay.Font = UiTheme.ButtonFont;
            this._lblDelay.Location = new Point(150, 216); this._lblDelay.Size = new Size(160, 22); this._lblDelay.Text = "Delay before grab (ms)"; this._lblDelay.Font = UiTheme.ButtonFont;
            this._numDelay.Location = new Point(320, 212); this._numDelay.Size = new Size(150, 26); this._numDelay.Minimum = 0m; this._numDelay.Maximum = 60000m; this._numDelay.DecimalPlaces = 0; this._numDelay.Increment = 10m; this._numDelay.Font = UiTheme.ValueFont;
            this._ckDelay.CheckedChanged += new System.EventHandler(this.OnCkDelay);

            // ROI (y=248)
            this._ckRoi.Location = new Point(20, 248); this._ckRoi.Size = new Size(120, 24); this._ckRoi.Text = "기본값"; this._ckRoi.Checked = true; this._ckRoi.Font = UiTheme.ButtonFont;
            this._lblRoi.Location = new Point(150, 250); this._lblRoi.Size = new Size(160, 22); this._lblRoi.Text = "ROI (X/Y/W/H 묶음)"; this._lblRoi.Font = UiTheme.ButtonFont;
            this._numRoiX.Location = new Point(320, 246); this._numRoiX.Size = new Size(70, 26); this._numRoiX.Minimum = 0m; this._numRoiX.Maximum = 8000m; this._numRoiX.DecimalPlaces = 0; this._numRoiX.Increment = 1m; this._numRoiX.Font = UiTheme.ValueFont;
            this._numRoiY.Location = new Point(398, 246); this._numRoiY.Size = new Size(70, 26); this._numRoiY.Minimum = 0m; this._numRoiY.Maximum = 8000m; this._numRoiY.DecimalPlaces = 0; this._numRoiY.Increment = 1m; this._numRoiY.Font = UiTheme.ValueFont;
            this._numRoiW.Location = new Point(476, 246); this._numRoiW.Size = new Size(70, 26); this._numRoiW.Minimum = 0m; this._numRoiW.Maximum = 8000m; this._numRoiW.DecimalPlaces = 0; this._numRoiW.Increment = 1m; this._numRoiW.Font = UiTheme.ValueFont;
            this._numRoiH.Location = new Point(554, 246); this._numRoiH.Size = new Size(70, 26); this._numRoiH.Minimum = 0m; this._numRoiH.Maximum = 8000m; this._numRoiH.DecimalPlaces = 0; this._numRoiH.Increment = 1m; this._numRoiH.Font = UiTheme.ValueFont;
            this._ckRoi.CheckedChanged += new System.EventHandler(this.OnCkRoi);

            // 버튼 (y=294)
            this._btnSave.Location = new Point(20, 294); this._btnSave.Size = new Size(110, 32); this._btnSave.Text = "저장"; this._btnSave.FlatStyle = FlatStyle.Flat; this._btnSave.Font = UiTheme.ButtonFont; this._btnSave.BackColor = UiTheme.Accent; this._btnSave.ForeColor = Color.White;
            this._btnSave.Click += new System.EventHandler(this.OnSaveClick);
            this._btnReset.Location = new Point(140, 294); this._btnReset.Size = new Size(120, 32); this._btnReset.Text = "기본값 복원"; this._btnReset.FlatStyle = FlatStyle.Flat; this._btnReset.Font = UiTheme.ButtonFont; this._btnReset.BackColor = Color.White; this._btnReset.ForeColor = Color.Black;
            this._btnReset.Click += new System.EventHandler(this.OnResetClick);
            this._btnCancel.Location = new Point(270, 294); this._btnCancel.Size = new Size(90, 32); this._btnCancel.Text = "취소"; this._btnCancel.FlatStyle = FlatStyle.Flat; this._btnCancel.Font = UiTheme.ButtonFont; this._btnCancel.BackColor = Color.White; this._btnCancel.ForeColor = Color.Black;
            this._btnCancel.Click += new System.EventHandler(this.OnCancelClick);
            this._btnTest.Location = new Point(370, 294); this._btnTest.Size = new Size(120, 32); this._btnTest.Text = "테스트 그랩"; this._btnTest.FlatStyle = FlatStyle.Flat; this._btnTest.Font = UiTheme.ButtonFont; this._btnTest.BackColor = Color.White; this._btnTest.ForeColor = Color.Black;
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
    }
}
