using System.ComponentModel;
using QMC.CDT_320.Ui.Pages;
using QMC.CDT_320.Ui.Pages.Material;
using QMC.CDT_320.Ui.Pages.Recipe;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Tabs
{
    /// <summary>레시피 — 300 구조 + 320 확장 (FRONT/REAR HEAD, Bottom/Side Vision).</summary>
    public partial class RecipeTab : TabBase
    {
        public RecipeTab()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            SetSidebarHeader("recipe.section");
            const UserLevel en = UserLevel.Engineer;

            AddSidebarButton("recipe.project",        en, () => new ProjectPage());
            AddSidebarButton("recipe.inputCassette",  en, () => new InputCassetteRecipePage());
            AddSidebarButton("recipe.inputFeeder",    en, () => new FeederRecipePage("recipe.inputFeeder"));
            AddSidebarButton("recipe.inputStage",     en, () => new StageRecipePage("recipe.inputStage"));
            AddSidebarButton("recipe.frontHead",      en, () => new HeadRecipePage("recipe.frontHead"));
            AddSidebarButton("recipe.rearHead",       en, () => new HeadRecipePage("recipe.rearHead"));
            AddSidebarButton("recipe.outputFeeder",   en, () => new FeederRecipePage("recipe.outputFeeder"));
            AddSidebarButton("recipe.outputCassette", en, () => new OutputCassetteRecipePage());
            AddSidebarButton("recipe.outputStage",    en, () => new StageRecipePage("recipe.outputStage"));
            AddSidebarSpacer();
            AddSidebarButton("recipe.inputVision",    en, () => new VisionRecipePage("recipe.inputVision"));
            AddSidebarButton("recipe.outputVision",   en, () => new VisionRecipePage("recipe.outputVision"));
            AddSidebarButton("recipe.lowerVision",    en, () => new VisionRecipePage("recipe.lowerVision"));
            AddSidebarButton("recipe.bottomVision",   en, () => new VisionRecipePage("recipe.bottomVision"));
            AddSidebarButton("recipe.sideVision",     en, () => new VisionRecipePage("recipe.sideVision"));
            AddSidebarSpacer();
            AddSidebarButton("recipe.inputMapCreate", en, () => new MapCreatePage("recipe.inputMapCreate"));
            AddSidebarButton("recipe.outputMapCreate",en, () => new MapCreatePage("recipe.outputMapCreate"));
            AddSidebarSpacer();
            // 310 이식 — Subset Recipe + BinCode
            AddSidebarButton("recipe.dieSubset",       en, () => new DieSubsetPage());
            AddSidebarButton("recipe.tapeFrameSubset", en, () => new TapeFrameSubsetPage());
            AddSidebarButton("recipe.loadFrame",       en, () => new LoadTapeFrameSubsetPage());
            AddSidebarButton("recipe.unloadFrame",     en, () => new UnloadTapeFrameSubsetPage());
            AddSidebarButton("recipe.binCode",         en, () => new MaterialBinPage());
            // Stage 38 — Module Subset (Pick/Collet/Inspection 옵션)
            AddSidebarButton("recipe.moduleSubset",    en, () => new ModuleSubsetPage());
            // Stage 57 — Output Subset (Plate + DiesPerWafer 등)
            AddSidebarButton("recipe.outputSubset",    en, () => new OutputSubsetPage());
            // Stage 61 — Pickup Sequence Subset (시작 코너 + 방향 + 지그재그/직선)
            AddSidebarButton("recipe.pickupSubset",    en, () => new PickupSubsetPage());

            AddSidebarButton("recipe.forceControl", UserLevel.Maintenance, () => new ForceControlPage(), toBottomArea: true);
        }
    }
}
