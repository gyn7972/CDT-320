using System.ComponentModel;
using QMC.CDT_320.Ui.Pages.Material;
using QMC.CDT_320.Ui.Pages.Recipe;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Tabs
{
    /// <summary>레시피 탭 - CDT-300 레시피 화면 기반 + CDT-320 확장 구성.</summary>
    public partial class RecipeTab : TabBase
    {
        public RecipeTab()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            SetSidebarHeader("recipe.section");
            const UserLevel en = UserLevel.Engineer;

            RegisterSidebarButton(BtnProject,          "recipe.project",         en, () => new ProjectPage());
            RegisterSidebarButton(BtnInputCassette,    "recipe.inputCassette",   en, () => new InputCassetteRecipePage());
            RegisterSidebarButton(BtnInputFeeder,      "recipe.inputFeeder",     en, () => new InputFeederRecipePage("recipe.inputFeeder"));
            RegisterSidebarButton(BtnInputStage,       "recipe.inputStage",      en, () => new InputStageRecipePage("recipe.inputStage"));
            RegisterSidebarButton(BtnFrontHead,        "recipe.frontHead",       en, () => new FrontPickerRecipePage());
            RegisterSidebarButton(BtnRearHead,         "recipe.rearHead",        en, () => new RearPickerRecipePage());
            RegisterSidebarButton(BtnOutputFeeder,     "recipe.outputFeeder",    en, () => new OutputFeederRecipePage("recipe.outputFeeder"));
            RegisterSidebarButton(BtnOutputCassette,   "recipe.outputCassette",  en, () => new OutputCassetteRecipePage());
            RegisterSidebarButton(BtnOutputStage,      "recipe.outputStage",     en, () => new OutputStageRecipePage("recipe.outputStage"));
            RegisterSidebarButton(BtnInputVision,      "recipe.inputVision",     en, () => new VisionRecipePage("recipe.inputVision"));
            RegisterSidebarButton(BtnOutputVision,     "recipe.outputVision",    en, () => new VisionRecipePage("recipe.outputVision"));
            RegisterSidebarButton(BtnLowerVision,      "recipe.lowerVision",     en, () => new VisionRecipePage("recipe.lowerVision"));
            RegisterSidebarButton(BtnBottomVision,     "recipe.bottomVision",    en, () => new VisionRecipePage("recipe.bottomVision"));
            RegisterSidebarButton(BtnSideVision,       "recipe.sideVision",      en, () => new VisionRecipePage("recipe.sideVision"));
            RegisterSidebarButton(BtnInputMapCreate,   "recipe.inputMapCreate",  en, () => new MapCreatePage("recipe.inputMapCreate"));
            RegisterSidebarButton(BtnOutputMapCreate,  "recipe.outputMapCreate", en, () => new MapCreatePage("recipe.outputMapCreate"));
            RegisterSidebarButton(BtnDieSubset,        "recipe.dieSubset",       en, () => new DieSubsetPage());
            RegisterSidebarButton(BtnTapeFrameSubset,  "recipe.tapeFrameSubset", en, () => new TapeFrameSubsetPage());
            RegisterSidebarButton(BtnLoadFrame,        "recipe.loadFrame",       en, () => new LoadTapeFrameSubsetPage());
            RegisterSidebarButton(BtnUnloadFrame,      "recipe.unloadFrame",     en, () => new UnloadTapeFrameSubsetPage());
            RegisterSidebarButton(BtnBinCode,          "recipe.binCode",         en, () => new MaterialBinPage());
            RegisterSidebarButton(BtnModuleSubset,     "recipe.moduleSubset",    en, () => new ModuleSubsetPage());
            RegisterSidebarButton(BtnOutputSubset,     "recipe.outputSubset",    en, () => new OutputSubsetPage());
            RegisterSidebarButton(BtnPickupSubset,     "recipe.pickupSubset",    en, () => new PickupSubsetPage());
            RegisterSidebarButton(BtnForceControl,     "recipe.forceControl",    UserLevel.Maintenance, () => new ForceControlPage());
        }
    }
}
