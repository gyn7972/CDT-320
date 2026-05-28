using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

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
        private Label lblNgCassette;
        private Label lblGood1Cassette;
        private Label lblGood2Cassette;
        private Label lblClampTitle;
        private Label lblUpDownTitle;
        private Label lblProtrusionSensor;
        private Label lblWaferDetect;
        private Label lblWaferClamped;
        private Label lblFeederPosTitle;
        private Label lblElevatorPosTitle;
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
            this.lblNgCassette = new Label();
            this.lblGood1Cassette = new Label();
            this.lblGood2Cassette = new Label();
            this.lblClampTitle = new Label();
            this.lblUpDownTitle = new Label();
            this.lblProtrusionSensor = new Label();
            this.lblWaferDetect = new Label();
            this.lblWaferClamped = new Label();
            this.lblFeederPosTitle = new Label();
            this.lblElevatorPosTitle = new Label();
            this.btnInit = new ActionButton();
            this.btnMap = new ActionButton();
            this.btnPick = new ActionButton();
            this.btnPlace = new ActionButton();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.grpCassettes.SuspendLayout();
            this.grpSafety.SuspendLayout();
            this.grpAxes.SuspendLayout();
            this.cassetteLayout.SuspendLayout();
            this.safetyLayout.SuspendLayout();
            this.axesLayout.SuspendLayout();
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
            this.lblHeader.Text = "OUTPUT FEEDER";
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

            this.grpCassettes.BackColor = UiTheme.OptionPanelBg;
            this.grpCassettes.Controls.Add(this.cassetteLayout);
            this.grpCassettes.Dock = DockStyle.Fill;
            this.grpCassettes.Font = UiTheme.SectionFont;
            this.grpCassettes.Margin = new Padding(4);
            this.grpCassettes.Text = "OUTPUT CASSETTE EXIST";

            this.grpSafety.BackColor = UiTheme.OptionPanelBg;
            this.grpSafety.Controls.Add(this.safetyLayout);
            this.grpSafety.Dock = DockStyle.Fill;
            this.grpSafety.Font = UiTheme.SectionFont;
            this.grpSafety.Margin = new Padding(4);
            this.grpSafety.Text = "FEEDER CYLINDER / SENSOR";

            this.grpAxes.BackColor = UiTheme.OptionPanelBg;
            this.grpAxes.Controls.Add(this.axesLayout);
            this.grpAxes.Dock = DockStyle.Fill;
            this.grpAxes.Font = UiTheme.SectionFont;
            this.grpAxes.Margin = new Padding(4);
            this.grpAxes.Text = "AXES";

            this.cassetteLayout.ColumnCount = 2;
            this.cassetteLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            this.cassetteLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            this.cassetteLayout.Controls.Add(this.dotNg, 0, 0);
            this.cassetteLayout.Controls.Add(this.lblNgCassette, 1, 0);
            this.cassetteLayout.Controls.Add(this.dotGood1, 0, 1);
            this.cassetteLayout.Controls.Add(this.lblGood1Cassette, 1, 1);
            this.cassetteLayout.Controls.Add(this.dotGood2, 0, 2);
            this.cassetteLayout.Controls.Add(this.lblGood2Cassette, 1, 2);
            this.cassetteLayout.Dock = DockStyle.Fill;
            this.cassetteLayout.Padding = new Padding(12, 18, 12, 12);
            this.cassetteLayout.RowCount = 4;
            this.cassetteLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.cassetteLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.cassetteLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.cassetteLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

            this.safetyLayout.ColumnCount = 2;
            this.safetyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            this.safetyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            this.safetyLayout.Controls.Add(this.lblClampTitle, 0, 0);
            this.safetyLayout.Controls.Add(this.lblClamp, 1, 0);
            this.safetyLayout.Controls.Add(this.lblUpDownTitle, 0, 1);
            this.safetyLayout.Controls.Add(this.lblUpDown, 1, 1);
            this.safetyLayout.Controls.Add(this.dotProtrusion, 0, 2);
            this.safetyLayout.Controls.Add(this.lblProtrusionSensor, 1, 2);
            this.safetyLayout.Controls.Add(this.dotDetect, 0, 3);
            this.safetyLayout.Controls.Add(this.lblWaferDetect, 1, 3);
            this.safetyLayout.Controls.Add(this.dotClamped, 0, 4);
            this.safetyLayout.Controls.Add(this.lblWaferClamped, 1, 4);
            this.safetyLayout.Dock = DockStyle.Fill;
            this.safetyLayout.Padding = new Padding(12, 18, 12, 12);
            this.safetyLayout.RowCount = 6;
            this.safetyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.safetyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.safetyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.safetyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.safetyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.safetyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

            this.axesLayout.ColumnCount = 2;
            this.axesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            this.axesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            this.axesLayout.Controls.Add(this.lblFeederPosTitle, 0, 0);
            this.axesLayout.Controls.Add(this.lblFeederPos, 1, 0);
            this.axesLayout.Controls.Add(this.lblElevatorPosTitle, 0, 1);
            this.axesLayout.Controls.Add(this.lblElevatorPos, 1, 1);
            this.axesLayout.Dock = DockStyle.Fill;
            this.axesLayout.Padding = new Padding(12, 18, 12, 12);
            this.axesLayout.RowCount = 4;
            this.axesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.axesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.axesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.axesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

            this.dotNg.Dock = DockStyle.Left;
            this.dotNg.Margin = new Padding(12, 8, 8, 8);
            this.dotNg.OnColor = Color.Red;
            this.dotNg.Size = new Size(18, 18);

            this.dotGood1.Dock = DockStyle.Left;
            this.dotGood1.Margin = new Padding(12, 8, 8, 8);
            this.dotGood1.OnColor = Color.LimeGreen;
            this.dotGood1.Size = new Size(18, 18);

            this.dotGood2.Dock = DockStyle.Left;
            this.dotGood2.Margin = new Padding(12, 8, 8, 8);
            this.dotGood2.OnColor = Color.LimeGreen;
            this.dotGood2.Size = new Size(18, 18);

            this.dotProtrusion.Dock = DockStyle.Left;
            this.dotProtrusion.Margin = new Padding(12, 8, 8, 8);
            this.dotProtrusion.OnColor = Color.Red;
            this.dotProtrusion.Size = new Size(18, 18);

            this.dotDetect.Dock = DockStyle.Left;
            this.dotDetect.Margin = new Padding(12, 8, 8, 8);
            this.dotDetect.OnColor = Color.LimeGreen;
            this.dotDetect.Size = new Size(18, 18);

            this.dotClamped.Dock = DockStyle.Left;
            this.dotClamped.Margin = new Padding(12, 8, 8, 8);
            this.dotClamped.OnColor = Color.Cyan;
            this.dotClamped.Size = new Size(18, 18);

            this.lblNgCassette.Dock = DockStyle.Fill;
            this.lblNgCassette.Font = UiTheme.ButtonFont;
            this.lblNgCassette.Padding = new Padding(8, 0, 0, 0);
            this.lblNgCassette.Text = "NG CASSETTE";
            this.lblNgCassette.TextAlign = ContentAlignment.MiddleLeft;

            this.lblGood1Cassette.Dock = DockStyle.Fill;
            this.lblGood1Cassette.Font = UiTheme.ButtonFont;
            this.lblGood1Cassette.Padding = new Padding(8, 0, 0, 0);
            this.lblGood1Cassette.Text = "GOOD1 CASSETTE";
            this.lblGood1Cassette.TextAlign = ContentAlignment.MiddleLeft;

            this.lblGood2Cassette.Dock = DockStyle.Fill;
            this.lblGood2Cassette.Font = UiTheme.ButtonFont;
            this.lblGood2Cassette.Padding = new Padding(8, 0, 0, 0);
            this.lblGood2Cassette.Text = "GOOD2 CASSETTE";
            this.lblGood2Cassette.TextAlign = ContentAlignment.MiddleLeft;

            this.lblClampTitle.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblClampTitle.BorderStyle = BorderStyle.FixedSingle;
            this.lblClampTitle.Dock = DockStyle.Fill;
            this.lblClampTitle.Font = UiTheme.ButtonFont;
            this.lblClampTitle.Padding = new Padding(8, 0, 0, 0);
            this.lblClampTitle.Text = "CLAMP";
            this.lblClampTitle.TextAlign = ContentAlignment.MiddleLeft;

            this.lblUpDownTitle.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblUpDownTitle.BorderStyle = BorderStyle.FixedSingle;
            this.lblUpDownTitle.Dock = DockStyle.Fill;
            this.lblUpDownTitle.Font = UiTheme.ButtonFont;
            this.lblUpDownTitle.Padding = new Padding(8, 0, 0, 0);
            this.lblUpDownTitle.Text = "UP/DOWN";
            this.lblUpDownTitle.TextAlign = ContentAlignment.MiddleLeft;

            this.lblFeederPosTitle.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblFeederPosTitle.BorderStyle = BorderStyle.FixedSingle;
            this.lblFeederPosTitle.Dock = DockStyle.Fill;
            this.lblFeederPosTitle.Font = UiTheme.ButtonFont;
            this.lblFeederPosTitle.Padding = new Padding(8, 0, 0, 0);
            this.lblFeederPosTitle.Text = "FEEDER Y";
            this.lblFeederPosTitle.TextAlign = ContentAlignment.MiddleLeft;

            this.lblElevatorPosTitle.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblElevatorPosTitle.BorderStyle = BorderStyle.FixedSingle;
            this.lblElevatorPosTitle.Dock = DockStyle.Fill;
            this.lblElevatorPosTitle.Font = UiTheme.ButtonFont;
            this.lblElevatorPosTitle.Padding = new Padding(8, 0, 0, 0);
            this.lblElevatorPosTitle.Text = "ELEVATOR Z";
            this.lblElevatorPosTitle.TextAlign = ContentAlignment.MiddleLeft;

            this.lblProtrusionSensor.Dock = DockStyle.Fill;
            this.lblProtrusionSensor.Font = UiTheme.ButtonFont;
            this.lblProtrusionSensor.Padding = new Padding(8, 0, 0, 0);
            this.lblProtrusionSensor.Text = "PROTRUSION SENSOR";
            this.lblProtrusionSensor.TextAlign = ContentAlignment.MiddleLeft;

            this.lblWaferDetect.Dock = DockStyle.Fill;
            this.lblWaferDetect.Font = UiTheme.ButtonFont;
            this.lblWaferDetect.Padding = new Padding(8, 0, 0, 0);
            this.lblWaferDetect.Text = "WAFER DETECT";
            this.lblWaferDetect.TextAlign = ContentAlignment.MiddleLeft;

            this.lblWaferClamped.Dock = DockStyle.Fill;
            this.lblWaferClamped.Font = UiTheme.ButtonFont;
            this.lblWaferClamped.Padding = new Padding(8, 0, 0, 0);
            this.lblWaferClamped.Text = "WAFER CLAMPED";
            this.lblWaferClamped.TextAlign = ContentAlignment.MiddleLeft;

            this.lblClamp.BackColor = Color.White;
            this.lblClamp.BorderStyle = BorderStyle.FixedSingle;
            this.lblClamp.Dock = DockStyle.Fill;
            this.lblClamp.Font = UiTheme.ValueFont;
            this.lblClamp.Text = "---";
            this.lblClamp.TextAlign = ContentAlignment.MiddleCenter;

            this.lblUpDown.BackColor = Color.White;
            this.lblUpDown.BorderStyle = BorderStyle.FixedSingle;
            this.lblUpDown.Dock = DockStyle.Fill;
            this.lblUpDown.Font = UiTheme.ValueFont;
            this.lblUpDown.Text = "---";
            this.lblUpDown.TextAlign = ContentAlignment.MiddleCenter;

            this.lblFeederPos.BackColor = Color.White;
            this.lblFeederPos.BorderStyle = BorderStyle.FixedSingle;
            this.lblFeederPos.Dock = DockStyle.Fill;
            this.lblFeederPos.Font = UiTheme.ValueFont;
            this.lblFeederPos.Text = "0.000 mm";
            this.lblFeederPos.TextAlign = ContentAlignment.MiddleCenter;

            this.lblElevatorPos.BackColor = Color.White;
            this.lblElevatorPos.BorderStyle = BorderStyle.FixedSingle;
            this.lblElevatorPos.Dock = DockStyle.Fill;
            this.lblElevatorPos.Font = UiTheme.ValueFont;
            this.lblElevatorPos.Text = "0.000 mm";
            this.lblElevatorPos.TextAlign = ContentAlignment.MiddleCenter;

            this.actionPanel.BackColor = UiTheme.MainBg;
            this.actionPanel.Dock = DockStyle.Fill;
            this.actionPanel.FlowDirection = FlowDirection.LeftToRight;
            this.actionPanel.Padding = new Padding(12);

            this.btnInit.Font = UiTheme.ButtonFont;
            this.btnInit.Height = 42;
            this.btnInit.Margin = new Padding(6);
            this.btnInit.Text = "FEEDER INIT";
            this.btnInit.Width = 150;

            this.btnMap.Font = UiTheme.ButtonFont;
            this.btnMap.Height = 42;
            this.btnMap.Margin = new Padding(6);
            this.btnMap.Text = "MAP CASSETTES";
            this.btnMap.Width = 170;

            this.btnPick.Font = UiTheme.ButtonFont;
            this.btnPick.Height = 42;
            this.btnPick.Margin = new Padding(6);
            this.btnPick.Text = "PICKUP @ STAGE";
            this.btnPick.Width = 180;

            this.btnPlace.Font = UiTheme.ButtonFont;
            this.btnPlace.Height = 42;
            this.btnPlace.Margin = new Padding(6);
            this.btnPlace.Text = "PLACE @ NG";
            this.btnPlace.Width = 160;

            this.actionPanel.Controls.Add(this.btnInit);
            this.actionPanel.Controls.Add(this.btnMap);
            this.actionPanel.Controls.Add(this.btnPick);
            this.actionPanel.Controls.Add(this.btnPlace);

            this.Controls.Add(this.rootLayout);
            this.Name = "OutputFeederPage";
            this.Size = new Size(1400, 900);
            this.axesLayout.ResumeLayout(false);
            this.safetyLayout.ResumeLayout(false);
            this.cassetteLayout.ResumeLayout(false);
            this.grpAxes.ResumeLayout(false);
            this.grpSafety.ResumeLayout(false);
            this.grpCassettes.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.rootLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
