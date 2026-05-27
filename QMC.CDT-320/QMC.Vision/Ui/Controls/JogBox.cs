using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>핸들러 축 이동용 Jog Box — X/Y/Z/R + 속도.</summary>
    public class JogBox : Panel
    {
        public JogBox()
        {
            BackColor = UiTheme.OptionPanelBg;
            BorderStyle = BorderStyle.FixedSingle;
            Size = new Size(260, 320);

            var hdr = new Label
            {
                Dock = DockStyle.Top, Height = 26, Text = "Jog Box",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(hdr);

            var up    = new Button { Location = new Point(96,  40), Size = new Size(60, 60), Text = "▲", Font = new Font("맑은 고딕", 16F), FlatStyle = FlatStyle.Flat };
            var down  = new Button { Location = new Point(96, 180), Size = new Size(60, 60), Text = "▼", Font = new Font("맑은 고딕", 16F), FlatStyle = FlatStyle.Flat };
            var left  = new Button { Location = new Point(26, 110), Size = new Size(60, 60), Text = "◀", Font = new Font("맑은 고딕", 16F), FlatStyle = FlatStyle.Flat };
            var right = new Button { Location = new Point(166,110), Size = new Size(60, 60), Text = "▶", Font = new Font("맑은 고딕", 16F), FlatStyle = FlatStyle.Flat };
            var cw    = new Button { Location = new Point(26, 250), Size = new Size(60, 40), Text = "↻",  Font = new Font("맑은 고딕", 14F), FlatStyle = FlatStyle.Flat };
            var ccw   = new Button { Location = new Point(166,250), Size = new Size(60, 40), Text = "↺",  Font = new Font("맑은 고딕", 14F), FlatStyle = FlatStyle.Flat };
            var speed = new ComboBox { Location = new Point(96, 260), Size = new Size(60, 24), DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.ValueFont };
            speed.Items.AddRange(new object[] { "Coarse", "Fine" });
            speed.SelectedIndex = 1;

            Controls.Add(up);   Controls.Add(down); Controls.Add(left); Controls.Add(right);
            Controls.Add(cw);   Controls.Add(ccw);  Controls.Add(speed);
        }
    }
}
