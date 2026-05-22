using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT320;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    /// <summary>
    /// Stage 56 — Plate Status Page (CDT-310 매뉴얼 NG/Good Plate 호환).<br/>
    /// Plate Registry 의 NG/Good 적재 현황 + 슬롯별 BinCode 라이브 표시.
    /// </summary>
    public class PlateStatusPage : PageBase
    {
        private const int SLOTS_PER_PLATE = 25;
        private readonly Label[] _ngSlots   = new Label[SLOTS_PER_PLATE];
        private readonly Label[] _goodSlots = new Label[SLOTS_PER_PLATE];
        private Label _lblNgCount, _lblGoodCount;
        private System.Windows.Forms.Timer _timer;

        public PlateStatusPage()
        {
            Controls.Add(CreateSectionHeader("wi.plateStatus"));

            // 좌측 — NG Plate
            BuildPlateColumn("NG PLATE",  Color.LightCoral, 8,   38, _ngSlots, out _lblNgCount);
            // 우측 — Good Plate
            BuildPlateColumn("GOOD PLATE", Color.LightGreen, 320, 38, _goodSlots, out _lblGoodCount);

            // 액션 버튼
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 80, Padding = new Padding(12),
                BackColor = UiTheme.MainBg, FlowDirection = FlowDirection.LeftToRight
            };
            var btnReset = new Button
            {
                Text = "PLATE RESET", Width = 180, Height = 50,
                FlatStyle = FlatStyle.Flat, BackColor = Color.LightYellow,
                Font = UiTheme.SectionFont
            };
            btnReset.Click += (s, e) =>
            {
                if (MessageBox.Show("Plate 적재 데이터를 모두 리셋합니다. 계속하시겠습니까?",
                    "Plate Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                    == DialogResult.Yes)
                {
                    PlateRegistry.Reset();
                    Refresh5();
                }
            };
            actions.Controls.Add(btnReset);
            Controls.Add(actions);

            _timer = new System.Windows.Forms.Timer { Interval = 300 };
            _timer.Tick += (s, e) => Refresh5();
            HandleCreated   += (s, e) => _timer.Start();
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private void BuildPlateColumn(string title, Color bg, int x, int y, Label[] slots, out Label countLabel)
        {
            // 헤더
            Controls.Add(new Label
            {
                Location = new Point(x, y), Size = new Size(280, 26),
                Text = title, BackColor = Color.Black, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleCenter
            });

            // 적재 카운트
            countLabel = new Label
            {
                Location = new Point(x, y + 30), Size = new Size(280, 22),
                Text = "0 / 25", BackColor = bg, ForeColor = Color.Black,
                Font = new Font("Consolas", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(countLabel);

            // 25 슬롯 표시 (5x5 그리드)
            int gridX = x;
            int gridY = y + 60;
            int slotW = 56, slotH = 30;
            for (int i = 0; i < SLOTS_PER_PLATE; i++)
            {
                int row = i / 5;
                int col = i % 5;
                var lbl = new Label
                {
                    Location = new Point(gridX + col * slotW, gridY + row * slotH),
                    Size = new Size(slotW - 2, slotH - 2),
                    Text = (i + 1).ToString("D2"),
                    BackColor = Color.LightGray, ForeColor = Color.Black,
                    Font = new Font("Consolas", 8F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BorderStyle = BorderStyle.FixedSingle
                };
                Controls.Add(lbl);
                slots[i] = lbl;
            }
        }

        private void Refresh5()
        {
            // NG Plate
            UpdatePlate(PlateRegistry.NgPlate, _ngSlots, _lblNgCount, Color.LightCoral);
            // Good Plate
            UpdatePlate(PlateRegistry.GoodPlate, _goodSlots, _lblGoodCount, Color.LightGreen);
        }

        private static void UpdatePlate(Plate p, Label[] slotLbls, Label countLbl, Color filledColor)
        {
            countLbl.Text = $"{p.FilledCount} / {p.MaxSlots}";
            for (int i = 0; i < slotLbls.Length; i++)
            {
                int code = (i < p.Slots.Length) ? p.Slots[i] : 0;
                Color c = code > 0 ? filledColor : Color.LightGray;
                if (slotLbls[i].BackColor != c) slotLbls[i].BackColor = c;
                string txt = code > 0 ? code.ToString() : (i + 1).ToString("D2");
                if (slotLbls[i].Text != txt) slotLbls[i].Text = txt;
            }
        }
    }
}
