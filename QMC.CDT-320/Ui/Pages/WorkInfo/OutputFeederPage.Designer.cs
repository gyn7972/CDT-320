using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class OutputFeederPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private GroupBox grpCassettes;
        private GroupBox grpSafety;
        private GroupBox grpAxes;
        private TableLayoutPanel cassetteLayout;
        private TableLayoutPanel safetyLayout;
        private TableLayoutPanel axesLayout;
        private FlowLayoutPanel actionPanel;
        private Label lblFeederPos;
        private Label lblElevatorPos;
        private Label lblClamp;
        private Label lblUpDown;
        private IndicatorDot dotNg;
        private IndicatorDot dotGood1;
        private IndicatorDot dotGood2;
        private IndicatorDot dotProtrusion;
        private IndicatorDot dotDetect;
        private IndicatorDot dotClamped;
        private ActionButton btnInit;
        private ActionButton btnMap;
        private ActionButton btnPick;
        private ActionButton btnPlace;

        private void InitializeComponent()
        {
            this.rootLayout = new TableLayoutPanel();
            this.lblHeader = new Label();
            this.contentLayout = new TableLayoutPanel();
            this.grpCassettes = new GroupBox();
            this.grpSafety = new GroupBox();
            this.grpAxes = new GroupBox();
            this.cassetteLayout = new TableLayoutPanel();
            this.safetyLayout = new TableLayoutPanel();
            this.axesLayout = new TableLayoutPanel();
            this.actionPanel = new FlowLayoutPanel();
            this.lblFeederPos = new Label();
            this.lblElevatorPos = new Label();
            this.lblClamp = new Label();
            this.lblUpDown = new Label();
            this.dotNg = new IndicatorDot();
            this.dotGood1 = new IndicatorDot();
            this.dotGood2 = new IndicatorDot();
            this.dotProtrusion = new IndicatorDot();
            this.dotDetect = new IndicatorDot();
            this.dotClamped = new IndicatorDot();
            this.btnInit = new ActionButton();
            this.btnMap = new ActionButton();
            this.btnPick = new ActionButton();
            this.btnPlace = new ActionButton();
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
            this.lblHeader.Tag = "i18n:wi.outputFeeder";
            this.lblHeader.Text = Lang.T("wi.outputFeeder");
            this.lblHeader.TextAlign = ContentAlignment.MiddleLeft;

            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 460F));
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.contentLayout.Controls.Add(this.grpCassettes, 0, 0);
            this.contentLayout.Controls.Add(this.grpSafety, 1, 0);
            this.contentLayout.Controls.Add(this.grpAxes, 0, 1);
            this.contentLayout.Dock = DockStyle.Top;
            this.contentLayout.Height = 440;
            this.contentLayout.Padding = new Padding(8);
            this.contentLayout.RowCount = 2;
            this.contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 220F));
            this.contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 190F));
            this.contentLayout.SetColumnSpan(this.grpAxes, 2);

            ConfigureGroup(this.grpCassettes, "OUTPUT CASSETTE EXIST", this.cassetteLayout, 4);
            ConfigureGroup(this.grpSafety, "FEEDER CYLINDER / SENSOR", this.safetyLayout, 6);
            ConfigureGroup(this.grpAxes, "AXES", this.axesLayout, 4);

            ConfigureDot(this.dotNg, Color.Red);
            ConfigureDot(this.dotGood1, Color.LimeGreen);
            ConfigureDot(this.dotGood2, Color.LimeGreen);
            AddDotRow(this.cassetteLayout, 0, this.dotNg, "NG CASSETTE");
            AddDotRow(this.cassetteLayout, 1, this.dotGood1, "GOOD1 CASSETTE");
            AddDotRow(this.cassetteLayout, 2, this.dotGood2, "GOOD2 CASSETTE");

            ConfigureValueLabel(this.lblClamp, "---");
            ConfigureValueLabel(this.lblUpDown, "---");
            ConfigureDot(this.dotProtrusion, Color.Red);
            ConfigureDot(this.dotDetect, Color.LimeGreen);
            ConfigureDot(this.dotClamped, Color.Cyan);
            AddValueRow(this.safetyLayout, 0, "CLAMP", this.lblClamp);
            AddValueRow(this.safetyLayout, 1, "UP/DOWN", this.lblUpDown);
            AddDotRow(this.safetyLayout, 2, this.dotProtrusion, "PROTRUSION SENSOR");
            AddDotRow(this.safetyLayout, 3, this.dotDetect, "WAFER DETECT");
            AddDotRow(this.safetyLayout, 4, this.dotClamped, "WAFER CLAMPED");

            ConfigureValueLabel(this.lblFeederPos, "0.000 mm");
            ConfigureValueLabel(this.lblElevatorPos, "0.000 mm");
            AddValueRow(this.axesLayout, 0, "FEEDER Y", this.lblFeederPos);
            AddValueRow(this.axesLayout, 1, "ELEVATOR Z", this.lblElevatorPos);

            this.actionPanel.BackColor = UiTheme.MainBg;
            this.actionPanel.Dock = DockStyle.Fill;
            this.actionPanel.FlowDirection = FlowDirection.LeftToRight;
            this.actionPanel.Padding = new Padding(12);
            ConfigureActionButton(this.btnInit, "FEEDER INIT", 150);
            ConfigureActionButton(this.btnMap, "MAP CASSETTES", 170);
            ConfigureActionButton(this.btnPick, "PICKUP @ STAGE", 180);
            ConfigureActionButton(this.btnPlace, "PLACE @ NG", 160);
            this.actionPanel.Controls.Add(this.btnInit);
            this.actionPanel.Controls.Add(this.btnMap);
            this.actionPanel.Controls.Add(this.btnPick);
            this.actionPanel.Controls.Add(this.btnPlace);

            this.Controls.Add(this.rootLayout);
            this.Name = "OutputFeederPage";
            this.Size = new Size(1400, 900);
            this.ResumeLayout(false);
        }

        private static void ConfigureGroup(GroupBox group, string title, TableLayoutPanel layout, int rows)
        {
            group.BackColor = UiTheme.OptionPanelBg;
            group.Controls.Add(layout);
            group.Dock = DockStyle.Fill;
            group.Font = UiTheme.SectionFont;
            group.Margin = new Padding(4);
            group.Text = title;

            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(12, 18, 12, 12);
            layout.RowCount = rows;
            for (int i = 0; i < rows; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        }

        private static void AddValueRow(TableLayoutPanel layout, int row, string key, Label valueLabel)
        {
            layout.Controls.Add(BuildKeyLabel(key), 0, row);
            layout.Controls.Add(valueLabel, 1, row);
        }

        private static void AddDotRow(TableLayoutPanel layout, int row, IndicatorDot dot, string text)
        {
            layout.Controls.Add(dot, 0, row);
            layout.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Font = UiTheme.ButtonFont,
                Padding = new Padding(8, 0, 0, 0),
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, row);
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
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleCenter;
        }

        private static void ConfigureDot(IndicatorDot dot, Color onColor)
        {
            dot.Dock = DockStyle.Left;
            dot.Margin = new Padding(12, 8, 8, 8);
            dot.OnColor = onColor;
            dot.Size = new Size(18, 18);
        }

        private static void ConfigureActionButton(ActionButton button, string text, int width)
        {
            button.Font = UiTheme.ButtonFont;
            button.Height = 42;
            button.Margin = new Padding(6);
            button.Text = text;
            button.Width = width;
        }
    }
}
