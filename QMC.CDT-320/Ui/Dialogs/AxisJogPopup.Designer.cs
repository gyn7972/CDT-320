namespace QMC.CDT_320.Ui.Dialogs
{
    partial class AxisJogPopup
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.TableLayoutPanel leftLayout;
        private System.Windows.Forms.Label lblPosition;
        private System.Windows.Forms.GroupBox grpSelectAxis;
        private System.Windows.Forms.ListBox selectAxisList;
        private QMC.CDT_320.Ui.Controls.JogPositionListControl jogPositionListControl;
        private QMC.CDT_320.Ui.Controls.JogAxisMoveControl jogAxisMoveControl;
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.GroupBox grpMove;
        private System.Windows.Forms.FlowLayoutPanel speedLayout;
        private System.Windows.Forms.RadioButton rdoFine;
        private System.Windows.Forms.RadioButton rdoCoarse;
        private System.Windows.Forms.GroupBox grpMoveMode;
        private System.Windows.Forms.TableLayoutPanel modeLayout;
        private System.Windows.Forms.FlowLayoutPanel moveModeRadioLayout;
        private System.Windows.Forms.RadioButton rdoContinuous;
        private System.Windows.Forms.RadioButton rdoStep;
        private System.Windows.Forms.NumericUpDown nudStep;
        private System.Windows.Forms.FlowLayoutPanel stepPresetLayout;
        private System.Windows.Forms.Button btnStep1;
        private System.Windows.Forms.Button btnStep01;
        private System.Windows.Forms.Button btnStep001;
        private System.Windows.Forms.Button btnStep0001;
        private System.Windows.Forms.Button btnStepZero;
        private System.Windows.Forms.TableLayoutPanel jogLayout;
        private System.Windows.Forms.Button btnTMinus;
        private System.Windows.Forms.Button btnYPlus;
        private System.Windows.Forms.Button btnTPlus;
        private System.Windows.Forms.Button btnZPlus;
        private System.Windows.Forms.Button btnXMinus;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnXPlus;
        private System.Windows.Forms.Button btnPrevIndex;
        private System.Windows.Forms.Button btnYMinus;
        private System.Windows.Forms.Button btnNextIndex;
        private System.Windows.Forms.Button btnZMinus;

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.leftLayout = new System.Windows.Forms.TableLayoutPanel();
            this.jogPositionListControl = new QMC.CDT_320.Ui.Controls.JogPositionListControl();
            this.grpSelectAxis = new System.Windows.Forms.GroupBox();
            this.selectAxisList = new System.Windows.Forms.ListBox();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.jogAxisMoveControl = new QMC.CDT_320.Ui.Controls.JogAxisMoveControl();
            this.lblPosition = new System.Windows.Forms.Label();
            this.grpMove = new System.Windows.Forms.GroupBox();
            this.speedLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.rdoFine = new System.Windows.Forms.RadioButton();
            this.rdoCoarse = new System.Windows.Forms.RadioButton();
            this.grpMoveMode = new System.Windows.Forms.GroupBox();
            this.modeLayout = new System.Windows.Forms.TableLayoutPanel();
            this.moveModeRadioLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.rdoContinuous = new System.Windows.Forms.RadioButton();
            this.rdoStep = new System.Windows.Forms.RadioButton();
            this.nudStep = new System.Windows.Forms.NumericUpDown();
            this.stepPresetLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.btnStep1 = new System.Windows.Forms.Button();
            this.btnStep01 = new System.Windows.Forms.Button();
            this.btnStep001 = new System.Windows.Forms.Button();
            this.btnStep0001 = new System.Windows.Forms.Button();
            this.btnStepZero = new System.Windows.Forms.Button();
            this.jogLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnTMinus = new System.Windows.Forms.Button();
            this.btnYPlus = new System.Windows.Forms.Button();
            this.btnTPlus = new System.Windows.Forms.Button();
            this.btnZPlus = new System.Windows.Forms.Button();
            this.btnXMinus = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnXPlus = new System.Windows.Forms.Button();
            this.btnZMinus = new System.Windows.Forms.Button();
            this.btnPrevIndex = new System.Windows.Forms.Button();
            this.btnYMinus = new System.Windows.Forms.Button();
            this.btnNextIndex = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.grpSelectAxis.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.grpMove.SuspendLayout();
            this.speedLayout.SuspendLayout();
            this.grpMoveMode.SuspendLayout();
            this.modeLayout.SuspendLayout();
            this.moveModeRadioLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudStep)).BeginInit();
            this.stepPresetLayout.SuspendLayout();
            this.jogLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 2;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 265F));
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.leftLayout, 0, 0);
            this.rootLayout.Controls.Add(this.rightLayout, 1, 0);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(6);
            this.rootLayout.RowCount = 1;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(746, 388);
            this.rootLayout.TabIndex = 0;
            // 
            // leftLayout
            // 
            this.leftLayout.ColumnCount = 1;
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Controls.Add(this.jogPositionListControl, 0, 0);
            this.leftLayout.Controls.Add(this.grpSelectAxis, 0, 1);
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Location = new System.Drawing.Point(9, 9);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.RowCount = 2;
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Size = new System.Drawing.Size(259, 365);
            this.leftLayout.TabIndex = 0;
            // 
            // jogPositionListControl
            // 
            this.jogPositionListControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogPositionListControl.Location = new System.Drawing.Point(3, 3);
            this.jogPositionListControl.Margin = new System.Windows.Forms.Padding(3, 3, 3, 4);
            this.jogPositionListControl.Name = "jogPositionListControl";
            this.jogPositionListControl.Size = new System.Drawing.Size(253, 45);
            this.jogPositionListControl.TabIndex = 0;
            // 
            // grpSelectAxis
            // 
            this.grpSelectAxis.Controls.Add(this.selectAxisList);
            this.grpSelectAxis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSelectAxis.Location = new System.Drawing.Point(3, 55);
            this.grpSelectAxis.Name = "grpSelectAxis";
            this.grpSelectAxis.Size = new System.Drawing.Size(253, 307);
            this.grpSelectAxis.TabIndex = 1;
            this.grpSelectAxis.TabStop = false;
            this.grpSelectAxis.Text = "Select Axis";
            // 
            // selectAxisList
            // 
            this.selectAxisList.BackColor = System.Drawing.Color.Black;
            this.selectAxisList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.selectAxisList.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold);
            this.selectAxisList.ForeColor = System.Drawing.Color.Lime;
            this.selectAxisList.IntegralHeight = false;
            this.selectAxisList.ItemHeight = 20;
            this.selectAxisList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectAxisList.Location = new System.Drawing.Point(3, 23);
            this.selectAxisList.Margin = new System.Windows.Forms.Padding(3);
            this.selectAxisList.Name = "selectAxisList";
            this.selectAxisList.Size = new System.Drawing.Size(247, 281);
            this.selectAxisList.TabIndex = 0;
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.jogAxisMoveControl, 0, 0);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(274, 9);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 1;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Size = new System.Drawing.Size(463, 365);
            this.rightLayout.TabIndex = 1;
            // 
            // jogAxisMoveControl
            // 
            this.jogAxisMoveControl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.jogAxisMoveControl.ButtonAreaMaxHeight = 92;
            this.jogAxisMoveControl.ButtonAreaMaxWidth = 160;
            this.jogAxisMoveControl.ButtonAreaMinHeight = 72;
            this.jogAxisMoveControl.ButtonAreaMinWidth = 132;
            this.jogAxisMoveControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogAxisMoveControl.Location = new System.Drawing.Point(0, 0);
            this.jogAxisMoveControl.Margin = new System.Windows.Forms.Padding(0);
            this.jogAxisMoveControl.Name = "jogAxisMoveControl";
            this.jogAxisMoveControl.ShowCurrentSpeedMode = false;
            this.jogAxisMoveControl.Size = new System.Drawing.Size(458, 365);
            this.jogAxisMoveControl.SpeedControl = null;
            this.jogAxisMoveControl.TabIndex = 1;
            // 
            // lblPosition
            // 
            this.lblPosition.BackColor = System.Drawing.Color.Black;
            this.lblPosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPosition.Font = new System.Drawing.Font("Consolas", 16F, System.Drawing.FontStyle.Bold);
            this.lblPosition.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(230)))), ((int)(((byte)(80)))));
            this.lblPosition.Location = new System.Drawing.Point(3, 0);
            this.lblPosition.Name = "lblPosition";
            this.lblPosition.Size = new System.Drawing.Size(178, 50);
            this.lblPosition.TabIndex = 0;
            this.lblPosition.Text = "000.000";
            this.lblPosition.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // grpMove
            // 
            this.grpMove.Controls.Add(this.speedLayout);
            this.grpMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMove.Location = new System.Drawing.Point(3, 3);
            this.grpMove.Name = "grpMove";
            this.grpMove.Size = new System.Drawing.Size(286, 64);
            this.grpMove.TabIndex = 0;
            this.grpMove.TabStop = false;
            this.grpMove.Text = "Move";
            // 
            // speedLayout
            // 
            this.speedLayout.Controls.Add(this.rdoFine);
            this.speedLayout.Controls.Add(this.rdoCoarse);
            this.speedLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.speedLayout.Location = new System.Drawing.Point(3, 21);
            this.speedLayout.Name = "speedLayout";
            this.speedLayout.Padding = new System.Windows.Forms.Padding(8, 8, 0, 0);
            this.speedLayout.Size = new System.Drawing.Size(280, 40);
            this.speedLayout.TabIndex = 0;
            // 
            // rdoFine
            // 
            this.rdoFine.AutoSize = true;
            this.rdoFine.Location = new System.Drawing.Point(11, 11);
            this.rdoFine.Name = "rdoFine";
            this.rdoFine.Size = new System.Drawing.Size(55, 19);
            this.rdoFine.TabIndex = 0;
            this.rdoFine.Text = "Fine";
            // 
            // rdoCoarse
            // 
            this.rdoCoarse.AutoSize = true;
            this.rdoCoarse.Location = new System.Drawing.Point(72, 11);
            this.rdoCoarse.Name = "rdoCoarse";
            this.rdoCoarse.Size = new System.Drawing.Size(75, 19);
            this.rdoCoarse.TabIndex = 1;
            this.rdoCoarse.Text = "Coarse";
            // 
            // grpMoveMode
            // 
            this.grpMoveMode.Controls.Add(this.modeLayout);
            this.grpMoveMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMoveMode.Location = new System.Drawing.Point(3, 73);
            this.grpMoveMode.Name = "grpMoveMode";
            this.grpMoveMode.Size = new System.Drawing.Size(286, 120);
            this.grpMoveMode.TabIndex = 1;
            this.grpMoveMode.TabStop = false;
            this.grpMoveMode.Text = "Move Mode";
            // 
            // modeLayout
            // 
            this.modeLayout.ColumnCount = 1;
            this.modeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.modeLayout.Controls.Add(this.moveModeRadioLayout, 0, 0);
            this.modeLayout.Controls.Add(this.nudStep, 0, 1);
            this.modeLayout.Controls.Add(this.stepPresetLayout, 0, 2);
            this.modeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.modeLayout.Location = new System.Drawing.Point(3, 21);
            this.modeLayout.Name = "modeLayout";
            this.modeLayout.Padding = new System.Windows.Forms.Padding(6, 3, 6, 5);
            this.modeLayout.RowCount = 3;
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.modeLayout.Size = new System.Drawing.Size(280, 96);
            this.modeLayout.TabIndex = 0;
            // 
            // moveModeRadioLayout
            // 
            this.moveModeRadioLayout.Controls.Add(this.rdoContinuous);
            this.moveModeRadioLayout.Controls.Add(this.rdoStep);
            this.moveModeRadioLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.moveModeRadioLayout.Location = new System.Drawing.Point(9, 6);
            this.moveModeRadioLayout.Name = "moveModeRadioLayout";
            this.moveModeRadioLayout.Size = new System.Drawing.Size(262, 24);
            this.moveModeRadioLayout.TabIndex = 0;
            // 
            // rdoContinuous
            // 
            this.rdoContinuous.AutoSize = true;
            this.rdoContinuous.Location = new System.Drawing.Point(3, 3);
            this.rdoContinuous.Name = "rdoContinuous";
            this.rdoContinuous.Size = new System.Drawing.Size(103, 19);
            this.rdoContinuous.TabIndex = 0;
            this.rdoContinuous.Text = "Continuous";
            // 
            // rdoStep
            // 
            this.rdoStep.AutoSize = true;
            this.rdoStep.Location = new System.Drawing.Point(112, 3);
            this.rdoStep.Name = "rdoStep";
            this.rdoStep.Size = new System.Drawing.Size(58, 19);
            this.rdoStep.TabIndex = 1;
            this.rdoStep.Text = "Step";
            // 
            // nudStep
            // 
            this.nudStep.Dock = System.Windows.Forms.DockStyle.Left;
            this.nudStep.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.nudStep.Location = new System.Drawing.Point(9, 36);
            this.nudStep.Maximum = new decimal(new int[] {
            900000,
            0,
            0,
            0});
            this.nudStep.Minimum = new decimal(new int[] {
            900000,
            0,
            0,
            -2147483648});
            this.nudStep.Name = "nudStep";
            this.nudStep.Size = new System.Drawing.Size(110, 25);
            this.nudStep.TabIndex = 1;
            this.nudStep.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // stepPresetLayout
            // 
            this.stepPresetLayout.Controls.Add(this.btnStep1);
            this.stepPresetLayout.Controls.Add(this.btnStep01);
            this.stepPresetLayout.Controls.Add(this.btnStep001);
            this.stepPresetLayout.Controls.Add(this.btnStep0001);
            this.stepPresetLayout.Controls.Add(this.btnStepZero);
            this.stepPresetLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stepPresetLayout.Location = new System.Drawing.Point(9, 66);
            this.stepPresetLayout.Name = "stepPresetLayout";
            this.stepPresetLayout.Size = new System.Drawing.Size(262, 22);
            this.stepPresetLayout.TabIndex = 2;
            this.stepPresetLayout.WrapContents = false;
            // 
            // btnStep1
            // 
            this.btnStep1.Location = new System.Drawing.Point(2, 2);
            this.btnStep1.Margin = new System.Windows.Forms.Padding(2);
            this.btnStep1.Name = "btnStep1";
            this.btnStep1.Size = new System.Drawing.Size(48, 24);
            this.btnStep1.TabIndex = 0;
            this.btnStep1.Text = "1000";
            // 
            // btnStep01
            // 
            this.btnStep01.Location = new System.Drawing.Point(54, 2);
            this.btnStep01.Margin = new System.Windows.Forms.Padding(2);
            this.btnStep01.Name = "btnStep01";
            this.btnStep01.Size = new System.Drawing.Size(48, 24);
            this.btnStep01.TabIndex = 1;
            this.btnStep01.Text = "100";
            // 
            // btnStep001
            // 
            this.btnStep001.Location = new System.Drawing.Point(106, 2);
            this.btnStep001.Margin = new System.Windows.Forms.Padding(2);
            this.btnStep001.Name = "btnStep001";
            this.btnStep001.Size = new System.Drawing.Size(48, 24);
            this.btnStep001.TabIndex = 2;
            this.btnStep001.Text = "10";
            // 
            // btnStep0001
            // 
            this.btnStep0001.Location = new System.Drawing.Point(158, 2);
            this.btnStep0001.Margin = new System.Windows.Forms.Padding(2);
            this.btnStep0001.Name = "btnStep0001";
            this.btnStep0001.Size = new System.Drawing.Size(48, 24);
            this.btnStep0001.TabIndex = 3;
            this.btnStep0001.Text = "1um";
            // 
            // btnStepZero
            // 
            this.btnStepZero.Location = new System.Drawing.Point(210, 2);
            this.btnStepZero.Margin = new System.Windows.Forms.Padding(2);
            this.btnStepZero.Name = "btnStepZero";
            this.btnStepZero.Size = new System.Drawing.Size(48, 24);
            this.btnStepZero.TabIndex = 4;
            this.btnStepZero.Text = "0\'um";
            // 
            // jogLayout
            // 
            this.jogLayout.ColumnCount = 4;
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.jogLayout.Controls.Add(this.btnTMinus, 0, 0);
            this.jogLayout.Controls.Add(this.btnYPlus, 1, 0);
            this.jogLayout.Controls.Add(this.btnTPlus, 2, 0);
            this.jogLayout.Controls.Add(this.btnZPlus, 3, 0);
            this.jogLayout.Controls.Add(this.btnXMinus, 0, 1);
            this.jogLayout.Controls.Add(this.btnStop, 1, 1);
            this.jogLayout.Controls.Add(this.btnXPlus, 2, 1);
            this.jogLayout.Controls.Add(this.btnZMinus, 3, 1);
            this.jogLayout.Controls.Add(this.btnPrevIndex, 0, 2);
            this.jogLayout.Controls.Add(this.btnYMinus, 1, 2);
            this.jogLayout.Controls.Add(this.btnNextIndex, 2, 2);
            this.jogLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogLayout.Location = new System.Drawing.Point(3, 199);
            this.jogLayout.Name = "jogLayout";
            this.jogLayout.Padding = new System.Windows.Forms.Padding(3);
            this.jogLayout.RowCount = 3;
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.333F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.333F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.333F));
            this.jogLayout.Size = new System.Drawing.Size(286, 210);
            this.jogLayout.TabIndex = 2;
            // 
            // btnTMinus
            // 
            this.btnTMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTMinus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnTMinus.Location = new System.Drawing.Point(7, 7);
            this.btnTMinus.Margin = new System.Windows.Forms.Padding(4);
            this.btnTMinus.Name = "btnTMinus";
            this.btnTMinus.Size = new System.Drawing.Size(62, 60);
            this.btnTMinus.TabIndex = 0;
            this.btnTMinus.Text = "T-";
            this.btnTMinus.UseVisualStyleBackColor = true;
            // 
            // btnYPlus
            // 
            this.btnYPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnYPlus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnYPlus.Location = new System.Drawing.Point(77, 7);
            this.btnYPlus.Margin = new System.Windows.Forms.Padding(4);
            this.btnYPlus.Name = "btnYPlus";
            this.btnYPlus.Size = new System.Drawing.Size(62, 60);
            this.btnYPlus.TabIndex = 1;
            this.btnYPlus.Text = "Y+";
            this.btnYPlus.UseVisualStyleBackColor = true;
            // 
            // btnTPlus
            // 
            this.btnTPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTPlus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnTPlus.Location = new System.Drawing.Point(147, 7);
            this.btnTPlus.Margin = new System.Windows.Forms.Padding(4);
            this.btnTPlus.Name = "btnTPlus";
            this.btnTPlus.Size = new System.Drawing.Size(62, 60);
            this.btnTPlus.TabIndex = 2;
            this.btnTPlus.Text = "T+";
            this.btnTPlus.UseVisualStyleBackColor = true;
            // 
            // btnZPlus
            // 
            this.btnZPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnZPlus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnZPlus.Location = new System.Drawing.Point(217, 7);
            this.btnZPlus.Margin = new System.Windows.Forms.Padding(4);
            this.btnZPlus.Name = "btnZPlus";
            this.btnZPlus.Size = new System.Drawing.Size(62, 60);
            this.btnZPlus.TabIndex = 3;
            this.btnZPlus.Text = "Z+";
            this.btnZPlus.UseVisualStyleBackColor = true;
            // 
            // btnXMinus
            // 
            this.btnXMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnXMinus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnXMinus.Location = new System.Drawing.Point(7, 75);
            this.btnXMinus.Margin = new System.Windows.Forms.Padding(4);
            this.btnXMinus.Name = "btnXMinus";
            this.btnXMinus.Size = new System.Drawing.Size(62, 60);
            this.btnXMinus.TabIndex = 4;
            this.btnXMinus.Text = "X-";
            this.btnXMinus.UseVisualStyleBackColor = true;
            // 
            // btnStop
            // 
            this.btnStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStop.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnStop.Location = new System.Drawing.Point(77, 75);
            this.btnStop.Margin = new System.Windows.Forms.Padding(4);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(62, 60);
            this.btnStop.TabIndex = 5;
            this.btnStop.Text = "STOP";
            this.btnStop.UseVisualStyleBackColor = true;
            // 
            // btnXPlus
            // 
            this.btnXPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnXPlus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnXPlus.Location = new System.Drawing.Point(147, 75);
            this.btnXPlus.Margin = new System.Windows.Forms.Padding(4);
            this.btnXPlus.Name = "btnXPlus";
            this.btnXPlus.Size = new System.Drawing.Size(62, 60);
            this.btnXPlus.TabIndex = 6;
            this.btnXPlus.Text = "X+";
            this.btnXPlus.UseVisualStyleBackColor = true;
            // 
            // btnZMinus
            // 
            this.btnZMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnZMinus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnZMinus.Location = new System.Drawing.Point(217, 75);
            this.btnZMinus.Margin = new System.Windows.Forms.Padding(4);
            this.btnZMinus.Name = "btnZMinus";
            this.btnZMinus.Size = new System.Drawing.Size(62, 60);
            this.btnZMinus.TabIndex = 7;
            this.btnZMinus.Text = "Z-";
            this.btnZMinus.UseVisualStyleBackColor = true;
            // 
            // btnPrevIndex
            // 
            this.btnPrevIndex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPrevIndex.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnPrevIndex.Location = new System.Drawing.Point(7, 143);
            this.btnPrevIndex.Margin = new System.Windows.Forms.Padding(4);
            this.btnPrevIndex.Name = "btnPrevIndex";
            this.btnPrevIndex.Size = new System.Drawing.Size(62, 60);
            this.btnPrevIndex.TabIndex = 8;
            this.btnPrevIndex.Text = "Prev\r\nIndex";
            this.btnPrevIndex.UseVisualStyleBackColor = true;
            // 
            // btnYMinus
            // 
            this.btnYMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnYMinus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnYMinus.Location = new System.Drawing.Point(77, 143);
            this.btnYMinus.Margin = new System.Windows.Forms.Padding(4);
            this.btnYMinus.Name = "btnYMinus";
            this.btnYMinus.Size = new System.Drawing.Size(62, 60);
            this.btnYMinus.TabIndex = 9;
            this.btnYMinus.Text = "Y-";
            this.btnYMinus.UseVisualStyleBackColor = true;
            // 
            // btnNextIndex
            // 
            this.jogLayout.SetColumnSpan(this.btnNextIndex, 2);
            this.btnNextIndex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNextIndex.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnNextIndex.Location = new System.Drawing.Point(147, 143);
            this.btnNextIndex.Margin = new System.Windows.Forms.Padding(4);
            this.btnNextIndex.Name = "btnNextIndex";
            this.btnNextIndex.Size = new System.Drawing.Size(132, 60);
            this.btnNextIndex.TabIndex = 10;
            this.btnNextIndex.Text = "Next\r\nIndex";
            this.btnNextIndex.UseVisualStyleBackColor = true;
            // 
            // AxisJogPopup
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(750, 390);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(560, 400);
            this.Name = "AxisJogPopup";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "JogPanel";
            this.rootLayout.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.grpSelectAxis.ResumeLayout(false);
            this.rightLayout.ResumeLayout(false);
            this.grpMove.ResumeLayout(false);
            this.speedLayout.ResumeLayout(false);
            this.speedLayout.PerformLayout();
            this.grpMoveMode.ResumeLayout(false);
            this.modeLayout.ResumeLayout(false);
            this.moveModeRadioLayout.ResumeLayout(false);
            this.moveModeRadioLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudStep)).EndInit();
            this.stepPresetLayout.ResumeLayout(false);
            this.jogLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }

    }
}
