using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>설정 - BARCODE READER. 포트/통신 설정 + TEST 버튼.</summary>
    public class BarcodeReaderPage : PageBase
    {
        public BarcodeReaderPage()
        {
            Controls.Add(CreateSectionHeader("set.barcode"));
            int y = 40;
            foreach (var kv in new (string, string)[]
            {
                ("PORT",          "COM3"),
                ("BAUD RATE",     "9600"),
                ("DATA BITS",     "8"),
                ("PARITY",        "NONE"),
                ("STOP BITS",     "1"),
                ("HEAD CHAR",     "STX"),
                ("TAIL CHAR",     "ETX"),
                ("READ TIMEOUT",  "3000 ms"),
                ("RETRY COUNT",   "3"),
            })
            {
                AddPair(this, 20, y, kv.Item1, kv.Item2);
                y += 34;
            }
            Controls.Add(new ActionButton { Location = new Point(20, y + 10), Size = new Size(150, 44), Text = "CONNECT" });
            Controls.Add(new ActionButton { Location = new Point(180, y + 10),Size = new Size(150, 44), Text = "TEST READ" });

            Controls.Add(new Label
            {
                Location = new Point(360, y + 20), Size = new Size(600, 28),
                Text = "Last Result : ", BackColor = Color.Black, ForeColor = Color.LimeGreen,
                Font = new Font("Consolas", 12F), TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            });
        }

        private static void AddPair(Control parent, int x, int y, string label, string value)
        {
            parent.Controls.Add(new Label { Location = new Point(x, y),       Size = new Size(200, 28), Text = label, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6,0,0,0), BorderStyle = BorderStyle.FixedSingle });
            parent.Controls.Add(new Label { Location = new Point(x + 204, y), Size = new Size(260, 28), Text = value, BackColor = Color.White, Font = new Font("Consolas", 10F), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0,0,6,0), BorderStyle = BorderStyle.FixedSingle });
        }
    }

    /// <summary>설정 - ZOOM LENS. 각 카메라별 렌즈 배율 설정.</summary>
    public class ZoomLensPage : PageBase
    {
        public ZoomLensPage()
        {
            Controls.Add(CreateSectionHeader("set.zoomLens"));
            int y = 40;
            foreach (var c in new[] { "INPUT VISION", "OUTPUT VISION", "LOWER VISION", "BOTTOM VISION", "SIDE VISION (FRONT)", "SIDE VISION (REAR)" })
            {
                var grp = new GroupBox { Location = new Point(20, y), Size = new Size(900, 80), Text = c, Font = UiTheme.SectionFont };
                AddKV(grp, 20,  28, "PORT",   "COM4");
                AddKV(grp, 320, 28, "CHANNEL", "1");
                AddKV(grp, 620, 28, "ZOOM",    "50");
                var btnApply = new Button { Location = new Point(720, 50), Size = new Size(140, 24), Text = Lang.T("common.apply"), Tag = "i18n:common.apply", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
                grp.Controls.Add(btnApply);
                Controls.Add(grp);
                y += 90;
            }
        }

        private static void AddKV(Control parent, int x, int y, string label, string value)
        {
            parent.Controls.Add(new Label { Location = new Point(x, y),      Size = new Size(120, 24), Text = label, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6,0,0,0), BorderStyle = BorderStyle.FixedSingle });
            parent.Controls.Add(new TextBox { Location = new Point(x + 124, y), Size = new Size(160, 24), Text = value, Font = UiTheme.ValueFont });
        }
    }

    /// <summary>설정 - HEIGHT SENSOR. 센서별 ZERO/한계값/현재값.</summary>
    public class HeightSensorPage : PageBase
    {
        public HeightSensorPage()
        {
            Controls.Add(CreateSectionHeader("set.heightSensor"));
            int y = 40;
            foreach (var name in new[] { "SENSOR #1", "SENSOR #2" })
            {
                var grp = new GroupBox { Location = new Point(20, y), Size = new Size(900, 110), Text = name, Font = UiTheme.SectionFont };
                AddKV(grp, 20,  28, "PORT",      "COM5");
                AddKV(grp, 320, 28, "CHANNEL",   "1");
                AddKV(grp, 620, 28, "ZERO",      "0.00");
                AddKV(grp, 20,  60, "LIMIT MIN", "-10.00");
                AddKV(grp, 320, 60, "LIMIT MAX", "10.00");
                AddKV(grp, 620, 60, "VALUE",     "0.000 mm");
                var btnZero = new Button { Location = new Point(744, 76), Size = new Size(140, 24), Text = "ZERO NOW", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
                grp.Controls.Add(btnZero);
                Controls.Add(grp);
                y += 120;
            }
        }

        private static void AddKV(Control parent, int x, int y, string label, string value)
        {
            parent.Controls.Add(new Label { Location = new Point(x, y),      Size = new Size(120, 24), Text = label, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6,0,0,0), BorderStyle = BorderStyle.FixedSingle });
            parent.Controls.Add(new TextBox { Location = new Point(x + 124, y), Size = new Size(160, 24), Text = value, Font = UiTheme.ValueFont });
        }
    }
}
