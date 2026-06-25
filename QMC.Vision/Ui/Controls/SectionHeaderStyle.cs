using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 섹션 타이틀 라벨을 GENERAL 스타일(회색 배경 + 명칭 + 주황 밑줄)로 통일하는 런타임 헬퍼.
    /// 기존에 주황 블록으로 박혀 있던 편집 페이지(VisionTarget/InspectorTarget 등) 섹션 헤더에 적용.
    /// </summary>
    public static class SectionHeaderStyle
    {
        private static readonly Color HeaderBg = Color.FromArgb(232, 234, 237);
        private static readonly Color HeaderFg = Color.FromArgb(48, 52, 58);
        private static readonly Color Accent   = Color.FromArgb(217, 119, 6);

        public static void Apply(params Label[] labels)
        {
            if (labels == null) return;
            foreach (var lbl in labels)
            {
                if (lbl == null) continue;
                lbl.BackColor = HeaderBg;
                lbl.ForeColor = HeaderFg;

                // 주황 밑줄(중복 추가 방지).
                bool hasAccent = false;
                foreach (Control c in lbl.Controls)
                {
                    if (c is Panel p && (p.Tag as string) == "sectionAccent") { hasAccent = true; break; }
                }
                if (!hasAccent)
                {
                    lbl.Controls.Add(new Panel
                    {
                        Dock = DockStyle.Bottom,
                        Height = 2,
                        BackColor = Accent,
                        Tag = "sectionAccent"
                    });
                }
            }
        }
    }
}
