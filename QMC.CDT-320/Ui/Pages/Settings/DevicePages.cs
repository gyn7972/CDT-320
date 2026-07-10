using QMC.CDT_320.Ui.Localization;

using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>Settings - barcode reader.</summary>
    public partial class BarcodeReaderPage : PageBase
    {
        public BarcodeReaderPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyCompactLayout();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.barcode");
            lblHeader.Tag = "i18n:set.barcode";
        }

        private void ApplyCompactLayout()
        {
            SettingsPageLayoutStyler.ApplyRoot(rootLayout);
            SettingsPageLayoutStyler.ApplyHeader(lblHeader);
            SettingsPageLayoutStyler.ApplyActionControl(btnConnect);
            SettingsPageLayoutStyler.ApplyActionControl(btnTestRead);
        }
    }

    /// <summary>Settings - zoom lens.</summary>
    public partial class ZoomLensPage : PageBase
    {
        public ZoomLensPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyCompactLayout();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.zoomLens");
            lblHeader.Tag = "i18n:set.zoomLens";
        }

        private void ApplyCompactLayout()
        {
            SettingsPageLayoutStyler.ApplyRoot(rootLayout);
            SettingsPageLayoutStyler.ApplyHeader(lblHeader);
        }
    }

    /// <summary>Settings - height sensor.</summary>
    public partial class HeightSensorPage : PageBase
    {
        public HeightSensorPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyCompactLayout();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.heightSensor");
            lblHeader.Tag = "i18n:set.heightSensor";
        }

        private void ApplyCompactLayout()
        {
            SettingsPageLayoutStyler.ApplyRoot(rootLayout);
            SettingsPageLayoutStyler.ApplyHeader(lblHeader);
        }
    }
}
