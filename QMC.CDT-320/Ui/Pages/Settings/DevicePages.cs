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
            lblHeader.BackColor = UiTheme.StatusBarBg;
            lblHeader.ForeColor = UiTheme.StatusBarFg;
            lblHeader.Font = UiTheme.SectionFont;
        }

        private void ApplyCompactLayout()
        {
            SettingsPageLayoutStyler.ApplyRoot(rootLayout);
            SettingsPageLayoutStyler.ApplyHeader(lblHeader);

            if (rootLayout.RowStyles.Count >= 4)
            {
                rootLayout.RowStyles[0].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[0].Height = 30F;
                rootLayout.RowStyles[1].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[1].Height = 312F;
                rootLayout.RowStyles[2].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[2].Height = 40F;
                rootLayout.RowStyles[3].SizeType = SizeType.Percent;
                rootLayout.RowStyles[3].Height = 100F;
            }

            optionLayout.Dock = DockStyle.Left;
            optionLayout.Width = 520;
            optionLayout.Margin = Padding.Empty;
            optionLayout.Padding = Padding.Empty;
            lblLastResult.Margin = Padding.Empty;
            lblLastResult.Dock = DockStyle.Left;
            lblLastResult.Width = optionLayout.Width;
            lblLastResult.Padding = new Padding(12, 0, 0, 0);
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
            lblHeader.BackColor = UiTheme.StatusBarBg;
            lblHeader.ForeColor = UiTheme.StatusBarFg;
            lblHeader.Font = UiTheme.SectionFont;
        }

        private void ApplyCompactLayout()
        {
            SettingsPageLayoutStyler.ApplyRoot(rootLayout);
            SettingsPageLayoutStyler.ApplyHeader(lblHeader);

            if (rootLayout.RowStyles.Count >= 3)
            {
                rootLayout.RowStyles[0].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[0].Height = 30F;
                rootLayout.RowStyles[1].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[1].Height = 500F;
                rootLayout.RowStyles[2].SizeType = SizeType.Percent;
                rootLayout.RowStyles[2].Height = 100F;
            }

            lensLayout.Margin = Padding.Empty;
            lensLayout.Padding = Padding.Empty;
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
            lblHeader.BackColor = UiTheme.StatusBarBg;
            lblHeader.ForeColor = UiTheme.StatusBarFg;
            lblHeader.Font = UiTheme.SectionFont;
        }

        private void ApplyCompactLayout()
        {
            SettingsPageLayoutStyler.ApplyRoot(rootLayout);
            SettingsPageLayoutStyler.ApplyHeader(lblHeader);

            if (rootLayout.RowStyles.Count >= 3)
            {
                rootLayout.RowStyles[0].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[0].Height = 30F;
                rootLayout.RowStyles[1].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[1].Height = 210F;
                rootLayout.RowStyles[2].SizeType = SizeType.Percent;
                rootLayout.RowStyles[2].Height = 100F;
            }

            sensorLayout.Margin = Padding.Empty;
            sensorLayout.Padding = Padding.Empty;
        }
    }
}
