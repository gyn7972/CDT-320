using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Security;
using QMC.Common;

namespace QMC.CDT_320
{
    public partial class Form1
    {
        /// <summary>미리보기 맵을 역할별 레시피 파일로 저장한다. 현재 Material과 Unit 설정은 변경하지 않는다.</summary>
        public bool TrySaveGeneratedWaferMap(RecipeProject project, GeneratedWaferMap generated,
            bool outputRole, WaferMapCreateDialog editor, out string reason)
        {
            reason = string.Empty;
            IDisposable applyLease = null;
            IDisposable uiProtection = null;
            bool persistenceStarted = false;
            string targetName = project != null ? NormalizeRecipeName(project.FileName) : string.Empty;
            try
            {
                if (project == null || generated == null || generated.Count == 0 ||
                    Machine == null || Controller == null || string.IsNullOrWhiteSpace(targetName))
                    throw new InvalidOperationException("저장할 맵 또는 활성 레시피가 준비되지 않았습니다.");
                // 사용자 요청에 따라 초과 다이도 PENDING 설정으로 저장한다.
                // 모든 좌표를 보존하며 FINAL APPLY·장비 사용의 경계 검증은 기존 경로에서 유지한다.
                if (InvokeRequired || editor == null || editor.IsDisposed || !editor.Modal ||
                    !Application.OpenForms.Cast<Form>().Contains(editor))
                    throw new InvalidOperationException("맵 생성 미리보기 창에서 저장을 실행하십시오.");
                if (!string.Equals(targetName, NormalizeRecipeName(ActiveRecipeName), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(targetName, NormalizeRecipeName(Controller.ActiveRecipeName), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("편집 중인 레시피와 활성 레시피가 다릅니다. 현재 레시피를 다시 열어 맵을 생성하십시오.");
                if (!TryValidateRecipeApplyUiState(out reason, editor))
                    return false;

                MaterialSnapshot expectedState;
                long expectedVersion;
                MaterialStateService.CaptureRecipeResetState(out expectedState, out expectedVersion);
                string activeLotId = GetActiveLotIdForRecipeReset();
                RecipeApplyFileSnapshot preparedFiles = RecipeApplyFileSnapshot.Capture(targetName);
                // 역할 전환 중 UI에 남아 있는 다른 역할의 미저장 편집값까지 함께 저장하지 않는다.
                RecipeProject saveProject = RecipeStore.Load(targetName);
                if (saveProject == null || !string.Equals(targetName, NormalizeRecipeName(saveProject.FileName), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("저장할 활성 레시피를 다시 읽지 못했습니다.");

                // 파일을 쓰기 전에 기존 레시피 보호 구간에 진입한다. 운전·수동·검사·자재 조건을 우회하지 않는다.
                bool materialRecipeRestore;
                if (!Controller.TryBeginRecipeApplyOperation(targetName, false,
                    out materialRecipeRestore, out applyLease, out reason))
                    return false;
                if (materialRecipeRestore)
                    throw new InvalidOperationException("Material 레시피 복구를 먼저 완료한 뒤 맵을 저장하십시오.");
                uiProtection = BeginRecipeApplyUiProtection();
                if (!MaterialStateService.IsRecipeResetStateCurrent(expectedState, expectedVersion, out reason) ||
                    !TryValidateRecipeApplyUiState(out reason, editor) || !preparedFiles.IsCurrent(out reason))
                    return false;
                if (!string.Equals(targetName, NormalizeRecipeName(ActiveRecipeName), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(activeLotId, GetActiveLotIdForRecipeReset(), StringComparison.Ordinal))
                    throw new InvalidOperationException("저장 준비 중 활성 레시피 또는 LOT이 변경되었습니다. 현재 상태에서 다시 저장하십시오.");

                RecipeMapBuildResult result = RecipeMapBuildService.CreateGeneratedBaseAndBuildRole(
                    saveProject, generated, outputRole, candidate =>
                    {
                        persistenceStarted = true;
                        return RecipeStore.Save(candidate);
                    });
                persistenceStarted = persistenceStarted || result.PersistenceStarted;
                if (!result.Success)
                    throw new IOException(result.Message);

                RecipeProject savedProject = RecipeStore.Load(targetName);
                if (savedProject == null)
                    throw new InvalidDataException("저장한 레시피를 다시 읽지 못했습니다.");
                _currentRecipe = savedProject;
                Controller.NotifyRecipeConfigurationApplied();
                Log.Write("Main", UserSession.Name, "SaveGeneratedWaferMap",
                    "생성 맵을 저장했습니다. recipe=" + targetName + ", role=" + (outputRole ? "OUTPUT" : "INPUT") +
                    ", grid=" + generated.UsedColumns + "x" + generated.UsedRows + ", count=" + generated.Count +
                    ", rotation=" + generated.RotationDegrees + ", boundaryOverflow=" + generated.OutOfBoundsCount +
                    ", approval=PENDING - Ok");
                return true;
            }
            catch (Exception ex)
            {
                reason = "생성 맵 저장 실패. recipe=" + targetName + ", 원인=" + ex.Message;
                if (persistenceStarted && Controller != null)
                {
                    // 기존 저장 계층의 복원까지 실패했을 가능성이 있으므로 성공한 레시피 재적용으로만 해제한다.
                    Controller.BlockRecipeStartAfterFailedApply(reason);
                    reason += " 저장 결과를 확인하고 레시피를 다시 적용하기 전까지 START가 차단됩니다.";
                }
                Log.Write("Main", UserSession.Name, "SaveGeneratedWaferMap", reason + ", exception=" + ex + " - Failed");
                return false;
            }
            finally
            {
                try
                {
                    if (uiProtection != null) uiProtection.Dispose();
                }
                finally
                {
                    if (applyLease != null) applyLease.Dispose();
                }
            }
        }
    }
}
