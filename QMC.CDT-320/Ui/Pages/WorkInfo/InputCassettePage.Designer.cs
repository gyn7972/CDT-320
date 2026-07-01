using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class InputCassettePage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private GroupBox grpSlotState;
        private GroupBox grpLifter;
        private GroupBox grpAction;
        private TableLayoutPanel topRow;
        private TableLayoutPanel slotStateLayout;
        private TableLayoutPanel lifterLayout;
        private TableLayoutPanel cassetteLevelLayout;
        private MaterialDetailView materialDetailView;
        private CassetteSlotView cassetteSlotView;
        private CassetteSlotView cassetteSlotViewLevel2;
        private TableLayoutPanel actionBar;
        private TableLayoutPanel lifterAxisPanel;
        private Label lblLifterAxisTitle;
        private TableLayoutPanel cassetteCheck1Panel;
        private IndicatorDot dotCassetteCheck1;
        private Label lblCassetteCheck1;
        private TableLayoutPanel cassetteCheck2Panel;
        private IndicatorDot dotCassetteCheck2;
        private Label lblCassetteCheck2;
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
        private Button btnPrev;
        private Button btnNext;
        private Button btnInit;
        private Button btnReady;
        private ActionButton btnMap;
        private ActionButton btnLoad;
        private ActionButton btnUnload;
        private ActionButton btnStop;

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.topRow = new System.Windows.Forms.TableLayoutPanel();
            this.grpSlotState = new System.Windows.Forms.GroupBox();
            this.slotStateLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblSlotNoTitle = new System.Windows.Forms.Label();
            this.lblSlotNoValue = new System.Windows.Forms.Label();
            this.btnPrev = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.lifterAxisPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblLifterAxisTitle = new System.Windows.Forms.Label();
            this._lifterPosLabel = new System.Windows.Forms.Label();
            this.lblSlotStateTitle = new System.Windows.Forms.Label();
            this.lblSlotStateValue = new System.Windows.Forms.Label();
            this.btnInit = new System.Windows.Forms.Button();
            this.btnReady = new System.Windows.Forms.Button();
            this.grpLifter = new System.Windows.Forms.GroupBox();
            this.lifterLayout = new System.Windows.Forms.TableLayoutPanel();
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
            this.cassetteLevelLayout = new System.Windows.Forms.TableLayoutPanel();
            this.cassetteSlotView = new QMC.CDT_320.Ui.Controls.CassetteSlotView();
            this.cassetteSlotViewLevel2 = new QMC.CDT_320.Ui.Controls.CassetteSlotView();
            this.grpAction = new System.Windows.Forms.GroupBox();
            this.actionBar = new System.Windows.Forms.TableLayoutPanel();
            this.btnMap = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnLoad = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnUnload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnStop = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.materialDetailView = new QMC.CDT_320.Ui.Controls.MaterialDetailView();
            this.cassetteCheck1Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotCassetteCheck1 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblCassetteCheck1 = new System.Windows.Forms.Label();
            this.cassetteCheck2Panel = new System.Windows.Forms.TableLayoutPanel();
            this.dotCassetteCheck2 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblCassetteCheck2 = new System.Windows.Forms.Label();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.topRow.SuspendLayout();
            this.grpSlotState.SuspendLayout();
            this.slotStateLayout.SuspendLayout();
            this.lifterAxisPanel.SuspendLayout();
            this.grpLifter.SuspendLayout();
            this.lifterLayout.SuspendLayout();
            this.legendReadyPanel.SuspendLayout();
            this.legendEmptyPanel.SuspendLayout();
            this.legendWorkingPanel.SuspendLayout();
            this.legendFinishPanel.SuspendLayout();
            this.legendWorkReadyPanel.SuspendLayout();
            this.cassetteLevelLayout.SuspendLayout();
            this.grpAction.SuspendLayout();
            this.actionBar.SuspendLayout();
            this.cassetteCheck1Panel.SuspendLayout();
            this.cassetteCheck2Panel.SuspendLayout();
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
            this.lblHeader.Tag = "i18n:wi.inputCassette";
            this.lblHeader.Text = "INPUT CASSETTE";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.Controls.Add(this.topRow, 0, 0);
            this.contentLayout.Controls.Add(this.grpLifter, 0, 1);
            this.contentLayout.Controls.Add(this.grpAction, 0, 2);
            this.contentLayout.Controls.Add(this.materialDetailView, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(0, 30);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.RowCount = 3;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 116F));
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.contentLayout.Size = new System.Drawing.Size(1678, 870);
            this.contentLayout.TabIndex = 2;
            // 
            // topRow
            // 
            this.topRow.ColumnCount = 1;
            this.topRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topRow.Controls.Add(this.grpSlotState, 0, 0);
            this.topRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topRow.Location = new System.Drawing.Point(0, 0);
            this.topRow.Margin = new System.Windows.Forms.Padding(0, 0, 3, 3);
            this.topRow.Name = "topRow";
            this.topRow.RowCount = 1;
            this.topRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topRow.Size = new System.Drawing.Size(836, 113);
            this.topRow.TabIndex = 1;
            // 
            // grpSlotState
            // 
            this.grpSlotState.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpSlotState.Controls.Add(this.slotStateLayout);
            this.grpSlotState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSlotState.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpSlotState.Location = new System.Drawing.Point(0, 0);
            this.grpSlotState.Margin = new System.Windows.Forms.Padding(0);
            this.grpSlotState.Name = "grpSlotState";
            this.grpSlotState.Size = new System.Drawing.Size(836, 113);
            this.grpSlotState.TabIndex = 0;
            this.grpSlotState.TabStop = false;
            this.grpSlotState.Text = "SLOT STATE";
            // 
            // slotStateLayout
            // 
            this.slotStateLayout.ColumnCount = 5;
            this.slotStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16F));
            this.slotStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23F));
            this.slotStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 19F));
            this.slotStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 19F));
            this.slotStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23F));
            this.slotStateLayout.Controls.Add(this.lblSlotNoTitle, 0, 0);
            this.slotStateLayout.Controls.Add(this.lblSlotNoValue, 1, 0);
            this.slotStateLayout.Controls.Add(this.btnPrev, 2, 0);
            this.slotStateLayout.Controls.Add(this.btnNext, 3, 0);
            this.slotStateLayout.Controls.Add(this.lifterAxisPanel, 4, 0);
            this.slotStateLayout.Controls.Add(this.lblSlotStateTitle, 0, 1);
            this.slotStateLayout.Controls.Add(this.lblSlotStateValue, 1, 1);
            this.slotStateLayout.Controls.Add(this.btnInit, 2, 1);
            this.slotStateLayout.Controls.Add(this.btnReady, 3, 1);
            this.slotStateLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.slotStateLayout.Location = new System.Drawing.Point(3, 23);
            this.slotStateLayout.Name = "slotStateLayout";
            this.slotStateLayout.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.slotStateLayout.RowCount = 2;
            this.slotStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.slotStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.slotStateLayout.Size = new System.Drawing.Size(830, 87);
            this.slotStateLayout.TabIndex = 0;
            // 
            // lblSlotNoTitle
            // 
            this.lblSlotNoTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSlotNoTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblSlotNoTitle.Location = new System.Drawing.Point(9, 4);
            this.lblSlotNoTitle.Name = "lblSlotNoTitle";
            this.lblSlotNoTitle.Size = new System.Drawing.Size(124, 39);
            this.lblSlotNoTitle.TabIndex = 0;
            this.lblSlotNoTitle.Text = "Slot No";
            this.lblSlotNoTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSlotNoValue
            // 
            this.lblSlotNoValue.BackColor = System.Drawing.SystemColors.Control;
            this.lblSlotNoValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSlotNoValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblSlotNoValue.Location = new System.Drawing.Point(139, 4);
            this.lblSlotNoValue.Name = "lblSlotNoValue";
            this.lblSlotNoValue.Size = new System.Drawing.Size(182, 39);
            this.lblSlotNoValue.TabIndex = 1;
            this.lblSlotNoValue.Text = "BIN 1";
            this.lblSlotNoValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnPrev
            // 
            this.btnPrev.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPrev.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrev.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnPrev.Location = new System.Drawing.Point(327, 7);
            this.btnPrev.Name = "btnPrev";
            this.btnPrev.Size = new System.Drawing.Size(149, 33);
            this.btnPrev.TabIndex = 4;
            this.btnPrev.Text = "PREV";
            // 
            // btnNext
            // 
            this.btnNext.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNext.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnNext.Location = new System.Drawing.Point(482, 7);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(149, 33);
            this.btnNext.TabIndex = 5;
            this.btnNext.Text = "NEXT";
            // 
            // lifterAxisPanel
            // 
            this.lifterAxisPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lifterAxisPanel.ColumnCount = 1;
            this.lifterAxisPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.lifterAxisPanel.Controls.Add(this.lblLifterAxisTitle, 0, 0);
            this.lifterAxisPanel.Controls.Add(this._lifterPosLabel, 0, 1);
            this.lifterAxisPanel.Location = new System.Drawing.Point(638, 8);
            this.lifterAxisPanel.Margin = new System.Windows.Forms.Padding(4);
            this.lifterAxisPanel.Name = "lifterAxisPanel";
            this.lifterAxisPanel.RowCount = 2;
            this.slotStateLayout.SetRowSpan(this.lifterAxisPanel, 2);
            this.lifterAxisPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.lifterAxisPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lifterAxisPanel.Size = new System.Drawing.Size(182, 71);
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
            this.lblLifterAxisTitle.Size = new System.Drawing.Size(176, 24);
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
            this._lifterPosLabel.Size = new System.Drawing.Size(176, 47);
            this._lifterPosLabel.TabIndex = 1;
            this._lifterPosLabel.Text = "0.000 mm";
            this._lifterPosLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblSlotStateTitle
            // 
            this.lblSlotStateTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSlotStateTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblSlotStateTitle.Location = new System.Drawing.Point(9, 43);
            this.lblSlotStateTitle.Name = "lblSlotStateTitle";
            this.lblSlotStateTitle.Size = new System.Drawing.Size(124, 40);
            this.lblSlotStateTitle.TabIndex = 2;
            this.lblSlotStateTitle.Text = "State";
            this.lblSlotStateTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSlotStateValue
            // 
            this.lblSlotStateValue.BackColor = System.Drawing.SystemColors.Control;
            this.lblSlotStateValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSlotStateValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblSlotStateValue.Location = new System.Drawing.Point(139, 43);
            this.lblSlotStateValue.Name = "lblSlotStateValue";
            this.lblSlotStateValue.Size = new System.Drawing.Size(182, 40);
            this.lblSlotStateValue.TabIndex = 3;
            this.lblSlotStateValue.Text = "EMPTY";
            this.lblSlotStateValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnInit
            // 
            this.btnInit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInit.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnInit.Location = new System.Drawing.Point(327, 46);
            this.btnInit.Name = "btnInit";
            this.btnInit.Size = new System.Drawing.Size(149, 34);
            this.btnInit.TabIndex = 6;
            this.btnInit.Text = "LIFTER INIT";
            // 
            // btnReady
            // 
            this.btnReady.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReady.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReady.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnReady.Location = new System.Drawing.Point(482, 46);
            this.btnReady.Name = "btnReady";
            this.btnReady.Size = new System.Drawing.Size(149, 34);
            this.btnReady.TabIndex = 7;
            this.btnReady.Text = "LIFTER READY";
            // 
            // grpLifter
            // 
            this.grpLifter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpLifter.Controls.Add(this.lifterLayout);
            this.grpLifter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLifter.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpLifter.Location = new System.Drawing.Point(0, 119);
            this.grpLifter.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.grpLifter.Name = "grpLifter";
            this.grpLifter.Size = new System.Drawing.Size(836, 588);
            this.grpLifter.TabIndex = 1;
            this.grpLifter.TabStop = false;
            this.grpLifter.Text = "LIFTER";
            // 
            // lifterLayout
            // 
            this.lifterLayout.ColumnCount = 5;
            this.lifterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.lifterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.lifterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.lifterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.lifterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.lifterLayout.Controls.Add(this.legendReadyPanel, 0, 0);
            this.lifterLayout.Controls.Add(this.legendEmptyPanel, 1, 0);
            this.lifterLayout.Controls.Add(this.legendWorkingPanel, 2, 0);
            this.lifterLayout.Controls.Add(this.legendFinishPanel, 3, 0);
            this.lifterLayout.Controls.Add(this.legendWorkReadyPanel, 4, 0);
            this.lifterLayout.Controls.Add(this.cassetteLevelLayout, 0, 1);
            this.lifterLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lifterLayout.Location = new System.Drawing.Point(3, 23);
            this.lifterLayout.Margin = new System.Windows.Forms.Padding(0);
            this.lifterLayout.Name = "lifterLayout";
            this.lifterLayout.Padding = new System.Windows.Forms.Padding(3);
            this.lifterLayout.RowCount = 2;
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 21F));
            this.lifterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lifterLayout.Size = new System.Drawing.Size(830, 562);
            this.lifterLayout.TabIndex = 0;
            // 
            // legendReadyPanel
            // 
            this.legendReadyPanel.ColumnCount = 2;
            this.legendReadyPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.legendReadyPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendReadyPanel.Controls.Add(this.lblLegendReadyColor, 0, 0);
            this.legendReadyPanel.Controls.Add(this.lblLegendReadyText, 1, 0);
            this.legendReadyPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.legendReadyPanel.Location = new System.Drawing.Point(6, 6);
            this.legendReadyPanel.Name = "legendReadyPanel";
            this.legendReadyPanel.RowCount = 1;
            this.legendReadyPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendReadyPanel.Size = new System.Drawing.Size(158, 15);
            this.legendReadyPanel.TabIndex = 0;
            // 
            // lblLegendReadyColor
            // 
            this.lblLegendReadyColor.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblLegendReadyColor.BackColor = System.Drawing.Color.Cyan;
            this.lblLegendReadyColor.Location = new System.Drawing.Point(9, 1);
            this.lblLegendReadyColor.Margin = new System.Windows.Forms.Padding(0);
            this.lblLegendReadyColor.Name = "lblLegendReadyColor";
            this.lblLegendReadyColor.Size = new System.Drawing.Size(12, 12);
            this.lblLegendReadyColor.TabIndex = 0;
            // 
            // lblLegendReadyText
            // 
            this.lblLegendReadyText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendReadyText.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLegendReadyText.Location = new System.Drawing.Point(33, 0);
            this.lblLegendReadyText.Name = "lblLegendReadyText";
            this.lblLegendReadyText.Size = new System.Drawing.Size(122, 15);
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
            this.legendEmptyPanel.Location = new System.Drawing.Point(170, 6);
            this.legendEmptyPanel.Name = "legendEmptyPanel";
            this.legendEmptyPanel.RowCount = 1;
            this.legendEmptyPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendEmptyPanel.Size = new System.Drawing.Size(158, 15);
            this.legendEmptyPanel.TabIndex = 1;
            // 
            // lblLegendEmptyColor
            // 
            this.lblLegendEmptyColor.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblLegendEmptyColor.BackColor = System.Drawing.Color.Lime;
            this.lblLegendEmptyColor.Location = new System.Drawing.Point(9, 1);
            this.lblLegendEmptyColor.Margin = new System.Windows.Forms.Padding(0);
            this.lblLegendEmptyColor.Name = "lblLegendEmptyColor";
            this.lblLegendEmptyColor.Size = new System.Drawing.Size(12, 12);
            this.lblLegendEmptyColor.TabIndex = 0;
            // 
            // lblLegendEmptyText
            // 
            this.lblLegendEmptyText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendEmptyText.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLegendEmptyText.Location = new System.Drawing.Point(33, 0);
            this.lblLegendEmptyText.Name = "lblLegendEmptyText";
            this.lblLegendEmptyText.Size = new System.Drawing.Size(122, 15);
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
            this.legendWorkingPanel.Location = new System.Drawing.Point(334, 6);
            this.legendWorkingPanel.Name = "legendWorkingPanel";
            this.legendWorkingPanel.RowCount = 1;
            this.legendWorkingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendWorkingPanel.Size = new System.Drawing.Size(158, 15);
            this.legendWorkingPanel.TabIndex = 2;
            // 
            // lblLegendWorkingColor
            // 
            this.lblLegendWorkingColor.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblLegendWorkingColor.BackColor = System.Drawing.Color.Orange;
            this.lblLegendWorkingColor.Location = new System.Drawing.Point(9, 1);
            this.lblLegendWorkingColor.Margin = new System.Windows.Forms.Padding(0);
            this.lblLegendWorkingColor.Name = "lblLegendWorkingColor";
            this.lblLegendWorkingColor.Size = new System.Drawing.Size(12, 12);
            this.lblLegendWorkingColor.TabIndex = 0;
            // 
            // lblLegendWorkingText
            // 
            this.lblLegendWorkingText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendWorkingText.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLegendWorkingText.Location = new System.Drawing.Point(33, 0);
            this.lblLegendWorkingText.Name = "lblLegendWorkingText";
            this.lblLegendWorkingText.Size = new System.Drawing.Size(122, 15);
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
            this.legendFinishPanel.Location = new System.Drawing.Point(498, 6);
            this.legendFinishPanel.Name = "legendFinishPanel";
            this.legendFinishPanel.RowCount = 1;
            this.legendFinishPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendFinishPanel.Size = new System.Drawing.Size(158, 15);
            this.legendFinishPanel.TabIndex = 3;
            // 
            // lblLegendFinishColor
            // 
            this.lblLegendFinishColor.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblLegendFinishColor.BackColor = System.Drawing.Color.Red;
            this.lblLegendFinishColor.Location = new System.Drawing.Point(9, 1);
            this.lblLegendFinishColor.Margin = new System.Windows.Forms.Padding(0);
            this.lblLegendFinishColor.Name = "lblLegendFinishColor";
            this.lblLegendFinishColor.Size = new System.Drawing.Size(12, 12);
            this.lblLegendFinishColor.TabIndex = 0;
            // 
            // lblLegendFinishText
            // 
            this.lblLegendFinishText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendFinishText.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLegendFinishText.Location = new System.Drawing.Point(33, 0);
            this.lblLegendFinishText.Name = "lblLegendFinishText";
            this.lblLegendFinishText.Size = new System.Drawing.Size(122, 15);
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
            this.legendWorkReadyPanel.Location = new System.Drawing.Point(662, 6);
            this.legendWorkReadyPanel.Name = "legendWorkReadyPanel";
            this.legendWorkReadyPanel.RowCount = 1;
            this.legendWorkReadyPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendWorkReadyPanel.Size = new System.Drawing.Size(162, 15);
            this.legendWorkReadyPanel.TabIndex = 4;
            // 
            // lblLegendWorkReadyColor
            // 
            this.lblLegendWorkReadyColor.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblLegendWorkReadyColor.BackColor = System.Drawing.Color.Navy;
            this.lblLegendWorkReadyColor.Location = new System.Drawing.Point(9, 1);
            this.lblLegendWorkReadyColor.Margin = new System.Windows.Forms.Padding(0);
            this.lblLegendWorkReadyColor.Name = "lblLegendWorkReadyColor";
            this.lblLegendWorkReadyColor.Size = new System.Drawing.Size(12, 12);
            this.lblLegendWorkReadyColor.TabIndex = 0;
            // 
            // lblLegendWorkReadyText
            // 
            this.lblLegendWorkReadyText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendWorkReadyText.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLegendWorkReadyText.Location = new System.Drawing.Point(33, 0);
            this.lblLegendWorkReadyText.Name = "lblLegendWorkReadyText";
            this.lblLegendWorkReadyText.Size = new System.Drawing.Size(126, 15);
            this.lblLegendWorkReadyText.TabIndex = 1;
            this.lblLegendWorkReadyText.Text = "WORK READY";
            this.lblLegendWorkReadyText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cassetteLevelLayout
            // 
            this.cassetteLevelLayout.ColumnCount = 2;
            this.lifterLayout.SetColumnSpan(this.cassetteLevelLayout, 5);
            this.cassetteLevelLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cassetteLevelLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 0F));
            this.cassetteLevelLayout.Controls.Add(this.cassetteSlotView, 0, 0);
            this.cassetteLevelLayout.Controls.Add(this.cassetteSlotViewLevel2, 1, 0);
            this.cassetteLevelLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cassetteLevelLayout.Location = new System.Drawing.Point(6, 27);
            this.cassetteLevelLayout.Name = "cassetteLevelLayout";
            this.cassetteLevelLayout.RowCount = 1;
            this.cassetteLevelLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cassetteLevelLayout.Size = new System.Drawing.Size(818, 529);
            this.cassetteLevelLayout.TabIndex = 0;
            // 
            // cassetteSlotView
            // 
            this.cassetteSlotView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.cassetteSlotView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cassetteSlotView.EmptyColor = System.Drawing.Color.LightGray;
            this.cassetteSlotView.Location = new System.Drawing.Point(0, 0);
            this.cassetteSlotView.Margin = new System.Windows.Forms.Padding(0);
            this.cassetteSlotView.Name = "cassetteSlotView";
            this.cassetteSlotView.Size = new System.Drawing.Size(818, 529);
            this.cassetteSlotView.TabIndex = 0;
            this.cassetteSlotView.Title = "INPUT CASSETTE 1단";
            // 
            // cassetteSlotViewLevel2
            // 
            this.cassetteSlotViewLevel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.cassetteSlotViewLevel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cassetteSlotViewLevel2.EmptyColor = System.Drawing.Color.LightGray;
            this.cassetteSlotViewLevel2.Location = new System.Drawing.Point(818, 0);
            this.cassetteSlotViewLevel2.Margin = new System.Windows.Forms.Padding(0);
            this.cassetteSlotViewLevel2.Name = "cassetteSlotViewLevel2";
            this.cassetteSlotViewLevel2.Size = new System.Drawing.Size(1, 529);
            this.cassetteSlotViewLevel2.TabIndex = 1;
            this.cassetteSlotViewLevel2.Title = "INPUT CASSETTE 2단";
            // 
            // grpAction
            // 
            this.grpAction.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpAction.Controls.Add(this.actionBar);
            this.grpAction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAction.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpAction.Location = new System.Drawing.Point(0, 713);
            this.grpAction.Margin = new System.Windows.Forms.Padding(0, 3, 3, 0);
            this.grpAction.Name = "grpAction";
            this.grpAction.Size = new System.Drawing.Size(836, 157);
            this.grpAction.TabIndex = 2;
            this.grpAction.TabStop = false;
            this.grpAction.Text = "ACTION";
            // 
            // actionBar
            // 
            this.actionBar.ColumnCount = 2;
            this.actionBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionBar.Controls.Add(this.btnMap, 0, 0);
            this.actionBar.Controls.Add(this.btnLoad, 1, 0);
            this.actionBar.Controls.Add(this.btnUnload, 0, 1);
            this.actionBar.Controls.Add(this.btnStop, 1, 1);
            this.actionBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.actionBar.Location = new System.Drawing.Point(3, 23);
            this.actionBar.Margin = new System.Windows.Forms.Padding(0);
            this.actionBar.Name = "actionBar";
            this.actionBar.RowCount = 2;
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBar.Size = new System.Drawing.Size(830, 92);
            this.actionBar.TabIndex = 0;
            // 
            // btnMap
            // 
            this.btnMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnMap.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMap.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnMap.ForeColor = System.Drawing.Color.White;
            this.btnMap.Location = new System.Drawing.Point(3, 3);
            this.btnMap.Name = "btnMap";
            this.btnMap.Size = new System.Drawing.Size(409, 40);
            this.btnMap.TabIndex = 0;
            this.btnMap.Tag = "i18n:wi.liftWaferMapping";
            this.btnMap.Text = "LIFT WAFER MAPPING";
            // 
            // btnLoad
            // 
            this.btnLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnLoad.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoad.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnLoad.ForeColor = System.Drawing.Color.White;
            this.btnLoad.Location = new System.Drawing.Point(418, 3);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(409, 40);
            this.btnLoad.TabIndex = 1;
            this.btnLoad.Tag = "i18n:wi.liftWaferLoading";
            this.btnLoad.Text = "LIFT WAFER LOADING";
            // 
            // btnUnload
            // 
            this.btnUnload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnUnload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUnload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUnload.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnUnload.ForeColor = System.Drawing.Color.White;
            this.btnUnload.Location = new System.Drawing.Point(3, 49);
            this.btnUnload.Name = "btnUnload";
            this.btnUnload.Size = new System.Drawing.Size(409, 40);
            this.btnUnload.TabIndex = 2;
            this.btnUnload.Tag = "i18n:wi.liftWaferUnloading";
            this.btnUnload.Text = "LIFT WAFER UNLOADING";
            // 
            // btnStop
            // 
            this.btnStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(214)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStop.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.Location = new System.Drawing.Point(418, 49);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(409, 40);
            this.btnStop.TabIndex = 3;
            this.btnStop.Text = "STOP";
            // 
            // materialDetailView
            // 
            this.materialDetailView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.materialDetailView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialDetailView.Location = new System.Drawing.Point(842, 0);
            this.materialDetailView.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.materialDetailView.Name = "materialDetailView";
            this.contentLayout.SetRowSpan(this.materialDetailView, 3);
            this.materialDetailView.ShowProcessTestDataButton = false;
            this.materialDetailView.Size = new System.Drawing.Size(836, 870);
            this.materialDetailView.TabIndex = 2;
            // 
            // cassetteCheck1Panel
            // 
            this.cassetteCheck1Panel.ColumnCount = 2;
            this.cassetteCheck1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.cassetteCheck1Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cassetteCheck1Panel.Controls.Add(this.dotCassetteCheck1, 0, 0);
            this.cassetteCheck1Panel.Controls.Add(this.lblCassetteCheck1, 1, 0);
            this.cassetteCheck1Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cassetteCheck1Panel.Location = new System.Drawing.Point(252, 12);
            this.cassetteCheck1Panel.Margin = new System.Windows.Forms.Padding(4);
            this.cassetteCheck1Panel.Name = "cassetteCheck1Panel";
            this.cassetteCheck1Panel.RowCount = 1;
            this.cassetteCheck1Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cassetteCheck1Panel.Size = new System.Drawing.Size(242, 80);
            this.cassetteCheck1Panel.TabIndex = 1;
            // 
            // dotCassetteCheck1
            // 
            this.dotCassetteCheck1.BackColor = System.Drawing.Color.Transparent;
            this.dotCassetteCheck1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotCassetteCheck1.Location = new System.Drawing.Point(10, 20);
            this.dotCassetteCheck1.Margin = new System.Windows.Forms.Padding(10, 20, 10, 20);
            this.dotCassetteCheck1.Name = "dotCassetteCheck1";
            this.dotCassetteCheck1.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotCassetteCheck1.OnColor = System.Drawing.Color.LimeGreen;
            this.dotCassetteCheck1.Size = new System.Drawing.Size(26, 40);
            this.dotCassetteCheck1.TabIndex = 0;
            // 
            // lblCassetteCheck1
            // 
            this.lblCassetteCheck1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblCassetteCheck1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCassetteCheck1.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblCassetteCheck1.Location = new System.Drawing.Point(49, 0);
            this.lblCassetteCheck1.Name = "lblCassetteCheck1";
            this.lblCassetteCheck1.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblCassetteCheck1.Size = new System.Drawing.Size(190, 80);
            this.lblCassetteCheck1.TabIndex = 1;
            this.lblCassetteCheck1.Text = "1단 사용";
            this.lblCassetteCheck1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cassetteCheck2Panel
            // 
            this.cassetteCheck2Panel.ColumnCount = 2;
            this.cassetteCheck2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.cassetteCheck2Panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cassetteCheck2Panel.Controls.Add(this.dotCassetteCheck2, 0, 0);
            this.cassetteCheck2Panel.Controls.Add(this.lblCassetteCheck2, 1, 0);
            this.cassetteCheck2Panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cassetteCheck2Panel.Location = new System.Drawing.Point(502, 12);
            this.cassetteCheck2Panel.Margin = new System.Windows.Forms.Padding(4);
            this.cassetteCheck2Panel.Name = "cassetteCheck2Panel";
            this.cassetteCheck2Panel.RowCount = 1;
            this.cassetteCheck2Panel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cassetteCheck2Panel.Size = new System.Drawing.Size(242, 80);
            this.cassetteCheck2Panel.TabIndex = 2;
            // 
            // dotCassetteCheck2
            // 
            this.dotCassetteCheck2.BackColor = System.Drawing.Color.Transparent;
            this.dotCassetteCheck2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotCassetteCheck2.Location = new System.Drawing.Point(10, 20);
            this.dotCassetteCheck2.Margin = new System.Windows.Forms.Padding(10, 20, 10, 20);
            this.dotCassetteCheck2.Name = "dotCassetteCheck2";
            this.dotCassetteCheck2.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotCassetteCheck2.OnColor = System.Drawing.Color.LimeGreen;
            this.dotCassetteCheck2.Size = new System.Drawing.Size(26, 40);
            this.dotCassetteCheck2.TabIndex = 0;
            // 
            // lblCassetteCheck2
            // 
            this.lblCassetteCheck2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblCassetteCheck2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCassetteCheck2.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblCassetteCheck2.Location = new System.Drawing.Point(49, 0);
            this.lblCassetteCheck2.Name = "lblCassetteCheck2";
            this.lblCassetteCheck2.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblCassetteCheck2.Size = new System.Drawing.Size(190, 80);
            this.lblCassetteCheck2.TabIndex = 1;
            this.lblCassetteCheck2.Text = "2단 사용";
            this.lblCassetteCheck2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // InputCassettePage
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.Controls.Add(this.rootLayout);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "InputCassettePage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.topRow.ResumeLayout(false);
            this.grpSlotState.ResumeLayout(false);
            this.slotStateLayout.ResumeLayout(false);
            this.lifterAxisPanel.ResumeLayout(false);
            this.grpLifter.ResumeLayout(false);
            this.lifterLayout.ResumeLayout(false);
            this.legendReadyPanel.ResumeLayout(false);
            this.legendEmptyPanel.ResumeLayout(false);
            this.legendWorkingPanel.ResumeLayout(false);
            this.legendFinishPanel.ResumeLayout(false);
            this.legendWorkReadyPanel.ResumeLayout(false);
            this.cassetteLevelLayout.ResumeLayout(false);
            this.grpAction.ResumeLayout(false);
            this.actionBar.ResumeLayout(false);
            this.cassetteCheck1Panel.ResumeLayout(false);
            this.cassetteCheck2Panel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}


