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
        private System.Windows.Forms.DataGridViewTextBoxColumn colValid;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TableLayoutPanel buttonPanel;
        private System.Windows.Forms.Button btnCheck;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnApplyHomeOffset;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
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
            this.colValid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblStatus = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnCheck = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnApplyHomeOffset = new System.Windows.Forms.Button();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
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
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 62F));
            this.rootLayout.Size = new System.Drawing.Size(1120, 680);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(225, 120, 0);
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("Malgun Gothic", 15F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1120, 52);
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
            this.mainLayout.Size = new System.Drawing.Size(1096, 494);
            this.mainLayout.TabIndex = 1;
            // 
            // groupSettings
            // 
            this.groupSettings.Controls.Add(this.gridSettings);
            this.groupSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupSettings.Font = new System.Drawing.Font("Malgun Gothic", 10F, System.Drawing.FontStyle.Bold);
            this.groupSettings.Location = new System.Drawing.Point(0, 0);
            this.groupSettings.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.groupSettings.Name = "groupSettings";
            this.groupSettings.Size = new System.Drawing.Size(350, 494);
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
            this.gridSettings.Size = new System.Drawing.Size(344, 470);
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
            this.groupResults.Font = new System.Drawing.Font("Malgun Gothic", 10F, System.Drawing.FontStyle.Bold);
            this.groupResults.Location = new System.Drawing.Point(360, 0);
            this.groupResults.Margin = new System.Windows.Forms.Padding(0);
            this.groupResults.Name = "groupResults";
            this.groupResults.Size = new System.Drawing.Size(736, 494);
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
            this.colValid});
            this.gridResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridResults.Location = new System.Drawing.Point(3, 21);
            this.gridResults.MultiSelect = false;
            this.gridResults.Name = "gridResults";
            this.gridResults.ReadOnly = true;
            this.gridResults.RowHeadersVisible = false;
            this.gridResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridResults.Size = new System.Drawing.Size(730, 470);
            this.gridResults.TabIndex = 0;
            // 
            // columns
            // 
            this.colItem.HeaderText = "ITEM";
            this.colItem.Name = "colItem";
            this.colItem.Width = 95;
            this.colSide.HeaderText = "SIDE";
            this.colSide.Name = "colSide";
            this.colSide.Width = 70;
            this.colCollet.HeaderText = "COLLET";
            this.colCollet.Name = "colCollet";
            this.colCollet.Width = 70;
            this.colOffsetX.HeaderText = "OFFSET X";
            this.colOffsetX.Name = "colOffsetX";
            this.colOffsetX.Width = 95;
            this.colOffsetY.HeaderText = "OFFSET Y";
            this.colOffsetY.Name = "colOffsetY";
            this.colOffsetY.Width = 95;
            this.colTheta.HeaderText = "THETA";
            this.colTheta.Name = "colTheta";
            this.colTheta.Width = 90;
            this.colTZero.HeaderText = "T ZERO";
            this.colTZero.Name = "colTZero";
            this.colTZero.Width = 90;
            this.colValid.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colValid.HeaderText = "VALID";
            this.colValid.Name = "colValid";
            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("Malgun Gothic", 10F);
            this.lblStatus.Location = new System.Drawing.Point(12, 558);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(1096, 46);
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
            this.buttonPanel.Location = new System.Drawing.Point(12, 604);
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(12, 0, 12, 12);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.RowCount = 1;
            this.buttonPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonPanel.Size = new System.Drawing.Size(1096, 64);
            this.buttonPanel.TabIndex = 3;
            // 
            // buttons
            // 
            this.btnCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCheck.Name = "btnCheck";
            this.btnCheck.Text = "CHECK";
            this.btnCheck.Click += new System.EventHandler(this.btnCheck_Click);
            this.btnStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStart.Name = "btnStart";
            this.btnStart.Text = "START";
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            this.btnApplyHomeOffset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApplyHomeOffset.Name = "btnApplyHomeOffset";
            this.btnApplyHomeOffset.Text = "APPLY T HOME";
            this.btnApplyHomeOffset.Click += new System.EventHandler(this.btnApplyHomeOffset_Click);
            this.btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReload.Name = "btnReload";
            this.btnReload.Text = "RELOAD";
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.Name = "btnSave";
            this.btnSave.Text = "SAVE";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.Name = "btnClose";
            this.btnClose.Text = "CLOSE";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // ColletCalibrationDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1120, 680);
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
