using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>레시피 - INPUT/OUTPUT MAP CREATE. 좌측 대형 맵 + 우측 설정 + MODE + 조그 + 액션.</summary>
    public class MapCreatePage : PageBase
    {
        public MapCreatePage(string titleI18n)
        {
            Controls.Add(CreateSectionHeader(titleI18n));

            // 좌측 맵 영역
            Controls.Add(new Label
            {
                Location = new Point(8, 36), Size = new Size(900, 26),
                Text = "빈 맵", BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });
            var map = new Panel { Location = new Point(8, 66), Size = new Size(900, 880), BackColor = Color.Black, BorderStyle = BorderStyle.FixedSingle };
            Controls.Add(map);

            // 우측 설정
            Controls.Add(RecipeLayout.OrangeBar(920, 36, 400, "설정"));
            int y = 70;
            foreach (var kv in new (string, string)[]
            {
                ("칩 가로",   "7435"),
                ("칩 세로",   "5840"),
                ("칩 PITCH X","7437"),
                ("칩 PITCH Y","5842"),
                ("Wafer 지름","300000"),
                ("Axis X",    "0"),
                ("Axis Y",    "0"),
            })
            {
                RecipeLayout.AddPair(this, 924, y, 140, 260, kv.Item1, kv.Item2);
                y += 32;
            }

            // MODE
            var mode = new GroupBox
            {
                Location = new Point(920, y + 10),
                Size     = new Size(400, 200),
                Text     = "MODE",
                Font     = UiTheme.SectionFont
            };
            string[] modes = { "STANDARD", "START INDEX", "1 REFERENCE INDEX", "2 REFERENCE INDEX", "MANUAL SELECT PICK", "ALIGN CHECK INDEX", "DRAG SELECT PICK" };
            int mi = 0;
            foreach (var m in modes)
            {
                mode.Controls.Add(new RadioButton { Location = new Point(12, 22 + mi * 22), AutoSize = true, Text = m, Checked = (mi == 0) });
                mi++;
            }
            Controls.Add(mode);

            // CREATE / SAVE
            var btnCreate = new ActionButton { Location = new Point(920, y + 220), Size = new Size(400, 40), Text = "CREATE" };
            var btnSave   = new ActionButton { Location = new Point(920, y + 266), Size = new Size(400, 40), Text = "SAVE" };
            Controls.Add(btnCreate); Controls.Add(btnSave);

            // 조그 패드
            Controls.Add(RecipeLayout.XyJog(920, y + 320, 400, 280, withTheta: true));

            // 추가 액션
            int ay = y + 610;
            foreach (var a in new[] { "첫번째 메뉴얼 얼라인 완료", "자동피치", "메뉴얼 첫 번째 등록", "두번 째 등록 후 얼라인", "THETA MATCH MOVE", "X_Y_MATCH MOVE" })
            {
                Controls.Add(new ActionButton { Location = new Point(920, ay), Size = new Size(400, 34), Text = a });
                ay += 38;
            }
        }
    }
}
