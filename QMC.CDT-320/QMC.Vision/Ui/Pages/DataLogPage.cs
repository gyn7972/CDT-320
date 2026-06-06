using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    public class DataLogPage : UserControl
    {
        public DataLogPage()
        {

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            var hdr = new Label
            {
                Dock = DockStyle.Top, Height = 30, Text = "Data Log",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(hdr);

            var tabs = new TabControl { Dock = DockStyle.Fill, Font = UiTheme.ButtonFont };
            tabs.TabPages.Add(new TabPage { Text = "Log" });
            tabs.TabPages.Add(new TabPage { Text = "Alarm" });
            tabs.TabPages.Add(new TabPage { Text = "Utility" });
            Controls.Add(tabs);
        }
    }
}
