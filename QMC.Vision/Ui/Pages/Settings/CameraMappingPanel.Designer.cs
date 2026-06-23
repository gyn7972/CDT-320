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
        // docked 골격 (핸들러 INPUT CASSETTE 스타일 — 화면 꽉 채움)
        private TableLayoutPanel _main;       // 2열: 좌(파라미터/조명) · 우(미리보기/버튼)
        private TableLayoutPanel _left;        // 좌 컬럼 세로 스택
        private Panel    _camRow;              // 카메라 ID + 검색 (상단 툴바)
        private Label    _lblCamId;
        private ComboBox _cbCameraId;
        private Button   _btnDiscover;
        private Label    _secParam;            // "CAMERA PARAMETERS" 섹션바
        // 카메라 파라미터 — 리스트 그리드(행=항목, Code 의 BuildParamItems 로 채움)
        private QMC.Vision.Ui.Controls.ParameterGridControl _paramGrid;
        private Label    _secScale;            // "SCALE / CALIBRATION" 섹션바
        // 스케일/좌표변환 — 전용 리스트 그리드(BuildScaleItems 로 채움)
        private QMC.Vision.Ui.Controls.ParameterGridControl _scaleGrid;
        private Panel    _milRow;              // MIL DCF 직접지정 (조건부)
        private CheckBox _chkMilDcf;
        private Label    _lblMil;    private TextBox _txtMilDcf;   private Button _btnMilBrowse;
        // 조명 컨트롤러/페이지 지정(모듈 Setup.LightPages)
        private Label    _lblLightAssign;
        private DataGridView _gridLightAssign;
        private Label    _lblLightStatus;
        private Panel    _lnParam, _lnScale, _lnLight;   // 섹션 타이틀 주황 밑줄
        // 우측 컬럼 — 버튼 2행(Top) + 상태(Top) + 미리보기(Fill)
        private Panel    _rightPanel;
        private TableLayoutPanel _btnGrid;     // 하단 툴바 — 불러오기/저장(GENERAL 동일)
        private TableLayoutPanel _btnRight;    // 우측 액션 버튼(취소/기본값/적용/Connect/스케일계산)
        private Button   _btnSave, _btnCancel, _btnReset, _btnApply, _btnScaleCalc, _btnLoad;
        private Button   _btnConnect;
        private Label    _lblStatus;
        private QMC.Vision.Ui.Controls.CameraView _camPreview;

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
            this._main = new System.Windows.Forms.TableLayoutPanel();
            this._left = new System.Windows.Forms.TableLayoutPanel();
            this._camRow = new System.Windows.Forms.Panel();
            this._lblCamId = new System.Windows.Forms.Label();
            this._cbCameraId = new System.Windows.Forms.ComboBox();
            this._btnDiscover = new System.Windows.Forms.Button();
            this._secParam = new System.Windows.Forms.Label();
            this._lnParam = new System.Windows.Forms.Panel();
            this._paramGrid = new QMC.Vision.Ui.Controls.ParameterGridControl();
            this._secScale = new System.Windows.Forms.Label();
            this._lnScale = new System.Windows.Forms.Panel();
            this._scaleGrid = new QMC.Vision.Ui.Controls.ParameterGridControl();
            this._milRow = new System.Windows.Forms.Panel();
            this._chkMilDcf = new System.Windows.Forms.CheckBox();
            this._lblMil = new System.Windows.Forms.Label();
            this._txtMilDcf = new System.Windows.Forms.TextBox();
            this._btnMilBrowse = new System.Windows.Forms.Button();
            this._lblLightAssign = new System.Windows.Forms.Label();
            this._lnLight = new System.Windows.Forms.Panel();
            this._gridLightAssign = new System.Windows.Forms.DataGridView();
            this.ControllerPort = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.LightName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Page = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this._lblLightStatus = new System.Windows.Forms.Label();
            this._rightPanel = new System.Windows.Forms.Panel();
            this._camPreview = new QMC.Vision.Ui.Controls.CameraView();
            this._lblStatus = new System.Windows.Forms.Label();
            this._btnRight = new System.Windows.Forms.TableLayoutPanel();
            this._btnCancel = new System.Windows.Forms.Button();
            this._btnReset = new System.Windows.Forms.Button();
            this._btnApply = new System.Windows.Forms.Button();
            this._btnConnect = new System.Windows.Forms.Button();
            this._btnScaleCalc = new System.Windows.Forms.Button();
            this._btnGrid = new System.Windows.Forms.TableLayoutPanel();
            this._btnLoad = new System.Windows.Forms.Button();
            this._btnSave = new System.Windows.Forms.Button();
            this._body.SuspendLayout();
            this._main.SuspendLayout();
            this._left.SuspendLayout();
            this._camRow.SuspendLayout();
            this._secParam.SuspendLayout();
            this._secScale.SuspendLayout();
            this._milRow.SuspendLayout();
            this._lblLightAssign.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._gridLightAssign)).BeginInit();
            this._rightPanel.SuspendLayout();
            this._btnRight.SuspendLayout();
            this._btnGrid.SuspendLayout();
            this.SuspendLayout();
            // 
            // _lblAlgorithm
            // 
            this._lblAlgorithm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._lblAlgorithm.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblAlgorithm.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._lblAlgorithm.ForeColor = System.Drawing.Color.White;
            this._lblAlgorithm.Location = new System.Drawing.Point(12, 8);
            this._lblAlgorithm.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this._lblAlgorithm.Name = "_lblAlgorithm";
            this._lblAlgorithm.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._lblAlgorithm.Size = new System.Drawing.Size(1116, 32);
            this._lblAlgorithm.TabIndex = 0;
            this._lblAlgorithm.Text = "카메라 매핑";
            this._lblAlgorithm.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _body
            // 
            this._body.AutoScroll = true;
            // 창이 작아져 세로 공간이 부족하면 좌측 설정(스케일·조명 등)이 잘리지 않도록 최소 가상 높이 지정.
            // 뷰포트가 이보다 작아지면 자동 세로 스크롤이 생기고, 충분히 크면 기존처럼 프리뷰가 채운다.
            this._body.AutoScrollMinSize = new System.Drawing.Size(0, 600);
            this._body.Controls.Add(this._main);
            this._body.Dock = System.Windows.Forms.DockStyle.Fill;
            this._body.Location = new System.Drawing.Point(12, 40);
            this._body.Name = "_body";
            this._body.Size = new System.Drawing.Size(1116, 501);
            this._body.TabIndex = 1;
            // 
            // _main
            // 
            this._main.ColumnCount = 2;
            this._main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this._main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this._main.Controls.Add(this._left, 0, 0);
            this._main.Controls.Add(this._rightPanel, 1, 0);
            this._main.Dock = System.Windows.Forms.DockStyle.Fill;
            this._main.Location = new System.Drawing.Point(0, 0);
            this._main.Margin = new System.Windows.Forms.Padding(0);
            this._main.Name = "_main";
            this._main.RowCount = 1;
            this._main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._main.Size = new System.Drawing.Size(1116, 501);
            this._main.TabIndex = 0;
            // 
            // _left
            // 
            this._left.ColumnCount = 1;
            this._left.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._left.Controls.Add(this._camRow, 0, 0);
            this._left.Controls.Add(this._secParam, 0, 1);
            this._left.Controls.Add(this._paramGrid, 0, 2);
            this._left.Controls.Add(this._secScale, 0, 3);
            this._left.Controls.Add(this._scaleGrid, 0, 4);
            this._left.Controls.Add(this._milRow, 0, 5);
            this._left.Controls.Add(this._lblLightAssign, 0, 6);
            this._left.Controls.Add(this._gridLightAssign, 0, 7);
            this._left.Controls.Add(this._lblLightStatus, 0, 8);
            this._left.Dock = System.Windows.Forms.DockStyle.Fill;
            this._left.Location = new System.Drawing.Point(0, 0);
            this._left.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this._left.Name = "_left";
            this._left.RowCount = 10;
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._left.Size = new System.Drawing.Size(605, 501);
            this._left.TabIndex = 0;
            // 
            // _camRow
            // 
            this._camRow.Controls.Add(this._lblCamId);
            this._camRow.Controls.Add(this._cbCameraId);
            this._camRow.Controls.Add(this._btnDiscover);
            this._camRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._camRow.Location = new System.Drawing.Point(0, 0);
            this._camRow.Margin = new System.Windows.Forms.Padding(0);
            this._camRow.Name = "_camRow";
            this._camRow.Size = new System.Drawing.Size(605, 40);
            this._camRow.TabIndex = 0;
            // 
            // _lblCamId
            // 
            this._lblCamId.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblCamId.Location = new System.Drawing.Point(4, 9);
            this._lblCamId.Name = "_lblCamId";
            this._lblCamId.Size = new System.Drawing.Size(90, 22);
            this._lblCamId.TabIndex = 0;
            this._lblCamId.Tag = "i18n:set.cam.camId";
            this._lblCamId.Text = "카메라 ID";
            // 
            // _cbCameraId
            // 
            this._cbCameraId.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbCameraId.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbCameraId.Location = new System.Drawing.Point(98, 7);
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
            this._btnDiscover.Location = new System.Drawing.Point(466, 5);
            this._btnDiscover.Name = "_btnDiscover";
            this._btnDiscover.Size = new System.Drawing.Size(120, 28);
            this._btnDiscover.TabIndex = 2;
            this._btnDiscover.Tag = "i18n:set.cam.discover";
            this._btnDiscover.Text = "카메라 검색";
            this._btnDiscover.UseVisualStyleBackColor = false;
            this._btnDiscover.Click += new System.EventHandler(this.OnDiscoverClick);
            // 
            // _secParam
            // 
            this._secParam.Controls.Add(this._lnParam);
            this._secParam.Dock = System.Windows.Forms.DockStyle.Fill;
            this._secParam.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._secParam.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this._secParam.Location = new System.Drawing.Point(0, 46);
            this._secParam.Margin = new System.Windows.Forms.Padding(0, 6, 0, 1);
            this._secParam.Name = "_secParam";
            this._secParam.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._secParam.Size = new System.Drawing.Size(605, 21);
            this._secParam.TabIndex = 3;
            this._secParam.Tag = "i18n:set.cam.secParam";
            this._secParam.Text = "카메라 파라미터";
            this._secParam.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lnParam
            // 
            this._lnParam.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._lnParam.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._lnParam.Location = new System.Drawing.Point(0, 19);
            this._lnParam.Name = "_lnParam";
            this._lnParam.Size = new System.Drawing.Size(605, 2);
            this._lnParam.TabIndex = 0;
            // 
            // _paramGrid
            // 
            this._paramGrid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this._paramGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._paramGrid.Location = new System.Drawing.Point(0, 69);
            this._paramGrid.Margin = new System.Windows.Forms.Padding(0, 1, 0, 4);
            this._paramGrid.Name = "_paramGrid";
            this._paramGrid.Size = new System.Drawing.Size(605, 115);
            this._paramGrid.TabIndex = 4;
            // 
            // _secScale
            // 
            this._secScale.Controls.Add(this._lnScale);
            this._secScale.Dock = System.Windows.Forms.DockStyle.Fill;
            this._secScale.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._secScale.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this._secScale.Location = new System.Drawing.Point(0, 194);
            this._secScale.Margin = new System.Windows.Forms.Padding(0, 6, 0, 1);
            this._secScale.Name = "_secScale";
            this._secScale.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._secScale.Size = new System.Drawing.Size(605, 21);
            this._secScale.TabIndex = 5;
            this._secScale.Tag = "i18n:set.cam.secScale";
            this._secScale.Text = "스케일 / 캘리브레이션";
            this._secScale.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lnScale
            // 
            this._lnScale.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._lnScale.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._lnScale.Location = new System.Drawing.Point(0, 19);
            this._lnScale.Name = "_lnScale";
            this._lnScale.Size = new System.Drawing.Size(605, 2);
            this._lnScale.TabIndex = 0;
            // 
            // _scaleGrid
            // 
            this._scaleGrid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this._scaleGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._scaleGrid.Location = new System.Drawing.Point(0, 217);
            this._scaleGrid.Margin = new System.Windows.Forms.Padding(0, 1, 0, 4);
            this._scaleGrid.Name = "_scaleGrid";
            this._scaleGrid.Size = new System.Drawing.Size(605, 115);
            this._scaleGrid.TabIndex = 6;
            // 
            // _milRow
            // 
            this._milRow.Controls.Add(this._chkMilDcf);
            this._milRow.Controls.Add(this._lblMil);
            this._milRow.Controls.Add(this._txtMilDcf);
            this._milRow.Controls.Add(this._btnMilBrowse);
            this._milRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._milRow.Location = new System.Drawing.Point(0, 336);
            this._milRow.Margin = new System.Windows.Forms.Padding(0);
            this._milRow.Name = "_milRow";
            this._milRow.Size = new System.Drawing.Size(605, 30);
            this._milRow.TabIndex = 7;
            // 
            // _chkMilDcf
            // 
            this._chkMilDcf.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._chkMilDcf.Location = new System.Drawing.Point(4, 4);
            this._chkMilDcf.Name = "_chkMilDcf";
            this._chkMilDcf.Size = new System.Drawing.Size(200, 22);
            this._chkMilDcf.TabIndex = 24;
            this._chkMilDcf.Tag = "i18n:set.cam.milDcf";
            this._chkMilDcf.Text = "MIL DCF 직접 지정";
            this._chkMilDcf.Visible = false;
            this._chkMilDcf.CheckedChanged += new System.EventHandler(this.OnMilDcfCheckedChanged);
            // 
            // _lblMil
            // 
            this._lblMil.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblMil.Location = new System.Drawing.Point(214, 4);
            this._lblMil.Name = "_lblMil";
            this._lblMil.Size = new System.Drawing.Size(80, 22);
            this._lblMil.TabIndex = 25;
            this._lblMil.Tag = "i18n:set.cam.dcfPath";
            this._lblMil.Text = "DCF 경로";
            this._lblMil.Visible = false;
            // 
            // _txtMilDcf
            // 
            this._txtMilDcf.Font = new System.Drawing.Font("Consolas", 10F);
            this._txtMilDcf.Location = new System.Drawing.Point(298, 3);
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
            this._btnMilBrowse.Location = new System.Drawing.Point(573, 2);
            this._btnMilBrowse.Name = "_btnMilBrowse";
            this._btnMilBrowse.Size = new System.Drawing.Size(50, 25);
            this._btnMilBrowse.TabIndex = 27;
            this._btnMilBrowse.Tag = "i18n:set.cam.browse";
            this._btnMilBrowse.Text = "찾기";
            this._btnMilBrowse.UseVisualStyleBackColor = false;
            this._btnMilBrowse.Visible = false;
            this._btnMilBrowse.Click += new System.EventHandler(this.OnMilBrowseClick);
            // 
            // _lblLightAssign
            // 
            this._lblLightAssign.Controls.Add(this._lnLight);
            this._lblLightAssign.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblLightAssign.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._lblLightAssign.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this._lblLightAssign.Location = new System.Drawing.Point(0, 372);
            this._lblLightAssign.Margin = new System.Windows.Forms.Padding(0, 6, 0, 1);
            this._lblLightAssign.Name = "_lblLightAssign";
            this._lblLightAssign.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._lblLightAssign.Size = new System.Drawing.Size(605, 21);
            this._lblLightAssign.TabIndex = 27;
            this._lblLightAssign.Tag = "i18n:set.cam.secLight";
            this._lblLightAssign.Text = "조명 컨트롤러 / 페이지 지정";
            this._lblLightAssign.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lnLight
            // 
            this._lnLight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._lnLight.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._lnLight.Location = new System.Drawing.Point(0, 19);
            this._lnLight.Name = "_lnLight";
            this._lnLight.Size = new System.Drawing.Size(605, 2);
            this._lnLight.TabIndex = 0;
            // 
            // _gridLightAssign
            // 
            this._gridLightAssign.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._gridLightAssign.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this._gridLightAssign.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._gridLightAssign.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this._gridLightAssign.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this._gridLightAssign.ColumnHeadersDefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            this._gridLightAssign.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(234)))), ((int)(((byte)(237)))));
            this._gridLightAssign.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this._gridLightAssign.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(52)))), ((int)(((byte)(58)))));
            this._gridLightAssign.ColumnHeadersHeight = 28;
            this._gridLightAssign.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this._gridLightAssign.EnableHeadersVisualStyles = false;
            this._gridLightAssign.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(228)))), ((int)(((byte)(232)))));
            this._gridLightAssign.AllowUserToResizeRows = false;
            this._gridLightAssign.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ControllerPort,
            this.LightName,
            this.Page});
            this._gridLightAssign.Dock = System.Windows.Forms.DockStyle.Fill;
            this._gridLightAssign.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._gridLightAssign.Location = new System.Drawing.Point(0, 396);
            this._gridLightAssign.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this._gridLightAssign.Name = "_gridLightAssign";
            this._gridLightAssign.RowHeadersVisible = false;
            this._gridLightAssign.RowTemplate.Height = 30;
            this._gridLightAssign.Size = new System.Drawing.Size(605, 166);
            this._gridLightAssign.TabIndex = 28;
            this._gridLightAssign.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnLightCellEndEdit);
            this._gridLightAssign.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.OnLightGridDataError);
            this._gridLightAssign.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.OnLightRowsRemoved);
            // 
            // ControllerPort
            // 
            this.ControllerPort.FillWeight = 42F;
            this.ControllerPort.HeaderText = "컨트롤러(Port)";
            this.ControllerPort.Name = "ControllerPort";
            //
            // LightName
            //
            this.LightName.FillWeight = 33F;
            this.LightName.HeaderText = "이름";
            this.LightName.Name = "LightName";
            this.LightName.ReadOnly = true;
            //
            // Page
            //
            this.Page.FillWeight = 25F;
            this.Page.HeaderText = "페이지";
            this.Page.Name = "Page";
            // 
            // _lblLightStatus
            // 
            this._lblLightStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblLightStatus.Font = new System.Drawing.Font("Consolas", 9F);
            this._lblLightStatus.ForeColor = System.Drawing.Color.DarkSlateGray;
            this._lblLightStatus.Location = new System.Drawing.Point(0, 564);
            this._lblLightStatus.Margin = new System.Windows.Forms.Padding(0);
            this._lblLightStatus.Name = "_lblLightStatus";
            this._lblLightStatus.Size = new System.Drawing.Size(605, 22);
            this._lblLightStatus.TabIndex = 29;
            // 
            // _rightPanel
            // 
            this._rightPanel.Controls.Add(this._camPreview);
            this._rightPanel.Controls.Add(this._lblStatus);
            this._rightPanel.Controls.Add(this._btnRight);
            this._rightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._rightPanel.Location = new System.Drawing.Point(613, 0);
            this._rightPanel.Margin = new System.Windows.Forms.Padding(0);
            this._rightPanel.Name = "_rightPanel";
            this._rightPanel.Size = new System.Drawing.Size(503, 501);
            this._rightPanel.TabIndex = 30;
            // 
            // _camPreview
            // 
            this._camPreview.BackColor = System.Drawing.Color.Black;
            this._camPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this._camPreview.InfoText = "";
            this._camPreview.Location = new System.Drawing.Point(0, 104);
            this._camPreview.MmPerPixelX = 0D;
            this._camPreview.MmPerPixelY = 0D;
            this._camPreview.Name = "_camPreview";
            this._camPreview.ShowCrosshair = false;
            this._camPreview.ShowLiveLabel = true;
            this._camPreview.ShowToolbar = false;
            this._camPreview.Size = new System.Drawing.Size(503, 397);
            this._camPreview.TabIndex = 0;
            // 
            // _lblStatus
            // 
            this._lblStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblStatus.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblStatus.ForeColor = System.Drawing.Color.DarkSlateGray;
            this._lblStatus.Location = new System.Drawing.Point(0, 80);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(503, 24);
            this._lblStatus.TabIndex = 1;
            // 
            // _btnRight
            // 
            this._btnRight.ColumnCount = 4;
            this._btnRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this._btnRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this._btnRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this._btnRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this._btnRight.Controls.Add(this._btnCancel, 0, 0);
            this._btnRight.Controls.Add(this._btnReset, 1, 0);
            this._btnRight.Controls.Add(this._btnApply, 2, 0);
            this._btnRight.Controls.Add(this._btnConnect, 0, 1);
            this._btnRight.Controls.Add(this._btnScaleCalc, 1, 1);
            this._btnRight.Dock = System.Windows.Forms.DockStyle.Top;
            this._btnRight.Location = new System.Drawing.Point(0, 0);
            this._btnRight.Margin = new System.Windows.Forms.Padding(0);
            this._btnRight.Name = "_btnRight";
            this._btnRight.RowCount = 2;
            this._btnRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this._btnRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this._btnRight.Size = new System.Drawing.Size(503, 80);
            this._btnRight.TabIndex = 2;
            // 
            // _btnCancel
            // 
            this._btnCancel.BackColor = System.Drawing.Color.White;
            this._btnCancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCancel.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnCancel.ForeColor = System.Drawing.Color.Black;
            this._btnCancel.Location = new System.Drawing.Point(3, 3);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(119, 34);
            this._btnCancel.TabIndex = 1;
            this._btnCancel.Tag = "i18n:common.cancel";
            this._btnCancel.Text = "취소";
            this._btnCancel.UseVisualStyleBackColor = false;
            this._btnCancel.Click += new System.EventHandler(this.OnCancelClick);
            // 
            // _btnReset
            // 
            this._btnReset.BackColor = System.Drawing.Color.White;
            this._btnReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnReset.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnReset.ForeColor = System.Drawing.Color.Black;
            this._btnReset.Location = new System.Drawing.Point(128, 3);
            this._btnReset.Name = "_btnReset";
            this._btnReset.Size = new System.Drawing.Size(119, 34);
            this._btnReset.TabIndex = 2;
            this._btnReset.Tag = "i18n:set.cam.reset";
            this._btnReset.Text = "기본값 복원";
            this._btnReset.UseVisualStyleBackColor = false;
            this._btnReset.Click += new System.EventHandler(this.OnResetClick);
            // 
            // _btnApply
            // 
            this._btnApply.BackColor = System.Drawing.Color.White;
            this._btnApply.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnApply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnApply.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnApply.ForeColor = System.Drawing.Color.Black;
            this._btnApply.Location = new System.Drawing.Point(253, 3);
            this._btnApply.Name = "_btnApply";
            this._btnApply.Size = new System.Drawing.Size(119, 34);
            this._btnApply.TabIndex = 3;
            this._btnApply.Tag = "i18n:set.cam.apply";
            this._btnApply.Text = "실행 모듈에 적용";
            this._btnApply.UseVisualStyleBackColor = false;
            this._btnApply.Click += new System.EventHandler(this.OnApplyClick);
            // 
            // _btnConnect
            // 
            this._btnConnect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnConnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnConnect.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnConnect.ForeColor = System.Drawing.Color.White;
            this._btnConnect.Location = new System.Drawing.Point(3, 43);
            this._btnConnect.Name = "_btnConnect";
            this._btnConnect.Size = new System.Drawing.Size(119, 34);
            this._btnConnect.TabIndex = 5;
            this._btnConnect.Text = "Connect";
            this._btnConnect.UseVisualStyleBackColor = false;
            this._btnConnect.Click += new System.EventHandler(this.OnConnectClick);
            // 
            // _btnScaleCalc
            // 
            this._btnScaleCalc.BackColor = System.Drawing.Color.White;
            this._btnScaleCalc.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnScaleCalc.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnScaleCalc.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnScaleCalc.ForeColor = System.Drawing.Color.Black;
            this._btnScaleCalc.Location = new System.Drawing.Point(128, 43);
            this._btnScaleCalc.Name = "_btnScaleCalc";
            this._btnScaleCalc.Size = new System.Drawing.Size(119, 34);
            this._btnScaleCalc.TabIndex = 8;
            this._btnScaleCalc.Tag = "i18n:set.cam.scaleCalc";
            this._btnScaleCalc.Text = "스케일 계산";
            this._btnScaleCalc.UseVisualStyleBackColor = false;
            this._btnScaleCalc.Click += new System.EventHandler(this.OnScaleCalcClick);
            // 
            // _btnGrid
            // 
            this._btnGrid.ColumnCount = 3;
            this._btnGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._btnGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this._btnGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this._btnGrid.Controls.Add(this._btnLoad, 1, 0);
            this._btnGrid.Controls.Add(this._btnSave, 2, 0);
            this._btnGrid.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._btnGrid.Location = new System.Drawing.Point(12, 541);
            this._btnGrid.Margin = new System.Windows.Forms.Padding(0);
            this._btnGrid.Name = "_btnGrid";
            this._btnGrid.RowCount = 1;
            this._btnGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._btnGrid.Size = new System.Drawing.Size(1116, 46);
            this._btnGrid.TabIndex = 3;
            // 
            // _btnLoad
            // 
            this._btnLoad.BackColor = System.Drawing.Color.White;
            this._btnLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnLoad.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnLoad.ForeColor = System.Drawing.Color.Black;
            this._btnLoad.Location = new System.Drawing.Point(863, 3);
            this._btnLoad.Name = "_btnLoad";
            this._btnLoad.Size = new System.Drawing.Size(122, 40);
            this._btnLoad.TabIndex = 9;
            this._btnLoad.Tag = "i18n:common.load";
            this._btnLoad.Text = "불러오기";
            this._btnLoad.UseVisualStyleBackColor = false;
            this._btnLoad.Click += new System.EventHandler(this.OnLoadClick);
            // 
            // _btnSave
            // 
            this._btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSave.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnSave.ForeColor = System.Drawing.Color.White;
            this._btnSave.Location = new System.Drawing.Point(991, 3);
            this._btnSave.Name = "_btnSave";
            this._btnSave.Size = new System.Drawing.Size(122, 40);
            this._btnSave.TabIndex = 0;
            this._btnSave.Tag = "i18n:common.save";
            this._btnSave.Text = "저장";
            this._btnSave.UseVisualStyleBackColor = false;
            this._btnSave.Click += new System.EventHandler(this.OnSaveClick);
            // 
            // CameraMappingPanel
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.Controls.Add(this._body);
            this.Controls.Add(this._btnGrid);
            this.Controls.Add(this._lblAlgorithm);
            this.Name = "CameraMappingPanel";
            this.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.Size = new System.Drawing.Size(1140, 595);
            this._body.ResumeLayout(false);
            this._main.ResumeLayout(false);
            this._left.ResumeLayout(false);
            this._camRow.ResumeLayout(false);
            this._secParam.ResumeLayout(false);
            this._secScale.ResumeLayout(false);
            this._milRow.ResumeLayout(false);
            this._milRow.PerformLayout();
            this._lblLightAssign.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._gridLightAssign)).EndInit();
            this._rightPanel.ResumeLayout(false);
            this._btnRight.ResumeLayout(false);
            this._btnGrid.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private DataGridViewComboBoxColumn ControllerPort;
        private DataGridViewTextBoxColumn  LightName;
        private DataGridViewComboBoxColumn Page;
    }
}
