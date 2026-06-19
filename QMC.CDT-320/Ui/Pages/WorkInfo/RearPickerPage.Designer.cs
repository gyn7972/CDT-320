using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class RearPickerPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private TableLayoutPanel topLayout;
        private GroupBox grpState;
        private GroupBox grpCounters;
        private GroupBox grpInfo;
        private GroupBox grpAxis;
        private TableLayoutPanel stateLayout;
        private TableLayoutPanel counterLayout;
        private TableLayoutPanel infoLayout;
        private FlowLayoutPanel actionPanel;
        private Button btnInitAll;
        private Button btnCountClear;
        private Label lblHeadZoneTitle;
        private Label lblHeadZoneValue;
        private Label lblHead1Title;
        private Label lblHead1Value;
        private Label lblHead2Title;
        private Label lblHead2Value;
        private Label lblHead3Title;
        private Label lblHead3Value;
        private Label lblHead4Title;
        private Label lblHead4Value;
        private Label lblColletChangeTitle;
        private Label lblColletChangeValue;
        private Label lblAutoPosTitle;
        private Label lblAutoPosValue;
        private Label lblColletCleaningTitle;
        private Label lblColletCleaningValue;
        private Label lblColletCheckTitle;
        private Label lblColletCheckValue;
        private Label lblPickFailTitle;
        private Label lblPickFailValue;
        private Label lblPlaceFailTitle;
        private Label lblPlaceFailValue;
        private Label lblCollet1UseTitle;
        private Label lblCollet1UseValue;
        private Label lblCollet2UseTitle;
        private Label lblCollet2UseValue;
        private Label lblCollet3UseTitle;
        private Label lblCollet3UseValue;
        private Label lblCollet4UseTitle;
        private Label lblCollet4UseValue;
        private TableLayoutPanel headAxisTPanel;
        private Label lblHeadAxisTTitle;
        private Label lblHeadAxisTValue;
        private TableLayoutPanel headProcessPanel;
        private Label lblHeadProcessTitle;
        private Label lblHeadProcessValue;
        private TableLayoutPanel processFlowPanel;
        private Label lblFlowAvoid;
        private Label lblFlowArrow1;
        private Label lblFlowPickup;
        private Label lblFlowArrow2;
        private Label lblFlowBottom;
        private Label lblFlowArrow3;
        private Label lblFlowSide;
        private Label lblFlowArrow4;
        private Label lblFlowPlace;
        private TableLayoutPanel processDetailPanel;
        private Label lblProcessDetailTitle;
        private Label lblProcessDetailValue;
        private TableLayoutPanel headVacuum1Panel;
        private IndicatorDot dotHeadVacuum1;
        private Label lblHeadVacuum1;
        private TableLayoutPanel headVacuum2Panel;
        private IndicatorDot dotHeadVacuum2;
        private Label lblHeadVacuum2;
        private TableLayoutPanel headVacuum3Panel;
        private IndicatorDot dotHeadVacuum3;
        private Label lblHeadVacuum3;
        private TableLayoutPanel headVacuum4Panel;
        private IndicatorDot dotHeadVacuum4;
        private Label lblHeadVacuum4;
        private TableLayoutPanel headBlow1Panel;
        private IndicatorDot dotHeadBlow1;
        private Label lblHeadBlow1;
        private TableLayoutPanel headBlow2Panel;
        private IndicatorDot dotHeadBlow2;
        private Label lblHeadBlow2;
        private TableLayoutPanel headBlow3Panel;
        private IndicatorDot dotHeadBlow3;
        private Label lblHeadBlow3;
        private TableLayoutPanel headBlow4Panel;
        private IndicatorDot dotHeadBlow4;
        private Label lblHeadBlow4;
        private TableLayoutPanel headFlow1Panel;
        private IndicatorDot dotHeadFlow1;
        private Label lblHeadFlow1;
        private TableLayoutPanel headFlow2Panel;
        private IndicatorDot dotHeadFlow2;
        private Label lblHeadFlow2;
        private TableLayoutPanel headFlow3Panel;
        private IndicatorDot dotHeadFlow3;
        private Label lblHeadFlow3;
        private TableLayoutPanel headFlow4Panel;
        private IndicatorDot dotHeadFlow4;
        private Label lblHeadFlow4;
        private DataGridView axisGrid;
        private ActionButton btnInput;
        private ActionButton btnInspect;
        private ActionButton btnBottom;
        private ActionButton btnSide;
        private ActionButton btnOutput;
        private ActionButton btnStop;
        private Panel actionSpacer;

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.topLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpState = new System.Windows.Forms.GroupBox();
            this.stateLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeadZoneTitle = new System.Windows.Forms.Label();
            this.lblHeadZoneValue = new System.Windows.Forms.Label();
            this.lblHead1Title = new System.Windows.Forms.Label();
            this.lblHead1Value = new System.Windows.Forms.Label();
            this.lblHead2Title = new System.Windows.Forms.Label();
            this.lblHead2Value = new System.Windows.Forms.Label();
            this.lblHead3Title = new System.Windows.Forms.Label();
            this.lblHead3Value = new System.Windows.Forms.Label();
            this.lblHead4Title = new System.Windows.Forms.Label();
            this.lblHead4Value = new System.Windows.Forms.Label();
            this.lblColletChangeTitle = new System.Windows.Forms.Label();
            this.lblColletChangeValue = new System.Windows.Forms.Label();
            this.lblAutoPosTitle = new System.Windows.Forms.Label();
            this.lblAutoPosValue = new System.Windows.Forms.Label();
            this.lblColletCleaningTitle = new System.Windows.Forms.Label();
            this.lblColletCleaningValue = new System.Windows.Forms.Label();
            this.lblColletCheckTitle = new System.Windows.Forms.Label();
            this.lblColletCheckValue = new System.Windows.Forms.Label();
            this.btnInitAll = new System.Windows.Forms.Button();
            this.grpCounters = new System.Windows.Forms.GroupBox();
            this.counterLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblPickFailTitle = new System.Windows.Forms.Label();
            this.lblPickFailValue = new System.Windows.Forms.Label();
            this.lblPlaceFailTitle = new System.Windows.Forms.Label();
            this.lblPlaceFailValue = new System.Windows.Forms.Label();
            this.lblCollet1UseTitle = new System.Windows.Forms.Label();
            this.lblCollet1UseValue = new System.Windows.Forms.Label();
            this.lblCollet2UseTitle = new System.Windows.Forms.Label();
            this.lblCollet2UseValue = new System.Windows.Forms.Label();
            this.lblCollet3UseTitle = new System.Windows.Forms.Label();
            this.lblCollet3UseValue = new System.Windows.Forms.Label();
            this.lblCollet4UseTitle = new System.Windows.Forms.Label();
            this.lblCollet4UseValue = new System.Windows.Forms.Label();
            this.btnCountClear = new System.Windows.Forms.Button();
            this.grpAxis = new System.Windows.Forms.GroupBox();
            this.axisGrid = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.grpInfo = new System.Windows.Forms.GroupBox();
            this.infoLayout = new System.Windows.Forms.TableLayoutPanel();
            this.headAxisTPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeadAxisTTitle = new System.Windows.Forms.Label();
            this.lblHeadAxisTValue = new System.Windows.Forms.Label();
            this.headProcessPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeadProcessTitle = new System.Windows.Forms.Label();
            this.lblHeadProcessValue = new System.Windows.Forms.Label();
            this.processFlowPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblFlowAvoid = new System.Windows.Forms.Label();
            this.lblFlowArrow1 = new System.Windows.Forms.Label();
            this.lblFlowPickup = new System.Windows.Forms.Label();
            this.lblFlowArrow2 = new System.Windows.Forms.Label();
            this.lblFlowBottom = new System.Windows.Forms.Label();
            this.lblFlowArrow3 = new System.Windows.Forms.Label();
            this.lblFlowSide = new System.Windows.Forms.Label();
            this.lblFlowArrow4 = new System.Windows.Forms.Label();
            this.lblFlowPlace = new System.Windows.Forms.Label();
            this.processDetailPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblProcessDetailTitle = new System.Windows.Forms.Label();
            this.lblProcessDetailValue = new System.Windows.Forms.Label();
            this.headVacuum1Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadVacuum1 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadVacuum1 = new System.Windows.Forms.Label();
            this.headVacuum2Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadVacuum2 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadVacuum2 = new System.Windows.Forms.Label();
            this.headVacuum3Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadVacuum3 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadVacuum3 = new System.Windows.Forms.Label();
            this.headVacuum4Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadVacuum4 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadVacuum4 = new System.Windows.Forms.Label();
            this.headBlow1Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadBlow1 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadBlow1 = new System.Windows.Forms.Label();
            this.headBlow2Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadBlow2 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadBlow2 = new System.Windows.Forms.Label();
            this.headBlow3Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadBlow3 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadBlow3 = new System.Windows.Forms.Label();
            this.headBlow4Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadBlow4 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadBlow4 = new System.Windows.Forms.Label();
            this.headFlow1Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadFlow1 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadFlow1 = new System.Windows.Forms.Label();
            this.headFlow2Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadFlow2 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadFlow2 = new System.Windows.Forms.Label();
            this.headFlow3Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadFlow3 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadFlow3 = new System.Windows.Forms.Label();
            this.headFlow4Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotHeadFlow4 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblHeadFlow4 = new System.Windows.Forms.Label();
            this.actionPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnInput = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnInspect = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnBottom = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSide = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnOutput = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.actionSpacer = new System.Windows.Forms.Panel();
            this.btnStop = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.topLayout.SuspendLayout();
            this.grpState.SuspendLayout();
            this.stateLayout.SuspendLayout();
            this.grpCounters.SuspendLayout();
            this.counterLayout.SuspendLayout();
            this.grpAxis.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axisGrid)).BeginInit();
            this.grpInfo.SuspendLayout();
            this.infoLayout.SuspendLayout();
            this.headAxisTPanel.SuspendLayout();
            this.headProcessPanel.SuspendLayout();
            this.processFlowPanel.SuspendLayout();
            this.processDetailPanel.SuspendLayout();
            this.headVacuum1Panel.SuspendLayout();
            this.headVacuum2Panel.SuspendLayout();
            this.headVacuum3Panel.SuspendLayout();
            this.headVacuum4Panel.SuspendLayout();
            this.headBlow1Panel.SuspendLayout();
            this.headBlow2Panel.SuspendLayout();
            this.headBlow3Panel.SuspendLayout();
            this.headBlow4Panel.SuspendLayout();
            this.headFlow1Panel.SuspendLayout();
            this.headFlow2Panel.SuspendLayout();
            this.headFlow3Panel.SuspendLayout();
            this.headFlow4Panel.SuspendLayout();
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
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 88F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 800);
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
            this.lblHeader.Tag = "i18n:wi.rearHead";
            this.lblHeader.Text = "REAR PICKER";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // contentLayout
            //
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 54F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 46F));
            this.contentLayout.Controls.Add(this.topLayout, 0, 0);
            this.contentLayout.Controls.Add(this.grpAxis, 1, 0);
            this.contentLayout.Controls.Add(this.grpInfo, 0, 1);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(3, 33);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(8);
            this.contentLayout.RowCount = 2;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 335F));
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1672, 676);
            this.contentLayout.TabIndex = 1;
            //
            // topLayout
            //
            this.topLayout.ColumnCount = 2;
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 48F));
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 52F));
            this.topLayout.Controls.Add(this.grpState, 0, 0);
            this.topLayout.Controls.Add(this.grpCounters, 1, 0);
            this.topLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topLayout.Location = new System.Drawing.Point(11, 11);
            this.topLayout.Name = "topLayout";
            this.topLayout.RowCount = 1;
            this.topLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topLayout.Size = new System.Drawing.Size(888, 329);
            this.topLayout.TabIndex = 0;
            //
            // grpState
            //
            this.grpState.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpState.Controls.Add(this.stateLayout);
            this.grpState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpState.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpState.Location = new System.Drawing.Point(3, 3);
            this.grpState.Name = "grpState";
            this.grpState.Size = new System.Drawing.Size(420, 323);
            this.grpState.TabIndex = 0;
            this.grpState.TabStop = false;
            this.grpState.Text = "WORK INFO";
            //
            // stateLayout
            //
            this.stateLayout.ColumnCount = 2;
            this.stateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 48F));
            this.stateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 52F));
            this.stateLayout.Controls.Add(this.lblHeadZoneTitle, 0, 0);
            this.stateLayout.Controls.Add(this.lblHeadZoneValue, 1, 0);
            this.stateLayout.Controls.Add(this.lblHead1Title, 0, 1);
            this.stateLayout.Controls.Add(this.lblHead1Value, 1, 1);
            this.stateLayout.Controls.Add(this.lblHead2Title, 0, 2);
            this.stateLayout.Controls.Add(this.lblHead2Value, 1, 2);
            this.stateLayout.Controls.Add(this.lblHead3Title, 0, 3);
            this.stateLayout.Controls.Add(this.lblHead3Value, 1, 3);
            this.stateLayout.Controls.Add(this.lblHead4Title, 0, 4);
            this.stateLayout.Controls.Add(this.lblHead4Value, 1, 4);
            this.stateLayout.Controls.Add(this.btnInitAll, 0, 5);
            this.stateLayout.Controls.Add(this.lblColletChangeTitle, 0, 6);
            this.stateLayout.Controls.Add(this.lblColletChangeValue, 1, 6);
            this.stateLayout.Controls.Add(this.lblAutoPosTitle, 0, 7);
            this.stateLayout.Controls.Add(this.lblAutoPosValue, 1, 7);
            this.stateLayout.Controls.Add(this.lblColletCleaningTitle, 0, 8);
            this.stateLayout.Controls.Add(this.lblColletCleaningValue, 1, 8);
            this.stateLayout.Controls.Add(this.lblColletCheckTitle, 0, 9);
            this.stateLayout.Controls.Add(this.lblColletCheckValue, 1, 9);
            this.stateLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stateLayout.Location = new System.Drawing.Point(3, 23);
            this.stateLayout.Name = "stateLayout";
            this.stateLayout.Padding = new System.Windows.Forms.Padding(2, 8, 2, 2);
            this.stateLayout.RowCount = 10;
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.615385F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.615385F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.615385F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.615385F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 13.46154F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.615385F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.615385F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.615385F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.615385F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.615385F));
            this.stateLayout.Size = new System.Drawing.Size(414, 297);
            this.stateLayout.TabIndex = 0;
            //
            // lblHeadZoneTitle
            //
            this.lblHeadZoneTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadZoneTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadZoneTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadZoneTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadZoneTitle.Location = new System.Drawing.Point(5, 8);
            this.lblHeadZoneTitle.Name = "lblHeadZoneTitle";
            this.lblHeadZoneTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadZoneTitle.Size = new System.Drawing.Size(190, 27);
            this.lblHeadZoneTitle.TabIndex = 0;
            this.lblHeadZoneTitle.Text = "HEAD ZONE";
            this.lblHeadZoneTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblHeadZoneValue
            //
            this.lblHeadZoneValue.BackColor = System.Drawing.Color.White;
            this.lblHeadZoneValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadZoneValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadZoneValue.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeadZoneValue.Location = new System.Drawing.Point(201, 8);
            this.lblHeadZoneValue.Name = "lblHeadZoneValue";
            this.lblHeadZoneValue.Size = new System.Drawing.Size(208, 27);
            this.lblHeadZoneValue.TabIndex = 1;
            this.lblHeadZoneValue.Text = "-";
            this.lblHeadZoneValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblHead1Title
            //
            this.lblHead1Title.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHead1Title.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead1Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead1Title.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHead1Title.Location = new System.Drawing.Point(5, 8);
            this.lblHead1Title.Name = "lblHead1Title";
            this.lblHead1Title.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHead1Title.Size = new System.Drawing.Size(190, 27);
            this.lblHead1Title.TabIndex = 0;
            this.lblHead1Title.Text = "HEAD #1";
            this.lblHead1Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblHead1Value
            //
            this.lblHead1Value.BackColor = System.Drawing.Color.White;
            this.lblHead1Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead1Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead1Value.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHead1Value.Location = new System.Drawing.Point(201, 8);
            this.lblHead1Value.Name = "lblHead1Value";
            this.lblHead1Value.Size = new System.Drawing.Size(208, 27);
            this.lblHead1Value.TabIndex = 1;
            this.lblHead1Value.Text = "-";
            this.lblHead1Value.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblHead1Value.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblHead1Value.Click += new System.EventHandler(this.lblHead1Value_Click);
            //
            // lblHead2Title
            //
            this.lblHead2Title.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHead2Title.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead2Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead2Title.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHead2Title.Location = new System.Drawing.Point(5, 35);
            this.lblHead2Title.Name = "lblHead2Title";
            this.lblHead2Title.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHead2Title.Size = new System.Drawing.Size(190, 27);
            this.lblHead2Title.TabIndex = 2;
            this.lblHead2Title.Text = "HEAD #2";
            this.lblHead2Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblHead2Value
            //
            this.lblHead2Value.BackColor = System.Drawing.Color.White;
            this.lblHead2Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead2Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead2Value.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHead2Value.Location = new System.Drawing.Point(201, 35);
            this.lblHead2Value.Name = "lblHead2Value";
            this.lblHead2Value.Size = new System.Drawing.Size(208, 27);
            this.lblHead2Value.TabIndex = 3;
            this.lblHead2Value.Text = "-";
            this.lblHead2Value.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblHead2Value.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblHead2Value.Click += new System.EventHandler(this.lblHead2Value_Click);
            //
            // lblHead3Title
            //
            this.lblHead3Title.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHead3Title.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead3Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead3Title.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHead3Title.Location = new System.Drawing.Point(5, 62);
            this.lblHead3Title.Name = "lblHead3Title";
            this.lblHead3Title.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHead3Title.Size = new System.Drawing.Size(190, 27);
            this.lblHead3Title.TabIndex = 4;
            this.lblHead3Title.Text = "HEAD #3";
            this.lblHead3Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblHead3Value
            //
            this.lblHead3Value.BackColor = System.Drawing.Color.White;
            this.lblHead3Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead3Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead3Value.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHead3Value.Location = new System.Drawing.Point(201, 62);
            this.lblHead3Value.Name = "lblHead3Value";
            this.lblHead3Value.Size = new System.Drawing.Size(208, 27);
            this.lblHead3Value.TabIndex = 5;
            this.lblHead3Value.Text = "-";
            this.lblHead3Value.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblHead3Value.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblHead3Value.Click += new System.EventHandler(this.lblHead3Value_Click);
            //
            // lblHead4Title
            //
            this.lblHead4Title.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHead4Title.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead4Title.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead4Title.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHead4Title.Location = new System.Drawing.Point(5, 89);
            this.lblHead4Title.Name = "lblHead4Title";
            this.lblHead4Title.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHead4Title.Size = new System.Drawing.Size(190, 27);
            this.lblHead4Title.TabIndex = 6;
            this.lblHead4Title.Text = "HEAD #4";
            this.lblHead4Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblHead4Value
            //
            this.lblHead4Value.BackColor = System.Drawing.Color.White;
            this.lblHead4Value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHead4Value.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHead4Value.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHead4Value.Location = new System.Drawing.Point(201, 89);
            this.lblHead4Value.Name = "lblHead4Value";
            this.lblHead4Value.Size = new System.Drawing.Size(208, 27);
            this.lblHead4Value.TabIndex = 7;
            this.lblHead4Value.Text = "-";
            this.lblHead4Value.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblHead4Value.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblHead4Value.Click += new System.EventHandler(this.lblHead4Value_Click);
            //
            // lblColletChangeTitle
            //
            this.lblColletChangeTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblColletChangeTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblColletChangeTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletChangeTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblColletChangeTitle.Location = new System.Drawing.Point(5, 154);
            this.lblColletChangeTitle.Name = "lblColletChangeTitle";
            this.lblColletChangeTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblColletChangeTitle.Size = new System.Drawing.Size(190, 27);
            this.lblColletChangeTitle.TabIndex = 8;
            this.lblColletChangeTitle.Text = "COLLET CHANGE";
            this.lblColletChangeTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblColletChangeValue
            //
            this.lblColletChangeValue.BackColor = System.Drawing.Color.White;
            this.lblColletChangeValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblColletChangeValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletChangeValue.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblColletChangeValue.Location = new System.Drawing.Point(201, 154);
            this.lblColletChangeValue.Name = "lblColletChangeValue";
            this.lblColletChangeValue.Size = new System.Drawing.Size(208, 27);
            this.lblColletChangeValue.TabIndex = 9;
            this.lblColletChangeValue.Text = "-";
            this.lblColletChangeValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblAutoPosTitle
            //
            this.lblAutoPosTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblAutoPosTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAutoPosTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAutoPosTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblAutoPosTitle.Location = new System.Drawing.Point(5, 181);
            this.lblAutoPosTitle.Name = "lblAutoPosTitle";
            this.lblAutoPosTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblAutoPosTitle.Size = new System.Drawing.Size(190, 27);
            this.lblAutoPosTitle.TabIndex = 10;
            this.lblAutoPosTitle.Text = "AUTO POSITION";
            this.lblAutoPosTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblAutoPosValue
            //
            this.lblAutoPosValue.BackColor = System.Drawing.Color.White;
            this.lblAutoPosValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAutoPosValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAutoPosValue.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblAutoPosValue.Location = new System.Drawing.Point(201, 181);
            this.lblAutoPosValue.Name = "lblAutoPosValue";
            this.lblAutoPosValue.Size = new System.Drawing.Size(208, 27);
            this.lblAutoPosValue.TabIndex = 11;
            this.lblAutoPosValue.Text = "-";
            this.lblAutoPosValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblColletCleaningTitle
            //
            this.lblColletCleaningTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblColletCleaningTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblColletCleaningTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletCleaningTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblColletCleaningTitle.Location = new System.Drawing.Point(5, 208);
            this.lblColletCleaningTitle.Name = "lblColletCleaningTitle";
            this.lblColletCleaningTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblColletCleaningTitle.Size = new System.Drawing.Size(190, 27);
            this.lblColletCleaningTitle.TabIndex = 12;
            this.lblColletCleaningTitle.Text = "COLLET CLEANING";
            this.lblColletCleaningTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblColletCleaningValue
            //
            this.lblColletCleaningValue.BackColor = System.Drawing.Color.White;
            this.lblColletCleaningValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblColletCleaningValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletCleaningValue.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblColletCleaningValue.Location = new System.Drawing.Point(201, 208);
            this.lblColletCleaningValue.Name = "lblColletCleaningValue";
            this.lblColletCleaningValue.Size = new System.Drawing.Size(208, 27);
            this.lblColletCleaningValue.TabIndex = 13;
            this.lblColletCleaningValue.Text = "-";
            this.lblColletCleaningValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblColletCheckTitle
            //
            this.lblColletCheckTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblColletCheckTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblColletCheckTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletCheckTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblColletCheckTitle.Location = new System.Drawing.Point(5, 235);
            this.lblColletCheckTitle.Name = "lblColletCheckTitle";
            this.lblColletCheckTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblColletCheckTitle.Size = new System.Drawing.Size(190, 27);
            this.lblColletCheckTitle.TabIndex = 14;
            this.lblColletCheckTitle.Text = "COLLET CHECK";
            this.lblColletCheckTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblColletCheckValue
            //
            this.lblColletCheckValue.BackColor = System.Drawing.Color.White;
            this.lblColletCheckValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblColletCheckValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblColletCheckValue.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblColletCheckValue.Location = new System.Drawing.Point(201, 235);
            this.lblColletCheckValue.Name = "lblColletCheckValue";
            this.lblColletCheckValue.Size = new System.Drawing.Size(208, 27);
            this.lblColletCheckValue.TabIndex = 15;
            this.lblColletCheckValue.Text = "-";
            this.lblColletCheckValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // btnInitAll
            //
            this.stateLayout.SetColumnSpan(this.btnInitAll, 2);
            this.btnInitAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInitAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInitAll.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnInitAll.Location = new System.Drawing.Point(5, 119);
            this.btnInitAll.Name = "btnInitAll";
            this.btnInitAll.Size = new System.Drawing.Size(404, 32);
            this.btnInitAll.TabIndex = 16;
            this.btnInitAll.Text = "INIT ALL";
            //
            // grpCounters
            //
            this.grpCounters.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpCounters.Controls.Add(this.counterLayout);
            this.grpCounters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCounters.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpCounters.Location = new System.Drawing.Point(429, 3);
            this.grpCounters.Name = "grpCounters";
            this.grpCounters.Size = new System.Drawing.Size(456, 323);
            this.grpCounters.TabIndex = 1;
            this.grpCounters.TabStop = false;
            this.grpCounters.Text = "COUNTER";
            //
            // counterLayout
            //
            this.counterLayout.ColumnCount = 2;
            this.counterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 48F));
            this.counterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 52F));
            this.counterLayout.Controls.Add(this.lblPickFailTitle, 0, 0);
            this.counterLayout.Controls.Add(this.lblPickFailValue, 1, 0);
            this.counterLayout.Controls.Add(this.lblPlaceFailTitle, 0, 1);
            this.counterLayout.Controls.Add(this.lblPlaceFailValue, 1, 1);
            this.counterLayout.Controls.Add(this.lblCollet1UseTitle, 0, 2);
            this.counterLayout.Controls.Add(this.lblCollet1UseValue, 1, 2);
            this.counterLayout.Controls.Add(this.lblCollet2UseTitle, 0, 3);
            this.counterLayout.Controls.Add(this.lblCollet2UseValue, 1, 3);
            this.counterLayout.Controls.Add(this.lblCollet3UseTitle, 0, 4);
            this.counterLayout.Controls.Add(this.lblCollet3UseValue, 1, 4);
            this.counterLayout.Controls.Add(this.lblCollet4UseTitle, 0, 5);
            this.counterLayout.Controls.Add(this.lblCollet4UseValue, 1, 5);
            this.counterLayout.Controls.Add(this.btnCountClear, 0, 6);
            this.counterLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.counterLayout.Location = new System.Drawing.Point(3, 23);
            this.counterLayout.Name = "counterLayout";
            this.counterLayout.Padding = new System.Windows.Forms.Padding(2, 8, 2, 2);
            this.counterLayout.RowCount = 8;
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.counterLayout.Size = new System.Drawing.Size(450, 297);
            this.counterLayout.TabIndex = 0;
            //
            // lblPickFailTitle
            //
            this.lblPickFailTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblPickFailTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPickFailTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickFailTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblPickFailTitle.Location = new System.Drawing.Point(5, 8);
            this.lblPickFailTitle.Name = "lblPickFailTitle";
            this.lblPickFailTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblPickFailTitle.Size = new System.Drawing.Size(208, 30);
            this.lblPickFailTitle.TabIndex = 0;
            this.lblPickFailTitle.Text = "PICK FAIL";
            this.lblPickFailTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblPickFailValue
            //
            this.lblPickFailValue.BackColor = System.Drawing.Color.White;
            this.lblPickFailValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPickFailValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickFailValue.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblPickFailValue.Location = new System.Drawing.Point(219, 8);
            this.lblPickFailValue.Name = "lblPickFailValue";
            this.lblPickFailValue.Size = new System.Drawing.Size(226, 30);
            this.lblPickFailValue.TabIndex = 1;
            this.lblPickFailValue.Text = "0 ea";
            this.lblPickFailValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblPlaceFailTitle
            //
            this.lblPlaceFailTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblPlaceFailTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPlaceFailTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlaceFailTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblPlaceFailTitle.Location = new System.Drawing.Point(5, 38);
            this.lblPlaceFailTitle.Name = "lblPlaceFailTitle";
            this.lblPlaceFailTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblPlaceFailTitle.Size = new System.Drawing.Size(208, 30);
            this.lblPlaceFailTitle.TabIndex = 2;
            this.lblPlaceFailTitle.Text = "PLACE FAIL";
            this.lblPlaceFailTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblPlaceFailValue
            //
            this.lblPlaceFailValue.BackColor = System.Drawing.Color.White;
            this.lblPlaceFailValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPlaceFailValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlaceFailValue.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblPlaceFailValue.Location = new System.Drawing.Point(219, 38);
            this.lblPlaceFailValue.Name = "lblPlaceFailValue";
            this.lblPlaceFailValue.Size = new System.Drawing.Size(226, 30);
            this.lblPlaceFailValue.TabIndex = 3;
            this.lblPlaceFailValue.Text = "0 ea";
            this.lblPlaceFailValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblCollet1UseTitle
            //
            this.lblCollet1UseTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblCollet1UseTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet1UseTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet1UseTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblCollet1UseTitle.Location = new System.Drawing.Point(5, 68);
            this.lblCollet1UseTitle.Name = "lblCollet1UseTitle";
            this.lblCollet1UseTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblCollet1UseTitle.Size = new System.Drawing.Size(208, 30);
            this.lblCollet1UseTitle.TabIndex = 4;
            this.lblCollet1UseTitle.Text = "#1 COLLET USE";
            this.lblCollet1UseTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblCollet1UseValue
            //
            this.lblCollet1UseValue.BackColor = System.Drawing.Color.White;
            this.lblCollet1UseValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet1UseValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet1UseValue.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblCollet1UseValue.Location = new System.Drawing.Point(219, 68);
            this.lblCollet1UseValue.Name = "lblCollet1UseValue";
            this.lblCollet1UseValue.Size = new System.Drawing.Size(226, 30);
            this.lblCollet1UseValue.TabIndex = 5;
            this.lblCollet1UseValue.Text = "0 ea";
            this.lblCollet1UseValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblCollet2UseTitle
            //
            this.lblCollet2UseTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblCollet2UseTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet2UseTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet2UseTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblCollet2UseTitle.Location = new System.Drawing.Point(5, 98);
            this.lblCollet2UseTitle.Name = "lblCollet2UseTitle";
            this.lblCollet2UseTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblCollet2UseTitle.Size = new System.Drawing.Size(208, 30);
            this.lblCollet2UseTitle.TabIndex = 6;
            this.lblCollet2UseTitle.Text = "#2 COLLET USE";
            this.lblCollet2UseTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblCollet2UseValue
            //
            this.lblCollet2UseValue.BackColor = System.Drawing.Color.White;
            this.lblCollet2UseValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet2UseValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet2UseValue.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblCollet2UseValue.Location = new System.Drawing.Point(219, 98);
            this.lblCollet2UseValue.Name = "lblCollet2UseValue";
            this.lblCollet2UseValue.Size = new System.Drawing.Size(226, 30);
            this.lblCollet2UseValue.TabIndex = 7;
            this.lblCollet2UseValue.Text = "0 ea";
            this.lblCollet2UseValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblCollet3UseTitle
            //
            this.lblCollet3UseTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblCollet3UseTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet3UseTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet3UseTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblCollet3UseTitle.Location = new System.Drawing.Point(5, 128);
            this.lblCollet3UseTitle.Name = "lblCollet3UseTitle";
            this.lblCollet3UseTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblCollet3UseTitle.Size = new System.Drawing.Size(208, 30);
            this.lblCollet3UseTitle.TabIndex = 8;
            this.lblCollet3UseTitle.Text = "#3 COLLET USE";
            this.lblCollet3UseTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblCollet3UseValue
            //
            this.lblCollet3UseValue.BackColor = System.Drawing.Color.White;
            this.lblCollet3UseValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet3UseValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet3UseValue.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblCollet3UseValue.Location = new System.Drawing.Point(219, 128);
            this.lblCollet3UseValue.Name = "lblCollet3UseValue";
            this.lblCollet3UseValue.Size = new System.Drawing.Size(226, 30);
            this.lblCollet3UseValue.TabIndex = 9;
            this.lblCollet3UseValue.Text = "0 ea";
            this.lblCollet3UseValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblCollet4UseTitle
            //
            this.lblCollet4UseTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblCollet4UseTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet4UseTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet4UseTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblCollet4UseTitle.Location = new System.Drawing.Point(5, 158);
            this.lblCollet4UseTitle.Name = "lblCollet4UseTitle";
            this.lblCollet4UseTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblCollet4UseTitle.Size = new System.Drawing.Size(208, 30);
            this.lblCollet4UseTitle.TabIndex = 10;
            this.lblCollet4UseTitle.Text = "#4 COLLET USE";
            this.lblCollet4UseTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblCollet4UseValue
            //
            this.lblCollet4UseValue.BackColor = System.Drawing.Color.White;
            this.lblCollet4UseValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCollet4UseValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet4UseValue.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblCollet4UseValue.Location = new System.Drawing.Point(219, 158);
            this.lblCollet4UseValue.Name = "lblCollet4UseValue";
            this.lblCollet4UseValue.Size = new System.Drawing.Size(226, 30);
            this.lblCollet4UseValue.TabIndex = 11;
            this.lblCollet4UseValue.Text = "0 ea";
            this.lblCollet4UseValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // btnCountClear
            //
            this.counterLayout.SetColumnSpan(this.btnCountClear, 2);
            this.btnCountClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCountClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCountClear.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnCountClear.Location = new System.Drawing.Point(5, 191);
            this.btnCountClear.Name = "btnCountClear";
            this.btnCountClear.Size = new System.Drawing.Size(440, 24);
            this.btnCountClear.TabIndex = 12;
            this.btnCountClear.Text = "COUNT CLEAR";
            //
            // grpAxis
            //
            this.grpAxis.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpAxis.Controls.Add(this.axisGrid);
            this.grpAxis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAxis.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpAxis.Location = new System.Drawing.Point(905, 11);
            this.grpAxis.Name = "grpAxis";
            this.contentLayout.SetRowSpan(this.grpAxis, 2);
            this.grpAxis.Size = new System.Drawing.Size(756, 654);
            this.grpAxis.TabIndex = 1;
            this.grpAxis.TabStop = false;
            this.grpAxis.Text = "PICKER AXIS MONITOR";
            //
            // axisGrid
            //
            this.axisGrid.AllowUserToAddRows = false;
            this.axisGrid.AllowUserToDeleteRows = false;
            this.axisGrid.AllowUserToResizeRows = false;
            this.axisGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.axisGrid.BackgroundColor = System.Drawing.Color.White;
            this.axisGrid.ColumnHeadersHeight = 28;
            this.axisGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.axisGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn4,
            this.dataGridViewTextBoxColumn5,
            this.dataGridViewTextBoxColumn6,
            this.dataGridViewTextBoxColumn7});
            this.axisGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axisGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.axisGrid.EnableHeadersVisualStyles = false;
            this.axisGrid.Location = new System.Drawing.Point(3, 23);
            this.axisGrid.Name = "axisGrid";
            this.axisGrid.RowHeadersVisible = false;
            this.axisGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.axisGrid.Size = new System.Drawing.Size(750, 628);
            this.axisGrid.TabIndex = 0;
            //
            // dataGridViewTextBoxColumn1
            //
            this.dataGridViewTextBoxColumn1.HeaderText = "Axis";
            this.dataGridViewTextBoxColumn1.Name = "colAxis";
            this.dataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // dataGridViewTextBoxColumn2
            //
            this.dataGridViewTextBoxColumn2.HeaderText = "Current";
            this.dataGridViewTextBoxColumn2.Name = "colCurrent";
            this.dataGridViewTextBoxColumn2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // dataGridViewTextBoxColumn3
            //
            this.dataGridViewTextBoxColumn3.HeaderText = "Command";
            this.dataGridViewTextBoxColumn3.Name = "colCommand";
            this.dataGridViewTextBoxColumn3.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dataGridViewTextBoxColumn3.Visible = false;
            //
            // dataGridViewTextBoxColumn4
            //
            this.dataGridViewTextBoxColumn4.HeaderText = "Servo";
            this.dataGridViewTextBoxColumn4.Name = "colServo";
            this.dataGridViewTextBoxColumn4.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dataGridViewTextBoxColumn4.Visible = false;
            //
            // dataGridViewTextBoxColumn5
            //
            this.dataGridViewTextBoxColumn5.HeaderText = "Home";
            this.dataGridViewTextBoxColumn5.Name = "colHome";
            this.dataGridViewTextBoxColumn5.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dataGridViewTextBoxColumn5.Visible = false;
            //
            // dataGridViewTextBoxColumn6
            //
            this.dataGridViewTextBoxColumn6.HeaderText = "Alarm";
            this.dataGridViewTextBoxColumn6.Name = "colAlarm";
            this.dataGridViewTextBoxColumn6.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dataGridViewTextBoxColumn6.Visible = false;
            //
            // dataGridViewTextBoxColumn7
            //
            this.dataGridViewTextBoxColumn7.HeaderText = "Moving";
            this.dataGridViewTextBoxColumn7.Name = "colMoving";
            this.dataGridViewTextBoxColumn7.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dataGridViewTextBoxColumn7.Visible = false;
            //
            // grpInfo
            //
            this.grpInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpInfo.Controls.Add(this.infoLayout);
            this.grpInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInfo.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpInfo.Location = new System.Drawing.Point(11, 346);
            this.grpInfo.Name = "grpInfo";
            this.grpInfo.Size = new System.Drawing.Size(888, 319);
            this.grpInfo.TabIndex = 2;
            this.grpInfo.TabStop = false;
            this.grpInfo.Text = "INFO";
            //
            // infoLayout
            //
            this.infoLayout.ColumnCount = 4;
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.infoLayout.Controls.Add(this.headVacuum1Panel, 0, 0);
            this.infoLayout.Controls.Add(this.headVacuum2Panel, 1, 0);
            this.infoLayout.Controls.Add(this.headVacuum3Panel, 2, 0);
            this.infoLayout.Controls.Add(this.headVacuum4Panel, 3, 0);
            this.infoLayout.Controls.Add(this.headBlow1Panel, 0, 1);
            this.infoLayout.Controls.Add(this.headBlow2Panel, 1, 1);
            this.infoLayout.Controls.Add(this.headBlow3Panel, 2, 1);
            this.infoLayout.Controls.Add(this.headBlow4Panel, 3, 1);
            this.infoLayout.Controls.Add(this.headProcessPanel, 0, 2);
            this.infoLayout.Controls.Add(this.processFlowPanel, 0, 3);
            this.infoLayout.Controls.Add(this.processDetailPanel, 0, 4);
            this.infoLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoLayout.Location = new System.Drawing.Point(3, 23);
            this.infoLayout.Name = "infoLayout";
            this.infoLayout.Padding = new System.Windows.Forms.Padding(12, 18, 12, 12);
            this.infoLayout.RowCount = 6;
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.infoLayout.Size = new System.Drawing.Size(882, 293);
            this.infoLayout.TabIndex = 0;
            this.infoLayout.SetColumnSpan(this.headProcessPanel, 4);
            this.infoLayout.SetColumnSpan(this.processFlowPanel, 4);
            this.infoLayout.SetColumnSpan(this.processDetailPanel, 4);
            //
            // headAxisTPanel
            //
            this.headAxisTPanel.ColumnCount = 2;
            this.infoLayout.SetColumnSpan(this.headAxisTPanel, 2);
            this.headAxisTPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.headAxisTPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.headAxisTPanel.Controls.Add(this.lblHeadAxisTTitle, 0, 0);
            this.headAxisTPanel.Controls.Add(this.lblHeadAxisTValue, 1, 0);
            this.headAxisTPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headAxisTPanel.Location = new System.Drawing.Point(15, 21);
            this.headAxisTPanel.Name = "headAxisTPanel";
            this.headAxisTPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headAxisTPanel.Size = new System.Drawing.Size(422, 32);
            this.headAxisTPanel.TabIndex = 0;
            //
            // lblHeadAxisTTitle
            //
            this.lblHeadAxisTTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadAxisTTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadAxisTTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadAxisTTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadAxisTTitle.Location = new System.Drawing.Point(3, 0);
            this.lblHeadAxisTTitle.Name = "lblHeadAxisTTitle";
            this.lblHeadAxisTTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadAxisTTitle.Size = new System.Drawing.Size(226, 32);
            this.lblHeadAxisTTitle.TabIndex = 0;
            this.lblHeadAxisTTitle.Text = "PICKER T1";
            this.lblHeadAxisTTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblHeadAxisTValue
            //
            this.lblHeadAxisTValue.BackColor = System.Drawing.Color.White;
            this.lblHeadAxisTValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadAxisTValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadAxisTValue.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadAxisTValue.Location = new System.Drawing.Point(235, 0);
            this.lblHeadAxisTValue.Name = "lblHeadAxisTValue";
            this.lblHeadAxisTValue.Size = new System.Drawing.Size(184, 32);
            this.lblHeadAxisTValue.TabIndex = 1;
            this.lblHeadAxisTValue.Text = "0 deg";
            this.lblHeadAxisTValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // headProcessPanel
            //
            this.headProcessPanel.ColumnCount = 2;
            this.headProcessPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.headProcessPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headProcessPanel.Controls.Add(this.lblHeadProcessTitle, 0, 0);
            this.headProcessPanel.Controls.Add(this.lblHeadProcessValue, 1, 0);
            this.headProcessPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headProcessPanel.Location = new System.Drawing.Point(15, 97);
            this.headProcessPanel.Name = "headProcessPanel";
            this.headProcessPanel.RowCount = 1;
            this.headProcessPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headProcessPanel.Size = new System.Drawing.Size(852, 32);
            this.headProcessPanel.TabIndex = 9;
            //
            // lblHeadProcessTitle
            //
            this.lblHeadProcessTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadProcessTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadProcessTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadProcessTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadProcessTitle.Location = new System.Drawing.Point(3, 0);
            this.lblHeadProcessTitle.Name = "lblHeadProcessTitle";
            this.lblHeadProcessTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadProcessTitle.Size = new System.Drawing.Size(174, 32);
            this.lblHeadProcessTitle.TabIndex = 0;
            this.lblHeadProcessTitle.Text = "CURRENT STEP";
            this.lblHeadProcessTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblHeadProcessValue
            //
            this.lblHeadProcessValue.BackColor = System.Drawing.Color.White;
            this.lblHeadProcessValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadProcessValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadProcessValue.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeadProcessValue.Location = new System.Drawing.Point(183, 0);
            this.lblHeadProcessValue.Name = "lblHeadProcessValue";
            this.lblHeadProcessValue.Size = new System.Drawing.Size(666, 32);
            this.lblHeadProcessValue.TabIndex = 1;
            this.lblHeadProcessValue.Text = "-";
            this.lblHeadProcessValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // processFlowPanel
            //
            this.processFlowPanel.ColumnCount = 9;
            this.processFlowPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.processFlowPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.processFlowPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.processFlowPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.processFlowPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.processFlowPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.processFlowPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.processFlowPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.processFlowPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.processFlowPanel.Controls.Add(this.lblFlowAvoid, 0, 0);
            this.processFlowPanel.Controls.Add(this.lblFlowArrow1, 1, 0);
            this.processFlowPanel.Controls.Add(this.lblFlowPickup, 2, 0);
            this.processFlowPanel.Controls.Add(this.lblFlowArrow2, 3, 0);
            this.processFlowPanel.Controls.Add(this.lblFlowBottom, 4, 0);
            this.processFlowPanel.Controls.Add(this.lblFlowArrow3, 5, 0);
            this.processFlowPanel.Controls.Add(this.lblFlowSide, 6, 0);
            this.processFlowPanel.Controls.Add(this.lblFlowArrow4, 7, 0);
            this.processFlowPanel.Controls.Add(this.lblFlowPlace, 8, 0);
            this.processFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.processFlowPanel.Location = new System.Drawing.Point(15, 135);
            this.processFlowPanel.Name = "processFlowPanel";
            this.processFlowPanel.RowCount = 1;
            this.processFlowPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.processFlowPanel.Size = new System.Drawing.Size(852, 36);
            this.processFlowPanel.TabIndex = 10;
            //
            // lblFlowAvoid
            //
            this.lblFlowAvoid.BackColor = System.Drawing.Color.DimGray;
            this.lblFlowAvoid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblFlowAvoid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFlowAvoid.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblFlowAvoid.ForeColor = System.Drawing.Color.White;
            this.lblFlowAvoid.Location = new System.Drawing.Point(3, 0);
            this.lblFlowAvoid.Name = "lblFlowAvoid";
            this.lblFlowAvoid.Size = new System.Drawing.Size(146, 36);
            this.lblFlowAvoid.TabIndex = 0;
            this.lblFlowAvoid.Text = "AVOID";
            this.lblFlowAvoid.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblFlowArrow1
            //
            this.lblFlowArrow1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFlowArrow1.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblFlowArrow1.Location = new System.Drawing.Point(155, 0);
            this.lblFlowArrow1.Name = "lblFlowArrow1";
            this.lblFlowArrow1.Size = new System.Drawing.Size(16, 36);
            this.lblFlowArrow1.TabIndex = 1;
            this.lblFlowArrow1.Text = ">";
            this.lblFlowArrow1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblFlowPickup
            //
            this.lblFlowPickup.BackColor = System.Drawing.Color.DimGray;
            this.lblFlowPickup.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblFlowPickup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFlowPickup.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblFlowPickup.ForeColor = System.Drawing.Color.White;
            this.lblFlowPickup.Location = new System.Drawing.Point(177, 0);
            this.lblFlowPickup.Name = "lblFlowPickup";
            this.lblFlowPickup.Size = new System.Drawing.Size(146, 36);
            this.lblFlowPickup.TabIndex = 2;
            this.lblFlowPickup.Text = "PICKUP";
            this.lblFlowPickup.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblFlowArrow2
            //
            this.lblFlowArrow2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFlowArrow2.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblFlowArrow2.Location = new System.Drawing.Point(329, 0);
            this.lblFlowArrow2.Name = "lblFlowArrow2";
            this.lblFlowArrow2.Size = new System.Drawing.Size(16, 36);
            this.lblFlowArrow2.TabIndex = 3;
            this.lblFlowArrow2.Text = ">";
            this.lblFlowArrow2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblFlowBottom
            //
            this.lblFlowBottom.BackColor = System.Drawing.Color.DimGray;
            this.lblFlowBottom.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblFlowBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFlowBottom.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblFlowBottom.ForeColor = System.Drawing.Color.White;
            this.lblFlowBottom.Location = new System.Drawing.Point(351, 0);
            this.lblFlowBottom.Name = "lblFlowBottom";
            this.lblFlowBottom.Size = new System.Drawing.Size(146, 36);
            this.lblFlowBottom.TabIndex = 4;
            this.lblFlowBottom.Text = "BOTTOM";
            this.lblFlowBottom.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblFlowArrow3
            //
            this.lblFlowArrow3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFlowArrow3.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblFlowArrow3.Location = new System.Drawing.Point(503, 0);
            this.lblFlowArrow3.Name = "lblFlowArrow3";
            this.lblFlowArrow3.Size = new System.Drawing.Size(16, 36);
            this.lblFlowArrow3.TabIndex = 5;
            this.lblFlowArrow3.Text = ">";
            this.lblFlowArrow3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblFlowSide
            //
            this.lblFlowSide.BackColor = System.Drawing.Color.DimGray;
            this.lblFlowSide.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblFlowSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFlowSide.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblFlowSide.ForeColor = System.Drawing.Color.White;
            this.lblFlowSide.Location = new System.Drawing.Point(525, 0);
            this.lblFlowSide.Name = "lblFlowSide";
            this.lblFlowSide.Size = new System.Drawing.Size(146, 36);
            this.lblFlowSide.TabIndex = 6;
            this.lblFlowSide.Text = "SIDE";
            this.lblFlowSide.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblFlowArrow4
            //
            this.lblFlowArrow4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFlowArrow4.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblFlowArrow4.Location = new System.Drawing.Point(677, 0);
            this.lblFlowArrow4.Name = "lblFlowArrow4";
            this.lblFlowArrow4.Size = new System.Drawing.Size(16, 36);
            this.lblFlowArrow4.TabIndex = 7;
            this.lblFlowArrow4.Text = ">";
            this.lblFlowArrow4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblFlowPlace
            //
            this.lblFlowPlace.BackColor = System.Drawing.Color.DimGray;
            this.lblFlowPlace.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblFlowPlace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFlowPlace.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblFlowPlace.ForeColor = System.Drawing.Color.White;
            this.lblFlowPlace.Location = new System.Drawing.Point(699, 0);
            this.lblFlowPlace.Name = "lblFlowPlace";
            this.lblFlowPlace.Size = new System.Drawing.Size(150, 36);
            this.lblFlowPlace.TabIndex = 8;
            this.lblFlowPlace.Text = "PLACE";
            this.lblFlowPlace.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // processDetailPanel
            //
            this.processDetailPanel.ColumnCount = 2;
            this.processDetailPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.processDetailPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.processDetailPanel.Controls.Add(this.lblProcessDetailTitle, 0, 0);
            this.processDetailPanel.Controls.Add(this.lblProcessDetailValue, 1, 0);
            this.processDetailPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.processDetailPanel.Location = new System.Drawing.Point(15, 177);
            this.processDetailPanel.Name = "processDetailPanel";
            this.processDetailPanel.RowCount = 1;
            this.processDetailPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.processDetailPanel.Size = new System.Drawing.Size(852, 32);
            this.processDetailPanel.TabIndex = 11;
            //
            // lblProcessDetailTitle
            //
            this.lblProcessDetailTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblProcessDetailTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblProcessDetailTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProcessDetailTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblProcessDetailTitle.Location = new System.Drawing.Point(3, 0);
            this.lblProcessDetailTitle.Name = "lblProcessDetailTitle";
            this.lblProcessDetailTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblProcessDetailTitle.Size = new System.Drawing.Size(174, 32);
            this.lblProcessDetailTitle.TabIndex = 0;
            this.lblProcessDetailTitle.Text = "PROCESS DETAIL";
            this.lblProcessDetailTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblProcessDetailValue
            //
            this.lblProcessDetailValue.BackColor = System.Drawing.Color.White;
            this.lblProcessDetailValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblProcessDetailValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProcessDetailValue.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblProcessDetailValue.Location = new System.Drawing.Point(183, 0);
            this.lblProcessDetailValue.Name = "lblProcessDetailValue";
            this.lblProcessDetailValue.Size = new System.Drawing.Size(666, 32);
            this.lblProcessDetailValue.TabIndex = 1;
            this.lblProcessDetailValue.Text = "-";
            this.lblProcessDetailValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // headVacuum1Panel
            //
            this.headVacuum1Panel.ColumnCount = 2;
            this.headVacuum1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.headVacuum1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headVacuum1Panel.Controls.Add(this.dotHeadVacuum1, 0, 0);
            this.headVacuum1Panel.Controls.Add(this.lblHeadVacuum1, 1, 0);
            this.headVacuum1Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headVacuum1Panel.Location = new System.Drawing.Point(15, 59);
            this.headVacuum1Panel.Name = "headVacuum1Panel";
            this.headVacuum1Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headVacuum1Panel.Size = new System.Drawing.Size(208, 32);
            this.headVacuum1Panel.TabIndex = 1;
            //
            // dotHeadVacuum1
            //
            this.dotHeadVacuum1.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadVacuum1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotHeadVacuum1.Location = new System.Drawing.Point(2, 8);
            this.dotHeadVacuum1.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.dotHeadVacuum1.Name = "dotHeadVacuum1";
            this.dotHeadVacuum1.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadVacuum1.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadVacuum1.Size = new System.Drawing.Size(18, 16);
            this.dotHeadVacuum1.TabIndex = 0;
            //
            // lblHeadVacuum1
            //
            this.lblHeadVacuum1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadVacuum1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadVacuum1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadVacuum1.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadVacuum1.Location = new System.Drawing.Point(25, 0);
            this.lblHeadVacuum1.Name = "lblHeadVacuum1";
            this.lblHeadVacuum1.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadVacuum1.Size = new System.Drawing.Size(180, 32);
            this.lblHeadVacuum1.TabIndex = 1;
            this.lblHeadVacuum1.Text = "HEAD VACUUM #1";
            this.lblHeadVacuum1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // headVacuum2Panel
            //
            this.headVacuum2Panel.ColumnCount = 2;
            this.headVacuum2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.headVacuum2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headVacuum2Panel.Controls.Add(this.dotHeadVacuum2, 0, 0);
            this.headVacuum2Panel.Controls.Add(this.lblHeadVacuum2, 1, 0);
            this.headVacuum2Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headVacuum2Panel.Location = new System.Drawing.Point(229, 59);
            this.headVacuum2Panel.Name = "headVacuum2Panel";
            this.headVacuum2Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headVacuum2Panel.Size = new System.Drawing.Size(208, 32);
            this.headVacuum2Panel.TabIndex = 2;
            //
            // dotHeadVacuum2
            //
            this.dotHeadVacuum2.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadVacuum2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotHeadVacuum2.Location = new System.Drawing.Point(2, 8);
            this.dotHeadVacuum2.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.dotHeadVacuum2.Name = "dotHeadVacuum2";
            this.dotHeadVacuum2.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadVacuum2.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadVacuum2.Size = new System.Drawing.Size(18, 16);
            this.dotHeadVacuum2.TabIndex = 0;
            //
            // lblHeadVacuum2
            //
            this.lblHeadVacuum2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadVacuum2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadVacuum2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadVacuum2.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadVacuum2.Location = new System.Drawing.Point(25, 0);
            this.lblHeadVacuum2.Name = "lblHeadVacuum2";
            this.lblHeadVacuum2.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadVacuum2.Size = new System.Drawing.Size(180, 32);
            this.lblHeadVacuum2.TabIndex = 1;
            this.lblHeadVacuum2.Text = "HEAD VACUUM #2";
            this.lblHeadVacuum2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // headVacuum3Panel
            //
            this.headVacuum3Panel.ColumnCount = 2;
            this.headVacuum3Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.headVacuum3Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headVacuum3Panel.Controls.Add(this.dotHeadVacuum3, 0, 0);
            this.headVacuum3Panel.Controls.Add(this.lblHeadVacuum3, 1, 0);
            this.headVacuum3Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headVacuum3Panel.Location = new System.Drawing.Point(443, 59);
            this.headVacuum3Panel.Name = "headVacuum3Panel";
            this.headVacuum3Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headVacuum3Panel.Size = new System.Drawing.Size(208, 32);
            this.headVacuum3Panel.TabIndex = 3;
            //
            // dotHeadVacuum3
            //
            this.dotHeadVacuum3.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadVacuum3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotHeadVacuum3.Location = new System.Drawing.Point(2, 8);
            this.dotHeadVacuum3.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.dotHeadVacuum3.Name = "dotHeadVacuum3";
            this.dotHeadVacuum3.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadVacuum3.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadVacuum3.Size = new System.Drawing.Size(18, 16);
            this.dotHeadVacuum3.TabIndex = 0;
            //
            // lblHeadVacuum3
            //
            this.lblHeadVacuum3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadVacuum3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadVacuum3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadVacuum3.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadVacuum3.Location = new System.Drawing.Point(25, 0);
            this.lblHeadVacuum3.Name = "lblHeadVacuum3";
            this.lblHeadVacuum3.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadVacuum3.Size = new System.Drawing.Size(180, 32);
            this.lblHeadVacuum3.TabIndex = 1;
            this.lblHeadVacuum3.Text = "HEAD VACUUM #3";
            this.lblHeadVacuum3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // headVacuum4Panel
            //
            this.headVacuum4Panel.ColumnCount = 2;
            this.headVacuum4Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.headVacuum4Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headVacuum4Panel.Controls.Add(this.dotHeadVacuum4, 0, 0);
            this.headVacuum4Panel.Controls.Add(this.lblHeadVacuum4, 1, 0);
            this.headVacuum4Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headVacuum4Panel.Location = new System.Drawing.Point(657, 59);
            this.headVacuum4Panel.Name = "headVacuum4Panel";
            this.headVacuum4Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headVacuum4Panel.Size = new System.Drawing.Size(210, 32);
            this.headVacuum4Panel.TabIndex = 4;
            //
            // dotHeadVacuum4
            //
            this.dotHeadVacuum4.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadVacuum4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotHeadVacuum4.Location = new System.Drawing.Point(2, 8);
            this.dotHeadVacuum4.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.dotHeadVacuum4.Name = "dotHeadVacuum4";
            this.dotHeadVacuum4.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadVacuum4.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadVacuum4.Size = new System.Drawing.Size(18, 16);
            this.dotHeadVacuum4.TabIndex = 0;
            //
            // lblHeadVacuum4
            //
            this.lblHeadVacuum4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadVacuum4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadVacuum4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadVacuum4.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadVacuum4.Location = new System.Drawing.Point(25, 0);
            this.lblHeadVacuum4.Name = "lblHeadVacuum4";
            this.lblHeadVacuum4.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadVacuum4.Size = new System.Drawing.Size(182, 32);
            this.lblHeadVacuum4.TabIndex = 1;
            this.lblHeadVacuum4.Text = "HEAD VACUUM #4";
            this.lblHeadVacuum4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // headBlow1Panel
            //
            this.headBlow1Panel.ColumnCount = 2;
            this.headBlow1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.headBlow1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headBlow1Panel.Controls.Add(this.dotHeadBlow1, 0, 0);
            this.headBlow1Panel.Controls.Add(this.lblHeadBlow1, 1, 0);
            this.headBlow1Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headBlow1Panel.Location = new System.Drawing.Point(15, 97);
            this.headBlow1Panel.Name = "headBlow1Panel";
            this.headBlow1Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headBlow1Panel.Size = new System.Drawing.Size(208, 32);
            this.headBlow1Panel.TabIndex = 5;
            //
            // dotHeadBlow1
            //
            this.dotHeadBlow1.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadBlow1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotHeadBlow1.Location = new System.Drawing.Point(2, 8);
            this.dotHeadBlow1.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.dotHeadBlow1.Name = "dotHeadBlow1";
            this.dotHeadBlow1.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadBlow1.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadBlow1.Size = new System.Drawing.Size(18, 16);
            this.dotHeadBlow1.TabIndex = 0;
            //
            // lblHeadBlow1
            //
            this.lblHeadBlow1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadBlow1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadBlow1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadBlow1.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadBlow1.Location = new System.Drawing.Point(25, 0);
            this.lblHeadBlow1.Name = "lblHeadBlow1";
            this.lblHeadBlow1.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadBlow1.Size = new System.Drawing.Size(180, 32);
            this.lblHeadBlow1.TabIndex = 1;
            this.lblHeadBlow1.Text = "HEAD BLOW #1";
            this.lblHeadBlow1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // headBlow2Panel
            //
            this.headBlow2Panel.ColumnCount = 2;
            this.headBlow2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.headBlow2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headBlow2Panel.Controls.Add(this.dotHeadBlow2, 0, 0);
            this.headBlow2Panel.Controls.Add(this.lblHeadBlow2, 1, 0);
            this.headBlow2Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headBlow2Panel.Location = new System.Drawing.Point(229, 97);
            this.headBlow2Panel.Name = "headBlow2Panel";
            this.headBlow2Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headBlow2Panel.Size = new System.Drawing.Size(208, 32);
            this.headBlow2Panel.TabIndex = 6;
            //
            // dotHeadBlow2
            //
            this.dotHeadBlow2.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadBlow2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotHeadBlow2.Location = new System.Drawing.Point(2, 8);
            this.dotHeadBlow2.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.dotHeadBlow2.Name = "dotHeadBlow2";
            this.dotHeadBlow2.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadBlow2.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadBlow2.Size = new System.Drawing.Size(18, 16);
            this.dotHeadBlow2.TabIndex = 0;
            //
            // lblHeadBlow2
            //
            this.lblHeadBlow2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadBlow2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadBlow2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadBlow2.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadBlow2.Location = new System.Drawing.Point(25, 0);
            this.lblHeadBlow2.Name = "lblHeadBlow2";
            this.lblHeadBlow2.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadBlow2.Size = new System.Drawing.Size(180, 32);
            this.lblHeadBlow2.TabIndex = 1;
            this.lblHeadBlow2.Text = "HEAD BLOW #2";
            this.lblHeadBlow2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // headBlow3Panel
            //
            this.headBlow3Panel.ColumnCount = 2;
            this.headBlow3Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.headBlow3Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headBlow3Panel.Controls.Add(this.dotHeadBlow3, 0, 0);
            this.headBlow3Panel.Controls.Add(this.lblHeadBlow3, 1, 0);
            this.headBlow3Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headBlow3Panel.Location = new System.Drawing.Point(443, 97);
            this.headBlow3Panel.Name = "headBlow3Panel";
            this.headBlow3Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headBlow3Panel.Size = new System.Drawing.Size(208, 32);
            this.headBlow3Panel.TabIndex = 7;
            //
            // dotHeadBlow3
            //
            this.dotHeadBlow3.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadBlow3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotHeadBlow3.Location = new System.Drawing.Point(2, 8);
            this.dotHeadBlow3.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.dotHeadBlow3.Name = "dotHeadBlow3";
            this.dotHeadBlow3.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadBlow3.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadBlow3.Size = new System.Drawing.Size(18, 16);
            this.dotHeadBlow3.TabIndex = 0;
            //
            // lblHeadBlow3
            //
            this.lblHeadBlow3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadBlow3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadBlow3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadBlow3.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadBlow3.Location = new System.Drawing.Point(25, 0);
            this.lblHeadBlow3.Name = "lblHeadBlow3";
            this.lblHeadBlow3.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadBlow3.Size = new System.Drawing.Size(180, 32);
            this.lblHeadBlow3.TabIndex = 1;
            this.lblHeadBlow3.Text = "HEAD BLOW #3";
            this.lblHeadBlow3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // headBlow4Panel
            //
            this.headBlow4Panel.ColumnCount = 2;
            this.headBlow4Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.headBlow4Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headBlow4Panel.Controls.Add(this.dotHeadBlow4, 0, 0);
            this.headBlow4Panel.Controls.Add(this.lblHeadBlow4, 1, 0);
            this.headBlow4Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headBlow4Panel.Location = new System.Drawing.Point(657, 97);
            this.headBlow4Panel.Name = "headBlow4Panel";
            this.headBlow4Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headBlow4Panel.Size = new System.Drawing.Size(210, 32);
            this.headBlow4Panel.TabIndex = 8;
            //
            // dotHeadBlow4
            //
            this.dotHeadBlow4.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadBlow4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotHeadBlow4.Location = new System.Drawing.Point(2, 8);
            this.dotHeadBlow4.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.dotHeadBlow4.Name = "dotHeadBlow4";
            this.dotHeadBlow4.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadBlow4.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadBlow4.Size = new System.Drawing.Size(18, 16);
            this.dotHeadBlow4.TabIndex = 0;
            //
            // lblHeadBlow4
            //
            this.lblHeadBlow4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadBlow4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadBlow4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadBlow4.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadBlow4.Location = new System.Drawing.Point(25, 0);
            this.lblHeadBlow4.Name = "lblHeadBlow4";
            this.lblHeadBlow4.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadBlow4.Size = new System.Drawing.Size(182, 32);
            this.lblHeadBlow4.TabIndex = 1;
            this.lblHeadBlow4.Text = "HEAD BLOW #4";
            this.lblHeadBlow4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // headFlow1Panel
            //
            this.headFlow1Panel.ColumnCount = 2;
            this.headFlow1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.headFlow1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headFlow1Panel.Controls.Add(this.dotHeadFlow1, 0, 0);
            this.headFlow1Panel.Controls.Add(this.lblHeadFlow1, 1, 0);
            this.headFlow1Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headFlow1Panel.Location = new System.Drawing.Point(15, 135);
            this.headFlow1Panel.Name = "headFlow1Panel";
            this.headFlow1Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headFlow1Panel.Size = new System.Drawing.Size(208, 32);
            this.headFlow1Panel.TabIndex = 9;
            //
            // dotHeadFlow1
            //
            this.dotHeadFlow1.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadFlow1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotHeadFlow1.Location = new System.Drawing.Point(2, 8);
            this.dotHeadFlow1.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.dotHeadFlow1.Name = "dotHeadFlow1";
            this.dotHeadFlow1.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadFlow1.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadFlow1.Size = new System.Drawing.Size(18, 16);
            this.dotHeadFlow1.TabIndex = 0;
            //
            // lblHeadFlow1
            //
            this.lblHeadFlow1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadFlow1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadFlow1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadFlow1.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadFlow1.Location = new System.Drawing.Point(25, 0);
            this.lblHeadFlow1.Name = "lblHeadFlow1";
            this.lblHeadFlow1.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadFlow1.Size = new System.Drawing.Size(180, 32);
            this.lblHeadFlow1.TabIndex = 1;
            this.lblHeadFlow1.Text = "HEAD FLOW #1 : OFF";
            this.lblHeadFlow1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // headFlow2Panel
            //
            this.headFlow2Panel.ColumnCount = 2;
            this.headFlow2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.headFlow2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headFlow2Panel.Controls.Add(this.dotHeadFlow2, 0, 0);
            this.headFlow2Panel.Controls.Add(this.lblHeadFlow2, 1, 0);
            this.headFlow2Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headFlow2Panel.Location = new System.Drawing.Point(229, 135);
            this.headFlow2Panel.Name = "headFlow2Panel";
            this.headFlow2Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headFlow2Panel.Size = new System.Drawing.Size(208, 32);
            this.headFlow2Panel.TabIndex = 10;
            //
            // dotHeadFlow2
            //
            this.dotHeadFlow2.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadFlow2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotHeadFlow2.Location = new System.Drawing.Point(2, 8);
            this.dotHeadFlow2.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.dotHeadFlow2.Name = "dotHeadFlow2";
            this.dotHeadFlow2.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadFlow2.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadFlow2.Size = new System.Drawing.Size(18, 16);
            this.dotHeadFlow2.TabIndex = 0;
            //
            // lblHeadFlow2
            //
            this.lblHeadFlow2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadFlow2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadFlow2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadFlow2.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadFlow2.Location = new System.Drawing.Point(25, 0);
            this.lblHeadFlow2.Name = "lblHeadFlow2";
            this.lblHeadFlow2.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadFlow2.Size = new System.Drawing.Size(180, 32);
            this.lblHeadFlow2.TabIndex = 1;
            this.lblHeadFlow2.Text = "HEAD FLOW #2 : OFF";
            this.lblHeadFlow2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // headFlow3Panel
            //
            this.headFlow3Panel.ColumnCount = 2;
            this.headFlow3Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.headFlow3Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headFlow3Panel.Controls.Add(this.dotHeadFlow3, 0, 0);
            this.headFlow3Panel.Controls.Add(this.lblHeadFlow3, 1, 0);
            this.headFlow3Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headFlow3Panel.Location = new System.Drawing.Point(443, 135);
            this.headFlow3Panel.Name = "headFlow3Panel";
            this.headFlow3Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headFlow3Panel.Size = new System.Drawing.Size(208, 32);
            this.headFlow3Panel.TabIndex = 11;
            //
            // dotHeadFlow3
            //
            this.dotHeadFlow3.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadFlow3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotHeadFlow3.Location = new System.Drawing.Point(2, 8);
            this.dotHeadFlow3.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.dotHeadFlow3.Name = "dotHeadFlow3";
            this.dotHeadFlow3.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadFlow3.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadFlow3.Size = new System.Drawing.Size(18, 16);
            this.dotHeadFlow3.TabIndex = 0;
            //
            // lblHeadFlow3
            //
            this.lblHeadFlow3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadFlow3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadFlow3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadFlow3.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadFlow3.Location = new System.Drawing.Point(25, 0);
            this.lblHeadFlow3.Name = "lblHeadFlow3";
            this.lblHeadFlow3.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadFlow3.Size = new System.Drawing.Size(180, 32);
            this.lblHeadFlow3.TabIndex = 1;
            this.lblHeadFlow3.Text = "HEAD FLOW #3 : OFF";
            this.lblHeadFlow3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // headFlow4Panel
            //
            this.headFlow4Panel.ColumnCount = 2;
            this.headFlow4Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.headFlow4Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.headFlow4Panel.Controls.Add(this.dotHeadFlow4, 0, 0);
            this.headFlow4Panel.Controls.Add(this.lblHeadFlow4, 1, 0);
            this.headFlow4Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headFlow4Panel.Location = new System.Drawing.Point(657, 135);
            this.headFlow4Panel.Name = "headFlow4Panel";
            this.headFlow4Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.headFlow4Panel.Size = new System.Drawing.Size(210, 32);
            this.headFlow4Panel.TabIndex = 12;
            //
            // dotHeadFlow4
            //
            this.dotHeadFlow4.BackColor = System.Drawing.Color.Transparent;
            this.dotHeadFlow4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotHeadFlow4.Location = new System.Drawing.Point(2, 8);
            this.dotHeadFlow4.Margin = new System.Windows.Forms.Padding(2, 8, 2, 8);
            this.dotHeadFlow4.Name = "dotHeadFlow4";
            this.dotHeadFlow4.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotHeadFlow4.OnColor = System.Drawing.Color.LimeGreen;
            this.dotHeadFlow4.Size = new System.Drawing.Size(18, 16);
            this.dotHeadFlow4.TabIndex = 0;
            //
            // lblHeadFlow4
            //
            this.lblHeadFlow4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblHeadFlow4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadFlow4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadFlow4.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeadFlow4.Location = new System.Drawing.Point(25, 0);
            this.lblHeadFlow4.Name = "lblHeadFlow4";
            this.lblHeadFlow4.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblHeadFlow4.Size = new System.Drawing.Size(182, 32);
            this.lblHeadFlow4.TabIndex = 1;
            this.lblHeadFlow4.Text = "HEAD FLOW #4 : OFF";
            this.lblHeadFlow4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // actionPanel
            //
            this.actionPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.actionPanel.Controls.Add(this.btnInput);
            this.actionPanel.Controls.Add(this.btnInspect);
            this.actionPanel.Controls.Add(this.btnBottom);
            this.actionPanel.Controls.Add(this.btnSide);
            this.actionPanel.Controls.Add(this.btnOutput);
            this.actionPanel.Controls.Add(this.actionSpacer);
            this.actionPanel.Controls.Add(this.btnStop);
            this.actionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionPanel.Location = new System.Drawing.Point(3, 715);
            this.actionPanel.Name = "actionPanel";
            this.actionPanel.Padding = new System.Windows.Forms.Padding(14, 10, 14, 10);
            this.actionPanel.Size = new System.Drawing.Size(1672, 82);
            this.actionPanel.TabIndex = 2;
            this.actionPanel.WrapContents = false;
            //
            // btnInput
            //
            this.btnInput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnInput.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInput.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnInput.ForeColor = System.Drawing.Color.White;
            this.btnInput.Location = new System.Drawing.Point(20, 16);
            this.btnInput.Margin = new System.Windows.Forms.Padding(6);
            this.btnInput.Name = "btnInput";
            this.btnInput.Size = new System.Drawing.Size(160, 64);
            this.btnInput.TabIndex = 0;
            this.btnInput.Text = "PICK UP";
            //
            // btnInspect
            //
            this.btnInspect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnInspect.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInspect.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnInspect.ForeColor = System.Drawing.Color.White;
            this.btnInspect.Location = new System.Drawing.Point(192, 16);
            this.btnInspect.Margin = new System.Windows.Forms.Padding(6);
            this.btnInspect.Name = "btnInspect";
            this.btnInspect.Size = new System.Drawing.Size(160, 64);
            this.btnInspect.TabIndex = 1;
            this.btnInspect.Text = "INSPECT";
            //
            // btnBottom
            //
            this.btnBottom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnBottom.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBottom.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnBottom.ForeColor = System.Drawing.Color.White;
            this.btnBottom.Location = new System.Drawing.Point(364, 16);
            this.btnBottom.Margin = new System.Windows.Forms.Padding(6);
            this.btnBottom.Name = "btnBottom";
            this.btnBottom.Size = new System.Drawing.Size(160, 64);
            this.btnBottom.TabIndex = 2;
            this.btnBottom.Text = "BOTTOM";
            //
            // btnSide
            //
            this.btnSide.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSide.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSide.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnSide.ForeColor = System.Drawing.Color.White;
            this.btnSide.Location = new System.Drawing.Point(536, 16);
            this.btnSide.Margin = new System.Windows.Forms.Padding(6);
            this.btnSide.Name = "btnSide";
            this.btnSide.Size = new System.Drawing.Size(160, 64);
            this.btnSide.TabIndex = 3;
            this.btnSide.Text = "SIDE";
            //
            // btnOutput
            //
            this.btnOutput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnOutput.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOutput.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnOutput.ForeColor = System.Drawing.Color.White;
            this.btnOutput.Location = new System.Drawing.Point(708, 16);
            this.btnOutput.Margin = new System.Windows.Forms.Padding(6);
            this.btnOutput.Name = "btnOutput";
            this.btnOutput.Size = new System.Drawing.Size(160, 64);
            this.btnOutput.TabIndex = 4;
            this.btnOutput.Text = "PLACE";
            //
            // actionSpacer
            //
            this.actionSpacer.Location = new System.Drawing.Point(874, 10);
            this.actionSpacer.Margin = new System.Windows.Forms.Padding(0);
            this.actionSpacer.Name = "actionSpacer";
            this.actionSpacer.Size = new System.Drawing.Size(612, 64);
            this.actionSpacer.TabIndex = 5;
            //
            // btnStop
            //
            this.btnStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(46)))), ((int)(((byte)(46)))));
            this.btnStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStop.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.Location = new System.Drawing.Point(1492, 16);
            this.btnStop.Margin = new System.Windows.Forms.Padding(6);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(160, 64);
            this.btnStop.TabIndex = 6;
            this.btnStop.Text = "STOP";
            //
            // RearPickerPage
            //
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.Controls.Add(this.rootLayout);
            this.Name = "RearPickerPage";
            this.Size = new System.Drawing.Size(1678, 800);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.topLayout.ResumeLayout(false);
            this.grpState.ResumeLayout(false);
            this.stateLayout.ResumeLayout(false);
            this.grpCounters.ResumeLayout(false);
            this.counterLayout.ResumeLayout(false);
            this.grpAxis.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.axisGrid)).EndInit();
            this.grpInfo.ResumeLayout(false);
            this.infoLayout.ResumeLayout(false);
            this.headAxisTPanel.ResumeLayout(false);
            this.headProcessPanel.ResumeLayout(false);
            this.processFlowPanel.ResumeLayout(false);
            this.processDetailPanel.ResumeLayout(false);
            this.headVacuum1Panel.ResumeLayout(false);
            this.headVacuum2Panel.ResumeLayout(false);
            this.headVacuum3Panel.ResumeLayout(false);
            this.headVacuum4Panel.ResumeLayout(false);
            this.headBlow1Panel.ResumeLayout(false);
            this.headBlow2Panel.ResumeLayout(false);
            this.headBlow3Panel.ResumeLayout(false);
            this.headBlow4Panel.ResumeLayout(false);
            this.headFlow1Panel.ResumeLayout(false);
            this.headFlow2Panel.ResumeLayout(false);
            this.headFlow3Panel.ResumeLayout(false);
            this.headFlow4Panel.ResumeLayout(false);
            this.actionPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
    }
}
