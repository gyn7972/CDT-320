using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// INPUT / OUTPUT CASSETTE STATUS 공용 다이얼로그.
    /// 25슬롯 리스트 + 각 슬롯 상태 색상 + READY/WORKING/FINISH 레전드.
    /// </summary>
    public class CstStatusDialog : Form
    {
        public CstStatusDialog(bool isInput)
        {
            Text            = isInput ? "INPUT CASSETTE STATUS" : "OUTPUT CASSETTE STATUS";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MinimizeBox     = MaximizeBox = false;
            ClientSize      = new Size(520, 720);
            BackColor       = UiTheme.MainBg;
            ShowIcon        = false;

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 40,
                Text = Text, BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = new Font("맑은 고딕", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(title);

            // 레전드
            var legend = new Panel { Location = new Point(10, 50), Size = new Size(500, 70), BorderStyle = BorderStyle.FixedSingle };
            AddLegend(legend, 10, 8,   Color.Cyan,      "READY");
            AddLegend(legend, 180,8,   Color.LimeGreen, "EMPTY");
            AddLegend(legend, 350,8,   Color.Orange,    "WORKING");
            AddLegend(legend, 10, 40,  Color.Red,       "FINISH");
            AddLegend(legend, 180,40,  Color.Navy,      "WORK READY");
            Controls.Add(legend);

            // 25슬롯 (25→1 역순 표시 - 상단이 25, 하단이 1)
            var slots = new Panel { Location = new Point(10, 130), Size = new Size(500, 540), AutoScroll = true, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            int y = 0;
            var rnd = new System.Random(7);
            for (int i = 25; i >= 1; i--)
            {
                var row = new Panel { Location = new Point(0, y), Size = new Size(490, 20) };
                row.Controls.Add(new Label { Location = new Point(0, 0),  Size = new Size(40, 20),  Text = i.ToString("D2"), Font = new Font("Consolas", 9F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter });
                // 랜덤 색상으로 시드
                Color c = Color.LimeGreen;
                int rr = rnd.Next(10);
                if (rr < 2) c = Color.Cyan;
                else if (rr < 4) c = Color.Orange;
                else if (rr < 5) c = Color.Red;
                else if (rr < 6) c = Color.Navy;
                row.Controls.Add(new Label { Location = new Point(44, 0), Size = new Size(446, 20), BackColor = c });
                slots.Controls.Add(row);
                y += 22;
            }
            Controls.Add(slots);

            // 하단 닫기 버튼
            var btnClose = new Button
            {
                Location = new Point(410, 678), Size = new Size(100, 30),
                Text = Lang.T("common.ok"), Tag = "i18n:common.ok",
                FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont,
                DialogResult = DialogResult.OK
            };
            Controls.Add(btnClose);
            AcceptButton = btnClose;

            Load += (s, e) => Lang.Apply(this);
        }

        private static void AddLegend(Control parent, int x, int y, Color c, string text)
        {
            parent.Controls.Add(new Label { Location = new Point(x, y),      Size = new Size(28, 20), BackColor = c });
            parent.Controls.Add(new Label { Location = new Point(x + 34, y), Size = new Size(120, 20), Text = text, Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft });
        }
    }
}
