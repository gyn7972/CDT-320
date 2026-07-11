namespace QMC.CDT_320.Ui.Pages.Settings
{
    partial class AxisSetupPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel _compactLayout;
        private System.Windows.Forms.Label lblSubHeader;
        private System.Windows.Forms.GroupBox _gridGroup;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.GroupBox _actionGroup;
        private System.Windows.Forms.TableLayoutPanel actionRow;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSave;
        private QMC.CDT_320.Ui.Controls.ActionButton btnReload;
        private QMC.CDT_320.Ui.Controls.ActionButton btnReset;
        private QMC.CDT_320.Ui.Controls.ActionButton btnApply;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSharedRailX;
        private QMC.CDT_320.Ui.Controls.ActionButton btnPickerZone;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            this._compactLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblSubHeader = new System.Windows.Forms.Label();
            this._gridGroup = new System.Windows.Forms.GroupBox();
            this.grid = new System.Windows.Forms.DataGridView();
            this.NO = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MODULE = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NAME = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BOARD = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CH = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UNIT = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.STROKE = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SIM = new System.Windows.Forms.DataGridViewButtonColumn();
            this.SLN = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SLP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._actionGroup = new System.Windows.Forms.GroupBox();
            this.actionRow = new System.Windows.Forms.TableLayoutPanel();
            this.btnSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnReload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnReset = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnApply = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSharedRailX = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnPickerZone = new QMC.CDT_320.Ui.Controls.ActionButton();
            this._compactLayout.SuspendLayout();
            this._gridGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this._actionGroup.SuspendLayout();
            this.actionRow.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblSubHeader
            // 
            this.lblSubHeader.BackColor = UiTheme.StatusBarBg;
            this.lblSubHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSubHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblSubHeader.ForeColor = System.Drawing.Color.White;
            this.lblSubHeader.Location = new System.Drawing.Point(8, 7);
            this.lblSubHeader.Name = "lblSubHeader";
            this.lblSubHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblSubHeader.Size = new System.Drawing.Size(1662, 26);
            this.lblSubHeader.TabIndex = 0;
            this.lblSubHeader.Text = "AXIS SETUP — Simulation / Unit / Stroke / Soft Limit (37 axes)";
            this.lblSubHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("맑은 고딕", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.grid.ColumnHeadersHeight = 29;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.NO,
            this.MODULE,
            this.NAME,
            this.BOARD,
            this.CH,
            this.UNIT,
            this.STROKE,
            this.SIM,
            this.SLN,
            this.SLP});
            this.grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grid.EnableHeadersVisualStyles = false;
            this.grid.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.grid.Location = new System.Drawing.Point(8, 37);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.RowHeadersWidth = 51;
            this.grid.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.grid.Size = new System.Drawing.Size(1662, 796);
            this.grid.TabIndex = 1;
            this.grid.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnGridCellClick);
            this.grid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnCellDoubleClick);
            this.grid.ColumnHeaderMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.OnColumnHeaderMouseDoubleClick);
            this.grid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.OnGridDataError);
            // 
            // NO
            // 
            this.NO.HeaderText = "NO";
            this.NO.MinimumWidth = 6;
            this.NO.Name = "NO";
            this.NO.ReadOnly = true;
            this.NO.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // MODULE
            // 
            this.MODULE.HeaderText = "MODULE";
            this.MODULE.MinimumWidth = 6;
            this.MODULE.Name = "MODULE";
            this.MODULE.ReadOnly = true;
            this.MODULE.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // NAME
            // 
            this.NAME.HeaderText = "AXIS NAME";
            this.NAME.MinimumWidth = 6;
            this.NAME.Name = "NAME";
            this.NAME.ReadOnly = true;
            this.NAME.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // BOARD
            // 
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.BOARD.DefaultCellStyle = dataGridViewCellStyle2;
            this.BOARD.HeaderText = "BOARD#";
            this.BOARD.MinimumWidth = 6;
            this.BOARD.Name = "BOARD";
            this.BOARD.ReadOnly = true;
            this.BOARD.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // CH
            // 
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.CH.DefaultCellStyle = dataGridViewCellStyle3;
            this.CH.HeaderText = "CH (slot)";
            this.CH.MinimumWidth = 6;
            this.CH.Name = "CH";
            this.CH.ReadOnly = true;
            this.CH.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // UNIT
            // 
            this.UNIT.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
            this.UNIT.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.UNIT.HeaderText = "UNIT";
            this.UNIT.Items.AddRange(new object[] {
            "mm",
            "um",
            "deg"});
            this.UNIT.MinimumWidth = 6;
            this.UNIT.Name = "UNIT";
            this.UNIT.ReadOnly = true;
            // 
            // STROKE
            // 
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.STROKE.DefaultCellStyle = dataGridViewCellStyle4;
            this.STROKE.HeaderText = "STROKE";
            this.STROKE.MinimumWidth = 6;
            this.STROKE.Name = "STROKE";
            this.STROKE.ReadOnly = true;
            this.STROKE.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // SIM
            // 
            this.SIM.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.SIM.HeaderText = "SIM";
            this.SIM.MinimumWidth = 6;
            this.SIM.Name = "SIM";
            this.SIM.ReadOnly = true;
            this.SIM.Text = "";
            // 
            // SLN
            // 
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.SLN.DefaultCellStyle = dataGridViewCellStyle5;
            this.SLN.HeaderText = "SOFT LIMIT(-)";
            this.SLN.MinimumWidth = 6;
            this.SLN.Name = "SLN";
            this.SLN.ReadOnly = true;
            this.SLN.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // SLP
            // 
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.SLP.DefaultCellStyle = dataGridViewCellStyle6;
            this.SLP.HeaderText = "SOFT LIMIT(+)";
            this.SLP.MinimumWidth = 6;
            this.SLP.Name = "SLP";
            this.SLP.ReadOnly = true;
            this.SLP.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // _compactLayout
            //
            this._compactLayout.BackColor = System.Drawing.Color.White;
            this._compactLayout.ColumnCount = 1;
            this._compactLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._compactLayout.Controls.Add(this.lblSubHeader, 0, 0);
            this._compactLayout.Controls.Add(this._gridGroup, 0, 1);
            this._compactLayout.Controls.Add(this._actionGroup, 0, 3);
            this._compactLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this._compactLayout.Location = new System.Drawing.Point(0, 0);
            this._compactLayout.Margin = new System.Windows.Forms.Padding(0);
            this._compactLayout.Name = "_compactLayout";
            this._compactLayout.RowCount = 4;
            this._compactLayout.SetRowSpan(this._gridGroup, 2);
            this._compactLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this._compactLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this._compactLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this._compactLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this._compactLayout.Size = new System.Drawing.Size(1678, 900);
            this._compactLayout.TabIndex = 0;
            //
            // _gridGroup
            //
            this._gridGroup.Controls.Add(this.grid);
            this._gridGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this._gridGroup.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._gridGroup.Location = new System.Drawing.Point(0, 30);
            this._gridGroup.Margin = new System.Windows.Forms.Padding(0);
            this._gridGroup.Name = "_gridGroup";
            this._gridGroup.Padding = new System.Windows.Forms.Padding(1, 9, 1, 1);
            this._gridGroup.Size = new System.Drawing.Size(1678, 783);
            this._gridGroup.TabIndex = 1;
            this._gridGroup.TabStop = false;
            this._gridGroup.Text = "AXIS SETUP LIST";
            //
            // _actionGroup
            //
            this._actionGroup.Controls.Add(this.actionRow);
            this._actionGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this._actionGroup.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._actionGroup.Location = new System.Drawing.Point(0, 813);
            this._actionGroup.Margin = new System.Windows.Forms.Padding(0);
            this._actionGroup.Name = "_actionGroup";
            this._actionGroup.Padding = new System.Windows.Forms.Padding(1, 9, 1, 1);
            this._actionGroup.Size = new System.Drawing.Size(1678, 87);
            this._actionGroup.TabIndex = 2;
            this._actionGroup.TabStop = false;
            this._actionGroup.Text = "ACTION";
            //
            // actionRow
            //
            this.actionRow.BackColor = System.Drawing.Color.White;
            this.actionRow.ColumnCount = 14;
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionRow.Controls.Add(this.btnSave, 0, 0);
            this.actionRow.Controls.Add(this.btnReload, 1, 0);
            this.actionRow.Controls.Add(this.btnReset, 2, 0);
            this.actionRow.Controls.Add(this.btnApply, 3, 0);
            this.actionRow.Controls.Add(this.btnSharedRailX, 4, 0);
            this.actionRow.Controls.Add(this.btnPickerZone, 5, 0);
            this.actionRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionRow.Location = new System.Drawing.Point(1, 22);
            this.actionRow.Margin = new System.Windows.Forms.Padding(0);
            this.actionRow.Name = "actionRow";
            this.actionRow.Padding = new System.Windows.Forms.Padding(1);
            this.actionRow.RowCount = 1;
            this.actionRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionRow.Size = new System.Drawing.Size(1676, 64);
            this.actionRow.TabIndex = 0;
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(12, 12);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(120, 44);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "SAVE";
            this.btnSave.Click += new System.EventHandler(this.OnSaveClick);
            // 
            // btnReload
            // 
            this.btnReload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnReload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReload.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnReload.ForeColor = System.Drawing.Color.White;
            this.btnReload.Location = new System.Drawing.Point(140, 12);
            this.btnReload.Margin = new System.Windows.Forms.Padding(4);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(120, 44);
            this.btnReload.TabIndex = 1;
            this.btnReload.Text = "RELOAD";
            this.btnReload.Click += new System.EventHandler(this.OnReloadClick);
            // 
            // btnReset
            // 
            this.btnReset.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnReset.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReset.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnReset.ForeColor = System.Drawing.Color.White;
            this.btnReset.Location = new System.Drawing.Point(268, 12);
            this.btnReset.Margin = new System.Windows.Forms.Padding(4);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(160, 44);
            this.btnReset.TabIndex = 2;
            this.btnReset.Text = "RESET DEFAULT";
            this.btnReset.Click += new System.EventHandler(this.OnResetClick);
            // 
            // btnApply
            // 
            this.btnApply.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnApply.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnApply.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnApply.ForeColor = System.Drawing.Color.White;
            this.btnApply.Location = new System.Drawing.Point(436, 12);
            this.btnApply.Margin = new System.Windows.Forms.Padding(4);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(248, 44);
            this.btnApply.TabIndex = 3;
            this.btnApply.Text = "APPLY (Soft Limit 반영)";
            this.btnApply.Click += new System.EventHandler(this.OnApplyClick);
            // 
            // btnSharedRailX
            // 
            this.btnSharedRailX.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(96)))), ((int)(((byte)(96)))));
            this.btnSharedRailX.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSharedRailX.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnSharedRailX.ForeColor = System.Drawing.Color.White;
            this.btnSharedRailX.Location = new System.Drawing.Point(692, 12);
            this.btnSharedRailX.Margin = new System.Windows.Forms.Padding(4);
            this.btnSharedRailX.Name = "btnSharedRailX";
            this.btnSharedRailX.Size = new System.Drawing.Size(180, 44);
            this.btnSharedRailX.TabIndex = 4;
            this.btnSharedRailX.Text = "SHARED RAIL X";
            this.btnSharedRailX.Click += new System.EventHandler(this.OnSharedRailXClick);
            // 
            // btnPickerZone
            // 
            this.btnPickerZone.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(96)))), ((int)(((byte)(96)))));
            this.btnPickerZone.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPickerZone.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnPickerZone.ForeColor = System.Drawing.Color.White;
            this.btnPickerZone.Location = new System.Drawing.Point(880, 12);
            this.btnPickerZone.Margin = new System.Windows.Forms.Padding(4);
            this.btnPickerZone.Name = "btnPickerZone";
            this.btnPickerZone.Size = new System.Drawing.Size(160, 44);
            this.btnPickerZone.TabIndex = 5;
            this.btnPickerZone.Text = "PICKER ZONE";
            this.btnPickerZone.Click += new System.EventHandler(this.OnPickerZoneClick);
            // 
            // AxisSetupPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this._compactLayout);
            this.Name = "AxisSetupPage";
            this.Size = new System.Drawing.Size(1678, 900);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this._gridGroup.ResumeLayout(false);
            this.actionRow.ResumeLayout(false);
            this._actionGroup.ResumeLayout(false);
            this._compactLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.DataGridViewTextBoxColumn NO;
        private System.Windows.Forms.DataGridViewTextBoxColumn MODULE;
        private System.Windows.Forms.DataGridViewTextBoxColumn NAME;
        private System.Windows.Forms.DataGridViewTextBoxColumn BOARD;
        private System.Windows.Forms.DataGridViewTextBoxColumn CH;
        private System.Windows.Forms.DataGridViewComboBoxColumn UNIT;
        private System.Windows.Forms.DataGridViewTextBoxColumn STROKE;
        private System.Windows.Forms.DataGridViewButtonColumn SIM;
        private System.Windows.Forms.DataGridViewTextBoxColumn SLN;
        private System.Windows.Forms.DataGridViewTextBoxColumn SLP;
    }
}
