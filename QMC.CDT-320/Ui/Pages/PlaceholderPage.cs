using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages
{
    /// <summary>
    /// 아직 구현되지 않은 서브 페이지 — 중앙에 "<페이지명> (placeholder)" 만 표시.
    /// 팩토리에서 생성 시 i18n 키를 받아 자동으로 라벨 적용.
    /// </summary>
    public class PlaceholderPage : PageBase
    {
        public PlaceholderPage() : this("common.caption") { }

        public PlaceholderPage(string i18nKey)
        {
            var header = CreateSectionHeader(i18nKey);
            Controls.Add(header);

            var lbl = new Label
            {
                Dock      = DockStyle.Fill,
                Text      = Lang.T(i18nKey) + "   (placeholder)",
                Tag       = "i18n:" + i18nKey,
                Font      = new Font("Segoe UI", 20F),
                ForeColor = Color.FromArgb(0x55, 0x55, 0x55),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(lbl);
            Controls.SetChildIndex(header, 0);
        }
    }
}
