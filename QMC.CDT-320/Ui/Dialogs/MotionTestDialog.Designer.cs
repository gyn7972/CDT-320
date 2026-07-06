namespace QMC.CDT_320.Ui.Dialogs
{
    partial class MotionTestDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.GroupBox grpAxis;
        private System.Windows.Forms.ListBox lstAxes;
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.GroupBox grpStatus;
        private System.Windows.Forms.TableLayoutPanel statusLayout;
        private System.Windows.Forms.Label lblAxisNameCaption;
        private System.Windows.Forms.Label lblAxisName;
        private System.Windows.Forms.Label lblActualCaption;
        private System.Windows.Forms.Label lblActual;
        private System.Windows.Forms.Label lblCommandCaption;
        private System.Windows.Forms.Label lblCommand;
        private System.Windows.Forms.Label lblUnitCaption;
        private System.Windows.Forms.Label lblUnit;
        private System.Windows.Forms.Label lblAxisState;
        private System.Windows.Forms.GroupBox grpPosition;
        private System.Windows.Forms.TableLayoutPanel positionLayout;
        private System.Windows.Forms.Label lblStartCaption;
        private System.Windows.Forms.NumericUpDown nudStartPosition;
        private System.Windows.Forms.Button btnCaptureStart;
        private System.Windows.Forms.Button btnMoveStart;
        private System.Windows.Forms.Label lblEndCaption;
        private System.Windows.Forms.NumericUpDown nudEndPosition;
        private System.Windows.Forms.Button btnCaptureEnd;
        private System.Windows.Forms.Button btnMoveEnd;
        private System.Windows.Forms.Button btnSwap;
        private System.Windows.Forms.GroupBox grpProfile;
        private System.Windows.Forms.TableLayoutPanel profileLayout;
        private System.Windows.Forms.DataGridView gridProfile;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProfileName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProfileValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colProfileUnit;
        private System.Windows.Forms.Button btnReloadDefault;
        private System.Windows.Forms.GroupBox grpRepeat;
        private System.Windows.Forms.TableLayoutPanel repeatLayout;
        private System.Windows.Forms.Label lblRepeatCaption;
        private System.Windows.Forms.NumericUpDown nudRepeatCount;
        private System.Windows.Forms.Label lblDwellCaption;
        private System.Windows.Forms.NumericUpDown nudDwellMs;
        private System.Windows.Forms.Label lblTimeoutCaption;
        private System.Windows.Forms.NumericUpDown nudTimeoutMs;
        private System.Windows.Forms.CheckBox chkSoftLimitCheck;
        private System.Windows.Forms.CheckBox chkStopOnAlarm;
        private System.Windows.Forms.FlowLayoutPanel commandLayout;
        private System.Windows.Forms.Button btnServoOn;
        private System.Windows.Forms.Button btnStartRepeat;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblCounter;
        private System.Windows.Forms.TableLayoutPanel statusLogLayout;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TableLayoutPanel motionLogLayout;
        private System.Windows.Forms.ListBox lstMotionLog;
        private System.Windows.Forms.Button btnMotionLogClear;

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpAxis = new System.Windows.Forms.GroupBox();
            this.lstAxes = new System.Windows.Forms.ListBox();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpStatus = new System.Windows.Forms.GroupBox();
            this.statusLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblAxisNameCaption = new System.Windows.Forms.Label();
            this.lblAxisName = new System.Windows.Forms.Label();
            this.lblActualCaption = new System.Windows.Forms.Label();
            this.lblActual = new System.Windows.Forms.Label();
            this.lblCommandCaption = new System.Windows.Forms.Label();
            this.lblCommand = new System.Windows.Forms.Label();
            this.lblUnitCaption = new System.Windows.Forms.Label();
            this.lblUnit = new System.Windows.Forms.Label();
            this.lblAxisState = new System.Windows.Forms.Label();
            this.grpPosition = new System.Windows.Forms.GroupBox();
            this.positionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblStartCaption = new System.Windows.Forms.Label();
            this.nudStartPosition = new System.Windows.Forms.NumericUpDown();
            this.btnCaptureStart = new System.Windows.Forms.Button();
            this.btnMoveStart = new System.Windows.Forms.Button();
            this.lblEndCaption = new System.Windows.Forms.Label();
            this.nudEndPosition = new System.Windows.Forms.NumericUpDown();
            this.btnCaptureEnd = new System.Windows.Forms.Button();
            this.btnMoveEnd = new System.Windows.Forms.Button();
            this.btnSwap = new System.Windows.Forms.Button();
            this.grpProfile = new System.Windows.Forms.GroupBox();
            this.profileLayout = new System.Windows.Forms.TableLayoutPanel();
            this.gridProfile = new System.Windows.Forms.DataGridView();
            this.colProfileName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colProfileValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colProfileUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnReloadDefault = new System.Windows.Forms.Button();
            this.grpRepeat = new System.Windows.Forms.GroupBox();
            this.repeatLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblRepeatCaption = new System.Windows.Forms.Label();
            this.nudRepeatCount = new System.Windows.Forms.NumericUpDown();
            this.lblDwellCaption = new System.Windows.Forms.Label();
            this.nudDwellMs = new System.Windows.Forms.NumericUpDown();
            this.lblTimeoutCaption = new System.Windows.Forms.Label();
            this.nudTimeoutMs = new System.Windows.Forms.NumericUpDown();
            this.chkSoftLimitCheck = new System.Windows.Forms.CheckBox();
            this.chkStopOnAlarm = new System.Windows.Forms.CheckBox();
            this.commandLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.btnServoOn = new System.Windows.Forms.Button();
            this.btnStartRepeat = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblCounter = new System.Windows.Forms.Label();
            this.statusLogLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.motionLogLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lstMotionLog = new System.Windows.Forms.ListBox();
            this.btnMotionLogClear = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.grpAxis.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.grpStatus.SuspendLayout();
            this.statusLayout.SuspendLayout();
            this.grpPosition.SuspendLayout();
            this.positionLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudStartPosition)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudEndPosition)).BeginInit();
            this.grpProfile.SuspendLayout();
            this.profileLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridProfile)).BeginInit();
            this.grpRepeat.SuspendLayout();
            this.repeatLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudRepeatCount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDwellMs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTimeoutMs)).BeginInit();
            this.commandLayout.SuspendLayout();
            this.statusLogLayout.SuspendLayout();
            this.motionLogLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 2;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 280F));
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.grpAxis, 0, 0);
            this.rootLayout.Controls.Add(this.rightLayout, 1, 0);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 1;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1040, 702);
            this.rootLayout.TabIndex = 0;
            // 
            // grpAxis
            // 
            this.grpAxis.Controls.Add(this.lstAxes);
            this.grpAxis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAxis.Location = new System.Drawing.Point(11, 11);
            this.grpAxis.Name = "grpAxis";
            this.grpAxis.Size = new System.Drawing.Size(274, 680);
            this.grpAxis.TabIndex = 0;
            this.grpAxis.TabStop = false;
            this.grpAxis.Text = "Axis";
            // 
            // lstAxes
            // 
            this.lstAxes.BackColor = System.Drawing.Color.Black;
            this.lstAxes.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstAxes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstAxes.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold);
            this.lstAxes.ForeColor = System.Drawing.Color.Lime;
            this.lstAxes.FormattingEnabled = true;
            this.lstAxes.IntegralHeight = false;
            this.lstAxes.ItemHeight = 15;
            this.lstAxes.Location = new System.Drawing.Point(3, 19);
            this.lstAxes.Name = "lstAxes";
            this.lstAxes.Size = new System.Drawing.Size(268, 658);
            this.lstAxes.TabIndex = 0;
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.grpStatus, 0, 0);
            this.rightLayout.Controls.Add(this.grpPosition, 0, 1);
            this.rightLayout.Controls.Add(this.grpProfile, 0, 2);
            this.rightLayout.Controls.Add(this.grpRepeat, 0, 3);
            this.rightLayout.Controls.Add(this.commandLayout, 0, 4);
            this.rightLayout.Controls.Add(this.lblCounter, 0, 5);
            this.rightLayout.Controls.Add(this.statusLogLayout, 0, 6);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(291, 11);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 7;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 106F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 124F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 92F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Size = new System.Drawing.Size(738, 680);
            this.rightLayout.TabIndex = 1;
            // 
            // grpStatus
            // 
            this.grpStatus.Controls.Add(this.statusLayout);
            this.grpStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpStatus.Location = new System.Drawing.Point(3, 3);
            this.grpStatus.Name = "grpStatus";
            this.grpStatus.Size = new System.Drawing.Size(732, 100);
            this.grpStatus.TabIndex = 0;
            this.grpStatus.TabStop = false;
            this.grpStatus.Text = "Axis Status";
            // 
            // statusLayout
            // 
            this.statusLayout.ColumnCount = 4;
            this.statusLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 86F));
            this.statusLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.statusLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 86F));
            this.statusLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.statusLayout.Controls.Add(this.lblAxisNameCaption, 0, 0);
            this.statusLayout.Controls.Add(this.lblAxisName, 1, 0);
            this.statusLayout.Controls.Add(this.lblActualCaption, 0, 1);
            this.statusLayout.Controls.Add(this.lblActual, 1, 1);
            this.statusLayout.Controls.Add(this.lblCommandCaption, 2, 1);
            this.statusLayout.Controls.Add(this.lblCommand, 3, 1);
            this.statusLayout.Controls.Add(this.lblUnitCaption, 2, 0);
            this.statusLayout.Controls.Add(this.lblUnit, 3, 0);
            this.statusLayout.Controls.Add(this.lblAxisState, 0, 2);
            this.statusLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusLayout.Location = new System.Drawing.Point(3, 19);
            this.statusLayout.Name = "statusLayout";
            this.statusLayout.RowCount = 3;
            this.statusLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.statusLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.statusLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.statusLayout.Size = new System.Drawing.Size(726, 78);
            this.statusLayout.TabIndex = 0;
            // 
            // lblAxisNameCaption
            // 
            this.lblAxisNameCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisNameCaption.Location = new System.Drawing.Point(3, 0);
            this.lblAxisNameCaption.Name = "lblAxisNameCaption";
            this.lblAxisNameCaption.Size = new System.Drawing.Size(80, 25);
            this.lblAxisNameCaption.TabIndex = 0;
            this.lblAxisNameCaption.Text = "Name";
            this.lblAxisNameCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxisName
            // 
            this.lblAxisName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisName.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblAxisName.Location = new System.Drawing.Point(89, 0);
            this.lblAxisName.Name = "lblAxisName";
            this.lblAxisName.Size = new System.Drawing.Size(161, 25);
            this.lblAxisName.TabIndex = 1;
            this.lblAxisName.Text = "-";
            this.lblAxisName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblActualCaption
            // 
            this.lblActualCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblActualCaption.Location = new System.Drawing.Point(3, 25);
            this.lblActualCaption.Name = "lblActualCaption";
            this.lblActualCaption.Size = new System.Drawing.Size(80, 25);
            this.lblActualCaption.TabIndex = 2;
            this.lblActualCaption.Text = "Actual";
            this.lblActualCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblActual
            // 
            this.lblActual.BackColor = System.Drawing.Color.Black;
            this.lblActual.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblActual.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            this.lblActual.ForeColor = System.Drawing.Color.Lime;
            this.lblActual.Location = new System.Drawing.Point(89, 25);
            this.lblActual.Name = "lblActual";
            this.lblActual.Size = new System.Drawing.Size(161, 25);
            this.lblActual.TabIndex = 3;
            this.lblActual.Text = "-";
            this.lblActual.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCommandCaption
            // 
            this.lblCommandCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCommandCaption.Location = new System.Drawing.Point(256, 25);
            this.lblCommandCaption.Name = "lblCommandCaption";
            this.lblCommandCaption.Size = new System.Drawing.Size(80, 25);
            this.lblCommandCaption.TabIndex = 4;
            this.lblCommandCaption.Text = "Command";
            this.lblCommandCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCommand
            // 
            this.lblCommand.BackColor = System.Drawing.Color.Black;
            this.lblCommand.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCommand.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            this.lblCommand.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(220)))), ((int)(((byte)(130)))));
            this.lblCommand.Location = new System.Drawing.Point(342, 25);
            this.lblCommand.Name = "lblCommand";
            this.lblCommand.Size = new System.Drawing.Size(161, 25);
            this.lblCommand.TabIndex = 5;
            this.lblCommand.Text = "-";
            this.lblCommand.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblUnitCaption
            // 
            this.lblUnitCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUnitCaption.Location = new System.Drawing.Point(256, 0);
            this.lblUnitCaption.Name = "lblUnitCaption";
            this.lblUnitCaption.Size = new System.Drawing.Size(80, 25);
            this.lblUnitCaption.TabIndex = 6;
            this.lblUnitCaption.Text = "Unit";
            this.lblUnitCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblUnit
            // 
            this.lblUnit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUnit.Location = new System.Drawing.Point(342, 0);
            this.lblUnit.Name = "lblUnit";
            this.lblUnit.Size = new System.Drawing.Size(161, 25);
            this.lblUnit.TabIndex = 7;
            this.lblUnit.Text = "-";
            this.lblUnit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxisState
            // 
            this.statusLayout.SetColumnSpan(this.lblAxisState, 4);
            this.lblAxisState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisState.Location = new System.Drawing.Point(3, 50);
            this.lblAxisState.Name = "lblAxisState";
            this.lblAxisState.Size = new System.Drawing.Size(500, 28);
            this.lblAxisState.TabIndex = 8;
            this.lblAxisState.Text = "-";
            this.lblAxisState.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpPosition
            // 
            this.grpPosition.Controls.Add(this.positionLayout);
            this.grpPosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPosition.Location = new System.Drawing.Point(3, 109);
            this.grpPosition.Name = "grpPosition";
            this.grpPosition.Size = new System.Drawing.Size(512, 124);
            this.grpPosition.TabIndex = 1;
            this.grpPosition.TabStop = false;
            this.grpPosition.Text = "Repeat Position";
            // 
            // positionLayout
            // 
            this.positionLayout.ColumnCount = 4;
            this.positionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.positionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.positionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 86F));
            this.positionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 86F));
            this.positionLayout.Controls.Add(this.lblStartCaption, 0, 0);
            this.positionLayout.Controls.Add(this.nudStartPosition, 1, 0);
            this.positionLayout.Controls.Add(this.btnCaptureStart, 2, 0);
            this.positionLayout.Controls.Add(this.btnMoveStart, 3, 0);
            this.positionLayout.Controls.Add(this.lblEndCaption, 0, 1);
            this.positionLayout.Controls.Add(this.nudEndPosition, 1, 1);
            this.positionLayout.Controls.Add(this.btnCaptureEnd, 2, 1);
            this.positionLayout.Controls.Add(this.btnMoveEnd, 3, 1);
            this.positionLayout.Controls.Add(this.btnSwap, 2, 2);
            this.positionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.positionLayout.Location = new System.Drawing.Point(3, 19);
            this.positionLayout.Name = "positionLayout";
            this.positionLayout.Padding = new System.Windows.Forms.Padding(4);
            this.positionLayout.RowCount = 3;
            this.positionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.positionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.positionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.positionLayout.Size = new System.Drawing.Size(506, 102);
            this.positionLayout.TabIndex = 0;
            // 
            // lblStartCaption
            // 
            this.lblStartCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStartCaption.Location = new System.Drawing.Point(7, 4);
            this.lblStartCaption.Name = "lblStartCaption";
            this.lblStartCaption.Size = new System.Drawing.Size(64, 28);
            this.lblStartCaption.TabIndex = 0;
            this.lblStartCaption.Text = "Start";
            this.lblStartCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudStartPosition
            // 
            this.nudStartPosition.DecimalPlaces = 3;
            this.nudStartPosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudStartPosition.Location = new System.Drawing.Point(77, 7);
            this.nudStartPosition.Maximum = new decimal(new int[] {
            100000000,
            0,
            0,
            0});
            this.nudStartPosition.Minimum = new decimal(new int[] {
            100000000,
            0,
            0,
            -2147483648});
            this.nudStartPosition.Name = "nudStartPosition";
            this.nudStartPosition.Size = new System.Drawing.Size(250, 23);
            this.nudStartPosition.TabIndex = 1;
            // 
            // btnCaptureStart
            // 
            this.btnCaptureStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCaptureStart.Location = new System.Drawing.Point(333, 7);
            this.btnCaptureStart.Name = "btnCaptureStart";
            this.btnCaptureStart.Size = new System.Drawing.Size(80, 22);
            this.btnCaptureStart.TabIndex = 2;
            this.btnCaptureStart.Text = "Capture";
            // 
            // btnMoveStart
            // 
            this.btnMoveStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMoveStart.Location = new System.Drawing.Point(419, 7);
            this.btnMoveStart.Name = "btnMoveStart";
            this.btnMoveStart.Size = new System.Drawing.Size(80, 22);
            this.btnMoveStart.TabIndex = 3;
            this.btnMoveStart.Text = "Move";
            // 
            // lblEndCaption
            // 
            this.lblEndCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEndCaption.Location = new System.Drawing.Point(7, 32);
            this.lblEndCaption.Name = "lblEndCaption";
            this.lblEndCaption.Size = new System.Drawing.Size(64, 28);
            this.lblEndCaption.TabIndex = 4;
            this.lblEndCaption.Text = "End";
            this.lblEndCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudEndPosition
            // 
            this.nudEndPosition.DecimalPlaces = 3;
            this.nudEndPosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudEndPosition.Location = new System.Drawing.Point(77, 35);
            this.nudEndPosition.Maximum = new decimal(new int[] {
            100000000,
            0,
            0,
            0});
            this.nudEndPosition.Minimum = new decimal(new int[] {
            100000000,
            0,
            0,
            -2147483648});
            this.nudEndPosition.Name = "nudEndPosition";
            this.nudEndPosition.Size = new System.Drawing.Size(250, 23);
            this.nudEndPosition.TabIndex = 5;
            // 
            // btnCaptureEnd
            // 
            this.btnCaptureEnd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCaptureEnd.Location = new System.Drawing.Point(333, 35);
            this.btnCaptureEnd.Name = "btnCaptureEnd";
            this.btnCaptureEnd.Size = new System.Drawing.Size(80, 22);
            this.btnCaptureEnd.TabIndex = 6;
            this.btnCaptureEnd.Text = "Capture";
            // 
            // btnMoveEnd
            // 
            this.btnMoveEnd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMoveEnd.Location = new System.Drawing.Point(419, 35);
            this.btnMoveEnd.Name = "btnMoveEnd";
            this.btnMoveEnd.Size = new System.Drawing.Size(80, 22);
            this.btnMoveEnd.TabIndex = 7;
            this.btnMoveEnd.Text = "Move";
            // 
            // btnSwap
            // 
            this.positionLayout.SetColumnSpan(this.btnSwap, 2);
            this.btnSwap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSwap.Location = new System.Drawing.Point(333, 63);
            this.btnSwap.Name = "btnSwap";
            this.btnSwap.Size = new System.Drawing.Size(166, 32);
            this.btnSwap.TabIndex = 8;
            this.btnSwap.Text = "Swap";
            // 
            // grpProfile
            // 
            this.grpProfile.Controls.Add(this.profileLayout);
            this.grpProfile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpProfile.Location = new System.Drawing.Point(3, 239);
            this.grpProfile.Name = "grpProfile";
            this.grpProfile.Size = new System.Drawing.Size(512, 118);
            this.grpProfile.TabIndex = 2;
            this.grpProfile.TabStop = false;
            this.grpProfile.Text = "Motion Profile";
            // 
            // profileLayout
            // 
            this.profileLayout.ColumnCount = 2;
            this.profileLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.profileLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.profileLayout.Controls.Add(this.gridProfile, 0, 0);
            this.profileLayout.Controls.Add(this.btnReloadDefault, 1, 0);
            this.profileLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.profileLayout.Location = new System.Drawing.Point(3, 19);
            this.profileLayout.Name = "profileLayout";
            this.profileLayout.RowCount = 1;
            this.profileLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.profileLayout.Size = new System.Drawing.Size(506, 96);
            this.profileLayout.TabIndex = 0;
            // 
            // gridProfile
            // 
            this.gridProfile.AllowUserToAddRows = false;
            this.gridProfile.AllowUserToDeleteRows = false;
            this.gridProfile.AllowUserToResizeRows = false;
            this.gridProfile.BackgroundColor = System.Drawing.Color.White;
            this.gridProfile.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridProfile.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colProfileName,
            this.colProfileValue,
            this.colProfileUnit});
            this.gridProfile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridProfile.Location = new System.Drawing.Point(3, 3);
            this.gridProfile.MultiSelect = false;
            this.gridProfile.Name = "gridProfile";
            this.gridProfile.RowHeadersVisible = false;
            this.gridProfile.RowTemplate.Height = 24;
            this.gridProfile.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.gridProfile.Size = new System.Drawing.Size(370, 90);
            this.gridProfile.TabIndex = 0;
            // 
            // colProfileName
            // 
            this.colProfileName.HeaderText = "Item";
            this.colProfileName.Name = "colProfileName";
            this.colProfileName.ReadOnly = true;
            this.colProfileName.Width = 130;
            // 
            // colProfileValue
            // 
            this.colProfileValue.HeaderText = "Value";
            this.colProfileValue.Name = "colProfileValue";
            this.colProfileValue.Width = 120;
            // 
            // colProfileUnit
            // 
            this.colProfileUnit.HeaderText = "Unit";
            this.colProfileUnit.Name = "colProfileUnit";
            this.colProfileUnit.ReadOnly = true;
            this.colProfileUnit.Width = 90;
            // 
            // btnReloadDefault
            // 
            this.btnReloadDefault.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReloadDefault.Location = new System.Drawing.Point(379, 3);
            this.btnReloadDefault.Name = "btnReloadDefault";
            this.btnReloadDefault.Size = new System.Drawing.Size(124, 90);
            this.btnReloadDefault.TabIndex = 1;
            this.btnReloadDefault.Text = "Reload\r\nDefault";
            this.btnReloadDefault.UseVisualStyleBackColor = true;
            // 
            // grpRepeat
            // 
            this.grpRepeat.Controls.Add(this.repeatLayout);
            this.grpRepeat.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpRepeat.Location = new System.Drawing.Point(3, 363);
            this.grpRepeat.Name = "grpRepeat";
            this.grpRepeat.Size = new System.Drawing.Size(512, 86);
            this.grpRepeat.TabIndex = 3;
            this.grpRepeat.TabStop = false;
            this.grpRepeat.Text = "Repeat Option";
            // 
            // repeatLayout
            // 
            this.repeatLayout.ColumnCount = 6;
            this.repeatLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.repeatLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.repeatLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.repeatLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.repeatLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.repeatLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.repeatLayout.Controls.Add(this.lblRepeatCaption, 0, 0);
            this.repeatLayout.Controls.Add(this.nudRepeatCount, 1, 0);
            this.repeatLayout.Controls.Add(this.lblDwellCaption, 2, 0);
            this.repeatLayout.Controls.Add(this.nudDwellMs, 3, 0);
            this.repeatLayout.Controls.Add(this.lblTimeoutCaption, 4, 0);
            this.repeatLayout.Controls.Add(this.nudTimeoutMs, 5, 0);
            this.repeatLayout.Controls.Add(this.chkSoftLimitCheck, 0, 1);
            this.repeatLayout.Controls.Add(this.chkStopOnAlarm, 3, 1);
            this.repeatLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.repeatLayout.Location = new System.Drawing.Point(3, 19);
            this.repeatLayout.Name = "repeatLayout";
            this.repeatLayout.Padding = new System.Windows.Forms.Padding(4);
            this.repeatLayout.RowCount = 2;
            this.repeatLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.repeatLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.repeatLayout.Size = new System.Drawing.Size(506, 64);
            this.repeatLayout.TabIndex = 0;
            // 
            // lblRepeatCaption
            // 
            this.lblRepeatCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRepeatCaption.Location = new System.Drawing.Point(7, 4);
            this.lblRepeatCaption.Name = "lblRepeatCaption";
            this.lblRepeatCaption.Size = new System.Drawing.Size(64, 30);
            this.lblRepeatCaption.TabIndex = 0;
            this.lblRepeatCaption.Text = "Repeat";
            this.lblRepeatCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudRepeatCount
            // 
            this.nudRepeatCount.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudRepeatCount.Location = new System.Drawing.Point(77, 7);
            this.nudRepeatCount.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
            this.nudRepeatCount.Name = "nudRepeatCount";
            this.nudRepeatCount.Size = new System.Drawing.Size(89, 23);
            this.nudRepeatCount.TabIndex = 1;
            this.nudRepeatCount.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // lblDwellCaption
            // 
            this.lblDwellCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDwellCaption.Location = new System.Drawing.Point(172, 4);
            this.lblDwellCaption.Name = "lblDwellCaption";
            this.lblDwellCaption.Size = new System.Drawing.Size(64, 30);
            this.lblDwellCaption.TabIndex = 2;
            this.lblDwellCaption.Text = "Dwell";
            this.lblDwellCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudDwellMs
            // 
            this.nudDwellMs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudDwellMs.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nudDwellMs.Location = new System.Drawing.Point(242, 7);
            this.nudDwellMs.Maximum = new decimal(new int[] {
            600000,
            0,
            0,
            0});
            this.nudDwellMs.Name = "nudDwellMs";
            this.nudDwellMs.Size = new System.Drawing.Size(89, 23);
            this.nudDwellMs.TabIndex = 3;
            this.nudDwellMs.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // lblTimeoutCaption
            // 
            this.lblTimeoutCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTimeoutCaption.Location = new System.Drawing.Point(337, 4);
            this.lblTimeoutCaption.Name = "lblTimeoutCaption";
            this.lblTimeoutCaption.Size = new System.Drawing.Size(64, 30);
            this.lblTimeoutCaption.TabIndex = 4;
            this.lblTimeoutCaption.Text = "Timeout";
            this.lblTimeoutCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudTimeoutMs
            // 
            this.nudTimeoutMs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudTimeoutMs.Increment = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudTimeoutMs.Location = new System.Drawing.Point(407, 7);
            this.nudTimeoutMs.Maximum = new decimal(new int[] {
            600000,
            0,
            0,
            0});
            this.nudTimeoutMs.Name = "nudTimeoutMs";
            this.nudTimeoutMs.Size = new System.Drawing.Size(92, 23);
            this.nudTimeoutMs.TabIndex = 5;
            this.nudTimeoutMs.Value = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            // 
            // chkSoftLimitCheck
            // 
            this.chkSoftLimitCheck.Checked = true;
            this.chkSoftLimitCheck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.repeatLayout.SetColumnSpan(this.chkSoftLimitCheck, 3);
            this.chkSoftLimitCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkSoftLimitCheck.Location = new System.Drawing.Point(7, 37);
            this.chkSoftLimitCheck.Name = "chkSoftLimitCheck";
            this.chkSoftLimitCheck.Size = new System.Drawing.Size(229, 20);
            this.chkSoftLimitCheck.TabIndex = 6;
            this.chkSoftLimitCheck.Text = "Soft limit check";
            // 
            // chkStopOnAlarm
            // 
            this.chkStopOnAlarm.Checked = true;
            this.chkStopOnAlarm.CheckState = System.Windows.Forms.CheckState.Checked;
            this.repeatLayout.SetColumnSpan(this.chkStopOnAlarm, 3);
            this.chkStopOnAlarm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkStopOnAlarm.Location = new System.Drawing.Point(242, 37);
            this.chkStopOnAlarm.Name = "chkStopOnAlarm";
            this.chkStopOnAlarm.Size = new System.Drawing.Size(257, 20);
            this.chkStopOnAlarm.TabIndex = 7;
            this.chkStopOnAlarm.Text = "Stop on alarm";
            // 
            // commandLayout
            // 
            this.commandLayout.Controls.Add(this.btnServoOn);
            this.commandLayout.Controls.Add(this.btnStartRepeat);
            this.commandLayout.Controls.Add(this.btnStop);
            this.commandLayout.Controls.Add(this.btnClose);
            this.commandLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.commandLayout.Location = new System.Drawing.Point(3, 455);
            this.commandLayout.Name = "commandLayout";
            this.commandLayout.Size = new System.Drawing.Size(512, 36);
            this.commandLayout.TabIndex = 4;
            this.commandLayout.WrapContents = false;
            // 
            // btnServoOn
            // 
            this.btnServoOn.Location = new System.Drawing.Point(3, 3);
            this.btnServoOn.Name = "btnServoOn";
            this.btnServoOn.Size = new System.Drawing.Size(90, 30);
            this.btnServoOn.TabIndex = 0;
            this.btnServoOn.Text = "SERVO ON";
            this.btnServoOn.UseVisualStyleBackColor = true;
            // 
            // btnStartRepeat
            // 
            this.btnStartRepeat.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(92)))), ((int)(((byte)(76)))));
            this.btnStartRepeat.ForeColor = System.Drawing.Color.White;
            this.btnStartRepeat.Location = new System.Drawing.Point(99, 3);
            this.btnStartRepeat.Name = "btnStartRepeat";
            this.btnStartRepeat.Size = new System.Drawing.Size(118, 30);
            this.btnStartRepeat.TabIndex = 1;
            this.btnStartRepeat.Text = "START REPEAT";
            this.btnStartRepeat.UseVisualStyleBackColor = false;
            // 
            // btnStop
            // 
            this.btnStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(122)))), ((int)(((byte)(46)))), ((int)(((byte)(46)))));
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.Location = new System.Drawing.Point(223, 3);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(86, 30);
            this.btnStop.TabIndex = 2;
            this.btnStop.Text = "STOP";
            this.btnStop.UseVisualStyleBackColor = false;
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(315, 3);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(86, 30);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = true;
            // 
            // lblCounter
            // 
            this.lblCounter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCounter.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblCounter.Location = new System.Drawing.Point(3, 494);
            this.lblCounter.Name = "lblCounter";
            this.lblCounter.Size = new System.Drawing.Size(512, 28);
            this.lblCounter.TabIndex = 5;
            this.lblCounter.Text = "Cycle 0 / 0  Legs 0";
            this.lblCounter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // statusLogLayout
            // 
            this.statusLogLayout.ColumnCount = 2;
            this.statusLogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 48F));
            this.statusLogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 52F));
            this.statusLogLayout.Controls.Add(this.lblStatus, 0, 0);
            this.statusLogLayout.Controls.Add(this.motionLogLayout, 1, 0);
            this.statusLogLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusLogLayout.Location = new System.Drawing.Point(3, 525);
            this.statusLogLayout.Name = "statusLogLayout";
            this.statusLogLayout.RowCount = 1;
            this.statusLogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.statusLogLayout.Size = new System.Drawing.Size(732, 152);
            this.statusLogLayout.TabIndex = 6;
            // 
            // lblStatus
            // 
            this.lblStatus.BackColor = System.Drawing.Color.Black;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblStatus.ForeColor = System.Drawing.Color.Lime;
            this.lblStatus.Location = new System.Drawing.Point(3, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblStatus.Size = new System.Drawing.Size(345, 152);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "Ready";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // motionLogLayout
            // 
            this.motionLogLayout.ColumnCount = 1;
            this.motionLogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.motionLogLayout.Controls.Add(this.lstMotionLog, 0, 0);
            this.motionLogLayout.Controls.Add(this.btnMotionLogClear, 0, 1);
            this.motionLogLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.motionLogLayout.Location = new System.Drawing.Point(354, 3);
            this.motionLogLayout.Name = "motionLogLayout";
            this.motionLogLayout.RowCount = 2;
            this.motionLogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.motionLogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.motionLogLayout.Size = new System.Drawing.Size(375, 146);
            this.motionLogLayout.TabIndex = 1;
            // 
            // lstMotionLog
            // 
            this.lstMotionLog.BackColor = System.Drawing.Color.Black;
            this.lstMotionLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lstMotionLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstMotionLog.Font = new System.Drawing.Font("Consolas", 8F);
            this.lstMotionLog.ForeColor = System.Drawing.Color.Lime;
            this.lstMotionLog.FormattingEnabled = true;
            this.lstMotionLog.HorizontalScrollbar = true;
            this.lstMotionLog.IntegralHeight = false;
            this.lstMotionLog.Location = new System.Drawing.Point(3, 3);
            this.lstMotionLog.Name = "lstMotionLog";
            this.lstMotionLog.Size = new System.Drawing.Size(369, 110);
            this.lstMotionLog.TabIndex = 0;
            // 
            // btnMotionLogClear
            // 
            this.btnMotionLogClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMotionLogClear.Location = new System.Drawing.Point(3, 119);
            this.btnMotionLogClear.Name = "btnMotionLogClear";
            this.btnMotionLogClear.Size = new System.Drawing.Size(369, 24);
            this.btnMotionLogClear.TabIndex = 1;
            this.btnMotionLogClear.Text = "Clear";
            this.btnMotionLogClear.UseVisualStyleBackColor = true;
            // 
            // MotionTestDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1040, 702);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.MinimumSize = new System.Drawing.Size(1056, 699);
            this.Name = "MotionTestDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Motion Test";
            this.rootLayout.ResumeLayout(false);
            this.grpAxis.ResumeLayout(false);
            this.rightLayout.ResumeLayout(false);
            this.grpStatus.ResumeLayout(false);
            this.statusLayout.ResumeLayout(false);
            this.grpPosition.ResumeLayout(false);
            this.positionLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudStartPosition)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudEndPosition)).EndInit();
            this.grpProfile.ResumeLayout(false);
            this.profileLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridProfile)).EndInit();
            this.grpRepeat.ResumeLayout(false);
            this.repeatLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudRepeatCount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDwellMs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTimeoutMs)).EndInit();
            this.commandLayout.ResumeLayout(false);
            this.statusLogLayout.ResumeLayout(false);
            this.motionLogLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
