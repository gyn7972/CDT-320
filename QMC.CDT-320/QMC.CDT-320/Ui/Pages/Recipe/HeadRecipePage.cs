using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>
    /// 레시피 HEAD 페이지 (FRONT/REAR 공용).
    /// 좌측: 옵션/대기시간/매뉴얼동작/실린더&I/O
    /// 중앙: 하부 카메라 뷰 + 조그 운전 + 속도 슬라이더
    /// </summary>
    public class HeadRecipePage : PageBase
    {
        public HeadRecipePage(string titleI18n)
        {
            Controls.Add(CreateSectionHeader(titleI18n));

            // ─── 좌측 옵션 패널 ───
            var left = new Panel { Location = new Point(8, 40), Size = new Size(400, 900), BackColor = UiTheme.OptionPanelBg };

            // 옵션 섹션
            AddOrangeBar(left, 0, 0, 400, "옵션");
            int y = 30;
            foreach (var k in new[]
            {
                ("기본 위치 및 오토셋팅", "설정"),
                ("PICK 위치",            "설정"),
                ("PLACE 위치",           "설정"),
                ("SEQUENCE 설정",        "설정"),
                ("PICK Z OFFSET 설정",   "설정"),
                ("PLACE Z OFFSET",       "설정"),
            })
            {
                AddRow(left, 4, y, 200, 180, k.Item1, k.Item2);
                y += 32;
            }
            AddRow(left, 4, y,   200, 180, "HEAD 사용",   "모두 사용");            AddRow(left, 210, y, 100, 80,  "VACUUM CHECK", "ENABLE/ENABLE");
            y += 32;
            AddRow(left, 4, y,   200, 180, "조명 설정",    "(PROJECT)");
            y += 32;
            AddRow(left, 4, y,   200, 180, "COLLET CHECK", "DISABLE");
            AddRow(left, 210, y, 100, 80,  "FIBER SENSOR CHECK", "DISABLE/300");
            y += 32;
            AddRow(left, 4, y,   200, 180, "오버라이드", "설정");
            y += 32;
            AddRow(left, 4, y,   200, 180, "HEAD T축 POWER", "50 %");
            y += 40;

            // 대기 시간
            AddOrangeBar(left, 0, y, 400, "대기시간");
            y += 30;
            AddRow(left, 4, y,   200, 180, "HEAD 이동후 대기시간", "0 ms"); y += 32;
            AddRow(left, 4, y,   200, 180, "VACUUM ON PICK/PLACE", "40 ms / 0 ms / 0 ms"); y += 32;
            AddRow(left, 4, y,   200, 180, "BLOW PICK / PLACE",    "0 ms / 40 ms");         y += 32;
            AddRow(left, 4, y,   200, 180, "PICK UP / DOWN",       "0 ms / 0 ms");          y += 32;
            AddRow(left, 4, y,   200, 180, "PLACE UP / DOWN",      "0 ms / 40 ms");         y += 40;

            // 매뉴얼 동작
            AddOrangeBar(left, 0, y, 400, "매뉴얼 동작");
            y += 30;
            foreach (var m in new[] { "PICK UP TEST", "PICK", "PICK DOWN", "PICK UP" })
            {
                left.Controls.Add(new ActionButton { Location = new Point(4, y), Size = new Size(180, 36), Text = m });
                y += 40;
            }
            int yR = y - 40 * 4;
            foreach (var m in new[] { "회피 위치 이동", "PLACE", "PLACE DOWN", "PLACE UP" })
            {
                left.Controls.Add(new ActionButton { Location = new Point(204, yR), Size = new Size(180, 36), Text = m });
                yR += 40;
            }

            // 실린더 & I/O
            AddOrangeBar(left, 0, y, 400, "실린더 & I/O");
            y += 30;
            foreach (var sw in new[] { "VACUUM BLOW #1", "VACUUM BLOW #2", "ALL ON / OFF" })
            {
                AddToggleRow(left, 4, y, sw);
                y += 28;
            }
            foreach (var s in new[] { "Flow Sensor # 1", "Break Sensor # 1", "Flow Sensor # 2", "Break Sensor # 2" })
            {
                AddIoDot(left, 4, y, s);
                y += 24;
            }

            Controls.Add(left);

            // ─── 중앙 카메라 + 조그 ───
            var center = new Panel { Location = new Point(420, 40), Size = new Size(740, 600) };
            var camHdr = new Label
            {
                Dock = DockStyle.Top, Height = 26, Text = "하부 카메라",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            var cam = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };
            cam.Controls.Add(new Label
            {
                Location = new Point(8, 8), AutoSize = true,
                Text = "STAGE\r\nW : 640\r\nH : 480\r\n11597\r\n9337\r\n6342\r\n5005",
                ForeColor = UiTheme.VisionInfoFg, BackColor = Color.Black,
                Font = new Font("Consolas", 9F)
            });
            center.Controls.Add(cam);
            center.Controls.Add(camHdr);
            Controls.Add(center);

            // 조그 운전
            var jog = new Panel { Location = new Point(420, 650), Size = new Size(740, 280), BackColor = UiTheme.OptionPanelBg };
            AddOrangeBar(jog, 0, 0, 740, "조그 운전");
            jog.Controls.Add(new Button { Location = new Point(10, 36),  Size = new Size(100, 30), Text = "저속", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont });
            jog.Controls.Add(new Button { Location = new Point(10, 70),  Size = new Size(100, 30), Text = "조그 이동", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont });
            jog.Controls.Add(new TextBox { Location = new Point(10, 104), Size = new Size(100, 26), Text = "0 um", Font = UiTheme.ValueFont });

            AddRow(jog, 120, 36, 90, 120, "T 축",        "0 um");
            AddRow(jog, 120, 70, 90, 120, "PICK Z 축",   "0 um");
            AddRow(jog, 120, 104,90, 120, "ALIGN T 축",  "... um");
            AddRow(jog, 120, 138,90, 120, "PLACE Z 축",  "0 um");

            // 큰 원형 화살표 4개 (PICK Z, PLACE Z)
            var pZUp   = new Button { Location = new Point(350, 36),  Size = new Size(80, 80), Text = "▲", Font = new Font("맑은 고딕", 20F), FlatStyle = FlatStyle.Flat };
            var pZLbl  = new Label  { Location = new Point(350, 118), Size = new Size(80, 22), Text = "PICK Z 축", TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(0xD0,0xD0,0xD0) };
            var pZDn   = new Button { Location = new Point(350, 142), Size = new Size(80, 80), Text = "▼", Font = new Font("맑은 고딕", 20F), FlatStyle = FlatStyle.Flat };
            var pLUp   = new Button { Location = new Point(440, 36),  Size = new Size(80, 80), Text = "▲", Font = new Font("맑은 고딕", 20F), FlatStyle = FlatStyle.Flat };
            var pLLbl  = new Label  { Location = new Point(440, 118), Size = new Size(80, 22), Text = "PLACE Z 축", TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(0xD0,0xD0,0xD0) };
            var pLDn   = new Button { Location = new Point(440, 142), Size = new Size(80, 80), Text = "▼", Font = new Font("맑은 고딕", 20F), FlatStyle = FlatStyle.Flat };
            var tLeft  = new Button { Location = new Point(540, 142), Size = new Size(80, 80), Text = "↺", Font = new Font("맑은 고딕", 20F), FlatStyle = FlatStyle.Flat };
            var tLbl   = new Label  { Location = new Point(620, 170), Size = new Size(60,  22), Text = "T 축",    TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(0xD0,0xD0,0xD0) };
            var tRight = new Button { Location = new Point(680, 142), Size = new Size(80, 80), Text = "↻", Font = new Font("맑은 고딕", 20F), FlatStyle = FlatStyle.Flat };
            jog.Controls.AddRange(new Control[] { pZUp, pZLbl, pZDn, pLUp, pLLbl, pLDn, tLeft, tLbl, tRight });

            Controls.Add(jog);

            // 속도 슬라이더
            var speedPanel = new Panel { Location = new Point(1170, 40), Size = new Size(160, 890), BackColor = UiTheme.MainBg };
            AddOrangeBar(speedPanel, 0, 0, 160, "속도");
            var bar = new Panel { Location = new Point(20, 40), Size = new Size(40, 800) };
            bar.Paint += (s, e) =>
            {
                using (var br = new System.Drawing.Drawing2D.LinearGradientBrush(bar.ClientRectangle, Color.Red, Color.Blue, 90f))
                    e.Graphics.FillRectangle(br, bar.ClientRectangle);
            };
            speedPanel.Controls.Add(bar);
            speedPanel.Controls.Add(new Label { Location = new Point(70, 40),  AutoSize = true, Text = "100%", Font = new Font("맑은 고딕", 9F) });
            speedPanel.Controls.Add(new Label { Location = new Point(70, 420), AutoSize = true, Text = "50%",  Font = new Font("맑은 고딕", 9F) });
            speedPanel.Controls.Add(new Label { Location = new Point(70, 820), AutoSize = true, Text = "0%",   Font = new Font("맑은 고딕", 9F) });
            speedPanel.Controls.Add(new Label { Location = new Point(20, 850), Size = new Size(120, 22), Text = "0%", BackColor = Color.FromArgb(0xD0,0xD0,0xD0), TextAlign = ContentAlignment.MiddleCenter });
            Controls.Add(speedPanel);
        }

        private static void AddOrangeBar(Control parent, int x, int y, int w, string text)
        {
            parent.Controls.Add(new Label
            {
                Location  = new Point(x, y), Size = new Size(w, 26),
                Text = text, BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });
        }

        private static void AddRow(Control parent, int x, int y, int labelW, int valueW, string label, string value)
        {
            parent.Controls.Add(new Label { Location = new Point(x, y),         Size = new Size(labelW, 28), Text = label, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6,0,0,0), BorderStyle = BorderStyle.FixedSingle });
            parent.Controls.Add(new Label { Location = new Point(x + labelW, y),Size = new Size(valueW, 28), Text = value, BackColor = Color.White, Font = new Font("Consolas", 10F), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0,0,6,0), BorderStyle = BorderStyle.FixedSingle });
        }

        private static void AddToggleRow(Control parent, int x, int y, string label)
        {
            parent.Controls.Add(new Label { Location = new Point(x, y),    Size = new Size(130, 26), Text = label, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4,0,0,0), BorderStyle = BorderStyle.FixedSingle });
            parent.Controls.Add(new Label { Location = new Point(x + 132, y), Size = new Size(110, 26), Text = "VACUUM", BackColor = Color.White, Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleCenter, BorderStyle = BorderStyle.FixedSingle });
            parent.Controls.Add(new Label { Location = new Point(x + 244, y), Size = new Size(110, 26), Text = "BLOW",   BackColor = Color.White, Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleCenter, BorderStyle = BorderStyle.FixedSingle });
        }

        private static void AddIoDot(Control parent, int x, int y, string label)
        {
            parent.Controls.Add(new IndicatorDot { Location = new Point(x + 4, y + 6), Size = new Size(12,12), OnColor = Color.LimeGreen });
            parent.Controls.Add(new Label { Location = new Point(x + 22, y), Size = new Size(200, 20), Text = label, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6,0,0,0), BorderStyle = BorderStyle.FixedSingle });
        }
    }
}
