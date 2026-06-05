using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.History
{
    /// <summary>이력 - MESSAGE 편집. 코드별 메시지 번역/커스터마이즈.</summary>
    public class MessageEditPage : PageBase
    {
        public MessageEditPage()
        {
            Controls.Add(CreateSectionHeader("hist.msgEdit"));
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill, AllowUserToAddRows = true, RowHeadersVisible = false,
                BackgroundColor = Color.White, Font = new Font("맑은 고딕", 9F),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 26 },
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(0x50,0x50,0x50), ForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
                }
            };
            grid.Columns.Add("CODE",  "CODE");
            grid.Columns.Add("KIND",  "KIND");
            grid.Columns.Add("KO",    "한국어");
            grid.Columns.Add("EN",    "ENGLISH");
            grid.Rows.Add("90100", "EVENT", "(MAIN MENU) OPERATION PAGE 버튼이 클릭되었습니다.", "(MAIN MENU) OPERATION PAGE button clicked.");
            grid.Rows.Add("90101", "EVENT", "(MAIN MENU) WORKING PAGE 버튼이 클릭되었습니다.",  "(MAIN MENU) WORKING PAGE button clicked.");
            grid.Rows.Add("8100",  "ALARM", "PICK 실패",   "PICK failed");
            grid.Rows.Add("8101",  "ALARM", "PLACE 실패",  "PLACE failed");
            grid.Rows.Add("7200",  "WARN",  "VACUUM 낮음", "Vacuum low");
            Controls.Add(grid);
            Controls.SetChildIndex(grid, 0);

            var act = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(8),
                BackColor = UiTheme.MainBg, FlowDirection = FlowDirection.LeftToRight
            };
            act.Controls.Add(new Button { Size = new Size(120, 32), Text = Lang.T("common.save"),   Tag = "i18n:common.save",   FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, Margin = new Padding(4) });
            act.Controls.Add(new Button { Size = new Size(120, 32), Text = Lang.T("common.add"),    Tag = "i18n:common.add",    FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, Margin = new Padding(4) });
            act.Controls.Add(new Button { Size = new Size(120, 32), Text = Lang.T("common.delete"), Tag = "i18n:common.delete", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, Margin = new Padding(4) });
            Controls.Add(act);
        }
    }
}
