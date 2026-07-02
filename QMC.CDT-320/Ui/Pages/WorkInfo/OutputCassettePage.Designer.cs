using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class OutputCassettePage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private GroupBox grpSlotState;
        private GroupBox grpLifter;
        private GroupBox grpAction;
        private TableLayoutPanel slotStateLayout;
        private TableLayoutPanel sensorsPanel;
        private TableLayoutPanel lifterContentLayout;
        private TableLayoutPanel cassetteLevelLayout;
        private MaterialDetailView materialDetailView;
        private CassetteSlotView _ngCassetteView;
        private CassetteSlotView _good1CassetteView;
        private CassetteSlotView _good2CassetteView;
        private TableLayoutPanel actionBar;
        private TableLayoutPanel lifterAxisPanel;
        private Label lblLifterAxisTitle;
        private Label lblElevatorPos;
        private TableLayoutPanel good1CheckPanel;
        private IndicatorDot dotGood1Check;
        private Label lblGood1Check;
        private TableLayoutPanel good2CheckPanel;
        private IndicatorDot dotGood2Check;
        private Label lblGood2Check;
        private TableLayoutPanel ngCheckPanel;
        private IndicatorDot dotNgCheck;
        private Label lblNgCheck;
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
            this.grpSlotState = new System.Windows.Forms.GroupBox();
            this.slotStateLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblSlotNoTitle = new System.Windows.Forms.Label();
            this.lblSlotNoValue = new System.Windows.Forms.Label();
            this.btnPrev = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.sensorsPanel = new System.Windows.Forms.TableLayoutPanel();
            this.good1CheckPanel = new System.Windows.Forms.TableLayoutPanel();
            this.dotGood1Check = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblGood1Check = new System.Windows.Forms.Label();
            this.good2CheckPanel = new System.Windows.Forms.TableLayoutPanel();
            this.dotGood2Check = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblGood2Check = new System.Windows.Forms.Label();
            this.ngCheckPanel = new System.Windows.Forms.TableLayoutPanel();
            this.dotNgCheck = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblNgCheck = new System.Windows.Forms.Label();
            this.lifterAxisPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblLifterAxisTitle = new System.Windows.Forms.Label();
            this.lblElevatorPos = new System.Windows.Forms.Label();
            this.lblSlotStateTitle = new System.Windows.Forms.Label();
            this.lblSlotStateValue = new System.Windows.Forms.Label();
            this.btnInit = new System.Windows.Forms.Button();
            this.btnReady = new System.Windows.Forms.Button();
            this.grpLifter = new System.Windows.Forms.GroupBox();
            this.lifterContentLayout = new System.Windows.Forms.TableLayoutPanel();
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
            this.cassetteLevelLayout = new System.Windows.Forms.TableLayoutPanel();
            this._good1CassetteView = new QMC.CDT_320.Ui.Controls.CassetteSlotView();
            this._good2CassetteView = new QMC.CDT_320.Ui.Controls.CassetteSlotView();
            this._ngCassetteView = new QMC.CDT_320.Ui.Controls.CassetteSlotView();
            this.grpAction = new System.Windows.Forms.GroupBox();
            this.actionBar = new System.Windows.Forms.TableLayoutPanel();
            this.btnMap = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnLoad = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnUnload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnStop = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.materialDetailView = new QMC.CDT_320.Ui.Controls.MaterialDetailView();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.grpSlotState.SuspendLayout();
            this.slotStateLayout.SuspendLayout();
            this.sensorsPanel.SuspendLayout();
            this.good1CheckPanel.SuspendLayout();
            this.good2CheckPanel.SuspendLayout();
            this.ngCheckPanel.SuspendLayout();
            this.lifterAxisPanel.SuspendLayout();
            this.grpLifter.SuspendLayout();
            this.lifterContentLayout.SuspendLayout();
            this.legendLayout.SuspendLayout();
            this.legendReadyPanel.SuspendLayout();
            this.legendEmptyPanel.SuspendLayout();
            this.legendWorkingPanel.SuspendLayout();
            this.legendFinishPanel.SuspendLayout();
            this.legendWorkReadyPanel.SuspendLayout();
            this.cassetteLevelLayout.SuspendLayout();
            this.grpAction.SuspendLayout();
            this.actionBar.SuspendLayout();
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
            this.lblHeader.Tag = "i18n:wi.outputCassette";
            this.lblHeader.Text = "OUTPUT CASSETTE";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.Controls.Add(this.grpSlotState, 0, 0);
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
            // grpSlotState
            // 
            this.grpSlotState.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpSlotState.Controls.Add(this.slotStateLayout);
            this.grpSlotState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSlotState.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpSlotState.Location = new System.Drawing.Point(0, 0);
            this.grpSlotState.Margin = new System.Windows.Forms.Padding(0, 0, 3, 3);
            this.grpSlotState.Name = "grpSlotState";
            this.grpSlotState.Size = new System.Drawing.Size(836, 113);
            this.grpSlotState.TabIndex = 0;
            this.grpSlotState.TabStop = false;
            this.grpSlotState.Text = "SLOT STATE";
            // 
            // slotStateLayout
            // 
            this.slotStateLayout.ColumnCount = 6;
            this.slotStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15.89242F));
            this.slotStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 22.98289F));
            this.slotStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 13F));
            this.slotStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 13F));
            this.slotStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.slotStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23F));
            this.slotStateLayout.Controls.Add(this.lblSlotNoTitle, 0, 0);
            this.slotStateLayout.Controls.Add(this.lblSlotNoValue, 1, 0);
            this.slotStateLayout.Controls.Add(this.btnPrev, 2, 0);
            this.slotStateLayout.Controls.Add(this.btnNext, 3, 0);
            this.slotStateLayout.Controls.Add(this.sensorsPanel, 4, 0);
            this.slotStateLayout.Controls.Add(this.lifterAxisPanel, 5, 0);
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
            this.lblSlotNoValue.Text = "GOOD1 / -";
            this.lblSlotNoValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnPrev
            // 
            this.btnPrev.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPrev.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrev.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnPrev.Location = new System.Drawing.Point(327, 7);
            this.btnPrev.Name = "btnPrev";
            this.btnPrev.Size = new System.Drawing.Size(100, 33);
            this.btnPrev.TabIndex = 4;
            this.btnPrev.Text = "PREV";
            // 
            // btnNext
            // 
            this.btnNext.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNext.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnNext.Location = new System.Drawing.Point(433, 7);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(100, 33);
            this.btnNext.TabIndex = 5;
            this.btnNext.Text = "NEXT";
            // 
            // sensorsPanel
            // 
            this.sensorsPanel.ColumnCount = 1;
            this.sensorsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorsPanel.Controls.Add(this.good1CheckPanel, 0, 0);
            this.sensorsPanel.Controls.Add(this.good2CheckPanel, 0, 1);
            this.sensorsPanel.Controls.Add(this.ngCheckPanel, 0, 2);
            this.sensorsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorsPanel.Location = new System.Drawing.Point(539, 4);
            this.sensorsPanel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.sensorsPanel.Name = "sensorsPanel";
            this.sensorsPanel.RowCount = 3;
            this.slotStateLayout.SetRowSpan(this.sensorsPanel, 2);
            this.sensorsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.sensorsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.sensorsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.sensorsPanel.Size = new System.Drawing.Size(92, 79);
            this.sensorsPanel.TabIndex = 8;
            // 
            // good1CheckPanel
            // 
            this.good1CheckPanel.ColumnCount = 2;
            this.good1CheckPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.good1CheckPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.good1CheckPanel.Controls.Add(this.dotGood1Check, 0, 0);
            this.good1CheckPanel.Controls.Add(this.lblGood1Check, 1, 0);
            this.good1CheckPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.good1CheckPanel.Location = new System.Drawing.Point(1, 1);
            this.good1CheckPanel.Margin = new System.Windows.Forms.Padding(1);
            this.good1CheckPanel.Name = "good1CheckPanel";
            this.good1CheckPanel.RowCount = 1;
            this.good1CheckPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.good1CheckPanel.Size = new System.Drawing.Size(90, 24);
            this.good1CheckPanel.TabIndex = 0;
            // 
            // dotGood1Check
            // 
            this.dotGood1Check.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotGood1Check.BackColor = System.Drawing.Color.Transparent;
            this.dotGood1Check.Location = new System.Drawing.Point(2, 6);
            this.dotGood1Check.Margin = new System.Windows.Forms.Padding(0);
            this.dotGood1Check.Name = "dotGood1Check";
            this.dotGood1Check.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotGood1Check.OnColor = System.Drawing.Color.LimeGreen;
            this.dotGood1Check.Size = new System.Drawing.Size(12, 12);
            this.dotGood1Check.TabIndex = 0;
            // 
            // lblGood1Check
            // 
            this.lblGood1Check.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblGood1Check.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGood1Check.Font = new System.Drawing.Font("Consolas", 8F);
            this.lblGood1Check.Location = new System.Drawing.Point(19, 0);
            this.lblGood1Check.Name = "lblGood1Check";
            this.lblGood1Check.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.lblGood1Check.Size = new System.Drawing.Size(68, 24);
            this.lblGood1Check.TabIndex = 1;
            this.lblGood1Check.Text = "GOOD 1단";
            this.lblGood1Check.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // good2CheckPanel
            // 
            this.good2CheckPanel.ColumnCount = 2;
            this.good2CheckPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.good2CheckPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.good2CheckPanel.Controls.Add(this.dotGood2Check, 0, 0);
            this.good2CheckPanel.Controls.Add(this.lblGood2Check, 1, 0);
            this.good2CheckPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.good2CheckPanel.Location = new System.Drawing.Point(1, 27);
            this.good2CheckPanel.Margin = new System.Windows.Forms.Padding(1);
            this.good2CheckPanel.Name = "good2CheckPanel";
            this.good2CheckPanel.RowCount = 1;
            this.good2CheckPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.good2CheckPanel.Size = new System.Drawing.Size(90, 24);
            this.good2CheckPanel.TabIndex = 1;
            // 
            // dotGood2Check
            // 
            this.dotGood2Check.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotGood2Check.BackColor = System.Drawing.Color.Transparent;
            this.dotGood2Check.Location = new System.Drawing.Point(2, 6);
            this.dotGood2Check.Margin = new System.Windows.Forms.Padding(0);
            this.dotGood2Check.Name = "dotGood2Check";
            this.dotGood2Check.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotGood2Check.OnColor = System.Drawing.Color.LimeGreen;
            this.dotGood2Check.Size = new System.Drawing.Size(12, 12);
            this.dotGood2Check.TabIndex = 0;
            // 
            // lblGood2Check
            // 
            this.lblGood2Check.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblGood2Check.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGood2Check.Font = new System.Drawing.Font("Consolas", 8F);
            this.lblGood2Check.Location = new System.Drawing.Point(19, 0);
            this.lblGood2Check.Name = "lblGood2Check";
            this.lblGood2Check.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.lblGood2Check.Size = new System.Drawing.Size(68, 24);
            this.lblGood2Check.TabIndex = 1;
            this.lblGood2Check.Text = "GOOD 2단";
            this.lblGood2Check.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ngCheckPanel
            // 
            this.ngCheckPanel.ColumnCount = 2;
            this.ngCheckPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.ngCheckPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ngCheckPanel.Controls.Add(this.dotNgCheck, 0, 0);
            this.ngCheckPanel.Controls.Add(this.lblNgCheck, 1, 0);
            this.ngCheckPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ngCheckPanel.Location = new System.Drawing.Point(1, 53);
            this.ngCheckPanel.Margin = new System.Windows.Forms.Padding(1);
            this.ngCheckPanel.Name = "ngCheckPanel";
            this.ngCheckPanel.RowCount = 1;
            this.ngCheckPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ngCheckPanel.Size = new System.Drawing.Size(90, 25);
            this.ngCheckPanel.TabIndex = 2;
            // 
            // dotNgCheck
            // 
            this.dotNgCheck.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotNgCheck.BackColor = System.Drawing.Color.Transparent;
            this.dotNgCheck.Location = new System.Drawing.Point(2, 6);
            this.dotNgCheck.Margin = new System.Windows.Forms.Padding(0);
            this.dotNgCheck.Name = "dotNgCheck";
            this.dotNgCheck.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotNgCheck.OnColor = System.Drawing.Color.LimeGreen;
            this.dotNgCheck.Size = new System.Drawing.Size(12, 12);
            this.dotNgCheck.TabIndex = 0;
            // 
            // lblNgCheck
            // 
            this.lblNgCheck.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblNgCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgCheck.Font = new System.Drawing.Font("Consolas", 8F);
            this.lblNgCheck.Location = new System.Drawing.Point(19, 0);
            this.lblNgCheck.Name = "lblNgCheck";
            this.lblNgCheck.Padding = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.lblNgCheck.Size = new System.Drawing.Size(68, 25);
            this.lblNgCheck.TabIndex = 1;
            this.lblNgCheck.Text = "NG";
            this.lblNgCheck.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lifterAxisPanel
            // 
            this.lifterAxisPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lifterAxisPanel.ColumnCount = 1;
            this.lifterAxisPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.lifterAxisPanel.Controls.Add(this.lblLifterAxisTitle, 0, 0);
            this.lifterAxisPanel.Controls.Add(this.lblElevatorPos, 0, 1);
            this.lifterAxisPanel.Location = new System.Drawing.Point(638, 8);
            this.lifterAxisPanel.Margin = new System.Windows.Forms.Padding(4);
            this.lifterAxisPanel.Name = "lifterAxisPanel";
            this.lifterAxisPanel.RowCount = 2;
            this.slotStateLayout.SetRowSpan(this.lifterAxisPanel, 2);
            this.lifterAxisPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.lifterAxisPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lifterAxisPanel.Size = new System.Drawing.Size(182, 71);
            this.lifterAxisPanel.TabIndex = 9;
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
            // lblElevatorPos
            // 
            this.lblElevatorPos.BackColor = System.Drawing.Color.White;
            this.lblElevatorPos.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblElevatorPos.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblElevatorPos.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblElevatorPos.Location = new System.Drawing.Point(3, 24);
            this.lblElevatorPos.Name = "lblElevatorPos";
            this.lblElevatorPos.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblElevatorPos.Size = new System.Drawing.Size(176, 47);
            this.lblElevatorPos.TabIndex = 1;
            this.lblElevatorPos.Text = "0 um";
            this.lblElevatorPos.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
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
            this.lblSlotStateValue.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblSlotStateValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSlotStateValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblSlotStateValue.Location = new System.Drawing.Point(139, 43);
            this.lblSlotStateValue.Name = "lblSlotStateValue";
            this.lblSlotStateValue.Size = new System.Drawing.Size(182, 40);
            this.lblSlotStateValue.TabIndex = 3;
            this.lblSlotStateValue.Text = "-";
            this.lblSlotStateValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnInit
            // 
            this.btnInit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInit.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnInit.Location = new System.Drawing.Point(327, 46);
            this.btnInit.Name = "btnInit";
            this.btnInit.Size = new System.Drawing.Size(100, 34);
            this.btnInit.TabIndex = 6;
            this.btnInit.Text = "LIFTER INIT";
            // 
            // btnReady
            // 
            this.btnReady.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReady.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReady.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnReady.Location = new System.Drawing.Point(433, 46);
            this.btnReady.Name = "btnReady";
            this.btnReady.Size = new System.Drawing.Size(100, 34);
            this.btnReady.TabIndex = 7;
            this.btnReady.Text = "LIFTER READY";
            // 
            // grpLifter
            // 
            this.grpLifter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpLifter.Controls.Add(this.lifterContentLayout);
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
            // lifterContentLayout
            // 
            this.lifterContentLayout.ColumnCount = 1;
            this.lifterContentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lifterContentLayout.Controls.Add(this.legendLayout, 0, 0);
            this.lifterContentLayout.Controls.Add(this.cassetteLevelLayout, 0, 1);
            this.lifterContentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lifterContentLayout.Location = new System.Drawing.Point(3, 23);
            this.lifterContentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.lifterContentLayout.Name = "lifterContentLayout";
            this.lifterContentLayout.Padding = new System.Windows.Forms.Padding(3);
            this.lifterContentLayout.RowCount = 2;
            this.lifterContentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 21F));
            this.lifterContentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lifterContentLayout.Size = new System.Drawing.Size(830, 562);
            this.lifterContentLayout.TabIndex = 0;
            // 
            // legendLayout
            // 
            this.legendLayout.ColumnCount = 5;
            this.legendLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.legendLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.legendLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.legendLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.legendLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.legendLayout.Controls.Add(this.legendReadyPanel, 0, 0);
            this.legendLayout.Controls.Add(this.legendEmptyPanel, 1, 0);
            this.legendLayout.Controls.Add(this.legendWorkingPanel, 2, 0);
            this.legendLayout.Controls.Add(this.legendFinishPanel, 3, 0);
            this.legendLayout.Controls.Add(this.legendWorkReadyPanel, 4, 0);
            this.legendLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.legendLayout.Location = new System.Drawing.Point(3, 3);
            this.legendLayout.Margin = new System.Windows.Forms.Padding(0);
            this.legendLayout.Name = "legendLayout";
            this.legendLayout.RowCount = 1;
            this.legendLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendLayout.Size = new System.Drawing.Size(824, 21);
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
            this.legendReadyPanel.Location = new System.Drawing.Point(3, 3);
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
            this.lblLegendReadyColor.Location = new System.Drawing.Point(8, 0);
            this.lblLegendReadyColor.Margin = new System.Windows.Forms.Padding(0);
            this.lblLegendReadyColor.Name = "lblLegendReadyColor";
            this.lblLegendReadyColor.Size = new System.Drawing.Size(14, 14);
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
            this.legendEmptyPanel.Location = new System.Drawing.Point(167, 3);
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
            this.lblLegendEmptyColor.Location = new System.Drawing.Point(8, 0);
            this.lblLegendEmptyColor.Margin = new System.Windows.Forms.Padding(0);
            this.lblLegendEmptyColor.Name = "lblLegendEmptyColor";
            this.lblLegendEmptyColor.Size = new System.Drawing.Size(14, 14);
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
            this.legendWorkingPanel.Location = new System.Drawing.Point(331, 3);
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
            this.lblLegendWorkingColor.Location = new System.Drawing.Point(8, 0);
            this.lblLegendWorkingColor.Margin = new System.Windows.Forms.Padding(0);
            this.lblLegendWorkingColor.Name = "lblLegendWorkingColor";
            this.lblLegendWorkingColor.Size = new System.Drawing.Size(14, 14);
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
            this.legendFinishPanel.Location = new System.Drawing.Point(495, 3);
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
            this.lblLegendFinishColor.Location = new System.Drawing.Point(8, 0);
            this.lblLegendFinishColor.Margin = new System.Windows.Forms.Padding(0);
            this.lblLegendFinishColor.Name = "lblLegendFinishColor";
            this.lblLegendFinishColor.Size = new System.Drawing.Size(14, 14);
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
            this.legendWorkReadyPanel.Location = new System.Drawing.Point(659, 3);
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
            this.lblLegendWorkReadyColor.Location = new System.Drawing.Point(8, 0);
            this.lblLegendWorkReadyColor.Margin = new System.Windows.Forms.Padding(0);
            this.lblLegendWorkReadyColor.Name = "lblLegendWorkReadyColor";
            this.lblLegendWorkReadyColor.Size = new System.Drawing.Size(14, 14);
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
            this.cassetteLevelLayout.ColumnCount = 3;
            this.cassetteLevelLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.cassetteLevelLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.cassetteLevelLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.cassetteLevelLayout.Controls.Add(this._good1CassetteView, 0, 0);
            this.cassetteLevelLayout.Controls.Add(this._good2CassetteView, 1, 0);
            this.cassetteLevelLayout.Controls.Add(this._ngCassetteView, 2, 0);
            this.cassetteLevelLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cassetteLevelLayout.Location = new System.Drawing.Point(6, 27);
            this.cassetteLevelLayout.Name = "cassetteLevelLayout";
            this.cassetteLevelLayout.RowCount = 1;
            this.cassetteLevelLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cassetteLevelLayout.Size = new System.Drawing.Size(818, 529);
            this.cassetteLevelLayout.TabIndex = 1;
            // 
            // _good1CassetteView
            // 
            this._good1CassetteView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._good1CassetteView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._good1CassetteView.EmptyColor = System.Drawing.Color.LightGray;
            this._good1CassetteView.Location = new System.Drawing.Point(0, 0);
            this._good1CassetteView.Margin = new System.Windows.Forms.Padding(0);
            this._good1CassetteView.Name = "_good1CassetteView";
            this._good1CassetteView.Size = new System.Drawing.Size(272, 529);
            this._good1CassetteView.TabIndex = 0;
            this._good1CassetteView.Title = "OUTPUT GOOD 1단";
            // 
            // _good2CassetteView
            // 
            this._good2CassetteView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._good2CassetteView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._good2CassetteView.EmptyColor = System.Drawing.Color.LightGray;
            this._good2CassetteView.Location = new System.Drawing.Point(272, 0);
            this._good2CassetteView.Margin = new System.Windows.Forms.Padding(0);
            this._good2CassetteView.Name = "_good2CassetteView";
            this._good2CassetteView.Size = new System.Drawing.Size(272, 529);
            this._good2CassetteView.TabIndex = 1;
            this._good2CassetteView.Title = "OUTPUT GOOD 2단";
            // 
            // _ngCassetteView
            // 
            this._ngCassetteView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._ngCassetteView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._ngCassetteView.EmptyColor = System.Drawing.Color.LightGray;
            this._ngCassetteView.Location = new System.Drawing.Point(544, 0);
            this._ngCassetteView.Margin = new System.Windows.Forms.Padding(0);
            this._ngCassetteView.Name = "_ngCassetteView";
            this._ngCassetteView.Size = new System.Drawing.Size(274, 529);
            this._ngCassetteView.TabIndex = 2;
            this._ngCassetteView.Title = "OUTPUT NG";
            // 
            // grpAction
            // 
            this.grpAction.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpAction.Controls.Add(this.actionBar);
            this.grpAction.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.grpAction.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpAction.Location = new System.Drawing.Point(0, 713);
            this.grpAction.Margin = new System.Windows.Forms.Padding(0, 3, 3, 0);
            this.grpAction.Name = "grpAction";
            this.grpAction.Size = new System.Drawing.Size(836, 122);
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
            this.actionBar.Padding = new System.Windows.Forms.Padding(3, 1, 3, 0);
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
            this.btnMap.Text = "LIFT BIN MAPPING";
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
            this.btnLoad.Text = "LIFT BIN LOADING";
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
            this.btnUnload.Text = "LIFT BIN UNLOADING";
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
            // OutputCassettePage
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.Controls.Add(this.rootLayout);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "OutputCassettePage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.grpSlotState.ResumeLayout(false);
            this.slotStateLayout.ResumeLayout(false);
            this.sensorsPanel.ResumeLayout(false);
            this.good1CheckPanel.ResumeLayout(false);
            this.good2CheckPanel.ResumeLayout(false);
            this.ngCheckPanel.ResumeLayout(false);
            this.lifterAxisPanel.ResumeLayout(false);
            this.grpLifter.ResumeLayout(false);
            this.lifterContentLayout.ResumeLayout(false);
            this.legendLayout.ResumeLayout(false);
            this.legendReadyPanel.ResumeLayout(false);
            this.legendEmptyPanel.ResumeLayout(false);
            this.legendWorkingPanel.ResumeLayout(false);
            this.legendFinishPanel.ResumeLayout(false);
            this.legendWorkReadyPanel.ResumeLayout(false);
            this.cassetteLevelLayout.ResumeLayout(false);
            this.grpAction.ResumeLayout(false);
            this.actionBar.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
