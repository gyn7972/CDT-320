namespace QMC.CDT_320.Ui.Pages.Settings
{
    partial class IoControlPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel root;
        private System.Windows.Forms.TableLayoutPanel headerLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TableLayoutPanel bodyLayout;
        private System.Windows.Forms.GroupBox grpDi;
        private System.Windows.Forms.GroupBox grpDo;
        private System.Windows.Forms.GroupBox grpAction;
        private System.Windows.Forms.TableLayoutPanel actionRow;
        private System.Windows.Forms.DataGridView diGrid;
        private System.Windows.Forms.DataGridView doGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn diNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn diAddress;
        private System.Windows.Forms.DataGridViewTextBoxColumn diName;
        private System.Windows.Forms.DataGridViewTextBoxColumn diModule;
        private System.Windows.Forms.DataGridViewTextBoxColumn diBit;
        private System.Windows.Forms.DataGridViewTextBoxColumn diState;
        private System.Windows.Forms.DataGridViewTextBoxColumn doNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn doAddress;
        private System.Windows.Forms.DataGridViewTextBoxColumn doName;
        private System.Windows.Forms.DataGridViewTextBoxColumn doModule;
        private System.Windows.Forms.DataGridViewTextBoxColumn doBit;
        private System.Windows.Forms.DataGridViewTextBoxColumn doState;
        private QMC.CDT_320.Ui.Controls.ActionButton btnRefresh;
        private QMC.CDT_320.Ui.Controls.ActionButton btnDoOn;
        private QMC.CDT_320.Ui.Controls.ActionButton btnDoOff;
        private QMC.CDT_320.Ui.Controls.ActionButton btnPulse;

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
            this.root = new System.Windows.Forms.TableLayoutPanel();
            this.headerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.bodyLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpDi = new System.Windows.Forms.GroupBox();
            this.diGrid = new System.Windows.Forms.DataGridView();
            this.diNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.diAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.diName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.diModule = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.diBit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.diState = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.grpDo = new System.Windows.Forms.GroupBox();
            this.doGrid = new System.Windows.Forms.DataGridView();
            this.doNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.doAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.doName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.doModule = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.doBit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.doState = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.grpAction = new System.Windows.Forms.GroupBox();
            this.actionRow = new System.Windows.Forms.TableLayoutPanel();
            this.btnRefresh = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnDoOn = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnDoOff = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnPulse = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.root.SuspendLayout();
            this.headerLayout.SuspendLayout();
            this.bodyLayout.SuspendLayout();
            this.grpDi.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.diGrid)).BeginInit();
            this.grpDo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.doGrid)).BeginInit();
            this.grpAction.SuspendLayout();
            this.actionRow.SuspendLayout();
            this.SuspendLayout();
            //
            // root
            //
            this.root.ColumnCount = 1;
            this.root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.Controls.Add(this.headerLayout, 0, 0);
            this.root.Controls.Add(this.bodyLayout, 0, 1);
            this.root.Controls.Add(this.grpAction, 0, 3);
            this.root.Dock = System.Windows.Forms.DockStyle.Fill;
            this.root.Location = new System.Drawing.Point(0, 0);
            this.root.Name = "root";
            this.root.Padding = new System.Windows.Forms.Padding(0);
            this.root.RowCount = 4;
            this.root.SetRowSpan(this.bodyLayout, 2);
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.root.Size = new System.Drawing.Size(1678, 900);
            this.root.TabIndex = 0;
            //
            // headerLayout
            //
            this.headerLayout.BackColor = UiTheme.StatusBarBg;
            this.headerLayout.ColumnCount = 2;
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headerLayout.Controls.Add(this.lblHeader, 0, 0);
            this.headerLayout.Controls.Add(this.lblStatus, 1, 0);
            this.headerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerLayout.Location = new System.Drawing.Point(0, 0);
            this.headerLayout.Margin = new System.Windows.Forms.Padding(0);
            this.headerLayout.Name = "headerLayout";
            this.headerLayout.RowCount = 1;
            this.headerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headerLayout.Size = new System.Drawing.Size(1678, 30);
            this.headerLayout.TabIndex = 0;
            //
            // lblHeader
            //
            this.lblHeader.BackColor = UiTheme.StatusBarBg;
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = UiTheme.StatusBarFg;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(150, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "I/O CONTROL";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblStatus
            //
            this.lblStatus.BackColor = UiTheme.StatusBarBg;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblStatus.ForeColor = System.Drawing.Color.White;
            this.lblStatus.Location = new System.Drawing.Point(150, 0);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(0);
            this.lblStatus.Size = new System.Drawing.Size(1528, 30);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "Live hardware I/O. DO commands are written directly to the mapped Ajin output.";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // bodyLayout
            //
            this.bodyLayout.ColumnCount = 2;
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.bodyLayout.Controls.Add(this.grpDi, 0, 0);
            this.bodyLayout.Controls.Add(this.grpDo, 1, 0);
            this.bodyLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bodyLayout.Location = new System.Drawing.Point(0, 30);
            this.bodyLayout.Margin = new System.Windows.Forms.Padding(0);
            this.bodyLayout.Name = "bodyLayout";
            this.bodyLayout.RowCount = 1;
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bodyLayout.Size = new System.Drawing.Size(1678, 783);
            this.bodyLayout.TabIndex = 1;
            //
            // grpDi
            //
            this.grpDi.BackColor = System.Drawing.Color.White;
            this.grpDi.Controls.Add(this.diGrid);
            this.grpDi.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpDi.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpDi.ForeColor = System.Drawing.Color.Black;
            this.grpDi.Location = new System.Drawing.Point(0, 0);
            this.grpDi.Margin = new System.Windows.Forms.Padding(0);
            this.grpDi.Name = "grpDi";
            this.grpDi.Padding = new System.Windows.Forms.Padding(1, 9, 1, 1);
            this.grpDi.Size = new System.Drawing.Size(839, 783);
            this.grpDi.TabIndex = 0;
            this.grpDi.TabStop = false;
            this.grpDi.Text = "DIGITAL INPUT";
            //
            // diGrid
            //
            this.diGrid.AllowUserToAddRows = false;
            this.diGrid.AllowUserToDeleteRows = false;
            this.diGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.diGrid.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("맑은 고딕", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.diGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.diGrid.ColumnHeadersHeight = 29;
            this.diGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.diNo,
            this.diAddress,
            this.diName,
            this.diModule,
            this.diBit,
            this.diState});
            this.diGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.diGrid.EnableHeadersVisualStyles = false;
            this.diGrid.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.diGrid.Location = new System.Drawing.Point(1, 22);
            this.diGrid.MultiSelect = false;
            this.diGrid.Name = "diGrid";
            this.diGrid.ReadOnly = true;
            this.diGrid.RowHeadersVisible = false;
            this.diGrid.RowHeadersWidth = 51;
            this.diGrid.RowTemplate.Height = 26;
            this.diGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.diGrid.Size = new System.Drawing.Size(837, 760);
            this.diGrid.TabIndex = 4;
            //
            // diNo
            //
            this.diNo.FillWeight = 45F;
            this.diNo.HeaderText = "NO";
            this.diNo.MinimumWidth = 6;
            this.diNo.Name = "diNo";
            this.diNo.ReadOnly = true;
            //
            // diAddress
            //
            this.diAddress.FillWeight = 70F;
            this.diAddress.HeaderText = "ADDR";
            this.diAddress.MinimumWidth = 6;
            this.diAddress.Name = "diAddress";
            this.diAddress.ReadOnly = true;
            //
            // diName
            //
            this.diName.HeaderText = "NAME";
            this.diName.MinimumWidth = 6;
            this.diName.Name = "diName";
            this.diName.ReadOnly = true;
            //
            // diModule
            //
            this.diModule.HeaderText = "MODULE";
            this.diModule.MinimumWidth = 6;
            this.diModule.Name = "diModule";
            this.diModule.ReadOnly = true;
            //
            // diBit
            //
            this.diBit.HeaderText = "BIT";
            this.diBit.MinimumWidth = 6;
            this.diBit.Name = "diBit";
            this.diBit.ReadOnly = true;
            //
            // diState
            //
            this.diState.HeaderText = "STATE";
            this.diState.MinimumWidth = 6;
            this.diState.Name = "diState";
            this.diState.ReadOnly = true;
            //
            // grpDo
            //
            this.grpDo.BackColor = System.Drawing.Color.White;
            this.grpDo.Controls.Add(this.doGrid);
            this.grpDo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpDo.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpDo.ForeColor = System.Drawing.Color.Black;
            this.grpDo.Location = new System.Drawing.Point(839, 0);
            this.grpDo.Margin = new System.Windows.Forms.Padding(0);
            this.grpDo.Name = "grpDo";
            this.grpDo.Padding = new System.Windows.Forms.Padding(1, 9, 1, 1);
            this.grpDo.Size = new System.Drawing.Size(839, 783);
            this.grpDo.TabIndex = 1;
            this.grpDo.TabStop = false;
            this.grpDo.Text = "DIGITAL OUTPUT";
            //
            // doGrid
            //
            this.doGrid.AllowUserToAddRows = false;
            this.doGrid.AllowUserToDeleteRows = false;
            this.doGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.doGrid.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("맑은 고딕", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.doGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.doGrid.ColumnHeadersHeight = 29;
            this.doGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.doNo,
            this.doAddress,
            this.doName,
            this.doModule,
            this.doBit,
            this.doState});
            this.doGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.doGrid.EnableHeadersVisualStyles = false;
            this.doGrid.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.doGrid.Location = new System.Drawing.Point(1, 22);
            this.doGrid.MultiSelect = false;
            this.doGrid.Name = "doGrid";
            this.doGrid.ReadOnly = true;
            this.doGrid.RowHeadersVisible = false;
            this.doGrid.RowHeadersWidth = 51;
            this.doGrid.RowTemplate.Height = 26;
            this.doGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.doGrid.Size = new System.Drawing.Size(837, 760);
            this.doGrid.TabIndex = 5;
            this.doGrid.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DoGrid_CellClick);
            //
            // doNo
            //
            this.doNo.FillWeight = 45F;
            this.doNo.HeaderText = "NO";
            this.doNo.MinimumWidth = 6;
            this.doNo.Name = "doNo";
            this.doNo.ReadOnly = true;
            //
            // doAddress
            //
            this.doAddress.FillWeight = 70F;
            this.doAddress.HeaderText = "ADDR";
            this.doAddress.MinimumWidth = 6;
            this.doAddress.Name = "doAddress";
            this.doAddress.ReadOnly = true;
            //
            // doName
            //
            this.doName.HeaderText = "NAME";
            this.doName.MinimumWidth = 6;
            this.doName.Name = "doName";
            this.doName.ReadOnly = true;
            //
            // doModule
            //
            this.doModule.HeaderText = "MODULE";
            this.doModule.MinimumWidth = 6;
            this.doModule.Name = "doModule";
            this.doModule.ReadOnly = true;
            //
            // doBit
            //
            this.doBit.HeaderText = "BIT";
            this.doBit.MinimumWidth = 6;
            this.doBit.Name = "doBit";
            this.doBit.ReadOnly = true;
            //
            // doState
            //
            this.doState.HeaderText = "STATE";
            this.doState.MinimumWidth = 6;
            this.doState.Name = "doState";
            this.doState.ReadOnly = true;
            //
            // grpAction
            //
            this.grpAction.BackColor = System.Drawing.Color.White;
            this.grpAction.Controls.Add(this.actionRow);
            this.grpAction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAction.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpAction.ForeColor = System.Drawing.Color.Black;
            this.grpAction.Location = new System.Drawing.Point(0, 813);
            this.grpAction.Margin = new System.Windows.Forms.Padding(0);
            this.grpAction.Name = "grpAction";
            this.grpAction.Padding = new System.Windows.Forms.Padding(1, 9, 1, 1);
            this.grpAction.Size = new System.Drawing.Size(1678, 87);
            this.grpAction.TabIndex = 2;
            this.grpAction.TabStop = false;
            this.grpAction.Text = "ACTION";
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
            this.actionRow.Controls.Add(this.btnRefresh, 0, 0);
            this.actionRow.Controls.Add(this.btnDoOn, 1, 0);
            this.actionRow.Controls.Add(this.btnDoOff, 2, 0);
            this.actionRow.Controls.Add(this.btnPulse, 3, 0);
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
            // btnRefresh
            //
            this.btnRefresh.BackColor = System.Drawing.Color.Gray;
            this.btnRefresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRefresh.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnRefresh.ForeColor = System.Drawing.Color.White;
            this.btnRefresh.Location = new System.Drawing.Point(4, 4);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(2);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(115, 56);
            this.btnRefresh.TabIndex = 0;
            this.btnRefresh.Text = "REFRESH";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            //
            // btnDoOn
            //
            this.btnDoOn.BackColor = System.Drawing.Color.Gray;
            this.btnDoOn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDoOn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDoOn.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnDoOn.ForeColor = System.Drawing.Color.White;
            this.btnDoOn.Location = new System.Drawing.Point(123, 4);
            this.btnDoOn.Margin = new System.Windows.Forms.Padding(2);
            this.btnDoOn.Name = "btnDoOn";
            this.btnDoOn.Size = new System.Drawing.Size(115, 56);
            this.btnDoOn.TabIndex = 1;
            this.btnDoOn.Text = "DO ON";
            this.btnDoOn.Click += new System.EventHandler(this.btnDoOn_Click);
            //
            // btnDoOff
            //
            this.btnDoOff.BackColor = System.Drawing.Color.Gray;
            this.btnDoOff.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDoOff.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDoOff.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnDoOff.ForeColor = System.Drawing.Color.White;
            this.btnDoOff.Location = new System.Drawing.Point(242, 4);
            this.btnDoOff.Margin = new System.Windows.Forms.Padding(2);
            this.btnDoOff.Name = "btnDoOff";
            this.btnDoOff.Size = new System.Drawing.Size(115, 56);
            this.btnDoOff.TabIndex = 2;
            this.btnDoOff.Text = "DO OFF";
            this.btnDoOff.Click += new System.EventHandler(this.btnDoOff_Click);
            //
            // btnPulse
            //
            this.btnPulse.BackColor = System.Drawing.Color.Gray;
            this.btnPulse.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPulse.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPulse.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnPulse.ForeColor = System.Drawing.Color.White;
            this.btnPulse.Location = new System.Drawing.Point(361, 4);
            this.btnPulse.Margin = new System.Windows.Forms.Padding(2);
            this.btnPulse.Name = "btnPulse";
            this.btnPulse.Size = new System.Drawing.Size(115, 56);
            this.btnPulse.TabIndex = 3;
            this.btnPulse.Text = "PULSE 200ms";
            this.btnPulse.Click += new System.EventHandler(this.btnPulse_Click);
            //
            // IoControlPage
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.root);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.Name = "IoControlPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.root.ResumeLayout(false);
            this.headerLayout.ResumeLayout(false);
            this.bodyLayout.ResumeLayout(false);
            this.grpDi.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.diGrid)).EndInit();
            this.grpDo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.doGrid)).EndInit();
            this.grpAction.ResumeLayout(false);
            this.actionRow.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
