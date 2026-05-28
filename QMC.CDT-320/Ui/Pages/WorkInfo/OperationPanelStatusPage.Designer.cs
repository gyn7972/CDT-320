using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class OperationPanelStatusPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private GroupBox grpButtons;
        private GroupBox grpLamps;
        private GroupBox grpResources;
        private GroupBox grpTower;
        private GroupBox grpIonizer;
        private TableLayoutPanel buttonLayout;
        private TableLayoutPanel lampLayout;
        private TableLayoutPanel towerLayout;
        private TableLayoutPanel resourceLayout;
        private TableLayoutPanel ionizerLayout;
        private IndicatorDot _dotStart;
        private IndicatorDot _dotStop;
        private IndicatorDot _dotReset;
        private IndicatorDot _dotEmgF;
        private IndicatorDot _dotEmgL;
        private IndicatorDot _dotEmgR;
        private IndicatorDot _ledStartLamp;
        private IndicatorDot _ledStopLamp;
        private IndicatorDot _ledResetLamp;
        private IndicatorDot _tlRed;
        private IndicatorDot _tlYellow;
        private IndicatorDot _tlGreen;
        private IndicatorDot _ledBuzzer;
        private IndicatorDot _dotCda1;
        private IndicatorDot _dotCda2;
        private IndicatorDot _dotVac1;
        private IndicatorDot _dotVac2;
        private IndicatorDot _dotVac3;
        private IndicatorDot _dotVac4;
        private IndicatorDot _dotIonizer;

        private void InitializeComponent()
        {
            this.rootLayout = new TableLayoutPanel();
            this.lblHeader = new Label();
            this.contentLayout = new TableLayoutPanel();
            this.grpButtons = new GroupBox();
            this.grpLamps = new GroupBox();
            this.grpResources = new GroupBox();
            this.grpTower = new GroupBox();
            this.grpIonizer = new GroupBox();
            this.buttonLayout = new TableLayoutPanel();
            this.lampLayout = new TableLayoutPanel();
            this.towerLayout = new TableLayoutPanel();
            this.resourceLayout = new TableLayoutPanel();
            this.ionizerLayout = new TableLayoutPanel();
            this._dotStart = new IndicatorDot();
            this._dotStop = new IndicatorDot();
            this._dotReset = new IndicatorDot();
            this._dotEmgF = new IndicatorDot();
            this._dotEmgL = new IndicatorDot();
            this._dotEmgR = new IndicatorDot();
            this._ledStartLamp = new IndicatorDot();
            this._ledStopLamp = new IndicatorDot();
            this._ledResetLamp = new IndicatorDot();
            this._tlRed = new IndicatorDot();
            this._tlYellow = new IndicatorDot();
            this._tlGreen = new IndicatorDot();
            this._ledBuzzer = new IndicatorDot();
            this._dotCda1 = new IndicatorDot();
            this._dotCda2 = new IndicatorDot();
            this._dotVac1 = new IndicatorDot();
            this._dotVac2 = new IndicatorDot();
            this._dotVac3 = new IndicatorDot();
            this._dotVac4 = new IndicatorDot();
            this._dotIonizer = new IndicatorDot();
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
            this.lblHeader.Tag = "i18n:wi.opPanelStatus";
            this.lblHeader.Text = Lang.T("wi.opPanelStatus");
            this.lblHeader.TextAlign = ContentAlignment.MiddleLeft;

            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            this.contentLayout.Controls.Add(this.grpButtons, 0, 0);
            this.contentLayout.Controls.Add(this.grpLamps, 1, 0);
            this.contentLayout.Controls.Add(this.grpResources, 2, 0);
            this.contentLayout.Controls.Add(this.grpTower, 1, 1);
            this.contentLayout.Controls.Add(this.grpIonizer, 2, 1);
            this.contentLayout.Dock = DockStyle.Top;
            this.contentLayout.Height = 360;
            this.contentLayout.Padding = new Padding(8);
            this.contentLayout.RowCount = 2;
            this.contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            this.contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 190F));

            ConfigureGroup(this.grpButtons, "Operation Buttons (DI)", this.buttonLayout);
            ConfigureGroup(this.grpLamps, "Operation Lamps (DO)", this.lampLayout);
            ConfigureGroup(this.grpTower, "Tower Lamp + Buzzer", this.towerLayout);
            ConfigureGroup(this.grpResources, "Resource Sensors", this.resourceLayout);
            ConfigureGroup(this.grpIonizer, "Ionizer", this.ionizerLayout);

            AddIndicatorRow(this.buttonLayout, 0, this._dotStart, "START", Color.LimeGreen);
            AddIndicatorRow(this.buttonLayout, 1, this._dotStop, "STOP", Color.LimeGreen);
            AddIndicatorRow(this.buttonLayout, 2, this._dotReset, "RESET", Color.LimeGreen);
            AddIndicatorRow(this.buttonLayout, 3, this._dotEmgF, "EMG Front", Color.Red);
            AddIndicatorRow(this.buttonLayout, 4, this._dotEmgL, "EMG Left", Color.Red);
            AddIndicatorRow(this.buttonLayout, 5, this._dotEmgR, "EMG Rear", Color.Red);

            AddIndicatorRow(this.lampLayout, 0, this._ledStartLamp, "START LAMP", Color.LimeGreen);
            AddIndicatorRow(this.lampLayout, 1, this._ledStopLamp, "STOP LAMP", Color.LimeGreen);
            AddIndicatorRow(this.lampLayout, 2, this._ledResetLamp, "RESET LAMP", Color.LimeGreen);

            AddIndicatorRow(this.towerLayout, 0, this._tlRed, "RED", Color.Red);
            AddIndicatorRow(this.towerLayout, 1, this._tlYellow, "YELLOW", Color.Goldenrod);
            AddIndicatorRow(this.towerLayout, 2, this._tlGreen, "GREEN", Color.LimeGreen);
            AddIndicatorRow(this.towerLayout, 3, this._ledBuzzer, "BUZZER", Color.Magenta);

            AddIndicatorRow(this.resourceLayout, 0, this._dotCda1, "MAIN CDA 1", Color.Cyan);
            AddIndicatorRow(this.resourceLayout, 1, this._dotCda2, "MAIN CDA 2", Color.Cyan);
            AddIndicatorRow(this.resourceLayout, 2, this._dotVac1, "VACUUM 1", Color.LimeGreen);
            AddIndicatorRow(this.resourceLayout, 3, this._dotVac2, "VACUUM 2", Color.LimeGreen);
            AddIndicatorRow(this.resourceLayout, 4, this._dotVac3, "VACUUM 3", Color.LimeGreen);
            AddIndicatorRow(this.resourceLayout, 5, this._dotVac4, "VACUUM 4", Color.LimeGreen);

            AddIndicatorRow(this.ionizerLayout, 0, this._dotIonizer, "IONIZER OK", Color.Cyan);

            this.Controls.Add(this.rootLayout);
            this.Name = "OperationPanelStatusPage";
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
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(12, 18, 12, 8);
            layout.RowCount = 7;
            for (int i = 0; i < 7; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        }

        private static void AddIndicatorRow(TableLayoutPanel layout, int row, IndicatorDot dot, string text, Color onColor)
        {
            dot.Dock = DockStyle.Fill;
            dot.Margin = new Padding(3, 6, 3, 6);
            dot.OnColor = onColor;

            var label = new Label
            {
                Dock = DockStyle.Fill,
                Font = UiTheme.ValueFont,
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };

            layout.Controls.Add(dot, 0, row);
            layout.Controls.Add(label, 1, row);
        }
    }
}
