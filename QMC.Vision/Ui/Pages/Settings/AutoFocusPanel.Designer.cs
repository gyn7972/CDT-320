using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using QMC.Vision.Ui;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    partial class AutoFocusPanel
    {
        private IContainer components = null;

        private Panel pnlNav;
        private Panel pnlNavHdrLine;
        private FlowLayoutPanel flowNav;
        private Panel pnlTop;
        private Panel pnlBest;
        private Panel pnlLog;
        private TableLayoutPanel logSplit;
        private Panel pnlComm;
        private Panel pnlTactBox;
        private Panel pnlChart;

        private Label lblNavHdr;
        private Label lblHdrBest;
        private Label lblHdrLog;
        private Label lblHdrTact;
        private Label lblHdrChart;
        private Label lblHdrImg;
        private Label lblRoiHdr;
        private Label lblTestHdr;

        private SidebarButton btnNav0;
        private SidebarButton btnNav1;
        private SidebarButton btnNav2;
        private SidebarButton btnNav3;

        private FlowLayoutPanel pnlButtons;
        private Button btnRoi0;
        private Button btnRoi1;
        private Button btnRoi2;
        private Button btnRoi3;
        private Button btnRoiJog;
        private Button btnProcImg;
        private Button btnRoiClear;
        private Button btnTestScan;
        private Button btnTcpScan;
        private Button btnTcpVal;
        private Button btnImgSeq;
        private Button btnTestStep;
        private Button btnReset;
        private Button btnClearLog;

        private DataGridView grid;
        private TextBox txtTact;
        private TextBox txtLog;
        private Chart chart;
        private TableLayoutPanel chartSplit;
        private Panel pnlChartSide;
        private Panel pnlImage;
        private CameraView camView;
        private Timer timer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new Container();
            this.pnlNav = new Panel();
            this.pnlNavHdrLine = new Panel();
            this.flowNav = new FlowLayoutPanel();
            this.pnlTop = new Panel();
            this.pnlBest = new Panel();
            this.pnlLog = new Panel();
            this.logSplit = new TableLayoutPanel();
            this.pnlComm = new Panel();
            this.pnlTactBox = new Panel();
            this.pnlChart = new Panel();
            this.lblNavHdr = new Label();
            this.lblHdrBest = new Label();
            this.lblHdrLog = new Label();
            this.lblHdrTact = new Label();
            this.lblHdrChart = new Label();
            this.lblHdrImg = new Label();
            this.lblRoiHdr = new Label();
            this.lblTestHdr = new Label();
            this.btnNav0 = new SidebarButton();
            this.btnNav1 = new SidebarButton();
            this.btnNav2 = new SidebarButton();
            this.btnNav3 = new SidebarButton();
            this.pnlButtons = new FlowLayoutPanel();
            this.btnRoi0 = new Button();
            this.btnRoi1 = new Button();
            this.btnRoi2 = new Button();
            this.btnRoi3 = new Button();
            this.btnRoiJog = new Button();
            this.btnProcImg = new Button();
            this.btnRoiClear = new Button();
            this.btnTestScan = new Button();
            this.btnTcpScan = new Button();
            this.btnTcpVal = new Button();
            this.btnImgSeq = new Button();
            this.btnTestStep = new Button();
            this.btnReset = new Button();
            this.btnClearLog = new Button();
            this.grid = new DataGridView();
            this.txtTact = new TextBox();
            this.txtLog = new TextBox();
            this.chart = new Chart();
            this.chartSplit = new TableLayoutPanel();
            this.pnlChartSide = new Panel();
            this.pnlImage = new Panel();
            this.camView = new CameraView();

            ChartArea area = new ChartArea();
            area.Name = "main";
            area.AxisX.Title = "모터 위치 (mm)";
            area.AxisY.Title = "Score";
            area.AxisX.MajorGrid.LineColor = Color.FromArgb(224, 224, 224);
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(224, 224, 224);
            area.AxisY.IsStartedFromZero = true;
            Legend legend = new Legend();
            legend.Name = "legend";
            legend.Docking = Docking.Top;
            legend.Alignment = StringAlignment.Center;

            ((ISupportInitialize)(this.grid)).BeginInit();
            ((ISupportInitialize)(this.chart)).BeginInit();
            this.pnlNav.SuspendLayout();
            this.flowNav.SuspendLayout();
            this.pnlTop.SuspendLayout();
            this.pnlBest.SuspendLayout();
            this.pnlLog.SuspendLayout();
            this.logSplit.SuspendLayout();
            this.pnlComm.SuspendLayout();
            this.pnlTactBox.SuspendLayout();
            this.pnlChart.SuspendLayout();
            this.chartSplit.SuspendLayout();
            this.pnlChartSide.SuspendLayout();
            this.pnlImage.SuspendLayout();
            this.pnlButtons.SuspendLayout();
            this.SuspendLayout();

            // ── 우측 레일: 오토 포커스 대상(4개) ──
            this.pnlNav.Dock = DockStyle.Right;
            this.pnlNav.Width = 196;
            this.pnlNav.BackColor = UiTheme.SidebarBg;
            this.pnlNav.Controls.Add(this.flowNav);
            this.pnlNav.Controls.Add(this.pnlNavHdrLine);
            this.pnlNav.Controls.Add(this.lblNavHdr);

            this.lblNavHdr.Dock = DockStyle.Top;
            this.lblNavHdr.Height = 28;
            this.lblNavHdr.Text = "  오토 포커스 대상";
            this.lblNavHdr.BackColor = UiTheme.SidebarHeaderBg;
            this.lblNavHdr.ForeColor = UiTheme.SidebarHeaderFg;
            this.lblNavHdr.Font = UiTheme.SectionFont;
            this.lblNavHdr.TextAlign = ContentAlignment.MiddleLeft;

            this.pnlNavHdrLine.Dock = DockStyle.Top;
            this.pnlNavHdrLine.Height = 2;
            this.pnlNavHdrLine.BackColor = UiTheme.StatusBarBg;

            this.flowNav.Dock = DockStyle.Fill;
            this.flowNav.FlowDirection = FlowDirection.TopDown;
            this.flowNav.WrapContents = false;
            this.flowNav.AutoScroll = true;
            this.flowNav.BackColor = UiTheme.SidebarBg;
            this.flowNav.Padding = new Padding(4, 6, 4, 6);
            this.flowNav.Controls.Add(this.btnNav0);
            this.flowNav.Controls.Add(this.btnNav1);
            this.flowNav.Controls.Add(this.btnNav2);
            this.flowNav.Controls.Add(this.btnNav3);
            this.btnNav0.Text = "바텀 검사 - 콜렛"; this.btnNav0.Width = 184; this.btnNav0.Height = 38; this.btnNav0.Margin = new Padding(0, 0, 0, 3);
            this.btnNav1.Text = "바텀 검사 - 다이"; this.btnNav1.Width = 184; this.btnNav1.Height = 38; this.btnNav1.Margin = new Padding(0, 0, 0, 3);
            this.btnNav2.Text = "앞쪽 측면 검사"; this.btnNav2.Width = 184; this.btnNav2.Height = 38; this.btnNav2.Margin = new Padding(0, 0, 0, 3);
            this.btnNav3.Text = "뒤쪽 측면 검사"; this.btnNav3.Width = 184; this.btnNav3.Height = 38; this.btnNav3.Margin = new Padding(0, 0, 0, 3);

            // ── 상단(높이 300): BEST 그리드(좌) | 통신로그+Tact(우, 50/50) ──
            this.pnlTop.Dock = DockStyle.Top;
            this.pnlTop.Height = 300;
            this.pnlTop.Controls.Add(this.pnlLog);
            this.pnlTop.Controls.Add(this.pnlBest);

            // pnlBest (좌): BEST 그리드
            this.pnlBest.Dock = DockStyle.Left;
            this.pnlBest.Width = 440;
            this.pnlBest.Padding = new Padding(6, 6, 3, 6);
            this.pnlBest.Controls.Add(this.grid);
            this.pnlBest.Controls.Add(this.lblHdrBest);
            this.lblHdrBest.Dock = DockStyle.Top;
            this.lblHdrBest.Height = 28;
            this.lblHdrBest.Text = "BEST — ROI별 최적 초점 (위치 / Score)";
            this.lblHdrBest.BackColor = UiTheme.StatusBarBg;
            this.lblHdrBest.ForeColor = UiTheme.StatusBarFg;
            this.lblHdrBest.Font = UiTheme.SectionFont;
            this.lblHdrBest.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrBest.Padding = new Padding(10, 0, 0, 0);
            this.grid.Dock = DockStyle.Fill;
            this.grid.ReadOnly = true;
            this.grid.AllowUserToAddRows = false;
            this.grid.RowHeadersVisible = false;
            this.grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // pnlLog (우): 통신로그(상) + Tact(하) 50/50
            this.pnlLog.Dock = DockStyle.Fill;
            this.pnlLog.Padding = new Padding(3, 6, 6, 6);
            this.pnlLog.Controls.Add(this.logSplit);

            this.logSplit.Dock = DockStyle.Fill;
            this.logSplit.ColumnCount = 1;
            this.logSplit.RowCount = 2;
            this.logSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this.logSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            this.logSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            this.logSplit.Controls.Add(this.pnlComm, 0, 0);
            this.logSplit.Controls.Add(this.pnlTactBox, 0, 1);

            this.pnlComm.Dock = DockStyle.Fill;
            this.pnlComm.Padding = new Padding(0, 0, 0, 3);
            this.pnlComm.Controls.Add(this.txtLog);
            this.pnlComm.Controls.Add(this.lblHdrLog);
            this.lblHdrLog.Dock = DockStyle.Top;
            this.lblHdrLog.Height = 28;
            this.lblHdrLog.Text = "통신 로그 (FOCUS TX / RX / EPD / ARM)";
            this.lblHdrLog.BackColor = UiTheme.StatusBarBg;
            this.lblHdrLog.ForeColor = UiTheme.StatusBarFg;
            this.lblHdrLog.Font = UiTheme.SectionFont;
            this.lblHdrLog.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrLog.Padding = new Padding(10, 0, 0, 0);
            this.txtLog.Dock = DockStyle.Fill;
            this.txtLog.Multiline = true;
            this.txtLog.ReadOnly = true;
            this.txtLog.WordWrap = false;
            this.txtLog.ScrollBars = ScrollBars.Both;
            this.txtLog.BackColor = UiTheme.VisionBg;
            this.txtLog.ForeColor = UiTheme.VisionInfoFg;
            this.txtLog.BorderStyle = BorderStyle.None;
            this.txtLog.Font = new Font("Consolas", 9F);

            this.pnlTactBox.Dock = DockStyle.Fill;
            this.pnlTactBox.Padding = new Padding(0, 3, 0, 0);
            this.pnlTactBox.Controls.Add(this.txtTact);
            this.pnlTactBox.Controls.Add(this.lblHdrTact);
            this.lblHdrTact.Dock = DockStyle.Top;
            this.lblHdrTact.Height = 28;
            this.lblHdrTact.Text = "Tact Time (사이클 / 스텝)";
            this.lblHdrTact.BackColor = UiTheme.StatusBarBg;
            this.lblHdrTact.ForeColor = UiTheme.StatusBarFg;
            this.lblHdrTact.Font = UiTheme.SectionFont;
            this.lblHdrTact.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrTact.Padding = new Padding(10, 0, 0, 0);
            this.txtTact.Dock = DockStyle.Fill;
            this.txtTact.Multiline = true;
            this.txtTact.ReadOnly = true;
            this.txtTact.WordWrap = false;
            this.txtTact.ScrollBars = ScrollBars.Both;
            this.txtTact.BackColor = UiTheme.VisionBg;
            this.txtTact.ForeColor = UiTheme.VisionInfoFg;
            this.txtTact.BorderStyle = BorderStyle.None;
            this.txtTact.Font = new Font("Consolas", 9F);

            // ── 하단(Fill): 차트 40 | 이미지 40 | 버튼 20 ──
            this.pnlChart.Dock = DockStyle.Fill;
            this.pnlChart.Padding = new Padding(6, 0, 6, 6);
            this.pnlChart.Controls.Add(this.chartSplit);

            this.chartSplit.Dock = DockStyle.Fill;
            this.chartSplit.ColumnCount = 3;
            this.chartSplit.RowCount = 1;
            this.chartSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            this.chartSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            this.chartSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            this.chartSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this.chartSplit.Controls.Add(this.pnlChartSide, 0, 0);
            this.chartSplit.Controls.Add(this.pnlImage, 1, 0);
            this.chartSplit.Controls.Add(this.pnlButtons, 2, 0);

            // 포커스 곡선
            this.pnlChartSide.Dock = DockStyle.Fill;
            this.pnlChartSide.Padding = new Padding(0, 0, 3, 0);
            this.pnlChartSide.Controls.Add(this.chart);
            this.pnlChartSide.Controls.Add(this.lblHdrChart);
            this.lblHdrChart.Dock = DockStyle.Top;
            this.lblHdrChart.Height = 28;
            this.lblHdrChart.Text = "포커스 곡선 (X=모터 위치, Y=Score · ★=Best)";
            this.lblHdrChart.BackColor = UiTheme.StatusBarBg;
            this.lblHdrChart.ForeColor = UiTheme.StatusBarFg;
            this.lblHdrChart.Font = UiTheme.SectionFont;
            this.lblHdrChart.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrChart.Padding = new Padding(10, 0, 0, 0);
            this.chart.Dock = DockStyle.Fill;
            this.chart.Size = new Size(360, 300);
            this.chart.BackColor = Color.White;
            this.chart.ChartAreas.Add(area);
            this.chart.Legends.Add(legend);

            // 라이브 이미지
            this.pnlImage.Dock = DockStyle.Fill;
            this.pnlImage.Padding = new Padding(3, 0, 3, 0);
            this.pnlImage.Controls.Add(this.camView);
            this.pnlImage.Controls.Add(this.lblHdrImg);
            this.lblHdrImg.Dock = DockStyle.Top;
            this.lblHdrImg.Height = 28;
            this.lblHdrImg.Text = "라이브 이미지 (현재 카메라 · Grab/Live)";
            this.lblHdrImg.BackColor = UiTheme.StatusBarBg;
            this.lblHdrImg.ForeColor = UiTheme.StatusBarFg;
            this.lblHdrImg.Font = UiTheme.SectionFont;
            this.lblHdrImg.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrImg.Padding = new Padding(10, 0, 0, 0);
            this.camView.Dock = DockStyle.Fill;
            this.camView.BackColor = Color.Black;
            this.camView.ShowToolbar = true;

            // 버튼 열(20%) — ROI 설정 + 테스트 (아이콘 없는 표준 버튼)
            this.pnlButtons.Dock = DockStyle.Fill;
            this.pnlButtons.FlowDirection = FlowDirection.TopDown;
            this.pnlButtons.WrapContents = false;
            this.pnlButtons.AutoScroll = true;
            this.pnlButtons.BackColor = UiTheme.OptionPanelBg;
            this.pnlButtons.Padding = new Padding(4, 0, 4, 6);
            this.pnlButtons.Controls.Add(this.lblRoiHdr);
            this.pnlButtons.Controls.Add(this.btnRoi0);
            this.pnlButtons.Controls.Add(this.btnRoi1);
            this.pnlButtons.Controls.Add(this.btnRoi2);
            this.pnlButtons.Controls.Add(this.btnRoi3);
            this.pnlButtons.Controls.Add(this.btnRoiJog);
            this.pnlButtons.Controls.Add(this.btnProcImg);
            this.pnlButtons.Controls.Add(this.btnRoiClear);
            this.pnlButtons.Controls.Add(this.lblTestHdr);
            this.pnlButtons.Controls.Add(this.btnTestScan);
            this.pnlButtons.Controls.Add(this.btnTcpScan);
            this.pnlButtons.Controls.Add(this.btnTcpVal);
            this.pnlButtons.Controls.Add(this.btnImgSeq);
            this.pnlButtons.Controls.Add(this.btnTestStep);
            this.pnlButtons.Controls.Add(this.btnReset);
            this.pnlButtons.Controls.Add(this.btnClearLog);

            this.lblRoiHdr.Text = "ROI 설정";
            this.lblRoiHdr.Width = 150;
            this.lblRoiHdr.Height = 24;
            this.lblRoiHdr.Margin = new Padding(0, 2, 0, 4);
            this.lblRoiHdr.TextAlign = ContentAlignment.MiddleLeft;
            this.lblRoiHdr.Font = UiTheme.SectionFont;
            this.lblRoiHdr.ForeColor = UiTheme.SidebarHeaderFg;
            this.lblRoiHdr.BackColor = UiTheme.SidebarHeaderBg;
            this.lblRoiHdr.Padding = new Padding(8, 0, 0, 0);
            this.lblTestHdr.Text = "테스트";
            this.lblTestHdr.Width = 150;
            this.lblTestHdr.Height = 24;
            this.lblTestHdr.Margin = new Padding(0, 2, 0, 4);
            this.lblTestHdr.TextAlign = ContentAlignment.MiddleLeft;
            this.lblTestHdr.Font = UiTheme.SectionFont;
            this.lblTestHdr.ForeColor = UiTheme.SidebarHeaderFg;
            this.lblTestHdr.BackColor = UiTheme.SidebarHeaderBg;
            this.lblTestHdr.Padding = new Padding(8, 0, 0, 0);
            this.lblTestHdr.Margin = new Padding(0, 10, 0, 4);

            this.btnRoi0.Text = "ROI 1 지정";
            this.btnRoi0.Width = 150;
            this.btnRoi0.Height = 30;
            this.btnRoi0.Margin = new Padding(0, 0, 0, 3);
            this.btnRoi0.FlatStyle = FlatStyle.Flat;
            this.btnRoi0.Font = UiTheme.ButtonFont;
            this.btnRoi0.BackColor = Color.White;
            this.btnRoi0.ForeColor = Color.Red;
            this.btnRoi0.TextAlign = ContentAlignment.MiddleLeft;
            this.btnRoi1.Text = "ROI 2 지정";
            this.btnRoi1.Width = 150;
            this.btnRoi1.Height = 30;
            this.btnRoi1.Margin = new Padding(0, 0, 0, 3);
            this.btnRoi1.FlatStyle = FlatStyle.Flat;
            this.btnRoi1.Font = UiTheme.ButtonFont;
            this.btnRoi1.BackColor = Color.White;
            this.btnRoi1.ForeColor = Color.Goldenrod;
            this.btnRoi1.TextAlign = ContentAlignment.MiddleLeft;
            this.btnRoi2.Text = "ROI 3 지정";
            this.btnRoi2.Width = 150;
            this.btnRoi2.Height = 30;
            this.btnRoi2.Margin = new Padding(0, 0, 0, 3);
            this.btnRoi2.FlatStyle = FlatStyle.Flat;
            this.btnRoi2.Font = UiTheme.ButtonFont;
            this.btnRoi2.BackColor = Color.White;
            this.btnRoi2.ForeColor = Color.RoyalBlue;
            this.btnRoi2.TextAlign = ContentAlignment.MiddleLeft;
            this.btnRoi3.Text = "ROI 4 지정";
            this.btnRoi3.Width = 150;
            this.btnRoi3.Height = 30;
            this.btnRoi3.Margin = new Padding(0, 0, 0, 3);
            this.btnRoi3.FlatStyle = FlatStyle.Flat;
            this.btnRoi3.Font = UiTheme.ButtonFont;
            this.btnRoi3.BackColor = Color.White;
            this.btnRoi3.ForeColor = Color.ForestGreen;
            this.btnRoi3.TextAlign = ContentAlignment.MiddleLeft;
            this.btnRoiJog.Text = "ROI 조그/크기";
            this.btnRoiJog.Width = 150;
            this.btnRoiJog.Height = 30;
            this.btnRoiJog.Margin = new Padding(0, 0, 0, 3);
            this.btnRoiJog.FlatStyle = FlatStyle.Flat;
            this.btnRoiJog.Font = UiTheme.ButtonFont;
            this.btnRoiJog.BackColor = Color.White;
            this.btnRoiJog.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnRoiJog.TextAlign = ContentAlignment.MiddleLeft;
            this.btnProcImg.Text = "처리 이미지 보기";
            this.btnProcImg.Width = 150;
            this.btnProcImg.Height = 30;
            this.btnProcImg.Margin = new Padding(0, 0, 0, 3);
            this.btnProcImg.FlatStyle = FlatStyle.Flat;
            this.btnProcImg.Font = UiTheme.ButtonFont;
            this.btnProcImg.BackColor = Color.White;
            this.btnProcImg.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnProcImg.TextAlign = ContentAlignment.MiddleLeft;
            this.btnRoiClear.Text = "ROI 전체 지우기";
            this.btnRoiClear.Width = 150;
            this.btnRoiClear.Height = 30;
            this.btnRoiClear.Margin = new Padding(0, 0, 0, 3);
            this.btnRoiClear.FlatStyle = FlatStyle.Flat;
            this.btnRoiClear.Font = UiTheme.ButtonFont;
            this.btnRoiClear.BackColor = Color.White;
            this.btnRoiClear.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnRoiClear.TextAlign = ContentAlignment.MiddleLeft;
            this.btnTestScan.Text = "테스트 스캔 (로컬)";
            this.btnTestScan.Width = 150;
            this.btnTestScan.Height = 30;
            this.btnTestScan.Margin = new Padding(0, 0, 0, 3);
            this.btnTestScan.FlatStyle = FlatStyle.Flat;
            this.btnTestScan.Font = UiTheme.ButtonFont;
            this.btnTestScan.BackColor = Color.White;
            this.btnTestScan.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnTestScan.TextAlign = ContentAlignment.MiddleLeft;
            this.btnTcpScan.Text = "TCP 스캔 (실통신)";
            this.btnTcpScan.Width = 150;
            this.btnTcpScan.Height = 30;
            this.btnTcpScan.Margin = new Padding(0, 0, 0, 3);
            this.btnTcpScan.FlatStyle = FlatStyle.Flat;
            this.btnTcpScan.Font = UiTheme.ButtonFont;
            this.btnTcpScan.BackColor = Color.White;
            this.btnTcpScan.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnTcpScan.TextAlign = ContentAlignment.MiddleLeft;
            this.btnTcpVal.Text = "TCP 스텝 (Z+0.2)";
            this.btnTcpVal.Width = 150;
            this.btnTcpVal.Height = 30;
            this.btnTcpVal.Margin = new Padding(0, 0, 0, 3);
            this.btnTcpVal.FlatStyle = FlatStyle.Flat;
            this.btnTcpVal.Font = UiTheme.ButtonFont;
            this.btnTcpVal.BackColor = Color.White;
            this.btnTcpVal.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnTcpVal.TextAlign = ContentAlignment.MiddleLeft;
            this.btnImgSeq.Text = "이미지 시퀀스 (텍타임)";
            this.btnImgSeq.Width = 150;
            this.btnImgSeq.Height = 30;
            this.btnImgSeq.Margin = new Padding(0, 0, 0, 3);
            this.btnImgSeq.FlatStyle = FlatStyle.Flat;
            this.btnImgSeq.Font = UiTheme.ButtonFont;
            this.btnImgSeq.BackColor = Color.White;
            this.btnImgSeq.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnImgSeq.TextAlign = ContentAlignment.MiddleLeft;
            this.btnTestStep.Text = "1점 추가 (로컬)";
            this.btnTestStep.Width = 150;
            this.btnTestStep.Height = 30;
            this.btnTestStep.Margin = new Padding(0, 0, 0, 3);
            this.btnTestStep.FlatStyle = FlatStyle.Flat;
            this.btnTestStep.Font = UiTheme.ButtonFont;
            this.btnTestStep.BackColor = Color.White;
            this.btnTestStep.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnTestStep.TextAlign = ContentAlignment.MiddleLeft;
            this.btnReset.Text = "세션 리셋";
            this.btnReset.Width = 150;
            this.btnReset.Height = 30;
            this.btnReset.Margin = new Padding(0, 0, 0, 3);
            this.btnReset.FlatStyle = FlatStyle.Flat;
            this.btnReset.Font = UiTheme.ButtonFont;
            this.btnReset.BackColor = Color.White;
            this.btnReset.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnReset.TextAlign = ContentAlignment.MiddleLeft;
            this.btnClearLog.Text = "로그 지우기";
            this.btnClearLog.Width = 150;
            this.btnClearLog.Height = 30;
            this.btnClearLog.Margin = new Padding(0, 0, 0, 3);
            this.btnClearLog.FlatStyle = FlatStyle.Flat;
            this.btnClearLog.Font = UiTheme.ButtonFont;
            this.btnClearLog.BackColor = Color.White;
            this.btnClearLog.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnClearLog.TextAlign = ContentAlignment.MiddleLeft;

            // timer
            this.timer = new Timer(this.components);
            this.timer.Interval = 400;

            // this
            this.Controls.Add(this.pnlChart);
            this.Controls.Add(this.pnlTop);
            this.Controls.Add(this.pnlNav);
            this.Name = "AutoFocusPanel";
            this.Size = new Size(1100, 680);

            ((ISupportInitialize)(this.grid)).EndInit();
            ((ISupportInitialize)(this.chart)).EndInit();
            this.flowNav.ResumeLayout(false);
            this.pnlNav.ResumeLayout(false);
            this.pnlComm.ResumeLayout(false);
            this.pnlTactBox.ResumeLayout(false);
            this.logSplit.ResumeLayout(false);
            this.pnlBest.ResumeLayout(false);
            this.pnlLog.ResumeLayout(false);
            this.pnlChartSide.ResumeLayout(false);
            this.pnlImage.ResumeLayout(false);
            this.pnlButtons.ResumeLayout(false);
            this.chartSplit.ResumeLayout(false);
            this.pnlChart.ResumeLayout(false);
            this.pnlTop.ResumeLayout(false);
            this.ResumeLayout(false);
        }

    }
}
