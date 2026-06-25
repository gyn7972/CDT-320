using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class LogicDetailPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private FlowLayoutPanel toolbar;
        private Label lblCategoryCaption;
        private ComboBox cmbCategory;
        private Label lblItemCaption;
        private ComboBox cmbItemFilter;
        private CheckBox chkAutoRefresh;
        private Button btnClearView;
        private Label lblSummary;
        private TabControl tabs;
        private TabPage tabLogic;
        private TabPage tabChart;
        private DataGridView _grid;
        private Panel _chartHost;
        private Label lblStatus;

        private void InitializeComponent()
        {
            this.rootLayout = new TableLayoutPanel();
            this.lblHeader = new Label();
            this.toolbar = new FlowLayoutPanel();
            this.lblCategoryCaption = new Label();
            this.cmbCategory = new ComboBox();
            this.lblItemCaption = new Label();
            this.cmbItemFilter = new ComboBox();
            this.chkAutoRefresh = new CheckBox();
            this.btnClearView = new Button();
            this.lblSummary = new Label();
            this.tabs = new TabControl();
            this.tabLogic = new TabPage();
            this.tabChart = new TabPage();
            this._grid = new DataGridView();
            this._chartHost = new Panel();
            this.lblStatus = new Label();
            this.SuspendLayout();

            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.toolbar, 0, 1);
            this.rootLayout.Controls.Add(this.tabs, 0, 2);
            this.rootLayout.Controls.Add(this.lblStatus, 0, 3);
            this.rootLayout.Dock = DockStyle.Fill;
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

            this.lblHeader.BackColor = UiTheme.StatusBarBg;
            this.lblHeader.Dock = DockStyle.Fill;
            this.lblHeader.Font = UiTheme.SectionFont;
            this.lblHeader.ForeColor = UiTheme.StatusBarFg;
            this.lblHeader.Padding = new Padding(10, 0, 0, 0);
            this.lblHeader.Tag = "i18n:wi.logic";
            this.lblHeader.Text = Lang.T("wi.logic");
            this.lblHeader.TextAlign = ContentAlignment.MiddleLeft;

            this.toolbar.BackColor = Color.FromArgb(0xEE, 0xEE, 0xEE);
            this.toolbar.Controls.Add(this.lblCategoryCaption);
            this.toolbar.Controls.Add(this.cmbCategory);
            this.toolbar.Controls.Add(this.lblItemCaption);
            this.toolbar.Controls.Add(this.cmbItemFilter);
            this.toolbar.Controls.Add(this.chkAutoRefresh);
            this.toolbar.Controls.Add(this.btnClearView);
            this.toolbar.Controls.Add(this.lblSummary);
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

            this.lblSummary.AutoSize = false;
            this.lblSummary.Dock = DockStyle.None;
            this.lblSummary.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.lblSummary.Margin = new Padding(0, 4, 0, 0);
            this.lblSummary.Size = new Size(680, 22);
            this.lblSummary.Text = "택타임 기록 대기 중";
            this.lblSummary.TextAlign = ContentAlignment.MiddleLeft;

            this.tabs.Dock = DockStyle.Fill;
            this.tabs.Font = UiTheme.ButtonFont;
            this.tabs.TabPages.Add(this.tabLogic);
            this.tabs.TabPages.Add(this.tabChart);

            this.tabLogic.Controls.Add(this._grid);
            this.tabLogic.Tag = "i18n:wi.logicLogic";
            this.tabLogic.Text = Lang.T("wi.logicLogic");
            this.tabLogic.UseVisualStyleBackColor = true;

            this.tabChart.Controls.Add(this._chartHost);
            this.tabChart.Tag = "i18n:wi.logicTimechart";
            this.tabChart.Text = Lang.T("wi.logicTimechart");
            this.tabChart.UseVisualStyleBackColor = true;

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
            foreach (DataGridViewColumn column in this._grid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
                column.Resizable = DataGridViewTriState.True;
            }

            this._chartHost.BackColor = Color.White;
            this._chartHost.Dock = DockStyle.Fill;
            this._chartHost.Paint += new PaintEventHandler(this.chartHost_Paint);

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

