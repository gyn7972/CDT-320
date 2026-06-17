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
        private Label    _lblTrig;   private ComboBox _cbTrigger;       // Trigger Source (Software/Line0..)
        private Label    _lblTrigMode; private ComboBox _cbTriggerMode; // Trigger Mode (On/Off)
        private Label    _lblPix;    private ComboBox _cbPixel;
        private Label    _lblDelay;  private NumericUpDown _numDelay;
        private Label    _lblRoi;
        private Label    _lblOffX;   private NumericUpDown _numRoiX;
        private Label    _lblOffY;   private NumericUpDown _numRoiY;
        private Label    _lblW;      private NumericUpDown _numRoiW;
        private Label    _lblH;      private NumericUpDown _numRoiH;
        private CheckBox _chkMilDcf;
        private Label    _lblMil;    private TextBox _txtMilDcf;   private Button _btnMilBrowse;
        // 조명 컨트롤러/페이지 지정(모듈 Setup.LightPages) — 좌하단 섹션
        private Label    _lblLightAssign;
        private DataGridView _gridLightAssign;
        private DataGridViewComboBoxColumn _colLightCtrl;
        private DataGridViewComboBoxColumn _colLightPage;
        private Label    _lblLightStatus;
        // 우측 컬럼 — docking 스택: 버튼 고정 2행(기능별 그룹) + 상태(Top) + 미리보기(Fill)
        private Panel    _rightPanel;
        private FlowLayoutPanel _flowRow1;   // 저장·취소·기본값복원·실행모듈적용 (편집/저장 그룹)
        private FlowLayoutPanel _flowRow2;   // Connect·테스트그랩·LiveStart·LiveStop (카메라 그룹)
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
            this._lblAlgorithm = new System.Windows.Forms.Label();
            this._body = new System.Windows.Forms.Panel();
            this._lblCamId = new System.Windows.Forms.Label();
            this._cbCameraId = new System.Windows.Forms.ComboBox();
            this._btnDiscover = new System.Windows.Forms.Button();
            this._lblExp = new System.Windows.Forms.Label();
            this._numExposure = new System.Windows.Forms.NumericUpDown();
            this._lblGain = new System.Windows.Forms.Label();
            this._numGain = new System.Windows.Forms.NumericUpDown();
            this._lblFps = new System.Windows.Forms.Label();
            this._numFps = new System.Windows.Forms.NumericUpDown();
            this._lblTrig = new System.Windows.Forms.Label();
            this._cbTrigger = new System.Windows.Forms.ComboBox();
            this._lblTrigMode = new System.Windows.Forms.Label();
            this._cbTriggerMode = new System.Windows.Forms.ComboBox();
            this._lblPix = new System.Windows.Forms.Label();
            this._cbPixel = new System.Windows.Forms.ComboBox();
            this._lblDelay = new System.Windows.Forms.Label();
            this._numDelay = new System.Windows.Forms.NumericUpDown();
            this._lblRoi = new System.Windows.Forms.Label();
            this._lblOffX = new System.Windows.Forms.Label();
            this._numRoiX = new System.Windows.Forms.NumericUpDown();
            this._lblOffY = new System.Windows.Forms.Label();
            this._numRoiY = new System.Windows.Forms.NumericUpDown();
            this._lblW = new System.Windows.Forms.Label();
            this._numRoiW = new System.Windows.Forms.NumericUpDown();
            this._lblH = new System.Windows.Forms.Label();
            this._numRoiH = new System.Windows.Forms.NumericUpDown();
            this._chkMilDcf = new System.Windows.Forms.CheckBox();
            this._lblMil = new System.Windows.Forms.Label();
            this._txtMilDcf = new System.Windows.Forms.TextBox();
            this._btnMilBrowse = new System.Windows.Forms.Button();
            this._lblLightAssign = new System.Windows.Forms.Label();
            this._gridLightAssign = new System.Windows.Forms.DataGridView();
            this._colLightCtrl = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this._colLightPage = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this._lblLightStatus = new System.Windows.Forms.Label();
            this._rightPanel = new System.Windows.Forms.Panel();
            this._flowRow1 = new System.Windows.Forms.FlowLayoutPanel();
            this._flowRow2 = new System.Windows.Forms.FlowLayoutPanel();
            this._btnSave = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._btnReset = new System.Windows.Forms.Button();
            this._btnApply = new System.Windows.Forms.Button();
            this._btnTestGrab = new System.Windows.Forms.Button();
            this._btnConnect = new System.Windows.Forms.Button();
            this._btnLiveStart = new System.Windows.Forms.Button();
            this._btnLiveStop = new System.Windows.Forms.Button();
            this._lblStatus = new System.Windows.Forms.Label();
            this._picPreview = new System.Windows.Forms.PictureBox();
            this._body.SuspendLayout();
            this._rightPanel.SuspendLayout();
            this._flowRow1.SuspendLayout();
            this._flowRow2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._numExposure)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numGain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numFps)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numRoiX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numRoiY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numRoiW)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numRoiH)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._gridLightAssign)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._picPreview)).BeginInit();
            this.SuspendLayout();
            //
            // _lblAlgorithm
            //
            this._lblAlgorithm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._lblAlgorithm.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblAlgorithm.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._lblAlgorithm.ForeColor = System.Drawing.Color.White;
            this._lblAlgorithm.Location = new System.Drawing.Point(0, 0);
            this._lblAlgorithm.Name = "_lblAlgorithm";
            this._lblAlgorithm.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._lblAlgorithm.Size = new System.Drawing.Size(1140, 30);
            this._lblAlgorithm.TabIndex = 0;
            this._lblAlgorithm.Text = "카메라 매핑";
            this._lblAlgorithm.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _body
            //
            this._body.AutoScroll = true;
            this._body.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._body.Controls.Add(this._lblCamId);
            this._body.Controls.Add(this._cbCameraId);
            this._body.Controls.Add(this._btnDiscover);
            this._body.Controls.Add(this._lblExp);
            this._body.Controls.Add(this._numExposure);
            this._body.Controls.Add(this._lblGain);
            this._body.Controls.Add(this._numGain);
            this._body.Controls.Add(this._lblFps);
            this._body.Controls.Add(this._numFps);
            this._body.Controls.Add(this._lblTrig);
            this._body.Controls.Add(this._cbTrigger);
            this._body.Controls.Add(this._lblTrigMode);
            this._body.Controls.Add(this._cbTriggerMode);
            this._body.Controls.Add(this._lblPix);
            this._body.Controls.Add(this._cbPixel);
            this._body.Controls.Add(this._lblDelay);
            this._body.Controls.Add(this._numDelay);
            this._body.Controls.Add(this._lblRoi);
            this._body.Controls.Add(this._lblOffX);
            this._body.Controls.Add(this._numRoiX);
            this._body.Controls.Add(this._lblOffY);
            this._body.Controls.Add(this._numRoiY);
            this._body.Controls.Add(this._lblW);
            this._body.Controls.Add(this._numRoiW);
            this._body.Controls.Add(this._lblH);
            this._body.Controls.Add(this._numRoiH);
            this._body.Controls.Add(this._chkMilDcf);
            this._body.Controls.Add(this._lblMil);
            this._body.Controls.Add(this._txtMilDcf);
            this._body.Controls.Add(this._btnMilBrowse);
            this._body.Controls.Add(this._lblLightAssign);
            this._body.Controls.Add(this._gridLightAssign);
            this._body.Controls.Add(this._lblLightStatus);
            this._body.Controls.Add(this._rightPanel);
            this._body.Dock = System.Windows.Forms.DockStyle.Fill;
            this._body.Location = new System.Drawing.Point(0, 0);
            this._body.Name = "_body";
            this._body.Padding = new System.Windows.Forms.Padding(10, 12, 10, 10);
            this._body.Size = new System.Drawing.Size(1140, 595);
            this._body.TabIndex = 1;
            //
            // _lblCamId
            //
            this._lblCamId.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblCamId.Location = new System.Drawing.Point(20, 30);
            this._lblCamId.Name = "_lblCamId";
            this._lblCamId.Size = new System.Drawing.Size(160, 22);
            this._lblCamId.TabIndex = 0;
            this._lblCamId.Text = "카메라 ID";
            //
            // _cbCameraId
            //
            this._cbCameraId.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbCameraId.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbCameraId.Location = new System.Drawing.Point(180, 28);
            this._cbCameraId.Name = "_cbCameraId";
            this._cbCameraId.Size = new System.Drawing.Size(360, 23);
            this._cbCameraId.TabIndex = 1;
            this._cbCameraId.SelectedIndexChanged += new System.EventHandler(this.OnAnyFieldChanged);
            //
            // _btnDiscover
            //
            this._btnDiscover.BackColor = System.Drawing.Color.White;
            this._btnDiscover.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnDiscover.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnDiscover.ForeColor = System.Drawing.Color.Black;
            this._btnDiscover.Location = new System.Drawing.Point(550, 28);
            this._btnDiscover.Name = "_btnDiscover";
            this._btnDiscover.Size = new System.Drawing.Size(120, 28);
            this._btnDiscover.TabIndex = 2;
            this._btnDiscover.Text = "카메라 검색";
            this._btnDiscover.UseVisualStyleBackColor = false;
            this._btnDiscover.Click += new System.EventHandler(this.OnDiscoverClick);
            //
            // _lblExp
            //
            this._lblExp.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblExp.Location = new System.Drawing.Point(20, 66);
            this._lblExp.Name = "_lblExp";
            this._lblExp.Size = new System.Drawing.Size(160, 22);
            this._lblExp.TabIndex = 3;
            this._lblExp.Text = "Exposure (μs)";
            //
            // _numExposure
            //
            this._numExposure.Font = new System.Drawing.Font("Consolas", 10F);
            this._numExposure.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this._numExposure.Location = new System.Drawing.Point(180, 64);
            this._numExposure.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this._numExposure.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._numExposure.Name = "_numExposure";
            this._numExposure.Size = new System.Drawing.Size(150, 23);
            this._numExposure.TabIndex = 4;
            this._numExposure.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._numExposure.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);
            //
            // _lblGain
            //
            this._lblGain.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblGain.Location = new System.Drawing.Point(20, 102);
            this._lblGain.Name = "_lblGain";
            this._lblGain.Size = new System.Drawing.Size(160, 22);
            this._lblGain.TabIndex = 5;
            this._lblGain.Text = "Gain (dB)";
            //
            // _numGain
            //
            this._numGain.DecimalPlaces = 1;
            this._numGain.Font = new System.Drawing.Font("Consolas", 10F);
            this._numGain.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this._numGain.Location = new System.Drawing.Point(180, 100);
            this._numGain.Maximum = new decimal(new int[] {
            48,
            0,
            0,
            0});
            this._numGain.Name = "_numGain";
            this._numGain.Size = new System.Drawing.Size(150, 23);
            this._numGain.TabIndex = 6;
            this._numGain.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);
            //
            // _lblFps
            //
            this._lblFps.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblFps.Location = new System.Drawing.Point(20, 138);
            this._lblFps.Name = "_lblFps";
            this._lblFps.Size = new System.Drawing.Size(160, 22);
            this._lblFps.TabIndex = 7;
            this._lblFps.Text = "Frame rate (fps)";
            //
            // _numFps
            //
            this._numFps.Font = new System.Drawing.Font("Consolas", 10F);
            this._numFps.Location = new System.Drawing.Point(180, 136);
            this._numFps.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._numFps.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._numFps.Name = "_numFps";
            this._numFps.Size = new System.Drawing.Size(150, 23);
            this._numFps.TabIndex = 8;
            this._numFps.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._numFps.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);
            //
            // _lblTrig
            //
            this._lblTrig.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblTrig.Location = new System.Drawing.Point(20, 174);
            this._lblTrig.Name = "_lblTrig";
            this._lblTrig.Size = new System.Drawing.Size(160, 22);
            this._lblTrig.TabIndex = 9;
            this._lblTrig.Text = "Trigger Source";
            //
            // _cbTrigger
            //
            this._cbTrigger.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbTrigger.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbTrigger.Location = new System.Drawing.Point(180, 172);
            this._cbTrigger.Name = "_cbTrigger";
            this._cbTrigger.Size = new System.Drawing.Size(150, 23);
            this._cbTrigger.TabIndex = 10;
            this._cbTrigger.SelectedIndexChanged += new System.EventHandler(this.OnAnyFieldChanged);
            //
            // _lblTrigMode
            //
            this._lblTrigMode.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblTrigMode.Location = new System.Drawing.Point(350, 174);
            this._lblTrigMode.Name = "_lblTrigMode";
            this._lblTrigMode.Size = new System.Drawing.Size(110, 22);
            this._lblTrigMode.TabIndex = 30;
            this._lblTrigMode.Text = "Trigger Mode";
            //
            // _cbTriggerMode
            //
            this._cbTriggerMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbTriggerMode.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbTriggerMode.Location = new System.Drawing.Point(470, 172);
            this._cbTriggerMode.Name = "_cbTriggerMode";
            this._cbTriggerMode.Size = new System.Drawing.Size(110, 23);
            this._cbTriggerMode.TabIndex = 31;
            this._cbTriggerMode.SelectedIndexChanged += new System.EventHandler(this.OnTriggerModeUiChanged);
            //
            // _lblPix
            //
            this._lblPix.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblPix.Location = new System.Drawing.Point(20, 210);
            this._lblPix.Name = "_lblPix";
            this._lblPix.Size = new System.Drawing.Size(160, 22);
            this._lblPix.TabIndex = 11;
            this._lblPix.Text = "Pixel format";
            //
            // _cbPixel
            //
            this._cbPixel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbPixel.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbPixel.Location = new System.Drawing.Point(180, 208);
            this._cbPixel.Name = "_cbPixel";
            this._cbPixel.Size = new System.Drawing.Size(150, 23);
            this._cbPixel.TabIndex = 12;
            this._cbPixel.SelectedIndexChanged += new System.EventHandler(this.OnAnyFieldChanged);
            //
            // _lblDelay
            //
            this._lblDelay.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblDelay.Location = new System.Drawing.Point(20, 246);
            this._lblDelay.Name = "_lblDelay";
            this._lblDelay.Size = new System.Drawing.Size(160, 22);
            this._lblDelay.TabIndex = 13;
            this._lblDelay.Text = "Delay before grab (ms)";
            //
            // _numDelay
            //
            this._numDelay.Font = new System.Drawing.Font("Consolas", 10F);
            this._numDelay.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this._numDelay.Location = new System.Drawing.Point(180, 244);
            this._numDelay.Maximum = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            this._numDelay.Name = "_numDelay";
            this._numDelay.Size = new System.Drawing.Size(150, 23);
            this._numDelay.TabIndex = 14;
            this._numDelay.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);
            //
            // _lblRoi
            //
            this._lblRoi.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblRoi.Location = new System.Drawing.Point(20, 282);
            this._lblRoi.Name = "_lblRoi";
            this._lblRoi.Size = new System.Drawing.Size(160, 22);
            this._lblRoi.TabIndex = 15;
            this._lblRoi.Text = "ROI (0=full sensor)";
            //
            // _lblOffX
            //
            this._lblOffX.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblOffX.Location = new System.Drawing.Point(180, 284);
            this._lblOffX.Name = "_lblOffX";
            this._lblOffX.Size = new System.Drawing.Size(56, 22);
            this._lblOffX.TabIndex = 16;
            this._lblOffX.Text = "OffsetX";
            //
            // _numRoiX
            //
            this._numRoiX.Font = new System.Drawing.Font("Consolas", 10F);
            this._numRoiX.Location = new System.Drawing.Point(240, 280);
            this._numRoiX.Maximum = new decimal(new int[] {
            8000,
            0,
            0,
            0});
            this._numRoiX.Name = "_numRoiX";
            this._numRoiX.Size = new System.Drawing.Size(80, 23);
            this._numRoiX.TabIndex = 17;
            this._numRoiX.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);
            //
            // _lblOffY
            //
            this._lblOffY.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblOffY.Location = new System.Drawing.Point(325, 284);
            this._lblOffY.Name = "_lblOffY";
            this._lblOffY.Size = new System.Drawing.Size(56, 22);
            this._lblOffY.TabIndex = 18;
            this._lblOffY.Text = "OffsetY";
            //
            // _numRoiY
            //
            this._numRoiY.Font = new System.Drawing.Font("Consolas", 10F);
            this._numRoiY.Location = new System.Drawing.Point(385, 280);
            this._numRoiY.Maximum = new decimal(new int[] {
            8000,
            0,
            0,
            0});
            this._numRoiY.Name = "_numRoiY";
            this._numRoiY.Size = new System.Drawing.Size(80, 23);
            this._numRoiY.TabIndex = 19;
            this._numRoiY.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);
            //
            // _lblW
            //
            this._lblW.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblW.Location = new System.Drawing.Point(470, 284);
            this._lblW.Name = "_lblW";
            this._lblW.Size = new System.Drawing.Size(16, 22);
            this._lblW.TabIndex = 20;
            this._lblW.Text = "W";
            //
            // _numRoiW
            //
            this._numRoiW.Font = new System.Drawing.Font("Consolas", 10F);
            this._numRoiW.Location = new System.Drawing.Point(490, 280);
            this._numRoiW.Maximum = new decimal(new int[] {
            8000,
            0,
            0,
            0});
            this._numRoiW.Name = "_numRoiW";
            this._numRoiW.Size = new System.Drawing.Size(80, 23);
            this._numRoiW.TabIndex = 21;
            this._numRoiW.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);
            //
            // _lblH
            //
            this._lblH.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblH.Location = new System.Drawing.Point(575, 284);
            this._lblH.Name = "_lblH";
            this._lblH.Size = new System.Drawing.Size(16, 22);
            this._lblH.TabIndex = 22;
            this._lblH.Text = "H";
            //
            // _numRoiH
            //
            this._numRoiH.Font = new System.Drawing.Font("Consolas", 10F);
            this._numRoiH.Location = new System.Drawing.Point(595, 280);
            this._numRoiH.Maximum = new decimal(new int[] {
            8000,
            0,
            0,
            0});
            this._numRoiH.Name = "_numRoiH";
            this._numRoiH.Size = new System.Drawing.Size(80, 23);
            this._numRoiH.TabIndex = 23;
            this._numRoiH.ValueChanged += new System.EventHandler(this.OnAnyFieldChanged);
            //
            // _chkMilDcf
            //
            this._chkMilDcf.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._chkMilDcf.Location = new System.Drawing.Point(20, 320);
            this._chkMilDcf.Name = "_chkMilDcf";
            this._chkMilDcf.Size = new System.Drawing.Size(220, 22);
            this._chkMilDcf.TabIndex = 24;
            this._chkMilDcf.Text = "MIL DCF 직접 지정";
            this._chkMilDcf.Visible = false;
            this._chkMilDcf.CheckedChanged += new System.EventHandler(this.OnMilDcfCheckedChanged);
            //
            // _lblMil
            //
            this._lblMil.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblMil.Location = new System.Drawing.Point(245, 320);
            this._lblMil.Name = "_lblMil";
            this._lblMil.Size = new System.Drawing.Size(100, 22);
            this._lblMil.TabIndex = 25;
            this._lblMil.Text = "DCF 경로";
            this._lblMil.Visible = false;
            //
            // _txtMilDcf
            //
            this._txtMilDcf.Font = new System.Drawing.Font("Consolas", 10F);
            this._txtMilDcf.Location = new System.Drawing.Point(345, 318);
            this._txtMilDcf.Name = "_txtMilDcf";
            this._txtMilDcf.ReadOnly = true;
            this._txtMilDcf.Size = new System.Drawing.Size(270, 23);
            this._txtMilDcf.TabIndex = 26;
            this._txtMilDcf.Visible = false;
            this._txtMilDcf.TextChanged += new System.EventHandler(this.OnMilTextChanged);
            //
            // _btnMilBrowse
            //
            this._btnMilBrowse.BackColor = System.Drawing.Color.White;
            this._btnMilBrowse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMilBrowse.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._btnMilBrowse.ForeColor = System.Drawing.Color.Black;
            this._btnMilBrowse.Location = new System.Drawing.Point(620, 317);
            this._btnMilBrowse.Name = "_btnMilBrowse";
            this._btnMilBrowse.Size = new System.Drawing.Size(50, 25);
            this._btnMilBrowse.TabIndex = 27;
            this._btnMilBrowse.Text = "찾기";
            this._btnMilBrowse.UseVisualStyleBackColor = false;
            this._btnMilBrowse.Visible = false;
            this._btnMilBrowse.Click += new System.EventHandler(this.OnMilBrowseClick);
            //
            // _lblLightAssign
            //
            this._lblLightAssign.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._lblLightAssign.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this._lblLightAssign.Location = new System.Drawing.Point(20, 356);
            this._lblLightAssign.Name = "_lblLightAssign";
            this._lblLightAssign.Size = new System.Drawing.Size(640, 22);
            this._lblLightAssign.TabIndex = 27;
            this._lblLightAssign.Text = "조명 컨트롤러/페이지 지정 (이 모듈 = 카메라 + 조명, 채널 레벨은 [레시피])";
            //
            // _gridLightAssign
            //
            this._gridLightAssign.AllowUserToAddRows = true;
            this._gridLightAssign.AllowUserToDeleteRows = true;
            this._gridLightAssign.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._gridLightAssign.BackgroundColor = System.Drawing.Color.White;
            this._gridLightAssign.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._colLightCtrl,
            this._colLightPage});
            this._gridLightAssign.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._gridLightAssign.Location = new System.Drawing.Point(20, 382);
            this._gridLightAssign.Name = "_gridLightAssign";
            this._gridLightAssign.RowHeadersVisible = false;
            this._gridLightAssign.Size = new System.Drawing.Size(640, 168);
            this._gridLightAssign.TabIndex = 28;
            this._gridLightAssign.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnLightCellEndEdit);
            this._gridLightAssign.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.OnLightGridDataError);
            this._gridLightAssign.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.OnLightRowsRemoved);
            //
            // _colLightCtrl
            //
            this._colLightCtrl.FillWeight = 60F;
            this._colLightCtrl.HeaderText = "컨트롤러(Port)";
            this._colLightCtrl.Name = "ControllerPort";
            //
            // _colLightPage
            //
            this._colLightPage.FillWeight = 40F;
            this._colLightPage.HeaderText = "페이지";
            this._colLightPage.Name = "Page";
            //
            // _lblLightStatus
            //
            this._lblLightStatus.Font = new System.Drawing.Font("Consolas", 9F);
            this._lblLightStatus.ForeColor = System.Drawing.Color.DarkSlateGray;
            this._lblLightStatus.Location = new System.Drawing.Point(20, 554);
            this._lblLightStatus.Name = "_lblLightStatus";
            this._lblLightStatus.Size = new System.Drawing.Size(640, 22);
            this._lblLightStatus.TabIndex = 29;
            //
            // _rightPanel — 우측 반응형 컬럼 (버튼 플로우 + 상태 + 미리보기 채움)
            //
            this._rightPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._rightPanel.Controls.Add(this._picPreview);
            this._rightPanel.Controls.Add(this._lblStatus);
            this._rightPanel.Controls.Add(this._flowRow2);
            this._rightPanel.Controls.Add(this._flowRow1);
            this._rightPanel.Location = new System.Drawing.Point(700, 36);
            this._rightPanel.Name = "_rightPanel";
            this._rightPanel.Size = new System.Drawing.Size(430, 549);
            this._rightPanel.TabIndex = 30;
            //
            // _flowRow1 — 1행: 저장·취소·기본값 복원·실행 모듈에 적용 (고정, 줄바꿈 없음)
            //
            this._flowRow1.AutoSize = true;
            this._flowRow1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._flowRow1.Controls.Add(this._btnSave);
            this._flowRow1.Controls.Add(this._btnCancel);
            this._flowRow1.Controls.Add(this._btnReset);
            this._flowRow1.Controls.Add(this._btnApply);
            this._flowRow1.Dock = System.Windows.Forms.DockStyle.Top;
            this._flowRow1.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this._flowRow1.Location = new System.Drawing.Point(0, 0);
            this._flowRow1.Name = "_flowRow1";
            this._flowRow1.Size = new System.Drawing.Size(430, 36);
            this._flowRow1.TabIndex = 0;
            this._flowRow1.WrapContents = false;
            //
            // _flowRow2 — 2행: Connect·테스트 그랩·Live Start·Live Stop (고정, 줄바꿈 없음)
            //
            this._flowRow2.AutoSize = true;
            this._flowRow2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._flowRow2.Controls.Add(this._btnConnect);
            this._flowRow2.Controls.Add(this._btnTestGrab);
            this._flowRow2.Controls.Add(this._btnLiveStart);
            this._flowRow2.Controls.Add(this._btnLiveStop);
            this._flowRow2.Dock = System.Windows.Forms.DockStyle.Top;
            this._flowRow2.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this._flowRow2.Location = new System.Drawing.Point(0, 36);
            this._flowRow2.Name = "_flowRow2";
            this._flowRow2.Size = new System.Drawing.Size(430, 36);
            this._flowRow2.TabIndex = 1;
            this._flowRow2.WrapContents = false;
            //
            // _btnSave
            //
            this._btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSave.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnSave.ForeColor = System.Drawing.Color.White;
            this._btnSave.Margin = new System.Windows.Forms.Padding(0, 0, 4, 4);
            this._btnSave.Name = "_btnSave";
            this._btnSave.Size = new System.Drawing.Size(100, 32);
            this._btnSave.TabIndex = 0;
            this._btnSave.Text = "저장";
            this._btnSave.UseVisualStyleBackColor = false;
            this._btnSave.Click += new System.EventHandler(this.OnSaveClick);
            //
            // _btnCancel
            //
            this._btnCancel.BackColor = System.Drawing.Color.White;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCancel.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnCancel.ForeColor = System.Drawing.Color.Black;
            this._btnCancel.Margin = new System.Windows.Forms.Padding(0, 0, 4, 4);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(90, 32);
            this._btnCancel.TabIndex = 1;
            this._btnCancel.Text = "취소";
            this._btnCancel.UseVisualStyleBackColor = false;
            this._btnCancel.Click += new System.EventHandler(this.OnCancelClick);
            //
            // _btnReset
            //
            this._btnReset.BackColor = System.Drawing.Color.White;
            this._btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnReset.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnReset.ForeColor = System.Drawing.Color.Black;
            this._btnReset.Margin = new System.Windows.Forms.Padding(0, 0, 4, 4);
            this._btnReset.Name = "_btnReset";
            this._btnReset.Size = new System.Drawing.Size(120, 32);
            this._btnReset.TabIndex = 2;
            this._btnReset.Text = "기본값 복원";
            this._btnReset.UseVisualStyleBackColor = false;
            this._btnReset.Click += new System.EventHandler(this.OnResetClick);
            //
            // _btnApply
            //
            this._btnApply.BackColor = System.Drawing.Color.White;
            this._btnApply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnApply.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnApply.ForeColor = System.Drawing.Color.Black;
            this._btnApply.Margin = new System.Windows.Forms.Padding(0, 0, 4, 4);
            this._btnApply.Name = "_btnApply";
            this._btnApply.Size = new System.Drawing.Size(150, 32);
            this._btnApply.TabIndex = 3;
            this._btnApply.Text = "실행 모듈에 적용";
            this._btnApply.UseVisualStyleBackColor = false;
            this._btnApply.Click += new System.EventHandler(this.OnApplyClick);
            //
            // _btnTestGrab
            //
            this._btnTestGrab.BackColor = System.Drawing.Color.White;
            this._btnTestGrab.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnTestGrab.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnTestGrab.ForeColor = System.Drawing.Color.Black;
            this._btnTestGrab.Margin = new System.Windows.Forms.Padding(0, 0, 4, 4);
            this._btnTestGrab.Name = "_btnTestGrab";
            this._btnTestGrab.Size = new System.Drawing.Size(120, 32);
            this._btnTestGrab.TabIndex = 4;
            this._btnTestGrab.Text = "테스트 그랩";
            this._btnTestGrab.UseVisualStyleBackColor = false;
            this._btnTestGrab.Click += new System.EventHandler(this.OnTestGrabClick);
            //
            // _btnConnect
            //
            this._btnConnect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnConnect.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnConnect.ForeColor = System.Drawing.Color.White;
            this._btnConnect.Margin = new System.Windows.Forms.Padding(0, 0, 4, 4);
            this._btnConnect.Name = "_btnConnect";
            this._btnConnect.Size = new System.Drawing.Size(104, 32);
            this._btnConnect.TabIndex = 5;
            this._btnConnect.Text = "Connect";
            this._btnConnect.UseVisualStyleBackColor = false;
            this._btnConnect.Click += new System.EventHandler(this.OnConnectClick);
            //
            // _btnLiveStart
            //
            this._btnLiveStart.BackColor = System.Drawing.Color.White;
            this._btnLiveStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnLiveStart.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnLiveStart.ForeColor = System.Drawing.Color.Black;
            this._btnLiveStart.Margin = new System.Windows.Forms.Padding(0, 0, 4, 4);
            this._btnLiveStart.Name = "_btnLiveStart";
            this._btnLiveStart.Size = new System.Drawing.Size(104, 32);
            this._btnLiveStart.TabIndex = 6;
            this._btnLiveStart.Text = "Live Start";
            this._btnLiveStart.UseVisualStyleBackColor = false;
            this._btnLiveStart.Click += new System.EventHandler(this.OnLiveStartClick);
            //
            // _btnLiveStop
            //
            this._btnLiveStop.BackColor = System.Drawing.Color.White;
            this._btnLiveStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnLiveStop.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnLiveStop.ForeColor = System.Drawing.Color.Black;
            this._btnLiveStop.Margin = new System.Windows.Forms.Padding(0, 0, 4, 4);
            this._btnLiveStop.Name = "_btnLiveStop";
            this._btnLiveStop.Size = new System.Drawing.Size(104, 32);
            this._btnLiveStop.TabIndex = 7;
            this._btnLiveStop.Text = "Live Stop";
            this._btnLiveStop.UseVisualStyleBackColor = false;
            this._btnLiveStop.Click += new System.EventHandler(this.OnLiveStopClick);
            //
            // _lblStatus
            //
            this._lblStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblStatus.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblStatus.ForeColor = System.Drawing.Color.DarkSlateGray;
            this._lblStatus.Location = new System.Drawing.Point(0, 76);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(430, 24);
            this._lblStatus.TabIndex = 1;
            //
            // _picPreview — 남는 공간 전부 채움
            //
            this._picPreview.BackColor = System.Drawing.Color.Black;
            this._picPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._picPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this._picPreview.MaximumSize = new System.Drawing.Size(640, 480);
            this._picPreview.Location = new System.Drawing.Point(0, 100);
            this._picPreview.Name = "_picPreview";
            this._picPreview.Size = new System.Drawing.Size(430, 449);
            this._picPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this._picPreview.TabIndex = 2;
            this._picPreview.TabStop = false;
            //
            // CameraMappingPanel
            //
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.Controls.Add(this._lblAlgorithm);
            this.Controls.Add(this._body);
            this.Name = "CameraMappingPanel";
            this.Size = new System.Drawing.Size(1140, 595);
            this._flowRow1.ResumeLayout(false);
            this._flowRow2.ResumeLayout(false);
            this._rightPanel.ResumeLayout(false);
            this._rightPanel.PerformLayout();
            this._body.ResumeLayout(false);
            this._body.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._numExposure)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numGain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numFps)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numRoiX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numRoiY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numRoiW)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numRoiH)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._gridLightAssign)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._picPreview)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
