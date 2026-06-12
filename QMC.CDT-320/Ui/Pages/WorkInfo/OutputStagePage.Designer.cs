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
        private FlowLayoutPanel actionPanel;
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
        private Label lblNgGuideTitle;
        private Label lblNgGuideValue;
        private Label lblNgClampTitle;
        private Label lblNgClampValue;
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
            this.lblNgGuideTitle = new System.Windows.Forms.Label();
            this.lblNgGuideValue = new System.Windows.Forms.Label();
            this.lblNgClampTitle = new System.Windows.Forms.Label();
            this.lblNgClampValue = new System.Windows.Forms.Label();
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
            this.materialPanel = new System.Windows.Forms.TableLayoutPanel();
            this.materialHeaderLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblMaterialTitle = new System.Windows.Forms.Label();
            this.rdoGoodMaterial = new System.Windows.Forms.RadioButton();
            this.rdoNgMaterial = new System.Windows.Forms.RadioButton();
            this.materialDetailView = new QMC.CDT_320.Ui.Controls.MaterialDetailView();
            this.actionPanel = new System.Windows.Forms.FlowLayoutPanel();
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
            this.btnStop = new QMC.CDT_320.Ui.Controls.ActionButton();
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
            this.materialPanel.SuspendLayout();
            this.materialHeaderLayout.SuspendLayout();
            this.actionPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 1);
            this.rootLayout.Controls.Add(this.actionPanel, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(3, 0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1672, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "OUTPUT STAGE";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.contentLayout.Controls.Add(this.leftLayout, 0, 0);
            this.contentLayout.Controls.Add(this.materialPanel, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(3, 33);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(8);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1672, 774);
            this.contentLayout.TabIndex = 1;
            // 
            // leftLayout
            // 
            this.leftLayout.ColumnCount = 3;
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 27F));
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.leftLayout.Controls.Add(this.grpState, 0, 0);
            this.leftLayout.Controls.Add(this.grpCounters, 1, 0);
            this.leftLayout.Controls.Add(this.grpCylinder, 2, 0);
            this.leftLayout.Controls.Add(this.grpInfo, 0, 1);
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Location = new System.Drawing.Point(11, 11);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.RowCount = 2;
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Size = new System.Drawing.Size(987, 752);
            this.leftLayout.TabIndex = 0;
            // 
            // grpState
            // 
            this.grpState.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpState.Controls.Add(this.stateLayout);
            this.grpState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpState.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpState.Location = new System.Drawing.Point(3, 3);
            this.grpState.Name = "grpState";
            this.grpState.Size = new System.Drawing.Size(319, 214);
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
            this.stateLayout.Padding = new System.Windows.Forms.Padding(12, 18, 12, 12);
            this.stateLayout.RowCount = 6;
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.stateLayout.Size = new System.Drawing.Size(313, 188);
            this.stateLayout.TabIndex = 0;
            // 
            // lblGoodExistTitle
            // 
            this.lblGoodExistTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblGoodExistTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodExistTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodExistTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGoodExistTitle.Location = new System.Drawing.Point(15, 18);
            this.lblGoodExistTitle.Name = "lblGoodExistTitle";
            this.lblGoodExistTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGoodExistTitle.Size = new System.Drawing.Size(161, 32);
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
            this.lblGoodExistValue.Location = new System.Drawing.Point(182, 18);
            this.lblGoodExistValue.Name = "lblGoodExistValue";
            this.lblGoodExistValue.Size = new System.Drawing.Size(116, 32);
            this.lblGoodExistValue.TabIndex = 1;
            this.lblGoodExistValue.Text = "EMPTY";
            this.lblGoodExistValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblGoodStateTitle
            // 
            this.lblGoodStateTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblGoodStateTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodStateTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodStateTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGoodStateTitle.Location = new System.Drawing.Point(15, 50);
            this.lblGoodStateTitle.Name = "lblGoodStateTitle";
            this.lblGoodStateTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGoodStateTitle.Size = new System.Drawing.Size(161, 32);
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
            this.lblGoodStateValue.Location = new System.Drawing.Point(182, 50);
            this.lblGoodStateValue.Name = "lblGoodStateValue";
            this.lblGoodStateValue.Size = new System.Drawing.Size(116, 32);
            this.lblGoodStateValue.TabIndex = 3;
            this.lblGoodStateValue.Text = "INCOMPLETE";
            this.lblGoodStateValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblNgExistTitle
            // 
            this.lblNgExistTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNgExistTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgExistTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgExistTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblNgExistTitle.Location = new System.Drawing.Point(15, 82);
            this.lblNgExistTitle.Name = "lblNgExistTitle";
            this.lblNgExistTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNgExistTitle.Size = new System.Drawing.Size(161, 32);
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
            this.lblNgExistValue.Location = new System.Drawing.Point(182, 82);
            this.lblNgExistValue.Name = "lblNgExistValue";
            this.lblNgExistValue.Size = new System.Drawing.Size(116, 32);
            this.lblNgExistValue.TabIndex = 5;
            this.lblNgExistValue.Text = "EMPTY";
            this.lblNgExistValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblNgStateTitle
            // 
            this.lblNgStateTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNgStateTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgStateTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgStateTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblNgStateTitle.Location = new System.Drawing.Point(15, 114);
            this.lblNgStateTitle.Name = "lblNgStateTitle";
            this.lblNgStateTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNgStateTitle.Size = new System.Drawing.Size(161, 32);
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
            this.lblNgStateValue.Location = new System.Drawing.Point(182, 114);
            this.lblNgStateValue.Name = "lblNgStateValue";
            this.lblNgStateValue.Size = new System.Drawing.Size(116, 32);
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
            this.grpCounters.Location = new System.Drawing.Point(328, 3);
            this.grpCounters.Name = "grpCounters";
            this.grpCounters.Size = new System.Drawing.Size(260, 214);
            this.grpCounters.TabIndex = 1;
            this.grpCounters.TabStop = false;
            this.grpCounters.Text = "COUNTER";
            // 
            // counterLayout
            // 
            this.counterLayout.ColumnCount = 2;
            this.counterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this.counterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.counterLayout.Controls.Add(this.lblGoodCountTitle, 0, 0);
            this.counterLayout.Controls.Add(this.lblGoodCountValue, 1, 0);
            this.counterLayout.Controls.Add(this.lblNgCountTitle, 0, 1);
            this.counterLayout.Controls.Add(this.lblNgCountValue, 1, 1);
            this.counterLayout.Controls.Add(this.lblTotalCountTitle, 0, 2);
            this.counterLayout.Controls.Add(this.lblTotalCountValue, 1, 2);
            this.counterLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.counterLayout.Location = new System.Drawing.Point(3, 23);
            this.counterLayout.Name = "counterLayout";
            this.counterLayout.Padding = new System.Windows.Forms.Padding(12, 18, 12, 12);
            this.counterLayout.RowCount = 4;
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.counterLayout.Size = new System.Drawing.Size(254, 188);
            this.counterLayout.TabIndex = 0;
            // 
            // lblGoodCountTitle
            // 
            this.lblGoodCountTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblGoodCountTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodCountTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodCountTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGoodCountTitle.Location = new System.Drawing.Point(15, 18);
            this.lblGoodCountTitle.Name = "lblGoodCountTitle";
            this.lblGoodCountTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGoodCountTitle.Size = new System.Drawing.Size(127, 32);
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
            this.lblGoodCountValue.Location = new System.Drawing.Point(148, 18);
            this.lblGoodCountValue.Name = "lblGoodCountValue";
            this.lblGoodCountValue.Size = new System.Drawing.Size(91, 32);
            this.lblGoodCountValue.TabIndex = 1;
            this.lblGoodCountValue.Text = "0 ea";
            this.lblGoodCountValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblNgCountTitle
            // 
            this.lblNgCountTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNgCountTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgCountTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgCountTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblNgCountTitle.Location = new System.Drawing.Point(15, 50);
            this.lblNgCountTitle.Name = "lblNgCountTitle";
            this.lblNgCountTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNgCountTitle.Size = new System.Drawing.Size(127, 32);
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
            this.lblNgCountValue.Location = new System.Drawing.Point(148, 50);
            this.lblNgCountValue.Name = "lblNgCountValue";
            this.lblNgCountValue.Size = new System.Drawing.Size(91, 32);
            this.lblNgCountValue.TabIndex = 3;
            this.lblNgCountValue.Text = "0 ea";
            this.lblNgCountValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblTotalCountTitle
            // 
            this.lblTotalCountTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblTotalCountTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblTotalCountTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalCountTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblTotalCountTitle.Location = new System.Drawing.Point(15, 82);
            this.lblTotalCountTitle.Name = "lblTotalCountTitle";
            this.lblTotalCountTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblTotalCountTitle.Size = new System.Drawing.Size(127, 32);
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
            this.lblTotalCountValue.Location = new System.Drawing.Point(148, 82);
            this.lblTotalCountValue.Name = "lblTotalCountValue";
            this.lblTotalCountValue.Size = new System.Drawing.Size(91, 32);
            this.lblTotalCountValue.TabIndex = 5;
            this.lblTotalCountValue.Text = "0 ea";
            this.lblTotalCountValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // grpCylinder
            // 
            this.grpCylinder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpCylinder.Controls.Add(this.cylinderLayout);
            this.grpCylinder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCylinder.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpCylinder.Location = new System.Drawing.Point(594, 3);
            this.grpCylinder.Name = "grpCylinder";
            this.grpCylinder.Size = new System.Drawing.Size(390, 214);
            this.grpCylinder.TabIndex = 2;
            this.grpCylinder.TabStop = false;
            this.grpCylinder.Text = "CYLINDER INFO";
            // 
            // cylinderLayout
            // 
            this.cylinderLayout.ColumnCount = 2;
            this.cylinderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this.cylinderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.cylinderLayout.Controls.Add(this.lblGoodGuideTitle, 0, 0);
            this.cylinderLayout.Controls.Add(this.lblGoodGuideValue, 1, 0);
            this.cylinderLayout.Controls.Add(this.lblGoodClampTitle, 0, 1);
            this.cylinderLayout.Controls.Add(this.lblGoodClampValue, 1, 1);
            this.cylinderLayout.Controls.Add(this.lblNgGuideTitle, 0, 2);
            this.cylinderLayout.Controls.Add(this.lblNgGuideValue, 1, 2);
            this.cylinderLayout.Controls.Add(this.lblNgClampTitle, 0, 3);
            this.cylinderLayout.Controls.Add(this.lblNgClampValue, 1, 3);
            this.cylinderLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cylinderLayout.Location = new System.Drawing.Point(3, 23);
            this.cylinderLayout.Name = "cylinderLayout";
            this.cylinderLayout.Padding = new System.Windows.Forms.Padding(12, 18, 12, 12);
            this.cylinderLayout.RowCount = 6;
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.cylinderLayout.Size = new System.Drawing.Size(384, 188);
            this.cylinderLayout.TabIndex = 0;
            // 
            // lblGoodGuideTitle
            // 
            this.lblGoodGuideTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblGoodGuideTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodGuideTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodGuideTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGoodGuideTitle.Location = new System.Drawing.Point(15, 18);
            this.lblGoodGuideTitle.Name = "lblGoodGuideTitle";
            this.lblGoodGuideTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGoodGuideTitle.Size = new System.Drawing.Size(202, 32);
            this.lblGoodGuideTitle.TabIndex = 0;
            this.lblGoodGuideTitle.Text = "GOOD GUIDE";
            this.lblGoodGuideTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblGoodGuideValue
            // 
            this.lblGoodGuideValue.BackColor = System.Drawing.Color.White;
            this.lblGoodGuideValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodGuideValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodGuideValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblGoodGuideValue.Location = new System.Drawing.Point(223, 18);
            this.lblGoodGuideValue.Name = "lblGoodGuideValue";
            this.lblGoodGuideValue.Size = new System.Drawing.Size(146, 32);
            this.lblGoodGuideValue.TabIndex = 1;
            this.lblGoodGuideValue.Text = "--";
            this.lblGoodGuideValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblGoodClampTitle
            // 
            this.lblGoodClampTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblGoodClampTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodClampTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodClampTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGoodClampTitle.Location = new System.Drawing.Point(15, 50);
            this.lblGoodClampTitle.Name = "lblGoodClampTitle";
            this.lblGoodClampTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGoodClampTitle.Size = new System.Drawing.Size(202, 32);
            this.lblGoodClampTitle.TabIndex = 2;
            this.lblGoodClampTitle.Text = "GOOD CLAMP";
            this.lblGoodClampTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblGoodClampValue
            // 
            this.lblGoodClampValue.BackColor = System.Drawing.Color.White;
            this.lblGoodClampValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblGoodClampValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodClampValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblGoodClampValue.Location = new System.Drawing.Point(223, 50);
            this.lblGoodClampValue.Name = "lblGoodClampValue";
            this.lblGoodClampValue.Size = new System.Drawing.Size(146, 32);
            this.lblGoodClampValue.TabIndex = 3;
            this.lblGoodClampValue.Text = "--";
            this.lblGoodClampValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblNgGuideTitle
            // 
            this.lblNgGuideTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNgGuideTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgGuideTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgGuideTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblNgGuideTitle.Location = new System.Drawing.Point(15, 82);
            this.lblNgGuideTitle.Name = "lblNgGuideTitle";
            this.lblNgGuideTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNgGuideTitle.Size = new System.Drawing.Size(202, 32);
            this.lblNgGuideTitle.TabIndex = 4;
            this.lblNgGuideTitle.Text = "NG GUIDE";
            this.lblNgGuideTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNgGuideValue
            // 
            this.lblNgGuideValue.BackColor = System.Drawing.Color.White;
            this.lblNgGuideValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgGuideValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgGuideValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblNgGuideValue.Location = new System.Drawing.Point(223, 82);
            this.lblNgGuideValue.Name = "lblNgGuideValue";
            this.lblNgGuideValue.Size = new System.Drawing.Size(146, 32);
            this.lblNgGuideValue.TabIndex = 5;
            this.lblNgGuideValue.Text = "--";
            this.lblNgGuideValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblNgClampTitle
            // 
            this.lblNgClampTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNgClampTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgClampTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgClampTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblNgClampTitle.Location = new System.Drawing.Point(15, 114);
            this.lblNgClampTitle.Name = "lblNgClampTitle";
            this.lblNgClampTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNgClampTitle.Size = new System.Drawing.Size(202, 32);
            this.lblNgClampTitle.TabIndex = 6;
            this.lblNgClampTitle.Text = "NG CLAMP";
            this.lblNgClampTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNgClampValue
            // 
            this.lblNgClampValue.BackColor = System.Drawing.Color.White;
            this.lblNgClampValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgClampValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgClampValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblNgClampValue.Location = new System.Drawing.Point(223, 114);
            this.lblNgClampValue.Name = "lblNgClampValue";
            this.lblNgClampValue.Size = new System.Drawing.Size(146, 32);
            this.lblNgClampValue.TabIndex = 7;
            this.lblNgClampValue.Text = "--";
            this.lblNgClampValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // grpInfo
            // 
            this.grpInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.leftLayout.SetColumnSpan(this.grpInfo, 3);
            this.grpInfo.Controls.Add(this.infoLayout);
            this.grpInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInfo.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpInfo.Location = new System.Drawing.Point(3, 223);
            this.grpInfo.Name = "grpInfo";
            this.grpInfo.Size = new System.Drawing.Size(981, 526);
            this.grpInfo.TabIndex = 3;
            this.grpInfo.TabStop = false;
            this.grpInfo.Text = "INFO";
            // 
            // infoLayout
            // 
            this.infoLayout.ColumnCount = 3;
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.infoLayout.Controls.Add(this.goodYPanel, 0, 0);
            this.infoLayout.Controls.Add(this.goodZPanel, 1, 0);
            this.infoLayout.Controls.Add(this.ngYPanel, 0, 1);
            this.infoLayout.Controls.Add(this.visionXPanel, 1, 1);
            this.infoLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoLayout.Location = new System.Drawing.Point(3, 23);
            this.infoLayout.Name = "infoLayout";
            this.infoLayout.Padding = new System.Windows.Forms.Padding(18, 28, 18, 18);
            this.infoLayout.RowCount = 4;
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.infoLayout.Size = new System.Drawing.Size(975, 500);
            this.infoLayout.TabIndex = 0;
            // 
            // goodYPanel
            // 
            this.goodYPanel.ColumnCount = 1;
            this.goodYPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.goodYPanel.Controls.Add(this.lblGoodYTitle, 0, 0);
            this.goodYPanel.Controls.Add(this.lblGoodYValue, 0, 1);
            this.goodYPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.goodYPanel.Location = new System.Drawing.Point(22, 32);
            this.goodYPanel.Margin = new System.Windows.Forms.Padding(4);
            this.goodYPanel.Name = "goodYPanel";
            this.goodYPanel.RowCount = 2;
            this.goodYPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.goodYPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.goodYPanel.Size = new System.Drawing.Size(304, 56);
            this.goodYPanel.TabIndex = 0;
            // 
            // lblGoodYTitle
            // 
            this.lblGoodYTitle.BackColor = System.Drawing.Color.Black;
            this.lblGoodYTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodYTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGoodYTitle.ForeColor = System.Drawing.Color.White;
            this.lblGoodYTitle.Location = new System.Drawing.Point(3, 0);
            this.lblGoodYTitle.Name = "lblGoodYTitle";
            this.lblGoodYTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGoodYTitle.Size = new System.Drawing.Size(298, 26);
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
            this.lblGoodYValue.Location = new System.Drawing.Point(3, 26);
            this.lblGoodYValue.Name = "lblGoodYValue";
            this.lblGoodYValue.Size = new System.Drawing.Size(298, 32);
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
            this.goodZPanel.Location = new System.Drawing.Point(334, 32);
            this.goodZPanel.Margin = new System.Windows.Forms.Padding(4);
            this.goodZPanel.Name = "goodZPanel";
            this.goodZPanel.RowCount = 2;
            this.goodZPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.goodZPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.goodZPanel.Size = new System.Drawing.Size(304, 56);
            this.goodZPanel.TabIndex = 1;
            // 
            // lblGoodZTitle
            // 
            this.lblGoodZTitle.BackColor = System.Drawing.Color.Black;
            this.lblGoodZTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodZTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGoodZTitle.ForeColor = System.Drawing.Color.White;
            this.lblGoodZTitle.Location = new System.Drawing.Point(3, 0);
            this.lblGoodZTitle.Name = "lblGoodZTitle";
            this.lblGoodZTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblGoodZTitle.Size = new System.Drawing.Size(298, 26);
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
            this.lblGoodZValue.Location = new System.Drawing.Point(3, 26);
            this.lblGoodZValue.Name = "lblGoodZValue";
            this.lblGoodZValue.Size = new System.Drawing.Size(298, 32);
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
            this.ngYPanel.Location = new System.Drawing.Point(22, 96);
            this.ngYPanel.Margin = new System.Windows.Forms.Padding(4);
            this.ngYPanel.Name = "ngYPanel";
            this.ngYPanel.RowCount = 2;
            this.ngYPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.ngYPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.ngYPanel.Size = new System.Drawing.Size(304, 56);
            this.ngYPanel.TabIndex = 2;
            // 
            // lblNgYTitle
            // 
            this.lblNgYTitle.BackColor = System.Drawing.Color.Black;
            this.lblNgYTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgYTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblNgYTitle.ForeColor = System.Drawing.Color.White;
            this.lblNgYTitle.Location = new System.Drawing.Point(3, 0);
            this.lblNgYTitle.Name = "lblNgYTitle";
            this.lblNgYTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNgYTitle.Size = new System.Drawing.Size(298, 26);
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
            this.lblNgYValue.Location = new System.Drawing.Point(3, 26);
            this.lblNgYValue.Name = "lblNgYValue";
            this.lblNgYValue.Size = new System.Drawing.Size(298, 32);
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
            this.visionXPanel.Location = new System.Drawing.Point(334, 96);
            this.visionXPanel.Margin = new System.Windows.Forms.Padding(4);
            this.visionXPanel.Name = "visionXPanel";
            this.visionXPanel.RowCount = 2;
            this.visionXPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.visionXPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.visionXPanel.Size = new System.Drawing.Size(304, 56);
            this.visionXPanel.TabIndex = 3;
            // 
            // lblVisionXTitle
            // 
            this.lblVisionXTitle.BackColor = System.Drawing.Color.Black;
            this.lblVisionXTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionXTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblVisionXTitle.ForeColor = System.Drawing.Color.White;
            this.lblVisionXTitle.Location = new System.Drawing.Point(3, 0);
            this.lblVisionXTitle.Name = "lblVisionXTitle";
            this.lblVisionXTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblVisionXTitle.Size = new System.Drawing.Size(298, 26);
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
            this.lblVisionXValue.Location = new System.Drawing.Point(3, 26);
            this.lblVisionXValue.Name = "lblVisionXValue";
            this.lblVisionXValue.Size = new System.Drawing.Size(298, 32);
            this.lblVisionXValue.TabIndex = 1;
            this.lblVisionXValue.Text = "0 um";
            this.lblVisionXValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // materialPanel
            // 
            this.materialPanel.ColumnCount = 1;
            this.materialPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materialPanel.Controls.Add(this.materialHeaderLayout, 0, 0);
            this.materialPanel.Controls.Add(this.materialDetailView, 0, 1);
            this.materialPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialPanel.Location = new System.Drawing.Point(1004, 11);
            this.materialPanel.Name = "materialPanel";
            this.materialPanel.RowCount = 2;
            this.materialPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.materialPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materialPanel.Size = new System.Drawing.Size(657, 752);
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
            this.materialHeaderLayout.Size = new System.Drawing.Size(651, 28);
            this.materialHeaderLayout.TabIndex = 0;
            // 
            // lblMaterialTitle
            // 
            this.lblMaterialTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMaterialTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblMaterialTitle.Location = new System.Drawing.Point(3, 0);
            this.lblMaterialTitle.Name = "lblMaterialTitle";
            this.lblMaterialTitle.Size = new System.Drawing.Size(457, 28);
            this.lblMaterialTitle.TabIndex = 0;
            this.lblMaterialTitle.Text = "MATERIAL";
            this.lblMaterialTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // rdoGoodMaterial
            // 
            this.rdoGoodMaterial.Appearance = System.Windows.Forms.Appearance.Button;
            this.rdoGoodMaterial.Checked = true;
            this.rdoGoodMaterial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoGoodMaterial.Location = new System.Drawing.Point(466, 3);
            this.rdoGoodMaterial.Name = "rdoGoodMaterial";
            this.rdoGoodMaterial.Size = new System.Drawing.Size(84, 22);
            this.rdoGoodMaterial.TabIndex = 1;
            this.rdoGoodMaterial.TabStop = true;
            this.rdoGoodMaterial.Text = "GOOD";
            this.rdoGoodMaterial.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // rdoNgMaterial
            // 
            this.rdoNgMaterial.Appearance = System.Windows.Forms.Appearance.Button;
            this.rdoNgMaterial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoNgMaterial.Location = new System.Drawing.Point(556, 3);
            this.rdoNgMaterial.Name = "rdoNgMaterial";
            this.rdoNgMaterial.Size = new System.Drawing.Size(84, 22);
            this.rdoNgMaterial.TabIndex = 2;
            this.rdoNgMaterial.Text = "NG";
            this.rdoNgMaterial.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // materialDetailView
            // 
            this.materialDetailView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.materialDetailView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialDetailView.Location = new System.Drawing.Point(4, 38);
            this.materialDetailView.Margin = new System.Windows.Forms.Padding(4);
            this.materialDetailView.Name = "materialDetailView";
            this.materialDetailView.Size = new System.Drawing.Size(649, 710);
            this.materialDetailView.TabIndex = 1;
            // 
            // actionPanel
            // 
            this.actionPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.actionPanel.Controls.Add(this.btnStageReady);
            this.actionPanel.Controls.Add(this.btnNgStageReady);
            this.actionPanel.Controls.Add(this.btnGoodProcess);
            this.actionPanel.Controls.Add(this.btnNgProcess);
            this.actionPanel.Controls.Add(this.btnGoodReceive);
            this.actionPanel.Controls.Add(this.btnNgReceive);
            this.actionPanel.Controls.Add(this.btnGoodUnload);
            this.actionPanel.Controls.Add(this.btnNgUnload);
            this.actionPanel.Controls.Add(this.btnInspect);
            this.actionPanel.Controls.Add(this.btnStageInit);
            this.actionPanel.Controls.Add(this.btnStop);
            this.actionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionPanel.Location = new System.Drawing.Point(3, 813);
            this.actionPanel.Name = "actionPanel";
            this.actionPanel.Padding = new System.Windows.Forms.Padding(18, 10, 18, 10);
            this.actionPanel.Size = new System.Drawing.Size(1672, 84);
            this.actionPanel.TabIndex = 2;
            this.actionPanel.WrapContents = false;
            // 
            // btnStageReady
            // 
            this.btnStageReady.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnStageReady.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStageReady.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnStageReady.ForeColor = System.Drawing.Color.White;
            this.btnStageReady.Location = new System.Drawing.Point(24, 16);
            this.btnStageReady.Margin = new System.Windows.Forms.Padding(6);
            this.btnStageReady.Name = "btnStageReady";
            this.btnStageReady.Size = new System.Drawing.Size(128, 64);
            this.btnStageReady.TabIndex = 0;
            this.btnStageReady.Text = "GOOD LOAD";
            // 
            // btnNgStageReady
            // 
            this.btnNgStageReady.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgStageReady.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgStageReady.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnNgStageReady.ForeColor = System.Drawing.Color.White;
            this.btnNgStageReady.Location = new System.Drawing.Point(164, 16);
            this.btnNgStageReady.Margin = new System.Windows.Forms.Padding(6);
            this.btnNgStageReady.Name = "btnNgStageReady";
            this.btnNgStageReady.Size = new System.Drawing.Size(128, 64);
            this.btnNgStageReady.TabIndex = 1;
            this.btnNgStageReady.Text = "NG LOAD";
            // 
            // btnGoodProcess
            // 
            this.btnGoodProcess.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGoodProcess.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGoodProcess.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnGoodProcess.ForeColor = System.Drawing.Color.White;
            this.btnGoodProcess.Location = new System.Drawing.Point(304, 16);
            this.btnGoodProcess.Margin = new System.Windows.Forms.Padding(6);
            this.btnGoodProcess.Name = "btnGoodProcess";
            this.btnGoodProcess.Size = new System.Drawing.Size(128, 64);
            this.btnGoodProcess.TabIndex = 2;
            this.btnGoodProcess.Text = "GOOD PROCESS";
            // 
            // btnNgProcess
            // 
            this.btnNgProcess.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgProcess.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgProcess.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnNgProcess.ForeColor = System.Drawing.Color.White;
            this.btnNgProcess.Location = new System.Drawing.Point(444, 16);
            this.btnNgProcess.Margin = new System.Windows.Forms.Padding(6);
            this.btnNgProcess.Name = "btnNgProcess";
            this.btnNgProcess.Size = new System.Drawing.Size(128, 64);
            this.btnNgProcess.TabIndex = 3;
            this.btnNgProcess.Text = "NG PROCESS";
            // 
            // btnGoodReceive
            // 
            this.btnGoodReceive.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGoodReceive.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGoodReceive.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnGoodReceive.ForeColor = System.Drawing.Color.White;
            this.btnGoodReceive.Location = new System.Drawing.Point(584, 16);
            this.btnGoodReceive.Margin = new System.Windows.Forms.Padding(6);
            this.btnGoodReceive.Name = "btnGoodReceive";
            this.btnGoodReceive.Size = new System.Drawing.Size(128, 64);
            this.btnGoodReceive.TabIndex = 4;
            this.btnGoodReceive.Text = "RECEIVE GOOD";
            // 
            // btnNgReceive
            // 
            this.btnNgReceive.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgReceive.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgReceive.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnNgReceive.ForeColor = System.Drawing.Color.White;
            this.btnNgReceive.Location = new System.Drawing.Point(724, 16);
            this.btnNgReceive.Margin = new System.Windows.Forms.Padding(6);
            this.btnNgReceive.Name = "btnNgReceive";
            this.btnNgReceive.Size = new System.Drawing.Size(128, 64);
            this.btnNgReceive.TabIndex = 5;
            this.btnNgReceive.Text = "RECEIVE NG";
            // 
            // btnGoodUnload
            // 
            this.btnGoodUnload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGoodUnload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGoodUnload.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnGoodUnload.ForeColor = System.Drawing.Color.White;
            this.btnGoodUnload.Location = new System.Drawing.Point(864, 16);
            this.btnGoodUnload.Margin = new System.Windows.Forms.Padding(6);
            this.btnGoodUnload.Name = "btnGoodUnload";
            this.btnGoodUnload.Size = new System.Drawing.Size(128, 64);
            this.btnGoodUnload.TabIndex = 6;
            this.btnGoodUnload.Text = "GOOD UNLOAD";
            // 
            // btnNgUnload
            // 
            this.btnNgUnload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNgUnload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNgUnload.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnNgUnload.ForeColor = System.Drawing.Color.White;
            this.btnNgUnload.Location = new System.Drawing.Point(1004, 16);
            this.btnNgUnload.Margin = new System.Windows.Forms.Padding(6);
            this.btnNgUnload.Name = "btnNgUnload";
            this.btnNgUnload.Size = new System.Drawing.Size(128, 64);
            this.btnNgUnload.TabIndex = 7;
            this.btnNgUnload.Text = "NG UNLOAD";
            // 
            // btnInspect
            // 
            this.btnInspect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnInspect.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInspect.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnInspect.ForeColor = System.Drawing.Color.White;
            this.btnInspect.Location = new System.Drawing.Point(1144, 16);
            this.btnInspect.Margin = new System.Windows.Forms.Padding(6);
            this.btnInspect.Name = "btnInspect";
            this.btnInspect.Size = new System.Drawing.Size(128, 64);
            this.btnInspect.TabIndex = 8;
            this.btnInspect.Text = "INSPECT";
            // 
            // btnStageInit
            // 
            this.btnStageInit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnStageInit.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStageInit.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnStageInit.ForeColor = System.Drawing.Color.White;
            this.btnStageInit.Location = new System.Drawing.Point(1284, 16);
            this.btnStageInit.Margin = new System.Windows.Forms.Padding(6);
            this.btnStageInit.Name = "btnStageInit";
            this.btnStageInit.Size = new System.Drawing.Size(128, 64);
            this.btnStageInit.TabIndex = 9;
            this.btnStageInit.Text = "AVOID";
            // 
            // btnStop
            // 
            this.btnStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(168)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.btnStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStop.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.Location = new System.Drawing.Point(1424, 16);
            this.btnStop.Margin = new System.Windows.Forms.Padding(6);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(140, 64);
            this.btnStop.TabIndex = 10;
            this.btnStop.Text = "STOP";
            // 
            // OutputStagePage
            // 
            this.Controls.Add(this.rootLayout);
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
            this.materialPanel.ResumeLayout(false);
            this.materialHeaderLayout.ResumeLayout(false);
            this.actionPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
