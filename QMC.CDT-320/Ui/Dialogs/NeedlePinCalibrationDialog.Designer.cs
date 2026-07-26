namespace QMC.CDT_320.Ui.Dialogs
{
    partial class NeedlePinCalibrationDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.TableLayoutPanel leftLayout;
        private System.Windows.Forms.GroupBox groupSetting;
        private System.Windows.Forms.TableLayoutPanel settingLayout;
        private System.Windows.Forms.GroupBox groupSaved;
        private System.Windows.Forms.GroupBox groupTeaching;
        private System.Windows.Forms.DataGridView _settingsGrid;
        private System.Windows.Forms.DataGridView _resultGrid;
        private System.Windows.Forms.DataGridView _teachingGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingUnit;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResultItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResultValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResultUnit;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTeachingItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTeachingTarget;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTeachingActual;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTeachingUnit;
        private System.Windows.Forms.Label _status;
        private System.Windows.Forms.TableLayoutPanel buttonPanel;
        private CalibrationDialogButton _btnCheck;
        private CalibrationDialogButton _btnMoveReady;
        private CalibrationDialogButton _btnTeach;
        private CalibrationDialogButton _btnMoveTeach;
        private CalibrationDialogButton _btnStart;
        private CalibrationDialogButton _btnReload;
        private CalibrationDialogButton _btnParameterSave;
        private CalibrationDialogButton _btnSave;
        private CalibrationDialogButton _btnClose;

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
            this.leftLayout = new System.Windows.Forms.TableLayoutPanel();
            this.groupSetting = new System.Windows.Forms.GroupBox();
            this.settingLayout = new System.Windows.Forms.TableLayoutPanel();
            this._settingsGrid = new System.Windows.Forms.DataGridView();
            this.colSettingItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSettingValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSettingUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupSaved = new System.Windows.Forms.GroupBox();
            this._resultGrid = new System.Windows.Forms.DataGridView();
            this.colResultItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResultValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResultUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupTeaching = new System.Windows.Forms.GroupBox();
            this._teachingGrid = new System.Windows.Forms.DataGridView();
            this.colTeachingItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTeachingTarget = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTeachingActual = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTeachingUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._status = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.TableLayoutPanel();
            this._btnCheck = new CalibrationDialogButton();
            this._btnMoveReady = new CalibrationDialogButton();
            this._btnTeach = new CalibrationDialogButton();
            this._btnMoveTeach = new CalibrationDialogButton();
            this._btnStart = new CalibrationDialogButton();
            this._btnReload = new CalibrationDialogButton();
            this._btnParameterSave = new CalibrationDialogButton();
            this._btnSave = new CalibrationDialogButton();
            this._btnClose = new CalibrationDialogButton();
            this.rootLayout.SuspendLayout();
            this.mainLayout.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.groupSetting.SuspendLayout();
            this.settingLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._settingsGrid)).BeginInit();
            this.groupSaved.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._resultGrid)).BeginInit();
            this.groupTeaching.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._teachingGrid)).BeginInit();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.mainLayout, 0, 1);
            this.rootLayout.Controls.Add(this._status, 0, 2);
            this.rootLayout.Controls.Add(this.buttonPanel, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(8, 8);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 54F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.rootLayout.Size = new System.Drawing.Size(1148, 665);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(126)))), ((int)(((byte)(0)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("留묒? 怨좊뵓", 14F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1148, 54);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "NEEDLE PIN CAL";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 2;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 540F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.leftLayout, 0, 0);
            this.mainLayout.Controls.Add(this.groupTeaching, 1, 0);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(0, 54);
            this.mainLayout.Margin = new System.Windows.Forms.Padding(0);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.RowCount = 1;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Size = new System.Drawing.Size(1148, 521);
            this.mainLayout.TabIndex = 1;
            // 
            // leftLayout
            // 
            this.leftLayout.ColumnCount = 1;
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Controls.Add(this.groupSetting, 0, 0);
            this.leftLayout.Controls.Add(this.groupSaved, 0, 1);
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Location = new System.Drawing.Point(0, 0);
            this.leftLayout.Margin = new System.Windows.Forms.Padding(0);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.RowCount = 2;
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 62F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 38F));
            this.leftLayout.Size = new System.Drawing.Size(540, 521);
            this.leftLayout.TabIndex = 0;
            // 
            // groupSetting
            // 
            this.groupSetting.Controls.Add(this.settingLayout);
            this.groupSetting.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupSetting.Font = new System.Drawing.Font("留묒? 怨좊뵓", 9F, System.Drawing.FontStyle.Bold);
            this.groupSetting.Location = new System.Drawing.Point(0, 0);
            this.groupSetting.Margin = new System.Windows.Forms.Padding(0, 0, 6, 6);
            this.groupSetting.Name = "groupSetting";
            this.groupSetting.Padding = new System.Windows.Forms.Padding(6);
            this.groupSetting.Size = new System.Drawing.Size(534, 317);
            this.groupSetting.TabIndex = 0;
            this.groupSetting.TabStop = false;
            this.groupSetting.Text = "CAL SETTING";
            //
            // settingLayout
            //
            this.settingLayout.ColumnCount = 1;
            this.settingLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.settingLayout.Controls.Add(this._settingsGrid, 0, 0);
            this.settingLayout.Controls.Add(this._btnParameterSave, 0, 1);
            this.settingLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingLayout.Location = new System.Drawing.Point(6, 22);
            this.settingLayout.Margin = new System.Windows.Forms.Padding(0);
            this.settingLayout.Name = "settingLayout";
            this.settingLayout.RowCount = 2;
            this.settingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.settingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.settingLayout.Size = new System.Drawing.Size(522, 289);
            this.settingLayout.TabIndex = 0;
            //
            // _settingsGrid
            // 
            this._settingsGrid.AllowUserToAddRows = false;
            this._settingsGrid.AllowUserToDeleteRows = false;
            this._settingsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._settingsGrid.BackgroundColor = System.Drawing.Color.White;
            this._settingsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._settingsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSettingItem,
            this.colSettingValue,
            this.colSettingUnit});
            this._settingsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._settingsGrid.Location = new System.Drawing.Point(0, 0);
            this._settingsGrid.MultiSelect = false;
            this._settingsGrid.Name = "_settingsGrid";
            this._settingsGrid.RowHeadersVisible = false;
            this._settingsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._settingsGrid.Size = new System.Drawing.Size(522, 243);
            this._settingsGrid.TabIndex = 0;
            this._settingsGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.SettingsGrid_CellDoubleClick);
            // 
            // colSettingItem
            // 
            this.colSettingItem.FillWeight = 48F;
            this.colSettingItem.HeaderText = "ITEM";
            this.colSettingItem.Name = "colSettingItem";
            this.colSettingItem.ReadOnly = true;
            // 
            // colSettingValue
            // 
            this.colSettingValue.FillWeight = 34F;
            this.colSettingValue.HeaderText = "VALUE";
            this.colSettingValue.Name = "colSettingValue";
            // 
            // colSettingUnit
            // 
            this.colSettingUnit.FillWeight = 18F;
            this.colSettingUnit.HeaderText = "UNIT";
            this.colSettingUnit.Name = "colSettingUnit";
            this.colSettingUnit.ReadOnly = true;
            // 
            // groupSaved
            // 
            this.groupSaved.Controls.Add(this._resultGrid);
            this.groupSaved.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupSaved.Font = new System.Drawing.Font("留묒? 怨좊뵓", 9F, System.Drawing.FontStyle.Bold);
            this.groupSaved.Location = new System.Drawing.Point(0, 323);
            this.groupSaved.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.groupSaved.Name = "groupSaved";
            this.groupSaved.Padding = new System.Windows.Forms.Padding(6);
            this.groupSaved.Size = new System.Drawing.Size(534, 198);
            this.groupSaved.TabIndex = 1;
            this.groupSaved.TabStop = false;
            this.groupSaved.Text = "SAVED RESULT";
            // 
            // _resultGrid
            // 
            this._resultGrid.AllowUserToAddRows = false;
            this._resultGrid.AllowUserToDeleteRows = false;
            this._resultGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._resultGrid.BackgroundColor = System.Drawing.Color.White;
            this._resultGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._resultGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colResultItem,
            this.colResultValue,
            this.colResultUnit});
            this._resultGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._resultGrid.Location = new System.Drawing.Point(6, 22);
            this._resultGrid.MultiSelect = false;
            this._resultGrid.Name = "_resultGrid";
            this._resultGrid.ReadOnly = true;
            this._resultGrid.RowHeadersVisible = false;
            this._resultGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._resultGrid.Size = new System.Drawing.Size(522, 170);
            this._resultGrid.TabIndex = 0;
            // 
            // colResultItem
            // 
            this.colResultItem.FillWeight = 48F;
            this.colResultItem.HeaderText = "ITEM";
            this.colResultItem.Name = "colResultItem";
            this.colResultItem.ReadOnly = true;
            // 
            // colResultValue
            // 
            this.colResultValue.FillWeight = 34F;
            this.colResultValue.HeaderText = "VALUE";
            this.colResultValue.Name = "colResultValue";
            this.colResultValue.ReadOnly = true;
            // 
            // colResultUnit
            // 
            this.colResultUnit.FillWeight = 18F;
            this.colResultUnit.HeaderText = "UNIT";
            this.colResultUnit.Name = "colResultUnit";
            this.colResultUnit.ReadOnly = true;
            // 
            // groupTeaching
            // 
            this.groupTeaching.Controls.Add(this._teachingGrid);
            this.groupTeaching.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupTeaching.Font = new System.Drawing.Font("留묒? 怨좊뵓", 9F, System.Drawing.FontStyle.Bold);
            this.groupTeaching.Location = new System.Drawing.Point(540, 0);
            this.groupTeaching.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.groupTeaching.Name = "groupTeaching";
            this.groupTeaching.Padding = new System.Windows.Forms.Padding(6);
            this.groupTeaching.Size = new System.Drawing.Size(608, 521);
            this.groupTeaching.TabIndex = 1;
            this.groupTeaching.TabStop = false;
            this.groupTeaching.Text = "TEACHING DATA";
            // 
            // _teachingGrid
            // 
            this._teachingGrid.AllowUserToAddRows = false;
            this._teachingGrid.AllowUserToDeleteRows = false;
            this._teachingGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._teachingGrid.BackgroundColor = System.Drawing.Color.White;
            this._teachingGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._teachingGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colTeachingItem,
            this.colTeachingTarget,
            this.colTeachingActual,
            this.colTeachingUnit});
            this._teachingGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._teachingGrid.Location = new System.Drawing.Point(6, 22);
            this._teachingGrid.MultiSelect = false;
            this._teachingGrid.Name = "_teachingGrid";
            this._teachingGrid.ReadOnly = true;
            this._teachingGrid.RowHeadersVisible = false;
            this._teachingGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._teachingGrid.Size = new System.Drawing.Size(596, 493);
            this._teachingGrid.TabIndex = 0;
            // 
            // colTeachingItem
            // 
            this.colTeachingItem.FillWeight = 42F;
            this.colTeachingItem.HeaderText = "ITEM";
            this.colTeachingItem.Name = "colTeachingItem";
            this.colTeachingItem.ReadOnly = true;
            // 
            // colTeachingTarget
            // 
            this.colTeachingTarget.FillWeight = 24F;
            this.colTeachingTarget.HeaderText = "TARGET";
            this.colTeachingTarget.Name = "colTeachingTarget";
            this.colTeachingTarget.ReadOnly = true;
            // 
            // colTeachingActual
            // 
            this.colTeachingActual.FillWeight = 24F;
            this.colTeachingActual.HeaderText = "ACTUAL";
            this.colTeachingActual.Name = "colTeachingActual";
            this.colTeachingActual.ReadOnly = true;
            // 
            // colTeachingUnit
            // 
            this.colTeachingUnit.FillWeight = 10F;
            this.colTeachingUnit.HeaderText = "UNIT";
            this.colTeachingUnit.Name = "colTeachingUnit";
            this.colTeachingUnit.ReadOnly = true;
            // 
            // _status
            // 
            this._status.BackColor = System.Drawing.Color.WhiteSmoke;
            this._status.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._status.Dock = System.Windows.Forms.DockStyle.Fill;
            this._status.Location = new System.Drawing.Point(0, 575);
            this._status.Margin = new System.Windows.Forms.Padding(0);
            this._status.Name = "_status";
            this._status.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this._status.Size = new System.Drawing.Size(1148, 38);
            this._status.TabIndex = 2;
            this._status.Text = "1) MOVE READY  2) USE CURRENT  3) MOVE TEACH  4) START CAL";
            this._status.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonPanel
            // 
            this.buttonPanel.ColumnCount = 8;
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.buttonPanel.Controls.Add(this._btnCheck, 0, 0);
            this.buttonPanel.Controls.Add(this._btnMoveReady, 1, 0);
            this.buttonPanel.Controls.Add(this._btnTeach, 2, 0);
            this.buttonPanel.Controls.Add(this._btnMoveTeach, 3, 0);
            this.buttonPanel.Controls.Add(this._btnStart, 4, 0);
            this.buttonPanel.Controls.Add(this._btnReload, 5, 0);
            this.buttonPanel.Controls.Add(this._btnSave, 6, 0);
            this.buttonPanel.Controls.Add(this._btnClose, 7, 0);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.Location = new System.Drawing.Point(0, 613);
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(0);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.buttonPanel.RowCount = 1;
            this.buttonPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonPanel.Size = new System.Drawing.Size(1148, 52);
            this.buttonPanel.TabIndex = 3;
            // 
            // 
            // _btnCheck
            // 
            this._btnCheck.BackColor = System.Drawing.Color.White;
            this._btnCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnCheck.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCheck.Font = new System.Drawing.Font("留묒? 怨좊뵓", 9.5F, System.Drawing.FontStyle.Bold);
            this._btnCheck.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnCheck.Name = "_btnCheck";
            this._btnCheck.Text = "CHECK READY";
            this._btnCheck.UseVisualStyleBackColor = false;
            // 
            // _btnMoveReady
            // 
            this._btnMoveReady.BackColor = System.Drawing.Color.White;
            this._btnMoveReady.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMoveReady.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMoveReady.Font = new System.Drawing.Font("留묒? 怨좊뵓", 9.5F, System.Drawing.FontStyle.Bold);
            this._btnMoveReady.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnMoveReady.Name = "_btnMoveReady";
            this._btnMoveReady.Text = "MOVE READY";
            this._btnMoveReady.UseVisualStyleBackColor = false;
            // 
            // _btnTeach
            // 
            this._btnTeach.BackColor = System.Drawing.Color.White;
            this._btnTeach.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnTeach.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnTeach.Font = new System.Drawing.Font("留묒? 怨좊뵓", 9.5F, System.Drawing.FontStyle.Bold);
            this._btnTeach.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnTeach.Name = "_btnTeach";
            this._btnTeach.Text = "USE CURRENT";
            this._btnTeach.UseVisualStyleBackColor = false;
            // 
            // _btnMoveTeach
            // 
            this._btnMoveTeach.BackColor = System.Drawing.Color.White;
            this._btnMoveTeach.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMoveTeach.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMoveTeach.Font = new System.Drawing.Font("留묒? 怨좊뵓", 9.5F, System.Drawing.FontStyle.Bold);
            this._btnMoveTeach.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnMoveTeach.Name = "_btnMoveTeach";
            this._btnMoveTeach.Text = "MOVE TEACH";
            this._btnMoveTeach.UseVisualStyleBackColor = false;
            // 
            // _btnStart
            // 
            this._btnStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnStart.Font = new System.Drawing.Font("留묒? 怨좊뵓", 9.5F, System.Drawing.FontStyle.Bold);
            this._btnStart.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnStart.Name = "_btnStart";
            this._btnStart.Text = "START CAL";
            this._btnStart.UseVisualStyleBackColor = false;
            // 
            // _btnReload
            // 
            this._btnReload.BackColor = System.Drawing.Color.White;
            this._btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnReload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnReload.Font = new System.Drawing.Font("留묒? 怨좊뵓", 9.5F, System.Drawing.FontStyle.Bold);
            this._btnReload.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnReload.Name = "_btnReload";
            this._btnReload.Text = "RELOAD";
            this._btnReload.UseVisualStyleBackColor = false;
            //
            // _btnParameterSave
            //
            this._btnParameterSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this._btnParameterSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnParameterSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnParameterSave.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this._btnParameterSave.ForeColor = System.Drawing.Color.White;
            this._btnParameterSave.Location = new System.Drawing.Point(4, 247);
            this._btnParameterSave.Margin = new System.Windows.Forms.Padding(4);
            this._btnParameterSave.Name = "_btnParameterSave";
            this._btnParameterSave.Role = CalibrationDialogButtonRole.Dark;
            this._btnParameterSave.Size = new System.Drawing.Size(514, 38);
            this._btnParameterSave.TabIndex = 1;
            this._btnParameterSave.Text = "PARAMETER SAVE";
            this._btnParameterSave.UseVisualStyleBackColor = false;
            this._btnParameterSave.Click += new System.EventHandler(this.BtnParameterSave_Click);
            //
            // _btnSave
            // 
            this._btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSave.Font = new System.Drawing.Font("留묒? 怨좊뵓", 9.5F, System.Drawing.FontStyle.Bold);
            this._btnSave.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnSave.Name = "_btnSave";
            this._btnSave.Text = "SAVE RESULT";
            this._btnSave.UseVisualStyleBackColor = false;
            // 
            // _btnClose
            // 
            this._btnClose.BackColor = System.Drawing.Color.White;
            this._btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnClose.Font = new System.Drawing.Font("留묒? 怨좊뵓", 9.5F, System.Drawing.FontStyle.Bold);
            this._btnClose.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnClose.Name = "_btnClose";
            this._btnClose.Text = "CLOSE";
            this._btnClose.UseVisualStyleBackColor = false;
            this._btnStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(126)))), ((int)(((byte)(0)))));
            this._btnStart.ForeColor = System.Drawing.Color.White;
            this._btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this._btnSave.ForeColor = System.Drawing.Color.White;
            this._btnCheck.Role = CalibrationDialogButtonRole.Normal;
            this._btnCheck.Click += new System.EventHandler(this.BtnCheck_Click);
            this._btnMoveReady.Role = CalibrationDialogButtonRole.Normal;
            this._btnMoveReady.Click += new System.EventHandler(this.BtnMoveReady_Click);
            this._btnTeach.Role = CalibrationDialogButtonRole.Normal;
            this._btnTeach.Click += new System.EventHandler(this.BtnTeach_Click);
            this._btnMoveTeach.Role = CalibrationDialogButtonRole.Normal;
            this._btnMoveTeach.Click += new System.EventHandler(this.BtnMoveTeach_Click);
            this._btnStart.Role = CalibrationDialogButtonRole.Primary;
            this._btnStart.Click += new System.EventHandler(this.BtnStart_Click);
            this._btnReload.Role = CalibrationDialogButtonRole.Normal;
            this._btnReload.Click += new System.EventHandler(this.BtnReload_Click);
            this._btnSave.Role = CalibrationDialogButtonRole.Dark;
            this._btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            this._btnClose.Role = CalibrationDialogButtonRole.Normal;
            this._btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            // 
            // NeedlePinCalibrationDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(238)))), ((int)(((byte)(238)))));
            this.ClientSize = new System.Drawing.Size(1164, 681);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("留묒? 怨좊뵓", 9F);
            this.MinimumSize = new System.Drawing.Size(980, 620);
            this.Name = "NeedlePinCalibrationDialog";
            this.Padding = new System.Windows.Forms.Padding(8);
            this.Text = "Needle Pin Calibration";
            this.rootLayout.ResumeLayout(false);
            this.mainLayout.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.groupSetting.ResumeLayout(false);
            this.settingLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._settingsGrid)).EndInit();
            this.groupSaved.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._resultGrid)).EndInit();
            this.groupTeaching.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._teachingGrid)).EndInit();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

    }
}


