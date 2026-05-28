using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class PlateStatusPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private GroupBox grpNg;
        private GroupBox grpGood;
        private TableLayoutPanel ngLayout;
        private TableLayoutPanel goodLayout;
        private TableLayoutPanel ngSlotLayout;
        private TableLayoutPanel goodSlotLayout;
        private FlowLayoutPanel actionLayout;
        private Label _lblNgCount;
        private Label _lblGoodCount;
        private Label[] _ngSlots;
        private Label[] _goodSlots;
        private Button btnReset;

        private void InitializeComponent()
        {
            this.rootLayout = new TableLayoutPanel();
            this.lblHeader = new Label();
            this.contentLayout = new TableLayoutPanel();
            this.grpNg = new GroupBox();
            this.grpGood = new GroupBox();
            this.ngLayout = new TableLayoutPanel();
            this.goodLayout = new TableLayoutPanel();
            this.ngSlotLayout = new TableLayoutPanel();
            this.goodSlotLayout = new TableLayoutPanel();
            this.actionLayout = new FlowLayoutPanel();
            this._lblNgCount = new Label();
            this._lblGoodCount = new Label();
            this.btnReset = new Button();
            this._ngSlots = new Label[SLOTS_PER_PLATE];
            this._goodSlots = new Label[SLOTS_PER_PLATE];
            this.SuspendLayout();

            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 1);
            this.rootLayout.Controls.Add(this.actionLayout, 0, 2);
            this.rootLayout.Dock = DockStyle.Fill;
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 260F));
            this.rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));

            this.lblHeader.BackColor = UiTheme.StatusBarBg;
            this.lblHeader.Dock = DockStyle.Fill;
            this.lblHeader.Font = UiTheme.SectionFont;
            this.lblHeader.ForeColor = UiTheme.StatusBarFg;
            this.lblHeader.Padding = new Padding(10, 0, 0, 0);
            this.lblHeader.Tag = "i18n:wi.plateStatus";
            this.lblHeader.Text = Lang.T("wi.plateStatus");
            this.lblHeader.TextAlign = ContentAlignment.MiddleLeft;

            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.contentLayout.Controls.Add(this.grpNg, 0, 0);
            this.contentLayout.Controls.Add(this.grpGood, 1, 0);
            this.contentLayout.Dock = DockStyle.Fill;
            this.contentLayout.Padding = new Padding(8);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            ConfigurePlateGroup(this.grpNg, this.ngLayout, this._lblNgCount, this.ngSlotLayout, "NG PLATE", Color.LightCoral);
            ConfigurePlateGroup(this.grpGood, this.goodLayout, this._lblGoodCount, this.goodSlotLayout, "GOOD PLATE", Color.LightGreen);
            AddSlots(this.ngSlotLayout, this._ngSlots);
            AddSlots(this.goodSlotLayout, this._goodSlots);

            this.actionLayout.Dock = DockStyle.Fill;
            this.actionLayout.FlowDirection = FlowDirection.LeftToRight;
            this.actionLayout.Padding = new Padding(12);

            this.btnReset.BackColor = Color.LightYellow;
            this.btnReset.FlatStyle = FlatStyle.Flat;
            this.btnReset.Font = UiTheme.SectionFont;
            this.btnReset.Height = 50;
            this.btnReset.Text = "PLATE RESET";
            this.btnReset.UseVisualStyleBackColor = false;
            this.btnReset.Width = 180;
            this.actionLayout.Controls.Add(this.btnReset);

            this.Controls.Add(this.rootLayout);
            this.Name = "PlateStatusPage";
            this.Size = new Size(1400, 900);
            this.ResumeLayout(false);
        }

        private static void ConfigurePlateGroup(GroupBox group, TableLayoutPanel layout, Label countLabel, TableLayoutPanel slotLayout, string title, Color countColor)
        {
            group.Controls.Add(layout);
            group.Dock = DockStyle.Fill;
            group.Font = UiTheme.SectionFont;
            group.Text = title;

            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(countLabel, 0, 0);
            layout.Controls.Add(slotLayout, 0, 1);
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(8, 18, 8, 8);
            layout.RowCount = 2;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            countLabel.BackColor = countColor;
            countLabel.Dock = DockStyle.Fill;
            countLabel.Font = new Font("Consolas", 11F, FontStyle.Bold);
            countLabel.Text = "0 / 25";
            countLabel.TextAlign = ContentAlignment.MiddleCenter;

            slotLayout.ColumnCount = 5;
            slotLayout.Dock = DockStyle.Fill;
            slotLayout.RowCount = 5;
            for (int i = 0; i < 5; i++)
            {
                slotLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
                slotLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            }
        }

        private static void AddSlots(TableLayoutPanel layout, Label[] slots)
        {
            for (int i = 0; i < SLOTS_PER_PLATE; i++)
            {
                var label = new Label
                {
                    BackColor = Color.LightGray,
                    BorderStyle = BorderStyle.FixedSingle,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 8F, FontStyle.Bold),
                    ForeColor = Color.Black,
                    Margin = new Padding(1),
                    Text = (i + 1).ToString("D2"),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                slots[i] = label;
                layout.Controls.Add(label, i % 5, i / 5);
            }
        }
    }
}
