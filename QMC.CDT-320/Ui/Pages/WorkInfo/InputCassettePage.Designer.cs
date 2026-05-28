using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class InputCassettePage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel topLayout;
        private TableLayoutPanel contentLayout;
        private GroupBox grpSlotState;
        private GroupBox grpLifter;
        private GroupBox grpLegend;
        private TableLayoutPanel slotStateLayout;
        private TableLayoutPanel lifterLayout;
        private FlowLayoutPanel actionsLayout;
        private Label _lifterPosLabel;
        private Label[] _slotLeds;
        private Label[] _slotIndexLbls;
        private Button btnPrev;
        private Button btnNext;
        private Button btnInit;
        private Button btnReady;
        private ActionButton btnMap;
        private ActionButton btnLoad;
        private ActionButton btnUnload;

        private void InitializeComponent()
        {
            this.rootLayout = new TableLayoutPanel();
            this.lblHeader = new Label();
            this.topLayout = new TableLayoutPanel();
            this.contentLayout = new TableLayoutPanel();
            this.grpSlotState = new GroupBox();
            this.grpLifter = new GroupBox();
            this.grpLegend = new GroupBox();
            this.slotStateLayout = new TableLayoutPanel();
            this.lifterLayout = new TableLayoutPanel();
            this.actionsLayout = new FlowLayoutPanel();
            this._lifterPosLabel = new Label();
            this._slotLeds = new Label[SLOT_COUNT_UI];
            this._slotIndexLbls = new Label[SLOT_COUNT_UI];
            this.btnPrev = new Button();
            this.btnNext = new Button();
            this.btnInit = new Button();
            this.btnReady = new Button();
            this.btnMap = new ActionButton();
            this.btnLoad = new ActionButton();
            this.btnUnload = new ActionButton();
            this.SuspendLayout();

            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.topLayout, 0, 1);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 2);
            this.rootLayout.Controls.Add(this.actionsLayout, 0, 3);
            this.rootLayout.Dock = DockStyle.Fill;
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));

            this.lblHeader.BackColor = UiTheme.StatusBarBg;
            this.lblHeader.Dock = DockStyle.Fill;
            this.lblHeader.Font = UiTheme.SectionFont;
            this.lblHeader.ForeColor = UiTheme.StatusBarFg;
            this.lblHeader.Padding = new Padding(10, 0, 0, 0);
            this.lblHeader.Tag = "i18n:common.info";
            this.lblHeader.Text = Lang.T("common.info");
            this.lblHeader.TextAlign = ContentAlignment.MiddleLeft;

            this.topLayout.ColumnCount = 4;
            this.topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
            this.topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            this.topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            this.topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.topLayout.Dock = DockStyle.Fill;
            this.topLayout.Padding = new Padding(8);
            this.topLayout.RowCount = 1;
            this.topLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.topLayout.Controls.Add(BuildAxisBlock("LIFTER AXIS Z", this._lifterPosLabel), 0, 0);
            this.topLayout.Controls.Add(BuildIoBlock("LIFTER CASSETTE CHECK #1"), 1, 0);
            this.topLayout.Controls.Add(BuildIoBlock("LIFTER CASSETTE CHECK #2"), 2, 0);
            this.topLayout.Controls.Add(this.grpLegend, 3, 0);

            ConfigureLegendGroup();

            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340F));
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390F));
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.contentLayout.Controls.Add(this.grpSlotState, 0, 0);
            this.contentLayout.Controls.Add(this.grpLifter, 1, 0);
            this.contentLayout.Dock = DockStyle.Fill;
            this.contentLayout.Padding = new Padding(12, 8, 12, 8);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.grpSlotState.BackColor = UiTheme.OptionPanelBg;
            this.grpSlotState.Controls.Add(this.slotStateLayout);
            this.grpSlotState.Dock = DockStyle.Top;
            this.grpSlotState.Font = UiTheme.SectionFont;
            this.grpSlotState.Height = 210;
            this.grpSlotState.Text = Lang.T("wi.slotState");

            this.slotStateLayout.ColumnCount = 2;
            this.slotStateLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.slotStateLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.slotStateLayout.Dock = DockStyle.Fill;
            this.slotStateLayout.Padding = new Padding(8, 20, 8, 8);
            this.slotStateLayout.RowCount = 5;
            this.slotStateLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.slotStateLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.slotStateLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            this.slotStateLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            this.slotStateLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            AddSlotStateRow(this.slotStateLayout, 0, "Slot No", "BIN 1");
            AddSlotStateRow(this.slotStateLayout, 1, "State", "EMPTY");
            ConfigureSmallButton(this.btnPrev, Lang.T("common.prev"), "i18n:common.prev");
            ConfigureSmallButton(this.btnNext, Lang.T("common.next"), "i18n:common.next");
            ConfigureSmallButton(this.btnInit, Lang.T("wi.lifterInit"), "i18n:wi.lifterInit");
            ConfigureSmallButton(this.btnReady, Lang.T("wi.lifterReady"), "i18n:wi.lifterReady");
            this.slotStateLayout.Controls.Add(this.btnPrev, 0, 2);
            this.slotStateLayout.Controls.Add(this.btnNext, 1, 2);
            this.slotStateLayout.Controls.Add(this.btnInit, 0, 3);
            this.slotStateLayout.Controls.Add(this.btnReady, 1, 3);

            this.grpLifter.BackColor = UiTheme.OptionPanelBg;
            this.grpLifter.Controls.Add(this.lifterLayout);
            this.grpLifter.Dock = DockStyle.Top;
            this.grpLifter.Font = UiTheme.SectionFont;
            this.grpLifter.Height = 430;
            this.grpLifter.Text = "LIFTER";

            this.lifterLayout.ColumnCount = 3;
            this.lifterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            this.lifterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42F));
            this.lifterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.lifterLayout.Dock = DockStyle.Fill;
            this.lifterLayout.Padding = new Padding(8, 20, 8, 8);
            this.lifterLayout.RowCount = SLOT_COUNT_UI;
            for (int i = 0; i < SLOT_COUNT_UI; i++)
            {
                this.lifterLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / SLOT_COUNT_UI));
                AddLifterSlotRow(i);
            }

            this.actionsLayout.BackColor = UiTheme.MainBg;
            this.actionsLayout.Dock = DockStyle.Fill;
            this.actionsLayout.FlowDirection = FlowDirection.LeftToRight;
            this.actionsLayout.Padding = new Padding(12);
            ConfigureAction(this.btnMap, Lang.T("wi.liftWaferMapping"), "i18n:wi.liftWaferMapping", 180);
            ConfigureAction(this.btnLoad, Lang.T("wi.liftWaferLoading"), "i18n:wi.liftWaferLoading", 180);
            ConfigureAction(this.btnUnload, Lang.T("wi.liftWaferUnloading"), "i18n:wi.liftWaferUnloading", 180);
            this.actionsLayout.Controls.Add(this.btnMap);
            this.actionsLayout.Controls.Add(this.btnLoad);
            this.actionsLayout.Controls.Add(this.btnUnload);

            this.Controls.Add(this.rootLayout);
            this.Name = "InputCassettePage";
            this.Size = new Size(1400, 900);
            this.ResumeLayout(false);
        }

        private static Control BuildAxisBlock(string title, Label valueLabel)
        {
            var panel = new TableLayoutPanel
            {
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                RowCount = 2
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(new Label
            {
                BackColor = Color.Black,
                Dock = DockStyle.Fill,
                Font = UiTheme.SectionFont,
                ForeColor = Color.White,
                Padding = new Padding(6, 0, 0, 0),
                Text = title,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            valueLabel.BackColor = Color.White;
            valueLabel.BorderStyle = BorderStyle.FixedSingle;
            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Font = new Font("Consolas", 10F);
            valueLabel.Padding = new Padding(0, 0, 6, 0);
            valueLabel.Text = "0.000 mm";
            valueLabel.TextAlign = ContentAlignment.MiddleRight;
            panel.Controls.Add(valueLabel, 0, 1);
            return panel;
        }

        private static Control BuildIoBlock(string title)
        {
            var panel = new TableLayoutPanel
            {
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                RowCount = 1
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            var dot = new IndicatorDot { Dock = DockStyle.Fill, Margin = new Padding(6, 26, 6, 26), OnColor = Color.LimeGreen };
            var label = new Label
            {
                BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0),
                Dock = DockStyle.Fill,
                Font = UiTheme.ValueFont,
                Padding = new Padding(6, 0, 0, 0),
                Text = title,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(dot, 0, 0);
            panel.Controls.Add(label, 1, 0);
            return panel;
        }

        private void ConfigureLegendGroup()
        {
            this.grpLegend.Dock = DockStyle.Fill;
            this.grpLegend.Font = UiTheme.SectionFont;
            this.grpLegend.Text = "Legend";
            var layout = new TableLayoutPanel
            {
                ColumnCount = 4,
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 18, 8, 8),
                RowCount = 2
            };
            for (int i = 0; i < 4; i++) layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            AddLegend(layout, 0, 0, Color.Cyan, Lang.T("wi.legend.ready"));
            AddLegend(layout, 1, 0, Color.LimeGreen, Lang.T("wi.legend.empty"));
            AddLegend(layout, 2, 0, Color.Orange, Lang.T("wi.legend.working"));
            AddLegend(layout, 3, 0, Color.Red, Lang.T("wi.legend.finish"));
            AddLegend(layout, 0, 1, Color.Navy, Lang.T("wi.legend.workReady"));
            this.grpLegend.Controls.Add(layout);
        }

        private static void AddLegend(TableLayoutPanel layout, int col, int row, Color color, string text)
        {
            var cell = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, RowCount = 1 };
            cell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
            cell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            cell.Controls.Add(new Label { BackColor = color, Dock = DockStyle.Fill, Margin = new Padding(2, 7, 2, 7) }, 0, 0);
            cell.Controls.Add(new Label { Dock = DockStyle.Fill, Font = UiTheme.ValueFont, Text = text, TextAlign = ContentAlignment.MiddleLeft }, 1, 0);
            layout.Controls.Add(cell, col, row);
        }

        private static void AddSlotStateRow(TableLayoutPanel layout, int row, string key, string value)
        {
            layout.Controls.Add(new Label { Dock = DockStyle.Fill, Font = UiTheme.ButtonFont, Text = key, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
            layout.Controls.Add(new Label { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Fill, Font = UiTheme.ValueFont, Text = value, TextAlign = ContentAlignment.MiddleCenter }, 1, row);
        }

        private void AddLifterSlotRow(int i)
        {
            var nameLbl = new Label
            {
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 8F),
                Text = "SLOT-" + (i + 1).ToString("D2"),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var idxLbl = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 8F, FontStyle.Bold),
                Text = (i + 1).ToString("D2"),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var ledLbl = new Label
            {
                BackColor = Color.LightGray,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Margin = new Padding(2)
            };
            this._slotIndexLbls[i] = idxLbl;
            this._slotLeds[i] = ledLbl;
            this.lifterLayout.Controls.Add(nameLbl, 0, i);
            this.lifterLayout.Controls.Add(idxLbl, 1, i);
            this.lifterLayout.Controls.Add(ledLbl, 2, i);
        }

        private static void ConfigureSmallButton(Button button, string text, string tag)
        {
            button.Dock = DockStyle.Fill;
            button.FlatStyle = FlatStyle.Flat;
            button.Font = UiTheme.ButtonFont;
            button.Tag = tag;
            button.Text = text;
        }

        private static void ConfigureAction(ActionButton button, string text, string tag, int width)
        {
            button.Margin = new Padding(6);
            button.Tag = tag;
            button.Text = text;
            button.Width = width;
        }
    }
}
