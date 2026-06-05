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
            // Stage 72 — Recipe 페이지 콘텐츠 영역(≈832px) 안에 카메라 아래로 들어가도록 280px 로 컴팩트화.
            //   기존 320px 레이아웃은 회전 버튼/속도 콤보가 잘렸음.
            Size = new Size(260, 280);

            var hdr = new Label
            {
                Dock = DockStyle.Top, Height = 26, Text = "Jog Box",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(hdr);

            var up    = new Button { Location = new Point(100, 32), Size = new Size(54, 54), Text = "▲", Font = new Font("맑은 고딕", 15F), FlatStyle = FlatStyle.Flat };
            var down  = new Button { Location = new Point(100, 150), Size = new Size(54, 54), Text = "▼", Font = new Font("맑은 고딕", 15F), FlatStyle = FlatStyle.Flat };
            var left  = new Button { Location = new Point(30,  91), Size = new Size(54, 54), Text = "◀", Font = new Font("맑은 고딕", 15F), FlatStyle = FlatStyle.Flat };
            var right = new Button { Location = new Point(170, 91), Size = new Size(54, 54), Text = "▶", Font = new Font("맑은 고딕", 15F), FlatStyle = FlatStyle.Flat };
            var cw    = new Button { Location = new Point(30, 214), Size = new Size(54, 34), Text = "↻",  Font = new Font("맑은 고딕", 13F), FlatStyle = FlatStyle.Flat };
            var ccw   = new Button { Location = new Point(170,214), Size = new Size(54, 34), Text = "↺",  Font = new Font("맑은 고딕", 13F), FlatStyle = FlatStyle.Flat };
            var speed = new ComboBox { Location = new Point(100, 219), Size = new Size(54, 24), DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.ValueFont };
            speed.Items.AddRange(new object[] { "Coarse", "Fine" });
            speed.SelectedIndex = 1;

            Controls.Add(up);   Controls.Add(down); Controls.Add(left); Controls.Add(right);
            Controls.Add(cw);   Controls.Add(ccw);  Controls.Add(speed);
        }
    }
}
