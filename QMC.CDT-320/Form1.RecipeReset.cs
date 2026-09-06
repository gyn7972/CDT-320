using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Ui.Security;
using QMC.Common;
using QMC.Common.Logging;

namespace QMC.CDT_320
{
    public partial class Form1
    {
        /// <summary>
        /// 레시피 적용부터 Material/마지막 레시피 저장까지 하나의 운전 차단 구간으로 처리합니다.
        /// 확인창과 사전검증에서는 기존 Material을 변경하지 않습니다.
        /// </summary>
        internal bool TryApplyMachineRecipe(
            RecipeProject project, bool saveProject, out bool cancelled, out string reason)
        {
            cancelled = false;
            reason = string.Empty;
            IDisposable applyLease = null;
            IDisposable uiProtection = null;
            bool mutationStarted = false;
            bool resetMaterial = false;
            string targetName = project != null ? NormalizeRecipeName(project.FileName) : string.Empty;
            string previousName = NormalizeRecipeName(ActiveRecipeName);
            string phase = "사전검증";
            try
            {
                if (project == null || string.IsNullOrWhiteSpace(targetName) || Machine == null || Controller == null)
                    throw new InvalidOperationException("적용할 레시피 또는 장비 제어기가 준비되지 않았습니다.");
                if (InvokeRequired)
                    throw new InvalidOperationException("레시피 적용은 메인 화면에서 실행해야 합니다.");

                bool initialFreshMaterial = string.IsNullOrWhiteSpace(previousName) && !_materialSnapshotRestored;
                bool confirmedSwitch =
                    (!string.IsNullOrWhiteSpace(previousName) &&
                     !string.Equals(previousName, targetName, StringComparison.OrdinalIgnoreCase)) ||
                    Controller.RequiresRecipeResetRecovery(targetName);
                resetMaterial = initialFreshMaterial || confirmedSwitch;
                if (!TryValidateRecipeApplyUiState(out reason))
                    return false;

                bool materialOnlyBlock;
                if (!TryValidateMachineRecipeChange(targetName, out materialOnlyBlock, out reason) &&
                    !(confirmedSwitch && materialOnlyBlock))
                    return false;

                MaterialSnapshot expectedState;
                long expectedVersion;
                MaterialStateService.CaptureRecipeResetState(out expectedState, out expectedVersion);
                string activeLotId = GetActiveLotIdForRecipeReset();
                if (initialFreshMaterial && !MaterialStateService.ReadState(state =>
                    ReferenceEquals(state, expectedState) && state != null &&
                    state.Wafers != null && state.Wafers.Count == 0 && state.Dies != null && state.Dies.Count == 0 &&
                    state.Cassettes != null && state.Cassettes.All(cassette => cassette != null &&
                        !cassette.IsPresent && !cassette.IsMapped && cassette.Slots != null &&
                        cassette.Slots.All(slot => slot != null && !slot.HasWafer &&
                            string.IsNullOrWhiteSpace(slot.WaferId) && string.IsNullOrWhiteSpace(slot.WaferInstanceId)))))
                {
                    reason = "최초 레시피 적용 전 Material이 확인되었습니다. 기존 자료를 복구하거나 정상 정리한 뒤 다시 적용하십시오.";
                    return false;
                }

                // 대상 Unit/Component 전체를 먼저 검사합니다. 실패해도 기존 생산 자료는 그대로입니다.
                if (!Machine.ValidateRecipe(targetName, out reason))
                    return false;

                // 같은 레시피의 Project 저장도 START와 겹치지 않도록 확인창보다 먼저 적용 준비를 독점합니다.
                bool materialRecipeRestore = false;
                if (initialFreshMaterial)
                {
                    if (!Controller.TryBeginStartupMaterialResetOperation(out applyLease, out reason))
                        return false;
                }
                else if (confirmedSwitch)
                {
                    if (!Controller.TryBeginRecipeSwitchPreparationOperation(targetName, out applyLease, out reason))
                        return false;
                }
                else if (!Controller.TryBeginRecipeApplyOperation(targetName, false,
                    out materialRecipeRestore, out applyLease, out reason))
                    return false;
                uiProtection = BeginRecipeApplyUiProtection();
                if (!MaterialStateService.IsRecipeResetStateCurrent(expectedState, expectedVersion, out reason))
                    return false;

                RecipeApplyFileSnapshot preparedFiles = RecipeApplyFileSnapshot.Capture(targetName);
                if (saveProject) project.FileName = targetName;
                RecipeProject preparedProject = saveProject ? project : RecipeStore.Load(targetName);
                if (preparedProject == null ||
                    !string.Equals(NormalizeRecipeName(preparedProject.FileName), targetName, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("대상 Project 파일을 읽지 못했거나 파일 안의 레시피 이름이 일치하지 않습니다.");

                MaterialSnapshot candidate = null;
                if (resetMaterial &&
                    !RecipeMaterialStateFactory.TryCreate(preparedProject, activeLotId, out candidate, out reason))
                    return false;

                if (confirmedSwitch)
                {
                    phase = "실물 제거 확인";
                    int waferCount = expectedState != null && expectedState.Wafers != null ? expectedState.Wafers.Count : 0;
                    int dieCount = expectedState != null && expectedState.Dies != null ? expectedState.Dies.Count : 0;
                    DialogResult answer = MessageDialog.Show(this,
                        "레시피를 변경하고 기존 생산 상태를 초기화합니다.\r\n\r\n" +
                        "현재: " + previousName + " → 대상: " + targetName + "\r\n" +
                        "기존 자료: Wafer " + waferCount + " / Die " + dieCount + "\r\n\r\n" +
                        "Front/Rear 픽커, Stage, Feeder, Cassette의 실제 제품을 모두 제거했는지 확인하십시오.\r\n" +
                        "FLOW가 OFF여도 실제 제품이 남아 있을 수 있습니다.\r\n\r\n" +
                        "[예] 기존 Material·지도·공정 재개 정보를 비우고 새 레시피를 적용합니다.\r\n" +
                        "다음 START는 새 Material 기준으로 진행하며 카세트 재매핑이 필요합니다.\r\n" +
                        "진행 중 LOT과 원점 완료 상태는 유지됩니다.\r\n\r\n" +
                        "실제 제품을 모두 제거했으며 레시피 변경을 진행하시겠습니까?",
                        "레시피 변경 및 Material 초기화", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (answer != DialogResult.Yes)
                    {
                        cancelled = true;
                        reason = "사용자가 레시피 변경을 취소했습니다.";
                        return false;
                    }
                }

                phase = "확인 후 상태 재검증";
                if (!string.Equals(previousName, NormalizeRecipeName(ActiveRecipeName), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(activeLotId, GetActiveLotIdForRecipeReset(), StringComparison.Ordinal))
                    throw new InvalidOperationException("확인 중 활성 레시피 또는 LOT이 변경되었습니다. 현재 상태에서 다시 적용하십시오.");
                if (!MaterialStateService.IsRecipeResetStateCurrent(expectedState, expectedVersion, out reason) ||
                    !TryValidateRecipeApplyUiState(out reason))
                    return false;

                if (confirmedSwitch)
                {
                    if (!Controller.TryConfirmRecipeSwitchEmptyMaterial(targetName, out reason))
                        return false;
                }
                // 보호 진입 직전 다른 UI/검사 작업이 완료되어 자료를 바꿨을 수 있으므로 마지막으로 세대를 확인합니다.
                if (!MaterialStateService.IsRecipeResetStateCurrent(expectedState, expectedVersion, out reason))
                    return false;
                if (!Machine.ValidateRecipe(targetName, out reason))
                    return false;
                if (!preparedFiles.IsCurrent(out reason))
                    return false;

                // 작업자 취소 시 활성 Project 파일도 바뀌지 않도록 저장은 확인과 재검증 뒤에 수행합니다.
                if (saveProject)
                {
                    phase = "Project 저장";
                    // 저장 실패의 복원까지 실패했을 수 있으므로 활성 파일 쓰기를 시도한 시점부터 실패 시 START를 차단합니다.
                    mutationStarted = string.Equals(previousName, targetName, StringComparison.OrdinalIgnoreCase);
                    if (!RecipeStore.Save(project))
                        throw new IOException("대상 Project 파일을 저장하지 못했습니다.");
                    preparedFiles = preparedFiles.CaptureAfterProjectSave();
                    preparedProject = RecipeStore.Load(targetName);
                    if (preparedProject == null)
                        throw new InvalidDataException("저장한 Project 파일을 다시 읽지 못했습니다.");
                    if (resetMaterial && !RecipeMaterialStateFactory.TryCreate(
                        preparedProject, activeLotId, out candidate, out reason))
                        throw new InvalidOperationException(reason);
                }

                phase = "레시피 적용";
                mutationStarted = true;
                if (!LoadMachineRecipeCore(targetName, materialRecipeRestore, reason, false, preparedProject))
                    throw new InvalidOperationException("장비 레시피 적용이 완료되지 않았습니다.");
                if (!preparedFiles.IsCurrent(out reason))
                    throw new InvalidOperationException(reason);

                if (resetMaterial)
                {
                    phase = "생산 상태 초기화";
                    if (confirmedSwitch && !Controller.TryRevalidateConfirmedRecipeSwitch(targetName, true, out reason))
                        throw new InvalidOperationException(reason);
                    if (!MaterialStateService.TryReplaceRecipeState(expectedState, expectedVersion, candidate,
                            "RecipeSwitch:" + previousName + "->" + targetName, out reason))
                        throw new InvalidOperationException(reason);
                    bool runtimeReset = initialFreshMaterial
                        ? Controller.TryResetCompletedRecipeRuntimeForStartup("InitialRecipeApply", out reason)
                        : Controller.TryResetCompletedRecipeRuntime(targetName, out reason);
                    if (!runtimeReset)
                        throw new InvalidOperationException(reason);
                    ResetCompletedInputStageRunReviewForRecipe("RecipeSwitch:" + previousName + "->" + targetName);
                }
                else if (!Controller.CompleteRecipeApplyContext(targetName, out reason))
                    throw new InvalidOperationException(reason);

                Controller.ApplyRecipeMode(preparedProject);
                LotSessionService.SynchronizeActiveLotToMaterial("RecipeApply");
                if (!string.Equals(activeLotId, GetActiveLotIdForRecipeReset(), StringComparison.Ordinal))
                    throw new InvalidOperationException("레시피 적용 중 활성 LOT이 변경되었습니다.");

                phase = "Material 저장";
                if (!MaterialStateService.TryFlushPendingSave("RecipeApplyCommitted:" + targetName))
                    throw new IOException("새 Material 상태의 저장 완료를 확인하지 못했습니다.");
                phase = "마지막 레시피 저장";
                if (!RecipeStore.TrySaveLastProjectName(targetName, out reason))
                    throw new IOException(reason);
                AppSettingsStore.Current.LastProject = targetName;
                if (!AppSettingsStore.TrySave(out reason))
                    throw new IOException(reason);

                phase = "적용 완료 검증";
                if (!string.Equals(ActiveRecipeName, targetName, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(Controller.ActiveRecipeName, targetName, StringComparison.OrdinalIgnoreCase) ||
                    !MaterialStateService.ReadState(state => state != null &&
                        string.Equals(NormalizeRecipeName(state.RecipeName), targetName, StringComparison.OrdinalIgnoreCase) &&
                        (!resetMaterial || (ReferenceEquals(state, candidate) && state.Wafers.Count == 0 && state.Dies.Count == 0)) &&
                        ((!resetMaterial && string.IsNullOrWhiteSpace(activeLotId)) ||
                         string.Equals(state.LotId, activeLotId, StringComparison.Ordinal))) ||
                    !string.Equals(RecipeStore.GetLastProjectName(), targetName, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("활성 레시피·Material·마지막 레시피 기록이 일치하지 않습니다.");
                if (confirmedSwitch && !Controller.TryRevalidateConfirmedRecipeSwitch(targetName, true, out reason))
                    throw new InvalidOperationException(reason);
                if (!preparedFiles.IsCurrent(out reason))
                    throw new InvalidOperationException(reason);

                Controller.ClearRecipeStartBlockAfterSuccessfulApply();
                RefreshProjectName(targetName);
                // 저장 확정 후 새 레시피를 알립니다. 다음 START의 기존 Vision ACK 검사는 그대로 유지됩니다.
                _ = VisionHub.BroadcastRecipeAsync(targetName);
                reason = string.Empty;
                Log.Write("Main", UserSession.Name, "RecipeApply",
                    "레시피 적용 및 저장을 완료했습니다. previous=" + previousName + ", target=" + targetName +
                    ", materialReset=" + resetMaterial + ", lot=" + activeLotId + " - Ok");
                return true;
            }
            catch (Exception ex)
            {
                reason = "레시피 적용 실패. 단계=" + phase + ", 대상=" + targetName + ", 원인=" + ex.Message;
                if (mutationStarted && Controller != null)
                {
                    if (resetMaterial) Controller.BlockRecipeStartAfterFailedMaterialReset(targetName, reason);
                    else Controller.BlockRecipeStartAfterFailedApply(reason);
                }
                EventLogger.Write(EventKind.Alarm, UserSession.Name, "RECIPE-APPLY", reason);
                return false;
            }
            finally
            {
                // 실패 이유를 Controller에 남긴 뒤 보호를 해제합니다. 임의의 다른 레시피 로드로 복구하지 않습니다.
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

        private static string GetActiveLotIdForRecipeReset()
        {
            return LotSessionService.IsLotActive ? (LotSessionService.ActiveLotId ?? string.Empty).Trim() : string.Empty;
        }

        private bool TryValidateRecipeApplyUiState(out string reason)
        {
            reason = string.Empty;
            if ((_inputStageRunReviewDialog != null && !_inputStageRunReviewDialog.IsDisposed) ||
                (_inputStageRunReviewOneShot != null && !_inputStageRunReviewOneShot.Completion.Task.IsCompleted) ||
                _inputStageRunReviewDrainingDialogs.Count != 0 ||
                _inputStageRunReviewJogStartPending || _inputStageRunReviewJogAxis != null ||
                _inputStageRunReviewJogScope != null || _inputStageRunReviewEmbeddedVisionScope != null ||
                _inputStageRunReviewEmbeddedVisionTransition || _inputStageRunReviewVisionTestScope != null ||
                (_inputStageRunReviewVisionTestDialog != null && !_inputStageRunReviewVisionTestDialog.IsDisposed))
            {
                reason = "InputStage Review와 검출·저장·Vision 종료 처리를 모두 완료한 뒤 레시피를 적용하십시오.";
                return false;
            }
            // 기존 확인창의 callback이 옛 Material/파라미터를 뒤늦게 적용하지 않도록 닫힌 상태에서만 진입합니다.
            if (Application.OpenForms.Cast<Form>().Any(form => form != this && !form.IsDisposed && form.Modal))
            {
                reason = "열려 있는 작업 확인/편집 창을 먼저 닫은 뒤 레시피를 적용하십시오.";
                return false;
            }
            return true;
        }

        private void ResetCompletedInputStageRunReviewForRecipe(string reason)
        {
            string blockedReason;
            if (!TryValidateRecipeApplyUiState(out blockedReason))
                throw new InvalidOperationException(blockedReason);
            if (_inputStageRunReviewRequestGeneration == long.MaxValue)
                throw new InvalidOperationException("Review 요청 세대를 더 증가시킬 수 없습니다.");

            // 종료된 UI 요청만 만료시킵니다. 기존 Review의 검사/OFFSET/CONFIRM 및 모션 종료 처리는 호출하지 않습니다.
            ClearInputStageRunReviewPendingOffset(reason);
            _inputStageRunReviewCleanedGeneration = _inputStageRunReviewSessionGeneration;
            _inputStageRunReviewOneShot = null;
            _inputStageRunReviewDialog = null;
            _inputStageRunReviewVisionTestDialog = null;
            _inputStageRunReviewPendingCaptureX = 0.0;
            _inputStageRunReviewPendingCaptureY = 0.0;
            _inputStageRunReviewPendingCaptureT = 0.0;
            _inputStageRunReviewDieDetectionSimulated = false;
        }

        private IDisposable BeginRecipeApplyUiProtection()
        {
            var controls = new List<Control> { _recipeTab, _workInfoTab, _settingsTab, _userTab };
            controls.AddRange(Application.OpenForms.Cast<Form>().Where(form => form != this && !form.IsDisposed));
            return new RecipeApplyUiProtection(controls);
        }

        private sealed class RecipeApplyUiProtection : IDisposable
        {
            private readonly Dictionary<Control, bool> _enabled = new Dictionary<Control, bool>();

            public RecipeApplyUiProtection(IEnumerable<Control> controls)
            {
                try
                {
                    foreach (Control control in controls)
                    {
                        if (control == null || control.IsDisposed || _enabled.ContainsKey(control)) continue;
                        _enabled.Add(control, control.Enabled);
                        control.Enabled = false;
                    }
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            public void Dispose()
            {
                foreach (KeyValuePair<Control, bool> item in _enabled)
                {
                    if (!item.Key.IsDisposed) item.Key.Enabled = item.Value;
                }
                _enabled.Clear();
            }
        }

        private void InitializeFreshMaterialStateForStartup(RecipeProject recipe)
        {
            string reason = string.Empty;
            IDisposable startupLease = null;
            try
            {
                MaterialSnapshot candidate;
                if (recipe == null)
                {
                    candidate = MaterialStorage.CreateDefaultState(1, 1, 25, 25);
                    candidate.LotId = GetActiveLotIdForRecipeReset();
                }
                else if (!RecipeMaterialStateFactory.TryCreate(recipe, GetActiveLotIdForRecipeReset(), out candidate, out reason))
                    throw new InvalidOperationException(reason);

                if (Controller == null || !Controller.TryBeginStartupMaterialResetOperation(out startupLease, out reason))
                    throw new InvalidOperationException(Controller == null ? "장비 제어기가 준비되지 않았습니다." : reason);
                if (!TryValidateRecipeApplyUiState(out reason))
                    throw new InvalidOperationException(reason);
                MaterialSnapshot expected;
                long expectedVersion;
                MaterialStateService.CaptureRecipeResetState(out expected, out expectedVersion);
                if (!MaterialStateService.TryReplaceRecipeState(expected, expectedVersion, candidate, "StartupFreshMaterial", out reason))
                    throw new InvalidOperationException(reason);
                if (Controller != null && !Controller.TryResetCompletedRecipeRuntimeForStartup("StartupFreshMaterial", out reason))
                    throw new InvalidOperationException(reason);
                ResetCompletedInputStageRunReviewForRecipe("StartupFreshMaterial");
                if (!MaterialStateService.TryFlushPendingSave("StartupFreshMaterial"))
                    throw new IOException("새 Material 상태의 저장 완료를 확인하지 못했습니다.");
                // 대상 레시피가 결정된 최종 기동 초기화만 이전 실패를 해소할 수 있습니다.
                if (recipe != null) Controller?.ClearRecipeStartBlockAfterSuccessfulApply();
            }
            catch (Exception ex)
            {
                reason = "새 Material 시작 상태 초기화 실패. " + ex.Message;
                Controller?.BlockRecipeStartAfterFailedMaterialReset(
                    recipe != null ? NormalizeRecipeName(recipe.FileName) : NormalizeRecipeName(ActiveRecipeName), reason);
                EventLogger.Write(EventKind.Alarm, UserSession.Name, "MATERIAL-STARTUP-FRESH", reason);
                MessageDialog.Show(this, reason, "Material 초기화", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (startupLease != null) startupLease.Dispose();
            }
        }
    }
}
