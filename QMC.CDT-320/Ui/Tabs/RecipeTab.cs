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
            LblSidebarHeader.BackColor = System.Drawing.Color.White;   // 작업정보 탭 헤더와 동일하게
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

            HideUnimplementedSidebarButtons();

            // 권한 Tag와 메뉴 키를 유지하고 현재 언어의 표시명을 사용한다.
            ApplyLocalizedMenuLabels();
            Lang.LanguageChanged += OnLanguageChangedMenu;
        }

        // [사이드바 정리 2026-08-10] 아직 사용하지 않는 사이드바 버튼을 화면에서만 숨긴다.
        //
        // - 등록(RegisterSidebarButton)은 그대로 두고 Visible 만 내린다.
        //   기능이 준비되면 아래 목록에서 해당 버튼만 빼면 배선 작업 없이 다시 나타난다.
        // - PnlSidebarButtons 는 FlowLayoutPanel 이라 숨긴 항목의 자리는 자동으로 사라지고 빈칸이 생기지 않는다.
        // - 배치 순서는 RecipeTab.Designer.cs 에서 이미 숨김 대상을 맨 아래로 내려두었다.
        // - 사용자 레벨 접근 제어(AccessControl)는 Enabled 만 조정하므로 이 숨김을 되돌리지 않는다.
        // - PnlForceSeparator 는 숨김 구역만 나누던 구분선이라 함께 숨긴다(맨 아래 빈 줄 방지).
        private void HideUnimplementedSidebarButtons()
        {
            Control[] hidden =
            {
                PnlForceSeparator,   // 숨김 구역 구분선
                BtnLoadFrame,        // 로드 웨이퍼
                BtnUnloadFrame,      // 언로드 웨이퍼
                BtnModuleSubset,     // 모듈 옵션
                BtnOutputSubset,     // 출력 옵션
                BtnForceControl      // FORCE CONTROL
            };

            foreach (Control control in hidden)
            {
                if (control != null)
                    control.Visible = false;
            }
        }

        /// <summary>사이드바 메뉴에 현재 언어를 적용한다. Tag(i18n/권한)는 유지.</summary>
        private void ApplyLocalizedMenuLabels()
        {
            try
            {
                foreach (var kv in SidebarButtons)
                {
                    if (kv.Value == null) continue;
                    string label = Lang.T(kv.Key);
                    if (!string.IsNullOrEmpty(label)) kv.Value.Text = label;
                }
                if (BtnDieMapSetup != null)
                {
                    string label = Lang.T("recipe.dieMapSetup");
                    if (!string.IsNullOrEmpty(label)) BtnDieMapSetup.Text = label;
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
            // 공통 언어 적용 이후에도 현재 언어의 메뉴 표시를 유지한다.
            try
            {
                if (IsDisposed || !IsHandleCreated) { ApplyLocalizedMenuLabels(); return; }
                BeginInvoke((Action)ApplyLocalizedMenuLabels);
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
                    Host.SaveMachineRecipe(Host.ActiveRecipeName);
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
