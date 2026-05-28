using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>
    /// 레시피 페이지 공통: 주황 섹션 헤더, "Label|Value" 행, 카메라 뷰, 조그 패드, 속도 슬라이더.
    /// </summary>
    internal static class RecipeLayout
    {
        public static Label OrangeBar(int x, int y, int w, string text) => new Label
        {
            Location  = new Point(x, y),
            Size      = new Size(w, 26),
            Text      = text,
            BackColor = UiTheme.StatusBarBg,
            ForeColor = Color.White,
            Font      = UiTheme.SectionFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(10, 0, 0, 0)
        };

        public static Label OrangeBarI18n(int x, int y, int w, string i18nKey)
        {
            var l = OrangeBar(x, y, w, Lang.T(i18nKey));
            l.Tag = "i18n:" + i18nKey;
            return l;
        }

        public static Label LabelCell(int x, int y, int w, int h, string text) => new Label
        {
            Location  = new Point(x, y), Size = new Size(w, h),
            Text = text, BackColor = Color.FromArgb(0xD0,0xD0,0xD0),
            Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0), BorderStyle = BorderStyle.FixedSingle
        };

        public static Label ValueCell(int x, int y, int w, int h, string value) => new Label
        {
            Location = new Point(x, y), Size = new Size(w, h),
            Text = value, BackColor = Color.White,
            Font = new Font("Consolas", 10F), TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 6, 0), BorderStyle = BorderStyle.FixedSingle
        };

        /// <summary>Label + Value 한 쌍 추가.</summary>
        public static void AddPair(Control parent, int x, int y, int labelW, int valueW, string label, string value, int h = 28)
        {
            parent.Controls.Add(LabelCell(x, y, labelW, h, label));
            parent.Controls.Add(ValueCell(x + labelW, y, valueW, h, value));
        }

        /// <summary>Label + 설정 버튼.</summary>
        public static void AddSettingRow(Control parent, int x, int y, int labelW, int btnW, string label)
        {
            parent.Controls.Add(LabelCell(x, y, labelW, 30, label));
            var btn = new Button
            {
                Location = new Point(x + labelW, y), Size = new Size(btnW, 30),
                Text = Lang.T("common.setting"), Tag = "i18n:common.setting",
                FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont,
                BackColor = Color.FromArgb(0xD0, 0xD0, 0xD0)
            };
            parent.Controls.Add(btn);
        }

        /// <summary>STAGE VISION / 하부 카메라 / 측면 카메라 뷰 placeholder.</summary>
        public static Panel CameraView(int x, int y, int w, int h, string titleI18n)
        {
            var p = new Panel { Location = new Point(x, y), Size = new Size(w, h) };
            p.Controls.Add(OrangeBarI18n(0, 0, w, titleI18n));
            var cam = new Panel
            {
                Location = new Point(0, 26), Size = new Size(w, h - 26),
                BackColor = Color.Black
            };
            cam.Controls.Add(new Label
            {
                Location = new Point(8, 8), AutoSize = true,
                Text = "STAGE\r\nW : 640\r\nH : 480\r\n11597\r\n9337\r\n6342\r\n5005",
                ForeColor = UiTheme.VisionInfoFg, BackColor = Color.Black,
                Font = new Font("Consolas", 9F)
            });
            cam.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom, Height = 18, Text = "Live",
                ForeColor = UiTheme.VisionInfoFg, BackColor = Color.Black,
                Font = new Font("Consolas", 9F), TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            });
            p.Controls.Add(cam);
            return p;
        }

        /// <summary>축 1개용 조그 (상/하만).</summary>
        public static Panel SimpleJog(int x, int y, int w, int h, string axisLabel)
        {
            var p = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = UiTheme.MainBg };
            p.Controls.Add(OrangeBarI18n(0, 0, w, "work.sec.visionView".Replace("visionView","jog.jogRun"))); // placeholder header
            var bUp  = new Button { Location = new Point((w - 80) / 2,       36),         Size = new Size(80, 80), Text = "▲", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat };
            var lbl  = new Label  { Location = new Point((w - 80) / 2,       120),        Size = new Size(80, 24), Text = axisLabel, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), TextAlign = ContentAlignment.MiddleCenter };
            var bDn  = new Button { Location = new Point((w - 80) / 2,       150),        Size = new Size(80, 80), Text = "▼", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat };
            p.Controls.Add(bUp); p.Controls.Add(lbl); p.Controls.Add(bDn);
            return p;
        }

        /// <summary>X/Y 4방향 + (옵션) T축 회전 조그.</summary>
        public static Panel XyJog(int x, int y, int w, int h, bool withTheta = true)
        {
            var p = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = UiTheme.MainBg };
            int cx = w / 2 - 40;
            p.Controls.Add(new Button { Location = new Point(cx,     10), Size = new Size(80, 80), Text = "▲", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat });
            p.Controls.Add(new Button { Location = new Point(cx - 90,100), Size = new Size(80, 80), Text = "◀", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat });
            p.Controls.Add(new Button { Location = new Point(cx + 90,100), Size = new Size(80, 80), Text = "▶", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat });
            p.Controls.Add(new Button { Location = new Point(cx,    100), Size = new Size(80, 80), Text = "●",  Font = new Font("맑은 고딕", 14F), FlatStyle = FlatStyle.Flat });
            p.Controls.Add(new Button { Location = new Point(cx,    190), Size = new Size(80, 80), Text = "▼", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat });
            if (withTheta)
            {
                p.Controls.Add(new Button { Location = new Point(cx - 90,190), Size = new Size(80, 80), Text = "↺", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat });
                p.Controls.Add(new Button { Location = new Point(cx + 90,190), Size = new Size(80, 80), Text = "↻", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat });
            }
            return p;
        }

        /// <summary>세로 속도 슬라이더 (red→blue).</summary>
        public static Panel VerticalSpeedBar(int x, int y, int w, int h)
        {
            var panel = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = UiTheme.MainBg };
            panel.Controls.Add(OrangeBar(0, 0, w, "속도"));
            var bar = new Panel { Location = new Point(20, 36), Size = new Size(40, h - 80) };
            bar.Paint += (s, e) =>
            {
                using (var br = new LinearGradientBrush(bar.ClientRectangle, Color.Red, Color.Blue, 90f))
                    e.Graphics.FillRectangle(br, bar.ClientRectangle);
            };
            panel.Controls.Add(bar);
            panel.Controls.Add(new Label { Location = new Point(70, 36),      AutoSize = true, Text = "100%", Font = new Font("맑은 고딕", 9F) });
            panel.Controls.Add(new Label { Location = new Point(70, h / 2),   AutoSize = true, Text = "50%",  Font = new Font("맑은 고딕", 9F) });
            panel.Controls.Add(new Label { Location = new Point(70, h - 48),  AutoSize = true, Text = "0%",   Font = new Font("맑은 고딕", 9F) });
            return panel;
        }
    }
}
