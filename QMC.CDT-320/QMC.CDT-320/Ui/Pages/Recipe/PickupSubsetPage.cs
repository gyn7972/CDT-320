using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>
    /// Stage 61 — Pickup Sequence Subset 편집.
    /// 시작 코너 (4 코너) / 방향 (가로/세로) / 패턴 (직선/지그재그) 설정.
    /// </summary>
    public class PickupSubsetPage : SubsetPageBase
    {
        private RadioButton _rbTL, _rbTR, _rbBL, _rbBR;
        private RadioButton _rbHoriz, _rbVert;
        private RadioButton _rbStraight, _rbZigZag;

        public PickupSubsetPage() : base("recipe.pickupSubset") { }

        protected override void BuildEditor(Panel c)
        {
            int yy = 10;

            // ── 시작 코너 — 자체 Panel (4개 radio mutually exclusive 그룹) ──────
            var cornerHeader = MakeGroupHeader("▮ 시작 코너 (Start Corner)", 10, yy);
            c.Controls.Add(cornerHeader);
            yy += 32;

            int cornerW = 200, cornerH = 50;
            int cornerPanelH = 2 * (cornerH + 4) + 8;
            var cornerPanel = new Panel
            {
                Location = new Point(10, yy), Size = new Size(450, cornerPanelH),
                BackColor = Color.Transparent
            };
            _rbTL = MakeCornerRadio("◤  좌상단  (Top Left)",     0,           4,             cornerW, cornerH);
            _rbTR = MakeCornerRadio("◥  우상단  (Top Right)",    cornerW+10,  4,             cornerW, cornerH);
            _rbBL = MakeCornerRadio("◣  좌하단  (Bottom Left)",  0,           cornerH+12,    cornerW, cornerH);
            _rbBR = MakeCornerRadio("◢  우하단  (Bottom Right)", cornerW+10,  cornerH+12,    cornerW, cornerH);
            cornerPanel.Controls.Add(_rbTL); cornerPanel.Controls.Add(_rbTR);
            cornerPanel.Controls.Add(_rbBL); cornerPanel.Controls.Add(_rbBR);
            c.Controls.Add(cornerPanel);
            yy += cornerPanelH + 16;

            // ── 픽업 방향 — 자체 Panel (2개 radio 그룹) ─────────────────────
            c.Controls.Add(MakeGroupHeader("▮ 픽업 방향 (Direction)", 10, yy));
            yy += 32;
            var dirPanel = new Panel
            {
                Location = new Point(10, yy), Size = new Size(490, 96),
                BackColor = Color.Transparent
            };
            _rbHoriz = MakeArrowRadio("▶  가로  (한 Row 내 모든 Col → 다음 Row)", 0, 0,  480, 40);
            _rbVert  = MakeArrowRadio("▼  세로  (한 Col 내 모든 Row → 다음 Col)", 0, 48, 480, 40);
            dirPanel.Controls.Add(_rbHoriz); dirPanel.Controls.Add(_rbVert);
            c.Controls.Add(dirPanel);
            yy += 96 + 16;

            // ── 패턴 — 자체 Panel (2개 radio 그룹) ───────────────────────────
            c.Controls.Add(MakeGroupHeader("▮ 패턴 (Pattern)", 10, yy));
            yy += 32;
            var patPanel = new Panel
            {
                Location = new Point(10, yy), Size = new Size(490, 96),
                BackColor = Color.Transparent
            };
            _rbStraight = MakeArrowRadio("━━━━  직선  (매 Row/Col 같은 시작점)", 0, 0,  480, 40);
            _rbZigZag   = MakeArrowRadio("⤺⤻⤺  지그재그  (교번 방향)",        0, 48, 480, 40);
            patPanel.Controls.Add(_rbStraight); patPanel.Controls.Add(_rbZigZag);
            c.Controls.Add(patPanel);
        }

        private static Label MakeGroupHeader(string text, int x, int y)
        {
            return new Label
            {
                Location = new Point(x, y), Size = new Size(490, 24),
                Text = text,
                Font = UiTheme.SectionFont, ForeColor = Color.DarkSlateGray,
                BorderStyle = BorderStyle.FixedSingle, BackColor = Color.LightYellow,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
            };
        }

        // Stage 61 — 선택/미선택 색상 (하이라이트)
        private static readonly Color RbUnchecked = Color.FromArgb(0xF7, 0xFA, 0xFD);
        private static readonly Color RbChecked   = Color.FromArgb(0x2E, 0x86, 0xDE);   // 진한 파랑
        private static readonly Color RbCheckedFg = Color.White;
        private static readonly Color RbUncheckedFg = Color.FromArgb(0x33, 0x33, 0x33);

        private static void ApplyRadioColors(RadioButton rb)
        {
            if (rb.Checked)
            {
                rb.BackColor = RbChecked;
                rb.ForeColor = RbCheckedFg;
                rb.FlatAppearance.BorderSize = 3;
                rb.FlatAppearance.BorderColor = Color.FromArgb(0x17, 0x4F, 0x8C);
            }
            else
            {
                rb.BackColor = RbUnchecked;
                rb.ForeColor = RbUncheckedFg;
                rb.FlatAppearance.BorderSize = 1;
                rb.FlatAppearance.BorderColor = Color.LightGray;
            }
        }

        private RadioButton MakeCornerRadio(string text, int x, int y, int w, int h)
        {
            var rb = new RadioButton
            {
                Location = new Point(x, y), Size = new Size(w, h),
                Text = text,
                Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter
            };
            rb.CheckedChanged += (s, e) => ApplyRadioColors(rb);
            ApplyRadioColors(rb);
            return rb;
        }

        private RadioButton MakeArrowRadio(string text, int x, int y, int w, int h)
        {
            var rb = new RadioButton
            {
                Location = new Point(x, y), Size = new Size(w, h),
                Text = text,
                Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };
            rb.CheckedChanged += (s, e) => ApplyRadioColors(rb);
            ApplyRadioColors(rb);
            return rb;
        }

        protected override void LoadFromRecipe()
        {
            var p = _project.Pickup ?? new PickupSubset();
            _rbTL.Checked = p.StartCorner == PickupStartCorner.TopLeft;
            _rbTR.Checked = p.StartCorner == PickupStartCorner.TopRight;
            _rbBL.Checked = p.StartCorner == PickupStartCorner.BottomLeft;
            _rbBR.Checked = p.StartCorner == PickupStartCorner.BottomRight;
            _rbHoriz.Checked = p.Direction == PickupDirection.Horizontal;
            _rbVert .Checked = p.Direction == PickupDirection.Vertical;
            _rbStraight.Checked = p.Pattern == PickupPattern.Straight;
            _rbZigZag  .Checked = p.Pattern == PickupPattern.ZigZag;
        }

        protected override void SaveToRecipe()
        {
            var p = _project.Pickup ?? (_project.Pickup = new PickupSubset());
            if      (_rbTL.Checked) p.StartCorner = PickupStartCorner.TopLeft;
            else if (_rbTR.Checked) p.StartCorner = PickupStartCorner.TopRight;
            else if (_rbBL.Checked) p.StartCorner = PickupStartCorner.BottomLeft;
            else if (_rbBR.Checked) p.StartCorner = PickupStartCorner.BottomRight;
            p.Direction = _rbVert.Checked ? PickupDirection.Vertical : PickupDirection.Horizontal;
            p.Pattern   = _rbZigZag.Checked ? PickupPattern.ZigZag : PickupPattern.Straight;

            // 저장 즉시 MachineController 의 PickupOptions 도 반영 + 시퀀스 재생성
            try
            {
                var host = FindForm() as Form1;
                if (host?.Controller != null)
                {
                    host.Controller.PickupOptions = p;
                    host.Controller.RebuildPickupSequence();
                }
            }
            catch { /* host 미초기화 환경 안전 무시 */ }
        }
    }
}
