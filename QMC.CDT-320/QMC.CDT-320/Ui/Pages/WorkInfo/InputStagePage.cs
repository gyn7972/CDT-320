using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    /// <summary>작업정보 - INPUT STAGE.</summary>
    public class InputStagePage : PageBase
    {
        public InputStagePage()
        {
            Controls.Add(CreateSectionHeader("tab.workInfo"));

            // 좌상단 작업 상태
            int y = 38;
            AddOrangeHeader(8, y, 600, "tab.workInfo");
            var statuses = new (string key, string val)[]
            {
                ("wi.stageExist",     "없음"),
                ("wi.stageAlign",     "미완료"),
                ("wi.stageBarcode",   "미완료"),
                ("wi.stageChipAlign", "미완료"),
                ("wi.stageFinish",    "미완료")
            };
            y += 32;
            foreach (var s in statuses)
            {
                var r = InfoRows.Pair(s.key, s.val, labelW: 200, valueW: 220);
                r.Location = new Point(8, y);
                Controls.Add(r);
                y += 32;
            }

            // 상단 중앙: NEEDLE/JELL 카운트
            var c1 = InfoRows.Pair("wi.needleUsing", "0 ea", labelW: 180, valueW: 200); c1.Location = new Point(440, 70);   Controls.Add(c1);
            var c2 = InfoRows.Pair("wi.jellPadUsing","0 ea", labelW: 180, valueW: 200); c2.Location = new Point(440, 168);  Controls.Add(c2);

            // 우상단 STAGE & NEEDLE 실린더 정보
            AddOrangeHeader(860, 38, 440, "stage.needleCylInfo");
            var exp = InfoRows.Pair("wi.expending",   "...", labelW: 200, valueW: 220); exp.Location = new Point(860, 70); Controls.Add(exp);
            var upD = InfoRows.Pair("wi.needleUpDown","...", labelW: 200, valueW: 220); upD.Location = new Point(860, 102); Controls.Add(upD);

            // 하단 정보 섹션
            AddOrangeHeader(8, 220, 1300, "common.info");
            // 축 2×2 블록
            var axBlocks = new (int x, int y, string title)[]
            {
                (8,   260, "STAGE AXIS X"),
                (280, 260, "STAGE AXIS T"),
                (8,   322, "STAGE AXIS Y"),
                (280, 322, "NEEDLE AXIS Z")
            };
            foreach (var a in axBlocks)
            {
                var b = BuildAxisBlock(a.title, "0 um");
                b.Location = new Point(a.x, a.y);
                Controls.Add(b);
            }
            // IO
            var io1 = BuildIoBlock("STAGE RING CHECK #1"); io1.Location = new Point(8,  390); Controls.Add(io1);
            var io2 = BuildIoBlock("STAGE RING CHECK #2"); io2.Location = new Point(8,  422); Controls.Add(io2);
            var io3 = BuildIoBlock("NEEDLE VACUUM");       io3.Location = new Point(8,  454); Controls.Add(io3);

            // 하단 ACTION
            var act = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 90, Padding = new Padding(12),
                BackColor = UiTheme.MainBg, FlowDirection = FlowDirection.LeftToRight
            };
            act.Controls.Add(new ActionButton { Text = Lang.T("wi.wfAlign"),   Tag = "i18n:wi.wfAlign",   Width = 160, Margin = new Padding(6) });
            act.Controls.Add(new ActionButton { Text = Lang.T("wi.wfBarcode"), Tag = "i18n:wi.wfBarcode", Width = 160, Margin = new Padding(6) });
            Controls.Add(act);
        }

        private void AddOrangeHeader(int x, int y, int w, string i18nKey)
        {
            Controls.Add(new Label
            {
                Location  = new Point(x, y),
                Size      = new Size(w, 26),
                Text      = Lang.T(i18nKey),
                Tag       = "i18n:" + i18nKey,
                BackColor = UiTheme.StatusBarBg,
                ForeColor = Color.White,
                Font      = UiTheme.SectionFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0)
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
