using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320;
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
        internal string GetProjectConfigurationStatus(RecipeProject saved)
        {
            if (saved == null || !string.Equals(NormalizeRecipeName(saved.FileName),
                NormalizeRecipeName(ActiveRecipeName), StringComparison.OrdinalIgnoreCase))
                return "선택 레시피 미적용";
            if (_currentRecipe == null) return "장비 반영 상태 확인 필요";
            using (var activeStream = new MemoryStream())
            using (var savedStream = new MemoryStream())
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(RecipeProject));
                serializer.WriteObject(activeStream, _currentRecipe);
                serializer.WriteObject(savedStream, saved);
                return activeStream.ToArray().SequenceEqual(savedStream.ToArray())
                    ? "저장값과 적용된 프로젝트 설정 일치"
                    : "저장값과 적용된 프로젝트 설정 다름 · 장비 적용 확인 필요";
            }
        }

        internal bool TrySaveAndApplyWaferMapProcessing(RecipeProject project,
            WaferMapProcessSettings input, WaferMapProcessSettings output,
            WaferMapProcessSettingsDialog editor, out RecipeProject savedProject, out string reason)
        {
            savedProject = null;
            reason = string.Empty;
            IDisposable applyLease = null;
            IDisposable uiProtection = null;
            bool persistenceStarted = false;
            string targetName = project == null ? "" : NormalizeRecipeName(project.FileName);
            try
            {
                if (project == null || Machine == null || Controller == null || string.IsNullOrWhiteSpace(targetName))
                    throw new InvalidOperationException("현재 장비와 활성 레시피를 확인할 수 없습니다.");
                if (InvokeRequired || editor == null || editor.IsDisposed || !editor.Modal ||
                    !Application.OpenForms.Cast<Form>().Contains(editor))
                    throw new InvalidOperationException("레시피 공정 맵 설정 창에서 저장을 실행하세요.");
                if (!string.Equals(targetName, NormalizeRecipeName(ActiveRecipeName), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(targetName, NormalizeRecipeName(Controller.ActiveRecipeName), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("현재 불러온 활성 레시피에서만 맵 설정을 적용할 수 있습니다.");
                if (!TryValidateRecipeApplyUiState(out reason, editor)) return false;

                MaterialSnapshot expectedState;
                long expectedVersion;
                MaterialStateService.CaptureRecipeResetState(out expectedState, out expectedVersion);
                string activeLotId = GetActiveLotIdForRecipeReset();
                RecipeApplyFileSnapshot files = RecipeApplyFileSnapshot.Capture(targetName);
                RecipeProject current = RecipeStore.Load(targetName);
                if (current == null || !string.Equals(targetName, NormalizeRecipeName(current.FileName), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("현재 레시피 파일을 다시 읽지 못했습니다.");
                bool network = RecipeInputMapSource.UsesRemote(current, AppSettingsStore.Current);
                bool requestedNetwork = editor.InputUsesRemote;
                if (network != requestedNetwork && MaterialStateService.ReadState(state => state != null &&
                    state.Wafers != null && state.Wafers.Any(wafer => wafer != null && wafer.CurrentLocation != null &&
                        (wafer.CurrentLocation.Kind == MaterialLocationKind.InputStage ||
                         wafer.CurrentLocation.Kind == MaterialLocationKind.InputFeeder ||
                         wafer.CurrentLocation.Kind == MaterialLocationKind.InputCassette) &&
                        (wafer.InputPreparedMap != null || wafer.HasInputStageDieMappingResult))))
                    throw new InvalidOperationException("이미 맵이 준비되거나 매핑된 입력 웨이퍼가 있습니다. 해당 작업을 완료하고 자재를 정리한 뒤 맵 사용 모드를 변경하세요.");
                RecipeProject updated = RecipeMapProcessSettingsService.CreateUpdatedProject(current, input, output, requestedNetwork);

                // 저장부터 활성 설정 갱신까지 기존 START/수동/검사/Material 보호 구간을 보유한다.
                bool materialRecipeRestore;
                if (!Controller.TryBeginRecipeApplyOperation(targetName, false,
                    out materialRecipeRestore, out applyLease, out reason)) return false;
                if (materialRecipeRestore)
                    throw new InvalidOperationException("Material 레시피 복구를 먼저 완료한 뒤 공정 맵 설정을 저장하세요.");
                uiProtection = BeginRecipeApplyUiProtection();
                if (!MaterialStateService.IsRecipeResetStateCurrent(expectedState, expectedVersion, out reason) ||
                    !TryValidateRecipeApplyUiState(out reason, editor) || !files.IsCurrent(out reason)) return false;
                if (!string.Equals(targetName, NormalizeRecipeName(ActiveRecipeName), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(activeLotId, GetActiveLotIdForRecipeReset(), StringComparison.Ordinal) ||
                    network != RecipeInputMapSource.UsesRemote(current, AppSettingsStore.Current))
                    throw new InvalidOperationException("저장 준비 중 활성 레시피·LOT 또는 맵 사용 모드가 변경되었습니다. 다시 저장하세요.");

                persistenceStarted = true;
                if (!RecipeStore.Save(updated)) throw new IOException("공정 맵 설정을 레시피에 저장하지 못했습니다.");
                files = files.CaptureAfterProjectSave();
                RecipeProject verified = RecipeStore.Load(targetName);
                if (verified == null || verified.InputUseRemoteWaferMap != requestedNetwork || WaferMapProcessService.GetSettingsKey(verified.InputMapProcessing) != WaferMapProcessService.GetSettingsKey(input) ||
                    WaferMapProcessService.GetSettingsKey(verified.OutputMapProcessing) != WaferMapProcessService.GetSettingsKey(output))
                    throw new InvalidDataException("저장된 공정 맵 설정이 요청한 설정과 일치하지 않습니다.");
                if (!files.IsCurrent(out reason)) throw new InvalidOperationException(reason);

                // 공정 맵 소비자는 활성 Recipe 파일을 읽는다. 동일 보호 구간에서 화면의 활성 객체도 갱신한다.
                // Unit 재로딩이나 Material 재생성은 이 설정 저장에 필요하지 않다.
                // 기존 공통 모드 OFF와 동일하게 LOT의 임시 BIN 선택을 초기화한다.
                if (network && !requestedNetwork)
                    MaterialStateService.ResetPickupBinSelectionToAll("RecipeInputMapModeOff:" + targetName);
                _currentRecipe = verified;
                Controller.NotifyRecipeConfigurationApplied();
                savedProject = verified;
                Log.Write("Main", UserSession.Name, "RecipeMapProcessing",
                    "공정 맵 설정 저장 및 활성 적용 완료. recipe=" + targetName +
                    ", remote=" + requestedNetwork + ", input=" + WaferMapProcessService.GetSettingsKey(input) +
                    ", output=" + WaferMapProcessService.GetSettingsKey(output) + " - Ok");
                return true;
            }
            catch (Exception ex)
            {
                reason = "공정 맵 설정 저장/적용 실패. recipe=" + targetName + ", 원인=" + ex.Message;
                if (persistenceStarted && Controller != null)
                {
                    Controller.BlockRecipeStartAfterFailedApply(reason);
                    reason += " 레시피를 확인하고 정상 재적용하기 전까지 START가 차단됩니다.";
                }
                Log.Write("Main", UserSession.Name, "RecipeMapProcessing", reason + ", exception=" + ex + " - Failed");
                return false;
            }
            finally
            {
                try { if (uiProtection != null) uiProtection.Dispose(); }
                finally { if (applyLease != null) applyLease.Dispose(); }
            }
        }
    }
}
