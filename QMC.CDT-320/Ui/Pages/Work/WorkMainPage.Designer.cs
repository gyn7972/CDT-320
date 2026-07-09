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
        private Label lblLive;
        private TabControl mapTabControl;
        private TabPage tabInputMap;
        private TabPage tabOutputGoodMap;
        private TabPage tabOutputNgMap;
        private LiveLotMapView lotMapView;
        private LiveLotMapView outputGoodLotMapView;
        private LiveLotMapView outputNgLotMapView;
        private TableLayoutPanel mapHeaderLayout;
        private Label lblTotalChipCaption;
        private Label lblTotalChip;
        private Label lblBinNumCaption;
        private Label lblBinNum;
        private Label lblVisionCaption;
        private Label lblPickCaption;
        private Label lblPlaceCaption;
        private TableLayoutPanel workInfoBody;
        private TableLayoutPanel workTimeBody;
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
        private Button btnTestAlarm;

        private static readonly Color AccentColor = Color.FromArgb(217, 119, 6);
        private static readonly Color TitleColor = Color.FromArgb(38, 50, 66);   // 다크 슬레이트 — 그룹 제목 가독성
        private static readonly Color CaptionBack = Color.FromArgb(236, 238, 241);
        private static readonly Color CaptionFore = Color.FromArgb(70, 70, 70);
        private static readonly Color ValueBack = Color.White;
        private static readonly Color ValueBorder = Color.FromArgb(214, 214, 214);
        private static readonly Color PanelBack = Color.White;
        private static readonly Color TileBack = Color.FromArgb(244, 246, 248);

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpVision = new System.Windows.Forms.GroupBox();
            this.grpMap = new System.Windows.Forms.GroupBox();
            this.grpInfo = new System.Windows.Forms.GroupBox();
            this.grpTime = new System.Windows.Forms.GroupBox();
            this.mapBody = new System.Windows.Forms.TableLayoutPanel();
            this.visionPanel = new System.Windows.Forms.Panel();
            this.lblStageInfo = new System.Windows.Forms.Label();
            this.lblLive = new System.Windows.Forms.Label();
            this.mapHeaderLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTotalChipCaption = new System.Windows.Forms.Label();
            this.lblTotalChip = new System.Windows.Forms.Label();
            this.lblBinNumCaption = new System.Windows.Forms.Label();
            this.lblBinNum = new System.Windows.Forms.Label();
            this.lblVisionCaption = new System.Windows.Forms.Label();
            this.lblPickCaption = new System.Windows.Forms.Label();
            this.lblPlaceCaption = new System.Windows.Forms.Label();
            this.mapTabControl = new System.Windows.Forms.TabControl();
            this.tabInputMap = new System.Windows.Forms.TabPage();
            this.tabOutputGoodMap = new System.Windows.Forms.TabPage();
            this.tabOutputNgMap = new System.Windows.Forms.TabPage();
            this.lotMapView = new QMC.CDT_320.Ui.Controls.LiveLotMapView();
            this.outputGoodLotMapView = new QMC.CDT_320.Ui.Controls.LiveLotMapView();
            this.outputNgLotMapView = new QMC.CDT_320.Ui.Controls.LiveLotMapView();
            this.workInfoBody = new System.Windows.Forms.TableLayoutPanel();
            this.lblProjectCaption = new System.Windows.Forms.Label();
            this.lblProject = new System.Windows.Forms.Label();
            this.lblPickFailCaption = new System.Windows.Forms.Label();
            this.lblPickFail = new System.Windows.Forms.Label();
            this.lblBinQtyCaption = new System.Windows.Forms.Label();
            this.lblBinQty = new System.Windows.Forms.Label();
            this.lblCollet1Caption = new System.Windows.Forms.Label();
            this.lblCollet1 = new System.Windows.Forms.Label();
            this.lblPlaceFailCaption = new System.Windows.Forms.Label();
            this.lblPlaceFail = new System.Windows.Forms.Label();
            this.lblNeedleCaption = new System.Windows.Forms.Label();
            this.lblNeedle = new System.Windows.Forms.Label();
            this.lblCollet2Caption = new System.Windows.Forms.Label();
            this.lblCollet2 = new System.Windows.Forms.Label();
            this.lblBinArrMonCaption = new System.Windows.Forms.Label();
            this.lblBinArrMon = new System.Windows.Forms.Label();
            this.workTimeBody = new System.Windows.Forms.TableLayoutPanel();
            this.lblLoadCaption = new System.Windows.Forms.Label();
            this.lblLoad = new System.Windows.Forms.Label();
            this.lblUpCaption = new System.Windows.Forms.Label();
            this.lblUp = new System.Windows.Forms.Label();
            this.lblContUpCaption = new System.Windows.Forms.Label();
            this.lblContUp = new System.Windows.Forms.Label();
            this.lblNormDownCaption = new System.Windows.Forms.Label();
            this.lblNormDown = new System.Windows.Forms.Label();
            this.lblErrDownCaption = new System.Windows.Forms.Label();
            this.lblErrDown = new System.Windows.Forms.Label();
            this.lblErrCntCaption = new System.Windows.Forms.Label();
            this.lblErrCnt = new System.Windows.Forms.Label();
            this.lblRecoveryCaption = new System.Windows.Forms.Label();
            this.lblRecovery = new System.Windows.Forms.Label();
            this.lblUphCaption = new System.Windows.Forms.Label();
            this.lblUph = new System.Windows.Forms.Label();
            this.lblMtbfCaption = new System.Windows.Forms.Label();
            this.lblMtbf = new System.Windows.Forms.Label();
            this.lblMttrCaption = new System.Windows.Forms.Label();
            this.lblMttr = new System.Windows.Forms.Label();
            this.lblCycleCaption = new System.Windows.Forms.Label();
            this.lblCycle = new System.Windows.Forms.Label();
            this.lblRateCaption = new System.Windows.Forms.Label();
            this.lblRate = new System.Windows.Forms.Label();
            this.lblLotCaption = new System.Windows.Forms.Label();
            this.lblLot = new System.Windows.Forms.Label();
            this.btnCcs = new System.Windows.Forms.Button();
            this.btnTestAlarm = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.grpVision.SuspendLayout();
            this.grpMap.SuspendLayout();
            this.grpInfo.SuspendLayout();
            this.grpTime.SuspendLayout();
            this.mapBody.SuspendLayout();
            this.visionPanel.SuspendLayout();
            this.mapHeaderLayout.SuspendLayout();
            this.mapTabControl.SuspendLayout();
            this.tabInputMap.SuspendLayout();
            this.tabOutputGoodMap.SuspendLayout();
            this.tabOutputNgMap.SuspendLayout();
            this.workInfoBody.SuspendLayout();
            this.workTimeBody.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout  (2x2, 50:50 / 65:35, 흰 배경)
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
            this.grpVision.ForeColor = TitleColor;
            this.grpVision.Margin = new System.Windows.Forms.Padding(4);
            this.grpVision.Name = "grpVision";
            this.grpVision.Padding = new System.Windows.Forms.Padding(6, 4, 6, 6);
            this.grpVision.TabIndex = 0;
            this.grpVision.TabStop = false;
            this.grpVision.Tag = "i18n:work.sec.visionView";
            this.grpVision.Text = "비전 화면";
            //
            // visionPanel
            //
            this.visionPanel.BackColor = System.Drawing.Color.Black;
            this.visionPanel.Controls.Add(this.lblStageInfo);
            this.visionPanel.Controls.Add(this.lblLive);
            this.visionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.visionPanel.Name = "visionPanel";
            this.visionPanel.TabIndex = 1;
            //
            // lblStageInfo
            //
            this.lblStageInfo.AutoSize = true;
            this.lblStageInfo.BackColor = System.Drawing.Color.Black;
            this.lblStageInfo.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblStageInfo.ForeColor = System.Drawing.Color.LightGreen;
            this.lblStageInfo.Location = new System.Drawing.Point(8, 8);
            this.lblStageInfo.Name = "lblStageInfo";
            this.lblStageInfo.Size = new System.Drawing.Size(70, 56);
            this.lblStageInfo.TabIndex = 0;
            this.lblStageInfo.Text = "STAGE\r\nW : 640\r\nH : 480\r\nframe : 0";
            //
            // lblLive
            //
            this.lblLive.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(20)))), ((int)(((byte)(20)))));
            this.lblLive.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblLive.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblLive.ForeColor = System.Drawing.Color.LightGreen;
            this.lblLive.Name = "lblLive";
            this.lblLive.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblLive.Size = new System.Drawing.Size(825, 20);
            this.lblLive.TabIndex = 1;
            this.lblLive.Text = "Live";
            this.lblLive.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // grpMap
            //
            this.grpMap.BackColor = System.Drawing.Color.White;
            this.grpMap.Controls.Add(this.mapBody);
            this.grpMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMap.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpMap.ForeColor = TitleColor;
            this.grpMap.Margin = new System.Windows.Forms.Padding(4);
            this.grpMap.Name = "grpMap";
            this.grpMap.Padding = new System.Windows.Forms.Padding(6, 4, 6, 6);
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
            this.mapBody.Margin = new System.Windows.Forms.Padding(0);
            this.mapBody.Name = "mapBody";
            this.mapBody.RowCount = 2;
            this.mapBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.mapBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapBody.TabIndex = 0;
            //
            // mapHeaderLayout
            //
            this.mapHeaderLayout.BackColor = CaptionBack;
            this.mapHeaderLayout.ColumnCount = 8;
            this.mapHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.mapHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.mapHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.mapHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.mapHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.mapHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.mapHeaderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.mapHeaderLayout.Controls.Add(this.lblTotalChipCaption, 0, 0);
            this.mapHeaderLayout.Controls.Add(this.lblTotalChip, 1, 0);
            this.mapHeaderLayout.Controls.Add(this.lblBinNumCaption, 2, 0);
            this.mapHeaderLayout.Controls.Add(this.lblBinNum, 3, 0);
            this.mapHeaderLayout.Controls.Add(this.lblVisionCaption, 5, 0);
            this.mapHeaderLayout.Controls.Add(this.lblPickCaption, 6, 0);
            this.mapHeaderLayout.Controls.Add(this.lblPlaceCaption, 7, 0);
            this.mapHeaderLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapHeaderLayout.Margin = new System.Windows.Forms.Padding(0);
            this.mapHeaderLayout.Name = "mapHeaderLayout";
            this.mapHeaderLayout.RowCount = 1;
            this.mapHeaderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapHeaderLayout.TabIndex = 0;
            //
            // lblTotalChipCaption
            //
            this.lblTotalChipCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalChipCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblTotalChipCaption.ForeColor = CaptionFore;
            this.lblTotalChipCaption.Name = "lblTotalChipCaption";
            this.lblTotalChipCaption.Size = new System.Drawing.Size(84, 26);
            this.lblTotalChipCaption.TabIndex = 0;
            this.lblTotalChipCaption.Text = "Total Chip :";
            this.lblTotalChipCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblTotalChip
            //
            this.lblTotalChip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalChip.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold);
            this.lblTotalChip.ForeColor = TitleColor;
            this.lblTotalChip.Name = "lblTotalChip";
            this.lblTotalChip.Size = new System.Drawing.Size(74, 26);
            this.lblTotalChip.TabIndex = 1;
            this.lblTotalChip.Text = "0";
            this.lblTotalChip.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblBinNumCaption
            //
            this.lblBinNumCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinNumCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblBinNumCaption.ForeColor = CaptionFore;
            this.lblBinNumCaption.Name = "lblBinNumCaption";
            this.lblBinNumCaption.Size = new System.Drawing.Size(54, 26);
            this.lblBinNumCaption.TabIndex = 2;
            this.lblBinNumCaption.Text = "Bin # :";
            this.lblBinNumCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblBinNum
            //
            this.lblBinNum.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinNum.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold);
            this.lblBinNum.ForeColor = TitleColor;
            this.lblBinNum.Name = "lblBinNum";
            this.lblBinNum.Size = new System.Drawing.Size(74, 26);
            this.lblBinNum.TabIndex = 3;
            this.lblBinNum.Text = "--";
            this.lblBinNum.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblVisionCaption
            //
            this.lblVisionCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblVisionCaption.ForeColor = CaptionFore;
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
            this.lblPickCaption.ForeColor = CaptionFore;
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
            this.lblPlaceCaption.ForeColor = CaptionFore;
            this.lblPlaceCaption.Name = "lblPlaceCaption";
            this.lblPlaceCaption.Size = new System.Drawing.Size(64, 26);
            this.lblPlaceCaption.TabIndex = 6;
            this.lblPlaceCaption.Text = "PLACE";
            this.lblPlaceCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // mapTabControl
            //
            this.mapTabControl.Controls.Add(this.tabInputMap);
            this.mapTabControl.Controls.Add(this.tabOutputGoodMap);
            this.mapTabControl.Controls.Add(this.tabOutputNgMap);
            this.mapTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapTabControl.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this.mapTabControl.ItemSize = new System.Drawing.Size(145, 21);
            this.mapTabControl.Margin = new System.Windows.Forms.Padding(0);
            this.mapTabControl.Name = "mapTabControl";
            this.mapTabControl.Padding = new System.Drawing.Point(6, 1);
            this.mapTabControl.SelectedIndex = 0;
            this.mapTabControl.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.mapTabControl.TabIndex = 1;
            //
            // tabInputMap
            //
            this.tabInputMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.tabInputMap.Controls.Add(this.lotMapView);
            this.tabInputMap.Margin = new System.Windows.Forms.Padding(0);
            this.tabInputMap.Name = "tabInputMap";
            this.tabInputMap.Text = "INPUT MAP";
            //
            // tabOutputGoodMap
            //
            this.tabOutputGoodMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.tabOutputGoodMap.Controls.Add(this.outputGoodLotMapView);
            this.tabOutputGoodMap.Margin = new System.Windows.Forms.Padding(0);
            this.tabOutputGoodMap.Name = "tabOutputGoodMap";
            this.tabOutputGoodMap.Text = "OUTPUT GOOD";
            //
            // tabOutputNgMap
            //
            this.tabOutputNgMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.tabOutputNgMap.Controls.Add(this.outputNgLotMapView);
            this.tabOutputNgMap.Margin = new System.Windows.Forms.Padding(0);
            this.tabOutputNgMap.Name = "tabOutputNgMap";
            this.tabOutputNgMap.Text = "OUTPUT NG";
            //
            // lotMapView
            //
            this.lotMapView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.lotMapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lotMapView.GridX = 5;
            this.lotMapView.GridY = 5;
            this.lotMapView.Margin = new System.Windows.Forms.Padding(0);
            this.lotMapView.Name = "lotMapView";
            this.lotMapView.SourceKind = QMC.CDT_320.Ui.Controls.LiveLotMapSourceKind.Input;
            this.lotMapView.TabIndex = 0;
            //
            // outputGoodLotMapView
            //
            this.outputGoodLotMapView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.outputGoodLotMapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputGoodLotMapView.GridX = 5;
            this.outputGoodLotMapView.GridY = 5;
            this.outputGoodLotMapView.Margin = new System.Windows.Forms.Padding(0);
            this.outputGoodLotMapView.Name = "outputGoodLotMapView";
            this.outputGoodLotMapView.SourceKind = QMC.CDT_320.Ui.Controls.LiveLotMapSourceKind.OutputGood;
            this.outputGoodLotMapView.TabIndex = 0;
            //
            // outputNgLotMapView
            //
            this.outputNgLotMapView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.outputNgLotMapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputNgLotMapView.GridX = 5;
            this.outputNgLotMapView.GridY = 5;
            this.outputNgLotMapView.Margin = new System.Windows.Forms.Padding(0);
            this.outputNgLotMapView.Name = "outputNgLotMapView";
            this.outputNgLotMapView.SourceKind = QMC.CDT_320.Ui.Controls.LiveLotMapSourceKind.OutputNg;
            this.outputNgLotMapView.TabIndex = 0;
            //
            // grpInfo
            //
            this.grpInfo.BackColor = System.Drawing.Color.White;
            this.grpInfo.Controls.Add(this.workInfoBody);
            this.grpInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInfo.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpInfo.ForeColor = TitleColor;
            this.grpInfo.Margin = new System.Windows.Forms.Padding(4);
            this.grpInfo.Name = "grpInfo";
            this.grpInfo.Padding = new System.Windows.Forms.Padding(6, 4, 6, 6);
            this.grpInfo.TabIndex = 2;
            this.grpInfo.TabStop = false;
            this.grpInfo.Tag = "i18n:work.sec.workInfo";
            this.grpInfo.Text = "작업 정보";
            //
            // workInfoBody  (내용은 .cs RebuildWorkInfoPanel 에서 재구성)
            //
            this.workInfoBody.BackColor = System.Drawing.Color.White;
            this.workInfoBody.ColumnCount = 4;
            this.workInfoBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workInfoBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workInfoBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workInfoBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workInfoBody.Controls.Add(this.lblProjectCaption, 0, 0);
            this.workInfoBody.Controls.Add(this.lblProject, 1, 0);
            this.workInfoBody.Controls.Add(this.lblBinQtyCaption, 2, 0);
            this.workInfoBody.Controls.Add(this.lblBinQty, 3, 0);
            this.workInfoBody.Controls.Add(this.lblPickFailCaption, 0, 1);
            this.workInfoBody.Controls.Add(this.lblPickFail, 1, 1);
            this.workInfoBody.Controls.Add(this.lblPlaceFailCaption, 2, 1);
            this.workInfoBody.Controls.Add(this.lblPlaceFail, 3, 1);
            this.workInfoBody.Controls.Add(this.lblCollet1Caption, 0, 2);
            this.workInfoBody.Controls.Add(this.lblCollet1, 1, 2);
            this.workInfoBody.Controls.Add(this.lblCollet2Caption, 2, 2);
            this.workInfoBody.Controls.Add(this.lblCollet2, 3, 2);
            this.workInfoBody.Controls.Add(this.lblNeedleCaption, 0, 3);
            this.workInfoBody.Controls.Add(this.lblNeedle, 1, 3);
            this.workInfoBody.Controls.Add(this.lblBinArrMonCaption, 2, 3);
            this.workInfoBody.Controls.Add(this.lblBinArrMon, 3, 3);
            this.workInfoBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoBody.Margin = new System.Windows.Forms.Padding(0);
            this.workInfoBody.Name = "workInfoBody";
            this.workInfoBody.Padding = new System.Windows.Forms.Padding(2);
            this.workInfoBody.RowCount = 4;
            this.workInfoBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workInfoBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workInfoBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workInfoBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.workInfoBody.TabIndex = 0;
            //
            // grpTime
            //
            this.grpTime.BackColor = System.Drawing.Color.White;
            this.grpTime.Controls.Add(this.workTimeBody);
            this.grpTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpTime.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpTime.ForeColor = TitleColor;
            this.grpTime.Margin = new System.Windows.Forms.Padding(4);
            this.grpTime.Name = "grpTime";
            this.grpTime.Padding = new System.Windows.Forms.Padding(6, 4, 6, 6);
            this.grpTime.TabIndex = 3;
            this.grpTime.TabStop = false;
            this.grpTime.Tag = "i18n:work.sec.workTime";
            this.grpTime.Text = "작업 시간";
            //
            // workTimeBody  (내용은 .cs RebuildWorkTimePanel 에서 타일로 재구성)
            //
            this.workTimeBody.BackColor = System.Drawing.Color.White;
            this.workTimeBody.ColumnCount = 1;
            this.workTimeBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.workTimeBody.Controls.Add(this.btnCcs, 0, 0);
            this.workTimeBody.Controls.Add(this.btnTestAlarm, 0, 1);
            this.workTimeBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeBody.Margin = new System.Windows.Forms.Padding(0);
            this.workTimeBody.Name = "workTimeBody";
            this.workTimeBody.Padding = new System.Windows.Forms.Padding(0);
            this.workTimeBody.RowCount = 2;
            this.workTimeBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.workTimeBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 0F));
            this.workTimeBody.TabIndex = 0;
            //
            // metric labels (부모/셀은 .cs RebuildWorkTimePanel 에서 배치)
            //
            this.lblLoadCaption.Name = "lblLoadCaption";
            this.lblLoadCaption.Tag = "i18n:work.workTime.load";
            this.lblLoadCaption.Text = "부하 시간";
            this.lblLoad.Name = "lblLoad";
            this.lblLoad.Text = "00:00:00";
            this.lblUpCaption.Name = "lblUpCaption";
            this.lblUpCaption.Tag = "i18n:work.workTime.up";
            this.lblUpCaption.Text = "가동 시간";
            this.lblUp.Name = "lblUp";
            this.lblUp.Text = "00:00:00";
            this.lblContUpCaption.Name = "lblContUpCaption";
            this.lblContUpCaption.Tag = "i18n:work.workTime.contUp";
            this.lblContUpCaption.Text = "연속 가동 시간";
            this.lblContUp.Name = "lblContUp";
            this.lblContUp.Text = "00:00:00";
            this.lblNormDownCaption.Name = "lblNormDownCaption";
            this.lblNormDownCaption.Tag = "i18n:work.workTime.normDown";
            this.lblNormDownCaption.Text = "통상 정지 시간";
            this.lblNormDown.Name = "lblNormDown";
            this.lblNormDown.Text = "00:00:00";
            this.lblErrDownCaption.Name = "lblErrDownCaption";
            this.lblErrDownCaption.Tag = "i18n:work.workTime.errDown";
            this.lblErrDownCaption.Text = "이상 정지 시간";
            this.lblErrDown.Name = "lblErrDown";
            this.lblErrDown.Text = "00:00:00";
            this.lblErrCntCaption.Name = "lblErrCntCaption";
            this.lblErrCntCaption.Tag = "i18n:work.workTime.errCnt";
            this.lblErrCntCaption.Text = "이상 정지 횟수";
            this.lblErrCnt.Name = "lblErrCnt";
            this.lblErrCnt.Text = "0 ea";
            this.lblRecoveryCaption.Name = "lblRecoveryCaption";
            this.lblRecoveryCaption.Tag = "i18n:work.workTime.recovery";
            this.lblRecoveryCaption.Text = "이상 복귀 시간";
            this.lblRecovery.Name = "lblRecovery";
            this.lblRecovery.Text = "00:00:00";
            this.lblUphCaption.Name = "lblUphCaption";
            this.lblUphCaption.Tag = "i18n:work.workTime.uph";
            this.lblUphCaption.Text = "UPH";
            this.lblUph.Name = "lblUph";
            this.lblUph.Text = "0.00";
            this.lblMtbfCaption.Name = "lblMtbfCaption";
            this.lblMtbfCaption.Tag = "i18n:work.workTime.mtbf";
            this.lblMtbfCaption.Text = "MTBF";
            this.lblMtbf.Name = "lblMtbf";
            this.lblMtbf.Text = "00:00:00";
            this.lblMttrCaption.Name = "lblMttrCaption";
            this.lblMttrCaption.Tag = "i18n:work.workTime.mttr";
            this.lblMttrCaption.Text = "MTTR";
            this.lblMttr.Name = "lblMttr";
            this.lblMttr.Text = "00:00:00";
            this.lblCycleCaption.Name = "lblCycleCaption";
            this.lblCycleCaption.Tag = "i18n:work.workTime.cycle";
            this.lblCycleCaption.Text = "CYCLE TIME";
            this.lblCycle.Name = "lblCycle";
            this.lblCycle.Text = "0 ms";
            this.lblRateCaption.Name = "lblRateCaption";
            this.lblRateCaption.Tag = "i18n:work.workTime.rate";
            this.lblRateCaption.Text = "가동률";
            this.lblRate.Name = "lblRate";
            this.lblRate.Text = "0.00 %";
            this.lblLotCaption.Name = "lblLotCaption";
            this.lblLotCaption.Tag = "i18n:work.workTime.lotId";
            this.lblLotCaption.Text = "작업중인 LOT ID";
            this.lblLot.Name = "lblLot";
            this.lblLot.Text = "(no lot)";
            //
            // 작업 정보 라벨 (텍스트/Tag만; 스타일·배치는 .cs)
            //
            this.lblProjectCaption.Name = "lblProjectCaption";
            this.lblProjectCaption.Tag = "i18n:work.workInfo.project";
            this.lblProjectCaption.Text = "프로젝트 이름";
            this.lblProject.Name = "lblProject";
            this.lblProject.Text = "--";
            this.lblPickFailCaption.Name = "lblPickFailCaption";
            this.lblPickFailCaption.Tag = "i18n:work.workInfo.pickFail";
            this.lblPickFailCaption.Text = "PICK 실패 수량";
            this.lblPickFail.Name = "lblPickFail";
            this.lblPickFail.Text = "0 ea";
            this.lblBinQtyCaption.Name = "lblBinQtyCaption";
            this.lblBinQtyCaption.Tag = "i18n:work.workInfo.workBinQty";
            this.lblBinQtyCaption.Text = "작업 BIN 수량";
            this.lblBinQty.Name = "lblBinQty";
            this.lblBinQty.Text = "0 ea";
            this.lblCollet1Caption.Name = "lblCollet1Caption";
            this.lblCollet1Caption.Tag = "i18n:work.workInfo.collet1Use";
            this.lblCollet1Caption.Text = "# 1 Collet 사용";
            this.lblCollet1.Name = "lblCollet1";
            this.lblCollet1.Text = "0";
            this.lblPlaceFailCaption.Name = "lblPlaceFailCaption";
            this.lblPlaceFailCaption.Tag = "i18n:work.workInfo.placeFail";
            this.lblPlaceFailCaption.Text = "PLACE 실패 수량";
            this.lblPlaceFail.Name = "lblPlaceFail";
            this.lblPlaceFail.Text = "0 ea";
            this.lblNeedleCaption.Name = "lblNeedleCaption";
            this.lblNeedleCaption.Tag = "i18n:work.workInfo.needleUse";
            this.lblNeedleCaption.Text = "NEEDLE 사용 횟수";
            this.lblNeedle.Name = "lblNeedle";
            this.lblNeedle.Text = "0";
            this.lblCollet2Caption.Name = "lblCollet2Caption";
            this.lblCollet2Caption.Tag = "i18n:work.workInfo.collet2Use";
            this.lblCollet2Caption.Text = "# 2 Collet 사용";
            this.lblCollet2.Name = "lblCollet2";
            this.lblCollet2.Text = "0";
            this.lblBinArrMonCaption.Name = "lblBinArrMonCaption";
            this.lblBinArrMonCaption.Tag = "i18n:work.workInfo.binArrMon";
            this.lblBinArrMonCaption.Text = "빈 배열 모니터링";
            this.lblBinArrMon.Name = "lblBinArrMon";
            //
            // btnCcs
            //
            this.btnCcs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(89)))), ((int)(((byte)(89)))), ((int)(((byte)(89)))));
            this.btnCcs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCcs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCcs.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnCcs.ForeColor = System.Drawing.Color.White;
            this.btnCcs.Margin = new System.Windows.Forms.Padding(2, 4, 2, 2);
            this.btnCcs.Name = "btnCcs";
            this.btnCcs.TabIndex = 26;
            this.btnCcs.Tag = "i18n:work.workTime.ccs";
            this.btnCcs.Text = "CCS 검수 확인";
            this.btnCcs.UseVisualStyleBackColor = false;
            //
            // btnTestAlarm
            //
            this.btnTestAlarm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(183)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.btnTestAlarm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTestAlarm.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnTestAlarm.ForeColor = System.Drawing.Color.White;
            this.btnTestAlarm.Name = "btnTestAlarm";
            this.btnTestAlarm.Size = new System.Drawing.Size(122, 1);
            this.btnTestAlarm.TabIndex = 27;
            this.btnTestAlarm.Text = "TEST ALARM";
            this.btnTestAlarm.UseVisualStyleBackColor = false;
            this.btnTestAlarm.Visible = false;
            //
            // WorkMainPage
            //
            this.Controls.Add(this.rootLayout);
            this.Name = "WorkMainPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.grpVision.ResumeLayout(false);
            this.grpMap.ResumeLayout(false);
            this.grpInfo.ResumeLayout(false);
            this.grpTime.ResumeLayout(false);
            this.mapBody.ResumeLayout(false);
            this.visionPanel.ResumeLayout(false);
            this.visionPanel.PerformLayout();
            this.mapHeaderLayout.ResumeLayout(false);
            this.mapTabControl.ResumeLayout(false);
            this.tabInputMap.ResumeLayout(false);
            this.tabOutputGoodMap.ResumeLayout(false);
            this.tabOutputNgMap.ResumeLayout(false);
            this.workInfoBody.ResumeLayout(false);
            this.workTimeBody.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
