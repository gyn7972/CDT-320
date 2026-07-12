using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Ui;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    partial class ColletRotCenterPage
    {
        private IContainer components = null;

        // 좌: 카메라(누적 라이브/평균 영상 + 크로스 마크) + COC 통신 로그
        private TableLayoutPanel _main;
        private TableLayoutPanel _left;
        private Label lblHdrImg;
        private CameraView camView;
        private TextBox txtLog;

        // 우: 동작(시작/종료 + 상태) + 결과 그리드 + PARAMETERS + 검사 조명
        private TableLayoutPanel _right;
        private Label lblHdrAction;
        private TableLayoutPanel _actionPanel;
        private Button btnStart;
        private Button btnEnd;
        private Label lblState;
        private Label lblHdrResult;
        private DataGridView grid;
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
            this.txtLog = new TextBox();
            this._right = new TableLayoutPanel();
            this.lblHdrAction = new Label();
            this._actionPanel = new TableLayoutPanel();
            this.btnStart = new Button();
            this.btnEnd = new Button();
            this.lblState = new Label();
            this.lblHdrResult = new Label();
            this.grid = new DataGridView();
            this.lblHdrParam = new Label();
            this._params = new ParameterGridControl();
            this.lblHdrLight = new Label();
            this.pnlLightHost = new Panel();
            this.timerRefresh = new Timer(this.components);

            Color hdrBg = Color.FromArgb(217, 119, 6);
            Font hdrFont = new Font("맑은 고딕", 11F, FontStyle.Bold);
            Color primaryBg = Color.FromArgb(232, 93, 26);

            ((ISupportInitialize)(this.grid)).BeginInit();
            this._main.SuspendLayout();
            this._left.SuspendLayout();
            this._actionPanel.SuspendLayout();
            this._right.SuspendLayout();
            this.SuspendLayout();

            // ── _main: 좌(100%) / 우(400px) ──
            this._main.Dock = DockStyle.Fill;
            this._main.ColumnCount = 2;
            this._main.RowCount = 1;
            this._main.Margin = new Padding(0);
            this._main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 400F));
            this._main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._main.Controls.Add(this._left, 0, 0);
            this._main.Controls.Add(this._right, 1, 0);

            // ── 좌: 카메라 + COC 로그 ──
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
            this._left.Controls.Add(this.txtLog, 0, 2);

            this.lblHdrImg.Dock = DockStyle.Fill;
            this.lblHdrImg.BackColor = hdrBg;
            this.lblHdrImg.ForeColor = Color.White;
            this.lblHdrImg.Font = hdrFont;
            this.lblHdrImg.Padding = new Padding(8, 0, 0, 0);
            this.lblHdrImg.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrImg.Text = "카메라 이미지 (누적 라이브 → 종료 시 평균 영상 + 회전 중심 크로스)";

            this.camView.Dock = DockStyle.Fill;
            this.camView.Margin = new Padding(0);
            this.camView.BackColor = Color.Black;
            this.camView.ShowToolbar = true;

            this.txtLog.Dock = DockStyle.Fill;
            this.txtLog.Margin = new Padding(0, 2, 0, 0);
            this.txtLog.Multiline = true;
            this.txtLog.ReadOnly = true;
            this.txtLog.WordWrap = false;
            this.txtLog.ScrollBars = ScrollBars.Both;
            this.txtLog.BackColor = UiTheme.VisionBg;
            this.txtLog.ForeColor = UiTheme.VisionInfoFg;
            this.txtLog.BorderStyle = BorderStyle.None;
            this.txtLog.Font = new Font("Consolas", 9F);

            // ── 우: 동작 + 결과 + PARAMETERS + 조명 ──
            this._right.Dock = DockStyle.Fill;
            this._right.ColumnCount = 1;
            this._right.RowCount = 8;
            this._right.Margin = new Padding(0);
            this._right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));    // 0: 동작 헤더
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));   // 1: 동작 패널
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));    // 2: 결과 헤더
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));   // 3: 결과 그리드
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));    // 4: PARAM 헤더(코드에서 0 으로 접힘)
            this._right.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));    // 5: 파라미터
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));    // 6: 조명 헤더
            this._right.RowStyles.Add(new RowStyle(SizeType.Absolute, 440F));   // 7: 조명 패널
            this._right.Controls.Add(this.lblHdrAction, 0, 0);
            this._right.Controls.Add(this._actionPanel, 0, 1);
            this._right.Controls.Add(this.lblHdrResult, 0, 2);
            this._right.Controls.Add(this.grid, 0, 3);
            this._right.Controls.Add(this.lblHdrParam, 0, 4);
            this._right.Controls.Add(this._params, 0, 5);
            this._right.Controls.Add(this.lblHdrLight, 0, 6);
            this._right.Controls.Add(this.pnlLightHost, 0, 7);

            this.lblHdrAction.Dock = DockStyle.Fill;
            this.lblHdrAction.BackColor = hdrBg;
            this.lblHdrAction.ForeColor = Color.White;
            this.lblHdrAction.Font = hdrFont;
            this.lblHdrAction.Padding = new Padding(8, 0, 0, 0);
            this.lblHdrAction.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrAction.Text = "동작 (COC — 콜렛 회전 중심)";

            // 동작 패널 — 1행 버튼 2개 + 1행 상태 라벨.
            this._actionPanel.Dock = DockStyle.Fill;
            this._actionPanel.Margin = new Padding(0);
            this._actionPanel.BackColor = UiTheme.SidebarBg;
            this._actionPanel.Padding = new Padding(3, 3, 3, 3);
            this._actionPanel.ColumnCount = 2;
            this._actionPanel.RowCount = 2;
            this._actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this._actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            this._actionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            this._actionPanel.Controls.Add(this.btnStart, 0, 0);
            this._actionPanel.Controls.Add(this.btnEnd, 1, 0);
            this._actionPanel.Controls.Add(this.lblState, 0, 1);
            this._actionPanel.SetColumnSpan(this.lblState, 2);

            this.btnStart.Text = "누적 시작";
            this.btnStart.Dock = DockStyle.Fill;
            this.btnStart.Margin = new Padding(3);
            this.btnStart.FlatStyle = FlatStyle.Flat;
            this.btnStart.Font = UiTheme.ButtonFont;
            this.btnStart.BackColor = Color.White;
            this.btnStart.ForeColor = Color.FromArgb(0x22, 0x22, 0x22);

            this.btnEnd.Text = "종료·중심 계산";
            this.btnEnd.Dock = DockStyle.Fill;
            this.btnEnd.Margin = new Padding(3);
            this.btnEnd.FlatStyle = FlatStyle.Flat;
            this.btnEnd.Font = UiTheme.ButtonFont;
            this.btnEnd.BackColor = primaryBg;
            this.btnEnd.ForeColor = Color.White;

            this.lblState.Dock = DockStyle.Fill;
            this.lblState.Margin = new Padding(6, 0, 3, 0);
            this.lblState.Font = UiTheme.ButtonFont;
            this.lblState.TextAlign = ContentAlignment.MiddleLeft;
            this.lblState.Text = "대기";

            this.lblHdrResult.Dock = DockStyle.Fill;
            this.lblHdrResult.BackColor = hdrBg;
            this.lblHdrResult.ForeColor = Color.White;
            this.lblHdrResult.Font = hdrFont;
            this.lblHdrResult.Padding = new Padding(8, 0, 0, 0);
            this.lblHdrResult.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHdrResult.Text = "결과 (회전 중심)";

            this.grid.Dock = DockStyle.Fill;
            this.grid.Margin = new Padding(0);
            this.grid.ReadOnly = true;
            this.grid.AllowUserToAddRows = false;
            this.grid.RowHeadersVisible = false;
            this.grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

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
            this.Name = "ColletRotCenterPage";
            this.Size = new Size(1710, 832);

            ((ISupportInitialize)(this.grid)).EndInit();
            this._actionPanel.ResumeLayout(false);
            this._left.ResumeLayout(false);
            this._right.ResumeLayout(false);
            this._main.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
