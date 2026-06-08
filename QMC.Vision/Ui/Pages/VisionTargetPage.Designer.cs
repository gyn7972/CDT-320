using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    partial class VisionTargetPage
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel _root;
        private TableLayoutPanel _main;
        private Label _lblStatus;
        // 좌
        private TableLayoutPanel _left;
        private Label _secCam;
        private CameraView _cam;
        private Label _secMatch;
        private DataGridView _result;
        // 중 (ACTION 전체)
        private TableLayoutPanel _center;
        private Label _secAction;
        private TableLayoutPanel _actionPanel;
        private Button _btnGrab, _btnMatch, _btnTrain, _btnLoad, _btnSaveImg, _btnEditSearch, _btnEditTrain;
        // 우 (PARAMETERS + 조명 + 라이브튜닝 — JOG/SPEED 교체)
        private TableLayoutPanel _right;
        private Label _secParam;
        private ParameterGridControl _params;
        private Panel _lightHost;      // 런타임: InspectionLightPanel(alg, settingId)
        private Label _secLive;
        private Panel _liveHost;       // 런타임: LightLiveTuningPanel

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._root = new System.Windows.Forms.TableLayoutPanel();
            this._main = new System.Windows.Forms.TableLayoutPanel();
            this._left = new System.Windows.Forms.TableLayoutPanel();
            this._secCam = new System.Windows.Forms.Label();
            this._cam = new QMC.Vision.Ui.Controls.CameraView();
            this._secMatch = new System.Windows.Forms.Label();
            this._result = new System.Windows.Forms.DataGridView();
            this._center = new System.Windows.Forms.TableLayoutPanel();
            this._secAction = new System.Windows.Forms.Label();
            this._actionPanel = new System.Windows.Forms.TableLayoutPanel();
            this._btnGrab = new System.Windows.Forms.Button();
            this._btnMatch = new System.Windows.Forms.Button();
            this._btnTrain = new System.Windows.Forms.Button();
            this._btnLoad = new System.Windows.Forms.Button();
            this._btnSaveImg = new System.Windows.Forms.Button();
            this._btnEditSearch = new System.Windows.Forms.Button();
            this._btnEditTrain = new System.Windows.Forms.Button();
            this._right = new System.Windows.Forms.TableLayoutPanel();
            this._secParam = new System.Windows.Forms.Label();
            this._params = new QMC.Vision.Ui.Controls.ParameterGridControl();
            this._lightHost = new System.Windows.Forms.Panel();
            this._secLive = new System.Windows.Forms.Label();
            this._liveHost = new System.Windows.Forms.Panel();
            this._lblStatus = new System.Windows.Forms.Label();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._secLight = new System.Windows.Forms.Label();
            this._root.SuspendLayout();
            this._main.SuspendLayout();
            this._left.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._result)).BeginInit();
            this._center.SuspendLayout();
            this._actionPanel.SuspendLayout();
            this._right.SuspendLayout();
            this.SuspendLayout();
            // 
            // _root
            // 
            this._root.ColumnCount = 1;
            this._root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._root.Controls.Add(this._main, 0, 0);
            this._root.Controls.Add(this._lblStatus, 0, 1);
            this._root.Dock = System.Windows.Forms.DockStyle.Fill;
            this._root.Location = new System.Drawing.Point(0, 0);
            this._root.Margin = new System.Windows.Forms.Padding(0);
            this._root.Name = "_root";
            this._root.RowCount = 2;
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._root.Size = new System.Drawing.Size(1710, 832);
            this._root.TabIndex = 0;
            // 
            // _main
            // 
            this._main.ColumnCount = 3;
            this._main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this._main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this._main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 380F));
            this._main.Controls.Add(this._left, 0, 0);
            this._main.Controls.Add(this._center, 1, 0);
            this._main.Controls.Add(this._right, 2, 0);
            this._main.Dock = System.Windows.Forms.DockStyle.Fill;
            this._main.Location = new System.Drawing.Point(0, 0);
            this._main.Margin = new System.Windows.Forms.Padding(0);
            this._main.Name = "_main";
            this._main.RowCount = 1;
            this._main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._main.Size = new System.Drawing.Size(1710, 808);
            this._main.TabIndex = 0;
            // 
            // _left
            // 
            this._left.ColumnCount = 1;
            this._left.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._left.Controls.Add(this._secCam, 0, 0);
            this._left.Controls.Add(this._cam, 0, 1);
            this._left.Controls.Add(this._secMatch, 0, 2);
            this._left.Controls.Add(this._result, 0, 3);
            this._left.Dock = System.Windows.Forms.DockStyle.Fill;
            this._left.Location = new System.Drawing.Point(0, 0);
            this._left.Margin = new System.Windows.Forms.Padding(0);
            this._left.Name = "_left";
            this._left.RowCount = 4;
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this._left.Size = new System.Drawing.Size(775, 808);
            this._left.TabIndex = 0;
            // 
            // _secCam
            // 
            this._secCam.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._secCam.Dock = System.Windows.Forms.DockStyle.Fill;
            this._secCam.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._secCam.ForeColor = System.Drawing.Color.White;
            this._secCam.Location = new System.Drawing.Point(3, 0);
            this._secCam.Name = "_secCam";
            this._secCam.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._secCam.Size = new System.Drawing.Size(769, 24);
            this._secCam.TabIndex = 0;
            this._secCam.Text = "CAMERA";
            this._secCam.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cam
            // 
            this._cam.BackColor = System.Drawing.Color.Black;
            this._cam.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cam.InfoText = "STAGE\r\nW:640 H:480";
            this._cam.Location = new System.Drawing.Point(3, 27);
            this._cam.Name = "_cam";
            this._cam.ShowCrosshair = true;
            this._cam.ShowLiveLabel = true;
            this._cam.Size = new System.Drawing.Size(769, 604);
            this._cam.TabIndex = 1;
            // 
            // _secMatch
            // 
            this._secMatch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._secMatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this._secMatch.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._secMatch.ForeColor = System.Drawing.Color.White;
            this._secMatch.Location = new System.Drawing.Point(3, 634);
            this._secMatch.Name = "_secMatch";
            this._secMatch.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._secMatch.Size = new System.Drawing.Size(769, 24);
            this._secMatch.TabIndex = 2;
            this._secMatch.Text = "MATCH RESULT";
            this._secMatch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _result
            // 
            this._result.AllowUserToAddRows = false;
            this._result.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._result.BackgroundColor = System.Drawing.Color.White;
            this._result.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn3,
            this.dataGridViewTextBoxColumn4,
            this.dataGridViewTextBoxColumn5});
            this._result.Dock = System.Windows.Forms.DockStyle.Fill;
            this._result.Font = new System.Drawing.Font("Consolas", 10F);
            this._result.Location = new System.Drawing.Point(3, 661);
            this._result.Name = "_result";
            this._result.RowHeadersVisible = false;
            this._result.Size = new System.Drawing.Size(769, 144);
            this._result.TabIndex = 3;
            // 
            // _center
            // 
            this._center.ColumnCount = 1;
            this._center.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._center.Controls.Add(this._secAction, 0, 0);
            this._center.Controls.Add(this._actionPanel, 0, 1);
            this._center.Dock = System.Windows.Forms.DockStyle.Fill;
            this._center.Location = new System.Drawing.Point(775, 0);
            this._center.Margin = new System.Windows.Forms.Padding(0);
            this._center.Name = "_center";
            this._center.RowCount = 2;
            this._center.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._center.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._center.Size = new System.Drawing.Size(554, 808);
            this._center.TabIndex = 1;
            // 
            // _secAction
            // 
            this._secAction.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._secAction.Dock = System.Windows.Forms.DockStyle.Fill;
            this._secAction.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._secAction.ForeColor = System.Drawing.Color.White;
            this._secAction.Location = new System.Drawing.Point(3, 0);
            this._secAction.Name = "_secAction";
            this._secAction.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._secAction.Size = new System.Drawing.Size(548, 24);
            this._secAction.TabIndex = 0;
            this._secAction.Text = "ACTION";
            this._secAction.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _actionPanel
            // 
            this._actionPanel.ColumnCount = 3;
            this._actionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this._actionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this._actionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this._actionPanel.Controls.Add(this._btnGrab, 0, 0);
            this._actionPanel.Controls.Add(this._btnMatch, 1, 0);
            this._actionPanel.Controls.Add(this._btnTrain, 2, 0);
            this._actionPanel.Controls.Add(this._btnLoad, 0, 1);
            this._actionPanel.Controls.Add(this._btnSaveImg, 1, 1);
            this._actionPanel.Controls.Add(this._btnEditSearch, 2, 1);
            this._actionPanel.Controls.Add(this._btnEditTrain, 0, 2);
            this._actionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._actionPanel.Location = new System.Drawing.Point(3, 27);
            this._actionPanel.Name = "_actionPanel";
            this._actionPanel.RowCount = 3;
            this._actionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this._actionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this._actionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this._actionPanel.Size = new System.Drawing.Size(548, 778);
            this._actionPanel.TabIndex = 1;
            // 
            // _btnGrab
            // 
            this._btnGrab.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnGrab.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnGrab.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnGrab.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnGrab.ForeColor = System.Drawing.Color.White;
            this._btnGrab.Location = new System.Drawing.Point(3, 3);
            this._btnGrab.Name = "_btnGrab";
            this._btnGrab.Size = new System.Drawing.Size(176, 253);
            this._btnGrab.TabIndex = 0;
            this._btnGrab.Text = "GRAB";
            this._btnGrab.UseVisualStyleBackColor = false;
            this._btnGrab.Click += new System.EventHandler(this.OnGrabClick);
            // 
            // _btnMatch
            // 
            this._btnMatch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnMatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMatch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMatch.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnMatch.ForeColor = System.Drawing.Color.White;
            this._btnMatch.Location = new System.Drawing.Point(185, 3);
            this._btnMatch.Name = "_btnMatch";
            this._btnMatch.Size = new System.Drawing.Size(176, 253);
            this._btnMatch.TabIndex = 1;
            this._btnMatch.Text = "MATCH";
            this._btnMatch.UseVisualStyleBackColor = false;
            this._btnMatch.Click += new System.EventHandler(this.OnMatchClick);
            // 
            // _btnTrain
            // 
            this._btnTrain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnTrain.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnTrain.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnTrain.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnTrain.ForeColor = System.Drawing.Color.White;
            this._btnTrain.Location = new System.Drawing.Point(367, 3);
            this._btnTrain.Name = "_btnTrain";
            this._btnTrain.Size = new System.Drawing.Size(178, 253);
            this._btnTrain.TabIndex = 2;
            this._btnTrain.Text = "TRAIN";
            this._btnTrain.UseVisualStyleBackColor = false;
            this._btnTrain.Click += new System.EventHandler(this.OnTrainClick);
            // 
            // _btnLoad
            // 
            this._btnLoad.BackColor = System.Drawing.Color.White;
            this._btnLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnLoad.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnLoad.ForeColor = System.Drawing.Color.Black;
            this._btnLoad.Location = new System.Drawing.Point(3, 262);
            this._btnLoad.Name = "_btnLoad";
            this._btnLoad.Size = new System.Drawing.Size(176, 253);
            this._btnLoad.TabIndex = 3;
            this._btnLoad.Text = "LOAD";
            this._btnLoad.UseVisualStyleBackColor = false;
            this._btnLoad.Click += new System.EventHandler(this.OnLoadClick);
            // 
            // _btnSaveImg
            // 
            this._btnSaveImg.BackColor = System.Drawing.Color.White;
            this._btnSaveImg.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnSaveImg.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSaveImg.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnSaveImg.ForeColor = System.Drawing.Color.Black;
            this._btnSaveImg.Location = new System.Drawing.Point(185, 262);
            this._btnSaveImg.Name = "_btnSaveImg";
            this._btnSaveImg.Size = new System.Drawing.Size(176, 253);
            this._btnSaveImg.TabIndex = 4;
            this._btnSaveImg.Text = "이미지 저장";
            this._btnSaveImg.UseVisualStyleBackColor = false;
            this._btnSaveImg.Click += new System.EventHandler(this.OnSaveImageClick);
            // 
            // _btnEditSearch
            // 
            this._btnEditSearch.BackColor = System.Drawing.Color.LightYellow;
            this._btnEditSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnEditSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnEditSearch.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnEditSearch.ForeColor = System.Drawing.Color.Black;
            this._btnEditSearch.Location = new System.Drawing.Point(367, 262);
            this._btnEditSearch.Name = "_btnEditSearch";
            this._btnEditSearch.Size = new System.Drawing.Size(178, 253);
            this._btnEditSearch.TabIndex = 5;
            this._btnEditSearch.Text = "EDIT SEARCH ROI";
            this._btnEditSearch.UseVisualStyleBackColor = false;
            this._btnEditSearch.Click += new System.EventHandler(this.OnEditSearchClick);
            // 
            // _btnEditTrain
            // 
            this._btnEditTrain.BackColor = System.Drawing.Color.LightYellow;
            this._btnEditTrain.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnEditTrain.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnEditTrain.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnEditTrain.ForeColor = System.Drawing.Color.Black;
            this._btnEditTrain.Location = new System.Drawing.Point(3, 521);
            this._btnEditTrain.Name = "_btnEditTrain";
            this._btnEditTrain.Size = new System.Drawing.Size(176, 254);
            this._btnEditTrain.TabIndex = 6;
            this._btnEditTrain.Text = "EDIT TRAIN ROI";
            this._btnEditTrain.UseVisualStyleBackColor = false;
            this._btnEditTrain.Click += new System.EventHandler(this.OnEditTrainClick);
            // 
            // _right
            // 
            this._right.ColumnCount = 1;
            this._right.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._right.Controls.Add(this._secParam, 0, 0);
            this._right.Controls.Add(this._params, 0, 1);
            this._right.Controls.Add(this._secLight, 0, 2);
            this._right.Controls.Add(this._lightHost, 0, 3);
            this._right.Controls.Add(this._secLive, 0, 4);
            this._right.Controls.Add(this._liveHost, 0, 5);
            this._right.Dock = System.Windows.Forms.DockStyle.Fill;
            this._right.Location = new System.Drawing.Point(1329, 0);
            this._right.Margin = new System.Windows.Forms.Padding(0);
            this._right.Name = "_right";
            this._right.RowCount = 6;
            this._right.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._right.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._right.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._right.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 240F));
            this._right.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._right.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this._right.Size = new System.Drawing.Size(381, 808);
            this._right.TabIndex = 2;
            // 
            // _secParam
            // 
            this._secParam.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._secParam.Dock = System.Windows.Forms.DockStyle.Fill;
            this._secParam.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._secParam.ForeColor = System.Drawing.Color.White;
            this._secParam.Location = new System.Drawing.Point(3, 0);
            this._secParam.Name = "_secParam";
            this._secParam.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._secParam.Size = new System.Drawing.Size(375, 24);
            this._secParam.TabIndex = 0;
            this._secParam.Text = "PARAMETERS";
            this._secParam.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _params
            // 
            this._params.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this._params.Dock = System.Windows.Forms.DockStyle.Fill;
            this._params.Location = new System.Drawing.Point(0, 24);
            this._params.Margin = new System.Windows.Forms.Padding(0);
            this._params.Name = "_params";
            this._params.Size = new System.Drawing.Size(381, 296);
            this._params.TabIndex = 1;
            // 
            // _lightHost
            // 
            this._lightHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._lightHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lightHost.Location = new System.Drawing.Point(3, 347);
            this._lightHost.Name = "_lightHost";
            this._lightHost.Size = new System.Drawing.Size(375, 234);
            this._lightHost.TabIndex = 3;
            // 
            // _secLive
            // 
            this._secLive.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._secLive.Dock = System.Windows.Forms.DockStyle.Fill;
            this._secLive.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._secLive.ForeColor = System.Drawing.Color.White;
            this._secLive.Location = new System.Drawing.Point(3, 584);
            this._secLive.Name = "_secLive";
            this._secLive.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._secLive.Size = new System.Drawing.Size(375, 24);
            this._secLive.TabIndex = 4;
            this._secLive.Text = "라이브 튜닝";
            this._secLive.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _liveHost
            // 
            this._liveHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._liveHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._liveHost.Location = new System.Drawing.Point(3, 611);
            this._liveHost.Name = "_liveHost";
            this._liveHost.Size = new System.Drawing.Size(375, 194);
            this._liveHost.TabIndex = 5;
            // 
            // _lblStatus
            // 
            this._lblStatus.BackColor = System.Drawing.Color.WhiteSmoke;
            this._lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblStatus.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblStatus.ForeColor = System.Drawing.Color.DarkSlateGray;
            this._lblStatus.Location = new System.Drawing.Point(3, 808);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblStatus.Size = new System.Drawing.Size(1704, 24);
            this._lblStatus.TabIndex = 1;
            this._lblStatus.Text = "Ready.";
            this._lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "Idx";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.HeaderText = "X";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.HeaderText = "Y";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.HeaderText = "Angle";
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.HeaderText = "Score";
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            // 
            // _secLight
            // 
            this._secLight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._secLight.Dock = System.Windows.Forms.DockStyle.Fill;
            this._secLight.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._secLight.ForeColor = System.Drawing.Color.White;
            this._secLight.Location = new System.Drawing.Point(3, 320);
            this._secLight.Name = "_secLight";
            this._secLight.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._secLight.Size = new System.Drawing.Size(375, 24);
            this._secLight.TabIndex = 2;
            this._secLight.Text = "검사 조명";
            this._secLight.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // VisionTargetPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.Controls.Add(this._root);
            this.Name = "VisionTargetPage";
            this.Size = new System.Drawing.Size(1710, 832);
            this._root.ResumeLayout(false);
            this._main.ResumeLayout(false);
            this._left.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._result)).EndInit();
            this._center.ResumeLayout(false);
            this._actionPanel.ResumeLayout(false);
            this._right.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private Label _secLight;
    }
}
