using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    partial class PlaceZCalibrationDialog
    {
        private IContainer components;
        private TableLayoutPanel _rootLayout;
        private Label _headerLabel;
        private GroupBox _batchGroup;
        private FlowLayoutPanel _batchFlow;
        private CheckBox _chkBatchAll;
        private CheckBox _chkBatchFront4;
        private CheckBox _chkBatchFront3;
        private CheckBox _chkBatchFront2;
        private CheckBox _chkBatchFront1;
        private CheckBox _chkBatchRear4;
        private CheckBox _chkBatchRear3;
        private CheckBox _chkBatchRear2;
        private CheckBox _chkBatchRear1;
        private CalibrationDialogButton _btnBatchStart;
        private Panel _selectorPanel;
        private Label _sideLabel;
        private ComboBox _cmbSide;
        private Label _outputLabel;
        private ComboBox _cmbOutputSide;
        private Label _pickerLabel;
        private ComboBox _cmbPickerNo;
        private TableLayoutPanel _bodyLayout;
        private GroupBox _settingsGroup;
        private TableLayoutPanel _settingsLayout;
        private DataGridView _settingsGrid;
        private DataGridViewTextBoxColumn _settingsParameterColumn;
        private DataGridViewTextBoxColumn _settingsValueColumn;
        private DataGridViewTextBoxColumn _settingsUnitColumn;
        private CalibrationDialogButton _btnParameterSave;
        private GroupBox _resultGroup;
        private DataGridView _resultGrid;
        private DataGridViewTextBoxColumn _resultItemColumn;
        private DataGridViewTextBoxColumn _resultSideColumn;
        private DataGridViewTextBoxColumn _resultOutputColumn;
        private DataGridViewTextBoxColumn _resultPickerColumn;
        private DataGridViewTextBoxColumn _resultOldPlaceColumn;
        private DataGridViewTextBoxColumn _resultStartZColumn;
        private DataGridViewTextBoxColumn _resultFlowZColumn;
        private DataGridViewTextBoxColumn _resultDieColumn;
        private DataGridViewTextBoxColumn _resultFilmColumn;
        private DataGridViewTextBoxColumn _resultSavedPlaceColumn;
        private DataGridViewTextBoxColumn _resultValidColumn;
        private Label _status;
        private TableLayoutPanel _footerLayout;
        private CalibrationDialogButton _btnCheck;
        private CalibrationDialogButton _btnMoveStart;
        private CalibrationDialogButton _btnStartScan;
        private CalibrationDialogButton _btnMoveAvoid;
        private CalibrationDialogButton _btnVacOff;
        private CalibrationDialogButton _btnSeqStop;
        private CalibrationDialogButton _btnReload;
        private CalibrationDialogButton _btnSave;
        private CalibrationDialogButton _btnClose;
        private Timer _flowStatusTimer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new Container();
            this._rootLayout = new TableLayoutPanel();
            this._headerLabel = new Label();
            this._batchGroup = new GroupBox();
            this._batchFlow = new FlowLayoutPanel();
            this._chkBatchAll = new CheckBox();
            this._chkBatchFront4 = new CheckBox();
            this._chkBatchFront3 = new CheckBox();
            this._chkBatchFront2 = new CheckBox();
            this._chkBatchFront1 = new CheckBox();
            this._chkBatchRear4 = new CheckBox();
            this._chkBatchRear3 = new CheckBox();
            this._chkBatchRear2 = new CheckBox();
            this._chkBatchRear1 = new CheckBox();
            this._btnBatchStart = new CalibrationDialogButton();
            this._selectorPanel = new Panel();
            this._sideLabel = new Label();
            this._cmbSide = new ComboBox();
            this._outputLabel = new Label();
            this._cmbOutputSide = new ComboBox();
            this._pickerLabel = new Label();
            this._cmbPickerNo = new ComboBox();
            this._bodyLayout = new TableLayoutPanel();
            this._settingsGroup = new GroupBox();
            this._settingsLayout = new TableLayoutPanel();
            this._settingsGrid = new DataGridView();
            this._settingsParameterColumn = new DataGridViewTextBoxColumn();
            this._settingsValueColumn = new DataGridViewTextBoxColumn();
            this._settingsUnitColumn = new DataGridViewTextBoxColumn();
            this._btnParameterSave = new CalibrationDialogButton();
            this._resultGroup = new GroupBox();
            this._resultGrid = new DataGridView();
            this._resultItemColumn = new DataGridViewTextBoxColumn();
            this._resultSideColumn = new DataGridViewTextBoxColumn();
            this._resultOutputColumn = new DataGridViewTextBoxColumn();
            this._resultPickerColumn = new DataGridViewTextBoxColumn();
            this._resultOldPlaceColumn = new DataGridViewTextBoxColumn();
            this._resultStartZColumn = new DataGridViewTextBoxColumn();
            this._resultFlowZColumn = new DataGridViewTextBoxColumn();
            this._resultDieColumn = new DataGridViewTextBoxColumn();
            this._resultFilmColumn = new DataGridViewTextBoxColumn();
            this._resultSavedPlaceColumn = new DataGridViewTextBoxColumn();
            this._resultValidColumn = new DataGridViewTextBoxColumn();
            this._status = new Label();
            this._footerLayout = new TableLayoutPanel();
            this._btnCheck = new CalibrationDialogButton();
            this._btnMoveStart = new CalibrationDialogButton();
            this._btnStartScan = new CalibrationDialogButton();
            this._btnMoveAvoid = new CalibrationDialogButton();
            this._btnVacOff = new CalibrationDialogButton();
            this._btnSeqStop = new CalibrationDialogButton();
            this._btnReload = new CalibrationDialogButton();
            this._btnSave = new CalibrationDialogButton();
            this._btnClose = new CalibrationDialogButton();
            this._flowStatusTimer = new Timer(this.components);
            this._rootLayout.SuspendLayout();
            this._batchGroup.SuspendLayout();
            this._batchFlow.SuspendLayout();
            this._selectorPanel.SuspendLayout();
            this._bodyLayout.SuspendLayout();
            this._settingsGroup.SuspendLayout();
            this._settingsLayout.SuspendLayout();
            ((ISupportInitialize)(this._settingsGrid)).BeginInit();
            this._resultGroup.SuspendLayout();
            ((ISupportInitialize)(this._resultGrid)).BeginInit();
            this._footerLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // _rootLayout
            // 
            this._rootLayout.ColumnCount = 1;
            this._rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._rootLayout.Controls.Add(this._headerLabel, 0, 0);
            this._rootLayout.Controls.Add(this._batchGroup, 0, 1);
            this._rootLayout.Controls.Add(this._selectorPanel, 0, 2);
            this._rootLayout.Controls.Add(this._bodyLayout, 0, 3);
            this._rootLayout.Controls.Add(this._status, 0, 4);
            this._rootLayout.Controls.Add(this._footerLayout, 0, 5);
            this._rootLayout.Dock = DockStyle.Fill;
            this._rootLayout.Location = new Point(0, 0);
            this._rootLayout.Name = "_rootLayout";
            this._rootLayout.Padding = new Padding(8);
            this._rootLayout.RowCount = 6;
            this._rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            this._rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            this._rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            this._rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            this._rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            this._rootLayout.Size = new Size(1104, 681);
            this._rootLayout.TabIndex = 0;
            // 
            // _headerLabel
            // 
            this._headerLabel.BackColor = Color.FromArgb(230, 126, 0);
            this._headerLabel.Dock = DockStyle.Fill;
            this._headerLabel.Font = new Font("Malgun Gothic", 14F, FontStyle.Bold);
            this._headerLabel.ForeColor = Color.White;
            this._headerLabel.Location = new Point(11, 8);
            this._headerLabel.Name = "_headerLabel";
            this._headerLabel.Padding = new Padding(14, 0, 0, 0);
            this._headerLabel.Size = new Size(1082, 54);
            this._headerLabel.TabIndex = 0;
            this._headerLabel.Text = "PLACE Z CAL";
            this._headerLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _batchGroup
            // 
            this._batchGroup.Controls.Add(this._batchFlow);
            this._batchGroup.Dock = DockStyle.Fill;
            this._batchGroup.Font = new Font("Malgun Gothic", 9F, FontStyle.Bold);
            this._batchGroup.Location = new Point(11, 65);
            this._batchGroup.Name = "_batchGroup";
            this._batchGroup.Padding = new Padding(6, 3, 6, 3);
            this._batchGroup.Size = new Size(1082, 48);
            this._batchGroup.TabIndex = 1;
            this._batchGroup.TabStop = false;
            this._batchGroup.Text = "BATCH (선택 Output 고정 · 직렬 측정: Front P4→P1, Rear P4→P1)";
            // 
            // _batchFlow
            // 
            this._batchFlow.Controls.Add(this._chkBatchAll);
            this._batchFlow.Controls.Add(this._chkBatchFront4);
            this._batchFlow.Controls.Add(this._chkBatchFront3);
            this._batchFlow.Controls.Add(this._chkBatchFront2);
            this._batchFlow.Controls.Add(this._chkBatchFront1);
            this._batchFlow.Controls.Add(this._chkBatchRear4);
            this._batchFlow.Controls.Add(this._chkBatchRear3);
            this._batchFlow.Controls.Add(this._chkBatchRear2);
            this._batchFlow.Controls.Add(this._chkBatchRear1);
            this._batchFlow.Controls.Add(this._btnBatchStart);
            this._batchFlow.Dock = DockStyle.Fill;
            this._batchFlow.Location = new Point(6, 19);
            this._batchFlow.Name = "_batchFlow";
            this._batchFlow.Padding = new Padding(2, 0, 0, 0);
            this._batchFlow.Size = new Size(1070, 26);
            this._batchFlow.TabIndex = 0;
            this._batchFlow.WrapContents = false;
            // 
            // batch target controls
            // 
            this._chkBatchAll.AutoSize = true;
            this._chkBatchAll.Name = "_chkBatchAll";
            this._chkBatchAll.TabIndex = 0;
            this._chkBatchAll.Text = "ALL";
            this._chkBatchAll.UseVisualStyleBackColor = true;
            this._chkBatchAll.CheckedChanged += new System.EventHandler(this.BatchAll_CheckedChanged);
            this._chkBatchFront4.AutoSize = true;
            this._chkBatchFront4.Name = "_chkBatchFront4";
            this._chkBatchFront4.TabIndex = 1;
            this._chkBatchFront4.Text = "F P4";
            this._chkBatchFront4.UseVisualStyleBackColor = true;
            this._chkBatchFront4.CheckedChanged += new System.EventHandler(this.BatchTarget_CheckedChanged);
            this._chkBatchFront3.AutoSize = true;
            this._chkBatchFront3.Name = "_chkBatchFront3";
            this._chkBatchFront3.TabIndex = 2;
            this._chkBatchFront3.Text = "F P3";
            this._chkBatchFront3.UseVisualStyleBackColor = true;
            this._chkBatchFront3.CheckedChanged += new System.EventHandler(this.BatchTarget_CheckedChanged);
            this._chkBatchFront2.AutoSize = true;
            this._chkBatchFront2.Name = "_chkBatchFront2";
            this._chkBatchFront2.TabIndex = 3;
            this._chkBatchFront2.Text = "F P2";
            this._chkBatchFront2.UseVisualStyleBackColor = true;
            this._chkBatchFront2.CheckedChanged += new System.EventHandler(this.BatchTarget_CheckedChanged);
            this._chkBatchFront1.AutoSize = true;
            this._chkBatchFront1.Name = "_chkBatchFront1";
            this._chkBatchFront1.TabIndex = 4;
            this._chkBatchFront1.Text = "F P1";
            this._chkBatchFront1.UseVisualStyleBackColor = true;
            this._chkBatchFront1.CheckedChanged += new System.EventHandler(this.BatchTarget_CheckedChanged);
            this._chkBatchRear4.AutoSize = true;
            this._chkBatchRear4.Name = "_chkBatchRear4";
            this._chkBatchRear4.TabIndex = 5;
            this._chkBatchRear4.Text = "R P4";
            this._chkBatchRear4.UseVisualStyleBackColor = true;
            this._chkBatchRear4.CheckedChanged += new System.EventHandler(this.BatchTarget_CheckedChanged);
            this._chkBatchRear3.AutoSize = true;
            this._chkBatchRear3.Name = "_chkBatchRear3";
            this._chkBatchRear3.TabIndex = 6;
            this._chkBatchRear3.Text = "R P3";
            this._chkBatchRear3.UseVisualStyleBackColor = true;
            this._chkBatchRear3.CheckedChanged += new System.EventHandler(this.BatchTarget_CheckedChanged);
            this._chkBatchRear2.AutoSize = true;
            this._chkBatchRear2.Name = "_chkBatchRear2";
            this._chkBatchRear2.TabIndex = 7;
            this._chkBatchRear2.Text = "R P2";
            this._chkBatchRear2.UseVisualStyleBackColor = true;
            this._chkBatchRear2.CheckedChanged += new System.EventHandler(this.BatchTarget_CheckedChanged);
            this._chkBatchRear1.AutoSize = true;
            this._chkBatchRear1.Name = "_chkBatchRear1";
            this._chkBatchRear1.TabIndex = 8;
            this._chkBatchRear1.Text = "R P1";
            this._chkBatchRear1.UseVisualStyleBackColor = true;
            this._chkBatchRear1.CheckedChanged += new System.EventHandler(this.BatchTarget_CheckedChanged);
            this._btnBatchStart.Margin = new Padding(16, 0, 6, 0);
            this._btnBatchStart.Name = "_btnBatchStart";
            this._btnBatchStart.Role = CalibrationDialogButtonRole.Primary;
            this._btnBatchStart.Size = new Size(180, 25);
            this._btnBatchStart.TabIndex = 9;
            this._btnBatchStart.Text = "BATCH START";
            this._btnBatchStart.UseVisualStyleBackColor = false;
            this._btnBatchStart.Click += new System.EventHandler(this.BtnBatchStart_Click);
            // 
            // _selectorPanel
            // 
            this._selectorPanel.Controls.Add(this._sideLabel);
            this._selectorPanel.Controls.Add(this._cmbSide);
            this._selectorPanel.Controls.Add(this._outputLabel);
            this._selectorPanel.Controls.Add(this._cmbOutputSide);
            this._selectorPanel.Controls.Add(this._pickerLabel);
            this._selectorPanel.Controls.Add(this._cmbPickerNo);
            this._selectorPanel.Dock = DockStyle.Fill;
            this._selectorPanel.Location = new Point(11, 119);
            this._selectorPanel.Name = "_selectorPanel";
            this._selectorPanel.Padding = new Padding(4, 6, 4, 4);
            this._selectorPanel.Size = new Size(1082, 38);
            this._selectorPanel.TabIndex = 2;
            this._sideLabel.Font = new Font("Malgun Gothic", 9F, FontStyle.Bold);
            this._sideLabel.Location = new Point(8, 8);
            this._sideLabel.Name = "_sideLabel";
            this._sideLabel.Size = new Size(48, 24);
            this._sideLabel.TabIndex = 0;
            this._sideLabel.Text = "Side";
            this._sideLabel.TextAlign = ContentAlignment.MiddleLeft;
            this._cmbSide.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cmbSide.FormattingEnabled = true;
            this._cmbSide.Items.AddRange(new object[] { "Front", "Rear" });
            this._cmbSide.Location = new Point(60, 7);
            this._cmbSide.Name = "_cmbSide";
            this._cmbSide.Size = new Size(120, 23);
            this._cmbSide.TabIndex = 1;
            this._cmbSide.SelectedIndexChanged += new System.EventHandler(this.PickerSelector_SelectedIndexChanged);
            this._outputLabel.Font = new Font("Malgun Gothic", 9F, FontStyle.Bold);
            this._outputLabel.Location = new Point(202, 8);
            this._outputLabel.Name = "_outputLabel";
            this._outputLabel.Size = new Size(62, 24);
            this._outputLabel.TabIndex = 2;
            this._outputLabel.Text = "Output";
            this._outputLabel.TextAlign = ContentAlignment.MiddleLeft;
            this._cmbOutputSide.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cmbOutputSide.FormattingEnabled = true;
            this._cmbOutputSide.Items.AddRange(new object[] { "Good", "NG" });
            this._cmbOutputSide.Location = new Point(266, 7);
            this._cmbOutputSide.Name = "_cmbOutputSide";
            this._cmbOutputSide.Size = new Size(90, 23);
            this._cmbOutputSide.TabIndex = 3;
            this._cmbOutputSide.SelectedIndexChanged += new System.EventHandler(this.OutputSelector_SelectedIndexChanged);
            this._pickerLabel.Font = new Font("Malgun Gothic", 9F, FontStyle.Bold);
            this._pickerLabel.Location = new Point(382, 8);
            this._pickerLabel.Name = "_pickerLabel";
            this._pickerLabel.Size = new Size(80, 24);
            this._pickerLabel.TabIndex = 4;
            this._pickerLabel.Text = "Picker No";
            this._pickerLabel.TextAlign = ContentAlignment.MiddleLeft;
            this._cmbPickerNo.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cmbPickerNo.FormattingEnabled = true;
            this._cmbPickerNo.Items.AddRange(new object[] { "1", "2", "3", "4" });
            this._cmbPickerNo.Location = new Point(466, 7);
            this._cmbPickerNo.Name = "_cmbPickerNo";
            this._cmbPickerNo.Size = new Size(80, 23);
            this._cmbPickerNo.TabIndex = 5;
            this._cmbPickerNo.SelectedIndexChanged += new System.EventHandler(this.PickerSelector_SelectedIndexChanged);
            // 
            // _bodyLayout
            // 
            this._bodyLayout.ColumnCount = 2;
            this._bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430F));
            this._bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._bodyLayout.Controls.Add(this._settingsGroup, 0, 0);
            this._bodyLayout.Controls.Add(this._resultGroup, 1, 0);
            this._bodyLayout.Dock = DockStyle.Fill;
            this._bodyLayout.Location = new Point(11, 163);
            this._bodyLayout.Name = "_bodyLayout";
            this._bodyLayout.RowCount = 1;
            this._bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._bodyLayout.Size = new Size(1082, 409);
            this._bodyLayout.TabIndex = 3;
            // 
            // settings area
            // 
            this._settingsGroup.Controls.Add(this._settingsLayout);
            this._settingsGroup.Dock = DockStyle.Fill;
            this._settingsGroup.Font = new Font("Malgun Gothic", 9F, FontStyle.Bold);
            this._settingsGroup.Name = "_settingsGroup";
            this._settingsGroup.Padding = new Padding(6);
            this._settingsGroup.TabIndex = 0;
            this._settingsGroup.TabStop = false;
            this._settingsGroup.Text = "CAL SETTING";
            this._settingsLayout.ColumnCount = 1;
            this._settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._settingsLayout.Controls.Add(this._settingsGrid, 0, 0);
            this._settingsLayout.Controls.Add(this._btnParameterSave, 0, 1);
            this._settingsLayout.Dock = DockStyle.Fill;
            this._settingsLayout.Name = "_settingsLayout";
            this._settingsLayout.RowCount = 2;
            this._settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            this._settingsLayout.TabIndex = 0;
            this._settingsGrid.AllowUserToAddRows = false;
            this._settingsGrid.AllowUserToDeleteRows = false;
            this._settingsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this._settingsGrid.BackgroundColor = Color.White;
            this._settingsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._settingsGrid.Columns.AddRange(new DataGridViewColumn[] {
                this._settingsParameterColumn,
                this._settingsValueColumn,
                this._settingsUnitColumn});
            this._settingsGrid.Dock = DockStyle.Fill;
            this._settingsGrid.Font = new Font("Malgun Gothic", 9F);
            this._settingsGrid.Name = "_settingsGrid";
            this._settingsGrid.ReadOnly = true;
            this._settingsGrid.RowHeadersVisible = false;
            this._settingsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this._settingsGrid.TabIndex = 0;
            this._settingsGrid.CellDoubleClick += new DataGridViewCellEventHandler(this.SettingsGrid_CellDoubleClick);
            this._settingsGrid.CellToolTipTextNeeded += new DataGridViewCellToolTipTextNeededEventHandler(this.SettingsGrid_CellToolTipTextNeeded);
            this._settingsParameterColumn.FillWeight = 52F;
            this._settingsParameterColumn.HeaderText = "PARAMETER";
            this._settingsParameterColumn.Name = "_settingsParameterColumn";
            this._settingsParameterColumn.ReadOnly = true;
            this._settingsValueColumn.FillWeight = 30F;
            this._settingsValueColumn.HeaderText = "VALUE";
            this._settingsValueColumn.Name = "_settingsValueColumn";
            this._settingsValueColumn.ReadOnly = true;
            this._settingsUnitColumn.FillWeight = 18F;
            this._settingsUnitColumn.HeaderText = "UNIT";
            this._settingsUnitColumn.Name = "_settingsUnitColumn";
            this._settingsUnitColumn.ReadOnly = true;
            this._btnParameterSave.Dock = DockStyle.Fill;
            this._btnParameterSave.Margin = new Padding(6, 4, 6, 2);
            this._btnParameterSave.Name = "_btnParameterSave";
            this._btnParameterSave.Role = CalibrationDialogButtonRole.Dark;
            this._btnParameterSave.TabIndex = 1;
            this._btnParameterSave.Text = "PARAMETER SAVE";
            this._btnParameterSave.UseVisualStyleBackColor = false;
            this._btnParameterSave.Click += new System.EventHandler(this.BtnParameterSave_Click);
            // 
            // result area
            // 
            this._resultGroup.Controls.Add(this._resultGrid);
            this._resultGroup.Dock = DockStyle.Fill;
            this._resultGroup.Font = new Font("Malgun Gothic", 9F, FontStyle.Bold);
            this._resultGroup.Name = "_resultGroup";
            this._resultGroup.Padding = new Padding(6);
            this._resultGroup.TabIndex = 1;
            this._resultGroup.TabStop = false;
            this._resultGroup.Text = "SAVED PLACE Z";
            this._resultGrid.AllowUserToAddRows = false;
            this._resultGrid.AllowUserToDeleteRows = false;
            this._resultGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this._resultGrid.BackgroundColor = Color.White;
            this._resultGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._resultGrid.Columns.AddRange(new DataGridViewColumn[] {
                this._resultItemColumn,
                this._resultSideColumn,
                this._resultOutputColumn,
                this._resultPickerColumn,
                this._resultOldPlaceColumn,
                this._resultStartZColumn,
                this._resultFlowZColumn,
                this._resultDieColumn,
                this._resultFilmColumn,
                this._resultSavedPlaceColumn,
                this._resultValidColumn});
            this._resultGrid.Dock = DockStyle.Fill;
            this._resultGrid.Font = new Font("Malgun Gothic", 9F);
            this._resultGrid.Name = "_resultGrid";
            this._resultGrid.ReadOnly = true;
            this._resultGrid.RowHeadersVisible = false;
            this._resultGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this._resultGrid.TabIndex = 0;
            this._resultItemColumn.FillWeight = 32F;
            this._resultItemColumn.HeaderText = "ITEM";
            this._resultItemColumn.Name = "_resultItemColumn";
            this._resultItemColumn.ReadOnly = true;
            this._resultSideColumn.FillWeight = 18F;
            this._resultSideColumn.HeaderText = "SIDE";
            this._resultSideColumn.Name = "_resultSideColumn";
            this._resultSideColumn.ReadOnly = true;
            this._resultOutputColumn.FillWeight = 18F;
            this._resultOutputColumn.HeaderText = "OUTPUT";
            this._resultOutputColumn.Name = "_resultOutputColumn";
            this._resultOutputColumn.ReadOnly = true;
            this._resultPickerColumn.FillWeight = 16F;
            this._resultPickerColumn.HeaderText = "PICKER";
            this._resultPickerColumn.Name = "_resultPickerColumn";
            this._resultPickerColumn.ReadOnly = true;
            this._resultOldPlaceColumn.FillWeight = 24F;
            this._resultOldPlaceColumn.HeaderText = "OLD PLACE";
            this._resultOldPlaceColumn.Name = "_resultOldPlaceColumn";
            this._resultOldPlaceColumn.ReadOnly = true;
            this._resultStartZColumn.FillWeight = 24F;
            this._resultStartZColumn.HeaderText = "START Z";
            this._resultStartZColumn.Name = "_resultStartZColumn";
            this._resultStartZColumn.ReadOnly = true;
            this._resultFlowZColumn.FillWeight = 24F;
            this._resultFlowZColumn.HeaderText = "FLOW Z";
            this._resultFlowZColumn.Name = "_resultFlowZColumn";
            this._resultFlowZColumn.ReadOnly = true;
            this._resultDieColumn.FillWeight = 18F;
            this._resultDieColumn.HeaderText = "DIE";
            this._resultDieColumn.Name = "_resultDieColumn";
            this._resultDieColumn.ReadOnly = true;
            this._resultFilmColumn.FillWeight = 18F;
            this._resultFilmColumn.HeaderText = "FILM";
            this._resultFilmColumn.Name = "_resultFilmColumn";
            this._resultFilmColumn.ReadOnly = true;
            this._resultSavedPlaceColumn.FillWeight = 24F;
            this._resultSavedPlaceColumn.HeaderText = "SAVED PLACE";
            this._resultSavedPlaceColumn.Name = "_resultSavedPlaceColumn";
            this._resultSavedPlaceColumn.ReadOnly = true;
            this._resultValidColumn.FillWeight = 14F;
            this._resultValidColumn.HeaderText = "VALID";
            this._resultValidColumn.Name = "_resultValidColumn";
            this._resultValidColumn.ReadOnly = true;
            // 
            // _status
            // 
            this._status.BackColor = Color.WhiteSmoke;
            this._status.BorderStyle = BorderStyle.FixedSingle;
            this._status.Dock = DockStyle.Fill;
            this._status.Name = "_status";
            this._status.Padding = new Padding(12, 0, 0, 0);
            this._status.TabIndex = 4;
            this._status.Text = "Output Place 위치 위에 Picker를 위치시킨 후 START SCAN을 실행하세요.";
            this._status.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _footerLayout
            // 
            this._footerLayout.ColumnCount = 9;
            this._footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11.11111F));
            this._footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11.11111F));
            this._footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11.11111F));
            this._footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11.11111F));
            this._footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11.11111F));
            this._footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11.11111F));
            this._footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11.11111F));
            this._footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11.11111F));
            this._footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11.11111F));
            this._footerLayout.Controls.Add(this._btnCheck, 0, 0);
            this._footerLayout.Controls.Add(this._btnMoveStart, 1, 0);
            this._footerLayout.Controls.Add(this._btnStartScan, 2, 0);
            this._footerLayout.Controls.Add(this._btnMoveAvoid, 3, 0);
            this._footerLayout.Controls.Add(this._btnVacOff, 4, 0);
            this._footerLayout.Controls.Add(this._btnSeqStop, 5, 0);
            this._footerLayout.Controls.Add(this._btnReload, 6, 0);
            this._footerLayout.Controls.Add(this._btnSave, 7, 0);
            this._footerLayout.Controls.Add(this._btnClose, 8, 0);
            this._footerLayout.Dock = DockStyle.Fill;
            this._footerLayout.Name = "_footerLayout";
            this._footerLayout.Padding = new Padding(0, 8, 0, 0);
            this._footerLayout.RowCount = 1;
            this._footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._footerLayout.TabIndex = 5;
            this._btnCheck.Dock = DockStyle.Fill;
            this._btnCheck.Name = "_btnCheck";
            this._btnCheck.Role = CalibrationDialogButtonRole.Normal;
            this._btnCheck.TabIndex = 0;
            this._btnCheck.Text = "CHECK";
            this._btnCheck.UseVisualStyleBackColor = false;
            this._btnCheck.Click += new System.EventHandler(this.BtnCheck_Click);
            this._btnMoveStart.Dock = DockStyle.Fill;
            this._btnMoveStart.Name = "_btnMoveStart";
            this._btnMoveStart.Role = CalibrationDialogButtonRole.Normal;
            this._btnMoveStart.TabIndex = 1;
            this._btnMoveStart.Text = "MOVE START";
            this._btnMoveStart.UseVisualStyleBackColor = false;
            this._btnMoveStart.Click += new System.EventHandler(this.BtnMoveStart_Click);
            this._btnStartScan.Dock = DockStyle.Fill;
            this._btnStartScan.Name = "_btnStartScan";
            this._btnStartScan.Role = CalibrationDialogButtonRole.Primary;
            this._btnStartScan.TabIndex = 2;
            this._btnStartScan.Text = "START SCAN";
            this._btnStartScan.UseVisualStyleBackColor = false;
            this._btnStartScan.Click += new System.EventHandler(this.BtnStartScan_Click);
            this._btnMoveAvoid.Dock = DockStyle.Fill;
            this._btnMoveAvoid.Name = "_btnMoveAvoid";
            this._btnMoveAvoid.Role = CalibrationDialogButtonRole.Normal;
            this._btnMoveAvoid.TabIndex = 3;
            this._btnMoveAvoid.Text = "Z AVOID";
            this._btnMoveAvoid.UseVisualStyleBackColor = false;
            this._btnMoveAvoid.Click += new System.EventHandler(this.BtnMoveAvoid_Click);
            this._btnVacOff.Dock = DockStyle.Fill;
            this._btnVacOff.Name = "_btnVacOff";
            this._btnVacOff.Role = CalibrationDialogButtonRole.Normal;
            this._btnVacOff.TabIndex = 4;
            this._btnVacOff.Text = "VAC OFF";
            this._btnVacOff.UseVisualStyleBackColor = false;
            this._btnVacOff.Click += new System.EventHandler(this.BtnVacOff_Click);
            this._btnSeqStop.Dock = DockStyle.Fill;
            this._btnSeqStop.Enabled = false;
            this._btnSeqStop.Name = "_btnSeqStop";
            this._btnSeqStop.Role = CalibrationDialogButtonRole.Normal;
            this._btnSeqStop.TabIndex = 5;
            this._btnSeqStop.Text = "SEQ STOP";
            this._btnSeqStop.UseVisualStyleBackColor = false;
            this._btnSeqStop.Click += new System.EventHandler(this.BtnSeqStop_Click);
            this._btnReload.Dock = DockStyle.Fill;
            this._btnReload.Name = "_btnReload";
            this._btnReload.Role = CalibrationDialogButtonRole.Normal;
            this._btnReload.TabIndex = 6;
            this._btnReload.Text = "RELOAD";
            this._btnReload.UseVisualStyleBackColor = false;
            this._btnReload.Click += new System.EventHandler(this.BtnReload_Click);
            this._btnSave.Dock = DockStyle.Fill;
            this._btnSave.Enabled = false;
            this._btnSave.Name = "_btnSave";
            this._btnSave.Role = CalibrationDialogButtonRole.Dark;
            this._btnSave.TabIndex = 7;
            this._btnSave.Text = "SAVE RESULT";
            this._btnSave.UseVisualStyleBackColor = false;
            this._btnSave.Click += new System.EventHandler(this.BtnSaveResult_Click);
            this._btnClose.Dock = DockStyle.Fill;
            this._btnClose.Name = "_btnClose";
            this._btnClose.Role = CalibrationDialogButtonRole.Normal;
            this._btnClose.TabIndex = 8;
            this._btnClose.Text = "CLOSE";
            this._btnClose.UseVisualStyleBackColor = false;
            this._btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            // 
            // _flowStatusTimer
            // 
            this._flowStatusTimer.Interval = 300;
            this._flowStatusTimer.Tick += new System.EventHandler(this.FlowStatusTimer_Tick);
            // 
            // PlaceZCalibrationDialog
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.FromArgb(238, 238, 238);
            this.ClientSize = new Size(1104, 681);
            this.Controls.Add(this._rootLayout);
            this.Font = new Font("Malgun Gothic", 9F);
            this.MinimumSize = new Size(980, 620);
            this.Name = "PlaceZCalibrationDialog";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Place Z Calibration";
            this._cmbSide.SelectedIndex = 0;
            this._cmbOutputSide.SelectedIndex = 0;
            this._cmbPickerNo.SelectedIndex = 0;
            this._rootLayout.ResumeLayout(false);
            this._batchGroup.ResumeLayout(false);
            this._batchFlow.ResumeLayout(false);
            this._batchFlow.PerformLayout();
            this._selectorPanel.ResumeLayout(false);
            this._bodyLayout.ResumeLayout(false);
            this._settingsGroup.ResumeLayout(false);
            this._settingsLayout.ResumeLayout(false);
            ((ISupportInitialize)(this._settingsGrid)).EndInit();
            this._resultGroup.ResumeLayout(false);
            ((ISupportInitialize)(this._resultGrid)).EndInit();
            this._footerLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
