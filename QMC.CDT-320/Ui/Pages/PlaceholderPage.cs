using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages
{
    public partial class PlaceholderPage : PageBase
    {
        private readonly string _i18nKey;
        private readonly bool _quietSurface;

        public PlaceholderPage() : this("common.caption", false)
        {
        }

        public PlaceholderPage(string i18nKey) : this(i18nKey, false)
        {
        }

        public PlaceholderPage(string i18nKey, bool quietSurface)
        {
            _i18nKey = i18nKey;
            _quietSurface = quietSurface;
            InitializeComponent();
            if (_quietSurface || IsDialogLaunchSurface())
                ApplyQuietSurface();
            ApplyCaption();
            if (IsDialogLaunchSurface())
                ApplyDialogLaunchSurface();
        }

        private void ApplyCaption()
        {
            string caption = Lang.T(_i18nKey);
            lblHeader.Tag = "i18n:" + _i18nKey;
            lblHeader.Text = caption;
            lblPlaceholder.Tag = "i18n:" + _i18nKey;
            lblPlaceholder.Text = _quietSurface ? caption : caption + "   (placeholder)";
        }

        private bool IsDialogLaunchSurface()
        {
            return string.Equals(_i18nKey, "set.selfTest", StringComparison.OrdinalIgnoreCase)
                || string.Equals(_i18nKey, "settings.remoteViewer", StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyDialogLaunchSurface()
        {
            string caption = Lang.T(_i18nKey);
            string note = string.Equals(_i18nKey, "set.selfTest", StringComparison.OrdinalIgnoreCase)
                ? "SYSTEM SELF-TEST"
                : "REMOTE VIEWER";

            lblPlaceholder.Tag = null;
            lblPlaceholder.Text = caption + "\r\n" + note;
            lblPlaceholder.BackColor = Color.White;
            lblPlaceholder.ForeColor = Color.FromArgb(38, 50, 66);
            lblPlaceholder.Font = new Font("Malgun Gothic", 24F, FontStyle.Bold);
            lblPlaceholder.Padding = new Padding(0, 0, 0, 36);
            lblPlaceholder.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void ApplyQuietSurface()
        {
            BackColor = Color.White;
            rootLayout.BackColor = Color.White;
            rootLayout.Margin = Padding.Empty;
            rootLayout.Padding = Padding.Empty;
            lblHeader.Margin = Padding.Empty;
            lblPlaceholder.Margin = Padding.Empty;
            lblPlaceholder.BackColor = Color.FromArgb(248, 249, 251);
            lblPlaceholder.Font = new Font(UiTheme.SectionFont.FontFamily, 15F, FontStyle.Regular);
            lblPlaceholder.ForeColor = Color.FromArgb(0x66, 0x66, 0x66);
        }
    }
}
