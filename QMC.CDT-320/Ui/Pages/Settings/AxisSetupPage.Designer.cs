namespace QMC.CDT_320.Ui.Pages.Settings
{
    partial class AxisSetupPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblSubHeader;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colModule;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBoard;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCh;
        private System.Windows.Forms.DataGridViewTextBoxColumn colUnit;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStroke;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBrake;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSln;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSlp;
        private System.Windows.Forms.DataGridViewTextBoxColumn colVel;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHomeDir;
        private System.Windows.Forms.FlowLayoutPanel actionsPanel;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSave;
        private QMC.CDT_320.Ui.Controls.ActionButton btnReload;
        private QMC.CDT_320.Ui.Controls.ActionButton btnReset;
        private QMC.CDT_320.Ui.Controls.ActionButton btnApply;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblSubHeader = new System.Windows.Forms.Label();
            this.grid = new System.Windows.Forms.DataGridView();
            this.colNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colModule = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBoard = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCh = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStroke = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBrake = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSln = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSlp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colVel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHomeDir = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.actionsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnReload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnReset = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnApply = new QMC.CDT_320.Ui.Controls.ActionButton();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.actionsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblSubHeader
            // 
            this.lblSubHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblSubHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblSubHeader.ForeColor = System.Drawing.Color.White;
            this.lblSubHeader.Location = new System.Drawing.Point(8, 36);
            this.lblSubHeader.Name = "lblSubHeader";
            this.lblSubHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblSubHeader.Size = new System.Drawing.Size(1400, 26);
            this.lblSubHeader.TabIndex = 0;
            this.lblSubHeader.Text = "AXIS SETUP — Stroke / Soft Limit / Velocity / Brake / Home Direction (37 axes)";
            this.lblSubHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.ColumnHeadersDefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.grid.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.grid.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grid.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colNo,
                this.colModule,
                this.colName,
                this.colBoard,
                this.colCh,
                this.colUnit,
                this.colStroke,
                this.colBrake,
                this.colSln,
                this.colSlp,
                this.colVel,
                this.colHomeDir});
            this.grid.EnableHeadersVisualStyles = false;
            this.grid.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.grid.Location = new System.Drawing.Point(8, 66);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.RowHeadersVisible = false;
            this.grid.RowTemplate.Height = 22;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(1400, 800);
            this.grid.TabIndex = 1;
            this.grid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnCellEdit);
            // 
            // colNo
            // 
            this.colNo.HeaderText = "NO";
            this.colNo.Name = "NO";
            this.colNo.ReadOnly = true;
            // 
            // colModule
            // 
            this.colModule.HeaderText = "MODULE";
            this.colModule.Name = "MODULE";
            this.colModule.ReadOnly = true;
            // 
            // colName
            // 
            this.colName.HeaderText = "AXIS NAME";
            this.colName.Name = "NAME";
            this.colName.ReadOnly = true;
            // 
            // colBoard
            // 
            this.colBoard.HeaderText = "BOARD#";
            this.colBoard.Name = "BOARD";
            // 
            // colCh
            // 
            this.colCh.HeaderText = "CH (slot)";
            this.colCh.Name = "CH";
            // 
            // colUnit
            // 
            this.colUnit.HeaderText = "UNIT";
            this.colUnit.Name = "UNIT";
            this.colUnit.ReadOnly = true;
            // 
            // colStroke
            // 
            this.colStroke.HeaderText = "STROKE";
            this.colStroke.Name = "STROKE";
            // 
            // colBrake
            // 
            this.colBrake.HeaderText = "BRAKE";
            this.colBrake.Name = "BRAKE";
            // 
            // colSln
            // 
            this.colSln.HeaderText = "SOFT LIMIT(-)";
            this.colSln.Name = "SLN";
            // 
            // colSlp
            // 
            this.colSlp.HeaderText = "SOFT LIMIT(+)";
            this.colSlp.Name = "SLP";
            // 
            // colVel
            // 
            this.colVel.HeaderText = "DEFAULT VEL";
            this.colVel.Name = "VEL";
            // 
            // colHomeDir
            // 
            this.colHomeDir.HeaderText = "HOME DIR";
            this.colHomeDir.Name = "HOMEDIR";
            // 
            // actionsPanel
            // 
            this.actionsPanel.BackColor = System.Drawing.Color.WhiteSmoke;
            this.actionsPanel.Controls.Add(this.btnSave);
            this.actionsPanel.Controls.Add(this.btnReload);
            this.actionsPanel.Controls.Add(this.btnReset);
            this.actionsPanel.Controls.Add(this.btnApply);
            this.actionsPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.actionsPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.actionsPanel.Location = new System.Drawing.Point(0, 920);
            this.actionsPanel.Name = "actionsPanel";
            this.actionsPanel.Padding = new System.Windows.Forms.Padding(8);
            this.actionsPanel.Size = new System.Drawing.Size(1416, 60);
            this.actionsPanel.TabIndex = 2;
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(4, 4);
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
            this.btnReload.Location = new System.Drawing.Point(132, 4);
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
            this.btnReset.Location = new System.Drawing.Point(260, 4);
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
            this.btnApply.Location = new System.Drawing.Point(428, 4);
            this.btnApply.Margin = new System.Windows.Forms.Padding(4);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(220, 44);
            this.btnApply.TabIndex = 3;
            this.btnApply.Text = "APPLY (Soft Limit 반영)";
            this.btnApply.Click += new System.EventHandler(this.OnApplyClick);
            // 
            // AxisSetupPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.lblSubHeader);
            this.Controls.Add(this.grid);
            this.Controls.Add(this.actionsPanel);
            this.Name = "AxisSetupPage";
            this.Size = new System.Drawing.Size(1416, 980);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.actionsPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}