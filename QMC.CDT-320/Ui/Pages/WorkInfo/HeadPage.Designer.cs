using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class HeadPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private GroupBox grpState;
        private GroupBox grpCounters;
        private GroupBox grpInfo;
        private TableLayoutPanel stateLayout;
        private TableLayoutPanel counterLayout;
        private TableLayoutPanel infoLayout;
        private Button btnInitAll;
        private Button btnCountClear;

        private void InitializeComponent()
        {
            this.rootLayout = new TableLayoutPanel();
            this.lblHeader = new Label();
            this.contentLayout = new TableLayoutPanel();
            this.grpState = new GroupBox();
            this.grpCounters = new GroupBox();
            this.grpInfo = new GroupBox();
            this.stateLayout = new TableLayoutPanel();
            this.counterLayout = new TableLayoutPanel();
            this.infoLayout = new TableLayoutPanel();
            this.btnInitAll = new Button();
            this.btnCountClear = new Button();
            this.SuspendLayout();

            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 1);
            this.rootLayout.Dock = DockStyle.Fill;
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.lblHeader.BackColor = UiTheme.StatusBarBg;
            this.lblHeader.Dock = DockStyle.Fill;
            this.lblHeader.Font = UiTheme.SectionFont;
            this.lblHeader.ForeColor = UiTheme.StatusBarFg;
            this.lblHeader.Padding = new Padding(10, 0, 0, 0);
            this.lblHeader.Tag = "i18n:wi.frontHead";
            this.lblHeader.Text = Lang.T("wi.frontHead");
            this.lblHeader.TextAlign = ContentAlignment.MiddleLeft;

            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 480F));
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.contentLayout.Controls.Add(this.grpState, 0, 0);
            this.contentLayout.Controls.Add(this.grpCounters, 1, 0);
            this.contentLayout.Controls.Add(this.grpInfo, 0, 1);
            this.contentLayout.Dock = DockStyle.Top;
            this.contentLayout.Height = 520;
            this.contentLayout.Padding = new Padding(8);
            this.contentLayout.RowCount = 2;
            this.contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 240F));
            this.contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 240F));
            this.contentLayout.SetColumnSpan(this.grpInfo, 2);

            ConfigureGroup(this.grpState, Lang.T("tab.workInfo"), this.stateLayout);
            ConfigureGroup(this.grpCounters, "COUNTER", this.counterLayout);
            ConfigureGroup(this.grpInfo, Lang.T("common.info"), this.infoLayout);

            AddPair(this.stateLayout, 0, Lang.T("wi.head1"), "EMPTY", Color.Green);
            AddPair(this.stateLayout, 1, Lang.T("wi.head2"), "EMPTY", Color.Green);
            ConfigureButton(this.btnInitAll, Lang.T("wi.head.initAll"), "i18n:wi.head.initAll");
            this.stateLayout.Controls.Add(this.btnInitAll, 0, 2);
            this.stateLayout.SetColumnSpan(this.btnInitAll, 2);
            AddPair(this.stateLayout, 3, Lang.T("wi.head.colletChange"), "INCOMPLETE", Color.Black);
            AddPair(this.stateLayout, 4, Lang.T("wi.head.autoPos"), "INCOMPLETE", Color.Black);
            AddPair(this.stateLayout, 5, Lang.T("wi.head.colletCleaning"), "INCOMPLETE", Color.Black);
            AddPair(this.stateLayout, 6, Lang.T("wi.head.colletCheck"), "INCOMPLETE", Color.Black);

            AddPair(this.counterLayout, 0, Lang.T("wi.pickFail"), "0 ea", Color.Black);
            AddPair(this.counterLayout, 1, Lang.T("wi.placeFail"), "0 ea", Color.Black);
            AddPair(this.counterLayout, 2, Lang.T("wi.collet1Use"), "0 ea", Color.Black);
            AddPair(this.counterLayout, 3, Lang.T("wi.collet2Use"), "0 ea", Color.Black);
            ConfigureButton(this.btnCountClear, Lang.T("wi.head.countClear"), "i18n:wi.head.countClear");
            this.counterLayout.Controls.Add(this.btnCountClear, 0, 4);
            this.counterLayout.SetColumnSpan(this.btnCountClear, 2);

            this.infoLayout.ColumnCount = 4;
            this.infoLayout.ColumnStyles.Clear();
            for (int i = 0; i < 4; i++) this.infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            this.infoLayout.RowCount = 3;
            this.infoLayout.RowStyles.Clear();
            this.infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            this.infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            this.infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            this.infoLayout.Controls.Add(BuildAxisBlock("HEAD AXIS T", "0 um"), 0, 0);
            this.infoLayout.Controls.Add(BuildIoBlock("HEAD VACUUM #1"), 0, 1);
            this.infoLayout.Controls.Add(BuildIoBlock("HEAD VACUUM #2"), 1, 1);
            this.infoLayout.Controls.Add(BuildIoBlock("HEAD BLOW #1"), 0, 2);
            this.infoLayout.Controls.Add(BuildIoBlock("HEAD BLOW #2"), 1, 2);

            this.Controls.Add(this.rootLayout);
            this.Name = "HeadPage";
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
            layout.RowCount = 8;
            for (int i = 0; i < 8; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        }

        private static void AddPair(TableLayoutPanel layout, int row, string key, string value, Color valueColor)
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
                ForeColor = valueColor,
                Text = value,
                TextAlign = ContentAlignment.MiddleCenter
            }, 1, row);
        }

        private static void ConfigureButton(Button button, string text, string tag)
        {
            button.Dock = DockStyle.Fill;
            button.FlatStyle = FlatStyle.Flat;
            button.Font = UiTheme.ButtonFont;
            button.Tag = tag;
            button.Text = text;
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
