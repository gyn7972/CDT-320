using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class OutputStagePage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private TableLayoutPanel leftLayout;
        private TableLayoutPanel materialPanel;
        private TableLayoutPanel materialHeaderLayout;
        private GroupBox grpState;
        private GroupBox grpCounters;
        private GroupBox grpCylinder;
        private GroupBox grpInfo;
        private MaterialDetailView materialDetailView;
        private TableLayoutPanel stateLayout;
        private TableLayoutPanel counterLayout;
        private TableLayoutPanel cylinderLayout;
        private TableLayoutPanel infoLayout;
        private TableLayoutPanel actionPanel;
        private TableLayoutPanel actionBar;
        private TableLayoutPanel actionRightPanel;
        private GroupBox grpAction;
        private Label lblMaterialTitle;
        private RadioButton rdoGoodMaterial;
        private RadioButton rdoNgMaterial;
        private Label lblGoodExistTitle;
        private Label lblGoodExistValue;
        private Label lblGoodStateTitle;
        private Label lblGoodStateValue;
        private Label lblNgExistTitle;
        private Label lblNgExistValue;
        private Label lblNgStateTitle;
        private Label lblNgStateValue;
        private Label lblGoodCountTitle;
        private Label lblGoodCountValue;
        private Label lblNgCountTitle;
        private Label lblNgCountValue;
        private Label lblTotalCountTitle;
        private Label lblTotalCountValue;
        private Label lblGoodGuideTitle;
        private Label lblGoodGuideValue;
        private Label lblGoodClampTitle;
        private Label lblGoodClampValue;
        private Label lblGoodClampStateTitle;
        private Label lblGoodClampStateValue;
        private Label lblNgGuideTitle;
        private Label lblNgGuideValue;
        private Label lblNgClampTitle;
        private Label lblNgClampValue;
        private Label lblNgClampStateTitle;
        private Label lblNgClampStateValue;
        private TableLayoutPanel goodYPanel;
        private Label lblGoodYTitle;
        private Label lblGoodYValue;
        private TableLayoutPanel goodZPanel;
        private Label lblGoodZTitle;
        private Label lblGoodZValue;
        private TableLayoutPanel ngYPanel;
        private Label lblNgYTitle;
        private Label lblNgYValue;
        private TableLayoutPanel visionXPanel;
        private Label lblVisionXTitle;
        private Label lblVisionXValue;
        private ActionButton btnStageReady;
        private ActionButton btnNgStageReady;
        private ActionButton btnGoodProcess;
        private ActionButton btnNgProcess;
        private ActionButton btnGoodReceive;
        private ActionButton btnNgReceive;
        private ActionButton btnGoodUnload;
        private ActionButton btnNgUnload;
        private ActionButton btnInspect;
        private ActionButton btnStageInit;
        private ActionButton btnStop;

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.leftLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpState = new System.Windows.Forms.GroupBox();
            this.stateLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblGoodExistTitle = new System.Windows.Forms.Label();
            this.lblGoodExistValue = new System.Windows.Forms.Label();
            this.lblGoodStateTitle = new System.Windows.Forms.Label();
            this.lblGoodStateValue = new System.Windows.Forms.Label();
            this.lblNgExistTitle = new System.Windows.Forms.Label();
            this.lblNgExistValue = new System.Windows.Forms.Label();
            this.lblNgStateTitle = new System.Windows.Forms.Label();
            this.lblNgStateValue = new System.Windows.Forms.Label();
            this.grpCounters = new System.Windows.Forms.GroupBox();
            this.counterLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblGoodCountTitle = new System.Windows.Forms.Label();
            this.lblGoodCountValue = new System.Windows.Forms.Label();
            this.lblNgCountTitle = new System.Windows.Forms.Label();
            this.lblNgCountValue = new System.Windows.Forms.Label();
            this.lblTotalCountTitle = new System.Windows.Forms.Label();
            this.lblTotalCountValue = new System.Windows.Forms.Label();
            this.grpCylinder = new System.Windows.Forms.GroupBox();
            this.cylinderLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblGoodGuideTitle = new System.Windows.Forms.Label();
            this.lblGoodGuideValue = new System.Windows.Forms.Label();
            this.lblGoodClampTitle = new System.Windows.Forms.Label();
            this.lblGoodClampValue = new System.Windows.Forms.Label();
            this.lblGoodClampStateTitle = new System.Windows.Forms.Label();
            this.lblGoodClampStateValue = new System.Windows.Forms.Label();
            this.lblNgGuideTitle = new System.Windows.Forms.Label();
            this.lblNgGuideValue = new System.Windows.Forms.Label();
            this.lblNgClampTitle = new System.Windows.Forms.Label();
            this.lblNgClampValue = new System.Windows.Forms.Label();
            this.lblNgClampStateTitle = new System.Windows.Forms.Label();
            this.lblNgClampStateValue = new System.Windows.Forms.Label();
            this.grpInfo = new System.Windows.Forms.GroupBox();
            this.infoLayout = new System.Windows.Forms.TableLayoutPanel();
            this.goodYPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblGoodYTitle = new System.Windows.Forms.Label();
            this.lblGoodYValue = new System.Windows.Forms.Label();
            this.goodZPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblGoodZTitle = new System.Windows.Forms.Label();
            this.lblGoodZValue = new System.Windows.Forms.Label();
            this.ngYPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblNgYTitle = new System.Windows.Forms.Label();
            this.lblNgYValue = new System.Windows.Forms.Label();
            this.visionXPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblVisionXTitle = new System.Windows.Forms.Label();
            this.lblVisionXValue = new System.Windows.Forms.Label();
            this.grpAction = new System.Windows.Forms.GroupBox();
            this.actionBar = new System.Windows.Forms.TableLayoutPanel();
            this.actionPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnStageReady = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNgStageReady = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnGoodProcess = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNgProcess = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnGoodReceive = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNgReceive = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnGoodUnload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNgUnload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnInspect = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnStageInit = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.actionRightPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnStop = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.materialPanel = new System.Windows.Forms.TableLayoutPanel();
            this.materialHeaderLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblMaterialTitle = new System.Windows.Forms.Label();
            this.rdoGoodMaterial = new System.Windows.Forms.RadioButton();
            this.rdoNgMaterial = new System.Windows.Forms.RadioButton();
            this.materialDetailView = new QMC.CDT_320.Ui.Controls.MaterialDetailView();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.grpState.SuspendLayout();
            this.stateLayout.SuspendLayout();
            this.grpCounters.SuspendLayout();
            this.counterLayout.SuspendLayout();
            this.grpCylinder.SuspendLayout();
            this.cylinderLayout.SuspendLayout();
            this.grpInfo.SuspendLayout();
            this.infoLayout.SuspendLayout();
            this.goodYPanel.SuspendLayout();
            this.goodZPanel.SuspendLayout();
            this.ngYPanel.SuspendLayout();
            this.visionXPanel.SuspendLayout();
            this.grpAction.SuspendLayout();
            this.actionBar.SuspendLayout();
            this.actionPanel.SuspendLayout();
            this.actionRightPanel.SuspendLayout();
            this.materialPanel.SuspendLayout();
            this.materialHeaderLayout.SuspendLayout();
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
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1678, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:wi.outputStage";
            this.lblHeader.Text = "OUTPUT STAGE";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.Controls.Add(this.leftLayout, 0, 0);
            this.contentLayout.Controls.Add(this.grpAction, 0, 1);
            this.contentLayout.Controls.Add(this.materialPanel, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(0, 30);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.RowCount = 2;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1678, 870);
            this.contentLayout.TabIndex = 1;
            // 
            // leftLayout
            // 
            this.leftLayout.ColumnCount = 3;
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.leftLayout.Controls.Add(this.grpState, 0, 0);
            this.leftLayout.Controls.Add(this.grpCounters, 1, 0);
            this.leftLayout.Controls.Add(this.grpCylinder, 1, 1);
            this.leftLayout.Controls.Add(this.grpInfo, 2, 0);
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Location = new System.Drawing.Point(0, 0);
            this.leftLayout.Margin = new System.Windows.Forms.Padding(0);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.RowCount = 2;
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.leftLayout.Size = new System.Drawing.Size(839, 300);
            this.leftLayout.TabIndex = 0;
            // 
            // grpState
            // 
            this.grpState.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpState.Controls.Add(this.stateLayout);
            this.grpState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpState.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpState.Location = new System.Drawing.Point(0, 0);
            this.grpState.Margin = new System.Windows.Forms.Padding(0, 0, 3, 3);
            this.grpState.Name = "grpState";
            this.leftLayout.SetRowSpan(this.grpState, 2);
            this.grpState.Size = new System.Drawing.Size(332, 297);
            this.grpState.TabIndex = 0;
            this.grpState.TabStop = false;
            this.grpState.Text = "WORK INFO";
            // 
            // stateLayout
            // 
            this.stateLayout.ColumnCount = 2;
            this.stateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this.stateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.stateLayout.Controls.Add(this.lblGoodExistTitle, 0, 0);
            this.stateLayout.Controls.Add(this.lblGoodExistValue, 1, 0);
            this.stateLayout.Controls.Add(this.lblGoodStateTitle, 0, 1);
            this.stateLayout.Controls.Add(this.lblGoodStateValue, 1, 1);
            this.stateLayout.Controls.Add(this.lblNgExistTitle, 0, 2);
            this.stateLayout.Controls.Add(this.lblNgExistValue, 1, 2);
            this.stateLayout.Controls.Add(this.lblNgStateTitle, 0, 3);
            this.stateLayout.Controls.Add(this.lblNgStateValue, 1, 3);
            this.stateLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stateLayout.Location = new System.Drawing.Point(3, 23);
            this.stateLayout.Name = "stateLayout";
            this.stateLayout.Padding = new System.Windows.Forms.Padding(8, 12, 8, 10);
            this.stateLayout.RowCount = 6;
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.stateLayout.Size = new System.Drawing.Size(326, 271);
            this.stateLayout.TabIndex = 0;
            // 
            // lblGoodExistTitle
            // 
            this.lblGoodExistTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblGoodExistTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodExistTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodExistTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblGoodExistTitle.Location = new System.Drawing.Point(11, 12);
            this.lblGoodExistTitle.Name = "lblGoodExistTitle";
            this.lblGoodExistTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGoodExistTitle.Size = new System.Drawing.Size(173, 32);
            this.lblGoodExistTitle.TabIndex = 0;
            this.lblGoodExistTitle.Text = "GOOD EXIST";
            this.lblGoodExistTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblGoodExistValue
            // 
            this.lblGoodExistValue.BackColor = System.Drawing.Color.White;
            this.lblGoodExistValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodExistValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodExistValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblGoodExistValue.Location = new System.Drawing.Point(190, 12);
            this.lblGoodExistValue.Name = "lblGoodExistValue";
            this.lblGoodExistValue.Size = new System.Drawing.Size(125, 32);
            this.lblGoodExistValue.TabIndex = 1;
            this.lblGoodExistValue.Text = "EMPTY";
            this.lblGoodExistValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblGoodStateTitle
            // 
            this.lblGoodStateTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblGoodStateTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodStateTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodStateTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblGoodStateTitle.Location = new System.Drawing.Point(11, 44);
            this.lblGoodStateTitle.Name = "lblGoodStateTitle";
            this.lblGoodStateTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGoodStateTitle.Size = new System.Drawing.Size(173, 32);
            this.lblGoodStateTitle.TabIndex = 2;
            this.lblGoodStateTitle.Text = "GOOD STATE";
            this.lblGoodStateTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblGoodStateValue
            // 
            this.lblGoodStateValue.BackColor = System.Drawing.Color.White;
            this.lblGoodStateValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodStateValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodStateValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblGoodStateValue.Location = new System.Drawing.Point(190, 44);
            this.lblGoodStateValue.Name = "lblGoodStateValue";
            this.lblGoodStateValue.Size = new System.Drawing.Size(125, 32);
            this.lblGoodStateValue.TabIndex = 3;
            this.lblGoodStateValue.Text = "INCOMPLETE";
            this.lblGoodStateValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblNgExistTitle
            // 
            this.lblNgExistTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblNgExistTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgExistTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgExistTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblNgExistTitle.Location = new System.Drawing.Point(11, 76);
            this.lblNgExistTitle.Name = "lblNgExistTitle";
            this.lblNgExistTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNgExistTitle.Size = new System.Drawing.Size(173, 32);
            this.lblNgExistTitle.TabIndex = 4;
            this.lblNgExistTitle.Text = "NG EXIST";
            this.lblNgExistTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNgExistValue
            // 
            this.lblNgExistValue.BackColor = System.Drawing.Color.White;
            this.lblNgExistValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgExistValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgExistValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblNgExistValue.Location = new System.Drawing.Point(190, 76);
            this.lblNgExistValue.Name = "lblNgExistValue";
            this.lblNgExistValue.Size = new System.Drawing.Size(125, 32);
            this.lblNgExistValue.TabIndex = 5;
            this.lblNgExistValue.Text = "EMPTY";
            this.lblNgExistValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblNgStateTitle
            // 
            this.lblNgStateTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblNgStateTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgStateTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgStateTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblNgStateTitle.Location = new System.Drawing.Point(11, 108);
            this.lblNgStateTitle.Name = "lblNgStateTitle";
            this.lblNgStateTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNgStateTitle.Size = new System.Drawing.Size(173, 32);
            this.lblNgStateTitle.TabIndex = 6;
            this.lblNgStateTitle.Text = "NG STATE";
            this.lblNgStateTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNgStateValue
            // 
            this.lblNgStateValue.BackColor = System.Drawing.Color.White;
            this.lblNgStateValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgStateValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgStateValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblNgStateValue.Location = new System.Drawing.Point(190, 108);
            this.lblNgStateValue.Name = "lblNgStateValue";
            this.lblNgStateValue.Size = new System.Drawing.Size(125, 32);
            this.lblNgStateValue.TabIndex = 7;
            this.lblNgStateValue.Text = "INCOMPLETE";
            this.lblNgStateValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // grpCounters
            // 
            this.grpCounters.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpCounters.Controls.Add(this.counterLayout);
            this.grpCounters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCounters.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpCounters.Location = new System.Drawing.Point(338, 0);
            this.grpCounters.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.grpCounters.Name = "grpCounters";
            this.grpCounters.Size = new System.Drawing.Size(203, 147);
            this.grpCounters.TabIndex = 1;
            this.grpCounters.TabStop = false;
            this.grpCounters.Text = "COUNTER";
            // 
            // counterLayout
            // 
            this.counterLayout.ColumnCount = 2;
            this.counterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 67.9558F));
            this.counterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 32.0442F));
            this.counterLayout.Controls.Add(this.lblGoodCountTitle, 0, 0);
            this.counterLayout.Controls.Add(this.lblGoodCountValue, 1, 0);
            this.counterLayout.Controls.Add(this.lblNgCountTitle, 0, 1);
            this.counterLayout.Controls.Add(this.lblNgCountValue, 1, 1);
            this.counterLayout.Controls.Add(this.lblTotalCountTitle, 0, 2);
            this.counterLayout.Controls.Add(this.lblTotalCountValue, 1, 2);
            this.counterLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.counterLayout.Location = new System.Drawing.Point(3, 23);
            this.counterLayout.Name = "counterLayout";
            this.counterLayout.Padding = new System.Windows.Forms.Padding(8, 12, 8, 10);
            this.counterLayout.RowCount = 4;
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.counterLayout.Size = new System.Drawing.Size(197, 121);
            this.counterLayout.TabIndex = 0;
            // 
            // lblGoodCountTitle
            // 
            this.lblGoodCountTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblGoodCountTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodCountTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodCountTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblGoodCountTitle.Location = new System.Drawing.Point(11, 12);
            this.lblGoodCountTitle.Name = "lblGoodCountTitle";
            this.lblGoodCountTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGoodCountTitle.Size = new System.Drawing.Size(117, 24);
            this.lblGoodCountTitle.TabIndex = 0;
            this.lblGoodCountTitle.Text = "GOOD COUNT";
            this.lblGoodCountTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblGoodCountValue
            // 
            this.lblGoodCountValue.BackColor = System.Drawing.Color.White;
            this.lblGoodCountValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodCountValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodCountValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblGoodCountValue.Location = new System.Drawing.Point(134, 12);
            this.lblGoodCountValue.Name = "lblGoodCountValue";
            this.lblGoodCountValue.Size = new System.Drawing.Size(52, 24);
            this.lblGoodCountValue.TabIndex = 1;
            this.lblGoodCountValue.Text = "0 ea";
            this.lblGoodCountValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblNgCountTitle
            // 
            this.lblNgCountTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblNgCountTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgCountTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgCountTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblNgCountTitle.Location = new System.Drawing.Point(11, 36);
            this.lblNgCountTitle.Name = "lblNgCountTitle";
            this.lblNgCountTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNgCountTitle.Size = new System.Drawing.Size(117, 24);
            this.lblNgCountTitle.TabIndex = 2;
            this.lblNgCountTitle.Text = "NG COUNT";
            this.lblNgCountTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNgCountValue
            // 
            this.lblNgCountValue.BackColor = System.Drawing.Color.White;
            this.lblNgCountValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgCountValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgCountValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblNgCountValue.Location = new System.Drawing.Point(134, 36);
            this.lblNgCountValue.Name = "lblNgCountValue";
            this.lblNgCountValue.Size = new System.Drawing.Size(52, 24);
            this.lblNgCountValue.TabIndex = 3;
            this.lblNgCountValue.Text = "0 ea";
            this.lblNgCountValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblTotalCountTitle
            // 
            this.lblTotalCountTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblTotalCountTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblTotalCountTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalCountTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblTotalCountTitle.Location = new System.Drawing.Point(11, 60);
            this.lblTotalCountTitle.Name = "lblTotalCountTitle";
            this.lblTotalCountTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblTotalCountTitle.Size = new System.Drawing.Size(117, 24);
            this.lblTotalCountTitle.TabIndex = 4;
            this.lblTotalCountTitle.Text = "TOTAL COUNT";
            this.lblTotalCountTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblTotalCountValue
            // 
            this.lblTotalCountValue.BackColor = System.Drawing.Color.White;
            this.lblTotalCountValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblTotalCountValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalCountValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblTotalCountValue.Location = new System.Drawing.Point(134, 60);
            this.lblTotalCountValue.Name = "lblTotalCountValue";
            this.lblTotalCountValue.Size = new System.Drawing.Size(52, 24);
            this.lblTotalCountValue.TabIndex = 5;
            this.lblTotalCountValue.Text = "0 ea";
            this.lblTotalCountValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // grpCylinder
            // 
            this.grpCylinder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.leftLayout.SetColumnSpan(this.grpCylinder, 2);
            this.grpCylinder.Controls.Add(this.cylinderLayout);
            this.grpCylinder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCylinder.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpCylinder.Location = new System.Drawing.Point(338, 153);
            this.grpCylinder.Name = "grpCylinder";
            this.grpCylinder.Size = new System.Drawing.Size(498, 144);
            this.grpCylinder.TabIndex = 2;
            this.grpCylinder.TabStop = false;
            this.grpCylinder.Text = "CYLINDER INFO";
            // 
            // cylinderLayout
            // 
            this.cylinderLayout.ColumnCount = 2;
            this.cylinderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 41F));
            this.cylinderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 59F));
            this.cylinderLayout.Controls.Add(this.lblGoodGuideTitle, 0, 0);
            this.cylinderLayout.Controls.Add(this.lblGoodGuideValue, 1, 0);
            this.cylinderLayout.Controls.Add(this.lblGoodClampTitle, 0, 1);
            this.cylinderLayout.Controls.Add(this.lblGoodClampValue, 1, 1);
            this.cylinderLayout.Controls.Add(this.lblGoodClampStateTitle, 0, 2);
            this.cylinderLayout.Controls.Add(this.lblGoodClampStateValue, 1, 2);
            this.cylinderLayout.Controls.Add(this.lblNgGuideTitle, 0, 3);
            this.cylinderLayout.Controls.Add(this.lblNgGuideValue, 1, 3);
            this.cylinderLayout.Controls.Add(this.lblNgClampTitle, 0, 4);
            this.cylinderLayout.Controls.Add(this.lblNgClampValue, 1, 4);
            this.cylinderLayout.Controls.Add(this.lblNgClampStateTitle, 0, 5);
            this.cylinderLayout.Controls.Add(this.lblNgClampStateValue, 1, 5);
            this.cylinderLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cylinderLayout.Location = new System.Drawing.Point(3, 23);
            this.cylinderLayout.Name = "cylinderLayout";
            this.cylinderLayout.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.cylinderLayout.RowCount = 6;
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.cylinderLayout.Size = new System.Drawing.Size(492, 118);
            this.cylinderLayout.TabIndex = 0;
            // 
            // lblGoodGuideTitle
            // 
            this.lblGoodGuideTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblGoodGuideTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodGuideTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodGuideTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblGoodGuideTitle.Location = new System.Drawing.Point(11, 3);
            this.lblGoodGuideTitle.Name = "lblGoodGuideTitle";
            this.lblGoodGuideTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGoodGuideTitle.Size = new System.Drawing.Size(189, 18);
            this.lblGoodGuideTitle.TabIndex = 0;
            this.lblGoodGuideTitle.Text = "GOOD GUIDE";
            this.lblGoodGuideTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblGoodGuideValue
            // 
            this.lblGoodGuideValue.BackColor = System.Drawing.Color.White;
            this.lblGoodGuideValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodGuideValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodGuideValue.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblGoodGuideValue.Location = new System.Drawing.Point(206, 3);
            this.lblGoodGuideValue.Name = "lblGoodGuideValue";
            this.lblGoodGuideValue.Size = new System.Drawing.Size(275, 18);
            this.lblGoodGuideValue.TabIndex = 1;
            this.lblGoodGuideValue.Text = "--";
            this.lblGoodGuideValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblGoodClampTitle
            // 
            this.lblGoodClampTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblGoodClampTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodClampTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodClampTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblGoodClampTitle.Location = new System.Drawing.Point(11, 21);
            this.lblGoodClampTitle.Name = "lblGoodClampTitle";
            this.lblGoodClampTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGoodClampTitle.Size = new System.Drawing.Size(189, 18);
            this.lblGoodClampTitle.TabIndex = 2;
            this.lblGoodClampTitle.Text = "GOOD CLAMP LIFT";
            this.lblGoodClampTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblGoodClampValue
            // 
            this.lblGoodClampValue.BackColor = System.Drawing.Color.White;
            this.lblGoodClampValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodClampValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodClampValue.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblGoodClampValue.Location = new System.Drawing.Point(206, 21);
            this.lblGoodClampValue.Name = "lblGoodClampValue";
            this.lblGoodClampValue.Size = new System.Drawing.Size(275, 18);
            this.lblGoodClampValue.TabIndex = 3;
            this.lblGoodClampValue.Text = "--";
            this.lblGoodClampValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblGoodClampStateTitle
            // 
            this.lblGoodClampStateTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblGoodClampStateTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodClampStateTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodClampStateTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblGoodClampStateTitle.Location = new System.Drawing.Point(11, 39);
            this.lblGoodClampStateTitle.Name = "lblGoodClampStateTitle";
            this.lblGoodClampStateTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGoodClampStateTitle.Size = new System.Drawing.Size(189, 18);
            this.lblGoodClampStateTitle.TabIndex = 8;
            this.lblGoodClampStateTitle.Text = "GOOD CLAMP";
            this.lblGoodClampStateTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblGoodClampStateValue
            // 
            this.lblGoodClampStateValue.BackColor = System.Drawing.Color.White;
            this.lblGoodClampStateValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodClampStateValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodClampStateValue.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblGoodClampStateValue.Location = new System.Drawing.Point(206, 39);
            this.lblGoodClampStateValue.Name = "lblGoodClampStateValue";
            this.lblGoodClampStateValue.Size = new System.Drawing.Size(275, 18);
            this.lblGoodClampStateValue.TabIndex = 9;
            this.lblGoodClampStateValue.Text = "--";
            this.lblGoodClampStateValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblNgGuideTitle
            // 
            this.lblNgGuideTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblNgGuideTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgGuideTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgGuideTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblNgGuideTitle.Location = new System.Drawing.Point(11, 57);
            this.lblNgGuideTitle.Name = "lblNgGuideTitle";
            this.lblNgGuideTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNgGuideTitle.Size = new System.Drawing.Size(189, 18);
            this.lblNgGuideTitle.TabIndex = 10;
            this.lblNgGuideTitle.Text = "NG GUIDE";
            this.lblNgGuideTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNgGuideValue
            // 
            this.lblNgGuideValue.BackColor = System.Drawing.Color.White;
            this.lblNgGuideValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgGuideValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgGuideValue.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblNgGuideValue.Location = new System.Drawing.Point(206, 57);
            this.lblNgGuideValue.Name = "lblNgGuideValue";
            this.lblNgGuideValue.Size = new System.Drawing.Size(275, 18);
            this.lblNgGuideValue.TabIndex = 11;
            this.lblNgGuideValue.Text = "--";
            this.lblNgGuideValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblNgClampTitle
            // 
            this.lblNgClampTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblNgClampTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgClampTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgClampTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblNgClampTitle.Location = new System.Drawing.Point(11, 75);
            this.lblNgClampTitle.Name = "lblNgClampTitle";
            this.lblNgClampTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNgClampTitle.Size = new System.Drawing.Size(189, 18);
            this.lblNgClampTitle.TabIndex = 12;
            this.lblNgClampTitle.Text = "NG CLAMP LIFT";
            this.lblNgClampTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNgClampValue
            // 
            this.lblNgClampValue.BackColor = System.Drawing.Color.White;
            this.lblNgClampValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgClampValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgClampValue.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblNgClampValue.Location = new System.Drawing.Point(206, 75);
            this.lblNgClampValue.Name = "lblNgClampValue";
            this.lblNgClampValue.Size = new System.Drawing.Size(275, 18);
            this.lblNgClampValue.TabIndex = 13;
            this.lblNgClampValue.Text = "--";
            this.lblNgClampValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblNgClampStateTitle
            // 
            this.lblNgClampStateTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblNgClampStateTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgClampStateTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgClampStateTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblNgClampStateTitle.Location = new System.Drawing.Point(11, 93);
            this.lblNgClampStateTitle.Name = "lblNgClampStateTitle";
            this.lblNgClampStateTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNgClampStateTitle.Size = new System.Drawing.Size(189, 22);
            this.lblNgClampStateTitle.TabIndex = 14;
            this.lblNgClampStateTitle.Text = "NG CLAMP";
            this.lblNgClampStateTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNgClampStateValue
            // 
            this.lblNgClampStateValue.BackColor = System.Drawing.Color.White;
            this.lblNgClampStateValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgClampStateValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgClampStateValue.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblNgClampStateValue.Location = new System.Drawing.Point(206, 93);
            this.lblNgClampStateValue.Name = "lblNgClampStateValue";
            this.lblNgClampStateValue.Size = new System.Drawing.Size(275, 22);
            this.lblNgClampStateValue.TabIndex = 15;
            this.lblNgClampStateValue.Text = "--";
            this.lblNgClampStateValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // grpInfo
            // 
            this.grpInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpInfo.Controls.Add(this.infoLayout);
            this.grpInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInfo.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpInfo.Location = new System.Drawing.Point(547, 0);
            this.grpInfo.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.grpInfo.Name = "grpInfo";
            this.grpInfo.Size = new System.Drawing.Size(289, 147);
            this.grpInfo.TabIndex = 3;
            this.grpInfo.TabStop = false;
            this.grpInfo.Text = "INFO";
            // 
            // infoLayout
            // 
            this.infoLayout.ColumnCount = 2;
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.infoLayout.Controls.Add(this.goodYPanel, 0, 0);
            this.infoLayout.Controls.Add(this.goodZPanel, 1, 0);
            this.infoLayout.Controls.Add(this.ngYPanel, 0, 1);
            this.infoLayout.Controls.Add(this.visionXPanel, 1, 1);
            this.infoLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoLayout.Location = new System.Drawing.Point(3, 23);
            this.infoLayout.Name = "infoLayout";
            this.infoLayout.Padding = new System.Windows.Forms.Padding(8, 12, 8, 12);
            this.infoLayout.RowCount = 2;
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.infoLayout.Size = new System.Drawing.Size(283, 121);
            this.infoLayout.TabIndex = 0;
            // 
            // goodYPanel
            // 
            this.goodYPanel.ColumnCount = 1;
            this.goodYPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.goodYPanel.Controls.Add(this.lblGoodYTitle, 0, 0);
            this.goodYPanel.Controls.Add(this.lblGoodYValue, 0, 1);
            this.goodYPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.goodYPanel.Location = new System.Drawing.Point(9, 13);
            this.goodYPanel.Margin = new System.Windows.Forms.Padding(1);
            this.goodYPanel.Name = "goodYPanel";
            this.goodYPanel.RowCount = 2;
            this.goodYPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 44F));
            this.goodYPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 56F));
            this.goodYPanel.Size = new System.Drawing.Size(131, 46);
            this.goodYPanel.TabIndex = 0;
            // 
            // lblGoodYTitle
            // 
            this.lblGoodYTitle.BackColor = System.Drawing.Color.Black;
            this.lblGoodYTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodYTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblGoodYTitle.ForeColor = System.Drawing.Color.White;
            this.lblGoodYTitle.Location = new System.Drawing.Point(3, 0);
            this.lblGoodYTitle.Name = "lblGoodYTitle";
            this.lblGoodYTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGoodYTitle.Size = new System.Drawing.Size(125, 20);
            this.lblGoodYTitle.TabIndex = 0;
            this.lblGoodYTitle.Text = "GOOD STAGE Y";
            this.lblGoodYTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblGoodYValue
            // 
            this.lblGoodYValue.BackColor = System.Drawing.Color.White;
            this.lblGoodYValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodYValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodYValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblGoodYValue.Location = new System.Drawing.Point(3, 20);
            this.lblGoodYValue.Name = "lblGoodYValue";
            this.lblGoodYValue.Size = new System.Drawing.Size(125, 26);
            this.lblGoodYValue.TabIndex = 1;
            this.lblGoodYValue.Text = "0 um";
            this.lblGoodYValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // goodZPanel
            // 
            this.goodZPanel.ColumnCount = 1;
            this.goodZPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.goodZPanel.Controls.Add(this.lblGoodZTitle, 0, 0);
            this.goodZPanel.Controls.Add(this.lblGoodZValue, 0, 1);
            this.goodZPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.goodZPanel.Location = new System.Drawing.Point(142, 13);
            this.goodZPanel.Margin = new System.Windows.Forms.Padding(1);
            this.goodZPanel.Name = "goodZPanel";
            this.goodZPanel.RowCount = 2;
            this.goodZPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 44F));
            this.goodZPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 56F));
            this.goodZPanel.Size = new System.Drawing.Size(132, 46);
            this.goodZPanel.TabIndex = 1;
            // 
            // lblGoodZTitle
            // 
            this.lblGoodZTitle.BackColor = System.Drawing.Color.Black;
            this.lblGoodZTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodZTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblGoodZTitle.ForeColor = System.Drawing.Color.White;
            this.lblGoodZTitle.Location = new System.Drawing.Point(3, 0);
            this.lblGoodZTitle.Name = "lblGoodZTitle";
            this.lblGoodZTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGoodZTitle.Size = new System.Drawing.Size(126, 20);
            this.lblGoodZTitle.TabIndex = 0;
            this.lblGoodZTitle.Text = "GOOD STAGE Z";
            this.lblGoodZTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblGoodZValue
            // 
            this.lblGoodZValue.BackColor = System.Drawing.Color.White;
            this.lblGoodZValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodZValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodZValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblGoodZValue.Location = new System.Drawing.Point(3, 20);
            this.lblGoodZValue.Name = "lblGoodZValue";
            this.lblGoodZValue.Size = new System.Drawing.Size(126, 26);
            this.lblGoodZValue.TabIndex = 1;
            this.lblGoodZValue.Text = "0 um";
            this.lblGoodZValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // ngYPanel
            // 
            this.ngYPanel.ColumnCount = 1;
            this.ngYPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ngYPanel.Controls.Add(this.lblNgYTitle, 0, 0);
            this.ngYPanel.Controls.Add(this.lblNgYValue, 0, 1);
            this.ngYPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ngYPanel.Location = new System.Drawing.Point(9, 61);
            this.ngYPanel.Margin = new System.Windows.Forms.Padding(1);
            this.ngYPanel.Name = "ngYPanel";
            this.ngYPanel.RowCount = 2;
            this.ngYPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 44F));
            this.ngYPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 56F));
            this.ngYPanel.Size = new System.Drawing.Size(131, 47);
            this.ngYPanel.TabIndex = 2;
            // 
            // lblNgYTitle
            // 
            this.lblNgYTitle.BackColor = System.Drawing.Color.Black;
            this.lblNgYTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgYTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblNgYTitle.ForeColor = System.Drawing.Color.White;
            this.lblNgYTitle.Location = new System.Drawing.Point(3, 0);
            this.lblNgYTitle.Name = "lblNgYTitle";
            this.lblNgYTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNgYTitle.Size = new System.Drawing.Size(125, 20);
            this.lblNgYTitle.TabIndex = 0;
            this.lblNgYTitle.Text = "NG STAGE Y";
            this.lblNgYTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNgYValue
            // 
            this.lblNgYValue.BackColor = System.Drawing.Color.White;
            this.lblNgYValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgYValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgYValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblNgYValue.Location = new System.Drawing.Point(3, 20);
            this.lblNgYValue.Name = "lblNgYValue";
            this.lblNgYValue.Size = new System.Drawing.Size(125, 27);
            this.lblNgYValue.TabIndex = 1;
            this.lblNgYValue.Text = "0 um";
            this.lblNgYValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // visionXPanel
            // 
            this.visionXPanel.ColumnCount = 1;
            this.visionXPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.visionXPanel.Controls.Add(this.lblVisionXTitle, 0, 0);
            this.visionXPanel.Controls.Add(this.lblVisionXValue, 0, 1);
            this.visionXPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.visionXPanel.Location = new System.Drawing.Point(142, 61);
            this.visionXPanel.Margin = new System.Windows.Forms.Padding(1);
            this.visionXPanel.Name = "visionXPanel";
            this.visionXPanel.RowCount = 2;
            this.visionXPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 44F));
            this.visionXPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 56F));
            this.visionXPanel.Size = new System.Drawing.Size(132, 47);
            this.visionXPanel.TabIndex = 3;
            // 
            // lblVisionXTitle
            // 
            this.lblVisionXTitle.BackColor = System.Drawing.Color.Black;
            this.lblVisionXTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionXTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblVisionXTitle.ForeColor = System.Drawing.Color.White;
            this.lblVisionXTitle.Location = new System.Drawing.Point(3, 0);
            this.lblVisionXTitle.Name = "lblVisionXTitle";
            this.lblVisionXTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblVisionXTitle.Size = new System.Drawing.Size(126, 20);
            this.lblVisionXTitle.TabIndex = 0;
            this.lblVisionXTitle.Text = "VISION AXIS X";
            this.lblVisionXTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVisionXValue
            // 
            this.lblVisionXValue.BackColor = System.Drawing.Color.White;
            this.lblVisionXValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblVisionXValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionXValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblVisionXValue.Location = new System.Drawing.Point(3, 20);
            this.lblVisionXValue.Name = "lblVisionXValue";
            this.lblVisionXValue.Size = new System.Drawing.Size(126, 27);
            this.lblVisionXValue.TabIndex = 1;
            this.lblVisionXValue.Text = "0 um";
            this.lblVisionXValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // grpAction
            // 
            this.grpAction.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpAction.Controls.Add(this.actionBar);
            this.grpAction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAction.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpAction.Location = new System.Drawing.Point(0, 303);
            this.grpAction.Margin = new System.Windows.Forms.Padding(0, 3, 3, 0);
            this.grpAction.Name = "grpAction";
            this.grpAction.Size = new System.Drawing.Size(836, 567);
            this.grpAction.TabIndex = 1;
            this.grpAction.TabStop = false;
            this.grpAction.Text = "ACTION";
            // 
            // actionBar
            // 
            this.actionBar.ColumnCount = 1;
            this.actionBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionBar.Controls.Add(this.actionPanel, 0, 0);
            this.actionBar.Controls.Add(this.actionRightPanel, 0, 1);
            this.actionBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionBar.Location = new System.Drawing.Point(3, 23);
            this.actionBar.Margin = new System.Windows.Forms.Padding(0);
            this.actionBar.Name = "actionBar";
            this.actionBar.RowCount = 2;
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 230F));
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionBar.Size = new System.Drawing.Size(830, 541);
            this.actionBar.TabIndex = 2;
            // 
            // actionPanel
            // 
            this.actionPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.actionPanel.ColumnCount = 2;
            this.actionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionPanel.Controls.Add(this.btnStageReady, 0, 0);
            this.actionPanel.Controls.Add(this.btnNgStageReady, 1, 0);
            this.actionPanel.Controls.Add(this.btnGoodProcess, 0, 1);
            this.actionPanel.Controls.Add(this.btnNgProcess, 1, 1);
            this.actionPanel.Controls.Add(this.btnGoodReceive, 0, 2);
            this.actionPanel.Controls.Add(this.btnNgReceive, 1, 2);
            this.actionPanel.Controls.Add(this.btnGoodUnload, 0, 3);
            this.actionPanel.Controls.Add(this.btnNgUnload, 1, 3);
            this.actionPanel.Controls.Add(this.btnInspect, 0, 4);
            this.actionPanel.Controls.Add(this.btnStageInit, 1, 4);
            this.actionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionPanel.Location = new System.Drawing.Point(0, 0);
            this.actionPanel.Margin = new System.Windows.Forms.Padding(0);
            this.actionPanel.Name = "actionPanel";
            this.actionPanel.RowCount = 5;
            this.actionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionPanel.Size = new System.Drawing.Size(830, 230);
            this.actionPanel.TabIndex = 0;
            // 
            // btnStageReady
            // 
            this.btnStageReady.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnStageReady.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStageReady.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStageReady.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnStageReady.ForeColor = System.Drawing.Color.White;
            this.btnStageReady.Location = new System.Drawing.Point(3, 3);
            this.btnStageReady.Name = "btnStageReady";
            this.btnStageReady.Size = new System.Drawing.Size(409, 40);
            this.btnStageReady.TabIndex = 0;
            this.btnStageReady.Text = "GOOD LOAD";
            // 
            // btnNgStageReady
            // 
            this.btnNgStageReady.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgStageReady.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgStageReady.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNgStageReady.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNgStageReady.ForeColor = System.Drawing.Color.White;
            this.btnNgStageReady.Location = new System.Drawing.Point(418, 3);
            this.btnNgStageReady.Name = "btnNgStageReady";
            this.btnNgStageReady.Size = new System.Drawing.Size(409, 40);
            this.btnNgStageReady.TabIndex = 1;
            this.btnNgStageReady.Text = "NG LOAD";
            // 
            // btnGoodProcess
            // 
            this.btnGoodProcess.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGoodProcess.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGoodProcess.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoodProcess.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnGoodProcess.ForeColor = System.Drawing.Color.White;
            this.btnGoodProcess.Location = new System.Drawing.Point(3, 49);
            this.btnGoodProcess.Name = "btnGoodProcess";
            this.btnGoodProcess.Size = new System.Drawing.Size(409, 40);
            this.btnGoodProcess.TabIndex = 2;
            this.btnGoodProcess.Text = "GOOD PROCESS";
            // 
            // btnNgProcess
            // 
            this.btnNgProcess.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgProcess.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgProcess.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNgProcess.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNgProcess.ForeColor = System.Drawing.Color.White;
            this.btnNgProcess.Location = new System.Drawing.Point(418, 49);
            this.btnNgProcess.Name = "btnNgProcess";
            this.btnNgProcess.Size = new System.Drawing.Size(409, 40);
            this.btnNgProcess.TabIndex = 3;
            this.btnNgProcess.Text = "NG PROCESS";
            // 
            // btnGoodReceive
            // 
            this.btnGoodReceive.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGoodReceive.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGoodReceive.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoodReceive.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnGoodReceive.ForeColor = System.Drawing.Color.White;
            this.btnGoodReceive.Location = new System.Drawing.Point(3, 95);
            this.btnGoodReceive.Name = "btnGoodReceive";
            this.btnGoodReceive.Size = new System.Drawing.Size(409, 40);
            this.btnGoodReceive.TabIndex = 4;
            this.btnGoodReceive.Text = "RECEIVE GOOD";
            // 
            // btnNgReceive
            // 
            this.btnNgReceive.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgReceive.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgReceive.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNgReceive.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNgReceive.ForeColor = System.Drawing.Color.White;
            this.btnNgReceive.Location = new System.Drawing.Point(418, 95);
            this.btnNgReceive.Name = "btnNgReceive";
            this.btnNgReceive.Size = new System.Drawing.Size(409, 40);
            this.btnNgReceive.TabIndex = 5;
            this.btnNgReceive.Text = "RECEIVE NG";
            // 
            // btnGoodUnload
            // 
            this.btnGoodUnload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGoodUnload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGoodUnload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGoodUnload.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnGoodUnload.ForeColor = System.Drawing.Color.White;
            this.btnGoodUnload.Location = new System.Drawing.Point(3, 141);
            this.btnGoodUnload.Name = "btnGoodUnload";
            this.btnGoodUnload.Size = new System.Drawing.Size(409, 40);
            this.btnGoodUnload.TabIndex = 6;
            this.btnGoodUnload.Text = "GOOD UNLOAD";
            // 
            // btnNgUnload
            // 
            this.btnNgUnload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgUnload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgUnload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNgUnload.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNgUnload.ForeColor = System.Drawing.Color.White;
            this.btnNgUnload.Location = new System.Drawing.Point(418, 141);
            this.btnNgUnload.Name = "btnNgUnload";
            this.btnNgUnload.Size = new System.Drawing.Size(409, 40);
            this.btnNgUnload.TabIndex = 7;
            this.btnNgUnload.Text = "NG UNLOAD";
            // 
            // btnInspect
            // 
            this.btnInspect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnInspect.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInspect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInspect.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnInspect.ForeColor = System.Drawing.Color.White;
            this.btnInspect.Location = new System.Drawing.Point(3, 187);
            this.btnInspect.Name = "btnInspect";
            this.btnInspect.Size = new System.Drawing.Size(409, 40);
            this.btnInspect.TabIndex = 8;
            this.btnInspect.Text = "INSPECT";
            // 
            // btnStageInit
            // 
            this.btnStageInit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnStageInit.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStageInit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStageInit.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnStageInit.ForeColor = System.Drawing.Color.White;
            this.btnStageInit.Location = new System.Drawing.Point(418, 187);
            this.btnStageInit.Name = "btnStageInit";
            this.btnStageInit.Size = new System.Drawing.Size(409, 40);
            this.btnStageInit.TabIndex = 9;
            this.btnStageInit.Text = "AVOID";
            // 
            // actionRightPanel
            // 
            this.actionRightPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.actionRightPanel.ColumnCount = 2;
            this.actionRightPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionRightPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionRightPanel.Controls.Add(this.btnStop, 1, 0);
            this.actionRightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionRightPanel.Location = new System.Drawing.Point(0, 230);
            this.actionRightPanel.Margin = new System.Windows.Forms.Padding(0);
            this.actionRightPanel.Name = "actionRightPanel";
            this.actionRightPanel.RowCount = 3;
            this.actionRightPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionRightPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionRightPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionRightPanel.Size = new System.Drawing.Size(830, 311);
            this.actionRightPanel.TabIndex = 1;
            // 
            // btnStop
            // 
            this.btnStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(214)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStop.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.Location = new System.Drawing.Point(418, 3);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(409, 40);
            this.btnStop.TabIndex = 10;
            this.btnStop.Text = "STOP";
            // 
            // materialPanel
            // 
            this.materialPanel.ColumnCount = 1;
            this.materialPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materialPanel.Controls.Add(this.materialHeaderLayout, 0, 0);
            this.materialPanel.Controls.Add(this.materialDetailView, 0, 1);
            this.materialPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialPanel.Location = new System.Drawing.Point(842, 0);
            this.materialPanel.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.materialPanel.Name = "materialPanel";
            this.materialPanel.RowCount = 2;
            this.contentLayout.SetRowSpan(this.materialPanel, 2);
            this.materialPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 76F));
            this.materialPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materialPanel.Size = new System.Drawing.Size(836, 870);
            this.materialPanel.TabIndex = 1;
            // 
            // materialHeaderLayout
            // 
            this.materialHeaderLayout.ColumnCount = 4;
            this.materialHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materialHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.materialHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.materialHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 8F));
            this.materialHeaderLayout.Controls.Add(this.lblMaterialTitle, 0, 0);
            this.materialHeaderLayout.Controls.Add(this.rdoGoodMaterial, 1, 0);
            this.materialHeaderLayout.Controls.Add(this.rdoNgMaterial, 2, 0);
            this.materialHeaderLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialHeaderLayout.Location = new System.Drawing.Point(3, 3);
            this.materialHeaderLayout.Name = "materialHeaderLayout";
            this.materialHeaderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.materialHeaderLayout.Size = new System.Drawing.Size(830, 70);
            this.materialHeaderLayout.TabIndex = 0;
            // 
            // lblMaterialTitle
            // 
            this.lblMaterialTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMaterialTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblMaterialTitle.Location = new System.Drawing.Point(3, 0);
            this.lblMaterialTitle.Name = "lblMaterialTitle";
            this.lblMaterialTitle.Size = new System.Drawing.Size(636, 70);
            this.lblMaterialTitle.TabIndex = 0;
            this.lblMaterialTitle.Text = "STAGE";
            this.lblMaterialTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // rdoGoodMaterial
            // 
            this.rdoGoodMaterial.Appearance = System.Windows.Forms.Appearance.Button;
            this.rdoGoodMaterial.Checked = true;
            this.rdoGoodMaterial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoGoodMaterial.Location = new System.Drawing.Point(645, 3);
            this.rdoGoodMaterial.Name = "rdoGoodMaterial";
            this.rdoGoodMaterial.Size = new System.Drawing.Size(84, 64);
            this.rdoGoodMaterial.TabIndex = 1;
            this.rdoGoodMaterial.TabStop = true;
            this.rdoGoodMaterial.Text = "GOOD";
            this.rdoGoodMaterial.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // rdoNgMaterial
            // 
            this.rdoNgMaterial.Appearance = System.Windows.Forms.Appearance.Button;
            this.rdoNgMaterial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoNgMaterial.Location = new System.Drawing.Point(735, 3);
            this.rdoNgMaterial.Name = "rdoNgMaterial";
            this.rdoNgMaterial.Size = new System.Drawing.Size(84, 64);
            this.rdoNgMaterial.TabIndex = 2;
            this.rdoNgMaterial.Text = "NG";
            this.rdoNgMaterial.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // materialDetailView
            // 
            this.materialDetailView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.materialDetailView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialDetailView.Location = new System.Drawing.Point(4, 80);
            this.materialDetailView.Margin = new System.Windows.Forms.Padding(0);
            this.materialDetailView.Name = "materialDetailView";
            this.materialDetailView.ShowProcessTestDataButton = false;
            this.materialDetailView.Size = new System.Drawing.Size(828, 786);
            this.materialDetailView.TabIndex = 1;
            // 
            // OutputStagePage
            // 
            this.Controls.Add(this.rootLayout);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "OutputStagePage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.grpState.ResumeLayout(false);
            this.stateLayout.ResumeLayout(false);
            this.grpCounters.ResumeLayout(false);
            this.counterLayout.ResumeLayout(false);
            this.grpCylinder.ResumeLayout(false);
            this.cylinderLayout.ResumeLayout(false);
            this.grpInfo.ResumeLayout(false);
            this.infoLayout.ResumeLayout(false);
            this.goodYPanel.ResumeLayout(false);
            this.goodZPanel.ResumeLayout(false);
            this.ngYPanel.ResumeLayout(false);
            this.visionXPanel.ResumeLayout(false);
            this.grpAction.ResumeLayout(false);
            this.actionBar.ResumeLayout(false);
            this.actionPanel.ResumeLayout(false);
            this.actionRightPanel.ResumeLayout(false);
            this.materialPanel.ResumeLayout(false);
            this.materialHeaderLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
