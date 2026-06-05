using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    /// <summary>
    /// 320 전용 — FRONT HEAD / REAR HEAD 용 재사용 페이지.
    /// <paramref name="headLabel"/> 로 헤드 명칭 구분 ("FRONT HEAD" / "REAR HEAD").
    /// </summary>
    public class HeadPage : PageBase
    {
        public HeadPage(string headTitleI18n)
        {
            Controls.Add(CreateSectionHeader(headTitleI18n));

            // 작업 상태
            int y = 38;
            AddOrangeHeader(8, y, 460, "tab.workInfo");
            y += 32;
            var h1 = InfoRows.Pair("wi.head1", "EMPTY", valueColor: Color.Green); h1.Location = new Point(8, y); Controls.Add(h1); y += 32;
            var h2 = InfoRows.Pair("wi.head2", "EMPTY", valueColor: Color.Green); h2.Location = new Point(8, y); Controls.Add(h2); y += 32;
            var btnInit = new Button { Location = new Point(8, y), Size = new Size(160, 30), Text = Lang.T("wi.head.initAll"), Tag = "i18n:wi.head.initAll", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            Controls.Add(btnInit); y += 40;

            var steps = new (string key, string val)[]
            {
                ("wi.head.colletChange",   "미완료"),
                ("wi.head.autoPos",        "미완료"),
                ("wi.head.colletCleaning", "미완료"),
                ("wi.head.colletCheck",    "미완료"),
            };
            foreach (var s in steps)
            {
                var r = InfoRows.Pair(s.key, s.val);
                r.Location = new Point(8, y);
                Controls.Add(r);
                y += 32;
            }

            // 우상단: PICK/PLACE 실패 + Collet 사용
            var p1 = InfoRows.Pair("wi.pickFail",   "0 ea", labelW: 160, valueW: 140); p1.Location = new Point(500, 70);  Controls.Add(p1);
            var p2 = InfoRows.Pair("wi.placeFail",  "0 ea", labelW: 160, valueW: 140); p2.Location = new Point(820, 70);  Controls.Add(p2);
            var p3 = InfoRows.Pair("wi.collet1Use", "0 ea", labelW: 160, valueW: 140); p3.Location = new Point(500, 102); Controls.Add(p3);
            var p4 = InfoRows.Pair("wi.collet2Use", "0 ea", labelW: 160, valueW: 140); p4.Location = new Point(820, 102); Controls.Add(p4);
            var btnClear = new Button { Location = new Point(820, 140), Size = new Size(140, 32), Text = Lang.T("wi.head.countClear"), Tag = "i18n:wi.head.countClear", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            Controls.Add(btnClear);

            // 하단 정보 (Stage 60 — width 1300 → 540: 콘텐츠 영역과 일치하도록 축소)
            AddOrangeHeader(8, 280, 540, "common.info");
            var axT = BuildAxisBlock("HEAD AXIS T", "0 um"); axT.Location = new Point(8, 320); Controls.Add(axT);
            var io1 = BuildIoBlock("HEAD VACUUM #1"); io1.Location = new Point(8,  382); Controls.Add(io1);
            var io2 = BuildIoBlock("HEAD VACUUM #2"); io2.Location = new Point(8,  414); Controls.Add(io2);
            var io3 = BuildIoBlock("HEAD BLOW #1");   io3.Location = new Point(280,382); Controls.Add(io3);
            var io4 = BuildIoBlock("HEAD BLOW #2");   io4.Location = new Point(280,414); Controls.Add(io4);
        }

        private void AddOrangeHeader(int x, int y, int w, string i18nKey)
        {
            Controls.Add(new Label
            {
                Location  = new Point(x, y), Size = new Size(w, 26),
                Text = Lang.T(i18nKey), Tag = "i18n:" + i18nKey,
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });
        }

        private static Control BuildAxisBlock(string title, string value)
        {
            var p = new Panel { Size = new Size(260, 54) };
            p.Controls.Add(new Label { Location = new Point(0, 0),  Size = new Size(260, 24), Text = title, BackColor = Color.Black, ForeColor = Color.White, Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0) });
            p.Controls.Add(new Label { Location = new Point(0, 26), Size = new Size(260, 28), Text = value, BackColor = Color.White, ForeColor = Color.Black, Font = new Font("Consolas", 10F), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 6, 0), BorderStyle = BorderStyle.FixedSingle });
            return p;
        }

        private static Control BuildIoBlock(string title)
        {
            var p = new Panel { Size = new Size(260, 28) };
            p.Controls.Add(new IndicatorDot { Location = new Point(6, 8), Size = new Size(12, 12), OnColor = Color.LimeGreen });
            p.Controls.Add(new Label { Location = new Point(24, 0), Size = new Size(236, 28), Text = title, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), ForeColor = Color.Black, Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0) });
            return p;
        }
    }
}
