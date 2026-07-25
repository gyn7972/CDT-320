namespace QMC.CDT_320.Ui.Dialogs
{
    partial class ColletCleaningControlDialog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;

        private System.Windows.Forms.GroupBox targetGroup;
        private System.Windows.Forms.FlowLayoutPanel targetFlow;
        private System.Windows.Forms.CheckBox chkTargetAll;
        private System.Windows.Forms.CheckBox chkFront4;
        private System.Windows.Forms.CheckBox chkFront3;
        private System.Windows.Forms.CheckBox chkFront2;
        private System.Windows.Forms.CheckBox chkFront1;
        private System.Windows.Forms.CheckBox chkRear4;
        private System.Windows.Forms.CheckBox chkRear3;
        private System.Windows.Forms.CheckBox chkRear2;
        private System.Windows.Forms.CheckBox chkRear1;

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.GroupBox groupSettings;
        private System.Windows.Forms.DataGridView gridSettings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingUnit;

        private System.Windows.Forms.GroupBox groupResults;
        private System.Windows.Forms.TableLayoutPanel resultsLayout;
        private System.Windows.Forms.DataGridView gridHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHistorySide;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHistoryCollet;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHistoryLastAt;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHistoryCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHistoryRetry;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHistoryResult;
        private System.Windows.Forms.Label lblRunLog;
        private System.Windows.Forms.ListBox lstRunLog;

        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.FlowLayoutPanel footerFlow;
        private CalibrationDialogButton btnStart;
        private CalibrationDialogButton btnStop;
        private CalibrationDialogButton btnSelectAll;
        private CalibrationDialogButton btnSelectNone;
        private CalibrationDialogButton btnReload;
        private CalibrationDialogButton btnSave;
        private CalibrationDialogButton btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.targetGroup = new System.Windows.Forms.GroupBox();
            this.targetFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.chkTargetAll = new System.Windows.Forms.CheckBox();
            this.chkFront4 = new System.Windows.Forms.CheckBox();
            this.chkFront3 = new System.Windows.Forms.CheckBox();
            this.chkFront2 = new System.Windows.Forms.CheckBox();
            this.chkFront1 = new System.Windows.Forms.CheckBox();
            this.chkRear4 = new System.Windows.Forms.CheckBox();
            this.chkRear3 = new System.Windows.Forms.CheckBox();
            this.chkRear2 = new System.Windows.Forms.CheckBox();
            this.chkRear1 = new System.Windows.Forms.CheckBox();
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.groupSettings = new System.Windows.Forms.GroupBox();
            this.gridSettings = new System.Windows.Forms.DataGridView();
            this.colSettingName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSettingValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSettingUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupResults = new System.Windows.Forms.GroupBox();
            this.resultsLayout = new System.Windows.Forms.TableLayoutPanel();
            this.gridHistory = new System.Windows.Forms.DataGridView();
            this.colHistorySide = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHistoryCollet = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHistoryLastAt = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHistoryCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHistoryRetry = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHistoryResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblRunLog = new System.Windows.Forms.Label();
            this.lstRunLog = new System.Windows.Forms.ListBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.footerFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.btnStart = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnStop = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnSelectAll = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnSelectNone = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnReload = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnSave = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnClose = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.rootLayout.SuspendLayout();
            this.targetGroup.SuspendLayout();
            this.targetFlow.SuspendLayout();
            this.mainLayout.SuspendLayout();
            this.groupSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSettings)).BeginInit();
            this.groupResults.SuspendLayout();
            this.resultsLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridHistory)).BeginInit();
            this.footerFlow.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.targetGroup, 0, 1);
            this.rootLayout.Controls.Add(this.mainLayout, 0, 2);
            this.rootLayout.Controls.Add(this.lblStatus, 0, 3);
            this.rootLayout.Controls.Add(this.footerFlow, 0, 4);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 5;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
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
            this.lblHeader.Text = "COLLET CLEANING";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // targetGroup
            //
            this.targetGroup.Controls.Add(this.targetFlow);
            this.targetGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.targetGroup.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.targetGroup.Location = new System.Drawing.Point(12, 55);
            this.targetGroup.Margin = new System.Windows.Forms.Padding(12, 3, 12, 0);
            this.targetGroup.Name = "targetGroup";
            this.targetGroup.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.targetGroup.Size = new System.Drawing.Size(1299, 56);
            this.targetGroup.TabIndex = 1;
            this.targetGroup.TabStop = false;
            this.targetGroup.Text = "TARGET (선택 콜렛 일괄 클리닝 · 각 side C4 → C3~1 순, 전부 클린 후 전부 검사)";
            //
            // targetFlow
            //
            this.targetFlow.Controls.Add(this.chkTargetAll);
            this.targetFlow.Controls.Add(this.chkFront4);
            this.targetFlow.Controls.Add(this.chkFront3);
            this.targetFlow.Controls.Add(this.chkFront2);
            this.targetFlow.Controls.Add(this.chkFront1);
            this.targetFlow.Controls.Add(this.chkRear4);
            this.targetFlow.Controls.Add(this.chkRear3);
            this.targetFlow.Controls.Add(this.chkRear2);
            this.targetFlow.Controls.Add(this.chkRear1);
            this.targetFlow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.targetFlow.Location = new System.Drawing.Point(4, 21);
            this.targetFlow.Margin = new System.Windows.Forms.Padding(0);
            this.targetFlow.Name = "targetFlow";
            this.targetFlow.Size = new System.Drawing.Size(1291, 32);
            this.targetFlow.TabIndex = 0;
            this.targetFlow.WrapContents = false;
            //
            // chkTargetAll
            //
            this.chkTargetAll.AutoSize = true;
            this.chkTargetAll.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.chkTargetAll.Location = new System.Drawing.Point(7, 5);
            this.chkTargetAll.Margin = new System.Windows.Forms.Padding(7, 5, 14, 3);
            this.chkTargetAll.Name = "chkTargetAll";
            this.chkTargetAll.Size = new System.Drawing.Size(51, 23);
            this.chkTargetAll.TabIndex = 0;
            this.chkTargetAll.Text = "ALL";
            this.chkTargetAll.UseVisualStyleBackColor = true;
            //
            // chkFront4
            //
            this.chkFront4.AutoSize = true;
            this.chkFront4.Location = new System.Drawing.Point(79, 5);
            this.chkFront4.Margin = new System.Windows.Forms.Padding(7, 5, 7, 3);
            this.chkFront4.Name = "chkFront4";
            this.chkFront4.Size = new System.Drawing.Size(57, 23);
            this.chkFront4.TabIndex = 1;
            this.chkFront4.Text = "F C4";
            this.chkFront4.UseVisualStyleBackColor = true;
            //
            // chkFront3
            //
            this.chkFront3.AutoSize = true;
            this.chkFront3.Location = new System.Drawing.Point(150, 5);
            this.chkFront3.Margin = new System.Windows.Forms.Padding(7, 5, 7, 3);
            this.chkFront3.Name = "chkFront3";
            this.chkFront3.Size = new System.Drawing.Size(57, 23);
            this.chkFront3.TabIndex = 2;
            this.chkFront3.Text = "F C3";
            this.chkFront3.UseVisualStyleBackColor = true;
            //
            // chkFront2
            //
            this.chkFront2.AutoSize = true;
            this.chkFront2.Location = new System.Drawing.Point(221, 5);
            this.chkFront2.Margin = new System.Windows.Forms.Padding(7, 5, 7, 3);
            this.chkFront2.Name = "chkFront2";
            this.chkFront2.Size = new System.Drawing.Size(57, 23);
            this.chkFront2.TabIndex = 3;
            this.chkFront2.Text = "F C2";
            this.chkFront2.UseVisualStyleBackColor = true;
            //
            // chkFront1
            //
            this.chkFront1.AutoSize = true;
            this.chkFront1.Location = new System.Drawing.Point(292, 5);
            this.chkFront1.Margin = new System.Windows.Forms.Padding(7, 5, 7, 3);
            this.chkFront1.Name = "chkFront1";
            this.chkFront1.Size = new System.Drawing.Size(57, 23);
            this.chkFront1.TabIndex = 4;
            this.chkFront1.Text = "F C1";
            this.chkFront1.UseVisualStyleBackColor = true;
            //
            // chkRear4
            //
            this.chkRear4.AutoSize = true;
            this.chkRear4.Location = new System.Drawing.Point(363, 5);
            this.chkRear4.Margin = new System.Windows.Forms.Padding(7, 5, 7, 3);
            this.chkRear4.Name = "chkRear4";
            this.chkRear4.Size = new System.Drawing.Size(58, 23);
            this.chkRear4.TabIndex = 5;
            this.chkRear4.Text = "R C4";
            this.chkRear4.UseVisualStyleBackColor = true;
            //
            // chkRear3
            //
            this.chkRear3.AutoSize = true;
            this.chkRear3.Location = new System.Drawing.Point(435, 5);
            this.chkRear3.Margin = new System.Windows.Forms.Padding(7, 5, 7, 3);
            this.chkRear3.Name = "chkRear3";
            this.chkRear3.Size = new System.Drawing.Size(58, 23);
            this.chkRear3.TabIndex = 6;
            this.chkRear3.Text = "R C3";
            this.chkRear3.UseVisualStyleBackColor = true;
            //
            // chkRear2
            //
            this.chkRear2.AutoSize = true;
            this.chkRear2.Location = new System.Drawing.Point(507, 5);
            this.chkRear2.Margin = new System.Windows.Forms.Padding(7, 5, 7, 3);
            this.chkRear2.Name = "chkRear2";
            this.chkRear2.Size = new System.Drawing.Size(58, 23);
            this.chkRear2.TabIndex = 7;
            this.chkRear2.Text = "R C2";
            this.chkRear2.UseVisualStyleBackColor = true;
            //
            // chkRear1
            //
            this.chkRear1.AutoSize = true;
            this.chkRear1.Location = new System.Drawing.Point(579, 5);
            this.chkRear1.Margin = new System.Windows.Forms.Padding(7, 5, 7, 3);
            this.chkRear1.Name = "chkRear1";
            this.chkRear1.Size = new System.Drawing.Size(58, 23);
            this.chkRear1.TabIndex = 8;
            this.chkRear1.Text = "R C1";
            this.chkRear1.UseVisualStyleBackColor = true;
            //
            // mainLayout
            //
            this.mainLayout.ColumnCount = 2;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 394F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.groupSettings, 0, 0);
            this.mainLayout.Controls.Add(this.groupResults, 1, 0);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(12, 114);
            this.mainLayout.Margin = new System.Windows.Forms.Padding(12, 3, 12, 3);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.RowCount = 1;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Size = new System.Drawing.Size(1299, 551);
            this.mainLayout.TabIndex = 2;
            //
            // groupSettings
            //
            this.groupSettings.Controls.Add(this.gridSettings);
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
            this.gridSettings.Location = new System.Drawing.Point(4, 21);
            this.gridSettings.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.gridSettings.MultiSelect = false;
            this.gridSettings.Name = "gridSettings";
            this.gridSettings.RowHeadersVisible = false;
            this.gridSettings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridSettings.ShowCellToolTips = true;
            this.gridSettings.Size = new System.Drawing.Size(376, 527);
            this.gridSettings.TabIndex = 0;
            //
            // colSettingName
            //
            this.colSettingName.HeaderText = "PARAMETER";
            this.colSettingName.Name = "colSettingName";
            this.colSettingName.ReadOnly = true;
            this.colSettingName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colSettingName.Width = 190;
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
            this.groupResults.Text = "COLLET CLEANING HISTORY";
            //
            // resultsLayout
            //
            this.resultsLayout.ColumnCount = 1;
            this.resultsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.resultsLayout.Controls.Add(this.gridHistory, 0, 0);
            this.resultsLayout.Controls.Add(this.lblRunLog, 0, 1);
            this.resultsLayout.Controls.Add(this.lstRunLog, 0, 2);
            this.resultsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resultsLayout.Location = new System.Drawing.Point(4, 21);
            this.resultsLayout.Margin = new System.Windows.Forms.Padding(0);
            this.resultsLayout.Name = "resultsLayout";
            this.resultsLayout.RowCount = 3;
            this.resultsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 210F));
            this.resultsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.resultsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.resultsLayout.Size = new System.Drawing.Size(897, 527);
            this.resultsLayout.TabIndex = 0;
            //
            // gridHistory
            //
            this.gridHistory.AllowUserToAddRows = false;
            this.gridHistory.AllowUserToDeleteRows = false;
            this.gridHistory.AllowUserToResizeRows = false;
            this.gridHistory.BackgroundColor = System.Drawing.Color.White;
            this.gridHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colHistorySide,
            this.colHistoryCollet,
            this.colHistoryLastAt,
            this.colHistoryCount,
            this.colHistoryRetry,
            this.colHistoryResult});
            this.gridHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridHistory.Location = new System.Drawing.Point(3, 3);
            this.gridHistory.MultiSelect = false;
            this.gridHistory.Name = "gridHistory";
            this.gridHistory.ReadOnly = true;
            this.gridHistory.RowHeadersVisible = false;
            this.gridHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridHistory.Size = new System.Drawing.Size(891, 204);
            this.gridHistory.TabIndex = 0;
            //
            // colHistorySide
            //
            this.colHistorySide.HeaderText = "SIDE";
            this.colHistorySide.Name = "colHistorySide";
            this.colHistorySide.ReadOnly = true;
            this.colHistorySide.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colHistorySide.Width = 90;
            //
            // colHistoryCollet
            //
            this.colHistoryCollet.HeaderText = "COLLET";
            this.colHistoryCollet.Name = "colHistoryCollet";
            this.colHistoryCollet.ReadOnly = true;
            this.colHistoryCollet.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colHistoryCollet.Width = 80;
            //
            // colHistoryLastAt
            //
            this.colHistoryLastAt.HeaderText = "LAST CLEANED";
            this.colHistoryLastAt.Name = "colHistoryLastAt";
            this.colHistoryLastAt.ReadOnly = true;
            this.colHistoryLastAt.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colHistoryLastAt.Width = 190;
            //
            // colHistoryCount
            //
            this.colHistoryCount.HeaderText = "TOTAL";
            this.colHistoryCount.Name = "colHistoryCount";
            this.colHistoryCount.ReadOnly = true;
            this.colHistoryCount.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colHistoryCount.Width = 80;
            //
            // colHistoryRetry
            //
            this.colHistoryRetry.HeaderText = "RETRY";
            this.colHistoryRetry.Name = "colHistoryRetry";
            this.colHistoryRetry.ReadOnly = true;
            this.colHistoryRetry.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colHistoryRetry.Width = 80;
            //
            // colHistoryResult
            //
            this.colHistoryResult.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colHistoryResult.HeaderText = "RESULT";
            this.colHistoryResult.Name = "colHistoryResult";
            this.colHistoryResult.ReadOnly = true;
            this.colHistoryResult.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // lblRunLog
            //
            this.lblRunLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRunLog.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblRunLog.Location = new System.Drawing.Point(3, 210);
            this.lblRunLog.Name = "lblRunLog";
            this.lblRunLog.Size = new System.Drawing.Size(891, 26);
            this.lblRunLog.TabIndex = 1;
            this.lblRunLog.Text = "RUN LOG";
            this.lblRunLog.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lstRunLog
            //
            this.lstRunLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstRunLog.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lstRunLog.FormattingEnabled = true;
            this.lstRunLog.HorizontalScrollbar = true;
            this.lstRunLog.ItemHeight = 19;
            this.lstRunLog.Location = new System.Drawing.Point(3, 239);
            this.lstRunLog.Name = "lstRunLog";
            this.lstRunLog.Size = new System.Drawing.Size(891, 285);
            this.lstRunLog.TabIndex = 2;
            //
            // lblStatus
            //
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblStatus.Location = new System.Drawing.Point(15, 668);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(15, 0, 12, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(1296, 48);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "대기 중입니다. 대상 콜렛과 클리닝 조건을 확인한 뒤 START를 실행하세요.";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // footerFlow
            //
            this.footerFlow.Controls.Add(this.btnStart);
            this.footerFlow.Controls.Add(this.btnStop);
            this.footerFlow.Controls.Add(this.btnSelectAll);
            this.footerFlow.Controls.Add(this.btnSelectNone);
            this.footerFlow.Controls.Add(this.btnReload);
            this.footerFlow.Controls.Add(this.btnSave);
            this.footerFlow.Controls.Add(this.btnClose);
            this.footerFlow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.footerFlow.Location = new System.Drawing.Point(12, 719);
            this.footerFlow.Margin = new System.Windows.Forms.Padding(12, 3, 12, 3);
            this.footerFlow.Name = "footerFlow";
            this.footerFlow.Size = new System.Drawing.Size(1299, 74);
            this.footerFlow.TabIndex = 4;
            this.footerFlow.WrapContents = false;
            //
            // btnStart
            //
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(126)))), ((int)(((byte)(0)))));
            this.btnStart.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStart.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.Location = new System.Drawing.Point(7, 4);
            this.btnStart.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnStart.Name = "btnStart";
            this.btnStart.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Primary;
            this.btnStart.Size = new System.Drawing.Size(150, 66);
            this.btnStart.TabIndex = 0;
            this.btnStart.Text = "START";
            this.btnStart.UseVisualStyleBackColor = false;
            //
            // btnStop
            //
            this.btnStop.BackColor = System.Drawing.Color.White;
            this.btnStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStop.Enabled = false;
            this.btnStop.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnStop.Location = new System.Drawing.Point(171, 4);
            this.btnStop.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnStop.Name = "btnStop";
            this.btnStop.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnStop.Size = new System.Drawing.Size(150, 66);
            this.btnStop.TabIndex = 1;
            this.btnStop.Text = "SEQ STOP";
            this.btnStop.UseVisualStyleBackColor = false;
            //
            // btnSelectAll
            //
            this.btnSelectAll.BackColor = System.Drawing.Color.White;
            this.btnSelectAll.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSelectAll.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnSelectAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelectAll.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSelectAll.Location = new System.Drawing.Point(335, 4);
            this.btnSelectAll.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnSelectAll.Size = new System.Drawing.Size(150, 66);
            this.btnSelectAll.TabIndex = 2;
            this.btnSelectAll.Text = "SELECT ALL";
            this.btnSelectAll.UseVisualStyleBackColor = false;
            //
            // btnSelectNone
            //
            this.btnSelectNone.BackColor = System.Drawing.Color.White;
            this.btnSelectNone.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSelectNone.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnSelectNone.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelectNone.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSelectNone.Location = new System.Drawing.Point(499, 4);
            this.btnSelectNone.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnSelectNone.Name = "btnSelectNone";
            this.btnSelectNone.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnSelectNone.Size = new System.Drawing.Size(150, 66);
            this.btnSelectNone.TabIndex = 3;
            this.btnSelectNone.Text = "CLEAR ALL";
            this.btnSelectNone.UseVisualStyleBackColor = false;
            //
            // btnReload
            //
            this.btnReload.BackColor = System.Drawing.Color.White;
            this.btnReload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReload.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnReload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReload.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnReload.Location = new System.Drawing.Point(663, 4);
            this.btnReload.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnReload.Name = "btnReload";
            this.btnReload.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnReload.Size = new System.Drawing.Size(150, 66);
            this.btnReload.TabIndex = 4;
            this.btnReload.Text = "RELOAD";
            this.btnReload.UseVisualStyleBackColor = false;
            //
            // btnSave
            //
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(827, 4);
            this.btnSave.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnSave.Name = "btnSave";
            this.btnSave.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Dark;
            this.btnSave.Size = new System.Drawing.Size(150, 66);
            this.btnSave.TabIndex = 5;
            this.btnSave.Text = "SAVE";
            this.btnSave.UseVisualStyleBackColor = false;
            //
            // btnClose
            //
            this.btnClose.BackColor = System.Drawing.Color.White;
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnClose.Location = new System.Drawing.Point(991, 4);
            this.btnClose.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnClose.Name = "btnClose";
            this.btnClose.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnClose.Size = new System.Drawing.Size(150, 66);
            this.btnClose.TabIndex = 6;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = false;
            //
            // ColletCleaningControlDialog
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1323, 814);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.MinimumSize = new System.Drawing.Size(1100, 700);
            this.Name = "ColletCleaningControlDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "COLLET CLEANING";
            this.rootLayout.ResumeLayout(false);
            this.targetGroup.ResumeLayout(false);
            this.targetFlow.ResumeLayout(false);
            this.targetFlow.PerformLayout();
            this.mainLayout.ResumeLayout(false);
            this.groupSettings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridSettings)).EndInit();
            this.groupResults.ResumeLayout(false);
            this.resultsLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridHistory)).EndInit();
            this.footerFlow.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
