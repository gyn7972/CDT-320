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
        private Panel pnlChart;

        private Label lblNavHdr;
        private Label lblHdrBest;
        private Label lblHdrLog;
        private Label lblHdrChart;

        private SidebarButton btnNav0;
        private SidebarButton btnNav1;
        private SidebarButton btnNav2;
        private SidebarButton btnNav3;

        private Panel pnlTest;
        private Button btnTestScan;
        private Button btnTestStep;
        private Button btnReset;
        private Button btnClearLog;

        private DataGridView grid;
        private TextBox txtLog;
        private Chart chart;
        private TableLayoutPanel chartSplit;
        private Panel pnlChartSide;
        private Panel pnlImage;
        private Label lblHdrImg;
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
            this.pnlChart = new Panel();
            this.lblNavHdr = new Label();
            this.lblHdrBest = new Label();
            this.lblHdrLog = new Label();
            this.lblHdrChart = new Label();
            this.btnNav0 = new SidebarButton();
            this.btnNav1 = new SidebarButton();
            this.btnNav2 = new SidebarButton();
            this.btnNav3 = new SidebarButton();
            this.pnlTest = new Panel();
            this.btnTestScan = new Button();
            this.btnTestStep = new Button();
            this.btnReset = new Button();
            this.btnClearLog = new Button();
            this.grid = new DataGridView();
            this.txtLog = new TextBox();
            this.chart = new Chart();
            this.chartSplit = new TableLayoutPanel();
            this.pnlChartSide = new Panel();
            this.pnlImage = new Panel();
            this.lblHdrImg = new Label();
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
            this.pnlTest.SuspendLayout();
            this.pnlTop.SuspendLayout();
            this.pnlBest.SuspendLayout();
            this.pnlLog.SuspendLayout();
            this.pnlChart.SuspendLayout();
            this.chartSplit.SuspendLayout();
            this.pnlChartSide.SuspendLayout();
            this.pnlImage.SuspendLayout();
            this.SuspendLayout();

            // pnlNav (우측 서브 네비 — 레시피 Finder/Inspector 레일과 동일)
            this.pnlNav.Dock = DockStyle.Right;
            this.pnlNav.Width = 196;
            this.pnlNav.BackColor = UiTheme.SidebarBg;
            this.pnlNav.Controls.Add(this.flowNav);
            this.pnlNav.Controls.Add(this.pnlTest);
            this.pnlNav.Controls.Add(this.pnlNavHdrLine);
            this.pnlNav.Controls.Add(this.lblNavHdr);

            // pnlTest (하단 테스트/유틸 버튼)
            this.pnlTest.Dock = DockStyle.Bottom;
            this.pnlTest.Height = 172;
            this.pnlTest.BackColor = UiTheme.SidebarBg;
            this.pnlTest.Controls.Add(this.btnClearLog);
            this.pnlTest.Controls.Add(this.btnReset);
            this.pnlTest.Controls.Add(this.btnTestStep);
            this.pnlTest.Controls.Add(this.btnTestScan);
            // btnTestScan
            this.btnTestScan.Text = "▶ 테스트 스캔";
            this.btnTestScan.Location = new Point(6, 8);
            this.btnTestScan.Size = new Size(184, 32);
            this.btnTestScan.FlatStyle = FlatStyle.Flat;
            this.btnTestScan.Font = UiTheme.ButtonFont;
            this.btnTestScan.BackColor = Color.White;
            this.btnTestScan.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            // btnTestStep
            this.btnTestStep.Text = "＋ 1점 추가";
            this.btnTestStep.Location = new Point(6, 46);
            this.btnTestStep.Size = new Size(184, 32);
            this.btnTestStep.FlatStyle = FlatStyle.Flat;
            this.btnTestStep.Font = UiTheme.ButtonFont;
            this.btnTestStep.BackColor = Color.White;
            this.btnTestStep.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            // btnReset
            this.btnReset.Text = "⟲ 세션 리셋";
            this.btnReset.Location = new Point(6, 84);
            this.btnReset.Size = new Size(184, 32);
            this.btnReset.FlatStyle = FlatStyle.Flat;
            this.btnReset.Font = UiTheme.ButtonFont;
            this.btnReset.BackColor = Color.White;
            this.btnReset.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            // btnClearLog
            this.btnClearLog.Text = "🗑 통신 로그 지우기";
            this.btnClearLog.Location = new Point(6, 122);
            this.btnClearLog.Size = new Size(184, 32);
            this.btnClearLog.FlatStyle = FlatStyle.Flat;
            this.btnClearLog.Font = UiTheme.ButtonFont;
            this.btnClearLog.BackColor = Color.White;
            this.btnClearLog.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);

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

            // btnNav0
            this.btnNav0.Text = "바텀 검사 - 콜렛";
            this.btnNav0.Width = 184; this.btnNav0.Height = 38; this.btnNav0.Margin = new Padding(0, 0, 0, 3);
            // btnNav1
            this.btnNav1.Text = "바텀 검사 - 다이";
            this.btnNav1.Width = 184; this.btnNav1.Height = 38; this.btnNav1.Margin = new Padding(0, 0, 0, 3);
            // btnNav2
            this.btnNav2.Text = "앞쪽 측면 검사";
            this.btnNav2.Width = 184; this.btnNav2.Height = 38; this.btnNav2.Margin = new Padding(0, 0, 0, 3);
            // btnNav3
            this.btnNav3.Text = "뒤쪽 측면 검사";
            this.btnNav3.Width = 184; this.btnNav3.Height = 38; this.btnNav3.Margin = new Padding(0, 0, 0, 3);

            // pnlTop (상단: BEST 그리드 | 통신 로그)
            this.pnlTop.Dock = DockStyle.Top;
            this.pnlTop.Height = 300;
            this.pnlTop.Controls.Add(this.pnlLog);
            this.pnlTop.Controls.Add(this.pnlBest);

            // pnlBest (좌)
            this.pnlBest.Dock = DockStyle.Left;
            this.pnlBest.Width = 440;
            this.pnlBest.Padding = new Padding(6, 6, 3, 6);
            this.pnlBest.Controls.Add(this.grid);
            this.pnlBest.Controls.Add(this.lblHdrBest);

            // lblHdrBest
            this.lblHdrBest.Dock = DockStyle.Top;
            this.lblHdrBest.Height = 28;
            this.lblHdrBest.Text = "BEST — Pickup별 최적 초점 (위치 / Score)";
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

            // pnlLog (우)
            this.pnlLog.Dock = DockStyle.Fill;
            this.pnlLog.Padding = new Padding(3, 6, 6, 6);
            this.pnlLog.Controls.Add(this.txtLog);
            this.pnlLog.Controls.Add(this.lblHdrLog);

            // lblHdrLog
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

            // pnlChart (하단: 포커스 곡선)
            this.pnlChart.Dock = DockStyle.Fill;
            this.pnlChart.Padding = new Padding(6, 0, 6, 6);
            this.pnlChart.Controls.Add(this.chartSplit);

            // chartSplit (좌: 포커스 곡선 | 우: 라이브 이미지, 50:50)
            this.chartSplit.Dock = DockStyle.Fill;
            this.chartSplit.ColumnCount = 2;
            this.chartSplit.RowCount = 1;
            this.chartSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            this.chartSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            this.chartSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this.chartSplit.Controls.Add(this.pnlChartSide, 0, 0);
            this.chartSplit.Controls.Add(this.pnlImage, 1, 0);

            // pnlChartSide (좌)
            this.pnlChartSide.Dock = DockStyle.Fill;
            this.pnlChartSide.Padding = new Padding(0, 0, 3, 0);
            this.pnlChartSide.Controls.Add(this.chart);
            this.pnlChartSide.Controls.Add(this.lblHdrChart);
            // lblHdrChart
            this.lblHdrChart.Dock = DockStyle.Top;
            this.lblHdrChart.Height = 28;
            this.lblHdrChart.Text = "포커스 곡선 (X=모터 위치, Y=Score · ★=Best)";
            this.lblHdrChart.BackColor = UiTheme.StatusBarBg;
            this.lblHdrChart.ForeColor = UiTheme.StatusBarFg;
            this.lblHdrChart.Font = UiTheme.SectionFont;
            this.lblHdrChart.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrChart.Padding = new Padding(10, 0, 0, 0);

            this.chart.Dock = DockStyle.Fill;
            this.chart.Size = new Size(420, 300);
            this.chart.BackColor = Color.White;
            this.chart.ChartAreas.Add(area);
            this.chart.Legends.Add(legend);

            // pnlImage (우: 라이브 이미지 — 공용 CameraView 내장 툴바 Grab/Live)
            this.pnlImage.Dock = DockStyle.Fill;
            this.pnlImage.Padding = new Padding(3, 0, 0, 0);
            this.pnlImage.Controls.Add(this.camView);
            this.pnlImage.Controls.Add(this.lblHdrImg);
            // lblHdrImg
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
            this.pnlTest.ResumeLayout(false);
            this.pnlNav.ResumeLayout(false);
            this.pnlBest.ResumeLayout(false);
            this.pnlLog.ResumeLayout(false);
            this.pnlChartSide.ResumeLayout(false);
            this.pnlImage.ResumeLayout(false);
            this.chartSplit.ResumeLayout(false);
            this.pnlChart.ResumeLayout(false);
            this.pnlTop.ResumeLayout(false);
            this.ResumeLayout(false);
        }

    }
}
