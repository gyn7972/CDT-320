namespace QMC.CDT_320.Ui.Dialogs
{
    partial class VisionFocusCalibrationDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.GroupBox groupSetting;
        private System.Windows.Forms.DataGridView gridSettings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingUnit;
        private System.Windows.Forms.GroupBox groupResult;
        private System.Windows.Forms.DataGridView gridSamples;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPosition;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScore;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResult;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRaw;
        private System.Windows.Forms.GroupBox groupSaved;
        private System.Windows.Forms.DataGridView gridSaved;
        private System.Windows.Forms.DataGridViewTextBoxColumn colItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDefaultPos;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBestPos;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBestScore;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPickerZ;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAutoFocusCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAutoFocusWafer;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValid;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TableLayoutPanel buttonPanel;
        private CalibrationDialogButton btnCheck;
        private CalibrationDialogButton btnUseCurrent;
        private CalibrationDialogButton btnMoveDefault;
        private CalibrationDialogButton btnMoveZAvoid;
        private CalibrationDialogButton btnMoveYAvoid;
        private CalibrationDialogButton btnStartScan;
        private CalibrationDialogButton btnSeqStop;
        private CalibrationDialogButton btnApplyBest;
        private CalibrationDialogButton btnResetAutoFocus;
        private CalibrationDialogButton btnReload;
        private CalibrationDialogButton btnSave;
        private CalibrationDialogButton btnClose;

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
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.groupSetting = new System.Windows.Forms.GroupBox();
            this.gridSettings = new System.Windows.Forms.DataGridView();
            this.colSettingName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSettingValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSettingUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupResult = new System.Windows.Forms.GroupBox();
            this.gridSamples = new System.Windows.Forms.DataGridView();
            this.colNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPosition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRaw = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupSaved = new System.Windows.Forms.GroupBox();
            this.gridSaved = new System.Windows.Forms.DataGridView();
            this.colItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDefaultPos = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBestPos = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBestScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPickerZ = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAutoFocusCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAutoFocusWafer = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblStatus = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnCheck = new CalibrationDialogButton();
            this.btnUseCurrent = new CalibrationDialogButton();
            this.btnMoveDefault = new CalibrationDialogButton();
            this.btnMoveZAvoid = new CalibrationDialogButton();
            this.btnMoveYAvoid = new CalibrationDialogButton();
            this.btnStartScan = new CalibrationDialogButton();
            this.btnSeqStop = new CalibrationDialogButton();
            this.btnApplyBest = new CalibrationDialogButton();
            this.btnResetAutoFocus = new CalibrationDialogButton();
            this.btnReload = new CalibrationDialogButton();
            this.btnSave = new CalibrationDialogButton();
            this.btnClose = new CalibrationDialogButton();
            this.rootLayout.SuspendLayout();
            this.mainLayout.SuspendLayout();
            this.groupSetting.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSettings)).BeginInit();
            this.groupResult.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSamples)).BeginInit();
            this.groupSaved.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSaved)).BeginInit();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.mainLayout, 0, 1);
            this.rootLayout.Controls.Add(this.lblStatus, 0, 2);
            this.rootLayout.Controls.Add(this.buttonPanel, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 67F));
            this.rootLayout.Size = new System.Drawing.Size(1018, 758);
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
            this.lblHeader.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1018, 56);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "VISION FOCUS CAL";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 2;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 469F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.groupSetting, 0, 0);
            this.mainLayout.Controls.Add(this.groupResult, 1, 0);
            this.mainLayout.Controls.Add(this.groupSaved, 0, 1);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(10, 69);
            this.mainLayout.Margin = new System.Windows.Forms.Padding(10, 13, 10, 13);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.RowCount = 2;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 64F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 36F));
            this.mainLayout.Size = new System.Drawing.Size(998, 559);
            this.mainLayout.TabIndex = 1;
            // 
            // groupSetting
            // 
            this.groupSetting.Controls.Add(this.gridSettings);
            this.groupSetting.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupSetting.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.groupSetting.Location = new System.Drawing.Point(0, 0);
            this.groupSetting.Margin = new System.Windows.Forms.Padding(0, 0, 9, 9);
            this.groupSetting.Name = "groupSetting";
            this.groupSetting.Size = new System.Drawing.Size(460, 348);
            this.groupSetting.TabIndex = 0;
            this.groupSetting.TabStop = false;
            this.groupSetting.Text = "SCAN SETTING";
            // 
            // gridSettings
            // 
            this.gridSettings.AllowUserToAddRows = false;
            this.gridSettings.AllowUserToDeleteRows = false;
            this.gridSettings.AllowUserToResizeRows = false;
            this.gridSettings.BackgroundColor = System.Drawing.Color.White;
            this.gridSettings.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridSettings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridSettings.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSettingName,
            this.colSettingValue,
            this.colSettingUnit});
            this.gridSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSettings.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.gridSettings.Location = new System.Drawing.Point(3, 21);
            this.gridSettings.MultiSelect = false;
            this.gridSettings.Name = "gridSettings";
            this.gridSettings.RowHeadersVisible = false;
            this.gridSettings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridSettings.Size = new System.Drawing.Size(454, 324);
            this.gridSettings.TabIndex = 0;
            this.gridSettings.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.gridSettings_CellBeginEdit);
            this.gridSettings.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridSettings_CellClick);
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
            this.colSettingName.Width = 178;
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
            this.colSettingUnit.Width = 55;
            // 
            // groupResult
            // 
            this.groupResult.Controls.Add(this.gridSamples);
            this.groupResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupResult.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.groupResult.Location = new System.Drawing.Point(469, 0);
            this.groupResult.Margin = new System.Windows.Forms.Padding(0, 0, 0, 9);
            this.groupResult.Name = "groupResult";
            this.mainLayout.SetRowSpan(this.groupResult, 2);
            this.groupResult.Size = new System.Drawing.Size(529, 550);
            this.groupResult.TabIndex = 1;
            this.groupResult.TabStop = false;
            this.groupResult.Text = "SCAN SAMPLE";
            // 
            // gridSamples
            // 
            this.gridSamples.AllowUserToAddRows = false;
            this.gridSamples.AllowUserToDeleteRows = false;
            this.gridSamples.AllowUserToResizeRows = false;
            this.gridSamples.BackgroundColor = System.Drawing.Color.White;
            this.gridSamples.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridSamples.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colNo,
            this.colPosition,
            this.colScore,
            this.colResult,
            this.colRaw});
            this.gridSamples.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSamples.Location = new System.Drawing.Point(3, 21);
            this.gridSamples.MultiSelect = false;
            this.gridSamples.Name = "gridSamples";
            this.gridSamples.ReadOnly = true;
            this.gridSamples.RowHeadersVisible = false;
            this.gridSamples.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridSamples.Size = new System.Drawing.Size(523, 526);
            this.gridSamples.TabIndex = 0;
            // 
            // colNo
            // 
            this.colNo.HeaderText = "NO";
            this.colNo.Name = "colNo";
            this.colNo.ReadOnly = true;
            this.colNo.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colNo.Width = 56;
            // 
            // colPosition
            // 
            this.colPosition.HeaderText = "POSITION";
            this.colPosition.Name = "colPosition";
            this.colPosition.ReadOnly = true;
            this.colPosition.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colPosition.Width = 120;
            // 
            // colScore
            // 
            this.colScore.HeaderText = "SCORE";
            this.colScore.Name = "colScore";
            this.colScore.ReadOnly = true;
            this.colScore.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colScore.Width = 110;
            // 
            // colResult
            // 
            this.colResult.HeaderText = "RESULT";
            this.colResult.Name = "colResult";
            this.colResult.ReadOnly = true;
            this.colResult.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colResult.Width = 90;
            // 
            // colRaw
            // 
            this.colRaw.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colRaw.HeaderText = "RAW";
            this.colRaw.Name = "colRaw";
            this.colRaw.ReadOnly = true;
            this.colRaw.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // groupSaved
            // 
            this.groupSaved.Controls.Add(this.gridSaved);
            this.groupSaved.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupSaved.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.groupSaved.Location = new System.Drawing.Point(0, 357);
            this.groupSaved.Margin = new System.Windows.Forms.Padding(0, 0, 9, 0);
            this.groupSaved.Name = "groupSaved";
            this.groupSaved.Size = new System.Drawing.Size(460, 202);
            this.groupSaved.TabIndex = 2;
            this.groupSaved.TabStop = false;
            this.groupSaved.Text = "SAVED RESULT";
            // 
            // gridSaved
            // 
            this.gridSaved.AllowUserToAddRows = false;
            this.gridSaved.AllowUserToDeleteRows = false;
            this.gridSaved.AllowUserToResizeRows = false;
            this.gridSaved.BackgroundColor = System.Drawing.Color.White;
            this.gridSaved.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridSaved.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colItem,
            this.colDefaultPos,
            this.colBestPos,
            this.colBestScore,
            this.colPickerZ,
            this.colAutoFocusCount,
            this.colAutoFocusWafer,
            this.colValid});
            this.gridSaved.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSaved.Location = new System.Drawing.Point(3, 21);
            this.gridSaved.MultiSelect = false;
            this.gridSaved.Name = "gridSaved";
            this.gridSaved.ReadOnly = true;
            this.gridSaved.RowHeadersVisible = false;
            this.gridSaved.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.gridSaved.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridSaved.Size = new System.Drawing.Size(454, 178);
            this.gridSaved.TabIndex = 0;
            this.gridSaved.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridSaved_CellDoubleClick);
            // 
            // colItem
            // 
            this.colItem.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colItem.FillWeight = 36F;
            this.colItem.HeaderText = "ITEM";
            this.colItem.Name = "colItem";
            this.colItem.ReadOnly = true;
            this.colItem.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colDefaultPos
            // 
            this.colDefaultPos.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colDefaultPos.FillWeight = 22F;
            this.colDefaultPos.HeaderText = "DEFAULT";
            this.colDefaultPos.Name = "colDefaultPos";
            this.colDefaultPos.ReadOnly = true;
            this.colDefaultPos.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colBestPos
            // 
            this.colBestPos.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colBestPos.FillWeight = 22F;
            this.colBestPos.HeaderText = "BEST";
            this.colBestPos.Name = "colBestPos";
            this.colBestPos.ReadOnly = true;
            this.colBestPos.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colBestScore
            // 
            this.colBestScore.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colBestScore.FillWeight = 16F;
            this.colBestScore.HeaderText = "SCORE";
            this.colBestScore.Name = "colBestScore";
            this.colBestScore.ReadOnly = true;
            this.colBestScore.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colPickerZ
            // 
            this.colPickerZ.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colPickerZ.FillWeight = 16F;
            this.colPickerZ.HeaderText = "PICKER Z";
            this.colPickerZ.Name = "colPickerZ";
            this.colPickerZ.ReadOnly = true;
            this.colPickerZ.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colAutoFocusCount
            // 
            this.colAutoFocusCount.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colAutoFocusCount.FillWeight = 14F;
            this.colAutoFocusCount.HeaderText = "AF CNT";
            this.colAutoFocusCount.Name = "colAutoFocusCount";
            this.colAutoFocusCount.ReadOnly = true;
            this.colAutoFocusCount.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colAutoFocusWafer
            // 
            this.colAutoFocusWafer.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colAutoFocusWafer.FillWeight = 20F;
            this.colAutoFocusWafer.HeaderText = "AF WAFER";
            this.colAutoFocusWafer.Name = "colAutoFocusWafer";
            this.colAutoFocusWafer.ReadOnly = true;
            this.colAutoFocusWafer.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colValid
            // 
            this.colValid.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colValid.FillWeight = 10F;
            this.colValid.HeaderText = "OK";
            this.colValid.Name = "colValid";
            this.colValid.ReadOnly = true;
            this.colValid.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // lblStatus
            // 
            this.lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblStatus.Location = new System.Drawing.Point(10, 641);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(10, 0, 10, 9);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblStatus.Size = new System.Drawing.Size(998, 41);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "대기 중입니다.";
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
            this.buttonPanel.Controls.Add(this.btnUseCurrent, 1, 0);
            this.buttonPanel.Controls.Add(this.btnMoveDefault, 2, 0);
            this.buttonPanel.Controls.Add(this.btnMoveZAvoid, 3, 0);
            this.buttonPanel.Controls.Add(this.btnMoveYAvoid, 4, 0);
            this.buttonPanel.Controls.Add(this.btnStartScan, 5, 0);
            this.buttonPanel.Controls.Add(this.btnSeqStop, 6, 0);
            this.buttonPanel.Controls.Add(this.btnApplyBest, 7, 0);
            this.buttonPanel.Controls.Add(this.btnResetAutoFocus, 8, 0);
            this.buttonPanel.Controls.Add(this.btnReload, 9, 0);
            this.buttonPanel.Controls.Add(this.btnSave, 10, 0);
            this.buttonPanel.Controls.Add(this.btnClose, 11, 0);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.Location = new System.Drawing.Point(10, 704);
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(10, 13, 10, 13);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.RowCount = 1;
            this.buttonPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonPanel.Size = new System.Drawing.Size(998, 41);
            this.buttonPanel.TabIndex = 3;
            // 
            // btnCheck
            // 
            this.btnCheck.BackColor = System.Drawing.Color.White;
            this.btnCheck.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCheck.FlatAppearance.BorderSize = 0;
            this.btnCheck.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCheck.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnCheck.ForeColor = System.Drawing.Color.Black;
            this.btnCheck.Location = new System.Drawing.Point(5, 6);
            this.btnCheck.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btnCheck.Name = "btnCheck";
            this.btnCheck.Size = new System.Drawing.Size(89, 29);
            this.btnCheck.TabIndex = 0;
            this.btnCheck.Text = "CHECK READY";
            this.btnCheck.UseVisualStyleBackColor = false;
            this.btnCheck.Role = CalibrationDialogButtonRole.Normal;
            this.btnCheck.Click += new System.EventHandler(this.btnCheck_Click);
            // 
            // btnUseCurrent
            // 
            this.btnUseCurrent.BackColor = System.Drawing.Color.White;
            this.btnUseCurrent.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUseCurrent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUseCurrent.FlatAppearance.BorderSize = 0;
            this.btnUseCurrent.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUseCurrent.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnUseCurrent.ForeColor = System.Drawing.Color.Black;
            this.btnUseCurrent.Location = new System.Drawing.Point(105, 6);
            this.btnUseCurrent.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btnUseCurrent.Name = "btnUseCurrent";
            this.btnUseCurrent.Size = new System.Drawing.Size(89, 29);
            this.btnUseCurrent.TabIndex = 1;
            this.btnUseCurrent.Text = "USE CURRENT";
            this.btnUseCurrent.UseVisualStyleBackColor = false;
            this.btnUseCurrent.Role = CalibrationDialogButtonRole.Normal;
            this.btnUseCurrent.Click += new System.EventHandler(this.btnUseCurrent_Click);
            // 
            // btnMoveDefault
            // 
            this.btnMoveDefault.BackColor = System.Drawing.Color.White;
            this.btnMoveDefault.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMoveDefault.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMoveDefault.FlatAppearance.BorderSize = 0;
            this.btnMoveDefault.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMoveDefault.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnMoveDefault.ForeColor = System.Drawing.Color.Black;
            this.btnMoveDefault.Location = new System.Drawing.Point(205, 6);
            this.btnMoveDefault.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btnMoveDefault.Name = "btnMoveDefault";
            this.btnMoveDefault.Size = new System.Drawing.Size(89, 29);
            this.btnMoveDefault.TabIndex = 2;
            this.btnMoveDefault.Text = "MOVE";
            this.btnMoveDefault.UseVisualStyleBackColor = false;
            this.btnMoveDefault.Role = CalibrationDialogButtonRole.Normal;
            this.btnMoveDefault.Click += new System.EventHandler(this.btnMoveDefault_Click);
            // 
            // btnMoveZAvoid
            // 
            this.btnMoveZAvoid.BackColor = System.Drawing.Color.White;
            this.btnMoveZAvoid.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMoveZAvoid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMoveZAvoid.FlatAppearance.BorderSize = 0;
            this.btnMoveZAvoid.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMoveZAvoid.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnMoveZAvoid.ForeColor = System.Drawing.Color.Black;
            this.btnMoveZAvoid.Location = new System.Drawing.Point(305, 6);
            this.btnMoveZAvoid.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btnMoveZAvoid.Name = "btnMoveZAvoid";
            this.btnMoveZAvoid.Size = new System.Drawing.Size(89, 29);
            this.btnMoveZAvoid.TabIndex = 3;
            this.btnMoveZAvoid.Text = "Z AVOID";
            this.btnMoveZAvoid.UseVisualStyleBackColor = false;
            this.btnMoveZAvoid.Role = CalibrationDialogButtonRole.Normal;
            this.btnMoveZAvoid.Click += new System.EventHandler(this.btnMoveZAvoid_Click);
            // 
            // btnMoveYAvoid
            // 
            this.btnMoveYAvoid.BackColor = System.Drawing.Color.White;
            this.btnMoveYAvoid.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMoveYAvoid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMoveYAvoid.FlatAppearance.BorderSize = 0;
            this.btnMoveYAvoid.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMoveYAvoid.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnMoveYAvoid.ForeColor = System.Drawing.Color.Black;
            this.btnMoveYAvoid.Location = new System.Drawing.Point(405, 6);
            this.btnMoveYAvoid.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btnMoveYAvoid.Name = "btnMoveYAvoid";
            this.btnMoveYAvoid.Size = new System.Drawing.Size(89, 29);
            this.btnMoveYAvoid.TabIndex = 4;
            this.btnMoveYAvoid.Text = "P-Y AVOID";
            this.btnMoveYAvoid.UseVisualStyleBackColor = false;
            this.btnMoveYAvoid.Role = CalibrationDialogButtonRole.Normal;
            this.btnMoveYAvoid.Click += new System.EventHandler(this.btnMoveYAvoid_Click);
            // 
            // btnStartScan
            // 
            this.btnStartScan.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(120)))), ((int)(((byte)(0)))));
            this.btnStartScan.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStartScan.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStartScan.FlatAppearance.BorderSize = 0;
            this.btnStartScan.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartScan.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnStartScan.ForeColor = System.Drawing.Color.White;
            this.btnStartScan.Location = new System.Drawing.Point(505, 6);
            this.btnStartScan.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btnStartScan.Name = "btnStartScan";
            this.btnStartScan.Size = new System.Drawing.Size(89, 29);
            this.btnStartScan.TabIndex = 5;
            this.btnStartScan.Text = "START SCAN";
            this.btnStartScan.UseVisualStyleBackColor = false;
            this.btnStartScan.Role = CalibrationDialogButtonRole.Primary;
            this.btnStartScan.Click += new System.EventHandler(this.btnStartScan_Click);
            // 
            // btnSeqStop
            // 
            this.btnSeqStop.BackColor = System.Drawing.Color.White;
            this.btnSeqStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSeqStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSeqStop.Enabled = false;
            this.btnSeqStop.FlatAppearance.BorderSize = 0;
            this.btnSeqStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSeqStop.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnSeqStop.ForeColor = System.Drawing.Color.Black;
            this.btnSeqStop.Location = new System.Drawing.Point(545, 6);
            this.btnSeqStop.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btnSeqStop.Name = "btnSeqStop";
            this.btnSeqStop.Size = new System.Drawing.Size(80, 29);
            this.btnSeqStop.TabIndex = 6;
            this.btnSeqStop.Text = "SEQ STOP";
            this.btnSeqStop.UseVisualStyleBackColor = false;
            this.btnSeqStop.Role = CalibrationDialogButtonRole.Normal;
            this.btnSeqStop.Click += new System.EventHandler(this.btnSeqStop_Click);
            // 
            // btnApplyBest
            // 
            this.btnApplyBest.BackColor = System.Drawing.Color.White;
            this.btnApplyBest.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnApplyBest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApplyBest.FlatAppearance.BorderSize = 0;
            this.btnApplyBest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApplyBest.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnApplyBest.ForeColor = System.Drawing.Color.Black;
            this.btnApplyBest.Location = new System.Drawing.Point(605, 6);
            this.btnApplyBest.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btnApplyBest.Name = "btnApplyBest";
            this.btnApplyBest.Size = new System.Drawing.Size(89, 29);
            this.btnApplyBest.TabIndex = 7;
            this.btnApplyBest.Text = "APPLY BEST";
            this.btnApplyBest.UseVisualStyleBackColor = false;
            this.btnApplyBest.Role = CalibrationDialogButtonRole.Normal;
            this.btnApplyBest.Click += new System.EventHandler(this.btnApplyBest_Click);
            // 
            // btnResetAutoFocus
            // 
            this.btnResetAutoFocus.BackColor = System.Drawing.Color.White;
            this.btnResetAutoFocus.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnResetAutoFocus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnResetAutoFocus.FlatAppearance.BorderSize = 0;
            this.btnResetAutoFocus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResetAutoFocus.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnResetAutoFocus.ForeColor = System.Drawing.Color.Black;
            this.btnResetAutoFocus.Location = new System.Drawing.Point(705, 6);
            this.btnResetAutoFocus.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btnResetAutoFocus.Name = "btnResetAutoFocus";
            this.btnResetAutoFocus.Size = new System.Drawing.Size(80, 29);
            this.btnResetAutoFocus.TabIndex = 8;
            this.btnResetAutoFocus.Text = "RESET AF";
            this.btnResetAutoFocus.UseVisualStyleBackColor = false;
            this.btnResetAutoFocus.Role = CalibrationDialogButtonRole.Normal;
            this.btnResetAutoFocus.Click += new System.EventHandler(this.btnResetAutoFocus_Click);
            // 
            // btnReload
            // 
            this.btnReload.BackColor = System.Drawing.Color.White;
            this.btnReload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReload.FlatAppearance.BorderSize = 0;
            this.btnReload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReload.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnReload.ForeColor = System.Drawing.Color.Black;
            this.btnReload.Location = new System.Drawing.Point(705, 6);
            this.btnReload.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(89, 29);
            this.btnReload.TabIndex = 9;
            this.btnReload.Text = "RELOAD";
            this.btnReload.UseVisualStyleBackColor = false;
            this.btnReload.Role = CalibrationDialogButtonRole.Normal;
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(805, 6);
            this.btnSave.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(89, 29);
            this.btnSave.TabIndex = 10;
            this.btnSave.Text = "SAVE";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Role = CalibrationDialogButtonRole.Dark;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.Color.White;
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnClose.ForeColor = System.Drawing.Color.Black;
            this.btnClose.Location = new System.Drawing.Point(905, 6);
            this.btnClose.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(88, 29);
            this.btnClose.TabIndex = 11;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Role = CalibrationDialogButtonRole.Normal;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // VisionFocusCalibrationDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1018, 758);
            this.Controls.Add(this.rootLayout);
            this.Name = "VisionFocusCalibrationDialog";
            this.Text = "Vision Focus Calibration";
            this.rootLayout.ResumeLayout(false);
            this.mainLayout.ResumeLayout(false);
            this.groupSetting.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridSettings)).EndInit();
            this.groupResult.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridSamples)).EndInit();
            this.groupSaved.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridSaved)).EndInit();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}


