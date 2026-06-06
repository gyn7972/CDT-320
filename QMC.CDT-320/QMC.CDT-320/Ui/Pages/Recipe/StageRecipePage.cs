using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>레시피 - INPUT/OUTPUT STAGE. 옵션 + STAGE VISION 뷰 + X/Y/T + EXPAND/NEEDLE/NEEDLE BLOCK Z 조그.</summary>
    public class StageRecipePage : PageBase
    {
        public StageRecipePage(string titleI18n)
        {
            Controls.Add(CreateSectionHeader(titleI18n));

            // 좌측 옵션
            Controls.Add(RecipeLayout.OrangeBar(8, 36, 400, "옵션"));
            int y = 70;
            foreach (var a in new[]
            {
                "LOADING 위치", "중심 위치", "NEEDLE 위치", "익스팬드 위치",
                "바코드 위치", "VISION ALIGN", "작업 반경", "오토 위치 및 컨버전",
                "Needle 측정 위치"
            })
            {
                RecipeLayout.AddSettingRow(this, 12, y, 200, 120, a);
                y += 36;
            }
            // 대기시간
            y += 8;
            Controls.Add(RecipeLayout.OrangeBar(8, y, 400, "대기시간"));
            y += 34;
            RecipeLayout.AddPair(this, 12, y, 140, 80, "NEEDLE UP (P)", "0 ms");
            RecipeLayout.AddPair(this, 232, y, 100, 80, "VACUUM ON",    "0 ms"); y += 32;
            RecipeLayout.AddPair(this, 12, y, 140, 80, "NEEDLE DOWN (P)","0 ms");
            RecipeLayout.AddPair(this, 232, y, 100, 80, "VACUUM OFF",   "0 ms"); y += 32;
            RecipeLayout.AddPair(this, 12, y, 140, 80, "MOVING",       "0 ms"); y += 40;

            // 매뉴얼 동작
            Controls.Add(RecipeLayout.OrangeBar(8, y, 400, "MANUAL 동작"));
            y += 34;
            string[] manL = { "로딩/언로딩 위치 이동", "중심 위치 이동", "BARCODE 위치 이동", "첫 번째 얼라인 위치 이동", "PICK UP TEST" };
            string[] manR = { "NEEDLE UP 위치 이동", "NEEDLE DOWN 위치 이동", "NEEDLE READY 위치 이동", "니들 블록 준비 위치 이동", "니들 블록 작업 위치 이동", "Auto 셋팅 위치 이동", "INPUT CONVERSION 동작", "EXPAND 작업 위치 이동" };
            int leftY = y; int rightY = y;
            // Stage 60 — 한국어 긴 라벨이 width 안에 들어가지 않아 잘리는 이슈 수정: 폰트 10pt
            var manFont = new Font("맑은 고딕", 10F, FontStyle.Bold);
            foreach (var m in manL) { var b = new ActionButton { Location = new Point(12,  leftY),  Size = new Size(185, 36), Text = m, Font = manFont }; Controls.Add(b); leftY  += 40; }
            foreach (var m in manR) { var b = new ActionButton { Location = new Point(206, rightY), Size = new Size(202, 36), Text = m, Font = manFont }; Controls.Add(b); rightY += 40; }

            // CYLINDER & I/O
            int cy = System.Math.Max(leftY, rightY) + 8;
            Controls.Add(RecipeLayout.OrangeBar(8, cy, 400, "CYLINDER & I/O"));
            cy += 30;
            Controls.Add(new IndicatorDot { Location = new Point(14, cy + 6), Size = new Size(12, 12), OnColor = Color.LimeGreen });
            Controls.Add(new Label { Location = new Point(30, cy), Size = new Size(130, 22), Text = "NEEDLE VACUUM", BackColor = Color.FromArgb(0xD0,0xD0,0xD0), Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6,0,0,0), BorderStyle = BorderStyle.FixedSingle });
            Controls.Add(new Label { Location = new Point(162,cy), Size = new Size(110, 22), Text = "VACUUM", BackColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("맑은 고딕", 9F), BorderStyle = BorderStyle.FixedSingle });
            Controls.Add(new Label { Location = new Point(274,cy), Size = new Size(130, 22), Text = "RING 감지 SENS", BackColor = Color.FromArgb(0xD0,0xD0,0xD0), Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6,0,0,0), BorderStyle = BorderStyle.FixedSingle });

            // 중앙 STAGE VISION
            Controls.Add(RecipeLayout.CameraView(420, 40, 760, 540, "STAGE VISION"));

            // 조그 + X/Y/T + Expand/Needle/NeedleBlock Z
            Controls.Add(RecipeLayout.OrangeBar(420, 600, 760, "조그 운전"));
            // X/Y/T value readouts
            int rx = 432, ry = 636;
            RecipeLayout.AddPair(this, rx,       ry,    80, 100, "AXIS X", "0 um");
            RecipeLayout.AddPair(this, rx + 190, ry,    80, 100, "AXIS T", "0 um"); ry += 30;
            RecipeLayout.AddPair(this, rx,       ry,    80, 100, "AXIS Y", "0 um");
            RecipeLayout.AddPair(this, rx + 190, ry,    80, 100, "EXPAND Z",    "0 um"); ry += 30;
            RecipeLayout.AddPair(this, rx,       ry,    80, 100, "AXIS NEEDLE Z", "0 um");
            RecipeLayout.AddPair(this, rx + 190, ry,    80, 100, "NEEDLE BLOCK Z","0 um"); ry += 40;

            // XY T jog
            Controls.Add(RecipeLayout.XyJog(432, ry, 260, 260, withTheta: true));
            // Expand Z / Needle Z / Needle Block Z (각 상하 버튼)
            int jx = 710;
            foreach (var axis in new[] { "EXPAND Z", "NEEDLE Z", "NEEDLE BLOCK Z" })
            {
                Controls.Add(new Button { Location = new Point(jx, ry),        Size = new Size(80, 80), Text = "▲", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat });
                Controls.Add(new Label  { Location = new Point(jx, ry + 84),   Size = new Size(80, 22), Text = axis, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("맑은 고딕", 8F) });
                Controls.Add(new Button { Location = new Point(jx, ry + 110),  Size = new Size(80, 80), Text = "▼", Font = new Font("맑은 고딕", 18F), FlatStyle = FlatStyle.Flat });
                jx += 96;
            }

            // 속도
            Controls.Add(RecipeLayout.VerticalSpeedBar(1200, 36, 140, 880));
        }
    }
}
