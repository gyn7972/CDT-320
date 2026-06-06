using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class OutputStagePage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private GroupBox grpStatus;
        private TableLayoutPanel statusLayout;
        private TableLayoutPanel actionPanel;
        private Label lblStageZCaption;
        private Label lblStageZValue;
        private Label lblGoodCountCaption;
        private Label lblGoodCountValue;
        private Label lblNgCountCaption;
        private Label lblNgCountValue;
        private ActionButton btnStageInit;
        private ActionButton btnStageReady;

        private void InitializeComponent()
        {
            this.rootLayout = new TableLayoutPanel();
            this.lblHeader = new Label();
            this.contentLayout = new TableLayoutPanel();
            this.grpStatus = new GroupBox();
            this.statusLayout = new TableLayoutPanel();
            this.actionPanel = new TableLayoutPanel();
            this.lblStageZCaption = new Label();
            this.lblStageZValue = new Label();
            this.lblGoodCountCaption = new Label();
            this.lblGoodCountValue = new Label();
            this.lblNgCountCaption = new Label();
            this.lblNgCountValue = new Label();
            this.btnStageInit = new ActionButton();
            this.btnStageReady = new ActionButton();
            this.SuspendLayout();

            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 1);
            this.rootLayout.Controls.Add(this.actionPanel, 0, 2);
            this.rootLayout.Dock = DockStyle.Fill;
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));

            this.lblHeader.BackColor = UiTheme.StatusBarBg;
            this.lblHeader.Dock = DockStyle.Fill;
            this.lblHeader.Font = UiTheme.SectionFont;
            this.lblHeader.ForeColor = UiTheme.StatusBarFg;
            this.lblHeader.Padding = new Padding(10, 0, 0, 0);
            this.lblHeader.Tag = "i18n:wi.outputStage";
            this.lblHeader.Text = Lang.T("wi.outputStage");
            this.lblHeader.TextAlign = ContentAlignment.MiddleLeft;

            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 480F));
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.contentLayout.Controls.Add(this.grpStatus, 0, 0);
            this.contentLayout.Dock = DockStyle.Top;
            this.contentLayout.Height = 260;
            this.contentLayout.Padding = new Padding(8);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.grpStatus.BackColor = UiTheme.OptionPanelBg;
            this.grpStatus.Controls.Add(this.statusLayout);
            this.grpStatus.Dock = DockStyle.Fill;
            this.grpStatus.Font = UiTheme.SectionFont;
            this.grpStatus.Text = Lang.T("common.info");

            this.statusLayout.ColumnCount = 2;
            this.statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            this.statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            this.statusLayout.Dock = DockStyle.Fill;
            this.statusLayout.Padding = new Padding(12, 18, 12, 12);
            this.statusLayout.RowCount = 6;
            this.statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.statusLayout.Controls.Add(this.lblStageZCaption, 0, 0);
            this.statusLayout.Controls.Add(this.lblStageZValue, 1, 0);
            this.statusLayout.Controls.Add(this.lblGoodCountCaption, 0, 1);
            this.statusLayout.Controls.Add(this.lblGoodCountValue, 1, 1);
            this.statusLayout.Controls.Add(this.lblNgCountCaption, 0, 2);
            this.statusLayout.Controls.Add(this.lblNgCountValue, 1, 2);

            this.lblStageZCaption.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblStageZCaption.BorderStyle = BorderStyle.FixedSingle;
            this.lblStageZCaption.Dock = DockStyle.Fill;
            this.lblStageZCaption.Font = UiTheme.ButtonFont;
            this.lblStageZCaption.Padding = new Padding(8, 0, 0, 0);
            this.lblStageZCaption.Text = Lang.T("wi.outStageZ");
            this.lblStageZCaption.TextAlign = ContentAlignment.MiddleLeft;
            this.lblStageZValue.BackColor = Color.White;
            this.lblStageZValue.BorderStyle = BorderStyle.FixedSingle;
            this.lblStageZValue.Dock = DockStyle.Fill;
            this.lblStageZValue.Font = UiTheme.ValueFont;
            this.lblStageZValue.Text = "0 um";
            this.lblStageZValue.TextAlign = ContentAlignment.MiddleCenter;
            this.lblGoodCountCaption.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblGoodCountCaption.BorderStyle = BorderStyle.FixedSingle;
            this.lblGoodCountCaption.Dock = DockStyle.Fill;
            this.lblGoodCountCaption.Font = UiTheme.ButtonFont;
            this.lblGoodCountCaption.Padding = new Padding(8, 0, 0, 0);
            this.lblGoodCountCaption.Text = Lang.T("wi.outGoodCount");
            this.lblGoodCountCaption.TextAlign = ContentAlignment.MiddleLeft;
            this.lblGoodCountValue.BackColor = Color.White;
            this.lblGoodCountValue.BorderStyle = BorderStyle.FixedSingle;
            this.lblGoodCountValue.Dock = DockStyle.Fill;
            this.lblGoodCountValue.Font = UiTheme.ValueFont;
            this.lblGoodCountValue.Text = "0 ea";
            this.lblGoodCountValue.TextAlign = ContentAlignment.MiddleCenter;
            this.lblNgCountCaption.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblNgCountCaption.BorderStyle = BorderStyle.FixedSingle;
            this.lblNgCountCaption.Dock = DockStyle.Fill;
            this.lblNgCountCaption.Font = UiTheme.ButtonFont;
            this.lblNgCountCaption.Padding = new Padding(8, 0, 0, 0);
            this.lblNgCountCaption.Text = Lang.T("wi.outNgCount");
            this.lblNgCountCaption.TextAlign = ContentAlignment.MiddleLeft;
            this.lblNgCountValue.BackColor = Color.White;
            this.lblNgCountValue.BorderStyle = BorderStyle.FixedSingle;
            this.lblNgCountValue.Dock = DockStyle.Fill;
            this.lblNgCountValue.Font = UiTheme.ValueFont;
            this.lblNgCountValue.Text = "0 ea";
            this.lblNgCountValue.TextAlign = ContentAlignment.MiddleCenter;

            this.actionPanel.BackColor = UiTheme.MainBg;
            this.actionPanel.ColumnCount = 4;
            this.actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            this.actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            this.actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1F));
            this.actionPanel.Controls.Add(this.btnStageInit, 0, 0);
            this.actionPanel.Controls.Add(this.btnStageReady, 1, 0);
            this.actionPanel.Dock = DockStyle.Fill;
            this.actionPanel.Padding = new Padding(12);
            this.actionPanel.RowCount = 1;
            this.actionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.btnStageInit.Dock = DockStyle.Top;
            this.btnStageInit.Font = UiTheme.ButtonFont;
            this.btnStageInit.Height = 42;
            this.btnStageInit.Margin = new Padding(6);
            this.btnStageInit.Tag = "i18n:wi.stageInit";
            this.btnStageInit.Text = Lang.T("wi.stageInit");
            this.btnStageReady.Dock = DockStyle.Top;
            this.btnStageReady.Font = UiTheme.ButtonFont;
            this.btnStageReady.Height = 42;
            this.btnStageReady.Margin = new Padding(6);
            this.btnStageReady.Tag = "i18n:wi.stageReady";
            this.btnStageReady.Text = Lang.T("wi.stageReady");

            this.Controls.Add(this.rootLayout);
            this.Name = "OutputStagePage";
            this.Size = new Size(1678, 900);
            this.ResumeLayout(false);
        }
    }
}

