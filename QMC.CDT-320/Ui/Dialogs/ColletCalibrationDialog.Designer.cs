namespace QMC.CDT_320.Ui.Dialogs
{
    partial class ColletCalibrationDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.GroupBox groupSettings;
        private System.Windows.Forms.DataGridView gridSettings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingUnit;
        private System.Windows.Forms.GroupBox groupResults;
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
        private System.Windows.Forms.DataGridViewTextBoxColumn colValid;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TableLayoutPanel buttonPanel;
        private CalibrationDialogButton btnCheck;
        private CalibrationDialogButton btnStart;
        private CalibrationDialogButton btnApplyHomeOffset;
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
            this.groupSettings = new System.Windows.Forms.GroupBox();
            this.gridSettings = new System.Windows.Forms.DataGridView();
            this.colSettingName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSettingValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSettingUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupResults = new System.Windows.Forms.GroupBox();
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
            this.colValid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblStatus = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnCheck = new CalibrationDialogButton();
            this.btnStart = new CalibrationDialogButton();
            this.btnApplyHomeOffset = new CalibrationDialogButton();
            this.btnReload = new CalibrationDialogButton();
            this.btnSave = new CalibrationDialogButton();
            this.btnClose = new CalibrationDialogButton();
            this.rootLayout.SuspendLayout();
            this.mainLayout.SuspendLayout();
            this.groupSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSettings)).BeginInit();
            this.groupResults.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridResults)).BeginInit();
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
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 62F));
            this.rootLayout.Size = new System.Drawing.Size(1161, 680);
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
            this.lblHeader.Size = new System.Drawing.Size(1161, 52);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "COLLET CALIBRATION";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 2;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 360F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.groupSettings, 0, 0);
            this.mainLayout.Controls.Add(this.groupResults, 1, 0);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(12, 64);
            this.mainLayout.Margin = new System.Windows.Forms.Padding(12);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.RowCount = 1;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Size = new System.Drawing.Size(1137, 496);
            this.mainLayout.TabIndex = 1;
            // 
            // groupSettings
            // 
            this.groupSettings.Controls.Add(this.gridSettings);
            this.groupSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupSettings.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.groupSettings.Location = new System.Drawing.Point(0, 0);
            this.groupSettings.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.groupSettings.Name = "groupSettings";
            this.groupSettings.Size = new System.Drawing.Size(350, 496);
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
            this.gridSettings.Location = new System.Drawing.Point(3, 21);
            this.gridSettings.MultiSelect = false;
            this.gridSettings.Name = "gridSettings";
            this.gridSettings.RowHeadersVisible = false;
            this.gridSettings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridSettings.Size = new System.Drawing.Size(344, 472);
            this.gridSettings.TabIndex = 0;
            this.gridSettings.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridSettings_CellDoubleClick);
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
            this.groupResults.Controls.Add(this.gridResults);
            this.groupResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupResults.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.groupResults.Location = new System.Drawing.Point(360, 0);
            this.groupResults.Margin = new System.Windows.Forms.Padding(0);
            this.groupResults.Name = "groupResults";
            this.groupResults.Size = new System.Drawing.Size(777, 496);
            this.groupResults.TabIndex = 1;
            this.groupResults.TabStop = false;
            this.groupResults.Text = "SAVED COLLET OFFSET";
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
            this.colValid});
            this.gridResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridResults.Location = new System.Drawing.Point(3, 21);
            this.gridResults.MultiSelect = false;
            this.gridResults.Name = "gridResults";
            this.gridResults.ReadOnly = true;
            this.gridResults.RowHeadersVisible = false;
            this.gridResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridResults.Size = new System.Drawing.Size(771, 472);
            this.gridResults.TabIndex = 0;
            // 
            // colItem
            // 
            this.colItem.HeaderText = "ITEM";
            this.colItem.Name = "colItem";
            this.colItem.ReadOnly = true;
            this.colItem.Width = 80;
            // 
            // colSide
            // 
            this.colSide.HeaderText = "SIDE";
            this.colSide.Name = "colSide";
            this.colSide.ReadOnly = true;
            this.colSide.Width = 60;
            // 
            // colCollet
            // 
            this.colCollet.HeaderText = "COLLET";
            this.colCollet.Name = "colCollet";
            this.colCollet.ReadOnly = true;
            this.colCollet.Width = 60;
            // 
            // colOffsetX
            // 
            this.colOffsetX.HeaderText = "OFFSET X";
            this.colOffsetX.Name = "colOffsetX";
            this.colOffsetX.ReadOnly = true;
            this.colOffsetX.Width = 85;
            // 
            // colOffsetY
            // 
            this.colOffsetY.HeaderText = "OFFSET Y";
            this.colOffsetY.Name = "colOffsetY";
            this.colOffsetY.ReadOnly = true;
            this.colOffsetY.Width = 85;
            // 
            // colTheta
            // 
            this.colTheta.HeaderText = "THETA";
            this.colTheta.Name = "colTheta";
            this.colTheta.ReadOnly = true;
            this.colTheta.Width = 80;
            // 
            // colTZero
            // 
            this.colTZero.HeaderText = "T ZERO";
            this.colTZero.Name = "colTZero";
            this.colTZero.ReadOnly = true;
            this.colTZero.Width = 80;
            // 
            // colFinalX
            // 
            this.colFinalX.HeaderText = "FINAL X";
            this.colFinalX.Name = "colFinalX";
            this.colFinalX.ReadOnly = true;
            this.colFinalX.Width = 85;
            // 
            // colFinalY
            // 
            this.colFinalY.HeaderText = "FINAL Y";
            this.colFinalY.Name = "colFinalY";
            this.colFinalY.ReadOnly = true;
            this.colFinalY.Width = 85;
            // 
            // colValid
            // 
            this.colValid.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colValid.HeaderText = "VALID";
            this.colValid.Name = "colValid";
            this.colValid.ReadOnly = true;
            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblStatus.Location = new System.Drawing.Point(12, 572);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(1137, 46);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "대기 중입니다.";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonPanel
            // 
            this.buttonPanel.ColumnCount = 6;
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.buttonPanel.Controls.Add(this.btnCheck, 0, 0);
            this.buttonPanel.Controls.Add(this.btnStart, 1, 0);
            this.buttonPanel.Controls.Add(this.btnApplyHomeOffset, 2, 0);
            this.buttonPanel.Controls.Add(this.btnReload, 3, 0);
            this.buttonPanel.Controls.Add(this.btnSave, 4, 0);
            this.buttonPanel.Controls.Add(this.btnClose, 5, 0);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.Location = new System.Drawing.Point(12, 618);
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(12, 0, 12, 12);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.RowCount = 1;
            this.buttonPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonPanel.Size = new System.Drawing.Size(1137, 50);
            this.buttonPanel.TabIndex = 3;
            // 
            // btnCheck
            // 
            this.btnCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCheck.Location = new System.Drawing.Point(3, 3);
            this.btnCheck.Name = "btnCheck";
            this.btnCheck.Size = new System.Drawing.Size(183, 44);
            this.btnCheck.TabIndex = 0;
            this.btnCheck.Text = "CHECK";
            this.btnCheck.Role = CalibrationDialogButtonRole.Normal;
            this.btnCheck.Click += new System.EventHandler(this.btnCheck_Click);
            // 
            // btnStart
            // 
            this.btnStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStart.Location = new System.Drawing.Point(192, 3);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(183, 44);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "START";
            this.btnStart.Role = CalibrationDialogButtonRole.Primary;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnApplyHomeOffset
            // 
            this.btnApplyHomeOffset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApplyHomeOffset.Location = new System.Drawing.Point(381, 3);
            this.btnApplyHomeOffset.Name = "btnApplyHomeOffset";
            this.btnApplyHomeOffset.Size = new System.Drawing.Size(183, 44);
            this.btnApplyHomeOffset.TabIndex = 2;
            this.btnApplyHomeOffset.Text = "APPLY T HOME";
            this.btnApplyHomeOffset.Role = CalibrationDialogButtonRole.Normal;
            this.btnApplyHomeOffset.Click += new System.EventHandler(this.btnApplyHomeOffset_Click);
            // 
            // btnReload
            // 
            this.btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReload.Location = new System.Drawing.Point(570, 3);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(183, 44);
            this.btnReload.TabIndex = 3;
            this.btnReload.Text = "RELOAD";
            this.btnReload.Role = CalibrationDialogButtonRole.Normal;
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            // 
            // btnSave
            // 
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.Location = new System.Drawing.Point(759, 3);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(183, 44);
            this.btnSave.TabIndex = 4;
            this.btnSave.Text = "SAVE";
            this.btnSave.Role = CalibrationDialogButtonRole.Dark;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnClose
            // 
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.Location = new System.Drawing.Point(948, 3);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(186, 44);
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "CLOSE";
            this.btnClose.Role = CalibrationDialogButtonRole.Normal;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // ColletCalibrationDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1161, 680);
            this.Controls.Add(this.rootLayout);
            this.MinimumSize = new System.Drawing.Size(980, 600);
            this.Name = "ColletCalibrationDialog";
            this.Text = "COLLET CALIBRATION";
            this.rootLayout.ResumeLayout(false);
            this.mainLayout.ResumeLayout(false);
            this.groupSettings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridSettings)).EndInit();
            this.groupResults.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridResults)).EndInit();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}


