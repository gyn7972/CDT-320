namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class OutputFeederRecipePage
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
        private System.Windows.Forms.TableLayoutPanel jogCompositeLayout;
        private QMC.CDT_320.Ui.Controls.JogPositionListControl jogPositionListControl;
        private QMC.CDT_320.Ui.Controls.JogAxisMoveControl jogAxisMoveControl;
        private QMC.CDT_320.Ui.Controls.JogSpeedControl jogSpeedControl;
        private System.Windows.Forms.Panel actionScrollPanel;
        private System.Windows.Forms.TableLayoutPanel actionLayout;
        private System.Windows.Forms.TableLayoutPanel goodLayout;
        private System.Windows.Forms.TableLayoutPanel ngLayout;
        private System.Windows.Forms.TableLayoutPanel commonLayout;
        private System.Windows.Forms.Label lblGoodGroup;
        private System.Windows.Forms.Label lblNgGroup;
        private System.Windows.Forms.Label lblCommonGroup;
        private QMC.CDT_320.Ui.Controls.ActionButton btnGoodLoadingMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnGoodUnloadingMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnGoodCassetteExchangeMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnGoodWaferLoadAvoidMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnGoodSlotStartMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnGoodWaferUnloadAvoidMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnGoodSlotEndMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnGoodWaferBarcodeMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnNgLoadingMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnNgUnloadingMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnNgCassetteExchangeMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnNgWaferLoadAvoidMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnNgSlotStartMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnNgWaferUnloadAvoidMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnNgSlotEndMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnNgWaferBarcodeMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnReadyMove;
        private QMC.CDT_320.Ui.Controls.IoCylinderPanelControl ioCylinderPanel;
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
        private QMC.CDT_320.Ui.Controls.ParameterGridControl waitParameterGrid;
        private System.Windows.Forms.TableLayoutPanel waitRows;
        private System.Windows.Forms.Label lblWaitScanSettleKey;
        private System.Windows.Forms.Label lblWaitScanSettleVal;
        private System.Windows.Forms.Label lblWaitMoveTimeoutKey;
        private System.Windows.Forms.Label lblWaitMoveTimeoutVal;

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
            this.actionScrollPanel = new System.Windows.Forms.Panel();
            this.actionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblGoodGroup = new System.Windows.Forms.Label();
            this.goodLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnGoodLoadingMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnGoodUnloadingMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnGoodCassetteExchangeMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnGoodWaferLoadAvoidMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnGoodSlotStartMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnGoodWaferUnloadAvoidMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnGoodSlotEndMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnGoodWaferBarcodeMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.lblNgGroup = new System.Windows.Forms.Label();
            this.ngLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnNgLoadingMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNgUnloadingMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNgCassetteExchangeMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNgWaferLoadAvoidMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNgSlotStartMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNgWaferUnloadAvoidMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNgSlotEndMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNgWaferBarcodeMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.lblCommonGroup = new System.Windows.Forms.Label();
            this.commonLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnReadyMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.grpIo = new System.Windows.Forms.GroupBox();
            this.ioCylinderPanel = new QMC.CDT_320.Ui.Controls.IoCylinderPanelControl();
            this.centerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpWait = new System.Windows.Forms.GroupBox();
            this.waitParameterGrid = new QMC.CDT_320.Ui.Controls.ParameterGridControl();
            this.grpOptions = new System.Windows.Forms.GroupBox();
            this.optionParameterGrid = new QMC.CDT_320.Ui.Controls.ParameterGridControl();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpJog = new System.Windows.Forms.GroupBox();
            this.jogCompositeLayout = new System.Windows.Forms.TableLayoutPanel();
            this.jogPositionListControl = new QMC.CDT_320.Ui.Controls.JogPositionListControl();
            this.jogAxisMoveControl = new QMC.CDT_320.Ui.Controls.JogAxisMoveControl();
            this.grpSpeed = new System.Windows.Forms.GroupBox();
            this.jogSpeedControl = new QMC.CDT_320.Ui.Controls.JogSpeedControl();
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
            this.actionScrollPanel.SuspendLayout();
            this.actionLayout.SuspendLayout();
            this.goodLayout.SuspendLayout();
            this.ngLayout.SuspendLayout();
            this.commonLayout.SuspendLayout();
            this.grpIo.SuspendLayout();
            this.centerLayout.SuspendLayout();
            this.grpWait.SuspendLayout();
            this.grpOptions.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.grpJog.SuspendLayout();
            this.jogCompositeLayout.SuspendLayout();
            this.grpSpeed.SuspendLayout();
            this.ioLayout.SuspendLayout();
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
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1678, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:recipe.outputFeeder";
            this.lblHeader.Text = "OUTPUT FEEDER";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(219)))), ((int)(((byte)(222)))));
            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 296F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 521F));
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
            this.contentLayout.Size = new System.Drawing.Size(1678, 870);
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
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 627F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Size = new System.Drawing.Size(290, 868);
            this.leftLayout.TabIndex = 0;
            // 
            // grpActions
            // 
            this.grpActions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpActions.Controls.Add(this.actionScrollPanel);
            this.grpActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpActions.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpActions.Location = new System.Drawing.Point(0, 0);
            this.grpActions.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpActions.Name = "grpActions";
            this.grpActions.Size = new System.Drawing.Size(290, 621);
            this.grpActions.TabIndex = 0;
            this.grpActions.TabStop = false;
            this.grpActions.Text = "MANUAL ACTION";
            // 
            // actionScrollPanel
            // 
            this.actionScrollPanel.AutoScroll = true;
            this.actionScrollPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.actionScrollPanel.Controls.Add(this.actionLayout);
            this.actionScrollPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionScrollPanel.Location = new System.Drawing.Point(3, 21);
            this.actionScrollPanel.Margin = new System.Windows.Forms.Padding(0);
            this.actionScrollPanel.Name = "actionScrollPanel";
            this.actionScrollPanel.Size = new System.Drawing.Size(284, 597);
            this.actionScrollPanel.TabIndex = 0;
            // 
            // actionLayout
            // 
            this.actionLayout.ColumnCount = 1;
            this.actionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionLayout.Controls.Add(this.lblGoodGroup, 0, 0);
            this.actionLayout.Controls.Add(this.goodLayout, 0, 1);
            this.actionLayout.Controls.Add(this.lblNgGroup, 0, 2);
            this.actionLayout.Controls.Add(this.ngLayout, 0, 3);
            this.actionLayout.Controls.Add(this.lblCommonGroup, 0, 4);
            this.actionLayout.Controls.Add(this.commonLayout, 0, 5);
            this.actionLayout.AutoSize = true;
            this.actionLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.actionLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.actionLayout.Location = new System.Drawing.Point(0, 0);
            this.actionLayout.Name = "actionLayout";
            this.actionLayout.Padding = new System.Windows.Forms.Padding(10, 12, 10, 6);
            this.actionLayout.RowCount = 6;
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 376F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 376F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.actionLayout.Size = new System.Drawing.Size(264, 870);
            this.actionLayout.TabIndex = 0;
            // 
            // lblGoodGroup
            // 
            this.lblGoodGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodGroup.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblGoodGroup.Location = new System.Drawing.Point(10, 12);
            this.lblGoodGroup.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.lblGoodGroup.Name = "lblGoodGroup";
            this.lblGoodGroup.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblGoodGroup.Size = new System.Drawing.Size(264, 22);
            this.lblGoodGroup.TabIndex = 0;
            this.lblGoodGroup.Text = "GOOD";
            this.lblGoodGroup.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // goodLayout
            // 
            this.goodLayout.ColumnCount = 1;
            this.goodLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.goodLayout.Controls.Add(this.btnGoodLoadingMove, 0, 0);
            this.goodLayout.Controls.Add(this.btnGoodUnloadingMove, 0, 1);
            this.goodLayout.Controls.Add(this.btnGoodCassetteExchangeMove, 0, 2);
            this.goodLayout.Controls.Add(this.btnGoodWaferLoadAvoidMove, 0, 3);
            this.goodLayout.Controls.Add(this.btnGoodSlotStartMove, 0, 4);
            this.goodLayout.Controls.Add(this.btnGoodWaferUnloadAvoidMove, 0, 5);
            this.goodLayout.Controls.Add(this.btnGoodSlotEndMove, 0, 6);
            this.goodLayout.Controls.Add(this.btnGoodWaferBarcodeMove, 0, 7);
            this.goodLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.goodLayout.Location = new System.Drawing.Point(10, 36);
            this.goodLayout.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.goodLayout.Name = "goodLayout";
            this.goodLayout.RowCount = 8;
            this.goodLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.goodLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.goodLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.goodLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.goodLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.goodLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.goodLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.goodLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.goodLayout.Size = new System.Drawing.Size(244, 376);
            this.goodLayout.TabIndex = 1;
            // 
            // btnGoodLoadingMove
            // 
            this.btnGoodLoadingMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGoodLoadingMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGoodLoadingMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoodLoadingMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnGoodLoadingMove.ForeColor = System.Drawing.Color.White;
            this.btnGoodLoadingMove.Location = new System.Drawing.Point(0, 0);
            this.btnGoodLoadingMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnGoodLoadingMove.Name = "btnGoodLoadingMove";
            this.btnGoodLoadingMove.Size = new System.Drawing.Size(264, 41);
            this.btnGoodLoadingMove.TabIndex = 0;
            this.btnGoodLoadingMove.Text = "GOOD LOADING MOVE";
            this.btnGoodLoadingMove.Click += new System.EventHandler(this.btnGoodLoadingMove_Click);
            // 
            // btnGoodUnloadingMove
            // 
            this.btnGoodUnloadingMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGoodUnloadingMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGoodUnloadingMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoodUnloadingMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnGoodUnloadingMove.ForeColor = System.Drawing.Color.White;
            this.btnGoodUnloadingMove.Location = new System.Drawing.Point(0, 49);
            this.btnGoodUnloadingMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnGoodUnloadingMove.Name = "btnGoodUnloadingMove";
            this.btnGoodUnloadingMove.Size = new System.Drawing.Size(264, 41);
            this.btnGoodUnloadingMove.TabIndex = 1;
            this.btnGoodUnloadingMove.Text = "GOOD UNLOADING MOVE";
            this.btnGoodUnloadingMove.Click += new System.EventHandler(this.btnGoodUnloadingMove_Click);
            // 
            // btnGoodCassetteExchangeMove
            // 
            this.btnGoodCassetteExchangeMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGoodCassetteExchangeMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGoodCassetteExchangeMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoodCassetteExchangeMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnGoodCassetteExchangeMove.ForeColor = System.Drawing.Color.White;
            this.btnGoodCassetteExchangeMove.Location = new System.Drawing.Point(0, 94);
            this.btnGoodCassetteExchangeMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnGoodCassetteExchangeMove.Name = "btnGoodCassetteExchangeMove";
            this.btnGoodCassetteExchangeMove.Size = new System.Drawing.Size(244, 39);
            this.btnGoodCassetteExchangeMove.TabIndex = 2;
            this.btnGoodCassetteExchangeMove.Text = "GOOD CASSETTE EXCHANGE POSITION";
            this.btnGoodCassetteExchangeMove.Click += new System.EventHandler(this.TeachingMoveButton_Click);
            // 
            // btnGoodWaferLoadAvoidMove
            // 
            this.btnGoodWaferLoadAvoidMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGoodWaferLoadAvoidMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGoodWaferLoadAvoidMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoodWaferLoadAvoidMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnGoodWaferLoadAvoidMove.ForeColor = System.Drawing.Color.White;
            this.btnGoodWaferLoadAvoidMove.Location = new System.Drawing.Point(0, 141);
            this.btnGoodWaferLoadAvoidMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnGoodWaferLoadAvoidMove.Name = "btnGoodWaferLoadAvoidMove";
            this.btnGoodWaferLoadAvoidMove.Size = new System.Drawing.Size(244, 39);
            this.btnGoodWaferLoadAvoidMove.TabIndex = 3;
            this.btnGoodWaferLoadAvoidMove.Text = "GOOD WAFER LOAD AVOID POSITION";
            this.btnGoodWaferLoadAvoidMove.Click += new System.EventHandler(this.TeachingMoveButton_Click);
            // 
            // btnGoodSlotStartMove
            // 
            this.btnGoodSlotStartMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGoodSlotStartMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGoodSlotStartMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoodSlotStartMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnGoodSlotStartMove.ForeColor = System.Drawing.Color.White;
            this.btnGoodSlotStartMove.Location = new System.Drawing.Point(0, 188);
            this.btnGoodSlotStartMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnGoodSlotStartMove.Name = "btnGoodSlotStartMove";
            this.btnGoodSlotStartMove.Size = new System.Drawing.Size(264, 41);
            this.btnGoodSlotStartMove.TabIndex = 4;
            this.btnGoodSlotStartMove.Text = "GOOD SLOT START";
            this.btnGoodSlotStartMove.Click += new System.EventHandler(this.btnGoodSlotStartMove_Click);
            // 
            // btnGoodWaferUnloadAvoidMove
            // 
            this.btnGoodWaferUnloadAvoidMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGoodWaferUnloadAvoidMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGoodWaferUnloadAvoidMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoodWaferUnloadAvoidMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnGoodWaferUnloadAvoidMove.ForeColor = System.Drawing.Color.White;
            this.btnGoodWaferUnloadAvoidMove.Location = new System.Drawing.Point(0, 235);
            this.btnGoodWaferUnloadAvoidMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnGoodWaferUnloadAvoidMove.Name = "btnGoodWaferUnloadAvoidMove";
            this.btnGoodWaferUnloadAvoidMove.Size = new System.Drawing.Size(244, 39);
            this.btnGoodWaferUnloadAvoidMove.TabIndex = 5;
            this.btnGoodWaferUnloadAvoidMove.Text = "GOOD WAFER UNLOAD AVOID POSITION";
            this.btnGoodWaferUnloadAvoidMove.Click += new System.EventHandler(this.TeachingMoveButton_Click);
            // 
            // btnGoodSlotEndMove
            // 
            this.btnGoodSlotEndMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGoodSlotEndMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGoodSlotEndMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoodSlotEndMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnGoodSlotEndMove.ForeColor = System.Drawing.Color.White;
            this.btnGoodSlotEndMove.Location = new System.Drawing.Point(0, 282);
            this.btnGoodSlotEndMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnGoodSlotEndMove.Name = "btnGoodSlotEndMove";
            this.btnGoodSlotEndMove.Size = new System.Drawing.Size(264, 41);
            this.btnGoodSlotEndMove.TabIndex = 6;
            this.btnGoodSlotEndMove.Text = "GOOD SLOT END";
            this.btnGoodSlotEndMove.Click += new System.EventHandler(this.btnGoodSlotEndMove_Click);
            // 
            // btnGoodWaferBarcodeMove
            // 
            this.btnGoodWaferBarcodeMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGoodWaferBarcodeMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGoodWaferBarcodeMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoodWaferBarcodeMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnGoodWaferBarcodeMove.ForeColor = System.Drawing.Color.White;
            this.btnGoodWaferBarcodeMove.Location = new System.Drawing.Point(0, 329);
            this.btnGoodWaferBarcodeMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnGoodWaferBarcodeMove.Name = "btnGoodWaferBarcodeMove";
            this.btnGoodWaferBarcodeMove.Size = new System.Drawing.Size(244, 39);
            this.btnGoodWaferBarcodeMove.TabIndex = 7;
            this.btnGoodWaferBarcodeMove.Text = "GOOD WAFER BARCODE POSITION";
            this.btnGoodWaferBarcodeMove.Click += new System.EventHandler(this.TeachingMoveButton_Click);
            // 
            // lblNgGroup
            // 
            this.lblNgGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgGroup.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblNgGroup.Location = new System.Drawing.Point(10, 412);
            this.lblNgGroup.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.lblNgGroup.Name = "lblNgGroup";
            this.lblNgGroup.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblNgGroup.Size = new System.Drawing.Size(264, 22);
            this.lblNgGroup.TabIndex = 2;
            this.lblNgGroup.Text = "NG";
            this.lblNgGroup.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ngLayout
            // 
            this.ngLayout.ColumnCount = 1;
            this.ngLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ngLayout.Controls.Add(this.btnNgLoadingMove, 0, 0);
            this.ngLayout.Controls.Add(this.btnNgUnloadingMove, 0, 1);
            this.ngLayout.Controls.Add(this.btnNgCassetteExchangeMove, 0, 2);
            this.ngLayout.Controls.Add(this.btnNgWaferLoadAvoidMove, 0, 3);
            this.ngLayout.Controls.Add(this.btnNgSlotStartMove, 0, 4);
            this.ngLayout.Controls.Add(this.btnNgWaferUnloadAvoidMove, 0, 5);
            this.ngLayout.Controls.Add(this.btnNgSlotEndMove, 0, 6);
            this.ngLayout.Controls.Add(this.btnNgWaferBarcodeMove, 0, 7);
            this.ngLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ngLayout.Location = new System.Drawing.Point(10, 436);
            this.ngLayout.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.ngLayout.Name = "ngLayout";
            this.ngLayout.RowCount = 8;
            this.ngLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.ngLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.ngLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.ngLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.ngLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.ngLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.ngLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.ngLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 47F));
            this.ngLayout.Size = new System.Drawing.Size(244, 376);
            this.ngLayout.TabIndex = 3;
            // 
            // btnNgLoadingMove
            // 
            this.btnNgLoadingMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgLoadingMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgLoadingMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNgLoadingMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNgLoadingMove.ForeColor = System.Drawing.Color.White;
            this.btnNgLoadingMove.Location = new System.Drawing.Point(0, 0);
            this.btnNgLoadingMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnNgLoadingMove.Name = "btnNgLoadingMove";
            this.btnNgLoadingMove.Size = new System.Drawing.Size(264, 41);
            this.btnNgLoadingMove.TabIndex = 4;
            this.btnNgLoadingMove.Text = "NG LOADING MOVE";
            this.btnNgLoadingMove.Click += new System.EventHandler(this.btnNgLoadingMove_Click);
            // 
            // btnNgUnloadingMove
            // 
            this.btnNgUnloadingMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgUnloadingMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgUnloadingMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNgUnloadingMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNgUnloadingMove.ForeColor = System.Drawing.Color.White;
            this.btnNgUnloadingMove.Location = new System.Drawing.Point(0, 49);
            this.btnNgUnloadingMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnNgUnloadingMove.Name = "btnNgUnloadingMove";
            this.btnNgUnloadingMove.Size = new System.Drawing.Size(264, 41);
            this.btnNgUnloadingMove.TabIndex = 5;
            this.btnNgUnloadingMove.Text = "NG UNLOADING MOVE";
            this.btnNgUnloadingMove.Click += new System.EventHandler(this.btnNgUnloadingMove_Click);
            // 
            // btnNgCassetteExchangeMove
            // 
            this.btnNgCassetteExchangeMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgCassetteExchangeMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgCassetteExchangeMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNgCassetteExchangeMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNgCassetteExchangeMove.ForeColor = System.Drawing.Color.White;
            this.btnNgCassetteExchangeMove.Location = new System.Drawing.Point(0, 94);
            this.btnNgCassetteExchangeMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnNgCassetteExchangeMove.Name = "btnNgCassetteExchangeMove";
            this.btnNgCassetteExchangeMove.Size = new System.Drawing.Size(244, 39);
            this.btnNgCassetteExchangeMove.TabIndex = 10;
            this.btnNgCassetteExchangeMove.Text = "NG CASSETTE EXCHANGE POSITION";
            this.btnNgCassetteExchangeMove.Click += new System.EventHandler(this.TeachingMoveButton_Click);
            // 
            // btnNgWaferLoadAvoidMove
            // 
            this.btnNgWaferLoadAvoidMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgWaferLoadAvoidMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgWaferLoadAvoidMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNgWaferLoadAvoidMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNgWaferLoadAvoidMove.ForeColor = System.Drawing.Color.White;
            this.btnNgWaferLoadAvoidMove.Location = new System.Drawing.Point(0, 141);
            this.btnNgWaferLoadAvoidMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnNgWaferLoadAvoidMove.Name = "btnNgWaferLoadAvoidMove";
            this.btnNgWaferLoadAvoidMove.Size = new System.Drawing.Size(244, 39);
            this.btnNgWaferLoadAvoidMove.TabIndex = 11;
            this.btnNgWaferLoadAvoidMove.Text = "NG WAFER LOAD AVOID POSITION";
            this.btnNgWaferLoadAvoidMove.Click += new System.EventHandler(this.TeachingMoveButton_Click);
            // 
            // btnNgSlotStartMove
            // 
            this.btnNgSlotStartMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgSlotStartMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgSlotStartMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNgSlotStartMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNgSlotStartMove.ForeColor = System.Drawing.Color.White;
            this.btnNgSlotStartMove.Location = new System.Drawing.Point(0, 188);
            this.btnNgSlotStartMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnNgSlotStartMove.Name = "btnNgSlotStartMove";
            this.btnNgSlotStartMove.Size = new System.Drawing.Size(264, 41);
            this.btnNgSlotStartMove.TabIndex = 12;
            this.btnNgSlotStartMove.Text = "NG SLOT START";
            this.btnNgSlotStartMove.Click += new System.EventHandler(this.btnNgSlotStartMove_Click);
            // 
            // btnNgWaferUnloadAvoidMove
            // 
            this.btnNgWaferUnloadAvoidMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgWaferUnloadAvoidMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgWaferUnloadAvoidMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNgWaferUnloadAvoidMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNgWaferUnloadAvoidMove.ForeColor = System.Drawing.Color.White;
            this.btnNgWaferUnloadAvoidMove.Location = new System.Drawing.Point(0, 235);
            this.btnNgWaferUnloadAvoidMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnNgWaferUnloadAvoidMove.Name = "btnNgWaferUnloadAvoidMove";
            this.btnNgWaferUnloadAvoidMove.Size = new System.Drawing.Size(244, 39);
            this.btnNgWaferUnloadAvoidMove.TabIndex = 13;
            this.btnNgWaferUnloadAvoidMove.Text = "NG WAFER UNLOAD AVOID POSITION";
            this.btnNgWaferUnloadAvoidMove.Click += new System.EventHandler(this.TeachingMoveButton_Click);
            // 
            // btnNgSlotEndMove
            // 
            this.btnNgSlotEndMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgSlotEndMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgSlotEndMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNgSlotEndMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNgSlotEndMove.ForeColor = System.Drawing.Color.White;
            this.btnNgSlotEndMove.Location = new System.Drawing.Point(0, 282);
            this.btnNgSlotEndMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnNgSlotEndMove.Name = "btnNgSlotEndMove";
            this.btnNgSlotEndMove.Size = new System.Drawing.Size(264, 41);
            this.btnNgSlotEndMove.TabIndex = 14;
            this.btnNgSlotEndMove.Text = "NG SLOT END";
            this.btnNgSlotEndMove.Click += new System.EventHandler(this.btnNgSlotEndMove_Click);
            // 
            // btnNgWaferBarcodeMove
            // 
            this.btnNgWaferBarcodeMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgWaferBarcodeMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgWaferBarcodeMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNgWaferBarcodeMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNgWaferBarcodeMove.ForeColor = System.Drawing.Color.White;
            this.btnNgWaferBarcodeMove.Location = new System.Drawing.Point(0, 329);
            this.btnNgWaferBarcodeMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnNgWaferBarcodeMove.Name = "btnNgWaferBarcodeMove";
            this.btnNgWaferBarcodeMove.Size = new System.Drawing.Size(244, 39);
            this.btnNgWaferBarcodeMove.TabIndex = 15;
            this.btnNgWaferBarcodeMove.Text = "NG WAFER BARCODE POSITION";
            this.btnNgWaferBarcodeMove.Click += new System.EventHandler(this.TeachingMoveButton_Click);
            // 
            // lblCommonGroup
            // 
            this.lblCommonGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCommonGroup.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblCommonGroup.Location = new System.Drawing.Point(10, 812);
            this.lblCommonGroup.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.lblCommonGroup.Name = "lblCommonGroup";
            this.lblCommonGroup.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblCommonGroup.Size = new System.Drawing.Size(264, 22);
            this.lblCommonGroup.TabIndex = 4;
            this.lblCommonGroup.Text = "COMMON";
            this.lblCommonGroup.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // commonLayout
            // 
            this.commonLayout.ColumnCount = 1;
            this.commonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.commonLayout.Controls.Add(this.btnReadyMove, 0, 0);
            this.commonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.commonLayout.Location = new System.Drawing.Point(10, 836);
            this.commonLayout.Margin = new System.Windows.Forms.Padding(0);
            this.commonLayout.Name = "commonLayout";
            this.commonLayout.RowCount = 1;
            this.commonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.commonLayout.Size = new System.Drawing.Size(244, 52);
            this.commonLayout.TabIndex = 5;
            // 
            // btnReadyMove
            // 
            this.btnReadyMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnReadyMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReadyMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReadyMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnReadyMove.ForeColor = System.Drawing.Color.White;
            this.btnReadyMove.Location = new System.Drawing.Point(0, 0);
            this.btnReadyMove.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.btnReadyMove.Name = "btnReadyMove";
            this.btnReadyMove.Size = new System.Drawing.Size(244, 44);
            this.btnReadyMove.TabIndex = 8;
            this.btnReadyMove.Text = "READY MOVE";
            this.btnReadyMove.Click += new System.EventHandler(this.btnReadyMove_Click);
            // 
            // grpIo
            // 
            this.grpIo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpIo.Controls.Add(this.ioCylinderPanel);
            this.grpIo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpIo.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpIo.Location = new System.Drawing.Point(0, 627);
            this.grpIo.Margin = new System.Windows.Forms.Padding(0);
            this.grpIo.Name = "grpIo";
            this.grpIo.Size = new System.Drawing.Size(290, 241);
            this.grpIo.TabIndex = 1;
            this.grpIo.TabStop = false;
            this.grpIo.Text = "CYLINDER && I/O";
            // 
            // ioCylinderPanel
            // 
            this.ioCylinderPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(205)))), ((int)(((byte)(205)))), ((int)(((byte)(205)))));
            this.ioCylinderPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ioCylinderPanel.Location = new System.Drawing.Point(3, 21);
            this.ioCylinderPanel.Margin = new System.Windows.Forms.Padding(0);
            this.ioCylinderPanel.Name = "ioCylinderPanel";
            this.ioCylinderPanel.Size = new System.Drawing.Size(284, 217);
            this.ioCylinderPanel.TabIndex = 0;
            // 
            // centerLayout
            // 
            this.centerLayout.ColumnCount = 1;
            this.centerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.centerLayout.Controls.Add(this.grpWait, 0, 1);
            this.centerLayout.Controls.Add(this.grpOptions, 0, 0);
            this.centerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.centerLayout.Location = new System.Drawing.Point(297, 1);
            this.centerLayout.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.centerLayout.Name = "centerLayout";
            this.centerLayout.RowCount = 2;
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 703F));
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.centerLayout.Size = new System.Drawing.Size(694, 868);
            this.centerLayout.TabIndex = 1;
            // 
            // grpWait
            // 
            this.grpWait.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpWait.Controls.Add(this.waitParameterGrid);
            this.grpWait.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpWait.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpWait.Location = new System.Drawing.Point(0, 703);
            this.grpWait.Margin = new System.Windows.Forms.Padding(0);
            this.grpWait.Name = "grpWait";
            this.grpWait.Size = new System.Drawing.Size(694, 165);
            this.grpWait.TabIndex = 1;
            this.grpWait.TabStop = false;
            this.grpWait.Text = "WAIT TIME";
            // 
            // waitParameterGrid
            // 
            this.waitParameterGrid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.waitParameterGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waitParameterGrid.Location = new System.Drawing.Point(3, 21);
            this.waitParameterGrid.Margin = new System.Windows.Forms.Padding(0);
            this.waitParameterGrid.Name = "waitParameterGrid";
            this.waitParameterGrid.Size = new System.Drawing.Size(688, 141);
            this.waitParameterGrid.TabIndex = 0;
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
            this.grpOptions.Size = new System.Drawing.Size(694, 697);
            this.grpOptions.TabIndex = 0;
            this.grpOptions.TabStop = false;
            this.grpOptions.Text = "OPTION";
            // 
            // optionParameterGrid
            // 
            this.optionParameterGrid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.optionParameterGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.optionParameterGrid.Location = new System.Drawing.Point(3, 21);
            this.optionParameterGrid.Margin = new System.Windows.Forms.Padding(0);
            this.optionParameterGrid.Name = "optionParameterGrid";
            this.optionParameterGrid.Size = new System.Drawing.Size(688, 673);
            this.optionParameterGrid.TabIndex = 0;
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 2;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 428F));
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 93F));
            this.rightLayout.Controls.Add(this.grpJog, 0, 0);
            this.rightLayout.Controls.Add(this.grpSpeed, 1, 0);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(997, 1);
            this.rightLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 1;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Size = new System.Drawing.Size(680, 868);
            this.rightLayout.TabIndex = 2;
            // 
            // grpJog
            // 
            this.grpJog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpJog.Controls.Add(this.jogCompositeLayout);
            this.grpJog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpJog.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpJog.Location = new System.Drawing.Point(0, 0);
            this.grpJog.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.grpJog.Name = "grpJog";
            this.grpJog.Size = new System.Drawing.Size(554, 868);
            this.grpJog.TabIndex = 0;
            this.grpJog.TabStop = false;
            this.grpJog.Text = "JOG OPERATION";
            // 
            // jogCompositeLayout
            // 
            this.jogCompositeLayout.ColumnCount = 1;
            this.jogCompositeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.jogCompositeLayout.Controls.Add(this.jogPositionListControl, 0, 0);
            this.jogCompositeLayout.Controls.Add(this.jogAxisMoveControl, 0, 1);
            this.jogCompositeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogCompositeLayout.Location = new System.Drawing.Point(3, 21);
            this.jogCompositeLayout.Margin = new System.Windows.Forms.Padding(0);
            this.jogCompositeLayout.Name = "jogCompositeLayout";
            this.jogCompositeLayout.RowCount = 2;
            this.jogCompositeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 62F));
            this.jogCompositeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.jogCompositeLayout.Size = new System.Drawing.Size(548, 844);
            this.jogCompositeLayout.TabIndex = 0;
            // 
            // jogPositionListControl
            // 
            this.jogPositionListControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogPositionListControl.Location = new System.Drawing.Point(0, 0);
            this.jogPositionListControl.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.jogPositionListControl.Name = "jogPositionListControl";
            this.jogPositionListControl.Size = new System.Drawing.Size(548, 56);
            this.jogPositionListControl.TabIndex = 0;
            this.jogPositionListControl.WrapColumnsWhenMany = true;
            // 
            // jogAxisMoveControl
            // 
            this.jogAxisMoveControl.AxisColumnsPerRow = 0;
            this.jogAxisMoveControl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.jogAxisMoveControl.ButtonAreaMaxHeight = 164;
            this.jogAxisMoveControl.ButtonAreaMaxWidth = 222;
            this.jogAxisMoveControl.ButtonAreaMinHeight = 164;
            this.jogAxisMoveControl.ButtonAreaMinWidth = 222;
            this.jogAxisMoveControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogAxisMoveControl.LayoutMode = QMC.CDT_320.Ui.Controls.JogAxisMoveLayoutMode.AxisColumns;
            this.jogAxisMoveControl.Location = new System.Drawing.Point(0, 62);
            this.jogAxisMoveControl.Margin = new System.Windows.Forms.Padding(0);
            this.jogAxisMoveControl.Name = "jogAxisMoveControl";
            this.jogAxisMoveControl.ShowCurrentSpeedMode = true;
            this.jogAxisMoveControl.Size = new System.Drawing.Size(548, 782);
            this.jogAxisMoveControl.SpeedControl = null;
            this.jogAxisMoveControl.TabIndex = 1;
            // 
            // grpSpeed
            // 
            this.grpSpeed.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpSpeed.Controls.Add(this.jogSpeedControl);
            this.grpSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSpeed.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpSpeed.Location = new System.Drawing.Point(560, 0);
            this.grpSpeed.Margin = new System.Windows.Forms.Padding(0);
            this.grpSpeed.Padding = new System.Windows.Forms.Padding(3, 2, 3, 3);
            this.grpSpeed.Name = "grpSpeed";
            this.grpSpeed.Size = new System.Drawing.Size(120, 868);
            this.grpSpeed.TabIndex = 1;
            this.grpSpeed.TabStop = false;
            this.grpSpeed.Text = "SPEED";
            // 
            // jogSpeedControl
            // 
            this.jogSpeedControl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.jogSpeedControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogSpeedControl.Location = new System.Drawing.Point(3, 26);
            this.jogSpeedControl.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.jogSpeedControl.Name = "jogSpeedControl";
            this.jogSpeedControl.Size = new System.Drawing.Size(114, 578);
            this.jogSpeedControl.SpeedPercent = 50;
            this.jogSpeedControl.TabIndex = 0;
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
            this.ioLayout.Location = new System.Drawing.Point(3, 21);
            this.ioLayout.Name = "ioLayout";
            this.ioLayout.Padding = new System.Windows.Forms.Padding(12, 22, 12, 12);
            this.ioLayout.RowCount = 6;
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 230F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ioLayout.Size = new System.Drawing.Size(208, 414);
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
            // OutputFeederRecipePage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "OutputFeederRecipePage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.grpActions.ResumeLayout(false);
            this.actionScrollPanel.ResumeLayout(false);
            this.actionScrollPanel.PerformLayout();
            this.actionLayout.ResumeLayout(false);
            this.goodLayout.ResumeLayout(false);
            this.ngLayout.ResumeLayout(false);
            this.commonLayout.ResumeLayout(false);
            this.grpIo.ResumeLayout(false);
            this.centerLayout.ResumeLayout(false);
            this.grpWait.ResumeLayout(false);
            this.grpOptions.ResumeLayout(false);
            this.rightLayout.ResumeLayout(false);
            this.grpJog.ResumeLayout(false);
            this.jogCompositeLayout.ResumeLayout(false);
            this.grpSpeed.ResumeLayout(false);
            this.ioLayout.ResumeLayout(false);
            this.optionRows.ResumeLayout(false);
            this.waitRows.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}


