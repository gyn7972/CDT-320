namespace QMC.CDT_320.Ui.Dialogs
{
    partial class PickerZoneSetupDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel layoutRoot;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label lblSide;
        private System.Windows.Forms.ComboBox cboSide;
        private System.Windows.Forms.CheckBox chkUseEncoderZone;
        private System.Windows.Forms.Label lblTolerance;
        private System.Windows.Forms.TextBox txtTolerance;
        private System.Windows.Forms.Label lblCurrent;
        private System.Windows.Forms.DataGridView gridZones;
        private System.Windows.Forms.FlowLayoutPanel pnlButtons;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnTeachMin;
        private System.Windows.Forms.Button btnTeachMax;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colUse;
        private System.Windows.Forms.DataGridViewTextBoxColumn colZone;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMin;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMax;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCurrent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMatch;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle headerStyle = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle rightStyle = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle centerStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.layoutRoot = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.lblSide = new System.Windows.Forms.Label();
            this.cboSide = new System.Windows.Forms.ComboBox();
            this.chkUseEncoderZone = new System.Windows.Forms.CheckBox();
            this.lblTolerance = new System.Windows.Forms.Label();
            this.txtTolerance = new System.Windows.Forms.TextBox();
            this.lblCurrent = new System.Windows.Forms.Label();
            this.gridZones = new System.Windows.Forms.DataGridView();
            this.colUse = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colZone = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMax = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCurrent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMatch = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnTeachMin = new System.Windows.Forms.Button();
            this.btnTeachMax = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.layoutRoot.SuspendLayout();
            this.pnlTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridZones)).BeginInit();
            this.pnlButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // layoutRoot
            // 
            this.layoutRoot.ColumnCount = 1;
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.Controls.Add(this.lblTitle, 0, 0);
            this.layoutRoot.Controls.Add(this.pnlTop, 0, 1);
            this.layoutRoot.Controls.Add(this.gridZones, 0, 2);
            this.layoutRoot.Controls.Add(this.pnlButtons, 0, 3);
            this.layoutRoot.Controls.Add(this.lblStatus, 0, 4);
            this.layoutRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutRoot.Location = new System.Drawing.Point(12, 12);
            this.layoutRoot.Name = "layoutRoot";
            this.layoutRoot.RowCount = 5;
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.layoutRoot.Size = new System.Drawing.Size(760, 386);
            this.layoutRoot.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(3, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(754, 36);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Picker X Zone Setup";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlTop
            // 
            this.pnlTop.Controls.Add(this.lblSide);
            this.pnlTop.Controls.Add(this.cboSide);
            this.pnlTop.Controls.Add(this.chkUseEncoderZone);
            this.pnlTop.Controls.Add(this.lblTolerance);
            this.pnlTop.Controls.Add(this.txtTolerance);
            this.pnlTop.Controls.Add(this.lblCurrent);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTop.Location = new System.Drawing.Point(3, 39);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(754, 58);
            this.pnlTop.TabIndex = 1;
            // 
            // lblSide
            // 
            this.lblSide.Location = new System.Drawing.Point(0, 8);
            this.lblSide.Name = "lblSide";
            this.lblSide.Size = new System.Drawing.Size(56, 24);
            this.lblSide.TabIndex = 0;
            this.lblSide.Text = "Picker";
            this.lblSide.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboSide
            // 
            this.cboSide.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSide.FormattingEnabled = true;
            this.cboSide.Items.AddRange(new object[] {
            "Front",
            "Rear"});
            this.cboSide.Location = new System.Drawing.Point(62, 8);
            this.cboSide.Name = "cboSide";
            this.cboSide.Size = new System.Drawing.Size(120, 23);
            this.cboSide.TabIndex = 1;
            this.cboSide.SelectedIndexChanged += new System.EventHandler(this.OnSideChanged);
            // 
            // chkUseEncoderZone
            // 
            this.chkUseEncoderZone.AutoSize = true;
            this.chkUseEncoderZone.Location = new System.Drawing.Point(204, 10);
            this.chkUseEncoderZone.Name = "chkUseEncoderZone";
            this.chkUseEncoderZone.Size = new System.Drawing.Size(142, 19);
            this.chkUseEncoderZone.TabIndex = 2;
            this.chkUseEncoderZone.Text = "Use encoder zone";
            this.chkUseEncoderZone.UseVisualStyleBackColor = true;
            // 
            // lblTolerance
            // 
            this.lblTolerance.Location = new System.Drawing.Point(370, 8);
            this.lblTolerance.Name = "lblTolerance";
            this.lblTolerance.Size = new System.Drawing.Size(96, 24);
            this.lblTolerance.TabIndex = 3;
            this.lblTolerance.Text = "Tolerance";
            this.lblTolerance.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtTolerance
            // 
            this.txtTolerance.Location = new System.Drawing.Point(472, 8);
            this.txtTolerance.Name = "txtTolerance";
            this.txtTolerance.ReadOnly = true;
            this.txtTolerance.Size = new System.Drawing.Size(80, 23);
            this.txtTolerance.TabIndex = 4;
            this.txtTolerance.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtTolerance.Click += new System.EventHandler(this.OnToleranceClick);
            // 
            // lblCurrent
            // 
            this.lblCurrent.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblCurrent.Location = new System.Drawing.Point(574, 8);
            this.lblCurrent.Name = "lblCurrent";
            this.lblCurrent.Size = new System.Drawing.Size(177, 24);
            this.lblCurrent.TabIndex = 5;
            this.lblCurrent.Text = "Current X: -";
            this.lblCurrent.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // gridZones
            // 
            this.gridZones.AllowUserToAddRows = false;
            this.gridZones.AllowUserToDeleteRows = false;
            this.gridZones.AllowUserToResizeRows = false;
            this.gridZones.BackgroundColor = System.Drawing.Color.White;
            headerStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            headerStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            headerStyle.Font = new System.Drawing.Font("맑은 고딕", 9F);
            headerStyle.ForeColor = System.Drawing.Color.White;
            this.gridZones.ColumnHeadersDefaultCellStyle = headerStyle;
            this.gridZones.ColumnHeadersHeight = 28;
            this.gridZones.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colUse,
            this.colZone,
            this.colMin,
            this.colMax,
            this.colCurrent,
            this.colMatch});
            this.gridZones.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridZones.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.gridZones.EnableHeadersVisualStyles = false;
            this.gridZones.Location = new System.Drawing.Point(3, 103);
            this.gridZones.MultiSelect = false;
            this.gridZones.Name = "gridZones";
            this.gridZones.RowHeadersVisible = false;
            this.gridZones.RowTemplate.Height = 26;
            this.gridZones.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridZones.Size = new System.Drawing.Size(754, 196);
            this.gridZones.TabIndex = 2;
            this.gridZones.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnGridCellClick);
            this.gridZones.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnGridCellContentClick);
            // 
            // colUse
            // 
            this.colUse.HeaderText = "Use";
            this.colUse.MinimumWidth = 50;
            this.colUse.Name = "colUse";
            this.colUse.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colUse.Width = 56;
            // 
            // colZone
            // 
            this.colZone.HeaderText = "Zone";
            this.colZone.MinimumWidth = 90;
            this.colZone.Name = "colZone";
            this.colZone.ReadOnly = true;
            this.colZone.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colZone.Width = 120;
            // 
            // colMin
            // 
            rightStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.colMin.DefaultCellStyle = rightStyle;
            this.colMin.HeaderText = "Min X";
            this.colMin.MinimumWidth = 100;
            this.colMin.Name = "colMin";
            this.colMin.ReadOnly = true;
            this.colMin.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colMin.Width = 120;
            // 
            // colMax
            // 
            this.colMax.DefaultCellStyle = rightStyle;
            this.colMax.HeaderText = "Max X";
            this.colMax.MinimumWidth = 100;
            this.colMax.Name = "colMax";
            this.colMax.ReadOnly = true;
            this.colMax.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colMax.Width = 120;
            // 
            // colCurrent
            // 
            this.colCurrent.DefaultCellStyle = rightStyle;
            this.colCurrent.HeaderText = "Current X";
            this.colCurrent.MinimumWidth = 100;
            this.colCurrent.Name = "colCurrent";
            this.colCurrent.ReadOnly = true;
            this.colCurrent.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colCurrent.Width = 120;
            // 
            // colMatch
            // 
            centerStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.colMatch.DefaultCellStyle = centerStyle;
            this.colMatch.HeaderText = "Match";
            this.colMatch.MinimumWidth = 80;
            this.colMatch.Name = "colMatch";
            this.colMatch.ReadOnly = true;
            this.colMatch.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colMatch.Width = 90;
            // 
            // pnlButtons
            // 
            this.pnlButtons.Controls.Add(this.btnReload);
            this.pnlButtons.Controls.Add(this.btnTeachMin);
            this.pnlButtons.Controls.Add(this.btnTeachMax);
            this.pnlButtons.Controls.Add(this.btnSave);
            this.pnlButtons.Controls.Add(this.btnClose);
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlButtons.Location = new System.Drawing.Point(3, 305);
            this.pnlButtons.Name = "pnlButtons";
            this.pnlButtons.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.pnlButtons.Size = new System.Drawing.Size(754, 50);
            this.pnlButtons.TabIndex = 3;
            // 
            // btnReload
            // 
            this.btnReload.Location = new System.Drawing.Point(4, 12);
            this.btnReload.Margin = new System.Windows.Forms.Padding(4);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(100, 32);
            this.btnReload.TabIndex = 0;
            this.btnReload.Text = "Reload";
            this.btnReload.UseVisualStyleBackColor = true;
            this.btnReload.Click += new System.EventHandler(this.OnReloadClick);
            // 
            // btnTeachMin
            // 
            this.btnTeachMin.Location = new System.Drawing.Point(112, 12);
            this.btnTeachMin.Margin = new System.Windows.Forms.Padding(4);
            this.btnTeachMin.Name = "btnTeachMin";
            this.btnTeachMin.Size = new System.Drawing.Size(112, 32);
            this.btnTeachMin.TabIndex = 1;
            this.btnTeachMin.Text = "Teach Min";
            this.btnTeachMin.UseVisualStyleBackColor = true;
            this.btnTeachMin.Click += new System.EventHandler(this.OnTeachMinClick);
            // 
            // btnTeachMax
            // 
            this.btnTeachMax.Location = new System.Drawing.Point(232, 12);
            this.btnTeachMax.Margin = new System.Windows.Forms.Padding(4);
            this.btnTeachMax.Name = "btnTeachMax";
            this.btnTeachMax.Size = new System.Drawing.Size(112, 32);
            this.btnTeachMax.TabIndex = 2;
            this.btnTeachMax.Text = "Teach Max";
            this.btnTeachMax.UseVisualStyleBackColor = true;
            this.btnTeachMax.Click += new System.EventHandler(this.OnTeachMaxClick);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(352, 12);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 32);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.OnSaveClick);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(460, 12);
            this.btnClose.Margin = new System.Windows.Forms.Padding(4);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 32);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.OnCloseClick);
            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Location = new System.Drawing.Point(3, 358);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(754, 28);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "Loaded.";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // PickerZoneSetupDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(784, 410);
            this.Controls.Add(this.layoutRoot);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.Name = "PickerZoneSetupDialog";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Picker X Zone Setup";
            this.layoutRoot.ResumeLayout(false);
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridZones)).EndInit();
            this.pnlButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
