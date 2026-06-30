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
            Header(this.lblHdrBest, "BEST — ROI별 최적 초점 (위치 / Score)");
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
            Header(this.lblHdrLog, "통신 로그 (FOCUS TX / RX / EPD / ARM)");
            LogBox(this.txtLog);

            this.pnlTactBox.Dock = DockStyle.Fill;
            this.pnlTactBox.Padding = new Padding(0, 3, 0, 0);
            this.pnlTactBox.Controls.Add(this.txtTact);
            this.pnlTactBox.Controls.Add(this.lblHdrTact);
            Header(this.lblHdrTact, "Tact Time (사이클 / 스텝)");
            LogBox(this.txtTact);

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
            Header(this.lblHdrChart, "포커스 곡선 (X=모터 위치, Y=Score · ★=Best)");
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
            Header(this.lblHdrImg, "라이브 이미지 (현재 카메라 · Grab/Live)");
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

            SectionLabel(this.lblRoiHdr, "ROI 설정");
            SectionLabel(this.lblTestHdr, "테스트");
            this.lblTestHdr.Margin = new Padding(0, 10, 0, 4);

            Std(this.btnRoi0, "ROI 1 지정", Color.Red);
            Std(this.btnRoi1, "ROI 2 지정", Color.Goldenrod);
            Std(this.btnRoi2, "ROI 3 지정", Color.RoyalBlue);
            Std(this.btnRoi3, "ROI 4 지정", Color.ForestGreen);
            Std(this.btnRoiJog, "ROI 조그/크기", Color.FromArgb(0x22, 0x22, 0x22));
            Std(this.btnProcImg, "처리 이미지 보기", Color.FromArgb(0x22, 0x22, 0x22));
            Std(this.btnRoiClear, "ROI 전체 지우기", Color.FromArgb(0x22, 0x22, 0x22));
            Std(this.btnTestScan, "테스트 스캔 (로컬)", Color.FromArgb(0x22, 0x22, 0x22));
            Std(this.btnTcpScan, "TCP 스캔 (실통신)", Color.FromArgb(0x22, 0x22, 0x22));
            Std(this.btnTcpVal, "TCP 스텝 (Z+0.2)", Color.FromArgb(0x22, 0x22, 0x22));
            Std(this.btnImgSeq, "이미지 시퀀스 (텍타임)", Color.FromArgb(0x22, 0x22, 0x22));
            Std(this.btnTestStep, "1점 추가 (로컬)", Color.FromArgb(0x22, 0x22, 0x22));
            Std(this.btnReset, "세션 리셋", Color.FromArgb(0x22, 0x22, 0x22));
            Std(this.btnClearLog, "로그 지우기", Color.FromArgb(0x22, 0x22, 0x22));

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

        // ── 공통 스타일 헬퍼 ──
        private static void Header(Label l, string text)
        {
            l.Dock = DockStyle.Top;
            l.Height = 28;
            l.Text = text;
            l.BackColor = UiTheme.StatusBarBg;
            l.ForeColor = UiTheme.StatusBarFg;
            l.Font = UiTheme.SectionFont;
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.Padding = new Padding(10, 0, 0, 0);
        }
        private static void LogBox(TextBox t)
        {
            t.Dock = DockStyle.Fill;
            t.Multiline = true;
            t.ReadOnly = true;
            t.WordWrap = false;
            t.ScrollBars = ScrollBars.Both;
            t.BackColor = UiTheme.VisionBg;
            t.ForeColor = UiTheme.VisionInfoFg;
            t.BorderStyle = BorderStyle.None;
            t.Font = new Font("Consolas", 9F);
        }
        private static void SectionLabel(Label l, string text)
        {
            l.Text = text;
            l.Width = 150; l.Height = 24;
            l.Margin = new Padding(0, 2, 0, 4);
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.Font = UiTheme.SectionFont;
            l.ForeColor = UiTheme.SidebarHeaderFg;
            l.BackColor = UiTheme.SidebarHeaderBg;
            l.Padding = new Padding(8, 0, 0, 0);
        }
        private static void Std(Button b, string text, Color fg)
        {
            b.Text = text;
            b.Width = 150; b.Height = 30; b.Margin = new Padding(0, 0, 0, 3);
            b.FlatStyle = FlatStyle.Flat;
            b.Font = UiTheme.ButtonFont;
            b.BackColor = Color.White;
            b.ForeColor = fg;
            b.TextAlign = ContentAlignment.MiddleLeft;
        }
    }
}
