namespace QMC.CDT_320.Ui.Pages.Settings
{
    partial class IoListPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Label lblSubHeader;
        private System.Windows.Forms.DataGridView _grid;
        private System.Windows.Forms.TableLayoutPanel cylinderTestPanel;
        private System.Windows.Forms.Label lblCylinderTestTitle;
        private System.Windows.Forms.Label lblSelectedCylinder;
        private System.Windows.Forms.Label lblFwdTimeout;
        private System.Windows.Forms.NumericUpDown nudFwdTimeout;
        private System.Windows.Forms.Label lblBwdTimeout;
        private System.Windows.Forms.NumericUpDown nudBwdTimeout;
        private System.Windows.Forms.Label lblFwdLabel;
        private System.Windows.Forms.TextBox txtFwdLabel;
        private System.Windows.Forms.Label lblBwdLabel;
        private System.Windows.Forms.TextBox txtBwdLabel;
        private System.Windows.Forms.CheckBox chkSingleSolenoid;
        private System.Windows.Forms.CheckBox chkUseFwdSensor;
        private System.Windows.Forms.CheckBox chkUseBwdSensor;
        private QMC.CDT_320.Ui.Controls.ActionButton btnCylinderApply;
        private QMC.CDT_320.Ui.Controls.ActionButton btnCylinderFwd;
        private QMC.CDT_320.Ui.Controls.ActionButton btnCylinderBwd;
        private QMC.CDT_320.Ui.Controls.ActionButton btnCylinderOff;
        private System.Windows.Forms.Label lblCylinderResult;
        private System.Windows.Forms.TableLayoutPanel actionsLayout;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSave;
        private QMC.CDT_320.Ui.Controls.ActionButton btnReload;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAddRow;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblSubHeader = new System.Windows.Forms.Label();
            this._grid = new System.Windows.Forms.DataGridView();
            this.cylinderTestPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblCylinderTestTitle = new System.Windows.Forms.Label();
            this.lblSelectedCylinder = new System.Windows.Forms.Label();
            this.lblFwdTimeout = new System.Windows.Forms.Label();
            this.nudFwdTimeout = new System.Windows.Forms.NumericUpDown();
            this.lblBwdTimeout = new System.Windows.Forms.Label();
            this.nudBwdTimeout = new System.Windows.Forms.NumericUpDown();
            this.chkSingleSolenoid = new System.Windows.Forms.CheckBox();
            this.chkUseFwdSensor = new System.Windows.Forms.CheckBox();
            this.chkUseBwdSensor = new System.Windows.Forms.CheckBox();
            this.lblFwdLabel = new System.Windows.Forms.Label();
            this.txtFwdLabel = new System.Windows.Forms.TextBox();
            this.lblBwdLabel = new System.Windows.Forms.Label();
            this.txtBwdLabel = new System.Windows.Forms.TextBox();
            this.btnCylinderApply = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnCylinderFwd = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnCylinderBwd = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnCylinderOff = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.lblCylinderResult = new System.Windows.Forms.Label();
            this.actionsLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnReload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAddRow = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.rootLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
            this.cylinderTestPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudFwdTimeout)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBwdTimeout)).BeginInit();
            this.actionsLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.lblSubHeader, 0, 1);
            this.rootLayout.Controls.Add(this._grid, 0, 2);
            this.rootLayout.Controls.Add(this.cylinderTestPanel, 0, 3);
            this.rootLayout.Controls.Add(this.actionsLayout, 0, 4);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 5;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 190F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.Location = new System.Drawing.Point(8, 8);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1662, 28);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "IO LIST";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSubHeader
            // 
            this.lblSubHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSubHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblSubHeader.Location = new System.Drawing.Point(8, 40);
            this.lblSubHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblSubHeader.Name = "lblSubHeader";
            this.lblSubHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblSubHeader.Size = new System.Drawing.Size(1662, 26);
            this.lblSubHeader.TabIndex = 1;
            this.lblSubHeader.Text = "IO LIST";
            this.lblSubHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _grid
            // 
            this._grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._grid.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this._grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this._grid.ColumnHeadersHeight = 29;
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.EnableHeadersVisualStyles = false;
            this._grid.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._grid.Location = new System.Drawing.Point(8, 70);
            this._grid.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this._grid.MultiSelect = false;
            this._grid.Name = "_grid";
            this._grid.RowHeadersVisible = false;
            this._grid.RowHeadersWidth = 51;
            this._grid.RowTemplate.Height = 26;
            this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._grid.Size = new System.Drawing.Size(1662, 564);
            this._grid.TabIndex = 2;
            // 
            // cylinderTestPanel
            // 
            this.cylinderTestPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(240)))), ((int)(((byte)(242)))));
            this.cylinderTestPanel.ColumnCount = 8;
            this.cylinderTestPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.cylinderTestPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 140F));
            this.cylinderTestPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 140F));
            this.cylinderTestPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.cylinderTestPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.cylinderTestPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.cylinderTestPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.cylinderTestPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cylinderTestPanel.Controls.Add(this.lblCylinderTestTitle, 0, 0);
            this.cylinderTestPanel.Controls.Add(this.lblSelectedCylinder, 1, 0);
            this.cylinderTestPanel.Controls.Add(this.lblFwdTimeout, 0, 1);
            this.cylinderTestPanel.Controls.Add(this.nudFwdTimeout, 1, 1);
            this.cylinderTestPanel.Controls.Add(this.lblBwdTimeout, 2, 1);
            this.cylinderTestPanel.Controls.Add(this.nudBwdTimeout, 3, 1);
            this.cylinderTestPanel.Controls.Add(this.chkSingleSolenoid, 4, 1);
            this.cylinderTestPanel.Controls.Add(this.chkUseFwdSensor, 5, 1);
            this.cylinderTestPanel.Controls.Add(this.chkUseBwdSensor, 6, 1);
            this.cylinderTestPanel.Controls.Add(this.lblFwdLabel, 0, 2);
            this.cylinderTestPanel.Controls.Add(this.txtFwdLabel, 1, 2);
            this.cylinderTestPanel.Controls.Add(this.lblBwdLabel, 2, 2);
            this.cylinderTestPanel.Controls.Add(this.txtBwdLabel, 3, 2);
            this.cylinderTestPanel.Controls.Add(this.btnCylinderApply, 0, 3);
            this.cylinderTestPanel.Controls.Add(this.btnCylinderFwd, 1, 3);
            this.cylinderTestPanel.Controls.Add(this.btnCylinderBwd, 2, 3);
            this.cylinderTestPanel.Controls.Add(this.btnCylinderOff, 3, 3);
            this.cylinderTestPanel.Controls.Add(this.lblCylinderResult, 4, 3);
            this.cylinderTestPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cylinderTestPanel.Location = new System.Drawing.Point(8, 642);
            this.cylinderTestPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.cylinderTestPanel.Name = "cylinderTestPanel";
            this.cylinderTestPanel.Padding = new System.Windows.Forms.Padding(8);
            this.cylinderTestPanel.RowCount = 4;
            this.cylinderTestPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.cylinderTestPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.cylinderTestPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.cylinderTestPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cylinderTestPanel.Size = new System.Drawing.Size(1662, 182);
            this.cylinderTestPanel.TabIndex = 3;
            // 
            // lblCylinderTestTitle
            // 
            this.lblCylinderTestTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCylinderTestTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblCylinderTestTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(34)))), ((int)(((byte)(40)))));
            this.lblCylinderTestTitle.Location = new System.Drawing.Point(11, 8);
            this.lblCylinderTestTitle.Name = "lblCylinderTestTitle";
            this.lblCylinderTestTitle.Size = new System.Drawing.Size(174, 36);
            this.lblCylinderTestTitle.TabIndex = 0;
            this.lblCylinderTestTitle.Text = "CYLINDER TEST";
            this.lblCylinderTestTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSelectedCylinder
            // 
            this.cylinderTestPanel.SetColumnSpan(this.lblSelectedCylinder, 7);
            this.lblSelectedCylinder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelectedCylinder.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblSelectedCylinder.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(95)))), ((int)(((byte)(150)))));
            this.lblSelectedCylinder.Location = new System.Drawing.Point(191, 8);
            this.lblSelectedCylinder.Name = "lblSelectedCylinder";
            this.lblSelectedCylinder.Size = new System.Drawing.Size(1460, 36);
            this.lblSelectedCylinder.TabIndex = 1;
            this.lblSelectedCylinder.Text = "-";
            this.lblSelectedCylinder.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblFwdTimeout
            // 
            this.lblFwdTimeout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFwdTimeout.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblFwdTimeout.Location = new System.Drawing.Point(11, 44);
            this.lblFwdTimeout.Name = "lblFwdTimeout";
            this.lblFwdTimeout.Size = new System.Drawing.Size(174, 42);
            this.lblFwdTimeout.TabIndex = 2;
            this.lblFwdTimeout.Text = "FWD WAIT(ms)";
            this.lblFwdTimeout.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // nudFwdTimeout
            // 
            this.nudFwdTimeout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudFwdTimeout.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.nudFwdTimeout.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudFwdTimeout.Location = new System.Drawing.Point(191, 47);
            this.nudFwdTimeout.Maximum = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            this.nudFwdTimeout.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudFwdTimeout.Name = "nudFwdTimeout";
            this.nudFwdTimeout.Size = new System.Drawing.Size(134, 30);
            this.nudFwdTimeout.TabIndex = 3;
            this.nudFwdTimeout.Value = new decimal(new int[] {
            3000,
            0,
            0,
            0});
            // 
            // lblBwdTimeout
            // 
            this.lblBwdTimeout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBwdTimeout.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblBwdTimeout.Location = new System.Drawing.Point(331, 44);
            this.lblBwdTimeout.Name = "lblBwdTimeout";
            this.lblBwdTimeout.Size = new System.Drawing.Size(134, 42);
            this.lblBwdTimeout.TabIndex = 4;
            this.lblBwdTimeout.Text = "BWD WAIT(ms)";
            this.lblBwdTimeout.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // nudBwdTimeout
            // 
            this.nudBwdTimeout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudBwdTimeout.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.nudBwdTimeout.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudBwdTimeout.Location = new System.Drawing.Point(471, 47);
            this.nudBwdTimeout.Maximum = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            this.nudBwdTimeout.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudBwdTimeout.Name = "nudBwdTimeout";
            this.nudBwdTimeout.Size = new System.Drawing.Size(144, 30);
            this.nudBwdTimeout.TabIndex = 5;
            this.nudBwdTimeout.Value = new decimal(new int[] {
            3000,
            0,
            0,
            0});
            // 
            // chkSingleSolenoid
            // 
            this.chkSingleSolenoid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkSingleSolenoid.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.chkSingleSolenoid.Location = new System.Drawing.Point(621, 47);
            this.chkSingleSolenoid.Name = "chkSingleSolenoid";
            this.chkSingleSolenoid.Size = new System.Drawing.Size(124, 36);
            this.chkSingleSolenoid.TabIndex = 6;
            this.chkSingleSolenoid.Text = "SINGLE";
            // 
            // chkUseFwdSensor
            // 
            this.chkUseFwdSensor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkUseFwdSensor.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.chkUseFwdSensor.Location = new System.Drawing.Point(751, 47);
            this.chkUseFwdSensor.Name = "chkUseFwdSensor";
            this.chkUseFwdSensor.Size = new System.Drawing.Size(124, 36);
            this.chkUseFwdSensor.TabIndex = 7;
            this.chkUseFwdSensor.Text = "FWD DI";
            // 
            // chkUseBwdSensor
            // 
            this.chkUseBwdSensor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkUseBwdSensor.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.chkUseBwdSensor.Location = new System.Drawing.Point(881, 47);
            this.chkUseBwdSensor.Name = "chkUseBwdSensor";
            this.chkUseBwdSensor.Size = new System.Drawing.Size(124, 36);
            this.chkUseBwdSensor.TabIndex = 8;
            this.chkUseBwdSensor.Text = "BWD DI";
            // 
            // lblFwdLabel
            // 
            this.lblFwdLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFwdLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblFwdLabel.Location = new System.Drawing.Point(11, 86);
            this.lblFwdLabel.Name = "lblFwdLabel";
            this.lblFwdLabel.Size = new System.Drawing.Size(174, 42);
            this.lblFwdLabel.TabIndex = 14;
            this.lblFwdLabel.Text = "FWD TEXT";
            this.lblFwdLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFwdLabel
            // 
            this.txtFwdLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtFwdLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.txtFwdLabel.Location = new System.Drawing.Point(192, 93);
            this.txtFwdLabel.Margin = new System.Windows.Forms.Padding(4, 7, 4, 7);
            this.txtFwdLabel.Name = "txtFwdLabel";
            this.txtFwdLabel.Size = new System.Drawing.Size(132, 30);
            this.txtFwdLabel.TabIndex = 15;
            this.txtFwdLabel.Text = "FWD";
            // 
            // lblBwdLabel
            // 
            this.lblBwdLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBwdLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblBwdLabel.Location = new System.Drawing.Point(331, 86);
            this.lblBwdLabel.Name = "lblBwdLabel";
            this.lblBwdLabel.Size = new System.Drawing.Size(134, 42);
            this.lblBwdLabel.TabIndex = 16;
            this.lblBwdLabel.Text = "BWD TEXT";
            this.lblBwdLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtBwdLabel
            // 
            this.txtBwdLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtBwdLabel.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.txtBwdLabel.Location = new System.Drawing.Point(472, 93);
            this.txtBwdLabel.Margin = new System.Windows.Forms.Padding(4, 7, 4, 7);
            this.txtBwdLabel.Name = "txtBwdLabel";
            this.txtBwdLabel.Size = new System.Drawing.Size(142, 30);
            this.txtBwdLabel.TabIndex = 17;
            this.txtBwdLabel.Text = "BWD";
            // 
            // btnCylinderApply
            // 
            this.btnCylinderApply.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnCylinderApply.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCylinderApply.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCylinderApply.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnCylinderApply.ForeColor = System.Drawing.Color.White;
            this.btnCylinderApply.Location = new System.Drawing.Point(12, 136);
            this.btnCylinderApply.Margin = new System.Windows.Forms.Padding(4, 8, 4, 4);
            this.btnCylinderApply.Name = "btnCylinderApply";
            this.btnCylinderApply.Size = new System.Drawing.Size(172, 34);
            this.btnCylinderApply.TabIndex = 18;
            this.btnCylinderApply.Text = "APPLY";
            // 
            // btnCylinderFwd
            // 
            this.btnCylinderFwd.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnCylinderFwd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCylinderFwd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCylinderFwd.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnCylinderFwd.ForeColor = System.Drawing.Color.White;
            this.btnCylinderFwd.Location = new System.Drawing.Point(192, 136);
            this.btnCylinderFwd.Margin = new System.Windows.Forms.Padding(4, 8, 4, 4);
            this.btnCylinderFwd.Name = "btnCylinderFwd";
            this.btnCylinderFwd.Size = new System.Drawing.Size(132, 34);
            this.btnCylinderFwd.TabIndex = 19;
            this.btnCylinderFwd.Text = "FWD";
            // 
            // btnCylinderBwd
            // 
            this.btnCylinderBwd.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnCylinderBwd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCylinderBwd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCylinderBwd.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnCylinderBwd.ForeColor = System.Drawing.Color.White;
            this.btnCylinderBwd.Location = new System.Drawing.Point(332, 136);
            this.btnCylinderBwd.Margin = new System.Windows.Forms.Padding(4, 8, 4, 4);
            this.btnCylinderBwd.Name = "btnCylinderBwd";
            this.btnCylinderBwd.Size = new System.Drawing.Size(132, 34);
            this.btnCylinderBwd.TabIndex = 20;
            this.btnCylinderBwd.Text = "BWD";
            // 
            // btnCylinderOff
            // 
            this.btnCylinderOff.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnCylinderOff.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCylinderOff.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCylinderOff.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnCylinderOff.ForeColor = System.Drawing.Color.White;
            this.btnCylinderOff.Location = new System.Drawing.Point(472, 136);
            this.btnCylinderOff.Margin = new System.Windows.Forms.Padding(4, 8, 4, 4);
            this.btnCylinderOff.Name = "btnCylinderOff";
            this.btnCylinderOff.Size = new System.Drawing.Size(142, 34);
            this.btnCylinderOff.TabIndex = 21;
            this.btnCylinderOff.Text = "OFF";
            // 
            // lblCylinderResult
            // 
            this.cylinderTestPanel.SetColumnSpan(this.lblCylinderResult, 4);
            this.lblCylinderResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCylinderResult.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblCylinderResult.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.lblCylinderResult.Location = new System.Drawing.Point(621, 128);
            this.lblCylinderResult.Name = "lblCylinderResult";
            this.lblCylinderResult.Size = new System.Drawing.Size(1030, 46);
            this.lblCylinderResult.TabIndex = 22;
            this.lblCylinderResult.Text = "READY";
            this.lblCylinderResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // actionsLayout
            // 
            this.actionsLayout.ColumnCount = 4;
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.actionsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionsLayout.Controls.Add(this.btnSave, 0, 0);
            this.actionsLayout.Controls.Add(this.btnReload, 1, 0);
            this.actionsLayout.Controls.Add(this.btnAddRow, 2, 0);
            this.actionsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionsLayout.Location = new System.Drawing.Point(8, 832);
            this.actionsLayout.Margin = new System.Windows.Forms.Padding(0);
            this.actionsLayout.Name = "actionsLayout";
            this.actionsLayout.RowCount = 1;
            this.actionsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionsLayout.Size = new System.Drawing.Size(1662, 60);
            this.actionsLayout.TabIndex = 4;
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(4, 8);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(122, 44);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "SAVE";
            // 
            // btnReload
            // 
            this.btnReload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnReload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReload.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnReload.ForeColor = System.Drawing.Color.White;
            this.btnReload.Location = new System.Drawing.Point(134, 8);
            this.btnReload.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(122, 44);
            this.btnReload.TabIndex = 1;
            this.btnReload.Text = "RELOAD";
            // 
            // btnAddRow
            // 
            this.btnAddRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnAddRow.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAddRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAddRow.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAddRow.ForeColor = System.Drawing.Color.White;
            this.btnAddRow.Location = new System.Drawing.Point(264, 8);
            this.btnAddRow.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnAddRow.Name = "btnAddRow";
            this.btnAddRow.Size = new System.Drawing.Size(122, 44);
            this.btnAddRow.TabIndex = 2;
            this.btnAddRow.Text = "ADD ROW";
            // 
            // IoListPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "IoListPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
            this.cylinderTestPanel.ResumeLayout(false);
            this.cylinderTestPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudFwdTimeout)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBwdTimeout)).EndInit();
            this.actionsLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
