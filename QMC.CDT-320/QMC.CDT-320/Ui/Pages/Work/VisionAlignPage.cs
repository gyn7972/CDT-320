using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Work
{
    /// <summary>작업 - 비전 얼라인. 중앙 비전 뷰 + 우측 ACTION/결과.</summary>
    public class VisionAlignPage : PageBase
    {
        public VisionAlignPage()
        {
            Controls.Add(CreateSectionHeader("work.visionAlign"));

            var cam = new Panel { Location = new Point(8, 40), Size = new Size(1000, 760), BackColor = Color.Black };
            cam.Controls.Add(new Label { Location = new Point(8, 8), AutoSize = true, Text = "STAGE\r\nW : 640\r\nH : 480", ForeColor = Color.LightGreen, BackColor = Color.Black, Font = new Font("Consolas", 9F) });
            cam.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 20, Text = "Live", ForeColor = Color.LightGreen, BackColor = Color.Black, Font = new Font("Consolas", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0) });
            Controls.Add(cam);

            int x = 1030;
            Controls.Add(new Label
            {
                Location = new Point(x, 40), Size = new Size(300, 26),
                Text = "ACTION", BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10,0,0,0)
            });
            int y = 70;
            foreach (var a in new[] { "AUTO ALIGN", "MANUAL ALIGN", "FIRST MARK", "SECOND MARK", "THETA MATCH", "X_Y MATCH", "SAVE", "CLOSE" })
            {
                Controls.Add(new ActionButton { Location = new Point(x, y), Size = new Size(300, 44), Text = a });
                y += 50;
            }

            Controls.Add(new Label
            {
                Location = new Point(x, y + 10), Size = new Size(300, 26),
                Text = "RESULT", BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10,0,0,0)
            });
            y += 44;
            foreach (var kv in new (string, string)[] { ("Delta X","0.000"), ("Delta Y","0.000"), ("Delta Theta","0.000°"), ("Score","0.0") })
            {
                Controls.Add(new Label { Location = new Point(x, y),       Size = new Size(130, 28), Text = kv.Item1, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6,0,0,0), BorderStyle = BorderStyle.FixedSingle });
                Controls.Add(new Label { Location = new Point(x + 134, y), Size = new Size(166, 28), Text = kv.Item2, BackColor = Color.White, Font = new Font("Consolas", 10F), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0,0,6,0), BorderStyle = BorderStyle.FixedSingle });
                y += 32;
            }
        }
    }

    /// <summary>작업 - 웨이퍼 맵 오픈. 저장된 맵 파일 선택 + 뷰어.</summary>
    public class WaferMapOpenPage : PageBase
    {
        public WaferMapOpenPage()
        {
            Controls.Add(CreateSectionHeader("work.waferMapOpen"));

            Controls.Add(new Label
            {
                Location = new Point(8, 40), Size = new Size(360, 26),
                Text = "맵 파일 목록", BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10,0,0,0)
            });
            var lb = new ListBox { Location = new Point(8, 68), Size = new Size(360, 860), Font = new Font("Consolas", 9F) };
            foreach (var f in new[] { "Y482CB1_2026-04-24.map", "Y482CB0_2026-04-23.map", "SAMPLE.map" }) lb.Items.Add(f);
            Controls.Add(lb);

            Controls.Add(new Label
            {
                Location = new Point(380, 40), Size = new Size(1000, 26),
                Text = "MAP VIEW", BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10,0,0,0)
            });
            Controls.Add(new Panel { Location = new Point(380, 68), Size = new Size(1000, 860), BackColor = Color.Black, BorderStyle = BorderStyle.FixedSingle });
        }
    }
}
