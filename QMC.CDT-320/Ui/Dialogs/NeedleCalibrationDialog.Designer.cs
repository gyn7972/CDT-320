namespace QMC.CDT_320.Ui.Dialogs
{
    partial class NeedleCalibrationDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.TableLayoutPanel leftLayout;
        private System.Windows.Forms.GroupBox groupSettings;
        private System.Windows.Forms.TableLayoutPanel settingsLayout;
        private System.Windows.Forms.DataGridView _settingsGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingUnit;
        private CalibrationDialogButton _btnSaveParameters;
        private System.Windows.Forms.GroupBox groupResults;
        private System.Windows.Forms.DataGridView _resultGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResultItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResultValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResultUnit;
        private System.Windows.Forms.GroupBox groupTeaching;
        private System.Windows.Forms.DataGridView _teachingGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTeachingItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTeachingActual;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTeachingUnit;
        private System.Windows.Forms.Label _status;
        private System.Windows.Forms.TableLayoutPanel buttonPanel;
        private CalibrationDialogButton _btnCheck;
        private CalibrationDialogButton _btnUseCurrent;
        private CalibrationDialogButton _btnMoveTouch;
        private CalibrationDialogButton _btnStart;
        private CalibrationDialogButton _btnSeqStop;
        private CalibrationDialogButton _btnAvoid;
        private CalibrationDialogButton _btnReload;
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
            this.groupSettings = new System.Windows.Forms.GroupBox();
            this.settingsLayout = new System.Windows.Forms.TableLayoutPanel();
            this._settingsGrid = new System.Windows.Forms.DataGridView();
            this.colSettingItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSettingValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSettingUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._btnSaveParameters = new CalibrationDialogButton();
            this.groupResults = new System.Windows.Forms.GroupBox();
            this._resultGrid = new System.Windows.Forms.DataGridView();
            this.colResultItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResultValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResultUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupTeaching = new System.Windows.Forms.GroupBox();
            this._teachingGrid = new System.Windows.Forms.DataGridView();
            this.colTeachingItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTeachingActual = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTeachingUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._status = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.TableLayoutPanel();
            this._btnCheck = new CalibrationDialogButton();
            this._btnUseCurrent = new CalibrationDialogButton();
            this._btnMoveTouch = new CalibrationDialogButton();
            this._btnStart = new CalibrationDialogButton();
            this._btnSeqStop = new CalibrationDialogButton();
            this._btnAvoid = new CalibrationDialogButton();
            this._btnReload = new CalibrationDialogButton();
            this._btnSave = new CalibrationDialogButton();
            this._btnClose = new CalibrationDialogButton();
            this.rootLayout.SuspendLayout();
            this.mainLayout.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.groupSettings.SuspendLayout();
            this.settingsLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._settingsGrid)).BeginInit();
            this.groupResults.SuspendLayout();
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
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 54F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.rootLayout.Size = new System.Drawing.Size(1180, 720);
            this.rootLayout.TabIndex = 0;
            //
            // lblHeader
            //
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(230, 126, 0);
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1180, 54);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "NEEDLE Z CAL";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // mainLayout
            //
            this.mainLayout.ColumnCount = 2;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 470F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.leftLayout, 0, 0);
            this.mainLayout.Controls.Add(this.groupTeaching, 1, 0);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(8, 62);
            this.mainLayout.Margin = new System.Windows.Forms.Padding(8);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.RowCount = 1;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Size = new System.Drawing.Size(1164, 546);
            this.mainLayout.TabIndex = 1;
            //
            // leftLayout
            //
            this.leftLayout.ColumnCount = 1;
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Controls.Add(this.groupSettings, 0, 0);
            this.leftLayout.Controls.Add(this.groupResults, 0, 1);
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Location = new System.Drawing.Point(0, 0);
            this.leftLayout.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.RowCount = 2;
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 68F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 32F));
            this.leftLayout.Size = new System.Drawing.Size(462, 546);
            this.leftLayout.TabIndex = 0;
            //
            // groupSettings
            //
            this.groupSettings.Controls.Add(this.settingsLayout);
            this.groupSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupSettings.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.groupSettings.Location = new System.Drawing.Point(0, 0);
            this.groupSettings.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.groupSettings.Name = "groupSettings";
            this.groupSettings.Padding = new System.Windows.Forms.Padding(6);
            this.groupSettings.Size = new System.Drawing.Size(462, 365);
            this.groupSettings.TabIndex = 0;
            this.groupSettings.TabStop = false;
            this.groupSettings.Text = "CAL SETTING";
            //
            // settingsLayout
            //
            this.settingsLayout.ColumnCount = 1;
            this.settingsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.settingsLayout.Controls.Add(this._settingsGrid, 0, 0);
            this.settingsLayout.Controls.Add(this._btnSaveParameters, 0, 1);
            this.settingsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsLayout.Location = new System.Drawing.Point(6, 22);
            this.settingsLayout.Margin = new System.Windows.Forms.Padding(0);
            this.settingsLayout.Name = "settingsLayout";
            this.settingsLayout.RowCount = 2;
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.settingsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.settingsLayout.Size = new System.Drawing.Size(450, 337);
            this.settingsLayout.TabIndex = 0;
            //
            // _settingsGrid
            //
            this._settingsGrid.AllowUserToAddRows = false;
            this._settingsGrid.AllowUserToDeleteRows = false;
            this._settingsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._settingsGrid.BackgroundColor = System.Drawing.Color.White;
            this._settingsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSettingItem,
            this.colSettingValue,
            this.colSettingUnit});
            this._settingsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._settingsGrid.Location = new System.Drawing.Point(0, 0);
            this._settingsGrid.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this._settingsGrid.Name = "_settingsGrid";
            this._settingsGrid.ReadOnly = true;
            this._settingsGrid.RowHeadersVisible = false;
            this._settingsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._settingsGrid.Size = new System.Drawing.Size(450, 291);
            this._settingsGrid.TabIndex = 0;
            this._settingsGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.SettingsGrid_CellDoubleClick);
            this._settingsGrid.CellToolTipTextNeeded += new System.Windows.Forms.DataGridViewCellToolTipTextNeededEventHandler(this.SettingsGrid_CellToolTipTextNeeded);
            //
            // colSettingItem
            //
            this.colSettingItem.FillWeight = 52F;
            this.colSettingItem.HeaderText = "ITEM";
            this.colSettingItem.Name = "colSettingItem";
            this.colSettingItem.ReadOnly = true;
            //
            // colSettingValue
            //
            this.colSettingValue.FillWeight = 30F;
            this.colSettingValue.HeaderText = "VALUE";
            this.colSettingValue.Name = "colSettingValue";
            this.colSettingValue.ReadOnly = true;
            //
            // colSettingUnit
            //
            this.colSettingUnit.FillWeight = 18F;
            this.colSettingUnit.HeaderText = "UNIT";
            this.colSettingUnit.Name = "colSettingUnit";
            this.colSettingUnit.ReadOnly = true;
            //
            // _btnSaveParameters
            //
            this._btnSaveParameters.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnSaveParameters.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this._btnSaveParameters.Location = new System.Drawing.Point(0, 295);
            this._btnSaveParameters.Margin = new System.Windows.Forms.Padding(0);
            this._btnSaveParameters.Name = "_btnSaveParameters";
            this._btnSaveParameters.Role = CalibrationDialogButtonRole.Dark;
            this._btnSaveParameters.Size = new System.Drawing.Size(450, 42);
            this._btnSaveParameters.TabIndex = 1;
            this._btnSaveParameters.Text = "PARAMETER SAVE";
            this._btnSaveParameters.UseVisualStyleBackColor = false;
            this._btnSaveParameters.Click += new System.EventHandler(this.btnSaveParameters_Click);
            //
            // groupResults
            //
            this.groupResults.Controls.Add(this._resultGrid);
            this.groupResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupResults.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.groupResults.Location = new System.Drawing.Point(0, 371);
            this.groupResults.Margin = new System.Windows.Forms.Padding(0);
            this.groupResults.Name = "groupResults";
            this.groupResults.Padding = new System.Windows.Forms.Padding(6);
            this.groupResults.Size = new System.Drawing.Size(462, 175);
            this.groupResults.TabIndex = 1;
            this.groupResults.TabStop = false;
            this.groupResults.Text = "SAVED RESULT";
            //
            // _resultGrid
            //
            this._resultGrid.AllowUserToAddRows = false;
            this._resultGrid.AllowUserToDeleteRows = false;
            this._resultGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._resultGrid.BackgroundColor = System.Drawing.Color.White;
            this._resultGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colResultItem,
            this.colResultValue,
            this.colResultUnit});
            this._resultGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._resultGrid.Location = new System.Drawing.Point(6, 22);
            this._resultGrid.Name = "_resultGrid";
            this._resultGrid.ReadOnly = true;
            this._resultGrid.RowHeadersVisible = false;
            this._resultGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._resultGrid.Size = new System.Drawing.Size(450, 147);
            this._resultGrid.TabIndex = 0;
            //
            // result columns
            //
            this.colResultItem.FillWeight = 52F;
            this.colResultItem.HeaderText = "ITEM";
            this.colResultItem.Name = "colResultItem";
            this.colResultItem.ReadOnly = true;
            this.colResultValue.FillWeight = 30F;
            this.colResultValue.HeaderText = "VALUE";
            this.colResultValue.Name = "colResultValue";
            this.colResultValue.ReadOnly = true;
            this.colResultUnit.FillWeight = 18F;
            this.colResultUnit.HeaderText = "UNIT";
            this.colResultUnit.Name = "colResultUnit";
            this.colResultUnit.ReadOnly = true;
            //
            // groupTeaching
            //
            this.groupTeaching.Controls.Add(this._teachingGrid);
            this.groupTeaching.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupTeaching.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.groupTeaching.Location = new System.Drawing.Point(470, 0);
            this.groupTeaching.Margin = new System.Windows.Forms.Padding(0);
            this.groupTeaching.Name = "groupTeaching";
            this.groupTeaching.Padding = new System.Windows.Forms.Padding(6);
            this.groupTeaching.Size = new System.Drawing.Size(694, 546);
            this.groupTeaching.TabIndex = 1;
            this.groupTeaching.TabStop = false;
            this.groupTeaching.Text = "CURRENT POSITION";
            //
            // _teachingGrid
            //
            this._teachingGrid.AllowUserToAddRows = false;
            this._teachingGrid.AllowUserToDeleteRows = false;
            this._teachingGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._teachingGrid.BackgroundColor = System.Drawing.Color.White;
            this._teachingGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colTeachingItem,
            this.colTeachingActual,
            this.colTeachingUnit});
            this._teachingGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._teachingGrid.Location = new System.Drawing.Point(6, 22);
            this._teachingGrid.Name = "_teachingGrid";
            this._teachingGrid.ReadOnly = true;
            this._teachingGrid.RowHeadersVisible = false;
            this._teachingGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._teachingGrid.Size = new System.Drawing.Size(682, 518);
            this._teachingGrid.TabIndex = 0;
            //
            // teaching columns
            //
            this.colTeachingItem.FillWeight = 48F;
            this.colTeachingItem.HeaderText = "ITEM";
            this.colTeachingItem.Name = "colTeachingItem";
            this.colTeachingItem.ReadOnly = true;
            this.colTeachingActual.FillWeight = 34F;
            this.colTeachingActual.HeaderText = "ACTUAL";
            this.colTeachingActual.Name = "colTeachingActual";
            this.colTeachingActual.ReadOnly = true;
            this.colTeachingUnit.FillWeight = 18F;
            this.colTeachingUnit.HeaderText = "UNIT";
            this.colTeachingUnit.Name = "colTeachingUnit";
            this.colTeachingUnit.ReadOnly = true;
            //
            // _status
            //
            this._status.BackColor = System.Drawing.Color.WhiteSmoke;
            this._status.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._status.Dock = System.Windows.Forms.DockStyle.Fill;
            this._status.Location = new System.Drawing.Point(8, 616);
            this._status.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this._status.Name = "_status";
            this._status.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this._status.Size = new System.Drawing.Size(1164, 40);
            this._status.TabIndex = 2;
            this._status.Text = "USE CURRENT로 티칭 위치를 확인하고 START CAL을 실행하세요.";
            this._status.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // buttonPanel
            //
            this.buttonPanel.ColumnCount = 9;
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.12F));
            this.buttonPanel.Controls.Add(this._btnCheck, 0, 0);
            this.buttonPanel.Controls.Add(this._btnUseCurrent, 1, 0);
            this.buttonPanel.Controls.Add(this._btnMoveTouch, 2, 0);
            this.buttonPanel.Controls.Add(this._btnStart, 3, 0);
            this.buttonPanel.Controls.Add(this._btnSeqStop, 4, 0);
            this.buttonPanel.Controls.Add(this._btnAvoid, 5, 0);
            this.buttonPanel.Controls.Add(this._btnReload, 6, 0);
            this.buttonPanel.Controls.Add(this._btnSave, 7, 0);
            this.buttonPanel.Controls.Add(this._btnClose, 8, 0);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.Location = new System.Drawing.Point(8, 664);
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(8);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.RowCount = 1;
            this.buttonPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonPanel.Size = new System.Drawing.Size(1164, 48);
            this.buttonPanel.TabIndex = 3;
            //
            // footer buttons
            //
            this._btnCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnCheck.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnCheck.Name = "_btnCheck";
            this._btnCheck.Text = "CHECK";
            this._btnCheck.Click += new System.EventHandler(this.btnCheck_Click);
            this._btnUseCurrent.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnUseCurrent.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnUseCurrent.Name = "_btnUseCurrent";
            this._btnUseCurrent.Text = "USE CURRENT";
            this._btnUseCurrent.Click += new System.EventHandler(this.btnUseCurrent_Click);
            this._btnMoveTouch.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMoveTouch.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnMoveTouch.Name = "_btnMoveTouch";
            this._btnMoveTouch.Text = "MOVE TOUCH";
            this._btnMoveTouch.Click += new System.EventHandler(this.btnMoveTouch_Click);
            this._btnStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnStart.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnStart.Name = "_btnStart";
            this._btnStart.Role = CalibrationDialogButtonRole.Primary;
            this._btnStart.Text = "START CAL";
            this._btnStart.Click += new System.EventHandler(this.btnStart_Click);
            this._btnSeqStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnSeqStop.Enabled = false;
            this._btnSeqStop.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnSeqStop.Name = "_btnSeqStop";
            this._btnSeqStop.Text = "SEQ STOP";
            this._btnSeqStop.Click += new System.EventHandler(this.btnSeqStop_Click);
            this._btnAvoid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnAvoid.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnAvoid.Name = "_btnAvoid";
            this._btnAvoid.Text = "Z AVOID";
            this._btnAvoid.Click += new System.EventHandler(this.btnAvoid_Click);
            this._btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnReload.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnReload.Name = "_btnReload";
            this._btnReload.Text = "RELOAD";
            this._btnReload.Click += new System.EventHandler(this.btnReload_Click);
            this._btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnSave.Enabled = false;
            this._btnSave.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnSave.Name = "_btnSave";
            this._btnSave.Role = CalibrationDialogButtonRole.Dark;
            this._btnSave.Text = "SAVE RESULT";
            this._btnSave.Click += new System.EventHandler(this.btnSaveResult_Click);
            this._btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnClose.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._btnClose.Name = "_btnClose";
            this._btnClose.Text = "CLOSE";
            this._btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // NeedleCalibrationDialog
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(238, 238, 238);
            this.ClientSize = new System.Drawing.Size(1180, 720);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.MinimumSize = new System.Drawing.Size(980, 620);
            this.Name = "NeedleCalibrationDialog";
            this.Text = "Needle Z Calibration";
            this.rootLayout.ResumeLayout(false);
            this.mainLayout.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.groupSettings.ResumeLayout(false);
            this.settingsLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._settingsGrid)).EndInit();
            this.groupResults.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._resultGrid)).EndInit();
            this.groupTeaching.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._teachingGrid)).EndInit();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
