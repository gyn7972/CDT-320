namespace QMC.CDT_320.Ui.Dialogs
{
    partial class ColletCalibrationDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.GroupBox groupSettings;
        private System.Windows.Forms.TableLayoutPanel settingsLayout;
        private System.Windows.Forms.DataGridView gridSettings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingUnit;
        private System.Windows.Forms.GroupBox groupResults;
        private System.Windows.Forms.TableLayoutPanel resultsLayout;
        private System.Windows.Forms.DataGridView gridResults;
        private System.Windows.Forms.DataGridViewTextBoxColumn colItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSide;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCollet;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOffsetX;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOffsetY;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTheta;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTZero;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFinalX;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFinalY;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFinalZ;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValid;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCocValid;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCocPixelX;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCocPixelY;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCocMachineX;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCocMachineY;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPickZ;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPlaceZ;
        private System.Windows.Forms.Label lblSaveHistory;
        private System.Windows.Forms.ListBox lstSaveHistory;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TableLayoutPanel buttonPanel;
        private CalibrationDialogButton btnCheck;
        private CalibrationDialogButton btnStart;
        private CalibrationDialogButton btnCoc;
        private CalibrationDialogButton btnCocCenter;
        private CalibrationDialogButton btnSaveBottomTeaching;
        private CalibrationDialogButton btnApplyHomeOffset;
        private CalibrationDialogButton btnMoveZForward;
        private CalibrationDialogButton btnMoveYAvoid;
        private CalibrationDialogButton btnSeqStop;
        private CalibrationDialogButton btnReload;
        private CalibrationDialogButton btnParameterSave;
        private CalibrationDialogButton btnSave;
        private CalibrationDialogButton btnClose;
        private System.Windows.Forms.GroupBox batchGroup;
        private System.Windows.Forms.FlowLayoutPanel batchFlow;
        private System.Windows.Forms.CheckBox chkBatchAll;
        private System.Windows.Forms.CheckBox chkBatchFront4;
        private System.Windows.Forms.CheckBox chkBatchFront1;
        private System.Windows.Forms.CheckBox chkBatchFront2;
        private System.Windows.Forms.CheckBox chkBatchFront3;
        private System.Windows.Forms.CheckBox chkBatchRear4;
        private System.Windows.Forms.CheckBox chkBatchRear1;
        private System.Windows.Forms.CheckBox chkBatchRear2;
        private System.Windows.Forms.CheckBox chkBatchRear3;
        private CalibrationDialogButton btnBatchStart;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.batchGroup = new System.Windows.Forms.GroupBox();
            this.batchFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.chkBatchAll = new System.Windows.Forms.CheckBox();
            this.chkBatchFront4 = new System.Windows.Forms.CheckBox();
            this.chkBatchFront1 = new System.Windows.Forms.CheckBox();
            this.chkBatchFront2 = new System.Windows.Forms.CheckBox();
            this.chkBatchFront3 = new System.Windows.Forms.CheckBox();
            this.chkBatchRear4 = new System.Windows.Forms.CheckBox();
            this.chkBatchRear1 = new System.Windows.Forms.CheckBox();
            this.chkBatchRear2 = new System.Windows.Forms.CheckBox();
            this.chkBatchRear3 = new System.Windows.Forms.CheckBox();
            this.btnBatchStart = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.groupSettings = new System.Windows.Forms.GroupBox();
            this.settingsLayout = new System.Windows.Forms.TableLayoutPanel();
            this.gridSettings = new System.Windows.Forms.DataGridView();
            this.colSettingName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSettingValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSettingUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupResults = new System.Windows.Forms.GroupBox();
            this.resultsLayout = new System.Windows.Forms.TableLayoutPanel();
            this.gridResults = new System.Windows.Forms.DataGridView();
            this.colItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSide = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCollet = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOffsetX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOffsetY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTheta = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTZero = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFinalX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFinalY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFinalZ = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCocValid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCocPixelX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCocPixelY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCocMachineX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCocMachineY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPickZ = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPlaceZ = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblSaveHistory = new System.Windows.Forms.Label();
            this.lstSaveHistory = new System.Windows.Forms.ListBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnCheck = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnStart = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnCoc = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnCocCenter = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnSaveBottomTeaching = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnApplyHomeOffset = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnMoveZForward = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnMoveYAvoid = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnSeqStop = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnReload = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnParameterSave = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnSave = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnClose = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.rootLayout.SuspendLayout();
            this.batchGroup.SuspendLayout();
            this.batchFlow.SuspendLayout();
            this.mainLayout.SuspendLayout();
            this.groupSettings.SuspendLayout();
            this.settingsLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSettings)).BeginInit();
            this.groupResults.SuspendLayout();
            this.resultsLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridResults)).BeginInit();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.batchGroup, 0, 1);
            this.rootLayout.Controls.Add(this.mainLayout, 0, 2);
            this.rootLayout.Controls.Add(this.lblStatus, 0, 3);
            this.rootLayout.Controls.Add(this.buttonPanel, 0, 4);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 5;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 59F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.rootLayout.Size = new System.Drawing.Size(1323, 814);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(120)))), ((int)(((byte)(0)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 15F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1323, 52);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "COLLET CALIBRATION";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // batchGroup
            // 
            this.batchGroup.Controls.Add(this.batchFlow);
            this.batchGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.batchGroup.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.batchGroup.Location = new System.Drawing.Point(12, 55);
            this.batchGroup.Margin = new System.Windows.Forms.Padding(12, 3, 12, 0);
            this.batchGroup.Name = "batchGroup";
            this.batchGroup.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.batchGroup.Size = new System.Drawing.Size(1299, 56);
            this.batchGroup.TabIndex = 4;
            this.batchGroup.TabStop = false;
            this.batchGroup.Text = "BATCH (일괄 콜렛 캘리브레이션 · 각 side C4 → C3 → C2 → C1 순, 측정 후 COC 연속)";
            // 
            // batchFlow
            // 
            this.batchFlow.Controls.Add(this.chkBatchAll);
            this.batchFlow.Controls.Add(this.chkBatchFront4);
            this.batchFlow.Controls.Add(this.chkBatchFront3);
            this.batchFlow.Controls.Add(this.chkBatchFront2);
            this.batchFlow.Controls.Add(this.chkBatchFront1);
            this.batchFlow.Controls.Add(this.chkBatchRear4);
            this.batchFlow.Controls.Add(this.chkBatchRear3);
            this.batchFlow.Controls.Add(this.chkBatchRear2);
            this.batchFlow.Controls.Add(this.chkBatchRear1);
            this.batchFlow.Controls.Add(this.btnBatchStart);
            this.batchFlow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.batchFlow.Location = new System.Drawing.Point(4, 19);
            this.batchFlow.Margin = new System.Windows.Forms.Padding(0);
            this.batchFlow.Name = "batchFlow";
            this.batchFlow.Size = new System.Drawing.Size(1291, 34);
            this.batchFlow.TabIndex = 0;
            this.batchFlow.WrapContents = false;
            // 
            // chkBatchAll
            // 
            this.chkBatchAll.AutoSize = true;
            this.chkBatchAll.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.chkBatchAll.Location = new System.Drawing.Point(5, 4);
            this.chkBatchAll.Margin = new System.Windows.Forms.Padding(5, 4, 14, 4);
            this.chkBatchAll.Name = "chkBatchAll";
            this.chkBatchAll.Size = new System.Drawing.Size(46, 19);
            this.chkBatchAll.TabIndex = 0;
            this.chkBatchAll.Text = "ALL";
            this.chkBatchAll.UseVisualStyleBackColor = true;
            this.chkBatchAll.CheckedChanged += new System.EventHandler(this.chkBatchAll_CheckedChanged);
            // 
            // chkBatchFront4
            // 
            this.chkBatchFront4.AutoSize = true;
            this.chkBatchFront4.Location = new System.Drawing.Point(70, 4);
            this.chkBatchFront4.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.chkBatchFront4.Name = "chkBatchFront4";
            this.chkBatchFront4.Size = new System.Drawing.Size(51, 19);
            this.chkBatchFront4.TabIndex = 1;
            this.chkBatchFront4.Text = "F C4";
            this.chkBatchFront4.UseVisualStyleBackColor = true;
            // 
            // chkBatchFront1
            // 
            this.chkBatchFront1.AutoSize = true;
            this.chkBatchFront1.Location = new System.Drawing.Point(253, 4);
            this.chkBatchFront1.Margin = new System.Windows.Forms.Padding(5, 4, 14, 4);
            this.chkBatchFront1.Name = "chkBatchFront1";
            this.chkBatchFront1.Size = new System.Drawing.Size(51, 19);
            this.chkBatchFront1.TabIndex = 4;
            this.chkBatchFront1.Text = "F C1";
            this.chkBatchFront1.UseVisualStyleBackColor = true;
            // 
            // chkBatchFront2
            // 
            this.chkBatchFront2.AutoSize = true;
            this.chkBatchFront2.Location = new System.Drawing.Point(192, 4);
            this.chkBatchFront2.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.chkBatchFront2.Name = "chkBatchFront2";
            this.chkBatchFront2.Size = new System.Drawing.Size(51, 19);
            this.chkBatchFront2.TabIndex = 3;
            this.chkBatchFront2.Text = "F C2";
            this.chkBatchFront2.UseVisualStyleBackColor = true;
            // 
            // chkBatchFront3
            // 
            this.chkBatchFront3.AutoSize = true;
            this.chkBatchFront3.Location = new System.Drawing.Point(131, 4);
            this.chkBatchFront3.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.chkBatchFront3.Name = "chkBatchFront3";
            this.chkBatchFront3.Size = new System.Drawing.Size(51, 19);
            this.chkBatchFront3.TabIndex = 2;
            this.chkBatchFront3.Text = "F C3";
            this.chkBatchFront3.UseVisualStyleBackColor = true;
            // 
            // chkBatchRear4
            // 
            this.chkBatchRear4.AutoSize = true;
            this.chkBatchRear4.Location = new System.Drawing.Point(323, 4);
            this.chkBatchRear4.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.chkBatchRear4.Name = "chkBatchRear4";
            this.chkBatchRear4.Size = new System.Drawing.Size(53, 19);
            this.chkBatchRear4.TabIndex = 5;
            this.chkBatchRear4.Text = "R C4";
            this.chkBatchRear4.UseVisualStyleBackColor = true;
            // 
            // chkBatchRear1
            // 
            this.chkBatchRear1.AutoSize = true;
            this.chkBatchRear1.Location = new System.Drawing.Point(512, 4);
            this.chkBatchRear1.Margin = new System.Windows.Forms.Padding(5, 4, 19, 4);
            this.chkBatchRear1.Name = "chkBatchRear1";
            this.chkBatchRear1.Size = new System.Drawing.Size(53, 19);
            this.chkBatchRear1.TabIndex = 8;
            this.chkBatchRear1.Text = "R C1";
            this.chkBatchRear1.UseVisualStyleBackColor = true;
            // 
            // chkBatchRear2
            // 
            this.chkBatchRear2.AutoSize = true;
            this.chkBatchRear2.Location = new System.Drawing.Point(449, 4);
            this.chkBatchRear2.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.chkBatchRear2.Name = "chkBatchRear2";
            this.chkBatchRear2.Size = new System.Drawing.Size(53, 19);
            this.chkBatchRear2.TabIndex = 7;
            this.chkBatchRear2.Text = "R C2";
            this.chkBatchRear2.UseVisualStyleBackColor = true;
            // 
            // chkBatchRear3
            // 
            this.chkBatchRear3.AutoSize = true;
            this.chkBatchRear3.Location = new System.Drawing.Point(386, 4);
            this.chkBatchRear3.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.chkBatchRear3.Name = "chkBatchRear3";
            this.chkBatchRear3.Size = new System.Drawing.Size(53, 19);
            this.chkBatchRear3.TabIndex = 6;
            this.chkBatchRear3.Text = "R C3";
            this.chkBatchRear3.UseVisualStyleBackColor = true;
            // 
            // btnBatchStart
            // 
            this.btnBatchStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(126)))), ((int)(((byte)(0)))));
            this.btnBatchStart.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBatchStart.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnBatchStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBatchStart.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnBatchStart.ForeColor = System.Drawing.Color.White;
            this.btnBatchStart.Location = new System.Drawing.Point(591, 4);
            this.btnBatchStart.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnBatchStart.Name = "btnBatchStart";
            this.btnBatchStart.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Primary;
            this.btnBatchStart.Size = new System.Drawing.Size(198, 31);
            this.btnBatchStart.TabIndex = 10;
            this.btnBatchStart.Text = "BATCH START";
            this.btnBatchStart.UseVisualStyleBackColor = false;
            this.btnBatchStart.Click += new System.EventHandler(this.btnBatchStart_Click);
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 2;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 394F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.groupSettings, 0, 0);
            this.mainLayout.Controls.Add(this.groupResults, 1, 0);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(12, 123);
            this.mainLayout.Margin = new System.Windows.Forms.Padding(12, 12, 12, 12);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.RowCount = 1;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Size = new System.Drawing.Size(1299, 551);
            this.mainLayout.TabIndex = 1;
            // 
            // groupSettings
            // 
            this.groupSettings.Controls.Add(this.settingsLayout);
            this.groupSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupSettings.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.groupSettings.Location = new System.Drawing.Point(0, 0);
            this.groupSettings.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.groupSettings.Name = "groupSettings";
            this.groupSettings.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupSettings.Size = new System.Drawing.Size(384, 551);
            this.groupSettings.TabIndex = 0;
            this.groupSettings.TabStop = false;
            this.groupSettings.Text = "SETTING";
            //
            // settingsLayout
            //
            this.settingsLayout.ColumnCount = 1;
            this.settingsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.settingsLayout.Controls.Add(this.gridSettings, 0, 0);
            this.settingsLayout.Controls.Add(this.btnParameterSave, 0, 1);
            this.settingsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsLayout.Location = new System.Drawing.Point(4, 21);
            this.settingsLayout.Margin = new System.Windows.Forms.Padding(0);
            this.settingsLayout.Name = "settingsLayout";
            this.settingsLayout.RowCount = 2;
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.settingsLayout.Size = new System.Drawing.Size(376, 527);
            this.settingsLayout.TabIndex = 0;
            //
            // gridSettings
            // 
            this.gridSettings.AllowUserToAddRows = false;
            this.gridSettings.AllowUserToDeleteRows = false;
            this.gridSettings.AllowUserToResizeRows = false;
            this.gridSettings.BackgroundColor = System.Drawing.Color.White;
            this.gridSettings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridSettings.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSettingName,
            this.colSettingValue,
            this.colSettingUnit});
            this.gridSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSettings.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.gridSettings.Location = new System.Drawing.Point(0, 0);
            this.gridSettings.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.gridSettings.MultiSelect = false;
            this.gridSettings.Name = "gridSettings";
            this.gridSettings.RowHeadersVisible = false;
            this.gridSettings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridSettings.Size = new System.Drawing.Size(376, 481);
            this.gridSettings.TabIndex = 0;
            this.gridSettings.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.gridSettings_CellBeginEdit);
            this.gridSettings.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridSettings_CellDoubleClick);
            this.gridSettings.CellToolTipTextNeeded += new System.Windows.Forms.DataGridViewCellToolTipTextNeededEventHandler(this.gridSettings_CellToolTipTextNeeded);
            this.gridSettings.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridSettings_CellValueChanged);
            this.gridSettings.CurrentCellDirtyStateChanged += new System.EventHandler(this.gridSettings_CurrentCellDirtyStateChanged);
            // 
            // colSettingName
            // 
            this.colSettingName.HeaderText = "PARAMETER";
            this.colSettingName.Name = "colSettingName";
            this.colSettingName.ReadOnly = true;
            this.colSettingName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colSettingName.Width = 155;
            // 
            // colSettingValue
            // 
            this.colSettingValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colSettingValue.HeaderText = "VALUE";
            this.colSettingValue.Name = "colSettingValue";
            this.colSettingValue.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colSettingUnit
            // 
            this.colSettingUnit.HeaderText = "UNIT";
            this.colSettingUnit.Name = "colSettingUnit";
            this.colSettingUnit.ReadOnly = true;
            this.colSettingUnit.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colSettingUnit.Width = 50;
            // 
            // groupResults
            // 
            this.groupResults.Controls.Add(this.resultsLayout);
            this.groupResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupResults.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.groupResults.Location = new System.Drawing.Point(394, 0);
            this.groupResults.Margin = new System.Windows.Forms.Padding(0);
            this.groupResults.Name = "groupResults";
            this.groupResults.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupResults.Size = new System.Drawing.Size(905, 551);
            this.groupResults.TabIndex = 1;
            this.groupResults.TabStop = false;
            this.groupResults.Text = "SAVED COLLET OFFSET";
            // 
            // resultsLayout
            // 
            this.resultsLayout.ColumnCount = 1;
            this.resultsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.resultsLayout.Controls.Add(this.gridResults, 0, 0);
            this.resultsLayout.Controls.Add(this.lblSaveHistory, 0, 1);
            this.resultsLayout.Controls.Add(this.lstSaveHistory, 0, 2);
            this.resultsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resultsLayout.Location = new System.Drawing.Point(4, 21);
            this.resultsLayout.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.resultsLayout.Name = "resultsLayout";
            this.resultsLayout.RowCount = 3;
            this.resultsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 189F));
            this.resultsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.resultsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.resultsLayout.Size = new System.Drawing.Size(897, 527);
            this.resultsLayout.TabIndex = 1;
            // 
            // gridResults
            // 
            this.gridResults.AllowUserToAddRows = false;
            this.gridResults.AllowUserToDeleteRows = false;
            this.gridResults.BackgroundColor = System.Drawing.Color.White;
            this.gridResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridResults.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colItem,
            this.colSide,
            this.colCollet,
            this.colOffsetX,
            this.colOffsetY,
            this.colTheta,
            this.colTZero,
            this.colFinalX,
            this.colFinalY,
            this.colFinalZ,
            this.colValid,
            this.colCocValid,
            this.colCocPixelX,
            this.colCocPixelY,
            this.colCocMachineX,
            this.colCocMachineY,
            this.colPickZ,
            this.colPlaceZ});
            this.gridResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridResults.Location = new System.Drawing.Point(0, 0);
            this.gridResults.Margin = new System.Windows.Forms.Padding(0);
            this.gridResults.MultiSelect = false;
            this.gridResults.Name = "gridResults";
            this.gridResults.ReadOnly = true;
            this.gridResults.RowHeadersVisible = false;
            this.gridResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridResults.Size = new System.Drawing.Size(897, 189);
            this.gridResults.TabIndex = 0;
            // 
            // colItem
            // 
            this.colItem.HeaderText = "ITEM";
            this.colItem.Name = "colItem";
            this.colItem.ReadOnly = true;
            this.colItem.ToolTipText = "저장된 Collet Calibration 항목입니다.";
            this.colItem.Width = 80;
            // 
            // colSide
            // 
            this.colSide.HeaderText = "SIDE";
            this.colSide.Name = "colSide";
            this.colSide.ReadOnly = true;
            this.colSide.ToolTipText = "Front 또는 Rear Picker Side입니다.";
            this.colSide.Width = 60;
            // 
            // colCollet
            // 
            this.colCollet.HeaderText = "COLLET";
            this.colCollet.Name = "colCollet";
            this.colCollet.ReadOnly = true;
            this.colCollet.ToolTipText = "Collet 번호입니다. #4는 Bottom 기준 Collet로 사용합니다.";
            this.colCollet.Width = 60;
            // 
            // colOffsetX
            // 
            this.colOffsetX.HeaderText = "OFFSET X";
            this.colOffsetX.Name = "colOffsetX";
            this.colOffsetX.ReadOnly = true;
            this.colOffsetX.ToolTipText = "#3/#2/#1은 #4 Bottom 기준 피치 위치에서 최종 OK 위치까지의 X 보정량입니다. #4는 기준이므로 0으로 저장합니다.";
            this.colOffsetX.Width = 85;
            // 
            // colOffsetY
            // 
            this.colOffsetY.HeaderText = "OFFSET Y";
            this.colOffsetY.Name = "colOffsetY";
            this.colOffsetY.ReadOnly = true;
            this.colOffsetY.ToolTipText = "#3/#2/#1은 #4 Bottom 기준 위치에서 최종 OK 위치까지의 Y 보정량입니다. #4는 기준이므로 0으로 저장합니다.";
            this.colOffsetY.Width = 85;
            // 
            // colTheta
            // 
            this.colTheta.HeaderText = "THETA";
            this.colTheta.Name = "colTheta";
            this.colTheta.ReadOnly = true;
            this.colTheta.ToolTipText = "최종 Vision 결과의 T/Theta 보정값입니다.";
            this.colTheta.Width = 80;
            // 
            // colTZero
            // 
            this.colTZero.HeaderText = "T ZERO";
            this.colTZero.Name = "colTZero";
            this.colTZero.ReadOnly = true;
            this.colTZero.ToolTipText = "현재 적용 중인 PC Offset에 캘리브레이션 잔여 T 오차를 더한 절대 보정값입니다. APPLY T HOME 시 선택 T축의 현재 보드 Com" +
    "mand/Actual 좌표를 0으로 프리셋하고, Picker T 홈 완료 후 이동/0점 설정값으로도 사용합니다.";
            this.colTZero.Width = 80;
            // 
            // colFinalX
            // 
            this.colFinalX.HeaderText = "FINAL X";
            this.colFinalX.Name = "colFinalX";
            this.colFinalX.ReadOnly = true;
            this.colFinalX.ToolTipText = "캘리브레이션이 OK로 끝난 최종 Picker X 실제 위치입니다.";
            this.colFinalX.Width = 85;
            // 
            // colFinalY
            // 
            this.colFinalY.HeaderText = "FINAL Y";
            this.colFinalY.Name = "colFinalY";
            this.colFinalY.ReadOnly = true;
            this.colFinalY.ToolTipText = "캘리브레이션이 OK로 끝난 최종 Picker Y 실제 위치입니다.";
            this.colFinalY.Width = 85;
            // 
            // colFinalZ
            // 
            this.colFinalZ.HeaderText = "FINAL Z";
            this.colFinalZ.Name = "colFinalZ";
            this.colFinalZ.ReadOnly = true;
            this.colFinalZ.ToolTipText = "캘리브레이션이 OK로 끝난 AutoFocus Best Z/최종 Picker Z 실제 위치입니다.";
            this.colFinalZ.Width = 85;
            // 
            // colValid
            // 
            this.colValid.HeaderText = "VALID";
            this.colValid.Name = "colValid";
            this.colValid.ReadOnly = true;
            this.colValid.ToolTipText = "이 Collet Calibration 결과가 검사 이동 보정에 사용 가능한 상태인지 표시합니다.";
            this.colValid.Width = 55;
            // 
            // colCocValid
            // 
            this.colCocValid.HeaderText = "COC";
            this.colCocValid.Name = "colCocValid";
            this.colCocValid.ReadOnly = true;
            this.colCocValid.ToolTipText = "COC(회전 중심) 검출 결과의 유효 여부입니다.";
            this.colCocValid.Width = 50;
            // 
            // colCocPixelX
            // 
            this.colCocPixelX.HeaderText = "COC Xpx";
            this.colCocPixelX.Name = "colCocPixelX";
            this.colCocPixelX.ReadOnly = true;
            this.colCocPixelX.ToolTipText = "COC 회전 중심 X (픽셀). Bottom Camera 픽셀 좌표계 기준입니다.";
            this.colCocPixelX.Width = 85;
            // 
            // colCocPixelY
            // 
            this.colCocPixelY.HeaderText = "COC Ypx";
            this.colCocPixelY.Name = "colCocPixelY";
            this.colCocPixelY.ReadOnly = true;
            this.colCocPixelY.ToolTipText = "COC 회전 중심 Y (픽셀). Bottom Camera 픽셀 좌표계 기준입니다.";
            this.colCocPixelY.Width = 85;
            // 
            // colCocMachineX
            // 
            this.colCocMachineX.HeaderText = "COC Xmm";
            this.colCocMachineX.Name = "colCocMachineX";
            this.colCocMachineX.ReadOnly = true;
            this.colCocMachineX.ToolTipText = "COC 회전 중심 X (기계 좌표, mm). Recipe에 저장된 PickerX 회전 중심입니다.";
            this.colCocMachineX.Width = 90;
            // 
            // colCocMachineY
            // 
            this.colCocMachineY.HeaderText = "COC Ymm";
            this.colCocMachineY.Name = "colCocMachineY";
            this.colCocMachineY.ReadOnly = true;
            this.colCocMachineY.ToolTipText = "COC 회전 중심 Y (기계 좌표, mm). Recipe에 저장된 PickerY 회전 중심입니다.";
            this.colCocMachineY.Width = 95;
            //
            // colPickZ
            //
            this.colPickZ.HeaderText = "PICK Z";
            this.colPickZ.Name = "colPickZ";
            this.colPickZ.ReadOnly = true;
            this.colPickZ.ToolTipText = "현재 Pick Z 티칭값(DiePickPosition, mm)입니다. 수정은 Recipe 화면에서 합니다.";
            this.colPickZ.Width = 90;
            //
            // colPlaceZ
            //
            this.colPlaceZ.HeaderText = "PLACE Z";
            this.colPlaceZ.Name = "colPlaceZ";
            this.colPlaceZ.ReadOnly = true;
            this.colPlaceZ.ToolTipText = "현재 Place Z 티칭값(DiePlacePosition, mm)입니다. 수정은 Recipe 화면에서 합니다.";
            this.colPlaceZ.Width = 90;
            //
            // lblSaveHistory
            // 
            this.lblSaveHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSaveHistory.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblSaveHistory.Location = new System.Drawing.Point(0, 189);
            this.lblSaveHistory.Margin = new System.Windows.Forms.Padding(0);
            this.lblSaveHistory.Name = "lblSaveHistory";
            this.lblSaveHistory.Size = new System.Drawing.Size(897, 22);
            this.lblSaveHistory.TabIndex = 1;
            this.lblSaveHistory.Text = "SAVE HISTORY";
            this.lblSaveHistory.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lstSaveHistory
            // 
            this.lstSaveHistory.BackColor = System.Drawing.Color.White;
            this.lstSaveHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstSaveHistory.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lstSaveHistory.FormattingEnabled = true;
            this.lstSaveHistory.HorizontalScrollbar = true;
            this.lstSaveHistory.IntegralHeight = false;
            this.lstSaveHistory.ItemHeight = 15;
            this.lstSaveHistory.Location = new System.Drawing.Point(0, 211);
            this.lstSaveHistory.Margin = new System.Windows.Forms.Padding(0);
            this.lstSaveHistory.Name = "lstSaveHistory";
            this.lstSaveHistory.Size = new System.Drawing.Size(897, 316);
            this.lstSaveHistory.TabIndex = 2;
            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblStatus.Location = new System.Drawing.Point(12, 686);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(1299, 48);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "대기 중입니다.";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonPanel
            // 
            this.buttonPanel.ColumnCount = 12;
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.buttonPanel.Controls.Add(this.btnCheck, 0, 0);
            this.buttonPanel.Controls.Add(this.btnStart, 1, 0);
            this.buttonPanel.Controls.Add(this.btnCoc, 2, 0);
            this.buttonPanel.Controls.Add(this.btnCocCenter, 3, 0);
            this.buttonPanel.Controls.Add(this.btnSaveBottomTeaching, 4, 0);
            this.buttonPanel.Controls.Add(this.btnApplyHomeOffset, 5, 0);
            this.buttonPanel.Controls.Add(this.btnMoveZForward, 6, 0);
            this.buttonPanel.Controls.Add(this.btnMoveYAvoid, 7, 0);
            this.buttonPanel.Controls.Add(this.btnSeqStop, 8, 0);
            this.buttonPanel.Controls.Add(this.btnReload, 9, 0);
            this.buttonPanel.Controls.Add(this.btnSave, 10, 0);
            this.buttonPanel.Controls.Add(this.btnClose, 11, 0);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.Location = new System.Drawing.Point(12, 734);
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(12, 0, 12, 12);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.RowCount = 1;
            this.buttonPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonPanel.Size = new System.Drawing.Size(1299, 68);
            this.buttonPanel.TabIndex = 3;
            // 
            // btnCheck
            // 
            this.btnCheck.BackColor = System.Drawing.Color.White;
            this.btnCheck.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCheck.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnCheck.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCheck.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnCheck.ForeColor = System.Drawing.Color.Black;
            this.btnCheck.Location = new System.Drawing.Point(7, 4);
            this.btnCheck.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnCheck.Name = "btnCheck";
            this.btnCheck.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnCheck.Size = new System.Drawing.Size(94, 60);
            this.btnCheck.TabIndex = 0;
            this.btnCheck.Text = "CHECK";
            this.btnCheck.UseVisualStyleBackColor = false;
            this.btnCheck.Click += new System.EventHandler(this.btnCheck_Click);
            // 
            // btnStart
            // 
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(126)))), ((int)(((byte)(0)))));
            this.btnStart.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStart.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.Location = new System.Drawing.Point(115, 4);
            this.btnStart.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnStart.Name = "btnStart";
            this.btnStart.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Primary;
            this.btnStart.Size = new System.Drawing.Size(94, 60);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "START";
            this.btnStart.UseVisualStyleBackColor = false;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnCoc
            // 
            this.btnCoc.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(126)))), ((int)(((byte)(0)))));
            this.btnCoc.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCoc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCoc.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnCoc.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCoc.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnCoc.ForeColor = System.Drawing.Color.White;
            this.btnCoc.Location = new System.Drawing.Point(223, 4);
            this.btnCoc.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnCoc.Name = "btnCoc";
            this.btnCoc.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Primary;
            this.btnCoc.Size = new System.Drawing.Size(94, 60);
            this.btnCoc.TabIndex = 2;
            this.btnCoc.Text = "COC START";
            this.btnCoc.UseVisualStyleBackColor = false;
            this.btnCoc.Click += new System.EventHandler(this.btnCoc_Click);
            // 
            // btnCocCenter
            // 
            this.btnCocCenter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(126)))), ((int)(((byte)(0)))));
            this.btnCocCenter.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCocCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCocCenter.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnCocCenter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCocCenter.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnCocCenter.ForeColor = System.Drawing.Color.White;
            this.btnCocCenter.Location = new System.Drawing.Point(331, 4);
            this.btnCocCenter.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnCocCenter.Name = "btnCocCenter";
            this.btnCocCenter.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Primary;
            this.btnCocCenter.Size = new System.Drawing.Size(94, 60);
            this.btnCocCenter.TabIndex = 3;
            this.btnCocCenter.Text = "COC \r\nRE-CAL";
            this.btnCocCenter.UseVisualStyleBackColor = false;
            this.btnCocCenter.Click += new System.EventHandler(this.btnCocCenter_Click);
            // 
            // btnSaveBottomTeaching
            // 
            this.btnSaveBottomTeaching.BackColor = System.Drawing.Color.White;
            this.btnSaveBottomTeaching.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSaveBottomTeaching.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveBottomTeaching.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnSaveBottomTeaching.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveBottomTeaching.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSaveBottomTeaching.ForeColor = System.Drawing.Color.Black;
            this.btnSaveBottomTeaching.Location = new System.Drawing.Point(439, 4);
            this.btnSaveBottomTeaching.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnSaveBottomTeaching.Name = "btnSaveBottomTeaching";
            this.btnSaveBottomTeaching.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnSaveBottomTeaching.Size = new System.Drawing.Size(94, 60);
            this.btnSaveBottomTeaching.TabIndex = 2;
            this.btnSaveBottomTeaching.Text = "SAVE BOT.\r\nPOS";
            this.btnSaveBottomTeaching.UseVisualStyleBackColor = false;
            this.btnSaveBottomTeaching.Click += new System.EventHandler(this.btnSaveBottomTeaching_Click);
            // 
            // btnApplyHomeOffset
            // 
            this.btnApplyHomeOffset.BackColor = System.Drawing.Color.White;
            this.btnApplyHomeOffset.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnApplyHomeOffset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApplyHomeOffset.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnApplyHomeOffset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApplyHomeOffset.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnApplyHomeOffset.ForeColor = System.Drawing.Color.Black;
            this.btnApplyHomeOffset.Location = new System.Drawing.Point(547, 4);
            this.btnApplyHomeOffset.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnApplyHomeOffset.Name = "btnApplyHomeOffset";
            this.btnApplyHomeOffset.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnApplyHomeOffset.Size = new System.Drawing.Size(94, 60);
            this.btnApplyHomeOffset.TabIndex = 3;
            this.btnApplyHomeOffset.Text = "APPLY T\r\nHOME";
            this.btnApplyHomeOffset.UseVisualStyleBackColor = false;
            this.btnApplyHomeOffset.Click += new System.EventHandler(this.btnApplyHomeOffset_Click);
            // 
            // btnMoveZForward
            // 
            this.btnMoveZForward.BackColor = System.Drawing.Color.White;
            this.btnMoveZForward.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMoveZForward.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMoveZForward.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnMoveZForward.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMoveZForward.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnMoveZForward.ForeColor = System.Drawing.Color.Black;
            this.btnMoveZForward.Location = new System.Drawing.Point(655, 4);
            this.btnMoveZForward.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnMoveZForward.Name = "btnMoveZForward";
            this.btnMoveZForward.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnMoveZForward.Size = new System.Drawing.Size(94, 60);
            this.btnMoveZForward.TabIndex = 4;
            this.btnMoveZForward.Text = "Z-AVOID";
            this.btnMoveZForward.UseVisualStyleBackColor = false;
            this.btnMoveZForward.Click += new System.EventHandler(this.btnMoveZForward_Click);
            // 
            // btnMoveYAvoid
            // 
            this.btnMoveYAvoid.BackColor = System.Drawing.Color.White;
            this.btnMoveYAvoid.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMoveYAvoid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMoveYAvoid.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnMoveYAvoid.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMoveYAvoid.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnMoveYAvoid.ForeColor = System.Drawing.Color.Black;
            this.btnMoveYAvoid.Location = new System.Drawing.Point(763, 4);
            this.btnMoveYAvoid.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnMoveYAvoid.Name = "btnMoveYAvoid";
            this.btnMoveYAvoid.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnMoveYAvoid.Size = new System.Drawing.Size(94, 60);
            this.btnMoveYAvoid.TabIndex = 5;
            this.btnMoveYAvoid.Text = "P-Y AVOID";
            this.btnMoveYAvoid.UseVisualStyleBackColor = false;
            this.btnMoveYAvoid.Click += new System.EventHandler(this.btnMoveYAvoid_Click);
            // 
            // btnSeqStop
            // 
            this.btnSeqStop.BackColor = System.Drawing.Color.White;
            this.btnSeqStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSeqStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSeqStop.Enabled = false;
            this.btnSeqStop.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnSeqStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSeqStop.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSeqStop.ForeColor = System.Drawing.Color.Black;
            this.btnSeqStop.Location = new System.Drawing.Point(871, 4);
            this.btnSeqStop.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnSeqStop.Name = "btnSeqStop";
            this.btnSeqStop.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnSeqStop.Size = new System.Drawing.Size(94, 60);
            this.btnSeqStop.TabIndex = 6;
            this.btnSeqStop.Text = "SEQ STOP";
            this.btnSeqStop.UseVisualStyleBackColor = false;
            this.btnSeqStop.Click += new System.EventHandler(this.btnSeqStop_Click);
            // 
            // btnReload
            // 
            this.btnReload.BackColor = System.Drawing.Color.White;
            this.btnReload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReload.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnReload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReload.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnReload.ForeColor = System.Drawing.Color.Black;
            this.btnReload.Location = new System.Drawing.Point(979, 4);
            this.btnReload.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnReload.Name = "btnReload";
            this.btnReload.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnReload.Size = new System.Drawing.Size(94, 60);
            this.btnReload.TabIndex = 7;
            this.btnReload.Text = "RELOAD";
            this.btnReload.UseVisualStyleBackColor = false;
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            //
            // btnParameterSave
            //
            this.btnParameterSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.btnParameterSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnParameterSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnParameterSave.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnParameterSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnParameterSave.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnParameterSave.ForeColor = System.Drawing.Color.White;
            this.btnParameterSave.Location = new System.Drawing.Point(4, 485);
            this.btnParameterSave.Margin = new System.Windows.Forms.Padding(4);
            this.btnParameterSave.Name = "btnParameterSave";
            this.btnParameterSave.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Dark;
            this.btnParameterSave.Size = new System.Drawing.Size(368, 38);
            this.btnParameterSave.TabIndex = 1;
            this.btnParameterSave.Text = "PARAMETER SAVE";
            this.btnParameterSave.UseVisualStyleBackColor = false;
            this.btnParameterSave.Click += new System.EventHandler(this.btnParameterSave_Click);
            //
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(1087, 4);
            this.btnSave.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnSave.Name = "btnSave";
            this.btnSave.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Dark;
            this.btnSave.Size = new System.Drawing.Size(94, 60);
            this.btnSave.TabIndex = 8;
            this.btnSave.Text = "SAVE RESULT";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.Color.White;
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnClose.ForeColor = System.Drawing.Color.Black;
            this.btnClose.Location = new System.Drawing.Point(1195, 4);
            this.btnClose.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnClose.Name = "btnClose";
            this.btnClose.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnClose.Size = new System.Drawing.Size(97, 60);
            this.btnClose.TabIndex = 9;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // ColletCalibrationDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1323, 814);
            this.Controls.Add(this.rootLayout);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MinimumSize = new System.Drawing.Size(980, 600);
            this.Name = "ColletCalibrationDialog";
            this.Text = "COLLET CALIBRATION";
            this.rootLayout.ResumeLayout(false);
            this.batchGroup.ResumeLayout(false);
            this.batchFlow.ResumeLayout(false);
            this.batchFlow.PerformLayout();
            this.mainLayout.ResumeLayout(false);
            this.groupSettings.ResumeLayout(false);
            this.settingsLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridSettings)).EndInit();
            this.groupResults.ResumeLayout(false);
            this.resultsLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridResults)).EndInit();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}


