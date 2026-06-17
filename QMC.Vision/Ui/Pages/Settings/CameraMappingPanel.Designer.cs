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
        private Panel    _milRow;              // MIL DCF 직접지정 (조건부)
        private CheckBox _chkMilDcf;
        private Label    _lblMil;    private TextBox _txtMilDcf;   private Button _btnMilBrowse;
        // 조명 컨트롤러/페이지 지정(모듈 Setup.LightPages)
        private Label    _lblLightAssign;
        private DataGridView _gridLightAssign;
        private DataGridViewComboBoxColumn _colLightCtrl;
        private DataGridViewComboBoxColumn _colLightPage;
        private Label    _lblLightStatus;
        // 우측 컬럼 — 버튼 2행(Top) + 상태(Top) + 미리보기(Fill)
        private Panel    _rightPanel;
        private TableLayoutPanel _btnGrid;     // 액션 버튼 4×2 균일 그리드
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
            this._main = new System.Windows.Forms.TableLayoutPanel();
            this._left = new System.Windows.Forms.TableLayoutPanel();
            this._camRow = new System.Windows.Forms.Panel();
            this._lblCamId = new System.Windows.Forms.Label();
            this._cbCameraId = new System.Windows.Forms.ComboBox();
            this._btnDiscover = new System.Windows.Forms.Button();
            this._secParam = new System.Windows.Forms.Label();
            this._paramGrid = new QMC.Vision.Ui.Controls.ParameterGridControl();
            this._milRow = new System.Windows.Forms.Panel();
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
            this._btnGrid = new System.Windows.Forms.TableLayoutPanel();
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
            this._main.SuspendLayout();
            this._left.SuspendLayout();
            this._camRow.SuspendLayout();
            this._milRow.SuspendLayout();
            this._rightPanel.SuspendLayout();
            this._btnGrid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._gridLightAssign)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._picPreview)).BeginInit();
            this.SuspendLayout();
            //
            // _lblAlgorithm — 페이지 상단 주황 헤더
            //
            this._lblAlgorithm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._lblAlgorithm.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblAlgorithm.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._lblAlgorithm.ForeColor = System.Drawing.Color.White;
            this._lblAlgorithm.Name = "_lblAlgorithm";
            this._lblAlgorithm.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._lblAlgorithm.Size = new System.Drawing.Size(1140, 30);
            this._lblAlgorithm.TabIndex = 0;
            this._lblAlgorithm.Text = "카메라 매핑";
            this._lblAlgorithm.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _body — 헤더 아래 전체 채움(docked _main 호스트)
            //
            this._body.AutoScroll = true;
            this._body.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._body.Controls.Add(this._main);
            this._body.Dock = System.Windows.Forms.DockStyle.Fill;
            this._body.Name = "_body";
            this._body.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this._body.Size = new System.Drawing.Size(1140, 565);
            this._body.TabIndex = 1;
            //
            // _main — 2열 (좌 62% · 우 38%)
            //
            this._main.ColumnCount = 2;
            this._main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this._main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this._main.RowCount = 1;
            this._main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._main.Dock = System.Windows.Forms.DockStyle.Fill;
            this._main.Margin = new System.Windows.Forms.Padding(0);
            this._main.Name = "_main";
            this._main.Controls.Add(this._left, 0, 0);
            this._main.Controls.Add(this._rightPanel, 1, 0);
            //
            // _left — 좌 컬럼 세로 스택
            //
            this._left.ColumnCount = 1;
            this._left.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._left.RowCount = 7;
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));   // 카메라 ID 행
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));   // PARAM 섹션바
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));   // 파라미터 그리드(채움)
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));   // MIL DCF 행
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));   // 조명 섹션바
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 170F));  // 조명 그리드
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));   // 조명 상태
            this._left.Dock = System.Windows.Forms.DockStyle.Fill;
            this._left.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this._left.Name = "_left";
            this._left.Controls.Add(this._camRow, 0, 0);
            this._left.Controls.Add(this._secParam, 0, 1);
            this._left.Controls.Add(this._paramGrid, 0, 2);
            this._left.Controls.Add(this._milRow, 0, 3);
            this._left.Controls.Add(this._lblLightAssign, 0, 4);
            this._left.Controls.Add(this._gridLightAssign, 0, 5);
            this._left.Controls.Add(this._lblLightStatus, 0, 6);
            //
            // _camRow — 카메라 ID + 검색 (툴바)
            //
            this._camRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._camRow.Margin = new System.Windows.Forms.Padding(0);
            this._camRow.Name = "_camRow";
            this._camRow.Controls.Add(this._lblCamId);
            this._camRow.Controls.Add(this._cbCameraId);
            this._camRow.Controls.Add(this._btnDiscover);
            //
            // _lblCamId
            //
            this._lblCamId.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._lblCamId.Location = new System.Drawing.Point(4, 9);
            this._lblCamId.Name = "_lblCamId";
            this._lblCamId.Size = new System.Drawing.Size(90, 22);
            this._lblCamId.TabIndex = 0;
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
            this._btnDiscover.Text = "카메라 검색";
            this._btnDiscover.UseVisualStyleBackColor = false;
            this._btnDiscover.Click += new System.EventHandler(this.OnDiscoverClick);
            //
            // _secParam — "CAMERA PARAMETERS" 섹션바(주황)
            //
            this._secParam.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._secParam.Dock = System.Windows.Forms.DockStyle.Fill;
            this._secParam.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._secParam.ForeColor = System.Drawing.Color.White;
            this._secParam.Margin = new System.Windows.Forms.Padding(0);
            this._secParam.Name = "_secParam";
            this._secParam.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._secParam.TabIndex = 3;
            this._secParam.Text = "CAMERA PARAMETERS";
            this._secParam.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _paramGrid — 카메라 파라미터 리스트(채움)
            //
            this._paramGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._paramGrid.Margin = new System.Windows.Forms.Padding(0);
            this._paramGrid.Name = "_paramGrid";
            this._paramGrid.TabIndex = 4;
            //
            // _milRow — MIL DCF (조건부 표시)
            //
            this._milRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._milRow.Margin = new System.Windows.Forms.Padding(0);
            this._milRow.Name = "_milRow";
            this._milRow.Controls.Add(this._chkMilDcf);
            this._milRow.Controls.Add(this._lblMil);
            this._milRow.Controls.Add(this._txtMilDcf);
            this._milRow.Controls.Add(this._btnMilBrowse);
            //
            // _chkMilDcf
            //
            this._chkMilDcf.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._chkMilDcf.Location = new System.Drawing.Point(4, 4);
            this._chkMilDcf.Name = "_chkMilDcf";
            this._chkMilDcf.Size = new System.Drawing.Size(200, 22);
            this._chkMilDcf.TabIndex = 24;
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
            this._btnMilBrowse.Text = "찾기";
            this._btnMilBrowse.UseVisualStyleBackColor = false;
            this._btnMilBrowse.Visible = false;
            this._btnMilBrowse.Click += new System.EventHandler(this.OnMilBrowseClick);
            //
            // _lblLightAssign — 조명 지정 섹션바(주황)
            //
            this._lblLightAssign.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._lblLightAssign.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblLightAssign.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this._lblLightAssign.ForeColor = System.Drawing.Color.White;
            this._lblLightAssign.Margin = new System.Windows.Forms.Padding(0);
            this._lblLightAssign.Name = "_lblLightAssign";
            this._lblLightAssign.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblLightAssign.TabIndex = 27;
            this._lblLightAssign.Text = "조명 컨트롤러/페이지 지정 (이 모듈 = 카메라 + 조명, 채널 레벨은 [레시피])";
            this._lblLightAssign.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
            this._gridLightAssign.Dock = System.Windows.Forms.DockStyle.Fill;
            this._gridLightAssign.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._gridLightAssign.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this._gridLightAssign.Name = "_gridLightAssign";
            this._gridLightAssign.RowHeadersVisible = false;
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
            this._lblLightStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblLightStatus.Font = new System.Drawing.Font("Consolas", 9F);
            this._lblLightStatus.ForeColor = System.Drawing.Color.DarkSlateGray;
            this._lblLightStatus.Margin = new System.Windows.Forms.Padding(0);
            this._lblLightStatus.Name = "_lblLightStatus";
            this._lblLightStatus.TabIndex = 29;
            //
            // _rightPanel — 우측 컬럼 (버튼 그리드 Top + 상태 Top + 미리보기 Fill)
            //
            this._rightPanel.Controls.Add(this._picPreview);
            this._rightPanel.Controls.Add(this._lblStatus);
            this._rightPanel.Controls.Add(this._btnGrid);
            this._rightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._rightPanel.Margin = new System.Windows.Forms.Padding(0);
            this._rightPanel.Name = "_rightPanel";
            this._rightPanel.TabIndex = 30;
            //
            // _btnGrid — 액션 버튼 4×2 균일 그리드(TableLayout, 미리보기 위)
            //
            this._btnGrid.ColumnCount = 4;
            this._btnGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this._btnGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this._btnGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this._btnGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this._btnGrid.RowCount = 2;
            this._btnGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this._btnGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this._btnGrid.Dock = System.Windows.Forms.DockStyle.Top;
            this._btnGrid.Height = 80;
            this._btnGrid.Margin = new System.Windows.Forms.Padding(0);
            this._btnGrid.Name = "_btnGrid";
            this._btnGrid.Controls.Add(this._btnSave, 0, 0);
            this._btnGrid.Controls.Add(this._btnCancel, 1, 0);
            this._btnGrid.Controls.Add(this._btnReset, 2, 0);
            this._btnGrid.Controls.Add(this._btnApply, 3, 0);
            this._btnGrid.Controls.Add(this._btnConnect, 0, 1);
            this._btnGrid.Controls.Add(this._btnTestGrab, 1, 1);
            this._btnGrid.Controls.Add(this._btnLiveStart, 2, 1);
            this._btnGrid.Controls.Add(this._btnLiveStop, 3, 1);
            //
            // _btnSave
            //
            this._btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSave.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnSave.ForeColor = System.Drawing.Color.White;
            this._btnSave.Margin = new System.Windows.Forms.Padding(3);
            this._btnSave.Name = "_btnSave";
            this._btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
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
            this._btnCancel.Margin = new System.Windows.Forms.Padding(3);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Dock = System.Windows.Forms.DockStyle.Fill;
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
            this._btnReset.Margin = new System.Windows.Forms.Padding(3);
            this._btnReset.Name = "_btnReset";
            this._btnReset.Dock = System.Windows.Forms.DockStyle.Fill;
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
            this._btnApply.Margin = new System.Windows.Forms.Padding(3);
            this._btnApply.Name = "_btnApply";
            this._btnApply.Dock = System.Windows.Forms.DockStyle.Fill;
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
            this._btnTestGrab.Margin = new System.Windows.Forms.Padding(3);
            this._btnTestGrab.Name = "_btnTestGrab";
            this._btnTestGrab.Dock = System.Windows.Forms.DockStyle.Fill;
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
            this._btnConnect.Margin = new System.Windows.Forms.Padding(3);
            this._btnConnect.Name = "_btnConnect";
            this._btnConnect.Dock = System.Windows.Forms.DockStyle.Fill;
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
            this._btnLiveStart.Margin = new System.Windows.Forms.Padding(3);
            this._btnLiveStart.Name = "_btnLiveStart";
            this._btnLiveStart.Dock = System.Windows.Forms.DockStyle.Fill;
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
            this._btnLiveStop.Margin = new System.Windows.Forms.Padding(3);
            this._btnLiveStop.Name = "_btnLiveStop";
            this._btnLiveStop.Dock = System.Windows.Forms.DockStyle.Fill;
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
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(430, 24);
            this._lblStatus.TabIndex = 1;
            //
            // _picPreview — 남는 공간 전부 채움
            //
            this._picPreview.BackColor = System.Drawing.Color.Black;
            this._picPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._picPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this._picPreview.Name = "_picPreview";
            this._picPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this._picPreview.TabIndex = 2;
            this._picPreview.TabStop = false;
            //
            // CameraMappingPanel
            //
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.Controls.Add(this._lblAlgorithm);
            this.Controls.Add(this._body);
            this.Controls.SetChildIndex(this._body, 0);
            this.Name = "CameraMappingPanel";
            this.Size = new System.Drawing.Size(1140, 595);
            this._camRow.ResumeLayout(false);
            this._milRow.ResumeLayout(false);
            this._milRow.PerformLayout();
            this._left.ResumeLayout(false);
            this._btnGrid.ResumeLayout(false);
            this._rightPanel.ResumeLayout(false);
            this._rightPanel.PerformLayout();
            this._main.ResumeLayout(false);
            this._body.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._gridLightAssign)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._picPreview)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
