using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class OutputStagePage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private TableLayoutPanel leftLayout;
        private TableLayoutPanel materialPanel;
        private TableLayoutPanel materialHeaderLayout;
        private GroupBox grpState;
        private GroupBox grpCounters;
        private GroupBox grpCylinder;
        private GroupBox grpInfo;
        private MaterialDetailView materialDetailView;
        private TableLayoutPanel stateLayout;
        private TableLayoutPanel counterLayout;
        private TableLayoutPanel cylinderLayout;
        private TableLayoutPanel infoLayout;
        private FlowLayoutPanel actionPanel;
        private Label lblMaterialTitle;
        private RadioButton rdoGoodMaterial;
        private RadioButton rdoNgMaterial;
        private Label lblGoodExistTitle;
        private Label lblGoodExistValue;
        private Label lblGoodStateTitle;
        private Label lblGoodStateValue;
        private Label lblNgExistTitle;
        private Label lblNgExistValue;
        private Label lblNgStateTitle;
        private Label lblNgStateValue;
        private Label lblGoodCountTitle;
        private Label lblGoodCountValue;
        private Label lblNgCountTitle;
        private Label lblNgCountValue;
        private Label lblTotalCountTitle;
        private Label lblTotalCountValue;
        private Label lblGoodGuideTitle;
        private Label lblGoodGuideValue;
        private Label lblGoodClampTitle;
        private Label lblGoodClampValue;
        private Label lblNgGuideTitle;
        private Label lblNgGuideValue;
        private Label lblNgClampTitle;
        private Label lblNgClampValue;
        private TableLayoutPanel goodYPanel;
        private Label lblGoodYTitle;
        private Label lblGoodYValue;
        private TableLayoutPanel goodZPanel;
        private Label lblGoodZTitle;
        private Label lblGoodZValue;
        private TableLayoutPanel ngYPanel;
        private Label lblNgYTitle;
        private Label lblNgYValue;
        private TableLayoutPanel visionXPanel;
        private Label lblVisionXTitle;
        private Label lblVisionXValue;
        private ActionButton btnStageReady;
        private ActionButton btnNgStageReady;
        private ActionButton btnGoodProcess;
        private ActionButton btnNgProcess;
        private ActionButton btnGoodReceive;
        private ActionButton btnNgReceive;
        private ActionButton btnGoodUnload;
        private ActionButton btnNgUnload;
        private ActionButton btnInspect;
        private ActionButton btnStageInit;
        private ActionButton btnStop;

        private void InitializeComponent()
        {
            this.rootLayout = new TableLayoutPanel();
            this.lblHeader = new Label();
            this.contentLayout = new TableLayoutPanel();
            this.leftLayout = new TableLayoutPanel();
            this.materialPanel = new TableLayoutPanel();
            this.materialHeaderLayout = new TableLayoutPanel();
            this.grpState = new GroupBox();
            this.grpCounters = new GroupBox();
            this.grpCylinder = new GroupBox();
            this.grpInfo = new GroupBox();
            this.materialDetailView = new MaterialDetailView();
            this.stateLayout = new TableLayoutPanel();
            this.counterLayout = new TableLayoutPanel();
            this.cylinderLayout = new TableLayoutPanel();
            this.infoLayout = new TableLayoutPanel();
            this.actionPanel = new FlowLayoutPanel();
            this.lblMaterialTitle = new Label();
            this.rdoGoodMaterial = new RadioButton();
            this.rdoNgMaterial = new RadioButton();
            this.lblGoodExistTitle = new Label();
            this.lblGoodExistValue = new Label();
            this.lblGoodStateTitle = new Label();
            this.lblGoodStateValue = new Label();
            this.lblNgExistTitle = new Label();
            this.lblNgExistValue = new Label();
            this.lblNgStateTitle = new Label();
            this.lblNgStateValue = new Label();
            this.lblGoodCountTitle = new Label();
            this.lblGoodCountValue = new Label();
            this.lblNgCountTitle = new Label();
            this.lblNgCountValue = new Label();
            this.lblTotalCountTitle = new Label();
            this.lblTotalCountValue = new Label();
            this.lblGoodGuideTitle = new Label();
            this.lblGoodGuideValue = new Label();
            this.lblGoodClampTitle = new Label();
            this.lblGoodClampValue = new Label();
            this.lblNgGuideTitle = new Label();
            this.lblNgGuideValue = new Label();
            this.lblNgClampTitle = new Label();
            this.lblNgClampValue = new Label();
            this.goodYPanel = new TableLayoutPanel();
            this.lblGoodYTitle = new Label();
            this.lblGoodYValue = new Label();
            this.goodZPanel = new TableLayoutPanel();
            this.lblGoodZTitle = new Label();
            this.lblGoodZValue = new Label();
            this.ngYPanel = new TableLayoutPanel();
            this.lblNgYTitle = new Label();
            this.lblNgYValue = new Label();
            this.visionXPanel = new TableLayoutPanel();
            this.lblVisionXTitle = new Label();
            this.lblVisionXValue = new Label();
            this.btnStageReady = new ActionButton();
            this.btnNgStageReady = new ActionButton();
            this.btnGoodProcess = new ActionButton();
            this.btnNgProcess = new ActionButton();
            this.btnGoodReceive = new ActionButton();
            this.btnNgReceive = new ActionButton();
            this.btnGoodUnload = new ActionButton();
            this.btnNgUnload = new ActionButton();
            this.btnInspect = new ActionButton();
            this.btnStageInit = new ActionButton();
            this.btnStop = new ActionButton();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.materialPanel.SuspendLayout();
            this.materialHeaderLayout.SuspendLayout();
            this.grpState.SuspendLayout();
            this.grpCounters.SuspendLayout();
            this.grpCylinder.SuspendLayout();
            this.grpInfo.SuspendLayout();
            this.stateLayout.SuspendLayout();
            this.counterLayout.SuspendLayout();
            this.cylinderLayout.SuspendLayout();
            this.infoLayout.SuspendLayout();
            this.goodYPanel.SuspendLayout();
            this.goodZPanel.SuspendLayout();
            this.ngYPanel.SuspendLayout();
            this.visionXPanel.SuspendLayout();
            this.actionPanel.SuspendLayout();
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
            this.lblHeader.Text = "OUTPUT STAGE";
            this.lblHeader.TextAlign = ContentAlignment.MiddleLeft;

            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            this.contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            this.contentLayout.Controls.Add(this.leftLayout, 0, 0);
            this.contentLayout.Controls.Add(this.materialPanel, 1, 0);
            this.contentLayout.Dock = DockStyle.Fill;
            this.contentLayout.Padding = new Padding(8);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.leftLayout.ColumnCount = 3;
            this.leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            this.leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27F));
            this.leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            this.leftLayout.Controls.Add(this.grpState, 0, 0);
            this.leftLayout.Controls.Add(this.grpCounters, 1, 0);
            this.leftLayout.Controls.Add(this.grpCylinder, 2, 0);
            this.leftLayout.Controls.Add(this.grpInfo, 0, 1);
            this.leftLayout.Dock = DockStyle.Fill;
            this.leftLayout.RowCount = 2;
            this.leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 220F));
            this.leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.leftLayout.SetColumnSpan(this.grpInfo, 3);

            this.materialPanel.ColumnCount = 1;
            this.materialPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.materialPanel.Controls.Add(this.materialHeaderLayout, 0, 0);
            this.materialPanel.Controls.Add(this.materialDetailView, 0, 1);
            this.materialPanel.Dock = DockStyle.Fill;
            this.materialPanel.RowCount = 2;
            this.materialPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            this.materialPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.materialHeaderLayout.ColumnCount = 4;
            this.materialHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.materialHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            this.materialHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            this.materialHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 8F));
            this.materialHeaderLayout.Controls.Add(this.lblMaterialTitle, 0, 0);
            this.materialHeaderLayout.Controls.Add(this.rdoGoodMaterial, 1, 0);
            this.materialHeaderLayout.Controls.Add(this.rdoNgMaterial, 2, 0);
            this.materialHeaderLayout.Dock = DockStyle.Fill;

            this.lblMaterialTitle.Dock = DockStyle.Fill;
            this.lblMaterialTitle.Font = UiTheme.SectionFont;
            this.lblMaterialTitle.Text = "MATERIAL";
            this.lblMaterialTitle.TextAlign = ContentAlignment.MiddleLeft;
            this.rdoGoodMaterial.Appearance = Appearance.Button;
            this.rdoGoodMaterial.Checked = true;
            this.rdoGoodMaterial.Dock = DockStyle.Fill;
            this.rdoGoodMaterial.Text = "GOOD";
            this.rdoGoodMaterial.TextAlign = ContentAlignment.MiddleCenter;
            this.rdoNgMaterial.Appearance = Appearance.Button;
            this.rdoNgMaterial.Dock = DockStyle.Fill;
            this.rdoNgMaterial.Text = "NG";
            this.rdoNgMaterial.TextAlign = ContentAlignment.MiddleCenter;

            ConfigureGroup(this.grpState, this.stateLayout, "WORK INFO");
            ConfigureGroup(this.grpCounters, this.counterLayout, "COUNTER");
            ConfigureGroup(this.grpCylinder, this.cylinderLayout, "CYLINDER INFO");
            ConfigureGroup(this.grpInfo, this.infoLayout, "INFO");

            ConfigureTwoColumnLayout(this.stateLayout, 6);
            AddPair(this.stateLayout, this.lblGoodExistTitle, this.lblGoodExistValue, 0, "GOOD EXIST", "EMPTY");
            AddPair(this.stateLayout, this.lblGoodStateTitle, this.lblGoodStateValue, 1, "GOOD STATE", "INCOMPLETE");
            AddPair(this.stateLayout, this.lblNgExistTitle, this.lblNgExistValue, 2, "NG EXIST", "EMPTY");
            AddPair(this.stateLayout, this.lblNgStateTitle, this.lblNgStateValue, 3, "NG STATE", "INCOMPLETE");

            ConfigureTwoColumnLayout(this.counterLayout, 4);
            AddPair(this.counterLayout, this.lblGoodCountTitle, this.lblGoodCountValue, 0, "GOOD COUNT", "0 ea");
            AddPair(this.counterLayout, this.lblNgCountTitle, this.lblNgCountValue, 1, "NG COUNT", "0 ea");
            AddPair(this.counterLayout, this.lblTotalCountTitle, this.lblTotalCountValue, 2, "TOTAL COUNT", "0 ea");

            ConfigureTwoColumnLayout(this.cylinderLayout, 6);
            AddPair(this.cylinderLayout, this.lblGoodGuideTitle, this.lblGoodGuideValue, 0, "GOOD GUIDE", "--");
            AddPair(this.cylinderLayout, this.lblGoodClampTitle, this.lblGoodClampValue, 1, "GOOD CLAMP", "--");
            AddPair(this.cylinderLayout, this.lblNgGuideTitle, this.lblNgGuideValue, 2, "NG GUIDE", "--");
            AddPair(this.cylinderLayout, this.lblNgClampTitle, this.lblNgClampValue, 3, "NG CLAMP", "--");

            this.infoLayout.ColumnCount = 3;
            this.infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            this.infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            this.infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            this.infoLayout.Controls.Add(this.goodYPanel, 0, 0);
            this.infoLayout.Controls.Add(this.goodZPanel, 1, 0);
            this.infoLayout.Controls.Add(this.ngYPanel, 0, 1);
            this.infoLayout.Controls.Add(this.visionXPanel, 1, 1);
            this.infoLayout.Dock = DockStyle.Fill;
            this.infoLayout.Padding = new Padding(18, 28, 18, 18);
            this.infoLayout.RowCount = 4;
            this.infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            this.infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            this.infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            this.infoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            ConfigureAxisPanel(this.goodYPanel, this.lblGoodYTitle, this.lblGoodYValue, "GOOD STAGE Y");
            ConfigureAxisPanel(this.goodZPanel, this.lblGoodZTitle, this.lblGoodZValue, "GOOD STAGE Z");
            ConfigureAxisPanel(this.ngYPanel, this.lblNgYTitle, this.lblNgYValue, "NG STAGE Y");
            ConfigureAxisPanel(this.visionXPanel, this.lblVisionXTitle, this.lblVisionXValue, "VISION AXIS X");

            this.materialDetailView.BackColor = UiTheme.OptionPanelBg;
            this.materialDetailView.Dock = DockStyle.Fill;
            this.materialDetailView.Margin = new Padding(4);
            this.materialDetailView.Name = "materialDetailView";

            this.actionPanel.BackColor = UiTheme.MainBg;
            this.actionPanel.Dock = DockStyle.Fill;
            this.actionPanel.FlowDirection = FlowDirection.LeftToRight;
            this.actionPanel.Padding = new Padding(18, 10, 18, 10);
            this.actionPanel.WrapContents = false;
            this.actionPanel.Controls.Add(this.btnStageReady);
            this.actionPanel.Controls.Add(this.btnNgStageReady);
            this.actionPanel.Controls.Add(this.btnGoodProcess);
            this.actionPanel.Controls.Add(this.btnNgProcess);
            this.actionPanel.Controls.Add(this.btnGoodReceive);
            this.actionPanel.Controls.Add(this.btnNgReceive);
            this.actionPanel.Controls.Add(this.btnGoodUnload);
            this.actionPanel.Controls.Add(this.btnNgUnload);
            this.actionPanel.Controls.Add(this.btnInspect);
            this.actionPanel.Controls.Add(this.btnStageInit);
            this.actionPanel.Controls.Add(this.btnStop);

            ConfigureStageActionButton(this.btnStageReady, "GOOD LOAD", 128);
            ConfigureStageActionButton(this.btnNgStageReady, "NG LOAD", 128);
            ConfigureStageActionButton(this.btnGoodProcess, "GOOD PROCESS", 128);
            ConfigureStageActionButton(this.btnNgProcess, "NG PROCESS", 128);
            ConfigureStageActionButton(this.btnGoodReceive, "RECEIVE GOOD", 128);
            ConfigureStageActionButton(this.btnNgReceive, "RECEIVE NG", 128);
            ConfigureStageActionButton(this.btnGoodUnload, "GOOD UNLOAD", 128);
            ConfigureStageActionButton(this.btnNgUnload, "NG UNLOAD", 128);
            ConfigureStageActionButton(this.btnInspect, "INSPECT", 128);
            ConfigureStageActionButton(this.btnStageInit, "AVOID", 128);
            ConfigureStageActionButton(this.btnStop, "STOP", 140);
            this.btnStop.BackColor = Color.FromArgb(0xA8, 0x2D, 0x2D);

            this.Controls.Add(this.rootLayout);
            this.Name = "OutputStagePage";
            this.Size = new Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.materialPanel.ResumeLayout(false);
            this.materialHeaderLayout.ResumeLayout(false);
            this.grpState.ResumeLayout(false);
            this.grpCounters.ResumeLayout(false);
            this.grpCylinder.ResumeLayout(false);
            this.grpInfo.ResumeLayout(false);
            this.stateLayout.ResumeLayout(false);
            this.counterLayout.ResumeLayout(false);
            this.cylinderLayout.ResumeLayout(false);
            this.infoLayout.ResumeLayout(false);
            this.goodYPanel.ResumeLayout(false);
            this.goodZPanel.ResumeLayout(false);
            this.ngYPanel.ResumeLayout(false);
            this.visionXPanel.ResumeLayout(false);
            this.actionPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private static void ConfigureGroup(GroupBox group, Control child, string title)
        {
            group.BackColor = UiTheme.OptionPanelBg;
            group.Controls.Add(child);
            group.Dock = DockStyle.Fill;
            group.Font = UiTheme.SectionFont;
            group.Text = title;
        }

        private static void ConfigureTwoColumnLayout(TableLayoutPanel layout, int rows)
        {
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(12, 18, 12, 12);
            layout.RowCount = rows;
            for (int i = 0; i < rows; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        }

        private static void AddPair(TableLayoutPanel layout, Label title, Label value, int row, string titleText, string valueText)
        {
            title.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            title.BorderStyle = BorderStyle.FixedSingle;
            title.Dock = DockStyle.Fill;
            title.Font = UiTheme.ButtonFont;
            title.Padding = new Padding(8, 0, 0, 0);
            title.Text = titleText;
            title.TextAlign = ContentAlignment.MiddleLeft;
            value.BackColor = Color.White;
            value.BorderStyle = BorderStyle.FixedSingle;
            value.Dock = DockStyle.Fill;
            value.Font = UiTheme.ValueFont;
            value.Text = valueText;
            value.TextAlign = ContentAlignment.MiddleCenter;
            layout.Controls.Add(title, 0, row);
            layout.Controls.Add(value, 1, row);
        }

        private static void ConfigureAxisPanel(TableLayoutPanel panel, Label title, Label value, string caption)
        {
            panel.ColumnCount = 1;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.Controls.Add(title, 0, 0);
            panel.Controls.Add(value, 0, 1);
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(4);
            panel.RowCount = 2;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            title.BackColor = Color.Black;
            title.Dock = DockStyle.Fill;
            title.Font = UiTheme.ButtonFont;
            title.ForeColor = Color.White;
            title.Padding = new Padding(8, 0, 0, 0);
            title.Text = caption;
            title.TextAlign = ContentAlignment.MiddleLeft;
            value.BackColor = Color.White;
            value.BorderStyle = BorderStyle.FixedSingle;
            value.Dock = DockStyle.Fill;
            value.Font = UiTheme.ValueFont;
            value.Text = "0 um";
            value.TextAlign = ContentAlignment.MiddleRight;
        }

        private static void ConfigureStageActionButton(ActionButton button, string text, int width)
        {
            button.Font = UiTheme.ButtonFont;
            button.Height = 64;
            button.Margin = new Padding(6);
            button.Text = text;
            button.Width = width;
        }
    }
}
