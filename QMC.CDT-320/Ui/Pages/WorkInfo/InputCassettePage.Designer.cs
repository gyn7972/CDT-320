using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class InputCassettePage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel topLayout;
        private TableLayoutPanel contentLayout;
        private GroupBox grpSlotState;
        private GroupBox grpLifter;
        private GroupBox grpLegend;
        private TableLayoutPanel slotStateLayout;
        private TableLayoutPanel lifterLayout;
        private FlowLayoutPanel actionsLayout;
        private TableLayoutPanel lifterAxisPanel;
        private Label lblLifterAxisTitle;
        private TableLayoutPanel cassetteCheck1Panel;
        private IndicatorDot dotCassetteCheck1;
        private Label lblCassetteCheck1;
        private TableLayoutPanel cassetteCheck2Panel;
        private IndicatorDot dotCassetteCheck2;
        private Label lblCassetteCheck2;
        private TableLayoutPanel legendLayout;
        private TableLayoutPanel legendReadyPanel;
        private TableLayoutPanel legendEmptyPanel;
        private TableLayoutPanel legendWorkingPanel;
        private TableLayoutPanel legendFinishPanel;
        private TableLayoutPanel legendWorkReadyPanel;
        private Label lblLegendReadyColor;
        private Label lblLegendReadyText;
        private Label lblLegendEmptyColor;
        private Label lblLegendEmptyText;
        private Label lblLegendWorkingColor;
        private Label lblLegendWorkingText;
        private Label lblLegendFinishColor;
        private Label lblLegendFinishText;
        private Label lblLegendWorkReadyColor;
        private Label lblLegendWorkReadyText;
        private Label lblSlotNoTitle;
        private Label lblSlotNoValue;
        private Label lblSlotStateTitle;
        private Label lblSlotStateValue;
        private Label _lifterPosLabel;
        private Label[] _slotLeds;
        private Label[] _slotIndexLbls;
        private Label[] _slotNameLbls;
        private Button btnPrev;
        private Button btnNext;
        private Button btnInit;
        private Button btnReady;
        private ActionButton btnMap;
        private ActionButton btnLoad;
        private ActionButton btnUnload;

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.topLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lifterAxisPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblLifterAxisTitle = new System.Windows.Forms.Label();
            this._lifterPosLabel = new System.Windows.Forms.Label();
            this.cassetteCheck1Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotCassetteCheck1 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblCassetteCheck1 = new System.Windows.Forms.Label();
            this.cassetteCheck2Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotCassetteCheck2 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblCassetteCheck2 = new System.Windows.Forms.Label();
            this.grpLegend = new System.Windows.Forms.GroupBox();
            this.legendLayout = new System.Windows.Forms.TableLayoutPanel();
            this.legendReadyPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblLegendReadyColor = new System.Windows.Forms.Label();
            this.lblLegendReadyText = new System.Windows.Forms.Label();
            this.legendEmptyPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblLegendEmptyColor = new System.Windows.Forms.Label();
            this.lblLegendEmptyText = new System.Windows.Forms.Label();
            this.legendWorkingPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblLegendWorkingColor = new System.Windows.Forms.Label();
            this.lblLegendWorkingText = new System.Windows.Forms.Label();
            this.legendFinishPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblLegendFinishColor = new System.Windows.Forms.Label();
            this.lblLegendFinishText = new System.Windows.Forms.Label();
            this.legendWorkReadyPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblLegendWorkReadyColor = new System.Windows.Forms.Label();
            this.lblLegendWorkReadyText = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpSlotState = new System.Windows.Forms.GroupBox();
            this.slotStateLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblSlotNoTitle = new System.Windows.Forms.Label();
            this.lblSlotNoValue = new System.Windows.Forms.Label();
            this.lblSlotStateTitle = new System.Windows.Forms.Label();
            this.lblSlotStateValue = new System.Windows.Forms.Label();
            this.btnPrev = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.btnInit = new System.Windows.Forms.Button();
            this.btnReady = new System.Windows.Forms.Button();
            this.grpLifter = new System.Windows.Forms.GroupBox();
            this.lifterLayout = new System.Windows.Forms.TableLayoutPanel();
            this.actionsLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.btnMap = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnLoad = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnUnload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.rootLayout.SuspendLayout();
            this.topLayout.SuspendLayout();
            this.lifterAxisPanel.SuspendLayout();
            this.cassetteCheck1Panel.SuspendLayout();
            this.cassetteCheck2Panel.SuspendLayout();
            this.grpLegend.SuspendLayout();
            this.legendLayout.SuspendLayout();
            this.legendReadyPanel.SuspendLayout();
            this.legendEmptyPanel.SuspendLayout();
            this.legendWorkingPanel.SuspendLayout();
            this.legendFinishPanel.SuspendLayout();
            this.legendWorkReadyPanel.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.grpSlotState.SuspendLayout();
            this.slotStateLayout.SuspendLayout();
            this.grpLifter.SuspendLayout();
            this.actionsLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.topLayout, 0, 1);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 2);
            this.rootLayout.Controls.Add(this.actionsLayout, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 88F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.rootLayout.Size = new System.Drawing.Size(1400, 900);
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
            this.lblHeader.Size = new System.Drawing.Size(1394, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:common.info";
            this.lblHeader.Text = "INFO";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // topLayout
            // 
            this.topLayout.ColumnCount = 4;
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 240F));
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topLayout.Controls.Add(this.lifterAxisPanel, 0, 0);
            this.topLayout.Controls.Add(this.cassetteCheck1Panel, 1, 0);
            this.topLayout.Controls.Add(this.cassetteCheck2Panel, 2, 0);
            this.topLayout.Controls.Add(this.grpLegend, 3, 0);
            this.topLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topLayout.Location = new System.Drawing.Point(3, 33);
            this.topLayout.Name = "topLayout";
            this.topLayout.Padding = new System.Windows.Forms.Padding(8);
            this.topLayout.RowCount = 1;
            this.topLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topLayout.Size = new System.Drawing.Size(1394, 82);
            this.topLayout.TabIndex = 1;
            // 
            // lifterAxisPanel
            // 
            this.lifterAxisPanel.ColumnCount = 1;
            this.lifterAxisPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.lifterAxisPanel.Controls.Add(this.lblLifterAxisTitle, 0, 0);
            this.lifterAxisPanel.Controls.Add(this._lifterPosLabel, 0, 1);
            this.lifterAxisPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lifterAxisPanel.Location = new System.Drawing.Point(12, 12);
            this.lifterAxisPanel.Margin = new System.Windows.Forms.Padding(4);
            this.lifterAxisPanel.Name = "lifterAxisPanel";
            this.lifterAxisPanel.RowCount = 2;
            this.lifterAxisPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.lifterAxisPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lifterAxisPanel.Size = new System.Drawing.Size(232, 58);
            this.lifterAxisPanel.TabIndex = 0;
            // 
            // lblLifterAxisTitle
            // 
            this.lblLifterAxisTitle.BackColor = System.Drawing.Color.Black;
            this.lblLifterAxisTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLifterAxisTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblLifterAxisTitle.ForeColor = System.Drawing.Color.White;
            this.lblLifterAxisTitle.Location = new System.Drawing.Point(3, 0);
            this.lblLifterAxisTitle.Name = "lblLifterAxisTitle";
            this.lblLifterAxisTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblLifterAxisTitle.Size = new System.Drawing.Size(226, 24);
            this.lblLifterAxisTitle.TabIndex = 0;
            this.lblLifterAxisTitle.Text = "LIFTER AXIS Z";
            this.lblLifterAxisTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lifterPosLabel
            // 
            this._lifterPosLabel.BackColor = System.Drawing.Color.White;
            this._lifterPosLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lifterPosLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lifterPosLabel.Font = new System.Drawing.Font("Consolas", 10F);
            this._lifterPosLabel.Location = new System.Drawing.Point(3, 24);
            this._lifterPosLabel.Name = "_lifterPosLabel";
            this._lifterPosLabel.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this._lifterPosLabel.Size = new System.Drawing.Size(226, 34);
            this._lifterPosLabel.TabIndex = 1;
            this._lifterPosLabel.Text = "0.000 mm";
            this._lifterPosLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cassetteCheck1Panel
            // 
            this.cassetteCheck1Panel.ColumnCount = 2;
            this.cassetteCheck1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.cassetteCheck1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cassetteCheck1Panel.Controls.Add(this.dotCassetteCheck1, 0, 0);
            this.cassetteCheck1Panel.Controls.Add(this.lblCassetteCheck1, 1, 0);
            this.cassetteCheck1Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cassetteCheck1Panel.Location = new System.Drawing.Point(252, 12);
            this.cassetteCheck1Panel.Margin = new System.Windows.Forms.Padding(4);
            this.cassetteCheck1Panel.Name = "cassetteCheck1Panel";
            this.cassetteCheck1Panel.RowCount = 1;
            this.cassetteCheck1Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cassetteCheck1Panel.Size = new System.Drawing.Size(212, 58);
            this.cassetteCheck1Panel.TabIndex = 1;
            // 
            // dotCassetteCheck1
            // 
            this.dotCassetteCheck1.BackColor = System.Drawing.Color.Transparent;
            this.dotCassetteCheck1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotCassetteCheck1.Location = new System.Drawing.Point(6, 26);
            this.dotCassetteCheck1.Margin = new System.Windows.Forms.Padding(6, 26, 6, 26);
            this.dotCassetteCheck1.Name = "dotCassetteCheck1";
            this.dotCassetteCheck1.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotCassetteCheck1.OnColor = System.Drawing.Color.LimeGreen;
            this.dotCassetteCheck1.Size = new System.Drawing.Size(16, 6);
            this.dotCassetteCheck1.TabIndex = 0;
            // 
            // lblCassetteCheck1
            // 
            this.lblCassetteCheck1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblCassetteCheck1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCassetteCheck1.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblCassetteCheck1.Location = new System.Drawing.Point(31, 0);
            this.lblCassetteCheck1.Name = "lblCassetteCheck1";
            this.lblCassetteCheck1.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblCassetteCheck1.Size = new System.Drawing.Size(178, 58);
            this.lblCassetteCheck1.TabIndex = 1;
            this.lblCassetteCheck1.Text = "LIFTER CASSETTE CHECK #1";
            this.lblCassetteCheck1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cassetteCheck2Panel
            // 
            this.cassetteCheck2Panel.ColumnCount = 2;
            this.cassetteCheck2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.cassetteCheck2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cassetteCheck2Panel.Controls.Add(this.dotCassetteCheck2, 0, 0);
            this.cassetteCheck2Panel.Controls.Add(this.lblCassetteCheck2, 1, 0);
            this.cassetteCheck2Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cassetteCheck2Panel.Location = new System.Drawing.Point(472, 12);
            this.cassetteCheck2Panel.Margin = new System.Windows.Forms.Padding(4);
            this.cassetteCheck2Panel.Name = "cassetteCheck2Panel";
            this.cassetteCheck2Panel.RowCount = 1;
            this.cassetteCheck2Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cassetteCheck2Panel.Size = new System.Drawing.Size(212, 58);
            this.cassetteCheck2Panel.TabIndex = 2;
            // 
            // dotCassetteCheck2
            // 
            this.dotCassetteCheck2.BackColor = System.Drawing.Color.Transparent;
            this.dotCassetteCheck2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotCassetteCheck2.Location = new System.Drawing.Point(6, 26);
            this.dotCassetteCheck2.Margin = new System.Windows.Forms.Padding(6, 26, 6, 26);
            this.dotCassetteCheck2.Name = "dotCassetteCheck2";
            this.dotCassetteCheck2.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotCassetteCheck2.OnColor = System.Drawing.Color.LimeGreen;
            this.dotCassetteCheck2.Size = new System.Drawing.Size(16, 6);
            this.dotCassetteCheck2.TabIndex = 0;
            // 
            // lblCassetteCheck2
            // 
            this.lblCassetteCheck2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblCassetteCheck2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCassetteCheck2.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblCassetteCheck2.Location = new System.Drawing.Point(31, 0);
            this.lblCassetteCheck2.Name = "lblCassetteCheck2";
            this.lblCassetteCheck2.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblCassetteCheck2.Size = new System.Drawing.Size(178, 58);
            this.lblCassetteCheck2.TabIndex = 1;
            this.lblCassetteCheck2.Text = "LIFTER CASSETTE CHECK #2";
            this.lblCassetteCheck2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpLegend
            // 
            this.grpLegend.Controls.Add(this.legendLayout);
            this.grpLegend.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLegend.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpLegend.Location = new System.Drawing.Point(691, 11);
            this.grpLegend.Name = "grpLegend";
            this.grpLegend.Size = new System.Drawing.Size(692, 60);
            this.grpLegend.TabIndex = 3;
            this.grpLegend.TabStop = false;
            this.grpLegend.Text = "Legend";
            // 
            // legendLayout
            // 
            this.legendLayout.ColumnCount = 4;
            this.legendLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.legendLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.legendLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.legendLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.legendLayout.Controls.Add(this.legendReadyPanel, 0, 0);
            this.legendLayout.Controls.Add(this.legendEmptyPanel, 1, 0);
            this.legendLayout.Controls.Add(this.legendWorkingPanel, 2, 0);
            this.legendLayout.Controls.Add(this.legendFinishPanel, 3, 0);
            this.legendLayout.Controls.Add(this.legendWorkReadyPanel, 0, 1);
            this.legendLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.legendLayout.Location = new System.Drawing.Point(3, 28);
            this.legendLayout.Name = "legendLayout";
            this.legendLayout.Padding = new System.Windows.Forms.Padding(8, 18, 8, 8);
            this.legendLayout.RowCount = 2;
            this.legendLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.legendLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.legendLayout.Size = new System.Drawing.Size(686, 29);
            this.legendLayout.TabIndex = 0;
            // 
            // legendReadyPanel
            // 
            this.legendReadyPanel.ColumnCount = 2;
            this.legendReadyPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.legendReadyPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendReadyPanel.Controls.Add(this.lblLegendReadyColor, 0, 0);
            this.legendReadyPanel.Controls.Add(this.lblLegendReadyText, 1, 0);
            this.legendReadyPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.legendReadyPanel.Location = new System.Drawing.Point(11, 21);
            this.legendReadyPanel.Name = "legendReadyPanel";
            this.legendReadyPanel.RowCount = 1;
            this.legendReadyPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendReadyPanel.Size = new System.Drawing.Size(161, 1);
            this.legendReadyPanel.TabIndex = 0;
            // 
            // lblLegendReadyColor
            // 
            this.lblLegendReadyColor.BackColor = System.Drawing.Color.Cyan;
            this.lblLegendReadyColor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendReadyColor.Location = new System.Drawing.Point(2, 7);
            this.lblLegendReadyColor.Margin = new System.Windows.Forms.Padding(2, 7, 2, 7);
            this.lblLegendReadyColor.Name = "lblLegendReadyColor";
            this.lblLegendReadyColor.Size = new System.Drawing.Size(26, 1);
            this.lblLegendReadyColor.TabIndex = 0;
            // 
            // lblLegendReadyText
            // 
            this.lblLegendReadyText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendReadyText.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLegendReadyText.Location = new System.Drawing.Point(33, 0);
            this.lblLegendReadyText.Name = "lblLegendReadyText";
            this.lblLegendReadyText.Size = new System.Drawing.Size(125, 1);
            this.lblLegendReadyText.TabIndex = 1;
            this.lblLegendReadyText.Text = "READY";
            this.lblLegendReadyText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // legendEmptyPanel
            // 
            this.legendEmptyPanel.ColumnCount = 2;
            this.legendEmptyPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.legendEmptyPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendEmptyPanel.Controls.Add(this.lblLegendEmptyColor, 0, 0);
            this.legendEmptyPanel.Controls.Add(this.lblLegendEmptyText, 1, 0);
            this.legendEmptyPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.legendEmptyPanel.Location = new System.Drawing.Point(178, 21);
            this.legendEmptyPanel.Name = "legendEmptyPanel";
            this.legendEmptyPanel.RowCount = 1;
            this.legendEmptyPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendEmptyPanel.Size = new System.Drawing.Size(161, 1);
            this.legendEmptyPanel.TabIndex = 1;
            // 
            // lblLegendEmptyColor
            // 
            this.lblLegendEmptyColor.BackColor = System.Drawing.Color.LimeGreen;
            this.lblLegendEmptyColor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendEmptyColor.Location = new System.Drawing.Point(2, 7);
            this.lblLegendEmptyColor.Margin = new System.Windows.Forms.Padding(2, 7, 2, 7);
            this.lblLegendEmptyColor.Name = "lblLegendEmptyColor";
            this.lblLegendEmptyColor.Size = new System.Drawing.Size(26, 1);
            this.lblLegendEmptyColor.TabIndex = 0;
            // 
            // lblLegendEmptyText
            // 
            this.lblLegendEmptyText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendEmptyText.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLegendEmptyText.Location = new System.Drawing.Point(33, 0);
            this.lblLegendEmptyText.Name = "lblLegendEmptyText";
            this.lblLegendEmptyText.Size = new System.Drawing.Size(125, 1);
            this.lblLegendEmptyText.TabIndex = 1;
            this.lblLegendEmptyText.Text = "EMPTY";
            this.lblLegendEmptyText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // legendWorkingPanel
            // 
            this.legendWorkingPanel.ColumnCount = 2;
            this.legendWorkingPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.legendWorkingPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendWorkingPanel.Controls.Add(this.lblLegendWorkingColor, 0, 0);
            this.legendWorkingPanel.Controls.Add(this.lblLegendWorkingText, 1, 0);
            this.legendWorkingPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.legendWorkingPanel.Location = new System.Drawing.Point(345, 21);
            this.legendWorkingPanel.Name = "legendWorkingPanel";
            this.legendWorkingPanel.RowCount = 1;
            this.legendWorkingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendWorkingPanel.Size = new System.Drawing.Size(161, 1);
            this.legendWorkingPanel.TabIndex = 2;
            // 
            // lblLegendWorkingColor
            // 
            this.lblLegendWorkingColor.BackColor = System.Drawing.Color.Orange;
            this.lblLegendWorkingColor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendWorkingColor.Location = new System.Drawing.Point(2, 7);
            this.lblLegendWorkingColor.Margin = new System.Windows.Forms.Padding(2, 7, 2, 7);
            this.lblLegendWorkingColor.Name = "lblLegendWorkingColor";
            this.lblLegendWorkingColor.Size = new System.Drawing.Size(26, 1);
            this.lblLegendWorkingColor.TabIndex = 0;
            // 
            // lblLegendWorkingText
            // 
            this.lblLegendWorkingText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendWorkingText.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLegendWorkingText.Location = new System.Drawing.Point(33, 0);
            this.lblLegendWorkingText.Name = "lblLegendWorkingText";
            this.lblLegendWorkingText.Size = new System.Drawing.Size(125, 1);
            this.lblLegendWorkingText.TabIndex = 1;
            this.lblLegendWorkingText.Text = "WORKING";
            this.lblLegendWorkingText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // legendFinishPanel
            // 
            this.legendFinishPanel.ColumnCount = 2;
            this.legendFinishPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.legendFinishPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendFinishPanel.Controls.Add(this.lblLegendFinishColor, 0, 0);
            this.legendFinishPanel.Controls.Add(this.lblLegendFinishText, 1, 0);
            this.legendFinishPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.legendFinishPanel.Location = new System.Drawing.Point(512, 21);
            this.legendFinishPanel.Name = "legendFinishPanel";
            this.legendFinishPanel.RowCount = 1;
            this.legendFinishPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendFinishPanel.Size = new System.Drawing.Size(163, 1);
            this.legendFinishPanel.TabIndex = 3;
            // 
            // lblLegendFinishColor
            // 
            this.lblLegendFinishColor.BackColor = System.Drawing.Color.Red;
            this.lblLegendFinishColor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendFinishColor.Location = new System.Drawing.Point(2, 7);
            this.lblLegendFinishColor.Margin = new System.Windows.Forms.Padding(2, 7, 2, 7);
            this.lblLegendFinishColor.Name = "lblLegendFinishColor";
            this.lblLegendFinishColor.Size = new System.Drawing.Size(26, 1);
            this.lblLegendFinishColor.TabIndex = 0;
            // 
            // lblLegendFinishText
            // 
            this.lblLegendFinishText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendFinishText.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLegendFinishText.Location = new System.Drawing.Point(33, 0);
            this.lblLegendFinishText.Name = "lblLegendFinishText";
            this.lblLegendFinishText.Size = new System.Drawing.Size(127, 1);
            this.lblLegendFinishText.TabIndex = 1;
            this.lblLegendFinishText.Text = "FINISH";
            this.lblLegendFinishText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // legendWorkReadyPanel
            // 
            this.legendWorkReadyPanel.ColumnCount = 2;
            this.legendWorkReadyPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.legendWorkReadyPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendWorkReadyPanel.Controls.Add(this.lblLegendWorkReadyColor, 0, 0);
            this.legendWorkReadyPanel.Controls.Add(this.lblLegendWorkReadyText, 1, 0);
            this.legendWorkReadyPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.legendWorkReadyPanel.Location = new System.Drawing.Point(11, 22);
            this.legendWorkReadyPanel.Name = "legendWorkReadyPanel";
            this.legendWorkReadyPanel.RowCount = 1;
            this.legendWorkReadyPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendWorkReadyPanel.Size = new System.Drawing.Size(161, 1);
            this.legendWorkReadyPanel.TabIndex = 4;
            // 
            // lblLegendWorkReadyColor
            // 
            this.lblLegendWorkReadyColor.BackColor = System.Drawing.Color.Navy;
            this.lblLegendWorkReadyColor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendWorkReadyColor.Location = new System.Drawing.Point(2, 7);
            this.lblLegendWorkReadyColor.Margin = new System.Windows.Forms.Padding(2, 7, 2, 7);
            this.lblLegendWorkReadyColor.Name = "lblLegendWorkReadyColor";
            this.lblLegendWorkReadyColor.Size = new System.Drawing.Size(26, 1);
            this.lblLegendWorkReadyColor.TabIndex = 0;
            // 
            // lblLegendWorkReadyText
            // 
            this.lblLegendWorkReadyText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendWorkReadyText.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLegendWorkReadyText.Location = new System.Drawing.Point(33, 0);
            this.lblLegendWorkReadyText.Name = "lblLegendWorkReadyText";
            this.lblLegendWorkReadyText.Size = new System.Drawing.Size(125, 1);
            this.lblLegendWorkReadyText.TabIndex = 1;
            this.lblLegendWorkReadyText.Text = "WORK READY";
            this.lblLegendWorkReadyText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 340F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 390F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Controls.Add(this.grpSlotState, 0, 0);
            this.contentLayout.Controls.Add(this.grpLifter, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(3, 121);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1394, 680);
            this.contentLayout.TabIndex = 2;
            // 
            // grpSlotState
            // 
            this.grpSlotState.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpSlotState.Controls.Add(this.slotStateLayout);
            this.grpSlotState.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpSlotState.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpSlotState.Location = new System.Drawing.Point(15, 11);
            this.grpSlotState.Name = "grpSlotState";
            this.grpSlotState.Size = new System.Drawing.Size(334, 210);
            this.grpSlotState.TabIndex = 0;
            this.grpSlotState.TabStop = false;
            this.grpSlotState.Text = "SLOT STATE";
            // 
            // slotStateLayout
            // 
            this.slotStateLayout.ColumnCount = 2;
            this.slotStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.slotStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.slotStateLayout.Controls.Add(this.lblSlotNoTitle, 0, 0);
            this.slotStateLayout.Controls.Add(this.lblSlotNoValue, 1, 0);
            this.slotStateLayout.Controls.Add(this.lblSlotStateTitle, 0, 1);
            this.slotStateLayout.Controls.Add(this.lblSlotStateValue, 1, 1);
            this.slotStateLayout.Controls.Add(this.btnPrev, 0, 2);
            this.slotStateLayout.Controls.Add(this.btnNext, 1, 2);
            this.slotStateLayout.Controls.Add(this.btnInit, 0, 3);
            this.slotStateLayout.Controls.Add(this.btnReady, 1, 3);
            this.slotStateLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.slotStateLayout.Location = new System.Drawing.Point(3, 28);
            this.slotStateLayout.Name = "slotStateLayout";
            this.slotStateLayout.Padding = new System.Windows.Forms.Padding(8, 20, 8, 8);
            this.slotStateLayout.RowCount = 5;
            this.slotStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.slotStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.slotStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.slotStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.slotStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.slotStateLayout.Size = new System.Drawing.Size(328, 179);
            this.slotStateLayout.TabIndex = 0;
            // 
            // lblSlotNoTitle
            // 
            this.lblSlotNoTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSlotNoTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblSlotNoTitle.Location = new System.Drawing.Point(11, 20);
            this.lblSlotNoTitle.Name = "lblSlotNoTitle";
            this.lblSlotNoTitle.Size = new System.Drawing.Size(150, 32);
            this.lblSlotNoTitle.TabIndex = 0;
            this.lblSlotNoTitle.Text = "Slot No";
            this.lblSlotNoTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSlotNoValue
            // 
            this.lblSlotNoValue.BackColor = System.Drawing.Color.White;
            this.lblSlotNoValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSlotNoValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSlotNoValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblSlotNoValue.Location = new System.Drawing.Point(167, 20);
            this.lblSlotNoValue.Name = "lblSlotNoValue";
            this.lblSlotNoValue.Size = new System.Drawing.Size(150, 32);
            this.lblSlotNoValue.TabIndex = 1;
            this.lblSlotNoValue.Text = "BIN 1";
            this.lblSlotNoValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSlotStateTitle
            // 
            this.lblSlotStateTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSlotStateTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblSlotStateTitle.Location = new System.Drawing.Point(11, 52);
            this.lblSlotStateTitle.Name = "lblSlotStateTitle";
            this.lblSlotStateTitle.Size = new System.Drawing.Size(150, 32);
            this.lblSlotStateTitle.TabIndex = 2;
            this.lblSlotStateTitle.Text = "State";
            this.lblSlotStateTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSlotStateValue
            // 
            this.lblSlotStateValue.BackColor = System.Drawing.Color.White;
            this.lblSlotStateValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSlotStateValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSlotStateValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblSlotStateValue.Location = new System.Drawing.Point(167, 52);
            this.lblSlotStateValue.Name = "lblSlotStateValue";
            this.lblSlotStateValue.Size = new System.Drawing.Size(150, 32);
            this.lblSlotStateValue.TabIndex = 3;
            this.lblSlotStateValue.Text = "EMPTY";
            this.lblSlotStateValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnPrev
            // 
            this.btnPrev.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPrev.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrev.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnPrev.Location = new System.Drawing.Point(11, 87);
            this.btnPrev.Name = "btnPrev";
            this.btnPrev.Size = new System.Drawing.Size(150, 36);
            this.btnPrev.TabIndex = 4;
            this.btnPrev.Tag = "i18n:common.prev";
            this.btnPrev.Text = "PREV";
            // 
            // btnNext
            // 
            this.btnNext.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNext.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnNext.Location = new System.Drawing.Point(167, 87);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(150, 36);
            this.btnNext.TabIndex = 5;
            this.btnNext.Tag = "i18n:common.next";
            this.btnNext.Text = "NEXT";
            // 
            // btnInit
            // 
            this.btnInit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInit.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnInit.Location = new System.Drawing.Point(11, 129);
            this.btnInit.Name = "btnInit";
            this.btnInit.Size = new System.Drawing.Size(150, 36);
            this.btnInit.TabIndex = 6;
            this.btnInit.Tag = "i18n:wi.lifterInit";
            this.btnInit.Text = "LIFTER INIT";
            // 
            // btnReady
            // 
            this.btnReady.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReady.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReady.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnReady.Location = new System.Drawing.Point(167, 129);
            this.btnReady.Name = "btnReady";
            this.btnReady.Size = new System.Drawing.Size(150, 36);
            this.btnReady.TabIndex = 7;
            this.btnReady.Tag = "i18n:wi.lifterReady";
            this.btnReady.Text = "LIFTER READY";
            // 
            // grpLifter
            // 
            this.grpLifter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpLifter.Controls.Add(this.lifterLayout);
            this.grpLifter.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpLifter.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpLifter.Location = new System.Drawing.Point(355, 11);
            this.grpLifter.Name = "grpLifter";
            this.grpLifter.Size = new System.Drawing.Size(384, 430);
            this.grpLifter.TabIndex = 1;
            this.grpLifter.TabStop = false;
            this.grpLifter.Text = "LIFTER";
            // 
            // lifterLayout
            // 
            this.lifterLayout.ColumnCount = 3;
            this.lifterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.lifterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.lifterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lifterLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lifterLayout.Location = new System.Drawing.Point(3, 28);
            this.lifterLayout.Name = "lifterLayout";
            this.lifterLayout.Padding = new System.Windows.Forms.Padding(8, 20, 8, 8);
            this.lifterLayout.RowCount = 16;
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.25F));
            this.lifterLayout.Size = new System.Drawing.Size(378, 399);
            this.lifterLayout.TabIndex = 0;
            // 
            // actionsLayout
            // 
            this.actionsLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.actionsLayout.Controls.Add(this.btnMap);
            this.actionsLayout.Controls.Add(this.btnLoad);
            this.actionsLayout.Controls.Add(this.btnUnload);
            this.actionsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionsLayout.Location = new System.Drawing.Point(3, 807);
            this.actionsLayout.Name = "actionsLayout";
            this.actionsLayout.Padding = new System.Windows.Forms.Padding(12);
            this.actionsLayout.Size = new System.Drawing.Size(1394, 90);
            this.actionsLayout.TabIndex = 3;
            // 
            // btnMap
            // 
            this.btnMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnMap.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMap.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnMap.ForeColor = System.Drawing.Color.White;
            this.btnMap.Location = new System.Drawing.Point(18, 18);
            this.btnMap.Margin = new System.Windows.Forms.Padding(6);
            this.btnMap.Name = "btnMap";
            this.btnMap.Size = new System.Drawing.Size(180, 64);
            this.btnMap.TabIndex = 0;
            this.btnMap.Tag = "i18n:wi.liftWaferMapping";
            this.btnMap.Text = "LIFT WAFER MAPPING";
            // 
            // btnLoad
            // 
            this.btnLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnLoad.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLoad.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnLoad.ForeColor = System.Drawing.Color.White;
            this.btnLoad.Location = new System.Drawing.Point(210, 18);
            this.btnLoad.Margin = new System.Windows.Forms.Padding(6);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(180, 64);
            this.btnLoad.TabIndex = 1;
            this.btnLoad.Tag = "i18n:wi.liftWaferLoading";
            this.btnLoad.Text = "LIFT WAFER LOADING";
            // 
            // btnUnload
            // 
            this.btnUnload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnUnload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUnload.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnUnload.ForeColor = System.Drawing.Color.White;
            this.btnUnload.Location = new System.Drawing.Point(402, 18);
            this.btnUnload.Margin = new System.Windows.Forms.Padding(6);
            this.btnUnload.Name = "btnUnload";
            this.btnUnload.Size = new System.Drawing.Size(180, 64);
            this.btnUnload.TabIndex = 2;
            this.btnUnload.Tag = "i18n:wi.liftWaferUnloading";
            this.btnUnload.Text = "LIFT WAFER UNLOADING";
            // 
            // InputCassettePage
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.Controls.Add(this.rootLayout);
            this.Name = "InputCassettePage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.topLayout.ResumeLayout(false);
            this.lifterAxisPanel.ResumeLayout(false);
            this.cassetteCheck1Panel.ResumeLayout(false);
            this.cassetteCheck2Panel.ResumeLayout(false);
            this.grpLegend.ResumeLayout(false);
            this.legendLayout.ResumeLayout(false);
            this.legendReadyPanel.ResumeLayout(false);
            this.legendEmptyPanel.ResumeLayout(false);
            this.legendWorkingPanel.ResumeLayout(false);
            this.legendFinishPanel.ResumeLayout(false);
            this.legendWorkReadyPanel.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.grpSlotState.ResumeLayout(false);
            this.slotStateLayout.ResumeLayout(false);
            this.grpLifter.ResumeLayout(false);
            this.actionsLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}


