namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class InputCassetteRecipePage
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel contentLayout;
        private System.Windows.Forms.TableLayoutPanel leftLayout;
        private System.Windows.Forms.TableLayoutPanel centerLayout;
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.GroupBox grpActions;
        private System.Windows.Forms.GroupBox grpIo;
        private System.Windows.Forms.GroupBox grpOptions;
        private System.Windows.Forms.GroupBox grpWait;
        private System.Windows.Forms.GroupBox grpJog;
        private System.Windows.Forms.GroupBox grpSpeed;
        private System.Windows.Forms.TableLayoutPanel actionLayout;
        private QMC.CDT_320.Ui.Controls.ActionButton btnLoadingMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnUnloadingMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnReadyMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSlotLoadingMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSlotUnloadingMove;
        private System.Windows.Forms.TableLayoutPanel ioLayout;
        private QMC.CDT_320.Ui.Controls.IndicatorDot dot8Inch;
        private QMC.CDT_320.Ui.Controls.IndicatorDot dot12Inch;
        private QMC.CDT_320.Ui.Controls.IndicatorDot dotProtrusion;
        private QMC.CDT_320.Ui.Controls.IndicatorDot dotMapping;
        private System.Windows.Forms.Label lbl8Inch;
        private System.Windows.Forms.Label lbl12Inch;
        private System.Windows.Forms.Label lblProtrusion;
        private System.Windows.Forms.Label lblMapping;
        private System.Windows.Forms.Label lblMappingBody;
        private QMC.CDT_320.Ui.Controls.ParameterGridControl optionParameterGrid;
        private System.Windows.Forms.TableLayoutPanel optionRows;
        private System.Windows.Forms.Label lblRecipeLoadingKey;
        private System.Windows.Forms.Label lblRecipeLoadingVal;
        private System.Windows.Forms.Label lblRecipeUnloadingKey;
        private System.Windows.Forms.Label lblRecipeUnloadingVal;
        private System.Windows.Forms.Label lblRecipeAvoidKey;
        private System.Windows.Forms.Label lblRecipeAvoidVal;
        private System.Windows.Forms.Label lblRecipeFirstSlotKey;
        private System.Windows.Forms.Label lblRecipeFirstSlotVal;
        private System.Windows.Forms.Label lblRecipeMappingStartKey;
        private System.Windows.Forms.Label lblRecipeMappingStartVal;
        private System.Windows.Forms.Label lblRecipeMappingEndKey;
        private System.Windows.Forms.Label lblRecipeMappingEndVal;
        private System.Windows.Forms.Label lblConfigLoadingOffsetKey;
        private System.Windows.Forms.Label lblConfigLoadingOffsetVal;
        private System.Windows.Forms.Label lblConfigUnloadingOffsetKey;
        private System.Windows.Forms.Label lblConfigUnloadingOffsetVal;
        private System.Windows.Forms.Label lblConfigSlotPitchKey;
        private System.Windows.Forms.Label lblConfigSlotPitchVal;
        private System.Windows.Forms.Label lblConfigSlotCountKey;
        private System.Windows.Forms.Label lblConfigSlotCountVal;
        private System.Windows.Forms.Label lblConfigScanVelocityKey;
        private System.Windows.Forms.Label lblConfigScanVelocityVal;
        private System.Windows.Forms.Label lblSetupToleranceKey;
        private System.Windows.Forms.Label lblSetupToleranceVal;
        private System.Windows.Forms.Label lblConfigInchKey;
        private System.Windows.Forms.Label lblConfigInchVal;
        private System.Windows.Forms.Label lblConfigLevelKey;
        private System.Windows.Forms.Label lblConfigLevelVal;
        private System.Windows.Forms.Label lblSetupSimulationKey;
        private System.Windows.Forms.Label lblSetupSimulationVal;
        private System.Windows.Forms.Label lblConfigDryRunKey;
        private System.Windows.Forms.Label lblConfigDryRunVal;
        private System.Windows.Forms.Label lblActualPositionKey;
        private System.Windows.Forms.Label lblActualPositionVal;
        private QMC.CDT_320.Ui.Controls.ParameterGridControl waitParameterGrid;
        private System.Windows.Forms.TableLayoutPanel waitRows;
        private System.Windows.Forms.Label lblWaitScanSettleKey;
        private System.Windows.Forms.Label lblWaitScanSettleVal;
        private System.Windows.Forms.Label lblWaitMoveTimeoutKey;
        private System.Windows.Forms.Label lblWaitMoveTimeoutVal;
        private System.Windows.Forms.TableLayoutPanel jogContainer;
        private System.Windows.Forms.GroupBox grpJogMove;
        private System.Windows.Forms.TableLayoutPanel jogMoveLayout;
        private System.Windows.Forms.RadioButton rdoFine;
        private System.Windows.Forms.RadioButton rdoCoarse;
        private System.Windows.Forms.GroupBox grpJogMode;
        private System.Windows.Forms.TableLayoutPanel jogModeLayout;
        private System.Windows.Forms.RadioButton rdoContinuous;
        private System.Windows.Forms.RadioButton rdoStep;
        private System.Windows.Forms.NumericUpDown numStepDistance;
        private System.Windows.Forms.Label lblStepUnit;
        private System.Windows.Forms.Button btnStep1;
        private System.Windows.Forms.Button btnStep01;
        private System.Windows.Forms.Button btnStep001;
        private System.Windows.Forms.Button btnStep0001;
        private System.Windows.Forms.Button btnStepZero;
        private System.Windows.Forms.TableLayoutPanel jogPadPanel;
        private System.Windows.Forms.Label lblJogAxisName;
        private System.Windows.Forms.Button btnJogPlus;
        private System.Windows.Forms.Button btnJogStop;
        private System.Windows.Forms.Button btnJogMinus;
        private System.Windows.Forms.TableLayoutPanel speedLayout;
        private System.Windows.Forms.TableLayoutPanel speedTrackLayout;
        private System.Windows.Forms.TrackBar trkSpeed;
        private System.Windows.Forms.Label lblSpeedValue;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.leftLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpActions = new System.Windows.Forms.GroupBox();
            this.actionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnLoadingMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnUnloadingMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnReadyMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSlotLoadingMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSlotUnloadingMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.grpIo = new System.Windows.Forms.GroupBox();
            this.ioLayout = new System.Windows.Forms.TableLayoutPanel();
            this.dot8Inch = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lbl8Inch = new System.Windows.Forms.Label();
            this.dot12Inch = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lbl12Inch = new System.Windows.Forms.Label();
            this.dotProtrusion = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblProtrusion = new System.Windows.Forms.Label();
            this.dotMapping = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblMapping = new System.Windows.Forms.Label();
            this.lblMappingBody = new System.Windows.Forms.Label();
            this.centerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpOptions = new System.Windows.Forms.GroupBox();
            this.optionParameterGrid = new QMC.CDT_320.Ui.Controls.ParameterGridControl();
            this.grpWait = new System.Windows.Forms.GroupBox();
            this.waitParameterGrid = new QMC.CDT_320.Ui.Controls.ParameterGridControl();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpJog = new System.Windows.Forms.GroupBox();
            this.jogContainer = new System.Windows.Forms.TableLayoutPanel();
            this.grpJogMove = new System.Windows.Forms.GroupBox();
            this.jogMoveLayout = new System.Windows.Forms.TableLayoutPanel();
            this.rdoFine = new System.Windows.Forms.RadioButton();
            this.rdoCoarse = new System.Windows.Forms.RadioButton();
            this.rdoCurrent = new System.Windows.Forms.RadioButton();
            this.grpJogMode = new System.Windows.Forms.GroupBox();
            this.jogModeLayout = new System.Windows.Forms.TableLayoutPanel();
            this.rdoContinuous = new System.Windows.Forms.RadioButton();
            this.rdoStep = new System.Windows.Forms.RadioButton();
            this.numStepDistance = new System.Windows.Forms.NumericUpDown();
            this.lblStepUnit = new System.Windows.Forms.Label();
            this.btnStep1 = new System.Windows.Forms.Button();
            this.btnStep01 = new System.Windows.Forms.Button();
            this.btnStep001 = new System.Windows.Forms.Button();
            this.btnStep0001 = new System.Windows.Forms.Button();
            this.btnStepZero = new System.Windows.Forms.Button();
            this.jogPadPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblJogAxisName = new System.Windows.Forms.Label();
            this.btnJogPlus = new System.Windows.Forms.Button();
            this.btnJogStop = new System.Windows.Forms.Button();
            this.btnJogMinus = new System.Windows.Forms.Button();
            this.lblActualPositionKey = new System.Windows.Forms.Label();
            this.lblActualPositionVal = new System.Windows.Forms.Label();
            this.grpSpeed = new System.Windows.Forms.GroupBox();
            this.speedLayout = new System.Windows.Forms.TableLayoutPanel();
            this.speedTrackLayout = new System.Windows.Forms.TableLayoutPanel();
            this.trkSpeed = new System.Windows.Forms.TrackBar();
            this.lblSpeedValue = new System.Windows.Forms.Label();
            this.optionRows = new System.Windows.Forms.TableLayoutPanel();
            this.lblRecipeLoadingKey = new System.Windows.Forms.Label();
            this.lblRecipeLoadingVal = new System.Windows.Forms.Label();
            this.lblRecipeUnloadingKey = new System.Windows.Forms.Label();
            this.lblRecipeUnloadingVal = new System.Windows.Forms.Label();
            this.lblRecipeAvoidKey = new System.Windows.Forms.Label();
            this.lblRecipeAvoidVal = new System.Windows.Forms.Label();
            this.lblRecipeFirstSlotKey = new System.Windows.Forms.Label();
            this.lblRecipeFirstSlotVal = new System.Windows.Forms.Label();
            this.lblRecipeMappingStartKey = new System.Windows.Forms.Label();
            this.lblRecipeMappingStartVal = new System.Windows.Forms.Label();
            this.lblRecipeMappingEndKey = new System.Windows.Forms.Label();
            this.lblRecipeMappingEndVal = new System.Windows.Forms.Label();
            this.lblConfigLoadingOffsetKey = new System.Windows.Forms.Label();
            this.lblConfigLoadingOffsetVal = new System.Windows.Forms.Label();
            this.lblConfigUnloadingOffsetKey = new System.Windows.Forms.Label();
            this.lblConfigUnloadingOffsetVal = new System.Windows.Forms.Label();
            this.lblConfigSlotPitchKey = new System.Windows.Forms.Label();
            this.lblConfigSlotPitchVal = new System.Windows.Forms.Label();
            this.lblConfigSlotCountKey = new System.Windows.Forms.Label();
            this.lblConfigSlotCountVal = new System.Windows.Forms.Label();
            this.lblConfigScanVelocityKey = new System.Windows.Forms.Label();
            this.lblConfigScanVelocityVal = new System.Windows.Forms.Label();
            this.lblSetupToleranceKey = new System.Windows.Forms.Label();
            this.lblSetupToleranceVal = new System.Windows.Forms.Label();
            this.lblConfigInchKey = new System.Windows.Forms.Label();
            this.lblConfigInchVal = new System.Windows.Forms.Label();
            this.lblConfigLevelKey = new System.Windows.Forms.Label();
            this.lblConfigLevelVal = new System.Windows.Forms.Label();
            this.lblSetupSimulationKey = new System.Windows.Forms.Label();
            this.lblSetupSimulationVal = new System.Windows.Forms.Label();
            this.lblConfigDryRunKey = new System.Windows.Forms.Label();
            this.lblConfigDryRunVal = new System.Windows.Forms.Label();
            this.waitRows = new System.Windows.Forms.TableLayoutPanel();
            this.lblWaitScanSettleKey = new System.Windows.Forms.Label();
            this.lblWaitScanSettleVal = new System.Windows.Forms.Label();
            this.lblWaitMoveTimeoutKey = new System.Windows.Forms.Label();
            this.lblWaitMoveTimeoutVal = new System.Windows.Forms.Label();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.grpActions.SuspendLayout();
            this.actionLayout.SuspendLayout();
            this.grpIo.SuspendLayout();
            this.ioLayout.SuspendLayout();
            this.centerLayout.SuspendLayout();
            this.grpOptions.SuspendLayout();
            this.grpWait.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.grpJog.SuspendLayout();
            this.jogContainer.SuspendLayout();
            this.grpJogMove.SuspendLayout();
            this.jogMoveLayout.SuspendLayout();
            this.grpJogMode.SuspendLayout();
            this.jogModeLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numStepDistance)).BeginInit();
            this.jogPadPanel.SuspendLayout();
            this.grpSpeed.SuspendLayout();
            this.speedLayout.SuspendLayout();
            this.speedTrackLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkSpeed)).BeginInit();
            this.optionRows.SuspendLayout();
            this.waitRows.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 1);
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1500, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1500, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:recipe.inputCassette";
            this.lblHeader.Text = "INPUT CASSETTE";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(219)))), ((int)(((byte)(222)))));
            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 440F));
            this.contentLayout.Controls.Add(this.leftLayout, 0, 0);
            this.contentLayout.Controls.Add(this.centerLayout, 1, 0);
            this.contentLayout.Controls.Add(this.rightLayout, 2, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(0, 30);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(1);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1500, 870);
            this.contentLayout.TabIndex = 1;
            // 
            // leftLayout
            // 
            this.leftLayout.ColumnCount = 1;
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Controls.Add(this.grpActions, 0, 0);
            this.leftLayout.Controls.Add(this.grpIo, 0, 1);
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Location = new System.Drawing.Point(1, 1);
            this.leftLayout.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.RowCount = 2;
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 430F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Size = new System.Drawing.Size(214, 868);
            this.leftLayout.TabIndex = 0;
            // 
            // grpActions
            // 
            this.grpActions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpActions.Controls.Add(this.actionLayout);
            this.grpActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpActions.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpActions.Location = new System.Drawing.Point(0, 0);
            this.grpActions.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpActions.Name = "grpActions";
            this.grpActions.Size = new System.Drawing.Size(214, 424);
            this.grpActions.TabIndex = 0;
            this.grpActions.TabStop = false;
            this.grpActions.Text = "ACTION";
            // 
            // actionLayout
            // 
            this.actionLayout.ColumnCount = 1;
            this.actionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionLayout.Controls.Add(this.btnLoadingMove, 0, 0);
            this.actionLayout.Controls.Add(this.btnUnloadingMove, 0, 1);
            this.actionLayout.Controls.Add(this.btnReadyMove, 0, 2);
            this.actionLayout.Controls.Add(this.btnSlotLoadingMove, 0, 3);
            this.actionLayout.Controls.Add(this.btnSlotUnloadingMove, 0, 4);
            this.actionLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.actionLayout.Location = new System.Drawing.Point(3, 26);
            this.actionLayout.Name = "actionLayout";
            this.actionLayout.Padding = new System.Windows.Forms.Padding(12, 18, 12, 0);
            this.actionLayout.RowCount = 6;
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 76F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 76F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 76F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 76F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 76F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionLayout.Size = new System.Drawing.Size(208, 398);
            this.actionLayout.TabIndex = 0;
            // 
            // btnLoadingMove
            // 
            this.btnLoadingMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnLoadingMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLoadingMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoadingMove.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.btnLoadingMove.ForeColor = System.Drawing.Color.White;
            this.btnLoadingMove.Location = new System.Drawing.Point(12, 18);
            this.btnLoadingMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.btnLoadingMove.Name = "btnLoadingMove";
            this.btnLoadingMove.Size = new System.Drawing.Size(184, 66);
            this.btnLoadingMove.TabIndex = 0;
            this.btnLoadingMove.Text = "ACTION\r\n\r\nLOADING MOVE";
            this.btnLoadingMove.Click += new System.EventHandler(this.btnLoadingMove_Click);
            // 
            // btnUnloadingMove
            // 
            this.btnUnloadingMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnUnloadingMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUnloadingMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUnloadingMove.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.btnUnloadingMove.ForeColor = System.Drawing.Color.White;
            this.btnUnloadingMove.Location = new System.Drawing.Point(12, 94);
            this.btnUnloadingMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.btnUnloadingMove.Name = "btnUnloadingMove";
            this.btnUnloadingMove.Size = new System.Drawing.Size(184, 66);
            this.btnUnloadingMove.TabIndex = 1;
            this.btnUnloadingMove.Text = "ACTION\r\n\r\nUNLOADING MOVE";
            this.btnUnloadingMove.Click += new System.EventHandler(this.btnUnloadingMove_Click);
            // 
            // btnReadyMove
            // 
            this.btnReadyMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnReadyMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReadyMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReadyMove.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.btnReadyMove.ForeColor = System.Drawing.Color.White;
            this.btnReadyMove.Location = new System.Drawing.Point(12, 170);
            this.btnReadyMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.btnReadyMove.Name = "btnReadyMove";
            this.btnReadyMove.Size = new System.Drawing.Size(184, 66);
            this.btnReadyMove.TabIndex = 2;
            this.btnReadyMove.Text = "ACTION\r\n\r\nREADY MOVE";
            this.btnReadyMove.Click += new System.EventHandler(this.btnReadyMove_Click);
            // 
            // btnSlotLoadingMove
            // 
            this.btnSlotLoadingMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSlotLoadingMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSlotLoadingMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSlotLoadingMove.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.btnSlotLoadingMove.ForeColor = System.Drawing.Color.White;
            this.btnSlotLoadingMove.Location = new System.Drawing.Point(12, 246);
            this.btnSlotLoadingMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.btnSlotLoadingMove.Name = "btnSlotLoadingMove";
            this.btnSlotLoadingMove.Size = new System.Drawing.Size(184, 66);
            this.btnSlotLoadingMove.TabIndex = 3;
            this.btnSlotLoadingMove.Text = "ACTION\r\n\r\nSLOT MOVE (LOADING)";
            this.btnSlotLoadingMove.Click += new System.EventHandler(this.btnSlotLoadingMove_Click);
            // 
            // btnSlotUnloadingMove
            // 
            this.btnSlotUnloadingMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSlotUnloadingMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSlotUnloadingMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSlotUnloadingMove.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.btnSlotUnloadingMove.ForeColor = System.Drawing.Color.White;
            this.btnSlotUnloadingMove.Location = new System.Drawing.Point(12, 322);
            this.btnSlotUnloadingMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.btnSlotUnloadingMove.Name = "btnSlotUnloadingMove";
            this.btnSlotUnloadingMove.Size = new System.Drawing.Size(184, 66);
            this.btnSlotUnloadingMove.TabIndex = 4;
            this.btnSlotUnloadingMove.Text = "ACTION\r\n\r\nSLOT MOVE (UNLOADING)";
            this.btnSlotUnloadingMove.Click += new System.EventHandler(this.btnSlotUnloadingMove_Click);
            // 
            // grpIo
            // 
            this.grpIo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpIo.Controls.Add(this.ioLayout);
            this.grpIo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpIo.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpIo.Location = new System.Drawing.Point(0, 430);
            this.grpIo.Margin = new System.Windows.Forms.Padding(0);
            this.grpIo.Name = "grpIo";
            this.grpIo.Size = new System.Drawing.Size(214, 438);
            this.grpIo.TabIndex = 1;
            this.grpIo.TabStop = false;
            this.grpIo.Text = "CYLINDER && I/O";
            // 
            // ioLayout
            // 
            this.ioLayout.ColumnCount = 2;
            this.ioLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.ioLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ioLayout.Controls.Add(this.dot8Inch, 0, 0);
            this.ioLayout.Controls.Add(this.lbl8Inch, 1, 0);
            this.ioLayout.Controls.Add(this.dot12Inch, 0, 1);
            this.ioLayout.Controls.Add(this.lbl12Inch, 1, 1);
            this.ioLayout.Controls.Add(this.dotProtrusion, 0, 2);
            this.ioLayout.Controls.Add(this.lblProtrusion, 1, 2);
            this.ioLayout.Controls.Add(this.dotMapping, 0, 3);
            this.ioLayout.Controls.Add(this.lblMapping, 1, 3);
            this.ioLayout.Controls.Add(this.lblMappingBody, 1, 4);
            this.ioLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ioLayout.Location = new System.Drawing.Point(3, 26);
            this.ioLayout.Name = "ioLayout";
            this.ioLayout.Padding = new System.Windows.Forms.Padding(12, 22, 12, 12);
            this.ioLayout.RowCount = 6;
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 230F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ioLayout.Size = new System.Drawing.Size(208, 409);
            this.ioLayout.TabIndex = 0;
            // 
            // dot8Inch
            // 
            this.dot8Inch.BackColor = System.Drawing.Color.Transparent;
            this.dot8Inch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dot8Inch.Location = new System.Drawing.Point(20, 30);
            this.dot8Inch.Margin = new System.Windows.Forms.Padding(8);
            this.dot8Inch.Name = "dot8Inch";
            this.dot8Inch.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dot8Inch.OnColor = System.Drawing.Color.LimeGreen;
            this.dot8Inch.Size = new System.Drawing.Size(16, 14);
            this.dot8Inch.TabIndex = 0;
            // 
            // lbl8Inch
            // 
            this.lbl8Inch.Location = new System.Drawing.Point(47, 22);
            this.lbl8Inch.Name = "lbl8Inch";
            this.lbl8Inch.Size = new System.Drawing.Size(100, 23);
            this.lbl8Inch.TabIndex = 1;
            this.lbl8Inch.Text = "8 INCH CASSETTE";
            // 
            // dot12Inch
            // 
            this.dot12Inch.BackColor = System.Drawing.Color.Transparent;
            this.dot12Inch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dot12Inch.Location = new System.Drawing.Point(20, 60);
            this.dot12Inch.Margin = new System.Windows.Forms.Padding(8);
            this.dot12Inch.Name = "dot12Inch";
            this.dot12Inch.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dot12Inch.OnColor = System.Drawing.Color.LimeGreen;
            this.dot12Inch.Size = new System.Drawing.Size(16, 14);
            this.dot12Inch.TabIndex = 2;
            // 
            // lbl12Inch
            // 
            this.lbl12Inch.Location = new System.Drawing.Point(47, 52);
            this.lbl12Inch.Name = "lbl12Inch";
            this.lbl12Inch.Size = new System.Drawing.Size(100, 23);
            this.lbl12Inch.TabIndex = 3;
            this.lbl12Inch.Text = "12 INCH CASSETTE";
            // 
            // dotProtrusion
            // 
            this.dotProtrusion.BackColor = System.Drawing.Color.Transparent;
            this.dotProtrusion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotProtrusion.Location = new System.Drawing.Point(20, 90);
            this.dotProtrusion.Margin = new System.Windows.Forms.Padding(8);
            this.dotProtrusion.Name = "dotProtrusion";
            this.dotProtrusion.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotProtrusion.OnColor = System.Drawing.Color.LimeGreen;
            this.dotProtrusion.Size = new System.Drawing.Size(16, 14);
            this.dotProtrusion.TabIndex = 4;
            // 
            // lblProtrusion
            // 
            this.lblProtrusion.Location = new System.Drawing.Point(47, 82);
            this.lblProtrusion.Name = "lblProtrusion";
            this.lblProtrusion.Size = new System.Drawing.Size(100, 23);
            this.lblProtrusion.TabIndex = 5;
            this.lblProtrusion.Text = "WAFER PROTRUSION";
            // 
            // dotMapping
            // 
            this.dotMapping.BackColor = System.Drawing.Color.Transparent;
            this.dotMapping.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotMapping.Location = new System.Drawing.Point(20, 120);
            this.dotMapping.Margin = new System.Windows.Forms.Padding(8);
            this.dotMapping.Name = "dotMapping";
            this.dotMapping.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotMapping.OnColor = System.Drawing.Color.LimeGreen;
            this.dotMapping.Size = new System.Drawing.Size(16, 14);
            this.dotMapping.TabIndex = 6;
            // 
            // lblMapping
            // 
            this.lblMapping.Location = new System.Drawing.Point(47, 112);
            this.lblMapping.Name = "lblMapping";
            this.lblMapping.Size = new System.Drawing.Size(100, 23);
            this.lblMapping.TabIndex = 7;
            this.lblMapping.Text = "WAFER MAPPING";
            // 
            // lblMappingBody
            // 
            this.lblMappingBody.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(205)))), ((int)(((byte)(205)))), ((int)(((byte)(205)))));
            this.lblMappingBody.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMappingBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMappingBody.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblMappingBody.Location = new System.Drawing.Point(44, 142);
            this.lblMappingBody.Margin = new System.Windows.Forms.Padding(0);
            this.lblMappingBody.Name = "lblMappingBody";
            this.lblMappingBody.Size = new System.Drawing.Size(152, 230);
            this.lblMappingBody.TabIndex = 8;
            this.lblMappingBody.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // centerLayout
            // 
            this.centerLayout.ColumnCount = 1;
            this.centerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.centerLayout.Controls.Add(this.grpOptions, 0, 0);
            this.centerLayout.Controls.Add(this.grpWait, 0, 1);
            this.centerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.centerLayout.Location = new System.Drawing.Point(221, 1);
            this.centerLayout.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.centerLayout.Name = "centerLayout";
            this.centerLayout.RowCount = 2;
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 610F));
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.centerLayout.Size = new System.Drawing.Size(832, 868);
            this.centerLayout.TabIndex = 1;
            // 
            // grpOptions
            // 
            this.grpOptions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpOptions.Controls.Add(this.optionParameterGrid);
            this.grpOptions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpOptions.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpOptions.Location = new System.Drawing.Point(0, 0);
            this.grpOptions.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpOptions.Name = "grpOptions";
            this.grpOptions.Size = new System.Drawing.Size(832, 604);
            this.grpOptions.TabIndex = 0;
            this.grpOptions.TabStop = false;
            this.grpOptions.Text = "OPTION";
            // 
            // optionParameterGrid
            // 
            this.optionParameterGrid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.optionParameterGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.optionParameterGrid.Location = new System.Drawing.Point(3, 26);
            this.optionParameterGrid.Margin = new System.Windows.Forms.Padding(0);
            this.optionParameterGrid.Name = "optionParameterGrid";
            this.optionParameterGrid.Size = new System.Drawing.Size(826, 575);
            this.optionParameterGrid.TabIndex = 0;
            // 
            // grpWait
            // 
            this.grpWait.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpWait.Controls.Add(this.waitParameterGrid);
            this.grpWait.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpWait.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpWait.Location = new System.Drawing.Point(0, 610);
            this.grpWait.Margin = new System.Windows.Forms.Padding(0);
            this.grpWait.Name = "grpWait";
            this.grpWait.Size = new System.Drawing.Size(832, 258);
            this.grpWait.TabIndex = 1;
            this.grpWait.TabStop = false;
            this.grpWait.Text = "WAIT TIME";
            // 
            // waitParameterGrid
            // 
            this.waitParameterGrid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.waitParameterGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waitParameterGrid.Location = new System.Drawing.Point(3, 26);
            this.waitParameterGrid.Margin = new System.Windows.Forms.Padding(0);
            this.waitParameterGrid.Name = "waitParameterGrid";
            this.waitParameterGrid.Size = new System.Drawing.Size(826, 229);
            this.waitParameterGrid.TabIndex = 0;
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.grpJog, 0, 0);
            this.rightLayout.Controls.Add(this.grpSpeed, 0, 1);
            this.rightLayout.Location = new System.Drawing.Point(1059, 1);
            this.rightLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 2;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 575F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Size = new System.Drawing.Size(440, 852);
            this.rightLayout.TabIndex = 2;
            // 
            // grpJog
            // 
            this.grpJog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpJog.Controls.Add(this.jogContainer);
            this.grpJog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpJog.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpJog.Location = new System.Drawing.Point(0, 0);
            this.grpJog.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpJog.Name = "grpJog";
            this.grpJog.Size = new System.Drawing.Size(440, 569);
            this.grpJog.TabIndex = 0;
            this.grpJog.TabStop = false;
            this.grpJog.Text = "JOG";
            // 
            // jogContainer
            // 
            this.jogContainer.ColumnCount = 1;
            this.jogContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.jogContainer.Controls.Add(this.grpJogMove, 0, 0);
            this.jogContainer.Controls.Add(this.grpJogMode, 0, 1);
            this.jogContainer.Controls.Add(this.jogPadPanel, 0, 2);
            this.jogContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogContainer.Location = new System.Drawing.Point(3, 26);
            this.jogContainer.Name = "jogContainer";
            this.jogContainer.Padding = new System.Windows.Forms.Padding(3);
            this.jogContainer.RowCount = 4;
            this.jogContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 92F));
            this.jogContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.jogContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 284F));
            this.jogContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.jogContainer.Size = new System.Drawing.Size(434, 540);
            this.jogContainer.TabIndex = 0;
            // 
            // grpJogMove
            // 
            this.grpJogMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(207)))), ((int)(((byte)(211)))), ((int)(((byte)(216)))));
            this.grpJogMove.Controls.Add(this.jogMoveLayout);
            this.grpJogMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpJogMove.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpJogMove.Location = new System.Drawing.Point(3, 3);
            this.grpJogMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpJogMove.Name = "grpJogMove";
            this.grpJogMove.Size = new System.Drawing.Size(428, 84);
            this.grpJogMove.TabIndex = 0;
            this.grpJogMove.TabStop = false;
            this.grpJogMove.Text = "Speed Mode";
            // 
            // jogMoveLayout
            // 
            this.jogMoveLayout.ColumnCount = 4;
            this.jogMoveLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.jogMoveLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 44F));
            this.jogMoveLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 56F));
            this.jogMoveLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 138F));
            this.jogMoveLayout.Controls.Add(this.rdoFine, 1, 1);
            this.jogMoveLayout.Controls.Add(this.rdoCoarse, 2, 1);
            this.jogMoveLayout.Controls.Add(this.rdoCurrent, 3, 1);
            this.jogMoveLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogMoveLayout.Location = new System.Drawing.Point(3, 26);
            this.jogMoveLayout.Margin = new System.Windows.Forms.Padding(1);
            this.jogMoveLayout.Name = "jogMoveLayout";
            this.jogMoveLayout.RowCount = 3;
            this.jogMoveLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.jogMoveLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.jogMoveLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.jogMoveLayout.Size = new System.Drawing.Size(422, 55);
            this.jogMoveLayout.TabIndex = 0;
            // 
            // rdoFine
            // 
            this.rdoFine.AutoSize = true;
            this.rdoFine.Checked = true;
            this.rdoFine.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoFine.Location = new System.Drawing.Point(25, 8);
            this.rdoFine.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.rdoFine.Name = "rdoFine";
            this.rdoFine.Size = new System.Drawing.Size(109, 38);
            this.rdoFine.TabIndex = 0;
            this.rdoFine.TabStop = true;
            this.rdoFine.Text = "Fine";
            this.rdoFine.UseVisualStyleBackColor = true;
            // 
            // rdoCoarse
            // 
            this.rdoCoarse.AutoSize = true;
            this.rdoCoarse.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoCoarse.Location = new System.Drawing.Point(140, 8);
            this.rdoCoarse.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.rdoCoarse.Name = "rdoCoarse";
            this.rdoCoarse.Size = new System.Drawing.Size(140, 38);
            this.rdoCoarse.TabIndex = 1;
            this.rdoCoarse.Text = "Coarse";
            this.rdoCoarse.UseVisualStyleBackColor = true;
            // 
            // rdoCurrent
            // 
            this.rdoCurrent.AutoSize = true;
            this.rdoCurrent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoCurrent.Location = new System.Drawing.Point(286, 8);
            this.rdoCurrent.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.rdoCurrent.Name = "rdoCurrent";
            this.rdoCurrent.Size = new System.Drawing.Size(133, 38);
            this.rdoCurrent.TabIndex = 2;
            this.rdoCurrent.Text = "Current";
            this.rdoCurrent.UseVisualStyleBackColor = true;
            // 
            // grpJogMode
            // 
            this.grpJogMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(207)))), ((int)(((byte)(211)))), ((int)(((byte)(216)))));
            this.grpJogMode.Controls.Add(this.jogModeLayout);
            this.grpJogMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpJogMode.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpJogMode.Location = new System.Drawing.Point(3, 95);
            this.grpJogMode.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpJogMode.Name = "grpJogMode";
            this.grpJogMode.Size = new System.Drawing.Size(428, 142);
            this.grpJogMode.TabIndex = 1;
            this.grpJogMode.TabStop = false;
            this.grpJogMode.Text = "Move Mode";
            // 
            // jogModeLayout
            // 
            this.jogModeLayout.ColumnCount = 7;
            this.jogModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.jogModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 22F));
            this.jogModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.jogModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.jogModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.jogModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 24F));
            this.jogModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.jogModeLayout.Controls.Add(this.rdoContinuous, 1, 0);
            this.jogModeLayout.Controls.Add(this.rdoStep, 3, 0);
            this.jogModeLayout.Controls.Add(this.numStepDistance, 1, 1);
            this.jogModeLayout.Controls.Add(this.lblStepUnit, 3, 1);
            this.jogModeLayout.Controls.Add(this.btnStep1, 1, 2);
            this.jogModeLayout.Controls.Add(this.btnStep01, 2, 2);
            this.jogModeLayout.Controls.Add(this.btnStep001, 3, 2);
            this.jogModeLayout.Controls.Add(this.btnStep0001, 4, 2);
            this.jogModeLayout.Controls.Add(this.btnStepZero, 5, 2);
            this.jogModeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogModeLayout.Location = new System.Drawing.Point(3, 26);
            this.jogModeLayout.Margin = new System.Windows.Forms.Padding(0);
            this.jogModeLayout.Name = "jogModeLayout";
            this.jogModeLayout.RowCount = 4;
            this.jogModeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.jogModeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.jogModeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.jogModeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.jogModeLayout.Size = new System.Drawing.Size(422, 113);
            this.jogModeLayout.TabIndex = 0;
            // 
            // rdoContinuous
            // 
            this.rdoContinuous.AutoSize = true;
            this.jogModeLayout.SetColumnSpan(this.rdoContinuous, 2);
            this.rdoContinuous.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoContinuous.Location = new System.Drawing.Point(25, 0);
            this.rdoContinuous.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.rdoContinuous.Name = "rdoContinuous";
            this.rdoContinuous.Size = new System.Drawing.Size(145, 32);
            this.rdoContinuous.TabIndex = 0;
            this.rdoContinuous.Text = "Continuous";
            this.rdoContinuous.UseVisualStyleBackColor = true;
            // 
            // rdoStep
            // 
            this.rdoStep.AutoSize = true;
            this.rdoStep.Checked = true;
            this.jogModeLayout.SetColumnSpan(this.rdoStep, 2);
            this.rdoStep.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoStep.Location = new System.Drawing.Point(176, 0);
            this.rdoStep.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.rdoStep.Name = "rdoStep";
            this.rdoStep.Size = new System.Drawing.Size(130, 32);
            this.rdoStep.TabIndex = 1;
            this.rdoStep.TabStop = true;
            this.rdoStep.Text = "Step";
            this.rdoStep.UseVisualStyleBackColor = true;
            // 
            // numStepDistance
            // 
            this.jogModeLayout.SetColumnSpan(this.numStepDistance, 2);
            this.numStepDistance.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numStepDistance.Location = new System.Drawing.Point(25, 36);
            this.numStepDistance.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.numStepDistance.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.numStepDistance.Name = "numStepDistance";
            this.numStepDistance.Size = new System.Drawing.Size(145, 30);
            this.numStepDistance.TabIndex = 2;
            this.numStepDistance.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // lblStepUnit
            // 
            this.lblStepUnit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStepUnit.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblStepUnit.Location = new System.Drawing.Point(176, 32);
            this.lblStepUnit.Name = "lblStepUnit";
            this.lblStepUnit.Size = new System.Drawing.Size(62, 34);
            this.lblStepUnit.TabIndex = 8;
            this.lblStepUnit.Text = "um";
            this.lblStepUnit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnStep1
            // 
            this.btnStep1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStep1.Location = new System.Drawing.Point(25, 70);
            this.btnStep1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 2);
            this.btnStep1.Name = "btnStep1";
            this.btnStep1.Size = new System.Drawing.Size(77, 30);
            this.btnStep1.TabIndex = 3;
            this.btnStep1.Text = "1000";
            this.btnStep1.Click += new System.EventHandler(this.btnStep1_Click);
            // 
            // btnStep01
            // 
            this.btnStep01.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStep01.Location = new System.Drawing.Point(108, 70);
            this.btnStep01.Margin = new System.Windows.Forms.Padding(3, 4, 3, 2);
            this.btnStep01.Name = "btnStep01";
            this.btnStep01.Size = new System.Drawing.Size(62, 30);
            this.btnStep01.TabIndex = 4;
            this.btnStep01.Text = "100";
            this.btnStep01.Click += new System.EventHandler(this.btnStep01_Click);
            // 
            // btnStep001
            // 
            this.btnStep001.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStep001.Location = new System.Drawing.Point(176, 70);
            this.btnStep001.Margin = new System.Windows.Forms.Padding(3, 4, 3, 2);
            this.btnStep001.Name = "btnStep001";
            this.btnStep001.Size = new System.Drawing.Size(62, 30);
            this.btnStep001.TabIndex = 5;
            this.btnStep001.Text = "10";
            this.btnStep001.Click += new System.EventHandler(this.btnStep001_Click);
            // 
            // btnStep0001
            // 
            this.btnStep0001.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStep0001.Location = new System.Drawing.Point(244, 70);
            this.btnStep0001.Margin = new System.Windows.Forms.Padding(3, 4, 3, 2);
            this.btnStep0001.Name = "btnStep0001";
            this.btnStep0001.Size = new System.Drawing.Size(62, 30);
            this.btnStep0001.TabIndex = 6;
            this.btnStep0001.Text = "1";
            this.btnStep0001.Click += new System.EventHandler(this.btnStep0001_Click);
            // 
            // btnStepZero
            // 
            this.btnStepZero.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStepZero.Location = new System.Drawing.Point(312, 70);
            this.btnStepZero.Margin = new System.Windows.Forms.Padding(3, 4, 3, 2);
            this.btnStepZero.Name = "btnStepZero";
            this.btnStepZero.Size = new System.Drawing.Size(84, 30);
            this.btnStepZero.TabIndex = 7;
            this.btnStepZero.Text = "0\'";
            this.btnStepZero.Click += new System.EventHandler(this.btnStepZero_Click);
            // 
            // jogPadPanel
            // 
            this.jogPadPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(207)))), ((int)(((byte)(211)))), ((int)(((byte)(216)))));
            this.jogPadPanel.ColumnCount = 3;
            this.jogPadPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.jogPadPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 32F));
            this.jogPadPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.jogPadPanel.Controls.Add(this.lblJogAxisName, 1, 0);
            this.jogPadPanel.Controls.Add(this.btnJogPlus, 1, 1);
            this.jogPadPanel.Controls.Add(this.btnJogStop, 1, 2);
            this.jogPadPanel.Controls.Add(this.btnJogMinus, 1, 3);
            this.jogPadPanel.Controls.Add(this.lblActualPositionKey, 0, 4);
            this.jogPadPanel.Controls.Add(this.lblActualPositionVal, 1, 4);
            this.jogPadPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogPadPanel.Location = new System.Drawing.Point(3, 245);
            this.jogPadPanel.Margin = new System.Windows.Forms.Padding(0);
            this.jogPadPanel.Name = "jogPadPanel";
            this.jogPadPanel.Padding = new System.Windows.Forms.Padding(3);
            this.jogPadPanel.RowCount = 5;
            this.jogPadPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.jogPadPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.jogPadPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.jogPadPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.jogPadPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.jogPadPanel.Size = new System.Drawing.Size(428, 284);
            this.jogPadPanel.TabIndex = 2;
            // 
            // lblJogAxisName
            // 
            this.lblJogAxisName.BackColor = System.Drawing.Color.White;
            this.lblJogAxisName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblJogAxisName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblJogAxisName.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblJogAxisName.Location = new System.Drawing.Point(149, 3);
            this.lblJogAxisName.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this.lblJogAxisName.Name = "lblJogAxisName";
            this.lblJogAxisName.Size = new System.Drawing.Size(129, 28);
            this.lblJogAxisName.TabIndex = 0;
            this.lblJogAxisName.Text = "AXIS Z";
            this.lblJogAxisName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnJogPlus
            // 
            this.btnJogPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogPlus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnJogPlus.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.btnJogPlus.Location = new System.Drawing.Point(149, 37);
            this.btnJogPlus.Margin = new System.Windows.Forms.Padding(3, 0, 3, 8);
            this.btnJogPlus.Name = "btnJogPlus";
            this.btnJogPlus.Size = new System.Drawing.Size(129, 58);
            this.btnJogPlus.TabIndex = 1;
            this.btnJogPlus.Text = "Z+";
            this.btnJogPlus.UseVisualStyleBackColor = false;
            this.btnJogPlus.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnJogPlus_MouseDown);
            this.btnJogPlus.MouseLeave += new System.EventHandler(this.btnJog_MouseLeave);
            this.btnJogPlus.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnJog_MouseUp);
            // 
            // btnJogStop
            // 
            this.btnJogStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnJogStop.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnJogStop.Location = new System.Drawing.Point(149, 103);
            this.btnJogStop.Margin = new System.Windows.Forms.Padding(3, 0, 3, 8);
            this.btnJogStop.Name = "btnJogStop";
            this.btnJogStop.Size = new System.Drawing.Size(129, 58);
            this.btnJogStop.TabIndex = 2;
            this.btnJogStop.Text = "STOP";
            this.btnJogStop.Click += new System.EventHandler(this.btnJogStop_Click);
            // 
            // btnJogMinus
            // 
            this.btnJogMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogMinus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnJogMinus.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.btnJogMinus.Location = new System.Drawing.Point(149, 169);
            this.btnJogMinus.Margin = new System.Windows.Forms.Padding(3, 0, 3, 8);
            this.btnJogMinus.Name = "btnJogMinus";
            this.btnJogMinus.Size = new System.Drawing.Size(129, 58);
            this.btnJogMinus.TabIndex = 3;
            this.btnJogMinus.Text = "Z-";
            this.btnJogMinus.UseVisualStyleBackColor = false;
            this.btnJogMinus.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnJogMinus_MouseDown);
            this.btnJogMinus.MouseLeave += new System.EventHandler(this.btnJog_MouseLeave);
            this.btnJogMinus.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnJog_MouseUp);
            // 
            // lblActualPositionKey
            // 
            this.lblActualPositionKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblActualPositionKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblActualPositionKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblActualPositionKey.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblActualPositionKey.Location = new System.Drawing.Point(6, 235);
            this.lblActualPositionKey.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.lblActualPositionKey.Name = "lblActualPositionKey";
            this.lblActualPositionKey.Size = new System.Drawing.Size(140, 46);
            this.lblActualPositionKey.TabIndex = 32;
            this.lblActualPositionKey.Text = "CURRENT POS";
            this.lblActualPositionKey.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblActualPositionVal
            // 
            this.lblActualPositionVal.BackColor = System.Drawing.Color.White;
            this.lblActualPositionVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.jogPadPanel.SetColumnSpan(this.lblActualPositionVal, 2);
            this.lblActualPositionVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblActualPositionVal.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblActualPositionVal.Location = new System.Drawing.Point(146, 235);
            this.lblActualPositionVal.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.lblActualPositionVal.Name = "lblActualPositionVal";
            this.lblActualPositionVal.Size = new System.Drawing.Size(276, 46);
            this.lblActualPositionVal.TabIndex = 33;
            this.lblActualPositionVal.Text = "0 um";
            this.lblActualPositionVal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // grpSpeed
            // 
            this.grpSpeed.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpSpeed.Controls.Add(this.speedLayout);
            this.grpSpeed.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpSpeed.Location = new System.Drawing.Point(0, 575);
            this.grpSpeed.Margin = new System.Windows.Forms.Padding(0);
            this.grpSpeed.Name = "grpSpeed";
            this.grpSpeed.Size = new System.Drawing.Size(440, 277);
            this.grpSpeed.TabIndex = 1;
            this.grpSpeed.TabStop = false;
            this.grpSpeed.Text = "SPEED";
            // 
            // speedLayout
            // 
            this.speedLayout.ColumnCount = 1;
            this.speedLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.speedLayout.Controls.Add(this.speedTrackLayout, 0, 0);
            this.speedLayout.Controls.Add(this.lblSpeedValue, 0, 1);
            this.speedLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.speedLayout.Location = new System.Drawing.Point(3, 26);
            this.speedLayout.Name = "speedLayout";
            this.speedLayout.Padding = new System.Windows.Forms.Padding(3);
            this.speedLayout.RowCount = 2;
            this.speedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 118F));
            this.speedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.speedLayout.Size = new System.Drawing.Size(434, 248);
            this.speedLayout.TabIndex = 0;
            // 
            // speedTrackLayout
            // 
            this.speedTrackLayout.ColumnCount = 1;
            this.speedTrackLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.speedTrackLayout.Controls.Add(this.trkSpeed, 0, 1);
            this.speedTrackLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.speedTrackLayout.Location = new System.Drawing.Point(6, 6);
            this.speedTrackLayout.Name = "speedTrackLayout";
            this.speedTrackLayout.RowCount = 3;
            this.speedTrackLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.speedTrackLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.speedTrackLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.speedTrackLayout.Size = new System.Drawing.Size(422, 112);
            this.speedTrackLayout.TabIndex = 0;
            // 
            // trkSpeed
            // 
            this.trkSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.trkSpeed.Location = new System.Drawing.Point(3, 30);
            this.trkSpeed.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.trkSpeed.Maximum = 100;
            this.trkSpeed.Minimum = 1;
            this.trkSpeed.Name = "trkSpeed";
            this.trkSpeed.Size = new System.Drawing.Size(416, 44);
            this.trkSpeed.TabIndex = 0;
            this.trkSpeed.TickFrequency = 10;
            this.trkSpeed.Value = 58;
            this.trkSpeed.ValueChanged += new System.EventHandler(this.trkSpeed_ValueChanged);
            //
            // lblSpeedValue
            //
            this.lblSpeedValue.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(205)))), ((int)(((byte)(205)))), ((int)(((byte)(205)))));
            this.lblSpeedValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSpeedValue.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSpeedValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblSpeedValue.Location = new System.Drawing.Point(6, 121);
            this.lblSpeedValue.Name = "lblSpeedValue";
            this.lblSpeedValue.Size = new System.Drawing.Size(422, 40);
            this.lblSpeedValue.TabIndex = 1;
            this.lblSpeedValue.Text = "58%";
            this.lblSpeedValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // optionRows
            //
            this.optionRows.ColumnCount = 2;
            this.optionRows.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 290F));
            this.optionRows.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.optionRows.Controls.Add(this.lblRecipeLoadingKey, 0, 0);
            this.optionRows.Controls.Add(this.lblRecipeLoadingVal, 1, 0);
            this.optionRows.Controls.Add(this.lblRecipeUnloadingKey, 0, 1);
            this.optionRows.Controls.Add(this.lblRecipeUnloadingVal, 1, 1);
            this.optionRows.Controls.Add(this.lblRecipeAvoidKey, 0, 2);
            this.optionRows.Controls.Add(this.lblRecipeAvoidVal, 1, 2);
            this.optionRows.Controls.Add(this.lblRecipeFirstSlotKey, 0, 3);
            this.optionRows.Controls.Add(this.lblRecipeFirstSlotVal, 1, 3);
            this.optionRows.Controls.Add(this.lblRecipeMappingStartKey, 0, 4);
            this.optionRows.Controls.Add(this.lblRecipeMappingStartVal, 1, 4);
            this.optionRows.Controls.Add(this.lblRecipeMappingEndKey, 0, 5);
            this.optionRows.Controls.Add(this.lblRecipeMappingEndVal, 1, 5);
            this.optionRows.Controls.Add(this.lblConfigLoadingOffsetKey, 0, 6);
            this.optionRows.Controls.Add(this.lblConfigLoadingOffsetVal, 1, 6);
            this.optionRows.Controls.Add(this.lblConfigUnloadingOffsetKey, 0, 7);
            this.optionRows.Controls.Add(this.lblConfigUnloadingOffsetVal, 1, 7);
            this.optionRows.Controls.Add(this.lblConfigSlotPitchKey, 0, 8);
            this.optionRows.Controls.Add(this.lblConfigSlotPitchVal, 1, 8);
            this.optionRows.Controls.Add(this.lblConfigSlotCountKey, 0, 9);
            this.optionRows.Controls.Add(this.lblConfigSlotCountVal, 1, 9);
            this.optionRows.Controls.Add(this.lblConfigScanVelocityKey, 0, 10);
            this.optionRows.Controls.Add(this.lblConfigScanVelocityVal, 1, 10);
            this.optionRows.Controls.Add(this.lblSetupToleranceKey, 0, 11);
            this.optionRows.Controls.Add(this.lblSetupToleranceVal, 1, 11);
            this.optionRows.Controls.Add(this.lblConfigInchKey, 0, 12);
            this.optionRows.Controls.Add(this.lblConfigInchVal, 1, 12);
            this.optionRows.Controls.Add(this.lblConfigLevelKey, 0, 13);
            this.optionRows.Controls.Add(this.lblConfigLevelVal, 1, 13);
            this.optionRows.Controls.Add(this.lblSetupSimulationKey, 0, 14);
            this.optionRows.Controls.Add(this.lblSetupSimulationVal, 1, 14);
            this.optionRows.Controls.Add(this.lblConfigDryRunKey, 0, 15);
            this.optionRows.Controls.Add(this.lblConfigDryRunVal, 1, 15);
            this.optionRows.Dock = System.Windows.Forms.DockStyle.Top;
            this.optionRows.Location = new System.Drawing.Point(3, 26);
            this.optionRows.Name = "optionRows";
            this.optionRows.Padding = new System.Windows.Forms.Padding(10, 18, 10, 10);
            this.optionRows.RowCount = 18;
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.optionRows.Size = new System.Drawing.Size(826, 540);
            this.optionRows.TabIndex = 0;
            // 
            // lblRecipeLoadingKey
            // 
            this.lblRecipeLoadingKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecipeLoadingKey.Location = new System.Drawing.Point(13, 18);
            this.lblRecipeLoadingKey.Name = "lblRecipeLoadingKey";
            this.lblRecipeLoadingKey.Size = new System.Drawing.Size(284, 23);
            this.lblRecipeLoadingKey.TabIndex = 0;
            this.lblRecipeLoadingKey.Text = "LOADING Z";
            this.lblRecipeLoadingKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecipeLoadingVal
            // 
            this.lblRecipeLoadingVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecipeLoadingVal.Location = new System.Drawing.Point(303, 18);
            this.lblRecipeLoadingVal.Name = "lblRecipeLoadingVal";
            this.lblRecipeLoadingVal.Size = new System.Drawing.Size(510, 23);
            this.lblRecipeLoadingVal.TabIndex = 1;
            this.lblRecipeLoadingVal.Text = "150000 um";
            this.lblRecipeLoadingVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecipeUnloadingKey
            // 
            this.lblRecipeUnloadingKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecipeUnloadingKey.Location = new System.Drawing.Point(13, 50);
            this.lblRecipeUnloadingKey.Name = "lblRecipeUnloadingKey";
            this.lblRecipeUnloadingKey.Size = new System.Drawing.Size(284, 23);
            this.lblRecipeUnloadingKey.TabIndex = 2;
            this.lblRecipeUnloadingKey.Text = "UNLOADING Z";
            this.lblRecipeUnloadingKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecipeUnloadingVal
            // 
            this.lblRecipeUnloadingVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecipeUnloadingVal.Location = new System.Drawing.Point(303, 50);
            this.lblRecipeUnloadingVal.Name = "lblRecipeUnloadingVal";
            this.lblRecipeUnloadingVal.Size = new System.Drawing.Size(510, 23);
            this.lblRecipeUnloadingVal.TabIndex = 3;
            this.lblRecipeUnloadingVal.Text = "150000 um";
            this.lblRecipeUnloadingVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecipeAvoidKey
            // 
            this.lblRecipeAvoidKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecipeAvoidKey.Location = new System.Drawing.Point(13, 82);
            this.lblRecipeAvoidKey.Name = "lblRecipeAvoidKey";
            this.lblRecipeAvoidKey.Size = new System.Drawing.Size(284, 23);
            this.lblRecipeAvoidKey.TabIndex = 4;
            this.lblRecipeAvoidKey.Text = "READY POSITION";
            this.lblRecipeAvoidKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecipeAvoidVal
            // 
            this.lblRecipeAvoidVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecipeAvoidVal.Location = new System.Drawing.Point(303, 82);
            this.lblRecipeAvoidVal.Name = "lblRecipeAvoidVal";
            this.lblRecipeAvoidVal.Size = new System.Drawing.Size(510, 23);
            this.lblRecipeAvoidVal.TabIndex = 5;
            this.lblRecipeAvoidVal.Text = "0 um";
            this.lblRecipeAvoidVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecipeFirstSlotKey
            // 
            this.lblRecipeFirstSlotKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecipeFirstSlotKey.Location = new System.Drawing.Point(13, 114);
            this.lblRecipeFirstSlotKey.Name = "lblRecipeFirstSlotKey";
            this.lblRecipeFirstSlotKey.Size = new System.Drawing.Size(284, 23);
            this.lblRecipeFirstSlotKey.TabIndex = 6;
            this.lblRecipeFirstSlotKey.Text = "FIRST SLOT";
            this.lblRecipeFirstSlotKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecipeFirstSlotVal
            // 
            this.lblRecipeFirstSlotVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecipeFirstSlotVal.Location = new System.Drawing.Point(303, 114);
            this.lblRecipeFirstSlotVal.Name = "lblRecipeFirstSlotVal";
            this.lblRecipeFirstSlotVal.Size = new System.Drawing.Size(510, 23);
            this.lblRecipeFirstSlotVal.TabIndex = 7;
            this.lblRecipeFirstSlotVal.Text = "10000 um";
            this.lblRecipeFirstSlotVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecipeMappingStartKey
            // 
            this.lblRecipeMappingStartKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecipeMappingStartKey.Location = new System.Drawing.Point(13, 146);
            this.lblRecipeMappingStartKey.Name = "lblRecipeMappingStartKey";
            this.lblRecipeMappingStartKey.Size = new System.Drawing.Size(284, 23);
            this.lblRecipeMappingStartKey.TabIndex = 8;
            this.lblRecipeMappingStartKey.Text = "MAPPING START Z";
            this.lblRecipeMappingStartKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecipeMappingStartVal
            // 
            this.lblRecipeMappingStartVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecipeMappingStartVal.Location = new System.Drawing.Point(303, 146);
            this.lblRecipeMappingStartVal.Name = "lblRecipeMappingStartVal";
            this.lblRecipeMappingStartVal.Size = new System.Drawing.Size(510, 23);
            this.lblRecipeMappingStartVal.TabIndex = 9;
            this.lblRecipeMappingStartVal.Text = "5000 um";
            this.lblRecipeMappingStartVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecipeMappingEndKey
            // 
            this.lblRecipeMappingEndKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecipeMappingEndKey.Location = new System.Drawing.Point(13, 178);
            this.lblRecipeMappingEndKey.Name = "lblRecipeMappingEndKey";
            this.lblRecipeMappingEndKey.Size = new System.Drawing.Size(284, 23);
            this.lblRecipeMappingEndKey.TabIndex = 10;
            this.lblRecipeMappingEndKey.Text = "MAPPING END Z";
            this.lblRecipeMappingEndKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecipeMappingEndVal
            // 
            this.lblRecipeMappingEndVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecipeMappingEndVal.Location = new System.Drawing.Point(303, 178);
            this.lblRecipeMappingEndVal.Name = "lblRecipeMappingEndVal";
            this.lblRecipeMappingEndVal.Size = new System.Drawing.Size(510, 23);
            this.lblRecipeMappingEndVal.TabIndex = 11;
            this.lblRecipeMappingEndVal.Text = "130000 um";
            this.lblRecipeMappingEndVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigLoadingOffsetKey
            // 
            this.lblConfigLoadingOffsetKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigLoadingOffsetKey.Location = new System.Drawing.Point(13, 210);
            this.lblConfigLoadingOffsetKey.Name = "lblConfigLoadingOffsetKey";
            this.lblConfigLoadingOffsetKey.Size = new System.Drawing.Size(284, 23);
            this.lblConfigLoadingOffsetKey.TabIndex = 12;
            this.lblConfigLoadingOffsetKey.Text = "LOADING OFFSET";
            this.lblConfigLoadingOffsetKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigLoadingOffsetVal
            // 
            this.lblConfigLoadingOffsetVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigLoadingOffsetVal.Location = new System.Drawing.Point(303, 210);
            this.lblConfigLoadingOffsetVal.Name = "lblConfigLoadingOffsetVal";
            this.lblConfigLoadingOffsetVal.Size = new System.Drawing.Size(510, 23);
            this.lblConfigLoadingOffsetVal.TabIndex = 13;
            this.lblConfigLoadingOffsetVal.Text = "0 um";
            this.lblConfigLoadingOffsetVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigUnloadingOffsetKey
            // 
            this.lblConfigUnloadingOffsetKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigUnloadingOffsetKey.Location = new System.Drawing.Point(13, 242);
            this.lblConfigUnloadingOffsetKey.Name = "lblConfigUnloadingOffsetKey";
            this.lblConfigUnloadingOffsetKey.Size = new System.Drawing.Size(284, 23);
            this.lblConfigUnloadingOffsetKey.TabIndex = 14;
            this.lblConfigUnloadingOffsetKey.Text = "UNLOADING OFFSET";
            this.lblConfigUnloadingOffsetKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigUnloadingOffsetVal
            // 
            this.lblConfigUnloadingOffsetVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigUnloadingOffsetVal.Location = new System.Drawing.Point(303, 242);
            this.lblConfigUnloadingOffsetVal.Name = "lblConfigUnloadingOffsetVal";
            this.lblConfigUnloadingOffsetVal.Size = new System.Drawing.Size(510, 23);
            this.lblConfigUnloadingOffsetVal.TabIndex = 15;
            this.lblConfigUnloadingOffsetVal.Text = "0 um";
            this.lblConfigUnloadingOffsetVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigSlotPitchKey
            // 
            this.lblConfigSlotPitchKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigSlotPitchKey.Location = new System.Drawing.Point(13, 274);
            this.lblConfigSlotPitchKey.Name = "lblConfigSlotPitchKey";
            this.lblConfigSlotPitchKey.Size = new System.Drawing.Size(284, 23);
            this.lblConfigSlotPitchKey.TabIndex = 16;
            this.lblConfigSlotPitchKey.Text = "SLOT PITCH";
            this.lblConfigSlotPitchKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigSlotPitchVal
            // 
            this.lblConfigSlotPitchVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigSlotPitchVal.Location = new System.Drawing.Point(303, 274);
            this.lblConfigSlotPitchVal.Name = "lblConfigSlotPitchVal";
            this.lblConfigSlotPitchVal.Size = new System.Drawing.Size(510, 23);
            this.lblConfigSlotPitchVal.TabIndex = 17;
            this.lblConfigSlotPitchVal.Text = "5000 um";
            this.lblConfigSlotPitchVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigSlotCountKey
            // 
            this.lblConfigSlotCountKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigSlotCountKey.Location = new System.Drawing.Point(13, 306);
            this.lblConfigSlotCountKey.Name = "lblConfigSlotCountKey";
            this.lblConfigSlotCountKey.Size = new System.Drawing.Size(284, 23);
            this.lblConfigSlotCountKey.TabIndex = 18;
            this.lblConfigSlotCountKey.Text = "SLOT COUNT";
            this.lblConfigSlotCountKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigSlotCountVal
            // 
            this.lblConfigSlotCountVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigSlotCountVal.Location = new System.Drawing.Point(303, 306);
            this.lblConfigSlotCountVal.Name = "lblConfigSlotCountVal";
            this.lblConfigSlotCountVal.Size = new System.Drawing.Size(510, 23);
            this.lblConfigSlotCountVal.TabIndex = 19;
            this.lblConfigSlotCountVal.Text = "25";
            this.lblConfigSlotCountVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigScanVelocityKey
            // 
            this.lblConfigScanVelocityKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigScanVelocityKey.Location = new System.Drawing.Point(13, 338);
            this.lblConfigScanVelocityKey.Name = "lblConfigScanVelocityKey";
            this.lblConfigScanVelocityKey.Size = new System.Drawing.Size(284, 23);
            this.lblConfigScanVelocityKey.TabIndex = 20;
            this.lblConfigScanVelocityKey.Text = "SCAN/JOG VELOCITY";
            this.lblConfigScanVelocityKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigScanVelocityVal
            // 
            this.lblConfigScanVelocityVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigScanVelocityVal.Location = new System.Drawing.Point(303, 338);
            this.lblConfigScanVelocityVal.Name = "lblConfigScanVelocityVal";
            this.lblConfigScanVelocityVal.Size = new System.Drawing.Size(510, 23);
            this.lblConfigScanVelocityVal.TabIndex = 21;
            this.lblConfigScanVelocityVal.Text = "20 mm/s";
            this.lblConfigScanVelocityVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSetupToleranceKey
            // 
            this.lblSetupToleranceKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSetupToleranceKey.Location = new System.Drawing.Point(13, 370);
            this.lblSetupToleranceKey.Name = "lblSetupToleranceKey";
            this.lblSetupToleranceKey.Size = new System.Drawing.Size(284, 23);
            this.lblSetupToleranceKey.TabIndex = 22;
            this.lblSetupToleranceKey.Text = "IN POSITION TOL.";
            this.lblSetupToleranceKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSetupToleranceVal
            // 
            this.lblSetupToleranceVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSetupToleranceVal.Location = new System.Drawing.Point(303, 370);
            this.lblSetupToleranceVal.Name = "lblSetupToleranceVal";
            this.lblSetupToleranceVal.Size = new System.Drawing.Size(510, 23);
            this.lblSetupToleranceVal.TabIndex = 23;
            this.lblSetupToleranceVal.Text = "50 um";
            this.lblSetupToleranceVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigInchKey
            // 
            this.lblConfigInchKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigInchKey.Location = new System.Drawing.Point(13, 402);
            this.lblConfigInchKey.Name = "lblConfigInchKey";
            this.lblConfigInchKey.Size = new System.Drawing.Size(284, 23);
            this.lblConfigInchKey.TabIndex = 24;
            this.lblConfigInchKey.Text = "INCH SELECT";
            this.lblConfigInchKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigInchVal
            // 
            this.lblConfigInchVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigInchVal.Location = new System.Drawing.Point(303, 402);
            this.lblConfigInchVal.Name = "lblConfigInchVal";
            this.lblConfigInchVal.Size = new System.Drawing.Size(510, 23);
            this.lblConfigInchVal.TabIndex = 25;
            this.lblConfigInchVal.Text = "1";
            this.lblConfigInchVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigLevelKey
            // 
            this.lblConfigLevelKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigLevelKey.Location = new System.Drawing.Point(13, 434);
            this.lblConfigLevelKey.Name = "lblConfigLevelKey";
            this.lblConfigLevelKey.Size = new System.Drawing.Size(284, 23);
            this.lblConfigLevelKey.TabIndex = 26;
            this.lblConfigLevelKey.Text = "CASSETTE LEVEL";
            this.lblConfigLevelKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigLevelVal
            // 
            this.lblConfigLevelVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigLevelVal.Location = new System.Drawing.Point(303, 434);
            this.lblConfigLevelVal.Name = "lblConfigLevelVal";
            this.lblConfigLevelVal.Size = new System.Drawing.Size(510, 23);
            this.lblConfigLevelVal.TabIndex = 27;
            this.lblConfigLevelVal.Text = "2";
            this.lblConfigLevelVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSetupSimulationKey
            // 
            this.lblSetupSimulationKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSetupSimulationKey.Location = new System.Drawing.Point(13, 466);
            this.lblSetupSimulationKey.Name = "lblSetupSimulationKey";
            this.lblSetupSimulationKey.Size = new System.Drawing.Size(284, 23);
            this.lblSetupSimulationKey.TabIndex = 28;
            this.lblSetupSimulationKey.Text = "SIMULATION MODE";
            this.lblSetupSimulationKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSetupSimulationVal
            // 
            this.lblSetupSimulationVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSetupSimulationVal.Location = new System.Drawing.Point(303, 466);
            this.lblSetupSimulationVal.Name = "lblSetupSimulationVal";
            this.lblSetupSimulationVal.Size = new System.Drawing.Size(510, 23);
            this.lblSetupSimulationVal.TabIndex = 29;
            this.lblSetupSimulationVal.Text = "True";
            this.lblSetupSimulationVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigDryRunKey
            // 
            this.lblConfigDryRunKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigDryRunKey.Location = new System.Drawing.Point(13, 498);
            this.lblConfigDryRunKey.Name = "lblConfigDryRunKey";
            this.lblConfigDryRunKey.Size = new System.Drawing.Size(284, 23);
            this.lblConfigDryRunKey.TabIndex = 30;
            this.lblConfigDryRunKey.Text = "DRY RUN";
            this.lblConfigDryRunKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblConfigDryRunVal
            // 
            this.lblConfigDryRunVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConfigDryRunVal.Location = new System.Drawing.Point(303, 498);
            this.lblConfigDryRunVal.Name = "lblConfigDryRunVal";
            this.lblConfigDryRunVal.Size = new System.Drawing.Size(510, 23);
            this.lblConfigDryRunVal.TabIndex = 31;
            this.lblConfigDryRunVal.Text = "True";
            this.lblConfigDryRunVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // waitRows
            // 
            this.waitRows.ColumnCount = 2;
            this.waitRows.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 290F));
            this.waitRows.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.waitRows.Controls.Add(this.lblWaitScanSettleKey, 0, 0);
            this.waitRows.Controls.Add(this.lblWaitScanSettleVal, 1, 0);
            this.waitRows.Controls.Add(this.lblWaitMoveTimeoutKey, 0, 1);
            this.waitRows.Controls.Add(this.lblWaitMoveTimeoutVal, 1, 1);
            this.waitRows.Dock = System.Windows.Forms.DockStyle.Top;
            this.waitRows.Location = new System.Drawing.Point(3, 26);
            this.waitRows.Name = "waitRows";
            this.waitRows.Padding = new System.Windows.Forms.Padding(10, 18, 10, 10);
            this.waitRows.RowCount = 3;
            this.waitRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.waitRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.waitRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.waitRows.Size = new System.Drawing.Size(826, 126);
            this.waitRows.TabIndex = 0;
            // 
            // lblWaitScanSettleKey
            // 
            this.lblWaitScanSettleKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblWaitScanSettleKey.Location = new System.Drawing.Point(13, 18);
            this.lblWaitScanSettleKey.Name = "lblWaitScanSettleKey";
            this.lblWaitScanSettleKey.Size = new System.Drawing.Size(284, 23);
            this.lblWaitScanSettleKey.TabIndex = 0;
            this.lblWaitScanSettleKey.Text = "SCAN SETTLE TIME";
            this.lblWaitScanSettleKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWaitScanSettleVal
            // 
            this.lblWaitScanSettleVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblWaitScanSettleVal.Location = new System.Drawing.Point(303, 18);
            this.lblWaitScanSettleVal.Name = "lblWaitScanSettleVal";
            this.lblWaitScanSettleVal.Size = new System.Drawing.Size(510, 23);
            this.lblWaitScanSettleVal.TabIndex = 1;
            this.lblWaitScanSettleVal.Text = "100 ms";
            this.lblWaitScanSettleVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblWaitMoveTimeoutKey
            //
            this.lblWaitMoveTimeoutKey.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblWaitMoveTimeoutKey.Location = new System.Drawing.Point(13, 50);
            this.lblWaitMoveTimeoutKey.Name = "lblWaitMoveTimeoutKey";
            this.lblWaitMoveTimeoutKey.Size = new System.Drawing.Size(284, 23);
            this.lblWaitMoveTimeoutKey.TabIndex = 2;
            this.lblWaitMoveTimeoutKey.Text = "MOVE TIMEOUT";
            this.lblWaitMoveTimeoutKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblWaitMoveTimeoutVal
            //
            this.lblWaitMoveTimeoutVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblWaitMoveTimeoutVal.Location = new System.Drawing.Point(303, 50);
            this.lblWaitMoveTimeoutVal.Name = "lblWaitMoveTimeoutVal";
            this.lblWaitMoveTimeoutVal.Size = new System.Drawing.Size(510, 23);
            this.lblWaitMoveTimeoutVal.TabIndex = 3;
            this.lblWaitMoveTimeoutVal.Text = "10000 ms";
            this.lblWaitMoveTimeoutVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // InputCassetteRecipePage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "InputCassetteRecipePage";
            this.Size = new System.Drawing.Size(1500, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.grpActions.ResumeLayout(false);
            this.actionLayout.ResumeLayout(false);
            this.grpIo.ResumeLayout(false);
            this.ioLayout.ResumeLayout(false);
            this.centerLayout.ResumeLayout(false);
            this.grpOptions.ResumeLayout(false);
            this.grpWait.ResumeLayout(false);
            this.rightLayout.ResumeLayout(false);
            this.grpJog.ResumeLayout(false);
            this.jogContainer.ResumeLayout(false);
            this.grpJogMove.ResumeLayout(false);
            this.jogMoveLayout.ResumeLayout(false);
            this.jogMoveLayout.PerformLayout();
            this.grpJogMode.ResumeLayout(false);
            this.jogModeLayout.ResumeLayout(false);
            this.jogModeLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numStepDistance)).EndInit();
            this.jogPadPanel.ResumeLayout(false);
            this.grpSpeed.ResumeLayout(false);
            this.speedLayout.ResumeLayout(false);
            this.speedTrackLayout.ResumeLayout(false);
            this.speedTrackLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkSpeed)).EndInit();
            this.optionRows.ResumeLayout(false);
            this.waitRows.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.RadioButton rdoCurrent;
    }
}
