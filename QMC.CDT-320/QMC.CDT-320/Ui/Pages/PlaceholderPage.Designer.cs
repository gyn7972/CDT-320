using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages
{
    partial class PlaceholderPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private Label lblPlaceholder;

        private void InitializeComponent()
        {
            this.rootLayout = new TableLayoutPanel();
            this.lblHeader = new Label();
            this.lblPlaceholder = new Label();
            this.SuspendLayout();

            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.lblPlaceholder, 0, 1);
            this.rootLayout.Dock = DockStyle.Fill;
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.lblHeader.BackColor = UiTheme.StatusBarBg;
            this.lblHeader.Dock = DockStyle.Fill;
            this.lblHeader.Font = UiTheme.SectionFont;
            this.lblHeader.ForeColor = UiTheme.StatusBarFg;
            this.lblHeader.Padding = new Padding(10, 0, 0, 0);
            this.lblHeader.Tag = "i18n:common.caption";
            this.lblHeader.Text = Lang.T("common.caption");
            this.lblHeader.TextAlign = ContentAlignment.MiddleLeft;

            this.lblPlaceholder.Dock = DockStyle.Fill;
            this.lblPlaceholder.Font = new Font("Segoe UI", 20F);
            this.lblPlaceholder.ForeColor = Color.FromArgb(0x55, 0x55, 0x55);
            this.lblPlaceholder.Tag = "i18n:common.caption";
            this.lblPlaceholder.Text = Lang.T("common.caption") + "   (placeholder)";
            this.lblPlaceholder.TextAlign = ContentAlignment.MiddleCenter;

            this.Controls.Add(this.rootLayout);
            this.Name = "PlaceholderPage";
            this.Size = new Size(1678, 900);
            this.ResumeLayout(false);
        }
    }
}

