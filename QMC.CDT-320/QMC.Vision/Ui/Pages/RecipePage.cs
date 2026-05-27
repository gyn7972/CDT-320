using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    public class RecipePage : UserControl
    {
        public RecipePage()
        {

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            var hdr = new Label
            {
                Dock = DockStyle.Top, Height = 30, Text = "Recipe",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(hdr);
            Controls.Add(new Label
            {
                Dock = DockStyle.Fill, Text = "Recipe (DieTransfer/WaferVision/EjectPin/Reticle/AlignDie/...)",
                Font = new Font("Segoe UI", 14F), ForeColor = Color.DimGray, TextAlign = ContentAlignment.MiddleCenter
            });
        }
    }
}
