namespace QMC.CDT_320.Ui.Dialogs
{
    partial class InputStageRunReviewDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.TableLayoutPanel headerLayout;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblDialogMode;
        private System.Windows.Forms.Label lblWaferCaption;
        private System.Windows.Forms.Label lblWaferValue;
        private System.Windows.Forms.Label lblRecipeCaption;
        private System.Windows.Forms.Label lblRecipeValue;
        private System.Windows.Forms.Label lblVisionCaption;
        private System.Windows.Forms.Label lblVisionValue;
        private System.Windows.Forms.Label lblAlignCaption;
        private System.Windows.Forms.Label lblAlignValue;
        private System.Windows.Forms.Label lblMappingCaption;
        private System.Windows.Forms.Label lblMappingValue;
        private System.Windows.Forms.Label lblReviewCaption;
        private System.Windows.Forms.Label lblReviewValue;
        private System.Windows.Forms.TableLayoutPanel bodyLayout;
        private System.Windows.Forms.TableLayoutPanel leftLayout;
        private System.Windows.Forms.GroupBox grpWaferVision;
        private System.Windows.Forms.TableLayoutPanel waferVisionLayout;
        private System.Windows.Forms.TableLayoutPanel waferVisionHeaderLayout;
        private System.Windows.Forms.Label lblWaferVisionState;
        private System.Windows.Forms.Button btnWaferVisionControl;
        private QMC.CDT_320.Ui.Controls.VisionViewerPanel waferVisionViewer;
        private System.Windows.Forms.TableLayoutPanel leftBottomLayout;
        private QMC.CDT320.Ui.Controls.DieMapView mapView;
        private System.Windows.Forms.DataGridView dieGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSequence;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMapX;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMapY;
        private System.Windows.Forms.DataGridViewTextBoxColumn colGridX;
        private System.Windows.Forms.DataGridViewTextBoxColumn colGridY;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOriginalX;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOriginalY;
        private System.Windows.Forms.DataGridViewTextBoxColumn colState;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResult;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBin;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPosX;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPosY;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDieUid;
        private System.Windows.Forms.TableLayoutPanel centerLayout;
        private System.Windows.Forms.GroupBox grpDieState;
        private System.Windows.Forms.TableLayoutPanel dieStateLayout;
        private System.Windows.Forms.FlowLayoutPanel dieStateOptions;
        private System.Windows.Forms.RadioButton rbDieStateWait;
        private System.Windows.Forms.RadioButton rbDieStateGood;
        private System.Windows.Forms.RadioButton rbDieStateNg;
        private System.Windows.Forms.RadioButton rbDieStateSkip;
        private System.Windows.Forms.Button btnApplyDieState;
        private System.Windows.Forms.GroupBox grpStartDie;
        private System.Windows.Forms.TableLayoutPanel startDieLayout;
        private System.Windows.Forms.Label lblStartDieCaption;
        private System.Windows.Forms.Label lblStartDieValue;
        private System.Windows.Forms.Label lblStartIndexCaption;
        private System.Windows.Forms.NumericUpDown numStartIndex;
        private System.Windows.Forms.Button btnSetStartIndex;
        private System.Windows.Forms.CheckBox chkUseSelectedStart;
        private System.Windows.Forms.Button btnSetStartDie;
        private System.Windows.Forms.GroupBox grpJog;
        private System.Windows.Forms.TableLayoutPanel jogLayout;
        private System.Windows.Forms.Label lblJogSpeed;
        private System.Windows.Forms.ComboBox cmbJogSpeed;
        private System.Windows.Forms.Label lblVisionXCaption;
        private System.Windows.Forms.Label lblVisionXValue;
        private System.Windows.Forms.Button btnVisionXMinus;
        private System.Windows.Forms.Button btnVisionXPlus;
        private System.Windows.Forms.Label lblWaferYCaption;
        private System.Windows.Forms.Label lblWaferYValue;
        private System.Windows.Forms.Button btnWaferYMinus;
        private System.Windows.Forms.Button btnWaferYPlus;
        private System.Windows.Forms.Label lblWaferTCaption;
        private System.Windows.Forms.Label lblWaferTValue;
        private System.Windows.Forms.Button btnWaferTMinus;
        private System.Windows.Forms.Button btnWaferTPlus;
        private System.Windows.Forms.Button btnJogStop;
        private System.Windows.Forms.GroupBox grpActions;
        private System.Windows.Forms.TableLayoutPanel actionLayout;
        private System.Windows.Forms.Button btnMoveSelectedDie;
        private System.Windows.Forms.Button btnThetaCorrection;
        private System.Windows.Forms.Button btnDieDetection;
        private System.Windows.Forms.Button btnOffsetApply;
        private System.Windows.Forms.Button btnVisionTest;
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.GroupBox grpMapInfo;
        private System.Windows.Forms.TableLayoutPanel mapInfoLayout;
        private System.Windows.Forms.Label lblMapGridCaption;
        private System.Windows.Forms.Label lblMapGridValue;
        private System.Windows.Forms.Label lblMapProgressCaption;
        private System.Windows.Forms.Label lblMapProgressValue;
        private System.Windows.Forms.Label lblTargetCountCaption;
        private System.Windows.Forms.Label lblTargetCountValue;
        private System.Windows.Forms.Label lblDieSizeXCaption;
        private System.Windows.Forms.Label lblDieSizeXValue;
        private System.Windows.Forms.Label lblDieSizeYCaption;
        private System.Windows.Forms.Label lblDieSizeYValue;
        private System.Windows.Forms.Label lblPitchGapXCaption;
        private System.Windows.Forms.Label lblPitchGapXValue;
        private System.Windows.Forms.Label lblPitchGapYCaption;
        private System.Windows.Forms.Label lblPitchGapYValue;
        private System.Windows.Forms.Label lblWaferDiameterCaption;
        private System.Windows.Forms.Label lblWaferDiameterValue;
        private System.Windows.Forms.Label lblInputCameraXCaption;
        private System.Windows.Forms.Label lblInputCameraXValue;
        private System.Windows.Forms.Label lblInputStageYCaption;
        private System.Windows.Forms.Label lblInputStageYValue;
        private System.Windows.Forms.Label lblEquipmentGridCaption;
        private System.Windows.Forms.Label lblEquipmentGridValue;
        private System.Windows.Forms.Label lblOriginalMapCaption;
        private System.Windows.Forms.Label lblOriginalMapValue;
        private System.Windows.Forms.Label lblMappingOriginCaption;
        private System.Windows.Forms.Label lblMappingOriginValue;
        private System.Windows.Forms.Label lblSelectedDieCaption;
        private System.Windows.Forms.Label lblSelectedDieValue;
        private System.Windows.Forms.Label lblSelectedSequenceCaption;
        private System.Windows.Forms.Label lblSelectedSequenceValue;
        private System.Windows.Forms.Label lblSelectedPositionCaption;
        private System.Windows.Forms.Label lblSelectedPositionValue;
        private System.Windows.Forms.GroupBox grpPickupRoute;
        private System.Windows.Forms.TableLayoutPanel pickupRouteLayout;
        private System.Windows.Forms.Label lblCornerCaption;
        private System.Windows.Forms.FlowLayoutPanel cornerOptions;
        private System.Windows.Forms.RadioButton rbCornerTopLeft;
        private System.Windows.Forms.RadioButton rbCornerTopRight;
        private System.Windows.Forms.RadioButton rbCornerBottomLeft;
        private System.Windows.Forms.RadioButton rbCornerBottomRight;
        private System.Windows.Forms.Label lblDirectionCaption;
        private System.Windows.Forms.FlowLayoutPanel directionOptions;
        private System.Windows.Forms.RadioButton rbDirectionHorizontal;
        private System.Windows.Forms.RadioButton rbDirectionVertical;
        private System.Windows.Forms.Label lblPatternCaption;
        private System.Windows.Forms.FlowLayoutPanel patternOptions;
        private System.Windows.Forms.RadioButton rbPatternStraight;
        private System.Windows.Forms.RadioButton rbPatternZigZag;
        private System.Windows.Forms.Button btnPreviewPath;
        private System.Windows.Forms.Button btnApplyPickupOrder;
        private System.Windows.Forms.GroupBox grpWorkflow;
        private System.Windows.Forms.TableLayoutPanel workflowLayout;
        private System.Windows.Forms.Label lblRevisionCaption;
        private System.Windows.Forms.Label lblMappingRevisionValue;
        private System.Windows.Forms.TextBox txtFailureDetail;
        private System.Windows.Forms.Button btnRetryAlign;
        private System.Windows.Forms.Button btnRetryMapping;
        private System.Windows.Forms.Button btnMappingSetup;
        private System.Windows.Forms.Button btnStartRun;
        private System.Windows.Forms.Button btnAbortAuto;
        private System.Windows.Forms.TableLayoutPanel footerLayout;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnBuzzerStop;
        private System.Windows.Forms.Button btnClose;

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
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.headerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblDialogMode = new System.Windows.Forms.Label();
            this.lblWaferCaption = new System.Windows.Forms.Label();
            this.lblWaferValue = new System.Windows.Forms.Label();
            this.lblRecipeCaption = new System.Windows.Forms.Label();
            this.lblRecipeValue = new System.Windows.Forms.Label();
            this.lblVisionCaption = new System.Windows.Forms.Label();
            this.lblVisionValue = new System.Windows.Forms.Label();
            this.lblAlignCaption = new System.Windows.Forms.Label();
            this.lblAlignValue = new System.Windows.Forms.Label();
            this.lblMappingCaption = new System.Windows.Forms.Label();
            this.lblMappingValue = new System.Windows.Forms.Label();
            this.lblReviewCaption = new System.Windows.Forms.Label();
            this.lblReviewValue = new System.Windows.Forms.Label();
            this.bodyLayout = new System.Windows.Forms.TableLayoutPanel();
            this.leftLayout = new System.Windows.Forms.TableLayoutPanel();
            this.leftBottomLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpWaferVision = new System.Windows.Forms.GroupBox();
            this.waferVisionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.waferVisionHeaderLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblWaferVisionState = new System.Windows.Forms.Label();
            this.btnWaferVisionControl = new System.Windows.Forms.Button();
            this.waferVisionViewer = new QMC.CDT_320.Ui.Controls.VisionViewerPanel();
            this.mapView = new QMC.CDT320.Ui.Controls.DieMapView();
            this.dieGrid = new System.Windows.Forms.DataGridView();
            this.colSequence = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMapX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMapY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colGridX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colGridY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOriginalX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOriginalY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colState = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPosX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPosY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDieUid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.centerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpDieState = new System.Windows.Forms.GroupBox();
            this.dieStateLayout = new System.Windows.Forms.TableLayoutPanel();
            this.dieStateOptions = new System.Windows.Forms.FlowLayoutPanel();
            this.rbDieStateWait = new System.Windows.Forms.RadioButton();
            this.rbDieStateGood = new System.Windows.Forms.RadioButton();
            this.rbDieStateNg = new System.Windows.Forms.RadioButton();
            this.rbDieStateSkip = new System.Windows.Forms.RadioButton();
            this.btnApplyDieState = new System.Windows.Forms.Button();
            this.grpStartDie = new System.Windows.Forms.GroupBox();
            this.startDieLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblStartDieCaption = new System.Windows.Forms.Label();
            this.lblStartDieValue = new System.Windows.Forms.Label();
            this.btnSetStartDie = new System.Windows.Forms.Button();
            this.lblStartIndexCaption = new System.Windows.Forms.Label();
            this.numStartIndex = new System.Windows.Forms.NumericUpDown();
            this.btnSetStartIndex = new System.Windows.Forms.Button();
            this.chkUseSelectedStart = new System.Windows.Forms.CheckBox();
            this.grpJog = new System.Windows.Forms.GroupBox();
            this.jogLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblJogSpeed = new System.Windows.Forms.Label();
            this.cmbJogSpeed = new System.Windows.Forms.ComboBox();
            this.lblVisionXCaption = new System.Windows.Forms.Label();
            this.lblVisionXValue = new System.Windows.Forms.Label();
            this.btnVisionXMinus = new System.Windows.Forms.Button();
            this.btnVisionXPlus = new System.Windows.Forms.Button();
            this.lblWaferYCaption = new System.Windows.Forms.Label();
            this.lblWaferYValue = new System.Windows.Forms.Label();
            this.btnWaferYMinus = new System.Windows.Forms.Button();
            this.btnWaferYPlus = new System.Windows.Forms.Button();
            this.lblWaferTCaption = new System.Windows.Forms.Label();
            this.lblWaferTValue = new System.Windows.Forms.Label();
            this.btnWaferTMinus = new System.Windows.Forms.Button();
            this.btnWaferTPlus = new System.Windows.Forms.Button();
            this.btnJogStop = new System.Windows.Forms.Button();
            this.grpActions = new System.Windows.Forms.GroupBox();
            this.actionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnMoveSelectedDie = new System.Windows.Forms.Button();
            this.btnThetaCorrection = new System.Windows.Forms.Button();
            this.btnDieDetection = new System.Windows.Forms.Button();
            this.btnOffsetApply = new System.Windows.Forms.Button();
            this.btnVisionTest = new System.Windows.Forms.Button();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpMapInfo = new System.Windows.Forms.GroupBox();
            this.mapInfoLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblMapGridCaption = new System.Windows.Forms.Label();
            this.lblMapGridValue = new System.Windows.Forms.Label();
            this.lblMapProgressCaption = new System.Windows.Forms.Label();
            this.lblMapProgressValue = new System.Windows.Forms.Label();
            this.lblTargetCountCaption = new System.Windows.Forms.Label();
            this.lblTargetCountValue = new System.Windows.Forms.Label();
            this.lblDieSizeXCaption = new System.Windows.Forms.Label();
            this.lblDieSizeXValue = new System.Windows.Forms.Label();
            this.lblDieSizeYCaption = new System.Windows.Forms.Label();
            this.lblDieSizeYValue = new System.Windows.Forms.Label();
            this.lblPitchGapXCaption = new System.Windows.Forms.Label();
            this.lblPitchGapXValue = new System.Windows.Forms.Label();
            this.lblPitchGapYCaption = new System.Windows.Forms.Label();
            this.lblPitchGapYValue = new System.Windows.Forms.Label();
            this.lblWaferDiameterCaption = new System.Windows.Forms.Label();
            this.lblWaferDiameterValue = new System.Windows.Forms.Label();
            this.lblInputCameraXCaption = new System.Windows.Forms.Label();
            this.lblInputCameraXValue = new System.Windows.Forms.Label();
            this.lblInputStageYCaption = new System.Windows.Forms.Label();
            this.lblInputStageYValue = new System.Windows.Forms.Label();
            this.lblEquipmentGridCaption = new System.Windows.Forms.Label();
            this.lblEquipmentGridValue = new System.Windows.Forms.Label();
            this.lblOriginalMapCaption = new System.Windows.Forms.Label();
            this.lblOriginalMapValue = new System.Windows.Forms.Label();
            this.lblMappingOriginCaption = new System.Windows.Forms.Label();
            this.lblMappingOriginValue = new System.Windows.Forms.Label();
            this.lblSelectedDieCaption = new System.Windows.Forms.Label();
            this.lblSelectedDieValue = new System.Windows.Forms.Label();
            this.lblSelectedSequenceCaption = new System.Windows.Forms.Label();
            this.lblSelectedSequenceValue = new System.Windows.Forms.Label();
            this.lblSelectedPositionCaption = new System.Windows.Forms.Label();
            this.lblSelectedPositionValue = new System.Windows.Forms.Label();
            this.grpPickupRoute = new System.Windows.Forms.GroupBox();
            this.pickupRouteLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblCornerCaption = new System.Windows.Forms.Label();
            this.cornerOptions = new System.Windows.Forms.FlowLayoutPanel();
            this.rbCornerTopLeft = new System.Windows.Forms.RadioButton();
            this.rbCornerTopRight = new System.Windows.Forms.RadioButton();
            this.rbCornerBottomLeft = new System.Windows.Forms.RadioButton();
            this.rbCornerBottomRight = new System.Windows.Forms.RadioButton();
            this.lblDirectionCaption = new System.Windows.Forms.Label();
            this.directionOptions = new System.Windows.Forms.FlowLayoutPanel();
            this.rbDirectionHorizontal = new System.Windows.Forms.RadioButton();
            this.rbDirectionVertical = new System.Windows.Forms.RadioButton();
            this.lblPatternCaption = new System.Windows.Forms.Label();
            this.patternOptions = new System.Windows.Forms.FlowLayoutPanel();
            this.rbPatternStraight = new System.Windows.Forms.RadioButton();
            this.rbPatternZigZag = new System.Windows.Forms.RadioButton();
            this.btnPreviewPath = new System.Windows.Forms.Button();
            this.btnApplyPickupOrder = new System.Windows.Forms.Button();
            this.grpWorkflow = new System.Windows.Forms.GroupBox();
            this.workflowLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblRevisionCaption = new System.Windows.Forms.Label();
            this.lblMappingRevisionValue = new System.Windows.Forms.Label();
            this.txtFailureDetail = new System.Windows.Forms.TextBox();
            this.btnRetryAlign = new System.Windows.Forms.Button();
            this.btnRetryMapping = new System.Windows.Forms.Button();
            this.btnMappingSetup = new System.Windows.Forms.Button();
            this.btnStartRun = new System.Windows.Forms.Button();
            this.btnAbortAuto = new System.Windows.Forms.Button();
            this.footerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnBuzzerStop = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.headerLayout.SuspendLayout();
            this.bodyLayout.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.leftBottomLayout.SuspendLayout();
            this.grpWaferVision.SuspendLayout();
            this.waferVisionLayout.SuspendLayout();
            this.waferVisionHeaderLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dieGrid)).BeginInit();
            this.centerLayout.SuspendLayout();
            this.grpDieState.SuspendLayout();
            this.dieStateLayout.SuspendLayout();
            this.dieStateOptions.SuspendLayout();
            this.grpStartDie.SuspendLayout();
            this.startDieLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numStartIndex)).BeginInit();
            this.grpJog.SuspendLayout();
            this.jogLayout.SuspendLayout();
            this.grpActions.SuspendLayout();
            this.actionLayout.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.grpMapInfo.SuspendLayout();
            this.mapInfoLayout.SuspendLayout();
            this.grpPickupRoute.SuspendLayout();
            this.pickupRouteLayout.SuspendLayout();
            this.cornerOptions.SuspendLayout();
            this.directionOptions.SuspendLayout();
            this.patternOptions.SuspendLayout();
            this.grpWorkflow.SuspendLayout();
            this.workflowLayout.SuspendLayout();
            this.footerLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(241)))), ((int)(((byte)(245)))));
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.headerLayout, 0, 0);
            this.rootLayout.Controls.Add(this.bodyLayout, 0, 1);
            this.rootLayout.Controls.Add(this.footerLayout, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(8, 8);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 72F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.rootLayout.Size = new System.Drawing.Size(1724, 924);
            this.rootLayout.TabIndex = 0;
            // 
            // headerLayout
            // 
            this.headerLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(48)))), ((int)(((byte)(58)))));
            this.headerLayout.ColumnCount = 14;
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 280F));
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 145F));
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 55F));
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 24F));
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14F));
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 55F));
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14F));
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14F));
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 62F));
            this.headerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14F));
            this.headerLayout.Controls.Add(this.lblTitle, 0, 0);
            this.headerLayout.Controls.Add(this.lblDialogMode, 1, 0);
            this.headerLayout.Controls.Add(this.lblWaferCaption, 2, 0);
            this.headerLayout.Controls.Add(this.lblWaferValue, 3, 0);
            this.headerLayout.Controls.Add(this.lblRecipeCaption, 4, 0);
            this.headerLayout.Controls.Add(this.lblRecipeValue, 5, 0);
            this.headerLayout.Controls.Add(this.lblVisionCaption, 6, 0);
            this.headerLayout.Controls.Add(this.lblVisionValue, 7, 0);
            this.headerLayout.Controls.Add(this.lblAlignCaption, 8, 0);
            this.headerLayout.Controls.Add(this.lblAlignValue, 9, 0);
            this.headerLayout.Controls.Add(this.lblMappingCaption, 10, 0);
            this.headerLayout.Controls.Add(this.lblMappingValue, 11, 0);
            this.headerLayout.Controls.Add(this.lblReviewCaption, 12, 0);
            this.headerLayout.Controls.Add(this.lblReviewValue, 13, 0);
            this.headerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerLayout.Location = new System.Drawing.Point(0, 0);
            this.headerLayout.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.headerLayout.Name = "headerLayout";
            this.headerLayout.RowCount = 1;
            this.headerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headerLayout.Size = new System.Drawing.Size(1724, 66);
            this.headerLayout.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 15F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(3, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.lblTitle.Size = new System.Drawing.Size(274, 66);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "WAFER ALIGN / DIE MAP REVIEW";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDialogMode
            // 
            this.lblDialogMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDialogMode.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblDialogMode.ForeColor = System.Drawing.Color.White;
            this.lblDialogMode.Location = new System.Drawing.Point(286, 14);
            this.lblDialogMode.Margin = new System.Windows.Forms.Padding(6, 14, 6, 14);
            this.lblDialogMode.Name = "lblDialogMode";
            this.lblDialogMode.Size = new System.Drawing.Size(133, 38);
            this.lblDialogMode.TabIndex = 1;
            this.lblDialogMode.Text = "MAPPING REVIEW";
            this.lblDialogMode.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblWaferCaption
            // 
            this.lblWaferCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferCaption.ForeColor = System.Drawing.Color.Silver;
            this.lblWaferCaption.Location = new System.Drawing.Point(428, 0);
            this.lblWaferCaption.Name = "lblWaferCaption";
            this.lblWaferCaption.Size = new System.Drawing.Size(49, 66);
            this.lblWaferCaption.TabIndex = 2;
            this.lblWaferCaption.Text = "WAFER";
            this.lblWaferCaption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblWaferValue
            // 
            this.lblWaferValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferValue.ForeColor = System.Drawing.Color.White;
            this.lblWaferValue.Location = new System.Drawing.Point(483, 0);
            this.lblWaferValue.Name = "lblWaferValue";
            this.lblWaferValue.Size = new System.Drawing.Size(219, 66);
            this.lblWaferValue.TabIndex = 3;
            this.lblWaferValue.Text = "-";
            this.lblWaferValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecipeCaption
            // 
            this.lblRecipeCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRecipeCaption.ForeColor = System.Drawing.Color.Silver;
            this.lblRecipeCaption.Location = new System.Drawing.Point(708, 0);
            this.lblRecipeCaption.Name = "lblRecipeCaption";
            this.lblRecipeCaption.Size = new System.Drawing.Size(54, 66);
            this.lblRecipeCaption.TabIndex = 4;
            this.lblRecipeCaption.Text = "RECIPE";
            this.lblRecipeCaption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblRecipeValue
            // 
            this.lblRecipeValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRecipeValue.ForeColor = System.Drawing.Color.White;
            this.lblRecipeValue.Location = new System.Drawing.Point(768, 0);
            this.lblRecipeValue.Name = "lblRecipeValue";
            this.lblRecipeValue.Size = new System.Drawing.Size(181, 66);
            this.lblRecipeValue.TabIndex = 5;
            this.lblRecipeValue.Text = "-";
            this.lblRecipeValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVisionCaption
            // 
            this.lblVisionCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionCaption.ForeColor = System.Drawing.Color.Silver;
            this.lblVisionCaption.Location = new System.Drawing.Point(955, 0);
            this.lblVisionCaption.Name = "lblVisionCaption";
            this.lblVisionCaption.Size = new System.Drawing.Size(52, 66);
            this.lblVisionCaption.TabIndex = 6;
            this.lblVisionCaption.Text = "VISION";
            this.lblVisionCaption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblVisionValue
            // 
            this.lblVisionValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionValue.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblVisionValue.Location = new System.Drawing.Point(1013, 0);
            this.lblVisionValue.Name = "lblVisionValue";
            this.lblVisionValue.Size = new System.Drawing.Size(125, 66);
            this.lblVisionValue.TabIndex = 7;
            this.lblVisionValue.Text = "-";
            this.lblVisionValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAlignCaption
            // 
            this.lblAlignCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAlignCaption.ForeColor = System.Drawing.Color.Silver;
            this.lblAlignCaption.Location = new System.Drawing.Point(1144, 0);
            this.lblAlignCaption.Name = "lblAlignCaption";
            this.lblAlignCaption.Size = new System.Drawing.Size(49, 66);
            this.lblAlignCaption.TabIndex = 8;
            this.lblAlignCaption.Text = "ALIGN";
            this.lblAlignCaption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblAlignValue
            // 
            this.lblAlignValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAlignValue.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblAlignValue.Location = new System.Drawing.Point(1199, 0);
            this.lblAlignValue.Name = "lblAlignValue";
            this.lblAlignValue.Size = new System.Drawing.Size(125, 66);
            this.lblAlignValue.TabIndex = 9;
            this.lblAlignValue.Text = "-";
            this.lblAlignValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMappingCaption
            // 
            this.lblMappingCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMappingCaption.ForeColor = System.Drawing.Color.Silver;
            this.lblMappingCaption.Location = new System.Drawing.Point(1330, 0);
            this.lblMappingCaption.Name = "lblMappingCaption";
            this.lblMappingCaption.Size = new System.Drawing.Size(64, 66);
            this.lblMappingCaption.TabIndex = 10;
            this.lblMappingCaption.Text = "MAPPING";
            this.lblMappingCaption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblMappingValue
            // 
            this.lblMappingValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMappingValue.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblMappingValue.Location = new System.Drawing.Point(1400, 0);
            this.lblMappingValue.Name = "lblMappingValue";
            this.lblMappingValue.Size = new System.Drawing.Size(125, 66);
            this.lblMappingValue.TabIndex = 11;
            this.lblMappingValue.Text = "-";
            this.lblMappingValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblReviewCaption
            // 
            this.lblReviewCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblReviewCaption.ForeColor = System.Drawing.Color.Silver;
            this.lblReviewCaption.Location = new System.Drawing.Point(1531, 0);
            this.lblReviewCaption.Name = "lblReviewCaption";
            this.lblReviewCaption.Size = new System.Drawing.Size(56, 66);
            this.lblReviewCaption.TabIndex = 12;
            this.lblReviewCaption.Text = "REVIEW";
            this.lblReviewCaption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblReviewValue
            // 
            this.lblReviewValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblReviewValue.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblReviewValue.ForeColor = System.Drawing.Color.Khaki;
            this.lblReviewValue.Location = new System.Drawing.Point(1593, 0);
            this.lblReviewValue.Name = "lblReviewValue";
            this.lblReviewValue.Size = new System.Drawing.Size(128, 66);
            this.lblReviewValue.TabIndex = 13;
            this.lblReviewValue.Text = "REQUIRED";
            this.lblReviewValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // bodyLayout
            // 
            this.bodyLayout.ColumnCount = 3;
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 59F));
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 22F));
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 19F));
            this.bodyLayout.Controls.Add(this.leftLayout, 0, 0);
            this.bodyLayout.Controls.Add(this.centerLayout, 1, 0);
            this.bodyLayout.Controls.Add(this.rightLayout, 2, 0);
            this.bodyLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bodyLayout.Location = new System.Drawing.Point(0, 72);
            this.bodyLayout.Margin = new System.Windows.Forms.Padding(0);
            this.bodyLayout.Name = "bodyLayout";
            this.bodyLayout.RowCount = 1;
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bodyLayout.Size = new System.Drawing.Size(1724, 788);
            this.bodyLayout.TabIndex = 1;
            // 
            // leftLayout
            // 
            this.leftLayout.ColumnCount = 1;
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Controls.Add(this.leftBottomLayout, 0, 0);
            this.leftLayout.Controls.Add(this.dieGrid, 0, 1);
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Location = new System.Drawing.Point(0, 0);
            this.leftLayout.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.RowCount = 2;
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 62F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 38F));
            this.leftLayout.Size = new System.Drawing.Size(1011, 788);
            this.leftLayout.TabIndex = 0;
            // 
            // leftBottomLayout
            // 
            this.leftBottomLayout.ColumnCount = 2;
            this.leftBottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.leftBottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.leftBottomLayout.Controls.Add(this.grpWaferVision, 0, 0);
            this.leftBottomLayout.Controls.Add(this.mapView, 1, 0);
            this.leftBottomLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftBottomLayout.Location = new System.Drawing.Point(0, 0);
            this.leftBottomLayout.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.leftBottomLayout.Name = "leftBottomLayout";
            this.leftBottomLayout.RowCount = 1;
            this.leftBottomLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftBottomLayout.Size = new System.Drawing.Size(1011, 482);
            this.leftBottomLayout.TabIndex = 1;
            // 
            // grpWaferVision
            // 
            this.grpWaferVision.Controls.Add(this.waferVisionLayout);
            this.grpWaferVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpWaferVision.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpWaferVision.Location = new System.Drawing.Point(0, 0);
            this.grpWaferVision.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.grpWaferVision.Name = "grpWaferVision";
            this.grpWaferVision.Padding = new System.Windows.Forms.Padding(6);
            this.grpWaferVision.Size = new System.Drawing.Size(502, 482);
            this.grpWaferVision.TabIndex = 0;
            this.grpWaferVision.TabStop = false;
            this.grpWaferVision.Text = "WAFER VISION";
            // 
            // waferVisionLayout
            // 
            this.waferVisionLayout.ColumnCount = 1;
            this.waferVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.waferVisionLayout.Controls.Add(this.waferVisionHeaderLayout, 0, 0);
            this.waferVisionLayout.Controls.Add(this.waferVisionViewer, 0, 1);
            this.waferVisionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waferVisionLayout.Location = new System.Drawing.Point(6, 22);
            this.waferVisionLayout.Margin = new System.Windows.Forms.Padding(0);
            this.waferVisionLayout.Name = "waferVisionLayout";
            this.waferVisionLayout.RowCount = 2;
            this.waferVisionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.waferVisionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.waferVisionLayout.Size = new System.Drawing.Size(490, 454);
            this.waferVisionLayout.TabIndex = 0;
            // 
            // waferVisionHeaderLayout
            // 
            this.waferVisionHeaderLayout.ColumnCount = 2;
            this.waferVisionHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.waferVisionHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.waferVisionHeaderLayout.Controls.Add(this.lblWaferVisionState, 0, 0);
            this.waferVisionHeaderLayout.Controls.Add(this.btnWaferVisionControl, 1, 0);
            this.waferVisionHeaderLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waferVisionHeaderLayout.Location = new System.Drawing.Point(0, 0);
            this.waferVisionHeaderLayout.Margin = new System.Windows.Forms.Padding(0);
            this.waferVisionHeaderLayout.Name = "waferVisionHeaderLayout";
            this.waferVisionHeaderLayout.RowCount = 1;
            this.waferVisionHeaderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.waferVisionHeaderLayout.Size = new System.Drawing.Size(490, 40);
            this.waferVisionHeaderLayout.TabIndex = 0;
            // 
            // lblWaferVisionState
            // 
            this.lblWaferVisionState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferVisionState.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblWaferVisionState.ForeColor = System.Drawing.Color.DimGray;
            this.lblWaferVisionState.Location = new System.Drawing.Point(3, 0);
            this.lblWaferVisionState.Name = "lblWaferVisionState";
            this.lblWaferVisionState.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblWaferVisionState.Size = new System.Drawing.Size(324, 40);
            this.lblWaferVisionState.TabIndex = 0;
            this.lblWaferVisionState.Text = "영상 수신 대기 (측정 가능)";
            this.lblWaferVisionState.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnWaferVisionControl
            // 
            this.btnWaferVisionControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnWaferVisionControl.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnWaferVisionControl.Location = new System.Drawing.Point(333, 3);
            this.btnWaferVisionControl.Name = "btnWaferVisionControl";
            this.btnWaferVisionControl.Size = new System.Drawing.Size(154, 34);
            this.btnWaferVisionControl.TabIndex = 1;
            this.btnWaferVisionControl.Text = "비전 사용 시작";
            this.btnWaferVisionControl.UseVisualStyleBackColor = true;
            this.btnWaferVisionControl.Click += new System.EventHandler(this.BtnWaferVisionControl_Click);
            // 
            // waferVisionViewer
            // 
            this.waferVisionViewer.AllowLive = false;
            this.waferVisionViewer.CameraCommandsEnabled = false;
            this.waferVisionViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waferVisionViewer.Location = new System.Drawing.Point(0, 40);
            this.waferVisionViewer.Margin = new System.Windows.Forms.Padding(0);
            this.waferVisionViewer.Name = "waferVisionViewer";
            this.waferVisionViewer.Size = new System.Drawing.Size(490, 414);
            this.waferVisionViewer.TabIndex = 1;
            // 
            // mapView
            // 
            this.mapView.BackColor = System.Drawing.Color.White;
            this.mapView.Caption = "Die Map";
            this.mapView.CompactUsedBounds = false;
            this.mapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapView.EnableRectangleSelection = false;
            this.mapView.Location = new System.Drawing.Point(508, 0);
            this.mapView.Map = null;
            this.mapView.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.mapView.Name = "mapView";
            this.mapView.SelectedEntry = null;
            this.mapView.ShowEquipmentAxes = false;
            this.mapView.ShowWaferOutline = false;
            this.mapView.Size = new System.Drawing.Size(503, 482);
            this.mapView.TabIndex = 0;
            this.mapView.CellClicked += new System.Action<QMC.CDT320.DieMaps.DieMapEntry>(this.MapView_CellClicked);
            this.mapView.CellDoubleClicked += new System.Action<QMC.CDT320.DieMaps.DieMapEntry>(this.MapView_CellDoubleClicked);
            this.mapView.SelectionRectangleCompleted += new System.Action<System.Collections.Generic.IReadOnlyList<QMC.CDT320.DieMaps.DieMapEntry>>(this.MapView_SelectionRectangleCompleted);
            // 
            // dieGrid
            // 
            this.dieGrid.AllowUserToAddRows = false;
            this.dieGrid.AllowUserToDeleteRows = false;
            this.dieGrid.AllowUserToResizeRows = false;
            this.dieGrid.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(230)))), ((int)(((byte)(236)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("맑은 고딕", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(48)))), ((int)(((byte)(58)))));
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(230)))), ((int)(((byte)(236)))));
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(48)))), ((int)(((byte)(58)))));
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dieGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dieGrid.ColumnHeadersHeight = 36;
            this.dieGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dieGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSequence,
            this.colMapX,
            this.colMapY,
            this.colGridX,
            this.colGridY,
            this.colOriginalX,
            this.colOriginalY,
            this.colState,
            this.colResult,
            this.colBin,
            this.colPosX,
            this.colPosY,
            this.colDieUid});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("맑은 고딕", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(48)))), ((int)(((byte)(58)))));
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(121)))), ((int)(((byte)(190)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dieGrid.DefaultCellStyle = dataGridViewCellStyle2;
            this.dieGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dieGrid.Location = new System.Drawing.Point(0, 488);
            this.dieGrid.Margin = new System.Windows.Forms.Padding(0);
            this.dieGrid.MultiSelect = false;
            this.dieGrid.Name = "dieGrid";
            this.dieGrid.ReadOnly = true;
            this.dieGrid.RowHeadersVisible = false;
            this.dieGrid.RowHeadersWidth = 51;
            this.dieGrid.RowTemplate.Height = 26;
            this.dieGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dieGrid.Size = new System.Drawing.Size(1011, 300);
            this.dieGrid.TabIndex = 1;
            this.dieGrid.SelectionChanged += new System.EventHandler(this.DieGrid_SelectionChanged);
            // 
            // colSequence
            // 
            this.colSequence.HeaderText = "SEQ";
            this.colSequence.MinimumWidth = 6;
            this.colSequence.Name = "colSequence";
            this.colSequence.ReadOnly = true;
            this.colSequence.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colSequence.Width = 52;
            // 
            // colMapX
            // 
            this.colMapX.HeaderText = "맵 X";
            this.colMapX.MinimumWidth = 6;
            this.colMapX.Name = "colMapX";
            this.colMapX.ReadOnly = true;
            this.colMapX.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colMapX.Width = 55;
            // 
            // colMapY
            // 
            this.colMapY.HeaderText = "맵 Y";
            this.colMapY.MinimumWidth = 6;
            this.colMapY.Name = "colMapY";
            this.colMapY.ReadOnly = true;
            this.colMapY.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colMapY.Width = 55;
            // 
            // colGridX
            // 
            this.colGridX.HeaderText = "GRID X";
            this.colGridX.MinimumWidth = 6;
            this.colGridX.Name = "colGridX";
            this.colGridX.Visible = false;
            this.colGridX.ReadOnly = true;
            this.colGridX.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colGridX.Width = 62;
            // 
            // colGridY
            // 
            this.colGridY.HeaderText = "GRID Y";
            this.colGridY.MinimumWidth = 6;
            this.colGridY.Name = "colGridY";
            this.colGridY.Visible = false;
            this.colGridY.ReadOnly = true;
            this.colGridY.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colGridY.Width = 62;
            // 
            // colOriginalX
            // 
            this.colOriginalX.HeaderText = "ORG X";
            this.colOriginalX.MinimumWidth = 6;
            this.colOriginalX.Name = "colOriginalX";
            this.colOriginalX.Visible = false;
            this.colOriginalX.ReadOnly = true;
            this.colOriginalX.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colOriginalX.Width = 58;
            // 
            // colOriginalY
            // 
            this.colOriginalY.HeaderText = "ORG Y";
            this.colOriginalY.MinimumWidth = 6;
            this.colOriginalY.Name = "colOriginalY";
            this.colOriginalY.Visible = false;
            this.colOriginalY.ReadOnly = true;
            this.colOriginalY.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colOriginalY.Width = 58;
            // 
            // colState
            // 
            this.colState.HeaderText = "STATE";
            this.colState.MinimumWidth = 6;
            this.colState.Name = "colState";
            this.colState.ReadOnly = true;
            this.colState.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colState.Width = 70;
            // 
            // colResult
            // 
            this.colResult.HeaderText = "RESULT";
            this.colResult.MinimumWidth = 6;
            this.colResult.Name = "colResult";
            this.colResult.ReadOnly = true;
            this.colResult.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colResult.Width = 70;
            // 
            // colBin
            // 
            this.colBin.HeaderText = "BIN";
            this.colBin.MinimumWidth = 6;
            this.colBin.Name = "colBin";
            this.colBin.ReadOnly = true;
            this.colBin.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colBin.Width = 45;
            // 
            // colPosX
            // 
            this.colPosX.HeaderText = "MACHINE X";
            this.colPosX.MinimumWidth = 6;
            this.colPosX.Name = "colPosX";
            this.colPosX.Visible = false;
            this.colPosX.ReadOnly = true;
            this.colPosX.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colPosX.Width = 82;
            // 
            // colPosY
            // 
            this.colPosY.HeaderText = "MACHINE Y";
            this.colPosY.MinimumWidth = 6;
            this.colPosY.Name = "colPosY";
            this.colPosY.Visible = false;
            this.colPosY.ReadOnly = true;
            this.colPosY.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colPosY.Width = 82;
            // 
            // colDieUid
            // 
            this.colDieUid.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colDieUid.HeaderText = "DIE UID";
            this.colDieUid.MinimumWidth = 120;
            this.colDieUid.Name = "colDieUid";
            this.colDieUid.ReadOnly = true;
            this.colDieUid.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // centerLayout
            // 
            this.centerLayout.ColumnCount = 1;
            this.centerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.centerLayout.Controls.Add(this.grpDieState, 0, 0);
            this.centerLayout.Controls.Add(this.grpStartDie, 0, 1);
            this.centerLayout.Controls.Add(this.grpJog, 0, 2);
            this.centerLayout.Controls.Add(this.grpActions, 0, 3);
            this.centerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.centerLayout.Location = new System.Drawing.Point(1017, 0);
            this.centerLayout.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.centerLayout.MinimumSize = new System.Drawing.Size(360, 0);
            this.centerLayout.Name = "centerLayout";
            this.centerLayout.RowCount = 4;
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 235F));
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.centerLayout.Size = new System.Drawing.Size(373, 788);
            this.centerLayout.TabIndex = 1;
            // 
            // grpDieState
            // 
            this.grpDieState.Controls.Add(this.dieStateLayout);
            this.grpDieState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpDieState.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpDieState.Location = new System.Drawing.Point(0, 0);
            this.grpDieState.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpDieState.Name = "grpDieState";
            this.grpDieState.Padding = new System.Windows.Forms.Padding(8);
            this.grpDieState.Size = new System.Drawing.Size(373, 174);
            this.grpDieState.TabIndex = 0;
            this.grpDieState.TabStop = false;
            this.grpDieState.Text = "DIE STATE EDIT";
            // 
            // dieStateLayout
            // 
            this.dieStateLayout.ColumnCount = 1;
            this.dieStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.dieStateLayout.Controls.Add(this.dieStateOptions, 0, 0);
            this.dieStateLayout.Controls.Add(this.btnApplyDieState, 0, 1);
            this.dieStateLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dieStateLayout.Location = new System.Drawing.Point(8, 24);
            this.dieStateLayout.Name = "dieStateLayout";
            this.dieStateLayout.RowCount = 2;
            this.dieStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.dieStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.dieStateLayout.Size = new System.Drawing.Size(357, 142);
            this.dieStateLayout.TabIndex = 0;
            // 
            // dieStateOptions
            // 
            this.dieStateOptions.Controls.Add(this.rbDieStateWait);
            this.dieStateOptions.Controls.Add(this.rbDieStateGood);
            this.dieStateOptions.Controls.Add(this.rbDieStateNg);
            this.dieStateOptions.Controls.Add(this.rbDieStateSkip);
            this.dieStateOptions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dieStateOptions.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.dieStateOptions.Location = new System.Drawing.Point(3, 3);
            this.dieStateOptions.Name = "dieStateOptions";
            this.dieStateOptions.Size = new System.Drawing.Size(351, 94);
            this.dieStateOptions.TabIndex = 0;
            this.dieStateOptions.WrapContents = false;
            // 
            // rbDieStateWait
            // 
            this.rbDieStateWait.AutoSize = true;
            this.rbDieStateWait.Checked = true;
            this.rbDieStateWait.Location = new System.Drawing.Point(3, 1);
            this.rbDieStateWait.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.rbDieStateWait.Name = "rbDieStateWait";
            this.rbDieStateWait.Size = new System.Drawing.Size(121, 19);
            this.rbDieStateWait.TabIndex = 0;
            this.rbDieStateWait.TabStop = true;
            this.rbDieStateWait.Text = "WAIT / 검사 대기";
            // 
            // rbDieStateGood
            // 
            this.rbDieStateGood.AutoSize = true;
            this.rbDieStateGood.Location = new System.Drawing.Point(3, 22);
            this.rbDieStateGood.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.rbDieStateGood.Name = "rbDieStateGood";
            this.rbDieStateGood.Size = new System.Drawing.Size(126, 19);
            this.rbDieStateGood.TabIndex = 1;
            this.rbDieStateGood.Text = "GOOD / 검사 완료";
            // 
            // rbDieStateNg
            // 
            this.rbDieStateNg.AutoSize = true;
            this.rbDieStateNg.Location = new System.Drawing.Point(3, 43);
            this.rbDieStateNg.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.rbDieStateNg.Name = "rbDieStateNg";
            this.rbDieStateNg.Size = new System.Drawing.Size(109, 19);
            this.rbDieStateNg.TabIndex = 2;
            this.rbDieStateNg.Text = "NG / 검사 불량";
            // 
            // rbDieStateSkip
            // 
            this.rbDieStateSkip.AutoSize = true;
            this.rbDieStateSkip.Location = new System.Drawing.Point(3, 64);
            this.rbDieStateSkip.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.rbDieStateSkip.Name = "rbDieStateSkip";
            this.rbDieStateSkip.Size = new System.Drawing.Size(88, 19);
            this.rbDieStateSkip.TabIndex = 3;
            this.rbDieStateSkip.Text = "SKIP / 제외";
            // 
            // btnApplyDieState
            // 
            this.btnApplyDieState.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(228)))), ((int)(((byte)(237)))));
            this.btnApplyDieState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApplyDieState.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApplyDieState.Location = new System.Drawing.Point(3, 103);
            this.btnApplyDieState.Name = "btnApplyDieState";
            this.btnApplyDieState.Size = new System.Drawing.Size(351, 36);
            this.btnApplyDieState.TabIndex = 1;
            this.btnApplyDieState.Text = "APPLY SELECTED STATE";
            this.btnApplyDieState.UseVisualStyleBackColor = false;
            this.btnApplyDieState.Click += new System.EventHandler(this.BtnApplyDieState_Click);
            // 
            // grpStartDie
            // 
            this.grpStartDie.Controls.Add(this.startDieLayout);
            this.grpStartDie.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpStartDie.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpStartDie.Location = new System.Drawing.Point(0, 180);
            this.grpStartDie.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpStartDie.Name = "grpStartDie";
            this.grpStartDie.Padding = new System.Windows.Forms.Padding(8);
            this.grpStartDie.Size = new System.Drawing.Size(373, 144);
            this.grpStartDie.TabIndex = 1;
            this.grpStartDie.TabStop = false;
            this.grpStartDie.Text = "START DIE";
            // 
            // startDieLayout
            // 
            this.startDieLayout.ColumnCount = 3;
            this.startDieLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 94F));
            this.startDieLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.startDieLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 92F));
            this.startDieLayout.Controls.Add(this.lblStartDieCaption, 0, 0);
            this.startDieLayout.Controls.Add(this.lblStartDieValue, 1, 0);
            this.startDieLayout.Controls.Add(this.btnSetStartDie, 2, 0);
            this.startDieLayout.Controls.Add(this.lblStartIndexCaption, 0, 1);
            this.startDieLayout.Controls.Add(this.numStartIndex, 1, 1);
            this.startDieLayout.Controls.Add(this.btnSetStartIndex, 2, 1);
            this.startDieLayout.Controls.Add(this.chkUseSelectedStart, 0, 2);
            this.startDieLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.startDieLayout.Location = new System.Drawing.Point(8, 24);
            this.startDieLayout.Name = "startDieLayout";
            this.startDieLayout.RowCount = 3;
            this.startDieLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.startDieLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.startDieLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.startDieLayout.Size = new System.Drawing.Size(357, 112);
            this.startDieLayout.TabIndex = 0;
            // 
            // lblStartDieCaption
            // 
            this.lblStartDieCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStartDieCaption.Location = new System.Drawing.Point(3, 0);
            this.lblStartDieCaption.Name = "lblStartDieCaption";
            this.lblStartDieCaption.Size = new System.Drawing.Size(88, 38);
            this.lblStartDieCaption.TabIndex = 0;
            this.lblStartDieCaption.Text = "Selected Die";
            this.lblStartDieCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStartDieValue
            // 
            this.lblStartDieValue.BackColor = System.Drawing.Color.White;
            this.lblStartDieValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStartDieValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStartDieValue.Location = new System.Drawing.Point(97, 5);
            this.lblStartDieValue.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.lblStartDieValue.Name = "lblStartDieValue";
            this.lblStartDieValue.Size = new System.Drawing.Size(165, 28);
            this.lblStartDieValue.TabIndex = 1;
            this.lblStartDieValue.Text = "NOT SET";
            this.lblStartDieValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnSetStartDie
            // 
            this.btnSetStartDie.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSetStartDie.Location = new System.Drawing.Point(268, 4);
            this.btnSetStartDie.Margin = new System.Windows.Forms.Padding(3, 4, 0, 4);
            this.btnSetStartDie.Name = "btnSetStartDie";
            this.btnSetStartDie.Size = new System.Drawing.Size(89, 30);
            this.btnSetStartDie.TabIndex = 2;
            this.btnSetStartDie.Text = "SET SELECT";
            this.btnSetStartDie.Click += new System.EventHandler(this.BtnSetStartDie_Click);
            // 
            // lblStartIndexCaption
            // 
            this.lblStartIndexCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStartIndexCaption.Location = new System.Drawing.Point(3, 38);
            this.lblStartIndexCaption.Name = "lblStartIndexCaption";
            this.lblStartIndexCaption.Size = new System.Drawing.Size(88, 36);
            this.lblStartIndexCaption.TabIndex = 3;
            this.lblStartIndexCaption.Text = "실행 순번";
            this.lblStartIndexCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numStartIndex
            // 
            this.numStartIndex.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.numStartIndex.Location = new System.Drawing.Point(97, 44);
            this.numStartIndex.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numStartIndex.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numStartIndex.Name = "numStartIndex";
            this.numStartIndex.Size = new System.Drawing.Size(165, 23);
            this.numStartIndex.TabIndex = 4;
            this.numStartIndex.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numStartIndex.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // btnSetStartIndex
            // 
            this.btnSetStartIndex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSetStartIndex.Location = new System.Drawing.Point(268, 42);
            this.btnSetStartIndex.Margin = new System.Windows.Forms.Padding(3, 4, 0, 4);
            this.btnSetStartIndex.Name = "btnSetStartIndex";
            this.btnSetStartIndex.Size = new System.Drawing.Size(89, 28);
            this.btnSetStartIndex.TabIndex = 5;
            this.btnSetStartIndex.Text = "SET INDEX";
            this.btnSetStartIndex.Click += new System.EventHandler(this.BtnSetStartIndex_Click);
            // 
            // chkUseSelectedStart
            // 
            this.chkUseSelectedStart.AutoSize = true;
            this.chkUseSelectedStart.Checked = true;
            this.chkUseSelectedStart.CheckState = System.Windows.Forms.CheckState.Checked;
            this.startDieLayout.SetColumnSpan(this.chkUseSelectedStart, 3);
            this.chkUseSelectedStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkUseSelectedStart.Location = new System.Drawing.Point(3, 77);
            this.chkUseSelectedStart.Name = "chkUseSelectedStart";
            this.chkUseSelectedStart.Size = new System.Drawing.Size(351, 32);
            this.chkUseSelectedStart.TabIndex = 6;
            this.chkUseSelectedStart.Text = "선택 Die를 첫 번째 순서로 사용";
            this.chkUseSelectedStart.CheckedChanged += new System.EventHandler(this.ChkUseSelectedStart_CheckedChanged);
            // 
            // grpJog
            // 
            this.grpJog.Controls.Add(this.jogLayout);
            this.grpJog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpJog.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpJog.Location = new System.Drawing.Point(0, 330);
            this.grpJog.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpJog.Name = "grpJog";
            this.grpJog.Padding = new System.Windows.Forms.Padding(8);
            this.grpJog.Size = new System.Drawing.Size(373, 229);
            this.grpJog.TabIndex = 2;
            this.grpJog.TabStop = false;
            this.grpJog.Text = "INPUT STAGE JOG";
            // 
            // jogLayout
            // 
            this.jogLayout.ColumnCount = 4;
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 26F));
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 24F));
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.jogLayout.Controls.Add(this.lblJogSpeed, 0, 0);
            this.jogLayout.Controls.Add(this.cmbJogSpeed, 1, 0);
            this.jogLayout.Controls.Add(this.lblVisionXCaption, 0, 1);
            this.jogLayout.Controls.Add(this.lblVisionXValue, 1, 1);
            this.jogLayout.Controls.Add(this.btnVisionXMinus, 2, 1);
            this.jogLayout.Controls.Add(this.btnVisionXPlus, 3, 1);
            this.jogLayout.Controls.Add(this.lblWaferYCaption, 0, 2);
            this.jogLayout.Controls.Add(this.lblWaferYValue, 1, 2);
            this.jogLayout.Controls.Add(this.btnWaferYMinus, 2, 2);
            this.jogLayout.Controls.Add(this.btnWaferYPlus, 3, 2);
            this.jogLayout.Controls.Add(this.lblWaferTCaption, 0, 3);
            this.jogLayout.Controls.Add(this.lblWaferTValue, 1, 3);
            this.jogLayout.Controls.Add(this.btnWaferTMinus, 2, 3);
            this.jogLayout.Controls.Add(this.btnWaferTPlus, 3, 3);
            this.jogLayout.Controls.Add(this.btnJogStop, 0, 4);
            this.jogLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogLayout.Location = new System.Drawing.Point(8, 24);
            this.jogLayout.Name = "jogLayout";
            this.jogLayout.RowCount = 5;
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.jogLayout.Size = new System.Drawing.Size(357, 197);
            this.jogLayout.TabIndex = 0;
            // 
            // lblJogSpeed
            // 
            this.lblJogSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblJogSpeed.Location = new System.Drawing.Point(3, 0);
            this.lblJogSpeed.Name = "lblJogSpeed";
            this.lblJogSpeed.Size = new System.Drawing.Size(86, 38);
            this.lblJogSpeed.TabIndex = 0;
            this.lblJogSpeed.Text = "Jog Speed";
            this.lblJogSpeed.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbJogSpeed
            // 
            this.cmbJogSpeed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.jogLayout.SetColumnSpan(this.cmbJogSpeed, 3);
            this.cmbJogSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbJogSpeed.Items.AddRange(new object[] {
            "Fine",
            "Medium",
            "Coarse"});
            this.cmbJogSpeed.Location = new System.Drawing.Point(95, 9);
            this.cmbJogSpeed.Name = "cmbJogSpeed";
            this.cmbJogSpeed.Size = new System.Drawing.Size(259, 23);
            this.cmbJogSpeed.TabIndex = 1;
            // 
            // lblVisionXCaption
            // 
            this.lblVisionXCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionXCaption.Location = new System.Drawing.Point(3, 38);
            this.lblVisionXCaption.Name = "lblVisionXCaption";
            this.lblVisionXCaption.Size = new System.Drawing.Size(86, 39);
            this.lblVisionXCaption.TabIndex = 2;
            this.lblVisionXCaption.Text = "Camera X";
            this.lblVisionXCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVisionXValue
            // 
            this.lblVisionXValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionXValue.Location = new System.Drawing.Point(95, 38);
            this.lblVisionXValue.Name = "lblVisionXValue";
            this.lblVisionXValue.Size = new System.Drawing.Size(79, 39);
            this.lblVisionXValue.TabIndex = 3;
            this.lblVisionXValue.Text = "0.000";
            this.lblVisionXValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnVisionXMinus
            // 
            this.btnVisionXMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnVisionXMinus.Location = new System.Drawing.Point(180, 41);
            this.btnVisionXMinus.Name = "btnVisionXMinus";
            this.btnVisionXMinus.Size = new System.Drawing.Size(83, 33);
            this.btnVisionXMinus.TabIndex = 4;
            this.btnVisionXMinus.Tag = QMC.CDT_320.Ui.Dialogs.InputStageReviewJogAxis.VisionX;
            this.btnVisionXMinus.Text = "X-";
            this.btnVisionXMinus.MouseDown += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseDown);
            this.btnVisionXMinus.MouseUp += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseUp);
            // 
            // btnVisionXPlus
            // 
            this.btnVisionXPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnVisionXPlus.Location = new System.Drawing.Point(269, 41);
            this.btnVisionXPlus.Name = "btnVisionXPlus";
            this.btnVisionXPlus.Size = new System.Drawing.Size(85, 33);
            this.btnVisionXPlus.TabIndex = 5;
            this.btnVisionXPlus.Tag = QMC.CDT_320.Ui.Dialogs.InputStageReviewJogAxis.VisionX;
            this.btnVisionXPlus.Text = "X+";
            this.btnVisionXPlus.MouseDown += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseDown);
            this.btnVisionXPlus.MouseUp += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseUp);
            // 
            // lblWaferYCaption
            // 
            this.lblWaferYCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferYCaption.Location = new System.Drawing.Point(3, 77);
            this.lblWaferYCaption.Name = "lblWaferYCaption";
            this.lblWaferYCaption.Size = new System.Drawing.Size(86, 39);
            this.lblWaferYCaption.TabIndex = 6;
            this.lblWaferYCaption.Text = "Stage Y";
            this.lblWaferYCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWaferYValue
            // 
            this.lblWaferYValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferYValue.Location = new System.Drawing.Point(95, 77);
            this.lblWaferYValue.Name = "lblWaferYValue";
            this.lblWaferYValue.Size = new System.Drawing.Size(79, 39);
            this.lblWaferYValue.TabIndex = 7;
            this.lblWaferYValue.Text = "0.000";
            this.lblWaferYValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnWaferYMinus
            // 
            this.btnWaferYMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnWaferYMinus.Location = new System.Drawing.Point(180, 80);
            this.btnWaferYMinus.Name = "btnWaferYMinus";
            this.btnWaferYMinus.Size = new System.Drawing.Size(83, 33);
            this.btnWaferYMinus.TabIndex = 8;
            this.btnWaferYMinus.Tag = QMC.CDT_320.Ui.Dialogs.InputStageReviewJogAxis.WaferY;
            this.btnWaferYMinus.Text = "Y-";
            this.btnWaferYMinus.MouseDown += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseDown);
            this.btnWaferYMinus.MouseUp += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseUp);
            // 
            // btnWaferYPlus
            // 
            this.btnWaferYPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnWaferYPlus.Location = new System.Drawing.Point(269, 80);
            this.btnWaferYPlus.Name = "btnWaferYPlus";
            this.btnWaferYPlus.Size = new System.Drawing.Size(85, 33);
            this.btnWaferYPlus.TabIndex = 9;
            this.btnWaferYPlus.Tag = QMC.CDT_320.Ui.Dialogs.InputStageReviewJogAxis.WaferY;
            this.btnWaferYPlus.Text = "Y+";
            this.btnWaferYPlus.MouseDown += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseDown);
            this.btnWaferYPlus.MouseUp += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseUp);
            // 
            // lblWaferTCaption
            // 
            this.lblWaferTCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferTCaption.Location = new System.Drawing.Point(3, 116);
            this.lblWaferTCaption.Name = "lblWaferTCaption";
            this.lblWaferTCaption.Size = new System.Drawing.Size(86, 39);
            this.lblWaferTCaption.TabIndex = 10;
            this.lblWaferTCaption.Text = "Stage T";
            this.lblWaferTCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWaferTValue
            // 
            this.lblWaferTValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferTValue.Location = new System.Drawing.Point(95, 116);
            this.lblWaferTValue.Name = "lblWaferTValue";
            this.lblWaferTValue.Size = new System.Drawing.Size(79, 39);
            this.lblWaferTValue.TabIndex = 11;
            this.lblWaferTValue.Text = "0.0000";
            this.lblWaferTValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnWaferTMinus
            // 
            this.btnWaferTMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnWaferTMinus.Location = new System.Drawing.Point(180, 119);
            this.btnWaferTMinus.Name = "btnWaferTMinus";
            this.btnWaferTMinus.Size = new System.Drawing.Size(83, 33);
            this.btnWaferTMinus.TabIndex = 12;
            this.btnWaferTMinus.Tag = QMC.CDT_320.Ui.Dialogs.InputStageReviewJogAxis.WaferT;
            this.btnWaferTMinus.Text = "T-";
            this.btnWaferTMinus.MouseDown += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseDown);
            this.btnWaferTMinus.MouseUp += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseUp);
            // 
            // btnWaferTPlus
            // 
            this.btnWaferTPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnWaferTPlus.Location = new System.Drawing.Point(269, 119);
            this.btnWaferTPlus.Name = "btnWaferTPlus";
            this.btnWaferTPlus.Size = new System.Drawing.Size(85, 33);
            this.btnWaferTPlus.TabIndex = 13;
            this.btnWaferTPlus.Tag = QMC.CDT_320.Ui.Dialogs.InputStageReviewJogAxis.WaferT;
            this.btnWaferTPlus.Text = "T+";
            this.btnWaferTPlus.MouseDown += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseDown);
            this.btnWaferTPlus.MouseUp += new System.Windows.Forms.MouseEventHandler(this.JogButton_MouseUp);
            // 
            // btnJogStop
            // 
            this.btnJogStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(190)))), ((int)(((byte)(76)))), ((int)(((byte)(68)))));
            this.jogLayout.SetColumnSpan(this.btnJogStop, 4);
            this.btnJogStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnJogStop.ForeColor = System.Drawing.Color.White;
            this.btnJogStop.Location = new System.Drawing.Point(3, 158);
            this.btnJogStop.Name = "btnJogStop";
            this.btnJogStop.Size = new System.Drawing.Size(351, 36);
            this.btnJogStop.TabIndex = 14;
            this.btnJogStop.Text = "STOP";
            this.btnJogStop.UseVisualStyleBackColor = false;
            this.btnJogStop.Click += new System.EventHandler(this.BtnJogStop_Click);
            // 
            // grpActions
            // 
            this.grpActions.Controls.Add(this.actionLayout);
            this.grpActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpActions.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpActions.Location = new System.Drawing.Point(0, 565);
            this.grpActions.Margin = new System.Windows.Forms.Padding(0);
            this.grpActions.Name = "grpActions";
            this.grpActions.Padding = new System.Windows.Forms.Padding(8);
            this.grpActions.Size = new System.Drawing.Size(373, 223);
            this.grpActions.TabIndex = 3;
            this.grpActions.TabStop = false;
            this.grpActions.Text = "ACTION";
            // 
            // actionLayout
            // 
            this.actionLayout.ColumnCount = 1;
            this.actionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionLayout.Controls.Add(this.btnMoveSelectedDie, 0, 0);
            this.actionLayout.Controls.Add(this.btnThetaCorrection, 0, 1);
            this.actionLayout.Controls.Add(this.btnDieDetection, 0, 2);
            this.actionLayout.Controls.Add(this.btnOffsetApply, 0, 3);
            this.actionLayout.Controls.Add(this.btnVisionTest, 0, 4);
            this.actionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionLayout.Location = new System.Drawing.Point(8, 24);
            this.actionLayout.Name = "actionLayout";
            this.actionLayout.RowCount = 5;
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.actionLayout.Size = new System.Drawing.Size(357, 191);
            this.actionLayout.TabIndex = 0;
            // 
            // btnMoveSelectedDie
            // 
            this.btnMoveSelectedDie.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMoveSelectedDie.Location = new System.Drawing.Point(3, 3);
            this.btnMoveSelectedDie.Name = "btnMoveSelectedDie";
            this.btnMoveSelectedDie.Size = new System.Drawing.Size(351, 32);
            this.btnMoveSelectedDie.TabIndex = 0;
            this.btnMoveSelectedDie.Text = "MOVE SELECTED DIE";
            this.btnMoveSelectedDie.Click += new System.EventHandler(this.BtnMoveSelectedDie_Click);
            // 
            // btnThetaCorrection
            // 
            this.btnThetaCorrection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnThetaCorrection.Location = new System.Drawing.Point(3, 41);
            this.btnThetaCorrection.Name = "btnThetaCorrection";
            this.btnThetaCorrection.Size = new System.Drawing.Size(351, 32);
            this.btnThetaCorrection.TabIndex = 1;
            this.btnThetaCorrection.Text = "T CORRECTION";
            this.btnThetaCorrection.Click += new System.EventHandler(this.BtnThetaCorrection_Click);
            // 
            // btnDieDetection
            // 
            this.btnDieDetection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDieDetection.Location = new System.Drawing.Point(3, 79);
            this.btnDieDetection.Name = "btnDieDetection";
            this.btnDieDetection.Size = new System.Drawing.Size(351, 32);
            this.btnDieDetection.TabIndex = 2;
            this.btnDieDetection.Text = "DIE DETECTION";
            this.btnDieDetection.Click += new System.EventHandler(this.BtnDieDetection_Click);
            // 
            // btnOffsetApply
            // 
            this.btnOffsetApply.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOffsetApply.Location = new System.Drawing.Point(3, 117);
            this.btnOffsetApply.Name = "btnOffsetApply";
            this.btnOffsetApply.Size = new System.Drawing.Size(351, 32);
            this.btnOffsetApply.TabIndex = 3;
            this.btnOffsetApply.Text = "APPLY OFFSET";
            this.btnOffsetApply.Click += new System.EventHandler(this.BtnOffsetApply_Click);
            // 
            // btnVisionTest
            // 
            this.btnVisionTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnVisionTest.Location = new System.Drawing.Point(3, 155);
            this.btnVisionTest.Name = "btnVisionTest";
            this.btnVisionTest.Size = new System.Drawing.Size(351, 33);
            this.btnVisionTest.TabIndex = 4;
            this.btnVisionTest.Text = "VISION TEST";
            this.btnVisionTest.Click += new System.EventHandler(this.BtnVisionTest_Click);
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.grpMapInfo, 0, 0);
            this.rightLayout.Controls.Add(this.grpPickupRoute, 0, 1);
            this.rightLayout.Controls.Add(this.grpWorkflow, 0, 2);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(1396, 0);
            this.rightLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rightLayout.MinimumSize = new System.Drawing.Size(320, 0);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 3;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 290F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 230F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Size = new System.Drawing.Size(328, 788);
            this.rightLayout.TabIndex = 2;
            // 
            // grpMapInfo
            // 
            this.grpMapInfo.Controls.Add(this.mapInfoLayout);
            this.grpMapInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMapInfo.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpMapInfo.Location = new System.Drawing.Point(0, 0);
            this.grpMapInfo.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpMapInfo.Name = "grpMapInfo";
            this.grpMapInfo.Padding = new System.Windows.Forms.Padding(8);
            this.grpMapInfo.Size = new System.Drawing.Size(328, 284);
            this.grpMapInfo.TabIndex = 0;
            this.grpMapInfo.TabStop = false;
            this.grpMapInfo.Text = "DIE MAP INFO";
            // 
            // mapInfoLayout
            // 
            this.mapInfoLayout.ColumnCount = 2;
            this.mapInfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 44F));
            this.mapInfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 56F));
            this.mapInfoLayout.Controls.Add(this.lblMapGridCaption, 0, 0);
            this.mapInfoLayout.Controls.Add(this.lblMapGridValue, 1, 0);
            this.mapInfoLayout.Controls.Add(this.lblMapProgressCaption, 0, 1);
            this.mapInfoLayout.Controls.Add(this.lblMapProgressValue, 1, 1);
            this.mapInfoLayout.Controls.Add(this.lblTargetCountCaption, 0, 2);
            this.mapInfoLayout.Controls.Add(this.lblTargetCountValue, 1, 2);
            this.mapInfoLayout.Controls.Add(this.lblDieSizeXCaption, 0, 3);
            this.mapInfoLayout.Controls.Add(this.lblDieSizeXValue, 1, 3);
            this.mapInfoLayout.Controls.Add(this.lblDieSizeYCaption, 0, 4);
            this.mapInfoLayout.Controls.Add(this.lblDieSizeYValue, 1, 4);
            this.mapInfoLayout.Controls.Add(this.lblPitchGapXCaption, 0, 5);
            this.mapInfoLayout.Controls.Add(this.lblPitchGapXValue, 1, 5);
            this.mapInfoLayout.Controls.Add(this.lblPitchGapYCaption, 0, 6);
            this.mapInfoLayout.Controls.Add(this.lblPitchGapYValue, 1, 6);
            this.mapInfoLayout.Controls.Add(this.lblWaferDiameterCaption, 0, 7);
            this.mapInfoLayout.Controls.Add(this.lblWaferDiameterValue, 1, 7);
            this.mapInfoLayout.Controls.Add(this.lblInputCameraXCaption, 0, 8);
            this.mapInfoLayout.Controls.Add(this.lblInputCameraXValue, 1, 8);
            this.mapInfoLayout.Controls.Add(this.lblInputStageYCaption, 0, 9);
            this.mapInfoLayout.Controls.Add(this.lblInputStageYValue, 1, 9);
            this.mapInfoLayout.Controls.Add(this.lblEquipmentGridCaption, 0, 10);
            this.mapInfoLayout.Controls.Add(this.lblEquipmentGridValue, 1, 10);
            this.mapInfoLayout.Controls.Add(this.lblOriginalMapCaption, 0, 11);
            this.mapInfoLayout.Controls.Add(this.lblOriginalMapValue, 1, 11);
            this.mapInfoLayout.Controls.Add(this.lblMappingOriginCaption, 0, 12);
            this.mapInfoLayout.Controls.Add(this.lblMappingOriginValue, 1, 12);
            this.mapInfoLayout.Controls.Add(this.lblSelectedDieCaption, 0, 13);
            this.mapInfoLayout.Controls.Add(this.lblSelectedDieValue, 1, 13);
            this.mapInfoLayout.Controls.Add(this.lblSelectedSequenceCaption, 0, 14);
            this.mapInfoLayout.Controls.Add(this.lblSelectedSequenceValue, 1, 14);
            this.mapInfoLayout.Controls.Add(this.lblSelectedPositionCaption, 0, 15);
            this.mapInfoLayout.Controls.Add(this.lblSelectedPositionValue, 1, 15);
            this.mapInfoLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapInfoLayout.Location = new System.Drawing.Point(8, 24);
            this.mapInfoLayout.Name = "mapInfoLayout";
            this.mapInfoLayout.RowCount = 16;
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.mapInfoLayout.Size = new System.Drawing.Size(312, 252);
            this.mapInfoLayout.TabIndex = 0;
            // 
            // lblMapGridCaption
            // 
            this.lblMapGridCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblMapGridCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMapGridCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMapGridCaption.Location = new System.Drawing.Point(0, 0);
            this.lblMapGridCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblMapGridCaption.Name = "lblMapGridCaption";
            this.lblMapGridCaption.Size = new System.Drawing.Size(137, 15);
            this.lblMapGridCaption.TabIndex = 0;
            this.lblMapGridCaption.Text = "Grid X/Y";
            this.lblMapGridCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMapGridValue
            // 
            this.lblMapGridValue.BackColor = System.Drawing.Color.White;
            this.lblMapGridValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMapGridValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMapGridValue.Location = new System.Drawing.Point(137, 0);
            this.lblMapGridValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblMapGridValue.Name = "lblMapGridValue";
            this.lblMapGridValue.Size = new System.Drawing.Size(175, 15);
            this.lblMapGridValue.TabIndex = 1;
            this.lblMapGridValue.Text = "-";
            this.lblMapGridValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblMapProgressCaption
            // 
            this.lblMapProgressCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblMapProgressCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMapProgressCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMapProgressCaption.Location = new System.Drawing.Point(0, 15);
            this.lblMapProgressCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblMapProgressCaption.Name = "lblMapProgressCaption";
            this.lblMapProgressCaption.Size = new System.Drawing.Size(137, 15);
            this.lblMapProgressCaption.TabIndex = 2;
            this.lblMapProgressCaption.Text = "Progress";
            this.lblMapProgressCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMapProgressValue
            // 
            this.lblMapProgressValue.BackColor = System.Drawing.Color.White;
            this.lblMapProgressValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMapProgressValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMapProgressValue.Location = new System.Drawing.Point(137, 15);
            this.lblMapProgressValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblMapProgressValue.Name = "lblMapProgressValue";
            this.lblMapProgressValue.Size = new System.Drawing.Size(175, 15);
            this.lblMapProgressValue.TabIndex = 3;
            this.lblMapProgressValue.Text = "-";
            this.lblMapProgressValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTargetCountCaption
            // 
            this.lblTargetCountCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblTargetCountCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblTargetCountCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTargetCountCaption.Location = new System.Drawing.Point(0, 30);
            this.lblTargetCountCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblTargetCountCaption.Name = "lblTargetCountCaption";
            this.lblTargetCountCaption.Size = new System.Drawing.Size(137, 15);
            this.lblTargetCountCaption.TabIndex = 4;
            this.lblTargetCountCaption.Text = "Target Count";
            this.lblTargetCountCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblTargetCountValue
            // 
            this.lblTargetCountValue.BackColor = System.Drawing.Color.White;
            this.lblTargetCountValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblTargetCountValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTargetCountValue.Location = new System.Drawing.Point(137, 30);
            this.lblTargetCountValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblTargetCountValue.Name = "lblTargetCountValue";
            this.lblTargetCountValue.Size = new System.Drawing.Size(175, 15);
            this.lblTargetCountValue.TabIndex = 5;
            this.lblTargetCountValue.Text = "-";
            this.lblTargetCountValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblDieSizeXCaption
            // 
            this.lblDieSizeXCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblDieSizeXCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDieSizeXCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieSizeXCaption.Location = new System.Drawing.Point(0, 45);
            this.lblDieSizeXCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblDieSizeXCaption.Name = "lblDieSizeXCaption";
            this.lblDieSizeXCaption.Size = new System.Drawing.Size(137, 15);
            this.lblDieSizeXCaption.TabIndex = 6;
            this.lblDieSizeXCaption.Text = "Die Size X (mm)";
            this.lblDieSizeXCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDieSizeXValue
            // 
            this.lblDieSizeXValue.BackColor = System.Drawing.Color.White;
            this.lblDieSizeXValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDieSizeXValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieSizeXValue.Location = new System.Drawing.Point(137, 45);
            this.lblDieSizeXValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblDieSizeXValue.Name = "lblDieSizeXValue";
            this.lblDieSizeXValue.Size = new System.Drawing.Size(175, 15);
            this.lblDieSizeXValue.TabIndex = 7;
            this.lblDieSizeXValue.Text = "-";
            this.lblDieSizeXValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblDieSizeYCaption
            // 
            this.lblDieSizeYCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblDieSizeYCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDieSizeYCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieSizeYCaption.Location = new System.Drawing.Point(0, 60);
            this.lblDieSizeYCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblDieSizeYCaption.Name = "lblDieSizeYCaption";
            this.lblDieSizeYCaption.Size = new System.Drawing.Size(137, 15);
            this.lblDieSizeYCaption.TabIndex = 8;
            this.lblDieSizeYCaption.Text = "Die Size Y (mm)";
            this.lblDieSizeYCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDieSizeYValue
            // 
            this.lblDieSizeYValue.BackColor = System.Drawing.Color.White;
            this.lblDieSizeYValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDieSizeYValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieSizeYValue.Location = new System.Drawing.Point(137, 60);
            this.lblDieSizeYValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblDieSizeYValue.Name = "lblDieSizeYValue";
            this.lblDieSizeYValue.Size = new System.Drawing.Size(175, 15);
            this.lblDieSizeYValue.TabIndex = 9;
            this.lblDieSizeYValue.Text = "-";
            this.lblDieSizeYValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPitchGapXCaption
            // 
            this.lblPitchGapXCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblPitchGapXCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchGapXCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchGapXCaption.Location = new System.Drawing.Point(0, 75);
            this.lblPitchGapXCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblPitchGapXCaption.Name = "lblPitchGapXCaption";
            this.lblPitchGapXCaption.Size = new System.Drawing.Size(137, 15);
            this.lblPitchGapXCaption.TabIndex = 10;
            this.lblPitchGapXCaption.Text = "Pitch Gap X (mm)";
            this.lblPitchGapXCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPitchGapXValue
            // 
            this.lblPitchGapXValue.BackColor = System.Drawing.Color.White;
            this.lblPitchGapXValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchGapXValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchGapXValue.Location = new System.Drawing.Point(137, 75);
            this.lblPitchGapXValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblPitchGapXValue.Name = "lblPitchGapXValue";
            this.lblPitchGapXValue.Size = new System.Drawing.Size(175, 15);
            this.lblPitchGapXValue.TabIndex = 11;
            this.lblPitchGapXValue.Text = "-";
            this.lblPitchGapXValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPitchGapYCaption
            // 
            this.lblPitchGapYCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblPitchGapYCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchGapYCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchGapYCaption.Location = new System.Drawing.Point(0, 90);
            this.lblPitchGapYCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblPitchGapYCaption.Name = "lblPitchGapYCaption";
            this.lblPitchGapYCaption.Size = new System.Drawing.Size(137, 15);
            this.lblPitchGapYCaption.TabIndex = 12;
            this.lblPitchGapYCaption.Text = "Pitch Gap Y (mm)";
            this.lblPitchGapYCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPitchGapYValue
            // 
            this.lblPitchGapYValue.BackColor = System.Drawing.Color.White;
            this.lblPitchGapYValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchGapYValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchGapYValue.Location = new System.Drawing.Point(137, 90);
            this.lblPitchGapYValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblPitchGapYValue.Name = "lblPitchGapYValue";
            this.lblPitchGapYValue.Size = new System.Drawing.Size(175, 15);
            this.lblPitchGapYValue.TabIndex = 13;
            this.lblPitchGapYValue.Text = "-";
            this.lblPitchGapYValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblWaferDiameterCaption
            // 
            this.lblWaferDiameterCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblWaferDiameterCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWaferDiameterCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferDiameterCaption.Location = new System.Drawing.Point(0, 105);
            this.lblWaferDiameterCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblWaferDiameterCaption.Name = "lblWaferDiameterCaption";
            this.lblWaferDiameterCaption.Size = new System.Drawing.Size(137, 15);
            this.lblWaferDiameterCaption.TabIndex = 14;
            this.lblWaferDiameterCaption.Text = "Wafer Diameter";
            this.lblWaferDiameterCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWaferDiameterValue
            // 
            this.lblWaferDiameterValue.BackColor = System.Drawing.Color.White;
            this.lblWaferDiameterValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWaferDiameterValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferDiameterValue.Location = new System.Drawing.Point(137, 105);
            this.lblWaferDiameterValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblWaferDiameterValue.Name = "lblWaferDiameterValue";
            this.lblWaferDiameterValue.Size = new System.Drawing.Size(175, 15);
            this.lblWaferDiameterValue.TabIndex = 15;
            this.lblWaferDiameterValue.Text = "-";
            this.lblWaferDiameterValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblInputCameraXCaption
            // 
            this.lblInputCameraXCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblInputCameraXCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblInputCameraXCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInputCameraXCaption.Location = new System.Drawing.Point(0, 120);
            this.lblInputCameraXCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblInputCameraXCaption.Name = "lblInputCameraXCaption";
            this.lblInputCameraXCaption.Size = new System.Drawing.Size(137, 15);
            this.lblInputCameraXCaption.TabIndex = 16;
            this.lblInputCameraXCaption.Text = "Input Camera X";
            this.lblInputCameraXCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblInputCameraXValue
            // 
            this.lblInputCameraXValue.BackColor = System.Drawing.Color.White;
            this.lblInputCameraXValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblInputCameraXValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInputCameraXValue.Location = new System.Drawing.Point(137, 120);
            this.lblInputCameraXValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblInputCameraXValue.Name = "lblInputCameraXValue";
            this.lblInputCameraXValue.Size = new System.Drawing.Size(175, 15);
            this.lblInputCameraXValue.TabIndex = 17;
            this.lblInputCameraXValue.Text = "-";
            this.lblInputCameraXValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblInputStageYCaption
            // 
            this.lblInputStageYCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblInputStageYCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblInputStageYCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInputStageYCaption.Location = new System.Drawing.Point(0, 135);
            this.lblInputStageYCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblInputStageYCaption.Name = "lblInputStageYCaption";
            this.lblInputStageYCaption.Size = new System.Drawing.Size(137, 15);
            this.lblInputStageYCaption.TabIndex = 18;
            this.lblInputStageYCaption.Text = "Input Stage Y";
            this.lblInputStageYCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblInputStageYValue
            // 
            this.lblInputStageYValue.BackColor = System.Drawing.Color.White;
            this.lblInputStageYValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblInputStageYValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInputStageYValue.Location = new System.Drawing.Point(137, 135);
            this.lblInputStageYValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblInputStageYValue.Name = "lblInputStageYValue";
            this.lblInputStageYValue.Size = new System.Drawing.Size(175, 15);
            this.lblInputStageYValue.TabIndex = 19;
            this.lblInputStageYValue.Text = "-";
            this.lblInputStageYValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblEquipmentGridCaption
            // 
            this.lblEquipmentGridCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblEquipmentGridCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblEquipmentGridCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEquipmentGridCaption.Location = new System.Drawing.Point(0, 150);
            this.lblEquipmentGridCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblEquipmentGridCaption.Name = "lblEquipmentGridCaption";
            this.lblEquipmentGridCaption.Size = new System.Drawing.Size(137, 15);
            this.lblEquipmentGridCaption.TabIndex = 20;
            this.lblEquipmentGridCaption.Text = "맵 X/Y";
            this.lblEquipmentGridCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblEquipmentGridValue
            // 
            this.lblEquipmentGridValue.BackColor = System.Drawing.Color.White;
            this.lblEquipmentGridValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblEquipmentGridValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEquipmentGridValue.Location = new System.Drawing.Point(137, 150);
            this.lblEquipmentGridValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblEquipmentGridValue.Name = "lblEquipmentGridValue";
            this.lblEquipmentGridValue.Size = new System.Drawing.Size(175, 15);
            this.lblEquipmentGridValue.TabIndex = 21;
            this.lblEquipmentGridValue.Text = "-";
            this.lblEquipmentGridValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblOriginalMapCaption
            // 
            this.lblOriginalMapCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblOriginalMapCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOriginalMapCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOriginalMapCaption.Location = new System.Drawing.Point(0, 165);
            this.lblOriginalMapCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblOriginalMapCaption.Name = "lblOriginalMapCaption";
            this.lblOriginalMapCaption.Size = new System.Drawing.Size(137, 15);
            this.lblOriginalMapCaption.TabIndex = 22;
            this.lblOriginalMapCaption.Text = "BIN";
            this.lblOriginalMapCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOriginalMapValue
            // 
            this.lblOriginalMapValue.BackColor = System.Drawing.Color.White;
            this.lblOriginalMapValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOriginalMapValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOriginalMapValue.Location = new System.Drawing.Point(137, 165);
            this.lblOriginalMapValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblOriginalMapValue.Name = "lblOriginalMapValue";
            this.lblOriginalMapValue.Size = new System.Drawing.Size(175, 15);
            this.lblOriginalMapValue.TabIndex = 23;
            this.lblOriginalMapValue.Text = "-";
            this.lblOriginalMapValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblMappingOriginCaption
            // 
            this.lblMappingOriginCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblMappingOriginCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMappingOriginCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMappingOriginCaption.Location = new System.Drawing.Point(0, 180);
            this.lblMappingOriginCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblMappingOriginCaption.Name = "lblMappingOriginCaption";
            this.lblMappingOriginCaption.Size = new System.Drawing.Size(137, 15);
            this.lblMappingOriginCaption.TabIndex = 24;
            this.lblMappingOriginCaption.Text = "맵 원점";
            this.lblMappingOriginCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMappingOriginValue
            // 
            this.lblMappingOriginValue.BackColor = System.Drawing.Color.White;
            this.lblMappingOriginValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMappingOriginValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMappingOriginValue.Location = new System.Drawing.Point(137, 180);
            this.lblMappingOriginValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblMappingOriginValue.Name = "lblMappingOriginValue";
            this.lblMappingOriginValue.Size = new System.Drawing.Size(175, 15);
            this.lblMappingOriginValue.TabIndex = 25;
            this.lblMappingOriginValue.Text = "-";
            this.lblMappingOriginValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblSelectedDieCaption
            // 
            this.lblSelectedDieCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblSelectedDieCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSelectedDieCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelectedDieCaption.Location = new System.Drawing.Point(0, 195);
            this.lblSelectedDieCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblSelectedDieCaption.Name = "lblSelectedDieCaption";
            this.lblSelectedDieCaption.Size = new System.Drawing.Size(137, 15);
            this.lblSelectedDieCaption.TabIndex = 26;
            this.lblSelectedDieCaption.Text = "Selected Die UID";
            this.lblSelectedDieCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSelectedDieValue
            // 
            this.lblSelectedDieValue.BackColor = System.Drawing.Color.White;
            this.lblSelectedDieValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSelectedDieValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelectedDieValue.Location = new System.Drawing.Point(137, 195);
            this.lblSelectedDieValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblSelectedDieValue.Name = "lblSelectedDieValue";
            this.lblSelectedDieValue.Size = new System.Drawing.Size(175, 15);
            this.lblSelectedDieValue.TabIndex = 27;
            this.lblSelectedDieValue.Text = "-";
            this.lblSelectedDieValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblSelectedSequenceCaption
            // 
            this.lblSelectedSequenceCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblSelectedSequenceCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSelectedSequenceCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelectedSequenceCaption.Location = new System.Drawing.Point(0, 210);
            this.lblSelectedSequenceCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblSelectedSequenceCaption.Name = "lblSelectedSequenceCaption";
            this.lblSelectedSequenceCaption.Size = new System.Drawing.Size(137, 15);
            this.lblSelectedSequenceCaption.TabIndex = 28;
            this.lblSelectedSequenceCaption.Text = "Selected Sequence";
            this.lblSelectedSequenceCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSelectedSequenceValue
            // 
            this.lblSelectedSequenceValue.BackColor = System.Drawing.Color.White;
            this.lblSelectedSequenceValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSelectedSequenceValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelectedSequenceValue.Location = new System.Drawing.Point(137, 210);
            this.lblSelectedSequenceValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblSelectedSequenceValue.Name = "lblSelectedSequenceValue";
            this.lblSelectedSequenceValue.Size = new System.Drawing.Size(175, 15);
            this.lblSelectedSequenceValue.TabIndex = 29;
            this.lblSelectedSequenceValue.Text = "-";
            this.lblSelectedSequenceValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblSelectedPositionCaption
            // 
            this.lblSelectedPositionCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(229)))), ((int)(((byte)(235)))));
            this.lblSelectedPositionCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSelectedPositionCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelectedPositionCaption.Location = new System.Drawing.Point(0, 225);
            this.lblSelectedPositionCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblSelectedPositionCaption.Name = "lblSelectedPositionCaption";
            this.lblSelectedPositionCaption.Size = new System.Drawing.Size(137, 27);
            this.lblSelectedPositionCaption.TabIndex = 30;
            this.lblSelectedPositionCaption.Text = "상태";
            this.lblSelectedPositionCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSelectedPositionValue
            // 
            this.lblSelectedPositionValue.BackColor = System.Drawing.Color.White;
            this.lblSelectedPositionValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSelectedPositionValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelectedPositionValue.Location = new System.Drawing.Point(137, 225);
            this.lblSelectedPositionValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblSelectedPositionValue.Name = "lblSelectedPositionValue";
            this.lblSelectedPositionValue.Size = new System.Drawing.Size(175, 27);
            this.lblSelectedPositionValue.TabIndex = 31;
            this.lblSelectedPositionValue.Text = "-";
            this.lblSelectedPositionValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // grpPickupRoute
            // 
            this.grpPickupRoute.Controls.Add(this.pickupRouteLayout);
            this.grpPickupRoute.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPickupRoute.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpPickupRoute.Location = new System.Drawing.Point(0, 290);
            this.grpPickupRoute.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpPickupRoute.Name = "grpPickupRoute";
            this.grpPickupRoute.Padding = new System.Windows.Forms.Padding(8);
            this.grpPickupRoute.Size = new System.Drawing.Size(328, 224);
            this.grpPickupRoute.TabIndex = 1;
            this.grpPickupRoute.TabStop = false;
            this.grpPickupRoute.Text = "PICKUP ROUTE";
            // 
            // pickupRouteLayout
            // 
            this.pickupRouteLayout.ColumnCount = 2;
            this.pickupRouteLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 82F));
            this.pickupRouteLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pickupRouteLayout.Controls.Add(this.lblCornerCaption, 0, 0);
            this.pickupRouteLayout.Controls.Add(this.cornerOptions, 1, 0);
            this.pickupRouteLayout.Controls.Add(this.lblDirectionCaption, 0, 1);
            this.pickupRouteLayout.Controls.Add(this.directionOptions, 1, 1);
            this.pickupRouteLayout.Controls.Add(this.lblPatternCaption, 0, 2);
            this.pickupRouteLayout.Controls.Add(this.patternOptions, 1, 2);
            this.pickupRouteLayout.Controls.Add(this.btnPreviewPath, 0, 3);
            this.pickupRouteLayout.Controls.Add(this.btnApplyPickupOrder, 0, 4);
            this.pickupRouteLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pickupRouteLayout.Location = new System.Drawing.Point(8, 24);
            this.pickupRouteLayout.Name = "pickupRouteLayout";
            this.pickupRouteLayout.RowCount = 5;
            this.pickupRouteLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.pickupRouteLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.pickupRouteLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.pickupRouteLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.pickupRouteLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pickupRouteLayout.Size = new System.Drawing.Size(312, 192);
            this.pickupRouteLayout.TabIndex = 0;
            // 
            // lblCornerCaption
            // 
            this.lblCornerCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCornerCaption.Location = new System.Drawing.Point(3, 0);
            this.lblCornerCaption.Name = "lblCornerCaption";
            this.lblCornerCaption.Size = new System.Drawing.Size(76, 48);
            this.lblCornerCaption.TabIndex = 0;
            this.lblCornerCaption.Text = "Start Corner";
            this.lblCornerCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cornerOptions
            // 
            this.cornerOptions.Controls.Add(this.rbCornerTopLeft);
            this.cornerOptions.Controls.Add(this.rbCornerTopRight);
            this.cornerOptions.Controls.Add(this.rbCornerBottomLeft);
            this.cornerOptions.Controls.Add(this.rbCornerBottomRight);
            this.cornerOptions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cornerOptions.Location = new System.Drawing.Point(85, 3);
            this.cornerOptions.Name = "cornerOptions";
            this.cornerOptions.Size = new System.Drawing.Size(224, 42);
            this.cornerOptions.TabIndex = 1;
            // 
            // rbCornerTopLeft
            // 
            this.rbCornerTopLeft.AutoSize = true;
            this.rbCornerTopLeft.Location = new System.Drawing.Point(3, 3);
            this.rbCornerTopLeft.Name = "rbCornerTopLeft";
            this.rbCornerTopLeft.Size = new System.Drawing.Size(77, 19);
            this.rbCornerTopLeft.TabIndex = 0;
            this.rbCornerTopLeft.Text = "TOP LEFT";
            this.rbCornerTopLeft.CheckedChanged += new System.EventHandler(this.PickupOption_CheckedChanged);
            // 
            // rbCornerTopRight
            // 
            this.rbCornerTopRight.AutoSize = true;
            this.rbCornerTopRight.Checked = true;
            this.rbCornerTopRight.Location = new System.Drawing.Point(86, 3);
            this.rbCornerTopRight.Name = "rbCornerTopRight";
            this.rbCornerTopRight.Size = new System.Drawing.Size(89, 19);
            this.rbCornerTopRight.TabIndex = 1;
            this.rbCornerTopRight.TabStop = true;
            this.rbCornerTopRight.Text = "TOP RIGHT";
            this.rbCornerTopRight.CheckedChanged += new System.EventHandler(this.PickupOption_CheckedChanged);
            // 
            // rbCornerBottomLeft
            // 
            this.rbCornerBottomLeft.AutoSize = true;
            this.rbCornerBottomLeft.Location = new System.Drawing.Point(3, 28);
            this.rbCornerBottomLeft.Name = "rbCornerBottomLeft";
            this.rbCornerBottomLeft.Size = new System.Drawing.Size(106, 19);
            this.rbCornerBottomLeft.TabIndex = 2;
            this.rbCornerBottomLeft.Text = "BOTTOM LEFT";
            this.rbCornerBottomLeft.CheckedChanged += new System.EventHandler(this.PickupOption_CheckedChanged);
            // 
            // rbCornerBottomRight
            // 
            this.rbCornerBottomRight.AutoSize = true;
            this.rbCornerBottomRight.Location = new System.Drawing.Point(3, 53);
            this.rbCornerBottomRight.Name = "rbCornerBottomRight";
            this.rbCornerBottomRight.Size = new System.Drawing.Size(118, 19);
            this.rbCornerBottomRight.TabIndex = 3;
            this.rbCornerBottomRight.Text = "BOTTOM RIGHT";
            this.rbCornerBottomRight.CheckedChanged += new System.EventHandler(this.PickupOption_CheckedChanged);
            // 
            // lblDirectionCaption
            // 
            this.lblDirectionCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDirectionCaption.Location = new System.Drawing.Point(3, 48);
            this.lblDirectionCaption.Name = "lblDirectionCaption";
            this.lblDirectionCaption.Size = new System.Drawing.Size(76, 34);
            this.lblDirectionCaption.TabIndex = 2;
            this.lblDirectionCaption.Text = "Direction";
            this.lblDirectionCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // directionOptions
            // 
            this.directionOptions.Controls.Add(this.rbDirectionHorizontal);
            this.directionOptions.Controls.Add(this.rbDirectionVertical);
            this.directionOptions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.directionOptions.Location = new System.Drawing.Point(85, 51);
            this.directionOptions.Name = "directionOptions";
            this.directionOptions.Size = new System.Drawing.Size(224, 28);
            this.directionOptions.TabIndex = 3;
            // 
            // rbDirectionHorizontal
            // 
            this.rbDirectionHorizontal.AutoSize = true;
            this.rbDirectionHorizontal.Location = new System.Drawing.Point(3, 3);
            this.rbDirectionHorizontal.Name = "rbDirectionHorizontal";
            this.rbDirectionHorizontal.Size = new System.Drawing.Size(102, 19);
            this.rbDirectionHorizontal.TabIndex = 0;
            this.rbDirectionHorizontal.Text = "HORIZONTAL";
            this.rbDirectionHorizontal.CheckedChanged += new System.EventHandler(this.PickupOption_CheckedChanged);
            // 
            // rbDirectionVertical
            // 
            this.rbDirectionVertical.AutoSize = true;
            this.rbDirectionVertical.Checked = true;
            this.rbDirectionVertical.Location = new System.Drawing.Point(111, 3);
            this.rbDirectionVertical.Name = "rbDirectionVertical";
            this.rbDirectionVertical.Size = new System.Drawing.Size(80, 19);
            this.rbDirectionVertical.TabIndex = 1;
            this.rbDirectionVertical.TabStop = true;
            this.rbDirectionVertical.Text = "VERTICAL";
            this.rbDirectionVertical.CheckedChanged += new System.EventHandler(this.PickupOption_CheckedChanged);
            // 
            // lblPatternCaption
            // 
            this.lblPatternCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPatternCaption.Location = new System.Drawing.Point(3, 82);
            this.lblPatternCaption.Name = "lblPatternCaption";
            this.lblPatternCaption.Size = new System.Drawing.Size(76, 34);
            this.lblPatternCaption.TabIndex = 4;
            this.lblPatternCaption.Text = "Pattern";
            this.lblPatternCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // patternOptions
            // 
            this.patternOptions.Controls.Add(this.rbPatternStraight);
            this.patternOptions.Controls.Add(this.rbPatternZigZag);
            this.patternOptions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.patternOptions.Location = new System.Drawing.Point(85, 85);
            this.patternOptions.Name = "patternOptions";
            this.patternOptions.Size = new System.Drawing.Size(224, 28);
            this.patternOptions.TabIndex = 5;
            // 
            // rbPatternStraight
            // 
            this.rbPatternStraight.AutoSize = true;
            this.rbPatternStraight.Location = new System.Drawing.Point(3, 3);
            this.rbPatternStraight.Name = "rbPatternStraight";
            this.rbPatternStraight.Size = new System.Drawing.Size(84, 19);
            this.rbPatternStraight.TabIndex = 0;
            this.rbPatternStraight.Text = "STRAIGHT";
            this.rbPatternStraight.CheckedChanged += new System.EventHandler(this.PickupOption_CheckedChanged);
            // 
            // rbPatternZigZag
            // 
            this.rbPatternZigZag.AutoSize = true;
            this.rbPatternZigZag.Checked = true;
            this.rbPatternZigZag.Location = new System.Drawing.Point(93, 3);
            this.rbPatternZigZag.Name = "rbPatternZigZag";
            this.rbPatternZigZag.Size = new System.Drawing.Size(69, 19);
            this.rbPatternZigZag.TabIndex = 1;
            this.rbPatternZigZag.TabStop = true;
            this.rbPatternZigZag.Text = "ZIGZAG";
            this.rbPatternZigZag.CheckedChanged += new System.EventHandler(this.PickupOption_CheckedChanged);
            // 
            // btnPreviewPath
            // 
            this.pickupRouteLayout.SetColumnSpan(this.btnPreviewPath, 2);
            this.btnPreviewPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPreviewPath.Location = new System.Drawing.Point(3, 119);
            this.btnPreviewPath.Name = "btnPreviewPath";
            this.btnPreviewPath.Size = new System.Drawing.Size(306, 26);
            this.btnPreviewPath.TabIndex = 6;
            this.btnPreviewPath.Text = "픽업 순서 보기";
            this.btnPreviewPath.Click += new System.EventHandler(this.BtnPreviewPath_Click);
            // 
            // btnApplyPickupOrder
            // 
            this.btnApplyPickupOrder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(123)))), ((int)(((byte)(184)))));
            this.pickupRouteLayout.SetColumnSpan(this.btnApplyPickupOrder, 2);
            this.btnApplyPickupOrder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApplyPickupOrder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApplyPickupOrder.ForeColor = System.Drawing.Color.White;
            this.btnApplyPickupOrder.Location = new System.Drawing.Point(3, 151);
            this.btnApplyPickupOrder.Name = "btnApplyPickupOrder";
            this.btnApplyPickupOrder.Size = new System.Drawing.Size(306, 38);
            this.btnApplyPickupOrder.TabIndex = 7;
            this.btnApplyPickupOrder.Text = "APPLY PICKUP ORDER";
            this.btnApplyPickupOrder.UseVisualStyleBackColor = false;
            this.btnApplyPickupOrder.Click += new System.EventHandler(this.BtnApplyPickupOrder_Click);
            // 
            // grpWorkflow
            // 
            this.grpWorkflow.Controls.Add(this.workflowLayout);
            this.grpWorkflow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpWorkflow.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpWorkflow.Location = new System.Drawing.Point(0, 520);
            this.grpWorkflow.Margin = new System.Windows.Forms.Padding(0);
            this.grpWorkflow.Name = "grpWorkflow";
            this.grpWorkflow.Padding = new System.Windows.Forms.Padding(8);
            this.grpWorkflow.Size = new System.Drawing.Size(328, 268);
            this.grpWorkflow.TabIndex = 2;
            this.grpWorkflow.TabStop = false;
            this.grpWorkflow.Text = "ALIGN / MAPPING / RUN";
            // 
            // workflowLayout
            // 
            this.workflowLayout.ColumnCount = 2;
            this.workflowLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.workflowLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.workflowLayout.Controls.Add(this.lblRevisionCaption, 0, 0);
            this.workflowLayout.Controls.Add(this.lblMappingRevisionValue, 1, 0);
            this.workflowLayout.Controls.Add(this.txtFailureDetail, 0, 1);
            this.workflowLayout.Controls.Add(this.btnRetryAlign, 0, 2);
            this.workflowLayout.Controls.Add(this.btnRetryMapping, 1, 2);
            this.workflowLayout.Controls.Add(this.btnMappingSetup, 0, 3);
            this.workflowLayout.Controls.Add(this.btnStartRun, 0, 4);
            this.workflowLayout.Controls.Add(this.btnAbortAuto, 1, 4);
            this.workflowLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workflowLayout.Location = new System.Drawing.Point(8, 24);
            this.workflowLayout.Name = "workflowLayout";
            this.workflowLayout.RowCount = 5;
            this.workflowLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workflowLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.workflowLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.workflowLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.workflowLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 54F));
            this.workflowLayout.Size = new System.Drawing.Size(312, 236);
            this.workflowLayout.TabIndex = 0;
            // 
            // lblRevisionCaption
            // 
            this.lblRevisionCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRevisionCaption.Location = new System.Drawing.Point(3, 0);
            this.lblRevisionCaption.Name = "lblRevisionCaption";
            this.lblRevisionCaption.Size = new System.Drawing.Size(150, 30);
            this.lblRevisionCaption.TabIndex = 0;
            this.lblRevisionCaption.Text = "Mapping Revision";
            this.lblRevisionCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMappingRevisionValue
            // 
            this.lblMappingRevisionValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMappingRevisionValue.Location = new System.Drawing.Point(159, 0);
            this.lblMappingRevisionValue.Name = "lblMappingRevisionValue";
            this.lblMappingRevisionValue.Size = new System.Drawing.Size(150, 30);
            this.lblMappingRevisionValue.TabIndex = 1;
            this.lblMappingRevisionValue.Text = "-";
            this.lblMappingRevisionValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtFailureDetail
            // 
            this.txtFailureDetail.BackColor = System.Drawing.Color.White;
            this.workflowLayout.SetColumnSpan(this.txtFailureDetail, 2);
            this.txtFailureDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtFailureDetail.Location = new System.Drawing.Point(3, 33);
            this.txtFailureDetail.Multiline = true;
            this.txtFailureDetail.Name = "txtFailureDetail";
            this.txtFailureDetail.ReadOnly = true;
            this.txtFailureDetail.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtFailureDetail.Size = new System.Drawing.Size(306, 62);
            this.txtFailureDetail.TabIndex = 2;
            // 
            // btnRetryAlign
            // 
            this.btnRetryAlign.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRetryAlign.Location = new System.Drawing.Point(3, 101);
            this.btnRetryAlign.Name = "btnRetryAlign";
            this.btnRetryAlign.Size = new System.Drawing.Size(150, 36);
            this.btnRetryAlign.TabIndex = 3;
            this.btnRetryAlign.Text = "RETRY ALIGN";
            this.btnRetryAlign.Click += new System.EventHandler(this.BtnRetryAlign_Click);
            // 
            // btnRetryMapping
            // 
            this.btnRetryMapping.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRetryMapping.Location = new System.Drawing.Point(159, 101);
            this.btnRetryMapping.Name = "btnRetryMapping";
            this.btnRetryMapping.Size = new System.Drawing.Size(150, 36);
            this.btnRetryMapping.TabIndex = 4;
            this.btnRetryMapping.Text = "RUN DIE MAPPING";
            this.btnRetryMapping.Click += new System.EventHandler(this.BtnRetryMapping_Click);
            // 
            // btnMappingSetup
            // 
            this.workflowLayout.SetColumnSpan(this.btnMappingSetup, 2);
            this.btnMappingSetup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMappingSetup.Location = new System.Drawing.Point(3, 143);
            this.btnMappingSetup.Name = "btnMappingSetup";
            this.btnMappingSetup.Size = new System.Drawing.Size(306, 36);
            this.btnMappingSetup.TabIndex = 5;
            this.btnMappingSetup.Text = "MAPPING SETUP";
            this.btnMappingSetup.Click += new System.EventHandler(this.BtnMappingSetup_Click);
            // 
            // btnStartRun
            // 
            this.btnStartRun.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(139)))), ((int)(((byte)(83)))));
            this.btnStartRun.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStartRun.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartRun.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnStartRun.ForeColor = System.Drawing.Color.White;
            this.btnStartRun.Location = new System.Drawing.Point(3, 185);
            this.btnStartRun.Name = "btnStartRun";
            this.btnStartRun.Size = new System.Drawing.Size(150, 48);
            this.btnStartRun.TabIndex = 6;
            this.btnStartRun.Text = "CONFIRM / CONTINUE AUTO";
            this.btnStartRun.UseVisualStyleBackColor = false;
            this.btnStartRun.Click += new System.EventHandler(this.BtnStartRun_Click);
            // 
            // btnAbortAuto
            // 
            this.btnAbortAuto.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(185)))), ((int)(((byte)(70)))), ((int)(((byte)(65)))));
            this.btnAbortAuto.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAbortAuto.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAbortAuto.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnAbortAuto.ForeColor = System.Drawing.Color.White;
            this.btnAbortAuto.Location = new System.Drawing.Point(159, 185);
            this.btnAbortAuto.Name = "btnAbortAuto";
            this.btnAbortAuto.Size = new System.Drawing.Size(150, 48);
            this.btnAbortAuto.TabIndex = 7;
            this.btnAbortAuto.Text = "CANCEL / RETRY T ALIGN";
            this.btnAbortAuto.UseVisualStyleBackColor = false;
            this.btnAbortAuto.Click += new System.EventHandler(this.BtnAbortAuto_Click);
            // 
            // footerLayout
            // 
            this.footerLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(48)))), ((int)(((byte)(58)))));
            this.footerLayout.ColumnCount = 3;
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.footerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.footerLayout.Controls.Add(this.lblStatus, 0, 0);
            this.footerLayout.Controls.Add(this.btnBuzzerStop, 1, 0);
            this.footerLayout.Controls.Add(this.btnClose, 2, 0);
            this.footerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.footerLayout.Location = new System.Drawing.Point(0, 866);
            this.footerLayout.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.footerLayout.Name = "footerLayout";
            this.footerLayout.RowCount = 1;
            this.footerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.footerLayout.Size = new System.Drawing.Size(1724, 58);
            this.footerLayout.TabIndex = 2;
            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblStatus.ForeColor = System.Drawing.Color.White;
            this.lblStatus.Location = new System.Drawing.Point(3, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.lblStatus.Size = new System.Drawing.Size(1448, 58);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "-";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnBuzzerStop
            // 
            this.btnBuzzerStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(206)))), ((int)(((byte)(132)))), ((int)(((byte)(32)))));
            this.btnBuzzerStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBuzzerStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBuzzerStop.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnBuzzerStop.ForeColor = System.Drawing.Color.White;
            this.btnBuzzerStop.Location = new System.Drawing.Point(1462, 10);
            this.btnBuzzerStop.Margin = new System.Windows.Forms.Padding(8, 10, 0, 10);
            this.btnBuzzerStop.Name = "btnBuzzerStop";
            this.btnBuzzerStop.Size = new System.Drawing.Size(142, 38);
            this.btnBuzzerStop.TabIndex = 1;
            this.btnBuzzerStop.Text = "BUZZER STOP";
            this.btnBuzzerStop.UseVisualStyleBackColor = false;
            this.btnBuzzerStop.Click += new System.EventHandler(this.BtnBuzzerStop_Click);
            // 
            // btnClose
            // 
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.Location = new System.Drawing.Point(1612, 10);
            this.btnClose.Margin = new System.Windows.Forms.Padding(8, 10, 10, 10);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(102, 38);
            this.btnClose.TabIndex = 2;
            this.btnClose.Text = "CLOSE";
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            // 
            // InputStageRunReviewDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(241)))), ((int)(((byte)(245)))));
            this.ClientSize = new System.Drawing.Size(1740, 940);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.KeyPreview = true;
            this.MinimumSize = new System.Drawing.Size(1480, 820);
            this.Name = "InputStageRunReviewDialog";
            this.Padding = new System.Windows.Forms.Padding(8);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Wafer Align / Die Mapping Review";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.InputStageRunReviewDialog_FormClosing);
            this.rootLayout.ResumeLayout(false);
            this.headerLayout.ResumeLayout(false);
            this.bodyLayout.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.leftBottomLayout.ResumeLayout(false);
            this.grpWaferVision.ResumeLayout(false);
            this.waferVisionLayout.ResumeLayout(false);
            this.waferVisionHeaderLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dieGrid)).EndInit();
            this.centerLayout.ResumeLayout(false);
            this.grpDieState.ResumeLayout(false);
            this.dieStateLayout.ResumeLayout(false);
            this.dieStateOptions.ResumeLayout(false);
            this.dieStateOptions.PerformLayout();
            this.grpStartDie.ResumeLayout(false);
            this.startDieLayout.ResumeLayout(false);
            this.startDieLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numStartIndex)).EndInit();
            this.grpJog.ResumeLayout(false);
            this.jogLayout.ResumeLayout(false);
            this.grpActions.ResumeLayout(false);
            this.actionLayout.ResumeLayout(false);
            this.rightLayout.ResumeLayout(false);
            this.grpMapInfo.ResumeLayout(false);
            this.mapInfoLayout.ResumeLayout(false);
            this.grpPickupRoute.ResumeLayout(false);
            this.pickupRouteLayout.ResumeLayout(false);
            this.cornerOptions.ResumeLayout(false);
            this.cornerOptions.PerformLayout();
            this.directionOptions.ResumeLayout(false);
            this.directionOptions.PerformLayout();
            this.patternOptions.ResumeLayout(false);
            this.patternOptions.PerformLayout();
            this.grpWorkflow.ResumeLayout(false);
            this.workflowLayout.ResumeLayout(false);
            this.workflowLayout.PerformLayout();
            this.footerLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
