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

            this.lblTargetHdr.Text = "포커스 대상";
            this.lblTargetHdr.Width = 180;
            this.lblTargetHdr.Height = 24;
            this.lblTargetHdr.Margin = new Padding(0, 2, 0, 4);
            this.lblTargetHdr.TextAlign = ContentAlignment.MiddleLeft;
            this.lblTargetHdr.Font = UiTheme.SectionFont;
            this.lblTargetHdr.ForeColor = UiTheme.SidebarHeaderFg;
            this.lblTargetHdr.BackColor = UiTheme.SidebarHeaderBg;
            this.lblTargetHdr.Padding = new Padding(8, 0, 0, 0);
            this.btnTargetCollet.Text = "콜렛";
            this.btnTargetCollet.Width = 180;
            this.btnTargetCollet.Height = 30;
            this.btnTargetCollet.Margin = new Padding(0, 0, 0, 3);
            this.btnTargetCollet.FlatStyle = FlatStyle.Flat;
            this.btnTargetCollet.Font = UiTheme.ButtonFont;
            this.btnTargetCollet.BackColor = Color.White;
            this.btnTargetCollet.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnTargetCollet.TextAlign = ContentAlignment.MiddleLeft;
            this.btnTargetDie.Text = "다이";
            this.btnTargetDie.Width = 180;
            this.btnTargetDie.Height = 30;
            this.btnTargetDie.Margin = new Padding(0, 0, 0, 3);
            this.btnTargetDie.FlatStyle = FlatStyle.Flat;
            this.btnTargetDie.Font = UiTheme.ButtonFont;
            this.btnTargetDie.BackColor = Color.White;
            this.btnTargetDie.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnTargetDie.TextAlign = ContentAlignment.MiddleLeft;
            this.lblRoiHdr.Text = "ROI 설정";
            this.lblRoiHdr.Width = 180;
            this.lblRoiHdr.Height = 24;
            this.lblRoiHdr.Margin = new Padding(0, 2, 0, 4);
            this.lblRoiHdr.TextAlign = ContentAlignment.MiddleLeft;
            this.lblRoiHdr.Font = UiTheme.SectionFont;
            this.lblRoiHdr.ForeColor = UiTheme.SidebarHeaderFg;
            this.lblRoiHdr.BackColor = UiTheme.SidebarHeaderBg;
            this.lblRoiHdr.Padding = new Padding(8, 0, 0, 0);
            this.lblRoiHdr.Margin = new Padding(0, 10, 0, 4);
            this.btnRoi0.Text = "ROI 1 지정";
            this.btnRoi0.Width = 180;
            this.btnRoi0.Height = 30;
            this.btnRoi0.Margin = new Padding(0, 0, 0, 3);
            this.btnRoi0.FlatStyle = FlatStyle.Flat;
            this.btnRoi0.Font = UiTheme.ButtonFont;
            this.btnRoi0.BackColor = Color.White;
            this.btnRoi0.ForeColor = Color.Red;
            this.btnRoi0.TextAlign = ContentAlignment.MiddleLeft;
            this.btnRoi1.Text = "ROI 2 지정";
            this.btnRoi1.Width = 180;
            this.btnRoi1.Height = 30;
            this.btnRoi1.Margin = new Padding(0, 0, 0, 3);
            this.btnRoi1.FlatStyle = FlatStyle.Flat;
            this.btnRoi1.Font = UiTheme.ButtonFont;
            this.btnRoi1.BackColor = Color.White;
            this.btnRoi1.ForeColor = Color.Goldenrod;
            this.btnRoi1.TextAlign = ContentAlignment.MiddleLeft;
            this.btnRoi2.Text = "ROI 3 지정";
            this.btnRoi2.Width = 180;
            this.btnRoi2.Height = 30;
            this.btnRoi2.Margin = new Padding(0, 0, 0, 3);
            this.btnRoi2.FlatStyle = FlatStyle.Flat;
            this.btnRoi2.Font = UiTheme.ButtonFont;
            this.btnRoi2.BackColor = Color.White;
            this.btnRoi2.ForeColor = Color.RoyalBlue;
            this.btnRoi2.TextAlign = ContentAlignment.MiddleLeft;
            this.btnRoi3.Text = "ROI 4 지정";
            this.btnRoi3.Width = 180;
            this.btnRoi3.Height = 30;
            this.btnRoi3.Margin = new Padding(0, 0, 0, 3);
            this.btnRoi3.FlatStyle = FlatStyle.Flat;
            this.btnRoi3.Font = UiTheme.ButtonFont;
            this.btnRoi3.BackColor = Color.White;
            this.btnRoi3.ForeColor = Color.ForestGreen;
            this.btnRoi3.TextAlign = ContentAlignment.MiddleLeft;
            this.btnRoiClear.Text = "ROI 전체 지우기";
            this.btnRoiClear.Width = 180;
            this.btnRoiClear.Height = 30;
            this.btnRoiClear.Margin = new Padding(0, 0, 0, 3);
            this.btnRoiClear.FlatStyle = FlatStyle.Flat;
            this.btnRoiClear.Font = UiTheme.ButtonFont;
            this.btnRoiClear.BackColor = Color.White;
            this.btnRoiClear.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnRoiClear.TextAlign = ContentAlignment.MiddleLeft;
            this.lblExposureHdr.Text = "카메라 노출 (µs)";
            this.lblExposureHdr.Width = 180;
            this.lblExposureHdr.Height = 24;
            this.lblExposureHdr.Margin = new Padding(0, 2, 0, 4);
            this.lblExposureHdr.TextAlign = ContentAlignment.MiddleLeft;
            this.lblExposureHdr.Font = UiTheme.SectionFont;
            this.lblExposureHdr.ForeColor = UiTheme.SidebarHeaderFg;
            this.lblExposureHdr.BackColor = UiTheme.SidebarHeaderBg;
            this.lblExposureHdr.Padding = new Padding(8, 0, 0, 0);
            this.lblExposureHdr.Margin = new Padding(0, 10, 0, 4);
            this.txtExposure.Width = 180;
            this.txtExposure.Margin = new Padding(0, 0, 0, 3);
            this.txtExposure.TextAlign = HorizontalAlignment.Right;
            this.btnExposureApply.Text = "노출 적용 (저장)";
            this.btnExposureApply.Width = 180;
            this.btnExposureApply.Height = 30;
            this.btnExposureApply.Margin = new Padding(0, 0, 0, 3);
            this.btnExposureApply.FlatStyle = FlatStyle.Flat;
            this.btnExposureApply.Font = UiTheme.ButtonFont;
            this.btnExposureApply.BackColor = Color.White;
            this.btnExposureApply.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnExposureApply.TextAlign = ContentAlignment.MiddleLeft;
            this.lblMeasureHdr.Text = "측정";
            this.lblMeasureHdr.Width = 180;
            this.lblMeasureHdr.Height = 24;
            this.lblMeasureHdr.Margin = new Padding(0, 2, 0, 4);
            this.lblMeasureHdr.TextAlign = ContentAlignment.MiddleLeft;
            this.lblMeasureHdr.Font = UiTheme.SectionFont;
            this.lblMeasureHdr.ForeColor = UiTheme.SidebarHeaderFg;
            this.lblMeasureHdr.BackColor = UiTheme.SidebarHeaderBg;
            this.lblMeasureHdr.Padding = new Padding(8, 0, 0, 0);
            this.lblMeasureHdr.Margin = new Padding(0, 10, 0, 4);
            this.btnMeasure.Text = "포커스 측정 (ROI별)";
            this.btnMeasure.Width = 180;
            this.btnMeasure.Height = 30;
            this.btnMeasure.Margin = new Padding(0, 0, 0, 3);
            this.btnMeasure.FlatStyle = FlatStyle.Flat;
            this.btnMeasure.Font = UiTheme.ButtonFont;
            this.btnMeasure.BackColor = Color.White;
            this.btnMeasure.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnMeasure.TextAlign = ContentAlignment.MiddleLeft;

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
            this.lblHdrGrid.Dock = DockStyle.Top;
            this.lblHdrGrid.Height = 28;
            this.lblHdrGrid.Text = "ROI별 포커스 값 (측정값 / Best)";
            this.lblHdrGrid.BackColor = UiTheme.StatusBarBg;
            this.lblHdrGrid.ForeColor = UiTheme.StatusBarFg;
            this.lblHdrGrid.Font = UiTheme.SectionFont;
            this.lblHdrGrid.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrGrid.Padding = new Padding(10, 0, 0, 0);
            this.grid.Dock = DockStyle.Fill;
            this.grid.ReadOnly = true;
            this.grid.AllowUserToAddRows = false;
            this.grid.RowHeadersVisible = false;
            this.grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            this.pnlChartBox.Dock = DockStyle.Fill;
            this.pnlChartBox.Padding = new Padding(0, 0, 3, 0);
            this.pnlChartBox.Controls.Add(this.chart);
            this.pnlChartBox.Controls.Add(this.lblHdrChart);
            this.lblHdrChart.Dock = DockStyle.Top;
            this.lblHdrChart.Height = 28;
            this.lblHdrChart.Text = "포커스 곡선 (X=모터 Z, Y=Score · ★=Best)";
            this.lblHdrChart.BackColor = UiTheme.StatusBarBg;
            this.lblHdrChart.ForeColor = UiTheme.StatusBarFg;
            this.lblHdrChart.Font = UiTheme.SectionFont;
            this.lblHdrChart.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrChart.Padding = new Padding(10, 0, 0, 0);
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
            this.lblHdrImg.Dock = DockStyle.Top;
            this.lblHdrImg.Height = 28;
            this.lblHdrImg.Text = "카메라 이미지 (Grab/Live · ROI 드래그 지정 · 프로토콜 그랩 자동 표시)";
            this.lblHdrImg.BackColor = UiTheme.StatusBarBg;
            this.lblHdrImg.ForeColor = UiTheme.StatusBarFg;
            this.lblHdrImg.Font = UiTheme.SectionFont;
            this.lblHdrImg.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrImg.Padding = new Padding(10, 0, 0, 0);
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



    }
}
