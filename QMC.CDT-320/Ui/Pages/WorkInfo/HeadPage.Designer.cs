using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

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
        private Label lblHead1Title;
        private Label lblHead1Value;
        private Label lblHead2Title;
        private Label lblHead2Value;
        private Label lblColletChangeTitle;
        private Label lblColletChangeValue;
        private Label lblAutoPosTitle;
        private Label lblAutoPosValue;
        private Label lblColletCleaningTitle;
        private Label lblColletCleaningValue;
        private Label lblColletCheckTitle;
        private Label lblColletCheckValue;
        private Label lblPickFailTitle;
        private Label lblPickFailValue;
        private Label lblPlaceFailTitle;
        private Label lblPlaceFailValue;
        private Label lblCollet1UseTitle;
        private Label lblCollet1UseValue;
        private Label lblCollet2UseTitle;
        private Label lblCollet2UseValue;
        private TableLayoutPanel headAxisTPanel;
        private Label lblHeadAxisTTitle;
        private Label lblHeadAxisTValue;
        private TableLayoutPanel headVacuum1Panel;
        private IndicatorDot dotHeadVacuum1;
        private Label lblHeadVacuum1;
        private TableLayoutPanel headVacuum2Panel;
        private IndicatorDot dotHeadVacuum2;
        private Label lblHeadVacuum2;
        private TableLayoutPanel headBlow1Panel;
        private IndicatorDot dotHeadBlow1;
        private Label lblHeadBlow1;
        private TableLayoutPanel headBlow2Panel;
        private IndicatorDot dotHeadBlow2;
        private Label lblHeadBlow2;

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
            this.lblHead1Title = new Label();
            this.lblHead1Value = new Label();
            this.lblHead2Title = new Label();
            this.lblHead2Value = new Label();
            this.lblColletChangeTitle = new Label();
            this.lblColletChangeValue = new Label();
            this.lblAutoPosTitle = new Label();
            this.lblAutoPosValue = new Label();
            this.lblColletCleaningTitle = new Label();
            this.lblColletCleaningValue = new Label();
            this.lblColletCheckTitle = new Label();
            this.lblColletCheckValue = new Label();
            this.lblPickFailTitle = new Label();
            this.lblPickFailValue = new Label();
            this.lblPlaceFailTitle = new Label();
            this.lblPlaceFailValue = new Label();
            this.lblCollet1UseTitle = new Label();
            this.lblCollet1UseValue = new Label();
            this.lblCollet2UseTitle = new Label();
            this.lblCollet2UseValue = new Label();
            this.headAxisTPanel = new TableLayoutPanel();
            this.lblHeadAxisTTitle = new Label();
            this.lblHeadAxisTValue = new Label();
            this.headVacuum1Panel = new TableLayoutPanel();
            this.dotHeadVacuum1 = new IndicatorDot();
            this.lblHeadVacuum1 = new Label();
            this.headVacuum2Panel = new TableLayoutPanel();
            this.dotHeadVacuum2 = new IndicatorDot();
            this.lblHeadVacuum2 = new Label();
            this.headBlow1Panel = new TableLayoutPanel();
            this.dotHeadBlow1 = new IndicatorDot();
            this.lblHeadBlow1 = new Label();
            this.headBlow2Panel = new TableLayoutPanel();
            this.dotHeadBlow2 = new IndicatorDot();
            this.lblHeadBlow2 = new Label();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.grpState.SuspendLayout();
            this.grpCounters.SuspendLayout();
            this.grpInfo.SuspendLayout();
            this.stateLayout.SuspendLayout();
            this.counterLayout.SuspendLayout();
            this.infoLayout.SuspendLayout();
            this.headAxisTPanel.SuspendLayout();
            this.headVacuum1Panel.SuspendLayout();
            this.headVacuum2Panel.SuspendLayout();
            this.headBlow1Panel.SuspendLayout();
            this.headBlow2Panel.SuspendLayout();
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
            this.lblHeader.Text = "FRONT HEAD";
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

            this.grpState.BackColor = UiTheme.OptionPanelBg;
            this.grpState.Controls.Add(this.stateLayout);
            this.grpState.Dock = DockStyle.Fill;
            this.grpState.Font = UiTheme.SectionFont;
            this.grpState.Text = "WORK INFO";

            this.grpCounters.BackColor = UiTheme.OptionPanelBg;
            this.grpCounters.Controls.Add(this.counterLayout);
            this.grpCounters.Dock = DockStyle.Fill;
            this.grpCounters.Font = UiTheme.SectionFont;
            this.grpCounters.Text = "COUNTER";

            this.grpInfo.BackColor = UiTheme.OptionPanelBg;
            this.grpInfo.Controls.Add(this.infoLayout);
            this.grpInfo.Dock = DockStyle.Fill;
            this.grpInfo.Font = UiTheme.SectionFont;
            this.grpInfo.Text = "INFO";

            this.stateLayout.ColumnCount = 2;
            this.stateLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            this.stateLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            this.stateLayout.Controls.Add(this.lblHead1Title, 0, 0);
            this.stateLayout.Controls.Add(this.lblHead1Value, 1, 0);
            this.stateLayout.Controls.Add(this.lblHead2Title, 0, 1);
            this.stateLayout.Controls.Add(this.lblHead2Value, 1, 1);
            this.stateLayout.Controls.Add(this.btnInitAll, 0, 2);
            this.stateLayout.Controls.Add(this.lblColletChangeTitle, 0, 3);
            this.stateLayout.Controls.Add(this.lblColletChangeValue, 1, 3);
            this.stateLayout.Controls.Add(this.lblAutoPosTitle, 0, 4);
            this.stateLayout.Controls.Add(this.lblAutoPosValue, 1, 4);
            this.stateLayout.Controls.Add(this.lblColletCleaningTitle, 0, 5);
            this.stateLayout.Controls.Add(this.lblColletCleaningValue, 1, 5);
            this.stateLayout.Controls.Add(this.lblColletCheckTitle, 0, 6);
            this.stateLayout.Controls.Add(this.lblColletCheckValue, 1, 6);
            this.stateLayout.Dock = DockStyle.Fill;
            this.stateLayout.Padding = new Padding(12, 18, 12, 12);
            this.stateLayout.RowCount = 8;
            this.stateLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.stateLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.stateLayout.SetColumnSpan(this.btnInitAll, 2);

            this.counterLayout.ColumnCount = 2;
            this.counterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            this.counterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            this.counterLayout.Controls.Add(this.lblPickFailTitle, 0, 0);
            this.counterLayout.Controls.Add(this.lblPickFailValue, 1, 0);
            this.counterLayout.Controls.Add(this.lblPlaceFailTitle, 0, 1);
            this.counterLayout.Controls.Add(this.lblPlaceFailValue, 1, 1);
            this.counterLayout.Controls.Add(this.lblCollet1UseTitle, 0, 2);
            this.counterLayout.Controls.Add(this.lblCollet1UseValue, 1, 2);
            this.counterLayout.Controls.Add(this.lblCollet2UseTitle, 0, 3);
            this.counterLayout.Controls.Add(this.lblCollet2UseValue, 1, 3);
            this.counterLayout.Controls.Add(this.btnCountClear, 0, 4);
            this.counterLayout.Dock = DockStyle.Fill;
            this.counterLayout.Padding = new Padding(12, 18, 12, 12);
            this.counterLayout.RowCount = 8;
            this.counterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.counterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.counterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.counterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.counterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.counterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.counterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.counterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            this.counterLayout.SetColumnSpan(this.btnCountClear, 2);

            this.infoLayout.ColumnCount = 4;
            this.infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            this.infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            this.infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            this.infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            this.infoLayout.Controls.Add(this.headAxisTPanel, 0, 0);
            this.infoLayout.Controls.Add(this.headVacuum1Panel, 0, 1);
            this.infoLayout.Controls.Add(this.headVacuum2Panel, 1, 1);
            this.infoLayout.Controls.Add(this.headBlow1Panel, 0, 2);
            this.infoLayout.Controls.Add(this.headBlow2Panel, 1, 2);
            this.infoLayout.Dock = DockStyle.Fill;
            this.infoLayout.Padding = new Padding(12, 18, 12, 12);
            this.infoLayout.RowCount = 3;
            this.infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            this.infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            this.infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

            this.btnInitAll.Dock = DockStyle.Fill;
            this.btnInitAll.FlatStyle = FlatStyle.Flat;
            this.btnInitAll.Font = UiTheme.ButtonFont;
            this.btnInitAll.Tag = "i18n:wi.head.initAll";
            this.btnInitAll.Text = "INIT ALL";

            this.btnCountClear.Dock = DockStyle.Fill;
            this.btnCountClear.FlatStyle = FlatStyle.Flat;
            this.btnCountClear.Font = UiTheme.ButtonFont;
            this.btnCountClear.Tag = "i18n:wi.head.countClear";
            this.btnCountClear.Text = "COUNT CLEAR";

            this.lblHead1Title.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblHead1Title.BorderStyle = BorderStyle.FixedSingle;
            this.lblHead1Title.Dock = DockStyle.Fill;
            this.lblHead1Title.Font = UiTheme.ButtonFont;
            this.lblHead1Title.Padding = new Padding(8, 0, 0, 0);
            this.lblHead1Title.Tag = "i18n:wi.head1";
            this.lblHead1Title.Text = "HEAD #1";
            this.lblHead1Title.TextAlign = ContentAlignment.MiddleLeft;

            this.lblHead1Value.BackColor = Color.White;
            this.lblHead1Value.BorderStyle = BorderStyle.FixedSingle;
            this.lblHead1Value.Dock = DockStyle.Fill;
            this.lblHead1Value.Font = UiTheme.ValueFont;
            this.lblHead1Value.ForeColor = Color.Green;
            this.lblHead1Value.Text = "EMPTY";
            this.lblHead1Value.TextAlign = ContentAlignment.MiddleCenter;

            this.lblHead2Title.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblHead2Title.BorderStyle = BorderStyle.FixedSingle;
            this.lblHead2Title.Dock = DockStyle.Fill;
            this.lblHead2Title.Font = UiTheme.ButtonFont;
            this.lblHead2Title.Padding = new Padding(8, 0, 0, 0);
            this.lblHead2Title.Tag = "i18n:wi.head2";
            this.lblHead2Title.Text = "HEAD #2";
            this.lblHead2Title.TextAlign = ContentAlignment.MiddleLeft;

            this.lblHead2Value.BackColor = Color.White;
            this.lblHead2Value.BorderStyle = BorderStyle.FixedSingle;
            this.lblHead2Value.Dock = DockStyle.Fill;
            this.lblHead2Value.Font = UiTheme.ValueFont;
            this.lblHead2Value.ForeColor = Color.Green;
            this.lblHead2Value.Text = "EMPTY";
            this.lblHead2Value.TextAlign = ContentAlignment.MiddleCenter;

            this.lblColletChangeTitle.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblColletChangeTitle.BorderStyle = BorderStyle.FixedSingle;
            this.lblColletChangeTitle.Dock = DockStyle.Fill;
            this.lblColletChangeTitle.Font = UiTheme.ButtonFont;
            this.lblColletChangeTitle.Padding = new Padding(8, 0, 0, 0);
            this.lblColletChangeTitle.Tag = "i18n:wi.head.colletChange";
            this.lblColletChangeTitle.Text = "COLLET CHANGE";
            this.lblColletChangeTitle.TextAlign = ContentAlignment.MiddleLeft;

            this.lblColletChangeValue.BackColor = Color.White;
            this.lblColletChangeValue.BorderStyle = BorderStyle.FixedSingle;
            this.lblColletChangeValue.Dock = DockStyle.Fill;
            this.lblColletChangeValue.Font = UiTheme.ValueFont;
            this.lblColletChangeValue.ForeColor = Color.Black;
            this.lblColletChangeValue.Text = "INCOMPLETE";
            this.lblColletChangeValue.TextAlign = ContentAlignment.MiddleCenter;

            this.lblAutoPosTitle.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblAutoPosTitle.BorderStyle = BorderStyle.FixedSingle;
            this.lblAutoPosTitle.Dock = DockStyle.Fill;
            this.lblAutoPosTitle.Font = UiTheme.ButtonFont;
            this.lblAutoPosTitle.Padding = new Padding(8, 0, 0, 0);
            this.lblAutoPosTitle.Tag = "i18n:wi.head.autoPos";
            this.lblAutoPosTitle.Text = "AUTO POSITION";
            this.lblAutoPosTitle.TextAlign = ContentAlignment.MiddleLeft;

            this.lblAutoPosValue.BackColor = Color.White;
            this.lblAutoPosValue.BorderStyle = BorderStyle.FixedSingle;
            this.lblAutoPosValue.Dock = DockStyle.Fill;
            this.lblAutoPosValue.Font = UiTheme.ValueFont;
            this.lblAutoPosValue.ForeColor = Color.Black;
            this.lblAutoPosValue.Text = "INCOMPLETE";
            this.lblAutoPosValue.TextAlign = ContentAlignment.MiddleCenter;

            this.lblColletCleaningTitle.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblColletCleaningTitle.BorderStyle = BorderStyle.FixedSingle;
            this.lblColletCleaningTitle.Dock = DockStyle.Fill;
            this.lblColletCleaningTitle.Font = UiTheme.ButtonFont;
            this.lblColletCleaningTitle.Padding = new Padding(8, 0, 0, 0);
            this.lblColletCleaningTitle.Tag = "i18n:wi.head.colletCleaning";
            this.lblColletCleaningTitle.Text = "COLLET CLEANING";
            this.lblColletCleaningTitle.TextAlign = ContentAlignment.MiddleLeft;

            this.lblColletCleaningValue.BackColor = Color.White;
            this.lblColletCleaningValue.BorderStyle = BorderStyle.FixedSingle;
            this.lblColletCleaningValue.Dock = DockStyle.Fill;
            this.lblColletCleaningValue.Font = UiTheme.ValueFont;
            this.lblColletCleaningValue.ForeColor = Color.Black;
            this.lblColletCleaningValue.Text = "INCOMPLETE";
            this.lblColletCleaningValue.TextAlign = ContentAlignment.MiddleCenter;

            this.lblColletCheckTitle.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblColletCheckTitle.BorderStyle = BorderStyle.FixedSingle;
            this.lblColletCheckTitle.Dock = DockStyle.Fill;
            this.lblColletCheckTitle.Font = UiTheme.ButtonFont;
            this.lblColletCheckTitle.Padding = new Padding(8, 0, 0, 0);
            this.lblColletCheckTitle.Tag = "i18n:wi.head.colletCheck";
            this.lblColletCheckTitle.Text = "COLLET CHECK";
            this.lblColletCheckTitle.TextAlign = ContentAlignment.MiddleLeft;

            this.lblColletCheckValue.BackColor = Color.White;
            this.lblColletCheckValue.BorderStyle = BorderStyle.FixedSingle;
            this.lblColletCheckValue.Dock = DockStyle.Fill;
            this.lblColletCheckValue.Font = UiTheme.ValueFont;
            this.lblColletCheckValue.ForeColor = Color.Black;
            this.lblColletCheckValue.Text = "INCOMPLETE";
            this.lblColletCheckValue.TextAlign = ContentAlignment.MiddleCenter;

            this.lblPickFailTitle.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblPickFailTitle.BorderStyle = BorderStyle.FixedSingle;
            this.lblPickFailTitle.Dock = DockStyle.Fill;
            this.lblPickFailTitle.Font = UiTheme.ButtonFont;
            this.lblPickFailTitle.Padding = new Padding(8, 0, 0, 0);
            this.lblPickFailTitle.Tag = "i18n:wi.pickFail";
            this.lblPickFailTitle.Text = "PICK FAIL";
            this.lblPickFailTitle.TextAlign = ContentAlignment.MiddleLeft;

            this.lblPickFailValue.BackColor = Color.White;
            this.lblPickFailValue.BorderStyle = BorderStyle.FixedSingle;
            this.lblPickFailValue.Dock = DockStyle.Fill;
            this.lblPickFailValue.Font = UiTheme.ValueFont;
            this.lblPickFailValue.ForeColor = Color.Black;
            this.lblPickFailValue.Text = "0 ea";
            this.lblPickFailValue.TextAlign = ContentAlignment.MiddleCenter;

            this.lblPlaceFailTitle.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblPlaceFailTitle.BorderStyle = BorderStyle.FixedSingle;
            this.lblPlaceFailTitle.Dock = DockStyle.Fill;
            this.lblPlaceFailTitle.Font = UiTheme.ButtonFont;
            this.lblPlaceFailTitle.Padding = new Padding(8, 0, 0, 0);
            this.lblPlaceFailTitle.Tag = "i18n:wi.placeFail";
            this.lblPlaceFailTitle.Text = "PLACE FAIL";
            this.lblPlaceFailTitle.TextAlign = ContentAlignment.MiddleLeft;

            this.lblPlaceFailValue.BackColor = Color.White;
            this.lblPlaceFailValue.BorderStyle = BorderStyle.FixedSingle;
            this.lblPlaceFailValue.Dock = DockStyle.Fill;
            this.lblPlaceFailValue.Font = UiTheme.ValueFont;
            this.lblPlaceFailValue.ForeColor = Color.Black;
            this.lblPlaceFailValue.Text = "0 ea";
            this.lblPlaceFailValue.TextAlign = ContentAlignment.MiddleCenter;

            this.lblCollet1UseTitle.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblCollet1UseTitle.BorderStyle = BorderStyle.FixedSingle;
            this.lblCollet1UseTitle.Dock = DockStyle.Fill;
            this.lblCollet1UseTitle.Font = UiTheme.ButtonFont;
            this.lblCollet1UseTitle.Padding = new Padding(8, 0, 0, 0);
            this.lblCollet1UseTitle.Tag = "i18n:wi.collet1Use";
            this.lblCollet1UseTitle.Text = "COLLET #1 USE";
            this.lblCollet1UseTitle.TextAlign = ContentAlignment.MiddleLeft;

            this.lblCollet1UseValue.BackColor = Color.White;
            this.lblCollet1UseValue.BorderStyle = BorderStyle.FixedSingle;
            this.lblCollet1UseValue.Dock = DockStyle.Fill;
            this.lblCollet1UseValue.Font = UiTheme.ValueFont;
            this.lblCollet1UseValue.ForeColor = Color.Black;
            this.lblCollet1UseValue.Text = "0 ea";
            this.lblCollet1UseValue.TextAlign = ContentAlignment.MiddleCenter;

            this.lblCollet2UseTitle.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblCollet2UseTitle.BorderStyle = BorderStyle.FixedSingle;
            this.lblCollet2UseTitle.Dock = DockStyle.Fill;
            this.lblCollet2UseTitle.Font = UiTheme.ButtonFont;
            this.lblCollet2UseTitle.Padding = new Padding(8, 0, 0, 0);
            this.lblCollet2UseTitle.Tag = "i18n:wi.collet2Use";
            this.lblCollet2UseTitle.Text = "COLLET #2 USE";
            this.lblCollet2UseTitle.TextAlign = ContentAlignment.MiddleLeft;

            this.lblCollet2UseValue.BackColor = Color.White;
            this.lblCollet2UseValue.BorderStyle = BorderStyle.FixedSingle;
            this.lblCollet2UseValue.Dock = DockStyle.Fill;
            this.lblCollet2UseValue.Font = UiTheme.ValueFont;
            this.lblCollet2UseValue.ForeColor = Color.Black;
            this.lblCollet2UseValue.Text = "0 ea";
            this.lblCollet2UseValue.TextAlign = ContentAlignment.MiddleCenter;

            this.headAxisTPanel.ColumnCount = 1;
            this.headAxisTPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.headAxisTPanel.Controls.Add(this.lblHeadAxisTTitle, 0, 0);
            this.headAxisTPanel.Controls.Add(this.lblHeadAxisTValue, 0, 1);
            this.headAxisTPanel.Dock = DockStyle.Fill;
            this.headAxisTPanel.Margin = new Padding(4);
            this.headAxisTPanel.RowCount = 2;
            this.headAxisTPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            this.headAxisTPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.lblHeadAxisTTitle.BackColor = Color.Black;
            this.lblHeadAxisTTitle.Dock = DockStyle.Fill;
            this.lblHeadAxisTTitle.Font = UiTheme.SectionFont;
            this.lblHeadAxisTTitle.ForeColor = Color.White;
            this.lblHeadAxisTTitle.Padding = new Padding(6, 0, 0, 0);
            this.lblHeadAxisTTitle.Text = "HEAD AXIS T";
            this.lblHeadAxisTTitle.TextAlign = ContentAlignment.MiddleLeft;

            this.lblHeadAxisTValue.BackColor = Color.White;
            this.lblHeadAxisTValue.BorderStyle = BorderStyle.FixedSingle;
            this.lblHeadAxisTValue.Dock = DockStyle.Fill;
            this.lblHeadAxisTValue.Font = new Font("Consolas", 10F);
            this.lblHeadAxisTValue.Padding = new Padding(0, 0, 6, 0);
            this.lblHeadAxisTValue.Text = "0 um";
            this.lblHeadAxisTValue.TextAlign = ContentAlignment.MiddleRight;

            this.headVacuum1Panel.ColumnCount = 2;
            this.headVacuum1Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24F));
            this.headVacuum1Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.headVacuum1Panel.Controls.Add(this.dotHeadVacuum1, 0, 0);
            this.headVacuum1Panel.Controls.Add(this.lblHeadVacuum1, 1, 0);
            this.headVacuum1Panel.Dock = DockStyle.Fill;
            this.headVacuum1Panel.Margin = new Padding(4);
            this.headVacuum1Panel.RowCount = 1;
            this.headVacuum1Panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.dotHeadVacuum1.Dock = DockStyle.Fill;
            this.dotHeadVacuum1.Margin = new Padding(6, 8, 6, 8);
            this.dotHeadVacuum1.OnColor = Color.LimeGreen;

            this.lblHeadVacuum1.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblHeadVacuum1.Dock = DockStyle.Fill;
            this.lblHeadVacuum1.Font = UiTheme.ValueFont;
            this.lblHeadVacuum1.Padding = new Padding(6, 0, 0, 0);
            this.lblHeadVacuum1.Text = "HEAD VACUUM #1";
            this.lblHeadVacuum1.TextAlign = ContentAlignment.MiddleLeft;

            this.headVacuum2Panel.ColumnCount = 2;
            this.headVacuum2Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24F));
            this.headVacuum2Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.headVacuum2Panel.Controls.Add(this.dotHeadVacuum2, 0, 0);
            this.headVacuum2Panel.Controls.Add(this.lblHeadVacuum2, 1, 0);
            this.headVacuum2Panel.Dock = DockStyle.Fill;
            this.headVacuum2Panel.Margin = new Padding(4);
            this.headVacuum2Panel.RowCount = 1;
            this.headVacuum2Panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.dotHeadVacuum2.Dock = DockStyle.Fill;
            this.dotHeadVacuum2.Margin = new Padding(6, 8, 6, 8);
            this.dotHeadVacuum2.OnColor = Color.LimeGreen;

            this.lblHeadVacuum2.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblHeadVacuum2.Dock = DockStyle.Fill;
            this.lblHeadVacuum2.Font = UiTheme.ValueFont;
            this.lblHeadVacuum2.Padding = new Padding(6, 0, 0, 0);
            this.lblHeadVacuum2.Text = "HEAD VACUUM #2";
            this.lblHeadVacuum2.TextAlign = ContentAlignment.MiddleLeft;

            this.headBlow1Panel.ColumnCount = 2;
            this.headBlow1Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24F));
            this.headBlow1Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.headBlow1Panel.Controls.Add(this.dotHeadBlow1, 0, 0);
            this.headBlow1Panel.Controls.Add(this.lblHeadBlow1, 1, 0);
            this.headBlow1Panel.Dock = DockStyle.Fill;
            this.headBlow1Panel.Margin = new Padding(4);
            this.headBlow1Panel.RowCount = 1;
            this.headBlow1Panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.dotHeadBlow1.Dock = DockStyle.Fill;
            this.dotHeadBlow1.Margin = new Padding(6, 8, 6, 8);
            this.dotHeadBlow1.OnColor = Color.LimeGreen;

            this.lblHeadBlow1.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblHeadBlow1.Dock = DockStyle.Fill;
            this.lblHeadBlow1.Font = UiTheme.ValueFont;
            this.lblHeadBlow1.Padding = new Padding(6, 0, 0, 0);
            this.lblHeadBlow1.Text = "HEAD BLOW #1";
            this.lblHeadBlow1.TextAlign = ContentAlignment.MiddleLeft;

            this.headBlow2Panel.ColumnCount = 2;
            this.headBlow2Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24F));
            this.headBlow2Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.headBlow2Panel.Controls.Add(this.dotHeadBlow2, 0, 0);
            this.headBlow2Panel.Controls.Add(this.lblHeadBlow2, 1, 0);
            this.headBlow2Panel.Dock = DockStyle.Fill;
            this.headBlow2Panel.Margin = new Padding(4);
            this.headBlow2Panel.RowCount = 1;
            this.headBlow2Panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.dotHeadBlow2.Dock = DockStyle.Fill;
            this.dotHeadBlow2.Margin = new Padding(6, 8, 6, 8);
            this.dotHeadBlow2.OnColor = Color.LimeGreen;

            this.lblHeadBlow2.BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0);
            this.lblHeadBlow2.Dock = DockStyle.Fill;
            this.lblHeadBlow2.Font = UiTheme.ValueFont;
            this.lblHeadBlow2.Padding = new Padding(6, 0, 0, 0);
            this.lblHeadBlow2.Text = "HEAD BLOW #2";
            this.lblHeadBlow2.TextAlign = ContentAlignment.MiddleLeft;

            this.Controls.Add(this.rootLayout);
            this.Name = "HeadPage";
            this.Size = new Size(1400, 900);
            this.headBlow2Panel.ResumeLayout(false);
            this.headBlow1Panel.ResumeLayout(false);
            this.headVacuum2Panel.ResumeLayout(false);
            this.headVacuum1Panel.ResumeLayout(false);
            this.headAxisTPanel.ResumeLayout(false);
            this.infoLayout.ResumeLayout(false);
            this.counterLayout.ResumeLayout(false);
            this.stateLayout.ResumeLayout(false);
            this.grpInfo.ResumeLayout(false);
            this.grpCounters.ResumeLayout(false);
            this.grpState.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.rootLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
