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

        // 좌: 카메라(축소) + FOCUS 통신 로그
        private TableLayoutPanel _main;
        private TableLayoutPanel _left;
        private Label lblHdrImg;
        private CameraView camView;
        private TextBox txtFocusLog;

        // 중: 동작 버튼 그리드 + 포커스 곡선 + ROI별 값 그리드
        private TableLayoutPanel _center;
        private Label lblHdrAction;
        private TableLayoutPanel _actionPanel;
        private Button btnTargetCollet;
        private Button btnTargetDie;
        private Button btnRoi0;
        private Button btnRoi1;
        private Button btnRoi2;
        private Button btnRoi3;
        private Button btnRoiClear;
        private Button btnMeasure;
        private Label lblHdrChart;
        private Chart chart;
        private Label lblHdrGrid;
        private DataGridView grid;

        // 우: PARAMETERS(ParameterGridControl) + 검사 조명
        private TableLayoutPanel _right;
        private Label lblHdrParam;
        private ParameterGridControl _params;
        private Label lblHdrLight;
        private Panel pnlLightHost;

        private Timer timerRefresh;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new Container();
            this._main = new TableLayoutPanel();
            this._left = new TableLayoutPanel();
            this.lblHdrImg = new Label();
            this.camView = new CameraView();
            this.txtFocusLog = new TextBox();
            this._center = new TableLayoutPanel();
            this.lblHdrAction = new Label();
            this._actionPanel = new TableLayoutPanel();
            this.btnTargetCollet = new Button();
            this.btnTargetDie = new Button();
            this.btnRoi0 = new Button();
            this.btnRoi1 = new Button();
            this.btnRoi2 = new Button();
            this.btnRoi3 = new Button();
            this.btnRoiClear = new Button();
            this.btnMeasure = new Button();
            this.lblHdrChart = new Label();
            this.chart = new Chart();
            this.lblHdrGrid = new Label();
            this.grid = new DataGridView();
            this._right = new TableLayoutPanel();
            this.lblHdrParam = new Label();
            this._params = new ParameterGridControl();
            this.lblHdrLight = new Label();
            this.pnlLightHost = new Panel();
            this.timerRefresh = new Timer(this.components);

            Color hdrBg = Color.FromArgb(217, 119, 6);
            Font hdrFont = new Font("맑은 고딕", 11F, FontStyle.Bold);
            Color primaryBg = Color.FromArgb(232, 93, 26);

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
            this._main.SuspendLayout();
            this._left.SuspendLayout();
            this._center.SuspendLayout();
            this._actionPanel.SuspendLayout();
            this._right.SuspendLayout();
            this.SuspendLayout();

            // ── _main: 좌(42%) / 중(30%) / 우(380px) ──
            this._main.Dock = DockStyle.Fill;
            this._main.ColumnCount = 3;
            this._main.RowCount = 1;
            this._main.Margin = new Padding(0);
            this._main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            this._main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            this._main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380F));
            this._main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._main.Controls.Add(this._left, 0, 0);
            this._main.Controls.Add(this._center, 1, 0);
            this._main.Controls.Add(this._right, 2, 0);

            // ── 좌: 카메라 + FOCUS 로그 ──
            this._left.Dock = DockStyle.Fill;
            this._left.ColumnCount = 1;
            this._left.RowCount = 3;
            this._left.Margin = new Padding(0);
            this._left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._left.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            this._left.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._left.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));
            this._left.Controls.Add(this.lblHdrImg, 0, 0);
            this._left.Controls.Add(this.camView, 0, 1);
            this._left.Controls.Add(this.txtFocusLog, 0, 2);

            this.lblHdrImg.Dock = DockStyle.Fill;
            this.lblHdrImg.BackColor = hdrBg;
            this.lblHdrImg.ForeColor = Color.White;
            this.lblHdrImg.Font = hdrFont;
            this.lblHdrImg.Padding = new Padding(8, 0, 0, 0);
            this.lblHdrImg.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrImg.Text = "카메라 이미지 (Grab/Live · ROI 드래그 지정 · 프로토콜 그랩 자동 표시)";

            this.camView.Dock = DockStyle.Fill;
            this.camView.Margin = new Padding(0);
            this.camView.BackColor = Color.Black;
            this.camView.ShowToolbar = true;

            this.txtFocusLog.Dock = DockStyle.Fill;
            this.txtFocusLog.Margin = new Padding(0, 2, 0, 0);
            this.txtFocusLog.Multiline = true;
            this.txtFocusLog.ReadOnly = true;
            this.txtFocusLog.WordWrap = false;
            this.txtFocusLog.ScrollBars = ScrollBars.Both;
            this.txtFocusLog.BackColor = UiTheme.VisionBg;
            this.txtFocusLog.ForeColor = UiTheme.VisionInfoFg;
            this.txtFocusLog.BorderStyle = BorderStyle.None;
            this.txtFocusLog.Font = new Font("Consolas", 9F);

            // ── 중: 동작 그리드 + 곡선 + 그리드 ──
            this._center.Dock = DockStyle.Fill;
            this._center.ColumnCount = 1;
            this._center.RowCount = 6;
            this._center.Margin = new Padding(0);
            this._center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 200F));
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            this._center.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            this._center.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            this._center.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            this._center.Controls.Add(this.lblHdrAction, 0, 0);
            this._center.Controls.Add(this._actionPanel, 0, 1);
            this._center.Controls.Add(this.lblHdrChart, 0, 2);
            this._center.Controls.Add(this.chart, 0, 3);
            this._center.Controls.Add(this.lblHdrGrid, 0, 4);
            this._center.Controls.Add(this.grid, 0, 5);

            this.lblHdrAction.Dock = DockStyle.Fill;
            this.lblHdrAction.BackColor = hdrBg;
            this.lblHdrAction.ForeColor = Color.White;
            this.lblHdrAction.Font = hdrFont;
            this.lblHdrAction.Padding = new Padding(8, 0, 0, 0);
            this.lblHdrAction.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrAction.Text = "동작";

            // 동작 버튼 그리드 — 2열 × 4행(다른 레시피 ACTION 패널과 동일한 그리드 정렬).
            this._actionPanel.Dock = DockStyle.Fill;
            this._actionPanel.Margin = new Padding(0);
            this._actionPanel.BackColor = UiTheme.SidebarBg;
            this._actionPanel.Padding = new Padding(3, 3, 3, 3);
            this._actionPanel.ColumnCount = 2;
            this._actionPanel.RowCount = 4;
            this._actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this._actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            this._actionPanel.Controls.Add(this.btnTargetCollet, 0, 0);
            this._actionPanel.Controls.Add(this.btnTargetDie, 1, 0);
            this._actionPanel.Controls.Add(this.btnRoi0, 0, 1);
            this._actionPanel.Controls.Add(this.btnRoi1, 1, 1);
            this._actionPanel.Controls.Add(this.btnRoi2, 0, 2);
            this._actionPanel.Controls.Add(this.btnRoi3, 1, 2);
            this._actionPanel.Controls.Add(this.btnRoiClear, 0, 3);
            this._actionPanel.Controls.Add(this.btnMeasure, 1, 3);

            this.btnTargetCollet.Text = "콜렛";
            this.btnTargetCollet.Dock = DockStyle.Fill;
            this.btnTargetCollet.Margin = new Padding(3);
            this.btnTargetCollet.FlatStyle = FlatStyle.Flat;
            this.btnTargetCollet.Font = UiTheme.ButtonFont;
            this.btnTargetCollet.BackColor = Color.White;
            this.btnTargetCollet.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnTargetDie.Text = "다이";
            this.btnTargetDie.Dock = DockStyle.Fill;
            this.btnTargetDie.Margin = new Padding(3);
            this.btnTargetDie.FlatStyle = FlatStyle.Flat;
            this.btnTargetDie.Font = UiTheme.ButtonFont;
            this.btnTargetDie.BackColor = Color.White;
            this.btnTargetDie.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnRoi0.Text = "ROI 1 지정";
            this.btnRoi0.Dock = DockStyle.Fill;
            this.btnRoi0.Margin = new Padding(3);
            this.btnRoi0.FlatStyle = FlatStyle.Flat;
            this.btnRoi0.Font = UiTheme.ButtonFont;
            this.btnRoi0.BackColor = Color.White;
            this.btnRoi0.ForeColor = Color.Red;
            this.btnRoi1.Text = "ROI 2 지정";
            this.btnRoi1.Dock = DockStyle.Fill;
            this.btnRoi1.Margin = new Padding(3);
            this.btnRoi1.FlatStyle = FlatStyle.Flat;
            this.btnRoi1.Font = UiTheme.ButtonFont;
            this.btnRoi1.BackColor = Color.White;
            this.btnRoi1.ForeColor = Color.Goldenrod;
            this.btnRoi2.Text = "ROI 3 지정";
            this.btnRoi2.Dock = DockStyle.Fill;
            this.btnRoi2.Margin = new Padding(3);
            this.btnRoi2.FlatStyle = FlatStyle.Flat;
            this.btnRoi2.Font = UiTheme.ButtonFont;
            this.btnRoi2.BackColor = Color.White;
            this.btnRoi2.ForeColor = Color.RoyalBlue;
            this.btnRoi3.Text = "ROI 4 지정";
            this.btnRoi3.Dock = DockStyle.Fill;
            this.btnRoi3.Margin = new Padding(3);
            this.btnRoi3.FlatStyle = FlatStyle.Flat;
            this.btnRoi3.Font = UiTheme.ButtonFont;
            this.btnRoi3.BackColor = Color.White;
            this.btnRoi3.ForeColor = Color.ForestGreen;
            this.btnRoiClear.Text = "ROI 전체 지우기";
            this.btnRoiClear.Dock = DockStyle.Fill;
            this.btnRoiClear.Margin = new Padding(3);
            this.btnRoiClear.FlatStyle = FlatStyle.Flat;
            this.btnRoiClear.Font = UiTheme.ButtonFont;
            this.btnRoiClear.BackColor = Color.White;
            this.btnRoiClear.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);
            this.btnMeasure.Text = "포커스 측정";
            this.btnMeasure.Dock = DockStyle.Fill;
            this.btnMeasure.Margin = new Padding(3);
            this.btnMeasure.FlatStyle = FlatStyle.Flat;
            this.btnMeasure.Font = UiTheme.ButtonFont;
            this.btnMeasure.BackColor = primaryBg;
            this.btnMeasure.ForeColor = Color.White;

            this.lblHdrChart.Dock = DockStyle.Fill;
            this.lblHdrChart.BackColor = hdrBg;
            this.lblHdrChart.ForeColor = Color.White;
            this.lblHdrChart.Font = hdrFont;
            this.lblHdrChart.Padding = new Padding(8, 0, 0, 0);
            this.lblHdrChart.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrChart.Text = "포커스 곡선 (X=모터 Z, Y=Score · ★=Best)";

            this.chart.Dock = DockStyle.Fill;
            this.chart.Margin = new Padding(0);
            this.chart.MinimumSize = new Size(1, 1);
            this.chart.BackColor = Color.White;
            this.chart.ChartAreas.Add(area);
            this.chart.Legends.Add(legend);

            this.lblHdrGrid.Dock = DockStyle.Fill;
            this.lblHdrGrid.BackColor = hdrBg;
            this.lblHdrGrid.ForeColor = Color.White;
            this.lblHdrGrid.Font = hdrFont;
            this.lblHdrGrid.Padding = new Padding(8, 0, 0, 0);
            this.lblHdrGrid.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrGrid.Text = "ROI별 포커스 값 (측정값 / Best)";

            this.grid.Dock = DockStyle.Fill;
            this.grid.Margin = new Padding(0);
            this.grid.ReadOnly = true;
            this.grid.AllowUserToAddRows = false;
            this.grid.RowHeadersVisible = false;
            this.grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // ── 우: PARAMETERS + 검사 조명 ──
            this._right.Dock = DockStyle.Fill;
            this._right.ColumnCount = 1;
            this._right.RowCount = 4;
            this._right.Margin = new Padding(0);
            this._right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            this._right.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 440F));
            this._right.Controls.Add(this.lblHdrParam, 0, 0);
            this._right.Controls.Add(this._params, 0, 1);
            this._right.Controls.Add(this.lblHdrLight, 0, 2);
            this._right.Controls.Add(this.pnlLightHost, 0, 3);

            this.lblHdrParam.Dock = DockStyle.Fill;
            this.lblHdrParam.BackColor = hdrBg;
            this.lblHdrParam.ForeColor = Color.White;
            this.lblHdrParam.Font = hdrFont;
            this.lblHdrParam.Padding = new Padding(8, 0, 0, 0);
            this.lblHdrParam.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrParam.Text = "PARAMETERS";

            this._params.Dock = DockStyle.Fill;
            this._params.Margin = new Padding(0);
            this._params.BackColor = Color.FromArgb(245, 246, 248);

            this.lblHdrLight.Dock = DockStyle.Fill;
            this.lblHdrLight.BackColor = hdrBg;
            this.lblHdrLight.ForeColor = Color.White;
            this.lblHdrLight.Font = hdrFont;
            this.lblHdrLight.Padding = new Padding(8, 0, 0, 0);
            this.lblHdrLight.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrLight.Text = "검사 조명";

            this.pnlLightHost.Dock = DockStyle.Fill;
            this.pnlLightHost.Margin = new Padding(0);
            this.pnlLightHost.BackColor = Color.FromArgb(191, 191, 191);

            this.timerRefresh.Interval = 400;

            // this
            this.Controls.Add(this._main);
            this.Name = "FocusTargetPage";
            this.Size = new Size(1710, 832);

            ((ISupportInitialize)(this.grid)).EndInit();
            ((ISupportInitialize)(this.chart)).EndInit();
            this._actionPanel.ResumeLayout(false);
            this._center.ResumeLayout(false);
            this._left.ResumeLayout(false);
            this._right.ResumeLayout(false);
            this._main.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
