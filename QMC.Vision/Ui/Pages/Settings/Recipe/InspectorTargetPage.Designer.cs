using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    partial class InspectorTargetPage
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel _root;
        private TableLayoutPanel _main;
        private Label _lblStatus;
        // 좌 (카메라 전용 — 확대)
        private TableLayoutPanel _left;
        private Label _secCam;
        private CameraView _cam;
        // 중 (ACTION 콤팩트 + 검사결과 + verdict)
        private TableLayoutPanel _center;
        private Label _secAction;
        private TableLayoutPanel _actionPanel;
        private Button _btnInspect, _btnEditRoi;
        private Label _secResult;
        private DataGridView _result;
        private Label _lblVerdict;
        // 우 (PARAMETERS + 조명)
        private TableLayoutPanel _right;
        private Label _secParam;
        private ParameterGridControl _params;
        private Label _secRoi;
        private Panel _roiHost;     // ROI 미세조정 컨트롤(런타임 채움) — 단일 Inspection ROI
        private Label _secLight;
        private Panel _lightHost;

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
            this._center = new System.Windows.Forms.TableLayoutPanel();
            this._secAction = new System.Windows.Forms.Label();
            this._actionPanel = new System.Windows.Forms.TableLayoutPanel();
            this._btnInspect = new System.Windows.Forms.Button();
            this._btnEditRoi = new System.Windows.Forms.Button();
            this._secRoi = new System.Windows.Forms.Label();
            this._roiHost = new System.Windows.Forms.Panel();
            this._secResult = new System.Windows.Forms.Label();
            this._result = new System.Windows.Forms.DataGridView();
            this._lblVerdict = new System.Windows.Forms.Label();
            this._right = new System.Windows.Forms.TableLayoutPanel();
            this._secParam = new System.Windows.Forms.Label();
            this._params = new QMC.Vision.Ui.Controls.ParameterGridControl();
            this._secLight = new System.Windows.Forms.Label();
            this._lightHost = new System.Windows.Forms.Panel();
            this._lblStatus = new System.Windows.Forms.Label();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._root.SuspendLayout();
            this._main.SuspendLayout();
            this._left.SuspendLayout();
            this._center.SuspendLayout();
            this._actionPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._result)).BeginInit();
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
            this._left.Dock = System.Windows.Forms.DockStyle.Fill;
            this._left.Location = new System.Drawing.Point(0, 0);
            this._left.Margin = new System.Windows.Forms.Padding(0);
            this._left.Name = "_left";
            this._left.RowCount = 2;
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._left.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
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
            this._cam.BackColor = System.Drawing.Color.DimGray;
            this._cam.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cam.InfoForeColor = System.Drawing.Color.LightGreen;
            this._cam.InfoText = "STAGE\r\nW:640 H:480";
            this._cam.Location = new System.Drawing.Point(3, 27);
            this._cam.MmPerPixelX = 0D;
            this._cam.MmPerPixelY = 0D;
            this._cam.Name = "_cam";
            this._cam.ShowCrosshair = true;
            this._cam.ShowCursorReadout = false;
            this._cam.ShowLiveLabel = true;
            this._cam.ShowToolbar = false;
            this._cam.Size = new System.Drawing.Size(769, 778);
            this._cam.TabIndex = 1;
            // 
            // _center
            // 
            this._center.ColumnCount = 1;
            this._center.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._center.Controls.Add(this._secAction, 0, 0);
            this._center.Controls.Add(this._actionPanel, 0, 1);
            this._center.Controls.Add(this._secRoi, 0, 2);
            this._center.Controls.Add(this._roiHost, 0, 3);
            this._center.Controls.Add(this._secResult, 0, 4);
            this._center.Controls.Add(this._result, 0, 5);
            this._center.Controls.Add(this._lblVerdict, 0, 6);
            this._center.Dock = System.Windows.Forms.DockStyle.Fill;
            this._center.Location = new System.Drawing.Point(775, 0);
            this._center.Margin = new System.Windows.Forms.Padding(0);
            this._center.Name = "_center";
            this._center.RowCount = 7;
            this._center.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._center.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this._center.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._center.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this._center.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._center.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._center.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
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
            this._actionPanel.ColumnCount = 2;
            this._actionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._actionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._actionPanel.Controls.Add(this._btnInspect, 0, 0);
            this._actionPanel.Controls.Add(this._btnEditRoi, 0, 1);
            this._actionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._actionPanel.Location = new System.Drawing.Point(0, 24);
            this._actionPanel.Margin = new System.Windows.Forms.Padding(0);
            this._actionPanel.Name = "_actionPanel";
            this._actionPanel.RowCount = 2;
            this._actionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this._actionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this._actionPanel.Size = new System.Drawing.Size(554, 100);
            this._actionPanel.TabIndex = 1;
            // 
            // _btnInspect
            // 
            this._btnInspect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._actionPanel.SetColumnSpan(this._btnInspect, 2);
            this._btnInspect.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnInspect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnInspect.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._btnInspect.ForeColor = System.Drawing.Color.White;
            this._btnInspect.Location = new System.Drawing.Point(3, 3);
            this._btnInspect.Name = "_btnInspect";
            this._btnInspect.Size = new System.Drawing.Size(548, 46);
            this._btnInspect.TabIndex = 0;
            this._btnInspect.Text = "INSPECT";
            this._btnInspect.UseVisualStyleBackColor = false;
            this._btnInspect.Click += new System.EventHandler(this.OnInspectClick);
            // 
            // _btnEditRoi
            // 
            this._btnEditRoi.BackColor = System.Drawing.Color.LightYellow;
            this._actionPanel.SetColumnSpan(this._btnEditRoi, 2);
            this._btnEditRoi.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnEditRoi.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnEditRoi.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._btnEditRoi.ForeColor = System.Drawing.Color.Black;
            this._btnEditRoi.Location = new System.Drawing.Point(3, 55);
            this._btnEditRoi.Name = "_btnEditRoi";
            this._btnEditRoi.Size = new System.Drawing.Size(548, 42);
            this._btnEditRoi.TabIndex = 1;
            this._btnEditRoi.Text = "EDIT INSPECTION ROI";
            this._btnEditRoi.UseVisualStyleBackColor = false;
            this._btnEditRoi.Click += new System.EventHandler(this.OnEditRoiClick);
            // 
            // _secRoi
            // 
            this._secRoi.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._secRoi.Dock = System.Windows.Forms.DockStyle.Fill;
            this._secRoi.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._secRoi.ForeColor = System.Drawing.Color.White;
            this._secRoi.Location = new System.Drawing.Point(3, 124);
            this._secRoi.Name = "_secRoi";
            this._secRoi.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._secRoi.Size = new System.Drawing.Size(548, 24);
            this._secRoi.TabIndex = 2;
            this._secRoi.Text = "ROI 제어";
            this._secRoi.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _roiHost
            // 
            this._roiHost.AutoScroll = true;
            this._roiHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._roiHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._roiHost.Location = new System.Drawing.Point(3, 151);
            this._roiHost.Name = "_roiHost";
            this._roiHost.Size = new System.Drawing.Size(548, 144);
            this._roiHost.TabIndex = 3;
            // 
            // _secResult
            // 
            this._secResult.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._secResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this._secResult.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._secResult.ForeColor = System.Drawing.Color.White;
            this._secResult.Location = new System.Drawing.Point(3, 298);
            this._secResult.Name = "_secResult";
            this._secResult.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._secResult.Size = new System.Drawing.Size(548, 24);
            this._secResult.TabIndex = 4;
            this._secResult.Text = "검사 결과";
            this._secResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _result
            // 
            this._result.AllowUserToAddRows = false;
            this._result.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._result.BackgroundColor = System.Drawing.Color.White;
            this._result.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn3});
            this._result.Dock = System.Windows.Forms.DockStyle.Fill;
            this._result.Font = new System.Drawing.Font("Consolas", 10F);
            this._result.Location = new System.Drawing.Point(3, 325);
            this._result.Name = "_result";
            this._result.RowHeadersVisible = false;
            this._result.Size = new System.Drawing.Size(548, 237);
            this._result.TabIndex = 5;
            // 
            // _lblVerdict
            // 
            this._lblVerdict.BackColor = System.Drawing.Color.Gray;
            this._lblVerdict.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblVerdict.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._lblVerdict.ForeColor = System.Drawing.Color.White;
            this._lblVerdict.Location = new System.Drawing.Point(3, 565);
            this._lblVerdict.Name = "_lblVerdict";
            this._lblVerdict.Size = new System.Drawing.Size(548, 243);
            this._lblVerdict.TabIndex = 6;
            this._lblVerdict.Text = "-";
            this._lblVerdict.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _right
            // 
            this._right.ColumnCount = 1;
            this._right.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._right.Controls.Add(this._secParam, 0, 0);
            this._right.Controls.Add(this._params, 0, 1);
            this._right.Controls.Add(this._secLight, 0, 2);
            this._right.Controls.Add(this._lightHost, 0, 3);
            this._right.Dock = System.Windows.Forms.DockStyle.Fill;
            this._right.Location = new System.Drawing.Point(1329, 0);
            this._right.Margin = new System.Windows.Forms.Padding(0);
            this._right.Name = "_right";
            this._right.RowCount = 4;
            this._right.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._right.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._right.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._right.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 440F));
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
            this._params.Size = new System.Drawing.Size(381, 320);
            this._params.TabIndex = 1;
            // 
            // _secLight
            // 
            this._secLight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._secLight.Dock = System.Windows.Forms.DockStyle.Fill;
            this._secLight.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._secLight.ForeColor = System.Drawing.Color.White;
            this._secLight.Location = new System.Drawing.Point(3, 344);
            this._secLight.Name = "_secLight";
            this._secLight.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._secLight.Size = new System.Drawing.Size(375, 24);
            this._secLight.TabIndex = 2;
            this._secLight.Text = "검사 조명";
            this._secLight.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lightHost
            // 
            this._lightHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._lightHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lightHost.Location = new System.Drawing.Point(3, 371);
            this._lightHost.Name = "_lightHost";
            this._lightHost.Size = new System.Drawing.Size(375, 434);
            this._lightHost.TabIndex = 3;
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
            this.dataGridViewTextBoxColumn1.HeaderText = "항목";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.HeaderText = "값";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.HeaderText = "결과";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            // 
            // InspectorTargetPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.Controls.Add(this._root);
            this.Name = "InspectorTargetPage";
            this.Size = new System.Drawing.Size(1710, 832);
            this._root.ResumeLayout(false);
            this._main.ResumeLayout(false);
            this._left.ResumeLayout(false);
            this._center.ResumeLayout(false);
            this._actionPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._result)).EndInit();
            this._right.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
    }
}
