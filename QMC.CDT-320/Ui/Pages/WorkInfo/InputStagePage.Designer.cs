using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class InputStagePage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private GroupBox grpState;
        private GroupBox grpCounters;
        private GroupBox grpCylinder;
        private GroupBox grpInfo;
        private TableLayoutPanel stateLayout;
        private TableLayoutPanel counterLayout;
        private TableLayoutPanel cylinderLayout;
        private TableLayoutPanel infoLayout;
        private FlowLayoutPanel actionsLayout;

        private void InitializeComponent()
        {
            this.rootLayout = new TableLayoutPanel();
            this.lblHeader = new Label();
            this.contentLayout = new TableLayoutPanel();
            this.grpState = new GroupBox();
            this.grpCounters = new GroupBox();
            this.grpCylinder = new GroupBox();
            this.grpInfo = new GroupBox();
            this.stateLayout = new TableLayoutPanel();
            this.counterLayout = new TableLayoutPanel();
            this.cylinderLayout = new TableLayoutPanel();
            this.infoLayout = new TableLayoutPanel();
            this.actionsLayout = new FlowLayoutPanel();
            this.SuspendLayout();

            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 1);
            this.rootLayout.Controls.Add(this.actionsLayout, 0, 2);
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
            this.lblHeader.Tag = "i18n:tab.workInfo";
            this.lblHeader.Text = Lang.T("tab.workInfo");
            this.lblHeader.TextAlign = ContentAlignment.MiddleLeft;

            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27F));
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            this.contentLayout.Controls.Add(this.grpState, 0, 0);
            this.contentLayout.Controls.Add(this.grpCounters, 1, 0);
            this.contentLayout.Controls.Add(this.grpCylinder, 2, 0);
            this.contentLayout.Controls.Add(this.grpInfo, 0, 1);
            this.contentLayout.Dock = DockStyle.Top;
            this.contentLayout.Height = 520;
            this.contentLayout.Padding = new Padding(8);
            this.contentLayout.RowCount = 2;
            this.contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 190F));
            this.contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 300F));
            this.contentLayout.SetColumnSpan(this.grpInfo, 3);

            ConfigureGroup(this.grpState, Lang.T("tab.workInfo"), this.stateLayout);
            ConfigureGroup(this.grpCounters, "COUNTER", this.counterLayout);
            ConfigureGroup(this.grpCylinder, Lang.T("stage.needleCylInfo"), this.cylinderLayout);
            ConfigureGroup(this.grpInfo, Lang.T("common.info"), this.infoLayout);

            AddPair(this.stateLayout, 0, Lang.T("wi.stageExist"), "EMPTY");
            AddPair(this.stateLayout, 1, Lang.T("wi.stageAlign"), "INCOMPLETE");
            AddPair(this.stateLayout, 2, Lang.T("wi.stageBarcode"), "INCOMPLETE");
            AddPair(this.stateLayout, 3, Lang.T("wi.stageChipAlign"), "INCOMPLETE");
            AddPair(this.stateLayout, 4, Lang.T("wi.stageFinish"), "INCOMPLETE");

            AddPair(this.counterLayout, 0, Lang.T("wi.needleUsing"), "0 ea");
            AddPair(this.counterLayout, 1, Lang.T("wi.jellPadUsing"), "0 ea");

            AddPair(this.cylinderLayout, 0, Lang.T("wi.expending"), "...");
            AddPair(this.cylinderLayout, 1, Lang.T("wi.needleUpDown"), "...");

            this.infoLayout.ColumnCount = 4;
            this.infoLayout.ColumnStyles.Clear();
            for (int i = 0; i < 4; i++) this.infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            this.infoLayout.RowCount = 3;
            this.infoLayout.RowStyles.Clear();
            this.infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            this.infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            this.infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            this.infoLayout.Controls.Add(BuildAxisBlock("STAGE AXIS X", "0 um"), 0, 0);
            this.infoLayout.Controls.Add(BuildAxisBlock("STAGE AXIS T", "0 um"), 1, 0);
            this.infoLayout.Controls.Add(BuildAxisBlock("STAGE AXIS Y", "0 um"), 2, 0);
            this.infoLayout.Controls.Add(BuildAxisBlock("NEEDLE AXIS Z", "0 um"), 3, 0);
            this.infoLayout.Controls.Add(BuildIoBlock("STAGE RING CHECK #1"), 0, 1);
            this.infoLayout.Controls.Add(BuildIoBlock("STAGE RING CHECK #2"), 1, 1);
            this.infoLayout.Controls.Add(BuildIoBlock("NEEDLE VACUUM"), 2, 1);

            this.actionsLayout.BackColor = UiTheme.MainBg;
            this.actionsLayout.Dock = DockStyle.Fill;
            this.actionsLayout.FlowDirection = FlowDirection.LeftToRight;
            this.actionsLayout.Padding = new Padding(12);
            this.actionsLayout.Controls.Add(new ActionButton { Text = Lang.T("wi.wfAlign"), Tag = "i18n:wi.wfAlign", Width = 160, Margin = new Padding(6) });
            this.actionsLayout.Controls.Add(new ActionButton { Text = Lang.T("wi.wfBarcode"), Tag = "i18n:wi.wfBarcode", Width = 160, Margin = new Padding(6) });

            this.Controls.Add(this.rootLayout);
            this.Name = "InputStagePage";
            this.Size = new Size(1400, 900);
            this.ResumeLayout(false);
        }

        private static void ConfigureGroup(GroupBox group, string title, TableLayoutPanel layout)
        {
            group.BackColor = UiTheme.OptionPanelBg;
            group.Controls.Add(layout);
            group.Dock = DockStyle.Fill;
            group.Font = UiTheme.SectionFont;
            group.Text = title;

            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(12, 18, 12, 12);
            layout.RowCount = 6;
            for (int i = 0; i < 6; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        }

        private static void AddPair(TableLayoutPanel layout, int row, string key, string value)
        {
            layout.Controls.Add(new Label
            {
                BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Font = UiTheme.ButtonFont,
                Padding = new Padding(8, 0, 0, 0),
                Text = key,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, row);
            layout.Controls.Add(new Label
            {
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Font = UiTheme.ValueFont,
                Text = value,
                TextAlign = ContentAlignment.MiddleCenter
            }, 1, row);
        }

        private static Control BuildAxisBlock(string title, string value)
        {
            var panel = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, RowCount = 2, Margin = new Padding(4) };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(new Label { BackColor = Color.Black, Dock = DockStyle.Fill, Font = UiTheme.SectionFont, ForeColor = Color.White, Padding = new Padding(6, 0, 0, 0), Text = title, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            panel.Controls.Add(new Label { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Fill, Font = new Font("Consolas", 10F), Padding = new Padding(0, 0, 6, 0), Text = value, TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            return panel;
        }

        private static Control BuildIoBlock(string title)
        {
            var panel = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, RowCount = 1, Margin = new Padding(4) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.Controls.Add(new IndicatorDot { Dock = DockStyle.Fill, Margin = new Padding(6, 8, 6, 8), OnColor = Color.LimeGreen }, 0, 0);
            panel.Controls.Add(new Label { BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0), Dock = DockStyle.Fill, Font = UiTheme.ValueFont, Padding = new Padding(6, 0, 0, 0), Text = title, TextAlign = ContentAlignment.MiddleLeft }, 1, 0);
            return panel;
        }
    }
}
