using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>Operation / Monitoring — 현재 생산 자재 상태 요약.</summary>
    public class OperationPage : UserControl
    {
        public OperationPage()
        {

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            var hdr = new Label
            {
                Dock = DockStyle.Top, Height = 30, Text = "Monitoring — Wafer stage state",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(hdr);

            var body = new Label
            {
                Dock = DockStyle.Fill, Text = "Wafer stage state (placeholder)",
                Font = new Font("Segoe UI", 18F), ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(body);
        }
    }
}
