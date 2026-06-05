using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>조명 4개 채널 밝기 슬라이더.</summary>
    public class IlluminatorPanel : Panel
    {
        public IlluminatorPanel()
        {
            BackColor = UiTheme.OptionPanelBg;
            BorderStyle = BorderStyle.FixedSingle;
            Height = 180;

            var hdr = new Label
            {
                Dock = DockStyle.Top, Height = 26, Text = "Illuminator",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(hdr);

            int y = 30;
            foreach (var n in new[] { "CH 1", "CH 2", "CH 3", "CH 4" })
            {
                Controls.Add(new Label   { Location = new Point(6, y + 4), Size = new Size(40, 20), Text = n, Font = UiTheme.ButtonFont });
                var tb = new TrackBar    { Location = new Point(50, y - 4), Size = new Size(180, 30), Minimum = 0, Maximum = 255, Value = 128, TickFrequency = 32 };
                var val = new Label       { Location = new Point(234, y + 4), Size = new Size(40, 20), Text = "128", Font = UiTheme.ValueFont, TextAlign = ContentAlignment.MiddleRight };
                tb.ValueChanged += (s, e) => val.Text = tb.Value.ToString();
                Controls.Add(tb); Controls.Add(val);
                y += 32;
            }
        }
    }
}
