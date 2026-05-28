namespace QMC.CDT_320.Ui.Pages.Settings
{
    partial class PositionTeachingPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel contentLayout;
        private System.Windows.Forms.DataGridView _grid;
        private System.Windows.Forms.TableLayoutPanel jogLayout;
        private System.Windows.Forms.Label lblJogHeader;
        private System.Windows.Forms.Label _jogAxisLabel;
        private System.Windows.Forms.Label _jogPosLabel;
        private System.Windows.Forms.Label lblSpeed;
        private System.Windows.Forms.TextBox _jogSpeedBox;
        private System.Windows.Forms.Label lblSpeedUnit;
        private System.Windows.Forms.Label lblStep;
        private System.Windows.Forms.TextBox _jogStepBox;
        private System.Windows.Forms.Button btnStepMul10;
        private System.Windows.Forms.Button btnStepDiv10;
        private System.Windows.Forms.TableLayoutPanel stepPresetLayout;
        private System.Windows.Forms.Button btnStep5;
        private System.Windows.Forms.Button btnStep1;
        private System.Windows.Forms.Button btnStep01;
        private System.Windows.Forms.Button btnStep001;
        private System.Windows.Forms.Button btnStep0001;
        private System.Windows.Forms.Panel _jogButtonArea;
        private System.Windows.Forms.Label lblJogHint;
        private System.Windows.Forms.TableLayoutPanel actionsPanel;
        private QMC.CDT_320.Ui.Controls.ActionButton btnTeach;
        private QMC.CDT_320.Ui.Controls.ActionButton btnGoto;
        private QMC.CDT_320.Ui.Controls.ActionButton btnApply;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSave;
        private QMC.CDT_320.Ui.Controls.ActionButton btnReload;
        private QMC.CDT_320.Ui.Controls.ActionButton btnReset;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_jogPosTimer != null)
                {
                    _jogPosTimer.Stop();
                    _jogPosTimer.Dispose();
                    _jogPosTimer = null;
                }

                if (components != null)
                    components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this._grid = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.jogLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblJogHeader = new System.Windows.Forms.Label();
            this._jogAxisLabel = new System.Windows.Forms.Label();
            this._jogPosLabel = new System.Windows.Forms.Label();
            this.lblSpeed = new System.Windows.Forms.Label();
            this._jogSpeedBox = new System.Windows.Forms.TextBox();
            this.lblSpeedUnit = new System.Windows.Forms.Label();
            this.lblStep = new System.Windows.Forms.Label();
            this._jogStepBox = new System.Windows.Forms.TextBox();
            this.btnStepMul10 = new System.Windows.Forms.Button();
            this.btnStepDiv10 = new System.Windows.Forms.Button();
            this.stepPresetLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnStep5 = new System.Windows.Forms.Button();
            this.btnStep1 = new System.Windows.Forms.Button();
            this.btnStep01 = new System.Windows.Forms.Button();
            this.btnStep001 = new System.Windows.Forms.Button();
            this.btnStep0001 = new System.Windows.Forms.Button();
            this._jogButtonArea = new System.Windows.Forms.Panel();
            this.lblJogHint = new System.Windows.Forms.Label();
            this.actionsPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnTeach = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnGoto = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnApply = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnReload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnReset = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.mainLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
            this.jogLayout.SuspendLayout();
            this.stepPresetLayout.SuspendLayout();
            this.actionsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 1;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.lblHeader, 0, 0);
            this.mainLayout.Controls.Add(this.contentLayout, 0, 1);
            this.mainLayout.Controls.Add(this.actionsPanel, 0, 2);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(0, 0);
            this.mainLayout.Margin = new System.Windows.Forms.Padding(0);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.Padding = new System.Windows.Forms.Padding(8);
            this.mainLayout.RowCount = 3;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.mainLayout.Size = new System.Drawing.Size(1416, 940);
            this.mainLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(8, 8);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1400, 26);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "POSITION TEACHING";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.contentLayout.Controls.Add(this._grid, 0, 0);
            this.contentLayout.Controls.Add(this.jogLayout, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(8, 38);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1400, 834);
            this.contentLayout.TabIndex = 1;
            // 
            // _grid
            // 
            this._grid.AllowUserToAddRows = false;
            this._grid.AllowUserToDeleteRows = false;
            this._grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._grid.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            this._grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this._grid.ColumnHeadersHeight = 29;
            this._grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn3,
            this.dataGridViewTextBoxColumn4,
            this.dataGridViewTextBoxColumn5,
            this.dataGridViewTextBoxColumn6,
            this.dataGridViewTextBoxColumn7});
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.EnableHeadersVisualStyles = false;
            this._grid.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._grid.Location = new System.Drawing.Point(0, 0);
            this._grid.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this._grid.MultiSelect = false;
            this._grid.Name = "_grid";
            this._grid.RowHeadersVisible = false;
            this._grid.RowHeadersWidth = 51;
            this._grid.RowTemplate.Height = 24;
            this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._grid.Size = new System.Drawing.Size(1092, 834);
            this._grid.TabIndex = 0;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "MODULE";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.HeaderText = "KEY";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.HeaderText = "NAME";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.HeaderText = "AXIS (#No)";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.HeaderText = "VALUE";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.HeaderText = "UNIT";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.HeaderText = "DESCRIPTION";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            // 
            // jogLayout
            // 
            this.jogLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(242)))), ((int)(((byte)(245)))), ((int)(((byte)(250)))));
            this.jogLayout.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.jogLayout.ColumnCount = 3;
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 82F));
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 74F));
            this.jogLayout.Controls.Add(this.lblJogHeader, 0, 0);
            this.jogLayout.Controls.Add(this._jogAxisLabel, 0, 1);
            this.jogLayout.Controls.Add(this._jogPosLabel, 0, 2);
            this.jogLayout.Controls.Add(this.lblSpeed, 0, 3);
            this.jogLayout.Controls.Add(this._jogSpeedBox, 1, 3);
            this.jogLayout.Controls.Add(this.lblSpeedUnit, 2, 3);
            this.jogLayout.Controls.Add(this.lblStep, 0, 4);
            this.jogLayout.Controls.Add(this._jogStepBox, 1, 4);
            this.jogLayout.Controls.Add(this.btnStepMul10, 0, 5);
            this.jogLayout.Controls.Add(this.btnStepDiv10, 1, 5);
            this.jogLayout.Controls.Add(this.stepPresetLayout, 0, 6);
            this.jogLayout.Controls.Add(this._jogButtonArea, 0, 7);
            this.jogLayout.Controls.Add(this.lblJogHint, 0, 8);
            this.jogLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogLayout.Location = new System.Drawing.Point(1100, 0);
            this.jogLayout.Margin = new System.Windows.Forms.Padding(0);
            this.jogLayout.Name = "jogLayout";
            this.jogLayout.RowCount = 9;
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 270F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.jogLayout.Size = new System.Drawing.Size(300, 834);
            this.jogLayout.TabIndex = 1;
            // 
            // lblJogHeader
            // 
            this.lblJogHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.jogLayout.SetColumnSpan(this.lblJogHeader, 3);
            this.lblJogHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblJogHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblJogHeader.ForeColor = System.Drawing.Color.White;
            this.lblJogHeader.Location = new System.Drawing.Point(1, 1);
            this.lblJogHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblJogHeader.Name = "lblJogHeader";
            this.lblJogHeader.Size = new System.Drawing.Size(298, 28);
            this.lblJogHeader.TabIndex = 0;
            this.lblJogHeader.Text = "AXIS JOG";
            this.lblJogHeader.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _jogAxisLabel
            // 
            this.jogLayout.SetColumnSpan(this._jogAxisLabel, 3);
            this._jogAxisLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._jogAxisLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._jogAxisLabel.Location = new System.Drawing.Point(4, 30);
            this._jogAxisLabel.Name = "_jogAxisLabel";
            this._jogAxisLabel.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._jogAxisLabel.Size = new System.Drawing.Size(292, 28);
            this._jogAxisLabel.TabIndex = 1;
            this._jogAxisLabel.Text = "Axis: (none)";
            this._jogAxisLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _jogPosLabel
            // 
            this.jogLayout.SetColumnSpan(this._jogPosLabel, 3);
            this._jogPosLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._jogPosLabel.Font = new System.Drawing.Font("Consolas", 10F);
            this._jogPosLabel.Location = new System.Drawing.Point(4, 59);
            this._jogPosLabel.Name = "_jogPosLabel";
            this._jogPosLabel.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._jogPosLabel.Size = new System.Drawing.Size(292, 28);
            this._jogPosLabel.TabIndex = 2;
            this._jogPosLabel.Text = "Actual Pos: -";
            this._jogPosLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSpeed
            // 
            this.lblSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSpeed.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSpeed.Location = new System.Drawing.Point(4, 88);
            this.lblSpeed.Name = "lblSpeed";
            this.lblSpeed.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblSpeed.Size = new System.Drawing.Size(76, 30);
            this.lblSpeed.TabIndex = 3;
            this.lblSpeed.Text = "Speed";
            this.lblSpeed.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _jogSpeedBox
            // 
            this._jogSpeedBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._jogSpeedBox.Font = new System.Drawing.Font("Consolas", 10F);
            this._jogSpeedBox.Location = new System.Drawing.Point(86, 92);
            this._jogSpeedBox.Margin = new System.Windows.Forms.Padding(2, 4, 2, 2);
            this._jogSpeedBox.Name = "_jogSpeedBox";
            this._jogSpeedBox.Size = new System.Drawing.Size(136, 27);
            this._jogSpeedBox.TabIndex = 4;
            this._jogSpeedBox.Text = "100";
            this._jogSpeedBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // lblSpeedUnit
            // 
            this.lblSpeedUnit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSpeedUnit.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblSpeedUnit.ForeColor = System.Drawing.Color.DimGray;
            this.lblSpeedUnit.Location = new System.Drawing.Point(228, 88);
            this.lblSpeedUnit.Name = "lblSpeedUnit";
            this.lblSpeedUnit.Size = new System.Drawing.Size(68, 30);
            this.lblSpeedUnit.TabIndex = 5;
            this.lblSpeedUnit.Text = "mm/s";
            this.lblSpeedUnit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStep
            // 
            this.lblStep.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStep.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStep.Location = new System.Drawing.Point(4, 119);
            this.lblStep.Name = "lblStep";
            this.lblStep.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblStep.Size = new System.Drawing.Size(76, 30);
            this.lblStep.TabIndex = 6;
            this.lblStep.Text = "Step";
            this.lblStep.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _jogStepBox
            // 
            this._jogStepBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._jogStepBox.Font = new System.Drawing.Font("Consolas", 10F);
            this._jogStepBox.Location = new System.Drawing.Point(86, 123);
            this._jogStepBox.Margin = new System.Windows.Forms.Padding(2, 4, 2, 2);
            this._jogStepBox.Name = "_jogStepBox";
            this._jogStepBox.Size = new System.Drawing.Size(136, 27);
            this._jogStepBox.TabIndex = 7;
            this._jogStepBox.Text = "1.0";
            this._jogStepBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // btnStepMul10
            // 
            this.btnStepMul10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStepMul10.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStepMul10.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnStepMul10.Location = new System.Drawing.Point(5, 154);
            this.btnStepMul10.Margin = new System.Windows.Forms.Padding(4);
            this.btnStepMul10.Name = "btnStepMul10";
            this.btnStepMul10.Size = new System.Drawing.Size(74, 24);
            this.btnStepMul10.TabIndex = 8;
            this.btnStepMul10.Text = "x10";
            // 
            // btnStepDiv10
            // 
            this.jogLayout.SetColumnSpan(this.btnStepDiv10, 2);
            this.btnStepDiv10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStepDiv10.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStepDiv10.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnStepDiv10.Location = new System.Drawing.Point(88, 154);
            this.btnStepDiv10.Margin = new System.Windows.Forms.Padding(4);
            this.btnStepDiv10.Name = "btnStepDiv10";
            this.btnStepDiv10.Size = new System.Drawing.Size(207, 24);
            this.btnStepDiv10.TabIndex = 9;
            this.btnStepDiv10.Text = "/10";
            // 
            // stepPresetLayout
            // 
            this.stepPresetLayout.ColumnCount = 5;
            this.jogLayout.SetColumnSpan(this.stepPresetLayout, 3);
            this.stepPresetLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.stepPresetLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.stepPresetLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.stepPresetLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.stepPresetLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.stepPresetLayout.Controls.Add(this.btnStep5, 0, 0);
            this.stepPresetLayout.Controls.Add(this.btnStep1, 1, 0);
            this.stepPresetLayout.Controls.Add(this.btnStep01, 2, 0);
            this.stepPresetLayout.Controls.Add(this.btnStep001, 3, 0);
            this.stepPresetLayout.Controls.Add(this.btnStep0001, 4, 0);
            this.stepPresetLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stepPresetLayout.Location = new System.Drawing.Point(5, 187);
            this.stepPresetLayout.Margin = new System.Windows.Forms.Padding(4);
            this.stepPresetLayout.Name = "stepPresetLayout";
            this.stepPresetLayout.RowCount = 1;
            this.stepPresetLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.stepPresetLayout.Size = new System.Drawing.Size(290, 26);
            this.stepPresetLayout.TabIndex = 10;
            // 
            // btnStep5
            // 
            this.btnStep5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStep5.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStep5.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnStep5.Location = new System.Drawing.Point(1, 1);
            this.btnStep5.Margin = new System.Windows.Forms.Padding(1);
            this.btnStep5.Name = "btnStep5";
            this.btnStep5.Size = new System.Drawing.Size(56, 24);
            this.btnStep5.TabIndex = 0;
            this.btnStep5.Text = "5";
            // 
            // btnStep1
            // 
            this.btnStep1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStep1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStep1.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnStep1.Location = new System.Drawing.Point(59, 1);
            this.btnStep1.Margin = new System.Windows.Forms.Padding(1);
            this.btnStep1.Name = "btnStep1";
            this.btnStep1.Size = new System.Drawing.Size(56, 24);
            this.btnStep1.TabIndex = 1;
            this.btnStep1.Text = "1";
            // 
            // btnStep01
            // 
            this.btnStep01.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStep01.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStep01.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnStep01.Location = new System.Drawing.Point(117, 1);
            this.btnStep01.Margin = new System.Windows.Forms.Padding(1);
            this.btnStep01.Name = "btnStep01";
            this.btnStep01.Size = new System.Drawing.Size(56, 24);
            this.btnStep01.TabIndex = 2;
            this.btnStep01.Text = "0.1";
            // 
            // btnStep001
            // 
            this.btnStep001.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStep001.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStep001.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnStep001.Location = new System.Drawing.Point(175, 1);
            this.btnStep001.Margin = new System.Windows.Forms.Padding(1);
            this.btnStep001.Name = "btnStep001";
            this.btnStep001.Size = new System.Drawing.Size(56, 24);
            this.btnStep001.TabIndex = 3;
            this.btnStep001.Text = "0.01";
            // 
            // btnStep0001
            // 
            this.btnStep0001.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStep0001.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStep0001.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnStep0001.Location = new System.Drawing.Point(233, 1);
            this.btnStep0001.Margin = new System.Windows.Forms.Padding(1);
            this.btnStep0001.Name = "btnStep0001";
            this.btnStep0001.Size = new System.Drawing.Size(56, 24);
            this.btnStep0001.TabIndex = 4;
            this.btnStep0001.Text = "0.001";
            // 
            // _jogButtonArea
            // 
            this._jogButtonArea.BackColor = System.Drawing.Color.White;
            this._jogButtonArea.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.jogLayout.SetColumnSpan(this._jogButtonArea, 3);
            this._jogButtonArea.Dock = System.Windows.Forms.DockStyle.Fill;
            this._jogButtonArea.Location = new System.Drawing.Point(9, 226);
            this._jogButtonArea.Margin = new System.Windows.Forms.Padding(8);
            this._jogButtonArea.Name = "_jogButtonArea";
            this._jogButtonArea.Size = new System.Drawing.Size(282, 254);
            this._jogButtonArea.TabIndex = 11;
            // 
            // lblJogHint
            // 
            this.jogLayout.SetColumnSpan(this.lblJogHint, 3);
            this.lblJogHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblJogHint.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblJogHint.ForeColor = System.Drawing.Color.DimGray;
            this.lblJogHint.Location = new System.Drawing.Point(4, 489);
            this.lblJogHint.Name = "lblJogHint";
            this.lblJogHint.Padding = new System.Windows.Forms.Padding(8);
            this.lblJogHint.Size = new System.Drawing.Size(292, 344);
            this.lblJogHint.TabIndex = 12;
            this.lblJogHint.Text = "Select a grid row to switch axis automatically.";
            // 
            // actionsPanel
            // 
            this.actionsPanel.BackColor = System.Drawing.Color.WhiteSmoke;
            this.actionsPanel.ColumnCount = 6;
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this.actionsPanel.Controls.Add(this.btnTeach, 0, 0);
            this.actionsPanel.Controls.Add(this.btnGoto, 1, 0);
            this.actionsPanel.Controls.Add(this.btnApply, 2, 0);
            this.actionsPanel.Controls.Add(this.btnSave, 3, 0);
            this.actionsPanel.Controls.Add(this.btnReload, 4, 0);
            this.actionsPanel.Controls.Add(this.btnReset, 5, 0);
            this.actionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionsPanel.Location = new System.Drawing.Point(8, 872);
            this.actionsPanel.Margin = new System.Windows.Forms.Padding(0);
            this.actionsPanel.Name = "actionsPanel";
            this.actionsPanel.Padding = new System.Windows.Forms.Padding(4);
            this.actionsPanel.RowCount = 1;
            this.actionsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionsPanel.Size = new System.Drawing.Size(1400, 60);
            this.actionsPanel.TabIndex = 2;
            // 
            // btnTeach
            // 
            this.btnTeach.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnTeach.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTeach.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTeach.Font = new System.Drawing.Font("¸¼Àº °íµñ", 12F, System.Drawing.FontStyle.Bold);
            this.btnTeach.ForeColor = System.Drawing.Color.White;
            this.btnTeach.Location = new System.Drawing.Point(8, 8);
            this.btnTeach.Margin = new System.Windows.Forms.Padding(4);
            this.btnTeach.Name = "btnTeach";
            this.btnTeach.Size = new System.Drawing.Size(212, 44);
            this.btnTeach.TabIndex = 0;
            this.btnTeach.Text = "TEACH";
            // 
            // btnGoto
            // 
            this.btnGoto.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGoto.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGoto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoto.Font = new System.Drawing.Font("¸¼Àº °íµñ", 12F, System.Drawing.FontStyle.Bold);
            this.btnGoto.ForeColor = System.Drawing.Color.White;
            this.btnGoto.Location = new System.Drawing.Point(228, 8);
            this.btnGoto.Margin = new System.Windows.Forms.Padding(4);
            this.btnGoto.Name = "btnGoto";
            this.btnGoto.Size = new System.Drawing.Size(212, 44);
            this.btnGoto.TabIndex = 1;
            this.btnGoto.Text = "MOVE TO";
            // 
            // btnApply
            // 
            this.btnApply.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnApply.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnApply.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApply.Font = new System.Drawing.Font("¸¼Àº °íµñ", 12F, System.Drawing.FontStyle.Bold);
            this.btnApply.ForeColor = System.Drawing.Color.White;
            this.btnApply.Location = new System.Drawing.Point(448, 8);
            this.btnApply.Margin = new System.Windows.Forms.Padding(4);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(172, 44);
            this.btnApply.TabIndex = 2;
            this.btnApply.Text = "APPLY";
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.Font = new System.Drawing.Font("¸¼Àº °íµñ", 12F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(628, 8);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(112, 44);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "SAVE";
            // 
            // btnReload
            // 
            this.btnReload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnReload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReload.Font = new System.Drawing.Font("¸¼Àº °íµñ", 12F, System.Drawing.FontStyle.Bold);
            this.btnReload.ForeColor = System.Drawing.Color.White;
            this.btnReload.Location = new System.Drawing.Point(748, 8);
            this.btnReload.Margin = new System.Windows.Forms.Padding(4);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(112, 44);
            this.btnReload.TabIndex = 4;
            this.btnReload.Text = "RELOAD";
            // 
            // btnReset
            // 
            this.btnReset.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnReset.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReset.Font = new System.Drawing.Font("¸¼Àº °íµñ", 12F, System.Drawing.FontStyle.Bold);
            this.btnReset.ForeColor = System.Drawing.Color.White;
            this.btnReset.Location = new System.Drawing.Point(868, 8);
            this.btnReset.Margin = new System.Windows.Forms.Padding(4);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(524, 44);
            this.btnReset.TabIndex = 5;
            this.btnReset.Text = "RESET DEFAULT";
            // 
            // PositionTeachingPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.Controls.Add(this.mainLayout);
            this.Name = "PositionTeachingPage";
            this.Size = new System.Drawing.Size(1416, 940);
            this.mainLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
            this.jogLayout.ResumeLayout(false);
            this.jogLayout.PerformLayout();
            this.stepPresetLayout.ResumeLayout(false);
            this.actionsPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
    }
}
