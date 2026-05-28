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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.lblSubHeader = new System.Windows.Forms.Label();
            this.grid = new System.Windows.Forms.DataGridView();
            this.NO = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MODULE = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NAME = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BOARD = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CH = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UNIT = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.STROKE = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BRAKE = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SLN = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SLP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.VEL = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HOMEDIR = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
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
            this.BRAKE,
            this.SLN,
            this.SLP,
            this.VEL,
            this.HOMEDIR});
            this.grid.EnableHeadersVisualStyles = false;
            this.grid.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.grid.Location = new System.Drawing.Point(8, 66);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.RowHeadersVisible = false;
            this.grid.RowHeadersWidth = 51;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(1400, 800);
            this.grid.TabIndex = 1;
            this.grid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnCellEdit);
            // 
            // NO
            // 
            this.NO.HeaderText = "NO";
            this.NO.MinimumWidth = 6;
            this.NO.Name = "NO";
            this.NO.ReadOnly = true;
            // 
            // MODULE
            // 
            this.MODULE.HeaderText = "MODULE";
            this.MODULE.MinimumWidth = 6;
            this.MODULE.Name = "MODULE";
            this.MODULE.ReadOnly = true;
            // 
            // NAME
            // 
            this.NAME.HeaderText = "AXIS NAME";
            this.NAME.MinimumWidth = 6;
            this.NAME.Name = "NAME";
            this.NAME.ReadOnly = true;
            // 
            // BOARD
            // 
            this.BOARD.HeaderText = "BOARD#";
            this.BOARD.MinimumWidth = 6;
            this.BOARD.Name = "BOARD";
            // 
            // CH
            // 
            this.CH.HeaderText = "CH (slot)";
            this.CH.MinimumWidth = 6;
            this.CH.Name = "CH";
            // 
            // UNIT
            // 
            this.UNIT.HeaderText = "UNIT";
            this.UNIT.MinimumWidth = 6;
            this.UNIT.Name = "UNIT";
            this.UNIT.ReadOnly = true;
            // 
            // STROKE
            // 
            this.STROKE.HeaderText = "STROKE";
            this.STROKE.MinimumWidth = 6;
            this.STROKE.Name = "STROKE";
            // 
            // BRAKE
            // 
            this.BRAKE.HeaderText = "BRAKE";
            this.BRAKE.MinimumWidth = 6;
            this.BRAKE.Name = "BRAKE";
            // 
            // SLN
            // 
            this.SLN.HeaderText = "SOFT LIMIT(-)";
            this.SLN.MinimumWidth = 6;
            this.SLN.Name = "SLN";
            // 
            // SLP
            // 
            this.SLP.HeaderText = "SOFT LIMIT(+)";
            this.SLP.MinimumWidth = 6;
            this.SLP.Name = "SLP";
            // 
            // VEL
            // 
            this.VEL.HeaderText = "DEFAULT VEL";
            this.VEL.MinimumWidth = 6;
            this.VEL.Name = "VEL";
            // 
            // HOMEDIR
            // 
            this.HOMEDIR.HeaderText = "HOME DIR";
            this.HOMEDIR.MinimumWidth = 6;
            this.HOMEDIR.Name = "HOMEDIR";
            // 
            // actionsPanel
            // 
            this.actionsPanel.BackColor = System.Drawing.Color.WhiteSmoke;
            this.actionsPanel.Controls.Add(this.btnSave);
            this.actionsPanel.Controls.Add(this.btnReload);
            this.actionsPanel.Controls.Add(this.btnReset);
            this.actionsPanel.Controls.Add(this.btnApply);
            this.actionsPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
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

        private System.Windows.Forms.DataGridViewTextBoxColumn NO;
        private System.Windows.Forms.DataGridViewTextBoxColumn MODULE;
        private System.Windows.Forms.DataGridViewTextBoxColumn NAME;
        private System.Windows.Forms.DataGridViewTextBoxColumn BOARD;
        private System.Windows.Forms.DataGridViewTextBoxColumn CH;
        private System.Windows.Forms.DataGridViewTextBoxColumn UNIT;
        private System.Windows.Forms.DataGridViewTextBoxColumn STROKE;
        private System.Windows.Forms.DataGridViewTextBoxColumn BRAKE;
        private System.Windows.Forms.DataGridViewTextBoxColumn SLN;
        private System.Windows.Forms.DataGridViewTextBoxColumn SLP;
        private System.Windows.Forms.DataGridViewTextBoxColumn VEL;
        private System.Windows.Forms.DataGridViewTextBoxColumn HOMEDIR;
    }
}