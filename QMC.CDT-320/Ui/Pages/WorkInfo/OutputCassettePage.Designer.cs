using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class OutputCassettePage
    {
        private const int SlotsPerCassette = 25;

        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private GroupBox grpElevator;
        private GroupBox grpNgCassette;
        private GroupBox grpGood1Cassette;
        private GroupBox grpGood2Cassette;
        private TableLayoutPanel elevatorLayout;
        private TableLayoutPanel ngLayout;
        private TableLayoutPanel good1Layout;
        private TableLayoutPanel good2Layout;
        private FlowLayoutPanel actionPanel;
        private Label lblElevatorPos;
        private Label[] ngLeds;
        private Label[] good1Leds;
        private Label[] good2Leds;
        private ActionButton btnMap;
        private ActionButton btnLoad;
        private ActionButton btnUnload;

        private void InitializeComponent()
        {
            this.rootLayout = new TableLayoutPanel();
            this.lblHeader = new Label();
            this.contentLayout = new TableLayoutPanel();
            this.grpElevator = new GroupBox();
            this.grpNgCassette = new GroupBox();
            this.grpGood1Cassette = new GroupBox();
            this.grpGood2Cassette = new GroupBox();
            this.elevatorLayout = new TableLayoutPanel();
            this.ngLayout = new TableLayoutPanel();
            this.good1Layout = new TableLayoutPanel();
            this.good2Layout = new TableLayoutPanel();
            this.actionPanel = new FlowLayoutPanel();
            this.lblElevatorPos = new Label();
            this.ngLeds = new Label[SlotsPerCassette];
            this.good1Leds = new Label[SlotsPerCassette];
            this.good2Leds = new Label[SlotsPerCassette];
            this.btnMap = new ActionButton();
            this.btnLoad = new ActionButton();
            this.btnUnload = new ActionButton();
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
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));

            this.lblHeader.BackColor = UiTheme.StatusBarBg;
            this.lblHeader.Dock = DockStyle.Fill;
            this.lblHeader.Font = UiTheme.SectionFont;
            this.lblHeader.ForeColor = UiTheme.StatusBarFg;
            this.lblHeader.Padding = new Padding(10, 0, 0, 0);
            this.lblHeader.Tag = "i18n:wi.outputCassette";
            this.lblHeader.Text = Lang.T("wi.outputCassette");
            this.lblHeader.TextAlign = ContentAlignment.MiddleLeft;

            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4F));
            this.contentLayout.Controls.Add(this.grpElevator, 0, 0);
            this.contentLayout.Controls.Add(this.grpNgCassette, 0, 1);
            this.contentLayout.Controls.Add(this.grpGood1Cassette, 1, 1);
            this.contentLayout.Controls.Add(this.grpGood2Cassette, 2, 1);
            this.contentLayout.Dock = DockStyle.Fill;
            this.contentLayout.Padding = new Padding(8);
            this.contentLayout.RowCount = 2;
            this.contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));
            this.contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.contentLayout.SetColumnSpan(this.grpElevator, 3);

            ConfigureGroup(this.grpElevator, "ELEVATOR Z", this.elevatorLayout);
            ConfigureGroup(this.grpNgCassette, "NG CASSETTE", this.ngLayout);
            ConfigureGroup(this.grpGood1Cassette, "GOOD1 CASSETTE", this.good1Layout);
            ConfigureGroup(this.grpGood2Cassette, "GOOD2 CASSETTE", this.good2Layout);

            this.elevatorLayout.ColumnCount = 2;
            this.elevatorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            this.elevatorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            this.elevatorLayout.Dock = DockStyle.Top;
            this.elevatorLayout.Height = 48;
            this.elevatorLayout.Padding = new Padding(12, 8, 12, 8);
            this.elevatorLayout.RowCount = 1;
            this.elevatorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.elevatorLayout.Controls.Add(BuildKeyLabel("ELEVATOR Z"), 0, 0);
            ConfigureValueLabel(this.lblElevatorPos, "0.000 mm");
            this.elevatorLayout.Controls.Add(this.lblElevatorPos, 1, 0);

            ConfigureCassetteLayout(this.ngLayout, this.ngLeds);
            ConfigureCassetteLayout(this.good1Layout, this.good1Leds);
            ConfigureCassetteLayout(this.good2Layout, this.good2Leds);

            this.actionPanel.BackColor = UiTheme.MainBg;
            this.actionPanel.Dock = DockStyle.Fill;
            this.actionPanel.FlowDirection = FlowDirection.LeftToRight;
            this.actionPanel.Padding = new Padding(12);
            ConfigureActionButton(this.btnMap, "MAP CASSETTES", null, 180);
            ConfigureActionButton(this.btnLoad, Lang.T("wi.liftWaferLoading"), "i18n:wi.liftWaferLoading", 180);
            ConfigureActionButton(this.btnUnload, Lang.T("wi.liftWaferUnloading"), "i18n:wi.liftWaferUnloading", 180);
            this.actionPanel.Controls.Add(this.btnMap);
            this.actionPanel.Controls.Add(this.btnLoad);
            this.actionPanel.Controls.Add(this.btnUnload);

            this.Controls.Add(this.rootLayout);
            this.Name = "OutputCassettePage";
            this.Size = new Size(1400, 900);
            this.ResumeLayout(false);
        }

        private static void ConfigureGroup(GroupBox group, string title, Control child)
        {
            group.BackColor = UiTheme.OptionPanelBg;
            group.Controls.Add(child);
            group.Dock = DockStyle.Fill;
            group.Font = UiTheme.SectionFont;
            group.Margin = new Padding(4);
            group.Text = title;
        }

        private static void ConfigureCassetteLayout(TableLayoutPanel layout, Label[] leds)
        {
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(12, 18, 12, 12);
            layout.RowCount = SlotsPerCassette;
            for (int i = 0; i < SlotsPerCassette; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / SlotsPerCassette));
                layout.Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 8F, FontStyle.Bold),
                    Text = (i + 1).ToString("D2"),
                    TextAlign = ContentAlignment.MiddleCenter
                }, 0, i);

                var led = new Label
                {
                    BackColor = Color.LightGray,
                    BorderStyle = BorderStyle.FixedSingle,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(2)
                };
                layout.Controls.Add(led, 1, i);
                leds[i] = led;
            }
        }

        private static Label BuildKeyLabel(string text) => new Label
        {
            BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0),
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Fill,
            Font = UiTheme.ButtonFont,
            Padding = new Padding(8, 0, 0, 0),
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        private static void ConfigureValueLabel(Label label, string text)
        {
            label.BackColor = Color.White;
            label.BorderStyle = BorderStyle.FixedSingle;
            label.Dock = DockStyle.Fill;
            label.Font = UiTheme.ValueFont;
            label.Padding = new Padding(0, 0, 8, 0);
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleRight;
        }

        private static void ConfigureActionButton(ActionButton button, string text, string tag, int width)
        {
            button.Font = UiTheme.ButtonFont;
            button.Height = 42;
            button.Margin = new Padding(6);
            button.Tag = tag;
            button.Text = text;
            button.Width = width;
        }
    }
}
