using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Work
{
    partial class WorkMainPage
    {
        private TableLayoutPanel rootLayout;
        private TableLayoutPanel visionLayout;
        private TableLayoutPanel mapLayout;
        private TableLayoutPanel infoLayout;
        private TableLayoutPanel timeLayout;
        private Label lblVisionHeader;
        private Label lblMapHeader;
        private Label lblInfoHeader;
        private Label lblTimeHeader;
        private Panel visionPanel;
        private LiveLotMapView lotMapView;
        private TableLayoutPanel mapHeaderLayout;
        private Label lblTotalChipCaption;
        private Label lblTotalChip;
        private Label lblBinNumCaption;
        private Label lblBinNum;
        private Label lblVisionCaption;
        private Label lblPickCaption;
        private Label lblPlaceCaption;
        private Label lblStageInfo;
        private Label lblLive;
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

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.visionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblVisionHeader = new System.Windows.Forms.Label();
            this.visionPanel = new System.Windows.Forms.Panel();
            this.lblStageInfo = new System.Windows.Forms.Label();
            this.lblLive = new System.Windows.Forms.Label();
            this.mapLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblMapHeader = new System.Windows.Forms.Label();
            this.mapHeaderLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTotalChipCaption = new System.Windows.Forms.Label();
            this.lblTotalChip = new System.Windows.Forms.Label();
            this.lblBinNumCaption = new System.Windows.Forms.Label();
            this.lblBinNum = new System.Windows.Forms.Label();
            this.lblVisionCaption = new System.Windows.Forms.Label();
            this.lblPickCaption = new System.Windows.Forms.Label();
            this.lblPlaceCaption = new System.Windows.Forms.Label();
            this.lotMapView = new QMC.CDT_320.Ui.Controls.LiveLotMapView();
            this.infoLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblInfoHeader = new System.Windows.Forms.Label();
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
            this.timeLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTimeHeader = new System.Windows.Forms.Label();
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
            this.visionLayout.SuspendLayout();
            this.visionPanel.SuspendLayout();
            this.mapLayout.SuspendLayout();
            this.mapHeaderLayout.SuspendLayout();
            this.infoLayout.SuspendLayout();
            this.workInfoBody.SuspendLayout();
            this.timeLayout.SuspendLayout();
            this.workTimeBody.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.rootLayout.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.rootLayout.ColumnCount = 2;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.rootLayout.Controls.Add(this.visionLayout, 0, 0);
            this.rootLayout.Controls.Add(this.mapLayout, 1, 0);
            this.rootLayout.Controls.Add(this.infoLayout, 0, 1);
            this.rootLayout.Controls.Add(this.timeLayout, 1, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // visionLayout
            // 
            this.visionLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.visionLayout.ColumnCount = 1;
            this.visionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.visionLayout.Controls.Add(this.lblVisionHeader, 0, 0);
            this.visionLayout.Controls.Add(this.visionPanel, 0, 1);
            this.visionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.visionLayout.Location = new System.Drawing.Point(4, 4);
            this.visionLayout.Name = "visionLayout";
            this.visionLayout.RowCount = 2;
            this.visionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.visionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.visionLayout.Size = new System.Drawing.Size(831, 487);
            this.visionLayout.TabIndex = 0;
            // 
            // lblVisionHeader
            // 
            this.lblVisionHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblVisionHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblVisionHeader.ForeColor = System.Drawing.Color.White;
            this.lblVisionHeader.Location = new System.Drawing.Point(3, 0);
            this.lblVisionHeader.Name = "lblVisionHeader";
            this.lblVisionHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblVisionHeader.Size = new System.Drawing.Size(825, 30);
            this.lblVisionHeader.TabIndex = 0;
            this.lblVisionHeader.Tag = "i18n:work.sec.visionView";
            this.lblVisionHeader.Text = "비전 화면";
            this.lblVisionHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // visionPanel
            // 
            this.visionPanel.BackColor = System.Drawing.Color.Black;
            this.visionPanel.Controls.Add(this.lblStageInfo);
            this.visionPanel.Controls.Add(this.lblLive);
            this.visionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.visionPanel.Location = new System.Drawing.Point(3, 33);
            this.visionPanel.Name = "visionPanel";
            this.visionPanel.Size = new System.Drawing.Size(825, 451);
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
            this.lblLive.BackColor = System.Drawing.Color.Black;
            this.lblLive.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblLive.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblLive.ForeColor = System.Drawing.Color.LightGreen;
            this.lblLive.Location = new System.Drawing.Point(0, 433);
            this.lblLive.Name = "lblLive";
            this.lblLive.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblLive.Size = new System.Drawing.Size(825, 18);
            this.lblLive.TabIndex = 1;
            this.lblLive.Text = "Live";
            this.lblLive.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mapLayout
            // 
            this.mapLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.mapLayout.ColumnCount = 1;
            this.mapLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapLayout.Controls.Add(this.lblMapHeader, 0, 0);
            this.mapLayout.Controls.Add(this.mapHeaderLayout, 0, 1);
            this.mapLayout.Controls.Add(this.lotMapView, 0, 2);
            this.mapLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapLayout.Location = new System.Drawing.Point(842, 4);
            this.mapLayout.Name = "mapLayout";
            this.mapLayout.RowCount = 3;
            this.mapLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.mapLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.mapLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapLayout.Size = new System.Drawing.Size(832, 487);
            this.mapLayout.TabIndex = 1;
            // 
            // lblMapHeader
            // 
            this.lblMapHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblMapHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMapHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblMapHeader.ForeColor = System.Drawing.Color.White;
            this.lblMapHeader.Location = new System.Drawing.Point(3, 0);
            this.lblMapHeader.Name = "lblMapHeader";
            this.lblMapHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblMapHeader.Size = new System.Drawing.Size(826, 30);
            this.lblMapHeader.TabIndex = 0;
            this.lblMapHeader.Tag = "i18n:work.sec.workMap";
            this.lblMapHeader.Text = "작업 맵";
            this.lblMapHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mapHeaderLayout
            // 
            this.mapHeaderLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
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
            this.mapHeaderLayout.Location = new System.Drawing.Point(3, 33);
            this.mapHeaderLayout.Name = "mapHeaderLayout";
            this.mapHeaderLayout.RowCount = 1;
            this.mapHeaderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapHeaderLayout.Size = new System.Drawing.Size(826, 18);
            this.mapHeaderLayout.TabIndex = 1;
            // 
            // lblTotalChipCaption
            // 
            this.lblTotalChipCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalChipCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblTotalChipCaption.ForeColor = System.Drawing.Color.White;
            this.lblTotalChipCaption.Location = new System.Drawing.Point(3, 0);
            this.lblTotalChipCaption.Name = "lblTotalChipCaption";
            this.lblTotalChipCaption.Size = new System.Drawing.Size(84, 18);
            this.lblTotalChipCaption.TabIndex = 0;
            this.lblTotalChipCaption.Text = "Total Chip :";
            this.lblTotalChipCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblTotalChip
            // 
            this.lblTotalChip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTotalChip.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold);
            this.lblTotalChip.ForeColor = System.Drawing.Color.White;
            this.lblTotalChip.Location = new System.Drawing.Point(93, 0);
            this.lblTotalChip.Name = "lblTotalChip";
            this.lblTotalChip.Size = new System.Drawing.Size(74, 18);
            this.lblTotalChip.TabIndex = 1;
            this.lblTotalChip.Text = "0";
            this.lblTotalChip.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblBinNumCaption
            // 
            this.lblBinNumCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinNumCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblBinNumCaption.ForeColor = System.Drawing.Color.White;
            this.lblBinNumCaption.Location = new System.Drawing.Point(173, 0);
            this.lblBinNumCaption.Name = "lblBinNumCaption";
            this.lblBinNumCaption.Size = new System.Drawing.Size(54, 18);
            this.lblBinNumCaption.TabIndex = 2;
            this.lblBinNumCaption.Text = "Bin # :";
            this.lblBinNumCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblBinNum
            // 
            this.lblBinNum.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinNum.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold);
            this.lblBinNum.ForeColor = System.Drawing.Color.White;
            this.lblBinNum.Location = new System.Drawing.Point(233, 0);
            this.lblBinNum.Name = "lblBinNum";
            this.lblBinNum.Size = new System.Drawing.Size(74, 18);
            this.lblBinNum.TabIndex = 3;
            this.lblBinNum.Text = "--";
            this.lblBinNum.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVisionCaption
            // 
            this.lblVisionCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblVisionCaption.ForeColor = System.Drawing.Color.White;
            this.lblVisionCaption.Location = new System.Drawing.Point(609, 0);
            this.lblVisionCaption.Name = "lblVisionCaption";
            this.lblVisionCaption.Size = new System.Drawing.Size(74, 18);
            this.lblVisionCaption.TabIndex = 4;
            this.lblVisionCaption.Text = "VISION";
            this.lblVisionCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPickCaption
            // 
            this.lblPickCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblPickCaption.ForeColor = System.Drawing.Color.White;
            this.lblPickCaption.Location = new System.Drawing.Point(689, 0);
            this.lblPickCaption.Name = "lblPickCaption";
            this.lblPickCaption.Size = new System.Drawing.Size(64, 18);
            this.lblPickCaption.TabIndex = 5;
            this.lblPickCaption.Text = "PICK";
            this.lblPickCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPlaceCaption
            // 
            this.lblPlaceCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlaceCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblPlaceCaption.ForeColor = System.Drawing.Color.White;
            this.lblPlaceCaption.Location = new System.Drawing.Point(759, 0);
            this.lblPlaceCaption.Name = "lblPlaceCaption";
            this.lblPlaceCaption.Size = new System.Drawing.Size(64, 18);
            this.lblPlaceCaption.TabIndex = 6;
            this.lblPlaceCaption.Text = "PLACE";
            this.lblPlaceCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lotMapView
            // 
            this.lotMapView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.lotMapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lotMapView.GridX = 5;
            this.lotMapView.GridY = 5;
            this.lotMapView.Location = new System.Drawing.Point(3, 57);
            this.lotMapView.Name = "lotMapView";
            this.lotMapView.Size = new System.Drawing.Size(826, 427);
            this.lotMapView.TabIndex = 2;
            // 
            // infoLayout
            // 
            this.infoLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.infoLayout.ColumnCount = 1;
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.infoLayout.Controls.Add(this.lblInfoHeader, 0, 0);
            this.infoLayout.Controls.Add(this.workInfoBody, 0, 1);
            this.infoLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoLayout.Location = new System.Drawing.Point(4, 498);
            this.infoLayout.Name = "infoLayout";
            this.infoLayout.RowCount = 2;
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.infoLayout.Size = new System.Drawing.Size(831, 398);
            this.infoLayout.TabIndex = 2;
            // 
            // lblInfoHeader
            // 
            this.lblInfoHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblInfoHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInfoHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblInfoHeader.ForeColor = System.Drawing.Color.White;
            this.lblInfoHeader.Location = new System.Drawing.Point(3, 0);
            this.lblInfoHeader.Name = "lblInfoHeader";
            this.lblInfoHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblInfoHeader.Size = new System.Drawing.Size(825, 30);
            this.lblInfoHeader.TabIndex = 0;
            this.lblInfoHeader.Tag = "i18n:work.sec.workInfo";
            this.lblInfoHeader.Text = "작업 정보";
            this.lblInfoHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workInfoBody
            // 
            this.workInfoBody.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.workInfoBody.ColumnCount = 4;
            this.workInfoBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workInfoBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workInfoBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workInfoBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workInfoBody.Controls.Add(this.lblProjectCaption, 0, 0);
            this.workInfoBody.Controls.Add(this.lblProject, 1, 0);
            this.workInfoBody.Controls.Add(this.lblPickFailCaption, 0, 1);
            this.workInfoBody.Controls.Add(this.lblPickFail, 1, 1);
            this.workInfoBody.Controls.Add(this.lblBinQtyCaption, 2, 1);
            this.workInfoBody.Controls.Add(this.lblBinQty, 3, 1);
            this.workInfoBody.Controls.Add(this.lblCollet1Caption, 0, 2);
            this.workInfoBody.Controls.Add(this.lblCollet1, 1, 2);
            this.workInfoBody.Controls.Add(this.lblPlaceFailCaption, 2, 2);
            this.workInfoBody.Controls.Add(this.lblPlaceFail, 3, 2);
            this.workInfoBody.Controls.Add(this.lblNeedleCaption, 0, 3);
            this.workInfoBody.Controls.Add(this.lblNeedle, 1, 3);
            this.workInfoBody.Controls.Add(this.lblCollet2Caption, 2, 3);
            this.workInfoBody.Controls.Add(this.lblCollet2, 3, 3);
            this.workInfoBody.Controls.Add(this.lblBinArrMonCaption, 0, 4);
            this.workInfoBody.Controls.Add(this.lblBinArrMon, 1, 4);
            this.workInfoBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workInfoBody.Location = new System.Drawing.Point(3, 33);
            this.workInfoBody.Name = "workInfoBody";
            this.workInfoBody.Padding = new System.Windows.Forms.Padding(6);
            this.workInfoBody.RowCount = 5;
            this.workInfoBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workInfoBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workInfoBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workInfoBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workInfoBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workInfoBody.Size = new System.Drawing.Size(825, 362);
            this.workInfoBody.TabIndex = 1;
            // 
            // lblProjectCaption
            // 
            this.lblProjectCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblProjectCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProjectCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblProjectCaption.Location = new System.Drawing.Point(7, 7);
            this.lblProjectCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblProjectCaption.Name = "lblProjectCaption";
            this.lblProjectCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblProjectCaption.Size = new System.Drawing.Size(201, 28);
            this.lblProjectCaption.TabIndex = 0;
            this.lblProjectCaption.Tag = "i18n:work.workInfo.project";
            this.lblProjectCaption.Text = "프로젝트 이름";
            this.lblProjectCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblProject
            // 
            this.lblProject.BackColor = System.Drawing.Color.White;
            this.lblProject.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProject.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblProject.Location = new System.Drawing.Point(210, 7);
            this.lblProject.Margin = new System.Windows.Forms.Padding(1);
            this.lblProject.Name = "lblProject";
            this.lblProject.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblProject.Size = new System.Drawing.Size(201, 28);
            this.lblProject.TabIndex = 1;
            this.lblProject.Text = "--";
            this.lblProject.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPickFailCaption
            // 
            this.lblPickFailCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblPickFailCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickFailCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblPickFailCaption.Location = new System.Drawing.Point(7, 37);
            this.lblPickFailCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblPickFailCaption.Name = "lblPickFailCaption";
            this.lblPickFailCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblPickFailCaption.Size = new System.Drawing.Size(201, 28);
            this.lblPickFailCaption.TabIndex = 2;
            this.lblPickFailCaption.Tag = "i18n:work.workInfo.pickFail";
            this.lblPickFailCaption.Text = "PICK 실패 수량";
            this.lblPickFailCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPickFail
            // 
            this.lblPickFail.BackColor = System.Drawing.Color.White;
            this.lblPickFail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickFail.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblPickFail.Location = new System.Drawing.Point(210, 37);
            this.lblPickFail.Margin = new System.Windows.Forms.Padding(1);
            this.lblPickFail.Name = "lblPickFail";
            this.lblPickFail.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblPickFail.Size = new System.Drawing.Size(201, 28);
            this.lblPickFail.TabIndex = 3;
            this.lblPickFail.Text = "0 ea";
            this.lblPickFail.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblBinQtyCaption
            // 
            this.lblBinQtyCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblBinQtyCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinQtyCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblBinQtyCaption.Location = new System.Drawing.Point(413, 37);
            this.lblBinQtyCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblBinQtyCaption.Name = "lblBinQtyCaption";
            this.lblBinQtyCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblBinQtyCaption.Size = new System.Drawing.Size(201, 28);
            this.lblBinQtyCaption.TabIndex = 4;
            this.lblBinQtyCaption.Tag = "i18n:work.workInfo.workBinQty";
            this.lblBinQtyCaption.Text = "작업 BIN 수량";
            this.lblBinQtyCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblBinQty
            // 
            this.lblBinQty.BackColor = System.Drawing.Color.White;
            this.lblBinQty.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinQty.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblBinQty.Location = new System.Drawing.Point(616, 37);
            this.lblBinQty.Margin = new System.Windows.Forms.Padding(1);
            this.lblBinQty.Name = "lblBinQty";
            this.lblBinQty.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblBinQty.Size = new System.Drawing.Size(202, 28);
            this.lblBinQty.TabIndex = 5;
            this.lblBinQty.Text = "0 ea";
            this.lblBinQty.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCollet1Caption
            // 
            this.lblCollet1Caption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblCollet1Caption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet1Caption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblCollet1Caption.Location = new System.Drawing.Point(7, 67);
            this.lblCollet1Caption.Margin = new System.Windows.Forms.Padding(1);
            this.lblCollet1Caption.Name = "lblCollet1Caption";
            this.lblCollet1Caption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblCollet1Caption.Size = new System.Drawing.Size(201, 28);
            this.lblCollet1Caption.TabIndex = 6;
            this.lblCollet1Caption.Tag = "i18n:work.workInfo.collet1Use";
            this.lblCollet1Caption.Text = "# 1 Collet 사용";
            this.lblCollet1Caption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCollet1
            // 
            this.lblCollet1.BackColor = System.Drawing.Color.White;
            this.lblCollet1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet1.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblCollet1.Location = new System.Drawing.Point(210, 67);
            this.lblCollet1.Margin = new System.Windows.Forms.Padding(1);
            this.lblCollet1.Name = "lblCollet1";
            this.lblCollet1.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblCollet1.Size = new System.Drawing.Size(201, 28);
            this.lblCollet1.TabIndex = 7;
            this.lblCollet1.Text = "0";
            this.lblCollet1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPlaceFailCaption
            // 
            this.lblPlaceFailCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblPlaceFailCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlaceFailCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblPlaceFailCaption.Location = new System.Drawing.Point(413, 67);
            this.lblPlaceFailCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblPlaceFailCaption.Name = "lblPlaceFailCaption";
            this.lblPlaceFailCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblPlaceFailCaption.Size = new System.Drawing.Size(201, 28);
            this.lblPlaceFailCaption.TabIndex = 8;
            this.lblPlaceFailCaption.Tag = "i18n:work.workInfo.placeFail";
            this.lblPlaceFailCaption.Text = "PLACE 실패 수량";
            this.lblPlaceFailCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPlaceFail
            // 
            this.lblPlaceFail.BackColor = System.Drawing.Color.White;
            this.lblPlaceFail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlaceFail.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblPlaceFail.Location = new System.Drawing.Point(616, 67);
            this.lblPlaceFail.Margin = new System.Windows.Forms.Padding(1);
            this.lblPlaceFail.Name = "lblPlaceFail";
            this.lblPlaceFail.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblPlaceFail.Size = new System.Drawing.Size(202, 28);
            this.lblPlaceFail.TabIndex = 9;
            this.lblPlaceFail.Text = "0 ea";
            this.lblPlaceFail.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblNeedleCaption
            // 
            this.lblNeedleCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNeedleCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblNeedleCaption.Location = new System.Drawing.Point(7, 97);
            this.lblNeedleCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblNeedleCaption.Name = "lblNeedleCaption";
            this.lblNeedleCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblNeedleCaption.Size = new System.Drawing.Size(201, 28);
            this.lblNeedleCaption.TabIndex = 10;
            this.lblNeedleCaption.Tag = "i18n:work.workInfo.needleUse";
            this.lblNeedleCaption.Text = "NEEDLE 사용 횟수";
            this.lblNeedleCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNeedle
            // 
            this.lblNeedle.BackColor = System.Drawing.Color.White;
            this.lblNeedle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedle.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblNeedle.Location = new System.Drawing.Point(210, 97);
            this.lblNeedle.Margin = new System.Windows.Forms.Padding(1);
            this.lblNeedle.Name = "lblNeedle";
            this.lblNeedle.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblNeedle.Size = new System.Drawing.Size(201, 28);
            this.lblNeedle.TabIndex = 11;
            this.lblNeedle.Text = "0";
            this.lblNeedle.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCollet2Caption
            // 
            this.lblCollet2Caption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblCollet2Caption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet2Caption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblCollet2Caption.Location = new System.Drawing.Point(413, 97);
            this.lblCollet2Caption.Margin = new System.Windows.Forms.Padding(1);
            this.lblCollet2Caption.Name = "lblCollet2Caption";
            this.lblCollet2Caption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblCollet2Caption.Size = new System.Drawing.Size(201, 28);
            this.lblCollet2Caption.TabIndex = 12;
            this.lblCollet2Caption.Tag = "i18n:work.workInfo.collet2Use";
            this.lblCollet2Caption.Text = "# 2 Collet 사용";
            this.lblCollet2Caption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCollet2
            // 
            this.lblCollet2.BackColor = System.Drawing.Color.White;
            this.lblCollet2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCollet2.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblCollet2.Location = new System.Drawing.Point(616, 97);
            this.lblCollet2.Margin = new System.Windows.Forms.Padding(1);
            this.lblCollet2.Name = "lblCollet2";
            this.lblCollet2.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblCollet2.Size = new System.Drawing.Size(202, 28);
            this.lblCollet2.TabIndex = 13;
            this.lblCollet2.Text = "0";
            this.lblCollet2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblBinArrMonCaption
            // 
            this.lblBinArrMonCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblBinArrMonCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinArrMonCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblBinArrMonCaption.Location = new System.Drawing.Point(7, 127);
            this.lblBinArrMonCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblBinArrMonCaption.Name = "lblBinArrMonCaption";
            this.lblBinArrMonCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblBinArrMonCaption.Size = new System.Drawing.Size(201, 228);
            this.lblBinArrMonCaption.TabIndex = 14;
            this.lblBinArrMonCaption.Tag = "i18n:work.workInfo.binArrMon";
            this.lblBinArrMonCaption.Text = "빈 배열 모니터링";
            this.lblBinArrMonCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblBinArrMon
            // 
            this.lblBinArrMon.BackColor = System.Drawing.Color.White;
            this.lblBinArrMon.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinArrMon.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblBinArrMon.Location = new System.Drawing.Point(210, 127);
            this.lblBinArrMon.Margin = new System.Windows.Forms.Padding(1);
            this.lblBinArrMon.Name = "lblBinArrMon";
            this.lblBinArrMon.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblBinArrMon.Size = new System.Drawing.Size(201, 228);
            this.lblBinArrMon.TabIndex = 15;
            this.lblBinArrMon.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // timeLayout
            // 
            this.timeLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.timeLayout.ColumnCount = 1;
            this.timeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.timeLayout.Controls.Add(this.lblTimeHeader, 0, 0);
            this.timeLayout.Controls.Add(this.workTimeBody, 0, 1);
            this.timeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.timeLayout.Location = new System.Drawing.Point(842, 498);
            this.timeLayout.Name = "timeLayout";
            this.timeLayout.RowCount = 2;
            this.timeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.timeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.timeLayout.Size = new System.Drawing.Size(832, 398);
            this.timeLayout.TabIndex = 3;
            // 
            // lblTimeHeader
            // 
            this.lblTimeHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblTimeHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTimeHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblTimeHeader.ForeColor = System.Drawing.Color.White;
            this.lblTimeHeader.Location = new System.Drawing.Point(3, 0);
            this.lblTimeHeader.Name = "lblTimeHeader";
            this.lblTimeHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblTimeHeader.Size = new System.Drawing.Size(826, 30);
            this.lblTimeHeader.TabIndex = 0;
            this.lblTimeHeader.Tag = "i18n:work.sec.workTime";
            this.lblTimeHeader.Text = "작업 시간";
            this.lblTimeHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workTimeBody
            // 
            this.workTimeBody.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.workTimeBody.ColumnCount = 4;
            this.workTimeBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workTimeBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workTimeBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workTimeBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.workTimeBody.Controls.Add(this.lblLoadCaption, 0, 0);
            this.workTimeBody.Controls.Add(this.lblLoad, 1, 0);
            this.workTimeBody.Controls.Add(this.lblUpCaption, 2, 0);
            this.workTimeBody.Controls.Add(this.lblUp, 3, 0);
            this.workTimeBody.Controls.Add(this.lblContUpCaption, 0, 1);
            this.workTimeBody.Controls.Add(this.lblContUp, 1, 1);
            this.workTimeBody.Controls.Add(this.lblNormDownCaption, 2, 1);
            this.workTimeBody.Controls.Add(this.lblNormDown, 3, 1);
            this.workTimeBody.Controls.Add(this.lblErrDownCaption, 0, 2);
            this.workTimeBody.Controls.Add(this.lblErrDown, 1, 2);
            this.workTimeBody.Controls.Add(this.lblErrCntCaption, 2, 2);
            this.workTimeBody.Controls.Add(this.lblErrCnt, 3, 2);
            this.workTimeBody.Controls.Add(this.lblRecoveryCaption, 0, 3);
            this.workTimeBody.Controls.Add(this.lblRecovery, 1, 3);
            this.workTimeBody.Controls.Add(this.lblUphCaption, 2, 3);
            this.workTimeBody.Controls.Add(this.lblUph, 3, 3);
            this.workTimeBody.Controls.Add(this.lblMtbfCaption, 0, 4);
            this.workTimeBody.Controls.Add(this.lblMtbf, 1, 4);
            this.workTimeBody.Controls.Add(this.lblMttrCaption, 2, 4);
            this.workTimeBody.Controls.Add(this.lblMttr, 3, 4);
            this.workTimeBody.Controls.Add(this.lblCycleCaption, 0, 5);
            this.workTimeBody.Controls.Add(this.lblCycle, 1, 5);
            this.workTimeBody.Controls.Add(this.lblRateCaption, 2, 5);
            this.workTimeBody.Controls.Add(this.lblRate, 3, 5);
            this.workTimeBody.Controls.Add(this.lblLotCaption, 0, 6);
            this.workTimeBody.Controls.Add(this.lblLot, 1, 6);
            this.workTimeBody.Controls.Add(this.btnCcs, 2, 6);
            this.workTimeBody.Controls.Add(this.btnTestAlarm, 3, 7);
            this.workTimeBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workTimeBody.Location = new System.Drawing.Point(3, 33);
            this.workTimeBody.Name = "workTimeBody";
            this.workTimeBody.Padding = new System.Windows.Forms.Padding(6);
            this.workTimeBody.RowCount = 8;
            this.workTimeBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workTimeBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workTimeBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workTimeBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workTimeBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workTimeBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.workTimeBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.workTimeBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 27F));
            this.workTimeBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.workTimeBody.Size = new System.Drawing.Size(826, 362);
            this.workTimeBody.TabIndex = 1;
            // 
            // lblLoadCaption
            // 
            this.lblLoadCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblLoadCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLoadCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblLoadCaption.Location = new System.Drawing.Point(7, 7);
            this.lblLoadCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblLoadCaption.Name = "lblLoadCaption";
            this.lblLoadCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblLoadCaption.Size = new System.Drawing.Size(201, 28);
            this.lblLoadCaption.TabIndex = 0;
            this.lblLoadCaption.Tag = "i18n:work.workTime.load";
            this.lblLoadCaption.Text = "부하 시간";
            this.lblLoadCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLoad
            // 
            this.lblLoad.BackColor = System.Drawing.Color.White;
            this.lblLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLoad.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLoad.Location = new System.Drawing.Point(210, 7);
            this.lblLoad.Margin = new System.Windows.Forms.Padding(1);
            this.lblLoad.Name = "lblLoad";
            this.lblLoad.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblLoad.Size = new System.Drawing.Size(201, 28);
            this.lblLoad.TabIndex = 1;
            this.lblLoad.Text = "00:00:00";
            this.lblLoad.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblUpCaption
            // 
            this.lblUpCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblUpCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUpCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblUpCaption.Location = new System.Drawing.Point(413, 7);
            this.lblUpCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblUpCaption.Name = "lblUpCaption";
            this.lblUpCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblUpCaption.Size = new System.Drawing.Size(201, 28);
            this.lblUpCaption.TabIndex = 2;
            this.lblUpCaption.Tag = "i18n:work.workTime.up";
            this.lblUpCaption.Text = "가동 시간";
            this.lblUpCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblUp
            // 
            this.lblUp.BackColor = System.Drawing.Color.White;
            this.lblUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUp.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblUp.Location = new System.Drawing.Point(616, 7);
            this.lblUp.Margin = new System.Windows.Forms.Padding(1);
            this.lblUp.Name = "lblUp";
            this.lblUp.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblUp.Size = new System.Drawing.Size(203, 28);
            this.lblUp.TabIndex = 3;
            this.lblUp.Text = "00:00:00";
            this.lblUp.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblContUpCaption
            // 
            this.lblContUpCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblContUpCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblContUpCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblContUpCaption.Location = new System.Drawing.Point(7, 37);
            this.lblContUpCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblContUpCaption.Name = "lblContUpCaption";
            this.lblContUpCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblContUpCaption.Size = new System.Drawing.Size(201, 28);
            this.lblContUpCaption.TabIndex = 4;
            this.lblContUpCaption.Tag = "i18n:work.workTime.contUp";
            this.lblContUpCaption.Text = "연속 가동 시간";
            this.lblContUpCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblContUp
            // 
            this.lblContUp.BackColor = System.Drawing.Color.White;
            this.lblContUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblContUp.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblContUp.Location = new System.Drawing.Point(210, 37);
            this.lblContUp.Margin = new System.Windows.Forms.Padding(1);
            this.lblContUp.Name = "lblContUp";
            this.lblContUp.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblContUp.Size = new System.Drawing.Size(201, 28);
            this.lblContUp.TabIndex = 5;
            this.lblContUp.Text = "00:00:00";
            this.lblContUp.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblNormDownCaption
            // 
            this.lblNormDownCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNormDownCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNormDownCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblNormDownCaption.Location = new System.Drawing.Point(413, 37);
            this.lblNormDownCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblNormDownCaption.Name = "lblNormDownCaption";
            this.lblNormDownCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblNormDownCaption.Size = new System.Drawing.Size(201, 28);
            this.lblNormDownCaption.TabIndex = 6;
            this.lblNormDownCaption.Tag = "i18n:work.workTime.normDown";
            this.lblNormDownCaption.Text = "통상 정지 시간";
            this.lblNormDownCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNormDown
            // 
            this.lblNormDown.BackColor = System.Drawing.Color.White;
            this.lblNormDown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNormDown.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblNormDown.Location = new System.Drawing.Point(616, 37);
            this.lblNormDown.Margin = new System.Windows.Forms.Padding(1);
            this.lblNormDown.Name = "lblNormDown";
            this.lblNormDown.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblNormDown.Size = new System.Drawing.Size(203, 28);
            this.lblNormDown.TabIndex = 7;
            this.lblNormDown.Text = "00:00:00";
            this.lblNormDown.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblErrDownCaption
            // 
            this.lblErrDownCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblErrDownCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblErrDownCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblErrDownCaption.Location = new System.Drawing.Point(7, 67);
            this.lblErrDownCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblErrDownCaption.Name = "lblErrDownCaption";
            this.lblErrDownCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblErrDownCaption.Size = new System.Drawing.Size(201, 28);
            this.lblErrDownCaption.TabIndex = 8;
            this.lblErrDownCaption.Tag = "i18n:work.workTime.errDown";
            this.lblErrDownCaption.Text = "이상 정지 시간";
            this.lblErrDownCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblErrDown
            // 
            this.lblErrDown.BackColor = System.Drawing.Color.White;
            this.lblErrDown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblErrDown.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblErrDown.Location = new System.Drawing.Point(210, 67);
            this.lblErrDown.Margin = new System.Windows.Forms.Padding(1);
            this.lblErrDown.Name = "lblErrDown";
            this.lblErrDown.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblErrDown.Size = new System.Drawing.Size(201, 28);
            this.lblErrDown.TabIndex = 9;
            this.lblErrDown.Text = "00:00:00";
            this.lblErrDown.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblErrCntCaption
            // 
            this.lblErrCntCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblErrCntCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblErrCntCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblErrCntCaption.Location = new System.Drawing.Point(413, 67);
            this.lblErrCntCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblErrCntCaption.Name = "lblErrCntCaption";
            this.lblErrCntCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblErrCntCaption.Size = new System.Drawing.Size(201, 28);
            this.lblErrCntCaption.TabIndex = 10;
            this.lblErrCntCaption.Tag = "i18n:work.workTime.errCnt";
            this.lblErrCntCaption.Text = "이상 정지 횟수";
            this.lblErrCntCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblErrCnt
            // 
            this.lblErrCnt.BackColor = System.Drawing.Color.White;
            this.lblErrCnt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblErrCnt.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblErrCnt.Location = new System.Drawing.Point(616, 67);
            this.lblErrCnt.Margin = new System.Windows.Forms.Padding(1);
            this.lblErrCnt.Name = "lblErrCnt";
            this.lblErrCnt.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblErrCnt.Size = new System.Drawing.Size(203, 28);
            this.lblErrCnt.TabIndex = 11;
            this.lblErrCnt.Text = "0 ea";
            this.lblErrCnt.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblRecoveryCaption
            // 
            this.lblRecoveryCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblRecoveryCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRecoveryCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblRecoveryCaption.Location = new System.Drawing.Point(7, 97);
            this.lblRecoveryCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblRecoveryCaption.Name = "lblRecoveryCaption";
            this.lblRecoveryCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblRecoveryCaption.Size = new System.Drawing.Size(201, 28);
            this.lblRecoveryCaption.TabIndex = 12;
            this.lblRecoveryCaption.Tag = "i18n:work.workTime.recovery";
            this.lblRecoveryCaption.Text = "이상 복귀 시간";
            this.lblRecoveryCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecovery
            // 
            this.lblRecovery.BackColor = System.Drawing.Color.White;
            this.lblRecovery.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRecovery.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblRecovery.Location = new System.Drawing.Point(210, 97);
            this.lblRecovery.Margin = new System.Windows.Forms.Padding(1);
            this.lblRecovery.Name = "lblRecovery";
            this.lblRecovery.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblRecovery.Size = new System.Drawing.Size(201, 28);
            this.lblRecovery.TabIndex = 13;
            this.lblRecovery.Text = "00:00:00";
            this.lblRecovery.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblUphCaption
            // 
            this.lblUphCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblUphCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUphCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblUphCaption.Location = new System.Drawing.Point(413, 97);
            this.lblUphCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblUphCaption.Name = "lblUphCaption";
            this.lblUphCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblUphCaption.Size = new System.Drawing.Size(201, 28);
            this.lblUphCaption.TabIndex = 14;
            this.lblUphCaption.Tag = "i18n:work.workTime.uph";
            this.lblUphCaption.Text = "UPH";
            this.lblUphCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblUph
            // 
            this.lblUph.BackColor = System.Drawing.Color.White;
            this.lblUph.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUph.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblUph.Location = new System.Drawing.Point(616, 97);
            this.lblUph.Margin = new System.Windows.Forms.Padding(1);
            this.lblUph.Name = "lblUph";
            this.lblUph.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblUph.Size = new System.Drawing.Size(203, 28);
            this.lblUph.TabIndex = 15;
            this.lblUph.Text = "0.00";
            this.lblUph.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblMtbfCaption
            // 
            this.lblMtbfCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblMtbfCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMtbfCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblMtbfCaption.Location = new System.Drawing.Point(7, 127);
            this.lblMtbfCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblMtbfCaption.Name = "lblMtbfCaption";
            this.lblMtbfCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblMtbfCaption.Size = new System.Drawing.Size(201, 28);
            this.lblMtbfCaption.TabIndex = 16;
            this.lblMtbfCaption.Tag = "i18n:work.workTime.mtbf";
            this.lblMtbfCaption.Text = "MTBF";
            this.lblMtbfCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMtbf
            // 
            this.lblMtbf.BackColor = System.Drawing.Color.White;
            this.lblMtbf.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMtbf.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblMtbf.Location = new System.Drawing.Point(210, 127);
            this.lblMtbf.Margin = new System.Windows.Forms.Padding(1);
            this.lblMtbf.Name = "lblMtbf";
            this.lblMtbf.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblMtbf.Size = new System.Drawing.Size(201, 28);
            this.lblMtbf.TabIndex = 17;
            this.lblMtbf.Text = "00:00:00";
            this.lblMtbf.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblMttrCaption
            // 
            this.lblMttrCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblMttrCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMttrCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblMttrCaption.Location = new System.Drawing.Point(413, 127);
            this.lblMttrCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblMttrCaption.Name = "lblMttrCaption";
            this.lblMttrCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblMttrCaption.Size = new System.Drawing.Size(201, 28);
            this.lblMttrCaption.TabIndex = 18;
            this.lblMttrCaption.Tag = "i18n:work.workTime.mttr";
            this.lblMttrCaption.Text = "MTTR";
            this.lblMttrCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMttr
            // 
            this.lblMttr.BackColor = System.Drawing.Color.White;
            this.lblMttr.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMttr.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblMttr.Location = new System.Drawing.Point(616, 127);
            this.lblMttr.Margin = new System.Windows.Forms.Padding(1);
            this.lblMttr.Name = "lblMttr";
            this.lblMttr.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblMttr.Size = new System.Drawing.Size(203, 28);
            this.lblMttr.TabIndex = 19;
            this.lblMttr.Text = "00:00:00";
            this.lblMttr.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCycleCaption
            // 
            this.lblCycleCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblCycleCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCycleCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblCycleCaption.Location = new System.Drawing.Point(7, 157);
            this.lblCycleCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblCycleCaption.Name = "lblCycleCaption";
            this.lblCycleCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblCycleCaption.Size = new System.Drawing.Size(201, 28);
            this.lblCycleCaption.TabIndex = 20;
            this.lblCycleCaption.Tag = "i18n:work.workTime.cycle";
            this.lblCycleCaption.Text = "CYCLE TIME";
            this.lblCycleCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCycle
            // 
            this.lblCycle.BackColor = System.Drawing.Color.White;
            this.lblCycle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCycle.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblCycle.Location = new System.Drawing.Point(210, 157);
            this.lblCycle.Margin = new System.Windows.Forms.Padding(1);
            this.lblCycle.Name = "lblCycle";
            this.lblCycle.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblCycle.Size = new System.Drawing.Size(201, 28);
            this.lblCycle.TabIndex = 21;
            this.lblCycle.Text = "0 ms";
            this.lblCycle.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblRateCaption
            // 
            this.lblRateCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblRateCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRateCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblRateCaption.Location = new System.Drawing.Point(413, 157);
            this.lblRateCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblRateCaption.Name = "lblRateCaption";
            this.lblRateCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblRateCaption.Size = new System.Drawing.Size(201, 28);
            this.lblRateCaption.TabIndex = 22;
            this.lblRateCaption.Tag = "i18n:work.workTime.rate";
            this.lblRateCaption.Text = "가동률";
            this.lblRateCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRate
            // 
            this.lblRate.BackColor = System.Drawing.Color.White;
            this.lblRate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRate.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblRate.Location = new System.Drawing.Point(616, 157);
            this.lblRate.Margin = new System.Windows.Forms.Padding(1);
            this.lblRate.Name = "lblRate";
            this.lblRate.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblRate.Size = new System.Drawing.Size(203, 28);
            this.lblRate.TabIndex = 23;
            this.lblRate.Text = "0.00 %";
            this.lblRate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblLotCaption
            // 
            this.lblLotCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblLotCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLotCaption.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblLotCaption.Location = new System.Drawing.Point(7, 187);
            this.lblLotCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblLotCaption.Name = "lblLotCaption";
            this.lblLotCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblLotCaption.Size = new System.Drawing.Size(201, 48);
            this.lblLotCaption.TabIndex = 24;
            this.lblLotCaption.Tag = "i18n:work.workTime.lotId";
            this.lblLotCaption.Text = "작업중인 LOT ID";
            this.lblLotCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLot
            // 
            this.lblLot.BackColor = System.Drawing.Color.White;
            this.lblLot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLot.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblLot.Location = new System.Drawing.Point(210, 187);
            this.lblLot.Margin = new System.Windows.Forms.Padding(1);
            this.lblLot.Name = "lblLot";
            this.lblLot.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblLot.Size = new System.Drawing.Size(201, 48);
            this.lblLot.TabIndex = 25;
            this.lblLot.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnCcs
            // 
            this.btnCcs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(89)))), ((int)(((byte)(89)))), ((int)(((byte)(89)))));
            this.workTimeBody.SetColumnSpan(this.btnCcs, 2);
            this.btnCcs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCcs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCcs.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnCcs.ForeColor = System.Drawing.Color.White;
            this.btnCcs.Location = new System.Drawing.Point(415, 189);
            this.btnCcs.Name = "btnCcs";
            this.btnCcs.Size = new System.Drawing.Size(402, 44);
            this.btnCcs.TabIndex = 26;
            this.btnCcs.Tag = "i18n:work.workTime.ccs";
            this.btnCcs.Text = "CCS 검수 확인";
            this.btnCcs.UseVisualStyleBackColor = false;
            // 
            // btnTestAlarm
            // 
            this.btnTestAlarm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(183)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.workTimeBody.SetColumnSpan(this.btnTestAlarm, 4);
            this.btnTestAlarm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTestAlarm.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnTestAlarm.ForeColor = System.Drawing.Color.White;
            this.btnTestAlarm.Location = new System.Drawing.Point(9, 266);
            this.btnTestAlarm.Name = "btnTestAlarm";
            this.btnTestAlarm.Size = new System.Drawing.Size(122, 47);
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
            this.visionLayout.ResumeLayout(false);
            this.visionPanel.ResumeLayout(false);
            this.visionPanel.PerformLayout();
            this.mapLayout.ResumeLayout(false);
            this.mapHeaderLayout.ResumeLayout(false);
            this.infoLayout.ResumeLayout(false);
            this.workInfoBody.ResumeLayout(false);
            this.timeLayout.ResumeLayout(false);
            this.workTimeBody.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}

