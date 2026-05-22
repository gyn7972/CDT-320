using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>레시피 - INPUT/OUTPUT FEEDER.</summary>
    public class FeederRecipePage : PageBase
    {
        public FeederRecipePage(string titleI18n)
        {
            Controls.Add(CreateSectionHeader(titleI18n));

            // 좌측 동작
            Controls.Add(RecipeLayout.OrangeBar(8, 36, 240, "동작"));
            int y = 70;
            foreach (var a in new[] { "HOME 위치 이동", "카세트 위치 이동", "STAGE 위치 이동", "UP", "DOWN", "CLAMP", "UNCLAMP" })
            {
                Controls.Add(new ActionButton { Location = new Point(12, y), Size = new Size(220, 44), Text = a });
                y += 52;
            }
            // 좌하단 I/O
            Controls.Add(RecipeLayout.OrangeBar(8, y + 8, 240, "실린더 & I/O"));
            y += 44;
            foreach (var t in new[] { "FEEDER CLAMP CHECK", "FEEDER UP/DOWN CHECK", "FEEDER RING CHECK", "FEEDER OVERLOAD CHECK" })
            {
                Controls.Add(new IndicatorDot { Location = new Point(14, y + 6), Size = new Size(12, 12), OnColor = Color.LimeGreen });
                Controls.Add(new Label { Location = new Point(30, y), Size = new Size(212, 22), Text = t, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0), BorderStyle = BorderStyle.FixedSingle });
                y += 24;
            }

            // 옵션
            Controls.Add(RecipeLayout.OrangeBar(260, 36, 600, "옵션"));
            y = 70;
            foreach (var kv in new (string, string)[]
            {
                ("HOME 위치",      "0 um"),
                ("카세트 위치",    "50000 um"),
                ("STAGE 위치",     "200000 um"),
                ("UP 완료 센서",   "ENABLE"),
                ("DOWN 완료 센서", "ENABLE"),
                ("CLAMP 시간",     "300 ms"),
                ("FEEDER 속도",    "500 mm/s"),
            })
            {
                RecipeLayout.AddPair(this, 264, y, 200, 380, kv.Item1, kv.Item2);
                y += 32;
            }

            // 대기시간
            Controls.Add(RecipeLayout.OrangeBar(260, y + 8, 600, "대기시간"));
            y += 44;
            RecipeLayout.AddPair(this, 264, y, 200, 380, "이동 후 대기시간", "100 ms"); y += 32;
            RecipeLayout.AddPair(this, 264, y, 200, 380, "UP/DOWN 완료 후", "50 ms");

            // 조그/속도
            Controls.Add(RecipeLayout.OrangeBar(880, 540, 300, "조그 운전"));
            Controls.Add(RecipeLayout.SimpleJog(880, 570, 300, 320, "AXIS Y"));
            Controls.Add(RecipeLayout.VerticalSpeedBar(1200, 36, 140, 880));
        }
    }
}
