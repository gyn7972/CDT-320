using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Work
{
    partial class WorkMainPage
    {
        private TableLayoutPanel rootLayout;
        private GroupBox grpVision;
        private GroupBox grpMap;
        private GroupBox grpInfo;
        private GroupBox grpTime;
        private TableLayoutPanel mapBody;
        private Panel visionPanel;
        private Label lblStageInfo;
        private TableLayoutPanel visionShellLayout;
        private TableLayoutPanel visionMainLayout;
        private TableLayoutPanel visionLeftLayout;
        private TableLayoutPanel visionSideLayout;
        private Panel pnlWaferVision;
        private Panel pnlBottomInspVision;
        private Panel pnlSideRearVision;
        private Panel pnlSideFrontVision;
        private Panel pnlBinVision;
        private Label lblBottomInspInfo;
        private Label lblSideRearInfo;
        private Label lblSideFrontInfo;
        private Label lblBinVisionInfo;
        private TabControl mapTabControl;
        private TabPage tabInputMap;
        private TabPage tabOutputGoodMap;
        private TabPage tabOutputNgMap;
        private LiveLotMapView lotMapView;
        private LiveLotMapView outputGoodLotMapView;
        private LiveLotMapView outputNgLotMapView;
        private TableLayoutPanel mapHeaderLayout;
        private Panel mapHeaderTotalTile;
        private Panel mapHeaderBinTile;
        private Label lblTotalChipCaption;
        private Label lblTotalChip;
        private Label lblBinNumCaption;
        private Label lblBinNum;
        private Label lblVisionCaption;
        private Label lblPickCaption;
        private Label lblPlaceCaption;
        private TableLayoutPanel workInfoBody;
        private TableLayoutPanel workTimeBody;
        private TableLayoutPanel workTimeActionPanel;
        private Label lblProjectCaption;
        private Label lblProject;
        private Label lblPickFailCaption;
        private Label lblPickFail;
        private Label lblBinQtyCaption;
        private Label lblBinQty;
        private Label lblCollet1Caption;
        private Label lblCollet1;
        private Label lblPlaceFailCaption;
        private Label lblPlaceFail;
        private Label lblNeedleCaption;
        private Label lblNeedle;
        private Label lblCollet2Caption;
        private Label lblCollet2;
        private Label lblBinArrMonCaption;
        private Label lblBinArrMon;
        private Label lblLoadCaption;
        private Label lblLoad;
        private Label lblUpCaption;
        private Label lblUp;
        private Label lblContUpCaption;
        private Label lblContUp;
        private Label lblNormDownCaption;
        private Label lblNormDown;
        private Label lblErrDownCaption;
        private Label lblErrDown;
        private Label lblErrCntCaption;
        private Label lblErrCnt;
        private Label lblRecoveryCaption;
        private Label lblRecovery;
        private Label lblUphCaption;
        private Label lblUph;
        private Label lblRecentMinuteUph;
        private Label lblMtbfCaption;
        private Label lblMtbf;
        private Label lblMttrCaption;
        private Label lblMttr;
        private Label lblCycleCaption;
        private Label lblCycle;
        private Label lblRateCaption;
        private Label lblRate;
        private Label lblLotCaption;
        private Label lblLot;
        private Button btnCcs;
        private Button btnWorkTimeClear;
        private Button btnTestAlarm;
        // LOT 관리 (2026-07-27 신규): 작업 정보 상단에 입력줄 한 줄만 둔다.
        // 진행 이력은 화면에 상주시키면 기존 타일 높이가 부족해 값이 잘리므로
        // [이력] 버튼 -> LotHistoryDialog 별도 창으로 뺐다.
        private TableLayoutPanel lotInputPanel;
        private Label lblLotIdCaption;
        private TextBox txtLotId;
        private Button btnLotStart;
        private Button btnLotComplete;
        private Button btnLotHistory;
        private Button btnBinSelect;
        private Panel workInfoProjectTile;
        private Panel workInfoBinQtyTile;
        private Panel workInfoPickFailTile;
        private Panel workInfoPlaceFailTile;
        private Panel workInfoNeedleTile;
        private Panel workInfoFrontCollet1Tile;
        private Panel workInfoFrontCollet2Tile;
        private Panel workInfoFrontCollet3Tile;
        private Panel workInfoFrontCollet4Tile;
        private Panel workInfoRearCollet1Tile;
        private Panel workInfoRearCollet2Tile;
        private Panel workInfoRearCollet3Tile;
        private Panel workInfoRearCollet4Tile;
        private Label lblFrontCollet1CaptionDesigner;
        private Label lblFrontCollet2CaptionDesigner;
        private Label lblFrontCollet3CaptionDesigner;
        private Label lblFrontCollet4CaptionDesigner;
        private Label lblRearCollet1CaptionDesigner;
        private Label lblRearCollet2CaptionDesigner;
        private Label lblRearCollet3CaptionDesigner;
        private Label lblRearCollet4CaptionDesigner;
        private Label lblFrontCollet1Designer;
        private Label lblFrontCollet2Designer;
        private Label lblFrontCollet3Designer;
        private Label lblFrontCollet4Designer;
        private Label lblRearCollet1Designer;
        private Label lblRearCollet2Designer;
        private Label lblRearCollet3Designer;
        private Label lblRearCollet4Designer;
        private Panel workTimeLotTile;
        private Panel workTimeUphTile;
        private TableLayoutPanel workTimeUphHeaderLayout;
        private TableLayoutPanel workTimeUphValueLayout;
        private Panel workTimeUpTile;
        private Panel workTimeContUpTile;
        private Panel workTimeRateTile;
        private Panel workTimeCycleTile;
        private Panel workTimeLoadTile;
        private Panel workTimeMtbfTile;
        private Panel workTimeMttrTile;
        private Panel workTimeRecoveryTile;
        private Panel workTimeNormDownTile;
        private Panel workTimeErrDownTile;
        private Panel workTimeErrCntTile;

        private static readonly Color AccentColor = Color.FromArgb(217, 119, 6);

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpVision = new System.Windows.Forms.GroupBox();
            this.visionPanel = new System.Windows.Forms.Panel();
            this.visionShellLayout = new System.Windows.Forms.TableLayoutPanel();
            this.visionMainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.visionLeftLayout = new System.Windows.Forms.TableLayoutPanel();
            this.pnlWaferVision = new System.Windows.Forms.Panel();
            this.lblStageInfo = new System.Windows.Forms.Label();
            this.pnlBinVision = new System.Windows.Forms.Panel();
            this.lblBinVisionInfo = new System.Windows.Forms.Label();
            this.visionSideLayout = new System.Windows.Forms.TableLayoutPanel();
            this.pnlSideRearVision = new System.Windows.Forms.Panel();
            this.lblSideRearInfo = new System.Windows.Forms.Label();
            this.pnlBottomInspVision = new System.Windows.Forms.Panel();
            this.lblBottomInspInfo = new System.Windows.Forms.Label();
            this.pnlSideFrontVision = new System.Windows.Forms.Panel();
            this.lblSideFrontInfo = new System.Windows.Forms.Label();
            this.grpMap = new System.Windows.Forms.GroupBox();
            this.mapBody = new System.Windows.Forms.TableLayoutPanel();
            this.mapHeaderLayout = new System.Windows.Forms.TableLayoutPanel();
            this.mapHeaderTotalTile = new System.Windows.Forms.Panel();
            this.lblTotalChip = new System.Windows.Forms.Label();
            this.lblTotalChipCaption = new System.Windows.Forms.Label();
            this.mapHeaderBinTile = new System.Windows.Forms.Panel();
            this.lblBinNum = new System.Windows.Forms.Label();
            this.lblBinNumCaption = new System.Windows.Forms.Label();
            this.mapTabControl = new System.Windows.Forms.TabControl();
            this.tabInputMap = new System.Windows.Forms.TabPage();
            this.lotMapView = new QMC.CDT_320.Ui.Controls.LiveLotMapView();
            this.tabOutputGoodMap = new System.Windows.Forms.TabPage();
            this.outputGoodLotMapView = new QMC.CDT_320.Ui.Controls.LiveLotMapView();
            this.tabOutputNgMap = new System.Windows.Forms.TabPage();
            this.outputNgLotMapView = new QMC.CDT_320.Ui.Controls.LiveLotMapView();
            this.grpInfo = new System.Windows.Forms.GroupBox();
            this.workInfoBody = new System.Windows.Forms.TableLayoutPanel();
            this.lotInputPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblLotIdCaption = new System.Windows.Forms.Label();
            this.txtLotId = new System.Windows.Forms.TextBox();
            this.btnLotStart = new System.Windows.Forms.Button();
            this.btnLotComplete = new System.Windows.Forms.Button();
            this.btnLotHistory = new System.Windows.Forms.Button();
            this.btnBinSelect = new System.Windows.Forms.Button();
            this.workInfoProjectTile = new System.Windows.Forms.Panel();
            this.lblProject = new System.Windows.Forms.Label();
            this.lblProjectCaption = new System.Windows.Forms.Label();
            this.workInfoBinQtyTile = new System.Windows.Forms.Panel();
            this.lblBinQty = new System.Windows.Forms.Label();
            this.lblBinQtyCaption = new System.Windows.Forms.Label();
            this.workInfoPickFailTile = new System.Windows.Forms.Panel();
            this.lblPickFail = new System.Windows.Forms.Label();
            this.lblPickFailCaption = new System.Windows.Forms.Label();
            this.workInfoPlaceFailTile = new System.Windows.Forms.Panel();
            this.lblPlaceFail = new System.Windows.Forms.Label();
            this.lblPlaceFailCaption = new System.Windows.Forms.Label();
            this.workInfoNeedleTile = new System.Windows.Forms.Panel();
            this.lblNeedle = new System.Windows.Forms.Label();
            this.lblNeedleCaption = new System.Windows.Forms.Label();
            this.workInfoFrontCollet1Tile = new System.Windows.Forms.Panel();
            this.lblFrontCollet1Designer = new System.Windows.Forms.Label();
            this.lblFrontCollet1CaptionDesigner = new System.Windows.Forms.Label();
            this.workInfoFrontCollet2Tile = new System.Windows.Forms.Panel();
            this.lblFrontCollet2Designer = new System.Windows.Forms.Label();
            this.lblFrontCollet2CaptionDesigner = new System.Windows.Forms.Label();
            this.workInfoFrontCollet3Tile = new System.Windows.Forms.Panel();
            this.lblFrontCollet3Designer = new System.Windows.Forms.Label();
            this.lblFrontCollet3CaptionDesigner = new System.Windows.Forms.Label();
            this.workInfoFrontCollet4Tile = new System.Windows.Forms.Panel();
            this.lblFrontCollet4Designer = new System.Windows.Forms.Label();
            this.lblFrontCollet4CaptionDesigner = new System.Windows.Forms.Label();
            this.workInfoRearCollet1Tile = new System.Windows.Forms.Panel();
            this.lblRearCollet1Designer = new System.Windows.Forms.Label();
            this.lblRearCollet1CaptionDesigner = new System.Windows.Forms.Label();
            this.workInfoRearCollet2Tile = new System.Windows.Forms.Panel();
            this.lblRearCollet2Designer = new System.Windows.Forms.Label();
            this.lblRearCollet2CaptionDesigner = new System.Windows.Forms.Label();
            this.workInfoRearCollet3Tile = new System.Windows.Forms.Panel();
            this.lblRearCollet3Designer = new System.Windows.Forms.Label();
            this.lblRearCollet3CaptionDesigner = new System.Windows.Forms.Label();
            this.workInfoRearCollet4Tile = new System.Windows.Forms.Panel();
            this.lblRearCollet4Designer = new System.Windows.Forms.Label();
            this.lblRearCollet4CaptionDesigner = new System.Windows.Forms.Label();
            this.grpTime = new System.Windows.Forms.GroupBox();
            this.workTimeBody = new System.Windows.Forms.TableLayoutPanel();
            this.workTimeLotTile = new System.Windows.Forms.Panel();
            this.lblLot = new System.Windows.Forms.Label();
            this.lblLotCaption = new System.Windows.Forms.Label();
            this.workTimeActionPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnCcs = new System.Windows.Forms.Button();
            this.btnWorkTimeClear = new System.Windows.Forms.Button();
            this.btnTestAlarm = new System.Windows.Forms.Button();
            this.workTimeUphTile = new System.Windows.Forms.Panel();
            this.workTimeUphValueLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblUph = new System.Windows.Forms.Label();
            this.workTimeUphHeaderLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblUphCaption = new System.Windows.Forms.Label();
            this.lblRecentMinuteUph = new System.Windows.Forms.Label();
            this.workTimeUpTile = new System.Windows.Forms.Panel();
            this.lblUp = new System.Windows.Forms.Label();
            this.lblUpCaption = new System.Windows.Forms.Label();
            this.workTimeContUpTile = new System.Windows.Forms.Panel();
            this.lblContUp = new System.Windows.Forms.Label();
            this.lblContUpCaption = new System.Windows.Forms.Label();
            this.workTimeRateTile = new System.Windows.Forms.Panel();
            this.lblRate = new System.Windows.Forms.Label();
            this.lblRateCaption = new System.Windows.Forms.Label();
            this.workTimeCycleTile = new System.Windows.Forms.Panel();
            this.lblCycle = new System.Windows.Forms.Label();
            this.lblCycleCaption = new System.Windows.Forms.Label();
            this.workTimeLoadTile = new System.Windows.Forms.Panel();
            this.lblLoad = new System.Windows.Forms.Label();
            this.lblLoadCaption = new System.Windows.Forms.Label();
            this.workTimeMtbfTile = new System.Windows.Forms.Panel();
            this.lblMtbf = new System.Windows.Forms.Label();
            this.lblMtbfCaption = new System.Windows.Forms.Label();
            this.workTimeMttrTile = new System.Windows.Forms.Panel();
            this.lblMttr = new System.Windows.Forms.Label();
            this.lblMttrCaption = new System.Windows.Forms.Label();
            this.workTimeRecoveryTile = new System.Windows.Forms.Panel();
            this.lblRecovery = new System.Windows.Forms.Label();
            this.lblRecoveryCaption = new System.Windows.Forms.Label();
            this.workTimeNormDownTile = new System.Windows.Forms.Panel();
            this.lblNormDown = new System.Windows.Forms.Label();
            this.lblNormDownCaption = new System.Windows.Forms.Label();
            this.workTimeErrDownTile = new System.Windows.Forms.Panel();
            this.lblErrDown = new System.Windows.Forms.Label();
            this.lblErrDownCaption = new System.Windows.Forms.Label();
            this.workTimeErrCntTile = new System.Windows.Forms.Panel();
            this.lblErrCnt = new System.Windows.Forms.Label();
            this.lblErrCntCaption = new System.Windows.Forms.Label();
            this.lblVisionCaption = new System.Windows.Forms.Label();
            this.lblPickCaption = new System.Windows.Forms.Label();
            this.lblPlaceCaption = new System.Windows.Forms.Label();
            this.lblCollet1Caption = new System.Windows.Forms.Label();
            this.lblCollet1 = new System.Windows.Forms.Label();
            this.lblCollet2Caption = new System.Windows.Forms.Label();
            this.lblCollet2 = new System.Windows.Forms.Label();
            this.lblBinArrMonCaption = new System.Windows.Forms.Label();
            this.lblBinArrMon = new System.Windows.Forms.Label();
            this.rootLayout.SuspendLayout();
            this.grpVision.SuspendLayout();
            this.visionPanel.SuspendLayout();
            this.visionShellLayout.SuspendLayout();
            this.visionMainLayout.SuspendLayout();
            this.visionLeftLayout.SuspendLayout();
            this.pnlWaferVision.SuspendLayout();
            this.pnlBinVision.SuspendLayout();
            this.visionSideLayout.SuspendLayout();
            this.pnlSideRearVision.SuspendLayout();
            this.pnlBottomInspVision.SuspendLayout();
            this.pnlSideFrontVision.SuspendLayout();
            this.grpMap.SuspendLayout();
            this.mapBody.SuspendLayout();
            this.mapHeaderLayout.SuspendLayout();
            this.mapHeaderTotalTile.SuspendLayout();
            this.mapHeaderBinTile.SuspendLayout();
            this.mapTabControl.SuspendLayout();
            this.tabInputMap.SuspendLayout();
            this.tabOutputGoodMap.SuspendLayout();
            this.tabOutputNgMap.SuspendLayout();
            this.grpInfo.SuspendLayout();
            this.workInfoBody.SuspendLayout();
            this.lotInputPanel.SuspendLayout();
            this.workInfoProjectTile.SuspendLayout();
            this.workInfoBinQtyTile.SuspendLayout();
            this.workInfoPickFailTile.SuspendLayout();
            this.workInfoPlaceFailTile.SuspendLayout();
            this.workInfoNeedleTile.SuspendLayout();
            this.workInfoFrontCollet1Tile.SuspendLayout();
            this.workInfoFrontCollet2Tile.SuspendLayout();
            this.workInfoFrontCollet3Tile.SuspendLayout();
            this.workInfoFrontCollet4Tile.SuspendLayout();
            this.workInfoRearCollet1Tile.SuspendLayout();
            this.workInfoRearCollet2Tile.SuspendLayout();
            this.workInfoRearCollet3Tile.SuspendLayout();
            this.workInfoRearCollet4Tile.SuspendLayout();
            this.grpTime.SuspendLayout();
            this.workTimeBody.SuspendLayout();
            this.workTimeLotTile.SuspendLayout();
            this.workTimeActionPanel.SuspendLayout();
            this.workTimeUphTile.SuspendLayout();
            this.workTimeUphValueLayout.SuspendLayout();
            this.workTimeUphHeaderLayout.SuspendLayout();
            this.workTimeUpTile.SuspendLayout();
            this.workTimeContUpTile.SuspendLayout();
            this.workTimeRateTile.SuspendLayout();
            this.workTimeCycleTile.SuspendLayout();
            this.workTimeLoadTile.SuspendLayout();
            this.workTimeMtbfTile.SuspendLayout();
            this.workTimeMttrTile.SuspendLayout();
            this.workTimeRecoveryTile.SuspendLayout();
            this.workTimeNormDownTile.SuspendLayout();
            this.workTimeErrDownTile.SuspendLayout();
            this.workTimeErrCntTile.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.BackColor = System.Drawing.Color.White;
            this.rootLayout.ColumnCount = 2;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.rootLayout.Controls.Add(this.grpVision, 0, 0);
            this.rootLayout.Controls.Add(this.grpMap, 1, 0);
            this.rootLayout.Controls.Add(this.grpInfo, 0, 1);
            this.rootLayout.Controls.Add(this.grpTime, 1, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(4);
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 65F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // grpVision
            // 
            this.grpVision.BackColor = System.Drawing.Color.White;
            this.grpVision.Controls.Add(this.visionPanel);
            this.grpVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpVision.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpVision.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            this.grpVision.Location = new System.Drawing.Point(8, 8);
            this.grpVision.Margin = new System.Windows.Forms.Padding(4);
            this.grpVision.Name = "grpVision";
            this.grpVision.Padding = new System.Windows.Forms.Padding(6, 4, 6, 6);
            this.grpVision.Size = new System.Drawing.Size(827, 571);
            this.grpVision.TabIndex = 0;
            this.grpVision.TabStop = false;
            this.grpVision.Tag = "i18n:work.sec.visionView";
            this.grpVision.Text = "비전 화면";
            // 
            // visionPanel
            // 
            this.visionPanel.BackColor = System.Drawing.Color.Black;
            this.visionPanel.Controls.Add(this.visionShellLayout);
            this.visionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.visionPanel.Location = new System.Drawing.Point(6, 24);
            this.visionPanel.Name = "visionPanel";
            this.visionPanel.Size = new System.Drawing.Size(815, 541);
            this.visionPanel.TabIndex = 1;
            // 
            // visionShellLayout
            // 
            this.visionShellLayout.BackColor = System.Drawing.Color.Black;
            this.visionShellLayout.ColumnCount = 1;
            this.visionShellLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.visionShellLayout.Controls.Add(this.visionMainLayout, 0, 0);
            this.visionShellLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.visionShellLayout.Location = new System.Drawing.Point(0, 0);
            this.visionShellLayout.Margin = new System.Windows.Forms.Padding(0);
            this.visionShellLayout.Name = "visionShellLayout";
            this.visionShellLayout.RowCount = 1;
            this.visionShellLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.visionShellLayout.Size = new System.Drawing.Size(815, 541);
            this.visionShellLayout.TabIndex = 0;
            // 
            // visionMainLayout
            // 
            this.visionMainLayout.BackColor = System.Drawing.Color.Black;
            this.visionMainLayout.ColumnCount = 2;
            this.visionMainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.visionMainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.visionMainLayout.Controls.Add(this.visionLeftLayout, 0, 0);
            this.visionMainLayout.Controls.Add(this.visionSideLayout, 1, 0);
            this.visionMainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.visionMainLayout.Location = new System.Drawing.Point(0, 0);
            this.visionMainLayout.Margin = new System.Windows.Forms.Padding(0);
            this.visionMainLayout.Name = "visionMainLayout";
            this.visionMainLayout.RowCount = 1;
            this.visionMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.visionMainLayout.Size = new System.Drawing.Size(815, 541);
            this.visionMainLayout.TabIndex = 0;
            // 
            // visionLeftLayout
            // 
            this.visionLeftLayout.BackColor = System.Drawing.Color.Black;
            this.visionLeftLayout.ColumnCount = 1;
            this.visionLeftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.visionLeftLayout.Controls.Add(this.pnlWaferVision, 0, 0);
            this.visionLeftLayout.Controls.Add(this.pnlBinVision, 0, 1);
            this.visionLeftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.visionLeftLayout.Location = new System.Drawing.Point(0, 0);
            this.visionLeftLayout.Margin = new System.Windows.Forms.Padding(0);
            this.visionLeftLayout.Name = "visionLeftLayout";
            this.visionLeftLayout.RowCount = 2;
            this.visionLeftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.visionLeftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.visionLeftLayout.Size = new System.Drawing.Size(407, 541);
            this.visionLeftLayout.TabIndex = 0;
            // 
            // pnlWaferVision
            // 
            this.pnlWaferVision.BackColor = System.Drawing.Color.Black;
            this.pnlWaferVision.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlWaferVision.Controls.Add(this.lblStageInfo);
            this.pnlWaferVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlWaferVision.Location = new System.Drawing.Point(1, 1);
            this.pnlWaferVision.Margin = new System.Windows.Forms.Padding(1);
            this.pnlWaferVision.Name = "pnlWaferVision";
            this.pnlWaferVision.Size = new System.Drawing.Size(405, 268);
            this.pnlWaferVision.TabIndex = 0;
            // 
            // lblStageInfo
            // 
            this.lblStageInfo.AutoSize = true;
            this.lblStageInfo.BackColor = System.Drawing.Color.Black;
            this.lblStageInfo.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblStageInfo.ForeColor = System.Drawing.Color.LightGreen;
            this.lblStageInfo.Location = new System.Drawing.Point(8, 8);
            this.lblStageInfo.Name = "lblStageInfo";
            this.lblStageInfo.Size = new System.Drawing.Size(91, 42);
            this.lblStageInfo.TabIndex = 0;
            this.lblStageInfo.Text = "WAFER VISION\r\nSTAGE\r\nW:640 H:480";
            // 
            // pnlBinVision
            // 
            this.pnlBinVision.BackColor = System.Drawing.Color.Black;
            this.pnlBinVision.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlBinVision.Controls.Add(this.lblBinVisionInfo);
            this.pnlBinVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBinVision.Location = new System.Drawing.Point(1, 271);
            this.pnlBinVision.Margin = new System.Windows.Forms.Padding(1);
            this.pnlBinVision.Name = "pnlBinVision";
            this.pnlBinVision.Size = new System.Drawing.Size(405, 269);
            this.pnlBinVision.TabIndex = 3;
            // 
            // lblBinVisionInfo
            // 
            this.lblBinVisionInfo.AutoSize = true;
            this.lblBinVisionInfo.BackColor = System.Drawing.Color.Black;
            this.lblBinVisionInfo.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblBinVisionInfo.ForeColor = System.Drawing.Color.LightGreen;
            this.lblBinVisionInfo.Location = new System.Drawing.Point(8, 8);
            this.lblBinVisionInfo.Name = "lblBinVisionInfo";
            this.lblBinVisionInfo.Size = new System.Drawing.Size(84, 42);
            this.lblBinVisionInfo.TabIndex = 0;
            this.lblBinVisionInfo.Text = "BIN VISION\r\nSTAGE\r\nW:640 H:480";
            // 
            // visionSideLayout
            // 
            this.visionSideLayout.BackColor = System.Drawing.Color.Black;
            this.visionSideLayout.ColumnCount = 1;
            this.visionSideLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.visionSideLayout.Controls.Add(this.pnlSideRearVision, 0, 0);
            this.visionSideLayout.Controls.Add(this.pnlBottomInspVision, 0, 1);
            this.visionSideLayout.Controls.Add(this.pnlSideFrontVision, 0, 2);
            this.visionSideLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.visionSideLayout.Location = new System.Drawing.Point(407, 0);
            this.visionSideLayout.Margin = new System.Windows.Forms.Padding(0);
            this.visionSideLayout.Name = "visionSideLayout";
            this.visionSideLayout.RowCount = 3;
            this.visionSideLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.visionSideLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.visionSideLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.visionSideLayout.Size = new System.Drawing.Size(408, 541);
            this.visionSideLayout.TabIndex = 2;
            // 
            // pnlSideRearVision
            // 
            this.pnlSideRearVision.BackColor = System.Drawing.Color.Black;
            this.pnlSideRearVision.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlSideRearVision.Controls.Add(this.lblSideRearInfo);
            this.pnlSideRearVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlSideRearVision.Location = new System.Drawing.Point(1, 1);
            this.pnlSideRearVision.Margin = new System.Windows.Forms.Padding(1);
            this.pnlSideRearVision.Name = "pnlSideRearVision";
            this.pnlSideRearVision.Size = new System.Drawing.Size(406, 178);
            this.pnlSideRearVision.TabIndex = 0;
            // 
            // lblSideRearInfo
            // 
            this.lblSideRearInfo.AutoSize = true;
            this.lblSideRearInfo.BackColor = System.Drawing.Color.Black;
            this.lblSideRearInfo.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblSideRearInfo.ForeColor = System.Drawing.Color.LightGreen;
            this.lblSideRearInfo.Location = new System.Drawing.Point(8, 8);
            this.lblSideRearInfo.Name = "lblSideRearInfo";
            this.lblSideRearInfo.Size = new System.Drawing.Size(119, 42);
            this.lblSideRearInfo.TabIndex = 0;
            this.lblSideRearInfo.Text = "REAR SIDE VISION\r\nSTAGE\r\nW:640 H:480";
            // 
            // pnlBottomInspVision
            // 
            this.pnlBottomInspVision.BackColor = System.Drawing.Color.Black;
            this.pnlBottomInspVision.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlBottomInspVision.Controls.Add(this.lblBottomInspInfo);
            this.pnlBottomInspVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBottomInspVision.Location = new System.Drawing.Point(1, 181);
            this.pnlBottomInspVision.Margin = new System.Windows.Forms.Padding(1);
            this.pnlBottomInspVision.Name = "pnlBottomInspVision";
            this.pnlBottomInspVision.Size = new System.Drawing.Size(406, 178);
            this.pnlBottomInspVision.TabIndex = 1;
            // 
            // lblBottomInspInfo
            // 
            this.lblBottomInspInfo.AutoSize = true;
            this.lblBottomInspInfo.BackColor = System.Drawing.Color.Black;
            this.lblBottomInspInfo.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblBottomInspInfo.ForeColor = System.Drawing.Color.LightGreen;
            this.lblBottomInspInfo.Location = new System.Drawing.Point(8, 8);
            this.lblBottomInspInfo.Name = "lblBottomInspInfo";
            this.lblBottomInspInfo.Size = new System.Drawing.Size(98, 42);
            this.lblBottomInspInfo.TabIndex = 0;
            this.lblBottomInspInfo.Text = "BOTTOM VISION\r\nSTAGE\r\nW:640 H:480";
            // 
            // pnlSideFrontVision
            // 
            this.pnlSideFrontVision.BackColor = System.Drawing.Color.Black;
            this.pnlSideFrontVision.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlSideFrontVision.Controls.Add(this.lblSideFrontInfo);
            this.pnlSideFrontVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlSideFrontVision.Location = new System.Drawing.Point(1, 361);
            this.pnlSideFrontVision.Margin = new System.Windows.Forms.Padding(1);
            this.pnlSideFrontVision.Name = "pnlSideFrontVision";
            this.pnlSideFrontVision.Size = new System.Drawing.Size(406, 179);
            this.pnlSideFrontVision.TabIndex = 1;
            // 
            // lblSideFrontInfo
            // 
            this.lblSideFrontInfo.AutoSize = true;
            this.lblSideFrontInfo.BackColor = System.Drawing.Color.Black;
            this.lblSideFrontInfo.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblSideFrontInfo.ForeColor = System.Drawing.Color.LightGreen;
            this.lblSideFrontInfo.Location = new System.Drawing.Point(8, 8);
            this.lblSideFrontInfo.Name = "lblSideFrontInfo";
            this.lblSideFrontInfo.Size = new System.Drawing.Size(126, 42);
            this.lblSideFrontInfo.TabIndex = 0;
            this.lblSideFrontInfo.Text = "FRONT SIDE VISION\r\nSTAGE\r\nW:640 H:480";
            // 
            // grpMap
            // 
            this.grpMap.BackColor = System.Drawing.Color.White;
            this.grpMap.Controls.Add(this.mapBody);
            this.grpMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMap.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpMap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            this.grpMap.Location = new System.Drawing.Point(843, 8);
            this.grpMap.Margin = new System.Windows.Forms.Padding(4);
            this.grpMap.Name = "grpMap";
            this.grpMap.Padding = new System.Windows.Forms.Padding(6, 4, 6, 6);
            this.grpMap.Size = new System.Drawing.Size(827, 571);
            this.grpMap.TabIndex = 1;
            this.grpMap.TabStop = false;
            this.grpMap.Tag = "i18n:work.sec.workMap";
            this.grpMap.Text = "작업 맵";
            // 
            // mapBody
            // 
            this.mapBody.BackColor = System.Drawing.Color.White;
            this.mapBody.ColumnCount = 1;
            this.mapBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapBody.Controls.Add(this.mapHeaderLayout, 0, 0);
            this.mapBody.Controls.Add(this.mapTabControl, 0, 1);
            this.mapBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapBody.Location = new System.Drawing.Point(6, 24);
            this.mapBody.Margin = new System.Windows.Forms.Padding(0);
            this.mapBody.Name = "mapBody";
            this.mapBody.RowCount = 2;
            this.mapBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.mapBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapBody.Size = new System.Drawing.Size(815, 541);
            this.mapBody.TabIndex = 0;
            // 
            // mapHeaderLayout
            // 
            this.mapHeaderLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(245)))));
            this.mapHeaderLayout.ColumnCount = 2;
            this.mapHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.mapHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.mapHeaderLayout.Controls.Add(this.mapHeaderTotalTile, 0, 0);
            this.mapHeaderLayout.Controls.Add(this.mapHeaderBinTile, 1, 0);
            this.mapHeaderLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapHeaderLayout.Location = new System.Drawing.Point(0, 0);
            this.mapHeaderLayout.Margin = new System.Windows.Forms.Padding(0);
            this.mapHeaderLayout.Name = "mapHeaderLayout";
            this.mapHeaderLayout.RowCount = 1;
            this.mapHeaderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapHeaderLayout.Size = new System.Drawing.Size(815, 32);
            this.mapHeaderLayout.TabIndex = 0;
            // 
            // mapHeaderTotalTile
            // 
            this.mapHeaderTotalTile.BackColor = System.Drawing.Color.White;
            this.mapHeaderTotalTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mapHeaderTotalTile.Controls.Add(this.lblTotalChip);
            this.mapHeaderTotalTile.Controls.Add(this.lblTotalChipCaption);
            this.mapHeaderTotalTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapHeaderTotalTile.Location = new System.Drawing.Point(2, 1);
            this.mapHeaderTotalTile.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.mapHeaderTotalTile.Name = "mapHeaderTotalTile";
            this.mapHeaderTotalTile.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.mapHeaderTotalTile.Size = new System.Drawing.Size(403, 30);
            this.mapHeaderTotalTile.TabIndex = 0;
            // 
            // lblTotalChip
            // 
            this.lblTotalChip.BackColor = System.Drawing.Color.Transparent;
            this.lblTotalChip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalChip.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblTotalChip.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblTotalChip.Location = new System.Drawing.Point(8, 12);
            this.lblTotalChip.Margin = new System.Windows.Forms.Padding(0);
            this.lblTotalChip.Name = "lblTotalChip";
            this.lblTotalChip.Size = new System.Drawing.Size(385, 16);
            this.lblTotalChip.TabIndex = 1;
            this.lblTotalChip.Text = "0";
            this.lblTotalChip.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblTotalChipCaption
            // 
            this.lblTotalChipCaption.BackColor = System.Drawing.Color.Transparent;
            this.lblTotalChipCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTotalChipCaption.Font = new System.Drawing.Font("맑은 고딕", 7F, System.Drawing.FontStyle.Bold);
            this.lblTotalChipCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblTotalChipCaption.Location = new System.Drawing.Point(8, 0);
            this.lblTotalChipCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblTotalChipCaption.Name = "lblTotalChipCaption";
            this.lblTotalChipCaption.Size = new System.Drawing.Size(385, 12);
            this.lblTotalChipCaption.TabIndex = 0;
            this.lblTotalChipCaption.Text = "TOTAL CHIP";
            this.lblTotalChipCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mapHeaderBinTile
            // 
            this.mapHeaderBinTile.BackColor = System.Drawing.Color.White;
            this.mapHeaderBinTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mapHeaderBinTile.Controls.Add(this.lblBinNum);
            this.mapHeaderBinTile.Controls.Add(this.lblBinNumCaption);
            this.mapHeaderBinTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapHeaderBinTile.Location = new System.Drawing.Point(409, 1);
            this.mapHeaderBinTile.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.mapHeaderBinTile.Name = "mapHeaderBinTile";
            this.mapHeaderBinTile.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.mapHeaderBinTile.Size = new System.Drawing.Size(404, 30);
            this.mapHeaderBinTile.TabIndex = 1;
            // 
            // lblBinNum
            // 
            this.lblBinNum.BackColor = System.Drawing.Color.Transparent;
            this.lblBinNum.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinNum.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblBinNum.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblBinNum.Location = new System.Drawing.Point(8, 12);
            this.lblBinNum.Margin = new System.Windows.Forms.Padding(0);
            this.lblBinNum.Name = "lblBinNum";
            this.lblBinNum.Size = new System.Drawing.Size(386, 16);
            this.lblBinNum.TabIndex = 3;
            this.lblBinNum.Text = "--";
            this.lblBinNum.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblBinNumCaption
            // 
            this.lblBinNumCaption.BackColor = System.Drawing.Color.Transparent;
            this.lblBinNumCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblBinNumCaption.Font = new System.Drawing.Font("맑은 고딕", 7F, System.Drawing.FontStyle.Bold);
            this.lblBinNumCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblBinNumCaption.Location = new System.Drawing.Point(8, 0);
            this.lblBinNumCaption.Margin = new System.Windows.Forms.Padding(0);
            this.lblBinNumCaption.Name = "lblBinNumCaption";
            this.lblBinNumCaption.Size = new System.Drawing.Size(386, 12);
            this.lblBinNumCaption.TabIndex = 2;
            this.lblBinNumCaption.Text = "CURRENT BIN";
            this.lblBinNumCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mapTabControl
            // 
            this.mapTabControl.Controls.Add(this.tabInputMap);
            this.mapTabControl.Controls.Add(this.tabOutputGoodMap);
            this.mapTabControl.Controls.Add(this.tabOutputNgMap);
            this.mapTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapTabControl.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this.mapTabControl.ItemSize = new System.Drawing.Size(260, 21);
            this.mapTabControl.Location = new System.Drawing.Point(0, 32);
            this.mapTabControl.Margin = new System.Windows.Forms.Padding(0);
            this.mapTabControl.Name = "mapTabControl";
            this.mapTabControl.Padding = new System.Drawing.Point(6, 1);
            this.mapTabControl.SelectedIndex = 0;
            this.mapTabControl.Size = new System.Drawing.Size(815, 509);
            this.mapTabControl.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.mapTabControl.TabIndex = 1;
            this.mapTabControl.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.MapTabControl_DrawItem);
            this.mapTabControl.SizeChanged += new System.EventHandler(this.MapTabControl_SizeChanged);
            // 
            // tabInputMap
            // 
            this.tabInputMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.tabInputMap.Controls.Add(this.lotMapView);
            this.tabInputMap.Location = new System.Drawing.Point(4, 25);
            this.tabInputMap.Margin = new System.Windows.Forms.Padding(0);
            this.tabInputMap.Name = "tabInputMap";
            this.tabInputMap.Size = new System.Drawing.Size(807, 480);
            this.tabInputMap.TabIndex = 0;
            this.tabInputMap.Text = "INPUT MAP";
            // 
            // lotMapView
            // 
            this.lotMapView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.lotMapView.Caption = "Die Map";
            this.lotMapView.CompactUsedBounds = true;
            this.lotMapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lotMapView.EnableRectangleSelection = false;
            this.lotMapView.GridX = 5;
            this.lotMapView.GridY = 5;
            this.lotMapView.Location = new System.Drawing.Point(0, 0);
            this.lotMapView.Map = null;
            this.lotMapView.Margin = new System.Windows.Forms.Padding(0);
            this.lotMapView.Name = "lotMapView";
            this.lotMapView.SelectedEntry = null;
            this.lotMapView.ShowEquipmentAxes = true;
            this.lotMapView.ShowWaferOutline = true;
            this.lotMapView.Size = new System.Drawing.Size(807, 480);
            this.lotMapView.SourceKind = QMC.CDT_320.Ui.Controls.LiveLotMapSourceKind.Input;
            this.lotMapView.TabIndex = 0;
            // 
            // tabOutputGoodMap
            // 
            this.tabOutputGoodMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.tabOutputGoodMap.Controls.Add(this.outputGoodLotMapView);
            this.tabOutputGoodMap.Location = new System.Drawing.Point(4, 25);
            this.tabOutputGoodMap.Margin = new System.Windows.Forms.Padding(0);
            this.tabOutputGoodMap.Name = "tabOutputGoodMap";
            this.tabOutputGoodMap.Size = new System.Drawing.Size(807, 480);
            this.tabOutputGoodMap.TabIndex = 1;
            this.tabOutputGoodMap.Text = "OUTPUT GOOD";
            // 
            // outputGoodLotMapView
            // 
            this.outputGoodLotMapView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.outputGoodLotMapView.Caption = "Die Map";
            this.outputGoodLotMapView.CompactUsedBounds = true;
            this.outputGoodLotMapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputGoodLotMapView.EnableRectangleSelection = false;
            this.outputGoodLotMapView.GridX = 5;
            this.outputGoodLotMapView.GridY = 5;
            this.outputGoodLotMapView.Location = new System.Drawing.Point(0, 0);
            this.outputGoodLotMapView.Map = null;
            this.outputGoodLotMapView.Margin = new System.Windows.Forms.Padding(0);
            this.outputGoodLotMapView.Name = "outputGoodLotMapView";
            this.outputGoodLotMapView.SelectedEntry = null;
            this.outputGoodLotMapView.ShowEquipmentAxes = true;
            this.outputGoodLotMapView.ShowWaferOutline = true;
            this.outputGoodLotMapView.Size = new System.Drawing.Size(807, 480);
            this.outputGoodLotMapView.SourceKind = QMC.CDT_320.Ui.Controls.LiveLotMapSourceKind.OutputGood;
            this.outputGoodLotMapView.TabIndex = 0;
            // 
            // tabOutputNgMap
            // 
            this.tabOutputNgMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.tabOutputNgMap.Controls.Add(this.outputNgLotMapView);
            this.tabOutputNgMap.Location = new System.Drawing.Point(4, 25);
            this.tabOutputNgMap.Margin = new System.Windows.Forms.Padding(0);
            this.tabOutputNgMap.Name = "tabOutputNgMap";
            this.tabOutputNgMap.Size = new System.Drawing.Size(807, 480);
            this.tabOutputNgMap.TabIndex = 2;
            this.tabOutputNgMap.Text = "OUTPUT NG";
            // 
            // outputNgLotMapView
            // 
            this.outputNgLotMapView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.outputNgLotMapView.Caption = "Die Map";
            this.outputNgLotMapView.CompactUsedBounds = true;
            this.outputNgLotMapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputNgLotMapView.EnableRectangleSelection = false;
            this.outputNgLotMapView.GridX = 5;
            this.outputNgLotMapView.GridY = 5;
            this.outputNgLotMapView.Location = new System.Drawing.Point(0, 0);
            this.outputNgLotMapView.Map = null;
            this.outputNgLotMapView.Margin = new System.Windows.Forms.Padding(0);
            this.outputNgLotMapView.Name = "outputNgLotMapView";
            this.outputNgLotMapView.SelectedEntry = null;
            this.outputNgLotMapView.ShowEquipmentAxes = true;
            this.outputNgLotMapView.ShowWaferOutline = true;
            this.outputNgLotMapView.Size = new System.Drawing.Size(807, 480);
            this.outputNgLotMapView.SourceKind = QMC.CDT_320.Ui.Controls.LiveLotMapSourceKind.OutputNg;
            this.outputNgLotMapView.TabIndex = 0;
            // 
            // grpInfo
            // 
            this.grpInfo.BackColor = System.Drawing.Color.White;
            this.grpInfo.Controls.Add(this.workInfoBody);
            this.grpInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInfo.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            this.grpInfo.Location = new System.Drawing.Point(8, 587);
            this.grpInfo.Margin = new System.Windows.Forms.Padding(4);
            this.grpInfo.Name = "grpInfo";
            this.grpInfo.Padding = new System.Windows.Forms.Padding(6, 4, 6, 6);
            this.grpInfo.Size = new System.Drawing.Size(827, 305);
            this.grpInfo.TabIndex = 2;
            this.grpInfo.TabStop = false;
            this.grpInfo.Tag = "i18n:work.sec.workInfo";
            this.grpInfo.Text = "작업 정보";
            // 
            // workInfoBody
            // 
            this.workInfoBody.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(245)))));
            this.workInfoBody.ColumnCount = 4;
            this.workInfoBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workInfoBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workInfoBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workInfoBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workInfoBody.Controls.Add(this.lotInputPanel, 0, 0);
            this.workInfoBody.Controls.Add(this.workInfoProjectTile, 0, 1);
            this.workInfoBody.Controls.Add(this.workInfoBinQtyTile, 0, 2);
            this.workInfoBody.Controls.Add(this.workInfoPickFailTile, 1, 2);
            this.workInfoBody.Controls.Add(this.workInfoPlaceFailTile, 2, 2);
            this.workInfoBody.Controls.Add(this.workInfoNeedleTile, 3, 2);
            this.workInfoBody.Controls.Add(this.workInfoFrontCollet1Tile, 0, 3);
            this.workInfoBody.Controls.Add(this.workInfoFrontCollet2Tile, 1, 3);
            this.workInfoBody.Controls.Add(this.workInfoFrontCollet3Tile, 2, 3);
            this.workInfoBody.Controls.Add(this.workInfoFrontCollet4Tile, 3, 3);
            this.workInfoBody.Controls.Add(this.workInfoRearCollet1Tile, 0, 4);
            this.workInfoBody.Controls.Add(this.workInfoRearCollet2Tile, 1, 4);
            this.workInfoBody.Controls.Add(this.workInfoRearCollet3Tile, 2, 4);
            this.workInfoBody.Controls.Add(this.workInfoRearCollet4Tile, 3, 4);
            this.workInfoBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoBody.Location = new System.Drawing.Point(6, 24);
            this.workInfoBody.Margin = new System.Windows.Forms.Padding(0);
            this.workInfoBody.Name = "workInfoBody";
            this.workInfoBody.Padding = new System.Windows.Forms.Padding(3);
            this.workInfoBody.RowCount = 5;
            this.workInfoBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.workInfoBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.workInfoBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.workInfoBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.workInfoBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.workInfoBody.Size = new System.Drawing.Size(815, 275);
            this.workInfoBody.TabIndex = 0;
            // 
            // lotInputPanel
            // 
            this.lotInputPanel.ColumnCount = 6;
            this.workInfoBody.SetColumnSpan(this.lotInputPanel, 4);
            this.lotInputPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.lotInputPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lotInputPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 92F));
            this.lotInputPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 92F));
            this.lotInputPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 72F));
            this.lotInputPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 72F));
            this.lotInputPanel.Controls.Add(this.lblLotIdCaption, 0, 0);
            this.lotInputPanel.Controls.Add(this.txtLotId, 1, 0);
            this.lotInputPanel.Controls.Add(this.btnLotStart, 2, 0);
            this.lotInputPanel.Controls.Add(this.btnLotComplete, 3, 0);
            this.lotInputPanel.Controls.Add(this.btnLotHistory, 4, 0);
            this.lotInputPanel.Controls.Add(this.btnBinSelect, 5, 0);
            this.lotInputPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lotInputPanel.Location = new System.Drawing.Point(3, 3);
            this.lotInputPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 3);
            this.lotInputPanel.Name = "lotInputPanel";
            this.lotInputPanel.RowCount = 1;
            this.lotInputPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lotInputPanel.Size = new System.Drawing.Size(809, 31);
            this.lotInputPanel.TabIndex = 0;
            // 
            // lblLotIdCaption
            // 
            this.lblLotIdCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLotIdCaption.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblLotIdCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.lblLotIdCaption.Location = new System.Drawing.Point(3, 0);
            this.lblLotIdCaption.Name = "lblLotIdCaption";
            this.lblLotIdCaption.Size = new System.Drawing.Size(54, 31);
            this.lblLotIdCaption.TabIndex = 0;
            this.lblLotIdCaption.Text = "LOT ID";
            this.lblLotIdCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtLotId
            // 
            this.txtLotId.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLotId.Font = new System.Drawing.Font("Consolas", 11F);
            this.txtLotId.Location = new System.Drawing.Point(63, 3);
            this.txtLotId.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);
            this.txtLotId.MaxLength = 64;
            this.txtLotId.Name = "txtLotId";
            this.txtLotId.Size = new System.Drawing.Size(484, 25);
            this.txtLotId.TabIndex = 1;
            // 
            // btnLotStart
            // 
            this.btnLotStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(21)))), ((int)(((byte)(128)))), ((int)(((byte)(61)))));
            this.btnLotStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLotStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLotStart.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnLotStart.ForeColor = System.Drawing.Color.White;
            this.btnLotStart.Location = new System.Drawing.Point(556, 2);
            this.btnLotStart.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnLotStart.Name = "btnLotStart";
            this.btnLotStart.Size = new System.Drawing.Size(86, 27);
            this.btnLotStart.TabIndex = 2;
            this.btnLotStart.Text = "LOT 시작";
            this.btnLotStart.UseVisualStyleBackColor = false;
            this.btnLotStart.Click += new System.EventHandler(this.btnLotStart_Click);
            // 
            // btnLotComplete
            // 
            this.btnLotComplete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(89)))), ((int)(((byte)(89)))), ((int)(((byte)(89)))));
            this.btnLotComplete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLotComplete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLotComplete.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnLotComplete.ForeColor = System.Drawing.Color.White;
            this.btnLotComplete.Location = new System.Drawing.Point(648, 2);
            this.btnLotComplete.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnLotComplete.Name = "btnLotComplete";
            this.btnLotComplete.Size = new System.Drawing.Size(86, 27);
            this.btnLotComplete.TabIndex = 3;
            this.btnLotComplete.Text = "LOT 완료";
            this.btnLotComplete.UseVisualStyleBackColor = false;
            this.btnLotComplete.Click += new System.EventHandler(this.btnLotComplete_Click);
            // 
            // btnLotHistory
            // 
            this.btnLotHistory.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
            this.btnLotHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLotHistory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLotHistory.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnLotHistory.ForeColor = System.Drawing.Color.White;
            this.btnLotHistory.Location = new System.Drawing.Point(740, 2);
            this.btnLotHistory.Margin = new System.Windows.Forms.Padding(3, 2, 0, 2);
            this.btnLotHistory.Name = "btnLotHistory";
            this.btnLotHistory.Size = new System.Drawing.Size(69, 27);
            this.btnLotHistory.TabIndex = 4;
            this.btnLotHistory.Text = "이력";
            this.btnLotHistory.UseVisualStyleBackColor = false;
            this.btnLotHistory.Click += new System.EventHandler(this.btnLotHistory_Click);
            //
            // btnBinSelect
            //
            this.btnBinSelect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
            this.btnBinSelect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBinSelect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBinSelect.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnBinSelect.ForeColor = System.Drawing.Color.White;
            this.btnBinSelect.Margin = new System.Windows.Forms.Padding(3, 2, 0, 2);
            this.btnBinSelect.Name = "btnBinSelect";
            this.btnBinSelect.Text = "BIN";
            this.btnBinSelect.UseVisualStyleBackColor = false;
            this.btnBinSelect.Click += new System.EventHandler(this.btnBinSelect_Click);
            // 
            // workInfoProjectTile
            // 
            this.workInfoProjectTile.BackColor = System.Drawing.Color.White;
            this.workInfoProjectTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workInfoBody.SetColumnSpan(this.workInfoProjectTile, 4);
            this.workInfoProjectTile.Controls.Add(this.lblProject);
            this.workInfoProjectTile.Controls.Add(this.lblProjectCaption);
            this.workInfoProjectTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoProjectTile.Location = new System.Drawing.Point(6, 39);
            this.workInfoProjectTile.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.workInfoProjectTile.Name = "workInfoProjectTile";
            this.workInfoProjectTile.Padding = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.workInfoProjectTile.Size = new System.Drawing.Size(803, 38);
            this.workInfoProjectTile.TabIndex = 1;
            // 
            // lblProject
            // 
            this.lblProject.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProject.Font = new System.Drawing.Font("Consolas", 11F, System.Drawing.FontStyle.Bold);
            this.lblProject.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblProject.Location = new System.Drawing.Point(156, 1);
            this.lblProject.Name = "lblProject";
            this.lblProject.Size = new System.Drawing.Size(637, 34);
            this.lblProject.TabIndex = 0;
            this.lblProject.Text = "--";
            this.lblProject.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblProjectCaption
            // 
            this.lblProjectCaption.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblProjectCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblProjectCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblProjectCaption.Location = new System.Drawing.Point(8, 1);
            this.lblProjectCaption.Name = "lblProjectCaption";
            this.lblProjectCaption.Size = new System.Drawing.Size(148, 34);
            this.lblProjectCaption.TabIndex = 1;
            this.lblProjectCaption.Tag = "i18n:work.workInfo.project";
            this.lblProjectCaption.Text = "프로젝트 이름";
            this.lblProjectCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workInfoBinQtyTile
            // 
            this.workInfoBinQtyTile.BackColor = System.Drawing.Color.White;
            this.workInfoBinQtyTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workInfoBinQtyTile.Controls.Add(this.lblBinQty);
            this.workInfoBinQtyTile.Controls.Add(this.lblBinQtyCaption);
            this.workInfoBinQtyTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoBinQtyTile.Location = new System.Drawing.Point(6, 82);
            this.workInfoBinQtyTile.Name = "workInfoBinQtyTile";
            this.workInfoBinQtyTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workInfoBinQtyTile.Size = new System.Drawing.Size(196, 58);
            this.workInfoBinQtyTile.TabIndex = 2;
            // 
            // lblBinQty
            // 
            this.lblBinQty.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinQty.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblBinQty.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(120)))), ((int)(((byte)(60)))));
            this.lblBinQty.Location = new System.Drawing.Point(8, 19);
            this.lblBinQty.Name = "lblBinQty";
            this.lblBinQty.Size = new System.Drawing.Size(178, 35);
            this.lblBinQty.TabIndex = 0;
            this.lblBinQty.Text = "0 ea";
            this.lblBinQty.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblBinQtyCaption
            // 
            this.lblBinQtyCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblBinQtyCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblBinQtyCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblBinQtyCaption.Location = new System.Drawing.Point(8, 2);
            this.lblBinQtyCaption.Name = "lblBinQtyCaption";
            this.lblBinQtyCaption.Size = new System.Drawing.Size(178, 17);
            this.lblBinQtyCaption.TabIndex = 1;
            this.lblBinQtyCaption.Tag = "i18n:work.workInfo.workBinQty";
            this.lblBinQtyCaption.Text = "작업 BIN 수량";
            this.lblBinQtyCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workInfoPickFailTile
            // 
            this.workInfoPickFailTile.BackColor = System.Drawing.Color.White;
            this.workInfoPickFailTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workInfoPickFailTile.Controls.Add(this.lblPickFail);
            this.workInfoPickFailTile.Controls.Add(this.lblPickFailCaption);
            this.workInfoPickFailTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoPickFailTile.Location = new System.Drawing.Point(208, 82);
            this.workInfoPickFailTile.Name = "workInfoPickFailTile";
            this.workInfoPickFailTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workInfoPickFailTile.Size = new System.Drawing.Size(196, 58);
            this.workInfoPickFailTile.TabIndex = 3;
            // 
            // lblPickFail
            // 
            this.lblPickFail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickFail.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblPickFail.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(190)))), ((int)(((byte)(55)))), ((int)(((byte)(55)))));
            this.lblPickFail.Location = new System.Drawing.Point(8, 19);
            this.lblPickFail.Name = "lblPickFail";
            this.lblPickFail.Size = new System.Drawing.Size(178, 35);
            this.lblPickFail.TabIndex = 0;
            this.lblPickFail.Text = "0 ea";
            this.lblPickFail.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPickFailCaption
            // 
            this.lblPickFailCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPickFailCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblPickFailCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblPickFailCaption.Location = new System.Drawing.Point(8, 2);
            this.lblPickFailCaption.Name = "lblPickFailCaption";
            this.lblPickFailCaption.Size = new System.Drawing.Size(178, 17);
            this.lblPickFailCaption.TabIndex = 1;
            this.lblPickFailCaption.Tag = "i18n:work.workInfo.pickFail";
            this.lblPickFailCaption.Text = "PICK 실패 수량";
            this.lblPickFailCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workInfoPlaceFailTile
            // 
            this.workInfoPlaceFailTile.BackColor = System.Drawing.Color.White;
            this.workInfoPlaceFailTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workInfoPlaceFailTile.Controls.Add(this.lblPlaceFail);
            this.workInfoPlaceFailTile.Controls.Add(this.lblPlaceFailCaption);
            this.workInfoPlaceFailTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoPlaceFailTile.Location = new System.Drawing.Point(410, 82);
            this.workInfoPlaceFailTile.Name = "workInfoPlaceFailTile";
            this.workInfoPlaceFailTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workInfoPlaceFailTile.Size = new System.Drawing.Size(196, 58);
            this.workInfoPlaceFailTile.TabIndex = 4;
            // 
            // lblPlaceFail
            // 
            this.lblPlaceFail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlaceFail.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblPlaceFail.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(190)))), ((int)(((byte)(55)))), ((int)(((byte)(55)))));
            this.lblPlaceFail.Location = new System.Drawing.Point(8, 19);
            this.lblPlaceFail.Name = "lblPlaceFail";
            this.lblPlaceFail.Size = new System.Drawing.Size(178, 35);
            this.lblPlaceFail.TabIndex = 0;
            this.lblPlaceFail.Text = "0 ea";
            this.lblPlaceFail.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPlaceFailCaption
            // 
            this.lblPlaceFailCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPlaceFailCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblPlaceFailCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblPlaceFailCaption.Location = new System.Drawing.Point(8, 2);
            this.lblPlaceFailCaption.Name = "lblPlaceFailCaption";
            this.lblPlaceFailCaption.Size = new System.Drawing.Size(178, 17);
            this.lblPlaceFailCaption.TabIndex = 1;
            this.lblPlaceFailCaption.Tag = "i18n:work.workInfo.placeFail";
            this.lblPlaceFailCaption.Text = "PLACE 실패 수량";
            this.lblPlaceFailCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workInfoNeedleTile
            // 
            this.workInfoNeedleTile.BackColor = System.Drawing.Color.White;
            this.workInfoNeedleTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workInfoNeedleTile.Controls.Add(this.lblNeedle);
            this.workInfoNeedleTile.Controls.Add(this.lblNeedleCaption);
            this.workInfoNeedleTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoNeedleTile.Location = new System.Drawing.Point(612, 82);
            this.workInfoNeedleTile.Name = "workInfoNeedleTile";
            this.workInfoNeedleTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workInfoNeedleTile.Size = new System.Drawing.Size(197, 58);
            this.workInfoNeedleTile.TabIndex = 5;
            // 
            // lblNeedle
            // 
            this.lblNeedle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedle.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblNeedle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblNeedle.Location = new System.Drawing.Point(8, 19);
            this.lblNeedle.Name = "lblNeedle";
            this.lblNeedle.Size = new System.Drawing.Size(179, 35);
            this.lblNeedle.TabIndex = 0;
            this.lblNeedle.Text = "0";
            this.lblNeedle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNeedleCaption
            // 
            this.lblNeedleCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblNeedleCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblNeedleCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblNeedleCaption.Location = new System.Drawing.Point(8, 2);
            this.lblNeedleCaption.Name = "lblNeedleCaption";
            this.lblNeedleCaption.Size = new System.Drawing.Size(179, 17);
            this.lblNeedleCaption.TabIndex = 1;
            this.lblNeedleCaption.Tag = "i18n:work.workInfo.needleUse";
            this.lblNeedleCaption.Text = "NEEDLE 사용 횟수";
            this.lblNeedleCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workInfoFrontCollet1Tile
            // 
            this.workInfoFrontCollet1Tile.BackColor = System.Drawing.Color.White;
            this.workInfoFrontCollet1Tile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workInfoFrontCollet1Tile.Controls.Add(this.lblFrontCollet1Designer);
            this.workInfoFrontCollet1Tile.Controls.Add(this.lblFrontCollet1CaptionDesigner);
            this.workInfoFrontCollet1Tile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoFrontCollet1Tile.Location = new System.Drawing.Point(6, 146);
            this.workInfoFrontCollet1Tile.Name = "workInfoFrontCollet1Tile";
            this.workInfoFrontCollet1Tile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workInfoFrontCollet1Tile.Size = new System.Drawing.Size(196, 58);
            this.workInfoFrontCollet1Tile.TabIndex = 6;
            // 
            // lblFrontCollet1Designer
            // 
            this.lblFrontCollet1Designer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFrontCollet1Designer.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblFrontCollet1Designer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblFrontCollet1Designer.Location = new System.Drawing.Point(8, 19);
            this.lblFrontCollet1Designer.Name = "lblFrontCollet1Designer";
            this.lblFrontCollet1Designer.Size = new System.Drawing.Size(178, 35);
            this.lblFrontCollet1Designer.TabIndex = 0;
            this.lblFrontCollet1Designer.Text = "00 ea";
            this.lblFrontCollet1Designer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblFrontCollet1CaptionDesigner
            // 
            this.lblFrontCollet1CaptionDesigner.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFrontCollet1CaptionDesigner.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblFrontCollet1CaptionDesigner.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblFrontCollet1CaptionDesigner.Location = new System.Drawing.Point(8, 2);
            this.lblFrontCollet1CaptionDesigner.Name = "lblFrontCollet1CaptionDesigner";
            this.lblFrontCollet1CaptionDesigner.Size = new System.Drawing.Size(178, 17);
            this.lblFrontCollet1CaptionDesigner.TabIndex = 1;
            this.lblFrontCollet1CaptionDesigner.Text = "FRONT COLLET #1";
            this.lblFrontCollet1CaptionDesigner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workInfoFrontCollet2Tile
            // 
            this.workInfoFrontCollet2Tile.BackColor = System.Drawing.Color.White;
            this.workInfoFrontCollet2Tile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workInfoFrontCollet2Tile.Controls.Add(this.lblFrontCollet2Designer);
            this.workInfoFrontCollet2Tile.Controls.Add(this.lblFrontCollet2CaptionDesigner);
            this.workInfoFrontCollet2Tile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoFrontCollet2Tile.Location = new System.Drawing.Point(208, 146);
            this.workInfoFrontCollet2Tile.Name = "workInfoFrontCollet2Tile";
            this.workInfoFrontCollet2Tile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workInfoFrontCollet2Tile.Size = new System.Drawing.Size(196, 58);
            this.workInfoFrontCollet2Tile.TabIndex = 7;
            // 
            // lblFrontCollet2Designer
            // 
            this.lblFrontCollet2Designer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFrontCollet2Designer.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblFrontCollet2Designer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblFrontCollet2Designer.Location = new System.Drawing.Point(8, 19);
            this.lblFrontCollet2Designer.Name = "lblFrontCollet2Designer";
            this.lblFrontCollet2Designer.Size = new System.Drawing.Size(178, 35);
            this.lblFrontCollet2Designer.TabIndex = 0;
            this.lblFrontCollet2Designer.Text = "00 ea";
            this.lblFrontCollet2Designer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblFrontCollet2CaptionDesigner
            // 
            this.lblFrontCollet2CaptionDesigner.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFrontCollet2CaptionDesigner.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblFrontCollet2CaptionDesigner.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblFrontCollet2CaptionDesigner.Location = new System.Drawing.Point(8, 2);
            this.lblFrontCollet2CaptionDesigner.Name = "lblFrontCollet2CaptionDesigner";
            this.lblFrontCollet2CaptionDesigner.Size = new System.Drawing.Size(178, 17);
            this.lblFrontCollet2CaptionDesigner.TabIndex = 1;
            this.lblFrontCollet2CaptionDesigner.Text = "FRONT COLLET #2";
            this.lblFrontCollet2CaptionDesigner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workInfoFrontCollet3Tile
            // 
            this.workInfoFrontCollet3Tile.BackColor = System.Drawing.Color.White;
            this.workInfoFrontCollet3Tile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workInfoFrontCollet3Tile.Controls.Add(this.lblFrontCollet3Designer);
            this.workInfoFrontCollet3Tile.Controls.Add(this.lblFrontCollet3CaptionDesigner);
            this.workInfoFrontCollet3Tile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoFrontCollet3Tile.Location = new System.Drawing.Point(410, 146);
            this.workInfoFrontCollet3Tile.Name = "workInfoFrontCollet3Tile";
            this.workInfoFrontCollet3Tile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workInfoFrontCollet3Tile.Size = new System.Drawing.Size(196, 58);
            this.workInfoFrontCollet3Tile.TabIndex = 8;
            // 
            // lblFrontCollet3Designer
            // 
            this.lblFrontCollet3Designer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFrontCollet3Designer.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblFrontCollet3Designer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblFrontCollet3Designer.Location = new System.Drawing.Point(8, 19);
            this.lblFrontCollet3Designer.Name = "lblFrontCollet3Designer";
            this.lblFrontCollet3Designer.Size = new System.Drawing.Size(178, 35);
            this.lblFrontCollet3Designer.TabIndex = 0;
            this.lblFrontCollet3Designer.Text = "00 ea";
            this.lblFrontCollet3Designer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblFrontCollet3CaptionDesigner
            // 
            this.lblFrontCollet3CaptionDesigner.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFrontCollet3CaptionDesigner.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblFrontCollet3CaptionDesigner.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblFrontCollet3CaptionDesigner.Location = new System.Drawing.Point(8, 2);
            this.lblFrontCollet3CaptionDesigner.Name = "lblFrontCollet3CaptionDesigner";
            this.lblFrontCollet3CaptionDesigner.Size = new System.Drawing.Size(178, 17);
            this.lblFrontCollet3CaptionDesigner.TabIndex = 1;
            this.lblFrontCollet3CaptionDesigner.Text = "FRONT COLLET #3";
            this.lblFrontCollet3CaptionDesigner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workInfoFrontCollet4Tile
            // 
            this.workInfoFrontCollet4Tile.BackColor = System.Drawing.Color.White;
            this.workInfoFrontCollet4Tile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workInfoFrontCollet4Tile.Controls.Add(this.lblFrontCollet4Designer);
            this.workInfoFrontCollet4Tile.Controls.Add(this.lblFrontCollet4CaptionDesigner);
            this.workInfoFrontCollet4Tile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoFrontCollet4Tile.Location = new System.Drawing.Point(612, 146);
            this.workInfoFrontCollet4Tile.Name = "workInfoFrontCollet4Tile";
            this.workInfoFrontCollet4Tile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workInfoFrontCollet4Tile.Size = new System.Drawing.Size(197, 58);
            this.workInfoFrontCollet4Tile.TabIndex = 9;
            // 
            // lblFrontCollet4Designer
            // 
            this.lblFrontCollet4Designer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFrontCollet4Designer.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblFrontCollet4Designer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblFrontCollet4Designer.Location = new System.Drawing.Point(8, 19);
            this.lblFrontCollet4Designer.Name = "lblFrontCollet4Designer";
            this.lblFrontCollet4Designer.Size = new System.Drawing.Size(179, 35);
            this.lblFrontCollet4Designer.TabIndex = 0;
            this.lblFrontCollet4Designer.Text = "00 ea";
            this.lblFrontCollet4Designer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblFrontCollet4CaptionDesigner
            // 
            this.lblFrontCollet4CaptionDesigner.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFrontCollet4CaptionDesigner.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblFrontCollet4CaptionDesigner.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblFrontCollet4CaptionDesigner.Location = new System.Drawing.Point(8, 2);
            this.lblFrontCollet4CaptionDesigner.Name = "lblFrontCollet4CaptionDesigner";
            this.lblFrontCollet4CaptionDesigner.Size = new System.Drawing.Size(179, 17);
            this.lblFrontCollet4CaptionDesigner.TabIndex = 1;
            this.lblFrontCollet4CaptionDesigner.Text = "FRONT COLLET #4";
            this.lblFrontCollet4CaptionDesigner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workInfoRearCollet1Tile
            // 
            this.workInfoRearCollet1Tile.BackColor = System.Drawing.Color.White;
            this.workInfoRearCollet1Tile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workInfoRearCollet1Tile.Controls.Add(this.lblRearCollet1Designer);
            this.workInfoRearCollet1Tile.Controls.Add(this.lblRearCollet1CaptionDesigner);
            this.workInfoRearCollet1Tile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoRearCollet1Tile.Location = new System.Drawing.Point(6, 210);
            this.workInfoRearCollet1Tile.Name = "workInfoRearCollet1Tile";
            this.workInfoRearCollet1Tile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workInfoRearCollet1Tile.Size = new System.Drawing.Size(196, 59);
            this.workInfoRearCollet1Tile.TabIndex = 10;
            // 
            // lblRearCollet1Designer
            // 
            this.lblRearCollet1Designer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRearCollet1Designer.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblRearCollet1Designer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblRearCollet1Designer.Location = new System.Drawing.Point(8, 19);
            this.lblRearCollet1Designer.Name = "lblRearCollet1Designer";
            this.lblRearCollet1Designer.Size = new System.Drawing.Size(178, 36);
            this.lblRearCollet1Designer.TabIndex = 0;
            this.lblRearCollet1Designer.Text = "00 ea";
            this.lblRearCollet1Designer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRearCollet1CaptionDesigner
            // 
            this.lblRearCollet1CaptionDesigner.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRearCollet1CaptionDesigner.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblRearCollet1CaptionDesigner.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblRearCollet1CaptionDesigner.Location = new System.Drawing.Point(8, 2);
            this.lblRearCollet1CaptionDesigner.Name = "lblRearCollet1CaptionDesigner";
            this.lblRearCollet1CaptionDesigner.Size = new System.Drawing.Size(178, 17);
            this.lblRearCollet1CaptionDesigner.TabIndex = 1;
            this.lblRearCollet1CaptionDesigner.Text = "REAR COLLET #1";
            this.lblRearCollet1CaptionDesigner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workInfoRearCollet2Tile
            // 
            this.workInfoRearCollet2Tile.BackColor = System.Drawing.Color.White;
            this.workInfoRearCollet2Tile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workInfoRearCollet2Tile.Controls.Add(this.lblRearCollet2Designer);
            this.workInfoRearCollet2Tile.Controls.Add(this.lblRearCollet2CaptionDesigner);
            this.workInfoRearCollet2Tile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoRearCollet2Tile.Location = new System.Drawing.Point(208, 210);
            this.workInfoRearCollet2Tile.Name = "workInfoRearCollet2Tile";
            this.workInfoRearCollet2Tile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workInfoRearCollet2Tile.Size = new System.Drawing.Size(196, 59);
            this.workInfoRearCollet2Tile.TabIndex = 11;
            // 
            // lblRearCollet2Designer
            // 
            this.lblRearCollet2Designer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRearCollet2Designer.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblRearCollet2Designer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblRearCollet2Designer.Location = new System.Drawing.Point(8, 19);
            this.lblRearCollet2Designer.Name = "lblRearCollet2Designer";
            this.lblRearCollet2Designer.Size = new System.Drawing.Size(178, 36);
            this.lblRearCollet2Designer.TabIndex = 0;
            this.lblRearCollet2Designer.Text = "00 ea";
            this.lblRearCollet2Designer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRearCollet2CaptionDesigner
            // 
            this.lblRearCollet2CaptionDesigner.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRearCollet2CaptionDesigner.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblRearCollet2CaptionDesigner.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblRearCollet2CaptionDesigner.Location = new System.Drawing.Point(8, 2);
            this.lblRearCollet2CaptionDesigner.Name = "lblRearCollet2CaptionDesigner";
            this.lblRearCollet2CaptionDesigner.Size = new System.Drawing.Size(178, 17);
            this.lblRearCollet2CaptionDesigner.TabIndex = 1;
            this.lblRearCollet2CaptionDesigner.Text = "REAR COLLET #2";
            this.lblRearCollet2CaptionDesigner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workInfoRearCollet3Tile
            // 
            this.workInfoRearCollet3Tile.BackColor = System.Drawing.Color.White;
            this.workInfoRearCollet3Tile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workInfoRearCollet3Tile.Controls.Add(this.lblRearCollet3Designer);
            this.workInfoRearCollet3Tile.Controls.Add(this.lblRearCollet3CaptionDesigner);
            this.workInfoRearCollet3Tile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoRearCollet3Tile.Location = new System.Drawing.Point(410, 210);
            this.workInfoRearCollet3Tile.Name = "workInfoRearCollet3Tile";
            this.workInfoRearCollet3Tile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workInfoRearCollet3Tile.Size = new System.Drawing.Size(196, 59);
            this.workInfoRearCollet3Tile.TabIndex = 12;
            // 
            // lblRearCollet3Designer
            // 
            this.lblRearCollet3Designer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRearCollet3Designer.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblRearCollet3Designer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblRearCollet3Designer.Location = new System.Drawing.Point(8, 19);
            this.lblRearCollet3Designer.Name = "lblRearCollet3Designer";
            this.lblRearCollet3Designer.Size = new System.Drawing.Size(178, 36);
            this.lblRearCollet3Designer.TabIndex = 0;
            this.lblRearCollet3Designer.Text = "00 ea";
            this.lblRearCollet3Designer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRearCollet3CaptionDesigner
            // 
            this.lblRearCollet3CaptionDesigner.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRearCollet3CaptionDesigner.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblRearCollet3CaptionDesigner.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblRearCollet3CaptionDesigner.Location = new System.Drawing.Point(8, 2);
            this.lblRearCollet3CaptionDesigner.Name = "lblRearCollet3CaptionDesigner";
            this.lblRearCollet3CaptionDesigner.Size = new System.Drawing.Size(178, 17);
            this.lblRearCollet3CaptionDesigner.TabIndex = 1;
            this.lblRearCollet3CaptionDesigner.Text = "REAR COLLET #3";
            this.lblRearCollet3CaptionDesigner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workInfoRearCollet4Tile
            // 
            this.workInfoRearCollet4Tile.BackColor = System.Drawing.Color.White;
            this.workInfoRearCollet4Tile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workInfoRearCollet4Tile.Controls.Add(this.lblRearCollet4Designer);
            this.workInfoRearCollet4Tile.Controls.Add(this.lblRearCollet4CaptionDesigner);
            this.workInfoRearCollet4Tile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoRearCollet4Tile.Location = new System.Drawing.Point(612, 210);
            this.workInfoRearCollet4Tile.Name = "workInfoRearCollet4Tile";
            this.workInfoRearCollet4Tile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workInfoRearCollet4Tile.Size = new System.Drawing.Size(197, 59);
            this.workInfoRearCollet4Tile.TabIndex = 13;
            // 
            // lblRearCollet4Designer
            // 
            this.lblRearCollet4Designer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRearCollet4Designer.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblRearCollet4Designer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblRearCollet4Designer.Location = new System.Drawing.Point(8, 19);
            this.lblRearCollet4Designer.Name = "lblRearCollet4Designer";
            this.lblRearCollet4Designer.Size = new System.Drawing.Size(179, 36);
            this.lblRearCollet4Designer.TabIndex = 0;
            this.lblRearCollet4Designer.Text = "00 ea";
            this.lblRearCollet4Designer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRearCollet4CaptionDesigner
            // 
            this.lblRearCollet4CaptionDesigner.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRearCollet4CaptionDesigner.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblRearCollet4CaptionDesigner.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblRearCollet4CaptionDesigner.Location = new System.Drawing.Point(8, 2);
            this.lblRearCollet4CaptionDesigner.Name = "lblRearCollet4CaptionDesigner";
            this.lblRearCollet4CaptionDesigner.Size = new System.Drawing.Size(179, 17);
            this.lblRearCollet4CaptionDesigner.TabIndex = 1;
            this.lblRearCollet4CaptionDesigner.Text = "REAR COLLET #4";
            this.lblRearCollet4CaptionDesigner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpTime
            // 
            this.grpTime.BackColor = System.Drawing.Color.White;
            this.grpTime.Controls.Add(this.workTimeBody);
            this.grpTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpTime.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            this.grpTime.Location = new System.Drawing.Point(843, 587);
            this.grpTime.Margin = new System.Windows.Forms.Padding(4);
            this.grpTime.Name = "grpTime";
            this.grpTime.Padding = new System.Windows.Forms.Padding(6, 4, 6, 6);
            this.grpTime.Size = new System.Drawing.Size(827, 305);
            this.grpTime.TabIndex = 3;
            this.grpTime.TabStop = false;
            this.grpTime.Tag = "i18n:work.sec.workTime";
            this.grpTime.Text = "작업 시간";
            // 
            // workTimeBody
            // 
            this.workTimeBody.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(245)))));
            this.workTimeBody.ColumnCount = 4;
            this.workTimeBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workTimeBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workTimeBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workTimeBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workTimeBody.Controls.Add(this.workTimeLotTile, 0, 0);
            this.workTimeBody.Controls.Add(this.workTimeActionPanel, 2, 0);
            this.workTimeBody.Controls.Add(this.workTimeUphTile, 0, 1);
            this.workTimeBody.Controls.Add(this.workTimeUpTile, 1, 1);
            this.workTimeBody.Controls.Add(this.workTimeContUpTile, 2, 1);
            this.workTimeBody.Controls.Add(this.workTimeRateTile, 3, 1);
            this.workTimeBody.Controls.Add(this.workTimeCycleTile, 0, 2);
            this.workTimeBody.Controls.Add(this.workTimeLoadTile, 1, 2);
            this.workTimeBody.Controls.Add(this.workTimeMtbfTile, 2, 2);
            this.workTimeBody.Controls.Add(this.workTimeMttrTile, 3, 2);
            this.workTimeBody.Controls.Add(this.workTimeRecoveryTile, 0, 3);
            this.workTimeBody.Controls.Add(this.workTimeNormDownTile, 1, 3);
            this.workTimeBody.Controls.Add(this.workTimeErrDownTile, 2, 3);
            this.workTimeBody.Controls.Add(this.workTimeErrCntTile, 3, 3);
            this.workTimeBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeBody.Location = new System.Drawing.Point(6, 24);
            this.workTimeBody.Margin = new System.Windows.Forms.Padding(0);
            this.workTimeBody.Name = "workTimeBody";
            this.workTimeBody.Padding = new System.Windows.Forms.Padding(3);
            this.workTimeBody.RowCount = 4;
            this.workTimeBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.workTimeBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.workTimeBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.workTimeBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.workTimeBody.Size = new System.Drawing.Size(815, 275);
            this.workTimeBody.TabIndex = 0;
            // 
            // workTimeLotTile
            // 
            this.workTimeLotTile.BackColor = System.Drawing.Color.White;
            this.workTimeLotTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workTimeBody.SetColumnSpan(this.workTimeLotTile, 2);
            this.workTimeLotTile.Controls.Add(this.lblLot);
            this.workTimeLotTile.Controls.Add(this.lblLotCaption);
            this.workTimeLotTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeLotTile.Location = new System.Drawing.Point(6, 5);
            this.workTimeLotTile.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.workTimeLotTile.Name = "workTimeLotTile";
            this.workTimeLotTile.Padding = new System.Windows.Forms.Padding(8, 1, 8, 1);
            this.workTimeLotTile.Size = new System.Drawing.Size(398, 38);
            this.workTimeLotTile.TabIndex = 0;
            // 
            // lblLot
            // 
            this.lblLot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLot.Font = new System.Drawing.Font("Consolas", 11F, System.Drawing.FontStyle.Bold);
            this.lblLot.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblLot.Location = new System.Drawing.Point(156, 1);
            this.lblLot.Name = "lblLot";
            this.lblLot.Size = new System.Drawing.Size(232, 34);
            this.lblLot.TabIndex = 0;
            this.lblLot.Text = "(no lot)";
            this.lblLot.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLotCaption
            // 
            this.lblLotCaption.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblLotCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblLotCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblLotCaption.Location = new System.Drawing.Point(8, 1);
            this.lblLotCaption.Name = "lblLotCaption";
            this.lblLotCaption.Size = new System.Drawing.Size(148, 34);
            this.lblLotCaption.TabIndex = 1;
            this.lblLotCaption.Tag = "i18n:work.workTime.lotId";
            this.lblLotCaption.Text = "작업중인 LOT ID";
            this.lblLotCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workTimeActionPanel
            // 
            this.workTimeActionPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(245)))));
            this.workTimeActionPanel.ColumnCount = 3;
            this.workTimeBody.SetColumnSpan(this.workTimeActionPanel, 2);
            this.workTimeActionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.workTimeActionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.workTimeActionPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.workTimeActionPanel.Controls.Add(this.btnCcs, 0, 0);
            this.workTimeActionPanel.Controls.Add(this.btnWorkTimeClear, 1, 0);
            this.workTimeActionPanel.Controls.Add(this.btnTestAlarm, 2, 0);
            this.workTimeActionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeActionPanel.Location = new System.Drawing.Point(407, 3);
            this.workTimeActionPanel.Margin = new System.Windows.Forms.Padding(0);
            this.workTimeActionPanel.Name = "workTimeActionPanel";
            this.workTimeActionPanel.RowCount = 1;
            this.workTimeActionPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.workTimeActionPanel.Size = new System.Drawing.Size(405, 42);
            this.workTimeActionPanel.TabIndex = 1;
            // 
            // btnCcs
            // 
            this.btnCcs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(89)))), ((int)(((byte)(89)))), ((int)(((byte)(89)))));
            this.btnCcs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCcs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCcs.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnCcs.ForeColor = System.Drawing.Color.White;
            this.btnCcs.Location = new System.Drawing.Point(3, 2);
            this.btnCcs.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnCcs.Name = "btnCcs";
            this.btnCcs.Size = new System.Drawing.Size(129, 38);
            this.btnCcs.TabIndex = 26;
            this.btnCcs.Tag = "i18n:work.workTime.ccs";
            this.btnCcs.Text = "CCS 검수 확인";
            this.btnCcs.UseVisualStyleBackColor = false;
            this.btnCcs.Click += new System.EventHandler(this.btnCcs_Click);
            // 
            // btnWorkTimeClear
            // 
            this.btnWorkTimeClear.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.btnWorkTimeClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnWorkTimeClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnWorkTimeClear.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnWorkTimeClear.ForeColor = System.Drawing.Color.White;
            this.btnWorkTimeClear.Location = new System.Drawing.Point(138, 2);
            this.btnWorkTimeClear.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnWorkTimeClear.Name = "btnWorkTimeClear";
            this.btnWorkTimeClear.Size = new System.Drawing.Size(129, 38);
            this.btnWorkTimeClear.TabIndex = 27;
            this.btnWorkTimeClear.Tag = "i18n:work.workTime.clear";
            this.btnWorkTimeClear.Text = "CLEAR";
            this.btnWorkTimeClear.UseVisualStyleBackColor = false;
            // 
            // btnTestAlarm
            // 
            this.btnTestAlarm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(57)))), ((int)(((byte)(43)))));
            this.btnTestAlarm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTestAlarm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTestAlarm.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnTestAlarm.ForeColor = System.Drawing.Color.White;
            this.btnTestAlarm.Location = new System.Drawing.Point(273, 2);
            this.btnTestAlarm.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnTestAlarm.Name = "btnTestAlarm";
            this.btnTestAlarm.Size = new System.Drawing.Size(129, 38);
            this.btnTestAlarm.TabIndex = 28;
            this.btnTestAlarm.Tag = "i18n:work.workTime.alarm";
            this.btnTestAlarm.Text = "ALARM";
            this.btnTestAlarm.UseVisualStyleBackColor = false;
            // 
            // workTimeUphTile
            // 
            this.workTimeUphTile.BackColor = System.Drawing.Color.White;
            this.workTimeUphTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workTimeUphTile.Controls.Add(this.workTimeUphValueLayout);
            this.workTimeUphTile.Controls.Add(this.workTimeUphHeaderLayout);
            this.workTimeUphTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeUphTile.Location = new System.Drawing.Point(6, 48);
            this.workTimeUphTile.Name = "workTimeUphTile";
            this.workTimeUphTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workTimeUphTile.Size = new System.Drawing.Size(196, 69);
            this.workTimeUphTile.TabIndex = 2;
            // 
            // workTimeUphValueLayout
            // 
            this.workTimeUphValueLayout.ColumnCount = 1;
            this.workTimeUphValueLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.workTimeUphValueLayout.Controls.Add(this.lblUph, 0, 0);
            this.workTimeUphValueLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeUphValueLayout.Location = new System.Drawing.Point(8, 19);
            this.workTimeUphValueLayout.Margin = new System.Windows.Forms.Padding(0);
            this.workTimeUphValueLayout.Name = "workTimeUphValueLayout";
            this.workTimeUphValueLayout.RowCount = 1;
            this.workTimeUphValueLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.workTimeUphValueLayout.Size = new System.Drawing.Size(178, 46);
            this.workTimeUphValueLayout.TabIndex = 0;
            // 
            // lblUph
            // 
            this.lblUph.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUph.Font = new System.Drawing.Font("Consolas", 10.5F, System.Drawing.FontStyle.Bold);
            this.lblUph.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(95)))), ((int)(((byte)(165)))));
            this.lblUph.Location = new System.Drawing.Point(3, 0);
            this.lblUph.Name = "lblUph";
            this.lblUph.Size = new System.Drawing.Size(172, 46);
            this.lblUph.TabIndex = 0;
            this.lblUph.Text = "0.00";
            this.lblUph.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workTimeUphHeaderLayout
            // 
            this.workTimeUphHeaderLayout.ColumnCount = 2;
            this.workTimeUphHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.workTimeUphHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this.workTimeUphHeaderLayout.Controls.Add(this.lblUphCaption, 0, 0);
            this.workTimeUphHeaderLayout.Controls.Add(this.lblRecentMinuteUph, 1, 0);
            this.workTimeUphHeaderLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.workTimeUphHeaderLayout.Location = new System.Drawing.Point(8, 2);
            this.workTimeUphHeaderLayout.Margin = new System.Windows.Forms.Padding(0);
            this.workTimeUphHeaderLayout.Name = "workTimeUphHeaderLayout";
            this.workTimeUphHeaderLayout.RowCount = 1;
            this.workTimeUphHeaderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.workTimeUphHeaderLayout.Size = new System.Drawing.Size(178, 17);
            this.workTimeUphHeaderLayout.TabIndex = 1;
            // 
            // lblUphCaption
            // 
            this.lblUphCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUphCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblUphCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblUphCaption.Location = new System.Drawing.Point(3, 0);
            this.lblUphCaption.Name = "lblUphCaption";
            this.lblUphCaption.Size = new System.Drawing.Size(68, 17);
            this.lblUphCaption.TabIndex = 0;
            this.lblUphCaption.Tag = "i18n:work.workTime.uph";
            this.lblUphCaption.Text = "UPH";
            this.lblUphCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecentMinuteUph
            // 
            this.lblRecentMinuteUph.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRecentMinuteUph.Font = new System.Drawing.Font("Consolas", 8F, System.Drawing.FontStyle.Bold);
            this.lblRecentMinuteUph.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(139)))), ((int)(((byte)(94)))));
            this.lblRecentMinuteUph.Location = new System.Drawing.Point(77, 0);
            this.lblRecentMinuteUph.Name = "lblRecentMinuteUph";
            this.lblRecentMinuteUph.Size = new System.Drawing.Size(98, 17);
            this.lblRecentMinuteUph.TabIndex = 1;
            this.lblRecentMinuteUph.Text = "1M 0 ea";
            this.lblRecentMinuteUph.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblRecentMinuteUph.Visible = false;
            // 
            // workTimeUpTile
            // 
            this.workTimeUpTile.BackColor = System.Drawing.Color.White;
            this.workTimeUpTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workTimeUpTile.Controls.Add(this.lblUp);
            this.workTimeUpTile.Controls.Add(this.lblUpCaption);
            this.workTimeUpTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeUpTile.Location = new System.Drawing.Point(208, 48);
            this.workTimeUpTile.Name = "workTimeUpTile";
            this.workTimeUpTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workTimeUpTile.Size = new System.Drawing.Size(196, 69);
            this.workTimeUpTile.TabIndex = 3;
            // 
            // lblUp
            // 
            this.lblUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUp.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblUp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblUp.Location = new System.Drawing.Point(8, 19);
            this.lblUp.Name = "lblUp";
            this.lblUp.Size = new System.Drawing.Size(178, 46);
            this.lblUp.TabIndex = 0;
            this.lblUp.Text = "00:00:00";
            this.lblUp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblUpCaption
            // 
            this.lblUpCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblUpCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblUpCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblUpCaption.Location = new System.Drawing.Point(8, 2);
            this.lblUpCaption.Name = "lblUpCaption";
            this.lblUpCaption.Size = new System.Drawing.Size(178, 17);
            this.lblUpCaption.TabIndex = 1;
            this.lblUpCaption.Tag = "i18n:work.workTime.up";
            this.lblUpCaption.Text = "가동 시간";
            this.lblUpCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workTimeContUpTile
            // 
            this.workTimeContUpTile.BackColor = System.Drawing.Color.White;
            this.workTimeContUpTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workTimeContUpTile.Controls.Add(this.lblContUp);
            this.workTimeContUpTile.Controls.Add(this.lblContUpCaption);
            this.workTimeContUpTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeContUpTile.Location = new System.Drawing.Point(410, 48);
            this.workTimeContUpTile.Name = "workTimeContUpTile";
            this.workTimeContUpTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workTimeContUpTile.Size = new System.Drawing.Size(196, 69);
            this.workTimeContUpTile.TabIndex = 4;
            // 
            // lblContUp
            // 
            this.lblContUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblContUp.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblContUp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblContUp.Location = new System.Drawing.Point(8, 19);
            this.lblContUp.Name = "lblContUp";
            this.lblContUp.Size = new System.Drawing.Size(178, 46);
            this.lblContUp.TabIndex = 0;
            this.lblContUp.Text = "00:00:00";
            this.lblContUp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblContUpCaption
            // 
            this.lblContUpCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblContUpCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblContUpCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblContUpCaption.Location = new System.Drawing.Point(8, 2);
            this.lblContUpCaption.Name = "lblContUpCaption";
            this.lblContUpCaption.Size = new System.Drawing.Size(178, 17);
            this.lblContUpCaption.TabIndex = 1;
            this.lblContUpCaption.Tag = "i18n:work.workTime.contUp";
            this.lblContUpCaption.Text = "연속 가동 시간";
            this.lblContUpCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workTimeRateTile
            // 
            this.workTimeRateTile.BackColor = System.Drawing.Color.White;
            this.workTimeRateTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workTimeRateTile.Controls.Add(this.lblRate);
            this.workTimeRateTile.Controls.Add(this.lblRateCaption);
            this.workTimeRateTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeRateTile.Location = new System.Drawing.Point(612, 48);
            this.workTimeRateTile.Name = "workTimeRateTile";
            this.workTimeRateTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workTimeRateTile.Size = new System.Drawing.Size(197, 69);
            this.workTimeRateTile.TabIndex = 5;
            // 
            // lblRate
            // 
            this.lblRate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRate.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblRate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(120)))), ((int)(((byte)(60)))));
            this.lblRate.Location = new System.Drawing.Point(8, 19);
            this.lblRate.Name = "lblRate";
            this.lblRate.Size = new System.Drawing.Size(179, 46);
            this.lblRate.TabIndex = 0;
            this.lblRate.Text = "0.00 %";
            this.lblRate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRateCaption
            // 
            this.lblRateCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRateCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblRateCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblRateCaption.Location = new System.Drawing.Point(8, 2);
            this.lblRateCaption.Name = "lblRateCaption";
            this.lblRateCaption.Size = new System.Drawing.Size(179, 17);
            this.lblRateCaption.TabIndex = 1;
            this.lblRateCaption.Tag = "i18n:work.workTime.rate";
            this.lblRateCaption.Text = "가동률";
            this.lblRateCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workTimeCycleTile
            // 
            this.workTimeCycleTile.BackColor = System.Drawing.Color.White;
            this.workTimeCycleTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workTimeCycleTile.Controls.Add(this.lblCycle);
            this.workTimeCycleTile.Controls.Add(this.lblCycleCaption);
            this.workTimeCycleTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeCycleTile.Location = new System.Drawing.Point(6, 123);
            this.workTimeCycleTile.Name = "workTimeCycleTile";
            this.workTimeCycleTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workTimeCycleTile.Size = new System.Drawing.Size(196, 69);
            this.workTimeCycleTile.TabIndex = 6;
            // 
            // lblCycle
            // 
            this.lblCycle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCycle.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblCycle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblCycle.Location = new System.Drawing.Point(8, 19);
            this.lblCycle.Name = "lblCycle";
            this.lblCycle.Size = new System.Drawing.Size(178, 46);
            this.lblCycle.TabIndex = 0;
            this.lblCycle.Text = "0 ms";
            this.lblCycle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCycleCaption
            // 
            this.lblCycleCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCycleCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblCycleCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblCycleCaption.Location = new System.Drawing.Point(8, 2);
            this.lblCycleCaption.Name = "lblCycleCaption";
            this.lblCycleCaption.Size = new System.Drawing.Size(178, 17);
            this.lblCycleCaption.TabIndex = 1;
            this.lblCycleCaption.Tag = "i18n:work.workTime.cycle";
            this.lblCycleCaption.Text = "CYCLE TIME";
            this.lblCycleCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workTimeLoadTile
            // 
            this.workTimeLoadTile.BackColor = System.Drawing.Color.White;
            this.workTimeLoadTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workTimeLoadTile.Controls.Add(this.lblLoad);
            this.workTimeLoadTile.Controls.Add(this.lblLoadCaption);
            this.workTimeLoadTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeLoadTile.Location = new System.Drawing.Point(208, 123);
            this.workTimeLoadTile.Name = "workTimeLoadTile";
            this.workTimeLoadTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workTimeLoadTile.Size = new System.Drawing.Size(196, 69);
            this.workTimeLoadTile.TabIndex = 7;
            // 
            // lblLoad
            // 
            this.lblLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLoad.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblLoad.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblLoad.Location = new System.Drawing.Point(8, 19);
            this.lblLoad.Name = "lblLoad";
            this.lblLoad.Size = new System.Drawing.Size(178, 46);
            this.lblLoad.TabIndex = 0;
            this.lblLoad.Text = "00:00:00";
            this.lblLoad.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLoadCaption
            // 
            this.lblLoadCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLoadCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblLoadCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblLoadCaption.Location = new System.Drawing.Point(8, 2);
            this.lblLoadCaption.Name = "lblLoadCaption";
            this.lblLoadCaption.Size = new System.Drawing.Size(178, 17);
            this.lblLoadCaption.TabIndex = 1;
            this.lblLoadCaption.Tag = "i18n:work.workTime.load";
            this.lblLoadCaption.Text = "부하 시간";
            this.lblLoadCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workTimeMtbfTile
            // 
            this.workTimeMtbfTile.BackColor = System.Drawing.Color.White;
            this.workTimeMtbfTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workTimeMtbfTile.Controls.Add(this.lblMtbf);
            this.workTimeMtbfTile.Controls.Add(this.lblMtbfCaption);
            this.workTimeMtbfTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeMtbfTile.Location = new System.Drawing.Point(410, 123);
            this.workTimeMtbfTile.Name = "workTimeMtbfTile";
            this.workTimeMtbfTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workTimeMtbfTile.Size = new System.Drawing.Size(196, 69);
            this.workTimeMtbfTile.TabIndex = 8;
            // 
            // lblMtbf
            // 
            this.lblMtbf.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMtbf.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblMtbf.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblMtbf.Location = new System.Drawing.Point(8, 19);
            this.lblMtbf.Name = "lblMtbf";
            this.lblMtbf.Size = new System.Drawing.Size(178, 46);
            this.lblMtbf.TabIndex = 0;
            this.lblMtbf.Text = "00:00:00";
            this.lblMtbf.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMtbfCaption
            // 
            this.lblMtbfCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblMtbfCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblMtbfCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblMtbfCaption.Location = new System.Drawing.Point(8, 2);
            this.lblMtbfCaption.Name = "lblMtbfCaption";
            this.lblMtbfCaption.Size = new System.Drawing.Size(178, 17);
            this.lblMtbfCaption.TabIndex = 1;
            this.lblMtbfCaption.Tag = "i18n:work.workTime.mtbf";
            this.lblMtbfCaption.Text = "MTBF";
            this.lblMtbfCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workTimeMttrTile
            // 
            this.workTimeMttrTile.BackColor = System.Drawing.Color.White;
            this.workTimeMttrTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workTimeMttrTile.Controls.Add(this.lblMttr);
            this.workTimeMttrTile.Controls.Add(this.lblMttrCaption);
            this.workTimeMttrTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeMttrTile.Location = new System.Drawing.Point(612, 123);
            this.workTimeMttrTile.Name = "workTimeMttrTile";
            this.workTimeMttrTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workTimeMttrTile.Size = new System.Drawing.Size(197, 69);
            this.workTimeMttrTile.TabIndex = 9;
            // 
            // lblMttr
            // 
            this.lblMttr.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMttr.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblMttr.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblMttr.Location = new System.Drawing.Point(8, 19);
            this.lblMttr.Name = "lblMttr";
            this.lblMttr.Size = new System.Drawing.Size(179, 46);
            this.lblMttr.TabIndex = 0;
            this.lblMttr.Text = "00:00:00";
            this.lblMttr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMttrCaption
            // 
            this.lblMttrCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblMttrCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblMttrCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblMttrCaption.Location = new System.Drawing.Point(8, 2);
            this.lblMttrCaption.Name = "lblMttrCaption";
            this.lblMttrCaption.Size = new System.Drawing.Size(179, 17);
            this.lblMttrCaption.TabIndex = 1;
            this.lblMttrCaption.Tag = "i18n:work.workTime.mttr";
            this.lblMttrCaption.Text = "MTTR";
            this.lblMttrCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workTimeRecoveryTile
            // 
            this.workTimeRecoveryTile.BackColor = System.Drawing.Color.White;
            this.workTimeRecoveryTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workTimeRecoveryTile.Controls.Add(this.lblRecovery);
            this.workTimeRecoveryTile.Controls.Add(this.lblRecoveryCaption);
            this.workTimeRecoveryTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeRecoveryTile.Location = new System.Drawing.Point(6, 198);
            this.workTimeRecoveryTile.Name = "workTimeRecoveryTile";
            this.workTimeRecoveryTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workTimeRecoveryTile.Size = new System.Drawing.Size(196, 71);
            this.workTimeRecoveryTile.TabIndex = 10;
            // 
            // lblRecovery
            // 
            this.lblRecovery.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRecovery.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblRecovery.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblRecovery.Location = new System.Drawing.Point(8, 19);
            this.lblRecovery.Name = "lblRecovery";
            this.lblRecovery.Size = new System.Drawing.Size(178, 48);
            this.lblRecovery.TabIndex = 0;
            this.lblRecovery.Text = "00:00:00";
            this.lblRecovery.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecoveryCaption
            // 
            this.lblRecoveryCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecoveryCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblRecoveryCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblRecoveryCaption.Location = new System.Drawing.Point(8, 2);
            this.lblRecoveryCaption.Name = "lblRecoveryCaption";
            this.lblRecoveryCaption.Size = new System.Drawing.Size(178, 17);
            this.lblRecoveryCaption.TabIndex = 1;
            this.lblRecoveryCaption.Tag = "i18n:work.workTime.recovery";
            this.lblRecoveryCaption.Text = "이상 복귀 시간";
            this.lblRecoveryCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workTimeNormDownTile
            // 
            this.workTimeNormDownTile.BackColor = System.Drawing.Color.White;
            this.workTimeNormDownTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workTimeNormDownTile.Controls.Add(this.lblNormDown);
            this.workTimeNormDownTile.Controls.Add(this.lblNormDownCaption);
            this.workTimeNormDownTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeNormDownTile.Location = new System.Drawing.Point(208, 198);
            this.workTimeNormDownTile.Name = "workTimeNormDownTile";
            this.workTimeNormDownTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workTimeNormDownTile.Size = new System.Drawing.Size(196, 71);
            this.workTimeNormDownTile.TabIndex = 11;
            // 
            // lblNormDown
            // 
            this.lblNormDown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNormDown.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblNormDown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblNormDown.Location = new System.Drawing.Point(8, 19);
            this.lblNormDown.Name = "lblNormDown";
            this.lblNormDown.Size = new System.Drawing.Size(178, 48);
            this.lblNormDown.TabIndex = 0;
            this.lblNormDown.Text = "00:00:00";
            this.lblNormDown.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNormDownCaption
            // 
            this.lblNormDownCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblNormDownCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblNormDownCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblNormDownCaption.Location = new System.Drawing.Point(8, 2);
            this.lblNormDownCaption.Name = "lblNormDownCaption";
            this.lblNormDownCaption.Size = new System.Drawing.Size(178, 17);
            this.lblNormDownCaption.TabIndex = 1;
            this.lblNormDownCaption.Tag = "i18n:work.workTime.normDown";
            this.lblNormDownCaption.Text = "통상 정지 시간";
            this.lblNormDownCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workTimeErrDownTile
            // 
            this.workTimeErrDownTile.BackColor = System.Drawing.Color.White;
            this.workTimeErrDownTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workTimeErrDownTile.Controls.Add(this.lblErrDown);
            this.workTimeErrDownTile.Controls.Add(this.lblErrDownCaption);
            this.workTimeErrDownTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeErrDownTile.Location = new System.Drawing.Point(410, 198);
            this.workTimeErrDownTile.Name = "workTimeErrDownTile";
            this.workTimeErrDownTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workTimeErrDownTile.Size = new System.Drawing.Size(196, 71);
            this.workTimeErrDownTile.TabIndex = 12;
            // 
            // lblErrDown
            // 
            this.lblErrDown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblErrDown.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblErrDown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(190)))), ((int)(((byte)(55)))), ((int)(((byte)(55)))));
            this.lblErrDown.Location = new System.Drawing.Point(8, 19);
            this.lblErrDown.Name = "lblErrDown";
            this.lblErrDown.Size = new System.Drawing.Size(178, 48);
            this.lblErrDown.TabIndex = 0;
            this.lblErrDown.Text = "00:00:00";
            this.lblErrDown.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblErrDownCaption
            // 
            this.lblErrDownCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblErrDownCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblErrDownCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblErrDownCaption.Location = new System.Drawing.Point(8, 2);
            this.lblErrDownCaption.Name = "lblErrDownCaption";
            this.lblErrDownCaption.Size = new System.Drawing.Size(178, 17);
            this.lblErrDownCaption.TabIndex = 1;
            this.lblErrDownCaption.Tag = "i18n:work.workTime.errDown";
            this.lblErrDownCaption.Text = "이상 정지 시간";
            this.lblErrDownCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workTimeErrCntTile
            // 
            this.workTimeErrCntTile.BackColor = System.Drawing.Color.White;
            this.workTimeErrCntTile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.workTimeErrCntTile.Controls.Add(this.lblErrCnt);
            this.workTimeErrCntTile.Controls.Add(this.lblErrCntCaption);
            this.workTimeErrCntTile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeErrCntTile.Location = new System.Drawing.Point(612, 198);
            this.workTimeErrCntTile.Name = "workTimeErrCntTile";
            this.workTimeErrCntTile.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.workTimeErrCntTile.Size = new System.Drawing.Size(197, 71);
            this.workTimeErrCntTile.TabIndex = 13;
            // 
            // lblErrCnt
            // 
            this.lblErrCnt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblErrCnt.Font = new System.Drawing.Font("Consolas", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblErrCnt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(190)))), ((int)(((byte)(55)))), ((int)(((byte)(55)))));
            this.lblErrCnt.Location = new System.Drawing.Point(8, 19);
            this.lblErrCnt.Name = "lblErrCnt";
            this.lblErrCnt.Size = new System.Drawing.Size(179, 48);
            this.lblErrCnt.TabIndex = 0;
            this.lblErrCnt.Text = "0 ea";
            this.lblErrCnt.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblErrCntCaption
            // 
            this.lblErrCntCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblErrCntCaption.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblErrCntCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblErrCntCaption.Location = new System.Drawing.Point(8, 2);
            this.lblErrCntCaption.Name = "lblErrCntCaption";
            this.lblErrCntCaption.Size = new System.Drawing.Size(179, 17);
            this.lblErrCntCaption.TabIndex = 1;
            this.lblErrCntCaption.Tag = "i18n:work.workTime.errCnt";
            this.lblErrCntCaption.Text = "이상 정지 횟수";
            this.lblErrCntCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVisionCaption
            // 
            this.lblVisionCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblVisionCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblVisionCaption.Location = new System.Drawing.Point(0, 0);
            this.lblVisionCaption.Name = "lblVisionCaption";
            this.lblVisionCaption.Size = new System.Drawing.Size(74, 26);
            this.lblVisionCaption.TabIndex = 4;
            this.lblVisionCaption.Text = "VISION";
            this.lblVisionCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPickCaption
            // 
            this.lblPickCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblPickCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblPickCaption.Location = new System.Drawing.Point(0, 0);
            this.lblPickCaption.Name = "lblPickCaption";
            this.lblPickCaption.Size = new System.Drawing.Size(64, 26);
            this.lblPickCaption.TabIndex = 5;
            this.lblPickCaption.Text = "PICK";
            this.lblPickCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPlaceCaption
            // 
            this.lblPlaceCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlaceCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblPlaceCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblPlaceCaption.Location = new System.Drawing.Point(0, 0);
            this.lblPlaceCaption.Name = "lblPlaceCaption";
            this.lblPlaceCaption.Size = new System.Drawing.Size(64, 26);
            this.lblPlaceCaption.TabIndex = 6;
            this.lblPlaceCaption.Text = "PLACE";
            this.lblPlaceCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblCollet1Caption
            // 
            this.lblCollet1Caption.Location = new System.Drawing.Point(0, 0);
            this.lblCollet1Caption.Name = "lblCollet1Caption";
            this.lblCollet1Caption.Size = new System.Drawing.Size(100, 23);
            this.lblCollet1Caption.TabIndex = 0;
            this.lblCollet1Caption.Tag = "i18n:work.workInfo.collet1Use";
            this.lblCollet1Caption.Text = "# 1 Collet 사용";
            // 
            // lblCollet1
            // 
            this.lblCollet1.Location = new System.Drawing.Point(0, 0);
            this.lblCollet1.Name = "lblCollet1";
            this.lblCollet1.Size = new System.Drawing.Size(100, 23);
            this.lblCollet1.TabIndex = 0;
            this.lblCollet1.Text = "0";
            // 
            // lblCollet2Caption
            // 
            this.lblCollet2Caption.Location = new System.Drawing.Point(0, 0);
            this.lblCollet2Caption.Name = "lblCollet2Caption";
            this.lblCollet2Caption.Size = new System.Drawing.Size(100, 23);
            this.lblCollet2Caption.TabIndex = 0;
            this.lblCollet2Caption.Tag = "i18n:work.workInfo.collet2Use";
            this.lblCollet2Caption.Text = "# 2 Collet 사용";
            // 
            // lblCollet2
            // 
            this.lblCollet2.Location = new System.Drawing.Point(0, 0);
            this.lblCollet2.Name = "lblCollet2";
            this.lblCollet2.Size = new System.Drawing.Size(100, 23);
            this.lblCollet2.TabIndex = 0;
            this.lblCollet2.Text = "0";
            // 
            // lblBinArrMonCaption
            // 
            this.lblBinArrMonCaption.Location = new System.Drawing.Point(0, 0);
            this.lblBinArrMonCaption.Name = "lblBinArrMonCaption";
            this.lblBinArrMonCaption.Size = new System.Drawing.Size(100, 23);
            this.lblBinArrMonCaption.TabIndex = 0;
            this.lblBinArrMonCaption.Tag = "i18n:work.workInfo.binArrMon";
            this.lblBinArrMonCaption.Text = "빈 배열 모니터링";
            // 
            // lblBinArrMon
            // 
            this.lblBinArrMon.Location = new System.Drawing.Point(0, 0);
            this.lblBinArrMon.Name = "lblBinArrMon";
            this.lblBinArrMon.Size = new System.Drawing.Size(100, 23);
            this.lblBinArrMon.TabIndex = 0;
            // 
            // WorkMainPage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "WorkMainPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.grpVision.ResumeLayout(false);
            this.visionPanel.ResumeLayout(false);
            this.visionShellLayout.ResumeLayout(false);
            this.visionMainLayout.ResumeLayout(false);
            this.visionLeftLayout.ResumeLayout(false);
            this.pnlWaferVision.ResumeLayout(false);
            this.pnlWaferVision.PerformLayout();
            this.pnlBinVision.ResumeLayout(false);
            this.pnlBinVision.PerformLayout();
            this.visionSideLayout.ResumeLayout(false);
            this.pnlSideRearVision.ResumeLayout(false);
            this.pnlSideRearVision.PerformLayout();
            this.pnlBottomInspVision.ResumeLayout(false);
            this.pnlBottomInspVision.PerformLayout();
            this.pnlSideFrontVision.ResumeLayout(false);
            this.pnlSideFrontVision.PerformLayout();
            this.grpMap.ResumeLayout(false);
            this.mapBody.ResumeLayout(false);
            this.mapHeaderLayout.ResumeLayout(false);
            this.mapHeaderTotalTile.ResumeLayout(false);
            this.mapHeaderBinTile.ResumeLayout(false);
            this.mapTabControl.ResumeLayout(false);
            this.tabInputMap.ResumeLayout(false);
            this.tabOutputGoodMap.ResumeLayout(false);
            this.tabOutputNgMap.ResumeLayout(false);
            this.grpInfo.ResumeLayout(false);
            this.workInfoBody.ResumeLayout(false);
            this.lotInputPanel.ResumeLayout(false);
            this.lotInputPanel.PerformLayout();
            this.workInfoProjectTile.ResumeLayout(false);
            this.workInfoBinQtyTile.ResumeLayout(false);
            this.workInfoPickFailTile.ResumeLayout(false);
            this.workInfoPlaceFailTile.ResumeLayout(false);
            this.workInfoNeedleTile.ResumeLayout(false);
            this.workInfoFrontCollet1Tile.ResumeLayout(false);
            this.workInfoFrontCollet2Tile.ResumeLayout(false);
            this.workInfoFrontCollet3Tile.ResumeLayout(false);
            this.workInfoFrontCollet4Tile.ResumeLayout(false);
            this.workInfoRearCollet1Tile.ResumeLayout(false);
            this.workInfoRearCollet2Tile.ResumeLayout(false);
            this.workInfoRearCollet3Tile.ResumeLayout(false);
            this.workInfoRearCollet4Tile.ResumeLayout(false);
            this.grpTime.ResumeLayout(false);
            this.workTimeBody.ResumeLayout(false);
            this.workTimeLotTile.ResumeLayout(false);
            this.workTimeActionPanel.ResumeLayout(false);
            this.workTimeUphTile.ResumeLayout(false);
            this.workTimeUphValueLayout.ResumeLayout(false);
            this.workTimeUphHeaderLayout.ResumeLayout(false);
            this.workTimeUpTile.ResumeLayout(false);
            this.workTimeContUpTile.ResumeLayout(false);
            this.workTimeRateTile.ResumeLayout(false);
            this.workTimeCycleTile.ResumeLayout(false);
            this.workTimeLoadTile.ResumeLayout(false);
            this.workTimeMtbfTile.ResumeLayout(false);
            this.workTimeMttrTile.ResumeLayout(false);
            this.workTimeRecoveryTile.ResumeLayout(false);
            this.workTimeNormDownTile.ResumeLayout(false);
            this.workTimeErrDownTile.ResumeLayout(false);
            this.workTimeErrCntTile.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
