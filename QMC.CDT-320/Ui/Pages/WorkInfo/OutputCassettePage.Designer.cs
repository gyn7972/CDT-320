using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class OutputCassettePage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel topLayout;
        private TableLayoutPanel contentLayout;
        private GroupBox grpSlotState;
        private GroupBox grpLifter;
        private GroupBox grpLegend;
        private TableLayoutPanel slotStateLayout;
        private TableLayoutPanel cassetteLevelLayout;
        private MaterialDetailView materialDetailView;
        private CassetteSlotView _ngCassetteView;
        private CassetteSlotView _good1CassetteView;
        private CassetteSlotView _good2CassetteView;
        private FlowLayoutPanel actionPanel;
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
            this.topLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lifterAxisPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblLifterAxisTitle = new System.Windows.Forms.Label();
            this.lblElevatorPos = new System.Windows.Forms.Label();
            this.good1CheckPanel = new System.Windows.Forms.TableLayoutPanel();
            this.dotGood1Check = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblGood1Check = new System.Windows.Forms.Label();
            this.good2CheckPanel = new System.Windows.Forms.TableLayoutPanel();
            this.dotGood2Check = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblGood2Check = new System.Windows.Forms.Label();
            this.ngCheckPanel = new System.Windows.Forms.TableLayoutPanel();
            this.dotNgCheck = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblNgCheck = new System.Windows.Forms.Label();
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
            this.cassetteLevelLayout = new System.Windows.Forms.TableLayoutPanel();
            this._good1CassetteView = new QMC.CDT_320.Ui.Controls.CassetteSlotView();
            this._good2CassetteView = new QMC.CDT_320.Ui.Controls.CassetteSlotView();
            this._ngCassetteView = new QMC.CDT_320.Ui.Controls.CassetteSlotView();
            this.materialDetailView = new QMC.CDT_320.Ui.Controls.MaterialDetailView();
            this.actionPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnMap = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnLoad = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnUnload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnStop = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.rootLayout.SuspendLayout();
            this.topLayout.SuspendLayout();
            this.lifterAxisPanel.SuspendLayout();
            this.good1CheckPanel.SuspendLayout();
            this.good2CheckPanel.SuspendLayout();
            this.ngCheckPanel.SuspendLayout();
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
            this.cassetteLevelLayout.SuspendLayout();
            this.actionPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.topLayout, 0, 1);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 2);
            this.rootLayout.Controls.Add(this.actionPanel, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 143F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(3, 0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1672, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "INFO";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // topLayout
            // 
            this.topLayout.ColumnCount = 5;
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 240F));
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 230F));
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 230F));
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 230F));
            this.topLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topLayout.Controls.Add(this.lifterAxisPanel, 0, 0);
            this.topLayout.Controls.Add(this.good1CheckPanel, 1, 0);
            this.topLayout.Controls.Add(this.good2CheckPanel, 2, 0);
            this.topLayout.Controls.Add(this.ngCheckPanel, 3, 0);
            this.topLayout.Controls.Add(this.grpLegend, 4, 0);
            this.topLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topLayout.Location = new System.Drawing.Point(3, 33);
            this.topLayout.Name = "topLayout";
            this.topLayout.Padding = new System.Windows.Forms.Padding(8);
            this.topLayout.RowCount = 1;
            this.topLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topLayout.Size = new System.Drawing.Size(1672, 137);
            this.topLayout.TabIndex = 1;
            // 
            // lifterAxisPanel
            // 
            this.lifterAxisPanel.ColumnCount = 1;
            this.lifterAxisPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lifterAxisPanel.Controls.Add(this.lblLifterAxisTitle, 0, 0);
            this.lifterAxisPanel.Controls.Add(this.lblElevatorPos, 0, 1);
            this.lifterAxisPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lifterAxisPanel.Location = new System.Drawing.Point(12, 12);
            this.lifterAxisPanel.Margin = new System.Windows.Forms.Padding(4);
            this.lifterAxisPanel.Name = "lifterAxisPanel";
            this.lifterAxisPanel.RowCount = 2;
            this.lifterAxisPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.lifterAxisPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lifterAxisPanel.Size = new System.Drawing.Size(232, 113);
            this.lifterAxisPanel.TabIndex = 0;
            // 
            // lblLifterAxisTitle
            // 
            this.lblLifterAxisTitle.BackColor = System.Drawing.Color.Black;
            this.lblLifterAxisTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLifterAxisTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold);
            this.lblLifterAxisTitle.ForeColor = System.Drawing.Color.White;
            this.lblLifterAxisTitle.Location = new System.Drawing.Point(3, 0);
            this.lblLifterAxisTitle.Name = "lblLifterAxisTitle";
            this.lblLifterAxisTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblLifterAxisTitle.Size = new System.Drawing.Size(226, 24);
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
            this.lblElevatorPos.Size = new System.Drawing.Size(226, 89);
            this.lblElevatorPos.TabIndex = 1;
            this.lblElevatorPos.Text = "0 um";
            this.lblElevatorPos.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // good1CheckPanel
            // 
            this.good1CheckPanel.ColumnCount = 2;
            this.good1CheckPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.good1CheckPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 66.66667F));
            this.good1CheckPanel.Controls.Add(this.lblGood1Check, 1, 1);
            this.good1CheckPanel.Controls.Add(this.dotGood1Check, 0, 1);
            this.good1CheckPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.good1CheckPanel.Location = new System.Drawing.Point(249, 9);
            this.good1CheckPanel.Margin = new System.Windows.Forms.Padding(1);
            this.good1CheckPanel.Name = "good1CheckPanel";
            this.good1CheckPanel.RowCount = 3;
            this.good1CheckPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.good1CheckPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.good1CheckPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.good1CheckPanel.Size = new System.Drawing.Size(228, 119);
            this.good1CheckPanel.TabIndex = 1;
            // 
            // dotGood1Check
            // 
            this.dotGood1Check.BackColor = System.Drawing.Color.Transparent;
            this.dotGood1Check.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotGood1Check.Location = new System.Drawing.Point(20, 44);
            this.dotGood1Check.Margin = new System.Windows.Forms.Padding(20, 5, 20, 5);
            this.dotGood1Check.Name = "dotGood1Check";
            this.dotGood1Check.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotGood1Check.OnColor = System.Drawing.Color.LimeGreen;
            this.dotGood1Check.Size = new System.Drawing.Size(35, 29);
            this.dotGood1Check.TabIndex = 0;
            // 
            // lblGood1Check
            // 
            this.lblGood1Check.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblGood1Check.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGood1Check.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblGood1Check.Location = new System.Drawing.Point(78, 39);
            this.lblGood1Check.Name = "lblGood1Check";
            this.lblGood1Check.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblGood1Check.Size = new System.Drawing.Size(147, 39);
            this.lblGood1Check.TabIndex = 1;
            this.lblGood1Check.Text = "GOOD 1단";
            this.lblGood1Check.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // good2CheckPanel
            // 
            this.good2CheckPanel.ColumnCount = 2;
            this.good2CheckPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.good2CheckPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 66.66666F));
            this.good2CheckPanel.Controls.Add(this.lblGood2Check, 1, 1);
            this.good2CheckPanel.Controls.Add(this.dotGood2Check, 0, 1);
            this.good2CheckPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.good2CheckPanel.Location = new System.Drawing.Point(482, 12);
            this.good2CheckPanel.Margin = new System.Windows.Forms.Padding(4);
            this.good2CheckPanel.Name = "good2CheckPanel";
            this.good2CheckPanel.RowCount = 3;
            this.good2CheckPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.good2CheckPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.good2CheckPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.good2CheckPanel.Size = new System.Drawing.Size(222, 113);
            this.good2CheckPanel.TabIndex = 2;
            // 
            // dotGood2Check
            // 
            this.dotGood2Check.BackColor = System.Drawing.Color.Transparent;
            this.dotGood2Check.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotGood2Check.Location = new System.Drawing.Point(20, 42);
            this.dotGood2Check.Margin = new System.Windows.Forms.Padding(20, 5, 20, 5);
            this.dotGood2Check.Name = "dotGood2Check";
            this.dotGood2Check.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotGood2Check.OnColor = System.Drawing.Color.LimeGreen;
            this.dotGood2Check.Size = new System.Drawing.Size(33, 27);
            this.dotGood2Check.TabIndex = 0;
            // 
            // lblGood2Check
            // 
            this.lblGood2Check.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblGood2Check.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGood2Check.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblGood2Check.Location = new System.Drawing.Point(76, 37);
            this.lblGood2Check.Name = "lblGood2Check";
            this.lblGood2Check.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblGood2Check.Size = new System.Drawing.Size(143, 37);
            this.lblGood2Check.TabIndex = 1;
            this.lblGood2Check.Text = "GOOD 2단";
            this.lblGood2Check.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ngCheckPanel
            // 
            this.ngCheckPanel.ColumnCount = 2;
            this.ngCheckPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.ngCheckPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 66.66666F));
            this.ngCheckPanel.Controls.Add(this.dotNgCheck, 0, 1);
            this.ngCheckPanel.Controls.Add(this.lblNgCheck, 1, 1);
            this.ngCheckPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ngCheckPanel.Location = new System.Drawing.Point(712, 12);
            this.ngCheckPanel.Margin = new System.Windows.Forms.Padding(4);
            this.ngCheckPanel.Name = "ngCheckPanel";
            this.ngCheckPanel.RowCount = 3;
            this.ngCheckPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.ngCheckPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.ngCheckPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.ngCheckPanel.Size = new System.Drawing.Size(222, 113);
            this.ngCheckPanel.TabIndex = 3;
            // 
            // dotNgCheck
            // 
            this.dotNgCheck.BackColor = System.Drawing.Color.Transparent;
            this.dotNgCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotNgCheck.Location = new System.Drawing.Point(20, 42);
            this.dotNgCheck.Margin = new System.Windows.Forms.Padding(20, 5, 20, 5);
            this.dotNgCheck.Name = "dotNgCheck";
            this.dotNgCheck.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotNgCheck.OnColor = System.Drawing.Color.LimeGreen;
            this.dotNgCheck.Size = new System.Drawing.Size(33, 27);
            this.dotNgCheck.TabIndex = 0;
            // 
            // lblNgCheck
            // 
            this.lblNgCheck.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNgCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgCheck.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblNgCheck.Location = new System.Drawing.Point(76, 37);
            this.lblNgCheck.Name = "lblNgCheck";
            this.lblNgCheck.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblNgCheck.Size = new System.Drawing.Size(143, 37);
            this.lblNgCheck.TabIndex = 1;
            this.lblNgCheck.Text = "NG";
            this.lblNgCheck.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpLegend
            // 
            this.grpLegend.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.grpLegend.Controls.Add(this.legendLayout);
            this.grpLegend.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLegend.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.grpLegend.Location = new System.Drawing.Point(942, 12);
            this.grpLegend.Margin = new System.Windows.Forms.Padding(4);
            this.grpLegend.Name = "grpLegend";
            this.grpLegend.Size = new System.Drawing.Size(718, 113);
            this.grpLegend.TabIndex = 4;
            this.grpLegend.TabStop = false;
            this.grpLegend.Text = "SLOT STATUS COLOR";
            // 
            // legendLayout
            // 
            this.legendLayout.ColumnCount = 3;
            this.legendLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.3F));
            this.legendLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.3F));
            this.legendLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.4F));
            this.legendLayout.Controls.Add(this.legendReadyPanel, 0, 0);
            this.legendLayout.Controls.Add(this.legendEmptyPanel, 1, 0);
            this.legendLayout.Controls.Add(this.legendWorkingPanel, 2, 0);
            this.legendLayout.Controls.Add(this.legendFinishPanel, 0, 1);
            this.legendLayout.Controls.Add(this.legendWorkReadyPanel, 1, 1);
            this.legendLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.legendLayout.Location = new System.Drawing.Point(3, 19);
            this.legendLayout.Name = "legendLayout";
            this.legendLayout.Padding = new System.Windows.Forms.Padding(10, 16, 10, 8);
            this.legendLayout.RowCount = 2;
            this.legendLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.legendLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.legendLayout.Size = new System.Drawing.Size(712, 91);
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
            this.legendReadyPanel.Location = new System.Drawing.Point(13, 19);
            this.legendReadyPanel.Name = "legendReadyPanel";
            this.legendReadyPanel.RowCount = 1;
            this.legendReadyPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendReadyPanel.Size = new System.Drawing.Size(224, 27);
            this.legendReadyPanel.TabIndex = 0;
            // 
            // lblLegendReadyColor
            // 
            this.lblLegendReadyColor.BackColor = System.Drawing.Color.Cyan;
            this.lblLegendReadyColor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendReadyColor.Location = new System.Drawing.Point(2, 6);
            this.lblLegendReadyColor.Margin = new System.Windows.Forms.Padding(2, 6, 2, 6);
            this.lblLegendReadyColor.Name = "lblLegendReadyColor";
            this.lblLegendReadyColor.Size = new System.Drawing.Size(26, 15);
            this.lblLegendReadyColor.TabIndex = 0;
            // 
            // lblLegendReadyText
            // 
            this.lblLegendReadyText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendReadyText.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLegendReadyText.Location = new System.Drawing.Point(33, 0);
            this.lblLegendReadyText.Name = "lblLegendReadyText";
            this.lblLegendReadyText.Size = new System.Drawing.Size(188, 27);
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
            this.legendEmptyPanel.Location = new System.Drawing.Point(243, 19);
            this.legendEmptyPanel.Name = "legendEmptyPanel";
            this.legendEmptyPanel.RowCount = 1;
            this.legendEmptyPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendEmptyPanel.Size = new System.Drawing.Size(224, 27);
            this.legendEmptyPanel.TabIndex = 1;
            // 
            // lblLegendEmptyColor
            // 
            this.lblLegendEmptyColor.BackColor = System.Drawing.Color.Lime;
            this.lblLegendEmptyColor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendEmptyColor.Location = new System.Drawing.Point(2, 6);
            this.lblLegendEmptyColor.Margin = new System.Windows.Forms.Padding(2, 6, 2, 6);
            this.lblLegendEmptyColor.Name = "lblLegendEmptyColor";
            this.lblLegendEmptyColor.Size = new System.Drawing.Size(26, 15);
            this.lblLegendEmptyColor.TabIndex = 0;
            // 
            // lblLegendEmptyText
            // 
            this.lblLegendEmptyText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendEmptyText.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLegendEmptyText.Location = new System.Drawing.Point(33, 0);
            this.lblLegendEmptyText.Name = "lblLegendEmptyText";
            this.lblLegendEmptyText.Size = new System.Drawing.Size(188, 27);
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
            this.legendWorkingPanel.Location = new System.Drawing.Point(473, 19);
            this.legendWorkingPanel.Name = "legendWorkingPanel";
            this.legendWorkingPanel.RowCount = 1;
            this.legendWorkingPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendWorkingPanel.Size = new System.Drawing.Size(226, 27);
            this.legendWorkingPanel.TabIndex = 2;
            // 
            // lblLegendWorkingColor
            // 
            this.lblLegendWorkingColor.BackColor = System.Drawing.Color.Orange;
            this.lblLegendWorkingColor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendWorkingColor.Location = new System.Drawing.Point(2, 6);
            this.lblLegendWorkingColor.Margin = new System.Windows.Forms.Padding(2, 6, 2, 6);
            this.lblLegendWorkingColor.Name = "lblLegendWorkingColor";
            this.lblLegendWorkingColor.Size = new System.Drawing.Size(26, 15);
            this.lblLegendWorkingColor.TabIndex = 0;
            // 
            // lblLegendWorkingText
            // 
            this.lblLegendWorkingText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendWorkingText.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLegendWorkingText.Location = new System.Drawing.Point(33, 0);
            this.lblLegendWorkingText.Name = "lblLegendWorkingText";
            this.lblLegendWorkingText.Size = new System.Drawing.Size(190, 27);
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
            this.legendFinishPanel.Location = new System.Drawing.Point(13, 52);
            this.legendFinishPanel.Name = "legendFinishPanel";
            this.legendFinishPanel.RowCount = 1;
            this.legendFinishPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendFinishPanel.Size = new System.Drawing.Size(224, 28);
            this.legendFinishPanel.TabIndex = 3;
            // 
            // lblLegendFinishColor
            // 
            this.lblLegendFinishColor.BackColor = System.Drawing.Color.Red;
            this.lblLegendFinishColor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendFinishColor.Location = new System.Drawing.Point(2, 6);
            this.lblLegendFinishColor.Margin = new System.Windows.Forms.Padding(2, 6, 2, 6);
            this.lblLegendFinishColor.Name = "lblLegendFinishColor";
            this.lblLegendFinishColor.Size = new System.Drawing.Size(26, 16);
            this.lblLegendFinishColor.TabIndex = 0;
            // 
            // lblLegendFinishText
            // 
            this.lblLegendFinishText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendFinishText.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLegendFinishText.Location = new System.Drawing.Point(33, 0);
            this.lblLegendFinishText.Name = "lblLegendFinishText";
            this.lblLegendFinishText.Size = new System.Drawing.Size(188, 28);
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
            this.legendWorkReadyPanel.Location = new System.Drawing.Point(243, 52);
            this.legendWorkReadyPanel.Name = "legendWorkReadyPanel";
            this.legendWorkReadyPanel.RowCount = 1;
            this.legendWorkReadyPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.legendWorkReadyPanel.Size = new System.Drawing.Size(224, 28);
            this.legendWorkReadyPanel.TabIndex = 4;
            // 
            // lblLegendWorkReadyColor
            // 
            this.lblLegendWorkReadyColor.BackColor = System.Drawing.Color.Navy;
            this.lblLegendWorkReadyColor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendWorkReadyColor.Location = new System.Drawing.Point(2, 6);
            this.lblLegendWorkReadyColor.Margin = new System.Windows.Forms.Padding(2, 6, 2, 6);
            this.lblLegendWorkReadyColor.Name = "lblLegendWorkReadyColor";
            this.lblLegendWorkReadyColor.Size = new System.Drawing.Size(26, 16);
            this.lblLegendWorkReadyColor.TabIndex = 0;
            // 
            // lblLegendWorkReadyText
            // 
            this.lblLegendWorkReadyText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLegendWorkReadyText.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLegendWorkReadyText.Location = new System.Drawing.Point(33, 0);
            this.lblLegendWorkReadyText.Name = "lblLegendWorkReadyText";
            this.lblLegendWorkReadyText.Size = new System.Drawing.Size(188, 28);
            this.lblLegendWorkReadyText.TabIndex = 1;
            this.lblLegendWorkReadyText.Text = "WORK READY";
            this.lblLegendWorkReadyText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 250F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 760F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Controls.Add(this.grpSlotState, 0, 0);
            this.contentLayout.Controls.Add(this.grpLifter, 1, 0);
            this.contentLayout.Controls.Add(this.materialDetailView, 2, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(3, 176);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1672, 625);
            this.contentLayout.TabIndex = 2;
            // 
            // grpSlotState
            // 
            this.grpSlotState.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpSlotState.Controls.Add(this.slotStateLayout);
            this.grpSlotState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSlotState.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold);
            this.grpSlotState.Location = new System.Drawing.Point(15, 11);
            this.grpSlotState.Name = "grpSlotState";
            this.grpSlotState.Size = new System.Drawing.Size(244, 603);
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
            this.slotStateLayout.Location = new System.Drawing.Point(3, 20);
            this.slotStateLayout.Name = "slotStateLayout";
            this.slotStateLayout.Padding = new System.Windows.Forms.Padding(8, 20, 8, 8);
            this.slotStateLayout.RowCount = 5;
            this.slotStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.slotStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.slotStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.slotStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.slotStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.slotStateLayout.Size = new System.Drawing.Size(238, 580);
            this.slotStateLayout.TabIndex = 0;
            // 
            // lblSlotNoTitle
            // 
            this.lblSlotNoTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSlotNoTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblSlotNoTitle.Location = new System.Drawing.Point(11, 20);
            this.lblSlotNoTitle.Name = "lblSlotNoTitle";
            this.lblSlotNoTitle.Size = new System.Drawing.Size(105, 32);
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
            this.lblSlotNoValue.Location = new System.Drawing.Point(122, 20);
            this.lblSlotNoValue.Name = "lblSlotNoValue";
            this.lblSlotNoValue.Size = new System.Drawing.Size(105, 32);
            this.lblSlotNoValue.TabIndex = 1;
            this.lblSlotNoValue.Text = "GOOD1 / -";
            this.lblSlotNoValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSlotStateTitle
            // 
            this.lblSlotStateTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSlotStateTitle.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblSlotStateTitle.Location = new System.Drawing.Point(11, 52);
            this.lblSlotStateTitle.Name = "lblSlotStateTitle";
            this.lblSlotStateTitle.Size = new System.Drawing.Size(105, 32);
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
            this.lblSlotStateValue.Location = new System.Drawing.Point(122, 52);
            this.lblSlotStateValue.Name = "lblSlotStateValue";
            this.lblSlotStateValue.Size = new System.Drawing.Size(105, 32);
            this.lblSlotStateValue.TabIndex = 3;
            this.lblSlotStateValue.Text = "-";
            this.lblSlotStateValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnPrev
            // 
            this.btnPrev.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPrev.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrev.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnPrev.Location = new System.Drawing.Point(11, 87);
            this.btnPrev.Name = "btnPrev";
            this.btnPrev.Size = new System.Drawing.Size(105, 36);
            this.btnPrev.TabIndex = 4;
            this.btnPrev.Text = "PREV";
            // 
            // btnNext
            // 
            this.btnNext.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNext.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnNext.Location = new System.Drawing.Point(122, 87);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(105, 36);
            this.btnNext.TabIndex = 5;
            this.btnNext.Text = "NEXT";
            // 
            // btnInit
            // 
            this.btnInit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInit.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnInit.Location = new System.Drawing.Point(11, 129);
            this.btnInit.Name = "btnInit";
            this.btnInit.Size = new System.Drawing.Size(105, 36);
            this.btnInit.TabIndex = 6;
            this.btnInit.Text = "LIFTER INIT";
            // 
            // btnReady
            // 
            this.btnReady.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReady.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReady.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnReady.Location = new System.Drawing.Point(122, 129);
            this.btnReady.Name = "btnReady";
            this.btnReady.Size = new System.Drawing.Size(105, 36);
            this.btnReady.TabIndex = 7;
            this.btnReady.Text = "LIFTER READY";
            // 
            // grpLifter
            // 
            this.grpLifter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpLifter.Controls.Add(this.cassetteLevelLayout);
            this.grpLifter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLifter.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold);
            this.grpLifter.Location = new System.Drawing.Point(265, 11);
            this.grpLifter.Name = "grpLifter";
            this.grpLifter.Size = new System.Drawing.Size(754, 603);
            this.grpLifter.TabIndex = 1;
            this.grpLifter.TabStop = false;
            this.grpLifter.Text = "LIFTER";
            // 
            // cassetteLevelLayout
            // 
            this.cassetteLevelLayout.ColumnCount = 3;
            this.cassetteLevelLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.cassetteLevelLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.cassetteLevelLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.cassetteLevelLayout.Controls.Add(this._good1CassetteView, 0, 0);
            this.cassetteLevelLayout.Controls.Add(this._good2CassetteView, 1, 0);
            this.cassetteLevelLayout.Controls.Add(this._ngCassetteView, 2, 0);
            this.cassetteLevelLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cassetteLevelLayout.Location = new System.Drawing.Point(3, 20);
            this.cassetteLevelLayout.Name = "cassetteLevelLayout";
            this.cassetteLevelLayout.RowCount = 1;
            this.cassetteLevelLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cassetteLevelLayout.Size = new System.Drawing.Size(748, 580);
            this.cassetteLevelLayout.TabIndex = 0;
            // 
            // _good1CassetteView
            // 
            this._good1CassetteView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._good1CassetteView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._good1CassetteView.EmptyColor = System.Drawing.Color.LightGray;
            this._good1CassetteView.Location = new System.Drawing.Point(0, 0);
            this._good1CassetteView.Margin = new System.Windows.Forms.Padding(0);
            this._good1CassetteView.Name = "_good1CassetteView";
            this._good1CassetteView.Size = new System.Drawing.Size(254, 580);
            this._good1CassetteView.TabIndex = 0;
            this._good1CassetteView.Title = "OUTPUT GOOD 1단";
            // 
            // _good2CassetteView
            // 
            this._good2CassetteView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._good2CassetteView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._good2CassetteView.EmptyColor = System.Drawing.Color.LightGray;
            this._good2CassetteView.Location = new System.Drawing.Point(254, 0);
            this._good2CassetteView.Margin = new System.Windows.Forms.Padding(0);
            this._good2CassetteView.Name = "_good2CassetteView";
            this._good2CassetteView.Size = new System.Drawing.Size(246, 580);
            this._good2CassetteView.TabIndex = 1;
            this._good2CassetteView.Title = "OUTPUT GOOD 2단";
            // 
            // _ngCassetteView
            // 
            this._ngCassetteView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._ngCassetteView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._ngCassetteView.EmptyColor = System.Drawing.Color.LightGray;
            this._ngCassetteView.Location = new System.Drawing.Point(500, 0);
            this._ngCassetteView.Margin = new System.Windows.Forms.Padding(0);
            this._ngCassetteView.Name = "_ngCassetteView";
            this._ngCassetteView.Size = new System.Drawing.Size(248, 580);
            this._ngCassetteView.TabIndex = 2;
            this._ngCassetteView.Title = "OUTPUT NG";
            // 
            // materialDetailView
            // 
            this.materialDetailView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.materialDetailView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialDetailView.Location = new System.Drawing.Point(1025, 11);
            this.materialDetailView.Name = "materialDetailView";
            this.materialDetailView.Size = new System.Drawing.Size(632, 603);
            this.materialDetailView.TabIndex = 2;
            // 
            // actionPanel
            // 
            this.actionPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.actionPanel.Controls.Add(this.btnMap);
            this.actionPanel.Controls.Add(this.btnLoad);
            this.actionPanel.Controls.Add(this.btnUnload);
            this.actionPanel.Controls.Add(this.btnStop);
            this.actionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionPanel.Location = new System.Drawing.Point(3, 807);
            this.actionPanel.Name = "actionPanel";
            this.actionPanel.Padding = new System.Windows.Forms.Padding(12);
            this.actionPanel.Size = new System.Drawing.Size(1672, 90);
            this.actionPanel.TabIndex = 3;
            this.actionPanel.WrapContents = false;
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
            this.btnMap.Text = "LIFT BIN MAPPING";
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
            this.btnLoad.Text = "LIFT BIN LOADING";
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
            this.btnUnload.Text = "LIFT BIN\r\nUNLOADING";
            // 
            // btnStop
            // 
            this.btnStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.btnStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStop.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.Location = new System.Drawing.Point(594, 18);
            this.btnStop.Margin = new System.Windows.Forms.Padding(6);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(140, 64);
            this.btnStop.TabIndex = 3;
            this.btnStop.Text = "STOP";
            // 
            // OutputCassettePage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "OutputCassettePage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.topLayout.ResumeLayout(false);
            this.lifterAxisPanel.ResumeLayout(false);
            this.good1CheckPanel.ResumeLayout(false);
            this.good2CheckPanel.ResumeLayout(false);
            this.ngCheckPanel.ResumeLayout(false);
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
            this.cassetteLevelLayout.ResumeLayout(false);
            this.actionPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

    }
}

