using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class LogicPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private Panel contentHost;

        private void InitializeComponent()
        {
            this.rootLayout = new TableLayoutPanel();
            this.lblHeader = new Label();
            this.contentHost = new Panel();
            this.SuspendLayout();

            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.contentHost, 0, 1);
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

            this.contentHost.Dock = DockStyle.Fill;

            this.Controls.Add(this.rootLayout);
            this.Name = "LogicPage";
            this.Size = new Size(1400, 900);
            this.ResumeLayout(false);
        }
    }
}
