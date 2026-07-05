using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using QMC.Vision.Ui;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    partial class FocusTargetPage
    {
        private IContainer components = null;

        private Panel pnlRight;
        private FlowLayoutPanel flowRight;
        private Label lblTargetHdr;
        private Button btnTargetCollet;
        private Button btnTargetDie;
        private Label lblRoiHdr;
        private Button btnRoi0;
        private Button btnRoi1;
        private Button btnRoi2;
        private Button btnRoi3;
        private Button btnRoiClear;
        private Label lblMeasureHdr;
        private Button btnMeasure;
        private Label lblExposureHdr;
        private TextBox txtExposure;
        private Button btnExposureApply;

        private Panel pnlBottom;
        private TableLayoutPanel bottomSplit;
        private Panel pnlChartBox;
        private Chart chart;
        private Label lblHdrChart;
        private Panel pnlLightHost;
        private Panel pnlGridBox;
        private DataGridView grid;
        private Label lblHdrGrid;

        private Panel pnlImage;
        private CameraView camView;
        private Label lblHdrImg;
        private TextBox txtFocusLog;

        private Timer timerRefresh;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new Container();
            this.pnlRight = new Panel();
            this.flowRight = new FlowLayoutPanel();
            this.lblTargetHdr = new Label();
            this.btnTargetCollet = new Button();
            this.btnTargetDie = new Button();
            this.lblRoiHdr = new Label();
            this.btnRoi0 = new Button();
            this.btnRoi1 = new Button();
            this.btnRoi2 = new Button();
            this.btnRoi3 = new Button();
            this.btnRoiClear = new Button();
            this.lblMeasureHdr = new Label();
            this.btnMeasure = new Button();
            this.lblExposureHdr = new Label();
            this.txtExposure = new TextBox();
            this.btnExposureApply = new Button();
            this.pnlBottom = new Panel();
            this.bottomSplit = new TableLayoutPanel();
            this.pnlChartBox = new Panel();
            this.chart = new Chart();
            this.lblHdrChart = new Label();
            this.pnlLightHost = new Panel();
            this.pnlGridBox = new Panel();
            this.grid = new DataGridView();
            this.lblHdrGrid = new Label();
            this.pnlImage = new Panel();
            this.camView = new CameraView();
            this.lblHdrImg = new Label();
            this.txtFocusLog = new TextBox();
            this.timerRefresh = new Timer(this.components);

            ChartArea area = new ChartArea();
            area.Name = "main";
            area.AxisX.Title = "모터 Z (mm)";
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
            this.pnlRight.SuspendLayout();
            this.flowRight.SuspendLayout();
            this.pnlBottom.SuspendLayout();
            this.bottomSplit.SuspendLayout();
            this.pnlChartBox.SuspendLayout();
            this.pnlGridBox.SuspendLayout();
            this.pnlImage.SuspendLayout();
            this.SuspendLayout();

            // ── 우측 레일: 타깃 선택 + ROI 지정 + 측정 ──
            this.pnlRight.Dock = DockStyle.Right;
            this.pnlRight.Width = 196;
            this.pnlRight.BackColor = UiTheme.SidebarBg;
            this.pnlRight.Controls.Add(this.flowRight);

            this.flowRight.Dock = DockStyle.Fill;
            this.flowRight.FlowDirection = FlowDirection.TopDown;
            this.flowRight.WrapContents = false;
            this.flowRight.AutoScroll = true;
            this.flowRight.Padding = new Padding(4, 6, 4, 6);
            this.flowRight.Controls.Add(this.lblTargetHdr);
            this.flowRight.Controls.Add(this.btnTargetCollet);
            this.flowRight.Controls.Add(this.btnTargetDie);
            this.flowRight.Controls.Add(this.lblRoiHdr);
            this.flowRight.Controls.Add(this.btnRoi0);
            this.flowRight.Controls.Add(this.btnRoi1);
            this.flowRight.Controls.Add(this.btnRoi2);
            this.flowRight.Controls.Add(this.btnRoi3);
            this.flowRight.Controls.Add(this.btnRoiClear);
            this.flowRight.Controls.Add(this.lblExposureHdr);
            this.flowRight.Controls.Add(this.txtExposure);
            this.flowRight.Controls.Add(this.btnExposureApply);
            this.flowRight.Controls.Add(this.lblMeasureHdr);
            this.flowRight.Controls.Add(this.btnMeasure);

            SectionLabel(this.lblTargetHdr, "포커스 대상");
            Std(this.btnTargetCollet, "콜렛", Color.FromArgb(0x22, 0x22, 0x22));
            Std(this.btnTargetDie, "다이", Color.FromArgb(0x22, 0x22, 0x22));
            SectionLabel(this.lblRoiHdr, "ROI 설정");
            this.lblRoiHdr.Margin = new Padding(0, 10, 0, 4);
            Std(this.btnRoi0, "ROI 1 지정", Color.Red);
            Std(this.btnRoi1, "ROI 2 지정", Color.Goldenrod);
            Std(this.btnRoi2, "ROI 3 지정", Color.RoyalBlue);
            Std(this.btnRoi3, "ROI 4 지정", Color.ForestGreen);
            Std(this.btnRoiClear, "ROI 전체 지우기", Color.FromArgb(0x22, 0x22, 0x22));
            SectionLabel(this.lblExposureHdr, "카메라 노출 (µs)");
            this.lblExposureHdr.Margin = new Padding(0, 10, 0, 4);
            this.txtExposure.Width = 180;
            this.txtExposure.Margin = new Padding(0, 0, 0, 3);
            this.txtExposure.TextAlign = HorizontalAlignment.Right;
            Std(this.btnExposureApply, "노출 적용 (저장)", Color.FromArgb(0x22, 0x22, 0x22));
            SectionLabel(this.lblMeasureHdr, "측정");
            this.lblMeasureHdr.Margin = new Padding(0, 10, 0, 4);
            Std(this.btnMeasure, "포커스 측정 (ROI별)", Color.FromArgb(0x22, 0x22, 0x22));

            // ── 하단: 피크 곡선(좌 40%) + 검사 조명(중 30%) + ROI별 값 그리드(우 30%) ──
            // 고정폭 도킹은 좁은 화면에서 차트 폭이 0 이하로 계산되어 Chart 가 예외를 던지므로
            // 비율 분할(TableLayoutPanel)로 배치한다(AutoFocusPanel chartSplit 과 동일 패턴).
            // 높이 360 — 조명 그리드에 바텀 4행(리스광 ch2 + 엘파인 ch6~8)이 스크롤 없이 보이는 높이.
            this.pnlBottom.Dock = DockStyle.Bottom;
            this.pnlBottom.Height = 360;
            this.pnlBottom.Padding = new Padding(6, 3, 6, 6);
            this.pnlBottom.Controls.Add(this.bottomSplit);

            this.bottomSplit.Dock = DockStyle.Fill;
            this.bottomSplit.ColumnCount = 3;
            this.bottomSplit.RowCount = 1;
            this.bottomSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            this.bottomSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            this.bottomSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            this.bottomSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this.bottomSplit.Controls.Add(this.pnlChartBox, 0, 0);
            this.bottomSplit.Controls.Add(this.pnlLightHost, 1, 0);
            this.bottomSplit.Controls.Add(this.pnlGridBox, 2, 0);

            // 검사 조명 호스트 — InspectionLightPanel(자체 헤더 보유)을 로직에서 채운다(VisionTargetPage 패턴).
            this.pnlLightHost.Dock = DockStyle.Fill;
            this.pnlLightHost.Padding = new Padding(3, 0, 3, 0);

            this.pnlGridBox.Dock = DockStyle.Fill;
            this.pnlGridBox.Padding = new Padding(3, 0, 0, 0);
            this.pnlGridBox.Controls.Add(this.grid);
            this.pnlGridBox.Controls.Add(this.lblHdrGrid);
            Header(this.lblHdrGrid, "ROI별 포커스 값 (측정값 / Best)");
            this.grid.Dock = DockStyle.Fill;
            this.grid.ReadOnly = true;
            this.grid.AllowUserToAddRows = false;
            this.grid.RowHeadersVisible = false;
            this.grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            this.pnlChartBox.Dock = DockStyle.Fill;
            this.pnlChartBox.Padding = new Padding(0, 0, 3, 0);
            this.pnlChartBox.Controls.Add(this.chart);
            this.pnlChartBox.Controls.Add(this.lblHdrChart);
            Header(this.lblHdrChart, "포커스 곡선 (X=모터 Z, Y=Score · ★=Best)");
            this.chart.Dock = DockStyle.Fill;
            this.chart.MinimumSize = new Size(1, 1);   // 과도 축소 시 0px 폭 예외(Chart ArgumentException) 방지
            this.chart.BackColor = Color.White;
            this.chart.ChartAreas.Add(area);
            this.chart.Legends.Add(legend);

            // ── 중앙: 카메라 이미지(ROI 오버레이 포함) + FOCUS 통신 로그(하단 스트립) ──
            this.pnlImage.Dock = DockStyle.Fill;
            this.pnlImage.Padding = new Padding(6, 6, 6, 3);
            this.pnlImage.Controls.Add(this.camView);
            this.pnlImage.Controls.Add(this.txtFocusLog);
            this.pnlImage.Controls.Add(this.lblHdrImg);
            Header(this.lblHdrImg, "카메라 이미지 (Grab/Live · ROI 드래그 지정 · 프로토콜 그랩 자동 표시)");
            this.camView.Dock = DockStyle.Fill;
            this.camView.BackColor = Color.Black;
            this.camView.ShowToolbar = true;

            this.txtFocusLog.Dock = DockStyle.Bottom;
            this.txtFocusLog.Height = 92;
            this.txtFocusLog.Multiline = true;
            this.txtFocusLog.ReadOnly = true;
            this.txtFocusLog.WordWrap = false;
            this.txtFocusLog.ScrollBars = ScrollBars.Both;
            this.txtFocusLog.BackColor = UiTheme.VisionBg;
            this.txtFocusLog.ForeColor = UiTheme.VisionInfoFg;
            this.txtFocusLog.BorderStyle = BorderStyle.None;
            this.txtFocusLog.Font = new Font("Consolas", 9F);

            this.timerRefresh.Interval = 400;

            // this
            this.Controls.Add(this.pnlImage);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.pnlRight);
            this.Name = "FocusTargetPage";
            this.Size = new Size(1100, 680);

            ((ISupportInitialize)(this.grid)).EndInit();
            ((ISupportInitialize)(this.chart)).EndInit();
            this.flowRight.ResumeLayout(false);
            this.pnlRight.ResumeLayout(false);
            this.pnlChartBox.ResumeLayout(false);
            this.pnlGridBox.ResumeLayout(false);
            this.bottomSplit.ResumeLayout(false);
            this.pnlBottom.ResumeLayout(false);
            this.pnlImage.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        // ── 공통 스타일 헬퍼 (AutoFocusPanel 과 동일 룩) ──
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

        private static void SectionLabel(Label l, string text)
        {
            l.Text = text;
            l.Width = 180; l.Height = 24;
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
            b.Width = 180; b.Height = 30; b.Margin = new Padding(0, 0, 0, 3);
            b.FlatStyle = FlatStyle.Flat;
            b.Font = UiTheme.ButtonFont;
            b.BackColor = Color.White;
            b.ForeColor = fg;
            b.TextAlign = ContentAlignment.MiddleLeft;
        }
    }
}
