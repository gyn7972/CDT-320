using System.ComponentModel;
using System.Windows.Forms;
using QMC.Vision.Ui.Pages;

namespace QMC.Vision.Ui.Tabs
{
    /// <summary>
    /// 레시피 탭 — 핸들러 RecipeTab 정렬.
    /// RecipePage 는 자체 2단 네비(알고리즘→세팅)를 가지므로, 1차로는 RecipePage 를 콘텐츠로 호스트하고
    /// 탭 사이드바는 숨긴다. (후속: 알고리즘 네비를 탭 사이드바로 끌어올려 완전 정렬)
    /// </summary>
    public class RecipeTab : TabBase
    {
        public RecipeTab()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            PnlSidebar.Visible = false;
            var page = new RecipePage { Dock = DockStyle.Fill };
            PnlContent.Controls.Add(page);
        }
    }
}
