using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>
    /// 레시피 - INPUT/OUTPUT CASSETTE (공용).
    /// 좌측 동작 버튼 + 중앙 옵션 + 대기시간 + 실린더 I/O + 우측 조그/속도.
    /// </summary>
    public class CassetteRecipePage : PageBase
    {
        public CassetteRecipePage(string titleI18n)
        {
            Controls.Add(CreateSectionHeader(titleI18n));

            // 좌측 동작
            Controls.Add(RecipeLayout.OrangeBar(8, 36, 240, "동작"));
            int y = 70;
            foreach (var a in new[] { "LOADING 위치 이동", "UNLOADING 위치 이동", "준비 위치 이동" })
            {
                Controls.Add(new ActionButton { Location = new Point(12, y), Size = new Size(220, 44), Text = a });
                y += 52;
            }
            y += 180;
            Controls.Add(new ActionButton { Location = new Point(12, y), Size = new Size(220, 44), Text = "SLOT 위치 이동 (LOADING)" });   y += 52;
            Controls.Add(new ActionButton { Location = new Point(12, y), Size = new Size(220, 44), Text = "SLOT 위치 이동 (UNLOADING)" }); y += 52;

            // 좌하단 실린더 & I/O
            Controls.Add(RecipeLayout.OrangeBar(8, y + 8, 240, "실린더 & I/O"));
            y += 44;
            foreach (var t in new[] { "CASSETTE 감지 SENSOR 1", "CASSETTE 감지 SENSOR 2", "돌출 감지 SENSOR", "맵핑센서" })
            {
                Controls.Add(new IndicatorDot { Location = new Point(14, y + 6), Size = new Size(12, 12), OnColor = Color.LimeGreen });
                Controls.Add(new Label { Location = new Point(30, y), Size = new Size(212, 22), Text = t, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0), BorderStyle = BorderStyle.FixedSingle });
                y += 24;
            }

            // 중앙 옵션
            Controls.Add(RecipeLayout.OrangeBar(260, 36, 600, "옵션"));
            y = 70;
            foreach (var kv in new (string, string)[]
            {
                ("LOADING Z",     "121070 um"),
                ("UNLOADING Z",   "117000 um"),
                ("READY POSITION","129771 um"),
                ("MAPPING Z",     "104745 um"),
                ("SLOT PITCH",    "19000 um"),
                ("카세트 간격",   "59000 um"),
                ("8인치 or 12인치","12인치"),
                ("카세트 2단 활성화","1단"),
            })
            {
                RecipeLayout.AddPair(this, 264, y, 200, 380, kv.Item1, kv.Item2);
                y += 32;
            }

            // 대기시간
            Controls.Add(RecipeLayout.OrangeBar(260, y + 8, 600, "대기시간"));
            y += 44;
            RecipeLayout.AddPair(this, 264, y, 200, 380, "이동 후 대기시간", "100 ms");

            // 조그 영역
            Controls.Add(RecipeLayout.OrangeBar(880, 540, 300, "조그 운전"));
            var jog = RecipeLayout.SimpleJog(880, 570, 300, 320, "AXIS Z");
            Controls.Add(jog);

            // 속도
            Controls.Add(RecipeLayout.VerticalSpeedBar(1200, 36, 140, 880));
        }
    }
}
