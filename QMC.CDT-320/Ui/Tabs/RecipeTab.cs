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

            // 레시피 사이드바 메뉴는 로케일과 무관하게 대문자 영어로 표시한다.
            // (번역 데이터/권한 Tag는 그대로 두고 표시 텍스트만 영어로 오버라이드)
            ApplyEnglishMenuLabels();
            Lang.LanguageChanged += OnLanguageChangedMenu;
        }

        /// <summary>사이드바 메뉴 버튼 텍스트를 영어(대문자)로 강제한다. Tag(i18n/권한)는 유지.</summary>
        private void ApplyEnglishMenuLabels()
        {
            try
            {
                foreach (var kv in SidebarButtons)
                {
                    if (kv.Value == null) continue;
                    string en = Lang.TEn(kv.Key);
                    if (!string.IsNullOrEmpty(en)) kv.Value.Text = en.ToUpperInvariant();
                }
                if (BtnDieMapSetup != null)
                {
                    string en = Lang.TEn("recipe.dieMapSetup");
                    if (!string.IsNullOrEmpty(en)) BtnDieMapSetup.Text = en.ToUpperInvariant();
                }
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Lang.LanguageChanged -= OnLanguageChangedMenu;
            base.Dispose(disposing);
        }

        private void OnLanguageChangedMenu()
        {
            // Lang.Apply 가 동기적으로 Text 를 되돌린 뒤에 다시 영어로 덮어쓰도록 지연 실행.
            try
            {
                if (IsDisposed || !IsHandleCreated) { ApplyEnglishMenuLabels(); return; }
                BeginInvoke((Action)ApplyEnglishMenuLabels);
            }
            catch { }
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
