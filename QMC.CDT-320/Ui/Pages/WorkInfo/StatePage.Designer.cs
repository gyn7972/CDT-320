using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class StatePage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;

        // ── ACTIVE LOT ──
        private GroupBox grpActiveLot;
        private TableLayoutPanel lotLayout;
        private Label lblIdCaption;
        private Label lblRecipeCaption;
        private Label lblStateCaption;
        private Label lblStartCaption;
        private Label lblProcessedCaption;
        private Label lblGoodCaption;
        private Label lblNgCaption;
        private Label lblYieldCaption;
        private Label _lblId;
        private Label _lblRecipe;
        private Label _lblState;
        private Label _lblStart;
        private Label _lblProcessed;
        private Label _lblGood;
        private Label _lblNg;
        private Label _lblYield;

        // ── BIN ──
        private GroupBox grpBin;
        private Panel _binPanel;

        // ── PLATE ──
        private TableLayoutPanel plateArea;
        private GroupBox grpNg;
        private TableLayoutPanel ngLayout;
        private TableLayoutPanel ngSlotLayout;
        private Label _lblNgCount;
        private GroupBox grpGood;
        private TableLayoutPanel goodLayout;
        private TableLayoutPanel goodSlotLayout;
        private Label _lblGoodCount;
        // _ngSlots / _goodSlots 슬롯 라벨은 StatePage.cs에서 런타임 생성한다.
        // (WinForms 디자이너가 컨트롤 배열을 직렬화하지 못해 재직렬화 시 드롭되므로 코드-비하인드에서 구성)
        private Button btnReset;

        // ── OPERATION PANEL SENSORS ──
        private TableLayoutPanel sensorGrid;
        private TableLayoutPanel leftSensors;
        private TableLayoutPanel rightSensors;
        private GroupBox grpButtons;
        private TableLayoutPanel buttonLayout;
        private GroupBox grpLamps;
        private TableLayoutPanel lampLayout;
        private GroupBox grpResources;
        private TableLayoutPanel resourceLayout;
        private GroupBox grpTower;
        private TableLayoutPanel towerLayout;
        private GroupBox grpIonizer;
        private TableLayoutPanel ionizerLayout;
        private IndicatorDot _dotStart;
        private IndicatorDot _dotStop;
        private IndicatorDot _dotReset;
        private IndicatorDot _dotEmgF;
        private IndicatorDot _dotEmgL;
        private IndicatorDot _dotEmgR;
        private IndicatorDot _ledStartLamp;
        private IndicatorDot _ledStopLamp;
        private IndicatorDot _ledResetLamp;
        private IndicatorDot _tlRed;
        private IndicatorDot _tlYellow;
        private IndicatorDot _tlGreen;
        private IndicatorDot _ledBuzzer;
        private IndicatorDot _dotCda1;
        private IndicatorDot _dotCda2;
        private IndicatorDot _dotVac1;
        private IndicatorDot _dotVac2;
        private IndicatorDot _dotVac3;
        private IndicatorDot _dotVac4;
        private IndicatorDot _dotIonizer;
        private Label lblStart;
        private Label lblStop;
        private Label lblReset;
        private Label lblEmgF;
        private Label lblEmgL;
        private Label lblEmgR;
        private Label lblStartLamp;
        private Label lblStopLamp;
        private Label lblResetLamp;
        private Label lblTlRed;
        private Label lblTlYellow;
        private Label lblTlGreen;
        private Label lblBuzzer;
        private Label lblCda1;
        private Label lblCda2;
        private Label lblVac1;
        private Label lblVac2;
        private Label lblVac3;
        private Label lblVac4;
        private Label lblIonizer;

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpActiveLot = new System.Windows.Forms.GroupBox();
            this.lotLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblIdCaption = new System.Windows.Forms.Label();
            this._lblId = new System.Windows.Forms.Label();
            this.lblRecipeCaption = new System.Windows.Forms.Label();
            this._lblRecipe = new System.Windows.Forms.Label();
            this.lblStateCaption = new System.Windows.Forms.Label();
            this._lblState = new System.Windows.Forms.Label();
            this.lblStartCaption = new System.Windows.Forms.Label();
            this._lblStart = new System.Windows.Forms.Label();
            this.lblProcessedCaption = new System.Windows.Forms.Label();
            this._lblProcessed = new System.Windows.Forms.Label();
            this.lblGoodCaption = new System.Windows.Forms.Label();
            this._lblGood = new System.Windows.Forms.Label();
            this.lblNgCaption = new System.Windows.Forms.Label();
            this._lblNg = new System.Windows.Forms.Label();
            this.lblYieldCaption = new System.Windows.Forms.Label();
            this._lblYield = new System.Windows.Forms.Label();
            this.grpBin = new System.Windows.Forms.GroupBox();
            this._binPanel = new System.Windows.Forms.Panel();
            this.plateArea = new System.Windows.Forms.TableLayoutPanel();
            this.grpNg = new System.Windows.Forms.GroupBox();
            this.ngLayout = new System.Windows.Forms.TableLayoutPanel();
            this._lblNgCount = new System.Windows.Forms.Label();
            this.ngSlotLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpGood = new System.Windows.Forms.GroupBox();
            this.goodLayout = new System.Windows.Forms.TableLayoutPanel();
            this._lblGoodCount = new System.Windows.Forms.Label();
            this.goodSlotLayout = new System.Windows.Forms.TableLayoutPanel();
            this.sensorGrid = new System.Windows.Forms.TableLayoutPanel();
            this.leftSensors = new System.Windows.Forms.TableLayoutPanel();
            this.grpButtons = new System.Windows.Forms.GroupBox();
            this.buttonLayout = new System.Windows.Forms.TableLayoutPanel();
            this._dotStart = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblStart = new System.Windows.Forms.Label();
            this._dotStop = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblStop = new System.Windows.Forms.Label();
            this._dotReset = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblReset = new System.Windows.Forms.Label();
            this._dotEmgF = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblEmgF = new System.Windows.Forms.Label();
            this._dotEmgL = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblEmgL = new System.Windows.Forms.Label();
            this._dotEmgR = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblEmgR = new System.Windows.Forms.Label();
            this.grpResources = new System.Windows.Forms.GroupBox();
            this.resourceLayout = new System.Windows.Forms.TableLayoutPanel();
            this._dotCda1 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblCda1 = new System.Windows.Forms.Label();
            this._dotCda2 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblCda2 = new System.Windows.Forms.Label();
            this._dotVac1 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblVac1 = new System.Windows.Forms.Label();
            this._dotVac2 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblVac2 = new System.Windows.Forms.Label();
            this._dotVac3 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblVac3 = new System.Windows.Forms.Label();
            this._dotVac4 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblVac4 = new System.Windows.Forms.Label();
            this.rightSensors = new System.Windows.Forms.TableLayoutPanel();
            this.grpLamps = new System.Windows.Forms.GroupBox();
            this.lampLayout = new System.Windows.Forms.TableLayoutPanel();
            this._ledStartLamp = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblStartLamp = new System.Windows.Forms.Label();
            this._ledStopLamp = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblStopLamp = new System.Windows.Forms.Label();
            this._ledResetLamp = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblResetLamp = new System.Windows.Forms.Label();
            this.grpTower = new System.Windows.Forms.GroupBox();
            this.towerLayout = new System.Windows.Forms.TableLayoutPanel();
            this._tlRed = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblTlRed = new System.Windows.Forms.Label();
            this._tlYellow = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblTlYellow = new System.Windows.Forms.Label();
            this._tlGreen = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblTlGreen = new System.Windows.Forms.Label();
            this._ledBuzzer = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblBuzzer = new System.Windows.Forms.Label();
            this.grpIonizer = new System.Windows.Forms.GroupBox();
            this.ionizerLayout = new System.Windows.Forms.TableLayoutPanel();
            this._dotIonizer = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblIonizer = new System.Windows.Forms.Label();
            this.btnReset = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.grpActiveLot.SuspendLayout();
            this.lotLayout.SuspendLayout();
            this.grpBin.SuspendLayout();
            this.plateArea.SuspendLayout();
            this.grpNg.SuspendLayout();
            this.ngLayout.SuspendLayout();
            this.grpGood.SuspendLayout();
            this.goodLayout.SuspendLayout();
            this.sensorGrid.SuspendLayout();
            this.leftSensors.SuspendLayout();
            this.grpButtons.SuspendLayout();
            this.buttonLayout.SuspendLayout();
            this.grpResources.SuspendLayout();
            this.resourceLayout.SuspendLayout();
            this.rightSensors.SuspendLayout();
            this.grpLamps.SuspendLayout();
            this.lampLayout.SuspendLayout();
            this.grpTower.SuspendLayout();
            this.towerLayout.SuspendLayout();
            this.grpIonizer.SuspendLayout();
            this.ionizerLayout.SuspendLayout();
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
            this.lblHeader.Tag = "i18n:wi.state";
            this.lblHeader.Text = "STATE";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.Controls.Add(this.grpActiveLot, 0, 0);
            this.contentLayout.Controls.Add(this.grpBin, 1, 0);
            this.contentLayout.Controls.Add(this.plateArea, 0, 1);
            this.contentLayout.Controls.Add(this.sensorGrid, 1, 1);
            this.contentLayout.Controls.Add(this.btnReset, 0, 2);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(0, 30);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.RowCount = 3;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 452F));
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.contentLayout.Size = new System.Drawing.Size(1678, 870);
            this.contentLayout.TabIndex = 1;
            // 
            // grpActiveLot
            // 
            this.grpActiveLot.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpActiveLot.Controls.Add(this.lotLayout);
            this.grpActiveLot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpActiveLot.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpActiveLot.Location = new System.Drawing.Point(0, 0);
            this.grpActiveLot.Margin = new System.Windows.Forms.Padding(0, 0, 3, 3);
            this.grpActiveLot.Name = "grpActiveLot";
            this.grpActiveLot.Size = new System.Drawing.Size(836, 449);
            this.grpActiveLot.TabIndex = 0;
            this.grpActiveLot.TabStop = false;
            this.grpActiveLot.Text = "ACTIVE LOT";
            // 
            // lotLayout
            // 
            this.lotLayout.ColumnCount = 2;
            this.lotLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 175F));
            this.lotLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lotLayout.Controls.Add(this.lblIdCaption, 0, 0);
            this.lotLayout.Controls.Add(this._lblId, 1, 0);
            this.lotLayout.Controls.Add(this.lblRecipeCaption, 0, 1);
            this.lotLayout.Controls.Add(this._lblRecipe, 1, 1);
            this.lotLayout.Controls.Add(this.lblStateCaption, 0, 2);
            this.lotLayout.Controls.Add(this._lblState, 1, 2);
            this.lotLayout.Controls.Add(this.lblStartCaption, 0, 3);
            this.lotLayout.Controls.Add(this._lblStart, 1, 3);
            this.lotLayout.Controls.Add(this.lblProcessedCaption, 0, 4);
            this.lotLayout.Controls.Add(this._lblProcessed, 1, 4);
            this.lotLayout.Controls.Add(this.lblGoodCaption, 0, 5);
            this.lotLayout.Controls.Add(this._lblGood, 1, 5);
            this.lotLayout.Controls.Add(this.lblNgCaption, 0, 6);
            this.lotLayout.Controls.Add(this._lblNg, 1, 6);
            this.lotLayout.Controls.Add(this.lblYieldCaption, 0, 7);
            this.lotLayout.Controls.Add(this._lblYield, 1, 7);
            this.lotLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lotLayout.Location = new System.Drawing.Point(3, 23);
            this.lotLayout.Name = "lotLayout";
            this.lotLayout.Padding = new System.Windows.Forms.Padding(14, 22, 14, 12);
            this.lotLayout.RowCount = 8;
            this.lotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.lotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.lotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.lotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.lotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.lotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.lotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.lotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.lotLayout.Size = new System.Drawing.Size(830, 423);
            this.lotLayout.TabIndex = 0;
            // 
            // lblIdCaption
            // 
            this.lblIdCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblIdCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblIdCaption.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblIdCaption.Location = new System.Drawing.Point(17, 22);
            this.lblIdCaption.Name = "lblIdCaption";
            this.lblIdCaption.Size = new System.Drawing.Size(169, 48);
            this.lblIdCaption.TabIndex = 0;
            this.lblIdCaption.Text = "Lot ID";
            this.lblIdCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblId
            // 
            this._lblId.BackColor = System.Drawing.Color.White;
            this._lblId.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblId.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblId.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            this._lblId.ForeColor = System.Drawing.Color.Black;
            this._lblId.Location = new System.Drawing.Point(192, 25);
            this._lblId.Margin = new System.Windows.Forms.Padding(3);
            this._lblId.Name = "_lblId";
            this._lblId.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblId.Size = new System.Drawing.Size(621, 42);
            this._lblId.TabIndex = 1;
            this._lblId.Text = "(none)";
            this._lblId.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecipeCaption
            // 
            this.lblRecipeCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRecipeCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblRecipeCaption.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblRecipeCaption.Location = new System.Drawing.Point(17, 70);
            this.lblRecipeCaption.Name = "lblRecipeCaption";
            this.lblRecipeCaption.Size = new System.Drawing.Size(169, 48);
            this.lblRecipeCaption.TabIndex = 2;
            this.lblRecipeCaption.Text = "Recipe";
            this.lblRecipeCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblRecipe
            // 
            this._lblRecipe.BackColor = System.Drawing.Color.White;
            this._lblRecipe.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblRecipe.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblRecipe.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            this._lblRecipe.ForeColor = System.Drawing.Color.Black;
            this._lblRecipe.Location = new System.Drawing.Point(192, 73);
            this._lblRecipe.Margin = new System.Windows.Forms.Padding(3);
            this._lblRecipe.Name = "_lblRecipe";
            this._lblRecipe.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblRecipe.Size = new System.Drawing.Size(621, 42);
            this._lblRecipe.TabIndex = 3;
            this._lblRecipe.Text = "(none)";
            this._lblRecipe.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStateCaption
            // 
            this.lblStateCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStateCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblStateCaption.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblStateCaption.Location = new System.Drawing.Point(17, 118);
            this.lblStateCaption.Name = "lblStateCaption";
            this.lblStateCaption.Size = new System.Drawing.Size(169, 48);
            this.lblStateCaption.TabIndex = 4;
            this.lblStateCaption.Text = "State";
            this.lblStateCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblState
            // 
            this._lblState.BackColor = System.Drawing.Color.White;
            this._lblState.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblState.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblState.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            this._lblState.ForeColor = System.Drawing.Color.Black;
            this._lblState.Location = new System.Drawing.Point(192, 121);
            this._lblState.Margin = new System.Windows.Forms.Padding(3);
            this._lblState.Name = "_lblState";
            this._lblState.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblState.Size = new System.Drawing.Size(621, 42);
            this._lblState.TabIndex = 5;
            this._lblState.Text = "(none)";
            this._lblState.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStartCaption
            // 
            this.lblStartCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStartCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblStartCaption.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblStartCaption.Location = new System.Drawing.Point(17, 166);
            this.lblStartCaption.Name = "lblStartCaption";
            this.lblStartCaption.Size = new System.Drawing.Size(169, 48);
            this.lblStartCaption.TabIndex = 6;
            this.lblStartCaption.Text = "Started";
            this.lblStartCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblStart
            // 
            this._lblStart.BackColor = System.Drawing.Color.White;
            this._lblStart.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblStart.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            this._lblStart.ForeColor = System.Drawing.Color.Black;
            this._lblStart.Location = new System.Drawing.Point(192, 169);
            this._lblStart.Margin = new System.Windows.Forms.Padding(3);
            this._lblStart.Name = "_lblStart";
            this._lblStart.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblStart.Size = new System.Drawing.Size(621, 42);
            this._lblStart.TabIndex = 7;
            this._lblStart.Text = "(none)";
            this._lblStart.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblProcessedCaption
            // 
            this.lblProcessedCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProcessedCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblProcessedCaption.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblProcessedCaption.Location = new System.Drawing.Point(17, 214);
            this.lblProcessedCaption.Name = "lblProcessedCaption";
            this.lblProcessedCaption.Size = new System.Drawing.Size(169, 48);
            this.lblProcessedCaption.TabIndex = 8;
            this.lblProcessedCaption.Text = "Processed / Total";
            this.lblProcessedCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblProcessed
            // 
            this._lblProcessed.BackColor = System.Drawing.Color.White;
            this._lblProcessed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblProcessed.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblProcessed.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            this._lblProcessed.ForeColor = System.Drawing.Color.Black;
            this._lblProcessed.Location = new System.Drawing.Point(192, 217);
            this._lblProcessed.Margin = new System.Windows.Forms.Padding(3);
            this._lblProcessed.Name = "_lblProcessed";
            this._lblProcessed.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblProcessed.Size = new System.Drawing.Size(621, 42);
            this._lblProcessed.TabIndex = 9;
            this._lblProcessed.Text = "(none)";
            this._lblProcessed.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblGoodCaption
            // 
            this.lblGoodCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGoodCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGoodCaption.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblGoodCaption.Location = new System.Drawing.Point(17, 262);
            this.lblGoodCaption.Name = "lblGoodCaption";
            this.lblGoodCaption.Size = new System.Drawing.Size(169, 48);
            this.lblGoodCaption.TabIndex = 10;
            this.lblGoodCaption.Text = "Good";
            this.lblGoodCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblGood
            // 
            this._lblGood.BackColor = System.Drawing.Color.White;
            this._lblGood.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblGood.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblGood.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            this._lblGood.ForeColor = System.Drawing.Color.Black;
            this._lblGood.Location = new System.Drawing.Point(192, 265);
            this._lblGood.Margin = new System.Windows.Forms.Padding(3);
            this._lblGood.Name = "_lblGood";
            this._lblGood.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblGood.Size = new System.Drawing.Size(621, 42);
            this._lblGood.TabIndex = 11;
            this._lblGood.Text = "(none)";
            this._lblGood.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNgCaption
            // 
            this.lblNgCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblNgCaption.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblNgCaption.Location = new System.Drawing.Point(17, 310);
            this.lblNgCaption.Name = "lblNgCaption";
            this.lblNgCaption.Size = new System.Drawing.Size(169, 48);
            this.lblNgCaption.TabIndex = 12;
            this.lblNgCaption.Text = "NG";
            this.lblNgCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblNg
            // 
            this._lblNg.BackColor = System.Drawing.Color.White;
            this._lblNg.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblNg.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblNg.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            this._lblNg.ForeColor = System.Drawing.Color.Black;
            this._lblNg.Location = new System.Drawing.Point(192, 313);
            this._lblNg.Margin = new System.Windows.Forms.Padding(3);
            this._lblNg.Name = "_lblNg";
            this._lblNg.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblNg.Size = new System.Drawing.Size(621, 42);
            this._lblNg.TabIndex = 13;
            this._lblNg.Text = "(none)";
            this._lblNg.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblYieldCaption
            // 
            this.lblYieldCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblYieldCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblYieldCaption.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblYieldCaption.Location = new System.Drawing.Point(17, 358);
            this.lblYieldCaption.Name = "lblYieldCaption";
            this.lblYieldCaption.Size = new System.Drawing.Size(169, 53);
            this.lblYieldCaption.TabIndex = 14;
            this.lblYieldCaption.Text = "Yield %";
            this.lblYieldCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblYield
            // 
            this._lblYield.BackColor = System.Drawing.Color.White;
            this._lblYield.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblYield.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblYield.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            this._lblYield.ForeColor = System.Drawing.Color.Black;
            this._lblYield.Location = new System.Drawing.Point(192, 361);
            this._lblYield.Margin = new System.Windows.Forms.Padding(3);
            this._lblYield.Name = "_lblYield";
            this._lblYield.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this._lblYield.Size = new System.Drawing.Size(621, 47);
            this._lblYield.TabIndex = 15;
            this._lblYield.Text = "(none)";
            this._lblYield.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpBin
            // 
            this.grpBin.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpBin.Controls.Add(this._binPanel);
            this.grpBin.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpBin.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpBin.Location = new System.Drawing.Point(842, 0);
            this.grpBin.Margin = new System.Windows.Forms.Padding(3, 0, 0, 3);
            this.grpBin.Name = "grpBin";
            this.grpBin.Size = new System.Drawing.Size(836, 449);
            this.grpBin.TabIndex = 1;
            this.grpBin.TabStop = false;
            this.grpBin.Text = "BIN DISTRIBUTION";
            // 
            // _binPanel
            // 
            this._binPanel.BackColor = System.Drawing.Color.White;
            this._binPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._binPanel.Location = new System.Drawing.Point(3, 23);
            this._binPanel.Margin = new System.Windows.Forms.Padding(8, 24, 8, 8);
            this._binPanel.Name = "_binPanel";
            this._binPanel.Size = new System.Drawing.Size(830, 423);
            this._binPanel.TabIndex = 0;
            this._binPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.OnPaintBin);
            // 
            // plateArea
            // 
            this.plateArea.ColumnCount = 2;
            this.plateArea.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.plateArea.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.plateArea.Controls.Add(this.grpNg, 0, 0);
            this.plateArea.Controls.Add(this.grpGood, 1, 0);
            this.plateArea.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plateArea.Location = new System.Drawing.Point(0, 455);
            this.plateArea.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.plateArea.Name = "plateArea";
            this.plateArea.RowCount = 1;
            this.plateArea.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.plateArea.Size = new System.Drawing.Size(836, 362);
            this.plateArea.TabIndex = 2;
            // 
            // grpNg
            // 
            this.grpNg.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpNg.Controls.Add(this.ngLayout);
            this.grpNg.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpNg.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpNg.Location = new System.Drawing.Point(0, 0);
            this.grpNg.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.grpNg.Name = "grpNg";
            this.grpNg.Size = new System.Drawing.Size(415, 362);
            this.grpNg.TabIndex = 0;
            this.grpNg.TabStop = false;
            this.grpNg.Text = "NG PLATE";
            // 
            // ngLayout
            // 
            this.ngLayout.ColumnCount = 1;
            this.ngLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ngLayout.Controls.Add(this._lblNgCount, 0, 0);
            this.ngLayout.Controls.Add(this.ngSlotLayout, 0, 1);
            this.ngLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ngLayout.Location = new System.Drawing.Point(3, 23);
            this.ngLayout.Name = "ngLayout";
            this.ngLayout.Padding = new System.Windows.Forms.Padding(6, 18, 6, 6);
            this.ngLayout.RowCount = 2;
            this.ngLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ngLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ngLayout.Size = new System.Drawing.Size(409, 336);
            this.ngLayout.TabIndex = 0;
            // 
            // _lblNgCount
            // 
            this._lblNgCount.BackColor = System.Drawing.Color.LightCoral;
            this._lblNgCount.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblNgCount.Font = new System.Drawing.Font("Consolas", 11F, System.Drawing.FontStyle.Bold);
            this._lblNgCount.Location = new System.Drawing.Point(7, 19);
            this._lblNgCount.Margin = new System.Windows.Forms.Padding(1, 1, 1, 3);
            this._lblNgCount.Name = "_lblNgCount";
            this._lblNgCount.Size = new System.Drawing.Size(395, 24);
            this._lblNgCount.TabIndex = 0;
            this._lblNgCount.Text = "0 / 25";
            this._lblNgCount.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ngSlotLayout
            // 
            this.ngSlotLayout.ColumnCount = 5;
            this.ngSlotLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.ngSlotLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.ngSlotLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.ngSlotLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.ngSlotLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.ngSlotLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ngSlotLayout.Location = new System.Drawing.Point(9, 49);
            this.ngSlotLayout.Name = "ngSlotLayout";
            this.ngSlotLayout.RowCount = 5;
            this.ngSlotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.ngSlotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.ngSlotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.ngSlotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.ngSlotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.ngSlotLayout.Size = new System.Drawing.Size(391, 278);
            this.ngSlotLayout.TabIndex = 1;
            // 
            // grpGood
            // 
            this.grpGood.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpGood.Controls.Add(this.goodLayout);
            this.grpGood.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpGood.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpGood.Location = new System.Drawing.Point(421, 0);
            this.grpGood.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.grpGood.Name = "grpGood";
            this.grpGood.Size = new System.Drawing.Size(415, 362);
            this.grpGood.TabIndex = 1;
            this.grpGood.TabStop = false;
            this.grpGood.Text = "GOOD PLATE";
            // 
            // goodLayout
            // 
            this.goodLayout.ColumnCount = 1;
            this.goodLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.goodLayout.Controls.Add(this._lblGoodCount, 0, 0);
            this.goodLayout.Controls.Add(this.goodSlotLayout, 0, 1);
            this.goodLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.goodLayout.Location = new System.Drawing.Point(3, 23);
            this.goodLayout.Name = "goodLayout";
            this.goodLayout.Padding = new System.Windows.Forms.Padding(6, 18, 6, 6);
            this.goodLayout.RowCount = 2;
            this.goodLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.goodLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.goodLayout.Size = new System.Drawing.Size(409, 336);
            this.goodLayout.TabIndex = 0;
            // 
            // _lblGoodCount
            // 
            this._lblGoodCount.BackColor = System.Drawing.Color.LightGreen;
            this._lblGoodCount.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblGoodCount.Font = new System.Drawing.Font("Consolas", 11F, System.Drawing.FontStyle.Bold);
            this._lblGoodCount.Location = new System.Drawing.Point(7, 19);
            this._lblGoodCount.Margin = new System.Windows.Forms.Padding(1, 1, 1, 3);
            this._lblGoodCount.Name = "_lblGoodCount";
            this._lblGoodCount.Size = new System.Drawing.Size(395, 24);
            this._lblGoodCount.TabIndex = 0;
            this._lblGoodCount.Text = "0 / 25";
            this._lblGoodCount.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // goodSlotLayout
            // 
            this.goodSlotLayout.ColumnCount = 5;
            this.goodSlotLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.goodSlotLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.goodSlotLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.goodSlotLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.goodSlotLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.goodSlotLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.goodSlotLayout.Location = new System.Drawing.Point(9, 49);
            this.goodSlotLayout.Name = "goodSlotLayout";
            this.goodSlotLayout.RowCount = 5;
            this.goodSlotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.goodSlotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.goodSlotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.goodSlotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.goodSlotLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.goodSlotLayout.Size = new System.Drawing.Size(391, 278);
            this.goodSlotLayout.TabIndex = 1;
            // 
            // sensorGrid
            // 
            this.sensorGrid.ColumnCount = 2;
            this.sensorGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.sensorGrid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.sensorGrid.Controls.Add(this.leftSensors, 0, 0);
            this.sensorGrid.Controls.Add(this.rightSensors, 1, 0);
            this.sensorGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorGrid.Location = new System.Drawing.Point(842, 455);
            this.sensorGrid.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
            this.sensorGrid.Name = "sensorGrid";
            this.sensorGrid.RowCount = 1;
            this.contentLayout.SetRowSpan(this.sensorGrid, 2);
            this.sensorGrid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorGrid.Size = new System.Drawing.Size(836, 415);
            this.sensorGrid.TabIndex = 3;
            // 
            // leftSensors
            // 
            this.leftSensors.ColumnCount = 1;
            this.leftSensors.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftSensors.Controls.Add(this.grpButtons, 0, 0);
            this.leftSensors.Controls.Add(this.grpResources, 0, 1);
            this.leftSensors.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftSensors.Location = new System.Drawing.Point(0, 0);
            this.leftSensors.Margin = new System.Windows.Forms.Padding(0);
            this.leftSensors.Name = "leftSensors";
            this.leftSensors.RowCount = 2;
            this.leftSensors.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.leftSensors.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.leftSensors.Size = new System.Drawing.Size(418, 415);
            this.leftSensors.TabIndex = 0;
            // 
            // grpButtons
            // 
            this.grpButtons.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpButtons.Controls.Add(this.buttonLayout);
            this.grpButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpButtons.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpButtons.Location = new System.Drawing.Point(0, 0);
            this.grpButtons.Margin = new System.Windows.Forms.Padding(0, 0, 3, 3);
            this.grpButtons.Name = "grpButtons";
            this.grpButtons.Size = new System.Drawing.Size(415, 204);
            this.grpButtons.TabIndex = 0;
            this.grpButtons.TabStop = false;
            this.grpButtons.Text = "OPERATION BUTTONS (DI)";
            // 
            // buttonLayout
            // 
            this.buttonLayout.ColumnCount = 2;
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonLayout.Controls.Add(this._dotStart, 0, 0);
            this.buttonLayout.Controls.Add(this.lblStart, 1, 0);
            this.buttonLayout.Controls.Add(this._dotStop, 0, 1);
            this.buttonLayout.Controls.Add(this.lblStop, 1, 1);
            this.buttonLayout.Controls.Add(this._dotReset, 0, 2);
            this.buttonLayout.Controls.Add(this.lblReset, 1, 2);
            this.buttonLayout.Controls.Add(this._dotEmgF, 0, 3);
            this.buttonLayout.Controls.Add(this.lblEmgF, 1, 3);
            this.buttonLayout.Controls.Add(this._dotEmgL, 0, 4);
            this.buttonLayout.Controls.Add(this.lblEmgL, 1, 4);
            this.buttonLayout.Controls.Add(this._dotEmgR, 0, 5);
            this.buttonLayout.Controls.Add(this.lblEmgR, 1, 5);
            this.buttonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonLayout.Location = new System.Drawing.Point(3, 23);
            this.buttonLayout.Name = "buttonLayout";
            this.buttonLayout.Padding = new System.Windows.Forms.Padding(10, 4, 8, 4);
            this.buttonLayout.RowCount = 6;
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.buttonLayout.Size = new System.Drawing.Size(409, 178);
            this.buttonLayout.TabIndex = 0;
            // 
            // _dotStart
            // 
            this._dotStart.BackColor = System.Drawing.Color.Transparent;
            this._dotStart.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._dotStart.Location = new System.Drawing.Point(13, 9);
            this._dotStart.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._dotStart.Name = "_dotStart";
            this._dotStart.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotStart.OnColor = System.Drawing.Color.LimeGreen;
            this._dotStart.Size = new System.Drawing.Size(26, 26);
            this._dotStart.TabIndex = 0;
            // 
            // lblStart
            // 
            this.lblStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStart.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStart.Location = new System.Drawing.Point(41, 4);
            this.lblStart.Name = "lblStart";
            this.lblStart.Size = new System.Drawing.Size(357, 28);
            this.lblStart.TabIndex = 1;
            this.lblStart.Text = "START";
            this.lblStart.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotStop
            // 
            this._dotStop.BackColor = System.Drawing.Color.Transparent;
            this._dotStop.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._dotStop.Location = new System.Drawing.Point(13, 37);
            this._dotStop.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._dotStop.Name = "_dotStop";
            this._dotStop.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotStop.OnColor = System.Drawing.Color.LimeGreen;
            this._dotStop.Size = new System.Drawing.Size(26, 26);
            this._dotStop.TabIndex = 2;
            // 
            // lblStop
            // 
            this.lblStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStop.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStop.Location = new System.Drawing.Point(41, 32);
            this.lblStop.Name = "lblStop";
            this.lblStop.Size = new System.Drawing.Size(357, 28);
            this.lblStop.TabIndex = 3;
            this.lblStop.Text = "STOP";
            this.lblStop.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotReset
            // 
            this._dotReset.BackColor = System.Drawing.Color.Transparent;
            this._dotReset.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._dotReset.Location = new System.Drawing.Point(13, 65);
            this._dotReset.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._dotReset.Name = "_dotReset";
            this._dotReset.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotReset.OnColor = System.Drawing.Color.LimeGreen;
            this._dotReset.Size = new System.Drawing.Size(26, 26);
            this._dotReset.TabIndex = 4;
            // 
            // lblReset
            // 
            this.lblReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblReset.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblReset.Location = new System.Drawing.Point(41, 60);
            this.lblReset.Name = "lblReset";
            this.lblReset.Size = new System.Drawing.Size(357, 28);
            this.lblReset.TabIndex = 5;
            this.lblReset.Text = "RESET";
            this.lblReset.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotEmgF
            // 
            this._dotEmgF.BackColor = System.Drawing.Color.Transparent;
            this._dotEmgF.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._dotEmgF.Location = new System.Drawing.Point(13, 93);
            this._dotEmgF.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._dotEmgF.Name = "_dotEmgF";
            this._dotEmgF.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotEmgF.OnColor = System.Drawing.Color.Red;
            this._dotEmgF.Size = new System.Drawing.Size(26, 26);
            this._dotEmgF.TabIndex = 6;
            // 
            // lblEmgF
            // 
            this.lblEmgF.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEmgF.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblEmgF.Location = new System.Drawing.Point(41, 88);
            this.lblEmgF.Name = "lblEmgF";
            this.lblEmgF.Size = new System.Drawing.Size(357, 28);
            this.lblEmgF.TabIndex = 7;
            this.lblEmgF.Text = "EMG Front";
            this.lblEmgF.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotEmgL
            // 
            this._dotEmgL.BackColor = System.Drawing.Color.Transparent;
            this._dotEmgL.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._dotEmgL.Location = new System.Drawing.Point(13, 121);
            this._dotEmgL.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._dotEmgL.Name = "_dotEmgL";
            this._dotEmgL.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotEmgL.OnColor = System.Drawing.Color.Red;
            this._dotEmgL.Size = new System.Drawing.Size(26, 26);
            this._dotEmgL.TabIndex = 8;
            // 
            // lblEmgL
            // 
            this.lblEmgL.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEmgL.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblEmgL.Location = new System.Drawing.Point(41, 116);
            this.lblEmgL.Name = "lblEmgL";
            this.lblEmgL.Size = new System.Drawing.Size(357, 28);
            this.lblEmgL.TabIndex = 9;
            this.lblEmgL.Text = "EMG Left";
            this.lblEmgL.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotEmgR
            // 
            this._dotEmgR.BackColor = System.Drawing.Color.Transparent;
            this._dotEmgR.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._dotEmgR.Location = new System.Drawing.Point(13, 149);
            this._dotEmgR.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._dotEmgR.Name = "_dotEmgR";
            this._dotEmgR.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotEmgR.OnColor = System.Drawing.Color.Red;
            this._dotEmgR.Size = new System.Drawing.Size(26, 26);
            this._dotEmgR.TabIndex = 10;
            // 
            // lblEmgR
            // 
            this.lblEmgR.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEmgR.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblEmgR.Location = new System.Drawing.Point(41, 144);
            this.lblEmgR.Name = "lblEmgR";
            this.lblEmgR.Size = new System.Drawing.Size(357, 30);
            this.lblEmgR.TabIndex = 11;
            this.lblEmgR.Text = "EMG Rear";
            this.lblEmgR.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpResources
            // 
            this.grpResources.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpResources.Controls.Add(this.resourceLayout);
            this.grpResources.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpResources.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpResources.Location = new System.Drawing.Point(0, 210);
            this.grpResources.Margin = new System.Windows.Forms.Padding(0, 3, 3, 0);
            this.grpResources.Name = "grpResources";
            this.grpResources.Size = new System.Drawing.Size(415, 205);
            this.grpResources.TabIndex = 2;
            this.grpResources.TabStop = false;
            this.grpResources.Text = "RESOURCE SENSORS";
            // 
            // resourceLayout
            // 
            this.resourceLayout.ColumnCount = 2;
            this.resourceLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.resourceLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.resourceLayout.Controls.Add(this._dotCda1, 0, 0);
            this.resourceLayout.Controls.Add(this.lblCda1, 1, 0);
            this.resourceLayout.Controls.Add(this._dotCda2, 0, 1);
            this.resourceLayout.Controls.Add(this.lblCda2, 1, 1);
            this.resourceLayout.Controls.Add(this._dotVac1, 0, 2);
            this.resourceLayout.Controls.Add(this.lblVac1, 1, 2);
            this.resourceLayout.Controls.Add(this._dotVac2, 0, 3);
            this.resourceLayout.Controls.Add(this.lblVac2, 1, 3);
            this.resourceLayout.Controls.Add(this._dotVac3, 0, 4);
            this.resourceLayout.Controls.Add(this.lblVac3, 1, 4);
            this.resourceLayout.Controls.Add(this._dotVac4, 0, 5);
            this.resourceLayout.Controls.Add(this.lblVac4, 1, 5);
            this.resourceLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resourceLayout.Location = new System.Drawing.Point(3, 23);
            this.resourceLayout.Name = "resourceLayout";
            this.resourceLayout.Padding = new System.Windows.Forms.Padding(10, 4, 8, 4);
            this.resourceLayout.RowCount = 6;
            this.resourceLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.resourceLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.resourceLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.resourceLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.resourceLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.resourceLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.resourceLayout.Size = new System.Drawing.Size(409, 179);
            this.resourceLayout.TabIndex = 0;
            // 
            // _dotCda1
            // 
            this._dotCda1.BackColor = System.Drawing.Color.Transparent;
            this._dotCda1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._dotCda1.Location = new System.Drawing.Point(13, 9);
            this._dotCda1.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._dotCda1.Name = "_dotCda1";
            this._dotCda1.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotCda1.OnColor = System.Drawing.Color.Cyan;
            this._dotCda1.Size = new System.Drawing.Size(26, 26);
            this._dotCda1.TabIndex = 0;
            // 
            // lblCda1
            // 
            this.lblCda1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCda1.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblCda1.Location = new System.Drawing.Point(41, 4);
            this.lblCda1.Name = "lblCda1";
            this.lblCda1.Size = new System.Drawing.Size(357, 28);
            this.lblCda1.TabIndex = 1;
            this.lblCda1.Text = "MAIN CDA 1";
            this.lblCda1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotCda2
            // 
            this._dotCda2.BackColor = System.Drawing.Color.Transparent;
            this._dotCda2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._dotCda2.Location = new System.Drawing.Point(13, 37);
            this._dotCda2.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._dotCda2.Name = "_dotCda2";
            this._dotCda2.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotCda2.OnColor = System.Drawing.Color.Cyan;
            this._dotCda2.Size = new System.Drawing.Size(26, 26);
            this._dotCda2.TabIndex = 2;
            // 
            // lblCda2
            // 
            this.lblCda2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCda2.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblCda2.Location = new System.Drawing.Point(41, 32);
            this.lblCda2.Name = "lblCda2";
            this.lblCda2.Size = new System.Drawing.Size(357, 28);
            this.lblCda2.TabIndex = 3;
            this.lblCda2.Text = "MAIN CDA 2";
            this.lblCda2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotVac1
            // 
            this._dotVac1.BackColor = System.Drawing.Color.Transparent;
            this._dotVac1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._dotVac1.Location = new System.Drawing.Point(13, 65);
            this._dotVac1.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._dotVac1.Name = "_dotVac1";
            this._dotVac1.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotVac1.OnColor = System.Drawing.Color.LimeGreen;
            this._dotVac1.Size = new System.Drawing.Size(26, 26);
            this._dotVac1.TabIndex = 4;
            // 
            // lblVac1
            // 
            this.lblVac1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVac1.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblVac1.Location = new System.Drawing.Point(41, 60);
            this.lblVac1.Name = "lblVac1";
            this.lblVac1.Size = new System.Drawing.Size(357, 28);
            this.lblVac1.TabIndex = 5;
            this.lblVac1.Text = "VACUUM 1";
            this.lblVac1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotVac2
            // 
            this._dotVac2.BackColor = System.Drawing.Color.Transparent;
            this._dotVac2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._dotVac2.Location = new System.Drawing.Point(13, 93);
            this._dotVac2.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._dotVac2.Name = "_dotVac2";
            this._dotVac2.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotVac2.OnColor = System.Drawing.Color.LimeGreen;
            this._dotVac2.Size = new System.Drawing.Size(26, 26);
            this._dotVac2.TabIndex = 6;
            // 
            // lblVac2
            // 
            this.lblVac2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVac2.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblVac2.Location = new System.Drawing.Point(41, 88);
            this.lblVac2.Name = "lblVac2";
            this.lblVac2.Size = new System.Drawing.Size(357, 28);
            this.lblVac2.TabIndex = 7;
            this.lblVac2.Text = "VACUUM 2";
            this.lblVac2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotVac3
            // 
            this._dotVac3.BackColor = System.Drawing.Color.Transparent;
            this._dotVac3.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._dotVac3.Location = new System.Drawing.Point(13, 121);
            this._dotVac3.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._dotVac3.Name = "_dotVac3";
            this._dotVac3.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotVac3.OnColor = System.Drawing.Color.LimeGreen;
            this._dotVac3.Size = new System.Drawing.Size(26, 26);
            this._dotVac3.TabIndex = 8;
            // 
            // lblVac3
            // 
            this.lblVac3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVac3.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblVac3.Location = new System.Drawing.Point(41, 116);
            this.lblVac3.Name = "lblVac3";
            this.lblVac3.Size = new System.Drawing.Size(357, 28);
            this.lblVac3.TabIndex = 9;
            this.lblVac3.Text = "VACUUM 3";
            this.lblVac3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dotVac4
            // 
            this._dotVac4.BackColor = System.Drawing.Color.Transparent;
            this._dotVac4.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._dotVac4.Location = new System.Drawing.Point(13, 149);
            this._dotVac4.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._dotVac4.Name = "_dotVac4";
            this._dotVac4.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotVac4.OnColor = System.Drawing.Color.LimeGreen;
            this._dotVac4.Size = new System.Drawing.Size(26, 26);
            this._dotVac4.TabIndex = 10;
            // 
            // lblVac4
            // 
            this.lblVac4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVac4.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblVac4.Location = new System.Drawing.Point(41, 144);
            this.lblVac4.Name = "lblVac4";
            this.lblVac4.Size = new System.Drawing.Size(357, 31);
            this.lblVac4.TabIndex = 11;
            this.lblVac4.Text = "VACUUM 4";
            this.lblVac4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // rightSensors
            // 
            this.rightSensors.ColumnCount = 1;
            this.rightSensors.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightSensors.Controls.Add(this.grpLamps, 0, 0);
            this.rightSensors.Controls.Add(this.grpTower, 0, 1);
            this.rightSensors.Controls.Add(this.grpIonizer, 0, 2);
            this.rightSensors.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightSensors.Location = new System.Drawing.Point(418, 0);
            this.rightSensors.Margin = new System.Windows.Forms.Padding(0);
            this.rightSensors.Name = "rightSensors";
            this.rightSensors.RowCount = 3;
            this.rightSensors.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.rightSensors.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45.5F));
            this.rightSensors.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 19.5F));
            this.rightSensors.Size = new System.Drawing.Size(418, 415);
            this.rightSensors.TabIndex = 1;
            // 
            // grpLamps
            // 
            this.grpLamps.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpLamps.Controls.Add(this.lampLayout);
            this.grpLamps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLamps.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpLamps.Location = new System.Drawing.Point(3, 0);
            this.grpLamps.Margin = new System.Windows.Forms.Padding(3, 0, 0, 3);
            this.grpLamps.Name = "grpLamps";
            this.grpLamps.Size = new System.Drawing.Size(415, 142);
            this.grpLamps.TabIndex = 1;
            this.grpLamps.TabStop = false;
            this.grpLamps.Text = "OPERATION LAMPS (DO)";
            // 
            // lampLayout
            // 
            this.lampLayout.ColumnCount = 2;
            this.lampLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.lampLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lampLayout.Controls.Add(this._ledStartLamp, 0, 0);
            this.lampLayout.Controls.Add(this.lblStartLamp, 1, 0);
            this.lampLayout.Controls.Add(this._ledStopLamp, 0, 1);
            this.lampLayout.Controls.Add(this.lblStopLamp, 1, 1);
            this.lampLayout.Controls.Add(this._ledResetLamp, 0, 2);
            this.lampLayout.Controls.Add(this.lblResetLamp, 1, 2);
            this.lampLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lampLayout.Location = new System.Drawing.Point(3, 23);
            this.lampLayout.Name = "lampLayout";
            this.lampLayout.Padding = new System.Windows.Forms.Padding(10, 4, 8, 4);
            this.lampLayout.RowCount = 3;
            this.lampLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.lampLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.lampLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.lampLayout.Size = new System.Drawing.Size(409, 116);
            this.lampLayout.TabIndex = 0;
            // 
            // _ledStartLamp
            // 
            this._ledStartLamp.BackColor = System.Drawing.Color.Transparent;
            this._ledStartLamp.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._ledStartLamp.Location = new System.Drawing.Point(13, 9);
            this._ledStartLamp.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._ledStartLamp.Name = "_ledStartLamp";
            this._ledStartLamp.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._ledStartLamp.OnColor = System.Drawing.Color.LimeGreen;
            this._ledStartLamp.Size = new System.Drawing.Size(26, 26);
            this._ledStartLamp.TabIndex = 0;
            // 
            // lblStartLamp
            // 
            this.lblStartLamp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStartLamp.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStartLamp.Location = new System.Drawing.Point(41, 4);
            this.lblStartLamp.Name = "lblStartLamp";
            this.lblStartLamp.Size = new System.Drawing.Size(357, 36);
            this.lblStartLamp.TabIndex = 1;
            this.lblStartLamp.Text = "START LAMP";
            this.lblStartLamp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _ledStopLamp
            // 
            this._ledStopLamp.BackColor = System.Drawing.Color.Transparent;
            this._ledStopLamp.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._ledStopLamp.Location = new System.Drawing.Point(13, 45);
            this._ledStopLamp.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._ledStopLamp.Name = "_ledStopLamp";
            this._ledStopLamp.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._ledStopLamp.OnColor = System.Drawing.Color.LimeGreen;
            this._ledStopLamp.Size = new System.Drawing.Size(26, 26);
            this._ledStopLamp.TabIndex = 2;
            // 
            // lblStopLamp
            // 
            this.lblStopLamp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStopLamp.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStopLamp.Location = new System.Drawing.Point(41, 40);
            this.lblStopLamp.Name = "lblStopLamp";
            this.lblStopLamp.Size = new System.Drawing.Size(357, 36);
            this.lblStopLamp.TabIndex = 3;
            this.lblStopLamp.Text = "STOP LAMP";
            this.lblStopLamp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _ledResetLamp
            // 
            this._ledResetLamp.BackColor = System.Drawing.Color.Transparent;
            this._ledResetLamp.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._ledResetLamp.Location = new System.Drawing.Point(13, 81);
            this._ledResetLamp.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._ledResetLamp.Name = "_ledResetLamp";
            this._ledResetLamp.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._ledResetLamp.OnColor = System.Drawing.Color.LimeGreen;
            this._ledResetLamp.Size = new System.Drawing.Size(26, 26);
            this._ledResetLamp.TabIndex = 4;
            // 
            // lblResetLamp
            // 
            this.lblResetLamp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblResetLamp.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblResetLamp.Location = new System.Drawing.Point(41, 76);
            this.lblResetLamp.Name = "lblResetLamp";
            this.lblResetLamp.Size = new System.Drawing.Size(357, 36);
            this.lblResetLamp.TabIndex = 5;
            this.lblResetLamp.Text = "RESET LAMP";
            this.lblResetLamp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpTower
            // 
            this.grpTower.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpTower.Controls.Add(this.towerLayout);
            this.grpTower.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpTower.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpTower.Location = new System.Drawing.Point(3, 148);
            this.grpTower.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.grpTower.Name = "grpTower";
            this.grpTower.Size = new System.Drawing.Size(415, 182);
            this.grpTower.TabIndex = 3;
            this.grpTower.TabStop = false;
            this.grpTower.Text = "TOWER LAMP + BUZZER";
            // 
            // towerLayout
            // 
            this.towerLayout.ColumnCount = 2;
            this.towerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 41F));
            this.towerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.towerLayout.Controls.Add(this._tlRed, 0, 0);
            this.towerLayout.Controls.Add(this.lblTlRed, 1, 0);
            this.towerLayout.Controls.Add(this._tlYellow, 0, 1);
            this.towerLayout.Controls.Add(this.lblTlYellow, 1, 1);
            this.towerLayout.Controls.Add(this._tlGreen, 0, 2);
            this.towerLayout.Controls.Add(this.lblTlGreen, 1, 2);
            this.towerLayout.Controls.Add(this._ledBuzzer, 0, 3);
            this.towerLayout.Controls.Add(this.lblBuzzer, 1, 3);
            this.towerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.towerLayout.Location = new System.Drawing.Point(3, 23);
            this.towerLayout.Name = "towerLayout";
            this.towerLayout.Padding = new System.Windows.Forms.Padding(10, 4, 8, 4);
            this.towerLayout.RowCount = 4;
            this.towerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.towerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.towerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.towerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.towerLayout.Size = new System.Drawing.Size(409, 156);
            this.towerLayout.TabIndex = 0;
            // 
            // _tlRed
            // 
            this._tlRed.BackColor = System.Drawing.Color.Transparent;
            this._tlRed.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._tlRed.Location = new System.Drawing.Point(13, 9);
            this._tlRed.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._tlRed.Name = "_tlRed";
            this._tlRed.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._tlRed.OnColor = System.Drawing.Color.Red;
            this._tlRed.Size = new System.Drawing.Size(26, 26);
            this._tlRed.TabIndex = 0;
            // 
            // lblTlRed
            // 
            this.lblTlRed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTlRed.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblTlRed.Location = new System.Drawing.Point(41, 4);
            this.lblTlRed.Name = "lblTlRed";
            this.lblTlRed.Size = new System.Drawing.Size(357, 37);
            this.lblTlRed.TabIndex = 1;
            this.lblTlRed.Text = "RED";
            this.lblTlRed.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tlYellow
            // 
            this._tlYellow.BackColor = System.Drawing.Color.Transparent;
            this._tlYellow.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._tlYellow.Location = new System.Drawing.Point(13, 46);
            this._tlYellow.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._tlYellow.Name = "_tlYellow";
            this._tlYellow.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._tlYellow.OnColor = System.Drawing.Color.Goldenrod;
            this._tlYellow.Size = new System.Drawing.Size(26, 26);
            this._tlYellow.TabIndex = 2;
            // 
            // lblTlYellow
            // 
            this.lblTlYellow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTlYellow.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblTlYellow.Location = new System.Drawing.Point(41, 41);
            this.lblTlYellow.Name = "lblTlYellow";
            this.lblTlYellow.Size = new System.Drawing.Size(357, 37);
            this.lblTlYellow.TabIndex = 3;
            this.lblTlYellow.Text = "YELLOW";
            this.lblTlYellow.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tlGreen
            // 
            this._tlGreen.BackColor = System.Drawing.Color.Transparent;
            this._tlGreen.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._tlGreen.Location = new System.Drawing.Point(13, 83);
            this._tlGreen.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._tlGreen.Name = "_tlGreen";
            this._tlGreen.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._tlGreen.OnColor = System.Drawing.Color.LimeGreen;
            this._tlGreen.Size = new System.Drawing.Size(26, 26);
            this._tlGreen.TabIndex = 4;
            // 
            // lblTlGreen
            // 
            this.lblTlGreen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTlGreen.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblTlGreen.Location = new System.Drawing.Point(41, 78);
            this.lblTlGreen.Name = "lblTlGreen";
            this.lblTlGreen.Size = new System.Drawing.Size(357, 37);
            this.lblTlGreen.TabIndex = 5;
            this.lblTlGreen.Text = "GREEN";
            this.lblTlGreen.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _ledBuzzer
            // 
            this._ledBuzzer.BackColor = System.Drawing.Color.Transparent;
            this._ledBuzzer.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._ledBuzzer.Location = new System.Drawing.Point(13, 120);
            this._ledBuzzer.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._ledBuzzer.Name = "_ledBuzzer";
            this._ledBuzzer.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._ledBuzzer.OnColor = System.Drawing.Color.Magenta;
            this._ledBuzzer.Size = new System.Drawing.Size(26, 26);
            this._ledBuzzer.TabIndex = 6;
            // 
            // lblBuzzer
            // 
            this.lblBuzzer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBuzzer.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblBuzzer.Location = new System.Drawing.Point(41, 115);
            this.lblBuzzer.Name = "lblBuzzer";
            this.lblBuzzer.Size = new System.Drawing.Size(357, 37);
            this.lblBuzzer.TabIndex = 7;
            this.lblBuzzer.Text = "BUZZER";
            this.lblBuzzer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpIonizer
            // 
            this.grpIonizer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpIonizer.Controls.Add(this.ionizerLayout);
            this.grpIonizer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpIonizer.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpIonizer.Location = new System.Drawing.Point(3, 336);
            this.grpIonizer.Margin = new System.Windows.Forms.Padding(3, 3, 0, 0);
            this.grpIonizer.Name = "grpIonizer";
            this.grpIonizer.Size = new System.Drawing.Size(415, 79);
            this.grpIonizer.TabIndex = 4;
            this.grpIonizer.TabStop = false;
            this.grpIonizer.Text = "IONIZER";
            // 
            // ionizerLayout
            // 
            this.ionizerLayout.ColumnCount = 2;
            this.ionizerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 41F));
            this.ionizerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ionizerLayout.Controls.Add(this._dotIonizer, 0, 0);
            this.ionizerLayout.Controls.Add(this.lblIonizer, 1, 0);
            this.ionizerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ionizerLayout.Location = new System.Drawing.Point(3, 23);
            this.ionizerLayout.Name = "ionizerLayout";
            this.ionizerLayout.Padding = new System.Windows.Forms.Padding(10, 4, 8, 4);
            this.ionizerLayout.RowCount = 1;
            this.ionizerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ionizerLayout.Size = new System.Drawing.Size(409, 53);
            this.ionizerLayout.TabIndex = 0;
            // 
            // _dotIonizer
            // 
            this._dotIonizer.BackColor = System.Drawing.Color.Transparent;
            this._dotIonizer.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._dotIonizer.Location = new System.Drawing.Point(13, 9);
            this._dotIonizer.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this._dotIonizer.Name = "_dotIonizer";
            this._dotIonizer.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this._dotIonizer.OnColor = System.Drawing.Color.Cyan;
            this._dotIonizer.Size = new System.Drawing.Size(26, 26);
            this._dotIonizer.TabIndex = 0;
            // 
            // lblIonizer
            // 
            this.lblIonizer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblIonizer.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblIonizer.Location = new System.Drawing.Point(41, 4);
            this.lblIonizer.Name = "lblIonizer";
            this.lblIonizer.Size = new System.Drawing.Size(357, 45);
            this.lblIonizer.TabIndex = 1;
            this.lblIonizer.Text = "IONIZER OK";
            this.lblIonizer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnReset
            // 
            this.btnReset.BackColor = System.Drawing.Color.LightYellow;
            this.btnReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReset.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btnReset.Location = new System.Drawing.Point(0, 823);
            this.btnReset.Margin = new System.Windows.Forms.Padding(0, 3, 3, 0);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(836, 47);
            this.btnReset.TabIndex = 4;
            this.btnReset.Text = "PLATE RESET";
            this.btnReset.UseVisualStyleBackColor = false;
            // 
            // StatePage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "StatePage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.grpActiveLot.ResumeLayout(false);
            this.lotLayout.ResumeLayout(false);
            this.grpBin.ResumeLayout(false);
            this.plateArea.ResumeLayout(false);
            this.grpNg.ResumeLayout(false);
            this.ngLayout.ResumeLayout(false);
            this.grpGood.ResumeLayout(false);
            this.goodLayout.ResumeLayout(false);
            this.sensorGrid.ResumeLayout(false);
            this.leftSensors.ResumeLayout(false);
            this.grpButtons.ResumeLayout(false);
            this.buttonLayout.ResumeLayout(false);
            this.grpResources.ResumeLayout(false);
            this.resourceLayout.ResumeLayout(false);
            this.rightSensors.ResumeLayout(false);
            this.grpLamps.ResumeLayout(false);
            this.lampLayout.ResumeLayout(false);
            this.grpTower.ResumeLayout(false);
            this.towerLayout.ResumeLayout(false);
            this.grpIonizer.ResumeLayout(false);
            this.ionizerLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
