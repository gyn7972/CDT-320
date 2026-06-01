using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class LogicDetailPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TabControl tabs;
        private TabPage tabLogic;
        private TabPage tabChart;
        private DataGridView _grid;
        private Panel _chartHost;

        private void InitializeComponent()
        {
            this.rootLayout = new TableLayoutPanel();
            this.lblHeader = new Label();
            this.tabs = new TabControl();
            this.tabLogic = new TabPage();
            this.tabChart = new TabPage();
            this._grid = new DataGridView();
            this._chartHost = new Panel();
            this.SuspendLayout();

            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.tabs, 0, 1);
            this.rootLayout.Dock = DockStyle.Fill;
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.lblHeader.BackColor = UiTheme.StatusBarBg;
            this.lblHeader.Dock = DockStyle.Fill;
            this.lblHeader.Font = UiTheme.SectionFont;
            this.lblHeader.ForeColor = UiTheme.StatusBarFg;
            this.lblHeader.Padding = new Padding(10, 0, 0, 0);
            this.lblHeader.Tag = "i18n:wi.logic";
            this.lblHeader.Text = Lang.T("wi.logic");
            this.lblHeader.TextAlign = ContentAlignment.MiddleLeft;

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
            this._grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this._grid.BackgroundColor = Color.White;
            this._grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this._grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0x50, 0x50, 0x50);
            this._grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this._grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            this._grid.Columns.Add("INDEX", "INDEX");
            this._grid.Columns.Add("MODULE", "MODULE");
            this._grid.Columns.Add("STATE", "STATE");
            this._grid.Columns.Add("STEP", "STEP");
            this._grid.Columns.Add("DESCRIPTION", "DESCRIPTION");
            this._grid.Columns.Add("DURATION", "DURATION(ms)");
            this._grid.Dock = DockStyle.Fill;
            this._grid.EnableHeadersVisualStyles = false;
            this._grid.Font = new Font("Segoe UI", 9F);
            this._grid.ReadOnly = true;
            this._grid.RowHeadersVisible = false;
            this._grid.RowTemplate.Height = 26;

            this._chartHost.BackColor = Color.White;
            this._chartHost.Dock = DockStyle.Fill;

            this.Controls.Add(this.rootLayout);
            this.Name = "LogicDetailPage";
            this.Size = new Size(1678, 900);
            this.ResumeLayout(false);
        }
    }
}

