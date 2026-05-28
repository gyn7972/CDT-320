using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>Settings - barcode reader.</summary>
    public partial class BarcodeReaderPage : PageBase
    {
        public BarcodeReaderPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.barcode");
            lblHeader.Tag = "i18n:set.barcode";
            lblHeader.BackColor = UiTheme.StatusBarBg;
            lblHeader.ForeColor = UiTheme.StatusBarFg;
            lblHeader.Font = UiTheme.SectionFont;
        }
    }

    /// <summary>Settings - zoom lens.</summary>
    public partial class ZoomLensPage : PageBase
    {
        public ZoomLensPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.zoomLens");
            lblHeader.Tag = "i18n:set.zoomLens";
            lblHeader.BackColor = UiTheme.StatusBarBg;
            lblHeader.ForeColor = UiTheme.StatusBarFg;
            lblHeader.Font = UiTheme.SectionFont;
        }
    }

    /// <summary>Settings - height sensor.</summary>
    public partial class HeightSensorPage : PageBase
    {
        public HeightSensorPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.heightSensor");
            lblHeader.Tag = "i18n:set.heightSensor";
            lblHeader.BackColor = UiTheme.StatusBarBg;
            lblHeader.ForeColor = UiTheme.StatusBarFg;
            lblHeader.Font = UiTheme.SectionFont;
        }
    }
}
