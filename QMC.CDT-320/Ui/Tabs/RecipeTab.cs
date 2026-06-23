using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Pages.Material;
using QMC.CDT_320.Ui.Pages.Recipe;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Tabs
{
    /// <summary>Recipe tab. CDT-300 recipe screen base with CDT-320 extensions.</summary>
    public partial class RecipeTab : TabBase
    {
        private InputStageDieMapSetupDialog _dieMapSetupDialog;

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
            RegisterSidebarButton(BtnInputVision,      "recipe.inputVision",     en, () => new VisionRecipePage_Old("recipe.inputVision"));
            RegisterSidebarButton(BtnOutputVision,     "recipe.outputVision",    en, () => new VisionRecipePage_Old("recipe.outputVision"));
            RegisterSidebarButton(BtnLowerVision,      "recipe.lowerVision",     en, () => new VisionRecipePage_Old("recipe.lowerVision"));
            RegisterSidebarButton(BtnBottomVision,     "recipe.bottomVision",    en, () => new VisionRecipePage_Old("recipe.bottomVision"));
            RegisterSidebarButton(BtnSideVision,       "recipe.sideVision",      en, () => new VisionRecipePage_Old("recipe.sideVision"));
            RegisterSidebarButton(BtnVisionStage,      "recipe.visionStage",     en, () => new VisionRecipePage("recipe.visionStage"));
            RegisterSidebarButton(BtnInputMapCreate,   "recipe.inputMapCreate",  en, () => new MapCreatePage("recipe.inputMapCreate"));
            RegisterSidebarButton(BtnOutputMapCreate,  "recipe.binMapCreate",    en, () => new MapCreatePage("recipe.binMapCreate"));
            RegisterSidebarActionButton(BtnDieMapSetup, "recipe.dieMapSetup",    en, OpenDieMapSetup);
            RegisterSidebarButton(BtnDieSubset,        "recipe.dieSubset",       en, () => new DieSubsetPage());
            RegisterSidebarButton(BtnTapeFrameSubset,  "recipe.tapeFrameSubset", en, () => new TapeFrameSubsetPage());
            RegisterSidebarButton(BtnLoadFrame,        "recipe.loadFrame",       en, () => new LoadTapeFrameSubsetPage());
            RegisterSidebarButton(BtnUnloadFrame,      "recipe.unloadFrame",     en, () => new UnloadTapeFrameSubsetPage());
            RegisterSidebarButton(BtnBinCode,          "recipe.binCode",         en, () => new MaterialBinPage());
            RegisterSidebarButton(BtnModuleSubset,     "recipe.moduleSubset",    en, () => new ModuleSubsetPage());
            RegisterSidebarButton(BtnOutputSubset,     "recipe.outputSubset",    en, () => new OutputSubsetPage());
            RegisterSidebarButton(BtnPickupSubset,     "recipe.pickupSubset",    en, () => new PickupSubsetPage());
            RegisterSidebarButton(BtnForceControl,     "recipe.forceControl",    UserLevel.Maintenance, () => new ForceControlPage());
            RegisterSidebarButton(BtnCalibration,      "recipe.calibration",     UserLevel.Maintenance, () => new CalibrationPage());
        }

        private void RegisterSidebarActionButton(SidebarButton button, string i18nKey, UserLevel minLevel, Action onClick)
        {
            try
            {
                if (button == null)
                    return;

                button.Tag = "i18n:" + i18nKey + ";level:" + minLevel;
                button.Text = Lang.T(i18nKey);
                button.Click += (s, e) => onClick?.Invoke();
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "RecipeSidebarAction",
                    "Recipe action button bind failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void OpenDieMapSetup()
        {
            try
            {
                if (Host == null || Host.Machine == null || Host.Machine.InputStageUnit == null)
                {
                    QMC.Common.MessageDialog.Show(FindForm(), "InputStage Unit을 찾을 수 없습니다.", "Die Map Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ShowPage("recipe.inputStage");

                if (_dieMapSetupDialog != null && !_dieMapSetupDialog.IsDisposed)
                {
                    _dieMapSetupDialog.Activate();
                    return;
                }

                _dieMapSetupDialog = new InputStageDieMapSetupDialog(Host.Machine.InputStageUnit, SaveCurrentRecipe);
                _dieMapSetupDialog.Owner = FindForm();
                _dieMapSetupDialog.StartPosition = FormStartPosition.Manual;
                _dieMapSetupDialog.Location = ResolveDieMapSetupLocation(_dieMapSetupDialog);
                _dieMapSetupDialog.FormClosed += (s, e) => _dieMapSetupDialog = null;
                _dieMapSetupDialog.Show(FindForm());
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "OpenDieMapSetup",
                    "Open Die Map Setup failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(FindForm(), "Die Map Setup 열기 실패:\r\n" + ex.Message, "Die Map Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private Point ResolveDieMapSetupLocation(Form dialog)
        {
            Form owner = FindForm();
            if (owner == null || dialog == null)
                return new Point(100, 100);

            int x = owner.Left + Math.Max(20, owner.Width - dialog.Width - 260);
            int y = owner.Top + 180;
            return new Point(x, y);
        }

        private void SaveCurrentRecipe()
        {
            try
            {
                if (Host != null)
                    Host.SaveMachineRecipe(Host.CurrentRecipeName);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "SaveDieMapSetup",
                    "Save Die Map Setup failed: " + ex.Message + " - Failed");
                throw;
            }
            finally
            {
            }
        }
    }
}
