using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class LogicDetailPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private FlowLayoutPanel sourceToolbar;
        private Label lblSourceCaption;
        private Label lblDataSource;
        private Button btnOpenHistory;
        private Button btnLiveView;
        private Label lblRunCaption;
        private ComboBox cmbRun;
        private Button btnCancelHistory;
        private ProgressBar progressHistory;
        private Label lblFileInfo;
        private FlowLayoutPanel toolbar;
        private Label lblCategoryCaption;
        private ComboBox cmbCategory;
        private Label lblItemCaption;
        private ComboBox cmbItemFilter;
        private Label lblChartCaption;
        private ComboBox cmbChartMode;
        private CheckBox chkAutoRefresh;
        private Button btnClearView;
        private Button btnResetChart;
        private Label lblSummary;
        private TabControl tabs;
        private TabPage tabLogic;
        private TabPage tabChart;
        private TabPage tabCycle;
        private DataGridView _grid;
        private Panel _chartHost;
        private TactTimeChartControl _timeChart;
        private Label lblStatus;

        private void InitializeComponent()
        {
            this.rootLayout = new TableLayoutPanel();
            this.lblHeader = new Label();
            this.sourceToolbar = new FlowLayoutPanel();
            this.lblSourceCaption = new Label();
            this.lblDataSource = new Label();
            this.btnOpenHistory = new Button();
            this.btnLiveView = new Button();
            this.lblRunCaption = new Label();
            this.cmbRun = new ComboBox();
            this.btnCancelHistory = new Button();
            this.progressHistory = new ProgressBar();
            this.lblFileInfo = new Label();
            this.toolbar = new FlowLayoutPanel();
            this.lblCategoryCaption = new Label();
            this.cmbCategory = new ComboBox();
            this.lblItemCaption = new Label();
            this.cmbItemFilter = new ComboBox();
            this.lblChartCaption = new Label();
            this.cmbChartMode = new ComboBox();
            this.chkAutoRefresh = new CheckBox();
            this.btnClearView = new Button();
            this.btnResetChart = new Button();
            this.lblSummary = new Label();
            this.tabs = new TabControl();
            this.tabLogic = new TabPage();
            this.tabChart = new TabPage();
            this.tabCycle = new TabPage();
            this._grid = new DataGridView();
            this._chartHost = new Panel();
            this._timeChart = new TactTimeChartControl();
            this.lblStatus = new Label();
            this.SuspendLayout();

            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.sourceToolbar, 0, 1);
            this.rootLayout.Controls.Add(this.toolbar, 0, 2);
            this.rootLayout.Controls.Add(this.lblSummary, 0, 3);
            this.rootLayout.Controls.Add(this.tabs, 0, 4);
            this.rootLayout.Controls.Add(this.lblStatus, 0, 5);
            this.rootLayout.Dock = DockStyle.Fill;
            this.rootLayout.RowCount = 6;
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

            this.lblHeader.BackColor = UiTheme.StatusBarBg;
            this.lblHeader.Dock = DockStyle.Fill;
            this.lblHeader.Font = UiTheme.SectionFont;
            this.lblHeader.ForeColor = UiTheme.StatusBarFg;
            this.lblHeader.Margin = new Padding(0);            // 헤더 바를 가장자리에 붙임(좌측 여백 제거)
            this.lblHeader.Padding = new Padding(10, 0, 0, 0);
            this.lblHeader.Tag = "i18n:wi.logic";
            this.lblHeader.Text = Lang.T("wi.logic");
            this.lblHeader.TextAlign = ContentAlignment.MiddleLeft;

            this.sourceToolbar.BackColor = Color.FromArgb(0xF8, 0xFA, 0xFC);
            this.sourceToolbar.Controls.Add(this.lblSourceCaption);
            this.sourceToolbar.Controls.Add(this.lblDataSource);
            this.sourceToolbar.Controls.Add(this.btnOpenHistory);
            this.sourceToolbar.Controls.Add(this.btnLiveView);
            this.sourceToolbar.Controls.Add(this.lblRunCaption);
            this.sourceToolbar.Controls.Add(this.cmbRun);
            this.sourceToolbar.Controls.Add(this.btnCancelHistory);
            this.sourceToolbar.Controls.Add(this.progressHistory);
            this.sourceToolbar.Controls.Add(this.lblFileInfo);
            this.sourceToolbar.Dock = DockStyle.Fill;
            this.sourceToolbar.FlowDirection = FlowDirection.LeftToRight;
            this.sourceToolbar.Padding = new Padding(8, 4, 8, 3);
            this.sourceToolbar.WrapContents = false;

            this.lblSourceCaption.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.lblSourceCaption.Margin = new Padding(0, 3, 4, 0);
            this.lblSourceCaption.Size = new Size(48, 24);
            this.lblSourceCaption.Text = "DATA";
            this.lblSourceCaption.TextAlign = ContentAlignment.MiddleLeft;

            this.lblDataSource.BackColor = Color.FromArgb(0x2F, 0x80, 0xC9);
            this.lblDataSource.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.lblDataSource.ForeColor = Color.White;
            this.lblDataSource.Margin = new Padding(0, 0, 10, 0);
            this.lblDataSource.Size = new Size(70, 26);
            this.lblDataSource.Text = "LIVE";
            this.lblDataSource.TextAlign = ContentAlignment.MiddleCenter;

            this.btnOpenHistory.BackColor = Color.FromArgb(0x4E, 0x69, 0x7A);
            this.btnOpenHistory.FlatStyle = FlatStyle.Flat;
            this.btnOpenHistory.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.btnOpenHistory.ForeColor = Color.White;
            this.btnOpenHistory.Margin = new Padding(0, 0, 6, 0);
            this.btnOpenHistory.Name = "btnOpenHistory";
            this.btnOpenHistory.Size = new Size(145, 27);
            this.btnOpenHistory.Text = "지난 기록 불러오기";
            this.btnOpenHistory.UseVisualStyleBackColor = false;
            this.btnOpenHistory.Click += new System.EventHandler(this.btnOpenHistory_Click);

            this.btnLiveView.BackColor = Color.FromArgb(0x2F, 0x80, 0xC9);
            this.btnLiveView.FlatStyle = FlatStyle.Flat;
            this.btnLiveView.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.btnLiveView.ForeColor = Color.White;
            this.btnLiveView.Margin = new Padding(0, 0, 12, 0);
            this.btnLiveView.Name = "btnLiveView";
            this.btnLiveView.Size = new Size(92, 27);
            this.btnLiveView.Text = "실시간 보기";
            this.btnLiveView.UseVisualStyleBackColor = false;
            this.btnLiveView.Click += new System.EventHandler(this.btnLiveView_Click);

            this.lblRunCaption.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.lblRunCaption.Margin = new Padding(0, 3, 4, 0);
            this.lblRunCaption.Size = new Size(36, 24);
            this.lblRunCaption.Text = "RUN";
            this.lblRunCaption.TextAlign = ContentAlignment.MiddleLeft;

            this.cmbRun.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbRun.Enabled = false;
            this.cmbRun.Font = new Font("맑은 고딕", 9F);
            this.cmbRun.Margin = new Padding(0, 0, 8, 0);
            this.cmbRun.Name = "cmbRun";
            this.cmbRun.Size = new Size(440, 23);
            this.cmbRun.SelectedIndexChanged += new System.EventHandler(this.cmbRun_SelectedIndexChanged);

            this.btnCancelHistory.BackColor = Color.FromArgb(0x99, 0x33, 0x33);
            this.btnCancelHistory.Enabled = false;
            this.btnCancelHistory.FlatStyle = FlatStyle.Flat;
            this.btnCancelHistory.Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold);
            this.btnCancelHistory.ForeColor = Color.White;
            this.btnCancelHistory.Margin = new Padding(0, 0, 6, 0);
            this.btnCancelHistory.Name = "btnCancelHistory";
            this.btnCancelHistory.Size = new Size(65, 27);
            this.btnCancelHistory.Text = "취소";
            this.btnCancelHistory.UseVisualStyleBackColor = false;
            this.btnCancelHistory.Click += new System.EventHandler(this.btnCancelHistory_Click);

            this.progressHistory.Margin = new Padding(0, 4, 8, 0);
            this.progressHistory.Name = "progressHistory";
            this.progressHistory.Size = new Size(110, 19);

            this.lblFileInfo.AutoEllipsis = true;
            this.lblFileInfo.Font = new Font("맑은 고딕", 8.5F);
            this.lblFileInfo.ForeColor = Color.DimGray;
            this.lblFileInfo.Margin = new Padding(0, 3, 0, 0);
            this.lblFileInfo.Size = new Size(370, 23);
            this.lblFileInfo.Text = "실시간 메모리 기록";
            this.lblFileInfo.TextAlign = ContentAlignment.MiddleLeft;

            this.toolbar.BackColor = Color.FromArgb(0xEE, 0xEE, 0xEE);
            this.toolbar.Controls.Add(this.lblCategoryCaption);
            this.toolbar.Controls.Add(this.cmbCategory);
            this.toolbar.Controls.Add(this.lblItemCaption);
            this.toolbar.Controls.Add(this.cmbItemFilter);
            this.toolbar.Controls.Add(this.lblChartCaption);
            this.toolbar.Controls.Add(this.cmbChartMode);
            this.toolbar.Controls.Add(this.chkAutoRefresh);
            this.toolbar.Controls.Add(this.btnClearView);
            this.toolbar.Controls.Add(this.btnResetChart);
            this.toolbar.Dock = DockStyle.Fill;
            this.toolbar.FlowDirection = FlowDirection.LeftToRight;
            this.toolbar.Padding = new Padding(8, 5, 8, 4);
            this.toolbar.WrapContents = false;

            this.lblCategoryCaption.AutoSize = false;
            this.lblCategoryCaption.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.lblCategoryCaption.Margin = new Padding(0, 3, 4, 0);
            this.lblCategoryCaption.Size = new Size(70, 24);
            this.lblCategoryCaption.Text = "CATEGORY";
            this.lblCategoryCaption.TextAlign = ContentAlignment.MiddleLeft;

            this.cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbCategory.Font = new Font("맑은 고딕", 9F);
            this.cmbCategory.Items.AddRange(new object[] {
            "ALL",
            "SEQUENCE",
            "Run",
            "Unit",
            "Process",
            "Step",
            "Motion",
            "Vision",
            "IO",
            "Wait",
            "Resource",
            "Logic"});
            this.cmbCategory.Margin = new Padding(0, 0, 16, 0);
            this.cmbCategory.Name = "cmbCategory";
            this.cmbCategory.Size = new Size(130, 23);
            this.cmbCategory.SelectedIndexChanged += new System.EventHandler(this.cmbCategory_SelectedIndexChanged);

            this.lblItemCaption.AutoSize = false;
            this.lblItemCaption.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.lblItemCaption.Margin = new Padding(0, 3, 4, 0);
            this.lblItemCaption.Size = new Size(42, 24);
            this.lblItemCaption.Text = "ITEM";
            this.lblItemCaption.TextAlign = ContentAlignment.MiddleLeft;

            this.cmbItemFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbItemFilter.Font = new Font("맑은 고딕", 9F);
            this.cmbItemFilter.Items.AddRange(new object[] {
            "ALL",
            "UNIT FLOW",
            "OUTPUT RECEIVE",
            "BOTTOM INSPECT",
            "BOTTOM VISION->PITCH",
            "BOTTOM INTERVAL",
            "SIDE 0 INSPECT",
            "SIDE 0 INTERVAL",
            "SIDE 0->90 MOTION",
            "SIDE 90 INSPECT",
            "SIDE 90 INTERVAL"});
            this.cmbItemFilter.Margin = new Padding(0, 0, 16, 0);
            this.cmbItemFilter.Name = "cmbItemFilter";
            this.cmbItemFilter.Size = new Size(170, 23);
            this.cmbItemFilter.SelectedIndexChanged += new System.EventHandler(this.cmbItemFilter_SelectedIndexChanged);

            this.lblChartCaption.AutoSize = false;
            this.lblChartCaption.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.lblChartCaption.Margin = new Padding(0, 3, 4, 0);
            this.lblChartCaption.Size = new Size(48, 24);
            this.lblChartCaption.Text = "CHART";
            this.lblChartCaption.TextAlign = ContentAlignment.MiddleLeft;

            this.cmbChartMode.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbChartMode.Font = new Font("맑은 고딕", 9F);
            this.cmbChartMode.Items.AddRange(new object[] {
            "택타임 추이",
            "장비 타임라인"});
            this.cmbChartMode.Margin = new Padding(0, 0, 16, 0);
            this.cmbChartMode.Name = "cmbChartMode";
            this.cmbChartMode.Size = new Size(125, 23);
            this.cmbChartMode.SelectedIndexChanged += new System.EventHandler(this.cmbChartMode_SelectedIndexChanged);

            this.chkAutoRefresh.Checked = true;
            this.chkAutoRefresh.CheckState = CheckState.Checked;
            this.chkAutoRefresh.Font = new Font("맑은 고딕", 9F);
            this.chkAutoRefresh.Margin = new Padding(0, 4, 16, 0);
            this.chkAutoRefresh.Name = "chkAutoRefresh";
            this.chkAutoRefresh.Size = new Size(115, 22);
            this.chkAutoRefresh.Text = "AUTO REFRESH";
            this.chkAutoRefresh.UseVisualStyleBackColor = true;
            this.chkAutoRefresh.CheckedChanged += new System.EventHandler(this.chkAutoRefresh_CheckedChanged);

            this.btnClearView.BackColor = Color.FromArgb(0x99, 0x33, 0x33);
            this.btnClearView.FlatStyle = FlatStyle.Flat;
            this.btnClearView.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.btnClearView.ForeColor = Color.White;
            this.btnClearView.Margin = new Padding(0, 0, 16, 0);
            this.btnClearView.Name = "btnClearView";
            this.btnClearView.Size = new Size(110, 26);
            this.btnClearView.Text = "CLEAR VIEW";
            this.btnClearView.UseVisualStyleBackColor = false;
            this.btnClearView.Click += new System.EventHandler(this.btnClearView_Click);

            this.btnResetChart.BackColor = Color.FromArgb(0x4E, 0x69, 0x7A);
            this.btnResetChart.FlatStyle = FlatStyle.Flat;
            this.btnResetChart.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.btnResetChart.ForeColor = Color.White;
            this.btnResetChart.Margin = new Padding(0, 0, 12, 0);
            this.btnResetChart.Name = "btnResetChart";
            this.btnResetChart.Size = new Size(100, 26);
            this.btnResetChart.Text = "전체 보기";
            this.btnResetChart.UseVisualStyleBackColor = false;
            this.btnResetChart.Click += new System.EventHandler(this.btnResetChart_Click);

            this.lblSummary.AutoSize = false;
            this.lblSummary.AutoEllipsis = true;
            this.lblSummary.BackColor = Color.White;
            this.lblSummary.BorderStyle = BorderStyle.FixedSingle;
            this.lblSummary.Dock = DockStyle.Fill;
            this.lblSummary.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.lblSummary.Margin = new Padding(0);
            this.lblSummary.Padding = new Padding(10, 0, 10, 0);
            this.lblSummary.Text = "택타임 기록 대기 중";
            this.lblSummary.TextAlign = ContentAlignment.MiddleLeft;

            this.tabs.Dock = DockStyle.Fill;
            this.tabs.Font = UiTheme.ButtonFont;
            this.tabs.TabPages.Add(this.tabLogic);
            this.tabs.TabPages.Add(this.tabChart);
            this.tabs.TabPages.Add(this.tabCycle);

            this.tabLogic.Controls.Add(this._grid);
            this.tabLogic.Tag = "i18n:wi.logicLogic";
            this.tabLogic.Text = Lang.T("wi.logicLogic");
            this.tabLogic.UseVisualStyleBackColor = true;

            this.tabChart.Controls.Add(this._chartHost);
            this.tabChart.Tag = "i18n:wi.logicTimechart";
            this.tabChart.Text = Lang.T("wi.logicTimechart");
            this.tabChart.UseVisualStyleBackColor = true;

            // CycleTime 간트 탭 — 내부 컨트롤은 탭을 처음 열 때 지연 생성한다(LogicDetailPage.cs).
            this.tabCycle.Text = "CYCLE TIME";
            this.tabCycle.BackColor = Color.FromArgb(0x0e, 0x11, 0x15);
            this.tabCycle.UseVisualStyleBackColor = false;

            this._grid.AllowUserToAddRows = false;
            this._grid.AllowUserToResizeColumns = true;
            this._grid.AllowUserToResizeRows = false;
            this._grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this._grid.BackgroundColor = Color.White;
            this._grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this._grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0x50, 0x50, 0x50);
            this._grid.ColumnHeadersDefaultCellStyle.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this._grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            this._grid.Columns.Add("NO", "NO");
            this._grid.Columns.Add("CATEGORY", "CATEGORY");
            this._grid.Columns.Add("UNIT", "UNIT");
            this._grid.Columns.Add("SEQUENCE", "SEQUENCE");
            this._grid.Columns.Add("PROCESS", "PROCESS");
            this._grid.Columns.Add("STEP", "STEP");
            this._grid.Columns.Add("RESULT", "RESULT");
            this._grid.Columns.Add("ELAPSED", "ELAPSED(ms)");
            this._grid.Columns.Add("START", "START");
            this._grid.Columns.Add("END", "END");
            this._grid.Columns.Add("DETAIL", "DETAIL");
            this._grid.Dock = DockStyle.Fill;
            this._grid.EnableHeadersVisualStyles = false;
            this._grid.Font = new Font("맑은 고딕", 9F);
            this._grid.MultiSelect = false;
            this._grid.ReadOnly = true;
            this._grid.RowHeadersVisible = false;
            this._grid.RowTemplate.Height = 26;
            this._grid.SelectionChanged += new System.EventHandler(this.grid_SelectionChanged);

            this._chartHost.BackColor = Color.White;
            this._chartHost.Controls.Add(this._timeChart);
            this._chartHost.Dock = DockStyle.Fill;

            this._timeChart.BackColor = Color.White;
            this._timeChart.Dock = DockStyle.Fill;
            this._timeChart.Location = new Point(0, 0);
            this._timeChart.Name = "_timeChart";
            this._timeChart.TabIndex = 0;

            this.lblStatus.BackColor = Color.White;
            this.lblStatus.BorderStyle = BorderStyle.FixedSingle;
            this.lblStatus.Dock = DockStyle.Fill;
            this.lblStatus.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.lblStatus.Padding = new Padding(10, 0, 10, 0);
            this.lblStatus.Text = "택타임 기록을 기다리는 중입니다.";
            this.lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            this.Controls.Add(this.rootLayout);
            this.Name = "LogicDetailPage";
            this.Size = new Size(1678, 900);
            this.ResumeLayout(false);
        }
    }
}

