using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>레시피 - FORCE CONTROL. 좌측 DO 리스트 + 우측 DI 리스트, 중앙에 강제 ON/OFF 버튼.</summary>
    public class ForceControlPage : PageBase
    {
        public ForceControlPage()
        {
            Controls.Add(CreateSectionHeader("recipe.forceControl"));

            // 좌측 DO
            Controls.Add(RecipeLayout.OrangeBar(8, 36, 620, "DO FORCE"));
            var grpDo = new DataGridView
            {
                Location = new Point(8, 66), Size = new Size(620, 800),
                ReadOnly = false, AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White, Font = new Font("맑은 고딕", 9F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 26 }
            };
            grpDo.Columns.Add("IDX", "IDX");
            grpDo.Columns.Add("SYM", "SYM");
            grpDo.Columns.Add("DESC","DESCRIPTION");
            grpDo.Columns.Add("STATE","STATE");
            for (int i = 0; i < 10; i++)
                grpDo.Rows.Add(i + 1, "Y" + i.ToString("D3"), "Sample DO " + i, "OFF");
            Controls.Add(grpDo);

            // 우측 DI (읽기 전용)
            Controls.Add(RecipeLayout.OrangeBar(640, 36, 620, "DI STATE"));
            var grpDi = new DataGridView
            {
                Location = new Point(640, 66), Size = new Size(620, 800),
                ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White, Font = new Font("맑은 고딕", 9F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 26 }
            };
            grpDi.Columns.Add("IDX", "IDX");
            grpDi.Columns.Add("SYM", "SYM");
            grpDi.Columns.Add("DESC","DESCRIPTION");
            grpDi.Columns.Add("STATE","STATE");
            for (int i = 0; i < 10; i++)
                grpDi.Rows.Add(i + 1, "X" + i.ToString("D3"), "Sample DI " + i, "OFF");
            Controls.Add(grpDi);

            // 하단 제어 버튼
            var act = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 60, BackColor = UiTheme.MainBg, Padding = new Padding(8),
                FlowDirection = FlowDirection.LeftToRight
            };
            act.Controls.Add(new ActionButton { Text = "FORCE ON",  Size = new Size(140, 44), Margin = new Padding(4) });
            act.Controls.Add(new ActionButton { Text = "FORCE OFF", Size = new Size(140, 44), Margin = new Padding(4) });
            act.Controls.Add(new ActionButton { Text = "ALL OFF",   Size = new Size(140, 44), Margin = new Padding(4) });
            Controls.Add(act);
        }
    }
}
