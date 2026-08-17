using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class TapeFrameSubsetPage
    {
        private System.ComponentModel.IContainer components = null;

        private ToolTip toolTipRecipeLocation;
        private TableLayoutPanel editorLayout;

        // 프리셋 바 (그룹 아님 — 화면 전체에 적용되는 사양 라이브러리 조작)
        private Panel pnlSpecPreset;
        private TableLayoutPanel tlpSpecPreset;
        private Label lblSpecLibrary;
        private ComboBox _cbSpecLibrary;
        private Button btnLoadSpec;
        private Button btnSaveSpec;
        private Label lblName;
        private TextBox _tbName;

        // ① 기준 격자(Base)
        private GroupBox grpBase;
        private TableLayoutPanel tlpBase;
        private Label lblWaferRole;
        private ComboBox _cbWaferRole;
        private Panel pnlBaseButtons;
        private Button _btnImportWaferMap;
        private Button btnGridMapCreate;
        private Label lblGridX;
        private NumericUpDown _nGridX;
        private Label lblGridY;
        private NumericUpDown _nGridY;
        private Label lblDiameter;
        private NumericUpDown _nDiameter;
        private Label lblEdgeMode;
        private ComboBox _cbEdgeSkipMode;
        private Label lblEdgeLR;
        private NumericUpDown _nSideEdgeSkip;
        private Label lblEdgeTB;
        private NumericUpDown _nTopBottomEdgeSkip;
        private Label lblMapFile;
        private Label _lblMapFileValue;
        private Label lblBaseHint;

        // ② 다이 배치
        private GroupBox grpDie;
        private TableLayoutPanel tlpDie;
        private Label lblDieSizeX;
        private NumericUpDown _nDieSizeX;
        private Label lblDieSizeY;
        private NumericUpDown _nDieSizeY;
        private Button btnEditDieSpec;
        private Label lblPitchX;
        private NumericUpDown _nPitchX;
        private Label lblPitchY;
        private NumericUpDown _nPitchY;
        private Label lblCenterPitch;
        private Label lblCenterPitchValue;
        private Label lblDieHint;

        // ③ 결과 / 승인
        private GroupBox grpResult;
        private TableLayoutPanel tlpResult;
        private Button btnGridCountPreview;
        private Label lblGridCountResult;
        private Label lblApprovalInput;
        private Label lblApprovalGood;
        private Label lblApprovalNg;

        // 우측 리포트
        private GroupBox grpMapSource;
        private TextBox _tbMapSourceInfo;

        // 미사용 보존 컨트롤(좌표 계산에 연결된 곳이 없어 화면에서 숨긴다)
        private Label lblRotate;
        private ComboBox _cbRotate;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolTipRecipeLocation = new System.Windows.Forms.ToolTip(this.components);
            this.editorLayout = new System.Windows.Forms.TableLayoutPanel();
            this.pnlSpecPreset = new System.Windows.Forms.Panel();
            this.tlpSpecPreset = new System.Windows.Forms.TableLayoutPanel();
            this.lblSpecLibrary = new System.Windows.Forms.Label();
            this._cbSpecLibrary = new System.Windows.Forms.ComboBox();
            this.btnLoadSpec = new System.Windows.Forms.Button();
            this.btnSaveSpec = new System.Windows.Forms.Button();
            this.lblName = new System.Windows.Forms.Label();
            this._tbName = new System.Windows.Forms.TextBox();
            this.grpBase = new System.Windows.Forms.GroupBox();
            this.tlpBase = new System.Windows.Forms.TableLayoutPanel();
            this.lblWaferRole = new System.Windows.Forms.Label();
            this._cbWaferRole = new System.Windows.Forms.ComboBox();
            this.pnlBaseButtons = new System.Windows.Forms.Panel();
            this._btnImportWaferMap = new System.Windows.Forms.Button();
            this.btnGridMapCreate = new System.Windows.Forms.Button();
            this.lblGridX = new System.Windows.Forms.Label();
            this._nGridX = new System.Windows.Forms.NumericUpDown();
            this.lblGridY = new System.Windows.Forms.Label();
            this._nGridY = new System.Windows.Forms.NumericUpDown();
            this.lblDiameter = new System.Windows.Forms.Label();
            this._nDiameter = new System.Windows.Forms.NumericUpDown();
            this.lblEdgeMode = new System.Windows.Forms.Label();
            this._cbEdgeSkipMode = new System.Windows.Forms.ComboBox();
            this.lblEdgeLR = new System.Windows.Forms.Label();
            this._nSideEdgeSkip = new System.Windows.Forms.NumericUpDown();
            this.lblEdgeTB = new System.Windows.Forms.Label();
            this._nTopBottomEdgeSkip = new System.Windows.Forms.NumericUpDown();
            this.lblMapFile = new System.Windows.Forms.Label();
            this._lblMapFileValue = new System.Windows.Forms.Label();
            this.lblBaseHint = new System.Windows.Forms.Label();
            this.grpDie = new System.Windows.Forms.GroupBox();
            this.tlpDie = new System.Windows.Forms.TableLayoutPanel();
            this.lblDieSizeX = new System.Windows.Forms.Label();
            this._nDieSizeX = new System.Windows.Forms.NumericUpDown();
            this.lblDieSizeY = new System.Windows.Forms.Label();
            this._nDieSizeY = new System.Windows.Forms.NumericUpDown();
            this.btnEditDieSpec = new System.Windows.Forms.Button();
            this.lblPitchX = new System.Windows.Forms.Label();
            this._nPitchX = new System.Windows.Forms.NumericUpDown();
            this.lblPitchY = new System.Windows.Forms.Label();
            this._nPitchY = new System.Windows.Forms.NumericUpDown();
            this.lblCenterPitch = new System.Windows.Forms.Label();
            this.lblCenterPitchValue = new System.Windows.Forms.Label();
            this.lblDieHint = new System.Windows.Forms.Label();
            this.grpResult = new System.Windows.Forms.GroupBox();
            this.tlpResult = new System.Windows.Forms.TableLayoutPanel();
            this.btnGridCountPreview = new System.Windows.Forms.Button();
            this.lblGridCountResult = new System.Windows.Forms.Label();
            this.lblApprovalInput = new System.Windows.Forms.Label();
            this.lblApprovalGood = new System.Windows.Forms.Label();
            this.lblApprovalNg = new System.Windows.Forms.Label();
            this.grpMapSource = new System.Windows.Forms.GroupBox();
            this._tbMapSourceInfo = new System.Windows.Forms.TextBox();
            this.lblRotate = new System.Windows.Forms.Label();
            this._cbRotate = new System.Windows.Forms.ComboBox();
            this._editorPanel.SuspendLayout();
            this.editorLayout.SuspendLayout();
            this.pnlSpecPreset.SuspendLayout();
            this.tlpSpecPreset.SuspendLayout();
            this.grpBase.SuspendLayout();
            this.tlpBase.SuspendLayout();
            this.pnlBaseButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nGridX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nGridY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDiameter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nSideEdgeSkip)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nTopBottomEdgeSkip)).BeginInit();
            this.grpDie.SuspendLayout();
            this.tlpDie.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nDieSizeX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDieSizeY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchY)).BeginInit();
            this.grpResult.SuspendLayout();
            this.tlpResult.SuspendLayout();
            this.grpMapSource.SuspendLayout();
            this.SuspendLayout();
            //
            // editorLayout
            //
            this.editorLayout.BackColor = System.Drawing.Color.White;
            this.editorLayout.ColumnCount = 2;
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.editorLayout.Controls.Add(this.pnlSpecPreset, 0, 0);
            this.editorLayout.Controls.Add(this.grpBase, 0, 1);
            this.editorLayout.Controls.Add(this.grpDie, 0, 2);
            this.editorLayout.Controls.Add(this.grpResult, 0, 3);
            this.editorLayout.Controls.Add(this.grpMapSource, 1, 0);
            this.editorLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editorLayout.Location = new System.Drawing.Point(8, 12);
            this.editorLayout.Margin = new System.Windows.Forms.Padding(0);
            this.editorLayout.Name = "editorLayout";
            this.editorLayout.RowCount = 4;
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 32F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 26F));
            this.editorLayout.SetRowSpan(this.grpMapSource, 4);
            this.editorLayout.Size = new System.Drawing.Size(1510, 678);
            this.editorLayout.TabIndex = 0;
            //
            // pnlSpecPreset
            //
            this.pnlSpecPreset.BackColor = System.Drawing.Color.White;
            this.pnlSpecPreset.Controls.Add(this.tlpSpecPreset);
            this.pnlSpecPreset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlSpecPreset.Location = new System.Drawing.Point(0, 0);
            this.pnlSpecPreset.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.pnlSpecPreset.Name = "pnlSpecPreset";
            this.pnlSpecPreset.Size = new System.Drawing.Size(755, 40);
            this.pnlSpecPreset.TabIndex = 0;
            //
            // tlpSpecPreset
            //
            this.tlpSpecPreset.ColumnCount = 6;
            this.tlpSpecPreset.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.tlpSpecPreset.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.tlpSpecPreset.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.tlpSpecPreset.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.tlpSpecPreset.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.tlpSpecPreset.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this.tlpSpecPreset.Controls.Add(this.lblSpecLibrary, 0, 0);
            this.tlpSpecPreset.Controls.Add(this._cbSpecLibrary, 1, 0);
            this.tlpSpecPreset.Controls.Add(this.btnLoadSpec, 2, 0);
            this.tlpSpecPreset.Controls.Add(this.btnSaveSpec, 3, 0);
            this.tlpSpecPreset.Controls.Add(this.lblName, 4, 0);
            this.tlpSpecPreset.Controls.Add(this._tbName, 5, 0);
            this.tlpSpecPreset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpSpecPreset.Location = new System.Drawing.Point(0, 0);
            this.tlpSpecPreset.Name = "tlpSpecPreset";
            this.tlpSpecPreset.RowCount = 1;
            this.tlpSpecPreset.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpSpecPreset.Size = new System.Drawing.Size(755, 40);
            this.tlpSpecPreset.TabIndex = 0;
            //
            // lblSpecLibrary
            //
            this.lblSpecLibrary.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblSpecLibrary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSpecLibrary.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblSpecLibrary.Location = new System.Drawing.Point(2, 2);
            this.lblSpecLibrary.Margin = new System.Windows.Forms.Padding(2);
            this.lblSpecLibrary.Name = "lblSpecLibrary";
            this.lblSpecLibrary.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblSpecLibrary.Size = new System.Drawing.Size(106, 36);
            this.lblSpecLibrary.TabIndex = 0;
            this.lblSpecLibrary.Text = "Saved spec";
            this.lblSpecLibrary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _cbSpecLibrary
            //
            this._cbSpecLibrary.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbSpecLibrary.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbSpecLibrary.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbSpecLibrary.Location = new System.Drawing.Point(112, 2);
            this._cbSpecLibrary.Margin = new System.Windows.Forms.Padding(2);
            this._cbSpecLibrary.Name = "_cbSpecLibrary";
            this._cbSpecLibrary.Size = new System.Drawing.Size(200, 25);
            this._cbSpecLibrary.TabIndex = 1;
            //
            // btnLoadSpec
            //
            this.btnLoadSpec.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoadSpec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoadSpec.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnLoadSpec.Location = new System.Drawing.Point(316, 2);
            this.btnLoadSpec.Margin = new System.Windows.Forms.Padding(2);
            this.btnLoadSpec.Name = "btnLoadSpec";
            this.btnLoadSpec.Size = new System.Drawing.Size(106, 36);
            this.btnLoadSpec.TabIndex = 2;
            this.btnLoadSpec.Text = "LOAD SPEC";
            this.toolTipRecipeLocation.SetToolTip(this.btnLoadSpec, "선택한 저장 사양을 화면 값으로만 불러옵니다. Recipe에는 SAVE를 눌러야 반영됩니다.");
            this.btnLoadSpec.UseVisualStyleBackColor = true;
            this.btnLoadSpec.Click += new System.EventHandler(this.btnLoadSpec_Click);
            //
            // btnSaveSpec
            //
            this.btnSaveSpec.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(88)))), ((int)(((byte)(31)))));
            this.btnSaveSpec.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveSpec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveSpec.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnSaveSpec.ForeColor = System.Drawing.Color.White;
            this.btnSaveSpec.Location = new System.Drawing.Point(426, 2);
            this.btnSaveSpec.Margin = new System.Windows.Forms.Padding(2);
            this.btnSaveSpec.Name = "btnSaveSpec";
            this.btnSaveSpec.Size = new System.Drawing.Size(106, 36);
            this.btnSaveSpec.TabIndex = 3;
            this.btnSaveSpec.Text = "SAVE SPEC";
            this.toolTipRecipeLocation.SetToolTip(this.btnSaveSpec, "Recipe 저장과 함께 현재 역할 값을 저장 사양 라이브러리에 기록합니다.");
            this.btnSaveSpec.UseVisualStyleBackColor = false;
            this.btnSaveSpec.Click += new System.EventHandler(this.btnSaveSpec_Click);
            //
            // lblName
            //
            this.lblName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblName.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblName.Location = new System.Drawing.Point(536, 2);
            this.lblName.Margin = new System.Windows.Forms.Padding(2);
            this.lblName.Name = "lblName";
            this.lblName.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblName.Size = new System.Drawing.Size(86, 36);
            this.lblName.TabIndex = 4;
            this.lblName.Text = "Spec name";
            this.lblName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _tbName
            //
            this._tbName.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbName.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._tbName.Location = new System.Drawing.Point(626, 2);
            this._tbName.Margin = new System.Windows.Forms.Padding(2);
            this._tbName.Name = "_tbName";
            this._tbName.Size = new System.Drawing.Size(127, 25);
            this._tbName.TabIndex = 5;
            //
            // grpBase
            //
            this.grpBase.BackColor = System.Drawing.Color.White;
            this.grpBase.Controls.Add(this.tlpBase);
            this.grpBase.Controls.Add(this.lblRotate);
            this.grpBase.Controls.Add(this._cbRotate);
            this.grpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpBase.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpBase.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(45)))), ((int)(((byte)(57)))));
            this.grpBase.Location = new System.Drawing.Point(0, 46);
            this.grpBase.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpBase.Name = "grpBase";
            this.grpBase.Padding = new System.Windows.Forms.Padding(10, 6, 10, 8);
            this.grpBase.Size = new System.Drawing.Size(755, 259);
            this.grpBase.TabIndex = 1;
            this.grpBase.TabStop = false;
            this.grpBase.Text = "① 기준 격자 (Base) — 변경하면 GRID MAP CREATE 필요";
            //
            // tlpBase
            //
            this.tlpBase.ColumnCount = 5;
            this.tlpBase.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpBase.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpBase.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tlpBase.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpBase.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 0F));
            this.tlpBase.Controls.Add(this.lblWaferRole, 0, 0);
            this.tlpBase.Controls.Add(this._cbWaferRole, 1, 0);
            this.tlpBase.Controls.Add(this.pnlBaseButtons, 2, 0);
            this.tlpBase.Controls.Add(this.lblGridX, 0, 1);
            this.tlpBase.Controls.Add(this._nGridX, 1, 1);
            this.tlpBase.Controls.Add(this.lblGridY, 2, 1);
            this.tlpBase.Controls.Add(this._nGridY, 3, 1);
            this.tlpBase.Controls.Add(this.lblDiameter, 0, 2);
            this.tlpBase.Controls.Add(this._nDiameter, 1, 2);
            this.tlpBase.Controls.Add(this.lblEdgeMode, 2, 2);
            this.tlpBase.Controls.Add(this._cbEdgeSkipMode, 3, 2);
            this.tlpBase.Controls.Add(this.lblEdgeLR, 0, 3);
            this.tlpBase.Controls.Add(this._nSideEdgeSkip, 1, 3);
            this.tlpBase.Controls.Add(this.lblEdgeTB, 2, 3);
            this.tlpBase.Controls.Add(this._nTopBottomEdgeSkip, 3, 3);
            this.tlpBase.Controls.Add(this.lblMapFile, 0, 4);
            this.tlpBase.Controls.Add(this._lblMapFileValue, 1, 4);
            this.tlpBase.Controls.Add(this.lblBaseHint, 0, 5);
            this.tlpBase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpBase.Location = new System.Drawing.Point(10, 28);
            this.tlpBase.Name = "tlpBase";
            this.tlpBase.RowCount = 6;
            this.tlpBase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tlpBase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpBase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpBase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpBase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpBase.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpBase.SetColumnSpan(this.pnlBaseButtons, 3);
            this.tlpBase.SetColumnSpan(this._lblMapFileValue, 4);
            this.tlpBase.SetColumnSpan(this.lblBaseHint, 5);
            this.tlpBase.Size = new System.Drawing.Size(735, 223);
            this.tlpBase.TabIndex = 0;
            //
            // lblWaferRole
            //
            this.lblWaferRole.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblWaferRole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferRole.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblWaferRole.Location = new System.Drawing.Point(2, 2);
            this.lblWaferRole.Margin = new System.Windows.Forms.Padding(2);
            this.lblWaferRole.Name = "lblWaferRole";
            this.lblWaferRole.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblWaferRole.Size = new System.Drawing.Size(146, 36);
            this.lblWaferRole.TabIndex = 0;
            this.lblWaferRole.Text = "Wafer role";
            this.lblWaferRole.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _cbWaferRole
            //
            this._cbWaferRole.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbWaferRole.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbWaferRole.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbWaferRole.Items.AddRange(new object[] {
            "INPUT WAFER",
            "OUTPUT WAFER"});
            this._cbWaferRole.Location = new System.Drawing.Point(152, 2);
            this._cbWaferRole.Margin = new System.Windows.Forms.Padding(2);
            this._cbWaferRole.Name = "_cbWaferRole";
            this._cbWaferRole.Size = new System.Drawing.Size(200, 25);
            this._cbWaferRole.TabIndex = 1;
            this.toolTipRecipeLocation.SetToolTip(this._cbWaferRole, "INPUT = Input Base/Die Map, OUTPUT = Output Base + GOOD/NG Bin Map 2종.");
            this._cbWaferRole.SelectedIndexChanged += new System.EventHandler(this.OnWaferRoleChanged);
            //
            // pnlBaseButtons
            //
            this.pnlBaseButtons.Controls.Add(this.btnGridMapCreate);
            this.pnlBaseButtons.Controls.Add(this._btnImportWaferMap);
            this.pnlBaseButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBaseButtons.Location = new System.Drawing.Point(356, 2);
            this.pnlBaseButtons.Margin = new System.Windows.Forms.Padding(2);
            this.pnlBaseButtons.Name = "pnlBaseButtons";
            this.pnlBaseButtons.Size = new System.Drawing.Size(377, 36);
            this.pnlBaseButtons.TabIndex = 2;
            //
            // _btnImportWaferMap
            //
            this._btnImportWaferMap.Dock = System.Windows.Forms.DockStyle.Left;
            this._btnImportWaferMap.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnImportWaferMap.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this._btnImportWaferMap.Location = new System.Drawing.Point(0, 0);
            this._btnImportWaferMap.Name = "_btnImportWaferMap";
            this._btnImportWaferMap.Size = new System.Drawing.Size(180, 36);
            this._btnImportWaferMap.TabIndex = 0;
            this._btnImportWaferMap.Text = "LOAD WAFER MAP";
            this._btnImportWaferMap.UseVisualStyleBackColor = true;
            this._btnImportWaferMap.Click += new System.EventHandler(this.btnImportWaferMap_Click);
            //
            // btnGridMapCreate
            //
            this.btnGridMapCreate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(88)))), ((int)(((byte)(31)))));
            this.btnGridMapCreate.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnGridMapCreate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGridMapCreate.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnGridMapCreate.ForeColor = System.Drawing.Color.White;
            this.btnGridMapCreate.Location = new System.Drawing.Point(187, 0);
            this.btnGridMapCreate.Name = "btnGridMapCreate";
            this.btnGridMapCreate.Size = new System.Drawing.Size(190, 36);
            this.btnGridMapCreate.TabIndex = 1;
            this.btnGridMapCreate.Text = "GRID MAP CREATE";
            this.btnGridMapCreate.UseVisualStyleBackColor = false;
            this.btnGridMapCreate.Click += new System.EventHandler(this.btnGridMapCreate_Click);
            //
            // lblGridX
            //
            this.lblGridX.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblGridX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGridX.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblGridX.Margin = new System.Windows.Forms.Padding(2);
            this.lblGridX.Name = "lblGridX";
            this.lblGridX.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblGridX.Size = new System.Drawing.Size(146, 30);
            this.lblGridX.TabIndex = 3;
            this.lblGridX.Text = "Grid X (count)";
            this.lblGridX.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _nGridX
            //
            this._nGridX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nGridX.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._nGridX.Margin = new System.Windows.Forms.Padding(2);
            this._nGridX.Maximum = new decimal(new int[] { 9999, 0, 0, 0});
            this._nGridX.Minimum = new decimal(new int[] { 1, 0, 0, 0});
            this._nGridX.Name = "_nGridX";
            this._nGridX.Size = new System.Drawing.Size(200, 25);
            this._nGridX.TabIndex = 4;
            this._nGridX.Value = new decimal(new int[] { 5, 0, 0, 0});
            //
            // lblGridY
            //
            this.lblGridY.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblGridY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGridY.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblGridY.Margin = new System.Windows.Forms.Padding(2);
            this.lblGridY.Name = "lblGridY";
            this.lblGridY.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblGridY.Size = new System.Drawing.Size(126, 30);
            this.lblGridY.TabIndex = 5;
            this.lblGridY.Text = "Grid Y (count)";
            this.lblGridY.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _nGridY
            //
            this._nGridY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nGridY.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._nGridY.Margin = new System.Windows.Forms.Padding(2);
            this._nGridY.Maximum = new decimal(new int[] { 9999, 0, 0, 0});
            this._nGridY.Minimum = new decimal(new int[] { 1, 0, 0, 0});
            this._nGridY.Name = "_nGridY";
            this._nGridY.Size = new System.Drawing.Size(200, 25);
            this._nGridY.TabIndex = 6;
            this._nGridY.Value = new decimal(new int[] { 5, 0, 0, 0});
            //
            // lblDiameter
            //
            this.lblDiameter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblDiameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDiameter.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDiameter.Margin = new System.Windows.Forms.Padding(2);
            this.lblDiameter.Name = "lblDiameter";
            this.lblDiameter.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblDiameter.Size = new System.Drawing.Size(146, 30);
            this.lblDiameter.TabIndex = 7;
            this.lblDiameter.Text = "Outer diameter (mm)";
            this.lblDiameter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _nDiameter
            //
            this._nDiameter.DecimalPlaces = 1;
            this._nDiameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nDiameter.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._nDiameter.Margin = new System.Windows.Forms.Padding(2);
            this._nDiameter.Maximum = new decimal(new int[] { 1000, 0, 0, 0});
            this._nDiameter.Minimum = new decimal(new int[] { 1, 0, 0, 0});
            this._nDiameter.Name = "_nDiameter";
            this._nDiameter.Size = new System.Drawing.Size(200, 25);
            this._nDiameter.TabIndex = 8;
            this._nDiameter.Value = new decimal(new int[] { 200, 0, 0, 0});
            //
            // lblEdgeMode
            //
            this.lblEdgeMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblEdgeMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEdgeMode.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblEdgeMode.Margin = new System.Windows.Forms.Padding(2);
            this.lblEdgeMode.Name = "lblEdgeMode";
            this.lblEdgeMode.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblEdgeMode.Size = new System.Drawing.Size(126, 30);
            this.lblEdgeMode.TabIndex = 9;
            this.lblEdgeMode.Text = "Edge skip mode";
            this.lblEdgeMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _cbEdgeSkipMode
            //
            this._cbEdgeSkipMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbEdgeSkipMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbEdgeSkipMode.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbEdgeSkipMode.Items.AddRange(new object[] {
            "GRID COUNT",
            "MM",
            "EXTERNAL MAP"});
            this._cbEdgeSkipMode.Margin = new System.Windows.Forms.Padding(2);
            this._cbEdgeSkipMode.Name = "_cbEdgeSkipMode";
            this._cbEdgeSkipMode.Size = new System.Drawing.Size(200, 25);
            this._cbEdgeSkipMode.TabIndex = 10;
            this._cbEdgeSkipMode.SelectedIndexChanged += new System.EventHandler(this._cbEdgeSkipMode_SelectedIndexChanged);
            //
            // lblEdgeLR
            //
            this.lblEdgeLR.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblEdgeLR.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEdgeLR.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblEdgeLR.Margin = new System.Windows.Forms.Padding(2);
            this.lblEdgeLR.Name = "lblEdgeLR";
            this.lblEdgeLR.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblEdgeLR.Size = new System.Drawing.Size(146, 30);
            this.lblEdgeLR.TabIndex = 11;
            this.lblEdgeLR.Text = "Edge skip L/R";
            this.lblEdgeLR.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _nSideEdgeSkip
            //
            this._nSideEdgeSkip.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nSideEdgeSkip.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._nSideEdgeSkip.Margin = new System.Windows.Forms.Padding(2);
            this._nSideEdgeSkip.Maximum = new decimal(new int[] { 500, 0, 0, 0});
            this._nSideEdgeSkip.Name = "_nSideEdgeSkip";
            this._nSideEdgeSkip.Size = new System.Drawing.Size(200, 25);
            this._nSideEdgeSkip.TabIndex = 12;
            //
            // lblEdgeTB
            //
            this.lblEdgeTB.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblEdgeTB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEdgeTB.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblEdgeTB.Margin = new System.Windows.Forms.Padding(2);
            this.lblEdgeTB.Name = "lblEdgeTB";
            this.lblEdgeTB.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblEdgeTB.Size = new System.Drawing.Size(126, 30);
            this.lblEdgeTB.TabIndex = 13;
            this.lblEdgeTB.Text = "Edge skip T/B";
            this.lblEdgeTB.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _nTopBottomEdgeSkip
            //
            this._nTopBottomEdgeSkip.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nTopBottomEdgeSkip.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._nTopBottomEdgeSkip.Margin = new System.Windows.Forms.Padding(2);
            this._nTopBottomEdgeSkip.Maximum = new decimal(new int[] { 500, 0, 0, 0});
            this._nTopBottomEdgeSkip.Name = "_nTopBottomEdgeSkip";
            this._nTopBottomEdgeSkip.Size = new System.Drawing.Size(200, 25);
            this._nTopBottomEdgeSkip.TabIndex = 14;
            //
            // lblMapFile
            //
            this.lblMapFile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblMapFile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMapFile.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblMapFile.Margin = new System.Windows.Forms.Padding(2);
            this.lblMapFile.Name = "lblMapFile";
            this.lblMapFile.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblMapFile.Size = new System.Drawing.Size(146, 30);
            this.lblMapFile.TabIndex = 15;
            this.lblMapFile.Text = "Recipe map file";
            this.lblMapFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _lblMapFileValue
            //
            this._lblMapFileValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblMapFileValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblMapFileValue.Font = new System.Drawing.Font("맑은 고딕", 8.5F);
            this._lblMapFileValue.Margin = new System.Windows.Forms.Padding(2);
            this._lblMapFileValue.Name = "_lblMapFileValue";
            this._lblMapFileValue.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this._lblMapFileValue.Size = new System.Drawing.Size(577, 30);
            this._lblMapFileValue.TabIndex = 16;
            this._lblMapFileValue.Text = "-";
            this._lblMapFileValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblBaseHint
            //
            this.lblBaseHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBaseHint.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblBaseHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.lblBaseHint.Margin = new System.Windows.Forms.Padding(4, 4, 2, 0);
            this.lblBaseHint.Name = "lblBaseHint";
            this.lblBaseHint.Size = new System.Drawing.Size(729, 30);
            this.lblBaseHint.TabIndex = 17;
            this.lblBaseHint.Text = "이 그룹 값은 LOAD WAFER MAP / GRID MAP CREATE로만 맵에 반영됩니다.";
            this.lblBaseHint.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            //
            // lblRotate
            //
            this.lblRotate.Location = new System.Drawing.Point(0, 0);
            this.lblRotate.Name = "lblRotate";
            this.lblRotate.Size = new System.Drawing.Size(10, 10);
            this.lblRotate.TabIndex = 90;
            this.lblRotate.Text = "Rotate (reserved)";
            this.lblRotate.Visible = false;
            //
            // _cbRotate
            //
            this._cbRotate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbRotate.Items.AddRange(new object[] {
            "None",
            "CW90",
            "CCW90",
            "Rotate180"});
            this._cbRotate.Location = new System.Drawing.Point(0, 0);
            this._cbRotate.Name = "_cbRotate";
            this._cbRotate.Size = new System.Drawing.Size(10, 25);
            this._cbRotate.TabIndex = 91;
            this._cbRotate.TabStop = false;
            this._cbRotate.Visible = false;
            //
            // grpDie
            //
            this.grpDie.BackColor = System.Drawing.Color.White;
            this.grpDie.Controls.Add(this.tlpDie);
            this.grpDie.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpDie.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpDie.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(45)))), ((int)(((byte)(57)))));
            this.grpDie.Location = new System.Drawing.Point(0, 311);
            this.grpDie.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpDie.Name = "grpDie";
            this.grpDie.Padding = new System.Windows.Forms.Padding(10, 6, 10, 8);
            this.grpDie.Size = new System.Drawing.Size(755, 196);
            this.grpDie.TabIndex = 2;
            this.grpDie.TabStop = false;
            this.grpDie.Text = "② 다이 배치 — 변경하면 상단 SAVE로 좌표 재계산";
            //
            // tlpDie
            //
            this.tlpDie.ColumnCount = 5;
            this.tlpDie.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpDie.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpDie.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tlpDie.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpDie.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpDie.Controls.Add(this.lblDieSizeX, 0, 0);
            this.tlpDie.Controls.Add(this._nDieSizeX, 1, 0);
            this.tlpDie.Controls.Add(this.lblDieSizeY, 2, 0);
            this.tlpDie.Controls.Add(this._nDieSizeY, 3, 0);
            this.tlpDie.Controls.Add(this.btnEditDieSpec, 4, 0);
            this.tlpDie.Controls.Add(this.lblPitchX, 0, 1);
            this.tlpDie.Controls.Add(this._nPitchX, 1, 1);
            this.tlpDie.Controls.Add(this.lblPitchY, 2, 1);
            this.tlpDie.Controls.Add(this._nPitchY, 3, 1);
            this.tlpDie.Controls.Add(this.lblCenterPitch, 0, 2);
            this.tlpDie.Controls.Add(this.lblCenterPitchValue, 1, 2);
            this.tlpDie.Controls.Add(this.lblDieHint, 0, 3);
            this.tlpDie.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpDie.Location = new System.Drawing.Point(10, 28);
            this.tlpDie.Name = "tlpDie";
            this.tlpDie.RowCount = 4;
            this.tlpDie.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpDie.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpDie.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tlpDie.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpDie.SetColumnSpan(this.lblCenterPitchValue, 4);
            this.tlpDie.SetColumnSpan(this.lblDieHint, 5);
            this.tlpDie.Size = new System.Drawing.Size(735, 160);
            this.tlpDie.TabIndex = 0;
            //
            // lblDieSizeX
            //
            this.lblDieSizeX.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblDieSizeX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieSizeX.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDieSizeX.Margin = new System.Windows.Forms.Padding(2);
            this.lblDieSizeX.Name = "lblDieSizeX";
            this.lblDieSizeX.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblDieSizeX.Size = new System.Drawing.Size(146, 32);
            this.lblDieSizeX.TabIndex = 0;
            this.lblDieSizeX.Text = "Die size X (mm)";
            this.lblDieSizeX.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _nDieSizeX
            //
            this._nDieSizeX.DecimalPlaces = 3;
            this._nDieSizeX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nDieSizeX.Enabled = false;
            this._nDieSizeX.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._nDieSizeX.Margin = new System.Windows.Forms.Padding(2);
            this._nDieSizeX.Maximum = new decimal(new int[] { 1000, 0, 0, 0});
            this._nDieSizeX.Name = "_nDieSizeX";
            this._nDieSizeX.ReadOnly = true;
            this._nDieSizeX.Size = new System.Drawing.Size(200, 25);
            this._nDieSizeX.TabIndex = 1;
            this._nDieSizeX.TabStop = false;
            this.toolTipRecipeLocation.SetToolTip(this._nDieSizeX, "Recipe 다이 사양(Die Spec) 값입니다. 편집은 [다이 사양 편집] 버튼으로 이동하세요.");
            //
            // lblDieSizeY
            //
            this.lblDieSizeY.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblDieSizeY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieSizeY.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDieSizeY.Margin = new System.Windows.Forms.Padding(2);
            this.lblDieSizeY.Name = "lblDieSizeY";
            this.lblDieSizeY.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblDieSizeY.Size = new System.Drawing.Size(126, 32);
            this.lblDieSizeY.TabIndex = 2;
            this.lblDieSizeY.Text = "Die size Y (mm)";
            this.lblDieSizeY.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _nDieSizeY
            //
            this._nDieSizeY.DecimalPlaces = 3;
            this._nDieSizeY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nDieSizeY.Enabled = false;
            this._nDieSizeY.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._nDieSizeY.Margin = new System.Windows.Forms.Padding(2);
            this._nDieSizeY.Maximum = new decimal(new int[] { 1000, 0, 0, 0});
            this._nDieSizeY.Name = "_nDieSizeY";
            this._nDieSizeY.ReadOnly = true;
            this._nDieSizeY.Size = new System.Drawing.Size(200, 25);
            this._nDieSizeY.TabIndex = 3;
            this._nDieSizeY.TabStop = false;
            this.toolTipRecipeLocation.SetToolTip(this._nDieSizeY, "Recipe 다이 사양(Die Spec) 값입니다. 편집은 [다이 사양 편집] 버튼으로 이동하세요.");
            //
            // btnEditDieSpec
            //
            this.btnEditDieSpec.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnEditDieSpec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEditDieSpec.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnEditDieSpec.Margin = new System.Windows.Forms.Padding(2);
            this.btnEditDieSpec.Name = "btnEditDieSpec";
            this.btnEditDieSpec.Size = new System.Drawing.Size(146, 32);
            this.btnEditDieSpec.TabIndex = 4;
            this.btnEditDieSpec.Text = "다이 사양 편집";
            this.toolTipRecipeLocation.SetToolTip(this.btnEditDieSpec, "Die Size는 이 화면에서 편집할 수 없습니다. Recipe > 다이 사양 화면으로 이동합니다.");
            this.btnEditDieSpec.UseVisualStyleBackColor = true;
            this.btnEditDieSpec.Click += new System.EventHandler(this.btnEditDieSpec_Click);
            //
            // lblPitchX
            //
            this.lblPitchX.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblPitchX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchX.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblPitchX.Margin = new System.Windows.Forms.Padding(2);
            this.lblPitchX.Name = "lblPitchX";
            this.lblPitchX.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblPitchX.Size = new System.Drawing.Size(146, 32);
            this.lblPitchX.TabIndex = 5;
            this.lblPitchX.Text = "DIE GAP X (mm)";
            this.lblPitchX.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _nPitchX
            //
            this._nPitchX.DecimalPlaces = 3;
            this._nPitchX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPitchX.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._nPitchX.Increment = new decimal(new int[] { 1, 0, 0, 131072});
            this._nPitchX.Margin = new System.Windows.Forms.Padding(2);
            this._nPitchX.Maximum = new decimal(new int[] { 50, 0, 0, 0});
            this._nPitchX.Name = "_nPitchX";
            this._nPitchX.Size = new System.Drawing.Size(200, 25);
            this._nPitchX.TabIndex = 6;
            //
            // lblPitchY
            //
            this.lblPitchY.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblPitchY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchY.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblPitchY.Margin = new System.Windows.Forms.Padding(2);
            this.lblPitchY.Name = "lblPitchY";
            this.lblPitchY.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblPitchY.Size = new System.Drawing.Size(126, 32);
            this.lblPitchY.TabIndex = 7;
            this.lblPitchY.Text = "DIE GAP Y (mm)";
            this.lblPitchY.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _nPitchY
            //
            this._nPitchY.DecimalPlaces = 3;
            this._nPitchY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPitchY.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._nPitchY.Increment = new decimal(new int[] { 1, 0, 0, 131072});
            this._nPitchY.Margin = new System.Windows.Forms.Padding(2);
            this._nPitchY.Maximum = new decimal(new int[] { 50, 0, 0, 0});
            this._nPitchY.Name = "_nPitchY";
            this._nPitchY.Size = new System.Drawing.Size(200, 25);
            this._nPitchY.TabIndex = 8;
            //
            // lblCenterPitch
            //
            this.lblCenterPitch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblCenterPitch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCenterPitch.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblCenterPitch.Margin = new System.Windows.Forms.Padding(2);
            this.lblCenterPitch.Name = "lblCenterPitch";
            this.lblCenterPitch.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblCenterPitch.Size = new System.Drawing.Size(146, 32);
            this.lblCenterPitch.TabIndex = 9;
            this.lblCenterPitch.Text = "중심 간 거리 (mm)";
            this.lblCenterPitch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblCenterPitchValue
            //
            this.lblCenterPitchValue.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(251)))));
            this.lblCenterPitchValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCenterPitchValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCenterPitchValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblCenterPitchValue.Margin = new System.Windows.Forms.Padding(2);
            this.lblCenterPitchValue.Name = "lblCenterPitchValue";
            this.lblCenterPitchValue.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblCenterPitchValue.Size = new System.Drawing.Size(577, 32);
            this.lblCenterPitchValue.TabIndex = 10;
            this.lblCenterPitchValue.Text = "-";
            this.lblCenterPitchValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTipRecipeLocation.SetToolTip(this.lblCenterPitchValue, "중심 간 거리 = Die size + DIE GAP. 실제 맵 좌표 계산에 쓰이는 값입니다.");
            //
            // lblDieHint
            //
            this.lblDieHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieHint.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDieHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.lblDieHint.Margin = new System.Windows.Forms.Padding(4, 4, 2, 0);
            this.lblDieHint.Name = "lblDieHint";
            this.lblDieHint.Size = new System.Drawing.Size(729, 30);
            this.lblDieHint.TabIndex = 11;
            this.lblDieHint.Text = "DIE GAP을 바꾸면 상단 SAVE로 역할 맵 좌표가 다시 계산됩니다.";
            this.lblDieHint.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            //
            // grpResult
            //
            this.grpResult.BackColor = System.Drawing.Color.White;
            this.grpResult.Controls.Add(this.tlpResult);
            this.grpResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpResult.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpResult.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(45)))), ((int)(((byte)(57)))));
            this.grpResult.Location = new System.Drawing.Point(0, 513);
            this.grpResult.Margin = new System.Windows.Forms.Padding(0);
            this.grpResult.Name = "grpResult";
            this.grpResult.Padding = new System.Windows.Forms.Padding(10, 6, 10, 8);
            this.grpResult.Size = new System.Drawing.Size(755, 165);
            this.grpResult.TabIndex = 3;
            this.grpResult.TabStop = false;
            this.grpResult.Text = "③ 결과 / 승인";
            //
            // tlpResult
            //
            this.tlpResult.ColumnCount = 3;
            this.tlpResult.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpResult.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpResult.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpResult.Controls.Add(this.btnGridCountPreview, 0, 0);
            this.tlpResult.Controls.Add(this.lblGridCountResult, 1, 0);
            this.tlpResult.Controls.Add(this.lblApprovalInput, 0, 1);
            this.tlpResult.Controls.Add(this.lblApprovalGood, 1, 1);
            this.tlpResult.Controls.Add(this.lblApprovalNg, 2, 1);
            this.tlpResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpResult.Location = new System.Drawing.Point(10, 28);
            this.tlpResult.Name = "tlpResult";
            this.tlpResult.RowCount = 2;
            this.tlpResult.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tlpResult.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpResult.SetColumnSpan(this.lblGridCountResult, 2);
            this.tlpResult.Size = new System.Drawing.Size(735, 129);
            this.tlpResult.TabIndex = 0;
            //
            // btnGridCountPreview
            //
            this.btnGridCountPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGridCountPreview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGridCountPreview.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnGridCountPreview.Margin = new System.Windows.Forms.Padding(2);
            this.btnGridCountPreview.Name = "btnGridCountPreview";
            this.btnGridCountPreview.Size = new System.Drawing.Size(146, 36);
            this.btnGridCountPreview.TabIndex = 0;
            this.btnGridCountPreview.Text = "COUNT CHECK";
            this.toolTipRecipeLocation.SetToolTip(this.btnGridCountPreview, "현재 Grid/DIE GAP/Die/Wafer Diameter 기준으로 생성 가능 개수를 계산합니다. 파일은 바뀌지 않습니다.");
            this.btnGridCountPreview.UseVisualStyleBackColor = true;
            this.btnGridCountPreview.Click += new System.EventHandler(this.btnGridCountPreview_Click);
            //
            // lblGridCountResult
            //
            this.lblGridCountResult.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGridCountResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGridCountResult.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblGridCountResult.Margin = new System.Windows.Forms.Padding(2);
            this.lblGridCountResult.Name = "lblGridCountResult";
            this.lblGridCountResult.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGridCountResult.Size = new System.Drawing.Size(577, 36);
            this.lblGridCountResult.TabIndex = 1;
            this.lblGridCountResult.Text = "Count: -";
            this.lblGridCountResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblApprovalInput
            //
            this.lblApprovalInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblApprovalInput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblApprovalInput.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblApprovalInput.Margin = new System.Windows.Forms.Padding(2);
            this.lblApprovalInput.Name = "lblApprovalInput";
            this.lblApprovalInput.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblApprovalInput.Size = new System.Drawing.Size(146, 30);
            this.lblApprovalInput.TabIndex = 2;
            this.lblApprovalInput.Text = "INPUT -";
            this.lblApprovalInput.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblApprovalGood
            //
            this.lblApprovalGood.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblApprovalGood.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblApprovalGood.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblApprovalGood.Margin = new System.Windows.Forms.Padding(2);
            this.lblApprovalGood.Name = "lblApprovalGood";
            this.lblApprovalGood.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblApprovalGood.Size = new System.Drawing.Size(285, 30);
            this.lblApprovalGood.TabIndex = 3;
            this.lblApprovalGood.Text = "GOOD -";
            this.lblApprovalGood.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblApprovalNg
            //
            this.lblApprovalNg.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblApprovalNg.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblApprovalNg.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblApprovalNg.Margin = new System.Windows.Forms.Padding(2);
            this.lblApprovalNg.Name = "lblApprovalNg";
            this.lblApprovalNg.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblApprovalNg.Size = new System.Drawing.Size(285, 30);
            this.lblApprovalNg.TabIndex = 4;
            this.lblApprovalNg.Text = "NG -";
            this.lblApprovalNg.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // grpMapSource
            //
            this.grpMapSource.BackColor = System.Drawing.Color.White;
            this.grpMapSource.Controls.Add(this._tbMapSourceInfo);
            this.grpMapSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMapSource.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpMapSource.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(45)))), ((int)(((byte)(57)))));
            this.grpMapSource.Location = new System.Drawing.Point(767, 0);
            this.grpMapSource.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.grpMapSource.Name = "grpMapSource";
            this.grpMapSource.Padding = new System.Windows.Forms.Padding(10, 8, 10, 10);
            this.grpMapSource.Size = new System.Drawing.Size(743, 678);
            this.grpMapSource.TabIndex = 4;
            this.grpMapSource.TabStop = false;
            this.grpMapSource.Text = "Loaded wafer map / apply flow";
            //
            // _tbMapSourceInfo
            //
            this._tbMapSourceInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(251)))));
            this._tbMapSourceInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._tbMapSourceInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbMapSourceInfo.Font = new System.Drawing.Font("Consolas", 10F);
            this._tbMapSourceInfo.Location = new System.Drawing.Point(10, 31);
            this._tbMapSourceInfo.Multiline = true;
            this._tbMapSourceInfo.Name = "_tbMapSourceInfo";
            this._tbMapSourceInfo.ReadOnly = true;
            this._tbMapSourceInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._tbMapSourceInfo.Size = new System.Drawing.Size(723, 637);
            this._tbMapSourceInfo.TabIndex = 0;
            this._tbMapSourceInfo.TabStop = false;
            this._tbMapSourceInfo.Text = "No Base Wafer Map loaded.";
            //
            // TapeFrameSubsetPage
            //
            this._editorPanel.Controls.Add(this.editorLayout);
            this.Name = "TapeFrameSubsetPage";
            this.Size = new System.Drawing.Size(1526, 764);
            this.Controls.SetChildIndex(this._editorPanel, 0);
            this._editorPanel.ResumeLayout(false);
            this.pnlSpecPreset.ResumeLayout(false);
            this.tlpSpecPreset.ResumeLayout(false);
            this.tlpSpecPreset.PerformLayout();
            this.grpBase.ResumeLayout(false);
            this.tlpBase.ResumeLayout(false);
            this.pnlBaseButtons.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nGridX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nGridY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDiameter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nSideEdgeSkip)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nTopBottomEdgeSkip)).EndInit();
            this.grpDie.ResumeLayout(false);
            this.tlpDie.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nDieSizeX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDieSizeY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchY)).EndInit();
            this.grpResult.ResumeLayout(false);
            this.tlpResult.ResumeLayout(false);
            this.grpMapSource.ResumeLayout(false);
            this.grpMapSource.PerformLayout();
            this.editorLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
